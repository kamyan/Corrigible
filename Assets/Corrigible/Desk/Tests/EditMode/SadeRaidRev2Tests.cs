using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Baskın HUD'u, Revizyon 2 · S6: köşe panellerinin yeri (raid_tokens › layout1080 ve S6 notu), son tur (SON TUR damgası ve
    /// EMRİ İLET'te “SON TUR”), makine evresinde kilitli kâğıt, kilit tehdidi altındaki uzmanın kartı, etiketlerde makine türü
    /// işareti; S7 sonuç kartı.
    /// </summary>
    public class SadeRaidRev2Tests
    {
        static RaidViewModel Vm(int turn, int turns, string phase) => new RaidViewModel
        {
            Turn = turn, Turns = turns, LastTurn = turn >= turns, Phase = phase, Locked = phase != "player", Civilians = 3, Trust = 11, TrustMax = 20, IsLootMission = true,
            AbilityLabel = "DAR KORİDOR", Goal = "Amaç: kasadan ikmal al", MachineMove = 2, MachineMoves = 3,
            Targets = new[] { new RaidTargetModel { Name = "İkmal kasası", Cell = "F4", Thumb = "thumb_cabinet_core", Hp = 4, Max = 4 } },
            Team = new[] { 'T', 'E', 'A' }.Select(l => new RaidTeamModel { Letter = l, Name = l.ToString(), Hp = 3, Max = 3, Move = true, Ability = true }).ToArray(),
        };

        static T Field<T>(SerializedObject so, string name) where T : Object => (T)so.FindProperty(name).objectReferenceValue;

        [Test]
        public void Hud_LastTurn_StampsStripAndEndButton_MachinePhaseShowsLockedPaper()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            try
            {
                var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RaidHudView>(true)).Single();
                var so = new SerializedObject(hud);
                hud.Render(Vm(1, 8, "player"));
                Assert.IsFalse(Field<RectTransform>(so, "lastStamp").gameObject.activeSelf);
                Assert.IsTrue(Field<TMPro.TMP_Text>(so, "lastTwoLabel").gameObject.activeSelf, "SON 2 TUR önceden işaretli");
                Assert.AreEqual(Loc.F("{0} · {1}", "İkmal kasası", "F4"), ((TMPro.TMP_Text)so.FindProperty("targetNames").GetArrayElementAtIndex(0).objectReferenceValue).text,
                    "görev çipinde hedefin karesi");

                var last = Vm(8, 8, "player");
                last.Team[0].Threat = Loc.F("KİLİT TEHDİDİ · #{0}", 1);
                hud.Render(last);
                Assert.IsTrue(Field<RectTransform>(so, "lastStamp").gameObject.activeSelf, "son turda SON TUR damgası");
                var card = new SerializedObject(so.FindProperty("team").GetArrayElementAtIndex(0).objectReferenceValue);
                Assert.AreEqual(Loc.F("KİLİT TEHDİDİ · #{0}", 1), Field<TMPro.TMP_Text>(card, "status").text, "kilit niyetinin hedefindeki uzman (S6c)");
                Assert.AreEqual(RaidHudView.C("ink.red"), Field<TMPro.TMP_Text>(card, "status").color);
                Assert.AreEqual(Loc.T("SON TUR"), Field<TMPro.TMP_Text>(so, "lastStampText").text);
                Assert.IsTrue(Field<TMPro.TMP_Text>(so, "endLastTurn").gameObject.activeSelf, "EMRİ İLET'te SON TUR");
                var fill8 = (UnityEngine.UI.Image)so.FindProperty("turnFills").GetArrayElementAtIndex(7).objectReferenceValue;
                Assert.IsTrue(fill8.enabled);
                Assert.AreEqual(RaidHudView.C("turnStrip.lastCurrent"), fill8.color, "son turda hücre 8 kırmızı");

                hud.Render(Vm(3, 8, "enemy"));
                Assert.IsTrue(Field<GameObject>(so, "lockedPaper").activeSelf, "makine evresinde kilitli kâğıt");
                Assert.IsFalse(Field<UnityEngine.UI.Button>(so, "end").gameObject.activeSelf);
                Assert.AreEqual(Loc.F("girdi kilitli · {0}/{1} hamle", 2, 3), Field<TMPro.TMP_Text>(so, "lockedLine").text);
                Assert.AreEqual(Loc.T("MAKİNELER"), Field<TMPro.TMP_Text>(so, "phase").text);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void HudPanels_SitInTheirS6Corners_AndKeepClearOfTheBoard()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidHud.prefab");
            try
            {
                RectTransform Panel(string path) => (RectTransform)root.transform.Find("Corners/" + path);
                void At(RectTransform rt, Vector2 anchor, Vector2 pos, float width)
                {
                    Assert.AreEqual(anchor, rt.anchorMin, rt.name); Assert.AreEqual(pos, rt.anchoredPosition, rt.name);
                    Assert.AreEqual(width, rt.sizeDelta.x, 0.5f, rt.name);
                }
                At(Panel("Status"), new Vector2(0, 1), new Vector2(20, -16), 592);
                At(Panel("Buttons"), new Vector2(1, 1), new Vector2(-16, -16), 548);
                At(Panel("Command"), new Vector2(1, 0), new Vector2(-16, 20), 474);
                At(Panel("Ability card"), Vector2.zero, new Vector2(20, 117), 432);
                At(Panel("Team"), Vector2.zero, new Vector2(20, 10), 736);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void HudTexts_AndBoardLabels_AreAtLeast14Px()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidHud.prefab");
            try
            {
                var small = root.transform.Find("Corners").GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t => t.fontSize < 14)
                    .Select(t => AnimationUtility.CalculateTransformPath(t.transform, root.transform) + " " + t.fontSize).ToArray();
                Assert.IsEmpty(small, string.Join("\n", small));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            // Can etiketi: tür işareti 18 px, pip 11 px; sıra bayrağının rakamı 20 px (S6 › etiket, bayrak)
            var label = AssetDatabase.LoadAssetAtPath<RaidScreenLabel>("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidScreenLabel.prefab");
            var so = new SerializedObject(label);
            Assert.AreEqual(11, so.FindProperty("pipSize").floatValue);
            Assert.AreEqual(18, so.FindProperty("glyphSize").floatValue);
            Assert.AreEqual(24, so.FindProperty("healthHeight").floatValue);
            Assert.GreaterOrEqual(so.FindProperty("flagFontPixels").floatValue, 20);
            Assert.AreEqual(new Vector2(30, 28), so.FindProperty("flagSize").vector2Value);
        }

        [Test]
        public void ResultCard_SitsRightOfTheBoard_StampOnceInTheTitlesPlace()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidPanels.prefab");
            try
            {
                // Revizyon 2 · S7a/S7b: 500 px kart x 1290 · y 318; damga tek ve başlık yerinde, altında yer satırı ve satırlar
                var view = root.GetComponentInChildren<RaidPanelsView>(true);
                var so = new SerializedObject(view);
                var card = Field<RectTransform>(so, "resultCard");
                Assert.AreEqual(new Vector2(0, 1), card.anchorMin);
                Assert.AreEqual(new Vector2(1290, -318), card.anchoredPosition);
                Assert.AreEqual(500, card.sizeDelta.x, 0.5f);
                var vm = Vm(6, 8, "won");
                vm.Finished = true; vm.Success = true; vm.Locked = true;
                vm.ResultTitle = Loc.T("SEKTÖR GERİ ALINDI"); vm.ResultTurn = Loc.F("TUR {0}/{1}", 6, 8); vm.ResultPlace = "Doğu Avrupa · 02 Dosya katı";
                vm.ResultRows = new[]
                {
                    new RaidResultRow { Label = "İkmal kasası", Value = Loc.T("alındı"), Good = true },
                    new RaidResultRow { Label = Loc.T("Kırılan sivil sunucu"), Value = "0" },
                    new RaidResultRow { Label = Loc.T("Ekip"), Value = Loc.F("{0}/{1} ayakta", 3, 3) },
                };
                view.Render(vm);
                var memo = Field<GameObject>(so, "resultMemo");
                Assert.IsTrue(memo.activeSelf);
                var shown = memo.GetComponentsInChildren<TMPro.TMP_Text>(false).Select(t => t.text).ToList();
                Assert.AreEqual(1, shown.Count(t => t == vm.ResultTitle), "sonuç tek damgada söylenir: " + string.Join(" | ", shown));
                Assert.AreEqual(shown.Count, shown.Distinct().Count(), "aynı söz kartta iki kez geçmez: " + string.Join(" | ", shown));
                Assert.AreEqual(RaidHudView.C("ink.blue"), Field<TMPro.TMP_Text>(so, "resultTitle").color, "geri alınan sektör mavi");
                var values = so.FindProperty("resultValues");
                Assert.AreEqual(RaidHudView.C("ink.green"), ((TMPro.TMP_Text)values.GetArrayElementAtIndex(0).objectReferenceValue).color);
                Assert.IsFalse(((Component)values.GetArrayElementAtIndex(3).objectReferenceValue).gameObject.activeSelf, "boş satır gizli");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void EveryMachineType_HasALabelGlyph()
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>("Assets/Corrigible/Settings/RaidPieceLibrary.asset");
            foreach (var pair in Baskin.UnitTypes.Where(p => p.Value.Side == 'e'))
                Assert.IsNotNull(lib.Glyph(pair.Key), pair.Key + " için etiket işareti yok");
            Assert.IsNull(lib.Glyph("tek"), "uzman etiketinde tür işareti yok");
            foreach (var badge in new[] { lib.badgeClamp, lib.badgeWrench, lib.badgeShield, lib.badgeClone, lib.armorIcon }) Assert.IsNotNull(badge);
        }
    }
}
