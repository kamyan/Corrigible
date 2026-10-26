using Corrigible.Core;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sığınak odasındaki sabit yazılar (tabelalar, plakalar, afiş, pano notları). Oda kurucusu bu kaynak metinleri
    /// <see cref="DeskLocText"/> ile sahneye koyar; <c>Loc.N</c> çeviri aracının onları tabloya almasını sağlar.
    /// Değişen içerik (sayaç değerleri, teleks satırları, oda kartları, CRT) kendi görünümlerinde <c>Loc</c> ile yazılır.
    /// </summary>
    public static class SiginakRoomWords
    {
        public static readonly string WallSign = Loc.N("KYGA / SIĞINAK 07");
        public static readonly string PosterLine1 = Loc.N("AĞA");
        public static readonly string PosterLine2 = Loc.N("BAĞLANMA");
        public static readonly string PosterLine3 = Loc.N("ANALOG KAL");
        public static readonly string PosterFooter = Loc.N("KYGA · DOKTRİN 1");
        public static readonly string FreedTitle = Loc.N("KURTARILAN BÖLGELER");
        public static readonly string Missing = Loc.N("KAYIP");
        public static readonly string MissingAPlace = Loc.N("Lyon · 2 yaş");
        public static readonly string CrateSupplies = Loc.N("ERZAK");
        public static readonly string CrateMaterials = Loc.N("MALZEME");
        public static readonly string ShelfPlate = Loc.N("SIĞINAK 07 · KESİT");
        public static readonly string TelexPlate = Loc.N("TELEKS");
        public static readonly string TrayPlate = Loc.N("EVRAK");
        public static readonly string CrtPlate = Loc.N("SAHA YAYINI · K-07");
        public static readonly string KeyCommand = Loc.N("KOMUTA");
        public static readonly string KeyShelter = Loc.N("SIĞINAK");
        public static readonly string KeyMissions = Loc.N("GÖREVLER");
        public static readonly string KeyGuide = Loc.N("KILAVUZ");
        public static readonly string KeyDesk = Loc.N("MASA");
        public static readonly string SwitchPlate = Loc.N("VARDİYAYI BİTİR");
        public static readonly string SwitchHint = Loc.N("Enter · kapağı kaldır");
        public static readonly string SwitchCover = Loc.N("VARDİYA");
        public static readonly string CrtChannel = Loc.N("SAHA YAYINI · KANAL 07");
        public static readonly string CrtTitle = Loc.N("SAHA YAYINI");
        public static readonly string CrtPattern = Loc.N("K-07");
        public static readonly string TelexHeader = Loc.N("TELEKS · KYGA-07");
        public static readonly string FreeRegions = Loc.N("ÖZGÜR BÖLGE");
        public static readonly string Funds = Loc.N("FON");
        public static readonly string Settings = Loc.N("AYARLAR");
        public static readonly string SpaceKey = Loc.N("Boşluk");
        public static readonly string Skip = Loc.N("ATLA");
        public static readonly string Locked = Loc.N("KİLİTLİ");
        public static readonly string Found = Loc.N("BULUNDU");
        public static readonly string GoingUp = Loc.N("YUKARI ÇIKIYORUZ");

        // Üzerine gelme etiketleri (başlık + tek satır)
        public static readonly string TagShelter = Loc.N("Kesit maketi · odaları yönet");
        public static readonly string TagCommand = Loc.N("Dünya haritası · cepheyi planla");
        public static readonly string TagMissions = Loc.N("Görev fişi · baskın emri");
        public static readonly string TagGuide = Loc.N("Harekât emrini yeniden oku");
        public static readonly string TagDesk = Loc.N("Masaya dön");
        public static readonly string TagShift = Loc.N("Kapağı kaldır, kolu indir: bir gün geçer");
        public static readonly string TagTelex = Loc.N("Son emirler ve sonuçlar");
        public static readonly string TagTray = Loc.N("Brifing ve vardiya raporları");
        public static readonly string TagCrt = Loc.N("Yalnız baskın emriyle açılır. Emri görev fişinden ver (3).");

        // Kurtuluş haritası (Aşama 5): lejant, fiş başlıkları, ekip figürü
        public static readonly string LegendTitle = Loc.N("İŞARETLER");
        public static readonly string LegendOccupied = Loc.N("işgal");
        public static readonly string LegendFront = Loc.N("cephe");
        public static readonly string LegendCandidate = Loc.N("aday");
        public static readonly string LegendFree = Loc.N("özgür");
        public static readonly string LegendRetaken = Loc.N("geri alındı");
        public static readonly string LegendReady = Loc.N("hazır");
        public static readonly string LegendLocked = Loc.N("kilitli");
        public static readonly string SlipCost = Loc.N("BEDEL");
        public static readonly string SlipReward = Loc.N("ÖDÜL");
        public static readonly string OrderGiven = Loc.N("EMİR VERİLDİ");
        public static readonly string EnterKey = Loc.N("Enter");
        public static readonly string EscKey = Loc.N("Esc");
        public static readonly string ShelterTag = Loc.N("S-07");
        public static readonly string Injured = Loc.N("YARALI");
        public static readonly string TeamTitle = Loc.N("EKİP ALFA");

        // Sığınak (Aşama 6)
        public static readonly string TagRoom = Loc.N("Tıkla: oda kartı panoda açılır");
        public static readonly string OrderDone = Loc.N("EMİR UYGULANDI");
        public static readonly string RoomLiving = Loc.N("Yaşam alanı");
        public static readonly string RoomGenerator = Loc.N("Jeneratör");
        public static readonly string RoomWorkshop = Loc.N("Atölye");
        public static readonly string RoomInfirmary = Loc.N("Revir");
    }
}
