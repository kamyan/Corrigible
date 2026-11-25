using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis baskınının sunum oturumu (aşama 3; FacilityRaidSession): yalnız tesis baskınında kurulur, ilk oda giriş odası, rapor
    /// dosyalanınca raid_room kaydedilir ve sıradaki oda açılır, kayıttan devamda içinde bulunulan oda baştan aynı tahtayla açılır,
    /// reddedilen rapor hiçbir şeyi değiştirmez, sonuç ve kırılan sivil toplamı raporlardan. Saf sınıf; sahne açmaz.
    /// </summary>
    public class FacilityRaidSessionTests
    {
        [Test]
        public void Applies_WhenARaidRuns()
        {
            var sim = FacilityDeskKit.New();
            Assert.IsFalse(FacilityRaidSession.Applies(sim), "emir yok");
            FacilityDeskKit.StartFirst(sim);
            Assert.IsTrue(FacilityRaidSession.Applies(sim));
            Assert.IsFalse(FacilityRaidSession.Applies(null));
        }

        [Test]
        public void Open_FirstRoomIsEntry()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            var session = new FacilityRaidSession(sim, null);
            Assert.IsFalse(session.Resumed);
            var B = session.Open();
            Assert.AreEqual(1, session.Step.Room);
            Assert.IsFalse(session.Step.Return);
            Assert.AreEqual(RoomType.Entry, B.Room.Type);
            Assert.AreEqual(0, session.CollateralBefore);
            Assert.IsFalse(session.Closed);
            Assert.IsNull(session.Result());
            Assert.IsNull(session.Preview(B), "oda sürüyor");
            Assert.IsNull(session.Member(0), "rapor yok");
        }

        [Test]
        public void FileAdvancesAndRecords()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            var session = new FacilityRaidSession(sim, null);
            var B = session.Open();
            FacilityDeskKit.Calm(B);
            FacilityDeskKit.ToGateCore(B);
            Assert.IsTrue(Baskin.Pass(B));
            var report = session.Preview(B);
            Assert.AreEqual("pass", report.End);
            Assert.IsFalse(report.Last);
            Assert.IsTrue(session.File(report).Ok);
            Assert.AreEqual("raid_room", sim.Record.commands.Last().kind);
            Assert.AreEqual(2, session.NextStep.Value.Room);
            Assert.AreEqual(1, session.Filed.Count);
            Assert.AreEqual("a", session.Member(0).State);
            var next = session.Open();
            Assert.AreEqual(2, next.Room.Index);
            Assert.AreEqual(report.Collateral, session.CollateralBefore);
        }

        [Test]
        public void Resume_ReopensCurrentRoomFromStart()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            var live = new FacilityRaidSession(sim, null);
            var B = live.Open();
            FacilityDeskKit.Calm(B);
            FacilityDeskKit.ToGateCore(B);
            Assert.IsTrue(Baskin.Pass(B));
            Assert.IsTrue(live.File(live.Preview(B)).Ok);
            string expected = Baskin.Signature(live.Open());

            var again = Replay.Restore(sim.Record);
            var resumed = new FacilityRaidSession(again, null);
            Assert.IsTrue(resumed.Resumed);
            Assert.AreEqual(2, resumed.NextStep.Value.Room);
            Assert.AreEqual(expected, Baskin.Signature(resumed.Open()));
        }

        [Test]
        public void RejectedReport_DoesNotAdvance()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            var session = new FacilityRaidSession(sim, null);
            var B = session.Open();
            FacilityDeskKit.Calm(B);
            FacilityDeskKit.ToGateCore(B);
            Assert.IsTrue(Baskin.Pass(B));
            var report = session.Preview(B);
            report.Room = 3;
            int commands = sim.Record.commands.Count;
            Assert.IsFalse(session.File(report).Ok);
            Assert.AreEqual(0, session.Filed.Count);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(1, session.NextStep.Value.Room);
        }

        /// <summary>Kanadın tamamı (ara odada bir makine kalır: dönüşte yeniden oynanır, giriş odası yürünür); sonuç raporların toplamı.</summary>
        [Test]
        public void WholeWing_ResultSumsTheReports()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            var session = new FacilityRaidSession(sim, null);
            var rooms = new System.Collections.Generic.List<string>();
            for (int guard = 0; guard < 6 && !session.Closed; guard++)
            {
                var B = session.Open();
                Assert.AreEqual(session.Filed.Sum(r => r.Collateral), session.CollateralBefore);
                rooms.Add(B.Room.Index + (session.Step.Return ? "d" : ""));
                FacilityDeskKit.PlayRoom(B, B.Room.Type == RoomType.Middle && !B.Escape ? 1 : 0);
                Assert.IsTrue(session.File(session.Preview(B)).Ok);
            }
            Assert.IsTrue(session.Closed, string.Join(" ", rooms));
            CollectionAssert.AreEqual(new[] { "1", "2", "3", "2d" }, rooms);
            CollectionAssert.AreEqual(new[] { 1 }, FacilityRaidSession.Skipped(session.Filed.Last(), session.NextStep));
            var result = session.Result();
            Assert.IsTrue(result.Success);
            Assert.AreEqual(session.Filed.Sum(r => r.Turns), result.Turns);
            Assert.IsNull(session.NextStep);
            Assert.Throws<System.InvalidOperationException>(() => session.Open());
        }
    }
}
