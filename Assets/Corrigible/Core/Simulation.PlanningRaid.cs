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
                // Tesis aşama 8: yardımcı yuvadaki ekipman (dinlenen köpek gitmez); hak emirde dengeden ve kazanımlardan dolar
                var gear = Board.Equipment.FirstOrDefault(e => e.OwnerId == person.Id && FacilityGear.IsGear(e.Kind) && e.Charges > 0 && e.Rest == 0
                    && PlanningBoardData.EquipmentSlot(e.Kind) == "utility");
                slots.Add(new SquadSlot
                {
                    Type = role, PersonId = person.Id, PersonName = person.Name,
                    // Yarım tayında kadro eksik canla çıkar (en az 1); fark dönüşte geri gelir.
                    Hp = HalfRationActive ? Math.Max(1, person.Hp - Math.Max(0, B.halfRationRaidHp)) : person.Hp, MaxHp = person.MaxHp,
                    Move = Math.Min(B.boardMaxFieldMove, person.Move + (tier >= B.boardFieldSkillMoveTier ? B.boardFieldSkillMoveBonus : 0)),
                    Power = Math.Min(B.boardMaxFieldPower, person.Power + (tier >= B.boardFieldSkillPowerTier ? B.boardFieldSkillPowerBonus : 0)),
                    Armor = Board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "armor" && e.Charges > 0) ? 1 : 0,
                    EmpBoosts = role == "emp" && Board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "emp_cell") ? B.boardEmpBoostCharges : 0,
                    EmpBoostDamage = B.boardEmpBoostDamage,
                    Gear = gear?.Kind, GearCharges = gear == null ? 0 : FacilityGear.RaidCharges(B, gear.Kind, Facility?.Perks),
                    Titles = FacilityTitlesOn ? person.Titles : FacilityTitle.None,
                });
            }
            return slots;
        }

        void SnapshotFieldIdentities(RaidParams p)
        {
            if (!B.infiltratorFieldEnabled || Admission == null || p.Squad == null) return;
            p.FieldTurn = Math.Max(1, B.infiltratorTurn);
            for (int i = 0; i < p.Squad.Count; i++)
            {
                var file = Admission.File(p.Squad[i].PersonId);
                var identity = file?.Identity;
                if (identity == null || file.Founder) continue;
                if (identity.Robot) p.Turncoats |= 1 << i;
                if ((identity.Robot || identity.HumanTrace) && identity.TraceDelay <= B.infiltratorTellMaxDelay)
                    p.Tells |= 1 << i;
            }
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
                if (person.Restricted) return Loc.F("{0}: {1}", person.Name, Loc.T("Kısıtlı sakin bu göreve erişemez."));
                if (!Board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "kit_" + slot.Type && e.Charges > 0))
                    return Loc.F("{0} için {1} gerekiyor.", person.Name, PlanningBoardData.EquipmentName("kit_" + slot.Type));
            }
            if (ShelterEvents != null)
                foreach (var feud in ShelterEvents.Feuds)
                    if (S.Day < feud.Until && squad.Any(x => x.PersonId == feud.A) && squad.Any(x => x.PersonId == feud.B))
                        return Loc.F("{0} ile {1} ayrı tutuluyor; aynı baskına giremezler.", EventPersonName(feud.A), EventPersonName(feud.B));
            return null;
        }

        /// <remarks>Tesis baskını (aşama 6, belge §3.6): düşen yalnız tesisten çıkarıldıysa (<see cref="RaidResult.Extracted"/>) yaşar;
        /// "ekip ayakta döndüyse düşenler kurtarılır" kuralı yoktur (tek tahtayla kalktı).</remarks>
        /// <param name="perks">Tesis aşama 8: emirdeki kazanımlar (<see cref="RaidParams.Perks"/>; Revir).</param>
        /// <param name="reports">Tesis aşama 8: baskının oda raporları (ekipmanın baskın sonu durumu son rapordan okunur: hakkı biten
        /// kelepçe panodan silinir, düşen köpek <see cref="Balance.facilityDogRestTurns"/> vardiya dinlenir; sonuç dizgisi değişmez).</param>
        /// <param name="experience">Tesis aşama 11: ayakta dönen deneyim kazanır mı (savunmada yalnız kazanılırsa, K23).</param>
        /// <remarks>Tesis aşama 12 (Katar): <see cref="RaidResult.Overboard"/>'daki kişi (açık kenardan düştü) ölmez;
        /// <see cref="Balance.facilityFallInjuryTurns"/> vardiya (en az 1) yatar, beceri indirimi, Revir ve deneyim yoktur, eşyası sahibinde
        /// kalır (<see cref="SquadOutcome.Overboard"/>).</remarks>
        void ReturnPlanningSquad(List<SquadSlot> squad, RaidResult result, LiberationRaidReward reward,
            IReadOnlyCollection<string> perks = null, IReadOnlyList<RoomReport> reports = null, bool experience = true, bool titleEligible = true)
        {
            bool infirmary = perks != null && perks.Contains(FacilityGear.Infirmary);
            if (squad == null) return;
            var hp = result.Hp != null && result.Hp.Count > 0 ? result.Hp : squad.Select((x, i) => i < result.Down ? 0 : x.Hp).ToList();
            var armor = result.Armor != null && result.Armor.Count > 0 ? result.Armor : squad.Select(x => x.Armor).ToList();
            // Tesis aşama 8: ekipmanın baskın sonu durumu son raporda (kelepçenin hakkı, köpeğin düşmesi)
            var last = reports != null && reports.Count > 0 ? reports[reports.Count - 1] : null;
            bool creditTitles = FacilityTitlesOn && titleEligible && result.Success && last != null;
            var lines = new List<string>();
            for (int i = 0; i < squad.Count; i++)
            {
                var slot = squad[i];
                var person = Board.Person(slot.PersonId);
                if (person == null || !person.Alive) continue;
                int after = i < hp.Count ? Math.Max(0, hp[i]) : 0;
                bool left = result.Left != null && result.Left.Contains(i);
                // Tesis aşama 12 (Katar, K14): açık kenardan düşen ölmez, düz yatar (RaidResult.Overboard; oda raporunda "k")
                bool over = result.Overboard != null && result.Overboard.Contains(i);
                bool turned = B.infiltratorFieldEnabled && result.Turned.Contains(i);
                bool subdued = turned && result.Subdued.Contains(i);
                bool dead = !subdued && (left || after <= 0 && !over && (result.Extracted == null || !result.Extracted.Contains(i)));
                bool broken = slot.Armor > 0 && i < armor.Count && armor[i] < slot.Armor;
                // Kelepçe kullanılınca tükenir (K27; kaybedilen baskında da): panodan silinir. Düşen köpek dinlenir (K16)
                bool clampSpent = last != null && slot.Gear == FacilityGear.Clamp && i < last.Team.Count && last.Team[i].Gear == 0;
                bool dogDown = last != null && slot.Gear == FacilityGear.Dog
                    && (last.Units ?? new List<RoomUnit>()).Any(u => u.Class == Baskin.ClassDog && u.Owner == i && u.State == "d");
                if (clampSpent)
                {
                    var clamp = Board.Equipment.FirstOrDefault(e => e.OwnerId == person.Id && e.Kind == FacilityGear.Clamp);
                    if (clamp != null) Board.Equipment.Remove(clamp);
                }
                if (dogDown)
                    foreach (var dog in Board.Equipment.Where(e => e.OwnerId == person.Id && e.Kind == FacilityGear.Dog)) dog.Rest = Math.Max(0, B.facilityDogRestTurns);
                if (broken)
                {
                    var plate = Board.Equipment.FirstOrDefault(e => e.OwnerId == person.Id && e.Kind == "armor");
                    if (plate != null) Board.Equipment.Remove(plate);
                }
                var file = turned ? Admission?.File(person.Id) : null;
                if (file != null) file.Revealed = true;
                if (subdued && file != null)
                {
                    person.Hp = 1; person.Down = 0; person.Job = "rest";
                    person.Isolated = true;
                    file.IsolatedUntil = -1;
                    file.FieldDecisionDue = true;
                    ReleaseResidentEquipment(person.Id, false);
                    lines.Add(Loc.F("{0} sahada yakalandı. Sorgu kararı bekliyor.", person.Name));
                }
                else if (dead)
                {
                    person.Alive = false; person.Hp = 0; person.Down = 0; person.Job = "rest";
                    // Köpek ölmez (tesis aşama 8, K14): ölen sahibinin köpeği depoya döner; öbür eşyası kaybolur
                    foreach (var dog in Board.Equipment.Where(e => e.OwnerId == person.Id && e.Kind == FacilityGear.Dog)) dog.OwnerId = null;
                    Board.Equipment.RemoveAll(e => e.OwnerId == person.Id);
                    lines.Add(Loc.F("{0} kaybedildi. Kişisel ustalığı ve geride kalan teçhizatı geri gelmeyecek.", person.Name));
                }
                else if (after <= 0 && over)
                {
                    // Düz yatış (en az 1): beceri indirimi ve Revir yok (Revir "taşınan düşen" içindir), deneyim yok; eşyası sahibinde kalır
                    person.Hp = 0;
                    person.Down = Math.Max(1, B.facilityFallInjuryTurns);
                    person.Job = "rest";
                    lines.Add(Loc.F("{0} vagondan düştü; {1} vardiya yatış gerekiyor.", person.Name, person.Down));
                }
                else if (after <= 0)
                {
                    int tier = BoardSkillTier(person, slot.Type);
                    person.Hp = 0;
                    // Revir (tesis aşama 8): tesisten çıkarılan düşenin yatışı kısalır (en az 1)
                    person.Down = Math.Max(1, B.boardInjuryTurns - (tier >= B.boardFieldSkillRecoveryTier ? B.boardFieldSkillRecoveryBonus : 0)
                        - (infirmary ? Math.Max(0, B.facilityInfirmaryShifts) : 0));
                    person.Job = "rest";
                    lines.Add(Loc.F("{0} ekipçe kurtarıldı; {1} vardiya yatış gerekiyor.", person.Name, person.Down));
                }
                else
                {
                    // Yarım tayında eksik çıkılan can kalıcı kayıp değildir: baskın sürerken pano değişmediği için fark buradan türer.
                    person.Hp = Math.Min(person.MaxHp, after + Math.Max(0, person.Hp - slot.Hp)); person.Down = 0;
                    if (experience) BoardGainExperience(person, slot.Type);
                    lines.Add(Loc.F("{0} döndü: can {1}/{2}.", person.Name, person.Hp, person.MaxHp));
                }
                var awards = creditTitles && !dead && !turned
                    ? FacilityTitles.Credit(person, FacilityTitles.RaidWorks(i, reports, result), B) : FacilityTitle.None;
                reward.Squad.Add(new SquadOutcome
                {
                    Type = slot.Type, PersonId = person.Id, PersonName = person.Name, Dead = dead,
                    HpBefore = slot.Hp, HpAfter = after, Down = after <= 0, Left = left, PlateBroken = broken,
                    ClampSpent = clampSpent, DogDown = dogDown, Overboard = !dead && after <= 0 && over,
                    NewTitles = awards, Turned = turned, Subdued = subdued,
                });
            }
            OpenFieldDecision();
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
