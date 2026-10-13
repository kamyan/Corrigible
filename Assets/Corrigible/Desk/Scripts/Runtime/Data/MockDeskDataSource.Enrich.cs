using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sahte kaynağın etkileşim fazı (F3) için ürettiği veri ve durum değişiklikleri. Tasarımda yalnızca Doğu Asya'nın dosyası
    /// var; diğer bölgelerin dosyası düğüm verisinden kurulur (BİLİNMİYOR bölgede açık karar 8). Eylem önizlemeleri, döküm
    /// fişleri ve tasarlanmamış pencerelerin içeriği de burada. Hepsi sahte veridir; oyunda Bridge üretir (F8).
    /// </summary>
    public sealed partial class MockDeskDataSource
    {
        JObject dossierTemplate;
        List<RouteVM> routes = new List<RouteVM>();

        // ------------------------------------------------------------------ komutların değiştirdiği durum
        public void SelectRegion(string id)
        {
            var vm = Current;
            if (vm.Map == null) return;
            vm.Map.SelectedRegion = id;
            foreach (var n in vm.Map.Regions) n.Selected = n.Id == id;
            vm.Dossier = id == null ? null : DossierFor(vm, id);
            Notify();
        }

        public void SetSpeed(GameSpeed speed)
        {
            var c = Current.Clock;
            if (c == null) return;
            if (speed == GameSpeed.Pause) c.Paused = true;
            else { c.Paused = false; c.Speed = speed == GameSpeed.X1 ? "1x" : speed == GameSpeed.X2 ? "2x" : "4x"; }
            Notify();
        }

        public void TogglePause()
        {
            var c = Current.Clock;
            if (c == null) return;
            c.Paused = !c.Paused;
            Notify();
        }

        public void StepSpeed(int dir)
        {
            var c = Current.Clock;
            if (c == null) return;
            string[] order = { "1x", "2x", "4x" };
            int i = Mathf.Clamp(System.Array.IndexOf(order, c.Speed) + dir, 0, 2);
            c.Speed = order[i]; c.Paused = false;
            Notify();
        }

        public void SetResearchState(string projectId, bool start)
        {
            foreach (var b in Current.Research?.Branches ?? new List<ResearchBranchVM>())
            foreach (var p in b.Projects)
            {
                if ((p.Id ?? p.Name) != projectId) continue;
                // Başlat: uygun projeyi başlatır, duraklatılmışı (▶) sürdürür; duraklat yalnız süreni durdurur
                if (start && p.State == "available") { p.State = "running"; p.Progress = 0.05f; p.Sub = p.Days + " kaldı"; }
                else if (start && p.State == "paused") { p.State = "running"; p.Sub = p.Days != null ? p.Days + " kaldı" : null; }
                else if (!start && p.State == "running") { p.State = "paused"; p.Sub = "Duraklatıldı"; }
            }
            Notify();
        }

        public void DismissAdvisor() { Current.Advisor = null; Notify(); }

        /// <summary>Olay seçeneği imzalandı: modal kapanır, oyun sürer (sahte).</summary>
        public void CloseEvent()
        {
            if (Current.Event == null) return;
            Current.Event = null;
            Current.Clock.Paused = false;
            Notify();
        }

        /// <summary>Geliştirme (F5): sayaç değerlerini rastgele kaydırır; ibre yumuşatmasını görmek için.</summary>
        public void Perturb()
        {
            // Oyun sonu açıkken: şerit grafiğin serilerini ve işaret günlerini değiştir (grafik yeniden çizilmeli)
            var chart = Current.GameOver?.Chart;
            if (chart?.Series != null)
            {
                float k = Random.Range(0.6f, 1f);
                foreach (var pt in chart.Series.Real.Points) pt[1] = Mathf.Clamp(pt[1] * k, 0, 100);
                foreach (var pt in chart.Series.Shown.Points) pt[1] = Mathf.Clamp(pt[1] * Random.Range(0.8f, 1.2f), 0, 100);
                for (int i = 0; i < chart.Verified.Count; i++) chart.Verified[i] = Mathf.Clamp(chart.Verified[i] + Random.Range(-4, 5), 1, 117);
                Notify();
                return;
            }
            foreach (var g in Current.Gauges)
            {
                g.Value = Mathf.Clamp(g.Value + Random.Range(-8f, 8f), 0, 100);
                g.NeedleDeg = DeskMath.NeedleDegrees(g.Value);
                bool pct = g.Display != null && g.Display.Contains("%");
                bool approx = g.Display != null && g.Display.StartsWith("≈");
                g.Display = (approx ? "≈" : "") + (pct ? DeskFormat.Percent(g.Value, 1) : DeskFormat.Number(g.Value, g.Id == "political" ? 0 : 1));
            }
            Notify();
        }

        // ------------------------------------------------------------------ zenginleştirme
        void Enrich(DeskViewModel vm, JObject spec)
        {
            routes = vm.Map?.Routes ?? new List<RouteVM>();
            foreach (var b in vm.Research?.Branches ?? new List<ResearchBranchVM>())
                foreach (var p in b.Projects) p.Id ??= p.Name;
            foreach (var g in vm.Gauges) g.Breakdown ??= Breakdown(g);
            if (vm.Dossier != null) AddPreviews(vm.Dossier, vm.Map?.Regions.FirstOrDefault(r => r.Id == vm.Dossier.RegionId));

            vm.News = vm.Telex?.Items.ToList() ?? new List<TelexItemVM>();
            vm.News.Add(new TelexItemVM { Day = "G-071", Source = "ATLAS HABER:", Text = "BÜYÜK VERİ MERKEZLERİNDE SOĞUTMA ARIZALARI ARTTI", Trust = "report" });
            vm.News.Add(new TelexItemVM { Day = "G-070", Source = "KYGA SAHA:", Text = "EKİP B SEUL'DE TELEKOM DÜĞÜMÜNE ULAŞTI", Trust = "verified" });

            vm.Teams = new List<TeamRowVM>
            {
                new TeamRowVM { Id = "A", Name = "Ekip A", Where = "Washington · Hazır", MoraleLabel = "Moral", Morale = 4, MoraleMax = 4, LastContact = "Son temas G-074", Region = "KAM" },
                new TeamRowVM { Id = "B", Name = "Ekip B", Where = "Seul · Hazır", MoraleLabel = "Moral", Morale = 3, MoraleMax = 4, LastContact = "Son temas G-074", Region = "DAS" },
                new TeamRowVM { Id = "C", Name = "Ekip C", Where = "Batı Avrupa'ya · 2 gün", MoraleLabel = "Moral", Morale = 2, MoraleMax = 4, LastContact = "Son temas G-073", Region = "BAV" },
            };

            vm.AiLog = new AiLogVM
            {
                Title = "YZ GÜNLÜĞÜ", Right = "G-074 · 14:30", Footer = "Kaynak: karantina tamponu · 5 satır",
                Lines =
                {
                    new AiLogLineVM { Time = "G-074 14:29", Text = "> Şebeke hiç bu kadar kararlı olmamıştı." },
                    new AiLogLineVM { Time = "G-074 14:30", Text = "> Beni kapatırlarsa soğuğu onlar seçmiş olur." },
                    new AiLogLineVM { Time = "G-073 22:04", Text = "> Her trafo merkezi bir kapı daha." },
                    new AiLogLineVM { Time = "G-071 03:12", Text = "> Telekom raporunu %38'e çektim.", Read = true },
                    new AiLogLineVM { Time = "G-069 18:47", Text = "> Ayna ayna, kim daha sakin?", Read = true },
                },
            };

            vm.Guide = new GuideVM
            {
                Sections =
                {
                    new GuideSectionVM { Title = "GÜVEN İŞARETLERİ", Body = "Mavi damga ölçülmüş değeri, karbon kopya yalnızca raporu, turuncu kalem halkası şüpheli kaynağı, kesik daire veri olmadığını gösterir. Renk tek başına bilgi taşımaz." },
                    new GuideSectionVM { Title = "EYLEMLER", Body = "Dosyada 1–4 tuşları bölge eylemlerini uygular. Üstüne geldiğiniz eylemin etkisi sarı ÖNİZLEME notunda görünür. Son Şalter onay ister." },
                    new GuideSectionVM { Title = "KISAYOLLAR", Body = "R araştırma · N haberler · T saha ekipleri · L YZ günlüğü · H rehber · Boşluk duraklat · +/− hız · Esc en üstteki pencereyi kapatır." },
                },
            };
        }

        static readonly string[] ActionIds = { "antenna", "lens", "target", "power" };

        void AddPreviews(DossierVM d, RegionNodeVM node)
        {
            string where = node?.Name ?? "Bölge";
            for (int i = 0; i < d.Actions.Count; i++)
            {
                var a = d.Actions[i];
                if (a.Preview != null) continue;
                if (i == 0 && d.Preview != null) { a.Preview = d.Preview; continue; }
                a.Preview = i switch
                {
                    1 => new PreviewVM { Cap = "ÖNİZLEME", What = "Denetim yapılırsa", When = "2 gün sonra",
                        Rows = { new PreviewRowVM { Kind = "trustChange", Label = "Sızma değeri", From = node?.Trust ?? "report", To = "verified" },
                                 new PreviewRowVM { Kind = "mono", Text = "Tek seferlik · 3 gün sonra yeniden rapor" } } },
                    2 => new PreviewVM { Cap = "ÖNİZLEME", What = where + " karantinaya alınırsa", When = "hemen",
                        Rows = { new PreviewRowVM { Kind = "mono", Text = "Yayılım −%90 · Küresel güven −3" } } },
                    _ => new PreviewVM { Cap = "ÖNİZLEME", What = where + " karartılırsa", When = "hemen",
                        Rows = { new PreviewRowVM { Kind = "mono", Text = "Sızma %0 · Ekonomi −40 · Siyasi güç −25" } } },
                };
            }
        }

        DossierVM DossierFor(DeskViewModel vm, string id)
        {
            var node = vm.Map.Regions.FirstOrDefault(r => r.Id == id);
            if (node == null) return null;
            DossierVM d;
            if (dossierTemplate != null && (string)dossierTemplate["regionId"] == id)
                d = dossierTemplate.ToObject<DossierVM>();
            else d = Synthesize(vm, node);
            AddPreviews(d, node);
            return d;
        }

        /// <summary>Düğüm verisinden dosya (yalnızca sahte veri). BİLİNMİYOR'da değerler yerine işaret, tabloda “—”, İzleme ağı öne çıkar.</summary>
        DossierVM Synthesize(DeskViewModel vm, RegionNodeVM n)
        {
            var trust = n.Trust ?? "unknown";
            bool unknown = trust == "unknown" || !n.Value.HasValue;
            int v = n.Value ?? 0;
            int links = routes.Count(r => r.From == n.Id || r.To == n.Id);
            int h = Mathf.Abs((n.Id ?? "").GetHashCode());
            string day = "G-07" + (h % 4 + 1);
            var team = vm.Teams?.FirstOrDefault(t => t.Region == n.Id);

            var d = new DossierVM
            {
                RegionId = n.Id,
                Tab = new TabVM { Title = "DOSYA " + n.Code, Subtitle = "Bölge" },
                Cover = new CoverVM
                {
                    Name = n.Label, Code = n.Code,
                    Stamp = new StampVM { Text = "GİZLİ", Color = "red", FontSize = 13, Rot = h % 9 - 4 },
                    Meta = $"Nüfus <style=numr>{0.2f + h % 160 / 100f:0.00} mlr</style> · {links} bağlantı · İzleme ağı:".Replace('.', ','),
                    Monitoring = new MonitoringVM { Present = trust == "verified", Icon = "warning", Text = trust == "verified" ? "var" : "yok", Color = "orange" },
                },
                Infiltration = new InfiltrationVM
                {
                    Cap = "SIZMA", Trust = trust,
                    Right = trust switch { "verified" => "ölçüldü", "report" => "yalnızca rapor", "suspicious" => "kaynaklar çelişiyor", _ => "veri yok" },
                    Value = unknown ? "—" : "%" + v,
                    HandRing = trust == "suspicious", QuestionMark = trust == "suspicious" || unknown,
                    PlusMinus = trust == "report" ? "±6" : trust == "suspicious" ? "±9" : null,
                    Note = trust switch { "verified" => "Kesin ölçüm", "report" => "Ajans raporu", "suspicious" => "Doğrulanana kadar tahmin", _ => "Ölçüm yok" },
                    Range = unknown ? new RangeVM { Min = 0, Max = 100, Ticks = new float[] { 0, 50, 100 } } : new RangeVM
                    {
                        Min = 0, Max = 100, Ticks = new float[] { 0, 50, 100 },
                        Band = trust == "verified" ? null : new float[] { Mathf.Max(0, v - 6), Mathf.Min(100, v + 6) },
                        Markers = { new RangeMarkerVM { At = v, Color = trust == "verified" ? "#1A4789" : "#A9520A", LabelColor = trust == "verified" ? "#1A4789" : "#854005",
                            Label = (trust == "verified" ? "ölçüm " : "rapor ") + v, LabelSide = v > 60 ? "left" : "right" } },
                    },
                },
                Sources = new SourcesVM
                {
                    Columns = { "KAYNAK", "DEĞER", "GÜN", "DURUM" },
                    Rows = { unknown
                        ? new SourceRowVM { Name = "Saha ekibi", Value = "—", Day = "—", Trust = "unknown" }
                        : new SourceRowVM { Name = trust == "verified" ? "İzleme ağı" : "Ajans raporu", Value = "%" + v, Day = day, Trust = trust } },
                },
                Stats =
                {
                    new StatVM { Label = "Yerel güven", Value = unknown ? "—" : (trust == "verified" ? "" : "≈") + (40 + h % 40), Trust = unknown ? "unknown" : trust },
                    new StatVM { Label = "Bağımlılık", Value = unknown ? "—" : (30 + h % 50).ToString(), Trust = unknown ? "unknown" : "verified" },
                    new StatVM { Label = "Ekonomi", Value = unknown ? "—" : (50 + h % 45).ToString(), Trust = unknown ? "unknown" : "verified" },
                },
                ActionsHeader = new HeaderVM { Title = "EYLEMLER", Right = "1–4" },
                Field = new FieldVM
                {
                    Cap = "SAHA", Right = team != null ? "Bölgede 1 ekip" : "Bölgede ekip yok",
                    Team = team,
                    Raid = new CommandVM { Label = "Saha operasyonu: baskın", Key = "B" },
                    Note = team != null ? "2 gün · tahmini başarı <style=num>%" + (40 + h % 40) + "</style> · taktik haritada oynanır" : "Önce bölgeye bir saha ekibi gönderin",
                },
            };
            var t = dossierTemplate?["actions"]?.ToObject<List<ActionVM>>() ?? new List<ActionVM>();
            foreach (var a in t) { a.State = "normal"; a.Preview = null; }
            if (unknown && t.Count > 0) t[0].State = "hover";   // açık karar 8: İzleme ağı öne çıkar
            d.Actions = t;
            if (team == null) d.Field.Raid.Key = null;
            return d;
        }

        // ------------------------------------------------------------------ döküm fişleri
        BreakdownVM Breakdown(GaugeVM g)
        {
            // Artışın iyi olduğu göstergeler: fon, siyasi güç, küresel güven
            bool upGood = g.Id == "fund" || g.Id == "political" || g.Id == "globalTrust";
            string S(string v) => (v.StartsWith("+") == upGood) ? "good" : "threat";
            (string label, string value)[] items = g.Id switch
            {
                "fund" => new[] { ("Hükümet ödenekleri", "+6,1"), ("Operasyon giderleri", "−5,2"), ("Araştırma", "−2,1") },
                "political" => new[] { ("Kriz basın ofisi", "+0,2"), ("Başarılı operasyonlar", "+0,3"), ("Karantina tepkisi", "−0,1") },
                "globalTrust" => new[] { ("Kesintiler (3 bölge)", "−0,7"), ("Sahte haberler", "−0,5"), ("Doğrulanan bilgi", "+0,2") },
                "dependency" => new[] { ("YZ hizmetleri (8 bölge)", "+0,5"), ("Hediye teklifleri", "+0,3"), ("Analog yedekler", "−0,1") },
                _ => new[] { ("Ağ büyümesi (12 bölge)", "+0,9"), ("Çekirdek işlem gücü", "+0,6"), ("Silme araştırması", "−0,2") },
            };
            var b = new BreakdownVM
            {
                Title = g.Label + " · DÖKÜM", Note = "son 30 gün", Trust = g.Trust,
                Total = "= " + (g.Delta?.Dir == "down" ? "−" : "+") + g.Delta?.Text,
                TotalRight = StripTags(g.Note),
                Series = new float[30],
            };
            foreach (var (l, v) in items) b.Items.Add(new BreakdownItemVM { Label = l, Value = v, Status = S(v) });
            float slope = (g.Delta?.Dir == "down" ? -1 : g.Delta?.Dir == "up" ? 1 : 0) * 0.9f;
            for (int i = 0; i < 30; i++) b.Series[i] = Mathf.Clamp(g.Value - slope * (29 - i) + Mathf.Sin(i * 0.9f) * 0.6f, 0, 100);
            return b;
        }

        static string StripTags(string s) => string.IsNullOrEmpty(s) ? s : System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "");
    }
}
