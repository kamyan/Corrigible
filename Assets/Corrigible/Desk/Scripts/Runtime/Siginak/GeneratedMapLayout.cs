using System;
using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üretilmiş haritanın (anahtar 1) otomatik yerleşimi (tasarım §6): bölge başına etiket, punto, iki satır eşiği, cephe
    /// bloğunun yanı (şerit, yüzde, saha işaretleri, YENİDEN İŞGAL, raptiye), ÖZGÜR damgası ve ekip noktası. Saf sınıftır:
    /// metin ölçümünü dışarıdan alır (<see cref="Measure"/>, TMP <c>GetPreferredValues</c>), Unity sahnesine dokunmaz; böylece
    /// EditMode testi 100 tohumda iki dilde denetleyebilir. Harita pikseli (1920×1080, sol üst orijin, y aşağı).
    /// <list type="bullet">
    /// <item><b>Etiket:</b> <see cref="MapRegion.Inner"/> aday sırasıyla (en içteki önce) hücre merkezi ve küçük kaydırmalar; punto
    /// kademesi <see cref="Metrics.FontTiers"/> (34 → 31 → 28; 28 K2'de ≈ 17,6 ekran pikseli, A10'un 14 px alt sınırının üstünde).
    /// Ad iki dilde ölçülür; tek satırı <see cref="Metrics.MaxLineWidth"/>'ı aşan dil iki satıra bölünür
    /// (<see cref="LiberationRegionMarks.TwoLines"/>). Kutu iki dilin büyüğüdür; konum iki dilde aynıdır.</item>
    /// <item><b>Çakışma listesi:</b> etiket, adın altı (yüzde, ADAY, ✓✓✓), ÖZGÜR damgası, cephe bloğu (şerit + yüzde, saha işaretleri,
    /// YENİDEN İŞGAL, raptiye), ekip figürü tabanı, S-07 işareti, lejant ve kablo raptiyeleri. Hangi iki kutunun aynı anda
    /// görünebildiği <see cref="Exclusive"/>'te: tek cephe ve tek figür olduğundan iki cephe bloğu ya da iki figür birbirini
    /// dışlamaz; figür cephe bölgesinde ya da karargâhta durur.</item>
    /// <item><b>Arama:</b> karargâh önce, sonra küçük bölgeler; bölge başına etiket yapılandırmaları puan sırasıyla denenir, her
    /// birinde damga, cephe bloğu ve ekip noktası aranır. Bir bölge sığmazsa sıranın başına alınıp yerleşim baştan yapılır.</item>
    /// <item>Hepsi harita alanında, <see cref="Metrics.Margin"/> payıyla. Ekip noktası hücre merkezi ya da aynı bölgenin iki komşu
    /// hücresinin ortası: hep bölgenin içinde.</item>
    /// </list>
    /// Sunumdur: simülasyona ve kayda hiçbir şey dönmez; aynı harita ve ölçümle aynı sonucu verir.
    /// </summary>
    public sealed class GeneratedMapLayout
    {
        public enum Kind { Label, Percent, Candidate, Ticks, Stamp, FrontStrip, FrontFields, FrontReoccupied, FrontPin, Team, Shelter, Legend, CablePin, Occluder }

        /// <summary>Çakışma listesindeki bir kutu; <see cref="Region"/> null ise sabit (S-07, lejant, kablo raptiyesi, örtücü).</summary>
        public readonly struct Box
        {
            public readonly string Region;
            public readonly Kind Kind;
            public readonly Rect Rect;
            public Box(string region, Kind kind, Rect rect) { Region = region; Kind = kind; Rect = rect; }
            public override string ToString() => $"{Region ?? "-"}:{Kind} {Rect}";
        }

        /// <summary>Ölçüler (harita pikseli). İki dile bağlı olanlar iki dilin büyüğüdür.</summary>
        public sealed class Metrics
        {
            /// <summary>Ad puntoları, büyükten küçüğe; sonuncusu alt sınır.</summary>
            public float[] FontTiers = { 34, 31, 28 };
            /// <summary>Tek satırı bunu aşan ad iki satıra bölünür.</summary>
            public float MaxLineWidth = 200;
            /// <summary>
            /// Bölgesine sığıyorsa ad tek satır kalabilir (iki satırlı hâlden önce denenir); tek satır bunu aşarsa denenmez.
            /// 0: hep <see cref="MaxLineWidth"/> kuralı.
            /// </summary>
            public float MaxSingleLine = 340;
            /// <summary>Seçim çipi payı (ad kutusu + bu).</summary>
            public Vector2 ChipPad = new Vector2(14, 2);
            /// <summary>İşgal/aday yüzdesi (CP 700 28).</summary>
            public Vector2 Percent = new Vector2(90, 34);
            /// <summary>ADAY damgası (yazı + 22 × 12).</summary>
            public Vector2 Candidate = new Vector2(110, 44);
            /// <summary>Cephe yüzdesi (CP 700 32, şeridin sağında).</summary>
            public Vector2 FrontPercent = new Vector2(100, 40);
            /// <summary>YENİDEN İŞGAL damgası (yazı + 24 × 14).</summary>
            public Vector2 Reoccupied = new Vector2(230, 44);
            /// <summary>ÖZGÜR damgası (ölçek 1): 112 px halka, ±7° dönük ve taze damgada 1,1 kat.</summary>
            public float FreeStamp = 137;
            /// <summary>Damga ölçeği kademesi: sığmayan bölgede damga küçülür.</summary>
            public float[] StampTiers = { 1f, 0.85f, 0.72f };
            /// <summary>Ekip figürünün tabanı (70 × 48 mm) ve piyonların payı; merkez noktaya göre kaydırma.</summary>
            public Vector2 Figure = new Vector2(116, 88);
            public Vector2 FigureOffset = new Vector2(0, -6);
            /// <summary>Figürün görünen taban elipsi (harita pikseli; merkez ekip noktası). Elipsin <see cref="FigureOwnShare"/>'i kendi bölgesinde olmalı.</summary>
            public Vector2 FigureBase = new Vector2(112, 77);
            public float FigureOwnShare = 0.85f;
            /// <summary>S-07 fişi (82 × 32) ve payı.</summary>
            public Vector2 Shelter = new Vector2(90, 40);
            public float Pin = 30, CablePin = 16;
            /// <summary>İki kutu arasında bırakılan en az boşluk.</summary>
            public float Gap = 4;
            /// <summary>Harita kenarı payı.</summary>
            public float Margin = 12;
            /// <summary>Basılı lejantın sahnedeki dikdörtgeni (harita pikseli); boşsa yok.</summary>
            public Rect Legend;
            /// <summary>
            /// Haritanın masadaki nesnelerin (daktilo, kalem) altında kalan yerleri (harita pikseli, K2 kamerası): hiçbir işaret ve
            /// lejant buraya konmaz.
            /// </summary>
            public List<Rect> Occluders = new List<Rect>();
            /// <summary>Ad ve altındakilerin bölge dışında kalabilecek en çok payı (Türkçe noktalı harf ve kuyruk payı).</summary>
            public float OwnTolerance = 0.03f;
            /// <summary>Lejant ile kablo raptiyesi/çizgisi arasında en az boşluk; lejant altında kalabilecek en çok kara payı.</summary>
            public float LegendCableGap = 12, LegendLand = 0.02f;
            /// <summary>
            /// Lejant köşeyi değiştirebilir: sahnedeki yer (sağ alt) ve öteki üç köşe denenir, altında en az kara kalan seçilir
            /// (eşitlikte sahnedeki yer). Kapalıysa lejant yerinde kalır.
            /// </summary>
            public bool MoveLegend = true;
            /// <summary>
            /// Adaya giden okun gövde boyu, başı ve çizgi kalınlığı (<c>GeneratedMapRenderer.SetFrontier</c> ile aynı token'lar) ve
            /// kablonun sarkması (boyuna oranla, 6–22 px): karargâhın komşu sınırlarındaki oklar ve kablo ipleri S-07'ye engel.
            /// </summary>
            public float FrontierLength = 56, FrontierHead = 20, FrontierWidth = 4, CableSag = 0.08f;
            /// <summary>
            /// S-07 fişinin karargâh konturundan payı (kontur yarı kalınlığı + kıyı halesi): kutu bu kadar büyütülünce köşeleri ve kenar
            /// ortaları da karargâhta kalmalı (fiş konturun ya da kıyının üstüne binmez).
            /// </summary>
            public float ShelterContourPad = 6;
            /// <summary>
            /// Cephe raptiyesinin merkezinden bölge sınırına en az uzaklık: baş yarıçapı (~7) + konturun içe kayması (5) + çizginin
            /// yarısı (~2,5) + görünür boşluk (~4); raptiye koyu konturun üstüne binmesin.
            /// </summary>
            public float PinClearance = 19;
            /// <summary>S-07 ile cephe şeridi ya da saha işaretleri aynı satırdaysa aralarındaki en az yatay boşluk (blokla tek grup gibi okunmasın).</summary>
            public float ShelterRowGap = 24;
            /// <summary>
            /// Karargâh figürü (oyuncunun ilk ekranı) birinci turda: taban elipsinin en az bu payı karargâhın karasında, figür kutusunun
            /// en çok bu payı başka bölgenin karasında.
            /// </summary>
            public float HqFigureOwn = 0.9f, HqFigureForeign = 0.05f;
            /// <summary>
            /// Karargâh figürünün taban elipsi bu kadar büyütülerek ölçülür: kontur bölge sınırının 5 px içinde çizilir, taban koyu
            /// çizginin üstüne binmesin.
            /// </summary>
            public float HqFigurePad = 6;
            /// <summary>Karargâhın ad kutusu (çip payı dahil) bu kadar büyütülünce köşeleri ve kenar ortaları karargâhta olmalı.</summary>
            public float HqLabelPad = 3;
            public float MinFont => FontTiers[FontTiers.Length - 1];
        }

        /// <summary>Metnin (büyük harfli, gerekirse iki satır) verilen puntodaki tercih edilen boyu (TMP <c>GetPreferredValues</c>).</summary>
        public delegate Vector2 Measure(string text, float fontSize);

        /// <summary>Bölgenin yerleşimi; <see cref="LiberationRegionMarks"/> bunu sahnedeki elle girilmiş değerlerin yerine kullanır.</summary>
        public sealed class Placement
        {
            public string Id;
            /// <summary>Etiketin merkezi.</summary>
            public Vector2 Label;
            public float FontSize;
            /// <summary>İki satır eşiği (tek satıra sığan yerleşimde <see cref="Metrics.MaxSingleLine"/>'dan büyük).</summary>
            public float MaxLineWidth;
            /// <summary>Ad kutusu (iki dilin büyüğü, çip payı hariç): cephe şeridi, yüzde ve raptiye buna göre durur.</summary>
            public Vector2 LabelBox;
            public LiberationRegionMarks.StripSide StripSide;
            public Vector2 StripOffset;
            /// <summary>YENİDEN İŞGAL damgasının merkezi, şeridin sol üstüne göre (sahnedeki düzen <see cref="ReoccupiedBelow"/>).</summary>
            public Vector2 ReoccupiedOffset = ReoccupiedBelow;
            /// <summary>Cephe raptiyesi adın sağında (yoksa solunda); çoğu zaman şeride göre (şerit soldaysa sağda), raptiye o yanda kendi
            /// karasında kalmıyorsa öbür yanda.</summary>
            public bool PinRight;
            public Vector2 Stamp;
            /// <summary>ÖZGÜR damgasının ölçeği (<see cref="Metrics.StampTiers"/>).</summary>
            public float StampScale = 1;
            public Vector2 Team;
            public int LabelCell;
            /// <summary>Ad ve altındakiler kendi bölgesinde (<see cref="Metrics.OwnTolerance"/>); değilse en iyi taşan seçenek kullanıldı.</summary>
            public bool LabelInRegion;
            /// <summary>Bölgenin kendi bölgesinde kalan bir etiket seçeneği vardı (yerleşmiş kutular yüzünden kullanılamamış olabilir).</summary>
            public bool HadInRegionOption;
            /// <summary>Cephe bloğu birinci turun kuralıyla (başka bölgenin karasında değil, şeridi kendi konturunu kesmiyor) yerleşti.</summary>
            public bool FrontOwnLand;
            /// <summary>Karargâh en sıkı kademesiyle yerleşti (figür içeride, bütün cephe bloğu ve raptiye kendi karasında, kablo ipine değmiyor).</summary>
            public bool HqStrict;
        }

        public WorldMap Map { get; }
        public Metrics Sizes { get; }
        public IReadOnlyDictionary<string, Placement> Placements => placements;
        /// <summary>Bütün kutular (sabitler dahil).</summary>
        public IReadOnlyList<Box> Boxes => boxes;
        /// <summary>Her şey çakışmasız yerleşti (yoksa en az çakışmalı yerleşim kullanıldı).</summary>
        public bool Clean { get; private set; }
        /// <summary>Baştan yerleştirme sayısı (bir bölge sığmayınca sıranın başına alınır).</summary>
        public int Restarts { get; private set; }
        /// <summary>Birinci turda (adlar ve cephe blokları kendi yerinde, gerekirse bölge bölge gevşetilerek) bitti.</summary>
        public bool FirstPass { get; private set; }
        /// <summary>
        /// S-07 fişinin merkezi: karargâhın içinde. <see cref="WorldMap.HqCell"/>'e kilitli değil; karargâhın iç hücrelerinden
        /// (<see cref="MapRegion.Inner"/>) ad, ilk cephe bloğu ve figürle birlikte en iyi sığan yer seçilir (sunumdur, kayda girmez).
        /// </summary>
        public Vector2 ShelterPoint { get; private set; }
        /// <summary>Lejantın bu haritadaki yeri (harita pikseli; boşsa lejant yok).</summary>
        public Rect Legend { get; private set; }

        public Placement Of(string id) => id != null && placements.TryGetValue(id, out var p) ? p : null;

        readonly Dictionary<string, Placement> placements = new Dictionary<string, Placement>();
        readonly List<Box> boxes = new List<Box>();
        readonly List<Box> fixedBoxes = new List<Box>();
        readonly GeneratedMapGeometry geometry;
        readonly IReadOnlyDictionary<string, IReadOnlyList<string>> names;
        readonly Measure measure;
        readonly Dictionary<(string, float, float), Vector2> labelSizes = new Dictionary<(string, float, float), Vector2>();
        readonly List<(Vector2 a, Vector2 b)> cables = new List<(Vector2, Vector2)>();
        string hq;

        GeneratedMapLayout(WorldMap map, GeneratedMapGeometry geometry, IReadOnlyDictionary<string, IReadOnlyList<string>> names,
            Measure measure, Metrics metrics)
        {
            Map = map;
            Sizes = metrics ?? new Metrics();
            this.geometry = geometry;
            this.names = names;
            this.measure = measure;
            // Bölge payı için kaba ızgara (10 px; nokta (i + ½) × 10): her kara hücresinin kırık halkası satır satır taranır
            // (çift-tek kuralı), örnekleme dizi okumasına iner; ekip noktası yine kesin halkayla denetlenir
            gridW = (int)(WorldMap.Width / GridStep) + 1;
            gridH = (int)(WorldMap.Height / GridStep) + 1;
            grid = new string[gridW * gridH];
            var xs = new List<float>(8);
            foreach (var c in map.Cells)
            {
                if (!c.Land) continue;
                var ring = geometry.CellRing(c.Index);
                float y0 = float.MaxValue, y1 = float.MinValue;
                foreach (var q in ring) { y0 = Mathf.Min(y0, q.y); y1 = Mathf.Max(y1, q.y); }
                for (int gy = Mathf.Max(0, Mathf.FloorToInt(y0 / GridStep - 0.5f)); gy < gridH; gy++)
                {
                    float y = (gy + 0.5f) * GridStep;
                    if (y > y1) break;
                    xs.Clear();
                    for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
                    {
                        Vector2 a = ring[i], b = ring[j];
                        if ((a.y > y) != (b.y > y)) xs.Add((b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x);
                    }
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                        for (int gx = Mathf.Max(0, Mathf.FloorToInt(xs[k] / GridStep - 0.5f)); gx < gridW; gx++)
                        {
                            float x = (gx + 0.5f) * GridStep;
                            if (x >= xs[k + 1]) break;
                            if (x > xs[k]) grid[gy * gridW + gx] = c.Region;
                        }
                }
            }
            BuildSums();
        }

        const float GridStep = 10;
        readonly int gridW, gridH;
        readonly string[] grid;
        // Izgaranın toplam alan tabloları (bölge başına ve bütün kara): dikdörtgendeki ızgara noktası sayısı O(1)
        readonly Dictionary<string, int[]> ownSums = new Dictionary<string, int[]>();
        int[] landSums;

        void BuildSums()
        {
            int w = gridW + 1;
            landSums = new int[w * (gridH + 1)];
            foreach (var r in Map.Regions) ownSums[r.Id] = new int[w * (gridH + 1)];
            foreach (var r in Map.Regions)
            {
                var sum = ownSums[r.Id];
                for (int y = 0; y < gridH; y++)
                    for (int x = 0; x < gridW; x++)
                        sum[(y + 1) * w + x + 1] = (grid[y * gridW + x] == r.Id ? 1 : 0) + sum[y * w + x + 1] + sum[(y + 1) * w + x] - sum[y * w + x];
            }
            for (int y = 0; y < gridH; y++)
                for (int x = 0; x < gridW; x++)
                    landSums[(y + 1) * w + x + 1] = (grid[y * gridW + x] != null ? 1 : 0) + landSums[y * w + x + 1] + landSums[(y + 1) * w + x] - landSums[y * w + x];
        }

        /// <summary>Izgara noktası aralığındaki (uçlar dahil, kırpılmış) toplam.</summary>
        int Sum(int[] sums, int gx0, int gy0, int gx1, int gy1)
        {
            int w = gridW + 1;
            gx0 = Mathf.Max(0, gx0); gy0 = Mathf.Max(0, gy0); gx1 = Mathf.Min(gridW - 1, gx1); gy1 = Mathf.Min(gridH - 1, gy1);
            if (gx1 < gx0 || gy1 < gy0) return 0;
            return sums[(gy1 + 1) * w + gx1 + 1] - sums[gy0 * w + gx1 + 1] - sums[(gy1 + 1) * w + gx0] + sums[gy0 * w + gx0];
        }

        string GridRegion(Vector2 p)
        {
            int gx = (int)(p.x / GridStep), gy = (int)(p.y / GridStep);
            return gx < 0 || gy < 0 || gx >= gridW || gy >= gridH ? null : grid[gy * gridW + gx];
        }

        /// <summary>
        /// Yerleşimi hesaplar. <paramref name="names"/>: bölge kimliği → adın her dildeki gösterilen hâli (büyük harf).
        /// </summary>
        public static GeneratedMapLayout Compute(WorldMap map, GeneratedMapGeometry geometry,
            IReadOnlyDictionary<string, IReadOnlyList<string>> names, Measure measure, Metrics metrics = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (geometry == null || geometry.Map != map) throw new ArgumentException("geometri bu haritanın değil", nameof(geometry));
            if (measure == null) throw new ArgumentNullException(nameof(measure));
            var layout = new GeneratedMapLayout(map, geometry, names, measure, metrics);
            layout.Run();
            return layout;
        }

        // ---------------------------------------------------------------- ad ölçüsü

        /// <summary>
        /// Adın bir dildeki kutusu (çip payı hariç): <see cref="LiberationRegionMarks"/>'ın kuralıyla, tek satırı
        /// <paramref name="maxLineWidth"/>'ı aşarsa iki satır.
        /// </summary>
        public static Vector2 LabelSize(string text, float fontSize, float maxLineWidth, Measure measure)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.zero;
            var size = measure(text, fontSize);
            if (size.x > maxLineWidth) size = measure(LiberationRegionMarks.TwoLines(text), fontSize);
            return size;
        }

        Vector2 LabelBoxOf(string id, float fontSize, float maxLineWidth)
        {
            if (labelSizes.TryGetValue((id, fontSize, maxLineWidth), out var v)) return v;
            v = Vector2.zero;
            if (names != null && names.TryGetValue(id, out var list) && list != null)
                foreach (var text in list) v = Vector2.Max(v, LabelSize(text, fontSize, maxLineWidth, measure));
            if (v == Vector2.zero) v = new Vector2(120, fontSize * 1.2f);
            labelSizes[(id, fontSize, maxLineWidth)] = v;
            return v;
        }

        /// <summary>Tek satır kipinin iki satır eşiği (hiçbir ad bölünmez).</summary>
        public const float SingleLine = 100000;

        // ---------------------------------------------------------------- kutular

        static Rect Centered(Vector2 c, Vector2 size) => new Rect(c - size * 0.5f, size);

        /// <summary>Etiket kutusu (çip dahil), etiket merkezine göre değil haritada.</summary>
        public static Rect LabelRect(Vector2 label, Vector2 labelBox, Metrics m) => Centered(label, labelBox + m.ChipPad);

        /// <summary>
        /// Adın altı (<see cref="LiberationRegionMarks"/> › Below ile aynı ölçüler; <c>below</c> = ad kutusunun yarı yüksekliği):
        /// işgal/aday yüzdesi, adayda ADAY damgası, özgürde ✓✓✓. Üçü aynı anda görünmez (yüzde ile ADAY birlikte görünür).
        /// </summary>
        public static void UnderRects(Vector2 label, Vector2 labelBox, Metrics m, out Rect percent, out Rect candidate, out Rect ticks)
        {
            float below = labelBox.y * 0.5f;
            percent = new Rect(label.x - m.Percent.x * 0.5f, label.y + below + 6, m.Percent.x, m.Percent.y);
            candidate = Centered(label + new Vector2(0, below + 62), m.Candidate);
            ticks = Rect.MinMaxRect(label.x - 60, label.y + below + 8, label.x + 60, label.y + below + 44);
        }

        /// <summary>YENİDEN İŞGAL'in dünya haritasındaki yeri: saha işaretlerinin sağ altı (şeridin sol üstüne göre).</summary>
        public static readonly Vector2 ReoccupiedBelow = new Vector2(150, 128);

        /// <summary>
        /// Sıkı düzen: YENİDEN İŞGAL saha işaretlerinin sağında, aynı satırda (blok 55 px kısalır); eğik damganın kutusu
        /// şeride değmesin diye gerekirse biraz aşağıda.
        /// </summary>
        public static Vector2 ReoccupiedBeside(Metrics m) => new Vector2(98 + m.Reoccupied.x * 0.5f, Mathf.Max(70, 40 + m.Gap + 0.5f + m.Reoccupied.y * 0.5f));

        /// <summary>Cephe bloğunun kutuları: şerit + yüzde, saha işaretleri, YENİDEN İŞGAL ve raptiye (Below/StripOrigin ile aynı).</summary>
        public static void FrontRects(Vector2 label, Vector2 labelBox, LiberationRegionMarks.StripSide side, Vector2 offset, Metrics m,
            out Rect strip, out Rect fields, out Rect reoccupied, out Rect pin) =>
            FrontRects(label, labelBox, side, offset, ReoccupiedBelow, m, out strip, out fields, out reoccupied, out pin);

        public static void FrontRects(Vector2 label, Vector2 labelBox, LiberationRegionMarks.StripSide side, Vector2 offset, Vector2 reoccupiedOffset,
            Metrics m, out Rect strip, out Rect fields, out Rect reoccupied, out Rect pin) =>
            FrontRects(label, labelBox, side, offset, reoccupiedOffset, side == LiberationRegionMarks.StripSide.Left, m, out strip, out fields, out reoccupied, out pin);

        /// <param name="pinRight">Raptiye adın sağında (yoksa solunda); <see cref="Placement.PinRight"/>.</param>
        public static void FrontRects(Vector2 label, Vector2 labelBox, LiberationRegionMarks.StripSide side, Vector2 offset, Vector2 reoccupiedOffset,
            bool pinRight, Metrics m, out Rect strip, out Rect fields, out Rect reoccupied, out Rect pin)
        {
            float w = labelBox.x, below = labelBox.y * 0.5f;
            var s = side switch
            {
                LiberationRegionMarks.StripSide.Right => new Vector2(w * 0.5f + offset.x, offset.y),
                LiberationRegionMarks.StripSide.Left => new Vector2(-w * 0.5f + offset.x, offset.y),
                _ => new Vector2(-w * 0.5f, below + 10),
            };
            // Şerit: 3 × 38 px (44 aralık) + 10 px + yüzde (sol orta, şeridin ortasının 1 px altı)
            var marks = new Rect(s.x, s.y, 2 * 44 + 38, 38);
            var percent = new Rect(s.x + 3 * 44 + 10, s.y + 20 - m.FrontPercent.y * 0.5f, m.FrontPercent.x, m.FrontPercent.y);
            strip = Union(marks, percent);
            // Saha işaretleri: merkez şerit + (19 − 25 + 48, 70), 96 × 46
            fields = Centered(s + new Vector2(42, 70), new Vector2(96, 46));
            reoccupied = Centered(s + reoccupiedOffset, m.Reoccupied);
            strip.position += label; fields.position += label; reoccupied.position += label;
            pin = PinRect(label, labelBox, pinRight, m);
        }

        /// <summary>Cephe raptiyesi: adın sağında ya da solunda, ilk satır hizasında (<see cref="LiberationRegionMarks"/> ile aynı).</summary>
        public static Rect PinRect(Vector2 label, Vector2 labelBox, bool right, Metrics m) =>
            Centered(label + new Vector2((right ? 1 : -1) * (labelBox.x * 0.5f + 18), -12), Vector2.one * m.Pin);

        public static Rect StampRect(Vector2 stamp, float scale, Metrics m) => Centered(stamp, Vector2.one * (m.FreeStamp * scale));
        public static Rect TeamRect(Vector2 anchor, Metrics m) => Centered(anchor + m.FigureOffset, m.Figure);

        /// <summary>Bir yerleşimin bütün kutuları (bölgesiyle).</summary>
        public static IEnumerable<Box> BoxesOf(Placement p, Metrics m)
        {
            yield return new Box(p.Id, Kind.Label, LabelRect(p.Label, p.LabelBox, m));
            UnderRects(p.Label, p.LabelBox, m, out var percent, out var candidate, out var ticks);
            yield return new Box(p.Id, Kind.Percent, percent);
            yield return new Box(p.Id, Kind.Candidate, candidate);
            yield return new Box(p.Id, Kind.Ticks, ticks);
            yield return new Box(p.Id, Kind.Stamp, StampRect(p.Stamp, p.StampScale, m));
            FrontRects(p.Label, p.LabelBox, p.StripSide, p.StripOffset, p.ReoccupiedOffset, p.PinRight, m, out var strip, out var fields, out var reocc, out var pin);
            yield return new Box(p.Id, Kind.FrontStrip, strip);
            yield return new Box(p.Id, Kind.FrontFields, fields);
            yield return new Box(p.Id, Kind.FrontReoccupied, reocc);
            yield return new Box(p.Id, Kind.FrontPin, pin);
            yield return new Box(p.Id, Kind.Team, TeamRect(p.Team, m));
        }

        public static bool IsFront(Kind k) => k == Kind.FrontStrip || k == Kind.FrontFields || k == Kind.FrontReoccupied || k == Kind.FrontPin;

        public static bool IsUnder(Kind k) => k == Kind.Percent || k == Kind.Candidate || k == Kind.Ticks;

        /// <summary>
        /// İki kutu aynı anda görünebilir mi (görünebiliyorsa çakışmamalı)? Tek cephe: iki cephe bloğu birlikte görünmez. Tek
        /// figür: iki ekip noktası birlikte görünmez; figür cephede (cephe özgürleşince de adaylar seçilene dek orada) ya da
        /// Teknisyen yatıkken karargâhta durur: karargâhın ekip noktası her cephe bloğuyla birlikte görünebilir. Bölgenin kendi
        /// kutuları: ad, altı (yüzde, ADAY, ✓✓✓) alt alta tasarlandı; raptiye adın yanında. Yüzde ve ADAY yalnız işgal/adayda,
        /// ✓✓✓, damga ve (bölgede duran) figür özgür ya da cephedeyken; cephe bloğu yalnız cephedeyken. Sabitler (S-07, lejant,
        /// kablo raptiyesi) her şeyle çakışmamalı; figür kablo raptiyesinin üstünde durabilir.
        /// </summary>
        public static bool Exclusive(in Box a, in Box b, string hq)
        {
            // Karargâh hiç işgal/aday olmaz (ilk cephedir): yüzdesi ve ADAY damgası hiç görünmez (sabitlerle de çakışmaz)
            if (NeverShown(a, hq) || NeverShown(b, hq)) return false;
            if (a.Region == null || b.Region == null)
            {
                // Sabitler arası (S-07, lejant, raptiye, örtücü) yerleşimden önce seçilir (lejant köşesi bunlara göre)
                if (a.Region == null && b.Region == null) return false;
                var fixedBox = a.Region == null ? a : b;
                var other = a.Region == null ? b : a;
                return !(fixedBox.Kind == Kind.CablePin && other.Kind == Kind.Team);
            }
            bool frontA = IsFront(a.Kind), frontB = IsFront(b.Kind);
            if (a.Region == b.Region)
            {
                Kind x = a.Kind, y = b.Kind;
                if (frontA && frontB) return false;
                if (frontA || frontB)
                {
                    var front = frontA ? x : y;
                    var other = frontA ? y : x;
                    if (other == Kind.Team) return true;
                    return other == Kind.Label && front != Kind.FrontPin;
                }
                if (x == Kind.Label && IsUnder(y) || y == Kind.Label && IsUnder(x)) return false;
                if (IsUnder(x) && IsUnder(y)) return false;
                // Yüzde ve ADAY (işgal/aday) ile damga ya da figür (özgür/cephe) birlikte görünmez
                if ((x == Kind.Percent || x == Kind.Candidate) && (y == Kind.Stamp || y == Kind.Team)) return false;
                if ((y == Kind.Percent || y == Kind.Candidate) && (x == Kind.Stamp || x == Kind.Team)) return false;
                return true;
            }
            if (frontA && frontB) return false;
            if (a.Kind == Kind.Team && b.Kind == Kind.Team) return false;
            if (a.Kind == Kind.Team && frontB) return a.Region == hq;
            if (b.Kind == Kind.Team && frontA) return b.Region == hq;
            return true;
        }

        static bool NeverShown(in Box b, string hq) => b.Region == hq && (b.Kind == Kind.Percent || b.Kind == Kind.Candidate);

        /// <summary>İki kutu <paramref name="gap"/> payıyla üst üste biniyor mu?</summary>
        public static bool Touch(Rect a, Rect b, float gap) =>
            a.xMin < b.xMax + gap && b.xMin < a.xMax + gap && a.yMin < b.yMax + gap && b.yMin < a.yMax + gap;

        bool Inside(Rect r) =>
            r.xMin >= Sizes.Margin && r.yMin >= Sizes.Margin
            && r.xMax <= (float)WorldMap.Width - Sizes.Margin && r.yMax <= (float)WorldMap.Height - Sizes.Margin;

        static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        // ---------------------------------------------------------------- bölge payı

        string RegionAt(Vector2 p) => geometry.RegionAt(p);

        /// <summary>Örnekleme: kutu 4 px içe daraltılır; içindeki 10 px ızgara noktaları sayılır.</summary>
        const float CoverInset = 4;

        /// <summary>
        /// Kutunun kendi bölgesinde (<c>own</c>) ve başka bir bölgenin karasında (<c>foreign</c>) kalan payları (ızgara noktası
        /// oranı; nokta yoksa en yakın nokta); kalanı deniz.
        /// </summary>
        (float own, float foreign) Coverage(Rect r, string id, float inset = CoverInset)
        {
            float x0 = r.xMin + inset, x1 = r.xMax - inset, y0 = r.yMin + inset, y1 = r.yMax - inset;
            // Izgara noktası i'nin yeri (i + ½) × adım
            int gx0 = Mathf.CeilToInt(x0 / GridStep - 0.5f), gx1 = Mathf.FloorToInt(x1 / GridStep - 0.5f);
            int gy0 = Mathf.CeilToInt(y0 / GridStep - 0.5f), gy1 = Mathf.FloorToInt(y1 / GridStep - 0.5f);
            if (gx1 < gx0) gx0 = gx1 = Mathf.FloorToInt(r.center.x / GridStep);
            if (gy1 < gy0) gy0 = gy1 = Mathf.FloorToInt(r.center.y / GridStep);
            float n = (gx1 - gx0 + 1) * (gy1 - gy0 + 1);
            int land = Sum(landSums, gx0, gy0, gx1, gy1);
            int own = id != null && ownSums.TryGetValue(id, out var sums) ? Sum(sums, gx0, gy0, gx1, gy1) : 0;
            return (own / n, (land - own) / n);
        }

        /// <summary>Kutunun bölge dışında kalan payı.</summary>
        float Outside(Rect r, string id, float inset = CoverInset) => 1 - Coverage(r, id, inset).own;

        /// <summary>Kutu kendi bölgesinin sınırını (kontur, kıyı) kesiyor mu: payın bir kısmı içeride, bir kısmı dışarıda.</summary>
        bool Straddles(Rect r, string id)
        {
            float own = Coverage(r, id).own;
            return own > 0.06f && own < 0.94f;
        }

        /// <summary>
        /// Elipsin (merkez, yarıçaplar) kendi bölgesinde kalan payı (7 × 5 ızgaranın elips içindeki noktaları; kesin halkalarla).
        /// </summary>
        float EllipseOwn(Vector2 center, Vector2 radii, string id)
        {
            int own = 0, n = 0;
            for (int iy = 0; iy < 5; iy++)
                for (int ix = 0; ix < 7; ix++)
                {
                    float u = ix / 3f - 1, v = iy / 2f - 1;
                    if (u * u + v * v > 1.0001f) continue;
                    n++;
                    if (RegionAt(center + new Vector2(u * radii.x, v * radii.y)) == id) own++;
                }
            return n > 0 ? own / (float)n : 0;
        }

        int CableHits(Rect r)
        {
            int hits = 0;
            foreach (var (a, b) in cables) if (SegmentHits(a, b, r)) hits++;
            return hits;
        }

        /// <summary>Doğru parçası dikdörtgene değiyor mu (Liang–Barsky)?</summary>
        static bool SegmentHits(Vector2 a, Vector2 b, Rect r)
        {
            float t0 = 0, t1 = 1, dx = b.x - a.x, dy = b.y - a.y;
            bool Clip(float p, float q)
            {
                if (Mathf.Approximately(p, 0)) return q >= 0;
                float t = q / p;
                if (p < 0) { if (t > t1) return false; if (t > t0) t0 = t; }
                else { if (t < t0) return false; if (t < t1) t1 = t; }
                return true;
            }
            return Clip(-dx, a.x - r.xMin) && Clip(dx, r.xMax - a.x) && Clip(-dy, a.y - r.yMin) && Clip(dy, r.yMax - a.y);
        }

        // ---------------------------------------------------------------- arama

        sealed class LabelOption
        {
            public int Cell, Rank, Tier, Bucket;
            public float MaxLine;
            public Vector2 Point, Box;
            public Rect Label, Percent, Candidate, Ticks;
            public float Score;
        }

        struct Spot
        {
            public Vector2 Point;
            public float Scale;
            public Rect Rect;
            public float Score;
            /// <summary>Ekip noktasında: taban elipsinin kendi bölgesindeki payı (karargâhta <see cref="Metrics.HqFigurePad"/> payıyla) ve
            /// figür kutusunun başka karadaki payı.</summary>
            public float Own, Foreign;
            /// <summary>S-07 adayında: konturdan payı, oklar ve kablo ipleri kuralına uyuyor.</summary>
            public bool Clear;
        }

        const int MaxRestarts = 16;

        void Run()
        {
            hq = Map.Hq;
            var m = Sizes;
            // Sabitler: S-07, lejant, kablo raptiyeleri
            foreach (var l in Map.Links)
            {
                if (!l.Cable) continue;
                cables.Add((new Vector2((float)l.X1, (float)l.Y1), new Vector2((float)l.X2, (float)l.Y2)));
            }

            // Sıra: karargâh önce (S-07 ve figürü her cepheyle birlikte görünür), sonra küçükten büyüğe
            var start = new List<string> { hq };
            var rest = new List<MapRegion>();
            foreach (var r in Map.Regions) if (r.Id != hq) rest.Add(r);
            rest.Sort((x, y) => x.Cells.Count != y.Cells.Count ? x.Cells.Count.CompareTo(y.Cells.Count) : string.CompareOrdinal(x.Id, y.Id));
            foreach (var r in rest) start.Add(r.Id);

            var options = new Dictionary<string, List<LabelOption>>();
            foreach (var id in start) options[id] = LabelOptions(Map.Region(id));

            // S-07 karargâhın içinde durur: aday yerler iç hücrelerin merkezleri ve küçük kaymaları; her lejant yerinde karargâhın
            // adı, ilk cephe bloğu ve figürüyle birlikte en iyi sığanı seçilir (ChooseShelter)
            var hqNeighbors = new HashSet<string>(Map.Region(hq).Neighbors);
            var shelters = ShelterSpots();
            ShelterPoint = shelters[0].Point;
            var slots = LegendSlots(shelters);
            List<string> order = null;
            // Birinci tur: her bölge adını (ve altındakileri) kendi bölgesinde ister; ilk sırada bile buna sığmayan bölgenin adı
            // taşabilir. Karargâh birinci turda adını ve cephe bloğunu hiç bırakmaz: adı kendi karasında, cephe bloğu başka bölgenin
            // karasında değil ve şeridi kendi konturunu kesmiyor (oyuncunun ilk ekranı). Üstüne üç sıkı kademe (HqFull: figür
            // içeride, bütün blok ve raptiye kendi karasında, blok kablo ipine değmiyor; HqFigure: figür ve raptiye; HqFigureOnly:
            // yalnız figür) sığdıkça tutulur; S-07 buna göre seçilir. İkinci tur (birinci hiçbir lejant yerinde bitmezse): eski kural, yalnız
            // çakışmasızlık. (Birinci tur süre için kısa tutulur: yer başına en çok 24 baştan yerleştirme.) Üçüncü tur (ikinci de
            // bitmezse): ikinci turun kuralı, adın hücre merkezinden daha uzağa kayabildiği geniş seçeneklerle (tek sözcüklü uzun ad dar
            // bölgede iki satıra bölünemez; ör. küçük ve komşu iki bölge). İlk iki turda biten yerleşim bundan hiç etkilenmez.
            for (int pass = 0; pass < 3; pass++)
            {
                if (pass == 2) foreach (var id in start) options[id] = LabelOptions(Map.Region(id), wide: true);
                for (int slot = 0; slot < slots.Count; slot++)
                {
                    var shelter = ChooseShelter(shelters, slots[slot], options[hq], pass == 0, out int hqLevel);
                    SetFixed(ShelterBox(shelter), slots[slot]);
                    // Karargâh sıkı kademedeyken birinci tur bu yerde bitmezse karargâh genel kurala inip aynı yerde bir kez daha
                    // denenir (sıradaki lejant yerine ve ikinci tura geçmeden)
                    for (bool again = true; again; )
                    {
                        again = false;
                        order = new List<string>(start);
                        // Gevşetme kademesi (bölge başına): 0 ad ve cephe bloğu kendi yerinde, 1 cephe bloğu serbest, 2 ad da serbest
                        var relax = new Dictionary<string, int>();
                        for (int attempt = 0; attempt <= (pass == 0 ? 24 : MaxRestarts); attempt++)
                        {
                            Reset();
                            string failed = null;
                            foreach (var id in order)
                            {
                                relax.TryGetValue(id, out int level);
                                var p = Place(Map.Region(id), options[id], strict: true, inRegionOnly: pass == 0 && level < 2, ownLandFront: pass == 0 && level < 1,
                                    hqLevel: id == hq ? hqLevel : HqLoose);
                                if (p == null) { failed = id; break; }
                                Commit(p);
                            }
                            Restarts++;
                            if (failed == null) { Clean = true; FirstPass = pass == 0; ShelterPoint = shelter; Restarts--; return; }
                            if (pass == 0)
                            {
                                // Sığmayan bölge bir kademe gevşer (ilk sıradaysa yerinde, değilse öne alınarak); tam gevşemişken ilk
                                // sırada bile sığmıyorsa sabitler (lejant yeri) engel: sıradaki yer. Karargâh adını ve cephe bloğunu bırakmaz.
                                relax.TryGetValue(failed, out int lv);
                                // Karargâh yalnız sıkı kademelerinden birinci turun kuralına iner (ad ve cephe bloğu kendi karasında kalır),
                                // ilk sıradayken; değilse önce öne alınır. Kademe inişi deneme sayılmaz: öteki bölgelerin gevşeme eşiği değişmesin.
                                if (failed == hq && hqLevel < HqLoose && order[0] == failed) { hqLevel++; attempt--; continue; }
                                if (failed == hq) lv = 2;
                                if (order[0] == failed && lv >= 2) break;
                                bool relaxNow = lv < 2 && (order[0] == failed || attempt >= 4);
                                // Karargâhın komşusu gevşemeden önce karargâh en sıkı kademesinden iner (figür içeride kalır): bütün bloğu
                                // kendi karasında isteyen karargâh komşunun adını ve cephe bloğunu kendi karasından itmesin (genel ölçüler)
                                if (relaxNow && hqLevel < HqFigure && hqNeighbors.Contains(failed)) { hqLevel++; relaxNow = false; }
                                if (relaxNow) relax[failed] = lv + 1;
                                if (order[0] == failed) continue;
                            }
                            // Sığmayan bölge sıranın başına
                            order.Remove(failed);
                            order.Insert(0, failed);
                        }
                        if (pass == 0 && hqLevel < HqLoose) { hqLevel = HqLoose; again = true; }
                    }
                }
            }

            // En az çakışmalı yerleşim, ilk lejant yeriyle ve S-07'nin ilk adayıyla (testler bunu yakalar)
            Clean = false;
            SetFixed(ShelterBox(shelters[0].Point), slots[0]);
            ShelterPoint = shelters[0].Point;
            Reset();
            foreach (var id in order)
                Commit(Place(Map.Region(id), options[id], strict: true) ?? Place(Map.Region(id), options[id], strict: false));
            // Son sıra her bölgeyi kurala uygun yerleştirmiş olabilir: çakışma yoksa yerleşim temizdir
            Clean = Overlaps().Count == 0 && boxes.TrueForAll(b => b.Region == null || NeverShown(b, hq) || Inside(b.Rect));
        }

        Box ShelterBox(Vector2 point) => new Box(null, Kind.Shelter, Centered(point, Sizes.Shelter));

        /// <summary>
        /// S-07'nin olası yerleri, tercih sırasıyla: karargâhın iç hücreleri (<see cref="MapRegion.Inner"/>, en içteki önce; ilki
        /// <see cref="WorldMap.HqCell"/>) ve her birinde merkez ile on iki kayma (24–52 px). Kutusu karargâhın karasında ve harita alanında
        /// olmayan yer atlanır. <see cref="Spot.Clear"/>: kutu <see cref="Metrics.ShelterContourPad"/> kadar büyütülünce köşeleri ve
        /// kenar ortaları da karargâhta (fiş konturun ve kıyının üstüne binmez), karargâhın komşu sınırlarındaki aday oklarına
        /// (<see cref="FrontierArrows"/>; karargâh özgürken komşu aday olunca çizilir) ve kablo iplerine değmiyor; uymayan aday
        /// ancak hiçbir uyan sığmazsa kullanılır. <see cref="Spot.Score"/>: iç sıra ve kayma (aday seçiminde ceza), uymayana +100.
        /// </summary>
        List<Spot> ShelterSpots()
        {
            var m = Sizes;
            var list = new List<Spot>();
            Vector2[] offsets = { Vector2.zero, new Vector2(0, -26), new Vector2(0, 26), new Vector2(-24, 0), new Vector2(24, 0),
                new Vector2(-48, 0), new Vector2(48, 0), new Vector2(0, -52), new Vector2(0, 52), new Vector2(-36, -26), new Vector2(36, -26),
                new Vector2(-36, 26), new Vector2(36, 26) };
            var region = Map.Region(hq);
            var arrows = FrontierArrows(hq);
            for (int rank = 0; rank < region.Inner.Count; rank++)
            {
                var center = GeneratedMapGeometry.Center(Map.Cells[region.Inner[rank]]);
                foreach (var off in offsets)
                {
                    var p = center + off;
                    var rect = Centered(p, m.Shelter);
                    if (RegionAt(p) != hq || !Inside(rect) || Coverage(rect, hq, 0).own < 0.95f) continue;
                    bool clear = PaddedInside(rect, m.ShelterContourPad, hq) && !arrows.Exists(a => Touch(a, rect, m.Gap)) && !CableRopeHits(rect);
                    list.Add(new Spot { Point = p, Rect = rect, Scale = 1, Clear = clear, Score = rank * ShelterRankCost + off.magnitude / 26 + (clear ? 0 : ShelterUnclearCost) });
                }
            }
            if (list.Count == 0) list.Add(new Spot { Point = geometry.ShelterPoint, Rect = Centered(geometry.ShelterPoint, m.Shelter), Scale = 1 });
            // Kararlı sıralama: ceza, sonra üretim sırası
            var indexed = new List<(Spot s, int i)>(list.Count);
            for (int i = 0; i < list.Count; i++) indexed.Add((list[i], i));
            indexed.Sort((a, b) => a.s.Score != b.s.Score ? a.s.Score.CompareTo(b.s.Score) : a.i.CompareTo(b.i));
            list.Clear();
            foreach (var x in indexed) list.Add(x.s);
            return list;
        }

        /// <summary>S-07 aday seçiminde iç sıranın bir adımının cezası (karargâhın ortasına yakın yer önce).</summary>
        const float ShelterRankCost = 1.5f;
        /// <summary>Konturdan payı, ok ya da kablo kuralına uymayan S-07 adayının cezası (uyan her adaydan sonra gelir).</summary>
        const float ShelterUnclearCost = 100;
        /// <summary>S-07 cephe şeridi ya da saha işaretleriyle aynı satırda yapışıkken cezası (ikinci adımda).</summary>
        const float ShelterRowCost = 3;

        /// <summary>
        /// Kutu <paramref name="pad"/> kadar büyütülünce dört köşesi ve dört kenar ortası (kesin halkalarla) <paramref name="id"/>
        /// bölgesinde mi?
        /// </summary>
        bool PaddedInside(Rect r, float pad, string id)
        {
            float x0 = r.xMin - pad, x1 = r.xMax + pad, y0 = r.yMin - pad, y1 = r.yMax + pad, xm = (x0 + x1) * 0.5f, ym = (y0 + y1) * 0.5f;
            return RegionAt(new Vector2(x0, y0)) == id && RegionAt(new Vector2(x1, y0)) == id && RegionAt(new Vector2(x0, y1)) == id
                && RegionAt(new Vector2(x1, y1)) == id && RegionAt(new Vector2(xm, y0)) == id && RegionAt(new Vector2(xm, y1)) == id
                && RegionAt(new Vector2(x0, ym)) == id && RegionAt(new Vector2(x1, ym)) == id;
        }

        /// <summary>
        /// Bölgenin kara komşularına giden aday oklarının kutuları (<c>GeneratedMapRenderer.SetFrontier</c>'ın çizdiği gövde ve baş,
        /// çizgi kalınlığı payıyla): bölge özgürken komşu aday olunca ortak sınırın ortadaki kenarında durur. Haritadan hesaplanır,
        /// durumdan bağımsızdır (her kara komşusu için).
        /// </summary>
        List<Rect> FrontierArrows(string id)
        {
            var m = Sizes;
            var list = new List<Rect>();
            float half = m.FrontierLength * 0.5f, head = m.FrontierHead, pad = m.FrontierWidth * 0.5f + 1;
            foreach (var l in Map.Links)
            {
                if (l.Cable) continue;
                string other = l.A == id ? l.B : l.B == id ? l.A : null;
                if (other == null) continue;
                var edge = geometry.MiddleEdge(id, other, out int fromCell, out int toCell);
                if (edge == null) continue;
                var mid = geometry.EdgeMiddle(edge.V1, edge.V2);
                var d = (GeneratedMapGeometry.Center(Map.Cells[toCell]) - GeneratedMapGeometry.Center(Map.Cells[fromCell])).normalized;
                var normal = new Vector2(-d.y, d.x);
                var tip = mid + d * half;
                Vector2 p0 = mid - d * half, w1 = tip - d * head + normal * head * 0.7f, w2 = tip - d * head - normal * head * 0.7f;
                var r = Rect.MinMaxRect(Mathf.Min(Mathf.Min(p0.x, tip.x), Mathf.Min(w1.x, w2.x)), Mathf.Min(Mathf.Min(p0.y, tip.y), Mathf.Min(w1.y, w2.y)),
                    Mathf.Max(Mathf.Max(p0.x, tip.x), Mathf.Max(w1.x, w2.x)), Mathf.Max(Mathf.Max(p0.y, tip.y), Mathf.Max(w1.y, w2.y)));
                list.Add(Rect.MinMaxRect(r.xMin - pad, r.yMin - pad, r.xMax + pad, r.yMax + pad));
            }
            return list;
        }

        /// <summary>Kutu bir kablo ipine (sarkık ip, iki doğru parçasıyla: uç → sarkmış orta → uç) değiyor mu?</summary>
        bool CableRopeHits(Rect r)
        {
            foreach (var (a, b) in cables)
            {
                float drop = Mathf.Clamp(Vector2.Distance(a, b) * Sizes.CableSag, 6, 22);
                var mid = (a + b) * 0.5f + new Vector2(0, drop * 0.5f);
                if (SegmentHits(a, mid, r) || SegmentHits(mid, b, r)) return true;
            }
            return false;
        }

        /// <summary>
        /// S-07 karargâhın cephe şeridi ya da saha işaretleriyle aynı satırda ve yatayda <see cref="Metrics.ShelterRowGap"/>'ten yakın mı
        /// (dikey örtüşme S-07'nin yarısından çok)? Öyleyse bloğun bir parçası gibi okunur.
        /// </summary>
        bool InFrontRow(Rect shelter, Placement p)
        {
            var m = Sizes;
            FrontRects(p.Label, p.LabelBox, p.StripSide, p.StripOffset, p.ReoccupiedOffset, m, out var strip, out var fields, out _, out _);
            foreach (var r in new[] { strip, fields })
            {
                float overlap = Mathf.Min(r.yMax, shelter.yMax) - Mathf.Max(r.yMin, shelter.yMin);
                float gapX = Mathf.Max(r.xMin - shelter.xMax, shelter.xMin - r.xMax);
                if (overlap > shelter.height * 0.5f && gapX < m.ShelterRowGap) return true;
            }
            return false;
        }

        /// <summary>
        /// Bu lejant yerinde S-07'nin yeri. Önce karargâhın kendisi (ad, ✓✓✓, damga, ilk cephe bloğu, figür) S-07'siz, yalnız
        /// öteki sabitlerle (lejant, örtücü, kablo raptiyesi) yerleşir; bu en iyi yerleşimdir. Onun kutularına ve sabitlere değmeyen,
        /// cephe satırına yapışmayan, kurallara uyan (<see cref="Spot.Clear"/>) adaylardan cezası en küçük olan seçilir. Yoksa her
        /// aday için karargâh S-07 ile yeniden yerleşir; puan: etiket seçeneği, cephe düzeni ve figür puanı + adayın cezası (+ cephe
        /// satırına yapışıksa <see cref="ShelterRowCost"/>); en küçüğü seçilir. <paramref name="strict"/> (birinci tur): ad kendi
        /// karasında, cephe bloğu kendi karasında ya da denizde, şerit konturu kesmiyor; karargâhın sıkı kademeleri sırayla denenir
        /// (<see cref="HqFull"/>, <see cref="HqFigure"/>, <see cref="HqFigureOnly"/>, <see cref="HqLoose"/>), sığan ilk kademe <paramref name="hqLevel"/>'de döner.
        /// Hiçbiri sığmazsa sabitlere değmeyen ilk aday (yoksa ilk aday).
        /// </summary>
        Vector2 ChooseShelter(List<Spot> spots, Rect legend, List<LabelOption> hqOptions, bool strict, out int hqLevel)
        {
            var m = Sizes;
            var region = Map.Region(hq);
            SetFixed(default, legend);
            fixedBoxes.RemoveAt(0);
            var open = spots.FindAll(s => !fixedBoxes.Exists(f => Touch(f.Rect, s.Rect, m.Gap)));
            // Birinci turda önce karargâhın sıkı kademeleri (figür içeride, bütün blok ve raptiye kendi karasında; sonra yalnız figür),
            // sığmazsa birinci turun kuralı; ikinci turda yalnız çakışmasızlık
            for (int level = strict ? HqFull : HqLoose; level <= HqLoose; level++)
            {
                hqLevel = level;
                SetFixed(default, legend);
                fixedBoxes.RemoveAt(0);
                Reset();
                var free = Place(region, hqOptions, strict: true, inRegionOnly: strict, ownLandFront: strict, hqLevel: level);
                float freeQ = lastQuality;
                // S-07 yalnız kısıt ekler: onsuz da sığmıyorsa hiçbir aday sığmaz
                if (free == null) continue;
                var taken = new List<Box>();
                foreach (var b in BoxesOf(free, m)) if (!NeverShown(b, hq)) taken.Add(b);
                foreach (var s in open)
                    if (s.Clear && !taken.Exists(b => Touch(b.Rect, s.Rect, m.Gap)) && !InFrontRow(s.Rect, free)) return s.Point;
                // Adaylar ceza sırasıyla denenir. Budama sezgiseldir: S-07'siz puan (freeQ) çoğu zaman alt sınırdır, ama S-07
                // karargâha daha geç bir etiket seçeneği seçtirince cephe ve figür puanı düşebilir; budamasız arama aynı ölçüleri
                // (Mono aynası 600 tohum) yaklaşık 1,6 kat sürede veriyor
                Vector2? best = null;
                float bestScore = float.MaxValue;
                float floor = freeQ;
                foreach (var s in open)
                {
                    if (floor + s.Score >= bestScore) break;
                    SetFixed(new Box(null, Kind.Shelter, s.Rect), legend);
                    Reset();
                    var p = Place(region, hqOptions, strict: true, inRegionOnly: strict, ownLandFront: strict, hqLevel: level);
                    if (p == null) continue;
                    float score = lastQuality + s.Score + (InFrontRow(s.Rect, p) ? ShelterRowCost : 0);
                    if (score < bestScore) { bestScore = score; best = s.Point; }
                }
                if (best != null) return best.Value;
            }
            hqLevel = HqLoose;
            return open.Count > 0 ? open[0].Point : spots[0].Point;
        }

        void SetFixed(Box shelter, Rect legend)
        {
            var m = Sizes;
            Legend = legend;
            fixedBoxes.Clear();
            fixedBoxes.Add(shelter);
            if (legend.width > 0 && legend.height > 0) fixedBoxes.Add(new Box(null, Kind.Legend, legend));
            foreach (var o in m.Occluders) fixedBoxes.Add(new Box(null, Kind.Occluder, o));
            foreach (var (a, b) in cables)
            {
                fixedBoxes.Add(new Box(null, Kind.CablePin, Centered(a, Vector2.one * m.CablePin)));
                fixedBoxes.Add(new Box(null, Kind.CablePin, Centered(b, Vector2.one * m.CablePin)));
            }
        }

        /// <summary>
        /// Lejantın aday köşeleri: altında kara payı <see cref="Metrics.LegendLand"/>'ı aşmayanlar önce (aralarında sahnedeki yer
        /// önce), sonra kara payı en az olan (eşitlik payı 5 %: sahnedeki yer önce). Örtücünün altında kalanlar ve kablo
        /// raptiyesine ya da çizgisine <see cref="Metrics.LegendCableGap"/>'ten yakın olanlar atlanır; S-07 adaylarının hepsine değen köşe de. Yerleşim ilkinde sığmazsa
        /// sıradakiyle yeniden dener.
        /// </summary>
        List<Rect> LegendSlots(List<Spot> shelters)
        {
            var m = Sizes;
            var home = m.Legend;
            var list = new List<Rect>();
            if (home.width <= 0 || home.height <= 0) { list.Add(default); return list; }
            if (!m.MoveLegend) { list.Add(home); return list; }
            float w = home.width, h = home.height, W = (float)WorldMap.Width, H = (float)WorldMap.Height, e = m.Margin;
            var slots = new[]
            {
                home,
                new Rect(e, H - e - h, w, h),
                new Rect(e, e, w, h),
                new Rect(W - e - w, e, w, h),
                new Rect(W - e - w, H - e - h, w, h),
            };
            var scored = new List<(Rect r, int bucket, float land, int i)>();
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                // S-07 lejanttan sonra seçilir: köşe ancak bütün S-07 adaylarına değiyorsa elenir
                if (shelters.TrueForAll(s => Touch(slot, s.Rect, m.Gap))) continue;
                bool blocked = false;
                // Örtücü lejantın onda birinden çoğunu kapatıyorsa (kalemin ucu sahnedeki yerde de lejantın köşesine değer)
                foreach (var o in m.Occluders)
                {
                    float ow = Mathf.Min(slot.xMax, o.xMax) - Mathf.Max(slot.xMin, o.xMin), oh = Mathf.Min(slot.yMax, o.yMax) - Mathf.Max(slot.yMin, o.yMin);
                    if (ow > 0 && oh > 0 && ow * oh > 0.1f * slot.width * slot.height) { blocked = true; break; }
                }
                var wide = new Rect(slot.x - m.LegendCableGap, slot.y - m.LegendCableGap, slot.width + 2 * m.LegendCableGap, slot.height + 2 * m.LegendCableGap);
                foreach (var (a, b) in cables) if (SegmentHits(a, b, wide)) { blocked = true; break; }
                if (blocked) continue;
                float land = Land(slot);
                scored.Add((slot, land <= m.LegendLand ? 0 : 1, land + (i == 0 ? 0 : 0.05f), i));
            }
            scored.Sort((a, b) => a.bucket != b.bucket ? a.bucket.CompareTo(b.bucket)
                : a.bucket == 0 ? a.i.CompareTo(b.i)
                : a.land != b.land ? a.land.CompareTo(b.land) : a.i.CompareTo(b.i));
            foreach (var x in scored) list.Add(x.r);
            if (list.Count == 0) list.Add(home);
            return list;
        }

        float Land(Rect r)
        {
            int land = 0, n = 0;
            for (float y = r.yMin + GridStep * 0.5f; y < r.yMax; y += GridStep)
                for (float x = r.xMin + GridStep * 0.5f; x < r.xMax; x += GridStep)
                {
                    n++;
                    if (GridRegion(new Vector2(x, y)) != null) land++;
                }
            return n > 0 ? land / (float)n : 0;
        }

        void Reset()
        {
            placements.Clear();
            boxes.Clear();
            boxes.AddRange(fixedBoxes);
        }

        void Commit(Placement p)
        {
            placements[p.Id] = p;
            boxes.AddRange(BoxesOf(p, Sizes));
        }

        /// <summary>
        /// Etiket seçenekleri, puan sırasıyla. Önce ad ve altındakilerin (yüzde, ADAY, ✓✓✓) hepsi kendi bölgesinde kalanlar
        /// (<see cref="Metrics.OwnTolerance"/>; kıyıya ve komşu bölgeye taşan ad okunmaz), aralarında: punto kademesi, iç sıra
        /// (<see cref="MapRegion.Inner"/>), kaydırma; sığıyorsa tek satır iki satırdan önce.
        /// </summary>
        static readonly float[] Dxs = { 0, -16, 16, -32, 32 }, Dys = { -20, -6, -34, 8, -46 };
        static readonly float[] WideDxs = { 0, -16, 16, -32, 32, -48, 48, -64, 64, -80, 80 }, WideDys = { -20, -6, -34, 8, -46, 22, -60 };

        List<LabelOption> LabelOptions(MapRegion r, bool wide = false)
        {
            var m = Sizes;
            var list = new List<LabelOption>();
            // Etiket + altı bloğunun ortası hücre merkezine gelsin diye ad biraz yukarıda (ideal dy −20); kaydırma kıyıdaki
            // hücrede adı karadan çıkarmasın diye küçük. Geniş seçenekler (wide) yalnız üçüncü turda: kayma puana eklenir.
            float[] dxs = wide ? WideDxs : Dxs;
            float[] dys = wide ? WideDys : Dys;
            for (int rank = 0; rank < r.Inner.Count; rank++)
            {
                int cell = r.Inner[rank];
                var center = GeneratedMapGeometry.Center(Map.Cells[cell]);
                for (int tier = 0; tier < m.FontTiers.Length; tier++)
                {
                    var twoLines = LabelBoxOf(r.Id, m.FontTiers[tier], m.MaxLineWidth);
                    var oneLine = LabelBoxOf(r.Id, m.FontTiers[tier], SingleLine);
                    bool single = m.MaxSingleLine > 0 && oneLine != twoLines && oneLine.x <= m.MaxSingleLine;
                    for (int variant = single ? 0 : 1; variant < 2; variant++)
                    {
                        var box = variant == 0 ? oneLine : twoLines;
                        foreach (float dy in dys)
                            foreach (float dx in dxs)
                            {
                                var point = center + new Vector2(dx, dy);
                                var label = LabelRect(point, box, m);
                                UnderRects(point, box, m, out var percent, out var candidate, out var ticks);
                                bool isHq = r.Id == hq;
                                if (!Inside(label) || !Inside(ticks) || !isHq && (!Inside(percent) || !Inside(candidate))) continue;
                                // Ad kutusu daraltılmaz: yazı bölge sınırına (ve onun hemen içindeki kontura) değmesin
                                float outLabel = Outside(label, r.Id, 0);
                                // Karargâhın yüzdesi ve ADAY'ı hiç görünmez (yalnız ✓✓✓)
                                float outUnder = isHq ? Outside(ticks, r.Id) : Mathf.Max(Outside(percent, r.Id), Mathf.Max(Outside(candidate, r.Id), Outside(ticks, r.Id)));
                                // Karargâhın adı (oyuncunun ilk ekranı) koyu konturdan da uzak: kutusu HqLabelPad büyütülünce köşeleri ve kenar
                                // ortaları karargâhta (kontur sınırın 5 px içinde çizilir; yazı çizgiye değmesin)
                                int bucket = outLabel <= m.OwnTolerance && outUnder <= m.OwnTolerance && (!isHq || PaddedInside(label, m.HqLabelPad, r.Id)) ? 0 : 1;
                                float score = bucket * 1000 + tier * 3 + rank * 4 + Mathf.Abs(dx) / 16 + Mathf.Abs(dy + 20) / 14
                                    + (single && variant == 1 ? 1.5f : 0)
                                    + outLabel * 25 + outUnder * 10 + CableHits(label) * 6;
                                list.Add(new LabelOption
                                {
                                    Cell = cell, Rank = rank, Tier = tier, Bucket = bucket, Point = point, Box = box, Score = score,
                                    MaxLine = variant == 0 ? SingleLine : m.MaxLineWidth,
                                    Label = label, Percent = percent, Candidate = candidate, Ticks = ticks,
                                });
                            }
                    }
                }
            }
            // Kararlı sıralama: puan, sonra üretim sırası
            var indexed = new List<(LabelOption o, int i)>(list.Count);
            for (int i = 0; i < list.Count; i++) indexed.Add((list[i], i));
            indexed.Sort((a, b) => a.o.Score != b.o.Score ? a.o.Score.CompareTo(b.o.Score) : a.i.CompareTo(b.i));
            list.Clear();
            foreach (var x in indexed) list.Add(x.o);
            return list;
        }

        /// <summary>Kutunun kurala göre çakışma sayısı (harita dışı 1 sayılır); <paramref name="stopAt"/>'ta durur.</summary>
        int Conflicts(in Box b, List<Box> against, int stopAt)
        {
            int n = 0;
            foreach (var o in against)
                if (Exclusive(b, o, hq) && Touch(b.Rect, o.Rect, Sizes.Gap) && ++n >= stopAt) return n;
            return n;
        }

        int Conflicts(in Box b, List<Box> own, bool strict)
        {
            int stop = strict ? 1 : int.MaxValue;
            int n = Inside(b.Rect) ? 0 : 1;
            if (strict && n > 0) return n;
            n += Conflicts(b, boxes, stop - n);
            if (strict && n > 0) return n;
            return n + Conflicts(b, own, strict ? 1 : int.MaxValue);
        }

        /// <summary>
        /// Karargâhın (oyuncunun ilk ekranı) birinci turdaki kademeleri: <see cref="HqFull"/> figür içeride, bütün cephe bloğu ve
        /// raptiye kendi karasında (başı konturun içinde), blok kablo ipine değmiyor; <see cref="HqFigure"/> figür içeride ve raptiye
        /// konturun içinde; <see cref="HqFigureOnly"/> yalnız figür içeride; <see cref="HqLoose"/>
        /// birinci turun genel kuralı (ad kendi karasında, cephe bloğu başka karada değil, şerit konturu kesmiyor).
        /// </summary>
        const int HqFull = 0, HqFigure = 1, HqFigureOnly = 2, HqLoose = 3;

        /// <param name="hqLevel">Karargâhın kademesi (yalnız birinci turda, <paramref name="ownLandFront"/> ile; bkz. <see cref="HqFull"/>).</param>
        Placement Place(MapRegion r, List<LabelOption> options, bool strict, bool inRegionOnly = false, bool ownLandFront = false, int hqLevel = HqLoose)
        {
            var m = Sizes;
            // Damga ve ekip adayları: yerleşmiş kutulara göre bir kez süzülür (etiket seçeneğinden bağımsız)
            var stamps = StampSpots(r, strict);
            // Karargâhın sıkı kademeleri (oyuncunun ilk ekranı): figürü içeride, bütün cephe bloğu kendi karasında
            int level = r.Id == hq && ownLandFront ? hqLevel : HqLoose;
            bool hqFigure = level <= HqFigureOnly, hqPin = level <= HqFigure, hqBlock = level <= HqFull;
            var teams = TeamSpots(r, strict, hqFigure);
            var own = new List<Box>(12);
            Placement best = null;
            int bestTotal = int.MaxValue;
            float bestScore = float.MaxValue;
            int tried = 0, deep = 0;
            foreach (var o in options)
            {
                if (!strict && ++tried > 60) break;
                // Seçenekler kendi bölgesinde kalanlar önce sıralı: ilk taşan seçenekte dur
                if (inRegionOnly && o.Bucket != 0) break;
                own.Clear();
                int total = 0;
                var label = new Box(r.Id, Kind.Label, o.Label);
                // Karargâhın yüzdesi ve ADAY'ı hiç görünmez: yalnız ad ve ✓✓✓ denetlenir
                var parts = r.Id == hq
                    ? new[] { label, new Box(r.Id, Kind.Ticks, o.Ticks) }
                    : new[] { label, new Box(r.Id, Kind.Percent, o.Percent), new Box(r.Id, Kind.Candidate, o.Candidate), new Box(r.Id, Kind.Ticks, o.Ticks) };
                bool fail = false;
                foreach (var b in parts)
                {
                    int c = Conflicts(b, own, strict);
                    if (strict && c > 0) { fail = true; break; }
                    total += c;
                    own.Add(b);
                }
                if (fail) continue;
                // Süre sınırı: adı çakışmasız ilk N seçenekte damga/cephe/figür bulunamazsa bölge sığmamış sayılır (baştan
                // yerleşimde öne alınır)
                if (strict && ++deep > (inRegionOnly ? 60 : 160)) break;

                var p = new Placement
                {
                    Id = r.Id, Label = o.Point, FontSize = m.FontTiers[o.Tier], MaxLineWidth = o.MaxLine, LabelBox = o.Box, LabelCell = o.Cell,
                    LabelInRegion = o.Bucket == 0, HadInRegionOption = options.Count > 0 && options[0].Bucket == 0, FrontOwnLand = ownLandFront, HqStrict = hqBlock,
                };
                if (!ChooseStamp(r, p, stamps, own, strict, ref total)) continue;
                if (!ChooseFront(r, p, own, strict, ownLandFront, hqBlock, ref total, hqPin)) continue;
                if (!ChooseTeam(r, p, teams, own, strict, hqFigure, ref total)) continue;
                if (strict) { lastQuality = o.Score + lastFrontScore + lastTeamScore; return p; }
                float score = o.Score;
                if (total < bestTotal || total == bestTotal && score < bestScore) { bestTotal = total; bestScore = score; best = p; }
                if (total == 0) break;
            }
            return best;
        }

        /// <summary>Son katı yerleşimin puanı (etiket seçeneği + cephe düzeni + figür; küçük iyi): S-07 seçimi karşılaştırır.</summary>
        float lastQuality, lastFrontScore, lastTeamScore;

        List<Spot> StampSpots(MapRegion r, bool strict)
        {
            var m = Sizes;
            var list = new List<Spot>();
            Vector2[] offsets = { Vector2.zero, new Vector2(24, 0), new Vector2(-24, 0), new Vector2(0, 24), new Vector2(0, -24) };
            var none = new List<Box>(0);
            for (int t = 0; t < m.StampTiers.Length; t++)
                foreach (int cell in r.Inner)
                {
                    var center = GeneratedMapGeometry.Center(Map.Cells[cell]);
                    foreach (var off in offsets)
                    {
                        var point = center + off;
                        var rect = StampRect(point, m.StampTiers[t], m);
                        int c = Conflicts(new Box(r.Id, Kind.Stamp, rect), none, strict);
                        if (strict && c > 0) continue;
                        list.Add(new Spot { Point = point, Scale = m.StampTiers[t], Rect = rect, Score = c * 1000 + t * 8 + Outside(rect, r.Id) * 20 + off.magnitude / 48 });
                    }
                }
            return list;
        }

        /// <param name="hqFirst">Karargâh, birinci tur: taban elipsi <see cref="Metrics.HqFigureOwn"/>'dan az kendi karasında ya da
        /// kutusu <see cref="Metrics.HqFigureForeign"/>'dan çok başka karada olan nokta denenmez.</param>
        List<Spot> TeamSpots(MapRegion r, bool strict, bool hqFirst = false)
        {
            // Aday noktalar ve bölgeye bağlı puanları bir kez; yerleşmiş kutulara göre süzme her yerleşimde
            if (!rawTeams.TryGetValue(r.Id, out var raw)) rawTeams[r.Id] = raw = RawTeamSpots(r);
            var list = new List<Spot>(raw.Count);
            var none = new List<Box>(0);
            foreach (var s in raw)
            {
                if (hqFirst && (s.Own < Sizes.HqFigureOwn || s.Foreign > Sizes.HqFigureForeign)) continue;
                int c = Conflicts(new Box(r.Id, Kind.Team, s.Rect), none, strict);
                if (strict && c > 0) continue;
                var spot = s;
                spot.Score += c * 1000;
                list.Add(spot);
            }
            return list;
        }

        readonly Dictionary<string, List<Spot>> rawTeams = new Dictionary<string, List<Spot>>();

        List<Spot> RawTeamSpots(MapRegion r)
        {
            var m = Sizes;
            // Hücre merkezleri ve aynı bölgenin komşu iki hücresinin ortası (ikisi de bölgenin içinde)
            var list = new List<Spot>();
            void Try(Vector2 a)
            {
                // Bölge sınırından en az 8 px içeride (isabet çokgeni ve dünya dönüşümünün yuvarlaması sınırı aşmasın)
                if (RegionAt(a) != r.Id) return;
                for (int k = 0; k < 8; k++)
                {
                    float angle = k * Mathf.PI / 4;
                    if (RegionAt(a + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 8) != r.Id) return;
                }
                var rect = TeamRect(a, m);
                // Taban elipsi kendi karasında (kıyıdan ve sınırdan taşmaz); altında kablo raptiyesi ya da çizgisi olmasın
                var radii = m.FigureBase * 0.5f;
                float own = EllipseOwn(a, radii, r.Id);
                // Karargâhta konturdan payla (sıkı kademelerin figür kuralı bu payı kullanır)
                float ownPad = r.Id == hq ? EllipseOwn(a, radii + Vector2.one * m.HqFigurePad, r.Id) : own;
                var cover = Coverage(rect, r.Id);
                float score = (own < m.FigureOwnShare ? 60 : 0) + (1 - own) * 20 + (1 - cover.own) * 2;
                var core = Centered(a, m.FigureBase * 0.7f);
                foreach (var (p, q) in cables)
                {
                    if (InEllipse(p, a, radii + Vector2.one * (m.CablePin * 0.5f)) || InEllipse(q, a, radii + Vector2.one * (m.CablePin * 0.5f))) score += 40;
                    else if (SegmentHits(p, q, core)) score += 10;
                }
                list.Add(new Spot { Point = a, Scale = 1, Rect = rect, Score = score, Own = ownPad, Foreign = cover.foreign });
            }
            Vector2[] nudges = { new Vector2(22, 0), new Vector2(-22, 0), new Vector2(0, 20), new Vector2(0, -20) };
            // Karargâhta (figürü birinci turda içeride olmalı) her iç hücrenin çevresinde 22 × 20 px'lik sık bir ızgara da: ad, cephe
            // bloğu ve S-07 hücre merkezlerini doldurunca figür aradaki boşluğu bulsun (yalnız tabanı içeride kalan noktalar eklenir)
            if (r.Id == hq)
                foreach (int cell in r.Inner)
                    for (int iy = -3; iy <= 3; iy++)
                        for (int ix = -3; ix <= 3; ix++)
                        {
                            if (Mathf.Abs(ix) <= 1 && Mathf.Abs(iy) <= 1 && (ix == 0 || iy == 0)) continue;
                            var a = GeneratedMapGeometry.Center(Map.Cells[cell]) + new Vector2(ix * 22, iy * 20);
                            if (RegionAt(a) == r.Id && EllipseOwn(a, m.FigureBase * 0.5f + Vector2.one * m.HqFigurePad, r.Id) >= m.HqFigureOwn) Try(a);
                        }
            foreach (int cell in r.Inner)
            {
                var c = Map.Cells[cell];
                Try(GeneratedMapGeometry.Center(c));
                foreach (var n in nudges) Try(GeneratedMapGeometry.Center(c) + n);
                for (int k = 0; k < 3; k++)
                {
                    int nb = c.Neighbor[k];
                    if (nb >= 0 && Map.Cells[nb].Land && Map.Cells[nb].Region == r.Id)
                        Try((GeneratedMapGeometry.Center(c) + GeneratedMapGeometry.Center(Map.Cells[nb])) * 0.5f);
                }
            }
            return list;
        }

        static bool InEllipse(Vector2 p, Vector2 center, Vector2 radii)
        {
            float u = (p.x - center.x) / radii.x, v = (p.y - center.y) / radii.y;
            return u * u + v * v <= 1;
        }

        bool ChooseStamp(MapRegion r, Placement p, List<Spot> spots, List<Box> own, bool strict, ref int total)
        {
            int bestC = int.MaxValue;
            float bestScore = float.MaxValue;
            Spot best = default;
            foreach (var s in spots)
            {
                int c = Conflicts(new Box(r.Id, Kind.Stamp, s.Rect), own, strict ? 1 : int.MaxValue);
                if (strict && c > 0) continue;
                float score = s.Score + Vector2.Distance(s.Point, p.Label) / 150;
                if (c < bestC || c == bestC && score < bestScore) { bestC = c; bestScore = score; best = s; }
            }
            if (bestC == int.MaxValue) return false;
            p.Stamp = best.Point;
            p.StampScale = best.Scale;
            total += bestC + (strict ? 0 : (int)(best.Score / 1000));
            own.Add(new Box(r.Id, Kind.Stamp, best.Rect));
            return true;
        }

        /// <param name="ownLand">Birinci tur: şerit ve saha işaretleri başka bölgenin karasına düşen ya da şeridi (yüzdesiyle) kendi
        /// konturunu kesen düzenler denenmez.</param>
        /// <param name="hqFirst">Karargâhın sıkı kademesi: ayrıca bütün blok kendi karasında, raptiye karada ve blok kablo ipine
        /// değmiyor.</param>
        /// <summary>Karargâhın birinci turdaki cephe bloğu: şeridin, saha işaretlerinin ve raptiyenin kendi karasındaki en az payı.</summary>
        const float HqStripOwn = 0.98f, HqFieldsOwn = 0.94f;

        /// <summary>
        /// Raptiye başı konturun içinde mi: merkezi ve <see cref="Metrics.PinClearance"/> yarıçaplı çemberin sekiz noktası kendi
        /// bölgesinde (kesin halkalarla). Koyu kontur bölge sınırının <see cref="GeneratedMapGeometry.ContourInset"/> px içinde
        /// çizildiği için yalnız "kendi karasında" yetmez: raptiye çizginin üstüne binerdi.
        /// </summary>
        bool PinInside(Vector2 c, string id)
        {
            if (RegionAt(c) != id) return false;
            float d = Sizes.PinClearance, e = d * 0.7071f;
            return RegionAt(c + new Vector2(d, 0)) == id && RegionAt(c + new Vector2(-d, 0)) == id && RegionAt(c + new Vector2(0, d)) == id
                && RegionAt(c + new Vector2(0, -d)) == id && RegionAt(c + new Vector2(e, e)) == id && RegionAt(c + new Vector2(-e, e)) == id
                && RegionAt(c + new Vector2(e, -e)) == id && RegionAt(c + new Vector2(-e, -e)) == id;
        }

        bool ChooseFront(MapRegion r, Placement p, List<Box> own, bool strict, bool ownLand, bool hqFirst, ref int total, bool hqPinRule = false)
        {
            var m = Sizes;
            float stripWidth = 3 * 44 + 10 + m.FrontPercent.x, half = p.LabelBox.y * 0.5f, w = p.LabelBox.x;
            Vector2 below = ReoccupiedBelow, beside = ReoccupiedBeside(m);
            // Blok yüksekliği şeridin üstünden: sahnedeki düzende YENİDEN İŞGAL'in altı, sıkı düzende saha işaretlerinin altı
            float tallBelow = below.y + m.Reoccupied.y * 0.5f, tallBeside = Mathf.Max(93, beside.y + m.Reoccupied.y * 0.5f);
            var R = LiberationRegionMarks.StripSide.Right;
            var L = LiberationRegionMarks.StripSide.Left;
            var choices = new (LiberationRegionMarks.StripSide side, Vector2 offset, Vector2 reocc, float cost)[]
            {
                (LiberationRegionMarks.StripSide.Below, Vector2.zero, below, 0),
                (LiberationRegionMarks.StripSide.Below, Vector2.zero, beside, 0.5f),
                (R, new Vector2(18, -half - 14), below, 1),
                (R, new Vector2(18, -half - 14), beside, 1.2f),
                (R, new Vector2(18, -half + 4), beside, 1.3f),
                (L, new Vector2(-stripWidth - 18, -half - 14), below, 1.5f),
                (L, new Vector2(-stripWidth - 18, -half - 14), beside, 1.7f),
                // Adın üstünde (sağ yan + sola kaydırma: şerit adın sol kenarından başlar)
                (R, new Vector2(-w, -half - 10 - tallBeside), beside, 2),
                (R, new Vector2(-w, -half - 10 - tallBelow), below, 2.2f),
                // Adın altında, sağa ya da sola kaydırılmış
                (R, new Vector2(-w + 60, half + 10), beside, 2.5f),
                (R, new Vector2(-w - 60, half + 10), beside, 2.6f),
            };
            var parts = new Box[3];
            int bestC = int.MaxValue;
            float bestScore = float.MaxValue;
            int bestIndex = -1;
            bool bestPinRight = false;
            for (int i = 0; i < choices.Length; i++)
            {
                var ch = choices[i];
                bool defaultRight = ch.side == LiberationRegionMarks.StripSide.Left;
                FrontRects(p.Label, p.LabelBox, ch.side, ch.offset, ch.reocc, defaultRight, m, out var strip, out var fields, out var reocc, out _);
                parts[0] = new Box(r.Id, Kind.FrontStrip, strip);
                parts[1] = new Box(r.Id, Kind.FrontFields, fields);
                parts[2] = new Box(r.Id, Kind.FrontReoccupied, reocc);
                // Bloğun kendi parçaları üst üste binmemeli (raptiye adın yanında, şerit kaydırılınca ona yaklaşabilir)
                if (Touch(reocc, strip, m.Gap) || Touch(reocc, fields, m.Gap)) continue;
                int c = 0;
                foreach (var b in parts)
                {
                    c += Conflicts(b, own, strict);
                    if (strict && c > 0) break;
                }
                if (strict && c > 0) continue;
                // Blok kendi bölgesinde ya da denizde durur, başka bölgenin karasında değil (kimin yüzdesi olduğu karışır);
                // kendi konturunu ve kıyısını kesmez (koyu çizgi yüzde rakamlarını yutar)
                var cs = Coverage(strip, r.Id);
                var cf = Coverage(fields, r.Id);
                var cr = Coverage(reocc, r.Id);
                if (ownLand && (cs.foreign > 0.1f || cf.foreign > 0.1f || Straddles(strip, r.Id))) continue;
                // Şerit ve saha işaretleri kablo ipine değmesin
                bool onCable = CableRopeHits(strip) || CableRopeHits(fields);
                // Karargâh (oyuncunun ilk ekranı) sıkı kademede: bütün blok kendi karasında (şerit ve saha işaretleri kıyıyı ve
                // konturu kesmez, denizde de durmaz), kablo ipine değmiyor
                if (hqFirst && (cs.own < HqStripOwn || cf.own < HqFieldsOwn || onCable)) continue;
                float score = ch.cost + (cs.foreign + cf.foreign) * 30 + cr.foreign * 10
                    + (Straddles(strip, r.Id) ? 6 : 0) + (Straddles(fields, r.Id) ? 4 : 0) + (Straddles(reocc, r.Id) ? 2 : 0)
                    + (2 - cs.own - cf.own) * 1.5f + (onCable ? 2 : 0);
                // Raptiye (kırmızı ipin bölge ucu) kendi karasında: şeride göre yanı (şerit soldaysa sağda) önce; orada karada
                // değilse ya da sığmıyorsa öbür yanı
                for (int k = 0; k < 2; k++)
                {
                    bool right = k == 0 ? defaultRight : !defaultRight;
                    var pin = PinRect(p.Label, p.LabelBox, right, m);
                    if (Touch(pin, strip, m.Gap) || Touch(pin, fields, m.Gap) || Touch(pin, reocc, m.Gap)) continue;
                    int pc = Conflicts(new Box(r.Id, Kind.FrontPin, pin), own, strict);
                    if (strict && pc > 0) continue;
                    bool pinIn = PinInside(pin.center, r.Id);
                    if (hqPinRule && !pinIn) continue;
                    float total2 = score + (pinIn ? 0 : 1) + (k == 1 ? 0.3f : 0);
                    if (c + pc < bestC || c + pc == bestC && total2 < bestScore) { bestC = c + pc; bestScore = total2; bestIndex = i; bestPinRight = right; }
                    if (pc == 0 && pinIn) break;
                }
            }
            if (bestIndex < 0) return false;
            lastFrontScore = bestScore;
            p.StripSide = choices[bestIndex].side;
            p.StripOffset = choices[bestIndex].offset;
            p.ReoccupiedOffset = choices[bestIndex].reocc;
            p.PinRight = bestPinRight;
            total += bestC;
            FrontRects(p.Label, p.LabelBox, p.StripSide, p.StripOffset, p.ReoccupiedOffset, p.PinRight, m, out var s1, out var f1, out var r1, out var p1);
            own.Add(new Box(r.Id, Kind.FrontStrip, s1));
            own.Add(new Box(r.Id, Kind.FrontFields, f1));
            own.Add(new Box(r.Id, Kind.FrontReoccupied, r1));
            own.Add(new Box(r.Id, Kind.FrontPin, p1));
            return true;
        }

        bool ChooseTeam(MapRegion r, Placement p, List<Spot> spots, List<Box> own, bool strict, bool hqFirst, ref int total)
        {
            // Etiketin altı, yakın olan önce (team_anchors.json kuralı)
            var ideal = p.Label + new Vector2(0, p.LabelBox.y * 0.5f + 75);
            int bestC = int.MaxValue;
            float bestScore = float.MaxValue;
            Spot best = default;
            foreach (var s in spots)
            {
                int c = Conflicts(new Box(r.Id, Kind.Team, s.Rect), own, strict ? 1 : int.MaxValue);
                if (strict && c > 0) continue;
                // Adın üstü cezalı (karargâhta değil: altını cephe bloğu doldurunca figür adın üstünde de içeride durabilir)
                float score = s.Score + Vector2.Distance(s.Point, ideal) / 40 + (!hqFirst && s.Point.y < p.Label.y ? 2 : 0);
                if (c < bestC || c == bestC && score < bestScore) { bestC = c; bestScore = score; best = s; }
            }
            if (bestC == int.MaxValue) return false;
            lastTeamScore = bestScore;
            p.Team = best.Point;
            total += bestC + (strict ? 0 : (int)(best.Score / 1000));
            own.Add(new Box(r.Id, Kind.Team, best.Rect));
            return true;
        }

        /// <summary>Kurala göre çakışan kutu çiftleri (boşluk payıyla); temiz yerleşimde boş.</summary>
        public List<(Box a, Box b)> Overlaps(float gap = 0)
        {
            var list = new List<(Box, Box)>();
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                    if (Exclusive(boxes[i], boxes[j], hq) && Touch(boxes[i].Rect, boxes[j].Rect, gap)) list.Add((boxes[i], boxes[j]));
            return list;
        }
    }
}
