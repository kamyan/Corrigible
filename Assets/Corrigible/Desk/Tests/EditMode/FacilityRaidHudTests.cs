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
    /// Tesis baskınının HUD'u (aşama 3; Docs/TESIS-BASKINLARI-TASARIMI.md §3.3, §11.5): herkes kapıdayken EMRİ İLET “GEÇ” olur
    /// (altında GERİ ALINMAZ), kapıdaki uzmanın kartı soluk ve tıklanmaz, kanatlı antet, geçit satırı can taşımaz, donanımın
    /// damgası, son turun onay başlığı. Her testte eski tek tahtanın modeli (yeni alanlar boş) bugünkü hâli verir. Boş sahne
    /// açıkken koşar (Baskin.unity eklemeli açılır).
    /// </summary>
    public class FacilityRaidHudTests
    {
        static RaidViewModel Vm() => new RaidViewModel
        {
            Turn = 2, Turns = 6, Phase = "player", Civilians = 2, Trust = 11, TrustMax = 20, AbilityLabel = "DAR KORİDOR",
            Goal = "Amaç: ekibi geçide götür, sonra GEÇ", BlueprintSheet = 1, BlueprintRoom = "Kuzey kanadı",
            Targets = new[] { new RaidTargetModel { Name = "Çekirdek", Cell = "F4", Thumb = "thumb_cabinet_core", Hp = 4, Max = 4, Armor = true } },
            Team = new[] { 'T', 'E', 'A' }.Select(l => new RaidTeamModel { Letter = l, Name = l.ToString(), Hp = 3, Max = 3, Move = true, Ability = true }).ToArray(),
        };

        static T Field<T>(SerializedObject so, string name) where T : Object => (T)so.FindProperty(name).objectReferenceValue;
        static T Element<T>(SerializedObject so, string name, int i) where T : Object => (T)so.FindProperty(name).GetArrayElementAtIndex(i).objectReferenceValue;

        static void WithHud(System.Action<RaidHudView, SerializedObject> body)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            var lang = DeskLoc.Instance.Lang;
            try
            {
                var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RaidHudView>(true)).Single();
                body(hud, new SerializedObject(hud));
            }
            finally { DeskLoc.Instance.SetLanguage(lang); EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void CanPass_TurnsSendOrdersIntoGoThrough()
        {
            WithHud((hud, so) =>
            {
                var end = Field<TMPro.TMP_Text>(so, "endLabel");
                var under = Field<TMPro.TMP_Text>(so, "endLastTurn");
                // Eski tahta: CanPass false → bugünkü EMRİ İLET, son tur değilken alt satır yok
                hud.Render(Vm());
                Assert.AreEqual(Loc.T("EMRİ İLET"), end.text);
                Assert.IsFalse(under.gameObject.activeSelf);

                var vm = Vm(); vm.CanPass = true;
                hud.Render(vm);
                Assert.AreEqual(Loc.T("GEÇ"), end.text);
                Assert.IsTrue(under.gameObject.activeSelf, "GEÇ'in altında GERİ ALINMAZ");
                Assert.AreEqual(Loc.T("GERİ ALINMAZ"), under.text);

                // Son turda da GEÇ önce gelir; onay (EMİN MİSİN?) GEÇ'i örtmez
                vm = Vm(); vm.CanPass = true; vm.Turn = vm.Turns; vm.LastTurn = true; vm.EndConfirm = true;
                hud.Render(vm);
                Assert.AreEqual(Loc.T("GEÇ"), end.text);
                Assert.AreEqual(Loc.T("GERİ ALINMAZ"), under.text);

                DeskLoc.Instance.SetLanguage(DeskLang.En);
                vm = Vm(); vm.CanPass = true;
                hud.Render(vm);
                Assert.AreEqual("GO THROUGH", end.text, "İngilizcesi tabloda");
                Assert.AreEqual("NO UNDO", under.text);
                // Düzenleme modunda eklemeli açılan sahnenin düzeni hesaplanmamıştır (etiket genişliği 0, her metin taşar): önce
                // düzen kurulur, etiket HorizontalLayoutGroup'tan genişliğini alır, sonra otomatik punto ölçülür.
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)end.canvas.transform);
                end.ForceMeshUpdate(true, true); under.ForceMeshUpdate(true, true);
                Assert.Greater(((RectTransform)end.transform).rect.width, 0f, "düzen kuruldu");
                Assert.IsFalse(end.isTextOverflowing, "GO THROUGH düğmeye sığar");
                Assert.IsFalse(under.isTextOverflowing, "NO UNDO sığar");
            });
        }

        [Test]
        public void AtGateCard_IsDimmedAndNotClickable()
        {
            WithHud((hud, so) =>
            {
                var view = Element<RaidTeamCardView>(so, "team", 0);
                var card = new SerializedObject(view);
                var status = Field<TMPro.TMP_Text>(card, "status");
                var button = Field<UnityEngine.UI.Button>(card, "button");
                var group = Field<CanvasGroup>(card, "group");

                // Eski tahta: AtGate false → bugünkü hak satırı, tıklanır
                view.Render(new RaidTeamModel { Letter = 'T', Name = "T", Hp = 3, Max = 3, Move = true, Ability = true }, false);
                Assert.AreEqual(Loc.T("HAREKET + YETENEK"), status.text);
                Assert.IsTrue(button.interactable);

                view.Render(new RaidTeamModel { Letter = 'T', Name = "T", Hp = 3, Max = 3, Move = false, Ability = true, AtGate = true }, false);
                Assert.AreEqual(Loc.T("✓ KAPIDA"), status.text);
                Assert.AreEqual(RaidHudView.C("ink.green"), status.color);
                Assert.IsFalse(button.interactable, "kapıdaki uzman seçilmez");
                if (group != null) Assert.AreEqual(0.75f, group.alpha, 1e-4f, "kart soluk");

                // Önceki paftada düşen: DÜŞTÜ · PAFTA n; DownText yoksa bugünkü DÜŞTÜ
                view.Render(new RaidTeamModel { Letter = 'E', Name = "E", Max = 3, Down = true, DownText = Loc.F("DÜŞTÜ · PAFTA {0}", 1) }, false);
                Assert.AreEqual(Loc.F("DÜŞTÜ · PAFTA {0}", 1), status.text);
                view.Render(new RaidTeamModel { Letter = 'E', Name = "E", Max = 3, Down = true }, false);
                Assert.AreEqual(Loc.T("DÜŞTÜ"), status.text);
            });
        }

        [Test]
        public void Blueprint_WingHeading_FitsInBothLanguages()
        {
            WithHud((hud, so) =>
            {
                var view = Field<RaidBlueprintView>(so, "blueprint");
                Assert.IsNotNull(view, "HUD'un anteti bağlı");
                var bp = new SerializedObject(view);
                var heading = Field<TMPro.TMP_Text>(bp, "heading");
                var room = Field<TMPro.TMP_Text>(bp, "room");

                var vm = Vm(); vm.BlueprintSheet = 2;
                view.Render(vm);
                Assert.AreEqual(Loc.F("OZALİT KOPYA · PAFTA {0}/3", 2), heading.text, "eski tahta (Wing 0) bugünkü antet");
                Assert.AreEqual(Loc.F("{0:00} {1}", 2, vm.BlueprintRoom), room.text);

                vm.BlueprintWing = 1;
                view.Render(vm);
                Assert.AreEqual(Loc.F("KANAT {0:00} · PAFTA {1}/3", 1, 2), heading.text);
                StringAssert.StartsWith("KANAT 01 · PAFTA 2/3", heading.text);
                Assert.AreEqual(Loc.F("{0:00} {1}", 1, vm.BlueprintRoom), room.text, "oda satırında kanadın numarası");

                vm.BlueprintReturn = true;
                view.Render(vm);
                Assert.AreEqual(Loc.F("KANAT {0:00} · PAFTA {1}/3 · DÖNÜŞ", 1, 2), heading.text);

                foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(lang);
                    view.Render(vm);
                    if (lang == DeskLang.En) Assert.AreEqual("WING 01 · SHEET 2/3 · RETURN", heading.text);
                    heading.ForceMeshUpdate(true, true);
                    Assert.IsFalse(heading.isTextOverflowing, lang + ": " + heading.text);
                    Assert.AreEqual(1, heading.textInfo.lineCount, lang + ": antet tek satır");
                }
            });
        }

        [Test]
        public void GateTargetRow_HasNoHealth()
        {
            WithHud((hud, so) =>
            {
                var vm = Vm();
                vm.Targets = new[]
                {
                    new RaidTargetModel { Name = "İleri geçit", Cell = "1-H4/H5", Thumb = "thumb_cooling", Max = 0 },
                    new RaidTargetModel { Name = "Soğutma vanası", Cell = "1-D2", Thumb = "thumb_cooling", Max = 2, Broken = true, Stamp = Loc.T("KAPANDI") },
                };
                hud.Render(vm);
                Assert.IsFalse(Element<TMPro.TMP_Text>(so, "targetCan", 0).gameObject.activeSelf, "geçit satırında CAN yok");
                var stamps = so.FindProperty("brokenStamps");
                if (stamps.arraySize > 1)
                {
                    Assert.IsTrue(Element<TMPro.TMP_Text>(so, "brokenStamps", 1).gameObject.activeSelf);
                    Assert.AreEqual(Loc.T("KAPANDI"), Element<TMPro.TMP_Text>(so, "brokenStamps", 1).text, "donanımın kendi damgası");
                }

                // Eski tahta: Max > 0, Stamp null → CAN görünür, damga bugünkü kuraldan
                vm = Vm();
                vm.Targets = new[] { new RaidTargetModel { Name = "Çekirdek", Cell = "F4", Thumb = "thumb_cabinet_core", Hp = 0, Max = 4, Broken = true } };
                hud.Render(vm);
                Assert.AreEqual(Loc.T("KIRILDI"), Element<TMPro.TMP_Text>(so, "brokenStamps", 0).text);
                vm.Targets[0].Broken = false; vm.Targets[0].Hp = 4;
                hud.Render(vm);
                Assert.IsTrue(Element<TMPro.TMP_Text>(so, "targetCan", 0).gameObject.activeSelf);
            });
        }

        [Test]
        public void ConfirmTitle_ReplacesTheNoteHeading_OnlyWhenGiven()
        {
            WithHud((hud, so) =>
            {
                var panels = hud.Panels;
                Assert.IsNotNull(panels);
                var title = Field<TMPro.TMP_Text>(new SerializedObject(panels), "confirmTitle");
                var vm = Vm(); vm.EndConfirm = true; vm.LastTurn = true; vm.Turn = vm.Turns;
                vm.FatalWarning = Loc.T("Süre dolunca ekip geri çekilir ve baskın başarısız biter. Kapıdakiler de döner.");
                vm.ConfirmTitle = Loc.T("SON TUR · GERİ ÇEKİLME");
                panels.Render(vm);
                Assert.AreEqual(Loc.T("SON TUR · GERİ ÇEKİLME"), title.text);
                vm.ConfirmTitle = null;
                panels.Render(vm);
                Assert.AreEqual(Loc.T("KAÇIŞ · SON TUR"), title.text, "eski tahta: bugünkü başlık");
            });
        }

        [Test]
        public void DevicePieces_FallBackToTheCoolingUnit_NotTheCivilianServer()
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>("Assets/Corrigible/Settings/RaidPieceLibrary.asset");
            Assert.IsNotNull(lib);
            Assert.AreSame(lib.civilian, lib.Object("civ"), "eski nesneler aynı");
            foreach (var (kind, own, scale) in new[] { ("pano", lib.fusePanel, lib.fusePanelFallbackScale), ("anten", lib.antenna, lib.antennaFallbackScale), ("vana", lib.valve, lib.valveFallbackScale) })
            {
                if (own != null) { Assert.AreSame(own, lib.Object(kind), kind); Assert.AreEqual(Vector3.one, lib.FallbackScale(kind), kind); continue; }
                Assert.IsFalse(lib.HasOwnPiece(kind), kind);
                Assert.AreSame(lib.cooling, lib.Object(kind), kind + ": yedek parça soğutma ünitesi (sivil sunucu değil)");
                Assert.AreEqual(scale, lib.FallbackScale(kind), kind);
            }
        }
    }
}
