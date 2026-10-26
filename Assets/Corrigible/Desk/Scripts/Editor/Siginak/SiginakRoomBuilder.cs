using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Sığınak komuta odasını DeskGame sahnesinde kurar ya da yeniden kurar (Docs/UI_Siginak PROMPT §4, Aşama 2).
    /// Yerleşim ve ölçüler <c>reference/scene_threejs/room.js</c>'ten satır satır aktarılır. Dönüşüm kuralı:
    /// yardımcıların (lbox, lcyl, lplane, group) konumu aynen; ham <c>position.set(a, b, c)</c> → (a, b, −c); açılar
    /// three.js Euler XYZ'den quaternion ile çevrilir (X ve Y işaret değiştirir, Z değiştirmez; README'deki “bütün açılar
    /// ters” kuralı Z için geçerli değil, bkz. IMPLEMENTATION_MAP). Tekrar çalıştırıldığında eski <c>SiginakRoom</c>
    /// silinir ve baştan kurulur; sahnedeki <c>SiginakPresentationSwitch</c> yeni köke bağlanır.
    /// Kalıcı kurucu Aşama 10'da silinir (IMPLEMENTATION_MAP › Soru 2); sahne asıl kaynaktır.
    /// </summary>
    public static partial class SiginakRoomBuilder
    {
        public const string RootName = "SiginakRoom";

        static SiginakTheme T => SiginakTheme.Active;
        static SiginakSettings Settings => SettingsAsset();

        [MenuItem("Corrigible/Sığınak/Odayı kur")]
        public static string Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.path.EndsWith("/DeskGame.unity")) return "Önce DeskGame sahnesini aç: " + scene.path;
            SiginakMaterials.ClearCache();
            bool wasNew = PrepareRebuild();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == RootName).ToArray()) Object.DestroyImmediate(old);

            var root = new GameObject(RootName).transform;
            SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            var refs = new Refs { Root = root };
            Shell(root);
            Bulb(root, refs);
            var desk = Group(root, "Desk", 0, 0, 0);
            Desk(desk, refs);
            Shelf(root, refs);
            CrtDesk(root, refs);
            Lights(root, refs);
            Poses(root, refs);
            var camera = scene.GetRootGameObjects().FirstOrDefault(g => g.name == LegacyRoot)?.GetComponentInChildren<Camera>(true);
            var hud = BuildHud(root, camera);
            var controls = AddControls(root, hud);

            string wiring = Wire(scene, refs, wasNew, hud, controls);
            // Bütün parçalar kurulduktan sonra (harita, sığınak, CRT, vardiya ve brifing Wire içinde kurulur)
            MarkStatic(root);
            EditorSceneManager.MarkSceneDirty(scene);
            int renderers = root.GetComponentsInChildren<MeshRenderer>(true).Length;
            int texts = root.GetComponentsInChildren<TextMeshPro>(true).Length;
            return $"{RootName}: {renderers} mesh, {texts} yazı, {root.GetComponentsInChildren<Light>(true).Length} ışık · {wiring}";
        }

        /// <summary>Kurulum sırasında sonraki aşamaların bağlayacağı noktalar.</summary>
        sealed class Refs
        {
            public Transform Root, MapMount, Shelf, Diorama, CrtScreen, Radio, LampBulb, PoseK1, PoseMap, PoseK4, PoseK5a, PoseK6a, PoseCrtDive;
            public Light Bulb, DeskLamp, CrtLeak, RadioDial;
        }

        // ------------------------------------------------------------------ yardımcılar (room.js › lib.js)

        /// <summary>three.js Euler XYZ (radyan) → Unity quaternion: three'deki q = qx·qy·qz, sonra z aynası (−x, −y, z, w).</summary>
        static Quaternion R(float rx, float ry, float rz)
        {
            var q = new Quaternion(Mathf.Sin(rx / 2), 0, 0, Mathf.Cos(rx / 2)) *
                    new Quaternion(0, Mathf.Sin(ry / 2), 0, Mathf.Cos(ry / 2)) *
                    new Quaternion(0, 0, Mathf.Sin(rz / 2), Mathf.Cos(rz / 2));
            return new Quaternion(-q.x, -q.y, q.z, q.w);
        }

        /// <summary>Test ve sonraki aşamalar için: three.js Euler XYZ (radyan) → Unity dönüşü.</summary>
        public static Quaternion ThreeEuler(float rx, float ry, float rz) => R(rx, ry, rz);

        const float D2R = Mathf.Deg2Rad;
        const float PI = Mathf.PI;

        static Transform Group(Transform parent, string name, float x, float y, float z, float yawDeg = 0)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = new Vector3(x, y, z);
            t.localRotation = Quaternion.Euler(0, yawDeg, 0);
            return t;
        }

        static Transform Node(Transform parent, string name, Vector3 pos, Quaternion rot)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = rot;
            return t;
        }

        static GameObject Primitive(PrimitiveType type, Transform parent, string name, Material m, Vector3 pos, Quaternion rot, Vector3 scale, bool cast)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            Render(go, m, cast);
            return go;
        }

        static void Render(GameObject go, Material m, bool cast)
        {
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
        }

        /// <summary>lbox: konum Unity koordinatıyla (yardımcı biçimi), açı three radyan.</summary>
        static GameObject Box(Transform p, string name, float w, float h, float d, Material m, float x, float y, float z,
            float rx = 0, float ry = 0, float rz = 0, bool cast = true) =>
            Primitive(PrimitiveType.Cube, p, name, m, new Vector3(x, y, z), R(rx, ry, rz), new Vector3(w, h, d), cast);

        /// <summary>lcyl / THREE.CylinderGeometry. Düz silindir Unity primitive'i; konik, açık ya da yarım olan kalıcı mesh.</summary>
        static GameObject Cyl(Transform p, string name, float r, float h, Material m, float x, float y, float z,
            float rx = 0, float ry = 0, float rz = 0, bool cast = true, float r2 = -1, int seg = 24, bool open = false,
            float t0 = 0, float tl = 2 * PI, bool inside = false)
        {
            float top = r2 < 0 ? r : r2;
            if (Mathf.Approximately(top, r) && !open && Mathf.Approximately(tl, 2 * PI) && !inside)
                return Primitive(PrimitiveType.Cylinder, p, name, m, new Vector3(x, y, z), R(rx, ry, rz), new Vector3(2 * r, h / 2, 2 * r), cast);
            return Mesh(p, name, SiginakMeshLib.Cylinder(top, r, h, seg, open, t0, tl, inside), m, new Vector3(x, y, z), R(rx, ry, rz), Vector3.one, cast);
        }

        static GameObject Sphere(Transform p, string name, float r, Material m, float x, float y, float z, bool cast = true) =>
            Primitive(PrimitiveType.Sphere, p, name, m, new Vector3(x, y, z), Quaternion.identity, Vector3.one * 2 * r, cast);

        /// <summary>lplane / PlaneGeometry: Quad, ön yüzü koltuğa (−z). Varsayılan gölge düşürmez.</summary>
        static GameObject Plane(Transform p, string name, float w, float h, Material m, float x, float y, float z,
            float rx = 0, float ry = 0, float rz = 0, bool cast = false) =>
            Primitive(PrimitiveType.Quad, p, name, m, new Vector3(x, y, z), R(rx, ry, rz), new Vector3(w, h, 1), cast);

        static GameObject Torus(Transform p, string name, float radius, float tube, int radialSeg, int tubularSeg, float arc, Material m,
            Vector3 pos, Quaternion rot, bool cast = true) =>
            Mesh(p, name, SiginakMeshLib.Torus(radius, tube, radialSeg, tubularSeg, arc), m, pos, rot, Vector3.one, cast);

        static GameObject Mesh(Transform p, string name, Mesh mesh, Material m, Vector3 pos, Quaternion rot, Vector3 scale, bool cast)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            Render(go, m, cast);
            return go;
        }

        static Material M(string name) => SiginakMaterials.Get(name);

        /// <summary>
        /// Odadaki yazı (TMP 3D, URP Lit malzemesiyle ışık alır). Yazı kendi XY düzleminde, ön yüzü −z; genişlik/yükseklik
        /// metre. <paramref name="em"/> harf (em) yüksekliği metre. Sığmazsa küçülür (plaka; JS textFit gibi).
        /// <paramref name="localize"/>: kaynak Türkçe metin <see cref="DeskLocText"/> ile çevrilir.
        /// </summary>
        static TextMeshPro Label(Transform parent, string name, string text, DeskFont font, float em, Color color, Vector3 pos,
            Quaternion rot, Vector2 size, TextAlignmentOptions align = TextAlignmentOptions.Center, bool localize = true,
            float spacingEm = 0, bool lit = true, bool wrap = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.localPosition = pos;
            rt.localRotation = rot;
            rt.sizeDelta = size;
            var t = go.AddComponent<TextMeshPro>();
            var asset = DeskTheme.Active.Font(font);
            t.font = asset;
            // Koyu mürekkep ışıksız TMP ile koyu kalır (Lit gölgede ortam rengine kayıyordu); açık boya ışık alır, parlamasın
            t.fontSharedMaterial = lit && color.grayscale > 0.35f ? SiginakMaterials.TextLit(asset) : asset.material;
            t.text = text;
            t.fontSize = em * TmpWorldScale;
            t.enableAutoSizing = true;
            t.fontSizeMax = t.fontSize;
            t.fontSizeMin = t.fontSize * 0.4f;
            t.color = color;
            t.alignment = align;
            t.characterSpacing = spacingEm * 100;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (localize) go.AddComponent<DeskLocText>();
            return t;
        }

        /// <summary>TMP 3D (TextMeshPro) yazısında font boyutu 10 ≈ 1 dünya birimi em.</summary>
        const float TmpWorldScale = 10f;

        /// <summary>Düzlemin önüne (−z) çok az kaydırılmış yazı konumu (z-fighting olmasın).</summary>
        static Vector3 Front(float x, float y, float z) => new Vector3(x, y, z - 0.0006f);

        // ------------------------------------------------------------------ oda kabuğu (room.js › room shell)

        static void Shell(Transform root)
        {
            var shell = Group(root, "Shell", 0, 0, 0);
            Plane(shell, "Back wall", 4.4f, 2.5f, SiginakMaterials.Textured("Concrete back", "tx_concrete_back", "concreteWall"), 0, 1.25f, 2.30f);
            var side = SiginakMaterials.Textured("Concrete side", "tx_concrete_side", "concreteWall");
            Plane(shell, "Left wall", 3.0f, 2.5f, side, -2.2f, 1.25f, 0.8f, ry: PI / 2);
            Plane(shell, "Right wall", 3.0f, 2.5f, side, 2.2f, 1.25f, 0.8f, ry: -PI / 2);
            Plane(shell, "Floor", 4.4f, 3.0f, SiginakMaterials.Textured("Concrete floor", "tx_concrete_floor", "floor"), 0, 0, 0.8f, rx: -PI / 2);
            Plane(shell, "Ceiling", 4.4f, 3.0f, M("ceiling"), 0, 2.5f, 0.8f, rx: PI / 2);

            // kanal ve borular
            Box(shell, "Duct", 4.4f, 0.22f, 0.26f, M("galv"), 0, 2.30f, 2.16f);
            int rib = 0;
            for (float x = -2.0f; x <= 2.0001f; x += 0.6f) Box(shell, "Duct rib " + rib++, 0.02f, 0.24f, 0.28f, M("steelDark"), x, 2.30f, 2.16f);
            Cyl(shell, "Pipe A", 0.038f, 2.5f, M("pipe"), 1.03f, 1.25f, 2.22f);
            Cyl(shell, "Pipe B", 0.03f, 2.5f, M("pipe"), 1.13f, 1.25f, 2.23f);
            Torus(shell, "Valve wheel", 0.075f, 0.012f, 8, 24, 2 * PI, M("red"), new Vector3(1.03f, 1.32f, 2.16f), Quaternion.identity);
            Cyl(shell, "Valve stem", 0.012f, 0.06f, M("steelDark"), 1.03f, 1.32f, 2.19f, rx: PI / 2);
            foreach (var y in new[] { 0.55f, 1.75f }) Box(shell, "Pipe clamp", 0.2f, 0.03f, 0.05f, M("steelDark"), 1.08f, y, 2.25f);
            Cyl(shell, "Conduit", 0.018f, 2.5f, M("galv"), -1.12f, 1.25f, 2.27f);
            Box(shell, "Junction box", 0.14f, 0.18f, 0.07f, M("galv"), -1.12f, 1.78f, 2.25f);
            Cyl(shell, "Cable", 0.012f, 1.0f, M("rubber"), -0.62f, 2.02f, 2.27f, rz: PI / 2);

            // duvar yazısı (Big Shoulders yerine CP 700 yedeği, IMPLEMENTATION_MAP › Soru 8)
            Label(shell, "Wall stencil", SiginakRoomWords.WallSign, DeskFont.CpBold, 0.105f, T.ColorOf("bunker.stencilPaint"),
                Front(-0.05f, 1.77f, 2.295f), Quaternion.identity, new Vector2(0.84f, 0.15f), spacingEm: 0.08f);

            // doktrin afişi
            var poster = Node(shell, "Doctrine poster", new Vector3(0.66f, 1.38f, 2.293f), R(0, 0, -1.5f * D2R));
            Plane(poster, "Paper", 0.42f, 0.57f, M("paper"), 0, 0, 0);
            Plane(poster, "Panel", 0.42f - 0.034f, 0.57f - 0.034f, M("posterRed"), 0, 0, -0.0004f);
            Plane(poster, "Pictogram", 0.42f, 0.42f * 320 / 880, SiginakMaterials.Cutout("Poster pictogram", "tx_poster_pictogram", null, 0.6f),
                0, 0.57f / 2 - (230 + 160) / 1200f * 0.57f, -0.0008f);
            var ink = T.ColorOf("materialsExtra.posterInk");
            float PosterY(float py) => 0.57f / 2 - py / 1200f * 0.57f;
            Label(poster, "Line 1", SiginakRoomWords.PosterLine1, DeskFont.CpBold, 0.057f, ink, new Vector3(0, PosterY(665), -0.001f), Quaternion.identity, new Vector2(0.36f, 0.07f));
            Label(poster, "Line 2", SiginakRoomWords.PosterLine2, DeskFont.CpBold, 0.057f, ink, new Vector3(0, PosterY(785), -0.001f), Quaternion.identity, new Vector2(0.35f, 0.07f));
            Plane(poster, "Rule", 0.42f - 240 / 880f * 0.42f, 8 / 1200f * 0.57f, M("posterInk"), 0, PosterY(884), -0.0008f);
            Label(poster, "Line 3", SiginakRoomWords.PosterLine3, DeskFont.CpBold, 0.044f, ink, new Vector3(0, PosterY(960), -0.001f), Quaternion.identity, new Vector2(0.34f, 0.06f));
            Label(poster, "Footer", SiginakRoomWords.PosterFooter, DeskFont.CpBold, 0.016f, ink, new Vector3(0, PosterY(1080), -0.001f), Quaternion.identity, new Vector2(0.34f, 0.025f));
            foreach (var (dx, dy) in new[] { (-0.19f, 0.27f), (0.19f, 0.27f), (-0.19f, -0.27f) })
                Plane(shell, "Poster tape", 0.06f, 0.025f, M("tape"), 0.66f + dx, 1.38f + dy, 2.291f, rz: (dx > 0 ? 30 : -30) * D2R);

            // mantar pano ve notlar
            var board = Group(shell, "Cork board", 0, 0, 0);
            Box(board, "Frame", 0.64f, 0.46f, 0.02f, M("wood"), -0.62f, 1.36f, 2.285f);
            Plane(board, "Cork", 0.60f, 0.42f, M("cork"), -0.62f, 1.36f, 2.274f);
            Note(board, "Freed regions", true, -0.76f, 1.42f, 0.24f, 0.17f, -2, null, null);
            Note(board, "Missing A", false, -0.47f, 1.46f, 0.15f, 0.107f, 3, "Elif Y.", SiginakRoomWords.MissingAPlace);
            Note(board, "Missing B", false, -0.50f, 1.29f, 0.15f, 0.107f, -4, "Marek D.", "Gdańsk");
            Note(board, "Missing C", false, -0.82f, 1.24f, 0.14f, 0.10f, 2.5f, "Ana R.", "Porto");
        }

        /// <summary>textures.js › noteTex: liste (KURTARILAN BÖLGELER) ya da KAYIP ilanı; yazılar TMP.</summary>
        static void Note(Transform board, string name, bool list, float x, float y, float w, float h, float rotDeg, string person, string place)
        {
            var n = Node(board, name, new Vector3(x, y, 2.271f), R(0, 0, rotDeg * D2R));
            Plane(n, "Paper", w, h, M(list ? "paperWhite" : "paper"), 0, 0, 0);
            Sphere(n, "Pin", 0.008f, M(list ? "pinBlue" : "pinRed"), 0, h / 2 - 0.015f, -0.005f);
            var ink = T.ColorOf("ink");
            float px = w / 420f;   // not dokusu 420 × 300 px
            Vector3 At(float tx, float ty) => new Vector3(-w / 2 + tx * px, h / 2 - ty * px, -0.0006f);
            if (list)
            {
                Label(n, "Title", SiginakRoomWords.FreedTitle, DeskFont.CpBold, 30 * px, ink, At(210, 40), Quaternion.identity,
                    new Vector2(w - 60 * px, 36 * px), TextAlignmentOptions.Left);
                for (int i = 0; i < 3; i++)
                {
                    var line = Label(n, "Entry " + (i + 1), (i + 1) + ".", DeskFont.KalamBold, 32 * px, T.ColorOf("stamp.free"),
                        At(230, 92 + i * 34), Quaternion.identity, new Vector2(w - 70 * px, 34 * px), TextAlignmentOptions.Left, localize: false);
                    line.color = i == 0 ? T.ColorOf("stamp.free") : new Color(ink.r, ink.g, ink.b, 0.35f);
                }
                return;
            }
            Label(n, "Title", SiginakRoomWords.Missing, DeskFont.CpBold, 46 * px, ink, At(110, 40), Quaternion.identity, new Vector2(180 * px, 50 * px), TextAlignmentOptions.Left);
            Plane(n, "Photo", 120 * px, 150 * px, M("paperWhite"), At(82, 151).x, At(82, 151).y, -0.0003f);
            Label(n, "Name", person, DeskFont.KalamBold, 34 * px, ink, At(280, 110), Quaternion.identity, new Vector2(240 * px, 40 * px), TextAlignmentOptions.Left, localize: false);
            Label(n, "Place", place, DeskFont.KalamBold, 26 * px, ink, At(280, 152), Quaternion.identity, new Vector2(240 * px, 32 * px), TextAlignmentOptions.Left,
                localize: place == SiginakRoomWords.MissingAPlace);
            Label(n, "Signature", "— KYGA-07", DeskFont.CpRegular, 22 * px, ink, At(280, 202), Quaternion.identity, new Vector2(240 * px, 28 * px), TextAlignmentOptions.Left, localize: false);
        }

        // ------------------------------------------------------------------ tavan ampulü

        static void Bulb(Transform root, Refs refs)
        {
            const float bx = 0.12f, by = 2.02f, bz = 1.70f;
            var g = Group(root, "Ceiling bulb", 0, 0, 0);
            Cyl(g, "Cord", 0.004f, 2.5f - by - 0.05f, M("rubber"), bx, (2.5f + by + 0.05f) / 2, bz, cast: false);
            Cyl(g, "Socket", 0.018f, 0.05f, M("bakelite"), bx, by + 0.05f, bz);
            var glass = Sphere(g, "Glass", 0.032f, M("bulbGlass"), bx, by, bz, cast: false);
            for (int i = 0; i < 4; i++)
                Torus(g, "Cage " + i, 0.05f, 0.002f, 4, 20, 2 * PI, M("steelDark"), new Vector3(bx, by, bz), R(0, i * PI / 4, 0));
            refs.Bulb = AddLight(g, "Bulb light", "bulb", new Vector3(bx, by - 0.03f, bz), LightType.Point, 1024);
            refs.Bulb.intensity = Settings.bulbIntensity;
            g.gameObject.AddComponent<BulbFlicker>().Configure(refs.Bulb, glass.GetComponent<MeshRenderer>());
        }

        // ------------------------------------------------------------------ ışıklar (room.js › lights; lights.*.urpStart)

        static Light AddLight(Transform parent, string name, string token, Vector3 localPos, LightType type, int shadowResolution)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.GetComponent<Light>();
            l.type = type;
            l.color = T.ColorOf("lights." + token + ".color");
            l.intensity = T.NumberOf("lights." + token + ".urpStart.intensity", 1);
            l.range = T.NumberOf("lights." + token + ".urpStart.range", 2);
            l.shadows = shadowResolution > 0 ? LightShadows.Soft : LightShadows.None;
            l.lightmapBakeType = LightmapBakeType.Realtime;
            var data = go.AddComponent<UniversalAdditionalLightData>();
            if (shadowResolution > 0)
            {
                // Çözünürlük kademesi yalnız serileştirilmiş alandan yazılabiliyor (özel = −1)
                var so = new SerializedObject(data);
                so.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom;
                so.ApplyModifiedPropertiesWithoutUndo();
                l.shadowCustomResolution = shadowResolution;
                l.shadowNearPlane = 0.05f;
            }
            return l;
        }

        static void Lights(Transform root, Refs refs)
        {
            var lamp = AddLight(root, "Desk lamp light", "deskLamp", root.InverseTransformPoint(refs.LampBulb.position), LightType.Spot, 2048);
            var target = new Vector3(0.02f, 0.76f, 1.22f);
            lamp.transform.rotation = Quaternion.LookRotation(target - lamp.transform.position);
            // three SpotLight açısı yarım açıdır (lights.deskLamp.angle = 47°); Unity spotAngle tam koni
            lamp.spotAngle = 2 * T.NumberOf("lights.deskLamp.angle", 47);
            lamp.innerSpotAngle = lamp.spotAngle * (1 - T.NumberOf("lights.deskLamp.penumbra", 0.55f));
            lamp.intensity = Settings.deskLampIntensity;
            refs.DeskLamp = lamp;

            // CRT sızıntısı: ekranın 0,42 m önü, 0,05 m altı (three yerel +z = Unity −z)
            refs.CrtLeak = AddLight(refs.CrtScreen, "CRT leak light", "crtLeak", new Vector3(0, -0.05f, -0.42f), LightType.Point, 0);
            refs.CrtLeak.intensity = Settings.crtLeakIntensity;
            refs.RadioDial = AddLight(refs.Radio, "Radio dial light", "radioDial", new Vector3(0.07f, 0.14f, -0.2f), LightType.Point, 0);
        }

        // ------------------------------------------------------------------ kamera pozları (tokens.cameras)

        static void Poses(Transform root, Refs refs)
        {
            var poses = Group(root, "Poses", 0, 0, 0);
            refs.PoseK1 = Node(poses, "K1", T.Vector("cameras.K1.pos"), Quaternion.Euler(T.Vector("cameras.K1.rotDeg")));
            refs.PoseMap = LookPose(poses, "Map", T.Vector("cameras.map.pos"), T.Vector("cameras.map.target"));
            // render.html P.K4/P.K5a: ham three.js yerel noktası (localToWorld(c.set(...))) → Unity yerel (x, y, −z)
            var shelfLocal = T.Vector("cameras.K4.localPos");
            var shelfTarget = T.Vector("cameras.K4.localTarget");
            refs.PoseK4 = LookPose(poses, "K4", refs.Shelf.TransformPoint(new Vector3(shelfLocal.x, shelfLocal.y, -shelfLocal.z)),
                refs.Shelf.TransformPoint(new Vector3(shelfTarget.x, shelfTarget.y, -shelfTarget.z)));
            var screenLocal = T.Vector("cameras.K5a.screenLocalPos");
            var screenTarget = T.Vector("cameras.K5a.screenLocalTarget");
            refs.PoseK5a = LookPose(poses, "K5a", refs.CrtScreen.TransformPoint(new Vector3(screenLocal.x, screenLocal.y, -screenLocal.z)),
                refs.CrtScreen.TransformPoint(new Vector3(screenTarget.x, screenTarget.y, -screenTarget.z)));
            refs.PoseK6a = LookPose(poses, "K6a", T.Vector("cameras.K6a.pos"), T.Vector("cameras.K6a.target"));
            // Baskın emrinde RaidLauncher'ın yaklaştığı nokta: ekran 40° FOV'da kadrajı taşacak kadar yakın (“ekranın içine girer”)
            refs.PoseCrtDive = LookPose(poses, "CrtDive", refs.CrtScreen.TransformPoint(new Vector3(0, 0, -0.2f)), refs.CrtScreen.position);
        }

        static Transform LookPose(Transform parent, string name, Vector3 worldPos, Vector3 worldTarget)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = worldPos;
            t.rotation = Quaternion.LookRotation(worldTarget - worldPos, Vector3.up);
            return t;
        }

        /// <summary>Yalnız hiç kıpırdamayan kabuk statik işaretlenir; masa nesneleri sonraki aşamalarda hareket eder.</summary>
        /// <summary>Hareket eden ya da her karede malzemesi değişen parçalar (statik birleştirmeye girmez).</summary>
        static readonly string[] MovingParts =
            { "Desk/Key rail", "Desk/Shift switch/Cover hinge", "Desk/Shift switch/Lever", "Desk/MapMount", "Desk/Counter bank", "Ceiling bulb", "Shelf/Idle tray" };

        /// <summary>
        /// Aşama 10 performans (1920×1080'de yeni oda eski odadan pahalıydı): hareket etmeyen bütün parçalar statik birleştirilir
        /// (maketteki piyon ve yuvalar hariç: oda bağlanınca yer değiştirir); küçük parçalar (sınır yarıçapı &lt; 6 cm: düğme, kalem,
        /// talaş, piyon…) gölge düşürmez, tavan ampulünün nokta ışığı her düşüreni altı yüze çiziyordu. Ekip figürü gölgede kalır.
        /// </summary>
        static void MarkStatic(Transform root)
        {
            var moving = MovingParts.Select(p => root.Find(p)).Where(t => t != null).ToArray();
            var figure = root.Find("Desk/MapMount/Team figure");
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponent<TextMeshPro>() != null) continue;
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off && r.bounds.extents.magnitude < 0.06f &&
                    (figure == null || !r.transform.IsChildOf(figure)))
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (moving.Any(m => r.transform.IsChildOf(m)) || r.name.StartsWith("Pawn ") || r.name.StartsWith("Slot ")) continue;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }
    }
}
