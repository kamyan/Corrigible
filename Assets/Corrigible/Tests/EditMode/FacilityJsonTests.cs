using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis baskınının kaydı, JsonUtility gidiş-dönüşüyle (aşama 2; yalnız Unity, Mono koşum takımında koşmaz): baskının
    /// ortasındaki durum (<see cref="RaidState.Reports"/>) ve baskın günlüğünün oda raporları (<see cref="RaidLogEntry.Rooms"/>)
    /// diske yazılıp okunduktan sonra yeniden oynatılan oyunla aynı.
    /// </summary>
    public class FacilityJsonTests
    {
        static string Json(object o) => UnityEngine.JsonUtility.ToJson(o);
        static GameRecord RoundTrip(GameRecord rec) => UnityEngine.JsonUtility.FromJson<GameRecord>(Json(rec));

        [Test]
        public void Json_MidRaid_RaidStateRoundTrips()
        {
            var sim = Sim();
            var G = sim.OpenRaidRoom();
            Calm(G); ToGate(G); Assert.IsTrue(Baskin.Pass(G));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(G)).Ok);
            Assert.AreEqual(new RoomStep(2, false), sim.RaidRoom.Value);
            sim.Record.day = sim.S.Day;
            var rec = RoundTrip(sim.Record);
            Assert.AreEqual(FacilityMaps.FingerprintFor(sim.B), rec.facility);
            var restored = Replay.Restore(rec);
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.AreEqual(Json(sim.S.Raid), Json(restored.S.Raid));
            var copy = UnityEngine.JsonUtility.FromJson<RaidState>(Json(sim.S.Raid));
            Assert.AreEqual(3, copy.Params.Rooms.Count);
            Assert.AreEqual(sim.S.Raid.Reports[0].Encode(), copy.Reports[0].Encode());
            Assert.AreEqual(Baskin.Signature(sim.OpenRaidRoom()), Baskin.Signature(restored.OpenRaidRoom()));
        }

        /// <summary>Aşama 10: önlem defterinin listeleri yeni oyunda boş (olaylı ve varsayılan denge), dolu hâli ve emrin kopyası
        /// (<see cref="RaidParams.Counters"/>, <see cref="RaidParams.Answers"/>) JsonUtility'den aynen döner.</summary>
        [Test]
        public void Json_CounterState_EmptyAtStart_RoundTrips()
        {
            foreach (var b in new[] { Bal(), new Balance { shelterEventsEnabled = false } })
            {
                string fresh = Json(Simulation.NewGame(79, b).S.Liberation.Facility);
                StringAssert.Contains("\"Tally\":[]", fresh);
                StringAssert.Contains("\"Counter\":[]", fresh);
                StringAssert.Contains("\"Learned\":[]", fresh);
            }
            var f = new FacilityState();
            f.Tally.AddRange(new[] { 0, 2, 0, 0, 6, 1 });
            f.Counter.AddRange(new[] { 1, 0, 0, 0, 0, 2 });
            f.Learned.Add(new FacilityCounterEntry { Day = 14, Region = "weu", Class = FacilityCounters.Push, Level = 1 });
            f.Learned.Add(new FacilityCounterEntry { Day = 30, Region = "af", Class = FacilityCounters.Gadget, Level = 2 });
            var copy = UnityEngine.JsonUtility.FromJson<FacilityState>(Json(f));
            Assert.AreEqual(CounterLine(f), CounterLine(copy));
            var p = new RaidParams { Counters = new List<int> { 1, 0, 0, 0, 0, 2 }, Answers = new List<string> { FacilityGear.AnsSokum } };
            var q = UnityEngine.JsonUtility.FromJson<RaidParams>(Json(p));
            CollectionAssert.AreEqual(p.Counters, q.Counters);
            CollectionAssert.AreEqual(p.Answers, q.Answers);
        }

        [Test]
        public void Json_RaidLog_RoomsRoundTrip()
        {
            var sim = Sim();
            var reps = PlayRaid(sim);
            Assert.AreEqual(reps.Count, sim.S.RaidLog.Last().Rooms.Count);
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(RoundTrip(sim.Record));
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.AreEqual(sim.S.RaidLog.Count, restored.S.RaidLog.Count);
            for (int i = 0; i < sim.S.RaidLog.Count; i++) Assert.AreEqual(Json(sim.S.RaidLog[i]), Json(restored.S.RaidLog[i]));
            CollectionAssert.AreEqual(sim.S.RaidLog.Last().Rooms, restored.S.RaidLog.Last().Rooms);
        }

        /// <summary>Aşama 11: kalıcı tahta, kuşatma ve onarım günlükleri yeni oyunda boş (olaylı ve varsayılan denge), emrin kipi ve iz kopyası,
        /// günlüğün kipi boş başlar; dolu hâlleri JsonUtility'den aynen döner (boş sonuç "" kalır, null olmaz).</summary>
        [Test]
        public void Json_Stage11State_EmptyAtStart_RoundTrips()
        {
            foreach (var b in new[] { Bal(), new Balance { shelterEventsEnabled = false } })
            {
                string fresh = Json(Simulation.NewGame(79, b).S.Liberation.Facility);
                StringAssert.Contains("\"Marks\":[]", fresh);
                StringAssert.Contains("\"Sieges\":[]", fresh);
                StringAssert.Contains("\"Repairs\":[]", fresh);
            }
            string order = Json(new RaidParams());
            StringAssert.Contains("\"Mode\":\"\"", order);
            StringAssert.Contains("\"Marks\":[]", order);
            StringAssert.Contains("\"Mode\":\"\"", Json(new RaidLogEntry()));
            var f = new FacilityState();
            f.Marks.Add(new FacilityMark { Room = "weu_1.1", Kind = FacilityMarks.Wire, X = 3, Y = 4, Hp = 2, Max = 3, Day = 7 });
            f.Marks.Add(new FacilityMark { Room = "weu_1.2", Kind = FacilityMarks.Wall, X = 0, Y = 5, Day = 7 });
            f.Repairs.Add(new FacilityMark { Room = "weu_2.1", Kind = FacilityMarks.Device, X = 6, Y = 2, Day = 9 });
            f.Sieges.Add(new FacilitySiegeEntry { Sector = "weu_1", Region = "weu", Day = 7, Due = 8, Outcome = "tutuldu", Closed = 8, Abandoned = 1 });
            f.Sieges.Add(new FacilitySiegeEntry { Sector = "weu_2", Region = "weu", Day = 9, Due = 9 });
            var copy = UnityEngine.JsonUtility.FromJson<FacilityState>(Json(f));
            Assert.AreEqual(MarkLine(f), MarkLine(copy));
            Assert.AreEqual(Json(f), Json(copy));
            Assert.AreEqual("", copy.Sieges[1].Outcome, "bekleyen kuşatma boş sonuçla döner");
            Assert.AreEqual(-1, copy.Sieges[1].Closed);
            var p = new RaidParams { Mode = "defense", Marks = new List<FacilityMark> { f.Marks[0] } };
            var q = UnityEngine.JsonUtility.FromJson<RaidParams>(Json(p));
            Assert.AreEqual("defense", q.Mode);
            Assert.AreEqual(MarkKey(p.Marks[0]), MarkKey(q.Marks[0]));
            Assert.AreEqual("defense", UnityEngine.JsonUtility.FromJson<RaidLogEntry>(Json(new RaidLogEntry { Mode = "defense" })).Mode);
        }

        /// <summary>Aşama 12: <see cref="RaidResult.Overboard"/> ve oda raporunun Katar kuyruğu (<see cref="RoomReport.Follow"/>,
        /// <see cref="RoomReport.Decouple"/>, kişi durumu "k") JsonUtility'den aynen döner; Katar'lı harekâtın kaydı diske yazılıp okunduktan
        /// sonra aynı oyuna döner.</summary>
        [Test]
        public void Json_KatarResultAndReportTail_RoundTrip()
        {
            var res = new RaidResult { Success = true, Hp = { 2, 0, 3 }, Armor = { 0, 0, 0 }, Down = 1, Overboard = { 1 } };
            var back = UnityEngine.JsonUtility.FromJson<RaidResult>(Json(res));
            CollectionAssert.AreEqual(new[] { 1 }, back.Overboard);
            Assert.AreEqual(res.Encode(), back.Encode());
            StringAssert.Contains("\"Overboard\":[]", Json(new RaidResult()), "boş liste");
            var rep = new RoomReport { Room = 3, End = "pass", Team = { new RoomMember { State = "k", Fell = 2, X = 3, Y = 0 } },
                Machines = { new RoomMachine { Type = "dron", X = 1, Y = 3, Hp = 3 } }, Follow = { 0 } };
            var r2 = UnityEngine.JsonUtility.FromJson<RoomReport>(Json(rep));
            Assert.AreEqual(rep.Encode(), r2.Encode());
            CollectionAssert.AreEqual(new[] { 0 }, r2.Follow);
            Assert.AreEqual("k", r2.Team[0].State);
            var dec = new RoomReport { Room = 2, Return = true, End = "pass", Decouple = true };
            Assert.IsTrue(UnityEngine.JsonUtility.FromJson<RoomReport>(Json(dec)).Decouple);

            var sim = SimAt(79, "na");
            FacilityConditionCampaignTests.Order(sim);
            RaidState mid = null;
            FacilityConditionCampaignTests.KatarRaid(sim, onFollowRoom: s => mid = UnityEngine.JsonUtility.FromJson<RaidState>(Json(s.S.Raid)));
            Assert.IsTrue(mid.Reports.Any(x => x.Follow.Count > 0), "takip raporu baskının ortasındaki durumda");
            Assert.IsTrue(mid.Reports.Any(x => x.Team.Any(m => m.State == "k")));
            sim.Record.day = sim.S.Day;
            var rec = RoundTrip(sim.Record);
            Assert.AreEqual(FacilityMaps.FingerprintFor(sim.B), rec.facility);
            var restored = Replay.Restore(rec);
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.AreEqual(State(sim), State(restored));
            Assert.AreEqual(Json(sim.S.RaidLog.Last()), Json(restored.S.RaidLog.Last()));
        }

        /// <summary>Aşama 11: iki yeni komutlu harekâtın kaydı diske yazılıp okunduktan sonra aynı oyuna döner (kuşatma günlüğü, izler,
        /// onarım, baskın günlüğünün kipi).</summary>
        [Test]
        public void Json_SiegeCampaign_RecordRoundTrips()
        {
            var b = Bal();
            b.liberationAlarmThreshold = 2;
            var sim = ShelterEventKit.New(b, 79);
            CaptureNext(sim);
            for (int i = 0; i < 4 && !sim.SiegeDecisionDue; i++) ShelterEventKit.Close(sim);
            Defend(sim);
            PlayDefense(sim, 1, true);
            for (int i = 0; i < 4 && !sim.SiegeDecisionDue; i++) ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.YieldSector().Ok);
            ShelterEventKit.Close(sim);
            sim.Record.day = sim.S.Day;
            var rec = RoundTrip(sim.Record);
            Assert.AreEqual(FacilityMaps.FingerprintFor(sim.B), rec.facility);
            var restored = Replay.Restore(rec);
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.AreEqual(State(sim), State(restored));
            Assert.AreEqual(Json(sim.S.Liberation.Facility), Json(restored.S.Liberation.Facility));
            for (int i = 0; i < sim.S.RaidLog.Count; i++) Assert.AreEqual(Json(sim.S.RaidLog[i]), Json(restored.S.RaidLog[i]));
            Assert.IsTrue(sim.S.RaidLog.Any(l => l.Mode == "defense"));
        }
    }
}
