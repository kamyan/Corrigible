using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Harekât sonu raporunun içeriği (Revizyon 2 · S8; görünüm <see cref="FinalReportView"/>). Simülasyonu yalnız okur.
    /// İşgal şeridi <c>S.Hist</c>'ten (x = vardiya 1…N; harekât tam işgalle başlar, bugünkü oranla biter). Kırılma anları
    /// sunumda türetilir (en çok altı): ilk özgür bölge, karşı saldırılar (<c>S.Moments</c>), öteki özgür bölgeler (baskın
    /// kaydı), kaybın nedeni (Dağılma'da son güven düşüşü: kapı ya da baskın) ve son olay (son sunucu, güvenin sıfırlanması ya
    /// da ölümcül kapı). Vardiya = gün + 1 (VARDİYA sayacı).
    /// </summary>
    public static class FinalReportBuilder
    {
        /// <summary>Kırılma anlarının en çoğu.</summary>
        public const int MaxMoments = 6;

        static string Name(string region) => Loc.T(GameData.Region(region).Name);


        static string Code(int day) => Loc.F("V.{0:000}", day + 1);

        public static FinalReportData Build(Simulation sim)
        {
            var S = sim.S;
            if (S.Ending == null) return null;
            var end = GameData.Ending(S.Ending);
            bool won = S.Ending == "silme";
            int shifts = S.Day + 1;
            var regions = S.Liberation.Regions;
            int free = regions.Count(r => r.Completed), sectors = regions.Sum(r => r.Sectors.Count), captured = regions.Sum(r => r.CapturedCount);
            int raids = S.RaidLog.Count, wins = S.RaidLog.Count(l => l.Success);
            double now = Js.Round(sim.Occupation * 10) / 10;

            var d = new FinalReportData
            {
                Head = Loc.T("KYGA · HAREKÂT SONU RAPORU"),
                Number = Loc.F("SIĞINAK 07 · V.{0:000} · tek nüsha", shifts),
                Won = won,
                Stamp = won ? Loc.T("KAZANILDI") : Loc.T("KAYBEDİLDİ"),
                Reason = won ? null : DeskText.Upper(Loc.T(end.Title)),
                Shifts = shifts,
                ChartTitle = Loc.T("İŞGAL ORANI"),
                ChartRange = Loc.F("VARDİYA 1–{0}", shifts),
                OccupiedLabel = now > 0.05 ? Loc.F("İŞGAL · {0}", DeskFormat.Percent(now)) : Loc.T("İŞGAL"),
                RetakenLabel = Loc.T("GERİ ALINAN"),
                MomentsTitle = Loc.T("KIRILMA ANLARI"),
                Action = Loc.T("YENİDEN BAŞLA"),
            };
            switch (S.Ending)
            {
                case "silme":
                    d.Headline = Loc.F("Bütün bölgeler özgür. YZ'nin son sunucusu {0}. vardiyada düştü.", shifts);
                    d.Sub = Loc.T("AĞA BAĞLANMA · ANALOG KAL");
                    d.SubIsMotto = true;
                    break;
                case "dagilma":
                    d.Headline = Loc.T("Sivil güven sıfıra indi; Sığınak 07 dağıldı.");
                    d.Sub = Loc.T(end.Text);
                    break;
                case "vuruldu":
                    d.Headline = Loc.F("Komutan kapıda vuruldu; Sığınak 07 {0}. vardiyada düştü.", shifts);
                    d.Sub = Loc.T(end.Text);
                    break;
                default:
                    d.Headline = Loc.F("Sığınak 07'nin yeri {0}. vardiyada YZ'ye sızdı.", shifts);
                    d.Sub = Loc.T(end.Text);
                    break;
            }

            d.Indicators.Add(new FinalIndicator { Label = Loc.T("SÜRE"), Value = shifts.ToString(), Sub = Loc.T("vardiya") });
            d.Indicators.Add(new FinalIndicator { Label = Loc.T("BASKIN"), Value = raids.ToString(), Sub = Loc.F("{0} başarılı · {1} başarısız", wins, raids - wins) });
            d.Indicators.Add(new FinalIndicator
            {
                Label = Loc.T("ÖZGÜR BÖLGE"), Value = Loc.F("{0}/{1}", free, regions.Count), Sub = Loc.F("{0}/{1} sektör geri alındı", captured, sectors),
                Tone = free == regions.Count ? OperationTone.Retaken : OperationTone.Plain,
            });
            d.Indicators.Add(new FinalIndicator
            {
                Label = Loc.T("SIĞINAK NÜFUSU"), Value = S.Liberation.Shelter.Population.ToString(), Sub = won ? Loc.T("kişi") : Loc.T("kişi kaldı"),
                Tone = won ? OperationTone.Plain : OperationTone.Alert,
            });

            // Şerit: harekât tam işgalle başlar; kayıt vardiya sonunda (gün = biten vardiya), bugünkü oranla biter
            d.Points.Add(new Vector2(1, 100));
            foreach (var h in S.Hist)
                if (h.Day >= 1 && h.Day < shifts && h.Day > d.Points[d.Points.Count - 1].x) d.Points.Add(new Vector2(h.Day, (float)h.Real));
            d.Points.Add(new Vector2(shifts, (float)now));

            var moments = Moments(sim, won);
            for (int i = 0; i < moments.Count; i++)
            {
                var m = moments[i];
                d.Moments.Add(new FinalMoment { Number = i + 1, Shift = Mathf.Clamp(m.Day + 1, 1, shifts), Code = Code(m.Day), Text = m.Text, Alert = m.Final && !won });
            }
            return d;
        }

        struct Candidate
        {
            public int Day, Order;
            public string Text;
            public bool Final;
        }

        /// <summary>Kırılma anları: zorunlular (ilk özgür bölge, kaybın nedeni, son olay), kalan yerlere karşı saldırılar, sonra öteki özgür bölgeler.</summary>
        static List<Candidate> Moments(Simulation sim, bool won)
        {
            var S = sim.S;
            var freed = S.RaidLog.Where(l => l.Success && l.LiberationReward?.RegionCompleted == true).ToList();
            var must = new List<Candidate>();
            var counters = S.Moments.Where(m => m.Kind == "bad").Select(m => new Candidate { Day = m.Day, Order = 1, Text = Counterattack(sim, m) }).ToList();
            var others = new List<Candidate>();

            Candidate? final = null;
            if (S.Ending == "silme" && freed.Count > 0)
            {
                var last = freed[freed.Count - 1];
                final = new Candidate { Day = last.Day, Order = 3, Text = Loc.F("{0} özgür, son sunucu düştü", Name(last.Region)), Final = true };
            }
            else if (S.Ending == "dagilma") final = new Candidate { Day = S.Day, Order = 3, Text = Loc.T("Güven 0 · sığınak dağıldı"), Final = true };
            else
            {
                var fatal = S.Door.Log.LastOrDefault(v => v.Ending != null);
                if (fatal != null) final = new Candidate { Day = fatal.Day, Order = 3, Text = Loc.F("Kapı açıldı · {0}", Loc.T(DoorCards.Get(fatal.Card).Who)), Final = true };
                else final = new Candidate { Day = S.Day, Order = 3, Text = Loc.T(GameData.Ending(S.Ending).Title), Final = true };
            }

            for (int i = 0; i < freed.Count; i++)
            {
                if (won && i == freed.Count - 1) continue;
                var c = new Candidate { Day = freed[i].Day, Order = 0, Text = i == 0 ? Loc.F("{0} özgür, ilk bölge", Name(freed[i].Region)) : Loc.F("{0} özgür", Name(freed[i].Region)) };
                if (i == 0) must.Add(c); else others.Add(c);
            }
            if (S.Ending == "dagilma" && Cause(sim) is Candidate cause) must.Add(cause);
            must.Add(final.Value);

            int room = Math.Max(0, MaxMoments - must.Count);
            var picked = Spread(counters, room);
            picked.AddRange(Spread(others, room - picked.Count));
            return must.Concat(picked).OrderBy(c => c.Day).ThenBy(c => c.Order).ToList();
        }

        /// <summary>En çok <paramref name="n"/> an; fazlaysa ilk ve son korunur, aradakiler eşit aralıkla.</summary>
        static List<Candidate> Spread(List<Candidate> xs, int n)
        {
            if (n <= 0) return new List<Candidate>();
            if (xs.Count <= n) return xs.ToList();
            if (n == 1) return new List<Candidate> { xs[xs.Count - 1] };
            return Enumerable.Range(0, n).Select(i => xs[(int)Math.Round(i * (xs.Count - 1) / (double)(n - 1))]).ToList();
        }

        /// <summary>
        /// “YZ karşı saldırısı: Doğu Avrupa 01 yeniden işgal”. Sektör anın metninden bulunur (an, yazıldığı dilde durur: her dilin
        /// biçimi denenir); bulunamazsa metin olduğu gibi.
        /// </summary>
        static string Counterattack(Simulation sim, Moment m)
        {
            foreach (var s in sim.S.Liberation.Regions.SelectMany(r => r.Sectors))
                foreach (var lang in Loc.Languages)
                {
                    // Anlar kayıttan yeniden oynatılınca bugünkü oda adıyla yazılır (sektör adı bölgeden)
                    var loc = DeskLoc.Instance;
                    string text = string.Format(loc.In(Loc.N("{0}: {1} yeniden işgal altında."), lang), loc.In(GameData.Region(s.Region).SourceName, lang),
                        loc.In(GameData.Region(s.Region).SourceSector(s.Index), lang));
                    if (text == m.Text) return Loc.F("YZ karşı saldırısı: {0} {1:00} yeniden işgal", Name(s.Region), s.Index + 1);
                }
            return Loc.F("YZ karşı saldırısı: {0}", m.Text.TrimEnd('.'));
        }

        /// <summary>Dağılma'nın nedeni: son güven düşüşü (kapıda verilen karar ya da sivil sunucu kıran baskın); karşı saldırıysa o zaten listede.</summary>
        static Candidate? Cause(Simulation sim)
        {
            var S = sim.S;
            var door = S.Door.Log.Where(v => v.Trust < 0).Select(v => (v.Day, Text: Door(v))).LastOrDefault();
            var raid = S.RaidLog.Where(l => l.LiberationReward != null && l.LiberationReward.Trust < 0 && l.Collateral > 0)
                .Select(l => (l.Day, Text: Loc.F("Baskında {0} sivil sunucu kırıldı · güven {1}", l.Collateral, DeskFormat.Signed(l.LiberationReward.Trust, 0)))).LastOrDefault();
            if (door.Text == null && raid.Text == null) return null;
            var last = raid.Text == null || door.Text != null && door.Day >= raid.Day ? door : raid;
            int counter = S.Moments.Where(m => m.Kind == "bad").Select(m => m.Day).DefaultIfEmpty(-1).Max();
            if (counter > last.Day) return null;
            return new Candidate { Day = last.Day, Order = 2, Text = last.Text };
        }

        static string Door(DoorVisit v)
        {
            var card = DoorCards.Exists(v.Card) ? DoorCards.Get(v.Card) : null;
            if (card == null) return Loc.F("Kapı · güven {0}", DeskFormat.Signed(v.Trust, 0));
            string label = DoorViewBuilder.ChoiceLabel(v, card, out _);
            return label == null
                ? Loc.F("Kapı: {0} · güven {1}", Loc.T(card.Who), DeskFormat.Signed(v.Trust, 0))
                : Loc.F("Kapı: {0} · “{1}” · güven {2}", Loc.T(card.Who), Loc.T(label), DeskFormat.Signed(v.Trust, 0));
        }
    }
}
