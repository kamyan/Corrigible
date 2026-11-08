using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Bina kiti (BASKIN-MEKANLARI Katman 3–5): baskın tahtasının duvar barikatı, kablo kanalının içi ve gedik molozu bölgenin
    /// binasına göre. Kurallı işaretler (kanalın kenar şeridi ve bakırı, kayar rafın el çarkı) kitten bağımsızdır; kit yalnız
    /// malzemeleri ve süsü değiştirir. Kiti köprü seçer (<see cref="RaidPieceLibrary.KitFor"/> → <see cref="RaidBoard3D.SetDress"/>);
    /// karolara <see cref="RaidTileSkin.Apply"/> giydirir. Boş bırakılan malzeme karonun varsayılan kitteki malzemesine düşer.
    /// Varlıklar <c>Assets/Corrigible/Settings/Kits/Kit_&lt;id&gt;.asset</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Raid Room Kit", fileName = "Kit_")]
    public sealed class RaidRoomKit : ScriptableObject
    {
        [Tooltip("Kitin kimliği: bölge kimliği (weu, eeu, me…) ya da varsayılan kit için \"default\".")]
        public string id = "default";
        [Tooltip("Bu kiti kullanan bölgeler (boşsa yalnız kimliğiyle aynı bölge).")]
        public string[] regions = new string[0];

        [Header("Duvar barikatı (Katman 3)")]
        [Tooltip("Blokların üst yüzü (ayrık blokta da).")]
        public Material wallTop;
        [Tooltip("Batı yüzü (kameranın gördüğü −X; doğu yüzü de bu).")]
        public Material wallWest;
        [Tooltip("Güney yüzü (kameranın gördüğü −Z; kuzey yüzü de bu).")]
        public Material wallSouth;
        [Tooltip("Ayrık blok (8 komşusunda duvar yok): kitin dik istifinin batı yüzü.")]
        public Material columnWest;
        [Tooltip("Ayrık bloğun güney yüzü.")]
        public Material columnSouth;
        [Tooltip("Komşu iki sabit duvarın arasındaki boşluğa köprü parçası girer (dizi tek yapı gibi okunur). Varsayılan kitte kapalı: " +
                 "eğitim ve örnek baskın bugünkü görünümle çizilir.")]
        public bool bridges = true;

        [Header("Kablo kanalı (Katman 4)")]
        [Tooltip("Göbeğin ve kolların oluk tabanı.")]
        public Material channelFloor;
        [Tooltip("Komşusu olmayan tek kanal karesinin (kitin kuyusu) tabanı.")]
        public Material wellFloor;

        [Header("Gedik molozu (Katman 5)")]
        [Tooltip("Yıkılan duvarın yerinde kalan alçak moloz (süs: RaidPiece ve çarpıştırıcı yok). Boşsa kırık dolap enkazı basık ölçekle.")]
        public GameObject wallRubble;

        [Header("Revizyon 3 model bağları")]
        public RaidModelMesh wallModel;
        public RaidModelMesh isolatedModel;
        public RaidModelMesh bridgeModel;
        public RaidPiece civilianModel;
        [Tooltip("Giriş satırındaki süs kapısı; oyun karesi veya çarpıştırıcısı değildir.")]
        public GameObject entryDoor;

        /// <summary>Kit bu bölgenin mi (kimliği ya da bölge listesi).</summary>
        public bool Serves(string region)
        {
            if (string.IsNullOrEmpty(region)) return false;
            if (id == region) return true;
            if (regions != null) foreach (var r in regions) if (r == region) return true;
            return false;
        }
    }
}
