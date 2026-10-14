using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>Kritik haberin öncelikli kâğıtları örtmeden, bir kez ve doğru satırda görünmesi.</summary>
    public sealed class CriticalNewsTests
    {
        static Simulation Scenario() => Simulation.NewGame(null, "manipulator", 19, new Balance(), null,
            new RuleOptions { Stage = 4, Events = false });

        static DeskViewModel Build(Simulation sim, DeskBridgeState state, bool raid = false) =>
            new SimDeskBuilder().Build(new DeskBuildInput
            {
                Sim = sim, State = state, RaidScene = raid, Info = new DeskGameInfo(),
                MapSpec = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/map.json").text),
            });

        [Test]
        public void CriticalNotice_IsDeferredByIntroSettingsEventRaidAndTutorial()
        {
            var sim = Scenario();
            var state = new DeskBridgeState { CriticalNewsId = "h5" };
            Assert.AreEqual("h5", Build(sim, state).CriticalNewsId);
            state.IntroOpen = true;
            Assert.IsNull(Build(sim, state).CriticalNewsId);
            state.IntroOpen = false; state.SettingsOpen = true;
            Assert.IsNull(Build(sim, state).CriticalNewsId);
            state.SettingsOpen = false;
            sim.S.PendingEvent = new PendingDilemma { Id = "secim", Region = "weu", Day = sim.S.Day };
            Assert.IsNull(Build(sim, state).CriticalNewsId);
            sim.S.PendingEvent = null;
            Assert.IsNull(Build(sim, state, true).CriticalNewsId);
            state.Tutorial = new TutorialVM { Kind = "step" };
            Assert.IsNull(Build(sim, state).CriticalNewsId);
            state.Tutorial = null;
            Assert.AreEqual("h5", Build(sim, state).CriticalNewsId, "Erteleme bildirimi tüketmez");
            sim.S.Ending = "acik";
            Assert.IsNull(Build(sim, state).CriticalNewsId);
            state.Reset();
            Assert.IsNull(state.CriticalNewsId, "Yeni/yüklenen oyunda eski uyarı açılmaz");
        }

        [Test]
        public void CriticalMetadata_DoesNotRevealHiddenNewsTruth()
        {
            var sim = Scenario();
            sim.S.News.Insert(0, new Headline { Id = "h5", Day = 0, Text = "test", Agency = "Atlas Haber", Kind = "alarm", Fake = true });
            var vm = Build(sim, new DeskBridgeState());
            var item = vm.News.First(n => n.Id == "h5");
            Assert.IsTrue(item.Critical);
            Assert.IsFalse(item.Fake, "Gizli sahte haber yalnız türü alarm olduğu için açığa çıkmamalı");
            Assert.AreEqual("report", item.Trust);
            Assert.IsNull(vm.CriticalNewsId, "Kaydı yeniden kurmak geçmiş alarmı yeni bildirim yapmaz");
        }

        [Test]
        public void NewsWindow_FocusesRequestedCriticalRow()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Windows/HABERLER.prefab");
            var go = Object.Instantiate(prefab);
            bool preview = DeskUi.EditPreview;
            DeskUi.EditPreview = true;
            try
            {
                var view = go.GetComponent<ContentWindowView>();
                var scroll = new SerializedObject(view).FindProperty("scroll").objectReferenceValue as ScrollRect;
                Assert.IsNotNull(scroll, "Kaydırma alanı prefab üzerinde bağlı olmalı");
                var vm = new DeskViewModel();
                for (int n = 0; n < 20; n++) vm.News.Add(new TelexItemVM { Id = "h" + n, Day = "G-001", Source = "Atlas Haber", Text = "Kritik haber satırı", Trust = "report", Critical = n == 15 });
                view.Bind(vm);
                view.FocusNews("h15");
                Assert.Greater(scroll.content.anchoredPosition.y, 0);
                float max = Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height);
                Assert.LessOrEqual(scroll.content.anchoredPosition.y, max + 0.01f);
                Assert.AreEqual(Vector2.zero, scroll.velocity);
                float before = scroll.content.anchoredPosition.y;
                view.FocusNews("missing");
                Assert.AreEqual(before, scroll.content.anchoredPosition.y);
            }
            finally { DeskUi.EditPreview = preview; Object.DestroyImmediate(go); }
        }
    }
}
