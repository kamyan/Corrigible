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
        [Header("Masa maketi / dünya etiketleri")]
        [SerializeField] Material ghostMaterial;
        [SerializeField] TMP_FontAsset handFont;
        [SerializeField] Material handLabelMaterial;
        [Tooltip("Dünya kâğıtları için TMP Overlay shader'ı; player build'de shader referansını korur.")]
        [SerializeField] Shader worldLabelShader;
        [SerializeField] Color paperColor = new Color(0.984f, 0.969f, 0.925f);
        [SerializeField] Color verifiedColor = new Color(0.102f, 0.278f, 0.537f);
        [SerializeField] Color graphiteColor = new Color(0.118f, 0.106f, 0.086f);
        [SerializeField] Color highlighterColor = new Color(0.961f, 0.863f, 0.388f, 0.95f);
        [Tooltip("Düşman turunda bir adımın süresi (saniye).")]
        [SerializeField, Min(0.05f)] float stepTime = 0.32f;

        public event Action<int, int> TileClicked;
        public event Action<(int x, int y)?> HoverChanged;
        public event Action<Baskin.Fx, int, int> EffectPlaying;
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
        readonly RaidArrow[] moveFrames = new RaidArrow[64], dangerFrames = new RaidArrow[64], repairFrames = new RaidArrow[64];
        readonly RaidArrow[] moveHatches = new RaidArrow[64], dangerHatches = new RaidArrow[64], wakeRings = new RaidArrow[64];
        readonly Dictionary<int, RaidWorldLabel> hpLabels = new Dictionary<int, RaidWorldLabel>();
        readonly Dictionary<int, RaidWorldLabel> orderLabels = new Dictionary<int, RaidWorldLabel>(), flagLabels = new Dictionary<int, RaidWorldLabel>(), stateLabels = new Dictionary<int, RaidWorldLabel>();
        readonly Dictionary<int, RaidWorldLabel> questionLabels = new Dictionary<int, RaidWorldLabel>();
        readonly Dictionary<int, GameObject> flagPins = new Dictionary<int, GameObject>();
        readonly Dictionary<int, RaidArrow> downMarks = new Dictionary<int, RaidArrow>();
        readonly List<RaidWorldLabel> paperLabels = new List<RaidWorldLabel>();
        readonly List<RaidWorldLabel[]> labelGroups = new List<RaidWorldLabel[]>();
        readonly List<Rect> occupiedLabels = new List<Rect>();
        readonly List<Mesh> effectMeshes = new List<Mesh>();
        readonly Dictionary<int, Baskin.IntentView> shownIntents = new Dictionary<int, Baskin.IntentView>();
        readonly Dictionary<int, RaidArrow> intentArrowByUnit = new Dictionary<int, RaidArrow>();
        RaidArrow scanOutline, selectionFrame;
        RaidArrow activeIntentHighlight;
        RaidWorldLabel scanLabel;
        GameObject hoverFrame;
        readonly List<RaidArrow> arrows = new List<RaidArrow>();
        readonly List<RaidArrow> previewArrows = new List<RaidArrow>();
        readonly List<GameObject> ghosts = new List<GameObject>();
        MaterialPropertyBlock mpb;
        (int x, int y)? hover;
        int buildVersion;
        int layoutWidth, layoutHeight;
        Vector3 layoutCameraPosition;
        Quaternion layoutCameraRotation;
        float layoutCameraSize;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int MainColor = Shader.PropertyToID("_Color");
        float PixelWorld => cam != null ? Mathf.Max(0.001f, 2 * cam.orthographicSize / Mathf.Max(1, cam.pixelHeight) * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f)) : 0.0113f;
        static Color Alpha(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);
        static readonly Vector2[] LabelDirections =
        {
            Vector2.up, Vector2.right, Vector2.left, Vector2.down,
            new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1)
        };

        // ------------------------------------------------------------------ kurulum
        public void Build(Baskin.BState B)
        {
            buildVersion++;
            StopAllCoroutines(); Busy = false; hover = null;
            ReleaseEffectMeshes();
            mpb ??= new MaterialPropertyBlock();
            Clear(tilesRoot); Clear(piecesRoot); Clear(marksRoot);
            units.Clear(); objects.Clear(); rubble.Clear(); arrows.Clear(); previewArrows.Clear(); ghosts.Clear(); labelGroups.Clear();
            hpLabels.Clear(); orderLabels.Clear(); flagLabels.Clear(); questionLabels.Clear(); stateLabels.Clear(); paperLabels.Clear(); shownIntents.Clear(); intentArrowByUnit.Clear(); flagPins.Clear(); downMarks.Clear();
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
                frames[k] = Frame($"Frame {x},{y}", W(x, y), lib.target, 0.86f, PixelWorld * 2.4f);
                frames[k].SetActive(false);
                moveFrames[k] = Mark("Move edge", W(x, y), 0.027f);
                moveFrames[k].SetFrame(0.86f, PixelWorld * 2, Alpha(lib.move, 0.85f));
                moveHatches[k] = Mark("Move hatch", W(x, y), 0.024f);
                moveHatches[k].SetHatch(0.86f, Alpha(lib.move, 0.34f), PixelWorld * 1.6f, true);
                dangerFrames[k] = Mark("Danger edge", W(x, y), 0.031f);
                dangerHatches[k] = Mark("Danger hatch", W(x, y), 0.028f);
                repairFrames[k] = Mark("Repair edge", W(x, y), 0.034f);
                repairFrames[k].SetFrame(0.88f, PixelWorld * 2, Alpha(lib.threat, 0.85f), true);
                wakeRings[k] = Mark("Wake pin ring", W(x, y), 0.04f);
                wakeRings[k].SetCircle(0.33f, lib.threat, PixelWorld * 2.4f, false, true);
                if (lib.spawnMarker != null)
                {
                    spawnMarks[k] = Instantiate(lib.spawnMarker, W(x, y), Quaternion.identity, marksRoot);
                    spawnMarks[k].SetActive(false);
                }
            }
            hoverFrame = Frame("Hover", Vector3.zero, lib.hover, 0.94f, PixelWorld * 1.8f);
            hoverFrame.SetActive(false);
            selectionFrame = Mark("Selected double frame", Vector3.zero, 0.04f);
            selectionFrame.SetFrame(0.90f, PixelWorld * 2, verifiedColor, false, true);
            scanOutline = Mark("Analyst scan boundary", Vector3.zero, 0.042f);
            scanLabel = NewPaperLabel("Scan label", 12);
            scanLabel.gameObject.SetActive(false);
            activeIntentHighlight = NewArrow("Current intent highlighter");
            activeIntentHighlight.gameObject.SetActive(false);
        }

        static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var go = t.GetChild(i).gameObject;
                go.SetActive(false); Destroy(go);
            }
        }

        void ReleaseEffectMeshes()
        {
            foreach (var mesh in effectMeshes) if (mesh != null) Destroy(mesh);
            effectMeshes.Clear();
        }

        void OnDisable() { buildVersion++; StopAllCoroutines(); Busy = false; }
        void OnDestroy() { ReleaseEffectMeshes(); }

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
            var mark = Mark(name, pos, 0.03f);
            mark.SetFrame(size, thick, color);
            mark.gameObject.SetActive(true);
            return mark.gameObject;
        }

        RaidArrow Mark(string name, Vector3 position, float height)
        {
            var mark = NewArrow(name);
            mark.transform.position = position + Vector3.up * height;
            mark.gameObject.SetActive(false);
            return mark;
        }

        RaidWorldLabel NewPaperLabel(string name, float pixels, bool hand = false)
        {
            var go = new GameObject(name, typeof(RaidWorldLabel));
            go.transform.SetParent(marksRoot, false);
            var label = go.GetComponent<RaidWorldLabel>();
            label.Configure(cam, hand && handFont != null ? handFont : lib.font, hand && handFont != null ? handLabelMaterial : lib.labelMaterial, lib.overlay, paperColor, pixels, hand ? 15 : 11, worldLabelShader);
            return label;
        }

        RaidWorldLabel PieceLabel(Dictionary<int, RaidWorldLabel> pool, int id, string name, float pixels = 12, bool hand = false)
        {
            if (!pool.TryGetValue(id, out var label)) { label = NewPaperLabel(name, pixels, hand); pool[id] = label; }
            return label;
        }

        // ------------------------------------------------------------------ eşitleme
        /// <summary>Görseli duruma anında eşitler (canlandırma yok).</summary>
        public void Sync(Baskin.BState B, Marks m)
        {
            labelGroups.Clear();
            foreach (var label in hpLabels.Values) label.gameObject.SetActive(false);
            foreach (var label in orderLabels.Values) label.gameObject.SetActive(false);
            foreach (var label in flagLabels.Values) label.gameObject.SetActive(false);
            foreach (var label in questionLabels.Values) label.gameObject.SetActive(false);
            foreach (var label in stateLabels.Values) label.gameObject.SetActive(false);
            foreach (var pin in flagPins.Values) pin.SetActive(false);
            foreach (var mark in downMarks.Values) mark.gameObject.SetActive(false);
            shownIntents.Clear();
            intentArrowByUnit.Clear();
            activeIntentHighlight.gameObject.SetActive(false);
            // Bu görünüş gerçek Intent.Dir alanını okumaz; yalan olabilen yön Core'un algı API'sinden gelir.
            foreach (var intent in Baskin.Intents(B)) shownIntents[intent.Unit] = intent;
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
                    piece.SetWorldLabelsVisible(false);
                    var tag = PieceLabel(hpLabels, -k - 1, "Object HP");
                    tag.SetAnchor(W(x, y) + Vector3.up * (piece.VisualHeight + 0.12f));
                    var ink = Baskin.IsTarget(o.K) || o.K == "spawner" ? lib.threat : graphiteColor;
                    tag.Show(Loc.F("{0}/{1}", o.Hp, o.Max), ink, ink);
                    labelGroups.Add(new[] { tag });
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
                // Düşen ekip kartları yerinde kalır; Core.UnitAt onları yürüyüş ve atıştan zaten çıkarır.
                if (u.Dead && u.Side != 'p') continue;
                alive.Add(u.Id);
                if (!units.TryGetValue(u.Id, out var p) || p == null)
                {
                    p = Instantiate(lib.Unit(u.Type), W(u.X, u.Y), Quaternion.Euler(0, u.Side == 'p' ? 45 : -135, 0), piecesRoot);
                    p.Kind = u.Type; p.name = $"{u.Type} #{u.Id}";
                    units[u.Id] = p;
                }
                p.X = u.X; p.Y = u.Y;
                p.gameObject.SetActive(true);
                p.transform.position = W(u.X, u.Y);
                p.SetHp(u.Hp, u.Max);
                p.SetSelected(false);
                p.SetSpent(m.Spent.Contains(u.Id));
                p.SetDown(u.Dead);
                p.SetWorldLabelsVisible(false);
                string letter = u.Type switch { "tek" => "T", "emp" => "E", "ana" => "A", "dron" => "D", _ => "B" };
                var hpTag = PieceLabel(hpLabels, u.Id, "Unit HP");
                hpTag.SetAnchor(u.Side == 'p' && !u.Dead ? W(u.X, u.Y) + Vector3.up * (p.VisualHeight + 0.12f) : W(u.X, u.Y) + Vector3.up * 0.18f - cam.transform.up * 0.26f);
                var hpInk = u.Side == 'e' || u.Dead ? lib.threat : graphiteColor;
                hpTag.Show(Loc.F("{0} {1}/{2}", letter, u.Dead ? 0 : u.Hp, u.Max), hpInk, hpInk);
                if (u.Dead || m.Spent.Contains(u.Id))
                {
                    var state = PieceLabel(stateLabels, u.Id, "Unit state");
                    state.SetAnchor(hpTag.transform.position - cam.transform.up * 0.30f);
                    state.Show(u.Dead ? Loc.T("DÜŞTÜ") : Loc.T("TUR BİTTİ"), hpInk, hpInk);
                    labelGroups.Add(new[] { hpTag, state });
                }
                else labelGroups.Add(new[] { hpTag });
                if (u.Dead)
                {
                    if (!downMarks.TryGetValue(u.Id, out var mark)) { mark = Mark("Down graphite cross", W(u.X, u.Y), 0.10f); downMarks[u.Id] = mark; }
                    mark.transform.position = W(u.X, u.Y) + Vector3.up * 0.11f;
                    mark.SetLines(new List<(Vector3, Vector3)> { (new Vector3(-0.25f, 0, -0.25f), new Vector3(0.25f, 0, 0.25f)), (new Vector3(-0.25f, 0, 0.25f), new Vector3(0.25f, 0, -0.25f)) }, lib.threat, PixelWorld * 2.8f);
                    mark.gameObject.SetActive(true);
                }
                if (shownIntents.TryGetValue(u.Id, out var intent))
                {
                    var order = PieceLabel(orderLabels, u.Id, "Order flag", 14);
                    order.SetAnchor(W(u.X, u.Y) + Vector3.up * (p.VisualHeight + 0.35f) + cam.transform.right * 0.26f);
                    string kind = intent.Kind == "repair" ? Loc.T("ONARIM") : intent.Kind == "shoot" ? Loc.T("ATEŞ") : Loc.T("VURUŞ");
                    Color border = intent.Uncertain ? lib.uncertain : intent.IsScanned ? verifiedColor : lib.threat;
                    border.a = 1;
                    order.SetOpacity(1);
                    order.SetPaperColor(paperColor);
                    order.Show(Loc.F("{0} {1}", intent.Order, kind), lib.threat, border, intent.Uncertain);
                    if (intent.Uncertain || intent.IsScanned)
                    {
                        var flag = PieceLabel(intent.Uncertain ? questionLabels : flagLabels, u.Id, "Trust flag", intent.Uncertain ? 18 : 12, intent.Uncertain);
                        flag.SetAnchor(order.transform.position + cam.transform.up * 0.32f);
                        flag.Show(intent.Uncertain ? "?" : Loc.T("✓ TARANDI"), border, border, intent.Uncertain);
                        labelGroups.Add(new[] { order, flag });
                    }
                    else labelGroups.Add(new[] { order });
                }
            }
            foreach (var id in units.Keys.Where(id => !alive.Contains(id)).ToList()) { if (units[id] != null) Destroy(units[id].gameObject); units.Remove(id); }

            // Karo vurguları
            for (int k = 0; k < 64; k++)
            {
                Color c = default; bool on = false;
                if (m.Moves.Contains(k)) { c = lib.move; on = true; }
                if (m.Danger.TryGetValue(k, out var sure)) { c = Alpha(lib.threat, sure ? 0.12f : 0.08f); on = true; }
                else if (m.Heal.Contains(k)) { c = lib.heal; on = true; }
                else if (m.Targets.Contains(k) && !on) { c = Alpha(lib.target, 0.08f); on = true; }
                tint[k].enabled = on;
                if (on) SetColor(tint[k], c);
                frames[k].SetActive(m.Targets.Contains(k));
                moveFrames[k].gameObject.SetActive(m.Moves.Contains(k));
                moveHatches[k].gameObject.SetActive(m.Moves.Contains(k) && !m.Danger.ContainsKey(k));
                bool hit = m.Danger.TryGetValue(k, out bool certain);
                dangerFrames[k].gameObject.SetActive(hit);
                dangerHatches[k].gameObject.SetActive(hit);
                if (hit)
                {
                    dangerFrames[k].SetFrame(0.88f, PixelWorld * 2.4f, Alpha(certain ? lib.threat : lib.uncertain, 0.92f), !certain);
                    dangerHatches[k].SetHatch(0.86f, Alpha(lib.threat, certain ? 0.46f : 0.30f), PixelWorld * 1.8f, false);
                }
                repairFrames[k].gameObject.SetActive(m.Heal.Contains(k));
                wakeRings[k].gameObject.SetActive(m.Spawns.Contains(k));
                if (spawnMarks[k] != null) spawnMarks[k].SetActive(m.Spawns.Contains(k));
            }
            var selected = B.Units.FirstOrDefault(u => u.Id == m.Selected && !u.Dead);
            selectionFrame.gameObject.SetActive(selected != null);
            if (selected != null) selectionFrame.transform.position = W(selected.X, selected.Y) + Vector3.up * 0.045f;
            DrawScan(B, selected != null && selected.Type == "ana");
            // Niyet okları
            for (int i = 0; i < m.Arrows.Count; i++)
            {
                if (i == arrows.Count) arrows.Add(NewArrow("Intent"));
                var a = m.Arrows[i];
                arrows[i].gameObject.SetActive(true);
                var intent = shownIntents.Values.FirstOrDefault(j => j.FromX == a.fx && j.FromY == a.fy && j.TargetX == a.tx && j.TargetY == a.ty);
                if (intent != null) intentArrowByUnit[intent.Unit] = arrows[i];
                var start = W(a.fx, a.fy) + Vector3.up * 0.30f;
                var end = W(a.tx, a.ty) + Vector3.up * 0.16f;
                var direction = (end - start).normalized;
                start += direction * 0.24f; end -= direction * 0.30f;
                bool curved = intent != null && intent.Kind != "shoot";
                arrows[i].Set(start, end, a.dashed ? Alpha(lib.threat, 0.85f) : lib.threat, a.dashed, PixelWorld * (curved ? 4.2f : 3), curved ? 0.30f : 0);
            }
            for (int i = m.Arrows.Count; i < arrows.Count; i++) arrows[i].gameObject.SetActive(false);
            LayoutLabels();
        }

        void FlagPin(int id, Vector3 from, Vector3 to)
        {
            if (!flagPins.TryGetValue(id, out var pin))
            {
                pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pin.name = "Order flag needle"; Destroy(pin.GetComponent<Collider>());
                pin.transform.SetParent(marksRoot, false);
                pin.GetComponent<Renderer>().sharedMaterial = lib.overlay;
                SetColor(pin.GetComponent<Renderer>(), graphiteColor);
                flagPins[id] = pin;
            }
            pin.SetActive(true);
            pin.transform.position = (from + to) * 0.5f;
            pin.transform.rotation = Quaternion.FromToRotation(Vector3.up, to - from);
            pin.transform.localScale = new Vector3(PixelWorld * 2, Vector3.Distance(from, to) * 0.5f, PixelWorld * 2);
        }

        void DrawScan(Baskin.BState B, bool selectedAnalyst)
        {
            var analysts = Baskin.Alive(B, 'p').Where(u => u.Type == "ana").ToList();
            bool show = analysts.Count > 0 && (B.Deceptive || selectedAnalyst) && B.Phase == "player";
            scanOutline.gameObject.SetActive(show); scanLabel.gameObject.SetActive(show);
            if (!show) return;
            bool Inside(int x, int y) => x >= 0 && x < 8 && y >= 0 && y < 8 && analysts.Any(a => Mathf.Abs(x - a.X) + Mathf.Abs(y - a.Y) <= Baskin.ScanRadius);
            var lines = new List<(Vector3, Vector3)>();
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) if (Inside(x, y))
            {
                var p = W(x, y);
                if (!Inside(x, y - 1)) lines.Add((p + new Vector3(-0.5f, 0, 0.5f), p + new Vector3(0.5f, 0, 0.5f)));
                if (!Inside(x + 1, y)) lines.Add((p + new Vector3(0.5f, 0, 0.5f), p + new Vector3(0.5f, 0, -0.5f)));
                if (!Inside(x, y + 1)) lines.Add((p + new Vector3(0.5f, 0, -0.5f), p + new Vector3(-0.5f, 0, -0.5f)));
                if (!Inside(x - 1, y)) lines.Add((p + new Vector3(-0.5f, 0, -0.5f), p + new Vector3(-0.5f, 0, 0.5f)));
            }
            scanOutline.SetLines(lines, Alpha(verifiedColor, 0.9f), PixelWorld * 2.2f, true);
            scanLabel.SetAnchor(W(analysts[0].X, analysts[0].Y) - cam.transform.up * 0.65f);
            scanLabel.Show(Loc.T("TARAMA · 3 KARE"), verifiedColor, verifiedColor);
            labelGroups.Add(new[] { scanLabel });
        }

        // Yakın etiketler birlikte taşınır; bayrak ile ?/TARANDI ilişkisi dil ve çözünürlükte korunur.
        void LayoutLabels()
        {
            if (cam == null) return;
            occupiedLabels.Clear();
            void Place(IEnumerable<RaidWorldLabel> group)
            {
                bool hasLabel = false;
                Rect bounds = default;
                foreach (var label in group)
                {
                    if (label == null || !label.gameObject.activeSelf) continue;
                    label.ResetAnchor();
                    var rect = label.ScreenBounds;
                    bounds = hasLabel ? Rect.MinMaxRect(Mathf.Min(bounds.xMin, rect.xMin), Mathf.Min(bounds.yMin, rect.yMin), Mathf.Max(bounds.xMax, rect.xMax), Mathf.Max(bounds.yMax, rect.yMax)) : rect;
                    hasLabel = true;
                }
                if (!hasLabel) return;
                bounds = Rect.MinMaxRect(bounds.xMin - 2, bounds.yMin - 2, bounds.xMax + 2, bounds.yMax + 2);
                float Overlap(Vector2 offset)
                {
                    var candidate = new Rect(bounds.position + offset, bounds.size);
                    float area = 0;
                    foreach (var rect in occupiedLabels)
                        area += Mathf.Max(0, Mathf.Min(candidate.xMax, rect.xMax) - Mathf.Max(candidate.xMin, rect.xMin)) * Mathf.Max(0, Mathf.Min(candidate.yMax, rect.yMax) - Mathf.Max(candidate.yMin, rect.yMin));
                    return area;
                }
                Vector2 best = Vector2.zero;
                float score = Overlap(best);
                for (float radius = 8; radius <= 64 && score > 0; radius += 8)
                    foreach (var direction in LabelDirections)
                    {
                        var offset = direction * radius;
                        float candidate = Overlap(offset);
                        if (candidate >= score) continue;
                        best = offset; score = candidate;
                        if (score <= 0) break;
                    }
                foreach (var label in group)
                    if (label != null && label.gameObject.activeSelf) label.OffsetPixels(best);
                occupiedLabels.Add(new Rect(bounds.position + best, bounds.size));
            }
            foreach (var group in labelGroups) Place(group);
            foreach (var preview in paperLabels) if (preview.gameObject.activeSelf) Place(new[] { preview });
            foreach (var pair in orderLabels)
            {
                if (!pair.Value.gameObject.activeSelf || !units.TryGetValue(pair.Key, out var actor)) continue;
                if (!actor.gameObject.activeSelf) { if (flagPins.TryGetValue(pair.Key, out var pin)) pin.SetActive(false); continue; }
                FlagPin(pair.Key, actor.transform.position + Vector3.up * actor.VisualHeight, pair.Value.transform.position);
            }
            layoutWidth = Screen.width; layoutHeight = Screen.height;
            layoutCameraPosition = cam.transform.position; layoutCameraRotation = cam.transform.rotation; layoutCameraSize = cam.orthographicSize;
        }

        void LateUpdate()
        {
            if (cam != null && (layoutWidth != Screen.width || layoutHeight != Screen.height || layoutCameraPosition != cam.transform.position || layoutCameraRotation != cam.transform.rotation || !Mathf.Approximately(layoutCameraSize, cam.orthographicSize))) LayoutLabels();
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
                previewArrows[i].Set(W(mv.fx, mv.fy) + Vector3.up * 0.06f, W(mv.tx, mv.ty) + Vector3.up * 0.06f, graphiteColor, false, PixelWorld * 3);
                var source = units.Values.FirstOrDefault(p => p.X == mv.fx && p.Y == mv.fy && p.gameObject.activeSelf);
                if (i == ghosts.Count) ghosts.Add(null);
                if (ghosts[i] != null) Destroy(ghosts[i]);
                ghosts[i] = source != null ? Ghost(source) : Frame("Ghost position", Vector3.zero, graphiteColor, 0.7f, PixelWorld * 2);
                ghosts[i].transform.position = W(mv.tx, mv.ty);
                ghosts[i].SetActive(true);
            }
            for (int i = 0; i < texts.Count; i++)
            {
                var (x, y, text, color) = texts[i];
                while (paperLabels.Count <= i) paperLabels.Add(NewPaperLabel("Ability preview", 16));
                var l = paperLabels[i];
                l.SetAnchor(W(x, y) + Vector3.up * 1.25f + cam.transform.right * (i % 2 == 0 ? -0.18f : 0.18f));
                l.Show(text, color, color);
            }
            LayoutLabels();
        }

        GameObject Ghost(RaidPiece source)
        {
            var go = Instantiate(source.gameObject, marksRoot);
            go.name = "Preview ghost";
            var p = go.GetComponent<RaidPiece>();
            if (p != null) { p.SetWorldLabelsVisible(false); p.SetSelected(false); }
            foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var material = ghostMaterial != null ? ghostMaterial : lib.highlight;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<TMP_Text>() != null || renderer.GetComponent<TMP_SubMesh>() != null) { renderer.enabled = false; continue; }
                var sourceMaterials = renderer.sharedMaterials;
                var copies = Enumerable.Repeat(material, sourceMaterials.Length).ToArray();
                renderer.sharedMaterials = copies;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                for (int j = 0; j < copies.Length; j++)
                {
                    var color = sourceMaterials[j] != null && sourceMaterials[j].HasProperty(BaseColor) ? sourceMaterials[j].GetColor(BaseColor) : paperColor;
                    mpb.Clear(); mpb.SetColor(BaseColor, Alpha(color, 0.42f)); mpb.SetColor(MainColor, Alpha(color, 0.42f));
                    renderer.SetPropertyBlock(mpb, j);
                }
            }
            return go;
        }

        public void ClearPreview()
        {
            foreach (var a in previewArrows) a.gameObject.SetActive(false);
            foreach (var g in ghosts) if (g != null) g.SetActive(false);
            foreach (var l in paperLabels) l.gameObject.SetActive(false);
            LayoutLabels();
        }

        // ------------------------------------------------------------------ girdi
        void Update()
        {
            if (cam == null || hoverFrame == null || Mouse.current == null) return;
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
            int version = buildVersion;
            Busy = true;
            var a = p.transform.position; var b = W(x, y);
            Sfx.Play(Cue.RaidStep, 1, Pan(b));
            float d = Mathf.Max(0.18f, Vector3.Distance(a, b) * 0.07f);
            for (float t = 0; t < 1; t += Time.deltaTime / d)
            {
                if (version != buildVersion || p == null) yield break;
                p.transform.position = Vector3.Lerp(a, b, Ease(t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.35f;
                yield return null;
            }
            if (version != buildVersion || p == null) yield break;
            p.transform.position = b; p.X = x; p.Y = y;
            Busy = false;
        }

        /// <summary>Bir eylemin ya da düşman turunun efektlerini sırayla oynatır. Görsel, çağrıdan önceki durumdadır; sonra Sync gerekir.</summary>
        public IEnumerator Animate(Baskin.BState after, List<Baskin.Fx> fx, bool enemyTurn)
        {
            int version = buildVersion;
            Busy = true;
            hoverFrame.SetActive(false);
            ClearPreview();
            scanOutline.gameObject.SetActive(false); scanLabel.gameObject.SetActive(false);
            selectionFrame.gameObject.SetActive(false);
            if (enemyTurn) foreach (var pair in units)
                if (after.Units.Any(u => u.Id == pair.Key && u.Side == 'p' && !u.Dead)) pair.Value.SetSpent(true);
            int popups = 0;
            int fxIndex = 0;
            foreach (var f in fx)
            {
                if (version != buildVersion) yield break;
                EffectPlaying?.Invoke(f, fxIndex++, fx.Count);
                switch (f.K)
                {
                    case "attack":
                        if (units.TryGetValue(f.Unit, out var atk))
                        {
                            var u = after.Units.FirstOrDefault(z => z.Id == f.Unit);
                            bool drone = u != null && u.Type == "dron";
                            if (enemyTurn) ShowExecuting(f.Unit, f.X, f.Y);
                            Sfx.Play(drone ? Cue.RaidDroneShot : Cue.RaidMelee, 1, Pan(atk.transform.position));
                            yield return drone ? Shot(atk, f.X, f.Y) : Lunge(atk, f.X, f.Y);
                            if (version != buildVersion) yield break;
                            if (enemyTurn) FinishExecuting(f.Unit);
                        }
                        break;
                    case "dmg":
                    {
                        RaidPiece p = f.Unit > 0 && units.TryGetValue(f.Unit, out var up) ? up : objects.TryGetValue(Key(f.X, f.Y), out var op) ? op : null;
                        p?.Flash(lib.threat);
                        Sfx.Play(f.Unit > 0 ? Cue.RaidHit : Cue.RaidHitObject, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.F("−{0}", f.N), lib.threat, popups++ * 0.04f));
                        yield return Wait(enemyTurn ? 0.5f : 0.35f);
                        break;
                    }
                    case "armor":
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("ZIRH"), graphiteColor, 0));
                        Sfx.Play(Cue.RaidArmor, 1, Pan(W(f.X, f.Y)));
                        break;
                    case "heal":
                        if (objects.TryGetValue(Key(f.X, f.Y), out var hp)) hp.Flash(lib.good);
                        Sfx.Play(Cue.RaidRepair, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("+1"), lib.good, 0));
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
                        StartCoroutine(Burst(W(f.X, f.Y), verifiedColor, 1.6f, 0.35f));
                        yield return Wait(0.15f);
                        break;
                    case "die":
                        if (units.TryGetValue(f.Unit, out var dp))
                        {
                            var du = after.Units.FirstOrDefault(z => z.Id == f.Unit);
                            Sfx.Play(du != null && du.Side == 'p' ? Cue.RaidUnitDown : Cue.RaidRobotDown, 1, Pan(dp.transform.position));
                            if (du != null && du.Side == 'p') yield return Fall(dp);
                            else yield return Sink(dp);
                            if (version != buildVersion) yield break;
                            if (orderLabels.TryGetValue(f.Unit, out var cancelled) && shownIntents.TryGetValue(f.Unit, out var cancelledIntent))
                            {
                                cancelled.SetOpacity(0.55f); cancelled.SetPaperColor(paperColor);
                                cancelled.Show(Loc.F("<s>{0}</s> İPTAL", cancelledIntent.Order), lib.threat, graphiteColor);
                                LayoutLabels();
                            }
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
                if (version != buildVersion) yield break;
            }
            yield return Wait(0.15f);
            if (version != buildVersion) yield break;
            activeIntentHighlight.gameObject.SetActive(false);
            Busy = false;
        }

        void ShowExecuting(int id, int x, int y)
        {
            if (!units.TryGetValue(id, out var actor)) return;
            // Yalnız başlamış efektin hedefi açılır; gelecekteki gizli yön hâlâ okunmaz.
            var from = actor.transform.position + Vector3.up * 0.30f;
            var to = W(x, y) + Vector3.up * 0.16f;
            var direction = (to - from).normalized;
            activeIntentHighlight.gameObject.SetActive(true);
            activeIntentHighlight.Set(from + direction * 0.24f, to - direction * 0.30f, highlighterColor, false, PixelWorld * 12, 0);
            if (orderLabels.TryGetValue(id, out var flag) && shownIntents.TryGetValue(id, out var intent))
            {
                flag.SetPaperColor(lib.threat);
                flag.Show(Loc.F("{0} ŞİMDİ", intent.Order), paperColor, lib.threat);
                LayoutLabels();
            }
        }

        void FinishExecuting(int id)
        {
            activeIntentHighlight.gameObject.SetActive(false);
            if (intentArrowByUnit.TryGetValue(id, out var arrow)) arrow.SetOpacity(0.35f);
            if (orderLabels.TryGetValue(id, out var flag) && shownIntents.TryGetValue(id, out var intent))
            {
                flag.SetPaperColor(paperColor); flag.SetOpacity(0.55f);
                flag.Show(Loc.F("<s>{0}</s> UYGULANDI", intent.Order), lib.threat, lib.threat);
                LayoutLabels();
            }
        }

        public float Pan(Vector3 world) => Sfx.PanOf(cam, world);

        IEnumerator Wait(float s) { yield return new WaitForSeconds(s * stepTime / 0.32f); }
        static float Ease(float t) => 1 - (1 - t) * (1 - t);

        IEnumerator Lunge(RaidPiece p, int x, int y)
        {
            int version = buildVersion;
            var a = p.transform.position;
            var dir = W(x, y) - a; dir.y = 0;
            var b = a + Vector3.ClampMagnitude(dir, 0.45f);
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 0.5f)) { if (version != buildVersion || p == null) yield break; p.transform.position = Vector3.Lerp(a, b, Mathf.Sin(t * Mathf.PI)); yield return null; }
            if (version != buildVersion || p == null) yield break;
            p.transform.position = a;
        }

        IEnumerator Shot(RaidPiece p, int x, int y)
        {
            int version = buildVersion;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(marksRoot, false);
            go.transform.localScale = Vector3.one * 0.16f;
            go.GetComponent<Renderer>().sharedMaterial = lib.overlay;
            var mf = go.GetComponent<MeshFilter>();
            var mesh = mf.mesh;
            effectMeshes.Add(mesh);
            mesh.SetColors(Enumerable.Repeat(lib.threat, mesh.vertexCount).ToList());
            var a = p.Body.position; var b = W(x, y) + Vector3.up * 0.5f;
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 0.6f)) { if (version != buildVersion || go == null) yield break; go.transform.position = Vector3.Lerp(a, b, t); yield return null; }
            effectMeshes.Remove(mesh); if (mesh != null) Destroy(mesh);
            Destroy(go);
        }

        IEnumerator Slide(RaidPiece p, Vector3 to, float time)
        {
            int version = buildVersion;
            var a = p.transform.position;
            for (float t = 0; t < 1; t += Time.deltaTime / time) { if (version != buildVersion || p == null) yield break; p.transform.position = Vector3.Lerp(a, to, Ease(t)); yield return null; }
            if (version != buildVersion || p == null) yield break;
            p.transform.position = to;
        }

        IEnumerator Sink(RaidPiece p)
        {
            int version = buildVersion;
            var a = p.Body.localPosition; var s = p.Body.localScale;
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 1.2f))
            {
                if (version != buildVersion || p == null) yield break;
                p.Body.localPosition = a + Vector3.down * (t * 0.6f);
                p.Body.localScale = s * (1 - t * 0.7f);
                yield return null;
            }
            if (version == buildVersion && p != null) p.gameObject.SetActive(false);
        }

        IEnumerator Fall(RaidPiece p)
        {
            int version = buildVersion;
            var rotation = p.Body.localRotation;
            var fallen = p.DownRotation;
            // Tur sonu dönüşü düşme canlandırmasıyla yarışmasın; krem/harf yüzü yukarı kalır.
            p.SetDown(true); p.Body.localRotation = rotation;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.30f)
            {
                if (version != buildVersion || p == null) yield break;
                p.Body.localRotation = Quaternion.Slerp(rotation, fallen, Ease(t));
                yield return null;
            }
            if (version == buildVersion && p != null) p.Body.localRotation = fallen;
        }

        IEnumerator Collapse(RaidPiece p)
        {
            int version = buildVersion;
            var s = p.Body.localScale;
            p.Flash(Color.white, 0.4f);
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 1.2f)) { if (version != buildVersion || p == null) yield break; p.Body.localScale = new Vector3(s.x * (1 + t * 0.15f), s.y * (1 - t * 0.85f), s.z * (1 + t * 0.15f)); yield return null; }
        }

        IEnumerator Burst(Vector3 at, Color color, float size, float time)
        {
            int version = buildVersion;
            var ring = Frame("Burst", at, color, 0.3f, 0.06f);
            for (float t = 0; t < 1; t += Time.deltaTime / time)
            {
                if (version != buildVersion || ring == null) yield break;
                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, size, Ease(t)) / 0.3f;
                yield return null;
            }
            Destroy(ring);
        }

        IEnumerator Popup(Vector3 at, string text, Color color, float delay)
        {
            int version = buildVersion;
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (version != buildVersion) yield break;
            var label = NewPaperLabel("Effect popup", 16);
            label.Show(text, color, color);
            var a = at + Vector3.up * 1.3f;
            for (float k = 0; k < 1; k += Time.deltaTime / 0.93f)
            {
                if (version != buildVersion || label == null) yield break;
                label.transform.position = a + cam.transform.up * (k * PixelWorld * 12);
                label.SetOpacity(1 - Mathf.Max(0, k - 0.73f) / 0.27f);
                yield return null;
            }
            Destroy(label.gameObject);
        }
    }
}
