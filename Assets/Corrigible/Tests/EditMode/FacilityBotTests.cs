using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Baskın botunun tesis zinciri sürücüsü (aşama 2, <see cref="RaidBot.PlayFacility"/>): oda zincirini yalnız simülasyonun
    /// komutlarıyla baştan sona oynar, donanım politikalarına uyar ve deterministiktir. Tesis bot oyununun bir değişiklikte aynı
    /// kaldığı Mono'da Camp8 çıktısının bayt karşılaştırmasıyla sınanır (tek kipten beri Library/Calisma/taban21/kilit21.sh).
    /// </summary>
    public class FacilityBotTests
    {
        /// <summary>Testlerde seçenek sayısı küçük (süre); oyunun kendisi aynı yoldan gelir.</summary>
        static RaidBot Bot(string dev) => new RaidBot { Devices = dev, Alternatives = 3 };

        /// <summary>Baskını bot oynar; oda başına (rapor, sayaç) listesi döner, sonuç yazılmaz.</summary>
        static List<(RoomReport rep, RaidBot.Tally tally)> Play(Simulation sim, RaidBot bot, out RaidResult result)
        {
            var rooms = new List<(RoomReport, RaidBot.Tally)>();
            bot.OnRoom = (B, rep, t) =>
            {
                Assert.IsTrue(B.Phase == "pass" || B.Phase == "won" || B.Phase == "lost", "oda bitmedi: " + B.Phase);
                rooms.Add((rep, t));
            };
            result = bot.PlayFacility(sim);
            return rooms;
        }

        [Test]
        public void Bot_PlaysTheChain([Values("kir", "hic", "hep")] string dev, [Range(1, 4)] int seed)
        {
            var sim = Sim((uint)seed);
            int start = sim.Record.commands.Count;
            var rooms = Play(sim, Bot(dev), out var result);
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            Assert.IsTrue(rooms.Count >= 1 && rooms.Count <= 5, "oda sayısı " + rooms.Count);
            CollectionAssert.AreEqual(rooms.Select(r => r.rep.Encode()).ToList(), sim.S.Raid.Reports.Select(r => r.Encode()).ToList());
            Assert.IsTrue(rooms.Last().rep.Last);
            Assert.IsTrue(rooms.Take(rooms.Count - 1).All(r => !r.rep.Last));
            Assert.AreEqual(sim.FacilityRaidResult().Encode(), result.Encode());
            var res = sim.ResolveRaid(result);
            Assert.IsTrue(res.Ok, res.Msg);
            Assert.AreEqual(rooms.Count, sim.S.RaidLog.Last().Rooms.Count);
            Assert.AreEqual(rooms.Count, sim.Record.commands.Skip(start).Count(c => c.kind == "raid_room"));
            if (dev == "hic")
            {
                Assert.AreEqual(0, rooms.Sum(r => r.tally.DeviceHits), "hic: donanıma eylem yok");
                Assert.IsTrue(rooms.All(r => !r.tally.DeviceGoal));
            }
            // Dönüş odalarında ve hedef odasında donanım hedefi seçilmez
            Assert.IsTrue(rooms.All(r => !r.tally.DeviceGoal || !r.rep.Return && r.rep.Room < 3));
        }

        [Test]
        public void Bot_BreaksDevices_SomeOfTheTime()
        {
            int broken = 0, goals = 0;
            for (uint seed = 1; seed <= 12; seed++)
            {
                var sim = Sim(seed);
                var rooms = Play(sim, Bot("kir"), out var result);
                Assert.IsTrue(sim.ResolveRaid(result).Ok);
                broken += rooms.Count(r => r.rep.Device != "");
                goals += rooms.Count(r => r.tally.DeviceGoal);
            }
            TestContext.WriteLine($"kir, 12 tohum: donanım hedefi {goals} oda, kırılan {broken}");
            Assert.Greater(broken, 0, "kir 12 tohumda hiç donanım kırmadı");
        }

        [Test]
        public void Bot_Chain_IsDeterministic()
        {
            foreach (uint seed in new uint[] { 79, 3 })
            {
                var a = Sim(seed);
                var b = Sim(seed);
                var ra = Play(a, Bot("kir"), out var resA);
                var rb = Play(b, Bot("kir"), out var resB);
                CollectionAssert.AreEqual(ra.Select(r => r.rep.Encode()).ToList(), rb.Select(r => r.rep.Encode()).ToList(), "tohum " + seed);
                Assert.AreEqual(resA.Encode(), resB.Encode());
                Assert.AreEqual(a.S.Rng.State, b.S.Rng.State);
            }
        }

        // ---------- Aşama 6: hamleler ve düşen politikası ----------
        [Test]
        public void Bot_FallenPolicy_PlaysTheChain([Values("hep", "hic")] string fallen, [Range(1, 3)] int seed)
        {
            var sim = Sim((uint)seed);
            var bot = new RaidBot { Devices = "kir", Alternatives = 3, Fallen = fallen };
            var rooms = Play(sim, bot, out var result);
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            Assert.IsTrue(rooms.Count >= 1 && rooms.Count <= 5, "oda sayısı " + rooms.Count);
            CollectionAssert.AreEqual(rooms.Select(r => r.rep.Encode()).ToList(), sim.S.Raid.Reports.Select(r => r.Encode()).ToList());
            Assert.AreEqual(sim.FacilityRaidResult().Encode(), result.Encode());
            if (fallen == "hic") Assert.AreEqual(0, rooms.Sum(r => r.tally.Drag), "hiç taşıma: sürükleme yok");
            var res = sim.ResolveRaid(result);
            Assert.IsTrue(res.Ok, res.Msg);
        }

        [Test]
        public void Bot_UsesNewMoves_Deterministic()
        {
            int used = 0;
            foreach (uint seed in new uint[] { 79, 3 })
            {
                var a = Sim(seed);
                var b = Sim(seed);
                var ra = Play(a, new RaidBot { Alternatives = 3, Fallen = "hep" }, out var resA);
                var rb = Play(b, new RaidBot { Alternatives = 3, Fallen = "hep" }, out var resB);
                CollectionAssert.AreEqual(ra.Select(r => r.rep.Encode()).ToList(), rb.Select(r => r.rep.Encode()).ToList(), "tohum " + seed);
                Assert.AreEqual(resA.Encode(), resB.Encode());
                Assert.AreEqual(a.S.Rng.State, b.S.Rng.State);
                used += ra.Sum(r => r.tally.Ambush + r.tally.Rush + r.tally.Drag + r.tally.Throw + r.tally.Joint + r.tally.Vent);
            }
            Assert.Greater(used, 0, "bot tesis hamlelerini hiç kullanmadı");
        }

        /// <summary>Tesisin girişi, kaçış, makinesiz: kadronun 1. üyesi yere düştü, 0. üyesi yanında. "Hep taşı" gövdeyi tesisten
        /// çıkarır (Out), "hiç taşıma" bırakır; ikisinde de ayaktakiler çıkar ve baskın kazanılır.</summary>
        [Test]
        public void Bot_Hep_DragsBodyOut_Hic_LeavesIt([Values("hep", "hic")] string fallen)
        {
            int tested = 0;
            foreach (var wing in Wings.Where(w => w[0].Mission == "server").Take(4))
            {
                var p = Params(wing, 13, 1, false);
                var B = Open(p, new List<RoomReport>(), Bal(events: false));
                Calm(B);
                B.Escape = true;
                Assert.IsTrue(Baskin.IsFinalExit(B));
                var body = BySlot(B, 1);
                Down(B, body);
                Assert.IsTrue(Baskin.LyingBody(body));
                var tally = new RaidBot { Alternatives = 3, Fallen = fallen }.Play(B);
                Assert.AreEqual("won", B.Phase, wing[0].Id);
                if (fallen == "hep")
                {
                    Assert.IsTrue(body.Out, wing[0].Id + ": gövde tesisten çıkarıldı");
                    Assert.Greater(tally.Drag, 0);
                }
                else
                {
                    Assert.IsTrue(Baskin.LyingBody(body), wing[0].Id + ": gövde yerde kaldı");
                    Assert.AreEqual(0, tally.Drag);
                }
                tested++;
            }
            Assert.AreEqual(4, tested);
        }

        // ---------- Aşama 6 kapanışı: sayaç ve pusu hattı ----------
        /// <summary>G tahtası (olaysız, makinesiz), uzman 4 karelik düz boş hattın başında, hattın 2. karesinde 5 canlı bir dron; makineler
        /// yürümez.</summary>
        static (Baskin.BState B, Baskin.Unit U, Baskin.Pt At, int[] D, Baskin.Unit E) AmbushSetup()
        {
            for (int w = 0; w < Wings.Count; w++)
            {
                var B = Open(Params(Wings[w], 41, 1, false), new List<RoomReport>(), Bal(events: false));
                Calm(B);
                var line = FindLine(B, 4);
                if (line == null) continue;
                var (at, d) = line.Value;
                B.StillTurns = 99;
                var u = BySlot(B, 1);
                Place(u, at);
                var e = B.Units.First(v => v.Side == 'e');
                e.Dead = false; e.Type = "dron"; e.Hp = e.Max = 5; e.Armor = 0; e.Ice = 0; e.Frozen = false; e.Intent = null;
                Place(e, Step(at, d, 2));
                return (B, u, at, d, e);
            }
            Assert.Fail("düz hat yok");
            return default;
        }

        /// <summary>Efekt sayacı her efekti bir kez sayar: makine evresinin pusu isabeti, sonraki turun yürüyüşünden sonra listede dursa da
        /// yeniden sayılmaz (inceleme bulgusu 1).</summary>
        [Test]
        public void CountFx_EachEffectOnce()
        {
            var (B, u, at, d, e) = AmbushSetup();
            var tally = new RaidBot.Tally();
            var first = Step(at, d, 1);
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActAmbush, first.X, first.Y));
            RaidBot.CountFx(B, tally);
            Assert.IsTrue(Baskin.EndTurn(B));
            RaidBot.CountFx(B, tally);
            Assert.AreEqual(1, tally.AmbushHits, "isabet bir kez");
            Assert.AreEqual(4, e.Hp);
            var mover = Standing(B).First(v => v != u);
            var to = Baskin.Reachable(B, mover).First(t => !Baskin.IsGateTile(B, t.X, t.Y));
            Assert.IsTrue(Baskin.DoMove(B, mover.Id, to.X, to.Y));
            Assert.IsTrue(B.FxList.Any(f => f.K == "ambush-hit"), "yürüyüş listeyi temizlemez (sınamanın koşulu)");
            RaidBot.CountFx(B, tally);
            RaidBot.CountFx(B, tally);
            Assert.AreEqual(1, tally.AmbushHits, "yürüyüşten sonra yeniden sayılmaz");
            // Yeni bir makine evresinin isabeti yeniden sayılır
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActAmbush, first.X, first.Y));
            RaidBot.CountFx(B, tally);
            Assert.IsTrue(Baskin.EndTurn(B));
            RaidBot.CountFx(B, tally);
            Assert.AreEqual(2, tally.AmbushHits);
        }

        /// <summary>Botun pusu hattı (<see cref="RaidBot.LineHits"/>) motorun hattıyla (<see cref="Baskin.AmbushLine"/>) aynı yerde biter:
        /// söndürme bulutunda durur.</summary>
        [Test]
        public void LineHits_StopsInCloud_LikeAmbushLine([Values(false, true)] bool cloud)
        {
            var (B, u, at, d, e) = AmbushSetup();
            Place(e, Step(at, d, 3));
            if (cloud) { B.Ev = new Baskin.EvState { Id = "bulut" }; var c = Step(at, d, 2); Baskin.CellAt(B, c.X, c.Y).Cloud = true; }
            var reach = new HashSet<int> { e.Y * Baskin.N + e.X };
            bool bot = RaidBot.LineHits(B, at.X, at.Y, d[0], d[1], B.Moves.AmbushRange, reach);
            var first = Step(at, d, 1);
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActAmbush, first.X, first.Y));
            bool engine = Baskin.AmbushLine(B, u).Any(p => reach.Contains(p.Y * Baskin.N + p.X));
            Assert.AreEqual(engine, bot);
            Assert.AreEqual(!cloud, bot, cloud ? "bulut hattı keser" : "açık hat");
        }

        // ---------- Aşama 8: ekipman ve birimler (RaidBot.Gear) ----------
        /// <summary>Saha rollerine (kadro sırasıyla) verilen ekipman setleri: her tür en az bir sette.</summary>
        static readonly string[][] Kits =
        {
            new[] { FacilityGear.Wire, FacilityGear.Turret, FacilityGear.Clamp },
            new[] { FacilityGear.Charge, FacilityGear.Decoy, FacilityGear.Dog },
            new[] { FacilityGear.Winch, FacilityGear.Wire, FacilityGear.Dog },
        };

        /// <summary>Kadroya ekipman takılmış baskın (eşya panoya doğrudan yazılır; emir PlanningSquad'dan hakları kurar).</summary>
        static Simulation GearSim(uint seed, string[] kit) => Sim(seed, before: s =>
        {
            for (int i = 0; i < PlanningBoardData.FieldJobs.Length; i++)
                s.AddBoardEquipment(kit[i], s.Board.People.First(x => x.Alive && x.Job == PlanningBoardData.FieldJobs[i]).Id);
        });

        static int GearUses(IEnumerable<RaidBot.Tally> ts) =>
            ts.Sum(t => t.Wire + t.Plank + t.Charge + t.Turret + t.Decoy + t.Winch + t.Clamp + t.Bark + t.Fetch + t.Sniff + t.Command);

        [Test]
        public void Bot_GearPolicy_PlaysTheChain([Values("kullan", "hic")] string gear, [Range(1, 3)] int seed)
        {
            var sim = GearSim((uint)seed, Kits[seed - 1]);
            CollectionAssert.AreEqual(Kits[seed - 1], sim.S.Raid.Params.Squad.Select(q => q.Gear).ToList(), "emirde ekipman");
            var rooms = Play(sim, new RaidBot { Alternatives = 3, Gear = gear }, out var result);
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            CollectionAssert.AreEqual(rooms.Select(r => r.rep.Encode()).ToList(), sim.S.Raid.Reports.Select(r => r.Encode()).ToList());
            Assert.AreEqual(sim.FacilityRaidResult().Encode(), result.Encode());
            if (gear == "hic") Assert.AreEqual(0, GearUses(rooms.Select(r => r.tally)), "hic: ekipman ve birim eylemi yok");
            var res = sim.ResolveRaid(result);
            Assert.IsTrue(res.Ok, res.Msg);
        }

        [Test]
        public void Bot_UsesGear_Deterministic()
        {
            int used = 0;
            foreach (uint seed in new uint[] { 79, 3 })
                for (int k = 0; k < Kits.Length; k++)
                {
                    var a = GearSim(seed, Kits[k]);
                    var b = GearSim(seed, Kits[k]);
                    var ra = Play(a, new RaidBot { Alternatives = 3 }, out var resA);
                    var rb = Play(b, new RaidBot { Alternatives = 3 }, out var resB);
                    CollectionAssert.AreEqual(ra.Select(r => r.rep.Encode()).ToList(), rb.Select(r => r.rep.Encode()).ToList(), "tohum " + seed + " set " + k);
                    Assert.AreEqual(resA.Encode(), resB.Encode());
                    Assert.AreEqual(a.S.Rng.State, b.S.Rng.State);
                    used += GearUses(ra.Select(r => r.tally));
                }
            Assert.Greater(used, 0, "bot ekipmanı hiç kullanmadı");
        }

        /// <summary>Düz tahta, tek uzman (yürüyemez, eylemi duruyor), kelepçeli; dik komşusunda 3 canlı bir dron: bot kelepçeyi takar.</summary>
        [Test]
        public void Bot_Clamp_TakesAdjacentMachine()
        {
            var B = Open(Params(Wings[0], 13, 1, false), new List<RoomReport>(), Bal(events: false));
            var at = new Baskin.Pt(3, 3);
            Flat(B, at);
            var u = BySlot(B, 0);
            Gear(u, FacilityGear.Clamp, 1);
            var e = AddMachine(B, "dron", new Baskin.Pt(4, 3));
            var tally = new RaidBot.Tally();
            var bot = new RaidBot { Alternatives = 3 };
            bot.OnTurn = (st, plan) => { foreach (var s in plan) if (s.Kind == Baskin.ActClamp) tally.Clamp++; };
            u.Moved = true;
            // Tek tur: planı seç ve uygula (Play turu bitirir; sayaç plan kancasından)
            bot.Play(B);
            Assert.Greater(tally.Clamp, 0, "kelepçe takılmadı");
        }

        /// <summary>Düz tahta, tek uzman (yürüyemez), örme setli; üç kare ötede ona nişanlı, yürümeyen bir dron: bot atış hattına kalas
        /// kurar (pusu ya da tel değil: kalas atışı keser).</summary>
        [Test]
        public void Bot_Plank_CutsTheShotLine()
        {
            var B = Open(Params(Wings[0], 13, 1, false), new List<RoomReport>(), Bal(events: false));
            var at = new Baskin.Pt(3, 3);
            Flat(B, at);
            var u = BySlot(B, 0);
            Gear(u, FacilityGear.Wire, 2);
            var e = AddMachine(B, "dron", new Baskin.Pt(3, 6));
            e.Intent = new Baskin.Intent { Kind = "shoot", Dir = 0 };
            u.Moved = true;
            B.StillTurns = 99;                                   // makineler yürümez (doğrulamadaki evre hattı değiştirmesin)
            var plans = new List<RaidBot.Step>();
            var bot = new RaidBot { Alternatives = 3 };
            bot.OnTurn = (st, plan) => { if (plans.Count == 0) plans.AddRange(plan); };
            var dir = Baskin.Dirs[0];
            Assert.AreEqual((0, -1), (dir[0], dir[1]), "yön 0 yukarı (y−)");
            bot.Play(B);
            Assert.IsTrue(plans.Any(s => s.Kind == Baskin.ActPlank && s.AX == 3 && s.AY == 4), "kalas atış hattına kurulmadı: "
                + string.Join(",", plans.Select(s => (s.Kind ?? "-") + s.AX + s.AY)));
        }

        /// <summary>Gerçek giriş odası, makinesiz: tahtada köpek ve kurulu bir taret varken bot ekibi kapıya yürütür ve GEÇ verir (köpek ve
        /// aygıt uzman değildir: kapıda olmaları gerekmez).</summary>
        [Test]
        public void Bot_DogAndDevices_NotSpecialists()
        {
            int tested = 0;
            foreach (var wing in Wings.Take(6))
            {
                var B = Open(Params(wing, 13, 1, false), new List<RoomReport>(), Bal(events: false));
                Calm(B);
                B.MaxTurns = 30;
                var tiles = Enumerable.Range(0, Baskin.N * Baskin.N).Select(k => new Baskin.Pt(k % Baskin.N, k / Baskin.N));
                var free = tiles.Where(t => Baskin.CellAt(B, t.X, t.Y).T == "floor" && Baskin.CellAt(B, t.X, t.Y).O == null
                    && Baskin.UnitAt(B, t.X, t.Y) == null && !Baskin.IsGateTile(B, t.X, t.Y) && !B.Room.West.Any(w => w.X == t.X && w.Y == t.Y)).ToList();
                if (free.Count < 2) continue;
                var dog = AddDog(B, 0, free[0]);
                var g = AddGadget(B, "taret", free[free.Count - 1], Baskin.Dirs[0], 3);
                var tally = new RaidBot { Alternatives = 3 }.Play(B);
                Assert.AreEqual("pass", B.Phase, wing[0].Id + ": GEÇ");
                Assert.IsFalse(dog.AtGate || g.AtGate, "köpek ve aygıt kapıya girmez");
                tested++;
            }
            Assert.Greater(tested, 0);
        }

        /// <summary>Yeni efekt türleri bir kez sayılır: taret isabeti makine evresinde, sonraki yürüyüşten sonra yeniden sayılmaz.</summary>
        [Test]
        public void CountFx_NewKinds_Once()
        {
            var (B, u, at, d, e) = AmbushSetup();
            Gear(u, FacilityGear.Turret, 1);
            var to = Step(at, d, 1);
            var tally = new RaidBot.Tally();
            Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActTurret, to.X, to.Y));
            RaidBot.CountFx(B, tally);
            Assert.IsTrue(Baskin.EndTurn(B));
            RaidBot.CountFx(B, tally);
            Assert.AreEqual(1, tally.TurretHits, "isabet bir kez");
            var mover = Standing(B).First(v => v != u);
            var dest = Baskin.Reachable(B, mover).First(t => !Baskin.IsGateTile(B, t.X, t.Y));
            Assert.IsTrue(Baskin.DoMove(B, mover.Id, dest.X, dest.Y));
            Assert.IsTrue(B.FxList.Any(f => f.K == "turret-hit"), "yürüyüş listeyi temizlemez (sınamanın koşulu)");
            RaidBot.CountFx(B, tally);
            Assert.AreEqual(1, tally.TurretHits, "yürüyüşten sonra yeniden sayılmaz");
        }

        // ---------- Aşama 8: planlama botu (CampaignBot) ----------
        /// <summary>Bölge tamamlanınca bekleyen ödülü seçer (tarif: açılır, kazanım: eklenir); bekleyen yokken komut vermez.</summary>
        [Test]
        public void Plan_ChoosesReward([Values("tarif", "kazanim")] string odul)
        {
            var sim = ShelterEventKit.New(Bal(), 79);
            int before = sim.Record.commands.Count;
            var bot = new CampaignBot { Odul = odul };
            bot.ChooseReward(sim);
            Assert.AreEqual(before, sim.Record.commands.Count, "bekleyen ödül yokken komut yok");
            string region = CompleteRegion(sim);
            Assert.AreEqual(region, sim.Facility.PendingReward);
            bot.ChooseReward(sim);
            Assert.IsTrue(string.IsNullOrEmpty(sim.Facility.PendingReward));
            var (recipe, perk) = FacilityGear.Reward(region);
            if (odul == "tarif") { CollectionAssert.Contains(sim.Facility.Recipes, recipe); Assert.AreEqual(1, bot.RewardRecipe); }
            else { CollectionAssert.Contains(sim.Facility.Perks, perk); Assert.AreEqual(1, bot.RewardPerk); }
        }

        /// <summary>Açılan ekipman tarifinin iş emrini açar, girdilerini ayırır; ürün saha rolüne takılır ve sonraki baskının emrine girer.</summary>
        [Test]
        public void Plan_Facility_QueuesUnlockedRecipe([Values("tek", "donus")] string arac)
        {
            var sim = ShelterEventKit.New(Bal(), 79);
            string region = CompleteRegion(sim);
            var bot = new CampaignBot { Arac = arac };
            bot.ChooseReward(sim);
            string kind = FacilityGear.Reward(region).Recipe;
            Assume.That(FacilityGear.IsGear(kind), "ilk bölgenin tarifi temel ekipman");
            foreach (var m in PlanningBoardData.MaterialKinds) sim.AddBoardMaterial(m, 20);
            ShelterEventKit.Close(sim);
            for (int shift = 0; shift < 8 && !sim.Board.Equipment.Any(e => e.Kind == kind); shift++)
            {
                bot.Plan(sim);
                if (shift == 0)
                {
                    Assert.AreEqual(1, bot.Orders, "emir açıldı");
                    Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "board_queue" && c.a == kind));
                    Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "board_reserve"));
                }
                ShelterEventKit.Close(sim);
            }
            Assert.IsTrue(sim.Board.Equipment.Any(e => e.Kind == kind), "ekipman üretildi");
            bot.Plan(sim);
            var holder = sim.Board.People.First(p => sim.Board.Equipment.Any(e => e.Kind == kind && e.OwnerId == p.Id));
            CollectionAssert.Contains(PlanningBoardData.FieldJobs, holder.Job, "ekipman saha rolünde");
            Assert.IsTrue(sim.PlanningSquad().Any(q => q.Gear == kind && q.GearCharges != 0), "emirde ekipman");
        }

        // ---------- Aşama 10: önlem defteri (bot ve planlama) ----------
        /// <summary>"onlem" ödülü (K31): temel pakette tarif; cevap paketinde tarif yalnız cevabın sınıfında önlem varken ve temeli
        /// açıkken (Söküm'ün temeli Teknisyen kiti hep açık); alt bayrak kapalıyken önlem yok sayılır (kazanım).</summary>
        [Test]
        public void Plan_Onlem_AnswerOnlyAgainstACountermeasure()
        {
            var sim = ShelterEventKit.New(Bal(), 79);
            Assert.AreEqual(FacilityGear.ChoiceRecipe, CampaignBot.AnswerChoice(sim, "in"), "temel paket");
            Assert.AreEqual(FacilityGear.ChoicePerk, CampaignBot.AnswerChoice(sim, "cn"), "önlem yok");
            Assert.AreEqual(FacilityGear.ChoicePerk, CampaignBot.AnswerChoice(sim, "na"), "önlem yok (Söküm)");
            sim.Facility.Counter.AddRange(new[] { 1, 0, 0, 1, 0, 0 });
            sim.Facility.Tally.AddRange(new[] { 0, 0, 0, 0, 0, 0 });
            Assert.AreEqual(FacilityGear.ChoicePerk, CampaignBot.AnswerChoice(sim, "cn"), "ele önlemi var ama kelepçe tarifi kapalı");
            Assert.AreEqual(FacilityGear.ChoiceRecipe, CampaignBot.AnswerChoice(sim, "na"), "itme önlemi; Teknisyen kiti hep açık");
            Assert.AreEqual(FacilityGear.ChoicePerk, CampaignBot.AnswerChoice(sim, "ru"), "örme önlemi yok");
            sim.Facility.PendingReward = "in";
            var bot = new CampaignBot { Odul = "onlem" };
            bot.ChooseReward(sim);
            CollectionAssert.Contains(sim.Facility.Recipes, FacilityGear.Clamp);
            sim.Facility.PendingReward = "cn";
            bot.ChooseReward(sim);
            CollectionAssert.Contains(sim.Facility.Recipes, FacilityGear.AnsRelay, "önlem ve temel var: cevap tarifi");
            Assert.AreEqual(2, bot.RewardRecipe);
            Assert.AreEqual(1, bot.RewardAnswer);
            sim.Facility.PendingReward = "ru";
            bot.ChooseReward(sim);
            CollectionAssert.Contains(sim.Facility.Perks, FacilityGear.Fur, "önlemsiz sınıfta kazanım");
            Assert.AreEqual(1, bot.RewardPerk);
            // Alt bayrak kapalı: kademe okunmaz
            var bo = Bal();
            bo.facilityCountermeasures = false;
            var off = ShelterEventKit.New(bo, 79);
            off.Facility.Counter.AddRange(new[] { 1, 0, 0, 1, 0, 0 });
            Assert.AreEqual(FacilityGear.ChoicePerk, CampaignBot.AnswerChoice(off, "na"));
        }

        /// <summary>TAK=hic (K32): üretilen ekipman kimseye takılmaz, emre girmez; emir ve üretim sürer.</summary>
        [Test]
        public void Plan_TakeGearOff_ProducesButDoesNotEquip()
        {
            var sim = ShelterEventKit.New(Bal(), 79);
            string region = CompleteRegion(sim);
            var bot = new CampaignBot { Arac = "tek", TakeGear = false };
            bot.ChooseReward(sim);
            string kind = FacilityGear.Reward(region).Recipe;
            Assume.That(FacilityGear.IsGear(kind), "ilk bölgenin tarifi temel ekipman");
            foreach (var m in PlanningBoardData.MaterialKinds) sim.AddBoardMaterial(m, 20);
            ShelterEventKit.Close(sim);
            for (int shift = 0; shift < 8 && !sim.Board.Equipment.Any(e => e.Kind == kind); shift++)
            {
                bot.Plan(sim);
                ShelterEventKit.Close(sim);
            }
            Assert.IsTrue(sim.Board.Equipment.Any(e => e.Kind == kind), "ekipman üretildi");
            bot.Plan(sim);
            Assert.IsFalse(sim.Board.Equipment.Any(e => e.Kind == kind && e.OwnerId != null), "takılmadı");
            Assert.IsFalse(sim.PlanningSquad().Any(q => q.Gear == kind), "emirde yok");
            Assert.Greater(bot.Orders, 0);
        }

        /// <summary>Söküm anahtarı Teknisyen kitinin sayfasını değiştirir (aynı kimlik, +1 devre): bot eksik kitin hammaddesini yine koyar
        /// ve kit yeniden üretilir (aşama 8'de nrKit hep 0; K21).</summary>
        [Test]
        public void Plan_AfterSokum_TechnicianKitStillPrepared()
        {
            var sim = ShelterEventKit.New(Bal(), 79);
            sim.Facility.PendingReward = "na";
            Assert.IsTrue(sim.ChooseServerReward("na", FacilityGear.ChoiceRecipe).Ok);
            var page = sim.BoardRecipes.First(r => r.Id == "kit_tek");
            Assert.AreEqual(FacilityGear.AnsSokum, page.Answer, "kit sayfası cevap sürümü");
            sim.Board.Equipment.RemoveAll(e => e.Kind == "kit_tek");
            foreach (var m in PlanningBoardData.MaterialKinds) sim.AddBoardMaterial(m, 20);
            var bot = new CampaignBot { Arac = "yok" };
            for (int shift = 0; shift < 6 && !sim.Board.Equipment.Any(e => e.Kind == "kit_tek" && e.Charges > 0); shift++)
            {
                if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
                bot.Plan(sim);
                ShelterEventKit.Close(sim);
            }
            Assert.IsTrue(sim.Board.Equipment.Any(e => e.Kind == "kit_tek" && e.Charges > 0), "Teknisyen kiti yeniden üretildi");
            bot.Plan(sim);
            StringAssert.DoesNotContain("gerekiyor", sim.PlanningRaidCheck() ?? "");
        }

        /// <summary>Taslak emrin temeline cevap tarifi seçilince taslak silinir (K22); bot sonraki planda emri cevap sayfasının girdileriyle
        /// yeniden açar (fiş kilitlenmez).</summary>
        [Test]
        public void Plan_DraftCancelledByAnswer_OrderReopens()
        {
            var sim = ShelterEventKit.New(Bal(), 79);
            sim.Facility.PendingReward = "in";
            Assert.IsTrue(sim.ChooseServerReward("in", FacilityGear.ChoiceRecipe).Ok);
            foreach (var m in PlanningBoardData.MaterialKinds) sim.AddBoardMaterial(m, 20);
            var bot = new CampaignBot { Arac = "tek" };
            bot.Plan(sim);
            Assert.AreEqual(1, bot.Orders, "taslak emir açıldı");
            var draft = sim.Board.Orders.Single();
            Assert.AreEqual(FacilityGear.Clamp, draft.RecipeId);
            Assert.IsFalse(draft.Started);
            sim.Facility.PendingReward = "cn";
            var r = sim.ChooseServerReward("cn", FacilityGear.ChoiceRecipe);
            Assert.IsTrue(r.Ok, r.Msg);
            Assert.AreEqual(0, sim.Board.Orders.Count, "taslak iptal edildi: " + r.Msg);
            bot.Plan(sim);
            Assert.AreEqual(2, bot.Orders, "emir yeniden açıldı");
            var again = sim.Board.Orders.Single();
            var recipe = sim.BoardRecipes.Single(x => x.Id == again.RecipeId);
            Assert.AreEqual(FacilityGear.AnsRelay, recipe.Answer);
            foreach (var i in recipe.Inputs)
                Assert.AreEqual(i.Quantity, again.Reserves.Where(x => x.Kind == i.Kind).Sum(x => x.Quantity), "ayrılan " + i.Kind);
            StringAssert.DoesNotContain("adedi", sim.BoardPlanCheck() ?? "");
        }

        /// <summary>Core'un tahta sayımı (raporların sınıf sayaçları) botun eylem ve efekt sayaçlarından kurulan eşlemeyle aynı
        /// (<see cref="RaidBot.TallyMap"/>; Camp8'in tl ↔ tlb tutarlılığı). Pusu isabeti yalnız ölçüm anahtarıyla aygıta girer.</summary>
        [Test]
        public void Tally_CoreEqualsBotMap([Values(false, true)] bool ambush)
        {
            int total = 0;
            for (uint seed = 1; seed <= 3; seed++)
            {
                var kit = Kits[seed - 1];
                var sim = Sim(seed, b => b.facilityTallyAmbush = ambush, s =>
                {
                    for (int i = 0; i < PlanningBoardData.FieldJobs.Length; i++)
                        s.AddBoardEquipment(kit[i], s.Board.People.First(x => x.Alive && x.Job == PlanningBoardData.FieldJobs[i]).Id);
                });
                Assert.AreEqual(6, sim.S.Raid.Params.Counters.Count, "defter açık: sayım açık");
                var rooms = Play(sim, new RaidBot { Alternatives = 3 }, out var result);
                var core = FacilityCounters.RaidTally(sim.S.Raid.Reports, 1000);
                var bot = RaidBot.TallyMap(rooms.Select(x => x.tally), sim.S.Raid.Params.Counters, ambush);
                CollectionAssert.AreEqual(core, bot, "tohum " + seed);
                total += core.Sum();
                Assert.IsTrue(sim.ResolveRaid(result).Ok);
            }
            Assert.Greater(total, 0, "hiç sayım yok");
        }

        /// <summary>Koku yayıcı (köpek önlemi 1. kademe): köpek erimde susturulmuşken, yanında yürüyemeyen Teknisyen olan odada bot
        /// köpeği ilk turdan sonra susturulmuş bırakmaz (yayıcıyı kırar ya da köpeği erimden çıkarır) ve odaların bazısında yayıcıyı
        /// kırar (<see cref="RaidBot.Weights.Emitter"/>).</summary>
        [Test]
        public void Bot_BreaksTheEmitter_ThatSilencesTheDog()
        {
            int tested = 0, breaks = 0;
            foreach (var wing in Wings.Take(8))
            {
                var p = Params(wing, 13, 1, false);
                p.Counters = new List<int> { 0, 0, 0, 0, 1, 0 };
                var B = Open(p, new List<RoomReport>(), Bal(events: false));
                Assert.AreEqual(1, B.Moves.Emitter);
                Calm(B);
                B.StillTurns = 99;
                var tiles = Enumerable.Range(0, Baskin.N * Baskin.N).Select(k => new Baskin.Pt(k % Baskin.N, k / Baskin.N)).ToList();
                var em = tiles.FirstOrDefault(t => Baskin.CellAt(B, t.X, t.Y).O?.K == "yayici");
                Assert.AreEqual("yayici", Baskin.CellAt(B, em.X, em.Y).O?.K, wing[0].Id + ": odada yayıcı");
                var tek = B.Units.First(u => Baskin.IsSpecialist(u) && u.Type == "tek");
                var at = FreeNeighbor(B, em);
                if (at == null) continue;
                Place(tek, at.Value);
                var dogAt = tiles.Where(t => Baskin.CellAt(B, t.X, t.Y).T != "wall" && Baskin.CellAt(B, t.X, t.Y).O == null && Baskin.UnitAt(B, t.X, t.Y) == null
                        && !Baskin.IsGateTile(B, t.X, t.Y) && System.Math.Abs(t.X - em.X) + System.Math.Abs(t.Y - em.Y) == 2).Select(t => (Baskin.Pt?)t).FirstOrDefault();
                if (dogAt == null) continue;
                var dog = AddDog(B, tek.Slot, dogAt.Value);
                Assert.IsTrue(Baskin.EmitterBlocks(B, dog), "köpek susturuldu (ön koşul)");
                tek.Moved = true;
                // Köpeğin susturulduğu tur başları: bot ilk turda yayıcıyı kırar (yanındaki Teknisyen'in sabotajı ya da EMP) ya da
                // köpeği erimden çıkarır; sonraki turların başında köpek susturulmuş değildir
                int silenced = 0, turns = 0;
                var bot = new RaidBot { Alternatives = 3 };
                bot.OnTurn = (st, plan) => { turns++; if (st.Units.Any(d => d.Id == dog.Id && !d.Dead && Baskin.EmitterBlocks(st, d))) silenced++; };
                var tally = bot.Play(B);
                Assert.AreEqual(1, silenced, wing[0].Id + ": köpek yalnız ilk turda susturuldu (tur " + turns + ")");
                breaks += tally.EmitterBreaks;
                tested++;
            }
            TestContext.WriteLine($"{tested} oda, kırılan yayıcı {breaks}");
            Assert.Greater(tested, 0);
            Assert.Greater(breaks, 0, "bot hiç yayıcı kırmadı (yalnız köpeği uzaklaştırdı)");
        }

        // ---------- Aşama 11: kuşatma ve savunma (bot ve planlama) ----------
        /// <summary>Kanadı alır ve kuşatmayı kapanışta kurar (karar vardiyası bu vardiya).</summary>
        static Simulation SiegeSim(uint seed, Action<Simulation> before = null)
        {
            var sim = ShelterEventKit.New(Bal(), seed);
            CaptureNext(sim);
            before?.Invoke(sim);
            ForceSiege(sim);
            Assert.IsTrue(sim.SiegeDecisionDue);
            return sim;
        }

        static RaidBot DefenseBot(string cekil) => new RaidBot { Alternatives = 3, Retreat = cekil };

        /// <summary>Süren tesis baskınını giriş odasında geri çekilerek kaybeder (komutlarla; FacilitySiegeTests kalıbı).</summary>
        static void LoseRaid(Simulation sim)
        {
            var B = sim.OpenRaidRoom();
            Calm(B);
            Assert.IsTrue(Baskin.Retreat(B));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
            var r = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(r.Ok, r.Msg);
        }

        /// <summary>Savunmayı bot oynar (aynı zincir sürücüsü): "hic" giriş odasını tutar (terk 0, kapıya girmez), "hep" giriş ve ara
        /// odadan içeri çekilir (terk 2; ekip daha önce düştüyse daha az ve kayıp). Sonuç yazılır, günlük girdisi savunmadır.</summary>
        [Test]
        public void Bot_Defense_PlaysTheChain([Values("hic", "hep")] string cekil, [Range(1, 3)] int seed)
        {
            var sim = SiegeSim((uint)seed);
            Defend(sim);
            int start = sim.Record.commands.Count;
            var rooms = Play(sim, DefenseBot(cekil), out var result);
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            var p = sim.S.Raid.Params;
            var reps = sim.S.Raid.Reports.ToList();
            CollectionAssert.AreEqual(rooms.Select(r => r.rep.Encode()).ToList(), reps.Select(r => r.Encode()).ToList());
            Assert.AreEqual(sim.FacilityRaidResult().Encode(), result.Encode());
            int left = RaidChain.Abandoned(p, reps);
            var res = sim.ResolveRaid(result);
            Assert.IsTrue(res.Ok, res.Msg);
            var log = sim.S.RaidLog.Last();
            Assert.AreEqual(Simulation.DefenseMode, log.Mode);
            Assert.AreEqual(rooms.Count, sim.Record.commands.Skip(start).Count(c => c.kind == "raid_room"));
            Assert.AreEqual(log.Success ? Simulation.SiegeHeld : Simulation.SiegeLost, sim.Facility.Sieges.Last().Outcome);
            if (cekil == "hic")
            {
                Assert.AreEqual(0, left, "hic: terk yok");
                Assert.AreEqual(1, rooms.Count);
                Assert.AreEqual(0, rooms.Sum(r => r.tally.Gates), "hic: kapıya girilmez");
            }
            else
            {
                Assert.IsTrue(left == 2 || !log.Success && left < 2, "hep: terk " + left + ", tutuldu " + log.Success);
                Assert.IsTrue(rooms.Take(left).All(r => r.rep.End == "pass"), "terk edilen oda GEÇ ile biter");
            }
            TestContext.WriteLine($"tohum {seed} {cekil}: terk {left}, tutuldu {log.Success}, tur {result.Turns}");
        }

        [Test]
        public void Bot_Defense_Deterministic([Values("hic", "hep")] string cekil)
        {
            var a = SiegeSim(2);
            var b = SiegeSim(2);
            Defend(a);
            Defend(b);
            var ra = Play(a, DefenseBot(cekil), out var resA);
            var rb = Play(b, DefenseBot(cekil), out var resB);
            CollectionAssert.AreEqual(ra.Select(r => r.rep.Encode()).ToList(), rb.Select(r => r.rep.Encode()).ToList());
            Assert.AreEqual(resA.Encode(), resB.Encode());
            Assert.AreEqual(a.S.Rng.State, b.S.Rng.State);
        }

        /// <summary>Tutma ("hic"): hiçbir turun başında ya da planından sonra uzman kapıda değildir (kapı sığınak değildir; bütün ekip kapıda
        /// olsa tur yalnız GEÇ ile biterdi).</summary>
        [Test]
        public void Bot_Defense_Hold_NeverAllAtGate()
        {
            for (uint seed = 1; seed <= 3; seed++)
            {
                var sim = SiegeSim(seed);
                Defend(sim);
                var bot = DefenseBot("hic");
                int turns = 0;
                bot.OnTurn = (st, plan) =>
                {
                    turns++;
                    Assert.IsTrue(Baskin.IsDefense(st));
                    Assert.IsFalse(st.Units.Any(u => Baskin.IsSpecialist(u) && u.AtGate), "turun başında kapıda uzman");
                    var c = st.Clone();
                    foreach (var step in plan)
                    {
                        if (step.Kind != null) continue;
                        if (step.Move) Assert.IsFalse(Baskin.IsGateTile(c, step.MX, step.MY), "kapı karesine yürüyüş");
                    }
                };
                var rooms = Play(sim, bot, out var result);
                Assert.AreEqual(1, rooms.Count, "tohum " + seed);
                Assert.Greater(turns, 0);
                Assert.IsTrue(sim.ResolveRaid(result).Ok);
            }
        }

        /// <summary>Barikat turu (makinesiz ilk tur): sonraki tur gelecek dalga giriş karesinde hayalet makine sayılır (W.WaveGhost); örme
        /// setli kadro barikat turunda engel kurar. Hayaletsiz (0) barikat turunda engel adayı hiç yoktur (makine erişimi boş).</summary>
        [Test]
        public void Bot_Defense_BuildsInBarricadeTurn()
        {
            int built = 0, builtNoGhost = 0, boards = 0;
            foreach (uint seed in new uint[] { 1, 2, 3, 4, 5, 6 })
                foreach (double ghost in new[] { 1.0, 0.0 })
                {
                    var sim = SiegeSim(seed, s =>
                    {
                        foreach (var job in PlanningBoardData.FieldJobs)
                            s.AddBoardEquipment(FacilityGear.Wire, s.Board.People.First(x => x.Alive && x.Job == job).Id);
                    });
                    Defend(sim);
                    Assume.That(sim.S.Raid.Params.Squad.Count(q => q.Gear == FacilityGear.Wire) > 0, "emirde örme seti");
                    var B = sim.OpenRaidRoom();
                    Assert.IsTrue(Baskin.BarricadeTurn(B));
                    Assert.AreEqual(0, Baskin.Alive(B, 'e').Count, "barikat turu makinesiz");
                    Assert.IsTrue(Baskin.DefenseIncoming(B).Any(v => v.Turn == B.Turn + 1 && v.X >= 0), "sonraki tur dalga gelir");
                    var bot = new RaidBot { Alternatives = 3 };
                    bot.W.WaveGhost = ghost;
                    int n = 0;
                    // Yalnız barikat turunun planı (kopyada; plan seçilince durulur, gerçek zincir sürmez)
                    bot.OnTurn = (st, plan) =>
                    {
                        n = plan.Count(x => x.Kind == Baskin.ActWire || x.Kind == Baskin.ActPlank);
                        throw new OperationCanceledException();
                    };
                    Assert.Throws<OperationCanceledException>(() => bot.Play(B.Clone()));
                    if (ghost > 0) { built += n; boards++; } else builtNoGhost += n;
                }
            TestContext.WriteLine($"{boards} tahta, barikat turunda engel {built} (hayaletsiz {builtNoGhost})");
            Assert.AreEqual(0, builtNoGhost, "hayaletsiz barikat turunda engel adayı yok");
            Assert.Greater(built, 0, "bot barikat turunda hiç engel kurmadı");
        }

        /// <summary>Kuşatma kararı (CampaignBot.SiegeChoice): kurulduğu vardiyada (vardiya ortası, başarısız baskından) komut yok; karar
        /// vardiyasında "hep" savunur (siege_defend), "hic" bırakır (siege_yield) ve aynı vardiya o kanada baskın reddedilir.</summary>
        [Test]
        public void Plan_Siege_DefendOrYield([Values("hep", "hic")] string savun)
        {
            var sim = ShelterEventKit.New(Bal(), 79);
            var w = CaptureNext(sim);
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.B.liberationAlarmPerFailure;
            ShelterEventKit.FillSquad(sim);
            Assert.IsTrue(sim.StartLiberationRaid(sim.NextLiberationSector(w.Region).Id).Ok);
            LoseRaid(sim);
            Assert.IsNotNull(sim.PendingSiege, "başarısız baskın kuşatma kurdu");
            var bot = new CampaignBot { Savun = savun };
            int n0 = sim.Record.commands.Count;
            Assert.IsNull(CampaignBot.SiegeChoice(sim, savun), "kurulduğu vardiya");
            Assert.IsFalse(bot.Siege(sim));
            Assert.AreEqual(n0, sim.Record.commands.Count, "kurulduğu vardiyada komut yok");
            ShelterEventKit.Close(sim);
            ShelterEventKit.FillSquad(sim);
            Assert.IsTrue(sim.SiegeDecisionDue);
            Assert.AreEqual(savun == "hep" ? "defend" : "yield", CampaignBot.SiegeChoice(sim, savun));
            Assert.IsNull(CampaignBot.SiegeChoice(sim, "yok"), "yok: komut yok");
            bool defended = bot.Siege(sim);
            Assert.AreEqual(savun == "hep", defended);
            Assert.AreEqual(savun == "hep" ? "siege_defend" : "siege_yield", sim.Record.commands.Last().kind);
            if (savun == "hep")
            {
                Assert.AreEqual(1, bot.Defended);
                Assert.IsTrue(RaidChain.IsDefense(sim.S.Raid.Params));
                Assert.IsTrue(sim.ResolveRaid(DefenseBot("hic").PlayFacility(sim)).Ok);
            }
            else
            {
                Assert.AreEqual(1, bot.Yielded);
                Assert.IsFalse(w.Captured, "bırakılan kanat düştü");
                var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
                Assert.AreEqual(w.Id, next.Id, "düşen kanat mevcut bölgede sıradaki");
                Assert.IsTrue(sim.YieldedToday(next.Id));
                Assert.IsNotNull(sim.LiberationRaidCheck(next.Id), "aynı vardiya bırakılan kanada baskın yok (nrYield)");
            }
            Assert.IsNull(sim.PendingSiege);
            Assert.IsNull(CampaignBot.SiegeChoice(sim, savun), "karar verildi");
        }

        /// <summary>"hep": savunma verilemiyorsa (kit eksik: pano denetimi) bırakır ve reddi sayar (C defNoSquad).</summary>
        [Test]
        public void Plan_Siege_NoSquad_Yields()
        {
            var sim = SiegeSim(79);
            ShelterEventKit.FillSquad(sim);
            var tek = sim.Board.People.First(p => p.Job == "tek");
            sim.Board.Equipment.RemoveAll(x => x.OwnerId == tek.Id && x.Kind == "kit_tek");
            Assert.IsNotNull(sim.DefendSectorCheck());
            Assert.AreEqual("yield", CampaignBot.SiegeChoice(sim, "hep"));
            var bot = new CampaignBot { Savun = "hep" };
            Assert.IsFalse(bot.Siege(sim));
            Assert.AreEqual(1, bot.DefendRefused);
            Assert.AreEqual(1, bot.Yielded);
            Assert.AreEqual(0, bot.Defended);
            Assert.AreEqual("siege_yield", sim.Record.commands.Last().kind);
            Assert.AreEqual(Simulation.SiegeYielded, sim.Facility.Sieges.Last().Outcome);
        }

        /// <summary>Savunma alt bayrağı kapalıyken kuşatma kararı hiç komut vermez (anlık karşı saldırı).</summary>
        [Test]
        public void Plan_Siege_DefenseOff_NoCommand()
        {
            var b = Bal();
            b.facilityDefenseEnabled = false;
            var sim = ShelterEventKit.New(b, 79);
            CaptureNext(sim);
            sim.S.Alarm = Math.Max(0, sim.B.liberationAlarmThreshold - sim.AlarmPerShift);
            ShelterEventKit.Close(sim);
            Assert.IsNull(sim.PendingSiege);
            var bot = new CampaignBot();
            int n0 = sim.Record.commands.Count;
            Assert.IsNull(CampaignBot.SiegeChoice(sim, "hep"));
            Assert.IsFalse(bot.Siege(sim));
            Assert.AreEqual(n0, sim.Record.commands.Count);
        }

        /// <summary>Tekrar baskını izli tahtada (başarısız baskından sonra aynı kanat; kanadın odalarına ayrıca rastgele tel, kalas, hurda,
        /// yıkılmış duvar ve açık kepenk izi): bot zinciri istisnasız bitirir (engel yolu kesmez; Core'un bağ kuralı).</summary>
        [Test]
        public void Bot_Retry_WithMarks_PlaysChain([Range(1, 4)] int seed)
        {
            var sim = ShelterEventKit.New(Bal(), (uint)seed);
            ShelterEventKit.FillSquad(sim);
            var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsTrue(sim.StartLiberationRaid(sector.Id).Ok);
            LoseRaid(sim);
            ShelterEventKit.Close(sim);
            var rng = new Rng((uint)seed * 7919u + 3u);
            foreach (var room in sim.FacilityPlan.Wing(sector.Id))
            {
                var cells = sim.FacilityPlanCells(room);
                for (int k = 0; k < 10; k++)
                {
                    int x = (int)Math.Floor(rng.Next() * Baskin.N), y = (int)Math.Floor(rng.Next() * Baskin.N);
                    var c = cells[y, x];
                    string kind = c.Shutter ? FacilityMarks.Shutter : c.T == "wall" ? FacilityMarks.Wall
                        : new[] { FacilityMarks.Wire, FacilityMarks.Plank, FacilityMarks.Scrap }[k % 3];
                    sim.Facility.Marks.Add(new FacilityMark { Room = room.Id, Kind = kind, X = x, Y = y, Hp = 2, Max = 2, Day = sim.S.Day });
                }
            }
            ShelterEventKit.FillSquad(sim);
            ChooseReward(sim);
            Assert.IsTrue(sim.StartLiberationRaid(sector.Id).Ok, "tekrar baskını");
            Assert.Greater(sim.S.Raid.Params.Marks.Count, 0, "emirde iz");
            var rooms = Play(sim, Bot("kir"), out var result);
            Assert.IsTrue(sim.RaidRoomsClosed);
            Assert.IsTrue(sim.ResolveRaid(result).Ok);
            TestContext.WriteLine($"tohum {seed}: iz {sim.S.Raid?.Params.Marks.Count ?? sim.S.RaidLog.Last().LiberationReward.MarksNew}, oda {rooms.Count}, zafer {sim.S.RaidLog.Last().Success}");
        }

        // ---------- Tam harekât (uzun) ----------
        [Test, Explicit("Uzun: bir tam harekât (Camp8'in tesis kipiyle aynı iddia)")]
        public void Bot_FullCampaign()
        {
            var sim = ShelterEventKit.New(Bal(), 7);
            var bot = Bot("kir");
            var planner = new CampaignBot();
            int raids = 0;
            for (int shift = 0; shift < 120 && sim.S.Ending == null; shift++)
            {
                if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
                planner.ChooseReward(sim);
                planner.Plan(sim);
                // Aşama 11: kuşatma kararı ("hep"); savunma oynandıysa o vardiya baskın yok
                if (planner.Siege(sim))
                {
                    var dr = sim.ResolveRaid(bot.PlayFacility(sim));
                    Assert.IsTrue(dr.Ok, dr.Msg);
                }
                else if (sim.PlanningRaidCheck() == null && TryStartRaid(sim))
                {
                    var res = sim.ResolveRaid(bot.PlayFacility(sim));
                    Assert.IsTrue(res.Ok, res.Msg);
                    raids++;
                }
                if (sim.S.Ending != null) break;
                ShelterEventKit.Close(sim);
            }
            Assert.Greater(raids, 0);
            Assert.IsTrue(sim.S.RaidLog.All(l => l.Rooms.Count >= 1 && l.Rooms.Count <= 5));
            sim.Record.day = sim.S.Day;
            var r = Replay.Restore(sim.Record);
            Assert.AreEqual(0, r.ReplayFailures);
            Assert.AreEqual(State(sim), State(r));
            Assert.Greater(sim.Facility.Sieges.Count, 0, "harekât en az bir kuşatma görür");
            TestContext.WriteLine($"gün {sim.S.Day}, baskın {raids}, zafer {sim.S.RaidLog.Count(l => l.Success)}, kuşatma {sim.Facility.Sieges.Count}"
                + $" (savunma {planner.Defended}), son {sim.S.Ending ?? "-"}");
        }
    }
}
