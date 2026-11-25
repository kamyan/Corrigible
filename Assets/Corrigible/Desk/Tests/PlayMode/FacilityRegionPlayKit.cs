using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 12, parça P4b: harekâtı istenen bölgeye getiren PlayMode kurulumu (EditMode <c>FacilityDeskKit.Reach</c>'in karşılığı;
    /// test derlemeleri ayrı). Canlı oyunun simülasyonuna yalnız komut verilir ve komutlar kayda yazılır: sınırda değilse bulunulan bölge
    /// tamamlanır (<see cref="FacilityRewardPlayKit.WinRaid"/>, vardiya kapanışları), bekleyen sunucu ödülü seçilir, kapanış yapılır,
    /// sınırdan hedef (yoksa hedefe en az bağlantıyla yaklaşan bölge) seçilir. Sahne yüklenmez (<c>Changed</c> yayılmaz); masada inen
    /// raporlar <see cref="Drain"/> ile Enter'la dosyalanır. <see cref="FacilityRewardPlayKit.Tune"/> alarmı sıfırladığı için yol boyunca
    /// kuşatma çıkmaz.
    /// </summary>
    public static class FacilityRegionPlayKit
    {
        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        /// <summary>Harekâtı <paramref name="region"/>'a getirir (cephe o bölge, baskın yok). Ödül seçimi <paramref name="choice"/>.</summary>
        public static void Reach(Simulation sim, string region, string choice = FacilityGear.ChoiceRecipe)
        {
            for (int guard = 0; guard < 2 * GameData.Regions.Length; guard++)
            {
                var lib = sim.S.Liberation;
                if (lib.CurrentRegion == region && !sim.LiberationRegion(region).Completed) return;
                if (sim.LiberationRegion(lib.CurrentRegion).Completed)
                {
                    var frontier = sim.LiberationFrontier.ToList();
                    Assert.IsNotEmpty(frontier, "sınır boş: " + region);
                    string pick = frontier.Contains(region) ? region : frontier.OrderBy(r => Hops(sim, r, region)).First();
                    Ok(sim.SelectLiberationRegion(pick), "cephe " + pick);
                    continue;
                }
                string current = lib.CurrentRegion;
                for (int g = 0; g < 6; g++)
                {
                    var sector = sim.NextLiberationSector(current);
                    Ok(sim.StartLiberationRaid(sector.Id), "baskın emri " + sector.Id);
                    FacilityRewardPlayKit.WinRaid(sim);
                    if (sim.LiberationRegion(current).Completed) break;
                    FacilityRewardPlayKit.Close(sim);
                }
                Assert.IsTrue(sim.LiberationRegion(current).Completed, "bölge tamamlanmadı: " + current);
                if (!string.IsNullOrEmpty(sim.Facility?.PendingReward)) Ok(sim.ChooseServerReward(sim.Facility.PendingReward, choice), "sunucu ödülü");
                FacilityRewardPlayKit.Close(sim);
            }
            Assert.Fail("bölgeye varılamadı: " + region);
        }

        /// <summary>İki bölge arasındaki en az bağlantı (<see cref="Simulation.Links"/>; bağlantı yoksa int.MaxValue).</summary>
        static int Hops(Simulation sim, string from, string to)
        {
            var dist = new Dictionary<string, int> { [from] = 0 };
            var queue = new Queue<string>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                if (at == to) return dist[at];
                foreach (var l in sim.Links)
                {
                    string next = l.A == at ? l.B : l.B == at ? l.A : null;
                    if (next == null || dist.ContainsKey(next)) continue;
                    dist[next] = dist[at] + 1;
                    queue.Enqueue(next);
                }
            }
            return int.MaxValue;
        }

        /// <summary>Masada inen operasyon raporlarını Enter'la dosyalar (komutlarla ilerleyen harekâtın son baskınının raporu iner; dosyalanmadan
        /// görev fişi açılmaz).</summary>
        public static IEnumerator Drain()
        {
            var operation = BoardPlayKit.Find<SiginakOperation>();
            for (int i = 0; i < 4; i++)
            {
                float t = 0;
                while (t < 2.5f && !operation.ReportOpen) { t += Time.unscaledDeltaTime; yield return null; }
                if (!operation.ReportOpen) yield break;
                yield return BoardPlayKit.WaitSeconds(1.2f);
                yield return BoardPlayKit.PressKey(Key.Enter);
                yield return BoardPlayKit.WaitFor(() => !operation.Busy, 4, "rapor dosyalanmadı");
                yield return BoardPlayKit.WaitSeconds(.8f);
            }
        }
    }
}
