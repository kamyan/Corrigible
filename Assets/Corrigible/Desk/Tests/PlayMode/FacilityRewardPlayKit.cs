using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 9, parça 3: sunucu ödülünün PlayMode kurulumu. Canlı oyunun simülasyonunda bölgeyi komutlarla tamamlar (EditMode
    /// <c>FacilityDeskKit.CompleteRegion</c>'un karşılığı; test derlemeleri ayrı): baskın emri, odalar betikli kazanılır (makineler kalkar,
    /// hedef odasında kaçış, ekip kapıdan geçer, <c>AdvanceRaidRoom</c>), <c>ResolveRaid</c>, vardiya kapanışı. Komutlar doğrudan
    /// simülasyona verilir (sahne yüklenmez: <c>Changed</c> yayılmaz, baskın başlatıcısı tetiklenmez) ve kayda yazılır; kayıttan devam
    /// aynı oyunu kurar.
    /// </summary>
    public static class FacilityRewardPlayKit
    {
        /// <summary>Olaylar kapalı, alarm artmaz (karşı saldırı kurulumu bozmasın).</summary>
        public static void Tune(Balance b)
        {
            b.shelterEventsEnabled = false;
            b.liberationAlarmPerShift = 0;
        }

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        static void Calm(Baskin.BState B)
        {
            foreach (var e in B.Units.Where(u => u.Side == 'e' && !u.Dead)) { e.Dead = true; e.Intent = null; }
            B.Spawns.Clear();
            B.SpawnPerTurn = 0;
        }

        static void ToGate(Baskin.BState B)
        {
            foreach (var u in B.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).OrderBy(u => u.Slot).ToList())
            {
                Baskin.Pt? free = null;
                foreach (var g in Baskin.GateTiles(B))
                {
                    var o = Baskin.UnitAt(B, g.X, g.Y);
                    if (o == null || o == u) { free = g; break; }
                }
                Assert.IsTrue(free.HasValue, "boş kapı karesi yok: " + B.Room?.Id);
                u.X = free.Value.X;
                u.Y = free.Value.Y;
                Assert.IsTrue(Baskin.EnterGate(B, u.Id), "EnterGate: " + B.Room?.Id);
            }
        }

        static void StartEscape(Baskin.BState B)
        {
            foreach (var t in Baskin.TargetsLeft(B)) Baskin.CellAt(B, t.X, t.Y).O = null;
            if (Baskin.IsLoot(B))
            {
                var team = B.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).OrderBy(u => u.Slot).ToList();
                B.Carrier = (team.FirstOrDefault(u => u.Type == "tek") ?? team.First()).Id;
            }
            B.Escape = true;
        }

        /// <summary>Süren tesis baskınını komutlarla kesin kazanır. <paramref name="onRoom"/> (aşama 10): her oda açılınca, makineler
        /// kalkmadan önce (ör. gerçek bir koşu darbesi; sayaç oda raporuna yazılır).</summary>
        public static void WinRaid(Simulation sim, System.Action<Baskin.BState> onRoom = null)
        {
            for (int guard = 0; guard < 6 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                onRoom?.Invoke(B);
                Calm(B);
                if (B.Room.Type == RoomType.Target && !B.Escape) StartEscape(B);
                ToGate(B);
                if (B.Phase == "player") Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
                Ok(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)), "raid_room");
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            Ok(sim.ResolveRaid(sim.FacilityRaidResult()), "raidres");
            Assert.IsTrue(sim.S.RaidLog.Last().Success, "kazanılmalı");
        }

        /// <summary>Vardiya kapanışı (board_print + board_file).</summary>
        public static void Close(Simulation sim)
        {
            Ok(sim.PrintShift(), "board_print");
            Ok(sim.FileShift(sim.Board.ReceiptId), "board_file");
        }

        /// <summary>
        /// Bulunulan bölgeyi tamamlar (bölge bittiyse sınırdan ilk bölge seçilir; dönen ekip için önce kapanış). Bölgeyi tamamlayan
        /// baskından sonra kapanış yapılmaz: bekleyen ödül ve okunmamış rapor açıkta kalır. Bölge kimliği döner.
        /// </summary>
        public static string CompleteRegion(Simulation sim, System.Action<Baskin.BState> onRoom = null)
        {
            var lib = sim.S.Liberation;
            if (sim.LiberationRegion(lib.CurrentRegion).Completed)
            {
                Ok(sim.SelectLiberationRegion(sim.LiberationFrontier.First()), "yeni cephe");
                if (sim.S.RaidLog.Count > 0 && sim.S.RaidLog.Last().Day == sim.S.Day) Close(sim);
            }
            string region = lib.CurrentRegion;
            for (int guard = 0; guard < 6; guard++)
            {
                var sector = sim.NextLiberationSector(region);
                Ok(sim.StartLiberationRaid(sector.Id), "baskın emri " + sector.Id);
                WinRaid(sim, onRoom);
                if (sim.LiberationRegion(region).Completed) return region;
                Close(sim);
            }
            Assert.Fail("bölge tamamlanmadı: " + region);
            return region;
        }
    }
}
