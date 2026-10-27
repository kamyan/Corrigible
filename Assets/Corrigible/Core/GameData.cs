using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    public sealed class RegionDef
    {
        public string Id;
        string name;
        public string Name { get => Loc.T(name); set => name = value; }
        public double Lon, Lat;
        /// <summary>Hesaplama gücü: taktik haritanın yerleşim seçimine katılır.</summary>
        public int Compute;
        /// <summary>Bölgenin kimliği: tahta özelliği (<c>Baskin.RegionTraits</c>) ve imza düşmanı (<c>Baskin.UnitTypes</c>).</summary>
        public string Trait, Enemy;
    }

    public sealed class LinkDef
    {
        public string A, B;
    }

    public sealed class EndingDef
    {
        public string Id;
        string title, text;
        public string Title { get => Loc.T(title); set => title = value; }
        public string Text { get => Loc.T(text); set => text = value; }
    }

    /// <summary>Oyunun sabit içeriği: bölgeler, komşuluklar ve harekâtın sonu.</summary>
    public static class GameData
    {
        public static readonly RegionDef[] Regions =
        {
            // Her bölgenin kendine özgü özelliği ve imza düşmanı (her düşman iki bölgede)
            R("na", Loc.N("Kuzey Amerika"), -100, 40, 5, "kisa", "havan"),
            R("sa", Loc.N("Güney Amerika"), -60, -15, 2, "takviye", "bomba"),
            R("weu", Loc.N("Batı Avrupa"), 5, 48, 4, "guvenli", "zdron"),
            R("eeu", Loc.N("Doğu Avrupa"), 27, 51, 2, "dar", "kilit"),
            R("ru", Loc.N("Rusya"), 90, 60, 3, "sogutma", "kalkan"),
            R("me", Loc.N("Orta Doğu"), 45, 28, 3, "sis", "bomba"),
            R("af", Loc.N("Afrika"), 20, 5, 1, "acik", "havan"),
            R("in", Loc.N("Hindistan"), 78, 22, 3, "kalabalik", "kalkan"),
            R("cn", Loc.N("Çin"), 105, 35, 5, "zirhli", "cogalt"),
            R("jk", Loc.N("Japonya-Kore"), 135, 37, 4, "sebeke", "kilit"),
            R("sea", Loc.N("Güneydoğu Asya"), 110, 5, 2, "fabrika", "cogalt"),
            R("oc", Loc.N("Okyanusya"), 135, -25, 2, "devriye", "zdron"),
        };

        /// <summary>Kara ve kablo bağlantıları: yeni cephe, özgür bir bölgeye komşu olmalı.</summary>
        public static readonly LinkDef[] Links =
        {
            L("na", "weu"), L("na", "sa"), L("na", "jk"),
            L("sa", "af"), L("weu", "eeu"), L("weu", "af"), L("weu", "me"),
            L("eeu", "ru"), L("eeu", "me"), L("ru", "cn"), L("me", "in"),
            L("me", "af"), L("in", "cn"), L("in", "sea"), L("cn", "jk"),
            L("cn", "sea"), L("sea", "oc"), L("jk", "oc"),
        };

        public static readonly EndingDef[] Endings =
        {
            new EndingDef { Id = "silme", Title = Loc.N("Silme"), Text = Loc.N("Çekirdek yok edildi, kopyalar temizlendi. Ama bir kez yapıldıysa, biri yine yapacak.") },
            new EndingDef { Id = "dagilma", Title = Loc.N("Dağılma"), Text = Loc.N("Hastaneler ve bankalar bizim baskınlarımızda karardı. Halk direnişe sırtını döndü; sığınağın kapısını artık kimse çalmıyor.") },
        };

        static readonly Dictionary<string, RegionDef> regionById = Regions.ToDictionary(r => r.Id);
        static readonly Dictionary<string, EndingDef> endingById = Endings.ToDictionary(e => e.Id);

        public static RegionDef Region(string id) => regionById[id];
        public static EndingDef Ending(string id) => endingById[id];

        static RegionDef R(string id, string name, double lon, double lat, int compute, string trait, string enemy) =>
            new RegionDef { Id = id, Name = name, Lon = lon, Lat = lat, Compute = compute, Trait = trait, Enemy = enemy };
        static LinkDef L(string a, string b) => new LinkDef { A = a, B = b };
    }
}
