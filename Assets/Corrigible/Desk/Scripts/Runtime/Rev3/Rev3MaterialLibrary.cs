using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Revizyon 3 malzeme anahtarları → URP Lit malzemeleri (token 1.3 <c>materials.*</c>, <c>materialsExtra.*</c>,
    /// <c>materials3d.*</c>, <c>kits.*</c>; değerler model manifestlerinde aynı). Değerleri birebir aynı anahtarlar tek
    /// malzemeyi paylaşır. Doku notu (<c>tex</c>) olan anahtarlar şimdilik düz renktir; dokular ilgili aşamada eklenir.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Rev3 Material Library", fileName = "Rev3MaterialLibrary")]
    public sealed class Rev3MaterialLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public Material material;
            [Tooltip("Manifestteki doku notu (yoksa boş): düz renkle duruyor.")]
            public string textureNote;
            [Tooltip("Gölge vermez (notu “gölgesiz” ya da Unlit: leke, iz, işaret).")]
            public bool noShadow;
        }

        public List<Entry> entries = new List<Entry>();

        Dictionary<string, Entry> map;

        void OnEnable() => map = null;

        public bool TryGet(string key, out Material material)
        {
            if (map == null)
            {
                map = new Dictionary<string, Entry>();
                foreach (var e in entries) if (!string.IsNullOrEmpty(e.key)) map[e.key] = e;
            }
            material = map.TryGetValue(key ?? "", out var entry) ? entry.material : null;
            return material != null;
        }

        public Material Get(string key) => TryGet(key, out var m) ? m : null;

        /// <summary>Anahtarın malzemesi gölge vermemeli mi.</summary>
        public bool NoShadow(string key)
        {
            if (map == null) TryGet(key, out _);
            return map.TryGetValue(key ?? "", out var e) && e.noShadow;
        }

        public void Invalidate() => map = null;
    }
}
