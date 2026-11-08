using System.Collections.Generic;
using Corrigible.Core;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 5 · W2 (PLAN §1.7, §1.11, §1.12): iş emri formunun çizimi (durum satırı, DURDU damgası, TARİF kutusundaki
    /// tarif kartı ve serbest pozu), [B] defterden alınan kopyanın form kartı olması, ürünün depoya kayması (ilk K4 varışında bir
    /// kez, <c>motion.board.productToStoreS</c>, ease-out), YENİ levhası (yalnız yeni üründe, ilk dokunuşta söner, boyut TR/EN) ve
    /// defter kipinin çarpıştırıcıları. Sahne gerekmez: pano görünümü bellekte kurulur, kartlar bellekteki şablonlardan çoğalır.
    /// Görünüm komut yazmaz; burada simülasyon yoktur.
    /// </summary>
    public class Rev3WorkOrderViewTests
    {
        const string FontDir = "Assets/Corrigible/Desk/Resources/Fonts & Materials/";
        const float MmTol = 1f;

        readonly List<Object> spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = spawned.Count - 1; i >= 0; i--) if (spawned[i] != null) Object.DestroyImmediate(spawned[i]);
            spawned.Clear();
            Loc.SetLanguage(Loc.Turkish);
        }

        // ================================================================== form: durum satırı ve damga

        [Test]
        public void WorkOrder_States_DrawWordAndStamp()
        {
            var rig = Build();
            var stopColour = ThemeColour("stamp.stopped", new Color32(0x9A, 0x1E, 0x14, 0xFF));
            var blue = new Color32(0x1A, 0x2F, 0x5A, 0xFF);
            rig.Status.color = blue;   // sahnedeki mavi kalem; görünüm değiştirmez

            foreach (bool english in new[] { false, true })
            {
                if (english) Loc.SetLanguage(Loc.English, new Dictionary<string, string> { { "DURDU", "STOPPED" } });
                else Loc.SetLanguage(Loc.Turkish);
                string word = Loc.T("DURDU");
                Assert.AreEqual(english ? "STOPPED" : "DURDU", word, "damga sözcüğü");
                string lang = english ? "EN " : "TR ";

                foreach (OrderState state in System.Enum.GetValues(typeof(OrderState)))
                {
                    var d = Sheet(state, word, english);
                    rig.Order.SetSheet(d);
                    string n = lang + state + ": ";
                    Assert.AreEqual(d.Number, rig.Number.text, n + "numara");
                    Assert.AreEqual(d.Status ?? "", rig.Status.text, n + "durum satırı");
                    Assert.AreEqual((Color)blue, rig.Status.color, n + "durum satırı mavi kalem kalır");
                    bool stopped = state == OrderState.Stopped;
                    Assert.AreEqual(stopped ? word : "", rig.Stamp.text, n + "damga sözcüğü (yalnız DURDU'da)");
                    Assert.AreEqual(stopped, rig.Order.Stamped, n + "Stamped");
                    Assert.AreEqual(stopped, rig.StampFrame.activeSelf, n + "damga çerçevesi damgayla birlikte");
                    if (stopped)
                    {
                        Assert.AreEqual(stopColour, rig.Stamp.color, n + "damga rengi stamp.stopped");
                        // durum yalnız renkle verilmez: sözcük + damga + neden satırı
                        StringAssert.Contains(d.StopReason, rig.Status.text, n + "durma nedeni durum satırında");
                    }
                }
            }

            // eski imza (Aşama 3/4) aynen çalışır ve damgaya dokunmaz
            rig.Order.SetSheet(Sheet(OrderState.Stopped, "DURDU", false));
            rig.Order.SetSheet("No 009", "zırh plakası");
            Assert.AreEqual("No 009", rig.Number.text);
            Assert.AreEqual("zırh plakası", rig.Status.text);
            rig.Order.SetSheet(Sheet(OrderState.Empty, "DURDU", false));
            Assert.IsFalse(rig.Order.Stamped, "boş form damgasız");
            Assert.IsFalse(rig.StampFrame.activeSelf);
        }

        // ================================================================== girdi alanı ipucu (Aşama 10, D-F)

        [Test]
        public void WorkOrder_DropHint_FollowsTheSnapshot()
        {
            var rig = Build();
            Assert.AreSame(rig.DropHint, rig.Order.DropHintText);
            const string hint = "GİRDİLER (hammaddeyi buraya bırak)";

            var draft = Sheet(OrderState.Missing, "DURDU", false);
            draft.DropHint = hint;
            rig.Order.SetSheet(draft);
            Assert.AreEqual(hint, rig.DropHint.text, "taslak hammadde bekliyor: ipucu yazılı");

            rig.Order.SetSheet("No 009", "zırh plakası");
            Assert.AreEqual(hint, rig.DropHint.text, "eski imza ipucuna dokunmaz");

            foreach (var state in new[] { OrderState.Empty, OrderState.Full, OrderState.Started, OrderState.Stopped })
            {
                rig.Order.SetSheet(draft);
                rig.Order.SetSheet(Sheet(state, "DURDU", false));
                Assert.AreEqual("", rig.DropHint.text, state + ": ipucu silinir");
            }
        }

        // ================================================================== TARİF kutusundaki kart

        [Test]
        public void WorkOrder_RecipeCard_AtOrderRecipe_1mm()
        {
            var rig = Build();
            var s = Snapshot();
            s.Order = Sheet(OrderState.Recipe, "DURDU", false);
            rig.View.Render(s, false);
            var card = rig.View.OrderRecipeCard;
            Assert.IsNotNull(card, "TARİF kutusunda kart");
            Assert.AreEqual(BoardCardKind.Recipe, card.Kind);
            Assert.AreEqual("is-1", card.Id, "form kartının kimliği iş emri");
            Assert.IsTrue(card.gameObject.activeSelf, "kart görünür");
            NearMm(BoardLayout.Recipe, card, "TARİF kutusu çapası (514, 328)");
            Assert.AreEqual(BoardLayout.CardZ(BoardLayout.RecipeLevel), card.transform.localPosition.z, 1e-5f, "kart seviyesi");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0, card.transform.localEulerAngles.z), 0.05f, "çapada açı 0");

            // çapa sahnede nereye konduysa kart oraya oturur (token değil)
            var anchorMm = new Vector2(530, 340);
            rig.RecipeHome.localPosition = BoardLayout.MmToLocal(anchorMm, 0);
            s.Order.OrderId = "is-2"; s.Order.Card.Id = "is-2";
            rig.View.Render(s, false);
            Assert.AreSame(card, rig.View.OrderRecipeCard, "havuzdaki tek form kartı yeniden bağlanır");
            Assert.AreEqual("is-2", card.Id);
            NearMm(anchorMm, card, "taşınan çapa");

            // [B] serbest poz (recipePoses) ve iş emri alanına kırpma
            rig.View.RecipePose = id => id == "is-2" ? new Vector3(600, 300, 5) : (Vector3?)null;
            rig.View.Render(s, false);
            NearMm(new Vector2(600, 300), card, "kayıtlı serbest poz");
            Assert.AreEqual(5f, Mathf.DeltaAngle(0, card.transform.localEulerAngles.z), 0.05f, "kayıtlı açı");
            rig.View.RecipePose = id => new Vector3(900, 500, 0);
            rig.View.Render(s, false);
            NearMm(BoardLayout.ClampRecipe(new Vector2(900, 500)), card, "iş emri alanına kırpılır");
            var r = BoardLayout.RecipeOnOrder;
            var mm = BoardLayout.LocalToMm(card.transform.localPosition);
            Assert.IsTrue(mm.x <= r.xMax + 0.01f && mm.y <= r.yMax + 0.01f, "kart İŞ EMRİ alanından taşmaz: " + mm);

            // emir kalkınca kutu boşalır, kart havuza döner
            s.Order = Sheet(OrderState.Empty, "DURDU", false);
            rig.View.Render(s, false);
            Assert.IsNull(rig.View.OrderRecipeCard, "boş formda kart yok");
            Assert.IsFalse(card.gameObject.activeSelf, "kart gizlendi");
        }

        [Test]
        public void RecipeCopy_BecomesFormCard_NoJump()
        {
            var rig = Build();
            var s = Snapshot();
            rig.View.Render(s, false);
            var recipe = new RecipeData { Id = "armor", Name = "Zırh plakası", Turns = 1, TurnsLine = "1 vardiya" };
            recipe.Inputs.Add(new RecipeInput { Kind = "metal", Name = "Metal", Quantity = 2, CardQty = "×2" });
            recipe.Inputs.Add(new RecipeInput { Kind = "kumas", Name = "Kumaş", Quantity = 1, CardQty = "×1" });

            var copy = rig.View.AcquireRecipeCopy(recipe);
            Assert.IsNotNull(copy, "kopya");
            Assert.AreEqual(BoardCardKind.Recipe, copy.Kind);
            Assert.AreEqual("armor", copy.Id, "kopyanın kimliği tarif");
            Assert.IsFalse(copy.gameObject.activeSelf, "kopya gizli başlar (sunucu konumlar)");
            Assert.IsFalse(copy.Collider.enabled, "kopyanın çarpıştırıcısı kapalı");
            Assert.AreSame(copy, rig.View.CardFor("armor"), "CardFor kopyayı bulur");

            // bırakma kabul edildi: sunucu pozu önceden yazdı, kopya bırakıldığı yerde (sürükleme denetleyicisi koydu)
            var dropMm = new Vector2(560, 320);
            copy.Place(BoardLayout.MmToLocal(dropMm, BoardLayout.CardZ(BoardLayout.RecipeLevel)), -4f);
            copy.SetVisible(true);
            rig.View.RecipePose = id => id == "is-1" ? new Vector3(dropMm.x, dropMm.y, -4f) : (Vector3?)null;
            s.Order = Sheet(OrderState.Recipe, "DURDU", false);
            rig.View.Render(s, true);
            Assert.AreSame(copy, rig.View.OrderRecipeCard, "kopya form kartı olur (ikinci kart açılmaz)");
            Assert.AreEqual("is-1", copy.Id);
            Assert.IsFalse(copy.Moving, "bırakıldığı yerde kalır, sıçramaz");
            NearMm(dropMm, copy, "bırakma pozu");

            // reddedilen bırakma: kopya havuza döner (form kartı olan kopyada etkisiz)
            rig.View.ReleaseRecipeCopy(copy);
            Assert.AreSame(copy, rig.View.OrderRecipeCard, "form kartı bırakılmaz");
            var other = rig.View.AcquireRecipeCopy(recipe);
            Assert.IsNotNull(other);
            Assert.AreNotSame(copy, other);
            other.SetVisible(true);
            rig.View.ReleaseRecipeCopy(other);
            Assert.IsFalse(other.gameObject.activeSelf, "dönen kopya gizlenir");
            Assert.IsNull(rig.View.CardFor("armor"), "dönen kopya artık bulunmaz");
        }

        [Test]
        public void RecipeCopy_NoSavedPose_StaysWhereDropped()
        {
            // Emrin kimliği komuttan sonra belli olur: sunucu pozu yazmadan çizim gelirse kart yine bırakıldığı yerde kalır.
            var rig = Build();
            var s = Snapshot();
            rig.View.Render(s, false);
            var copy = rig.View.AcquireRecipeCopy(new RecipeData { Id = "armor", Name = "Zırh plakası", Turns = 1 });
            var dropMm = new Vector2(580, 330);
            copy.Place(BoardLayout.MmToLocal(dropMm, BoardLayout.CardZ(BoardLayout.RecipeLevel)), 3f);
            copy.SetVisible(true);
            s.Order = Sheet(OrderState.Recipe, "DURDU", false);
            rig.View.Render(s, true);
            Assert.AreSame(copy, rig.View.OrderRecipeCard);
            NearMm(dropMm, copy, "kayıtsız kabul: bırakıldığı yer");
            Assert.AreEqual(3f, Mathf.DeltaAngle(0, copy.transform.localEulerAngles.z), 0.05f, "bırakıldığı açı");

            // bir sonraki çizimde (poz hâlâ kayıtsız) TARİF kutusunun çapasına döner: kopya bayrağı bir kezlik
            rig.View.Render(s, false);
            NearMm(BoardLayout.Recipe, copy, "kayıtsız form kartı çapada");
        }

        // ================================================================== ürünün depoya kayması

        [Test]
        public void Store_SlideIn_Token_EaseOut()
        {
            float seconds = Theme("motion.board.productToStoreS", -1f);
            Assert.AreEqual(0.3f, seconds, 1e-4f, "motion.board.productToStoreS (tema)");

            var rig = Build();
            var card = Object.Instantiate(rig.ItemTemplate, rig.Cards, false);
            spawned.Add(card.gameObject);
            var sounds = new List<BoardSound>();
            var finished = new List<BoardCardView>();
            rig.Store.Sound += sounds.Add;
            rig.Store.SlideFinished += finished.Add;
            var from = BoardLayout.MmToLocal(BoardLayout.Recipe, BoardLayout.CardZ(2));
            var to = BoardLayout.MmToLocal(BoardLayout.KitCard(3, 0), BoardLayout.CardZ(BoardLayout.KitLevel(0)));

            rig.Store.SlideIn(card, from, to, seconds);
            CollectionAssert.AreEqual(new[] { BoardSound.ProductSlide }, sounds, "başta bir kez \"şık\"");
            Assert.AreSame(card, rig.Store.Sliding);
            Assert.IsTrue(card.gameObject.activeSelf);
            Near(from, card.transform.localPosition, 1e-5f, "TARİF kutusundan başlar");

            rig.Store.Advance(seconds / 2f);
            float k = Vector3.Distance(from, card.transform.localPosition) / Vector3.Distance(from, to);
            Assert.AreEqual(CardEasing.Evaluate(CardEase.EaseOut, 0.5f), k, 1e-3f, "yarı sürede ease-out ilerleme");
            Assert.GreaterOrEqual(k, 0.6f, "ease-out: yarı sürede yolun ≥ %60'ı");
            Assert.IsEmpty(finished, "yarıda bitmedi");

            rig.Store.Advance(seconds / 2f + 0.001f);
            Near(to, card.transform.localPosition, 1e-5f, "yığının üst kart yerinde biter");
            Assert.IsNull(rig.Store.Sliding);
            CollectionAssert.AreEqual(new[] { card }, finished, "SlideFinished bir kez");
            Assert.AreEqual(1, sounds.Count, "ses tekrar etmez");
        }

        [Test]
        public void NewItem_WaitsInRecipeBox_SlidesOnFirstK4Arrival_Once()
        {
            var rig = Build();
            bool slid = false;
            var slidEvents = new List<NewItemData>();
            rig.View.NewItemPending = d => !slid;
            rig.View.NewItemSlid += d => { slid = true; slidEvents.Add(d); };
            var sounds = new List<BoardSound>();
            rig.View.Sound += sounds.Add;

            var s = WithNewItem(Snapshot());
            rig.View.Render(s, false);   // K1'den bakış / kayıttan devam: K4 dışında
            var card = rig.View.NewItemCard;
            Assert.IsNotNull(card, "yeni ürün kartı");
            Assert.AreEqual("esya-12", card.Id);
            Assert.IsTrue(rig.View.NewItemWaiting, "K4'e gelmeden TARİF kutusunda bekler");
            NearMm(BoardLayout.Recipe, card, "TARİF kutusunda");
            Assert.IsNull(rig.Store.Sliding, "K4 dışında kayış yok");

            rig.View.Render(s, true);    // başka bir değişim: hâlâ bekler
            Assert.IsTrue(rig.View.NewItemWaiting);

            rig.View.SetInteractive(true);   // ilk K4 varışı
            Assert.IsTrue(rig.View.NewItemSliding, "varışta kayış başlar");
            Assert.AreSame(card, rig.Store.Sliding);
            Assert.Contains(BoardSound.ProductSlide, sounds, "ProductSlide sesi BoardView3D.Sound'dan");

            rig.Store.Advance(Theme("motion.board.productToStoreS", 0.3f) + 0.01f);
            Assert.IsFalse(rig.View.NewItemSliding);
            Assert.AreEqual(1, slidEvents.Count, "NewItemSlid bir kez");
            Assert.AreEqual("esya-12", slidEvents[0].ItemId);
            NearMm(BoardLayout.KitCard(3, 0), card, "zırh yığınının üst kart yeri");

            // sonraki çizimler ve ikinci varış kayışı yeniden oynatmaz
            rig.View.Render(s, true);
            rig.View.SetInteractive(false);
            rig.View.SetInteractive(true);
            rig.View.Render(s, true);
            Assert.IsFalse(rig.View.NewItemWaiting || rig.View.NewItemSliding, "kayış bir kez");
            Assert.AreEqual(1, slidEvents.Count);
            NearMm(BoardLayout.KitCard(3, 0), card, "yığında kalır");

            // kayıttan devam: kayış oynamışsa (sunum durumu) kart doğrudan yığında
            var rig2 = Build();
            rig2.View.NewItemPending = d => false;
            rig2.View.Render(WithNewItem(Snapshot()), false);
            Assert.IsFalse(rig2.View.NewItemWaiting, "oynamış kayış beklemez");
            NearMm(BoardLayout.KitCard(3, 0), rig2.View.NewItemCard, "doğrudan yığında");
        }

        // ================================================================== YENİ levhası

        [Test]
        public void NewTag_OnlyOnNewItem_FallsOnTouch()
        {
            var rig = Build();
            bool touched = false;
            var touchEvents = new List<NewItemData>();
            rig.View.NewItemPending = d => false;
            rig.View.NewItemTagged = d => !touched;
            rig.View.NewItemTouched += d => { touched = true; touchEvents.Add(d); };

            var s = WithNewItem(Snapshot());
            rig.View.Render(s, false);
            var card = rig.View.NewItemCard;
            var old = rig.View.CardFor("esya-3");
            Assert.IsNotNull(card);
            Assert.IsNotNull(old, "aynı yığında eski kart");
            Assert.IsTrue(rig.Tag.Shown, "yeni üründe YENİ");
            Assert.AreSame(card.transform, rig.Tag.Target, "levha yalnız yeni kartta");
            Assert.AreEqual("YENİ", rig.Tag.Word.text, "sözcük Loc.T(\"YENİ\")");

            rig.View.SetInteractive(true);
            old.OnPointerEnter(null);
            Assert.IsTrue(rig.Tag.Shown, "başka karta dokunmak etiketi düşürmez");
            Assert.IsEmpty(touchEvents);

            card.OnPointerEnter(null);   // ilk dokunuş: işaretçi girişi (D9-A)
            Assert.AreEqual(1, touchEvents.Count, "NewItemTouched bir kez");
            Assert.IsFalse(rig.Tag.Shown);
            Assert.IsTrue(rig.Tag.Fading, "0,15 s'de söner");
            rig.Tag.Advance(Theme("motion.board.itemTuckS", 0.15f) + 0.01f);
            Assert.IsFalse(rig.Tag.gameObject.activeSelf, "söndükten sonra gizli");
            card.OnPointerClick(null);
            Assert.AreEqual(1, touchEvents.Count, "ikinci dokunuş olay yaymaz");

            rig.View.Render(s, true);
            Assert.IsFalse(rig.Tag.gameObject.activeSelf, "dokunulmuş ürüne levha dönmez");

            // yeni ürün yoksa levha yok
            var rig2 = Build();
            rig2.View.Render(Snapshot(), false);
            Assert.IsFalse(rig2.Tag.gameObject.activeSelf, "yeni ürün yokken levha yok");
            Assert.IsNull(rig2.View.NewItemCard);
        }

        [Test]
        public void NewTag_Size_TrEn_Fits()
        {
            var rig = Build();
            var size = new Vector2(Theme("board.newTag.sizeMm[0]", 32f), Theme("board.newTag.sizeMm[1]", 13f));
            Assert.AreEqual(new Vector2(32f, 13f), size, "board.newTag.sizeMm (D6-A: 32 × 13)");
            Assert.AreEqual(9.4f, Theme("board.newTag.capMm", -1f), 1e-4f, "board.newTag.capMm");
            var tagColour = ThemeColour("board.newTag.color", Color.magenta);
            Assert.Less(Mathf.Abs(tagColour.r - 0xB4 / 255f) + Mathf.Abs(tagColour.g - 0x29 / 255f) + Mathf.Abs(tagColour.b - 0x1C / 255f), 0.01f,
                "levha rengi #B4291C: " + tagColour);

            rig.Tag.ApplyTheme();
            var rt = (RectTransform)rig.Tag.transform;
            Assert.AreEqual(size, rt.sizeDelta, "levha boyutu temadan");
            Assert.AreEqual(9.4f, rig.Tag.Word.fontSize, 1e-4f, "em 9,4 mm (≥ 14 px K4'te)");
            Assert.IsFalse(rig.Tag.Word.enableAutoSizing, "otomatik punto yok");
            Assert.AreEqual(ThemeColour("board.newTag.color", Color.magenta), rig.Tag.Background.color, "zemin rengi");
            Assert.AreEqual(ThemeColour("board.newTag.ink", Color.magenta), rig.Tag.Word.color, "yazı rengi");

            // yer: sol kenar kart merkezinin 8 mm sağında, alt kenar kartın üst kenarına 3 mm biner, yüz tuvalinin önünde
            var off = rig.Tag.OffsetLocal * 1000f;
            Assert.AreEqual(8f + size.x / 2f, off.x, 0.01f, "levha merkezi x");
            Assert.AreEqual(Theme("cards.sizesMm.item[1]", 50f) / 2f + size.y / 2f - 3f, off.y, 0.01f, "levha merkezi y");
            Assert.Less(off.z, -1f, "kartın yüz tuvalinin (z −1 mm) önünde");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CP-Bold SDF.asset");
            if (font == null) Assert.Ignore("CP-Bold SDF yok");
            rig.Tag.Word.font = font;
            rig.Tag.Word.fontSharedMaterial = font.material;
            foreach (var word in new[] { "YENİ", "NEW" })
            {
                rig.Tag.SetText(word);
                // Test düzeneğinde sözcüğün tuvali yok, örgü kurulmaz (lineCount 0): tek satır = levhaya sığan genişlik (aşağıda)
                Assert.LessOrEqual(rig.Tag.Word.preferredWidth, size.x - 2f + 0.01f, word + ": levhaya 1 mm payla sığar");
                Assert.LessOrEqual(rig.Tag.Word.preferredHeight, size.y + 0.01f, word + ": yükseklik");
            }
        }

        // ================================================================== defter kipi (§1.12)

        [Test]
        public void BookMode_DisablesBoardColliders_KeepsFaceBackSignPages()
        {
            var rig = Build();
            var s = Snapshot();
            s.Order = Sheet(OrderState.Recipe, "DURDU", false);
            rig.View.Render(s, false);
            rig.View.SetInteractive(true);

            Assert.IsFalse(rig.Entry.enabled, "K4'te Pano erişimi kapalı");
            Assert.IsTrue(rig.Face.enabled && rig.Back.enabled && rig.Tray.enabled, "K4'te sabit hedefler açık");
            var material = rig.View.CardFor("metal");
            var recipe = rig.View.OrderRecipeCard;
            Assert.IsTrue(material.Collider.enabled, "stok kartı tıklanır");
            Assert.IsTrue(recipe.Collider.enabled, "form kartı tıklanır");

            rig.View.SetBookMode(true);
            Assert.IsTrue(rig.View.BookMode);
            Assert.IsTrue(rig.Face.enabled, "Pano yüzü açık kalır (defter dışına tık ona düşer)");
            Assert.IsTrue(rig.Back.enabled, "MASAYA DÖN açık kalır");
            Assert.IsFalse(rig.Tray.enabled, "deste tepsisi kapalı");
            Assert.IsFalse(material.Collider.enabled, "kartlar kapalı");
            Assert.IsFalse(recipe.Collider.enabled, "form kartı kapalı");
            Assert.IsFalse(rig.Entry.enabled, "Pano erişimi kapalı kalır");
            Assert.IsTrue(rig.PageLeft.enabled && rig.PageRight.enabled, "defterin sayfa çarpıştırıcılarına dokunulmaz");

            rig.View.Render(s, true);
            Assert.IsFalse(material.Collider.enabled, "çizim defter kipini bozmaz");

            rig.View.SetBookMode(false);
            Assert.IsTrue(rig.Tray.enabled && material.Collider.enabled && recipe.Collider.enabled, "defter kapanınca geri açılır");

            rig.View.SetBookMode(true);
            rig.View.SetInteractive(false);
            Assert.IsFalse(rig.View.BookMode, "K4'ten çıkış defter kipini bitirir");
            Assert.IsTrue(rig.Entry.enabled, "uzakta Pano erişimi açık");
            rig.View.SetInteractive(true);
            Assert.IsTrue(rig.Tray.enabled && material.Collider.enabled, "dönüşte defter kipi kapalı başlar");
        }

        // ================================================================== kurulum

        sealed class Rig
        {
            public BoardView3D View;
            public Transform Cards, RecipeHome;
            public WorkOrderView Order;
            public StoreView Store;
            public NewItemTag Tag;
            public TMP_Text Number, Status, Stamp, DropHint;
            public GameObject StampFrame;
            public BoardCardView ItemTemplate;
            public BoxCollider Entry, Face, Back, Tray, PageLeft, PageRight;
        }

        Rig Build()
        {
            var rig = new Rig();
            var root = Track(new GameObject("Planning board"));
            rig.View = root.AddComponent<BoardView3D>();
            rig.Cards = Child(root.transform, "Kartlar");

            // iş emri formu
            var order = Child(root.transform, "İş emri");
            rig.Order = order.gameObject.AddComponent<WorkOrderView>();
            rig.Number = Text3D(order, "Numara");
            rig.Status = Text3D(order, "Durum");
            rig.Stamp = Text3D(order, "Damga");
            rig.DropHint = Text3D(order, "Girdi ipucu");
            rig.StampFrame = Child(order, "Damga çerçevesi").gameObject;
            rig.StampFrame.SetActive(false);
            rig.RecipeHome = Child(rig.Cards, "Order_Recipe");
            rig.RecipeHome.localPosition = BoardLayout.MmToLocal(BoardLayout.Recipe, 0);
            Set(rig.Order, ("numberText", rig.Number), ("recipeText", rig.Status), ("stampText", rig.Stamp),
                ("stampFrame", rig.StampFrame), ("recipeHome", rig.RecipeHome), ("dropHintText", rig.DropHint));

            // depo ve YENİ levhası (sahnedeki gibi başta pasif)
            var store = Child(root.transform, "Depo");
            rig.Store = store.gameObject.AddComponent<StoreView>();
            rig.Tag = BuildTag(root.transform);
            Set(rig.Store, ("newTag", rig.Tag));

            // sabit hedefler
            rig.Entry = Box(root.transform, "Pano erişimi");
            rig.Face = Box(root.transform, "Pano yüzü");
            rig.Back = Box(root.transform, "BackSign");
            rig.Back.gameObject.AddComponent<DeskTarget>();
            rig.Tray = Box(root.transform, "Deste tepsisi");
            rig.PageLeft = Box(root.transform, "Sayfa L çarpıştırıcı");
            rig.PageRight = Box(root.transform, "Sayfa R çarpıştırıcı");

            // kart şablonları (sahne dışında, pasif)
            var recipeT = Template(BoardCardKind.Recipe, new Vector2(112, 80));
            rig.ItemTemplate = Template(BoardCardKind.Item, new Vector2(80, 50));
            var materialT = Template(BoardCardKind.Material, new Vector2(80, 56));

            var so = new SerializedObject(rig.View);
            so.FindProperty("cardRoot").objectReferenceValue = rig.Cards;
            so.FindProperty("recipePrefab").objectReferenceValue = recipeT;
            so.FindProperty("itemPrefab").objectReferenceValue = rig.ItemTemplate;
            so.FindProperty("materialPrefab").objectReferenceValue = materialT;
            so.FindProperty("order").objectReferenceValue = rig.Order;
            so.FindProperty("store").objectReferenceValue = rig.Store;
            so.FindProperty("boardEntry").objectReferenceValue = rig.Entry;
            so.FindProperty("boardFace").objectReferenceValue = rig.Face;
            so.FindProperty("backTarget").objectReferenceValue = rig.Back.GetComponent<DeskTarget>();
            var statics = so.FindProperty("staticColliders");
            var list = new Collider[] { rig.Face, rig.Back, rig.Tray };
            statics.arraySize = list.Length;
            for (int i = 0; i < list.Length; i++) statics.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return rig;
        }

        NewItemTag BuildTag(Transform parent)
        {
            var go = new GameObject("YENİ etiketi", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.localScale = Vector3.one * 0.001f;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var group = go.AddComponent<CanvasGroup>();
            var back = Stretch(rt, "Zemin").gameObject.AddComponent<Image>();
            back.raycastTarget = false;
            var word = Stretch(rt, "Sözcük").gameObject.AddComponent<TextMeshProUGUI>();
            word.raycastTarget = false;
            word.horizontalAlignment = HorizontalAlignmentOptions.Center;
            word.verticalAlignment = VerticalAlignmentOptions.Middle;
            word.textWrappingMode = TextWrappingModes.NoWrap;
            var tag = go.AddComponent<NewItemTag>();
            Set(tag, ("background", back), ("word", word), ("group", group));
            go.SetActive(false);
            return tag;
        }

        static RectTransform Stretch(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        BoardCardView Template(BoardCardKind kind, Vector2 mm)
        {
            var go = Track(new GameObject(kind + "Card"));
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(mm.x / 1000f, mm.y / 1000f, 0.004f);
            box.center = new Vector3(0, 0, -0.002f);
            var view = go.AddComponent<BoardCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("kind").intValue = (int)kind;
            so.FindProperty("hit").objectReferenceValue = box;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);
            return view;
        }

        /// <summary>Boş pano: dört hammadde, altı boş yığın, boş form.</summary>
        static BoardSnapshot Snapshot()
        {
            var s = new BoardSnapshot { Editable = true, RecipesDraggable = true };
            for (int i = 0; i < 4; i++)
                s.Materials[i] = new MaterialCardData { Kind = BoardLayout.MaterialKinds[i], Name = BoardLayout.MaterialKinds[i], Stock = 3, Free = 3 };
            for (int k = 0; k < 6; k++) s.Kits[k] = new KitStackData { Kind = BoardLayout.KitKinds[k], CountText = "×0" };
            s.Order = Sheet(OrderState.Empty, "DURDU", false);
            return s;
        }

        /// <summary>Zırh yığınında eski bir kart (esya-3) ve son kapanışın yeni ürünü (esya-12, üstte).</summary>
        static BoardSnapshot WithNewItem(BoardSnapshot s)
        {
            foreach (var id in new[] { "esya-3", "esya-12" })
                s.Items.Add(new ItemCardData { Id = id, Kind = "armor", Name = "Zırh plakası", Slot = ItemSlotKind.Armor });
            var kit = s.Kits[3];
            kit.TopIds.Add("esya-3");
            kit.TopIds.Add("esya-12");
            kit.Count = 2;
            kit.CountText = "×2";
            s.NewItem = new NewItemData { ItemId = "esya-12", Kind = "armor", Stack = 3, Day = 1 };
            return s;
        }

        /// <summary>Kurucunun (W4) her durum için verdiği form verisinin biçimi.</summary>
        static OrderData Sheet(OrderState state, string stampWord, bool english)
        {
            var d = new OrderData { State = state, Number = "No 031" };
            if (state == OrderState.Empty) return d;
            d.OrderId = "is-1";
            d.RecipeId = "armor";
            d.Turns = 1;
            d.Card = new RecipeCardData { Id = "is-1", RecipeId = "armor", Name = english ? "Armour plate" : "Zırh plakası", IconKey = "armor", Turns = 1 };
            switch (state)
            {
                case OrderState.Recipe: d.Status = english ? "missing: 2 metal + 1 cloth" : "eksik: 2 metal + 1 kumaş"; break;
                case OrderState.Missing: d.Status = english ? "missing: 1 cloth" : "eksik: 1 kumaş"; break;
                case OrderState.Full: d.Status = english ? "armour plate · inputs held · maker: Suna" : "zırh plakası · girdiler ayrıldı · üretici: Suna"; break;
                case OrderState.Started:
                    d.Started = true; d.Turns = 2; d.Done = 1; d.RecipeId = "emp_cell";
                    d.Card = new RecipeCardData { Id = "is-1", RecipeId = "emp_cell", Name = "EMP hücresi", IconKey = "emp_cell", Turns = 2, Done = 1 };
                    d.Status = english ? "EMP cell · inputs paid · 1/2 · maker: Suna" : "emp hücresi · girdiler ödendi · 1/2 · üretici: Suna";
                    break;
                case OrderState.Stopped:
                    d.Stamp = stampWord;
                    d.StopReason = english ? "starvation" : "açlık";
                    d.Status = (english ? "armour plate · " : "zırh plakası · ") + d.StopReason;
                    break;
            }
            return d;
        }

        // ------------------------------------------------------------------ yardımcılar

        GameObject Track(GameObject go)
        {
            spawned.Add(go);
            return go;
        }

        static Transform Child(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static BoxCollider Box(Transform parent, string name) => Child(parent, name).gameObject.AddComponent<BoxCollider>();

        static TMP_Text Text3D(Transform parent, string name) => Child(parent, name).gameObject.AddComponent<TextMeshPro>();

        static void Set(Object owner, params (string field, Object value)[] values)
        {
            var so = new SerializedObject(owner);
            foreach (var (field, value) in values)
            {
                var p = so.FindProperty(field);
                Assert.IsNotNull(p, owner.GetType().Name + "." + field);
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static float Theme(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        static Color ThemeColour(string key, Color fallback)
        {
            var t = SiginakTheme.Active;
            return t != null && t.TryGet(key, out var c) ? c : fallback;
        }

        static void NearMm(Vector2 expected, BoardCardView card, string what)
        {
            Assert.IsNotNull(card, what + ": kart");
            var mm = BoardLayout.LocalToMm(card.transform.localPosition);
            Assert.IsTrue(Mathf.Abs(mm.x - expected.x) <= MmTol && Mathf.Abs(mm.y - expected.y) <= MmTol,
                what + ": beklenen " + expected.ToString("F2") + " mm, olan " + mm.ToString("F2"));
        }

        static void Near(Vector3 expected, Vector3 actual, float tol, string what) =>
            Assert.IsTrue(Vector3.Distance(expected, actual) <= tol, what + ": beklenen " + expected.ToString("F5") + ", olan " + actual.ToString("F5"));
    }
}
