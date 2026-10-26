using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Sığınak Aşama 9: emir belgesi metinleri ve kipleri, kayıt özeti (yeniden oynatma), metin sınırları, açılışın
    /// atlanması, teleks açılış satırı, EVRAK'taki belge ve sahne bağlantıları.
    /// </summary>
    public class SiginakBriefingTests
    {
        static Simulation NewLiberation(double fon = 100) => Simulation.NewGame(null, "manipulator", 731, new Balance { startFon = fon },
            rules: new RuleOptions { LiberationCampaign = true });

        [Test]
        public void Build_Modes_NoSave_Save_Armed_Reading()
        {
            var d = BriefingBuilder.Build(false, null, false);
            Assert.IsFalse(d.HasSave);
            Assert.IsFalse(d.Reading);
            Assert.AreEqual(Loc.T("BAŞLA"), d.Action);
            Assert.AreEqual(Loc.T("KAYIT YOK"), d.NoSave);
            Assert.AreEqual(3, d.Paragraphs.Length);
            Assert.AreEqual(Loc.T("HAREKÂT EMRİ"), d.Title);

            var save = new SavedSummary { Shift = 14, Free = 1, Regions = 12 };
            d = BriefingBuilder.Build(false, save, false);
            Assert.IsTrue(d.HasSave);
            Assert.AreEqual(Loc.F("V.{0:000} · özgür {1}/{2}", 14, 1, 12), d.TabSummary);
            d = BriefingBuilder.Build(false, save, true);
            Assert.AreEqual(Loc.T("BAŞLA · ONAY"), d.Action, "kayıt varken ikinci Enter onaydır");
            var (status, message) = BriefingBuilder.Warning(save);
            Assert.AreEqual(Loc.T("KAYIT VAR"), status);
            Assert.AreEqual(Loc.F("V.{0:000} kaydının üstüne yazılır · onay: Enter", 14), message);

            d = BriefingBuilder.Build(true, save, false);
            Assert.IsTrue(d.Reading);
            Assert.AreEqual(Loc.T("KAPAT"), d.Action);
        }

        [Test]
        public void Summarize_ReplaysTheRecord()
        {
            Assert.IsNull(BriefingBuilder.Summarize(null));
            var sim = NewLiberation();
            SiginakFixtures.ApplyV014(sim);
            // Kaydın günü GameController.Save'de yazılır (son komuttan sonraki boş günler)
            sim.Record.day = sim.S.Day;
            var s = BriefingBuilder.Summarize(sim.Record);
            Assert.IsNotNull(s);
            Assert.AreEqual(14, s.Value.Shift);
            Assert.AreEqual(1, s.Value.Free);
            Assert.AreEqual(12, s.Value.Regions);
        }

        [Test]
        public void Body_StaysWithinDraftLimits()
        {
            var lang = DeskLoc.Instance.Lang;
            try
            {
                foreach (var (l, limit) in new[] { (DeskLang.Tr, 520), (DeskLang.En, 680) })
                {
                    DeskLoc.Instance.SetLanguage(l);
                    var d = BriefingBuilder.Build(false, null, false);
                    int chars = d.Paragraphs.Sum(p => p.Length) + d.Closing.Length;
                    Assert.LessOrEqual(chars, limit, l.ToString());
                    Assert.IsFalse(d.Paragraphs.Concat(new[] { d.Title, d.Action, d.Closing }).Any(string.IsNullOrEmpty), l.ToString());
                }
            }
            finally { DeskLoc.Instance.SetLanguage(lang); }
        }

        [Test]
        public void Opening_Skip_FiresRemainingCuesAndLightsTheRoom()
        {
            var go = new GameObject("Opening");
            var blackGo = new GameObject("Black", typeof(RectTransform), typeof(Image));
            try
            {
                var seq = go.AddComponent<OpeningSequence>();
                var black = blackGo.GetComponent<Image>();
                seq.Configure(black, null);
                var fired = new List<(OpeningSequence.Cue, bool)>();
                seq.Play((c, s) => fired.Add((c, s)));
                Assert.IsTrue(seq.Playing);
                Assert.IsTrue(black.gameObject.activeSelf, "açılış karanlıkta başlar");
                Assert.AreEqual(1, black.color.a, 1e-4);
                seq.Skip();
                Assert.IsFalse(seq.Playing);
                Assert.AreEqual(new[] { OpeningSequence.Cue.Lights, OpeningSequence.Cue.Document, OpeningSequence.Cue.End }, fired.Select(f => f.Item1).ToArray());
                Assert.IsTrue(fired.All(f => f.Item2));
                Assert.IsFalse(black.gameObject.activeSelf, "ışık geldi");
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(blackGo); }
        }

        [Test]
        public void Telex_HeaderLine_IsZeroAndCountingContinues()
        {
            var go = new GameObject("Telex");
            try
            {
                var feed = go.AddComponent<TelexFeed>();
                Assert.AreEqual("001/00", feed.Post(1, "KYGA-07 HAT AÇIK", "", TelexKind.Info, true).code);
                Assert.AreEqual("001/01", feed.Post(1, "ÖN KONTROL", "x", TelexKind.Info).code);
                Assert.AreEqual("001/02", feed.Post(1, "YAPILAMADI", "y", TelexKind.Failed).code);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Tray_ShowsBriefingUntilAReportIsFiled()
        {
            var go = new GameObject("Tray");
            var sheet = new GameObject("Sheet");
            try
            {
                var title = sheet.AddComponent<TextMeshPro>();
                var tray = go.AddComponent<EvrakTray>();
                tray.Configure(sheet, title);
                tray.Restore(null);
                Assert.IsFalse(sheet.activeSelf, "yeni oyunda açılışa kadar tepsi boş");
                tray.SetBriefing(true);
                Assert.IsTrue(sheet.activeSelf);
                Assert.AreEqual(Loc.T("HAREKÂT EMRİ"), title.text);
                tray.SetBriefingOut(true);
                Assert.IsFalse(sheet.activeSelf, "belge okunuyor");
                tray.SetBriefingOut(false);
                tray.Add(new ShiftReportInput { before = new ShiftSnapshot { shift = 3 }, after = new ShiftSnapshot { shift = 4 } });
                Assert.AreEqual(Loc.F("VARDİYA RAPORU · {0:000}", 3), title.text, "üstte son rapor");
                tray.SetBriefingOut(true);
                Assert.IsTrue(sheet.activeSelf, "belge raporların altında: üstteki kâğıt yerinde");
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(sheet); }
        }

        [Test]
        public void DeskGameScene_BriefingIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var briefing = roots.SelectMany(g => g.GetComponentsInChildren<SiginakBriefing>(true)).Single();
                var so = new SerializedObject(briefing);
                foreach (var f in new[] { "game", "surface", "rig", "telex", "tray", "shift", "view", "opening", "camera3d" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                var surface = roots.SelectMany(g => g.GetComponentsInChildren<LiberationDeskView>(true)).Single();
                Assert.AreSame(briefing, new SerializedObject(surface).FindProperty("siginakBriefing").objectReferenceValue);
                var desk = roots.SelectMany(g => g.GetComponentsInChildren<SiginakDesk>(true)).Single();
                Assert.AreSame(briefing, new SerializedObject(desk).FindProperty("briefing").objectReferenceValue);
                var shift = roots.SelectMany(g => g.GetComponentsInChildren<SiginakShift>(true)).Single();
                Assert.AreSame(briefing, new SerializedObject(shift).FindProperty("briefing").objectReferenceValue);

                var view = (BriefingView)so.FindProperty("view").objectReferenceValue;
                Assert.IsFalse(view.gameObject.activeSelf, "belge kapalı başlar");
                Assert.AreEqual(3, new SerializedObject(view).FindProperty("paragraphs").arraySize);
                var opening = (OpeningSequence)so.FindProperty("opening").objectReferenceValue;
                var black = (Image)new SerializedObject(opening).FindProperty("black").objectReferenceValue;
                Assert.AreEqual(0, black.transform.GetSiblingIndex(), "siyah örtü HUD'un en altında (ATLA plakası üstünde kalır)");
                Assert.IsFalse(black.gameObject.activeSelf);
                Assert.IsTrue(view.transform.GetSiblingIndex() > black.transform.GetSiblingIndex());
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
