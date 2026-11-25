using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 12, simülasyon (Docs/TESIS-BASKINLARI-TASARIMI.md §10.1, §10.2; aşama 12 tasarımı §6, §8.4): na ve oc emrinin şartı
    /// (Katar, Yalpa) ve okuyucuları (<see cref="Simulation.RegionEvent"/>, <see cref="Simulation.RegionNote"/>), kenardan düşen kadro
    /// üyesinin dönüşü (ölmez, <see cref="Balance.facilityFallInjuryTurns"/> vardiya yatar; Revir, beceri ve deneyim yok), "ekip düştü"
    /// baskını, oda raporunun Katar alanlarının yapısal denetimi (<see cref="Simulation.AdvanceRaidRoomCheck"/>) ve yeni adlar. Harekât
    /// yalnız komutlarla kurulur (<see cref="FacilityKit.SimAt"/>); Katar baskını betikli sürücüyle (<see cref="KatarRaid"/>) oynanır.
    /// </summary>
    public class FacilityConditionCampaignTests
    {
        static RoomReport Copy(RoomReport r) => RoomReport.Decode(r.Encode());
        static Baskin.Pt Pt(int x, int y) => new Baskin.Pt(x, y);

        /// <summary>Kadro sırasıyla uzmanların kareleri: EMP uzmanı (3,3), Analist (3,0) açık kenarda, öbürü (5,5).</summary>
        static Baskin.Pt[] FallSpots(Baskin.BState B) =>
            B.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).Select(u => u.Type == "emp" ? Pt(3, 3) : u.Type == "ana" ? Pt(3, 0) : Pt(5, 5)).ToArray();

        static Baskin.Unit OfType(Baskin.BState B, string type) => B.Units.First(u => Baskin.IsSpecialist(u) && u.Type == type);

        static void Advance(Simulation sim, Baskin.BState B, string at)
        {
            var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
            Assert.IsTrue(ok.Ok, at + ": " + ok.Msg);
        }

        /// <summary>
        /// Süren na baskınını (Katar) yalnız komut ve tahta kurulumuyla oynar: G temiz geçilir; A'da (Katar, gidiş) düz zeminde EMP uzmanı
        /// Analist'i açık kenardan düşürür ("k"). <paramref name="teamDown"/>: öbür iki uzman da yere düşer, baskın A'da biter. Değilse H'de
        /// kaçış başlar, batı geçidinin bitişiğindeki makine takip eder (rapor Follow); A'nın dönüşü (temiz olsa da) takipçiyle açılır:
        /// önce <paramref name="onFollowRoom"/> çağrılır, sonra makineler kalkar ve Teknisyen kapıda AYIR verir (rapor Decouple); kalan
        /// odalar betikli oynanır. Sonuç raporlardan yazılır (<see cref="Simulation.ResolveRaid"/>). Kadro (emir kopyası) döner.
        /// </summary>
        internal static List<SquadSlot> KatarRaid(Simulation sim, bool teamDown = false, Action<Simulation> onFollowRoom = null)
        {
            Assert.AreEqual("katar", sim.S.Raid.Params.Event, "na emri Katar taşır");
            var squad = sim.S.Raid.Params.Squad.ToList();
            // G (giriş vagonu, şartsız): makineler kalkar, geçilir
            var G = sim.OpenRaidRoom();
            Assert.AreEqual(1, G.Room.Index);
            Assert.AreNotEqual("katar", G.Ev?.Id, "giriş vagonu şartsız");
            Calm(G); ToGate(G); Assert.IsTrue(Baskin.Pass(G));
            Advance(sim, G, "G");
            // A (Katar, gidiş): Analist kenardan düşer
            var A = sim.OpenRaidRoom();
            Assert.AreEqual(new RoomStep(2, false), sim.RaidRoom.Value);
            Assert.AreEqual("katar", A.Ev?.Id);
            Flat(A, FallSpots(A));
            var ana = OfType(A, "ana");
            Assert.IsTrue(Baskin.IsOpenEdge(A, ana.X, ana.Y));
            Assert.IsTrue(Baskin.DoAct(A, OfType(A, "emp").Id, 3, 1), "EMP");
            Assert.IsTrue(ana.Overboard && ana.Dead, "Analist vagondan düştü");
            if (teamDown)
            {
                Down(A, OfType(A, "tek"));
                Down(A, OfType(A, "emp"));
                Assert.IsTrue(Baskin.EndTurn(A));
                Assert.AreEqual("lost", A.Phase);
                Advance(sim, A, "A ekip düştü");
                Assert.IsTrue(sim.RaidRoomsClosed);
            }
            else
            {
                ToGate(A); Assert.IsTrue(Baskin.Pass(A));
                Advance(sim, A, "A");
                // H (Katar): kaçış, batı geçidinin bitişiğinde bir makine (takip eder)
                var H = sim.OpenRaidRoom();
                Assert.AreEqual(3, H.Room.Index);
                Flat(H, H.Units.Where(Baskin.IsSpecialist).Where(Baskin.OnBoard).OrderBy(u => u.Slot).Select((u, i) => Pt(5 + i % 2, 5 + i / 2)).ToArray());
                StartEscape(H);
                var w = H.Room.West.OrderBy(t => t.Y).First();
                var follower = AddMachine(H, "dron", Pt(w.X + 1, w.Y));
                CollectionAssert.AreEqual(new[] { follower.Id }, Baskin.FollowerIds(H));
                ToGate(H); Assert.IsTrue(Baskin.Pass(H));
                var hr = sim.RaidRoomReport(H);
                CollectionAssert.AreEqual(new[] { 0 }, hr.Follow);
                Assert.IsTrue(sim.AdvanceRaidRoom(hr).Ok);
                Assert.AreEqual(new RoomStep(2, true), sim.RaidRoom.Value, "takip A'yı temizken de oynatır");
                // A dönüşü: takipçi raporlardan kurulur
                onFollowRoom?.Invoke(sim);
                var R = sim.OpenRaidRoom();
                Assert.IsTrue(R.Escape);
                Assert.AreEqual(1, R.Units.Count(u => u.Side == 'e' && !u.Dead && u.Type == "dron"), "takipçi dönüş tahtasında");
                Calm(R); ToGate(R);
                Assert.IsTrue(Baskin.CanDecouple(R), "AYIR: " + Baskin.DecoupleBlock(R));
                Assert.IsTrue(Baskin.Decouple(R));
                var rr = sim.RaidRoomReport(R);
                Assert.IsTrue(rr.Decouple);
                Assert.IsTrue(sim.AdvanceRaidRoom(rr).Ok);
                for (int guard = 0; guard < 3 && !sim.RaidRoomsClosed; guard++)
                {
                    var B = sim.OpenRaidRoom();
                    PlayRoom(B, 0);
                    Advance(sim, B, "kalan oda");
                }
                Assert.IsTrue(sim.RaidRoomsClosed);
            }
            var res = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(res.Ok, res.Msg);
            return squad;
        }

        /// <summary>Bölgenin sıradaki sektörüne kadro ve emir (Assert).</summary>
        internal static void Order(Simulation sim)
        {
            ShelterEventKit.FillSquad(sim);
            var started = sim.StartLiberationRaid(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id);
            Assert.IsTrue(started.Ok, started.Msg);
        }

        // ---------- 1. Emir ve okuyucular ----------
        [Test]
        public void Orders_CarryConditions_AndReaders()
        {
            var na = SimAt(79, "na");
            Assert.AreEqual("katar", na.RegionEvent("na"));
            Assert.AreEqual("yalpa", na.RegionEvent("oc"));
            Assert.AreEqual("asansor", GameData.Region("na").Event, "bölgenin bugünkü olayı değişmez");
            Assert.AreEqual(GameData.Region("na").FacilityNote, na.RegionNote("na"));
            Assert.AreEqual(GameData.Region("oc").FacilityNote, na.RegionNote("oc"));
            Assert.AreEqual(GameData.Region("weu").Event, na.RegionEvent("weu"), "öbür bölgelerde şart bugünkü olay");
            Assert.AreEqual(GameData.Region("weu").BuildingNote, na.RegionNote("weu"));
            Assert.IsNull(GameData.Region("weu").FacilityNote);
            Assert.IsNull(na.RegionEvent("xx"));
            Assert.IsNull(na.RegionNote("xx"));
            Order(na);
            Assert.AreEqual("katar", na.S.Raid.Params.Event);
            Assert.AreEqual(na.RegionEvent("na"), na.S.Raid.Params.Event, "okuyucu emirle aynı kural");
            Assert.AreEqual("katar", na.OpenRaidRoom().Params.Event);

            var oc = SimAt(79, "oc");
            Order(oc);
            Assert.AreEqual("yalpa", oc.S.Raid.Params.Event);
            Assert.AreEqual(oc.RegionEvent("oc"), oc.S.Raid.Params.Event);
            Assert.AreEqual(3, oc.S.Raid.Params.Rooms.Count);

            // Alt bayraklar kapalı: tesis aşama 11'in olayları
            var sub = Bal();
            sub.facilityKatar = false;
            sub.facilityYalpa = false;
            var subSim = Simulation.NewGame(79, sub);
            Assert.AreEqual("asansor", subSim.RegionEvent("na"));
            Assert.AreEqual("yag", subSim.RegionEvent("oc"));
            Assert.AreEqual(GameData.Region("na").BuildingNote, subSim.RegionNote("na"));
            // Bölge olayları kapalı: olay yok, statik not
            var calm = Simulation.NewGame(79, Bal(events: false));
            Assert.IsNull(calm.RegionEvent("na"));
            Assert.AreEqual(GameData.Region("na").BuildingNote, calm.RegionNote("na"));
        }

        // ---------- 1b. Odanın tur bütçesi okuyucusu (aşama 12 kapanışı) ----------
        /// <summary>
        /// <see cref="Simulation.FacilityRoomTurns"/> (bina planının yatış takvimi bunu okur) = emrin o odada açtığı ilk ziyaret tahtasının
        /// MaxTurns'ü: na (hedefte "kisa") ve oc, üç oda türü; Eşik kazanımı giriş odasına işler; denge değişince ikisi birlikte değişir.
        /// Sunum kuralı kopyalamaz; Core'un kuralı değişirse bu test kırılır.
        /// </summary>
        [Test]
        public void FacilityRoomTurns_EqualsTheOpenedBoard()
        {
            foreach (var (region, tune, threshold) in new (string, Action<Balance>, bool)[]
                     {
                         ("na", null, false), ("oc", null, false), ("na", b => { b.facilityTargetTurns = 6; b.facilityMiddleTurns = 4; }, false),
                         ("na", null, true), ("oc", null, true),
                     })
            {
                var sim = SimAt(79, region, tune);
                if (threshold) sim.Facility.Perks.Add(FacilityGear.Threshold);
                Order(sim);
                var p = sim.S.Raid.Params;
                var rooms = p.Rooms.Select(id => sim.FacilityPlan.Rooms.First(r => r.Id == id)).ToList();
                CollectionAssert.AreEqual(new[] { RoomType.Entry, RoomType.Middle, RoomType.Target }, rooms.Select(r => r.Type).ToArray());
                var turns = rooms.Select(r => Baskin.CreateRoom(p, r, RaidChain.Setup(sim.B, r, p, new List<RoomReport>(), false)).MaxTurns).ToArray();
                CollectionAssert.AreEqual(turns, rooms.Select(sim.FacilityRoomTurns).ToArray(), region + (threshold ? " Eşik" : ""));
                Assert.AreEqual(turns[0], sim.OpenRaidRoom().MaxTurns, "gerçek emrin ilk tahtası");
                int entry = sim.B.facilityEntryTurns + (threshold ? sim.B.facilityThresholdTurns : 0);
                int target = sim.B.facilityTargetTurns - (region == "na" ? 1 : 0);
                CollectionAssert.AreEqual(new[] { entry, sim.B.facilityMiddleTurns, target }, turns, "na hedefi kısa nöbet");
            }
            Assert.AreEqual(0, SimAt(79, "na").FacilityRoomTurns(null));
        }

        // ---------- 2. Kenardan düşen yaşar ve yatar ----------
        [Test]
        public void Overboard_Survives_LaidUpFlat_NoInfirmaryNoSkillNoExperience()
        {
            // Revir ve beceri indirimi bu dengede her düşenin yatışını kısaltırdı: kenardan düşene işlemez
            var sim = SimAt(79, "na", b =>
            {
                b.boardFieldSkillRecoveryTier = 0;
                b.boardFieldSkillRecoveryBonus = 2;
                b.facilityInfirmaryShifts = 2;
            });
            sim.Facility.Perks.Add(FacilityGear.Infirmary);
            Order(sim);
            Assert.IsTrue(sim.S.Raid.Params.Perks.Contains(FacilityGear.Infirmary));
            var slots = sim.S.Raid.Params.Squad;
            int a = slots.FindIndex(s => s.Type == "ana");
            var person = sim.Board.People.First(p => p.Id == slots[a].PersonId);
            int xp = person.Skill("ana");
            var gear = sim.Board.Equipment.Where(e => e.OwnerId == person.Id).Select(e => e.Id).OrderBy(x => x).ToList();
            var others = slots.Where((s, i) => i != a).Select(s => sim.Board.People.First(p => p.Id == s.PersonId)).ToList();

            var squad = KatarRaid(sim);
            var log = sim.S.RaidLog.Last();
            var rooms = log.Rooms.Select(RoomReport.Decode).ToList();
            Assert.AreEqual("k", rooms.Last().Team[a].State, "rapor kenardan düşeni taşır");
            Assert.IsTrue(rooms.Any(r => r.Follow.Count > 0) && rooms.Any(r => r.Decouple), "takip ve AYIR kayıtta");
            var res = RaidResult.Decode(sim.Record.commands.Last(c => c.kind == "raidres").a);
            CollectionAssert.AreEqual(new[] { a }, res.Overboard);

            Assert.IsTrue(person.Alive, "kenardan düşen ölmez");
            Assert.AreEqual(0, person.Hp);
            Assert.AreEqual(sim.B.facilityFallInjuryTurns, person.Down, "düz yatış: Revir ve beceri işlemez");
            Assert.AreEqual(5, person.Down);
            Assert.AreEqual("rest", person.Job);
            Assert.AreEqual(xp, person.Skill("ana"), "deneyim yok");
            CollectionAssert.AreEqual(gear, sim.Board.Equipment.Where(e => e.OwnerId == person.Id).Select(e => e.Id).OrderBy(x => x).ToList(), "eşyası sahibinde");
            var o = log.LiberationReward.Squad[a];
            Assert.IsTrue(o.Overboard && o.Down && !o.Dead);
            Assert.IsFalse(log.LiberationReward.Squad.Where((x, i) => i != a).Any(x => x.Overboard));
            StringAssert.Contains(Loc.F("{0} vagondan düştü; {1} vardiya yatış gerekiyor.", person.Name, 5), sim.Board.RaidSummary);
            Assert.IsTrue(others.All(p => p.Alive && p.Down == 0), "ayakta dönenler");
            Assert.AreEqual(3, squad.Count);

            // En az 1 vardiya
            var zero = SimAt(79, "na", b => b.facilityFallInjuryTurns = 0);
            Order(zero);
            int z = zero.S.Raid.Params.Squad.FindIndex(s => s.Type == "ana");
            var zp = zero.Board.People.First(p => p.Id == zero.S.Raid.Params.Squad[z].PersonId);
            KatarRaid(zero);
            Assert.IsTrue(zp.Alive);
            Assert.AreEqual(1, zp.Down);
        }

        // ---------- 3. Ekip düştü ----------
        [Test]
        public void TeamDown_OverboardLives_LyingDie()
        {
            var sim = SimAt(79, "na");
            Order(sim);
            var squad = KatarRaid(sim, teamDown: true);
            var log = sim.S.RaidLog.Last();
            Assert.IsFalse(log.Success);
            var last = RoomReport.Decode(log.Rooms.Last());
            for (int i = 0; i < squad.Count; i++)
            {
                var p = sim.Board.People.First(x => x.Id == squad[i].PersonId);
                if (squad[i].Type == "ana")
                {
                    Assert.AreEqual("k", last.Team[i].State);
                    Assert.IsTrue(p.Alive, "kenardan düşen yaşar");
                    Assert.AreEqual(sim.B.facilityFallInjuryTurns, p.Down);
                    Assert.IsTrue(log.LiberationReward.Squad[i].Overboard);
                }
                else
                {
                    Assert.AreEqual("d", last.Team[i].State);
                    Assert.IsFalse(p.Alive, "yerde yatan çıkarılmadı: ölür");
                    Assert.IsTrue(log.LiberationReward.Squad[i].Dead);
                    Assert.IsFalse(log.LiberationReward.Squad[i].Overboard);
                }
            }
        }

        // ---------- 4. Rapor denetimi ----------
        [Test]
        public void AdvanceRaidRoomCheck_KatarFields()
        {
            var sim = SimAt(79, "na");
            Order(sim);
            void Refused(RoomReport r, string why)
            {
                int n = sim.Record.commands.Count, reps = sim.S.Raid.Reports.Count;
                Assert.AreEqual(Loc.T(why), sim.AdvanceRaidRoomCheck(r), why);
                Assert.IsFalse(sim.AdvanceRaidRoom(r).Ok);
                Assert.AreEqual(n, sim.Record.commands.Count, why + ": kayda girmez");
                Assert.AreEqual(reps, sim.S.Raid.Reports.Count);
            }
            const string Where = "Takip ve AYIR yalnız Katar vagonunun dönüş geçişinde olur.";
            const string Bad = "Raporun takip listesi geçersiz.";
            const string Both = "Rapor hem takip hem AYIR taşıyor.";
            const string Edge = "Kenardan düşme yalnız Katar vagonunda olur.";
            // G (şartsız, gidiş)
            var G = sim.OpenRaidRoom();
            Calm(G); ToGate(G); Assert.IsTrue(Baskin.Pass(G));
            var g = sim.RaidRoomReport(G);
            var g1 = Copy(g); g1.Machines.Add(new RoomMachine { Type = "dron", X = 1, Y = 3, Hp = 3 }); g1.Follow.Add(0); Refused(g1, Where);
            var g2 = Copy(g); g2.Follow.Add(0); Refused(g2, Bad);
            var g3 = Copy(g); g3.Decouple = true; Refused(g3, Where);
            var g4 = Copy(g); g4.Team[2] = new RoomMember { State = "k", Fell = 1, X = 3, Y = 0 }; Refused(g4, Edge);
            Assert.IsNull(sim.AdvanceRaidRoomCheck(g));
            Assert.IsTrue(sim.AdvanceRaidRoom(g).Ok);
            // A (Katar, gidiş): kenardan düşen kabul; takip ve AYIR yok
            var A = sim.OpenRaidRoom();
            Flat(A, FallSpots(A));
            Assert.IsTrue(Baskin.DoAct(A, OfType(A, "emp").Id, 3, 1));
            ToGate(A); Assert.IsTrue(Baskin.Pass(A));
            var a = sim.RaidRoomReport(A);
            Assert.IsTrue(a.Team.Any(m => m.State == "k"));
            var a1 = Copy(a); a1.Decouple = true; Refused(a1, Where);
            var a2 = Copy(a); a2.Machines.Add(new RoomMachine { Type = "dron", X = 6, Y = 3, Hp = 3 }); a2.Follow.Add(0); Refused(a2, Where);
            Assert.IsTrue(sim.AdvanceRaidRoom(a).Ok, "Katar odasında \"k\" geçerli");
            // H (Katar, kaçış): takip geçerli; aralık dışı, çift ve AYIR'la birlikte değil
            var H = sim.OpenRaidRoom();
            Flat(H, H.Units.Where(Baskin.IsSpecialist).Where(Baskin.OnBoard).OrderBy(u => u.Slot).Select((u, i) => Pt(5 + i, 5)).ToArray());
            StartEscape(H);
            var w = H.Room.West.OrderBy(t => t.Y).First();
            AddMachine(H, "dron", Pt(w.X + 1, w.Y));
            ToGate(H); Assert.IsTrue(Baskin.Pass(H));
            var h = sim.RaidRoomReport(H);
            CollectionAssert.AreEqual(new[] { 0 }, h.Follow);
            var h1 = Copy(h); h1.Decouple = true; Refused(h1, Both);
            var h2 = Copy(h); h2.Follow[0] = h2.Machines.Count; Refused(h2, Bad);
            var h3 = Copy(h); h3.Follow.Add(0); Refused(h3, Bad);
            var h4 = Copy(h); h4.Follow[0] = -1; Refused(h4, Bad);
            var h5 = Copy(h); h5.Team.First(m => m.State == "k").Fell = 1; Refused(h5, Edge);
            Assert.IsNull(sim.AdvanceRaidRoomCheck(h));
            Assert.IsTrue(sim.AdvanceRaidRoom(h).Ok);

            // Katar dışı tesis (weu): hedef odasının kaçışında takip ve kenardan düşen reddedilir
            var weu = Sim();
            for (int guard = 0; guard < 2; guard++)
            {
                var B = weu.OpenRaidRoom();
                ClearGate(B); ToGate(B); Assert.IsTrue(Baskin.Pass(B));
                Assert.IsTrue(weu.AdvanceRaidRoom(weu.RaidRoomReport(B)).Ok);
            }
            var WH = weu.OpenRaidRoom();
            Assert.AreEqual(3, WH.Room.Index);
            StartEscape(WH); Calm(WH); ToGate(WH); Assert.IsTrue(Baskin.Pass(WH));
            var wh = weu.RaidRoomReport(WH);
            Assert.IsEmpty(wh.Follow);
            var w1 = Copy(wh); w1.Machines.Add(new RoomMachine { Type = "dron", X = 1, Y = 3, Hp = 3 }); w1.Follow.Add(0);
            Assert.AreEqual(Loc.T(Where), weu.AdvanceRaidRoomCheck(w1));
            var w2 = Copy(wh); w2.Decouple = true;
            Assert.AreEqual(Loc.T(Where), weu.AdvanceRaidRoomCheck(w2));
            var w3 = Copy(wh); w3.Team[0] = new RoomMember { State = "k", Fell = 3, X = 3, Y = 0 };
            Assert.AreEqual(Loc.T(Edge), weu.AdvanceRaidRoomCheck(w3));
            Assert.IsNull(weu.AdvanceRaidRoomCheck(wh));
        }

        // ---------- 5. Adlar ----------
        [Test]
        public void Names_TrainAndRig()
        {
            var na = GameData.Region("na");
            Assert.AreEqual("Sevkiyat katarı", na.Building);
            CollectionAssert.AreEqual(new[] { "Yük vagonları", "Ayıklama vagonu", "Sunucu vagonu" }, new[] { na.Sector(0), na.Sector(1), na.Sector(2) });
            StringAssert.Contains("yük asansörü", na.BuildingNote, "statik not bugünkü olaya da uyar");
            StringAssert.Contains("kuzey ve güney kenarı açık", na.FacilityNote);
            var oc = GameData.Region("oc");
            Assert.AreEqual("Yüzer maden platformu", oc.Building);
            CollectionAssert.AreEqual(new[] { "Kumanya güvertesi", "Bant galerisi", "Makine dairesi" }, new[] { oc.Sector(0), oc.Sector(1), oc.Sector(2) });
            StringAssert.Contains("hortumu", oc.BuildingNote);
            StringAssert.Contains("batıya, doğuya, batıya", oc.FacilityNote);
            Assert.AreEqual(2, GameData.Regions.Count(r => r.FacilityNote != null), "tesis notu yalnız na ve oc'de");
        }
    }
}
