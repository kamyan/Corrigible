using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 8 (ikinci tur kararları, inceleme sonrası): CRT masasındaki telsiz. Telsiz ve altındaki cihaz kapı kanadının
    /// süpürmesinden çekildi; ahize kordonu hiçbir konumda hem kanattan hem CRT'den uzak kalamadığı için (CRT camını deliyor ya da
    /// ekran metninin önünden geçiyordu) <c>SM_Radio</c>'nun LOD'ları kordonsuz türev mesh'lerle (<c>SceneMeshes/Radio</c>) çizilir.
    /// Sahne açıkken (kullanıcının sahnesi) atlanır; sahne kaydedilmeden kapatılır.
    /// </summary>
    public sealed class KosStage8SceneTests
    {
        const string Path = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const string RadioMeshes = "Assets/Corrigible/Desk/Art/Rev3/SceneMeshes/Radio/";

        Scene scene, previous;
        bool userScene, opened;

        [OneTimeSetUp]
        public void Prepare()
        {
            var existing = SceneManager.GetSceneByPath(Path);
            userScene = existing.IsValid() && existing.isLoaded;
            previous = SceneManager.GetActiveScene();
        }

        [OneTimeTearDown]
        public void CloseWithoutSaving()
        {
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }

        void RequireScene()
        {
            if (userScene) Assert.Ignore("Sahne doğrulaması boş sahne açıkken çalıştırılmalı; kullanıcının açık sahnesine dokunulmadı.");
            if (opened) return;
            scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
            opened = true;
        }

        Transform Named(string name, string parent)
            => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Single(t => t.name == name && t.parent != null && t.parent.name == parent);

        /// <summary>Telsiz kökü altındaki LOD0 köşeleri (cihaz + telsiz), dünya uzayında.</summary>
        static List<Vector3> Lod0Points(Transform radio)
            => radio.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name.EndsWith("_LOD0"))
                .SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToList();

        [Test]
        public void Radio_DrawsCordlessDerivedMeshes_AtDecidedPlace()
        {
            RequireScene();
            var radio = Named("Radio", "CRT desk");
            // Kapıdan uzağa: masa ekseninde 9,3 cm geri ve 2,7 cm sola (aşama 7 konumu 0,235; 0,155); kanatla 1 cm, CRT ile 1 cm pay
            Assert.Less(Vector3.Distance(new Vector3(0.208f, 0.87f, 0.248f), radio.localPosition), 1e-4f, "telsiz kökünün yeri");
            var lods = radio.Find("SM_Radio").GetComponentsInChildren<MeshFilter>(true);
            Assert.AreEqual(5, lods.Length, "SM_Radio LOD0–4");
            foreach (var f in lods)
            {
                string path = AssetDatabase.GetAssetPath(f.sharedMesh);
                Assert.AreEqual(RadioMeshes + f.name + "_NoCord.asset", path, f.name + ": kordonsuz türev mesh");
                // Kordon gövdenin solundan sarkıyordu (kök yerelinde x ≈ −0,42); gövde −0,24'ten sola geçmez
                float minX = f.sharedMesh.vertices.Min(v => radio.InverseTransformPoint(f.transform.TransformPoint(v)).x);
                Assert.Greater(minX, -0.25f, f.name + ": gövdeden sola sarkan parça (kordon) kalmamalı");
            }
            var lodGroup = radio.Find("SM_Radio").GetComponent<LODGroup>();
            Assert.IsNotNull(lodGroup);
            Assert.AreEqual(5, lodGroup.GetLODs().Length);
        }

        [Test]
        public void Radio_ClearOfCrt()
        {
            RequireScene();
            var desk = Named("CRT", "CRT desk").parent;
            var radio = Named("Radio", "CRT desk");
            var tv = Named("CRT", "CRT desk").GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "SM_TV_2_LOD0");
            var lo = Vector3.positiveInfinity; var hi = Vector3.negativeInfinity;
            foreach (var v in tv.sharedMesh.vertices)
            {
                var p = desk.InverseTransformPoint(tv.transform.TransformPoint(v));
                lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
            }
            const float margin = 0.005f;
            var inside = Lod0Points(radio).Select(desk.InverseTransformPoint)
                .Count(p => p.x > lo.x - margin && p.x < hi.x + margin && p.y > lo.y - margin && p.y < hi.y + margin && p.z > lo.z - margin && p.z < hi.z + margin);
            Assert.AreEqual(0, inside, "telsiz/cihaz köşesi CRT kutusuna 5 mm'den yakın");
        }

        [Test]
        public void Radio_DoorLeafSweep_KeepsOneCentimetre()
        {
            RequireScene();
            var door = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DoorProp>(true)).Single();
            var leaf = door.Leaf;
            var parts = leaf.GetComponentsInChildren<MeshFilter>(true).Where(f => f.transform.parent == leaf && f.sharedMesh != null).ToList();
            Assert.That(parts.Select(f => f.name), Does.Contain("LeafMesh"));
            var points = Lod0Points(Named("Radio", "CRT desk"));
            Assert.Greater(points.Count, 5000);
            var rest = leaf.localRotation;
            float closest = float.MaxValue; int at = -1;
            for (int a = 0; a <= 95; a++)
            {
                var toLeaf = Matrix4x4.TRS(leaf.localPosition, rest * Quaternion.Euler(0, a, 0), leaf.localScale).inverse * leaf.parent.worldToLocalMatrix;
                foreach (var f in parts)
                {
                    var toPart = Matrix4x4.TRS(f.transform.localPosition, f.transform.localRotation, f.transform.localScale).inverse * toLeaf;
                    var b = f.sharedMesh.bounds;
                    foreach (var w in points)
                    {
                        var p = toPart.MultiplyPoint3x4(w);
                        // Kutunun dışındaki nokta için en büyük eksen açıklığı (> 0: değmiyor; mesh kutusu kanattan büyük, temkinli)
                        float gap = Mathf.Max(Mathf.Max(b.min.x - p.x, p.x - b.max.x), Mathf.Max(Mathf.Max(b.min.y - p.y, p.y - b.max.y), Mathf.Max(b.min.z - p.z, p.z - b.max.z)));
                        if (gap < closest) { closest = gap; at = a; }
                    }
                }
            }
            Assert.GreaterOrEqual(closest, 0.01f, "kanat " + at + "°'de telsize/cihaza 1 cm'den yakın");
        }
    }
}
