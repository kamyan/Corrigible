using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 6: oda hâlleri ve pano içeriği (not, maliyet, düğme hâlleri). Sahne bağlantısı sınamaları Aşama 10'da (T1a) maketle birlikte kalktı.</summary>
    public class SiginakShelterTests
    {
        static Simulation NewLiberation(double fon = 100) => LegacySimulation.NewGame(731, new Balance { startFon = fon });

        static ShelterRoomData Room(Simulation sim, string id) => ShelterViewBuilder.Rooms(sim, id).Single(r => r.Id == id);

        [Test]
        public void Rooms_NewGame_WorkshopAndInfirmaryStartDamaged()
        {
            var sim = NewLiberation();
            var rooms = ShelterViewBuilder.Rooms(sim, ShelterLogic.Living);
            CollectionAssert.AreEqual(ShelterViewBuilder.Order, rooms.Select(r => r.Id).ToArray());
            Assert.AreEqual(RoomCondition.Working, Room(sim, ShelterLogic.Living).Condition);
            Assert.AreEqual(RoomCondition.Working, Room(sim, ShelterLogic.Radio).Condition);
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Workshop).Condition);
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Infirmary).Condition);
            Assert.IsTrue(rooms.Single(r => r.Id == ShelterLogic.Living).Selected);
            Assert.AreEqual(sim.B.shelterWorkersPerLevel, Room(sim, ShelterLogic.Living).Capacity, "kapasite = yuva sayısı");
        }

        [Test]
        public void V014_RadioWithoutStaff_NotesAlarm_AndSummaryShowsSquadAndPlates()
        {
            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            Assert.AreEqual(RoomCondition.Working, Room(sim, ShelterLogic.Workshop).Condition, "malzeme var, depo boş: döküm sürer");
            Assert.AreEqual(RoomCondition.Damaged, Room(sim, ShelterLogic.Infirmary).Condition);
            Assert.IsTrue(Room(sim, ShelterLogic.Workshop).Top, "atölye SV3: en üst seviye");
            StringAssert.Contains("3/3", Room(sim, ShelterLogic.Workshop).Line, "K8: kartta 3/3");

            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Radio);
            Assert.AreEqual(Loc.F("Görevli yok: alarm her vardiya +{0} yükselir.", "1"), d.Note);
            // Seviye 2 iki özgür bölge ister; fon ve malzeme yetse de düğme kilitli
            Assert.AreEqual(SlipButtonState.Disabled, d.Action.State);
            Assert.AreEqual(sim.ShelterLevelLock(ShelterLogic.Radio), d.ActionNote, "kilit gerekçesi düğmenin altında");
            Assert.AreEqual(Loc.F("V.{0:000}", sim.S.Day + 1), d.Shift);
            CollectionAssert.AreEqual(new[] { "T", "E", "A", "M", "S" }, d.Squad.Select(m => m.Letter).ToArray(), "beş uzman diskle");
            Assert.IsTrue(d.Squad.All(m => !m.Missing), "atölye SV3: hepsi kadroda");
            Assert.AreEqual(Loc.F("{0}/{1}", 3, 3), d.Squad[0].Hp);
            // Yeni harekâtta Madenci ve Sis uzmanı atölyeyi bekler: soluk disk, yanında seviye
            var fresh = ShelterViewBuilder.Board(NewLiberation(), ShelterLogic.Workshop);
            CollectionAssert.AreEqual(new[] { false, false, false, true, true }, fresh.Squad.Select(m => m.Missing).ToArray());
            Assert.AreEqual(Loc.F("sv {0}", 2), fresh.Squad[3].Hp);
            StringAssert.Contains(Loc.F("Seviye {0}: {1} kadroya katılır.", 2, Baskin.UnitTypes["maden"].Name), fresh.Description);
            StringAssert.Contains("0/3", d.Plates);
            // S3: asıl soru erzak kaç vardiya yeter (22 ÷ 1,4 = 15)
            Assert.AreEqual(Loc.T("ERZAK YETER"), d.SuppliesLabel);
            Assert.AreEqual(Loc.F("{0} VARDİYA", 15), d.SuppliesBig);
            Assert.IsFalse(d.SuppliesLow);
            Assert.IsNull(d.SuppliesStamp);
            Assert.AreEqual(Loc.F("ERZAK · {0} VARDİYA", 15), ShelterViewBuilder.Store(sim).Slate);
        }

        /// <summary>S3 › özet: n = ⌊erzak ÷ |fark|⌋; fark ≥ 0 → ERZAK ARTIYOR; 3 ve altı kırmızı + AZ KALDI.</summary>
        [Test]
        public void ShiftsLeft_FloorsAndFlagsLowAndRising()
        {
            Assert.AreEqual(15, ShelterViewBuilder.ShiftsLeft(22, -1.4));
            Assert.AreEqual(17, ShelterViewBuilder.ShiftsLeft(24, -1.4));
            Assert.AreEqual(3, ShelterViewBuilder.ShiftsLeft(4.2, -1.4), "tam bölünen: 3");
            Assert.AreEqual(0, ShelterViewBuilder.ShiftsLeft(0, -1.4));
            Assert.IsNull(ShelterViewBuilder.ShiftsLeft(10, 0), "fark ≥ 0: artıyor");
            Assert.IsNull(ShelterViewBuilder.ShiftsLeft(10, 0.7));

            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            sim.Shelter.Supplies = 4;
            var low = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.AreEqual(Loc.F("{0} VARDİYA", 2), low.SuppliesBig);
            Assert.IsTrue(low.SuppliesLow);
            Assert.AreEqual(Loc.T("AZ KALDI"), low.SuppliesStamp);
            sim.Shelter.Room(ShelterLogic.Living).Workers = 2;
            sim.Shelter.Population = 2;
            var rising = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.AreEqual(Loc.T("ERZAK"), rising.SuppliesLabel);
            Assert.AreEqual(Loc.T("ARTIYOR"), rising.SuppliesBig);
            Assert.AreEqual(Loc.T("ERZAK ARTIYOR"), ShelterViewBuilder.Store(sim).Slate);
        }

        /// <summary>DEPO (S3, Soru 6 ara kararı a): 10 sandık = erzak sınırı, 5 kova = malzeme deposu, plaka rafı 3 yuva.</summary>
        [Test]
        public void Store_CountsCratesBinsAndPlates()
        {
            // V.014: erzak 22/30 → sandık 3: 7 dolu + yarım; malzeme 19/60 → kova 12: 2 kova, sonuncusu 7/12; plaka 0/3
            var d = ShelterViewBuilder.Store(22, 30, 19, 60, 0, 3);
            Assert.AreEqual(3, d.CrateValue, 1e-9);
            Assert.AreEqual(7, d.Crates);
            Assert.IsTrue(d.CratePartial);
            Assert.AreEqual(12, d.BinValue, 1e-9);
            Assert.AreEqual(2, d.Bins);
            Assert.AreEqual(7.0 / 12, d.LastBinFill, 1e-9);
            Assert.AreEqual(0, d.Plates);
            Assert.AreEqual(3, d.PlateCapacity);

            var full = ShelterViewBuilder.Store(30, 30, 60, 60, 3, 3);
            Assert.AreEqual(10, full.Crates);
            Assert.IsFalse(full.CratePartial);
            Assert.AreEqual(5, full.Bins);
            Assert.AreEqual(1, full.LastBinFill, 1e-9);
            Assert.AreEqual(3, full.Plates);

            // Yaşam alanı SV2: sınır 60 → sandık 6; atölye SV1: rafın bir yuvası açık
            var big = ShelterViewBuilder.Store(24, 60, 0, 60, 2, 1);
            Assert.AreEqual(6, big.CrateValue, 1e-9);
            Assert.AreEqual(4, big.Crates);
            Assert.IsFalse(big.CratePartial);
            Assert.AreEqual(0, big.Bins);
            Assert.AreEqual(1, big.PlateCapacity);
            Assert.AreEqual(1, big.Plates, "plaka açık yuvadan fazla gösterilmez");

            var exact = ShelterViewBuilder.Store(24, 30, 12, 60, 0, 0);
            Assert.AreEqual(8, exact.Crates);
            Assert.IsFalse(exact.CratePartial);
            Assert.AreEqual(1, exact.Bins);
            Assert.AreEqual(1, exact.LastBinFill, 1e-9);
        }

        [Test]
        public void Workshop_StopsWhenStorageFullOrNoMaterials()
        {
            var sim = NewLiberation();
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            sim.Shelter.Plates = ShelterLogic.PlateCapacity(sim.Shelter, sim.B);
            Assert.AreEqual(RoomCondition.Stopped, Room(sim, ShelterLogic.Workshop).Condition);
            Assert.AreEqual(Loc.F("Depo dolu: {0} plaka hazır. Kırılan plakanın yerine yenisi dökülür; geliştirirsen depo büyür.", 1),
                ShelterViewBuilder.Board(sim, ShelterLogic.Workshop).Note);
            sim.Shelter.Plates = 0; sim.Shelter.Materials = 1;
            Assert.AreEqual(RoomCondition.Stopped, Room(sim, ShelterLogic.Workshop).Condition);
            Assert.AreEqual(Loc.F("Malzeme yetmiyor: plaka {0} malzeme ister. İkmal baskını malzeme getirir.", "4"), ShelterViewBuilder.Board(sim, ShelterLogic.Workshop).Note);
        }

        [Test]
        public void Board_MissingMaterials_ShortWithNumbers()
        {
            var sim = NewLiberation();
            sim.Shelter.Materials = 5;
            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Infirmary);
            var cost = ShelterLogic.UpgradeCost(sim.Shelter, ShelterLogic.Infirmary, sim.B);
            // S3: yetersiz kaynakta düğme açık kalır, eksik kalem çizik + “yetersiz”; teleks gerekçesi sayılarla
            Assert.AreEqual(SlipButtonState.Short, d.Action.State);
            Assert.AreEqual(Loc.F("{0} · {1} + {2} · yetersiz", Loc.T("ONAR"), Loc.F("{0} fon", DeskFormat.Number(cost.Fon, 0)),
                "<s>" + Loc.F("{0} malzeme", DeskFormat.Number(cost.Materials, 0)) + "</s>"), d.Action.Label, "hasarlı oda: ONAR");
            Assert.AreEqual(TelexReasons.Upgrade(sim, ShelterLogic.Infirmary), d.Action.Hover);
            Assert.IsNull(d.ActionNote);
            Assert.AreEqual(Loc.T("Hasarlı: onarılana kadar çalışmaz, personel alamaz."), d.Note);
            Assert.AreEqual(SlipButtonState.Disabled, d.Plus.State, "hasarlı oda personel alamaz");
        }

        [Test]
        public void Board_TopLevelAndFullRoom()
        {
            var sim = NewLiberation();
            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.AreEqual(SlipButtonState.Disabled, d.Plus.State, "yaşam alanı 2/2: oda dolu");
            Assert.AreEqual(Loc.T("Oda dolu"), d.Plus.Hover);
            Assert.AreEqual(SlipButtonState.Normal, d.Minus.State);
            Assert.AreEqual(Loc.F("{0}/{1}", 2, 2), d.Staff);
            Assert.IsNull(d.StaffNote);

            sim.Shelter.Room(ShelterLogic.Living).Level = sim.B.shelterMaxRoomLevel;
            var top = ShelterViewBuilder.Board(sim, ShelterLogic.Living);
            Assert.IsTrue(top.Top, "en üst seviyede düğme yerine EN ÜST SEVİYE damgası");
        }

        /// <summary>S3 kabulü: boşta kimse yokken [+] devre dışı ve “Boşta kimse yok”; görevli yokken [−] devre dışı.</summary>
        [Test]
        public void Board_Stepper_NoIdleAndNoStaff()
        {
            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            var radio = sim.Shelter.Room(ShelterLogic.Radio);
            Assert.AreEqual(0, radio.Workers);
            // Boşta herkesi atölyeye ver: telsiz odasına verecek kimse kalmaz
            var workshop = sim.Shelter.Room(ShelterLogic.Workshop);
            workshop.Workers += ShelterLogic.AvailableWorkers(sim.Shelter);
            Assert.AreEqual(0, ShelterLogic.AvailableWorkers(sim.Shelter));
            var d = ShelterViewBuilder.Board(sim, ShelterLogic.Radio);
            Assert.AreEqual(SlipButtonState.Disabled, d.Plus.State);
            Assert.AreEqual(Loc.T("Boşta kimse yok"), d.StaffNote);
            Assert.AreEqual(Loc.T("Boşta kimse yok"), d.Plus.Hover);
            Assert.AreEqual(SlipButtonState.Disabled, d.Minus.State);
            Assert.AreEqual(Loc.T("Görevli yok"), d.Minus.Hover);
        }

        /// <summary>S3 › odaların çıktısı: ranzada yedek, telsizde görevliyle kadran, revirde yatan uzman.</summary>
        [Test]
        public void Rooms_ShowOutputs()
        {
            var sim = NewLiberation();
            SiginakFixtures.ApplyShelterV014(sim);
            Assert.AreEqual(sim.Shelter.Reserves.Count(r => r.Down <= 0), Room(sim, ShelterLogic.Living).Occupants);
            Assert.IsTrue(Room(sim, ShelterLogic.Living).Active, "görevli var: erzak kasası");
            Assert.IsFalse(Room(sim, ShelterLogic.Radio).Active, "telsizde görevli yok");
            sim.Shelter.Room(ShelterLogic.Radio).Workers = 1;
            Assert.IsTrue(Room(sim, ShelterLogic.Radio).Active);
            Assert.AreEqual(0, Room(sim, ShelterLogic.Infirmary).Occupants, "hasarlı revirde hasta yok");
            sim.Shelter.Room(ShelterLogic.Infirmary).Level = 1;
            sim.S.Teams[0].Members[0].Down = 2;
            Assert.AreEqual(1, Room(sim, ShelterLogic.Infirmary).Occupants);
        }

        [Test]
        public void Upgrade_StampsRoomForOneShift()
        {
            var sim = NewLiberation();
            Assert.IsFalse(Room(sim, ShelterLogic.Workshop).Fresh);
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok, "onarım (seviye 1) bölge istemez");
            Assert.IsTrue(Room(sim, ShelterLogic.Workshop).Fresh, "EMİR UYGULANDI damgası");
            Assert.IsFalse(Room(sim, ShelterLogic.Living).Fresh);
            sim.Step();
            Assert.IsFalse(Room(sim, ShelterLogic.Workshop).Fresh, "bir vardiya sonra kalkar");
        }
    }
}
