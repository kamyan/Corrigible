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

        [Test]
        public void Hud_ShowsTrustAndFirstBrokenCivilian()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            try
            {
                var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RaidHudView>(true)).Single();
                // Tahtada beş sivil sunucu, HUD'da üç yuva: ilk kırılan da işaretlenir
                var vm = new RaidViewModel
                {
                    Turn = 1, Turns = 6, Phase = "player", Civilians = 5, CivilianBroken = 1, Trust = 9, TrustMax = 10,
                    Team = new[] { 'T', 'E', 'A' }.Select(l => new RaidTeamModel { Letter = l, Name = l.ToString(), Thumb = "thumb_standee_" + l, Hp = 3, Max = 3 }).ToArray(),
                };
                hud.Render(vm);
                var so = new SerializedObject(hud);
                Assert.AreEqual("GÜVEN 9", ((TMPro.TMP_Text)so.FindProperty("civilianLabel").objectReferenceValue).text);
                var icons = so.FindProperty("civilians");
                var shown = Enumerable.Range(0, icons.arraySize).Select(i => (UnityEngine.UI.Image)icons.GetArrayElementAtIndex(i).objectReferenceValue)
                    .Where(image => image.gameObject.activeSelf).Select(image => image.sprite.name).ToArray();
                Assert.AreEqual(1, shown.Count(n => n == "civ_broken_20"), string.Join(",", shown));
                Assert.AreEqual("civ_broken_20", shown.Last());
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
