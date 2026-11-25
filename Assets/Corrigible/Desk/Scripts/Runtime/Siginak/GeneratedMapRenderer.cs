using System;
using System.Collections.Generic;
using System.Text;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üretilmiş haritanın (anahtar 1) kâğıt çizimi: sığınağın telsiz raporları ve kablo hatlarından kurşun kalemle çizdiği
    /// vaziyet kâğıdı. Harekât başında bir kez kurulur (<see cref="Apply"/>; aynı harita yeniden kurulmaz), sonra yalnız
    /// bölgelerin rengi ve adaylara giden oklar değişir. Katmanlar (alttan üste, sahnede MapRoot altında):
    /// <list type="number">
    /// <item>kıyısız kâğıt (<see cref="paper"/>; dünya haritasının <c>map_base</c> / <c>map_lines</c> dokuları gizlenir),</item>
    /// <item>kıyı halesi: kıyı halkaları 3–4 kez, dıştan içe kalınlaşıp soluklaşan çizgiler; üstünde denize doğru kaydırılmış ince
    /// su çizgileri (eski deniz haritası taraması),</item>
    /// <item>bölge dolguları (Sade isabet dolgusu ve kurtuluş taramaları; hücre çokgenleri <see cref="LiberationRegionMarks"/>'a),</item>
    /// <item>bölge sınırı (açık alt çizgi + koyu çizgi), koyu kıyı çizgisi ve kurşun kalemin ikinci, soluk geçişi; kablo hatları
    /// sarkan kesik ip, uçlarında raptiye (gölge, kırmızı baş, parıltı); adaylara özgür bölgeden giden oklar.</item>
    /// </list>
    /// Etiket, damga, cephe bloğu, ekip ve sığınak yerleşimi <see cref="GeneratedMapLayout"/>'tan (iki dilde ölçülür). Renk ve
    /// ölçüler temadan (<c>map.*</c>, <c>mapLiberation.*</c>, <c>space.mapGen.*</c>). Dünya haritasına dönüşte (<see cref="Clear"/>)
    /// sahnedeki veri ve katmanlar olduğu gibi geri gelir. Hiçbir çizgi tıklama almaz.
    /// </summary>
    public sealed class GeneratedMapRenderer : MonoBehaviour
    {
        [Tooltip("Kıyısız kâğıt (graticule'lü düz kâğıt); yalnız üretilmiş haritada açık.")]
        [SerializeField] RawImage paper;
        [Tooltip("Dünya haritasının pişmiş katmanları (map_base, map_lines): üretilmiş haritada gizlenir.")]
        [SerializeField] GameObject[] worldLayers = Array.Empty<GameObject>();
        [Tooltip("Üretilmiş haritanın kapları (hale, çizgiler, lejantın kablo satırı); dünya haritasında kapalı.")]
        [SerializeField] GameObject[] generatedLayers = Array.Empty<GameObject>();
        [Tooltip("Kıyı halesi, en dıştaki (en kalın, en soluk) önce.")]
        [SerializeField] UILineGraphic[] halo = Array.Empty<UILineGraphic>();
        [Tooltip("Kıyı taraması: denize doğru kaydırılmış ince su çizgileri, kıyıya en yakın önce.")]
        [SerializeField] UILineGraphic[] waterlines = Array.Empty<UILineGraphic>();
        [SerializeField] UILineGraphic borderUnder, border, coast, coastSketch, cable, cableEnds, cablePinShadow, cablePinShine;
        [Tooltip("Adaylara özgür bölgeden giden oklar (kara sınırında ve kablonun aday ucunda).")]
        [SerializeField] UILineGraphic frontier;
        [Tooltip("Lejantın “kablo hattı” satırının simgesi: kesik çizgi ve uçlarındaki raptiyeler.")]
        [SerializeField] UILineGraphic legendCable, legendCablePins;

        // Kat anahtarları (tema anahtar denetimi sabit dizgi ister)
        static readonly string[] HaloWidth = { "space.mapGen.haloWidth[0]", "space.mapGen.haloWidth[1]", "space.mapGen.haloWidth[2]", "space.mapGen.haloWidth[3]" };
        static readonly string[] HaloAlpha = { "space.mapGen.haloAlpha[0]", "space.mapGen.haloAlpha[1]", "space.mapGen.haloAlpha[2]", "space.mapGen.haloAlpha[3]" };
        static readonly string[] WaterOffset = { "space.mapGen.waterlineOffset[0]", "space.mapGen.waterlineOffset[1]", "space.mapGen.waterlineOffset[2]" };
        static readonly string[] WaterWidth = { "space.mapGen.waterlineWidth[0]", "space.mapGen.waterlineWidth[1]", "space.mapGen.waterlineWidth[2]" };
        static readonly string[] WaterAlpha = { "space.mapGen.waterlineAlpha[0]", "space.mapGen.waterlineAlpha[1]", "space.mapGen.waterlineAlpha[2]" };
        // Haritanın masadaki nesnelerin altında kalan yerleri (x, y, genişlik, yükseklik)
        static readonly string[][] Occluders =
        {
            new[] { "space.mapGen.occluderTypewriter[0]", "space.mapGen.occluderTypewriter[1]", "space.mapGen.occluderTypewriter[2]", "space.mapGen.occluderTypewriter[3]" },
            new[] { "space.mapGen.occluderPencil[0]", "space.mapGen.occluderPencil[1]", "space.mapGen.occluderPencil[2]", "space.mapGen.occluderPencil[3]" },
        };

        GeneratedMapGeometry geometry;
        GeneratedMapLayout layout;
        string frontierKey;

        /// <summary>Uygulanan haritanın geometrisi; dünya haritasında null.</summary>
        public GeneratedMapGeometry Geometry => geometry;
        /// <summary>Uygulanan haritanın yerleşimi; dünya haritasında null.</summary>
        public GeneratedMapLayout Layout => layout;

        /// <summary>
        /// Haritayı kurar, yerleşimi hesaplar ve bölge işaretlerine verir. Aynı harita için ikinci çağrı bir şey yapmaz (yalnız
        /// renk değişir). <paramref name="legend"/>: lejantın harita dikdörtgenini veren çağrı (kablo satırı açıldıktan sonra
        /// ölçülür); <paramref name="pixelsPerMeter"/>: figür tabanının harita pikseli.
        /// </summary>
        public void Apply(WorldMap map, IReadOnlyList<LiberationRegionMarks> regions, Func<Rect> legend = null, float pixelsPerMeter = 1600,
            Vector2 shelterSize = default)
        {
            if (map == null) { Clear(regions); return; }
            if (geometry != null && geometry.Map == map) return;
            var t = SiginakTheme.Active;
            geometry = BuildGeometry(map, t);
            frontierKey = null;
            BuildLayers(t);
            Relayout(regions, legend, pixelsPerMeter, shelterSize);
        }

        void BuildLayers(SiginakTheme t)
        {
            foreach (var go in worldLayers) if (go != null) go.SetActive(false);
            foreach (var go in generatedLayers)
            {
                if (go == null) continue;
                go.SetActive(true);
                // Lejant satırı açılınca kutu büyür (ContentSizeFitter): yerleşim yeni boyu görmeli
                if (go.transform.parent is RectTransform parent) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
            if (paper != null) paper.gameObject.SetActive(true);

            DrawLines(t);
            if (frontier != null)
            {
                Style(frontier, t.ColorOf("mapLiberation.frontierArrow"), t.NumberOf("space.mapGen.frontierWidth", 3), null, true);
                frontier.Curve = LineCurve.Polyline;
                frontier.SetPathsTopLeft(null, Vector2.zero);
            }
            StyleLegend(t);
        }

        /// <summary>
        /// Yerleşimi yeniden hesaplar (geometri ve çizgiler aynı kalır) ve bölge işaretlerine verir. Harita uygulandığında
        /// çağrılır; lejant sahne yüklenirken henüz ölçülemediyse (yazılar kurulmadan) katman ilk karede yeniden çağırır.
        /// </summary>
        public void Relayout(IReadOnlyList<LiberationRegionMarks> regions, Func<Rect> legend = null, float pixelsPerMeter = 1600,
            Vector2 shelterSize = default)
        {
            if (geometry == null) return;
            var map = geometry.Map;
            var t = SiginakTheme.Active;
            // Yerleşim: adlar iki dilde ölçülür, aynı konum iki dilde kullanılır
            var metrics = Metrics(regions, t, legend != null ? legend() : default, pixelsPerMeter);
            // S-07 fişi sahnede ölçülür (yerleşim onun büyümesini görsün); pay her yana 4 px
            if (shelterSize != Vector2.zero) metrics.Shelter = shelterSize + Vector2.one * 8;
            var measure = MeasureWith(regions);
            layout = GeneratedMapLayout.Compute(map, geometry, Names(map), measure, metrics);
            if (!layout.Clean) Debug.LogWarning($"[Harita] Yerleşim çakışmasız bulunamadı (tohum {map.Seed}, {layout.Overlaps().Count} çakışma)");

            foreach (var r in regions)
            {
                if (r == null) continue;
                var place = layout.Of(r.RegionId);
                if (place == null) continue;
                r.SetGenerated(geometry.RegionPolygons[r.RegionId], geometry.RegionContours[r.RegionId], place);
            }
        }

        /// <summary>
        /// Haritanın çizim geometrisi, kenar kırma değerleri temadan (<c>space.mapGen.edgeAmplitude</c>, <c>vertexJitter</c>,
        /// <c>coastAmplitude</c>, <c>coastJitter</c>). Oyun ve testler aynı yoldan kurar.
        /// </summary>
        public static GeneratedMapGeometry BuildGeometry(WorldMap map, SiginakTheme t = null)
        {
            t = t != null ? t : SiginakTheme.Active;
            return new GeneratedMapGeometry(map, t.NumberOf("space.mapGen.edgeAmplitude", 0.14f), t.NumberOf("space.mapGen.vertexJitter", 0.06f),
                t.NumberOf("space.mapGen.coastAmplitude", 0.24f), t.NumberOf("space.mapGen.coastJitter", 0.12f));
        }

        /// <summary>
        /// Bölge adlarının her dildeki (<see cref="Loc.Languages"/>) gösterilen hâli (büyük harf): bölgeyi tutan YZ'nin adı
        /// (<see cref="RegionDef.Ai"/>; aşama 3, çevrilmez; haritanın havuz adı etikette görünmez). Yerleşim hepsini ölçer: konum her
        /// dilde aynıdır.
        /// </summary>
        public static Dictionary<string, IReadOnlyList<string>> Names(WorldMap map)
        {
            var names = new Dictionary<string, IReadOnlyList<string>>();
            foreach (var r in map.Regions)
            {
                var list = new string[Loc.Languages.Length];
                for (int i = 0; i < list.Length; i++)
                {
                    string lang = Loc.Languages[i];
                    list[i] = DeskText.Upper(GameData.Region(r.Id).Ai, lang == Loc.Turkish ? DeskLang.Tr : DeskLang.En);
                }
                names[r.Id] = list;
            }
            return names;
        }

        /// <summary>Etiketin TMP'siyle ölçüm (ilk bölge işaretinin ad yazısı; hepsi aynı font).</summary>
        public static GeneratedMapLayout.Measure MeasureWith(IReadOnlyList<LiberationRegionMarks> regions)
        {
            LiberationRegionMarks probe = null;
            foreach (var r in regions) if (r != null) { probe = r; break; }
            if (probe == null) return (text, size) => new Vector2(text.Length * size * 0.75f, size * 1.2f);
            var cache = new Dictionary<(string, float), Vector2>();
            return (text, size) =>
            {
                if (!cache.TryGetValue((text, size), out var v)) cache[(text, size)] = v = probe.MeasureLabel(text, size);
                return v;
            };
        }

        /// <summary>Yerleşimin ölçüleri: punto kademesi ve sınırlar temadan, damga ve yazı boyları sahnedeki işaretlerden.</summary>
        public static GeneratedMapLayout.Metrics Metrics(IReadOnlyList<LiberationRegionMarks> regions, SiginakTheme t, Rect legend, float pixelsPerMeter)
        {
            var m = new GeneratedMapLayout.Metrics
            {
                FontTiers = new[]
                {
                    t.NumberOf("space.mapGen.labelSize[0]", 34), t.NumberOf("space.mapGen.labelSize[1]", 31), t.NumberOf("space.mapGen.labelSize[2]", 28),
                },
                MaxLineWidth = t.NumberOf("space.mapGen.labelMaxLine", 200),
                MaxSingleLine = t.NumberOf("space.mapGen.labelMaxSingle", 340),
                Gap = t.NumberOf("space.mapGen.layoutGap", 4),
                Margin = t.NumberOf("space.mapGen.layoutMargin", 12),
                OwnTolerance = t.NumberOf("space.mapGen.labelOwnTolerance", 0.03f),
                FigureOwnShare = t.NumberOf("space.mapGen.figureOwnShare", 0.85f),
                LegendCableGap = t.NumberOf("space.mapGen.legendCableGap", 12),
                LegendLand = t.NumberOf("space.mapGen.legendLand", 0.02f),
                FrontierLength = t.NumberOf("space.mapGen.frontierLength", 30),
                FrontierHead = t.NumberOf("space.mapGen.frontierHead", 10),
                FrontierWidth = t.NumberOf("space.mapGen.frontierWidth", 3),
                CableSag = t.NumberOf("space.mapGen.cableSag", 0.08f),
                Legend = legend,
            };
            foreach (var keys in Occluders)
            {
                var r = new Rect(t.NumberOf(keys[0]), t.NumberOf(keys[1]), t.NumberOf(keys[2]), t.NumberOf(keys[3]));
                if (r.width > 0 && r.height > 0) m.Occluders.Add(r);
            }
            foreach (var r in regions) if (r != null) { r.MeasureMetrics(m); break; }
            // Figür tabanı (mm → harita pikseli) ve piyonların payı; token yoksa sahnedeki figür modelinin tabanı (70 × 48 mm)
            var baseMm = t.Vector2Of("space.teamFigure.baseMm");
            if (baseMm == Vector2.zero) baseMm = new Vector2(70, 48);
            var basePx = baseMm * (pixelsPerMeter / 1000f);
            float pawnPad = t.NumberOf("space.mapGen.figurePawnPad", 10);
            m.FigureBase = basePx;
            m.Figure = basePx + new Vector2(t.NumberOf("space.mapGen.figureSidePad", 4), pawnPad);
            m.FigureOffset = new Vector2(0, -pawnPad * 0.5f);
            return m;
        }

        void DrawLines(SiginakTheme t)
        {
            // Kıyı halesi: dıştan içe kalınlık ve alfa tokenlardan (n kat)
            for (int i = 0; i < halo.Length; i++)
            {
                var line = halo[i];
                if (line == null) continue;
                bool inner = i == halo.Length - 1;
                var c = t.ColorOf(inner ? "map.coastHaloNarrow" : "map.coastHaloWide");
                c.a *= i < HaloAlpha.Length ? t.NumberOf(HaloAlpha[i], 0.5f) : 0.5f;
                float width = i < HaloWidth.Length ? t.NumberOf(HaloWidth[i], 9) : 9;
                Draw(line, c, width, null, false, geometry.CoastLoops, true);
            }
            // Su çizgileri: kıyıdan denize doğru 3 ince, giderek soluk çizgi
            for (int i = 0; i < waterlines.Length; i++)
            {
                var line = waterlines[i];
                if (line == null || i >= WaterOffset.Length) continue;
                var c = t.ColorOf("map.coast");
                c.a *= t.NumberOf(WaterAlpha[i], 0.3f);
                line.Curve = LineCurve.Polyline;
                Draw(line, c, t.NumberOf(WaterWidth[i], 1.2f), null, false, geometry.Waterline(t.NumberOf(WaterOffset[i], 10 + 10 * i)), true);
            }
            Draw(borderUnder, t.ColorOf("map.regionBorderUnder"), t.NumberOf("space.mapGen.borderUnderWidth", 5), null, true, geometry.BorderPaths, false);
            Draw(border, t.ColorOf("map.regionBorder"), t.NumberOf("space.mapGen.borderWidth", 2), null, true, geometry.BorderPaths, false);
            Draw(coast, t.ColorOf("map.coast"), t.NumberOf("space.mapGen.coastWidth", 3), null, false, geometry.CoastLoops, true);
            if (coastSketch != null)
            {
                var c = t.ColorOf("map.coast");
                c.a *= t.NumberOf("space.mapGen.sketchAlpha", 0.45f);
                Draw(coastSketch, c, t.NumberOf("space.mapGen.sketchWidth", 1.2f), null, true,
                    geometry.Sketch(geometry.CoastLoops, t.NumberOf("space.mapGen.sketchJitter", 2.2f), 0x5C3Fu), true);
            }

            // Kablo: sarkan kesik ip (Quadratic), uçlarda raptiye
            var curves = geometry.CableCurves(t.NumberOf("space.mapGen.cableSag", 0.08f));
            if (cable != null)
            {
                Draw(cable, t.ColorOf("mapLiberation.cable"), t.NumberOf("space.mapGen.cableWidth", 2.4f),
                    new[] { t.NumberOf("space.mapGen.cableDash[0]", 10), t.NumberOf("space.mapGen.cableDash[1]", 7) }, true, curves, false);
                cable.Curve = LineCurve.Quadratic;
            }
            float pin = t.NumberOf("space.mapGen.cablePin", 9);
            var shadowOffset = t.Vector2Of("space.mapGen.pinShadowOffset");
            Dots(cablePinShadow, t.ColorOf("mapLiberation.cablePinShadow"), pin + 1, shadowOffset);
            Dots(cableEnds, t.ColorOf("mapLiberation.cablePin"), pin, Vector2.zero);
            // Parıltı: başın sol üstünde küçük açık nokta (çap ve kayma raptiye çapına oranla)
            var shine = t.Vector2Of("space.mapGen.pinShineOffset");
            Dots(cablePinShine, t.ColorOf("mapLiberation.cablePinShine"), pin * t.NumberOf("space.mapGen.pinShineSize", 0.35f), shine * pin);
        }

        void Dots(UILineGraphic line, Color color, float size, Vector2 offset)
        {
            if (line == null) return;
            // Sıfıra yakın boylu, yuvarlak uçlu kısa yol = nokta
            var dots = new List<IReadOnlyList<Vector2>>(geometry.CablePaths.Count * 2);
            foreach (var p in geometry.CablePaths)
            {
                var dir = (p[1] - p[0]).normalized * 0.5f;
                dots.Add(new[] { p[0] - dir + offset, p[0] + dir + offset });
                dots.Add(new[] { p[1] - dir + offset, p[1] + dir + offset });
            }
            Draw(line, color, size, null, true, dots, false);
        }

        void StyleLegend(SiginakTheme t)
        {
            if (legendCable != null)
            {
                Style(legendCable, t.ColorOf("mapLiberation.cable"), t.NumberOf("space.mapGen.cableWidth", 2.4f),
                    new[] { t.NumberOf("space.mapGen.legendCableDash[0]", 6), t.NumberOf("space.mapGen.legendCableDash[1]", 4) }, true);
            }
            if (legendCablePins != null) Style(legendCablePins, t.ColorOf("mapLiberation.cablePin"), t.NumberOf("space.mapGen.legendCablePin", 7), null, true);
        }

        /// <summary>
        /// Adaylara giden oklar: her aday bölge için ona bağlı özgür bölgeden, kara komşuluğunda ortak sınırın ortasında sınırı
        /// kesen ok, kablo bağlantısında kablonun aday ucuna yakın ok başı. Tek ortak kenarlı komşuluk da böylece okunur. Durum
        /// değişmedikçe yeniden çizilmez.
        /// </summary>
        public void SetFrontier(LiberationMapState state)
        {
            if (frontier == null || geometry == null) return;
            var free = new HashSet<string>();
            var candidates = new List<string>();
            if (state != null)
                foreach (var r in state.Regions)
                {
                    if (r.Kind == MapRegionKind.Free) free.Add(r.Id);
                    else if (r.Kind == MapRegionKind.Candidate) candidates.Add(r.Id);
                }
            var key = new StringBuilder();
            foreach (var c in candidates) key.Append(c).Append(',');
            key.Append('|');
            foreach (var f in free) key.Append(f).Append(',');
            if (key.ToString() == frontierKey) return;
            frontierKey = key.ToString();

            var t = SiginakTheme.Active;
            float half = t.NumberOf("space.mapGen.frontierLength", 30) * 0.5f, head = t.NumberOf("space.mapGen.frontierHead", 10);
            var paths = new List<IReadOnlyList<Vector2>>();
            var map = geometry.Map;
            foreach (var cand in candidates)
                foreach (var l in map.Links)
                {
                    string other = l.A == cand ? l.B : l.B == cand ? l.A : null;
                    if (other == null || !free.Contains(other)) continue;
                    if (l.Cable)
                    {
                        // Kablonun aday ucu: raptiyenin gerisinde (frontierCableBack), kablo yönünde ok başı
                        Vector2 a = new Vector2((float)l.X1, (float)l.Y1), b = new Vector2((float)l.X2, (float)l.Y2);
                        Vector2 from = l.A == cand ? b : a, to = l.A == cand ? a : b;
                        var dir = (to - from).normalized;
                        Arrow(paths, to - dir * t.NumberOf("space.mapGen.frontierCableBack", 26), dir, 0, head);
                        continue;
                    }
                    var edge = geometry.MiddleEdge(other, cand, out var fromCell, out var toCell);
                    if (edge == null) continue;
                    var mid = geometry.EdgeMiddle(edge.V1, edge.V2);
                    var d = (GeneratedMapGeometry.Center(map.Cells[toCell]) - GeneratedMapGeometry.Center(map.Cells[fromCell])).normalized;
                    Arrow(paths, mid, d, half, head);
                }
            frontier.SetPathsTopLeft(paths, Vector2.zero);
        }

        static void Arrow(List<IReadOnlyList<Vector2>> paths, Vector2 at, Vector2 dir, float half, float head)
        {
            var normal = new Vector2(-dir.y, dir.x);
            var tip = at + dir * half;
            if (half > 0) paths.Add(new[] { at - dir * half, tip });
            paths.Add(new[] { tip - dir * head + normal * head * 0.7f, tip, tip - dir * head - normal * head * 0.7f });
        }

        /// <summary>Dünya haritasına dönüş (anahtar 0): sahnedeki katmanlar ve bölge verisi geri gelir.</summary>
        public void Clear(IReadOnlyList<LiberationRegionMarks> regions)
        {
            if (geometry == null) return;
            geometry = null;
            layout = null;
            frontierKey = null;
            foreach (var go in worldLayers) if (go != null) go.SetActive(true);
            foreach (var go in generatedLayers)
            {
                if (go == null) continue;
                go.SetActive(false);
                if (go.transform.parent is RectTransform parent) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
            if (paper != null) paper.gameObject.SetActive(false);
            foreach (var r in regions) r?.ClearGenerated();
        }

        static void Style(UILineGraphic line, Color color, float width, float[] dash, bool roundCap)
        {
            line.color = color;
            line.Thickness = width;
            line.SetDash(dash);
            line.SetRoundCap(roundCap);
            line.raycastTarget = false;
        }

        static void Draw(UILineGraphic line, Color color, float width, float[] dash, bool roundCap,
            IReadOnlyList<IReadOnlyList<Vector2>> paths, bool closed)
        {
            if (line == null) return;
            Style(line, color, width, dash, roundCap);
            line.Curve = LineCurve.Polyline;
            line.SetPathsTopLeft(paths, Vector2.zero, closed);
        }
    }
}
