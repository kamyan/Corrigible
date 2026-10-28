using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>Sığınağın kapısı: vuruş zamanı, açma/açmama, bedeller, zincirler, ölümcül ziyaretçilerin kuralları, kayıt.</summary>
    public class DoorTests
    {
        static Simulation New(uint seed = 101, Balance b = null) => Simulation.NewGame(seed, b ?? new Balance { startFon = 100 });

        static void Until(Simulation sim, int day) { while (sim.S.Day < day && sim.S.Ending == null) sim.Step(); }

        /// <summary>Ziyaretçiyi doğrudan kapıya koyar (kartın kendi koşulunu beklemeden).</summary>
        static void Knock(Simulation sim, string card) => sim.S.Door.Visit = new DoorVisit { Day = sim.S.Day, Card = card };

        [Test]
        public void FirstKnock_IsTheFriendlyScriptedVisitor_AfterTheFirstShifts()
        {
            var sim = New();
            Assert.IsNull(sim.DoorVisitor);
            Until(sim, sim.B.doorFirstKnock - 1);
            Assert.IsNull(sim.DoorVisitor, "ilk vardiyalar (eğitim) kapısız");
            sim.Step();
            Assert.AreEqual(DoorCards.First, sim.DoorVisitor?.Card);
            Assert.IsFalse(DoorCards.Get(DoorCards.First).Deadly);
        }

        [Test]
        public void OpeningThenChoosing_AppliesTheEffect_AndIsRecorded()
        {
            var sim = New();
            Until(sim, sim.B.doorFirstKnock);
            double supplies = sim.Shelter.Supplies;
            int trust = sim.S.Trust;
            Assert.IsNotNull(sim.DoorAnswerCheck("al"), "kapı kapalıyken açık kapının seçeneği yok");
            Assert.IsTrue(sim.AnswerDoor(Simulation.DoorOpen).Ok);
            Assert.IsTrue(sim.DoorVisitor.Open);
            Assert.IsNotNull(sim.DoorAnswerCheck(Simulation.DoorRefuse), "açılan kapı artık açılmamış sayılmaz");
            Assert.IsTrue(sim.AnswerDoor("al").Ok);
            Assert.IsNull(sim.DoorVisitor);
            var v = sim.S.Door.Log.Last();
            Assert.AreEqual("al", v.Choice);
            Assert.IsFalse(v.Auto);
            Assert.AreEqual(supplies + v.Supplies, sim.Shelter.Supplies, 1e-9);
            Assert.Greater(v.Supplies, 0, "kese depoya girer");
            Assert.AreEqual(trust + v.Trust, sim.S.Trust);
            Assert.AreEqual(2, sim.Record.commands.Count(c => c.kind == "door_answer"));
            Assert.AreEqual(DoorCards.First, sim.Record.commands.Last().b);
        }

        [Test]
        public void UnansweredVisitor_CountsAsRefused_WhenTheShiftEnds()
        {
            var sim = New();
            Until(sim, sim.B.doorFirstKnock);
            Knock(sim, "anne");
            int trust = sim.S.Trust;
            sim.Step();
            var v = sim.S.Door.Log.Last();
            Assert.AreEqual("anne", v.Card);
            Assert.AreEqual(Simulation.DoorRefuse, v.Choice);
            Assert.IsTrue(v.Auto);
            Assert.AreEqual(-1, v.Trust);
            Assert.AreEqual(trust - 1, sim.S.Trust, "kapıyı açmamanın bedeli");
        }

        [Test]
        public void Knocks_ComeEveryFewShifts_AndNeverTouchTheMainRng()
        {
            for (uint seed = 1; seed <= 20; seed++)
            {
                var sim = New(seed, new Balance { startFon = 100, liberationTrustStart = 40, liberationAlarmThreshold = 1000 });
                uint rng = sim.S.Rng.State;
                int last = -1;
                for (int i = 0; i < 40 && sim.S.Ending == null; i++)
                {
                    sim.Step();
                    var v = sim.DoorVisitor;
                    if (v == null || v.Day != sim.S.Day) continue;
                    if (last >= 0) Assert.That(v.Day - last, Is.InRange(sim.B.doorKnockMin, sim.B.doorKnockMax + 1), $"tohum {seed}");
                    last = v.Day;
                    // Açılan kapı da ana sırayı tüketmez
                    if (sim.DoorAnswerCheck(Simulation.DoorOpen) == null && !DoorCards.Get(v.Card).Deadly) sim.AnswerDoor(Simulation.DoorOpen);
                }
                Assert.Greater(sim.S.Door.Log.Count, 8, $"tohum {seed}: 40 günde birçok ziyaret");
                Assert.AreEqual(rng, sim.S.Rng.State, $"tohum {seed}: kapı ana rastgelelik sırasına dokunmaz");
            }
        }

        [Test]
        public void Copy_NeedsAWarning_NeverComesInSafeShifts_AndOnlyOpeningKills()
        {
            var b = new Balance { startFon = 100, liberationTrustStart = 40, liberationAlarmThreshold = 1000, doorCopyAlarm = 0.003 };
            // Uyarı yoksa kopya gelmez (her ziyaretçi geri çevrilir: uyarı da hiç alınmaz)
            for (uint seed = 1; seed <= 15; seed++)
            {
                var sim = New(seed, b);
                for (int i = 0; i < 60 && sim.S.Ending == null; i++)
                {
                    sim.Step();
                    if (sim.DoorVisitor != null) Assert.IsTrue(sim.AnswerDoor(Simulation.DoorRefuse).Ok);
                }
                Assert.IsFalse(sim.S.Door.Log.Any(v => v.Card == "kopya"), $"tohum {seed}");
                Assert.IsNull(sim.S.Ending);
            }
            // Uyarıldıysan ve alarm yüksekse gelir; güvenli günlerden önce asla
            int seen = 0;
            for (uint seed = 1; seed <= 15; seed++)
            {
                var sim = New(seed, b);
                sim.S.Door.Flags.Add(DoorCards.CopyWarning);
                for (int i = 0; i < 60 && sim.S.Ending == null; i++)
                {
                    sim.Step();
                    var v = sim.DoorVisitor;
                    if (v == null) continue;
                    if (v.Card != "kopya") { sim.AnswerDoor(Simulation.DoorRefuse); continue; }
                    Assert.GreaterOrEqual(v.Day, sim.B.doorSafeShifts, $"tohum {seed}");
                    seen++;
                    Assert.IsTrue(sim.AnswerDoor(seen % 2 == 0 ? Simulation.DoorRefuse : Simulation.DoorOpen).Ok);
                    Assert.AreEqual(seen % 2 == 0 ? null : "sizma", sim.S.Ending, $"tohum {seed}: yalnız açmak öldürür");
                }
            }
            Assert.Greater(seen, 3, "uyarılmış ve alarmı yüksek harekâtta kopya kapıyı çalar");
        }

        [Test]
        public void ArmedResident_FollowsARefusedDelegation_WhileTheShelterStarves()
        {
            var sim = New(7, new Balance { startFon = 100, liberationTrustStart = 40, liberationAlarmThreshold = 1000 });
            Until(sim, 9);
            // Açlık: erzak yok, toplayan yok
            sim.Shelter.Room(ShelterLogic.Living).Workers = 0;
            sim.Shelter.Supplies = 0;
            sim.S.Door.Visit = null;
            Knock(sim, "heyet");
            Assert.IsTrue(sim.AnswerDoor(Simulation.DoorOpen).Ok);
            Assert.IsNull(sim.DoorAnswerCheck("fon"), "fon yetiyor: fonla erzak bulunabilir");
            Assert.IsTrue(sim.AnswerDoor("bekle").Ok);
            Assert.IsTrue(sim.DoorFlag(DoorCards.DelegationRefused));
            var chain = sim.S.Door.Chains.Single();
            Assert.AreEqual("silahli", chain.Card);
            while (sim.S.Day < chain.Day + sim.B.doorKnockMax + 1 && sim.DoorVisitor?.Card != "silahli")
            {
                sim.Step();
                if (sim.DoorVisitor != null && sim.DoorVisitor.Card != "silahli") sim.AnswerDoor(Simulation.DoorRefuse);
            }
            Assert.AreEqual("silahli", sim.DoorVisitor?.Card, "geri çevrilen heyet, açlık sürerse silahla döner");
            // Erzak yok, güven 20'nin altında: konuşmak kapıyı kurtarmaz ama kimse ölmez
            sim.S.Trust = 12;
            int trust = sim.S.Trust;
            Assert.IsNull(sim.DoorAnswerCheck("konus"), "konuşmanın bedeli yok; sonucu duruma bağlı");
            Assert.IsTrue(sim.AnswerDoor("konus").Ok);
            Assert.IsNull(sim.S.Ending);
            Assert.AreEqual(trust - 2, sim.S.Trust);
            Assert.IsFalse(sim.DoorFlag(DoorCards.DelegationRefused));
        }

        [Test]
        public void ArmedResident_IsDroppedIfTheHungerEnded()
        {
            var sim = New(7, new Balance { startFon = 100, liberationTrustStart = 40, liberationAlarmThreshold = 1000 });
            Until(sim, 9);
            sim.S.Door.Visit = null;
            Knock(sim, "heyet");
            sim.AnswerDoor(Simulation.DoorOpen);
            sim.AnswerDoor("bekle");
            // Erzak bol: açlık bitti, silahlı sakin gelmez
            for (int i = 0; i < 10; i++)
            {
                sim.Step();
                Assert.AreNotEqual("silahli", sim.DoorVisitor?.Card);
                if (sim.DoorVisitor != null) sim.AnswerDoor(Simulation.DoorRefuse);
            }
            Assert.IsEmpty(sim.S.Door.Chains.Where(c => c.Card == "silahli"));
        }

        [Test]
        public void OpeningToAnArmedVisitor_EndsTheCampaign()
        {
            var sim = New();
            Until(sim, 9);
            sim.S.Door.Visit = null;
            Knock(sim, "silahli");
            Assert.IsTrue(sim.AnswerDoor(Simulation.DoorOpen).Ok);
            Assert.AreEqual("vuruldu", sim.S.Ending);
            Assert.AreEqual("vuruldu", sim.S.Door.Log.Last().Ending);
            Assert.IsNull(sim.DoorVisitor);
            Assert.IsNotNull(GameData.Ending("vuruldu"));
            Assert.IsFalse(sim.AnswerDoor(Simulation.DoorRefuse).Ok);
        }

        [Test]
        public void Costs_AndRoomNeeds_AreCheckedBeforeAnswering()
        {
            var sim = New();
            Until(sim, 3);
            sim.S.Door.Visit = null;
            Knock(sim, "anne");
            sim.AnswerDoor(Simulation.DoorOpen);
            sim.Shelter.Supplies = 2;
            Assert.IsNotNull(sim.DoorAnswerCheck("erzak"), "erzak yetmiyor");
            Assert.IsNotNull(sim.DoorAnswerCheck("revir"), "revir onarılmamış");
            Assert.IsFalse(sim.AnswerDoor("erzak").Ok);
            Assert.IsNotNull(sim.DoorVisitor, "başarısız cevap ziyareti bitirmez");
            sim.Shelter.Supplies = 20;
            Assert.IsNull(sim.DoorAnswerCheck("erzak"));
            Assert.IsTrue(sim.AnswerDoor("erzak").Ok);
            Assert.AreEqual(12, sim.Shelter.Supplies, 1e-9);
            Assert.AreEqual("anne_oglu", sim.S.Door.Chains.Single().Card, "yardım edilen anne: oğlu sonra kapıya gelir");
        }

        [Test]
        public void Leaving_UnassignsWorkers_FromTheBackRoomsFirst()
        {
            var sim = New();
            var s = sim.Shelter;
            s.Room(ShelterLogic.Radio).Workers = 2;
            s.Room(ShelterLogic.Living).Workers = 2;
            s.Population = 4;
            ShelterLogic.Leave(s, 1);
            Assert.AreEqual(3, s.Population);
            Assert.AreEqual(1, s.Room(ShelterLogic.Radio).Workers);
            Assert.AreEqual(2, s.Room(ShelterLogic.Living).Workers);
            ShelterLogic.Leave(s, 10);
            Assert.AreEqual(1, s.Population, "sığınak boşalmaz");
            Assert.AreEqual(1, s.Rooms.Sum(r => r.Workers));
        }

        [Test]
        public void EveryCard_IsComplete_AndItsChainsAndEndingsExist()
        {
            var sim = New();
            foreach (var c in DoorCards.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(c.Who) || string.IsNullOrEmpty(c.Knock) || string.IsNullOrEmpty(c.Peek) || string.IsNullOrEmpty(c.Ask), c.Id);
                Assert.IsTrue(c.Options.Length == 0 || !string.IsNullOrEmpty(c.Opened), c.Id + ": açılan kapıda söylenen");
                Assert.IsTrue(c.Options.Length > 0 || c.OnOpen?.Text != null, c.Id + ": açılınca biten ziyaretin sonucu");
                Assert.IsNotNull(c.Ignore?.Text, c.Id + ": açılmayan kapının sonucu");
                Assert.IsTrue(!c.Deadly || c.OnOpen?.End != null, c.Id + ": ölümcül kart açılınca bitirir");
                Assert.IsFalse(c.Options.Concat(c.Closed).Any(o => o.Id == Simulation.DoorOpen || o.Id == Simulation.DoorRefuse), c.Id);
                foreach (var o in c.Options.Concat(c.Closed))
                {
                    var e = o.Effect(sim);
                    Assert.IsNotNull(e?.Text, c.Id + "." + o.Id);
                    foreach (var next in e.Chain ?? new string[0]) Assert.IsTrue(DoorCards.Exists(next), c.Id + " → " + next);
                    if (e.End != null) Assert.IsNotNull(GameData.Ending(e.End));
                }
                if (c.OnOpen?.End != null) Assert.IsNotNull(GameData.Ending(c.OnOpen.End));
                Assert.DoesNotThrow(() => c.When?.Invoke(sim), c.Id);
                Assert.DoesNotThrow(() => c.Still?.Invoke(sim), c.Id);
            }
        }

        [Test]
        public void Replay_RestoresDoorAnswersAndVisitorsExactly()
        {
            var sim = New(33);
            for (int i = 0; i < 30 && sim.S.Ending == null; i++)
            {
                sim.Step();
                var v = sim.DoorVisitor;
                if (v == null || DoorCards.Get(v.Card).Deadly) continue;
                // Sırayla: aç ve ilk uygun seçenek; açma; cevapsız bırak
                if (i % 3 == 0 && sim.AnswerDoor(Simulation.DoorOpen).Ok && sim.DoorVisitor != null)
                {
                    var opt = sim.DoorOptions().FirstOrDefault(o => sim.DoorAnswerCheck(o.Id) == null);
                    if (opt != null) sim.AnswerDoor(opt.Id);
                }
                else if (i % 3 == 1) sim.AnswerDoor(Simulation.DoorRefuse);
            }
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(sim.Record);
            Assert.AreEqual(sim.S.Trust, restored.S.Trust);
            Assert.AreEqual(sim.S.Fon, restored.S.Fon, 1e-9);
            Assert.AreEqual(sim.Shelter.Supplies, restored.Shelter.Supplies, 1e-9);
            Assert.AreEqual(sim.Shelter.Population, restored.Shelter.Population);
            Assert.AreEqual(sim.S.Door.Log.Select(v => v.Card + ":" + v.Choice), restored.S.Door.Log.Select(v => v.Card + ":" + v.Choice));
            Assert.AreEqual(sim.DoorVisitor?.Card, restored.DoorVisitor?.Card);
            Assert.AreEqual(sim.DoorVisitor?.Open, restored.DoorVisitor?.Open);
            Assert.AreEqual(sim.S.Door.NextKnock, restored.S.Door.NextKnock);
            Assert.AreEqual(sim.S.Door.Flags, restored.S.Door.Flags);
        }
    }
}
