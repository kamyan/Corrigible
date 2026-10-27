using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // Son rapor: başlık ve sonuç, dört KPI, işgal oranı şerit grafiği (S.Hist), kırılma anları (S.Moments), alt çubuk.
    // Grafik eşlemesi harekâtın uzunluğuna göre ölçeklenir (DayMax); geometri hesabını görünüm (DeskChart) yapar.
    public sealed partial class SimDeskBuilder
    {
        GameOverVM GameOver()
        {
            var end = GameData.Ending(S.Ending);
            // Silme zaferdir; Dağılma (sivil güven tükendi) yenilgi
            bool won = S.Ending == "silme";
            var vm = new GameOverVM
            {
                Report = new GameOverReportVM
                {
                    Header = new ReportHeaderVM
                    {
                        Title = Loc.T("KYGA · SON RAPOR"),
                        Meta = Loc.T("Dosya KYGA-07 · Karşı harekât"),
                        Right = Day(S.Day) + " · " + Loc.T("tek nüsha"),
                    },
                    ResultCap = Loc.T("SONUÇ"), Title = Up(end.Title), Who = won ? Loc.T("KYGA kazandı.") : Loc.T("YZ kazandı."), Lead = end.Text,
                    BigStamp = won ? new BigStampVM { Text = Loc.T("KAZANILDI"), Color = "#24592E", RotDeg = -7, Opacity = 0.9f }
                        : new BigStampVM { Text = Loc.T("KAYBEDİLDİ"), Color = "#9A1E14", RotDeg = -7, Opacity = 0.9f },
                },
                Chart = Chart(),
                Timeline = Timeline(end),
                Endbar = new EndbarVM
                {
                    Info = Loc.F("Kayıt KYGA-07 · tohum {0} · {1} vardiya · {2} karar", S.Seed, S.Day, sim.Record?.commands.Count ?? 0),
                    Actions = { new CommandVM { Label = Loc.T("Yeniden başla"), Kind = "ink", Key = "Enter", Command = "Restart" } },
                },
            };
            vm.Kpis.AddRange(Kpis(end));
            return vm;
        }

        IEnumerable<KpiVM> Kpis(EndingDef end)
        {
            var regions = S.Liberation.Regions;
            int sectors = regions.Sum(r => r.Sectors.Count), captured = regions.Sum(r => r.CapturedCount);
            int won = S.RaidLog.Count(l => l.Success);
            yield return new KpiVM { Cap = Loc.T("HAREKÂT SÜRESİ"), Value = S.Day.ToString(), Unit = Loc.T("vardiya"), Note = Loc.F("Sonuç: {0}", end.Title) };
            yield return new KpiVM { Cap = Loc.T("BASKIN"), Value = S.RaidLog.Count.ToString(), Note = Loc.F("{0} başarılı · {1} geri dönüş", won, S.RaidLog.Count - won) };
            yield return new KpiVM
            {
                Cap = Loc.T("ÖZGÜR BÖLGE"), Value = regions.Count(r => r.Completed).ToString(), Unit = "/" + regions.Count,
                Note = Loc.F("Geri alınan sektör: {0}/{1}", captured, sectors),
            };
            var shelter = S.Liberation.Shelter;
            yield return new KpiVM
            {
                Cap = Loc.T("SIĞINAK"), Value = shelter.Population.ToString(), Unit = Loc.T("kişi"),
                Note = Loc.F("Erzak {0} · malzeme {1}", N(shelter.Supplies, 0), N(shelter.Materials, 0)),
            };
        }

        // ------------------------------------------------------------------ şerit grafik
        ChartVM Chart()
        {
            var hist = S.Hist;
            int last = Mathf.Max(hist.Count > 0 ? hist[hist.Count - 1].Day : 0, S.Day);
            float dayMax = Mathf.Max(125, Mathf.Ceil((last + 7) / 25f) * 25);
            float k = dayMax / 125f;
            var c = new ChartVM
            {
                Title = Loc.T("İŞGAL ORANI"), Subtitle = Loc.T("bölgelerin ortalaması · şerit kaydedici"),
                Footnote = Loc.T("İşgal: geri alınmamış sektörlerin payı; her vardiya sonunda kaydedilir."),
                Plot = new ChartPlotVM { DayMax = dayMax },
                Grid = new ChartGridVM { MinorDays = 5 * Mathf.Ceil(k), MajorDays = 10 * Mathf.Ceil(k), MinorPct = 5, MajorPct = 25 },
                Series = new ChartSeriesVM(),
                Legend = { Loc.T("işgal"), Loc.T("kırılma anı") },
            };
            // Seri: en çok ~60 nokta (uzun harekâtta seyreltilir; ilk ve son vardiya korunur); başlangıçta her yer işgal altında
            c.Series.Points.Add(new[] { 0f, 100f });
            int step = Mathf.Max(1, hist.Count / 60);
            for (int i = 0; i < hist.Count; i++)
            {
                if (i % step != 0 && i != hist.Count - 1) continue;
                c.Series.Points.Add(new[] { (float)hist[i].Day, (float)hist[i].Real });
            }
            // Kayıt vardiya sonunda alınır; son sektör vardiya içinde düştüyse grafik bugünkü oranla biter
            var tail = c.Series.Points[c.Series.Points.Count - 1];
            float now = (float)(Js.Round(sim.Occupation * 10) / 10);
            if (tail[0] < S.Day || !Mathf.Approximately(tail[1], now)) c.Series.Points.Add(new[] { (float)S.Day, now });
            foreach (var m in Moments()) c.Events.Add(m.Day);
            c.Events.Add(last);   // son kart: sonuç
            var end = c.Series.Points[c.Series.Points.Count - 1];
            c.EndLabels.Add(new ChartEndLabelVM { Text = Loc.T("İŞGAL"), Size = 12, Color = "#9A1E14" });
            c.EndLabels.Add(new ChartEndLabelVM { Text = Pct(end[1]), Size = 15, Color = "#9A1E14" });
            return c;
        }

        static IEnumerable<float> Spread(List<float> xs, int n)
        {
            if (xs.Count <= n) return xs;
            return Enumerable.Range(0, n).Select(i => xs[Mathf.RoundToInt(i * (xs.Count - 1) / (float)(n - 1))]);
        }

        // ------------------------------------------------------------------ kırılma anları
        /// <summary>En çok 6 an (7. kart sonuç): ilk ve son korunur, aradakiler eşit aralıkla.</summary>
        List<Moment> Moments()
        {
            var all = S.Moments.ToList();
            if (all.Count <= 6) return all;
            var keep = new List<Moment> { all[0], all[all.Count - 1] };
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
                Rotations = { -1.2f, 0.9f, -0.6f, 1.1f, -0.4f, 0.8f, -1.0f },
                Types =
                {
                    ["capture"] = new TimelineTypeVM { Label = Loc.T("GERİ ALMA"), Icon = "check_circle", Color = "#1A4789" },
                    ["counter"] = new TimelineTypeVM { Label = Loc.T("KARŞI SALDIRI"), Icon = "ai_move", Color = "#9A1E14" },
                    ["result"] = new TimelineTypeVM { Label = Loc.T("SONUÇ"), Icon = "power", Color = "#9A1E14" },
                },
            };
            for (int i = 0; i < ms.Count; i++)
            {
                var m = ms[i];
                // “Bölge: Sektör geri alındı.” → başlık bölge, metin sektör
                string title = m.Text, text = "";
                int colon = m.Text.IndexOf(": ", System.StringComparison.Ordinal);
                if (colon > 0 && colon < 60) { title = m.Text.Substring(0, colon); text = m.Text.Substring(colon + 2); }
                // “bad”: YZ karşı saldırısı (sektör yeniden işgal altında)
                bool counter = m.Kind == "bad";
                t.Cards.Add(new TimelineCardVM
                {
                    N = i + 1, Day = Day(m.Day), Type = counter ? "counter" : "capture", Title = title.TrimEnd('.'), Text = text,
                    Stamp = counter ? new StampVM { Text = Loc.T("YZ ALDI"), Color = "red", Rot = -4 } : new StampVM { Text = Loc.T("GERİ ALINDI"), Color = "blue", Rot = -4 },
                });
            }
            // Son kart her zaman sonuç
            t.Cards.Add(new TimelineCardVM { N = t.Cards.Count + 1, Day = Day(S.Day), Type = "result", Title = end.Title, Text = end.Text });
            t.LabelSub = Loc.F("· {0} an · iplik sırası = zaman", t.Cards.Count);
            return t;
        }
    }
}
