using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 8, pano tarafı (Docs/TESIS-BASKINLARI-TASARIMI.md §5.1, §5.6): ödülle açılan tarifler (kilit, sıra, girdi, süre,
    /// erzak bedeli), otomatik yolun yalnız eski altıyı tanıması, tesis kipinde emir açıkken eksik kitin üretimi, kadroya ekipman
    /// (yardımcı yuva, baskın hakkı), köpeğin dinlenmesi ve ölen sahibin köpeği. Tarif ödülü komutla verilir (bekleyen ödül elle kurulur).
    /// </summary>
    public class FacilityRecipeTests
    {
        static Simulation NewSim(Action<Balance> tune = null)
        {
            var b = Bal();
            b.admissionKnockMin = b.admissionKnockMax = 100;
            tune?.Invoke(b);
            return ShelterEventKit.New(b, 79);
        }

        static void Unlock(Simulation sim, string kind)
        {
            string region = GameData.Regions.Select(r => r.Id).First(id => FacilityGear.Reward(id).Recipe == kind);
            sim.Facility.PendingReward = region;
            var r = sim.ChooseServerReward(region, "tarif");
            Assert.IsTrue(r.Ok, r.Msg);
        }

        static void Stock(Simulation sim, int each = 6)
        {
            foreach (string kind in PlanningBoardData.MaterialKinds) sim.AddBoardMaterial(kind, each);
        }

        static void Order(Simulation sim, string recipeId)
        {
            var q = sim.QueueProduction(recipeId);
            Assert.IsTrue(q.Ok, q.Msg);
            foreach (var input in sim.BoardRecipes.Single(r => r.Id == recipeId).Inputs)
            {
                var r = sim.ReserveProductionMaterial(input.Kind, input.Quantity);
                Assert.IsTrue(r.Ok, r.Msg);
            }
        }

        static int Count(Simulation sim, string kind) => sim.Board.Equipment.Count(e => e.Kind == kind);
        static int Qty(Simulation sim, string kind) => sim.Board.Material(kind)?.Quantity ?? 0;

        // ---------- Tarif kilidi ve sırası ----------
        [Test]
        public void Recipes_LockedRejectedByQueue()
        {
            var sim = NewSim();
            int commands = sim.Record.commands.Count;
            foreach (string kind in FacilityGear.Kinds)
                Assert.AreEqual(Loc.T("Böyle bir tarif yok."), sim.QueueProduction(kind).Msg, kind);
            Assert.AreEqual(commands, sim.Record.commands.Count);
        }

        [Test]
        public void Recipes_UnlockOrder_AppendsPages()
        {
            var sim = NewSim();
            var old = sim.BoardRecipes.Select(r => r.Id).ToList();
            Unlock(sim, FacilityGear.Dog);
            Unlock(sim, FacilityGear.Wire);
            Unlock(sim, FacilityGear.Turret);
            var ids = sim.BoardRecipes.Select(r => r.Id).ToList();
            CollectionAssert.AreEqual(old, ids.Take(6), "eski sayfaların dizini kaymaz");
            CollectionAssert.AreEqual(new[] { FacilityGear.Dog, FacilityGear.Wire, FacilityGear.Turret }, ids.Skip(6), "açılış sırası");
            Assert.IsTrue(sim.BoardRecipes.Skip(6).All(r => r.Reward));
            Assert.IsTrue(sim.BoardRecipes.Take(6).All(r => !r.Reward));
        }

        static readonly Dictionary<string, (int metal, int devre, int kumas, int kimyasal, double supplies, int turns)> Table =
            new Dictionary<string, (int, int, int, int, double, int)>
            {
                { FacilityGear.Wire, (2, 0, 1, 0, 0, 1) },
                { FacilityGear.Charge, (1, 0, 0, 2, 0, 2) },
                { FacilityGear.Turret, (2, 1, 0, 0, 0, 2) },
                { FacilityGear.Decoy, (1, 1, 1, 0, 0, 1) },
                { FacilityGear.Winch, (2, 0, 1, 0, 0, 1) },
                { FacilityGear.Clamp, (1, 2, 0, 0, 0, 2) },
                { FacilityGear.Dog, (0, 0, 1, 0, 4, 3) },
            };

        [Test]
        public void Recipe_InputsShiftsOutput([Values("wire_kit", "charge", "turret", "decoy", "winch", "clamp", "dog")] string kind)
        {
            var sim = NewSim(tune: b => b.boardCraftExtraProgress = 0);
            Unlock(sim, kind);
            Stock(sim);
            sim.Shelter.Supplies = 60;
            var recipe = sim.BoardRecipes.Single(r => r.Id == kind);
            var want = Table[kind];
            int Need(string m) => recipe.Inputs.Where(i => i.Kind == m).Sum(i => i.Quantity);
            Assert.AreEqual(want.metal, Need("metal"), "metal");
            Assert.AreEqual(want.devre, Need("devre"), "devre");
            Assert.AreEqual(want.kumas, Need("kumas"), "kumaş");
            Assert.AreEqual(want.kimyasal, Need("kimyasal"), "kimyasal");
            Assert.IsTrue(recipe.Inputs.All(i => i.Quantity > 0), "sıfır adetli girdi yazılmaz");
            Assert.AreEqual(want.supplies, recipe.Supplies);
            Assert.AreEqual(want.turns, recipe.Turns);
            Assert.AreEqual(kind, recipe.Output);
            Assert.AreEqual(PlanningBoardData.EquipmentName(kind), recipe.Name);
            Assert.AreEqual("utility", PlanningBoardData.EquipmentSlot(kind));

            var before = PlanningBoardData.MaterialKinds.ToDictionary(m => m, m => Qty(sim, m));
            Order(sim, kind);
            for (int shift = 1; shift <= recipe.Turns; shift++)
            {
                ShelterEventKit.Close(sim);
                if (shift == 1)
                    foreach (string m in PlanningBoardData.MaterialKinds)
                        Assert.AreEqual(before[m] - Need(m), Qty(sim, m), "girdi emir başlarken düşer: " + m);
                if (shift < recipe.Turns)
                {
                    Assert.AreEqual(0, Count(sim, kind), "süre dolmadan çıkmaz");
                    Assert.AreEqual(recipe.Turns - shift, sim.Board.Orders.Single().Remaining);
                }
            }
            Assert.AreEqual(1, Count(sim, kind));
            var item = sim.Board.Equipment.Single(e => e.Kind == kind);
            Assert.IsNull(item.OwnerId, "depoya");
            Assert.AreEqual(1, item.Charges);
            Assert.IsFalse(item.Bound, "K14: köpek de kişiye bağlı değil");
            Assert.AreEqual(0, item.Rest);
            CollectionAssert.IsEmpty(sim.Board.Orders);
            CollectionAssert.Contains(sim.Board.LastCompleted, kind);
        }

        [Test]
        public void Recipe_Dog_PaysSupplies_AtStart()
        {
            var sim = NewSim(tune: b => b.boardCraftExtraProgress = 0);
            Unlock(sim, FacilityGear.Dog);
            Stock(sim);
            sim.Shelter.Supplies = 40;
            Order(sim, FacilityGear.Dog);
            double preview = sim.BoardFoodPreview, start = sim.Shelter.Supplies;
            ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.Board.Orders.Single().Started);
            Assert.AreEqual(start + preview - 4, sim.Shelter.Supplies, 1e-9, "erzak bedeli emir başlarken düşer");
            Assert.AreEqual(preview - 4, sim.Board.LastSuppliesChange, 1e-9);
            Assert.AreEqual(sim.Board.LastSuppliesChange, sim.Shelter.LastSuppliesChange);
            // Sonraki kapanışlarda bedel yeniden ödenmez
            preview = sim.BoardFoodPreview; start = sim.Shelter.Supplies;
            ShelterEventKit.Close(sim);
            Assert.AreEqual(start + preview, sim.Shelter.Supplies, 1e-9);
        }

        [Test]
        public void Recipe_Dog_WaitsWithoutSupplies()
        {
            var sim = NewSim(tune: b => b.boardCraftExtraProgress = 0);
            Unlock(sim, FacilityGear.Dog);
            Stock(sim);
            Order(sim, FacilityGear.Dog);
            sim.Shelter.Supplies = 40;
            double balance = sim.BoardFoodPreview;
            sim.Shelter.Supplies = 2 - balance;   // kapanıştan sonra 2 erzak: aç değil, bedele yetmez
            int cloth = Qty(sim, "kumas");
            ShelterEventKit.Close(sim);
            Assert.IsFalse(sim.Shelter.Starving);
            Assert.AreEqual(2, sim.Shelter.Supplies, 1e-9);
            var order = sim.Board.Orders.Single();
            Assert.IsFalse(order.Started, "erzak yetmezse emir bekler");
            Assert.AreEqual(3, order.Remaining);
            Assert.AreEqual(cloth, Qty(sim, "kumas"), "hammadde de düşmez");
            sim.Shelter.Supplies = 40;
            ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.Board.Orders.Single().Started);
            Assert.AreEqual(cloth - 1, Qty(sim, "kumas"));
        }

        // ---------- Otomatik yol, eksik kit, depo ----------
        [Test]
        public void AutoPath_IgnoresRewardRecipes()
        {
            var sim = NewSim();
            Unlock(sim, FacilityGear.Wire);
            Unlock(sim, FacilityGear.Winch);
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            Assert.IsTrue(sim.StageProductionMaterial("metal", 2).Ok);
            Assert.IsTrue(sim.StageProductionMaterial("kumas", 1).Ok);
            Assert.AreEqual("armor", sim.AutomaticProductionRecipe.Id, "örme ve vinç zırhla aynı girdiyi ister; otomatik yol yalnız eski altıyı tanır");
            int armor = Count(sim, "armor");
            ShelterEventKit.Close(sim);
            Assert.AreEqual(armor + 1, Count(sim, "armor"));
            Assert.AreEqual(0, Count(sim, FacilityGear.Wire));
            Assert.AreEqual(0, Count(sim, FacilityGear.Winch));
        }

        static void LoseKit(Simulation sim, string kind)
        {
            foreach (var kit in sim.Board.Equipment.Where(e => e.Kind == kind).ToList())
                Assert.IsTrue(sim.ScrapEquipment(kit.Id).Ok);
            Assert.IsTrue(sim.BoardKitMissing(kind));
        }

        [Test]
        public void KitWhileOrder_Facility_ProducesKit_OrderPauses()
        {
            var sim = NewSim();
            Stock(sim);
            LoseKit(sim, "kit_ana");
            Order(sim, "emp_cell");
            int metal = Qty(sim, "metal"), devre = Qty(sim, "devre");
            var kit = sim.BoardRecipes.Single(r => r.Id == "kit_ana");
            ShelterEventKit.Close(sim);
            Assert.AreEqual(1, Count(sim, "kit_ana"), "emir açıkken eksik kit üretilir");
            Assert.IsNull(sim.Board.Equipment.Single(e => e.Kind == "kit_ana").OwnerId);
            var order = sim.Board.Orders.Single();
            Assert.IsFalse(order.Started, "emir o kapanış ilerlemez");
            Assert.AreEqual(sim.BoardRecipes.Single(r => r.Id == "emp_cell").Turns, order.Remaining);
            Assert.AreEqual(metal - kit.Inputs.Where(i => i.Kind == "metal").Sum(i => i.Quantity), Qty(sim, "metal"));
            Assert.AreEqual(devre - kit.Inputs.Where(i => i.Kind == "devre").Sum(i => i.Quantity), Qty(sim, "devre"));
            Assert.AreEqual(sim.BoardRecipes.Single(r => r.Id == "emp_cell").Inputs.Sum(i => i.Quantity), order.Reserves.Sum(r => r.Quantity), "ayrılmış girdi yerinde");
            ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.Board.Orders.Single().Started, "kit tamamlanınca emir sürer");
        }

        [Test]
        public void KitWhileOrder_UsesStagedFirst_ThenFree()
        {
            var sim = NewSim(tune: b => b.boardMissingKitCircuits = 1);
            Stock(sim);
            LoseKit(sim, "kit_tek");
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            Assert.IsTrue(sim.StageProductionMaterial("metal", 1).Ok);
            Order(sim, "emp_cell");
            ShelterEventKit.Close(sim);
            Assert.AreEqual(1, Count(sim, "kit_tek"));
            Assert.AreEqual(0, sim.Board.ProductionInputs.Sum(m => m.Quantity), "üretim alanındaki metal önce kullanılır");
        }

        [Test]
        public void Depot_CapStopsProduction()
        {
            var sim = NewSim();
            Stock(sim);
            LoseKit(sim, "kit_ana");
            Order(sim, "emp_cell");
            // Kişiye bağlı eşya tavana sayılmaz; tavanın son boş yeri eksik kite gider, emir ilerlemez
            sim.Board.Equipment.Add(new BoardEquipment { Id = "bound-fixture", Kind = "armor", Bound = true });
            while (sim.BoardDepotCount < sim.B.boardEquipmentCapacity - 1)
                sim.Board.Equipment.Add(new BoardEquipment { Id = "fixture-" + sim.Board.Equipment.Count, Kind = "medkit" });
            int metal = Qty(sim, "metal");
            ShelterEventKit.Close(sim);
            Assert.AreEqual(1, Count(sim, "kit_ana"));
            Assert.AreEqual(sim.B.boardEquipmentCapacity, sim.BoardDepotCount);
            Assert.IsFalse(sim.Board.Orders.Single().Started);
            Assert.AreEqual(metal - sim.B.boardKitMetal, Qty(sim, "metal"));
            Assert.AreEqual(Loc.T("Teçhizat deposu dolu. Üretimden önce yer aç."), sim.BoardPlanCheck(), "depo doluyken emirli plan basılmaz");
        }

        // ---------- Kadroya ekipman ----------
        [Test]
        public void Squad_GearFromUtility()
        {
            var sim = NewSim();
            ShelterEventKit.FillSquad(sim);
            var people = sim.PlanningSquad().Select(x => x.PersonId).ToList();
            Assert.IsNotNull(sim.AddBoardEquipment(FacilityGear.Charge, people[0]));
            Assert.IsNotNull(sim.AddBoardEquipment(FacilityGear.Dog, people[1]));
            Assert.IsNotNull(sim.AddBoardEquipment("medkit", people[2]));
            var squad = sim.PlanningSquad();
            Assert.AreEqual(FacilityGear.Charge, squad[0].Gear);
            Assert.AreEqual(sim.B.facilityChargeCharges, squad[0].GearCharges);
            Assert.AreEqual(FacilityGear.Dog, squad[1].Gear);
            Assert.AreEqual(-1, squad[1].GearCharges, "köpek sürekli");
            Assert.IsNull(squad[2].Gear, "bakım kiti ekipman değil");
            Assert.AreEqual(0, squad[2].GearCharges);
            CollectionAssert.AreEqual(squad.Select(x => x.Gear), sim.LiberationSquadPreview().Squad.Select(x => x.Gear), "görev fişi aynı hakkı gösterir");
            ShelterEventKit.StartRaid(sim);
            Assert.AreEqual(FacilityGear.Charge, sim.S.Raid.Params.Squad[0].Gear);
            Assert.AreEqual(1, sim.Board.Equipment.Count(e => e.Kind == FacilityGear.Charge), "eşya panoda kalır (hak baskında dolar)");
        }

        [Test]
        public void Squad_RestingDog_NotCarried()
        {
            var sim = NewSim();
            ShelterEventKit.FillSquad(sim);
            var dog = sim.AddBoardEquipment(FacilityGear.Dog, sim.PlanningSquad()[0].PersonId);
            dog.Rest = 2;
            Assert.IsNull(sim.PlanningSquad()[0].Gear, "dinlenen köpek baskına gitmez");
            dog.Rest = 0;
            Assert.AreEqual(FacilityGear.Dog, sim.PlanningSquad()[0].Gear);
        }

        [Test]
        public void Dog_ScrapRejected_LegacyRecordedScrapRestoresItsMetal()
        {
            var sim = SimAt(79, "jk", b =>
            {
                b.boardStartSupplies = b.boardSuppliesCapacity = 150;
                b.boardMaterialsCapacity = 200;
            });
            CompleteRegion(sim);
            Assert.IsTrue(ChooseReward(sim));
            ShelterEventKit.Close(sim);
            Order(sim, FacilityGear.Dog);
            int shifts = sim.BoardRecipes.Single(r => r.Id == FacilityGear.Dog).Turns;
            for (int i = 0; i < shifts; i++) ShelterEventKit.Close(sim);
            var dog = sim.Board.Equipment.Single(e => e.Kind == FacilityGear.Dog);
            int metal = Qty(sim, "metal"), commands = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;
            Assert.AreEqual(Loc.T("Köpek hurdaya atılmaz."), sim.ScrapEquipmentCheck(dog.Id));
            var result = sim.ScrapEquipment(dog.Id);
            Assert.IsFalse(result.Ok);
            Assert.AreEqual(sim.ScrapEquipmentCheck(dog.Id), result.Msg);
            Assert.AreSame(dog, sim.Board.EquipmentItem(dog.Id));
            Assert.AreEqual(metal, Qty(sim, "metal"));
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(rng, sim.S.Rng.State);

            // Aşama 15'te aynı komut dizisinin sonunda başarıyla kaydedilebilen hurda.
            sim.Record.day = sim.S.Day;
            var previous = new SavedCommand { day = sim.S.Day, kind = "board_scrap", a = dog.Id };
            sim.Record.commands.Add(previous);
            var restored = Replay.Restore(sim.Record);
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.IsNull(restored.Board.EquipmentItem(dog.Id));
            Assert.AreEqual(metal + sim.B.boardScrapMetal, Qty(restored, "metal"));
            Assert.AreEqual(rng, restored.S.Rng.State);
            sim.Record.commands.Add(previous);
            var twice = Replay.Restore(sim.Record);
            Assert.AreEqual(1, twice.ReplayFailures);
            Assert.AreEqual(Qty(restored, "metal"), Qty(twice, "metal"));
        }

        [Test]
        public void Dog_RestCountsDown()
        {
            var sim = NewSim();
            var dog = sim.AddBoardEquipment(FacilityGear.Dog, null);
            dog.Rest = 3;
            for (int left = 2; left >= 0; left--)
            {
                ShelterEventKit.Close(sim);
                Assert.AreEqual(left, sim.Board.EquipmentItem(dog.Id).Rest);
            }
            ShelterEventKit.Close(sim);
            Assert.AreEqual(0, sim.Board.EquipmentItem(dog.Id).Rest, "sıfırın altına inmez");
        }

        [Test]
        public void Dog_OwnerDies_ReturnsToDepot()
        {
            string owner = null, dogId = null;
            var sim = Sim(before: s =>
            {
                owner = s.PlanningSquad()[0].PersonId;
                dogId = s.AddBoardEquipment(FacilityGear.Dog, owner).Id;
            });
            Assert.AreEqual(FacilityGear.Dog, sim.S.Raid.Params.Squad[0].Gear);
            FacilityRewardTests.HandRaid(sim, extracted: false);
            Assert.IsFalse(sim.Board.Person(owner).Alive, "çıkarılmayan düşen ölür");
            var dog = sim.Board.EquipmentItem(dogId);
            Assert.IsNotNull(dog, "köpek ölmez");
            Assert.IsNull(dog.OwnerId, "depoya döner");
            Assert.IsFalse(sim.Board.Equipment.Any(e => e.OwnerId == owner), "öbür eşyası kaybolur");
        }
    }
}
