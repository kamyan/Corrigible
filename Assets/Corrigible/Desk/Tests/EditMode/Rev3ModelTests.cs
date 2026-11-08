using System.Collections.Generic;
using System.Linq;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 Aşama 1 (model hattı): manifest → prefab. Her model için üçgen sayısı ve sınır kutusu (OBJ köşeleriyle
    /// ≤ 0,1 mm, manifest kutusunun ≤ 1 mm içinde), parça hiyerarşisi ve pivotlar, yazı yuvaları, çapalar, ışık noktaları,
    /// parçaların aralığında kendi ekseninde dönmesi/kayması, swap malzemeleri ve malzeme kitaplığı.
    /// Prefab'lar <c>Corrigible/Revizyon 3/Modelleri içe aktar</c> ile kurulur; manifest değişince yeniden içe aktarılmalı.
    /// </summary>
    public class Rev3ModelTests
    {
        static List<Rev3Manifest> manifests;
        static List<Rev3Manifest> Manifests => manifests ??= Rev3Manifest.LoadAll();

        static GameObject PrefabOf(Rev3Manifest m) => AssetDatabase.LoadAssetAtPath<GameObject>(Rev3ModelImporter.PrefabPath(m.family, m.name));

        [Test]
        public void EveryManifestHasAPrefab()
        {
            Assert.AreEqual(198, Manifests.Count, "paket 198 model");
            var missing = Manifests.Where(m => PrefabOf(m) == null || PrefabOf(m).GetComponent<Rev3Model>() == null).Select(m => m.name).ToList();
            Assert.IsEmpty(missing, "prefab'ı olmayan modeller");
            foreach (var m in Manifests)
            {
                var model = PrefabOf(m).GetComponent<Rev3Model>();
                Assert.AreEqual(m.name, model.ModelName); Assert.AreEqual(m.family, model.Family); Assert.AreEqual(m.tris, model.ManifestTris);
            }
        }

        [Test]
        public void TrisAndBoundsMatchManifest()
        {
            var bad = new List<string>();
            foreach (var m in Manifests)
            {
                var model = PrefabOf(m).GetComponent<Rev3Model>();
                int tris = Rev3ModelImporter.Tris(model);
                if (tris != m.tris) bad.Add($"{m.name}: üçgen {tris} ≠ {m.tris}");
                var b = Rev3ModelImporter.ZeroPoseBounds(model);
                float objError = Rev3ModelImporter.BoundsError(b, Rev3ModelImporter.ObjBounds(m, Rev3ModelImporter.ObjPath(m.family, m.name)));
                if (objError > 0.0001f) bad.Add($"{m.name}: OBJ kutusundan {objError * 1000:0.00} mm");
                var e = m.Bounds;
                float outside = Mathf.Max(Mathf.Max(e.min.x - b.min.x, e.min.y - b.min.y, e.min.z - b.min.z), Mathf.Max(b.max.x - e.max.x, b.max.y - e.max.y, b.max.z - e.max.z));
                if (outside > 0.001f) bad.Add($"{m.name}: manifest kutusunun {outside * 1000:0.0} mm dışında");
                foreach (var budget in new[] { ("big", 4000), ("small", 800), ("raid", 400), ("unit", 1500) })
                    if (m.budget == budget.Item1 && tris > budget.Item2) bad.Add($"{m.name}: bütçe {tris}/{budget.Item2}");
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void HierarchySlotsAnchorsAndLightsMatchManifest()
        {
            var bad = new List<string>();
            foreach (var m in Manifests)
            {
                var model = PrefabOf(m).GetComponent<Rev3Model>();
                if (model.Parts.Length != m.parts.Length) bad.Add($"{m.name}: parça {model.Parts.Length} ≠ {m.parts.Length}");
                foreach (var p in m.parts)
                {
                    var d = model.Part(p.name);
                    if (d == null) { bad.Add($"{m.name}/{p.name}: parça yok"); continue; }
                    string parent = d.transform.parent.name;
                    if (parent != (p.parent ?? "Body")) bad.Add($"{m.name}/{p.name}: ebeveyn {parent} ≠ {p.parent}");
                    if ((d.PivotLocal - Rev3Manifest.V(p.pivotLocal)).magnitude > 1e-5f) bad.Add($"{m.name}/{p.name}: pivot");
                    if (p.kind != "swap" && Mathf.Abs(d.Rest - (p.rest ?? 0)) > 1e-4f) bad.Add($"{m.name}/{p.name}: rest");
                }
                if (model.Slots.Length != m.texts.Length) bad.Add($"{m.name}: yazı yuvası {model.Slots.Length} ≠ {m.texts.Length}");
                foreach (var x in m.texts)
                {
                    var s = model.Slot(x.name);
                    if (s == null) { bad.Add($"{m.name}/{x.name}: yuva yok"); continue; }
                    if (s.transform.parent.name != (x.part ?? "Body")) bad.Add($"{m.name}/{x.name}: yuvanın parçası {s.transform.parent.name}");
                    if (!s.IsSurface && (s.Text == null || string.IsNullOrEmpty(s.Text.text))) bad.Add($"{m.name}/{x.name}: örnek metin boş");
                    // Yuva model uzayında manifestteki merkezde (sıfır poz; dinlenmesi 0 olmayan parçada yazı yok)
                    var parentPivot = x.part == null || x.part == "Body" ? Vector3.zero : Rev3Manifest.V(m.parts.First(p => p.name == x.part).pivotModel);
                    var expected = Rev3Manifest.V(x.center) - parentPivot;
                    if ((s.transform.localPosition - expected).magnitude > 0.001f) bad.Add($"{m.name}/{x.name}: konum");
                    if (Vector3.Angle(-(s.transform.localRotation * Vector3.forward), Rev3Manifest.V(x.normal)) > 0.5f) bad.Add($"{m.name}/{x.name}: normal");
                }
                if (model.Anchors.Length != m.anchors.Length) bad.Add($"{m.name}: çapa {model.Anchors.Length} ≠ {m.anchors.Length}");
                int lights = m.lights.Count(l => l.type == "point" || l.type == "spot");
                if (model.Lights.Length != lights) bad.Add($"{m.name}: ışık {model.Lights.Length} ≠ {lights}");
                if (model.Lights.Any(l => l.enabled)) bad.Add($"{m.name}: prefab ışığı açık (poz ışığı açar)");
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        static Vector3 AxisOf(PartDriver d) => d.Axis == PartAxis.X ? Vector3.right : d.Axis == PartAxis.Y ? Vector3.up : Vector3.forward;

        [Test]
        public void PartsMoveAroundTheirAxisWithinRange()
        {
            var bad = new List<string>();
            int rotate = 0, slide = 0, swap = 0;
            foreach (var m in Manifests)
            {
                var go = Object.Instantiate(PrefabOf(m));
                try
                {
                    foreach (var d in go.GetComponent<Rev3Model>().Parts)
                    {
                        var tr = d.transform;
                        if (d.Kind == PartKind.Rotate)
                        {
                            rotate++;
                            d.Set(d.RangeTo);
                            if (Quaternion.Angle(tr.localRotation, Quaternion.AngleAxis(d.RangeTo, AxisOf(d))) > 0.01f) bad.Add($"{m.name}/{d.PartName}: dönüş");
                            if ((tr.localPosition - d.PivotLocal).magnitude > 1e-6f) bad.Add($"{m.name}/{d.PartName}: dönerken pivot kaydı");
                            d.Set(d.RangeTo + Mathf.Sign(d.RangeTo - d.RangeFrom) * 30);
                            if (!Mathf.Approximately(d.Value, d.RangeTo)) bad.Add($"{m.name}/{d.PartName}: aralık dışına çıktı");
                        }
                        else if (d.Kind == PartKind.Slide)
                        {
                            slide++;
                            d.Set(d.RangeTo);
                            if ((tr.localPosition - (d.PivotLocal + AxisOf(d) * d.RangeTo)).magnitude > 1e-6f) bad.Add($"{m.name}/{d.PartName}: kayma");
                        }
                        else if (d.SwapCount > 1)
                        {
                            swap++;
                            var r = d.GetComponent<Renderer>();
                            var before = r.sharedMaterials.ToArray();
                            d.SetSwap(1);
                            if (r.sharedMaterials.SequenceEqual(before)) bad.Add($"{m.name}/{d.PartName}: swap malzemesi değişmedi");
                            d.SetSwap(0);
                            if (!r.sharedMaterials.SequenceEqual(before)) bad.Add($"{m.name}/{d.PartName}: swap geri dönmedi");
                        }
                        d.ResetToRest();
                        if (d.Kind != PartKind.Swap && !Mathf.Approximately(d.Value, d.Rest)) bad.Add($"{m.name}/{d.PartName}: dinlenmeye dönmedi");
                    }
                }
                finally { Object.DestroyImmediate(go); }
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
            Assert.AreEqual(76, rotate, "dönen parça"); Assert.AreEqual(13, slide, "kayan parça");
            Assert.Greater(swap, 20, "malzeme değiştiren swap parçası");
        }

        /// <summary>Aşama 0: uzun kol +x, kısa kol +z, küp üstte ve önde; Flap +z ekseninde 90°'de dik kalkar.</summary>
        [Test]
        public void AxisTestStandsInUnityAxes()
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Rev3ModelImporter.PrefabPath("test", "_AxisTest")));
            try
            {
                var model = go.GetComponent<Rev3Model>();
                var b = Rev3ModelImporter.ZeroPoseBounds(model);
                Assert.AreEqual(0.62f, b.max.x, 1e-4f, "uzun kol +x"); Assert.AreEqual(0.25f, b.max.z, 1e-4f, "kısa kol +z (arka)");
                Assert.AreEqual(0.16f, b.max.y, 1e-4f, "küp üstte"); Assert.AreEqual(-0.08f, b.min.z, 1e-4f, "küp önde (−z)");
                var flap = model.Part("Flap");
                flap.Set(90);
                var tip = flap.transform.TransformPoint(new Vector3(0.12f, 0, 0)) - flap.transform.position;
                Assert.Greater(tip.y, 0.11f, "Flap 90°'de yukarı");
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>Punto büyük harf yüksekliğinden (fontun cap oranı); şablon rolü Big Shoulders Stencil'dir.</summary>
        [Test]
        public void TextSlotsUseCapHeightAndRoleFont()
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Rev3ModelImporter.PrefabPath("desk", "CounterRack")));
            try
            {
                foreach (var s in go.GetComponent<Rev3Model>().Slots)
                {
                    s.Text.ForceMeshUpdate();
                    // Düz üstlü ve düz tabanlı büyük harf (yuvarlak harfler taşar)
                    var c = s.Text.textInfo.characterInfo.Take(s.Text.textInfo.characterCount).First(ch => "EFHIKLMNRDTZ".IndexOf(ch.character) >= 0);
                    float capMm = (c.topLeft.y - c.bottomLeft.y) * 1000;
                    Assert.AreEqual(s.CapMm, capMm, s.CapMm * 0.06f, s.SlotName + " büyük harf");
                }
            }
            finally { Object.DestroyImmediate(go); }

            var stencils = Manifests.SelectMany(m => m.texts.Where(t => (t.role ?? "").StartsWith("wallStencil")).Select(t => (m, t))).ToList();
            Assert.Greater(stencils.Count, 5);
            var bss = DeskTheme.Active.Font(DeskFont.StencilExtraBold);
            Assert.AreEqual("BSS-ExtraBold SDF", bss.name);
            foreach (var (m, t) in stencils)
                Assert.AreEqual(bss, PrefabOf(m).GetComponent<Rev3Model>().Slot(t.name).Text.font, $"{m.name}/{t.name} şablon fontu");
        }

        [Test]
        public void MaterialLibraryCoversEveryKeyWithTokenValues()
        {
            var lib = AssetDatabase.LoadAssetAtPath<Rev3MaterialLibrary>(Rev3MaterialSetup.LibraryPath);
            Assert.IsNotNull(lib);
            var bad = new List<string>();
            foreach (var mat in Manifests.SelectMany(m => m.materials).GroupBy(x => x.key).Select(g => g.First()))
            {
                var material = lib.Get(mat.key);
                if (material == null) { bad.Add(mat.key + ": yok"); continue; }
                bool unlit = Rev3MaterialSetup.IsUnlit(mat);
                if (material.shader.name != (unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")) bad.Add(mat.key + ": shader " + material.shader.name);
                if (lib.NoShadow(mat.key) != Rev3MaterialSetup.CastsNoShadow(mat)) bad.Add(mat.key + ": gölge bayrağı");
                ColorUtility.TryParseHtmlString(mat.hex, out var hex);
                if (Rev3MaterialSetup.TextureFor(mat) != null)
                {
                    // Dokulu anahtar: doku bağlı, taban beyaz
                    if (material.GetTexture("_BaseMap") != Rev3MaterialSetup.TextureFor(mat)) bad.Add(mat.key + ": doku");
                    hex = Color.white;
                }
                var c = material.GetColor("_BaseColor");
                if (Mathf.Abs(c.r - hex.r) > 0.002f || Mathf.Abs(c.g - hex.g) > 0.002f || Mathf.Abs(c.b - hex.b) > 0.002f) bad.Add(mat.key + ": renk");
                if (!unlit && Mathf.Abs(material.GetFloat("_Smoothness") - (1 - mat.rough)) > 1e-4f) bad.Add(mat.key + ": pürüzlülük");
                if (!unlit && Mathf.Abs(material.GetFloat("_Metallic") - mat.metal) > 1e-4f) bad.Add(mat.key + ": metal");
                bool transparent = (mat.alpha ?? 1) < 1;
                if (transparent != material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")) bad.Add(mat.key + ": saydamlık");
                if (!string.IsNullOrEmpty(mat.emissive) != material.IsKeywordEnabled("_EMISSION")) bad.Add(mat.key + ": ışıma");
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        /// <summary>Paketteki devir fişinin OBJ'si bozuk (15 yüz); içe aktarıcı PlaneGeometry dizinini yeniden yazar.</summary>
        [Test]
        public void HandoverSlipFacesAreRepaired()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Rev3ModelImporter.PrefabPath("desk", "HandoverSlip")).GetComponent<Rev3Model>();
            Assert.AreEqual(64, Rev3ModelImporter.Tris(model));
            var b = Rev3ModelImporter.ZeroPoseBounds(model);
            Assert.AreEqual(0.1f, b.size.x, 1e-4f); Assert.AreEqual(0.15f, b.size.y, 1e-4f);
        }
    }
}
