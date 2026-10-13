using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // Harita (F8): düğümler oyunun enlem/boylamından tasarım projeksiyonuyla, yollar oyunun bağlantılarından tasarımın
    // yapım kuralıyla (map.json routes[].build), analist katmanı gösterilen bilgiden türetilir. Harita sprite'ı aynen kalır.
    public sealed partial class SimDeskBuilder
    {
        /// <summary>Düğüm merkezi (tasarım pikseli, sol-üst orijin).</summary>
        public static Vector2 Center(string id) { var d = Def(id); return DeskMath.Project(d.Lon, d.Lat); }

        static Vector2 PinOf(string id) => Center(id) + new Vector2(0, -14);
        static float[] A(Vector2 v) => new[] { Mathf.Round(v.x * 10) / 10, Mathf.Round(v.y * 10) / 10 };
        static string Code(string id) => "R-" + (System.Array.FindIndex(GameData.Regions, r => r.Id == id) + 1).ToString("00");

        /// <summary>Güven: verified/field/truth → onaylı; rapor (+ işaretli medya → şüpheli); bilinmiyor.</summary>
        string TrustOf(ShownValue v) => v.Src switch
        {
            "verified" or "field" or "truth" or "off" => "verified",
            "report" => v.Flag ? "suspicious" : "report",
            _ => "unknown",
        };

        MapVM Map()
        {
            var shown = S.Regions.ToDictionary(r => r.Id, r => sim.Shown(r));
            int mon = S.Regions.Count(r => r.Monitored);
            int ver = shown.Values.Count(v => TrustOf(v) == "verified");
            int sus = shown.Values.Count(v => TrustOf(v) == "suspicious");
            int unk = shown.Values.Count(v => TrustOf(v) == "unknown");
            var spec = input.MapSpec;
            var map = new MapVM
            {
                Header = new MapHeaderVM
                {
                    Title = Loc.T("KYGA-07 · HAREKÂT HARİTASI · DÜNYA"),
                    Summary = Loc.F("İzleme kapsamı {0} · Onaylanan {1} · Şüpheli {2} · Bilinmiyor {3}",
                        Num($"{mon}/12"), Num($"{ver}/12"), Num(sus.ToString()), Num(unk.ToString())),
                    Right = Day(S.Day) + " · " + TimeText,
                },
                AnalystLayer = true,
                SelectedRegion = st.Selected,
            };

            int suspiciousSeen = 0;
            foreach (var def in GameData.Regions)
            {
                var r = S.Region(def.Id);
                var v = shown[def.Id];
                var c = Center(def.Id);
                string trust = TrustOf(v);
                var node = new RegionNodeVM
                {
                    Id = def.Id, Code = Code(def.Id), Name = def.Name, Label = Up(def.Name),
                    Center = A(c), Pin = A(PinOf(def.Id)), LabelW = 160,
                    Value = trust == "unknown" || v.V == null ? (int?)null : Mathf.RoundToInt((float)v.V.Value),
                    Trust = trust,
                    Selected = st.Selected == def.Id,
                    Quarantine = r.Q, QuarantineClean = illusion,
                    Core = revealed ? S.Core == def.Id : S.CoreKnown == def.Id,
                };
                if (trust == "suspicious") { node.QuestionMark = true; node.RingVariant = suspiciousSeen++ % 2 == 0 ? "a" : "b"; }
                // Etiket: tasarımdaki dört yerleşim (alt, üst, sol, sağ) düğüm merkezine göre; masa haritası için seçilmiş
                switch (LabelSide(def))
                {
                    case 't': node.LabelX = c.x - 80; node.LabelY = c.y - 40; node.LabelAlign = "center"; break;
                    case 'l': node.LabelX = c.x - 182; node.LabelY = c.y - 7; node.LabelAlign = "right"; break;
                    case 'r': node.LabelX = c.x + 20; node.LabelY = c.y + 6; node.LabelAlign = "left"; break;
                    default: node.LabelX = c.x - 80; node.LabelY = c.y + 20; node.LabelAlign = "center"; break;
                }
                map.Regions.Add(node);
            }

            foreach (var l in GameData.Links)
            {
                if (l.Wrap) { EdgeRoutes(map, l); continue; }
                map.Routes.Add(Route(l));
            }

            if (spec?["graticuleLabels"] is JArray grat)
            {
                bool en = DeskLoc.Instance.Lang == DeskLang.En;
                foreach (var gl in grat)
                {
                    string t = (string)gl["text"];
                    if (en) t = t.Replace("K", "N").Replace("G", "S").Replace("B", "W").Replace("D", "E");
                    map.GraticuleLabels.Add(new MapTextVM { Text = t, X = (float)gl["x"], Y = (float)gl["y"] });
                }
            }

            if (sim.TeamsOn) Teams(map);
            map.Analyst = Analyst(map, shown);
            if (illusion) map.IllusionTraces = Traces(map.Analyst);
            if (st.Selected != null) map.Selection = new SelectionVM { From = A(PinOf(st.Selected)), To = new[] { 1544f, 218f } };
            return map;
        }

        // ------------------------------------------------------------------ yollar
        static Vector2 Q(Vector2 a, Vector2 c, Vector2 b, float t) => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b;
        static Vector2 QTangent(Vector2 a, Vector2 c, Vector2 b, float t) => 2 * (1 - t) * (c - a) + 2 * t * (b - c);

        /// <summary>
        /// Tasarımın yol kuralı: düğüm merkezleri arasında karesel Bézier, kontrol orta noktadan yukarı doğru normal × uzunluk × 0,10;
        /// uçlar merkezden 21 px kırpılır (alt eğri de Casteljau ile). map.json'daki yolları ±1 px verir.
        /// </summary>
        public static (Vector2 p0, Vector2 c, Vector2 p1) RouteCurve(Vector2 a, Vector2 b, float bend = 0.10f, float trim = 21)
        {
            var d = b - a; float len = d.magnitude;
            var n = new Vector2(d.y, -d.x) / len;
            if (n.y > 0) n = -n;
            var c = (a + b) / 2 + n * len * bend;
            float Solve(System.Func<float, float> f)
            {
                float lo = 0, hi = 0.5f;
                for (int i = 0; i < 40; i++) { float m = (lo + hi) / 2; if (f(m) < trim) lo = m; else hi = m; }
                return (lo + hi) / 2;
            }
            float t0 = Solve(t => (Q(a, c, b, t) - a).magnitude);
            float t1 = 1 - Solve(t => (Q(a, c, b, 1 - t) - b).magnitude);
            var q = (1 - t0) * (1 - t1) * a + ((1 - t0) * t1 + t0 * (1 - t1)) * c + t0 * t1 * b;
            return (Q(a, c, b, t0), q, Q(a, c, b, t1));
        }

        RouteVM Route(LinkDef l)
        {
            var (p0, c, p1) = RouteCurve(Center(l.A), Center(l.B));
            var route = new RouteVM { Id = l.A + "-" + l.B, From = l.A, To = l.B, Q = { A(p0), A(c), A(p1) }, State = "open" };
            // Karantinadaki uç: yol orada kesik (map.json cutMark: düğüm merkezinden ~29 px, yola dik)
            var ra = S.Region(l.A); var rb = S.Region(l.B);
            if (ra.Q || rb.Q)
            {
                route.State = "cut";
                var qn = ra.Q ? Center(l.A) : Center(l.B);
                float best = 0, bestErr = float.MaxValue;
                for (int i = 0; i <= 100; i++)
                {
                    float t = i / 100f, err = Mathf.Abs((Q(p0, c, p1, t) - qn).magnitude - 29);
                    if (err < bestErr) { bestErr = err; best = t; }
                }
                route.CutCenter = A(Q(p0, c, p1, best));
                route.CutTangentDeg = Mathf.Round(Deg(QTangent(p0, c, p1, best)) * 10) / 10;
            }
            return route;
        }

        /// <summary>Pasifik bağlantısı haritanın kenarından dolaşır: batı ucundan sola, doğu ucundan sağa ok ve kenar etiketi.</summary>
        void EdgeRoutes(MapVM map, LinkDef l)
        {
            var a = Center(l.A); var b = Center(l.B);
            var (west, east) = a.x < b.x ? (l.A, l.B) : (l.B, l.A);
            var w = Center(west); var e = Center(east);
            // Tasarımdaki iki kenar yolunun düğüme göre biçimi (map.json edgeRoutes)
            map.EdgeRoutes.Add(new EdgeRouteVM
            {
                Q = { A(w + new Vector2(-21.5f, -4)), A(w + new Vector2(-62.5f, -13)), new[] { 421f, w.y - 17 } },
                ArrowPos = new[] { 418f, w.y - 17.4f }, ArrowRotDeg = 186,
            });
            map.EdgeRoutes.Add(new EdgeRouteVM
            {
                Q = { A(e + new Vector2(21, 3.8f)), new[] { (e.x + 1499) / 2 + 20, e.y + 20.8f }, new[] { 1499f, e.y + 30.8f } },
                ArrowPos = new[] { 1502f, e.y + 31.2f }, ArrowRotDeg = 6,
            });
            map.EdgeLabels.Add(new MapTextVM { Text = "← " + Up(Short(east)), X = 422, Y = w.y - 39 });
            map.EdgeLabels.Add(new MapTextVM { Text = Up(Short(west)) + " →", X = 1412, Y = e.y + 37.8f });
        }

        /// <summary>
        /// Masa haritasındaki etiket yönü: oyunun LabelPos'u eski harita için seçildi; bu haritada yollarla çakışmasın diye
        /// tasarımın yerleşimine yakın bir tablo (Batı Avrupa altta, Doğu Avrupa üstte, Çin üstte, Japonya-Kore altta).
        /// </summary>
        static char LabelSide(RegionDef d) => d.Id switch
        {
            "weu" => 'b', "eeu" => 't', "ru" => 'r', "cn" => 't', "jk" => 'b', "in" => 'b', "sea" => 'b', "me" => 'l',
            _ => d.LabelPos,
        };

        static string Short(string id) => id switch
        {
            "na" => Loc.T("K. Amerika"),
            "jk" => Loc.T("Japonya-Kore"),
            _ => Def(id).Name,
        };

        // ------------------------------------------------------------------ saha ekipleri
        void Teams(MapVM map)
        {
            var atRegion = new Dictionary<string, int>();
            foreach (var t in S.Teams)
            {
                var tok = new TeamTokenVM { Id = TeamLetter(t), State = t.Dest != null ? "yolda" : t.At };
                Vector2 pos;
                if (t.Dest != null)
                {
                    // Yolda: çıkıştan varışa düz çizgide ilerleme; varış etiketi ve kesik kalem oku
                    float k = t.Trip > 0 ? Mathf.Clamp01((t.Trip - t.Eta) / (float)t.Trip) : 0;
                    var from = Center(t.From ?? t.Dest); var to = Center(t.Dest);
                    pos = Vector2.Lerp(from, to, Mathf.Lerp(0.2f, 0.8f, k)) - new Vector2(11, 11);
                    tok.Tag = Loc.F("→ {0} · {1} gün", Def(t.Dest).Name, t.Eta);
                    tok.TagX = pos.x - 39; tok.TagY = pos.y + 27;
                }
                else
                {
                    // Bölgedeki ekipler düğümün sol-üstünde yan yana (komşu düğümle çakışmasın: sola doğru)
                    atRegion.TryGetValue(t.At, out int n);
                    atRegion[t.At] = n + 1;
                    pos = Center(t.At) + new Vector2(-40.5f - 24 * n, -32);
                }
                tok.X = Mathf.Round(pos.x); tok.Y = Mathf.Round(pos.y);
                map.Teams.Add(tok);
            }
        }

        // ------------------------------------------------------------------ analist katmanı (dekor; tek bilgi kaynağı değil)
        AnalystLayerVM Analyst(MapVM map, Dictionary<string, ShownValue> shown)
        {
            var a = new AnalystLayerVM();
            foreach (var n in map.Regions) if (n.Trust != "unknown") a.Pins.Add(n.Pin);

            // Yayılım ipliği: gösterilen değeri yüksek bölgeden komşularına (bilinen değerlerle)
            foreach (var l in GameData.Links)
            {
                if (l.Wrap) continue;
                var va = shown[l.A].V; var vb = shown[l.B].V;
                if (va == null || vb == null) continue;
                string hot = null, other = null;
                if (va >= 50 && vb >= 20) { hot = l.A; other = l.B; }
                else if (vb >= 50 && va >= 20) { hot = l.B; other = l.A; }
                if (hot == null) continue;
                var p = PinOf(hot); var q = PinOf(other);
                a.Strings.Add(new AnalystStringVM { A = A(p), B = A(q), FlagPos = A(Vector2.Lerp(p, q, 0.68f)), FlagRotDeg = Mathf.Round(Deg(q - p) * 10) / 10 });
            }

            // Yoldaki ekiplerin kalem oku
            foreach (var t in map.Teams.Where(x => x.Tag != null))
            {
                var team = S.Teams.FirstOrDefault(x => TeamLetter(x) == t.Id && x.Dest != null);
                if (team == null) continue;
                var s = new Vector2(t.X + 22, t.Y + 4); var e = Center(team.Dest) + new Vector2(-27, -10);
                var c = (s + e) / 2 + new Vector2(0, -12);
                a.PencilArrows.Add(new PencilArrowVM
                {
                    Q = { A(s), A(c), A(e) }, Color = "#4A4640", Width = 1.5f, Dash = new[] { 5f, 4f },
                    HeadPos = A(e + (e - c).normalized * 3), HeadRotDeg = Mathf.Round(Deg(e - c)),
                });
            }

            // Çekirdeğin yeri biliniyorsa kırmızı not ve ok
            string core = revealed ? S.Core : S.CoreKnown;
            if (core != null)
            {
                var c0 = Center(core);
                a.Notes.Add(new AnalystNoteVM { Text = Loc.T("çekirdek!"), Color = "#9A1E14", X = c0.x - 79.5f, Y = c0.y + 23.4f, RotDeg = -3, Weight = 700 });
                a.PencilArrows.Add(new PencilArrowVM
                {
                    Q = { A(c0 + new Vector2(-19.5f, 31.4f)), A(c0 + new Vector2(-7.5f, 25.4f)), A(c0 + new Vector2(-4.5f, 17.4f)) },
                    Color = "#9A1E14", Width = 1.6f, HeadPos = A(c0 + new Vector2(-3.5f, 15.4f)), HeadRotDeg = -70,
                });
            }

            // Karantina notu: kararın günü (kayıttaki son karantina komutu)
            foreach (var r in S.Regions.Where(x => x.Q))
            {
                var cmd = sim.Record?.commands.LastOrDefault(x => x.kind == "act" && x.a == Actions.Quarantine && x.b == r.Id);
                var c0 = Center(r.Id);
                a.Notes.Add(new AnalystNoteVM
                {
                    Text = cmd != null ? Loc.F("karantina · {0}", Day(cmd.day)) : Loc.T("karantina"),
                    Color = "#24592E", X = c0.x + 37, Y = c0.y - 30, RotDeg = -3, Weight = 400,
                });
            }

            // Şüpheli düğümlerin yanına kalem soru işareti
            foreach (var n in map.Regions.Where(x => x.Trust == "suspicious"))
                a.QuestionMarks.Add(new[] { n.Center[0] + 24.5f, n.Center[1] - 42.9f });

            // Kör noktalar: izlenmeyen, değeri bilinmeyen ya da şüpheli bölgeler
            var blind = GameData.Regions.Where(d => !S.Region(d.Id).Monitored && (TrustOf(shown[d.Id]) is "unknown" or "suspicious"))
                .Take(3).Select(d => d.Name).ToList();
            if (blind.Count > 0 && S.Ending == null)
                a.Sticky = new StickyVM
                {
                    Text = Loc.T("<b>Kör noktalar:</b>") + "\n" + string.Join(",\n", blind) + "\n" + Loc.T("→ izleme ağı kur!"),
                    X = 428, Y = 760, W = 140, RotDeg = -2, TapeX = 46, TapeY = -7, TapeRotDeg = -4,
                };
            return a;
        }

        /// <summary>İllüzyon: analist notları silinmiş; yerlerinde silik iz ve silgi lekesi kalır.</summary>
        static IllusionTracesVM Traces(AnalystLayerVM a)
        {
            var t = new IllusionTracesVM();
            foreach (var n in a.Notes) t.GhostNotes.Add(new GhostNoteVM { Text = n.Text, X = n.X, Y = n.Y, RotDeg = n.RotDeg, Alpha = 0.09f, Weight = n.Weight });
            t.EraserSmudge = new EraserVM { Center = new[] { 1290f, 545f }, Radius = new[] { 95f, 30f }, Color = "#FFFFFF", Alpha = 0.025f };
            return t;
        }
    }
}
