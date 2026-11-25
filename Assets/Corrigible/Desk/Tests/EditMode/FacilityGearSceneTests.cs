using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
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
    /// Ekipman ve dördüncü birimin HUD'u RaidHud.prefab içeriğinde (aşama 9; tasarım §2.1, §3.1, §3.4, §11.4): yetenek kartının ekipman
    /// satırı kopya havuzunun başında (R fişi, tık ChooseGear; hamle satırları ve E fişi kayar), en kötü durumda (3 + 1 + 6 satır) iki dilde
    /// durum panelinin altına değmez; dört ekip kartı 1920×1080'de Command paneline değmez, dördüncü kartın yazıları taşmaz; üç kartın
    /// yeri değişmez; R ve 4 tuşları tekil. Prefab içeriğinde ölçülür (sahne kökü düzenleme kipinde yerleşim kurmayabilir).
    /// </summary>
    public class FacilityGearSceneTests
    {
        const string HudPath = "Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidHud.prefab";

        sealed class Commands : IRaidCommands
        {
            public readonly List<string> Calls = new List<string>();
            public void SelectUnit(char letter) => Calls.Add("unit " + letter);
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
            public void ArmAction() { }
            public void ChooseAction(int index) => Calls.Add("action " + index);
            public void SelectUnitId(int id) => Calls.Add("id " + id);
            public void ArmGear() { }
            public void ChooseGear(int index) => Calls.Add("gear " + index);
        }

        static string Longest => CandidateData.Names.Concat(CandidateData.Founders).OrderByDescending(n => n.Length).First();

        static RaidTeamModel[] Team(bool dog)
        {
            var team = new[] { 'T', 'E', 'A' }.Select(l => new RaidTeamModel { Letter = l, Name = Loc.F("{0} · {1}", Longest, Loc.T("EMP UZMANI")), Hp = 3, Max = 3, Move = true, Ability = true, MoveRange = 3, Power = 1,
                Equipment = Loc.F("{0} · {1}", PlanningBoardData.EquipmentName("kit_emp"), Loc.F("{0} · {1} hak", PlanningBoardData.EquipmentName(FacilityGear.Wire), 2)) }).ToList();
            team[0].Selected = true;
            if (dog) team.Add(new RaidTeamModel { Letter = Loc.T("Ö")[0], UnitId = 99, Dog = true, Name = Loc.T("KÖPEK"), Hp = 3, Max = 3, MoveRange = 5,
                Equipment = Loc.F("Sahibi: {0} · +{1} köpek", Longest, 1), Move = true, Ability = true });
            return team.ToArray();
        }

        static RaidViewModel Vm(RaidTeamModel[] team, RaidCardRow[] gears, int gearKey, RaidCardRow[] actions, int actionKey)
        {
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
                GearRows = gears, GearKey = gearKey, ActionRows = actions, ActionKey = actionKey,
            };
        }

        /// <summary>En uzun ekipman satırı (iki dilde): yem (evre başına adım ve tur) ve kalan hak.</summary>
        static RaidCardRow GearRow() => new RaidCardRow { Icon = "ic_bolt_s", Text = PlanningBoardData.EquipmentName(FacilityGear.Decoy),
            Detail = Loc.F("{0} · {1} hak", Loc.F("evre başına {0} kare · {1} tur", 2, 3), 1) };

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

        static void WithHud(System.Action<GameObject, RaidHudView> body)
        {
            var root = PrefabUtility.LoadPrefabContents(HudPath);
            var lang = DeskLoc.Instance.Lang;
            try
            {
                if (root.GetComponentInParent<Canvas>() == null) root.AddComponent<Canvas>();
                body(root, root.GetComponentInChildren<RaidHudView>(true));
            }
            finally { DeskLoc.Instance.SetLanguage(lang); PrefabUtility.UnloadPrefabContents(root); }
        }

        static Transform Mirror(Transform templ, Transform templRoot, Transform copyRoot)
        {
            var path = new List<int>();
            for (var t = templ; t != templRoot; t = t.parent) path.Add(t.GetSiblingIndex());
            var c = copyRoot;
            for (int i = path.Count - 1; i >= 0; i--) c = c.GetChild(path[i]);
            return c;
        }

        static List<Transform> Copies(SerializedObject so)
        {
            var row2 = Element<RectTransform>(so, "rows", 2);
            var effects = Field<GameObject>(so, "effectRow").transform;
            var parent = row2.parent;
            return Enumerable.Range(row2.GetSiblingIndex() + 1, effects.GetSiblingIndex() - row2.GetSiblingIndex() - 1).Select(parent.GetChild).ToList();
        }

        [Test]
        public void Card_GearRow_FirstCopy_RKey_ClickChoosesGear()
        {
            WithHud((root, hud) =>
            {
                var card = Field<UnitCardView>(new SerializedObject(hud), "unitCard");
                var so = new SerializedObject(card);
                var commands = new Commands();
                card.Bind(commands);
                var actions = Six("Bernard").Take(2).ToArray();
                hud.Render(Vm(Team(false), new[] { GearRow() }, 0, actions, 1));
                var copies = Copies(so).Where(c => c.gameObject.activeSelf).ToList();
                Assert.AreEqual(3, copies.Count, "ekipman + iki hamle");
                var row1 = Element<RectTransform>(so, "rows", 1);
                var key = Field<GameObject>(so, "abilityKey").transform;
                var keyText = Field<TMP_Text>(so, "abilityKeyText").transform;
                var name = Element<TMP_Text>(so, "rowName", 1).transform;
                Assert.AreEqual(GearRow().Text, Mirror(name, row1, copies[0]).GetComponent<TMP_Text>().text, "ekipman satırı plakadan sonra ilk");
                Assert.AreEqual(actions[0].Text, Mirror(name, row1, copies[1]).GetComponent<TMP_Text>().text);
                Assert.IsTrue(Mirror(key, row1, copies[0]).gameObject.activeSelf);
                Assert.AreEqual("R", Mirror(keyText, row1, copies[0]).GetComponent<TMP_Text>().text);
                Assert.IsFalse(Mirror(key, row1, copies[1]).gameObject.activeSelf, "ActionKey 1: ilk hamlede E yok");
                Assert.IsTrue(Mirror(key, row1, copies[2]).gameObject.activeSelf);
                Assert.AreEqual("E", Mirror(keyText, row1, copies[2]).GetComponent<TMP_Text>().text);
                Assert.AreEqual("Q", keyText.GetComponent<TMP_Text>().text, "yetenek satırının Q fişi değişmez");
                foreach (var c in copies) c.GetComponent<Button>().onClick.Invoke();
                CollectionAssert.AreEqual(new[] { "gear 0", "action 0", "action 1" }, commands.Calls);
                // Ekipmansız: havuz hamlelerle başlar (eski eşleme)
                commands.Calls.Clear();
                hud.Render(Vm(Team(false), System.Array.Empty<RaidCardRow>(), -1, actions, 0));
                copies = Copies(so).Where(c => c.gameObject.activeSelf).ToList();
                Assert.AreEqual(2, copies.Count);
                Assert.AreEqual("E", Mirror(keyText, row1, copies[0]).GetComponent<TMP_Text>().text);
                foreach (var c in copies) c.GetComponent<Button>().onClick.Invoke();
                CollectionAssert.AreEqual(new[] { "action 0", "action 1" }, commands.Calls);
                // Kullanılamaz ekipman satırı tıklanmaz, R fişi yok
                var used = GearRow(); used.Used = true;
                hud.Render(Vm(Team(false), new[] { used }, -1, actions, 0));
                copies = Copies(so).Where(c => c.gameObject.activeSelf).ToList();
                Assert.IsFalse(copies[0].GetComponent<Button>().interactable);
                Assert.IsFalse(Mirror(key, row1, copies[0]).gameObject.activeSelf);
            });
        }

        /// <summary>
        /// En kötü kart (3 satır + ekipman + 6 hamle = <see cref="Bridge.RaidDataSource.MaxCardRows"/>; en uzun kişi adı) iki dilde: ad tek
        /// satır, ayrıntı en çok iki satır ve satırın içinde; 1080 başvuru yüksekliğinde kartın tepesi durum panelinin altından aşağıda.
        /// </summary>
        [Test]
        public void Card_GearRow_WorstCase_FitsBothLanguages()
        {
            WithHud((root, hud) =>
            {
                var card = Field<UnitCardView>(new SerializedObject(hud), "unitCard");
                var so = new SerializedObject(card);
                // Aşama 7'nin ölçüm yolu (FacilityMovesSceneTests): durum paneli köşe kabının yolundan
                var status = (RectTransform)root.transform.Find("Corners/Status");
                Assert.IsNotNull(status, "durum paneli");
                var cardRect = (RectTransform)card.transform;
                var report = new List<string>();
                foreach (var l in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(l);
                    hud.Render(Vm(Team(false), new[] { GearRow() }, 0, Six(Longest), 0));
                    LayoutRebuilder.ForceRebuildLayoutImmediate(status);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
                    var rows = new List<Transform> { Element<RectTransform>(so, "rows", 0), Element<RectTransform>(so, "rows", 1), Element<RectTransform>(so, "rows", 2) };
                    rows.AddRange(Copies(so).Where(c => c.gameObject.activeSelf));
                    Assert.AreEqual(Bridge.RaidDataSource.MaxCardRows, rows.Count);
                    var row1 = Element<RectTransform>(so, "rows", 1);
                    var name = Element<TMP_Text>(so, "rowName", 1).transform;
                    var detail = Element<TMP_Text>(so, "rowDetail", 1).transform;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var r = rows[i];
                        var n = i < 3 ? Element<TMP_Text>(so, "rowName", i) : Mirror(name, row1, r).GetComponent<TMP_Text>();
                        var d = i < 3 ? Element<TMP_Text>(so, "rowDetail", i) : Mirror(detail, row1, r).GetComponent<TMP_Text>();
                        foreach (var t in new[] { n, d }) t.ForceMeshUpdate(true, true);
                        string at = l + " " + n.text + " | " + d.text + " (ad " + n.preferredWidth.ToString("0") + "/" + n.rectTransform.rect.width.ToString("0") + ", " + d.textInfo.lineCount + " satır)";
                        report.Add(at);
                        Assert.Greater(n.rectTransform.rect.width, 0, "yerleşim kurulmadı: " + at);
                        Assert.LessOrEqual(n.preferredWidth, n.rectTransform.rect.width + 1f, "ad sığmıyor: " + at);
                        Assert.IsFalse(d.isTextOverflowing, "ayrıntı kesiliyor: " + at);
                        Assert.LessOrEqual(d.textInfo.lineCount, 2, "ayrıntı ikiden çok satır: " + at);
                        Assert.LessOrEqual(d.GetPreferredValues(d.text, d.rectTransform.rect.width, float.PositiveInfinity).y, ((RectTransform)r).rect.height + 0.5f, "ayrıntı satırdan taşıyor: " + at);
                    }
                    float cardTop = cardRect.anchoredPosition.y + (1 - cardRect.pivot.y) * cardRect.rect.height;
                    float statusBottom = 1080 + status.anchoredPosition.y - status.pivot.y * status.rect.height;
                    report.Add(l + ": kartın tepesi " + cardTop.ToString("0") + ", durum panelinin altı " + statusBottom.ToString("0"));
                    Assert.Less(cardTop, statusBottom, string.Join("\n", report));
                }
                TestContext.WriteLine(string.Join("\n", report));
            });
        }

        static (RectTransform team, RectTransform command, RaidTeamCardView[] cards) TeamParts(GameObject root, RaidHudView hud)
        {
            var so = new SerializedObject(hud);
            var cards = Enumerable.Range(0, so.FindProperty("team").arraySize).Select(i => Element<RaidTeamCardView>(so, "team", i)).ToArray();
            var command = (RectTransform)root.transform.Find("Corners/Command");
            Assert.IsNotNull(command, "Command paneli");
            return ((RectTransform)cards[0].transform.parent, command, cards);
        }

        static void Rebuild(RectTransform team, IEnumerable<RaidTeamCardView> cards)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(team);
            foreach (var c in cards.Where(c => c.gameObject.activeSelf))
                foreach (var g in c.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true).Reverse()) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)g.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(team);
        }

        /// <summary>Köpeksiz kadro: kap eski yerinde ve 1060 genişliğinde; üç kart 345 px'e sabitlendi (eski 345,33;
        /// force-expand kabı yayıyordu), sol kenarlar 6 / 357 / 708 (eski 357,33 / 708,67): en çok 0,67 px kayma, aşama 9 sapması
        /// (TESIS §14 on ikinci tur).</summary>
        [Test]
        public void TeamCards_Three_Fixed345_ContainerUnchanged()
        {
            WithHud((root, hud) =>
            {
                var (team, _, cards) = TeamParts(root, hud);
                Assert.AreEqual(4, cards.Length, "HUD'un dört kart yuvası");
                Assert.AreEqual("Card 4", cards[3].name);
                Assert.IsFalse(cards[3].gameObject.activeSelf, "dördüncü kart prefab'da kapalı");
                hud.Render(Vm(Team(false), System.Array.Empty<RaidCardRow>(), -1, System.Array.Empty<RaidCardRow>(), -1));
                Rebuild(team, cards);
                Assert.IsFalse(cards[3].gameObject.activeSelf, "köpeksiz kadroda dördüncü kart kapalı");
                // Kap ve kartlar: 20, 12; kap 1060 (eski genişlik), kartlar 345 aralık 6 (eski 345,33: kap yayıyordu)
                Assert.AreEqual(new Vector2(20, 12), team.anchoredPosition);
                Assert.AreEqual(1060, team.rect.width, .5f);
                for (int i = 0; i < 3; i++)
                {
                    var r = (RectTransform)cards[i].transform;
                    Assert.AreEqual(345, r.rect.width, .5f, "kart " + i);
                    Assert.AreEqual(142, r.rect.height, .5f);
                    float left = r.anchoredPosition.x - r.pivot.x * r.rect.width;
                    Assert.AreEqual(6 + i * 351, left, .5f, "kart " + i + " sol kenarı");
                }
            });
        }

        [Test]
        public void TeamCards_Four_Fit1080_BothLanguages()
        {
            WithHud((root, hud) =>
            {
                var (team, command, cards) = TeamParts(root, hud);
                var report = new List<string>();
                foreach (var l in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(l);
                    var models = Team(true);
                    foreach (var down in new[] { false, true })
                    {
                        models[3].Down = down; models[3].DownText = down ? Loc.T("TAHTADAN ÇEKİLDİ") : null;
                        hud.Render(Vm(models, System.Array.Empty<RaidCardRow>(), -1, System.Array.Empty<RaidCardRow>(), -1));
                        Rebuild(team, cards);
                        Assert.IsTrue(cards[3].gameObject.activeSelf, "köpekli kadroda dördüncü kart açık");
                        var c4 = (RectTransform)cards[3].transform;
                        Assert.AreEqual(280, c4.rect.width, .5f, "dördüncü kart dar");
                        float right = team.anchoredPosition.x + team.rect.width;
                        Assert.AreEqual(1, command.anchorMin.x, 1e-4, "Command sağdan bağlı");
                        float commandLeft = 1920 + command.anchoredPosition.x - command.pivot.x * command.rect.width;
                        report.Add(l + (down ? " düşmüş" : "") + ": kadronun sağı " + right.ToString("0") + ", Command'ın solu " + commandLeft.ToString("0"));
                        Assert.LessOrEqual(right + 6, commandLeft, string.Join("\n", report));
                        // Dördüncü kartın yazıları taşmaz
                        var view = new SerializedObject(cards[3]);
                        var column = Field<TMP_Text>(view, "nameLabel").transform.parent as RectTransform;
                        foreach (var field in new[] { "nameLabel", "stats", "status" })
                        {
                            var t = Field<TMP_Text>(view, field);
                            t.ForceMeshUpdate(true, true);
                            report.Add("  " + field + " '" + t.text + "' " + t.preferredWidth.ToString("0") + "/" + column.rect.width.ToString("0"));
                            Assert.LessOrEqual(t.preferredWidth, column.rect.width + 1f, field + " taşıyor: " + t.text + "\n" + string.Join("\n", report));
                        }
                        var eq = Field<TMP_Text>(view, "equipment");
                        eq.ForceMeshUpdate(true, true);
                        report.Add("  equipment '" + eq.text + "' " + eq.textInfo.lineCount + " satır, " + eq.GetPreferredValues(eq.text, eq.rectTransform.rect.width, float.PositiveInfinity).y.ToString("0") + "/" + eq.rectTransform.rect.height.ToString("0"));
                        Assert.Greater(eq.rectTransform.rect.width, 0);
                        Assert.LessOrEqual(eq.GetPreferredValues(eq.text, eq.rectTransform.rect.width, float.PositiveInfinity).y, eq.rectTransform.rect.height + .5f, "eşya satırı taşıyor\n" + string.Join("\n", report));
                    }
                }
                TestContext.WriteLine(string.Join("\n", report));
            });
        }

        [Test]
        public void KeyMap_RGear_Digit4_Free()
        {
            var keys = RaidHudView.LetterKeys;
            CollectionAssert.AllItemsAreUnique(keys.Select(k => k.key).ToArray());
            CollectionAssert.AllItemsAreUnique(keys.Select(k => k.id).ToArray());
            Assert.AreEqual("gear", keys.Single(k => k.key == Key.R).id);
            Assert.AreEqual("action", keys.Single(k => k.key == Key.E).id);
            CollectionAssert.AreEqual(new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 }, RaidHudView.DigitKeys);
            var taken = RaidHudView.DigitKeys.Concat(new[] { Key.Enter, Key.Escape, Key.F1, Key.Space, Key.Slash }).ToArray();
            CollectionAssert.IsEmpty(keys.Select(k => k.key).Intersect(taken).ToArray());
        }
            /// <summary>
        /// Tahtanın ekipman çizimi (aşama 9; tasarım §5.1–5.2, §11.4): aygıt ve köpek kendi parçasıyla (dron değil), aygıt Dir'e bakar
        /// ve yön her eşitlemede yazılır; tutsak ekip yönüne döner, etiketi son turda kırmızı; taret ve vinç hattı, yem yolu, havlama
        /// erimi, NİYETSİZ notu; engel kendi parçası, kırılınca yerine enkaz konmaz; kepenk, kalkık kepenk, sandık nişi (kapalı/açık)
        /// ve tutuş krikosu süsleri yalnız tesis odasında; düşen köpek ve kırık aygıt çizilmez; havlama önizlemesi erimi boyar. Eski tek
        /// tahtada hiçbiri.
        /// </summary>
        [Test]
        public void Board_GearMarksAndDecor()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            try
            {
                var live = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LiveRaidDataSource>(true)).Single();
                var board = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RaidBoard3D>(true)).Single();
                var sim = FacilityDeskKit.New();
                FacilityDeskKit.StartFirst(sim);
                live.OpenFacilityForValidation(sim);
                var B = live.State;
                FacilityDeskKit.Calm(B); FacilityMovesTestKit.Floor(B); FacilityMovesTestKit.Park(B);
                B.Ev = null;
                foreach (var (x, y) in new[] { (2, 1), (4, 1), (6, 3) }) Assert.AreEqual(0, Baskin.CellAt(B, x, y).Door, "süs karesi kapı değil");
                var T = FacilityMovesTestKit.Team(B, "tek");
                var tur = FacilityGearTestKit.AddGadget(B, "taret", 1, 3, 1, 3, T.Slot);
                var m1 = FacilityGearTestKit.AddMachine(B, "dron", 3, 3);
                var win = FacilityGearTestKit.AddGadget(B, "vinc", 1, 4, 1, 3, T.Slot);
                var m2 = FacilityGearTestKit.AddMachine(B, "dron", 3, 4);
                var dec = FacilityGearTestKit.AddGadget(B, "yem", 2, 6, 1, 3, T.Slot);
                var dog = FacilityGearTestKit.AddDog(B, T.Slot, 5, 2);
                dog.Bark = true;
                var cap = FacilityGearTestKit.Captive(B, FacilityGearTestKit.AddMachine(B, "bakim", 6, 1), T.Slot, 0);
                // Planı atlanan makine (Core: bırakılan tutsak ve kilitli makine planda Intent'siz kalır, Idle silinir): NİYETSİZ
                var idle = FacilityGearTestKit.AddMachine(B, "dron", 6, 5);
                Baskin.CellAt(B, 4, 6).O = new Baskin.Obj { K = "kalas", Hp = 3, Max = 3 };
                Baskin.CellAt(B, 5, 6).O = new Baskin.Obj { K = "tel", Hp = 1, Max = 1 };
                var shut = Baskin.CellAt(B, 2, 1); shut.T = "wall"; shut.Shutter = true;
                var up = Baskin.CellAt(B, 4, 1); up.T = "floor"; up.Shutter = true;
                var niche = Baskin.CellAt(B, 6, 3); niche.T = "wall"; niche.Crate = true;
                FacilityMovesTestKit.Put(T, 2, 2);
                B.HoldId = T.Id; B.HoldX = 2; B.HoldY = 1;
                FacilityMovesTestKit.Republish(live);

                // Parçalar: kendi modelleri, aygıtın yönü
                Assert.IsNotNull(board.UnitPiece(tur.Id).transform.Find("Visual/Barrel"), "taret kendi parçası (dron değil)");
                Assert.IsNotNull(board.UnitPiece(dog.Id).transform.Find("Visual/Snout"), "köpek kendi parçası");
                Assert.IsNotNull(board.UnitPiece(win.Id).transform.Find("Visual/Drum"), "vinç kendi parçası");
                Assert.IsNotNull(board.UnitPiece(dec.Id).transform.Find("Visual/KeyWing"), "yem kendi parçası");
                Assert.AreEqual(0, Mathf.DeltaAngle(board.UnitPiece(tur.Id).transform.eulerAngles.y, RaidBoard3D.GadgetYaw(1)), 0.5f, "taret doğuya");
                tur.Dir = 2;
                FacilityMovesTestKit.Republish(live);
                Assert.AreEqual(0, Mathf.DeltaAngle(board.UnitPiece(tur.Id).transform.eulerAngles.y, RaidBoard3D.GadgetYaw(2)), 0.5f, "yön her eşitlemede");
                tur.Dir = 1;
                Assert.AreEqual(0, Mathf.DeltaAngle(board.UnitPiece(cap.Id).transform.eulerAngles.y, 45), 0.5f, "tutsak ekip yönünde");
                Assert.IsNotNull(board.ObjectPiece(4, 6).transform.Find("Visual/Board"), "kalas kendi parçası");
                Assert.IsNotNull(board.ObjectPiece(5, 6).transform.Find("Visual/Strand"), "tel kendi parçası");

                // İşaretler ve etiketler
                FacilityMovesTestKit.Republish(live);
                var marks = live.MarksFor(live.Current);
                Assert.AreEqual(2, marks.Devices.Count, "taret ve vinç hattı");
                Assert.IsTrue(marks.Devices.Any(d => d.kind == "taret" && d.hit == 3 * 8 + 3), "taret hattı makinede");
                Assert.IsTrue(marks.Devices.Any(d => d.kind == "vinc" && d.hit == 4 * 8 + 3), "vinç hattı makinede");
                Assert.AreEqual(2, board.DeviceLineCount, "iki hat çizili");
                Assert.AreEqual(1, marks.DecoyPaths.Count, "yemin yolu");
                Assert.AreEqual(6 * 8 + 2, marks.DecoyPaths[0][0]);
                CollectionAssert.AreEqual(new[] { 6 * 8 + 2, 6 * 8 + 3 }, marks.DecoyPaths[0], "yem kalasta durur");
                Assert.AreEqual(marks.DecoyPaths[0].Length - 1, board.DecoyPathSegments);
                int r = B.Moves.BarkRange;
                int bark = Enumerable.Range(0, 64).Count(k => System.Math.Abs(k % 8 - dog.X) + System.Math.Abs(k / 8 - dog.Y) <= r);
                Assert.AreEqual(bark, marks.Bark.Count, "havlama erimi");
                Assert.AreEqual(bark, board.BarkTiles, "erim boyalı");
                Assert.AreEqual((Loc.T("SON TUR"), true), marks.Tags[cap.Id], "tutsağın son turu kırmızı");
                Assert.AreEqual(Loc.T("KÖPEK"), marks.Tags[dog.Id].text);
                Assert.AreEqual(Loc.F("{0} TUR", 3), marks.Tags[tur.Id].text);
                Assert.IsFalse(marks.Tags.ContainsKey(T.Id), "uzmanın etiketinde kişinin adı");
                Assert.IsTrue(marks.Notes.Contains((5 * 8 + 6, Loc.T("NİYETSİZ"))), "niyetsiz makinenin notu");
                Assert.IsFalse(marks.Spent.Contains(tur.Id), "aygıt harcanmış boyanmaz");

                // Süsler
                Assert.AreEqual("shutter", board.DecorAt(2, 1));
                Assert.AreEqual("shutterOpen", board.DecorAt(4, 1));
                Assert.AreEqual("crateNiche", board.DecorAt(6, 3));
                Assert.IsTrue(board.HoldJackShown, "tutuş krikosu");
                B.CrateOpen = true; B.HoldId = 0;
                FacilityMovesTestKit.Republish(live);
                Assert.AreEqual("crateNicheOpen", board.DecorAt(6, 3), "açılan niş");
                Assert.IsFalse(board.HoldJackShown, "tutuş bitti");

                // Havlama önizlemesi: erim boyanır
                dog.Bark = false;
                live.SelectId(dog.Id);
                live.ArmAction();
                Assert.AreEqual(Baskin.ActBark, live.ActionKind, "köpeğin ilk komutu havla");
                live.Hover(new Vector2Int(dog.X, dog.Y));
                FacilityMovesTestKit.Republish(live);
                Assert.AreEqual(bark, board.PreviewAreaTiles, "önizlemenin erimi");
                live.Cancel();
                FacilityMovesTestKit.Republish(live);
                Assert.AreEqual(0, board.PreviewAreaTiles, "Esc önizlemeyi siler");

                // Kırılan engel enkaz bırakmaz; düşen köpek ve kırık aygıt çizilmez
                Baskin.CellAt(B, 4, 6).O = null;
                dog.Dead = true; dog.Hp = 0;
                tur.Dead = true; tur.Hp = 0;
                FacilityMovesTestKit.Republish(live);
                Assert.IsNull(board.ObjectPiece(4, 6), "kalas kalktı");
                Assert.IsFalse(board.RubbleAt(4, 6), "yerine enkaz konmaz");
                Assert.IsNull(board.UnitPiece(dog.Id), "düşen köpek tahtadan kalkar");
                Assert.IsNull(board.UnitPiece(tur.Id), "kırık aygıt tahtadan kalkar");
                Assert.AreEqual(1, board.DeviceLineCount, "kırık taretin hattı yok");

                // Eski tek tahta: hiçbiri
                var p = new RaidParams
                {
                    Seed = 731, Region = "cn", Compute = 5, IsCore = true, Tier = 3, RaidNumber = 3,
                    Traits = new List<string> { GameData.Region("cn").Trait, "kopya" }, Signature = GameData.Region("cn").Enemy,
                };
                live.OpenForValidation(Baskin.Create(p));
                var old = live.MarksFor(live.Current);
                CollectionAssert.IsEmpty(old.Devices); CollectionAssert.IsEmpty(old.DecoyPaths); CollectionAssert.IsEmpty(old.Bark); CollectionAssert.IsEmpty(old.Tags);
                Assert.AreEqual(0, board.DeviceLineCount);
                Assert.AreEqual(0, board.DecoyPathSegments);
                Assert.AreEqual(0, board.BarkTiles);
                Assert.IsFalse(board.HoldJackShown);
                for (int k = 0; k < 64; k++) Assert.IsNull(board.DecorAt(k % 8, k / 8), "eski tahtada süs yok");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
