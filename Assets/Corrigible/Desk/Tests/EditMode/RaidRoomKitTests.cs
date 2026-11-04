using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using Corrigible.Game.Raid;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Bina kitleri (BASKIN-MEKANLARI Katman 3–4, Aşama A) ve okunurluk kapısı. Motifi çizilen her kit kütüphanede ve harekât
    /// baskınında (MapSeed ≠ 0) kendi bölgesine verilir; MapSeed 0 (eğitim, örnek baskın, testler) ve kiti olmayan bölge
    /// varsayılan kiti alır. Her kitin (varsayılan dahil) yüzleri kapıdan geçer: renk = doku ortalaması × _BaseColor, CIE76 ΔE*ab
    /// (sRGB → Lab, D65); üst L* 58–74, yan L* ≥ 40, kanal ve kuyu L* ≤ 30, C* ≤ 22, ahşaba ΔE ≥ 15, ayrılmış renklere ΔE ≥ 20,
    /// duvar deseni ΔL* ≤ 15 (%5–%95), URP Lit, mat, ışımasız; duvar karosu en çok 3 renderer. On iki bölgenin hepsinin kiti var ve
    /// kapıdan geçer; Batı Avrupa kiti varsayılana en yakın kittir (eğitim ile weu_2 aynı binada geçer); su (Güneydoğu Asya kanalı)
    /// mavi değildir. Gedik molozu (Katman 5): her bölge kitinin alçak, tek renderer'lı, çarpıştırıcısız, kırmızısız kendi molozu var;
    /// varsayılan kit bugünkü kırık dolap enkazına düşer.
    /// </summary>
    public class RaidRoomKitTests
    {
        /// <summary>Her bölgenin kendi kiti olmalı (kitsiz bölge varsayılan kite düşerdi).</summary>
        static readonly bool EveryRegionNeedsAKit = true;

        static RaidPieceLibrary Lib => AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(RaidKitTextureBaker.LibraryPath);
        static string[] RegionIds => GameData.Regions.Select(r => r.Id).ToArray();

        /// <summary>Varsayılan kit ve kütüphanedeki bütün kitler.</summary>
        static IEnumerable<RaidRoomKit> AllKits()
        {
            var lib = Lib;
            yield return lib.defaultKit;
            foreach (var k in lib.kits ?? new RaidRoomKit[0]) if (k != null) yield return k;
        }

        static RaidRoomKit Linked(string id) => (Lib.kits ?? new RaidRoomKit[0]).FirstOrDefault(k => k != null && k.id == id);

        // ------------------------------------------------------------------ seçim

        [Test]
        public void ListedKits_AreLinkedAndChosenForTheirRegionInCampaignRaids()
        {
            var lib = Lib;
            var ids = RaidKitTextureBaker.KitIds;
            Assert.AreEqual(12, RegionIds.Length, "on iki bölge");
            CollectionAssert.AreEquivalent(RegionIds, ids, "her bölgenin motifi çizilir; motifi olan her kit bir bölgenin");
            CollectionAssert.AreEqual(ids, lib.kits.Select(k => k.id).ToArray(), "kütüphanede motif tablosunun sırasıyla, eksiksiz");
            foreach (var id in ids)
            {
                CollectionAssert.Contains(RegionIds, id, "kit kimliği bir bölge kimliği");
                var kit = Linked(id);
                Assert.IsNotNull(kit, id + ": kit kütüphaneye bağlı (kapıdan geçti)");
                Assert.AreEqual(RaidKitTextureBaker.KitPath(id), AssetDatabase.GetAssetPath(kit));
                Assert.IsTrue(kit.bridges, id + ": bölge kitinde komşu duvarlar köprüyle birleşir");
                foreach (uint seed in new uint[] { 1, 4284, 20261102, uint.MaxValue })
                    Assert.AreSame(kit, lib.KitFor(id, seed), $"{id}: harekât baskını (MapSeed {seed}) bölgenin kiti");
                Assert.AreSame(kit, lib.KitById(id), id + ": editörün kitOverride'ı");
            }
            Assert.AreEqual(lib.kits.Length, lib.kits.Where(k => k != null).Select(k => k.id).Distinct().Count(), "her kimlik bir kez");
        }

        [Test]
        public void MapSeedZeroAndUnknownRegions_GetTheDefaultKit()
        {
            var lib = Lib;
            foreach (var region in RegionIds)
                Assert.AreSame(lib.defaultKit, lib.KitFor(region, 0), region + ": MapSeed 0 (eğitim, örnek baskın, yakalama seti, testler)");
            foreach (var region in new[] { null, "", "yok", "default", "WEU" })
                Assert.AreSame(lib.defaultKit, lib.KitFor(region, 4284), "bilinmeyen bölge: " + (region ?? "null"));
        }

        /// <summary>Harekât baskınında (MapSeed ≠ 0) hiçbir bölge varsayılan kite düşmez.</summary>
        [Test]
        public void EveryRegion_HasItsOwnKitInCampaignRaids()
        {
            var lib = Lib;
            var missing = RegionIds.Where(r => lib.KitFor(r, 4284) == lib.defaultKit).ToList();
            var line = missing.Count == 0 ? "her bölgenin kiti var" : "kiti olmayan bölgeler (varsayılan kitle çizilir): " + string.Join(", ", missing);
            TestContext.WriteLine(line);
            Debug.Log("[Kit] " + line);
            if (EveryRegionNeedsAKit) Assert.IsEmpty(missing, line);
            // Motifi çizilen bir kit raporda kitsiz görünmemeli
            CollectionAssert.IsEmpty(missing.Intersect(RaidKitTextureBaker.KitIds), "motifi olan kit kütüphanede olmalı");
        }

        [Test]
        public void EveryKit_HasWallColumnChannelAndWellMaterials()
        {
            foreach (var kit in AllKits())
            {
                var slots = new (string name, Material m)[]
                {
                    ("wallTop", kit.wallTop), ("wallWest", kit.wallWest), ("wallSouth", kit.wallSouth), ("columnWest", kit.columnWest),
                    ("columnSouth", kit.columnSouth), ("channelFloor", kit.channelFloor), ("wellFloor", kit.wellFloor),
                };
                foreach (var (name, m) in slots) Assert.IsNotNull(m, kit.id + " " + name);
            }
            foreach (var id in RaidKitTextureBaker.KitIds)
            {
                var kit = Linked(id);
                Assert.IsNotNull(kit, id);
                foreach (var (m, sheet) in new[] { (kit.wallTop, RaidKitTextureBaker.Sheet.Top), (kit.wallWest, RaidKitTextureBaker.Sheet.Side),
                             (kit.wallSouth, RaidKitTextureBaker.Sheet.Side), (kit.columnWest, RaidKitTextureBaker.Sheet.Column),
                             (kit.columnSouth, RaidKitTextureBaker.Sheet.Column), (kit.channelFloor, RaidKitTextureBaker.Sheet.Channel),
                             (kit.wellFloor, RaidKitTextureBaker.Sheet.Well) })
                {
                    Assert.AreEqual(RaidKitTextureBaker.TexturePath(id, sheet), AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")), m.name + " dokusu");
                    StringAssert.StartsWith(RaidKitTextureBaker.OutRoot + "/" + id + "/", AssetDatabase.GetAssetPath(m), m.name + " kitin klasöründe");
                }
                // Batı yüzü dokunun kendisi; güney yüzü aynı dokunun parlaklık tonu (gri, batıdan koyu)
                Assert.AreEqual(Color.white, kit.wallWest.GetColor("_BaseColor"));
                var tint = kit.wallSouth.GetColor("_BaseColor");
                Assert.Less(tint.maxColorComponent, 1, "güney yüzü batıdan koyu");
                Assert.Less(tint.maxColorComponent - Mathf.Min(tint.r, tint.g, tint.b), 0.06f, "güney tonu yaklaşık gri");
            }
        }

        // ------------------------------------------------------------------ renk kapısı

        [Test]
        public void Lab_MatchesReferenceValues()
        {
            void Near(Vector3 expected, Color c)
            {
                var lab = RaidKitGate.Lab(c);
                Assert.AreEqual(expected.x, lab.x, 0.02f, "L*"); Assert.AreEqual(expected.y, lab.y, 0.02f, "a*"); Assert.AreEqual(expected.z, lab.z, 0.02f, "b*");
            }
            Near(new Vector3(100, 0, 0), Color.white);
            Near(Vector3.zero, Color.black);
            Near(new Vector3(53.585f, 0, 0), new Color32(128, 128, 128, 255));
            Near(new Vector3(70.498f, 6.239f, 27.938f), RaidKitGate.Hex("C9A77A"));
            Near(new Vector3(33.257f, 11.315f, -40.126f), RaidKitGate.Hex("2C4C8E"));
            Assert.AreEqual(6.501f, RaidKitGate.DeltaE(RaidKitGate.Hex("C9A77A"), RaidKitGate.Hex("C9A36B")), 0.01f, "CIE76");
            Assert.AreEqual(79.798f, RaidKitGate.DeltaE(RaidKitGate.Hex("9A1E14"), RaidKitGate.Hex("24592E")), 0.01f);
        }

        [Test]
        public void ColourGate_FaceMeansStayInTheReadableRange()
        {
            var failures = new List<string>();
            foreach (var kit in AllKits())
                foreach (var f in RaidKitGate.Faces(kit))
                {
                    string at = kit.id + " " + f.Name;
                    if (f.Material == null) { failures.Add(at + ": malzeme yok"); continue; }
                    float L = f.Lab.x;
                    if (f.Kind == RaidKitGate.FaceKind.Top && (L < 58 || L > 74)) failures.Add($"{at}: üst yüz L* {L:F1} (58–74)");
                    if (f.Kind == RaidKitGate.FaceKind.Side && L < 40) failures.Add($"{at}: yan yüz L* {L:F1} (≥ 40)");
                    if (f.Kind == RaidKitGate.FaceKind.Floor && L > 30) failures.Add($"{at}: kanal L* {L:F1} (≤ 30)");
                    if (f.Chroma > 22) failures.Add($"{at}: C* {f.Chroma:F1} (≤ 22)");
                    foreach (var (name, color) in RaidKitGate.Wood)
                        if (RaidKitGate.DeltaE(f.Mean, color) < 15) failures.Add($"{at}: {name} ΔE {RaidKitGate.DeltaE(f.Mean, color):F1} (≥ 15)");
                    foreach (var (name, color) in RaidKitGate.Reserved)
                        if (RaidKitGate.DeltaE(f.Mean, color) < 20) failures.Add($"{at}: {name} ΔE {RaidKitGate.DeltaE(f.Mean, color):F1} (≥ 20)");
                }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void ColourGate_WallPatternContrastIsLow()
        {
            var failures = new List<string>();
            foreach (var kit in AllKits())
                foreach (var f in RaidKitGate.Faces(kit).Where(f => f.Kind != RaidKitGate.FaceKind.Floor && f.Material != null))
                    if (f.Pattern > 15) failures.Add($"{kit.id} {f.Name}: desen ΔL* {f.Pattern:F1} (%5 {f.PatternLow:F1}, %95 {f.PatternHigh:F1}; ≤ 15)");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void Materials_AreMatteLitWithoutEmission()
        {
            var failures = new List<string>();
            foreach (var kit in AllKits())
                foreach (var f in RaidKitGate.Faces(kit).Where(f => f.Material != null))
                {
                    var m = f.Material;
                    string at = kit.id + " " + f.Name + " (" + m.name + ")";
                    if (m.shader.name != "Universal Render Pipeline/Lit") failures.Add(at + ": URP Lit değil");
                    float max = f.Kind == RaidKitGate.FaceKind.Floor ? 0.35f : 0.15f;
                    if (m.GetFloat("_Smoothness") > max + 1e-4f) failures.Add($"{at}: smoothness {m.GetFloat("_Smoothness")} (≤ {max})");
                    if (m.GetFloat("_Metallic") > 1e-4f) failures.Add(at + ": metalik");
                    if (m.IsKeywordEnabled("_EMISSION")) failures.Add(at + ": _EMISSION açık");
                    if (m.GetColor("_EmissionColor").maxColorComponent > 1e-3f) failures.Add(at + ": ışıma rengi");
                }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void WallTile_StaysWithinThreeRenderersWithEveryKit()
        {
            var failures = new List<string>();
            foreach (var kit in AllKits()) failures.AddRange(RaidKitGate.CheckRenderers(kit, Lib));
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary>Kapının tamamı (baker'ın kütüphaneye bağlarken kullandığı denetim) her kitte boş.</summary>
        [Test]
        public void EveryKit_PassesTheWholeGate()
        {
            var failures = AllKits().SelectMany(k => RaidKitGate.Check(k, Lib)).ToList();
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // ------------------------------------------------------------------ dokular ve tokenlar

        [Test]
        public void KitTextures_AreUpToDateAndImportedForTiling()
        {
            foreach (var id in RaidKitTextureBaker.KitIds)
                foreach (RaidKitTextureBaker.Sheet sheet in System.Enum.GetValues(typeof(RaidKitTextureBaker.Sheet)))
                {
                    string path = RaidKitTextureBaker.TexturePath(id, sheet), at = id + " " + sheet;
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    Assert.IsNotNull(tex, path);
                    var (w, h) = RaidKitTextureBaker.SheetSize(sheet);
                    Assert.AreEqual(w, tex.width, at); Assert.AreEqual(h, tex.height, at);
                    var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                    Assert.AreEqual(TextureImporterType.Default, ti.textureType, at);
                    Assert.IsTrue(ti.sRGBTexture, at + " sRGB");
                    Assert.IsTrue(ti.mipmapEnabled, at + " mipmap");
                    Assert.AreEqual(TextureWrapMode.Repeat, ti.wrapMode, at);
                    Assert.AreEqual(FilterMode.Bilinear, ti.filterMode, at);
                    Assert.AreEqual(4, ti.anisoLevel, at);
                    Assert.AreEqual(TextureImporterCompression.Uncompressed, ti.textureCompression, at);

                    // Diskteki doku baker'ın ve tokenların güncel çıktısı (tohum sabit: iki çizim aynı)
                    var fresh = RaidKitTextureBaker.Render(id, sheet);
                    var disk = RaidKitGate.Pixels(tex);
                    Assert.AreEqual(fresh.Length, disk.Length, at);
                    int worst = 0;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            Color32 a = fresh[y * w + x], b = disk[(h - 1 - y) * w + x];
                            worst = Mathf.Max(worst, Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));
                        }
                    Assert.LessOrEqual(worst, 1, at + ": doku eski; Corrigible › Baskın › Kit dokularını üret");
                }
            CollectionAssert.AreEqual(RaidKitTextureBaker.Render("weu", RaidKitTextureBaker.Sheet.Top), RaidKitTextureBaker.Render("weu", RaidKitTextureBaker.Sheet.Top), "sabit tohum");
        }

        [Test]
        public void KitTokens_AreInTheRaidThemeAndListedAsNew13()
        {
            var root = JObject.Parse(File.ReadAllText(SadeThemeSetup.RaidTokensPath));
            Assert.AreEqual("1.2", (string)root["meta"]["version"], "sürüm değişmedi; yeni gruplar _new_1_3'te");
            var listed = ((JArray)root["_new_1_3"]).Select(t => (string)t).ToList();
            foreach (var id in RaidKitTextureBaker.KitIds)
            {
                CollectionAssert.Contains(listed, "kits." + id);
                foreach (var key in RaidKitTextureBaker.TokenKeys)
                {
                    Assert.IsNotNull(root["kits"]?[id]?[key], $"raid_tokens.json › kits.{id}.{key}");
                    Assert.IsTrue(RaidTheme.Active.TryGet($"kits.{id}.{key}", out _), $"RaidTheme › kits.{id}.{key} (Tokenları içe aktar)");
                }
                // Yüzlerin ortalaması token rengine eşit (baker dokuyu eşitler)
                var kit = Linked(id);
                foreach (var (face, key) in new[] { ("wallTop", "wallTop"), ("wallWest", "wallWest"), ("wallSouth", "wallSouth"), ("columnWest", "wallWest"),
                             ("columnSouth", "wallSouth"), ("channelFloor", "channel"), ("wellFloor", "well") })
                {
                    var f = RaidKitGate.Faces(kit).First(x => x.Name == face);
                    Assert.Less(RaidKitGate.DeltaE(f.Mean, RaidTheme.Active.Get($"kits.{id}.{key}")), 1.5f, $"{id} {face} ≈ kits.{id}.{key}");
                }
            }
        }

        // ------------------------------------------------------------------ kitlerin birbirine göre yeri

        /// <summary>Duvarın üç yüzünün (üst, batı, güney) ortalama renginin varsayılan kite ortalama CIE76 uzaklığı.</summary>
        static float WallDistance(RaidRoomKit a, RaidRoomKit b)
        {
            var fa = RaidKitGate.Faces(a); var fb = RaidKitGate.Faces(b);
            return new[] { "wallTop", "wallWest", "wallSouth" }
                .Average(n => RaidKitGate.DeltaE(fa.First(f => f.Name == n).Mean, fb.First(f => f.Name == n).Mean));
        }

        /// <summary>Batı Avrupa kiti varsayılana en yakın kit (BASKIN-MEKANLARI Katman 3: eğitim, MapSeed 0 ile varsayılan kitte;
        /// weu_2 aynı binada geçiyor gibi görünmeli): aynı renk ailesi (ortalama ΔE ≤ 4) ve başka hiçbir bölge kiti ondan yakın değil.</summary>
        [Test]
        public void WesternEuropeKit_IsTheClosestToTheDefault()
        {
            var lib = Lib;
            var ranked = lib.kits.Where(k => k != null).Select(k => (k.id, d: WallDistance(k, lib.defaultKit))).OrderBy(x => x.d).ToList();
            var line = string.Join(", ", ranked.Select(x => $"{x.id} {x.d:F1}"));
            TestContext.WriteLine("varsayılana uzaklık (duvar yüzleri, ortalama ΔE): " + line);
            Assert.AreEqual("weu", ranked[0].id, line);
            Assert.LessOrEqual(ranked[0].d, 4f, line);
            Assert.Greater(ranked[1].d - ranked[0].d, 1f, "weu ile en yakın ikinci kit arasında açık fark: " + line);
        }

        /// <summary>Kanal ve kuyu tabanı hiçbir kitte mavi değil (mavi geri alınmış kare ve hareket demek): b* ≥ −3; Güneydoğu Asya'nın
        /// kanalı ve kuyusu sudur (koyu reçine su, smoothness 0,35), ötekilerin kanalı mat.</summary>
        [Test]
        public void ChannelFloors_AreNeverBlue_AndOnlySoutheastAsiaIsWater()
        {
            foreach (var kit in AllKits())
                foreach (var f in RaidKitGate.Faces(kit).Where(f => f.Kind == RaidKitGate.FaceKind.Floor))
                {
                    Assert.GreaterOrEqual(f.Lab.z, -3f, $"{kit.id} {f.Name}: b* {f.Lab.z:F1} (mavi değil)");
                    float smooth = f.Material.GetFloat("_Smoothness");
                    if (kit.id == "sea") Assert.AreEqual(0.35f, smooth, 1e-4f, "sea " + f.Name + ": su");
                    else Assert.LessOrEqual(smooth, 0.15f, kit.id + " " + f.Name + ": mat");
                }
            Assert.Less(RaidKitGate.DeltaE(RaidKitGate.Faces(Linked("sea")).First(f => f.Name == "channelFloor").Mean, RaidKitGate.Hex("343A33")), 1.5f,
                "sea kanalı koyu reçine su #343A33");
        }

        // ------------------------------------------------------------------ gedik molozu (Katman 5)

        const string RubbleDir = "Assets/Corrigible/Prefabs/Raid/Kits";

        /// <summary>Varsayılan kitin molozu yok: tahta bugünkü gibi kırık dolap enkazını basık ölçekle (0,9 / 0,3 / 0,9) koyar.</summary>
        [Test]
        public void DefaultKit_FallsBackToTheFlattenedCabinetRubble()
        {
            var lib = Lib;
            Assert.IsNull(lib.defaultKit.wallRubble, "varsayılan kit (eğitim, örnek baskın): bugünkü görünüm");
            Assert.AreSame(lib.rubble, lib.BreachRubble(lib.defaultKit, out var scale));
            Assert.AreEqual(new Vector3(0.9f, 0.3f, 0.9f), scale);
            Assert.AreSame(lib.rubble, lib.BreachRubble(null, out scale), "kitsiz tahta da yedeğe düşer");
            Assert.AreEqual(new Vector3(0.9f, 0.3f, 0.9f), scale);
        }

        /// <summary>
        /// Her bölge kitinin kendi gedik molozu: Prefabs/Raid/Kits/WallRubble_&lt;id&gt;.prefab, tahta ölçek 1 ile koyar; tek renderer
        /// (birleşik mesh, alt mesh başına bir malzeme), çarpıştırıcı ve RaidPiece yok (süs; geri alınınca silinir); alçak (≤ 0,25) ve
        /// her yaw'da karenin içinde (yatay yarıçap ≤ 0,45); malzemeleri kitin duvar malzemeleri artı tek vurgu; hiçbiri kırmızı değil,
        /// hepsi mat URP Lit, ışımasız; vurgu ahşaba ve ayrılmış renklere uzak (piyon, sivil, iyi, tehlike, eğitim sanılmaz).
        /// </summary>
        [Test]
        public void EveryRegionKit_HasItsOwnLowRedlessWallRubble()
        {
            var lib = Lib;
            var failures = new List<string>();
            var meshes = new HashSet<Mesh>();
            foreach (var id in RaidKitTextureBaker.KitIds)
            {
                var kit = Linked(id);
                Assert.IsNotNull(kit, id);
                var prefab = kit.wallRubble;
                Assert.IsNotNull(prefab, id + ": wallRubble atanmış");
                Assert.AreEqual(RubbleDir + "/WallRubble_" + id + ".prefab", AssetDatabase.GetAssetPath(prefab), id);
                Assert.AreSame(prefab, lib.BreachRubble(kit, out var scale), id + ": tahtanın gedik dalı kitin molozunu kullanır");
                Assert.AreEqual(Vector3.one, scale, id + ": kendi ölçüsünde");

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Assert.AreEqual(1, renderers.Length, id + ": tek renderer");
                CollectionAssert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), id + ": çarpıştırıcı yok (tıklama karoya düşer)");
                Assert.IsNull(prefab.GetComponentInChildren<RaidPiece>(true), id + ": RaidPiece yok (süs)");
                var r = renderers[0];
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.On, r.shadowCastingMode, id + ": gölge atar (zemine oturur)");
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                Assert.IsNotNull(mesh, id);
                Assert.IsTrue(meshes.Add(mesh), id + ": kendi meshi");

                // Boyut: prefab uzayında (kök başlangıçta, ölçek 1); tahta yalnız yaw verir
                var m = r.transform.localToWorldMatrix;
                float maxY = float.MinValue, minY = float.MaxValue, maxR = 0;
                foreach (var v in mesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v);
                    maxY = Mathf.Max(maxY, p.y); minY = Mathf.Min(minY, p.y); maxR = Mathf.Max(maxR, new Vector2(p.x, p.z).magnitude);
                }
                if (maxY > 0.25f) failures.Add($"{id}: yükseklik {maxY:F3} (≤ 0,25)");
                if (minY < -0.005f) failures.Add($"{id}: zeminin altında {minY:F3}");
                if (maxR > 0.45f) failures.Add($"{id}: yatay yarıçap {maxR:F3} (≤ 0,45: her yaw'da karenin içinde)");

                // Malzemeler: alt mesh başına bir; kitin duvar malzemeleri ve tek vurgu
                var mats = r.sharedMaterials;
                Assert.AreEqual(mesh.subMeshCount, mats.Length, id + ": alt mesh başına malzeme");
                for (int s = 0; s < mesh.subMeshCount; s++)
                    if (mesh.GetSubMesh(s).indexCount == 0) failures.Add($"{id}: alt mesh {s} boş");
                CollectionAssert.AllItemsAreNotNull(mats, id);
                CollectionAssert.IsSubsetOf(new[] { kit.wallWest, kit.wallSouth, kit.wallTop }, mats, id + ": kitin duvar malzemeleri");
                var accents = mats.Except(new[] { kit.wallWest, kit.wallSouth, kit.wallTop, kit.columnWest, kit.columnSouth }).Distinct().ToList();
                Assert.AreEqual(1, accents.Count, id + ": tek vurgu malzemesi: " + string.Join(", ", accents.Select(a => a.name)));
                StringAssert.StartsWith(RaidKitTextureBaker.OutRoot + "/" + id + "/", AssetDatabase.GetAssetPath(accents[0]), id + ": vurgu kitin klasöründe");
                foreach (var mat in mats.Distinct())
                {
                    string at = id + " " + mat.name;
                    Color.RGBToHSV(mat.GetColor("_BaseColor"), out float h, out float sat, out _);
                    if ((h < 0.04f || h > 0.96f) && sat > 0.5f) failures.Add(at + ": kırmızı (yalnız YZ)");
                    if (mat.shader.name != "Universal Render Pipeline/Lit") failures.Add(at + ": URP Lit değil");
                    if (mat.GetFloat("_Smoothness") > 0.15f + 1e-4f) failures.Add(at + ": parlak");
                    if (mat.GetFloat("_Metallic") > 1e-4f) failures.Add(at + ": metalik");
                    if (mat.IsKeywordEnabled("_EMISSION") || mat.GetColor("_EmissionColor").maxColorComponent > 1e-3f) failures.Add(at + ": ışıma");
                }
                var accent = accents[0];
                Assert.IsNull(accent.GetTexture("_BaseMap"), id + ": vurgu düz renk");
                var c = accent.GetColor("_BaseColor");
                foreach (var (name, color) in RaidKitGate.Wood)
                    if (RaidKitGate.DeltaE(c, color) < RaidKitGate.WoodDeltaEMin) failures.Add($"{id} vurgu: {name} ΔE {RaidKitGate.DeltaE(c, color):F1} (≥ 15)");
                foreach (var (name, color) in RaidKitGate.Reserved)
                    if (RaidKitGate.DeltaE(c, color) < RaidKitGate.ReservedDeltaEMin) failures.Add($"{id} vurgu: {name} ΔE {RaidKitGate.DeltaE(c, color):F1} (≥ 20)");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // ------------------------------------------------------------------ adlar (Katman 6)

        /// <summary>
        /// Kitlerin adları (RaidKitText): kütüphanedeki her kitin duvar, kanal ve moloz adı var, adlar kitler arasında tek; varsayılan
        /// kitin adı yok. Tahtasız veri kaynağının kit seçimi (RaidKitText.KitFor) kütüphanenin seçimiyle (KitFor, kitOverride → KitById)
        /// her bölgede ve tohumda aynı: kartın alt satırı tahtanın çizdiği kitin adını söyler.
        /// </summary>
        [Test]
        public void KitNames_CoverEveryKit_AndFollowTheLibrarysChoice()
        {
            var lib = Lib;
            CollectionAssert.AreEquivalent(lib.kits.Where(k => k != null).Select(k => k.id), RaidKitText.KitIds, "her kitin adı var; adı olan her kit kütüphanede");
            Assert.AreEqual(RaidKitText.Default, lib.defaultKit.id);
            Assert.IsFalse(RaidKitText.Has(RaidKitText.Default), "varsayılan kitin adı yok (ek satır çıkmaz)");
            Assert.IsNull(RaidKitText.Cable(RaidKitText.Default));
            foreach (var id in RaidKitText.KitIds)
                foreach (var name in new[] { RaidKitText.Wall(id), RaidKitText.Cable(id), RaidKitText.Rubble(id) })
                    Assert.IsFalse(string.IsNullOrEmpty(name), id);
            foreach (var pick in new System.Func<string, string>[] { RaidKitText.Wall, RaidKitText.Cable, RaidKitText.Rubble })
                Assert.AreEqual(RaidKitText.KitIds.Count(), RaidKitText.KitIds.Select(pick).Distinct().Count(), "adlar kitler arasında tek");
            foreach (var region in RegionIds.Concat(new[] { null, "", "yok", "default", "WEU" }))
                foreach (uint seed in new uint[] { 0, 1, 4284, uint.MaxValue })
                    Assert.AreEqual(lib.KitFor(region, seed).id, RaidKitText.KitFor(region, seed), $"{region ?? "null"} · MapSeed {seed}");
            foreach (var id in RegionIds.Concat(new[] { "default", "yok", "" }))
                Assert.AreEqual(lib.KitById(id).id, RaidKitText.KitFor(id, 0, true), "kitOverride " + id);
        }

        [Test]
        public void Baker_IsDeterministicWithoutUnityRandom()
        {
            foreach (var file in Directory.GetFiles("Assets/Corrigible/Desk/Scripts/Editor/Raid", "RaidKitTextureBaker*.cs"))
            {
                var text = File.ReadAllText(file);
                StringAssert.DoesNotContain("Random.", text, file + ": UnityEngine.Random yok, sabit tohum");
                StringAssert.DoesNotContain("System.Random", text, file);
            }
        }
    }
}
