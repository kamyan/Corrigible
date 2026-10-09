using System;
using System.Globalization;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    public class SimulationTests
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [Test]
        public void Rng_MatchesBrowserPrototype()
        {
            // Değerler tarayıcı prototipindeki mulberry32'den (sim.js) alındı
            var r = new Rng(42);
            Assert.AreEqual(0.601103751920164, r.Next(), 1e-15);
            Assert.AreEqual(0.448290558997542, r.Next(), 1e-15);
            Assert.AreEqual(0.852465793490410, r.Next(), 1e-15);
            Assert.AreEqual(0.669734041439369, r.Next(), 1e-15);
        }

        [Test]
        public void SameSeedAndCommands_GiveSameGame()
        {
            string a = PlayScripted("manipulator", 1234), b = PlayScripted("manipulator", 1234);
            Assert.AreEqual(a, b);
        }

        [TestCase("manipulator", 7u, "acik@185 fon=350.837433")]
        [TestCase("kahin", 99u, "sessiz@230 fon=698.699413")]
        [TestCase("solucan", 12345u, "acik@192 fon=414.256816")]
        public void FullGame_MatchesBrowserPrototype(string type, uint seed, string expected)
        {
            // Aynı tohum ve aynı komutlarla prototipteki sim.js'in verdiği sonuç
            Assert.AreEqual(expected, PlayScripted(type, seed));
        }

        [Test]
        public void PassivePlayer_AlwaysLoses()
        {
            foreach (var type in GameData.AiTypes.Select(t => t.Id))
                for (uint seed = 1; seed <= 20; seed++)
                {
                    var sim = Simulation.NewGame(null, type, seed);
                    while (sim.S.Ending == null && sim.S.Day < 2000)
                    {
                        if (sim.S.PendingGift != null) sim.ResolveGift(Simulation.GiftAccept);
                        sim.Step(); sim.S.Events.Clear();
                    }
                    Assert.AreEqual(EndingKind.Loss, GameData.Ending(sim.S.Ending).Kind, $"{type} tohum {seed}: {sim.S.Ending}@{sim.S.Day}");
                }
        }

        /// <summary>
        /// İlk oyun testinde bulunan açık: "her yeri izle, görünen her sızmayı denetle" oyunu 41–60. günde Silme ile bitiriyordu.
        /// YZ çekirdeğiyle Silme en erken ~130. günde mümkün olmalı.
        /// </summary>
        [Test]
        public void MonitorEverythingAuditEverything_CannotEradicateEarly()
        {
            string[] cheap = { "af", "sa", "eeu", "sea", "oc", "ru", "me", "in", "weu", "jk", "na", "cn" };
            foreach (var type in GameData.AiTypes.Select(t => t.Id))
                for (uint seed = 1; seed <= 40; seed++)
                {
                    var sim = Simulation.NewGame(null, type, seed);
                    var s = sim.S;
                    while (s.Ending == null && s.Day < 120)
                    {
                        if (s.PendingGift != null) sim.ResolveGift(Simulation.GiftReject);
                        foreach (var id in cheap)
                            if (!s.Region(id).Monitored && s.Fon >= Simulation.MonitorCost(id)) sim.Act(Actions.Monitor, id);
                        foreach (var r in s.Regions.Where(r => (sim.Shown(r).V ?? 0) > 0.5).ToList())
                            if (s.Fon >= sim.AuditCost && s.Sg >= 1) sim.Act(Actions.Audit, r.Id);
                        if (s.CoreKnown != null && s.Sg >= sim.SalterSg(s.Region(s.CoreKnown))) sim.Act(Actions.KillSwitch, s.CoreKnown);
                        sim.Step(); s.Events.Clear();
                    }
                    Assert.AreNotEqual("silme", s.Ending, $"{type} tohum {seed}: Silme {s.Day}. günde");
                }
        }

        [Test]
        public void Alignment_CannotStartBeforeDetection()
        {
            var sim = Simulation.NewGame(null, "manipulator", 5);
            var res = sim.StartResearch("yorum");
            Assert.IsFalse(res.Ok);
            StringAssert.Contains("Gözlemlenmemiş", res.Msg);
        }

        [Test]
        public void Core_EscapesOnFirstDetection()
        {
            var sim = Simulation.NewGame(null, "manipulator", 11);
            var s = sim.S;
            string origin = s.Origin;
            s.Fon = 1000;
            foreach (var r in s.Regions) sim.Act(Actions.Monitor, r.Id);
            while (!s.Aware && s.Day < 200) { sim.Step(); s.Events.Clear(); }
            Assert.IsTrue(s.Aware);
            Assert.AreNotEqual(origin, s.Core, "YZ fark edildiği an çekirdeğini taşımalı");
            Assert.GreaterOrEqual(s.Region(s.Core).Inf, sim.B.coreSeed);
        }

        /// <summary>Kayıt (tohum + komut geçmişi) JSON'a yazılıp geri yüklendiğinde oyun birebir aynı durumda olmalı.</summary>
        [TestCase("manipulator", 21u)]
        [TestCase("kahin", 22u)]
        [TestCase("solucan", 23u)]
        public void SaveAndRestore_ReproducesExactState(string type, uint seed)
        {
            var profile = new PlayerProfile { runs = 2, karantina = 4, firstRegions = { "weu", "weu" }, lastBranch = GameData.BranchAlign };
            var sim = Simulation.NewGame(profile, type, seed);
            var s = sim.S;
            var rng = new Rng(seed * 7 + 1);
            string[] ids = GameData.Regions.Select(r => r.Id).ToArray();
            while (s.Ending == null && s.Day < 160)
            {
                if (s.PendingGift != null) sim.ResolveGift(rng.Pick(new[] { Simulation.GiftAccept, Simulation.GiftReject, Simulation.GiftControlled }));
                if (rng.Next() < 0.3) sim.Act(rng.Pick(Actions.All), rng.Pick(ids));
                if (s.Research.Cur == null && rng.Next() < 0.2) sim.StartResearch(rng.Pick(GameData.Research).Id);
                if (s.News.Count > 0 && rng.Next() < 0.05) sim.Verify(s.News[0].Id);
                if (rng.Next() < 0.02) sim.DismissAdvice();
                if (rng.Next() < 0.03) sim.SetResearchReserve(rng.Pick(Simulation.ResearchReserves));
                sim.Step(); s.Events.Clear();
            }
            sim.Record.day = s.Day;
            var json = UnityEngine.JsonUtility.ToJson(sim.Record);
            var restored = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(json));
            Assert.Greater(sim.Record.commands.Count, 10);
            Assert.AreEqual(Signature(sim), Signature(restored));
        }

        /// <summary>
        /// Son Hizalama araştırması günlük gelirden pahalıdır; acil durum fonu yokken fon sıfıra çakılır.
        /// Rezerv seçilince araştırma fonu rezervin altına indirmez, oyuncunun eylem parası kalır.
        /// </summary>
        [Test]
        public void ResearchReserve_KeepsFundsForActions()
        {
            double MinFon(double reserve)
            {
                var sim = Simulation.NewGame(null, "solucan", 3);
                var s = sim.S;
                s.Aware = true;
                s.Fon = 5;
                s.Research.Done.Add("yorum"); s.Research.Done.Add("duzelt");
                foreach (var r in s.Regions) { r.Monitored = true; r.Detected = true; }
                Assert.IsTrue(sim.SetResearchReserve(reserve).Ok);
                Assert.IsTrue(sim.StartResearch("kilit").Ok);
                double min = double.MaxValue;
                for (int i = 0; i < 30 && s.Ending == null; i++)
                {
                    if (s.PendingGift != null) sim.ResolveGift(Simulation.GiftReject);
                    sim.Step(); s.Events.Clear();
                    if (i >= 15) min = Math.Min(min, s.Fon);
                }
                return min;
            }
            Assert.Less(MinFon(0), 1, "Rezerv yokken Değer Kilidi fonu tüketmeli");
            Assert.GreaterOrEqual(MinFon(30), 30 - 1e-9, "Araştırma fonu rezervin altına indirmemeli");
            Assert.IsFalse(Simulation.NewGame(null, "solucan", 1).SetResearchReserve(42).Ok);
        }

        static string Signature(Simulation sim)
        {
            var s = sim.S;
            return $"{s.Day}|{s.Ending}|{s.Fon.ToString("R", Inv)}|{s.Sg.ToString("R", Inv)}|{s.Tek.ToString("R", Inv)}|{s.Core}|{s.CoreKnown}|{string.Join(",", s.Traits)}|{s.News.Count}|{s.Rng.State}|{s.ResearchReserve}|"
                + string.Join(";", s.Regions.Select(r => r.Inf.ToString("R", Inv) + "/" + r.Trust.ToString("R", Inv)));
        }

        // ---------- Deneysel mod: saha ekipleri, zaman ölçeği, baskın ----------

        [TestCase("manipulator", 7u, 1.5, "acik@275 fon=357.230322 raids=0")]
        [TestCase("kahin", 99u, 2, "acik@390 fon=506.263642 raids=1")]
        [TestCase("solucan", 12345u, 1.5, "acik@280 fon=367.141110 raids=0")]
        public void FullGameExp_MatchesBrowserPrototype(string type, uint seed, double timeScale, string expected)
        {
            // Beklenenler Prototype/parite.js çıktısıdır (aynı senaryo, deneysel mod açık)
            Assert.AreEqual(expected, PlayScriptedExp(type, seed, timeScale));
        }

        [TestCase(11, 5, 45, true, 1, "",
            "m4.1.3 m5.2.4 a5.5.4 m6.0.3 e m4.1.2 m5.1.3 a5.6.3 m6.2.4 a6.3.4 e m4.3.1 m5.1.2 m6.1.3 e m4.4.3 a4.3.3 m5.3.1 a5.5.1 m6.3.3 e m4.6.4 a4.6.3 m5.4.3 a5.6.3 m6.6.4 a6.6.3 e m4.5.4 a4.4.4 m6.6.5 a6.6.3 e",
            "lost|t7|c3|rs-383647171|dron74:1,bakim33:0x,dron14:1,tek54:2,emp43:0x,ana64:0x,dron44:0x,bakim62:1,dron02:2,bakim56:2,dron67:2|spawner60:2,core63:3,civ16:1,spawner77:2")]
        [TestCase(12345, 3, 60, false, 2, "aldatma",
            "m4.1.3 m5.2.4 a5.5.4 m6.3.3 e m4.3.4 a4.4.4 m5.4.3 a5.6.3 m6.4.4 a6.5.4 e m4.3.3 m5.4.5 m6.4.3 e m4.5.4 m5.6.4 a5.3.4 m6.7.4 a6.7.1 e m4.7.3 a4.6.3 m5.5.4 a5.3.4 m6.6.4 a6.6.3 e m5.4.3 a5.6.3 e",
            "lost|t7|c3|rs-199387546|dron44:0x,bakim34:0x,dron54:0x,tek73:0x,emp43:2,ana64:0x,dron04:1,dron34:0x,bakim73:1,dron74:0x,bakim65:1,dron46:1,dron20:2,dron70:2|spawner60:2,cool42:1,node63:3,civ16:1,spawner77:2")]
        [TestCase(777, 4, 30, true, 1, "yedek,kopya,hizlanma",
            "m4.1.3 m5.2.4 a5.4.4 m6.3.3 e m4.3.4 m5.4.3 a5.6.3 m6.4.4 a6.5.4 e m4.5.4 a4.6.4 m5.6.4 a5.6.6 m6.4.3 e m4.4.4 m5.5.4 a5.5.1 m6.6.4 a6.6.3 e m4.4.3 m5.4.4 a5.4.0 e",
            "lost|t6|c1|rs-1583376548|dron64:1,bakim74:2,dron44:0x,tek43:3,emp44:0x,ana64:0x,dron64:0x,bakim40:1,dron73:2,bakim45:1,dron20:0x,dron62:2|civ00:1,spawner60:2,cool42:1,core63:4,civ16:1,backup66:2,spawner77:2")]
        public void Raid_MatchesBrowserPrototype(int seed, int compute, double inf, bool isCore, int raidNumber, string traits, string moves, string expected)
        {
            // Hamleler ve beklenen son durum Prototype/parite.js çıktısıdır: aynı hamleler aynı savaşı vermeli
            var B = Baskin.Create(new RaidParams
            {
                Seed = seed, Compute = compute, Inf = inf, IsCore = isCore, RaidNumber = raidNumber,
                Traits = traits.Length == 0 ? new System.Collections.Generic.List<string>() : traits.Split(',').ToList(),
            });
            foreach (var m in moves.Split(' '))
            {
                if (m == "e") { Assert.IsTrue(Baskin.EndTurn(B), m); continue; }
                var p = m.Substring(1).Split('.').Select(int.Parse).ToArray();
                Assert.IsTrue(m[0] == 'm' ? Baskin.DoMove(B, p[0], p[1], p[2]) : Baskin.DoAct(B, p[0], p[1], p[2]), m);
            }
            Assert.AreEqual(expected, Baskin.Signature(B));
        }

        [Test]
        public void Raid_UndoRestoresExactState()
        {
            var B = Baskin.Create(new RaidParams { Seed = 99, Compute = 4, Inf = 40, IsCore = true });
            var u = B.Units.First(x => x.Type == "tek");
            string before = Baskin.Signature(B);
            var to = Baskin.Reachable(B, u)[0];
            Assert.IsTrue(Baskin.DoMove(B, u.Id, to.X, to.Y));
            Assert.AreNotEqual(before, Baskin.Signature(B));
            Assert.IsTrue(Baskin.Undo(B));
            Assert.AreEqual(before, Baskin.Signature(B));
        }

        [Test]
        public void Teams_AreRequiredForFieldWork()
        {
            var sim = Simulation.NewGame(null, "manipulator", 5, null, new ExpOptions());
            Assert.AreEqual(3, sim.S.Teams.Count);
            Assert.IsFalse(sim.Act(Actions.Monitor, "na").Ok, "Bölgede ekip yokken izleme ağı kurulamamalı");
            Assert.IsTrue(sim.Act(Actions.Monitor, "weu").Ok, "Karargâhta ekip var");
            Assert.IsTrue(sim.MoveTeam("bravo", "na").Ok);
            for (int i = 0; i < Simulation.TravelDays("weu", "na"); i++) { sim.Step(); sim.S.Events.Clear(); }
            Assert.AreEqual("na", sim.Team("bravo").At);
            Assert.IsTrue(sim.Act(Actions.Monitor, "na").Ok);
        }

        /// <summary>Deneysel modda da kayıt (ekip, baskın komutları dahil) birebir aynı duruma dönmeli.</summary>
        [TestCase("manipulator", 31u)]
        [TestCase("kahin", 32u)]
        public void SaveAndRestoreExp_ReproducesExactState(string type, uint seed)
        {
            var sim = Simulation.NewGame(null, type, seed, null, new ExpOptions { TimeScale = 1.5 });
            var s = sim.S;
            var rng = new Rng(seed * 3 + 1);
            string[] ids = GameData.Regions.Select(r => r.Id).ToArray();
            int raids = 0;
            while (s.Ending == null && s.Day < 260)
            {
                if (s.PendingGift != null) sim.ResolveGift(rng.Pick(new[] { Simulation.GiftAccept, Simulation.GiftReject }));
                if (rng.Next() < 0.15) sim.MoveTeam(rng.Pick(Simulation.TeamNames).ToLowerInvariant(), rng.Pick(ids));
                if (rng.Next() < 0.3) sim.Act(rng.Pick(Actions.All), rng.Pick(ids));
                if (rng.Next() < 0.02) sim.HireTeam();
                if (s.Research.Cur == null && rng.Next() < 0.2) sim.StartResearch(rng.Pick(GameData.Research).Id);
                foreach (var r in s.Regions)
                    if (sim.RaidCheck(r.Id) == null && rng.Next() < 0.5)
                    {
                        sim.StartRaid(r.Id);
                        sim.ResolveRaid(new RaidResult { Success = rng.Next() < 0.5, Collateral = (int)(rng.Next() * 3), Down = (int)(rng.Next() * 2) });
                        raids++;
                        break;
                    }
                sim.Step(); s.Events.Clear();
            }
            sim.Record.day = s.Day;
            var json = UnityEngine.JsonUtility.ToJson(sim.Record);
            var restored = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(json));
            Assert.Greater(raids, 0, "senaryo en az bir baskın içermeli");
            Assert.AreEqual(Signature(sim) + TeamSig(sim), Signature(restored) + TeamSig(restored));
        }

        static string TeamSig(Simulation sim) => string.Join(";", sim.S.Teams.Select(t => $"{t.Id}@{t.At}>{t.Dest}:{t.Eta}/{t.Busy}/{t.Down}")) + $"|r{sim.S.Raids}";

        /// <summary>Prototype/parite.js'teki deneysel senaryonun aynısı.</summary>
        static string PlayScriptedExp(string type, uint seed, double ts)
        {
            var sim = Simulation.NewGame(null, type, seed, null, new ExpOptions { Teams = true, TimeScale = ts, StartTeams = 3 });
            var s = sim.S;
            sim.MoveTeam("alfa", "na"); sim.MoveTeam("bravo", "cn");
            while (s.Ending == null && s.Day < 3000)
            {
                if (s.PendingGift != null) sim.ResolveGift(Simulation.GiftAccept);
                foreach (var id in new[] { "na", "cn" }) if (!s.Region(id).Monitored && sim.IdleTeamAt(id) != null) sim.Act(Actions.Monitor, id);
                if (s.Aware && s.Research.Cur == null) sim.StartResearch("yorum");
                if (s.CoreKnown != null)
                {
                    if (sim.RaidCheck(s.CoreKnown) == null) { sim.StartRaid(s.CoreKnown); sim.ResolveRaid(new RaidResult { Success = true, Collateral = 1, Down = 1, BackupDestroyed = true }); }
                    else { var c = sim.Team("charlie"); if (c != null && Simulation.TeamReady(c) && c.At != null && c.At != s.CoreKnown) sim.MoveTeam("charlie", s.CoreKnown); }
                }
                sim.Step(); s.Events.Clear();
            }
            return $"{s.Ending}@{s.Day} fon={s.Fon.ToString("F6", Inv)} raids={s.Raids}";
        }

        /// <summary>Prototipteki sim.js ile karşılaştırma için kullanılan sabit oyun senaryosu.</summary>
        static string PlayScripted(string type, uint seed)
        {
            var sim = Simulation.NewGame(null, type, seed);
            var s = sim.S;
            sim.Act(Actions.Monitor, "na"); sim.Act(Actions.Monitor, "cn");
            while (s.Ending == null && s.Day < 2000)
            {
                if (s.PendingGift != null) sim.ResolveGift(Simulation.GiftAccept);
                if (s.Aware && s.Research.Cur == null) sim.StartResearch("yorum");
                sim.Step(); s.Events.Clear();
            }
            return $"{s.Ending}@{s.Day} fon={s.Fon.ToString("F6", Inv)}";
        }
    }
}
