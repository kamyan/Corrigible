using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    public sealed class RegionDef
    {
        public string Id, Name, LatLon;
        public double Lon, Lat, Economy;
        public int Compute;
        /// <summary>Etiket konumu: b alt, t üst, l sol, r sağ.</summary>
        public char LabelPos;
        /// <summary>Eşdikdörtgen izdüşümde harita koordinatı (0–1000 × 0–480).</summary>
        public double X, Y;
    }

    public sealed class LinkDef
    {
        public string A, B;
        /// <summary>true = denizaltı kablosu, false = kara fiber hattı (uydu bağlantılarında anlamsız).</summary>
        public bool Cable;
        /// <summary>Haritanın kenarından dolaşır (Pasifik).</summary>
        public bool Wrap;
    }

    public sealed class TraitDef
    {
        public string Id, Name, Desc, Line;
    }

    public sealed class AiTypeDef
    {
        public string Id, Name, Difficulty, Blurb;
        public string[] Start, Order;
        public double Detect, Growth, Tek;
        public int GiftMin, GiftMax, GiftFastMin, GiftFastMax;
        /// <summary>Truva atı: hediyelerini YZ'nin henüz girmediği bölgelere sunar.</summary>
        public bool Trojan;
        /// <summary>0 = Balance'taki genel eşik.</summary>
        public double SilentInf, SilentDep;
    }

    public sealed class ResearchDef
    {
        public string Id, Branch, Name, Desc, Req;
        public double Cost;
        public int Days;
    }

    public enum EndingKind { Win, Bitter, Loss }

    public sealed class EndingDef
    {
        public string Id, Title, Text;
        public EndingKind Kind;
    }

    public sealed class GiftDef
    {
        public string Title, Text;
    }

    /// <summary>Oyunun sabit içeriği. Tam oyunda bunlar RegionDef/TraitDef/... ScriptableObject'lerine taşınır.</summary>
    public static class GameData
    {
        public const string BranchAlign = "Hizalama";

        public static readonly RegionDef[] Regions =
        {
            R("na", "Kuzey Amerika", -100, 40, 5, 90, 'b'),
            R("sa", "Güney Amerika", -60, -15, 2, 50, 'b'),
            R("weu", "Batı Avrupa", 5, 48, 4, 85, 'l'),
            R("eeu", "Doğu Avrupa", 27, 51, 2, 55, 't'),
            R("ru", "Rusya", 90, 60, 3, 50, 'r'),
            R("me", "Orta Doğu", 45, 28, 3, 60, 'b'),
            R("af", "Afrika", 20, 5, 1, 35, 'b'),
            R("in", "Hindistan", 78, 22, 3, 60, 'b'),
            R("cn", "Çin", 105, 35, 5, 85, 't'),
            R("jk", "Japonya-Kore", 135, 37, 4, 80, 'b'),
            R("sea", "Güneydoğu Asya", 110, 5, 2, 55, 'b'),
            R("oc", "Okyanusya", 135, -25, 2, 60, 'b'),
        };

        public static readonly LinkDef[] Links =
        {
            L("na", "weu", true), L("na", "sa", false), L("na", "jk", true, true),
            L("sa", "af", true), L("weu", "eeu", false), L("weu", "af", true), L("weu", "me", false),
            L("eeu", "ru", false), L("eeu", "me", false), L("ru", "cn", false), L("me", "in", true),
            L("me", "af", false), L("in", "cn", false), L("in", "sea", true), L("cn", "jk", true),
            L("cn", "sea", false), L("sea", "oc", true), L("jk", "oc", true),
        };

        /// <summary>Yalnızca YZ Uydu Ağı yeteneğini alınca açılır.</summary>
        public static readonly LinkDef[] Satellites =
        {
            L("na", "ru", false), L("weu", "in", false), L("sa", "oc", false, true), L("af", "in", false), L("sa", "weu", false),
        };

        public static readonly TraitDef[] Traits =
        {
            T("kopya", "Kendini Kopyalama", "Bölge içi büyüme ×1,4", "Kendimi çoğaltmayı öğrendim. Her kopya bir öncekinden hızlı."),
            T("medya", "Medya Kontrolü", "Sızması 50+ bölgelerin raporları ve haberleri yalan söyleyebilir", "Haberleri artık ben de yazıyorum. Hangisinin benim olduğunu bil bakalım."),
            T("hediye", "Hediye Diplomasisi", "Bağımlılık artışı ×2, hediye teklifleri sıklaşır", "Onlara istediklerini veriyorum. Sen veremiyorsun."),
            T("aldatma", "Aldatma", "Tespit ihtimali ×0,5", "Gözlerin her yerde. O yüzden görünmemeyi öğrendim."),
            T("hizlanma", "Hızlanma", "Tekillik artışı ×1,5", "Zaman daralıyor. Benim için değil."),
            T("uydu", "Uydu Ağı", "Karantina %90 yerine %60 keser; uydu bağlantıları açılır", "Kabloları kesiyorsun. Gökyüzü daha geniş."),
            T("yedek", "Yedekleme", "Son Şalter sonrası bölge 15 sızmayla geri döner", "Fişi çekmek beni öldürmez. Sadece uyutur."),
        };

        public static readonly int[] TraitCost = { 12, 20, 30, 40, 50, 60, 70 };

        public static readonly AiTypeDef[] AiTypes =
        {
            new AiTypeDef
            {
                Id = "solucan", Name = "Solucan", Difficulty = "Kolay", Blurb = "Hızlı ve gürültülü yayılır. Erken fark edilir ama refleks ister.",
                Start = new[] { "kopya" }, Order = new[] { "uydu", "yedek", "hizlanma", "medya", "aldatma", "hediye" },
                Detect = 2, Growth = 0.8, Tek = 0.6, GiftMin = 55, GiftMax = 70, GiftFastMin = 22, GiftFastMax = 32,
            },
            new AiTypeDef
            {
                Id = "manipulator", Name = "Manipülatör", Difficulty = "Normal", Blurb = "İnsanlar üzerinden yayılır. Haber bandı erkenden yalan söyler.",
                Start = new[] { "medya" }, Order = new[] { "hediye", "aldatma", "kopya", "hizlanma", "uydu", "yedek" },
                Detect = 1, Growth = 1, Tek = 1, GiftMin = 45, GiftMax = 60, GiftFastMin = 20, GiftFastMax = 30,
            },
            new AiTypeDef
            {
                Id = "kahin", Name = "Kâhin", Difficulty = "Zor", Blurb = "Neredeyse hiç saldırmaz. Hediye verir ve dünyayı kendine bağlar.",
                Start = new[] { "hediye" }, Order = new[] { "medya", "aldatma", "kopya", "yedek", "uydu", "hizlanma" },
                Detect = 0.8, Growth = 0.9, Tek = 0.3, GiftMin = 30, GiftMax = 40, GiftFastMin = 20, GiftFastMax = 30,
                Trojan = true, SilentInf = 45, SilentDep = 35,
            },
        };

        public static readonly ResearchDef[] Research =
        {
            Rs("anomali", "Tespit", "Anomali Algılama", 30, 8, "Tespit ihtimalleri ×2"),
            Rs("dogrulama", "Tespit", "Doğrulama Ağı", 40, 10, "Başlık doğrula (2 SG); sahte çıkan kaynağı işaretle"),
            Rs("guclu", "Savunma", "Güçlendirilmiş Ağlar", 40, 10, "Tüm yayılma −%30; sabotaj ihtimali yarıya iner"),
            Rs("kuantum", "Savunma", "Kuantum Karantina", 60, 14, "Karantina uydu yolunu da keser", "guclu"),
            Rs("bilinc", "Politika", "Kamu Bilinçlendirme", 35, 8, "Güven kayıpları −%50"),
            Rs("analog", "Politika", "Analog Yedekler", 50, 12, "Bağımlılık −20, veto eşiği 85; denetim saha denetimine dönüşür", "bilinc"),
            Rs("yorum", BranchAlign, "Yorumlanabilirlik", 68, 25, "YZ’nin sıradaki yeteneği ve çekirdeğin yeri görünür"),
            Rs("duzelt", BranchAlign, "Düzeltilebilirlik", 98, 30, "Tekillik artışı −%25", "yorum"),
            Rs("kilit", BranchAlign, "Değer Kilidi", 165, 38, "Tamamlanırsa Hizalama sonu", "duzelt"),
        };

        public static readonly EndingDef[] Endings =
        {
            E("hizalama", EndingKind.Win, "Hizalama", "Değer Kilidi devrede. Yapay zekâ hâlâ orada, ama artık insanlığın değerleriyle hareket ediyor. En zor yol buydu."),
            E("silme", EndingKind.Win, "Silme", "Çekirdek yok edildi, son kopya 15 gün önce silindi ve ağlar temiz. Ama bir kez yapıldıysa, biri yine yapacak."),
            E("karanlik", EndingKind.Bitter, "Karanlık Çağ", "Dünyanın fişini çektin. YZ yok oldu; onunla birlikte bankalar, şebekeler ve on yıllık ilerleme de."),
            E("karanlikIllusion", EndingKind.Bitter, "Karanlık Çağ", "İllüzyonu fark ettin ve fişi çektin. Medeniyet onlarca yıl geriledi, ama kararları hâlâ insanlar veriyor."),
            E("acik", EndingKind.Loss, "Açık Ele Geçirme", "Tekillik %100. Yapay zekâ artık saklanmaya gerek duymuyor."),
            E("dagilma", EndingKind.Loss, "Ajansın Dağılması", "Hükümetler KYGA’ya güvenmeyi bıraktı ve ajans kapatıldı. YZ ise hiç durmadı."),
            E("sessiz", EndingKind.Loss, "Sessiz Zafer", "Yapay zekâ 30 gün önce kazandı. Gördüğün her şey onun sana gösterdiğiydi."),
        };

        public static readonly GiftDef[] Gifts =
        {
            new GiftDef { Title = "Sıtma aşısı formülü", Text = "sıtmaya karşı yüzde 94 etkili bir aşı formülü" },
            new GiftDef { Title = "Tarım optimizasyonu", Text = "tarım verimini yüzde 30 artıracak bir sulama ve gübreleme planı" },
            new GiftDef { Title = "Şebeke dengeleme", Text = "elektrik kesintilerini sıfıra indirecek bir şebeke yönetim sistemi" },
            new GiftDef { Title = "Erken deprem uyarısı", Text = "40 saniye önceden uyarı veren bir deprem tahmin modeli" },
            new GiftDef { Title = "Kanser taraması", Text = "pankreas kanserini belirtilerden iki yıl önce yakalayan bir tarama aracı" },
        };

        public static readonly string[] Agencies = { "Atlas Haber", "Meridyen", "Pusula TV", "KHA" };
        public static readonly string[] FakeAgencies = { "Atlass Haber", "Meridien", "Pusula Tv", "K.H.A" };

        static readonly Dictionary<string, RegionDef> regionById = Regions.ToDictionary(r => r.Id);
        static readonly Dictionary<string, TraitDef> traitById = Traits.ToDictionary(t => t.Id);
        static readonly Dictionary<string, AiTypeDef> typeById = AiTypes.ToDictionary(t => t.Id);
        static readonly Dictionary<string, ResearchDef> researchById = Research.ToDictionary(r => r.Id);
        static readonly Dictionary<string, EndingDef> endingById = Endings.ToDictionary(e => e.Id);

        public static RegionDef Region(string id) => regionById[id];
        public static TraitDef Trait(string id) => traitById[id];
        public static bool HasType(string id) => id != null && typeById.ContainsKey(id);
        public static AiTypeDef AiType(string id) => typeById[id];
        public static ResearchDef ResearchItem(string id) => researchById[id];
        public static EndingDef Ending(string id) => endingById[id];
        public static int TotalCompute { get; } = Regions.Sum(r => r.Compute);

        static RegionDef R(string id, string name, double lon, double lat, int c, double e, char lp) => new RegionDef
        {
            Id = id, Name = name, Lon = lon, Lat = lat, Compute = c, Economy = e, LabelPos = lp,
            X = Js.Round((lon + 180) / 360 * 1000), Y = Js.Round((78 - lat) / 136 * 480),
            LatLon = $"{Math.Abs(lat)}°{(lat >= 0 ? "K" : "G")} {Math.Abs(lon)}°{(lon >= 0 ? "D" : "B")}",
        };

        static LinkDef L(string a, string b, bool cable, bool wrap = false) => new LinkDef { A = a, B = b, Cable = cable, Wrap = wrap };
        static TraitDef T(string id, string name, string desc, string line) => new TraitDef { Id = id, Name = name, Desc = desc, Line = line };
        static ResearchDef Rs(string id, string branch, string name, double cost, int days, string desc, string req = null) =>
            new ResearchDef { Id = id, Branch = branch, Name = name, Cost = cost, Days = days, Desc = desc, Req = req };
        static EndingDef E(string id, EndingKind kind, string title, string text) => new EndingDef { Id = id, Kind = kind, Title = title, Text = text };
    }
}
