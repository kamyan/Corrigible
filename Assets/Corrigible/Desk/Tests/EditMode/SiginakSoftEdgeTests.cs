using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Eğik kâğıtlarda kenar (2026-11-02): ekran katmanı kenar yumuşatmadığı için eğik çizilen sert kenarlı sprite basamaklanır
    /// ("kırık kenar"). Zemin, düğme, gölge ve çerçeveler yumuşak kenarlı türevle (1 px saydam pay), ince çizgiler 2 px kalın
    /// "Soft line" çocuğuyla çizilir (<see cref="SiginakTextureBaker.SoftEdges"/>).
    /// </summary>
    public class SiginakSoftEdgeTests
    {
        static Texture2D Load(string path)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), path);
            return tex;
        }

        [Test]
        public void SoftSprites_AreTheSourceWithATransparentRing()
        {
            foreach (var name in SiginakTextureBaker.SoftSources)
            {
                var src = SiginakSpriteManifest.Get(name);
                var soft = SiginakSpriteManifest.Get(name + SiginakSpriteManifest.SoftSuffix);
                Assert.IsNotNull(soft, name + " yumuşak türevi tabloda");
                Assert.IsTrue(soft.Generated, name);
                Assert.AreEqual(src.Kind, soft.Kind, name);
                Assert.AreEqual(src.SpriteBorder + Vector4.one, soft.SpriteBorder, name + " dilim kenarı +1");
                var a = Load(src.AssetPath);
                var b = Load(soft.AssetPath);
                try
                {
                    Assert.AreEqual((a.width + 2, a.height + 2), (b.width, b.height), name);
                    var pa = a.GetPixels32();
                    var pb = b.GetPixels32();
                    for (int y = 0; y < b.height; y++)
                        for (int x = 0; x < b.width; x++)
                        {
                            var c = pb[y * b.width + x];
                            if (x == 0 || y == 0 || x == b.width - 1 || y == b.height - 1) Assert.AreEqual(0, c.a, $"{name} ({x},{y}) saydam pay");
                            else Assert.AreEqual(pa[(y - 1) * a.width + x - 1].a, c.a, $"{name} ({x},{y}) kaynakla aynı");
                        }
                }
                finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
            }
        }

        [Test]
        public void SoftLines_HaveTransparentEdgesAcrossTheLine()
        {
            foreach (var name in SiginakTextureBaker.SoftLines)
            {
                var e = SiginakSpriteManifest.Get(name);
                Assert.IsNotNull(e, name);
                var tex = Load(e.AssetPath);
                try
                {
                    bool vertical = name == "ui_line_v_aa";
                    int across = vertical ? tex.width : tex.height;
                    var px = tex.GetPixels32();
                    for (int i = 0; i < (vertical ? tex.height : tex.width); i++)
                    {
                        Color32 At(int k) => vertical ? px[i * tex.width + k] : px[k * tex.width + i];
                        Assert.AreEqual(0, At(0).a, name + " bir yanı saydam");
                        Assert.AreEqual(0, At(across - 1).a, name + " öbür yanı saydam");
                    }
                    Assert.IsTrue(px.Any(c => c.a == 255), name + " çizgi dolu");
                }
                finally { Object.DestroyImmediate(tex); }
            }
        }

        static readonly HashSet<string> Hard = new HashSet<string>(SiginakTextureBaker.SoftSources.Concat(new[] { "ui_dash_h" }));

        static bool Rotated(Transform t)
        {
            float z = t.eulerAngles.z % 90f;
            if (z > 45) z -= 90;
            if (z < -45) z += 90;
            return Mathf.Abs(z) >= 0.05f;
        }

        static IEnumerable<string> HardRotated(GameObject root) =>
            root.GetComponentsInChildren<Image>(true)
                .Where(i => i.enabled && i.sprite != null && Hard.Contains(i.sprite.name) && Rotated(i.transform))
                .Select(i => i.sprite.name + " · " + string.Join("/", i.GetComponentsInParent<Transform>(true).Reverse().Skip(2).Select(t => t.name)));

        [Test]
        public void DeskGameScene_RotatedPapersHaveNoHardEdges()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var bad = scene.GetRootGameObjects().SelectMany(HardRotated).ToList();
                Assert.IsEmpty(bad, "eğik ve sert kenarlı:\n" + string.Join("\n", bad));
                // İnce çizgilerin görseli çocukta; kod yalnız çizgiyi açıp kapar
                var soft = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Image>(true)).Where(i => i.name == "Soft line").ToList();
                Assert.Greater(soft.Count, 30);
                foreach (var s in soft)
                {
                    var parent = s.transform.parent.GetComponent<Image>();
                    Assert.IsNotNull(parent, s.transform.parent.name);
                    Assert.IsFalse(parent.enabled, s.transform.parent.name + " ana görsel kapalı");
                    Assert.AreEqual(parent.color, s.color, s.transform.parent.name);
                }
                // Kodun atadığı sprite'lar (damga çerçevesi, alarm hücresi) eğik kâğıtlarda yumuşak
                foreach (var (type, field) in new[] { (typeof(OperationSlip), "frameSolid"), (typeof(OperationSlip), "frameDashed"), (typeof(DoorCardView), "frameSolid"),
                    (typeof(DoorCardView), "frameDashed"), (typeof(ShiftReportView), "cellFill"), (typeof(ShiftReportView), "cellFrame"),
                    (typeof(OperationReportView), "cellFill"), (typeof(OperationReportView), "cellFrame") })
                {
                    var c = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren(type, true)).Single();
                    var sprite = (Sprite)new SerializedObject(c).FindProperty(field).objectReferenceValue;
                    StringAssert.EndsWith(SiginakSpriteManifest.SoftSuffix, sprite.name, type.Name + "." + field);
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void RaidPanels_RotatedNotesHaveNoHardEdges()
        {
            foreach (var path in new[] { DeskSpriteManifest.Root + "/Prefabs/Sade/Raid/RaidHud.prefab", DeskSpriteManifest.Root + "/Prefabs/Sade/Raid/RaidPanels.prefab" })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var bad = HardRotated(root).ToList();
                    Assert.IsEmpty(bad, path + ":\n" + string.Join("\n", bad));
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
    }
}
