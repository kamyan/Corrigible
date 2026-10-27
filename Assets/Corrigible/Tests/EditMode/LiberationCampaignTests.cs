using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>İşgal harekâtı: sektör sırası, sınır ilerlemesi, tek ödül ve kayıt bütünlüğü.</summary>
    public class LiberationCampaignTests
    {
        static Simulation New(uint seed = 101) => Simulation.NewGame(seed, new Balance { startFon = 100 });

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
            Assert.AreEqual("weu", sim.S.Liberation.CurrentRegion);
            Assert.AreEqual(GameData.Regions.Length, sim.S.Liberation.Regions.Count);
            Assert.AreEqual(100, sim.Occupation);
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
            Assert.AreEqual(100.0 * 35 / 36, sim.Occupation, 1e-9);
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
            Assert.IsTrue(sim.LiberationRegion("weu").Completed);
            CollectionAssert.AreEquivalent(new[] { "na", "eeu", "af", "me" }, sim.LiberationFrontier);
            Assert.IsFalse(sim.SelectLiberationRegion("cn").Ok);
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            Assert.IsFalse(sim.SelectLiberationRegion("na").Ok, "Yeni bölge yarım bırakılmaz.");
        }

        [Test]
        public void Shifts_NeverEndTheCampaign_AndRaidFreezesClock()
        {
            var sim = New();
            for (int i = 0; i < 400; i++) sim.Step();
            Assert.IsNull(sim.S.Ending, "Harekât yalnız bütün bölgeler geri alınınca biter.");
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            int day = sim.S.Day;
            sim.Step();
            Assert.AreEqual(day, sim.S.Day);
        }

        [Test]
        public void MachinesCopyThemselves_AfterTwoLiberatedRegions()
        {
            var sim = New();
            CompleteRegion(sim);
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            Assert.IsTrue(sim.StartLiberationRaid("eeu_1").Ok);
            Assert.IsEmpty(sim.S.Raid.Params.Traits, "Bir bölge özgürken makineler henüz kopyalanmaz.");
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            CompleteRegion(sim);
            Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            Assert.IsTrue(sim.StartLiberationRaid(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id).Ok);
            CollectionAssert.AreEqual(new[] { "kopya" }, sim.S.Raid.Params.Traits);
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
            Assert.AreEqual(0, sim.Occupation);
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
    }
}
