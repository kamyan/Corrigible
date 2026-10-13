using Corrigible.Desk;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>F1 temel kuralları: biçimlendirme, Türkçe harf, sayaç/kama açıları, projeksiyon, rampa, TSV dönüşümü.</summary>
    public class DeskFoundationTests
    {
        const string M = "−";

        [Test]
        public void Number_TrEn()
        {
            Assert.AreEqual("47,3", DeskFormat.Number(47.3, 1, DeskLang.Tr));
            Assert.AreEqual("47.3", DeskFormat.Number(47.3, 1, DeskLang.En));
            Assert.AreEqual(M + "6,4", DeskFormat.Number(-6.4, 1, DeskLang.Tr));
            Assert.AreEqual(M + "6.4", DeskFormat.Number(-6.4, 1, DeskLang.En));
            Assert.AreEqual("0,0", DeskFormat.Number(-0.01, 1, DeskLang.Tr), "−0,0 yazılmaz");
            Assert.AreEqual("1000,5", DeskFormat.Number(1000.5, 1, DeskLang.Tr), "binlik ayırıcı yok");
        }

        [Test]
        public void Signed_And_Percent()
        {
            Assert.AreEqual("+0,7", DeskFormat.Signed(0.7, 1, DeskLang.Tr));
            Assert.AreEqual(M + "1,2", DeskFormat.Signed(-1.2, 1, DeskLang.Tr));
            Assert.AreEqual("0,0", DeskFormat.Signed(0, 1, DeskLang.Tr));
            Assert.AreEqual("%37,2", DeskFormat.Percent(37.2, 1, DeskLang.Tr));
            Assert.AreEqual("37.2%", DeskFormat.Percent(37.2, 1, DeskLang.En));
            Assert.AreEqual("%38", DeskFormat.Percent(38, 0, DeskLang.Tr));
            Assert.AreEqual(M + "%6", DeskFormat.Percent(-6, 0, DeskLang.Tr));
            Assert.AreEqual("≈41,8", DeskFormat.Approximate(DeskFormat.Number(41.8, 1, DeskLang.Tr)));
        }

        [Test]
        public void DayCode_TrEn()
        {
            Assert.AreEqual("G-074", DeskFormat.DayCode(74, DeskLang.Tr));
            Assert.AreEqual("D-074", DeskFormat.DayCode(74, DeskLang.En));
            Assert.AreEqual("G-118", DeskFormat.DayCode(118, DeskLang.Tr));
        }

        [Test]
        public void Format_IndependentOfThreadCulture()
        {
            var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                Assert.AreEqual("47.3", DeskFormat.Number(47.3, 1, DeskLang.En));
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                Assert.AreEqual("47,3", DeskFormat.Number(47.3, 1, DeskLang.Tr));
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = prev; }
        }

        [Test]
        public void TurkishCase()
        {
            Assert.AreEqual("İZLEME AĞI", DeskText.Upper("izleme ağı", DeskLang.Tr));
            Assert.AreEqual("ILIK", DeskText.Upper("ılık", DeskLang.Tr));
            Assert.AreEqual("ILLUSION", DeskText.Upper("illusion", DeskLang.En));
            Assert.AreEqual("ılık", DeskText.Lower("ILIK", DeskLang.Tr));
            var cap = DeskText.Capitalize("şebekeyi kapat", DeskLang.Tr);
            Assert.AreEqual("Şebekeyi kapat", cap);
            Assert.IsFalse(cap.Contains("̇"), "birleşik nokta (U+0307) üretilmemeli");
            Assert.AreEqual("İzleme", DeskText.Capitalize("izleme", DeskLang.Tr));
            Assert.AreEqual("TITLE", "title".ToUpperInvariant(), "kod tanımlayıcıları değişmez kültürle");
        }

        [Test]
        public void NeedleAndWedge()
        {
            Assert.AreEqual(-60f, DeskMath.NeedleDegrees(0), 1e-4);
            Assert.AreEqual(60f, DeskMath.NeedleDegrees(100), 1e-4);
            Assert.AreEqual(0f, DeskMath.NeedleDegrees(50), 1e-4);
            // Mock: fon 47,3 → −3,24°; bağımlılık 58,4 → 10,08°
            Assert.AreEqual(-3.24f, DeskMath.NeedleDegrees(47.3f), 1e-3);
            Assert.AreEqual(10.08f, DeskMath.NeedleDegrees(58.4f), 1e-3);
            Assert.AreEqual(3.24f, DeskMath.NeedleZ(47.3f), 1e-3);
            var (fill, z) = DeskMath.Wedge(41.8f, 3);
            Assert.AreEqual(7.2f / 360f, fill, 1e-5);
            Assert.AreEqual(-(DeskMath.NeedleDegrees(41.8f) - 3.6f), z, 1e-4);
            var (zf, zz) = DeskMath.Zone(70, 100);
            Assert.AreEqual(36f / 360f, zf, 1e-5);
            Assert.AreEqual(-24f, zz, 1e-4);
        }

        [Test]
        public void MapProjection()
        {
            // 0° boylam x = 865,5; ekvator y = 622
            Assert.AreEqual(865.5f, DeskMath.Project(0, 0).x, 1e-3);
            Assert.AreEqual(622f, DeskMath.Project(0, 0).y, 1e-3);
            var p = DeskMath.Project(120, 30);
            Assert.AreEqual(960 + 93 * 3.5f, p.x, 1e-3);
            Assert.AreEqual(622 - 133.5f, p.y, 1e-3);
        }

        [Test]
        public void Ramp()
        {
            Assert.AreEqual(0, DeskMath.RampIndex(0));
            Assert.AreEqual(0, DeskMath.RampIndex(19.9));
            Assert.AreEqual(1, DeskMath.RampIndex(20));
            Assert.AreEqual(2, DeskMath.RampIndex(46));
            Assert.AreEqual(4, DeskMath.RampIndex(100));
            var theme = DeskTheme.Active;
            Assert.IsNotNull(theme, "Resources/DeskTheme yok");
            Assert.AreEqual("B4442E", ColorUtility.ToHtmlStringRGB(theme.Ramp(46).fill));
            Assert.AreEqual("FFF8EE", ColorUtility.ToHtmlStringRGB(theme.Ramp(46).text), "40+ krem yazı");
            Assert.AreEqual("1E1B16", ColorUtility.ToHtmlStringRGB(theme.Ramp(12).text));
            Assert.AreEqual("5C100A", ColorUtility.ToHtmlStringRGB(theme.Ramp(80).fill));
        }

        [Test]
        public void TsvToRect()
        {
            // Döndürülmüş damga: sınırlayıcı kutu 100×30 @ (10,20), asıl boyut 90×22
            var tl = DeskMath.UnrotatedTopLeft(new Rect(10, 20, 100, 30), new Vector2(90, 22));
            Assert.AreEqual(new Vector2(15, 24), tl);
            Assert.AreEqual(new Vector2(12, -4), DeskMath.AnchoredFromScreen(new Vector2(28, 208), new Vector2(16, 204)));
            Assert.AreEqual(4f, DeskMath.CssRotationToZ(-4));
        }

        [Test]
        public void LineSpacing()
        {
            Assert.AreEqual(13.8f, DeskTheme.LineSpacing(DeskFont.LfRegular, 1.35f), 1e-3);
            Assert.AreEqual(-12.3f, DeskTheme.LineSpacing(DeskFont.CpBold, 1.0f), 1e-3);
            Assert.AreEqual(-44.4f, DeskTheme.LineSpacing(DeskFont.KalamRegular, 1.15f), 1e-3);
        }

        [Test]
        public void StampAngle_IsStableAndBounded()
        {
            var theme = DeskTheme.Active;
            float a = theme.StampAngle("ONAYLANDI-fon"), b = theme.StampAngle("ONAYLANDI-fon");
            Assert.AreEqual(a, b);
            for (int i = 0; i < 50; i++)
            {
                float x = theme.StampAngle("id" + i);
                Assert.That(x, Is.InRange(-5f, 5f));
            }
            Assert.AreEqual(-4f, theme.StampAngle("x", -4f), "VM açısı önceliklidir");
        }

        [Test]
        public void MarkPreview_CopiesAreUnsavedAndLocked()
        {
            // Edit mod önizlemesinin kopyaları sahneye kaydedilmez ve elle düzenlenemez (Play'de veriden yeniden kurulur)
            var go = new GameObject("Kopya");
            new GameObject("Çocuk").transform.SetParent(go.transform);
            try
            {
                DeskUi.MarkPreview(go);
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    Assert.IsTrue((t.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0, t.name);
                    Assert.IsTrue((t.gameObject.hideFlags & HideFlags.NotEditable) != 0, t.name);
                    Assert.IsTrue((t.hideFlags & HideFlags.NotEditable) != 0, t.name + ": taşınamaz");
                }
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
