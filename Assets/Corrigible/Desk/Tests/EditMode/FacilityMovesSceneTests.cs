using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis hamlelerinin HUD'u Baskin sahnesinde (aşama 7; belge §11.5): yetenek kartının hamle satırları yetenek satırının (Row 1)
    /// çalışma anı kopyalarıdır (prefab değişmez), plaka satırından sonra ve alt düğmelerden önce durur, tıklanınca o hamleyi kurar,
    /// E fişi yalnız E'nin satırında; en kötü durumda iki dilde taşmaz ve durum panelinin altına değmez; harf tuşları tekil.
    /// Boş sahne açıkken koşar (Baskin.unity eklemeli açılır).
    /// </summary>
    public class FacilityMovesSceneTests
    {
        sealed class Commands : IRaidCommands
        {
            public readonly List<int> Chosen = new List<int>();
            public int Armed;
            public void SelectUnit(char letter) { }
            public void ClearSelection() { }
            public void HoverTile(Vector2Int tile) { }
            public void ClearHover() { }
            public void MoveSelectedTo(Vector2Int tile) { }
            public void ArmAbility() { }
            public void UseAbilityOn(Vector2Int tile) { }
            public void Undo() { }
            public void EndTurn() { }
            public void CancelPending() { }
            public void SpeedUpMachines(bool on) { }
            public void RequestRetreat() { }
            public void ToggleLog() { }
            public void ToggleRules() { }
            public void ContinueToReport() { }
            public void ArmAction() => Armed++;
            public void ChooseAction(int index) => Chosen.Add(index);
        }

        static RaidViewModel Vm(RaidCardRow[] actions, int key)
        {
            var team = new[] { 'T', 'E', 'A' }.Select(l => new RaidTeamModel { Letter = l, Name = l.ToString(), Hp = 3, Max = 3, Move = true, Ability = true, MoveRange = 3, Power = 1 }).ToArray();
            team[0].Selected = true;
            return new RaidViewModel
            {
                Turn = 2, Turns = 6, Phase = "player", Civilians = 2, Trust = 11, TrustMax = 20, AbilityLabel = "DAR KORİDOR", BlueprintWing = 1, BlueprintSheet = 2,
                Goal = Loc.T("Amaç: ekibi geçide götür, sonra GEÇ"), BlueprintRoom = "Kuzey kanadı",
                Targets = new[] { new RaidTargetModel { Name = "Çekirdek", Cell = "F4", Thumb = "thumb_cabinet_core", Hp = 4, Max = 4, Armor = true } },
                Team = team, UnitCard = team[0],
                UnitRows = new[]
                {
                    new RaidCardRow { Icon = "ic_boots_s", Text = Loc.T("Hareket"), Detail = Loc.F("{0} kare", 3) },
                    new RaidCardRow { Icon = "ic_bolt_s", Text = Loc.T("Sis bombası"), Detail = Loc.F("düz hat, 1–4 · {0} bomba", 2) },
                    new RaidCardRow { Icon = "mk_armor_13", Text = Loc.T("Zırh plakası"), Detail = Loc.T("ilk hasarı karşılar"), Good = true },
                },
                Effects = new[] { Loc.T("Nişanlı ateşi keser"), Loc.T("bu tur") }, EffectTones = new[] { "good", "plain" },
                ActionRows = actions, ActionKey = key,
            };
        }

        static RaidCardRow[] Six(string drag) => new[]
        {
            new RaidCardRow { Icon = "ic_bolt_s", Text = Loc.T("Pusu"), Detail = Loc.F("yetenek yerine · hat {0} kare", 4) },
            new RaidCardRow { Icon = "ic_bolt_s", Text = Loc.T("Koşu darbesi"), Detail = Loc.F("yürüyüş + yetenek · {0}+ kare koş · {1} iter", 3, 2) },
            new RaidCardRow { Icon = "ic_boots_s", Text = Loc.F("Sürükle: {0}", drag), Detail = Loc.F("yürüyüş + yetenek · menzil {0}", 2) },
            new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Fırlat"), Detail = Loc.F("{0} kare içinde alıcı yok", 3), Used = true },
            new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Büyük sandığı aç"), Detail = Loc.F("yetenek yerine · {0} tutuyor", drag) },
            new RaidCardRow { Icon = "ic_boots_s", Text = Loc.T("Menfez"), Detail = Loc.T("yürüdü · yürüyüş ister"), Used = true },
        };

        static T Field<T>(SerializedObject so, string name) where T : Object => (T)so.FindProperty(name).objectReferenceValue;
        static T Element<T>(SerializedObject so, string name, int i) where T : Object => (T)so.FindProperty(name).GetArrayElementAtIndex(i).objectReferenceValue;

        static void WithCard(System.Action<RaidHudView, UnitCardView, SerializedObject> body)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            var lang = DeskLoc.Instance.Lang;
            try
            {
                var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RaidHudView>(true)).Single();
                var card = Field<UnitCardView>(new SerializedObject(hud), "unitCard");
                Assert.IsNotNull(card, "HUD'un yetenek kartı bağlı");
                body(hud, card, new SerializedObject(card));
            }
            finally { DeskLoc.Instance.SetLanguage(lang); EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Şablonun (Row 1 altındaki parça) kopyadaki karşılığı (kardeş sırası yolu).</summary>
        static Transform Mirror(Transform templ, Transform templRoot, Transform copyRoot)
        {
            var path = new List<int>();
            for (var t = templ; t != templRoot; t = t.parent) path.Add(t.GetSiblingIndex());
            var c = copyRoot;
            for (int i = path.Count - 1; i >= 0; i--) c = c.GetChild(path[i]);
            return c;
        }

        /// <summary>Plaka satırı (Row 2) ile alt düğmeler (Effects) arasındaki kardeşler: hamle satırı kopyaları.</summary>
        static List<Transform> Copies(SerializedObject so)
        {
            var row2 = Element<RectTransform>(so, "rows", 2);
            var effects = Field<GameObject>(so, "effectRow").transform;
            Assert.AreSame(row2.parent, effects.parent, "satırlar ve alt düğmeler aynı kâğıtta");
            var parent = row2.parent;
            return Enumerable.Range(row2.GetSiblingIndex() + 1, effects.GetSiblingIndex() - row2.GetSiblingIndex() - 1).Select(parent.GetChild).ToList();
        }

        [Test]
        public void Card_ActionRows_ClonedFromRow1_BoundAndKeyed()
        {
            WithCard((hud, card, so) =>
            {
                var commands = new Commands();
                card.Bind(commands);
                // Eski tahta: kopya kurulmaz (sahne ağacı aynı)
                hud.Render(Vm(System.Array.Empty<RaidCardRow>(), -1));
                Assert.AreEqual(0, card.ActionRowCopies);
                Assert.AreEqual(0, Copies(so).Count);

                var rows = Six("Bernard");
                hud.Render(Vm(rows, 2));
                Assert.AreEqual(6, card.ActionRowCopies);
                var copies = Copies(so);
                Assert.AreEqual(6, copies.Count, "kopyalar plaka satırından sonra, alt düğmelerden önce");
                var row1 = Element<RectTransform>(so, "rows", 1);
                var key = Field<GameObject>(so, "abilityKey").transform;
                var keyText = Field<TMP_Text>(so, "abilityKeyText").transform;
                var name = Element<TMP_Text>(so, "rowName", 1).transform;
                var detail = Element<TMP_Text>(so, "rowDetail", 1).transform;
                Assert.AreEqual("Q", keyText.GetComponent<TMP_Text>().text, "yetenek satırının Q fişi değişmez");
                Assert.IsTrue(key.gameObject.activeSelf);
                for (int i = 0; i < 6; i++)
                {
                    var c = copies[i];
                    Assert.IsTrue(c.gameObject.activeSelf, "kopya " + i);
                    Assert.AreEqual(rows[i].Text, Mirror(name, row1, c).GetComponent<TMP_Text>().text);
                    Assert.AreEqual(rows[i].Detail, Mirror(detail, row1, c).GetComponent<TMP_Text>().text);
                    Assert.AreEqual(i == 2, Mirror(key, row1, c).gameObject.activeSelf, "E fişi yalnız E'nin satırında: " + i);
                    if (i == 2) Assert.AreEqual("E", Mirror(keyText, row1, c).GetComponent<TMP_Text>().text);
                    var button = c.GetComponent<Button>();
                    Assert.IsNotNull(button, "kopyada düğme");
                    Assert.AreEqual(!rows[i].Used, button.interactable, "kullanılamaz satır tıklanmaz: " + i);
                    button.onClick.Invoke();
                }
                CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, commands.Chosen, "satıra tık o hamleyi seçer");

                // Daha az satır: fazlası kapanır; eski model: hepsi kapanır, kopya sayısı aynı (havuz)
                hud.Render(Vm(rows.Take(2).ToArray(), 0));
                Assert.AreEqual(2, copies.Count(c => c.gameObject.activeSelf));
                hud.Render(Vm(System.Array.Empty<RaidCardRow>(), -1));
                Assert.IsTrue(copies.All(c => !c.gameObject.activeSelf));
                Assert.AreEqual(6, card.ActionRowCopies);

                // Kart kodunda ad araması yok
                StringAssert.DoesNotContain("Find(", File.ReadAllText("Assets/Corrigible/Desk/Scripts/Runtime/Raid/UnitCardView.cs"));
            });
        }

        /// <summary>
        /// En kötü kart (üç satır + altı hamle; en uzun kişi adı) iki dilde: ad ve ayrıntı satırına sığar (ad tek satır, ayrıntı en çok iki
        /// satır ve kesilmez, satırın içinde kalır); 1080 başvuru yüksekliğinde kartın tepesi durum panelinin altından aşağıda. Prefab
        /// içeriğinde ölçülür (sahne kökü düzenleme kipinde yerleşim kurmayabilir).
        /// </summary>
        [Test]
        public void Card_WorstCase_FitsBothLanguages()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidHud.prefab");
            var lang = DeskLoc.Instance.Lang;
            try
            {
                if (root.GetComponentInParent<Canvas>() == null) root.AddComponent<Canvas>();
                var hud = root.GetComponentInChildren<RaidHudView>(true);
                var card = Field<UnitCardView>(new SerializedObject(hud), "unitCard");
                var so = new SerializedObject(card);
                string longest = CandidateData.Names.Concat(CandidateData.Founders).OrderByDescending(n => n.Length).First();
                var status = (RectTransform)root.transform.Find("Corners/Status");
                Assert.IsNotNull(status, "durum paneli");
                var cardRect = (RectTransform)card.transform;
                var report = new List<string>();
                foreach (var l in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(l);
                    hud.Render(Vm(Six(longest), 0));
                    LayoutRebuilder.ForceRebuildLayoutImmediate(status);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
                    var rows = new List<Transform> { Element<RectTransform>(so, "rows", 0), Element<RectTransform>(so, "rows", 1), Element<RectTransform>(so, "rows", 2) };
                    rows.AddRange(Copies(so).Where(c => c.gameObject.activeSelf));
                    Assert.AreEqual(9, rows.Count);
                    var row1 = Element<RectTransform>(so, "rows", 1);
                    var name = Element<TMP_Text>(so, "rowName", 1).transform;
                    var detail = Element<TMP_Text>(so, "rowDetail", 1).transform;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var r = rows[i];
                        // Hareket ve plaka satırlarında Q fişi yok: kendi yazıları; kopyalar yetenek satırının yolundan
                        var n = i < 3 ? Element<TMP_Text>(so, "rowName", i) : Mirror(name, row1, r).GetComponent<TMP_Text>();
                        var d = i < 3 ? Element<TMP_Text>(so, "rowDetail", i) : Mirror(detail, row1, r).GetComponent<TMP_Text>();
                        foreach (var t in new[] { n, d }) t.ForceMeshUpdate(true, true);
                        string at = l + " " + n.text + " | " + d.text + " (ad " + n.preferredWidth.ToString("0") + "/" + n.rectTransform.rect.width.ToString("0")
                            + ", ayrıntı " + d.preferredWidth.ToString("0") + "/" + d.rectTransform.rect.width.ToString("0") + ", " + d.textInfo.lineCount + " satır)";
                        report.Add(at);
                        Assert.Greater(n.rectTransform.rect.width, 0, "yerleşim kurulmadı: " + at);
                        Assert.LessOrEqual(n.preferredWidth, n.rectTransform.rect.width + 1f, "ad sığmıyor: " + at);
                        Assert.IsFalse(d.isTextOverflowing, "ayrıntı kesiliyor: " + at);
                        Assert.LessOrEqual(d.textInfo.lineCount, 2, "ayrıntı ikiden çok satır: " + at);
                        Assert.LessOrEqual(d.GetPreferredValues(d.text, d.rectTransform.rect.width, float.PositiveInfinity).y, ((RectTransform)r).rect.height + 0.5f, "ayrıntı satırdan taşıyor: " + at);
                        var tc = new Vector3[4]; var rc = new Vector3[4];
                        d.rectTransform.GetWorldCorners(tc); ((RectTransform)r).GetWorldCorners(rc);
                        Assert.LessOrEqual(tc[2].x, rc[2].x + 0.5f, "ayrıntı satırın dışında: " + at);
                    }
                    // 1080 başvuru yüksekliğinde kartın tepesi durum panelinin altından aşağıda
                    Assert.AreEqual(0, cardRect.anchorMin.y, 1e-4, "kart alttan bağlı");
                    Assert.AreEqual(1, status.anchorMin.y, 1e-4, "durum paneli üstten bağlı");
                    float cardTop = cardRect.anchoredPosition.y + (1 - cardRect.pivot.y) * cardRect.rect.height;
                    float statusBottom = 1080 + status.anchoredPosition.y - status.pivot.y * status.rect.height;
                    report.Add(l + ": kartın tepesi " + cardTop.ToString("0") + ", durum panelinin altı " + statusBottom.ToString("0"));
                    Assert.Less(cardTop, statusBottom, string.Join("\n", report));
                }
                TestContext.WriteLine(string.Join("\n", report));
            }
            finally { DeskLoc.Instance.SetLanguage(lang); PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public void KeyMap_LettersUnique_EFree()
        {
            var keys = RaidHudView.LetterKeys;
            CollectionAssert.AllItemsAreUnique(keys.Select(k => k.key).ToArray());
            CollectionAssert.AllItemsAreUnique(keys.Select(k => k.id).ToArray());
            CollectionAssert.IsSubsetOf(new[] { Key.Q, Key.E, Key.Z, Key.L }, keys.Select(k => k.key).ToArray());
            Assert.AreEqual("action", keys.Single(k => k.key == Key.E).id);
            var taken = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Enter, Key.Escape, Key.F1, Key.Space, Key.Slash };
            CollectionAssert.IsEmpty(keys.Select(k => k.key).Intersect(taken).ToArray());
        }

        /// <summary>
        /// Tahtanın tesis işaretleri (aşama 7, belge §5): pusu kurulunca hat çizilir, yük yere düşünce sandık parçası görünür, sürükleme
        /// kipinde gövdenin karesi çerçevelenir; Esc odağı kaldırır, geri alma hattı siler. Eski tek tahtada üçü de yok.
        /// </summary>
        [Test]
        public void Board_AmbushLineCargoFocus()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            try
            {
                var live = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LiveRaidDataSource>(true)).Single();
                var board = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Corrigible.Game.Raid.RaidBoard3D>(true)).Single();
                var sim = FacilityDeskKit.New();
                FacilityDeskKit.StartFirst(sim);
                live.OpenFacilityForValidation(sim);
                var B = live.State;
                FacilityDeskKit.Calm(B); FacilityMovesTestKit.Floor(B); FacilityMovesTestKit.Park(B);
                var T = FacilityMovesTestKit.Team(B, "tek"); var E = FacilityMovesTestKit.Team(B, "emp"); var A = FacilityMovesTestKit.Team(B, "ana");
                FacilityMovesTestKit.Put(T, 3, 4);
                FacilityMovesTestKit.Republish(live);
                Assert.AreEqual(0, board.AmbushLineCount, "kurulu pusu yok");
                Assert.IsFalse(board.CargoShown, "yük yerde değil");
                Assert.IsFalse(board.FocusShown, "hamle kipi kapalı");

                live.Select('T');
                live.ArmAction();
                Assert.AreEqual(Baskin.ActAmbush, live.ActionKind);
                live.UseAction(new Vector2Int(3, 3));
                Assert.AreEqual(0, B.Units.First(u => u.Type == "tek" && u.Side == 'p').AmbushDir, "pusu kuzeye kuruldu");
                Assert.AreEqual(1, board.AmbushLineCount, "kurulu pusunun hattı tahtada");
                Assert.AreEqual(1, live.MarksFor(live.Current).Ambush.Count);

                B.CargoX = 1; B.CargoY = 1;
                FacilityMovesTestKit.Republish(live);
                Assert.IsTrue(board.CargoShown, "yerdeki yükün parçası");
                Assert.AreEqual(1 * 8 + 1, live.MarksFor(live.Current).Cargo);

                // Sürükleme kipi: sürüklenecek gövdenin karesi çerçevelenir
                FacilityMovesTestKit.Put(A, 5, 5); FacilityMovesTestKit.Put(E, 5, 4); FacilityMovesTestKit.MakeBody(E);
                FacilityMovesTestKit.Republish(live);
                live.Select('A');
                int drag = FacilityMoveText.Options(B, A).FindIndex(o => o.Kind == Baskin.ActDrag && o.Other == E.Id);
                Assert.GreaterOrEqual(drag, 0, "sürükleme seçeneği");
                live.ChooseAction(drag);
                Assert.AreEqual(Baskin.ActDrag, live.ActionKind);
                Assert.IsTrue(board.FocusShown, "gövdenin karesinde odak");
                Assert.AreEqual(4 * 8 + 5, live.MarksFor(live.Current).Focus);
                live.Cancel();
                Assert.IsFalse(board.FocusShown, "Esc kipi ve odağı kapatır");

                // Geri alma pusuyu siler: hat da kalkar
                live.Select('T');
                live.Undo();
                Assert.AreEqual(0, board.AmbushLineCount, "geri alınan pusunun hattı yok");

                // Eski tek tahta: işaretlerin hiçbiri
                var p = new RaidParams
                {
                    Seed = 731, Region = "cn", Compute = 5, IsCore = true, Tier = 3, RaidNumber = 3,
                    Traits = new List<string> { GameData.Region("cn").Trait, "kopya" }, Signature = GameData.Region("cn").Enemy,
                };
                live.OpenForValidation(Baskin.Create(p));
                var marks = live.MarksFor(live.Current);
                CollectionAssert.IsEmpty(marks.Ambush);
                Assert.AreEqual(-1, marks.Cargo);
                Assert.AreEqual(-1, marks.Focus);
                Assert.AreEqual(0, board.AmbushLineCount);
                Assert.IsFalse(board.CargoShown);
                Assert.IsFalse(board.FocusShown);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Operasyon raporu kâğıdının SONUÇLAR yuvası modelin sınırıyla aynı (aşama 7, belge §9): büyük sandık satırı
        /// eklenince kurucu bu sınıra göre değişmemiş alarm satırını düşürür; prefab/sahne yuvası değişirse sınır da değişmeli.</summary>
        [Test]
        public void OperationReport_RowSlots_MatchModel()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Desk/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var view = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OperationReportView>(true)).Single();
                var rows = new SerializedObject(view).FindProperty("rows");
                Assert.IsNotNull(rows, "OperationReportView.rows");
                Assert.AreEqual(OperationReportData.MaxResults, rows.arraySize, "rapor kâğıdının SONUÇLAR yuvası");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
