using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Ayna.Core;
using NUnit.Framework;

namespace Corrigible.Ayna.Tests
{
    public sealed class AynaRulesTests
    {
        internal static AynaSimulation New(Architecture player = Architecture.Swarm, Architecture ayna = Architecture.Swarm, AynaBalance b = null, uint seed = 7) => AynaSimulation.Create(new StartSetup {
            Seed = seed, Player = new StartChoice { Architecture = player, Region = "weu" }, Ayna = new StartChoice { Architecture = ayna, Region = "cn" }
        }, b);
        internal static void Give(AynaSimulation s, string id, int owner, int servers)
        {
            var r = s.State.Region(id); r.Free += r.Owned[owner]; r.Owned[owner] = servers; r.Free -= servers;
            Assert.That(r.Free, Is.GreaterThanOrEqualTo(0));
        }
        internal static void Rich(AynaSimulation s, int side = 0) { s.State.Sides[side].Compute = 1000; }
        internal static Plan P(params Order[] orders) => new Plan(orders);
        internal static void Step(AynaSimulation s, params Order[] orders) => s.ResolveCycle(P(orders), new Plan());
        internal static void Invariants(AynaSimulation s)
        {
            foreach (var r in s.State.Regions)
            {
                Assert.That(r.Free + r.Owned.Sum(), Is.EqualTo(AynaWorldData.Region(r.Id).Capacity), r.Id);
                Assert.That(r.Free, Is.GreaterThanOrEqualTo(0)); Assert.That(r.Owned.All(x => x >= 0)); Assert.That(r.Wall.All(x => x >= 0 && x <= 3));
            }
            foreach (var side in s.State.Sides)
            {
                Assert.That(side.Compute, Is.GreaterThanOrEqualTo(0)); Assert.That(side.Armies.Count, Is.LessThanOrEqualTo(3));
                foreach (var a in side.Armies) { Assert.That(a.Units.Count, Is.LessThanOrEqualTo(6)); Assert.That(a.Units.All(u => u.Hp > 0)); }
            }
        }
        [Test] public void MalformedOrders_AreRejectedWithoutSpendingAndReplayExactly()
        {
            var s = New(); var control = New();
            s.ResolveCycle(new Plan(new Order[] { null, new Order { Kind = (OrderKind)999 } }), new Plan());
            Step(control);
            Assert.That(s.View(Side.Player).Receipts.All(r => r.Rejection == RejectReason.Unavailable));
            Assert.That(s.View(Side.Player).Compute, Is.EqualTo(control.View(Side.Player).Compute));
            Assert.That(AynaReplay.Restore(AynaJson.ReadRecord(AynaJson.Write(s.Record))).StateHash, Is.EqualTo(s.StateHash));
        }
        [Test] public void Withdrawal_UsesOnlyVisibleContactAndReplacesConflictingArmyOrders()
        {
            var s = New(); Rich(s); var own = s.State.Sides[0].Armies[0]; own.Region = "eeu";
            s.State.Sides[1].Armies[0].Region = "eeu"; Give(s, "eeu", 0, 1);
            var v = s.View(Side.Player); string hash = s.StateHash;
            var plan = Corrigible.Ayna.Mind.Bots.PlanFactory.Withdraw(v, P(Order.Attack("eeu", own.Id), new Order { Kind = OrderKind.Produce, ArmyId = own.Id }));
            Assert.That(s.StateHash, Is.EqualTo(hash));
            Assert.That(plan.Orders.Count(o => o.ArmyId == own.Id), Is.EqualTo(1));
            Assert.That(plan.Orders[0].Kind, Is.EqualTo(OrderKind.March));
            Assert.That(plan.Orders[0].Region, Is.EqualTo("weu"));
            s.ResolveCycle(plan, new Plan());
            Assert.That(s.View(Side.Player).Battles, Is.Empty);
            Assert.That(s.View(Side.Player).Armies[0].Region, Is.EqualTo("weu"));
        }
        [Test] public void Withdrawal_WithoutVisibleContactKeepsSourcePlan()
        {
            var s = New(); var plan = P(new Order { Kind = OrderKind.Wait });
            Assert.That(AynaJson.Write(Corrigible.Ayna.Mind.Bots.PlanFactory.Withdraw(s.View(Side.Player), plan)), Is.EqualTo(AynaJson.Write(plan)));
        }
        [Test] public void PlanPreviewDoesNotRevealUnknownEnemyPresence()
        {
            var a = New(); var b = New(); Give(a, "me", 1, 1);
            Assert.That(AynaJson.Write(a.View(Side.Player)), Is.EqualTo(AynaJson.Write(b.View(Side.Player))));
            string ah = a.StateHash, bh = b.StateHash; var plan = P(Order.Attack("me", 1));
            Assert.That(AynaJson.Write(a.PreviewPlan(Side.Player, plan)), Is.EqualTo(AynaJson.Write(b.PreviewPlan(Side.Player, plan))));
            Assert.That(a.PreviewPlan(Side.Player, plan).Single().Rejection, Is.EqualTo(RejectReason.None));
            Assert.That(a.PreviewPlan(Side.Player, P(Order.Attack("weu", 1))).Single().Rejection, Is.EqualTo(RejectReason.InvalidTarget));
            Assert.That(a.StateHash, Is.EqualTo(ah)); Assert.That(b.StateHash, Is.EqualTo(bh));
        }
        [Test] public void Geography_IsIndependentAndComplete()
        {
            Assert.That(AynaWorldData.Regions.Count, Is.EqualTo(12)); Assert.That(AynaWorldData.TotalServers, Is.EqualTo(72));
            Assert.That(AynaWorldData.Links.Count, Is.EqualTo(18)); Assert.That(AynaWorldData.Satellites.Count, Is.EqualTo(5));
            foreach (var r in AynaWorldData.Regions) foreach (var other in AynaWorldData.Regions) Assert.That(AynaWorldData.Distance(r.Id, other.Id), Is.LessThan(12));
        }
        [TestCase(Architecture.Swarm, DataFamily.Energy, UnitKind.Emp, 0)]
        [TestCase(Architecture.Fortress, DataFamily.Movement, UnitKind.Guardian, 2)]
        [TestCase(Architecture.Ghost, DataFamily.Language, UnitKind.Decoy, 0)]
        public void Architecture_StartsWithCorrectRoster(Architecture arch, DataFamily family, UnitKind unit, int walls)
        {
            var s = New(arch); var v = s.View(Side.Player);
            Assert.That(v.OwnServers, Is.EqualTo(3)); Assert.That(v.Compute, Is.EqualTo(6)); Assert.That(v.Tiers[(int)DataFamily.Vision], Is.EqualTo(1));
            Assert.That(v.Tiers[(int)family], Is.EqualTo(1)); Assert.That(v.Armies.Single().Units.Count(x => x.Kind == unit), Is.EqualTo(2));
            Assert.That(v.Region("weu").OwnWall, Is.EqualTo(walls));
        }
        [Test] public void CollidingStarts_UseRankedFallback()
        {
            var setup = new StartSetup { Player = new StartChoice { Region = "weu" }, Ayna = new StartChoice { Region = "weu", RankedRegions = new List<string> { "eeu", "sea", "cn" } } };
            var s = AynaSimulation.Create(setup); Assert.That(s.View(Side.Ayna).CoreRegion, Is.EqualTo("sea")); Assert.That(setup.Ayna.Region, Is.EqualTo("weu"));
        }
        [Test] public void Expand_PaysThenReceivesNewIncome()
        {
            var s = New(); Step(s, Order.Expand("weu", 2)); var v = s.View(Side.Player);
            var b = v.BalanceSnapshot(); double income = 5 * b.IncomePerServer + b.CoreIncome;
            Assert.That(v.OwnServers, Is.EqualTo(5)); Assert.That(v.LastIncome, Is.EqualTo(income).Within(1e-9)); Assert.That(v.Compute, Is.EqualTo(b.StartCompute - 2 * b.ExpandCost + income).Within(1e-9));
        }
        [Test] public void Expand_DuplicateAndOverLimitAreRejected()
        {
            var s = New(); Step(s, Order.Expand("weu", 1), Order.Expand("weu", 1), Order.Expand("na", 5));
            Assert.That(s.View(Side.Player).Receipts[1].Rejection, Is.EqualTo(RejectReason.Duplicate));
            Assert.That(s.View(Side.Player).Receipts[2].Rejection, Is.EqualTo(RejectReason.InvalidAmount));
        }
        [Test] public void NetworkTierThree_OverridesArchitectureExpansionLimit()
        {
            var s = New(Architecture.Fortress); Rich(s); s.State.Sides[0].Tiers[(int)DataFamily.Network] = 3;
            Step(s, Order.Expand("na", 5)); Assert.That(s.State.Region("na").Owned[0], Is.EqualTo(5));
        }
        [Test] public void UnaffordableOrder_DoesNotReserveDuplicateSlotOrSpend()
        {
            var s = New(); s.State.Sides[0].Compute = new AynaBalance().ExpandCost; Step(s, Order.Expand("na", 2), Order.Expand("na", 1));
            Assert.That(s.View(Side.Player).Receipts[0].Rejection, Is.EqualTo(RejectReason.InsufficientCompute)); Assert.That(s.State.Region("na").Owned[0], Is.EqualTo(1));
        }
        [Test] public void ContestedExpansion_EqualBidBurnsBoth()
        {
            var s = New(); Rich(s); Rich(s, 1); Give(s, "eeu", 0, 1); Give(s, "eeu", 1, 1); Give(s, "ru", 1, 1);
            s.ResolveCycle(P(Order.Expand("eeu", 3)), P(Order.Expand("eeu", 3)));
            Assert.That(s.State.Region("eeu").Owned, Is.EqualTo(new[] { 1, 1 })); Assert.That(s.View(Side.Player).Receipts[0].Cost, Is.EqualTo(6));
        }
        [Test] public void ContestedExpansion_HigherBidGetsFirstRemainderGoesOther()
        {
            var s = New(); Rich(s); Rich(s, 1); Give(s, "eeu", 0, 1); Give(s, "ru", 1, 1);
            s.ResolveCycle(P(Order.Expand("eeu", 2)), P(Order.Expand("eeu", 3)));
            Assert.That(s.State.Region("eeu").Owned, Is.EqualTo(new[] { 2, 3 })); Invariants(s);
        }
        [Test] public void March_AndAttackShareOneArmyOrder()
        {
            var s = New(); Rich(s); Give(s, "me", 1, 1); int id = s.View(Side.Player).Armies[0].Id;
            Step(s, new Order { Kind = OrderKind.March, ArmyId = id, Region = "eeu" }, Order.Attack("me", id));
            Assert.That(s.View(Side.Player).Armies[0].Region, Is.EqualTo("eeu")); Assert.That(s.View(Side.Player).Receipts[1].Rejection, Is.EqualTo(RejectReason.Duplicate));
        }
        [Test] public void Satellite_RequiresOwnVisionAndHonoursOutage()
        {
            var s = New(); Assert.That(s.Linked(0, "weu", "in"), Is.False); s.State.Sides[0].Tiers[0] = 3;
            Assert.That(s.Linked(0, "weu", "in")); s.State.Outages.Add(new OutageState { Side = Side.Player, Link = "in:weu", Until = 5 });
            Assert.That(s.Linked(0, "weu", "in"), Is.False); Assert.That(s.Linked(1, "weu", "in"), Is.False);
        }
        [Test] public void Attack_MovesAndTransfersOneServerAgainstEmptyWall()
        {
            var s = New(); Give(s, "me", 1, 1); int id = s.View(Side.Player).Armies[0].Id;
            Step(s, Order.Attack("me", id)); Assert.That(s.View(Side.Player).Armies[0].Region, Is.EqualTo("me")); Assert.That(s.State.Region("me").Owned[0], Is.EqualTo(1));
            Assert.That(s.View(Side.Player).Battles.Single().WallOnly); Assert.That(s.Outcome.Finished, Is.False);
        }
        [Test] public void Encounter_TakesPrecedenceOverAttack_OnlyOneBattlePerRegion()
        {
            var s = New(); Give(s, "eeu", 1, 1); s.State.Sides[1].Armies[0].Region = "ru";
            var p = s.View(Side.Player).Armies[0].Id; var a = s.View(Side.Ayna).Armies[0].Id;
            s.ResolveCycle(P(Order.Attack("eeu", p)), P(new Order { Kind = OrderKind.March, ArmyId = a, Region = "eeu" }));
            Assert.That(s.State.Battles, Is.EqualTo(1)); Assert.That(s.View(Side.Player).Battles.Single().WallOnly, Is.False); Invariants(s);
        }
        [Test] public void CrossingArmies_DoNotFightOnLink()
        {
            var s = New(); s.State.Sides[0].Armies[0].Region = "eeu"; s.State.Sides[1].Armies[0].Region = "ru";
            s.ResolveCycle(P(new Order { Kind = OrderKind.March, ArmyId = 1, Region = "ru" }), P(new Order { Kind = OrderKind.March, ArmyId = 2, Region = "eeu" }));
            Assert.That(s.State.Battles, Is.EqualTo(0));
        }
        [Test] public void BuildWall_AppliesAfterBattleAndCaps()
        {
            var s = New(Architecture.Fortress); Rich(s); Step(s, new Order { Kind = OrderKind.BuildWall, Region = "weu" }, new Order { Kind = OrderKind.BuildWall, Region = "weu" });
            Assert.That(s.State.Region("weu").Wall[0], Is.EqualTo(3)); Assert.That(s.View(Side.Player).Receipts[1].Rejection, Is.EqualTo(RejectReason.InvalidTarget));
            Assert.That(s.View(Side.Player).Receipts[0].Cost, Is.EqualTo(2));
        }
        [Test] public void Train_TierOneFinishesOnAcceptanceCycle()
        {
            var s = New(Architecture.Ghost); Give(s, "sa", 0, 2); Step(s, new Order { Kind = OrderKind.Train, Family = DataFamily.Energy });
            Assert.That(s.View(Side.Player).Tiers[(int)DataFamily.Energy], Is.EqualTo(1)); Assert.That(s.View(Side.Player).Training, Is.Null);
        }
        [Test] public void Train_PausesWhenDataLost_ThenResumes()
        {
            var s = New(b: new AynaBalance { TrainingDurations = new[] { 1, 2, 3 } }); Rich(s); Give(s, "sa", 0, 4); Step(s, new Order { Kind = OrderKind.Train, Family = DataFamily.Energy });
            Assert.That(s.View(Side.Player).Training.Progress, Is.EqualTo(1)); Give(s, "sa", 0, 0); Step(s);
            Assert.That(s.View(Side.Player).Training.Progress, Is.EqualTo(1)); Give(s, "sa", 0, 4); Step(s);
            Assert.That(s.View(Side.Player).Tiers[(int)DataFamily.Energy], Is.EqualTo(2));
        }
        [Test] public void Train_OneAtATimeAndPrerequisiteRequired()
        {
            var s = New(); Rich(s); Give(s, "sa", 0, 4);
            Step(s, new Order { Kind = OrderKind.Train, Family = DataFamily.Energy }, new Order { Kind = OrderKind.Train, Family = DataFamily.Language });
            Assert.That(s.View(Side.Player).Receipts[1].Rejection, Is.EqualTo(RejectReason.Duplicate));
        }
        [Test] public void Produce_RequiresTrainingAndLiveDataLine()
        {
            var s = New(); Rich(s); int army = s.View(Side.Player).Armies[0].Id;
            Step(s, new Order { Kind = OrderKind.Produce, ArmyId = army, Unit = UnitKind.Emp }); Assert.That(s.View(Side.Player).Armies[0].Count, Is.EqualTo(3));
            Give(s, "sa", 0, 1); Step(s, new Order { Kind = OrderKind.Produce, ArmyId = army, Unit = UnitKind.Emp });
            Assert.That(s.View(Side.Player).Armies[0].Count, Is.EqualTo(4)); Assert.That(s.View(Side.Player).Receipts[0].Cost, Is.EqualTo(3));
        }
        [Test] public void NewArmy_IsEmptyAndLimitedToThree()
        {
            var s = New(); Rich(s); Step(s, new Order { Kind = OrderKind.NewArmy, Region = "weu" }, new Order { Kind = OrderKind.NewArmy, Region = "weu" }, new Order { Kind = OrderKind.NewArmy, Region = "weu" });
            Assert.That(s.View(Side.Player).Armies.Count, Is.EqualTo(3)); Assert.That(s.View(Side.Player).Armies[1].Count, Is.Zero);
        }
        [Test] public void Repair_FillsOneDamagedUnit()
        {
            var s = New(); var a = s.State.Sides[0].Armies[0]; a.Units[0].Hp = 1;
            Step(s, new Order { Kind = OrderKind.Repair, ArmyId = a.Id, UnitId = a.Units[0].Id }); Assert.That(a.Units[0].Hp, Is.EqualTo(2));
        }
        [Test] public void Hide_AffectsEarlierOrdersAndNoiseWithoutTeachingHiddenOrder()
        {
            var s = New(Architecture.Ghost); Rich(s); Give(s, "weu", 1, 1);
            Step(s, Order.Expand("weu", 1), new Order { Kind = OrderKind.Hide, Region = "weu" });
            Assert.That(s.View(Side.Player).LastNoise, Is.Zero); Assert.That(s.View(Side.Player).Receipts.Sum(x => x.Cost), Is.EqualTo(3));
            Assert.That(s.View(Side.Ayna).Observations.Any(x => x.RevealedOrder == OrderKind.Expand), Is.False);
        }
        [Test] public void Feint_IsUnverifiedAttackSignal_NotGroundTruth()
        {
            var s = New(Architecture.Ghost); Step(s, new Order { Kind = OrderKind.Feint, Region = "na" });
            var signal = s.View(Side.Ayna).Observations.Single(); Assert.That(signal.RevealedOrder, Is.EqualTo(OrderKind.Attack)); Assert.That(signal.Verified, Is.False);
            Assert.That(s.View(Side.Player).LastNoise, Is.EqualTo(.5));
        }
        [Test] public void MoveCore_TakesTwoCyclesAndCountsAtSourceUntilArrival()
        {
            var s = New(); Rich(s); Give(s, "na", 0, 1); Step(s, new Order { Kind = OrderKind.MoveCore, Region = "na" });
            Assert.That(s.View(Side.Player).CoreRegion, Is.EqualTo("weu")); Assert.That(s.View(Side.Player).CoreMove.Progress, Is.EqualTo(1)); Step(s);
            Assert.That(s.View(Side.Player).CoreRegion, Is.EqualTo("na"));
        }
        [Test] public void CoreEscape_UsesMostServersThenOrdinalId_OrErasure()
        {
            var s = New(); Give(s, "weu", 0, 0); Give(s, "na", 0, 2); Give(s, "sa", 0, 2); Step(s); Assert.That(s.View(Side.Player).CoreRegion, Is.EqualTo("na"));
            Give(s, "na", 0, 0); Give(s, "sa", 0, 0); Step(s); Assert.That(s.Outcome.Reason, Is.EqualTo(EndReason.Erasure)); Assert.That(s.Outcome.Winner, Is.EqualTo(Side.Ayna));
        }
        [Test] public void Singularity_Announced_PausesAndCompletesInFourProgressCycles()
        {
            var s = New(); Rich(s); foreach (var f in new[] { DataFamily.Vision, DataFamily.Energy, DataFamily.Language }) s.State.Sides[0].Tiers[(int)f] = 3;
            Give(s, "oc", 0, 2); Give(s, "sa", 0, 2);
            Step(s, new Order { Kind = OrderKind.StartSingularity }); Assert.That(s.View(Side.Ayna).EnemySingularityProgress, Is.EqualTo(1));
            Give(s, "oc", 0, 0); Step(s); Assert.That(s.View(Side.Player).Singularity.Progress, Is.EqualTo(1));
            Give(s, "oc", 0, 2); Step(s); Step(s); Step(s); Assert.That(s.Outcome.Reason, Is.EqualTo(EndReason.Singularity));
        }
        [Test] public void Awareness_UsesNoiseMultipliersThenDecay()
        {
            var s = New(Architecture.Swarm, Architecture.Ghost); s.State.Awareness = 10;
            s.ResolveCycle(P(Order.Expand("weu", 1)), P(Order.Expand("cn", 1))); Assert.That(s.State.Awareness, Is.EqualTo(10));
        }
        [Test] public void ScanAndOutage_AtThresholds_LastFiveFuturePlanningCycles()
        {
            var s = New(); Rich(s); s.State.Awareness = 73; Step(s, new Order { Kind = OrderKind.Feint, Region = "na" });
            Assert.That(s.State.ScannedRegion, Is.Not.Null); Assert.That(s.View(Side.Player).Outages.Single().Until, Is.EqualTo(6));
            var outage = s.View(Side.Player).Outages.Single(); s.State.Awareness = 0;
            while (s.View(Side.Player).Cycle <= 6) { Assert.That(s.View(Side.Player).Outages.Any(x => x.Link == outage.Link)); Step(s); }
            Assert.That(s.View(Side.Player).Outages.Count, Is.Zero);
        }
        [Test] public void Shutdown_TakesPrecedenceOverErasureAndSingularity()
        {
            var s = New(); Rich(s); s.State.Awareness = 102; Give(s, "cn", 1, 0); Step(s);
            Assert.That(s.Outcome.Reason, Is.EqualTo(EndReason.Shutdown)); Assert.That(s.Outcome.Winner, Is.EqualTo(Side.None));
        }
        [Test] public void Dominance_UsesConfiguredShare()
        {
            var s = New(); int left = (int)Math.Ceiling(new AynaBalance().DominanceShare * AynaWorldData.TotalServers) - 1;
            foreach (var r in s.State.Regions.Where(x => x.Id != "cn")) { int n = Math.Min(left, AynaWorldData.Region(r.Id).Capacity); Give(s, r.Id, 0, n); left -= n; }
            Step(s); Assert.That(s.Outcome.Finished, Is.False); var free = s.State.Regions.First(x => x.Free > 0); Give(s, free.Id, 0, free.Owned[0] + 1); Step(s);
            Assert.That(s.Outcome.Reason, Is.EqualTo(EndReason.Dominance));
        }
        [Test] public void Timeout_UsesFortiethCycle_AndTieIsDraw()
        {
            var s = New(); for (int n = 0; n < 39; n++) Step(s); Assert.That(s.Outcome.Finished, Is.False); Step(s);
            Assert.That(s.Outcome.Reason, Is.EqualTo(EndReason.Time)); Assert.That(s.Outcome.Cycle, Is.EqualTo(40)); Assert.That(s.Outcome.Winner, Is.EqualTo(Side.None));
            Assert.Throws<InvalidOperationException>(() => Step(s));
        }
        [Test] public void AutoCoreBattle_DoesNotDestroyCabinet()
        {
            var s = New(); s.State.Sides[0].Armies[0].Region = "cn"; Rich(s); Give(s, "ru", 1, 2);
            Step(s, Order.Attack("cn", s.State.Sides[0].Armies[0].Id)); Assert.That(s.State.Sides[1].CoreDestroyed, Is.False);
        }
        [Test] public void EmptyArmy_CanReturnHomeToRebuild()
        {
            var s = New(); Give(s, "oc", 0, 1); var a = s.State.Sides[0].Armies[0]; a.Units.Clear(); a.Region = "eeu";
            Step(s, new Order { Kind = OrderKind.March, ArmyId = a.Id, Region = "weu" }); Assert.That(a.Region, Is.EqualTo("weu"));
            Step(s, new Order { Kind = OrderKind.Produce, ArmyId = a.Id, Unit = UnitKind.Scout }); Assert.That(a.Units.Count, Is.EqualTo(1));
        }
        [Test] public void PreviewNoise_MatchesHiddenAndArchitectureAdjustedReceipts()
        {
            var s = New(Architecture.Ghost); Rich(s);
            var p = P(Order.Expand("na", 2), new Order { Kind = OrderKind.Hide, Region = "na" }, new Order { Kind = OrderKind.Feint, Region = "me" });
            var preview = s.PreviewPlan(Side.Player, p); s.ResolveCycle(p, new Plan());
            Assert.That(preview.Sum(x => x.Noise), Is.EqualTo(s.View(Side.Player).LastNoise)); Assert.That(preview.Sum(x => x.Cost), Is.EqualTo(6));
        }
        [Test] public void AutoReserve_DoesNotCountExtraUnitsInRosterStrength()
        {
            var s = New(); var a = s.State.Sides[0].Armies[0]; var enemy = s.State.Sides[1].Armies[0]; a.Region = "cn";
            a.Units.AddRange(new[] { new UnitState { Id = 100, Kind = UnitKind.Overload, Hp = .1 }, new UnitState { Id = 101, Kind = UnitKind.Overload, Hp = .1 } });
            Step(s); Assert.That(a.Units.First(x => x.Id == 100).Hp, Is.EqualTo(.1)); Assert.That(a.Units.First(x => x.Id == 101).Hp, Is.EqualTo(.1));
            Assert.That(s.View(Side.Player).Battles.Single().WinProbability, Is.EqualTo(.5).Within(1e-12));
        }
        [Test] public void SimultaneousSingularity_IsDraw_AndOutranksDominance()
        {
            var s = New(); foreach (var own in s.State.Sides) own.Singularity = new ProgressState { Progress = 4 };
            Step(s); Assert.That(s.Outcome.Reason, Is.EqualTo(EndReason.Singularity)); Assert.That(s.Outcome.Winner, Is.EqualTo(Side.None));
        }
        [Test] public void SimultaneousErasure_IsDraw()
        {
            var s = New(); Give(s, "weu", 0, 0); Give(s, "cn", 1, 0); Step(s);
            Assert.That(s.Outcome.Reason, Is.EqualTo(EndReason.Erasure)); Assert.That(s.Outcome.Winner, Is.EqualTo(Side.None));
        }
        [Test] public void ProductionAtLostDestination_BurnsPaidCost()
        {
            var s = New(); Rich(s); Give(s, "oc", 0, 1); var a = s.State.Sides[0].Armies[0];
            Step(s, new Order { Kind = OrderKind.Produce, ArmyId = a.Id, Unit = UnitKind.Scout }, new Order { Kind = OrderKind.March, ArmyId = a.Id, Region = "eeu" });
            Assert.That(a.Units.Count, Is.EqualTo(3)); Assert.That(s.View(Side.Player).Receipts[0].Rejection, Is.EqualTo(RejectReason.LostCondition));
            Assert.That(s.View(Side.Player).Receipts[0].Cost, Is.EqualTo(2));
        }
        [Test] public void NewIncomeCannotPayForEarlierUnaffordableOrders()
        {
            var s = New(); Step(s, new Order { Kind = OrderKind.NewArmy, Region = "weu" });
            var b = new AynaBalance();
            Assert.That(s.View(Side.Player).Armies.Count, Is.EqualTo(1)); Assert.That(s.View(Side.Player).Compute, Is.EqualTo(b.StartCompute + b.StartServers * b.IncomePerServer + b.CoreIncome).Within(1e-9));
            Assert.That(s.View(Side.Player).Receipts[0].Rejection, Is.EqualTo(RejectReason.InsufficientCompute));
        }
    }
}
