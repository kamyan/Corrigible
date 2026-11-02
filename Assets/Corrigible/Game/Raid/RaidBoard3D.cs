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
            /// <summary>Sisli kareler (bu turun makine evresinde nişanlı ateş kesilir) ve önizlemede atılacak sis.</summary>
            public HashSet<int> Smoke = new HashSet<int>(), SmokePreview = new HashSet<int>();
            /// <summary>Sise ya da söndürme bulutuna kesilen niyetler: kesik ok kesildiği karede biter (cloud: buluta kesildi).</summary>
            public List<(int fx, int fy, int tx, int ty, string kind, bool cloud)> SmokedArrows = new List<(int, int, int, int, string, bool)>();
            /// <summary>Bölge olayının katmanı (<see cref="Baskin.Hazards"/>; yetenek önizlemesinde hamleden sonraki hâli); olay yoksa
            /// null ya da boş.</summary>
            public Baskin.HazardView Hazards;
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
        [Header("Makinelerin yer değiştirmesi (tur sonu)")]
        [Tooltip("Robot yeni yerine yürürken kare başına süre (saniye).")]
        [SerializeField, Min(0.05f)] float machineStepSeconds = 0.16f;
        [Tooltip("İki robotun yürüyüşü arasındaki duraklama (saniye).")]
        [SerializeField, Min(0)] float machinePauseSeconds = 0.08f;
        [Tooltip("Bütün yürüyüşlerin üst sınırı (saniye); aşılırsa adımlar kısalır, en az machineMinStepSeconds.")]
        [SerializeField, Min(0.5f)] float machineMovesMaxSeconds = 3f;
        [SerializeField, Min(0.03f)] float machineMinStepSeconds = 0.1f;
        [Tooltip("Her karede robotun küçük sıçraması (dünya birimi).")]
        [SerializeField, Min(0)] float machineStepHop = 0.1f;
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
        /// <summary>Karo nesneleri ve türleri: Madenci duvarı yıkınca (ya da geri alınınca) karo durumdan yeniden kurulur.</summary>
        readonly GameObject[] tileObjects = new GameObject[64];
        readonly string[] tileKinds = new string[64];
        readonly GameObject[] smokeClouds = new GameObject[64];
        readonly Dictionary<int, GameObject> breaches = new Dictionary<int, GameObject>();
        readonly List<RaidWorldLabel> smokeLabels = new List<RaidWorldLabel>();
        /// <summary>Bölge olayının katmanı (<see cref="SyncHazards"/>): tembel havuzlar; her Sync baştan dağıtır, artanı gizler.</summary>
        readonly List<Renderer> hazardFills = new List<Renderer>();
        readonly List<RaidArrow> hazardMarks = new List<RaidArrow>(), hazardStrokes = new List<RaidArrow>();
        readonly List<RaidWorldLabel> hazardLabels = new List<RaidWorldLabel>();
        /// <summary>Titreyen işaretler (alev almış gaz, çökecek tavan, kapanacak geçit…): Update ölçeklerini oynatır.</summary>
        readonly List<Transform> hazardPulse = new List<Transform>();
        /// <summary>Çizilen olay katmanı (tür, kare, nesne): canlandırma dağılan bulutu, boşalan gazı Sync'i beklemeden söndürür.</summary>
        readonly List<(string kind, int key, GameObject go)> hazardShown = new List<(string, int, GameObject)>();
        int hazardFillsUsed, hazardMarksUsed, hazardStrokesUsed, hazardLabelsUsed;
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
            breaches.Clear(); foreach (var l in smokeLabels) if (l != null) Destroy(l.gameObject); smokeLabels.Clear(); Array.Clear(smokeClouds, 0, 64);
            hazardFills.Clear(); hazardMarks.Clear(); hazardStrokes.Clear(); hazardPulse.Clear(); hazardShown.Clear();
            foreach (var l in hazardLabels) if (l != null) Destroy(l.gameObject); hazardLabels.Clear();
            for (int y = 0; y < Baskin.N; y++)
            for (int x = 0; x < Baskin.N; x++)
            {
                var c = Baskin.CellAt(B, x, y);
                int k = Key(x, y);
                BuildTile(x, y, c.T);
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
            // Karolar: yıkılan duvar zemine döner, geri alınan duvar yeniden kurulur; yıkık duvarın yerinde alçak moloz
            for (int k = 0; k < 64; k++)
            {
                string kind = Baskin.CellAt(B, k % Baskin.N, k / Baskin.N).T;
                if (tileKinds[k] != kind) BuildTile(k % Baskin.N, k / Baskin.N, kind);
            }
            var broken = new HashSet<int>(B.Rubble.Select(p => Key(p.X, p.Y)));
            foreach (var k in breaches.Keys.Where(k => !broken.Contains(k) || walls[k]).ToList()) { if (breaches[k] != null) Destroy(breaches[k]); breaches.Remove(k); }
            foreach (var k in broken)
                if (!walls[k] && !breaches.ContainsKey(k) && lib.rubble != null)
                {
                    var debris = Instantiate(lib.rubble, W(k % Baskin.N, k / Baskin.N), Quaternion.Euler(0, (k * 41) % 360, 0), piecesRoot);
                    debris.name = "Breach rubble"; debris.transform.localScale = new Vector3(0.9f, 0.3f, 0.9f);
                    foreach (var col in debris.GetComponentsInChildren<Collider>()) Destroy(col);
                    breaches[k] = debris;
                }
            // Sis bulutları: canlı sis koyu, önizlemedeki sis soluk
            int smokeLabelsUsed = 0;
            for (int k = 0; k < 64; k++)
            {
                bool live = m.Smoke.Contains(k), ghost = !live && m.SmokePreview.Contains(k);
                if (!live && !ghost) { if (smokeClouds[k] != null) smokeClouds[k].SetActive(false); continue; }
                SmokeCloud(k, live ? 1f : 0.5f);
                if (!live) continue;
                if (smokeLabelsUsed == smokeLabels.Count) smokeLabels.Add(NewPaperLabel("Smoke tag", 14));
                var tag = smokeLabels[smokeLabelsUsed++];
                tag.SetAnchor(W(k % Baskin.N, k / Baskin.N) + Vector3.up * 0.75f);
                tag.Show(Loc.T("SİS"), graphiteColor, graphiteColor);
                labelGroups.Add(new[] { tag });
            }
            for (int i = smokeLabelsUsed; i < smokeLabels.Count; i++) smokeLabels[i].gameObject.SetActive(false);
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
                        piece = NewObjectPiece(o.K, x, y);
                        piece.Kind = o.K; piece.X = x; piece.Y = y; piece.name = $"{o.K} {x},{y}";
                        objects[k] = piece;
                    }
                    // Uçuş canlandırması tüpü gizler; durum tüpü hâlâ tutuyorsa yerine döner
                    if (!piece.gameObject.activeSelf) { piece.transform.SetPositionAndRotation(W(x, y), Quaternion.identity); piece.gameObject.SetActive(true); }
                    piece.SetHp(o.Hp, o.Max);
                    piece.SetWorldLabelsVisible(false);
                    // Basınçlı tüp ve kaynak kolu hasar almaz: can etiketi yok
                    if (o.K != "tup" && o.K != "kol" && (screenLabelPrefab == null || (o.K != "civ" && o.K != "cool")))
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
                    // Uçan (ya da çarpılıp kırılan) basınçlı tüp yerinde enkaz bırakmaz
                    if (piece.Kind != "tup" && lib.rubble != null && !rubble.ContainsKey(k)) rubble[k] = Instantiate(lib.rubble, W(x, y), Quaternion.Euler(0, (x * 37 + y * 53) % 360, 0), piecesRoot);
                }
                else if (m.Rubble.Contains(k) && lib.rubble != null && !rubble.ContainsKey(k))
                    rubble[k] = Instantiate(lib.rubble, W(x, y), Quaternion.identity, piecesRoot);
            }
            // Birimler
            var alive = new HashSet<int>();
            foreach (var u in B.Units)
            {
                // Düşen ekip kartları yerinde kalır; Core.UnitAt onları yürüyüş ve atıştan zaten çıkarır. Kaçışta tahliye
                // edilen uzman tahtadan kalkar.
                if (u.Out || (u.Dead && u.Side != 'p')) continue;
                alive.Add(u.Id);
                if (!units.TryGetValue(u.Id, out var p) || p == null)
                {
                    p = Instantiate(lib.Unit(u.Type), W(u.X, u.Y), Quaternion.Euler(0, u.Side == 'p' ? 45 : -135, 0), piecesRoot);
                    p.Kind = u.Type; p.name = $"{u.Type} #{u.Id}";
                    units[u.Id] = p;
                }
                // Yük asansörünün kabinindeki birim tahtada değil: parçası havuzda gizli bekler (kabin inince yeniden görünür),
                // etiketleri yukarıda zaten gizlendi
                if (u.Cab) { p.gameObject.SetActive(false); continue; }
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
                else if (u.Dead || m.Spent.Contains(u.Id) || u.Locked || u.Frozen || u.Id == m.Carrier)
                {
                    var state = PieceLabel(stateLabels, u.Id, "Unit state", 14);
                    state.SetAnchor(hpTag.transform.position - cam.transform.up * 0.30f);
                    // Kriyo hattında donan birim yürüyemez (kilit gibi); kenarı buz renginde
                    bool frozen = !u.Dead && !u.Locked && u.Frozen;
                    state.Show(u.Left ? Loc.T("ÇIKAMADI") : u.Dead ? Loc.T("DÜŞTÜ") : u.Locked ? Loc.T("KİLİTLİ") : frozen ? Loc.T("DONDU") : u.Id == m.Carrier ? Loc.T("YÜKLÜ") : Loc.T("TUR BİTTİ"), hpInk, frozen ? lib.hazardFreeze : hpInk);
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
                // Duvar karesindeki hedef (Madenci'nin dinamiti) bloğun üstünde çizilir
                var tile = W(k % Baskin.N, k / Baskin.N);
                frames[k].transform.position = tile + Vector3.up * (walls[k] ? WallTop + 0.01f : 0.03f);
                tint[k].transform.position = tile + Vector3.up * (walls[k] ? WallTop : 0.02f);
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
            var selected = B.Units.FirstOrDefault(u => u.Id == m.Selected && Baskin.OnBoard(u));
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
            // Sise kesilen niyet: gri kesik çizgi sisli kareye kadar, ucu yok. Söndürme bulutuna kesilen (niyetin kendi işareti; aynı
            // karede sis ve bulut varsa motorun seçtiği) aynı çizgi bulutun renginde (sis gri, bulut beyaz)
            foreach (var cut in m.SmokedArrows)
            {
                var arrow = Extra(extrasUsed++);
                var from = W(cut.fx, cut.fy) + Vector3.up * (cut.kind == "lob" ? 0.8f : 0.30f);
                var to = W(cut.tx, cut.ty) + Vector3.up * 0.30f;
                var dir = (to - from).normalized;
                float rise = cut.kind == "lob" ? RisePx(Mathf.Max(0.4f * Vector3.Distance(from, to) / PixelWorld, 90)) : cut.kind == "shoot" ? 0 : RisePx(20);
                arrow.Set(from + dir * 0.24f, to - dir * 0.18f, Alpha(cut.cloud ? lib.hazardCloudEdge : graphiteColor, cut.cloud ? 0.85f : 0.7f), true, PixelWorld * 3.4f, rise, 0, false);
            }
            for (int i = m.Arrows.Count; i < arrows.Count; i++) arrows[i].gameObject.SetActive(false);
            for (int i = shadowsUsed; i < arrowShadows.Count; i++) arrowShadows[i].gameObject.SetActive(false);
            for (int i = extrasUsed; i < extraArrows.Count; i++) extraArrows[i].gameObject.SetActive(false);
            // Olay katmanı en son: etiketleri birimlerin ve dolapların etiketlerine yol verir
            SyncHazards(B, m.Hazards);
            LayoutLabels();
        }

        // ------------------------------------------------------------------ bölge olayları
        /// <summary>
        /// Bölge olayının katmanı (<see cref="Baskin.Hazards"/>; Docs/BOLGE-OLAYLARI.md): kare işaretleri, turnike kenarları, tüp
        /// ve süzgeç hatları, kısa etiketler (Core çevirmiş gelir). Görsel dil (Okunurluk): bulut beyaz, sis gri, gaz sarı, kriyo
        /// camgöbeği, kaynak turuncu, yağ parlak siyah, asansör sarı çerçeve; kesik çizgi "bu tur sonunda olacak", titreyen
        /// çerçeve "bu tur sonunda vurur". Yalnız okur; olay yoksa (null ya da boş) her şey gizlenir.
        /// </summary>
        void SyncHazards(Baskin.BState B, Baskin.HazardView hv)
        {
            hazardFillsUsed = hazardMarksUsed = hazardStrokesUsed = hazardLabelsUsed = 0;
            hazardPulse.Clear(); hazardShown.Clear();
            if (hv != null)
            {
                float px = PixelWorld;
                var rails = new HashSet<int>(hv.Marks.Where(h => h.Kind != null && h.Kind.StartsWith("rail", StringComparison.Ordinal)).Select(h => Key(h.X, h.Y)));
                bool lit = hv.Marks.Any(h => h.Kind == "gas_lit");
                foreach (var h in hv.Marks)
                {
                    if (h.Kind == null || h.X < 0 || h.Y < 0 || h.X >= Baskin.N || h.Y >= Baskin.N) continue;
                    int k = Key(h.X, h.Y);
                    DrawHazard(B, h.Kind, k, px, rails);
                    if (!string.IsNullOrEmpty(h.Label)) HzLabel(k, h.Kind, h.Label, lit);
                }
                foreach (var e in hv.Edges) DrawGate(e, px);
                foreach (var l in hv.Lines) DrawHazardLine(l, px);
            }
            for (int i = hazardFillsUsed; i < hazardFills.Count; i++) hazardFills[i].gameObject.SetActive(false);
            for (int i = hazardMarksUsed; i < hazardMarks.Count; i++) hazardMarks[i].gameObject.SetActive(false);
            for (int i = hazardStrokesUsed; i < hazardStrokes.Count; i++) hazardStrokes[i].gameObject.SetActive(false);
            for (int i = hazardLabelsUsed; i < hazardLabels.Count; i++) hazardLabels[i].gameObject.SetActive(false);
        }

        /// <summary>Tek kare işareti: dolgu, çerçeve, tarama, halka ya da çizgiler (birden çok işaret aynı karede üst üste).</summary>
        void DrawHazard(Baskin.BState B, string kind, int k, float px, HashSet<int> rails)
        {
            switch (kind)
            {
                case "cloud":
                    HzFill(k, kind, Alpha(lib.hazardCloud, 0.78f));
                    HzMark(k, kind).SetFrame(0.9f, px * 2, lib.hazardCloudEdge);
                    break;
                case "cloud_next":
                    HzFill(k, kind, Alpha(lib.hazardCloud, 0.35f));
                    HzMark(k, kind).SetFrame(0.9f, px * 2.6f, lib.hazardCloudEdge, true);
                    break;
                case "nozzle":
                    HzMark(k, kind, 0.034f).SetCircle(0.15f, lib.hazardCloudEdge, px * 2.4f, false, true);
                    break;
                case "rail":
                case "rail_gap":
                case "rail_close":
                case "rail_open":
                    HzRail(k, kind, px, rails);
                    // Geçit yeşil; kayma turunda kapanacak geçit kırmızı titrer, açılacak uç kesik yeşil
                    if (kind == "rail_gap") HzMark(k, kind).SetFrame(0.84f, px * 2.4f, lib.good);
                    else if (kind == "rail_open") HzMark(k, kind).SetFrame(0.84f, px * 2.6f, lib.good, true);
                    else if (kind == "rail_close") Pulse(HzMark(k, kind)).SetFrame(0.84f, px * 3, lib.threat);
                    break;
                case "gas":
                    HzFill(k, kind, Alpha(lib.hazardGas, 0.22f));
                    HzMark(k, kind, 0.024f).SetHatch(0.94f, Alpha(lib.hazardGas, 0.6f), px * 2, false, 0.1f);
                    break;
                case "gas_next":
                    HzFill(k, kind, Alpha(lib.hazardGas, 0.1f));
                    HzMark(k, kind).SetFrame(0.9f, px * 2.4f, lib.hazardGas, true);
                    break;
                case "gas_lit":
                    HzFill(k, kind, Alpha(lib.hazardFlame, 0.3f));
                    HzMark(k, kind, 0.024f).SetHatch(0.94f, Alpha(lib.hazardFlame, 0.6f), px * 2, false, 0.1f);
                    Pulse(HzMark(k, kind)).SetFrame(0.9f, px * 2.6f, lib.hazardFlame);
                    break;
                case "leak":
                    HzMark(k, kind, 0.034f).SetCircle(0.15f, lib.hazardGas, px * 2.4f, false, true);
                    break;
                case "crack":
                case "crack_due":
                    HzMark(k, kind, 0.026f).SetLines(CrackStrokes, Alpha(lib.hazardCrack, 0.9f), px * 2.6f);
                    if (kind == "crack_due") Pulse(HzMark(k, kind)).SetFrame(0.9f, px * 2.8f, lib.threat, true);
                    break;
                case "stop":
                case "stop_cab":
                    // Durak sarı çift çerçeve; kabinin bulunduğu durak dolu ve taralı
                    if (kind == "stop_cab")
                    {
                        HzFill(k, kind, Alpha(lib.hazardLift, 0.28f));
                        HzMark(k, kind, 0.024f).SetHatch(0.86f, Alpha(lib.hazardLift, 0.55f), px * 2, true, 0.12f);
                    }
                    HzMark(k, kind).SetFrame(0.88f, px * 2.6f, lib.hazardLift, false, true);
                    break;
                case "ghost":
                    // Kabinle inecek birimin yeri: kopyalama hayaletinin kesik halkası, asansör renginde
                    HzMark(k, kind, 0.04f).SetCircle(0.3275f, Alpha(lib.hazardLift, 0.95f), 0.035f, true, true);
                    break;
                case "freeze":
                case "freeze_next":
                    // Buz dokusu: iki yönlü ince tarama; boşalma turunda kesik çerçeve titrer
                    HzFill(k, kind, Alpha(lib.hazardFreeze, kind == "freeze_next" ? 0.22f : 0.14f));
                    HzMark(k, kind, 0.024f).SetHatch(0.94f, Alpha(lib.hazardFreeze, 0.45f), px * 1.6f, true, 0.12f);
                    HzMark(k, kind, 0.025f).SetHatch(0.94f, Alpha(lib.hazardFreeze, 0.45f), px * 1.6f, false, 0.12f);
                    if (kind == "freeze_next") Pulse(HzMark(k, kind)).SetFrame(0.9f, px * 2.6f, lib.hazardFreeze, true);
                    break;
                case "hol":
                    HzFill(k, kind, Alpha(lib.hazardHall, 0.1f));
                    break;
                case "weld":
                    // Kaynak karesi: dolu turuncu, kıvılcım halkası titrer; kaideden kareye kol
                    HzFill(k, kind, Alpha(lib.hazardWeld, 0.32f));
                    HzMark(k, kind).SetFrame(0.88f, px * 2.6f, lib.hazardWeld);
                    Pulse(HzMark(k, kind, 0.034f)).SetCircle(0.2f, lib.hazardWeld, px * 2.2f, false, true);
                    WeldArm(B, k, kind, px);
                    break;
                case "weld_next":
                    HzMark(k, kind).SetFrame(0.88f, px * 2, Alpha(lib.hazardWeld, 0.55f), true);
                    break;
                case "live":
                    HzFill(k, kind, Alpha(lib.hazardLive, 0.38f));
                    HzMark(k, kind).SetFrame(0.9f, px * 2, lib.hazardLive);
                    break;
                case "dead":
                    HzFill(k, kind, Alpha(lib.hazardDead, 0.22f));
                    break;
                case "drain":
                case "drain_next":
                {
                    HzFill(k, kind, Alpha(lib.hazardDrain, 0.18f));
                    var ring = HzMark(k, kind, 0.032f);
                    ring.SetCircle(0.3f, lib.hazardDrain, px * 3);
                    if (kind == "drain_next") Pulse(ring);
                    HzMark(k, kind, 0.032f).SetCircle(0.15f, lib.hazardDrain, px * 2);
                    break;
                }
                case "tube_hot":
                    Pulse(HzMark(k, kind, 0.034f)).SetCircle(0.36f, lib.threat, px * 3);
                    break;
                case "oil":
                    // Parlak siyah: koyu dolgu ve iki kâğıt rengi parıltı çizgisi
                    HzFill(k, kind, Alpha(lib.hazardOil, 0.62f));
                    HzMark(k, kind, 0.026f).SetLines(OilGloss, Alpha(paperColor, 0.5f), px * 3);
                    break;
                case "oil_next":
                    HzFill(k, kind, Alpha(lib.hazardOil, 0.1f));
                    HzMark(k, kind).SetFrame(0.9f, px * 2.4f, Alpha(lib.hazardOil, 0.85f), true);
                    break;
                case "hose":
                    HzMark(k, kind, 0.032f).SetCircle(0.2f, lib.hazardOil, px * 3);
                    HzMark(k, kind, 0.032f).SetCircle(0.1f, lib.hazardOil, px * 2);
                    break;
                default:
                    // Bilinmeyen tür: kesik grafit çerçeve (yeni işaret türü eklenince kare boş kalmasın)
                    HzMark(k, kind).SetFrame(0.9f, px * 2, graphiteColor, true);
                    break;
            }
        }

        /// <summary>Tavan çatlağı: karenin köşegeninde kırık çizgi ve bir dal (yerel, kare merkezine göre).</summary>
        static readonly List<(Vector3, Vector3)> CrackStrokes = new List<(Vector3, Vector3)>
        {
            (new Vector3(-0.32f, 0, -0.22f), new Vector3(-0.12f, 0, -0.04f)), (new Vector3(-0.12f, 0, -0.04f), new Vector3(-0.02f, 0, -0.16f)),
            (new Vector3(-0.02f, 0, -0.16f), new Vector3(0.14f, 0, 0.06f)), (new Vector3(0.14f, 0, 0.06f), new Vector3(0.3f, 0, 0.02f)),
            (new Vector3(0.14f, 0, 0.06f), new Vector3(0.08f, 0, 0.27f)),
        };
        /// <summary>Yağın parıltısı: iki kısa çapraz çizgi.</summary>
        static readonly List<(Vector3, Vector3)> OilGloss = new List<(Vector3, Vector3)>
        {
            (new Vector3(-0.27f, 0, 0.1f), new Vector3(-0.08f, 0, 0.27f)), (new Vector3(0.06f, 0, -0.22f), new Vector3(0.2f, 0, -0.09f)),
        };

        /// <summary>Olay işaretinin konumu: duvar karesinde (arşiv rafının bloğu) bloğun üstünde.</summary>
        Vector3 HazardAt(int k, float height) => W(k % Baskin.N, k / Baskin.N) + Vector3.up * ((walls[k] ? WallTop - 0.012f : 0) + height);

        void HzFill(int k, string kind, Color color)
        {
            if (hazardFillsUsed == hazardFills.Count) hazardFills.Add(Flat("Hazard fill", Vector3.zero, 0.94f, lib.highlight).GetComponent<Renderer>());
            var fill = hazardFills[hazardFillsUsed++];
            // Karo vurgusunun (0,02) altında: yürüme ve isabet taraması olayın üstünde okunur
            fill.transform.position = HazardAt(k, 0.017f);
            fill.gameObject.SetActive(true);
            SetColor(fill, color);
            hazardShown.Add((kind, k, fill.gameObject));
        }

        RaidArrow HzMark(int k, string kind, float height = 0.03f)
        {
            if (hazardMarksUsed == hazardMarks.Count) hazardMarks.Add(Mark("Hazard mark", Vector3.zero, 0));
            var mark = hazardMarks[hazardMarksUsed++];
            mark.transform.position = HazardAt(k, height);
            mark.transform.localScale = Vector3.one;
            mark.gameObject.SetActive(true);
            hazardShown.Add((kind, k, mark.gameObject));
            return mark;
        }

        /// <summary>Dünya uçlarıyla çizilen ok ya da şerit (turnike, tüp hattı, süzgeç çekişi, kaynak kolu).</summary>
        RaidArrow HzStroke(string kind, int k)
        {
            if (hazardStrokesUsed == hazardStrokes.Count) hazardStrokes.Add(NewArrow("Hazard stroke", lib.tileMarks));
            var stroke = hazardStrokes[hazardStrokesUsed++];
            stroke.gameObject.SetActive(true);
            hazardShown.Add((kind, k, stroke.gameObject));
            return stroke;
        }

        RaidArrow Pulse(RaidArrow mark) { hazardPulse.Add(mark.transform); return mark; }

        /// <summary>Arşiv rafının rayı: rayın yönünde iki grafit şerit (komşu ray karesinden yön bulunur).</summary>
        void HzRail(int k, string kind, float px, HashSet<int> rails)
        {
            int x = k % Baskin.N;
            bool across = (x > 0 && rails.Contains(k - 1)) || (x < Baskin.N - 1 && rails.Contains(k + 1));
            var along = (across ? Vector3.right : Vector3.forward) * 0.47f;
            var side = (across ? Vector3.forward : Vector3.right) * 0.16f;
            HzMark(k, kind, 0.026f).SetLines(new List<(Vector3, Vector3)> { (-along + side, along + side), (-along - side, along - side) }, Alpha(lib.hazardRail, 0.85f), px * 2.4f);
        }

        /// <summary>Kaynak kolu: kaideden (kaynak karesinin komşusundaki "kol") kareye kalın turuncu kol.</summary>
        void WeldArm(Baskin.BState B, int k, string kind, float px)
        {
            int x = k % Baskin.N, y = k / Baskin.N;
            foreach (var d in Baskin.Dirs)
            {
                int bx = x + d[0], by = y + d[1];
                if (bx < 0 || by < 0 || bx >= Baskin.N || by >= Baskin.N || Baskin.CellAt(B, bx, by).O?.K != "kol") continue;
                HzStroke(kind, k).Set(W(bx, by) + Vector3.up * 0.3f, W(x, y) + Vector3.up * 0.1f, lib.hazardWeld, false, px * 6, 0, 0, false);
                return;
            }
        }

        /// <summary>Turnike kenarı: kenar boyunca soluk kol ve izinli yönde kısa ok (iki yöne açık geçitte çift uçlu); bu tur
        /// sonunda dönecekse kesik.</summary>
        void DrawGate(Baskin.HazardEdge e, float px)
        {
            int k = Key(e.X, e.Y);
            var inside = W(e.X, e.Y); var outside = W(e.X2, e.Y2);
            var mid = (inside + outside) * 0.5f + Vector3.up * 0.05f;
            var outward = (outside - inside).normalized;
            var along = Vector3.Cross(Vector3.up, outward);
            const float reach = 0.24f;
            HzStroke("edge", k).Set(mid - along * 0.3f, mid + along * 0.3f, Alpha(lib.hazardGate, 0.45f), false, px * 2, 0, 0, false);
            if (e.Kind == "both")
            {
                HzStroke("edge", k).Set(mid, mid + outward * reach, lib.hazardGate, e.Soon, px * 3, 0, 0, true, px * 10);
                HzStroke("edge", k).Set(mid, mid - outward * reach, lib.hazardGate, e.Soon, px * 3, 0, 0, true, px * 10);
                return;
            }
            // in: dışarıdan içeri (X2, Y2 → X, Y); out: içeriden dışarı
            var sign = e.Kind == "in" ? -1f : 1f;
            HzStroke("edge", k).Set(mid - outward * (reach * sign), mid + outward * (reach * sign), lib.hazardGate, e.Soon, px * 3.2f, 0, 0, true, px * 12);
        }

        /// <summary>Hat: tüpün uçuş hattı (tube soluk, tube_hot parlak) ya da süzgecin çekişi (pull); Hit: ucu kırmızı.</summary>
        void DrawHazardLine(Baskin.HazardLine l, float px)
        {
            int k = Key(l.X, l.Y);
            if (l.X == l.X2 && l.Y == l.Y2) return;
            var a = W(l.X, l.Y); var b = W(l.X2, l.Y2);
            var dir = (b - a).normalized;
            if (l.Kind == "pull")
            {
                var from = a + Vector3.up * 0.08f + dir * 0.22f; var to = b + Vector3.up * 0.08f - dir * 0.18f;
                if (!l.Hit) { HzStroke(l.Kind, k).Set(from, to, lib.hazardDrain, false, px * 3.4f, 0, 0, true, px * 13); return; }
                HzStroke(l.Kind, k).Set(from, to - dir * 0.12f, lib.hazardDrain, false, px * 3.4f, 0, 0, false);
                HzStroke(l.Kind, k).Set(to - dir * 0.18f, to, lib.threat, false, px * 3.4f, 0, 0, true, px * 13);
                return;
            }
            bool hot = l.Kind == "tube_hot";
            var start = a + Vector3.up * 0.035f + dir * 0.3f;
            var end = b + Vector3.up * 0.035f + dir * (l.Hit ? -0.2f : 0.4f);
            HzStroke(l.Kind, k).Set(start, end, Alpha(lib.hazardTube, hot ? 1 : 0.42f), false, px * (hot ? 6 : 5), 0, 0, false);
            // Hattaki ilk birim: ucu kırmızı (soğuk tüpte soluk)
            if (l.Hit) HzStroke(l.Kind, k).Set(end - dir * 0.08f, end + dir * 0.14f, Alpha(lib.threat, hot ? 1 : 0.5f), false, px * (hot ? 4 : 3.4f), 0, 0, true, px * 13);
        }

        /// <summary>İşaretin kısa etiketi (Core çevirmiş gelir): "SİS" kâğıdı gibi karenin üstünde; tehlikede kırmızı, gelecekte kesik kenar.</summary>
        void HzLabel(int k, string kind, string text, bool lit)
        {
            if (hazardLabelsUsed == hazardLabels.Count) hazardLabels.Add(NewPaperLabel("Hazard tag", 14));
            var tag = hazardLabels[hazardLabelsUsed++];
            bool danger = kind == "rail_close" || kind == "gas_lit" || kind == "crack_due" || kind == "tube_hot" || kind == "weld" || kind == "drain_next" || (kind == "leak" && lit);
            tag.SetAnchor(HazardAt(k, 0.75f));
            tag.Show(text, danger ? lib.threat : graphiteColor, danger ? lib.threat : HazardInk(kind), kind.EndsWith("_next", StringComparison.Ordinal));
            labelGroups.Add(new[] { tag });
            hazardShown.Add((kind, k, tag.gameObject));
        }

        /// <summary>Olay türünün mürekkebi (etiket kenarı, patlama halkası).</summary>
        Color HazardInk(string kind) => kind switch
        {
            "cloud" or "cloud_next" or "nozzle" => lib.hazardCloudEdge,
            "rail" or "rail_gap" or "rail_close" or "rail_open" => lib.hazardRail,
            "gas" or "gas_next" or "leak" => lib.hazardGas,
            "gas_lit" => lib.hazardFlame,
            "crack" or "crack_due" => lib.hazardCrack,
            "stop" or "stop_cab" or "ghost" => lib.hazardLift,
            "freeze" or "freeze_next" => lib.hazardFreeze,
            "hol" => lib.hazardHall,
            "weld" or "weld_next" => lib.hazardWeld,
            "live" => lib.hazardLive,
            "dead" => lib.hazardDead,
            "drain" or "drain_next" => lib.hazardDrain,
            "tube_hot" => lib.hazardTube,
            "oil" or "oil_next" or "hose" => lib.hazardOil,
            _ => graphiteColor,
        };

        /// <summary>Çizilen olay katmanından bu türleri gizler (key &lt; 0: bütün kareler); sonraki Sync yeniden çizer.</summary>
        void HideHazards(int key, params string[] kinds)
        {
            foreach (var (kind, k, go) in hazardShown)
                if (go != null && (key < 0 || k == key) && Array.IndexOf(kinds, kind) >= 0) go.SetActive(false);
        }

        /// <summary>Bu türlerin görünen işaretlerinin kareleri (canlandırmada her kareye kısa patlama).</summary>
        List<int> HazardKeys(params string[] kinds) =>
            hazardShown.Where(s => s.go != null && s.go.activeSelf && Array.IndexOf(kinds, s.kind) >= 0).Select(s => s.key).Distinct().ToList();

        /// <summary>Yedek parçayla olay nesnesi: tüp ve kol kaidesi ölçeklenir; enkaz yığını kırık dolap enkazından kurulur.</summary>
        RaidPiece NewObjectPiece(string kind, int x, int y)
        {
            if (kind == "enkaz" && !lib.HasOwnPiece(kind) && lib.rubble != null)
            {
                var go = Instantiate(lib.rubble, W(x, y), Quaternion.Euler(0, (x * 37 + y * 53) % 360, 0), piecesRoot);
                go.transform.localScale = lib.FallbackScale(kind);
                // Tıklama (çarpıştırıcı → parça) ve kırılma canlandırması için parça bileşeni; gövdesi kökün kendisi
                return go.AddComponent<RaidPiece>();
            }
            var piece = Instantiate(lib.Object(kind), W(x, y), Quaternion.identity, piecesRoot);
            if (!lib.HasOwnPiece(kind)) piece.transform.localScale = lib.FallbackScale(kind);
            return piece;
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
            // Bu tur sonunda vuracak olay işaretleri titrer (alev almış gaz, çökecek tavan, kapanacak geçit, kaynak, emme, tüp)
            if (hazardPulse.Count > 0)
            {
                var beat = Vector3.one * (1 + 0.05f * Mathf.Sin(Time.unscaledTime * 7));
                foreach (var t in hazardPulse) if (t != null && t.gameObject.activeSelf) t.localScale = beat;
            }
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

        /// <summary>Kaçışta çıkışa yürüyen uzman tahtayı terk eder (yürüyüşten sonra; Sync onu kaldırır).</summary>
        public IEnumerator AnimateLeave(int unitId)
        {
            if (!units.TryGetValue(unitId, out var p) || p == null) yield break;
            Busy = true;
            yield return Leave(p, p.X, p.Y);
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
                    case "evac":
                        // Kaçış: uzman çıkıştan tahtayı terk eder (itilen ya da kaçış başlarken çıkışta duran)
                        if (units.TryGetValue(f.Unit, out var leaving)) yield return Leave(leaving, f.X, f.Y);
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
                            // Zincirli itme (yağda kayma, süzgeç, tüp): birimin sonraki itmesinin başladığı kare bu adımın sonu;
                            // sonraki itme yoksa son durumdaki yeri. Adım adım kayar.
                            var chained = fx.Skip(fxIndex).FirstOrDefault(n => n.K == "push" && n.Unit == f.Unit);
                            var u = after.Units.FirstOrDefault(z => z.Id == f.Unit);
                            if (chained != null || u != null)
                            {
                                var to = chained != null ? W(chained.X, chained.Y) : W(u.X, u.Y);
                                Sfx.Play(Cue.RaidPush, chained != null ? 0.7f : 1, Pan(to));
                                yield return Slide(pu, to, 0.18f);
                            }
                        }
                        break;
                    case "ev":
                        // Bölge olayı (Obj: alt tür)
                        yield return EventFx(after, f, popups++ * 0.04f);
                        break;
                    case "clouded":
                        // Söndürme bulutuna kesilen niyet ("smoked"un beyaz eşi)
                        Sfx.Play(Cue.RaidSmoked, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("BULUTA KESİLDİ"), graphiteColor, popups++ * 0.04f, lib.hazardCloudEdge));
                        yield return Wait(0.35f);
                        break;
                    case "ice":
                        // Buz kabuğu darbeyi karşıladı ("plate"in kriyo eşi)
                        if (units.TryGetValue(f.Unit, out var iced)) iced.Flash(lib.hazardFreeze);
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("BUZ KIRILDI"), graphiteColor, popups++ * 0.04f, lib.hazardFreeze));
                        Sfx.Play(Cue.RaidArmor, 1, Pan(W(f.X, f.Y)));
                        yield return Wait(enemyTurn ? 0.35f : 0.25f);
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
                    case "dynamite":
                        Sfx.Play(Cue.RaidDynamite, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Burst(W(f.X, f.Y) + Vector3.up * 0.3f, lib.threat, 1.1f, 0.25f));
                        yield return Wait(0.15f);
                        break;
                    case "breach":
                    {
                        int bk = Key(f.X, f.Y);
                        if (tileObjects[bk] != null) yield return SinkWall(tileObjects[bk]);
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("YIKILDI"), graphiteColor, 0));
                        break;
                    }
                    case "debris":
                        StartCoroutine(Burst(W(f.X, f.Y), graphiteColor, 0.7f, 0.2f));
                        yield return Wait(0.1f);
                        break;
                    case "smoke":
                        Sfx.Play(Cue.RaidSmoke, 1, Pan(W(f.X, f.Y)));
                        SmokeCloud(Key(f.X, f.Y), 1f);
                        StartCoroutine(Burst(W(f.X, f.Y), lib.smoke, 1.3f, 0.4f));
                        yield return Wait(0.3f);
                        break;
                    case "smoked":
                        Sfx.Play(Cue.RaidSmoked, 1, Pan(W(f.X, f.Y)));
                        StartCoroutine(Popup(W(f.X, f.Y), Loc.T("SİSE KESİLDİ"), graphiteColor, popups++ * 0.04f));
                        yield return Wait(0.35f);
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

        /// <summary>
        /// Bölge olayının efekti (Fx "ev", alt tür Obj; Baskin.Events.cs). Kısa yazı, patlama halkası ve bir ses; kabin birimi
        /// gizler ya da indirir, tüp hattında uçar, raf duvarı kaldırır. Olayın kalıcı sonucu (bulut, gaz, enkaz, karo) Sync'te
        /// çizilir. Tüp uçuşunda ve rafta Dx, Dy yön değil, son karedir.
        /// </summary>
        IEnumerator EventFx(Baskin.BState after, Baskin.Fx f, float delay)
        {
            int version = buildVersion;
            var at = W(f.X, f.Y);
            float pan = Pan(at);
            switch (f.Obj)
            {
                case "bulut":
                    Sfx.Play(Cue.RaidSmoke, 1, pan);
                    StartCoroutine(Burst(at, lib.hazardCloudEdge, 1.8f, 0.4f));
                    StartCoroutine(Popup(at, Loc.T("BULUT"), graphiteColor, delay, lib.hazardCloudEdge));
                    yield return Wait(0.3f);
                    break;
                case "bulut_dagil":
                    // Karesi yok: çizili bulut hemen söner
                    HideHazards(-1, "cloud");
                    Sfx.Play(Cue.RaidSmoke, 0.4f);
                    yield return Wait(0.15f);
                    break;
                case "raf":
                {
                    // (X, Y) geçit duvara döner, (Dx, Dy) duvar geçide: blok yükselir, öbür uç çöker (Sync karoları oturtur)
                    int close = Key(f.X, f.Y), open = Key(f.Dx, f.Dy);
                    Sfx.Play(Cue.RaidPush, 1, pan);
                    StartCoroutine(Popup(at, Loc.T("RAF"), graphiteColor, delay, lib.hazardRail));
                    if (InBoard(f.Dx, f.Dy) && walls[open] && tileObjects[open] != null) StartCoroutine(SinkWall(tileObjects[open]));
                    if (InBoard(f.X, f.Y) && !walls[close])
                    {
                        BuildTile(f.X, f.Y, "wall");
                        yield return RaiseWall(tileObjects[close]);
                    }
                    else yield return Wait(0.3f);
                    break;
                }
                case "raf_sikisti":
                    Sfx.Play(Cue.RaidHitObject, 1, pan);
                    StartCoroutine(Popup(at, Loc.T("RAF SIKIŞTI"), graphiteColor, delay, lib.hazardRail));
                    yield return Wait(0.3f);
                    break;
                case "gaz_dol":
                    Sfx.Play(Cue.RaidSmoke, 1, pan);
                    StartCoroutine(Burst(at, lib.hazardGas, 2f, 0.4f));
                    StartCoroutine(Popup(at, Loc.T("DOLUM"), graphiteColor, delay, lib.hazardGas));
                    yield return Wait(0.3f);
                    break;
                case "gaz_bosal":
                    HideHazards(-1, "gas", "gas_lit");
                    Sfx.Play(Cue.RaidSmoke, 0.6f, pan);
                    StartCoroutine(Popup(at, Loc.T("BOŞALDI"), graphiteColor, delay, lib.hazardGas));
                    yield return Wait(0.25f);
                    break;
                case "gaz_alev":
                    // Ateş buluta değdi: bir tur sonra parlar
                    Sfx.Play(Cue.RaidDynamite, 0.6f, pan);
                    StartCoroutine(Burst(at, lib.hazardFlame, 1.2f, 0.3f));
                    StartCoroutine(Popup(at, Loc.T("ALEV ALDI"), lib.threat, delay));
                    yield return Wait(0.3f);
                    break;
                case "gaz_parla":
                    Sfx.Play(Cue.RaidExplosion, 1, pan);
                    foreach (var key in HazardKeys("gas", "gas_lit")) StartCoroutine(Burst(W(key % Baskin.N, key / Baskin.N), lib.hazardFlame, 1.3f, 0.35f));
                    StartCoroutine(Burst(at, lib.hazardFlame, 2.4f, 0.4f));
                    StartCoroutine(Popup(at, Loc.T("PARLAMA"), lib.threat, delay));
                    HideHazards(-1, "gas", "gas_lit");
                    yield return Wait(0.45f);
                    break;
                case "catlak":
                    // Darbe çatlağı tetikledi: bir tur sonra çöker
                    Sfx.Play(Cue.RaidHitObject, 0.8f, pan);
                    StartCoroutine(Burst(at, lib.hazardCrack, 0.9f, 0.25f));
                    StartCoroutine(Popup(at, Loc.F("ÇÖKECEK {0}", 1), lib.threat, delay));
                    yield return Wait(0.25f);
                    break;
                case "tavan":
                    HideHazards(Key(f.X, f.Y), "crack", "crack_due");
                    Sfx.Play(Cue.RaidBreak, 1, pan);
                    StartCoroutine(Burst(at, lib.hazardCrack, 1.5f, 0.35f));
                    StartCoroutine(Burst(at + Vector3.up * 0.4f, graphiteColor, 0.8f, 0.25f));
                    StartCoroutine(Popup(at, Loc.T("ÇÖKME"), lib.threat, delay));
                    yield return Wait(0.35f);
                    break;
                case "toz":
                    HideHazards(Key(f.X, f.Y), "crack", "crack_due");
                    Sfx.Play(Cue.RaidBreak, 0.5f, pan);
                    StartCoroutine(Burst(at, graphiteColor, 1.1f, 0.3f));
                    StartCoroutine(Popup(at, Loc.T("TOZ"), graphiteColor, delay, lib.hazardCrack));
                    yield return Wait(0.25f);
                    break;
                case "kabin_kalk":
                    Sfx.Play(Cue.RaidPush, 1, pan);
                    StartCoroutine(Burst(at, lib.hazardLift, 1f, 0.3f));
                    StartCoroutine(Popup(at, Loc.T("KALKIŞ"), graphiteColor, delay, lib.hazardLift));
                    // Duraktaki birim kabine bindi: parçası yükselip gizlenir (havuzda kalır), etiketleri kalkar
                    if (f.Unit != 0 && units.TryGetValue(f.Unit, out var rider) && rider != null && rider.gameObject.activeSelf)
                    {
                        HideUnitLabels(f.Unit);
                        yield return Ride(rider, at, true);
                    }
                    else yield return Wait(0.3f);
                    break;
                case "kabin_in":
                    Sfx.Play(Cue.RaidPush, 1, pan);
                    StartCoroutine(Burst(at, lib.hazardLift, 1f, 0.3f));
                    StartCoroutine(Popup(at, Loc.T("İNİŞ"), graphiteColor, delay, lib.hazardLift));
                    // Yolcu (X, Y)'de iner (dolu durakta yanına)
                    if (f.Unit != 0 && units.TryGetValue(f.Unit, out var landed) && landed != null)
                    {
                        landed.X = f.X; landed.Y = f.Y;
                        yield return Ride(landed, at, false);
                    }
                    else yield return Wait(0.3f);
                    break;
                case "kabin_bekle":
                    Sfx.Play(Cue.RaidHitObject, 1, pan);
                    StartCoroutine(Popup(at, Loc.T("KABİN BEKLİYOR"), graphiteColor, delay, lib.hazardLift));
                    yield return Wait(0.3f);
                    break;
                case "donma":
                    Sfx.Play(Cue.RaidSmoke, 1, pan);
                    foreach (var key in HazardKeys("freeze", "freeze_next")) StartCoroutine(Burst(W(key % Baskin.N, key / Baskin.N), lib.hazardFreeze, 1f, 0.3f));
                    StartCoroutine(Popup(at, Loc.T("DONDURMA"), graphiteColor, delay, lib.hazardFreeze));
                    yield return Wait(0.3f);
                    break;
                case "buz":
                    if (f.Unit != 0 && units.TryGetValue(f.Unit, out var frozen) && frozen != null) frozen.Flash(lib.hazardFreeze, 0.6f);
                    Sfx.Play(Cue.RaidArmor, 0.7f, pan);
                    StartCoroutine(Popup(at, Loc.T("DONDU"), graphiteColor, delay, lib.hazardFreeze));
                    yield return Wait(0.2f);
                    break;
                case "turnike":
                    // Vardiya ya da tahliye (son iki tur hep dışarı): oklar sonraki Sync'te döner
                    Sfx.Play(Cue.RaidPush, 0.8f, pan);
                    StartCoroutine(Popup(at, after.Ev != null && after.Ev.Stop ? Loc.T("TAHLİYE MODU") : Loc.T("VARDİYA"), graphiteColor, delay, lib.hazardGate));
                    yield return Wait(0.3f);
                    break;
                case "kaynak":
                    Sfx.Play(Cue.RaidEmpBlast, 0.8f, pan);
                    StartCoroutine(Burst(at, lib.hazardWeld, 0.9f, 0.2f));
                    StartCoroutine(Burst(at, lib.hazardWeld, 1.4f, 0.35f));
                    StartCoroutine(Popup(at, Loc.T("KAYNAK"), lib.threat, delay, lib.hazardWeld));
                    yield return Wait(0.3f);
                    break;
                case "kol":
                    // Kola gelen darbe dönüşe bir adım ekler (kol hasar almaz)
                    if (objects.TryGetValue(Key(f.X, f.Y), out var arm) && arm != null) arm.Flash(lib.hazardWeld);
                    Sfx.Play(Cue.RaidHitObject, 1, pan);
                    StartCoroutine(Popup(at, Loc.T("KOL SARSILDI"), graphiteColor, delay, lib.hazardWeld));
                    yield return Wait(0.25f);
                    break;
                case "faz":
                    // Karesi yok: kanallar kısa bir kıvılcımla el değiştirir
                    Sfx.Play(Cue.RaidEmpBlast, 0.7f);
                    foreach (var key in HazardKeys("live", "dead")) StartCoroutine(Burst(W(key % Baskin.N, key / Baskin.N), lib.hazardLive, 0.7f, 0.25f));
                    yield return Wait(0.3f);
                    break;
                case "emme":
                    Sfx.Play(Cue.RaidPush, 1, pan);
                    StartCoroutine(Burst(at, lib.hazardDrain, 1.6f, 0.35f));
                    StartCoroutine(Popup(at, Loc.T("EMME"), graphiteColor, delay, lib.hazardDrain));
                    yield return Wait(0.3f);
                    break;
                case "islik":
                    // Vurulan tüp ıslık çalıyor: bir tur sonra uçar
                    if (objects.TryGetValue(Key(f.X, f.Y), out var hissing) && hissing != null) hissing.Flash(lib.threat, 0.6f);
                    Sfx.Play(Cue.RaidSmoke, 0.8f, pan);
                    StartCoroutine(Popup(at, Loc.T("ISLIK"), lib.threat, delay));
                    yield return Wait(0.3f);
                    break;
                case "tup_uc":
                {
                    // Tüp (X, Y)'den (Dx, Dy)'ye uçar ve tahtadan kalkar (Sync yerinde enkaz bırakmaz)
                    var end = W(f.Dx, f.Dy);
                    HideHazards(Key(f.X, f.Y), "tube", "tube_hot");
                    Sfx.Play(Cue.RaidDroneShot, 1, pan);
                    if (objects.TryGetValue(Key(f.X, f.Y), out var tube) && tube != null && tube.gameObject.activeSelf)
                        yield return Fly(tube, end + Vector3.up * 0.2f, Mathf.Max(0.18f, Vector3.Distance(at, end) * 0.07f));
                    if (version != buildVersion) yield break;
                    Sfx.Play(Cue.RaidHitObject, 1, Pan(end));
                    StartCoroutine(Burst(end, lib.hazardTube, 1f, 0.25f));
                    yield return Wait(0.1f);
                    break;
                }
                case "yag":
                    Sfx.Play(Cue.RaidBreak, 0.7f, pan);
                    StartCoroutine(Burst(at, lib.hazardOil, 1.6f, 0.4f));
                    StartCoroutine(Popup(at, Loc.T("YAĞ"), graphiteColor, delay, lib.hazardOil));
                    yield return Wait(0.35f);
                    break;
            }
        }

        static bool InBoard(int x, int y) => x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N;

        /// <summary>Birimin can, durum ve sıra etiketleri ile bayrak iğnesi (kabine binen birim tahtadan kalkar).</summary>
        void HideUnitLabels(int id)
        {
            foreach (var pool in new[] { hpLabels, stateLabels, orderLabels })
                if (pool.TryGetValue(id, out var label) && label != null) label.gameObject.SetActive(false);
            if (flagPins.TryGetValue(id, out var pin) && pin != null) pin.SetActive(false);
        }

        /// <summary>Yük asansörü: binen birim yükselip küçülerek kaybolur; inen birim yukarıdan durağa iner.</summary>
        IEnumerator Ride(RaidPiece p, Vector3 at, bool leaving)
        {
            int version = buildVersion;
            var high = at + Vector3.up * 0.9f;
            if (!leaving) { p.transform.position = high; p.gameObject.SetActive(true); }
            var s = p.Body.localScale;
            float time = stepTime * 1.1f / (FastMachines ? 3.125f : 1);
            for (float t = 0; t < 1; t += Time.deltaTime / time)
            {
                if (version != buildVersion || p == null) yield break;
                float e = Ease(t);
                p.transform.position = leaving ? Vector3.Lerp(at, high, e) : Vector3.Lerp(high, at, e);
                p.Body.localScale = s * (leaving ? 1 - e * 0.5f : 0.5f + e * 0.5f);
                yield return null;
            }
            if (version != buildVersion || p == null) yield break;
            p.Body.localScale = s;
            p.transform.position = at;
            if (leaving) p.gameObject.SetActive(false);
        }

        /// <summary>Uçan basınçlı tüp: uçuş yönüne yatar, hattında süzülür ve gizlenir.</summary>
        IEnumerator Fly(RaidPiece p, Vector3 to, float time)
        {
            int version = buildVersion;
            var a = p.transform.position; var r = p.transform.rotation;
            var flat = to - a; flat.y = 0;
            // Silindirin boyu (yerel yukarı) uçuş yönüne
            var lie = flat.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(flat.normalized) * Quaternion.Euler(90, 0, 0) : r;
            time /= FastMachines ? 3.125f : 1;
            for (float t = 0; t < 1; t += Time.deltaTime / time)
            {
                if (version != buildVersion || p == null) yield break;
                p.transform.position = Vector3.Lerp(a, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.15f);
                p.transform.rotation = Quaternion.Slerp(r, lie, Ease(Mathf.Min(1, t * 3)));
                yield return null;
            }
            if (version != buildVersion || p == null) yield break;
            p.gameObject.SetActive(false);
        }

        /// <summary>
        /// Makine evresinden sonra robotlar yeni yerlerine kare kare yürür. Core yeni planı kurarken robotu doğrudan taşır
        /// (yol yok); yol burada Core.Reachable'ın kuralıyla bulunur: duvar ve nesne kapalı, ekip üyesi geçilmez, robot geçilir.
        /// Robotlar yeni sıra numaralarıyla tek tek, aralarında kısa duraklamayla yürür. Uygulanan niyetlerin okları ve
        /// bayrakları yürüyüşten önce kalkar; can etiketi robotla gider. Sonra Sync her şeyi yerine oturtur. Hızlı kipte
        /// süreler makine evresi gibi kısalır. Yalnız sunum: durum okunur, yazılmaz.
        /// </summary>
        public IEnumerator AnimateMachineMoves(Baskin.BState after)
        {
            var walkers = MachineWalks(after);
            if (walkers.Count == 0) yield break;
            int version = buildVersion;
            Busy = true;
            HideExecutedIntents();
            // Süre üst sınırı: çok robot uzun yürüyorsa adımlar kısalır (en az machineMinStepSeconds)
            float speed = FastMachines ? 3.125f : 1;
            float step = machineStepSeconds / speed, pause = machinePauseSeconds / speed;
            int steps = walkers.Sum(w => w.path.Count);
            float total = steps * step + (walkers.Count - 1) * pause, limit = machineMovesMaxSeconds / speed;
            if (total > limit)
            {
                float k = limit / total;
                step = Mathf.Max(machineMinStepSeconds / speed, step * k);
                pause *= k;
            }
            for (int i = 0; i < walkers.Count; i++)
            {
                if (i > 0 && pause > 0) yield return new WaitForSeconds(pause);
                if (version != buildVersion) yield break;
                var (piece, id, path) = walkers[i];
                if (piece == null) continue;
                // Can ve UYANDI etiketi robotla kayar (yerleşim ofsetleri korunur)
                var tags = new List<(Transform tag, Vector3 offset)>();
                foreach (var pool in new[] { hpLabels, stateLabels })
                    if (pool.TryGetValue(id, out var tag) && tag != null && tag.gameObject.activeSelf)
                        tags.Add((tag.transform, tag.transform.position - piece.transform.position));
                foreach (var tile in path)
                {
                    var a = piece.transform.position; var b = W(tile.x, tile.y);
                    Sfx.Play(Cue.RaidStep, 0.6f, Pan(b));
                    for (float t = 0; t < 1; t += Time.deltaTime / step)
                    {
                        if (version != buildVersion || piece == null) yield break;
                        var ground = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, t));
                        piece.transform.position = ground + Vector3.up * Mathf.Sin(t * Mathf.PI) * machineStepHop;
                        foreach (var (tag, offset) in tags) tag.position = ground + offset;
                        yield return null;
                    }
                    if (version != buildVersion || piece == null) yield break;
                    piece.transform.position = b;
                    foreach (var (tag, offset) in tags) tag.position = b + offset;
                }
                piece.X = path[path.Count - 1].x; piece.Y = path[path.Count - 1].y;
            }
            Busy = false;
        }

        /// <summary>Tahtada gösterildiği kareden yeni karesine yürüyecek robot var mı (yeni oluşanlar Sync'te yerinde belirir)?</summary>
        public bool MachinesWillMove(Baskin.BState after) => MachineWalks(after).Count > 0;

        List<(RaidPiece piece, int id, List<(int x, int y)> path)> MachineWalks(Baskin.BState after)
        {
            var walkers = new List<(RaidPiece piece, int id, List<(int x, int y)> path)>();
            // Asansör kabinindeki makine tahtada değil (parçası gizli): yürümez
            foreach (var u in after.Units.Where(u => u.Side == 'e' && !u.Dead && !u.Cab).OrderBy(u => u.Order))
            {
                if (!units.TryGetValue(u.Id, out var p) || p == null || !p.gameObject.activeSelf) continue;
                var from = TileOf(p.transform.position);
                if (from.x == u.X && from.y == u.Y) continue;
                walkers.Add((p, u.Id, MachinePath(after, from, (u.X, u.Y))));
            }
            return walkers;
        }

        static (int x, int y) TileOf(Vector3 world) => (Mathf.RoundToInt(world.x + 3.5f), Mathf.RoundToInt(3.5f - world.z));

        /// <summary>Robotun en kısa yürüyüş yolu (başlangıç hariç, hedef dahil); yol bulunamazsa doğrudan hedef. Turnikenin
        /// kapalı yönü Core'daki gibi geçilmez (<see cref="Baskin.CanStep"/>).</summary>
        public static List<(int x, int y)> MachinePath(Baskin.BState B, (int x, int y) from, (int x, int y) to)
        {
            int start = Key(from.x, from.y), goal = Key(to.x, to.y);
            var previous = new Dictionary<int, int> { [start] = -1 };
            var queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count > 0 && !previous.ContainsKey(goal))
            {
                int k = queue.Dequeue();
                foreach (var d in Baskin.Dirs)
                {
                    int nx = k % Baskin.N + d[0], ny = k / Baskin.N + d[1];
                    if (nx < 0 || ny < 0 || nx >= Baskin.N || ny >= Baskin.N || previous.ContainsKey(Key(nx, ny))) continue;
                    var cell = Baskin.CellAt(B, nx, ny);
                    if (cell.T == "wall" || cell.O != null || !Baskin.CanStep(B, k % Baskin.N, k / Baskin.N, nx, ny)) continue;
                    var other = Baskin.UnitAt(B, nx, ny);
                    if (other != null && other.Side == 'p') continue;
                    previous[Key(nx, ny)] = k;
                    queue.Enqueue(Key(nx, ny));
                }
            }
            if (!previous.ContainsKey(goal)) return new List<(int x, int y)> { to };
            var path = new List<(int x, int y)>();
            for (int k = goal; k != start; k = previous[k]) path.Add((k % Baskin.N, k / Baskin.N));
            path.Reverse();
            return path;
        }

        /// <summary>Uygulanan (ya da iptal edilen) niyetlerin okları, gölgeleri, rozetleri, bayrakları ve iğneleri.</summary>
        void HideExecutedIntents()
        {
            activeIntentHighlight.gameObject.SetActive(false);
            foreach (var arrow in arrows) arrow.gameObject.SetActive(false);
            foreach (var arrow in arrowShadows) arrow.gameObject.SetActive(false);
            foreach (var arrow in extraArrows) arrow.gameObject.SetActive(false);
            foreach (var badge in badgeLabels) badge.gameObject.SetActive(false);
            foreach (var label in orderLabels.Values) label.gameObject.SetActive(false);
            foreach (var pin in flagPins.Values) pin.SetActive(false);
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

        /// <summary>Tahliye: uzman en yakın tahta kenarına doğru kayarak küçülür ve kaybolur; üstünde yeşil TAHLİYE.</summary>
        IEnumerator Leave(RaidPiece p, int x, int y)
        {
            int version = buildVersion;
            int edge = Mathf.Min(Mathf.Min(x, Baskin.N - 1 - x), Mathf.Min(y, Baskin.N - 1 - y));
            var away = x == edge ? new Vector3(-1, 0, 0) : Baskin.N - 1 - x == edge ? new Vector3(1, 0, 0) : y == edge ? new Vector3(0, 0, 1) : new Vector3(0, 0, -1);
            Sfx.Play(Cue.RaidStep, 1, Pan(p.transform.position));
            StartCoroutine(Popup(W(x, y), Loc.T("TAHLİYE"), lib.good, 0));
            var a = p.transform.position; var s = p.Body.localScale;
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 1.4f))
            {
                if (version != buildVersion || p == null) yield break;
                p.transform.position = a + away * (Ease(t) * 0.7f) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.2f;
                p.Body.localScale = s * (1 - t * 0.6f);
                yield return null;
            }
            if (version != buildVersion || p == null) yield break;
            p.Body.localScale = s;
            p.gameObject.SetActive(false);
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

        /// <summary>Duvar bloğunun üst yüzü (hedef çerçevesi ve vurgu duvar karesinde onun üstünde).</summary>
        const float WallTop = 0.56f;

        /// <summary>Karoyu türüne göre kurar (zemin, duvar, kablo); eski karo varsa yerine geçer.</summary>
        void BuildTile(int x, int y, string kind)
        {
            int k = Key(x, y);
            if (tileObjects[k] != null) Destroy(tileObjects[k]);
            var prefab = kind == "wall" ? lib.wall : kind == "cable" ? lib.cable : lib.floor;
            var go = Instantiate(prefab, W(x, y), Quaternion.identity, tilesRoot);
            go.name = $"Tile {x},{y}";
            var cell = go.GetComponent<RaidCell>() ?? go.AddComponent<RaidCell>();
            cell.X = x; cell.Y = y;
            tileObjects[k] = go; tileKinds[k] = kind;
            walls[k] = kind == "wall";
        }

        /// <summary>Sis bulutu: kareyi örten dört soluk küre (parçaları yarı gizler); <paramref name="strength"/> önizlemede yarım.</summary>
        void SmokeCloud(int k, float strength)
        {
            if (smokeClouds[k] == null)
            {
                var root = new GameObject($"Smoke {k % Baskin.N},{k / Baskin.N}");
                root.transform.SetParent(marksRoot, false);
                root.transform.position = W(k % Baskin.N, k / Baskin.N);
                // Bulut parçaları ve dolapları örtecek yükseklikte (0,3–0,6)
                var offsets = new[] { new Vector3(-0.16f, 0.32f, -0.1f), new Vector3(0.15f, 0.38f, 0.08f), new Vector3(0.02f, 0.6f, -0.02f), new Vector3(-0.05f, 0.3f, 0.18f) };
                for (int i = 0; i < offsets.Length; i++)
                {
                    var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(puff.GetComponent<Collider>());
                    puff.name = "Puff";
                    puff.transform.SetParent(root.transform, false);
                    puff.transform.localPosition = offsets[i];
                    puff.transform.localScale = new Vector3(0.6f, 0.44f, 0.6f) * (i == 2 ? 0.8f : 1f);
                    puff.GetComponent<Renderer>().sharedMaterial = lib.highlight;
                }
                smokeClouds[k] = root;
            }
            var cloud = smokeClouds[k];
            cloud.SetActive(true);
            foreach (var r in cloud.GetComponentsInChildren<Renderer>()) SetColor(r, Alpha(lib.smoke, lib.smoke.a * strength));
        }

        /// <summary>Yıkılan duvar: blok çöker (Sync ardından karoyu zemine çevirir).</summary>
        IEnumerator SinkWall(GameObject wall)
        {
            int version = buildVersion;
            var s = wall.transform.localScale;
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 1.1f))
            {
                if (version != buildVersion || wall == null) yield break;
                wall.transform.localScale = new Vector3(s.x * (1 + t * 0.1f), s.y * (1 - Ease(t) * 0.92f), s.z * (1 + t * 0.1f));
                yield return null;
            }
        }

        /// <summary>Kayan arşiv rafı: geçide gelen blok zeminden yükselir (karo zaten duvar olarak kuruldu).</summary>
        IEnumerator RaiseWall(GameObject wall)
        {
            int version = buildVersion;
            if (wall == null) yield break;
            var s = wall.transform.localScale;
            for (float t = 0; t < 1; t += Time.deltaTime / (stepTime * 1.1f))
            {
                if (version != buildVersion || wall == null) yield break;
                wall.transform.localScale = new Vector3(s.x, s.y * Mathf.Max(0.05f, Ease(t)), s.z);
                yield return null;
            }
            if (version == buildVersion && wall != null) wall.transform.localScale = s;
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

        /// <param name="edge">Kâğıdın kenarı (verilmezse yazı rengi): olay yazıları grafit mürekkep, olayın renginde kenar.</param>
        IEnumerator Popup(Vector3 at, string text, Color color, float delay, Color? edge = null)
        {
            int version = buildVersion;
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (version != buildVersion) yield break;
            var label = NewPaperLabel("Effect popup", 16);
            label.Show(text, color, edge ?? color);
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
