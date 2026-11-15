using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Aday grubu (biçim 16): kapıya 2–3 aday gelir, en çok biri alınır. Çekim sırası kayıt biçiminin parçasıdır (her üretim tam 32
    /// çekim; tohum 79 çizelgesi kilitli), komutlar ve okuyucular RNG çekmez, gizli kimlik görünür veriyi değiştirmez, ilk grup ve
    /// kurucular sızmacı olamaz. Kişiye bağlı teçhizat, taksitler, dolu sığınakta yer açarak seçim, acil grup, kilitli planda seçim,
    /// kit kilidi ve kayıttan devam burada sınanır. Test derlemesi Core'un iç (internal) alanlarını göremez: kimlik yalnız davranışla
    /// ve olasılıkları 0/1 yaparak sınanır.
    /// </summary>
    public class CandidateGroupTests
    {
        const int DrawsPerGroup = 32;

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- kurulum

        /// <summary>Kapıya her kapanışta (kapı boşsa) bir grup gelir; alarm kapalı, erzak bol.</summary>
        static Balance Bal(int knock = 1) => new Balance
        { shelterEventsEnabled = false,
            planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = knock, admissionKnockMax = knock,
            boardStartSupplies = 60, boardSuppliesCapacity = 100,
        };

        static Simulation New(uint seed = 79, Balance balance = null) => Simulation.NewGame(seed, balance ?? Bal());

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        static List<CommunityApplicant> NextGroup(Simulation sim)
        {
            int limit = sim.S.Day + 8;
            while (sim.Admission.Group.Count == 0 && sim.S.Day < limit) Close(sim);
            Assert.Greater(sim.Admission.Group.Count, 0, "kapıya grup gelmedi");
            return sim.Admission.Group;
        }

        /// <summary>Aday kartının oyuncuya görünen bütün değerleri.</summary>
        static string Card(CommunityApplicant c)
        {
            var r = c.Resident;
            return string.Join(" ", new[]
            {
                r.Id, r.Name, r.TrainingSubject, "k" + c.Tier, string.Join("+", c.Perks), c.SuspicionKey, c.SuspicionArg ?? "-",
                "can" + r.Hp + "/" + r.MaxHp, "h" + r.Move, "g" + r.Power, "yatis" + r.Down, "usta" + r.Skill(r.TrainingSubject),
            });
        }

        static string Cards(Simulation sim) => string.Join(" ; ", sim.Admission.Group.Select(Card));

        static string Passed(Simulation sim) => string.Join(",", sim.Admission.Passed.Select(p => p.Name + ":" + p.Kind + ":" + p.Tier + ":" + p.Shift));

        static string BoardJson(Simulation sim) => UnityEngine.JsonUtility.ToJson(sim.Board);

        /// <summary>
        /// Tohumları sırayla tarar ve koşulu sağlayan ilk adayın kapıda beklediği oyunu döndürür. Deterministiktir; hangi tohumda
        /// hangi özelliğin çıktığını teste gömmez.
        /// </summary>
        static Simulation Find(Func<CommunityApplicant, bool> match, out CommunityApplicant found, Func<Balance> balance = null)
        {
            for (uint seed = 1; seed <= 300; seed++)
            {
                var sim = New(seed, balance != null ? balance() : Bal());
                for (int g = 0; g < 6; g++)
                {
                    found = NextGroup(sim).FirstOrDefault(match);
                    if (found != null) return sim;
                    Assert.IsTrue(sim.PassCandidates().Ok);
                }
            }
            found = null;
            Assert.Fail("300 tohumda koşulu sağlayan aday çıkmadı");
            return null;
        }

        /// <summary>Kadronun üçü de sahada kalır (kişiler ölür, teçhizatları yok olur).</summary>
        static void LoseWholeSquad(Simulation sim)
        {
            var started = sim.StartLiberationRaid("weu_1");
            Assert.IsTrue(started.Ok, started.Msg);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Down = 3, Hp = { 0, 0, 0 }, Armor = { 0, 0, 0 } }).Ok);
        }

        /// <summary>Kadro kayıpsız geri çekilir; plan kilitli kalır.</summary>
        static void RetreatUnharmed(Simulation sim)
        {
            var squad = sim.PlanningSquad();
            var result = new RaidResult { Retreat = true };
            foreach (var slot in squad) { result.Hp.Add(slot.Hp); result.Armor.Add(slot.Armor); }
            Assert.IsTrue(sim.ResolveRaid(result).Ok);
        }

        static uint After(uint state, int draws)
        {
            var rng = new Rng(state);
            for (int i = 0; i < draws; i++) rng.Next();
            return rng.State;
        }

        // ---------------------------------------------------------------- çekim sırası

        [Test]
        public void Generation_DrawsExactly32_ForEverySize_WhateverTheChancesAndShares()
        {
            var variants = new Func<Balance>[]
            {
                () => Bal(),
                () => { var b = Bal(); b.candidateGroupMin = 2; b.candidateGroupMax = 2; return b; },
                () => { var b = Bal(); b.candidateGroupMin = 3; b.candidateGroupMax = 3; return b; },
                () => { var b = Bal(); b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = 1; b.humanTraceChance = 1; b.humanSecondTraceChance = 1; return b; },
                () => { var b = Bal(); b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = 0; b.humanTraceChance = 0; b.humanSecondTraceChance = 0; return b; },
                () => { var b = Bal(); b.candidateTier0Share = 1; b.candidateTier0CostChance = 1; return b; },
                () => { var b = Bal(); b.candidateTier0Share = 0; b.candidateTier2Share = 1; return b; },
                () => { var b = Bal(); b.candidateEmergencyBelow = 9; return b; },
            };
            var sizes = new HashSet<int>();
            for (int v = 0; v < variants.Length; v++)
            {
                for (uint seed = 1; seed <= 12; seed++)
                {
                    var sim = New(seed, variants[v]());
                    for (int g = 0; g < 4; g++)
                    {
                        Assert.AreEqual(0, sim.Admission.Group.Count);
                        Assert.IsTrue(sim.PrintShift().Ok);
                        uint before = sim.S.Rng.State;
                        int groups = sim.Admission.GeneratedGroups;
                        Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
                        Assert.AreEqual(groups + 1, sim.Admission.GeneratedGroups, "kapı boşken her kapanış bir grup üretir");
                        Assert.AreEqual(After(before, DrawsPerGroup), sim.S.Rng.State, "çeşit " + v + ", tohum " + seed + ", grup " + g);
                        sizes.Add(sim.Admission.Group.Count);
                        Assert.AreEqual(v == 7, sim.Admission.GroupEmergency);
                        Assert.IsTrue(sim.PassCandidates().Ok);
                    }
                }
            }
            CollectionAssert.AreEquivalent(new[] { 2, 3 }, sizes, "iki ve üç kişilik gruplar da aynı sayıda çekim yapar");
        }

        [Test]
        public void ClosedDoor_DrawsNothingAtClosing()
        {
            var sim = New(79, Bal(100));
            for (int i = 0; i < 6; i++)
            {
                uint before = sim.S.Rng.State;
                Close(sim);
                Assert.AreEqual(before, sim.S.Rng.State, "kapı kapalıyken kapanış çekim yapmaz");
            }
            Assert.AreEqual(0, sim.Admission.GeneratedGroups);
            Assert.AreEqual(0, sim.Admission.Group.Count);
        }

        /// <summary>Varsayılan denge (yalnız alarm kapalı), kimse karar vermez: üretilen grupların zaman çizelgesi (değerler gerçek Core'dan üretildi).</summary>
        static List<string> Timeline(Simulation sim, int groups, int lastShift)
        {
            var lines = new List<string>();
            int seen = 0;
            while (sim.S.Day < lastShift && seen < groups)
            {
                Close(sim);
                var a = sim.Admission;
                if (a.GeneratedGroups == seen) continue;
                seen = a.GeneratedGroups;
                lines.Add("vardiya " + sim.S.Day + " · gelis " + a.GroupArrivalShift + " · son " + a.GroupWaitingUntil + " · sonraki " + a.NextArrivalShift
                    + " · acil " + (a.GroupEmergency ? 1 : 0) + " · aday " + a.GeneratedApplicants + " · rng " + sim.S.Rng.State);
                foreach (var c in a.Group) lines.Add(Card(c));
            }
            return lines;
        }

        [Test]
        public void Seed79_FirstThreeGroups_GoldenTimeline()
        {
            var sim = Simulation.NewGame(79, new Balance { shelterEventsEnabled = false, liberationAlarmPerShift = 0 });
            Assert.AreEqual(4, sim.Admission.NextArrivalShift);
            var seen = Timeline(sim, 3, 20);
            var expected = new[]
            {
                "vardiya 4 · gelis 4 · son 6 · sonraki 7 · acil 0 · aday 2 · rng 3215543326",
                "resident-9 Dilan craft k2 saglam+devre tek_basina - can3/3 h4 g1 yatis0 usta2",
                "resident-10 Yalcin cook k1 erzak cift_kayit - can4/4 h2 g1 yatis0 usta2",
                "vardiya 7 · gelis 7 · son 9 · sonraki 10 · acil 0 · aday 4 · rng 1696107198",
                "resident-11 Feride cook k2 devre+erzak yarasiz - can4/4 h2 g1 yatis0 usta2",
                "resident-12 Umut craft k1 erzak cift_kayit - can4/4 h2 g1 yatis0 usta2",
                "vardiya 10 · gelis 10 · son 12 · sonraki 14 · acil 0 · aday 6 · rng 176671070",
                "resident-13 Ayca tek k0 saglam kefil Orhan can4/4 h2 g2 yatis0 usta2",
                "resident-14 Levent cook k1 devre belge_yanmis - can3/3 h3 g1 yatis0 usta2",
            };
            CollectionAssert.AreEqual(expected, seen, "gözlenen:\n" + string.Join("\n", seen));
        }

        /// <summary>Üç kişilik gruplar (tohum 3): üçüncü yuvanın çekimleri de kilitli.</summary>
        [Test]
        public void Seed3_ThreeCandidateGroups_GoldenTimeline()
        {
            var sim = Simulation.NewGame(3, new Balance { shelterEventsEnabled = false, liberationAlarmPerShift = 0 });
            Assert.AreEqual(3, sim.Admission.NextArrivalShift);
            var seen = Timeline(sim, 3, 20);
            var expected = new[]
            {
                "vardiya 3 · gelis 3 · son 5 · sonraki 7 · acil 0 · aday 3 · rng 3215543250",
                "resident-9 Ece cook k0 zirh sevk_tam - can3/3 h2 g2 yatis0 usta2",
                "resident-10 Murat emp k0 obur sevk_tam - can3/3 h2 g2 yatis0 usta2",
                "resident-11 Aylin ana k1 erzak belge_yanmis - can4/4 h2 g1 yatis0 usta2",
                "vardiya 7 · gelis 7 · son 9 · sonraki 11 · acil 0 · aday 6 · rng 1696107122",
                "resident-12 Baran emp k1 devre cift_kayit - can3/3 h2 g2 yatis0 usta2",
                "resident-13 Banu tek k1 saglam belge_yanmis - can5/5 h2 g1 yatis0 usta2",
                "resident-14 Ozan emp k0 erzak sevk_tam - can3/3 h3 g1 yatis0 usta2",
                "vardiya 11 · gelis 11 · son 13 · sonraki 15 · acil 0 · aday 9 · rng 176670994",
                "resident-15 Lale craft k2 saglam+devre gece_gecis - can4/4 h3 g1 yatis0 usta2",
                "resident-16 Sinan cook k1 devre belge_yanmis - can2/2 h4 g1 yatis0 usta2",
                "resident-17 Asli tek k2 kit+usta tek_basina - can2/2 h4 g1 yatis0 usta6",
            };
            CollectionAssert.AreEqual(expected, seen, "gözlenen:\n" + string.Join("\n", seen));
        }

        // ---------------------------------------------------------------- komutlar RNG çekmez

        [Test]
        public void CommandsAndReaders_ConsumeNoRandomness_AndFailedCommandsAreNotRecorded()
        {
            var sim = New();
            var group = NextGroup(sim);
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count;
            string first = group[0].Resident.Id, founder = sim.Board.People[0].Id;

            for (int i = 0; i < 10; i++)
            {
                sim.CandidateChoiceCheck(first, null);
                sim.CandidateChoiceCheck(first, founder);
                sim.CandidatePassCheck();
                Assert.AreEqual(Math.Min(sim.Shelter.Supplies, sim.B.dismissSupplies), sim.DismissSuppliesPreview);
                Assert.IsNotNull(sim.BoardReceiptPreview);
                Assert.IsFalse(sim.BoardKitMissing("kit_tek"));
                Assert.AreEqual(6, sim.BoardRecipes.Count);
                foreach (var c in group)
                {
                    Assert.IsNotEmpty(CandidateData.TierName(c.Tier));
                    Assert.IsNotEmpty(CandidateData.SuspicionLine(c.SuspicionKey, c.SuspicionArg));
                    foreach (string perk in c.Perks)
                    {
                        Assert.IsNotEmpty(CandidateData.PerkName(perk, sim.B));
                        Assert.IsNotEmpty(CandidateData.PerkEffect(perk, sim.B, c.Resident.TrainingSubject));
                    }
                }
            }
            Assert.AreEqual(rng, sim.S.Rng.State, "okuyucular çekim yapmaz");
            Assert.AreEqual(commands, sim.Record.commands.Count);

            // Reddedilen komutlar: bilinmeyen aday, yer varken yollama
            Assert.IsFalse(sim.ChooseCandidate("resident-yok").Ok);
            Assert.IsFalse(sim.ChooseCandidate(null).Ok);
            Assert.IsFalse(sim.ChooseCandidate(first, founder).Ok);
            Assert.AreEqual(commands, sim.Record.commands.Count, "başarısız komut kayda yazılmaz");
            Assert.AreEqual(rng, sim.S.Rng.State);

            Assert.IsTrue(sim.ChooseCandidate(first).Ok);
            Assert.AreEqual(rng, sim.S.Rng.State, "seçim çekim yapmaz");
            Assert.AreEqual(commands + 1, sim.Record.commands.Count);
            var saved = sim.Record.commands.Last();
            Assert.AreEqual("candidate_choose", saved.kind);
            Assert.AreEqual(first, saved.a);
            Assert.IsNull(saved.b);
            Assert.AreEqual(sim.S.Day, saved.day);

            Assert.IsFalse(sim.PassCandidates().Ok, "kapı boş");
            Assert.AreEqual(commands + 1, sim.Record.commands.Count);

            NextGroup(sim);
            rng = sim.S.Rng.State;
            Assert.IsTrue(sim.PassCandidates().Ok);
            Assert.AreEqual(rng, sim.S.Rng.State, "geri çevirme çekim yapmaz");
            Assert.AreEqual("candidate_pass", sim.Record.commands.Last().kind);
        }

        [Test]
        public void DisplayHelpers_TranslateEveryIdentifier()
        {
            var b = new Balance { shelterEventsEnabled = false };
            foreach (string perk in CandidateData.Benefits.Concat(CandidateData.Costs))
            {
                Assert.AreNotEqual(perk, CandidateData.PerkName(perk, b), perk);
                Assert.AreNotEqual(perk, CandidateData.PerkEffect(perk, b, "tek"), perk);
                Assert.AreEqual(Array.IndexOf(CandidateData.Costs, perk) >= 0, CandidateData.IsCost(perk));
            }
            foreach (string[] tier in CandidateData.Suspicions)
            {
                Assert.GreaterOrEqual(tier.Length, 3, "her kademede en az üç şüphe satırı");
                foreach (string key in tier) Assert.AreNotEqual(key, CandidateData.SuspicionLine(key, "Derya"), key);
            }
            StringAssert.Contains("Derya", CandidateData.SuspicionLine("kefil", "Derya"));
            Assert.AreEqual(3, new[] { 0, 1, 2 }.Select(CandidateData.TierName).Distinct().Count());
            StringAssert.Contains(PlanningBoardData.EquipmentName("kit_emp"), CandidateData.PerkEffect("kit", b, "emp"));
        }

        /// <summary>Yeni kapı metinlerinin hepsi çeviri tablosunda: İngilizce oyunda eksik metin ve Türkçe harf yok.</summary>
        [Test]
        public void English_DoorTexts_AreTranslated()
        {
            var missing = new HashSet<string>();
            Action<string> onMissing = x => missing.Add(x);
            Loc.Missing += onMissing;
            try
            {
                Loc.SetLanguage(Loc.English, Loc.ParseTable(System.IO.File.ReadAllText(Corrigible.EditorTools.LocalizationTool.TableFile))[Loc.English]);
                var b = new Balance { shelterEventsEnabled = false };
                var texts = new List<string>();
                for (int tier = 0; tier < 3; tier++) texts.Add(CandidateData.TierName(tier));
                foreach (string key in CandidateData.Suspicions.SelectMany(x => x)) texts.Add(CandidateData.SuspicionLine(key, "Derya"));
                foreach (string perk in CandidateData.Benefits.Concat(CandidateData.Costs))
                {
                    texts.Add(CandidateData.PerkName(perk, b));
                    foreach (string subject in PlanningBoardData.FieldJobs) texts.Add(CandidateData.PerkEffect(perk, b, subject));
                }

                var sim = New(79, BoundKits());
                var group = NextGroup(sim);
                texts.AddRange(sim.BoardReceiptPreview.Split('\n').Where(line => line.StartsWith("DOOR")));
                texts.Add(sim.ChooseCandidate("resident-yok").Msg);
                texts.Add(sim.ChooseCandidate(group[0].Resident.Id, "sakin-1").Msg);
                var chosen = sim.ChooseCandidate(group[0].Resident.Id);
                Assert.IsTrue(chosen.Ok);
                texts.Add(chosen.Msg);
                var kit = sim.Board.Equipment.First(e => e.Bound);
                texts.Add(sim.EquipResident(kit.Id, null).Msg);
                texts.Add(sim.ScrapEquipment(kit.Id).Msg);
                texts.Add(sim.EquipResident(sim.Board.Equipment.First(e => !e.Bound && e.Kind.StartsWith("kit_")).Id, kit.OwnerId).Msg);
                texts.Add(sim.PassCandidates().Msg);
                group = NextGroup(sim);
                texts.Add(sim.ChooseCandidate(group[0].Resident.Id).Msg);
                texts.Add(sim.ChooseCandidate(group[0].Resident.Id, "sakin-99").Msg);
                Close(sim);
                Assert.Greater(sim.Admission.Group.Count, 0);
                texts.AddRange(sim.BoardReceiptPreview.Split('\n').Where(line => line.StartsWith("DOOR")));
                Assert.IsTrue(sim.PrintShift().Ok);
                texts.Add(sim.PassCandidates().Msg);
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
                NextGroup(sim);
                var passed = sim.PassCandidates();
                Assert.IsTrue(passed.Ok);
                texts.Add(passed.Msg);

                Assert.IsEmpty(missing, "Tabloda olmayan metinler:\n" + string.Join("\n", missing));
                Assert.Greater(texts.Count, 55);
                Assert.AreEqual(3, texts.Count(t => t.StartsWith("DOOR")), "fişin iki kapı satırı da çevrildi");
                var turkish = texts.Where(t => string.IsNullOrEmpty(t) || System.Text.RegularExpressions.Regex.IsMatch(t, "[ğüşıöçĞÜŞİÖÇ]")).Distinct().ToList();
                Assert.IsEmpty(turkish, "Türkçe kalan metinler:\n" + string.Join("\n", turkish));
            }
            finally
            {
                Loc.Missing -= onMissing;
                Loc.SetLanguage(Loc.Turkish);
            }
        }

        // ---------------------------------------------------------------- gizlilik (1. parça)

        static Balance HiddenChances(double p)
        {
            var b = Bal();
            b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = p;
            b.humanTraceChance = p;
            b.humanSecondTraceChance = p;
            return b;
        }

        /// <summary>
        /// İki grup; ikincisinden bir aday alınır. <paramref name="work"/> değilse kişi Boşta kalır (erişimi olmayan sızmacı iz
        /// bırakmaz) ve iki kapanış geçer. <paramref name="work"/> ise kişi Mutfağa konur ve yalnız yerleşme vardiyası kapanır:
        /// sızmacı erişim kazanmıştır ama ilk izini henüz bırakmamıştır; o ana kadar da hiçbir public değer ayrışmamalıdır.
        /// </summary>
        static List<string> VisibleStory(uint seed, Balance balance, bool work = false)
        {
            var sim = New(seed, balance);
            var story = new List<string>();
            NextGroup(sim);
            story.Add(Cards(sim)); story.Add(BoardJson(sim));
            Assert.IsTrue(sim.PassCandidates().Ok);
            var second = NextGroup(sim);
            story.Add(Cards(sim)); story.Add(BoardJson(sim));
            string id = second[0].Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            story.Add(BoardJson(sim));
            if (work)
            {
                // Yaralı gelen aday görev alamaz; sonuç kimlikten bağımsızdır ve hikâyeye yazılır
                story.Add("mutfak:" + sim.AssignResident(id, "cook").Ok);
                Close(sim);
                Assert.AreEqual(0, sim.Admission.File(id).ClueCount, "test düzeni: yerleşme vardiyasında iz yok");
            }
            else { Close(sim); Close(sim); }
            story.Add(Cards(sim)); story.Add(BoardJson(sim));
            story.Add(sim.S.Rng.State + "|" + sim.Shelter.Supplies + "|" + sim.S.Fon + "|" + sim.S.Alarm + "|" + sim.S.Trust + "|" + Passed(sim));
            return story;
        }

        [Test]
        public void HiddenIdentity_FlippedChances_LeaveEveryPublicValueIdentical()
        {
            for (uint seed = 1; seed <= 10; seed++)
            {
                var none = VisibleStory(seed, HiddenChances(0));
                var normal = VisibleStory(seed, Bal());
                var all = VisibleStory(seed, HiddenChances(1));
                CollectionAssert.AreEqual(none, normal, "tohum " + seed);
                CollectionAssert.AreEqual(none, all, "tohum " + seed);
            }
        }

        /// <summary>Çalışan sızmacı da ilk izine kadar görünmez: erişim defteri (<c>LastAccessJob</c>) public duruma sızmaz.</summary>
        [Test]
        public void HiddenIdentity_WorkingResident_StaysIdenticalUntilTheFirstTrace()
        {
            int worked = 0;
            for (uint seed = 1; seed <= 20; seed++)
            {
                var none = VisibleStory(seed, HiddenChances(0), true);
                var normal = VisibleStory(seed, Bal(), true);
                var all = VisibleStory(seed, HiddenChances(1), true);
                CollectionAssert.AreEqual(none, normal, "tohum " + seed);
                CollectionAssert.AreEqual(none, all, "tohum " + seed);
                if (none.Contains("mutfak:True")) worked++;
            }
            Assert.Greater(worked, 10, "test düzeni: adayların çoğu göreve konabildi");
        }

        [Test]
        public void PublicJson_CarriesNoHiddenField()
        {
            var sim = New(79, HiddenChances(1));
            NextGroup(sim);
            Assert.IsTrue(sim.PassCandidates().Ok);
            string chosen = NextGroup(sim)[0].Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(chosen).Ok);
            string json = BoardJson(sim);
            StringAssert.Contains("\"Group\"", json, "pano JSON'u kabul durumunu taşır");
            StringAssert.Contains("\"Passed\"", json);
            foreach (string hidden in new[] { "Robot", "TraceDelay", "SecondTraceGap", "HumanTrace", "HumanSecondTrace", "TraceClock", "Sabotaged" })
                StringAssert.DoesNotContain(hidden, json, hidden);

            // Yansıma: gizli alanların hiçbiri public değildir
            Assert.AreEqual(0, typeof(AdmissionIdentity).GetFields(BindingFlags.Instance | BindingFlags.Public).Length);
            Assert.IsNull(typeof(PassedCandidate).GetField("Robot", BindingFlags.Instance | BindingFlags.Public));
            // Erişim defteri yalnız sızmacıda yazılır: public olamaz
            foreach (string field in new[] { "LastAccessJob", "TraceClock", "Sabotaged" })
            {
                Assert.IsNull(typeof(ResidentAdmissionFile).GetField(field, BindingFlags.Instance | BindingFlags.Public), field);
                Assert.IsNotNull(typeof(ResidentAdmissionFile).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic), field);
            }

            // Kişi bir vardiya çalıştıktan sonra da pano JSON'u erişim defterini taşımaz
            sim.AssignResident(chosen, "cook");
            Close(sim);
            json = BoardJson(sim);
            foreach (string hidden in new[] { "LastAccessJob", "TraceClock", "Sabotaged", "Robot" })
                StringAssert.DoesNotContain(hidden, json, hidden);
            Assert.IsNotNull(typeof(PassedCandidate).GetField("Robot", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        // ---------------------------------------------------------------- ilk grup ve kurucular

        /// <summary>
        /// İlk grup ve kurucular sızmacı olamaz, sonraki gruplar olabilir (olaylar açık, sızmacı olasılığı 1, insan izi yok). Kimlik
        /// okunmaz, davranış sınanır: ilk gruptan alınan kişi ve kurucular uzun kapanış dizisinde iz bırakmaz; ikinci gruptan alınan
        /// kişinin izi gecikmesi (1) dolunca açılır.
        /// </summary>
        [Test]
        public void FirstGroupAndFounders_CannotBeInfiltrators_LaterGroupsCan()
        {
            var first = ShelterEventKit.New(ShelterEventKit.Robots(ShelterEventKit.Quiet(1)));
            ShelterEventKit.Admit(first, 0);
            ShelterEventKit.ShutDoor(first);
            for (int i = 0; i < 8; i++) ShelterEventKit.CloseTo(first, "-");
            Assert.AreEqual(0, ShelterEventKit.Count(first, "iz"));
            Assert.IsTrue(first.Admission.Files.All(f => f.ClueCount == 0 && f.TraceAreas.Count == 0 && !f.Revealed), "ilk grup ve kurucular iz bırakmaz");

            var later = ShelterEventKit.New(ShelterEventKit.Robots(ShelterEventKit.Quiet(1)));
            string id = ShelterEventKit.Admit(later, 1);
            ShelterEventKit.ShutDoor(later);
            var e = ShelterEventKit.CloseTo(later, "iz");
            Assert.AreEqual(id, e.PersonId, "ikinci grubun adayı sızmacı olabilir");
            Assert.IsTrue(later.Admission.Files.Where(f => f.Founder).All(f => f.ClueCount == 0), "kurucular zincire girmez");
        }

        [Test]
        public void ChancesZero_NobodyLeavesATrace()
        {
            var b = HiddenChances(0);
            b.candidateTier0CostChance = 0;
            var sim = New(79, b);
            NextGroup(sim);
            Assert.IsTrue(sim.PassCandidates().Ok);
            var person = NextGroup(sim)[0].Resident;
            Assert.IsTrue(sim.ChooseCandidate(person.Id).Ok);
            Assert.IsTrue(sim.AssignResident(person.Id, "gather").Ok);
            for (int i = 0; i < 6; i++) Close(sim);
            Assert.AreEqual(0, sim.Admission.File(person.Id).ClueCount);
            CollectionAssert.IsEmpty(sim.Admission.File(person.Id).TraceAreas);
            Assert.IsFalse(sim.Admission.File(person.Id).Revealed);
        }

        // ---------------------------------------------------------------- kademe ve özellik kuralları

        [Test]
        public void SingleTierGroup_ShiftsTheLastCandidate()
        {
            for (uint seed = 1; seed <= 12; seed++)
            {
                var allVouched = Bal(); allVouched.candidateTier0Share = 1;
                var group = NextGroup(New(seed, allVouched));
                Assert.IsTrue(group.Take(group.Count - 1).All(c => c.Tier == 0));
                Assert.AreEqual(1, group.Last().Tier, "0 → 1");

                var allSuspect = Bal(); allSuspect.candidateTier0Share = 0; allSuspect.candidateTier2Share = 1;
                group = NextGroup(New(seed, allSuspect));
                Assert.IsTrue(group.Take(group.Count - 1).All(c => c.Tier == 2));
                Assert.AreEqual(1, group.Last().Tier, "2 → 1");

                var allUnregistered = Bal(); allUnregistered.candidateTier0Share = 0; allUnregistered.candidateTier2Share = 0;
                group = NextGroup(New(seed, allUnregistered));
                Assert.IsTrue(group.Take(group.Count - 1).All(c => c.Tier == 1));
                Assert.AreEqual(2, group.Last().Tier, "1 → 2");

                // Kaydırılan adayın şüphe satırı ve özellikleri yeni kademeden okunur
                Assert.Contains(group.Last().SuspicionKey, CandidateData.Suspicions[2]);
                Assert.AreEqual(2, group.Last().Perks.Count);
            }
        }

        [Test]
        public void PerkRules_HoldAcrossManySeeds()
        {
            var seenPerks = new HashSet<string>();
            var seenKeys = new HashSet<string>();
            var seenTiers = new HashSet<int>();
            int candidates = 0;
            for (uint seed = 1; seed <= 40; seed++)
            {
                var sim = New(seed);
                var b = sim.B;
                var founders = sim.Board.People.Select(p => p.Name).ToList();
                for (int g = 0; g < 6; g++)
                {
                    var group = NextGroup(sim);
                    Assert.That(group.Count, Is.InRange(2, 3));
                    Assert.Greater(group.Select(c => c.Tier).Distinct().Count(), 1, "grupta herkes aynı kademede olamaz");
                    foreach (var c in group)
                    {
                        candidates++;
                        var r = c.Resident;
                        string what = "tohum " + seed + " · " + Card(c);
                        seenTiers.Add(c.Tier);
                        seenKeys.Add(c.SuspicionKey);
                        foreach (string perk in c.Perks) seenPerks.Add(perk);

                        Assert.Contains(r.TrainingSubject, PlanningBoardData.CandidateSubjects, what);
                        Assert.Contains(c.SuspicionKey, CandidateData.Suspicions[c.Tier], what);
                        Assert.AreEqual(c.SuspicionKey == "kefil", c.SuspicionArg != null, what);
                        if (c.SuspicionArg != null) Assert.Contains(c.SuspicionArg, founders, what);
                        Assert.AreEqual(r.Id, c.Portrait, what);
                        Assert.AreEqual("rest", r.Job, what);
                        Assert.IsNull(sim.Board.Person(r.Id), "aday seçilene kadar panoda değildir");

                        // Kademe 0 tek özellik (getiri ya da bedel), kademe 1 tek getiri, kademe 2 iki ayrı getiri
                        Assert.AreEqual(c.Tier == 2 ? 2 : 1, c.Perks.Count, what);
                        Assert.AreEqual(c.Perks.Count, c.Perks.Distinct().Count(), what);
                        if (c.Tier > 0) Assert.IsFalse(c.Perks.Any(CandidateData.IsCost), what);
                        foreach (string perk in c.Perks) Assert.IsTrue(CandidateData.Benefits.Contains(perk) || CandidateData.Costs.Contains(perk), what);
                        if (c.Perks.Contains("kit")) Assert.Contains(r.TrainingSubject, PlanningBoardData.FieldJobs, what);

                        // Profil kişilik satırından okunur; bedel ve getiri gövde değerlerine uygulanmış gelir
                        int hp = b.boardBaseHp, move = b.boardBaseMove, power = b.boardBasePower;
                        if (r.Trait == "Dayanıklı, ağır adımlı") { hp++; move--; }
                        else if (r.Trait == "Çevik, narin") { hp--; move++; }
                        else if (r.Trait == "Güçlü, temkinli") { power++; move--; }
                        else Assert.AreEqual("Dengeli, saha deneyimi az", r.Trait, what);
                        if (c.Perks.Contains("yavas")) Assert.GreaterOrEqual(move, 3, "aksaklık yalnız hareketi 3 ve üstü profilde · " + what);
                        Assert.AreEqual(hp + (c.Perks.Contains("saglam") ? b.perkSturdyHp : 0), r.MaxHp, what);
                        Assert.AreEqual(move - (c.Perks.Contains("yavas") ? b.perkSlowMove : 0), r.Move, what);
                        Assert.GreaterOrEqual(r.Move, 2, what);
                        Assert.AreEqual(power, r.Power, what);
                        Assert.AreEqual(c.Perks.Contains("yarali") ? 1 : r.MaxHp, r.Hp, what);
                        Assert.AreEqual(c.Perks.Contains("yarali") ? b.perkWoundedShifts : 0, r.Down, what);
                        int skill = c.Perks.Contains("usta") ? b.boardExperiencedAt : c.Perks.Contains("acemi") ? 0 : b.boardTrainedAt;
                        Assert.AreEqual(skill, r.Skill(r.TrainingSubject), what);
                        Assert.AreEqual(c.Perks.Contains("usta") ? 2 : c.Perks.Contains("acemi") ? 0 : 1, sim.BoardSkillTier(r, r.TrainingSubject), what);
                        Assert.IsFalse(c.Perks.Contains("usta") && c.Perks.Contains("acemi"), what);
                    }
                    Assert.IsTrue(sim.PassCandidates().Ok);
                }
            }
            Assert.Greater(candidates, 400);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, seenTiers);
            CollectionAssert.AreEquivalent(CandidateData.Benefits.Concat(CandidateData.Costs), seenPerks, "tarama bütün özellikleri gördü");
            CollectionAssert.AreEquivalent(CandidateData.Suspicions.SelectMany(x => x), seenKeys, "tarama bütün şüphe satırlarını gördü");
        }

        // ---------------------------------------------------------------- adlar

        [Test]
        public void Names_NeverCollideWithFoundersOrEachOther_AndGetANumberAfterThePoolWraps()
        {
            foreach (uint seed in new uint[] { 79, 3, 4242 })
            {
                var sim = New(seed);
                var names = sim.Board.People.Select(p => p.Name).ToList();
                Assert.AreEqual(8, names.Count);
                int candidates = 0;
                while (candidates < 70)
                {
                    foreach (var c in NextGroup(sim))
                    {
                        candidates++;
                        string name = c.Resident.Name;
                        Assert.IsFalse(names.Contains(name), "ad çakışması: " + name + " (aday " + candidates + ", tohum " + seed + ")");
                        names.Add(name);
                        if (candidates <= CandidateData.Names.Length) Assert.Contains(name, CandidateData.Names, "havuz bir tur dönene kadar ad yalındır");
                        else StringAssert.EndsWith(" " + c.Resident.Id.Substring("resident-".Length), name, "havuz dönünce ada sakin numarası eklenir");
                    }
                    Assert.IsTrue(sim.PassCandidates().Ok);
                }
                Assert.AreEqual(candidates, sim.Admission.GeneratedApplicants);
                Assert.AreEqual(candidates, sim.Admission.Passed.Count);
            }
            // On kurucu adının hiçbiri havuzda yoktur; havuzda yinelenen ad yoktur
            foreach (string founder in new[] { "Derya", "Selin", "İpek", "Deniz", "Nermin", "Zeynep", "Orhan", "Cem", "Eren", "Suna" })
                Assert.IsFalse(CandidateData.Names.Contains(founder), founder);
            Assert.AreEqual(64, CandidateData.Names.Distinct().Count());
        }

        // ---------------------------------------------------------------- seçim, geri çevirme, süre aşımı

        [Test]
        public void Choose_AddsTheSamePersonIdle_WritesTheFile_AndSendsTheOthersAway()
        {
            var sim = New();
            var group = NextGroup(sim).ToList();
            var chosen = group[1];
            string id = chosen.Resident.Id;
            int people = sim.Board.People.Count, equipmentId = sim.Board.NextEquipmentId;
            Assert.IsNull(sim.CandidateChoiceCheck(id, null));
            Assert.IsNull(sim.CandidateChoiceCheck(id, ""), "boş dizgi de 'kimse yollanmıyor' demektir");

            var result = sim.ChooseCandidate(id);
            Assert.IsTrue(result.Ok, result.Msg);
            StringAssert.Contains(chosen.Resident.Name, result.Msg);
            Assert.AreSame(chosen.Resident, sim.Board.Person(id));
            Assert.AreEqual("rest", chosen.Resident.Job);
            Assert.IsFalse(chosen.Resident.Restricted);
            Assert.AreEqual(people + 1, sim.Board.People.Count);
            Assert.AreEqual(people + 1, sim.Shelter.Population);

            var file = sim.Admission.File(id);
            Assert.IsNotNull(file);
            Assert.AreSame(chosen.Resident, file.Resident);
            Assert.IsFalse(file.Founder);
            Assert.AreEqual(chosen.Tier, file.Tier);
            Assert.AreEqual(chosen.SuspicionKey, file.SuspicionKey);
            Assert.AreEqual(chosen.SuspicionArg, file.SuspicionArg);
            CollectionAssert.AreEqual(chosen.Perks, file.Perks);
            Assert.AreNotSame(chosen.Perks, file.Perks);
            Assert.AreEqual(chosen.ArrivalShift, file.ArrivalShift);
            Assert.AreEqual(sim.S.Day, file.AcceptedShift);
            Assert.IsFalse(file.Departed);
            Assert.IsNull(file.DepartReason);
            Assert.AreEqual(-1, file.DepartShift);
            Assert.AreEqual(chosen.Perks.Contains("devre") ? sim.B.perkCircuitShifts : 0, file.CircuitInstallments);
            Assert.AreEqual(chosen.Perks.Contains("erzak") ? sim.B.perkSupplyShifts : 0, file.SupplyInstallments);

            int gear = chosen.Perks.Count(p => p == "kit" || p == "zirh");
            Assert.AreEqual(gear, sim.Board.Equipment.Count(e => e.OwnerId == id && e.Bound));
            Assert.AreEqual(equipmentId + gear, sim.Board.NextEquipmentId, "teçhizat kimliği yalnız seçilen aday için tüketilir");

            Assert.AreEqual(0, sim.Admission.Group.Count);
            Assert.IsFalse(sim.Admission.GroupEmergency);
            CollectionAssert.AreEqual(group.Where(c => c != chosen).Select(c => c.Resident.Name + ":notchosen:" + c.Tier + ":" + sim.S.Day),
                sim.Admission.Passed.Select(p => p.Name + ":" + p.Kind + ":" + p.Tier + ":" + p.Shift));
            foreach (var other in group.Where(c => c != chosen))
            {
                Assert.IsNull(sim.Board.Person(other.Resident.Id));
                Assert.IsNull(sim.Admission.File(other.Resident.Id));
            }

            var again = sim.ChooseCandidate(id);
            Assert.IsFalse(again.Ok, "aynı aday ikinci kez alınamaz");
            Assert.AreEqual(Loc.T("Kapıda aday yok."), again.Msg);
            Assert.IsFalse(sim.ChooseCandidate(group[0].Resident.Id).Ok, "bir gruptan en çok bir kişi alınır");
            Assert.IsFalse(sim.Board.Locked);
            Assert.IsFalse(sim.Board.Printed);
            Close(sim);
            Assert.AreSame(chosen.Resident, sim.Board.Person(id));
        }

        [Test]
        public void Group_WaitsTwoShifts_ThenLeavesWithoutPenalty_AndTheSlipWarnsOnTheLastShift()
        {
            var sim = New();
            var quiet = New(79, Bal(100));
            Close(sim); Close(quiet);
            var group = sim.Admission.Group.ToList();
            Assert.Greater(group.Count, 0);
            Assert.AreEqual(1, sim.Admission.GroupArrivalShift);
            Assert.AreEqual(1 + sim.B.candidateWaitShifts, sim.Admission.GroupWaitingUntil);
            Assert.IsTrue(group.All(c => c.ArrivalShift == 1));

            string waiting = Loc.F("KAPI · {0} aday bekliyor.", group.Count), last = Loc.T("KAPI · son vardiya: karar verilmezse adaylar gider.");
            StringAssert.Contains(waiting, sim.BoardReceiptPreview);
            StringAssert.DoesNotContain(last, sim.BoardReceiptPreview);
            StringAssert.DoesNotContain("KAPI", quiet.BoardReceiptPreview);

            Close(sim); Close(quiet);
            CollectionAssert.AreEqual(group, sim.Admission.Group, "grup geldiği vardiya ve sonraki vardiya bekler");
            StringAssert.Contains(waiting, sim.BoardReceiptPreview);
            StringAssert.Contains(last, sim.BoardReceiptPreview);
            Assert.IsTrue(sim.PrintShift().Ok);
            StringAssert.Contains(last, sim.Board.ReceiptText, "basılan fiş son vardiya uyarısını taşır");
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            Close(quiet);

            CollectionAssert.AreEqual(group.Select(c => c.Resident.Name + ":expired:" + c.Tier + ":3"),
                sim.Admission.Passed.Select(p => p.Name + ":" + p.Kind + ":" + p.Tier + ":" + p.Shift));
            Assert.IsFalse(sim.Admission.Group.Any(group.Contains), "süresi dolan grup gider");
            Assert.AreEqual(2, sim.Admission.GeneratedGroups, "aynı kapanışta vadesi gelen yeni grup kapıya gelir");

            // Ceza yok: kapısı kapalı ikiz oyunla aynı erzak, fon, alarm, güven ve kadro
            Assert.AreEqual(quiet.Shelter.Supplies, sim.Shelter.Supplies);
            Assert.AreEqual(quiet.S.Fon, sim.S.Fon);
            Assert.AreEqual(quiet.S.Alarm, sim.S.Alarm);
            Assert.AreEqual(quiet.S.Trust, sim.S.Trust);
            Assert.AreEqual(quiet.Shelter.Population, sim.Shelter.Population);
            CollectionAssert.AreEqual(quiet.Board.People.Select(p => p.Id + p.Job + p.Hp), sim.Board.People.Select(p => p.Id + p.Job + p.Hp));
        }

        [Test]
        public void Pass_SendsTheWholeGroupAway_WithoutPenalty()
        {
            var sim = New();
            var group = NextGroup(sim).ToList();
            double supplies = sim.Shelter.Supplies, fon = sim.S.Fon;
            Assert.IsNull(sim.CandidatePassCheck());
            var result = sim.PassCandidates();
            Assert.IsTrue(result.Ok);
            Assert.AreEqual(Loc.T("Adaylar geri çevrildi."), result.Msg);
            Assert.AreEqual(0, sim.Admission.Group.Count);
            CollectionAssert.AreEqual(group.Select(c => c.Resident.Name + ":pass:" + c.Tier + ":" + sim.S.Day),
                sim.Admission.Passed.Select(p => p.Name + ":" + p.Kind + ":" + p.Tier + ":" + p.Shift));
            Assert.AreEqual(supplies, sim.Shelter.Supplies);
            Assert.AreEqual(fon, sim.S.Fon);
            Assert.AreEqual(8, sim.Board.People.Count);
            Assert.AreEqual(Loc.T("Kapıda aday yok."), sim.CandidatePassCheck());
            Assert.IsFalse(sim.PassCandidates().Ok);
            StringAssert.DoesNotContain("KAPI", sim.BoardReceiptPreview);
        }

        // ---------------------------------------------------------------- dolu sığınak

        static Balance Full(double supplies = 60)
        {
            var b = Bal();
            b.boardStartResidents = 10;
            b.boardStartSupplies = supplies;
            return b;
        }

        [Test]
        public void FullShelter_ChoosingNeedsSomeoneToLeave_WhoTakesTravelRations()
        {
            var sim = New(79, Full());
            Assert.AreEqual(sim.B.boardResidentCapacity, sim.Board.People.Count(p => p.Alive));
            var group = NextGroup(sim).ToList();
            string id = group[0].Resident.Id;
            int commands = sim.Record.commands.Count;

            string full = Loc.F("Sığınak dolu ({0}); aday için yollanacak kişiyi seç.", sim.B.boardResidentCapacity);
            Assert.AreEqual(full, sim.CandidateChoiceCheck(id, null));
            Assert.AreEqual(full, sim.CandidateChoiceCheck(id, ""));
            Assert.AreEqual(full, sim.ChooseCandidate(id).Msg);
            Assert.AreEqual(full, sim.ChooseCandidate(id, "").Msg);
            string living = Loc.T("Yollanacak canlı bir sakin seç.");
            Assert.AreEqual(living, sim.ChooseCandidate(id, "sakin-99").Msg);
            Assert.AreEqual(living, sim.ChooseCandidate(id, group[1].Resident.Id).Msg, "kapıdaki başka bir aday yollanamaz");
            Assert.AreEqual(Loc.T("Bu aday kapıda değil."), sim.ChooseCandidate("sakin-1", "sakin-2").Msg);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(group.Count, sim.Admission.Group.Count);

            // Kurucu sakin-1 Teknisyen kitini ve bir zırhı taşır: ikisi de kişiye bağlı değildir, depoya döner
            var leaver = sim.Board.Person("sakin-1");
            var gear = sim.Board.Equipment.Where(e => e.OwnerId == leaver.Id).ToList();
            Assert.AreEqual(2, gear.Count);
            double supplies = sim.Shelter.Supplies;
            Assert.AreEqual(sim.B.dismissSupplies, sim.DismissSuppliesPreview);
            var result = sim.ChooseCandidate(id, leaver.Id);
            Assert.IsTrue(result.Ok, result.Msg);
            StringAssert.Contains(group[0].Resident.Name, result.Msg);
            StringAssert.Contains(leaver.Name, result.Msg);
            Assert.AreEqual(supplies - sim.B.dismissSupplies, sim.Shelter.Supplies, 1e-9, "yolluk erzaktan düşer");
            Assert.IsNull(sim.Board.Person(leaver.Id));
            Assert.IsNotNull(sim.Board.Person(id));
            Assert.AreEqual(sim.B.boardResidentCapacity, sim.Board.People.Count(p => p.Alive));
            Assert.AreEqual(sim.B.boardResidentCapacity, sim.Shelter.Population);
            var file = sim.Admission.File(leaver.Id);
            Assert.IsTrue(file.Departed);
            Assert.AreEqual("yollandi", file.DepartReason);
            Assert.AreEqual(sim.S.Day, file.DepartShift);
            Assert.IsTrue(gear.All(e => sim.Board.Equipment.Contains(e) && e.OwnerId == null), "bağlı olmayan teçhizat depoya döner");
            var saved = sim.Record.commands.Last();
            Assert.AreEqual("candidate_choose", saved.kind);
            Assert.AreEqual(id, saved.a);
            Assert.AreEqual(leaver.Id, saved.b);

            // Ayrılan kişi bir daha yollanamaz
            var next = NextGroup(sim);
            Assert.AreEqual(living, sim.ChooseCandidate(next[0].Resident.Id, leaver.Id).Msg);
        }

        [Test]
        public void FullShelter_LowStock_TravelRationsAreWhatIsLeft()
        {
            var sim = New(79, Full(3));
            var group = NextGroup(sim);
            // Stok elle kurulur ki test tüketim dengesine bağlı kalmasın (aşama 8 denemesinde, tüketim 0,575 iken depo bu anda boştu)
            sim.Shelter.Supplies = 1.5;
            double supplies = sim.Shelter.Supplies;
            Assert.Greater(supplies, 0, "test düzeni: stok sıfır değil");
            Assert.Less(supplies, sim.B.dismissSupplies, "test düzeni: stok yolluktan az");
            Assert.AreEqual(supplies, sim.DismissSuppliesPreview);
            Assert.IsTrue(sim.ChooseCandidate(group[0].Resident.Id, "sakin-8").Ok);
            Assert.AreEqual(0, sim.Shelter.Supplies);
            Assert.AreEqual(0, sim.DismissSuppliesPreview);
        }

        [Test]
        public void RoomAvailable_NobodyCanBeSentAway()
        {
            var sim = New();
            var group = NextGroup(sim);
            int commands = sim.Record.commands.Count;
            var refused = sim.ChooseCandidate(group[0].Resident.Id, "sakin-1");
            Assert.IsFalse(refused.Ok);
            Assert.AreEqual(Loc.T("Boş yer varken kimse yollanamaz."), refused.Msg);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.IsNotNull(sim.Board.Person("sakin-1"));
            Assert.AreEqual(group.Count, sim.Admission.Group.Count);
        }

        // ---------------------------------------------------------------- kişiye bağlı teçhizat

        /// <summary>Dokuz kurucu, depo dolu, ilk grup acil (canlı 9 &lt; 10): her aday kendi rol kitiyle gelir.</summary>
        static Balance BoundKits()
        {
            var b = Bal();
            b.boardStartResidents = 9;
            b.candidateEmergencyBelow = 10;
            b.boardEquipmentCapacity = 6;
            b.candidateTier0CostChance = 0;
            return b;
        }

        [Test]
        public void BoundGear_IsGivenOverAFullDepot_AndCannotBeMovedSwappedOrScrapped()
        {
            var sim = New(79, BoundKits());
            Assert.AreEqual(sim.B.boardEquipmentCapacity, sim.Board.Equipment.Count, "test düzeni: depo tavanı dolu");
            var group = NextGroup(sim);
            Assert.IsTrue(sim.Admission.GroupEmergency);
            var chosen = group.First(c => !c.Perks.Contains("zirh"));
            string id = chosen.Resident.Id, role = chosen.Resident.TrainingSubject;
            Assert.Contains("kit", chosen.Perks);
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);

            var kit = sim.Board.Equipment.Single(e => e.OwnerId == id);
            Assert.IsTrue(kit.Bound);
            Assert.AreEqual("kit_" + role, kit.Kind);
            Assert.AreEqual(sim.B.boardEquipmentCapacity + 1, sim.Board.Equipment.Count, "kişiye bağlı teçhizat depo tavanını aşabilir");
            Assert.IsTrue(sim.Board.Equipment.Where(e => e != kit).All(e => !e.Bound), "kurucu teçhizatı bağlı değildir");

            int commands = sim.Record.commands.Count;
            string fixedGear = Loc.T("Bu teçhizat sahibine bağlı; devredilemez.");
            Assert.AreEqual(fixedGear, sim.EquipResident(kit.Id, null).Msg, "depoya konamaz");
            Assert.AreEqual(fixedGear, sim.EquipResident(kit.Id, "sakin-5").Msg, "başkasına verilemez");
            Assert.AreEqual(Loc.T("Bu teçhizat sahibine bağlı; sökülemez."), sim.ScrapEquipment(kit.Id).Msg);
            var founderKit = sim.Board.Equipment.First(e => !e.Bound && e.Kind.StartsWith("kit_"));
            string founderOwner = founderKit.OwnerId;
            Assert.AreEqual(Loc.T("Bu cebi kişinin kendi teçhizatı tutuyor."), sim.EquipResident(founderKit.Id, id).Msg, "takas bağlı kiti cebinden çıkaramaz");
            Assert.AreEqual(founderOwner, founderKit.OwnerId);
            Assert.AreEqual(id, kit.OwnerId);
            Assert.AreEqual(commands, sim.Record.commands.Count);

            // Başka cep serbest: bağlı olmayan zırh takılabilir ve geri alınabilir
            var armor = sim.Board.Equipment.First(e => e.Kind == "armor");
            Assert.IsTrue(sim.EquipResident(armor.Id, id).Ok);
            Assert.AreEqual(id, armor.OwnerId);
            // (Olay kısıtında bağlı olmayan depoya döner, bağlı eşya kişide kalır: ShelterEventCaseTests.Restricted_OnlyTrainingAndIdle_…)
        }

        [Test]
        public void BoundGear_LeavesWithItsOwner_UnboundGearReturnsToTheDepot()
        {
            var sim = New(79, BoundKits());
            var chosen = NextGroup(sim).First(c => !c.Perks.Contains("zirh"));
            string id = chosen.Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            var kit = sim.Board.Equipment.Single(e => e.OwnerId == id);
            var armor = sim.Board.Equipment.First(e => e.Kind == "armor");
            Assert.IsTrue(sim.EquipResident(armor.Id, id).Ok);
            Assert.AreEqual(sim.B.boardResidentCapacity, sim.Board.People.Count(p => p.Alive));

            var next = NextGroup(sim);
            Assert.IsFalse(sim.Admission.GroupEmergency, "sığınak doluyken gelen grup olağandır");
            Assert.IsTrue(sim.ChooseCandidate(next[0].Resident.Id, id).Ok);
            Assert.IsFalse(sim.Board.Equipment.Contains(kit), "bağlı kit sahibiyle gider");
            Assert.IsFalse(sim.Board.Equipment.Any(e => e.OwnerId == id));
            Assert.IsTrue(sim.Board.Equipment.Contains(armor));
            Assert.IsNull(armor.OwnerId, "bağlı olmayan zırh depoya döner");
            Assert.IsTrue(sim.Admission.File(id).Departed);
        }

        [Test]
        public void BoundGear_IsLostWhenItsOwnerDies()
        {
            var sim = New(79, BoundKits());
            var chosen = NextGroup(sim).First(c => c.Resident.TrainingSubject == "tek");
            string id = chosen.Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            Assert.IsTrue(sim.Board.Equipment.Any(e => e.OwnerId == id && e.Bound && e.Kind == "kit_tek"));
            Assert.IsTrue(sim.AssignResident(id, "tek").Ok, "kendi kitiyle gelen kişi sahaya çıkabilir");
            Assert.IsNull(sim.PlanningRaidCheck(), sim.PlanningRaidCheck());
            var squad = sim.PlanningSquad();
            Assert.AreEqual(id, squad[0].PersonId);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Retreat = true, Hp = { 0, squad[1].Hp, squad[2].Hp }, Armor = { 0, squad[1].Armor, squad[2].Armor }, Left = { 0 } }).Ok);
            Assert.IsFalse(sim.Board.Person(id).Alive);
            Assert.IsFalse(sim.Board.Equipment.Any(e => e.OwnerId == id), "ölen kişinin bağlı teçhizatı da yok olur");
            Assert.IsFalse(sim.Board.Equipment.Any(e => e.Bound));
        }

        // ---------------------------------------------------------------- taksitler ve obur

        /// <summary>Erzak dengesi ustalıkla kaymasın ve ikmalci hurda devresi getirmesin: taksitler tek başına ölçülür.</summary>
        static Balance Flat(int residents = 8)
        {
            var b = Bal();
            b.boardStartResidents = residents;
            b.boardCookSavingPerTier = 0;
            b.boardGatherSuppliesPerTier = 0;
            b.boardGatherCircuits = 0;
            b.boardGatherMetal = 0;
            return b;
        }

        static bool Only(CommunityApplicant c, string perk) => c.Perks.Count == 1 && c.Perks[0] == perk;

        [Test]
        public void CircuitInstallments_PayTheTotalOverTheClosings_ThenStop()
        {
            CommunityApplicant c;
            var sim = Find(x => Only(x, "devre"), out c, () => Flat());
            string id = c.Resident.Id;
            int total = sim.B.perkCircuits, shifts = sim.B.perkCircuitShifts;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            var file = sim.Admission.File(id);
            Assert.AreEqual(shifts, file.CircuitInstallments);
            int start = sim.Board.Material("devre").Quantity;
            for (int i = 1; i <= shifts; i++)
            {
                if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
                Close(sim);
                Assert.AreEqual(shifts - i, file.CircuitInstallments);
                Assert.AreEqual(start + total * i / shifts, sim.Board.Material("devre").Quantity, "kapanış " + i);
            }
            Assert.AreEqual(start + total, sim.Board.Material("devre").Quantity, "toplam tam ödenir");
            Close(sim);
            Assert.AreEqual(start + total, sim.Board.Material("devre").Quantity, "taksit bitince devre gelmez");
            Assert.AreEqual(0, file.CircuitInstallments);
        }

        [Test]
        public void SupplyInstallments_MatchTheSlipPreview_AndPayTheTotal()
        {
            CommunityApplicant c;
            var sim = Find(x => Only(x, "erzak"), out c, () => Flat());
            string id = c.Resident.Id;
            double total = sim.B.perkSupplies;
            int shifts = sim.B.perkSupplyShifts;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            var file = sim.Admission.File(id);
            Assert.AreEqual(shifts, file.SupplyInstallments);
            var changes = new List<double>();
            for (int i = 0; i <= shifts; i++)
            {
                if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
                double preview = sim.BoardFoodPreview, before = sim.Shelter.Supplies;
                Assert.IsTrue(sim.PrintShift().Ok);
                StringAssert.Contains(Loc.F("Beklenen erzak değişimi: {0:0.##}", preview), sim.Board.ReceiptText);
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
                Assert.AreEqual(preview, sim.Shelter.Supplies - before, 1e-9, "fiş ön izlemesi kapanışla aynı (kapanış " + (i + 1) + ")");
                Assert.AreEqual(Math.Max(0, shifts - i - 1), file.SupplyInstallments);
                changes.Add(sim.Shelter.Supplies - before);
            }
            // Son kapanış taksitsizdir; kadro ve işler aynı kaldığı için fark yalnız taksittir
            double plain = changes[shifts];
            for (int i = 0; i < shifts; i++) Assert.AreEqual(total / shifts, changes[i] - plain, 1e-9, "kapanış " + (i + 1));
            Assert.AreEqual(total, changes.Take(shifts).Sum() - plain * shifts, 1e-9);
        }

        [Test]
        public void Installments_StopWhenThePersonLeaves()
        {
            CommunityApplicant c;
            var sim = Find(x => Only(x, "devre"), out c, () => Flat(9));
            string id = c.Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            var file = sim.Admission.File(id);
            int start = sim.Board.Material("devre").Quantity;
            Close(sim);
            Assert.AreEqual(sim.B.perkCircuitShifts - 1, file.CircuitInstallments);
            int paid = sim.Board.Material("devre").Quantity;
            Assert.Greater(paid, start);
            Assert.Less(paid, start + sim.B.perkCircuits, "test düzeni: taksit sürüyor");

            var next = NextGroup(sim).First(x => !x.Perks.Contains("devre"));
            Assert.IsTrue(sim.ChooseCandidate(next.Resident.Id, id).Ok, "dolu sığınakta taksiti süren kişi yollanır");
            Assert.AreEqual(0, file.CircuitInstallments, "kişi çıkınca kalan taksit kesilir");
            Assert.AreEqual(0, file.SupplyInstallments);
            Close(sim);
            if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
            Close(sim);
            Assert.AreEqual(paid, sim.Board.Material("devre").Quantity);
        }

        /// <summary>Biçim 15'in erzak dengesi, harfiyen: obur ve erzak taksiti yokken yeni denge bununla bit düzeyinde aynıdır.</summary>
        static double PlainFoodBalance(Simulation sim)
        {
            var b = sim.B;
            double consumption = sim.Board.People.Count(p => p.Alive) * Math.Max(0, b.boardSuppliesPerResident);
            var cook = sim.Board.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == "cook");
            if (cook != null) consumption -= Math.Min(consumption * b.boardCookingMaxSavingFraction, b.boardCookSaving + sim.BoardSkillTier(cook, "cook") * b.boardCookSavingPerTier);
            var gather = sim.Board.People.FirstOrDefault(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == "gather");
            double gain = gather == null ? 0 : b.boardGatherSupplies + sim.BoardSkillTier(gather, "gather") * b.boardGatherSuppliesPerTier;
            return gain - Math.Max(0, consumption);
        }

        /// <summary>Fişteki erzak ön izlemesinin biçim 15 hâli (depo sınırlarıyla).</summary>
        static double PlainFoodPreview(Simulation sim)
            => Math.Min(Math.Max(0, sim.B.boardSuppliesCapacity), Math.Max(0, sim.Shelter.Supplies + PlainFoodBalance(sim))) - sim.Shelter.Supplies;

        [Test]
        public void Glutton_EatsTheRationFactor_AndWithoutOneTheBalanceIsBitIdentical()
        {
            // Obur yok: varsayılan dengede altı kapanış boyunca eski formülle birebir
            var plain = New(79, Bal(100));
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(PlainFoodPreview(plain), plain.BoardFoodPreview, "kapanış " + i);
                Close(plain);
            }

            CommunityApplicant c;
            var sim = Find(x => Only(x, "obur"), out c);
            double per = sim.B.boardSuppliesPerResident, before = sim.BoardFoodPreview;
            Assert.AreEqual(PlainFoodPreview(sim), before);
            Assert.IsTrue(sim.ChooseCandidate(c.Resident.Id).Ok);
            Assert.AreEqual(before - per * sim.B.perkDoubleRationFactor, sim.BoardFoodPreview, 1e-9, "obur kişi tayının katını yer");
            Assert.AreEqual(PlainFoodPreview(sim) - per * (sim.B.perkDoubleRationFactor - 1), sim.BoardFoodPreview, 1e-9);
            double supplies = sim.Shelter.Supplies, preview = sim.BoardFoodPreview;
            Close(sim);
            Assert.AreEqual(preview, sim.Shelter.Supplies - supplies, 1e-9);

            // Sıradan (bedelsiz, erzaksız) bir kişi yalnız bir tayın yer
            var other = Find(x => !x.Perks.Contains("obur") && !x.Perks.Contains("erzak"), out c);
            before = other.BoardFoodPreview;
            Assert.IsTrue(other.ChooseCandidate(c.Resident.Id).Ok);
            Assert.AreEqual(before - per, other.BoardFoodPreview, 1e-9);
            Assert.AreEqual(PlainFoodPreview(other), other.BoardFoodPreview);
        }

        // ---------------------------------------------------------------- kilit kuralı

        [Test]
        public void LockedPlan_StillAllowsTheDoor_ButNotDuringARaidOrAfterTheSlip()
        {
            var sim = New();
            var group = NextGroup(sim).ToList();
            string id = group[0].Resident.Id;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            int commands = sim.Record.commands.Count;
            string raid = Loc.T("Baskın sürüyor.");
            Assert.AreEqual(raid, sim.CandidateChoiceCheck(id, null));
            Assert.AreEqual(raid, sim.CandidatePassCheck());
            Assert.AreEqual(raid, sim.ChooseCandidate(id).Msg);
            Assert.AreEqual(raid, sim.PassCandidates().Msg);
            Assert.AreEqual(commands, sim.Record.commands.Count);

            RetreatUnharmed(sim);
            Assert.IsTrue(sim.Board.Locked, "baskından dönüşte plan kilitlidir");
            Assert.IsFalse(sim.Board.Printed);
            Assert.IsFalse(sim.AssignResident(sim.Board.People.First(p => p.Job == "rest").Id, "contact").Ok, "pano komutları kilitli");
            Assert.IsNull(sim.CandidateChoiceCheck(id, null), "plan kilidi kapıyı kapatmaz");
            Assert.IsNull(sim.CandidatePassCheck());
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            Assert.IsTrue(sim.Board.Locked, "seçim plan kilidine dokunmaz");
            Assert.IsFalse(sim.Board.Printed);
            Assert.AreEqual("rest", sim.Board.Person(id).Job);
            Close(sim);
            Assert.IsFalse(sim.Board.Locked);
            Assert.IsNotNull(sim.Board.Person(id));

            // Kilitli planda geri çevirme
            var other = New();
            NextGroup(other);
            Assert.IsTrue(other.StartLiberationRaid("weu_1").Ok);
            RetreatUnharmed(other);
            Assert.IsTrue(other.PassCandidates().Ok);
            Assert.IsTrue(other.Board.Locked);
            Assert.AreEqual(0, other.Admission.Group.Count);

            // Fiş basıldıktan sonra kapı o vardiya kapalıdır
            var printed = New();
            var waiting = NextGroup(printed).ToList();
            Assert.IsTrue(printed.PrintShift().Ok);
            StringAssert.Contains(Loc.F("KAPI · {0} aday bekliyor.", waiting.Count), printed.Board.ReceiptText);
            commands = printed.Record.commands.Count;
            string closed = Loc.T("Fiş basıldı; kapı bu vardiya kapalı.");
            Assert.AreEqual(closed, printed.ChooseCandidate(waiting[0].Resident.Id).Msg);
            Assert.AreEqual(closed, printed.PassCandidates().Msg);
            Assert.AreEqual(commands, printed.Record.commands.Count);
            Assert.IsTrue(printed.FileShift(printed.Board.ReceiptId).Ok);
            CollectionAssert.AreEqual(waiting, printed.Admission.Group, "grup ertesi vardiyada hâlâ kapıda");
            Assert.IsTrue(printed.ChooseCandidate(waiting[0].Resident.Id).Ok);
        }

        /// <summary>
        /// Kilitlenme (softlock) koruması: açık iş emri varken depo tavanın bir altında; baskından dönülmüş (plan kilitli) ve kapıdan
        /// kişiye bağlı teçhizatlı aday alınır. Bağlı eşya depo tavanına sayılmaz; fiş basılır, üretim yürür. Sayılsaydı fiş "depo
        /// dolu" diye reddedilir, kilitli planda emir de eşya da kaldırılamazdı.
        /// </summary>
        [Test]
        public void LockedPlan_BoundGearOverTheDepotCap_NeverBlocksTheSlipOrProduction()
        {
            Simulation sim = null;
            CommunityApplicant c = null;
            for (uint seed = 1; seed <= 200 && c == null; seed++)
            {
                var b = Bal();
                b.candidateTier0CostChance = 0;
                sim = New(seed, b);
                Close(sim);
                c = sim.Admission.Group.FirstOrDefault(x => x.Perks.Contains("zirh") || x.Perks.Contains("kit"));
            }
            Assert.IsNotNull(c, "test düzeni: bağlı teçhizatlı aday bulunamadı");
            int bound = c.Perks.Count(x => x == "zirh" || x == "kit");
            int cap = sim.B.boardEquipmentCapacity;
            while (sim.Board.Equipment.Count < cap - 1) Assert.IsNotNull(sim.AddBoardEquipment("medkit"));
            Assert.IsTrue(sim.QueueProduction("armor").Ok);
            foreach (var input in sim.BoardRecipes.First(r => r.Id == "armor").Inputs)
            {
                sim.AddBoardMaterial(input.Kind, input.Quantity);
                Assert.IsTrue(sim.ReserveProductionMaterial(input.Kind, input.Quantity).Ok);
            }
            Assert.IsNull(sim.BoardPlanCheck());
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            RetreatUnharmed(sim);
            Assert.IsTrue(sim.Board.Locked);

            Assert.IsTrue(sim.ChooseCandidate(c.Resident.Id).Ok);
            Assert.AreEqual(cap - 1 + bound, sim.Board.Equipment.Count);
            Assert.GreaterOrEqual(sim.Board.Equipment.Count, cap, "test düzeni: bağlı eşyayla toplam tavana ulaştı");
            Assert.AreEqual(cap - 1, sim.BoardDepotCount, "bağlı eşya depoda yer tutmaz");
            Assert.AreEqual(Loc.T("Plan kilitli. Fişi devir kutusuna bırak."), sim.CancelProduction(sim.Board.Orders[0].Id).Msg, "kilitli planda başka çıkış yok");
            Assert.IsNull(sim.BoardPlanCheck(), "bağlı eşya fişi kilitlemez");
            Close(sim);
            Assert.IsFalse(sim.Board.Locked);
            Assert.IsTrue(sim.Board.Orders.Count == 0 || sim.Board.Orders[0].Started, "üretim durmadı");

            // Depo gerçekten dolunca (bağlı olmayan eşya tavanda) eski kural sürer
            while (sim.BoardDepotCount < cap) Assert.IsNotNull(sim.AddBoardEquipment("medkit"));
            Assert.IsNull(sim.AddBoardEquipment("medkit"), "dolu depoya bağsız eşya girmez");
            if (sim.Board.Orders.Count > 0) Assert.AreEqual(Loc.T("Teçhizat deposu dolu. Üretimden önce yer aç."), sim.BoardPlanCheck());
        }

        // ---------------------------------------------------------------- acil grup

        static Balance Four(int knock)
        {
            var b = Bal(knock);
            b.boardStartResidents = 4;
            return b;
        }

        [Test]
        public void EmergencyGroup_ArrivesWhenDeathsLeaveFewerThanThree_WithThreeMissingRolesAndKits()
        {
            var sim = New(79, Four(100));
            Close(sim);
            Assert.AreEqual(0, sim.Admission.Group.Count, "dört canlı: acil grup yok");
            int nextArrival = sim.Admission.NextArrivalShift;
            LoseWholeSquad(sim);
            Assert.AreEqual(1, sim.Board.People.Count(p => p.Alive));
            Assert.IsNull(sim.S.Ending);
            Assert.IsTrue(PlanningBoardData.FieldJobs.All(r => sim.BoardKitMissing("kit_" + r)));
            Assert.AreEqual(0, sim.Admission.Group.Count, "acil grup kapanışta gelir");

            Assert.IsTrue(sim.PrintShift().Ok);
            uint before = sim.S.Rng.State;
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            Assert.AreEqual(After(before, DrawsPerGroup), sim.S.Rng.State, "acil grup da tam 32 çekim yapar");
            var group = sim.Admission.Group.ToList();
            Assert.IsTrue(sim.Admission.GroupEmergency);
            Assert.AreEqual(3, group.Count);
            CollectionAssert.AreEqual(PlanningBoardData.FieldJobs, group.Select(c => c.Resident.TrainingSubject), "üç eksik rol, rol sırasıyla");
            Assert.IsTrue(group.All(c => c.Perks.Contains("kit")), "her acil aday rol kitiyle gelir");
            Assert.IsTrue(group.All(c => c.Perks.Count == c.Perks.Distinct().Count()));
            Assert.IsTrue(group.All(c => c.Perks.Count == (c.Tier == 2 ? 3 : 2)), "kit, çekilen özelliklere ektir");
            Assert.AreEqual(nextArrival, sim.Admission.NextArrivalShift, "acil geliş olağan aralık sayacına dokunmaz");
            Assert.AreEqual(sim.S.Day + sim.B.candidateWaitShifts, sim.Admission.GroupWaitingUntil);

            // Biri alınır: kiti kişiye bağlı gelir; canlı 2 olduğu için sonraki kapanışta yeni acil grup
            var first = group[1];
            Assert.IsTrue(sim.ChooseCandidate(first.Resident.Id).Ok);
            Assert.IsFalse(sim.Admission.GroupEmergency);
            var kit = sim.Board.Equipment.Single(e => e.OwnerId == first.Resident.Id && e.Kind == "kit_emp");
            Assert.IsTrue(kit.Bound);
            Assert.IsFalse(sim.BoardKitMissing("kit_emp"));
            Assert.AreEqual(2, sim.Board.People.Count(p => p.Alive));
            Close(sim);
            var second = sim.Admission.Group.ToList();
            Assert.IsTrue(sim.Admission.GroupEmergency);
            Assert.AreEqual(3, second.Count);
            CollectionAssert.AreEqual(new[] { "tek", "ana", "emp" }, second.Select(c => c.Resident.TrainingSubject), "kiti hiç bulunmayan roller önce");
            Assert.IsTrue(second.All(c => c.Perks.Contains("kit")));
            Assert.AreEqual(nextArrival, sim.Admission.NextArrivalShift);

            // Canlı üç olunca acil grup gelmez
            Assert.IsTrue(sim.ChooseCandidate(second[0].Resident.Id).Ok);
            Assert.AreEqual(3, sim.Board.People.Count(p => p.Alive));
            int groups = sim.Admission.GeneratedGroups;
            for (int i = 0; i < 3; i++) Close(sim);
            Assert.AreEqual(groups, sim.Admission.GeneratedGroups);
            Assert.AreEqual(0, sim.Admission.Group.Count);
        }

        /// <summary>
        /// Acil adayın kiti çekilen özelliğinin yerine geçmez: özellik çekimi <c>kit</c> çıkarsa RNG'siz devreye (o da varsa erzağa)
        /// çevrilir, sonra bağlı kit eklenir. Çekim sayısı değişmez.
        /// </summary>
        [Test]
        public void EmergencyGroup_KitIsAlwaysOnTopOfTheDrawnPerks()
        {
            int candidates = 0, twoBenefits = 0;
            for (uint seed = 1; seed <= 60; seed++)
            {
                var sim = New(seed, BoundKits());
                Assert.IsTrue(sim.PrintShift().Ok);
                uint before = sim.S.Rng.State;
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
                Assert.AreEqual(After(before, DrawsPerGroup), sim.S.Rng.State, "tohum " + seed);
                Assert.IsTrue(sim.Admission.GroupEmergency, "tohum " + seed);
                foreach (var c in sim.Admission.Group)
                {
                    string what = "tohum " + seed + " " + Card(c);
                    Assert.AreEqual(1, c.Perks.Count(x => x == "kit"), what);
                    Assert.AreEqual("kit", c.Perks[c.Perks.Count - 1], what);
                    Assert.AreEqual(c.Tier == 2 ? 3 : 2, c.Perks.Count, what);
                    Assert.AreEqual(c.Perks.Count, c.Perks.Distinct().Count(), what);
                    Assert.Contains(c.Resident.TrainingSubject, PlanningBoardData.FieldJobs, what);
                    candidates++;
                    if (c.Tier == 2 && c.Perks.Contains("devre") && c.Perks.Contains("erzak")) twoBenefits++;
                }
            }
            Assert.AreEqual(180, candidates);
            Assert.Greater(twoBenefits, 0, "test düzeni: devre + erzak + kit taşıyan şüpheli aday görüldü");
        }

        [Test]
        public void EmergencyGroup_WaitsForTheDoorToEmpty()
        {
            var sim = New(79, Four(1));
            Close(sim);
            var ordinary = sim.Admission.Group.ToList();
            Assert.Greater(ordinary.Count, 0);
            Assert.IsFalse(sim.Admission.GroupEmergency);
            LoseWholeSquad(sim);
            Assert.AreEqual(1, sim.Board.People.Count(p => p.Alive));
            Close(sim);
            CollectionAssert.AreEqual(ordinary, sim.Admission.Group, "olağan grup kapıdayken acil grup üretilmez");
            Assert.IsFalse(sim.Admission.GroupEmergency);
            Assert.AreEqual(1, sim.Admission.GeneratedGroups);
            Close(sim);
            Assert.IsTrue(sim.Admission.GroupEmergency, "olağan grubun süresi dolunca aynı kapanışta acil grup gelir");
            Assert.AreEqual(3, sim.Admission.Group.Count);
            Assert.AreEqual(2, sim.Admission.GeneratedGroups);
            Assert.IsTrue(sim.Admission.Passed.All(p => p.Kind == "expired"));
        }

        [Test]
        public void EmergencyThresholdZero_NeverCallsAnEmergencyGroup()
        {
            var b = Four(100);
            b.candidateEmergencyBelow = 0;
            var sim = New(79, b);
            LoseWholeSquad(sim);
            uint rng = sim.S.Rng.State;
            for (int i = 0; i < 3; i++) Close(sim);
            Assert.AreEqual(0, sim.Admission.GeneratedGroups);
            Assert.AreEqual(rng, sim.S.Rng.State);
        }

        // ---------------------------------------------------------------- kit kilidi

        static Balance Starved()
        {
            var b = Bal(100);
            b.boardStartSupplies = 0;
            return b;
        }

        static string Inputs(Simulation sim, string recipe) =>
            string.Join("+", sim.BoardRecipes.Single(r => r.Id == recipe).Inputs.Select(i => i.Kind + ":" + i.Quantity));

        static void Queue(Simulation sim, string recipeId)
        {
            Assert.IsTrue(sim.QueueProduction(recipeId).Ok);
            foreach (var input in sim.BoardRecipes.Single(r => r.Id == recipeId).Inputs)
                Assert.IsTrue(sim.ReserveProductionMaterial(input.Kind, input.Quantity).Ok, input.Kind);
        }

        static void ScrapKit(Simulation sim, string kind) => Assert.IsTrue(sim.ScrapEquipment(sim.Board.Equipment.Single(e => e.Kind == kind).Id).Ok);

        [Test]
        public void KitLock_MissingRoleKitNeedsNoCircuit_UntilItExistsAgain()
        {
            var sim = New(79, Bal(100));
            string full = "metal:" + sim.B.boardKitMetal + "+devre:" + sim.B.boardKitCircuits;
            foreach (string role in PlanningBoardData.FieldJobs)
            {
                Assert.IsFalse(sim.BoardKitMissing("kit_" + role));
                Assert.AreEqual(full, Inputs(sim, "kit_" + role));
            }
            Assert.IsFalse(sim.BoardKitMissing("armor"), "yalnız rol kitleri sayılır");
            Assert.IsFalse(sim.BoardKitMissing(null));

            ScrapKit(sim, "kit_tek");
            Assert.IsTrue(sim.BoardKitMissing("kit_tek"));
            Assert.AreEqual("metal:" + sim.B.boardKitMetal, Inputs(sim, "kit_tek"), "eksik rol kiti devre istemez");
            Assert.AreEqual(full, Inputs(sim, "kit_emp"), "eksik olmayan kit yine devre ister");
            Assert.IsFalse(sim.ReserveProductionMaterial("devre", 1).Ok);

            int circuits = sim.Board.Material("devre").Quantity, metal = sim.Board.Material("metal").Quantity;
            Queue(sim, "kit_tek");
            Close(sim);
            Assert.IsFalse(sim.BoardKitMissing("kit_tek"));
            Assert.AreEqual(1, sim.Board.Equipment.Count(e => e.Kind == "kit_tek"));
            Assert.AreEqual(circuits, sim.Board.Material("devre").Quantity, "devre harcanmadı");
            Assert.AreEqual(metal - sim.B.boardKitMetal, sim.Board.Material("metal").Quantity);
            Assert.AreEqual(full, Inputs(sim, "kit_tek"), "kit var olunca tarif yine devre ister");
        }

        [Test]
        public void KitLock_BoundKitCountsAsPresent()
        {
            var sim = New(79, BoundKits());
            var chosen = NextGroup(sim).First(c => c.Resident.TrainingSubject == "tek");
            ScrapKit(sim, "kit_tek");
            Assert.IsTrue(sim.BoardKitMissing("kit_tek"));
            Assert.IsTrue(sim.ChooseCandidate(chosen.Resident.Id).Ok);
            Assert.IsFalse(sim.BoardKitMissing("kit_tek"), "kişiye bağlı kit de sayılır");
            Assert.AreEqual(2, sim.BoardRecipes.Single(r => r.Id == "kit_tek").Inputs.Count);
        }

        [Test]
        public void KitLock_StarvingClosing_StillBuildsAMissingRoleKit_ButNothingElse()
        {
            // Eksik rol kiti aç kapanışta üretilir
            var sim = New(79, Starved());
            ScrapKit(sim, "kit_tek");
            Queue(sim, "kit_tek");
            Close(sim);
            Assert.IsTrue(sim.Shelter.Starving, "test düzeni: kapanış aç");
            Assert.AreEqual(1, sim.Board.Equipment.Count(e => e.Kind == "kit_tek"));
            CollectionAssert.Contains(sim.Board.LastCompleted, "kit_tek");
            Assert.AreEqual(0, sim.Board.Orders.Count);

            // Eksik olmayan kit ve zırh aç kapanışta üretilmez; girdiler harcanmaz
            foreach (string recipe in new[] { "kit_emp", "armor" })
            {
                var other = New(79, Starved());
                int count = other.Board.Equipment.Count;
                string materials = string.Join(",", other.Board.Materials.Select(m => m.Kind + m.Quantity));
                Queue(other, recipe);
                Close(other);
                Assert.IsTrue(other.Shelter.Starving);
                Assert.AreEqual(count, other.Board.Equipment.Count, recipe);
                Assert.AreEqual(1, other.Board.Orders.Count, recipe);
                Assert.IsFalse(other.Board.Orders[0].Started, recipe);
                Assert.AreEqual(materials, string.Join(",", other.Board.Materials.Select(m => m.Kind + m.Quantity)), recipe);
            }

            // Otomatik üretim: tek metal, eksik kiti aç kapanışta da tetikler
            var auto = New(79, Starved());
            ScrapKit(auto, "kit_ana");
            Assert.IsTrue(auto.EnableAutomaticProduction().Ok);
            Assert.IsTrue(auto.StageProductionMaterial("metal", auto.B.boardKitMetal).Ok);
            Assert.AreEqual("kit_ana", auto.AutomaticProductionRecipe.Id);
            Close(auto);
            Assert.IsTrue(auto.Shelter.Starving);
            Assert.AreEqual(1, auto.Board.Equipment.Count(e => e.Kind == "kit_ana"));
        }

        [Test]
        public void KitLock_BalanceSwitches_RestoreTheOldRules()
        {
            var circuits = Bal(100);
            circuits.boardMissingKitCircuits = 1;
            var sim = New(79, circuits);
            ScrapKit(sim, "kit_tek");
            Assert.IsTrue(sim.BoardKitMissing("kit_tek"));
            Assert.AreEqual("metal:" + sim.B.boardKitMetal + "+devre:1", Inputs(sim, "kit_tek"), "boardMissingKitCircuits = 1: eksik kit de devre ister");

            var strict = Starved();
            strict.boardStarvingKitProduction = false;
            var hungry = New(79, strict);
            ScrapKit(hungry, "kit_tek");
            Queue(hungry, "kit_tek");
            Close(hungry);
            Assert.IsTrue(hungry.Shelter.Starving);
            Assert.AreEqual(0, hungry.Board.Equipment.Count(e => e.Kind == "kit_tek"), "boardStarvingKitProduction = false: aç kapanışta üretim yok");
            Assert.IsFalse(hungry.Board.Orders[0].Started);
        }

        // ---------------------------------------------------------------- kayıt ve yeniden oynatma

        /// <summary>Seçim, yer açarak seçim, geri çevirme, süre aşımı ve kilitli planda seçim içeren oyun.</summary>
        static Simulation Story(uint seed)
        {
            var b = Bal();
            b.boardStartResidents = 9;
            b.candidateTier0CostChance = 0;
            var sim = New(seed, b);
            var group = NextGroup(sim);
            var first = group[0].Resident;
            Assert.IsTrue(sim.ChooseCandidate(first.Id).Ok);                      // seçim (b = null)
            Assert.IsTrue(sim.AssignResident(first.Id, "gather").Ok);
            group = NextGroup(sim);
            Assert.IsTrue(sim.ChooseCandidate(group[group.Count - 1].Resident.Id, "sakin-9").Ok);   // yer açarak seçim
            group = NextGroup(sim);
            Assert.IsTrue(sim.PassCandidates().Ok);                                // geri çevirme
            NextGroup(sim);
            Close(sim); Close(sim);                                               // süre aşımı
            Assert.IsTrue(sim.Admission.Passed.Any(p => p.Kind == "expired"));
            NextGroup(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            RetreatUnharmed(sim);
            Assert.IsTrue(sim.PassCandidates().Ok);                                // kilitli planda geri çevirme
            Close(sim);
            return sim;
        }

        static GameRecord RoundTrip(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            return UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
        }

        static void AssertSameGame(Simulation sim, Simulation restored, string what)
        {
            Assert.AreEqual(sim.S.Day, restored.S.Day, what);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State, what);
            Assert.AreEqual(sim.S.Ending, restored.S.Ending, what);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies, what);
            Assert.AreEqual(Cards(sim), Cards(restored), what);
            Assert.AreEqual(Passed(sim), Passed(restored), what);
            Assert.AreEqual(BoardJson(sim), BoardJson(restored), what);
        }

        [Test]
        public void Replay_AfterJsonRoundTrip_RebuildsTheSameGame_AndContinuesIdentically()
        {
            foreach (uint seed in new uint[] { 79, 5, 31 })
            {
                var sim = Story(seed);
                var kinds = sim.Record.commands.Select(c => c.kind).ToList();
                Assert.AreEqual(2, kinds.Count(k => k == "candidate_choose"));
                Assert.AreEqual(2, kinds.Count(k => k == "candidate_pass"));
                var plain = sim.Record.commands.First(c => c.kind == "candidate_choose");
                Assert.IsNull(plain.b, "yer açmadan seçim: yollanan yok");

                var record = RoundTrip(sim);
                Assert.AreEqual(GameRecord.CurrentVersion, record.version);
                Assert.IsTrue(string.IsNullOrEmpty(record.commands.First(c => c.kind == "candidate_choose").b), "JSON gidiş-dönüşünde null, boş dizgi olur");
                Assert.AreEqual("sakin-9", record.commands.Last(c => c.kind == "candidate_choose").b);
                Assert.AreEqual(sim.B.candidateTier0CostChance, record.balance.candidateTier0CostChance, "yeni denge alanları kayıtla taşınır");
                Assert.AreEqual(sim.B.perkSupplies, record.balance.perkSupplies);

                var restored = Replay.Restore(record);
                AssertSameGame(sim, restored, "tohum " + seed);
                Assert.IsNull(restored.Board.Person("sakin-9"));
                Assert.IsTrue(restored.Admission.File("sakin-9").Departed);

                // Kayıttan devam: iki oyun aynı komutlarla aynı yere varır
                foreach (var game in new[] { sim, restored })
                {
                    var group = NextGroup(game);
                    Assert.IsTrue(game.ChooseCandidate(group[0].Resident.Id, game.Board.People.First(p => p.Alive && p.Job == "rest").Id).Ok);
                    Close(game); Close(game);
                }
                AssertSameGame(sim, restored, "devam, tohum " + seed);
            }
        }

        /// <summary>JsonUtility null dizgiyi "" yazar: yollanan kişi alanı "" olan kayıt, null olanla aynı oyunu kurar.</summary>
        [Test]
        public void Replay_EmptyReplaceId_IsTheSameAsNull()
        {
            var sim = Story(79);
            sim.Record.day = sim.S.Day;
            var copy = new GameRecord
            {
                version = sim.Record.version, seed = sim.Record.seed, balance = sim.Record.balance.Clone(), day = sim.Record.day,
                commands = sim.Record.commands.Select(c => new SavedCommand { day = c.day, kind = c.kind, a = c.a, b = c.b ?? "" }).ToList(),
            };
            Assert.IsTrue(copy.commands.Any(c => c.kind == "candidate_choose" && c.b == ""));
            Assert.IsTrue(copy.commands.Any(c => c.kind == "candidate_pass" && c.b == ""));
            AssertSameGame(sim, Replay.Restore(copy), "b = \"\"");
        }

        [Test]
        public void Replay_MidShift_RestoresTheWaitingGroupAndPendingReceipt()
        {
            var sim = New();
            NextGroup(sim);
            Assert.IsTrue(sim.PassCandidates().Ok);
            var group = NextGroup(sim);
            Assert.IsTrue(sim.ChooseCandidate(group[0].Resident.Id).Ok);
            Close(sim);
            NextGroup(sim);
            Assert.IsTrue(sim.PrintShift().Ok);
            var restored = Replay.Restore(RoundTrip(sim));
            AssertSameGame(sim, restored, "fiş basılı, grup kapıda");
            Assert.IsTrue(restored.Board.Printed);
            Assert.AreEqual(sim.Board.ReceiptText, restored.Board.ReceiptText);
            Assert.Greater(restored.Admission.Group.Count, 0);
            Assert.AreEqual(Loc.T("Fiş basıldı; kapı bu vardiya kapalı."), restored.CandidatePassCheck());
        }

        // ---------------------------------------------------------------- denge ve sürüm

        [Test]
        public void BalanceClone_IsIndependent_BecauseEveryFieldIsFlat()
        {
            var fields = typeof(Balance).GetFields(BindingFlags.Instance | BindingFlags.Public);
            Assert.Greater(fields.Length, 100);
            foreach (string added in new[]
            {
                "admissionKnockMax", "candidateGroupMin", "candidateGroupMax", "candidateWaitShifts", "candidateEmergencyBelow",
                "candidateTier0Share", "candidateTier2Share", "candidateTier0Robot", "candidateTier1Robot", "candidateTier2Robot",
                "candidateTier0CostChance", "perkCircuits", "perkCircuitShifts", "perkSupplies", "perkSupplyShifts", "perkWoundedShifts",
                "perkSturdyHp", "perkSlowMove", "perkDoubleRationFactor", "dismissSupplies", "traceDelayMin", "traceDelayMax",
                "secondTraceGapMin", "secondTraceGapMax", "humanTraceChance", "humanSecondTraceChance", "boardMissingKitCircuits",
                "boardStarvingKitProduction", "exposureThreshold", "exposureWarnAt1", "exposureWarnAt2", "exposureAlarmAt",
                "exposureAlarmBonus", "abandonUnderstaffedShifts", "lastSwitchMinShift", "evacuationTrustBelow", "victoryCleanMaxDeaths",
                "victoryCleanMaxExposure", "victoryCleanMinTrust",
            })
                Assert.IsTrue(fields.Any(f => f.Name == added), added);

            var source = new Balance { shelterEventsEnabled = false };
            var copy = source.Clone();
            Assert.AreNotSame(source, copy);
            foreach (var f in fields)
            {
                Assert.IsTrue(f.FieldType.IsValueType || f.FieldType == typeof(string), f.Name + ": denge alanları düz kalmalı (sığ kopya yeter)");
                object original = f.GetValue(source);
                Assert.AreEqual(original, f.GetValue(copy), f.Name);
                object changed;
                if (f.FieldType == typeof(int)) changed = (int)original + 1;
                else if (f.FieldType == typeof(double)) changed = (double)original + 1;
                else if (f.FieldType == typeof(bool)) changed = !(bool)original;
                else if (f.FieldType == typeof(string)) changed = original + "x";
                else continue;
                f.SetValue(copy, changed);
                Assert.AreEqual(original, f.GetValue(source), f.Name + ": kopya kaynağı etkiledi");
                Assert.AreNotEqual(original, f.GetValue(copy), f.Name);
            }

            // Kayıt, oyun kurulurken verilen dengenin kopyasını taşır
            var balance = Bal();
            var sim = New(79, balance);
            balance.candidateWaitShifts = 9;
            balance.perkSupplies = 99;
            Assert.AreEqual(2, sim.Record.balance.candidateWaitShifts);
            Assert.AreEqual(6, sim.Record.balance.perkSupplies);
        }

        [Test]
        public void Defaults_AndVersion17()
        {
            var b = new Balance { shelterEventsEnabled = false };
            Assert.AreEqual(3, b.admissionKnockMin);
            Assert.AreEqual(4, b.admissionKnockMax);
            Assert.AreEqual(2, b.candidateGroupMin);
            Assert.AreEqual(3, b.candidateGroupMax);
            Assert.AreEqual(2, b.candidateWaitShifts);
            Assert.AreEqual(3, b.candidateEmergencyBelow);
            Assert.AreEqual(0, b.boardMissingKitCircuits);
            Assert.IsTrue(b.boardStarvingKitProduction);
            Assert.AreEqual(17, GameRecord.CurrentVersion, "sonlar (biçim 17); aday grubu, kişiye bağlı teçhizat, acil grup ve kit kilidi biçim 16'da girdi; eski biçim yüklenmez");
            Assert.AreEqual(17, new GameRecord().version);
            var sim = New();
            Assert.AreEqual(17, sim.Record.version);
            Assert.AreEqual(0, sim.Admission.Group.Count);
            Assert.AreEqual(-1, sim.Admission.GroupArrivalShift);
            Assert.AreEqual(0, sim.Admission.Passed.Count);
            CollectionAssert.AreEqual(new[] { "tek", "emp", "ana", "craft", "cook" }, PlanningBoardData.CandidateSubjects);
        }
    }
}
