using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tek kâğıt akışı (teleks ve devir kâğıdı birleşti; <see cref="PlatenFeed"/>, <see cref="Teleprinter"/>): yeni kâğıdın satır satır
    /// yükselişinin saf hesabı (satır geri alması, ease-out kayış) ve silindir devir fişindeyken teleksin davranışı (kâğıt yok, basım
    /// yok, okunmaz; satır şeride düşer, kayda girer). Yalın düzenekler; sahne açılmaz. Edit modda Update çalışmaz, animasyon
    /// PlayMode testlerinde (<c>TelexSheetPlayTests</c>).
    /// </summary>
    public sealed class TelexPlatenTests
    {
        readonly List<Object> created = new List<Object>();

        [TearDown]
        public void Clean()
        {
            foreach (var o in created)
            {
                if (o is Camera c && c != null) c.targetTexture = null;
            }
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        // ------------------------------------------------------------------ saf

        /// <summary>
        /// Satır geri alması: 0 harfte ilk satırın değeri; satır başlarında bir sonraki satıra adım; harf sayısıyla artmaz (sırasız
        /// değer verilse de); satır bitince (<c>typed ≥ length</c>) dinlenme (0 ya da verilen son değer); negatif değer 0'a kırpılır.
        /// </summary>
        [Test]
        public void PlatenFeed_RowRetract_MonotoneToRest()
        {
            var starts = new[] { 0, 8, 20, 45 };
            var rows = new[] { 0.15f, 0.125f, 0.10f, 0.08f };
            int length = 70;
            Assert.AreEqual(0.15f, PlatenFeed.RowRetract(0, length, starts, rows), 1e-6f, "0 harf: ilk satır");
            Assert.AreEqual(0.15f, PlatenFeed.RowRetract(7, length, starts, rows), 1e-6f, "kod satırı sürüyor");
            Assert.AreEqual(0.125f, PlatenFeed.RowRetract(8, length, starts, rows), 1e-6f, "durumun ilk harfi: adım");
            Assert.AreEqual(0.10f, PlatenFeed.RowRetract(20, length, starts, rows), 1e-6f);
            Assert.AreEqual(0.08f, PlatenFeed.RowRetract(69, length, starts, rows), 1e-6f, "son satır");
            Assert.AreEqual(0f, PlatenFeed.RowRetract(70, length, starts, rows), 1e-6f, "satır bitti: dinlenme");
            Assert.AreEqual(0.02f, PlatenFeed.RowRetract(70, length, starts, rows, 0.02f), 1e-6f, "bitişte verilen değer");
            float last = float.MaxValue;
            for (int n = 0; n <= length; n++)
            {
                float r = PlatenFeed.RowRetract(n, length, starts, rows);
                Assert.LessOrEqual(r, last + 1e-6f, "harf " + n + ": artmaz");
                Assert.GreaterOrEqual(r, 0f);
                last = r;
            }
            Assert.AreEqual(0.10f, PlatenFeed.RowRetract(30, length, starts, new[] { 0.15f, 0.10f, 0.12f, 0.08f }), 1e-6f, "sırasız değer: önceki adımı geçmez");
            Assert.AreEqual(0f, PlatenFeed.RowRetract(0, length, starts, new[] { -0.01f, -0.02f, 0f, 0f }), 1e-6f, "negatif → 0");
            Assert.AreEqual(0f, PlatenFeed.RowRetract(3, length, new int[0], new float[0]), 1e-6f, "ölçü yok: dinlenme");
        }

        /// <summary>Satır adımı kayışı: başta çıkış değeri, <c>slideS</c> sonunda hedef; ease-out (yarı sürede yolun yarısından çok); süre 0 ise hedef.</summary>
        [Test]
        public void PlatenFeed_Ease_StepsWithinSlide()
        {
            Assert.AreEqual(0.15f, PlatenFeed.Ease(0.15f, 0.12f, 0, 0.06f), 1e-6f);
            Assert.AreEqual(0.12f, PlatenFeed.Ease(0.15f, 0.12f, 0.06f, 0.06f), 1e-6f, "kayış sonunda hedef");
            Assert.AreEqual(0.12f, PlatenFeed.Ease(0.15f, 0.12f, 1f, 0.06f), 1e-6f);
            float mid = PlatenFeed.Ease(0.15f, 0.12f, 0.03f, 0.06f);
            Assert.Less(mid, 0.135f, "ease-out: yarı sürede yolun yarısından çok");
            Assert.Greater(mid, 0.12f);
            Assert.AreEqual(0.12f, PlatenFeed.Ease(0.15f, 0.12f, 0, 0), 1e-6f, "süre 0: anında");
            Assert.AreEqual(CardEasing.Evaluate(CardEase.EaseOut, 0.25f), PlatenFeed.Ease(0, 1, 0.25f, 1), 1e-6f, "fişin satır adımıyla aynı eğri");
            Assert.AreEqual(PlatenFeed.TypedFeed(0.3f, 7, 0.35f), ShiftReceiptPaper.TypedFeed(0.3f, 7, 0.35f), 1e-6f, "fişin çıkışı ortak hesapta");
        }

        // ------------------------------------------------------------------ silindir fişte

        sealed class Rig
        {
            public Teleprinter printer;
            public TelexFeed feed;
            public TelexStrip strip;
            public Camera cam;
            public Transform sheet;
        }

        static void Wire(Object owner, string field, Object value)
        {
            var so = new SerializedObject(owner);
            var p = so.FindProperty(field);
            Assert.IsNotNull(p, field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        TextMeshPro Slot(Transform parent, string name, float y)
        {
            var t = new GameObject(name, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            t.transform.SetParent(parent, false);
            t.transform.localPosition = new Vector3(0, y, 0);
            t.rectTransform.sizeDelta = new Vector2(0.4f, 0.06f);
            t.alignment = TextAlignmentOptions.TopLeft;
            t.fontSize = 0.5f;
            return t;
        }

        /// <summary>Teleprinter + TelexPaper benzeri kâğıt (quad, dört yuva, yazım çizgisi), yakın kamera (1920 × 1080) ve şerit.</summary>
        Rig Build()
        {
            var r = new Rig();
            var go = new GameObject("Teleprinter");
            created.Add(go);
            var sheet = new GameObject("TelexPaper").transform;
            sheet.SetParent(go.transform, false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "Body";
            quad.transform.SetParent(sheet, false);
            quad.transform.localScale = new Vector3(0.26f, 0.225f, 1);
            var code = Slot(sheet, "TXT_Code", 0.09f);
            var status = Slot(sheet, "TXT_Status", 0.05f);
            var message = Slot(sheet, "TXT_Message", 0.0f);
            var prev = Slot(sheet, "TXT_Prev", -0.05f);
            var line = new GameObject("Print line").transform;
            line.SetParent(go.transform, false);
            line.localPosition = new Vector3(0, -0.06f, 0);
            var targetGo = new GameObject("Paper target", typeof(BoxCollider));
            targetGo.transform.SetParent(go.transform, false);
            r.printer = go.AddComponent<Teleprinter>();
            Wire(r.printer, "paperRenderer", quad.GetComponent<MeshRenderer>());
            Wire(r.printer, "frontCode", code);
            Wire(r.printer, "frontStatus", status);
            Wire(r.printer, "frontMessage", message);
            Wire(r.printer, "frontPrev", prev);
            Wire(r.printer, "sheet", sheet);
            Wire(r.printer, "sheetTarget", targetGo.GetComponent<BoxCollider>());
            Wire(r.printer, "printLine", line);
            r.sheet = sheet;

            var camGo = new GameObject("Kamera");
            created.Add(camGo);
            var texture = new RenderTexture(1920, 1080, 0);
            created.Add(texture);
            r.cam = camGo.AddComponent<Camera>();
            r.cam.enabled = false;
            r.cam.targetTexture = texture;
            r.cam.fieldOfView = 40;
            r.cam.transform.SetPositionAndRotation(new Vector3(0, 0.05f, -0.3f), Quaternion.identity);
            created.Add(r.cam);

            var stripGo = new GameObject("Telex strip");
            created.Add(stripGo);
            r.strip = stripGo.AddComponent<TelexStrip>();
            r.feed = go.AddComponent<TelexFeed>();
            r.feed.Configure(r.printer, r.strip, r.cam);
            return r;
        }

        static int StripCount(TelexStrip strip) =>
            ((Queue<TelexLine>)typeof(TelexStrip).GetField("queue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(strip)).Count;

        static List<TelexLine> Lines(params string[] statuses)
        {
            var l = new List<TelexLine>();
            for (int i = 0; i < statuses.Length; i++) l.Add(new TelexLine { code = "001/0" + i, status = statuses[i], message = "İleti " + i + "." });
            return l;
        }

        /// <summary>
        /// Silindir fişteyken (<see cref="Teleprinter.PlatenBusy"/>): teleks kâğıdı silindirde yok (nesnesi ve hedefi kapalı), satır
        /// basılmaz (<see cref="Teleprinter.Typing"/> false; fişin beslemesi beklemez), kâğıt okunmaz ve görünmez; yuvalar yine yazılır
        /// (satır kaydedilir). Fiş kalkıp yeniden gösterilince kâğıt dinlenme pozunda geri gelir.
        /// </summary>
        [Test]
        public void Teleprinter_ShowWhileBusy_NoTyping_NotReadable()
        {
            var r = Build();
            var lines = Lines("HAT AÇIK", "ÖN KONTROL");
            r.printer.Show(lines, false);
            Assert.IsTrue(r.printer.SheetPresent, "fiş yokken kâğıt silindirde");
            Assert.IsTrue(r.printer.IsReadableFrom(r.cam, 14), "fiş yokken yakından okunur (düzenek doğru)");

            bool busy = true;
            r.printer.PlatenBusy = () => busy;
            r.printer.Show(lines, false);
            Assert.IsFalse(r.printer.SheetPresent, "fiş silindirde: teleks kâğıdı yok");
            Assert.IsFalse(r.sheet.gameObject.activeSelf, "kâğıdın nesnesi kapalı");
            Assert.IsFalse(((Collider)new SerializedObject(r.printer).FindProperty("sheetTarget").objectReferenceValue).enabled, "kâğıdın hedefi kapalı");
            Assert.IsFalse(r.printer.Typing, "basım yok (fişin beslemesi beklemez)");
            Assert.IsFalse(r.printer.IsReadableFrom(r.cam, 14), "okunmaz (satır şeride düşer)");
            Assert.IsFalse(r.printer.IsVisibleFrom(r.cam), "görünmez");
            StringAssert.Contains("ÖN KONTROL", ((TMP_Text)new SerializedObject(r.printer).FindProperty("frontStatus").objectReferenceValue).text, "yuvalar yine yazılır");
            Assert.IsTrue(r.printer.Settled, "kâğıt yok: oturmuş sayılır");

            busy = false;
            r.printer.Show(lines, false);
            Assert.IsTrue(r.printer.SheetPresent, "fiş kalkınca kâğıt dinlenme pozunda geri gelir");
            Assert.IsTrue(r.sheet.gameObject.activeSelf);
            Assert.AreEqual(0f, r.printer.SheetRetractM, 1e-6f, "dinlenme pozu");
            Assert.IsTrue(r.printer.IsReadableFrom(r.cam, 14));
        }

        static void ExpireInstantWindow(Teleprinter printer) =>
            typeof(Teleprinter).GetField("instantUntilFrame", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(printer, int.MinValue);

        /// <summary>
        /// Silindirin eşitlenmesi (<see cref="Teleprinter.SyncPlaten"/>; Play'de Update her kare, köprü geri yüklemeden sonra
        /// <c>instant</c>): "anında" penceresi dışında fiş silindiri bırakınca silindir boş kalır (kâğıt geri gelmez; sonraki satır yeni
        /// kâğıda basılır); anında eşitlemede kâğıt son satırla dinlenme pozunda geri gelir; fiş tutarken anında eşitleme kâğıdı gizler
        /// (Edit modda düşüş yok). Edit modda <c>Time.frameCount</c> ilerlemediği için pencere yansımayla kapatılır.
        /// </summary>
        [Test]
        public void Teleprinter_SyncPlaten_OutsideInstantWindow_LeavesPlatenEmpty()
        {
            var r = Build();
            var lines = Lines("HAT AÇIK", "ÖN KONTROL");
            r.printer.Show(lines, false);
            Assert.IsTrue(r.printer.SheetPresent);
            bool busy = false;
            r.printer.PlatenBusy = () => busy;
            ExpireInstantWindow(r.printer);

            busy = true;
            r.printer.SyncPlaten(false);
            Assert.IsFalse(r.printer.SheetPresent, "fiş silindiri tuttu: kâğıt yok");
            Assert.IsFalse(r.printer.Dropping, "Edit modda düşüş yok");

            busy = false;
            r.printer.SyncPlaten(false);
            Assert.IsFalse(r.printer.SheetPresent, "pencere dışında fiş bırakınca silindir boş kalır");
            Assert.IsFalse(r.sheet.gameObject.activeSelf);

            busy = true;
            r.printer.SyncPlaten(false);
            busy = false;
            r.printer.SyncPlaten(true);
            Assert.IsTrue(r.printer.SheetPresent, "anında eşitlemede kâğıt geri gelir (kayıttan devam, fiş yok)");
            Assert.AreEqual(0f, r.printer.SheetRetractM, 1e-6f, "dinlenme pozunda");
            Assert.IsTrue(r.printer.Settled);
            StringAssert.Contains("ÖN KONTROL", ((TMP_Text)new SerializedObject(r.printer).FindProperty("frontStatus").objectReferenceValue).text, "son satırla");

            ExpireInstantWindow(r.printer);
            busy = true;
            r.printer.SyncPlaten(true);
            Assert.IsFalse(r.printer.SheetPresent, "fiş tutarken anında eşitleme kâğıdı gizler");
            Assert.IsFalse(r.printer.Dropping);
        }

        /// <summary>
        /// İleti yuvası basım boyunca tam metni taşır, harfler <c>maxVisibleCharacters</c> ile açılır: sarma basım boyunca tam metninki
        /// (satır başları ölçülenle aynı; yarım kelime üst satırda basılıp aşağı atlamaz). Tam basılınca bütün harfler görünür.
        /// </summary>
        [Test]
        public void Teleprinter_MessageSlot_FullTextWithVisibleCount()
        {
            var r = Build();
            var message = (TMP_Text)new SerializedObject(r.printer).FindProperty("frontMessage").objectReferenceValue;
            var lines = new List<TelexLine> { new TelexLine { code = "001/01", status = "DENEME", message = "Uzun bir ileti satırı sarılır." } };
            r.printer.Show(lines, false);
            Assert.AreEqual("Uzun bir ileti satırı sarılır.", message.text);
            Assert.GreaterOrEqual(message.maxVisibleCharacters, message.text.Length, "tam basılı: bütün harfler görünür");
            var render = typeof(Teleprinter).GetMethod("Render", BindingFlags.Instance | BindingFlags.NonPublic);
            int messageStart = ("001/01  DENEME  ").Length;
            render.Invoke(r.printer, new object[] { messageStart + 4 });
            Assert.AreEqual("Uzun bir ileti satırı sarılır.", message.text, "basım sürerken yuvada tam metin");
            Assert.AreEqual(4, message.maxVisibleCharacters, "ilk 4 harf görünür");
            render.Invoke(r.printer, new object[] { messageStart });
            Assert.AreEqual(0, message.maxVisibleCharacters, "ileti henüz basılmadı");
        }

        /// <summary>
        /// Fiş silindirdeyken gelen satır: kayda girer (<see cref="TelexFeed.Lines"/>, kod sayacı ilerler), kâğıda basılmaz ve ekran
        /// şeridine düşer; silindir boşalınca (okunur kâğıtta) satır şeride düşmez.
        /// </summary>
        [Test]
        public void TelexFeed_PostWhileBusy_GoesToStrip()
        {
            var r = Build();
            r.feed.Post(1, "HAT AÇIK", "Analog hat açık.", TelexKind.Info, header: true);
            Assert.AreEqual(0, StripCount(r.strip), "okunur kâğıtta şerit yok");

            bool busy = true;
            r.feed.PlatenBusy = () => busy;
            Assert.IsNotNull(r.printer.PlatenBusy, "TelexFeed.PlatenBusy Teleprinter'a iletilir");
            var a = r.feed.Post(1, "YAPILAMADI", "Kapı kapalı.", TelexKind.Failed);
            Assert.AreEqual("001/01", a.code);
            Assert.AreEqual(1, StripCount(r.strip), "fiş silindirde: satır şeritte");
            Assert.AreEqual(2, r.feed.Lines.Count, "satır kayda girdi");
            Assert.IsFalse(r.feed.Typing, "basım yok");
            Assert.IsFalse(r.printer.SheetPresent);
            var b = r.feed.Post(1, "DİL", "Türkçe.", TelexKind.Info);
            Assert.AreEqual("001/02", b.code, "kod sayacı ilerler");
            Assert.AreEqual(2, StripCount(r.strip));

            busy = false;
            r.feed.Post(1, "VARDİYA DEVREDİLDİ", "Yeni vardiya.", TelexKind.Done);
            Assert.AreEqual(2, StripCount(r.strip), "silindir boşken okunur: şeride düşmez");
            Assert.AreEqual(4, r.feed.Lines.Count);
        }
    }
}
