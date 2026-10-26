using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Oda malzemeleri (Docs/UI_Siginak PROMPT §4): <c>tokens.materials</c> ve uygulama eki <c>materialsExtra</c> → URP Lit
    /// asset'leri (<c>Art/Siginak/Materials</c>). Renk, pürüzlülük (Smoothness = 1 − roughness), metal ve emissive
    /// tokenlardan; dokulu yüzeylerin taban rengi beyazdır (renk dokudadır, three.js'teki gibi). Odadaki TMP yazıları için
    /// TMP'nin kendi URP Lit shader'ıyla font başına malzeme (yazı oda ışığını alsın; özel shader yazılmaz).
    /// </summary>
    public static class SiginakMaterials
    {
        public const string Dir = SiginakSpriteManifest.Root + "/Materials";
        const string LitShader = "Universal Render Pipeline/Lit";
        const string UnlitShader = "Universal Render Pipeline/Unlit";
        const string TextLitShader = "TextMeshPro/SRP/TMP_SDF-URP Lit";

        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static void ClearCache() => Cache.Clear();

        /// <summary>Token malzemesi: önce “materials.&lt;ad&gt;”, yoksa “materialsExtra.&lt;ad&gt;”.</summary>
        public static Material Get(string name)
        {
            if (Cache.TryGetValue(name, out var m) && m != null) return m;
            var t = SiginakTheme.Active;
            string key = t.TryGet("materials." + name, out _) ? "materials." + name : "materialsExtra." + name;
            if (!t.TryGet(key, out var color)) throw new KeyNotFoundException("Sığınak malzeme tokenı yok: " + name);
            m = LoadOrCreate(name, LitShader);
            Lit(m, color, t.NumberOf(key + ".roughness", 0.8f), t.NumberOf(key + ".metal"));
            // Yalnız renk + şiddet birlikte verilmişse kendinden ışıklı. crtScreen (yalnız renk) ve counterFace (yalnız şiddet)
            // three.js'te emissiveMap'le yazı parlatıyor; Unity'de o yazılar TMP'dir, yüzey parlamaz.
            if (t.TryGet(key + ".emissive", out var emissive) && t.TryNumber(key + ".emissiveIntensity", out var intensity)) Emission(m, emissive, intensity);
            else NoEmission(m);
            return Cache[name] = Save(m);
        }

        /// <summary>Dokulu Lit malzeme (beton, linolyum, klavye): taban rengi beyaz, pürüzlülük token malzemesinden.</summary>
        public static Material Textured(string name, string textureName, string roughnessFrom, Vector2 tiling = default)
        {
            if (Cache.TryGetValue(name, out var m) && m != null) return m;
            var t = SiginakTheme.Active;
            m = LoadOrCreate(name, LitShader);
            Lit(m, Color.white, t.NumberOf("materials." + roughnessFrom + ".roughness", t.NumberOf("materialsExtra." + roughnessFrom + ".roughness", 0.9f)), 0);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SiginakSpriteManifest.TexturesDir + "/" + textureName + ".png")
                      ?? AssetDatabase.LoadAssetAtPath<Texture2D>(SiginakSpriteManifest.SpritesDir + "/" + textureName + ".png");
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling == default ? Vector2.one : tiling);
            NoEmission(m);
            return Cache[name] = Save(m);
        }

        /// <summary>Saydam kenarlı dokulu yüzey (afiş piktogramı, ilk yardım işareti): alfa kesme.</summary>
        public static Material Cutout(string name, string textureName, string tintKey, float roughness)
        {
            if (Cache.TryGetValue(name, out var m) && m != null) return m;
            m = Textured(name, textureName, "paper");
            Cache.Remove(name);
            m.SetColor("_BaseColor", tintKey == null ? Color.white : SiginakTheme.Active.ColorOf(tintKey));
            m.SetFloat("_Smoothness", 1 - roughness);
            m.SetFloat("_AlphaClip", 1);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            return Cache[name] = Save(m);
        }

        /// <summary>Kendinden ışıklı dokulu yüzey (telsiz kadranı): Unlit, renk = doku × token.</summary>
        public static Material Glow(string name, string textureName, string colorKey, float intensity)
        {
            if (Cache.TryGetValue(name, out var m) && m != null) return m;
            m = LoadOrCreate(name, UnlitShader);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SiginakSpriteManifest.TexturesDir + "/" + textureName + ".png");
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", SiginakTheme.Active.ColorOf(colorKey) * intensity);
            return Cache[name] = Save(m);
        }

        /// <summary>
        /// Işıksız dokulu ya da düz yüzey (sayaç çarkı, çark gölgesi, ± fişi kâğıdı, mühür bandı). Renk = beyaz × parlaklık;
        /// <paramref name="transparent"/> ise alfa karışımlı (MaterialPropertyBlock ile solar).
        /// </summary>
        public static Material UnlitTexture(string name, string textureName, float brightness, bool transparent, Vector2 tiling = default)
        {
            if (Cache.TryGetValue(name, out var m) && m != null) return m;
            m = LoadOrCreate(name, UnlitShader);
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(textureName))
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SiginakSpriteManifest.TexturesDir + "/" + textureName + ".png")
                      ?? AssetDatabase.LoadAssetAtPath<Texture2D>(SiginakSpriteManifest.SpritesDir + "/" + textureName + ".png");
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling == default ? Vector2.one : tiling);
            m.SetColor("_BaseColor", new Color(brightness, brightness, brightness, 1));
            if (transparent)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                m.SetFloat("_Surface", 0);
                m.SetFloat("_ZWrite", 1);
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = -1;
            }
            return Cache[name] = Save(m);
        }

        /// <summary>Odadaki TMP yazısı için fontun URP Lit kopyası (atlas ve SDF ayarları fonttan).</summary>
        public static Material TextLit(TMP_FontAsset font)
        {
            string name = "Text/" + font.name + " URP Lit";
            if (Cache.TryGetValue(name, out var m) && m != null) return m;
            var shader = Shader.Find(TextLitShader);
            if (shader == null) throw new FileNotFoundException("TMP URP Lit shader'ı yok: " + TextLitShader);
            m = LoadOrCreate(name, TextLitShader);
            m.CopyPropertiesFromMaterial(font.material);
            m.shader = shader;
            m.SetTexture("_MainTex", font.atlasTexture);
            // Kazınmış/boyalı yazı mat: parlama yazıyı zeminden açık gösteriyordu
            m.SetColor("_SpecularColor", Color.black);
            return Cache[name] = Save(m);
        }

        static void Lit(Material m, Color baseColor, float roughness, float metal)
        {
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Smoothness", Mathf.Clamp01(1 - roughness));
            m.SetFloat("_Metallic", Mathf.Clamp01(metal));
            m.SetFloat("_SpecularHighlights", 1);
            m.SetFloat("_EnvironmentReflections", 1);
        }

        static void Emission(Material m, Color color, float intensity)
        {
            m.EnableKeyword("_EMISSION");
            // SetColor HDR değeri sRGB sayıp yeniden dönüştürür; doğrusal değer SetVector ile olduğu gibi yazılır
            var linear = color.linear * intensity;
            m.SetVector("_EmissionColor", new Vector4(linear.r, linear.g, linear.b, 1));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        static void NoEmission(Material m)
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        static Material LoadOrCreate(string name, string shaderName)
        {
            string path = Dir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (m != null) { if (m.shader != shader) m.shader = shader; return m; }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            m = new Material(shader) { name = Path.GetFileName(name) };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material Save(Material m) { EditorUtility.SetDirty(m); return m; }
    }
}
