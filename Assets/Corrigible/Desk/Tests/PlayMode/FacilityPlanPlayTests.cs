using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 5 (§7.1), gerçek DeskGame: görev fişi açılınca bina planının üç paftası fişin iç yanında, üst bantta açılır
    /// (asset dengesi; tesis tek baskın kipi), kadrajdadır, fişi ve kadro raporunu örtmez, fiş kapanınca kapanır; TR ve EN. Paftalar
    /// komut yazmaz, RNG çekmez.
    /// </summary>
    public sealed class FacilityPlanPlayTests
    {
        [UnityTearDown] public IEnumerator TearDown() { yield return BoardPlayKit.TearDown(); }

        static T Field<T>(Object o, string name) => (T)o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);

        static Rect ScreenRect(RectTransform r)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);
            float x0 = c.Min(p => p.x), x1 = c.Max(p => p.x), y0 = c.Min(p => p.y), y1 = c.Max(p => p.y);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static void OnScreen(Rect r, string what)
        {
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(0f), what);
            Assert.That(r.xMax, Is.LessThanOrEqualTo((float)Screen.width), what);
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(0f), what);
            Assert.That(r.yMax, Is.LessThanOrEqualTo((float)Screen.height), what);
        }

        [UnityTest]
        public IEnumerator Plans_OpenBesideTheSlip_AndStayClearOfTheReport_InBothLanguages()
        {
            yield return BoardPlayKit.LoadDesk();
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var plans = Object.FindFirstObjectByType<FacilityPlanSheets>(FindObjectsInactive.Include);
            var slip = Object.FindFirstObjectByType<OperationSlip>(FindObjectsInactive.Include);
            var report = Object.FindFirstObjectByType<SquadReportSlip>(FindObjectsInactive.Include);
            Assert.IsNotNull(BoardPlayKit.Sim.FacilityPlan, "tesis planı");
            var before = BoardPlayKit.Snapshot();
            foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
            {
                DeskLoc.Instance.SetLanguage(lang);
                map.ShowMissions();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
                yield return BoardPlayKit.WaitFor(() => map.SlipOpen, 2, "görev fişi açılmadı");
                yield return BoardPlayKit.WaitSeconds(.7f);
                Assert.IsTrue(map.PlansOpen, lang + ": paftalar açık");
                Assert.AreEqual(slip.RegionId, plans.RegionId);
                Assert.AreEqual(3, plans.Data.Sheets.Count);
                Assert.IsTrue(plans.Data.Sheets.All(s => !s.Known), "yeni harekâtta bilinen oda yok");
                Assert.AreEqual(Corrigible.Core.Loc.T("PLAN YOK"), Field<TMPro.TMP_Text>(plans.Sheet(0), "stampWord").text);
                var band = ScreenRect(plans.Band);
                OnScreen(band, lang + ": paftalar kadrajda");
                Assert.IsFalse(band.Overlaps(ScreenRect(slip.Paper)), lang + ": paftalar fişi örtmez");
                Assert.AreEqual(1f, Field<CanvasGroup>(plans, "group").alpha, 0.01f);

                map.HandleEnter();
                yield return BoardPlayKit.WaitSeconds(.4f);
                Assert.IsTrue(map.SquadReportOpen);
                Assert.IsTrue(map.PlansOpen, "kadro raporu açıkken paftalar yerinde");
                Assert.IsFalse(band.Overlaps(ScreenRect(Field<RectTransform>(report, "paper"))), lang + ": paftalar kadro raporuyla çakışmaz");

                Assert.IsTrue(map.HandleEscape());
                Assert.IsTrue(map.HandleEscape());
                yield return BoardPlayKit.WaitSeconds(.5f);
                Assert.IsFalse(map.SlipOpen);
                Assert.IsFalse(map.PlansOpen, "fiş kapanınca paftalar kapanır");
                Assert.IsFalse(plans.gameObject.activeSelf);
                BoardPlayKit.Rig.ResetView();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            }
            Assert.AreEqual(before, BoardPlayKit.Snapshot(), "paftalar komut yazmaz, RNG çekmez");
        }
    }
}
