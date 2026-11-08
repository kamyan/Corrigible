using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 5 (R18a, PLAN §1.4–§1.5, paket W1): 3B tarif defteri (<see cref="RecipeBookView"/>) ve sayfa yüzü
    /// (<see cref="RecipeBookPage"/>). Sahne açılmaz: defter, kapak ve yaprak parçaları (manifestteki aralık ve pivotlarla) boş
    /// nesnelerle kurulur; zaman <see cref="RecipeBookView.Advance"/> ile ilerler (Edit modda <c>Update</c> çalışmaz), süreler
    /// <c>SiginakTheme</c>'den okunur. Sayfa düzeni tasarım kaynağının (<c>kagit_kaynak/tarif_acilim_n_*.html</c>, 560 × 800 px) CSS
    /// akışıyla karşılaştırılır; prefab gerektirenler (<c>RecipeBookPage.prefab</c>, Aşama 5'in tek seferlik kurulumu) yoksa atlanır.
    /// </summary>
    public class Rev3RecipeBookTests
    {
        const string PagePrefabPath = "Assets/Corrigible/Desk/Prefabs/Rev3Board/RecipeBookPage.prefab";
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const float Dt = 1f / 240f;
        static readonly string[] Ids = { "armor", "medkit", "emp_cell", "kit_tek", "kit_emp", "kit_ana" };

        readonly List<Object> created = new List<Object>();

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void Clean()
        {
            Loc.SetLanguage(Loc.Turkish);
            DeskTarget.Blocked = null;
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        // ================================================================== kurulum

        static SiginakTheme Theme
        {
            get
            {
                var t = SiginakTheme.Active;
                Assert.IsNotNull(t, "Resources/SiginakTheme");
                return t;
            }
        }

        static float Token(string key)
        {
            Assert.IsTrue(Theme.TryNumber(key, out var v), "temada yok: " + key);
            return v;
        }

        sealed class Bench
        {
            public GameObject board, book;
            public RecipeBookView view;
            public PartDriver cover, leaf;
            public RecipeBookPage left, right, front, back;
            public BoxCollider closed, hitL, hitR;
            public readonly List<BoardSound> sounds = new List<BoardSound>();
            public readonly List<int> spreads = new List<int>();
            public readonly List<(string id, Pose pose)> lifted = new List<(string, Pose)>();
            public int clicks;
            public List<RecipeData> recipes;
        }

        GameObject Make(string name, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            else created.Add(go);
            return go;
        }

        static RecipeBookPage Page(Transform parent, string name, Vector3 local, float yaw, out BoxCollider hit)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(99, 140);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one * 0.001f;
            rt.localPosition = local;
            rt.localRotation = Quaternion.Euler(0, yaw, 0);
            var page = go.AddComponent<RecipeBookPage>();
            var h = new GameObject(name + " çarpıştırıcı");
            h.transform.SetParent(go.transform, false);
            hit = h.AddComponent<BoxCollider>();
            hit.size = new Vector3(99, 140, 2);
            return page;
        }

        /// <summary>
        /// Pano (çerçeve ve ebeveyn), kancadaki defter (Aşama 3 sahne pozu), Body altında Cover (y 0…−178°, pivot (0; 0; −0,009)) ve
        /// Leaf (y 0…−176°, pivot (0,0005; 0; −0,0004)), dört sayfa yüzü (D §1.1 konumları) ve çarpıştırıcılar.
        /// </summary>
        Bench Build(Vector3 boardPos = default, float boardYaw = 0f)
        {
            var b = new Bench();
            b.board = Make("Planning board", null);
            b.board.transform.position = boardPos;
            b.board.transform.rotation = Quaternion.Euler(0, boardYaw, 0);
            b.book = Make("Tarif defteri", b.board.transform);
            var closedPose = RecipeBookView.ClosedPoseLocal;
            b.book.transform.localPosition = closedPose.position;
            b.book.transform.localRotation = closedPose.rotation;
            var body = Make("Body", b.book.transform);
            b.cover = Make("Cover", body.transform).AddComponent<PartDriver>();
            b.cover.Configure("Cover", PartKind.Rotate, PartAxis.Y, 0, -178, 0, new Vector3(0, 0, -0.009f));
            b.leaf = Make("Leaf", body.transform).AddComponent<PartDriver>();
            b.leaf.Configure("Leaf", PartKind.Rotate, PartAxis.Y, 0, -176, 0, new Vector3(0.0005f, 0, -0.0004f));
            b.left = Page(b.cover.transform, "Sayfa L", new Vector3(0.0535f, 0.074f, 0.0093f), 180, out b.hitL);
            b.right = Page(body.transform, "Sayfa R", new Vector3(0.0535f, 0.074f, -0.0006f), 0, out b.hitR);
            b.front = Page(b.leaf.transform, "Yaprak ön", new Vector3(0.0525f, 0.074f, -0.0004f), 0, out var fh);
            b.back = Page(b.leaf.transform, "Yaprak arka", new Vector3(0.0525f, 0.074f, 0.0001f), 180, out var bh);
            b.closed = b.book.AddComponent<BoxCollider>();
            b.closed.center = new Vector3(0.0525f, 0.074f, 0);
            b.closed.size = new Vector3(0.105f, 0.148f, 0.018f);
            b.view = b.book.AddComponent<RecipeBookView>();
            b.view.Configure(b.book.transform, b.board.transform, b.cover, b.leaf, b.left, b.right, b.front, b.back, b.closed, b.hitL, b.hitR);
            b.view.Sound += s => b.sounds.Add(s);
            b.view.SpreadChanged += s => b.spreads.Add(s);
            b.view.Clicked += () => b.clicks++;
            b.view.RecipeLifted += (id, e, pose) => b.lifted.Add((id, pose));
            b.recipes = Recipes();
            b.view.Bind(b.recipes, Words(), "TARİF DEFTERİ", "6 tarif");
            return b;
        }

        static List<RecipeData> Recipes()
        {
            var list = new List<RecipeData>();
            for (int i = 0; i < Ids.Length; i++)
            {
                var r = new RecipeData { Id = Ids[i], Name = Ids[i], Title = Ids[i].ToUpperInvariant(), Index = i, Turns = 1, Number = "TARİF 0" + (i + 1) };
                r.Inputs.Add(new RecipeInput { Kind = "metal", Name = "Metal", BookQty = "× 1", CardQty = "×1", Quantity = 1 });
                r.Inputs.Add(new RecipeInput { Kind = "devre", Name = "Devre", BookQty = "× 1", CardQty = "×1", Quantity = 1 });
                list.Add(r);
            }
            return list;
        }

        static BookWords Words() => new BookWords { Header = "KYGA · TARİF DEFTERİ", Output = "ÇIKTI", Input = "GİRDİ", Time = "SÜRE", Next = "sonraki ►", Prev = "◄ önceki", Tab = "İŞ EMRİ" };

        static void Step(RecipeBookView v, float seconds)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds / Dt));
            for (int i = 0; i < n; i++) v.Advance(Dt);
        }

        static PointerEventData Event(GameObject go, Vector3 world)
        {
            var rr = new RaycastResult { gameObject = go, worldPosition = world };
            return new PointerEventData(null) { button = PointerEventData.InputButton.Left, pointerPressRaycast = rr, pointerCurrentRaycast = rr };
        }

        static void Click(RecipeBookView v, GameObject go, Vector3 world)
        {
            var e = Event(go, world);
            v.OnPointerDown(e);
            v.OnPointerClick(e);
        }

        static void ClickPage(Bench b, bool right, Vector2 mm)
        {
            var page = right ? b.right : b.left;
            Click(b.view, (right ? b.hitR : b.hitL).gameObject, page.WorldAt(mm));
        }

        static void OpenSettled(Bench b)
        {
            b.view.SetInteractive(true);
            b.view.Open();
            Step(b.view, RecipeBookView.ForwardS + 0.05f);
            Assert.AreEqual(BookState.Open, b.view.State);
        }

        static RecipePageGeometry G(bool right) => RecipePageMetrics.Default.Geometry(right);
        static Vector2 NextCorner => new Vector2(95, 136);
        static Vector2 PrevCorner => new Vector2(4, 136);

        static void Near(Vector3 expected, Vector3 actual, float tol, string what) =>
            Assert.Less((expected - actual).magnitude, tol, what + $": beklenen {expected:F5}, olan {actual:F5}");

        static void Near(float expected, float actual, float tol, string what) => Assert.AreEqual(expected, actual, tol, what);

        static float Fraction(Vector3 from, Vector3 to, Vector3 now)
        {
            var d = to - from;
            return Vector3.Dot(now - from, d) / d.sqrMagnitude;
        }

        // ================================================================== E1 · durum makinesi

        [Test]
        public void Book_StateMachine_FromTokens()
        {
            // Süreler temadan: öne gelme 0,35 · kapak 0,25 (0,10'da başlar) · sayfa 0,25 · kapanış 0,3 · alçalma 0,25
            Assert.AreEqual(0.35f, Token("motion.board.bookForwardS"), 1e-6f);
            Assert.AreEqual(0.25f, Token("motion.board.bookCoverS"), 1e-6f);
            Assert.AreEqual(0.25f, Token("motion.board.pageTurnS"), 1e-6f);
            Assert.AreEqual(0.3f, Token("motion.board.bookCloseS"), 1e-6f);
            Assert.AreEqual(0.25f, Token("motion.board.bookDropS"), 1e-6f);
            Assert.AreEqual(0.1f, Token("recipeBook.coverDelayS"), 1e-6f);
            Assert.AreEqual(Token("motion.board.bookForwardS"), RecipeBookView.ForwardS, 1e-6f);
            Assert.AreEqual(Token("motion.board.bookCoverS"), RecipeBookView.CoverS, 1e-6f);
            Assert.AreEqual(Token("motion.board.pageTurnS"), RecipeBookView.TurnS, 1e-6f);
            Assert.AreEqual(Token("motion.board.bookCloseS"), RecipeBookView.CloseS, 1e-6f);
            Assert.AreEqual(Token("motion.board.bookDropS"), RecipeBookView.DropS, 1e-6f);
            Assert.AreEqual(Token("recipeBook.coverDelayS"), RecipeBookView.CoverDelayS, 1e-6f);

            var b = Build();
            var v = b.view;
            b.view.SetInteractive(true);
            Assert.AreEqual(BookState.Closed, v.State);
            var closedPos = v.BasePose.position;
            var readPos = RecipeBookView.ReadPoseLocal.position;

            // B1: kapalı deftere tık → açılıyor; ses başta, olay açılmaya başladıktan sonra
            Click(v, b.closed.gameObject, b.closed.transform.TransformPoint(b.closed.center));
            Assert.AreEqual(BookState.Opening, v.State);
            Assert.AreEqual(1, b.clicks);
            CollectionAssert.AreEqual(new[] { BoardSound.BookOpen }, b.sounds);

            float forward = RecipeBookView.ForwardS, delay = RecipeBookView.CoverDelayS, coverS = RecipeBookView.CoverS;
            Step(v, delay - 0.01f);
            Assert.AreEqual(0f, v.CoverProgress, 1e-6f, "kapak 0,10 s'den önce kıpırdamaz (örtüşük açılış)");
            Step(v, 0.03f);
            Assert.Greater(v.CoverProgress, 0f, "kapak 0,10 s'de başlar");
            Step(v, forward / 2 - delay - 0.02f);
            float half = Fraction(closedPos, readPos, v.BasePose.position);
            Assert.GreaterOrEqual(half, 0.6f, "öne gelme ease-out: yarı sürede yolun ≥ %60'ı (kübik 0,875)");
            Assert.Less(half, 0.95f);
            Step(v, forward - forward / 2 - 0.015f);
            Assert.AreEqual(BookState.Opening, v.State, "0,35 s dolmadan açık değil");
            Assert.Less(v.CoverProgress, 1f);
            Step(v, 0.03f);
            Assert.AreEqual(BookState.Open, v.State, "0,35 s'de açık (kapak 0,10 + 0,25)");
            Assert.AreEqual(1f, v.CoverProgress, 1e-6f);
            Assert.AreEqual(-178f, b.cover.Value, 1e-4f, "kapak parçaya oturur (−178°)");
            Assert.Less(Quaternion.Angle(Quaternion.AngleAxis(-178f, Vector3.up), b.cover.transform.localRotation), 0.01f);
            Assert.AreEqual(forward, delay + coverS, 1e-6f, "kapak öne gelmeyle aynı anda biter");

            // B2: sonraki ► → çevriliyor → açık(2) → açık(3); son açılımda sonraki yok
            ClickPage(b, true, NextCorner);
            Assert.AreEqual(BookState.Turning, v.State);
            Step(v, RecipeBookView.TurnS - 0.015f);
            Assert.AreEqual(BookState.Turning, v.State);
            Step(v, 0.03f);
            Assert.AreEqual(BookState.Open, v.State);
            Assert.AreEqual(1, v.Spread);
            ClickPage(b, true, NextCorner);
            Step(v, RecipeBookView.TurnS + 0.02f);
            Assert.AreEqual(2, v.Spread);
            ClickPage(b, true, NextCorner);
            Assert.AreEqual(BookState.Open, v.State, "açılım 3'te sonraki köşesi yok");
            v.Turn(1);
            Assert.AreEqual(BookState.Open, v.State);
            Assert.AreEqual(2, v.Spread);
            CollectionAssert.AreEqual(new[] { 1, 2 }, b.spreads);

            // B5: kapanış — kapak ilk 0,25 s'de kapanır, kök 0,3 s'de kancada
            v.Close();
            Assert.AreEqual(BookState.Closing, v.State);
            float closeS = RecipeBookView.CloseS, coverClose = Mathf.Min(coverS, closeS);
            Step(v, coverClose + 0.01f);
            Assert.AreEqual(0f, v.CoverProgress, 1e-6f, "kapak 0,25 s'de kapalı");
            Assert.AreEqual(BookState.Closing, v.State, "kök hâlâ kancaya gidiyor");
            Step(v, closeS - coverClose);
            Assert.AreEqual(BookState.Closed, v.State);
            Assert.AreEqual(0f, b.cover.Value, 1e-6f);
            Near(closedPos, b.book.transform.localPosition, 1e-4f, "kapanınca tam kapalı poz");
            CollectionAssert.AreEqual(new[] { BoardSound.BookOpen, BoardSound.PageTurn, BoardSound.PageTurn, BoardSound.BookClose }, b.sounds);

            // Açılım hatırlanır: yeniden açılınca açılım 3'ten
            v.Open();
            Step(v, forward + 0.05f);
            Assert.AreEqual(2, v.Spread);
            Assert.AreSame(b.recipes[4], b.left.Recipe);
            Assert.AreSame(b.recipes[5], b.right.Recipe);
        }

        [Test]
        public void Book_Close_InterruptsAndReverses_FromCurrentPose()
        {
            var b = Build();
            var v = b.view;
            v.SetInteractive(true);
            var closedPos = v.BasePose.position;
            v.Open();
            Step(v, 0.2f);
            v.Close();
            Assert.AreEqual(BookState.Closing, v.State);
            var mid = v.BasePose.position;
            v.Advance(Dt);
            Assert.Less((v.BasePose.position - mid).magnitude, 0.01f, "kapanış kaldığı yerden başlar (zıplamaz)");
            Step(v, 0.1f);
            v.Open();   // kapanırken tık: yeniden açılır
            Assert.AreEqual(BookState.Opening, v.State);
            Step(v, RecipeBookView.ForwardS + 0.05f);
            Assert.AreEqual(BookState.Open, v.State);
            v.Close(false);
            Assert.AreEqual(BookState.Closed, v.State, "animasyonsuz kapanış anında");
            Near(closedPos, b.book.transform.localPosition, 1e-5f, "anında kapalı poz");
            Assert.AreEqual(0f, b.cover.Value, 1e-6f);
            Assert.IsTrue(b.leaf.gameObject.activeSelf, "kapalı defterde yaprak yerinde");
            Assert.IsFalse(b.left.gameObject.activeSelf, "kapalıyken sayfa tuvalleri kapalı");
            Assert.IsFalse(b.right.gameObject.activeSelf);
        }

        // ================================================================== E2 · pozlar

        [Test]
        public void Book_Poses_HookReadLow()
        {
            // Kapalı (Aşama 3 sahne pozu, PBST:493–495 kilitli), okuma ve alçak (dress_room_board.js:169; yerel token)
            var closed = RecipeBookView.ClosedPoseLocal;
            Near(new Vector3(-0.562f, 0.993f, -0.033f), closed.position, 1e-6f, "kanca");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(-4, 0, 0), closed.rotation), 0.01f);
            var read = RecipeBookView.ReadPoseLocal;
            Near(Theme.Vector("recipeBook.readPoseLocal.pos"), read.position, 1e-6f, "okuma pozu token");
            Near(new Vector3(0f, 1.227f, -0.665f), read.position, 1e-5f, "okuma pozu");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(Token("recipeBook.readPoseLocal.tiltX"), 0, 0), read.rotation), 0.01f);
            Assert.AreEqual(-9f, Token("recipeBook.readPoseLocal.tiltX"), 1e-6f);
            var low = RecipeBookView.LowPoseLocal;
            Near(Theme.Vector("recipeBook.lowPoseLocal.pos"), low.position, 1e-6f, "alçak poz token");
            Near(new Vector3(-0.02f, 1.062f, -0.62f), low.position, 1e-5f, "alçak poz");
            Assert.AreEqual(-14f, Token("recipeBook.lowPoseLocal.tiltX"), 1e-6f);
            // R18a "kameranın 0,32 m önüne": K4 kamerası pano yerelinde z −0,98
            Assert.AreEqual(0.315f, read.position.z - Token("cameras.K4.localPos[2]"), 1e-3f, "kameraya 0,315 m");

            var b = Build();
            var v = b.view;
            Near(closed.position, b.book.transform.localPosition, 1e-4f, "kancada");
            Assert.Less(Quaternion.Angle(closed.rotation, b.book.transform.localRotation), 0.05f);

            OpenSettled(b);
            Near(read.position, b.book.transform.localPosition, 5e-4f, "okuma pozunda (±0,5 mm)");
            Assert.Less(Quaternion.Angle(read.rotation, b.book.transform.localRotation), 0.05f, "x −9°");
            Assert.IsFalse(b.leaf.gameObject.activeSelf, "açık defterde yaprak gizli");

            v.Lower(true);
            Assert.AreEqual(BookState.Lowered, v.State);
            Step(v, RecipeBookView.DropS + 0.02f);
            Near(low.position, b.book.transform.localPosition, 5e-4f, "alçak poz (±0,5 mm)");
            Assert.Less(Quaternion.Angle(low.rotation, b.book.transform.localRotation), 0.05f, "x −14°");
            v.Lower(true);
            Assert.AreEqual(BookState.Lowered, v.State, "Lower(true) iki kez zararsız");

            v.Lower(false);
            Assert.AreEqual(BookState.Open, v.State);
            Step(v, RecipeBookView.ForwardS + 0.02f);
            Near(read.position, b.book.transform.localPosition, 5e-4f, "okuma pozuna döndü");

            // B8: alçaktan kancaya
            v.Lower(true);
            Step(v, 0.1f);
            v.Close();
            Step(v, RecipeBookView.CloseS + 0.02f);
            Assert.AreEqual(BookState.Closed, v.State);
            Near(closed.position, b.book.transform.localPosition, 1e-4f, "kapanınca kanca (< 0,1 mm)");
            Assert.Less(Quaternion.Angle(closed.rotation, b.book.transform.localRotation), 0.05f);
        }

        [Test]
        public void Book_Poses_AreInTheBoardFrame()
        {
            // Pano taşınıp döndürülse de okuma pozu pano çerçevesinde: kök = pano.TransformPoint(okuma)
            var b = Build(new Vector3(1.2f, 0.3f, -2f), 35f);
            OpenSettled(b);
            var read = RecipeBookView.ReadPoseLocal;
            Near(b.board.transform.TransformPoint(read.position), b.book.transform.position, 5e-4f, "okuma pozu dünya");
            Assert.Less(Quaternion.Angle(b.board.transform.rotation * read.rotation, b.book.transform.rotation), 0.05f);
        }

        // ================================================================== E3 · bölgeler

        [Test]
        public void Book_Zones_BoundsAndTabs()
        {
            // Köşe sekmeleri: sonraki yalnız sağ sayfada ve son açılımdan önce; önceki yalnız sol sayfada ve ilk açılımdan sonra
            Assert.AreEqual(BookZone.Next, RecipeBookView.ZoneAt(true, NextCorner, 0, 3, true));
            Assert.AreEqual(BookZone.Next, RecipeBookView.ZoneAt(true, NextCorner, 1, 3, false));
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(true, NextCorner, 2, 3, true), "açılım 3'te sonraki yok");
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(false, NextCorner, 0, 3, true), "sol sayfanın sağ altı sonraki değil");
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(false, PrevCorner, 0, 3, true), "açılım 1'de önceki yok");
            Assert.AreEqual(BookZone.Prev, RecipeBookView.ZoneAt(false, PrevCorner, 1, 3, true));
            Assert.AreEqual(BookZone.Prev, RecipeBookView.ZoneAt(false, PrevCorner, 2, 3, true));
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(true, PrevCorner, 2, 3, true), "sağ sayfanın sol altı önceki değil");
            // köşe karesi 16 × 16 mm (çizilen üçgen ~10 mm; 1,6 kat) ve yazının şeridi
            Assert.AreEqual(BookZone.Next, RecipeBookView.ZoneAt(true, new Vector2(99 - 15.5f, 140 - 15.5f), 0, 3, false));
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(true, new Vector2(99 - 16.5f, 140 - 15.5f), 0, 3, false));
            var gr = G(true);
            var label = new Vector2(gr.CornerTextX - 8f, gr.CornerTextCenter);
            Assert.AreEqual(BookZone.Next, RecipeBookView.ZoneAt(true, label, 0, 3, false), "'sonraki ►' yazısı da çevirir");
            var gl = G(false);
            Assert.AreEqual(BookZone.Prev, RecipeBookView.ZoneAt(false, new Vector2(gl.CornerTextX + 8f, gl.CornerTextCenter), 1, 3, false), "'◄ önceki' yazısı");

            // [B] tarif kutusu ve İŞ EMRİ ▶ sekmesi: yalnız sürüklenebilirken
            Assert.AreEqual(BookZone.RecipeL, RecipeBookView.ZoneAt(false, gl.Box.center, 1, 3, true));
            Assert.AreEqual(BookZone.RecipeR, RecipeBookView.ZoneAt(true, gr.Box.center, 1, 3, true));
            Assert.AreEqual(BookZone.RecipeL, RecipeBookView.ZoneAt(false, gl.Tab.center, 1, 3, true), "sekme kutunun parçası");
            Assert.AreEqual(BookZone.RecipeR, RecipeBookView.ZoneAt(true, gr.Tab.center, 1, 3, true));
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(false, gl.Box.center, 1, 3, false), "[A]/düzenlenemez: tarif bölgesi yok");
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(true, gr.Tab.center, 1, 3, false));
            // sayfanın geri kalanı ve dışı
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(true, new Vector2(50, 8), 0, 3, true), "künye");
            Assert.AreEqual(BookZone.Page, RecipeBookView.ZoneAt(false, new Vector2(50, 100), 0, 3, true), "not alanı");
            Assert.AreEqual(BookZone.Outside, RecipeBookView.ZoneAt(true, new Vector2(-0.5f, 50), 0, 3, true));
            Assert.AreEqual(BookZone.Outside, RecipeBookView.ZoneAt(true, new Vector2(50, 140.5f), 0, 3, true));
            Assert.AreEqual(BookZone.Outside, RecipeBookView.ZoneAt(false, new Vector2(99.5f, 50), 0, 3, true));

            // Görünüm: çevirme sürerken tık yok sayılır (B4), sayfa dışı tık kapatır (B5), sayfa içi tık bir şey yapmaz
            var b = Build();
            OpenSettled(b);
            ClickPage(b, true, new Vector2(50, 60));
            Assert.AreEqual(BookState.Open, b.view.State, "sayfa tıkı bir şey yapmaz");
            ClickPage(b, false, PrevCorner);
            Assert.AreEqual(BookState.Open, b.view.State, "açılım 1'de önceki köşesi yok");
            ClickPage(b, true, NextCorner);
            Assert.AreEqual(BookState.Turning, b.view.State);
            Step(b.view, 0.05f);
            ClickPage(b, true, NextCorner);
            ClickPage(b, false, PrevCorner);
            Assert.AreEqual(BookState.Turning, b.view.State);
            CollectionAssert.AreEqual(new[] { 1 }, b.spreads, "çevirme sürerken tık sıraya girmez");
            Step(b.view, RecipeBookView.TurnS);
            Assert.AreEqual(1, b.view.Spread);
            ClickPage(b, false, PrevCorner);
            Step(b.view, RecipeBookView.TurnS + 0.02f);
            Assert.AreEqual(0, b.view.Spread);
            ClickPage(b, true, new Vector2(120, 50));   // çarpıştırıcının sayfa dışı kenarı (kapak kenarı)
            Assert.AreEqual(BookState.Closing, b.view.State, "sayfalar dışı tık kapatır");
            Click(b.view, b.closed.gameObject, b.closed.transform.position);
            Assert.AreEqual(BookState.Closing, b.view.State, "kapanırken tık yok sayılır");
        }

        // ================================================================== E4 · yaprak

        [Test]
        public void Book_Leaf_TurnsBothWays()
        {
            var b = Build();
            var v = b.view;
            var r = b.recipes;
            OpenSettled(b);
            Assert.AreSame(r[0], b.left.Recipe);
            Assert.AreSame(r[1], b.right.Recipe);
            Assert.IsFalse(b.right.RightPage == false, "sağ sayfa sağ düzende");
            Assert.IsTrue(b.right.ShowsNext && !b.right.ShowsPrev);
            Assert.IsFalse(b.left.ShowsPrev, "açılım 1'in sol sayfasında önceki yok");
            Assert.IsFalse(b.leaf.gameObject.activeSelf);
            Assert.IsFalse(b.front.gameObject.activeSelf, "yaprak yüzleri yalnız çevirirken");

            // ileri: yaprağın önü eski sağ sayfa, arkası yeni sol sayfa; altından yeni sağ sayfa hemen; örtülen sol sayfa %90'da
            b.sounds.Clear();
            v.Turn(1);
            CollectionAssert.AreEqual(new[] { BoardSound.PageTurn }, b.sounds, "çevirme başında ses");
            Assert.IsTrue(b.leaf.gameObject.activeSelf);
            Assert.IsTrue(b.front.gameObject.activeInHierarchy && b.back.gameObject.activeInHierarchy);
            Assert.AreSame(r[1], b.front.Recipe); Assert.IsTrue(b.front.RightPage);
            Assert.AreSame(r[2], b.back.Recipe); Assert.IsFalse(b.back.RightPage);
            Assert.AreSame(r[3], b.right.Recipe, "açılan sağ sayfa çevirme başında yeni açılımda");
            Assert.AreSame(r[0], b.left.Recipe, "örtülen sol sayfa henüz eski");
            float last = v.LeafProgress;
            bool midChecked = false;
            while (v.State == BookState.Turning)
            {
                v.Advance(Dt);
                float p = v.LeafProgress;
                if (v.State != BookState.Turning) break;
                Assert.GreaterOrEqual(p, last - 1e-6f, "açı tekdüze");
                last = p;
                if (p < RecipeBookView.CoveredPageSwitchAt - 0.02f) Assert.AreSame(r[0], b.left.Recipe);
                if (p >= RecipeBookView.CoveredPageSwitchAt) Assert.AreSame(r[2], b.left.Recipe);
                if (!midChecked && p >= 0.45f && p <= 0.55f)
                {
                    midChecked = true;
                    // +θ yönü: yaprak yarıda okuyucuya doğru dikilir (−z), defterin içinden geçmez
                    var tip = b.leaf.transform.localRotation * Vector3.right;
                    Assert.Less(tip.z, -0.9f, "yaprak yarıda öne (−z) dikilir");
                }
            }
            Assert.IsTrue(midChecked);
            Assert.AreEqual(BookState.Open, v.State);
            Assert.AreEqual(1, v.Spread);
            Assert.AreSame(r[2], b.left.Recipe);
            Assert.AreSame(r[3], b.right.Recipe);
            Assert.IsTrue(b.left.ShowsPrev && b.right.ShowsNext, "açılım 2: iki köşe");
            Assert.IsFalse(b.leaf.gameObject.activeSelf, "çevirme bitince yaprak gizli");
            Assert.AreEqual(0f, v.LeafProgress, 1e-6f);

            // geri: yaprağın arkası eski sol sayfa, önü yeni sağ sayfa; altından yeni sol sayfa hemen; örtülen sağ sayfa %90'da
            v.Turn(-1);
            Assert.AreEqual(1f, v.LeafProgress, 1e-6f, "geri çevirme −176°'den başlar");
            Assert.AreSame(r[2], b.back.Recipe);
            Assert.AreSame(r[1], b.front.Recipe);
            Assert.AreSame(r[0], b.left.Recipe, "açılan sol sayfa hemen yeni");
            Assert.AreSame(r[3], b.right.Recipe, "örtülen sağ sayfa henüz eski");
            last = v.LeafProgress;
            while (v.State == BookState.Turning)
            {
                v.Advance(Dt);
                if (v.State != BookState.Turning) break;
                float p = v.LeafProgress;
                Assert.LessOrEqual(p, last + 1e-6f, "açı tekdüze (geri)");
                last = p;
                if (1f - p >= RecipeBookView.CoveredPageSwitchAt) Assert.AreSame(r[1], b.right.Recipe);
            }
            Assert.AreEqual(0, v.Spread);
            Assert.AreSame(r[0], b.left.Recipe);
            Assert.AreSame(r[1], b.right.Recipe);
            CollectionAssert.AreEqual(new[] { 1, 0 }, b.spreads);
            CollectionAssert.AreEqual(new[] { BoardSound.PageTurn, BoardSound.PageTurn }, b.sounds);

            // Çevirme sürerken kapanış: çevirme anında biter, defter kapanır
            v.Turn(1);
            Step(v, 0.05f);
            v.Close();
            Assert.AreEqual(BookState.Closing, v.State);
            Assert.AreEqual(1, v.Spread);
            Assert.IsFalse(b.front.gameObject.activeSelf);
        }

        [Test]
        public void Book_CoverOpensTowardTheReader()
        {
            var b = Build();
            b.view.SetInteractive(true);
            b.view.Open();
            bool checkedMid = false;
            for (int i = 0; i < 200 && b.view.State == BookState.Opening; i++)
            {
                b.view.Advance(Dt);
                float p = b.view.CoverProgress;
                if (!checkedMid && p >= 0.45f && p <= 0.55f)
                {
                    checkedMid = true;
                    var edge = b.cover.transform.localRotation * Vector3.right;
                    Assert.Less(edge.z, -0.9f, "kapak yarıda okuyucuya doğru (−z) dikilir; sayfa bloğunun içinden geçmez");
                }
            }
            Assert.IsTrue(checkedMid);
        }

        // ================================================================== E6 · üzerine gelme

        [Test]
        public void Book_HoverTag_Text()
        {
            var b = Build();
            var v = b.view;
            Assert.AreEqual("TARİF DEFTERİ", v.HoverTitle);
            Assert.AreEqual("6 tarif", v.Line);
            var closed = v.BasePose.position;

            v.OnPointerEnter(Event(b.closed.gameObject, Vector3.zero));
            Assert.IsFalse(v.Hovered, "K4 dışında üzerine gelme yok");
            v.SetInteractive(true);
            v.OnPointerEnter(Event(b.closed.gameObject, Vector3.zero));
            Assert.IsTrue(v.Hovered);
            Assert.AreEqual(4f, Token("recipeBook.hoverMm"), 1e-6f, "tek anahtar recipeBook.hoverMm");
            Step(v, RecipeBookView.HoverS + 0.01f);
            Near(closed + new Vector3(0, 0, -RecipeBookView.HoverMm * 0.001f), b.book.transform.localPosition, 1e-5f, "kök 4 mm öne (pano −z)");
            Near(closed, v.BasePose.position, 1e-6f, "temel poz değişmez");
            v.OnPointerExit(Event(b.closed.gameObject, Vector3.zero));
            Assert.IsFalse(v.Hovered);
            Step(v, RecipeBookView.HoverS + 0.01f);
            Near(closed, b.book.transform.localPosition, 1e-6f, "çıkınca geri");

            DeskTarget.Blocked = () => true;
            v.OnPointerEnter(Event(b.closed.gameObject, Vector3.zero));
            Assert.IsFalse(v.Hovered, "oda kilitliyken üzerine gelme yok");
            DeskTarget.Blocked = null;

            OpenSettled(b);
            v.OnPointerEnter(Event(b.hitR.gameObject, Vector3.zero));
            Assert.IsFalse(v.Hovered, "açık defterde etiket yok");

            // Anlık görüntünün sözcükleri (W4 kurucusu; EN satırı çeviri tablosu güncellenince)
            var sim = Simulation.NewGame(79, new Balance { planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100 });
            var tr = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), false, false, false);
            Assert.AreEqual("TARİF DEFTERİ", tr.BookTitle);
            Assert.AreEqual("6 tarif", tr.BookLine);
            UseEnglish();
            var en = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), true, false, false);
            Assert.AreEqual("RECIPE BOOK", en.BookTitle);
            Assert.AreEqual("6 recipes", en.BookLine);
        }

        // ================================================================== B10 · kilitli plan

        [Test]
        public void Book_Locked_RecipePressShakes_NoLift()
        {
            var b = Build();
            var v = b.view;
            OpenSettled(b);
            var read = v.BasePose.position;
            var gr = G(true);

            // kilitli: kart kalkmaz, kök 2 mm titrer, ret sesi; defter okunur (sayfa çevrilir)
            v.Locked = true;
            v.RecipesDraggable = false;
            b.sounds.Clear();
            var e = Event(b.hitR.gameObject, b.right.WorldAt(gr.Box.center));
            v.OnPointerDown(e);
            v.OnBeginDrag(e);
            Assert.IsEmpty(b.lifted, "kilitli planda tarif kalkmaz");
            CollectionAssert.AreEqual(new[] { BoardSound.Deny }, b.sounds);
            Assert.AreEqual(BookState.Open, v.State);
            Assert.AreEqual(2f, Token("motion.board.denyShakeMm"), 1e-6f);
            float shakeS = RecipeBookView.DenyS;
            Step(v, shakeS / 6f);
            float dx = (b.book.transform.localPosition - read).x * 1000f;
            Assert.AreEqual(RecipeBookView.DenyMm, Mathf.Abs(dx), 0.15f, "titreme genliği 2 mm (yanal, pano x)");
            Step(v, shakeS);
            Near(read, b.book.transform.localPosition, 1e-5f, "titreme biter, okuma pozu");
            ClickPage(b, true, NextCorner);
            Assert.AreEqual(BookState.Turning, v.State, "kilitli defter de çevrilir");
            Step(v, RecipeBookView.TurnS + 0.02f);

            // düzenlenemez ama kilitsiz ([A]/baskın): tarif bölgesi yok, hiçbir şey olmaz
            v.Locked = false;
            v.RecipesDraggable = false;
            b.sounds.Clear();
            e = Event(b.hitR.gameObject, b.right.WorldAt(gr.Box.center));
            v.OnPointerDown(e);
            v.OnBeginDrag(e);
            Assert.IsEmpty(b.lifted);
            Assert.IsEmpty(b.sounds);

            // [B] düzenlenebilir: sürükleme eşiğinde tarif kalkar (sayfadaki kutunun pozuyla); alçalmayı sunucu ister
            v.RecipesDraggable = true;
            e = Event(b.hitR.gameObject, b.right.WorldAt(gr.Box.center));
            v.OnPointerDown(e);
            v.OnBeginDrag(e);
            Assert.AreEqual(1, b.lifted.Count);
            Assert.AreEqual(b.recipes[3].Id, b.lifted[0].id, "açılım 2'nin sağ sayfası");
            Near(b.right.RecipeBoxPose().position, b.lifted[0].pose.position, 1e-6f, "kutunun dünya pozu");
            Near(b.right.WorldAt(gr.Box.center), b.lifted[0].pose.position, 1e-5f, "kutunun ortası");
            Assert.IsEmpty(b.sounds, "kalkış sesi kartın (RecipeLift), defter ses yaymaz");
            Assert.AreEqual(BookState.Open, v.State, "defter kendiliğinden alçalmaz; sunucu Lower(true) der");
            var tab = Event(b.hitL.gameObject, b.left.WorldAt(G(false).Tab.center));
            v.OnPointerDown(tab);
            v.OnBeginDrag(tab);
            Assert.AreEqual(b.recipes[2].Id, b.lifted[1].id, "İŞ EMRİ ▶ sekmesinden de kalkar");
            var outside = Event(b.hitL.gameObject, b.left.WorldAt(new Vector2(50, 8)));
            v.OnPointerDown(outside);
            v.OnBeginDrag(outside);
            Assert.AreEqual(2, b.lifted.Count, "kutu dışından kalkmaz");
        }

        // ================================================================== çarpıştırıcılar (yalnız K4)

        [Test]
        public void Book_Colliders_OnlyWhileInteractive()
        {
            var b = Build();
            var v = b.view;
            Assert.IsFalse(b.closed.enabled, "K4 dışında defter tık almaz (K1'de Pano erişimi'ni çalmaz)");
            v.SetInteractive(true);
            Assert.IsTrue(b.closed.enabled);
            Assert.IsFalse(b.hitL.enabled || b.hitR.enabled);
            v.Open();
            Assert.IsFalse(b.closed.enabled);
            Assert.IsFalse(b.hitR.enabled, "açılırken sayfa tıkı yok");
            Step(v, RecipeBookView.ForwardS + 0.05f);
            Assert.IsTrue(b.hitL.enabled && b.hitR.enabled, "açıkken iki sayfa");
            v.Turn(1);
            Assert.IsTrue(b.hitR.enabled);
            Step(v, RecipeBookView.TurnS + 0.02f);
            v.Lower(true);
            Assert.IsFalse(b.hitL.enabled || b.hitR.enabled, "alçakta sayfalar tık almaz (kopya denetçide)");
            v.Lower(false);
            Step(v, RecipeBookView.ForwardS + 0.05f);
            // B6: K4'ten çıkış açık defteri animasyonla kapatır, bütün çarpıştırıcılar kapanır
            v.SetInteractive(false);
            Assert.AreEqual(BookState.Closing, v.State);
            Assert.IsFalse(b.closed.enabled || b.hitL.enabled || b.hitR.enabled);
            Step(v, RecipeBookView.CloseS + 0.02f);
            Assert.AreEqual(BookState.Closed, v.State);
            Assert.IsFalse(b.closed.enabled);
            Click(v, b.closed.gameObject, b.closed.transform.position);
            Assert.AreEqual(BookState.Closed, v.State, "etkileşimsizken tık açmaz");
        }

        // ================================================================== sayfa düzeni (KK, × 99/560 yatay, × 0,175 düşey)

        [Test]
        public void Pages_Metrics_FromKK_Mm()
        {
            const float Tol = 0.2f;
            float sx = 99f / 560f, sy = 0.175f;
            var m = RecipePageMetrics.Default;
            Assert.AreEqual(0.175f, m.emMmPerPx, 1e-6f, "yazı ölçeği 0,25 tasarım mm/px × 0,70");
            Assert.AreEqual(new Vector2(99, 140), m.sizeMm);
            var l = G(false);
            var r = G(true);
            Assert.AreEqual(sx, l.Sx, 1e-6f);
            Assert.AreEqual(sy, l.Sy, 1e-6f);

            // CSS akışı (sol sayfa; kenar 1 px, iç boşluk 26/34/70/30 px; sağ sayfa 26/30/70/34 px)
            Near(31 * sx, l.ContentLeft, Tol, "sol sayfa içerik solu"); Near(525 * sx, l.ContentRight, Tol, "sol sayfa içerik sağı");
            Near(35 * sx, r.ContentLeft, Tol, "sağ sayfa içerik solu"); Near(529 * sx, r.ContentRight, Tol, "sağ sayfa içerik sağı");
            Near((27 + 9) * sy, l.HeaderCenter, Tol, "künye satırı");
            Near((49 + 16.1f) * sy, l.TitleCenter, Tol, "ad satırı");
            Near(89.2f * sy, l.RedRule.y, Tol, "kırmızı çizgi"); Near(2 * sy, l.RedRule.height, 0.01f, "çizgi 2 px");
            Near(37 * sx, l.Box.x, Tol, "kutu solu (margin 6 px)"); Near(121.2f * sy, l.Box.y, Tol, "kutu üstü (margin 30 px)");
            Near(444 * sx, l.Box.width, Tol, "kutu genişliği (sağ margin 44 px)"); Near((490.37f - 121.2f) * sy, l.Box.height, Tol, "kutu yüksekliği");
            Near(41 * sx, r.Box.x, Tol, "sağ sayfada kutu 4 px sağda");
            // D §1.5'in bağımsız ölçümü (R10 PNG'si, yaklaşık): x 6…85, y 21…86 mm
            Near(6f, l.Box.xMin, 1f, "ölçüm x"); Near(85f, l.Box.xMax, 1f, "ölçüm x"); Near(21f, l.Box.yMin, 1f, "ölçüm y"); Near(86f, l.Box.yMax, 1f, "ölçüm y");
            Near(60 * sx, l.InnerLeft, Tol, "kutu içi sol (1 + 22 px)"); Near(458 * sx, l.InnerRight, Tol, "kutu içi sağ");
            Near((142.2f + 7.86f) * sy, l.OutLabelCenter, Tol, "ÇIKTI");
            Near(60 * sx, l.IconBox.x, Tol, "simge kutusu"); Near(167.92f * sy, l.IconBox.y, Tol, "simge kutusu üstü"); Near(95 * sx, l.IconBox.width, Tol, "92 + 1,5 + 1,5 px");
            Near(171 * sx, l.TextX, Tol, "çıktı yazısı (simge + 16 px)");
            Near((284.92f + 7.86f) * sy, l.InLabelCenter, Tol, "GİRDİ");
            Near((306.64f + 22) * sy, l.InRowCenter(0), Tol, "girdi 1"); Near((350.64f + 22) * sy, l.InRowCenter(1), Tol, "girdi 2");
            Near((414.64f + 7.86f) * sy, l.TimeLabelCenter, Tol, "SÜRE");
            Near((438.36f + 13.5f) * sy, l.TimeCenter, Tol, "süre satırı");
            // köşeler 26 px, kutunun dolgu kenarından 4 px taşar
            Near(34 * sx, l.Corners[0].x, Tol, "sol üst köşe x"); Near(118.2f * sy, l.Corners[0].y, Tol, "sol üst köşe y");
            Near((484 - 26) * sx, l.Corners[1].x, Tol, "sağ üst köşe x"); Near(26 * sx, l.Corners[1].width, 0.01f, "köşe 26 px");
            Near((490.37f - 1 + 4 - 26) * sy, l.Corners[2].y, Tol, "sağ alt köşe y");
            // İŞ EMRİ ▶ sekmesi: right −40 px, top %40, 38 × 150 px + 1,5 px çerçeve (solu yok)
            Near(480.5f * sx, l.Tab.x, Tol, "sekme solu"); Near(520 * sx, l.Tab.xMax, Tol, "sekme sağı");
            Near((122.2f + 0.4f * 367.17f) * sy, l.Tab.y, Tol, "sekme üstü"); Near(153 * sy, l.Tab.height, Tol, "sekme 150 + 3 px");
            // el notu: kutudan 26 px sonra, solda 20 px
            Near(516.37f * sy, l.NoteTop, Tol, "not üstü"); Near(51 * sx, l.NoteLeft, Tol, "not solu"); Near(444 * sx, l.NoteWidth, Tol, "not genişliği");
            // alt: çizgi alttan 44 px (x 25…535), sayfa no ve açılım alttan 14 px, köşe sekmesi 58 px, yazısı 64 px içeride
            Near(754 * sy, l.FooterRule.y, Tol, "alt çizgi"); Near(25 * sx, l.FooterRule.x, Tol, "alt çizgi solu"); Near(510 * sx, l.FooterRule.width, Tol, "alt çizgi boyu");
            Near((785 - 7.86f) * sy, l.FooterCenter, Tol, "sayfa no satırı");
            Near(535 * sx, l.PageNumberX, Tol, "sol sayfada sayfa no sağda"); Near(25 * sx, r.PageNumberX, Tol, "sağ sayfada solda");
            Near(280 * sx, r.SpreadX, Tol, "AÇILIM ortada");
            Near(501 * sx, r.CornerTab.x, Tol, "sonraki üçgeni"); Near(741 * sy, r.CornerTab.y, Tol, "sonraki üçgeni");
            Near(58 * sx, r.CornerTab.width, 0.01f, "üçgen 58 px");
            Near(1 * sx, l.CornerTab.x, Tol, "önceki üçgeni");
            Near(539 * sx, r.CornerArrow.x, Tol, "▶ (38…48, 40…52)"); Near(781 * sy, r.CornerArrow.y, Tol, "▶");
            Near(11 * sx, l.CornerArrow.x, Tol, "◀ (10…20)");
            Near(495 * sx, r.CornerTextX, Tol, "'sonraki ►' sağ kenarı (right 64 px)"); Near(65 * sx, l.CornerTextX, Tol, "'◄ önceki' sol kenarı");
            Near((793 - 8.42f) * sy, r.CornerTextCenter, Tol, "köşe yazısı alttan 6 px");
            // kutunun ortası sağ sayfadaki tarif bölgesi
            Assert.IsTrue(r.InRecipe(r.Box.center) && r.InRecipe(r.Tab.center));
            Assert.IsFalse(r.InRecipe(new Vector2(r.Box.x - 0.5f, r.Box.center.y)));
        }

        [Test]
        public void Pages_Layout_FromKK_Mm()
        {
            var page = LoadPage();
            var recipe = Recipes()[2];
            recipe.Note = "→ deneyimli üretici: tek kapanış";
            recipe.PageLabel = "s. 3"; recipe.SpreadLabel = "AÇILIM 2/3"; recipe.SlotWord = "yardımcı yuvası"; recipe.OutLine = "1 ×"; recipe.TurnsLine = "2 vardiya";
            foreach (bool right in new[] { false, true })
            {
                page.Bind(recipe, Words(), right, true, true, true);
                var g = G(right);
                AssertRect(g.RedRule, page.RedRule, "kırmızı çizgi");
                AssertRect(g.Box, page.BoxEdge, "tarif kutusu");
                AssertRect(g.Tab, page.TabEdge, "İŞ EMRİ sekmesi");
                AssertRect(g.IconBox, page.IconEdge, "simge kutusu");
                AssertRect(g.FooterRule, page.FooterRule, "alt çizgi");
                AssertRect(g.CornerTab, page.CornerTab, "köşe sekmesi");
                for (int i = 0; i < 4; i++) AssertRect(g.Corners[i], page.Corners[i], "köşe " + i);
                Assert.AreEqual((BookPageShape.Shape)0, page.Corners[0].Kind);
                Assert.AreEqual(right ? BookPageShape.Shape.CornerBottomRight : BookPageShape.Shape.CornerBottomLeft, page.CornerTab.Kind);
                // yazılar: x hizası ve em (tasarım px × 0,175; küçük etiketler 18 px, D5-A)
                Near(g.ContentLeft, page.HeaderLeft.rectTransform.anchoredPosition.x, 0.05f, "künye solu");
                Near(g.ContentRight, page.HeaderRight.rectTransform.anchoredPosition.x, 0.05f, "künye sağa yaslı");
                Assert.AreEqual(HorizontalAlignmentOptions.Right, page.HeaderRight.horizontalAlignment);
                Near(g.InnerRight, page.InputQty[0].rectTransform.anchoredPosition.x, 0.05f, "'× n' sağa yaslı");
                Near(g.TextX, page.OutCount.rectTransform.anchoredPosition.x, 0.05f, "'1 ×'");
                Near(g.NoteLeft, page.Note.rectTransform.anchoredPosition.x, 0.05f, "not");
                Em(28, page.Title); Em(18, page.HeaderLeft); Em(18, page.HeaderRight); Em(18, page.OutputLabel); Em(18, page.InputLabel); Em(18, page.TimeLabel);
                Em(34, page.OutCount); Em(24, page.OutName); Em(18, page.SlotWord); Em(21, page.InputNames[0]); Em(26, page.InputQty[0]);
                Em(24, page.TimeText); Em(22, page.Note); Em(19, page.PageNumber); Em(19, page.CornerText); Em(18, page.TabText);
                // alt satır ve köşe yazısı 19 px (Aşama 5 kabulü: sağ sayfanın alt köşesi okuma pozunda 18 px'te 13,9 ekran px)
                // '1 ×' ve çıktı adı aynı taban çizgisinde
                Near(page.OutCount.rectTransform.anchoredPosition.y, page.OutName.rectTransform.anchoredPosition.y, 0.01f, "taban çizgisi");
                Assert.AreEqual(2f, page.Note.rectTransform.localEulerAngles.z, 0.01f, "el notu −2° (CSS; tuvalde +2)");
                Assert.AreEqual(90f, page.TabText.rectTransform.localEulerAngles.z, 0.01f, "dikey 'İŞ EMRİ' alttan üste");
                Assert.AreEqual(right, page.SpreadText.gameObject.activeSelf, "AÇILIM yalnız sağ sayfada");
                Assert.AreEqual(right ? "sonraki ►" : "◄ önceki", page.CornerText.text);
                Assert.IsFalse(page.Overflowing, string.Join(", ", page.OverflowFields));
            }

            page.Bind(recipe, Words(), true, false, false, false);
            Assert.IsFalse(page.CornerTab.gameObject.activeSelf, "son açılımda sonraki yok");
            Assert.IsFalse(page.TabEdge.gameObject.activeSelf, "[A]: İŞ EMRİ sekmesi çizilmez");
            page.Bind(null, Words(), true, false, true, true);
            Assert.IsFalse(page.Content.gameObject.activeSelf, "boş sayfa");
            Assert.IsTrue(page.FooterRule.gameObject.activeSelf && page.CornerTab.gameObject.activeSelf, "boş sayfada çizgi ve sekme kalır");
            int layouts = RecipeBookPage.LayoutCount;
            page.Bind(null, Words(), true, false, true, true);
            Assert.AreEqual(layouts, RecipeBookPage.LayoutCount, "aynı veride yerleşim yok");
        }

        [Test]
        public void Pages_NoOverflow_NoAutoSize_TrEn()
        {
            var page = LoadPage();
            var desk = DeskTheme.Active;
            var fonts = new[] { desk.Font(DeskFont.CpBold), desk.Font(DeskFont.LfSemiBold), desk.Font(DeskFont.LfRegular), desk.Font(DeskFont.KalamBold) };
            foreach (var t in page.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.IsInstanceOf<TextMeshProUGUI>(t, t.name);
                CollectionAssert.Contains(fonts, t.font, t.name);
                Assert.AreSame(t.font.material, t.fontSharedMaterial, t.name + ": fontun kendi (ışıksız) malzemesi");
                Assert.IsFalse(t.enableAutoSizing, t.name + ": otomatik punto yok");
                Assert.IsFalse(t.raycastTarget, t.name);
            }
            foreach (var gr in page.GetComponentsInChildren<Graphic>(true)) Assert.IsFalse(gr.raycastTarget, gr.name + ": raycastTarget");
            Assert.AreSame(desk.Font(DeskFont.LfSemiBold), page.OutName.font, "çıktı adı Libre Franklin 600");
            Assert.AreSame(desk.Font(DeskFont.LfRegular), page.InputNames[0].font, "girdi adı Libre Franklin 400");
            Assert.AreSame(desk.Font(DeskFont.KalamBold), page.Note.font, "el notu Kalam 700");

            var sim = Simulation.NewGame(79, new Balance { planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100 });
            foreach (bool english in new[] { false, true })
            {
                if (english) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                var s = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), english, false, false);
                Assert.AreEqual(6, s.Recipes.Count, "altı tarif");
                for (int i = 0; i < s.Recipes.Count; i++)
                {
                    bool right = i % 2 == 1;
                    int spread = i / 2;
                    page.Bind(s.Recipes[i], s.Book, right, spread > 0, spread < 2, true);
                    string where = (english ? "EN " : "TR ") + s.Recipes[i].Id;
                    Assert.IsFalse(page.Overflowing, where + ": " + string.Join(", ", page.OverflowFields));
                    foreach (var t in new[] { page.Title, page.OutName, page.SlotWord, page.HeaderLeft, page.HeaderRight, page.TimeText, page.InputNames[0], page.InputNames[1] })
                    {
                        if (string.IsNullOrEmpty(t.text)) continue;
                        t.ForceMeshUpdate(true, true);
                        Assert.AreEqual(1, t.textInfo.lineCount, where + ": '" + t.text + "' tek satır");
                    }
                }
            }

            // En uzun sözcükler (KK): TEKNİSYEN KİTİ / TECHNICIAN KIT, iki el notu
            var worst = Recipes()[3];
            worst.Title = "TECHNICIAN KIT"; worst.Name = "Technician kit"; worst.SlotWord = "kit slot"; worst.TurnsLine = "2 shifts"; worst.OutLine = "1 ×";
            worst.Note = "→ a missing field kit comes first"; worst.PageLabel = "p. 4"; worst.SpreadLabel = "SPREAD 2/3"; worst.Number = "RECIPE 04";
            var enWords = new BookWords { Header = "KYGA · RECIPE BOOK", Output = "OUTPUT", Input = "INPUT", Time = "DURATION", Next = "next ►", Prev = "◄ previous", Tab = "WORK ORDER" };
            page.Bind(worst, enWords, true, true, true, true);
            Assert.IsFalse(page.Overflowing, string.Join(", ", page.OverflowFields));
            worst.Title = "TEKNİSYEN KİTİ"; worst.Name = "Teknisyen kiti"; worst.Note = "→ deneyimli üretici: tek kapanış";
            page.Bind(worst, Words(), false, true, false, true);
            Assert.IsFalse(page.Overflowing, string.Join(", ", page.OverflowFields));
            page.Note.ForceMeshUpdate(true, true);
            Assert.LessOrEqual(page.Note.textInfo.lineCount, 2, "not en çok iki satır");
        }

        [Test]
        public void PagePrefab_CanvasAndCollider()
        {
            var page = LoadPage();
            var rt = page.Face;
            Assert.AreSame(page.transform, rt, "tuval sayfanın kökü");
            var canvas = rt.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.IsNull(rt.GetComponent<GraphicRaycaster>(), "tuvalde GraphicRaycaster yok (tık çarpıştırıcıdan)");
            Assert.AreEqual(new Vector2(99, 140), rt.sizeDelta, "99 × 140 birim");
            Near(Vector3.one * 0.001f, rt.localScale, 1e-7f, "1 birim = 1 mm");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.pivot, "pivot orta (sayfa yuvasının ortası)");
            Assert.IsNotNull(page.Hit, "sayfa çarpıştırıcısı");
            Assert.AreEqual(new Vector3(99, 140, 2), page.Hit.size, "99 × 140 × 2 mm");
            Assert.IsFalse(page.Hit.enabled, "kapalı başlar; defter açar");
            Assert.IsNotNull(page.Content);
            Assert.AreEqual(6, page.Icons.Count);
            Assert.AreEqual(4, page.MaterialIcons.Count);
            Assert.IsTrue(page.Icons.All(s => s != null) && page.MaterialIcons.All(s => s != null), "simgeler kart setinden");
            Assert.IsNotNull(page.Clock.sprite, "SÜRE saati");
            foreach (var t in page.GetComponentsInChildren<TMP_Text>(true)) Assert.IsNull(t.GetComponent<DeskLocText>(), t.name + ": metin koddan, DeskLocText yok");
        }

        [Test]
        public void BookTextures_And_Materials()
        {
            var right = AssetDatabase.LoadAssetAtPath<Material>(Rev3BoardTextureBakerPaths.RightMat);
            if (right == null) Assert.Ignore("Recipe page R.mat yok (Aşama 5'in tek seferlik kurulumu): " + Rev3BoardTextureBakerPaths.RightMat);
            var left = AssetDatabase.LoadAssetAtPath<Material>(Rev3BoardTextureBakerPaths.LeftMat);
            var cover = AssetDatabase.LoadAssetAtPath<Material>(Rev3BoardTextureBakerPaths.CoverMat);
            var page = AssetDatabase.LoadAssetAtPath<Texture2D>(Rev3BoardTextureBakerPaths.PageTex);
            var coverTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Rev3BoardTextureBakerPaths.CoverTex);
            Assert.IsNotNull(left); Assert.IsNotNull(cover); Assert.IsNotNull(page); Assert.IsNotNull(coverTex);
            foreach (var m in new[] { left, right, cover })
            {
                Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, m.name);
                // Aşama 5 kabulü: kamerayı izleyen dolgu 0,3 m'deki defteri soldurur; sayfa tabanı R18a'nın bölge rengine göre ılık
                var want = m == cover ? Color.white : new Color(0.9f, 0.74f, 0.62f, 1f);
                Assert.Less(Mathf.Abs(want.r - m.GetColor("_BaseColor").r) + Mathf.Abs(want.g - m.GetColor("_BaseColor").g) + Mathf.Abs(want.b - m.GetColor("_BaseColor").b), 1e-3f, m.name + (m == cover ? ": beyaz boya" : ": ılık boya (0,9; 0,74; 0,62)"));
                Assert.AreEqual(0f, m.GetFloat("_Surface"), m.name + ": opak");
            }
            Assert.AreSame(page, left.GetTexture("_BaseMap"));
            Assert.AreSame(page, right.GetTexture("_BaseMap"));
            Assert.AreSame(coverTex, cover.GetTexture("_BaseMap"));
            Assert.AreEqual(new Vector2(-1, 1), left.GetTextureScale("_BaseMap"), "sol sayfa U aynalı (sırt U = 1)");
            Assert.AreEqual(new Vector2(1, 0), left.GetTextureOffset("_BaseMap"));
            Assert.AreEqual(Vector2.one, right.GetTextureScale("_BaseMap"));
            Assert.AreEqual(0.10f, left.GetFloat("_Smoothness"), 1e-4f, "pageFace pürüzü 0,9");
            Assert.AreEqual(0.18f, cover.GetFloat("_Smoothness"), 1e-4f, "bookCloth pürüzü 0,82");
            Assert.AreEqual(792, page.width); Assert.AreEqual(1120, page.height);
            Assert.AreEqual(744, coverTex.width); Assert.AreEqual(1024, coverTex.height);
            foreach (var path in new[] { Rev3BoardTextureBakerPaths.PageTex, Rev3BoardTextureBakerPaths.CoverTex })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.IsTrue(ti.sRGBTexture, path + ": sRGB");
                Assert.IsTrue(ti.mipmapEnabled, path + ": mip");
                Assert.LessOrEqual(ti.maxTextureSize, 2048, path);
            }
            // paylaşılan model malzemeleri değişmez (yalnız sahne örneğinde geçersiz kılınır)
            var pageFace = AssetDatabase.LoadAssetAtPath<Material>("Assets/Corrigible/Desk/Art/Rev3/Materials/pageFace.mat");
            if (pageFace != null) Assert.IsNull(pageFace.GetTexture("_BaseMap"), "pageFace.mat dokusuz kalır");
        }

        // ================================================================== yardımcılar

        /// <summary>Doku ve malzeme yolları (editör baker'ının sabitleriyle aynı; test baker'a değil yola bağlı).</summary>
        static class Rev3BoardTextureBakerPaths
        {
            public const string PageTex = "Assets/Corrigible/Desk/Art/Rev3/Textures/board/tx_recipe_page.png";
            public const string CoverTex = "Assets/Corrigible/Desk/Art/Rev3/Textures/board/tx_book_cover.png";
            public const string LeftMat = "Assets/Corrigible/Desk/Art/Rev3/SceneMaterials/board/Recipe page L.mat";
            public const string RightMat = "Assets/Corrigible/Desk/Art/Rev3/SceneMaterials/board/Recipe page R.mat";
            public const string CoverMat = "Assets/Corrigible/Desk/Art/Rev3/SceneMaterials/board/Book cover.mat";
        }

        RecipeBookPage LoadPage()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PagePrefabPath);
            if (prefab == null) Assert.Ignore("Sayfa prefab'ı yok (Corrigible/Revizyon 3/Aşama 5/Tarif defteri dokuları ve sayfa prefab'ı): " + PagePrefabPath);
            var go = Object.Instantiate(prefab);
            created.Add(go);
            var page = go.GetComponent<RecipeBookPage>();
            Assert.IsNotNull(page, "kökte RecipeBookPage");
            return page;
        }

        static void AssertRect(Rect expectedMm, Graphic g, string what)
        {
            Assert.IsNotNull(g, what);
            var rt = g.rectTransform;
            Assert.AreEqual(new Vector2(0, 1), rt.anchorMin, what + ": sol üst çapa");
            Assert.AreEqual(new Vector2(0, 1), rt.pivot, what + ": sol üst pivot");
            Near(expectedMm.x, rt.anchoredPosition.x, 0.2f, what + " x");
            Near(-expectedMm.y, rt.anchoredPosition.y, 0.2f, what + " y");
            Near(expectedMm.width, rt.sizeDelta.x, 0.2f, what + " w");
            Near(expectedMm.height, rt.sizeDelta.y, 0.2f, what + " h");
        }

        static void Em(float designPx, TMP_Text t)
        {
            Assert.IsNotNull(t);
            Assert.AreEqual(designPx * 0.175f, t.fontSize, 1e-3f, t.name + ": " + designPx + " px × 0,175 mm");
        }

        static void UseEnglish()
        {
            Dictionary<string, string> en = null;
            if (File.Exists(TablePath))
            {
                var table = Loc.ParseTable(File.ReadAllText(TablePath));
                table.TryGetValue(Loc.English, out en);
            }
            Loc.SetLanguage(Loc.English, en);
        }
    }
}
