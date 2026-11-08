using System.Collections.Generic;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 6 (R9, PLAN §1.4 ve §3 W1): kapının zamanı ve kartlı oyunda kapının devri. Sahne açılmaz; kapı boş
    /// nesnelerle kurulur (menteşe, çark, gözetleme kapağı, vuruş lambası ve ışığı, iki merdiven ışığı, boş siluet). Zaman
    /// <see cref="DoorProp.Advance"/> ile ilerler (Edit modda <c>Update</c> ve <c>OnEnable</c> çalışmaz); süreler <c>SiginakTheme</c>'den.
    /// M1 vuruş (0 / 0,3 / 0,6 s'de 0,08 s parlama, sonra amber), M3 açılış (çark 0,5 → kanat 0,8, merdiven ışığı 0,3; kapanış 0,6),
    /// eski yolun aynı kaldığı ve kartlı oyunda kırmızı lamba, kırmızı oda, siluet ve tebeşir çentiğinin hiç çağrılmadığı.
    /// </summary>
    public class Rev3DoorTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        /// <summary>Evre uçlarının yakalama payı (DoorProp.EndSlack 1e-6 s) ve ardışık adımların float toplamı için.</summary>
        const float Eps = 1e-4f;

        readonly List<Object> created = new List<Object>();
        System.Func<bool> blockedWas;

        [SetUp]
        public void Begin()
        {
            blockedWas = DeskTarget.Blocked;
            DeskTarget.Blocked = null;
        }

        [TearDown]
        public void Clean()
        {
            DeskTarget.Blocked = blockedWas;
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

        Material Mat(string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
            var m = new Material(shader) { name = name };
            created.Add(m);
            return m;
        }

        sealed class Door
        {
            public DoorProp prop;
            public Transform leaf, wheel, peep;
            public MeshRenderer lamp;
            public Material on, off, fatal;
            public Light lampLight;
            public Light[] stairs;
            public float[] stairBase;
            public VisitorSilhouette silhouette;
            public Transform figure;
            public float lampIntensity, flashBoost;
        }

        /// <summary>DeskGame'deki <c>Bunker door</c> düzeninin boş nesnelerle kopyası; bağlar serileşen alanlara yazılır.</summary>
        Door MakeDoor()
        {
            var d = new Door();
            var root = Make("Bunker door");
            d.prop = root.AddComponent<DoorProp>();
            d.leaf = Make("Hinge", root.transform).transform;
            d.wheel = Make("Wheel", d.leaf).transform;
            d.peep = Make("PeepCover", d.leaf).transform;
            d.lamp = Make("Lamp", root.transform).AddComponent<MeshRenderer>();
            d.on = Mat("jewelAmberOn");
            d.off = Mat("jewelAmberOff");
            d.fatal = Mat("knockLampRed");
            d.lamp.sharedMaterial = d.off;
            d.lampLight = Make("KnockLight", root.transform).AddComponent<Light>();
            d.lampLight.enabled = false;
            d.stairBase = new[] { 3f, 1.5f };
            d.stairs = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                d.stairs[i] = Make("Stair light " + i, root.transform).AddComponent<Light>();
                d.stairs[i].intensity = d.stairBase[i];
                d.stairs[i].enabled = false;
            }
            // Siluet bileşeni bir biçimle bağlı: kartlı oyunda (ziyaretçi listesi boş) hiç görünmemeli
            d.silhouette = Make("Visitors", root.transform).AddComponent<VisitorSilhouette>();
            d.figure = Make("Figure", d.silhouette.transform).transform;
            d.figure.gameObject.SetActive(false);
            var slot = Make("Slot", d.silhouette.transform).transform;
            var ss = new SerializedObject(d.silhouette);
            var figures = ss.FindProperty("figures");
            figures.arraySize = 1;
            figures.GetArrayElementAtIndex(0).FindPropertyRelative("kind").stringValue = VisitorSilhouette.Generic;
            figures.GetArrayElementAtIndex(0).FindPropertyRelative("root").objectReferenceValue = d.figure;
            var slots = ss.FindProperty("slots");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = slot;
            ss.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(d.prop);
            so.FindProperty("leaf").objectReferenceValue = d.leaf;
            so.FindProperty("wheel").objectReferenceValue = d.wheel;
            so.FindProperty("peepCover").objectReferenceValue = d.peep;
            so.FindProperty("lamp").objectReferenceValue = d.lamp;
            so.FindProperty("lampOn").objectReferenceValue = d.on;
            so.FindProperty("lampOff").objectReferenceValue = d.off;
            so.FindProperty("lampFatal").objectReferenceValue = d.fatal;
            so.FindProperty("lampLight").objectReferenceValue = d.lampLight;
            so.FindProperty("silhouette").objectReferenceValue = d.silhouette;
            var stairs = so.FindProperty("stairLights");
            stairs.arraySize = 2;
            for (int i = 0; i < 2; i++) stairs.GetArrayElementAtIndex(i).objectReferenceValue = d.stairs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            d.lampIntensity = so.FindProperty("lampIntensity").floatValue;
            d.flashBoost = so.FindProperty("flashBoost").floatValue;
            Assert.AreEqual(95, so.FindProperty("openAngle").floatValue, 1e-4f, "kanat 95° (layout3d.door.leafOpenDeg)");
            Assert.AreEqual(1.25f, so.FindProperty("wheelTurns").floatValue, 1e-6f, "çark 1,25 tur = 450°");
            return d;
        }

        static void AssertRotation(Quaternion expected, Quaternion actual, string what) =>
            Assert.Less(Quaternion.Angle(expected, actual), 0.01f, what + ": " + actual.eulerAngles);

        static void AssertAmberSteady(Door d, string when)
        {
            Assert.IsTrue(d.prop.Knocking, when + ": kapıda biri bekliyor");
            Assert.IsFalse(d.prop.LampFlashing, when + ": parlama yok");
            Assert.AreSame(d.on, d.lamp.sharedMaterial, when + ": lamba amber (jewelAmberOn)");
            Assert.IsTrue(d.lampLight.enabled, when + ": KnockLight yanık");
            Assert.AreEqual(d.lampIntensity, d.lampLight.intensity, 1e-5f, when + ": sabit amber şiddeti (parlama çarpanı yok)");
        }

        static void AssertLampOff(Door d, string when)
        {
            Assert.IsFalse(d.prop.Knocking, when);
            Assert.AreSame(d.off, d.lamp.sharedMaterial, when + ": lamba sönük (jewelAmberOff)");
            Assert.IsFalse(d.lampLight.enabled, when + ": KnockLight kapalı");
        }

        // ------------------------------------------------------------------ M1 · vuruş

        [Test]
        public void Knock_ThreeFlashesIn0_9_ThenAmber()
        {
            float knock = Token("motion.doorKnockS"), flash = Token("motion.doorLampFlashS");
            Assert.AreEqual(0.9f, knock, 1e-6f, "R9-not: üç vuruş 0,9 s");
            Assert.AreEqual(0.08f, flash, 1e-6f, "R9-not: her vuruşta 0,08 s parlar");
            var d = MakeDoor();
            d.prop.ResetClosed();
            AssertLampOff(d, "başvuru yok");
            d.prop.Advance(1f);
            AssertLampOff(d, "vuruşsuz zaman lambayı yakmaz");

            d.prop.Knock();
            Assert.IsTrue(d.prop.Knocking);
            Assert.IsTrue(d.prop.KnockPlaying);
            Assert.IsTrue(d.prop.LampFlashing, "ilk vuruş hemen (t = 0)");
            Assert.AreSame(d.on, d.lamp.sharedMaterial);
            Assert.IsTrue(d.lampLight.enabled);
            Assert.AreEqual(d.lampIntensity * d.flashBoost, d.lampLight.intensity, 1e-4f, "parlamada ışık çarpanı (1,2 → 2,2)");
            Assert.AreEqual(Theme.ColorOf("door.knockLamp"), d.lampLight.color, "amber ışık rengi token'dan");

            // 2 ms adımlarla 1 s: parlama pencereleri 0 / 0,3 / 0,6 s'de başlar ve 0,08 s sürer
            const float step = 0.002f;
            var rises = new List<float> { 0 };
            var falls = new List<float>();
            bool was = true;
            float t = 0;
            for (int i = 0; i < 500; i++)
            {
                d.prop.Advance(step);
                t += step;
                bool now = d.prop.LampFlashing;
                if (now && !was) rises.Add(t);
                if (!now && was) falls.Add(t);
                was = now;
                Assert.AreNotSame(d.fatal, d.lamp.sharedMaterial, "kırmızı hiç yanmaz");
                Assert.AreSame(d.on, d.lamp.sharedMaterial, "vuruş boyunca lamba amber (t = " + t + ")");
                Assert.IsTrue(d.lampLight.enabled, "vuruş boyunca ışık yanık");
                float want = now ? d.lampIntensity * d.flashBoost : d.lampIntensity;
                Assert.AreEqual(want, d.lampLight.intensity, 1e-4f, "şiddet parlamayla eşleşir (t = " + t + ")");
            }
            Assert.AreEqual(3, rises.Count, "üç vuruş: " + string.Join(", ", rises));
            Assert.AreEqual(3, falls.Count, "üç parlama söner: " + string.Join(", ", falls));
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i * knock / 3, rises[i], 2 * step, "vuruş " + (i + 1) + " başlangıcı");
                Assert.AreEqual(flash, falls[i] - rises[i], 2 * step, "vuruş " + (i + 1) + " parlaması");
            }
            Assert.IsFalse(d.prop.KnockPlaying, "0,9 s sonra vuruş biter");
            AssertAmberSteady(d, "vuruştan sonra");
            d.prop.Advance(5f);
            AssertAmberSteady(d, "dikkat: aday bekledikçe");

            // Lamba sönük = bugünkü SetKnocking(false); yeni vuruş baştan başlar
            d.prop.SetKnocking(false);
            AssertLampOff(d, "karar verildi");
            d.prop.Knock();
            d.prop.Advance(0.1f);
            Assert.IsTrue(d.prop.KnockPlaying);
            Assert.IsFalse(d.prop.LampFlashing, "0,1 s: ilk parlama bitti");
            d.prop.Advance(0.21f);
            Assert.IsTrue(d.prop.LampFlashing, "0,31 s: ikinci vuruş");
        }

        [Test]
        public void Knock_ClockIsInternal_NotWallTime()
        {
            var d = MakeDoor();
            d.prop.Knock();
            // Gerçek zaman geçse de Advance çağrılmadıkça vuruş ilerlemez (Time.unscaledTime okunmaz)
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < 120) { }
            d.prop.Advance(0);
            Assert.IsTrue(d.prop.KnockPlaying);
            Assert.IsTrue(d.prop.LampFlashing, "saat yalnız Advance ile akar");
            d.prop.ResetClosed();
            Assert.IsFalse(d.prop.KnockPlaying, "yeni oyun / kayıttan devam vuruşu keser");
            AssertLampOff(d, "ResetClosed");
        }

        // ------------------------------------------------------------------ M3 · açılış ve kapanış

        [Test]
        public void Open_Wheel0_5_ThenLeaf0_8_Stair0_3_Close0_6()
        {
            float wheelS = Token("admission.motion.admitWheelS"), leafS = Token("admission.motion.admitLeafS"), stairS = Token("motion.stairLightS");
            Assert.AreEqual(0.5f, wheelS, 1e-6f, "R9-not: çark 0,5 s");
            Assert.AreEqual(0.8f, leafS, 1e-6f, "R9-not: kanat 0,8 s");
            Assert.AreEqual(0.3f, stairS, 1e-6f, "R9-not: merdiven ışığı 0,3 s");
            var d = MakeDoor();
            d.prop.ResetClosed();
            Quaternion leafRest = d.leaf.localRotation, wheelRest = d.wheel.localRotation;
            d.prop.SetKnocking(true);

            d.prop.SetOpen(true, null, 1, false, wheelS, leafS);
            Assert.IsTrue(d.prop.Moving);
            // Çark: 0 → 450° (0,5 s); kanat ve ışık beklemede
            d.prop.Advance(wheelS * 0.5f);
            Assert.AreEqual(225, d.prop.WheelDegrees, 0.01f, "yarı yolda çark 225° (kolaylaştırma ortası)");
            Assert.AreEqual(0, d.prop.LeafDegrees, "çark dönerken kanat kımıldamaz");
            d.prop.Advance(wheelS * 0.5f - Eps);
            Assert.Less(d.prop.WheelDegrees, 450);
            Assert.AreEqual(0, d.prop.LeafDegrees);
            Assert.IsFalse(d.stairs[0].enabled, "kanat kımıldamadan merdiven ışığı yok");
            d.prop.Advance(Eps);
            Assert.AreEqual(450, d.prop.WheelDegrees, 1e-3f, "0,5 s: çark 450°");
            AssertRotation(wheelRest * Quaternion.Euler(0, 0, 450), d.wheel.localRotation, "çark");
            Assert.AreEqual(0, d.prop.LeafDegrees, 1e-3f, "0,5 s: kanat henüz 0°");
            Assert.AreEqual(0, d.prop.StairGlow, Eps);

            // Merdiven ışığı kanat kımıldayınca 0,3 s'de yanar
            d.prop.Advance(stairS - Eps);
            Assert.Greater(d.prop.StairGlow, 0.9f);
            Assert.Less(d.prop.StairGlow, 1);
            Assert.IsTrue(d.stairs[0].enabled);
            d.prop.Advance(Eps);
            Assert.AreEqual(1, d.prop.StairGlow, "0,8 s: merdiven ışığı tam");
            for (int i = 0; i < 2; i++) Assert.AreEqual(d.stairBase[i], d.stairs[i].intensity, 1e-5f, "sahnedeki şiddet " + i);
            Assert.Greater(d.prop.LeafDegrees, 0);

            // Kanat: 0 → 95° (0,8 s; çarktan sonra)
            d.prop.Advance(leafS - stairS - Eps);
            Assert.IsFalse(d.prop.IsOpen);
            Assert.IsTrue(d.prop.Moving);
            Assert.Less(d.prop.LeafDegrees, 95);
            d.prop.Advance(Eps);
            Assert.IsTrue(d.prop.IsOpen, "1,3 s: kanat açık");
            Assert.IsFalse(d.prop.Moving, "uçlar tam değere oturur");
            Assert.AreEqual(95, d.prop.LeafDegrees, "kanat 95° (tam)");
            AssertRotation(leafRest * Quaternion.Euler(0, 95, 0), d.leaf.localRotation, "kanat");
            Assert.IsFalse(d.silhouette.Visible, "kartlı oyunda koridor boş (siluet yok)");
            Assert.IsFalse(d.figure.gameObject.activeSelf);
            d.prop.Advance(1);
            Assert.AreEqual(95, d.prop.LeafDegrees, "açık kalır");

            // Kapanış: kanat 0,6 s (ışık onunla söner), sonra çark 0,5 × 0,6 s
            d.prop.SetOpen(false);
            Assert.IsTrue(d.prop.Moving);
            d.prop.Advance(0.6f - Eps);
            Assert.Greater(d.prop.LeafDegrees, 0);
            Assert.AreEqual(450, d.prop.WheelDegrees, 1e-3f, "kanat kapanırken çark yerinde");
            d.prop.Advance(Eps);
            Assert.AreEqual(0, d.prop.LeafDegrees, "0,6 s: kanat kapalı");
            Assert.AreEqual(0, d.prop.StairGlow, "merdiven ışığı kanatla söner");
            Assert.IsFalse(d.stairs[0].enabled);
            Assert.IsFalse(d.stairs[1].enabled);
            d.prop.Advance(wheelS * 0.6f - Eps);
            Assert.Greater(d.prop.WheelDegrees, 0);
            Assert.IsTrue(d.prop.Moving);
            d.prop.Advance(Eps);
            Assert.AreEqual(0, d.prop.WheelDegrees, "çark geri döndü");
            Assert.IsFalse(d.prop.Moving);
            AssertRotation(leafRest, d.leaf.localRotation, "kanat dinlenmede");
            AssertRotation(wheelRest, d.wheel.localRotation, "çark dinlenmede");
            Assert.IsFalse(d.silhouette.Visible, "kapanırken de siluet yok");
            Assert.AreNotSame(d.fatal, d.lamp.sharedMaterial, "kırmızı yok");
        }

        [Test]
        public void Open_SingleLargeStep_CarriesTheRestToTheNextPhase()
        {
            var d = MakeDoor();
            d.prop.ResetClosed();
            d.prop.SetOpen(true, null, 1, false, 0.5f, 0.8f);
            // Tek adımda 0,9 s: 0,5 s çark + 0,4 s kanat (artan süre devredilir; kanada tam 0,9 s verilmez)
            d.prop.Advance(0.9f);
            Assert.AreEqual(450, d.prop.WheelDegrees, 1e-3f);
            Assert.AreEqual(1, d.prop.StairGlow, "0,4 s ≥ 0,3 s: ışık tam");
            Assert.AreEqual(95 * 0.5f, d.prop.LeafDegrees, 1e-3f, "kanat yarı yolda (0,4 / 0,8; kolaylaştırma ortası)");
            d.prop.Advance(0.4f);
            Assert.IsTrue(d.prop.IsOpen);
            Assert.IsFalse(d.prop.Moving);
        }

        // ------------------------------------------------------------------ eski yol

        [Test]
        public void Legacy_Unchanged()
        {
            float wheelS = Token("motion.doorWheelS"), leafS = Token("motion.doorLeafS");
            var d = MakeDoor();
            d.prop.ResetClosed();
            // Eski imza: token süreleri (motion.doorWheelS / doorLeafS)
            d.prop.SetOpen(true, null);
            d.prop.Advance(wheelS);
            Assert.AreEqual(450, d.prop.WheelDegrees, 1e-3f);
            Assert.AreEqual(0, d.prop.LeafDegrees, 1e-3f);
            d.prop.Advance(leafS);
            Assert.IsTrue(d.prop.IsOpen);
            Assert.IsFalse(d.prop.Moving);

            // Süreli açılış kendi sürelerini kullanır; ardından eski imza yine token'a döner
            d.prop.ResetClosed();
            d.prop.SetOpen(true, null, 1, false, 0.2f, 0.3f);
            d.prop.Advance(0.2f);
            Assert.AreEqual(450, d.prop.WheelDegrees, 1e-3f, "süreli çark 0,2 s");
            d.prop.Advance(0.3f);
            Assert.IsTrue(d.prop.IsOpen, "süreli kanat 0,3 s");
            d.prop.ResetClosed();
            d.prop.SetOpen(true);
            d.prop.Advance(0.2f);
            Assert.Less(d.prop.WheelDegrees, 449, "eski imza süreli açılışın süresini taşımaz");

            // Eski oyunda kırmızı lamba aynen çalışır
            d.prop.ResetClosed();
            d.prop.SetFatal(true);
            Assert.AreSame(d.fatal, d.lamp.sharedMaterial, "eski yolun ölümcül kapısı (Soru 5 / K9)");
            d.prop.SetFatal(false);

            // Eski kapı bağlayıcısı: kabul bağı olsa da eski oyunda masa kilidi bugünkü ifade (kapı kımıldıyorsa meşgul)
            var (door, game, _) = Controller(LegacySimulation.NewGame(77, new Balance { startFon = 100 }), d, withAdmission: true);
            Assert.IsNull(game.Sim.Board, "eski oyun");
            Assert.IsFalse(door.Busy);
            d.prop.SetOpen(true);
            // Bugünkü ifade kapının etkinliğini de sayar (Edit modda isActiveAndEnabled OnEnable'a bağlı; Rev3ShiftCloseFlowTests kalıbı)
            Assert.AreEqual(d.prop.isActiveAndEnabled, door.Busy, "eski oyunda kanat kımıldarken masa kilitli (kabulün Busy'si değil)");
            d.prop.ResetClosed();
            Call(door, "ResetDoor");
            var target = (DeskTarget)Ref(door, "target");
            Assert.AreEqual(Loc.T("Biri çalınca: gözetleme deliği, interkom, kapıyı aç ya da açma."), target.LineOverride, "eski satır");
            Assert.AreEqual("SIĞINAK KAPISI", target.Title, "başlık değişmez");
        }

        // ------------------------------------------------------------------ kartlı oyun

        /// <summary>
        /// Kapı bağlayıcısı: gerçek <see cref="GameController"/> (simülasyon yansımayla), boş masa yüzü, kapı hedefi, kırmızıya dönmesi
        /// eski yolda beklenen bir oda ışığı, tebeşir çentiği; istenirse kabul (W0 sözleşmesi). <c>OnEnable</c> çağrılmaz.
        /// </summary>
        (SiginakDoor door, GameController game, Light room) Controller(Simulation sim, Door d, bool withAdmission)
        {
            var game = Make("Game").AddComponent<GameController>();
            typeof(GameController).GetProperty("Sim").GetSetMethod(true).Invoke(game, new object[] { sim });
            var surface = Make("Surface").AddComponent<LiberationDeskView>();
            var ss = new SerializedObject(surface);
            ss.FindProperty("game").objectReferenceValue = game;
            ss.ApplyModifiedPropertiesWithoutUndo();
            var go = Make("Door controller");
            var door = go.AddComponent<SiginakDoor>();
            // DeskTarget [RequireComponent(typeof(Collider))] ister; Collider soyut olduğundan Unity kendisi ekleyemez ve
            // AddComponent null döner. Sahnedeki kapı hedefinin de çarpıştırıcısı var (Rev3ShiftSwitchTests kalıbı).
            d.prop.gameObject.AddComponent<BoxCollider>();
            var target = d.prop.gameObject.AddComponent<DeskTarget>();
            Assert.IsNotNull(target, "DeskTarget eklenemedi (çarpıştırıcı?)");
            target.Configure("SIĞINAK KAPISI", "Biri çalınca: gözetleme deliği, interkom, kapıyı aç ya da açma.", null, null);
            var room = Make("Bulb light").AddComponent<Light>();
            room.color = Color.white;
            var tally = Make("Visitor tally").AddComponent<ChalkTally>();
            var so = new SerializedObject(door);
            so.FindProperty("game").objectReferenceValue = game;
            so.FindProperty("surface").objectReferenceValue = surface;
            so.FindProperty("prop").objectReferenceValue = d.prop;
            so.FindProperty("target").objectReferenceValue = target;
            so.FindProperty("tally").objectReferenceValue = tally;
            var lights = so.FindProperty("roomLights");
            lights.arraySize = 1;
            lights.GetArrayElementAtIndex(0).objectReferenceValue = room;
            if (withAdmission) so.FindProperty("admission").objectReferenceValue = Make("Admission controller").AddComponent<SiginakAdmission>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return (door, game, room);
        }

        static object Ref(Object o, string field) => new SerializedObject(o).FindProperty(field).objectReferenceValue;

        static void Call(object o, string method)
        {
            var m = o.GetType().GetMethod(method, Private);
            Assert.IsNotNull(m, method);
            m.Invoke(o, null);
        }

        /// <summary>Kartlı oyun (tohum 79, alarm yok); <paramref name="candidate"/> ise ilk aday gelene dek gerçek kapanışlar.</summary>
        static Simulation BoardGame(bool candidate)
        {
            var sim = Simulation.NewGame(79, new Balance { planningBoardEnabled = true, liberationAlarmPerShift = 0 });
            Assert.IsNotNull(sim.Board, "kartlı oyun");
            for (int i = 0; candidate && sim.Admission?.Candidate == null && i < 20; i++)
            {
                Assert.IsTrue(sim.PrintShift().Ok, "fiş basılır (V." + (sim.S.Day + 1) + ")");
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok, "fiş dosyalanır");
            }
            Assert.AreEqual(candidate, sim.Admission?.Candidate != null, candidate ? "kapıda aday (tohum 79: V.005)" : "Day 0: kapıda kimse yok");
            return sim;
        }

        [Test]
        public void BoardMode_NeverRed()
        {
            var sim = BoardGame(candidate: true);
            var d = MakeDoor();
            d.prop.ResetClosed();
            var (door, _, room) = Controller(sim, d, withAdmission: false);
            var tally = (ChalkTally)Ref(door, "tally");
            int tallyShown = tally.Shown;

            // Kabul bağı yok: eski yedek, aday varken lamba sürekli amber (vuruş yok)
            for (int i = 0; i < 5; i++) Call(door, "Update");
            Assert.IsTrue(d.prop.Knocking, "yedek: aday varken amber");
            Assert.IsFalse(d.prop.KnockPlaying, "yedek vuruş çalmaz");
            Assert.AreSame(d.on, d.lamp.sharedMaterial);
            var target = (DeskTarget)Ref(door, "target");
            Assert.AreEqual(Loc.T("Kapıda başvuru var: tezgâha bak."), target.LineOverride, "kartlı satır (aday var)");
            Assert.AreEqual("SIĞINAK KAPISI", target.Title, "başlık değişmez (sahne testi ve çeviri kaynağı)");

            // Harekât bitti: lamba söner, kırmızıya hiç dönmez; oda ışığı rengi aynı; siluet ve çentik çağrılmaz
            sim.S.Ending = "vuruldu";
            Call(door, "Update");
            Assert.IsFalse(d.prop.Knocking);
            Assert.AreSame(d.off, d.lamp.sharedMaterial, "kartlı oyunda kırmızı lamba yok");
            Assert.AreEqual(Color.white, room.color, "oda kırmızıya dönmez");
            Assert.AreEqual(Loc.T("Kapıda kimse yok."), target.LineOverride);
            sim.S.Ending = null;
            Call(door, "ResetDoor");
            Assert.AreEqual(Color.white, room.color);
            Assert.AreNotSame(d.fatal, d.lamp.sharedMaterial);
            Assert.IsFalse(d.silhouette.Visible, "siluet yok");
            Assert.AreEqual(tallyShown, tally.Shown, "tebeşir çentiği kartlı oyunda çağrılmaz");
            Assert.IsFalse(door.Busy, "yedekte kapı kımıldamıyorsa masa açık");
        }

        [Test]
        public void BoardMode_DelegatesTheDoorToAdmission()
        {
            var sim = BoardGame(candidate: true);
            var d = MakeDoor();
            d.prop.ResetClosed();
            var (door, game, room) = Controller(sim, d, withAdmission: true);
            var admission = (SiginakAdmission)Ref(door, "admission");
            Assert.IsNotNull(admission);

            // Lamba kabulün işi: bağlayıcı her karede yazmaz (vuruşu ve amberi kabul kurar)
            for (int i = 0; i < 3; i++) Call(door, "Update");
            Assert.IsFalse(d.prop.Knocking, "kabul bağlıyken SiginakDoor lambayı sürmez (yalnız yedek)");
            d.prop.Knock();
            Call(door, "Update");
            d.prop.Advance(0.05f);
            Assert.IsTrue(d.prop.KnockPlaying, "kabulün vuruşu bağlayıcının karesinde kesilmez");
            Assert.AreEqual(Loc.T("Kapıda başvuru var: tezgâha bak."), ((DeskTarget)Ref(door, "target")).LineOverride);

            // Masa kilidi kabulün dizisi: kanat kımıldasa da kabul meşgul değilse kapı da değil
            d.prop.SetOpen(true, null, 1, false, 0.5f, 0.8f);
            Assert.IsTrue(d.prop.Moving);
            Assert.AreEqual(admission.Busy, door.Busy, "kartlı oyunda Busy = SiginakAdmission.Busy");

            // Yeni oyun / kayıttan devam (D12): kapı kapanır, bekleyen aday için vuruşsuz sürekli amber
            Call(door, "ResetDoor");
            Assert.IsFalse(d.prop.Moving);
            Assert.AreEqual(0, d.prop.LeafDegrees);
            Assert.IsFalse(d.prop.KnockPlaying, "yüklemede vuruş yok");
            AssertAmberSteady(d, "kayıttan devam, aday kapıda");
            Assert.AreEqual(Color.white, room.color);

            // Kapı hedefi: K8 ailesi dışında odanın kilidine uyar
            var blocked = (System.Func<bool>)System.Delegate.CreateDelegate(typeof(System.Func<bool>), door,
                typeof(SiginakDoor).GetMethod("TargetBlocked", Private));
            Assert.AreEqual(admission.Active, blocked(), "kabul etkin değilken (K8 ailesi dışında) kapı hedefi açık");
            DeskTarget.Blocked = () => true;
            Assert.IsTrue(blocked(), "odanın kilidi geçerli");
            DeskTarget.Blocked = null;

            // Aday yokken: satır "kimse yok", yüklemede lamba sönük
            typeof(GameController).GetProperty("Sim").GetSetMethod(true).Invoke(game, new object[] { BoardGame(candidate: false) });
            Call(door, "ResetDoor");
            AssertLampOff(d, "başvuru yok");
            Assert.AreEqual(Loc.T("Kapıda kimse yok."), ((DeskTarget)Ref(door, "target")).LineOverride);
        }

        [Test]
        public void BoardLines_AreLocSources()
        {
            // Kaynak metinler Loc.N ile tabloda (PLAN §4; çeviri W6) ve kapının sabitleri arasında
            var fields = typeof(SiginakDoor).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
            var values = new List<string>();
            foreach (var f in fields) if (f.FieldType == typeof(string)) values.Add((string)f.GetValue(null));
            CollectionAssert.Contains(values, "Kapıda başvuru var: tezgâha bak.");
            CollectionAssert.Contains(values, "Kapıda kimse yok.");
            CollectionAssert.Contains(values, "SIĞINAK KAPISI");
            CollectionAssert.Contains(values, "Biri çalınca: gözetleme deliği, interkom, kapıyı aç ya da açma.");
        }
    }
}
