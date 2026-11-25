using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 12, baskın botu (aşama 12 tasarımı §9.1, §9.4): bot Katar'ı (na) ve Yalpa'yı (oc) istisnasız oynar; AYIR politikası
    /// (<see cref="RaidBot.Detach"/>) Katar'ın dönüş geçidinde çalışır, verilmeyen AYIR'ın nedeni sayılır; kenardan düşen ekip üyesinin
    /// bedeli politikadan gelir (<see cref="RaidBot.Edge"/>): "hic" kendi üyesini düşürmez, "kacis" yetişemeyecek üyeyi EMP ile kenardan
    /// atar (rapor "k", sonuçta ölmez, yatar); kenardan düşen makine, yatış ve kayma efektlerden bir kez sayılır, kayma itme sayılmaz.
    /// Harekât <see cref="FacilityKit.SimAt"/> ile yalnız komutlarla kurulur; oda düzeyi testler tahtayı test yardımcılarıyla kurar.
    /// </summary>
    public class FacilityConditionBotTests
    {
        static Baskin.Pt Pt(int x, int y) => new Baskin.Pt(x, y);

        /// <summary>Testlerde seçenek sayısı küçük (süre); oyunun kendisi aynı yoldan gelir.</summary>
        static RaidBot Bot(string ayir = "hep", string kenar = "hic") => new RaidBot { Alternatives = 3, Detach = ayir, Edge = kenar };

        /// <summary>Baskını bot oynar; oda başına (rapor, sayaç, son tahta) listesi döner, sonuç yazılmaz.</summary>
        static List<(RoomReport rep, RaidBot.Tally tally, Baskin.BState B)> Play(Simulation sim, RaidBot bot, out RaidResult result)
        {
            var rooms = new List<(RoomReport, RaidBot.Tally, Baskin.BState)>();
            bot.OnRoom = (B, rep, t) =>
            {
                Assert.IsTrue(B.Phase == "pass" || B.Phase == "won" || B.Phase == "lost", "oda bitmedi: " + B.Phase);
                rooms.Add((rep, t, B));
            };
            result = bot.PlayFacility(sim);
            return rooms;
        }

        /// <summary>Bölgeye komutla gelinir ve sıradaki sektöre emir verilir (şart emirde).</summary>
        static Simulation Ordered(uint seed, string region, string ev)
        {
            var sim = SimAt(seed, region);
            FacilityConditionCampaignTests.Order(sim);
            Assert.AreEqual(ev, sim.S.Raid.Params.Event, region + " emri " + ev + " taşır");
            return sim;
        }

        static int Get(Dictionary<string, int> map, string k) => map.TryGetValue(k, out int n) ? n : 0;

        // ---------- Katar: zincir ----------
        [Test]
        public void Bot_Katar_PlaysTheChain([Values("hep", "hic")] string ayir, [Values("hic", "kacis")] string kenar, [Range(1, 3)] int seed)
        {
            var sim = Ordered((uint)seed, "na", "katar");
            int start = sim.Record.commands.Count;
            var squad = sim.S.Raid.Params.Squad.ToList();
            var rooms = Play(sim, Bot(ayir, kenar), out var result);
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            CollectionAssert.AreEqual(rooms.Select(r => r.rep.Encode()).ToList(), sim.S.Raid.Reports.Select(r => r.Encode()).ToList());
            Assert.AreEqual(sim.FacilityRaidResult().Encode(), result.Encode());
            foreach (var (rep, t, B) in rooms)
            {
                string at = (rep.Return ? "dönüş " : "") + rep.Room;
                // Giriş vagonunda (G, g) şart yok, kenar olayı yok; A ve H Katar
                Assert.AreEqual(rep.Room == 1 ? null : "katar", B.Ev?.Id, at);
                if (rep.Room == 1) Assert.AreEqual(0, t.EdgeMachines + t.EdgeTeam + t.EdgeOther, "girişte kenar olayı: " + at);
                // Gidişte (G, A) AYIR fırsatı ve takip yok
                if (!rep.Return && rep.Room < 3) Assert.IsTrue(t.Detaches == 0 && t.DetachMiss.Count == 0 && t.Followers == 0, "gidişte AYIR/takip: " + at);
                Assert.AreEqual(rep.Follow.Count, t.Followers, "raporun takipçileri: " + at);
                Assert.AreEqual(rep.Decouple ? 1 : 0, t.Detaches, "raporun AYIR'ı: " + at);
                if (ayir == "hic") Assert.AreEqual(0, t.Detaches, "hic: AYIR yok");
                if (t.Detaches > 0) Assert.AreEqual(0, t.Followers, "AYIR takibi keser");
                Assert.AreEqual(0, t.Tilts, "Katar'da yatış yok");
            }
            var res = sim.ResolveRaid(result);
            Assert.IsTrue(res.Ok, res.Msg);
            Assert.AreEqual(rooms.Count, sim.Record.commands.Skip(start).Count(c => c.kind == "raid_room"));
            // Kenardan düşen ölmez, facilityFallInjuryTurns vardiya yatar
            var rw = sim.S.RaidLog.Last().LiberationReward;
            foreach (int i in result.Overboard)
            {
                var p = sim.Board.Person(squad[i].PersonId);
                Assert.IsTrue(p.Alive && rw.Squad[i].Overboard && !rw.Squad[i].Dead, "kenardan düşen yaşar");
                Assert.AreEqual(Math.Max(1, sim.B.facilityFallInjuryTurns), p.Down);
            }
            Assert.AreEqual(rooms.Sum(r => r.tally.EdgeTeam), result.Overboard.Count, "kenardan düşen uzman sonuçta");
            TestContext.WriteLine($"{ayir}/{kenar} tohum {seed}: oda {rooms.Count}, AYIR {rooms.Sum(r => r.tally.Detaches)}, takip "
                + $"{rooms.Sum(r => r.tally.Followers)}, kenar makine {rooms.Sum(r => r.tally.EdgeMachines)} ekip {result.Overboard.Count}, zafer {sim.S.RaidLog.Last().Success}");
        }

        // ---------- Katar: AYIR (oda düzeyi) ----------
        /// <summary>H (Katar) kaçışta, düz zemin, bütün ekip batı geçidinde (GEÇ verilebilir); <paramref name="follower"/>: batı geçidinin
        /// bitişiğinde bir dron (takip eder). <paramref name="before"/> ekip kapıya girmeden önce (ör. Teknisyen düşer).</summary>
        static Baskin.BState AtReturnGate(out RaidParams p, out List<RoomReport> reps, bool follower, Action<Baskin.BState> before = null)
        {
            p = Params(Wing("na_1"), 11);
            reps = new List<RoomReport> { Rep(1), Rep(2) };
            var H = Open(p, reps);
            Assert.AreEqual(3, H.Room.Index);
            Assert.AreEqual("katar", H.Ev?.Id);
            Flat(H, Pt(5, 5), Pt(6, 5), Pt(6, 6));
            StartEscape(H);
            if (follower)
            {
                var w = H.Room.West.OrderBy(t => t.Y).First();
                AddMachine(H, "dron", Pt(w.X + 1, w.Y));
                Assert.AreEqual(1, Baskin.FollowerIds(H).Count);
            }
            before?.Invoke(H);
            ToGate(H);
            Assert.IsTrue(Baskin.CanPass(H));
            return H;
        }

        [Test]
        public void Bot_Katar_Detach_NoFollowers([Values("hep", "takip")] string ayir)
        {
            var H = AtReturnGate(out var p, out var reps, true);
            var t = Bot(ayir).Play(H);
            Assert.AreEqual("pass", H.Phase);
            Assert.IsTrue(H.Decoupled, "AYIR verildi");
            Assert.AreEqual(1, t.Detaches);
            Assert.AreEqual(0, t.DetachMiss.Count);
            Assert.AreEqual(0, t.Followers);
            Assert.IsEmpty(Baskin.FollowerIds(H));
            var r = File(p, reps, H);
            Assert.IsTrue(r.Decouple);
            Assert.IsEmpty(r.Follow);
            Assert.IsNull(RaidChain.Next(p, reps), "takip yok: temiz vagonlar yürünür, baskın biter");
        }

        [Test]
        public void Bot_Katar_Pass_FollowersPlaced()
        {
            var H = AtReturnGate(out var p, out var reps, true);
            var t = Bot("hic").Play(H);
            Assert.AreEqual("pass", H.Phase);
            Assert.IsFalse(H.Decoupled);
            Assert.AreEqual(0, t.Detaches);
            Assert.AreEqual(1, Get(t.DetachMiss, "pol"), "politika AYIR istemedi");
            Assert.AreEqual(1, t.Followers);
            var r = File(p, reps, H);
            CollectionAssert.AreEqual(new[] { 0 }, r.Follow);
            Assert.AreEqual(new RoomStep(2, true), RaidChain.Next(p, reps), "takipçi temiz vagonu oynatır");
            var A = Open(p, reps);
            Assert.IsTrue(A.Escape);
            var dron = A.Units.Where(u => u.Side == 'e' && !u.Dead && u.Type == "dron").ToList();
            Assert.AreEqual(1, dron.Count, "takipçi sonraki vagonda");
            int near = A.Room.Forward.Min(g => Math.Abs(g.X - dron[0].X) + Math.Abs(g.Y - dron[0].Y));
            Assert.LessOrEqual(near, 2, "doğu geçidine yakın");

            // "takip": takipçi yokken AYIR verilmez (GEÇ)
            var H2 = AtReturnGate(out _, out _, false);
            var t2 = Bot("takip").Play(H2);
            Assert.IsFalse(H2.Decoupled);
            Assert.AreEqual(1, Get(t2.DetachMiss, "pol"));
        }

        [Test]
        public void Bot_Katar_DetachMissed_WhenTechSpentAbility()
        {
            // Teknisyen bu tur yeteneğini kullandı: AYIR verilemez (acted), GEÇ verilir, takip işler
            var H = AtReturnGate(out var p, out var reps, true);
            H.Units.First(u => Baskin.IsSpecialist(u) && u.Type == "tek").Acted = true;
            Assert.AreEqual("acted", Baskin.DecoupleBlock(H));
            var t = Bot("hep").Play(H);
            Assert.AreEqual("pass", H.Phase);
            Assert.IsFalse(H.Decoupled);
            Assert.AreEqual(0, t.Detaches);
            Assert.AreEqual(1, Get(t.DetachMiss, "acted"));
            Assert.AreEqual(1, t.Followers);
            CollectionAssert.AreEqual(new[] { 0 }, File(p, reps, H).Follow);

            // Teknisyen yerde: nedeni "tek"
            var H2 = AtReturnGate(out _, out _, true, B => Down(B, B.Units.First(u => Baskin.IsSpecialist(u) && u.Type == "tek")));
            Assert.AreEqual("tek", Baskin.DecoupleBlock(H2));
            var t2 = Bot("hep").Play(H2);
            Assert.AreEqual(1, Get(t2.DetachMiss, "tek"));
            Assert.AreEqual(0, t2.Detaches);
        }

        // ---------- Katar: kenar ----------
        /// <summary>A (Katar, gidiş), düz zemin: EMP uzmanı (3,3), Analist kuzey kenarında (3,0), Teknisyen (5,5); makine (2,1). EMP'nin (3,1)
        /// atışı makineyi iter ve Analist'i kenardan düşürür.</summary>
        static Baskin.BState EdgeBoard(int maxTurns)
        {
            var p = Params(Wing("na_1"), 11);
            var B = Open(p, new List<RoomReport> { Rep(1) });
            Assert.AreEqual("katar", B.Ev?.Id);
            var team = B.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).ToList();
            Flat(B, team.Select(u => u.Type == "emp" ? Pt(3, 3) : u.Type == "ana" ? Pt(3, 0) : Pt(5, 5)).ToArray());
            AddMachine(B, "bakim", Pt(2, 1), 3);
            B.MaxTurns = maxTurns;
            return B;
        }

        [Test]
        public void Bot_Kenar_Hic_NeverPushesTeamOff()
        {
            // Sınamanın koşulu: kenardan düşürme ödüllendirilirse bot bu seçeneği bulur (aday var)
            var lure = EdgeBoard(1);
            var bait = Bot();
            bait.W.EdgeFall = -2000;
            var tb = bait.Play(lure);
            Assert.GreaterOrEqual(tb.EdgeTeam, 1, "ödüllü kenar: aday üretilmiyor");
            Assert.IsTrue(lure.Units.Any(u => Baskin.IsSpecialist(u) && u.Overboard));

            var B = EdgeBoard(3);
            var t = Bot("hep", "hic").Play(B);
            Assert.AreEqual(0, t.EdgeTeam, "hic: üye kenardan atılmaz");
            Assert.IsFalse(B.Units.Any(u => Baskin.IsSpecialist(u) && u.Overboard));
        }

        /// <summary>
        /// "kacis": H'nin (Katar) son kaçış turunda Analist (6,0) ve EMP uzmanı (4,1) batı geçidine yetişemez; Teknisyen kapıda. Bot EMP ile
        /// Analist'i kenardan atar; rapor "k", sonuçta ölmez, facilityFallInjuryTurns vardiya yatar. "hic" atmaz (Analist geride kalır).
        /// </summary>
        [Test]
        public void Bot_Kenar_Kacis_PushesDoomedOff([Values("kacis", "hic")] string kenar)
        {
            var sim = Ordered(79, "na", "katar");
            var squad = sim.S.Raid.Params.Squad.ToList();
            for (int k = 0; k < 2; k++)
            {
                var B = sim.OpenRaidRoom();
                Calm(B); ToGate(B); Assert.IsTrue(Baskin.Pass(B));
                Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
            }
            var H = sim.OpenRaidRoom();
            Assert.AreEqual(3, H.Room.Index);
            var team = H.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).ToList();
            Flat(H, team.Select(u => u.Type == "emp" ? Pt(4, 1) : u.Type == "ana" ? Pt(6, 0) : Pt(1, 4)).ToArray());
            StartEscape(H);
            var tek = team.First(u => u.Type == "tek");
            var ana = team.First(u => u.Type == "ana");
            ToGate(H, tek);
            H.MaxTurns = H.Turn;
            var t = Bot("hep", kenar).Play(H);
            if (kenar == "hic")
            {
                Assert.IsFalse(ana.Overboard, "hic: kenardan atılmaz");
                Assert.AreEqual(0, t.EdgeTeam);
                return;
            }
            Assert.IsTrue(ana.Overboard && ana.Dead, "kacis: yetişemeyecek Analist kenardan atıldı");
            Assert.AreEqual(1, t.EdgeTeam);
            var rep = sim.RaidRoomReport(H);
            Assert.AreEqual("k", rep.Team[ana.Slot].State);
            Assert.IsTrue(sim.AdvanceRaidRoom(rep).Ok);
            for (int guard = 0; guard < 3 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                PlayRoom(B, 0);
                Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
            }
            var result = sim.FacilityRaidResult();
            CollectionAssert.AreEqual(new[] { ana.Slot }, result.Overboard);
            Assert.IsTrue(sim.ResolveRaid(result).Ok);
            var p = sim.Board.Person(squad[ana.Slot].PersonId);
            Assert.IsTrue(p.Alive, "kenardan düşen ölmez");
            Assert.AreEqual(sim.B.facilityFallInjuryTurns, p.Down);
            Assert.IsTrue(sim.S.RaidLog.Last().LiberationReward.Squad[ana.Slot].Overboard);
        }

        [Test]
        public void Bot_Katar_PushesMachineOff()
        {
            // A (Katar, gidiş): kuzey kenarında canı yüksek bakım robotu (3,0), yanında bu tur yürümüş Teknisyen (3,1); öbür uzmanlar
            // kapıda. Teknisyen'in tek kazançlı hamlesi sabotajla makineyi kenardan düşürmek (kopyadaki doğrulama makineyi yok görür)
            var p = Params(Wing("na_1"), 11);
            var B = Open(p, new List<RoomReport> { Rep(1) });
            var team = B.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).ToList();
            Flat(B, team.Select(u => u.Type == "tek" ? Pt(3, 1) : u.Type == "emp" ? Pt(5, 5) : Pt(6, 6)).ToArray());
            var e = AddMachine(B, "bakim", Pt(3, 0), 9);
            foreach (var u in team.Where(u => u.Type != "tek")) ToGate(B, u);
            var tek = team.First(u => u.Type == "tek");
            tek.Moved = true;
            B.MaxTurns = 4;
            var t = Bot().Play(B);
            Assert.IsTrue(e.Dead, "makine düştü");
            Assert.AreEqual(1, t.EdgeMachines);
            Assert.AreEqual(1, Get(t.EdgeBy, "sab"), "kaynak sabotaj");
            Assert.AreEqual(0, t.EdgeTeam);
        }

        // ---------- Yalpa ----------
        [Test]
        public void Bot_Yalpa_PlaysTheChain([Range(1, 3)] int seed)
        {
            var sim = Ordered((uint)seed, "oc", "yalpa");
            var rooms = Play(sim, Bot(), out var result);
            Assert.IsTrue(sim.RaidRoomsClosed);
            CollectionAssert.AreEqual(rooms.Select(r => r.rep.Encode()).ToList(), sim.S.Raid.Reports.Select(r => r.Encode()).ToList());
            foreach (var (rep, t, B) in rooms)
            {
                Assert.AreEqual(rep.Room == 1 ? null : "yalpa", B.Ev?.Id);
                // Takvim: yatış yalnız 2/4/6. tur sonunda ve bütçe içinde (son turda değil)
                Assert.LessOrEqual(t.Tilts, Baskin.TiltSchedule(B.MaxTurns).Count, "takvim dışı yatış");
                if (rep.Room == 1) Assert.AreEqual(0, t.Tilts + t.Slides.Values.Sum());
                Assert.AreEqual(0, t.EdgeMachines + t.EdgeTeam + t.EdgeOther + t.Detaches + t.DetachMiss.Count, "Yalpa'da kenar ve AYIR yok");
            }
            Assert.IsTrue(sim.ResolveRaid(result).Ok);
            TestContext.WriteLine($"tohum {seed}: oda {rooms.Count}, yatış {rooms.Sum(r => r.tally.Tilts)}, kayma {rooms.Sum(r => r.tally.Slides.Values.Sum())}, zafer {sim.S.RaidLog.Last().Success}");
        }

        /// <summary>Yalpa'nın kayması önlem defterinde itme sayılmaz: Core'un sayımı botun eşlemesiyle aynı (Camp8 tl = tlb).</summary>
        [Test]
        public void Bot_Yalpa_TallyNoPush()
        {
            int tilts = 0, slides = 0;
            for (uint seed = 1; seed <= 3; seed++)
            {
                var sim = Ordered(seed, "oc", "yalpa");
                Assert.AreEqual(6, sim.S.Raid.Params.Counters.Count, "defter açık");
                var rooms = Play(sim, Bot(), out var result);
                var core = FacilityCounters.RaidTally(sim.S.Raid.Reports, 1000);
                var bot = RaidBot.TallyMap(rooms.Select(x => x.tally), sim.S.Raid.Params.Counters, sim.B.facilityTallyAmbush);
                CollectionAssert.AreEqual(core, bot, "tohum " + seed);
                tilts += rooms.Sum(r => r.tally.Tilts);
                slides += rooms.Sum(r => r.tally.Slides.Values.Sum());
                Assert.IsTrue(sim.ResolveRaid(result).Ok);
            }
            Assert.Greater(tilts, 0, "üç tohumda yatış yok");
            Assert.Greater(slides, 0, "üç tohumda kayma yok");
        }

        // ---------- Efekt sayımı ----------
        [Test]
        public void CountFx_FallTiltSlide_Once()
        {
            // Katar: EMP makineyi kenardan düşürür ("fall"; kaynak emp)
            var p = Params(Wing("na_1"), 11);
            var A = Open(p, new List<RoomReport> { Rep(1) });
            var team = A.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).ToList();
            Flat(A, team.Select(u => u.Type == "emp" ? Pt(3, 3) : u.Type == "tek" ? Pt(6, 6) : Pt(5, 6)).ToArray());
            var e = AddMachine(A, "dron", Pt(3, 0));
            var emp = team.First(u => u.Type == "emp");
            var t = new RaidBot.Tally();
            Assert.IsTrue(Baskin.DoAct(A, emp.Id, 3, 1));
            Assert.IsTrue(e.Dead && A.FxList.Any(f => f.K == "fall" && f.Unit == e.Id));
            RaidBot.CountFx(A, t, "emp");
            RaidBot.CountFx(A, t, "emp");
            Assert.AreEqual(1, t.EdgeMachines, "düşen makine bir kez");
            Assert.AreEqual(1, Get(t.EdgeBy, "emp"));
            Assert.AreEqual(0, t.EdgeTeam);

            // Yalpa: 2. tur sonunda batıya yatış; üç uzman (biri sivil dolabına çarpar), hurda ve yük kayar
            var Y = Open(Params(Wing("oc_1"), 13), new List<RoomReport> { Rep(1) });
            Assert.AreEqual("yalpa", Y.Ev?.Id);
            Flat(Y, Pt(3, 3), Pt(1, 5), Pt(4, 1));
            Y.MaxTurns = 5; Y.StillTurns = 99; Y.Turn = 2;
            Baskin.CellAt(Y, 0, 5).O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            Baskin.CellAt(Y, 5, 7).O = new Baskin.Obj { K = "hurda", Hp = 1, Max = 1 };
            Y.CargoX = 6; Y.CargoY = 2;
            var ty = new RaidBot.Tally();
            Assert.IsTrue(Baskin.EndTurn(Y));
            RaidBot.CountFx(Y, ty, "evre");
            RaidBot.CountFx(Y, ty, "evre");
            Assert.AreEqual(1, ty.Tilts, "yatış bir kez");
            Assert.AreEqual(3, Get(ty.Slides, "p"), "üç uzman");
            Assert.AreEqual(1, Get(ty.Slides, "hurda"));
            Assert.AreEqual(1, Get(ty.Slides, "yuk"));
            Assert.AreEqual(1, ty.TiltHurtP, "çarpan uzman");
            var mover = Standing(Y).First();
            var dest = Baskin.Reachable(Y, mover).First(q => !Baskin.IsGateTile(Y, q.X, q.Y));
            Assert.IsTrue(Baskin.DoMove(Y, mover.Id, dest.X, dest.Y));
            RaidBot.CountFx(Y, ty);
            Assert.AreEqual(1, ty.Tilts, "yürüyüşten sonra yeniden sayılmaz");
            Assert.AreEqual(3, Get(ty.Slides, "p"));

            // AYIR: "decouple" bir kez
            var H = AtReturnGate(out _, out _, false);
            var td = new RaidBot.Tally();
            Assert.IsTrue(Baskin.Decouple(H));
            RaidBot.CountFx(H, td);
            RaidBot.CountFx(H, td);
            Assert.AreEqual(1, td.Detaches);
        }

        // ---------- Determinizm ----------
        [Test]
        public void Bot_KatarYalpa_Deterministic()
        {
            foreach (var (region, ev) in new[] { ("na", "katar"), ("oc", "yalpa") })
            {
                var a = Ordered(79, region, ev);
                var b = Ordered(79, region, ev);
                var ra = Play(a, Bot("hic", "kacis"), out var resA);
                var rb = Play(b, Bot("hic", "kacis"), out var resB);
                CollectionAssert.AreEqual(ra.Select(r => r.rep.Encode()).ToList(), rb.Select(r => r.rep.Encode()).ToList(), region);
                Assert.AreEqual(resA.Encode(), resB.Encode());
                Assert.IsTrue(a.ResolveRaid(resA).Ok);
                Assert.IsTrue(b.ResolveRaid(resB).Ok);
                Assert.AreEqual(State(a), State(b), region);
            }
        }
    }
}
