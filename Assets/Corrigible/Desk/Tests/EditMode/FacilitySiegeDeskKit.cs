using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 11 (P4a): kuşatma ve savunmanın masa testlerinin ortak kurulumu. Kartlı tesis oyunu (tohum 79), alarm her vardiya
    /// eşik kadar artar (ilk kapanış eşiğe varır; bellek dengesi). İlk kanat komutlarla kesin alınır ve vardiya kapanır: Core kapanışta
    /// kuşatmayı kurar, yeni vardiya karar vardiyasıdır. Savunma yalnız komut ve tahtayla oynanır (Core <c>FacilityKit.PlayDefense</c>'in
    /// public karşılığı). Sayılar Core okuyucularından okunur (P3'ün olası savunma ayarı testleri bozmasın).
    /// </summary>
    public static class FacilitySiegeDeskKit
    {
        /// <summary>Tesis açık, olaylar kapalı, alarm her vardiya eşik kadar (savunma ve iz alt bayrakları C# varsayılanı: açık).</summary>
        public static Balance Bal()
        {
            var b = FacilityDeskKit.Bal();
            b.liberationAlarmPerShift = b.liberationAlarmThreshold;
            return b;
        }

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        /// <summary>Vardiya kapanışı (board_print + board_file).</summary>
        public static void Close(Simulation sim)
        {
            Ok(sim.PrintShift(), "board_print");
            Ok(sim.FileShift(sim.Board.ReceiptId), "board_file");
        }

        /// <summary>İlk kanadı alır ve vardiyayı kapatır; kapanışta kurulan kuşatmanın kanadı döner (karar vardiyası bugün).</summary>
        public static LiberationSectorState Sieged(Simulation sim)
        {
            string id = FacilityDeskKit.StartFirst(sim);
            FacilityDeskKit.WinRaid(sim);
            Close(sim);
            Assert.IsTrue(sim.DefenseOn, "savunma açık");
            Assert.IsNotNull(sim.PendingSiege, "kapanışta kuşatma kurulmadı");
            Assert.AreEqual(id, sim.PendingSiege.Sector, "son alınan kanat kuşatılır");
            Assert.IsTrue(sim.SiegeDecisionDue, "kapanışta kurulan kuşatmanın karar vardiyası ertesi vardiya (şimdi)");
            return sim.LiberationSector(id);
        }

        /// <summary>Yeni oyun ve kuşatma.</summary>
        public static (Simulation sim, LiberationSectorState sector) NewSieged(uint seed = 79, System.Action<Balance> tune = null)
        {
            var b = Bal();
            tune?.Invoke(b);
            var sim = Simulation.NewGame(seed, b);
            return (sim, Sieged(sim));
        }

        static bool Fit(Simulation sim, ResidentState p) => p.Alive && p.Down <= 0 && p.Hp > 0 && !p.Restricted && !p.Isolated
            && sim.S.Day >= sim.Admission.File(p.Id).IdleUntil;

        /// <summary>Üç saha rolünü sağlıklı kişilerle ve kitleriyle doldurur (eksik kit doğrudan eklenir; test kurulumu, kayda girmez).</summary>
        public static void FillSquad(Simulation sim)
        {
            foreach (string role in PlanningBoardData.FieldJobs)
            {
                var person = sim.Board.People.FirstOrDefault(p => Fit(sim, p) && p.Job == role);
                if (person == null)
                {
                    person = sim.Board.People.First(p => Fit(sim, p) && !PlanningBoardData.FieldJobs.Contains(p.Job));
                    Ok(sim.AssignResident(person.Id, role), "rol " + role);
                }
                if (!sim.Board.Equipment.Any(e => e.OwnerId == person.Id && e.Kind == "kit_" + role && e.Charges > 0))
                    Assert.IsNotNull(sim.AddBoardEquipment("kit_" + role, person.Id, true));
            }
        }

        /// <summary>Kadro hazır değilse doldurur ve savunma emrini verir.</summary>
        public static void Defend(Simulation sim)
        {
            if (sim.DefendSectorCheck() != null) FillSquad(sim);
            Ok(sim.DefendSector(), "siege_defend");
            Assert.IsTrue(Simulation.IsDefense(sim.S.Raid.Params), "savunma emri");
        }

        /// <summary>
        /// Süren savunmayı komut ve tahtayla oynar: ilk <paramref name="abandon"/> oda içeri çekilerek terk edilir (kapı + GEÇ); sonraki odada
        /// <paramref name="hold"/> ise süre dolana kadar tutulur (her tur sonu makineler kalkar), değilse geri çekilinir (kayıp). Sonucu yazar.
        /// </summary>
        public static void PlayDefense(Simulation sim, int abandon = 0, bool hold = true)
        {
            for (int guard = 0; guard < 4 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                Assert.IsTrue(Baskin.IsDefense(B), "savunma tahtası");
                FacilityDeskKit.Calm(B);
                if (B.Room.Index <= abandon)
                {
                    FacilityDeskKit.ToGateCore(B);
                    Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
                }
                else if (!hold) Assert.IsTrue(Baskin.Retreat(B), "geri çekilme: " + B.Room.Id);
                else
                    for (int t = 0; t < 40 && B.Phase == "player"; t++)
                    {
                        Assert.IsTrue(Baskin.EndTurn(B), "EndTurn t" + B.Turn);
                        FacilityDeskKit.Calm(B);
                    }
                Ok(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)), "raid_room");
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "savunmanın oda zinciri bitmedi");
            Ok(sim.ResolveRaid(sim.FacilityRaidResult()), "raidres");
            Assert.AreEqual(Simulation.DefenseMode, sim.S.RaidLog.Last().Mode);
        }
    }
}
