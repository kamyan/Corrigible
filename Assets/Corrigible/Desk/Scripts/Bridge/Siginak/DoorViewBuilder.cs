using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Kapı kâğıdının evresi: kapalı kapı (bak, sor, aç, açma), açık kapı (seçim), sonuç.</summary>
    public enum DoorPhase { None, Closed, Open, Result }

    /// <summary>
    /// Kapı kâğıdını kurar (saf C#): kapalı kapıda duyulan, gözetleme deliğinden görülen ve interkomdan alınan cevap; açık
    /// kapıda söylenen ve seçenekler (bedeli ya da yapılamama gerekçesiyle); sonuçta ziyaretin metni ve etkisi. Gözetleme ve
    /// interkom sunumdur, komut değildir. Metinler simülasyonun kart tablosundan çevrilir.
    /// </summary>
    public static class DoorViewBuilder
    {
        /// <summary>Sunumun kendi düğmeleri (komut değil): gözetleme deliği, interkom, kâğıdı kapat, son rapor.</summary>
        public const string Peek = "peek", Ask = "ask", Close = "close", Ending = "end";

        public static DoorCardData Build(Simulation sim, DoorPhase phase, DoorVisit visit, bool peeked, bool asked)
        {
            var card = DoorCards.Get(visit.Card);
            var d = new DoorCardData
            {
                Head = Loc.T("KAPI · SIĞINAK 07"),
                Code = Loc.F("VARDİYA {0:000}", visit.Day + 1),
                Title = DeskText.Upper(Loc.T(card.Who)),
            };
            switch (phase)
            {
                case DoorPhase.Closed:
                    d.Body = Closed(card, peeked, asked);
                    d.Buttons.Add(new DoorButtonData { Id = Peek, Label = Loc.T("Gözetleme deliğinden bak"), Enabled = !peeked });
                    d.Buttons.Add(new DoorButtonData { Id = Ask, Label = Loc.T("İnterkomdan parolayı sor"), Enabled = !asked });
                    foreach (var o in sim.DoorOptions()) d.Buttons.Add(Option(sim, o, o.Id == Simulation.DoorOpen));
                    d.Hint = Loc.T("Esc · masaya dön; ziyaretçi vardiya bitene kadar bekler");
                    break;
                case DoorPhase.Open:
                    d.Body = Paragraphs(Loc.T(card.Opened), visit.Result == null ? null : Loc.T(visit.Result));
                    foreach (var o in sim.DoorOptions()) d.Buttons.Add(Option(sim, o, false));
                    d.Hint = Loc.T("Esc · masaya dön; kapı açık bekler");
                    break;
                default:
                    var lines = new List<string>();
                    if (visit.Result != null) lines.Add(Loc.T(visit.Result));
                    string effects = Effects(visit);
                    if (effects != null) lines.Add("<b>" + effects + "</b>");
                    d.Body = Paragraphs(lines.ToArray());
                    if (visit.Ending != null)
                    {
                        d.Stamp = DeskText.Upper(GameData.Ending(visit.Ending).Title);
                        d.Buttons.Add(new DoorButtonData { Id = Ending, Label = Loc.T("Son rapor"), Primary = true });
                        d.Hint = Loc.T("Enter · son rapor");
                    }
                    else
                    {
                        d.Buttons.Add(new DoorButtonData { Id = Close, Label = Loc.T("Kapıyı kapat"), Primary = true });
                        d.Hint = Loc.T("Enter · kapıyı kapat");
                    }
                    break;
            }
            return d;
        }

        static string Closed(DoorCard card, bool peeked, bool asked)
        {
            var parts = new List<string> { Loc.T(card.Knock) };
            if (peeked) parts.Add("<b>" + Loc.T("Gözetleme deliği.") + "</b> " + Loc.T(card.Peek));
            if (asked) parts.Add("<b>" + Loc.T("İnterkom.") + "</b> " + Loc.T(card.Ask));
            return Paragraphs(parts.ToArray());
        }

        static DoorButtonData Option(Simulation sim, DoorOption o, bool primary)
        {
            string why = sim.DoorAnswerCheck(o.Id);
            var e = sim.DoorOptionEffect(o.Id);
            return new DoorButtonData
            {
                Id = o.Id, Label = Loc.T(o.Label), Primary = primary, Enabled = why == null,
                Detail = why ?? (o.Paid ? Price(e) : null),
            };
        }

        /// <summary>Seçeneğin bedeli (“−5 erzak · −10 fon”); bedel yoksa null.</summary>
        public static string Price(DoorEffect e)
        {
            if (e == null) return null;
            var parts = new List<string>();
            if (e.Supplies < 0) parts.Add(Loc.F("{0} erzak", DeskFormat.Signed(e.Supplies, 0)));
            if (e.Materials < 0) parts.Add(Loc.F("{0} malzeme", DeskFormat.Signed(e.Materials, 0)));
            if (e.Fon < 0) parts.Add(Loc.F("{0} fon", DeskFormat.Signed(e.Fon, 0)));
            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        /// <summary>Ziyaretin gerçekleşen etkisi tek satırda (“Erzak +6 · Güven +1”); etkisi yoksa null.</summary>
        public static string Effects(DoorVisit v)
        {
            var parts = new List<string>();
            if (System.Math.Abs(v.Supplies) >= 0.05) parts.Add(Loc.F("Erzak {0}", DeskFormat.Signed(v.Supplies, 0)));
            if (System.Math.Abs(v.Materials) >= 0.05) parts.Add(Loc.F("Malzeme {0}", DeskFormat.Signed(v.Materials, 0)));
            if (System.Math.Abs(v.Fon) >= 0.05) parts.Add(Loc.F("Fon {0}", DeskFormat.Signed(v.Fon, 0)));
            if (v.Population != 0) parts.Add(Loc.F("Sakin {0}", DeskFormat.Signed(v.Population, 0)));
            if (v.Plates != 0) parts.Add(Loc.F("Zırh plakası {0}", DeskFormat.Signed(v.Plates, 0)));
            if (v.Healed > 0) parts.Add(Loc.F("{0} yara sarıldı", v.Healed));
            if (v.Wounded > 0) parts.Add(Loc.F("Uzmanlara {0} yara", v.Wounded));
            if (v.Trust != 0) parts.Add(Loc.F("Güven {0}", DeskFormat.Signed(v.Trust, 0)));
            if (System.Math.Abs(v.Alarm) >= 0.05) parts.Add(Loc.F("YZ alarmı {0}", DeskFormat.Signed(v.Alarm, 0)));
            if (v.Counterattack) parts.Add(Loc.T("YZ karşı saldırdı"));
            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        /// <summary>Teleks satırı: “Alt kattan bir anne · Erzak −5 · Güven +2” (etkisi yoksa yalnız kim).</summary>
        public static string Telex(DoorVisit v)
        {
            string who = Loc.T(DoorCards.Get(v.Card).Who), effects = Effects(v);
            return effects == null ? who : who + " · " + effects;
        }

        static string Paragraphs(params string[] parts) => string.Join("\n\n", parts.Where(p => !string.IsNullOrEmpty(p)));
    }
}
