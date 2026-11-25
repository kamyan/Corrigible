using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 12, parça P4a (aşama 12 tasarımı §11.1, §11.5, §11.6): iki tesis şartının (Katar, Yalpa) saf masa metinleri. Paftanın
    /// açık kenar satırı (yatış <see cref="Balance.facilityFallInjuryTurns"/>), yatış takvimi (Core <see cref="Baskin.TiltSchedule"/>: 5 turluk
    /// odada 6. tur yok, 8 turluk odada var), odanın tur bütçesi, operasyon raporunun kenar satırı ve fişin olayı; TR ve EN (EN'de Türkçe
    /// harf yok). Bölge olayları ya da alt bayrak kapalıyken metin yok.
    /// </summary>
    public sealed class FacilityConditionTextTests
    {
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void Restore() => Loc.SetLanguage(Loc.Turkish);

        static void UseEnglish()
        {
            var table = Loc.ParseTable(File.ReadAllText(TablePath));
            table.TryGetValue(Loc.English, out var en);
            Loc.SetLanguage(Loc.English, en);
        }

        static bool HasTurkish(string s) => s != null && s.Any(c => "çğışöüÇĞİŞÖÜ".IndexOf(c) >= 0);

        static FacilityRoom Room(Simulation sim, string region, int wing, int index) =>
            sim.FacilityPlan.Room(sim.LiberationRegion(region).Sectors[wing].Id, index);

        [Test]
        public void PlanLines_Calendar_AndReport_Turkish()
        {
            var b = new Balance();
            Assert.AreEqual(5, b.facilityFallInjuryTurns, "C# varsayılanı (tasarım §1.2)");
            Assert.AreEqual("Açık kenar (kuzey, güney): itilen düşer; makine yok olur, ekip üyesi 5 vardiya yatar.", FacilityConditionText.PlanOpenEdge(b));
            b.facilityFallInjuryTurns = 3;
            StringAssert.Contains("ekip üyesi 3 vardiya yatar", FacilityConditionText.PlanOpenEdge(b));
            b.facilityFallInjuryTurns = 0;
            Assert.AreEqual(1, FacilityConditionText.FallShifts(b), "Core'un dönüşü gibi en az 1");

            Assert.AreEqual("2. tur batı, 4. tur doğu", FacilityConditionText.TiltCalendar(5), "5 turluk odada 6. tur yazılmaz");
            Assert.AreEqual("2. tur batı, 4. tur doğu, 6. tur batı", FacilityConditionText.TiltCalendar(8));
            Assert.AreEqual("2. tur batı, 4. tur doğu, 6. tur batı", FacilityConditionText.TiltCalendar(7));
            Assert.AreEqual("2. tur batı", FacilityConditionText.TiltCalendar(3));
            Assert.IsNull(FacilityConditionText.TiltCalendar(2), "son turda yatış yok");
            Assert.IsNull(FacilityConditionText.PlanTilt(2));
            // Takvim Core'dan (TiltSchedule); beklenti sabit metin ve sabit adımlar (aşama 12 kapanışı: aynı ifadeyle yeniden kurmak yerine)
            Assert.AreEqual("2. tur batı, 4. tur doğu", FacilityConditionText.TiltCalendar(6), "6 turluk odada 6. tur son tur");
            CollectionAssert.AreEqual(new[] { (2, -1), (4, 1), (6, -1) }, Baskin.TiltSchedule(8).Select(s => (s.Turn, s.Dir)).ToArray());
            CollectionAssert.AreEqual(new[] { (2, -1), (4, 1) }, Baskin.TiltSchedule(5).Select(s => (s.Turn, s.Dir)).ToArray());
            Assert.AreEqual("Yalpa: 2. tur batı, 4. tur doğu · sabitlenmemiş her şey 1 kare kayar", FacilityConditionText.PlanTilt(5));
            Assert.AreEqual("Yalpa: 2. tur batı, 4. tur doğu, 6. tur batı · sabitlenmemiş her şey 1 kare kayar", FacilityConditionText.PlanTilt(8));

            Assert.AreEqual("vagondan düştü · 5 vardiya yatış", FacilityConditionText.ReportOverboard(5));
        }

        [Test]
        public void PlanLines_Calendar_AndReport_English_NoTurkishLetters()
        {
            UseEnglish();
            var b = new Balance();
            Assert.AreEqual("Open edge (north, south): pushed units fall; machines are lost, crew are laid up 5 shifts.", FacilityConditionText.PlanOpenEdge(b));
            Assert.AreEqual("turn 2 west, turn 4 east", FacilityConditionText.TiltCalendar(5));
            Assert.AreEqual("Roll: turn 2 west, turn 4 east, turn 6 west · anything not fixed slides 1 square", FacilityConditionText.PlanTilt(8));
            Assert.AreEqual("fell off the car · laid up 5 shifts", FacilityConditionText.ReportOverboard(5));
            foreach (var t in new[] { FacilityConditionText.PlanOpenEdge(b), FacilityConditionText.PlanTilt(5), FacilityConditionText.PlanTilt(8),
                         FacilityConditionText.ReportOverboard(5) })
                Assert.IsFalse(HasTurkish(t), t);

            var sim = FacilityDeskKit.New();
            Assert.AreEqual("Open edge (north, south): pushed units fall; machines are lost, crew are laid up 5 shifts.",
                FacilityConditionText.PlanLine(sim, Room(sim, "na", 0, 2)));
            Assert.IsFalse(HasTurkish(FacilityConditionText.PlanLine(sim, Room(sim, "oc", 2, 3))));
        }

        [Test]
        public void PlanLine_FollowsTheRoomsCondition_AndTurnBudget()
        {
            var sim = FacilityDeskKit.New();
            for (int wing = 0; wing < 3; wing++)
            {
                var naEntry = Room(sim, "na", wing, 1);
                var naMid = Room(sim, "na", wing, 2);
                var naTarget = Room(sim, "na", wing, 3);
                Assert.IsNull(naEntry.Event, "giriş vagonu şartsız");
                Assert.IsNull(FacilityConditionText.PlanLine(sim, naEntry));
                Assert.IsEmpty(FacilityConditionText.PlanOpenRows(sim, naEntry));
                foreach (var r in new[] { naMid, naTarget })
                {
                    Assert.AreEqual("katar", r.Event, r.Id);
                    Assert.AreEqual(FacilityConditionText.PlanOpenEdge(sim.B), FacilityConditionText.PlanLine(sim, r), r.Id);
                    CollectionAssert.AreEqual(Baskin.OpenEdgeRows(r), FacilityConditionText.PlanOpenRows(sim, r), r.Id);
                    CollectionAssert.AreEqual(new[] { 0, 7 }, FacilityConditionText.PlanOpenRows(sim, r), r.Id);
                }
                // Tur bütçesi: giriş ve ara 6, hedef 10 (na "kisa": 9)
                Assert.AreEqual(sim.B.facilityEntryTurns, FacilityConditionText.RoomTurns(sim, naEntry));
                Assert.AreEqual(sim.B.facilityMiddleTurns, FacilityConditionText.RoomTurns(sim, naMid));
                Assert.AreEqual(sim.B.facilityTargetTurns - 1, FacilityConditionText.RoomTurns(sim, naTarget), "na kısa nöbet: hedef bir tur kısa");

                var ocEntry = Room(sim, "oc", wing, 1);
                var ocMid = Room(sim, "oc", wing, 2);
                var ocTarget = Room(sim, "oc", wing, 3);
                Assert.IsNull(FacilityConditionText.PlanLine(sim, ocEntry));
                Assert.AreEqual("yalpa", ocMid.Event);
                Assert.AreEqual(sim.B.facilityMiddleTurns, FacilityConditionText.RoomTurns(sim, ocMid));
                Assert.AreEqual(sim.B.facilityTargetTurns, FacilityConditionText.RoomTurns(sim, ocTarget));
                Assert.AreEqual(FacilityConditionText.PlanTilt(sim.B.facilityMiddleTurns), FacilityConditionText.PlanLine(sim, ocMid));
                Assert.AreEqual(FacilityConditionText.PlanTilt(sim.B.facilityTargetTurns), FacilityConditionText.PlanLine(sim, ocTarget));
                StringAssert.DoesNotContain("6. tur", FacilityConditionText.PlanLine(sim, ocMid));
                StringAssert.Contains("6. tur batı", FacilityConditionText.PlanLine(sim, ocTarget));
                Assert.IsEmpty(FacilityConditionText.PlanOpenRows(sim, ocMid), "Yalpa'da açık kenar yok");
            }
            // Öbür bölgelerin şartlı odalarında satır yok
            foreach (var room in sim.FacilityPlan.Rooms.Where(r => r.Region != "na" && r.Region != "oc"))
                Assert.IsNull(FacilityConditionText.PlanLine(sim, room), room.Id);
            Assert.AreEqual("katar", FacilityConditionText.EventOf(sim, "na"));
            Assert.AreEqual("yalpa", FacilityConditionText.EventOf(sim, "oc"));
            Assert.AreEqual(GameData.Region("weu").Event, FacilityConditionText.EventOf(sim, "weu"));
            Assert.AreEqual("asansor", FacilityConditionText.EventOf(null, "na"), "simülasyonsuz: bugünkü olay");
        }

        [Test]
        public void FlagsOff_NoText()
        {
            // Bölge olayları kapalı: şart planda görünmez
            var bal = FacilityDeskKit.Bal();
            bal.liberationRegionEvents = false;
            var quiet = Simulation.NewGame(79, bal);
            Assert.IsNull(FacilityConditionText.PlanLine(quiet, Room(quiet, "na", 0, 2)));
            Assert.IsNull(FacilityConditionText.PlanLine(quiet, Room(quiet, "oc", 0, 2)));
            Assert.IsEmpty(FacilityConditionText.PlanOpenRows(quiet, Room(quiet, "na", 0, 2)));
            Assert.IsNull(FacilityConditionText.EventOf(quiet, "na"));

            // Alt bayraklar kapalı: na ve oc'nin odaları bugünkü olayla, satır yok
            bal = FacilityDeskKit.Bal();
            bal.facilityKatar = false;
            bal.facilityYalpa = false;
            var sub = Simulation.NewGame(79, bal);
            var subNa = Room(sub, "na", 0, 2);
            var subOc = Room(sub, "oc", 0, 2);
            Assert.AreEqual("asansor", subNa.Event);
            Assert.AreEqual("yag", subOc.Event);
            Assert.IsNull(FacilityConditionText.PlanLine(sub, subNa));
            Assert.IsNull(FacilityConditionText.PlanLine(sub, subOc));
            Assert.IsEmpty(FacilityConditionText.PlanOpenRows(sub, subNa));
            Assert.AreEqual("asansor", FacilityConditionText.EventOf(sub, "na"));
        }
    }
}
