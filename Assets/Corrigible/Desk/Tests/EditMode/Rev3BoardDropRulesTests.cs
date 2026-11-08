using System.Collections.Generic;
using System.Linq;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 3 kartlı panonun bırakma kuralları (PLAN §4.7, §5.1): her hareket doğru türde ve doğru bağımsız
    /// değişkenlerle tek bir Core komutudur; takas tek komuttur; yanlış kart türü ve aynı yer komutsuzdur; kilitli planda bırakma
    /// reddedilir ve hiçbir şey kayda girmez; kısıtlı kişi için yalnız BOŞTA geçerlidir. <c>BoardDropRules.Valid</c>
    /// Core'un sonucunu önceden bilir ve gerekçesi Core'un iletisidir. Aşama 5 · [B]: stok kartı iş emrine <c>board_reserve</c>
    /// (ayrılan + 1), fiş depoya <c>board_reserve</c> (ayrılan − 1); eski konmuş fiş <c>board_stage</c> ile döner (tarif yolları
    /// <c>Rev3RecipeDragTests</c>'te).
    /// </summary>
    public sealed class Rev3BoardDropRulesTests
    {
        const string LockedMessage = "Plan kilitli. Fişi devir kutusuna bırak.";
        static readonly string[] PocketJobs = { "tek", "emp", "ana", "craft", "cook", "train" };

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- kurulum (Rev3BoardSnapshotTests ile aynı)

        static Simulation Ready(uint seed = 79) => Simulation.NewGame(seed, new Balance
        {
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
        });

        /// <summary>Sunucunun ilk açılıştaki <c>RetireJobs</c>'u: otomatik üretim ve emekli görevlerin BOŞTA'ya alınması.</summary>
        static Simulation Retired(uint seed = 79)
        {
            var sim = Ready(seed);
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "rest").Ok);
            return sim;
        }

        /// <summary>Karışık kadro: 5 kısıtlı, 6 yatıyor, 7 Üretim eğitiminde, 0 EMP'de usta. Cepler 0–4'te (tek emp ana craft cook).</summary>
        static Simulation Mixed()
        {
            var sim = Retired();
            Assert.IsTrue(sim.RestrictResident(At(sim, 5).Id, true).Ok);
            Assert.IsTrue(sim.TrainResident(At(sim, 7).Id, "craft").Ok);
            At(sim, 6).Down = 2;                                    // test düzeni: yatan yaralı
            SetSkill(At(sim, 0), "emp", sim.B.boardExpertAt);       // test düzeni: konuda usta
            return sim;
        }

        static ResidentState At(Simulation sim, int index) => sim.Board.People[index];

        static void SetSkill(ResidentState p, string subject, int points)
        {
            var skill = p.Experience.FirstOrDefault(e => e.Subject == subject);
            if (skill == null) p.Experience.Add(new ResidentSkill { Subject = subject, Points = points });
            else skill.Points = points;
        }

        static BoardEquipment Item(Simulation sim, string kind, string ownerId) =>
            sim.Board.Equipment.First(e => e.Kind == kind && e.OwnerId == ownerId);

        static int Metal(Simulation sim) => sim.Board.Material("metal")?.Quantity ?? 0;

        static BoardTarget Deck() => new BoardTarget(BoardTargetKind.Deck);

        /// <summary>Bırakmayı sunucunun yaptığı gibi yürütür; geçerli ve tek komut olmalı. Yazılan kaydı döner.</summary>
        static SavedCommand Drop(Simulation sim, BoardCardKind kind, string id, BoardTarget t, BoardUiState ui = null)
        {
            string label = kind + " " + id + " → " + t;
            Assert.IsTrue(BoardDropRules.Valid(kind, id, t, sim, ui), label + ": " + BoardDropRules.Reason(kind, id, t, sim, ui));
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(kind, id, t, sim), label);
            var command = BoardDropRules.CommandFor(kind, id, t, sim, ui);
            Assert.IsNotNull(command, label);
            int before = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;
            var result = command(sim);
            Assert.IsTrue(result.Ok, label + ": " + result.Msg);
            Assert.AreEqual(before + 1, sim.Record.commands.Count, label + ": bir bırakma tek komuttur.");
            Assert.AreEqual(rng, sim.S.Rng.State, label + ": pano komutları RNG çekmez.");
            var c = sim.Record.commands[sim.Record.commands.Count - 1];
            Assert.AreEqual(sim.S.Day, c.day);
            return c;
        }

        static void AssertCommand(SavedCommand c, string kind, string a, string b)
        {
            Assert.AreEqual(kind, c.kind);
            Assert.AreEqual(a, c.a);
            Assert.AreEqual(b, c.b);
        }

        /// <summary>Komutsuz bırakma: komut yok, geçersiz (yerleşim dışı), ön koşul gerekçesi yok.</summary>
        static void AssertNoCommand(Simulation sim, BoardCardKind kind, string id, BoardTarget t, BoardUiState ui = null)
        {
            string label = kind + " " + id + " → " + t;
            Assert.IsNull(BoardDropRules.CommandFor(kind, id, t, sim, ui), label);
            Assert.IsFalse(BoardDropRules.Valid(kind, id, t, sim, ui), label);
            Assert.IsTrue(BoardDropRules.Plan(kind, id, t, sim, ui).IsNone, label);
            Assert.IsNull(BoardDropRules.Reason(kind, id, t, sim, ui), label);
        }

        /// <summary>Reddedilen bırakma: komut kurulur ama geçersizdir; çalıştırılırsa Core aynı iletiyle reddeder, kayıt ve durum değişmez.</summary>
        static void AssertRefused(Simulation sim, BoardCardKind kind, string id, BoardTarget t, string message, BoardUiState ui = null)
        {
            string label = kind + " " + id + " → " + t;
            Assert.IsFalse(BoardDropRules.Valid(kind, id, t, sim, ui), label);
            Assert.AreEqual(message, BoardDropRules.Reason(kind, id, t, sim, ui), label);
            var command = BoardDropRules.CommandFor(kind, id, t, sim, ui);
            Assert.IsNotNull(command, label);
            string before = State(sim);
            var result = command(sim);
            Assert.IsFalse(result.Ok, label);
            Assert.AreEqual(message, result.Msg, label);
            Assert.AreEqual(before, State(sim), label + ": reddedilen komut kayda girmez, durumu değiştirmez.");
        }

        static string State(Simulation sim)
        {
            var b = sim.Board;
            var sb = new StringBuilder();
            sb.Append(sim.S.Rng.State).Append('|').Append(sim.Record.commands.Count).Append('|').Append(b.Locked).Append(b.Printed).Append('|');
            foreach (var p in b.People) sb.Append(p.Id).Append(p.Job).Append(p.TrainingSubject).Append(p.Restricted).Append(';');
            foreach (var e in b.Equipment) sb.Append(e.Id).Append(e.OwnerId).Append(';');
            foreach (var m in b.Materials) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var m in b.ProductionInputs) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var o in b.Orders)
            {
                sb.Append(o.Id).Append(o.RecipeId).Append(o.Remaining).Append(o.Started).Append(':');
                foreach (var r in o.Reserves) sb.Append(r.Kind).Append(r.Quantity).Append(',');
                sb.Append(';');
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- kişi → cep / BOŞTA

        [Test]
        public void Person_ToAnEmptyPocketAndToBosta_IsOneAssign()
        {
            var sim = Retired();
            var cook = At(sim, 4);
            var idle = At(sim, 5);
            AssertCommand(Drop(sim, BoardCardKind.Person, cook.Id, BoardTarget.Bosta()), "board_assign", cook.Id, "rest");
            Assert.AreEqual("rest", cook.Job);
            AssertCommand(Drop(sim, BoardCardKind.Person, idle.Id, BoardTarget.Pocket("cook")), "board_assign", idle.Id, "cook");
            Assert.AreEqual("cook", idle.Job);
            Assert.AreEqual("rest", cook.Job, "Boş cebe bırakmada takas yok.");

            // Deste hedefi BOŞTA ile aynıdır; cep dikdörtgeni "rest" görevini de taşıyabilir
            AssertCommand(Drop(sim, BoardCardKind.Person, idle.Id, Deck()), "board_assign", idle.Id, "rest");
            AssertCommand(Drop(sim, BoardCardKind.Person, At(sim, 3).Id, BoardTarget.Pocket("rest")), "board_assign", At(sim, 3).Id, "rest");
        }

        [Test]
        public void Person_OnAnOccupiedPocket_SwapIsOneAssign_ItemsStayWithTheirOwners()
        {
            var sim = Retired();
            var tek = At(sim, 0);
            var idle = At(sim, 5);
            var kit = Item(sim, "kit_tek", tek.Id);
            AssertCommand(Drop(sim, BoardCardKind.Person, idle.Id, BoardTarget.Pocket("tek")), "board_assign", idle.Id, "tek");
            Assert.AreEqual("tek", idle.Job);
            Assert.AreEqual("rest", tek.Job, "Cepteki kişi gelenin eski yerine (BOŞTA) geçer.");
            Assert.AreEqual(tek.Id, kit.OwnerId, "Takasta eşyalar sahibinde kalır.");

            // İki cep arası takas: emp ↔ ana, yine tek komut
            var emp = At(sim, 1);
            var ana = At(sim, 2);
            AssertCommand(Drop(sim, BoardCardKind.Person, emp.Id, BoardTarget.Pocket("ana")), "board_assign", emp.Id, "ana");
            Assert.AreEqual("ana", emp.Job);
            Assert.AreEqual("emp", ana.Job);
        }

        [Test]
        public void Person_OnAnotherPersonCard_MeansThatPersonsPlace()
        {
            var sim = Retired();
            var craft = At(sim, 3);
            var cook = At(sim, 4);
            AssertCommand(Drop(sim, BoardCardKind.Person, craft.Id, BoardTarget.Person(cook.Id)), "board_assign", craft.Id, "cook");
            Assert.AreEqual("cook", craft.Job);
            Assert.AreEqual("craft", cook.Job);

            // Destedeki bir kartın üstü BOŞTA'dır: takas yok, yalnız gelen dinlenmeye geçer
            var idle = At(sim, 6);
            AssertCommand(Drop(sim, BoardCardKind.Person, cook.Id, BoardTarget.Person(idle.Id)), "board_assign", cook.Id, "rest");
            Assert.AreEqual("rest", idle.Job);
            Assert.AreEqual("rest", cook.Job);
        }

        // ---------------------------------------------------------------- eğitim

        [Test]
        public void Person_ToTraining_IsOneTrain_SubjectFromRowThenTraineeThenUi()
        {
            var sim = Retired();
            var a = At(sim, 5);
            var b = At(sim, 6);
            var c = At(sim, 7);

            // Kimse eğitimde değil: konu sunumun seçtiği konu, yoksa "tek"
            Assert.AreEqual("tek", BoardDropRules.TrainingSubject(sim, null));
            Assert.AreEqual("ana", BoardDropRules.TrainingSubject(sim, new BoardUiState { TrainingTopic = "ana" }));
            AssertCommand(Drop(sim, BoardCardKind.Person, c.Id, BoardTarget.Pocket("train"), new BoardUiState { TrainingTopic = "ana" }),
                "board_train", c.Id, "ana");
            Assert.AreEqual("train", c.Job);
            Assert.AreEqual("ana", c.TrainingSubject);

            // Biri eğitimdeyse cebin konusu onun konusudur; eski eğitimli gelenin eski yerine geçer (takas, tek komut)
            Assert.AreEqual("ana", BoardDropRules.TrainingSubject(sim, new BoardUiState { TrainingTopic = "cook" }));
            AssertCommand(Drop(sim, BoardCardKind.Person, b.Id, BoardTarget.Pocket("train"), new BoardUiState { TrainingTopic = "cook" }),
                "board_train", b.Id, "ana");
            Assert.AreEqual("train", b.Job);
            Assert.AreEqual("rest", c.Job);

            // Konu satırına bırakma o konuyu seçer; satırlı cep hedefi de satırın konusunu alır
            AssertCommand(Drop(sim, BoardCardKind.Person, a.Id, BoardTarget.Topic(4)), "board_train", a.Id, "cook");
            Assert.AreEqual("cook", a.TrainingSubject);
            Assert.AreEqual("rest", b.Job);
            AssertCommand(Drop(sim, BoardCardKind.Person, At(sim, 3).Id, new BoardTarget(BoardTargetKind.Pocket, "train", null, 0)),
                "board_train", At(sim, 3).Id, "tek");
            Assert.AreEqual("craft", a.Job, "Önceki eğitimli gelenin cebine (Üretim) geçer.");

            // Eğitimdeki kişinin kendi satırı aynı yerdir; başka satırı konu değişimidir
            var trainee = At(sim, 3);
            AssertNoCommand(sim, BoardCardKind.Person, trainee.Id, BoardTarget.Topic(0));
            AssertNoCommand(sim, BoardCardKind.Person, trainee.Id, BoardTarget.Pocket("train"));
            AssertCommand(Drop(sim, BoardCardKind.Person, trainee.Id, BoardTarget.Topic(2)), "board_train", trainee.Id, "ana");
        }

        [Test]
        public void TopicRowClick_WithATrainee_IsOneTrain_OtherwiseNoCommand()
        {
            var sim = Retired();
            Assert.IsNull(BoardDropRules.TopicCommand(4, sim), "Kimse eğitimde değilken satır yalnız sunumun konusunu değiştirir.");
            var trainee = At(sim, 7);
            Assert.IsTrue(sim.TrainResident(trainee.Id, "craft").Ok);
            Assert.IsNull(BoardDropRules.TopicCommand(3, sim), "Aynı konu komutsuz.");
            Assert.IsNull(BoardDropRules.TopicCommand(-1, sim));
            Assert.IsNull(BoardDropRules.TopicCommand(PlanningBoardData.Subjects.Length, sim));
            var command = BoardDropRules.TopicCommand(4, sim);
            Assert.IsNotNull(command);
            int before = sim.Record.commands.Count;
            Assert.IsTrue(command(sim).Ok);
            Assert.AreEqual(before + 1, sim.Record.commands.Count);
            AssertCommand(sim.Record.commands[before], "board_train", trainee.Id, "cook");
            Assert.AreEqual("cook", trainee.TrainingSubject);
            Assert.AreEqual("train", trainee.Job);
        }

        // ---------------------------------------------------------------- eşya

        [Test]
        public void Item_ToAPersonCard_IsOneEquip_SameSlotSwapIsOneCommand()
        {
            var sim = Retired();
            var tek = At(sim, 0);
            var emp = At(sim, 1);
            var medkit = Item(sim, "medkit", null);
            AssertCommand(Drop(sim, BoardCardKind.Item, medkit.Id, BoardTarget.Person(tek.Id)), "board_equip", medkit.Id, tek.Id);
            Assert.AreEqual(tek.Id, medkit.OwnerId);

            // Aynı yuvadaki eski eşya yeni eşyanın önceki sahibine geçer (iki kişilik takas, tek komut)
            var kitTek = Item(sim, "kit_tek", tek.Id);
            var kitEmp = Item(sim, "kit_emp", emp.Id);
            AssertCommand(Drop(sim, BoardCardKind.Item, kitTek.Id, BoardTarget.Person(emp.Id)), "board_equip", kitTek.Id, emp.Id);
            Assert.AreEqual(emp.Id, kitTek.OwnerId);
            Assert.AreEqual(tek.Id, kitEmp.OwnerId);
        }

        [Test]
        public void Item_OnAPocket_GoesToItsOccupant_ToDepoAndHurda()
        {
            var sim = Retired();
            var tek = At(sim, 0);
            var ana = At(sim, 2);
            var cook = At(sim, 4);
            var medkit = Item(sim, "medkit", null);
            AssertCommand(Drop(sim, BoardCardKind.Item, medkit.Id, BoardTarget.Pocket("cook")), "board_equip", medkit.Id, cook.Id);
            Assert.AreEqual(cook.Id, medkit.OwnerId);

            var armor = Item(sim, "armor", tek.Id);
            AssertCommand(Drop(sim, BoardCardKind.Item, armor.Id, BoardTarget.Pocket("ana")), "board_equip", armor.Id, ana.Id);
            Assert.AreEqual(ana.Id, armor.OwnerId);

            // DEPO: sahibinden depoya (personId null kaydedilir)
            AssertCommand(Drop(sim, BoardCardKind.Item, armor.Id, BoardTarget.Store()), "board_equip", armor.Id, null);
            Assert.IsNull(armor.OwnerId);

            // HURDA: kişideki eşya da sökülür; +1 metal
            var kitAna = Item(sim, "kit_ana", ana.Id);
            int metal = Metal(sim);
            int count = sim.Board.Equipment.Count;
            AssertCommand(Drop(sim, BoardCardKind.Item, kitAna.Id, BoardTarget.Hurda()), "board_scrap", kitAna.Id, null);
            Assert.IsNull(sim.Board.EquipmentItem(kitAna.Id));
            Assert.AreEqual(count - 1, sim.Board.Equipment.Count);
            Assert.AreEqual(metal + sim.B.boardScrapMetal, Metal(sim));
        }

        // ---------------------------------------------------------------- hammadde ve fişler

        [Test]
        public void Material_ToOrder_IsAbsoluteReserve()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            int free = sim.BoardMaterialFree("metal");
            Assert.AreEqual(sim.B.boardStartMetal, free);
            AssertCommand(Drop(sim, BoardCardKind.Material, "metal", BoardTarget.Order()), "board_reserve", "metal", "1");
            AssertCommand(Drop(sim, BoardCardKind.Material, "metal", BoardTarget.Order()), "board_reserve", "metal", "2");
            AssertCommand(Drop(sim, BoardCardKind.Material, "kumas", BoardTarget.Order()), "board_reserve", "kumas", "1");
            Assert.AreEqual(2, BoardDropRules.Reserved(sim.Board, "metal"));
            Assert.AreEqual(2, BoardDropRules.ChipCount(sim.Board, "metal"));
            Assert.AreEqual(0, BoardDropRules.Staged(sim.Board, "metal"), "B'de konmuş fiş yazılmaz.");
            Assert.AreEqual(free - 2, sim.BoardMaterialFree("metal"));
            Assert.IsNull(sim.BoardPlanCheck(), "Tarifin girdileri tam: plan açık.");

            // Önizleme: başlık İŞ EMRİ ← tür, gövde serbest sayısı
            var p = BoardPreviewText.For(BoardCardKind.Material, "devre", BoardTarget.Order(), sim);
            Assert.AreEqual("İŞ EMRİ ← DEVRE", p.Title);
        }

        [Test]
        public void Chip_ToDepot_IsReserveMinusOne()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("metal", 2).Ok);
            AssertCommand(Drop(sim, BoardCardKind.Chip, BoardUiState.ChipId("metal", 1), BoardTarget.Store()), "board_reserve", "metal", "1");
            Assert.AreEqual(1, BoardDropRules.Reserved(sim.Board, "metal"));
            AssertNoCommand(sim, BoardCardKind.Chip, BoardUiState.ChipId("metal", 1), BoardTarget.Store());
            var p = BoardPreviewText.For(BoardCardKind.Chip, BoardUiState.ChipId("metal", 0), BoardTarget.Store(), sim);
            Assert.AreEqual("DEPO ← METAL", p.Title);
            AssertCommand(Drop(sim, BoardCardKind.Chip, BoardUiState.ChipId("metal", 0), BoardTarget.Store()), "board_reserve", "metal", "0");
            Assert.AreEqual(0, BoardDropRules.ChipCount(sim.Board, "metal"));
            Assert.AreEqual(sim.B.boardStartMetal, sim.BoardMaterialFree("metal"));
        }

        [Test]
        public void LegacyStagedChip_ComesFirst_AndReturnsWithStage()
        {
            // Eski (A dönemi) kayıt: konmuş fiş + yeni emir; fiş sırası önce konmuşlar, sonra ayrılanlar
            var sim = Retired();
            Assert.IsTrue(sim.StageProductionMaterial("metal", 1).Ok);
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("metal", 1).Ok);
            Assert.AreEqual(2, BoardDropRules.ChipCount(sim.Board, "metal"));
            AssertCommand(Drop(sim, BoardCardKind.Chip, BoardUiState.ChipId("metal", 0), BoardTarget.Store()), "board_stage", "metal", "0");
            Assert.AreEqual(1, BoardDropRules.ChipCount(sim.Board, "metal"));
            AssertCommand(Drop(sim, BoardCardKind.Chip, BoardUiState.ChipId("metal", 0), BoardTarget.Store()), "board_reserve", "metal", "0");
            Assert.AreEqual(0, BoardDropRules.ChipCount(sim.Board, "metal"));
        }

        [Test]
        public void Chip_MovedOnTheOrderSheet_IsLayoutOnly_NothingRecorded()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("metal", 1).Ok);
            string chip = BoardUiState.ChipId("metal", 0);
            string before = State(sim);
            Assert.IsTrue(BoardDropRules.IsLayoutOnly(BoardCardKind.Chip, chip, BoardTarget.Order(), sim));
            Assert.IsTrue(BoardDropRules.Valid(BoardCardKind.Chip, chip, BoardTarget.Order(), sim));
            Assert.IsNull(BoardDropRules.CommandFor(BoardCardKind.Chip, chip, BoardTarget.Order(), sim));
            Assert.AreEqual(before, State(sim));
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Chip, BoardUiState.ChipId("metal", 1), BoardTarget.Order(), sim), "Fazladan fiş yok.");
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Material, "metal", BoardTarget.Order(), sim));

            // Eski konmuş fiş de kâğıt üstünde yer değiştirebilir
            var legacy = Retired();
            Assert.IsTrue(legacy.StageProductionMaterial("metal", 1).Ok);
            Assert.IsTrue(BoardDropRules.IsLayoutOnly(BoardCardKind.Chip, chip, BoardTarget.Order(), legacy));
        }

        [Test]
        public void NoOrder_MaterialRefused_CoreMessage()
        {
            var sim = Retired();
            Assert.Greater(sim.BoardMaterialFree("metal"), 0);
            AssertRefused(sim, BoardCardKind.Material, "metal", BoardTarget.Order(), "Girdisi ayrılabilen bir taslak iş yok.");
            Assert.IsNull(BoardDropRules.CanPick(BoardCardKind.Material, "metal", sim), "Serbest var: kart kalkar, bırakış reddedilir (yetersiz kaynak değil).");

            // Otomatik üretimsiz (eski) kayıtta da aynı yol
            var manual = Ready();
            Assert.IsFalse(manual.Board.AutomaticProduction);
            AssertRefused(manual, BoardCardKind.Material, "metal", BoardTarget.Order(), "Girdisi ayrılabilen bir taslak iş yok.");
        }

        [Test]
        public void Material_NotInRecipe_OrFull_Refused()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            AssertRefused(sim, BoardCardKind.Material, "devre", BoardTarget.Order(), "Bu tarif için uygun tür ve miktarı ayır.");
            AssertRefused(sim, BoardCardKind.Material, "kimyasal", BoardTarget.Order(), "Bu tarif için uygun tür ve miktarı ayır.");
            Assert.IsTrue(sim.ReserveProductionMaterial("kumas", sim.B.boardArmorCloth).Ok);
            AssertRefused(sim, BoardCardKind.Material, "kumas", BoardTarget.Order(), "Bu tarif için uygun tür ve miktarı ayır.");

            // Başlamış emir ayırma almaz; fiş de çizilmez (girdiler ödendi)
            var started = Ready();
            Assert.IsTrue(started.QueueProduction("emp_cell").Ok);
            Assert.IsTrue(started.ReserveProductionMaterial("metal", started.B.boardEmpMetal).Ok);
            Assert.IsTrue(started.ReserveProductionMaterial("devre", started.B.boardEmpCircuits).Ok);
            Assert.IsTrue(started.PrintShift().Ok, started.BoardPlanCheck());
            Assert.IsTrue(started.FileShift(started.Board.ReceiptId).Ok);
            Assert.IsTrue(started.Board.Orders[0].Started, "EMP hücresi iki vardiya sürer.");
            Assert.AreEqual(0, BoardDropRules.ChipCount(started.Board, "metal"));
            AssertRefused(started, BoardCardKind.Material, "metal", BoardTarget.Order(), "Girdisi ayrılabilen bir taslak iş yok.");
            AssertNoCommand(started, BoardCardKind.Chip, BoardUiState.ChipId("metal", 0), BoardTarget.Store());
        }

        [Test]
        public void Material_WithoutAFreeUnit_IsRefused_AndCannotBePicked()
        {
            var sim = Retired();
            Assert.IsTrue(sim.QueueProduction("medkit").Ok);
            Assert.IsTrue(sim.ReserveProductionMaterial("kimyasal", 1).Ok);
            Assert.AreEqual(0, sim.BoardMaterialFree("kimyasal"));
            Assert.AreEqual("Serbest hammadde yetersiz.", BoardDropRules.CanPick(BoardCardKind.Material, "kimyasal", sim));

            // Tarifin istediği türde serbest 0 ve ihtiyaç dolmamış: Core "Serbest hammadde yetersiz." der
            var tight = Retired();
            Assert.IsTrue(tight.StageProductionMaterial("metal", tight.BoardMaterialFree("metal")).Ok);   // eski konmuş fiş serbesti yer
            Assert.IsTrue(tight.QueueProduction("armor").Ok);
            AssertRefused(tight, BoardCardKind.Material, "metal", BoardTarget.Order(), "Serbest hammadde yetersiz.");
        }

        // ---------------------------------------------------------------- dosya sekmeleri

        [Test]
        public void FolderTabs_AreOneCommandEach_ReviewNeedsAnIdleWatcher()
        {
            var sim = Retired();
            var p = At(sim, 5);
            int before = sim.Record.commands.Count;
            Assert.IsTrue(BoardDropRules.AccessCommand(p.Id, sim)(sim).Ok);
            AssertCommand(sim.Record.commands[before], "restrictResident", p.Id, "1");
            Assert.IsTrue(p.Restricted);
            Assert.IsTrue(BoardDropRules.AccessCommand(p.Id, sim)(sim).Ok);
            AssertCommand(sim.Record.commands[before + 1], "restrictResident", p.Id, "0");
            Assert.IsFalse(p.Restricted);

            // K4-A (biçim 15): Boşta'da hedeften ayrı uygun kişi var (5 serbest bırakıldı → rest, 6 emekli) → İNCELE tek bir
            // reviewResident komutudur
            var q = At(sim, 7);
            Assert.IsNotNull(sim.AdmissionWatcher(q.Id));
            Assert.IsNull(sim.ResidentReviewCheck(q.Id));
            int reviewAt = sim.Record.commands.Count;
            uint rng = sim.S.Rng.State;
            var review = BoardDropRules.ReviewCommand(q.Id)(sim);
            Assert.IsTrue(review.Ok, review.Msg);
            Assert.AreEqual(reviewAt + 1, sim.Record.commands.Count, "İNCELE tek komuttur");
            AssertCommand(sim.Record.commands[reviewAt], "reviewResident", q.Id, null);
            Assert.AreEqual(rng, sim.S.Rng.State, "İNCELE RNG çekmez");
            Assert.AreEqual(q.Id, sim.Admission.ObservationTarget);
            Assert.IsTrue(sim.Admission.File(q.Id).ReviewPending);

            int count = sim.Record.commands.Count;
            Assert.IsTrue(BoardDropRules.DismissCommand(q.Id)(sim).Ok);
            Assert.AreEqual(count + 1, sim.Record.commands.Count);
            AssertCommand(sim.Record.commands[count], "dismissResident", q.Id, null);
            Assert.IsNull(sim.Board.Person(q.Id));

            Assert.IsNull(BoardDropRules.ReviewCommand(null));
            Assert.IsNull(BoardDropRules.DismissCommand(""));
            Assert.IsNull(BoardDropRules.AccessCommand("yok-1", sim));

            // Boşta boş (hedef dışındaki uygunlar gerçek komutlarla cebe alınır): İNCELE reddedilir, ileti Core'un, kayıt yok
            var empty = Retired();
            var target = At(empty, 7);
            string[] away = { "watch", "contact", "train" };
            var idle = empty.Board.People.Where(x => x.Alive && x.Id != target.Id && x.Job == "rest" && !x.Restricted && x.Down <= 0).ToList();
            Assert.LessOrEqual(idle.Count, away.Length);
            for (int i = 0; i < idle.Count; i++) Assert.IsTrue(empty.AssignResident(idle[i].Id, away[i]).Ok, idle[i].Name + " → " + away[i]);
            string why = empty.ResidentReviewCheck(target.Id);
            Assert.AreEqual("Gözetmen yok: dış kayıt için Boşta'da ayrı, sağlam ve kısıtsız bir sakin bırak.", why);
            string state = State(empty);
            var refused = BoardDropRules.ReviewCommand(target.Id)(empty);
            Assert.IsFalse(refused.Ok);
            Assert.AreEqual(why, refused.Msg);
            Assert.AreEqual(state, State(empty), "reddedilen İNCELE kayda girmez, durumu değiştirmez");
        }

        // ---------------------------------------------------------------- komutsuz bırakmalar

        [Test]
        public void SameTarget_HasNoCommand()
        {
            var sim = Retired();
            Assert.IsTrue(sim.StageProductionMaterial("metal", 1).Ok);
            var tek = At(sim, 0);
            var idle = At(sim, 5);
            AssertNoCommand(sim, BoardCardKind.Person, tek.Id, BoardTarget.Pocket("tek"));
            AssertNoCommand(sim, BoardCardKind.Person, tek.Id, BoardTarget.Person(tek.Id));
            AssertNoCommand(sim, BoardCardKind.Person, idle.Id, BoardTarget.Bosta());
            AssertNoCommand(sim, BoardCardKind.Person, idle.Id, Deck());
            AssertNoCommand(sim, BoardCardKind.Person, idle.Id, BoardTarget.Person(At(sim, 6).Id));
            AssertNoCommand(sim, BoardCardKind.Item, Item(sim, "kit_tek", tek.Id).Id, BoardTarget.Person(tek.Id));
            AssertNoCommand(sim, BoardCardKind.Item, Item(sim, "kit_tek", tek.Id).Id, BoardTarget.Pocket("tek"));
            AssertNoCommand(sim, BoardCardKind.Item, Item(sim, "medkit", null).Id, BoardTarget.Store());
            AssertNoCommand(sim, BoardCardKind.Material, "metal", BoardTarget.Store());
        }

        [Test]
        public void WrongCardKindOrOutside_HasNoCommand()
        {
            var sim = Retired();
            Assert.IsTrue(sim.StageProductionMaterial("metal", 1).Ok);
            var tek = At(sim, 0);
            var medkit = Item(sim, "medkit", null).Id;
            var outside = new[]
            {
                BoardTarget.None, new BoardTarget(BoardTargetKind.Empty), new BoardTarget(BoardTargetKind.Back),
                new BoardTarget(BoardTargetKind.Folder),
            };

            // Kişi: DEPO, HURDA, iş emri, pano dışı; geçersiz satır ve bilinmeyen kişi
            foreach (var t in new[] { BoardTarget.Store(), BoardTarget.Hurda(), BoardTarget.Order() }.Concat(outside))
                AssertNoCommand(sim, BoardCardKind.Person, tek.Id, t);
            AssertNoCommand(sim, BoardCardKind.Person, tek.Id, BoardTarget.Topic(-1));
            AssertNoCommand(sim, BoardCardKind.Person, tek.Id, BoardTarget.Topic(PlanningBoardData.Subjects.Length));
            AssertNoCommand(sim, BoardCardKind.Person, tek.Id, BoardTarget.Pocket("watch"));
            AssertNoCommand(sim, BoardCardKind.Person, "yok-1", BoardTarget.Pocket("cook"));
            AssertNoCommand(sim, BoardCardKind.Person, null, BoardTarget.Pocket("cook"));

            // Eşya: boş cep (eğitim), BOŞTA, deste, iş emri, konu, pano dışı; bilinmeyen eşya
            foreach (var t in new[] { BoardTarget.Pocket("train"), BoardTarget.Bosta(), Deck(), BoardTarget.Order(), BoardTarget.Topic(0) }.Concat(outside))
                AssertNoCommand(sim, BoardCardKind.Item, medkit, t);
            AssertNoCommand(sim, BoardCardKind.Item, medkit, BoardTarget.Person("yok-1"));
            AssertNoCommand(sim, BoardCardKind.Item, "esya-999", BoardTarget.Hurda());

            // Stok kartı yalnız iş emrine; fiş yalnız DEPO'ya (iş emri içinde yer değiştirme komutsuzdur)
            foreach (var t in new[] { BoardTarget.Hurda(), BoardTarget.Pocket("tek"), BoardTarget.Person(tek.Id), BoardTarget.Bosta() }.Concat(outside))
                AssertNoCommand(sim, BoardCardKind.Material, "metal", t);
            AssertNoCommand(sim, BoardCardKind.Material, "altin", BoardTarget.Order());
            string chip = BoardUiState.ChipId("metal", 0);
            foreach (var t in new[] { BoardTarget.Hurda(), BoardTarget.Pocket("tek"), BoardTarget.Person(tek.Id) }.Concat(outside))
                AssertNoCommand(sim, BoardCardKind.Chip, chip, t);
            AssertNoCommand(sim, BoardCardKind.Chip, "chip:metal", BoardTarget.Store());
            AssertNoCommand(sim, BoardCardKind.Chip, BoardUiState.ChipId("devre", 0), BoardTarget.Store());

            // [B] Tarif kopyası yalnız iş emrine (kuyruk; Rev3RecipeDragTests), defterine dönüşü ve başka hedefler komutsuz
            Assert.AreEqual(BoardDropAction.Queue, BoardDropRules.Plan(BoardCardKind.Recipe, "kit_tek", BoardTarget.Order(), sim).Action);
            foreach (var t in new[] { BoardTarget.Book(), BoardTarget.Store(), BoardTarget.Hurda(), BoardTarget.Pocket("tek"), BoardTarget.Bosta() }.Concat(outside))
                AssertNoCommand(sim, BoardCardKind.Recipe, "kit_tek", t);
            AssertNoCommand(sim, BoardCardKind.Recipe, "altin_kulce", BoardTarget.Order());

            // Panosuz kayıt
            Assert.IsNull(BoardDropRules.CommandFor(BoardCardKind.Person, tek.Id, BoardTarget.Pocket("cook"), null));
            Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Person, tek.Id, BoardTarget.Pocket("cook"), null));
            Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Chip, chip, BoardTarget.Order(), null));
        }

        // ---------------------------------------------------------------- kilitli plan

        [Test]
        public void LockedPlan_RefusesEveryGesture_NothingRecorded()
        {
            var sim = Retired();
            Assert.IsTrue(sim.StageProductionMaterial("metal", 1).Ok);
            Assert.IsTrue(sim.TrainResident(At(sim, 7).Id, "craft").Ok);
            Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
            Assert.IsTrue(sim.Board.Locked);
            var tek = At(sim, 0);
            string chip = BoardUiState.ChipId("metal", 0);

            AssertRefused(sim, BoardCardKind.Person, At(sim, 5).Id, BoardTarget.Pocket("tek"), LockedMessage);
            AssertRefused(sim, BoardCardKind.Person, At(sim, 5).Id, BoardTarget.Pocket("train"), LockedMessage);
            AssertRefused(sim, BoardCardKind.Person, At(sim, 6).Id, BoardTarget.Person(At(sim, 4).Id), LockedMessage);
            AssertRefused(sim, BoardCardKind.Person, tek.Id, BoardTarget.Bosta(), LockedMessage);
            AssertRefused(sim, BoardCardKind.Person, At(sim, 3).Id, BoardTarget.Topic(4), LockedMessage);
            AssertRefused(sim, BoardCardKind.Item, Item(sim, "medkit", null).Id, BoardTarget.Person(tek.Id), LockedMessage);
            AssertRefused(sim, BoardCardKind.Item, Item(sim, "armor", tek.Id).Id, BoardTarget.Store(), LockedMessage);
            AssertRefused(sim, BoardCardKind.Item, Item(sim, "kit_ana", At(sim, 2).Id).Id, BoardTarget.Hurda(), LockedMessage);
            AssertRefused(sim, BoardCardKind.Material, "metal", BoardTarget.Order(), LockedMessage);
            AssertRefused(sim, BoardCardKind.Chip, chip, BoardTarget.Store(), LockedMessage);

            // Kilitliyken fiş kâğıt üstünde de oynamaz; satır tıklaması da reddedilir
            Assert.IsFalse(BoardDropRules.IsLayoutOnly(BoardCardKind.Chip, chip, BoardTarget.Order(), sim));
            Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Chip, chip, BoardTarget.Order(), sim));
            string state = State(sim);
            var topic = BoardDropRules.TopicCommand(4, sim);
            Assert.IsNotNull(topic);
            Assert.IsFalse(topic(sim).Ok);
            Assert.AreEqual(state, State(sim));
        }

        [Test]
        public void RaidOrdered_RefusesWhileTheRaidRuns_ThenThePlanStaysLocked()
        {
            var sim = Ready();
            var idle = At(sim, 7);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            AssertRefused(sim, BoardCardKind.Person, idle.Id, BoardTarget.Pocket("cook"), "Baskın sürerken plan değiştirilemez.");
            AssertRefused(sim, BoardCardKind.Item, sim.Board.Equipment.First().Id, BoardTarget.Hurda(), "Baskın sürerken plan değiştirilemez.");
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.IsNull(sim.S.Raid);
            AssertRefused(sim, BoardCardKind.Person, idle.Id, BoardTarget.Pocket("cook"), LockedMessage);
        }

        // ---------------------------------------------------------------- kısıtlı ve yatan kişi

        [Test]
        public void RestrictedPerson_OnlyBostaIsValid()
        {
            var sim = Retired();
            var r = At(sim, 5);
            Assert.IsTrue(sim.RestrictResident(r.Id, true).Ok);
            Assert.AreEqual("rest", r.Job, "Kısıtlama kişiyi yedeğe (BOŞTA) alır.");

            // Bugünkü kurallarla hiçbir cep, hiçbir konu açık değil; BOŞTA zaten yeri (komutsuz)
            foreach (var job in PocketJobs)
            {
                Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Person, r.Id, BoardTarget.Pocket(job), sim), job);
                if (job != "train") Assert.AreEqual(sim.AssignResidentCheck(r.Id, job), BoardDropRules.Reason(BoardCardKind.Person, r.Id, BoardTarget.Pocket(job), sim), job);
            }
            Assert.AreEqual("Kısıtlı sakin bu göreve erişemez.", BoardDropRules.Reason(BoardCardKind.Person, r.Id, BoardTarget.Pocket("tek"), sim));
            Assert.AreEqual("Kısıtlı sakin için ayrı gözetmen gerekiyor.", BoardDropRules.Reason(BoardCardKind.Person, r.Id, BoardTarget.Pocket("train"), sim));
            for (int row = 0; row < PlanningBoardData.Subjects.Length; row++)
                Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Person, r.Id, BoardTarget.Topic(row), sim), "satır " + row);
            Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Person, r.Id, BoardTarget.Person(At(sim, 0).Id), sim));
            AssertNoCommand(sim, BoardCardKind.Person, r.Id, BoardTarget.Bosta());

            // Kısıtlı kişiye eşya verilmez
            AssertRefused(sim, BoardCardKind.Item, Item(sim, "medkit", null).Id, BoardTarget.Person(r.Id), "Bu sakin teçhizat kullanamıyor.");

            // Test düzeni: kısıtlı kişi bir ceptedir; geçerli tek hedef BOŞTA
            Assert.IsTrue(sim.AssignResident(At(sim, 4).Id, "rest").Ok);
            r.Job = "cook";
            var valid = PocketJobs.Select(job => BoardTarget.Pocket(job)).Concat(new[] { BoardTarget.Bosta(), Deck() })
                .Where(t => BoardDropRules.Valid(BoardCardKind.Person, r.Id, t, sim)).ToArray();
            CollectionAssert.AreEqual(new[] { BoardTarget.Bosta(), Deck() }, valid);
            AssertCommand(Drop(sim, BoardCardKind.Person, r.Id, BoardTarget.Bosta()), "board_assign", r.Id, "rest");
        }

        [Test]
        public void BedriddenPerson_OnlyBostaIsValid_ItemsMayStillGoToThem()
        {
            var sim = Retired();
            var bed = At(sim, 6);
            bed.Down = 2;   // test düzeni: yatan yaralı
            foreach (var job in PocketJobs)
                Assert.AreEqual("Yatan yaralı önce dinlenmeli.", BoardDropRules.Reason(BoardCardKind.Person, bed.Id, BoardTarget.Pocket(job), sim), job);
            Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Person, bed.Id, BoardTarget.Topic(0), sim));
            AssertNoCommand(sim, BoardCardKind.Person, bed.Id, BoardTarget.Bosta());

            // Cepteki biri yatan yarayla takas olmaz; destedeki kartın üstü BOŞTA olduğu için geçerlidir
            Assert.IsTrue(BoardDropRules.Valid(BoardCardKind.Person, At(sim, 0).Id, BoardTarget.Person(bed.Id), sim));
            var medkit = Item(sim, "medkit", null);
            AssertCommand(Drop(sim, BoardCardKind.Item, medkit.Id, BoardTarget.Person(bed.Id)), "board_equip", medkit.Id, bed.Id);
        }

        // ---------------------------------------------------------------- Valid = Core'un sonucu

        [Test]
        public void Valid_PredictsTheCoreResult_ForEveryPersonAndPlace()
        {
            var sim = Mixed();
            var ids = sim.Board.People.Select(p => p.Id).ToArray();
            var targets = new List<BoardTarget>();
            targets.AddRange(PocketJobs.Select(job => BoardTarget.Pocket(job)));
            targets.Add(BoardTarget.Bosta());
            for (int row = 0; row < PlanningBoardData.Subjects.Length; row++) targets.Add(BoardTarget.Topic(row));
            targets.AddRange(ids.Select(id => BoardTarget.Person(id)));
            var counts = Predict(ref sim, BoardCardKind.Person, ids, targets, Mixed);
            Assert.Greater(counts.accepted, 0);
            Assert.Greater(counts.refused, 0);
        }

        [Test]
        public void Valid_PredictsTheCoreResult_ForEveryItemAndTarget()
        {
            var sim = Mixed();
            var ids = sim.Board.Equipment.Select(e => e.Id).ToArray();
            var targets = new List<BoardTarget> { BoardTarget.Store(), BoardTarget.Hurda() };
            targets.AddRange(PocketJobs.Select(job => BoardTarget.Pocket(job)));
            targets.AddRange(sim.Board.People.Select(p => BoardTarget.Person(p.Id)));
            var counts = Predict(ref sim, BoardCardKind.Item, ids, targets, Mixed);
            Assert.Greater(counts.accepted, 0);
            Assert.Greater(counts.refused, 0);
        }

        /// <summary>
        /// Her (kart, hedef) için: komut yoksa geçersiz; varsa çalıştırılır ve Core'un sonucu <c>Valid</c>'e, reddin iletisi
        /// <c>Reason</c>'a eşit olmalı. Başarılı komut tam bir kayıt satırı yazar; ardından simülasyon baştan kurulur.
        /// </summary>
        static (int accepted, int refused) Predict(ref Simulation sim, BoardCardKind kind, IEnumerable<string> ids, IEnumerable<BoardTarget> targets,
            System.Func<Simulation> fresh)
        {
            int accepted = 0, refused = 0;
            var all = targets.ToArray();
            foreach (var id in ids)
            foreach (var t in all)
            {
                string label = kind + " " + id + " → " + t;
                bool valid = BoardDropRules.Valid(kind, id, t, sim);
                var command = BoardDropRules.CommandFor(kind, id, t, sim);
                if (command == null)
                {
                    Assert.IsFalse(valid, label + ": komutsuz bırakma geçerli sayılmaz.");
                    continue;
                }
                string reason = BoardDropRules.Reason(kind, id, t, sim);
                int before = sim.Record.commands.Count;
                uint rng = sim.S.Rng.State;
                var result = command(sim);
                Assert.AreEqual(valid, result.Ok, label + ": " + result.Msg);
                Assert.AreEqual(rng, sim.S.Rng.State, label);
                if (result.Ok)
                {
                    Assert.IsNull(reason, label);
                    Assert.AreEqual(before + 1, sim.Record.commands.Count, label);
                    accepted++;
                    sim = fresh();
                }
                else
                {
                    Assert.AreEqual(reason, result.Msg, label);
                    Assert.AreEqual(before, sim.Record.commands.Count, label);
                    refused++;
                }
            }
            return (accepted, refused);
        }
    }
}
