using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>3D paftadaki çapa, farklı ölçekteki ekran kâğıdında aynı pikselin üzerinde kalır.</summary>
    public sealed class CommandPostProjectionTests
    {
        readonly List<GameObject> objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        GameObject Create(string name, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            objects.Add(go);
            return go;
        }

        RectTransform Child(string name, RectTransform parent, Vector2 size)
        {
            var rect = (RectTransform)Create(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        void Setup(out Camera camera, out RectTransform map, out RectTransform anchor, out RectTransform context)
        {
            camera = Create("Perspektif kamera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 53;
            camera.aspect = 16f / 9f;
            camera.pixelRect = new Rect(0, 0, 1280, 720);
            camera.nearClipPlane = .01f;

            var canvas = Create("Dünya paftası", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            map = (RectTransform)canvas.transform;
            map.sizeDelta = new Vector2(1920, 1080);
            map.position = new Vector3(.8f, .7f, 1.2f);
            map.rotation = Quaternion.Euler(90, 16, -5);
            map.localScale = Vector3.one * .0025f;
            camera.transform.position = map.position + new Vector3(1.7f, 3f, -3.6f);
            camera.transform.LookAt(map.position);

            // Yazı katmanının kendi kamerası boş olabilir; izdüşüm kök canvas'ın kamerasını kullanmalıdır.
            var nested = Child("Pafta yazı katmanı", map, map.sizeDelta);
            nested.gameObject.AddComponent<Canvas>();
            anchor = Child("Bölge çapası", nested, new Vector2(32, 32));

            var overlay = Create("Ekran kâğıtları", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            context = Child("Ölçekli kart ebeveyni", (RectTransform)overlay.transform, new Vector2(900, 600));
            context.pivot = new Vector2(.23f, .71f);
            context.anchoredPosition = new Vector2(67, -43);
            context.localScale = new Vector3(.75f, .85f, 1);
        }

        static Vector2 ScreenXY(Vector3 value) => new Vector2(value.x, value.y);

        static Vector2 AssertProjectedAtCameraPixel(Camera camera, RectTransform anchor, RectTransform context)
        {
            var world = anchor.position;
            // Beklenen piksel, canvas yardımcısından bağımsız olarak gerçek perspektif kameradan hesaplanır.
            var expected = camera.WorldToScreenPoint(world);
            Assert.That(expected.z, Is.GreaterThan(0), "çapa kamera önünde olmalı");
            var projected = DeskCanvasProjection.WorldPoint(anchor, world, context);
            Assert.That(Vector2.Distance(ScreenXY(expected), ScreenXY(projected)), Is.LessThan(.01f));

            var local = DeskCanvasProjection.LocalPoint(anchor, world, context);
            var reconstructed = context.TransformPoint(local);
            Assert.That(Vector2.Distance(ScreenXY(expected), ScreenXY(reconstructed)), Is.LessThan(.01f),
                "ebeveynin pivotu ve ölçeği çapanın ekran konumunu değiştirmemeli");
            return local;
        }

        [TestCase(-160f, 90f)]
        [TestCase(280f, -130f)]
        public void RotatedWorldMap_ProjectsAnchorIntoOverlayParent(float x, float y)
        {
            Setup(out var camera, out _, out var anchor, out var context);
            anchor.anchoredPosition = new Vector2(x, y);
            Assert.IsTrue(DeskCanvasProjection.RequiresProjection(anchor, context));
            Assert.IsTrue(DeskCanvasProjection.IsWorldSpace(anchor));
            AssertProjectedAtCameraPixel(camera, anchor, context);
        }

        [Test]
        public void CameraMovement_ReprojectsUnchangedWorldAnchor()
        {
            Setup(out var camera, out var map, out var anchor, out var context);
            anchor.anchoredPosition = new Vector2(280, 130);
            var unchangedWorld = anchor.position;
            var before = AssertProjectedAtCameraPixel(camera, anchor, context);

            camera.transform.position += new Vector3(.8f, .2f, .35f);
            camera.transform.LookAt(map.position + new Vector3(.15f, 0, 0));
            var after = AssertProjectedAtCameraPixel(camera, anchor, context);

            Assert.AreEqual(unchangedWorld, anchor.position, "yalnız kamera hareket etti");
            Assert.That(Vector2.Distance(before, after), Is.GreaterThan(5),
                "aynı dünya noktası için eski ekran konumu önbellekten dönmemeli");
        }

        [Test]
        public void OverlayToOverlay_KeepsExistingWorldCoordinates()
        {
            var from = Create("Eski harita", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            var to = Create("Eski kart katmanı", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            from.renderMode = to.renderMode = RenderMode.ScreenSpaceOverlay;
            var anchor = Child("Bölge", (RectTransform)from.transform, new Vector2(32, 32));
            anchor.anchoredPosition = new Vector2(173, -94);
            var context = Child("Kart ebeveyni", (RectTransform)to.transform, new Vector2(900, 600));
            context.anchoredPosition = new Vector2(43, -27);
            context.localScale = new Vector3(.8f, .8f, 1);
            var world = anchor.TransformPoint(new Vector3(13, -8));

            Assert.IsFalse(DeskCanvasProjection.RequiresProjection(anchor, context));
            Assert.AreEqual(world, DeskCanvasProjection.WorldPoint(anchor, world, context));
            Assert.AreEqual((Vector2)context.InverseTransformPoint(world),
                DeskCanvasProjection.LocalPoint(anchor, world, context));
        }
    }
}
