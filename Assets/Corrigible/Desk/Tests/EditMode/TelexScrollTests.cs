using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Her Flush'ta yenilenen VM, haber ekleme ve kısa listelerin kesintisiz teleks akışı.</summary>
    public sealed class TelexScrollTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject root;
        TelexStripView strip;
        RectTransform run, view;
        bool wasPreview;

        [SetUp]
        public void SetUp()
        {
            wasPreview = DeskUi.EditPreview;
            DeskUi.EditPreview = true;
            root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Screens/Telex.prefab"));
            strip = root.GetComponent<TelexStripView>();
            run = Read<RectTransform>(strip, "run");
            view = Read<RectTransform>(strip, "view");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            DeskUi.EditPreview = wasPreview;
        }

        static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        void Call(string method, params object[] args) => typeof(TelexStripView).GetMethod(method, Private).Invoke(strip, args);
        void Advance(double seconds) => Call("Advance", seconds);
        TelexItemView[] ActiveItems() => run.GetComponentsInChildren<TelexItemView>(true).Where(i => i.gameObject.activeSelf).ToArray();
        static TelexItemVM News(string id, string text = "KYGA RAPORU") => new TelexItemVM { Id = id, Day = "001", Source = "KYGA:", Text = text, Trust = "report" };
        static TelexVM Model(params TelexItemVM[] items) => new TelexVM { Label = "TELEKS", LampOn = true, Items = new List<TelexItemVM>(items) };

        float VisibleX(string id)
        {
            return ActiveItems().Where(i => Read<TelexItemVM>(i, "lastBound").Id == id)
                .Select(i => ((RectTransform)i.transform).anchoredPosition.x + run.anchoredPosition.x)
                .Where(x => x + ActiveItems().First(i => Read<TelexItemVM>(i, "lastBound").Id == id).Width > 0)
                .Min();
        }

        [Test]
        public void EquivalentFreshModel_PreservesPhaseAndMeasuredItems()
        {
            strip.Bind(Model(News("a"), News("b", "BOLGE RAPORU")));
            Advance(2.375);
            var before = run.anchoredPosition;
            var items = ActiveItems();
            var snapshot = Read<TelexItemVM>(items[0], "lastBound");
            for (int i = 0; i < 20; i++) strip.Bind(Model(News("a"), News("b", "BOLGE RAPORU")));
            Assert.AreEqual(before, run.anchoredPosition, "Yeni VM nesnesi teleksi başa sarmamalı.");
            CollectionAssert.AreEqual(items, ActiveItems(), "Aynı listede havuz yeniden üretilmemeli.");
            Assert.AreSame(snapshot, Read<TelexItemVM>(items[0], "lastBound"), "Aynı içerik yeniden ölçülüp bağlanmamalı.");
        }

        [Test]
        public void HeadlinePrependAndRemoval_KeepVisibleSurvivingHeadlineInPlace()
        {
            var a = News("a", "ILK RAPOR");
            var b = News("b", "UZUN BOLGE RAPORU");
            var c = News("c", "SON RAPOR");
            strip.Bind(Model(a, b, c));
            float firstWidth = ActiveItems()[0].Width;
            Advance((firstWidth + Read<float>(strip, "gap") + 25) / DeskTheme.Active.telexSpeed);
            float before = VisibleX("b");
            strip.Bind(Model(News("new", "YENI HABER"), a, b, c));
            Assert.AreEqual(before, VisibleX("b"), 0.002f, "Öne eklenen haber, okunan haberin konumunu bozmamalı.");
            strip.Bind(Model(News("new", "YENI HABER"), b, c));
            Assert.AreEqual(before, VisibleX("b"), 0.002f, "Önceki haber listeden düşerken görünür haber yerinde kalmalı.");
        }

        [Test]
        public void InPlaceTrustMutation_IsReboundWithoutMovingTheHeadline()
        {
            var item = News("a");
            var model = Model(item);
            strip.Bind(model);
            Advance(0.5);
            float before = VisibleX("a");
            item.Trust = "verified";
            item.StampRot = -3;
            strip.Bind(model);
            Assert.AreEqual(before, VisibleX("a"), 0.002f);
            foreach (var active in ActiveItems()) Assert.AreEqual("verified", Read<TelexItemVM>(active, "lastBound").Trust);
        }

        [Test]
        public void ShortSingleHeadline_FillsWideViewportAcrossEveryCycle()
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(4200, 26);
            strip.Bind(Model(News("a", "KISA")));
            Assert.Greater(Read<int>(strip, "copies"), 2, "Kısa içerikte yalnız iki kopya geniş ekranı doldurmaz.");
            float cycle = Read<float>(strip, "cycle");
            Advance((cycle - 0.4) / DeskTheme.Active.telexSpeed);
            var items = ActiveItems().OrderBy(i => ((RectTransform)i.transform).anchoredPosition.x).ToArray();
            float right = ((RectTransform)items.Last().transform).anchoredPosition.x + items.Last().Width + run.anchoredPosition.x;
            Assert.GreaterOrEqual(right, view.rect.width);
            float expectedGap = Read<float>(strip, "gap");
            for (int i = 1; i < items.Length; i++)
            {
                float distance = ((RectTransform)items[i].transform).anchoredPosition.x - ((RectTransform)items[i - 1].transform).anchoredPosition.x - items[i - 1].Width;
                Assert.AreEqual(expectedGap, distance, 0.002f, "Döngü sınırında fazladan boşluk oluşmamalı.");
            }
        }

        [Test]
        public void WrapAndLongFrame_PreserveSubpixelDistance()
        {
            strip.Bind(Model(News("a", "TEK HABER RAPORU")));
            double speed = DeskTheme.Active.telexSpeed;
            double cycle = Read<float>(strip, "cycle");
            Advance((cycle - 0.4) / speed);
            float before = VisibleX("a");
            Advance(0.2 / speed);
            Assert.AreEqual(before - 0.2f, VisibleX("a"), 0.002f, "Döngü sarılırken yalnız normal kare mesafesi ilerlemeli.");
            Advance((5 * cycle + 7.125) / speed);
            Assert.AreEqual(-7.225, Read<double>(strip, "offset"), 0.002, "Uzun kare birden fazla döngüyü atlayıp kesri korumalı.");
        }

        [Test]
        public void DifferentFrameCadences_TravelTheSameFractionalDistance()
        {
            strip.Bind(Model(News("a", "TEK HABER RAPORU")));
            Advance(1);
            double expected = Read<double>(strip, "offset");
            strip.Freeze(true);
            strip.Freeze(false);
            for (int i = 0; i < 120; i++) Advance(1.0 / 120);
            Assert.AreEqual(expected, Read<double>(strip, "offset"), 0.000001, "Akış mesafesi kare sayısına veya piksel yuvarlamasına bağlı olmamalı.");
        }

        [Test]
        public void ViewportResize_ChangesCoverageWithoutRestartingFlow()
        {
            strip.Bind(Model(News("a", "KISA")));
            Advance(0.333);
            var before = run.anchoredPosition;
            int oldCopies = Read<int>(strip, "copies");
            ((RectTransform)root.transform).sizeDelta = new Vector2(4200, 26);
            Call("EnsureCoverage");
            Assert.Greater(Read<int>(strip, "copies"), oldCopies);
            Assert.AreEqual(before, run.anchoredPosition);
            ((RectTransform)root.transform).sizeDelta = new Vector2(800, 26);
            Call("EnsureCoverage");
            Assert.Less(Read<int>(strip, "copies"), oldCopies);
            Assert.AreEqual(before, run.anchoredPosition);
        }

        [Test]
        public void HoverAndFreeze_PauseAndResume_EmptyNewsClearThePool()
        {
            strip.Bind(Model(News("a")));
            strip.Paused = true;
            var before = run.anchoredPosition;
            Advance(10);
            strip.Bind(Model(News("a")));
            Assert.AreEqual(before, run.anchoredPosition);
            strip.Paused = false;
            Advance(0.2);
            Assert.AreNotEqual(before, run.anchoredPosition);
            strip.Freeze(true);
            before = run.anchoredPosition;
            Advance(10);
            Assert.AreEqual(before, run.anchoredPosition);
            strip.Freeze(false);
            Advance(0.2);
            Assert.AreNotEqual(before, run.anchoredPosition);
            strip.Bind(Model());
            Assert.AreEqual(0, ActiveItems().Length);
            Assert.AreEqual(0, Read<float>(strip, "cycle"));
            strip.Bind(Model(News("b")));
            Assert.Greater(ActiveItems().Length, 0, "Boş listeden sonra haber yeniden akabilmeli.");
        }
    }
}
