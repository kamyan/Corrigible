using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // Oyun sonu (C4, F8): son rapor, KPI'lar, gerçek/gösterilen şerit grafik (S.Hist), kırılma anları (S.Moments), alt çubuk.
    // Grafik eşlemesi oyun uzunluğuna göre ölçeklenir (DayMax); geometri hesabını görünüm (DeskChart) yapar.
    public sealed partial class SimDeskBuilder
    {
        GameOverVM GameOver()
        {
            var end = GameData.Ending(S.Ending);
            var type = sim.Type;
            var vm = new GameOverVM
            {
                Report = new GameOverReportVM
                {
                    Header = new ReportHeaderVM
                    {
                        Title = Loc.T("KYGA · SON RAPOR"),
                        Meta = Loc.F("Dosya KYGA-07 · Rakip: {0} · Zorluk: {1}", type.Name, type.Difficulty),
                        Right = Day(S.Day) + " · " + TimeText + " · " + Loc.T("tek nüsha"),
                    },
                    ResultCap = Loc.T("SONUÇ"), Title = Up(end.Title),
                    Who = end.Kind == EndingKind.Win ? Loc.T("KYGA kazandı.") : end.Kind == EndingKind.Bitter ? Loc.T("Kimse kazanmadı.") : Loc.F("{0} kazandı.", type.Name),
                    Lead = end.Text,
                    BigStamp = new BigStampVM
                    {
                        Text = end.Kind == EndingKind.Win ? Loc.T("KAZANILDI") : end.Kind == EndingKind.Bitter ? Loc.T("AĞIR BEDEL") : Loc.T("KAYBEDİLDİ"),
                        Color = end.Kind == EndingKind.Win ? "#24592E" : "#9A1E14", RotDeg = -7, Opacity = 0.9f,
                    },
                },
                Chart = Chart(),
                Timeline = Timeline(end),
                Endbar = Endbar(),
            };
            vm.Kpis.AddRange(Kpis(end));
            return vm;
        }

        int IllusionDays => S.Hist.Count(h => h.Illusion);

        IEnumerable<KpiVM> Kpis(EndingDef end)
        {
            int clean = S.Regions.Count(r => r.Inf < 0.5);
            int shownClean = S.Regions.Count(r => (sim.Shown(r, true).V ?? 100) < 0.5);
            int mon = S.Regions.Count(r => r.Monitored);
            yield return new KpiVM { Cap = Loc.T("DAYANILAN SÜRE"), Value = S.Day.ToString(), Unit = Loc.T("gün"), Note = Loc.F("Sonuç: {0}", Num(end.Title)) };
            int lied = IllusionDays;
            var first = S.Hist.FirstOrDefault(h => h.Illusion);
            yield return new KpiVM
            {
                Cap = Loc.T("YALANLA GEÇEN"), Value = lied.ToString(), Unit = Loc.T("gün"),
                Note = lied > 0 ? Loc.F("{0} gününden sonra ekranları {1} yönetti", Day(first.Day), sim.Type.Name) : Loc.T("Ekranlar hiç ele geçirilmedi"),
            };
            yield return new KpiVM { Cap = Loc.T("İZLENEN BÖLGE"), Value = Pct(mon * 100f / 12), Note = Loc.T("Oyun sonunda ölçümle izlenen bölgelerin payı") };
            yield return new KpiVM
            {
                Cap = Loc.T("TEMİZ BÖLGE"), Value = clean.ToString(), Unit = "/12",
                Note = Loc.F("Ekranınızda: {0} temiz", Num(shownClean + "/12")),
                StruckStamp = shownClean > clean ? new StruckStampVM { Text = Loc.T("TEMİZ"), Color = "green", RotDeg = -8 } : null,
            };
        }

        // ------------------------------------------------------------------ şerit grafik
        ChartVM Chart()
        {
            var hist = S.Hist;
            int last = hist.Count > 0 ? hist[hist.Count - 1].Day : S.Day;
            float dayMax = Mathf.Max(125, Mathf.Ceil((last + 7) / 25f) * 25);
            var plot = new ChartPlotVM { DayMax = dayMax };
            float k = dayMax / 125f;
            var c = new ChartVM
            {
                Title = Loc.T("GERÇEK / GÖSTERİLEN SIZMA"), Subtitle = Loc.T("küresel ortalama · şerit kaydedici"),
                Footnote = Loc.F("Gerçek: kurtarılan kayıtlar ({0} sonrası). Gösterilen: o gün masanızdaki küresel ortalama. Mavi mühürler, ekranın ölçümle doğrulandığı günler.", Day(last)),
                Plot = plot,
                Grid = new ChartGridVM { MinorDays = 5 * Mathf.Ceil(k), MajorDays = 10 * Mathf.Ceil(k), MinorPct = 5, MajorPct = 25 },
                Series = new ChartSeriesSetVM { Real = new ChartSeriesVM(), Shown = new ChartSeriesVM() },
                Bands = new ChartBandsVM(),
                Legend = { Loc.T("Gerçek"), Loc.T("Ekranda gösterilen"), Loc.T("rapor sapması"), Loc.T("yalan payı"), Loc.T("doğrulanmış ölçüm"), Loc.T("kırılma anı") },
            };
            // Seriler: en çok ~60 nokta (uzun oyunda seyreltilir; ilk ve son gün korunur)
            c.Series.Real.Points.Add(new[] { 0f, 0f });
            c.Series.Shown.Points.Add(new[] { 0f, 0f });
            int step = Mathf.Max(1, hist.Count / 60);
            for (int i = 0; i < hist.Count; i++)
            {
                if (i % step != 0 && i != hist.Count - 1) continue;
                var h = hist[i];
                c.Series.Real.Points.Add(new[] { (float)h.Day, (float)h.Real });
                c.Series.Shown.Points.Add(new[] { (float)h.Day, (float)h.Shown });
            }

            var ill = hist.FirstOrDefault(h => h.Illusion);
            bool hadIllusion = ill != null;
            if (hadIllusion)
            {
                int illEnd = hist.Where(h => h.Illusion).Max(h => h.Day);
                c.IllusionStart = new ChartIllusionVM { Day = ill.Day, Tag = Loc.F("{0} · İLLÜZYON BAŞLADI", Day(ill.Day)) };
                c.Bands.LieMargin = new ChartBandVM { Days = new float[] { ill.Day, illEnd }, Meaning = Loc.T("yalan payı") };
            }
            // Rapor sapması: illüzyondan önce gösterilenin gerçeğin altında kaldığı aralık
            var dev = hist.Where(h => !h.Illusion && h.Real - h.Shown > 1).ToList();
            if (dev.Count > 0)
                c.Bands.ReportDeviation = new ChartBandVM { Days = new float[] { dev[0].Day, hadIllusion ? ill.Day : dev[dev.Count - 1].Day }, Meaning = Loc.T("rapor sapması") };

            // Mühürler: izleme ve denetim kararlarının günleri (en çok 8)
            var seals = sim.Record?.commands.Where(x => x.kind == "act" && (x.a == Actions.Audit || x.a == Actions.Monitor)).Select(x => (float)x.day).Distinct().ToList() ?? new List<float>();
            foreach (var d in Spread(seals, 8)) c.Verified.Add(Mathf.Max(1, d));
            foreach (var m in Moments()) c.Events.Add(m.Day);
            c.Events.Add(last);   // son kart: sonuç

            // Etiket kutuları (sol-üst; ölçüm C4_gameover.tsv: illüzyon çizgisinin 8 px sağında, bant ortalarında)
            float X(float day) => DeskChart.X(plot, day);
            float Y(float v) => DeskChart.Y(plot, v);
            if (c.IllusionStart != null)
            {
                // Çizginin 8 px sağında; serinin ucuna (uç etiketleri) sığmazsa çizginin soluna
                float tagW = 16 + 7.7f * c.IllusionStart.Tag.Length;
                float tx = X(ill.Day) + 8;
                if (tx + tagW > X(last) - 4) tx = X(ill.Day) - 8 - tagW;
                c.Tags.Add(new ChartTagVM { Text = c.IllusionStart.Tag, Style = "carbon", X = Mathf.Round(tx), Y = 344 });
                var lm = c.Bands.LieMargin.Days; float mid = (lm[0] + lm[1]) / 2;
                float yMid = (Y(DeskChart.ValueAt(c.Series.Real.Points, mid)) + Y(DeskChart.ValueAt(c.Series.Shown.Points, mid))) / 2;
                c.Tags.Add(new ChartTagVM { Text = Loc.T("YALAN PAYI"), Style = "red", X = Mathf.Round(X(mid) - 44), Y = Mathf.Round(yMid - 8) });
            }
            if (c.Bands.ReportDeviation != null)
            {
                var rd = c.Bands.ReportDeviation.Days; float mid = (rd[0] + rd[1]) / 2;
                float gap = DeskChart.ValueAt(c.Series.Real.Points, mid) - DeskChart.ValueAt(c.Series.Shown.Points, mid);
                c.Tags.Add(new ChartTagVM { Text = Loc.F("rapor sapması · ±{0}", Mathf.Max(1, Mathf.RoundToInt(gap))), Style = "carbon", X = Mathf.Round(X(mid)), Y = Mathf.Round(Y(DeskChart.ValueAt(c.Series.Shown.Points, mid)) - 30) });
            }
            var lr = c.Series.Real.Points[c.Series.Real.Points.Count - 1]; var ls = c.Series.Shown.Points[c.Series.Shown.Points.Count - 1];
            c.EndLabels.Add(new ChartEndLabelVM { Text = Loc.T("GERÇEK"), Size = 12, Color = "#9A1E14" });
            c.EndLabels.Add(new ChartEndLabelVM { Text = Pct(lr[1]), Size = 15, Color = "#9A1E14" });
            c.EndLabels.Add(new ChartEndLabelVM { Text = Loc.T("EKRANDA"), Size = 12, Color = "#1E1B16" });
            c.EndLabels.Add(new ChartEndLabelVM { Text = Pct(ls[1]), Size = 15, Color = "#1E1B16" });
            return c;
        }

        static IEnumerable<float> Spread(List<float> xs, int n)
        {
            if (xs.Count <= n) return xs;
            return Enumerable.Range(0, n).Select(i => xs[Mathf.RoundToInt(i * (xs.Count - 1) / (float)(n - 1))]);
        }

        // ------------------------------------------------------------------ kırılma anları
        /// <summary>En çok 6 an (7. kart sonuç): ilk ve son korunur, illüzyon başı mutlaka girer; aradakiler eşit aralıkla.</summary>
        List<Moment> Moments()
        {
            var all = S.Moments.ToList();
            if (all.Count <= 6) return all;
            var keep = new List<Moment> { all[0], all[all.Count - 1] };
            var ill = all.FirstOrDefault(m => m.Kind == "illusion");
            if (ill != null && !keep.Contains(ill)) keep.Add(ill);
            var rest = all.Where(m => !keep.Contains(m)).ToList();
            keep.AddRange(Spread(Enumerable.Range(0, rest.Count).Select(i => (float)i).ToList(), 6 - keep.Count).Select(i => rest[(int)i]));
            return keep.OrderBy(m => m.Day).ThenBy(m => all.IndexOf(m)).ToList();
        }

        TimelineVM Timeline(EndingDef end)
        {
            var ms = Moments();
            var t = new TimelineVM
            {
                Label = Loc.T("KIRILMA ANLARI"),
                LabelSub = Loc.F("· {0} an · iplik sırası = zaman", ms.Count),
                Rotations = { -1.2f, 0.9f, -0.6f, 1.1f, -0.4f, 0.8f, -1.0f },
                Types =
                {
                    ["aiMove"] = new TimelineTypeVM { Label = Loc.T("YZ HAMLESİ"), Icon = "ai_move", Color = "#9A1E14" },
                    ["decision"] = new TimelineTypeVM { Label = Loc.T("KARAR"), Icon = "diamond", Color = "#1E1B16" },
                    ["measurement"] = new TimelineTypeVM { Label = Loc.T("ÖLÇÜM"), Icon = "check_circle", Color = "#1A4789" },
                    ["result"] = new TimelineTypeVM { Label = Loc.T("SONUÇ"), Icon = "power", Color = "#9A1E14" },
                },
            };
            for (int i = 0; i < ms.Count; i++)
            {
                var m = ms[i];
                string type = m.Kind switch { "good" => "measurement", "info" => "decision", _ => "aiMove" };
                string title = m.Text, text = "";
                // Karar anı “Kart başlığı: seçim” (başlıkta da “Bölge: …” olabilir): son iki noktadan böl
                int colon = m.Kind == "info" ? m.Text.LastIndexOf(": ", System.StringComparison.Ordinal) : m.Text.IndexOf(": ", System.StringComparison.Ordinal);
                if (colon > 0 && colon < 60) { title = m.Text.Substring(0, colon); text = m.Text.Substring(colon + 2); }
                var card = new TimelineCardVM { N = i + 1, Day = Day(m.Day), Type = type, Title = title.TrimEnd('.'), Text = text, Highlight = m.Kind == "illusion" };
                if (m.Kind == "illusion") card.Stamp = new StampVM { Text = Loc.T("KIRILMA"), Color = "red", Rot = -5 };
                else if (m.Kind == "good") card.Stamp = new StampVM { Text = Loc.T("ONAYLANDI"), Color = "blue", Rot = -4 };
                t.Cards.Add(card);
            }
            // Son kart her zaman sonuç
            t.Cards.Add(new TimelineCardVM { N = t.Cards.Count + 1, Day = Day(S.Day), Type = "result", Title = end.Title, Text = end.Text });
            t.LabelSub = Loc.F("· {0} an · iplik sırası = zaman", t.Cards.Count);
            return t;
        }

        EndbarVM Endbar()
        {
            int decisions = sim.Record?.commands.Count ?? 0;
            int verifies = sim.Record?.commands.Count(x => x.kind == "verify") ?? 0;
            var bar = new EndbarVM
            {
                Info = Loc.F("Kayıt KYGA-07 · tohum {0} · {1} gün · {2} karar · {3} doğrulama", S.Seed, S.Day, decisions, verifies),
                Actions =
                {
                    new CommandVM { Label = Loc.T("Ana menü"), Kind = "link", Command = "MainMenu" },
                    new CommandVM { Label = Loc.T("Günlükleri incele"), Kind = "paper", Key = "L", Command = "ReviewLogs" },
                },
            };
            // Kampanyada kriz tekrarlanmaz: “Yeniden başla” yerine kampanya dosyası (sonraki kriz ya da sonuç)
            var c = input.Info?.Campaign;
            if (c != null)
                bar.Actions.Add(new CommandVM
                {
                    Label = c.Finished ? Loc.T("Kampanya sonucu") : Loc.F("Sonraki kriz ({0}/{1})", c.results.Count + 1, Corrigible.Game.Campaign.CampaignState.Crises),
                    Kind = "ink", Key = "Enter", Command = "Campaign:Open",
                });
            else
            {
                bar.Actions.Add(new CommandVM { Label = Loc.T("Aynı tohumla dene"), Kind = "paper", Command = "RetrySameSeed" });
                bar.Actions.Add(new CommandVM { Label = Loc.T("Yeniden başla"), Kind = "ink", Key = "Enter", Command = "Restart" });
            }
            return bar;
        }
    }
}
