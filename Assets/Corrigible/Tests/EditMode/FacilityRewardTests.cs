using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 8, sunucu ödülü ve kazanımların sığınak tarafı (Docs/TESIS-BASKINLARI-TASARIMI.md §6.1–6.2, §11.3): bölge ilk kez
    /// tamamlanınca ödül bekler, baskın emrini kilitler, <c>server_reward</c> komutuyla tarif ya da kazanım seçilir ve kayıttan aynen
    /// yeniden kurulur. Kazanım testleri kazanımı komutla verir (<see cref="Grant"/>: bekleyen ödül elle kurulur, seçim komutla).
    /// </summary>
    public class FacilityRewardTests
    {
        static Simulation NewSim(uint seed = 79, Action<Balance> tune = null)
        {
            var b = Bal();
            tune?.Invoke(b);
            return ShelterEventKit.New(b, seed);
        }

        static Simulation Restore(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            var r = Replay.Restore(sim.Record);
            Assert.AreEqual(0, r.ReplayFailures);
            return r;
        }

        /// <summary>Bölgenin ödülünü komutla verir: bekleyen ödül elle kurulur (bölgeyi oynamadan), seçim server_reward komutudur.</summary>
        static void Grant(Simulation sim, string region, string choice)
        {
            sim.Facility.PendingReward = region;
            var r = sim.ChooseServerReward(region, choice);
            Assert.IsTrue(r.Ok, r.Msg);
        }

        /// <summary>Kazanımı veren bölge (ödül tablosundan).</summary>
        static string RegionOf(string perk) => GameData.Regions.Select(r => r.Id).First(id => FacilityGear.Reward(id).Perk == perk);

        static int Commands(Simulation sim) => sim.Record.commands.Count;

        // ---------- Ödülün kurulması ve kilit ----------
        [Test]
        public void Reward_PendingAfterRegionComplete()
        {
            var sim = NewSim();
            Assert.AreEqual("", sim.Facility.PendingReward);
            string region = CompleteRegion(sim);
            Assert.AreEqual("weu", region);
            Assert.AreEqual("weu", sim.Facility.PendingReward);
            Assert.IsTrue(sim.LiberationRegion("weu").Rewarded);
            var logs = sim.S.RaidLog;
            Assert.AreEqual("weu", logs.Last().LiberationReward.ServerReward, "sunum alanı");
            Assert.IsTrue(logs.Take(logs.Count - 1).All(l => l.LiberationReward.ServerReward == null), "önceki baskınlarda ödül yok");
            CollectionAssert.AreEqual(new[] { "tarif", "kazanim" }, sim.ServerRewardOptions("weu"));
            CollectionAssert.IsEmpty(sim.ServerRewardOptions("xx"));
        }

        [Test]
        public void Reward_BlocksRaidOrder_NotShiftClose()
        {
            var sim = NewSim();
            CompleteRegion(sim);
            ShelterEventKit.Close(sim);
            Assert.AreEqual("weu", sim.Facility.PendingReward, "kapanış ödülü düşürmez");
            Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok, "bölge seçimi kilitlenmez");
            ShelterEventKit.FillSquad(sim);
            var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.AreEqual(Loc.T("Önce sunucu ödülünü seç: tarif ya da kazanım."), sim.LiberationRaidCheck(sector.Id));
            int commands = Commands(sim);
            Assert.IsFalse(sim.StartLiberationRaid(sector.Id).Ok);
            Assert.AreEqual(commands, Commands(sim), "reddedilen emir kayda girmez");
            Assert.IsTrue(sim.QueueProduction("armor").Ok, "pano komutları kilitlenmez");
            Assert.IsTrue(sim.CancelProduction(sim.Board.Orders[0].Id).Ok);
            Assert.IsTrue(ChooseReward(sim));
            Assert.IsNull(sim.LiberationRaidCheck(sector.Id));
            Assert.IsTrue(sim.StartLiberationRaid(sector.Id).Ok);
        }

        [Test]
        public void Reward_Tarif_UnlocksRecipe()
        {
            var sim = NewSim();
            Assert.AreEqual(6, sim.BoardRecipes.Count);
            Assert.AreEqual(Loc.T("Böyle bir tarif yok."), sim.QueueProduction(FacilityGear.Wire).Msg);
            CompleteRegion(sim);
            var r = sim.ChooseServerReward("weu", "tarif");
            Assert.IsTrue(r.Ok, r.Msg);
            Assert.AreEqual(Loc.F("Tarif açıldı: {0}.", Loc.T("Örme seti")), r.Msg);
            CollectionAssert.AreEqual(new[] { FacilityGear.Wire }, sim.Facility.Recipes);
            CollectionAssert.IsEmpty(sim.Facility.Perks);
            Assert.AreEqual("", sim.Facility.PendingReward);
            Assert.AreEqual(7, sim.BoardRecipes.Count);
            var recipe = sim.BoardRecipes.Last();
            Assert.AreEqual(FacilityGear.Wire, recipe.Id);
            Assert.IsTrue(recipe.Reward);
            Assert.AreEqual("server_reward", sim.Record.commands.Last().kind);
            ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.QueueProduction(FacilityGear.Wire).Ok);
        }

        [Test]
        public void Reward_Kazanim_AddsPerk_RecipeStaysLocked()
        {
            var sim = NewSim();
            CompleteRegion(sim);
            var r = sim.ChooseServerReward("weu", "kazanim");
            Assert.IsTrue(r.Ok, r.Msg);
            Assert.AreEqual(Loc.F("Kazanım: {0}.", Loc.T("Kulak")), r.Msg);
            CollectionAssert.AreEqual(new[] { FacilityGear.Ear }, sim.Facility.Perks);
            CollectionAssert.IsEmpty(sim.Facility.Recipes);
            Assert.AreEqual(6, sim.BoardRecipes.Count);
            Assert.IsFalse(sim.QueueProduction(FacilityGear.Wire).Ok, "seçilmeyen tarif alınamaz");
            Assert.AreEqual(Loc.T("Bu bölgenin bekleyen sunucu ödülü yok."), sim.ChooseServerReward("weu", "tarif").Msg, "ikinci seçim yok");
        }

        /// <summary>Savunma alt bayrağı kapalı (aşama 10 davranışı): ikinci ödül yok, bölge fonu yeniden verilir.</summary>
        [Test]
        public void Reward_NotTwice_AfterRecapture()
        {
            var sim = NewSim(tune: b => b.facilityDefenseEnabled = false);
            CompleteRegion(sim);
            Assert.IsTrue(ChooseReward(sim));
            ShelterEventKit.Close(sim);
            // Karşı saldırının yaptığı gibi sunucu kanadı düşer (durum doğrudan); bölge yeniden alınınca ödül yok
            var lost = sim.LiberationSector("weu_3");
            lost.Captured = false;
            Assert.IsFalse(sim.LiberationRegion("weu").Completed);
            double fon = sim.S.Fon;
            ShelterEventKit.FillSquad(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_3").Ok);
            WinRaid(sim);
            Assert.IsTrue(sim.LiberationRegion("weu").Completed);
            Assert.AreEqual("", sim.Facility.PendingReward, "ikinci ödül yok");
            Assert.IsNull(sim.S.RaidLog.Last().LiberationReward.ServerReward);
            CollectionAssert.AreEqual(new[] { FacilityGear.Wire }, sim.Facility.Recipes);
            Assert.AreEqual(fon - sim.B.liberationRaidCost + sim.B.liberationServerFunds + sim.B.liberationCaptureFunds, sim.S.Fon,
                "savunma kapalıyken bölge fonu yeniden verilir (aşama 10)");
            Assert.IsFalse(sim.S.RaidLog.Last().LiberationReward.RegionRepeat);
            Assert.IsTrue(sim.RegionFundsDue("weu"));
        }

        /// <summary>
        /// Aşama 11 (§9.3, K3): bölge tamamlanır → kuşatma → bırak → yeniden tamamlanır: bölge fonu ve bölge güveni ikinci kez verilmez,
        /// sunucu ödülü ve önlem öğrenmesi yok; baskının kendi ganimeti (sunucu fonu, pano malzemesi) her seferinde. Ödülün sunum alanı
        /// <see cref="LiberationRaidReward.RegionRepeat"/>.
        /// </summary>
        [Test]
        public void Recapture_AfterYield_NoSecondRegionFundsOrTrust()
        {
            var sim = NewSim();
            string region = CompleteRegion(sim);
            Assert.IsFalse(sim.S.RaidLog.Last().LiberationReward.RegionRepeat, "ilk tamamlanış");
            Assert.IsFalse(sim.RegionFundsDue(region));
            Assert.IsTrue(sim.RegionRewarded(region));
            Assert.IsTrue(ChooseReward(sim));
            ShelterEventKit.Close(sim);
            var e = ForceSiege(sim);
            Assert.AreEqual(region + "_3", e.Sector, "son alınan kanat");
            Assert.IsTrue(sim.YieldSector().Ok);
            Assert.IsFalse(sim.LiberationRegion(region).Completed);
            Assert.IsTrue(sim.LiberationRegion(region).Rewarded, "Rewarded kalır");
            ShelterEventKit.Close(sim);
            int learned = sim.Facility.Learned.Count;
            int metal = sim.Board.Material("metal")?.Quantity ?? 0;
            double fon = sim.S.Fon;
            // Güven tavanın altında (tavan farkı yutmasın)
            sim.S.Trust = 5;
            int trust = sim.S.Trust;
            ShelterEventKit.FillSquad(sim);
            Assert.IsTrue(sim.StartLiberationRaid(e.Sector).Ok);
            WinRaid(sim);
            var log = sim.S.RaidLog.Last();
            var rw = log.LiberationReward;
            Assert.IsTrue(sim.LiberationRegion(region).Completed);
            Assert.IsTrue(rw.RegionCompleted);
            Assert.IsTrue(rw.RegionRepeat);
            Assert.AreEqual(fon - sim.B.liberationRaidCost + sim.B.liberationServerFunds, sim.S.Fon, "bölge fonu ikinci kez yok; sunucu fonu var");
            Assert.AreEqual(rw.TrustFromCivilians - log.Collateral * sim.B.liberationTrustPerCivilian, sim.S.Trust - trust, "bölge güveni ikinci kez yok");
            Assert.AreEqual("", sim.Facility.PendingReward, "ikinci ödül yok");
            Assert.IsNull(rw.ServerReward);
            Assert.IsNull(rw.Countermeasure);
            Assert.AreEqual(learned, sim.Facility.Learned.Count, "öğrenme yok");
            Assert.AreEqual(metal + sim.B.boardServerMetal, sim.Board.Material("metal")?.Quantity ?? 0, "baskın ganimeti var");
        }

        [Test]
        public void Reward_NoneOnVictory()
        {
            var sim = NewSim();
            foreach (var area in sim.S.Liberation.Regions.Where(r => r.Id != "weu"))
                foreach (var s in area.Sectors) s.Captured = true;
            CompleteRegion(sim);
            Assert.AreEqual("silme", sim.S.Ending);
            Assert.AreEqual("", sim.Facility.PendingReward, "zaferde seçim kurulmaz");
            Assert.IsNull(sim.S.RaidLog.Last().LiberationReward.ServerReward);
        }

        [Test]
        public void Reward_AnswerPackage_TarifRecordedNoRecipe()
        {
            var sim = NewSim();
            foreach (string region in new[] { "na", "cn", "eeu", "ru", "oc" })
                CollectionAssert.AreEqual(new[] { "tarif", "kazanim" }, sim.ServerRewardOptions(region), region);
            Grant(sim, "na", "tarif");
            CollectionAssert.AreEqual(new[] { FacilityGear.AnsSokum }, sim.Facility.Recipes, "K22: cevap kimliği yazılır");
            Assert.AreEqual(6, sim.BoardRecipes.Count, "cevap tarifi deftere girmez (aşama 10)");
            Assert.IsFalse(sim.QueueProduction(FacilityGear.AnsSokum).Ok);
            Assert.AreEqual(Loc.T("Söküm anahtarı"), FacilityGear.RecipeName(FacilityGear.AnsSokum));
        }

        [Test]
        public void Reward_Check_Messages()
        {
            var sim = NewSim();
            Assert.AreEqual(Loc.T("Bu bölgenin bekleyen sunucu ödülü yok."), sim.ChooseServerRewardCheck("weu", "tarif"));
            sim.Facility.PendingReward = "weu";
            Assert.AreEqual(Loc.T("Bu bölgenin bekleyen sunucu ödülü yok."), sim.ChooseServerRewardCheck("af", "tarif"));
            Assert.AreEqual(Loc.T("Bu bölgenin bekleyen sunucu ödülü yok."), sim.ChooseServerRewardCheck(null, "tarif"));
            Assert.AreEqual(Loc.T("Tarif ya da kazanım seç."), sim.ChooseServerRewardCheck("weu", "ikisi"));
            Assert.AreEqual(Loc.T("Tarif ya da kazanım seç."), sim.ChooseServerRewardCheck("weu", null));
            Assert.IsNull(sim.ChooseServerRewardCheck("weu", "kazanim"));
            int commands = Commands(sim);
            uint rng = sim.S.Rng.State;
            Assert.IsFalse(sim.ChooseServerReward("weu", "ikisi").Ok);
            Assert.AreEqual(commands, Commands(sim), "reddedilen seçim kayda girmez");

            var raiding = Sim();
            raiding.Facility.PendingReward = "weu";
            Assert.AreEqual(Loc.T("Önce süren baskını bitir."), raiding.ChooseServerRewardCheck("weu", "tarif"));

            sim.S.Ending = "silme";
            Assert.AreEqual(Loc.T("Oyun bitti."), sim.ChooseServerRewardCheck("weu", "tarif"));
            Assert.AreEqual(rng, sim.S.Rng.State);
        }

        [Test]
        public void Reward_Replay_SameState()
        {
            var sim = NewSim();
            CompleteRegion(sim);
            Assert.IsTrue(ChooseReward(sim, "kazanim"));
            ShelterEventKit.Close(sim);
            string second = CompleteRegion(sim);
            Assert.AreEqual(second, sim.Facility.PendingReward);
            Assert.IsTrue(ChooseReward(sim, "tarif"));
            ShelterEventKit.Close(sim);
            Assert.AreEqual(2, sim.Record.commands.Count(c => c.kind == "server_reward"));
            var r = Restore(sim);
            Assert.AreEqual(State(sim), State(r));
            CollectionAssert.AreEqual(sim.Facility.Recipes, r.Facility.Recipes);
            CollectionAssert.AreEqual(sim.Facility.Perks, r.Facility.Perks);
            CollectionAssert.AreEqual(new[] { FacilityGear.Ear }, r.Facility.Perks);
            CollectionAssert.AreEqual(new[] { FacilityGear.Reward(second).Recipe }, r.Facility.Recipes);
            CollectionAssert.AreEqual(sim.S.Liberation.Regions.Select(x => x.Rewarded), r.S.Liberation.Regions.Select(x => x.Rewarded));
            Assert.AreEqual(sim.S.Rng.State, r.S.Rng.State);
        }

        [Test]
        public void Reward_PendingSurvivesSaveAndContinue()
        {
            var sim = NewSim();
            CompleteRegion(sim);
            var r = Restore(sim);
            Assert.AreEqual("weu", r.Facility.PendingReward, "bekleyen ödül kayıttan yeniden kurulur");
            Assert.IsTrue(r.ChooseServerReward("weu", "tarif").Ok);
        }

        [Test]
        public void Reward_UnknownCaseGuard()
        {
            var sim = NewSim();
            sim.Record.commands.Add(new SavedCommand { day = 0, kind = "server_reward", a = "xx", b = "tarif" });
            var r = Replay.Restore(sim.Record);
            Assert.AreEqual(1, r.ReplayFailures, "server_reward tanınır: geçersiz bölge başarısız sayılır (bilinmeyen tür gibi sessiz geçmez)");
        }

        [Test]
        public void Fingerprint_F8_RejectsEarlierFacilityGenerators()
        {
            var on = Simulation.NewGame(79, Bal());
            Assert.AreEqual("f10", on.Record.facility, "aşama 17: sahadaki sızmacı (varsayılan açık; saha kapalıyken f8, aşama 14)");
            Assert.AreEqual(10, FacilityMaps.GeneratorVersion);
            on.Record.facility = "f9";
            Assert.IsFalse(Replay.MapMatches(on.Record), "sahadaki sızmacı aşamasının tesis kaydı (f9; sürüm 10'dan önce) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f7";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 13'ün tesis kaydı (f7) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f6";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 12'nin tesis kaydı (f6) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f5";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 11'in tesis kaydı (f5) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            Assert.AreEqual(22, GameRecord.CurrentVersion, "22: görevde iyileşme; 21: tek kip; 20 (aşama 13): g5 paket yerleşimi");
            on.Record.facility = "f4";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 10'un tesis kaydı (f4) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f3";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 8–9'un tesis kaydı (f3) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f2";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 6'nın tesis kaydı (f2) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            Assert.AreEqual("f10", Simulation.NewGame(79, new Balance()).Record.facility, "varsayılan denge de tesis izi taşır (tek kip)");
        }

        // ---------- Kazanımlar ----------
        [Test]
        public void Perks_TableIsComplete()
        {
            CollectionAssert.AreEquivalent(GameData.Regions.Select(r => FacilityGear.Reward(r.Id).Perk), FacilityGear.Perks);
            CollectionAssert.AreEquivalent(GameData.Regions.Select(r => FacilityGear.Reward(r.Id).Recipe), FacilityGear.Kinds.Concat(FacilityGear.Answers));
            var b = new Balance();
            foreach (string perk in FacilityGear.Perks)
            {
                Assert.AreNotEqual(perk, FacilityGear.PerkName(perk), perk);
                Assert.IsNotEmpty(FacilityGear.PerkDesc(b, perk), perk);
            }
            var none = FacilityGear.Reward("xx");
            Assert.IsNull(none.Recipe);
            Assert.IsNull(none.Perk);
        }

        [Test]
        public void Perk_Ear_WatchPlusOne()
        {
            int Gain(string choice)
            {
                var sim = NewSim(tune: b => b.facilityIntelCost = 100);
                CompleteRegion(sim);
                Assert.IsTrue(ChooseReward(sim, choice));
                ShelterEventKit.Close(sim);
                var p = sim.Board.People.First(x => x.Alive && x.Job == "rest" && x.Down <= 0);
                Assert.IsTrue(sim.AssignResident(p.Id, "watch").Ok);
                int before = sim.Facility.Intel;
                ShelterEventKit.Close(sim);
                return sim.Facility.Intel - before;
            }
            Assert.AreEqual(1, Gain("tarif"));
            Assert.AreEqual(2, Gain("kazanim"), "Kulak (weu): +facilityEarIntel");
        }

        [Test]
        public void Perk_PlanBook_LearnsFirstEntryOnSelect()
        {
            bool Learns(bool perk)
            {
                var sim = NewSim();
                CompleteRegion(sim);
                Assert.IsTrue(ChooseReward(sim, "tarif"));
                if (perk) Grant(sim, RegionOf(FacilityGear.PlanBook), "kazanim");
                ShelterEventKit.Close(sim);
                string next = sim.LiberationFrontier.First();
                Assert.IsTrue(sim.SelectLiberationRegion(next).Ok);
                return sim.FacilityKnown(FacilityMaps.RoomId(sim.NextLiberationSector(next).Id, 1));
            }
            Assert.IsFalse(Learns(false));
            Assert.IsTrue(Learns(true), "Plan defteri: yeni cephenin ilk giriş odası");
        }

        /// <summary>Elle oda raporlarıyla biten tesis baskını: G ve A geçilir, hedef odasında kaçışla çıkılır; kadronun ilk kişisi
        /// düşmüş ve tesisten çıkarılmıştır (<paramref name="extracted"/>) ya da yerde kalmıştır.</summary>
        internal static void HandRaid(Simulation sim, bool extracted)
        {
            int squad = RaidChain.SquadCount(sim.S.Raid.Params);
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(1, squad: squad)).Ok);
            Assert.IsTrue(sim.AdvanceRaidRoom(Rep(2, squad: squad)).Ok);
            var last = Rep(3, squad: squad);
            last.Last = true;
            last.Success = true;
            if (sim.S.Raid.Params.MissionKind == "loot") last.Cargo = 1;
            last.Team[0].State = "d";
            last.Team[0].Hp = 0;
            last.Team[0].Body = extracted ? 2 : 0;
            var ok = sim.AdvanceRaidRoom(last);
            Assert.IsTrue(ok.Ok, ok.Msg);
            var res = sim.ResolveRaid(null);
            Assert.IsTrue(res.Ok, res.Msg);
        }

        [Test]
        public void Perk_Infirmary_DownMinusOne_Min1()
        {
            int Down(bool perk, int injury)
            {
                var sim = Sim(tune: b => b.boardInjuryTurns = injury, before: s => { if (perk) Grant(s, RegionOf(FacilityGear.Infirmary), "kazanim"); });
                string id = sim.S.Raid.Params.Squad[0].PersonId;
                HandRaid(sim, true);
                var p = sim.Board.Person(id);
                Assert.IsTrue(p.Alive, "çıkarılan düşen yaşar");
                return p.Down;
            }
            int plain = Down(false, 4);
            Assert.Greater(plain, 1);
            Assert.AreEqual(plain - 1, Down(true, 4), "Revir: −facilityInfirmaryShifts");
            Assert.AreEqual(1, Down(false, 1));
            Assert.AreEqual(1, Down(true, 1), "en az 1");
        }

        [Test]
        public void Perk_Workbench_RewardRecipesShorter_Min1()
        {
            var sim = NewSim();
            Grant(sim, "sa", "tarif");
            Grant(sim, "weu", "tarif");
            var before = sim.BoardRecipes.ToDictionary(r => r.Id, r => r.Turns);
            Assert.AreEqual(2, before[FacilityGear.Turret]);
            Assert.AreEqual(1, before[FacilityGear.Wire]);
            Grant(sim, RegionOf(FacilityGear.Workbench), "kazanim");
            var after = sim.BoardRecipes.ToDictionary(r => r.Id, r => r.Turns);
            Assert.AreEqual(1, after[FacilityGear.Turret], "Tezgâh: −facilityWorkbenchShifts");
            Assert.AreEqual(1, after[FacilityGear.Wire], "en az 1");
            foreach (string old in new[] { "armor", "medkit", "emp_cell", "kit_tek", "kit_emp", "kit_ana" })
                Assert.AreEqual(before[old], after[old], "eski altıya işlemez: " + old);
        }

        [Test]
        public void Perk_SpareBag_WireAndChargePlusOne()
        {
            var b = new Balance();
            var bag = new[] { FacilityGear.SpareBag };
            Assert.AreEqual(2, FacilityGear.RaidCharges(b, FacilityGear.Wire, null));
            Assert.AreEqual(3, FacilityGear.RaidCharges(b, FacilityGear.Wire, bag));
            Assert.AreEqual(2, FacilityGear.RaidCharges(b, FacilityGear.Charge, null));
            Assert.AreEqual(3, FacilityGear.RaidCharges(b, FacilityGear.Charge, bag));
            foreach (string kind in new[] { FacilityGear.Turret, FacilityGear.Decoy, FacilityGear.Winch, FacilityGear.Clamp })
                Assert.AreEqual(1, FacilityGear.RaidCharges(b, kind, bag), "öbür türlere işlemez: " + kind);
            Assert.AreEqual(-1, FacilityGear.RaidCharges(b, FacilityGear.Dog, bag));
            Assert.AreEqual(0, FacilityGear.RaidCharges(b, "medkit", bag));

            // Gerçek yol: kazanım komutla, hak baskın emrinde
            var sim = Sim(before: s =>
            {
                Grant(s, RegionOf(FacilityGear.SpareBag), "kazanim");
                Assert.IsNotNull(s.AddBoardEquipment(FacilityGear.Wire, s.PlanningSquad()[0].PersonId));
                Assert.IsNotNull(s.AddBoardEquipment(FacilityGear.Turret, s.PlanningSquad()[1].PersonId));
            });
            var squad = sim.S.Raid.Params.Squad;
            Assert.AreEqual(FacilityGear.Wire, squad[0].Gear);
            Assert.AreEqual(3, squad[0].GearCharges);
            Assert.AreEqual(FacilityGear.Turret, squad[1].Gear);
            Assert.AreEqual(1, squad[1].GearCharges);
        }

        /// <summary>Siper saldırı baskınını değiştirmez (etkisi savunmada; <see cref="Perk_Cover_DefenseRelayPlusOne"/>).</summary>
        [Test]
        public void Perk_Cover_NoEffectOnAttack()
        {
            string Snapshot(bool perk)
            {
                var sim = Sim(before: s => { if (perk) Grant(s, RegionOf(FacilityGear.Cover), "kazanim"); });
                var p = sim.S.Raid.Params;
                return string.Join(";", sim.BoardRecipes.Select(r => r.Id + r.Turns)) + "|"
                    + string.Join(";", p.Squad.Select(x => $"{x.Type}:{x.Hp}:{x.Armor}:{x.Gear}:{x.GearCharges}")) + "|" + Baskin.Signature(sim.OpenRaidRoom());
            }
            Assert.AreEqual(Snapshot(false), Snapshot(true), "Siper saldırıda etkisiz");
        }

        /// <summary>Aşama 11 (§6.2, K33): Siper savunmada saha rölesine +<see cref="Balance.facilityCoverRelayHp"/> can; metin dengeden.</summary>
        [Test]
        public void Perk_Cover_DefenseRelayPlusOne()
        {
            var b = Bal();
            Assert.AreEqual(Loc.F("Savunmada saha rölesi +{0} can.", 1), FacilityGear.PerkDesc(b, FacilityGear.Cover));
            b.facilityCoverRelayHp = 2;
            Assert.AreEqual(Loc.F("Savunmada saha rölesi +{0} can.", 2), FacilityGear.PerkDesc(b, FacilityGear.Cover));
            foreach (bool perk in new[] { false, true })
            {
                var sim = NewSim();
                if (perk) Grant(sim, RegionOf(FacilityGear.Cover), "kazanim");
                CaptureNext(sim);
                ForceSiege(sim);
                Defend(sim);
                Assert.AreEqual(perk, sim.S.Raid.Params.Perks.Contains(FacilityGear.Cover));
                int want = sim.B.facilityDefenseRelayHp + (perk ? sim.B.facilityCoverRelayHp : 0);
                Assert.AreEqual(want, sim.DefenseRelayHpFor(sim.S.Raid.Params.Perks));
                for (int i = 0; i < 2; i++)
                {
                    var B = sim.OpenRaidRoom();
                    Assert.AreEqual(-1, Baskin.RelayHp(B), "röle yalnız hedef odasında");
                    Calm(B);
                    ToGate(B);
                    Assert.IsTrue(Baskin.Pass(B));
                    Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
                }
                var H = sim.OpenRaidRoom();
                Assert.AreEqual(want, Baskin.RelayHp(H), perk ? "Siper" : "Sipersiz");
                Assert.AreEqual(want, Baskin.RelayMax(H));
            }
        }

        [Test]
        public void Perks_CopiedToRaidParams_AtOrder()
        {
            var sim = Sim(before: s =>
            {
                Grant(s, RegionOf(FacilityGear.Sight), "kazanim");
                Grant(s, RegionOf(FacilityGear.Fur), "kazanim");
            });
            var perks = sim.S.Raid.Params.Perks;
            CollectionAssert.AreEqual(new[] { FacilityGear.Sight, FacilityGear.Fur }, perks);
            Assert.AreNotSame(sim.Facility.Perks, perks, "kopya");
            sim.Facility.Perks.Add(FacilityGear.Pulley);
            CollectionAssert.AreEqual(new[] { FacilityGear.Sight, FacilityGear.Fur }, sim.S.Raid.Params.Perks, "sonradan eklenen süren baskını değiştirmez");
            sim.Facility.PendingReward = "oc";
            Assert.IsFalse(sim.ChooseServerReward("oc", "kazanim").Ok, "baskın sürerken ödül seçilmez");
        }
    }
}
