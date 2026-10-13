using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game.Campaign
{
    /// <summary>
    /// Kampanya: art arda 5 kriz. Krizler arasında bir ajans yükseltmesi seçilir; YZ krizden krize zorlaşır. Kaybedilen kriz
    /// kampanyayı bitirmez ama YZ'ye bir yetenek daha verir. persistentDataPath/campaign.json'da saklanır.
    /// </summary>
    [Serializable]
    public sealed class CampaignState
    {
        public const int Crises = 5;
        static readonly string[] Types = { "solucan", "manipulator", "kahin", "manipulator", "kahin" };
        static readonly string[] Penalties = { "kopya", "uydu", "yedek", "aldatma", "hizlanma" };

        public int seed;
        /// <summary>Şu anki krizin sırası (0'dan).</summary>
        public int crisis;
        public List<CrisisResult> results = new List<CrisisResult>();
        public List<string> upgrades = new List<string>();

        public bool Finished => results.Count >= Crises;
        public string CurrentType => Types[Mathf.Clamp(crisis, 0, Crises - 1)];
        public int Wins => results.Count(r => r.won);
        public int Losses => results.Count(r => !r.won);

        public static CampaignState New() => new CampaignState { seed = UnityEngine.Random.Range(1, int.MaxValue) };

        /// <summary>Krizin kuralları: bütün mekanikler, olay kartları ve kampanya zorluğu.</summary>
        public RuleOptions Rules()
        {
            var r = new RuleOptions { Stage = 4, Events = true };
            if (crisis >= 3) r.ExtraTraits.Add("aldatma");
            if (crisis >= 4) r.ExtraTraits.Add("hizlanma");
            // Kaybedilen her kriz YZ'ye bir yetenek daha verir
            foreach (var p in Penalties.Where(p => !r.ExtraTraits.Contains(p)).Take(Losses)) r.ExtraTraits.Add(p);
            return r;
        }

        public void Apply(Balance b, ExpOptions exp)
        {
            foreach (var id in upgrades) CampaignUpgrade.Get(id)?.Apply(b, exp);
        }

        /// <summary>Bu kriz sonrası seçilebilecek 3 yükseltme (tohumdan, alınmamış olanlardan).</summary>
        public List<CampaignUpgrade> Offers(bool teamsOn)
        {
            var rnd = new System.Random(seed + crisis * 7919);
            return CampaignUpgrade.All.Where(u => !upgrades.Contains(u.Id) && (teamsOn || !u.NeedsTeams)).OrderBy(_ => rnd.Next()).Take(3).ToList();
        }

        public void Record(string ending, int day)
        {
            if (results.Count > crisis) return;   // aynı kriz iki kez yazılmasın
            results.Add(new CrisisResult { type = CurrentType, ending = ending, day = day, won = GameData.Ending(ending).Kind == EndingKind.Win });
        }

        public void Advance(string upgradeId)
        {
            if (upgradeId != null && !upgrades.Contains(upgradeId)) upgrades.Add(upgradeId);
            crisis = results.Count;
        }

        // ---------------------------------------------------------------- kalıcılık
        static string FilePath => Path.Combine(Application.persistentDataPath, "campaign.json");

        public static CampaignState Load()
        {
            try { return File.Exists(FilePath) ? JsonUtility.FromJson<CampaignState>(File.ReadAllText(FilePath)) : null; }
            catch (Exception e) { Debug.LogWarning($"Kampanya okunamadı: {e.Message}"); return null; }
        }

        public void Save()
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(this)); }
            catch (Exception e) { Debug.LogWarning($"Kampanya kaydedilemedi: {e.Message}"); }
        }

        public static void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning($"Kampanya silinemedi: {e.Message}"); }
        }
    }

    [Serializable]
    public sealed class CrisisResult
    {
        public string type, ending;
        public int day;
        public bool won;
    }
}
