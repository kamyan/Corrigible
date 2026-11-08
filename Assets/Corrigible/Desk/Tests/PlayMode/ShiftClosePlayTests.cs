using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Where = Corrigible.Desk.ShiftReceiptPaper.Where;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Vardiya kapanışı, şalter → fiş → EVRAK (Revizyon 3 · Aşama 4, R19; PLAN §5.2 P1–P13, E §3.3, §4). DeskGame gerçek sahnesiyle
    /// Play'de, tohum 79 (<see cref="ShiftPlayKit.Begin"/>). Süreler token'dan (<c>motion.shiftClose.*</c>, <c>shiftClose.states.*</c>),
    /// tolerans token + iki kare (<see cref="BoardPlayKit.FrameTolerance"/>); olaylar <see cref="ShiftLog"/>'la, kareler
    /// <see cref="ShiftSample"/>'la ölçülür. Simülasyonu yalnız iki komut değiştirir: kol <c>board_print</c>, EVRAK'a kabul edilen
    /// bırakış <c>board_file</c> (gün tam bir kez); okumak, Esc, sağ tık, reddedilen bırakış ve kilitli tık komut yazmaz, RNG
    /// çekmez. Şalter tutamağı kapanışın bağından (sahnede iki <see cref="ShiftSwitch"/> var); kapanış sahneye bağlı değilse
    /// (W6 sahne adımından önce) testler nedeniyle düşer.
    /// </summary>
    public sealed class ShiftClosePlayTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return ShiftPlayKit.End();
        }

        static float Tol => BoardPlayKit.FrameTolerance;

        static void AssertDuration(float took, float seconds, string what) =>
            Assert.That(took, Is.InRange(seconds - Tol, seconds + Tol), what + ": " + took.ToString("0.000") + " s (beklenen " + seconds + " ± " + Tol.ToString("0.000") + ")");

        static void AssertColor(Color expected, Color actual, string what)
        {
            const float eps = 1.5f / 255f;
            Assert.That(Mathf.Abs(expected.r - actual.r) <= eps && Mathf.Abs(expected.g - actual.g) <= eps && Mathf.Abs(expected.b - actual.b) <= eps &&
                        Mathf.Abs(expected.a - actual.a) <= eps, what + ": " + actual + " (beklenen " + expected + ")");
        }

        static float Open => ShiftPlayKit.Token("shiftClose.states.cover.cover", 112);
        static float LeverUp => ShiftPlayKit.Token("shiftClose.states.ready.lever", 35);
        static float LeverDown => ShiftPlayKit.Token("shiftClose.states.waiting.lever", -35);

        /// <summary>Şalter hazır: kapak kapalı, kol yukarıda, hazır lambası yanık, bant yok, plaka "kapağı kaldır" (koyu kahve).</summary>
        static void AssertReady(string context)
        {
            var sw = ShiftPlayKit.Switch;
            var plate = ShiftPlayKit.Plate;
            Assert.AreEqual(ShiftPhase.Ready, sw.Phase, context + ": şalter hazır (" + sw.LockPlate + ")");
            Assert.AreEqual(0, sw.CoverGoalDeg, 0.01f, context + ": kapak kapalı");
            Assert.AreEqual(LeverUp, sw.LeverDeg, 0.01f, context + ": kol yukarıda (+35)");
            Assert.IsTrue(ShiftPlayKit.ReadyLampLit, context + ": hazır lambası yanık");
            Assert.IsFalse(ShiftPlayKit.LockBandShown, context + ": kilit bandı gizli");
            Assert.AreEqual(ShiftPlate.TextFor(ShiftPhase.Ready, null), plate.Text, context + ": plaka “kapağı kaldır”");
            AssertColor(ShiftPlate.InkFor(ShiftPhase.Ready), plate.Line2.color, context + ": plaka mürekkebi (shiftClose.plate.line2Ink)");
        }

        /// <summary>Fiş bekliyor: kapak açık, kol aşağıda, hazır lambası sönük, fiş ağzı lambası yanık, plaka kırmızı "fişi EVRAK’a koy".</summary>
        static void AssertWaiting(string context)
        {
            var sw = ShiftPlayKit.Switch;
            var plate = ShiftPlayKit.Plate;
            Assert.AreEqual(ShiftPhase.Waiting, sw.Phase, context + ": şalter fiş bekliyor");
            Assert.AreEqual(Open, sw.CoverDeg, 0.01f, context + ": kapak açık (112°)");
            Assert.AreEqual(LeverDown, sw.LeverDeg, 0.01f, context + ": kol aşağıda (−35°)");
            Assert.IsFalse(sw.Moving, context + ": şalter duruyor");
            Assert.IsFalse(sw.Target.Interactable, context + ": fiş beklenirken şalter tıklanmaz");
            Assert.IsFalse(ShiftPlayKit.ReadyLampLit, context + ": hazır lambası sönük (readyLampOff)");
            Assert.IsFalse(ShiftPlayKit.LockBandShown, context + ": kilit bandı gizli");
            Assert.IsTrue(ShiftPlayKit.Machine.LampLit, context + ": fiş ağzı lambası yanık");
            Assert.AreEqual(ShiftPlate.TextFor(ShiftPhase.Waiting, null), plate.Text, context + ": plaka “fişi EVRAK’a koy”");
            AssertColor(ShiftPlate.InkFor(ShiftPhase.Waiting), plate.Line2.color, context + ": plaka kırmızı (shiftClose.plate.line2InkWaiting)");
        }

        /// <summary>Kilitli: bant görünür, hazır lambası sönük, plaka kısa neden, üzerine gelme satırı uzun neden.</summary>
        static void AssertLocked(string plateText, string line, string context)
        {
            var sw = ShiftPlayKit.Switch;
            Assert.AreEqual(ShiftPhase.Locked, sw.Phase, context + ": şalter kilitli");
            Assert.AreEqual(plateText, sw.LockPlate, context + ": kilidin kısa nedeni");
            Assert.AreEqual(plateText, ShiftPlayKit.Plate.Text, context + ": plakanın 2. satırı kısa neden");
            if (line != null)
            {
                Assert.AreEqual(line, sw.LockLine, context + ": kilidin uzun nedeni");
                Assert.AreEqual(line, sw.Target.LineOverride, context + ": üzerine gelme satırı uzun neden (ShiftPlate yazar)");
            }
            Assert.IsTrue(ShiftPlayKit.LockBandShown, context + ": kilit bandı (KİLİTLİ) görünür");
            Assert.IsFalse(ShiftPlayKit.ReadyLampLit, context + ": hazır lambası sönük");
            Assert.AreEqual(0, sw.CoverGoalDeg, 0.01f, context + ": kapak kapalı");
        }

        /// <summary>
        /// Kilitli tık (T6): <see cref="ShiftSwitch.Denied"/> bir kez, kapağın ön kenarı ≤ <c>motion.board.denyShakeMm</c> (2 mm)
        /// kalkıp <c>motion.board.denyShakeS</c> (temada yok, yedek 0,18 s) içinde yuvasına iner; durum ve komut değişmez.
        /// </summary>
        static IEnumerator AssertDeniedJiggle(ShiftLog log, string context)
        {
            var sw = ShiftPlayKit.Switch;
            float denyMm = ShiftPlayKit.Token("motion.board.denyShakeMm", 2), denyS = ShiftPlayKit.TokenOr("motion.board.denyShakeS", 0.18f);
            int denied = log.Count("Denied"), clacks = log.Count("SwitchSound", ShiftSound.Denied.ToString());
            var mark = ShiftPlayKit.Mark();
            var samples = new List<ShiftSample>();
            BoardPlayKit.ResetFrameStats();
            ShiftPlayKit.ClickSwitch();
            Assert.AreEqual(denied + 1, log.Count("Denied"), context + ": kilitli tık reddedilir (Denied)");
            Assert.AreEqual(0, log.Count("Armed"), context + ": kapak kalkmaz");
            Assert.AreEqual(clacks + 1, log.Count("SwitchSound", ShiftSound.Denied.ToString()), context + ": “klak” (SwitchClack) ses olayı");
            yield return ShiftPlayKit.Watch(samples, denyS + Tol + 0.05f);
            float peak = CoverEdge(samples.Max(s => s.Cover));
            Assert.LessOrEqual(peak, denyMm + 0.05f, context + ": titreme ≤ 2 mm (ön kenar)");
            Assert.Greater(peak, denyMm * 0.5f, context + ": titreme görüldü (ön kenarın tepesi " + peak.ToString("0.00") + " mm)");
            Assert.LessOrEqual(sw.CoverDeg, 0.01f, context + ": kapak yuvasına döndü");
            Assert.IsTrue(samples.All(s => s.Phase == ShiftPhase.Locked), context + ": titreme sırasında durum kilitli kalır");
            ShiftPlayKit.AssertUnchanged(mark, context + ": kilitli tık");
        }

        static float CoverEdge(float deg) => ShiftPlayKit.CoverEdgeMm(deg);

        // ================================================================== P1

        /// <summary>
        /// P1: K1'de şaltere tık → kapak 0,25 s'de 112° (ease-out), plaka "kolu indir", teleks ÖN KONTROL, kamera K6'ya, komut yok;
        /// ikinci tık → kol 0,3 s'de −35°, tam bir <c>board_print</c> (eski gün), RNG aynı; fiş kol −35°'ye varınca ağızdan çıkar,
        /// 0,35 s'de dilde (±1 mm, ease-out: yarı sürede yolun ≥ %60'ı); hazır lambası sönük, fiş ağzı lambası yanık, plaka kırmızı;
        /// üçüncü tık bir şey değiştirmez.
        /// </summary>
        [UnityTest]
        public IEnumerator Cover_Lever_PrintsSlip_ToTongue()
        {
            yield return ShiftPlayKit.Begin();
            var sw = ShiftPlayKit.Switch;
            var paper = ShiftPlayKit.Paper;
            var plate = ShiftPlayKit.Plate;
            var sim = ShiftPlayKit.Sim;
            float coverS = ShiftPlayKit.Token("motion.shiftClose.coverS", 0.25f);
            float leverS = ShiftPlayKit.Token("motion.shiftClose.leverS", 0.3f);
            float exitS = ShiftPlayKit.Token("motion.shiftClose.slipExitS", 0.35f);
            AssertReady("açılış");
            Assert.IsFalse(ShiftPlayKit.Machine.LampLit, "açılışta fiş ağzı lambası sönük");
            var start = ShiftPlayKit.Mark();
            int day = sim.S.Day;
            var log = ShiftPlayKit.Log();
            var samples = new List<ShiftSample>();

            // 1. tık: kapak
            BoardPlayKit.ResetFrameStats();
            ShiftPlayKit.ClickSwitch();
            float clicked = Time.unscaledTime;
            Assert.AreEqual(1, log.Count("Armed"), "1. tık kapağı kaldırır (Armed)");
            Assert.AreEqual(ShiftPhase.CoverUp, sw.Phase, "kapak kalkık");
            Assert.AreEqual(ShiftPlate.TextFor(ShiftPhase.CoverUp, null), plate.Text, "plaka “kolu indir”");
            Assert.AreEqual(1, log.Count("SwitchSound", ShiftSound.CoverOpen.ToString()), "menteşe “tık” ses olayı");
            var precheck = ShiftPlayKit.LastTelex;
            Assert.AreEqual(Loc.T("ÖN KONTROL"), precheck.status, "kapak kalkınca teleks ÖN KONTROL");
            Assert.AreEqual(sim.BoardPlanCheck() ?? Loc.T("Plan hazır. Devir fişini çıkar."), precheck.message, "ön kontrolün iletisi");
            Assert.AreEqual(SiginakPose.ShiftClose, ShiftPlayKit.Rig.Current, "kapak kalkınca kamera K6'ya (SiginakShiftClose.PreCheck)");
            yield return ShiftPlayKit.WatchUntil(samples, () => !sw.Moving, 2, "kapak açılışı bitmedi");
            AssertDuration(samples.Last().At - clicked, coverS, "kapak 0 → 112°");
            Assert.AreEqual(Open, sw.CoverDeg, 0.01f, "kapak 112°'de");
            var coverMid = samples.FirstOrDefault(s => s.At - clicked >= coverS * 0.5f);
            if (coverMid.Frame != 0) Assert.GreaterOrEqual(coverMid.Cover / Open, 0.6f, "kapak ease-out (yarı sürede ≥ %60)");
            ShiftPlayKit.AssertUnchanged(start, "kapağı kaldırmak");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.ShiftClose);

            // 2. tık: kol, basım
            samples.Clear();
            BoardPlayKit.ResetFrameStats();
            ShiftPlayKit.ClickSwitch();
            float thrown = log.At("Thrown");
            var printed = ShiftPlayKit.Since(start.commands);
            Assert.AreEqual(1, printed.Count, "kol tam bir komut yazar: " + ShiftPlayKit.Describe(printed));
            Assert.AreEqual("board_print", printed[0].kind, "kolun komutu board_print");
            Assert.AreEqual(day, printed[0].day, "board_print eski günle");
            Assert.AreEqual(start.rng, sim.S.Rng.State, "basım RNG çekmez");
            Assert.IsTrue(sim.Board.Printed && sim.Board.Locked, "fiş basıldı, plan kilitli");
            Assert.AreEqual(sim.Board.ReceiptId, paper.ReceiptId, "kâğıdın kimliği basılan fişinki");
            Assert.IsTrue(paper.FeedPending, "fiş kol inmeden çıkmaz (bekleyen besleme)");
            Assert.AreEqual(Where.Hidden, paper.State, "kol inerken fiş ağızda");
            Assert.AreEqual(1, log.Count("SwitchSound", ShiftSound.Lever.ToString()), "“klak-klunk” ses olayı");
            yield return ShiftPlayKit.WatchUntil(samples, () => paper.State == Where.Tongue && !paper.FeedPending, 3, "fiş dile çıkmadı (" + paper.State + ")");

            float leverAt = log.At("Lever", "down", thrown);
            AssertDuration(leverAt - thrown, leverS, "kol +35 → −35°");
            float feedAt = log.At("Slip", Where.Feeding.ToString(), thrown);
            Assert.GreaterOrEqual(feedAt, leverAt, "fiş kol −35°'ye varınca çıkar");
            int leverFrame = log.Entries.First(e => e.Name == "Lever" && e.Value == "down").Frame;
            int feedFrame = log.Entries.First(e => e.Name == "Slip" && e.Value == Where.Feeding.ToString()).Frame;
            Assert.LessOrEqual(feedFrame - leverFrame, 1, "çıkış kolun varışıyla aynı (ya da sonraki) karede başlar");
            Assert.IsTrue(samples.Where(s => s.Frame < leverFrame).All(s => s.Slip == Where.Hidden), "kol inerken fiş görünmez");
            float tongueAt = log.At("Slip", Where.Tongue.ToString(), feedAt);
            AssertDuration(tongueAt - feedAt, exitS, "fiş çıkışı (ağız → dil)");
            Assert.LessOrEqual(ShiftPlayKit.FromTongueMm, 1f, "fiş dil pozunda (±1 mm)");
            var path = Vector3.Distance(paper.FeedFrom, paper.TongueTarget.position);
            // Fiş LateUpdate'te yürür; örnek eşyordamda (Update'ten sonra, LateUpdate'ten önce) alınır, yani i. örneğin konumu
            // (i − 1). karenin LateUpdate'idir. Yarı süre o önceki karenin zamanına göre aranır (test koşucusunun düşük kare hızında
            // bir kare çıkışın yarısı kadar sürebilir).
            int midIndex = -1;
            for (int i = 1; i < samples.Count; i++)
                if (samples[i].Slip == Where.Feeding && samples[i - 1].At - feedAt >= exitS * 0.5f) { midIndex = i; break; }
            if (midIndex > 0)
                Assert.GreaterOrEqual(Vector3.Distance(paper.FeedFrom, samples[midIndex].SlipPosition) / path, 0.6f, "çıkış ease-out (yarı sürede yolun ≥ %60'ı)");
            else TestContext.WriteLine("çıkışın ortası örneklenmedi (kare süresi " + BoardPlayKit.MaxFrameDt.ToString("0.000") + " s)");
            Assert.AreEqual(1, log.Count("SlipSound", ShiftSound.SlipPrint.ToString()), "çıkışın başında yazıcı “tırrt”");
            Assert.AreEqual(1, log.Count("SlipSound", ShiftSound.SlipTear.ToString()), "dilde kopma “çıt”");
            AssertWaiting("fiş dilde");
            Assert.AreEqual(day, sim.S.Day, "basım günü ilerletmez");

            // 3. tık: bir şey değişmez
            var afterPrint = ShiftPlayKit.Mark();
            ShiftPlayKit.ClickSwitch();
            sw.Press();
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(1, log.Count("Armed"), "3. tık kapağa dokunmaz");
            Assert.AreEqual(1, log.Count("Thrown"), "3. tık kolu yeniden indirmez");
            AssertWaiting("3. tıktan sonra");
            Assert.AreEqual(Where.Tongue, paper.State, "fiş dilde kalır");
            ShiftPlayKit.AssertUnchanged(afterPrint, "fiş beklenirken şaltere tık");
            Assert.AreEqual(day, sim.S.Day);
        }

        // ================================================================== P2

        /// <summary>
        /// P2: kapağı kaldır, ikinci tık yok → 4 s ± tolerans sonra kapak kendiliğinden kapanır (<c>Cancelled</c> bir kez,
        /// <c>motion.shiftClose.coverCloseS</c> 0,3 s'de 0°), plaka "kapağı kaldır"; kamera K6'da kalır (Karar D6); komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Cover_AutoCloses_After4s()
        {
            yield return ShiftPlayKit.Begin();
            var sw = ShiftPlayKit.Switch;
            float auto = ShiftPlayKit.Token("shiftClose.states.cover.autoCloseS", 4);
            float closeS = ShiftPlayKit.Token("motion.shiftClose.coverCloseS", 0.3f);
            var start = ShiftPlayKit.Mark();
            var log = ShiftPlayKit.Log();
            var samples = new List<ShiftSample>();

            ShiftPlayKit.ClickSwitch();
            float armed = log.At("Armed");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.ShiftClose);
            BoardPlayKit.ResetFrameStats();
            yield return ShiftPlayKit.WatchUntil(samples, () => log.Count("Cancelled") > 0, auto + 1, "kapak kendiliğinden kapanmadı");
            float cancelled = log.At("Cancelled");
            AssertDuration(cancelled - armed, auto, "otomatik kapanış (shiftClose.states.cover.autoCloseS)");
            Assert.IsTrue(samples.Where(s => s.At < cancelled).All(s => s.Phase == ShiftPhase.CoverUp), "4 s boyunca kapak kalkık");
            Assert.AreEqual(1, log.Count("SwitchSound", ShiftSound.CoverShut.ToString()), "kapanış ses olayı");

            samples.Clear();
            yield return ShiftPlayKit.WatchUntil(samples, () => !sw.Moving, 1, "kapak kapanışı bitmedi");
            float closed = samples.Count > 0 ? samples.Last().At : cancelled;
            AssertDuration(closed - cancelled, closeS, "kapak 112 → 0°");
            AssertReady("kendiliğinden kapanınca");

            yield return ShiftPlayKit.Watch(null, 0.6f);
            Assert.AreEqual(1, log.Count("Cancelled"), "Cancelled bir kez");
            Assert.AreEqual(0, log.Count("Thrown"), "kol inmedi");
            Assert.AreEqual(SiginakPose.ShiftClose, ShiftPlayKit.Rig.Current, "kapak kendiliğinden kapanınca kamera K6'da kalır (D6)");
            Assert.IsNull(log.First("Pose", null, cancelled), "kapanış kamerayı oynatmaz: " + log);
            ShiftPlayKit.AssertUnchanged(start, "kapağın kendiliğinden kapanışı");
        }

        // ================================================================== P3

        /// <summary>
        /// P3: dildeki fişe tık → 0,25 s'de el pozu; fiş ekran yüksekliğinin %67'si (± %2), dört köşesi kadrajda; masa kilitli, fişin
        /// kendisi tıklanır, kamera girdisi askıda. 5 s okuma (otomatik kapanıştan uzun) boyunca her karede gün, komut, RNG, basılı fiş
        /// ve kimlik aynı, şalter fiş bekliyor; Enter ve <c>Surface.AdvanceShift()</c> günü ilerletmez. Fişe ikinci tık 0,25 s'de dile
        /// döndürür.
        /// </summary>
        [UnityTest]
        public IEnumerator Slip_ToHand_Reading_DoesNotAdvanceTime()
        {
            yield return ShiftPlayKit.Begin();
            yield return ShiftPlayKit.PrintToTongue();
            var sim = ShiftPlayKit.Sim;
            var paper = ShiftPlayKit.Paper;
            var surface = BoardPlayKit.Surface;
            float toHandS = ShiftPlayKit.Token("motion.shiftClose.slipToHandS", 0.25f);
            var printed = ShiftPlayKit.Mark();
            int day = sim.S.Day;
            string id = sim.Board.ReceiptId;
            var log = ShiftPlayKit.Log();

            BoardPlayKit.ResetFrameStats();
            ShiftPlayKit.ClickSlip();
            float clicked = log.At("Slip", Where.ToHand.ToString());
            Assert.AreEqual(1, log.Count("SlipSound", ShiftSound.SlipLift.ToString()), "ele alış ses olayı (BoardLift)");
            Assert.IsTrue(ShiftPlayKit.Close.Holding, "fiş ele geliyor (Holding)");
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Hand, 1, "fiş ele gelmedi (" + paper.State + ")");
            AssertDuration(log.At("Slip", Where.Hand.ToString(), clicked) - clicked, toHandS, "dil → el");
            yield return BoardPlayKit.Frames(2);
            float fraction = ShiftPlayKit.SlipHeightFraction();
            Assert.That(fraction, Is.InRange(0.67f * 0.98f, 0.67f * 1.02f), "elde fiş ekran yüksekliğinin %67'si (" + fraction.ToString("0.000") + ")");
            Assert.IsTrue(ShiftPlayKit.SlipInFrame(), "eldeki fişin dört köşesi kadrajda");
            Assert.IsTrue(surface.InputBlocked, "fiş eldeyken masa kilitli");
            Assert.IsFalse(surface.BlockedExceptShift, "fişin kendisi tıklanır kalır (BlockedExceptShift)");
            Assert.IsTrue(ShiftPlayKit.Rig.InputSuspended, "fiş eldeyken kamera kısayolları, bakış ve sağ tık askıda");

            var samples = new List<ShiftSample>();
            yield return ShiftPlayKit.Watch(samples, 2.5f);
            yield return BoardPlayKit.PressKey(Key.Enter);
            Assert.IsFalse(surface.AdvanceShift(), "kartlı oyunda AdvanceShift vardiyayı bitirmez");
            yield return ShiftPlayKit.Watch(samples, 2.5f);
            foreach (var s in samples)
            {
                Assert.AreEqual(day, s.Day, "okurken gün ilerlemez (" + s.At.ToString("0.00") + ")");
                Assert.AreEqual(printed.commands, s.Commands, "okurken komut yazılmaz");
                Assert.AreEqual(printed.rng, s.Rng, "okurken RNG çekilmez");
                Assert.IsTrue(s.Printed, "okurken fiş basılı kalır");
                Assert.AreEqual(Where.Hand, s.Slip, "fiş elde kalır");
                Assert.AreEqual(ShiftPhase.Waiting, s.Phase, "şalter fiş bekliyor (kapak kendiliğinden kapanmaz)");
            }
            Assert.AreEqual(id, sim.Board.ReceiptId, "fişin kimliği aynı");
            Assert.AreEqual(id, paper.ReceiptId);
            ShiftPlayKit.AssertUnchanged(printed, "5 s okuma, Enter ve AdvanceShift");
            Assert.AreEqual(0, log.Count("Armed") + log.Count("Thrown"), "Enter fişi dosyalamaz, şaltere gitmez");

            BoardPlayKit.ResetFrameStats();
            ShiftPlayKit.ClickSlip();
            float back = log.At("Slip", Where.Returning.ToString(), clicked);
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Tongue, 1, "eldeki fişe tık dile döndürmedi");
            AssertDuration(log.At("Slip", Where.Tongue.ToString(), back) - back, toHandS, "el → dil");
            Assert.LessOrEqual(ShiftPlayKit.FromTongueMm, 1f, "fiş dil pozunda (±1 mm)");
            Assert.AreEqual(1, log.Count("SlipSound", ShiftSound.SlipReturn.ToString()), "dile dönüş ses olayı (BoardReturn)");
            ShiftPlayKit.AssertUnchanged(printed, "fişi dile geri koymak");
            AssertWaiting("fiş dile döndü");
        }

        // ================================================================== P4

        /// <summary>
        /// P4: fişi EVRAK dışına sürükleyip bırak → <c>motion.board.returnArcS</c> (0,25 s) içinde dile döner (±1 mm), kimlik aynı, hedef
        /// ışığı hiç yanmadı, komut yok; ikinci deneme EVRAK'a: tam bir gün (fiş takılı kalmaz).
        /// </summary>
        [UnityTest]
        public IEnumerator DropOutside_ReturnsToTongue_ThenEvrakWorks()
        {
            yield return ShiftPlayKit.Begin();
            yield return ShiftPlayKit.PrintToTongue();
            var sim = ShiftPlayKit.Sim;
            var paper = ShiftPlayKit.Paper;
            var tray = ShiftPlayKit.Tray;
            float returnS = ShiftPlayKit.Token("motion.board.returnArcS", 0.25f);
            var printed = ShiftPlayKit.Mark();
            int day = sim.S.Day;
            string id = sim.Board.ReceiptId;
            var log = ShiftPlayKit.Log();

            var from = ShiftPlayKit.ScreenOf(paper.transform.position);
            var outside = ShiftPlayKit.OutsidePoint(from, 10);
            var lit = new List<bool>();
            BoardPlayKit.ResetFrameStats();
            yield return ShiftPlayKit.DragSlip(new[] { outside }, 10, 2,
                _ => lit.Add(tray.ReceiptTargetLit || tray.TargetLight != null && tray.TargetLight.enabled));
            float released = Time.unscaledTime;
            Assert.AreEqual(0, log.Count("Filed"), "EVRAK dışı bırakış dosyalanmaz");
            Assert.IsFalse(ShiftPlayKit.Close.Filing, "dosyalama başlamadı");
            Assert.AreEqual(Where.Returning, paper.State, "fiş dile dönüyor (bulgu 17: askıda kalmaz)");
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Tongue, 1, "fiş dile dönmedi (" + paper.State + ")");
            AssertDuration(log.At("Slip", Where.Tongue.ToString(), released) - released, returnS, "EVRAK dışından dile dönüş");
            Assert.LessOrEqual(ShiftPlayKit.FromTongueMm, 1f, "fiş dil pozunda (±1 mm)");
            Assert.IsNotEmpty(lit, "sürükleme kareleri");
            Assert.IsTrue(lit.All(l => !l), "EVRAK dışındaki sürüklemede hedef ışığı hiç yanmaz");
            Assert.IsFalse(tray.ReceiptTargetLit, "hedef ışığı sönük");
            Assert.AreEqual(id, paper.ReceiptId, "kâğıdın kimliği aynı");
            Assert.AreEqual(id, sim.Board.ReceiptId, "basılı fiş aynı");
            Assert.AreEqual(1, log.Count("SlipSound", ShiftSound.SlipReturn.ToString()), "dile dönüş ses olayı");
            ShiftPlayKit.AssertUnchanged(printed, "EVRAK dışı bırakış");
            Assert.AreEqual(day, sim.S.Day, "gün aynı");
            AssertWaiting("EVRAK dışı bırakıştan sonra");

            var before = ShiftPlayKit.Mark();
            yield return ShiftPlayKit.FileOnEvrak();
            var commands = ShiftPlayKit.Since(before.commands);
            var files = commands.Where(c => c.kind == "board_file").ToList();
            Assert.AreEqual(1, files.Count, "ikinci deneme tam bir board_file yazar: " + ShiftPlayKit.Describe(commands));
            Assert.AreEqual(id, files[0].a, "dosyalanan fiş basılan fiş");
            Assert.AreEqual(day, files[0].day, "board_file eski günle");
            Assert.AreEqual(day + 1, sim.S.Day, "gün tam bir kez ilerledi");
        }

        // ================================================================== P5

        /// <summary>
        /// P5 (bulgu 17): kâğıda bayat bir kimlik verilir (yalnız sunum), EVRAK'a bırakılır → <c>CanFile</c> reddeder, fiş tepside
        /// asılı kalmaz, 0,25 s'de dile döner; komut ve gün aynı. Ardından bir <c>Changed</c> gerçek fişi geri getirir.
        /// </summary>
        [UnityTest]
        public IEnumerator RejectedEvrakDrop_ReturnsToTongue()
        {
            yield return ShiftPlayKit.Begin();
            yield return ShiftPlayKit.PrintToTongue();
            var sim = ShiftPlayKit.Sim;
            var paper = ShiftPlayKit.Paper;
            var machine = ShiftPlayKit.Machine;
            var tray = ShiftPlayKit.Tray;
            float returnS = ShiftPlayKit.Token("motion.board.returnArcS", 0.25f);
            string real = sim.Board.ReceiptId;
            int day = sim.S.Day;
            var printed = ShiftPlayKit.Mark();
            var log = ShiftPlayKit.Log();

            const string stale = "eski-fis";
            machine.Present(stale, paper.Facts, false);
            Assert.AreEqual(stale, paper.ReceiptId, "kâğıtta bayat kimlik (yalnız sunum)");
            Assert.AreEqual(Where.Tongue, paper.State, "bayat fiş dilde");
            yield return BoardPlayKit.Frames(1);

            BoardPlayKit.ResetFrameStats();
            yield return ShiftPlayKit.DragSlip(new[] { ShiftPlayKit.TrayScreen() }, 8, 2);
            float released = Time.unscaledTime;
            Assert.AreEqual(0, log.Count("Filed"), "bayat kimlik EVRAK'a kabul edilmez (CanFile false)");
            Assert.IsFalse(ShiftPlayKit.Close.Filing, "dosyalama başlamadı");
            Assert.AreNotEqual(Where.ToTray, paper.State, "reddedilen fiş tepsiye inmez");
            Assert.AreNotEqual(Where.Tray, paper.State, "reddedilen fiş tepside asılı kalmaz");
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Tongue, 1, "reddedilen bırakış fişi dile döndürmedi (" + paper.State + ")");
            AssertDuration(log.At("Slip", Where.Tongue.ToString(), released) - released, returnS, "reddedilen bırakıştan dile dönüş");
            Assert.LessOrEqual(ShiftPlayKit.FromTongueMm, 1f, "fiş dil pozunda (±1 mm)");
            Assert.IsTrue(machine.LampLit, "fiş masada: ağız lambası yanık");
            Assert.IsFalse(tray.ReceiptTargetLit, "bırakışta hedef ışığı söner");
            ShiftPlayKit.AssertUnchanged(printed, "reddedilen EVRAK bırakışı");
            Assert.AreEqual(day, sim.S.Day, "gün aynı");
            Assert.IsTrue(sim.Board.Printed, "basılı fiş duruyor");
            Assert.AreEqual(real, sim.Board.ReceiptId, "Core'un fişi aynı");

            ShiftPlayKit.RaiseChanged();
            Assert.AreEqual(real, paper.ReceiptId, "Changed gerçek fişi geri getirir");
            Assert.AreEqual(Where.Tongue, paper.State, "gerçek fiş dilde");
            yield return BoardPlayKit.Frames(2);
            ShiftPlayKit.AssertUnchanged(printed, "Changed tazelemesi");
            AssertWaiting("gerçek fiş geri geldi");
        }

        // ================================================================== P6

        /// <summary>
        /// P6: EVRAK üstünde hedef ışığı açık (#FFB44A, 0,55 × ölçek, 0,35 m, gölgesiz), dışında kapalı; kabul edilen bırakıştan
        /// (t = 0) fiş 0,2 s'de tepsiye, ağız lambası söner; 0,2–0,3 kararma, 0,3'te tam karanlıkta tam bir <c>board_file</c>
        /// (<c>a == ReceiptId</c>, eski gün), gün tam bir kez +1 (o anda alfa ≥ 0,99, ışın alınır, VARDİYA hâlâ eski), kamera K1'e kesilir;
        /// ikizle RNG, gün, erzak ve kişiler eşit; 0,3–0,4 açılma; 0,4'te şalter 0,3 s'de hazıra, VARDİYA 0,4 s'de döner, teleks
        /// "VARDİYA DEVREDİLDİ" (kod yeni vardiya) 1,2 s'de basılır; pano açık, fiş gizli, son fiş tepside (D7-A). 2 s sonra gün yine +1.
        /// <c>board_file</c> dışındaki komutlar (RetireJobs; E §8.13) adıyla yazılır.
        /// </summary>
        [UnityTest]
        public IEnumerator DropOnEvrak_AdvancesExactlyOneDay()
        {
            yield return ShiftPlayKit.Begin();
            yield return ShiftPlayKit.PrintToTongue();
            var sim = ShiftPlayKit.Sim;
            var sw = ShiftPlayKit.Switch;
            var paper = ShiftPlayKit.Paper;
            var machine = ShiftPlayKit.Machine;
            var tray = ShiftPlayKit.Tray;
            var fade = ShiftPlayKit.Fade;
            float toTrayS = ShiftPlayKit.Token("motion.shiftClose.slipToTrayS", 0.2f);
            float darkS = ShiftPlayKit.Token("motion.shiftClose.darkS", 0.2f);
            float countersS = ShiftPlayKit.Token("motion.shiftClose.countersS", 0.4f);
            float telexS = ShiftPlayKit.Token("motion.shiftClose.telexPrintS", 1.2f);
            float leverS = ShiftPlayKit.Token("motion.shiftClose.leverS", 0.3f);
            float fileAt = toTrayS + darkS * 0.5f, lightAt = fileAt + darkS * 0.5f;
            int day = sim.S.Day, shiftBefore = BoardPlayKit.Surface.Shift;
            string id = sim.Board.ReceiptId;
            Assert.AreEqual(shiftBefore, (int)ShiftPlayKit.ShiftCounter.Value, "VARDİYA sayacı devirden önce");

            var twin = ShiftPlayKit.Twin();
            Assert.AreEqual(ShiftPlayKit.Fingerprint(sim), ShiftPlayKit.Fingerprint(twin), "ikiz canlıyla aynı yerden başlar");
            var before = ShiftPlayKit.Mark();
            var log = ShiftPlayKit.Log();
            var light = tray.TargetLight;
            Assert.IsNotNull(light, "EVRAK hedef ışığı (Paper tray/Slip target light; W6)");
            Assert.IsFalse(light.enabled, "hedef ışığı sürükleme yokken kapalı");

            // sürükleme: önce EVRAK dışı, sonra EVRAK üstü; bırakmadan bekle
            var from = ShiftPlayKit.ScreenOf(paper.transform.position);
            var outside = ShiftPlayKit.OutsidePoint(from, 8);
            var over = ShiftPlayKit.TrayScreen();
            var lit = new List<(int leg, bool lit, bool on)>();
            BoardPlayKit.ResetFrameStats();
            yield return ShiftPlayKit.DragSlip(new[] { outside, over }, 8, 4, leg => lit.Add((leg, tray.ReceiptTargetLit, light.enabled)));
            float released = log.At("Filed");
            Assert.AreEqual(id, log.Entries.First(e => e.Name == "Filed").Value, "EVRAK'a kabul edilen fiş basılan fiş");
            Assert.IsTrue(lit.Where(l => l.leg == 0).All(l => !l.lit && !l.on), "EVRAK dışında hedef ışığı kapalı");
            Assert.IsTrue(lit.Where(l => l.leg == 2).All(l => l.lit && l.on), "EVRAK üstünde hedef ışığı açık");
            Assert.IsFalse(light.enabled, "bırakışta hedef ışığı söner");
            var theme = SiginakTheme.Active;
            Assert.AreEqual(LightType.Point, light.type, "hedef ışığı nokta (shiftClose.evrakHighlight.type)");
            AssertColor(theme.ColorOf("shiftClose.evrakHighlight.color"), light.color, "hedef ışığının rengi token'dan");
            AssertColor(new Color32(0xFF, 0xB4, 0x4A, 0xFF), light.color, "hedef ışığı #FFB44A (PLAN §1.7)");
            float scale = ShiftPlayKit.Get<float>(tray, "targetIntensityScale");
            Assert.AreEqual(ShiftPlayKit.Token("shiftClose.evrakHighlight.intensity", 0.55f) * scale, light.intensity, 1e-4f, "hedef ışığı şiddeti (0,55 × targetIntensityScale)");
            Assert.AreEqual(ShiftPlayKit.Token("shiftClose.evrakHighlight.range", 0.35f), light.range, 1e-4f, "hedef ışığı menzili");
            Assert.AreEqual(LightShadows.None, light.shadows, "hedef ışığı gölgesiz");
            Assert.IsFalse(machine.LampLit, "kabul edilen bırakışta fiş ağzı lambası söner");
            Assert.IsTrue(ShiftPlayKit.Close.Filing, "dosyalama başladı");
            Assert.AreEqual(day, sim.S.Day, "bırakış anında gün henüz aynı");

            var samples = new List<ShiftSample>();
            yield return ShiftPlayKit.WatchUntil(samples,
                () => !ShiftPlayKit.Close.Filing && !sw.Moving && !ShiftPlayKit.ShiftCounterRolling && !ShiftPlayKit.TelexTyping, 4, "dosyalama dizisi bitmedi");

            // fiş tepsiye
            AssertDuration(log.At("Slip", Where.Tray.ToString(), released) - released, toTrayS, "fiş tepsiye (slipToTrayS)");
            Assert.AreEqual(1, log.Count("SlipSound", ShiftSound.SlipFile.ToString()), "tepside kâğıt “şap”");

            // kararma ve gün
            Assert.IsTrue(log.DayChanged, "gün ilerlemedi");
            AssertDuration(log.DayChangedAt - released, fileAt, "gün tam karanlıkta ilerler");
            Assert.GreaterOrEqual(log.AlphaAtDayChange, 0.99f, "gün ilerlediği anda kararma tam (alfa)");
            Assert.IsTrue(log.BlocksAtDayChange, "kararma ışın alır");
            Assert.AreEqual(shiftBefore, (int)log.ShiftCounterAtDayChange, "VARDİYA sayacı karanlıkta eski değerde (HoldCounters)");
            var dark = samples.Where(s => s.Alpha > 0.001f).ToList();
            Assert.IsNotEmpty(dark, "kararma görülmedi");
            AssertDuration(dark.First().At - released, toTrayS, "kararmanın başı (darkS / 2 kapanma)");
            AssertDuration(dark.Last().At - released, lightAt, "kararmanın sonu (darkS / 2 açılma)");
            Assert.IsTrue(dark.All(s => s.Blocks), "kararma sürerken ışın alınır (blocksRaycasts)");
            Assert.AreEqual(0, fade.alpha, 1e-4f, "kararma açıldı");
            Assert.IsFalse(fade.blocksRaycasts, "kararma açılınca ışın almaz");
            int cutFrame = log.Entries.First(e => e.Name == "Pose" && e.Value == SiginakPose.Desk.ToString() && e.At >= released).Frame;
            Assert.AreEqual(log.DayChangedFrame, cutFrame, "kamera gün ilerlediği karede, tam karanlıkta K1'e kesilir");

            // komutlar ve ikiz
            var commands = ShiftPlayKit.Since(before.commands);
            var files = commands.Where(c => c.kind == "board_file").ToList();
            Assert.AreEqual(1, files.Count, "tam bir board_file: " + ShiftPlayKit.Describe(commands));
            Assert.AreEqual(id, files[0].a, "board_file'ın fişi basılan fiş");
            Assert.AreEqual(day, files[0].day, "board_file eski güne yazılır");
            Assert.AreEqual("board_file", commands[0].kind, "bırakıştan sonraki ilk komut board_file");
            var others = commands.Where(c => c.kind != "board_file").ToList();
            if (others.Count > 0) TestContext.WriteLine("board_file dışı komutlar (RetireJobs, E §8.13): " + ShiftPlayKit.Describe(others));
            Assert.AreEqual(day + 1, sim.S.Day, "gün tam bir kez ilerledi");
            var twinFiled = twin.FileShift(id);
            Assert.IsTrue(twinFiled.Ok, "ikiz aynı fişi dosyalar: " + twinFiled.Msg);
            Assert.AreEqual(ShiftPlayKit.Fingerprint(twin), log.FingerprintAtDayChange, "devir ikizle aynı (RNG, gün, erzak, kişiler)");

            // sayaçlar ve teleks (karanlık açılınca)
            string handedOver = Loc.T("VARDİYA DEVREDİLDİ");
            float telexAt = log.At("Telex", handedOver, released);
            AssertDuration(telexAt - released, lightAt, "teleks ve sayaçlar karanlık açılınca");
            Assert.IsTrue(samples.Where(s => s.At < telexAt).All(s => (int)s.ShiftCounter == shiftBefore), "VARDİYA karanlık açılana dek eski değerde");
            Assert.AreEqual(shiftBefore + 1, (int)ShiftPlayKit.ShiftCounter.Value, "VARDİYA yeni vardiya");
            var rolling = samples.Where(s => s.At >= telexAt && s.ShiftRolling).ToList();
            Assert.IsNotEmpty(rolling, "VARDİYA çarkı döndü");
            var rolled = samples.FirstOrDefault(s => s.At > rolling.Last().At && !s.ShiftRolling);
            Assert.AreNotEqual(0, rolled.Frame, "VARDİYA çarkının durduğu kare");
            AssertDuration(rolled.At - telexAt, countersS, "VARDİYA çarkı (countersS)");
            var line = ShiftPlayKit.Telex.Lines.Last(l => l.status == handedOver);
            Assert.AreEqual(Loc.T("Devir fişi EVRAK kutusuna kondu. Yeni vardiya başladı."), line.message, "devir satırının iletisi");
            StringAssert.StartsWith((shiftBefore + 1).ToString("000") + "/", line.code, "satırın kodu yeni vardiyanın numarası");
            Assert.AreEqual(0, log.Entries.Count(e => e.Name == "Telex" && e.At >= released && e.At < telexAt), "karanlıkta teleks satırı basılmaz (ilk satır devir satırı)");
            Assert.AreEqual(telexS, log.TelexSeconds(handedOver), 1e-4f, "devir satırı telexPrintS sürede basılır");
            var typing = samples.Where(s => s.At >= telexAt && s.Typing).ToList();
            Assert.IsNotEmpty(typing, "teleks satırı basıldı");
            var typed = samples.FirstOrDefault(s => s.At > typing.Last().At && !s.Typing);
            Assert.AreNotEqual(0, typed.Frame, "teleks satırının bittiği kare");
            var laterLine = log.First("Telex", null, telexAt + 1e-4f);
            if (laterLine.HasValue && laterLine.Value <= typed.At)
                TestContext.WriteLine("devir satırından sonra başka bir teleks satırı basıldı (" + laterLine.Value.ToString("0.000") + "); 1,2 s ölçülmedi: " + log);
            else AssertDuration(typed.At - telexAt, telexS, "teleks satırı (telexPrintS)");

            // şalter, fiş, pano, kamera, EVRAK
            float leverUp = log.At("Lever", "up", released);
            AssertDuration(leverUp - telexAt, leverS, "kol −35 → +35°");
            Assert.AreEqual(1, log.Count("SwitchSound", ShiftSound.CoverShut.ToString()), "kapak kapanış ses olayı");
            AssertReady("devirden sonra");
            Assert.IsFalse(machine.LampLit, "fiş ağzı lambası sönük");
            Assert.AreEqual(Where.Hidden, paper.State, "canlı fiş gizli");
            Assert.IsFalse(paper.gameObject.activeSelf, "canlı fişin nesnesi kapalı");
            Assert.IsTrue(string.IsNullOrEmpty(paper.ReceiptId), "kâğıtta fiş yok");
            Assert.IsFalse(sim.Board.Printed, "yeni vardiyada fiş basılmamış");
            Assert.IsFalse(sim.Board.Locked, "pano açık");
            Assert.AreEqual(SiginakPose.Desk, ShiftPlayKit.Rig.Current, "kamera K1'de");
            Assert.IsFalse(ShiftPlayKit.Rig.InTransition, "K1'e kesme (geçiş yok)");
            Assert.IsNotNull(tray.LastFiled, "dosyalanan fiş tepside (D7-A)");
            Assert.AreEqual(id, tray.LastFiled.receiptId, "tepsideki fiş dosyalanan fiş");
            Assert.AreEqual(1, ShiftPlayKit.Close.Archive.Count, "EVRAK arşivinde bir fiş");
            var filed = ShiftPlayKit.FiledSlip;
            Assert.IsTrue(filed != null && filed.gameObject.activeSelf, "tepside dosyalanmış fişin kopyası görünür");
            Assert.IsFalse(ShiftPlayKit.Close.Busy, "kapanış meşgul değil (masa kilidi kalktı)");
            Assert.IsFalse(ShiftPlayKit.Rig.InputSuspended, "kamera girdisi askıda değil");

            var after = ShiftPlayKit.Mark();
            yield return ShiftPlayKit.Watch(null, 2f);
            Assert.AreEqual(day + 1, sim.S.Day, "2 s sonra gün yine +1 (ikinci Step yok)");
            Assert.AreEqual(0, ShiftPlayKit.Since(after.commands).Count(c => c.kind == "board_file"), "ikinci board_file yok");
        }

        // ================================================================== P7

        /// <summary>
        /// P7: dosyalanan fiş yeniden kullanılmaz: <c>FileBoardShift(eski)</c> reddedilir (komut, gün aynı); eski kimlik kâğıda zorla
        /// verilip EVRAK'a bırakılırsa reddedilir ve fiş dile döner; ertesi vardiyanın basımı yeni kimlikle (<c>vardiya-1</c>), eski
        /// (<c>vardiya-0</c>) onun yerine geçmez.
        /// </summary>
        [UnityTest]
        public IEnumerator SameReceipt_CannotBeReused()
        {
            yield return ShiftPlayKit.Begin();
            yield return ShiftPlayKit.PrintToTongue();
            var sim = ShiftPlayKit.Sim;
            var paper = ShiftPlayKit.Paper;
            var machine = ShiftPlayKit.Machine;
            var sw = ShiftPlayKit.Switch;
            string old = sim.Board.ReceiptId;
            var facts = paper.Facts;
            yield return ShiftPlayKit.FileOnEvrak();
            int day = sim.S.Day;

            var filed = ShiftPlayKit.Mark();
            var again = ShiftPlayKit.Game.FileBoardShift(old);
            Assert.IsFalse(again.Ok, "dosyalanmış fiş ikinci kez dosyalanmaz");
            ShiftPlayKit.AssertUnchanged(filed, "eski fişi yeniden dosyalamak");
            Assert.AreEqual(day, sim.S.Day, "gün aynı");

            // eski kimlik sunuma zorla verilir, EVRAK'a bırakılır
            var log = ShiftPlayKit.Log();
            machine.Present(old, facts, false);
            Assert.AreEqual(old, paper.ReceiptId, "kâğıtta eski kimlik (yalnız sunum)");
            Assert.AreEqual(Where.Tongue, paper.State);
            var forced = ShiftPlayKit.Mark();
            // Devirden sonra kamera K1'de: fişe tık K6'ya götürüp ele alır, sürükleme elden başlar
            yield return ShiftPlayKit.TakeToHand();
            yield return ShiftPlayKit.DragSlip(new[] { ShiftPlayKit.TrayScreen() }, 8, 2);
            Assert.AreEqual(0, log.Count("Filed"), "eski kimlik EVRAK'a kabul edilmez");
            Assert.IsFalse(ShiftPlayKit.Close.Filing, "dosyalama başlamadı");
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Tongue, 1, "reddedilen eski fiş dile dönmedi (" + paper.State + ")");
            ShiftPlayKit.AssertUnchanged(forced, "eski kimlikle EVRAK");
            Assert.AreEqual(day, sim.S.Day, "gün aynı");

            // gerçek görüntü: basılı fiş yok, şalter hazıra
            ShiftPlayKit.RaiseChanged();
            Assert.AreEqual(Where.Hidden, paper.State, "basılı fiş yokken kâğıt gizlenir");
            yield return BoardPlayKit.WaitFor(() => sw.Phase == ShiftPhase.Ready && !sw.Moving, 2, "şalter hazıra dönmedi (" + sw.Phase + ")");

            yield return ShiftPlayKit.PrintToTongue();
            string next = sim.Board.ReceiptId;
            Assert.AreEqual("vardiya-" + day, next, "ertesi basış yeni günün kimliğiyle");
            Assert.AreNotEqual(old, next, "yeni fiş eskisinden ayrı");
            var printed = ShiftPlayKit.Mark();
            var stale = ShiftPlayKit.Game.FileBoardShift(old);
            Assert.IsFalse(stale.Ok, "dünkü fiş (" + old + ") yeni basılı fişin (" + next + ") yerine geçmez");
            ShiftPlayKit.AssertUnchanged(printed, "dünkü fişle dosyalama");
            Assert.AreEqual(day, sim.S.Day, "gün aynı");
            Assert.IsTrue(sim.Board.Printed, "yeni fiş basılı kalır");
        }

        // ================================================================== P8

        /// <summary>
        /// P8 (Karar D1-A): girdisiz iş emri (<c>QueueProduction("emp_cell")</c>, <c>BoardPlanCheck</c> hatası) şalteri kilitler: bant,
        /// plaka "plan düzeltilmeli", üzerine gelme satırı Core'un iletisi; tık ve Enter yalnız titretir (≤ 2 mm), komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator InvalidPlan_Locks()
        {
            yield return ShiftPlayKit.Begin();
            var sim = ShiftPlayKit.Sim;
            var queued = ShiftPlayKit.Game.ExecuteBoardCommand(s => s.QueueProduction("emp_cell"));
            if (!queued.Ok) Assert.Inconclusive("girdisiz iş emri verilemedi: " + queued.Msg);
            string issue = sim.BoardPlanCheck();
            Assert.IsNotNull(issue, "girdisiz iş emri planı geçersiz kılar (BoardPlanCheck)");
            yield return BoardPlayKit.Frames(2);
            AssertLocked(Loc.T("plan düzeltilmeli"), issue, "geçersiz plan");
            Assert.IsTrue(ShiftPlayKit.Switch.Target.Interactable, "kilitliyken tık şaltere ulaşır (yalnız titreme)");
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "geçersiz planda masa açık");
            var log = ShiftPlayKit.Log();
            yield return AssertDeniedJiggle(log, "geçersiz plan");

            var mark = ShiftPlayKit.Mark();
            yield return BoardPlayKit.PressKey(Key.Enter);
            Assert.AreEqual(2, log.Count("Denied"), "Enter de reddedilir (T6)");
            Assert.AreEqual(0, log.Count("Armed"), "Enter kapağı kaldırmaz");
            yield return ShiftPlayKit.Watch(null, 0.3f);
            AssertLocked(Loc.T("plan düzeltilmeli"), issue, "Enter'den sonra");
            ShiftPlayKit.AssertUnchanged(mark, "kilitliyken Enter");
            Assert.IsFalse(sim.Board.Printed, "fiş basılmadı");
        }

        // ================================================================== P8b

        /// <summary>
        /// P8b (yetersiz kaynak yolu): plan aynı karede geçersizleşir ve iki <c>Press()</c> gelir (kilit bir sonraki Update'te
        /// yazılır) → kapak kalkar, kol 0,3 s'de iner, ağızdan fiş çıkmaz ("ağız boş" kolun varışında), teleks YAPILAMADI + Core
        /// iletisi, plaka "fiş basılamadı", kol <c>motion.shiftClose.rejectLeverS</c> (0,4 s) içinde kalkar; <c>board_print</c> yok.
        /// Ret bitince kilit görünür (plan düzeltilmeli).
        /// </summary>
        [UnityTest]
        public IEnumerator PrintRefusal_LeverDropsMouthEmpty()
        {
            yield return ShiftPlayKit.Begin();
            var sim = ShiftPlayKit.Sim;
            var sw = ShiftPlayKit.Switch;
            var plate = ShiftPlayKit.Plate;
            float leverS = ShiftPlayKit.Token("motion.shiftClose.leverS", 0.3f);
            float rejectS = ShiftPlayKit.Token("motion.shiftClose.rejectLeverS", 0.4f);
            var log = ShiftPlayKit.Log();
            var samples = new List<ShiftSample>();

            var queued = ShiftPlayKit.Game.ExecuteBoardCommand(s => s.QueueProduction("emp_cell"));
            if (!queued.Ok) Assert.Inconclusive("girdisiz iş emri verilemedi: " + queued.Msg);
            string issue = sim.BoardPlanCheck();
            Assert.IsNotNull(issue, "plan geçersiz");
            var mark = ShiftPlayKit.Mark();
            Assert.AreEqual(ShiftPhase.Ready, sw.Phase, "kilit henüz yazılmadı (SiginakDesk.Update bir sonraki karede)");
            BoardPlayKit.ResetFrameStats();
            sw.Press();
            sw.Press();
            Assert.AreEqual(1, log.Count("Armed"), "kapak kalktı");
            Assert.AreEqual(1, log.Count("Thrown"), "kol indi");
            float thrown = log.At("Thrown");
            Assert.IsFalse(sim.Board.Printed, "basım reddedildi");
            ShiftPlayKit.AssertUnchanged(mark, "reddedilen basım");
            Assert.AreEqual(ShiftPhase.Rejecting, sw.Phase, "şalter ret yolunda");
            Assert.AreEqual(ShiftPlate.TextFor(ShiftPhase.Rejecting, null), plate.Text, "plaka “fiş basılamadı”");
            Assert.IsTrue(ShiftPlayKit.Get<bool>(ShiftPlayKit.Close, "rejectPending"), "ağız boş sesi kolun varışını bekliyor");
            var failed = ShiftPlayKit.LastTelex;
            Assert.AreEqual(Loc.T("YAPILAMADI"), failed.status, "teleks YAPILAMADI");
            Assert.AreEqual(issue, failed.message, "teleks Core'un iletisini basar");

            yield return ShiftPlayKit.WatchUntil(samples, () => log.Count("Lever", "up") > 0 && !sw.Moving, 3, "kol geri kalkmadı");
            float down = log.At("Lever", "down", thrown);
            float up = log.At("Lever", "up", down);
            AssertDuration(down - thrown, leverS, "kol +35 → −35°");
            AssertDuration(up - down, rejectS, "kol ret dönüşü (rejectLeverS)");
            Assert.IsFalse(ShiftPlayKit.Get<bool>(ShiftPlayKit.Close, "rejectPending"), "kol inince ağız boş (MouthEmpty) çalındı");
            Assert.IsTrue(samples.All(s => s.Slip == Where.Hidden && !s.FeedPending), "ağızdan fiş çıkmaz");
            Assert.IsTrue(samples.All(s => !s.MouthLit), "fiş ağzı lambası sönük kalır");
            Assert.IsTrue(log.Entries.Any(e => e.Name == "Phase" && e.Value == ShiftPhase.Rejecting.ToString()), "Rejecting durumu yayınlandı");
            Assert.AreEqual(LeverUp, sw.LeverDeg, 0.01f, "kol yukarıda");
            Assert.AreEqual(0, ShiftPlayKit.Since(mark.commands).Count(c => c.kind == "board_print"), "board_print yok");
            ShiftPlayKit.AssertUnchanged(mark, "basım reddi");
            AssertLocked(Loc.T("plan düzeltilmeli"), issue, "ret bitince");
        }

        // ================================================================== P9

        /// <summary>
        /// P9: baskın emri doğrudan simülasyona (Baskin yüklenmez; yapay sunum durumu: gerçek oyunda baskın sırasında masa kilitli ve
        /// oda gizli, PLAN §1.9) → şalter kilitli "baskın sürüyor"; tık yalnız titretir (≤ 2 mm), komut yok. Baskın doğrudan çözülünce
        /// kilit kalkar.
        /// </summary>
        [UnityTest]
        public IEnumerator Locked_DuringRaid_ClickDenied()
        {
            yield return ShiftPlayKit.Begin();
            var sim = ShiftPlayKit.Sim;
            var sw = ShiftPlayKit.Switch;
            var raid = ShiftPlayKit.StartRaidDirect();
            if (!raid.Ok) Assert.Inconclusive("baskın emri verilemedi: " + raid.Msg);
            Assert.IsNotNull(sim.S.Raid, "baskın sürüyor");
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(RaidLauncher.IsRaidLoaded, "Baskin sahnesi yüklenmedi (Changed yok)");
            AssertLocked(Loc.T("baskın sürüyor"), Loc.T("Baskın sürüyor: vardiya bitirilemez."), "baskın");
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "yapay durum: masa açık, tık şaltere ulaşır");
            var log = ShiftPlayKit.Log();
            yield return AssertDeniedJiggle(log, "baskın");

            ShiftPlayKit.ResolveRaidDirect();
            Assert.IsNull(sim.S.Raid, "baskın çözüldü");
            yield return BoardPlayKit.Frames(2);
            Assert.AreNotEqual(Loc.T("baskın sürüyor"), sw.LockPlate, "baskın kilidi kalktı");
            if (sw.Phase == ShiftPhase.Ready) AssertReady("baskından sonra");
            else Assert.AreEqual(Loc.T("plan düzeltilmeli"), sw.LockPlate, "baskından sonra yalnız plan kilidi olabilir (" + sw.Phase + ")");
        }

        // ================================================================== P10

        /// <summary>
        /// P10: emir belgesi açıkken (<c>LoadDesk(79, passBriefing: false)</c>) şalter kilitli "belge açık"; masa kilitli olduğu için tık
        /// şaltere ulaşmaz (DeskTarget.Blocked), doğrudan basış yalnız reddedilir; komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Locked_WhileBriefingOpen()
        {
            yield return ShiftPlayKit.Begin(passBriefing: false);
            yield return BoardPlayKit.Frames(2);
            var sw = ShiftPlayKit.Switch;
            Assert.IsTrue(ShiftPlayKit.Game.StoryHold || BoardPlayKit.Briefing.Busy, "emir belgesi açık");
            AssertLocked(Loc.T("belge açık"), Loc.T("Önce açılış belgesini kapat."), "belge açık");
            Assert.IsTrue(BoardPlayKit.Surface.InputBlocked, "belge açıkken masa kilitli");
            var start = ShiftPlayKit.Mark();
            var log = ShiftPlayKit.Log();

            ShiftPlayKit.ClickSwitch();
            Assert.AreEqual(0, log.Count("Denied") + log.Count("Armed"), "masa kilitliyken tık şaltere ulaşmaz");
            sw.Press();
            Assert.AreEqual(1, log.Count("Denied"), "doğrudan basış reddedilir (T6)");
            Assert.AreEqual(0, log.Count("Armed"), "kapak kalkmaz");
            yield return ShiftPlayKit.Watch(null, 0.4f);
            AssertLocked(Loc.T("belge açık"), Loc.T("Önce açılış belgesini kapat."), "basıştan sonra");
            ShiftPlayKit.AssertUnchanged(start, "belge açıkken şalter");
            Assert.IsFalse(ShiftPlayKit.Sim.Board.Printed, "fiş basılmadı");
            Assert.AreEqual(Where.Hidden, ShiftPlayKit.Paper.State, "fiş yok");
        }

        // ================================================================== P11

        /// <summary>
        /// P11 (E §4): basılı fişle kaydet (her başarılı komut kaydeder) → aynı klasörden KAYITTAN DEVAM: kayıt sürümü 14, son komut
        /// <c>board_print</c>; devamda komut geçmişi, RNG, fişin kimliği, metni ve önizlemesi birebir; fiş dilde anında (çıkış
        /// oynamaz), şalter fiş bekliyor duruşunda olaysız, ağız lambası yanık, plaka kırmızı, pano kilitli. EVRAK'a bırakış tam bir
        /// gün ilerletir (ikizle aynı), ardından aynı fiş reddedilir.
        /// </summary>
        [UnityTest]
        public IEnumerator PrintedSlip_SurvivesSaveAndContinue()
        {
            yield return ShiftPlayKit.Begin();
            yield return ShiftPlayKit.PrintToTongue();
            var sim = ShiftPlayKit.Sim;
            var saved = SaveStore.Load();
            Assert.IsNotNull(saved, "basımdan sonra kayıt dosyası var (GameController.Run kaydeder)");
            Assert.AreEqual(15, GameRecord.CurrentVersion, "kayıt biçimi 15 (Aşama 6 K4-A gözetmen kuralı)");
            Assert.AreEqual(GameRecord.CurrentVersion, saved.version, "kaydın sürümü");
            Assert.AreEqual("board_print", saved.commands.Last().kind, "kaydın son komutu board_print");
            Assert.AreEqual(sim.S.Day, saved.day, "kaydın günü");
            var lines = ShiftPlayKit.CommandLines(sim.Record);
            CollectionAssert.AreEqual(lines, ShiftPlayKit.CommandLines(saved), "kayıt dosyası canlı komut geçmişiyle aynı");
            uint rng = sim.S.Rng.State;
            int day = sim.S.Day;
            string id = sim.Board.ReceiptId, text = sim.Board.ReceiptText, preview = sim.BoardReceiptPreview;

            yield return ShiftPlayKit.Continue();
            sim = ShiftPlayKit.Sim;
            var log = ShiftPlayKit.Log();
            var paper = ShiftPlayKit.Paper;
            CollectionAssert.AreEqual(lines, ShiftPlayKit.CommandLines(sim.Record), "kayıttan devam komut geçmişini değiştirmez");
            Assert.AreEqual(rng, sim.S.Rng.State, "RNG aynı");
            Assert.AreEqual(day, sim.S.Day, "gün aynı");
            Assert.IsTrue(sim.Board.Printed, "basılı fiş geri geldi");
            Assert.IsTrue(sim.Board.Locked, "pano kilitli");
            Assert.AreEqual(id, sim.Board.ReceiptId, "fişin kimliği aynı");
            Assert.AreEqual(text, sim.Board.ReceiptText, "fişin metni aynı");
            Assert.AreEqual(preview, sim.BoardReceiptPreview, "fişin önizlemesi aynı");
            Assert.IsTrue(paper.gameObject.activeSelf, "fiş görünür");
            Assert.AreEqual(Where.Tongue, paper.State, "fiş dilde (elde değil)");
            Assert.IsFalse(paper.FeedPending, "çıkış animasyonu oynamaz");
            Assert.IsFalse(paper.ReadOnly, "canlı fiş (arşiv değil)");
            Assert.AreEqual(id, paper.ReceiptId, "kâğıdın kimliği kayıttaki fiş");
            Assert.LessOrEqual(ShiftPlayKit.FromTongueMm, 1f, "fiş dil pozunda (±1 mm)");
            AssertWaiting("kayıttan devam");
            Assert.AreEqual(SiginakPose.Desk, ShiftPlayKit.Rig.Current, "kayıttan devam K1'de");

            var loaded = ShiftPlayKit.Mark();
            yield return ShiftPlayKit.Watch(null, 0.5f);
            var motion = log.Entries.Where(e => e.Name == "Lever" || e.Name == "Armed" || e.Name == "Thrown" || e.Name == "SwitchSound" ||
                                                e.Name == "Slip" || e.Name == "SlipSound").ToList();
            Assert.IsEmpty(motion, "yüklemeden sonra şalter ve fiş olay çıkarmaz: " + string.Join(", ", motion.Select(e => e.ToString())));
            Assert.LessOrEqual(ShiftPlayKit.FromTongueMm, 1f, "fiş dilde duruyor");
            AssertWaiting("yüklemeden 0,5 s sonra");
            ShiftPlayKit.AssertUnchanged(loaded, "yüklemeden sonra bekleyiş");

            // K1'den fişe tık: K6 ve el; sürükleme elden başlar (D5)
            yield return ShiftPlayKit.TakeToHand();
            ShiftPlayKit.AssertUnchanged(loaded, "kayıttan dönen fişi ele almak");
            var twin = ShiftPlayKit.Twin();
            var before = ShiftPlayKit.Mark();
            yield return ShiftPlayKit.FileOnEvrak();
            var commands = ShiftPlayKit.Since(before.commands);
            var files = commands.Where(c => c.kind == "board_file").ToList();
            Assert.AreEqual(1, files.Count, "kayıttan dönen fiş tam bir board_file: " + ShiftPlayKit.Describe(commands));
            Assert.AreEqual(id, files[0].a);
            Assert.AreEqual(day, files[0].day);
            Assert.AreEqual(day + 1, sim.S.Day, "gün tam bir kez ilerledi");
            var twinFiled = twin.FileShift(id);
            Assert.IsTrue(twinFiled.Ok, "ikiz aynı fişi dosyalar: " + twinFiled.Msg);
            Assert.AreEqual(ShiftPlayKit.Fingerprint(twin), log.FingerprintAtDayChange, "devir ikizle aynı");
            var mark = ShiftPlayKit.Mark();
            var again = ShiftPlayKit.Game.FileBoardShift(id);
            Assert.IsFalse(again.Ok, "aynı fiş ikinci kez dosyalanmaz");
            ShiftPlayKit.AssertUnchanged(mark, "aynı fişi yeniden dosyalamak");
        }

        // ================================================================== P12

        /// <summary>
        /// P12 (Karar D5, PLAN §1.10): K6'da Esc ve sürüklemesiz sağ tık sırası, gerçek sanal klavye ve fareyle (eldeyken tuşları
        /// <c>SiginakShiftClose.Update</c> okur; masa kilitli, kamera askıda): kapak açıkken Esc kapağı kapatır (K6'da kalınır), sonra
        /// K1; basılı fiş eldeyken Esc ve sağ tık fişi dile döndürür (kamera K6'da, geri tık kameraya ulaşmaz), fiş dildeyken Esc ve
        /// sağ tık K1'e döndürür. K1'den fişe tık K6'ya götürüp ele alır. Hiçbiri komut yazmaz.
        /// </summary>
        [UnityTest]
        public IEnumerator Hand_EscAndRightClick_ReturnSlip_ThenK1()
        {
            yield return ShiftPlayKit.Begin();
            var sw = ShiftPlayKit.Switch;
            var rig = ShiftPlayKit.Rig;
            var paper = ShiftPlayKit.Paper;
            var start = ShiftPlayKit.Mark();
            var log = ShiftPlayKit.Log();

            // A) basılmadan: kapak açık → Esc kapatır, K6'da kalınır → Esc K1
            ShiftPlayKit.ClickSwitch();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.ShiftClose);
            Assert.AreEqual(ShiftPhase.CoverUp, sw.Phase, "kapak kalkık");
            yield return BoardPlayKit.PressKey(Key.Escape);
            Assert.AreEqual(1, log.Count("Cancelled"), "K6'da Esc önce açık kapağı kapatır");
            Assert.AreEqual(SiginakPose.ShiftClose, rig.Current, "kapak kapanınca kamera K6'da kalır");
            yield return BoardPlayKit.WaitFor(() => !sw.Moving, 1, "kapak kapanmadı");
            yield return BoardPlayKit.PressKey(Key.Escape);
            Assert.AreEqual(SiginakPose.Desk, rig.Current, "kapak kapalıyken K6'da Esc masaya (K1) döner");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            ShiftPlayKit.AssertUnchanged(start, "kapak ve Esc");

            // B) basılı fiş elde: Esc → dile (K6'da) → Esc → K1
            yield return ShiftPlayKit.PrintToTongue();
            var printed = ShiftPlayKit.Mark();
            yield return ShiftPlayKit.TakeToHand();
            int poses = log.Count("Pose");
            yield return BoardPlayKit.PressKey(Key.Escape);
            Assert.That(paper.State == Where.Returning || paper.State == Where.Tongue, "eldeyken Esc fişi dile döndürür (" + paper.State + ")");
            Assert.AreEqual(SiginakPose.ShiftClose, rig.Current, "fiş dile dönerken kamera K6'da");
            Assert.AreEqual(poses, log.Count("Pose"), "eldeki Esc kamerayı oynatmaz");
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Tongue, 1, "fiş dile dönmedi");
            yield return BoardPlayKit.Frames(2);
            yield return BoardPlayKit.PressKey(Key.Escape);
            Assert.AreEqual(SiginakPose.Desk, rig.Current, "fiş dildeyken Esc K1'e döner");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            Assert.AreEqual(Where.Tongue, paper.State, "fiş dilde kalır");

            // C) K1'den fişe tık: K6 + el; sağ tık → dile; sağ tık → K1
            ShiftPlayKit.ClickSlip();
            Assert.AreEqual(SiginakPose.ShiftClose, rig.Current, "K1'den fişe tık K6'ya götürür");
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Hand, 1, "fiş ele gelmedi");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.ShiftClose);
            Assert.IsTrue(ShiftPlayKit.SlipInFrame(), "K1'den alınan fiş K6'da elde, kadrajda");
            int backs = log.Count("Back");
            yield return BoardPlayKit.RightDrag(BoardPlayKit.ScreenCentre, new Vector2(3, 0), 1);
            Assert.AreEqual(backs, log.Count("Back"), "fiş eldeyken sağ tık kameraya ulaşmaz (rig.InputSuspended)");
            Assert.That(paper.State == Where.Returning || paper.State == Where.Tongue, "eldeyken sürüklemesiz sağ tık fişi dile döndürür (" + paper.State + ")");
            Assert.AreEqual(SiginakPose.ShiftClose, rig.Current, "kamera K6'da");
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Tongue, 1, "fiş dile dönmedi");
            yield return BoardPlayKit.Frames(2);
            yield return BoardPlayKit.RightDrag(BoardPlayKit.ScreenCentre, new Vector2(3, 0), 1);
            Assert.AreEqual(backs + 1, log.Count("Back"), "fiş dildeyken sağ tık kameranın geri tıkıdır (BackClicked)");
            Assert.AreEqual(SiginakPose.ShiftClose.ToString(), log.Entries.Last(e => e.Name == "Back").Value, "geri tık K6'da");
            Assert.AreEqual(SiginakPose.Desk, rig.Current, "fiş dildeyken sağ tık K1'e döner");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);

            ShiftPlayKit.AssertUnchanged(printed, "Esc ve sağ tık");
            AssertWaiting("Esc ve sağ tıktan sonra");
            Assert.AreEqual(Where.Tongue, paper.State, "fiş dilde");
        }

        // ================================================================== P13

        /// <summary>
        /// P13 (Karar D7-A): dosyalanan son fiş tepside üst kâğıt; fiş basılı değilken EVRAK'a tık onu elde salt okunur açar (K6'ya,
        /// tepsideki kopya gizli), sürüklenmez ve dosyalanmaz, tık tepsiye döndürür (kopya geri gelir, kamera K1'e); komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator EvrakClick_ReadsLastFiledSlip_ReadOnly()
        {
            yield return ShiftPlayKit.Begin();
            yield return ShiftPlayKit.PrintToTongue();
            string id = ShiftPlayKit.Sim.Board.ReceiptId;
            yield return ShiftPlayKit.FileOnEvrak();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            var tray = ShiftPlayKit.Tray;
            var paper = ShiftPlayKit.Paper;
            var filed = ShiftPlayKit.FiledSlip;
            Assert.IsNotNull(tray.LastFiled, "dosyalanan fiş tepside");
            Assert.AreEqual(id, tray.LastFiled.receiptId);
            Assert.IsTrue(filed != null && filed.gameObject.activeSelf, "tepside dosyalanmış fişin kopyası görünür");
            var target = ShiftPlayKit.TrayTarget;
            Assert.IsNotNull(target, "EVRAK'ın DeskTarget'ı (SiginakDesk.trayTarget)");
            var start = ShiftPlayKit.Mark();
            var log = ShiftPlayKit.Log();

            BoardPlayKit.Click(target.gameObject);
            Assert.IsTrue(paper.ReadOnly, "EVRAK'a tık son fişi salt okunur açar");
            Assert.AreEqual(Where.Reading, paper.State, "arşiv fişi ele geliyor");
            Assert.AreEqual(id, paper.Facts.receiptId, "okunan fiş dosyalanan fiş");
            Assert.IsTrue(tray.FiledHidden, "okunurken tepsideki kopya gizli");
            Assert.IsFalse(filed.gameObject.activeSelf, "kopyanın nesnesi kapalı");
            Assert.AreEqual(SiginakPose.ShiftClose, ShiftPlayKit.Rig.Current, "arşiv fişi K6'da okunur");
            Assert.AreEqual(1, log.Count("SlipSound", ShiftSound.SlipLift.ToString()), "ele alış ses olayı");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.ShiftClose);
            yield return ShiftPlayKit.Watch(null, 0.3f);
            Assert.IsTrue(ShiftPlayKit.SlipInFrame(), "arşiv fişi elde, kadrajda");
            Assert.That(ShiftPlayKit.SlipHeightFraction(), Is.InRange(0.67f * 0.98f, 0.67f * 1.02f), "arşiv fişi de ekranın %67'si");
            Assert.IsTrue(ShiftPlayKit.Close.Busy, "okurken masa kilitli");
            Assert.IsFalse(ShiftPlayKit.Machine.LampLit, "arşiv okuması fiş ağzı lambasını yakmaz");

            yield return ShiftPlayKit.DragSlip(new[] { ShiftPlayKit.TrayScreen() }, 6, 1);
            Assert.AreEqual(0, log.Count("Filed"), "arşiv fişi dosyalanmaz");
            Assert.IsFalse(ShiftPlayKit.Close.Filing, "dosyalama başlamadı");
            Assert.AreEqual(Where.Reading, paper.State, "arşiv fişi sürüklenmez");
            Assert.IsTrue(paper.ReadOnly);

            ShiftPlayKit.ClickSlip();
            Assert.AreEqual(Where.Returning, paper.State, "tık arşiv fişini tepsiye döndürür");
            yield return BoardPlayKit.WaitFor(() => paper.State == Where.Hidden, 1, "arşiv fişi tepsiye dönmedi (" + paper.State + ")");
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(paper.ReadOnly, "okuma bitti");
            Assert.IsFalse(tray.FiledHidden, "tepsideki kopya geri geldi");
            Assert.IsTrue(filed.gameObject.activeSelf, "kopyanın nesnesi açık");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            Assert.IsFalse(ShiftPlayKit.Close.Busy, "kapanış meşgul değil");
            ShiftPlayKit.AssertUnchanged(start, "arşiv fişini okumak");
            Assert.IsFalse(ShiftPlayKit.Sim.Board.Printed, "fiş basılmadı");
            Assert.AreEqual(1, ShiftPlayKit.Close.Archive.Count, "arşivde bir fiş");
        }
    }
}
