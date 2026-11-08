using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Corrigible.Core
{
    public sealed partial class Simulation
    {
        public PlanningBoardState Board => Shelter?.Plan;
        internal bool BoardMayAdvance => Board == null || Board.Filing;

        public List<BoardRecipe> BoardRecipes => new List<BoardRecipe>
        {
            Recipe("armor", Loc.T("Zırh plakası"), "armor", B.boardArmorTurns, "metal", B.boardArmorMetal, "kumas", B.boardArmorCloth),
            Recipe("medkit", Loc.T("Bakım kiti"), "medkit", B.boardMedkitTurns, "kumas", B.boardMedkitCloth, "kimyasal", B.boardMedkitChemicals),
            Recipe("emp_cell", Loc.T("EMP hücresi"), "emp_cell", B.boardEmpTurns, "metal", B.boardEmpMetal, "devre", B.boardEmpCircuits),
            // Son teknisyenin kaybı, kalan sakinlerin temel saha kitine erişimini kapatmaz.
            Recipe("kit_tek", Loc.T("Teknisyen kiti"), "kit_tek", B.boardKitTurns, "metal", B.boardKitMetal, "devre", B.boardKitCircuits),
            Recipe("kit_emp", Loc.T("EMP kiti"), "kit_emp", B.boardKitTurns, "metal", B.boardKitMetal, "devre", B.boardKitCircuits),
            Recipe("kit_ana", Loc.T("Analist kiti"), "kit_ana", B.boardKitTurns, "metal", B.boardKitMetal, "devre", B.boardKitCircuits),
        };

        static BoardRecipe Recipe(string id, string name, string output, int turns, string first, int firstCount, string second, int secondCount) =>
            new BoardRecipe { Id = id, Name = name, Output = output, Turns = Math.Max(1, turns), Inputs = new List<BoardMaterial>
            { new BoardMaterial { Kind = first, Quantity = Math.Max(0, firstCount) }, new BoardMaterial { Kind = second, Quantity = Math.Max(0, secondCount) } } };

        void SetupPlanningBoard()
        {
            if (!B.planningBoardEnabled || Shelter == null) return;
            Shelter.Plan = new PlanningBoardState { Shift = S.Day };
            var names = new List<string> { "Derya", "Selin", "İpek", "Deniz", "Nermin", "Zeynep", "Orhan", "Cem", "Eren", "Suna" };
            for (int i = names.Count - 1; i > 0; i--)
            {
                int other = (int)Math.Floor(S.Rng.Next() * (i + 1));
                string hold = names[i]; names[i] = names[other]; names[other] = hold;
            }
            string[] jobs = { "tek", "emp", "ana", "craft", "cook", "care", "gather", "rest" };
            int count = Math.Min(Math.Max(3, B.boardStartResidents), Math.Max(3, B.boardResidentCapacity));
            for (int i = 0; i < count; i++)
            {
                var person = CreateBoardResident("sakin-" + (i + 1).ToString(CultureInfo.InvariantCulture), names[i % names.Count], i);
                person.Job = i < jobs.Length ? jobs[i] : "rest";
                Board.People.Add(person);
            }
            Board.NextResidentId = count + 1;
            AddBoardMaterial("metal", B.boardStartMetal);
            AddBoardMaterial("devre", B.boardStartCircuits);
            AddBoardMaterial("kumas", B.boardStartCloth);
            AddBoardMaterial("kimyasal", B.boardStartChemicals);
            for (int i = 0; i < PlanningBoardData.FieldJobs.Length; i++) AddBoardEquipment("kit_" + PlanningBoardData.FieldJobs[i], Board.People[i].Id);
            for (int i = 0; i < Math.Max(0, B.boardStartArmor); i++) AddBoardEquipment("armor", i < 2 ? Board.People[i].Id : null);
            for (int i = 0; i < Math.Max(0, B.boardStartMedkits); i++) AddBoardEquipment("medkit", null);
            Shelter.Supplies = Math.Min(B.boardSuppliesCapacity, Math.Max(0, B.boardStartSupplies));
            Shelter.Population = Board.People.Count(p => p.Alive);
            // Eski anonim oda işçileri, yedekler ve plaka stoku yeni panoda ikinci katkı sağlamaz.
            foreach (var room in Shelter.Rooms) room.Workers = 0;
            Shelter.Reserves.Clear();
            Shelter.Plates = 0;
            SyncBoardMaterialTotal();
        }

        ResidentState CreateBoardResident(string id, string name, int index)
        {
            string[] subjects = PlanningBoardData.Subjects;
            int subject = index >= 0 && index < subjects.Length ? index : (int)Math.Floor(S.Rng.Next() * subjects.Length);
            int profile = (int)Math.Floor(S.Rng.Next() * 4);
            int step = Math.Max(0, B.boardProfileStatStep);
            var p = new ResidentState
            {
                Id = id, Name = name, MaxHp = Math.Max(2, B.boardBaseHp), Move = Math.Max(2, B.boardBaseMove), Power = Math.Max(1, B.boardBasePower),
                Profession = ProfessionName(subjects[subject]), TrainingSubject = subjects[subject], Job = "rest"
            };
            switch (profile)
            {
                case 0: p.MaxHp += step; p.Move = Math.Max(2, p.Move - step); p.Trait = Loc.N("Dayanıklı, ağır adımlı"); break;
                case 1: p.MaxHp = Math.Max(2, p.MaxHp - step); p.Move += step; p.Trait = Loc.N("Çevik, narin"); break;
                case 2: p.Power += step; p.Move = Math.Max(2, p.Move - step); p.Trait = Loc.N("Güçlü, temkinli"); break;
                default: p.Trait = Loc.N("Dengeli, saha deneyimi az"); break;
            }
            p.Hp = p.MaxHp;
            p.Experience.Add(new ResidentSkill { Subject = subjects[subject], Points = B.boardTrainedAt });
            return p;
        }

        internal ResidentState CreateAdmissionResident(string id) => CreateBoardResident(id, id, -1);

        static string ProfessionName(string subject)
        {
            switch (subject)
            {
                case "tek": return Loc.N("Elektronikçi");
                case "emp": return Loc.N("Elektrikçi");
                case "ana": return Loc.N("Haberleşmeci");
                case "craft": return Loc.N("Tamirci");
                case "cook": return Loc.N("Aşçı");
                case "care": return Loc.N("Sağlık çalışanı");
                default: return Loc.N("Depo görevlisi");
            }
        }

        public int BoardSkillTier(ResidentState person, string subject)
        {
            int xp = person?.Skill(subject) ?? 0;
            return xp >= B.boardExpertAt ? 3 : xp >= B.boardExperiencedAt ? 2 : xp >= B.boardTrainedAt ? 1 : 0;
        }

        public void BoardGainExperience(ResidentState person, string subject)
        {
            if (person == null || !person.Alive || !PlanningBoardData.Subjects.Contains(subject)) return;
            GainBoardExperience(person, subject, B.boardWorkGain);
        }

        void GainBoardExperience(ResidentState person, string subject, int amount)
        {
            var skill = person.Experience.FirstOrDefault(x => x.Subject == subject);
            if (skill == null) { skill = new ResidentSkill { Subject = subject }; person.Experience.Add(skill); }
            skill.Points = Math.Min(Math.Max(0, B.boardExpertAt), skill.Points + Math.Max(0, amount));
        }

        string BoardCommandCheck(bool allowLocked = false)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (Board == null) return Loc.T("Bu kayıtta vardiya panosu yok.");
            if (S.Raid != null) return Loc.T("Baskın sürerken plan değiştirilemez.");
            if (Board.Locked && !allowLocked) return Loc.T("Plan kilitli. Fişi devir kutusuna bırak.");
            return null;
        }

        string BoardResidentJobCheck(ResidentState person, string job)
        {
            if (person == null || !person.Alive) return Loc.T("Bu sakin görev alamıyor.");
            if (!PlanningBoardData.Jobs.Contains(job)) return Loc.T("Böyle bir görev yok.");
            if (job == "rest") return null;
            if (person.Down > 0 || person.Hp <= 0) return Loc.T("Yatan yaralı önce dinlenmeli.");
            if (person.Restricted)
            {
                if (PlanningBoardData.FieldJobs.Contains(job) || job == "craft" || job == "contact" || job == "watch")
                    return Loc.T("Kısıtlı sakin bu göreve erişemez.");
                if (!CanRestrictedResidentWork(person.Id)) return Loc.T("Kısıtlı sakin için ayrı gözetmen gerekiyor.");
            }
            return null;
        }

        public string AssignResidentCheck(string personId, string jobId)
        {
            string why = BoardCommandCheck();
            if (why != null) return why;
            var person = Board.Person(personId);
            why = BoardResidentJobCheck(person, jobId);
            if (why != null) return why;
            if (person.Job == jobId) return Loc.T("Kişi zaten bu görevde.");
            var other = jobId == "rest" ? null : Board.People.FirstOrDefault(p => p.Alive && p.Id != personId && p.Job == jobId);
            if (other != null)
            {
                why = BoardResidentJobCheck(other, person.Job);
                if (why != null) return why;
                if (person.Job == "train" && (!PlanningBoardData.Subjects.Contains(other.TrainingSubject) || other.Skill(other.TrainingSubject) >= B.boardExpertAt))
                    return Loc.T("Takas edilecek kişinin uygun eğitim alanını seç.");
            }
            if (jobId == "train" && (!PlanningBoardData.Subjects.Contains(person.TrainingSubject) || person.Skill(person.TrainingSubject) >= B.boardExpertAt))
                return Loc.T("Önce uygun eğitim alanını seç.");
            return null;
        }

        public ActResult AssignResident(string personId, string jobId) => RunCommand(() =>
        {
            string why = AssignResidentCheck(personId, jobId);
            if (why != null) return ActResult.Fail(why);
            var person = Board.Person(personId);
            var other = jobId == "rest" ? null : Board.People.FirstOrDefault(p => p.Alive && p.Id != personId && p.Job == jobId);
            if (other != null) other.Job = person.Job;
            person.Job = jobId;
            return ActResult.Success(Loc.F("{0}: {1}.", person.Name, PlanningBoardData.JobName(jobId)));
        }, "board_assign", personId, jobId);

        public ActResult TrainResident(string personId, string subject) => RunCommand(() =>
        {
            string why = BoardCommandCheck();
            if (why != null) return ActResult.Fail(why);
            var person = Board.Person(personId);
            why = BoardResidentJobCheck(person, "train");
            if (why != null) return ActResult.Fail(why);
            if (!PlanningBoardData.Subjects.Contains(subject)) return ActResult.Fail(Loc.T("Böyle bir eğitim alanı yok."));
            if (person.Skill(subject) >= B.boardExpertAt) return ActResult.Fail(Loc.T("Kişi bu alanda zaten uzman."));
            if (person.Job == "train" && person.TrainingSubject == subject) return ActResult.Fail(Loc.T("Bu eğitim zaten seçili."));
            var other = Board.People.FirstOrDefault(p => p.Alive && p.Id != personId && p.Job == "train");
            if (other != null)
            {
                why = BoardResidentJobCheck(other, person.Job);
                if (why != null) return ActResult.Fail(why);
                other.Job = person.Job;
            }
            person.TrainingSubject = subject;
            person.Job = "train";
            return ActResult.Success(Loc.F("{0}, {1} eğitimine ayrıldı.", person.Name, PlanningBoardData.SubjectName(subject)));
        }, "board_train", personId, subject);

        public ActResult EquipResident(string equipmentId, string personId) => RunCommand(() =>
        {
            string why = BoardCommandCheck();
            if (why != null) return ActResult.Fail(why);
            var equipment = Board.EquipmentItem(equipmentId);
            if (equipment == null || equipment.Charges <= 0) return ActResult.Fail(Loc.T("Bu eşya depoda veya kişide bulunmuyor."));
            personId = string.IsNullOrEmpty(personId) ? null : personId;
            var person = personId == null ? null : Board.Person(personId);
            if (personId != null && (person == null || !person.Alive || person.Restricted)) return ActResult.Fail(Loc.T("Bu sakin teçhizat kullanamıyor."));
            if (equipment.OwnerId == personId) return ActResult.Fail(Loc.T("Eşya zaten bu cepte."));
            if (person != null)
            {
                string slot = PlanningBoardData.EquipmentSlot(equipment.Kind);
                var old = Board.Equipment.FirstOrDefault(e => e.Id != equipmentId && e.OwnerId == personId && PlanningBoardData.EquipmentSlot(e.Kind) == slot);
                if (old != null) old.OwnerId = equipment.OwnerId;
            }
            equipment.OwnerId = personId;
            return ActResult.Success(Loc.T("Teçhizat yerleştirildi."));
        }, "board_equip", equipmentId, personId);

        public int BoardMaterialFree(string kind) => Board == null ? 0 : Math.Max(0, (Board.Material(kind)?.Quantity ?? 0)
            - Board.Orders.Where(o => !o.Started).Sum(o => o.Reserves.Where(r => r.Kind == kind).Sum(r => r.Quantity))
            - Board.ProductionInputs.Where(m => m.Kind == kind).Sum(m => m.Quantity));

        /// <summary>Yeni üretim yolu ayrı bir komutla açılır; eski kayıtların komut sonuçları korunur.</summary>
        public ActResult EnableAutomaticProduction() => RunCommand(() =>
        {
            var why=BoardCommandCheck(); if(why!=null)return ActResult.Fail(why);
            if(Board.AutomaticProduction)return ActResult.Fail(Loc.T("Otomatik üretim zaten açık."));
            Board.Orders.RemoveAll(o=>!o.Started);
            Board.AutomaticProduction=true;
            return ActResult.Success(Loc.T("Üretim alanı hammaddelerle çalışır."));
        }, "board_auto");

        public ActResult StageProductionMaterial(string kind,int amount) => RunCommand(() =>
        {
            var why=BoardCommandCheck();if(why!=null)return ActResult.Fail(why);
            if(!Board.AutomaticProduction||!PlanningBoardData.MaterialKinds.Contains(kind)||amount<0)return ActResult.Fail(Loc.T("Geçerli hammadde miktarı seç."));
            var input=Board.ProductionInputs.FirstOrDefault(m=>m.Kind==kind);int old=input?.Quantity??0;
            if(amount==old||amount>BoardMaterialFree(kind)+old)return ActResult.Fail(Loc.T("Serbest hammadde yetersiz veya miktar aynı."));
            if(input==null){input=new BoardMaterial{Kind=kind};Board.ProductionInputs.Add(input);}input.Quantity=amount;
            return ActResult.Success(Loc.T("Hammadde üretim alanına kondu; uygun tarif varsa kapanışta işlenir."));
        }, "board_stage",kind,amount.ToString(CultureInfo.InvariantCulture));

        public BoardRecipe AutomaticProductionRecipe => Board==null ? null : BoardRecipes
            .Where(r=>r.Inputs.All(m=>Board.ProductionInputs.Where(i=>i.Kind==m.Kind).Sum(i=>i.Quantity)>=m.Quantity))
            .OrderBy(r=>r.Output.StartsWith("kit_",StringComparison.Ordinal)&&!Board.Equipment.Any(e=>e.Kind==r.Output)?0:1).FirstOrDefault();

        public ActResult ScrapEquipment(string equipmentId) => RunCommand(() =>
        {
            string why = BoardCommandCheck();
            if (why != null) return ActResult.Fail(why);
            var equipment = Board.EquipmentItem(equipmentId);
            if (equipment == null) return ActResult.Fail(Loc.T("Hurdaya ayrılabilen bir eşya seç."));
            Board.Equipment.Remove(equipment);
            AddBoardMaterial("metal", B.boardScrapMetal);
            return ActResult.Success(Loc.F("{0} hurdaya ayrıldı; {1} metal kazanıldı.", PlanningBoardData.EquipmentName(equipment.Kind), Math.Max(0, B.boardScrapMetal)));
        }, "board_scrap", equipmentId);

        public ActResult QueueProduction(string recipeId) => RunCommand(() =>
        {
            string why = BoardCommandCheck();
            if (why != null) return ActResult.Fail(why);
            if (Board.Orders.Count != 0) return ActResult.Fail(Loc.T("Önce mevcut üretim emrini tamamla veya iptal et."));
            var recipe = BoardRecipes.FirstOrDefault(r => r.Id == recipeId);
            if (recipe == null) return ActResult.Fail(Loc.T("Böyle bir tarif yok."));
            Board.Orders.Add(new BoardProductionOrder { Id = "is-" + (Board.NextOrderId++).ToString(CultureInfo.InvariantCulture), RecipeId = recipeId, Remaining = recipe.Turns });
            return ActResult.Success(Loc.F("{0} iş emri açıldı. Girdileri ayır.", recipe.Name));
        }, "board_queue", recipeId);

        /// <summary>Geçerli miktar, bu girdinin emirde ayrılmış toplamıdır; tekrar bırakmak ödeme yapmaz.</summary>
        public ActResult ReserveProductionMaterial(string materialKind, int amount) => RunCommand(() =>
        {
            string why = BoardCommandCheck();
            if (why != null) return ActResult.Fail(why);
            var order = Board.Orders.FirstOrDefault();
            if (order == null || order.Started) return ActResult.Fail(Loc.T("Girdisi ayrılabilen bir taslak iş yok."));
            var recipe = BoardRecipes.First(r => r.Id == order.RecipeId);
            int need = recipe.Inputs.FirstOrDefault(i => i.Kind == materialKind)?.Quantity ?? 0;
            var reserve = order.Reserves.FirstOrDefault(i => i.Kind == materialKind);
            int old = reserve?.Quantity ?? 0;
            if (amount < 0 || amount > need || need == 0) return ActResult.Fail(Loc.T("Bu tarif için uygun tür ve miktarı ayır."));
            if (amount == old) return ActResult.Fail(Loc.T("Bu miktar zaten ayrılmış."));
            if (amount > BoardMaterialFree(materialKind) + old) return ActResult.Fail(Loc.T("Serbest hammadde yetersiz."));
            if (reserve == null) { reserve = new BoardMaterial { Kind = materialKind }; order.Reserves.Add(reserve); }
            reserve.Quantity = amount;
            return ActResult.Success(Loc.T("Hammadde iş emrine ayrıldı; kapanışta ödenecek."));
        }, "board_reserve", materialKind, amount.ToString(CultureInfo.InvariantCulture));

        public ActResult CancelProduction(string orderId) => RunCommand(() =>
        {
            string why = BoardCommandCheck();
            if (why != null) return ActResult.Fail(why);
            var order = Board.Order(orderId);
            if (order == null) return ActResult.Fail(Loc.T("Böyle bir iş emri yok."));
            Board.Orders.Remove(order);
            return ActResult.Success(order.Started ? Loc.T("İş iptal edildi. Tüketilen girdiler geri dönmez.") : Loc.T("İş iptal edildi. Ayrılan girdiler serbest bırakıldı."));
        }, "board_cancel", orderId);

        public string BoardPlanCheck()
        {
            string why = BoardCommandCheck(true);
            if (why != null) return why;
            if (!Board.Locked)
            {
                foreach (var person in Board.People.Where(p => p.Alive))
                {
                    why = BoardResidentJobCheck(person, person.Job);
                    if (why != null) return Loc.F("{0}: {1}", person.Name, why);
                    if (person.Job == "train" && (!PlanningBoardData.Subjects.Contains(person.TrainingSubject) || person.Skill(person.TrainingSubject) >= B.boardExpertAt))
                        return Loc.F("{0} için uygun eğitim alanını seç.", person.Name);
                }
            }
            foreach (var order in Board.Orders.Where(o => !o.Started))
            {
                var recipe = BoardRecipes.FirstOrDefault(r => r.Id == order.RecipeId);
                if (recipe == null) return Loc.T("İş emrinin tarifi bulunamadı.");
                foreach (var input in recipe.Inputs)
                    if (order.Reserves.Where(r => r.Kind == input.Kind).Sum(r => r.Quantity) != input.Quantity)
                        return Loc.F("{0}: {1} adedi {2} olmalı.", recipe.Name, PlanningBoardData.MaterialName(input.Kind), input.Quantity);
            }
            if (Board.Orders.Count > 0 && Board.Equipment.Count >= B.boardEquipmentCapacity)
                return Loc.T("Teçhizat deposu dolu. Üretimden önce yer aç.");
            return null;
        }

        public double BoardFoodPreview
            => Board == null ? 0 : Math.Min(Math.Max(0, B.boardSuppliesCapacity), Math.Max(0, Shelter.Supplies + BoardFoodBalance)) - Shelter.Supplies;

        double BoardFoodBalance
        {
            get
            {
                if (Board == null) return 0;
                double consumption = Board.People.Count(p => p.Alive) * Math.Max(0, B.boardSuppliesPerResident);
                var cook = Board.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == "cook");
                if (cook != null) consumption -= Math.Min(consumption * B.boardCookingMaxSavingFraction, B.boardCookSaving + BoardSkillTier(cook, "cook") * B.boardCookSavingPerTier);
                var gather = Board.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == "gather");
                double gain = gather == null ? 0 : B.boardGatherSupplies + BoardSkillTier(gather, "gather") * B.boardGatherSuppliesPerTier;
                return gain - Math.Max(0, consumption);
            }
        }

        public ActResult PrintShift() => RunCommand(() =>
        {
            string why = BoardPlanCheck();
            if (why != null) return ActResult.Fail(why);
            if (Board.Printed) return ActResult.Fail(Loc.T("Bu vardiyanın fişi zaten çıkış tepsisinde."));
            Board.Locked = true;
            Board.Printed = true;
            Board.ReceiptId = "vardiya-" + S.Day.ToString(CultureInfo.InvariantCulture);
            Board.ReceiptText = BuildBoardReceipt();
            return ActResult.Success(Loc.T("Devir fişi basıldı. Günü bitirmek için devir kutusuna bırak."));
        }, "board_print");

        public ActResult FileShift(string receiptId)
        {
            string why = BoardCommandCheck(true);
            if (why != null) return ActResult.Fail(why);
            if (!Board.Printed || string.IsNullOrEmpty(receiptId) || Board.ReceiptId != receiptId || Board.LastFiledReceiptId == receiptId)
                return ActResult.Fail(Loc.T("Bu fiş bu vardiyanın etkin devir fişi değil."));
            // Komut eski güne yazılır. Yeniden oynatma aynı fişi bir kez kapatıp bir gün ilerler.
            var result = Rec(ActResult.Success(Loc.T("Vardiya devredildi.")), "board_file", receiptId);
            Board.LastFiledReceiptId = receiptId;
            Board.Filing = true;
            Step();
            return result;
        }

        /// <summary>Kilitli planın o dildeki salt okunur baskısı; zaman veya komut üretmez.</summary>
        public string BoardReceiptPreview => Board == null ? null : BuildBoardReceipt();

        string BuildBoardReceipt()
        {
            var lines = new List<string> { Loc.F("Vardiya {0} · Devir fişi", S.Day + 1) };
            foreach (var person in Board.People.Where(p => p.Alive))
                lines.Add(Loc.F("{0}: {1}", person.Name, PlanningBoardData.JobName(person.Job)));
            if (!string.IsNullOrEmpty(Board.RaidSummary)) lines.Add(Board.RaidSummary);
            lines.Add(Loc.F("Beklenen erzak değişimi: {0:0.##}", BoardFoodPreview));
            foreach (var order in Board.Orders)
            {
                var recipe = BoardRecipes.First(r => r.Id == order.RecipeId);
                lines.Add(Loc.F("{0}: kalan {1} vardiya.", recipe.Name, order.Remaining));
            }
            if(Board.AutomaticProduction)lines.Add(Loc.F("Üretim alanı: {0}",AutomaticProductionRecipe?.Name??Loc.T("Uygun tarif yok; malzemeler korunur.")));
            foreach (var person in Board.People.Where(p => p.Alive && p.Job == "train"))
                lines.Add(Loc.F("{0}: {1} alanında +{2} eğitim.", person.Name, PlanningBoardData.SubjectName(person.TrainingSubject), B.boardTrainingGain));
            foreach (var person in Board.People.Where(p => p.Alive))
            {
                string risk = ResidentAdmissionRisk(person.Id);
                if (!string.IsNullOrEmpty(risk)) lines.Add(Loc.F("{0}: {1}", person.Name, risk));
            }
            lines.Add(Loc.T("Açık kapı başvurusu kararsızsa bu kapanışta reddedilir."));
            return string.Join("\n", lines);
        }

        void AdvancePlanningBoard()
        {
            if (Board == null) return;
            double before = Shelter.Supplies;
            double food = BoardFoodBalance;
            var working = new HashSet<string>(Board.People.Where(p => p.Alive && p.Down <= 0 && p.Hp > 0).Select(p => p.Id));
            Shelter.Supplies = Math.Min(B.boardSuppliesCapacity, Math.Max(0, Shelter.Supplies + food));
            Shelter.Starving = before + food < 0;
            Board.LastSuppliesChange = Shelter.Supplies - before;
            Shelter.LastSuppliesChange = Board.LastSuppliesChange;
            Board.LastCompleted.Clear();
            // Bütün saha kitleri kaybedilse de gerideki topluluk yavaşça yeniden sefere hazırlanabilir.
            if (!Shelter.Starving && Board.People.Any(p => working.Contains(p.Id) && p.Job == "gather"))
            {
                Board.GatheringProgress++;
                if (Board.GatheringProgress >= Math.Max(1, B.boardGatherSalvageInterval))
                {
                    Board.GatheringProgress = 0;
                    AddBoardMaterial("metal", B.boardGatherMetal);
                    AddBoardMaterial("devre", B.boardGatherCircuits);
                }
            }
            foreach (var person in Board.People.Where(p => p.Alive).ToList())
            {
                if (person.Down > 0) { person.Down--; person.Hp = Math.Min(person.MaxHp, Math.Max(1, person.Hp) + Math.Max(0, B.boardRestHeal)); continue; }
                if (person.Job == "train")
                {
                    GainBoardExperience(person, person.TrainingSubject, B.boardTrainingGain);
                    person.Job = "rest";
                }
                else if (person.Job == "rest") person.Hp = Math.Min(person.MaxHp, person.Hp + Math.Max(0, B.boardRestHeal));
                else if (person.Job == "cook" || person.Job == "gather") BoardGainExperience(person, person.Job);
            }
            AdvanceBoardProduction(working);
            AdvanceBoardCare(working);
            Shelter.Population = Board.People.Count(p => p.Alive);
            SyncBoardMaterialTotal();
            Board.Shift = S.Day;
            // Vadesinin son vardiyasını çalışan kişi yeni planda karar bekler; gizlice süresiz çalışmaz.
            foreach (var person in Board.People.Where(p => p.Alive && p.Restricted))
                if ((Admission?.File(person.Id)?.RestrictedUntil ?? -1) <= S.Day) person.Job = "rest";
            Board.Locked = false;
            Board.Printed = false;
            Board.Filing = false;
            Board.ReceiptId = null;
            Board.ReceiptText = null;
            Board.RaidSummary = null;
        }

        void AdvanceBoardProduction(HashSet<string> working)
        {
            var producer = Board.People.FirstOrDefault(p => working.Contains(p.Id) && p.Job == "craft");
            var order = Board.Orders.FirstOrDefault();
            if(producer==null||Shelter.Starving||Board.Equipment.Count>=B.boardEquipmentCapacity)return;
            if(order==null)
            {
                if(!Board.AutomaticProduction)return;
                var automatic=AutomaticProductionRecipe;if(automatic==null)return;
                if(automatic.Inputs.Any(m=>(Board.Material(m.Kind)?.Quantity??0)<m.Quantity))return;
                foreach(var input in automatic.Inputs){Board.Material(input.Kind).Quantity-=input.Quantity;Board.ProductionInputs.First(i=>i.Kind==input.Kind).Quantity-=input.Quantity;}
                AddBoardEquipment(automatic.Output,null);Board.LastCompleted.Add(automatic.Output);BoardGainExperience(producer,"craft");return;
            }
            var recipe = BoardRecipes.FirstOrDefault(r => r.Id == order.RecipeId);
            if (recipe == null) return;
            if (!order.Started)
            {
                foreach (var input in recipe.Inputs)
                    if (order.Reserves.Where(r => r.Kind == input.Kind).Sum(r => r.Quantity) != input.Quantity || (Board.Material(input.Kind)?.Quantity ?? 0) < input.Quantity) return;
                foreach (var input in recipe.Inputs) Board.Material(input.Kind).Quantity -= input.Quantity;
                order.Started = true;
            }
            int progress = 1 + (BoardSkillTier(producer, "craft") >= B.boardCraftSpeedTier ? Math.Max(0, B.boardCraftExtraProgress) : 0);
            order.Remaining = Math.Max(0, order.Remaining - progress);
            BoardGainExperience(producer, "craft");
            if (order.Remaining > 0) return;
            AddBoardEquipment(recipe.Output, null);
            Board.LastCompleted.Add(recipe.Output);
            Board.Orders.Remove(order);
        }

        void AdvanceBoardCare(HashSet<string> working)
        {
            var carer = Board.People.FirstOrDefault(p => working.Contains(p.Id) && p.Job == "care");
            if (carer == null || Shelter.Starving) return;
            var patient = Board.People.Where(p => p.Alive && p.Id != carer.Id && p.Hp < p.MaxHp)
                .OrderByDescending(p => p.Wounds).ThenBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault();
            if (patient == null) return;
            int heal = Math.Max(0, B.boardCareHeal + BoardSkillTier(carer, "care") * B.boardCareHealPerTier);
            var medkit = Board.Equipment.FirstOrDefault(e => e.Kind == "medkit" && e.Charges > 0 && (e.OwnerId == null || e.OwnerId == carer.Id));
            if (medkit != null) { heal += Math.Max(0, B.boardMedkitHeal); Board.Equipment.Remove(medkit); }
            if (heal <= 0) return;
            patient.Hp = Math.Min(patient.MaxHp, patient.Hp + heal);
            BoardGainExperience(carer, "care");
        }

        public void AddBoardMaterial(string kind, int amount)
        {
            if (Board == null || !PlanningBoardData.MaterialKinds.Contains(kind)) return;
            var material = Board.Material(kind);
            if (material == null) { material = new BoardMaterial { Kind = kind }; Board.Materials.Add(material); }
            material.Quantity = Math.Min(Math.Max(0, B.boardMaterialsCapacity), material.Quantity + Math.Max(0, amount));
            SyncBoardMaterialTotal();
        }

        public BoardEquipment AddBoardEquipment(string kind, string ownerId = null)
        {
            if (Board == null || Board.Equipment.Count >= B.boardEquipmentCapacity) return null;
            var equipment = new BoardEquipment { Id = "esya-" + (Board.NextEquipmentId++).ToString(CultureInfo.InvariantCulture), Kind = kind, OwnerId = ownerId,
                Charges = kind == "emp_cell" ? Math.Max(1, B.boardEmpCellCharges) : 1 };
            Board.Equipment.Add(equipment);
            return equipment;
        }

        void SyncBoardMaterialTotal()
        {
            if (Board != null) Shelter.Materials = Board.Materials.Sum(m => m.Quantity);
        }
    }
}
