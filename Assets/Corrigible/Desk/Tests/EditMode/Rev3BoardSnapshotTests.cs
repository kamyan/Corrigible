using System.Collections.Generic;
using System.Linq;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 3 kartlı panonun anlık görüntüsü (PLAN §4, §5.1): gerçek bir kartlı simülasyondan başlık, kilit nedeni,
    /// şerit önceliği, cepler ve deste, hammadde, kit yığınları, fişler, eşya sırası, ölüm günlüğü, dosya ve konular. Görüntü kurmak
    /// RNG çekmez, komut yazmaz, Core durumunu değiştirmez.
    /// <para>
    /// Aşama 5 · R18 (PLAN W4, [B]): defter sayfaları Core tariflerinden (TR/EN, denge), iş emri formunun durumları (E14), formun
    /// tahmini = gerçek kapanış (E13), ayrılan girdilerden fişler, yeni ürün (E20) ve sunum durumunun eşitlenmesi (E19),
    /// <see cref="ProductionForecast"/> = devir fişinin üretimi.
    /// </para>
    /// </summary>
    public sealed class Rev3BoardSnapshotTests
    {
        const string Minus = "−";

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        static Simulation Ready(uint seed = 79) => Simulation.NewGame(seed, new Balance
        { shelterEventsEnabled = false,
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100
        });

        /// <summary>Sunucunun ilk açılıştaki <c>RetireJobs</c>'u: otomatik üretim ve emekli görevlerin (care, gather, watch, contact) BOŞTA'ya alınması.</summary>
        static Simulation Retired(uint seed = 79)
        {
            var sim = Ready(seed);
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "rest").Ok);
            return sim;
        }

        static BoardSnapshot Build(Simulation sim, BoardUiState ui = null, bool english = false, bool inputBlocked = false, bool storyHold = false) =>
            PlanningBoardSnapshotBuilder.Build(sim, ui ?? new BoardUiState(), english, inputBlocked, storyHold);

        static ResidentState At(Simulation sim, int index) => sim.Board.People[index];

        /// <summary>
        /// Test düzeni: olay kısıtının durumu (kısıtı yalnız olay kararı kurar; eski elle kısıt aşama 7'de silindi). Kişi Boşta'ya iner,
        /// bağlı olmayan eşyası depoya döner.
        /// </summary>
        static void Restrict(Simulation sim, string id)
        {
            var p = sim.Board.Person(id);
            p.Restricted = true;
            p.Job = "rest";
            sim.Admission.File(id).RestrictedUntil = sim.S.Day + 3;
            foreach (var e in sim.Board.Equipment.Where(e => e.OwnerId == id && !e.Bound)) e.OwnerId = null;
        }

        /// <summary>Kadronun ilk kişisi kurtarılır (yatıyor), üçüncüsü kaçışta geride kalır (ölür); plan kilitli kalır.</summary>
        static Simulation RaidWithLoss(out string lost, out string rescued)
        {
            var sim = Ready();
            var squad = sim.PlanningSquad();
            rescued = squad[0].PersonId;
            lost = squad[2].PersonId;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Retreat = true, Hp = { 0, squad[1].Hp, 0 }, Armor = { 0, 1, 0 }, Left = { 2 } }).Ok);
            Assert.IsFalse(sim.Board.Person(lost).Alive);
            Assert.Greater(sim.Board.Person(rescued).Down, 0);
            return sim;
        }

        static string Fingerprint(Simulation sim)
        {
            var b = sim.Board;
            var sb = new StringBuilder();
            sb.Append(sim.S.Rng.State).Append('|').Append(sim.Record.commands.Count).Append('|').Append(sim.S.Day).Append('|')
              .Append(sim.Shelter.Supplies.ToString("R")).Append('|').Append(b.Locked).Append(b.Printed).Append('|');
            foreach (var p in b.People)
                sb.Append(p.Id).Append(p.Job).Append(p.Hp).Append(p.Down).Append(p.Restricted).Append(p.TrainingSubject)
                  .Append(string.Join(",", p.Experience.Select(x => x.Subject + x.Points))).Append(';');
            foreach (var e in b.Equipment) sb.Append(e.Id).Append(e.OwnerId).Append(e.Charges).Append(';');
            foreach (var m in b.Materials) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var m in b.ProductionInputs) sb.Append(m.Kind).Append(m.Quantity).Append(';');
            foreach (var o in b.Orders)
                sb.Append(o.Id).Append(o.RecipeId).Append(o.Remaining).Append(o.Started)
                  .Append(string.Join(",", o.Reserves.Select(r => r.Kind + r.Quantity))).Append(';');
            sb.Append(string.Join(",", b.LastCompleted)).Append('|').Append(b.NextEquipmentId).Append(b.NextOrderId).Append('|');
            var a = sim.Admission;
            sb.Append(a.Group.Count).Append('|').Append(a.Passed.Count).Append('|');
            foreach (var f in a.Files) sb.Append(f.PersonId).Append(f.Departed).Append(f.RestrictedUntil).Append(f.ClueCount).Append(';');
            return sb.ToString();
        }

        // ---------------------------------------------------------------- başlık ve kilit

        [Test]
        public void Header_AtV001_AfterRetire_ReadsDesignValue()
        {
            var s = Build(Retired());
            // Tasarım değeri 2,1 idi (tüketim 0,35); aşama 8 dengesiyle (2026-11-15) 8 × 0,40 − 0,7 = 2,5
            Assert.AreEqual("V.001 · erzak 24 (" + Minus + "2,5) · 8/10 kişi", s.Header);
            Assert.IsFalse(s.Locked);
            Assert.AreEqual("", s.LockReason);
            Assert.AreEqual("PLAN AÇIK · 8/10 kişi", s.EntryLine);
            Assert.AreEqual("1 eşya → 1 metal", s.HurdaPlateLine);
            Assert.AreEqual(s.HurdaPlateLine, s.HurdaLine);
        }

        [Test]
        public void Header_English_UsesDecimalPointAndTheEnglishSentence()
        {
            var sim = Retired();
            Loc.SetLanguage(Loc.English, new Dictionary<string, string>
            {
                { "V.{0:000} · erzak {1} ({2}) · {3}/{4} kişi", "S.{0:000} · rations {1} ({2}) · {3}/{4} people" },
            });
            var s = Build(sim, english: true);
            Assert.IsTrue(s.English);
            Assert.AreEqual("S.001 · rations 24 (" + Minus + "2.5) · 8/10 people", s.Header);
        }

        [Test]
        public void LockReason_RaidOrderedStaysAfterResolve_PrintedShiftWins()
        {
            var sim = Ready();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            var during = Build(sim);
            Assert.IsTrue(during.Locked);
            Assert.IsFalse(during.Editable);
            Assert.AreEqual("baskın emri verildi", during.LockReason);
            Assert.AreEqual("PLAN KİLİTLİ · baskın emri verildi", during.EntryLine);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.IsNull(sim.S.Raid);
            Assert.AreEqual("baskın emri verildi", Build(sim).LockReason, "Baskın çözülünce plan vardiya sonuna kadar kilitli kalır.");

            var printed = Retired();
            Assert.IsTrue(printed.PrintShift().Ok, printed.BoardPlanCheck());
            var s = Build(printed);
            Assert.IsTrue(s.Locked);
            Assert.AreEqual("devir fişi basıldı", s.LockReason);
        }

        [Test]
        public void Editable_FollowsLockInputBlockAndStoryHold()
        {
            var sim = Retired();
            Assert.IsTrue(Build(sim).Editable);
            Assert.IsFalse(Build(sim, inputBlocked: true).Editable);
            Assert.IsFalse(Build(sim, storyHold: true).Editable);
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.IsFalse(Build(sim).Editable);
            var empty = PlanningBoardSnapshotBuilder.Build(null, null, false, false, false);
            Assert.IsFalse(empty.Editable);
            Assert.AreEqual(4, empty.Materials.Count(m => m != null));
            Assert.AreEqual(6, empty.Kits.Count(k => k != null && k.Count == 0));
        }

        [Test]
        public void Build_DrawsNoRandomness_RecordsNothing_ChangesNothing()
        {
            var sim = Retired();
            Assert.IsTrue(sim.StageProductionMaterial("metal", 1).Ok);
            Assert.IsTrue(sim.TrainResident(At(sim, 7).Id, "craft").Ok);
            string before = Fingerprint(sim);
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count;
            var ui = new BoardUiState { SelectedId = At(sim, 0).Id, TrainingTopic = "cook" };
            var first = Build(sim, ui);
            Build(sim, ui, inputBlocked: true, storyHold: true);
            Build(sim, new BoardUiState { SelectedId = sim.Board.Equipment[0].Id });
            Loc.SetLanguage(Loc.English, new Dictionary<string, string>());
            Build(sim, ui, english: true);
            Loc.SetLanguage(Loc.Turkish);
            var again = Build(sim, ui);
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(before, Fingerprint(sim));
            Assert.AreEqual(first.Header, again.Header);
            CollectionAssert.AreEqual(first.People.Select(p => p.Strip), again.People.Select(p => p.Strip));
        }

        // ---------------------------------------------------------------- cepler ve deste

        [Test]
        public void Pockets_AndDeck_EligibleFirstThenTheRest_InPeopleOrder()
        {
            var sim = Retired();
            var s = Build(sim);
            Assert.AreEqual(At(sim, 0).Id, s.PocketOccupant["tek"]);
            Assert.AreEqual(At(sim, 1).Id, s.PocketOccupant["emp"]);
            Assert.AreEqual(At(sim, 2).Id, s.PocketOccupant["ana"]);
            Assert.AreEqual(At(sim, 3).Id, s.PocketOccupant["craft"]);
            Assert.AreEqual(At(sim, 4).Id, s.PocketOccupant["cook"]);
            Assert.IsFalse(s.PocketOccupant.ContainsKey("train"));
            CollectionAssert.AreEqual(new[] { At(sim, 5).Id, At(sim, 6).Id, At(sim, 7).Id }, s.DeckOrder);
            Assert.AreEqual("rest", s.Person(At(sim, 5).Id).JobId, "Emekli görevler BOŞTA destesindedir.");

            // Kısıtlı kişi üst karttan iner (K4-A: üst kart Boşta'nın ilk uygun kişisi)
            Restrict(sim, At(sim, 5).Id);
            CollectionAssert.AreEqual(new[] { At(sim, 6).Id, At(sim, 7).Id, At(sim, 5).Id }, Build(sim).DeckOrder);
        }

        [Test]
        public void Deck_PutsTheBedriddenLast_DeadHaveNoPlace()
        {
            var sim = RaidWithLoss(out string lost, out string rescued);
            var s = Build(sim);
            // Emeklilik bu kayıtta çalışmadı: care ve gather destede, sırayla; yatan en altta
            CollectionAssert.AreEqual(new[] { At(sim, 5).Id, At(sim, 6).Id, At(sim, 7).Id, rescued }, s.DeckOrder);
            Assert.IsFalse(s.DeckOrder.Contains(lost));
            Assert.IsFalse(s.PocketOccupant.ContainsValue(lost));
            Assert.IsFalse(s.PocketOccupant.ContainsKey("tek"));
            Assert.IsFalse(s.PocketOccupant.ContainsKey("ana"));
            Assert.AreEqual(lost, s.People.Last().Id, "Ölüler listenin sonunda (havuz önce canlılara).");
            Assert.AreEqual(sim.Board.People.Count(p => p.Alive), s.People.Count(p => !p.Lost));
        }

        // ---------------------------------------------------------------- kişi kartı

        [Test]
        public void Strip_LostAndBedRest_FromARealRaid()
        {
            var sim = RaidWithLoss(out string lost, out string rescued);
            var s = Build(sim);
            var dead = s.Person(lost);
            Assert.IsTrue(dead.Lost);
            Assert.AreEqual("Kayıp · V.001", dead.Strip);
            Assert.AreEqual(StripTone.Ink, dead.StripTone);
            Assert.IsFalse(dead.Injured || dead.Bedridden || dead.Restricted || dead.Isolated);

            var bed = s.Person(rescued);
            Assert.AreEqual("Yatıyor · " + sim.Board.Person(rescued).Down + " v.", bed.Strip);
            Assert.AreEqual(StripTone.Blue, bed.StripTone);
            Assert.IsTrue(bed.Bedridden);
            Assert.IsTrue(bed.Injured);
            Assert.IsTrue(bed.Hurt);
            Assert.AreEqual("rest", bed.JobId);
        }

        [Test]
        public void Strip_RestrictedTrainingRestAndDuty_FirstMatchWins()
        {
            var sim = Retired();
            var restricted = At(sim, 5);
            var trainee = At(sim, 6);
            var hurt = At(sim, 7);
            Restrict(sim, restricted.Id);
            Assert.IsTrue(sim.TrainResident(trainee.Id, "craft").Ok);
            var s = Build(sim);
            Assert.AreEqual("Kısıtlı · 3 v.", s.Person(restricted.Id).Strip);
            Assert.AreEqual(StripTone.Red, s.Person(restricted.Id).StripTone);
            Assert.IsTrue(s.Person(restricted.Id).Restricted);
            Assert.AreEqual("Üretim +2", s.Person(trainee.Id).Strip);
            Assert.AreEqual("train", s.Person(trainee.Id).JobId);
            Assert.AreEqual("Boşta", s.Person(hurt.Id).Strip);
            Assert.AreEqual(StripTone.Mute, s.Person(hurt.Id).StripTone);
            Assert.AreEqual("Teknisyen", s.Person(At(sim, 0).Id).Strip);
            Assert.AreEqual("EMP uzmanı", s.Person(At(sim, 1).Id).Strip);
            Assert.AreEqual(StripTone.Ink, s.Person(At(sim, 0).Id).StripTone);

            hurt.Hp = hurt.MaxHp - 1;   // test düzeni: yaralı dinlenen
            var injured = Build(sim).Person(hurt.Id);
            Assert.AreEqual("Boşta · +1 can", injured.Strip);
            Assert.IsTrue(injured.Injured);
            Assert.AreEqual(hurt.Hp + "/" + hurt.MaxHp, injured.StatValues[0]);
            Assert.AreEqual(hurt.Name, injured.HoverTitle);
            Assert.AreEqual("Boşta · +1 can", injured.HoverLine);
        }

        [Test]
        public void PersonCard_MasteryLettersTiers_AndFieldPocketShowsRaidValues()
        {
            var sim = Retired();
            var tek = At(sim, 0);
            tek.Experience.Single(e => e.Subject == "tek").Points = sim.B.boardExperiencedAt;   // test düzeni: kademe 2
            var s = Build(sim, new BoardUiState { SelectedId = tek.Id });
            var card = s.Person(tek.Id);
            CollectionAssert.AreEqual(new[] { "T", "E", "A", "Ü", "M", "B", "İ" }, card.MasteryLetters);
            Assert.AreEqual(2, card.MasteryTiers[0]);
            Assert.IsTrue(card.MasteryTiers.Skip(1).All(t => t == 0));
            CollectionAssert.AreEqual(new[] { "CAN", "HAR", "GÜÇ" }, card.StatLabels);
            var slot = sim.PlanningSquad().Single(x => x.PersonId == tek.Id);
            Assert.AreEqual(System.Math.Min(sim.B.boardMaxFieldMove, tek.Move + sim.B.boardFieldSkillMoveBonus), slot.Move);
            Assert.AreEqual(DeskFormat.Int(slot.Move), card.StatValues[1], "Saha cebinde baskının gerçek HAR değeri.");
            Assert.AreEqual(DeskFormat.Int(slot.Power), card.StatValues[2]);
            Assert.AreEqual(tek.Name, card.Name);
            Assert.AreEqual(Loc.T(tek.Profession), card.Job);
            Assert.AreEqual("tek", card.JobId);
            Assert.IsTrue(card.Selected);
            Assert.IsFalse(s.People.Where(p => p.Id != tek.Id).Any(p => p.Selected));
            Assert.AreEqual(tek.Id, s.SelectedId);

            var cook = At(sim, 4);
            Assert.AreEqual(DeskFormat.Int(cook.Move), s.Person(cook.Id).StatValues[1], "Saha dışı cepte temel değer.");
        }

        [Test]
        public void PortraitKey_FoldsToAsciiLowerCase_AndDropsTheNumericSuffix()
        {
            Assert.AreEqual("ipek", PlanningBoardSnapshotBuilder.PortraitKeyOf("İpek"));
            Assert.AreEqual("suna", PlanningBoardSnapshotBuilder.PortraitKeyOf("Suna 12"));
            Assert.AreEqual("yalcin", PlanningBoardSnapshotBuilder.PortraitKeyOf("Yalcin 25"));
            Assert.AreEqual("sule", PlanningBoardSnapshotBuilder.PortraitKeyOf("Şule"));
            Assert.AreEqual("ozgur", PlanningBoardSnapshotBuilder.PortraitKeyOf("Özgür"));
            Assert.AreEqual("", PlanningBoardSnapshotBuilder.PortraitKeyOf(null));
            var sim = Retired();
            var s = Build(sim);
            Assert.IsTrue(s.People.All(p => p.PortraitKey == PlanningBoardSnapshotBuilder.PortraitKeyOf(p.Name)));
        }

        // ---------------------------------------------------------------- depo

        [Test]
        public void Materials_FreeHeldAndStockAboveTwenty()
        {
            var sim = Retired();
            sim.AddBoardMaterial("metal", 22);   // test düzeni: 25 metal
            Assert.IsTrue(sim.StageProductionMaterial("metal", 2).Ok);
            var s = Build(sim);
            var metal = s.Materials[0];
            Assert.AreEqual("metal", metal.Kind);
            Assert.AreEqual("METAL", metal.Name);
            Assert.AreEqual(25, metal.Stock);
            Assert.AreEqual(23, metal.Free);
            Assert.AreEqual(2, metal.Held);
            Assert.AreEqual("stok 25", metal.StockLine);
            Assert.AreEqual("ayrı 2", metal.HeldLine);
            Assert.AreEqual(sim.BoardMaterialFree("metal"), metal.Free);
            CollectionAssert.AreEqual(new[] { "metal", "devre", "kumas", "kimyasal" }, s.Materials.Select(m => m.Kind));
            CollectionAssert.AreEqual(new[] { "METAL", "DEVRE", "KUMAŞ", "KİMYASAL" }, s.Materials.Select(m => m.Name));
            Assert.AreEqual(0, s.Materials[1].Held);
            Assert.AreEqual(s.Materials[1].Stock, s.Materials[1].Free);
        }

        [Test]
        public void Chips_OnePerStagedUnit_CapEight_EveryStagedKindShows()
        {
            var sim = Retired();
            sim.AddBoardMaterial("metal", 10);   // test düzeni
            Assert.IsTrue(sim.StageProductionMaterial("metal", 6).Ok);
            Assert.IsTrue(sim.StageProductionMaterial("devre", 2).Ok);
            Assert.IsTrue(sim.StageProductionMaterial("kumas", 2).Ok);
            var s = Build(sim);
            CollectionAssert.AreEqual(new[]
            {
                "chip:metal:0", "chip:metal:1", "chip:metal:2", "chip:metal:3", "chip:devre:0", "chip:devre:1", "chip:kumas:0", "chip:kumas:1",
            }, s.Chips.Select(c => c.Id));
            Assert.AreEqual("METAL", s.Chips[0].Name);
            Assert.AreEqual(3, s.Chips[3].Index);
            Assert.AreEqual(6, s.Materials[0].Held, "Tam sayı stok kartında.");

            Assert.IsTrue(sim.StageProductionMaterial("metal", 1).Ok);
            CollectionAssert.AreEqual(new[] { "chip:metal:0", "chip:devre:0", "chip:devre:1", "chip:kumas:0", "chip:kumas:1" },
                Build(sim).Chips.Select(c => c.Id));
        }

        [Test]
        public void KitStacks_CountTextAndNewestOnTop()
        {
            var sim = Retired();
            var s = Build(sim);
            CollectionAssert.AreEqual(new[] { "kit_tek", "kit_emp", "kit_ana", "armor", "medkit", "emp_cell" }, s.Kits.Select(k => k.Kind));
            Assert.AreEqual(0, s.Kits[0].Count);
            Assert.AreEqual("×0", s.Kits[0].CountText);
            Assert.IsEmpty(s.Kits[0].TopIds);
            var medkit = sim.Board.Equipment.Single(e => e.Kind == "medkit");
            Assert.AreEqual(1, s.Kits[4].Count);
            CollectionAssert.AreEqual(new[] { medkit.Id }, s.Kits[4].TopIds);

            var worn = sim.Board.Equipment.First(e => e.Kind == "armor" && e.OwnerId != null);
            Assert.IsTrue(sim.EquipResident(worn.Id, null).Ok);
            var added = Enumerable.Range(0, 3).Select(_ => sim.AddBoardEquipment("armor").Id).ToArray();   // test düzeni
            var armor = Build(sim).Kits[3];
            Assert.AreEqual(4, armor.Count);
            Assert.AreEqual("×4", armor.CountText);
            CollectionAssert.AreEqual(added, armor.TopIds, "En çok üç kart, alttan üste; en yeni üstte.");
        }

        [Test]
        public void Items_BehindAPerson_AreCompactedKitArmorAid()
        {
            var sim = Retired();
            var owner = At(sim, 0);
            var medkit = sim.Board.Equipment.Single(e => e.Kind == "medkit");
            Assert.IsTrue(sim.EquipResident(medkit.Id, owner.Id).Ok);
            var s = Build(sim);
            CollectionAssert.AreEqual(new[] { ItemSlotKind.Kit, ItemSlotKind.Armor, ItemSlotKind.Aid },
                s.Items.Where(i => i.OwnerId == owner.Id).Select(i => i.Slot));
            var card = s.Item(medkit.Id);
            Assert.AreEqual("Bakım kiti", card.Name);
            Assert.AreEqual("YARDIMCI", card.SlotLabel);
            Assert.AreEqual("bakım görevinde +1 can; panoda bakım cebi yok", card.UseLine);
            Assert.AreEqual(owner.Name + " · " + card.UseLine, card.HoverLine);
            Assert.AreEqual("saha rolü: Teknisyen", s.Items.Single(i => i.Kind == "kit_tek").UseLine);

            var armor = sim.Board.Equipment.Single(e => e.Kind == "armor" && e.OwnerId == owner.Id);
            Assert.IsTrue(sim.EquipResident(armor.Id, null).Ok);
            var after = Build(sim);
            CollectionAssert.AreEqual(new[] { ItemSlotKind.Kit, ItemSlotKind.Aid }, after.Items.Where(i => i.OwnerId == owner.Id).Select(i => i.Slot));
            Assert.IsNull(after.Item(armor.Id).OwnerId);
            Assert.AreEqual("DEPO · ilk hasarı karşılar, kırılır", after.Item(armor.Id).HoverLine);
            Assert.AreEqual(sim.Board.Equipment.Count, after.Items.Count);
        }

        // ---------------------------------------------------------------- KAYIPLAR

        [Test]
        public void DeathLog_GivesTheRaidDay_NullWithoutAnEntry()
        {
            var sim = RaidWithLoss(out string lost, out string rescued);
            Assert.AreEqual(sim.S.Day, BoardDeathLog.DeathDay(sim, lost));
            Assert.IsNull(BoardDeathLog.DeathDay(sim, rescued), "Kurtarılan kişi ölmedi.");
            Assert.IsNull(BoardDeathLog.DeathDay(sim, "yok-1"));
            Assert.IsNull(BoardDeathLog.DeathDay(null, lost));

            var s = Build(sim);
            Assert.AreEqual(1, s.Lost.Count);
            Assert.AreEqual(lost, s.Lost[0].Id);
            Assert.AreEqual(sim.Board.Person(lost).Name, s.Lost[0].Name);
            Assert.AreEqual("Kayıp · V.001", s.Lost[0].Line);
            Assert.AreEqual("KAYIPLAR · 1", s.LostCover);
            Assert.AreEqual("", Build(Retired()).LostCover);
        }

        // ---------------------------------------------------------------- konular ve dosya

        [Test]
        public void Topics_RingFollowsTheTrainee_ElseThePresentationTopic()
        {
            var sim = Retired();
            var s = Build(sim, new BoardUiState { TrainingTopic = "cook" });
            CollectionAssert.AreEqual(PlanningBoardData.Subjects, s.Topics.Subjects);
            CollectionAssert.AreEqual(new[] { "TEKNİSYEN", "EMP", "ANALİZ", "ÜRETİM", "MUTFAK", "BAKIM", "İKMAL" }, s.Topics.Labels);
            Assert.AreEqual(4, s.Topics.RingRow);
            Assert.IsNull(s.Topics.TraineeId);
            Assert.IsTrue(s.Topics.Mastered.All(m => !m));

            var trainee = At(sim, 7);
            Assert.IsTrue(sim.TrainResident(trainee.Id, "craft").Ok);
            var emp = trainee.Experience.FirstOrDefault(e => e.Subject == "emp");
            if (emp == null) trainee.Experience.Add(new ResidentSkill { Subject = "emp", Points = sim.B.boardExpertAt });   // test düzeni
            else emp.Points = sim.B.boardExpertAt;
            var t = Build(sim, new BoardUiState { TrainingTopic = "cook" }).Topics;
            Assert.AreEqual(3, t.RingRow);
            Assert.AreEqual(trainee.Id, t.TraineeId);
            Assert.IsTrue(t.Mastered[1]);
            Assert.AreEqual("usta", PlanningBoardSnapshotBuilder.TopicHoverLine(t, 1));
            Assert.IsNull(PlanningBoardSnapshotBuilder.TopicHoverLine(t, 3));
        }

        [Test]
        public void Folder_SelectedPerson_Pages_LegacyTabsClosed()
        {
            var sim = Retired();
            var p = At(sim, 7);
            var s = Build(sim, new BoardUiState { SelectedId = p.Id });
            Assert.AreEqual(DeskText.Upper(p.Name, DeskLang.Tr), s.FolderCoverLine);
            var f = s.Folder;
            Assert.IsNotNull(f);
            Assert.AreEqual(p.Id, f.PersonId);
            Assert.AreEqual(p.Name, f.Name);
            Assert.IsTrue(f.Left.Any(l => l.Label == "MESLEK" && l.Value == Loc.T(p.Profession)));
            Assert.AreEqual(7 + 1, f.Left.SkipWhile(l => l.Label != "USTALIK").TakeWhile(l => l.Label != "TEÇHİZAT").Count());
            Assert.AreEqual("sakin-8", p.Id, "kurucu fikstürü");
            Assert.AreEqual("KALICI KİŞİ DOSYASI", f.RightHeader, "sağ sayfanın üst satırı (ikinci tur kararı)");
            Assert.IsFalse(f.Right.Any(l => l.Label == "KALICI KİŞİ DOSYASI"), "başlık satırlar arasında tekrarlanmaz");
            Assert.IsFalse(f.DossierRight.Any(l => l.Label == "KALICI KİŞİ DOSYASI"));
            Assert.IsTrue(f.Right.Any(l => l.Section && l.Label == "KAYIT"));
            Assert.AreEqual("evet", f.Right.Single(l => l.Label == "KURUCU").Value);
            Assert.IsFalse(f.Right.Any(l => l.Label == "BELGE NO" || l.Label == "DIŞ KAYIT" || l.Label == "İNCELEME"), "eski belge ve inceleme satırları yok");
            Assert.IsFalse(f.Right.Any(l => l.Value != null && l.Value.StartsWith("KARAR")), "kurucuda karar satırı yok");
            // Eski sekmeler (İNCELE, ERİŞİM, TAHLİYE) ve künye aşama 7'de söküldü: kısıt ve yollama yalnız olay kararıyla
            Assert.IsNull(typeof(FolderData).GetField("LegacyActions"));
            Assert.IsNull(typeof(FolderData).GetField("AdmissionHeader"));

            var none = Build(sim, new BoardUiState { SelectedId = "yok-1" });
            Assert.IsNull(none.Folder);
            Assert.IsNull(none.SelectedId);
            Assert.AreEqual("— kart seç —", none.FolderCoverLine);
        }

        [Test]
        public void Words_AreInTheCurrentLanguage()
        {
            var s = Build(Retired());
            Assert.AreEqual("YARALI", s.Words.Injured);
            Assert.AreEqual("KİT", s.Words.Kit);
            Loc.SetLanguage(Loc.English, new Dictionary<string, string> { { "YARALI", "INJURED" }, { "KİT", "KIT" } });
            var en = Build(Retired(), english: true);
            Assert.AreEqual("INJURED", en.Words.Injured);
            Assert.AreEqual("KIT", en.Words.Kit);
        }

        // ================================================================ Aşama 5 · R18 (PLAN W4, [B])

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        static void Ok(ActResult r) => Assert.IsTrue(r.Ok, r.Msg);

        /// <summary>Zırh plakası emri, girdileri tam ayrılmış (V.001: metal 3 → 2 ayrı, kumaş 2 → 1 ayrı).</summary>
        static Simulation ArmorOrder(Simulation sim = null)
        {
            sim = sim ?? Retired();
            Ok(sim.QueueProduction("armor"));
            Ok(sim.ReserveProductionMaterial("metal", sim.B.boardArmorMetal));
            Ok(sim.ReserveProductionMaterial("kumas", sim.B.boardArmorCloth));
            return sim;
        }

        /// <summary>EMP hücresi emri (iki vardiya), girdileri tam ayrılmış.</summary>
        static Simulation EmpOrder(Simulation sim = null)
        {
            sim = sim ?? Retired();
            Ok(sim.QueueProduction("emp_cell"));
            Ok(sim.ReserveProductionMaterial("metal", sim.B.boardEmpMetal));
            Ok(sim.ReserveProductionMaterial("devre", sim.B.boardEmpCircuits));
            return sim;
        }

        static void FillStore(Simulation sim)
        {
            while (sim.Board.Equipment.Count < sim.B.boardEquipmentCapacity) Assert.IsNotNull(sim.AddBoardEquipment("medkit"));   // test düzeni
        }

        /// <summary>
        /// KOS aşama 2 (A1 bulgusu D1): kişiye bağlı teçhizat depo tavanına sayılmaz. Tahmin Core'un <c>BoardDepotCount</c>'unu okur: bağlı
        /// eşyayla toplam sayı tavana varsa da üretim durmaz; bağlı olmayan eşya tavanı doldurunca "eşya deposu dolu".
        /// </summary>
        [Test]
        public void Forecast_BoundGear_DoesNotFillTheStore()
        {
            var sim = Retired();
            Ok(sim.QueueProduction("armor"));
            Ok(sim.ReserveProductionMaterial("metal", 2));
            Ok(sim.ReserveProductionMaterial("kumas", 1));
            int capacity = sim.B.boardEquipmentCapacity;
            while (sim.BoardDepotCount < capacity - 1) Assert.IsNotNull(sim.AddBoardEquipment("medkit"));   // test düzeni
            string owner = At(sim, 6).Id;
            Assert.IsNotNull(sim.AddBoardEquipment("armor", owner, true), "bağlı eşya tavana bakmadan eklenir");
            Assert.IsNotNull(sim.AddBoardEquipment("kit_tek", owner, true));
            Assert.GreaterOrEqual(sim.Board.Equipment.Count, capacity, "kurulum: toplam eşya tavanda ya da üstünde");
            Assert.AreEqual(capacity - 1, sim.BoardDepotCount, "depoda bir boş yer");
            var open = ProductionForecast.Of(sim);
            Assert.AreEqual(ProductionStop.None, open.Stop, "bağlı eşya depoyu doldurmaz");
            Assert.IsTrue(open.Works);
            Assert.IsNull(open.StopReason);
            Assert.IsNull(sim.BoardPlanCheck(), "Core da fişi engellemez");

            Assert.IsNotNull(sim.AddBoardEquipment("medkit"));
            Assert.AreEqual(capacity, sim.BoardDepotCount);
            var full = ProductionForecast.Of(sim);
            Assert.AreEqual(ProductionStop.StoreFull, full.Stop);
            Assert.AreEqual("eşya deposu dolu", full.StopReason);
            Assert.IsFalse(full.Works);

            // Tahmin = Core: dolu depoda Core da fişi bastırmaz; yer açılınca emir kapanışta biter
            var order = sim.Board.Orders.First();
            Assert.IsNotNull(sim.BoardPlanCheck(), "Core: depo doluyken plan basılmaz");
            sim.Board.Equipment.Remove(sim.Board.Equipment.Last(e => !e.Bound && e.OwnerId == null));   // test düzeni
            Assert.AreEqual(ProductionStop.None, ProductionForecast.Of(sim).Stop);
            Assert.IsNull(sim.BoardPlanCheck());
            Close(sim);
            Assert.IsNull(sim.Board.Order(order.Id), "yer açılınca emir bitti (bağlı eşya hâlâ kişide)");
            Assert.AreEqual(2, sim.Board.Equipment.Count(e => e.Bound && e.OwnerId == owner));
        }

        /// <summary>
        /// Kit kilidi (KOS aşama 1): aç kapanış eksik bir rol kitinin üretimini durdurmaz; tahmin de öyle der ve kapanışla birebir tutar.
        /// Kilit kapalıyken ya da kit eksik değilken açlık durdurur. Durma nedeninin sırası Core'unki: depo, sonra açlık.
        /// </summary>
        [Test]
        public void Forecast_Starving_MissingKitOrderStillWorks()
        {
            Simulation KitOrder(bool missing)
            {
                var s = Retired();
                if (missing) s.Board.Equipment.RemoveAll(e => e.Kind == "kit_emp");   // test düzeni
                Assert.AreEqual(missing, s.BoardKitMissing("kit_emp"), "kurulum");
                Ok(s.QueueProduction("kit_emp"));
                foreach (var input in s.BoardRecipes.First(r => r.Id == "kit_emp").Inputs)
                {
                    s.AddBoardMaterial(input.Kind, input.Quantity);   // test düzeni
                    Ok(s.ReserveProductionMaterial(input.Kind, input.Quantity));
                }
                s.Shelter.Supplies = 0;   // test düzeni
                return s;
            }

            var sim = KitOrder(true);
            Assert.IsTrue(sim.B.boardStarvingKitProduction, "kurulum: kit kilidi açık");
            Assert.IsTrue(ProductionForecast.Starving(sim));
            int remaining = sim.Board.Orders.First().Remaining;
            string orderId = sim.Board.Orders.First().Id;
            var o = ProductionForecast.Of(sim);
            Assert.AreEqual(ProductionStop.None, o.Stop, "aç ama eksik kit: durmaz");
            Assert.IsNull(o.StopReason);
            Assert.IsTrue(o.Works);
            Assert.Greater(o.After, o.Done);

            // Sıra: dolu depo açlıktan önce
            var stored = new List<BoardEquipment>();
            while (sim.BoardDepotCount < sim.B.boardEquipmentCapacity) stored.Add(sim.AddBoardEquipment("medkit"));   // test düzeni
            Assert.AreEqual(ProductionStop.StoreFull, ProductionForecast.Of(sim).Stop, "dolu depo eksik kiti de durdurur");
            foreach (var e in stored) sim.Board.Equipment.Remove(e);

            CloseAndCompareForecast(sim, "aç + eksik kit");
            Assert.IsTrue(sim.Shelter.Starving, "Core da açlık saydı");
            var left = sim.Board.Order(orderId);
            Assert.IsTrue(left == null || left.Remaining < remaining, "Core kiti ilerletti");

            // Kit eksik değil: açlık durdurur
            var owned = KitOrder(false);
            Assert.AreEqual(ProductionStop.Starvation, ProductionForecast.Of(owned).Stop);
            CloseAndCompareForecast(owned, "aç + eldeki kit");

            // Kilit kapalı: eksik kit de durur
            var off = KitOrder(true);
            off.B.boardStarvingKitProduction = false;
            Assert.AreEqual(ProductionStop.Starvation, ProductionForecast.Of(off).Stop);
            CloseAndCompareForecast(off, "aç + kilit kapalı");
        }

        // ---------------------------------------------------------------- E7 · defter sayfaları

        [Test]
        public void Recipes_FollowCore_TrEn()
        {
            var sim = Retired();
            var core = sim.BoardRecipes;
            var s = Build(sim);
            Assert.AreEqual(6, s.Recipes.Count);
            CollectionAssert.AreEqual(new[] { "armor", "medkit", "emp_cell", "kit_tek", "kit_emp", "kit_ana" }, s.Recipes.Select(r => r.Id));
            CollectionAssert.AreEqual(core.Select(r => r.Id), s.Recipes.Select(r => r.Id), "Sayfa i = BoardRecipes[i].");
            CollectionAssert.AreEqual(Enumerable.Range(0, 6), s.Recipes.Select(r => r.Index));
            CollectionAssert.AreEqual(new[] { "ZIRH PLAKASI", "BAKIM KİTİ", "EMP HÜCRESİ", "TEKNİSYEN KİTİ", "EMP KİTİ", "ANALİST KİTİ" },
                s.Recipes.Select(r => r.Title), "Başlık TR büyük harfle (İ).");
            CollectionAssert.AreEqual(new[] { "TARİF 01", "TARİF 02", "TARİF 03", "TARİF 04", "TARİF 05", "TARİF 06" }, s.Recipes.Select(r => r.Number));
            CollectionAssert.AreEqual(new[] { "zırh yuvası", "yardımcı yuvası", "yardımcı yuvası", "kit yuvası", "kit yuvası", "kit yuvası" },
                s.Recipes.Select(r => r.SlotWord));
            Assert.IsTrue(s.Recipes.All(r => r.OutLine == "1 ×"));
            CollectionAssert.AreEqual(new[] { "1 vardiya", "1 vardiya", "2 vardiya", "1 vardiya", "1 vardiya", "1 vardiya" }, s.Recipes.Select(r => r.TurnsLine));
            CollectionAssert.AreEqual(core.Select(r => r.Turns), s.Recipes.Select(r => r.Turns));
            CollectionAssert.AreEqual(new[] { "s. 1", "s. 2", "s. 3", "s. 4", "s. 5", "s. 6" }, s.Recipes.Select(r => r.PageLabel));
            CollectionAssert.AreEqual(new[] { "AÇILIM 1/3", "AÇILIM 1/3", "AÇILIM 2/3", "AÇILIM 2/3", "AÇILIM 3/3", "AÇILIM 3/3" }, s.Recipes.Select(r => r.SpreadLabel));
            // El notu: yalnız EMP hücresinde (iki vardiya, deneyimli üretici tek kapanışta bitirir); [B] 04'ün notu çizilmez (D8)
            CollectionAssert.AreEqual(new[] { "", "", "→ deneyimli üretici: tek kapanış", "", "", "" }, s.Recipes.Select(r => r.Note));
            var armor = s.Recipes[0];
            Assert.AreEqual("Zırh plakası", armor.Name);
            CollectionAssert.AreEqual(new[] { "metal", "kumas" }, armor.Inputs.Select(i => i.Kind));
            CollectionAssert.AreEqual(new[] { "Metal", "Kumaş" }, armor.Inputs.Select(i => i.Name));
            CollectionAssert.AreEqual(new[] { 2, 1 }, armor.Inputs.Select(i => i.Quantity));
            CollectionAssert.AreEqual(new[] { "× 2", "× 1" }, armor.Inputs.Select(i => i.BookQty));
            CollectionAssert.AreEqual(new[] { "×2", "×1" }, armor.Inputs.Select(i => i.CardQty));
            for (int i = 0; i < core.Count; i++)
                CollectionAssert.AreEqual(core[i].Inputs.Select(m => m.Kind + m.Quantity), s.Recipes[i].Inputs.Select(m => m.Kind + m.Quantity), core[i].Id);

            // Sabit sözcükler ve kapalı defterin üzerine gelme satırları
            Assert.AreEqual("KYGA · TARİF DEFTERİ", s.Book.Header);
            Assert.AreEqual("ÇIKTI", s.Book.Output);
            Assert.AreEqual("GİRDİ", s.Book.Input);
            Assert.AreEqual("SÜRE", s.Book.Time);
            Assert.AreEqual("sonraki ►", s.Book.Next);
            Assert.AreEqual("◄ önceki", s.Book.Prev);
            Assert.AreEqual("İŞ EMRİ", s.Book.Tab);
            Assert.AreEqual("TARİF DEFTERİ", s.BookTitle);
            Assert.AreEqual("6 tarif", s.BookLine);

            // Denge değişince sayfa değişir (PROMPT §7: kartlar veriden çizilir)
            var heavy = Simulation.NewGame(79, new Balance
            { shelterEventsEnabled = false,
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100,
                boardArmorMetal = 3, boardEmpTurns = 3, boardCraftExtraProgress = 1,
            });
            var h = Build(heavy);
            Assert.AreEqual("× 3", h.Recipes[0].Inputs[0].BookQty);
            Assert.AreEqual("3 vardiya", h.Recipes[2].TurnsLine);
            Assert.AreEqual("", h.Recipes[2].Note, "Üç vardiyalık işi deneyimli üretici de tek kapanışta bitiremez: not çizilmez.");

            // EN
            Loc.SetLanguage(Loc.English, new Dictionary<string, string>
            {
                { "Zırh plakası", "Armour plate" }, { "EMP hücresi", "EMP cell" }, { "Teknisyen kiti", "Technician kit" }, { "Kumaş", "Cloth" },
                { "TARİF {0:00}", "RECIPE {0:00}" }, { "KYGA · TARİF DEFTERİ", "KYGA · RECIPE BOOK" }, { "ÇIKTI", "OUTPUT" }, { "GİRDİ", "INPUT" },
                { "SÜRE", "DURATION" }, { "1 vardiya", "1 shift" }, { "{0} vardiya", "{0} shifts" }, { "zırh yuvası", "armour slot" },
                { "kit yuvası", "kit slot" }, { "s. {0}", "p. {0}" }, { "AÇILIM {0}/{1}", "SPREAD {0}/{1}" }, { "sonraki ►", "next ►" },
                { "◄ önceki", "◄ previous" }, { "İŞ EMRİ", "WORK ORDER" }, { "TARİF DEFTERİ", "RECIPE BOOK" }, { "{0} tarif", "{0} recipes" },
                { "→ deneyimli üretici: tek kapanış", "→ experienced maker: one closing" },
            });
            var en = Build(sim, english: true);
            Assert.AreEqual("ARMOUR PLATE", en.Recipes[0].Title);
            Assert.AreEqual("Armour plate", en.Recipes[0].Name);
            Assert.AreEqual("TECHNICIAN KIT", en.Recipes[3].Title);
            Assert.AreEqual("RECIPE 01", en.Recipes[0].Number);
            Assert.AreEqual("armour slot", en.Recipes[0].SlotWord);
            Assert.AreEqual("1 shift", en.Recipes[0].TurnsLine);
            Assert.AreEqual("2 shifts", en.Recipes[2].TurnsLine);
            Assert.AreEqual("→ experienced maker: one closing", en.Recipes[2].Note);
            Assert.AreEqual("Cloth", en.Recipes[0].Inputs[1].Name);
            Assert.AreEqual("p. 3", en.Recipes[2].PageLabel);
            Assert.AreEqual("SPREAD 2/3", en.Recipes[2].SpreadLabel);
            Assert.AreEqual("KYGA · RECIPE BOOK", en.Book.Header);
            Assert.AreEqual("next ►", en.Book.Next);
            Assert.AreEqual("RECIPE BOOK", en.BookTitle);
            Assert.AreEqual("6 recipes", en.BookLine);
        }

        // ---------------------------------------------------------------- E14 · iş emri formu

        [Test]
        public void Order_B_States()
        {
            var sim = Retired();
            string maker = At(sim, 3).Name;
            Assert.AreEqual("craft", At(sim, 3).Job);

            // boş
            var empty = Build(sim).Order;
            Assert.AreEqual(OrderState.Empty, empty.State);
            Assert.AreEqual("No 001", empty.Number);
            Assert.IsNull(empty.Card);
            Assert.IsNull(empty.OrderId);
            Assert.AreEqual("", empty.Status);
            Assert.AreEqual("", empty.Stamp);
            Assert.AreEqual("", empty.DropHint, "Boş form hammadde almaz: ipucu yok.");
            Assert.IsFalse(empty.CardDraggable);
            Assert.IsTrue(Build(sim).RecipesDraggable, "Plan açık: tarif defterden sürüklenebilir.");

            // tarif (ayrılan 0)
            Ok(sim.QueueProduction("armor"));
            string orderId = sim.Board.Orders.Single().Id;
            var recipe = Build(sim).Order;
            Assert.AreEqual(OrderState.Recipe, recipe.State);
            Assert.AreEqual(orderId, recipe.OrderId);
            Assert.AreEqual("armor", recipe.RecipeId);
            Assert.AreEqual("eksik: 2 metal + 1 kumaş", recipe.Status);
            Assert.AreEqual("", recipe.Stamp);
            const string dropHint = "GİRDİLER (hammaddeyi buraya bırak)";
            Assert.AreEqual(dropHint, recipe.DropHint, "Tarifi konmuş taslak hammadde bekler.");
            Assert.AreEqual("", Build(sim, storyHold: true).Order.DropHint, "Düzenlenemeyen planda ipucu yok.");
            Assert.IsNotNull(recipe.Card);
            Assert.AreEqual(orderId, recipe.Card.Id, "Formdaki kartın kimliği iş emri.");
            Assert.AreEqual("armor", recipe.Card.RecipeId);
            Assert.AreEqual("Zırh plakası", recipe.Card.Name);
            Assert.AreEqual("armor", recipe.Card.IconKey);
            Assert.AreEqual("1 vardiya", recipe.Card.TurnsLine);
            Assert.AreEqual("→ 1 ürün", recipe.Card.OutLine);
            Assert.AreEqual("GİRDİ", recipe.Card.NeedsLabel);
            CollectionAssert.AreEqual(new[] { "×2", "×1" }, recipe.Card.Inputs.Select(i => i.CardQty));
            Assert.AreEqual(-1, recipe.Card.Done, "Tek vardiyalık tarifte ilerleme kutusu yok (D7).");
            Assert.IsTrue(recipe.CardDraggable);
            Assert.IsFalse(recipe.Started);
            Assert.AreEqual(1, recipe.Turns);
            Assert.AreEqual(0, recipe.Done);

            // eksik: yalnız metal ayrıldı; Core'un plan denetimi aynı eksiği söyler (şalter kilidi, Aşama 4 D1-A)
            Ok(sim.ReserveProductionMaterial("metal", 2));
            var missing = Build(sim).Order;
            Assert.AreEqual(OrderState.Missing, missing.State);
            Assert.AreEqual("eksik: 1 kumaş", missing.Status);
            Assert.AreEqual(dropHint, missing.DropHint, "Eksik taslak hammadde bekler.");
            Assert.AreEqual("Zırh plakası: Kumaş adedi 1 olmalı.", sim.BoardPlanCheck());
            // Eksik emirde DURDU yok (girdiler hazır değil): üretici yokken de
            Ok(sim.AssignResident(At(sim, 3).Id, "rest"));
            Assert.AreEqual(OrderState.Missing, Build(sim).Order.State);
            Assert.AreEqual("", Build(sim).Order.Stamp);
            Ok(sim.AssignResident(At(sim, 3).Id, "craft"));

            // tam
            Ok(sim.ReserveProductionMaterial("kumas", 1));
            var full = Build(sim).Order;
            Assert.AreEqual(OrderState.Full, full.State);
            Assert.AreEqual("zırh plakası · girdiler ayrıldı · üretici: " + maker, full.Status);
            Assert.AreEqual("", full.DropHint, "Tam emirde ayıracak girdi kalmadı.");
            Assert.AreEqual("", full.Stamp);
            Assert.AreEqual("", full.StopReason);
            Assert.IsNull(sim.BoardPlanCheck());

            // tek emir kuralı (SPB:262)
            var second = sim.QueueProduction("medkit");
            Assert.IsFalse(second.Ok);
            Assert.AreEqual("Önce mevcut üretim emrini tamamla veya iptal et.", second.Msg);
            Assert.AreEqual(OrderState.Full, Build(sim).Order.State);

            // durdu: üç neden (Core'un AdvanceBoardProduction koşulları), damga + neden sözcüğü
            Ok(sim.AssignResident(At(sim, 3).Id, "rest"));
            var noMaker = Build(sim).Order;
            Assert.AreEqual(OrderState.Stopped, noMaker.State);
            Assert.AreEqual("DURDU", noMaker.Stamp);
            Assert.AreEqual("üretici yok", noMaker.StopReason);
            Assert.AreEqual("zırh plakası · üretici yok", noMaker.Status);
            Assert.IsNotNull(noMaker.Card, "Durmuş emrin kartı TARİF kutusunda kalır.");
            Ok(sim.AssignResident(At(sim, 3).Id, "craft"));

            double supplies = sim.Shelter.Supplies;
            sim.Shelter.Supplies = 0;   // test düzeni: kapanış erzağı 0'a iner
            var hungry = Build(sim).Order;
            Assert.AreEqual(OrderState.Stopped, hungry.State);
            Assert.AreEqual("açlık", hungry.StopReason);
            Assert.AreEqual("DURDU", hungry.Stamp);
            sim.Shelter.Supplies = supplies;
            Assert.AreEqual(OrderState.Full, Build(sim).Order.State);

            // EN: damga ve satırlar
            Loc.SetLanguage(Loc.English, new Dictionary<string, string>
            {
                { "Zırh plakası", "Armour plate" }, { "Kumaş", "Cloth" }, { "DURDU", "STOPPED" }, { "üretici yok", "no maker" },
                { "{0} · girdiler ayrıldı · üretici: {1}", "{0} · inputs held · maker: {1}" }, { "No {0:000}", "No. {0:000}" },
                { "eksik: {0}", "missing: {0}" },
            });
            var enFull = Build(sim, english: true).Order;
            Assert.AreEqual("armour plate · inputs held · maker: " + maker, enFull.Status);
            Assert.AreEqual("No. 001", enFull.Number);
            Ok(sim.AssignResident(At(sim, 3).Id, "rest"));
            var enStop = Build(sim, english: true).Order;
            Assert.AreEqual("STOPPED", enStop.Stamp);
            Assert.AreEqual("no maker", enStop.StopReason);
            Assert.AreEqual("armour plate · no maker", enStop.Status);
            Ok(sim.CancelProduction(orderId));
            Ok(sim.QueueProduction("armor"));
            Assert.AreEqual("missing: 2 metal + 1 cloth", Build(sim, english: true).Order.Status);
            Loc.SetLanguage(Loc.Turkish);

            // tamamlandı: kapanışta emir silinir, form boşalır
            Ok(sim.AssignResident(At(sim, 3).Id, "craft"));
            Ok(sim.ReserveProductionMaterial("metal", 2));
            Ok(sim.ReserveProductionMaterial("kumas", 1));
            Close(sim);
            var done = Build(sim).Order;
            Assert.AreEqual(OrderState.Empty, done.State);
            Assert.AreEqual("No 002", done.Number);
            Assert.IsNull(done.Card);
            CollectionAssert.AreEqual(new[] { "armor" }, sim.Board.LastCompleted);

            // kilitli plan: kart ve tarifler tutulmaz
            var printed = ArmorOrder();
            Ok(printed.PrintShift());
            var locked = Build(printed);
            Assert.IsFalse(locked.RecipesDraggable);
            Assert.IsFalse(locked.Order.CardDraggable);
            Assert.AreEqual(OrderState.Full, locked.Order.State);
        }

        [Test]
        public void Order_B_StartedTwoShiftOrder_ProgressBoxesAndPaidInputs()
        {
            var sim = EmpOrder();
            string maker = At(sim, 3).Name;
            var before = Build(sim).Order;
            Assert.AreEqual(OrderState.Full, before.State);
            Assert.AreEqual(2, before.Turns);
            Assert.AreEqual(0, before.Done);
            Assert.AreEqual(0, before.Card.Done, "İki vardiyalık tarifte iki boş kutu (D7).");
            Assert.AreEqual(2, before.Card.Turns);
            Assert.AreEqual("2 vardiya", before.Card.TurnsLine);
            Assert.AreEqual("EMP hücresi · girdiler ayrıldı · üretici: " + maker, before.Status, "Kısaltmayla başlayan ad küçültülmez.");
            Assert.AreEqual(1, Build(sim).Materials[0].Held);

            Close(sim);
            var order = sim.Board.Orders.Single();
            Assert.IsTrue(order.Started);
            Assert.AreEqual(1, order.Remaining, "Craft kademe 1 (2 → 3 puan): EMP hücresi iki kapanış sürer.");
            var s = Build(sim);
            var started = s.Order;
            Assert.AreEqual(OrderState.Started, started.State);
            Assert.AreEqual("", started.DropHint, "Başlamış emir taslak değil.");
            Assert.IsTrue(started.Started);
            Assert.AreEqual(1, started.Done, "Done = Turns − Remaining.");
            Assert.AreEqual(1, started.Card.Done, "İlk kutu dolu.");
            Assert.AreEqual("EMP hücresi · girdiler ödendi · 1/2 · üretici: " + maker, started.Status);
            CollectionAssert.IsEmpty(s.Chips, "Başlamış emirde girdi ödendi: fiş çizilmez (D7).");
            Assert.AreEqual(0, s.Materials[0].Held, "Ödenen girdi artık ayrı sayılmaz.");
            Assert.AreEqual(0, s.Materials[1].Held);

            // başlamış ama durmuş: damga
            Ok(sim.AssignResident(At(sim, 3).Id, "rest"));
            var stopped = Build(sim).Order;
            Assert.AreEqual(OrderState.Stopped, stopped.State);
            Assert.AreEqual("DURDU", stopped.Stamp);
            Assert.AreEqual(1, stopped.Card.Done);
            Ok(sim.AssignResident(At(sim, 3).Id, "craft"));

            Close(sim);
            Assert.AreEqual(OrderState.Empty, Build(sim).Order.State);
            CollectionAssert.AreEqual(new[] { "emp_cell" }, sim.Board.LastCompleted);
        }

        [Test]
        public void Order_B_DepotFull_LocksAndStops()
        {
            var sim = ArmorOrder();
            FillStore(sim);
            Assert.AreEqual(sim.B.boardEquipmentCapacity, sim.Board.Equipment.Count);
            var order = Build(sim).Order;
            Assert.AreEqual(OrderState.Stopped, order.State);
            Assert.AreEqual("DURDU", order.Stamp);
            Assert.AreEqual("eşya deposu dolu", order.StopReason);
            Assert.AreEqual("zırh plakası · eşya deposu dolu", order.Status);
            Assert.AreEqual("Teçhizat deposu dolu. Üretimden önce yer aç.", sim.BoardPlanCheck(), "Emir varken depo dolu: şalter kilitli (SPB:322–323).");
            Assert.IsFalse(sim.PrintShift().Ok);

            // hurda yer açar: emir yeniden tam
            Ok(sim.ScrapEquipment(sim.Board.Equipment.Last(e => string.IsNullOrEmpty(e.OwnerId)).Id));
            Assert.AreEqual(OrderState.Full, Build(sim).Order.State);
            Assert.IsNull(sim.BoardPlanCheck());

            // emir yokken depo dolu: kilit yok, form boş
            var idle = Retired();
            FillStore(idle);
            Assert.AreEqual(OrderState.Empty, Build(idle).Order.State);
            Assert.IsNull(idle.BoardPlanCheck());
        }

        // ---------------------------------------------------------------- E13 · tahmin = kapanış

        /// <summary>Formu ve tahmini kurar, vardiyayı kapatır, gerçek kapanışla karşılaştırır.</summary>
        static ProductionOutlook CloseAndCompareForecast(Simulation sim, string label)
        {
            var o = ProductionForecast.Of(sim);
            var form = Build(sim).Order;
            var order = sim.Board.Orders.FirstOrDefault();
            string orderId = order?.Id;
            int remaining = order?.Remaining ?? 0;
            bool wasStarted = order?.Started ?? false;
            var stock = BoardLayout.MaterialKinds.ToDictionary(k => k, k => sim.Board.Material(k)?.Quantity ?? 0);
            var recipe = o.IsNone ? null : sim.BoardRecipes.First(r => r.Id == o.RecipeId);
            bool readyForm = form.State == OrderState.Full || form.State == OrderState.Started || form.State == OrderState.Stopped;
            Assert.AreEqual(order != null && readyForm && o.Stop != ProductionStop.None, form.State == OrderState.Stopped, label + ": DURDU = hazır emir + durma nedeni");
            Close(sim);
            var completed = sim.Board.LastCompleted;
            if (o.Completes)
            {
                CollectionAssert.AreEqual(new[] { o.Output }, completed, label + ": ürün çıktı");
                if (orderId != null) Assert.IsNull(sim.Board.Order(orderId), label + ": biten emir kalkar");
            }
            else if (o.Works)
            {
                CollectionAssert.IsEmpty(completed, label + ": sürüyor");
                var now = sim.Board.Order(orderId);
                Assert.IsTrue(now.Started, label);
                Assert.AreEqual(o.After, o.Turns - now.Remaining, label + ": ilerleme");
            }
            else
            {
                CollectionAssert.IsEmpty(completed, label + ": çıktı yok");
                if (orderId != null)
                {
                    Assert.AreEqual(remaining, sim.Board.Order(orderId).Remaining, label + ": emir ilerlemedi");
                    Assert.AreEqual(wasStarted, sim.Board.Order(orderId).Started, label + ": ödeme yok");
                }
            }
            // Girdiler yalnız çalışan ve başlamamış işte (ya da otomatik yolda) bir kez düşer
            bool pays = o.Works && !wasStarted;
            foreach (string kind in BoardLayout.MaterialKinds)
            {
                int spent = pays ? recipe.Inputs.Where(i => i.Kind == kind).Sum(i => i.Quantity) : 0;
                Assert.AreEqual(stock[kind] - spent, sim.Board.Material(kind)?.Quantity ?? 0, label + ": " + kind + " stoğu");
            }
            return o;
        }

        [Test]
        public void Order_PredictionMatchesClose()
        {
            // tam zırh emri: kapanışta biter
            var sim = ArmorOrder();
            var armor = CloseAndCompareForecast(sim, "zırh");
            Assert.IsTrue(armor.Works && armor.Completes && armor.FromOrder);
            Assert.AreEqual("armor", armor.Output);

            // EMP hücresi: 0/2 → 1/2 → 2/2
            EmpOrder(sim);
            var first = CloseAndCompareForecast(sim, "EMP 1/2");
            Assert.IsTrue(first.Works);
            Assert.IsFalse(first.Completes);
            Assert.AreEqual(1, first.After);
            var second = CloseAndCompareForecast(sim, "EMP 2/2");
            Assert.IsTrue(second.Completes);
            Assert.IsTrue(second.Started);

            // üretici yok: ödeme yok, ilerleme yok
            var idle = ArmorOrder();
            Ok(idle.AssignResident(At(idle, 3).Id, "rest"));
            var noMaker = CloseAndCompareForecast(idle, "üreticisiz");
            Assert.AreEqual(ProductionStop.NoMaker, noMaker.Stop);
            Assert.IsFalse(noMaker.Works);

            // açlık: kapanış erzağı 0 (Core: before + food < 0)
            var hungry = ArmorOrder();
            hungry.Shelter.Supplies = 0;   // test düzeni
            var starving = CloseAndCompareForecast(hungry, "açlık");
            Assert.AreEqual(ProductionStop.Starvation, starving.Stop);
            Assert.IsTrue(hungry.Shelter.Starving, "Core da açlık saydı.");

            // başlamış EMP, ikinci kapanışta üretici yok: ilerlemez, girdiler geri gelmez
            var paused = EmpOrder();
            CloseAndCompareForecast(paused, "EMP başladı");
            Ok(paused.AssignResident(At(paused, 3).Id, "rest"));
            var pausedOutlook = CloseAndCompareForecast(paused, "başlamış · üreticisiz");
            Assert.IsTrue(pausedOutlook.Started);
            Assert.IsFalse(pausedOutlook.Works);

            // deneyimli üretici (kademe 2): EMP tek kapanışta biter
            var fast = Retired();
            At(fast, 3).Experience.Single(e => e.Subject == "craft").Points = fast.B.boardExperiencedAt;   // test düzeni
            EmpOrder(fast);
            var quick = CloseAndCompareForecast(fast, "deneyimli");
            Assert.IsTrue(quick.Completes);
            Assert.AreEqual(2, quick.After);

            // eski konmuş fişler, emir yok: otomatik yol tek kapanışta
            var legacy = Retired();
            Ok(legacy.StageProductionMaterial("metal", 2));
            Ok(legacy.StageProductionMaterial("kumas", 1));
            var auto = CloseAndCompareForecast(legacy, "otomatik");
            Assert.IsFalse(auto.FromOrder);
            Assert.IsTrue(auto.Completes);
        }

        [Test]
        public void Forecast_SameAsSlipFacts()
        {
            void Same(Simulation sim, string label)
            {
                var o = ProductionForecast.Of(sim);
                var p = HandoverSlipFactsBuilder.Build(sim).production;
                if (o.IsNone)
                {
                    Assert.AreEqual(SlipProductionKind.None, p.kind, label);
                    return;
                }
                Assert.AreEqual(o.Works ? SlipProductionKind.Progress : SlipProductionKind.NoOutput, p.kind, label + ": tür");
                Assert.AreEqual(o.Output, p.itemType, label + ": eşya");
                Assert.AreEqual(o.Turns, p.turns, label + ": süre");
                Assert.AreEqual(o.Done, p.done, label + ": biten");
                Assert.AreEqual(o.After, p.after, label + ": sonra");
                Assert.AreEqual(o.Maker, p.maker, label + ": üretici");
            }

            var sim = Retired();
            Same(sim, "V.001 boş");
            Assert.IsTrue(ProductionForecast.Of(sim).IsNone);
            Assert.IsTrue(ProductionForecast.Of(null).IsNone);

            Ok(sim.QueueProduction("armor"));
            Same(sim, "tarif");
            var draft = ProductionForecast.Of(sim);
            Assert.IsFalse(draft.Ready);
            Assert.IsFalse(draft.Works, "Girdisiz emir üretmez.");
            Assert.AreEqual(ProductionStop.None, draft.Stop);
            Assert.IsNull(draft.StopReason);
            Ok(sim.ReserveProductionMaterial("metal", 2));
            Same(sim, "eksik");
            Ok(sim.ReserveProductionMaterial("kumas", 1));
            Same(sim, "tam");
            var full = ProductionForecast.Of(sim);
            Assert.IsTrue(full.Ready && full.Works && full.Completes && full.FromOrder && !full.Started);
            Assert.AreEqual(At(sim, 3).Name, full.Maker);
            Assert.AreEqual(1, full.Turns);
            Assert.AreEqual(0, full.Done);
            Assert.AreEqual(1, full.After);

            Ok(sim.AssignResident(At(sim, 3).Id, "rest"));
            Same(sim, "üreticisiz");
            var none = ProductionForecast.Of(sim);
            Assert.AreEqual(ProductionStop.NoMaker, none.Stop);
            Assert.AreEqual("üretici yok", none.StopReason);
            Assert.IsNull(none.Maker);
            Ok(sim.AssignResident(At(sim, 3).Id, "craft"));

            double supplies = sim.Shelter.Supplies;
            sim.Shelter.Supplies = 0;   // test düzeni
            Same(sim, "açlık");
            Assert.AreEqual(ProductionStop.Starvation, ProductionForecast.Of(sim).Stop);
            Assert.AreEqual("açlık", ProductionForecast.Of(sim).StopReason);
            sim.Shelter.Supplies = supplies;
            Assert.AreEqual(ProductionStop.None, ProductionForecast.Of(sim).Stop);
            FillStore(sim);
            Same(sim, "depo dolu");
            Assert.AreEqual(ProductionStop.StoreFull, ProductionForecast.Of(sim).Stop);
            Assert.AreEqual("eşya deposu dolu", ProductionForecast.Of(sim).StopReason);

            var emp = EmpOrder();
            Close(emp);
            Same(emp, "EMP başladı");
            var started = ProductionForecast.Of(emp);
            Assert.IsTrue(started.Started && started.Ready);
            Assert.AreEqual(1, started.Done);
            Assert.AreEqual(2, started.After);

            var legacy = Retired();
            Ok(legacy.StageProductionMaterial("metal", 1));
            Ok(legacy.StageProductionMaterial("devre", 1));
            Same(legacy, "otomatik");
            var auto = ProductionForecast.Of(legacy);
            Assert.IsFalse(auto.FromOrder);
            Assert.AreEqual("emp_cell", auto.RecipeId, "Otomatik yol: liste sırası (kitler eksik değil).");
            Assert.AreEqual(1, auto.Turns);
            Ok(legacy.AssignResident(At(legacy, 3).Id, "rest"));
            Same(legacy, "otomatik · üreticisiz");
        }

        // ---------------------------------------------------------------- fişler (B: ayrılan girdiler)

        [Test]
        public void Chips_FromReserves_CapEight()
        {
            // B: başlamamış emrin ayrılanı, tür başına bir fiş her birime
            var sim = ArmorOrder();
            var s = Build(sim);
            CollectionAssert.AreEqual(new[] { "chip:metal:0", "chip:metal:1", "chip:kumas:0" }, s.Chips.Select(c => c.Id));
            Assert.AreEqual("KUMAŞ", s.Chips[2].Name);
            Assert.AreEqual(2, s.Materials[0].Held);
            Assert.AreEqual(1, s.Materials[2].Held);
            Ok(sim.ReserveProductionMaterial("metal", 1));
            CollectionAssert.AreEqual(new[] { "chip:metal:0", "chip:kumas:0" }, Build(sim).Chips.Select(c => c.Id));

            // eski (A dönemi) konmuş fişler + ayrılan: tavan 8, türler sırayla paylaşır (§1.8, D2 öncesi)
            var mixed = Retired();
            mixed.AddBoardMaterial("metal", 10);   // test düzeni: 13 metal
            Ok(mixed.StageProductionMaterial("metal", 6));
            Ok(mixed.StageProductionMaterial("devre", 2));
            ArmorOrder(mixed);
            Assert.AreEqual(8, BoardDropRules.ChipCount(mixed.Board, "metal"));
            var capped = Build(mixed);
            CollectionAssert.AreEqual(new[]
            {
                "chip:metal:0", "chip:metal:1", "chip:metal:2", "chip:metal:3", "chip:metal:4", "chip:devre:0", "chip:devre:1", "chip:kumas:0",
            }, capped.Chips.Select(c => c.Id));
            Assert.AreEqual(8, capped.Materials[0].Held, "Tam sayı stok kartında.");
        }

        // ---------------------------------------------------------------- E20 · yeni ürün

        [Test]
        public void NewItem_FromLastCompleted_NewestStored()
        {
            var sim = Retired();
            Assert.IsNull(Build(sim).NewItem, "V.001: son kapanış yok.");
            ArmorOrder(sim);
            Close(sim);
            var s = Build(sim);
            Assert.IsNotNull(s.NewItem);
            Assert.AreEqual("esya-" + (sim.Board.NextEquipmentId - 1), s.NewItem.ItemId);
            Assert.AreEqual("armor", s.NewItem.Kind);
            Assert.AreEqual(System.Array.IndexOf(BoardLayout.KitKinds, "armor"), s.NewItem.Stack);
            Assert.AreEqual(3, s.NewItem.Stack);
            Assert.AreEqual(sim.S.Day, s.NewItem.Day);
            Assert.IsNull(sim.Board.EquipmentItem(s.NewItem.ItemId).OwnerId);
            Assert.AreEqual(1, s.Kits[3].Count, "Yığın sayısı değişmez kuralla.");

            // bir sonraki kapanışta ürün yok: YENİ yok
            Close(sim);
            CollectionAssert.IsEmpty(sim.Board.LastCompleted);
            Assert.IsNull(Build(sim).NewItem);

            // uç durum: bakım kiti çıktı ama bakımda tüketildi (B §5): depoda yoksa yeni ürün yok
            sim.Board.LastCompleted.Add("medkit");   // test düzeni
            sim.Board.Equipment.RemoveAll(e => e.Kind == "medkit");
            Assert.IsNull(Build(sim).NewItem);
            Assert.IsNull(PlanningBoardSnapshotBuilder.NewestStoredProduct(sim.Board));
        }

        [Test]
        public void NewItem_NumericSuffix_FixedForTheDay()
        {
            var sim = Retired();
            for (int i = 0; i < 3; i++) Assert.IsNotNull(sim.AddBoardEquipment("armor"));   // test düzeni: esya-7, 8, 9 depoda
            ArmorOrder(sim);
            Close(sim);
            Assert.AreEqual("esya-10", PlanningBoardSnapshotBuilder.NewestStoredProduct(sim.Board), "Sayısal sonek: esya-10 > esya-9.");
            Assert.Greater(string.CompareOrdinal("esya-9", "esya-10"), 0, "Dizgi sırası yanlış olurdu.");
            Assert.AreEqual(10, PlanningBoardSnapshotBuilder.EquipmentNumber("esya-10"));
            Assert.AreEqual(-1, PlanningBoardSnapshotBuilder.EquipmentNumber("esya-x"));

            var ui = new BoardUiState();
            Assert.IsTrue(PlanningBoardSnapshotBuilder.SyncUi(sim, ui), "Yeni günün ürünü sabitlenir.");
            Assert.AreEqual(sim.S.Day, ui.NewItem.Day);
            Assert.AreEqual("esya-10", ui.NewItem.Id);
            Assert.IsFalse(ui.NewItem.Slid);
            Assert.IsFalse(ui.NewItem.Touched);
            Assert.IsFalse(PlanningBoardSnapshotBuilder.SyncUi(sim, ui), "Aynı gün ikinci eşitleme bir şey değiştirmez.");

            // aynı gün depoya giren aynı türden eşya etiketi çalmaz
            string later = sim.AddBoardEquipment("armor").Id;   // test düzeni: esya-11
            Assert.AreEqual("esya-10", Build(sim, ui).NewItem.ItemId);
            Assert.AreEqual(later, Build(sim, new BoardUiState()).NewItem.ItemId, "Sabitlenmemiş durum aynı kuralı yeniden uygular.");
            Assert.IsFalse(PlanningBoardSnapshotBuilder.SyncUi(sim, ui));
            Assert.AreEqual("esya-10", ui.NewItem.Id);

            // kayış oynadı: etiket kalır; dokunuldu: etiket düşer
            ui.NewItem.Slid = true;
            Assert.AreEqual("esya-10", Build(sim, ui).NewItem.ItemId);
            ui.NewItem.Touched = true;
            Assert.IsNull(Build(sim, ui).NewItem);
            ui.NewItem.Touched = false;

            // eşya depodan çıktı (kişiye verildi): yeni ürün yok
            Ok(sim.EquipResident("esya-10", At(sim, 5).Id));
            Assert.IsNull(Build(sim, ui).NewItem);

            // ertesi gün ürün yok: durum düşer
            Close(sim);
            Assert.IsTrue(PlanningBoardSnapshotBuilder.SyncUi(sim, ui));
            Assert.IsNull(ui.NewItem);
            Assert.IsFalse(PlanningBoardSnapshotBuilder.SyncUi(sim, ui));
        }

        // ---------------------------------------------------------------- E19 · sunum durumunun eşitlenmesi

        [Test]
        public void Chips_LeftoverAfterClose_KeepPoses()
        {
            // eski konmuş fişler: metal 2 + kumaş 1 + devre 1 → otomatik zırh; devre artar ve yerinde kalır
            var sim = Retired();
            Ok(sim.StageProductionMaterial("metal", 2));
            Ok(sim.StageProductionMaterial("kumas", 1));
            Ok(sim.StageProductionMaterial("devre", 1));
            var ui = new BoardUiState();
            ui.SetChipPose(BoardUiState.ChipId("metal", 0), new Vector3(612, 318, -4));
            ui.SetChipPose(BoardUiState.ChipId("metal", 1), new Vector3(640, 322, 3));
            ui.SetChipPose(BoardUiState.ChipId("kumas", 0), new Vector3(668, 330, 7));
            ui.SetChipPose(BoardUiState.ChipId("devre", 0), new Vector3(742.5f, 326.25f, -9));
            Assert.IsFalse(PlanningBoardSnapshotBuilder.SyncUi(sim, ui), "Çizilen her fişin yeri kalır.");
            Assert.AreEqual(4, ui.ChipPoses.Count);
            Close(sim);
            CollectionAssert.AreEqual(new[] { "armor" }, sim.Board.LastCompleted);
            Assert.IsTrue(PlanningBoardSnapshotBuilder.SyncUi(sim, ui));
            Assert.AreEqual(new Vector3(742.5f, 326.25f, -9), ui.ChipPose("chip:devre:0"), "Artan fiş yerinde kalır.");
            Assert.IsNull(ui.ChipPose("chip:metal:0"));
            Assert.IsNull(ui.ChipPose("chip:metal:1"));
            Assert.IsNull(ui.ChipPose("chip:kumas:0"));
            CollectionAssert.AreEqual(new[] { "chip:devre:0" }, Build(sim, ui).Chips.Select(c => c.Id));

            // [B] emir: tarif kartı ve fişlerin yerleri emir bitince silinir
            var order = ArmorOrder();
            string orderId = order.Board.Orders.Single().Id;
            var ou = new BoardUiState();
            ou.SetRecipePose(orderId, new Vector3(520.5f, 331, -6));
            ou.SetChipPose(BoardUiState.ChipId("metal", 0), new Vector3(612, 318, 2));
            ou.SetChipPose(BoardUiState.ChipId("kumas", 0), new Vector3(668, 330, -3));
            ou.RecipePoses["is-99"] = new Vector3(1, 2, 3);   // artık olmayan emir
            Assert.IsTrue(PlanningBoardSnapshotBuilder.SyncUi(order, ou));
            Assert.AreEqual(new Vector3(520.5f, 331, -6), ou.RecipePose(orderId));
            Assert.IsNull(ou.RecipePose("is-99"));
            Assert.AreEqual(2, ou.ChipPoses.Count, "Ayrılan girdilerin fiş yerleri kalır.");
            Close(order);
            Assert.IsTrue(PlanningBoardSnapshotBuilder.SyncUi(order, ou));
            Assert.IsNull(ou.RecipePose(orderId), "Tamamlanan emrin kart yeri silinir.");
            Assert.IsEmpty(ou.ChipPoses, "Harcanan girdilerin fiş yerleri silinir.");
            Assert.IsNotNull(ou.NewItem, "Kapanışın ürünü sabitlendi.");
        }

        // ---------------------------------------------------------------- saflık (genişletilmiş)

        [Test]
        public void Builder_IsPure()
        {
            var sim = EmpOrder();
            Close(sim);   // başlamış EMP emri, yeni gün
            Ok(sim.StageProductionMaterial("kumas", 1));
            var ui = new BoardUiState { SelectedId = At(sim, 0).Id, TrainingTopic = "cook", BookSpread = 2 };
            ui.SetRecipePose(sim.Board.Orders.Single().Id, new Vector3(520, 330, -6));
            ui.SetChipPose(BoardUiState.ChipId("kumas", 0), new Vector3(668, 330, 4));
            ui.SetChipPose(BoardUiState.ChipId("metal", 5), new Vector3(612, 318, 0));   // çizilmeyen fiş: kurucu silmez
            ui.NewItem = new NewItemState { Day = sim.S.Day, Id = "esya-404", Slid = true };
            string before = Fingerprint(sim);
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count;
            string uiBefore = UiDump(ui);

            var first = Build(sim, ui);
            Build(sim, ui, inputBlocked: true, storyHold: true);
            Build(sim, new BoardUiState());
            ProductionForecast.Of(sim);
            PlanningBoardSnapshotBuilder.NewItemOf(sim, ui);
            Loc.SetLanguage(Loc.English, new Dictionary<string, string>());
            Build(sim, ui, english: true);
            Loc.SetLanguage(Loc.Turkish);
            var again = Build(sim, ui);

            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(before, Fingerprint(sim));
            Assert.AreEqual(uiBefore, UiDump(ui), "Kurucu sunum durumunu da değiştirmez (eşitleme SyncUi'nin işi).");
            Assert.AreEqual(first.Order.Status, again.Order.Status);
            Assert.AreEqual(first.Order.State, again.Order.State);
            CollectionAssert.AreEqual(first.Recipes.Select(r => r.Title), again.Recipes.Select(r => r.Title));
            CollectionAssert.AreEqual(first.Chips.Select(c => c.Id), again.Chips.Select(c => c.Id));
            Assert.IsNull(first.NewItem, "Sabitlenmiş kimlik depoda yoksa yeni ürün yok.");
        }

        static string UiDump(BoardUiState ui)
        {
            var sb = new StringBuilder();
            sb.Append(ui.SelectedId).Append('|').Append(ui.TrainingTopic).Append('|').Append(ui.BookSpread).Append('|');
            foreach (var kv in ui.ChipPoses.OrderBy(k => k.Key, System.StringComparer.Ordinal)) sb.Append(kv.Key).Append(kv.Value).Append(';');
            foreach (var kv in ui.RecipePoses.OrderBy(k => k.Key, System.StringComparer.Ordinal)) sb.Append(kv.Key).Append(kv.Value).Append(';');
            var n = ui.NewItem;
            sb.Append(n == null ? "-" : n.Day + n.Id + n.Slid + n.Touched);
            return sb.ToString();
        }

        // ================================================================ kişi dosyasındaki iz (yeni zincir; aşama 7'de yeniden yazıldı)

        /// <summary>
        /// Olaylar açık, sızmacı olasılığı 1, insan izi yok, iz gecikmesi 1, her kapanışta grup: ilk grup geri çevrilir, ikinci grubun ilk
        /// adayı (sızmacı) alınır, ÜRETİM'e konur, kapı kapatılır ve iki kapanış geçer (iz ve ikinci iz; açılan her olayda ilk açık seçenek, izde "yoksay").
        /// </summary>
        static Simulation AdmittedInfiltrator(out string id)
        {
            var sim = Simulation.NewGame(79, new Balance
            {
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1, candidateEmergencyBelow = 0,
                candidateTier0Robot = 1, candidateTier1Robot = 1, candidateTier2Robot = 1, humanTraceChance = 0, humanSecondTraceChance = 0,
                traceDelayMin = 1, traceDelayMax = 1, secondTraceGapMin = 1, secondTraceGapMax = 1,
            });
            Ok(sim.EnableAutomaticProduction());
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Ok(sim.AssignResident(p.Id, "rest"));
            for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++) Close(sim);
            Ok(sim.PassCandidates());
            for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++) { DecideAny(sim); Close(sim); }
            id = sim.Admission.Group[0].Resident.Id;
            Ok(sim.ChooseCandidate(id));
            Ok(sim.AssignResident(id, "craft"));
            sim.Admission.NextArrivalShift = 100000;   // test düzeni: kapı kapalı
            for (int i = 0; i < 2; i++)
            {
                DecideAny(sim);
                if (sim.Admission.Group.Count > 0) Ok(sim.PassCandidates());
                Close(sim);
            }
            DecideAny(sim);
            return sim;
        }

        static void DecideAny(Simulation sim)
        {
            if (sim.PendingShelterEvent == null) return;
            var open = sim.ShelterEventOptions().Where(o => o.Open).ToList();
            var pick = open.FirstOrDefault(o => o.Id == "yoksay") ?? open.First();
            Ok(sim.DecideShelterEvent(pick.Id));
        }

        static FolderData FolderOf(Simulation sim, string id, bool english = false) =>
            Build(sim, new BoardUiState { SelectedId = id }, english).Folder;

        static IEnumerable<string> FolderTexts(FolderData f)
        {
            yield return f.Name;
            yield return f.TagText;
            foreach (var l in f.Left.Concat(f.Right).Concat(f.DossierRight)) { yield return l.Label; yield return l.Value; }
        }

        static IEnumerable<string> CardTexts(PersonCardData c)
        {
            yield return c.Name; yield return c.Job; yield return c.Strip; yield return c.HoverTitle; yield return c.HoverLine;
            foreach (var t in c.StatLabels.Concat(c.StatValues)) yield return t;
        }

        static bool SaysRobot(string t) => t != null && t.IndexOf("robot", System.StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Kişi dosyası ve kartlar hiçbir dilde "robot" demez; sızmacının dosyasında izler ve KARAR satırı görünür (TR ve EN).</summary>
        [Test]
        public void Folder_NeverSaysRobot_TrEn()
        {
            var sim = AdmittedInfiltrator(out string id);
            var file = sim.Admission.File(id);
            Assert.GreaterOrEqual(file.ClueCount, 1, "test düzeni: sızmacının izi dosyada");
            foreach (bool english in new[] { false, true })
            {
                if (english) Rev3AdmissionEnglish.Use(); else Loc.SetLanguage(Loc.Turkish);
                foreach (var p in sim.Board.People.Where(x => x.Alive))
                {
                    var f = FolderOf(sim, p.Id, english);
                    foreach (var t in FolderTexts(f)) Assert.IsFalse(SaysRobot(t), p.Name + ": " + t);
                    var s = Build(sim, new BoardUiState { SelectedId = p.Id }, english);
                    foreach (var t in CardTexts(s.Person(p.Id))) Assert.IsFalse(SaysRobot(t), p.Name + ": " + t);
                }
                var mine = FolderOf(sim, id, english);
                Assert.IsTrue(mine.Right.Any(l => l.Section && l.Label == (english ? Loc.T("İZLER") : "İZLER")), "izler bölümü");
                Assert.AreEqual(1, mine.Right.Count(l => l.Value != null && l.Value.StartsWith(english ? "DECISION" : "KARAR")), "karar satırı");
            }
            Loc.SetLanguage(Loc.Turkish);
        }

        [Test]
        public void Folder_FileTag_Restricted()
        {
            // Kurucular: etiket yok
            var sim = Retired();
            foreach (var p in sim.Board.People) Assert.IsNull(FolderOf(sim, p.Id).TagText, p.Name);

            // Kısıtlı: "KISITLI · n V" (kalan vardiya; plan eki D16), süre dolunca "KISITLI"
            var r = sim.Board.People.First(x => x.Alive && x.Job == "cook");
            Restrict(sim, r.Id);
            Assert.AreEqual("KISITLI · 3 V", FolderOf(sim, r.Id).TagText);
            Close(sim);
            Assert.AreEqual("KISITLI · 2 V", FolderOf(sim, r.Id).TagText);
            Close(sim);
            Close(sim);
            Assert.AreEqual(sim.S.Day, sim.Admission.File(r.Id).RestrictedUntil);
            Assert.AreEqual("KISITLI", FolderOf(sim, r.Id).TagText, "süresi doldu: karar bekler");
            Rev3AdmissionEnglish.Use();
            Assert.AreEqual("RESTRICTED", FolderOf(sim, r.Id, true).TagText);
            Loc.SetLanguage(Loc.Turkish);
        }

    }
}
