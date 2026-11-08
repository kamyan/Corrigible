using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>İsimli kişi/eşya sahipliği, vardiya kilidi, gerçek üretim ve eğitim için deterministik regresyonlar.</summary>
    public class PlanningBoardTests
    {
        static Simulation Ready(uint seed = 79) => Simulation.NewGame(seed, new Balance
        {
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
        });

        static void Close(Simulation sim)
        {
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            string receipt = sim.Board.ReceiptId;
            Assert.IsTrue(sim.FileShift(receipt).Ok);
        }

        static void ReserveRecipe(Simulation sim, string recipeId)
        {
            Assert.IsTrue(sim.QueueProduction(recipeId).Ok);
            foreach (var input in sim.BoardRecipes.Single(r => r.Id == recipeId).Inputs)
                Assert.IsTrue(sim.ReserveProductionMaterial(input.Kind, input.Quantity).Ok);
        }

        [Test]
        public void AutomaticProduction_PartialInputsStayAndProduceOnlyAfterACompleteRecipe()
        {
            var sim=Ready();Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            int metal=sim.Board.Material("metal").Quantity, cloth=sim.Board.Material("kumas").Quantity, count=sim.Board.Equipment.Count;
            Assert.IsTrue(sim.StageProductionMaterial("metal",2).Ok);Close(sim);
            Assert.AreEqual(metal,sim.Board.Material("metal").Quantity);Assert.AreEqual(count,sim.Board.Equipment.Count);
            Assert.IsTrue(sim.StageProductionMaterial("kumas",1).Ok);Close(sim);
            Assert.AreEqual(count+1,sim.Board.Equipment.Count);Assert.AreEqual(metal-2,sim.Board.Material("metal").Quantity);Assert.AreEqual(cloth-1,sim.Board.Material("kumas").Quantity);
            Assert.AreEqual(0,sim.Board.ProductionInputs.Sum(m=>m.Quantity));
        }

        [Test]
        public void AutomaticProduction_PrefersMissingFieldKitAndReplaysItsInputs()
        {
            var sim=Ready();Assert.IsTrue(sim.ScrapEquipment(sim.Board.Equipment.First(e=>e.Kind=="kit_ana").Id).Ok);
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);Assert.IsTrue(sim.StageProductionMaterial("metal",1).Ok);Assert.IsTrue(sim.StageProductionMaterial("devre",1).Ok);
            Assert.AreEqual("kit_ana",sim.AutomaticProductionRecipe.Id);Close(sim);
            Assert.IsTrue(sim.Board.Equipment.Any(e=>e.Kind=="kit_ana"));Assert.IsFalse(sim.Board.Equipment.Any(e=>e.Kind=="emp_cell"));
            sim.Record.day=sim.S.Day;var restored=Replay.Restore(sim.Record);
            Assert.IsTrue(restored.Board.AutomaticProduction);Assert.AreEqual(sim.S.Rng.State,restored.S.Rng.State);
            CollectionAssert.AreEqual(sim.Board.Materials.Select(m=>m.Quantity),restored.Board.Materials.Select(m=>m.Quantity));
            CollectionAssert.AreEqual(sim.Board.ProductionInputs.Select(m=>m.Quantity),restored.Board.ProductionInputs.Select(m=>m.Quantity));
            CollectionAssert.AreEqual(sim.Board.Equipment.Select(e=>e.Id+e.Kind),restored.Board.Equipment.Select(e=>e.Id+e.Kind));
        }

        [Test]
        public void AutomaticProduction_WithoutProducerOrStorageSpaceSpendsNothing()
        {
            var sim=Ready();sim.EnableAutomaticProduction();sim.StageProductionMaterial("metal",2);sim.StageProductionMaterial("kumas",1);
            var producer=sim.Board.People.First(p=>p.Job=="craft");sim.AssignResident(producer.Id,"rest");int metal=sim.Board.Material("metal").Quantity;Close(sim);
            Assert.AreEqual(metal,sim.Board.Material("metal").Quantity);Assert.AreEqual(3,sim.Board.ProductionInputs.Sum(m=>m.Quantity));
            sim.AssignResident(producer.Id,"craft");
            while(sim.Board.Equipment.Count<sim.B.boardEquipmentCapacity)sim.Board.Equipment.Add(new BoardEquipment{Id="fixture-"+sim.Board.Equipment.Count,Kind="armor"});
            Close(sim);Assert.AreEqual(metal,sim.Board.Material("metal").Quantity);Assert.AreEqual(3,sim.Board.ProductionInputs.Sum(m=>m.Quantity));
        }

        [Test]
        public void AutomaticProduction_ReservationCanReturnToDepotAndCannotExceedStockOrChangeLockedPlan()
        {
            var sim=Ready();sim.EnableAutomaticProduction();int metal=sim.BoardMaterialFree("metal");sim.StageProductionMaterial("metal",1);
            Assert.AreEqual(metal-1,sim.BoardMaterialFree("metal"));Assert.IsTrue(sim.StageProductionMaterial("metal",0).Ok);Assert.AreEqual(metal,sim.BoardMaterialFree("metal"));
            int commands=sim.Record.commands.Count;uint rng=sim.S.Rng.State;
            Assert.IsFalse(sim.StageProductionMaterial("metal",metal+1).Ok);Assert.AreEqual(commands,sim.Record.commands.Count);Assert.AreEqual(rng,sim.S.Rng.State);
            Assert.IsTrue(sim.PrintShift().Ok);commands=sim.Record.commands.Count;Assert.IsFalse(sim.StageProductionMaterial("metal",1).Ok);Assert.AreEqual(commands,sim.Record.commands.Count);
        }

        [Test]
        public void NamedCommunity_CanCompleteAllRegionsWithDefaultEconomy_AndReplayTheVictory()
        {
            var sim = Simulation.NewGame(79);
            for (int region = 0; region < GameData.Regions.Length; region++)
            {
                string current = sim.S.Liberation.CurrentRegion;
                while (!sim.LiberationRegion(current).Completed)
                {
                    var sector = sim.NextLiberationSector(current);
                    var launched = sim.StartLiberationRaid(sector.Id);
                    Assert.IsTrue(launched.Ok, launched.Msg);
                    Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
                    if (sim.S.Ending == null) Close(sim);
                }
                if (sim.S.Ending == null)
                {
                    Assert.IsNotEmpty(sim.LiberationFrontier);
                    Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
                }
            }
            Assert.AreEqual("silme", sim.S.Ending);
            Assert.AreEqual(0, sim.Occupation);
            Assert.AreEqual(8, sim.Board.People.Count(p => p.Alive));
            Assert.Greater(sim.Shelter.Supplies, 0);
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(sim.Record);
            Assert.AreEqual(sim.S.Ending, restored.S.Ending);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies);
        }

        [Test]
        public void SameSeed_CreatesEightDistinctKnownResidentsAndCorrelatedProfiles()
        {
            var sim = Ready();
            var same = Ready();
            Assert.AreEqual(8, sim.Board.People.Count);
            Assert.AreEqual(8, sim.Board.People.Select(p => p.Id).Distinct().Count());
            Assert.AreEqual(8, sim.Board.People.Select(p => p.Name).Distinct().Count());
            CollectionAssert.AreEqual(sim.Board.People.Select(p => p.Name), same.Board.People.Select(p => p.Name));
            CollectionAssert.AreEqual(sim.Board.People.Select(p => p.Hp), same.Board.People.Select(p => p.Hp));
            CollectionAssert.AreEqual(sim.Board.People.Select(p => p.Move), same.Board.People.Select(p => p.Move));
            Assert.IsTrue(sim.Board.People.All(p => p.Alive && !p.Restricted && p.Hp == p.MaxHp));
            Assert.IsTrue(sim.Board.People.All(p => p.MaxHp >= 2 && p.Move >= 2 && p.Power >= 1));
            Assert.AreEqual(sim.S.Rng.State, same.S.Rng.State);
            Assert.AreEqual(0, sim.Record.commands.Count);
        }

        [Test]
        public void Assignment_SwapsExactlyTwoPeopleAndInvalidDropDoesNotMutateOrConsumeRandomness()
        {
            var sim = Ready();
            var producer = sim.Board.People.Single(p => p.Job == "craft");
            var emp = sim.Board.People.Single(p => p.Job == "emp");
            uint rng = sim.S.Rng.State;
            Assert.IsTrue(sim.AssignResident(producer.Id, "emp").Ok);
            Assert.AreEqual("emp", producer.Job);
            Assert.AreEqual("craft", emp.Job);
            Assert.AreEqual(1, sim.Board.People.Count(p => p.Job == "emp"));
            Assert.AreEqual(1, sim.Board.People.Count(p => p.Job == "craft"));
            int commands = sim.Record.commands.Count;
            Assert.IsFalse(sim.AssignResident(producer.Id, "missing").Ok);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual("emp", producer.Job);
            Assert.AreEqual(rng, sim.S.Rng.State);
            producer.Down = 1;
            Assert.IsFalse(sim.AssignResident(producer.Id, "cook").Ok);
            Assert.AreEqual("emp", producer.Job);
        }

        [Test]
        public void Equipment_TransfersOnePhysicalItemAndSwapsOccupiedPocketWithoutDuplication()
        {
            var sim = Ready();
            var first = sim.Board.People[0];
            var second = sim.Board.People[1];
            var firstKit = sim.Board.Equipment.Single(e => e.OwnerId == first.Id && e.Kind.StartsWith("kit_"));
            var secondKit = sim.Board.Equipment.Single(e => e.OwnerId == second.Id && e.Kind.StartsWith("kit_"));
            int count = sim.Board.Equipment.Count;
            Assert.IsTrue(sim.EquipResident(firstKit.Id, second.Id).Ok);
            Assert.AreEqual(second.Id, firstKit.OwnerId);
            Assert.AreEqual(first.Id, secondKit.OwnerId);
            Assert.AreEqual(count, sim.Board.Equipment.Count);
            Assert.AreEqual(count, sim.Board.Equipment.Select(e => e.Id).Distinct().Count());
            Assert.IsTrue(sim.EquipResident(firstKit.Id, null).Ok);
            Assert.IsNull(firstKit.OwnerId);
            Assert.IsFalse(sim.EquipResident(firstKit.Id, null).Ok);
        }

        [Test]
        public void Scrapping_RemovesOneOwnedItemReturnsMetalOnceAndReplays()
        {
            var sim = Ready();
            var plate = sim.Board.Equipment.First(e => e.Kind == "armor");
            string id = plate.Id;
            int metal = sim.Board.Material("metal").Quantity;
            int count = sim.Board.Equipment.Count;
            Assert.IsTrue(sim.ScrapEquipment(id).Ok);
            Assert.IsNull(sim.Board.EquipmentItem(id));
            Assert.AreEqual(count - 1, sim.Board.Equipment.Count);
            Assert.AreEqual(metal + sim.B.boardScrapMetal, sim.Board.Material("metal").Quantity);
            Assert.IsFalse(sim.ScrapEquipment(id).Ok);
            var restored = Replay.Restore(sim.Record);
            Assert.IsNull(restored.Board.EquipmentItem(id));
            Assert.AreEqual(sim.Board.Material("metal").Quantity, restored.Board.Material("metal").Quantity);
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.IsFalse(sim.ScrapEquipment(sim.Board.Equipment.First().Id).Ok);
        }

        [Test]
        public void Production_ReservesWithoutSpending_PaysOnlyFirstWorkingShift_AndCanChangeWorker()
        {
            var sim = Ready();
            int metal = sim.Board.Material("metal").Quantity;
            int circuits = sim.Board.Material("devre").Quantity;
            ReserveRecipe(sim, "emp_cell");
            Assert.AreEqual(metal, sim.Board.Material("metal").Quantity);
            Assert.AreEqual(metal - sim.B.boardEmpMetal, sim.BoardMaterialFree("metal"));
            var order = sim.Board.Orders.Single();
            var producer = sim.Board.People.Single(p => p.Job == "craft");
            Close(sim);
            Assert.IsTrue(order.Started);
            Assert.AreEqual(1, order.Remaining);
            Assert.AreEqual(metal - sim.B.boardEmpMetal, sim.Board.Material("metal").Quantity);
            Assert.AreEqual(circuits - sim.B.boardEmpCircuits, sim.Board.Material("devre").Quantity);
            Assert.IsTrue(sim.AssignResident(producer.Id, "rest").Ok);
            Close(sim);
            Assert.AreEqual(1, order.Remaining, "İşçisiz iş durur; ilerleme kişiye bağlı değildir.");
            var replacement = sim.Board.People.Single(p => p.Job == "gather");
            Assert.IsTrue(sim.AssignResident(replacement.Id, "craft").Ok);
            Close(sim);
            Assert.AreEqual(0, sim.Board.Orders.Count);
            Assert.AreEqual(1, sim.Board.Equipment.Count(e => e.Kind == "emp_cell"));
            Assert.AreEqual(metal - sim.B.boardEmpMetal, sim.Board.Material("metal").Quantity, "İkinci iş vardiyası tekrar ödeme yapmaz.");
            Assert.AreEqual(circuits - sim.B.boardEmpCircuits, sim.Board.Material("devre").Quantity);
        }

        [Test]
        public void ExperiencedProducer_CompletesTwoShiftWorkInOneShiftWithoutDiscountingInputs()
        {
            var sim = Ready();
            var producer = sim.Board.People.Single(p => p.Job == "craft");
            producer.Experience.Single(e => e.Subject == "craft").Points = sim.B.boardExperiencedAt;
            int metal = sim.Board.Material("metal").Quantity;
            ReserveRecipe(sim, "emp_cell");
            Close(sim);
            Assert.AreEqual(0, sim.Board.Orders.Count);
            Assert.AreEqual(1, sim.Board.Equipment.Count(e => e.Kind == "emp_cell"));
            Assert.AreEqual(metal - sim.B.boardEmpMetal, sim.Board.Material("metal").Quantity);
        }

        [Test]
        public void ActiveContact_BringsArrivalForwardOneShiftComparedWithIdleResident()
        {
            var balance = new Balance { admissionKnockMin = 3, admissionKnockMax = 3, liberationAlarmPerShift = 0 };
            var active = Simulation.NewGame(79, balance);
            var idle = Simulation.NewGame(79, balance);
            var worker = active.Board.People.Single(p => p.Job == "rest");
            Assert.IsTrue(active.AssignResident(worker.Id, "contact").Ok);
            Close(active); Close(idle);
            Assert.IsNull(active.Admission.Candidate);
            Close(active); Close(idle);
            Assert.IsNotNull(active.Admission.Candidate);
            Assert.IsNull(idle.Admission.Candidate);
            Assert.IsTrue(active.Admission.Candidate.ArrivalShift < idle.Admission.NextArrivalShift);
        }

        [Test]
        public void Cancellation_ReleasesDraftReservationButDoesNotRefundStartedWork()
        {
            var sim = Ready();
            int metal = sim.Board.Material("metal").Quantity;
            ReserveRecipe(sim, "emp_cell");
            Assert.IsTrue(sim.CancelProduction(sim.Board.Orders.Single().Id).Ok);
            Assert.AreEqual(metal, sim.BoardMaterialFree("metal"));
            ReserveRecipe(sim, "emp_cell");
            Close(sim);
            Assert.IsTrue(sim.CancelProduction(sim.Board.Orders.Single().Id).Ok);
            Assert.AreEqual(metal - sim.B.boardEmpMetal, sim.BoardMaterialFree("metal"));
            Assert.AreEqual(0, sim.Board.Equipment.Count(e => e.Kind == "emp_cell"));
        }

        [Test]
        public void Training_IsOneExclusiveShift_PersistsEachField_AndRejectsUselessExpertLesson()
        {
            var sim = Ready();
            var producer = sim.Board.People.Single(p => p.Job == "craft");
            int craft = producer.Skill("craft");
            ReserveRecipe(sim, "armor");
            Assert.IsTrue(sim.TrainResident(producer.Id, "ana").Ok);
            Close(sim);
            Assert.AreEqual(sim.B.boardTrainingGain, producer.Skill("ana"));
            Assert.AreEqual(craft, producer.Skill("craft"), "Eğitim ve iş deneyimi aynı kişiye aynı vardiyada verilmez.");
            Assert.AreEqual("rest", producer.Job);
            Assert.IsFalse(sim.Board.Orders.Single().Started);
            Assert.IsTrue(sim.TrainResident(producer.Id, "cook").Ok);
            Close(sim);
            Assert.AreEqual(sim.B.boardTrainingGain, producer.Skill("ana"));
            Assert.AreEqual(sim.B.boardTrainingGain, producer.Skill("cook"));
            producer.Experience.Add(new ResidentSkill { Subject = "tek", Points = sim.B.boardExpertAt });
            Assert.IsFalse(sim.TrainResident(producer.Id, "tek").Ok);
            Assert.AreEqual("rest", producer.Job);
        }

        [Test]
        public void EmptyProductionAndEmptyTriage_DoNotCreateExperienceOrConsumeMedkit()
        {
            var sim = Ready();
            var producer = sim.Board.People.Single(p => p.Job == "craft");
            var carer = sim.Board.People.Single(p => p.Job == "care");
            int craft = producer.Skill("craft"), care = carer.Skill("care");
            int medkits = sim.Board.Equipment.Count(e => e.Kind == "medkit");
            Close(sim);
            Assert.AreEqual(craft, producer.Skill("craft"));
            Assert.AreEqual(care, carer.Skill("care"));
            Assert.AreEqual(medkits, sim.Board.Equipment.Count(e => e.Kind == "medkit"));
        }

        [Test]
        public void Receipt_IsPersistentPlanLock_AndOnlyCurrentReceiptAdvancesExactlyOnce()
        {
            var sim = Ready();
            int day = sim.S.Day;
            uint rng = sim.S.Rng.State;
            sim.Step();
            Assert.AreEqual(day, sim.S.Day, "Fiş kutuya bırakılmadan zaman geçmez.");
            Assert.IsTrue(sim.PrintShift().Ok);
            string receipt = sim.Board.ReceiptId;
            Assert.IsTrue(sim.Board.Locked);
            Assert.IsFalse(sim.AssignResident(sim.Board.People[0].Id, "rest").Ok);
            Assert.IsFalse(sim.PrintShift().Ok);
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.IsFalse(sim.FileShift("eski-fis").Ok);
            Assert.IsTrue(sim.FileShift(receipt).Ok);
            Assert.AreEqual(day + 1, sim.S.Day);
            Assert.IsFalse(sim.Board.Locked);
            Assert.IsFalse(sim.Board.Printed);
            Assert.IsFalse(sim.FileShift(receipt).Ok);
            Assert.AreEqual(day + 1, sim.S.Day);
        }

        [Test]
        public void Replay_RestoresPendingReceiptAndStartedProduction_ThenContinuesIdentically()
        {
            var sim = Ready();
            var trainee = sim.Board.People.Single(p => p.Job == "rest");
            Assert.IsTrue(sim.TrainResident(trainee.Id, "craft").Ok);
            ReserveRecipe(sim, "emp_cell");
            Close(sim);
            Assert.IsTrue(sim.PrintShift().Ok);
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(sim.Record);
            Assert.AreEqual(sim.Board.ReceiptId, restored.Board.ReceiptId);
            Assert.AreEqual(sim.Board.ReceiptText, restored.Board.ReceiptText);
            Assert.IsTrue(restored.Board.Locked);
            Assert.IsTrue(restored.Board.Printed);
            Assert.AreEqual(sim.Board.Orders.Single().Remaining, restored.Board.Orders.Single().Remaining);
            Assert.AreEqual(sim.Board.Material("metal").Quantity, restored.Board.Material("metal").Quantity);
            Assert.AreEqual(sim.Board.Person(trainee.Id).Skill("craft"), restored.Board.Person(trainee.Id).Skill("craft"));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            string receipt = sim.Board.ReceiptId;
            Assert.IsTrue(sim.FileShift(receipt).Ok);
            Assert.IsTrue(restored.FileShift(receipt).Ok);
            Assert.AreEqual(sim.S.Day, restored.S.Day);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies);
            CollectionAssert.AreEqual(sim.Board.Equipment.Select(e => e.Id + ":" + e.Kind + ":" + e.OwnerId), restored.Board.Equipment.Select(e => e.Id + ":" + e.Kind + ":" + e.OwnerId));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
        }

        [Test]
        public void TacticalPreviewAndActualBoard_UseTheSameIndividualsAndEffectiveBodyValues()
        {
            var sim = Ready();
            var person = sim.Board.People.Single(p => p.Job == "tek");
            person.Hp = person.MaxHp - 1;
            person.Experience.Single(e => e.Subject == "tek").Points = sim.B.boardExperiencedAt;
            var preview = sim.PlanningSquad();
            Assert.AreEqual(person.Id, preview[0].PersonId);
            Assert.AreEqual(person.Hp, preview[0].Hp);
            Assert.AreEqual(person.MaxHp, preview[0].MaxHp);
            Assert.AreEqual(System.Math.Min(sim.B.boardMaxFieldMove, person.Move + sim.B.boardFieldSkillMoveBonus), preview[0].Move);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            var board = Baskin.Create(sim.S.Raid.Params);
            var actual = board.Units.Single(u => u.PersonId == person.Id);
            Assert.AreEqual(preview[0].Hp, actual.Hp);
            Assert.AreEqual(preview[0].MaxHp, actual.Max);
            Assert.AreEqual(preview[0].Move, actual.MoveRange);
            Assert.AreEqual(preview[0].Power, actual.Power);
            Assert.AreEqual(preview[0].PersonName, actual.PersonName);
            Assert.IsTrue(sim.Board.Locked);
            Assert.IsFalse(sim.TrainResident(sim.Board.People.Last().Id, "craft").Ok);
        }

        [Test]
        public void RaidReturn_RescuesDownedResident_ButExplicitlyLeftResidentDiesWithoutAutomaticReplacement()
        {
            var sim = Ready();
            var squad = sim.PlanningSquad();
            string rescued = squad[0].PersonId, lost = squad[2].PersonId;
            int before = sim.Board.People.Count;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult
            {
                Retreat = true, Hp = { 0, squad[1].Hp, 0 }, Armor = { 0, 1, 0 }, Left = { 2 }
            }).Ok);
            Assert.IsTrue(sim.Board.Person(rescued).Alive);
            Assert.Greater(sim.Board.Person(rescued).Down, 0);
            Assert.AreEqual("rest", sim.Board.Person(rescued).Job);
            Assert.IsFalse(sim.Board.Person(lost).Alive);
            Assert.AreEqual(0, sim.Board.Person(lost).Hp);
            Assert.AreEqual(before, sim.Board.People.Count, "Ölü kişi arşivde kalır; yeni yedek kendiliğinden üremez.");
            Assert.AreEqual(before - 1, sim.Shelter.Population);
            Assert.IsFalse(sim.Board.Equipment.Any(e => e.OwnerId == lost));
            Assert.IsFalse(sim.AssignResident(lost, "ana").Ok);
            Assert.IsTrue(sim.PrintShift().Ok, "Dönüşte yaralanma kilitlenmiş planın fişini engellemez.");
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(sim.Record);
            Assert.IsFalse(restored.Board.Person(lost).Alive);
            Assert.AreEqual(sim.Board.Person(rescued).Down, restored.Board.Person(rescued).Down);
            Assert.AreEqual(sim.Board.ReceiptText, restored.Board.ReceiptText);
        }

        [Test]
        public void WholeSquadLoss_WorkingGathererRebuildsMissingKitsAndFiveSurvivorsCanLaunchAgain_AfterReplay()
        {
            var sim = Ready();
            var lost = sim.PlanningSquad().Select(p => p.PersonId).ToArray();
            Assert.Less(sim.Board.Material("devre").Quantity, PlanningBoardData.FieldJobs.Length * sim.B.boardKitCircuits,
                "Başlangıç devre stoğu üç yeni kit için tek başına yetmez; toparlanma gerçekten ikmale dayanmalı.");
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Down = 3, Hp = { 0, 0, 0 }, Armor = { 0, 0, 0 } }).Ok);
            Assert.IsTrue(lost.All(id => !sim.Board.Person(id).Alive));
            Assert.AreEqual(5, sim.Shelter.Population);
            Assert.IsFalse(sim.Board.Equipment.Any(e => e.Kind.StartsWith("kit_")));
            Assert.IsNull(sim.S.Ending, "Topluluk yaşarken bir saha kaybı oyunu gizlice sona erdirmemeli.");
            Close(sim);

            var gatherer = sim.Board.People.Single(p => p.Alive && p.Job == "gather");
            int worked = sim.Board.GatheringProgress;
            int metal = sim.Board.Material("metal").Quantity;
            int circuits = sim.Board.Material("devre").Quantity;
            Assert.IsTrue(sim.AssignResident(gatherer.Id, "rest").Ok);
            Close(sim);
            Assert.AreEqual(worked, sim.Board.GatheringProgress, "İkmalci başka işteyken kurtarma sayacı ilerlemez.");
            Assert.AreEqual(metal, sim.Board.Material("metal").Quantity);
            Assert.AreEqual(circuits, sim.Board.Material("devre").Quantity);
            Assert.IsTrue(sim.AssignResident(gatherer.Id, "gather").Ok);

            foreach (string role in PlanningBoardData.FieldJobs)
            {
                int wait = 0;
                while (sim.BoardMaterialFree("metal") < sim.B.boardKitMetal || sim.BoardMaterialFree("devre") < sim.B.boardKitCircuits)
                {
                    Assert.Less(wait++, sim.B.boardGatherSalvageInterval * PlanningBoardData.FieldJobs.Length,
                        "Canlı ikmalci ve üretici ile temel saha kitine geri dönüş mümkün olmalı.");
                    Close(sim);
                }
                ReserveRecipe(sim, "kit_" + role);
                int production = 0;
                while (sim.Board.Orders.Count > 0)
                {
                    Assert.Less(production++, sim.B.boardKitTurns + 1);
                    Close(sim);
                }
            }
            Assert.AreEqual(3, sim.Board.Equipment.Count(e => e.Kind.StartsWith("kit_")));
            var replacements = new[]
            {
                sim.Board.People.Single(p => p.Alive && p.Job == "craft"),
                sim.Board.People.Single(p => p.Alive && p.Job == "care"),
                sim.Board.People.Single(p => p.Alive && p.Job == "rest")
            };
            for (int i = 0; i < replacements.Length; i++)
            {
                string role = PlanningBoardData.FieldJobs[i];
                Assert.IsTrue(sim.AssignResident(replacements[i].Id, role).Ok);
                Assert.IsTrue(sim.EquipResident(sim.Board.Equipment.Single(e => e.Kind == "kit_" + role).Id, replacements[i].Id).Ok);
            }
            Assert.IsNull(sim.PlanningRaidCheck());
            Assert.IsNull(sim.LiberationRaidCheck("weu_1"));
            Assert.IsFalse(sim.PlanningSquad().Any(p => lost.Contains(p.PersonId)), "Kaybedilen kişiler geri dönmez; kalan kişiler yeni görevleri üstlenir.");

            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(sim.Record);
            Assert.IsNull(restored.PlanningRaidCheck());
            Assert.IsNull(restored.LiberationRaidCheck("weu_1"));
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies);
            Assert.AreEqual(sim.Board.GatheringProgress, restored.Board.GatheringProgress);
            CollectionAssert.AreEqual(sim.Board.Materials.Select(m => m.Kind + ":" + m.Quantity), restored.Board.Materials.Select(m => m.Kind + ":" + m.Quantity));
            CollectionAssert.AreEqual(sim.Board.Equipment.Select(e => e.Id + ":" + e.Kind + ":" + e.OwnerId), restored.Board.Equipment.Select(e => e.Id + ":" + e.Kind + ":" + e.OwnerId));
            CollectionAssert.AreEqual(sim.Board.People.Select(p => p.Id + ":" + p.Alive + ":" + p.Job), restored.Board.People.Select(p => p.Id + ":" + p.Alive + ":" + p.Job));
        }

        [Test]
        public void Loot_GrantsTypedMaterialsWithoutCreatingAnonymousPeople_AndDoesNotUnlockPlan()
        {
            var sim = Ready();
            int people = sim.Board.People.Count;
            int metal = sim.Board.Material("metal").Quantity;
            int circuits = sim.Board.Material("devre").Quantity;
            int cloth = sim.Board.Material("kumas").Quantity;
            int chemicals = sim.Board.Material("kimyasal").Quantity;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.AreEqual(metal + sim.B.boardLootMetal, sim.Board.Material("metal").Quantity);
            Assert.AreEqual(circuits + sim.B.boardLootCircuits, sim.Board.Material("devre").Quantity);
            Assert.AreEqual(cloth + sim.B.boardLootCloth, sim.Board.Material("kumas").Quantity);
            Assert.AreEqual(chemicals + sim.B.boardLootChemicals, sim.Board.Material("kimyasal").Quantity);
            Assert.AreEqual(people, sim.Board.People.Count);
            Assert.AreEqual(people, sim.Shelter.Population);
            Assert.IsTrue(sim.Board.Locked);
            Assert.IsFalse(sim.QueueProduction("armor").Ok);
            Assert.IsFalse(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
        }

        [Test]
        public void EmpCell_IsConsumedOnDeparture_AndPreviewPreservesItsSingleRealDamageBonus()
        {
            var sim = Ready();
            ReserveRecipe(sim, "emp_cell");
            Close(sim); Close(sim);
            var cell = sim.Board.Equipment.Single(e => e.Kind == "emp_cell");
            var person = sim.Board.People.Single(p => p.Job == "emp");
            Assert.IsTrue(sim.EquipResident(cell.Id, person.Id).Ok);
            var preview = sim.PlanningSquad().Single(p => p.Type == "emp");
            Assert.AreEqual(sim.B.boardEmpBoostCharges, preview.EmpBoosts);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsFalse(sim.Board.Equipment.Any(e => e.Id == cell.Id));
            var board = Baskin.Create(sim.S.Raid.Params);
            var emp = board.Units.Single(u => u.PersonId == person.Id);
            board.Units.RemoveAll(u => u.Id != emp.Id);
            board.Ev = null;
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++)
            {
                board.Cells[y, x].T = "floor";
                board.Cells[y, x].O = null;
            }
            emp.X = 0; emp.Y = 0;
            var target = new Baskin.Unit { Id = 999, Type = "dron", Side = 'e', X = 2, Y = 0, Hp = 3, Max = 3 };
            board.Units.Add(target);
            Baskin.Targets(board, emp); Baskin.Targets(board, emp);
            Assert.AreEqual(sim.B.boardEmpBoostCharges, emp.EmpBoosts, "Yasal hedefleri okumak sarf tüketmez.");
            var shot = Baskin.Preview(board, emp.Id, target.X, target.Y);
            Assert.AreEqual(1 + sim.B.boardEmpBoostDamage, shot.Fx.Single(f => f.K == "dmg" && f.Unit == target.Id).N);
            Assert.AreEqual(sim.B.boardEmpBoostCharges, emp.EmpBoosts, "Önizleme kopyada çalışır.");
            Assert.IsTrue(Baskin.DoAct(board, emp.Id, target.X, target.Y));
            Assert.AreEqual(1 + sim.B.boardEmpBoostDamage, board.FxList.Single(f => f.K == "dmg" && f.Unit == target.Id).N);
            Assert.AreEqual(sim.B.boardEmpBoostCharges - 1, emp.EmpBoosts);
            Assert.AreEqual(1, target.Hp);
            emp.Acted = false; emp.Moved = false;
            Assert.IsTrue(Baskin.DoAct(board, emp.Id, target.X, target.Y));
            Assert.AreEqual(1, board.FxList.Single(f => f.K == "dmg" && f.Unit == target.Id).N, "Temel EMP, sarf bitince çalışmaya devam eder.");
        }
    }
}
