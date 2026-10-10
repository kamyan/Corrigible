using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Baskının 3D tahtası (Into the Breach tarzı izometrik görünüm). Kurallar Core'daki <see cref="Baskin"/>'de; bu bileşen
    /// durumu çizer, fareyle kare seçtirir ve bir hamlenin efekt listesini (<see cref="Baskin.Fx"/>) sırayla canlandırır.
    /// Kare (x, y) dünyada <see cref="W"/> konumundadır: x sağa-aşağı, y sola-aşağı (izometrik elmas).
    /// </summary>
    public sealed class RaidBoard3D : MonoBehaviour
    {
        /// <summary>Karoların ve vurguların çizildiği bilgi: RaidView hesaplar, tahta yalnızca gösterir.</summary>
        public sealed class Marks
        {
            public HashSet<int> Moves = new HashSet<int>(), Targets = new HashSet<int>(), Heal = new HashSet<int>(), Spawns = new HashSet<int>();
            /// <summary>Tehlikeli kare → kesin mi (yalan olabilecek niyetler kesin değildir).</summary>
            public Dictionary<int, bool> Danger = new Dictionary<int, bool>();
            public HashSet<int> Spent = new HashSet<int>();
            public int Selected = -1;
            public Dictionary<int, (string order, string flag, Color flagColor)> Badges = new Dictionary<int, (string, string, Color)>();
            public List<(int fx, int fy, int tx, int ty, Color color, bool dashed)> Arrows = new List<(int, int, int, int, Color, bool)>();
        }

        [SerializeField] RaidPieceLibrary lib;
        [SerializeField] Camera cam;
        [SerializeField] Transform tilesRoot;
        [SerializeField] Transform piecesRoot;
        [SerializeField] Transform marksRoot;
        [Tooltip("Düşman turunda bir adımın süresi (saniye).")]
        [SerializeField, Min(0.05f)] float stepTime = 0.32f;

        public event Action<int, int> TileClicked;
        public event Action<(int x, int y)?> HoverChanged;
        public bool Busy { get; private set; }
        public bool InputEnabled { get; set; } = true;
        public Camera Camera => cam;

        public const float TileTop = 0f;
        public static Vector3 W(float x, float y) => new Vector3(x - 3.5f, TileTop, 3.5f - y);
        static int Key(int x, int y) => y * Baskin.N + x;

        readonly Dictionary<int, RaidPiece> units = new Dictionary<int, RaidPiece>();
        readonly Dictionary<int, RaidPiece> objects = new Dictionary<int, RaidPiece>();
        readonly Dictionary<int, GameObject> rubble = new Dictionary<int, GameObject>();
        readonly Renderer[] tint = new Renderer[64];
        readonly GameObject[] frames = new GameObject[64];
        readonly GameObject[] spawnMarks = new GameObject[64];
        GameObject hoverFrame;
        readonly List<RaidArrow> arrows = new List<RaidArrow>();
        readonly List<RaidArrow> previewArrows = new List<RaidArrow>();
        readonly List<GameObject> ghosts = new List<GameObject>();
        readonly List<TextMeshPro> labels = new List<TextMeshPro>();
        MaterialPropertyBlock mpb;
        (int x, int y)? hover;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int MainColor = Shader.PropertyToID("_Color");

        // ------------------------------------------------------------------ kurulum
        public void Build(Baskin.BState B)
        {
            mpb ??= new MaterialPropertyBlock();
            Clear(tilesRoot); Clear(piecesRoot); Clear(marksRoot);
            units.Clear(); objects.Clear(); rubble.Clear(); arrows.Clear(); previewArrows.Clear(); ghosts.Clear(); labels.Clear();
            for (int y = 0; y < Baskin.N; y++)
            for (int x = 0; x < Baskin.N; x++)
            {
                var c = Baskin.CellAt(B, x, y);
                var prefab = c.T == "wall" ? lib.wall : c.T == "cable" ? lib.cable : lib.floor;
                var go = Instantiate(prefab, W(x, y), Quaternion.identity, tilesRoot);
                go.name = $"Tile {x},{y}";
                var cell = go.GetComponent<RaidCell>() ?? go.AddComponent<RaidCell>();
                cell.X = x; cell.Y = y;
                int k = Key(x, y);
                tint[k] = Flat($"Tint {x},{y}", W(x, y) + Vector3.up * 0.02f, 0.94f, lib.highlight).GetComponent<Renderer>();
                tint[k].enabled = false;
                frames[k] = Frame($"Frame {x},{y}", W(x, y), lib.target, 0.86f, 0.07f);
                frames[k].SetActive(false);
                if (lib.spawnMarker != null)
                {
                    spawnMarks[k] = Instantiate(lib.spawnMarker, W(x, y), Quaternion.identity, marksRoot);
                    spawnMarks[k].SetActive(false);
                }
            }
            hoverFrame = Frame("Hover", Vector3.zero, lib.hover, 0.96f, 0.04f);
            hoverFrame.SetActive(false);
        }

        static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }

        GameObject Flat(string name, Vector3 pos, float size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(marksRoot, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(90, 0, 0));
            go.transform.localScale = new Vector3(size, size, 1);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // Kare çerçevesi: dört ince kenar (köşe rengiyle, her zaman üstte)
        GameObject Frame(string name, Vector3 pos, Color color, float size, float thick)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(marksRoot, false);
            go.transform.position = pos + Vector3.up * 0.03f;
            var m = new Mesh { name = "Frame" };
            var v = new List<Vector3>(); var t = new List<int>();
            float h = size / 2, i = h - thick;
            void Q(float x0, float z0, float x1, float z1)
            {
                int s = v.Count;
                v.Add(new Vector3(x0, 0, z0)); v.Add(new Vector3(x0, 0, z1)); v.Add(new Vector3(x1, 0, z1)); v.Add(new Vector3(x1, 0, z0));
                t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            Q(-h, h - thick, h, h); Q(-h, -h, h, -h + thick); Q(-h, -i, -i, i); Q(i, -i, h, i);
            m.SetVertices(v); m.SetTriangles(t, 0); m.SetColors(Enumerable.Repeat(color, v.Count).ToList());
            go.GetComponent<MeshFilter>().sharedMesh = m;
            go.GetComponent<MeshRenderer>().sharedMaterial = lib.overlay;
            return go;
        }

        // ------------------------------------------------------------------ eşitleme
        /// <summary>Görseli duruma anında eşitler (canlandırma yok).</summary>
        public void Sync(Baskin.BState B, Marks m)
        {
            // Dolaplar ve enkaz
            for (int y = 0; y < Baskin.N; y++)
            for (int x = 0; x < Baskin.N; x++)
            {
                int k = Key(x, y);
                var o = Baskin.CellAt(B, x, y).O;
                objects.TryGetValue(k, out var piece);
                if (o != null)
                {
                    if (piece == null || piece.Kind != o.K)
                    {
                        if (piece != null) Destroy(piece.gameObject);
                        piece = Instantiate(lib.Object(o.K), W(x, y), Quaternion.identity, piecesRoot);
                        piece.Kind = o.K; piece.X = x; piece.Y = y; piece.name = $"{o.K} {x},{y}";
                        objects[k] = piece;
                    }
                    piece.SetHp(o.Hp, o.Max);
                    if (rubble.TryGetValue(k, out var rb)) { Destroy(rb); rubble.Remove(k); }
                }
                else if (piece != null)
                {
                    Destroy(piece.gameObject); objects.Remove(k);
                    if (lib.rubble != null && !rubble.ContainsKey(k)) rubble[k] = Instantiate(lib.rubble, W(x, y), Quaternion.Euler(0, (x * 37 + y * 53) % 360, 0), piecesRoot);
                }
            }
            // Birimler
            var alive = new HashSet<int>();
            foreach (var u in B.Units)
            {
                if (u.Dead) continue;
                alive.Add(u.Id);
                if (!units.TryGetValue(u.Id, out var p) || p == null)
                {
                    p = Instantiate(lib.Unit(u.Type), W(u.X, u.Y), Quaternion.Euler(0, u.Side == 'p' ? 45 : -135, 0), piecesRoot);
                    p.Kind = u.Type; p.name = $"{u.Type} #{u.Id}";
                    units[u.Id] = p;
                }
                p.X = u.X; p.Y = u.Y;
                p.transform.position = W(u.X, u.Y);
                p.Body.localScale = Vector3.one;
                p.SetHp(u.Hp, u.Max);
                p.SetSelected(u.Id == m.Selected);
                p.SetSpent(m.Spent.Contains(u.Id));
                if (m.Badges.TryGetValue(u.Id, out var b)) p.SetBadges(b.order, b.flag, b.flagColor);
                else p.SetBadges(null, null, default);
            }
            foreach (var id in units.Keys.Where(id => !alive.Contains(id)).ToList()) { if (units[id] != null) Destroy(units[id].gameObject); units.Remove(id); }

            // Karo vurguları
            for (int k = 0; k < 64; k++)
            {
                Color c = default; bool on = false;
                if (m.Moves.Contains(k)) { c = lib.move; on = true; }
                if (m.Danger.TryGetValue(k, out var sure)) { c = sure ? lib.danger : lib.uncertain; on = true; }
                else if (m.Heal.Contains(k)) { c = lib.heal; on = true; }
                tint[k].enabled = on;
                if (on) SetColor(tint[k], c);
                frames[k].SetActive(m.Targets.Contains(k) && !m.Moves.Contains(k));
                if (spawnMarks[k] != null) spawnMarks[k].SetActive(m.Spawns.Contains(k));
            }
            // Niyet okları
            for (int i = 0; i < m.Arrows.Count; i++)
            {
                if (i == arrows.Count) arrows.Add(NewArrow("Intent"));
                var a = m.Arrows[i];
                arrows[i].gameObject.SetActive(true);
                arrows[i].Set(W(a.fx, a.fy) + Vector3.up * 0.06f, W(a.tx, a.ty) + Vector3.up * 0.06f, a.color, a.dashed, 0.11f);
            }
            for (int i = m.Arrows.Count; i < arrows.Count; i++) arrows[i].gameObject.SetActive(false);
        }

        void SetColor(Renderer r, Color c)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(MainColor, c); mpb.SetColor(BaseColor, c);
            r.SetPropertyBlock(mpb);
        }

        RaidArrow NewArrow(string name)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(RaidArrow));
            go.transform.SetParent(marksRoot, false);
            go.GetComponent<MeshRenderer>().sharedMaterial = lib.overlay;
            return go.GetComponent<RaidArrow>();
        }

        // ------------------------------------------------------------------ önizleme
        public void SetPreview(IList<(float x, float y, string text, Color color)> texts, IList<(int fx, int fy, int tx, int ty)> moves)
        {
            ClearPreview();
            for (int i = 0; i < moves.Count; i++)
            {
                var mv = moves[i];
                if (i == previewArrows.Count) previewArrows.Add(NewArrow("Preview"));
                previewArrows[i].gameObject.SetActive(true);
                previewArrows[i].Set(W(mv.fx, mv.fy) + Vector3.up * 0.5f, W(mv.tx, mv.ty) + Vector3.up * 0.5f, lib.ink, false, 0.09f);
                if (i == ghosts.Count) ghosts.Add(Frame("Ghost", Vector3.zero, lib.ink, 0.7f, 0.05f));
                ghosts[i].transform.position = W(mv.tx, mv.ty) + Vector3.up * 0.04f;
                ghosts[i].SetActive(true);
            }
            for (int i = 0; i < texts.Count; i++)
            {
                var (x, y, text, color) = texts[i];
                var l = Label(i);
                l.transform.position = W(x, y) + Vector3.up * 1.9f;
                l.text = text; l.color = color; l.fontSize = 6;
                l.gameObject.SetActive(true);
            }
        }

        public void ClearPreview()
        {
            foreach (var a in previewArrows) a.gameObject.SetActive(false);
            foreach (var g in ghosts) g.SetActive(false);
            foreach (var l in labels) l.gameObject.SetActive(false);
        }

        TextMeshPro Label(int i)
        {
            while (labels.Count <= i)
            {
                var go = new GameObject("Label", typeof(TextMeshPro), typeof(Billboard));
                go.transform.SetParent(marksRoot, false);
                var t = go.GetComponent<TextMeshPro>();
                t.font = lib.font; t.fontSharedMaterial = lib.labelMaterial; t.fontStyle = FontStyles.Bold;
                t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap;
                t.rectTransform.sizeDelta = new Vector2(6, 1.2f);
                t.sortingOrder = 50;
                labels.Add(t);
            }
            return labels[i];
        }

        // ------------------------------------------------------------------ girdi
        void Update()
        {
            if (cam == null || Mouse.current == null) return;
            (int x, int y)? h = null;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (InputEnabled && !Busy && !overUi && Physics.Raycast(cam.ScreenPointToRay(Mouse.current.position.ReadValue()), out var hit, 200f))
            {
                var cell = hit.collider.GetComponentInParent<RaidCell>();
                if (cell != null) h = (cell.X, cell.Y);
                else { var p = hit.collider.GetComponentInParent<RaidPiece>(); if (p != null) h = (p.X, p.Y); }
            }
            if (h != hover)
            {
                hover = h;
                hoverFrame.SetActive(h != null);
                if (h != null) hoverFrame.transform.position = W(h.Value.x, h.Value.y) + Vector3.up * 0.035f;
                HoverChanged?.Invoke(h);
            }
            if (h != null && Mouse.current.leftButton.wasPressedThisFrame) TileClicked?.Invoke(h.Value.x, h.Value.y);
        }

        /// <summary>Kare tıklanmış gibi davranır (test ve klavye için).</summary>
        public void Click(int x, int y) => TileClicked?.Invoke(x, y);
        public void Hover(int x, int y) { hover = (x, y); HoverChanged?.Invoke(hover); }

        // ------------------------------------------------------------------ canlandırma
        /// <summary>Birimi kayarak (küçük bir sıçramayla) yeni karesine taşır.</summary>
        public IEnumerator AnimateMove(int unitId, int x, int y)
        {
            if (!units.TryGetValue(unitId, out var p)) yield break;
            Busy = true;
            var a = p.transform.position; var b = W(x, y);
            Sfx.Play(Cue.RaidStep, 1, Pan(b));
            float d = Mathf.Max(0.18f, Vector3.Distance(a, b) * 0.07f);
            for (float t = 0; t < 1; t += Time.deltaTime / d)
            {
                p.transform.position = Vector3.Lerp(a, b, Ease(t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.35f;
                yield return null;
            }
            p.transform.position = b; p.X = x; p.Y = y;
            Busy = false;
        }

        /// <summary>Bir eylemin ya da düşman turunun efektlerini sırayla oynatır. Görsel, çağrıdan önceki durumdadır; sonra Sync gerekir.</summary>
        public IEnumerator Animate(Baskin.BState after, List<Baskin.Fx> fx, bool enemyTurn)
        {
            Busy = true;
            hoverFrame.SetActive(false);
            ClearPreview();
            int popups = 0;
            foreach (var f in fx)
            {
                switch (f.K)
                {
                    case "attack":
                        if (units.TryGetValue(f.Unit, out var atk))
                        {
                            var u = after.Units.FirstOrDefault(z => z.Id == f.Unit);
                            bool drone = u != null && u.Type == "dron";
                            Sfx.Play(drone ? Cue.RaidDroneShot : Cue.RaidMelee, 1, Pan(atk.transform.position));
                            yield return drone ? Shot(atk, f.X, f.Y) : Lunge(atk, f.X, f.Y);
                        }
                        break;
                    case "dmg":
                    {
                        RaidPiece p = f.Unit > 0 && units.TryGetValue(f.Unit, out var up) ? up : objects.TryGetValue(Key(f.X, f.Y), out var op) ? op : null;
                        p?.Flash(lib.threat);
                        Sfx.Play(f.Unit > 0 ? Cue.RaidHit : Cue.RaidHitObject, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Popup(W(f.X, f.Y), "−" + f.N, lib.threat, popups++ * 0.04f));
                        yield return Wait(enemyTurn ? 0.5f : 0.35f);
                        break;
                    }
                    case "armor":
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("zırh"), lib.ink, 0));
                        Sfx.Play(Cue.RaidArmor, 1, Pan(W(f.X, f.Y)));
                        break;
                    case "heal":
                        if (objects.TryGetValue(Key(f.X, f.Y), out var hp)) hp.Flash(lib.good);
                        Sfx.Play(Cue.RaidRepair, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Popup(W(f.X, f.Y), "+1", lib.good, 0));
                        yield return Wait(0.6f);
                        break;
                    case "push":
                    case "pull":
                        if (units.TryGetValue(f.Unit, out var pu))
                        {
                            var u = after.Units.FirstOrDefault(z => z.Id == f.Unit);
                            if (u != null) { Sfx.Play(Cue.RaidPush, 1, Pan(W(u.X, u.Y))); yield return Slide(pu, W(u.X, u.Y), 0.18f); }
                        }
                        break;
                    case "hook":
                        yield return Burst(W(f.X, f.Y), lib.ink, 0.4f, 0.2f);
                        break;
                    case "emp":
                        Sfx.Play(Cue.RaidEmpBlast, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Burst(W(f.X, f.Y), new Color(0.55f, 0.85f, 1f, 0.9f), 1.6f, 0.35f));
                        yield return Wait(0.15f);
                        break;
                    case "die":
                        if (units.TryGetValue(f.Unit, out var dp))
                        {
                            var du = after.Units.FirstOrDefault(z => z.Id == f.Unit);
                            Sfx.Play(du != null && du.Side == 'p' ? Cue.RaidUnitDown : Cue.RaidRobotDown, 1, Pan(dp.transform.position));
                            yield return Sink(dp);
                        }
                        break;
                    case "break":
                        Sfx.Play(f.Obj == "civ" ? Cue.RaidCivilianBreak : f.Obj == "cool" ? Cue.RaidExplosion : Baskin.IsTarget(f.Obj) ? Cue.RaidCoreBreak : Cue.RaidBreak, 1, Pan(W(f.X, f.Y)));
                        if (objects.TryGetValue(Key(f.X, f.Y), out var bp)) yield return Collapse(bp);
                        break;
                    case "spawn":
                        Sfx.Play(Cue.RaidSpawn, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Burst(W(f.X, f.Y), lib.threat, 1.0f, 0.3f));
                        yield return Wait(0.2f);
                        break;
                }
            }
            yield return Wait(0.15f);
            Busy = false;
        }

        public float Pan(Vector3 world) => Sfx.PanOf(cam, world);

        IEnumerator Wait(float s) { yield return new WaitForSeconds(s * stepTime / 0.32f); }
        static float Ease(float t) => 1 - (1 - t) * (1 - t);

        IEnumerator Lunge(RaidPiece p, int x, int y)
        {
            var a = p.transform.position;
            var dir = W(x, y) - a; dir.y = 0;
            var b = a + Vector3.ClampMagnitude(dir, 0.45f);
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 0.5f)) { p.transform.position = Vector3.Lerp(a, b, Mathf.Sin(t * Mathf.PI)); yield return null; }
            p.transform.position = a;
        }

        IEnumerator Shot(RaidPiece p, int x, int y)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(marksRoot, false);
            go.transform.localScale = Vector3.one * 0.16f;
            go.GetComponent<Renderer>().sharedMaterial = lib.overlay;
            var mf = go.GetComponent<MeshFilter>();
            mf.mesh.SetColors(Enumerable.Repeat(lib.threat, mf.mesh.vertexCount).ToList());
            var a = p.Body.position; var b = W(x, y) + Vector3.up * 0.5f;
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 0.6f)) { go.transform.position = Vector3.Lerp(a, b, t); yield return null; }
            Destroy(go);
        }

        IEnumerator Slide(RaidPiece p, Vector3 to, float time)
        {
            var a = p.transform.position;
            for (float t = 0; t < 1; t += Time.deltaTime / time) { p.transform.position = Vector3.Lerp(a, to, Ease(t)); yield return null; }
            p.transform.position = to;
        }

        IEnumerator Sink(RaidPiece p)
        {
            var a = p.Body.localPosition; var s = p.Body.localScale;
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 1.2f))
            {
                p.Body.localPosition = a + Vector3.down * (t * 0.6f);
                p.Body.localScale = s * (1 - t * 0.7f);
                yield return null;
            }
            p.gameObject.SetActive(false);
        }

        IEnumerator Collapse(RaidPiece p)
        {
            var s = p.Body.localScale;
            p.Flash(Color.white, 0.4f);
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 1.2f)) { p.Body.localScale = new Vector3(s.x * (1 + t * 0.15f), s.y * (1 - t * 0.85f), s.z * (1 + t * 0.15f)); yield return null; }
        }

        IEnumerator Burst(Vector3 at, Color color, float size, float time)
        {
            var ring = Frame("Burst", at, color, 0.3f, 0.06f);
            for (float t = 0; t < 1; t += Time.deltaTime / time)
            {
                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, size, Ease(t)) / 0.3f;
                yield return null;
            }
            Destroy(ring.GetComponent<MeshFilter>().sharedMesh);
            Destroy(ring);
        }

        IEnumerator Popup(Vector3 at, string text, Color color, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            var go = new GameObject("Popup", typeof(TextMeshPro), typeof(Billboard));
            go.transform.SetParent(marksRoot, false);
            var t = go.GetComponent<TextMeshPro>();
            t.font = lib.font; t.fontSharedMaterial = lib.labelMaterial; t.fontStyle = FontStyles.Bold; t.fontSize = 7;
            t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap; t.sortingOrder = 60;
            t.text = text; t.color = color;
            var a = at + Vector3.up * 1.5f;
            for (float k = 0; k < 1; k += Time.deltaTime / 0.9f)
            {
                go.transform.position = a + Vector3.up * (k * 0.8f);
                t.alpha = 1 - Mathf.Max(0, k - 0.6f) / 0.4f;
                yield return null;
            }
            Destroy(go);
        }
    }
}
