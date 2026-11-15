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
    /// Revizyon 3 · Aşama 6 (R17, PLAN §1.6–§1.12, paket W2) ve KOS aşama 2: kabul istasyonunun hareketi (tezgâh, lamba, çekmece, iki karar
    /// damgası, aday kartları ve grup fişi). Sahne açılmaz: paket model prefab'ları (<c>Prefabs/Rev3/admission</c>) DeskGame'deki yerlerinde
    /// (ADM tabanı (2,20; 0,98; −0,27) yaw 90) örneklenir; aday kartı ve grup fişi prefab'ları istasyonun altında. Kamera K8A token'ından
    /// (1920 × 1080). Zaman <c>Advance</c> ile ilerler (Edit modda <c>Update</c> çalışmaz); süreler <c>SiginakTheme</c>'den, uç değerler tam
    /// (E M2–M8). Eski tek adaylı kabulün araçları (büyüteç, talep defteri, buğu camı, interkom), belgesi, talimat şeridi, bulgu fişleri ve
    /// tenekeleri aşama 7'de söküldü; testleri de.
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
            public CandidateGroupView view;
            public PartDriver top, bracketL, bracketR, drawer;
            public BoxCollider counterHit, deskKey;
            public Renderer lampBody;
            public Material lampOn, lampOff;
            public GameObject spot;
            public StampDecision[] stamps = new StampDecision[0];
            public Camera cam;
            public float time;
            public readonly List<AdmissionSound> sounds = new List<AdmissionSound>();
            public readonly List<(AdmissionPaper kind, int index)> papers = new List<(AdmissionPaper, int)>();
            public int counterClicks, deskClicks;

            public StampDecision Admit => stamps.First(s => s.Choice == StampChoice.Admit);
            public StampDecision Reject => stamps.First(s => s.Choice == StampChoice.Reject);

            public void Advance(float dt)
            {
                station.Advance(dt);
                foreach (var s in stamps) s.Advance(dt);
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
        /// Top'ta bir gevşek nesne grubu (MASA tuşu). <paramref name="tools"/>: iki karar damgası (rafın orta yuvaları), üç aday kartı ve
        /// grup fişi (prefab örnekleri) ve K8A kamerası.
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

            b.station = b.root.AddComponent<AdmissionStation>();
            b.station.ConfigureCounter(b.counter.transform, b.top, b.bracketL, b.bracketR, b.counterHit, b.deskKey);
            b.station.ConfigureLamp(b.lampBody, 4, b.lampOn, b.lampOff, b.spot);
            b.station.ConfigurePanel(b.panel.transform, b.drawer);

            if (tools)
            {
                b.cam = K8ACamera();
                var choices = new[] { StampChoice.Admit, StampChoice.Reject };
                b.stamps = new StampDecision[choices.Length];
                for (int i = 0; i < choices.Length; i++)
                {
                    // Rafın orta yuvaları (Slot2, Slot3; aşama 7)
                    var go = Model("admission", "DecisionStamp", b.loose.transform, new Vector3(0.30f - 0.0295f + i * 0.059f, 0.0555f, -0.22f), -8f);
                    var box = go.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, 0.042f, 0f);
                    box.size = new Vector3(0.054f, 0.085f, 0.032f);
                    var tongue = Make("Kilit dili", go.transform);
                    var s = go.AddComponent<StampDecision>();
                    s.Configure(choices[i], box, Part(go, "Band").GetComponent<Renderer>(), tongue, b.top.transform, b.cam);
                    b.stamps[i] = s;
                }
                var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdmissionFixtures.CardPrefab);
                var slipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AdmissionFixtures.GroupSlipPrefab);
                if (cardPrefab == null || slipPrefab == null) Assert.Ignore("Aday kartı / grup fişi prefab'ı yok (KOS aşama 2 kurulumu çalışmamış).");
                var holder = Make("Aday grubu", b.root.transform);
                var cards = new Transform[CandidateGroupView.MaxCards];
                for (int i = 0; i < cards.Length; i++)
                {
                    var card = Object.Instantiate(cardPrefab, holder.transform);
                    card.name = "Aday kartı " + (i + 1);
                    card.SetActive(false);
                    cards[i] = card.transform;
                }
                var slip = Object.Instantiate(slipPrefab, holder.transform);
                slip.name = "Grup fişi";
                slip.SetActive(false);
                b.view = holder.AddComponent<CandidateGroupView>();
                b.view.Configure(b.top.transform, b.drawer.transform, cards, slip.transform);
                b.station.ConfigureCandidates(b.view);
            }
            b.station.ConfigureTools(new[] { b.loose }, b.stamps);
            b.station.Sound += s => b.sounds.Add(s);
            b.station.PaperClicked += (k, i) => b.papers.Add((k, i));
            b.station.CounterClicked += () => b.counterClicks++;
            b.station.DeskKeyClicked += () => b.deskClicks++;
            foreach (var s in b.stamps) s.Sound += x => b.sounds.Add(x);
            Physics.SyncTransforms();
            return b;
        }

        static CandidateGroupData GroupData(int cards, bool full = false, string key = "group:4:resident-9")
        {
            var g = new CandidateGroupData
            {
                Key = key, Full = full, WaitLeft = 2, SlipHeader = "KYGA · KAPI FİŞİ", SlipShift = "V.005", SlipTitle = cards + " ADAY",
                WaitLine = "bekleme: 2 vardiya", HintLine = "En çok biri alınır. KABUL: bir karta. GERİ ÇEVİR: bu fişe.",
                RoomTitle = full ? "YER YOK · 8/8" : "YER VAR · 8/10", CanPass = true, SlipHoverTitle = "KAPI FİŞİ",
                SlipHoverLine = "GERİ ÇEVİR damgasını bu fişe bas.", RowHoverLine = "Tıkla: yollanacak kişi olarak işaretle.",
            };
            for (int i = 0; i < cards; i++)
            {
                var card = new CandidateCardData
                {
                    Id = "resident-" + (9 + i), Name = "ADAY " + (i + 1), Arrived = "V.005", Mastery = "Teknisyen · eğitimli", Stats = "CAN 3/3 · HAR 3 · GÜÇ 1",
                    Tier = i % 3, TierName = "KAYITSIZ", TierColor = "#854005", Suspicion = "kaydı yok", CanChoose = true, HoverLine = "KABUL damgasını bu karta bas.",
                };
                card.Perks.Add(new PerkRowData { Id = "saglam", Tag = "GETİRİ", Name = "Dayanıklı", Effect = "En yüksek can +1." });
                g.Cards.Add(card);
            }
            if (full)
                for (int i = 0; i < 8; i++)
                    g.Replace.Add(new ReplaceRowData { Id = "resident-" + (i + 1), Name = "Kişi " + (i + 1), Job = "Boşta", Label = "Kişi " + (i + 1) + " · Boşta", Enabled = true });
            return g;
        }

        /// <summary>Anlık görüntü: grup varsa tezgâh inik (Core kuralı: tezgâh yalnız grup beklerken iner).</summary>
        static AdmissionSnapshot Snap(CandidateGroupData g)
        {
            return new AdmissionSnapshot
            {
                Active = true,
                Counter = g != null ? CounterState.Down : CounterState.Folded,
                Group = g,
                Words = new AdmissionWords { Desk = "MASA", Drawer = "EVRAK", Under = "KABUL" },
                CounterHoverTitle = "KABUL TEZGÂHI",
                CounterHoverLine = g != null ? g.Cards.Count + " aday bekliyor" : "aday yok",
            };
        }

        /// <summary>Araçlı tezgâh: <paramref name="group"/> animasyonsuz bağlanır, istasyon etkileşimli, iki damga açık.</summary>
        Bench GroupBench(CandidateGroupData group)
        {
            var b = Build(true);
            b.station.Bind(Snap(group), false);
            b.station.SetInteractive(true);
            foreach (var s in b.stamps)
                s.Bind(new StampData { Enabled = true, Plate = s.Choice == StampChoice.Admit ? "KABUL ·\nTAM ERİŞİM" : "GERİ\nÇEVİR", BandColor = "#24592E" });
            b.sounds.Clear();
            b.Zero();
            Physics.SyncTransforms();
            return b;
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

        /// <summary>Damgayı rafından alır, <paramref name="world"/> noktasına sürükleyip bırakır.</summary>
        static void DragStamp(Bench b, StampDecision stamp, Vector3 world)
        {
            stamp.OnBeginDrag(Pointer(Screen(b.cam, stamp.transform.position), stamp.gameObject));
            Assert.IsTrue(stamp.Held, stamp.Choice + " kalktı");
            b.To(b.time + StampDecision.LiftS + Edge);
            var screen = Aim(b, stamp, world);
            stamp.OnDrag(Pointer(screen, stamp.gameObject));
            stamp.OnEndDrag(Pointer(screen, stamp.gameObject));
        }

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
            Assert.AreEqual(Token("motion.hoverLiftMm"), AdmissionStation.HoverLiftMm, 1e-6f);
            Assert.AreEqual(0.63f, AdmissionStation.UnfoldTotalS, 1e-5f, "T1 0,15 + 0,4 + 0,08");
            Assert.AreEqual(0.58f, AdmissionStation.FoldTotalS, 1e-5f, "T4 0,08 + 0,35 + 0,15");
            Assert.AreEqual(Token("admission.motion.stampLiftS"), StampDecision.LiftS, 1e-6f);
            Assert.AreEqual(Token("admission.motion.stampPressS"), StampDecision.PressS, 1e-6f);
            Assert.AreEqual(Token("motion.team.liftMm"), StampDecision.LiftMm, 1e-6f);
            Assert.AreEqual(Token("motion.board.returnArcS"), StampDecision.ReturnS, 1e-6f);
            Assert.AreEqual(Token("motion.board.denyShakeMm"), StampDecision.DenyMm, 1e-6f);
        }

        // ================================================================== M2 · tezgâh

        [Test]
        public void M2_Counter_UnfoldFold_DurationsAndExactEnds()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(null), false);
            Assert.AreEqual(CounterState.Folded, st.Counter);
            Assert.AreEqual(88f, b.top.Value, "katlı: Top 88");
            Assert.AreEqual(-90f, b.bracketL.Value, "BracketL −90");
            Assert.AreEqual(90f, b.bracketR.Value, "BracketR +90");
            Assert.AreEqual(0f, b.drawer.Value);
            Assert.IsFalse(st.LampOn);
            Assert.IsFalse(b.spot.activeSelf, "CounterSpot GameObject kapalı");
            Assert.AreSame(b.lampOff, b.lampBody.sharedMaterials[4], "cam sönük");
            Assert.IsFalse(b.loose.activeSelf, "katlı kanatta gevşek nesne yok");
            Assert.IsEmpty(b.sounds, "ilk bağlama sessiz");

            // T1: konsollar 0,15 → kanat 0,4 → lamba 0,08
            b.Zero();
            st.Bind(Snap(GroupData(2)), true);
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
            b.To(AdmissionStation.UnfoldTotalS + Edge);
            Assert.AreEqual(CounterState.Down, st.Counter);
            b.To(AdmissionStation.UnfoldTotalS + AdmissionStation.DrawerS + Edge);
            Assert.AreEqual(0f, b.drawer.Value, "grup gelince çekmece açılmaz (yalnız karar dizisinde)");
            Assert.IsFalse(b.sounds.Contains(AdmissionSound.Drawer));

            // T4: lamba söner (0,08) → Top 88 (0,35) → konsollar (0,15)
            b.Zero();
            b.sounds.Clear();
            st.Bind(Snap(null), true);
            Assert.AreEqual(CounterState.Folding, st.Counter);
            Assert.IsFalse(st.LampOn, "lamba katlanışın başında söner");
            Assert.IsFalse(b.spot.activeSelf);
            Assert.IsFalse(b.loose.activeSelf, "gevşek nesneler T4 başında gizli");
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
            st.Bind(Snap(null), false);
            st.Bind(Snap(GroupData(2)), true);
            bool seenHiddenLow = false;
            while (st.Counter != CounterState.Down)
            {
                b.Advance(Dt);
                if (b.top.Value >= AdmissionStation.ToolsVisibleBelowDeg) Assert.IsFalse(b.loose.activeSelf, "Top ≥ 30°: gevşek nesneler gizli (" + b.top.Value + ")");
                else if (!b.loose.activeSelf) seenHiddenLow = true;
                Assert.Less(b.time, 2f);
            }
            Assert.IsFalse(seenHiddenLow, "Top 30°'nin altına inince görünür");
            Assert.IsTrue(b.loose.activeSelf);
            st.Bind(Snap(null), true);
            Assert.IsFalse(b.loose.activeSelf, "T4 başında gizlenir");
            Assert.IsFalse(b.deskKey.enabled, "gizli MASA tuşunun çarpıştırıcısı kapalı");
        }

        [Test]
        public void T5_FirstBindAndSnap_NoAnimationNoSound()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(GroupData(2)), true);   // ilk bağlama: animasyonsuz
            Assert.AreEqual(CounterState.Down, st.Counter);
            Assert.AreEqual(0f, b.top.Value);
            Assert.AreEqual(0f, b.drawer.Value, "çekmece kapalı");
            Assert.IsTrue(st.LampOn);
            Assert.IsTrue(b.loose.activeSelf);
            Assert.IsEmpty(b.sounds);
            Assert.IsFalse(st.Moving);

            // Katlanırken animasyonsuz bağlama sona atlar
            st.Bind(Snap(null), true);
            b.To(0.2f);
            st.Bind(Snap(null), false);
            Assert.AreEqual(CounterState.Folded, st.Counter);
            Assert.AreEqual(88f, b.top.Value);
            Assert.AreEqual(-90f, b.bracketL.Value);
            Assert.AreEqual(0f, b.drawer.Value);
            Assert.IsFalse(st.Moving);
        }

        // ================================================================== M8 · damga

        [Test]
        public void M8_Stamp_Lift25mmIn01_Press012_ThenUsedPose()
        {
            var b = GroupBench(GroupData(3));
            var stamp = b.Admit;
            var presses = new List<StampChoice>();
            stamp.Pressed += c => presses.Add(c);
            var rest = stamp.transform.position;
            var restScreen = Screen(b.cam, rest);
            Assert.IsFalse(stamp.LockTongue.activeSelf, "açık damgada pirinç dil yok");

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

            // İkinci karta sürükle-bırak → 0,12 s basış
            var targetScreen = Aim(b, stamp, b.view.CardPapers[1].position);
            stamp.OnDrag(Pointer(targetScreen, stamp.gameObject));
            Assert.IsTrue(stamp.DropPoint(targetScreen, out var dropPoint));
            Assert.IsTrue(b.view.AcceptStamp(StampChoice.Admit, dropPoint), "bırakış kartın üstünde");
            b.Zero();
            stamp.OnEndDrag(Pointer(targetScreen, stamp.gameObject));
            Assert.AreEqual(StampDecision.StampState.Pressing, stamp.State);
            var target = b.view.StampPose(StampChoice.Admit);
            b.To(StampDecision.PressS - Edge);
            Assert.IsEmpty(presses, "basış bitmeden komut yok");
            b.To(StampDecision.PressS + 0.5f * Dt);
            CollectionAssert.AreEqual(new[] { StampChoice.Admit }, presses, "basış anında bir kez");
            Assert.AreEqual(AdmissionSound.StampPress, b.sounds.Last(), "'tak'");
            Near(target.position, stamp.transform.position, 2e-4f, "kartın iz merkezinde");
            b.To(StampDecision.PressS + StampDecision.ReturnS + Edge);
            Assert.AreEqual(StampDecision.StampState.Used, stamp.State);
            Near(b.top.transform.TransformPoint(StampDecision.UsedLocal), stamp.transform.position, 1e-5f, "kullanılmış poz");
            Assert.IsFalse(stamp.Hit.enabled, "kullanılmış damga tıklanmaz");

            // Tezgâh katlanırken rafına döner
            b.station.Bind(Snap(null), true);
            Assert.AreEqual(StampDecision.StampState.Rack, stamp.State);
            Near(rest, stamp.transform.position, 1e-5f, "rafında");
        }

        [Test]
        public void M8_Stamp_DropElsewhere_ReturnsInArc_AndLockedShakes()
        {
            var b = GroupBench(GroupData(2));
            var stamp = b.Admit;
            int presses = 0;
            stamp.Pressed += c => presses++;
            var rest = stamp.transform.position;
            var restScreen = Screen(b.cam, rest);
            stamp.OnBeginDrag(Pointer(restScreen, stamp.gameObject));
            b.To(b.time + StampDecision.LiftS + Edge);
            var elsewhere = Aim(b, stamp, b.top.transform.TransformPoint(new Vector3(-0.3f, 0.0043f, -0.25f)));
            Assert.IsTrue(stamp.DropPoint(elsewhere, out var p));
            Assert.IsFalse(b.view.AcceptStamp(StampChoice.Admit, p));
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
            stamp.Bind(new StampData { Enabled = false, Plate = "KABUL ·\nTAM ERİŞİM", Reason = "Sığınak dolu: önce yollanacak kişiyi işaretle." });
            Assert.IsTrue(stamp.LockTongue.activeSelf, "pirinç dil");
            Assert.AreEqual("Sığınak dolu: önce yollanacak kişiyi işaretle.", stamp.Line, "üzerine gelme satırı Core iletisi");
            Assert.AreEqual("KABUL · TAM ERİŞİM", stamp.HoverTitle);
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
            var b = GroupBench(GroupData(2));
            var stamp = b.Admit;
            stamp.Pressed += c => stamp.ReturnToRack(true);   // sunucu: komut reddi → rafına
            var rest = stamp.transform.position;
            DragStamp(b, stamp, b.view.CardPapers[0].position);
            Assert.AreEqual(StampDecision.StampState.Pressing, stamp.State);
            b.Zero();
            b.To(StampDecision.PressS + 0.5f * Dt);
            Assert.AreEqual(StampDecision.StampState.Returning, stamp.State, "kullanılmış poza gitmez");
            b.To(StampDecision.PressS + StampDecision.ReturnS + Edge);
            Near(rest, stamp.transform.position, 1e-5f, "rafında");
        }

        [Test]
        public void Stamp_ClickClick_AndEscReturns()
        {
            var b = GroupBench(GroupData(2));
            var stamp = b.Reject;
            int presses = 0;
            stamp.Pressed += c => presses++;
            var rest = stamp.transform.position;
            var restScreen = Screen(b.cam, rest);

            // Tık: elde; imleci izler (25 mm yukarıda)
            stamp.OnPointerClick(Pointer(restScreen, stamp.gameObject));
            Assert.IsTrue(stamp.Held);
            Assert.AreSame(stamp, b.station.HeldStamp);
            var slip = b.view.SlipPaper.position;
            var targetScreen = Aim(b, stamp, slip);
            stamp.Follow(targetScreen);
            b.To(b.time + StampDecision.LiftS + Edge);
            Assert.IsTrue(stamp.DropPoint(targetScreen, out var over));
            Near(over + b.top.transform.up * stamp.HeldHeight, stamp.transform.position, 1e-5f, "imlecin altında, tutma yüksekliğinde");
            Assert.IsTrue(b.view.AcceptStamp(StampChoice.Reject, over));

            // İkinci tık istasyona düşer (tezgâh) → elde damga basar
            b.station.OnPointerClick(Pointer(targetScreen, b.counterHit.gameObject, over));
            Assert.AreEqual(StampDecision.StampState.Pressing, stamp.State);
            b.To(b.time + StampDecision.PressS + Edge);
            Assert.AreEqual(1, presses);

            // Tık → elde → Esc (sunucu ReturnHeldStamp) → rafına 0,25 s
            var other = b.Admit;
            var otherRest = other.transform.position;
            other.OnPointerClick(Pointer(Screen(b.cam, otherRest), other.gameObject));
            Assert.IsTrue(other.Held);
            b.Zero();
            Assert.IsTrue(b.station.ReturnHeldStamp());
            Assert.IsFalse(other.Held);
            Assert.IsFalse(b.station.ReturnHeldStamp(), "elde damga yok");
            b.To(StampDecision.ReturnS + Edge);
            Near(otherRest, other.transform.position, 1e-5f, "rafında");

            // Tık → elde → kâğıt dışına tık → rafına
            other.OnPointerClick(Pointer(Screen(b.cam, otherRest), other.gameObject));
            b.To(b.time + StampDecision.LiftS + Edge);
            other.ClickAt(b.top.transform.TransformPoint(new Vector3(-0.3f, 0.0043f, -0.25f)));
            Assert.AreEqual(StampDecision.StampState.Returning, other.State);
            Assert.AreEqual(1, presses);
        }

        // ================================================================== KOS aşama 2 · aday grubu (istasyonla bütünleşme)

        /// <summary>
        /// Grup bağlıyken tezgâh iner, kartlar ve fiş kanatla birlikte görünür; iki damga rafta. Grup kalkınca tezgâh katlanır, kâğıtlar
        /// gizlenir; yeni grubun kartları inen kanadı izler.
        /// </summary>
        [Test]
        public void Candidates_ShownWithTheCounter()
        {
            var b = GroupBench(GroupData(3));
            var view = b.view;
            Assert.AreEqual(CounterState.Down, b.station.Counter);
            Assert.AreSame(view, b.station.Candidates);
            Assert.IsTrue(view.Shown);
            Assert.AreEqual(3, view.CardCount);
            Assert.IsTrue(view.Interactive, "inik ve etkileşimli");
            Assert.IsTrue(view.CardPapers.All(p => p.gameObject.activeSelf) && view.SlipPaper.gameObject.activeSelf);
            Assert.IsTrue(b.Admit.gameObject.activeInHierarchy, "KABUL damgası rafta");
            Assert.IsTrue(b.Reject.gameObject.activeInHierarchy, "GERİ ÇEVİR damgası rafta");
            Assert.AreEqual(2, b.station.Stamps.Count, "istasyonda yalnız iki damga");
            for (int i = 0; i < 3; i++)
                Near(CandidateGroupView.CardLocal(i, 3), b.top.transform.InverseTransformPoint(view.CardPapers[i].position), 1e-5f, "kart " + i + " kanadın yerelinde");

            // Grup kalktı: tezgâh katlanır (T4), kâğıtlar gizlenir
            b.station.Bind(Snap(null), true);
            b.Zero();
            b.To(AdmissionStation.FoldTotalS + Edge);
            Assert.AreEqual(CounterState.Folded, b.station.Counter);
            Assert.IsFalse(view.Shown);
            Assert.IsTrue(view.CardPapers.All(p => !p.gameObject.activeSelf) && !view.SlipPaper.gameObject.activeSelf);

            // Yeni grup (iki kart): kartlar kanat inince görünür ve kanatla birlikte durur
            b.station.Bind(Snap(GroupData(2, key: "group:7:resident-12")), true);
            b.Zero();
            bool seen = false;
            while (b.time < AdmissionStation.UnfoldTotalS + Edge)
            {
                b.Advance(Dt);
                Assert.AreEqual(b.station.ToolsVisible, view.Shown, "kâğıtlar gevşek nesnelerle birlikte görünür");
                if (view.Shown)
                {
                    seen = true;
                    Near(CandidateGroupView.CardLocal(0, 2), b.top.transform.InverseTransformPoint(view.CardPapers[0].position), 1e-5f, "kart inen kanadı izler");
                }
            }
            Assert.IsTrue(seen);
            Assert.AreEqual(CounterState.Down, b.station.Counter);
            Assert.AreEqual(2, view.CardCount);
            Assert.IsFalse(view.CardPapers[2].gameObject.activeSelf, "üçüncü kart kapalı");
        }

        /// <summary>
        /// Damga yönlendirme: KABUL bir karta sürüklenip bırakılınca basar (hedef kart <c>StampCandidateId</c>, damga kartın iz merkezinde);
        /// fişe ya da tezgâhın boşuna bırakılırsa rafına döner. GERİ ÇEVİR yalnız fişe basar.
        /// </summary>
        [Test]
        public void Candidates_StampDrag_AdmitOnACard_RejectOnTheSlip()
        {
            var group = GroupData(3);
            var b = GroupBench(group);
            var view = b.view;
            var presses = new List<StampChoice>();
            foreach (var s in b.stamps) s.Pressed += presses.Add;
            var admit = b.Admit;
            var reject = b.Reject;
            var admitRest = admit.transform.position;
            var slipCentre = view.SlipPaper.position;

            // KABUL → fiş: basmaz, rafına
            DragStamp(b, admit, slipCentre);
            Assert.AreEqual(StampDecision.StampState.Returning, admit.State, "KABUL fişe basılmaz");
            b.To(b.time + StampDecision.ReturnS + Edge);
            Near(admitRest, admit.transform.position, 1e-5f, "rafında");
            Assert.IsEmpty(presses);
            Assert.IsNull(view.StampCandidateId);

            // KABUL → ikinci kart: basar
            DragStamp(b, admit, view.CardPapers[1].position);
            Assert.AreEqual(StampDecision.StampState.Pressing, admit.State);
            Assert.AreEqual(group.Cards[1].Id, view.StampCandidateId, "hedef: bırakılan kart");
            var target = view.StampPose(StampChoice.Admit);
            b.To(b.time + StampDecision.PressS + Edge);
            CollectionAssert.AreEqual(new[] { StampChoice.Admit }, presses, "basış anında bir kez");
            Near(target.position, admit.transform.position, 2e-4f, "damga kartın iz merkezinde");
            Assert.Less((target.position - view.CardPapers[1].position).magnitude, 0.08f, "iz merkezi kartın içinde");
            admit.ReturnToRack(false);

            // GERİ ÇEVİR → kart: basmaz; → fiş: basar
            DragStamp(b, reject, view.CardPapers[0].position);
            Assert.AreEqual(StampDecision.StampState.Returning, reject.State, "GERİ ÇEVİR karta basılmaz");
            b.To(b.time + StampDecision.ReturnS + Edge);
            Assert.AreEqual(1, presses.Count);
            DragStamp(b, reject, slipCentre);
            Assert.AreEqual(StampDecision.StampState.Pressing, reject.State);
            Assert.IsNull(view.StampCandidateId, "fiş hedefken kart hedefi yok");
            var pass = view.StampPose(StampChoice.Reject);
            b.To(b.time + StampDecision.PressS + Edge);
            CollectionAssert.AreEqual(new[] { StampChoice.Admit, StampChoice.Reject }, presses);
            Near(pass.position, reject.transform.position, 2e-4f, "damga fişin sol bloğunda");
        }

        /// <summary>Tık-tık: elde damga varken karta tık damgaya gider (kart okunmaz, basış başlar); elde damga yokken aynı tık okumadır.</summary>
        [Test]
        public void Candidates_HeldStamp_ClickGoesToTheStamp()
        {
            var group = GroupData(2);
            var b = GroupBench(group);
            var view = b.view;
            int presses = 0;
            var admit = b.Admit;
            admit.Pressed += c => presses++;
            var card = view.CardPapers[0];
            var hit = card.GetComponentInChildren<Collider>(true);
            Assert.IsTrue(hit.enabled);

            // Elde damga yok: karta tık okuma
            var plain = Pointer(Screen(b.cam, card.position), hit.gameObject, card.position);
            view.OnPointerDown(plain);
            view.OnPointerClick(plain);
            CollectionAssert.AreEqual(new[] { (AdmissionPaper.Candidate, 0) }, b.papers);

            // Damga elde: aynı karta tık basış
            admit.OnPointerClick(Pointer(Screen(b.cam, admit.transform.position), admit.gameObject));
            Assert.IsTrue(admit.Held);
            Assert.AreSame(admit, b.station.HeldStamp);
            b.To(b.time + StampDecision.LiftS + Edge);
            var screen = Aim(b, admit, card.position);
            admit.Follow(screen);
            var click = Pointer(screen, hit.gameObject, card.position);
            view.OnPointerDown(click);
            view.OnPointerClick(click);
            Assert.AreEqual(StampDecision.StampState.Pressing, admit.State, "tık elde damgaya gitti");
            Assert.AreEqual(1, b.papers.Count, "kart okunmadı");
            Assert.AreEqual(group.Cards[0].Id, view.StampCandidateId);
            b.To(b.time + StampDecision.PressS + Edge);
            Assert.AreEqual(1, presses);
        }

        /// <summary>
        /// İstasyon aday olaylarını yönlendirir: kart → <c>PaperClicked(Candidate, i)</c>, fiş → <c>PaperClicked(GroupSlip, 0)</c>, satır →
        /// <c>ReplaceClicked</c>; okuma pozu kâğıdın merkezine bakar. Etkileşim kapalıyken kâğıtlar tık vermez.
        /// </summary>
        [Test]
        public void Candidates_PaperClicks_ReadPose_AndReplace()
        {
            var group = GroupData(3, full: true);
            var b = GroupBench(group);
            var view = b.view;
            var replaced = new List<string>();
            b.station.ReplaceClicked += replaced.Add;
            for (int i = 0; i < 3; i++) view.ClickCollider(view.CardPapers[i].GetComponentInChildren<Collider>(true));
            view.ClickCollider(view.SlipPaper.GetComponentInChildren<Collider>(true));
            CollectionAssert.AreEqual(new[]
            {
                (AdmissionPaper.Candidate, 0), (AdmissionPaper.Candidate, 1), (AdmissionPaper.Candidate, 2), (AdmissionPaper.GroupSlip, 0),
            }, b.papers);
            view.ClickSlip(3);
            CollectionAssert.AreEqual(new[] { "resident-4" }, replaced, "satır → yollanacak kişi");
            Assert.AreEqual(4, b.papers.Count, "satır tıkı okuma değil");
            Assert.AreEqual(0, b.counterClicks, "kâğıt tıkı tezgâh tıkı sayılmaz");

            foreach (var (kind, index, paper) in new[]
                     {
                         (AdmissionPaper.Candidate, 0, view.CardPapers[0]), (AdmissionPaper.Candidate, 2, view.CardPapers[2]), (AdmissionPaper.GroupSlip, 0, view.SlipPaper),
                     })
            {
                var pose = b.station.ReadPose(kind, index, 0.318f);
                Assert.AreEqual(0.318f, (pose.position - paper.position).magnitude, 1e-3f, kind + " " + index + ": okuma mesafesi");
                Assert.Greater(Vector3.Dot(pose.rotation * Vector3.forward, (paper.position - pose.position).normalized), 0.999f, kind + ": kâğıda bakar");
            }
            var none = b.station.ReadPose(AdmissionPaper.Candidate, 5, 0.318f);
            Near(b.station.transform.position, none.position, 1e-5f, "olmayan kart: istasyon pozu");
            Near(b.station.transform.position, b.station.ReadPose(AdmissionPaper.Document, 0, 0.318f).position, 1e-5f, "eski kâğıt türü: istasyon pozu");

            b.station.SetInteractive(false);
            Assert.IsFalse(view.Interactive);
            Assert.IsTrue(view.CardPapers.All(p => !p.GetComponentInChildren<Collider>(true).enabled), "etkileşim kapalıyken çarpıştırıcılar kapalı");
            view.ClickCollider(view.CardPapers[0].GetComponentInChildren<Collider>(true));
            view.ClickSlip(1);
            Assert.AreEqual(4, b.papers.Count);
            Assert.AreEqual(1, replaced.Count);
        }

        /// <summary>
        /// Karar dizisi: KABUL'de çekmece açılırken kartlar ve fiş çekmeceye kalkar (<c>docDropS</c>), çekmece kapanınca gizlenir, tezgâh
        /// katlanır ve <c>done</c> çağrılır; dizi boyunca kâğıtlar tık ve damga almaz. GERİ ÇEVİR'de kâğıtlar 0,2 s sonra kalkar.
        /// </summary>
        [TestCase(StampChoice.Admit)]
        [TestCase(StampChoice.Reject)]
        public void Candidates_Decision_StowThenHide(StampChoice choice)
        {
            var b = GroupBench(GroupData(3));
            var view = b.view;
            bool done = false;
            float delay = choice == StampChoice.Reject ? AdmissionStation.DocDropS : 0f;
            var rest = view.CardPapers[0].position;
            b.Zero();
            b.station.PlayDecision(choice, () => done = true);
            // Komut basışta yazıldı: dizi bitince uygulanacak görüntü grupsuzdur (SiginakAdmission aynı sırayla bağlar)
            b.station.Bind(Snap(null), true);
            Assert.IsTrue(b.station.SequenceActive);
            b.To(delay + Edge);
            Assert.IsTrue(view.Moving, "kâğıtlar çekmeceye kalkıyor");
            Assert.IsFalse(view.Interactive, "dizide kâğıtlar tıklanmaz");
            Assert.IsFalse(view.AcceptStamp(StampChoice.Admit, rest), "dizide damga bırakılmaz");
            Assert.AreEqual(1, b.sounds.Count(s => s == AdmissionSound.DocDrop), "kâğıt sesi bir kez");
            b.To(delay + AdmissionStation.DocDropS + Edge);
            Assert.IsFalse(view.Moving);
            for (int i = 0; i < 3; i++)
                Near(b.drawer.transform.TransformPoint(AdmissionStation.DocInDrawerLocal + new Vector3(0f, CandidateGroupView.StowStepM * (i + 1), 0f)),
                    view.CardPapers[i].position, 2e-4f, "kart " + i + " çekmecede");
            Assert.IsTrue(view.CardPapers[0].gameObject.activeSelf, "çekmece kapanana dek görünür");

            float guard = 0f;
            while (!done && guard < 4f) { b.Advance(Dt); guard += Dt; }
            Assert.IsTrue(done, "dizi bitti");
            Assert.IsFalse(b.station.SequenceActive);
            Assert.AreEqual(CounterState.Folded, b.station.Counter);
            Assert.IsFalse(view.Shown);
            Assert.IsNull(view.Data);
            Assert.IsNull(view.StampCandidateId);
            Assert.IsTrue(view.CardPapers.All(p => !p.gameObject.activeSelf) && !view.SlipPaper.gameObject.activeSelf, "kâğıtlar gizli");
            Assert.AreEqual(0, b.papers.Count);
        }

        // ================================================================== karar dizileri (zaman çizelgesi)

        [Test]
        public void Decision_Admit_DrawerThenFold()
        {
            var b = GroupBench(GroupData(2));
            var st = b.station;
            int stowed = 0, done = 0;
            st.PlayDecision(StampChoice.Admit, () => stowed++, () => done++);
            Assert.IsTrue(st.SequenceActive);
            Assert.IsTrue(st.Moving);
            // komut basışta yazıldı: grup gitti, ama kâğıtlar diziye ait
            st.Bind(Snap(null), true);
            b.Advance(Dt);
            CollectionAssert.Contains(b.sounds, AdmissionSound.Drawer);
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
            Assert.IsFalse(b.view.Shown, "kâğıtlar dosyaya geçti");
            float end = AdmissionStation.DrawerS + AdmissionStation.FoldTotalS;
            b.To(end - Edge);
            Assert.AreEqual(0, done);
            b.To(end + Edge);
            Assert.AreEqual(1, done, "0,93 s");
            Assert.AreEqual(CounterState.Folded, st.Counter);
            Assert.AreEqual(88f, b.top.Value);
            Assert.IsFalse(st.SequenceActive);
        }

        [Test]
        public void Decision_Reject_PapersGoBackOut()
        {
            var b = GroupBench(GroupData(2));
            var st = b.station;
            int stowed = 0, done = 0;
            st.PlayDecision(StampChoice.Reject, () => stowed++, () => done++);
            // komut basışta yazıldı: grup gitti (canlı sıra Changed → Bind); dizi bitince bu son görüntü uygulanır
            st.Bind(Snap(null), true);
            b.To(AdmissionStation.DocDropS - Edge);
            Assert.AreEqual(0f, b.drawer.Value, "çekmece 0,2'de açılır");
            float open = AdmissionStation.DocDropS, close = open + AdmissionStation.DrawerS + AdmissionStation.DocDropS;
            b.To(open + AdmissionStation.DrawerS + Edge);
            Assert.AreEqual(-0.22f, b.drawer.Value);
            b.To(close + Edge);
            Assert.AreEqual(1, stowed, "0,75 dışarı döner");
            Assert.AreEqual(CounterState.Down, st.Counter, "katlanış çekmece kapanınca");
            b.To(close + AdmissionStation.DrawerS + Edge);
            Assert.IsFalse(b.view.Shown);
            Assert.AreEqual(CounterState.Folding, st.Counter);
            b.To(close + AdmissionStation.DrawerS + AdmissionStation.FoldTotalS + Edge);
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
            var b = GroupBench(GroupData(2));
            int done = 0;
            b.station.PlayDecision(StampChoice.Admit, () => done++);
            b.To(0.1f);
            b.station.Bind(Snap(null), false);
            Assert.AreEqual(1, done, "dil/yükleme: dizi sona atlar");
            Assert.AreEqual(CounterState.Folded, b.station.Counter);
            Assert.IsFalse(b.station.Moving);
        }

        // ================================================================== okuma pozu, üzerine gelme, tık

        [Test]
        public void ReadPose_CounterPapersFromAbove()
        {
            var b = GroupBench(GroupData(2));
            const float d = 0.318f;
            var card = b.view.CardPapers[0];
            var pose = b.station.ReadPose(AdmissionPaper.Candidate, 0, d);
            Assert.AreEqual(d, (pose.position - card.position).magnitude, 1e-5f);
            var dir = b.counter.transform.TransformDirection(AdmissionStation.CounterReadDirection.normalized);
            Near(card.position + dir * d, pose.position, 1e-5f, "tezgâh yönü (0; 0,4; −0,1)");
            Assert.Less(Vector3.Angle(card.position - pose.position, pose.rotation * Vector3.forward), 0.01f, "kâğıda bakar");
            Assert.AreEqual(75.96f, Vector3.Angle(pose.rotation * Vector3.forward, Vector3.ProjectOnPlane(pose.rotation * Vector3.forward, Vector3.up)), 0.05f, "eğim 75,96°");
            Assert.Greater(Vector3.Dot(pose.rotation * Vector3.up, b.counter.transform.forward), 0f, "ekranın üstü duvara");

            b.station.OnPointerEnter(Pointer(Vector2.zero, b.counterHit.gameObject));
            b.To(AdmissionStation.HoverS + Edge);
            var whileLifted = b.station.ReadPose(AdmissionPaper.Candidate, 0, d);
            b.station.OnPointerExit(null);
            b.To(AdmissionStation.HoverS * 2 + Edge * 2);
            Near(card.position + dir * d, whileLifted.position, 1e-5f, "hover kalkınca okuma çapası kaymaz");
            var resting = card.position;
            b.station.SetReading(true);
            b.station.OnPointerEnter(Pointer(Vector2.zero, b.counterHit.gameObject));
            b.To(b.time + AdmissionStation.HoverS + Edge);
            Near(resting, card.position, 1e-5f, "yakın okumada tezgâh tekrar kalkmaz");
        }

        [Test]
        public void Hover_CounterLifts4mm_TitlesFromTheSnapshot()
        {
            var b = Build();
            var st = b.station;
            st.Bind(Snap(GroupData(2)), false);
            st.SetInteractive(true);
            st.OnPointerEnter(Pointer(Vector2.zero, b.counterHit.gameObject));
            Assert.AreEqual("KABUL TEZGÂHI", st.HoverTitle);
            Assert.AreEqual("2 aday bekliyor", st.Line);
            b.Zero();
            b.To(AdmissionStation.HoverS + Edge);
            var lift = b.top.transform.localPosition - b.top.PivotLocal;
            Assert.AreEqual(0.004f, lift.y, 1e-6f, "4 mm");
            st.OnPointerMove(Pointer(Vector2.zero, b.deskKey.gameObject));
            Assert.AreEqual("MASA", st.HoverTitle, "MASA tuşu");
            Assert.IsNull(st.Line);
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
            st.Bind(Snap(null), false);
            Assert.IsFalse(b.counterHit.enabled, "etkileşim yokken kapalı");
            st.SetInteractive(true);
            Assert.IsTrue(b.counterHit.enabled, "katlıyken de üzerine gelme (grup yok)");
            Assert.IsFalse(b.deskKey.enabled, "katlı kanatta MASA tuşu yok");
            st.ClickCollider(b.counterHit);
            Assert.AreEqual(0, b.counterClicks, "katlı tezgâh tıklanmaz");

            st.Bind(Snap(GroupData(2)), false);
            Assert.IsTrue(b.deskKey.enabled);
            st.ClickCollider(b.counterHit);
            Assert.AreEqual(1, b.counterClicks);
            st.ClickCollider(b.deskKey);
            Assert.AreEqual(1, b.deskClicks);
            Assert.IsEmpty(b.papers);

            DeskTarget.Blocked = () => true;
            st.OnPointerClick(Pointer(Vector2.zero, b.counterHit.gameObject));
            Assert.AreEqual(1, b.counterClicks, "oda kilitliyken tık yok");
            DeskTarget.Blocked = null;
            st.SetInteractive(false);
            Assert.IsFalse(b.counterHit.enabled || b.deskKey.enabled);
        }
    }
}
