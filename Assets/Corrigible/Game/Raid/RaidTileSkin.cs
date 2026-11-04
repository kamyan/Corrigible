using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Duvar ve kablo kanalı karosunun giydirmesi (BASKIN-MEKANLARI Katman 3–4): komşu maskesine (<see cref="RaidTileMask"/>) göre
    /// çocukları açar, kapatır ve kitin malzemelerini atar. Yalnız SetActive, sharedMaterials ve çocuk dönüşü değiştirir; nesne
    /// üretmez, silmez (Edit modunda da güvenli: kit önizlemesi ve testler aynı yolu kullanır). Çocuklarda çarpıştırıcı yoktur:
    /// tıklama karonun kök çarpıştırıcısına düşer.
    /// <list type="bullet">
    /// <item>Duvar: <c>Body</c> (WallBlock: 0 ±X batı, 1 ±Z güney, 2 üst/alt/basamak), <c>BridgeE</c> (x + 1 komşusuyla arası),
    /// <c>BridgeN</c> (y − 1 komşusuyla arası), <c>ShelfGear</c> (kayar rafın el çarkı ve tekerlekleri). Köprü yalnız iki sabit duvar
    /// arasında ve kit köprü istiyorsa; raf duvarı köprü almaz. Karo başına en çok 3 etkin renderer.</item>
    /// <item>Kanal: <c>Hub</c> (göbeğin oluk tabanı) ve <c>Junction</c> (kesişen bakırı örten bağlantı plakası), <c>EdgeN/E/S/W</c>
    /// (kolu olmayan kenarın rayı ve tehlike şeridi), <c>ArmN/E/S/W</c> (kol: taban, iki ray, iki şerit, üç bakır), <c>Single</c>
    /// (komşusuz kanalın kuyusu). Düz geçişte (yalnız karşılıklı iki kol) Junction gizlenir, bakır kesintisiz okunur.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaidTileSkin : MonoBehaviour
    {
        public enum TileShape { Wall, Cable }

        [SerializeField] TileShape shape;
        [Tooltip("Kit verilmezse ya da kitin malzemesi boşsa kullanılan kit (Kit_default).")]
        [SerializeField] RaidRoomKit fallbackKit;

        [Header("Duvar")]
        [Tooltip("WallBlock mesh'i: alt kat 0,94, üst kat 0,92; malzemeler [batı, güney, üst].")]
        [SerializeField] MeshRenderer body;
        [Tooltip("Doğu komşusuyla (x + 1) arasındaki köprü.")]
        [SerializeField] MeshRenderer bridgeE;
        [Tooltip("Kuzey komşusuyla (y − 1) arasındaki köprü.")]
        [SerializeField] MeshRenderer bridgeN;
        [Tooltip("Kayar rafın uç levhasındaki el çarkı ve iki tekerlek (tek renderer); batı yüzünde, doğu–batı rayda güney yüzüne döner.")]
        [SerializeField] MeshRenderer shelfGear;

        [Header("Kanal")]
        [Tooltip("Göbeğin 0,6 × 0,6 oluk tabanı (tek karede kuyunun tabanı).")]
        [SerializeField] MeshRenderer hub;
        [Tooltip("Kesişen bakırı örten bağlantı plakası ve pabuçlar.")]
        [SerializeField] GameObject junction;
        [Tooltip("Kolu olmayan kenarlar, maske sırasıyla N, E, S, W.")]
        [SerializeField] GameObject[] edges = new GameObject[4];
        [Tooltip("Kollar, maske sırasıyla N, E, S, W (0. malzeme oluk tabanı).")]
        [SerializeField] MeshRenderer[] arms = new MeshRenderer[4];
        [Tooltip("Komşusu olmayan kanalın içeriği (kitin kuyusu; varsayılanda kısa bakır kangal).")]
        [SerializeField] GameObject single;

        public TileShape Shape => shape;
        public RaidRoomKit FallbackKit => fallbackKit;
        public MeshRenderer Body => body;
        public MeshRenderer ShelfGear => shelfGear;
        public MeshRenderer Hub => hub;
        public GameObject Junction => junction;
        public GameObject Single => single;
        /// <summary>Köprü: <see cref="RaidTileMask.E"/> ya da <see cref="RaidTileMask.N"/>.</summary>
        public MeshRenderer Bridge(int bit) => bit == RaidTileMask.E ? bridgeE : bit == RaidTileMask.N ? bridgeN : null;
        /// <summary>Kol ve kenar: 0 N, 1 E, 2 S, 3 W (<see cref="RaidTileMask.Sides"/> sırası).</summary>
        public MeshRenderer Arm(int side) => arms != null && side >= 0 && side < arms.Length ? arms[side] : null;
        public GameObject Edge(int side) => edges != null && side >= 0 && side < edges.Length ? edges[side] : null;

        /// <summary>Son giydirme (önizleme ve testler için); hiç giydirilmediyse maske −1.</summary>
        public int AppliedMask { get; private set; } = -1;
        public bool AppliedIsolated { get; private set; }
        public int AppliedShelfAxis { get; private set; }
        public RaidRoomKit AppliedKit { get; private set; }

        public void Apply(RaidTileMask.Dress dress, RaidRoomKit kit) => Apply(dress.Mask, dress.Isolated, dress.ShelfAxis, kit);

        /// <param name="mask">Aynı türden komşular (N 1, E 2, S 4, W 8); duvarda yalnız sabit duvar komşuları.</param>
        /// <param name="isolated">Duvar: 8 komşusunda duvar yok (kitin dik istifi).</param>
        /// <param name="shelfAxis">Duvar: kayar rafın ray yönü (<see cref="RaidTileMask.AxisNS"/> / <see cref="RaidTileMask.AxisEW"/>); 0 raf değil.</param>
        /// <param name="kit">Bina kiti; null ise <see cref="FallbackKit"/>.</param>
        public void Apply(int mask, bool isolated, int shelfAxis, RaidRoomKit kit)
        {
            AppliedMask = mask; AppliedIsolated = isolated; AppliedShelfAxis = shelfAxis; AppliedKit = kit;
            if (shape == TileShape.Wall) ApplyWall(mask, isolated, shelfAxis, kit);
            else ApplyCable(mask, kit);
        }

        /// <summary>Canlandırma sırasında (blok çöker ya da yükselir) köprüleri gizler; sonraki <see cref="Apply"/> yeniden açar.</summary>
        public void HideBridges()
        {
            if (bridgeE != null) bridgeE.gameObject.SetActive(false);
            if (bridgeN != null) bridgeN.gameObject.SetActive(false);
        }

        void ApplyWall(int mask, bool isolated, int shelfAxis, RaidRoomKit kit)
        {
            bool shelf = shelfAxis != RaidTileMask.AxisNone;
            // Unity nesnesinde ?. sahte null'u görmez: açık denetim
            RaidRoomKit k = kit != null ? kit : null, f = fallbackKit != null ? fallbackKit : null;
            var top = Pick(k != null ? k.wallTop : null, f != null ? f.wallTop : null);
            var west = Pick(k != null ? k.wallWest : null, f != null ? f.wallWest : null);
            var south = Pick(k != null ? k.wallSouth : null, f != null ? f.wallSouth : null);
            // Ayrık blok kitin dik istifi (yoksa aynı kitin duvarı); raf duvarı her zaman arşiv rafının kendisi
            var bodyWest = isolated && !shelf ? Pick(k != null ? k.columnWest : null, k != null ? k.wallWest : null, f != null ? f.columnWest : null, west) : west;
            var bodySouth = isolated && !shelf ? Pick(k != null ? k.columnSouth : null, k != null ? k.wallSouth : null, f != null ? f.columnSouth : null, south) : south;
            if (body != null) Assign(body, bodyWest, bodySouth, top);
            bool bridges = k != null ? k.bridges : f != null && f.bridges;
            SetBridge(bridgeE, bridges && !shelf && (mask & RaidTileMask.E) != 0, west, south, top);
            SetBridge(bridgeN, bridges && !shelf && (mask & RaidTileMask.N) != 0, west, south, top);
            if (shelfGear != null)
            {
                shelfGear.gameObject.SetActive(shelf);
                // Kuzey–güney rayda el çarkı batı yüzünde; doğu–batı rayda güney yüzünde (−X → −Z)
                shelfGear.transform.localRotation = shelfAxis == RaidTileMask.AxisEW ? Quaternion.Euler(0, -90, 0) : Quaternion.identity;
            }
        }

        static void SetBridge(MeshRenderer bridge, bool on, Material west, Material south, Material top)
        {
            if (bridge == null) return;
            bridge.gameObject.SetActive(on);
            // UnitBox yüz sırası: −X, −Z, +X, +Z, +Y, −Y
            if (on) Assign(bridge, west, south, west, south, top, south);
        }

        void ApplyCable(int mask, RaidRoomKit kit)
        {
            int armCount = 0;
            for (int i = 0; i < 4; i++) if ((mask & RaidTileMask.Sides[i].bit) != 0) armCount++;
            bool straight = mask == (RaidTileMask.N | RaidTileMask.S) || mask == (RaidTileMask.E | RaidTileMask.W);
            RaidRoomKit k = kit != null ? kit : null, f = fallbackKit != null ? fallbackKit : null;
            var floor = Pick(k != null ? k.channelFloor : null, f != null ? f.channelFloor : null);
            var well = Pick(k != null ? k.wellFloor : null, f != null ? f.wellFloor : null, floor);
            if (hub != null) { hub.gameObject.SetActive(true); SetFirst(hub, armCount == 0 ? well : floor); }
            if (junction != null) junction.SetActive(armCount > 0 && !straight);
            if (single != null) single.SetActive(armCount == 0);
            for (int i = 0; i < 4; i++)
            {
                bool on = (mask & RaidTileMask.Sides[i].bit) != 0;
                var edge = Edge(i);
                if (edge != null) edge.SetActive(!on);
                var arm = Arm(i);
                if (arm == null) continue;
                arm.gameObject.SetActive(on);
                if (on) SetFirst(arm, floor);
            }
        }

        static Material Pick(params Material[] options)
        {
            foreach (var m in options) if (m != null) return m;
            return null;
        }

        /// <summary>Malzemeleri sırayla atar; boş olan yuva olduğu gibi kalır.</summary>
        static void Assign(MeshRenderer r, params Material[] materials)
        {
            var current = r.sharedMaterials;
            if (current.Length != materials.Length) System.Array.Resize(ref current, materials.Length);
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] != null && current[i] != materials[i]) { current[i] = materials[i]; changed = true; }
            if (changed) r.sharedMaterials = current;
        }

        static void SetFirst(MeshRenderer r, Material m)
        {
            if (m == null) return;
            var current = r.sharedMaterials;
            if (current.Length == 0 || current[0] == m) return;
            current[0] = m;
            r.sharedMaterials = current;
        }
    }
}
