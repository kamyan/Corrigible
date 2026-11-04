using System.Collections.Generic;
using System.Linq;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Duvar ve kanal karosunun giydirmesi (BASKIN-MEKANLARI Katman 3–4), bina kitinin seçimi. Duvar: karo başına en çok 3 etkin
    /// renderer (gövde + iki köprü), raf duvarı gövde + el çarkı; varsayılan kit bugünkü bloğu çizer (köprüsüz, aynı malzeme).
    /// Kanal: her maskede kolların iki dış kenarında tehlike şeridi, kolu olmayan kenarda şerit, en az bir görünür bakır; her şey
    /// zemin hizasında (≤ 0,05). Kit: MapSeed 0 her zaman varsayılan kit.
    /// </summary>
    public class RaidTileSkinTests
    {
        const string Prefabs = "Assets/Corrigible/Prefabs/Raid/";
        const string Mats = "Assets/Corrigible/Art/Raid/Analyst/Materials/";
        static RaidPieceLibrary Lib => AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>("Assets/Corrigible/Settings/RaidPieceLibrary.asset");
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Mats + name + ".mat");
        static GameObject Make(string name) => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab"));
        static string[] Active(GameObject go) => go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.name).OrderBy(n => n).ToArray();

        /// <summary>Kütüphanedeki bütün kitler (varsayılan dahil).</summary>
        static IEnumerable<RaidRoomKit> Kits()
        {
            var lib = Lib;
            yield return lib.defaultKit;
            if (lib.kits != null) foreach (var k in lib.kits) if (k != null) yield return k;
        }

        /// <summary>Köprüleri açık kit (varsayılanın kopyası): bütçe en kalabalık hâlde denenir.</summary>
        static RaidRoomKit BridgeKit()
        {
            var k = Object.Instantiate(Lib.defaultKit);
            k.id = "test"; k.bridges = true;
            return k;
        }

        // ------------------------------------------------------------------ duvar
        [Test]
        public void Wall_EveryMaskStaysWithinTheRendererBudget()
        {
            var bridged = BridgeKit();
            var go = Make("Wall");
            try
            {
                var skin = go.GetComponent<RaidTileSkin>();
                Assert.IsNotNull(skin, "Wall.prefab'ın kökünde RaidTileSkin");
                Assert.AreEqual(RaidTileSkin.TileShape.Wall, skin.Shape);
                foreach (var kit in Kits().Append(bridged))
                    for (int mask = 0; mask < 16; mask++)
                        foreach (bool isolated in new[] { false, true })
                            foreach (int axis in new[] { RaidTileMask.AxisNone, RaidTileMask.AxisNS, RaidTileMask.AxisEW })
                            {
                                skin.Apply(mask, isolated, axis, kit);
                                var on = Active(go);
                                string at = $"{kit.id} maske {mask} ayrık {isolated} raf {axis}";
                                Assert.LessOrEqual(on.Length, 3, at + ": karo başına en çok 3 renderer");
                                Assert.Contains("Body", on, at);
                                if (axis != RaidTileMask.AxisNone)
                                {
                                    CollectionAssert.AreEqual(new[] { "Body", "ShelfGear" }, on, at + ": raf duvarı gövde + el çarkı");
                                    continue;
                                }
                                Assert.IsFalse(on.Contains("ShelfGear"), at + ": el çarkı yalnız kayar rafta");
                                Assert.AreEqual(kit.bridges && (mask & RaidTileMask.E) != 0, on.Contains("BridgeE"), at + ": doğu köprüsü");
                                Assert.AreEqual(kit.bridges && (mask & RaidTileMask.N) != 0, on.Contains("BridgeN"), at + ": kuzey köprüsü");
                            }
                // Canlandırma sırasında köprüler gizlenir
                skin.Apply(RaidTileMask.All, false, RaidTileMask.AxisNone, bridged);
                skin.HideBridges();
                CollectionAssert.AreEqual(new[] { "Body" }, Active(go));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(bridged); }
        }

        [Test]
        public void Wall_DefaultKitDrawsTodaysBlock()
        {
            var kit = Lib.defaultKit;
            Assert.IsFalse(kit.bridges, "varsayılan kit köprüsüz: eğitim ve örnek baskın bugünkü görünümle");
            var go = Make("Wall");
            try
            {
                var skin = go.GetComponent<RaidTileSkin>();
                var box = go.GetComponent<BoxCollider>();
                Assert.AreEqual(new Vector3(1, 0.63f, 1), box.size, "çarpıştırıcı değişmedi");
                Assert.AreEqual(0.235f, box.center.y, 1e-4f);
                Assert.AreEqual(1, go.GetComponentsInChildren<Collider>(true).Length, "kit çocuklarında çarpıştırıcı yok");
                var mesh = skin.Body.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(3, mesh.subMeshCount, "WallBlock: ±X, ±Z, üst/alt/basamak");
                Assert.AreEqual(new Vector3(-0.47f, 0.003f, -0.47f).ToString("F3"), mesh.bounds.min.ToString("F3"), "alt kat 0,94");
                Assert.AreEqual(new Vector3(0.47f, 0.547f, 0.47f).ToString("F3"), mesh.bounds.max.ToString("F3"), "tepe 0,547");
                // İki katman: alt 0,94 (y 0,003–0,272), üst 0,92 (y 0,278–0,547)
                var ys = mesh.vertices.Select(v => System.Math.Round(v.y, 3)).Distinct().OrderBy(y => y).ToArray();
                CollectionAssert.AreEqual(new[] { 0.003, 0.272, 0.278, 0.547 }, ys);
                Assert.IsTrue(mesh.vertices.Where(v => v.y > 0.275f).All(v => Mathf.Abs(Mathf.Abs(v.x) - 0.46f) < 1e-4f && Mathf.Abs(Mathf.Abs(v.z) - 0.46f) < 1e-4f), "üst kat 0,92");
                Assert.AreEqual(Vector3.zero, skin.Body.transform.localPosition);
                Assert.AreEqual(Vector3.one, skin.Body.transform.localScale);
                foreach (bool isolated in new[] { false, true })
                {
                    skin.Apply(0, isolated, RaidTileMask.AxisNone, kit);
                    CollectionAssert.AreEqual(new[] { Mat("WallWest"), Mat("WallSouth"), Mat("WallTop") }, skin.Body.sharedMaterials, "bugünkü renkler");
                }
                Assert.IsNull(kit.wallRubble, "varsayılan kitin molozu bugünkü kırık dolap enkazı");
                Assert.AreEqual(Mat("Trench"), kit.channelFloor);
                Assert.AreEqual(Mat("Trench"), kit.wellFloor);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Wall_IsolatedBlockTakesTheKitColumnAndBridgesTheWallFaces()
        {
            var kit = BridgeKit();
            kit.columnWest = Mat("TrenchWall"); kit.columnSouth = Mat("Trench");
            var go = Make("Wall");
            try
            {
                var skin = go.GetComponent<RaidTileSkin>();
                skin.Apply(0, true, RaidTileMask.AxisNone, kit);
                CollectionAssert.AreEqual(new[] { kit.columnWest, kit.columnSouth, kit.wallTop }, skin.Body.sharedMaterials, "ayrık blok kitin dik istifi");
                skin.Apply(0, true, RaidTileMask.AxisNS, kit);
                CollectionAssert.AreEqual(new[] { kit.wallWest, kit.wallSouth, kit.wallTop }, skin.Body.sharedMaterials, "raf her zaman arşiv rafı");
                skin.Apply(RaidTileMask.E | RaidTileMask.N, false, RaidTileMask.AxisNone, kit);
                CollectionAssert.AreEqual(new[] { kit.wallWest, kit.wallSouth, kit.wallTop }, skin.Body.sharedMaterials);
                foreach (var bit in new[] { RaidTileMask.E, RaidTileMask.N })
                {
                    var bridge = skin.Bridge(bit);
                    CollectionAssert.AreEqual(new[] { kit.wallWest, kit.wallSouth, kit.wallWest, kit.wallSouth, kit.wallTop, kit.wallSouth }, bridge.sharedMaterials);
                    // Boşluğu doldurur (0,44–0,56), blok tepesinin (0,547) altında kalır; komşu karonun içine 0,06'dan fazla girmez
                    var b = bridge.bounds;
                    float along = bit == RaidTileMask.E ? b.center.x : b.center.z, across = bit == RaidTileMask.E ? b.size.z : b.size.x;
                    Assert.AreEqual(0.5f, along, 1e-3f, "iki bloğun arasında");
                    Assert.AreEqual(0.6f, across, 1e-3f, "genişlik 0,6");
                    Assert.LessOrEqual(b.max.y, 0.5f + 1e-4f, "blok tepesinin altında");
                    Assert.AreEqual(0.12f, bit == RaidTileMask.E ? b.size.x : b.size.z, 1e-3f);
                }
                // Kit malzemesi boşsa varsayılan kite düşer
                var bare = ScriptableObject.CreateInstance<RaidRoomKit>();
                try
                {
                    skin.Apply(0, false, RaidTileMask.AxisNone, bare);
                    CollectionAssert.AreEqual(new[] { Mat("WallWest"), Mat("WallSouth"), Mat("WallTop") }, skin.Body.sharedMaterials);
                }
                finally { Object.DestroyImmediate(bare); }
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(kit); }
        }

        [Test]
        public void Wall_ShelfGearSitsOnTheFaceAlongTheRail()
        {
            var go = Make("Wall");
            try
            {
                var skin = go.GetComponent<RaidTileSkin>();
                skin.Apply(0, false, RaidTileMask.AxisNS, Lib.defaultKit);
                var b = skin.ShelfGear.bounds;
                Assert.Less(b.max.x, -0.45f, "kuzey–güney ray: el çarkı batı yüzünde");
                Assert.AreEqual(0, b.center.z, 0.02f);
                Assert.GreaterOrEqual(b.min.x, -0.51f, "komşu karonun bloğuna değmez");
                skin.Apply(0, false, RaidTileMask.AxisEW, Lib.defaultKit);
                b = skin.ShelfGear.bounds;
                Assert.Less(b.max.z, -0.45f, "doğu–batı ray: el çarkı güney yüzünde");
                Assert.AreEqual(0, b.center.x, 0.02f);
                Assert.LessOrEqual(b.max.y, 0.547f, "blok yüzünün içinde");
                // Çark ve tekerlek çelik ya da koyu: kırmızı (YZ) ve mavi (geri alınan) değil
                foreach (var m in skin.ShelfGear.sharedMaterials)
                {
                    Color.RGBToHSV(m.GetColor("_BaseColor"), out _, out float s, out _);
                    Assert.Less(s, 0.2f, m.name + " doygun değil");
                    Assert.LessOrEqual(m.GetFloat("_Smoothness"), 0.15f, m.name + " mat");
                }
                Assert.AreEqual(1, skin.ShelfGear.GetComponents<MeshRenderer>().Length);
                Assert.AreEqual(0, skin.ShelfGear.transform.childCount, "tek renderer, birleşik mesh");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ------------------------------------------------------------------ kanal
        static readonly Vector3[] Along = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        static readonly Vector3[] Across = { Vector3.right, Vector3.forward, Vector3.right, Vector3.forward };

        /// <summary>Etkin renderer'ların bu malzemeyle çizilen üçgenleri (karo uzayında).</summary>
        static List<Vector3[]> Triangles(GameObject root, Material material)
        {
            var tris = new List<Vector3[]>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                var mats = r.sharedMaterials;
                var v = mesh.vertices;
                for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
                {
                    if (mats[s] != material) continue;
                    var idx = mesh.GetTriangles(s);
                    for (int i = 0; i < idx.Length; i += 3)
                        tris.Add(new[] { idx[i], idx[i + 1], idx[i + 2] }.Select(j => root.transform.InverseTransformPoint(r.transform.TransformPoint(v[j]))).ToArray());
                }
            }
            return tris;
        }

        /// <summary>Nokta (x, z) bir üçgenin yatay izdüşümünün içinde mi.</summary>
        static bool Covered(List<Vector3[]> tris, Vector3 p)
        {
            foreach (var t in tris)
            {
                Vector2 a = new Vector2(t[0].x, t[0].z), b = new Vector2(t[1].x, t[1].z), c = new Vector2(t[2].x, t[2].z), q = new Vector2(p.x, p.z);
                float d = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
                if (Mathf.Abs(d) < 1e-8f) continue;
                float l1 = ((b.y - c.y) * (q.x - c.x) + (c.x - b.x) * (q.y - c.y)) / d;
                float l2 = ((c.y - a.y) * (q.x - c.x) + (a.x - c.x) * (q.y - c.y)) / d;
                if (l1 >= -1e-4f && l2 >= -1e-4f && 1 - l1 - l2 >= -1e-4f) return true;
            }
            return false;
        }

        [Test]
        public void Cable_EveryMaskKeepsTheHazardStripeAndCopper()
        {
            var hazard = Mat("Hazard"); var copper = Mat("Copper");
            var go = Make("Cable");
            try
            {
                var skin = go.GetComponent<RaidTileSkin>();
                Assert.IsNotNull(skin, "Cable.prefab'ın kökünde RaidTileSkin");
                Assert.AreEqual(RaidTileSkin.TileShape.Cable, skin.Shape);
                foreach (var kit in Kits())
                    for (int mask = 0; mask < 16; mask++)
                    {
                        skin.Apply(mask, false, RaidTileMask.AxisNone, kit);
                        string at = $"{kit.id} maske {mask}";
                        int arms = 0;
                        for (int side = 0; side < 4; side++) if ((mask & RaidTileMask.Sides[side].bit) != 0) arms++;
                        bool straight = mask == (RaidTileMask.N | RaidTileMask.S) || mask == (RaidTileMask.E | RaidTileMask.W);
                        Assert.IsTrue(skin.Hub.gameObject.activeSelf, at + ": göbek");
                        Assert.AreEqual(arms > 0 && !straight, skin.Junction.activeInHierarchy, at + ": bağlantı plakası yalnız kesişimde ve uçta");
                        Assert.AreEqual(arms == 0, skin.Single.activeSelf, at + ": kuyu yalnız tek karede");
                        Assert.AreEqual(arms == 0 ? kit.wellFloor : kit.channelFloor, skin.Hub.sharedMaterials[0], at + ": göbeğin tabanı");
                        var stripes = Triangles(go, hazard);
                        for (int side = 0; side < 4; side++)
                        {
                            bool arm = (mask & RaidTileMask.Sides[side].bit) != 0;
                            Assert.AreEqual(arm, skin.Arm(side).gameObject.activeSelf, at + " kol " + side);
                            Assert.AreEqual(!arm, skin.Edge(side).activeSelf, at + " kenar " + side);
                            if (arm)
                            {
                                Assert.AreEqual(kit.channelFloor, skin.Arm(side).sharedMaterials[0], at + ": kolun tabanı");
                                // Kolun iki dış kenarında, göbekten karonun kenarına kadar şerit (±0,36)
                                foreach (float lateral in new[] { -0.36f, 0.36f })
                                    foreach (float t in new[] { 0.36f, 0.4f, 0.45f, 0.499f })
                                        Assert.IsTrue(Covered(stripes, Along[side] * t + Across[side] * lateral), $"{at}: kol {side} şeridi ({t}, {lateral})");
                            }
                            else
                                // Kolu olmayan kenarda şerit boydan boya (köşeleri kapatır)
                                foreach (float lateral in new[] { -0.36f, -0.2f, 0f, 0.2f, 0.36f })
                                    Assert.IsTrue(Covered(stripes, Along[side] * 0.36f + Across[side] * lateral), $"{at}: kenar {side} şeridi ({lateral})");
                        }
                        Assert.IsFalse(Covered(stripes, Vector3.zero), at + ": göbeğin ortasında şerit yok");
                        Assert.IsNotEmpty(Triangles(go, copper), at + ": görünür bakır");
                    }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Cable_StraightRunsCarryTheCopperAcrossTheTile()
        {
            var copper = Mat("Copper");
            var go = Make("Cable");
            try
            {
                var skin = go.GetComponent<RaidTileSkin>();
                foreach (var (mask, axis) in new[] { (RaidTileMask.N | RaidTileMask.S, Vector3.forward), (RaidTileMask.E | RaidTileMask.W, Vector3.right) })
                {
                    skin.Apply(mask, false, RaidTileMask.AxisNone, Lib.defaultKit);
                    Assert.IsFalse(skin.Junction.activeInHierarchy, "düz geçişte bağlantı plakası yok");
                    var tris = Triangles(go, copper);
                    var side = Vector3.Cross(Vector3.up, axis);
                    // Üç bakır kesintisiz: karonun bir kenarından öbürüne
                    foreach (float offset in new[] { -0.12f, 0f, 0.12f })
                        foreach (float t in new[] { -0.49f, -0.25f, 0f, 0.25f, 0.49f })
                            Assert.IsTrue(Covered(tris, axis * t + side * offset), $"maske {mask}: bakır ({offset}, {t})");
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Cable_EverythingStaysAtFloorHeightWithoutColliders()
        {
            var go = Make("Cable");
            try
            {
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.SetActive(true);
                var renderers = go.GetComponentsInChildren<Renderer>();
                Assert.GreaterOrEqual(renderers.Length, 11, "göbek, bağlantı, 4 kenar, 4 kol, kuyu");
                foreach (var r in renderers)
                {
                    Assert.LessOrEqual(r.bounds.max.y, 0.05f, r.name + ": kanalın üstünden yürünür (zemin hizası)");
                    Assert.LessOrEqual(Mathf.Max(Mathf.Abs(r.bounds.min.x), Mathf.Abs(r.bounds.max.x), Mathf.Abs(r.bounds.min.z), Mathf.Abs(r.bounds.max.z)), 0.5f + 1e-4f, r.name + ": karonun içinde");
                    Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, r.shadowCastingMode, r.name + ": 6 cm'den alçak parça gölge atmaz");
                }
                Assert.AreEqual(1, go.GetComponentsInChildren<Collider>(true).Length, "yalnız kökün çarpıştırıcısı");
                var box = go.GetComponent<BoxCollider>();
                Assert.AreEqual(0, box.center.y + box.size.y / 2, 1e-4f, "çarpıştırıcının tepesi zeminde");
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ------------------------------------------------------------------ kit seçimi
        [Test]
        public void Kits_MapSeedZeroAndUnknownRegionsGetTheDefaultKit()
        {
            var lib = Lib;
            Assert.IsNotNull(lib.defaultKit, "varsayılan kit bağlı");
            Assert.AreEqual("default", lib.defaultKit.id);
            Assert.AreEqual("Assets/Corrigible/Settings/Kits/Kit_default.asset", AssetDatabase.GetAssetPath(lib.defaultKit));
            foreach (var region in new[] { "weu", "eeu", "me", "af", "na", "ru", "in", "cn", "jk", "sea", "sa", "oc" })
                Assert.AreSame(lib.defaultKit, lib.KitFor(region, 0), region + ": MapSeed 0 (eğitim, örnek baskın, testler) varsayılan kit");
            Assert.AreSame(lib.defaultKit, lib.KitFor(null, 1234));
            Assert.AreSame(lib.defaultKit, lib.KitFor("yok", 1234), "kiti olmayan bölge");
            Assert.AreSame(lib.defaultKit, lib.KitById("default"));
            Assert.AreSame(lib.defaultKit, lib.KitById(null));
            Assert.AreSame(lib.defaultKit, lib.KitById("yok"));

            // Bölge kiti yalnız harekât baskınında (MapSeed ≠ 0); kitOverride kimliği MapSeed 0'da da zorlar
            var test = ScriptableObject.CreateInstance<RaidPieceLibrary>();
            var eeu = ScriptableObject.CreateInstance<RaidRoomKit>();
            var shared = ScriptableObject.CreateInstance<RaidRoomKit>();
            try
            {
                eeu.id = "eeu"; shared.id = "beds"; shared.regions = new[] { "sea", "sa" };
                test.defaultKit = lib.defaultKit;
                test.kits = new[] { null, eeu, shared };
                Assert.AreSame(lib.defaultKit, test.KitFor("eeu", 0));
                Assert.AreSame(eeu, test.KitFor("eeu", 7));
                Assert.AreSame(shared, test.KitFor("sa", 7), "bölge listesi");
                Assert.AreSame(lib.defaultKit, test.KitFor("weu", 7));
                Assert.AreSame(eeu, test.KitById("eeu"));

                var board = new GameObject("Tahta testi").AddComponent<RaidBoard3D>();
                try
                {
                    var so = new SerializedObject(board);
                    so.FindProperty("lib").objectReferenceValue = test;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Assert.AreSame(lib.defaultKit, board.Kit, "SetDress'ten önce varsayılan kit");
                    board.SetDress("eeu", 7);
                    Assert.AreSame(eeu, board.Kit);
                    board.SetDress("eeu", 0);
                    Assert.AreSame(lib.defaultKit, board.Kit, "eğitim ve örnek baskın");
                    board.SetDress("eeu", 0, true);
                    Assert.AreSame(eeu, board.Kit, "editörün kitOverride'ı");
                }
                finally { Object.DestroyImmediate(board.gameObject); }
            }
            finally { Object.DestroyImmediate(test); Object.DestroyImmediate(eeu); Object.DestroyImmediate(shared); }
        }

        [Test]
        public void Kits_UseMattePipelineMaterialsWithoutEmission()
        {
            foreach (var kit in Kits())
                foreach (var m in new[] { kit.wallTop, kit.wallWest, kit.wallSouth, kit.columnWest, kit.columnSouth, kit.channelFloor, kit.wellFloor })
                {
                    Assert.IsNotNull(m, kit.id + ": kitin bütün malzemeleri atanmış");
                    Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, m.name);
                    Assert.LessOrEqual(m.GetFloat("_Smoothness"), 0.35f, m.name + " mat (su en çok 0,35)");
                    Assert.IsFalse(m.IsKeywordEnabled("_EMISSION"), m.name + " ışımaz");
                }
        }
    }
}
