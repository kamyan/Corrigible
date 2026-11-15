using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 2 · aday grubunun tezgâhtaki görünümü (<see cref="CandidateGroupView"/>; tasarım §7): 2 ve 3 kartın ortalanmış, çakışmayan
    /// yerleşimi ve fişin yeri (ölçüler temadan), KABUL damgasının hedefi (bir kart) ve GERİ ÇEVİR'in hedefi (yalnız fiş), basış pozu,
    /// kart / fiş / satır tıkları ve üzerine gelme metinleri, çarpıştırıcıların yalnız etkileşimliyken açık oluşu, karar dizisinde
    /// çekmeceye kaldırma ve gizleme. Sahne açılmaz: tezgâh kanadı ve çekmece boş nesnelerdir, kâğıtlar prefab örnekleri. İstasyonla
    /// bütünleşme (damga yönlendirme, karar dizisi, emekli araçlar) <c>Rev3AdmissionMotionTests</c>'te.
    /// </summary>
    public sealed class Rev3CandidateViewTests
    {
        const float Dt = 1f / 240f;
        readonly List<Object> created = new List<Object>();

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void Clean()
        {
            DeskTarget.Blocked = null;
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
            Loc.SetLanguage(Loc.Turkish);
        }

        // ---------------------------------------------------------------- kurulum

        sealed class Rig
        {
            public Transform top, drawer;
            public CandidateGroupView view;
            public Transform[] cards;
            public Transform slip;
            public readonly List<int> cardClicks = new List<int>();
            public readonly List<string> replaces = new List<string>();
            public readonly List<AdmissionSound> sounds = new List<AdmissionSound>();
            public int slipClicks;

            public void Advance(float seconds)
            {
                for (float t = 0; t < seconds - 1e-6f; t += Dt) view.Advance(Dt);
            }

            public Collider CardHit(int i) => cards[i].GetComponentInChildren<Collider>(true);
            public Collider SlipHit => slip.GetComponentInChildren<Collider>(true);

            /// <summary>Kartın yerelindeki mm'nin (sol üst köken, y aşağı) dünya noktası.</summary>
            public Vector3 OnCard(int i, Vector2 mm) => cards[i].TransformPoint(new Vector3((mm.x - 74f) / 1000f, (52.5f - mm.y) / 1000f, 0f));

            public Vector3 OnSlip(Vector2 mm) => slip.TransformPoint(new Vector3((mm.x - 105f) / 1000f, (37f - mm.y) / 1000f, 0f));
        }

        Rig Build()
        {
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdmissionFixtures.CardPrefab);
            var slipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdmissionFixtures.GroupSlipPrefab);
            if (cardPrefab == null || slipPrefab == null) Assert.Ignore("Aday kartı / grup fişi prefab'ı yok (KOS aşama 2 kurulumu çalışmamış).");
            var r = new Rig();
            var root = new GameObject("Admission station");
            created.Add(root);
            root.transform.SetPositionAndRotation(new Vector3(2.2f, 0.98f, -0.27f), Quaternion.Euler(0f, 90f, 0f));
            r.top = new GameObject("Top").transform;
            r.top.SetParent(root.transform, false);
            r.drawer = new GameObject("Drawer").transform;
            r.drawer.SetParent(root.transform, false);
            r.drawer.localPosition = new Vector3(0.1f, 0.3f, 0.05f);
            var holder = new GameObject("Aday grubu");
            holder.transform.SetParent(root.transform, false);
            r.cards = new Transform[CandidateGroupView.MaxCards];
            for (int i = 0; i < r.cards.Length; i++)
            {
                var card = Object.Instantiate(cardPrefab, holder.transform);
                card.name = "Aday kartı " + (i + 1);
                card.SetActive(false);
                r.cards[i] = card.transform;
            }
            var slip = Object.Instantiate(slipPrefab, holder.transform);
            slip.name = "Grup fişi";
            slip.SetActive(false);
            r.slip = slip.transform;
            r.view = holder.AddComponent<CandidateGroupView>();
            r.view.Configure(r.top, r.drawer, r.cards, r.slip);
            r.view.CardClicked += r.cardClicks.Add;
            r.view.SlipClicked += () => r.slipClicks++;
            r.view.ReplaceClicked += r.replaces.Add;
            r.view.Sound += r.sounds.Add;
            return r;
        }

        static Simulation Game(uint seed, int capacity = 0)
        {
            var b = new Balance { shelterEventsEnabled = false, planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1 };
            if (capacity > 0) b.boardResidentCapacity = capacity;
            var sim = Simulation.NewGame(seed, b);
            for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++)
            {
                Assert.IsTrue(sim.PrintShift().Ok, sim.BoardPlanCheck());
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            }
            Assert.Greater(sim.Admission.Group.Count, 0, "kurulum: kapıda aday grubu");
            return sim;
        }

        static CandidateGroupData Group(Simulation sim, string replaceId = null) => AdmissionSnapshotBuilder.Build(sim,
            new AdmissionUi { ReplaceForGroup = AdmissionSnapshotBuilder.GroupKey(sim), ReplaceId = replaceId }).Group;

        /// <summary>Tohum 79: iki kart; tohum 3: üç kart.</summary>
        static CandidateGroupData Cards(int n)
        {
            var g = Group(Game(n == 2 ? 79u : 3u));
            Assert.AreEqual(n, g.Cards.Count, "kurulum: " + n + " kartlı grup");
            return g;
        }

        Rig Shown(CandidateGroupData g)
        {
            var r = Build();
            r.view.SetShown(true);
            r.view.SetInteractive(true);
            r.view.Bind(g, false);
            Physics.SyncTransforms();
            return r;
        }

        static PointerEventData Pointer(GameObject hit, Vector3 world) => new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left,
            pointerCurrentRaycast = new RaycastResult { gameObject = hit, worldPosition = world },
            pointerPressRaycast = new RaycastResult { gameObject = hit, worldPosition = world },
        };

        static void Near(Vector3 expected, Vector3 actual, float tolerance, string message) =>
            Assert.Less((expected - actual).magnitude, tolerance, message + " (beklenen " + expected.ToString("F4") + ", gelen " + actual.ToString("F4") + ")");

        /// <summary>Kâğıdın köşelerinin tezgâh kanadı yerelindeki XZ kutusu (m).</summary>
        static Rect Footprint(Transform top, Transform paper, Vector2 sizeMm, float scale)
        {
            float xMin = float.MaxValue, xMax = float.MinValue, zMin = float.MaxValue, zMax = float.MinValue;
            foreach (int sx in new[] { -1, 1 })
                foreach (int sy in new[] { -1, 1 })
                {
                    var world = paper.position + paper.rotation * new Vector3(sx * sizeMm.x / 2000f * scale, sy * sizeMm.y / 2000f * scale, 0f);
                    var local = top.InverseTransformPoint(world);
                    xMin = Mathf.Min(xMin, local.x); xMax = Mathf.Max(xMax, local.x);
                    zMin = Mathf.Min(zMin, local.z); zMax = Mathf.Max(zMax, local.z);
                    Assert.AreEqual(CandidateGroupView.SurfaceY, local.y, 2e-4f, paper.name + ": kâğıt tezgâh yüzeyinde yatar");
                }
            return Rect.MinMaxRect(xMin, zMin, xMax, zMax);
        }

        // ---------------------------------------------------------------- yerleşim

        /// <summary>
        /// 2 ve 3 kart: sıra <c>rowCenterX</c> çevresinde ortalı, adım kart eni × ölçek + boşluk; kart kutuları birbiriyle ve fişle
        /// çakışmaz; damga rafının sol kenarını (x 0,19), MASA tuşunu (x −0,36) ve tezgâhın ön kenarını (z −0,30) geçmez. Fazla kart kapalı.
        /// </summary>
        [Test]
        public void Layout_TwoAndThreeCards_Centred_NoOverlap()
        {
            float scale = CandidateGroupView.CardScale;
            Assert.That(scale, Is.InRange(0.5f, 1f), "admission.candidates.cardScale");
            var cardSize = new Vector2(CandidateCardFace.WidthMm, CandidateCardFace.HeightMm);
            var slipSize = new Vector2(CandidateGroupSlipFace.WidthMm, CandidateGroupSlipFace.HeightMm);
            foreach (int n in new[] { 2, 3 })
            {
                var r = Shown(Cards(n));
                Assert.IsTrue(r.view.Shown);
                Assert.AreEqual(n, r.view.CardCount);
                var boxes = new List<Rect>();
                float sum = 0f;
                for (int i = 0; i < CandidateGroupView.MaxCards; i++)
                {
                    Assert.AreEqual(i < n, r.cards[i].gameObject.activeSelf, n + " kart: kart " + i);
                    if (i >= n) continue;
                    var local = r.top.InverseTransformPoint(r.cards[i].position);
                    Near(CandidateGroupView.CardLocal(i, n), local, 1e-5f, n + " kart: kart " + i + " yeri");
                    Assert.AreEqual(scale, r.cards[i].localScale.x, 1e-6f, "kart ölçeği temadan");
                    Assert.Greater(Vector3.Dot(r.cards[i].rotation * Vector3.back, r.top.up), 0.999f, "kartın yüzü yukarı");
                    sum += local.x;
                    boxes.Add(Footprint(r.top, r.cards[i], cardSize, scale));
                    Assert.AreSame(r.cards[i], r.view.PaperOf(AdmissionPaper.Candidate, i));
                    Assert.IsNotNull(r.view.CardFace(i).Data, "kart yüzü bağlandı");
                    Assert.IsFalse(r.view.CardFace(i).Overflowing, "kart " + i + " taşmaz");
                }
                Assert.IsNull(r.view.PaperOf(AdmissionPaper.Candidate, n), "olmayan kart");
                Assert.AreEqual(CandidateGroupView.RowCenterX, sum / n, 1e-5f, n + " kart: sıra ortalı");
                float pitch = (CandidateCardFace.WidthMm * scale + CandidateGroupView.GapMm) / 1000f;
                Assert.AreEqual(pitch, CandidateGroupView.CardLocal(1, n).x - CandidateGroupView.CardLocal(0, n).x, 1e-6f, "adım = kart eni + boşluk");
                for (int i = 0; i < boxes.Count; i++)
                    for (int k = i + 1; k < boxes.Count; k++) Assert.IsFalse(boxes[i].Overlaps(boxes[k]), n + " kart: " + i + " ve " + k + " çakışıyor");

                Assert.IsTrue(r.slip.gameObject.activeSelf, "grup fişi görünür");
                Near(CandidateGroupView.SlipLocal, r.top.InverseTransformPoint(r.slip.position), 1e-5f, "fişin yeri");
                Assert.AreSame(r.slip, r.view.PaperOf(AdmissionPaper.GroupSlip, 0));
                var slipBox = Footprint(r.top, r.slip, slipSize, 1f);
                foreach (var box in boxes)
                {
                    Assert.IsFalse(box.Overlaps(slipBox), n + " kart: kart fişle çakışıyor");
                    Assert.Greater(box.yMin, slipBox.yMax, "fiş kartların önünde (oyuncuya yakın)");
                }
                foreach (var box in boxes.Concat(new[] { slipBox }))
                {
                    Assert.LessOrEqual(box.xMax, 0.19f, n + " kart: damga rafının sol kenarını geçmez");
                    Assert.GreaterOrEqual(box.xMin, -0.36f, n + " kart: MASA tuşuna girmez");
                    Assert.GreaterOrEqual(box.yMin, -0.30f, n + " kart: tezgâhın ön kenarından taşmaz");
                    Assert.LessOrEqual(box.yMax, 0f, n + " kart: tezgâhın menteşe kenarını geçmez");
                }
                Assert.AreEqual(n + " ADAY", r.view.SlipFace.Title.text);
            }
        }

        /// <summary>Görünürlük ve çarpıştırıcılar: kanat inik değilken (SetShown false) ya da veri yokken kâğıt yok; çarpıştırıcılar yalnız etkileşimliyken açık.</summary>
        [Test]
        public void Shown_AndColliders_FollowTheStation()
        {
            var r = Build();
            var g = Cards(3);
            r.view.Bind(g, false);
            Assert.IsFalse(r.view.Shown, "kanat inmeden görünmez");
            Assert.IsTrue(r.cards.All(c => !c.gameObject.activeSelf) && !r.slip.gameObject.activeSelf);
            Assert.IsNull(r.view.PaperOf(AdmissionPaper.Candidate, 0));
            Assert.IsFalse(r.view.AcceptStamp(StampChoice.Admit, r.top.TransformPoint(CandidateGroupView.CardLocal(0, 3))), "görünmeyen karta damga bırakılmaz");

            r.view.SetShown(true);
            Assert.IsTrue(r.view.Shown);
            Assert.IsTrue(r.cards.All(c => c.gameObject.activeSelf) && r.slip.gameObject.activeSelf);
            Assert.IsTrue(Enumerable.Range(0, 3).All(i => !r.CardHit(i).enabled) && !r.SlipHit.enabled, "etkileşim kapalıyken çarpıştırıcı kapalı");
            r.view.ClickCollider(r.CardHit(0));
            r.view.ClickSlip(-1);
            Assert.IsEmpty(r.cardClicks);
            Assert.AreEqual(0, r.slipClicks, "etkileşim kapalıyken tık yok");

            r.view.SetInteractive(true);
            Assert.IsTrue(Enumerable.Range(0, 3).All(i => r.CardHit(i).enabled) && r.SlipHit.enabled);
            r.view.SetShown(false);
            Assert.IsTrue(Enumerable.Range(0, 3).All(i => !r.CardHit(i).enabled) && !r.SlipHit.enabled, "kanat katlanınca kapanır");
            Assert.IsTrue(r.cards.All(c => !c.gameObject.activeSelf));

            r.view.SetShown(true);
            r.view.Bind(null, false);
            Assert.IsFalse(r.view.Shown, "veri yok");
            Assert.IsNull(r.view.Data);
            Assert.AreEqual(0, r.view.CardCount);
            Assert.IsTrue(r.cards.All(c => !c.gameObject.activeSelf) && !r.slip.gameObject.activeSelf);

            // İki kartlı grupta üçüncü kartın çarpıştırıcısı kapalı kalır
            r.view.Bind(Cards(2), false);
            Assert.IsTrue(r.CardHit(0).enabled && r.CardHit(1).enabled);
            Assert.IsFalse(r.CardHit(2).enabled);
            Assert.IsFalse(r.cards[2].gameObject.activeSelf);
        }

        // ---------------------------------------------------------------- damga

        /// <summary>
        /// KABUL yalnız bir kartın üstüne bırakılır (hedef kart saklanır: <c>StampCandidateId</c>), GERİ ÇEVİR yalnız fişin üstüne; KISITLI ve
        /// YARDIM hiçbir yere. Kartların arası, tezgâhın boşu ve kâğıdın 30 mm'den yükseği hedef değildir. Basış pozu hedefin iz merkezinde,
        /// yüzeyin 0,2 mm üstünde. Yeni grup hedefi sıfırlar.
        /// </summary>
        [Test]
        public void AcceptStamp_AdmitOnCard_RejectOnSlipOnly()
        {
            var g = Cards(3);
            var r = Shown(g);
            Assert.IsNull(r.view.StampCandidateId, "damga basılmadan hedef yok");
            var slipCentre = r.OnSlip(new Vector2(105, 37));
            for (int i = 0; i < 3; i++)
            {
                var centre = r.cards[i].position;
                Assert.IsTrue(r.view.AcceptStamp(StampChoice.Admit, centre), "KABUL kart " + i);
                Assert.AreEqual(g.Cards[i].Id, r.view.StampCandidateId, "hedef basılan kart");
                Assert.IsTrue(r.view.AcceptStamp(StampChoice.Admit, r.OnCard(i, new Vector2(4, 4))), "kartın köşesi de kart");
                Assert.IsTrue(r.view.AcceptStamp(StampChoice.Admit, centre + r.top.up * 0.025f), "25 mm yukarıdan bırakılabilir");
                Assert.IsFalse(r.view.AcceptStamp(StampChoice.Admit, centre + r.top.up * 0.04f), "40 mm yukarısı kart değil");
                Assert.IsFalse(r.view.AcceptStamp(StampChoice.Reject, centre), "GERİ ÇEVİR karta basılmaz");
                Assert.IsFalse(r.view.AcceptStamp(StampChoice.Restricted, centre), "KISITLI kullanılmıyor");
                Assert.IsFalse(r.view.AcceptStamp(StampChoice.Aid, centre), "YARDIM kullanılmıyor");
                Assert.AreEqual(g.Cards[i].Id, r.view.StampCandidateId, "reddedilen bırakış hedefi değiştirmez");

                var pose = r.view.StampPose(StampChoice.Admit);
                var face = r.view.CardFace(i);
                Near(r.OnCard(i, face.ImpressionCenterMm) + r.top.up * 0.0002f, pose.position, 1e-4f, "basış pozu kart " + i + " iz merkezinde");
                Assert.Greater(Vector3.Dot(pose.rotation * Vector3.up, r.top.up), 0.999f, "damga dik");
            }
            // Kartların arası ve tezgâhın boşu
            var between = (r.cards[0].position + r.cards[1].position) / 2f;
            Assert.IsFalse(r.view.AcceptStamp(StampChoice.Admit, between), "iki kartın arası (8 mm boşluk) kart değil");
            Assert.IsFalse(r.view.AcceptStamp(StampChoice.Admit, r.top.TransformPoint(0.3f, CandidateGroupView.SurfaceY, -0.1f)), "tezgâhın boşu");
            Assert.IsFalse(r.view.AcceptStamp(StampChoice.Admit, slipCentre), "KABUL fişe basılmaz");

            Assert.IsTrue(r.view.AcceptStamp(StampChoice.Admit, r.cards[2].position));
            Assert.IsTrue(r.view.AcceptStamp(StampChoice.Reject, slipCentre), "GERİ ÇEVİR fişe");
            Assert.IsNull(r.view.StampCandidateId, "fiş hedefken kart hedefi yok");
            Assert.IsTrue(r.view.AcceptStamp(StampChoice.Reject, r.OnSlip(new Vector2(180, 50))), "fişin sağ bloğu da fiş");
            Assert.IsFalse(r.view.AcceptStamp(StampChoice.Reject, r.OnSlip(new Vector2(105, 37)) + r.top.up * 0.04f));
            var pass = r.view.StampPose(StampChoice.Reject);
            Near(r.OnSlip(r.view.SlipFace.StampCenterMm) + r.top.up * 0.0002f, pass.position, 1e-4f, "GERİ ÇEVİR izi sol bloğun ortasında");
            Assert.IsTrue(r.view.SlipFace.InStampArea(r.view.SlipMm(pass.position)), "basış noktası damga alanında");

            // Yeni grup: hedef sıfır; aynı grubun yeniden bağlanması hedefi korur
            Assert.IsTrue(r.view.AcceptStamp(StampChoice.Admit, r.cards[1].position));
            r.view.Bind(g, false);
            Assert.AreEqual(g.Cards[1].Id, r.view.StampCandidateId, "aynı grup: hedef kalır");
            var other = Cards(2);
            Assert.AreNotEqual(g.Key, other.Key = "group:9:" + other.Cards[0].Id, "kurulum: başka grup (anahtar geliş vardiyası + ilk aday)");
            r.view.Bind(other, false);
            Assert.IsNull(r.view.StampCandidateId, "yeni grup hedefi sıfırlar");
            Assert.IsFalse(r.view.AcceptStamp(StampChoice.Admit, r.top.TransformPoint(CandidateGroupView.CardLocal(2, 3))), "iki kartlı grupta üçüncü kartın yeri boş");
        }

        // ---------------------------------------------------------------- tık ve üzerine gelme

        /// <summary>
        /// Karta tık okuma (<c>CardClicked</c>), fişe tık okuma (<c>SlipClicked</c>), dolu sığınakta fişin satırına tık yollanacak kişi
        /// (<c>ReplaceClicked</c>, kişi kimliği). Gerçek işaretçi olayı vuruş noktasını satıra çevirir. Üzerine gelme başlığı ve satırı veriden.
        /// </summary>
        [Test]
        public void Click_Row_RaisesReplace_ClickCard_RaisesPaper()
        {
            var sim = Game(79, capacity: 8);
            var g = Group(sim);
            Assert.IsTrue(g.Full);
            var r = Shown(g);
            Assert.AreEqual(8, r.view.SlipFace.RowCount);

            for (int i = 0; i < g.Cards.Count; i++) r.view.ClickCollider(r.CardHit(i));
            CollectionAssert.AreEqual(Enumerable.Range(0, g.Cards.Count), r.cardClicks, "kart tıkı dizinle");
            r.view.ClickCollider(r.SlipHit);
            Assert.AreEqual(1, r.slipClicks, "fişe (satır dışı) tık okuma");
            Assert.IsEmpty(r.replaces);

            // Satır: işaretçi olayının dünya noktasından
            for (int row = 0; row < 8; row++)
            {
                var world = r.OnSlip(r.view.SlipFace.RowRect(row).center);
                Assert.AreEqual(row, r.view.SlipFace.RowAt(r.view.SlipMm(world)), "dünya noktası → satır " + row);
                var e = Pointer(r.SlipHit.gameObject, world);
                r.view.OnPointerDown(e);
                r.view.OnPointerClick(e);
            }
            CollectionAssert.AreEqual(g.Replace.Select(x => x.Id), r.replaces, "her satır kendi kişisini bildirir");
            Assert.AreEqual(1, r.slipClicks, "satır tıkı okuma saymaz");

            // Sol blok ve yer başlığı: okuma
            var left = Pointer(r.SlipHit.gameObject, r.OnSlip(new Vector2(30, 40)));
            r.view.OnPointerDown(left);
            r.view.OnPointerClick(left);
            Assert.AreEqual(2, r.slipClicks);
            Assert.AreEqual(8, r.replaces.Count);

            // Karta işaretçi tıkı; sağ tuş ve sürükleme tık değildir; basılan ile bırakılan farklıysa tık yok
            var onCard = Pointer(r.CardHit(1).gameObject, r.cards[1].position);
            r.view.OnPointerDown(onCard);
            r.view.OnPointerClick(onCard);
            Assert.AreEqual(1, r.cardClicks.Last());
            int clicks = r.cardClicks.Count;
            var right = Pointer(r.CardHit(0).gameObject, r.cards[0].position);
            right.button = PointerEventData.InputButton.Right;
            r.view.OnPointerDown(right);
            r.view.OnPointerClick(right);
            var drag = Pointer(r.CardHit(0).gameObject, r.cards[0].position);
            drag.dragging = true;
            r.view.OnPointerDown(drag);
            r.view.OnPointerClick(drag);
            r.view.OnPointerDown(Pointer(r.CardHit(0).gameObject, r.cards[0].position));
            r.view.OnPointerClick(Pointer(r.CardHit(1).gameObject, r.cards[1].position));
            Assert.AreEqual(clicks, r.cardClicks.Count, "sağ tuş, sürükleme ve kayan basış tık değil");

            // Oda kilitliyken tık yok
            DeskTarget.Blocked = () => true;
            r.view.OnPointerDown(onCard);
            r.view.OnPointerClick(onCard);
            Assert.AreEqual(clicks, r.cardClicks.Count, "oda kilitliyken tık yok");
            DeskTarget.Blocked = null;

            // Seçilemeyen satır bildirilmez
            g.Replace[2].Enabled = false;
            g.Replace[2].Reason = "Yollanacak canlı bir sakin seç.";
            r.view.ClickSlip(2);
            Assert.AreEqual(8, r.replaces.Count, "kapalı satır tık vermez");

            // Üzerine gelme: kart → ad + KABUL satırı; satır → kişi adı + ipucu (kapalıysa gerekçe); fişin geri kalanı → KAPI FİŞİ
            r.view.OnPointerEnter(Pointer(r.CardHit(0).gameObject, r.cards[0].position));
            Assert.AreEqual(g.Cards[0].Name, r.view.HoverTitle);
            Assert.AreEqual(g.Cards[0].HoverLine, r.view.Line);
            r.view.OnPointerMove(Pointer(r.SlipHit.gameObject, r.OnSlip(r.view.SlipFace.RowRect(5).center)));
            Assert.AreEqual(g.Replace[5].Name, r.view.HoverTitle);
            Assert.AreEqual("Tıkla: yollanacak kişi olarak işaretle.", r.view.Line);
            r.view.OnPointerMove(Pointer(r.SlipHit.gameObject, r.OnSlip(r.view.SlipFace.RowRect(2).center)));
            Assert.AreEqual("Yollanacak canlı bir sakin seç.", r.view.Line, "kapalı satırın gerekçesi");
            r.view.OnPointerMove(Pointer(r.SlipHit.gameObject, r.OnSlip(new Vector2(30, 40))));
            Assert.AreEqual("KAPI FİŞİ", r.view.HoverTitle);
            Assert.AreEqual(g.SlipHoverLine, r.view.Line);
            r.view.OnPointerExit(Pointer(null, Vector3.zero));
            Assert.IsNull(r.view.HoverTitle);
            Assert.IsNull(r.view.Line);
        }

        // ---------------------------------------------------------------- izler ve karar dizisi

        /// <summary>İz verisi yüzlere geçer: KABUL izi yalnız damgalanan kartta, GERİ ÇEVİR izi fişte; animasyonlu bağlamada 0'dan başlar.</summary>
        [Test]
        public void Bind_Impressions_OnTheStampedCardAndSlip()
        {
            var sim = Game(3);
            var g = Group(sim);
            var r = Shown(g);
            Assert.IsTrue(Enumerable.Range(0, 3).All(i => !r.view.CardFace(i).ImpressionVisible));
            Assert.IsFalse(r.view.SlipFace.StampVisible);

            var stamped = Group(sim);
            AdmissionSnapshotBuilder.StampCard(stamped, stamped.Cards[1].Id, sim.B);
            r.view.Bind(stamped, true);
            Assert.IsTrue(r.view.CardFace(1).ImpressionVisible);
            Assert.IsFalse(r.view.CardFace(0).ImpressionVisible);
            Assert.IsFalse(r.view.CardFace(2).ImpressionVisible);
            Assert.AreEqual(0f, r.view.CardFace(1).ImpressionGroup.alpha, 1e-6f, "basışın başı");
            r.view.CardFace(1).Advance(1f);
            Assert.AreEqual(1f, r.view.CardFace(1).ImpressionGroup.alpha, 1e-6f);
            r.view.Bind(stamped, true);
            Assert.AreEqual(1f, r.view.CardFace(1).ImpressionGroup.alpha, 1e-6f, "aynı iz yeniden oynamaz");

            var passed = Group(sim);
            AdmissionSnapshotBuilder.StampPass(passed);
            r.view.Bind(passed, false);
            Assert.IsTrue(r.view.SlipFace.StampVisible);
            Assert.AreEqual("GERİ ÇEVRİLDİ", r.view.SlipFace.StampText.text);
            Assert.IsTrue(Enumerable.Range(0, 3).All(i => !r.view.CardFace(i).ImpressionVisible), "izsiz veri izi kaldırır");
        }

        /// <summary>
        /// Karar dizisi: <c>Stow</c> görünen bütün kâğıtları o anki pozundan evrak çekmecesine kaldırır (<c>DocDrop</c> bir kez, çarpıştırıcılar
        /// kapalı, damga ve tık yok), süre sonunda kâğıtlar çekmece pozunda üst üste; <c>HideAll</c> gizler ve veriyi, hedefi sıfırlar.
        /// </summary>
        [Test]
        public void Stow_ThenHide()
        {
            var g = Cards(3);
            var r = Shown(g);
            Assert.IsTrue(r.view.AcceptStamp(StampChoice.Admit, r.cards[0].position));
            var start = r.cards[0].position;
            r.view.Stow(0.2f);
            Assert.IsTrue(r.view.Moving);
            CollectionAssert.AreEqual(new[] { AdmissionSound.DocDrop }, r.sounds);
            Assert.IsTrue(Enumerable.Range(0, 3).All(i => !r.CardHit(i).enabled) && !r.SlipHit.enabled, "kalkarken tıklanmaz");
            Assert.IsFalse(r.view.AcceptStamp(StampChoice.Admit, r.cards[0].position), "kalkarken damga bırakılmaz");
            r.view.ClickCollider(r.CardHit(0));
            Assert.IsEmpty(r.cardClicks);
            r.view.Stow(0.2f);
            Assert.AreEqual(1, r.sounds.Count, "ikinci Stow yok sayılır");

            r.Advance(0.1f);
            var mid = r.cards[0].position;
            Assert.Greater((mid - start).magnitude, 0.01f, "yolda");
            r.Advance(0.15f);
            Assert.IsFalse(r.view.Moving);
            for (int i = 0; i < 3; i++)
            {
                var expected = r.drawer.TransformPoint(AdmissionStation.DocInDrawerLocal + new Vector3(0f, CandidateGroupView.StowStepM * (i + 1), 0f));
                Near(expected, r.cards[i].position, 1e-4f, "kart " + i + " çekmece pozunda");
                Assert.IsTrue(r.cards[i].gameObject.activeSelf, "çekmece kapanana dek görünür");
            }
            Near(r.drawer.TransformPoint(AdmissionStation.DocInDrawerLocal + new Vector3(0f, CandidateGroupView.StowStepM * (CandidateGroupView.MaxCards + 1), 0f)),
                r.slip.position, 1e-4f, "fiş çekmece pozunda, en üstte");
            Assert.AreEqual(g.Cards[0].Id, r.view.StampCandidateId, "hedef karar bitene dek durur");

            r.view.HideAll();
            Assert.IsTrue(r.cards.All(c => !c.gameObject.activeSelf) && !r.slip.gameObject.activeSelf);
            Assert.IsNull(r.view.Data);
            Assert.IsNull(r.view.StampCandidateId);
            Assert.IsFalse(r.view.Shown);
            Assert.IsNull(r.view.CardFace(0).Data, "yüzler boşaldı");

            // Sonraki grup tezgâhta, çekmecede değil
            r.view.Bind(Cards(2), false);
            Near(CandidateGroupView.CardLocal(0, 2), r.top.InverseTransformPoint(r.cards[0].position), 1e-5f, "yeni grup tezgâhta");
            Assert.IsTrue(r.CardHit(0).enabled);
        }
    }
}
