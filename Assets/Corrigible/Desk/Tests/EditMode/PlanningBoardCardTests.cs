using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk.Tests
{
    /// <summary>Hurdaya bırakılırken gizlenen depo kartı sonraki eşya için sağlam biçimde yeniden kullanılmalı.</summary>
    public sealed class PlanningBoardCardTests
    {
        [Test]
        public void AcceptedReserveDrop_SettlesFromReleasePoint_InsteadOfRestoringPreviousJob()
        {
            var root = new GameObject("Boşta bırakma testi", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var plane = (RectTransform)root.transform;
                var view = root.AddComponent<PlanningBoardView>();
                var motion = root.AddComponent<PlanningBoardMotion>();
                var zoneObject = new GameObject("Boşta", typeof(RectTransform), typeof(PlanningBoardDropZone));
                zoneObject.transform.SetParent(plane, false);
                var zone = zoneObject.GetComponent<PlanningBoardDropZone>();
                zone.Configure(PlanningDropKind.Job, "rest", null, null, (RectTransform)zoneObject.transform);
                typeof(PlanningBoardView).GetField("dragPlane", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, plane);
                typeof(PlanningBoardView).GetField("zones", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, new[] { zone });
                typeof(PlanningBoardMotion).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(motion, view);
                var obj = new GameObject("Kişi", typeof(RectTransform), typeof(CanvasGroup), typeof(PlanningBoardCard));
                obj.transform.SetParent(plane, false);
                var card = obj.GetComponent<PlanningBoardCard>();
                card.Configure(PlanningCardKind.Person, null, null, null, null, null, plane);
                typeof(PlanningBoardCard).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, "sakin-1");
                var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
                ((RectTransform)obj.transform).anchoredPosition = new Vector2(-600, 200);
                card.OnBeginDrag(pointer);
                var release = new Vector2(135.75f, 210.25f);
                ((RectTransform)obj.transform).anchoredPosition = release;
                motion.AcceptReserveDrop(card);
                card.OnEndDrag(pointer);
                Assert.AreEqual(release, ((RectTransform)obj.transform).anchoredPosition, "Bırakış eski göreve dönmemeli.");
                motion.BeginReserve(true); motion.PlaceReserve(card, 0, 1);
                Assert.AreEqual(release, ((RectTransform)obj.transform).anchoredPosition, "Desteye ilk katılım bırakış noktasını ezmemeli.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void FreeSurfaceClick_OnlySignalsAnIdleLeftClick()
        {
            var root = new GameObject("Pano tıklama testi", typeof(RectTransform), typeof(PlanningBoardSurface));
            try
            {
                var surface = root.GetComponent<PlanningBoardSurface>(); int clicks = 0;
                surface.Clicked += () => clicks++;
                var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
                surface.OnPointerClick(pointer); Assert.AreEqual(1, clicks);
                pointer.dragging = true; surface.OnPointerClick(pointer);
                pointer.dragging = false; pointer.button = PointerEventData.InputButton.Right; surface.OnPointerClick(pointer);
                pointer.button = PointerEventData.InputButton.Left; surface.Interactable = false; surface.OnPointerClick(pointer);
                Assert.AreEqual(1, clicks, "Sürükleme, sağ tık ve kilitli yüzey desteyi kapatmamalı.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DragRelease_DoesNotBecomeAClick_ButNextPressCanClick()
        {
            var root=new GameObject("Tıklama sürükleme testi",typeof(RectTransform));
            try
            {
                var obj=new GameObject("Kişi",typeof(RectTransform),typeof(CanvasGroup),typeof(PlanningBoardCard));obj.transform.SetParent(root.transform,false);
                var card=obj.GetComponent<PlanningBoardCard>();card.Configure(PlanningCardKind.Person,null,null,null,null,null,(RectTransform)root.transform);
                typeof(PlanningBoardCard).GetField("id",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(card,"sakin-1");
                int selections=0;card.Selected+=c=>selections++;
                var pointer=new PointerEventData(null){button=PointerEventData.InputButton.Left};
                card.OnPointerDown(pointer);card.OnBeginDrag(pointer);Assert.AreEqual(1,selections);
                card.OnEndDrag(pointer);card.OnPointerClick(pointer);Assert.AreEqual(1,selections,"Sürükleme bitişi desteyi açacak tıklama üretmemeli.");
                card.OnPointerDown(pointer);card.OnPointerClick(pointer);Assert.AreEqual(2,selections,"Yeni saf tıklama çalışmalı.");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test]
        public void JobAreaPlacement_ReplacesTheOldFreePositionWithTheAssignedArea()
        {
            var root = new GameObject("Görev alanı konum testi", typeof(RectTransform), typeof(PlanningBoardView));
            try
            {
                var plane = (RectTransform)root.transform;
                plane.sizeDelta = new Vector2(1000, 800);
                var view = root.GetComponent<PlanningBoardView>();
                typeof(PlanningBoardView).GetField("dragPlane", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, plane);
                var cardObject = new GameObject("Atanan kişi", typeof(RectTransform), typeof(CanvasGroup), typeof(PlanningBoardCard));
                cardObject.transform.SetParent(plane, false);
                var card = cardObject.GetComponent<PlanningBoardCard>();
                ((RectTransform)cardObject.transform).sizeDelta = new Vector2(180, 120);
                var zoneObject = new GameObject("Teknisyen alanı", typeof(RectTransform), typeof(PlanningBoardDropZone));
                zoneObject.transform.SetParent(plane, false);
                var zoneRect = (RectTransform)zoneObject.transform;
                zoneRect.anchoredPosition = new Vector2(250, 170);
                zoneRect.sizeDelta = new Vector2(240, 190);
                var content = (RectTransform)new GameObject("Kart alanı", typeof(RectTransform)).transform;
                content.SetParent(zoneRect, false);
                content.sizeDelta = new Vector2(220, 140);
                content.anchoredPosition = new Vector2(0, -15);
                var zone = zoneObject.GetComponent<PlanningBoardDropZone>();
                zone.Configure(PlanningDropKind.Job, "tek", null, null, content);

                ((RectTransform)card.transform).anchoredPosition = new Vector2(-317.25f, -243.75f);
                view.PlaceInZone(card, zone);
                Assert.Less(Vector3.Distance(card.transform.position, content.TransformPoint(content.rect.center)), .01f,
                    "Kayıttaki eski serbest konum atanmış görevin görüntüsünü ezmemeli.");
                zoneRect.anchoredPosition = new Vector2(-220, 110);
                view.PlaceInZone(card, zone);
                Assert.Less(Vector3.Distance(card.transform.position, content.TransformPoint(content.rect.center)), .01f,
                    "Görev alanı sahnede taşınınca kart da serileştirilmiş alana göre yerleşmeli.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void WrongCardType_DoesNotReachAJobOrStorageCommandTarget()
        {
            var root = new GameObject("Görev alanı bırakma testi", typeof(RectTransform));
            try
            {
                var plane = (RectTransform)root.transform;
                var cardObject = new GameObject("Kişi", typeof(RectTransform), typeof(CanvasGroup), typeof(PlanningBoardCard));
                cardObject.transform.SetParent(plane, false);
                var card = cardObject.GetComponent<PlanningBoardCard>();
                var identity = typeof(PlanningBoardCard).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
                var targetObject = new GameObject("Bırakma alanı", typeof(RectTransform), typeof(PlanningBoardDropZone));
                targetObject.transform.SetParent(plane, false);
                var zone = targetObject.GetComponent<PlanningBoardDropZone>();
                int accepted = 0;
                zone.Dropped += (dropped, destination) => accepted++;
                var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };

                card.Configure(PlanningCardKind.Person, null, null, null, null, null, plane);
                identity.SetValue(card, "sakin-1");
                card.OnBeginDrag(pointer);
                zone.Configure(PlanningDropKind.Inventory, "stock", null, null, (RectTransform)targetObject.transform);
                zone.OnDrop(pointer);
                Assert.AreEqual(0, accepted, "Kişi kartı Depo'ya bırakılınca sahiplik veya görev komutu gitmemeli.");
                zone.Configure(PlanningDropKind.Job, "tek", null, null, (RectTransform)targetObject.transform);
                zone.OnDrop(pointer);
                Assert.AreEqual(1, accepted, "Kişi kartı görev alanına bırakılabilmeli.");
                card.OnEndDrag(pointer);

                card.Configure(PlanningCardKind.Equipment, null, null, null, null, null, plane);
                identity.SetValue(card, "esya-1");
                card.OnBeginDrag(pointer);
                zone.OnDrop(pointer);
                Assert.AreEqual(1, accepted, "Teçhizatı boş görev alanına bırakmak kişi atama komutu üretmemeli.");
                zone.Configure(PlanningDropKind.Inventory, "stock", null, null, (RectTransform)targetObject.transform);
                zone.OnDrop(pointer);
                Assert.AreEqual(2, accepted, "Teçhizat Depo'ya bırakılabilmeli.");
                card.OnEndDrag(pointer);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void FreeSurfaceDrop_PreservesTheAcceptedUngridPosition_AfterDragEnds()
        {
            var root = new GameObject("Serbest pano testi", typeof(RectTransform));
            try
            {
                var plane = (RectTransform)root.transform;
                plane.sizeDelta = new Vector2(1000, 800);
                var surface = root.AddComponent<PlanningBoardSurface>();
                var person = new GameObject("Kişi kartı", typeof(RectTransform), typeof(CanvasGroup), typeof(PlanningBoardCard));
                person.transform.SetParent(plane, false);
                var card = person.GetComponent<PlanningBoardCard>();
                card.Configure(PlanningCardKind.Person, null, null, null, null, null, plane);
                typeof(PlanningBoardCard).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, "sakin-1");
                var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left, position = new Vector2(400, 300) };
                var accepted = new Vector2(127.375f, -86.25f);
                surface.Dropped += (dragged, data) => dragged.KeepPosition(accepted);

                card.OnBeginDrag(pointer);
                surface.OnDrop(pointer);
                card.OnEndDrag(pointer);

                Assert.IsFalse(card.IsDragging);
                Assert.IsNull(PlanningBoardCard.Dragging);
                Assert.AreSame(plane, card.transform.parent);
                Assert.AreEqual(accepted, ((RectTransform)card.transform).anchoredPosition,
                    "Başarılı boş pano bırakması eski cebe ya da ızgaraya dönmemeli.");
                Assert.IsTrue(person.GetComponent<CanvasGroup>().blocksRaycasts);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void WorkArea_ClampsOnlyItsEdges_AndKeepsInteriorFreeCoordinates()
        {
            var root = new GameObject("Pano kenarı testi", typeof(RectTransform), typeof(PlanningBoardView));
            try
            {
                var plane = (RectTransform)root.transform;
                plane.sizeDelta = new Vector2(1000, 800);
                var area = (RectTransform)new GameObject("Çalışma alanı", typeof(RectTransform)).transform;
                area.SetParent(plane, false);
                area.sizeDelta = new Vector2(500, 300);
                var cardObject = new GameObject("Serbest kart", typeof(RectTransform), typeof(CanvasGroup), typeof(PlanningBoardCard));
                cardObject.transform.SetParent(plane, false);
                var rect = (RectTransform)cardObject.transform;
                rect.sizeDelta = new Vector2(100, 80);
                var card = cardObject.GetComponent<PlanningBoardCard>();
                var view = root.GetComponent<PlanningBoardView>();
                typeof(PlanningBoardView).GetField("dragPlane", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, plane);
                typeof(PlanningBoardView).GetField("workArea", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, area);

                var interior = new Vector2(31.125f, -22.875f);
                Assert.AreEqual(interior, view.ClampPosition(card, interior), "Pano içindeki yerleşim ızgaraya yuvarlanmamalı.");
                rect.anchoredPosition = view.ClampPosition(card, new Vector2(1000, -1000));
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector2 local = area.InverseTransformPoint(corner);
                    Assert.IsTrue(area.rect.Contains(local), "Kart bütünüyle panonun içinde kalmalı.");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DisabledWhileDragging_RestoresPocketAndInputBeforeNotification_AndRefilledSlotCanDragAgain()
        {
            var root = new GameObject("Kart sürükleme testi", typeof(RectTransform));
            try
            {
                var pocket = new GameObject("Depo cebi", typeof(RectTransform)).transform;
                pocket.SetParent(root.transform, false);
                var plane = (RectTransform)new GameObject("Sürükleme yüzeyi", typeof(RectTransform)).transform;
                plane.SetParent(root.transform, false);
                plane.sizeDelta = new Vector2(1000, 800);
                var cardObject = new GameObject("Yeniden kullanılan eşya kartı", typeof(RectTransform), typeof(CanvasGroup), typeof(PlanningBoardCard));
                cardObject.transform.SetParent(pocket, false);
                var home = new Vector3(15, -40, 0);
                cardObject.transform.localPosition = home;
                var card = cardObject.GetComponent<PlanningBoardCard>();
                card.Configure(PlanningCardKind.Equipment, null, null, null, null, null, plane);
                var identity = typeof(PlanningBoardCard).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
                identity.SetValue(card, "esya-32");
                var group = cardObject.GetComponent<CanvasGroup>();
                var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left, position = new Vector2(500, 400) };
                int released = 0;
                card.DragChanged += (changed, dragging) =>
                {
                    if (dragging) return;
                    released++;
                    Assert.IsFalse(changed.IsDragging);
                    Assert.IsNull(PlanningBoardCard.Dragging);
                    Assert.IsTrue(group.blocksRaycasts);
                    Assert.AreEqual(1, group.alpha);
                };

                card.OnBeginDrag(pointer);
                Assert.IsTrue(card.IsDragging);
                Assert.AreSame(plane, card.transform.parent);
                Assert.IsFalse(group.blocksRaycasts);
                cardObject.SetActive(false); // Son eşya hurdaya ayrıldığında Refresh bu kart yuvasını gizler.
                // Bu MonoBehaviour EditMode'da çalışmaz; Play'de Unity'nin çağırdığı yaşam döngüsü girişini açıkça sür.
                typeof(PlanningBoardCard).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(card, null);
                Assert.AreEqual(1, released);
                // SetActive callback'i bittikten sonra bağlayıcı kartı tekrar kullanmadan önce yerine taşır.
                card.RestoreAfterDrag();
                Assert.AreSame(pocket, card.transform.parent);
                Assert.AreEqual(home, card.transform.localPosition);
                card.OnEndDrag(pointer);
                Assert.AreEqual(1, released, "EventSystem'in sonradan gelen bitiş olayı ikinci kez açmamalı.");

                identity.SetValue(card, "esya-33");
                cardObject.SetActive(true); // Üretilen yeni eşya aynı serileştirilmiş kart yuvasını doldurur.
                Assert.AreSame(pocket, card.transform.parent);
                Assert.IsTrue(group.blocksRaycasts);
                Assert.AreEqual(1, group.alpha);
                card.OnBeginDrag(pointer);
                Assert.IsTrue(card.IsDragging, "Yeniden doldurulan kart sürüklenebilir kalmalı.");
                card.OnEndDrag(pointer);
                Assert.AreEqual(2, released);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
