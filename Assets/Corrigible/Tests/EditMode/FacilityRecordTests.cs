using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis baskınının kaydı (aşama 2), JsonUtility'siz: oda raporunun kodlanması, raid_room komutunun denetimi, sonucun
    /// raporlardan yazılması, kayıt sırası, baskının ortasında kaydet-devam, tam harekâtın yeniden oynatılması, sürüm 19 ve tesis
    /// parmak izi. JSON gidiş-dönüşlü iki test <see cref="FacilityJsonTests"/>'te (yalnız Unity).
    /// </summary>
    public class FacilityRecordTests
    {
        static RoomReport Full() => new RoomReport
        {
            Room = 3, Return = false, Turns = 7, End = "won", Last = true, Success = true, Retreat = false, Cargo = -2, Collateral = 2,
            Civilians = 1, Device = "",
            Team =
            {
                new RoomMember { State = "a", Hp = 3, Armor = 1, Emp = 2, Charges = -1, Fell = 0, X = 5, Y = 2 },
                new RoomMember { State = "d", Hp = 0, Armor = 0, Emp = 0, Charges = 1, Fell = 1, X = 4, Y = 4 },
                new RoomMember { State = "l", Hp = 0, Armor = 1, Emp = 0, Charges = 0, Fell = 3, X = 0, Y = 7 },
            },
            Machines = { new RoomMachine { Type = "dron", X = 5, Y = 2, Hp = 2, Armor = 0 }, new RoomMachine { Type = "kalkan", X = 6, Y = 4, Hp = 1, Armor = 1 } },
            Objects = { new RoomObject { K = "civ", X = 1, Y = 3, Hp = 2, Max = 2 }, new RoomObject { K = "spawner", X = 6, Y = 6, Hp = 1, Max = 2 } },
            Rubble = { 19, 27 },
        };

        static string Fields(RoomReport r) =>
            $"{r.Room}|{r.Return}|{r.Turns}|{r.End}|{r.Last}|{r.Success}|{r.Retreat}|{r.Cargo}|{r.Collateral}|{r.Civilians}|{r.Device}|"
            + string.Join(";", r.Team.Select(m => $"{m.State},{m.Hp},{m.Armor},{m.Emp},{m.Charges},{m.Fell},{m.X},{m.Y}")) + "|"
            + string.Join(";", r.Machines.Select(m => $"{m.Type},{m.X},{m.Y},{m.Hp},{m.Armor}")) + "|"
            + string.Join(";", r.Objects.Select(o => $"{o.K},{o.X},{o.Y},{o.Hp},{o.Max}")) + "|" + string.Join(";", r.Rubble);

        // ---------- Rapor biçimi ----------
        [Test]
        public void Report_EncodeDecode_RoundTrip()
        {
            foreach (var r in new[] { Full(), new RoomReport(), new RoomReport { Room = 2, Return = true, End = "pass", Cargo = 0, Device = "anten" } })
            {
                string s = r.Encode();
                var d = RoomReport.Decode(s);
                Assert.AreEqual(s, d.Encode());
                Assert.AreEqual(Fields(r), Fields(d), s);
            }
            Assert.AreEqual("3|0|7|won|1|1|0|-2|2|1||a:3:1:2:-1:0:5:2,d:0:0:0:1:1:4:4,l:0:1:0:0:3:0:7|dron:5:2:2:0,kalkan:6:4:1:1|civ:1:3:2:2,spawner:6:6:1:2|19,27", Full().Encode());
            Assert.AreEqual("0|0|0|lost|0|0|0|-1|0|0|||||", new RoomReport().Encode());
        }

        [Test]
        public void Report_Decode_ShortTail_Defaults()
        {
            var r = RoomReport.Decode("2|1|4|pass|0|0|0|1|3|2|vana");
            Assert.AreEqual(2, r.Room); Assert.IsTrue(r.Return); Assert.AreEqual(4, r.Turns); Assert.AreEqual("pass", r.End);
            Assert.AreEqual(1, r.Cargo); Assert.AreEqual(3, r.Collateral); Assert.AreEqual(2, r.Civilians); Assert.AreEqual("vana", r.Device);
            Assert.AreEqual(0, r.Team.Count); Assert.AreEqual(0, r.Machines.Count); Assert.AreEqual(0, r.Objects.Count); Assert.AreEqual(0, r.Rubble.Count);
            var t = RoomReport.Decode("1|0|5");
            Assert.AreEqual("lost", t.End); Assert.AreEqual(-1, t.Cargo); Assert.AreEqual("", t.Device); Assert.IsFalse(t.Last);
            var m = RoomReport.Decode("1|0|5|pass|0|0|0|-1|0|0||a:3:1:0:2:0").Team.Single();
            Assert.AreEqual("a", m.State); Assert.AreEqual(3, m.Hp); Assert.AreEqual(1, m.Armor); Assert.AreEqual(2, m.Charges);
            Assert.AreEqual(0, m.Fell); Assert.AreEqual(-1, m.X); Assert.AreEqual(-1, m.Y);
            Assert.IsNull(RoomReport.Decode(null));
            Assert.IsNull(RoomReport.Decode(""));
        }

        // ---------- raid_room ----------
        static RoomReport Copy(RoomReport r) => RoomReport.Decode(r.Encode());

        [Test]
        public void AdvanceRaidRoom_Checks()
        {
            var idle = ShelterEventKit.New(Bal());
            var none = idle.AdvanceRaidRoom(Rep(1));
            Assert.IsFalse(none.Ok);
            Assert.AreEqual(Loc.T("Süren baskın yok."), none.Msg);
            Assert.IsFalse(idle.Record.commands.Any(c => c.kind == "raid_room"));

            var sim = Sim();
            int n = sim.Record.commands.Count;
            var G = sim.OpenRaidRoom();
            Calm(G);
            ToGate(G);
            Assert.IsTrue(Baskin.Pass(G));
            var good = sim.RaidRoomReport(G);
            void Refused(RoomReport r, string why)
            {
                var res = sim.AdvanceRaidRoom(r);
                Assert.IsFalse(res.Ok, why);
                Assert.AreEqual(Loc.T(why), res.Msg);
                Assert.AreEqual(n, sim.Record.commands.Count, why + ": kayda girmez");
                Assert.AreEqual(0, sim.S.Raid.Reports.Count, why);
            }
            Refused(null, "Oda raporu eksik.");
            var wrongRoom = Copy(good); wrongRoom.Room = 2; Refused(wrongRoom, "Rapor bu odanın değil.");
            var wrongWay = Copy(good); wrongWay.Return = true; Refused(wrongWay, "Rapor bu odanın değil.");
            var small = Copy(good); small.Team.RemoveAt(2); Refused(small, "Rapor kadroyla uyuşmuyor.");
            var odd = Copy(good); odd.End = "draw"; Refused(odd, "Raporun bitişi tanınmıyor.");
            var last = Copy(good); last.Last = true; Refused(last, "Raporun son oda bilgisi tutarsız.");
            var lostNotLast = Copy(good); lostNotLast.End = "lost"; Refused(lostNotLast, "Raporun son oda bilgisi tutarsız.");

            Assert.IsTrue(sim.AdvanceRaidRoom(good).Ok);
            Assert.AreEqual(n + 1, sim.Record.commands.Count);
            Assert.AreEqual("raid_room", sim.Record.commands.Last().kind);
            Assert.AreEqual(good.Encode(), sim.Record.commands.Last().a);
            Assert.AreNotSame(good, sim.S.Raid.Reports[0], "simülasyon kopyayı saklar");
            Assert.AreEqual(good.Encode(), sim.S.Raid.Reports[0].Encode());
            good.Turns = 99;
            Assert.AreNotEqual(99, sim.S.Raid.Reports[0].Turns);

            // Baskın biter (A'dan geri çekilme); sonra rapor reddedilir
            var A = sim.OpenRaidRoom();
            Assert.IsTrue(Baskin.Retreat(A));
            var end = sim.RaidRoomReport(A);
            Assert.IsTrue(end.Last);
            Assert.IsTrue(sim.AdvanceRaidRoom(end).Ok);
            Assert.IsTrue(sim.RaidRoomsClosed);
            Assert.IsNull(sim.RaidRoom);
            n = sim.Record.commands.Count;
            var after = sim.AdvanceRaidRoom(Copy(end));
            Assert.IsFalse(after.Ok);
            Assert.AreEqual(Loc.T("Baskının son odası raporlandı."), after.Msg);
            Assert.AreEqual(n, sim.Record.commands.Count);
            Assert.Throws<InvalidOperationException>(() => sim.OpenRaidRoom());
        }

        [Test]
        public void ResolveRaid_NeedsLastReport_UsesReports()
        {
            var sim = Sim();
            int n = sim.Record.commands.Count;
            var fake = new RaidResult { Success = true, Collateral = 9, Down = 0, Turns = 1, Hp = { 9, 9, 9 }, Armor = { 0, 0, 0 }, Reason = "sunum" };
            var early = sim.ResolveRaid(fake);
            Assert.IsFalse(early.Ok);
            Assert.AreEqual(Loc.T("Baskının son odası raporlanmadı."), early.Msg);
            Assert.AreEqual(n, sim.Record.commands.Count);
            Assert.IsNotNull(sim.S.Raid);
            for (int guard = 0; guard < 6 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                PlayRoom(B);
                Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
            }
            Assert.IsTrue(sim.RaidRoomsClosed);
            var expected = sim.FacilityRaidResult();
            var rooms = sim.S.Raid.Reports.Select(r => r.Encode()).ToList();
            var res = sim.ResolveRaid(fake);
            Assert.IsTrue(res.Ok, res.Msg);
            Assert.AreEqual("raidres", sim.Record.commands.Last().kind);
            Assert.AreEqual(expected.Encode(), sim.Record.commands.Last().a, "kayda raporlardan toplanan sonuç yazılır");
            Assert.AreNotEqual(fake.Encode(), sim.Record.commands.Last().a);
            CollectionAssert.AreEqual(rooms, sim.S.RaidLog.Last().Rooms);
            Assert.AreEqual(expected.Success, sim.S.RaidLog.Last().Success);
            Assert.AreEqual(expected.Collateral, sim.S.RaidLog.Last().Collateral);
            Assert.IsNull(sim.S.Raid);
        }

        [Test]
        public void RaidRoom_RecordOrder()
        {
            foreach (uint seed in new uint[] { 79, 5, 17 })
            {
                var sim = Sim(seed);
                int start = sim.Record.commands.FindLastIndex(c => c.kind == "liberation_raid");
                var reps = PlayRaid(sim);
                var kinds = sim.Record.commands.Skip(start).Select(c => c.kind).ToList();
                Assert.AreEqual("liberation_raid", kinds[0]);
                Assert.AreEqual("raidres", kinds[kinds.Count - 1]);
                Assert.AreEqual(reps.Count, kinds.Count - 2);
                Assert.IsTrue(kinds.Skip(1).Take(reps.Count).All(k => k == "raid_room"), string.Join(",", kinds));
                Assert.IsTrue(reps.Count >= 1 && reps.Count <= 5);
                var lastRoom = sim.Record.commands[sim.Record.commands.Count - 2];
                Assert.IsTrue(RoomReport.Decode(lastRoom.a).Last, "raidres'ten önceki raid_room son oda");
                Assert.IsTrue(sim.Record.commands.Skip(start + 1).Take(reps.Count - 1).All(c => !RoomReport.Decode(c.a).Last));
            }
        }

        // ---------- Kaydet ve devam ----------
        static Simulation Restore(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            var r = Replay.Restore(sim.Record);
            Assert.AreEqual(0, r.ReplayFailures);
            return r;
        }

        static void SameRaid(Simulation sim, Simulation r, string at)
        {
            Assert.AreEqual(sim.S.Rng.State, r.S.Rng.State, at);
            CollectionAssert.AreEqual(sim.S.Raid.Reports.Select(x => x.Encode()).ToList(), r.S.Raid.Reports.Select(x => x.Encode()).ToList(), at);
            Assert.AreEqual(sim.RaidRoom, r.RaidRoom, at);
            Assert.AreEqual(Baskin.Signature(sim.OpenRaidRoom()), Baskin.Signature(r.OpenRaidRoom()), at);
        }

        [Test]
        public void MidRaid_Resume_SameBoard()
        {
            var sim = Sim();
            SameRaid(sim, Restore(sim), "G");
            var G = sim.OpenRaidRoom();
            Assert.IsTrue(Baskin.EndTurn(G));
            ClearGate(G); ToGate(G); Assert.IsTrue(Baskin.Pass(G));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(G)).Ok);
            Assert.AreEqual(new RoomStep(2, false), sim.RaidRoom.Value);
            SameRaid(sim, Restore(sim), "A gidiş");
            var A = sim.OpenRaidRoom();
            ClearGate(A); ToGate(A); Assert.IsTrue(Baskin.Pass(A));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(A)).Ok);
            Assert.AreEqual(new RoomStep(3, false), sim.RaidRoom.Value);
            SameRaid(sim, Restore(sim), "H");
            var H = sim.OpenRaidRoom();
            StartEscape(H); Calm(H); ToGate(H); Assert.IsTrue(Baskin.Pass(H));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(H)).Ok);
            Assert.AreEqual(new RoomStep(2, true), sim.RaidRoom.Value);
            SameRaid(sim, Restore(sim), "A dönüş");
            // Son oda raporlanıp sonuç yazılmadan çıkıldı: devamda tahta açılmaz, sonuç raporlardan
            var R = sim.OpenRaidRoom();
            Assert.IsTrue(Baskin.Retreat(R) == false);
            Calm(R);
            ToGate(R);
            Assert.IsTrue(Baskin.Pass(R));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(R)).Ok);
            while (!sim.RaidRoomsClosed)
            {
                var B = sim.OpenRaidRoom();
                PlayRoom(B);
                Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
            }
            var r = Restore(sim);
            Assert.IsTrue(r.RaidRoomsClosed);
            Assert.AreEqual(sim.FacilityRaidResult().Encode(), r.FacilityRaidResult().Encode());
            Assert.IsTrue(r.ResolveRaid(null).Ok, "tesiste verilen sonuç yok sayılır");
        }

        [Test]
        public void FullCampaign_Replay_SameState()
        {
            // En az iki baskınlı ilk kampanya (79'dan; sürüm 10'da 79'un ilk baskını Teknisyen'i kaybediyor ve yedek yok)
            Simulation sim = null;
            int raids = 0;
            for (uint seed = 79; seed < 99 && raids <= 1; seed++)
            {
                sim = ShelterEventKit.New(Bal(), seed);
                raids = 0;
                for (int shift = 0; shift < 8 && sim.S.Ending == null; shift++)
                {
                    if (TryStartRaid(sim)) { PlayRaid(sim); raids++; }
                    ShelterEventKit.Close(sim);
                }
            }
            Assert.Greater(raids, 1);
            Assert.IsTrue(sim.S.RaidLog.All(l => l.Rooms.Count >= 1 && l.Rooms.Count <= 5));
            Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "raid_room"));
            var r = Restore(sim);
            Assert.AreEqual(State(sim), State(r));
            Assert.AreEqual(sim.S.Day, r.S.Day);
            Assert.AreEqual(sim.S.Fon, r.S.Fon);
            Assert.AreEqual(sim.S.Trust, r.S.Trust);
            Assert.AreEqual(sim.S.Alarm, r.S.Alarm);
            Assert.AreEqual(sim.S.Rng.State, r.S.Rng.State);
            for (int i = 0; i < sim.S.RaidLog.Count; i++)
            {
                Assert.AreEqual(sim.S.RaidLog[i].Success, r.S.RaidLog[i].Success);
                CollectionAssert.AreEqual(sim.S.RaidLog[i].Rooms, r.S.RaidLog[i].Rooms);
            }
        }

        // ---------- Aşama 12: Katar ----------
        /// <summary>Komutlarla na'ya varılan harekâtta Katar baskını (kenardan düşen, takip, AYIR) ve sonraki vardiyalar; kayıttan yeniden
        /// oynatılan oyun aynı (raporlar, sonuç dizgisi, kişilerin yatışı).</summary>
        [Test]
        public void KatarCampaign_Replay_SameState()
        {
            var sim = SimAt(79, "na");
            FacilityConditionCampaignTests.Order(sim);
            FacilityConditionCampaignTests.KatarRaid(sim);
            var res = sim.Record.commands.Last(c => c.kind == "raidres").a;
            Assert.AreEqual(1, RaidResult.Decode(res).Overboard.Count, "raidres kenardan düşeni taşır");
            Assert.IsTrue(sim.Record.commands.Where(c => c.kind == "raid_room").Select(c => RoomReport.Decode(c.a)).Any(r => r.Decouple));
            ShelterEventKit.Close(sim);
            for (int shift = 0; shift < 4 && sim.S.Ending == null; shift++)
            {
                if (TryStartRaid(sim)) PlayRaid(sim);
                ShelterEventKit.Close(sim);
            }
            var r = Restore(sim);
            Assert.AreEqual(State(sim), State(r));
            Assert.AreEqual(sim.S.Rng.State, r.S.Rng.State);
            for (int i = 0; i < sim.S.RaidLog.Count; i++)
                CollectionAssert.AreEqual(sim.S.RaidLog[i].Rooms, r.S.RaidLog[i].Rooms);
            CollectionAssert.AreEqual(sim.Record.commands.Select(c => c.kind + c.a + c.b).ToList(), r.Record.commands.Select(c => c.kind + c.a + c.b).ToList());
        }

        /// <summary>Takipli dönüş odasının (A'nın dönüşü) ortasında kaydet ve devam: aynı tahta, takipçi raporlardan kurulur; devam eden
        /// oyunda da baskın aynı biter.</summary>
        [Test]
        public void KatarFollowRoom_Resume_SameBoard()
        {
            var sim = SimAt(79, "na");
            FacilityConditionCampaignTests.Order(sim);
            Simulation resumed = null;
            FacilityConditionCampaignTests.KatarRaid(sim, onFollowRoom: s =>
            {
                Assert.AreEqual(new RoomStep(2, true), s.RaidRoom.Value);
                // Odanın ortası: tahta açılıp hamle yapılır, sonra kaydedilir (tahta kayda girmez)
                var mid = s.OpenRaidRoom();
                Assert.IsTrue(Baskin.EndTurn(mid));
                resumed = Restore(s);
                SameRaid(s, resumed, "takipli A dönüşü");
                var a = s.OpenRaidRoom();
                var b = resumed.OpenRaidRoom();
                var fa = a.Units.Where(u => u.Side == 'e' && !u.Dead).Select(u => $"{u.Type}:{u.X}:{u.Y}:{u.Hp}").ToList();
                Assert.AreEqual(1, fa.Count(x => x.StartsWith("dron:")), "takipçi");
                CollectionAssert.AreEqual(fa, b.Units.Where(u => u.Side == 'e' && !u.Dead).Select(u => $"{u.Type}:{u.X}:{u.Y}:{u.Hp}").ToList());
            });
            Assert.IsNotNull(resumed);
            var again = Restore(sim);
            Assert.AreEqual(State(sim), State(again));
        }

        // ---------- Sürüm ve parmak izi ----------
        [Test]
        public void Version_Is22()
        {
            Assert.AreEqual(22, GameRecord.CurrentVersion, "22: görevde iyileşme; 21: tek tahta kaldırıldı, tesis baskını tek kip; 20: g5 paket yerleşimi; 19: tesis oda zinciri (raid_room), tesis parmak izi");
            Assert.AreEqual(22, new GameRecord().version);
            Assert.AreEqual(22, Simulation.NewGame(79, new Balance()).Record.version);
        }

        /// <summary>Tek kip (kayıt 21): tesis izi hiç boş değil. Varsayılan denge (saha açık) f10, saha kapalıyken f8s10 (sürüm 10); boş izli kayıt (eski
        /// tek tahta kaydı) geçerli harita iziyle de yükleme süzgecinden geçmez.</summary>
        [Test]
        public void Fingerprint_NeverEmpty()
        {
            Assert.AreEqual("f10", FacilityMaps.FingerprintFor(new Balance()));
            Assert.AreEqual("f8s10", FacilityMaps.FingerprintFor(new Balance { infiltratorFieldEnabled = false }));
            var sim = Simulation.NewGame(79, new Balance());
            Assert.AreEqual("f10", sim.Record.facility);
            Assert.IsTrue(Replay.MapMatches(sim.Record));
            Assert.IsNotEmpty(sim.Record.map);
            sim.Record.facility = "";
            Assert.IsFalse(Replay.MapMatches(sim.Record), "izsiz kayıt geçerli haritayla da reddedilir");
            sim.Record.facility = null;
            Assert.IsFalse(Replay.MapMatches(sim.Record));
        }

        [Test]
        public void Facility_Fingerprint()
        {
            var on = Simulation.NewGame(79, Bal());
            Assert.AreEqual("f10", on.Record.facility);
            Assert.AreEqual("f10", on.Record.facility, "10: simetrik geçit, geçenlerle devam, kasayı her uzman açar (saha kapalıyken f8s10); 9: sahadaki sızmacı (saha kapalıyken f8); 6: aşama 12 (Katar ve Yalpa); 5: aşama 11 (kalıcı tahta, kuşatma ve savunma); 4: aşama 10 (önlem defteri); 3: aşama 8 (ekipman, sunucu ödülü, tesis kipinde üretim); 2: aşama 6 (büyük sandık nişi, düşen kişi ve ölüm kuralı)");
            Assert.AreEqual(on.Record.facility, on.FacilityFingerprint);
            Assert.IsTrue(Replay.MapMatches(on.Record));
            on.Record.facility = "f8";
            Assert.IsFalse(Replay.MapMatches(on.Record));
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "";
            Assert.IsFalse(Replay.MapMatches(on.Record), "izsiz tesis kaydı süzgeçten geçmez");
            var r = Replay.Restore(on.Record);
            Assert.AreEqual("f10", on.Record.facility, "Restore boşu doldurur (koddan kurulan geçici kayıt)");
            Assert.AreEqual(0, r.ReplayFailures);
            on.Record.facility = null;
            Replay.Restore(on.Record);
            Assert.AreEqual("f10", on.Record.facility);
            on.Record.facility = "f1";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 2–5'in tesis kaydı (f1) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f5";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 11'in tesis kaydı (f5) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f4";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 10'un tesis kaydı (f4) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f2";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 6'nın tesis kaydı (f2) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            on.Record.facility = "f3";
            Assert.IsFalse(Replay.MapMatches(on.Record), "aşama 8–9'un tesis kaydı (f3) yüklenmez");
            Assert.Throws<InvalidOperationException>(() => Replay.Restore(on.Record));
            Assert.AreEqual("", FacilityMaps.FingerprintFor(null), "dengesiz çağrı (savunma amaçlı)");
        }

        [Test]
        public void RaidRoom_EmptyReport_Fails()
        {
            var on = Sim();
            on.Record.commands.Add(new SavedCommand { day = on.S.Day, kind = "raid_room", a = null });
            on.Record.day = on.S.Day;
            Assert.AreEqual(1, Replay.Restore(on.Record).ReplayFailures, "boş rapor sessiz geçmez");
        }
    }
}
