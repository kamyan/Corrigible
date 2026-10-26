using TMPro;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>Sol raf ve kesit maketi (room.js › left shelf with section model, buildDiorama).</summary>
    public static partial class SiginakRoomBuilder
    {
        /// <summary>Kesit maketindeki odalar: kimlik (ShelterLogic), three.js anahtarı, merkez x, taban y.</summary>
        public static readonly (string id, string name, float cx, float floorY)[] DioramaRooms =
        {
            ("living", "Living", -0.2075f, 0.32f), ("generator", "Generator", 0.2075f, 0.32f),
            ("workshop", "Workshop", -0.2075f, 0.085f), ("infirmary", "Infirmary", 0.2075f, 0.085f),
        };

        static void Shelf(Transform root, Refs refs)
        {
            var shelf = Group(root, "Shelf", -1.50f, 0, 1.72f, -35);
            refs.Shelf = shelf;
            foreach (var (x, z) in new[] { (-0.6f, -0.21f), (0.6f, -0.21f), (-0.6f, 0.21f), (0.6f, 0.21f) })
                Box(shelf, "Post", 0.035f, 2.0f, 0.035f, M("steelDark"), x, 1.0f, z);
            foreach (var y in new[] { 0.14f, 0.78f, 1.52f, 1.96f })
            {
                Box(shelf, "Board", 1.24f, 0.025f, 0.44f, M("steel"), 0, y - 0.0125f, 0);
                Box(shelf, "Board lip", 1.24f, 0.03f, 0.01f, M("steelDark"), 0, y - 0.015f, -0.222f);
            }
            PlateWithText(shelf, "Plate", 0.25f, 0.032f, new Vector3(0.20f, 0.763f, -0.2265f), Quaternion.identity, (SiginakRoomWords.ShelfPlate, 0.53f, 0.58f));

            // üst raf: sandıklar, bidon, umut işareti (filizli teneke)
            Crate(shelf, "Crate supplies", 0.30f, 0.20f, 0.26f, new Vector3(-0.36f, 1.62f, 0), SiginakRoomWords.CrateSupplies);
            Crate(shelf, "Crate materials", 0.30f, 0.17f, 0.26f, new Vector3(-0.02f, 1.605f, 0), SiginakRoomWords.CrateMaterials);
            Box(shelf, "Jerrycan", 0.16f, 0.25f, 0.09f, M("olive"), 0.25f, 1.645f, 0.02f);
            Box(shelf, "Jerrycan cap", 0.05f, 0.03f, 0.03f, M("olive"), 0.21f, 1.785f, 0.02f);
            var hope = Group(shelf, "Hope sprout", 0, 0, 0);
            Cyl(hope, "Tin", 0.035f, 0.065f, M("galv"), 0.46f, 1.5525f, -0.06f);
            Cyl(hope, "Soil", 0.032f, 0.004f, M("soil1"), 0.46f, 1.585f, -0.06f, cast: false);
            for (int i = 0; i < 3; i++)
                Box(hope, "Leaf " + i, 0.02f, 0.035f, 0.001f, M("leaf"), 0.46f + (i - 1) * 0.008f, 1.60f + i * 0.006f, -0.06f, rz: (i - 1) * 0.6f, ry: i);

            // alt raf: sandık, ilk yardım kutusu, klasörler
            Box(shelf, "Low crate", 0.34f, 0.22f, 0.30f, M("wood"), -0.38f, 0.25f, 0);
            Box(shelf, "First aid box", 0.26f, 0.15f, 0.12f, M("paperWhite"), 0.05f, 0.215f, 0.05f);
            Plane(shelf, "First aid sign", 0.11f, 0.11f, SiginakMaterials.Cutout("First aid sign", "decal_first_aid_iso7010", null, 0.6f), 0.05f, 0.215f, -0.0106f);
            for (int i = 0; i < 4; i++) Box(shelf, "Binder " + i, 0.05f, 0.28f, 0.22f, M("book" + (i + 1)), 0.32f + i * 0.06f, 0.28f, 0);

            var dio = Node(shelf, "Diorama", new Vector3(0.20f, 0.78f, -0.01f), Quaternion.identity);
            refs.Diorama = dio;
            Diorama(dio);
            // Maket tavan ampulüyle kıskaçlı pano arasında kalıyor ve özet panosunu (K4) karartıyordu; three.js referansında
            // pano aydınlık. Maket gölge düşürmez, gölge almaya devam eder; odaların kendi ışıkları var.
            foreach (var r in dio.GetComponentsInChildren<MeshRenderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // kıskaçlı pano (özet panosu, Aşama 6) ve BOŞTA tepsisi
            var cbd = Node(shelf, "Clipboard", new Vector3(-0.40f, 0.78f, 0.02f), R(-8 * D2R, 0, 0));
            Box(cbd, "Board", 0.38f, 0.56f, 0.008f, M("clipboard"), 0, 0.28f, 0);
            Box(cbd, "Paper", 0.35f, 0.50f, 0.002f, M("paper"), 0, 0.255f, -0.0055f);
            Box(cbd, "Clip", 0.13f, 0.035f, 0.018f, M("chrome"), 0, 0.535f, -0.01f);
            Box(shelf, "Clipboard ledge", 0.36f, 0.03f, 0.06f, M("wood"), -0.40f, 0.795f, 0.07f);
            var idle = Node(shelf, "Idle tray", new Vector3(-0.40f, 0.78f, -0.155f), Quaternion.identity);
            Box(idle, "Tray", 0.26f, 0.018f, 0.07f, M("wood"), 0, 0.009f, 0);
            Box(idle, "Lip", 0.26f, 0.026f, 0.006f, M("woodLight"), 0, 0.013f, -0.035f);
        }

        /// <summary>textures.js › crateTex: ahşap sandık, ön yüzde şablon boyalı yazı.</summary>
        static void Crate(Transform shelf, string name, float w, float h, float d, Vector3 pos, string label)
        {
            var crate = Node(shelf, name, pos, Quaternion.identity);
            Box(crate, "Body", w, h, d, M("wood"), 0, 0, 0);
            var paint = T.ColorOf("bunker.stencilPaint");
            paint.a = 0.82f;
            Label(crate, "Stencil", label, DeskFont.CpBold, h * 0.3f, paint, new Vector3(0, -h * 0.03f, -d / 2 - 0.0006f), Quaternion.identity,
                new Vector2(w * 0.9f, h * 0.5f), spacingEm: 0.06f);
        }

        // room.js › buildDiorama: çerçeve, yüzey, YZ direği, toprak, 2 × 2 oda, şaft, merdiven, eşyalar.
        // Oda durumları (ışık, duvar, piyonlar, hasar) Aşama 6'da RoomView ile; burada V.014 referans durumu kurulur.
        static void Diorama(Transform g)
        {
            const float W = 0.80f, H = 0.62f, D = 0.34f;
            var frame = Group(g, "Frame", 0, 0, 0);
            Box(frame, "Left", 0.02f, H, D, M("wood"), -W / 2 + 0.01f, H / 2, 0);
            Box(frame, "Right", 0.02f, H, D, M("wood"), W / 2 - 0.01f, H / 2, 0);
            Box(frame, "Bottom", W, 0.02f, D, M("wood"), 0, 0.01f, 0);
            Box(frame, "Back", W, H, 0.01f, M("soil3"), 0, H / 2, D / 2 - 0.005f);

            var surface = Group(g, "Surface", 0, 0, 0);
            Box(surface, "Rubble strip", W - 0.04f, 0.012f, D - 0.02f, M("rubble"), 0, 0.566f, 0);
            foreach (var (x, h, w) in new[] { (-0.31f, 0.05f, 0.06f), (-0.22f, 0.03f, 0.05f), (-0.12f, 0.065f, 0.04f), (0.05f, 0.035f, 0.07f), (0.14f, 0.02f, 0.05f), (0.31f, 0.045f, 0.05f) })
                Box(surface, "Ruin", w, h, 0.05f, M("ruin"), x, 0.572f + h / 2, 0.03f + x * 7 % 0.05f);
            Box(surface, "Debris", 0.035f, 0.012f, 0.012f, M("ruin"), -0.12f, 0.64f, 0.03f, rz: 0.5f);
            Cyl(surface, "AI mast", 0.0025f, 0.08f, M("steelDark"), 0.24f, 0.612f, -0.02f);
            Sphere(surface, "AI beacon", 0.006f, M("mastRed"), 0.24f, 0.655f, -0.02f, cast: false);

            var strata = Group(g, "Strata", 0, 0, 0);
            Box(strata, "Soil 2", W - 0.04f, 0.025f, D - 0.02f, M("soil2"), 0, 0.5475f, 0, cast: false);
            Box(strata, "Soil 1", W - 0.04f, 0.025f, D - 0.02f, M("soil1"), 0, 0.5225f, 0, cast: false);
            Box(strata, "Soil 3", W - 0.04f, 0.02f, D - 0.02f, M("soil3"), 0, 0.50f, 0, cast: false);
            Box(strata, "Top slab", W - 0.04f, 0.02f, D - 0.02f, M("slab"), 0, 0.48f, 0);
            Box(strata, "Card band low", W - 0.04f, 0.065f, D - 0.02f, M("slab"), 0, 0.0525f, 0);
            Box(strata, "Card band mid", W - 0.04f, 0.065f, D - 0.02f, M("slab"), 0, 0.2875f, 0);
            foreach (var x in new[] { -0.375f, 0.375f, -0.04f, 0.04f }) Box(strata, "Wall", 0.01f, 0.39f, D - 0.02f, M("slab"), x, 0.28f, 0);
            var ladder = Group(g, "Ladder", 0, 0, 0);
            foreach (var x in new[] { -0.022f, 0.022f }) Box(ladder, "Rail", 0.004f, 0.39f, 0.004f, M("steelDark"), x, 0.28f, 0.10f);
            for (float y = 0.1f; y < 0.47f; y += 0.03f) Box(ladder, "Rung", 0.044f, 0.003f, 0.003f, M("steelDark"), 0, y, 0.10f);

            const float roomH = 0.17f, roomW = 0.325f;
            foreach (var (id, name, cx, fy) in DioramaRooms)
            {
                bool powered = id == "living" || id == "generator";   // V.014 referans durumu
                var room = Group(g, "Room " + name, 0, 0, 0);
                Plane(room, "Back wall", roomW, roomH, M(powered ? "roomWall" : "roomWallOff"), cx, fy + roomH / 2, D / 2 - 0.012f);
                Box(room, "Floor", roomW, 0.004f, D - 0.03f, M(powered ? "roomFloor" : "roomFloorOff"), cx, fy + 0.002f, 0, cast: false);
                Sphere(room, "Bulb", 0.007f, M(powered ? "bulbGlass" : "roomBulbOff"), cx, fy + roomH - 0.012f, 0.02f, cast: false);
                var light = AddLight(room, "Room light", "roomBulb", new Vector3(cx, fy + roomH - 0.03f, 0), LightType.Point, 0);
                light.enabled = powered;
                Box(room, "Brass edge", roomW, 0.004f, 0.004f, M("brassDark"), cx, fy + 0.001f, -D / 2 + 0.012f, cast: false);
                var holder = Group(room, "Card holder", 0, 0, 0);
                Box(holder, "Top", 0.20f, 0.004f, 0.006f, M("brass"), cx, fy - 0.006f, -D / 2 + 0.006f, cast: false);
                Box(holder, "Bottom", 0.20f, 0.004f, 0.006f, M("brass"), cx, fy - 0.064f, -D / 2 + 0.006f, cast: false);
                foreach (var sx in new[] { -0.1f, 0.1f }) Box(holder, "Side", 0.004f, 0.062f, 0.006f, M("brass"), cx + sx, fy - 0.035f, -D / 2 + 0.006f, cast: false);
                RoomProps(Group(room, "Props", 0, 0, 0), id, D);
            }
        }

        static void RoomProps(Transform p, string id, float D)
        {
            switch (id)
            {
                case "living":
                    foreach (var bx in new[] { -0.33f, -0.21f })
                    {
                        foreach (var by in new[] { 0.335f, 0.385f }) Box(p, "Bunk", 0.10f, 0.012f, 0.055f, M("blanket"), bx, by, 0.09f);
                        foreach (var (px, pz) in new[] { (-0.05f, 0.065f), (0.05f, 0.065f), (-0.05f, 0.115f), (0.05f, 0.115f) })
                            Box(p, "Bunk post", 0.004f, 0.08f, 0.004f, M("steelDark"), bx + px, 0.36f, pz);
                    }
                    Box(p, "Table", 0.05f, 0.004f, 0.04f, M("wood"), -0.10f, 0.355f, 0.07f);
                    Box(p, "Table leg", 0.004f, 0.032f, 0.004f, M("wood"), -0.10f, 0.338f, 0.07f);
                    break;
                case "generator":
                    foreach (var gx in new[] { 0.12f, 0.20f, 0.28f }) Cyl(p, "Generator", 0.026f, 0.09f, M("genGreen"), gx, 0.365f, 0.08f);
                    Cyl(p, "Manifold", 0.006f, 0.20f, M("pipe"), 0.20f, 0.42f, 0.08f, rz: PI / 2);
                    Box(p, "Switchbox", 0.04f, 0.06f, 0.02f, M("steelDark"), 0.34f, 0.355f, 0.12f);
                    break;
                case "workshop":
                    Box(p, "Bench", 0.17f, 0.006f, 0.055f, M("wood"), -0.22f, 0.13f, 0.10f);
                    foreach (var px in new[] { -0.295f, -0.145f }) Box(p, "Bench leg", 0.006f, 0.045f, 0.05f, M("wood"), px, 0.107f, 0.10f);
                    Plane(p, "Pegboard", 0.16f, 0.07f, M("pegboard"), -0.22f, 0.19f, D / 2 - 0.014f);
                    Box(p, "Vise", 0.02f, 0.015f, 0.02f, M("steelDark"), -0.16f, 0.14f, 0.09f);
                    break;
                case "infirmary":
                    Box(p, "Cot", 0.12f, 0.018f, 0.05f, M("paperWhite"), 0.16f, 0.105f, 0.09f, rz: 0.06f);
                    Box(p, "Cabinet", 0.05f, 0.09f, 0.04f, M("paperWhite"), 0.32f, 0.13f, 0.11f);
                    Plane(p, "First aid sign", 0.03f, 0.03f, SiginakMaterials.Cutout("First aid sign", "decal_first_aid_iso7010", null, 0.6f), 0.32f, 0.14f, 0.089f);
                    var damage = Group(p, "Damage", 0, 0, 0);
                    var rubble = new[] { (0.22f, 0.095f, -0.02f, 0.03f, 0.02f), (0.26f, 0.092f, 0.02f, 0.025f, 0.018f), (0.12f, 0.093f, 0.03f, 0.02f, 0.015f), (0.29f, 0.094f, -0.06f, 0.028f, 0.02f), (0.18f, 0.091f, -0.07f, 0.022f, 0.014f), (0.24f, 0.1f, 0.0f, 0.018f, 0.016f) };
                    for (int i = 0; i < rubble.Length; i++)
                    {
                        var (x, y, z, s, h) = rubble[i];
                        Box(damage, "Rubble", s, h, s * 0.9f, M("rubble"), x, y, z, ry: i * 0.7f, rz: i * 0.3f);
                    }
                    Box(damage, "Fallen beam", 0.24f, 0.016f, 0.016f, M("wood"), 0.20f, 0.16f, -0.01f, rz: -0.42f);
                    Plane(damage, "Crack A", 0.012f, 0.12f, M("crack"), 0.25f, 0.17f, D / 2 - 0.0135f, rz: 0.4f);
                    Plane(damage, "Crack B", 0.01f, 0.07f, M("crack"), 0.21f, 0.20f, D / 2 - 0.0135f, rz: -0.5f);
                    break;
            }
        }
    }
}
