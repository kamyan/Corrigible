using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using Corrigible.Game.Raid;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Bölge olaylarının nesneleri (BASKIN-MEKANLARI Katman 5): basınçlı tüp, kaynak kolu kaidesi ve tavan enkazı kendi
    /// parçalarıyla gelir (yedek parça ölçeklenmez), kırmızısızdır, çarpıştırıcıları görünen gövdeyi sarar; inceleme kartı her
    /// nesnenin kendi küçük resmini gösterir.
    /// </summary>
    public sealed class EventObjectModelTests
    {
        const string LibraryPath = "Assets/Corrigible/Settings/RaidPieceLibrary.asset";
        static readonly string[] EventKinds = { "tup", "kol", "enkaz" };

        static RaidPieceLibrary Lib => AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);

        /// <summary>Görünen gövdenin renderer'ları (etkin, TMP hariç).</summary>
        static Renderer[] Visible(Component piece) =>
            piece.GetComponentsInChildren<Renderer>().Where(r => r.GetComponent<TMP_Text>() == null && r.GetComponent<TMP_SubMesh>() == null).ToArray();

        static Bounds BoundsOf(IEnumerable<Renderer> rs) => rs.Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });

        /// <summary>Parçayı sahneye kurar (kök orijinde, zemin y = 0), işi yapar, siler.</summary>
        static void With(RaidPiece prefab, System.Action<RaidPiece> check)
        {
            var piece = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            try { check(piece); }
            finally { Object.DestroyImmediate(piece.gameObject); }
        }

        /// <summary>Parçanın URP Lit malzemeleri (kapalı çocuklar dahil; etiket yazısının TMP malzemesi hariç).</summary>
        static IEnumerable<Material> Materials(GameObject go) =>
            go.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials)
                .Where(m => m != null && m.shader != null && m.shader.name == "Universal Render Pipeline/Lit").Distinct();

        static bool IsRed(Color c) { Color.RGBToHSV(c, out float h, out float s, out _); return (h < 0.04f || h > 0.96f) && s > 0.5f; }
        static bool IsOrange(Color c) { Color.RGBToHSV(c, out float h, out float s, out _); return h >= 0.04f && h < 0.1f && s > 0.5f; }
        static bool IsYellow(Color c) { Color.RGBToHSV(c, out float h, out float s, out _); return h >= 0.1f && h < 0.2f && s > 0.5f; }

        [Test]
        public void EventObjects_HaveTheirOwnRedlessPieces()
        {
            var lib = Lib;
            Assert.IsNotNull(lib.tube); Assert.IsNotNull(lib.weldArm); Assert.IsNotNull(lib.debris);
            Assert.AreSame(lib.tube, lib.Object("tup"));
            Assert.AreSame(lib.weldArm, lib.Object("kol"));
            Assert.AreSame(lib.debris, lib.Object("enkaz"));
            var shared = new[] { lib.core, lib.node, lib.civilian, lib.cooling, lib.spawner, lib.lootCrate };
            foreach (var kind in EventKinds)
            {
                var piece = lib.Object(kind);
                Assert.IsTrue(lib.HasOwnPiece(kind), kind + " kendi parçası");
                Assert.AreEqual(Vector3.one, lib.FallbackScale(kind), kind + " yedek ölçek uygulanmaz");
                CollectionAssert.DoesNotContain(shared, piece, kind + " başka nesnenin parçası değil");
                // Kök dönüşsüz ve ölçeksiz (tüpün uçuşu kökü yatırır; tahta kökü kare merkezine kurar)
                Assert.AreEqual(Vector3.one, piece.transform.localScale, kind);
                Assert.AreEqual(Quaternion.identity, piece.transform.localRotation, kind);
                var so = new SerializedObject(piece);
                var body = so.FindProperty("body").objectReferenceValue as Transform;
                Assert.IsNotNull(body, kind + " gövdesi");
                Assert.IsNull(so.FindProperty("hp").objectReferenceValue, kind + " can yazısı ortak ekran havuzunda");
                Assert.IsNotNull(piece.GetComponentInChildren<Rev3Model>(),kind+" kaynak modeli");
                // Parlama (vuruş, ıslık, kol) ana kütleye işler: görünen her renderer boyanır
                var tinted = Enumerable.Range(0, so.FindProperty("tinted").arraySize)
                    .Select(i => so.FindProperty("tinted").GetArrayElementAtIndex(i).objectReferenceValue as Renderer).ToList();
                Assert.IsFalse(tinted.Contains(null), kind + " boyananlarda boş yok");
                foreach (var r in body.GetComponentsInChildren<Renderer>().Where(r => r.GetComponent<TMP_Text>() == null))
                    CollectionAssert.Contains(tinted, r, kind + "/" + r.name + " parlamaya katılır");
                // Kırmızı yalnız YZ; ışıma yok. Yüzey değerleri kaynak modelin paletinden gelir (Rev3ModelTests).
                var mats = Materials(piece.gameObject).ToList();
                Assert.IsNotEmpty(mats, kind);
                foreach (var m in mats)
                {
                    Assert.IsFalse(IsRed(m.GetColor("_BaseColor")), kind + " kırmızı: " + m.name);
                    var e = m.GetColor("_EmissionColor");
                    Assert.IsTrue(e.r + e.g + e.b < 1e-4f && !m.IsKeywordEnabled("_EMISSION"), kind + " ışıma: " + m.name);
                }
            }
        }

        /// <summary>
        /// Basınçlı tüp: Ø0,2 × 0,75 ince silindir, uzun ekseni yukarı; sıcak çemberin (r 0,36) içinde. Soğutma ünitesine benzemez:
        /// fan, ızgara ve sarı uyarı üçgeni yok, malzemesi ortak değil; turuncu omuz bandı var.
        /// </summary>
        [Test]
        public void Tube_IsASlimCylinderNotACoolingUnit()
        {
            var lib = Lib;
            With(lib.tube, tube =>
            {
                var b = BoundsOf(Visible(tube));
                Assert.AreEqual(0.75f, tube.VisualHeight, 0.02f, "boy 0,75");
                Assert.AreEqual(0f, b.min.y, 0.01f, "zeminde durur");
                Assert.LessOrEqual(Mathf.Max(b.size.x, b.size.z), 0.35f, "vana dahil ayak izi ≤ 0,35");
                Assert.Greater(b.size.y, 2 * Mathf.Max(b.size.x, b.size.z), "uzun ekseni yukarı (uçuşta uçuş yönüne yatar)");
                // Sıcak çember (RaidBoard3D: tube_hot r 0,36) ve uçuş hattının başı (0,3) tüpün dışında
                foreach (var x in new[] { b.min.x, b.max.x })
                    foreach (var z in new[] { b.min.z, b.max.z })
                        Assert.Less(new Vector2(x, z).magnitude, 0.3f, "tüp sıcak çemberin ve hattın başının içinde");
                foreach (var r in tube.GetComponentsInChildren<Renderer>(true))
                    foreach (var word in new[] { "Fan", "Grille", "Warning" })
                        StringAssert.DoesNotContain(word, r.name, "soğutma ünitesinin işareti: " + r.name);
                var mats = Materials(tube.gameObject).ToList();
                Assert.AreEqual("Tube",tube.GetComponentInChildren<Rev3Model>().ModelName,"soğutma modelinden türetilmez; ortak çelik malzemesi olabilir");
                Assert.IsFalse(mats.Any(m => IsYellow(m.GetColor("_BaseColor"))), "sarı (uyarı üçgeni) yok");
                Assert.IsTrue(mats.Any(m => IsOrange(m.GetColor("_BaseColor"))), "turuncu omuz bandı");
            });
        }

        /// <summary>Kaynak kolu kaidesi: 0,7'lik plaka; döner tablanın tepesi tahtanın kolu çizdiği yükseklikte; turuncu halka.</summary>
        [Test]
        public void WeldArmBase_TopMeetsTheDrawnArm()
        {
            With(Lib.weldArm, arm =>
            {
                var models=arm.GetComponentsInChildren<Rev3Model>();
                var b = BoundsOf(Visible(models.Single(m=>m.ModelName=="WeldArmBase")));
                Assert.AreEqual(RaidBoard3D.WeldArmHeight,BoundsOf(Visible(models.Single(m=>m.ModelName=="WeldArm"))).min.y,.005f,"kol modelinin altı tablayla buluşur");
                Assert.AreEqual(RaidBoard3D.WeldArmHeight, b.max.y, 0.005f, "kol tablanın tepesinden çıkar");
                Assert.AreEqual(0.7f, b.size.x, 0.02f, "plaka 0,7"); Assert.AreEqual(0.7f, b.size.z, 0.02f, "plaka 0,7");
                Assert.IsTrue(Materials(arm.gameObject).Any(m => IsOrange(m.GetColor("_BaseColor"))), "turuncu kaynak halkası");
            });
        }

        /// <summary>Tavan enkazı: alçak yığın, karenin içinde; 2 canın 1'inde hasar çatlakları görünür.</summary>
        [Test]
        public void CeilingDebris_IsLowAndShowsDamage()
        {
            var lib = Lib;
            With(lib.debris, debris =>
            {
                var b = BoundsOf(Visible(debris));
                Assert.LessOrEqual(b.max.y, 0.45f, "enkaz alçak");
                Assert.GreaterOrEqual(b.min.y, -0.005f, "zeminin altına girmez");
                Assert.LessOrEqual(Mathf.Max(b.max.x, -b.min.x, b.max.z, -b.min.z), 0.45f, "karenin içinde");
                Assert.AreEqual("CeilingDebris",debris.GetComponentInChildren<Rev3Model>().ModelName);
                Assert.DoesNotThrow(()=>debris.SetHp(1,2),"can ortak ekran etiketinde, kaynak geometri sabit kalır");
                Assert.DoesNotThrow(()=>debris.SetHp(2,2));
            });
        }

        /// <summary>Tıklama parçayı bulur: kökteki kutu görünen gövdeyi sarar (tepesi en az görünen tepe − 0,05, zeminden başlar).</summary>
        [Test]
        public void EventObjects_CollidersReachWhatTheyShow()
        {
            foreach (var kind in EventKinds)
                With(Lib.Object(kind), piece =>
                {
                    var box = piece.GetComponent<BoxCollider>();
                    Assert.IsNotNull(box, kind + " çarpıştırıcısı kökte");
                    var b = BoundsOf(Visible(piece));
                    var c = box.bounds;
                    Assert.GreaterOrEqual(c.max.y, b.max.y - 0.05f, kind + " çarpıştırıcısı görünen tepesine uzanır");
                    Assert.LessOrEqual(c.min.y, 0.05f, kind + " çarpıştırıcısı zeminden başlar");
                    Assert.GreaterOrEqual(c.size.x, b.size.x - 0.05f, kind); Assert.GreaterOrEqual(c.size.z, b.size.z - 0.05f, kind);
                });
        }

        /// <summary>Üretilen küçük resimler sprite tablosunda ve kayıtta; inceleme kartı her nesneye kendi resmini verir.</summary>
        [Test]
        public void PieceThumbs_AreRegisteredAndShownOnTheHoverCard()
        {
            var names = SiginakTextureBaker.PieceThumbNames;
            CollectionAssert.IsSubsetOf(new[] { "thumb_crate", "thumb_tube", "thumb_weld", "thumb_debris", "thumb_civ", "thumb_spawner" }, names);
            foreach (var name in names)
            {
                var e = SiginakSpriteManifest.Get(name);
                Assert.IsNotNull(e, name + " tabloda");
                Assert.IsTrue(e.Generated && e.Kind == SiginakSpriteManifest.Kind.Simple, name);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(e.AssetPath), name + " sprite olarak içe aktarıldı");
                Assert.IsTrue(DeskSprites.Instance.TryGet(name, out var entry) && entry.sprite != null && entry.type == Image.Type.Simple && !entry.tint, name + " kayıtta");
            }

            var owner = new GameObject("Olay nesnesi kartı");
            owner.SetActive(false);
            try
            {
                var source = owner.AddComponent<LiveRaidDataSource>();
                var so = new SerializedObject(source);
                so.FindProperty("theme").objectReferenceValue = SadeTheme.Active;
                so.ApplyModifiedPropertiesWithoutUndo();
                var state = Baskin.Create(new RaidParams { Seed = 11, Compute = 3 });
                var free = Enumerable.Range(0, Baskin.N * Baskin.N).Select(i => new Vector2Int(i % Baskin.N, i / Baskin.N))
                    .Where(t => Baskin.CellAt(state, t.x, t.y).T == "floor" && Baskin.CellAt(state, t.x, t.y).O == null && Baskin.UnitAt(state, t.x, t.y) == null)
                    .ToArray();
                var expected = new Dictionary<string, string>
                {
                    ["tup"] = "thumb_tube", ["kol"] = "thumb_weld", ["enkaz"] = "thumb_debris", ["civ"] = "thumb_civ", ["spawner"] = "thumb_spawner",
                    ["cool"] = "thumb_cooling", ["node"] = "thumb_cabinet_node",
                };
                int i = 0;
                var at = new Dictionary<string, Vector2Int>();
                foreach (var kind in expected.Keys)
                {
                    var t = free[i++];
                    state.Cells[t.y, t.x].O = new Baskin.Obj { K = kind, Hp = 1, Max = 1 };
                    at[kind] = t;
                }
                source.OpenForValidation(state);
                foreach (var pair in expected)
                {
                    source.Hover(at[pair.Key]);
                    typeof(RaidDataSource).GetField("hoverVisible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, true);
                    typeof(RaidDataSource).GetMethod("Publish", BindingFlags.Instance | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null).Invoke(source, null);
                    Assert.AreEqual(pair.Value, source.Current.HoverCard.Thumb, pair.Key);
                    Assert.IsTrue(DeskSprites.Instance.TryGet(pair.Value, out _), pair.Value + " kayıtta (eski resim kalmaz)");
                }
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
