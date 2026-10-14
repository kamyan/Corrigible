using System.Reflection;
using Corrigible.Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>İpucu, farklı çapa/pivot/ölçekteki ebeveynde de kaynağın yanında ve görünür alanın içinde kalır.</summary>
    public class TooltipPlacementTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            if (parent != null) rt.SetParent(parent, false);
            rt.sizeDelta = size;
            return rt;
        }

        static MonoBehaviour Setup(bool desk, Vector2 size, out RectTransform root, out RectTransform panel)
        {
            root = Rect("Alan", null, new Vector2(1000, 600));
            root.gameObject.SetActive(false);
            root.pivot = new Vector2(0, 1);
            root.position = new Vector3(120, 650, 0);
            var parent = Rect("İç katman", root, new Vector2(900, 700));
            parent.anchorMin = parent.anchorMax = new Vector2(0.25f, 0.75f);
            parent.pivot = new Vector2(0.15f, 0.8f);
            parent.anchoredPosition = new Vector2(57, -43);
            parent.localScale = new Vector3(0.8f, 0.8f, 1);
            panel = Rect("İpucu", parent, size);
            panel.anchorMin = panel.anchorMax = new Vector2(1, 0);
            panel.pivot = new Vector2(0.8f, 0.2f);
            MonoBehaviour view = desk ? root.gameObject.AddComponent<DeskTooltip>() : root.gameObject.AddComponent<TooltipView>();
            if (desk)
            {
                Set(view, "canvasRoot", root);
                Set(view, "active", panel);
                Set(view, "activeScale", Vector3.one);
            }
            else
            {
                Set(view, "panel", panel);
                Set(view, "panelScale", Vector3.one);
            }
            return view;
        }

        static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        static void Place(object view, Vector2 point) => view.GetType().GetMethod("Place", Private).Invoke(view, new object[] { point });

        static void AssertInside(RectTransform panel, RectTransform root)
        {
            var corners = new Vector3[4];
            panel.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                Vector2 local = root.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(root.rect.xMin - 0.01f, root.rect.xMax + 0.01f), "yatayda görünür alanı aşmamalı");
                Assert.That(local.y, Is.InRange(root.rect.yMin - 0.01f, root.rect.yMax + 0.01f), "dikeyde görünür alanı aşmamalı");
            }
        }

        [TestCase(true, 0.01f, 0.01f)]
        [TestCase(true, 0.99f, 0.01f)]
        [TestCase(true, 0.01f, 0.99f)]
        [TestCase(true, 0.99f, 0.99f)]
        [TestCase(false, 0.01f, 0.01f)]
        [TestCase(false, 0.99f, 0.01f)]
        [TestCase(false, 0.01f, 0.99f)]
        [TestCase(false, 0.99f, 0.99f)]
        public void Corners_UseActualParentCoordinates(bool desk, float x, float y)
        {
            var view = Setup(desk, new Vector2(420, 260), out var root, out var panel);
            try
            {
                Vector2 pointer = root.TransformPoint(new Vector2(Mathf.Lerp(root.rect.xMin, root.rect.xMax, x), Mathf.Lerp(root.rect.yMin, root.rect.yMax, y)));
                Place(view, pointer);
                AssertInside(panel, root);
                Assert.AreEqual(Vector3.one, panel.localScale, "normal fişin yazıları küçülmemeli");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OversizedContent_FitsAndRecoversAfterResize(bool desk)
        {
            var view = Setup(desk, new Vector2(1600, 1000), out var root, out var panel);
            try
            {
                Place(view, root.position);
                AssertInside(panel, root);
                Assert.Less(panel.localScale.x, 1, "ters Clamp sınırları yerine uzun fiş görünür alana sığmalı");
                root.sizeDelta = new Vector2(2000, 1600);
                Place(view, root.position);
                AssertInside(panel, root);
                Assert.AreEqual(Vector3.one, panel.localScale, "önceki küçültme kalıcı olmamalı");
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }

        [Test]
        public void GaugeBreakdown_StaysBelowSourceWhilePointerMoves()
        {
            var view = Setup(true, new Vector2(420, 260), out var root, out var panel);
            try
            {
                var source = Rect("Gösterge", root, new Vector2(180, 90));
                source.anchorMin = source.anchorMax = source.pivot = new Vector2(0.5f, 1);
                source.anchoredPosition = new Vector2(0, -20);
                Set(view, "source", source);
                Place(view, root.TransformPoint(new Vector2(450, -30)));
                var before = panel.position;
                Place(view, root.TransformPoint(new Vector2(550, -100)));
                Assert.AreEqual(before, panel.position, "gösterge içindeki fare hareketi dökümü sağa sola oynatmamalı");
                var corners = new Vector3[4];
                source.GetWorldCorners(corners);
                Assert.Less(panel.position.y, corners[0].y, "fiş göstergenin alt kenarının altında açılmalı");
                AssertInside(panel, root);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
    }
}
