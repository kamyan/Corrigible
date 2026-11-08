using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Where = Corrigible.Desk.ShiftReceiptPaper.Where;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 4 (R19b, PLAN §1.5–§1.7, paket W2): devir fişinin hareketi ve EVRAK hedefi. Sahne açılmaz; şalter, fiş
    /// ağzı ve EVRAK tepsisi DeskGame'deki yerlerinde (şalter (0,765; 0,76; 0,885) yaw 10, tepsi (0,765; 0,76; 1,30) yaw −4, çapalar
    /// SlipMouth manifestinden) boş nesnelerle kurulur; kamera K6 token'ından (1920 × 1080). Zaman <c>Advance</c> ile ilerler
    /// (Edit modda <c>Update</c> ve <c>OnEnable</c> çalışmaz), süreler <c>SiginakTheme</c>'den okunur.
    /// </summary>
    public class Rev3SlipMotionTests
    {
        const string Id = "vardiya-0";

        readonly List<Object> created = new List<Object>();
        readonly List<Camera> cameras = new List<Camera>();

        [TearDown]
        public void Clean()
        {
            foreach (var c in cameras) if (c != null) c.targetTexture = null;
            cameras.Clear();
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        // ------------------------------------------------------------------ kurulum

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

        GameObject Make(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            else created.Add(go);
            return go;
        }

        sealed class Bench
        {
            public Transform switchRoot, exit, start, over, sheet;
            public EvrakTray tray;
            public BoxCollider trayBox;
            public Light light;
            public Camera cam;
            public ShiftReceiptPaper paper;
            public BoxCollider hit;
            public ShiftReceiptMachine machine;
            public ShiftSwitch shiftSwitch;
            public PartDriver lamp;
            public readonly List<ShiftSound> sounds = new List<ShiftSound>();
            public readonly List<string> filed = new List<string>();
            public readonly List<bool> drags = new List<bool>();

            public Vector2 TrayScreen => cam.WorldToScreenPoint(tray.transform.TransformPoint(0, 0.045f, 0));
            /// <summary>Kadrajın sol altı: tepsinin dışında, sürükleme düzlemiyle kesişir.</summary>
            public Vector2 Outside => new Vector2(60, 60);
        }

        Camera K6Camera()
        {
            var go = Make("K6 kamera");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            var texture = new RenderTexture(1920, 1080, 0);
            created.Add(texture);
            cameras.Add(cam);
            cam.targetTexture = texture;
            cam.aspect = 16f / 9f;
            cam.nearClipPlane = 0.03f;
            cam.fieldOfView = Token("cameras.K6.fovV");
            var pos = Theme.Vector("cameras.K6.pos");
            var target = Theme.Vector("cameras.K6.target");
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(target - pos, Vector3.up));
            return cam;
        }

        Material Mat(string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
            var m = new Material(shader) { name = name };
            created.Add(m);
            return m;
        }

        /// <summary>
        /// Şalter + fiş ağzı çapaları, EVRAK tepsisi (çarpıştırıcı, üst kâğıt, <c>Slip over</c>, hedef ışığı), K6 kamerası, fiş ve
        /// (istenirse) makine ile lambası. <paramref name="withSwitch"/>: makine gerçek bir <see cref="ShiftSwitch"/>'e bağlı
        /// (kol inişi beklenir); değilse besleme hemen başlar.
        /// </summary>
        Bench Build(bool withMachine = true, bool withSwitch = false)
        {
            var b = new Bench();
            var sw = Make("Shift switch");
            sw.transform.SetPositionAndRotation(new Vector3(0.765f, 0.76f, 0.885f), Quaternion.Euler(0, 10, 0));
            b.switchRoot = sw.transform;
            b.exit = Make("SlipExit", sw.transform).transform;
            b.exit.localPosition = new Vector3(-0.30f, 0.14f, 0.015f);
            b.start = Make("SlipStart", sw.transform).transform;
            b.start.localPosition = new Vector3(-0.18f, 0.14f, 0.015f);

            var trayGo = Make("Paper tray");
            trayGo.transform.SetPositionAndRotation(new Vector3(0.765f, 0.76f, 1.30f), Quaternion.Euler(0, -4, 0));
            b.trayBox = trayGo.AddComponent<BoxCollider>();
            b.trayBox.center = new Vector3(0, 0.045f, 0);
            b.trayBox.size = new Vector3(0.33f, 0.09f, 0.27f);
            b.tray = trayGo.AddComponent<EvrakTray>();
            var sheet = Make("Report sheet", trayGo.transform);
            sheet.transform.localPosition = new Vector3(-0.012f, 0.0285f, 0.008f);
            sheet.SetActive(false);
            b.sheet = sheet.transform;
            b.over = Make("Slip over", trayGo.transform).transform;
            b.over.localPosition = new Vector3(0, 0.10f, 0);
            b.over.rotation = Quaternion.Euler(77.39f, -4f, 4.58f);
            var lightGo = Make("Slip target light", trayGo.transform);
            lightGo.transform.localPosition = new Vector3(0, 0.11f, -0.02f);
            b.light = lightGo.AddComponent<Light>();
            b.light.type = LightType.Spot;   // token türü yazmalı
            b.light.enabled = false;
            b.tray.Configure(sheet, null);
            b.tray.ConfigureReceiptDrop(b.trayBox);
            b.tray.ConfigureSlip(b.light, b.over, null);

            b.cam = K6Camera();

            var paperGo = Make("Physical receipt");
            b.hit = paperGo.AddComponent<BoxCollider>();
            b.hit.size = new Vector3(0.10f, 0.15f, 0.006f);
            b.hit.center = new Vector3(0, 0, -0.002f);
            b.paper = paperGo.AddComponent<ShiftReceiptPaper>();
            b.paper.Configure(b.exit, b.over, b.hit, null, null, b.cam);
            b.paper.ConfigureSlip(b.start, null, b.tray);
            paperGo.SetActive(false);

            if (withMachine)
            {
                var lampGo = Make("Lamp");
                var renderer = lampGo.AddComponent<MeshRenderer>();
                var off = Mat("jewelAmberOff");
                var on = Mat("jewelAmberOn");
                renderer.sharedMaterial = off;
                b.lamp = lampGo.AddComponent<PartDriver>();
                b.lamp.Configure("Lamp", PartKind.Swap, PartAxis.Z, 0, 0, 0, Vector3.zero, renderer, 0, new[] { off, on });

                if (withSwitch)
                {
                    var switchGo = Make("ShiftSwitch");
                    var hinge = Make("Cover", switchGo.transform).transform;
                    var lever = Make("Lever", switchGo.transform).transform;
                    b.shiftSwitch = switchGo.AddComponent<ShiftSwitch>();
                    b.shiftSwitch.Configure(hinge, lever, null, null, null, null, null);
                }

                var machineGo = Make("Machine");
                b.machine = machineGo.AddComponent<ShiftReceiptMachine>();
                b.machine.Configure(b.shiftSwitch, b.paper, b.tray, b.cam, b.exit, null);
                b.machine.ConfigureLamp(b.lamp);
                b.machine.Sound += s => b.sounds.Add(s);
                b.machine.ReceiptFiled += id => b.filed.Add(id);
                b.machine.DragChanged += on2 => b.drags.Add(on2);
            }
            else
            {
                b.paper.Sound += s => b.sounds.Add(s);
                b.paper.DragChanged += on2 => b.drags.Add(on2);
            }
            Physics.SyncTransforms();
            return b;
        }

        static HandoverSlipFacts Facts(int shift) => new HandoverSlipFacts { receiptId = "vardiya-" + (shift - 1), shift = shift, alive = 8, capacity = 10 };

        static PointerEventData Pointer(Vector2 position) => new PointerEventData(null) { button = PointerEventData.InputButton.Left, position = position };

        static void RaiseLeverArrived(ShiftSwitch sw, bool down)
        {
            var field = typeof(ShiftSwitch).GetField("LeverArrived", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "ShiftSwitch.LeverArrived olayının alanı");
            var handler = field.GetValue(sw) as Action<bool>;
            Assert.IsNotNull(handler, "makine kol varışına bağlı (tembel bağlama)");
            handler(down);
        }

        static void AssertPose(Pose expected, Transform actual, float mm, string what)
        {
            Assert.Less(Vector3.Distance(expected.position, actual.position), mm * 0.001f, what + ": konum");
            Assert.Less(Quaternion.Angle(expected.rotation, actual.rotation), 0.05f, what + ": dönüş");
        }

        static float Progress(Vector3 from, Vector3 to, Vector3 at) => Vector3.Dot(at - from, to - from) / (to - from).sqrMagnitude;

        // ------------------------------------------------------------------ pozlar

        /// <summary>E15: K6 token kamerasında el pozu: dört köşe kadrajda, fişin yüksekliği ekranın %67'si (723,6 px ± %1); yüz kameraya
        /// dönük, x eğimi 0°, z −2,9° (Karar D5); örnek pozu canlı kameradan aynı sonucu verir.</summary>
        [Test]
        public void HandPose_67Percent_AllCornersInFrame()
        {
            var cam = K6Camera();
            float w = Token("shiftClose.slip.sizeMm[0]") * 0.001f, h = Token("shiftClose.slip.sizeMm[1]") * 0.001f;
            Assert.AreEqual(0.10f, w, 1e-6f);
            Assert.AreEqual(0.15f, h, 1e-6f);
            var pose = ShiftReceiptPaper.HandPose(cam.transform.position, cam.transform.rotation, cam.fieldOfView);

            for (int i = 0; i < 4; i++)
            {
                var corner = pose.position + pose.rotation * new Vector3((i & 1) == 0 ? -w / 2 : w / 2, (i & 2) == 0 ? -h / 2 : h / 2, 0);
                var s = cam.WorldToScreenPoint(corner);
                Assert.Greater(s.z, cam.nearClipPlane, "köşe " + i + " kameranın önünde");
                Assert.That(s.x, Is.InRange(0f, 1920f), "köşe " + i + " yatayda kadrajda");
                Assert.That(s.y, Is.InRange(0f, 1080f), "köşe " + i + " dikeyde kadrajda");
            }
            var top = cam.WorldToScreenPoint(pose.position + pose.rotation * new Vector3(0, h / 2, 0));
            var bottom = cam.WorldToScreenPoint(pose.position + pose.rotation * new Vector3(0, -h / 2, 0));
            float px = Vector2.Distance(top, bottom);
            Assert.AreEqual(0.67f * 1080f, px, 0.01f * 0.67f * 1080f, "fişin ekrandaki yüksekliği");

            float d = Vector3.Dot(pose.position - cam.transform.position, cam.transform.forward);
            Assert.AreEqual(0.15f / (0.67f * 2 * Mathf.Tan(21f * Mathf.Deg2Rad)), d, 1e-4f, "K6'da uzaklık ≈ 0,2916 m");
            Assert.Greater(Vector3.Dot(pose.rotation * Vector3.back, cam.transform.position - pose.position), 0, "yüz (−z) kameraya bakar");
            Assert.Less(Vector3.Angle(pose.rotation * Vector3.forward, cam.transform.forward), 0.1f, "x eğimi 0° (D5; acos kayan nokta payı)");
            Assert.AreEqual(-2.9f, Vector3.SignedAngle(cam.transform.up, pose.rotation * Vector3.up, cam.transform.forward), 0.01f, "düzlem içi −2,9°");
            var local = Quaternion.Inverse(cam.transform.rotation) * (pose.position - cam.transform.position);
            Assert.AreEqual(0.02f, local.x, 1e-5f, "kamera sağına 2 cm");
            Assert.AreEqual(-0.004f, local.y, 1e-5f, "kamera aşağısına 4 mm");

            var b = Build(withMachine: false);
            var live = b.paper.HandTarget;
            Assert.Less(Vector3.Distance(live.position, ShiftReceiptPaper.HandPose(b.cam.transform.position, b.cam.transform.rotation, b.cam.fieldOfView).position), 1e-5f,
                "örnek pozu token boyutu ve varsayılanlarla statik pozla aynı");
        }

        /// <summary>Dil pozu: şalter yerelinde Euler (90, −90, 0), y 0,1356 ± 0,5 mm; fişin üstü ağızdan uzakta, yüzü yukarıda.</summary>
        [Test]
        public void TonguePose_TopAwayFromMouth_RestsOnTongue()
        {
            var b = Build(withMachine: false);
            var tongue = b.paper.TongueTarget;
            var local = b.switchRoot.InverseTransformPoint(tongue.position);
            Assert.AreEqual(-0.30f, local.x, 0.0005f);
            Assert.AreEqual(0.1356f, local.y, 0.0005f, "dilin üstünde (çapa 0,14 değil)");
            Assert.AreEqual(0.015f, local.z, 0.0005f);
            var rel = Quaternion.Inverse(b.switchRoot.rotation) * tongue.rotation;
            Assert.Less(Quaternion.Angle(rel, Quaternion.Euler(90, -90, 0)), 0.01f, "şalter yerelinde (90, −90, 0)");
            var mouth = (b.start.localPosition - b.exit.localPosition).normalized;
            var top = b.switchRoot.InverseTransformDirection(tongue.rotation * Vector3.up);
            Assert.Less(Vector3.Dot(top, mouth), -0.999f, "üst kenar ağızdan uzakta");
            var face = b.switchRoot.InverseTransformDirection(tongue.rotation * Vector3.back);
            Assert.Greater(Vector3.Dot(face, Vector3.up), 0.999f, "yüz yukarı");

            var stat = ShiftReceiptPaper.TonguePose(b.exit, new Vector3(0, -0.0044f, 0), new Vector3(90, -90, 0));
            Assert.Less(Vector3.Distance(stat.position, tongue.position), 1e-6f);
            b.paper.Present(Id, Facts(1), ShiftReceiptPaper.Entry.Instant);
            AssertPose(tongue, b.paper.transform, 0.01f, "anında sunulan fiş");
        }

        // ------------------------------------------------------------------ çıkış

        /// <summary>
        /// Basılan fiş kol aşağıya varana dek görünmez ve lamba sönük; kol varınca ağzın 40 mm içinden (Karar D12, yol 160 mm)
        /// 0,35 s'de dile çıkar, ease-out (yarı zamanda ≥ 0,6), başta yazıcı, sonda kopma sesi; lamba besleme başında yanar.
        /// </summary>
        [Test]
        public void Feed_WaitsForLever_EaseOut_035s()
        {
            var b = Build(withSwitch: true);
            b.shiftSwitch.Press();
            b.shiftSwitch.Press();   // kol inmeye başladı; Edit modda Advance olmadan varmaz
            b.machine.Present(Id, Facts(1), true);
            Assert.AreEqual(Where.Hidden, b.paper.State);
            Assert.IsTrue(b.paper.FeedPending, "kol inişini bekler");
            Assert.IsFalse(b.paper.gameObject.activeSelf, "beklerken görünmez");
            Assert.IsFalse(b.machine.LampLit, "lamba besleme başında yanar, önce değil");
            Assert.AreEqual(0, b.lamp.SwapState);
            b.paper.Advance(1f);
            Assert.AreEqual(Where.Hidden, b.paper.State, "zaman geçse de kol varmadan çıkmaz");
            Assert.IsEmpty(b.sounds);

            // D12: SlipStart'ın 40 mm içinden; yol ≈ 160 mm (dikeyde çapa 0,14 → dil 0,1356).
            var from = b.switchRoot.InverseTransformPoint(b.paper.FeedFrom);
            Assert.AreEqual(-0.14f, from.x, 0.0005f, "SlipStart + 40 mm ağız içine");
            Assert.AreEqual(0.14f, from.y, 0.0005f, "ağzın ekseninde başlar");
            Assert.AreEqual(0.015f, from.z, 0.0005f);
            Assert.AreEqual(0.16f, b.paper.FeedPathM, 0.0005f, "yol 160 mm");

            RaiseLeverArrived(b.shiftSwitch, true);
            Assert.AreEqual(Where.Feeding, b.paper.State);
            Assert.IsTrue(b.paper.gameObject.activeSelf);
            Assert.IsTrue(b.machine.LampLit);
            Assert.AreEqual(1, b.lamp.SwapState, "fiş ağzı lambası yanık");
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipPrint }, b.sounds);
            Assert.Less(Vector3.Distance(b.paper.transform.position, b.paper.FeedFrom), 1e-5f, "çıkış başında");

            float exit = Token("motion.shiftClose.slipExitS");
            Assert.AreEqual(0.35f, exit, 1e-6f);
            var start = b.paper.FeedFrom;
            var end = b.paper.TongueTarget.position;
            b.paper.Advance(exit * 0.5f);
            Assert.AreEqual(Where.Feeding, b.paper.State);
            float half = Progress(start, end, b.paper.transform.position);
            Assert.GreaterOrEqual(half, 0.6f, "ease-out: yarı zamanda yolun ≥ %60'ı");
            Assert.Less(half, 1f);
            b.paper.Advance(exit * 0.5f - 0.005f);
            Assert.AreEqual(Where.Feeding, b.paper.State, "0,35 s dolmadan dilde değil");
            b.paper.Advance(0.01f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            AssertPose(b.paper.TongueTarget, b.paper.transform, 1f, "dilde");
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipPrint, ShiftSound.SlipTear }, b.sounds);
            Assert.IsTrue(b.machine.LampLit);
        }

        /// <summary>Aynı kimlikle yeniden sunmak fişi yeniden çıkarmaz (yalnız yüz verisi yenilenir); kol beklenirken anında sunma beklemeyi bozmaz.</summary>
        [Test]
        public void Present_SameId_NoRefeed()
        {
            var b = Build();
            b.machine.Present(Id, Facts(1), true);   // şalter yok: besleme hemen
            Assert.AreEqual(Where.Feeding, b.paper.State);
            b.paper.Advance(1f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            var before = b.paper.transform.position;
            var refreshed = Facts(1);
            b.machine.Present(Id, refreshed, true);
            b.machine.Present(Id, refreshed, false);
            Assert.AreEqual(Where.Tongue, b.paper.State, "aynı kimlik: çıkış yeniden oynamaz");
            Assert.AreSame(refreshed, b.paper.Facts, "yüz yeni veriyle");
            Assert.AreEqual(before, b.paper.transform.position);
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipPrint, ShiftSound.SlipTear }, b.sounds, "tek çıkış");

            // Kol inerken gelen dil değişimi (aynı kimlik, feed false) beklemeyi bozmaz.
            var w = Build(withSwitch: true);
            w.shiftSwitch.Press();
            w.shiftSwitch.Press();
            w.machine.Present(Id, Facts(1), true);
            w.machine.Present(Id, Facts(1), false);
            Assert.IsTrue(w.paper.FeedPending, "bekleme sürer");
            Assert.AreEqual(Where.Hidden, w.paper.State);
            RaiseLeverArrived(w.shiftSwitch, true);
            Assert.AreEqual(Where.Feeding, w.paper.State);
        }

        /// <summary>Kayıttan yükleme / dil: fiş dilde anında belirir, çıkış ve ses yok, lamba yanık; boş kimlik gizler ve söndürür.</summary>
        [Test]
        public void Present_Instant_NoFeed_NoSound()
        {
            var b = Build();
            b.machine.Present(Id, Facts(1), false);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            Assert.IsFalse(b.paper.FeedPending);
            Assert.IsTrue(b.paper.gameObject.activeSelf);
            AssertPose(b.paper.TongueTarget, b.paper.transform, 0.01f, "anında dilde");
            Assert.IsTrue(b.machine.LampLit);
            Assert.AreEqual(1, b.lamp.SwapState);
            b.paper.Advance(1f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            Assert.IsEmpty(b.sounds, "anında sunuşta ses yok");

            b.machine.Present(null, null, false);
            Assert.AreEqual(Where.Hidden, b.paper.State);
            Assert.IsFalse(b.paper.gameObject.activeSelf);
            Assert.IsFalse(b.machine.LampLit);
            Assert.AreEqual(0, b.lamp.SwapState);
            Assert.IsEmpty(b.sounds);
        }

        // ------------------------------------------------------------------ bırakış

        /// <summary>
        /// EVRAK dışına bırakılan fiş 0,25 s'de (<c>motion.board.returnArcS</c>) ortada <c>cards.heldLiftMm</c> kavisle dile döner;
        /// sürüklerken EVRAK üstü dönüşüne geçer; dosyalama yok, lamba yanık kalır.
        /// </summary>
        [Test]
        public void DropOutside_ReturnsToTongueIn025s()
        {
            var b = Build();
            b.machine.Present(Id, Facts(1), false);
            b.paper.OnBeginDrag(Pointer(b.Outside));
            Assert.AreEqual(Where.Dragging, b.paper.State);
            Assert.IsTrue(b.paper.IsDragging);
            Assert.IsFalse(b.hit.enabled, "sürüklerken fişin çarpıştırıcısı kapalı");
            CollectionAssert.AreEqual(new[] { true }, b.drags);
            b.paper.OnDrag(Pointer(b.Outside));
            b.paper.Advance(0.2f);
            Assert.AreEqual(b.over.position.y, b.paper.transform.position.y, 1e-4f, "EVRAK üstü çapasının yatay düzleminde");
            Assert.Less(Quaternion.Angle(b.over.rotation, b.paper.transform.rotation), 0.05f, "0,1 s'de EVRAK üstü dönüşü");
            var release = b.paper.transform.position;

            b.paper.OnEndDrag(Pointer(b.Outside));
            Assert.AreEqual(Where.Returning, b.paper.State);
            Assert.IsFalse(b.paper.IsDragging);
            Assert.IsTrue(b.hit.enabled);
            CollectionAssert.AreEqual(new[] { true, false }, b.drags);
            Assert.IsEmpty(b.filed);
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipReturn }, b.sounds);
            Assert.IsTrue(b.machine.LampLit);
            Assert.IsFalse(b.tray.ReceiptTargetLit);

            float back = Token("motion.board.returnArcS");
            float arcM = Token("cards.heldLiftMm") * 0.001f;
            Assert.AreEqual(0.25f, back, 1e-6f);
            var tongue = b.paper.TongueTarget.position;
            b.paper.Advance(back * 0.5f);
            var straight = (release + tongue) * 0.5f;   // in-out yarı zamanda yolun yarısı
            Assert.AreEqual(straight.y + arcM, b.paper.transform.position.y, 2e-4f, "ortada kavis tepesi");
            Assert.AreEqual(straight.x, b.paper.transform.position.x, 2e-4f);
            Assert.AreEqual(straight.z, b.paper.transform.position.z, 2e-4f);
            b.paper.Advance(back * 0.5f - 0.01f);
            Assert.AreEqual(Where.Returning, b.paper.State);
            b.paper.Advance(0.02f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            AssertPose(b.paper.TongueTarget, b.paper.transform, 0.01f, "dile döndü");
            Assert.IsEmpty(b.filed);
        }

        /// <summary>Bulgu 17: EVRAK üstüne bırakılsa da <c>CanFile</c> reddederse fiş dile döner (tepside asılı kalmaz), ışık söner.</summary>
        [Test]
        public void DropOnTray_CanFileFalse_Returns()
        {
            var b = Build();
            var asked = new List<string>();
            b.machine.CanFile = id => { asked.Add(id); return false; };
            b.machine.Present(Id, Facts(1), false);
            b.paper.OnBeginDrag(Pointer(b.TrayScreen));
            b.paper.OnDrag(Pointer(b.TrayScreen));
            Assert.IsTrue(b.tray.ReceiptTargetLit, "üstündeyken ışık yanar");
            b.paper.OnEndDrag(Pointer(b.TrayScreen));
            CollectionAssert.AreEqual(new[] { Id }, asked, "karar bağlayıcının");
            Assert.IsEmpty(b.filed);
            Assert.AreEqual(Where.Returning, b.paper.State);
            Assert.IsFalse(b.tray.ReceiptTargetLit);
            Assert.IsFalse(b.light.enabled);
            Assert.IsTrue(b.machine.LampLit, "fiş EVRAK'a girmedi: lamba yanık");
            b.paper.Advance(Token("motion.board.returnArcS") + 0.01f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            AssertPose(b.paper.TongueTarget, b.paper.transform, 0.01f, "dile döndü");
        }

        /// <summary>Kabul edilen bırakış: <c>ReceiptFiled</c> bir kez, lamba hemen söner, fiş 0,2 s'de (ease-out) tepsinin bırakma pozuna
        /// iner ve varınca “şap”; tepside bekler; gizlenince kimlik boşalır.</summary>
        [Test]
        public void DropOnTray_Files_02s_LampOff()
        {
            var b = Build();
            b.machine.CanFile = _ => true;
            b.machine.Present(Id, Facts(1), false);
            b.paper.OnBeginDrag(Pointer(b.TrayScreen));
            b.paper.OnDrag(Pointer(b.TrayScreen));
            b.paper.Advance(0.2f);
            b.paper.OnEndDrag(Pointer(b.TrayScreen));
            CollectionAssert.AreEqual(new[] { Id }, b.filed);
            Assert.AreEqual(Where.ToTray, b.paper.State);
            Assert.IsFalse(b.machine.LampLit, "kabul edilen bırakışta lamba söner");
            Assert.AreEqual(0, b.lamp.SwapState);
            Assert.IsFalse(b.tray.ReceiptTargetLit);
            Assert.IsFalse(b.sounds.Contains(ShiftSound.SlipFile), "ses varışta");

            float toTray = Token("motion.shiftClose.slipToTrayS");
            Assert.AreEqual(0.2f, toTray, 1e-6f);
            b.paper.Advance(toTray - 0.01f);
            Assert.AreEqual(Where.ToTray, b.paper.State);
            b.paper.Advance(0.02f);
            Assert.AreEqual(Where.Tray, b.paper.State);
            AssertPose(b.tray.SlipDropPose, b.paper.transform, 0.01f, "tepsideki üst kâğıdın üstünde");
            Assert.AreEqual(1, b.sounds.Count(s => s == ShiftSound.SlipFile));
            Assert.AreEqual(ShiftSound.SlipFile, b.sounds.Last());
            b.paper.Advance(1f);
            Assert.AreEqual(Where.Tray, b.paper.State, "karar gelene dek tepside");
            Assert.AreEqual(1, b.filed.Count);

            b.paper.Hide();
            Assert.AreEqual(Where.Hidden, b.paper.State);
            Assert.IsNull(b.paper.ReceiptId, "Core fişi tüketti");
            Assert.IsFalse(b.paper.gameObject.activeSelf);
            Assert.IsFalse(b.machine.LampLit);
        }

        /// <summary>Dosyalama Core'da reddedilirse (<c>Reject</c>) fiş tepsiden de, tepsiye inerken de dile döner ve lamba yeniden yanar.</summary>
        [Test]
        public void Reject_FromTray_ReturnsAndRelights()
        {
            var b = Build();
            b.machine.Present(Id, Facts(1), false);
            b.paper.OnBeginDrag(Pointer(b.TrayScreen));
            b.paper.OnEndDrag(Pointer(b.TrayScreen));
            b.paper.Advance(1f);
            Assert.AreEqual(Where.Tray, b.paper.State);
            Assert.IsFalse(b.machine.LampLit);
            b.sounds.Clear();

            b.machine.Reject();
            Assert.AreEqual(Where.Returning, b.paper.State);
            Assert.IsTrue(b.machine.LampLit, "lamba yeniden yanar");
            Assert.AreEqual(1, b.lamp.SwapState);
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipReturn }, b.sounds);
            b.paper.Advance(Token("motion.board.returnArcS") + 0.01f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            AssertPose(b.paper.TongueTarget, b.paper.transform, 0.01f, "dile döndü");

            // Tepsiye inerken gelen ret de fişi dile döndürür.
            b.paper.OnBeginDrag(Pointer(b.TrayScreen));
            b.paper.OnEndDrag(Pointer(b.TrayScreen));
            Assert.AreEqual(Where.ToTray, b.paper.State);
            b.machine.Reject();
            Assert.AreEqual(Where.Returning, b.paper.State);
            b.paper.Advance(1f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            Assert.IsTrue(b.machine.LampLit);
        }

        /// <summary>Bırakışı dinleyen yoksa da fiş askıda kalmaz: dile döner.</summary>
        [Test]
        public void Drop_WithoutMachine_StillReturnsToTongue()
        {
            var b = Build(withMachine: false);
            b.paper.Present(Id, Facts(1), ShiftReceiptPaper.Entry.Instant);
            b.paper.OnBeginDrag(Pointer(b.TrayScreen));
            b.paper.OnEndDrag(Pointer(b.TrayScreen));
            Assert.AreEqual(Where.Returning, b.paper.State);
            b.paper.Advance(1f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
        }

        // ------------------------------------------------------------------ EVRAK hedefi

        /// <summary>Hedef ışığı yalnız sürüklenen fişin işaretçisi EVRAK kutusunun üstündeyken yanar (ölçüt ekran noktasının ışını).</summary>
        [Test]
        public void TargetLight_OnlyWhilePointerOverTray()
        {
            var b = Build();
            b.machine.Present(Id, Facts(1), false);
            Assert.IsFalse(b.tray.ReceiptTargetLit);
            Assert.IsFalse(b.light.enabled);
            Assert.IsTrue(b.tray.ContainsReceiptDrop(b.TrayScreen, b.cam), "tepsinin ortası kutunun içinde");
            Assert.IsFalse(b.tray.ContainsReceiptDrop(b.Outside, b.cam), "sol alt köşe kutunun dışında");

            b.paper.OnBeginDrag(Pointer(b.Outside));
            Assert.IsFalse(b.light.enabled, "dışarıda başlayan sürükleme");
            b.paper.OnDrag(Pointer(b.TrayScreen));
            Assert.IsTrue(b.tray.ReceiptTargetLit);
            Assert.IsTrue(b.light.enabled);
            b.paper.OnDrag(Pointer(b.Outside));
            Assert.IsFalse(b.light.enabled);
            b.paper.OnDrag(Pointer(b.TrayScreen));
            Assert.IsTrue(b.light.enabled);
            b.paper.OnEndDrag(Pointer(b.Outside));
            Assert.IsFalse(b.tray.ReceiptTargetLit, "bırakışta söner");
            Assert.IsFalse(b.light.enabled);
        }

        /// <summary>Bırakma pozu tepsideki üst kâğıdın yüksekliğini izler (erken 0,0285, geç yığın 0,0582; +1,5 mm); yüz yukarı, üst oyuncudan uzakta.</summary>
        [Test]
        public void DropPose_FollowsTraySheetHeight()
        {
            var b = Build(withMachine: false);
            var tray = b.tray.transform;
            foreach (var y in new[] { 0.0285f, 0.0582f })
            {
                b.sheet.localPosition = new Vector3(b.sheet.localPosition.x, y, b.sheet.localPosition.z);
                var pose = b.tray.SlipDropPose;
                var local = tray.InverseTransformPoint(pose.position);
                Assert.AreEqual(0.006f, local.x, 1e-5f);
                Assert.AreEqual(y + 0.0015f, local.y, 1e-5f, "üst kâğıdın 1,5 mm üstünde (" + y + ")");
                Assert.AreEqual(-0.004f, local.z, 1e-5f);
                Assert.Less(Quaternion.Angle(tray.rotation * Quaternion.Euler(90, -0.3f, 0), pose.rotation), 0.01f);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(90, -4.3f, 0), pose.rotation), 0.05f, "dünya ≈ (90; −4,3; 0)");
                Assert.Greater(Vector3.Dot(pose.rotation * Vector3.back, Vector3.up), 0.999f, "yüz yukarı");
                Assert.Greater(Vector3.Dot(pose.rotation * Vector3.up, tray.forward), 0.999f, "üst kenar oyuncudan uzakta");
            }

            var facts = Facts(1);
            b.tray.ShowFiled(facts);
            Assert.AreSame(facts, b.tray.LastFiled);
            b.tray.SetFiledHidden(true);
            Assert.IsTrue(b.tray.FiledHidden);
            b.tray.SetFiledHidden(false);
            b.tray.ShowFiled(null);
            Assert.IsNull(b.tray.LastFiled);
        }

        /// <summary>Hedef ışığı yanarken türü, rengi, şiddeti (× <c>targetIntensityScale</c>), menzili ve gölgesi
        /// <c>shiftClose.evrakHighlight</c>'tan; sönünce kapalı.</summary>
        [Test]
        public void TargetLight_UsesEvrakHighlightTokens()
        {
            var theme = Theme;
            Assert.AreEqual("Point", theme.Text("shiftClose.evrakHighlight.type"));
            Assert.IsTrue(theme.TryGet("shiftClose.evrakHighlight.color", out var color));
            var hex = new Color32(0xFF, 0xB4, 0x4A, 0xFF);
            Assert.AreEqual(hex.r / 255f, color.r, 1 / 255f);
            Assert.AreEqual(hex.g / 255f, color.g, 1 / 255f);
            Assert.AreEqual(hex.b / 255f, color.b, 1 / 255f);
            float intensity = Token("shiftClose.evrakHighlight.intensity"), range = Token("shiftClose.evrakHighlight.range");
            Assert.AreEqual(0.55f, intensity, 1e-6f);
            Assert.AreEqual(0.35f, range, 1e-6f);
            Assert.AreEqual(0f, Token("shiftClose.evrakHighlight.shadows"), "gölgesiz");

            var b = Build(withMachine: false);
            var so = new SerializedObject(b.tray);
            so.FindProperty("targetIntensityScale").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            b.light.shadows = LightShadows.Hard;

            b.tray.SetReceiptTarget(true);
            Assert.IsTrue(b.tray.ReceiptTargetLit);
            Assert.IsTrue(b.light.enabled);
            Assert.AreEqual(LightType.Point, b.light.type);
            Assert.AreEqual(color.r, b.light.color.r, 1e-4f);
            Assert.AreEqual(color.g, b.light.color.g, 1e-4f);
            Assert.AreEqual(color.b, b.light.color.b, 1e-4f);
            Assert.AreEqual(intensity * 2f, b.light.intensity, 1e-5f, "token × URP çarpanı");
            Assert.AreEqual(range, b.light.range, 1e-5f);
            Assert.AreEqual(LightShadows.None, b.light.shadows);

            b.tray.SetReceiptTarget(false);
            Assert.IsFalse(b.tray.ReceiptTargetLit);
            Assert.IsFalse(b.light.enabled);
        }

        // ------------------------------------------------------------------ el ve arşiv

        /// <summary>El: 0,25 s'de ele gelir, hedef canlı kamerayı izler; elde de izler; <c>PutBack</c> 0,25 s'de dile döndürür; sürükleme
        /// elden de başlar (Karar D5). Okumak hiçbir karar olayı çıkarmaz.</summary>
        [Test]
        public void TakeToHand_FollowsLiveCamera_PutBackReturns()
        {
            var b = Build();
            b.machine.Present(Id, Facts(1), false);
            b.paper.TakeToHand();
            Assert.AreEqual(Where.ToHand, b.paper.State);
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipLift }, b.sounds);

            float toHand = Token("motion.shiftClose.slipToHandS");
            Assert.AreEqual(0.25f, toHand, 1e-6f);
            b.paper.Advance(toHand * 0.5f);
            Assert.AreEqual(Where.ToHand, b.paper.State);
            b.cam.transform.position += new Vector3(0, 0.05f, 0.02f);   // kamera K6'ya giderken
            b.paper.Advance(toHand * 0.5f + 0.01f);
            Assert.AreEqual(Where.Hand, b.paper.State);
            AssertPose(b.paper.HandTarget, b.paper.transform, 0.01f, "elde, kameranın yeni yerine göre");
            b.cam.transform.rotation *= Quaternion.Euler(2, 3, 0);
            b.paper.Advance(1 / 60f);
            AssertPose(b.paper.HandTarget, b.paper.transform, 0.01f, "elde kamerayı izler");
            Assert.IsTrue(b.machine.LampLit, "elde: fiş EVRAK'a girmedi");

            b.paper.PutBack();
            Assert.AreEqual(Where.Returning, b.paper.State);
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipLift, ShiftSound.SlipReturn }, b.sounds);
            b.paper.Advance(toHand + 0.01f);
            Assert.AreEqual(Where.Tongue, b.paper.State);
            AssertPose(b.paper.TongueTarget, b.paper.transform, 0.01f, "dile döndü");
            b.paper.PutBack();
            Assert.AreEqual(Where.Tongue, b.paper.State, "dildeyken PutBack yok sayılır");
            Assert.AreEqual(2, b.sounds.Count);

            b.paper.TakeToHand();
            b.paper.Advance(toHand + 0.01f);
            Assert.AreEqual(Where.Hand, b.paper.State);
            b.paper.OnBeginDrag(Pointer(b.TrayScreen));
            Assert.AreEqual(Where.Dragging, b.paper.State, "sürükleme elden de başlar");
            b.paper.OnEndDrag(Pointer(b.Outside));
            Assert.AreEqual(Where.Returning, b.paper.State);
            Assert.IsEmpty(b.filed);
        }

        /// <summary>Sürüklemesiz sol tık yalnız fiş masadayken ve <c>Interactable</c> iken <c>Clicked</c> çıkarır; kapalıyken sürükleme de başlamaz.</summary>
        [Test]
        public void Click_RaisesClicked_OnlyWhenInteractable()
        {
            var b = Build();
            int clicks = 0;
            b.machine.Clicked += () => clicks++;
            b.paper.OnPointerClick(Pointer(b.TrayScreen));
            Assert.AreEqual(0, clicks, "fiş yokken tık yok");
            b.machine.Present(Id, Facts(1), false);
            b.paper.OnPointerClick(Pointer(b.TrayScreen));
            Assert.AreEqual(1, clicks);
            b.paper.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Right });
            Assert.AreEqual(1, clicks, "sağ tık köprünün (geri)");
            b.paper.Interactable = false;
            b.paper.OnPointerClick(Pointer(b.TrayScreen));
            b.paper.OnBeginDrag(Pointer(b.TrayScreen));
            Assert.AreEqual(1, clicks);
            Assert.AreEqual(Where.Tongue, b.paper.State, "kilitliyken sürüklenmez");
        }

        /// <summary>Karar D7-A: arşivdeki fiş tepsideki kopyanın pozundan ele gelir, salt okunur (sürüklenmez, dosyalanmaz, lamba sönük);
        /// okunurken kopya gizli; <c>PutBack</c> tepsiye döndürüp gizler. Canlı fiş varken arşiv açılmaz.</summary>
        [Test]
        public void ReadArchived_ReadOnly_PutBackHidesAndRestoresCopy()
        {
            var b = Build();
            var archived = Facts(1);
            b.tray.ShowFiled(archived);
            var from = b.tray.SlipDropPose;
            int clicks = 0;
            b.machine.Clicked += () => clicks++;

            b.paper.ReadArchived(archived, from);
            Assert.AreEqual(Where.Reading, b.paper.State);
            Assert.IsTrue(b.paper.ReadOnly);
            Assert.IsTrue(b.paper.gameObject.activeSelf);
            Assert.AreSame(archived, b.paper.Facts);
            Assert.IsTrue(b.tray.FiledHidden, "okunurken tepsideki kopya gizli");
            Assert.IsFalse(b.machine.LampLit, "arşiv fişi ağız lambasını yakmaz");
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipLift }, b.sounds);
            AssertPose(from, b.paper.transform, 0.01f, "tepsideki kopyanın pozundan");

            float toHand = Token("motion.shiftClose.slipToHandS");
            b.paper.Advance(toHand + 0.01f);
            Assert.AreEqual(Where.Reading, b.paper.State);
            AssertPose(b.paper.HandTarget, b.paper.transform, 0.01f, "elde");
            b.paper.OnBeginDrag(Pointer(b.TrayScreen));
            Assert.IsFalse(b.paper.IsDragging, "salt okunur: sürüklenmez");
            b.paper.FileToTray();
            Assert.AreEqual(Where.Reading, b.paper.State, "salt okunur: dosyalanmaz");
            b.paper.OnPointerClick(Pointer(b.TrayScreen));
            Assert.AreEqual(1, clicks, "tık köprüye gider (geri koy)");
            b.machine.Present(null, null, false);
            Assert.AreEqual(Where.Reading, b.paper.State, "boş kimlikli tazeleme okumayı bozmaz");

            b.paper.PutBack();
            Assert.AreEqual(Where.Returning, b.paper.State);
            CollectionAssert.AreEqual(new[] { ShiftSound.SlipLift, ShiftSound.SlipReturn }, b.sounds);
            b.paper.Advance(toHand + 0.01f);
            Assert.AreEqual(Where.Hidden, b.paper.State);
            Assert.IsFalse(b.paper.ReadOnly);
            Assert.IsFalse(b.paper.gameObject.activeSelf);
            Assert.IsFalse(b.tray.FiledHidden, "kopya tepside yeniden görünür");
            Assert.IsEmpty(b.filed);

            b.machine.Present(Id, Facts(2), false);
            b.paper.ReadArchived(archived, from);
            Assert.IsFalse(b.paper.ReadOnly, "basılı fiş varken arşiv okunmaz");
            Assert.AreEqual(Where.Tongue, b.paper.State);
        }
    }
}
