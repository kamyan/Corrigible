using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Corrigible.Core
{
    /// <summary>
    /// Üretilmiş haritanın bir hücresi: sapmalı altıgen ızgarada sivri tepeli bir altıgen (tek satırlar yarım hücre sağa kayık).
    /// Koordinatlar tasarım px'i (1920×1080, sol üst orijin, y aşağı). Köşe k ile k+1 arasındaki kenar <see cref="Neighbor"/>[k]'ya
    /// bakar; köşeler ekranda saat yönünde: 0 üst, 1 sağ üst, 2 sağ alt, 3 alt, 4 sol alt, 5 sol üst. Salt okunur: harita
    /// önbellekte paylaşılır (<see cref="WorldMap.Generate"/>), sunum kenar kırarken kendi kopyasını kurar.
    /// </summary>
    public sealed class MapCell
    {
        public int Index { get; internal set; }
        public int Col { get; internal set; }
        public int Row { get; internal set; }
        public bool Land { get; internal set; }
        /// <summary>Kıta (kara bileşeni) sırası; denizde −1.</summary>
        public int Continent { get; internal set; } = -1;
        /// <summary>Bölgenin paket kimliği (<c>GameData.Regions</c>: "weu", "cn"…); denizde null.</summary>
        public string Region { get; internal set; }
        /// <summary>Bölge sınırına (deniz, başka bölge, harita kenarı) hücre ağında BFS uzaklığı: 0 sınırda; denizde −1.</summary>
        public int Depth { get; internal set; } = -1;
        /// <summary>Hücrenin merkezi (köşelerin ortalaması).</summary>
        public double CX { get; internal set; }
        public double CY { get; internal set; }

        internal readonly double[] x = new double[6], y = new double[6];
        internal readonly int[] vertex = new int[6], neighbor = new int[6];

        /// <summary>Altı köşe (sapmalı; iki komşu hücre ortak köşeyi aynı yerde görür).</summary>
        public IReadOnlyList<double> X { get; }
        public IReadOnlyList<double> Y { get; }
        /// <summary>Köşe kimlikleri (ortak köşe aynı kimlik).</summary>
        public IReadOnlyList<int> Vertex { get; }
        /// <summary>Altı yöndeki komşu hücre dizini (yön k: köşe k ile k+1 arasındaki kenar); ızgara dışı −1.</summary>
        public IReadOnlyList<int> Neighbor { get; }

        internal MapCell()
        {
            X = Array.AsReadOnly(x);
            Y = Array.AsReadOnly(y);
            Vertex = Array.AsReadOnly(vertex);
            Neighbor = Array.AsReadOnly(neighbor);
        }
    }

    /// <summary>İki hücre arasındaki kenar: kıyı (kara | deniz) ya da bölge sınırı (iki bölge). Uçlar köşe kimliğiyle zincirlenir.</summary>
    public sealed class MapEdge
    {
        /// <summary>CellA kara (kıyıda) ya da sınırın küçük dizinli tarafı; CellB deniz ya da öteki bölge (ızgara dışı −1).</summary>
        public int CellA { get; internal set; }
        public int CellB { get; internal set; }
        public string RegionA { get; internal set; }
        public string RegionB { get; internal set; }
        public int V1 { get; internal set; }
        public int V2 { get; internal set; }
        public double X1 { get; internal set; }
        public double Y1 { get; internal set; }
        public double X2 { get; internal set; }
        public double Y2 { get; internal set; }
        internal MapEdge() { }
    }

    /// <summary>Üretilmiş haritada bir bölge: 12 kimlik paketinden biri düğüme yerleşmiş hâli.</summary>
    public sealed class MapRegion
    {
        /// <summary>Paket kimliği (iç anahtar; sektör, kit, komut kimlikleri buna bağlı).</summary>
        public string Id { get; internal set; }
        /// <summary>Paketi işgal eden YZ'nin sabit adı. G5 harita özeti bu değeri kullanır.</summary>
        public string SourceName { get; internal set; }
        public int Continent { get; internal set; }
        /// <summary>Karargâhtan çizge (BFS) uzaklığı; karargâh 0.</summary>
        public int Distance { get; internal set; }
        /// <summary>Paketin kademe etiketi (<see cref="WorldMap.PackageStage"/>): 0 karargâh, 1 erken, 2 orta, 3 geç.</summary>
        public int Stage { get; internal set; }

        internal readonly List<int> cells = new List<int>(), inner = new List<int>();
        internal readonly List<string> neighbors = new List<string>();

        /// <summary>Hücreler (artan dizin).</summary>
        public IReadOnlyList<int> Cells { get; }
        /// <summary>
        /// Etiket adayı hücreler, en içteki önce: <see cref="MapCell.Depth"/> büyükten küçüğe, sonra bölgenin ortasına yakınlık
        /// (bölge hücrelerine altıgen uzaklıkları toplamı), sonra dizin. İlki bölgenin "en iç" hücresi.
        /// </summary>
        public IReadOnlyList<int> Inner { get; }
        /// <summary>Komşu bölgeler (kara ve kablo; <c>GameData.Regions</c> sırasıyla).</summary>
        public IReadOnlyList<string> Neighbors { get; }

        internal MapRegion()
        {
            Cells = cells.AsReadOnly();
            Inner = inner.AsReadOnly();
            Neighbors = neighbors.AsReadOnly();
        }
    }

    /// <summary>İki bölge arasındaki bağlantı: kara komşuluğu ya da kıtalar arası kablo hattı.</summary>
    public sealed class MapLink
    {
        public string A { get; internal set; }
        public string B { get; internal set; }
        /// <summary>Kıtalar arası (ya da denizi aşan) kablo hattı: çizimde kesik çizgi. Üçüncü bir bölgenin karasından ve başka bir
        /// kablodan geçmez.</summary>
        public bool Cable { get; internal set; }
        /// <summary>Kara komşuluğunda ortak kenar sayısı; kabloda 0.</summary>
        public int SharedEdges { get; internal set; }
        /// <summary>Kablonun uç hücreleri (A'nın ve B'nin kıyı hücreleri) ve merkezleri; kara komşuluğunda −1 ve 0.</summary>
        public int CellA { get; internal set; } = -1;
        public int CellB { get; internal set; } = -1;
        public double X1 { get; internal set; }
        public double Y1 { get; internal set; }
        public double X2 { get; internal set; }
        public double Y2 { get; internal set; }
        internal MapLink() { }
    }

    /// <summary>
    /// Her yeni harekâtta tohumdan üretilen dünya haritası (<c>Balance.liberationMapGen</c> = 1). Motordan bağımsız ve
    /// deterministiktir: aynı harekât tohumu her zaman aynı haritayı verir; kayda harita yazılmaz, yalnız kısa bir parmak izi
    /// (<see cref="Fingerprint"/>) yazılır. Kendi akışını kullanır (<c>new Rng(Rng.Mix(seed, Salt + deneme))</c>):
    /// <c>S.Rng</c>'den tüketmez, baskın tohumlarını ve altın zaman çizgilerini değiştirmez. Kararlarda (kara, bölge, komşuluk,
    /// karargâh, paket dağıtımı) trigonometri, üs ve kayan noktalı karekök yok; karşılaştırmalar tam sayıyla. Köşe sapması ve
    /// köşe koordinatları yalnız çizim içindir. Harita salt okunurdur (son üretilen önbellekte paylaşılır). Üretici kodu ya da
    /// tabloları (<see cref="PackageStage"/>) değişirse aynı tohum başka harita verir: <see cref="GeneratorVersion"/>'ı
    /// ve <c>GameRecord.CurrentVersion</c>'ı artır, <c>WorldMapTests</c>'in altın haritasını güncelle.
    /// </summary>
    public sealed partial class WorldMap
    {
        /// <summary>
        /// Üreticinin sürümü: parmak izine girer (üretici değişince eski kayıtların izi tutmaz). 3: açılış karıştırması. 4: karargâhın
        /// alan şartı (<see cref="HqMinThick"/>, <see cref="HqMinSize10"/>, <see cref="HqEdgeBand"/>).
        /// </summary>
        public const int GeneratorVersion = 5;
        /// <summary>Üreticinin akış tuzu ("MAP1"); kayıt 21'de kalkan taktik harita havuzunun tuzlarından (≈0–6000) çok uzak.</summary>
        public const uint Salt = 0x4D415031u;
        public const int RegionCount = 12;
        public const double Width = 1920, Height = 1080;
        public const int Cols = 20, Rows = 12;

        /// <summary>Harekât tohumu.</summary>
        public readonly uint Seed;
        /// <summary>Geçerli haritayı veren deneme (0'dan); yedek düzende −1.</summary>
        public readonly int Attempt;
        /// <summary>Deneme sınırı aşıldı, sabit yedek düzen kullanıldı.</summary>
        public bool Fallback => Attempt < 0;

        MapCell[] cells;
        readonly List<MapRegion> regions = new List<MapRegion>();
        readonly List<MapLink> links = new List<MapLink>();
        readonly List<MapEdge> coast = new List<MapEdge>(), borders = new List<MapEdge>();

        /// <summary>Hücreler: dizin = satır × <see cref="Cols"/> + sütun.</summary>
        public IReadOnlyList<MapCell> Cells { get; private set; }
        /// <summary>12 bölge, <c>GameData.Regions</c> (paket) sırasıyla.</summary>
        public IReadOnlyList<MapRegion> Regions { get; }
        /// <summary>Bağlantılar, <c>GameData.Regions</c> sırasıyla (A önce).</summary>
        public IReadOnlyList<MapLink> Links { get; }
        /// <summary>Kıyı kenarları (kara | deniz).</summary>
        public IReadOnlyList<MapEdge> Coast { get; }
        /// <summary>Bölge sınırı kenarları (iki farklı bölgenin kara hücreleri arasında).</summary>
        public IReadOnlyList<MapEdge> Borders { get; }
        /// <summary>Kıta sayısı (2–4).</summary>
        public int Continents { get; private set; }
        /// <summary>Simülasyonun komşuluk tablosu (<see cref="Links"/> ile aynı sıra).</summary>
        public IReadOnlyList<LinkDef> LinkDefs { get; private set; }
        /// <summary>Kısa parmak izi ("m1-xxxxxxxx"): üretici sürümü, komşuluk, paket yerleşimi ve adların özeti. Kayıtla karşılaştırılır.</summary>
        public string Fingerprint { get; private set; }

        /// <summary>Karargâhın paket kimliği (hep <c>Simulation.HQ</c> = "weu").</summary>
        public string Hq => Simulation.HQ;
        /// <summary>Karargâhın en iç hücresi (sığınak işareti, S-07).</summary>
        public int HqCell => Region(Hq).Inner[0];

        Dictionary<string, MapRegion> byId;

        WorldMap(uint seed, int attempt)
        {
            Seed = seed;
            Attempt = attempt;
            Regions = regions.AsReadOnly();
            Links = links.AsReadOnly();
            Coast = coast.AsReadOnly();
            Borders = borders.AsReadOnly();
        }

        public MapRegion Region(string id) => id != null && byId.TryGetValue(id, out var r) ? r : null;

        /// <summary>Bölgenin kara ve kablo komşuları.</summary>
        public bool Adjacent(string a, string b) => links.Any(l => l.A == a && l.B == b || l.A == b && l.B == a);

        /// <summary>Altıgen (küp) uzaklığı: iki hücre arasındaki adım sayısı.</summary>
        public static int HexDistance(int a, int b)
        {
            int ca = a % Cols, ra = a / Cols, cb = b % Cols, rb = b / Cols;
            int xa = ca - (ra - (ra & 1)) / 2, xb = cb - (rb - (rb & 1)) / 2;
            int dx = xa - xb, dz = ra - rb, dy = -dx - dz;
            return Math.Max(Math.Abs(dx), Math.Max(Math.Abs(dy), Math.Abs(dz)));
        }

        // ---------------------------------------------------------------- parmak izi

        /// <summary>Dünya haritasının (anahtar 0) parmak izi: sabit komşuluk tablosu.</summary>
        public static string WorldFingerprint
        {
            get
            {
                var sb = new StringBuilder("g0|").Append(Simulation.HQ);
                foreach (var l in GameData.Links) sb.Append('|').Append(l.A).Append('-').Append(l.B);
                return "m0-" + Fnv(sb.ToString());
            }
        }

        /// <summary>Kaydın beklediği parmak izi: tohum ve denge değerlerinden (harita üretilir ya da önbellekten; simülasyon kurulmaz).</summary>
        public static string FingerprintFor(uint seed, Balance balance) =>
            balance != null && balance.liberationMapGen == 1 ? Generate(seed).Fingerprint : WorldFingerprint;

        string BuildFingerprint()
        {
            var sb = new StringBuilder("g").Append(GeneratorVersion.ToString(CultureInfo.InvariantCulture)).Append('|').Append(Continents);
            foreach (var r in regions)
            {
                sb.Append('|').Append(r.Id).Append('=').Append(r.cells.Count.ToString(CultureInfo.InvariantCulture))
                    .Append('@').Append(r.cells[0].ToString(CultureInfo.InvariantCulture)).Append(':').Append(r.SourceName).Append('>');
                sb.Append(string.Join(",", r.neighbors));
            }
            foreach (var l in links) if (l.Cable) sb.Append("|~").Append(l.A).Append('-').Append(l.B);
            return "m1-" + Fnv(sb.ToString());
        }

        static string Fnv(string text)
        {
            uint h = 2166136261u;
            unchecked
            {
                foreach (char ch in text) { h ^= ch; h *= 16777619u; }
            }
            return h.ToString("x8", CultureInfo.InvariantCulture);
        }

        // ---------------------------------------------------------------- geçerlilik

        /// <summary>
        /// Haritanın kuralları (üretici yalnız bunları geçen haritayı verir): 12 bölge, 2–4 kıta, kara 110–150 hücre, kıtalar arası
        /// boğaz 2–3 kıtada en az iki deniz hücresi (<see cref="StraitWidth"/>), bölge boyutu ortalamanın ±%30'u, her bölgede en az bir kalın (altı komşusu da aynı
        /// bölgede) hücre, bağlı çizge, derece 2–5, toplam bağlantı 15–21, karargâhta "weu" ve derecesi 3–4, karargâhta en az iki kalın
        /// hücre ve ortalama bölge boyutunun en az %90'ı, en iç hücresi kenar bandının içinde (S-07, ad, ilk cephe ve figür sığsın), karargâhın komşularında
        /// en az bir erken paket (ilk kararda kolay bir seçenek), adlar tekrarsız. Geçerliyse null, değilse ilk bozulan kural. Testler kuralları buradan
        /// bağımsız da yeniden hesaplar.
        /// </summary>
        public string Validate()
        {
            if (regions.Count != RegionCount) return "bolge_sayisi";
            if (Continents < MinContinents || Continents > MaxContinents) return "kita";
            int land = cells.Count(c => c.Land);
            if (land < MinLand || land > MaxLand) return "kara";
            if (StraitWidth[Continents] == 2 && NarrowStrait()) return "bogaz";
            foreach (var r in regions)
            {
                if (!SizeOk(r.cells.Count, land)) return "boyut";
                if (!r.cells.Any(i => Thick(i))) return "kalin";
            }
            foreach (var c in cells) if (c.Land != (c.Region != null)) return "sahipsiz_kara";
            if (Region(Hq) == null || Region(Hq).neighbors.Count < 3 || Region(Hq).neighbors.Count > 4) return "karargah";
            var hqRegion = Region(Hq);
            if (hqRegion.cells.Count(i => Thick(i)) < HqMinThick || 10 * RegionCount * hqRegion.cells.Count < HqMinSize10 * land
                || !InEdgeBand(HqCell)) return "karargah_alan";
            if (!Region(Hq).neighbors.Any(id => PackageStage[id] == 1)) return "acilis";
            foreach (var r in regions) if (r.neighbors.Count < 2 || r.neighbors.Count > 5) return "derece";
            if (links.Count < MinLinks || links.Count > MaxLinks) return "toplam";
            if (!Connected()) return "bagli";
            if (regions.Select(r => r.SourceName).Distinct().Count() != RegionCount) return "ad";
            return null;
        }

        /// <summary>Kalın hücre: altı komşusu da aynı bölgede (etiket ve figür sığar).</summary>
        public bool Thick(int cell)
        {
            var c = cells[cell];
            if (c.Region == null) return false;
            foreach (int n in c.neighbor) if (n < 0 || cells[n].Region != c.Region) return false;
            return true;
        }

        static bool SizeOk(int size, int land) => 10 * RegionCount * size >= 7 * land && 10 * RegionCount * size <= 13 * land;

        /// <summary>İki farklı kıtanın kara hücreleri arasında ikiden az deniz hücresi var (altıgen uzaklığı &lt; 3).</summary>
        bool NarrowStrait()
        {
            foreach (var c in cells)
            {
                if (!c.Land) continue;
                foreach (int n in c.neighbor)
                {
                    if (n < 0) continue;
                    if (cells[n].Land && cells[n].Continent != c.Continent) return true;
                    foreach (int m in cells[n].neighbor)
                        if (m >= 0 && cells[m].Land && cells[m].Continent != c.Continent) return true;
                }
            }
            return false;
        }

        bool Connected()
        {
            var seen = new HashSet<string> { regions[0].Id };
            var queue = new Queue<string>(seen);
            while (queue.Count > 0)
                foreach (string n in Region(queue.Dequeue()).neighbors)
                    if (seen.Add(n)) queue.Enqueue(n);
            return seen.Count == regions.Count;
        }
    }
}
