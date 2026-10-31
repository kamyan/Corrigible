using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Kapı kâğıdının evresi: kapalı kapı (bak, sor, aç, açma), açık kapı (seçim), sonuç.</summary>
    public enum DoorPhase { None, Closed, Open, Result }

    /// <summary>
    /// Kapı kâğıdını kurar (saf C#, Revizyon 2 · S4): kapalı kapıda duyulan; gözetleme deliğinden görülen ve interkomdan
    /// alınan cevap ayrı bloklarda; seçenekler sağda bedel/etki ya da gerekçeyle; sonuçta seçim satırı, metin ve etki,
    /// ölümcülde damga. Gözetleme ve interkom sunumdur, komut değildir. Metinler simülasyonun kart tablosundan çevrilir.
    /// Kapı açılınca koridorda duran siluetin türü de burada (<see cref="Silhouette"/>).
    /// </summary>
    public static class DoorViewBuilder
    {
        /// <summary>Sunumun kendi düğmeleri (komut değil): gözetleme deliği, interkom, kâğıdı kapat, son rapor.</summary>
        public const string Peek = "peek", Ask = "ask", Close = "close", Ending = "end";

        /// <summary>
        /// Kart → koridordaki siluet (IMPLEMENTATION_MAP §4, Soru 5'in ara cevabı). Altı tür + genel biçim; kalabalık
        /// kartlar birden çok siluet, çocuklar küçük siluet. Kimse durmayan kartta (zarf, paket) boş. Yeni kart eklenince
        /// <c>SiginakDoorTests</c> eksik eşlemede kırılır.
        /// </summary>
        static readonly Dictionary<string, string[]> Visitors = new Dictionary<string, string[]>
        {
            [DoorCards.First] = new[] { VisitorSilhouette.Yasli },
            ["anne"] = new[] { VisitorSilhouette.Anne },
            ["anne_oglu"] = new[] { VisitorSilhouette.Asker },
            ["kurye"] = new[] { VisitorSilhouette.Asker },
            ["kacakci"] = new[] { VisitorSilhouette.Generic },
            ["kacakci_intikam"] = new string[0],
            ["teknisyen"] = new[] { VisitorSilhouette.Yarali },
            ["teknisyen_sadik"] = new[] { VisitorSilhouette.Generic },
            ["teknisyen_casus"] = new[] { VisitorSilhouette.Generic },
            ["gozcu"] = new[] { VisitorSilhouette.Yarali },
            ["kopya"] = new[] { VisitorSilhouette.Kopya },
            ["disarida"] = new[] { VisitorSilhouette.Asker },
            ["heyet"] = new[] { VisitorSilhouette.Generic, VisitorSilhouette.Generic, VisitorSilhouette.Generic },
            ["silahli"] = new[] { VisitorSilhouette.Silahli },
            ["silahli_baba"] = new[] { VisitorSilhouette.Silahli },
            ["baba"] = new[] { VisitorSilhouette.Generic },
            ["hekim"] = new[] { VisitorSilhouette.Yasli },
            ["cocuklar"] = new[] { VisitorSilhouette.Child, VisitorSilhouette.Child, VisitorSilhouette.Child },
            ["amator"] = new[] { VisitorSilhouette.Generic },
            ["aile"] = new[] { VisitorSilhouette.Anne, VisitorSilhouette.Generic },
            ["zarf"] = new string[0],
            ["ogretmen"] = new[] { VisitorSilhouette.Yasli },
            ["hirsiz"] = new[] { VisitorSilhouette.Generic, VisitorSilhouette.Generic, VisitorSilhouette.Generic },
        };

        /// <summary>Ölümcül kapının kâğıttaki son cümlesi (sonun kendi metni son raporda).</summary>
        static readonly Dictionary<string, string> Epilogues = new Dictionary<string, string>
        {
            ["vuruldu"] = Loc.N("Sığınak 07'nin komutanı düştü. Harekât burada biter."),
            ["sizma"] = Loc.N("Kopya sığınağın içinde. Harekât burada biter."),
        };

        static readonly string EpilogueDefault = Loc.N("Harekât burada biter.");
        /// <summary>Kapalı kapının ortak seçenekleri (simülasyondakiyle aynı kaynak metin).</summary>
        static readonly string OpenLabel = Loc.N("Kapıyı aç"), RefuseLabel = Loc.N("Açma");

        /// <summary>Kartın eşlemesi var mı (test: her kartın bir karşılığı olmalı).</summary>
        public static bool HasSilhouette(string card) => Visitors.ContainsKey(card);

        /// <summary>Kapı açılınca koridorda duranlar (tür adları); kimse yoksa boş. Eşlemesi olmayan kartta genel biçim.</summary>
        public static string[] Silhouette(string card) => Visitors.TryGetValue(card, out var kinds) ? kinds : new[] { VisitorSilhouette.Generic };

        /// <summary>
        /// Siluetin ziyarete bağlı küçük çeşitlemesi (S4d): boy ±%4 ve aksesuarın tarafı. Sunumdur; ziyaretin gününden ve
        /// kartından türetilir (aynı ziyaret hep aynı görünür, simülasyonun rastgeleliğine dokunmaz).
        /// </summary>
        public static (float height, bool mirror) Variation(DoorVisit v)
        {
            uint h = 2166136261;
            foreach (char c in v.Card ?? "") h = (h ^ c) * 16777619;
            h = (h ^ (uint)v.Day) * 16777619;
            h ^= h >> 13;
            return (0.96f + 0.08f * (h % 1000) / 999f, (h >> 10 & 1) == 1);
        }

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
                    d.Body = Loc.T(card.Knock);
                    if (peeked) d.Observations.Add(new DoorObservation { Label = Loc.T("GÖZETLEME DELİĞİ"), Text = Loc.T(card.Peek) });
                    if (asked) d.Observations.Add(new DoorObservation { Label = Loc.T("İNTERKOM"), Text = Loc.T(card.Ask), Intercom = true });
                    d.Buttons.Add(new DoorButtonData { Id = Peek, Label = Loc.T("Gözetleme deliğinden bak"), Used = peeked ? Loc.T("BAKILDI") : null });
                    d.Buttons.Add(new DoorButtonData { Id = Ask, Label = Loc.T("İnterkomdan parolayı sor"), Used = asked ? Loc.T("SORULDU") : null });
                    foreach (var o in sim.DoorOptions()) d.Buttons.Add(Option(sim, o, false));
                    d.HintKey = "Esc";
                    d.Hint = Loc.T("masaya dön · ziyaretçi vardiya bitene kadar bekler");
                    break;
                case DoorPhase.Open:
                    d.Body = Paragraphs(Loc.T(card.Opened), visit.Result == null ? null : Loc.T(visit.Result));
                    foreach (var o in sim.DoorOptions()) d.Buttons.Add(Option(sim, o, true));
                    d.HintKey = "Esc";
                    d.Hint = Loc.T("masaya dön · kapı açık bekler");
                    break;
                default:
                    d.Choice = Choice(visit, card);
                    if (visit.Ending != null)
                    {
                        d.Fatal = true;
                        d.Body = visit.Result == null ? null : Loc.T(visit.Result);
                        d.Stamp = DeskText.Upper(GameData.Ending(visit.Ending).Title);
                        d.Epilogue = Loc.T(Epilogues.TryGetValue(visit.Ending, out var line) ? line : EpilogueDefault);
                        d.ActionId = Ending;
                        d.Action = Loc.T("SON RAPORA GEÇ");
                        break;
                    }
                    string effects = Effects(visit);
                    d.Body = Paragraphs(visit.Result == null ? null : Loc.T(visit.Result), effects == null ? null : "<b>" + effects + "</b>");
                    d.ActionId = Close;
                    d.Action = Loc.T("KAPIYI KAPAT");
                    break;
            }
            return d;
        }

        /// <summary>Sonuçtaki seçim satırı: “SEÇİM · 3 KAPIYI AÇ” (rakam seçeneğin kâğıttaki kısayolu); seçim yoksa null.</summary>
        static string Choice(DoorVisit visit, DoorCard card)
        {
            string label = ChoiceLabel(visit, card, out int i);
            return label == null ? null : Loc.F("SEÇİM · {0} {1}", i + 1, DeskText.Upper(Loc.T(label)));
        }

        /// <summary>
        /// Ziyaretin son seçiminin kaynak etiketi (çevrilmemiş; gösterirken <c>Loc.T</c>) ve kâğıttaki sırası (0 tabanlı);
        /// seçim yoksa null. Kapalı kapı: bak, sor, kapalı seçenekler, aç, açma; açık kapı: kartın seçenekleri.
        /// </summary>
        public static string ChoiceLabel(DoorVisit visit, DoorCard card, out int index)
        {
            index = -1;
            if (visit.Choice == null) return null;
            var closed = card.Closed.Select(o => (o.Id, o.Label)).ToList();
            closed.Add((Simulation.DoorOpen, OpenLabel));
            closed.Add((Simulation.DoorRefuse, RefuseLabel));
            int i = closed.FindIndex(o => o.Id == visit.Choice);
            if (i >= 0) { index = i + 2; return closed[i].Label; }
            i = System.Array.FindIndex(card.Options, o => o.Id == visit.Choice);
            if (i < 0) return null;
            index = i;
            return card.Options[i].Label;
        }

        static DoorButtonData Option(Simulation sim, DoorOption o, bool open)
        {
            string why = sim.DoorAnswerCheck(o.Id);
            var e = sim.DoorOptionEffect(o.Id);
            var b = new DoorButtonData { Id = o.Id, Label = Loc.T(o.Label) };
            var (supplies, materials, fon) = Shortfall(sim, o, e);
            if (why != null && (supplies || materials || fon))
            {
                // Yetersiz kaynak: satır açık kalır, eksik kalem üstü çizik; basınca komut denenir, teleks gerekçeyi basar
                b.State = SlipButtonState.Short;
                b.Detail = Price(e, supplies, materials, fon) + " · " +
                           (supplies ? Loc.T("erzak yetersiz") : materials ? Loc.T("malzeme yetersiz") : Loc.T("fon yetersiz"));
            }
            else if (why != null)
            {
                b.State = SlipButtonState.Disabled;
                b.Detail = Reason(why);
            }
            else
            {
                // Bedel ve (açık kapıda) güvene etkisi sağda: “−8 erzak”, “güven −1”
                var parts = new List<string>();
                if (o.Paid && Price(e) is string price) parts.Add(price);
                if (open && e != null && e.Trust != 0) parts.Add(Loc.F("güven {0}", DeskFormat.Signed(e.Trust, 0)));
                b.Detail = parts.Count == 0 ? null : string.Join(" · ", parts);
            }
            return b;
        }

        /// <summary>Seçeneği yalnız bedeli engelliyor mu: hangi kalem eksik (koşul, son, baskın engeliyse hiçbiri).</summary>
        static (bool supplies, bool materials, bool fon) Shortfall(Simulation sim, DoorOption o, DoorEffect e)
        {
            if (!o.Paid || e == null || sim.S.Ending != null || sim.S.Raid != null || o.Need?.Invoke(sim) != null) return default;
            return (e.Supplies < 0 && sim.Shelter.Supplies < -e.Supplies - 1e-9, e.Materials < 0 && sim.Shelter.Materials < -e.Materials - 1e-9,
                e.Fon < 0 && sim.S.Fon < -e.Fon - 1e-9);
        }

        /// <summary>Gerekçe sağ sütunda küçük harfle ve noktasız (“revirde görevli yok”); kısaltma ile başlıyorsa olduğu gibi.</summary>
        static string Reason(string why)
        {
            string s = why.TrimEnd('.');
            if (s.Length < 2 || !char.IsLower(s[1])) return s;
            return DeskText.Lower(s.Substring(0, 1), DeskFormat.Lang) + s.Substring(1);
        }

        /// <summary>Seçeneğin bedeli (“−5 erzak · −10 fon”); bedel yoksa null.</summary>
        public static string Price(DoorEffect e) => Price(e, false, false, false);

        static string Price(DoorEffect e, bool strikeSupplies, bool strikeMaterials, bool strikeFon)
        {
            if (e == null) return null;
            var parts = new List<string>();
            string Strike(string s, bool on) => on ? "<s>" + s + "</s>" : s;
            if (e.Supplies < 0) parts.Add(Strike(Loc.F("{0} erzak", DeskFormat.Signed(e.Supplies, 0)), strikeSupplies));
            if (e.Materials < 0) parts.Add(Strike(Loc.F("{0} malzeme", DeskFormat.Signed(e.Materials, 0)), strikeMaterials));
            if (e.Fon < 0) parts.Add(Strike(Loc.F("{0} fon", DeskFormat.Signed(e.Fon, 0)), strikeFon));
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
