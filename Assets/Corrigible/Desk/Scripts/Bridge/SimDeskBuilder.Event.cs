using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // Olay bildirimi (C3, F8): oyunun olay kartları (ikilemler) ve YZ'nin hediye teklifi aynı kâğıtla gösterilir.
    // Kartlar yalnızca “etiket + etki metni” taşır; etki metni (Türkçe kaynak) bedel/kazanç satırlarına ayrıştırılır ve
    // önizleme şimdiki değerlerden hesaplanır. Seçim 1/2/3 ile imzalanır (SimDeskCommands.SignEventOption).
    public sealed partial class SimDeskBuilder
    {
        static readonly float[] MemoRect = { 236, 60, 712, -1 };
        static readonly float[] LogRect = { 1296, 206, 548, -1 };
        static readonly float[] MemoPin = { 926, 82 };
        static readonly string[] OptionIcons = { "power", "lens", "hourglass" };

        /// <summary>Hediyede seçenek sırası (1/2/3) → Simulation kararı.</summary>
        public static readonly string[] GiftChoices = { Simulation.GiftAccept, Simulation.GiftControlled, Simulation.GiftReject };

        EventVM Dilemma()
        {
            var p = S.PendingEvent;
            var def = Simulation.Dilemma(p.Id);
            var r = S.Region(p.Region);
            string name = Def(r.Id).Name;
            // Bölgeye bağlı kartın başlığında ya da metninde bölge adı geçer; diğerleri küresel (Pick ilk bölgeyi verir)
            bool regional = def.Title.Contains("{0}") || def.Body.Contains("{0}");
            var e = Paper(Up(Loc.F(Loc.T(def.Title), name)), Loc.F(Loc.T(def.Body), name), regional ? r.Id : null, p.Day);
            for (int i = 0; i < def.Choices.Length; i++)
            {
                var ch = def.Choices[i];
                var lines = ParseEffect(ch.Effect, regional ? name : null);
                // Etki satırlara ayrıştıysa açıklama tekrar etmez; ayrıştırılamayan kart metni açıklamada kalır
                string desc = lines.Count == 0 ? Loc.T(ch.Effect) : null;
                var o = Option(i, Up(Loc.T(ch.Label)), desc, lines, regional ? r : null);
                string why = sim.ChoiceBlocked(i);
                if (why != null) o.Risk = Loc.F("<b>Yapılamaz:</b> {0}", why);
                e.Options.Add(o);
            }
            return Keys(e);
        }

        /// <summary>Alt şeritteki seçim tuşları seçenek sayısı kadar.</summary>
        static EventVM Keys(EventVM e)
        {
            var hint = e.Footer?.Hints.LastOrDefault();
            if (hint != null) { hint.Keys.Clear(); for (int i = 0; i < e.Options.Count; i++) hint.Keys.Add((i + 1).ToString()); }
            return e;
        }

        EventVM Gift()
        {
            var g = S.PendingGift;
            var r = S.Region(g.Region);
            string name = Def(r.Id).Name;
            var e = Paper(Up(g.Gift.Title),
                Loc.F("Kaynağı belirsiz bir araştırma ağı, {0} hükümetine {1} sunuyor. Bilim insanları sonuçları doğruladı. Kodu kimin yazdığını kimse bilmiyor.", name, g.Gift.Text),
                r.Id, S.Day, Loc.T("TEKLİF"));
            float giftInf = (float)sim.B.giftInf;
            var accept = new List<EffectLine>
            {
                new EffectLine("ekonomi", 10, name), new EffectLine("güven", 5, name),
                new EffectLine("bağımlılık", 15, name), new EffectLine("sızma", giftInf, name),
            };
            var controlled = new List<EffectLine>
            {
                new EffectLine("fon", -20, null), new EffectLine("ekonomi", 10, name), new EffectLine("güven", 5, name),
                new EffectLine("bağımlılık", 5, name), new EffectLine("sızma", giftInf / 2, name),
            };
            var reject = new List<EffectLine>
            {
                new EffectLine("güven", (float)(-15 * (S.Done("bilinc") ? 0.5 : 1)), name),
                new EffectLine("küresel güven", (float)(-sim.B.rejectTrust * (S.Done("bilinc") ? 0.5 : 1)), null),
                new EffectLine("SG", (float)-sim.B.rejectSg, null),
            };
            e.Options.Add(Option(0, Loc.T("KABUL ET"), Loc.T("Halk sevinir; YZ bölgeye bir kapı daha açar."), accept, r));
            var c = Option(1, Loc.T("DENETİMLİ KABUL"), Loc.T("Analog yedekle devreye al: etkisi yarıya iner."), controlled, r);
            if (!S.Done("analog")) c.Risk = Loc.T("<b>Yapılamaz:</b> Denetimli kabul için Analog Yedekler gerekli.");
            else if (S.Fon < 20) c.Risk = Loc.T("<b>Yapılamaz:</b> Denetimli kabul 20 fon ister.");
            e.Options.Add(c);
            e.Options.Add(Option(2, Loc.T("REDDET"), Loc.T("Hayat kurtaracak teknolojiyi engellemiş olursun."), reject, r));
            return Keys(e);
        }

        /// <summary>Bildirim kâğıdı, spot (bölgeye bağlıysa), ele geçirilmiş günlük ve alt şerit.</summary>
        EventVM Paper(string title, string body, string regionId, int day, string kind = null)
        {
            var e = new EventVM();
            string regionText;
            if (regionId != null)
            {
                var c = Center(regionId);
                e.Spotlight = new EventSpotVM { Region = regionId, Center = A(c), HoleDiameter = 148, Ring = 12, ScrimAlpha = 0.78f };
                // Halka ve iplik her zaman verilir; bölge kâğıtların altında kalıyorsa görünüm gizler (kâğıtların ve haritanın
                // sahnedeki gerçek yerine bakar; spot yine çizilir, Boşluk basılıyken harita görünür)
                e.MarkerRing = new EventRingVM { Center = A(c + new Vector2(0, -2)), Sprite = "modal_marker_ring", Color = "#B8241A", Alpha = 0.92f };
                e.String = new EventStringVM { From = MemoPin, To = A(PinOf(regionId)), Color = "#B8241A", Width = 2.2f, ShadowAlpha = 0.35f };
                var r = S.Region(regionId);
                string state = r.Off > 0 ? Loc.T("çevrimdışı") : r.Q ? Loc.T("karantinada") : r.Monitored ? Loc.T("izleniyor") : Loc.T("izlenmiyor");
                regionText = Up(Def(regionId).Name) + " · " + Code(regionId) + " · " + state;
            }
            else regionText = Loc.T("KÜRESEL");

            e.Memo = new EventMemoVM
            {
                Rect = MemoRect,
                Header = new HeaderVM { Title = Loc.T("KYGA · OLAY BİLDİRİMİ"), Right = Loc.F("No. OB-{0} · tek nüsha", day.ToString("000")) },
                Fields =
                {
                    new EventFieldVM { K = Loc.T("TARİH"), V = Day(day) + " · " + TimeText },
                    new EventFieldVM { K = Loc.T("ÖNCELİK"), Stamp = new StampVM { Text = kind ?? Loc.T("ACİL"), Color = "red", FontSize = 13, Rot = -4 } },
                    new EventFieldVM { K = Loc.T("BÖLGE"), V = regionText },
                    new EventFieldVM { K = Loc.T("KARAR"), V = Loc.T("Masa şefi") },
                },
                Title = title,
                Subtitle = regionId != null
                    ? Loc.F("{0} · yerel güven {1} · bağımlılık {2}", Def(regionId).Name, N(S.Region(regionId).Trust, 0), N(S.Region(regionId).Dep, 0))
                    : Loc.F("Küresel güven {0} · fon {1} · siyasi güç {2}", N(sim.Global().Trust), N(S.Fon), N(S.Sg, 0)),
                Body = body,
            };
            // Kaynaklar: bölgeyle (ya da en son) ilgili haberler
            var news = (regionId != null ? S.News.Where(h => h.Src == regionId) : S.News).Take(3).ToList();
            if (news.Count > 0)
            {
                e.Memo.SourcesHeader = new HeaderVM { Title = Loc.T("KAYNAKLAR"), Right = news.Count.ToString() };
                foreach (var h in news)
                {
                    var t = TelexItem(h);
                    e.Memo.Sources.Add(new EventSourceVM
                    {
                        Trust = t.Fake ? "suspicious" : t.Trust, StampRot = t.Trust == "verified" ? -3 : (float?)null,
                        Title = Loc.T(h.Agency), Text = h.Text, Day = Day(h.Day), Carbon = t.Trust == "report",
                    });
                }
            }
            if (S.Log.Count > 0)
            {
                var lines = S.Log.Take(4).Select(l => "> " + l.Text).ToList();
                e.AiLog = new EventLogVM
                {
                    Rect = LogRect, RotDeg = 1.2f, Header = Loc.T("YZ GÜNLÜĞÜ · ELE GEÇİRİLDİ"), Right = Day(S.Log[0].Day),
                    Lines = lines, Footer = Loc.F("Kaynak: karantina tamponu · {0} satır", lines.Count),
                };
            }
            e.Footer = new EventFooterVM
            {
                Text = Loc.T("Süre durdu: karar verilene kadar oyun ilerlemez. Bu olay ertelenemez."),
                Hints =
                {
                    new EventHintVM { Keys = { Loc.T("Boşluk") }, Text = Loc.T("basılı: haritaya bak") },
                    new EventHintVM { Keys = { "1", "2", "3" }, Text = Loc.T("seç") },
                },
            };
            return e;
        }

        // ------------------------------------------------------------------ seçenekler
        EventOptionVM Option(int i, string title, string desc, List<EffectLine> lines, RegionState r)
        {
            var o = new EventOptionVM
            {
                Key = (i + 1).ToString(), Title = title, Icon = OptionIcons[Mathf.Min(i, OptionIcons.Length - 1)], Desc = desc, State = "normal",
                PreviewSlot = Loc.T("Üstüne gel: göstergelere etkisi"),
                Sign = new CommandVM { Label = Loc.T("Kararı imzala"), Kind = i == 0 ? "ink" : "paper", Key = i == 0 ? "Enter" : (i + 1).ToString() },
                Preview = new EventPreviewVM { Cap = Loc.T("ÖNİZLEME"), Right = Loc.F("imzalanırsa · {0}", Day(S.Day + 1)) },
            };
            foreach (var l in lines)
            {
                var vm = l.ToLine();
                (l.Good ? o.Gains : o.Costs).Add(vm);
                var pr = Preview(l, r);
                if (pr != null && o.Preview.Rows.Count < 3) o.Preview.Rows.Add(pr);
            }
            if (o.Preview.Rows.Count == 0) o.Preview = null;
            return o;
        }

        /// <summary>Önizleme satırı: şimdiki değer → imzadan sonra (bilinen değerlerde kesin, sızmada gösterilen değerden tahmin).</summary>
        EventPreviewRowVM Preview(EffectLine l, RegionState r)
        {
            if (l.Text != null) return null;
            switch (l.Metric)
            {
                case "fon": return Row(Loc.T("Fon"), S.Fon, l.Value, 1, "verified", Loc.T("kesin"));
                case "SG": return Row(Loc.T("Siyasi güç"), S.Sg, l.Value, 0, "verified", Loc.T("kesin"));
                case "küresel güven": return Row(Loc.T("Küresel güven"), sim.Global().Trust, l.Value, 1, "report", Loc.T("tahmin"));
            }
            if (r == null || l.Region == null) return null;
            switch (l.Metric)
            {
                case "güven": return Row(l.Label, r.Trust, l.Value, 0, "verified", Loc.T("kesin"));
                case "bağımlılık": return Row(l.Label, r.Dep, l.Value, 0, "verified", Loc.F("veto {0}", N(sim.VetoThreshold, 0)));
                case "ekonomi": return Row(l.Label, r.Econ, l.Value, 0, "verified", Loc.T("kesin"));
                case "sızma":
                    var v = sim.Shown(r);
                    if (v.V == null) return null;
                    return new EventPreviewRowVM
                    {
                        Label = l.Label, Trust = TrustOf(v), Right = Loc.T("tahmin"),
                        Value = Pct(v.V.Value) + " → " + Pct(Mathf.Clamp((float)v.V.Value + l.Value, 0, 100)),
                    };
            }
            return null;
        }

        static EventPreviewRowVM Row(string label, double now, float delta, int d, string trust, string right) => new EventPreviewRowVM
        {
            Label = label, Trust = trust, Right = right,
            Value = N(now, d) + " → " + N(Mathf.Clamp((float)now + delta, 0, now > 100 || d == 1 ? 9999 : 100), d),
        };

        // ------------------------------------------------------------------ etki metni ayrıştırma
        /// <summary>Etki satırı: ölçü (fon, SG, güven, bağımlılık, ekonomi, sızma, araştırma, YZ) + değer, ya da serbest metin.</summary>
        public sealed class EffectLine
        {
            public string Metric, Region, Text, Unit;
            public float Value;
            public bool Global;

            public EffectLine() { }
            public EffectLine(string metric, float value, string region) { Metric = metric; Value = value; Region = region; }

            /// <summary>Oyuncu için iyi mi: kaynak/güven/ekonomi artışı, bağımlılık/sızma/YZ puanı düşüşü, araştırma ilerlemesi.</summary>
            public bool Good => Text != null || Metric switch
            {
                "bağımlılık" or "sızma" or "YZ" => Value < 0,
                _ => Value > 0,
            };

            public string Label => Metric switch
            {
                "fon" => Loc.T("Fon"),
                "SG" => Loc.T("Siyasi güç"),
                "araştırma" => Loc.T("Araştırma"),
                "YZ" => Loc.T("YZ hesaplama puanı"),
                "küresel güven" => Loc.T("Küresel güven"),
                "güven" => Region != null ? Loc.F("{0} güveni", Region) : Global ? Loc.T("Bütün bölgelerde güven") : Loc.T("Güven"),
                "bağımlılık" => Region != null ? Loc.F("{0} bağımlılığı", Region) : Global ? Loc.T("Bütün bölgelerde bağımlılık") : Loc.T("Bağımlılık"),
                "ekonomi" => Region != null ? Loc.F("{0} ekonomisi", Region) : Loc.T("Ekonomi"),
                "sızma" => Region != null ? Loc.F("{0} sızması", Region) : Loc.T("Sızma"),
                _ => Text,
            };

            public EventLineVM ToLine()
            {
                if (Text != null) return new EventLineVM { Label = Text, Value = "", ValueIsText = true, Trust = "unknown" };
                string v = DeskFormat.Signed(Value, Mathf.Approximately(Value, Mathf.Round(Value)) ? 0 : 1) + (Unit ?? "");
                return new EventLineVM { Label = Label, Value = v, Trust = Metric is "sızma" ? "report" : null };
            }
        }

        static readonly Regex SignedRx = new Regex(@"^([−+-])(\d+(?:,\d+)?)\s+(SG|fon)(?:\s*\(.*\))?$");
        static readonly Regex ScopedRx = new Regex(@"^(bölgede |bütün bölgelerde )?(güven|bağımlılık|ekonomi|sızma) ([−+-])(\d+)$");
        static readonly Regex ResearchRx = new Regex(@"^araştırma ([−+-]?)(\d+) gün( geriler)?$");
        static readonly Regex AiRx = new Regex(@"^YZ \+(\d+) hesaplama puanı(?:\s*\(.*\))?$");

        /// <summary>Etki metinlerindeki serbest parçalar (çeviri aracı toplasın diye işaretli).</summary>
        static readonly string[] EffectTexts = { Loc.N("%65 ihtimalle çekirdeğin yeri ortaya çıkar"), Loc.N("karantina biter") };

        static float Sign(string s) => s == "+" || s == "" ? 1 : -1;

        /// <summary>Kartın Türkçe etki metnini satırlara ayırır (“−5 SG · bölgede güven +15”, “bölgede ekonomi +10, bağımlılık +15”).</summary>
        public static List<EffectLine> ParseEffect(string effect, string regionName)
        {
            var res = new List<EffectLine>();
            if (string.IsNullOrEmpty(effect) || effect == "hiçbir şey olmaz") return res;
            string scope = null;
            foreach (var group in effect.Split(new[] { " · " }, System.StringSplitOptions.RemoveEmptyEntries))
            foreach (var raw in group.Split(new[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var part = raw.Trim();
                Match m;
                if ((m = SignedRx.Match(part)).Success)
                    res.Add(new EffectLine(m.Groups[3].Value, Sign(m.Groups[1].Value) * float.Parse(m.Groups[2].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture), null));
                else if ((m = ScopedRx.Match(part)).Success)
                {
                    if (m.Groups[1].Success && m.Groups[1].Value.Length > 0) scope = m.Groups[1].Value.Trim();
                    bool all = scope == "bütün bölgelerde";
                    res.Add(new EffectLine(m.Groups[2].Value, Sign(m.Groups[3].Value) * int.Parse(m.Groups[4].Value), all ? null : regionName) { Global = all });
                }
                else if ((m = ResearchRx.Match(part)).Success)
                    res.Add(new EffectLine("araştırma", (m.Groups[3].Success && m.Groups[3].Value.Length > 0 ? -1 : Sign(m.Groups[1].Value)) * int.Parse(m.Groups[2].Value), null) { Unit = " " + Loc.T("gün") });
                else if ((m = AiRx.Match(part)).Success)
                    res.Add(new EffectLine("YZ", int.Parse(m.Groups[1].Value), null));
                else res.Add(new EffectLine { Text = Loc.T(part) });
            }
            return res;
        }
    }
}
