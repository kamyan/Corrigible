using System.Collections.Generic;
using System.Linq;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 6 (R17, PLAN §1.6–§1.12, paket W2): kabul istasyonunun ve araçlarının hareketi. Sahne açılmaz: paket model
    /// prefab'ları (<c>Prefabs/Rev3/admission</c>, <c>door/IntercomBox</c>) DeskGame'deki yerlerinde (ADM tabanı (2,20; 0,98; −0,27) yaw 90)
    /// örneklenir, kâğıtlar çarpıştırıcılı boş nesnelerdir (yüzler W3'ün; burada yalnız pozlar). Kamera K8A token'ından (1920 × 1080).
    /// Zaman <c>Advance</c> ile ilerler (Edit modda <c>Update</c> çalışmaz); süreler <c>SiginakTheme</c>'den, uç değerler tam (E M2–M8).
    /// </summary>
    public class Rev3AdmissionMotionTests
    {
        const float Dt = 1f / 240f;
        const float Edge = 2.5f * Dt;

        readonly List<Object> created = new List<Object>();
        readonly List<Camera> cameras = new List<Camera>();

        [TearDown]
        public void Clean()
        {
            DeskTarget.Blocked = null;
            foreach (var c in cameras) if (c != null) c.targetTexture = null;
            cameras.Clear();
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

        GameObject Make(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            else created.Add(go);
            return go;
        }

        GameObject Model(string family, string name, Transform parent, Vector3 local, float yaw = 0f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Rev3ModelImporter.PrefabPath(family, name));
            if (asset == null) Assert.Ignore("Model prefab'ı yok (Corrigible/Revizyon 3/Modelleri içe aktar): " + name);
            var go = Object.Instantiate(asset);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            else created.Add(go);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        static PartDriver Part(GameObject model, string name)
        {
            var p = model.GetComponent<Rev3Model>().Part(name);
            Assert.IsNotNull(p, model.name + "/" + name);
            return p;
        }

        Material Mat(string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
            var m = new Material(shader) { name = name };
            created.Add(m);
            return m;
        }

        Transform Paper(string name, Transform parent, Vector2 sizeMm)
        {
            var go = Make(name, parent);
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(sizeMm.x / 1000f, sizeMm.y / 1000f, 0.002f);
            return go.transform;
        }

        Camera K8ACamera()
        {
            var go = Make("K8A kamera");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            var texture = new RenderTexture(1920, 1080, 0);
            created.Add(texture);
            cameras.Add(cam);
            cam.targetTexture = texture;
            cam.aspect = 16f / 9f;
            cam.nearClipPlane = 0.03f;
            cam.fieldOfView = Token("cameras.K8A.fovV");
            var pos = Theme.Vector("cameras.K8A.pos");
            var target = Theme.Vector("cameras.K8A.target");
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(target - pos, Vector3.up));
            return cam;
        }

        sealed class Bench
        {
            public GameObject root, counter, panel, lamp, loose;
            public AdmissionStation station;
            public PartDriver top, bracketL, bracketR, drawer, marker, jewel;
            public BoxCollider counterHit, deskKey;
            public Renderer lampBody;
            public Material lampOn, lampOff;
            public GameObject spot;
            public Transform doc, strip;
            public Transform[] archive, slips;
            public GameObject[] tins;
            public StampDecision[] stamps = new StampDecision[0];
            public MagnifierTool magnifier;
            public RequestPadTool pad;
            public Camera cam;
            public float time;
            public readonly List<AdmissionSound> sounds = new List<AdmissionSound>();
            public readonly List<(AdmissionPaper kind, int index)> papers = new List<(AdmissionPaper, int)>();
            public int counterClicks, deskClicks, taken;

            public void Advance(float dt)
            {
                station.Advance(dt);
                foreach (var s in stamps) s.Advance(dt);
                if (magnifier != null) magnifier.Advance(dt);
                if (pad != null) pad.Advance(dt);
                time += dt;
            }

            /// <summary>Eylemden bu yana geçen süreyi <paramref name="t"/>'ye getirir.</summary>
            public void To(float t)
            {
                while (time < t - 1e-6f) Advance(Dt);
            }

            public void Zero() => time = 0f;
        }

        /// <summary>
        /// İstasyon kökü (2,20; 0,98; −0,27) yaw 90, altında AdmissionCounter ve AdmissionPanel (yerel 0), CounterLamp (0,16; 0,80; 0);
        /// belge, şerit, 2 ESKİ şeridi, 5 fiş; çekmecede 2 teneke; Top'ta bir gevşek araç grubu. <paramref name="tools"/>: dört damga,
        /// büyüteç ve talep defteri (Top'ta, DA:112–126 yerleri) ve K8A kamerası.
        /// </summary>
        Bench Build(bool tools = false)
        {
            var b = new Bench();
            b.root = Make("Admission station");
            b.root.transform.SetPositionAndRotation(new Vector3(2.2f, 0.98f, -0.27f), Quaternion.Euler(0f, 90f, 0f));
            b.counter = Model("admission", "AdmissionCounter", b.root.transform, Vector3.zero);
            b.panel = Model("admission", "AdmissionPanel", b.root.transform, Vector3.zero);
            b.lamp = Model("admission", "CounterLamp", b.root.transform, new Vector3(0.16f, 0.80f, 0f));
            b.top = Part(b.counter, "Top");
            b.bracketL = Part(b.counter, "BracketL");
            b.bracketR = Part(b.counter, "BracketR");
            b.drawer = Part(b.panel, "Drawer");
            b.marker = Part(b.panel, "Marker");
            b.jewel = Part(b.panel, "Jewel");

            b.counterHit = Make("Tezgâh hedefi", b.top.transform).AddComponent<BoxCollider>();
            b.counterHit.center = new Vector3(0f, -0.005f, -0.15f);
            b.counterHit.size = new Vector3(0.84f, 0.01f, 0.3f);
            b.loose = Make("Gevşek araçlar", b.top.transform);
            b.deskKey = Make("MASA tuşu", b.loose.transform).AddComponent<BoxCollider>();
            b.deskKey.size = new Vector3(0.03f, 0.01f, 0.03f);

            b.lampBody = b.lamp.GetComponentsInChildren<MeshRenderer>(true).First(r => r.sharedMaterials.Length >= 5);
            b.lampOn = Mat("bulbGlass test");
            b.lampOff = Mat("roomBulbOff test");
            var light = b.lamp.GetComponent<Rev3Model>().LightPoint("CounterSpot");
            Assert.IsNotNull(light, "CounterSpot");
            b.spot = light.gameObject;

            b.doc = Paper("Belge", b.root.transform, AdmissionStation.DocSizeMm);
            b.strip = Paper("Şerit", b.panel.transform, AdmissionStation.StripSizeMm);
            b.archive = new[] { Paper("ESKİ 1", b.panel.transform, AdmissionStation.StripSizeMm), Paper("ESKİ 2", b.panel.transform, AdmissionStation.StripSizeMm) };
            b.slips = Enumerable.Range(0, 5).Select(i => Paper("Fiş " + i, b.counter.transform, new Vector2(90f, 60f))).ToArray();
            b.tins = new[] { Make("Teneke L", b.drawer.transform), Make("Teneke R", b.drawer.transform) };

            b.station = b.root.AddComponent<AdmissionStation>();
            b.station.ConfigureCounter(b.counter.transform, b.top, b.bracketL, b.bracketR, b.counterHit, b.deskKey);
            b.station.ConfigureLamp(b.lampBody, 4, b.lampOn, b.lampOff, b.spot);
            b.station.ConfigurePanel(b.panel.transform, b.drawer, b.marker, b.jewel);
            b.station.ConfigurePapers(b.doc, b.strip, b.archive, b.slips);

            if (tools)
            {
                b.cam = K8ACamera();
                b.stamps = new StampDecision[4];
                for (int i = 0; i < 4; i++)
                {
                    var go = Model("admission", "DecisionStamp", b.loose.transform, new Vector3(0.30f - 0.0885f + i * 0.059f, 0.0555f, -0.22f), -8f);
                    var box = go.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, 0.042f, 0f);
                    box.size = new Vector3(0.054f, 0.085f, 0.032f);
                    var tongue = Make("Kilit dili", go.transform);
                    var s = go.AddComponent<StampDecision>();
                    s.Configure((StampChoice)i, box, Part(go, "Band").GetComponent<Renderer>(), tongue, b.top.transform, b.cam);
                    b.stamps[i] = s;
                }
                var mag = Model("admission", "Magnifier", b.loose.transform, new Vector3(0.02f, 0.0035f, -0.22f), 182f);
                var magBox = mag.AddComponent<BoxCollider>();
                magBox.size = new Vector3(0.3f, 0.016f, 0.145f);
                b.magnifier = mag.AddComponent<MagnifierTool>();
                b.magnifier.Configure(b.top.transform, Part(mag, "Carriage"), magBox, b.cam);
                var padGo = Model("admission", "RequestPad", b.loose.transform, new Vector3(-0.295f, 0.003f, -0.215f), 6f);
                var padBox = padGo.AddComponent<BoxCollider>();
                padBox.size = new Vector3(0.11f, 0.02f, 0.15f);
                var sheet = Part(padGo, "Sheet");
                var sheetPaper = Paper("Talep yaprağı", sheet.transform, RequestPadTool.SheetSizeMm);
                sheetPaper.localPosition = new Vector3(0f, 0.0004f, -0.072f);
                sheetPaper.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var carbon = Paper("Karbon", padGo.transform, RequestPadTool.SheetSizeMm);
                b.pad = padGo.AddComponent<RequestPadTool>();
                b.pad.Configure(sheet, sheetPaper, carbon, b.panel.transform, padBox, b.cam);
            }
            b.station.ConfigureTools(new[] { b.loose }, b.stamps, b.magnifier, b.pad, b.tins);
            b.station.Sound += s => b.sounds.Add(s);
            b.station.PaperClicked += (k, i) => b.papers.Add((k, i));
            b.station.CounterClicked += () => b.counterClicks++;
            b.station.DeskKeyClicked += () => b.deskClicks++;
            b.station.DocumentTaken += () => b.taken++;
            foreach (var s in b.stamps) s.Sound += x => b.sounds.Add(x);
            Physics.SyncTransforms();
            return b;
        }

        static OrdersData Orders(int version) => new OrdersData { Version = version, Title = "SÜRÜM " + version };

        static SlipData Slip(string key, AdmissionTool? tool = AdmissionTool.Papers, bool review = false) =>
            new SlipData { Key = key, Tool = tool, Review = review, Title = "BELGELERİ KARŞILAŞTIR", Body = "Belge No. N-17/009 · sevk No. N-17/009." };

        static AdmissionSnapshot Snap(bool down, string candidate = null, bool onCounter = false, int markerRule = -1, int orders = 1, params SlipData[] slips)
        {
            var s = new AdmissionSnapshot
            {
                Active = true,
                Counter = down ? CounterState.Down : CounterState.Folded,
                CandidateId = candidate,
                Document = candidate != null ? new DocumentData { Name = "SUNA 9" } : null,
                DocumentOnCounter = onCounter,
                Orders = Orders(orders),
                MarkerRule = markerRule,
                Words = new AdmissionWords { Desk = "MASA", Drawer = "EVRAK", Holder = "TALİMAT", Findings = "BULGULAR" },
                CounterHoverTitle = "KABUL TEZGÂHI",
                CounterHoverLine = candidate != null ? "başvuru · SUNA 9" : "başvuru yok",
            };
            foreach (var d in slips) s.Slips.Add(d);
            return s;
        }

        static void Near(Vector3 expected, Vector3 actual, float tolerance, string message)
        {
            Assert.Less((expected - actual).magnitude, tolerance, message + " (beklenen " + expected.ToString("F4") + ", gelen " + actual.ToString("F4") + ")");
        }

        static PointerEventData Pointer(Vector2 position, GameObject hit = null, Vector3 world = default) => new PointerEventData(null)
        {
            button = PointerEventData.InputButton.Left,
            position = position,
            pointerCurrentRaycast = new RaycastResult { gameObject = hit, worldPosition = world },
            pointerPressRaycast = new RaycastResult { gameObject = hit, worldPosition = world },
        };

        static Vector2 Screen(Camera cam, Vector3 world) => cam.WorldToScreenPoint(world);

        /// <summary>Damganın sürükleme düzleminden (Top yereli y 0,0043) yüksekliği.</summary>
        static float Height(Bench b, Vector3 world) =>
            Vector3.Dot(world - b.top.transform.TransformPoint(0f, StampDecision.DragPlaneY, 0f), b.top.transform.up);

        /// <summary>Elde damga <paramref name="world"/> noktasının üstüne gelsin diye imlecin ekran noktası (tutma yüksekliğinde).</summary>
        static Vector2 Aim(Bench b, StampDecision s, Vector3 world) =>
            Screen(b.cam, world + b.top.transform.up * (s.HeldHeight - Height(b, world)));

        // ================================================================== token

        [Test]
        public void Durations_AreTheThemeTokens()
        {
            Assert.AreEqual(Token("admission.motion.counterBracketS"), AdmissionStation.BracketS, 1e-6f);
            Assert.AreEqual(0.15f, AdmissionStation.BracketS, 1e-6f);
            Assert.AreEqual(0.4f, AdmissionStation.LeafS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.counterLeafS"), AdmissionStation.LeafS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.lampS"), AdmissionStation.LampS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.counterFoldS"), AdmissionStation.FoldS, 1e-6f);
            Assert.AreEqual(0.35f, AdmissionStation.FoldS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.drawerS"), AdmissionStation.DrawerS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.docDropS"), AdmissionStation.DocDropS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.markerS"), AdmissionStation.MarkerS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.slipS"), AdmissionStation.SlipS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.slipDropS"), AdmissionStation.SlipDropS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.stripOutS"), AdmissionStation.StripOutS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.stripInS"), AdmissionStation.StripInS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.aidDrawerS"), AdmissionStation.AidDrawerS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.admitHoldS"), AdmissionStation.HoldS, 1e-6f);
            Assert.AreEqual(Token("admission.slipPathMm"), AdmissionStation.SlipPathMm, 1e-6f);
            Assert.AreEqual(Token("motion.hoverLiftMm"), AdmissionStation.HoverLiftMm, 1e-6f);
            Assert.AreEqual(0.63f, AdmissionStation.UnfoldTotalS, 1e-5f, "T1 0,15 + 0,4 + 0,08");
            Assert.AreEqual(0.58f, AdmissionStation.FoldTotalS, 1e-5f, "T4 0,08 + 0,35 + 0,15");
            Assert.AreEqual(Token("admission.motion.stampLiftS"), StampDecision.LiftS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.stampPressS"), StampDecision.PressS, 1e-6f);
            Assert.AreEqual(Token("motion.team.liftMm"), StampDecision.LiftMm, 1e-6f);
            Assert.AreEqual(Token("motion.board.returnArcS"), StampDecision.ReturnS, 1e-6f);
            Assert.AreEqual(Token("motion.board.denyShakeMm"), StampDecision.DenyMm, 1e-6f);
            Assert.AreEqual(Token("admission.motion.glassCoverS"), ColdGlassView.CoverS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.fogInS"), ColdGlassView.FogInS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.fogOutS"), ColdGlassView.FogOutS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.intercomCrackleS"), IntercomTool.CrackleS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.transcriptPerCharS"), IntercomTool.PerCharS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.padTearS"), RequestPadTool.TearS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.magnifierMoveS"), MagnifierTool.MoveS, 1e-6f);
        }

        // ================================================================== M2 · tezgâh

        [Test]
        public void M2_Counter_UnfoldDrawerTakeFold_DurationsAndExactEnds()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(false), false);
            Assert.AreEqual(CounterState.Folded, st.Counter);
            Assert.AreEqual(88f, b.top.Value, "katlı: Top 88");
            Assert.AreEqual(-90f, b.bracketL.Value, "BracketL −90");
            Assert.AreEqual(90f, b.bracketR.Value, "BracketR +90");
            Assert.AreEqual(0f, b.drawer.Value);
            Assert.IsFalse(st.LampOn);
            Assert.IsFalse(b.spot.activeSelf, "CounterSpot GameObject kapalı");
            Assert.AreSame(b.lampOff, b.lampBody.sharedMaterials[4], "cam sönük");
            Assert.IsFalse(b.loose.activeSelf, "katlı kanatta gevşek araç yok");
            Assert.IsFalse(b.doc.gameObject.activeSelf);
            Assert.IsEmpty(b.sounds, "ilk bağlama sessiz");

            // T1: konsollar 0,15 → kanat 0,4 → lamba 0,08
            b.Zero();
            st.Bind(Snap(true, "resident-9"), true);
            Assert.AreEqual(CounterState.Lowering, st.Counter);
            b.To(AdmissionStation.BracketS * 0.5f);
            Assert.Less(b.bracketL.Value, -30f, "konsollar yarıda açılıyor");
            Assert.AreEqual(88f, b.top.Value, "kanat konsollardan sonra");
            b.To(AdmissionStation.BracketS + Edge);
            Assert.AreEqual(0f, b.bracketL.Value, "BracketL tam 0");
            Assert.AreEqual(0f, b.bracketR.Value, "BracketR tam 0");
            float leafEnd = AdmissionStation.BracketS + AdmissionStation.LeafS;
            b.To(leafEnd - Edge);
            Assert.Greater(b.top.Value, 0f, "kanat iniyor");
            Assert.IsFalse(b.sounds.Contains(AdmissionSound.CounterDown));
            b.To(leafEnd + Edge);
            Assert.AreEqual(0f, b.top.Value, "Top tam 0");
            Assert.AreEqual(1, b.sounds.Count(x => x == AdmissionSound.CounterDown), "'tank' kanat inişinin sonunda bir kez");
            Assert.IsTrue(st.LampOn);
            Assert.IsTrue(b.spot.activeSelf);
            Assert.AreSame(b.lampOn, b.lampBody.sharedMaterials[4], "cam yanık");
            Assert.AreEqual(CounterState.Lowering, st.Counter, "lamba adımı 0,08 s");
            Assert.AreEqual(0f, b.drawer.Value, "çekmece tezgâh inmeden açılmaz");
            b.To(AdmissionStation.UnfoldTotalS + Edge);
            Assert.AreEqual(CounterState.Down, st.Counter);

            // T2: çekmece 0 → −0,22 (0,35), belge tablada
            Assert.IsTrue(b.sounds.Contains(AdmissionSound.Drawer));
            Assert.IsTrue(st.DocumentInDrawer);
            Assert.IsTrue(b.doc.gameObject.activeSelf);
            b.To(AdmissionStation.UnfoldTotalS + AdmissionStation.DrawerS + Edge);
            Assert.AreEqual(-0.22f, b.drawer.Value, "Drawer tam −0,22");
            Near(b.drawer.transform.TransformPoint(AdmissionStation.DocInDrawerLocal), b.doc.position, 1e-5f, "belge çekmece tablasında");

            // T3: belge Doc'a (0,2), sonra çekmece kapanır (0,35)
            b.Zero();
            b.sounds.Clear();
            st.TakeDocument();
            Assert.AreEqual(1, b.taken, "DocumentTaken");
            CollectionAssert.Contains(b.sounds, AdmissionSound.DocDrop);
            b.To(AdmissionStation.DocDropS + Edge);
            Assert.IsTrue(st.DocumentOnCounter);
            Near(b.top.transform.TransformPoint(AdmissionStation.DocOnCounterLocal), b.doc.position, 1e-5f, "belge tezgâhta");
            Assert.AreEqual(-0.22f, b.drawer.Value, 1e-3f, "çekmece belge inince kapanmaya başlar");
            b.To(AdmissionStation.DocDropS + AdmissionStation.DrawerS + Edge);
            Assert.AreEqual(0f, b.drawer.Value, "Drawer tam 0");

            // T4: lamba söner (0,08) → Top 88 (0,35) → konsollar (0,15)
            b.Zero();
            b.sounds.Clear();
            st.Bind(Snap(false), true);
            Assert.AreEqual(CounterState.Folding, st.Counter);
            Assert.IsFalse(st.LampOn, "lamba katlanışın başında söner");
            Assert.IsFalse(b.spot.activeSelf);
            Assert.IsFalse(b.loose.activeSelf, "araçlar T4 başında gizli");
            CollectionAssert.AreEqual(new[] { AdmissionSound.CounterFold }, b.sounds);
            b.To(AdmissionStation.LampS - Edge);
            Assert.AreEqual(0f, b.top.Value, "kanat lambadan sonra");
            float foldEnd = AdmissionStation.LampS + AdmissionStation.FoldS;
            b.To(foldEnd - Edge);
            Assert.Less(b.top.Value, 88f);
            Assert.AreEqual(0f, b.bracketL.Value, "konsollar kanattan sonra");
            b.To(foldEnd + Edge);
            Assert.AreEqual(88f, b.top.Value, "Top tam 88");
            b.To(AdmissionStation.FoldTotalS + Edge);
            Assert.AreEqual(-90f, b.bracketL.Value, "BracketL tam −90");
            Assert.AreEqual(90f, b.bracketR.Value, "BracketR tam +90");
            Assert.AreEqual(CounterState.Folded, st.Counter);
            Assert.IsFalse(st.Moving);
        }

        [Test]
        public void Tools_HiddenWhenFolded_ShownBelow30Degrees()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(false), false);
            st.Bind(Snap(true), true);
            bool seenHiddenLow = false;
            while (st.Counter != CounterState.Down)
            {
                b.Advance(Dt);
                if (b.top.Value >= AdmissionStation.ToolsVisibleBelowDeg) Assert.IsFalse(b.loose.activeSelf, "Top ≥ 30°: araçlar gizli (" + b.top.Value + ")");
                else if (!b.loose.activeSelf) seenHiddenLow = true;
                Assert.Less(b.time, 2f);
            }
            Assert.IsFalse(seenHiddenLow, "Top 30°'nin altına inince görünür");
            Assert.IsTrue(b.loose.activeSelf);
            st.Bind(Snap(false), true);
            Assert.IsFalse(b.loose.activeSelf, "T4 başında gizlenir");
            Assert.IsFalse(b.deskKey.enabled, "gizli MASA tuşunun çarpıştırıcısı kapalı");
        }

        [Test]
        public void T5_FirstBindAndSnap_NoAnimationNoSound()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(true, "resident-9", false), true);   // ilk bağlama: animasyonsuz
            Assert.AreEqual(CounterState.Down, st.Counter);
            Assert.AreEqual(0f, b.top.Value);
            Assert.AreEqual(-0.22f, b.drawer.Value, "aday var, belge çekmecede: çekmece açık");
            Assert.IsTrue(st.LampOn);
            Assert.IsTrue(b.loose.activeSelf);
            Assert.IsEmpty(b.sounds);
            Assert.IsFalse(st.Moving);

            // Katlanırken animasyonsuz bağlama sona atlar
            st.Bind(Snap(false), true);
            b.To(0.2f);
            st.Bind(Snap(false), false);
            Assert.AreEqual(CounterState.Folded, st.Counter);
            Assert.AreEqual(88f, b.top.Value);
            Assert.AreEqual(-90f, b.bracketL.Value);
            Assert.AreEqual(0f, b.drawer.Value);
            Assert.IsFalse(b.doc.gameObject.activeSelf);
            Assert.IsFalse(st.Moving);

            // Belge tezgâhta (kayıttan devam): çekmece kapalı
            st.Bind(Snap(true, "resident-9", true), false);
            Assert.IsTrue(st.DocumentOnCounter);
            Assert.AreEqual(0f, b.drawer.Value);
            Near(b.top.transform.TransformPoint(AdmissionStation.DocOnCounterLocal), b.doc.position, 1e-5f, "belge tezgâhta");
        }

        // ================================================================== M4 · işaretçi

        [Test]
        public void M4_Marker_AlignsToRuleIn02_JewelLit_ReturnsTopOnOrdersChange()
        {
            var b = Build();
            var st = b.station;
            st.RuleCenterMm = i => 32f + 26f * i;   // v2 TR ölçümü (C §2.5a): 32 / 58 / 84 mm
            st.Bind(Snap(true, "resident-9", true), false);
            Assert.AreEqual(0f, b.marker.Value);
            Assert.AreEqual(0, b.jewel.SwapState, "yakut sönük");

            b.Zero();
            st.Bind(Snap(true, "resident-9", true, markerRule: 1), true);
            CollectionAssert.Contains(b.sounds, AdmissionSound.Marker);
            Assert.AreEqual(1, b.jewel.SwapState, "yakut yanar");
            b.To(AdmissionStation.MarkerS - Edge);
            Assert.Greater(b.marker.Value, -0.040f);
            b.To(AdmissionStation.MarkerS + Edge);
            Assert.AreEqual(-(58f - AdmissionStation.MarkerZeroBelowStripTopMm) / 1000f, b.marker.Value, 1e-6f, "y = −(58 − 18)/1000");

            // Alt sınır −0,17
            st.RuleCenterMm = i => 300f;
            b.Advance(Dt);
            Assert.AreEqual(-0.17f, b.marker.Value, 1e-6f, "alt sınır");

            // Talimat değişince en üste (0,2 s), yakut söner
            st.RuleCenterMm = i => 32f + 26f * i;
            b.To(b.time + 0.1f);
            b.Zero();
            st.PlayOrdersChange(Orders(1), Orders(2), null);
            Assert.AreEqual(0, b.jewel.SwapState);
            b.To(AdmissionStation.MarkerS + Edge);
            Assert.AreEqual(0f, b.marker.Value, "işaretçi en üstte");
        }

        // ================================================================== M5 · fiş

        [Test]
        public void M5_Slip_FromMouthIn035_ThenClip_OncePerKey()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(true, "resident-9", true), false);
            var slip = Slip("cand:resident-9:papers");
            b.Zero();
            st.Bind(Snap(true, "resident-9", true, -1, 1, slip), true);
            var paper = b.slips[0];
            Assert.IsFalse(paper.gameObject.activeSelf, "çıkış saati gelmeden görünmez");
            b.Advance(Dt);
            Assert.IsTrue(paper.gameObject.activeSelf);
            Assert.AreEqual(1, b.sounds.Count(x => x == AdmissionSound.SlipOut));
            var mouth = b.panel.transform.TransformPoint(AdmissionStation.SlipMouthOut);
            float path = AdmissionStation.SlipPathMm / 1000f;
            Assert.AreEqual(path, (mouth - paper.position).magnitude, 0.01f, "160 mm'lik yolun başında (ağzın 40 mm içi)");
            // slipS EaseOut (kübik): kalan yol = yol × (1 − e(u)); erken varmaz. 0,35'ten 2,5 kare önce kalan yalnız ~2 µm olduğundan
            // sabit eşik (1e-4) yerine eğrinin beklenen değeriyle karşılaştırılır (istasyon saati b.time ile aynı adımlarla ilerler).
            b.To(0.8f * AdmissionStation.SlipS);
            float left = path * (1f - CardEasing.Evaluate(CardEase.EaseOut, b.time / AdmissionStation.SlipS));
            Assert.AreEqual(left, (mouth - paper.position).magnitude, 1e-5f, "0,8 slipS: EaseOut eğrisinde (kalan ~1,3 mm)");
            b.To(AdmissionStation.SlipS - Edge);
            left = path * (1f - CardEasing.Evaluate(CardEase.EaseOut, b.time / AdmissionStation.SlipS));
            Assert.Greater((mouth - paper.position).magnitude, 0.5f * left, "0,35'ten önce ağza varmadı");
            b.To(AdmissionStation.SlipS + 0.5f * Dt);
            Near(mouth, paper.position, 2e-3f, "0,35 s'de ağızda");
            b.To(AdmissionStation.SlipS + AdmissionStation.SlipDropS + Edge);
            var clip = b.counter.transform.TransformPoint(AdmissionStation.ClipLocal(0, 1));
            Near(clip, paper.position, 1e-5f, "kıskaçta (uç tam)");
            Assert.Less(Vector3.Angle(b.counter.transform.up, paper.rotation * Vector3.back), 0.01f, "kıskaçta yüzü yukarı");
            CollectionAssert.AreEqual(new[] { "cand:resident-9:papers" }, st.SlipKeys);

            // Aynı anahtar ikinci kez çıkmaz (yeniden bağlama ve PlayInspection)
            st.Bind(Snap(true, "resident-9", true, -1, 1, slip), true);
            st.PlayInspection(AdmissionTool.Papers, slip);
            b.To(b.time + 1f);
            Assert.AreEqual(1, b.sounds.Count(x => x == AdmissionSound.SlipOut));

            // Aynı anda gelen iki fiş sırayla çıkar
            b.Zero();
            b.sounds.Clear();
            st.Bind(Snap(true, "resident-9", true, -1, 1, slip, Slip("cand:resident-9:interview", AdmissionTool.Interview), Slip("cand:resident-9:breath", AdmissionTool.Breath)), true);
            b.Advance(Dt);
            Assert.AreEqual(1, b.sounds.Count(x => x == AdmissionSound.SlipOut), "ikincisi birincinin ardından");
            b.To(AdmissionStation.SlipS + AdmissionStation.SlipDropS + Edge);
            Assert.AreEqual(2, b.sounds.Count(x => x == AdmissionSound.SlipOut));
            b.To(2f * (AdmissionStation.SlipS + AdmissionStation.SlipDropS) + Edge);
            Assert.IsFalse(st.Moving);
            for (int i = 0; i < 3; i++)
                Near(b.counter.transform.TransformPoint(AdmissionStation.ClipLocal(i, 3)), b.slips[i].position, 1e-5f, "yelpaze " + i);
        }

        /// <summary>A10 G06 (R17_inspect): cetvel belgedeyken karşılaştırma fişi ağızda kalır, okunur; cetvel kalkınca kıskaca düşer.</summary>
        [Test]
        public void PapersSlip_StaysAtMouth_WhileRulerOnDocument_ThenDropsToClip()
        {
            var b = Build(true);
            var st = b.station;
            var mag = b.magnifier;
            st.Bind(Snap(true, "resident-9", true), false);
            var slip = Slip("cand:resident-9:papers");
            b.Zero();
            mag.Bind(new ToolData { Enabled = false, Used = true });
            mag.PlayInspect("serial", null);
            st.PlayInspection(AdmissionTool.Papers, slip);
            st.Bind(Snap(true, "resident-9", true, -1, 1, slip), true);
            var paper = b.slips[0];
            var mouth = b.panel.transform.TransformPoint(AdmissionStation.SlipMouthOut);
            var clip = b.counter.transform.TransformPoint(AdmissionStation.ClipLocal(0, 1));

            b.To(AdmissionStation.SlipS + AdmissionStation.SlipDropS + MagnifierTool.MoveS + MagnifierTool.CarriageS + Edge);
            Assert.AreEqual(MagnifierTool.MagnifierState.Inspect, mag.State);
            Assert.IsFalse(st.Moving, "fiş ağızda durdu");
            Near(mouth, paper.position, 1e-5f, "cetvel belgedeyken fiş ağızda (R17_inspect)");

            // Okuma pozu ağızdaki fişi hedefler
            var read = st.ReadPose(AdmissionPaper.Slip, 0, 0.318f);
            Assert.Less(Vector3.Cross(read.rotation * Vector3.forward, mouth - read.position).magnitude / 0.318f, 0.01f, "K8R ağızdaki fişe bakar");

            // Cetvel kalkınca kıskaca düşer (slipDropS)
            float t0 = b.time;
            mag.ReturnToRest(true);
            b.To(t0 + AdmissionStation.SlipDropS + 2f * Edge);
            Near(clip, paper.position, 1e-5f, "cetvel kalkınca kıskaçta");
            Assert.AreEqual(1, b.sounds.Count(x => x == AdmissionSound.SlipOut), "düşüş yeni çıkış sayılmaz");

            // Kıskaçtaki fiş, cetvel anında belgeye konunca (fikstür) ağza geçer
            mag.SnapInspect("serial");
            b.Advance(Dt);
            Near(mouth, paper.position, 1e-5f, "SnapInspect sonrası ağızda");
        }

        /// <summary>A10 G09: üstteki fişin kâğıdı alttaki fişin yazı yüzünün (kâğıt + 0,4 mm) üstünde; yazı içinden okunmaz.</summary>
        [Test]
        public void ClipFan_UpperPaper_ClearsLowerFace()
        {
            const float faceOffsetM = 0.0004f;
            Assert.Greater(AdmissionStation.ClipLocal(1, 2).y - AdmissionStation.ClipLocal(0, 2).y, faceOffsetM + 1e-4f);
        }

        [Test]
        public void PlayInspection_BeforeBind_EmergesOnce_AndTakesTheDocument()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(true, "resident-9", false), false);
            Assert.IsTrue(st.DocumentInDrawer);
            var slip = Slip("cand:resident-9:interview", AdmissionTool.Interview);
            st.PlayInspection(AdmissionTool.Interview, slip);
            Assert.AreEqual(1, b.taken, "ilk araç kullanımı belgeyi tezgâha alır (T3)");
            st.Bind(Snap(true, "resident-9", true, -1, 1, slip), true);
            b.To(1f);
            Assert.AreEqual(1, b.sounds.Count(x => x == AdmissionSound.SlipOut), "önce PlayInspection, sonra Bind: tek çıkış");
            Assert.IsTrue(st.DocumentOnCounter);
        }

        // ================================================================== M6 · interkom

        [Test]
        public void M6_Intercom_Button4mm_Crackle02_LineForTranscript()
        {
            var box = Model("door", "IntercomBox", null, Vector3.zero);
            var led = Part(box, "Led").GetComponent<Renderer>();
            var on = Mat("jewelAmberOn test");
            var off = Mat("jewelAmberOff test");
            var hit = box.AddComponent<BoxCollider>();
            var tool = box.AddComponent<IntercomTool>();
            tool.Configure(Part(box, "Button"), led, on, off, hit);
            var sounds = new List<AdmissionSound>();
            tool.Sound += s => sounds.Add(s);
            int used = 0;
            tool.Used += () => used++;
            tool.Bind(new ToolData { Enabled = true, Title = "İNTERKOM", Reason = "görüşme" });
            tool.SetInteractive(true);

            var body = new string('a', 145);
            Assert.AreEqual(4.35f, IntercomTool.TranscriptSeconds(body), 1e-4f, "145 harf × 0,03");
            tool.OnPointerClick(Pointer(Vector2.zero, box));
            Assert.AreEqual(1, used);

            int done = 0;
            float time = 0f;
            tool.Play(IntercomTool.CrackleS + IntercomTool.TranscriptSeconds(body), () => done++);
            CollectionAssert.AreEqual(new[] { AdmissionSound.IntercomCrackle }, sounds);
            Assert.IsTrue(tool.LineOpen);
            Assert.AreSame(on, led.sharedMaterial, "hat açık: LED amber");
            void To(float t) { while (time < t - 1e-6f) { tool.Advance(Dt); time += Dt; } }
            To(IntercomTool.PressS + Edge);
            Assert.AreEqual(0.004f, Part(box, "Button").Value, "Button tam 4 mm");
            To(IntercomTool.CrackleS - Edge);
            Assert.AreEqual(0, done);
            To(IntercomTool.CrackleS + Edge);
            Assert.AreEqual(1, done, "cızırtı 0,2 s");
            To(IntercomTool.CrackleS + IntercomTool.PressS + Edge);
            Assert.AreEqual(0f, Part(box, "Button").Value, "düğme bırakıldı");
            Assert.IsTrue(tool.LineOpen, "döküm boyunca hat açık");
            To(IntercomTool.CrackleS + 4.35f + Edge);
            Assert.IsFalse(tool.LineOpen);
            Assert.AreSame(off, led.sharedMaterial);

            // Kilitliyken olay yok, ret sesi
            tool.Bind(new ToolData { Enabled = false, Used = true, Reason = "Bu inceleme zaten yapıldı." });
            tool.OnPointerClick(Pointer(Vector2.zero, box));
            Assert.AreEqual(1, used);
            Assert.AreEqual(AdmissionSound.Deny, sounds.Last());
            Assert.AreEqual("Bu inceleme zaten yapıldı.", tool.Line);
        }

        // ================================================================== M7 · buğu camı

        ColdGlassView Glass(out PartDriver cover, out Renderer fog)
        {
            var glass = Model("admission", "ColdGlass", null, Vector3.zero);
            cover = Part(glass, "Cover");
            var fogPart = Part(glass, "Fog");
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(fogPart.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, -0.0001f);
            quad.transform.localScale = new Vector3(0.108f, 0.108f, 1f);
            fog = quad.GetComponent<Renderer>();
            fog.sharedMaterial = Mat("Cold fog test");
            var view = glass.AddComponent<ColdGlassView>();
            view.Configure(cover, fog, glass.AddComponent<BoxCollider>());
            return view;
        }

        [Test]
        public void M7_ColdGlass_Cover105In02_FogIn06_Out25_ClearStaysClear()
        {
            var view = Glass(out var cover, out var fog);
            var sounds = new List<AdmissionSound>();
            view.Sound += s => sounds.Add(s);
            Assert.IsFalse(fog.enabled, "başta buğu yok");
            int done = 0;
            float time = 0f;
            void To(float t) { while (time < t - 1e-6f) { view.Advance(Dt); time += Dt; } }

            view.Play(true, () => done++);
            CollectionAssert.AreEqual(new[] { AdmissionSound.GlassCover }, sounds);
            To(ColdGlassView.CoverS - Edge);
            Assert.Less(cover.Value, 105f);
            Assert.AreEqual(0f, view.FogAlpha, "buğu kapak açılınca");
            To(ColdGlassView.CoverS + Edge);
            Assert.AreEqual(105f, cover.Value, "Cover tam 105°");
            Assert.AreEqual(AdmissionSound.Fog, sounds.Last());
            float peak = ColdGlassView.CoverS + ColdGlassView.FogInS;
            To(peak - Edge);
            Assert.Less(view.FogAlpha, 1f);
            Assert.AreEqual(0, done);
            To(peak + 0.5f * Dt);
            Assert.Greater(view.FogAlpha, 0.99f, "0,6 s'de tepede");
            Assert.AreEqual(1, done, "done buğunun tepesinde");
            Assert.IsTrue(fog.enabled);
            To(peak + ColdGlassView.FogOutS - Edge);
            Assert.Greater(view.FogAlpha, 0f);
            To(peak + ColdGlassView.FogOutS + Edge);
            Assert.AreEqual(0f, view.FogAlpha, "2,5 s'de söndü (tam)");
            Assert.AreEqual(ColdGlassView.FogEndScale, view.FogScale, 1e-6f, "kenardan söner: ölçek 0,85");
            Assert.IsFalse(fog.enabled);
            Assert.AreEqual(105f, cover.Value, "kapak çıkışa dek açık");

            view.Close();
            time = 0f;
            To(ColdGlassView.CoverS + Edge);
            Assert.AreEqual(0f, cover.Value, "Close 0,2 s");

            // Core "clear": alfa hep 0
            view.ResetClosed();
            done = 0;
            time = 0f;
            view.Play(false, () => done++);
            float maxAlpha = 0f;
            while (time < 4f) { view.Advance(Dt); time += Dt; maxAlpha = Mathf.Max(maxAlpha, view.FogAlpha); if (time > ColdGlassView.CoverS + Edge) Assert.AreEqual(1, done); }
            Assert.AreEqual(0f, maxAlpha, "berrak cam");
            Assert.IsFalse(fog.enabled);
            Assert.AreEqual(105f, cover.Value);
        }

        // ================================================================== M8 · damga

        Bench StampBench(out StampDecision stamp)
        {
            var b = Build(true);
            b.station.Bind(Snap(true, "resident-9", true), false);
            b.station.SetInteractive(true);
            foreach (var s in b.stamps) s.Bind(new StampData { Enabled = true, Plate = "KISITLI\nKABUL", BandColor = "#A9520A" });
            stamp = b.stamps[(int)StampChoice.Restricted];
            Physics.SyncTransforms();
            return b;
        }

        [Test]
        public void M8_Stamp_Lift25mmIn01_Press012_ThenUsedPose()
        {
            var b = StampBench(out var stamp);
            var presses = new List<StampChoice>();
            stamp.Pressed += c => presses.Add(c);
            var rest = stamp.transform.position;
            var restScreen = Screen(b.cam, rest);
            Assert.IsFalse(stamp.LockTongue.activeSelf, "açık damgada pirinç dil yok");

            var up = b.top.transform.up;
            float restHeight = Height(b, rest);
            b.Zero();
            stamp.OnBeginDrag(Pointer(restScreen, stamp.gameObject));
            Assert.IsTrue(stamp.Held);
            Assert.AreEqual(AdmissionSound.StampLift, b.sounds.Last());
            Assert.AreEqual(restHeight + 0.025f, stamp.HeldHeight, 1e-5f, "raftaki yüksekliğin 25 mm üstü");
            b.To(StampDecision.LiftS - Edge);
            float h0 = Height(b, stamp.transform.position);
            Assert.Greater(h0, restHeight);
            Assert.Less(h0, restHeight + 0.025f);
            b.To(StampDecision.LiftS + Edge);
            Assert.AreEqual(StampDecision.StampState.Held, stamp.State);
            Assert.AreEqual(restHeight + 0.025f, Height(b, stamp.transform.position), 1e-5f, "25 mm kalkık");
            Assert.Less(Vector2.Distance(restScreen, Screen(b.cam, stamp.transform.position)), 0.5f, "imlecin altında");

            // Damga alanına sürükle-bırak → 0,12 s basış
            var target = b.station.StampPressPose();
            var targetScreen = Aim(b, stamp, target.position);
            stamp.OnDrag(Pointer(targetScreen, stamp.gameObject));
            Assert.IsTrue(stamp.DropPoint(targetScreen, out var dropPoint));
            Assert.IsTrue(b.station.StampAreaContains(dropPoint), "bırakış damga alanında");
            b.Zero();
            stamp.OnEndDrag(Pointer(targetScreen, stamp.gameObject));
            Assert.AreEqual(StampDecision.StampState.Pressing, stamp.State);
            b.To(StampDecision.PressS - Edge);
            Assert.IsEmpty(presses, "basış bitmeden komut yok");
            b.To(StampDecision.PressS + 0.5f * Dt);
            CollectionAssert.AreEqual(new[] { StampChoice.Restricted }, presses, "basış anında bir kez");
            Assert.AreEqual(AdmissionSound.StampPress, b.sounds.Last(), "'tak'");
            Near(target.position, stamp.transform.position, 2e-4f, "belgenin damga alanında");
            b.To(StampDecision.PressS + StampDecision.ReturnS + Edge);
            Assert.AreEqual(StampDecision.StampState.Used, stamp.State);
            Near(b.top.transform.TransformPoint(StampDecision.UsedLocal), stamp.transform.position, 1e-5f, "kullanılmış poz");
            Assert.IsFalse(stamp.Hit.enabled, "kullanılmış damga tıklanmaz");

            // Tezgâh katlanırken rafına döner
            b.station.Bind(Snap(false), true);
            Assert.AreEqual(StampDecision.StampState.Rack, stamp.State);
            Near(rest, stamp.transform.position, 1e-5f, "rafında");
        }

        [Test]
        public void M8_Stamp_DropElsewhere_ReturnsInArc_AndLockedShakes()
        {
            var b = StampBench(out var stamp);
            int presses = 0;
            stamp.Pressed += c => presses++;
            var rest = stamp.transform.position;
            var restScreen = Screen(b.cam, rest);
            stamp.OnBeginDrag(Pointer(restScreen, stamp.gameObject));
            b.To(b.time + StampDecision.LiftS + Edge);
            var elsewhere = Aim(b, stamp, b.top.transform.TransformPoint(new Vector3(-0.3f, 0.0043f, -0.25f)));
            Assert.IsTrue(stamp.DropPoint(elsewhere, out var p));
            Assert.IsFalse(b.station.StampAreaContains(p));
            b.Zero();
            stamp.OnDrag(Pointer(elsewhere, stamp.gameObject));
            stamp.OnEndDrag(Pointer(elsewhere, stamp.gameObject));
            Assert.AreEqual(StampDecision.StampState.Returning, stamp.State);
            Assert.AreEqual(AdmissionSound.StampReturn, b.sounds.Last());
            b.To(StampDecision.ReturnS + Edge);
            Assert.AreEqual(StampDecision.StampState.Rack, stamp.State);
            Near(rest, stamp.transform.position, 1e-5f, "rafına döndü");
            Assert.AreEqual(0, presses);

            // Kilitli: dil görünür, kalkmaz, 2 mm titrer + ret sesi
            stamp.Bind(new StampData { Enabled = false, Plate = "KISITLI\nKABUL", Reason = "Tek gözetim yeri dolu; önce mevcut kısıtlı kabulü çöz." });
            Assert.IsTrue(stamp.LockTongue.activeSelf, "pirinç dil");
            Assert.AreEqual("Tek gözetim yeri dolu; önce mevcut kısıtlı kabulü çöz.", stamp.Line, "üzerine gelme satırı Core iletisi");
            Assert.AreEqual("KISITLI KABUL", stamp.HoverTitle);
            b.Zero();
            stamp.OnPointerClick(Pointer(restScreen, stamp.gameObject));
            stamp.OnBeginDrag(Pointer(restScreen, stamp.gameObject));
            Assert.IsFalse(stamp.Held);
            Assert.AreEqual(AdmissionSound.Deny, b.sounds.Last());
            float peak = 0f;
            while (b.time < StampDecision.DenyS + Edge) { b.Advance(Dt); peak = Mathf.Max(peak, Mathf.Abs(stamp.ShakeOffsetMm)); }
            Assert.AreEqual(2f, peak, 0.05f, "2 mm titreme");
            Near(rest, stamp.transform.position, 1e-6f, "titreme biter, rafında");
        }

        [Test]
        public void M8_Stamp_RefusedCommand_ReturnsToRack()
        {
            var b = StampBench(out var stamp);
            stamp.Pressed += c => stamp.ReturnToRack(true);   // sunucu: komut reddi → rafına
            var rest = stamp.transform.position;
            stamp.OnBeginDrag(Pointer(Screen(b.cam, rest), stamp.gameObject));
            var targetScreen = Aim(b, stamp, b.station.StampPressPose().position);
            stamp.OnDrag(Pointer(targetScreen, stamp.gameObject));
            stamp.OnEndDrag(Pointer(targetScreen, stamp.gameObject));
            b.Zero();
            b.To(StampDecision.PressS + 0.5f * Dt);
            Assert.AreEqual(StampDecision.StampState.Returning, stamp.State, "kullanılmış poza gitmez");
            b.To(StampDecision.PressS + StampDecision.ReturnS + Edge);
            Near(rest, stamp.transform.position, 1e-5f, "rafında");
        }

        [Test]
        public void Stamp_ClickClick_AndEscReturns()
        {
            var b = StampBench(out var stamp);
            int presses = 0;
            stamp.Pressed += c => presses++;
            var rest = stamp.transform.position;
            var restScreen = Screen(b.cam, rest);

            // Tık: elde; imleci izler (25 mm yukarıda)
            stamp.OnPointerClick(Pointer(restScreen, stamp.gameObject));
            Assert.IsTrue(stamp.Held);
            Assert.AreSame(stamp, b.station.HeldStamp);
            var target = b.station.StampPressPose();
            var targetScreen = Aim(b, stamp, target.position);
            stamp.Follow(targetScreen);
            b.To(b.time + StampDecision.LiftS + Edge);
            Assert.IsTrue(stamp.DropPoint(targetScreen, out var over));
            Near(over + b.top.transform.up * stamp.HeldHeight, stamp.transform.position, 1e-5f, "imlecin altında, tutma yüksekliğinde");
            Assert.IsTrue(b.station.StampAreaContains(over));

            // İkinci tık belgede (istasyona düşer) → bas
            b.station.OnPointerClick(Pointer(targetScreen, b.doc.gameObject, over));
            Assert.AreEqual(StampDecision.StampState.Pressing, stamp.State);
            b.To(b.time + StampDecision.PressS + Edge);
            Assert.AreEqual(1, presses);

            // Tık → elde → Esc (sunucu ReturnHeldStamp) → rafına 0,25 s
            var other = b.stamps[(int)StampChoice.Reject];
            var otherRest = other.transform.position;
            other.OnPointerClick(Pointer(Screen(b.cam, otherRest), other.gameObject));
            Assert.IsTrue(other.Held);
            b.Zero();
            Assert.IsTrue(b.station.ReturnHeldStamp());
            Assert.IsFalse(other.Held);
            Assert.IsFalse(b.station.ReturnHeldStamp(), "elde damga yok");
            b.To(StampDecision.ReturnS + Edge);
            Near(otherRest, other.transform.position, 1e-5f, "rafında");

            // Tık → elde → belge dışına tık → rafına
            other.OnPointerClick(Pointer(Screen(b.cam, otherRest), other.gameObject));
            b.To(b.time + StampDecision.LiftS + Edge);
            other.ClickAt(b.top.transform.TransformPoint(new Vector3(-0.3f, 0.0043f, -0.25f)));
            Assert.AreEqual(StampDecision.StampState.Returning, other.State);
            Assert.AreEqual(1, presses);
        }

        // ================================================================== DIŞ KAYIT defteri

        [Test]
        public void RequestSheet_LiftsUpward_TearsAndFliesToOutSlot()
        {
            var b = Build(true);
            var pad = b.pad;
            pad.SetInteractive(true);
            var sounds = new List<AdmissionSound>();
            pad.Sound += s => sounds.Add(s);
            int used = 0;
            pad.Used += () => used++;
            pad.Bind(new ToolData { Enabled = true, Title = "TALEP DEFTERİ", Reason = "dış kayıt" });
            var request = new RequestData { Sent = false, Supervisor = "Cem" };
            pad.Bind(request);
            Assert.AreEqual(RequestPadTool.PadState.OnPad, pad.State);
            Assert.IsNull(pad.CarbonPaper, "gönderilmeden karbon görünmez");
            pad.OnPointerClick(Pointer(Vector2.zero, pad.gameObject));
            Assert.AreEqual(1, used);

            var sheet = pad.Sheet.transform;
            // Ön kenar defterin çerçevesinde ölçülür: tezgâh bağlanmadığından Top katlı (88°) ve dünya y'si "defterden yukarı" değildir.
            var padFrame = pad.transform;
            var frontEdge0 = padFrame.InverseTransformPoint(sheet.TransformPoint(new Vector3(0f, 0f, -0.14f)));
            int done = 0;
            b.Zero();
            pad.PlaySend(() => done++);
            b.To(RequestPadTool.TearS - Edge);
            Assert.Less(pad.SheetAngle, RequestPadTool.LiftDeg);
            var frontEdge = padFrame.InverseTransformPoint(sheet.TransformPoint(new Vector3(0f, 0f, -0.14f)));
            Assert.Greater(frontEdge.y, frontEdge0.y + 0.05f, "yaprak defterden yukarı kalkar (+35°, bloğa gömülmez)");
            b.To(RequestPadTool.TearS - 1.5f * Dt);
            float angle = Quaternion.Angle(Quaternion.identity, sheet.localRotation);
            Assert.Greater(angle, 30f);
            Assert.IsFalse(sounds.Contains(AdmissionSound.PadTear));
            b.To(RequestPadTool.TearS + Edge);
            CollectionAssert.Contains(sounds, AdmissionSound.PadTear);
            Assert.AreEqual(RequestPadTool.PadState.Flying, pad.State);
            b.To(RequestPadTool.TearS + RequestPadTool.FlyS + Edge);
            Assert.AreEqual(RequestPadTool.PadState.Outgoing, pad.State);
            Assert.AreEqual(1, done);
            var outPose = pad.OutgoingPose;
            Near(outPose.position, pad.OutgoingPaper.position, 1e-5f, "GİDEN ağzında");
            Assert.Less(Vector3.Angle(b.panel.transform.TransformDirection(RequestPadTool.OutgoingNormal.normalized), pad.OutgoingPaper.rotation * Vector3.back), 0.05f, "normal (0; 0,26; −0,966)");
            Assert.IsNotNull(pad.CarbonPaper, "karbon defterde");
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, sheet.localRotation), 0.01f, "defterin yaprağı yerine");

            // Gönderilmiş durumda bağlama yerinde tutar; deftere tık karbonu okur
            var papers = new List<AdmissionPaper>();
            pad.PaperClicked += k => papers.Add(k);
            pad.Bind(new RequestData { Sent = true, Supervisor = "Cem" });
            Assert.AreEqual(RequestPadTool.PadState.Outgoing, pad.State);
            pad.OnPointerClick(Pointer(Vector2.zero, pad.gameObject));
            CollectionAssert.AreEqual(new[] { AdmissionPaper.Carbon }, papers);
            Assert.AreEqual(1, used, "gönderilmiş defter komut istemez");
            // Sonuç geldi: yaprak GİDEN'den kalkar, karbon kalır
            pad.Bind(new RequestData { Sent = true, Supervisor = "Cem" }, false);
            Assert.AreEqual(RequestPadTool.PadState.OnPad, pad.State);
            Assert.IsFalse(pad.OutgoingPaper != null);
            Assert.IsNotNull(pad.CarbonPaper);
            // Yeni aday: temiz defter
            pad.Bind(new RequestData { Sent = false, Supervisor = "İpek" });
            Assert.IsNull(pad.CarbonPaper);
        }

        [Test]
        public void RequestPad_LiveBindThenPlaySend_AnimatesOnce_FirstBindSnaps_NoSupervisorLocked()
        {
            var b = Build(true);
            var pad = b.pad;
            pad.SetInteractive(true);
            pad.Bind(new ToolData { Enabled = true });
            pad.Bind(new RequestData { Sent = false });
            // Canlı: komut → Changed → Bind(Sent) → PlaySend (aynı kare)
            int done = 0;
            b.Zero();
            pad.Bind(new RequestData { Sent = true });
            Assert.AreEqual(RequestPadTool.PadState.Lifting, pad.State, "gönderim animasyonla başlar (anında oturmaz)");
            pad.PlaySend(() => done++);
            b.To(RequestPadTool.TearS + RequestPadTool.FlyS + Edge);
            Assert.AreEqual(1, done);
            Assert.AreEqual(RequestPadTool.PadState.Outgoing, pad.State);

            // Kayıttan devam: ilk bağlama anında GİDEN'de
            var b2 = Build(true);
            b2.pad.Bind(new RequestData { Sent = true });
            Assert.AreEqual(RequestPadTool.PadState.Outgoing, b2.pad.State);

            // Gözetmensiz: kalkmaz, kısa etiket, ret
            var sounds = new List<AdmissionSound>();
            b2.pad.Sound += s => sounds.Add(s);
            int used = 0;
            b2.pad.Used += () => used++;
            b2.pad.Bind(new RequestData { Sent = false, SupervisorMissing = true });
            b2.pad.Bind(new ToolData { Enabled = false, Title = "TALEP DEFTERİ", Reason = "gözetmen yok · BOŞTA boş" });
            b2.pad.SetInteractive(true);
            b2.pad.OnPointerClick(Pointer(Vector2.zero, b2.pad.gameObject));
            b2.pad.OnBeginDrag(Pointer(Vector2.zero, b2.pad.gameObject));
            Assert.AreEqual(0, used);
            Assert.AreEqual(0f, b2.pad.SheetAngle, "defter kalkmaz");
            Assert.AreEqual(AdmissionSound.Deny, sounds.Last());
            Assert.AreEqual("gözetmen yok · BOŞTA boş", b2.pad.Line);
        }

        // ================================================================== büyüteç

        [Test]
        public void Magnifier_PlayInspect_MoveThenCarriage_FieldsFromCoreCodes()
        {
            // Alan tablosu DA'nın pozunu verir: SEVK No. → araba 0,158, kök (0,03; 0,0048; −0,027) −3°
            MagnifierTool.InspectFor("serial", out var lp, out var lr, out float c);
            Assert.AreEqual(MagnifierTool.InspectCarriage, c, 1e-5f);
            Near(MagnifierTool.InspectLocal, lp, 1e-5f, "DA:124 inceleme pozu");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, MagnifierTool.InspectYaw, 0f), lr), 0.01f);
            MagnifierTool.InspectFor("match", out _, out _, out float cm);
            Assert.AreEqual(c, cm, 1e-6f, "uyumlu belgede SEVK No.");
            foreach (var code in new[] { "date", "seal", "origin" })
            {
                MagnifierTool.InspectFor(code, out _, out _, out float cc);
                Assert.GreaterOrEqual(cc, 0f, code);
                Assert.LessOrEqual(cc, 0.22f, code);
                var lens = MagnifierTool.FieldLensMm(code);
                Assert.LessOrEqual(Mathf.Abs(lens.x), AdmissionStation.DocSizeMm.x / 2f, code + " belgenin içinde");
                Assert.LessOrEqual(Mathf.Abs(lens.y), AdmissionStation.DocSizeMm.y / 2f, code);
            }

            var b = Build(true);
            var mag = b.magnifier;
            var carriage = mag.Carriage;
            mag.Bind(new ToolData { Enabled = true });
            var rest = mag.transform.position;
            int done = 0;
            b.Zero();
            // Canlı sıra: Bind(Used) → PlayInspect aynı karede, tek hareket
            mag.Bind(new ToolData { Enabled = false, Used = true });
            mag.PlayInspect("seal", () => done++);
            Assert.AreEqual(MagnifierTool.MagnifierState.ToInspect, mag.State);
            b.To(MagnifierTool.MoveS - Edge);
            Assert.AreEqual(0f, carriage.Value, "araba poz oturmadan kıpırdamaz");
            b.To(MagnifierTool.MoveS + Edge);
            MagnifierTool.InspectFor("seal", out var sealPos, out _, out float sealC);
            Near(b.top.transform.TransformPoint(sealPos), mag.transform.position, 1e-5f, "inceleme pozunda");
            b.To(MagnifierTool.MoveS + MagnifierTool.CarriageS + Edge);
            Assert.AreEqual(sealC, carriage.Value, "araba MÜHÜR alanında (tam)");
            Assert.AreEqual(1, done);

            // Yeni aday: dinlenmeye döner (0,25 s)
            b.Zero();
            mag.Bind(new ToolData { Enabled = true, Used = false });
            b.To(MagnifierTool.MoveS + Edge);
            Assert.AreEqual(MagnifierTool.MagnifierState.Rest, mag.State);
            Near(rest, mag.transform.position, 1e-5f, "dinlenme pozu");
            Assert.AreEqual(0f, carriage.Value);

            // Kayıttan devam: kullanılmış olsa da bağlama büyüteci belgeye götürmez (tasarım: yalnız karşılaştırma anında belgede)
            var b2 = Build(true);
            b2.magnifier.Bind(new ToolData { Enabled = false, Used = true });
            Assert.AreEqual(MagnifierTool.MagnifierState.Rest, b2.magnifier.State);
            Assert.AreEqual(0f, b2.magnifier.Carriage.Value, 1e-5f);
        }

        // ================================================================== karar dizileri

        Bench DecisionBench()
        {
            var b = Build();
            b.station.Bind(Snap(true, "resident-9", true, -1, 1, Slip("cand:resident-9:papers"), Slip("cand:resident-9:breath", AdmissionTool.Breath)), false);
            b.sounds.Clear();
            b.Zero();
            return b;
        }

        [Test]
        public void Decision_AdmitAndRestricted_PapersToDrawer_ThenFold()
        {
            foreach (var choice in new[] { StampChoice.Admit, StampChoice.Restricted })
            {
                var b = DecisionBench();
                var st = b.station;
                int stowed = 0, done = 0;
                st.PlayDecision(choice, () => stowed++, () => done++);
                Assert.IsTrue(st.SequenceActive);
                Assert.IsTrue(st.Moving);
                // komut basışta yazıldı: aday gitti, ama belge ve fişler diziye ait
                st.Bind(Snap(false), true);
                b.Advance(Dt);
                Assert.IsTrue(b.doc.gameObject.activeSelf, "dizide belge görünür");
                CollectionAssert.Contains(b.sounds, AdmissionSound.Drawer);
                b.To(AdmissionStation.DocDropS + Edge);
                Near(b.drawer.transform.TransformPoint(AdmissionStation.DocInDrawerLocal), b.doc.position, 1e-5f, "belge tablada");
                b.To(AdmissionStation.DrawerS - Edge);
                Assert.AreEqual(0, stowed);
                Assert.AreEqual(CounterState.Down, st.Counter);
                b.To(AdmissionStation.DrawerS + Edge);
                Assert.AreEqual(1, stowed, "0,35'te kâğıtlar çekmecede, kamera K8'e");
                Assert.AreEqual(CounterState.Folding, st.Counter, "kanat açılmadan tezgâh katlanır (T4 0,35'te)");
                Assert.IsFalse(st.LampOn);
                CollectionAssert.Contains(b.sounds, AdmissionSound.CounterFold);
                b.To(2f * AdmissionStation.DrawerS + Edge);
                Assert.AreEqual(0f, b.drawer.Value);
                Assert.IsFalse(b.doc.gameObject.activeSelf, "belge dosyaya geçti");
                Assert.IsFalse(b.slips[0].gameObject.activeSelf);
                float end = AdmissionStation.DrawerS + AdmissionStation.FoldTotalS;
                b.To(end - Edge);
                Assert.AreEqual(0, done);
                b.To(end + Edge);
                Assert.AreEqual(1, done, choice + ": 0,93 s");
                Assert.AreEqual(CounterState.Folded, st.Counter);
                Assert.AreEqual(88f, b.top.Value);
                Assert.IsFalse(st.SequenceActive);
                Assert.IsFalse(st.TinsVisible);
                Assert.IsEmpty(st.SlipKeys);
            }
        }

        [Test]
        public void Decision_Reject_DocumentGoesBackOut()
        {
            var b = DecisionBench();
            var st = b.station;
            int stowed = 0, done = 0;
            st.PlayDecision(StampChoice.Reject, () => stowed++, () => done++);
            // komut basışta yazıldı: aday gitti (canlı sıra Changed → Bind); dizi bitince bu son görüntü uygulanır
            st.Bind(Snap(false), true);
            b.To(AdmissionStation.DocDropS - Edge);
            Assert.AreEqual(0f, b.drawer.Value, "çekmece 0,2'de açılır");
            float open = AdmissionStation.DocDropS, close = open + AdmissionStation.DrawerS + AdmissionStation.DocDropS;
            b.To(open + AdmissionStation.DrawerS + Edge);
            Assert.AreEqual(-0.22f, b.drawer.Value);
            Assert.IsTrue(b.doc.gameObject.activeSelf);
            b.To(close + Edge);
            Assert.AreEqual(1, stowed, "0,75 dışarı döner");
            Assert.AreEqual(CounterState.Down, st.Counter, "katlanış çekmece kapanınca");
            b.To(close + AdmissionStation.DrawerS + Edge);
            Assert.IsFalse(b.doc.gameObject.activeSelf);
            Assert.AreEqual(CounterState.Folding, st.Counter);
            b.To(close + AdmissionStation.DrawerS + AdmissionStation.FoldTotalS + Edge);
            Assert.AreEqual(1, done);
            Assert.AreEqual(CounterState.Folded, st.Counter);
            Assert.IsFalse(st.TinsVisible, "retde teneke yok");
        }

        [Test]
        public void Decision_Aid_TinsOutWithTheDrawer_ThenBack()
        {
            var b = DecisionBench();
            var st = b.station;
            int done = 0;
            st.PlayDecision(StampChoice.Aid, () => done++);
            st.Bind(Snap(false), true);   // komut basışta yazıldı: aday gitti; dizi bitince bu son görüntü uygulanır
            b.Advance(Dt);
            Assert.IsTrue(st.TinsVisible);
            Assert.IsTrue(b.tins.All(t => t.activeSelf), "iki teneke çekmecede");
            Assert.AreEqual(AdmissionSound.TinsOut, b.sounds.First());
            float close = AdmissionStation.DrawerS + AdmissionStation.HoldS;
            b.To(close - Edge);
            Assert.AreEqual(-0.22f, b.drawer.Value, "bekleme");
            b.To(close + AdmissionStation.AidDrawerS + Edge);
            Assert.AreEqual(0f, b.drawer.Value, "aidDrawerS 0,35");
            Assert.IsFalse(b.tins.Any(t => t.activeSelf), "tenekeler ve belge dışarı");
            Assert.IsFalse(b.doc.gameObject.activeSelf);
            b.To(close + AdmissionStation.AidDrawerS + AdmissionStation.FoldTotalS + Edge);
            Assert.AreEqual(1, done);
            Assert.AreEqual(CounterState.Folded, st.Counter);
        }

        [Test]
        public void Decision_Unconfigured_FinishesImmediately()
        {
            var go = Make("Kabuk istasyon");
            var st = go.AddComponent<AdmissionStation>();
            int stowed = 0, done = 0;
            st.PlayDecision(StampChoice.Admit, () => stowed++, () => done++);
            Assert.AreEqual(1, stowed);
            Assert.AreEqual(1, done);
            Assert.AreEqual(CounterState.Folded, st.Counter);
        }

        [Test]
        public void Decision_SnapBind_JumpsToTheEnd_AndCallsDone()
        {
            var b = DecisionBench();
            int done = 0;
            b.station.PlayDecision(StampChoice.Admit, () => done++);
            b.To(0.1f);
            b.station.Bind(Snap(false), false);
            Assert.AreEqual(1, done, "dil/yükleme: dizi sona atlar");
            Assert.AreEqual(CounterState.Folded, b.station.Counter);
            Assert.IsFalse(b.station.Moving);
        }

        // ================================================================== talimat değişimi

        [Test]
        public void OrdersChange_StripToArchive_NewStripSeats_ReviewSlipAfter()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(false), false);
            var slot = b.panel.transform.TransformPoint(AdmissionStation.StripSlotLocal);
            Near(slot, b.strip.position, 1e-5f, "şerit yuvada");

            // Talimat 2 görülmedi: tezgâh iner (başvuru olmasa da), İNCELEME FİŞİ değişimden önce çıkmaz
            var review = Slip("review:4", null, true);
            var s2 = Snap(true, null, false, -1, 2, review);
            s2.OrdersUnseen = true;
            s2.Archive.Add(Orders(1));
            b.Zero();
            st.Bind(s2, true);
            Assert.AreEqual(CounterState.Lowering, st.Counter);
            st.PlayOrdersChange(Orders(1), Orders(2), review);
            Assert.IsFalse(st.OrdersChanging, "tezgâh inmeden değişim başlamaz");
            while (!st.OrdersChanging) { b.Advance(Dt); Assert.Less(b.time, AdmissionStation.UnfoldTotalS + Edge, "tezgâh inince değişim başlar"); }
            Assert.Greater(b.time, AdmissionStation.UnfoldTotalS - Edge);
            Assert.IsEmpty(b.sounds.Where(x => x == AdmissionSound.SlipOut), "fiş değişimden sonra");
            float t0 = b.time;
            b.To(t0 + AdmissionStation.StripOutS - 1.5f * Dt);
            Near(b.panel.transform.TransformPoint(AdmissionStation.StripTransitLocal), b.strip.position, 2e-3f, "eski şerit ESKİ cebinin ara pozunda");
            b.To(t0 + AdmissionStation.StripOutS + AdmissionStation.StripInS + Edge);
            Assert.IsFalse(st.OrdersChanging);
            Near(slot, b.strip.position, 1e-5f, "yeni şerit yuvada");
            Assert.IsTrue(b.archive[0].gameObject.activeSelf, "ESKİ cebinde sürüm 1");
            Assert.IsFalse(b.archive[1].gameObject.activeSelf);
            CollectionAssert.Contains(b.sounds, AdmissionSound.SlipOut);
            CollectionAssert.AreEqual(new[] { "review:4" }, st.SlipKeys);
        }

        // ================================================================== okuma pozu, üzerine gelme, tık

        [Test]
        public void ReadPose_CounterPapersFromAbove_StripFromItsFront()
        {
            var b = Build();
            b.station.Bind(Snap(true, "resident-9", true), false);
            const float d = 0.318f;
            var pose = b.station.ReadPose(AdmissionPaper.Document, 0, d);
            Assert.AreEqual(d, (pose.position - b.doc.position).magnitude, 1e-5f);
            var dir = b.counter.transform.TransformDirection(AdmissionStation.CounterReadDirection.normalized);
            Near(b.doc.position + dir * d, pose.position, 1e-5f, "tezgâh yönü (0; 0,4; −0,1)");
            Assert.Less(Vector3.Angle(b.doc.position - pose.position, pose.rotation * Vector3.forward), 0.01f, "kâğıda bakar");
            Assert.AreEqual(75.96f, Vector3.Angle(pose.rotation * Vector3.forward, Vector3.ProjectOnPlane(pose.rotation * Vector3.forward, Vector3.up)), 0.05f, "eğim 75,96°");
            Assert.Greater(Vector3.Dot(pose.rotation * Vector3.up, b.counter.transform.forward), 0f, "ekranın üstü duvara");

            var sp = b.station.ReadPose(AdmissionPaper.Strip, 0, d);
            Near(b.strip.position - b.strip.forward * d, sp.position, 1e-5f, "şeridin önünde");
            Assert.Less(Vector3.Angle(b.strip.forward, sp.rotation * Vector3.forward), 0.01f);
            Assert.AreEqual(b.root.transform.position, b.station.ReadPose(AdmissionPaper.Slip, 0, d).position, "olmayan kâğıt: istasyonun pozu");
            b.station.SetInteractive(true);
            b.station.OnPointerEnter(Pointer(Vector2.zero, b.counterHit.gameObject));
            b.To(AdmissionStation.HoverS + Edge);
            var whileLifted = b.station.ReadPose(AdmissionPaper.Document, 0, d);
            b.station.OnPointerExit(null);
            b.To(AdmissionStation.HoverS * 2 + Edge * 2);
            Near(b.doc.position + dir * d, whileLifted.position, 1e-5f, "hover kalkınca okuma çapası kaymaz");
            var resting = b.doc.position;
            b.station.SetReading(true);
            b.station.OnPointerEnter(Pointer(Vector2.zero, b.counterHit.gameObject));
            b.To(b.time + AdmissionStation.HoverS + Edge);
            Near(resting, b.doc.position, 1e-5f, "yakın okumada tezgâh tekrar kalkmaz");
        }

        [Test]
        public void Hover_CounterLifts4mm_TitlesFromTheSnapshot()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(true, "resident-9", false), false);
            st.SetInteractive(true);
            st.OnPointerEnter(Pointer(Vector2.zero, b.counterHit.gameObject));
            Assert.AreEqual("KABUL TEZGÂHI", st.HoverTitle);
            Assert.AreEqual("başvuru · SUNA 9", st.Line);
            b.Zero();
            b.To(AdmissionStation.HoverS + Edge);
            var lift = b.top.transform.localPosition - b.top.PivotLocal;
            Assert.AreEqual(0.004f, lift.y, 1e-6f, "4 mm");
            st.OnPointerMove(Pointer(Vector2.zero, b.doc.gameObject));
            Assert.AreEqual("EVRAK", st.HoverTitle, "çekmecedeki belge");
            Assert.AreEqual("SUNA 9", st.Line);
            st.OnPointerExit(Pointer(Vector2.zero));
            b.To(b.time + AdmissionStation.HoverS + Edge);
            Assert.AreEqual(b.top.PivotLocal, b.top.transform.localPosition);
            Assert.IsNull(st.HoverTitle);
        }

        [Test]
        public void Colliders_OnlyWhenInteractiveAndVisible_ClicksRouteToEvents()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(false), false);
            Assert.IsFalse(b.counterHit.enabled, "etkileşim yokken kapalı");
            st.SetInteractive(true);
            Assert.IsTrue(b.counterHit.enabled, "katlıyken de üzerine gelme (başvuru yok)");
            Assert.IsFalse(b.strip.GetComponent<Collider>().enabled, "katlı kanat şeridi örter");
            Assert.IsFalse(b.doc.GetComponent<Collider>().enabled);
            Assert.IsFalse(b.slips.Any(s => s.GetComponent<Collider>().enabled));
            st.ClickCollider(b.counterHit);
            Assert.AreEqual(0, b.counterClicks, "katlı tezgâh tıklanmaz");

            st.Bind(Snap(true, "resident-9", false, -1, 1, Slip("cand:resident-9:papers")), false);
            Assert.IsTrue(b.strip.GetComponent<Collider>().enabled);
            Assert.IsTrue(b.doc.GetComponent<Collider>().enabled);
            Assert.IsTrue(b.deskKey.enabled);
            Assert.IsTrue(b.slips[0].GetComponent<Collider>().enabled);
            Assert.IsFalse(b.slips[1].GetComponent<Collider>().enabled, "boş havuz yuvası kapalı");
            st.ClickCollider(b.counterHit);
            Assert.AreEqual(1, b.counterClicks);
            st.ClickCollider(b.deskKey);
            Assert.AreEqual(1, b.deskClicks);
            st.ClickCollider(b.doc.GetComponent<Collider>());
            Assert.AreEqual(1, b.taken, "çekmecedeki belgeye tık: tezgâha al");
            Assert.IsEmpty(b.papers);
            b.To(1f);
            st.ClickCollider(b.doc.GetComponent<Collider>());
            st.ClickCollider(b.strip.GetComponent<Collider>());
            st.ClickCollider(b.slips[0].GetComponent<Collider>());
            CollectionAssert.AreEqual(new[] { (AdmissionPaper.Document, 0), (AdmissionPaper.Strip, 0), (AdmissionPaper.Slip, 0) }, b.papers);

            DeskTarget.Blocked = () => true;
            st.OnPointerClick(Pointer(Vector2.zero, b.counterHit.gameObject));
            Assert.AreEqual(1, b.counterClicks, "oda kilitliyken tık yok");
            DeskTarget.Blocked = null;
            st.SetInteractive(false);
            Assert.IsFalse(b.counterHit.enabled || b.deskKey.enabled || b.strip.GetComponent<Collider>().enabled || b.slips[0].GetComponent<Collider>().enabled);
        }
    }
}
