using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>İşgal harekâtı: sektör sırası, sınır ilerlemesi, tek ödül ve kayıt bütünlüğü.</summary>
    public class LiberationCampaignTests
    {
        static Simulation New(uint seed = 101) => Simulation.NewGame(null, "manipulator", seed,
            new Balance { startFon = 100 }, rules: new RuleOptions { LiberationCampaign = true });

        static void CaptureNext(Simulation sim)
        {
            string id = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id;
            Assert.IsTrue(sim.StartLiberationRaid(id).Ok, id);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok, id);
        }

        static void CompleteRegion(Simulation sim)
        {
            while (!sim.LiberationRegion(sim.S.Liberation.CurrentRegion).Completed) CaptureNext(sim);
        }

        [Test]
        public void Opening_IsKnownFullOccupation_WithOneTeamAndThreeSectorSequence()
        {
            var sim = New();
            Assert.IsTrue(sim.LiberationOn);
            Assert.AreEqual("weu", sim.S.Liberation.CurrentRegion);
            Assert.AreEqual(GameData.Regions.Length, sim.S.Liberation.Regions.Count);
            Assert.IsTrue(sim.S.Regions.All(r => sim.Shown(r).V == 100));
            Assert.AreEqual(1, sim.S.Teams.Count);
            Assert.AreEqual(new[] { "loot", "loot", "server" }, sim.LiberationRegion("weu").Sectors.Select(s => s.MissionKind).ToArray());
            Assert.IsNotNull(sim.Shelter);
            Assert.IsNull(sim.LiberationRaidCheck("weu_1"));
            Assert.IsNotNull(sim.LiberationRaidCheck("weu_2"));
            Assert.IsNotNull(sim.LiberationRaidCheck("na_1"));
        }

        [Test]
        public void SectorSuccess_CapturesExactlyOnce_AndPassesMissionToRaid()
        {
            var sim = New();
            double before = sim.S.Fon;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual("loot", sim.S.Raid.Params.MissionKind);
            Assert.AreEqual("weu_1", sim.S.Raid.Params.SectorId);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.AreEqual(before - sim.B.liberationRaidCost + sim.B.liberationLootFunds, sim.S.Fon);
            Assert.IsTrue(sim.LiberationSector("weu_1").Captured);
            Assert.AreEqual(2, sim.LiberationRegion("weu").Sectors.Count(s => !s.Captured));
            Assert.AreEqual(100.0 * 2 / 3, sim.Shown(sim.S.Region("weu")).V);
            before = sim.S.Fon;
            Assert.IsFalse(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsFalse(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.AreEqual(before, sim.S.Fon, "Temizlenmiş sektör ödülü tekrar alınamaz.");
            Assert.IsNull(sim.LiberationRaidCheck("weu_2"));
        }

        [Test]
        public void Failure_PreservesSector_RecoversTeam_AndCanBeRetried()
        {
            var sim = New();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = false, Down = 3, Retreat = true }).Ok);
            Assert.IsFalse(sim.LiberationSector("weu_1").Captured);
            Assert.AreEqual(1, sim.S.Teams.Count, "Son ekip kaybedilip harekât kilitlenmemeli.");
            Assert.IsNotNull(sim.LiberationRaidCheck("weu_1"));
            for (int i = 0; i < 3 * sim.B.liberationInjuryDays; i++) sim.Step();
            Assert.IsNull(sim.LiberationRaidCheck("weu_1"));
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual(2, sim.LiberationSector("weu_1").Attempts);
        }

        [Test]
        public void RegionCompletion_UnlocksOnlyAdjacentFrontier_AndPaysCaptureBonusOnce()
        {
            var sim = New();
            Assert.IsFalse(sim.SelectLiberationRegion("eeu").Ok, "Üç sektör tamamlanmadan sınır açılmaz.");
            CaptureNext(sim); CaptureNext(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_3").Ok);
            Assert.AreEqual("server", sim.S.Raid.Params.MissionKind);
            double before = sim.S.Fon;
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.AreEqual(before + sim.B.liberationServerFunds + sim.B.liberationCaptureFunds, sim.S.Fon);
            Assert.AreEqual(0, sim.Shown(sim.S.Region("weu")).V);
            CollectionAssert.AreEquivalent(new[] { "na", "eeu", "af", "me" }, sim.LiberationFrontier);
            Assert.IsFalse(sim.SelectLiberationRegion("cn").Ok);
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            Assert.IsFalse(sim.SelectLiberationRegion("na").Ok, "Yeni bölge yarım bırakılmaz.");
        }

        [Test]
        public void WorldTick_DoesNotTriggerLegacyLoss_AndRaidFreezesClock()
        {
            var sim = New();
            for (int i = 0; i < 400; i++) sim.Step();
            Assert.IsNull(sim.S.Ending);
            Assert.IsNull(sim.S.Illusion);
            Assert.IsNull(sim.S.PendingGift);
            Assert.IsNull(sim.S.PendingEvent);
            Assert.AreEqual(0, sim.S.Tek);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            int day = sim.S.Day;
            sim.Step();
            Assert.AreEqual(day, sim.S.Day);
        }

        [Test]
        public void LegacyActionsAndResearch_CannotBypassCampaign()
        {
            var sim = New();
            Assert.IsFalse(sim.Act(Actions.Audit, "weu").Ok);
            Assert.IsFalse(sim.Act(Actions.KillSwitch, "weu").Ok);
            Assert.IsFalse(sim.GlobalKillSwitch().Ok);
            Assert.IsFalse(sim.StartResearch("yorum").Ok);
            Assert.AreEqual(100, sim.Shown(sim.S.Region("weu")).V);
            Assert.IsTrue(sim.StartRaid("weu").Ok, "Eski masa komutu sıradaki sektöre yönlenir.");
            Assert.AreEqual(sim.B.liberationRaidCost, sim.RaidCost);
            Assert.AreEqual(0, sim.RaidSgCost);
        }

        [Test]
        public void WholeCampaign_CanReachEveryRegion_FromFrontier_AndWin()
        {
            var sim = New();
            for (int i = 0; i < GameData.Regions.Length; i++)
            {
                CompleteRegion(sim);
                if (i < GameData.Regions.Length - 1)
                {
                    Assert.IsNull(sim.S.Ending);
                    var frontier = sim.LiberationFrontier.ToArray();
                    Assert.IsNotEmpty(frontier, "Hiçbir bölge seçimi harekâtı çıkmaza sokmamalı.");
                    Assert.IsTrue(sim.SelectLiberationRegion(frontier[0]).Ok);
                }
            }
            Assert.AreEqual("silme", sim.S.Ending);
            Assert.IsTrue(sim.S.Liberation.Regions.All(r => r.Completed));
            Assert.IsTrue(sim.S.Regions.All(r => sim.Shown(r).V == 0));
        }

        [Test]
        public void SaveReplay_RestoresSectorProgressAndPendingRaidExactly()
        {
            var sim = New(142);
            CompleteRegion(sim);
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            for (int i = 0; i < 3; i++) sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid("eeu_1").Ok);
            sim.Record.day = sim.S.Day;
            var record = UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
            var restored = Replay.Restore(record);
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Liberation), UnityEngine.JsonUtility.ToJson(restored.S.Liberation));
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Raid), UnityEngine.JsonUtility.ToJson(restored.S.Raid));
            Assert.AreEqual(sim.S.Fon, restored.S.Fon);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.IsTrue(restored.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.IsTrue(restored.LiberationSector("eeu_1").Captured);
        }

        [Test]
        public void FlagFalse_PreservesNormalSimulationAndRng()
        {
            var a = Simulation.NewGame(null, "manipulator", 173);
            var b = Simulation.NewGame(null, "manipulator", 173, rules: new RuleOptions { LiberationCampaign = false });
            Assert.IsNull(a.S.Liberation);
            Assert.IsNull(b.S.Liberation);
            for (int i = 0; i < 40 && a.S.Ending == null; i++) { a.Step(); b.Step(); }
            Assert.AreEqual(a.S.Rng.State, b.S.Rng.State);
            Assert.AreEqual(a.S.Fon, b.S.Fon);
            Assert.AreEqual(a.S.Tek, b.S.Tek);
            CollectionAssert.AreEqual(a.S.Regions.Select(r => r.Inf), b.S.Regions.Select(r => r.Inf));
        }
    }
}
