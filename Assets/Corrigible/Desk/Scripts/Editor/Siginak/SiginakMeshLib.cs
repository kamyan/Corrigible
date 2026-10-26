using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// three.js geometrilerinin Unity'de primitive karşılığı olmayanları (konik/açık/yarım silindir, simit) için kalıcı
    /// mesh'ler. Geometri three.js koordinatında üretilir, sonra z eksenine aynalanır ve üçgen sırası çevrilir (Unity sol el).
    /// Aynı parametreli mesh bir kez üretilir: <c>Art/Siginak/Meshes/&lt;anahtar&gt;.asset</c>.
    /// </summary>
    public static class SiginakMeshLib
    {
        public const string Dir = SiginakSpriteManifest.Root + "/Meshes";
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        static string F(float v) => v.ToString("0.#####", CultureInfo.InvariantCulture);

        /// <summary>THREE.CylinderGeometry(rTop, rBottom, h, seg, 1, open, θ0, θuzunluk); <paramref name="inside"/> iç yüzü gösterir.</summary>
        public static Mesh Cylinder(float rTop, float rBottom, float h, int seg, bool open, float theta0, float thetaLength, bool inside = false)
        {
            string key = $"cyl_{F(rTop)}_{F(rBottom)}_{F(h)}_{seg}_{(open ? "o" : "c")}_{F(theta0)}_{F(thetaLength)}{(inside ? "_in" : "")}";
            return Get(key, () =>
            {
                var v = new List<Vector3>(); var t = new List<int>();
                // Yan yüz
                for (int y = 0; y <= 1; y++)
                for (int i = 0; i <= seg; i++)
                {
                    float th = theta0 + i / (float)seg * thetaLength, r = y == 0 ? rTop : rBottom;
                    v.Add(new Vector3(r * Mathf.Sin(th), h / 2 - y * h, r * Mathf.Cos(th)));
                }
                for (int i = 0; i < seg; i++)
                {
                    int a = i, b = i + seg + 1, c = i + seg + 2, d = i + 1;
                    t.AddRange(new[] { a, b, d, b, c, d });
                }
                if (!open)
                    for (int top = 0; top <= 1; top++)
                    {
                        float y = top == 1 ? h / 2 : -h / 2, r = top == 1 ? rTop : rBottom;
                        if (r <= 0) continue;
                        int center = v.Count; v.Add(new Vector3(0, y, 0));
                        for (int i = 0; i <= seg; i++)
                        {
                            float th = theta0 + i / (float)seg * thetaLength;
                            v.Add(new Vector3(r * Mathf.Sin(th), y, r * Mathf.Cos(th)));
                        }
                        for (int i = 0; i < seg; i++)
                            if (top == 1) t.AddRange(new[] { center, center + 1 + i, center + 2 + i });
                            else t.AddRange(new[] { center, center + 2 + i, center + 1 + i });
                    }
                return Build(v, t, inside);
            });
        }

        /// <summary>THREE.TorusGeometry(R, r, radialSeg, tubularSeg, yay): XY düzleminde, ekseni z.</summary>
        public static Mesh Torus(float radius, float tube, int radialSeg, int tubularSeg, float arc)
        {
            string key = $"torus_{F(radius)}_{F(tube)}_{radialSeg}_{tubularSeg}_{F(arc)}";
            return Get(key, () =>
            {
                var v = new List<Vector3>(); var t = new List<int>();
                for (int j = 0; j <= radialSeg; j++)
                for (int i = 0; i <= tubularSeg; i++)
                {
                    float u = i / (float)tubularSeg * arc, w = j / (float)radialSeg * Mathf.PI * 2;
                    v.Add(new Vector3((radius + tube * Mathf.Cos(w)) * Mathf.Cos(u), (radius + tube * Mathf.Cos(w)) * Mathf.Sin(u), tube * Mathf.Sin(w)));
                }
                for (int j = 1; j <= radialSeg; j++)
                for (int i = 1; i <= tubularSeg; i++)
                {
                    int a = (tubularSeg + 1) * j + i - 1, b = (tubularSeg + 1) * (j - 1) + i - 1, c = (tubularSeg + 1) * (j - 1) + i, d = (tubularSeg + 1) * j + i;
                    t.AddRange(new[] { a, b, d, b, c, d });
                }
                return Build(v, t, false);
            });
        }

        /// <summary>THREE.RingGeometry(iç, dış, seg) / CircleGeometry (iç = 0): XY düzleminde, yüzü +z (three) = −z (Unity).</summary>
        public static Mesh Ring(float inner, float outer, int seg)
        {
            string key = $"ring_{F(inner)}_{F(outer)}_{seg}";
            return Get(key, () =>
            {
                var v = new List<Vector3>(); var t = new List<int>();
                for (int i = 0; i <= seg; i++)
                {
                    float th = i / (float)seg * Mathf.PI * 2;
                    v.Add(new Vector3(inner * Mathf.Cos(th), inner * Mathf.Sin(th), 0));
                    v.Add(new Vector3(outer * Mathf.Cos(th), outer * Mathf.Sin(th), 0));
                }
                for (int i = 0; i < seg; i++)
                {
                    int a = i * 2, b = a + 1, c = a + 3, d = a + 2;
                    t.AddRange(new[] { a, b, c, a, c, d });
                }
                return Build(v, t, false);
            });
        }

        /// <summary>three → Unity: z aynalanır, üçgen sırası çevrilir; normaller yeniden hesaplanır.</summary>
        static Mesh Build(List<Vector3> threeVerts, List<int> threeTris, bool inside)
        {
            var verts = new Vector3[threeVerts.Count];
            for (int i = 0; i < verts.Length; i++) verts[i] = new Vector3(threeVerts[i].x, threeVerts[i].y, -threeVerts[i].z);
            var tris = threeTris.ToArray();
            if (!inside)
                for (int i = 0; i < tris.Length; i += 3) (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh Get(string key, System.Func<Mesh> make)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            string path = Dir + "/" + key + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                Directory.CreateDirectory(Dir);
                mesh = make();
                mesh.name = key;
                AssetDatabase.CreateAsset(mesh, path);
            }
            Cache[key] = mesh;
            return mesh;
        }
    }
}
