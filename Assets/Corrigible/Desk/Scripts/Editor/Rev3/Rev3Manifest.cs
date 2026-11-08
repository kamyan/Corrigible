using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Revizyon 3 model manifesti (Docs/UI_Siginak_Rev3/modeller/manifest/&lt;Model&gt;.json; README: modeller/README.md).
    /// Koordinatlar Unity: metre, x sağ, y yukarı, ön yüz −z. Parça pivotu model uzayında (<see cref="Part.pivotModel"/>) ve
    /// ebeveyne göre (<see cref="Part.pivotLocal"/>); yazı, çapa ve ışık konumları sıfır pozda model uzayında.
    /// </summary>
    public sealed class Rev3Manifest
    {
        public const string Dir = "Docs/UI_Siginak_Rev3/modeller";

        public string name, family, budget, origin, note;
        public Part[] parts = new Part[0];
        public Text[] texts = new Text[0];
        public Anchor[] anchors = new Anchor[0];
        public LightPoint[] lights = new LightPoint[0];
        public Place[] place = new Place[0];
        public int tris;
        public Box bbox;
        public Mat[] materials = new Mat[0];

        public sealed class Part
        {
            public string name, parent, kind, axis, note;
            public float[] pivotModel, pivotLocal, range;
            public float? rest;
        }

        public sealed class Text
        {
            public string name, part, role, font, align, sample, sampleEn, note;
            public float[] center, size, normal, up;
            public float? capMm;
        }

        public sealed class Anchor { public string name, part, note; public float[] pos; }

        public sealed class LightPoint { public string name, part, type, color, note; public float[] pos; public bool shadows; }

        public sealed class Place { public float[] pos, euler; public float? yaw; public string note; }

        public sealed class Box { public float[] min, max, size; }

        public sealed class Mat
        {
            public string key, hex, note, tex, emissive;
            public float rough, metal;
            public float? alpha, ei;
            public int tris;
        }

        public static Vector3 V(float[] a) => a == null || a.Length < 3 ? Vector3.zero : new Vector3(a[0], a[1], a[2]);
        public static Vector2 V2(float[] a) => a == null || a.Length < 2 ? Vector2.zero : new Vector2(a[0], a[1]);

        public Bounds Bounds => new Bounds((V(bbox.min) + V(bbox.max)) / 2, V(bbox.max) - V(bbox.min));

        public string ObjSource => Dir + "/obj/" + name + ".obj";
        public string MtlSource => Dir + "/obj/" + name + ".mtl";
        public string SheetPath => Dir + "/sayfa/" + name + ".jpg";

        public static Rev3Manifest Load(string modelName) =>
            JsonConvert.DeserializeObject<Rev3Manifest>(File.ReadAllText(Dir + "/manifest/" + modelName + ".json"));

        public static IEnumerable<string> AllNames() =>
            Directory.GetFiles(Dir + "/manifest", "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n, StringComparer.Ordinal);

        public static List<Rev3Manifest> LoadAll() => AllNames().Select(Load).ToList();
    }
}
