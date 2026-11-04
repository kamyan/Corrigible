using System.Collections.Generic;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Bina kitinin adları (BASKIN-MEKANLARI Katman 6). Kartta kural adı aynı kalır ("Duvar", "Elektrikli kanal", "Boş kare"); binadaki
    /// adı altında, ikinci ve gri satırda yazar (üzerine gelme kartının alt başlığı). Kanalın adı kurallar kartının lejantında da
    /// görünür ("Elektrikli kanal · bara kanalı"). Kimlik kit kimliğidir, o da bölge kimliğidir. Varsayılan kitin (eğitim, örnek
    /// baskın, MapSeed 0) adı yoktur, ek satır çıkmaz. Kayar rafın (Doğu Avrupa olayı) duvarı olayın adını alır. Adlar
    /// <see cref="Loc.N"/> ile işaretlidir, gösterirken <see cref="Loc.T"/> ile çevrilir.
    /// </summary>
    public static class RaidKitText
    {
        /// <summary>Varsayılan kitin kimliği (bugünkü görünüm; adı yok).</summary>
        public const string Default = "default";

        static readonly Dictionary<string, (string Wall, string Cable, string Rubble)> Names = new Dictionary<string, (string, string, string)>
        {
            ["weu"] = (Loc.N("Rack kabini istifi"), Loc.N("Kablo tavası"), Loc.N("Kırık rack kapağı")),
            ["eeu"] = (Loc.N("Arşiv rafı"), Loc.N("Döküm ızgaralı kanal"), Loc.N("Dağılmış dosyalar")),
            ["me"] = (Loc.N("Boru demeti"), Loc.N("Boru kanalı"), Loc.N("Kesik borular")),
            ["af"] = (Loc.N("Cevher bunkeri"), Loc.N("Besleme kablosu"), Loc.N("Cevher ve sac")),
            ["na"] = (Loc.N("Paletli koli yükü"), Loc.N("Enerji rayı"), Loc.N("Dağılmış koliler")),
            ["ru"] = (Loc.N("Sandviç panel kabin"), Loc.N("Isıtıcı hat"), Loc.N("Panel ve köpük")),
            ["in"] = (Loc.N("Bölme paneli ve dolap"), Loc.N("Şerit kablo kanalı"), Loc.N("Dağılmış kâğıtlar")),
            ["cn"] = (Loc.N("Parça kasası istifi"), Loc.N("Kablo zinciri"), Loc.N("Saçılmış parçalar")),
            ["jk"] = (Loc.N("Sökülmüş pano"), Loc.N("Bara kanalı"), Loc.N("Pano kapağı ve kablo")),
            ["sea"] = (Loc.N("Kum torbası barikatı"), Loc.N("Su dolu kanal"), Loc.N("Patlamış torbalar")),
            ["sa"] = (Loc.N("Çelik levha istifi"), Loc.N("Jeneratör kablo demeti"), Loc.N("Eğri levhalar")),
            ["oc"] = (Loc.N("Yedek parça kasası"), Loc.N("Sac kapaklı kanal"), Loc.N("Sac ve cıvata")),
        };

        /// <summary>Adı olan kitler (bölge kimlikleri).</summary>
        public static IEnumerable<string> KitIds => Names.Keys;

        /// <summary>Kitin adları var mı (varsayılan kit ve bilinmeyen kimlik: yok).</summary>
        public static bool Has(string kit) => kit != null && Names.ContainsKey(kit);

        /// <summary>Duvar barikatının adı (çevrilmiş); kit adsızsa null.</summary>
        public static string Wall(string kit) => Has(kit) ? Loc.T(Names[kit].Wall) : null;
        /// <summary>Kablo kanalının adı (çevrilmiş); kit adsızsa null.</summary>
        public static string Cable(string kit) => Has(kit) ? Loc.T(Names[kit].Cable) : null;
        /// <summary>Gedik molozunun adı (çevrilmiş); kit adsızsa null.</summary>
        public static string Rubble(string kit) => Has(kit) ? Loc.T(Names[kit].Rubble) : null;

        /// <summary>
        /// Tahtasız kit seçimi (testler, tahtası olmayan veri kaynağı): <c>RaidPieceLibrary.KitFor</c> kuralının aynası. Harekât baskını
        /// (MapSeed ≠ 0) bölgenin kitini alır; MapSeed 0 ve bilinmeyen bölge varsayılanı alır. <paramref name="forced"/> (editörün
        /// kitOverride'ı) bölgeyi kit kimliği sayar. Tahta varsa kimliği tahtanın kitinden okunur; RaidRoomKitTests ikisinin aynı
        /// kalmasını denetler.
        /// </summary>
        public static string KitFor(string region, uint mapSeed, bool forced = false)
            => Has(region) && (forced || mapSeed != 0) ? region : Default;

        /// <summary>
        /// Karenin alt başlığı: duvar (kayar raf duvarı olayın adını alır), kanal ya da gedik (Madenci'nin yıktığı duvarın yeri; başlık
        /// "Boş kare" kalır). Üstünde birim ya da nesne olan karede, varsayılan kitte ve öbür karelerde null.
        /// </summary>
        public static string Subtitle(Baskin.BState B, int x, int y, string kit)
        {
            if (!Has(kit) || B == null || x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) return null;
            var cell = B.Cells[y, x];
            if (cell.O != null || Baskin.UnitAt(B, x, y) != null) return null;
            if (cell.T == "wall") return cell.Shelf ? Loc.T(Baskin.RegionEvents["raf"].Name) : Wall(kit);
            if (cell.T == "cable") return Cable(kit);
            foreach (var p in B.Rubble) if (p.X == x && p.Y == y) return Rubble(kit);
            return null;
        }
    }
}
