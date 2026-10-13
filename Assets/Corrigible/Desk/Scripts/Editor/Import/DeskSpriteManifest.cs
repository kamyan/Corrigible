using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// <c>Art/Sprites/sprites.json</c>: her sprite'ın 9-dilim kenarı, pivotu, boyanabilirliği ve Image türü.
    /// İçe aktarıcı ve prefab kurulumu buradan okur; dosya değişince önbellek yenilenir.
    /// </summary>
    public static class DeskSpriteManifest
    {
        public const string Root = "Assets/Corrigible/Desk";
        public const string SpritesDir = Root + "/Art/Sprites";
        public const string TexturesDir = Root + "/Art/Textures";
        public const string MapDir = Root + "/Art/Map";
        public const string ManifestPath = SpritesDir + "/sprites.json";

        [Serializable]
        public sealed class Entry
        {
            public string file, group, imageType, use;
            public int[] size1x, sizePx;
            [JsonProperty("spriteBorderPx_LBRT")] public int[] border;
            public float[] pivot;
            public bool tint;

            public string Name => Path.GetFileNameWithoutExtension(file);
            public Vector4 Border => border == null || border.Length < 4 ? Vector4.zero : new Vector4(border[0], border[1], border[2], border[3]);
            public Vector2 Pivot => pivot == null || pivot.Length < 2 ? new Vector2(0.5f, 0.5f) : new Vector2(pivot[0], pivot[1]);
            public Vector2 Size => new Vector2(size1x[0], size1x[1]);
        }

        sealed class File { public List<Entry> sprites; }

        static Dictionary<string, Entry> cache;
        static DateTime stamp;

        public static Entry Get(string spriteName)
        {
            Load();
            return cache.TryGetValue(spriteName, out var e) ? e : null;
        }

        public static IEnumerable<Entry> All { get { Load(); return cache.Values; } }

        static void Load()
        {
            var full = Path.GetFullPath(ManifestPath);
            var t = System.IO.File.Exists(full) ? System.IO.File.GetLastWriteTimeUtc(full) : DateTime.MinValue;
            if (cache != null && t == stamp) return;
            stamp = t;
            cache = new Dictionary<string, Entry>();
            if (t == DateTime.MinValue) return;
            var f = JsonConvert.DeserializeObject<File>(System.IO.File.ReadAllText(full));
            foreach (var e in f.sprites) cache[e.Name] = e;
        }
    }
}
