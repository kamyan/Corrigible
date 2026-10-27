using System.Linq;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Baskin sahnesinin bağlantıları ve kural/lejant metinlerinin çevrilmesi.</summary>
    public class SadeRaidStateTests
    {
        [Test]
        public void RuleAndLegendKeysAreTranslated()
        {
            Assert.AreNotEqual("rules.1", RaidText.Get("rules.1"));
            Assert.AreNotEqual("legend.armor", RaidText.Get("legend.armor"));
        }

        [Test]
        public void LiveScene_UsesSerializedSadeSourceAndCommands()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                var live = all.OfType<LiveRaidDataSource>().Single();
                var hud = all.OfType<RaidHudView>().Single();
                var commands = all.OfType<RaidCommands>().Single();
                Assert.AreSame(live, new SerializedObject(hud).FindProperty("dataSource").objectReferenceValue);
                Assert.AreSame(commands, new SerializedObject(hud).FindProperty("commandSource").objectReferenceValue);
                Assert.AreSame(live, new SerializedObject(commands).FindProperty("source").objectReferenceValue);
                Assert.IsNotNull(new SerializedObject(hud).FindProperty("panels").objectReferenceValue);
                Assert.AreEqual(1, all.OfType<RaidEventSystemGate>().Count());
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
