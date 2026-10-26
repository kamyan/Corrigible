using System.Linq;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Aşama 4: kurulan geometriye masa kontrollerini ekler — sayaç çarkları ve ± fişleri, anahtar rayı, vardiya şalteri,
    /// teleks kâğıdı, tıklanır hedefler (çarpıştırıcı + <see cref="DeskTarget"/>), umut işaretleri.
    /// </summary>
    public static partial class SiginakRoomBuilder
    {
        sealed class Controls
        {
            public CounterBank Counters;
            public StationKeyRail Rail;
            public ShiftSwitch Switch;
            public TelexFeed Telex;
            public Teleprinter Printer;
            public HopeMarks Hope;
            public TMP_Text[] FreedEntries;
            public DeskTarget Shelf, Crt, Tray, TelexPaper;
            public BulbFlicker Flicker;
        }

        /// <summary>Haritanın (sıra 0) ve harita fişlerinin (sıra 10) üstünde çizilen saydam dünya nesneleri: ± fişleri.</summary>
        const int SlipSortingOrder = 20;

        static Transform Child(Transform root, string path)
        {
            var t = root.Find(path);
            if (t == null) throw new System.InvalidOperationException("Oda kurulumunda yol yok: " + path);
            return t;
        }

        static Controls AddControls(Transform root, Hud hud)
        {
            var c = new Controls();
            c.Counters = CounterControls(Child(root, "Desk/Counter bank"));
            c.Rail = RailControls(Child(root, "Desk/Key rail"), hud);
            c.Switch = SwitchControls(Child(root, "Desk/Shift switch"), hud);
            var tp = Child(root, "Desk/Teleprinter");
            c.Printer = TeleprinterControls(tp);
            c.Telex = tp.gameObject.AddComponent<TelexFeed>();
            c.TelexPaper = Target(tp.gameObject, SiginakRoomWords.TelexPlate, null, SiginakRoomWords.TagTelex, false, null, hud);
            c.Tray = Target(Child(root, "Desk/Paper tray").gameObject, SiginakRoomWords.TrayPlate, null, SiginakRoomWords.TagTray, false, null, hud);
            c.Shelf = Target(Child(root, "Shelf/Diorama").gameObject, SiginakRoomWords.KeyShelter, "2", SiginakRoomWords.TagShelter, true, null, hud);
            c.Crt = Target(Child(root, "CRT desk/CRT").gameObject, SiginakRoomWords.CrtTitle, null, SiginakRoomWords.TagCrt, false, null, hud);
            c.Flicker = Child(root, "Ceiling bulb").GetComponent<BulbFlicker>();
            var board = Child(root, "Shell/Cork board/Freed regions");
            c.FreedEntries = Enumerable.Range(1, 3).Select(i => Child(board, "Entry " + i).GetComponent<TMP_Text>()).ToArray();
            c.Hope = HopeControls(root, c.Flicker);
            return c;
        }

        /// <summary>
        /// Hedef: nesnenin görünür parçalarını kapsayan kutu çarpıştırıcı (yerel uzayda) ve üzerine gelme / tıklama.
        /// <paramref name="lift"/> verilirse o parça kalkar.
        /// </summary>
        static DeskTarget Target(GameObject go, string title, string key, string line, bool frame, Transform lift, Hud hud, Renderer[] boundsFrom = null)
        {
            var renderers = boundsFrom ?? go.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<TextMeshPro>() == null).ToArray();
            var box = go.AddComponent<BoxCollider>();
            if (renderers.Length > 0)
            {
                Vector3 min = Vector3.one * float.MaxValue, max = Vector3.one * float.MinValue;
                foreach (var r in renderers)
                {
                    var b = r.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        var local = go.transform.InverseTransformPoint(corner);
                        min = Vector3.Min(min, local); max = Vector3.Max(max, local);
                    }
                }
                box.center = (min + max) / 2;
                box.size = max - min;
            }
            var target = go.AddComponent<DeskTarget>();
            target.Configure(title, key, line, frame, lift, renderers, hud.Tag, hud.Highlight);
            return target;
        }

        // ---------- Sayaç rafı: çarklar, ek yazı, onda bir noktası, plaka yazısı, ± fişi ----------

        static CounterBank CounterControls(Transform cb)
        {
            var face = Child(cb, "Face");
            var wheelMat = SiginakMaterials.UnlitTexture("Counter wheel", "tx_counter_digits", 0.85f, false, new Vector2(1, 1f / 11));
            var redMat = SiginakMaterials.UnlitTexture("Counter wheel red", "tx_counter_digits_red", 0.85f, false, new Vector2(1, 1f / 11));
            var shadeMat = SiginakMaterials.UnlitTexture("Counter shade", "tx_counter_shade", 1, true);
            var slipMat = SiginakMaterials.UnlitTexture("Delta slip paper", null, 1, true);
            string[] labels = { SiginakRoomWords.SwitchCover, SiginakRoomWords.Funds, SiginakRoomWords.CrateSupplies, SiginakRoomWords.CrateMaterials, SiginakRoomWords.FreeRegions };
            int[] digits = { 3, 3, 2, 3, 2 };
            bool[] hasTenths = { false, true, false, false, false };
            string[] suffixes = { "", "", "/40", "", "/12" };
            Vector3 Px(float px, float py, float z) => new Vector3(px / 2000f - 0.78f, 0.106f - py / 2000f, z);
            var modules = new CounterModule[5];
            for (int i = 0; i < 5; i++)
            {
                var node = Child(face, "Module " + i);
                float mx = 32 + i * (592 + 24);
                // Plaka yazısı: textures.js › counterFaceTex (CP 700, ortak boy ~71 px; 2000 px/m)
                Label(node, "Label", labels[i], DeskFont.CpBold, 66 / 2000f, T.ColorOf("brass.engraving"), Px(mx + 296, 93, -0.0009f),
                    Quaternion.identity, new Vector2(520 / 2000f, 80 / 2000f));
                // Çarklar pencerenin ortasında: textures.js düzeni (çark 96 × 224 px, aralık 8, ek yazı CP 700 82)
                int n = digits[i] + (hasTenths[i] ? 1 : 0);
                float fixedW = suffixes[i].Length > 0 ? suffixes[i].Length * 0.6f * 82 + 16 : 0;
                float total = n * 96 + (n - 1) * 8 + fixedW;
                float wx = mx + 296 - total / 2;
                var integer = new CounterWheel[digits[i]];
                CounterWheel tenths = null;
                for (int w = 0; w < n; w++)
                {
                    bool red = hasTenths[i] && w == n - 1;
                    var center = Px(wx + 48, 166 + 112, -0.0012f);
                    if (red)
                    {
                        // Onda bir noktası: pirinç küçük disk (counter.decimalDot)
                        var dot = Primitive(PrimitiveType.Cylinder, node, "Decimal dot", M("brass"), Px(wx - 4, 166 + 224 - 22, -0.0013f),
                            R(PI / 2, 0, 0), new Vector3(0.01f, 0.0005f, 0.01f), false);
                        dot.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    var quad = Primitive(PrimitiveType.Quad, node, red ? "Wheel tenths" : "Wheel " + w, red ? redMat : wheelMat, center, Quaternion.identity,
                        new Vector3(96 / 2000f, 224 / 2000f, 1), false);
                    Primitive(PrimitiveType.Quad, node, "Wheel shade", shadeMat, center + new Vector3(0, 0, -0.0004f), Quaternion.identity,
                        new Vector3(96 / 2000f, 224 / 2000f, 1), false);
                    var wheel = quad.AddComponent<CounterWheel>();
                    if (red) tenths = wheel; else integer[w] = wheel;
                    wx += 96 + 8;
                }
                TMP_Text suffix = null;
                if (suffixes[i].Length > 0)
                    suffix = Label(node, "Suffix", suffixes[i], DeskFont.CpBold, 82 / 2000f, T.ColorOf("counter.decimalDot"),
                        Px(wx + 6 + fixedW / 2, 166 + 112 + 10, -0.0012f), Quaternion.identity, new Vector2(fixedW / 2000f, 100 / 2000f),
                        TextAlignmentOptions.Left, localize: false, lit: false);
                // ± fişi: modülün alt kenarının altında, yüzün düzleminde
                var slipNode = Node(node, "Delta slip", Px(mx + 296, 424 + 4, -0.003f), Quaternion.identity);
                var paper = Primitive(PrimitiveType.Quad, slipNode, "Paper", slipMat, new Vector3(0, -0.018f, 0), Quaternion.identity, new Vector3(0.07f, 0.03f, 1), false);
                var slipText = Label(slipNode, "Text", "+0", DeskFont.CpBold, 0.017f, T.ColorOf("status.good"), new Vector3(0, -0.018f, -0.0006f),
                    Quaternion.identity, new Vector2(0.066f, 0.028f), localize: false, lit: false);
                // Fiş harita pozunda haritanın üst kenarına sarkar: saydam harita canvas'larından (sıra 0) sonra çizilsin
                paper.GetComponent<MeshRenderer>().sortingOrder = SlipSortingOrder;
                slipText.sortingOrder = SlipSortingOrder + 1;
                var slip = slipNode.gameObject.AddComponent<DeltaSlip>();
                slip.Configure(paper.GetComponent<MeshRenderer>(), slipText);
                slipNode.gameObject.SetActive(false);
                var module = node.gameObject.AddComponent<CounterModule>();
                module.Configure(integer, tenths, suffix, slip);
                modules[i] = module;
            }
            var bank = cb.gameObject.AddComponent<CounterBank>();
            bank.Configure(modules, Child(face, "Panel").GetComponent<MeshRenderer>());
            return bank;
        }

        // ---------- Anahtar rayı ----------

        static StationKeyRail RailControls(Transform rail, Hud hud)
        {
            string[] caps = { "1", "2", "3", "H", "Home" };
            string[] titles = { SiginakRoomWords.KeyCommand, SiginakRoomWords.KeyShelter, SiginakRoomWords.KeyMissions, SiginakRoomWords.KeyGuide, SiginakRoomWords.KeyDesk };
            string[] lines = { SiginakRoomWords.TagCommand, SiginakRoomWords.TagShelter, SiginakRoomWords.TagMissions, SiginakRoomWords.TagGuide, SiginakRoomWords.TagDesk };
            var keys = new StationKey[5];
            for (int i = 0; i < 5; i++)
            {
                var node = Child(rail, "Key " + caps[i]);
                var target = Target(node.gameObject, titles[i], caps[i], lines[i], false, null, hud);
                var key = node.gameObject.AddComponent<StationKey>();
                key.Configure(Child(node, "Cap"), Child(node, "Jewel").GetComponent<MeshRenderer>(), Child(node, "Plate/Brass").GetComponent<MeshRenderer>(),
                    M("jewelOn"), M("jewelOff"), target);
                keys[i] = key;
            }
            var component = rail.gameObject.AddComponent<StationKeyRail>();
            component.Configure(keys);
            return component;
        }

        // ---------- Vardiya şalteri ----------

        static ShiftSwitch SwitchControls(Transform sw, Hud hud)
        {
            var hinge = Child(sw, "Cover hinge");
            // Devre dışı mühür bandı: kapağın üstünde sarı-siyah şerit + “KİLİTLİ”
            var band = Node(hinge, "Lock band", new Vector3(0, 0.0958f, -0.07f), R(-PI / 2, 0, 0));
            Primitive(PrimitiveType.Quad, band, "Stripes", SiginakMaterials.UnlitTexture("Hazard band", "ui_hazard_stripes", 1, false, new Vector2(4, 1)),
                Vector3.zero, Quaternion.identity, new Vector3(0.085f, 0.034f, 1), false);
            Label(band, "Text", SiginakRoomWords.Locked, DeskFont.CpBold, 0.013f, T.ColorOf("bunker.hazard[1]"), new Vector3(0, 0, -0.0006f),
                Quaternion.identity, new Vector2(0.08f, 0.02f), lit: false);
            band.gameObject.SetActive(false);
            var parts = sw.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<TextMeshPro>() == null && !r.transform.IsChildOf(band)).ToArray();
            var target = Target(sw.gameObject, SiginakRoomWords.SwitchPlate, "Enter", SiginakRoomWords.TagShift, false, null, hud, parts);
            var component = sw.gameObject.AddComponent<ShiftSwitch>();
            component.Configure(hinge, Child(sw, "Lever"), Child(sw, "Ready lamp").GetComponent<MeshRenderer>(), M("readyLamp"), M("jewelOff"), band.gameObject, target);
            return component;
        }

        // ---------- Teleks kâğıdı ----------

        static Teleprinter TeleprinterControls(Transform tp)
        {
            var flat = Child(tp, "Paper flat");
            var node = Node(tp, "Paper text", flat.localPosition + new Vector3(0, 0.0006f, 0), flat.localRotation);
            var text = Label(node, "Lines", "", DeskFont.CpRegular, 0.0055f, T.ColorOf("ink"), Vector3.zero, Quaternion.identity,
                new Vector2(0.18f, 0.22f), TextAlignmentOptions.TopLeft, localize: false, wrap: true);
            text.enableAutoSizing = false;
            text.lineSpacing = -8;
            var printer = tp.gameObject.AddComponent<Teleprinter>();
            printer.Configure(text, flat.GetComponent<MeshRenderer>(), SiginakRoomWords.TelexHeader);
            return printer;
        }

        // ---------- Umut işaretleri ----------

        static HopeMarks HopeControls(Transform root, BulbFlicker flicker)
        {
            var group = Group(root, "Hope marks", 0, 0, 0);
            // 3/12: filizli teneke (rafta, kurulmuş) + KAYIP ilanına BULUNDU damgası
            var note = Child(root, "Shell/Cork board/Missing A");
            var found = Label(note, "Found stamp", SiginakRoomWords.Found, DeskFont.CpBold, 0.022f, T.ColorOf("stamp.received"),
                new Vector3(0.01f, -0.01f, -0.0012f), Quaternion.Euler(0, 0, 12), new Vector2(0.13f, 0.03f), spacingEm: 0.08f);
            // 6/12: panoya çocuk çizimi, masada ikinci kupa
            var drawing = Node(group, "Child drawing", new Vector3(-0.38f, 1.26f, 2.271f), R(0, 0, 6 * D2R));
            Plane(drawing, "Paper", 0.12f, 0.09f, M("paperWhite"), 0, 0, 0);
            Box(drawing, "Sun", 0.022f, 0.022f, 0.001f, M("pencil"), 0.035f, 0.022f, -0.001f, cast: false);
            Box(drawing, "Ground", 0.10f, 0.006f, 0.001f, M("leaf"), 0, -0.028f, -0.001f, cast: false);
            Box(drawing, "House", 0.03f, 0.026f, 0.001f, M("red"), -0.02f, -0.012f, -0.001f, cast: false);
            Sphere(drawing, "Pin", 0.007f, M("pinBlue"), 0, 0.035f, -0.004f);
            var mug = Child(root, "Desk/Mug");
            var mug2 = Object.Instantiate(mug.gameObject, group);
            mug2.name = "Second mug";
            mug2.transform.position = mug.position + new Vector3(-0.09f, 0, 0.12f);
            // 12/12: afişe el yazısı (piktogramın üstündeki boş şerit; basılı satırlarla çakışmaz), havalandırmadan gün ışığı
            var poster = Child(root, "Shell/Doctrine poster");
            var hand = Label(poster, "Going up", SiginakRoomWords.GoingUp, DeskFont.KalamBold, 0.03f, T.ColorOf("pen.signature"),
                new Vector3(0.005f, 0.57f / 2 - 125 / 1200f * 0.57f, -0.0016f), Quaternion.Euler(0, 0, 5), new Vector2(0.34f, 0.05f));
            var sunGo = new GameObject("Daylight", typeof(Light));
            sunGo.transform.SetParent(group, false);
            sunGo.transform.position = new Vector3(-0.4f, 2.18f, 2.02f);
            sunGo.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0.76f, 1.3f) - sunGo.transform.position);
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Spot;
            sun.color = T.ColorOf("lightsExtra.daylight.color");
            sun.intensity = T.Number("lightsExtra.daylight.intensity", 0.8f);
            sun.range = T.Number("lightsExtra.daylight.range", 3.2f);
            sun.spotAngle = T.Number("lightsExtra.daylight.angle", 38);
            sun.shadows = LightShadows.None;
            var hope = group.gameObject.AddComponent<HopeMarks>();
            hope.Configure(new[]
            {
                // Teneke hep rafta; filiz (yapraklar) 3/12'de çıkar
                new HopeMarks.Mark { threshold = 3, objects = Child(root, "Shelf/Hope sprout").Cast<Transform>().Where(t => t.name.StartsWith("Leaf"))
                    .Select(t => t.gameObject).Append(found.gameObject).ToArray() },
                new HopeMarks.Mark { threshold = 6, objects = new[] { drawing.gameObject, mug2 } },
                new HopeMarks.Mark { threshold = 12, objects = new[] { hand.gameObject, sunGo } },
            }, flicker);
            hope.Show(0, true);   // sahne açılış durumuyla kaydedilir; oyunda bağlayıcı günceller
            return hope;
        }
    }
}
