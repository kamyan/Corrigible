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
                string[] gear = { Item(items, e => e.Kind == "kit_" + role), Item(items, e => e.Kind == "armor"), Item(items, e => !e.Kind.StartsWith("kit_", StringComparison.Ordinal) && e.Kind != "armor") };
                return new SlipMember
                {
                    Letter = Baskin.UnitTypes[role].Short, PersonId = p?.Id, Name = p?.Name ?? Loc.T("KADRO EKSİK"),
                    Profession = p == null ? Baskin.UnitTypes[role].Name : Loc.T(p.Profession), Missing = p == null,
                    Health = p?.Hp ?? 0, MaxHealth = p?.MaxHp ?? 0, Hp = p == null ? "—" : Loc.F("{0}/{1}", p.Hp, p.MaxHp),
                    Stats = p == null ? "—" : Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}", p.Hp, p.MaxHp, slot?.Move ?? p.Move, slot?.Power ?? p.Power),
                    Slots = gear, Equipment = string.Join(" · ", gear.Where(s => !string.IsNullOrEmpty(s))),
                    Hover = p == null ? null : Loc.F("{0} · ustalık {1}", Baskin.UnitTypes[role].Name, sim.BoardSkillTier(p, role)),
                };
            }).ToList();
        }

        static string Item(IEnumerable<BoardEquipment> items, Func<BoardEquipment, bool> predicate) =>
            string.Join(" / ", items.Where(predicate).Select(e => PlanningBoardData.EquipmentName(e.Kind)));

        public static int[] Nominal(Simulation sim, bool loot) => loot
            ? new[] { sim.B.boardLootMetal, sim.B.boardLootCircuits, sim.B.boardLootCloth, sim.B.boardLootChemicals }
            : new[] { sim.B.boardServerMetal, sim.B.boardServerCircuits, sim.B.boardServerCloth, sim.B.boardServerChemicals };

        /// <summary>Kayıttaki n'inci baskın sonucunun hemen öncesini yeniden oynatır. Kopyanın komutları ayrı listededir.</summary>
        public static int[] MaterialGains(Simulation sim, RaidLogEntry log)
        {
            var gains = new int[4];
            if (!log.Success || sim.Board == null || sim.Record == null) return gains;
            var before = BeforeResult(sim, log);
            if (before?.Board == null) return gains;
            int[] nominal = Nominal(before, log.MissionKind == "loot");
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
