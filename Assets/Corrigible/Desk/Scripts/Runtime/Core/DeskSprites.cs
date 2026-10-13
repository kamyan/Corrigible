using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sprite kiti kaydı (<c>Resources/DeskSprites</c>): ad → sprite + Image türü. Durum değiştiren bileşenler
    /// (ör. devre dışıda kesik kenar) sprite'ı adla buradan alır. <c>Corrigible/Desk UI/Sprite kaydını güncelle</c> doldurur.
    /// </summary>
    public sealed class DeskSprites : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string name;
            public Sprite sprite;
            public Image.Type type;
            public bool tint;
        }

        public List<Entry> sprites = new List<Entry>();
        public List<Texture2D> textures = new List<Texture2D>();

        static DeskSprites instance;
        Dictionary<string, Entry> map;
        Dictionary<string, Texture2D> texMap;

        public static DeskSprites Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<DeskSprites>("DeskSprites");
                return instance;
            }
        }

        void OnEnable() { map = null; texMap = null; }

        public bool TryGet(string name, out Entry e)
        {
            if (map == null)
            {
                map = new Dictionary<string, Entry>();
                foreach (var s in sprites) map[s.name] = s;
            }
            return map.TryGetValue(name, out e);
        }

        public Texture2D Texture(string name)
        {
            if (texMap == null)
            {
                texMap = new Dictionary<string, Texture2D>();
                foreach (var t in textures) if (t != null) texMap[t.name] = t;
            }
            return texMap.TryGetValue(name, out var tex) ? tex : null;
        }

        /// <summary>Sprite'ı ve Image türünü uygular (yalnızca değiştiyse).</summary>
        public static void Apply(Image img, string spriteName)
        {
            if (img == null || Instance == null || !Instance.TryGet(spriteName, out var e)) return;
            if (img.sprite != e.sprite) img.sprite = e.sprite;
            // Düz kare dolgu her boyutta Simple: 9-dilimde ince çizgi (1 px) kenar dilimini örnekler ve atlas boşluğuyla
            // karışıp yarı saydam çıkar. Doldurulmuş (Filled) görüntüler türünü korur.
            var type = spriteName == "sh_fill_r0" ? Image.Type.Simple : e.type;
            if (img.type != type && img.type != Image.Type.Filled) img.type = type;
            if (img.pixelsPerUnitMultiplier != 1) img.pixelsPerUnitMultiplier = 1;
        }
    }
}
