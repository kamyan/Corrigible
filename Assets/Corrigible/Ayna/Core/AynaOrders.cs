using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Ayna.Core
{
    public sealed partial class AynaSimulation
    {
        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)] internal sealed class ValidatedOrder
        {
            internal Order Order;
            internal int Index;
            internal double Cost, Noise;
            internal RejectReason Rejection;
            internal bool Valid => Rejection == RejectReason.None;
            internal OrderReceipt Receipt => new OrderReceipt(Index, Order, Rejection, Cost, Noise);
        }
        List<ValidatedOrder> ValidatePlan(int i, Plan plan, bool preview = false)
        {
            var output = new List<ValidatedOrder>(); var own = state.Sides[i]; double cash = own.Compute;
            var used = new HashSet<string>(); int newArmies = own.Armies.Count;
            var newUnits = own.Armies.ToDictionary(x => x.Id, x => x.Units.Count);
            var wallAdded = new Dictionary<string, int>();
            foreach (var source in plan?.Orders ?? new List<Order>())
            {
                var o = source?.Copy() ?? new Order { Kind = (OrderKind)(-1) };
                var v = new ValidatedOrder { Order = o, Index = output.Count }; output.Add(v);
                var region = state.Region(o.Region); var army = own.Armies.FirstOrDefault(x => x.Id == o.ArmyId);
                // Sürümlü kimlikler ardışıktır; aynı ret kümesini kutulama/yansıma olmadan doğrula.
                if ((uint)o.Kind > (uint)OrderKind.Wait || (uint)o.Family > (uint)DataFamily.Language || (uint)o.Unit > (uint)UnitKind.PhantomOrder) { v.Rejection = RejectReason.Unavailable; continue; }
                string key = null;
                bool valid = true;
                switch (o.Kind)
                {
                    case OrderKind.Expand:
                        v.Cost = balance.ExpandCost * o.Amount; v.Noise = balance.ExpandNoise; key = "expand:" + o.Region;
                        valid = region != null && (region.Owned[i] > 0 || state.Regions.Any(x => x.Owned[i] > 0 && Linked(i, x.Id, o.Region)));
                        if (o.Amount < 1 || o.Amount > balance.ExpansionLimit(own.Architecture, own.Tiers[(int)DataFamily.Network])) v.Rejection = RejectReason.InvalidAmount;
                        break;
                    case OrderKind.March:
                    case OrderKind.Attack:
                        v.Cost = o.Kind == OrderKind.March ? balance.MarchCost : balance.AttackCost; v.Noise = o.Kind == OrderKind.Attack ? balance.AttackNoise : 0; key = "army:" + o.ArmyId;
                        valid = army != null && (o.Kind == OrderKind.March || army.Units.Count > 0) && region != null && (o.Kind == OrderKind.Attack && army.Region == o.Region || Linked(i, army.Region, o.Region));
                        // Kesin görüş dışındaki hedef önizlemede olasıdır; gerçek koşul ÇÖZ'de denetlenir.
                        if (o.Kind == OrderKind.Attack) valid &= region != null && (preview && !IsExact(i, region.Id) || region.Owned[1 - i] > 0);
                        break;
                    case OrderKind.BuildWall:
                        v.Cost = own.Architecture == Architecture.Fortress ? balance.FortressWallCost : balance.WallCost;
                        valid = region != null && region.Owned[i] > 0 && region.Wall[i] + (wallAdded.TryGetValue(o.Region ?? "", out int count) ? count : 0) < balance.MaxWall;
                        break;
                    case OrderKind.Train:
                        int tier = own.Tiers[(int)o.Family]; key = "train";
                        valid = own.Training == null && tier < 3 && state.Data(i, o.Family) >= balance.TrainingServers[Math.Min(2, tier)];
                        v.Cost = balance.TrainingCosts[Math.Min(2, tier)]; break;
                    case OrderKind.Produce:
                        var unit = AynaUnits.Get(o.Unit); v.Cost = balance.ProductionCost(own.Architecture, o.Unit);
                        valid = army != null && state.Region(army.Region).Owned[i] > 0 && own.Tiers[(int)unit.Family] >= unit.Tier && state.Data(i, unit.Family) > 0 && newUnits[army.Id] < balance.MaxUnits;
                        break;
                    case OrderKind.NewArmy:
                        v.Cost = balance.NewArmyCost; valid = region != null && region.Owned[i] > 0 && newArmies < balance.MaxArmies; break;
                    case OrderKind.Repair:
                        key = "repair:" + o.UnitId; v.Cost = balance.RepairCost;
                        var damaged = army?.Units.FirstOrDefault(x => x.Id == o.UnitId);
                        valid = damaged != null && damaged.Hp < balance.UnitHp[(int)damaged.Kind] && state.Region(army.Region).Owned[i] > 0; break;
                    case OrderKind.Hide:
                        key = "hide:" + o.Region; v.Cost = own.Architecture == Architecture.Ghost ? balance.GhostHideCost : balance.HideCost; valid = region != null; break;
                    case OrderKind.Feint:
                        key = "feint:" + o.Region; v.Cost = own.Architecture == Architecture.Ghost ? balance.GhostFeintCost : balance.FeintCost; v.Noise = balance.FeintNoise; valid = region != null; break;
                    case OrderKind.MoveCore:
                        key = "core"; v.Cost = balance.CoreMoveCost; v.Noise = balance.CoreMoveNoise;
                        valid = region != null && region.Owned[i] > 0 && o.Region != own.CoreRegion && own.CoreMove == null; break;
                    case OrderKind.StartSingularity:
                        key = "singularity"; v.Cost = balance.SingularityCost;
                        valid = own.Singularity == null && own.Tiers.Count(x => x >= 3) >= balance.SingularityFamilies; break;
                    case OrderKind.Wait: key = "wait"; break;
                }
                if (v.Rejection != RejectReason.None) continue;
                if (!valid) { v.Rejection = RejectReason.InvalidTarget; continue; }
                if (key != null && used.Contains(key)) { v.Rejection = RejectReason.Duplicate; continue; }
                if (cash < v.Cost) { v.Rejection = RejectReason.InsufficientCompute; continue; }
                cash -= v.Cost;
                if (key != null) used.Add(key);
                if (o.Kind == OrderKind.NewArmy) newArmies++;
                if (o.Kind == OrderKind.Produce) newUnits[army.Id]++;
                if (o.Kind == OrderKind.BuildWall) wallAdded[o.Region] = wallAdded.TryGetValue(o.Region, out int old) ? old + 1 : 1;
            }
            return output;
        }

        void MoveArmies(List<ValidatedOrder>[] orders)
        {
            for (int i = 0; i < 2; i++) foreach (var v in orders[i].Where(x => x.Valid && (x.Order.Kind == OrderKind.March || x.Order.Kind == OrderKind.Attack)))
                state.Sides[i].Armies.First(x => x.Id == v.Order.ArmyId).Region = v.Order.Region;
        }

        void ResolveExpansions(List<ValidatedOrder>[] orders)
        {
            foreach (var r in state.Regions)
            {
                var p = orders[0].FirstOrDefault(x => x.Valid && x.Order.Kind == OrderKind.Expand && x.Order.Region == r.Id);
                var a = orders[1].FirstOrDefault(x => x.Valid && x.Order.Kind == OrderKind.Expand && x.Order.Region == r.Id);
                int pc = p?.Order.Amount ?? 0, ac = a?.Order.Amount ?? 0;
                if (pc + ac <= r.Free) { GrantFree(r, 0, pc); GrantFree(r, 1, ac); }
                else if (p != null && a != null && p.Cost == a.Cost) { }
                else
                {
                    int first = (p?.Cost ?? -1) > (a?.Cost ?? -1) ? 0 : 1;
                    GrantFree(r, first, Math.Min(r.Free, first == 0 ? pc : ac));
                    GrantFree(r, 1 - first, Math.Min(r.Free, first == 0 ? ac : pc));
                }
            }
        }
        static void GrantFree(RegionState r, int i, int count) { r.Free -= count; r.Owned[i] += count; }
        internal static int Transfer(RegionState r, int i, int count)
        {
            int enemy = Math.Min(r.Owned[1 - i], count); r.Owned[1 - i] -= enemy; r.Owned[i] += enemy;
            int free = Math.Min(r.Free, count - enemy); r.Free -= free; r.Owned[i] += free; return enemy + free;
        }

        void ApplyDevelopment(int i, List<ValidatedOrder> orders)
        {
            var own = state.Sides[i];
            foreach (var v in orders.Where(x => x.Valid))
            {
                var o = v.Order; var r = state.Region(o.Region); var army = own.Armies.FirstOrDefault(x => x.Id == o.ArmyId);
                bool lost = false;
                switch (o.Kind)
                {
                    case OrderKind.BuildWall:
                        if (r.Owned[i] > 0) r.Wall[i] = Math.Min(balance.MaxWall, r.Wall[i] + 1); else lost = true; break;
                    case OrderKind.Train: own.Training = new ProgressState { Family = o.Family, Tier = own.Tiers[(int)o.Family] + 1 }; break;
                    case OrderKind.Produce:
                        var def = AynaUnits.Get(o.Unit);
                        if (army != null && state.Region(army.Region).Owned[i] > 0 && state.Data(i, def.Family) > 0 && army.Units.Count < balance.MaxUnits)
                            army.Units.Add(new UnitState { Id = AllocateUnit(own), Kind = o.Unit, Hp = balance.UnitHp[(int)o.Unit] }); else lost = true;
                        break;
                    case OrderKind.NewArmy:
                        if (r.Owned[i] > 0) own.Armies.Add(new ArmyState { Id = AllocateArmy(own), Region = r.Id }); else lost = true; break;
                    case OrderKind.Repair:
                        var u = army?.Units.FirstOrDefault(x => x.Id == o.UnitId);
                        if (u != null && state.Region(army.Region).Owned[i] > 0) u.Hp = balance.UnitHp[(int)u.Kind]; else lost = true; break;
                    case OrderKind.MoveCore: own.CoreMove = new ProgressState { Region = o.Region }; break;
                    case OrderKind.StartSingularity:
                        own.Singularity = new ProgressState { Families = Enumerable.Range(0, 5).Where(f => own.Tiers[f] >= 3).Take(balance.SingularityFamilies).Select(f => (DataFamily)f).ToList() }; break;
                }
                if (lost) state.Knowledge[i].Receipts[v.Index] = new OrderReceipt(v.Index, o, RejectReason.LostCondition, v.Cost, state.Knowledge[i].Receipts[v.Index].Noise);
            }
            if (own.Training != null)
            {
                var t = own.Training;
                if (state.Data(i, t.Family) >= balance.TrainingServers[t.Tier - 1]) t.Progress++;
                if (t.Progress >= balance.TrainingDurations[t.Tier - 1]) { own.Tiers[(int)t.Family] = t.Tier; own.Training = null; }
            }
            if (own.Singularity != null && own.Singularity.Families.All(f => state.Data(i, f) >= balance.SingularityServers)) own.Singularity.Progress++;
            if (own.CoreMove != null)
            {
                own.CoreMove.Progress++;
                if (own.CoreMove.Progress >= balance.CoreMoveDuration)
                {
                    if (state.Region(own.CoreMove.Region).Owned[i] > 0) own.CoreRegion = own.CoreMove.Region;
                    own.CoreMove = null;
                }
            }
        }
    }
}
