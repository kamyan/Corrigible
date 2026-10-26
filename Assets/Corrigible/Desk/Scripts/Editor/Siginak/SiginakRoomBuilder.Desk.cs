using TMPro;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>Komuta masası ve üstündekiler (room.js › main desk … right side desk props).</summary>
    public static partial class SiginakRoomBuilder
    {
        static void Desk(Transform desk, Refs refs)
        {
            Box(desk, "Top", 1.84f, 0.04f, 1.10f, SiginakMaterials.Textured("Lino top", "tx_lino", "lino", new Vector2(2, 1)), 0, 0.74f, 1.33f);
            Box(desk, "Front brass strip", 1.86f, 0.012f, 0.012f, M("brassDark"), 0, 0.754f, 0.775f);
            Box(desk, "Frame", 1.84f, 0.03f, 1.10f, M("steel"), 0, 0.705f, 1.33f);
            foreach (var sx in new[] { -0.71f, 0.71f })
            {
                var ped = Group(desk, sx < 0 ? "Pedestal left" : "Pedestal right", 0, 0, 0);
                Box(ped, "Body", 0.42f, 0.66f, 1.06f, M("steel"), sx, 0.36f, 1.33f);
                for (int i = 0; i < 3; i++)
                {
                    float y = 0.56f - i * 0.2f;
                    Box(ped, "Drawer " + i, 0.38f, 0.17f, 0.01f, M("steelDark"), sx, y, 0.795f);
                    Box(ped, "Handle " + i, 0.12f, 0.012f, 0.02f, M("brassDark"), sx, y + 0.04f, 0.785f);
                }
            }
            Box(desk, "Modesty panel", 1.0f, 0.6f, 0.02f, M("steelDark"), 0, 0.40f, 1.80f);

            refs.MapMount = Node(desk, "MapMount", new Vector3(0, 0.7612f, 1.3175f), Quaternion.identity);
            CounterBank(desk);
            KeyRail(desk);
            ShiftSwitch(desk);
            Teleprinter(desk);
            DeskLamp(desk, refs);
            Props(desk);
        }

        /// <summary>
        /// Plaka: pirinç düzlem + üstünde kazınmış yazı satırları (plateTex). Düzlem ölçeklendiği için yazılar aynı
        /// konum/açıdaki ayrı düğümün çocuklarıdır. Satır: (metin, yerel y oranı, em oranı) — JS'teki H × 0.36/0.74 düzeni.
        /// </summary>
        static Transform PlateWithText(Transform parent, string name, float w, float h, Vector3 pos, Quaternion rot,
            params (string text, float yFrac, float emFrac)[] lines)
        {
            var node = Node(parent, name, pos, rot);
            Plane(node, "Brass", w, h, M("plate"), 0, 0, 0);
            float pad = h * 0.16f;
            int i = 0;
            foreach (var (text, yFrac, emFrac) in lines)
                Label(node, "Text " + i++, text, DeskFont.CpBold, h * emFrac, T.ColorOf("brass.engraving"),
                    new Vector3(0, h * (0.5f - yFrac), -0.0006f), Quaternion.identity, new Vector2(w - pad * 2, h * emFrac * 1.4f));
            return node;
        }

        // room.js › counter bank (çarklar ve plaka yazıları Aşama 4'te CounterBank ile)
        static void CounterBank(Transform desk)
        {
            var cb = Group(desk, "Counter bank", 0, 0, 0);
            Box(cb, "Body", 1.60f, 0.195f, 0.08f, M("bakelite"), 0, 0.8575f, 1.84f);
            Box(cb, "Brass cap", 1.60f, 0.012f, 0.09f, M("brassDark"), 0, 0.958f, 1.835f);
            Box(cb, "Slope", 1.60f, 0.212f, 0.10f, M("bakelite"), 0, 0.830f, 1.775f, rx: -45 * D2R);
            var face = Node(cb, "Face", new Vector3(0, 0.8657f, 1.7388f), R(-45 * D2R, 0, 0));
            Plane(face, "Panel", 1.56f, 0.212f, M("counterFace"), 0, 0, 0);
            // counterFaceTex düzeni: yüz 1,56 × 0,212 m, 2000 px/m; modül 592 px, aralık 24, sol boşluk 32
            Vector3 Px(float px, float py, float z) => new Vector3(px / 2000f - 0.78f, 0.106f - py / 2000f, z);
            for (int i = 0; i < 5; i++)
            {
                float mx = 32 + i * (592 + 24);
                var module = Node(face, "Module " + i, Vector3.zero, Quaternion.identity);
                Plane(module, "Label plate", 576 / 2000f, 94 / 2000f, M("plate"), Px(mx + 8 + 288, 44 + 47, 0).x, Px(0, 91, 0).y, -0.0004f);
                Plane(module, "Window bezel", 548 / 2000f, 264 / 2000f, M("brassDark"), Px(mx + 22 + 274, 0, 0).x, Px(0, 146 + 132, 0).y, -0.0004f);
                Plane(module, "Window", 528 / 2000f, 244 / 2000f, M("bakelite"), Px(mx + 32 + 264, 0, 0).x, Px(0, 156 + 122, 0).y, -0.0008f);
            }
            // modül başına pirinç çerçeve (room.js › raised brass bezels)
            for (int i = 0; i < 5; i++)
            {
                float mx = -0.78f + 0.016f + i * 0.308f + 0.148f;
                var fr = Node(cb, "Bezel " + i, new Vector3(0, 0.865f, 1.7395f), R(-45 * D2R, 0, 0));
                const float wy = 0.106f - 0.139f, wh = 0.132f, ww = 0.27f;
                foreach (var (w, h, x, y) in new[] { (ww, 0.006f, mx, wy + wh / 2), (ww, 0.006f, mx, wy - wh / 2), (0.006f, wh, mx - ww / 2, wy), (0.006f, wh, mx + ww / 2, wy) })
                    Box(fr, "Bar", w, h, 0.008f, M("brass"), x, y, -0.004f);
            }
        }

        // room.js › key rail (durumlar Aşama 4'te StationKey ile)
        static void KeyRail(Transform desk)
        {
            var rail = Node(desk, "Key rail", new Vector3(-0.075f, 0.775f, 0.865f), R(12 * D2R, 0, 0));
            Box(rail, "Base", 1.27f, 0.03f, 0.15f, M("bakelite"), 0, 0, 0);
            Box(rail, "Brass edge", 1.28f, 0.008f, 0.008f, M("brassDark"), 0, 0.012f, -0.075f);
            float[] keyX = { -0.58f, -0.33f, -0.08f, 0.17f, 0.42f };
            string[] caps = { "1", "2", "3", "H", "Home" };
            string[] labels = { SiginakRoomWords.KeyCommand, SiginakRoomWords.KeyShelter, SiginakRoomWords.KeyMissions, SiginakRoomWords.KeyGuide, SiginakRoomWords.KeyDesk };
            for (int i = 0; i < 5; i++)
            {
                float capX = keyX[i] - 0.075f;
                var key = Node(rail, "Key " + caps[i], Vector3.zero, Quaternion.identity);
                Box(key, "Bezel", 0.072f, 0.008f, 0.072f, M("brass"), capX, 0.017f, 0);
                var cap = Node(key, "Cap", new Vector3(capX, 0.034f, 0), Quaternion.identity);
                Box(cap, "Body", 0.056f, 0.03f, 0.056f, M("bakelite"), 0, 0, 0);
                Label(cap, "Glyph", caps[i], DeskFont.CpBold, caps[i].Length > 1 ? 0.0165f : 0.03f, T.ColorOf("counter.digit"),
                    new Vector3(0, 0.0156f, 0), R(-PI / 2, 0, 0), new Vector2(0.05f, 0.05f), localize: false);
                Sphere(key, "Jewel", 0.009f, M("jewelOff"), capX, 0.02f, 0.05f);
                Cyl(key, "Jewel bezel", 0.012f, 0.008f, M("brass"), capX, 0.016f, 0.05f, seg: 16);
                float pw = i == 4 ? 0.12f : 0.15f;
                PlateWithText(key, "Plate", pw, 0.05f, new Vector3(capX + 0.045f + pw / 2, 0.0156f, 0), R(-PI / 2, 0, 0), (labels[i], 0.53f, 0.58f));
            }
        }

        // room.js › shift switch (kapalı hâl; durum makinesi Aşama 4/8'de ShiftSwitch ile)
        static void ShiftSwitch(Transform desk)
        {
            var sw = Group(desk, "Shift switch", 0.765f, 0.76f, 0.885f, 10);
            Box(sw, "Body", 0.30f, 0.09f, 0.20f, M("olive"), 0, 0.045f, 0);
            Box(sw, "Base", 0.31f, 0.01f, 0.21f, M("steelDark"), 0, 0.005f, 0);
            Box(sw, "Front slope", 0.30f, 0.012f, 0.075f, M("olive"), 0, 0.085f, -0.075f, rx: 32 * D2R);
            PlateWithText(sw, "Plate", 0.27f, 0.062f, new Vector3(0, 0.0925f, -0.081f), R(-58 * D2R, 0, 0),
                (SiginakRoomWords.SwitchPlate, 0.36f, 0.42f), (SiginakRoomWords.SwitchHint, 0.74f, 0.24f));
            Box(sw, "Lever base", 0.13f, 0.016f, 0.13f, M("brassDark"), 0.02f, 0.098f, 0.02f);
            var lever = Node(sw, "Lever", new Vector3(0.02f, 0.106f, 0.01f), R(-35 * D2R, 0, 0));
            Cyl(lever, "Rod", 0.009f, 0.075f, M("chrome"), 0, 0.0375f, 0, r2: 0.007f, seg: 12);
            Sphere(lever, "Knob", 0.011f, M("chrome"), 0, 0.077f, 0);
            var hinge = Node(sw, "Cover hinge", new Vector3(0.02f, 0.106f, 0.075f), Quaternion.identity);
            Box(hinge, "Top", 0.085f, 0.006f, 0.14f, M("coverRed"), 0, 0.092f, -0.07f);
            Label(hinge, "Stencil", SiginakRoomWords.SwitchCover, DeskFont.CpBold, 0.016f, T.ColorOf("materialsExtra.posterInk"),
                new Vector3(0, 0.0956f, -0.075f), R(-PI / 2, 0, PI / 2), new Vector2(0.075f, 0.024f), spacingEm: 0.1f);
            foreach (var sx in new[] { -0.0395f, 0.0395f }) Box(hinge, "Side", 0.006f, 0.09f, 0.14f, M("coverRed"), sx, 0.045f, -0.07f);
            Box(hinge, "Front", 0.085f, 0.09f, 0.006f, M("coverRed"), 0, 0.045f, -0.137f);
            Box(hinge, "Back", 0.085f, 0.09f, 0.006f, M("coverRed"), 0, 0.045f, -0.003f);
            Cyl(sw, "Ready lamp bezel", 0.013f, 0.01f, M("brass"), -0.10f, 0.095f, 0.03f);
            Sphere(sw, "Ready lamp", 0.01f, M("readyLamp"), -0.10f, 0.101f, 0.03f, cast: false);
        }

        // room.js › teleprinter (satırlar Aşama 4'te Teleprinter ile)
        static void Teleprinter(Transform desk)
        {
            var tp = Group(desk, "Teleprinter", -0.77f, 0.76f, 1.20f, -12);
            Box(tp, "Body", 0.30f, 0.16f, 0.36f, M("oliveLight"), 0, 0.08f, 0);
            Box(tp, "Top", 0.30f, 0.035f, 0.20f, M("oliveLight"), 0, 0.177f, -0.06f, rx: 10 * D2R);
            Box(tp, "Window", 0.15f, 0.004f, 0.07f, M("telexWindow"), 0.04f, 0.1955f, -0.07f, rx: 10 * D2R, cast: false);
            Box(tp, "Knob", 0.05f, 0.012f, 0.05f, M("bakelite"), -0.09f, 0.20f, -0.08f);
            Cyl(tp, "Paper roll", 0.045f, 0.22f, M("paperWhite"), 0, 0.215f, 0.12f, rz: PI / 2);
            foreach (var sx in new[] { -0.12f, 0.12f }) Box(tp, "Roll bracket", 0.012f, 0.07f, 0.06f, M("steelDark"), sx, 0.19f, 0.12f);
            PlateWithText(tp, "Plate", 0.12f, 0.03f, new Vector3(0, 0.10f, -0.1805f), Quaternion.identity, (SiginakRoomWords.TelexPlate, 0.53f, 0.58f));
            Plane(tp, "Paper drop", 0.2f, 0.17f, M("paper"), 0, 0.085f, -0.183f);
            Plane(tp, "Paper flat", 0.2f, 0.24f, M("paper"), 0, 0.0025f, -0.30f, rx: -PI / 2);
        }

        // room.js › desk lamp (banker)
        static void DeskLamp(Transform desk, Refs refs)
        {
            var lamp = Group(desk, "Desk lamp", 0.84f, 0.76f, 1.61f, 30);
            Cyl(lamp, "Base", 0.075f, 0.022f, M("brass"), 0, 0.011f, 0);
            Cyl(lamp, "Stem", 0.008f, 0.30f, M("brass"), 0, 0.17f, 0);
            Box(lamp, "Arm", 0.02f, 0.012f, 0.11f, M("brass"), 0, 0.31f, -0.05f);
            var shade = Node(lamp, "Shade", new Vector3(0, 0.33f, -0.10f), R(-10 * D2R, 0, 0));
            Cyl(shade, "Glass", 0.075f, 0.30f, M("lampGlass"), 0, 0, 0, rz: PI / 2, seg: 32, open: true, tl: PI);
            Cyl(shade, "Glass underside", 0.075f, 0.30f, M("lampGlass"), 0, 0, 0, rz: PI / 2, seg: 32, open: true, tl: PI, inside: true, cast: false);
            Cyl(shade, "Inner", 0.072f, 0.29f, M("lampInner"), 0, 0, 0, rz: PI / 2, seg: 32, open: true, tl: PI, inside: true, cast: false);
            foreach (var sx in new[] { -0.152f, 0.152f })
                Cyl(shade, "End cap", 0.076f, 0.006f, M("brass"), sx, 0, 0, rz: PI / 2, seg: 32, tl: PI);
            refs.LampBulb = Cyl(shade, "Bulb", 0.012f, 0.12f, M("lampBulb"), 0, 0.022f, 0, rz: PI / 2, cast: false).transform;
            Cyl(lamp, "Pull chain", 0.0015f, 0.08f, M("brass"), 0.05f, 0.27f, 0.06f, cast: false);
        }

        // room.js › right side desk props: evrak tepsisi, damga rafı, kupa, kalem, talaş, silgi
        static void Props(Transform desk)
        {
            var tray = Group(desk, "Paper tray", 0.765f, 0.76f, 1.30f, -4);
            Box(tray, "Floor", 0.26f, 0.006f, 0.33f, M("steelDark"), 0, 0.003f, 0);
            foreach (var (w, h, d, x, z) in new[] { (0.26f, 0.05f, 0.006f, 0f, -0.165f), (0.26f, 0.05f, 0.006f, 0f, 0.165f), (0.006f, 0.05f, 0.33f, -0.13f, 0f), (0.006f, 0.05f, 0.33f, 0.13f, 0f) })
                Box(tray, "Wall", w, h, d, M("steelDark"), x, 0.025f, z);
            Box(tray, "Papers", 0.22f, 0.02f, 0.29f, M("paper"), 0, 0.016f, 0.005f);
            Box(tray, "Top sheet", 0.21f, 0.004f, 0.28f, M("paperWhite"), 0.008f, 0.028f, -0.004f, ry: 3 * D2R);
            PlateWithText(tray, "Plate", 0.10f, 0.025f, new Vector3(0, 0.03f, -0.1695f), Quaternion.identity, (SiginakRoomWords.TrayPlate, 0.53f, 0.58f));

            var stamps = Group(desk, "Stamp rack", -0.76f, 0.76f, 1.55f, 8);
            Box(stamps, "Rack", 0.17f, 0.022f, 0.07f, M("wood"), 0, 0.011f, 0);
            foreach (var (sx, ink) in new[] { (-0.045f, "inkBlue"), (0.045f, "inkBlack") })
            {
                Box(stamps, "Stamp block", 0.055f, 0.022f, 0.04f, M("woodLight"), sx, 0.035f, 0);
                Box(stamps, "Stamp rubber", 0.055f, 0.004f, 0.04f, M(ink), sx, 0.023f, 0);
                Cyl(stamps, "Stamp stem", 0.009f, 0.04f, M("woodLight"), sx, 0.066f, 0);
                Sphere(stamps, "Stamp knob", 0.013f, M("woodLight"), sx, 0.092f, 0);
            }

            var mug = Group(desk, "Mug", 0.88f, 0.76f, 1.08f);
            Cyl(mug, "Body", 0.042f, 0.095f, M("enamel"), 0, 0.0475f, 0);
            Cyl(mug, "Coffee", 0.038f, 0.002f, M("coffee"), 0, 0.085f, 0, cast: false);
            Torus(mug, "Rim", 0.041f, 0.003f, 6, 24, 2 * PI, M("mugRim"), new Vector3(0, 0.095f, 0), R(PI / 2, 0, 0));
            Torus(mug, "Handle", 0.022f, 0.006f, 6, 16, PI, M("enamel"), new Vector3(0.042f, 0.05f, 0), R(0, 0, -PI / 2));

            var pen = Group(desk, "Pencil", 0.40f, 0.7625f, 0.945f, 62);
            Cyl(pen, "Body", 0.0045f, 0.16f, M("pencil"), 0, 0.0045f, 0, rx: PI / 2, seg: 6);
            Cyl(pen, "Tip", 0.0045f, 0.02f, M("pawn"), 0, 0.0045f, -0.09f, rx: -PI / 2, r2: 0.0005f, seg: 6);
            var shavings = Group(desk, "Shavings", 0, 0, 0);
            for (int i = 0; i < 7; i++)
                Torus(shavings, "Shaving " + i, 0.006f + (i % 3) * 0.002f, 0.0015f, 4, 10, PI * 1.4f, M("woodLight"),
                    new Vector3(0.47f + (i % 4) * 0.012f, 0.764f, 0.905f + (i / 4) * 0.014f), R(PI / 2, 0, i), cast: false);
            Box(desk, "Eraser", 0.045f, 0.012f, 0.022f, M("eraser"), 0.28f, 0.767f, 0.945f, ry: 20 * D2R);
        }
    }
}
