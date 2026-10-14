using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>F8 oyun köprüsü: harita geometrisi tasarım kuralıyla, oyun durumundan görünüm modeli, olay/hediye/oyun sonu, etki ayrıştırma.</summary>
    public class DeskBridgeTests
    {
        const string Root = "Assets/Corrigible/Desk";

        static JObject MapSpec() => JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/Mock/map.json").text);

        static DeskViewModel Build(Simulation sim, DeskBridgeState st = null, int speed = 0) =>
            new SimDeskBuilder().Build(new DeskBuildInput
            {
                Sim = sim, Speed = speed, DayProgress = 0.5f, SecondsPerDay = 0.9f, State = st ?? new DeskBridgeState(), MapSpec = MapSpec(),
            });

        static Simulation NewGame(uint seed = 7, bool teams = true) =>
            Simulation.NewGame(null, "manipulator", seed, new Balance(), teams ? new ExpOptions() : null, new RuleOptions { Stage = 4, Events = true });

        [Test]
        public void RouteCurve_ReproducesDesignRoutes()
        {
            // map.json routes[].build: kontrol orta noktadan yukarı normal × uzunluk × 0,10; uçlar 21 px kırpılmış
            var spec = MapSpec();
            var centers = spec["regions"].ToDictionary(r => (string)r["id"], r => new Vector2((float)r["center"][0], (float)r["center"][1]));
            foreach (var r in spec["routes"])
            {
                var (p0, c, p1) = SimDeskBuilder.RouteCurve(centers[(string)r["from"]], centers[(string)r["to"]]);
                var q = r["q"];
                Assert.AreEqual((float)q[0][0], p0.x, 1.2f, (string)r["id"]); Assert.AreEqual((float)q[0][1], p0.y, 1.2f, (string)r["id"]);
                Assert.AreEqual((float)q[1][0], c.x, 1.2f, (string)r["id"]); Assert.AreEqual((float)q[1][1], c.y, 1.2f, (string)r["id"]);
                Assert.AreEqual((float)q[2][0], p1.x, 1.2f, (string)r["id"]); Assert.AreEqual((float)q[2][1], p1.y, 1.2f, (string)r["id"]);
            }
        }

        [Test]
        public void NewGame_BuildsWholeDesk()
        {
            var vm = Build(NewGame());
            Assert.AreEqual("C1", vm.Screen);
            Assert.AreEqual(5, vm.Gauges.Count);
            Assert.AreEqual(12, vm.Map.Regions.Count);
            Assert.AreEqual(GameData.Links.Count(l => !l.Wrap), vm.Map.Routes.Count);
            Assert.AreEqual(2, vm.Map.EdgeRoutes.Count, "Pasifik bağlantısı iki kenar oku");
            Assert.IsTrue(vm.Map.Regions.All(n => n.Trust == "unknown" && n.Value == null), "başlangıçta hiçbir şey bilinmiyor");
            Assert.AreEqual(new Vector2(1138.5f, 524.1f), new Vector2(vm.Map.Regions.First(n => n.Id == "in").Center[0], vm.Map.Regions.First(n => n.Id == "in").Center[1]), "projeksiyon: Hindistan = tasarımdaki Güney Asya");
            Assert.AreEqual(5, vm.Research.Branches.Count);
            Assert.AreEqual(14, vm.Research.Branches.Sum(b => b.Projects.Count));
            Assert.IsTrue(vm.Research.Branches.SelectMany(b => b.Projects).All(p => !p.CanPause), "henüz yürüyen Hizalama projesi yok");
            Assert.AreEqual(Simulation.ResearchReserves.Length, vm.Research.EmergencyFund.Options.Count);
            Assert.AreEqual(3, vm.Map.Teams.Count, "deneysel mod: üç ekip karargâhta");
            Assert.IsNull(vm.Dossier);
            Assert.IsNull(vm.Event);
            Assert.IsNull(vm.GameOver);
            Assert.AreEqual(0, vm.Clock.MinutesPerSecond, "duraklatılmışken saat durur");
            Assert.AreEqual("12:00", vm.Clock.Time);
        }

        [Test]
        public void SelectedRegion_OpensDossierWithRuleChecks()
        {
            var sim = NewGame();
            var st = new DeskBridgeState { Selected = "cn" };
            var vm = Build(sim, st);
            var d = vm.Dossier;
            Assert.IsNotNull(d);
            Assert.AreEqual("cn", d.RegionId);
            Assert.AreEqual(4, d.Actions.Count);
            Assert.IsTrue(d.Actions[3].Danger);
            // Bölgede ekip yok: izleme ve denetim kapalı, saha düğmesi en yakın ekibi gönderir
            Assert.AreEqual("disabled", d.Actions[0].State);
            Assert.IsNotNull(d.Field);
            Assert.AreEqual("B", d.Field.Raid.Key);
            StringAssert.StartsWith("MoveTeam:", d.Field.Raid.Command);
            // Oyuncu göndereceği ekibi seçer: hazır her ekip için bir fiş (haritadaki harf · yol günü); en yakını mürekkep, B
            Assert.AreEqual(sim.S.Teams.Count(Simulation.TeamReady), d.Field.Send.Count, "karargâhtaki üç hazır ekip");
            Assert.IsTrue(d.Field.Send.All(c => c.Command.StartsWith("Team:Send:") && c.Command.EndsWith(":cn")), "komut ekibi ve bölgeyi taşır");
            Assert.AreEqual(1, d.Field.Send.Count(c => c.Kind == "ink-small" && c.Key == "B"), "yalnız en yakını B ile");
            Assert.AreEqual("Team:Send:" + sim.S.Teams[0].Id + ":cn", d.Field.Send.Single(c => c.Key == "B").Command, "eşit uzaklıkta kadro sırası");
            CollectionAssert.AreEqual(sim.S.Teams.Select(t => t.Name.Substring(0, 1).ToUpperInvariant()), d.Field.Send.Select(c => c.Label.Substring(0, 1)));
            Assert.IsTrue(vm.Map.Regions.Single(n => n.Id == "cn").Selected);
            Assert.IsNotNull(vm.Map.Selection);
        }

        [Test]
        public void ParseEffect_CoversWholeDeck()
        {
            var known = new HashSet<string> { "%65 ihtimalle çekirdeğin yeri ortaya çıkar", "karantina biter" };
            foreach (var d in Simulation.AllDilemmas)
                foreach (var c in d.Choices)
                {
                    var lines = SimDeskBuilder.ParseEffect(c.Effect, "Çin");
                    foreach (var l in lines.Where(x => x.Text != null))
                        Assert.IsTrue(known.Contains(l.Text), $"{d.Id}: ayrıştırılamayan parça «{l.Text}»");
                }
            var e = SimDeskBuilder.ParseEffect("−2 SG · bölgede bağımlılık −15, güven −10", "Çin");
            Assert.AreEqual(3, e.Count);
            Assert.AreEqual(("SG", -2f), (e[0].Metric, e[0].Value));
            Assert.AreEqual(("bağımlılık", -15f, "Çin"), (e[1].Metric, e[1].Value, e[1].Region));
            Assert.AreEqual(("güven", -10f, "Çin"), (e[2].Metric, e[2].Value, e[2].Region), "kapsam (bölgede) sonraki parçaya geçer");
            Assert.IsTrue(e[1].Good, "bağımlılık düşüşü kazançtır");
            Assert.IsFalse(e[2].Good);
            var r = SimDeskBuilder.ParseEffect("araştırma 3 gün geriler", null);
            Assert.AreEqual(-3f, r[0].Value);
            Assert.IsEmpty(SimDeskBuilder.ParseEffect("hiçbir şey olmaz", null));
        }

        [Test]
        public void Dilemma_RegionalAndGlobalCards()
        {
            var sim = NewGame();
            sim.S.PendingEvent = new PendingDilemma { Id = "secim", Region = "weu", Day = sim.S.Day };
            var vm = Build(sim);
            Assert.AreEqual("C3", vm.Screen);
            Assert.IsNotNull(vm.Event);
            Assert.AreEqual(2, vm.Event.Options.Count);
            Assert.AreEqual("weu", vm.Event.Spotlight.Region, "bölgeye bağlı kart: spot");
            Assert.AreEqual(2, vm.Event.Footer.Hints.Last().Keys.Count, "alt şerit tuşları seçenek sayısı kadar");
            Assert.IsTrue(vm.Event.Options[0].Costs.Any(l => l.Label.Contains("Siyasi")), "−5 SG bedel");

            sim.S.PendingEvent = new PendingDilemma { Id = "bagis", Region = sim.S.Regions[0].Id, Day = sim.S.Day };
            vm = Build(sim);
            Assert.IsNull(vm.Event.Spotlight, "küresel kart: spot yok");
            Assert.IsNull(vm.Event.String);
        }

        [Test]
        public void Gift_UsesEventPaperWithThreeChoices()
        {
            var sim = NewGame();
            sim.S.PendingGift = new PendingGift { Region = "af", Gift = GameData.Gifts[0] };
            var vm = Build(sim);
            Assert.AreEqual(3, vm.Event.Options.Count);
            Assert.AreEqual(new[] { Simulation.GiftAccept, Simulation.GiftControlled, Simulation.GiftReject }, SimDeskBuilder.GiftChoices);
            StringAssert.Contains("Yapılamaz", vm.Event.Options[1].Risk, "denetimli kabul Analog Yedekler ister");
        }

        [Test]
        public void GameOver_FromPlayedGame()
        {
            var sim = NewGame(3, false);
            for (int i = 0; i < 2000 && sim.S.Ending == null; i++)
            {
                if (sim.S.PendingGift != null) { sim.ResolveGift(Simulation.GiftReject); continue; }
                if (sim.S.PendingEvent != null) { if (!sim.ResolveEvent(0).Ok) sim.ResolveEvent(1); continue; }
                sim.Step();
            }
            Assert.IsNotNull(sim.S.Ending, "oyun bir sonla bitti");
            var st = new DeskBridgeState();
            var vm = Build(sim, st);
            Assert.AreEqual("C4", vm.Screen);
            var g = vm.GameOver;
            Assert.IsNotNull(g);
            Assert.AreEqual(4, g.Kpis.Count);
            Assert.GreaterOrEqual(g.Chart.Plot.DayMax, sim.S.Day);
            Assert.AreEqual(sim.S.Hist.Count > 0, g.Chart.Series.Real.Points.Count > 1);
            Assert.LessOrEqual(g.Timeline.Cards.Count, 7);
            Assert.AreEqual("result", g.Timeline.Cards.Last().Type, "son kart sonuç");
            Assert.AreEqual(g.Timeline.Cards.Count, g.Chart.Events.Count, "grafikteki numaralar kartlarla aynı");
            Assert.AreEqual(4, g.Endbar.Actions.Count);
            foreach (var t in g.Chart.Tags) Assert.That(t.X, Is.InRange(108f, 1812f), t.Text);

            st.Review = true;
            vm = Build(sim, st);
            Assert.IsNull(vm.GameOver, "günlükleri incele: rapor kapalı, masa gerçekle");
            Assert.AreEqual("ShowReport", vm.Advisor.Actions[0].Command);
            Assert.IsTrue(vm.Map.Regions.All(n => n.Trust == "verified"), "oyun sonunda harita gerçeği gösterir");
        }

        [Test]
        public void Messages_NewestFirstAndExpire()
        {
            var st = new DeskBridgeState();
            st.Push("a", 10); st.Push("b", 11); st.Push("a", 12);
            Assert.AreEqual(new[] { "a", "b" }, st.Messages.Select(m => m.Text).ToArray(), "en yeni önce, tekrar yok");
            st.Expire(16);
            Assert.AreEqual(new[] { "a" }, st.Messages.Select(m => m.Text).ToArray(), "4 günden eski düşer");
        }

        [Test]
        public void EditPreview_UsesOpeningGameWithoutWriting()
        {
            // Edit mod önizlemesi oyun sahnesinde Play'deki açılış masasını kurar: aynı bölgeler (aynı yerler), pencere düzeni;
            // tercihe, kayda ve profile yazmaz
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Root + "/Scenes/DeskGame.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
            string save = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
            var saveTime = System.IO.File.Exists(save) ? System.IO.File.GetLastWriteTimeUtc(save) : default;
            string type = PlayerPrefs.GetString("corrigible.aiType", null);
            try
            {
                var src = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SimDeskDataSource>(true)).Single();
                Assert.IsInstanceOf<IDeskPreviewSource>(src);
                var vm = src.BuildPreview(false);
                CollectionAssert.AreEqual(GameData.Regions.Select(r => r.Id), vm.Map.Regions.Select(n => n.Id), "oyunun bölgeleri");
                var expect = Build(NewGame()).Map.Regions;
                for (int i = 0; i < expect.Count; i++)
                    Assert.AreEqual(expect[i].Center, vm.Map.Regions[i].Center, "aynı projeksiyon: düğümler Play'deki yerde");
                Assert.IsNull(vm.Intro, "masa kipi: giriş kâğıdı kapalı");
                Assert.IsNull(vm.Dossier, "açılışta seçim yok: dosya kapalı");
                Assert.IsNotNull(src.BuildPreview(true).Intro, "giriş kipi");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
            Assert.AreEqual(type, PlayerPrefs.GetString("corrigible.aiType", null), "tercih yazılmadı");
            if (saveTime != default) Assert.AreEqual(saveTime, System.IO.File.GetLastWriteTimeUtc(save), "kayıt yazılmadı");
        }

        [Test]
        public void DeskGameScene_IsWired()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Root + "/Scenes/DeskGame.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var pres = roots.SelectMany(g => g.GetComponentsInChildren<DeskPresenter>(true)).Single();
                var so = new SerializedObject(pres);
                Assert.IsInstanceOf<SimDeskDataSource>(so.FindProperty("dataSource").objectReferenceValue);
                Assert.IsInstanceOf<SimDeskCommands>(so.FindProperty("commands").objectReferenceValue);
                Assert.IsTrue(roots.Any(g => g.GetComponent<Corrigible.Game.GameController>() != null));
                Assert.IsFalse(roots.SelectMany(g => g.GetComponentsInChildren<MockDeskDataSource>(true)).Any(), "oyun sahnesinde sahte kaynak yok");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
