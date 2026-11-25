using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>İsimli saha kadrosu ve kayıt komutundan gerçek ganimet; canlı durumu değiştirmeyen kâğıt izdüşümü.</summary>
    public static class RaidPaperFacts
    {
        public static List<SlipMember> Members(Simulation sim)
        {
            var slots = sim.PlanningSquad();
            return PlanningBoardData.FieldJobs.Select(role =>
            {
                var p = sim.Board.People.FirstOrDefault(x => x.Alive && x.Job == role);
                var slot = slots.FirstOrDefault(x => x.Type == role);
                var items = sim.Board.Equipment.Where(e => p != null && e.OwnerId == p.Id).ToArray();
                string[] gear = { Item(items, e => e.Kind == "kit_" + role), Item(items, e => e.Kind == "armor"), Utility(sim, items, slot) };
                return new SlipMember
                {
                    Letter = Baskin.UnitTypes[role].Short, PersonId = p?.Id, Name = p?.Name ?? Loc.T("KADRO EKSİK"),
                    Profession = p == null ? Baskin.UnitTypes[role].Name : Loc.T(p.Profession), Missing = p == null,
                    // Baskına çıkacak can kadro yuvasından (yarım tayında eksik çıkar; Core PlanningSquad); yuva yoksa kişinin canı
                    Health = slot?.Hp ?? p?.Hp ?? 0, MaxHealth = p?.MaxHp ?? 0, Hp = p == null ? "—" : Loc.F("{0}/{1}", slot?.Hp ?? p.Hp, p.MaxHp),
                    Stats = p == null ? "—" : Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}", slot?.Hp ?? p.Hp, p.MaxHp, slot?.Move ?? p.Move, slot?.Power ?? p.Power),
                    Slots = gear, Equipment = string.Join(" · ", gear.Where(s => !string.IsNullOrEmpty(s))),
                    Hover = p == null ? null : Loc.F("{0} · ustalık {1}", Baskin.UnitTypes[role].Name, sim.BoardSkillTier(p, role)),
                };
            }).ToList();
        }

        static string Item(IEnumerable<BoardEquipment> items, Func<BoardEquipment, bool> predicate) =>
            string.Join(" / ", items.Where(predicate).Select(e => PlanningBoardData.EquipmentName(e.Kind)));

        /// <summary>
        /// Üçüncü yuva (yardımcı). Tesis aşama 9 (§7.5): emrin götüreceği ekipman hakkıyla ("Taret · 1 hak"; köpek yalnız adıyla), dinlenen
        /// köpek "Köpek (dinleniyor · 2 V) · baskına gitmez" (Core <c>PlanningSquad</c> onu götürmez). Öbür eşyalar adla.
        /// </summary>
        static string Utility(Simulation sim, IEnumerable<BoardEquipment> items, SquadSlot slot)
        {
            var list = items.Where(e => !e.Kind.StartsWith("kit_", StringComparison.Ordinal) && e.Kind != "armor").ToList();
            if (!FacilityBoardText.On(sim)) return Item(list, e => true);
            return string.Join(" / ", list.Select(e =>
            {
                string name = PlanningBoardData.EquipmentName(e.Kind);
                if (!FacilityGear.IsGear(e.Kind)) return name;
                if (e.Rest > 0) return Loc.F("{0} (dinleniyor · {1} V) · baskına gitmez", name, e.Rest);
                if (e.Kind == FacilityGear.Dog || slot?.Gear != e.Kind) return name;
                return Loc.F("{0} · {1} hak", name, slot.GearCharges);
            }));
        }

        public static int[] Nominal(Simulation sim, bool loot) => loot
            ? new[] { sim.B.boardLootMetal, sim.B.boardLootCircuits, sim.B.boardLootCloth, sim.B.boardLootChemicals }
            : new[] { sim.B.boardServerMetal, sim.B.boardServerCircuits, sim.B.boardServerCloth, sim.B.boardServerChemicals };

        /// <summary>
        /// Kayıttaki n'inci baskın sonucunun hemen öncesini yeniden oynatır. Kopyanın komutları ayrı listededir. Tesis baskınında
        /// büyük sandık açıldıysa (<see cref="LiberationRaidReward.BigCrate"/>) metal ve devreye sandığın eki de sayılır; kapasite
        /// kırpması tek <c>min</c> ile (Core'un iki ardışık <c>AddBoardMaterial</c> kırpmasıyla aynı sonuç).
        /// </summary>
        public static int[] MaterialGains(Simulation sim, RaidLogEntry log)
        {
            var gains = new int[4];
            // Tesis aşama 11: savunma hammadde getirmez
            if (!log.Success || sim.Board == null || sim.Record == null || FacilityDefenseText.IsDefense(log)) return gains;
            var before = BeforeResult(sim, log);
            if (before?.Board == null) return gains;
            int[] nominal = Nominal(before, log.MissionKind == "loot");
            if (log.LiberationReward?.BigCrate == true)
            {
                nominal[Array.IndexOf(PlanningBoardData.MaterialKinds, "metal")] += Math.Max(0, before.B.facilityBigCrateMetal);
                nominal[Array.IndexOf(PlanningBoardData.MaterialKinds, "devre")] += Math.Max(0, before.B.facilityBigCrateCircuits);
            }
            for (int i = 0; i < gains.Length; i++)
                gains[i] = Math.Max(0, Math.Min(Math.Max(0, before.B.boardMaterialsCapacity) - (before.Board.Material(PlanningBoardData.MaterialKinds[i])?.Quantity ?? 0), Math.Max(0, nominal[i])));
            return gains;
        }

        static int ResultIndex(Simulation sim, RaidLogEntry log)
        {
            if (sim.Record == null) return -1;
            int ordinal = sim.S.RaidLog.IndexOf(log), n = -1, index = -1;
            for (int i = 0; i < sim.Record.commands.Count; i++)
                if (sim.Record.commands[i].kind == "raidres" && ++n == ordinal) { index = i; break; }
            return index;
        }

        public static RaidResult Result(Simulation sim, RaidLogEntry log)
        {
            int index = ResultIndex(sim, log);
            return index < 0 ? null : RaidResult.Decode(sim.Record.commands[index].a);
        }

        public static Simulation BeforeResult(Simulation sim, RaidLogEntry log)
        {
            int index = ResultIndex(sim, log);
            if (index < 0) return null;
            return Replay.Restore(new GameRecord { seed = sim.Record.seed, balance = sim.Record.balance,
                day = log.Day, commands = sim.Record.commands.Take(index).ToList() });
        }

        public static string DisabledReason(Simulation sim)
        {
            if (sim.S.Raid != null) return Loc.T("SAHADA");
            // Tesis aşama 9 (K28): bekleyen sunucu ödülü baskın emrini tutar (Core LiberationRaidCheck)
            if (FacilityRewardText.Pending(sim)) return Loc.T("ÖDÜL SEÇİLMEDİ");
            if (sim.Board.Locked) return Loc.T("PLAN KİLİTLİ");
            foreach (var role in PlanningBoardData.FieldJobs)
            {
                var p = sim.Board.People.FirstOrDefault(x => x.Alive && x.Job == role);
                if (p == null) return Loc.T("KADRO EKSİK");
                if (p.Down > 0 || p.Hp <= 0) return Loc.T("YARALI KİŞİ");
                if (!sim.Board.Equipment.Any(e => e.OwnerId == p.Id && e.Kind == "kit_" + role && e.Charges > 0)) return Loc.T("KİT EKSİK");
            }
            return Loc.T("PANO PLANINI DENETLE");
        }
    }
}
