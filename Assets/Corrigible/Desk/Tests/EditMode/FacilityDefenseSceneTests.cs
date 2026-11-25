using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 11, parça P4b (aşama 11 tasarımı §8.8, §8.10): savunmanın sahne ve asset bağları. Baskın HUD'unun tur şeridi on hücre
    /// (RaidHud.prefab'a Cell 9 ve Cell 10; hücre 9–10 <c>Turns ≤ 8</c> iken gizli: bugünkü baskın görünümü aynı), dalga işareti, İÇERİ
    /// ÇEKİL düğmesi (onayda EMİN MİSİN?), savunma anteti ve görev çipi, geri çekilme metni ve kurallar kartının savunma bölümü (iki dilde
    /// sığar); saha rölesinin parçası (<c>RaidPieceLibrary.relay</c>, kendi prefab'ı) ve küçük resmi (<c>thumb_relay</c>). Boş sahne açıkken
    /// koşar (Baskin.unity eklemeli açılır).
    /// </summary>
    public sealed class FacilityDefenseSceneTests
    {
        const string LibraryPath = "Assets/Corrigible/Settings/RaidPieceLibrary.asset";

        static T Field<T>(SerializedObject so, string name) where T : Object => (T)so.FindProperty(name).objectReferenceValue;
        static T Element<T>(SerializedObject so, string name, int i) where T : Object => (T)so.FindProperty(name).GetArrayElementAtIndex(i).objectReferenceValue;

        static RaidViewModel Vm(int turns) => new RaidViewModel
        {
            Turn = 2, Turns = turns, Phase = "player", Civilians = 2, Trust = 11, TrustMax = 20, AbilityLabel = "DAR KORİDOR",
            Goal = "Amaç: 10. tura dayan", BlueprintSheet = 1, BlueprintRoom = "Kuzey kanadı", BlueprintWing = 1,
            Targets = new[] { new RaidTargetModel { Name = "İleri geçit", Cell = "1-H4/H5", Thumb = "ic_arrow_s", Max = 0 } },
            Team = new[] { 'T', 'E', 'A' }.Select(l => new RaidTeamModel { Letter = l, Name = l.ToString(), Hp = 3, Max = 3, Move = true, Ability = true }).ToArray(),
        };

        static void WithHud(System.Action<RaidHudView, SerializedObject> body)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            var lang = DeskLoc.Instance.Lang;
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.Tr);
                var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RaidHudView>(true)).Single();
                body(hud, new SerializedObject(hud));
            }
            finally { DeskLoc.Instance.SetLanguage(lang); EditorSceneManager.CloseScene(scene, true); }
        }

        static bool CellOn(SerializedObject so, int i) => Element<Image>(so, "turnFills", i).transform.parent.gameObject.activeSelf;

        /// <summary>Ters sınama hedefi: eski prefab'ta şerit 8 hücreydi (savunmanın 9. ve 10. turu çizilmezdi).</summary>
        [Test]
        public void TurnStrip_TenCells_NinthAndTenthHiddenUpToEight()
        {
            WithHud((hud, so) =>
            {
                Assert.AreEqual(10, so.FindProperty("turnBoxes").arraySize, "şeritte on hücre");
                Assert.AreEqual(10, so.FindProperty("turnFills").arraySize);
                Assert.AreEqual(10, so.FindProperty("turnFrames").arraySize);
                var cells = Enumerable.Range(0, 10).Select(i => Element<Image>(so, "turnFills", i).transform.parent).ToList();
                Assert.AreEqual(10, cells.Distinct().Count(), "her hücre ayrı");
                Assert.AreEqual("Cell 9", cells[8].name);
                Assert.AreEqual("Cell 10", cells[9].name);
                Assert.AreSame(cells[7].parent, cells[9].parent, "aynı şerit (Cells)");
                Assert.AreEqual(cells[8].GetSiblingIndex() + 1, cells[9].GetSiblingIndex(), "Cell 10 Cell 9'un hemen ardından");
                for (int i = 0; i < 10; i++)
                {
                    Assert.IsTrue(Element<TMPro.TMP_Text>(so, "turnBoxes", i).transform.IsChildOf(cells[i]), "hücre " + (i + 1) + " yazısı kendi hücresinde");
                    Assert.IsTrue(Element<Image>(so, "turnFrames", i).transform.IsChildOf(cells[i]), "hücre " + (i + 1) + " çerçevesi kendi hücresinde");
                }
                Assert.IsFalse(cells[8].gameObject.activeSelf || cells[9].gameObject.activeSelf, "Cell 9/10 kapalı kaydedildi");

                // na hedef odası 9 tur (kısa nöbet; diğer hedefler 10): 9. hücre açık, 10. gizli
                hud.Render(Vm(9));
                Assert.IsTrue(CellOn(so, 8));
                Assert.IsFalse(CellOn(so, 9), "9 turda 10 gizli");
                // 8 ve altı turda (giriş/ara/dönüş 6, eski hedef 8) 9–10 gizli
                hud.Render(Vm(8));
                Assert.IsTrue(CellOn(so, 7));
                Assert.IsFalse(CellOn(so, 8) || CellOn(so, 9), "8 turda 9–10 gizli");
                hud.Render(Vm(6));
                Assert.IsFalse(Enumerable.Range(6, 4).Any(i => CellOn(so, i)), "6 turda 7–10 gizli");

                // Savunma: on tur ve dalga işaretleri
                var vm = Vm(10);
                vm.TurnEvents = new string[11];
                vm.TurnEvents[2] = vm.TurnEvents[5] = vm.TurnEvents[8] = FacilityDefenseRaidText.WaveGlyph;
                hud.Render(vm);
                Assert.IsTrue(Enumerable.Range(0, 10).All(i => CellOn(so, i)), "10 turda bütün hücreler");
                Assert.AreEqual("10", Element<TMPro.TMP_Text>(so, "turnBoxes", 9).text);
                Assert.AreEqual("2/10", Field<TMPro.TMP_Text>(so, "turnNumber").text);
                Assert.AreEqual(Loc.F("{0}<size=55%>{1}</size>", 5, "D"), Element<TMPro.TMP_Text>(so, "turnBoxes", 4).text, "dalga turunun işareti");
                Assert.AreEqual("4", Element<TMPro.TMP_Text>(so, "turnBoxes", 3).text);
                // 1080 sığma: on hücre 8 hücrelik şeridin genişliğine sığar (hücreler daralır), evre çipi yerinde kalır
                float Right(RectTransform rt) { var c = new Vector3[4]; rt.GetWorldCorners(c); return c[2].x; }
                var sheet = (RectTransform)cells[0].parent.parent;
                var phase = Field<TMPro.TMP_Text>(so, "phase").rectTransform;
                void Layout() { Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)hud.transform); LayoutRebuilder.ForceRebuildLayoutImmediate(sheet.parent as RectTransform ?? sheet); }
                Layout();
                float right10 = Right((RectTransform)cells[9]), phase10 = Right(phase), width10 = ((RectTransform)cells[9]).rect.width;
                hud.Render(Vm(8));
                Layout();
                float right8 = Right((RectTransform)cells[7]), phase8 = Right(phase), width8 = ((RectTransform)cells[7]).rect.width;
                Assert.Greater(width10, 0f, "düzen kuruldu");
                Assert.Less(width10, width8, "10 turda hücreler daralır");
                Assert.AreEqual(right8, right10, 1.5f * Mathf.Abs(sheet.lossyScale.x), "on hücre 8 hücrenin genişliğinde");
                Assert.AreEqual(phase8, phase10, 1.5f * Mathf.Abs(sheet.lossyScale.x), "evre çipi yerinde (SIRA SENDE örtülmez)");
                Assert.AreEqual(34f, width8, 0.5f, "8 turda hücre prefab'daki genişlikte");
                // na hedef odası (9 tur): dokuz hücre de aynı genişliğe sığar, evre çipi yerinde
                hud.Render(Vm(9));
                Layout();
                Assert.AreEqual(right8, Right((RectTransform)cells[8]), 1.5f * Mathf.Abs(sheet.lossyScale.x), "dokuz hücre 8 hücrenin genişliğinde");
                Assert.AreEqual(phase8, Right(phase), 1.5f * Mathf.Abs(sheet.lossyScale.x), "9 turda evre çipi yerinde");
            });
        }

        [Test]
        public void FallBackButton_DefenceHeading_RetreatBody_Rules_FitBothLanguages()
        {
            WithHud((hud, so) =>
            {
                var end = Field<TMPro.TMP_Text>(so, "endLabel");
                var under = Field<TMPro.TMP_Text>(so, "endLastTurn");
                var objective = Field<TMPro.TMP_Text>(so, "objective");
                var vm = Vm(10); vm.CanPass = true; vm.Defense = vm.BlueprintDefense = true; vm.Place = "DOSYA KATI";
                vm.PassLabel = FacilityDefenseRaidText.PassLabel; vm.PassNote = FacilityDefenseRaidText.PassNote(1);
                vm.RetreatBody = FacilityDefenseRaidText.RetreatBody(2); vm.RulesDefense = FacilityDefenseRaidText.Rules(10, 1);
                hud.Render(vm);
                Assert.AreEqual("İÇERİ ÇEKİL", end.text);
                Assert.AreEqual("güven −1", under.text);
                Assert.AreEqual("DOSYA KATI · SAVUNMA", objective.text);
                vm.EndConfirm = true;
                hud.Render(vm);
                Assert.AreEqual(Loc.T("EMİN MİSİN?"), end.text, "ilk basış onay sorar");

                var view = Field<RaidBlueprintView>(so, "blueprint");
                var heading = Field<TMPro.TMP_Text>(new SerializedObject(view), "heading");
                var panels = new SerializedObject(hud.Panels);
                var retreatBody = Field<TMPro.TMP_Text>(panels, "retreatBody");
                var rule6 = Element<TMPro.TMP_Text>(panels, "ruleRows", 5);
                foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(lang);
                    vm.EndConfirm = false; vm.PassLabel = FacilityDefenseRaidText.PassLabel; vm.PassNote = FacilityDefenseRaidText.PassNote(1);
                    vm.RetreatBody = FacilityDefenseRaidText.RetreatBody(2); vm.RulesDefense = FacilityDefenseRaidText.Rules(10, 1);
                    vm.RetreatArmed = true; vm.OpenPanel = null;
                    hud.Render(vm);
                    view.Render(vm);
                    Assert.AreEqual(lang == DeskLang.Tr ? "KANAT 01 · PAFTA 1/3 · SAVUNMA" : "WING 01 · SHEET 1/3 · DEFENCE", heading.text);
                    Assert.AreEqual(vm.RetreatBody, retreatBody.text, lang + ": geri çekilme metni savunmanın");
                    vm.RetreatArmed = false; vm.OpenPanel = "rules";
                    hud.Render(vm);
                    Assert.AreEqual(vm.RulesDefense, rule6.text, lang + ": kurallar kartında savunma bölümü");
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)end.canvas.transform);
                    foreach (var t in new[] { end, under, heading, objective })
                    {
                        t.ForceMeshUpdate(true, true);
                        Assert.IsFalse(t.isTextOverflowing, lang + ": " + t.name + " = " + t.text);
                    }
                    Assert.AreEqual(1, heading.textInfo.lineCount, lang + ": antet tek satır");
                    if (lang == DeskLang.En) Assert.AreEqual("FALL BACK", end.text);
                }
            });
        }

        [Test]
        public void Relay_OwnPiece_Thumb()
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);
            Assert.IsNotNull(lib.relay, "RaidPieceLibrary.relay atanmış");
            Assert.AreEqual("Assets/Corrigible/Desk/Prefabs/Raid/Gear/Relay.prefab", AssetDatabase.GetAssetPath(lib.relay));
            Assert.IsTrue(lib.HasOwnPiece(Baskin.RelayKind));
            Assert.AreSame(lib.relay, lib.Object(Baskin.RelayKind), "röle sivil sunucu çizilmez");
            Assert.AreNotSame(lib.civilian, lib.Object(Baskin.RelayKind));
            Assert.AreEqual(Vector3.one, lib.FallbackScale(Baskin.RelayKind));
            Assert.IsNotNull(lib.relay.transform.Find("Visual/Mast"), "anten direği");
            Assert.IsNotNull(lib.relay.GetComponent<BoxCollider>(), "tıklanır");

            var empty = ScriptableObject.CreateInstance<RaidPieceLibrary>();
            try
            {
                Assert.IsFalse(empty.HasOwnPiece(Baskin.RelayKind));
                Assert.AreEqual(empty.relayFallbackScale, empty.FallbackScale(Baskin.RelayKind), "atanmamışsa yedek ölçek");
            }
            finally { Object.DestroyImmediate(empty); }

            CollectionAssert.Contains(SiginakTextureBaker.PieceThumbNames, "thumb_relay");
            Assert.IsTrue(DeskSprites.Instance.TryGet("thumb_relay", out var entry) && entry.sprite != null, "küçük resim kayıtta");
        }
    }
}
