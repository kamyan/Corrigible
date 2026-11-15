using System.Collections;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using K = Corrigible.Desk.Tests.AdmissionPlayKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 6 (sunum · sonlar) gerçek DeskGame'de, Play'de: İFŞA şeridi durumu izler, son şalter 5. vardiyadan önce mühürlü
    /// (tık yalnız titretir, komut yok), iki adım (kapak, kol) emir kâğıdını açar, Esc vazgeçer (komut yok), DAMGALA'ya gerçek ışınla
    /// tık <c>last_switch</c>'i yazar ve son raporu ÇEKİLDİ damgasıyla açar. Kayıtlar test klasöründe (<see cref="BoardPlayKit"/>);
    /// 5. vardiyaya kayıt fikstürüyle ulaşılır (olağan olaylar susturulmuş).
    /// </summary>
    public sealed class KosEndingsPlayTests
    {
        [UnityTearDown]
        public IEnumerator TearDown() { yield return ShiftPlayKit.End(); }

        static Simulation Sim => K.Sim;
        static SiginakLastSwitch Bridge => BoardPlayKit.Find<SiginakLastSwitch>();
        static ExposurePanel Exposure => BoardPlayKit.Desk != null ? BoardPlayKit.Desk.Exposure : null;

        static void Quiet(Balance b)
        {
            b.liberationAlarmPerShift = 0;
            b.desertStreak = 0;
            b.eventBigTheftMax = b.eventDeserterMax = b.eventLowRationMax = b.eventTheftMax = b.eventWoundedMax = 0;
            b.eventFeudMax = b.eventMemorialMax = b.eventBlackoutMax = b.eventTradeMax = b.eventCallMax = b.eventConvoyMax = 0;
        }

        static void RequireWired()
        {
            var bridge = Bridge;
            Assert.IsNotNull(bridge, "SiginakLastSwitch sahnede (SiginakRoom/Last switch controller)");
            Assert.IsNotNull(bridge.Switch, "son şalter");
            Assert.IsNotNull(bridge.Plate, "gerekçe plakası");
            Assert.IsNotNull(bridge.Order, "emir kâğıdı");
            Assert.AreSame(bridge, ShiftPlayKit.Get<SiginakLastSwitch>(BoardPlayKit.Surface, "lastSwitch"), "LiberationDeskView.lastSwitch");
            Assert.IsNotNull(Exposure, "SiginakDesk.exposure");
        }

        /// <summary>DeskGame, 5. vardiyada (fikstür + KAYITTAN DEVAM); kamera K1'de.</summary>
        static IEnumerator AtShiftFive()
        {
            yield return K.Begin();
            var sim = K.FixtureGame(BoardPlayKit.DefaultSeed, Quiet);
            K.FixtureToDay(sim, sim.B.lastSwitchMinShift - 1);
            yield return K.ContinueFrom(sim);
            RequireWired();
            yield return BoardPlayKit.Frames(3);
        }

        static IEnumerator Pull()
        {
            var sw = Bridge.Switch;
            BoardPlayKit.Click(sw.Target.gameObject);
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(ShiftPhase.CoverUp, sw.Phase, "1. tık kapağı kaldırır");
            yield return BoardPlayKit.Frames(1);
            Assert.AreEqual(LastSwitchViewBuilder.Variant(Sim).evac ? Loc.T("TAHLİYE · kolu indir") : Loc.T("ÇEKİLME · kolu indir"), Bridge.Plate.Text,
                "kapak kalkıkken de çeşit plakada");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.LastSwitch);
            BoardPlayKit.Click(sw.Target.gameObject);
            yield return BoardPlayKit.WaitFor(() => Bridge.OrderOpen, 3, "emir kâğıdı açılır");
            yield return BoardPlayKit.WaitFor(() => !Bridge.Order.Animating, 3, "kâğıt dinlenir");
        }

        [UnityTest]
        public IEnumerator Exposure_PanelFollowsState_AndBeaconBlinksOnWarning()
        {
            yield return K.Begin();
            RequireWired();
            Assert.IsTrue(Exposure.gameObject.activeInHierarchy, "olaylar açıkken şerit görünür");
            int steps = 0;
            System.Action<int> onStep = _ => steps++;
            Exposure.Stepped += onStep;
            try
            {
                Sim.Shelter.Exposure = 2;   // hata ayıklama kurulumu (komut değil)
                ShiftPlayKit.RaiseChanged();
                yield return BoardPlayKit.WaitFor(() => Exposure.LitCount == 2, 2, "iki hücre sırayla yanar");
                Assert.AreEqual(2, steps, "her hücre bir kez Stepped (ses)");
                Assert.AreEqual(0, Exposure.BeaconLevel);
                StringAssert.StartsWith(Loc.F("İfşa {0}.", Loc.F("{0}/{1}", 2, Sim.B.exposureThreshold)), Exposure.Target.Line);

                Sim.Shelter.Exposure = Sim.B.exposureThreshold;
                Sim.Shelter.FallPending = true;
                ShiftPlayKit.RaiseChanged();
                yield return BoardPlayKit.WaitFor(() => Exposure.LitCount == Exposure.CellCount, 3, "eşikte bütün hücreler");
                Assert.AreEqual(2, Exposure.BeaconLevel, "KONUM ÇÖZÜLDÜ: lamba yanıp söner");
                yield return BoardPlayKit.Frames(3);
                Assert.IsTrue(LastSwitchViewBuilder.Variant(Sim).evac, "uyarı vardiyasında çekilirse tahliye");
                // 1. vardiyada son şalter mühürlü: metin onu seçenek olarak önermez
                Assert.AreEqual(Loc.T("KONUM ÇÖZÜLDÜ · bu vardiya son vardiya: son baskın ya da sorgu."), Exposure.Target.Line);
            }
            finally { Exposure.Stepped -= onStep; }
        }

        [UnityTest]
        public IEnumerator LastSwitch_LockedBeforeShiftFive_DeniedWithoutCommand()
        {
            yield return K.Begin();
            RequireWired();
            yield return BoardPlayKit.Frames(2);
            var sw = Bridge.Switch;
            Assert.AreEqual(ShiftPhase.Locked, sw.Phase, "1. vardiyada mühürlü");
            Assert.AreEqual(Loc.F("mühürlü · {0}. vardiya", Sim.B.lastSwitchMinShift), Bridge.Plate.Text);
            Assert.AreEqual(Sim.LastSwitchCheck(), Bridge.Plate.Line, "üzerine gelme satırı Core gerekçesi");
            Assert.AreEqual(LastSwitchViewBuilder.SwitchName, Bridge.Plate.Line1.text, "plakanın adı köprüden");
            var mark = ShiftPlayKit.Mark();
            bool denied = false;
            System.Action onDeny = () => denied = true;
            var telexLines = new System.Collections.Generic.List<string>();
            System.Action<string, string> onTelex = (status, message) => telexLines.Add(status + " · " + message);
            sw.Denied += onDeny;
            Bridge.TelexLine += onTelex;
            try
            {
                BoardPlayKit.Click(sw.Target.gameObject);
                yield return BoardPlayKit.Frames(3);
                BoardPlayKit.Click(sw.Target.gameObject);
                yield return BoardPlayKit.Frames(3);
            }
            finally { sw.Denied -= onDeny; Bridge.TelexLine -= onTelex; }
            Assert.IsTrue(denied, "kilitliyken tık titretir");
            CollectionAssert.AreEqual(new[] { LastSwitchViewBuilder.SwitchName + " · " + Sim.LastSwitchCheck() }, telexLines,
                "titremenin yanında teleks nedeni bir kez söyler (art arda tık yinelemez)");
            Assert.AreEqual(ShiftPhase.Locked, sw.Phase);
            ShiftPlayKit.AssertUnchanged(mark, "mühürlü şalter komut yazmaz");
        }

        [UnityTest]
        public IEnumerator LastSwitch_TwoSteps_EscapeCancels_NoCommand()
        {
            yield return AtShiftFive();
            Assert.AreEqual(ShiftPhase.Ready, Bridge.Switch.Phase, "5. vardiyada hazır: " + Bridge.Switch.LockPlate);
            Assert.AreEqual(Loc.T("çekilirse: ÇEKİLME"), Bridge.Plate.Text);
            var mark = ShiftPlayKit.Mark();
            yield return Pull();
            Assert.AreEqual(Loc.T("ÇEKİLME EMRİ"), ShiftPlayKit.Get<TMPro.TMP_Text>(Bridge.Order, "title").text);
            Assert.IsTrue(BoardPlayKit.Surface.InputBlocked, "emir açıkken masa kilitli");
            BoardPlayKit.Surface.Escape();
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(Bridge.OrderOpen, "Esc kâğıdı kaldırır");
            Assert.IsFalse(Bridge.Order.gameObject.activeSelf);
            yield return BoardPlayKit.WaitFor(() => Bridge.Switch.Phase == ShiftPhase.Ready, 2, "kol kalkar, kapak kapanır");
            ShiftPlayKit.AssertUnchanged(mark, "damgasız vazgeçiş komut yazmaz");
            Assert.IsNull(Sim.S.Ending);
        }

        /// <summary>Sürüklemesiz sağ tık (kameranın <c>BackClicked</c> olayı; köprü ona abone) emir kâğıdını kaldırır, komut yazmaz.</summary>
        [UnityTest]
        public IEnumerator LastSwitch_RightClickBack_CancelsOrder_NoCommand()
        {
            yield return AtShiftFive();
            var mark = ShiftPlayKit.Mark();
            yield return Pull();
            StringAssert.Contains(Loc.T("Damgalamadan vazgeç: Esc ya da sağ tık."), string.Join("\n", LastSwitchViewBuilder.Order(Sim).Paragraphs), "görünür vazgeçiş satırı");
            var field = typeof(SiginakCameraRig).GetField("BackClicked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, "SiginakCameraRig.BackClicked olayının alanı");
            (field.GetValue(BoardPlayKit.Rig) as System.Action<SiginakPose>)?.Invoke(BoardPlayKit.Rig.Current);
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(Bridge.OrderOpen, "sağ tık kâğıdı kaldırır");
            yield return BoardPlayKit.WaitFor(() => Bridge.Switch.Phase == ShiftPhase.Ready, 2, "kol kalkar, kapak kapanır");
            ShiftPlayKit.AssertUnchanged(mark, "sağ tıkla vazgeçiş komut yazmaz");
            Assert.IsNull(Sim.S.Ending);
        }

        /// <summary>
        /// Gerçek karar zinciri: iz tutanağında "görmezden gel" satırı şeridin önizlemesini taşır ("İfşa 0/6 → 1/6."), karar komutu
        /// (<c>event_decide:yoksay</c>) ifşayı artırır, yeni hücre sırayla yanar (<see cref="ExposurePanel.Stepped"/>) ve tutanak elden
        /// çıkınca şerit dikkat flaşı yapar. Olayın açılışı hata ayıklama kurulumudur; karar komuttur.
        /// </summary>
        [UnityTest]
        public IEnumerator Exposure_RealDecision_PreviewOnPaper_StepsThenFlash()
        {
            yield return K.Begin();
            RequireWired();
            yield return BoardPlayKit.Frames(3);
            var person = Sim.Board.People.First(p => p.Alive);
            Sim.ShelterEvents.Pending = new ShelterEventInstance { Kind = "iz", OpenedShift = Sim.S.Day, PersonId = person.Id, Area = "uretim" };
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.Frames(3);
            var paper = ShelterEventSnapshotBuilder.Build(Sim, new ShelterEventUi());
            var ignore = paper.Rows.Single(r => r.Id == "yoksay");
            StringAssert.Contains(Loc.F("İfşa {0} → {1}.", Loc.F("{0}/{1}", 0, Sim.B.exposureThreshold), Loc.F("{0}/{1}", Sim.B.exposureIgnoredTrace, Sim.B.exposureThreshold)),
                ignore.Cost, "kâğıttaki bedel şeridin değeriyle");
            int steps = 0;
            System.Action<int> onStep = _ => steps++;
            Exposure.Stepped += onStep;
            try
            {
                int before = Sim.Record.commands.Count;
                var r = BoardPlayKit.Game.ExecuteBoardCommand(s => s.DecideShelterEvent("yoksay"));
                Assert.IsTrue(r.Ok, r.Msg);
                Assert.AreEqual(1, Sim.Record.commands.Count - before, "tek komut");
                Assert.AreEqual(Sim.B.exposureIgnoredTrace, Sim.Shelter.Exposure);
                yield return BoardPlayKit.WaitFor(() => Exposure.LitCount == Sim.B.exposureIgnoredTrace, 3, "yeni hücre yanar");
                Assert.AreEqual(Sim.B.exposureIgnoredTrace, steps, "her yeni hücre bir Stepped (ExposureStep sesi)");
                yield return BoardPlayKit.WaitFor(() => Exposure.Flashing, 6, "tutanak elden çıkınca dikkat flaşı");
            }
            finally { Exposure.Stepped -= onStep; }
        }

        [UnityTest]
        public IEnumerator LastSwitch_Withdrawal_StampClick_EndsWithWithdrawnReport()
        {
            yield return AtShiftFive();
            int before = Sim.Record.commands.Count;
            yield return Pull();
            var button = ShiftPlayKit.Get<Button>(Bridge.Order, "action");
            var rt = (RectTransform)button.transform;
            var screen = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
            var hit = BoardPlayKit.FirstHit(screen);
            Assert.IsTrue(hit.gameObject != null && hit.gameObject.transform.IsChildOf(button.transform) || hit.gameObject == button.gameObject,
                "DAMGALA ışınla ulaşılır: " + BoardPlayKit.DescribeHits(screen));
            BoardPlayKit.ClickHit(hit);
            yield return BoardPlayKit.WaitFor(() => Sim.S.Ending != null, 3, "damga komutu işler");
            Assert.AreEqual("salter", Sim.S.Ending);
            Assert.AreEqual("cekilme", Sim.Shelter.SwitchKind);
            var added = Sim.Record.commands.Skip(before).ToList();
            Assert.AreEqual(1, added.Count(c => c.kind == "last_switch"), "last_switch tam bir kez");
            yield return BoardPlayKit.WaitFor(() => FinalReportView.Instance != null && FinalReportView.Instance.IsOpen, 3, "son raporu açılır");
            yield return BoardPlayKit.WaitSeconds(2.5f);
            var stamp = ShiftPlayKit.Get<TMPro.TMP_Text>(FinalReportView.Instance, "stampText");
            Assert.AreEqual(Loc.T("ÇEKİLDİ"), stamp.text, "düzenli çekilme yenilgi değil");
            Assert.AreNotEqual(Loc.T("KAYBEDİLDİ"), stamp.text);
            Assert.IsTrue(FinalReportView.Instance.HasRecord, "SİCİL sekmesi var");
            FinalReportView.Instance.TogglePage();
            Assert.AreEqual(1, FinalReportView.Instance.Page);
        }
    }
}
