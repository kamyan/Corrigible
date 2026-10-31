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
        /// <summary>Karoların ve vurguların çizildiği bilgi: baskın veri kaynağı hesaplar, tahta yalnızca gösterir.</summary>
        public sealed class Marks
        {
            public HashSet<int> Moves = new HashSet<int>(), Targets = new HashSet<int>(), Heal = new HashSet<int>(), Spawns = new HashSet<int>();
            /// <summary>Kopyalama niyetinin hedef kareleri: kesikli kahverengi hayalet halka (raid.js ghostWake), iğnesiz.</summary>
            public HashSet<int> Clones = new HashSet<int>();
            /// <summary>Düşman niyetinin ya da önizlenen hamlenin vuracağı kareler.</summary>
            public HashSet<int> Danger = new HashSet<int>();
            public HashSet<int> Spent = new HashSet<int>();
            /// <summary>Salt sunum örneğinde başlangıçta kırık gelen dolaplar; canlı oyunda boş kalır.</summary>
            public HashSet<int> Rubble = new HashSet<int>();
            public int Selected = -1;
            /// <summary>İkmal görevinde kasa alındıysa çıkışlar (giriş kareleri) ve malzemeyi taşıyan birim; yoksa boş / −1.</summary>
            public HashSet<int> Exits = new HashSet<int>();
            public int Carrier = -1;
            /// <summary>Eğitim baskınında sıradaki hedef kare (sarı, nabız gibi atan çift çerçeve); yoksa −1.</summary>
            public int Guide = -1;
            public List<(int fx, int fy, int tx, int ty)> Arrows = new List<(int, int, int, int)>();
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
        [Tooltip("Sade tahta: ekran etiket prefab'ı atanırsa can sayıları yerine noktalar çizilir.")]
        [SerializeField] RaidScreenLabel screenLabelPrefab;
        [SerializeField] Canvas screenLabelCanvas;
        /// <summary>Sonuç kartında (S7) tahtanın etiketleri ve bayrakları gizli: karartılmış tahtada yalnız spot.</summary>
        public bool LabelsHidden
        {
            get => screenLabelCanvas != null && !screenLabelCanvas.enabled;
            set { if (screenLabelCanvas != null && screenLabelCanvas.enabled == value) screenLabelCanvas.enabled = !value; }
        }
        [SerializeField] Color spentPaperColor = new Color(.914f, .867f, .776f);
        [SerializeField, Min(.1f)] float markScale = 1;
        [SerializeField] Color paperColor = new Color(0.984f, 0.969f, 0.925f);
        [SerializeField] Color verifiedColor = new Color(0.102f, 0.278f, 0.537f);
        [SerializeField] Color graphiteColor = new Color(0.118f, 0.106f, 0.086f);
        [SerializeField] Color highlighterColor = new Color(0.961f, 0.863f, 0.388f, 0.95f);
        [Tooltip("Düşman turunda bir adımın süresi (saniye).")]
        [SerializeField, Min(0.05f)] float stepTime = 0.32f;
        [Tooltip("Pafta kenarındaki kare harf ve sayılarının boyu.")]
        [SerializeField, Min(0.1f)] float coordinateSize = 2.2f;

        public event Action<int, int> TileClicked;
        public event Action<(int x, int y)?> HoverChanged;
        public event Action<Baskin.Fx, int, int> EffectPlaying;
        public bool Busy { get; private set; }
        public bool InputEnabled { get; set; } = true;
        public bool FastMachines { get; set; }
        public float MachineDelay { get; set; }
        (int x, int y)? externalHover;
        /// <summary>Ekip kartı üstüne gelme işareti; UI seçimi ya da simülasyonu değiştirmez.</summary>
        public void SetExternalHover((int x, int y)? tile) => externalHover = tile;
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
        /// <summary>Duvar kareleri ve vurgulu kareler (yürüme, hedef, eğitim çerçevesi): tıklamanın hangi kareye düşeceği.</summary>
        readonly bool[] walls = new bool[64];
        readonly HashSet<int> pickable = new HashSet<int>();
        int guideKey = -1;
        readonly RaycastHit[] hits = new RaycastHit[16];
        static readonly Comparer<RaycastHit> ByDistance = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
        readonly RaidArrow[] moveFrames = new RaidArrow[64], dangerFrames = new RaidArrow[64], repairFrames = new RaidArrow[64];
        readonly RaidArrow[] moveHatches = new RaidArrow[64], dangerHatches = new RaidArrow[64], wakeRings = new RaidArrow[64], ghostRings = new RaidArrow[64], exitFrames = new RaidArrow[64];
        readonly Dictionary<int, RaidWorldLabel> hpLabels = new Dictionary<int, RaidWorldLabel>();
        readonly Dictionary<int, RaidWorldLabel> orderLabels = new Dictionary<int, RaidWorldLabel>(), stateLabels = new Dictionary<int, RaidWorldLabel>();
        readonly Dictionary<int, GameObject> flagPins = new Dictionary<int, GameObject>();
        readonly Dictionary<int, RaidArrow> downMarks = new Dictionary<int, RaidArrow>();
        readonly List<RaidWorldLabel> paperLabels = new List<RaidWorldLabel>();
        readonly List<RaidWorldLabel[]> labelGroups = new List<RaidWorldLabel[]>();
        readonly List<Rect> occupiedLabels = new List<Rect>();
        readonly List<Mesh> effectMeshes = new List<Mesh>();
        readonly Dictionary<int, Baskin.IntentView> shownIntents = new Dictionary<int, Baskin.IntentView>();
        readonly Dictionary<int, RaidArrow> intentArrowByUnit = new Dictionary<int, RaidArrow>();
        RaidArrow selectionFrame, guideFrame;
        Renderer selectionFill;
        RaidArrow activeIntentHighlight;
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
        float PixelWorld => (cam != null ? Mathf.Max(0.001f, 2 * cam.orthographicSize / Mathf.Max(1, cam.pixelHeight) * Mathf.Min(cam.pixelWidth / 1920f, cam.pixelHeight / 1080f)) : 0.0113f) * markScale;
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
            hpLabels.Clear(); orderLabels.Clear(); stateLabels.Clear(); paperLabels.Clear(); shownIntents.Clear(); intentArrowByUnit.Clear(); flagPins.Clear(); downMarks.Clear();
            arrowShadows.Clear(); extraArrows.Clear(); woke.Clear(); wokeStamped.Clear(); foreach (var b in badgeLabels) if (b != null) Destroy(b.gameObject); badgeLabels.Clear();
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
                walls[k] = c.T == "wall";
                tint[k] = Flat($"Tint {x},{y}", W(x, y) + Vector3.up * 0.02f, 0.94f, lib.highlight).GetComponent<Renderer>();
                tint[k].enabled = false;
                // raid.js mark(): yetenek hedefi yeşil çift çerçeve (8/3 px), hareket taraması kenarı 4 px; 1 doku pikseli = 0,98/256
                var targetFrame = Mark($"Frame {x},{y}", W(x, y), 0.03f);
                targetFrame.SetDoubleFrame(0.873f, 0.0306f, 0.72f, 0.0115f, Alpha(lib.target, 0.95f));
                frames[k] = targetFrame.gameObject;
                moveFrames[k] = Mark("Move edge", W(x, y), 0.027f);
                moveFrames[k].SetFrame(0.90f, 0.0153f, Alpha(lib.move, 0.9f));
                moveHatches[k] = Mark("Move hatch", W(x, y), 0.024f);
                moveHatches[k].SetHatch(0.98f, Alpha(lib.move, 0.34f), 0.019f, true);
                dangerFrames[k] = Mark("Danger edge", W(x, y), 0.031f);
                dangerHatches[k] = Mark("Danger hatch", W(x, y), 0.028f);
                repairFrames[k] = Mark("Repair edge", W(x, y), 0.034f);
                repairFrames[k].SetFrame(0.88f, PixelWorld * 2, Alpha(lib.threat, 0.85f), true);
                wakeRings[k] = Mark("Wake pin ring", W(x, y), 0.04f);
                wakeRings[k].SetCircle(0.3275f, Alpha(lib.threat, 0.95f), 0.035f, false, true);
                ghostRings[k] = Mark("Clone ring", W(x, y), 0.04f);
                ghostRings[k].SetCircle(0.3275f, Alpha(lib.intentQuiet, 0.95f), 0.035f, true, true);
                exitFrames[k] = Mark("Exit edge", W(x, y), 0.036f);
                exitFrames[k].SetFrame(0.80f, PixelWorld * 3.5f, lib.good, true);
                if (lib.spawnMarker != null)
                {
                    spawnMarks[k] = Instantiate(lib.spawnMarker, W(x, y), Quaternion.identity, marksRoot);
                    spawnMarks[k].SetActive(false);
                }
            }
            hoverFrame = Frame("Hover", Vector3.zero, lib.hover, 0.94f, PixelWorld * 1.8f);
            hoverFrame.SetActive(false);
            // raid.js frameTex('#1A4789', 10, 4): dış 0,873 kalın, iç 0,72 ince, α .10 dolgu
            selectionFrame = Mark("Selected double frame", Vector3.zero, 0.04f);
            selectionFrame.SetDoubleFrame(0.873f, 0.0383f, 0.72f, 0.0153f, Alpha(verifiedColor, 0.95f));
            selectionFill = Flat("Selected fill", Vector3.zero, 0.873f, lib.highlight).GetComponent<Renderer>();
            SetColor(selectionFill, Alpha(verifiedColor, 0.10f));
            selectionFill.enabled = false;
            guideFrame = Mark("Tutorial guide frame", Vector3.zero, 0.05f, true);
            guideFrame.SetFrame(0.97f, PixelWorld * 5, highlighterColor, false, true);
            guideFrame.gameObject.SetActive(false);
            activeIntentHighlight = NewArrow("Current intent highlighter");
            activeIntentHighlight.gameObject.SetActive(false);
            BuildCoordinates();
        }

        /// <summary>Paftadaki kare adı: sütun harfi ve satır sayısı ("C3"). Telsizde kareler bu adla anılır.</summary>
        public static string CellName(int x, int y) => Column(x) + Row(y);
        static string Column(int x) => ((char)('A' + x)).ToString();
        static string Row(int y) => (Baskin.N - y).ToString();

        /// <summary>
        /// Paftanın kenar yazıları kameraya bakan kenarlarda: sütun harfleri güneyde (A soldan), satır sayıları batıda (1
        /// kameraya en yakın). Elle yazılmış gibi, mavi mürekkeple; yazı yukarısı ekranın yukarısına bakar.
        /// </summary>
        void BuildCoordinates()
        {
            var font = handFont != null ? handFont : lib.font;
            if (font == null) return;
            const float margin = 4.13f;
            var rotation = Quaternion.Euler(90, 45, 0);
            for (int i = 0; i < Baskin.N; i++)
            {
                Coordinate(Column(i), new Vector3(i - 3.5f, TileTop + 0.006f, -margin), rotation, font);
                Coordinate(Row(i), new Vector3(-margin, TileTop + 0.006f, 3.5f - i), rotation, font);
            }
        }

        void Coordinate(string text, Vector3 position, Quaternion rotation, TMP_FontAsset font)
        {
            var go = new GameObject("Pafta " + text, typeof(TextMeshPro));
            go.transform.SetParent(marksRoot, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var label = go.GetComponent<TextMeshPro>();
            label.font = font;
            label.text = text;
            label.fontSize = coordinateSize;
            label.color = verifiedColor;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(0.4f, 0.3f);
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

        /// <summary>Karo işareti: parçalar örter (<c>lib.tileMarks</c>); <paramref name="onTop"/> eğitim çerçevesi gibi her zaman üstte.</summary>
        RaidArrow Mark(string name, Vector3 position, float height, bool onTop = false)
        {
            var mark = NewArrow(name, onTop ? null : lib.tileMarks);
            mark.transform.position = position + Vector3.up * height;
            mark.gameObject.SetActive(false);
            return mark;
        }

        RaidWorldLabel NewPaperLabel(string name, float pixels, bool hand = false)
        {
            var go = new GameObject(name, typeof(RaidWorldLabel));
            go.transform.SetParent(marksRoot, false);
            var label = go.GetComponent<RaidWorldLabel>();
            if (screenLabelPrefab != null && screenLabelCanvas != null)
            {
                var screen = Instantiate(screenLabelPrefab, screenLabelCanvas.transform);
                screen.name = name;
                label.ConfigureScreen(cam, screenLabelCanvas, screen, hand && handFont != null ? handFont : lib.font, paperColor, pixels, hand ? 15 : 11);
                return label;
            }
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
            // Sahne kapanırken (Play durur, oyun kapanır) arayüz son bir “üzerinden çıkıldı” gönderebilir; kamera ya da tahta gitmiştir
            if (cam == null || this == null) return;
            lastState = B;
            labelGroups.Clear();
            foreach (var label in hpLabels.Values) label.gameObject.SetActive(false);
            foreach (var label in orderLabels.Values) label.gameObject.SetActive(false);
            foreach (var label in stateLabels.Values) label.gameObject.SetActive(false);
            foreach (var pin in flagPins.Values) pin.SetActive(false);
            foreach (var mark in downMarks.Values) mark.gameObject.SetActive(false);
            shownIntents.Clear();
            intentArrowByUnit.Clear();
            activeIntentHighlight.gameObject.SetActive(false);
            // Niyetler Core'un görünüm API'sinden (Baskin.Intents) okunur.
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
                    if (screenLabelPrefab == null || (o.K != "civ" && o.K != "cool"))
                    {
                        var tag = PieceLabel(hpLabels, -k - 1, "Object HP");
                        // Revizyon 2 · S6: nesnenin etiketi tepe noktasının 28 px üstünde
                        tag.SetAnchor(W(x, y) + Vector3.up * piece.VisualHeight + cam.transform.up * (PixelWorld * 28));
                        var ink = Baskin.IsTarget(o.K) || o.K == "spawner" ? lib.threat : graphiteColor;
                        if (screenLabelPrefab != null) tag.ShowHealth(o.Hp, o.Max, ink, Baskin.IsTarget(o.K), o.K == "node" || o.K == "core", null, lib.threat);
                        else tag.Show(Loc.F("{0}/{1}", o.Hp, o.Max), ink, ink);
                        labelGroups.Add(new[] { tag });
                    }
                    if (rubble.TryGetValue(k, out var rb)) { Destroy(rb); rubble.Remove(k); }
                }
                else if (piece != null)
                {
                    Destroy(piece.gameObject); objects.Remove(k);
                    if (lib.rubble != null && !rubble.ContainsKey(k)) rubble[k] = Instantiate(lib.rubble, W(x, y), Quaternion.Euler(0, (x * 37 + y * 53) % 360, 0), piecesRoot);
                }
                else if (m.Rubble.Contains(k) && lib.rubble != null && !rubble.ContainsKey(k))
                    rubble[k] = Instantiate(lib.rubble, W(x, y), Quaternion.identity, piecesRoot);
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
                p.SetPlate(!u.Dead && u.Armor > 0);
                p.SetWorldLabelsVisible(false);
                string letter = u.Type switch { "tek" => "T", "emp" => "E", "ana" => "A", "dron" => "D", _ => "B" };
                var hpTag = PieceLabel(hpLabels, u.Id, "Unit HP");
                // Revizyon 2 · S6: uzmanın ve robotun etiketi altlığın önünde (u − 0,24, v + 0,24); robotta tür işareti + zırh
                hpTag.SetAnchor(W(u.X, u.Y) + new Vector3(-0.24f, 0.02f, -0.24f));
                var hpInk = u.Side == 'e' || u.Dead ? lib.threat : graphiteColor;
                if (screenLabelPrefab != null)
                {
                    hpTag.SetPaperColor(m.Spent.Contains(u.Id) ? spentPaperColor : paperColor);
                    hpTag.ShowHealth(u.Dead ? 0 : u.Hp, u.Max, hpInk, false, !u.Dead && u.Armor > 0, u.Side == 'e' ? lib.Glyph(u.Type) : null, lib.threat);
                }
                else hpTag.Show(Loc.F("{0} {1}/{2}", letter, u.Dead ? 0 : u.Hp, u.Max), hpInk, hpInk);
                bool justWoke = u.Side == 'e' && !u.Dead && woke.TryGetValue(u.Id, out int wokeTurn) && B.Turn == wokeTurn;
                RaidWorldLabel wokeLabel = null;
                if (justWoke)
                {
                    // UYANDI: robotun tepesinde, sıra bayrağının hemen altında kırmızı kenarlı kâğıt; bayrakla birlikte yerleşir
                    wokeLabel = PieceLabel(stateLabels, u.Id, "Unit state", 14);
                    wokeLabel.SetAnchor(W(u.X, u.Y) + Vector3.up * p.VisualHeight + cam.transform.up * (PixelWorld * 22));
                    wokeLabel.Show(Loc.T("UYANDI"), lib.threat, lib.threat);
                    if (wokeStamped.Add(u.Id)) wokeLabel.Stamp(wakeStampSeconds);
                    labelGroups.Add(new[] { hpTag });
                }
                else if (u.Dead || m.Spent.Contains(u.Id) || u.Locked || u.Id == m.Carrier)
                {
                    var state = PieceLabel(stateLabels, u.Id, "Unit state", 14);
                    state.SetAnchor(hpTag.transform.position - cam.transform.up * 0.30f);
                    state.Show(u.Dead ? Loc.T("DÜŞTÜ") : u.Locked ? Loc.T("KİLİTLİ") : u.Id == m.Carrier ? Loc.T("YÜKLÜ") : Loc.T("TUR BİTTİ"), hpInk, hpInk);
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
                    var order = PieceLabel(orderLabels, u.Id, "Order flag", 20);
                    // Bayrak robotun tepe noktasından 34 px iğneyle, sağa açılır (kâğıt 30 × 28)
                    order.SetAnchor(W(u.X, u.Y) + Vector3.up * p.VisualHeight + cam.transform.up * (PixelWorld * (34 + 14)) + cam.transform.right * (PixelWorld * 15));
                    string kind = intent.Kind == "repair" ? Loc.T("ONARIM") : intent.Kind == "shoot" ? Loc.T("ATEŞ") : Loc.T("VURUŞ");
                    Color border = lib.threat;
                    border.a = 1;
                    order.SetOpacity(1);
                    order.SetPaperColor(paperColor);
                    if (screenLabelPrefab != null) order.ShowFlag(intent.Order.ToString(), lib.threat, border);
                    else order.Show(Loc.F("{0} {1}", intent.Order, kind), lib.threat, border, false);
                    labelGroups.Add(wokeLabel != null ? new[] { order, wokeLabel } : new[] { order });
                }
                else if (wokeLabel != null) labelGroups.Add(new[] { wokeLabel });
            }
            foreach (var id in units.Keys.Where(id => !alive.Contains(id)).ToList()) { if (units[id] != null) Destroy(units[id].gameObject); units.Remove(id); }

            // Karo vurguları
            for (int k = 0; k < 64; k++)
            {
                Color c = default; bool on = false;
                if (m.Moves.Contains(k)) { c = lib.move; on = true; }
                if (m.Danger.Contains(k)) { c = Alpha(lib.threat, 0.12f); on = true; }
                else if (m.Heal.Contains(k)) { c = lib.heal; on = true; }
                else if (m.Targets.Contains(k) && !on) { c = Alpha(lib.target, 0.10f); on = true; }
                tint[k].enabled = on;
                if (on) SetColor(tint[k], c);
                frames[k].SetActive(m.Targets.Contains(k));
                moveFrames[k].gameObject.SetActive(m.Moves.Contains(k));
                moveHatches[k].gameObject.SetActive(m.Moves.Contains(k) && !m.Danger.Contains(k));
                bool hit = m.Danger.Contains(k);
                dangerFrames[k].gameObject.SetActive(hit);
                dangerHatches[k].gameObject.SetActive(hit);
                if (hit)
                {
                    dangerFrames[k].SetFrame(0.90f, 0.023f, Alpha(lib.threat, 0.9f), false);
                    dangerHatches[k].SetHatch(0.98f, Alpha(lib.threat, 0.5f), 0.019f, false);
                }
                repairFrames[k].gameObject.SetActive(m.Heal.Contains(k));
                wakeRings[k].gameObject.SetActive(m.Spawns.Contains(k));
                ghostRings[k].gameObject.SetActive(m.Clones.Contains(k) && !m.Spawns.Contains(k));
                exitFrames[k].gameObject.SetActive(m.Exits.Contains(k));
                if (spawnMarks[k] != null) spawnMarks[k].SetActive(m.Spawns.Contains(k));
            }
            var selected = B.Units.FirstOrDefault(u => u.Id == m.Selected && !u.Dead);
            selectionFrame.gameObject.SetActive(selected != null);
            selectionFill.enabled = selected != null;
            if (selected != null)
            {
                selectionFrame.transform.position = W(selected.X, selected.Y) + Vector3.up * 0.045f;
                selectionFill.transform.position = W(selected.X, selected.Y) + Vector3.up * 0.021f;
            }
            pickable.Clear(); pickable.UnionWith(m.Moves); pickable.UnionWith(m.Targets);
            if (m.Guide >= 0) pickable.Add(m.Guide);
            guideKey = m.Guide;
            guideFrame.gameObject.SetActive(m.Guide >= 0);
            if (m.Guide >= 0) guideFrame.transform.position = W(m.Guide % Baskin.N, m.Guide / Baskin.N) + Vector3.up * 0.06f;
            // Niyet okları (Revizyon 2 · S9-C/C2): türe göre kalınlık, yay ve rozet; gölge (1,5; 2) α .28; her zaman üstte
            int shadowsUsed = 0, extrasUsed = 0, badgesUsed = 0;
            foreach (var badge in badgeLabels) badge.gameObject.SetActive(false);
            for (int i = 0; i < m.Arrows.Count; i++)
            {
                if (i == arrows.Count) arrows.Add(NewArrow("Intent"));
                var a = m.Arrows[i];
                arrows[i].gameObject.SetActive(true);
                var intent = shownIntents.Values.FirstOrDefault(j => j.FromX == a.fx && j.FromY == a.fy && j.TargetX == a.tx && j.TargetY == a.ty);
                if (intent != null) intentArrowByUnit[intent.Unit] = arrows[i];
                DrawIntent(arrows[i], intent?.Kind ?? "shoot", a.fx, a.fy, a.tx, a.ty, ref shadowsUsed, ref extrasUsed, ref badgesUsed);
            }
            // Patlama: ok yok; dört komşuya kısa çıkış okları (0,30 → 0,66 birim, uç 12 px)
            foreach (var blast in shownIntents.Values.Where(j => j.Kind == "blast"))
            {
                var center = W(blast.FromX, blast.FromY) + Vector3.up * 0.16f;
                foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    var arrow = Extra(extrasUsed++);
                    arrow.Set(center + d * 0.30f, center + d * 0.66f, lib.intent, false, PixelWorld * 2.4f, 0, 0, true, PixelWorld * 12);
                    Shadow(shadowsUsed++, center + d * 0.30f, center + d * 0.66f, false, PixelWorld * 2.4f, 0, true, PixelWorld * 12);
                }
            }
            for (int i = m.Arrows.Count; i < arrows.Count; i++) arrows[i].gameObject.SetActive(false);
            for (int i = shadowsUsed; i < arrowShadows.Count; i++) arrowShadows[i].gameObject.SetActive(false);
            for (int i = extrasUsed; i < extraArrows.Count; i++) extraArrows[i].gameObject.SetActive(false);
            LayoutLabels();
        }

        readonly List<RaidArrow> arrowShadows = new List<RaidArrow>(), extraArrows = new List<RaidArrow>();
        /// <summary>Son eşitlenen tahta (havan ve kalkan oku hedefin tepe noktasını bulur).</summary>
        Baskin.BState lastState;
        /// <summary>Uyanan robotun kimliği → uyanmasından sonraki tur (o tur boyunca UYANDI etiketi taşır).</summary>
        readonly Dictionary<int, int> woke = new Dictionary<int, int>();
        /// <summary>UYANDI damgası inmiş robotlar (damga bir kez iner).</summary>
        readonly HashSet<int> wokeStamped = new HashSet<int>();
        [Tooltip("UYANDI etiketinin damga gibi inme süresi (S6: 0,12 s).")]
        [SerializeField, Min(0)] float wakeStampSeconds = 0.12f;
        readonly List<RaidWorldLabel> badgeLabels = new List<RaidWorldLabel>();

        /// <summary>Dikey dünya birimi ekranda yatay pikselin ~1/1,22'si (35° eğik kamera): yay yüksekliği pikselden.</summary>
        float RisePx(float px) => PixelWorld * px * 1.22f;

        RaidArrow Extra(int i)
        {
            if (i == extraArrows.Count) extraArrows.Add(NewArrow("Intent extra"));
            extraArrows[i].gameObject.SetActive(true);
            return extraArrows[i];
        }

        void Shadow(int i, Vector3 from, Vector3 to, bool dashed, float width, float arc, bool tip, float head)
        {
            if (i == arrowShadows.Count)
            {
                var s = NewArrow("Intent shadow");
                s.GetComponent<MeshRenderer>().sortingOrder = -1;
                arrowShadows.Add(s);
            }
            var shadow = arrowShadows[i];
            shadow.gameObject.SetActive(true);
            var offset = cam.transform.right * (PixelWorld * 1.5f) - cam.transform.up * (PixelWorld * 2);
            shadow.Set(from + offset, to + offset, new Color(0, 0, 0, 0.28f), dashed, width, arc, 0, tip, head);
        }

        void Badge(ref int used, Sprite icon, Vector3 world, float size)
        {
            if (icon == null) return;
            if (used == badgeLabels.Count) badgeLabels.Add(NewPaperLabel("Intent badge", 12, false));
            var badge = badgeLabels[used++];
            badge.SetAnchor(world);
            badge.ShowIcon(icon, size, Color.white);
        }

        /// <summary>
        /// Bir niyetin oku (S9-C/C2): ateş düz 3,4 px; yakın dövüş ve kilit yay 4,6 px (kilitte kıskaç rozeti); onarım kesik
        /// 2,6 px + anahtar; havan yüksek yay (kirişin 0,4'ü, en az 90 px) namludan; kalkan çift şerit, ucu yok, kalkan rozeti;
        /// kopyalama kesik + uç 11 px, dron rozeti. Zarar vermeyenler #854005.
        /// </summary>
        void DrawIntent(RaidArrow arrow, string kind, int fx, int fy, int tx, int ty, ref int shadows, ref int extras, ref int badges)
        {
            var start = W(fx, fy) + Vector3.up * 0.30f;
            var end = W(tx, ty) + Vector3.up * 0.16f;
            var direction = (end - start).normalized;
            float px = PixelWorld;
            bool quiet = kind == "shield" || kind == "clone";
            var color = quiet ? lib.intentQuiet : lib.intent;
            float width = 3.4f, arc = 0, head = 16;
            bool dashed = false, tip = true;
            switch (kind)
            {
                case "shoot":
                    start += direction * 0.24f; end -= direction * 0.40f;
                    break;
                case "lob":
                {
                    start = W(fx, fy) + Vector3.up * 0.8f;
                    var target = Baskin.UnitAt(lastState, tx, ty);
                    end = W(tx, ty) + Vector3.up * (target != null && units.TryGetValue(target.Id, out var tp) && tp != null ? tp.VisualHeight : 0.16f) + cam.transform.up * (px * 10);
                    float chord = Vector3.Distance(start, end) / px;
                    arc = RisePx(Mathf.Max(0.4f * chord, 90));
                    break;
                }
                case "repair":
                    width = 2.6f; arc = RisePx(26); dashed = true; head = 11; start += direction * 0.24f; end -= direction * 0.30f;
                    break;
                case "shield":
                    width = 1.6f; arc = RisePx(20); tip = false; start += direction * 0.24f; end -= direction * 0.30f;
                    break;
                case "clone":
                    width = 2.6f; dashed = true; head = 11; start += direction * 0.24f; end -= direction * 0.30f;
                    break;
                default:
                    // Yakın dövüş ve kilit: yay 4,6 px (yükselme 14–26 px), uç 15 px
                    width = 4.6f; arc = RisePx(20); head = 15; start += direction * 0.24f; end -= direction * 0.30f;
                    break;
            }
            Shadow(shadows++, start, end, dashed, px * width, arc, tip, px * head);
            arrow.Set(start, end, color, dashed, px * width, arc, 0, tip, px * head);
            Vector3 mid = (start + end) * 0.5f + Vector3.up * arc;
            switch (kind)
            {
                case "shield":
                {
                    // Çift şerit: ikinci çizgi 3,2 px yanda
                    var across = Vector3.Cross(Vector3.up, (end - start).normalized) * (px * 3.2f);
                    var twin = Extra(extras++);
                    twin.Set(start + across, end + across, color, false, px * width, arc, 0, false);
                    var shielded = Baskin.UnitAt(lastState, tx, ty);
                    float top = shielded != null && units.TryGetValue(shielded.Id, out var sp) && sp != null ? sp.VisualHeight : 0.5f;
                    Badge(ref badges, lib.badgeShield, W(tx, ty) + Vector3.up * top + cam.transform.up * (px * 22), 28);
                    break;
                }
                case "lock": Badge(ref badges, lib.badgeClamp, mid, 26); break;
                case "repair": Badge(ref badges, lib.badgeWrench, mid, 22); break;
                case "clone": Badge(ref badges, lib.badgeClone, W(tx, ty) + Vector3.up * 0.2f, 24); break;
            }
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

        // Yakın etiketler birlikte taşınır; ilişkileri dil ve çözünürlükte korunur.
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
                // İğne tepe noktasından kâğıdın sol alt köşesine
                FlagPin(pair.Key, actor.transform.position + Vector3.up * actor.VisualHeight,
                    pair.Value.transform.position - cam.transform.right * (PixelWorld * 15) - cam.transform.up * (PixelWorld * 14));
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

        RaidArrow NewArrow(string name, Material material = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(RaidArrow));
            go.transform.SetParent(marksRoot, false);
            go.GetComponent<MeshRenderer>().sharedMaterial = material != null ? material : lib.overlay;
            return go.GetComponent<RaidArrow>();
        }

        // ------------------------------------------------------------------ önizleme
        public void SetPreview(IList<(float x, float y, string text, Color color)> texts, IList<(int fx, int fy, int tx, int ty)> moves, ISet<int> blockedMoves = null)
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
                if (blockedMoves != null && blockedMoves.Contains(i)) { ghosts[i] = null; continue; }
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
            // Eğitim çerçevesi nabız gibi atar
            if (guideFrame != null && guideFrame.gameObject.activeSelf) guideFrame.transform.localScale = Vector3.one * (1 + 0.06f * Mathf.Sin(Time.unscaledTime * 6));
            if (cam == null || hoverFrame == null || Mouse.current == null) return;
            (int x, int y)? h = InputEnabled && !Busy ? externalHover : null;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (InputEnabled && !Busy && !overUi) h = Pick(cam.ScreenPointToRay(Mouse.current.position.ReadValue())) ?? h;
            if (h != hover)
            {
                hover = h;
                hoverFrame.SetActive(h != null);
                if (h != null) hoverFrame.transform.position = W(h.Value.x, h.Value.y) + Vector3.up * 0.035f;
                HoverChanged?.Invoke(h);
            }
            if (h != null && !overUi && Mouse.current.leftButton.wasPressedThisFrame) TileClicked?.Invoke(h.Value.x, h.Value.y);
        }

        /// <summary>
        /// Işının ilk çarptığı kare ya da parça. Duvar bloğu yarım kare yüksekliğindedir ve arkasındaki karenin yarısını örter:
        /// vurgusuz bir duvar, arkasındaki vurgulu kareyi (yürüme, hedef) bırakır. Eğitim çerçevesinin karesi ışının üstündeyse
        /// önünde ne olursa olsun o seçilir. Işın zemine değince tarama biter (zeminin altındaki çarpışmalar sayılmaz).
        /// </summary>
        (int x, int y)? Pick(Ray ray)
        {
            int n = Physics.RaycastNonAlloc(ray, hits, 200f);
            Array.Sort(hits, 0, n, ByDistance);
            (int x, int y)? first = null;
            bool throughWall = false;
            for (int i = 0; i < n; i++)
            {
                var cell = hits[i].collider.GetComponentInParent<RaidCell>();
                var piece = cell == null ? hits[i].collider.GetComponentInParent<RaidPiece>() : null;
                if (cell == null && piece == null) continue;
                var t = cell != null ? (cell.X, cell.Y) : (piece.X, piece.Y);
                int k = Key(t.Item1, t.Item2);
                if (k == guideKey) return t;
                if (first == null) { first = t; throughWall = cell != null && walls[k] && !pickable.Contains(k); }
                else if (throughWall && pickable.Contains(k)) return t;
                if (cell != null && !walls[k]) break;
            }
            return first;
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
            selectionFrame.gameObject.SetActive(false);
            guideFrame.gameObject.SetActive(false);
            if (enemyTurn) foreach (var pair in units)
                if (after.Units.Any(u => u.Id == pair.Key && u.Side == 'p' && !u.Dead)) pair.Value.SetSpent(true);
            int popups = 0;
            int fxIndex = 0;
            foreach (var f in fx)
            {
                if (version != buildVersion) yield break;
                if(enemyTurn&&f.K=="attack"&&fxIndex>0&&MachineDelay>0)yield return new WaitForSecondsRealtime(MachineDelay);
                EffectPlaying?.Invoke(f, fxIndex++, fx.Count);
                switch (f.K)
                {
                    case "attack":
                        if (units.TryGetValue(f.Unit, out var atk))
                        {
                            var u = after.Units.FirstOrDefault(z => z.Id == f.Unit);
                            // Uzaktan vuranlar (dron, havan, kilitleyici, kalkan dronu) atış; ötekiler hamle
                            bool drone = u != null && Baskin.UnitTypes.TryGetValue(u.Type, out var ut) && ut.Ranged;
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
                    case "shield":
                        // Kalkan dronu yanındaki makineye zırh verdi
                        if (units.TryGetValue(f.Unit, out var shielded)) shielded.Flash(verifiedColor);
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("ZIRHLANDI"), lib.threat, popups++ * 0.04f));
                        Sfx.Play(Cue.RaidArmor, 1, Pan(W(f.X, f.Y)));
                        yield return Wait(0.3f);
                        break;
                    case "lock":
                        if (units.TryGetValue(f.Unit, out var locked)) locked.Flash(lib.threat);
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("KİLİTLENDİ"), lib.threat, popups++ * 0.04f));
                        Sfx.Play(Cue.RaidHit, 1, Pan(W(f.X, f.Y)));
                        yield return Wait(0.35f);
                        break;
                    case "blast":
                        Sfx.Play(Cue.RaidExplosion, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Burst(W(f.X, f.Y), lib.threat, 2.2f, 0.35f));
                        yield return Wait(0.2f);
                        break;
                    case "plate":
                        // Uzmanın zırh plakası darbeyi karşıladı ve kırıldı
                        if (units.TryGetValue(f.Unit, out var plated)) plated.Flash(graphiteColor);
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("PLAKA KIRILDI"), graphiteColor, popups++ * 0.04f));
                        Sfx.Play(Cue.RaidArmor, 1, Pan(W(f.X, f.Y)));
                        yield return Wait(enemyTurn ? 0.35f : 0.25f);
                        break;
                    case "heal":
                        if (objects.TryGetValue(Key(f.X, f.Y), out var hp)) hp.Flash(lib.good);
                        Sfx.Play(Cue.RaidRepair, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("+1"), lib.good, 0));
                        yield return Wait(0.6f);
                        break;
                    case "loot":
                        if (objects.TryGetValue(Key(f.X, f.Y), out var cargo)) cargo.Flash(lib.good);
                        Sfx.Play(Cue.RaidRepair, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("İKMAL ALINDI"), lib.good, 0));
                        yield return Wait(0.45f);
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
                        // Yeni robot: uyandığı makine evresinden sonraki tur boyunca üstünde UYANDI (S6b). Uyanan robot
                        // aynı adımda planlanıp yürüyebildiği için kareyle değil kimlikle bulunur: uyanmalar kimlik sırasıyla gelir.
                        var born = after.Units.Where(n => n.Side == 'e' && !units.ContainsKey(n.Id) && !woke.ContainsKey(n.Id)).OrderBy(n => n.Id).FirstOrDefault();
                        if (born != null) woke[born.Id] = after.Turn;
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
                if (screenLabelPrefab != null) flag.ShowFlag(intent.Order.ToString(), paperColor, lib.threat);
                else flag.Show(Loc.F("{0} ŞİMDİ", intent.Order), paperColor, lib.threat);
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
                // Biten niyet: bayrak soluk, rakam çizik
                if (screenLabelPrefab != null) flag.ShowFlag(Loc.F("<s>{0}</s>", intent.Order), lib.threat, lib.threat);
                else flag.Show(Loc.F("<s>{0}</s> UYGULANDI", intent.Order), lib.threat, lib.threat);
                LayoutLabels();
            }
        }

        /// <summary>Örnek tahta ya da sunum köprüsü: komut yürütmeden mevcut niyetin görsel evresini seçer.</summary>
        public void SetIntentPresentation(int id, string state)
        {
            if (!shownIntents.TryGetValue(id, out var intent)) return;
            if (state == "done") FinishExecuting(id);
            else if (state == "now") ShowExecuting(id, intent.TargetX, intent.TargetY);
            else if (state == "cancel" && orderLabels.TryGetValue(id, out var cancelled))
            {
                cancelled.SetOpacity(0.55f);
                if (screenLabelPrefab != null) cancelled.ShowFlag(Loc.F("<s>{0}</s>", intent.Order), lib.threat, graphiteColor);
                else cancelled.Show(Loc.F("<s>{0}</s> İPTAL", intent.Order), lib.threat, graphiteColor);
            }
        }

        public float Pan(Vector3 world) => Sfx.PanOf(cam, world);

        IEnumerator Wait(float s) { yield return new WaitForSeconds(s * stepTime / 0.32f / (FastMachines ? 3.125f : 1)); }
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
            // Tur sonu dönüşü düşme canlandırmasıyla yarışmasın; harf yüzü yukarı kalır.
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
