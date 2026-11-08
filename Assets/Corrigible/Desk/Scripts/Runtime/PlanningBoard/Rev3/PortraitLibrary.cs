using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kişi kartı portreleri (R16c; D8): ad anahtarı → portre dokusu. Anahtar adın ASCII küçük harfli hâlidir, sondaki sayı eki atılır
    /// ("İpek" → ipek, "Suna 12" → suna): portre adı izler, robot durumunu asla. Bilinmeyen ad yer tutucu dokuyu alır.
    /// <para>Her portre için şablondan (URP Lit, renk beyaz) bir malzeme ilk istendiğinde kurulur ve saklanır; <c>_BaseMap</c> dokusu
    /// tasarımın kart kırpmasıyla (viewBox [36, 26, 228, 250] / 300 × 340) döşenir: ölçek (0,7600; 0,7353), kayma (0,1200; 0,1882).
    /// Yer tutucu kırpılmaz (kendi çerçevesi 44 × 48,2 mm'dir).</para>
    /// Asset: <c>Desk/Data/Rev3/PortraitLibrary.asset</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Revizyon 3/Portre kitaplığı", fileName = "PortraitLibrary")]
    public sealed class PortraitLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("ASCII küçük harfli ad (ipek, suna).")]
            public string key;
            public Texture2D tex;
        }

        public static readonly Vector2 CropScale = new Vector2(0.7600f, 0.7353f);
        public static readonly Vector2 CropOffset = new Vector2(0.1200f, 0.1882f);
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [SerializeField] Entry[] entries = new Entry[0];
        [Tooltip("Portre malzemesinin şablonu (URP Lit, renk beyaz, pürüzlülük kart yüzüyle aynı).")]
        [SerializeField] Material template;
        [SerializeField] Texture2D placeholder;

        Dictionary<string, Texture2D> index;
        bool stale;
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        Material placeholderMaterial;

        public IReadOnlyList<Entry> Entries => entries;
        public Material Template => template;
        public Texture2D Placeholder => placeholder;

        /// <summary>Editör araçları ve testler için içerik.</summary>
        public void Configure(Entry[] list, Material templateMaterial, Texture2D placeholderTexture)
        {
            entries = list ?? new Entry[0];
            template = templateMaterial;
            placeholder = placeholderTexture;
            ClearCache();
        }

        /// <summary>Kişinin adının (ya da portre anahtarının) portre malzemesi; şablon yoksa null.</summary>
        public Material For(string name)
        {
            if (template == null) return null;
            if (stale) ClearCache();
            string key = KeyOf(name);
            if (key.Length > 0 && materials.TryGetValue(key, out var cached) && cached != null) return cached;
            var tex = TextureFor(key);
            if (tex == null) return PlaceholderMaterial();
            var m = Create("Portre " + key, tex, CropScale, CropOffset);
            materials[key] = m;
            return m;
        }

        /// <summary>Anahtarın kayıtlı dokusu (yoksa null).</summary>
        public Texture2D TextureFor(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (index == null)
            {
                index = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
                foreach (var e in entries)
                {
                    string k = KeyOf(e.key);
                    if (k.Length > 0 && e.tex != null && !index.ContainsKey(k)) index.Add(k, e.tex);
                }
            }
            return index.TryGetValue(key, out var t) ? t : null;
        }

        Material PlaceholderMaterial()
        {
            if (placeholderMaterial != null) return placeholderMaterial;
            placeholderMaterial = Create("Portre yer tutucu", placeholder, Vector2.one, Vector2.zero);
            return placeholderMaterial;
        }

        Material Create(string materialName, Texture2D tex, Vector2 scale, Vector2 offset)
        {
            var m = new Material(template) { name = materialName, hideFlags = HideFlags.DontSave };
            if (m.HasProperty(BaseMapId))
            {
                m.SetTexture(BaseMapId, tex);
                m.SetTextureScale(BaseMapId, scale);
                m.SetTextureOffset(BaseMapId, offset);
            }
            else
            {
                m.mainTexture = tex;
                m.mainTextureScale = scale;
                m.mainTextureOffset = offset;
            }
            return m;
        }

        /// <summary>Kurulan malzemeleri bırakır (asset değişince ya da kapanırken).</summary>
        public void ClearCache()
        {
            stale = false;
            index = null;
            foreach (var m in materials.Values) Release(m);
            materials.Clear();
            Release(placeholderMaterial);
            placeholderMaterial = null;
        }

        static void Release(Material m)
        {
            if (m == null) return;
            if (Application.isPlaying) Destroy(m);
            else DestroyImmediate(m);
        }

        // OnValidate içinde nesne yok edilmez: önbellek bir sonraki istekte bırakılır
        void OnValidate() { index = null; stale = true; }
        void OnDisable() => ClearCache();

        /// <summary>
        /// Adın portre anahtarı: kırpılır, sondaki sayı eki atılır, Türkçe harfler ASCII'ye katlanır (İ/ı → i, Ş → s, Ğ → g, Ü → u,
        /// Ö → o, Ç → c, şapkalılar düz), küçük harfe iner; harf ve rakam dışındaki karakterler düşer.
        /// </summary>
        public static string KeyOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int end = name.Length;
            while (end > 0 && char.IsWhiteSpace(name[end - 1])) end--;
            int digits = end;
            while (digits > 0 && char.IsDigit(name[digits - 1])) digits--;
            if (digits < end && digits > 0 && char.IsWhiteSpace(name[digits - 1])) end = digits - 1;
            var sb = new StringBuilder(end);
            for (int i = 0; i < end; i++)
            {
                char f = Fold(name[i]);
                if ((f >= 'a' && f <= 'z') || (f >= '0' && f <= '9')) sb.Append(f);
            }
            return sb.ToString();
        }

        static char Fold(char c)
        {
            switch (c)
            {
                case 'İ': case 'I': case 'ı': case 'Î': case 'î': case 'Í': case 'í': case 'Ì': case 'ì': case 'Ï': case 'ï': return 'i';
                case 'Ş': case 'ş': return 's';
                case 'Ğ': case 'ğ': return 'g';
                case 'Ü': case 'ü': case 'Û': case 'û': case 'Ú': case 'ú': case 'Ù': case 'ù': return 'u';
                case 'Ö': case 'ö': case 'Ó': case 'ó': case 'Ò': case 'ò': case 'Ô': case 'ô': return 'o';
                case 'Ç': case 'ç': return 'c';
                case 'Â': case 'â': case 'Á': case 'á': case 'À': case 'à': case 'Ä': case 'ä': return 'a';
                case 'É': case 'é': case 'È': case 'è': case 'Ê': case 'ê': case 'Ë': case 'ë': return 'e';
            }
            if (c >= 'A' && c <= 'Z') return (char)(c + 32);
            return c;
        }
    }
}
