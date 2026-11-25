using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Where = Corrigible.Desk.ShiftReceiptPaper.Where;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tek kâğıt akışı (teleks ve devir kâğıdı birleşti; <see cref="Teleprinter"/>), DeskGame gerçek sahnesiyle Play'de (tohum 79,
    /// <see cref="ShiftPlayKit.Begin"/>): yeni teleks satırında o anki kâğıt düşer, yenisi silindirin içinden satır satır yazılarak
    /// çıkar ve dinlenme pozuna oturur; aynı karedeki iki satır tek kopya düşürür; fiş silindirdeyken gelen satır kâğıda basılmaz,
    /// şeride düşer, fiş yerinde kalır. Teleks satırları yalnız sunumdur (komut yazmaz, RNG çekmez).
    /// </summary>
    public sealed class TelexSheetPlayTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return ShiftPlayKit.End();
        }

        static float Tol => BoardPlayKit.FrameTolerance;

        static IEnumerator Settle()
        {
            yield return BoardPlayKit.WaitFor(() => ShiftPlayKit.TelexSettled && !ShiftPlayKit.TornActive, 6, "teleks satırı bitmedi");
        }

        /// <summary>
        /// Yeni satır: eski kâğıdın kopyası düşer (<c>tearSeconds</c>), yeni kâğıt aynı karede ≥ 0,14 m geri alınmış (ilk satırı yazım
        /// çizgisinde) başlar; basım boyunca geri alma artmaz, rulo döner; basım satır süresinde biter, son kayış
        /// <c>motion.telexFeed.slideS</c> + iki karede dinlenmede (geri alma 0) biter; kâğıt ve hedefi açık.
        /// </summary>
        [UnityTest]
        public IEnumerator TelexLine_DropsSheet_NewSheetTypesUpFromPlaten()
        {
            yield return ShiftPlayKit.Begin();
            yield return Settle();
            var printer = ShiftPlayKit.Printer;
            Assert.IsTrue(printer.SheetPresent, "açılıştan sonra teleks kâğıdı silindirde");
            Assert.AreEqual(0f, printer.SheetRetractM, 1e-6f, "dinlenme pozunda");
            float lineS = 1.0f;
            float slideS = ShiftPlayKit.Token("motion.telexFeed.slideS", 0.06f);
            float tearS = ShiftPlayKit.Get<float>(printer, "tearSeconds");
            var mark = ShiftPlayKit.Mark();
            float roll0 = printer.RollDegrees;

            ShiftPlayKit.Telex.Post(BoardPlayKit.Surface.Shift, "DENEME", "Yeni satır alttan yazılarak çıkar; eski kâğıt arkaya düşer.", TelexKind.Info, typeSeconds: lineS);
            float posted = Time.unscaledTime;
            Assert.IsTrue(printer.Dropping, "eski kâğıt düşüyor");
            Assert.GreaterOrEqual(printer.SheetRetractM, 0.14f, "yeni kâğıt silindirin içinden başlar (ilk satırı yazım çizgisinde)");
            Assert.IsTrue(printer.Typing, "satır basılıyor");
            Assert.IsTrue(printer.SheetPresent);

            var samples = new List<(float at, float r, bool typing, bool dropping)>();
            while ((!printer.Settled || printer.Dropping) && Time.unscaledTime - posted < 4)
            {
                yield return null;
                samples.Add((Time.unscaledTime - posted, printer.SheetRetractM, printer.Typing, printer.Dropping));
            }
            Assert.IsTrue(printer.Settled, "kâğıt dinlenmeye oturmadı");
            for (int i = 1; i < samples.Count; i++)
                Assert.LessOrEqual(samples[i].r, samples[i - 1].r + 1e-5f, "geri alma artmaz (" + samples[i].at.ToString("0.000") + " s)");
            Assert.Greater(samples.Select(s => s.r).Distinct().Count(), 3, "kâğıt satır satır yükselir");
            Assert.AreNotEqual(roll0, printer.RollDegrees, "rulo döner");
            var typedEnd = samples.First(s => !s.typing);
            Assert.That(typedEnd.at, Is.InRange(lineS - Tol, lineS + Tol), "basım satır süresinde biter (" + typedEnd.at.ToString("0.000") + ")");
            var settled = samples.First(s => s.r <= 1e-6f && !s.typing);
            Assert.LessOrEqual(settled.at - typedEnd.at, slideS + 2 * BoardPlayKit.MaxFrameDt + 1e-3f, "son kayış slideS içinde");
            var dropEnd = samples.First(s => !s.dropping);
            Assert.That(dropEnd.at, Is.InRange(tearS - Tol, tearS + Tol), "düşüş " + tearS + " s");
            Assert.AreEqual(0f, printer.SheetRetractM, 1e-6f);
            Assert.IsTrue(ShiftPlayKit.PaperTargetEnabled, "kâğıdın hedefi açık");
            StringAssert.Contains("DENEME", ShiftPlayKit.Get<TMP_Text>(printer, "frontStatus").text, "yeni satır kâğıtta");
            ShiftPlayKit.AssertUnchanged(mark, "teleks satırı (yalnız sunum)");
        }

        /// <summary>
        /// Aynı karede iki satır (ör. harita eylemi): yalnız bir kopya düşer ve eski kâğıdın satırını taşır; ikinci satır geldiğinde
        /// yeni kâğıdın hiç harfi basılmamıştır, boş kâğıt düşmez; kâğıtta ikinci satır basılır.
        /// </summary>
        [UnityTest]
        public IEnumerator TwoLinesSameFrame_OneDrop()
        {
            yield return ShiftPlayKit.Begin();
            yield return Settle();
            var printer = ShiftPlayKit.Printer;
            string old = ShiftPlayKit.LastTelex.status;
            int shift = BoardPlayKit.Surface.Shift;
            ShiftPlayKit.Telex.Post(shift, "BİRİNCİ", "İlk satır.", TelexKind.Info);
            var torn = ShiftPlayKit.Get<TMP_Text>(printer, "tornStatus");
            Assert.IsTrue(printer.Dropping);
            StringAssert.Contains(old, torn.text, "kopya eski kâğıdın satırını taşır");
            ShiftPlayKit.Telex.Post(shift, "İKİNCİ", "İkinci satır.", TelexKind.Info);
            Assert.IsTrue(printer.Dropping, "tek kopya düşmeye devam eder");
            StringAssert.Contains(old, torn.text, "ikinci satırda boş kâğıt düşmez (kopya değişmez)");
            StringAssert.DoesNotContain("BİRİNCİ", torn.text);
            yield return Settle();
            StringAssert.Contains("İKİNCİ", ShiftPlayKit.Get<TMP_Text>(printer, "frontStatus").text, "kâğıtta ikinci satır");
            StringAssert.Contains("BİRİNCİ", ShiftPlayKit.Get<TMP_Text>(printer, "frontPrev").text, "önceki satır yuvasında birinci satır");
        }

        /// <summary>
        /// Fiş silindirdeyken gelen satır: kâğıda basılmaz (basım yok, kâğıt yok, düşüş yok), ekran şeridine düşer ve kayda girer; fiş
        /// dil pozunda kıpırdamadan kalır.
        /// </summary>
        [UnityTest]
        public IEnumerator TelexLine_WhileSlipInPlaten_GoesToStrip_SlipStays()
        {
            yield return ShiftPlayKit.Begin();
            yield return ShiftPlayKit.PrintToTongue();
            var printer = ShiftPlayKit.Printer;
            var paper = ShiftPlayKit.Paper;
            yield return BoardPlayKit.WaitFor(() => !printer.Dropping, 2, "besleme başındaki düşüş bitmedi");
            Assert.AreEqual(Where.Tongue, paper.State);
            Assert.IsFalse(printer.SheetPresent, "fiş silindirde: teleks kâğıdı yok");
            var pos = paper.transform.position;
            int count = ShiftPlayKit.Telex.Lines.Count;
            var line = ShiftPlayKit.Telex.Post(BoardPlayKit.Surface.Shift, "YAPILAMADI", "Fiş silindirde; satır şeritte.", TelexKind.Failed);
            Assert.AreEqual(count + 1, ShiftPlayKit.Telex.Lines.Count, "satır kayda girer");
            Assert.IsTrue(ShiftPlayKit.StripQueue.Any(l => l.code == line.code), "satır ekran şeridinde");
            Assert.IsFalse(printer.Typing, "kâğıda basılmaz (fişin beslemesi beklemez)");
            Assert.IsFalse(printer.SheetPresent);
            Assert.IsFalse(printer.Dropping, "fiş düşmez");
            yield return BoardPlayKit.Frames(5);
            Assert.AreEqual(Where.Tongue, paper.State, "fiş dilde");
            Assert.Less(Vector3.Distance(pos, paper.transform.position), 1e-5f, "fiş kıpırdamaz");
            Assert.IsFalse(printer.SheetPresent, "teleks kâğıdı gelmez");
            Assert.IsFalse(ShiftPlayKit.PaperTargetEnabled);
        }
    }
}
