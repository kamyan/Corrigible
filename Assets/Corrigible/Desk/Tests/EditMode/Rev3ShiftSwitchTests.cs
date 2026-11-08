using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 4 · R19 vardiya şalteri ve plakası (PLAN §1.4, §1.9; W1): durumların açı / lamba / bant / tıklanırlık /
    /// plaka metni ve rengi token'la aynı (E1); süreler token'dan (kapak 0,25 açılış, 0,3 kapanış, kol 0,3 iki aşamalı, 4 s otomatik
    /// kapanış, ret dönüşü 0,4); fiş beklerken tık yok, dosyalama sonu kol ve kapak animasyonla döner (E2); kilitliyken tık reddedilir
    /// ve ön kenar 2 mm titrer (E3); kapak açıkken kilit kapağı kapatır, kol inmez (E4); eski kip (ayarlar dolabı, menü) aynen (E5);
    /// üzerine gelme 3 mm; kayıttan anında oturuş olaysız; üzerine gelme satırının tek yazıcısı plaka.
    /// <para>
    /// Sahneye bağlı değildir: şalter, plaka ve parçaları boş nesnelerle kurulur. Edit modda <c>Awake</c>/<c>Update</c> koşmadığı
    /// için süre <see cref="ShiftSwitch.Advance"/> ile kare kare (1/60 s) ilerletilir; toleranslar ± 1 kare.
    /// </para>
    /// </summary>
    public class Rev3ShiftSwitchTests
    {
        const float Dt = 1f / 60f;
        const float FrameTol = Dt * 1.5f;          // ± 1 kare (bir kare kayması + kayan nokta payı)
        const float Radius = 0.159f;               // ShiftSwitch.coverRadiusM varsayılanı (ön kenar, D §1.1)
        const string LampOnPath = "Assets/Corrigible/Desk/Art/Rev3/Materials/readyLamp.mat";
        const string LampOffPath = "Assets/Corrigible/Desk/Art/Rev3/Materials/readyLampOff.mat";
        const string RaidLine = "Baskın sürüyor: vardiya bitirilemez.";
        const string RaidPlate = "baskın sürüyor";

        static readonly string[] Keys = { "ready", "cover", "waiting", "locked" };

        readonly List<Object> created = new List<Object>();

        [TearDown]
        public void Clean()
        {
            if (DeskLoc.Instance.Lang != DeskLang.Tr) DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            Loc.SetLanguage(Loc.Turkish);
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        // ================================================================== E1 · durumlar = token

        [Test]
        public void SwitchStates_MatchShiftCloseTokens()
        {
            var t = Theme;
            Assert.AreEqual(0f, t.NumberOf("shiftClose.states.waiting.clickable", -1), "token: fiş bekliyor tıklanmaz");
            Assert.AreEqual("LockBand", t.Text("shiftClose.states.locked.band"), "token: kilitte LockBand görünür");
            string lockTr = t.Text("shiftClose.states.locked.plate2.tr"), lockEn = t.Text("shiftClose.states.locked.plate2.en");
            Assert.AreEqual(RaidPlate, lockTr, "token: kilit örneği baskın");

            foreach (var key in Keys)
            {
                var rig = Make();
                Reach(rig, key, RaidLine, lockTr);
                rig.Run(1f);
                string n = key + ": ";
                string k = "shiftClose.states." + key;

                Assert.AreEqual(PhaseOf(key), rig.Switch.Phase, n + "durum");
                Assert.IsFalse(rig.Switch.Moving, n + "hareket bitti");
                Assert.AreEqual(Num(t, k + ".cover"), XAngle(rig.Hinge), 0.01f, n + "kapak açısı");
                Assert.AreEqual(Num(t, k + ".lever"), XAngle(rig.Lever), 0.01f, n + "kol açısı");
                Assert.AreEqual(t.Text(k + ".lamp"), rig.Lamp.sharedMaterial.name, n + "hazır lambası");
                Assert.AreEqual(key == "locked", rig.Band.activeSelf, n + "kilit bandı");
                Assert.AreEqual(key != "waiting", rig.Target.Interactable, n + "tıklanır");

                string tr = key == "locked" ? lockTr : t.Text(k + ".plate2.tr");
                Assert.AreEqual(tr, rig.Plate.Text, n + "plaka TR");
                Assert.AreEqual(tr, rig.Line2.text, n + "TXT_Plate2 TR");
                AssertColor(t.ColorOf(key == "waiting" ? "shiftClose.plate.line2InkWaiting" : "shiftClose.plate.line2Ink"), rig.Line2.color, n + "plaka rengi");

                if (key == "locked")
                {
                    // Kilit nedeni köprüden o anki dilde gelir.
                    rig.Switch.SetDisabled(true, "Raid in progress: the shift cannot end.", lockEn);
                    Assert.AreEqual(lockEn, rig.Plate.Text, n + "plaka EN");
                }
                else
                {
                    Loc.SetLanguage(Loc.English, TokenTable(t));
                    rig.Plate.Relabel();
                    Assert.AreEqual(t.Text(k + ".plate2.en"), rig.Plate.Text, n + "plaka EN (kod anahtarı = token TR)");
                    Loc.SetLanguage(Loc.Turkish);
                }
            }
        }

        // ================================================================== süreler

        [Test]
        public void CoverAndLever_UseTokenDurations()
        {
            var t = Theme;
            float coverS = t.NumberOf("motion.shiftClose.coverS"), closeS = t.NumberOf("motion.shiftClose.coverCloseS");
            float leverS = t.NumberOf("motion.shiftClose.leverS"), autoS = t.NumberOf("shiftClose.states.cover.autoCloseS");
            Assert.AreEqual(0.25f, coverS, 1e-4f, "motion.shiftClose.coverS");
            Assert.AreEqual(0.3f, closeS, 1e-4f, "motion.shiftClose.coverCloseS");
            Assert.AreEqual(0.3f, leverS, 1e-4f, "motion.shiftClose.leverS");
            Assert.AreEqual(4f, autoS, 1e-4f, "shiftClose.states.cover.autoCloseS");
            float open = Num(t, "shiftClose.states.cover.cover"), up = Num(t, "shiftClose.states.ready.lever"), down = Num(t, "shiftClose.states.waiting.lever");

            var rig = Make();
            rig.Switch.Press();
            float t0 = rig.Time;
            CollectionAssert.AreEqual(new[] { ShiftSound.CoverOpen }, rig.Sounds, "kapak sesi");
            Assert.AreEqual(1, rig.Armed);
            while (rig.Time - t0 < coverS / 2 - 1e-5f) rig.Step();
            Assert.GreaterOrEqual(XAngle(rig.Hinge) / open, 0.6f, "kapak ease-out: yarı zamanda ≥ %60");
            rig.Until(() => !rig.Switch.Moving);
            Assert.AreEqual(coverS, rig.Time - t0, FrameTol, "kapak açılışı");
            Assert.AreEqual(open, XAngle(rig.Hinge), 0.01f);

            rig.Until(() => rig.Cancelled == 1, 6f);
            Assert.AreEqual(autoS, rig.Time - t0, FrameTol, "ikinci tık yoksa kapak kendiliğinden kapanır");
            Assert.AreEqual(ShiftPhase.Ready, rig.Switch.Phase);
            float t1 = rig.Time;
            rig.Until(() => !rig.Switch.Moving);
            Assert.AreEqual(closeS, rig.Time - t1, FrameTol, "kapak kapanışı");
            Assert.AreEqual(0f, XAngle(rig.Hinge), 0.01f);
            CollectionAssert.AreEqual(new[] { ShiftSound.CoverOpen, ShiftSound.CoverShut }, rig.Sounds);

            rig.Switch.Press();
            rig.Until(() => !rig.Switch.Moving);
            rig.Sounds.Clear();
            rig.Switch.Press();
            float t2 = rig.Time;
            Assert.AreEqual(1, rig.Thrown);
            CollectionAssert.AreEqual(new[] { ShiftSound.Lever }, rig.Sounds, "tek klip “klak-klunk”");
            while (rig.Time - t2 < leverS / 2 - 1e-5f) rig.Step();
            Assert.AreEqual((up + down) / 2, XAngle(rig.Lever), 0.5f, "iki aşamalı kol: ortada bekleme");
            rig.Until(() => rig.Arrived.Count == 1);
            Assert.AreEqual(leverS, rig.Time - t2, FrameTol, "kol inişi");
            Assert.IsTrue(rig.Arrived[0], "kol aşağıda durdu");
            Assert.AreEqual(down, XAngle(rig.Lever), 0.01f);
            Assert.IsTrue(rig.Switch.LeverDown);
        }

        // ================================================================== E2 · fiş bekliyor

        [Test]
        public void Waiting_IgnoresPress_ReleaseAnimates()
        {
            var t = Theme;
            float open = Num(t, "shiftClose.states.cover.cover"), up = Num(t, "shiftClose.states.ready.lever"), down = Num(t, "shiftClose.states.waiting.lever");
            float closeS = t.NumberOf("motion.shiftClose.coverCloseS"), leverS = t.NumberOf("motion.shiftClose.leverS");

            var rig = Make();
            rig.Switch.Press();
            rig.Run(0.3f);
            rig.Switch.Press();
            rig.Switch.SetAwaitingReceipt(true, false);      // köprü: basım kabul edildi (Thrown işleyicisi)
            Assert.AreEqual(ShiftPhase.Waiting, rig.Switch.Phase);
            rig.Until(() => rig.Arrived.Count == 1);
            Assert.IsTrue(rig.Arrived[0], "kol inince fiş çıkabilir");
            Assert.IsFalse(rig.Target.Interactable, "fiş beklenirken tıklanmaz");
            Assert.AreEqual(t.Text("shiftClose.states.waiting.lamp"), rig.Lamp.sharedMaterial.name);

            int armed = rig.Armed, thrown = rig.Thrown, sounds = rig.Sounds.Count;
            rig.Switch.Press();
            rig.Click();
            rig.Switch.Press();
            Assert.AreEqual((armed, thrown, sounds, 0), (rig.Armed, rig.Thrown, rig.Sounds.Count, rig.Denied), "basış yok sayılır");
            Assert.IsFalse(rig.Switch.Cancel(), "Esc kapağı kapatmaz");
            rig.Switch.Release();                             // SiginakShift.Abort: bekleme hâlinde no-op
            rig.Run(0.5f);
            Assert.AreEqual(ShiftPhase.Waiting, rig.Switch.Phase);
            Assert.AreEqual(open, XAngle(rig.Hinge), 0.01f);
            Assert.AreEqual(down, XAngle(rig.Lever), 0.01f);

            rig.Sounds.Clear();
            rig.Switch.SetAwaitingReceipt(false, false);      // dosyalama sonu (T7)
            float t0 = rig.Time;
            CollectionAssert.AreEqual(new[] { ShiftSound.CoverShut }, rig.Sounds);
            Assert.AreEqual(ShiftPhase.Ready, rig.Switch.Phase);
            Assert.AreEqual(ShiftSwitch.State.Idle, rig.Switch.Current);
            Assert.IsTrue(rig.Target.Interactable);
            Assert.AreEqual(t.Text("shiftClose.states.ready.lamp"), rig.Lamp.sharedMaterial.name);
            Assert.AreEqual(t.Text("shiftClose.states.ready.plate2.tr"), rig.Plate.Text);
            rig.Until(() => Mathf.Abs(rig.Switch.CoverDeg) < 1e-3f);
            Assert.AreEqual(closeS, rig.Time - t0, FrameTol, "kapak 0,3 s'de kapanır");
            rig.Until(() => rig.Arrived.Count == 2);
            Assert.AreEqual(leverS, rig.Time - t0, FrameTol, "kol 0,3 s'de kalkar");
            Assert.IsFalse(rig.Arrived[1], "kol yukarıda durdu");
            Assert.AreEqual(up, XAngle(rig.Lever), 0.01f);
        }

        // ================================================================== E3 · kilitli

        [Test]
        public void Locked_ClickDenied_Jiggles2mm_NoStateChange()
        {
            var t = Theme;
            float mm = t.NumberOf("motion.board.denyShakeMm", 2f), s = t.NumberOf("motion.board.denyShakeS", 0.18f);
            Assert.AreEqual(2f, mm, 1e-4f, "motion.board.denyShakeMm");

            var rig = Make();
            rig.Switch.SetDisabled(true, RaidLine, RaidPlate);
            rig.Run(0.2f);
            Assert.AreEqual(ShiftPhase.Locked, rig.Switch.Phase);
            Assert.AreEqual(ShiftSwitch.State.Disabled, rig.Switch.Current);
            Assert.IsTrue(rig.Band.activeSelf, "bant görünür");
            Assert.AreEqual(t.Text("shiftClose.states.locked.lamp"), rig.Lamp.sharedMaterial.name, "hazır lambası sönük");
            Assert.IsTrue(rig.Target.Interactable, "R19: kilitliyken tık ulaşır (yalnız titreme)");
            Assert.AreEqual(RaidPlate, rig.Plate.Text);
            Assert.AreEqual(RaidLine, rig.Target.LineOverride, "üzerine gelme satırı uzun neden");
            Assert.AreEqual(0f, rig.FrontLiftMm, 1e-3f);

            rig.Sounds.Clear();
            rig.Click();
            Assert.AreEqual(1, rig.Denied, "Denied bir kez");
            CollectionAssert.AreEqual(new[] { ShiftSound.Denied }, rig.Sounds);
            Assert.AreEqual((0, 0, 0), (rig.Armed, rig.Thrown, rig.Cancelled), "durum değişmez");
            Assert.AreEqual(ShiftSwitch.State.Disabled, rig.Switch.Current);

            float t0 = rig.Time, peak = 0, low = 0;
            while (rig.Time - t0 < s + 3 * Dt)
            {
                rig.Step();
                peak = Mathf.Max(peak, rig.FrontLiftMm);
                low = Mathf.Min(low, rig.FrontLiftMm);
                Assert.AreEqual(ShiftPhase.Locked, rig.Switch.Phase);
            }
            Assert.AreEqual(mm, peak, 0.1f, "ön kenar 2 mm kalkar");
            Assert.GreaterOrEqual(low, -0.01f, "kapak yuvasının altına inmez");
            Assert.AreEqual(0f, rig.FrontLiftMm, 1e-3f, "titreme biter");
            Assert.AreEqual(0f, XAngle(rig.Hinge), 1e-3f);

            rig.Switch.Press();                               // Enter
            Assert.AreEqual(2, rig.Denied, "Enter de reddedilir");
            CollectionAssert.AreEqual(new[] { ShiftSound.Denied, ShiftSound.Denied }, rig.Sounds);
            Assert.IsTrue(rig.Phases.All(p => p == ShiftPhase.Locked), "yalnız kilit durumu: " + string.Join(",", rig.Phases));
        }

        // ================================================================== E4 · kapak açıkken kilit

        [Test]
        public void LockWhileCoverOpen_ClosesCover_NoThrow()
        {
            var t = Theme;
            float closeS = t.NumberOf("motion.shiftClose.coverCloseS");
            var rig = Make();
            rig.Switch.Press();
            rig.Run(0.3f);
            Assert.AreEqual(ShiftPhase.CoverUp, rig.Switch.Phase);
            rig.Sounds.Clear();

            rig.Switch.SetDisabled(true, RaidLine, RaidPlate);
            float t0 = rig.Time;
            Assert.AreEqual(ShiftPhase.Locked, rig.Switch.Phase);
            Assert.AreEqual(ShiftSwitch.State.Disabled, rig.Switch.Current);
            Assert.AreEqual(0, rig.Cancelled, "kilit kapağı Cancelled bildirmeden kapatır");
            Assert.AreEqual(0, rig.Thrown);
            Assert.AreEqual(0f, rig.Switch.CoverGoalDeg, 1e-4f);
            CollectionAssert.AreEqual(new[] { ShiftSound.CoverShut }, rig.Sounds);
            Assert.IsTrue(rig.Band.activeSelf);
            rig.Until(() => !rig.Switch.Moving);
            Assert.AreEqual(closeS, rig.Time - t0, FrameTol, "kapak 0,3 s'de kapanır");
            Assert.AreEqual(0f, XAngle(rig.Hinge), 0.01f);

            rig.Switch.Press();
            Assert.AreEqual(0, rig.Thrown, "kilitliyken kol inmez");
            Assert.AreEqual(1, rig.Denied);
            rig.Run(5f);
            Assert.AreEqual(0, rig.Cancelled, "kilitliyken otomatik kapanış sayacı yok");

            rig.Switch.SetDisabled(false, null, null);
            Assert.AreEqual(ShiftPhase.Ready, rig.Switch.Phase);
            Assert.AreEqual(ShiftSwitch.State.Idle, rig.Switch.Current);
            Assert.IsFalse(rig.Band.activeSelf);
            Assert.AreEqual(t.Text("shiftClose.states.ready.plate2.tr"), rig.Plate.Text);
        }

        // ================================================================== E5 · eski kip

        [Test]
        public void LegacyMode_BehavesLikeBefore()
        {
            var t = Theme;
            foreach (bool withPlate in new[] { false, true })
            {
                string n = withPlate ? "plakalı: " : "plakasız: ";
                var rig = Make(shiftClose: false, withPlate: withPlate);
                var sw = rig.Switch;

                // SiginakDeskTests.ShiftSwitch_TwoStepsAndCancel'in adımları
                sw.Press();
                Assert.AreEqual(ShiftSwitch.State.CoverOpen, sw.Current, n);
                Assert.IsTrue(sw.Cancel(), n);
                Assert.AreEqual(ShiftSwitch.State.Idle, sw.Current, n);
                Assert.IsFalse(sw.Cancel(), n + "kapalı kapakta Esc ayarlara kalır");
                sw.Press(); sw.Press();
                Assert.AreEqual(ShiftSwitch.State.Thrown, sw.Current, n);
                Assert.AreEqual((2, 1, 1), (rig.Armed, rig.Thrown, rig.Cancelled), n);
                sw.Press();
                Assert.AreEqual(1, rig.Thrown, n + "kol inikken yeni basış yok sayılır");
                sw.Release();
                sw.SetDisabled(true, "baskın");
                sw.Press();
                Assert.AreEqual(ShiftSwitch.State.Disabled, sw.Current, n);
                Assert.AreEqual(2, rig.Armed, n);

                // Eski kipte kilit: satırı şalter yazar, tık ulaşmaz, titreme ve ses yok, plaka yazılmaz.
                Assert.AreEqual("baskın", rig.Target.LineOverride, n + "eski kipte satırı şalter yazar");
                Assert.IsFalse(rig.Target.Interactable, n + "kilitliyken tık ulaşmaz");
                Assert.IsTrue(rig.Band.activeSelf, n);
                Assert.AreEqual(LampOffPath, AssetDatabase.GetAssetPath(rig.Lamp.sharedMaterial), n);
                Assert.AreEqual(0, rig.Denied, n);
                if (withPlate) Assert.AreEqual(string.Empty, rig.Plate.Text, n + "eski kipte plaka yazılmaz");
                sw.SetDisabled(false);
                Assert.IsNull(rig.Target.LineOverride, n);
                Assert.IsTrue(rig.Target.Interactable, n);
                Assert.AreEqual(ShiftSwitch.State.Idle, sw.Current, n);

                // Eski süreler: kapak iki yönde motion.coverLiftS, kol 0,15 s.
                float lift = t.Number("motion.coverLiftS", 0.25f);
                sw.Press();
                float t0 = rig.Time;
                rig.Until(() => Mathf.Abs(sw.CoverDeg - 112f) < 1e-3f);
                Assert.AreEqual(lift, rig.Time - t0, FrameTol, n + "eski kapak açılışı");
                sw.Press();
                float t1 = rig.Time;
                rig.Until(() => Mathf.Abs(sw.LeverDeg + 35f) < 1e-3f);
                Assert.AreEqual(0.15f, rig.Time - t1, FrameTol, n + "eski kol 0,15 s");
                sw.Release();
                float t2 = rig.Time;
                rig.Until(() => Mathf.Abs(sw.CoverDeg) < 1e-3f);
                Assert.AreEqual(lift, rig.Time - t2, FrameTol, n + "eski kapak kapanışı açılışla aynı");

                // Bekleme ve ret
                sw.SetAwaitingReceipt(true);
                Assert.AreEqual(ShiftSwitch.State.Thrown, sw.Current, n);
                Assert.IsFalse(rig.Target.Interactable, n);
                sw.Press();
                sw.Release();
                Assert.AreEqual(ShiftSwitch.State.Thrown, sw.Current, n + "bekleme hâlinde Release no-op");
                sw.SetAwaitingReceipt(false);
                Assert.AreEqual(ShiftSwitch.State.Idle, sw.Current, n);
                sw.Press(); sw.Press();
                sw.RejectThrow();
                Assert.AreEqual(ShiftSwitch.State.Idle, sw.Current, n + "eski kipte ret = Release");

                Assert.IsEmpty(rig.Sounds, n + "eski kipte ses olayı yok");
                Assert.IsEmpty(rig.Phases, n + "eski kipte PhaseChanged yok");
                Assert.IsEmpty(rig.Arrived, n + "eski kipte LeverArrived yok");
            }
        }

        // ================================================================== ret (yetersiz plan)

        [Test]
        public void RejectThrow_DownThenUpIn04s_CoverUp()
        {
            var t = Theme;
            float leverS = t.NumberOf("motion.shiftClose.leverS"), rejectS = t.NumberOf("motion.shiftClose.rejectLeverS");
            float autoS = t.NumberOf("shiftClose.states.cover.autoCloseS"), closeS = t.NumberOf("motion.shiftClose.coverCloseS");
            Assert.AreEqual(0.4f, rejectS, 1e-4f, "motion.shiftClose.rejectLeverS (yerel token)");
            float open = Num(t, "shiftClose.states.cover.cover"), up = Num(t, "shiftClose.states.ready.lever"), down = Num(t, "shiftClose.states.waiting.lever");

            var rig = Make();
            rig.Switch.Thrown += rig.Switch.RejectThrow;     // köprü: PrintBoardShift reddedildi (Thrown işleyicisinde)
            rig.Switch.Press();
            rig.Run(0.3f);
            rig.Sounds.Clear();
            rig.Switch.Press();
            float t0 = rig.Time;
            Assert.AreEqual(ShiftPhase.Rejecting, rig.Switch.Phase);
            Assert.AreEqual("fiş basılamadı", rig.Plate.Text);
            Assert.AreEqual("readyLamp", rig.Lamp.sharedMaterial.name, "ret sürerken hazır lambası yanık");
            Assert.IsFalse(rig.Band.activeSelf);
            Assert.IsFalse(rig.Target.Interactable, "ret sürerken tıklanmaz");
            rig.Switch.Press();
            Assert.AreEqual(1, rig.Thrown, "ret sürerken basış yok sayılır");

            rig.Until(() => rig.Arrived.Count == 1);
            Assert.IsTrue(rig.Arrived[0], "kol aşağıya vardı (köprü boş ağız sesini çalar)");
            Assert.AreEqual(leverS, rig.Time - t0, FrameTol);
            Assert.AreEqual(down, XAngle(rig.Lever), 0.01f);
            Assert.AreEqual(open, XAngle(rig.Hinge), 0.01f, "kapak açık kalır");
            float t1 = rig.Time;
            rig.Until(() => rig.Arrived.Count == 2);
            Assert.IsFalse(rig.Arrived[1]);
            Assert.AreEqual(rejectS, rig.Time - t1, FrameTol, "kol 0,4 s'de geri kalkar");
            Assert.AreEqual(up, XAngle(rig.Lever), 0.01f);
            Assert.AreEqual(open, XAngle(rig.Hinge), 0.01f, "kapak açık kalır");
            Assert.AreEqual(ShiftPhase.CoverUp, rig.Switch.Phase);
            Assert.AreEqual(ShiftSwitch.State.CoverOpen, rig.Switch.Current);
            Assert.AreEqual(t.Text("shiftClose.states.cover.plate2.tr"), rig.Plate.Text);
            Assert.IsTrue(rig.Target.Interactable);
            Assert.AreEqual(0, rig.Cancelled);
            CollectionAssert.AreEqual(new[] { ShiftSound.Lever }, rig.Sounds, "ret kendi sesini çıkarmaz (boş ağız köprünün)");
            CollectionAssert.Contains(rig.Phases, ShiftPhase.Rejecting);
            Assert.AreEqual(ShiftPhase.CoverUp, rig.Phases.Last());

            float t2 = rig.Time;
            rig.Until(() => rig.Cancelled == 1, 6f);
            Assert.AreEqual(autoS, rig.Time - t2, FrameTol, "4 s sayacı kol kalkınca yeniden başlar");

            // Ret sürerken kilit gelirse (geçersiz plan bir sonraki karede kilitler) kol kalkınca kapak kapanır.
            var locked = Make();
            locked.Switch.Thrown += locked.Switch.RejectThrow;
            locked.Switch.Press();
            locked.Run(0.3f);
            locked.Switch.Press();
            locked.Step();
            locked.Switch.SetDisabled(true, "Kısıtlı sakin için ayrı gözetmen gerekiyor.", "plan düzeltilmeli");
            Assert.AreEqual(ShiftPhase.Rejecting, locked.Switch.Phase, "ret bitene dek ret");
            locked.Until(() => locked.Arrived.Count == 2);
            Assert.AreEqual(ShiftPhase.Locked, locked.Switch.Phase);
            Assert.AreEqual(0f, locked.Switch.CoverGoalDeg, 1e-4f);
            Assert.AreEqual(ShiftSound.CoverShut, locked.Sounds.Last());
            Assert.AreEqual(0, locked.Cancelled);
            float t3 = locked.Time;
            locked.Until(() => !locked.Switch.Moving);
            Assert.AreEqual(closeS, locked.Time - t3, FrameTol + Dt, "kapak 0,3 s'de kapanır");
            Assert.IsTrue(locked.Band.activeSelf);
            Assert.AreEqual("plan düzeltilmeli", locked.Plate.Text);
        }

        // ================================================================== üzerine gelme

        [Test]
        public void Hover_Ready_LiftsFrontEdge3mm()
        {
            var t = Theme;
            float mm = t.NumberOf("motion.shiftClose.hoverLiftMm");
            Assert.AreEqual(3f, mm, 1e-4f, "motion.shiftClose.hoverLiftMm (yerel token)");
            Assert.AreEqual(4f, t.NumberOf("motion.hoverLiftMm"), 1e-4f, "genel üzerine gelme kalkışı değişmez");

            var rig = Make();
            rig.Run(0.1f);
            Assert.AreEqual(0f, rig.FrontLiftMm, 1e-3f);
            rig.Target.OnPointerEnter(null);
            Assert.IsTrue(rig.Target.IsHovered);
            float t0 = rig.Time;
            rig.Until(() => rig.FrontLiftMm >= mm - 0.01f, 1f);
            Assert.LessOrEqual(rig.Time - t0, 0.08f + FrameTol, "DeskTarget'ın kalkış süresiyle");
            rig.Run(0.2f);
            Assert.AreEqual(mm, rig.FrontLiftMm, 0.05f, "ön kenar 3 mm kalkar");
            Assert.AreEqual(ShiftPhase.Ready, rig.Switch.Phase);
            Assert.AreEqual(0, rig.Sounds.Count + rig.Armed + rig.Denied, "üzerine gelme olay çıkarmaz");

            rig.Target.OnPointerExit(null);
            rig.Run(0.2f);
            Assert.AreEqual(0f, rig.FrontLiftMm, 0.01f, "çıkınca iner");

            rig.Switch.SetDisabled(true, RaidLine, RaidPlate);
            rig.Target.OnPointerEnter(null);
            rig.Run(0.2f);
            Assert.AreEqual(0f, rig.FrontLiftMm, 0.01f, "kilitliyken kalkmaz");
            rig.Switch.SetDisabled(false, null, null);
            rig.Run(0.2f);
            Assert.AreEqual(mm, rig.FrontLiftMm, 0.05f, "kilit kalkınca üzerindeyse kalkar");

            rig.Switch.Press();
            rig.Run(0.5f);
            Assert.AreEqual(Num(t, "shiftClose.states.cover.cover"), XAngle(rig.Hinge), 0.01f, "kapak açıkken kalkış eklenmez");
        }

        // ================================================================== kayıttan yükleme

        [Test]
        public void AwaitingInstant_Snaps_NoEvents()
        {
            var t = Theme;
            float open = Num(t, "shiftClose.states.waiting.cover"), up = Num(t, "shiftClose.states.ready.lever"), down = Num(t, "shiftClose.states.waiting.lever");

            var rig = Make(init: false);                       // ilk çağrı: Awake'ten önce gelen yükleme (A §7)
            rig.Switch.SetAwaitingReceipt(true, true);
            Assert.AreEqual(open, XAngle(rig.Hinge), 0.01f, "kapak anında açık");
            Assert.AreEqual(down, XAngle(rig.Lever), 0.01f, "kol anında aşağıda");
            Assert.IsTrue(rig.Switch.LeverDown);
            Assert.IsFalse(rig.Switch.Moving);
            Assert.AreEqual(ShiftPhase.Waiting, rig.Switch.Phase);
            Assert.AreEqual(t.Text("shiftClose.states.waiting.lamp"), rig.Lamp.sharedMaterial.name);
            Assert.IsFalse(rig.Target.Interactable);
            Assert.IsFalse(rig.Band.activeSelf);
            Assert.AreEqual(t.Text("shiftClose.states.waiting.plate2.tr"), rig.Plate.Text);
            AssertColor(t.ColorOf("shiftClose.plate.line2InkWaiting"), rig.Line2.color, "kırmızı satır");

            InvokeAwake(rig.Switch);                          // sahnede Awake sonra koşarsa duruşu ezmez
            rig.Run(1f);
            Assert.AreEqual(open, XAngle(rig.Hinge), 0.01f, "Awake/Update duruşu bozmaz");
            Assert.AreEqual(down, XAngle(rig.Lever), 0.01f);
            Assert.AreEqual((0, 0, 0, 0), (rig.Armed, rig.Thrown, rig.Cancelled, rig.Denied), "eylem olayı yok");
            Assert.IsEmpty(rig.Arrived, "LeverArrived yok (fiş yeniden çıkmaz)");
            Assert.IsEmpty(rig.Sounds, "ses yok");
            CollectionAssert.AreEqual(new[] { ShiftPhase.Waiting }, rig.Phases, "yalnız durum bildirimi");

            rig.Switch.Release();                             // SiginakShift.Abort (kayıttan yükleme)
            rig.Run(0.5f);
            Assert.AreEqual(ShiftPhase.Waiting, rig.Switch.Phase, "bekleme hâlinde Release no-op");
            Assert.AreEqual(down, XAngle(rig.Lever), 0.01f);

            rig.Switch.SetAwaitingReceipt(false, true);
            Assert.AreEqual(0f, XAngle(rig.Hinge), 0.01f);
            Assert.AreEqual(up, XAngle(rig.Lever), 0.01f);
            Assert.AreEqual(ShiftPhase.Ready, rig.Switch.Phase);
            Assert.IsEmpty(rig.Sounds);
            Assert.IsEmpty(rig.Arrived);

            rig.Click();
            Assert.AreEqual((1, 0), (rig.Armed, rig.Thrown), "tık aboneliği bir kez kuruldu");
        }

        // ================================================================== plaka

        [Test]
        public void Plate_LockReasonAndWaitingRed_TrEn()
        {
            var t = Theme;
            AssertColor(new Color32(0x4A, 0x3A, 0x22, 0xFF), t.ColorOf("shiftClose.plate.line2Ink"), "token line2Ink");
            AssertColor(new Color32(0x7A, 0x18, 0x10, 0xFF), t.ColorOf("shiftClose.plate.line2InkWaiting"), "token line2InkWaiting");
            Color ink = t.ColorOf("shiftClose.plate.line2Ink"), red = t.ColorOf("shiftClose.plate.line2InkWaiting");

            var rig = Make();
            Assert.AreEqual("kapağı kaldır", rig.Plate.Text);
            Assert.AreEqual("kapağı kaldır", rig.Target.LineOverride, "kilitsizken satır = plaka metni");

            var reasons = new[]
            {
                (RaidPlate, RaidLine),
                ("harekât bitti", "Harekât bitti."),
                ("belge açık", "Önce açılış belgesini kapat."),
                ("rapor açık", "Rapor açık: önce EVRAK tepsisine dosyala."),
                ("plan düzeltilmeli", "Kısıtlı sakin için ayrı gözetmen gerekiyor."),
            };
            foreach (var (plate, line) in reasons)
            {
                rig.Switch.SetDisabled(true, line, plate);
                Assert.AreEqual(plate, rig.Plate.Text, plate);
                Assert.AreEqual(plate, rig.Line2.text, plate);
                Assert.AreEqual(line, rig.Target.LineOverride, plate + ": üzerine gelme satırı uzun neden");
                Assert.AreEqual((plate, line), (rig.Switch.LockPlate, rig.Switch.LockLine));
                AssertColor(ink, rig.Line2.color, plate);
                Assert.IsTrue(rig.Band.activeSelf, plate);
            }
            rig.Switch.SetDisabled(true, null, "rapor açık");
            Assert.AreEqual("rapor açık", rig.Target.LineOverride, "uzun neden yoksa kısa neden");
            rig.Switch.SetDisabled(false, null, null);
            Assert.AreEqual("kapağı kaldır", rig.Plate.Text);
            Assert.AreEqual("kapağı kaldır", rig.Target.LineOverride);
            Assert.IsNull(rig.Switch.LockPlate);

            rig.Switch.Press();
            Assert.AreEqual("kolu indir", rig.Plate.Text);
            Assert.AreEqual("kolu indir", rig.Target.LineOverride);
            rig.Switch.Press();
            rig.Switch.SetAwaitingReceipt(true, false);
            Assert.AreEqual("fişi EVRAK’a koy", rig.Plate.Text, "tipografik kesme (token ve R19c)");
            Assert.AreEqual("fişi EVRAK’a koy", rig.Target.LineOverride);
            AssertColor(red, rig.Line2.color, "fiş bekliyor kırmızı");

            var en = TokenTable(t);
            en["fiş basılamadı"] = "slip not printed";
            Loc.SetLanguage(Loc.English, en);
            rig.Plate.Relabel();
            Assert.AreEqual("file the slip in PAPERS", rig.Plate.Text, "D3-A: EN'de EVRAK yerine PAPERS");
            AssertColor(red, rig.Line2.color);

            var reject = Make();
            reject.Switch.Thrown += reject.Switch.RejectThrow;
            reject.Switch.Press(); reject.Switch.Press();
            Assert.AreEqual("slip not printed", reject.Plate.Text);
            AssertColor(ink, reject.Line2.color);
            Loc.SetLanguage(Loc.Turkish);
            reject.Plate.Relabel();
            Assert.AreEqual("fiş basılamadı", reject.Plate.Text);

            // Dil değişince (DeskLoc.Changed) plaka kendiliğinden yeniden yazılır; çeviri tablodan (strings.txt).
            var fresh = Make();
            Assert.AreEqual("kapağı kaldır", fresh.Plate.Text);
            DeskLoc.Instance.SetLanguage(DeskLang.En);
            Assert.AreEqual(Loc.T("kapağı kaldır"), fresh.Plate.Text);
            Assert.AreNotEqual("kapağı kaldır", fresh.Plate.Text, "tabloda EN karşılığı var");
            Assert.AreEqual(fresh.Plate.Text, fresh.Target.LineOverride);
        }

        [Test]
        public void LineOverride_OnlyPlateWritesInShiftCloseMode()
        {
            var bare = Make(withPlate: false);
            bare.Switch.SetDisabled(true, RaidLine, RaidPlate);
            Assert.IsNull(bare.Target.LineOverride, "R19 kipinde şalter satırı yazmaz");
            Assert.AreEqual((RaidPlate, RaidLine), (bare.Switch.LockPlate, bare.Switch.LockLine), "nedenler saklanır");
            Assert.AreEqual(ShiftPhase.Locked, bare.Switch.Phase);
            Assert.IsTrue(bare.Band.activeSelf, "plakasız da bant görünür");
            bare.Target.LineOverride = "elle";
            for (int i = 0; i < 3; i++) bare.Switch.SetDisabled(true, RaidLine, RaidPlate);   // SiginakDesk.Update her karede
            Assert.AreEqual("elle", bare.Target.LineOverride);

            var rig = Make();
            rig.Switch.SetDisabled(true, RaidLine, RaidPlate);
            Assert.AreEqual(RaidLine, rig.Target.LineOverride);
            for (int i = 0; i < 3; i++) { rig.Switch.SetDisabled(true, RaidLine, RaidPlate); rig.Step(); }
            Assert.AreEqual(RaidLine, rig.Target.LineOverride, "her kare aynı neden: satır titremez");
            rig.Switch.SetDisabled(false, null, null);
            for (int i = 0; i < 3; i++) { rig.Switch.SetDisabled(false, null, null); rig.Step(); }
            Assert.AreEqual("kapağı kaldır", rig.Target.LineOverride);
        }

        // ================================================================== kurulum

        sealed class Rig
        {
            public ShiftSwitch Switch;
            public ShiftPlate Plate;
            public DeskTarget Target;
            public Transform Hinge, Lever, Front;
            public MeshRenderer Lamp;
            public GameObject Band;
            public TextMeshPro Line2;
            public int Armed, Thrown, Cancelled, Denied;
            public readonly List<bool> Arrived = new List<bool>();
            public readonly List<ShiftSound> Sounds = new List<ShiftSound>();
            public readonly List<ShiftPhase> Phases = new List<ShiftPhase>();
            /// <summary><see cref="ShiftSwitch.Advance"/>'a verilen sürelerin toplamı.</summary>
            public float Time;

            public void Step()
            {
                Switch.Advance(Dt);
                Time += Dt;
            }

            public void Run(float seconds)
            {
                int frames = Mathf.RoundToInt(seconds / Dt);
                for (int i = 0; i < frames; i++) Step();
            }

            /// <summary>Koşul doğru olana dek kare kare ilerler.</summary>
            public void Until(Func<bool> done, float limit = 2f)
            {
                float start = Time;
                while (!done())
                {
                    if (Time - start > limit) Assert.Fail("koşul " + limit + " s içinde gerçekleşmedi");
                    Step();
                }
            }

            /// <summary>Kapağın ön kenarının (menteşeden 0,159 m önde) kapalı hâle göre kalkışı, mm.</summary>
            public float FrontLiftMm => Front.position.y * 1000f;

            public void Click() => Target.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
        }

        Rig Make(bool shiftClose = true, bool withPlate = true, bool init = true)
        {
            var root = new GameObject("Shift switch");
            created.Add(root);
            root.AddComponent<BoxCollider>();
            var target = root.AddComponent<DeskTarget>();
            target.BlockedOverride = () => false;            // odanın girdi kilidinden bağımsız
            var hinge = Child(root.transform, "Cover");
            var front = Child(hinge, "Front edge");
            front.localPosition = new Vector3(0, 0, -Radius);
            var band = Child(hinge, "LockBand").gameObject;
            var lever = Child(root.transform, "Lever");
            var lamp = Child(root.transform, "ReadyLamp").gameObject.AddComponent<MeshRenderer>();
            var on = AssetDatabase.LoadAssetAtPath<Material>(LampOnPath);
            var off = AssetDatabase.LoadAssetAtPath<Material>(LampOffPath);
            Assert.IsNotNull(on, LampOnPath);
            Assert.IsNotNull(off, LampOffPath);
            lamp.sharedMaterial = on;

            ShiftPlate plate = null;
            TextMeshPro line2 = null;
            if (withPlate)
            {
                line2 = Child(root.transform, "TXT_Plate2").gameObject.AddComponent<TextMeshPro>();
                plate = root.AddComponent<ShiftPlate>();
                plate.Configure(line2, target);
            }

            var sw = root.AddComponent<ShiftSwitch>();
            sw.Configure(hinge, lever, lamp, on, off, band, target);
            sw.ConfigureShiftClose(plate, shiftClose);

            var rig = new Rig
            {
                Switch = sw, Plate = plate, Target = target, Hinge = hinge, Lever = lever, Front = front, Lamp = lamp, Band = band, Line2 = line2,
            };
            sw.Armed += () => rig.Armed++;
            sw.Thrown += () => rig.Thrown++;
            sw.Cancelled += () => rig.Cancelled++;
            sw.Denied += () => rig.Denied++;
            sw.LeverArrived += rig.Arrived.Add;
            sw.Sound += rig.Sounds.Add;
            sw.PhaseChanged += rig.Phases.Add;
            if (init) sw.Advance(0);                         // Edit modda Awake yok: tembel kurulum
            return rig;
        }

        static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void Reach(Rig rig, string key, string lockLine, string lockPlate)
        {
            switch (key)
            {
                case "cover": rig.Switch.Press(); break;
                case "waiting": rig.Switch.Press(); rig.Switch.Press(); rig.Switch.SetAwaitingReceipt(true, false); break;
                case "locked": rig.Switch.SetDisabled(true, lockLine, lockPlate); break;
            }
        }

        static ShiftPhase PhaseOf(string key) => key == "cover" ? ShiftPhase.CoverUp
            : key == "waiting" ? ShiftPhase.Waiting
            : key == "locked" ? ShiftPhase.Locked
            : ShiftPhase.Ready;

        /// <summary>Plaka metinlerinin token EN karşılıkları (TR token → EN token); kodun Loc anahtarı token TR'siyle aynı olmalı.</summary>
        static Dictionary<string, string> TokenTable(SiginakTheme t)
        {
            var table = new Dictionary<string, string>();
            foreach (var key in new[] { "ready", "cover", "waiting" })
                table[t.Text("shiftClose.states." + key + ".plate2.tr")] = t.Text("shiftClose.states." + key + ".plate2.en");
            return table;
        }

        static SiginakTheme Theme
        {
            get
            {
                var t = SiginakTheme.Active;
                Assert.IsNotNull(t, "Resources/SiginakTheme yok");
                return t;
            }
        }

        static float Num(SiginakTheme t, string key)
        {
            Assert.IsTrue(t.TryNumber(key, out var v), "token yok: " + key);
            return v;
        }

        /// <summary>Yalnız x çevresindeki yerel dönüşün işaretli açısı (°; 112° gibi 90'ı aşan açılar için Euler'e güvenilmez).</summary>
        static float XAngle(Transform tr)
        {
            var q = tr.localRotation;
            float a = 2f * Mathf.Atan2(q.x, q.w) * Mathf.Rad2Deg;
            if (a > 180f) a -= 360f;
            else if (a <= -180f) a += 360f;
            return a;
        }

        static void AssertColor(Color expected, Color actual, string message = "renk")
        {
            Assert.AreEqual(expected.r, actual.r, 1.5f / 255f, message + " (r)");
            Assert.AreEqual(expected.g, actual.g, 1.5f / 255f, message + " (g)");
            Assert.AreEqual(expected.b, actual.b, 1.5f / 255f, message + " (b)");
        }

        static void InvokeAwake(ShiftSwitch sw)
        {
            var awake = typeof(ShiftSwitch).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(awake, "ShiftSwitch.Awake");
            awake.Invoke(sw, null);
        }
    }
}
