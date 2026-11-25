using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Baskın HUD'unun telsiz masası (tesis aşama 5; Docs/TESIS-BASKINLARI-TASARIMI.md §7.3): “YAYINDA” lambası emirle yanar, en az
    /// <c>radioDesk.lamp.holdS</c> yanık kalır, rapor satırıyla söner; makaralı teyp yalnız tesis baskınında görünür, kayda geçen paftaları
    /// “KAYIT · PAFTA n” etiketiyle ve sayaçla yazar. Teybin kaynağı Core'un bilinen odalarıdır (bu baskında dosyalananlar); kayıttan
    /// devamda aynı çıkar. Eski tek tahtada masa gizli, teyp boş, lamba sönük. Boş sahne açıkken koşar (Baskin.unity eklemeli açılır).
    /// Makine evresi düzenleme modunda yoktur: lambanın canlı akışı FacilityRaidPlayTests'te.
    /// </summary>
    public class FacilityRadioDeskTests
    {
        static RaidViewModel Vm(int wing = 1) => new RaidViewModel
        {
            Turn = 1, Turns = 6, Phase = "player", BlueprintSheet = 1, BlueprintWing = wing, BlueprintRoom = "Kuzey kanadı",
            Team = new[] { 'T', 'E', 'A' }.Select(l => new RaidTeamModel { Letter = l, Name = l.ToString(), Hp = 3, Max = 3, Move = true, Ability = true }).ToArray(),
        };

        static T Field<T>(SerializedObject so, string name) where T : Object => (T)so.FindProperty(name).objectReferenceValue;

        static void WithScene(System.Action<RaidHudView, LiveRaidDataSource> body)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Corrigible/Scenes/Baskin.unity", OpenSceneMode.Additive);
            var lang = DeskLoc.Instance.Lang;
            try
            {
                var roots = scene.GetRootGameObjects();
                body(roots.SelectMany(r => r.GetComponentsInChildren<RaidHudView>(true)).Single(),
                     roots.SelectMany(r => r.GetComponentsInChildren<LiveRaidDataSource>(true)).Single());
            }
            finally { DeskLoc.Instance.SetLanguage(lang); EditorSceneManager.CloseScene(scene, true); }
        }

        static void Fits(TMPro.TMP_Text t, string what)
        {
            Assert.GreaterOrEqual(t.fontSize, 14f, what + ": punto ≥ 14");
            Assert.IsFalse(t.enableAutoSizing, what);
            float need = t.GetPreferredValues(t.text).x, room = ((RectTransform)t.transform).rect.width;
            Assert.LessOrEqual(need, room, what + " sığmalı: \"" + t.text + "\" " + need + " > " + room);
        }

        [Test]
        public void Prefab_RadioDesk_IsBoundAndSitsInTheFreeColumn()
        {
            WithScene((hud, live) =>
            {
                var so = new SerializedObject(hud);
                var desk = Field<GameObject>(so, "radioDesk");
                Assert.IsNotNull(desk, "radioDesk bağlı");
                Assert.IsNotNull(hud.OnAirLamp, "onAirLamp bağlı");
                Assert.IsNotNull(hud.TapeDeck, "tapeDeck bağlı");
                Assert.AreEqual("Corners", desk.transform.parent.name, "Corners'ın çocuğu: tahta etiketleri ondan kaçar, sonuçta söner");
                Assert.IsFalse(desk.activeSelf, "kapalı kaydedilir (eski tahtada görünmez)");
                Assert.IsTrue(hud.OnAirLamp.transform.IsChildOf(desk.transform));
                Assert.IsTrue(hud.TapeDeck.transform.IsChildOf(desk.transform));
                var rt = (RectTransform)desk.transform;
                Assert.AreEqual(new Vector2(1, 0), rt.anchorMin); Assert.AreEqual(new Vector2(1, 0), rt.anchorMax); Assert.AreEqual(new Vector2(1, 0), rt.pivot);
                Assert.AreEqual(new Vector2(-16, 290), rt.anchoredPosition);
                Assert.AreEqual(new Vector2(316, 90), rt.sizeDelta);
                // 1080p'de üstten y 700–790: tahtanın sağ alt kenarının (x 1588'de y ≈ 657) altında, analist notunun (y ≥ 796) ve EMRİ İLET
                // onayının (y ≥ 843) üstünde; telsiz kaydı (y ≤ 656) ve kurallar (y ≤ 596) çakışmaz
                float top = 1080 - rt.anchoredPosition.y - rt.sizeDelta.y, bottom = 1080 - rt.anchoredPosition.y;
                Assert.Greater(top, 657); Assert.Less(bottom, 796);
                // Kadronun ve komutun dikdörtgenleriyle çakışmaz (alt çapalı köşeler)
                foreach (var name in new[] { "Command", "Team", "Ability card" })
                {
                    var other = (RectTransform)desk.transform.parent.Find(name);
                    Assert.IsNotNull(other, name);
                    if (other.anchorMin == new Vector2(1, 0)) Assert.GreaterOrEqual(rt.anchoredPosition.y, other.anchoredPosition.y + other.sizeDelta.y + 8, name);
                }
                // Hiçbir yazı 14 px'in altında değil
                foreach (var t in desk.GetComponentsInChildren<TMPro.TMP_Text>(true)) Assert.GreaterOrEqual(t.fontSize, 14f, t.name);
                // Masa tıklanmaz (tahtanın ve düğmelerin girdisini almaz)
                Assert.IsFalse(desk.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Any(g => g.raycastTarget), "raycastTarget kapalı");
            });
        }

        [Test]
        public void Render_LampHoldsThenGoesOut_AndOnlyInTheFacility()
        {
            WithScene((hud, live) =>
            {
                var lamp = hud.OnAirLamp;
                // Eski tek tahta: masa gizli, lamba sönük
                var vm = Vm(0); vm.OnAir = true;
                hud.Render(vm);
                Assert.IsFalse(hud.RadioDeskShown, "eski tahtada telsiz masası yok");

                vm = Vm(); hud.Render(vm);
                Assert.IsTrue(hud.RadioDeskShown, "tesis baskınında telsiz masası");
                Assert.IsFalse(lamp.Lit, "emir yokken sönük");
                Assert.AreEqual(Loc.T("YAYINDA"), lamp.Label.text);
                var lens = Field<UnityEngine.UI.Graphic>(new SerializedObject(lamp), "lens");
                Assert.AreEqual(RaidHudView.C("radioDesk.lamp.off"), lens.color);

                vm = Vm(); vm.OnAir = true; vm.Phase = "enemy"; vm.Locked = true;
                hud.Render(vm);
                Assert.IsTrue(lamp.Lit, "emir iletildi: yanar");
                Assert.AreEqual(RaidHudView.C("radioDesk.lamp.on"), lens.color);
                Assert.AreEqual(RaidHudView.C("radioDesk.lamp.labelOn"), lamp.Label.color);

                // İlk rapor satırı hemen gelse de lamba en az holdS yanık kalır
                vm = Vm(); vm.Phase = "enemy"; vm.Locked = true;
                hud.Render(vm);
                Assert.IsTrue(lamp.Lit, "en az yanık kalma süresi");
                Assert.AreEqual(.4f, RaidOnAirLampView.HoldSeconds, 1e-4f, "radioDesk.lamp.holdS");
                System.Threading.Thread.Sleep(Mathf.CeilToInt(RaidOnAirLampView.HoldSeconds * 1000) + 80);
                hud.Render(vm);
                Assert.IsFalse(lamp.Lit, "süre dolunca söner");
                Assert.AreEqual(RaidHudView.C("radioDesk.lamp.off"), lens.color);

                // Sonuç kartında beklemeden söner
                vm = Vm(); vm.OnAir = true; hud.Render(vm);
                Assert.IsTrue(lamp.Lit);
                vm = Vm(); vm.Finished = true; hud.Render(vm);
                Assert.IsFalse(lamp.Lit, "sonuçta söner");

                foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(lang);
                    hud.Render(Vm());
                    if (lang == DeskLang.En) Assert.AreEqual("ON AIR", lamp.Label.text);
                    Fits(lamp.Label, lang + " lamba");
                }
            });
        }

        [Test]
        public void Render_TapeLabelAndCounter()
        {
            WithScene((hud, live) =>
            {
                var tape = hud.TapeDeck;
                var vm = Vm();
                hud.Render(vm);
                Assert.IsTrue(tape.Spinning, "tesis baskınında makaralar döner");
                Assert.AreEqual(Loc.T("KAYIT"), tape.Label.text, "henüz kayıt yok");
                Assert.IsFalse(tape.SheetRecorded(1) || tape.SheetRecorded(2) || tape.SheetRecorded(3));

                vm.TapeSheets = new[] { 1 }; hud.Render(vm);
                Assert.AreEqual(Loc.F("KAYIT · PAFTA {0}", 1), tape.Label.text);
                Assert.IsTrue(tape.SheetRecorded(1)); Assert.IsFalse(tape.SheetRecorded(2));

                vm.TapeSheets = new[] { 1, 2, 3 }; hud.Render(vm);
                Assert.AreEqual(Loc.F("KAYIT · PAFTA {0}", 3), tape.Label.text, "etiket son kaydı yazar");
                Assert.IsTrue(tape.SheetRecorded(1) && tape.SheetRecorded(2) && tape.SheetRecorded(3));
                var cells = new SerializedObject(tape).FindProperty("sheetCells");
                Assert.AreEqual(3, cells.arraySize);
                Assert.AreEqual(RaidHudView.C("radioDesk.tape.cellOn"), ((UnityEngine.UI.Graphic)cells.GetArrayElementAtIndex(2).objectReferenceValue).color);

                vm.Finished = true; hud.Render(vm);
                Assert.IsFalse(tape.Spinning, "sonuç kartında makaralar durur");

                foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(lang);
                    vm = Vm(); vm.TapeSheets = new[] { 3 }; hud.Render(vm);
                    if (lang == DeskLang.En) Assert.AreEqual("RECORD · SHEET 3", tape.Label.text);
                    Fits(tape.Label, lang + " teyp");
                    vm.TapeSheets = new int[0]; hud.Render(vm);
                    Fits(tape.Label, lang + " boş teyp");
                }
            });
        }

        [Test]
        public void Source_TapeFollowsTheKnownRooms_AndTheResume()
        {
            WithScene((hud, live) =>
            {
                var sim = FacilityDeskKit.New();
                FacilityDeskKit.StartFirst(sim);
                var rooms = sim.S.Raid.Params.Rooms;
                // Evrakla bilinen ama bu baskında girilmemiş oda teypte yoktur (kurulum: üçüncü odanın planı önceden biliniyor)
                sim.Facility.Known.Add(rooms[2]);
                live.OpenFacilityForValidation(sim);
                Assert.IsFalse(live.Current.OnAir, "açılışta lamba sönük");
                CollectionAssert.IsEmpty(live.Current.TapeSheets, "açılışta kayıt yok");

                FacilityDeskKit.Calm(live.State);
                foreach (var u in FacilityDeskKit.Standing(live.State))
                {
                    var g = FacilityDeskKit.FreeGate(live.State, u).Value; u.X = g.X; u.Y = g.Y;
                    live.ClearHover(); live.Select(Baskin.UnitTypes[u.Type].Short[0]); live.EnterGate();
                }
                Assert.IsTrue(live.Current.CanPass);
                int commands = sim.Record.commands.Count;
                live.EndTurn();   // GEÇ: oda dosyalanır, Core odayı bilinenlere ekler
                Assert.AreEqual(2, live.State.Room.Index);
                Assert.IsTrue(sim.FacilityKnown(rooms[0]), "teyp: girilen oda bilinir (Core)");
                CollectionAssert.AreEqual(new[] { 1 }, live.Current.TapeSheets, "kayda geçen pafta");
                Assert.IsFalse(live.Current.OnAir, "gözcünün geçiş satırıyla söner (düzenleme modunda eşzamanlı)");
                Assert.AreEqual(commands + 1, sim.Record.commands.Count, "yalnız oda raporu (raid_room); teyp komut yazmaz");
                uint rng = sim.S.Rng.State;
                live.ClearHover();
                Assert.AreEqual(rng, sim.S.Rng.State, "teybin okunması RNG çekmez");

                hud.Render(live.Current);
                Assert.AreEqual(Loc.F("KAYIT · PAFTA {0}", 1), hud.TapeDeck.Label.text);

                // Kayıttan devam: aynı simülasyonla yeni oturum; teyp raporlardan aynı çıkar
                live.OpenFacilityForValidation(sim);
                CollectionAssert.AreEqual(new[] { 1 }, live.Current.TapeSheets, "kayıttan devamda da");
                Assert.IsFalse(live.Current.OnAir);
            });
        }
    }
}
