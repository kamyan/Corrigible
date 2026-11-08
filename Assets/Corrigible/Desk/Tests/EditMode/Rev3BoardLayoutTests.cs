using Corrigible.Desk;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 kartlı panonun saf geometrisi (Aşama 3 · E4, E6, E7; sahne açılmaz): pano mm ↔ model çapaları, BOŞTA destesinin
    /// aralığı ve zaman çizelgesi (yarıda tersine dönüşün sürekliliği dahil), kişinin arkasındaki eşya yığını, yüz düzlemindeki
    /// bölge isabeti (<see cref="BoardLayout.ZoneAt"/>) ve sürükleme denetçisinin saf kuralları. Sayılar tema
    /// (<c>SiginakTheme</c>) üzerinden okunur; beklenenler model çapalarıdır (C §2.3, E §6).
    /// </summary>
    public class Rev3BoardLayoutTests
    {
        const float AnchorTol = 0.0005f;   // 0,5 mm (model yereli metre)
        const float MmTol = 0.01f;

        static void AssertAnchor(Vector2 mm, float x, float y, string what)
        {
            var p = BoardLayout.MmToLocal(mm, BoardLayout.FaceZ);
            Assert.That(p.x, Is.EqualTo(x).Within(AnchorTol), what + " x");
            Assert.That(p.y, Is.EqualTo(y).Within(AnchorTol), what + " y");
        }

        // ---------------------------------------------------------------- E4

        [Test]
        public void BoardLayout_MmMapsToModelAnchors()
        {
            AssertAnchor(BoardLayout.CardCentre("tek"), -0.512f, 1.465f, "Slot_tek");
            AssertAnchor(BoardLayout.CardCentre("emp"), -0.394f, 1.465f, "Slot_emp");
            AssertAnchor(BoardLayout.CardCentre("ana"), -0.276f, 1.465f, "Slot_ana");
            AssertAnchor(BoardLayout.CardCentre("craft"), -0.512f, 1.265f, "Slot_uret");
            AssertAnchor(BoardLayout.CardCentre("cook"), -0.394f, 1.265f, "Slot_mutf");
            AssertAnchor(BoardLayout.CardCentre("train"), -0.276f, 1.265f, "Slot_egit");
            AssertAnchor(BoardLayout.Deck, 0.518f, 1.465f, "Bosta_Deck");
            Assert.That(BoardLayout.MmToLocal(new Vector2(BoardLayout.HandLeftX, 0), 0).x, Is.EqualTo(-0.118f).Within(AnchorTol), "Bosta_HandLeft x");
            AssertAnchor(BoardLayout.Material(0), -0.400f, 1.091f, "Mat_metal");
            AssertAnchor(BoardLayout.Material(2), -0.224f, 1.091f, "Mat_kumas");
            Assert.That(BoardLayout.MmToLocal(BoardLayout.Kit(0), 0).x, Is.EqualTo(-0.039f).Within(AnchorTol), "Kit_0 x");
            AssertAnchor(BoardLayout.Recipe, -0.066f, 1.265f, "Order_Recipe");
            AssertAnchor(BoardLayout.FolderFace.center, 0.448f, 1.235f, "Folder (dosya yüzünün merkezi)");
            // Konu listesi: model çapası ilk satırın sol taban çizgisi (356, 282). Yedi satır × 16 mm (Soru 1) taban çizgisini
            // 22 mm'lik beş satırlık baskıdan biraz kaydırır; sol kenar aynıdır.
            AssertAnchor(new Vector2(356, 282), -0.224f, 1.311f, "Topics");
            Assert.That(BoardLayout.TopicBaseline(0).x, Is.EqualTo(356f).Within(0.5f), "konu yazısının sol kenarı");
            Assert.That(BoardLayout.TopicBaseline(0).y, Is.EqualTo(282f).Within(2f), "ilk konunun taban çizgisi");
        }

        [Test]
        public void BoardLayout_MmAndLocal_RoundTrip()
        {
            var samples = new[] { Vector2.zero, new Vector2(1160, 600), new Vector2(68, 128), new Vector2(1098.5f, 501.25f) };
            foreach (var mm in samples)
            {
                var back = BoardLayout.LocalToMm(BoardLayout.MmToLocal(mm, -0.02f));
                Assert.That(back.x, Is.EqualTo(mm.x).Within(0.01f), "x " + mm);
                Assert.That(back.y, Is.EqualTo(mm.y).Within(0.01f), "y " + mm);
            }
            // Kartlar yüzün önünde (daha eksi z); seviye büyüdükçe öne
            Assert.Less(BoardLayout.CardZ(0), BoardLayout.FaceZ);
            Assert.Less(BoardLayout.CardZ(BoardLayout.PocketLevel), BoardLayout.CardZ(0));
        }

        // ---------------------------------------------------------------- E6

        [Test]
        public void Deck_SpacingAndSchedule()
        {
            for (int n = 2; n <= 8; n++) Assert.That(BoardLayout.HandStep(n), Is.EqualTo(82f).Within(MmTol), "aralık n = " + n);
            Assert.That(BoardLayout.HandStep(9), Is.EqualTo(79.5f).Within(MmTol), "aralık n = 9");
            Assert.That(BoardLayout.HandStep(10), Is.EqualTo(636f / 9f).Within(MmTol), "aralık n = 10");
            Assert.That(BoardLayout.HandStep(10), Is.EqualTo(70.67f).Within(0.01f));

            for (int n = 1; n <= 10; n++)
            {
                Assert.GreaterOrEqual(BoardLayout.HandOpen(n - 1, n).x, BoardLayout.HandLeftX - MmTol, "en soldaki kart, n = " + n);
                Assert.AreEqual(BoardLayout.Deck, BoardLayout.HandOpen(0, n), "üst kart tepside kalır, n = " + n);
                Assert.AreEqual(BoardLayout.Deck, BoardLayout.DeckClosed(0), "kapalı destenin üstü");
            }
            Assert.That(BoardLayout.HandOpen(2, 3).x, Is.EqualTo(934f).Within(MmTol), "n = 3");
            Assert.That(BoardLayout.HandOpen(7, 8).x, Is.EqualTo(524f).Within(MmTol), "n = 8");
            Assert.That(BoardLayout.HandOpen(9, 10).x, Is.EqualTo(462f).Within(MmTol), "n = 10");
            Assert.Less(80f - BoardLayout.HandStep(10), 25f, "n = 10'da örtüşme adları kapatmaz");

            // Kapalı deste: her alttaki 1 mm sağ-aşağı; üst kart en önde (açık elde de)
            for (int j = 1; j < 5; j++)
            {
                Assert.AreEqual(BoardLayout.Deck + new Vector2(j, j), BoardLayout.DeckClosed(j), "kapalı j = " + j);
                Assert.Greater(BoardLayout.CardZ(BoardLayout.DeckClosedLevel(j, 5)), BoardLayout.CardZ(BoardLayout.DeckClosedLevel(j - 1, 5)), "kapalı sıra j = " + j);
                Assert.Greater(BoardLayout.CardZ(BoardLayout.HandLevel(j, 5)), BoardLayout.CardZ(BoardLayout.HandLevel(j - 1, 5)), "açık sıra j = " + j);
            }

            // Zaman çizelgesi: açılış üstten (j·0,07), toplama alttan; kart başına 0,32 s
            const int count = 5;
            for (int j = 0; j < count; j++)
            {
                Assert.That(DeckMotion.OpenDelay(j, count), Is.EqualTo(0.07f * j).Within(1e-4f), "açılış başı j = " + j);
                Assert.That(BoardLayout.DeckEnd(j) - BoardLayout.DeckStart(j), Is.EqualTo(0.32f).Within(1e-4f), "kart süresi j = " + j);
                Assert.That(DeckMotion.CollectDelay(j, count), Is.EqualTo(0.07f * (count - 1 - j)).Within(1e-4f), "toplama başı j = " + j);
            }
            Assert.That(DeckMotion.CollectDelay(count - 1, count), Is.EqualTo(0f).Within(1e-6f), "en alttaki önce toplanır");
            Assert.That(DeckMotion.WaveSeconds(10), Is.EqualTo(0.95f).Within(1e-4f), "n = 10 toplam");
            Assert.That(DeckMotion.WaveSeconds(1), Is.EqualTo(0.32f).Within(1e-4f));

            // Model yereli pozlar: açık elde j. kart HandOpen'da, kapalıda DeckClosed'da
            var open3 = DeckMotion.OpenLocal(3, count);
            var closed3 = DeckMotion.ClosedLocal(3, count);
            Assert.That(BoardLayout.LocalToMm(open3).x, Is.EqualTo(1098f - 3 * 82f).Within(MmTol));
            Assert.That(BoardLayout.LocalToMm(closed3).x, Is.EqualTo(1101f).Within(MmTol));
        }

        [Test]
        public void Deck_ReversalMidway_IsContinuousAndNoFaster()
        {
            const int n = 5;
            float per = BoardLayout.DeckPerCardS;
            for (int j = 1; j < n; j++)
            {
                var closed = DeckMotion.ClosedLocal(j, n);
                var open = DeckMotion.OpenLocal(j, n);
                float delay = DeckMotion.OpenDelay(j, n);
                float full = (open - closed).magnitude;
                float peakOpen = PeakSpeed(closed, open, delay, per, delay, delay + per);

                for (float tr = 0.05f; tr < delay + per; tr += 0.05f)
                {
                    var at = DeckMotion.Sample(closed, open, delay, per, tr);
                    float seconds = DeckMotion.ReverseSeconds((at - closed).magnitude, full);
                    Assert.GreaterOrEqual(seconds, DeckMotion.MinReverseS - 1e-6f, "en kısa dönüş");
                    Assert.LessOrEqual(seconds, per + 1e-6f, "dönüş bir karttan uzun sürmez");

                    // Süreklilik: dönüş o anki yerden başlar
                    var start = DeckMotion.Sample(at, closed, tr, seconds, tr);
                    Assert.Less((start - at).magnitude, 1e-6f, "sıçrama yok, j = " + j + ", t = " + tr);
                    var end = DeckMotion.Sample(at, closed, tr, seconds, tr + seconds);
                    Assert.Less((end - closed).magnitude, 1e-6f, "dönüş kapalı yerde biter");

                    // Tepe hızı ilk hareketinkini aşmaz (kalan yolun payıyla kısalan süre)
                    float peakBack = PeakSpeed(at, closed, tr, seconds, tr, tr + seconds);
                    Assert.LessOrEqual(peakBack, peakOpen * 1.01f + 1e-6f, "dönüş hızı, j = " + j + ", t = " + tr);
                }
            }
        }

        static float PeakSpeed(Vector3 from, Vector3 to, float delay, float seconds, float t0, float t1)
        {
            const float dt = 0.001f;
            float peak = 0f;
            var prev = DeckMotion.Sample(from, to, delay, seconds, t0);
            for (float t = t0 + dt; t <= t1 + 1e-6f; t += dt)
            {
                var p = DeckMotion.Sample(from, to, delay, seconds, t);
                peak = Mathf.Max(peak, (p - prev).magnitude / dt);
                prev = p;
            }
            return peak;
        }

        // ---------------------------------------------------------------- E7

        [Test]
        public void ItemStack_OrderAndOffsets()
        {
            // Kişi kartı 80 × 120 (merkezden alt kenar 60 mm), eşya 80 × 50 (yarı yükseklik 25 mm): her bant 14 mm görünür
            float personBottom = 60f, itemHalf = 25f, previousBottom = personBottom;
            for (int k = 0; k < 3; k++)
            {
                var off = BoardLayout.ItemOffsetMm(k);
                Assert.AreEqual(0f, off.x, "eşya kişinin altında ortalı, k = " + k);
                Assert.That(off.y, Is.EqualTo(49f + 14f * k).Within(MmTol), "eşya merkezi, k = " + k);
                Assert.That(off.y + itemHalf - previousBottom, Is.EqualTo(14f).Within(MmTol), "görünen şerit, k = " + k);
                previousBottom = off.y + itemHalf;

                // Model yerelinde: her bant 14 mm aşağı
                float dy = BoardLayout.MmToLocal(off, 0).y - BoardLayout.MmToLocal(Vector2.zero, 0).y;
                Assert.That(dy, Is.EqualTo(-0.049f - 0.014f * k).Within(1e-5f), "yerel y, k = " + k);

                // Kişinin arkasında, sıradaki daha arkada
                float z = BoardLayout.CardZ(BoardLayout.ItemLevel(k));
                Assert.Greater(z, BoardLayout.CardZ(BoardLayout.PocketLevel), "eşya kişinin arkasında, k = " + k);
                if (k > 0) Assert.Greater(z, BoardLayout.CardZ(BoardLayout.ItemLevel(k - 1)), "sonraki bant daha arkada, k = " + k);
                Assert.Less(z, BoardLayout.FaceZ, "eşya pano yüzünün önünde, k = " + k);
            }
            Assert.AreEqual(BoardLayout.ItemLevel(2), BoardLayout.ItemLevel(3), "en çok üç bant (kit, zırh, yardımcı)");
            Assert.AreEqual(BoardLayout.ItemLevel(0), BoardLayout.ItemLevel(-1));
        }

        // ---------------------------------------------------------------- bölge isabeti (yüz düzlemi)

        [Test]
        public void ZoneAt_CentreInsideAndOutside()
        {
            foreach (var job in BoardLayout.Jobs)
                ExpectRect(BoardLayout.Pocket(job), BoardCardKind.Person, BoardTarget.Pocket(job), "cep " + job);
            ExpectRect(BoardLayout.Bosta, BoardCardKind.Person, BoardTarget.Bosta(), "BOŞTA");
            ExpectRect(BoardLayout.Order, BoardCardKind.Material, BoardTarget.Order(), "İŞ EMRİ");

            var depot = BoardLayout.Depot;
            foreach (var kind in new[] { BoardCardKind.Item, BoardCardKind.Chip })
            {
                Assert.AreEqual(BoardTarget.Store(), BoardLayout.ZoneAt(depot.center, kind), "DEPO merkezi " + kind);
                Assert.AreEqual(BoardTarget.Store(), BoardLayout.ZoneAt(new Vector2(depot.center.x, depot.yMin + 1), kind), "DEPO üst kenarın 1 mm içi " + kind);
                Assert.AreEqual(BoardTarget.Store(), BoardLayout.ZoneAt(new Vector2(depot.center.x, depot.yMax - 1), kind), "DEPO alt kenarın 1 mm içi " + kind);
                Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(new Vector2(depot.center.x, depot.yMin - 1), kind), "DEPO üst kenarın 1 mm dışı " + kind);
                Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(new Vector2(depot.center.x, depot.yMax + 1), kind), "pano dışı " + kind);
            }
        }

        static void ExpectRect(Rect r, BoardCardKind kind, BoardTarget expected, string what)
        {
            Assert.Greater(r.width, 0f, what + " genişlik");
            var c = r.center;
            Assert.AreEqual(expected, BoardLayout.ZoneAt(c, kind), what + " merkez");
            Assert.AreEqual(expected, BoardLayout.ZoneAt(new Vector2(r.xMin + 1, c.y), kind), what + " sol kenarın 1 mm içi");
            Assert.AreEqual(expected, BoardLayout.ZoneAt(new Vector2(r.xMax - 1, c.y), kind), what + " sağ kenarın 1 mm içi");
            Assert.AreEqual(expected, BoardLayout.ZoneAt(new Vector2(c.x, r.yMin + 1), kind), what + " üst kenarın 1 mm içi");
            Assert.AreEqual(expected, BoardLayout.ZoneAt(new Vector2(c.x, r.yMax - 1), kind), what + " alt kenarın 1 mm içi");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(new Vector2(r.xMin - 1, c.y), kind), what + " sol kenarın 1 mm dışı");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(new Vector2(r.xMax + 1, c.y), kind), what + " sağ kenarın 1 mm dışı");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(new Vector2(c.x, r.yMin - 1), kind), what + " üst kenarın 1 mm dışı");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(new Vector2(c.x, r.yMax + 1), kind), what + " alt kenarın 1 mm dışı");
        }

        [Test]
        public void ZoneAt_WrongKind_IsNone()
        {
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(BoardLayout.Order.center, BoardCardKind.Person), "kişi iş emrine bırakılmaz");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(BoardLayout.Depot.center, BoardCardKind.Person), "kişi depoya bırakılmaz");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(BoardLayout.Pocket("tek").center, BoardCardKind.Material), "hammadde cebe bırakılmaz");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(BoardLayout.Pocket("tek").center, BoardCardKind.Item), "eşya boş cebe değil, kişi kartına (çarpıştırıcı)");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(BoardLayout.Bosta.center, BoardCardKind.Item), "eşya BOŞTA'ya bırakılmaz");
            Assert.AreEqual(BoardTarget.None, BoardLayout.ZoneAt(BoardLayout.Order.center, BoardCardKind.Chip), "fişin iş emri içindeki yeri denetçide");
        }

        // ---------------------------------------------------------------- tarif kartı (Aşama 5 · [B])

        [Test]
        public void ZoneAt_Recipe_IsOrderInside_BookEverywhereElse()
        {
            var order = BoardLayout.Order;
            var c = order.center;
            Assert.AreEqual(BoardTarget.Order(), BoardLayout.ZoneAt(c, BoardCardKind.Recipe), "İŞ EMRİ merkezi");
            Assert.AreEqual(BoardTarget.Order(), BoardLayout.ZoneAt(new Vector2(order.xMin + 1, c.y), BoardCardKind.Recipe), "sol kenarın 1 mm içi");
            Assert.AreEqual(BoardTarget.Order(), BoardLayout.ZoneAt(new Vector2(c.x, order.yMax - 1), BoardCardKind.Recipe), "alt kenarın 1 mm içi");
            Assert.AreEqual(BoardTarget.Book(), BoardLayout.ZoneAt(new Vector2(order.xMin - 1, c.y), BoardCardKind.Recipe), "sol kenarın 1 mm dışı");
            Assert.AreEqual(BoardTarget.Book(), BoardLayout.ZoneAt(new Vector2(c.x, order.yMax + 1), BoardCardKind.Recipe), "alt kenarın 1 mm dışı");
            Assert.AreEqual(BoardTarget.Book(), BoardLayout.ZoneAt(BoardLayout.Pocket("tek").center, BoardCardKind.Recipe), "cep: deftere dön");
            Assert.AreEqual(BoardTarget.Book(), BoardLayout.ZoneAt(BoardLayout.Depot.center, BoardCardKind.Recipe), "depo: deftere dön");
            Assert.AreEqual(BoardTarget.Book(), BoardLayout.ZoneAt(BoardLayout.Book, BoardCardKind.Recipe), "defterin kendisi");
            Assert.AreEqual(BoardTarget.Book(), BoardLayout.ZoneAt(new Vector2(-50, -50), BoardCardKind.Recipe), "pano dışı");
            Assert.IsTrue(order.Contains(BoardLayout.Recipe), "TARİF kutusu iş emrinin içinde");
        }

        [Test]
        public void RecipeOnOrder_KeepsTheWholeCardOnTheOrder()
        {
            var order = BoardLayout.Order;
            var size = BoardLayout.RecipeSizeMm;
            Assert.That(size.x, Is.EqualTo(112f).Within(MmTol));
            Assert.That(size.y, Is.EqualTo(80f).Within(MmTol));
            var r = BoardLayout.RecipeOnOrder;
            Assert.That(r.xMin, Is.EqualTo(order.xMin + 56).Within(MmTol));
            Assert.That(r.xMax, Is.EqualTo(order.xMax - 56).Within(MmTol));
            Assert.That(r.yMin, Is.EqualTo(order.yMin + 40).Within(MmTol));
            Assert.That(r.yMax, Is.EqualTo(order.yMax - 40).Within(MmTol));
            Assert.IsTrue(r.Contains(BoardLayout.Recipe), "TARİF kutusunun yeri serbest alanın içinde");

            var inside = BoardLayout.Recipe + new Vector2(12.5f, -7.25f);
            Assert.AreEqual(inside, BoardLayout.ClampRecipe(inside), "iç nokta olduğu gibi (serbest konum)");
            var corner = BoardLayout.ClampRecipe(new Vector2(order.xMax + 30, order.yMax + 30));
            Assert.That(corner.x, Is.EqualTo(r.xMax).Within(1e-4f));
            Assert.That(corner.y, Is.EqualTo(r.yMax).Within(1e-4f));
            // Kırpılmış kartın dört köşesi iş emrinin içinde
            foreach (var p in new[] { BoardLayout.ClampRecipe(new Vector2(-100, -100)), corner })
            {
                Assert.GreaterOrEqual(p.x - size.x / 2, order.xMin - MmTol);
                Assert.LessOrEqual(p.x + size.x / 2, order.xMax + MmTol);
                Assert.GreaterOrEqual(p.y - size.y / 2, order.yMin - MmTol);
                Assert.LessOrEqual(p.y + size.y / 2, order.yMax + MmTol);
            }
            // Formdaki kart fişlerle aynı düzlemde, pano yüzünün önünde
            Assert.Less(BoardLayout.CardZ(BoardLayout.RecipeLevel), BoardLayout.FaceZ);
        }

        // ---------------------------------------------------------------- sürükleme denetçisinin saf kuralları

        [Test]
        public void DragController_PureRules()
        {
            var order = BoardDragController.KeyboardTargets;
            Assert.AreEqual(10, order.Length, "Tab sırası");
            string[] jobs = { "tek", "emp", "ana", "craft", "cook", "train" };
            for (int i = 0; i < jobs.Length; i++) Assert.AreEqual(BoardTarget.Pocket(jobs[i]), order[i], "Tab " + i);
            Assert.AreEqual(BoardTarget.Bosta(), order[6]);
            Assert.AreEqual(BoardTarget.Order(), order[7]);
            Assert.AreEqual(BoardTarget.Store(), order[8]);
            Assert.AreEqual(BoardTarget.Hurda(), order[9]);

            // Tasarımda saat yönü artı (kişi +3°, desteden alınan −3°, eşya −4°); Unity'de (kamera −z'den bakar) işaret ters
            Assert.That(BoardDragController.HeldTiltFor(BoardCardKind.Person, false), Is.EqualTo(-3f).Within(1e-4f), "kişi");
            Assert.That(BoardDragController.HeldTiltFor(BoardCardKind.Person, true), Is.EqualTo(3f).Within(1e-4f), "desteden alınan");
            Assert.That(BoardDragController.HeldTiltFor(BoardCardKind.Item, false), Is.EqualTo(4f).Within(1e-4f), "eşya");
            Assert.That(BoardDragController.HeldTiltFor(BoardCardKind.Recipe, false), Is.EqualTo(-6f).Within(1e-4f), "tarif: görselde saat yönünde 6° (R18b)");
            Assert.IsFalse(BoardDragController.UsesColliderTargets(BoardCardKind.Recipe), "tarif kartının hedefi yalnız yüz düzleminden");

            // Fiş yeri: iç taraf ızgaraya oturtulmaz, kenar içeri itilir
            var inputs = BoardLayout.Inputs;
            var inside = inputs.center + new Vector2(3.3f, -2.7f);
            Assert.AreEqual(inside, BoardDragController.ClampToInputs(inside), "iç nokta olduğu gibi");
            var outside = BoardDragController.ClampToInputs(new Vector2(inputs.xMax + 40, inputs.yMin - 40));
            Assert.That(outside.x, Is.EqualTo(inputs.xMax).Within(1e-4f));
            Assert.That(outside.y, Is.EqualTo(inputs.yMin).Within(1e-4f));

            Assert.IsTrue(BoardDragController.TargetRectMm(BoardTarget.Store(), out var store));
            Assert.AreEqual(BoardLayout.Depot, store, "DEPO çerçevesi = depo bandı");
            Assert.IsTrue(BoardDragController.TargetRectMm(BoardTarget.Pocket("train"), out var train));
            Assert.AreEqual(BoardLayout.Pocket("train"), train);
            Assert.IsFalse(BoardDragController.TargetRectMm(BoardTarget.Hurda(), out _), "hurda çerçevesi pano yüzünde değil (ön yüz 75 mm önde)");
        }
    }
}
