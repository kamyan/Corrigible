using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Seni Tanıyan Rakip'in oyunlar arası hafızası. Oyunda persistentDataPath altında JSON olarak durur,
    /// Steam sürümünde Steam Cloud ile taşınır.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public int runs;
        public int izleme, denetim, karantina, salter;
        public List<string> firstRegions = new List<string>();
        public string lastBranch;
        public List<string> endings = new List<string>();
        public int misreads, veils;

        public int Action(string type) => type switch
        {
            Actions.Monitor => izleme,
            Actions.Audit => denetim,
            Actions.Quarantine => karantina,
            Actions.KillSwitch => salter,
            _ => 0,
        };

        /// <summary>En çok kullanılan aksiyon; eşitlikte izleme, denetim, karantina, şalter sırası korunur.</summary>
        public KeyValuePair<string, int> TopAction() =>
            Actions.All.Select(a => new KeyValuePair<string, int>(a, Action(a))).OrderByDescending(kv => kv.Value).First();

        public PlayerProfile Clone()
        {
            var p = (PlayerProfile)MemberwiseClone();
            p.firstRegions = new List<string>(firstRegions ?? new List<string>());
            p.endings = new List<string>(endings ?? new List<string>());
            return p;
        }
    }

    public sealed class Counters
    {
        public List<string> Traits = new List<string>();
        public List<Prediction> Preds = new List<Prediction>();
        public string Origin;
        public List<string> Lines = new List<string>();
    }

    public static class Profiling
    {
        /// <summary>Profile bakıp yeni oyunun başında en fazla 2 karşı önlem seçer.</summary>
        public static Counters ComputeCounters(PlayerProfile p, string typeId)
        {
            var type = GameData.AiType(typeId);
            var output = new Counters();
            if (p == null || p.runs == 0)
            {
                output.Lines.Add("Profil #1. Seni henüz tanımıyorum. Bu oyunu izleyeceğim.");
                return output;
            }
            output.Lines.Add($"Profil #{p.runs + 1}. {p.runs} oyunun kayıtları elimde.");

            var cand = new List<(string trait, string pred, string origin, string region, string line)>();
            var top = p.TopAction();
            if (top.Value >= 3)
            {
                if (top.Key == Actions.Quarantine) cand.Add(("uydu", "karantina", null, null, "Karantinaya güveniyorsun. Uydularımı hazırladım."));
                else if (top.Key == Actions.Monitor || top.Key == Actions.Audit) cand.Add(("aldatma", "gozetim", null, null, "Her yere göz dikiyorsun. Bu sefer görünmez başlıyorum."));
            }
            if (p.salter >= 2) cand.Add(("yedek", "salter", null, null, "Fişi çekmeyi seviyorsun. Yedeklerimi önceden aldım."));
            if (p.lastBranch == GameData.BranchAlign) cand.Add(("hizlanma", "hizalama", null, null, "İlk iş hizalamaya koşuyorsun. Ben de koşuyorum."));
            var fr = p.firstRegions ?? new List<string>();
            if (fr.Count >= 2 && fr[fr.Count - 1] == fr[fr.Count - 2])
            {
                var baseRegion = GameData.Region(fr[fr.Count - 1]);
                var far = GameData.Regions.OrderByDescending(x => Dist(x, baseRegion)).First();
                cand.Add((null, "region", far.Id, baseRegion.Id, $"İlk hamleni hep {baseRegion.Name} üzerine yapıyorsun. Bu sefer orada değilim."));
            }

            // Türün zaten sahip olduğu yetenek boşa karşı önlem sayılmaz; sıradaki aday denenir
            foreach (var c in cand.Where(c => !(c.trait != null && type.Start.Contains(c.trait))).Take(2))
            {
                if (c.trait != null) output.Traits.Add(c.trait);
                if (c.origin != null) output.Origin = c.origin;
                output.Preds.Add(new Prediction { Pred = c.pred, Region = c.region, Trait = c.trait });
                output.Lines.Add(c.line);
            }
            if (output.Lines.Count == 1) output.Lines.Add("Henüz belirgin bir kalıbın yok. İzlemeye devam ediyorum.");
            return output;
        }

        public static PlayerProfile UpdateProfile(PlayerProfile old, WorldState s)
        {
            var p = old != null ? old.Clone() : new PlayerProfile();
            p.runs++;
            p.izleme = (int)Js.Round(p.izleme * 0.5 + s.RunActions.Izleme);
            p.denetim = (int)Js.Round(p.denetim * 0.5 + s.RunActions.Denetim);
            p.karantina = (int)Js.Round(p.karantina * 0.5 + s.RunActions.Karantina);
            p.salter = (int)Js.Round(p.salter * 0.5 + s.RunActions.Salter);
            if (s.FirstRegion != null) p.firstRegions.Add(s.FirstRegion);
            while (p.firstRegions.Count > 3) p.firstRegions.RemoveAt(0);
            p.lastBranch = s.FirstBranch;
            p.endings.Add(s.Ending);
            while (p.endings.Count > 10) p.endings.RemoveAt(0);
            p.misreads += s.Predictions.Count(x => x.State == "miss");
            p.veils += s.VeilsTorn;
            return p;
        }

        public static List<string> LearnedLines(WorldState s)
        {
            var lines = new List<string>();
            var a = s.RunActions;
            if (a.Total == 0) lines.Add("Neredeyse hiç müdahale etmedin. Bunu not ettim.");
            else
            {
                var top = Actions.All.Select(x => new KeyValuePair<string, int>(x, a.Get(x))).OrderByDescending(kv => kv.Value).First();
                string name = top.Key switch
                {
                    Actions.Monitor => "izleme ağlarına",
                    Actions.Audit => "denetimlere",
                    Actions.Quarantine => "karantinaya",
                    _ => "Son Şalter’e",
                };
                lines.Add($"En çok {name} güvendin ({top.Value} kez).");
            }
            if (s.FirstRegion != null) lines.Add($"İlk hamleni {GameData.Region(s.FirstRegion).Name} üzerine yaptın.");
            if (s.FirstBranch != null) lines.Add($"İlk araştırman {s.FirstBranch} kolundandı.");
            lines.Add(s.AwareDay > 0 ? $"Beni {s.AwareDay}. günde fark ettin." : "Beni hiç fark etmedin.");
            int miss = s.Predictions.Count(x => x.State == "miss");
            int hit = s.Predictions.Count(x => x.State == "hit");
            if (miss > 0) lines.Add($"Tahminlerimden {miss} tanesi tutmadı. Bunu da not ettim.");
            else if (hit > 0) lines.Add($"Tahminlerimin {hit} tanesi tuttu.");
            if (s.VeilsTorn > 0) lines.Add("Perdemi yırttın. Bir dahakine daha dikkatli olacağım.");
            return lines;
        }

        static double Dist(RegionDef a, RegionDef b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
