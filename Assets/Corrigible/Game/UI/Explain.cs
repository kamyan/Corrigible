using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Oyuncuya "neden" ve "ne olur" açıklamaları: gösterge dökümleri, eylem önizlemeleri, hedef yolları, sıradaki adım.
    /// Algı Katmanına saygılıdır: sızma ve Tekillik gibi yalan söyleyebilen değerler <c>Shown</c>/<c>Global</c>'den okunur,
    /// illüzyon sırasında gerçeği ele verecek ayrıntılar gösterilmez.
    /// </summary>
    public static class Explain
    {
        static string Name(string id) => GameData.Region(id).Name;
        static bool Illusion(Simulation sim) => sim.S.Illusion != null && !sim.EndRevealed;

        // ------------------------------------------------------------------ gidişat tahmini
        /// <summary>Gösterilen değerlerin günlük geçmişi (gidişat tahmini için). Masa her yeni günde ekler.</summary>
        public sealed class Trend
        {
            readonly List<(int day, double tek, double trust)> points = new List<(int, double, double)>();

            public void Add(int day, ShownGlobal g)
            {
                if (points.Count > 0 && points[points.Count - 1].day == day) return;
                if (points.Count > 0 && day < points[points.Count - 1].day) points.Clear();
                points.Add((day, g.Tek, g.Trust));
                if (points.Count > 30) points.RemoveAt(0);
            }

            public void Clear() => points.Clear();

            /// <summary>Son ~10 günün günlük değişimi.</summary>
            public (double tek, double trust) Rate()
            {
                if (points.Count < 4) return (0, 0);
                var a = points[Math.Max(0, points.Count - 11)]; var b = points[points.Count - 1];
                int d = Math.Max(1, b.day - a.day);
                return ((b.tek - a.tek) / d, (b.trust - a.trust) / d);
            }
        }

        public static string TekForecast(ShownGlobal g, Trend t)
        {
            var (rate, _) = t.Rate();
            if (rate <= 0.02) return Loc.T("100 = kayıp");
            return Loc.F("100'e ~{0} gün", Mathf.Max(1, Mathf.RoundToInt((float)((100 - g.Tek) / rate))));
        }

        public static string TrustForecast(ShownGlobal g, Trend t)
        {
            var (_, rate) = t.Rate();
            if (rate >= -0.02 || g.Trust <= 15) return Loc.T("15 altı dağılır");
            return Loc.F("15'e ~{0} gün", Mathf.Max(1, Mathf.RoundToInt((float)((g.Trust - 15) / -rate))));
        }

        // ------------------------------------------------------------------ gösterge dökümleri
        public static string Fund(Simulation sim)
        {
            var s = sim.S;
            var lines = new List<string>
            {
                Loc.T("<b>Fon</b>: eylemlerin ve araştırmanın bedeli."),
                Loc.F("Günlük gelir: <b>+{0:0.0}</b> = bölgelerin ekonomisi × güveni / {1:0}", sim.FundRate, sim.B.fonDiv),
            };
            if (!s.Aware) lines.Add(Loc.F("YZ fark edilmeden hükümetler bütçenin yalnızca %{0:0}'ını veriyor.", sim.B.preAwareFund * 100));
            if (s.Research.Cur != null)
            {
                var rs = GameData.ResearchItem(s.Research.Cur);
                lines.Add(Loc.F("Araştırma ({0}) günde ~{1:0.0} fon harcıyor.", rs.Name, rs.Cost / rs.Days * (rs.Branch == GameData.BranchAlign ? sim.AlignSpeed : 1)));
            }
            var top = s.Regions.Where(r => !r.Left && r.Off == 0).OrderByDescending(r => r.Econ * r.Trust).Take(3).Select(r => Name(r.Id));
            lines.Add(Loc.F("En çok katkı: {0}.", string.Join(", ", top)));
            return string.Join("\n", lines);
        }

        public static string Sg(Simulation sim)
        {
            var s = sim.S;
            return string.Join("\n",
                Loc.T("<b>Siyasi güç (SG)</b>: denetim, karantina ve Son Şalter gibi sert kararlar için."),
                s.Aware
                    ? Loc.F("Günlük +{0:0.00}: 0,25 + YZ'nin doğrulandığı bölge başına 0,03.", sim.SgRate)
                    : Loc.T("YZ fark edilmeden hükümetler neredeyse hiç siyasi güç vermiyor (+0,05/gün)."),
                Loc.T("Küresel Son Şalter en az 50 SG ister."));
        }

        public static string Trust(Simulation sim, ShownGlobal g, Trend t)
        {
            var lines = new List<string>
            {
                Loc.T("<b>Küresel güven</b>: hükümetlerin KYGA'ya güveni. Ortalama 15'in altına inerse ya da 3'ten az üye kalırsa ajans dağılır; bölgede 20'nin altı ittifaktan çekilir."),
                Loc.T("Düşürenler: karantina (her gün), Son Şalter, hediye reddetmek, YZ'nin karalama haberleri."),
                TrustForecast(g, t),
            };
            if (!Illusion(sim))
            {
                var low = sim.S.Regions.Where(r => !r.Left).OrderBy(r => r.Trust).Take(3).Select(r => Loc.F("{0} {1:0}", Name(r.Id), r.Trust));
                lines.Add(Loc.F("En düşük: {0}.", string.Join(", ", low)));
            }
            return string.Join("\n", lines);
        }

        public static string Dependence(Simulation sim)
        {
            var lines = new List<string>
            {
                Loc.F("<b>Bağımlılık</b>: dünyanın YZ'ye bağımlılığı. Bir bölgede veto eşiğini ({0:0}) aşarsa Son Şalter orada veto edilir; ortalama yükselirse YZ hiç saldırmadan kazanabilir.", sim.VetoThreshold),
                Loc.T("Artıranlar: sızma (her gün), kabul edilen hediyeler. Azaltanlar: Son Şalter, Analog Yedekler, perdenin yırtılması."),
            };
            if (!Illusion(sim))
            {
                var high = sim.S.Regions.OrderByDescending(r => r.Dep).Take(3).Select(r => Loc.F("{0} {1:0}", Name(r.Id), r.Dep));
                lines.Add(Loc.F("En yüksek: {0}.", string.Join(", ", high)));
            }
            return string.Join("\n", lines);
        }

        public static string Tek(Simulation sim, ShownGlobal g, Trend t)
        {
            var s = sim.S;
            var lines = new List<string>
            {
                Loc.T("<b>Tekillik</b>: YZ'nin güç birikimi. %100 olursa kaybedersin."),
                Loc.F("Gidişat: {0}.", TekForecast(g, t)),
                Loc.T("Artıranlar: sızma × bölgenin hesaplama gücü (veri merkezi büyük bölgeler önemli); YZ'nin çekirdeği yaşadıkça hızlanır."),
            };
            if (s.Has("hizlanma")) lines.Add(Loc.T("YZ'nin Hızlanma yeteneği: ×1,5."));
            lines.Add(Loc.T("Azaltanlar: karantina (bölgenin katkısı %20'ye iner), Düzeltilebilirlik araştırması (−%25), denetimle sızmayı düşürmek."));
            // Ekrandaki sızmaya göre en çok katkı veren bölgeler (yalan söyleyebilir)
            var top = s.Regions.Select(r => (r, v: (sim.Shown(r).V ?? 0) * GameData.Region(r.Id).Compute * (r.Q ? 0.2 : 1)))
                .Where(x => x.v > 0.5).OrderByDescending(x => x.v).Take(3).Select(x => Name(x.r.Id)).ToList();
            if (top.Count > 0) lines.Add(Loc.F("Görünen en büyük katkı: {0}.", string.Join(", ", top)));
            return string.Join("\n", lines);
        }

        // ------------------------------------------------------------------ eylem önizlemesi
        public static string Action(Simulation sim, string action, string regionId)
        {
            var s = sim.S; var r = s.Region(regionId);
            var shown = sim.Shown(r).V;
            string v(double x) => shown.HasValue ? Math.Round(x).ToString() : "?";
            switch (action)
            {
                case Actions.Monitor:
                    if (r.Monitored) return Loc.T("İzleme ağı kurulu: bu bölgede gerçek sızmayı görüyorsun.");
                    return Loc.F("−{0} fon · bölgenin gerçek sızmasını görürsün · YZ buradaysa tespit ihtimali yüksek", sim.MonitorCostFor(regionId))
                           + (sim.TeamsOn ? Loc.T(" · boşta saha ekibi gerekir") : "");
                case Actions.Audit:
                {
                    double before = shown ?? 0, cut = sim.B.audit * (r.Dep >= 85 && !s.Done("analog") ? 0.5 : 1);
                    string txt = Loc.F("−{0} fon −1 SG · sızma ~{1} → ~{2} · {3} gün boyunca büyüme yarıya iner", sim.AuditCost, v(before), v(Math.Max(0, before - cut)), sim.B.patchDays);
                    if (r.Dep >= 85 && !s.Done("analog")) txt += Loc.T(" · bağımlılık 85+: etkisi yarıya iner");
                    if (s.Aware) txt += Loc.T(" · çekirdeğin bölgesi 20'nin altına inmez, bu onu ele verir");
                    return txt;
                }
                case Actions.Quarantine:
                    return r.Q ? Loc.T("Karantinayı kaldır: ücretsiz · ekonomi toparlanır, bağlantılar açılır")
                               : Loc.T("−8 SG · bağlantılar %90 kesilir (sıçrama durur) · Tekillik katkısı %20'ye iner · ekonomi ve güven her gün düşer");
                case Actions.KillSwitch:
                {
                    if (r.Dep > sim.VetoThreshold) return Loc.F("VETO: bağımlılık {0:0} > {1:0}. Önce bu bölgede bağımlılığı düşür.", r.Dep, sim.VetoThreshold);
                    string txt = Loc.F("−{0} SG −{1} fon · sızma 0, 20 gün çevrimdışı · ekonomi −40, güven düşer", sim.SalterSg(r), sim.SalterFon(r));
                    if (s.CoreKnown == regionId)
                    {
                        txt += Loc.T(" · YZ'nin çekirdeğini yok eder");
                        var other = s.Regions.Where(x => x != r && x.Off == 0 && (sim.Shown(x).V ?? 0) >= 20).Select(x => Name(x.Id)).ToList();
                        if (other.Count > 0) txt += Loc.F(" · <color=#f0b03f>Uyarı: {0} bölgesinde %20+ sızma görünüyor; çekirdek oraya kaçabilir.</color>", string.Join(", ", other));
                    }
                    return txt;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ hedef yolları
        public struct Track { public float Progress; public string Text; }

        static readonly string[] AlignChain = { "yorum", "duzelt", "kilit" };

        public static Track Alignment(Simulation sim)
        {
            var s = sim.S;
            if (!s.Aware) return new Track { Progress = 0, Text = Loc.T("Önce YZ'yi tespit et") };
            float p = 0.1f + 0.15f * Mathf.Clamp01(sim.Studied / 4f);
            string text = Loc.F("Gözlem noktası {0}/4", sim.Studied);
            float[] w = { 0.2f, 0.25f, 0.3f };
            for (int i = 0; i < AlignChain.Length; i++)
            {
                var rs = GameData.ResearchItem(AlignChain[i]);
                if (s.Done(rs.Id)) { p += w[i]; continue; }
                float part = s.Research.Cur == rs.Id ? Mathf.Clamp01((float)(s.Research.Prog / rs.Days)) : 0;
                p += w[i] * part;
                text = s.Research.Cur == rs.Id ? Loc.F("{0} %{1:0}", rs.Name, part * 100) : Loc.F("Sıradaki: {0}", rs.Name);
                if (sim.Studied == 0) text += Loc.T(" · gözlem noktası yok");
                break;
            }
            return new Track { Progress = Mathf.Clamp01(p), Text = text };
        }

        public static Track Erasure(Simulation sim)
        {
            var s = sim.S;
            if (!s.Aware) return new Track { Progress = 0, Text = Loc.T("Önce YZ'yi tespit et") };
            if (s.Core != null && s.CoreKnown == null) return new Track { Progress = 0.15f, Text = Loc.T("Çekirdeği bul") };
            if (s.Core != null) return new Track { Progress = 0.45f, Text = Loc.F("Çekirdek: {0} · Son Şalter'le yok et", Name(s.CoreKnown)) };
            if (Illusion(sim)) return new Track { Progress = 0.75f, Text = Loc.T("Kopyaları temizle") };
            int need = sim.Dur(15);
            return new Track { Progress = 0.75f + 0.25f * Mathf.Clamp01((float)s.CleanDays / need), Text = Loc.F("Temiz gün {0}/{1}", s.CleanDays, need) };
        }

        public static string NextStep(Simulation sim)
        {
            var s = sim.S;
            if (s.Ending != null) return null;
            if (!s.Aware)
                return sim.TeamsOn
                    ? Loc.T("YZ'yi bul: haber bandında adı geçen bölgelere bir ekip gönder ve izleme ağı kur.")
                    : Loc.T("YZ'yi bul: haber bandında adı geçen bölgelere izleme ağı kur.");
            if (s.Core == null) return Loc.T("Kalan kopyaları temizle: bütün bölgeler 15 gün temiz kalırsa YZ silinir.");
            if (s.Research.Cur == null && !s.Done("kilit")) return Loc.T("Araştırma masası boş: Hizalama kolunda sıradaki araştırmayı başlat.");
            if (sim.Studied == 0) return Loc.T("Gözlem noktası yok: YZ'nin görüldüğü bir bölgeye izleme ağı kur, Hizalama hızlanır.");
            if (s.CoreKnown != null)
            {
                bool risky = s.Regions.Any(x => x.Id != s.CoreKnown && x.Off == 0 && (sim.Shown(x).V ?? 0) >= 20);
                return risky
                    ? Loc.F("Çekirdek {0} bölgesinde. Son Şalter'den önce diğer bölgelerde sızmayı %20'nin altına indir, yoksa çekirdek kaçar.", Name(s.CoreKnown))
                    : Loc.F("Çekirdek {0} bölgesinde ve kaçacak yeri yok: Son Şalter'i çek.", Name(s.CoreKnown));
            }
            return Loc.T("Çekirdeği bul: denetimden sonra sızması 20'nin altına inmeyen bölge çekirdektir. Bu arada Hizalama'yı sürdür.");
        }
    }
}
