using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    public sealed partial class Simulation
    {
        /// <summary>Kilitlenecek gerçek kişiler; rol tanımı, kimlik ve taktik değerleri aynı sırada taşınır.</summary>
        public List<SquadSlot> PlanningSquad()
        {
            var slots = new List<SquadSlot>();
            if (Board == null) return slots;
            foreach (string role in PlanningBoardData.FieldJobs)
            {
                var person = Board.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == role);
                if (person == null) continue;
                int tier = BoardSkillTier(person, role);
                slots.Add(new SquadSlot
                {
                    Type = role, PersonId = person.Id, PersonName = person.Name,
                    Hp = person.Hp, MaxHp = person.MaxHp,
                    Move = Math.Min(B.boardMaxFieldMove, person.Move + (tier >= B.boardFieldSkillMoveTier ? B.boardFieldSkillMoveBonus : 0)),
                    Power = Math.Min(B.boardMaxFieldPower, person.Power + (tier >= B.boardFieldSkillPowerTier ? B.boardFieldSkillPowerBonus : 0)),
                    Armor = Board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "armor" && e.Charges > 0) ? 1 : 0,
                    EmpBoosts = role == "emp" && Board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "emp_cell") ? B.boardEmpBoostCharges : 0,
                    EmpBoostDamage = B.boardEmpBoostDamage,
                });
            }
            return slots;
        }

        public string PlanningRaidCheck()
        {
            if (Board.Locked) return Loc.T("Plan kilitli. Önce devir fişiyle vardiyayı kapat.");
            string why = BoardPlanCheck();
            if (why != null) return why;
            var squad = PlanningSquad();
            if (squad.Count != PlanningBoardData.FieldJobs.Length) return Loc.T("Panoda Teknisyen, EMP uzmanı ve Analist için üç sağlıklı kişi seç.");
            foreach (var slot in squad)
            {
                var person = Board.Person(slot.PersonId);
                if (person.Restricted) return Loc.F("{0} kısıtlı kabulde; saha erişimi kapalı.", person.Name);
                if (!Board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "kit_" + slot.Type && e.Charges > 0))
                    return Loc.F("{0} için {1} gerekiyor.", person.Name, PlanningBoardData.EquipmentName("kit_" + slot.Type));
            }
            return null;
        }

        void ReturnPlanningSquad(List<SquadSlot> squad, RaidResult result, LiberationRaidReward reward)
        {
            if (squad == null) return;
            var hp = result.Hp != null && result.Hp.Count > 0 ? result.Hp : squad.Select((x, i) => i < result.Down ? 0 : x.Hp).ToList();
            var armor = result.Armor != null && result.Armor.Count > 0 ? result.Armor : squad.Select(x => x.Armor).ToList();
            bool survivor = hp.Any(x => x > 0);
            var lines = new List<string>();
            for (int i = 0; i < squad.Count; i++)
            {
                var slot = squad[i];
                var person = Board.Person(slot.PersonId);
                if (person == null || !person.Alive) continue;
                int after = i < hp.Count ? Math.Max(0, hp[i]) : 0;
                bool left = result.Left != null && result.Left.Contains(i);
                bool dead = left || after <= 0 && !survivor;
                bool broken = slot.Armor > 0 && i < armor.Count && armor[i] < slot.Armor;
                if (broken)
                {
                    var plate = Board.Equipment.FirstOrDefault(e => e.OwnerId == person.Id && e.Kind == "armor");
                    if (plate != null) Board.Equipment.Remove(plate);
                }
                if (dead)
                {
                    person.Alive = false; person.Hp = 0; person.Down = 0; person.Job = "rest";
                    Board.Equipment.RemoveAll(e => e.OwnerId == person.Id);
                    lines.Add(Loc.F("{0} kaybedildi. Kişisel ustalığı ve geride kalan teçhizatı geri gelmeyecek.", person.Name));
                }
                else if (after <= 0)
                {
                    int tier = BoardSkillTier(person, slot.Type);
                    person.Hp = 0;
                    person.Down = Math.Max(1, B.boardInjuryTurns - (tier >= B.boardFieldSkillRecoveryTier ? B.boardFieldSkillRecoveryBonus : 0));
                    person.Job = "rest";
                    lines.Add(Loc.F("{0} ekipçe kurtarıldı; {1} vardiya yatış gerekiyor.", person.Name, person.Down));
                }
                else
                {
                    person.Hp = Math.Min(person.MaxHp, after); person.Down = 0;
                    BoardGainExperience(person, slot.Type);
                    lines.Add(Loc.F("{0} döndü: can {1}/{2}.", person.Name, person.Hp, person.MaxHp));
                }
                reward.Squad.Add(new SquadOutcome
                {
                    Type = slot.Type, PersonId = person.Id, PersonName = person.Name, Dead = dead,
                    HpBefore = slot.Hp, HpAfter = after, Down = after <= 0, Left = left, PlateBroken = broken,
                });
            }
            Board.RaidSummary = string.Join("\n", lines);
            Shelter.Population = Board.People.Count(p => p.Alive);
            if (Shelter.Population == 0) End("topluluk");
        }

        void PlanningRaidReward(bool loot)
        {
            AddBoardMaterial("metal", loot ? B.boardLootMetal : B.boardServerMetal);
            AddBoardMaterial("devre", loot ? B.boardLootCircuits : B.boardServerCircuits);
            AddBoardMaterial("kumas", loot ? B.boardLootCloth : B.boardServerCloth);
            AddBoardMaterial("kimyasal", loot ? B.boardLootChemicals : B.boardServerChemicals);
            if (loot) Shelter.Supplies = Math.Min(B.boardSuppliesCapacity, Shelter.Supplies + B.boardLootSupplies);
        }
    }
}
