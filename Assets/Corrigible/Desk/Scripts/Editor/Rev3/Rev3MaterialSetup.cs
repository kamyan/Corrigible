using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Revizyon 3 malzeme kitaplığı: manifestlerdeki her anahtar → URP Lit (özel shader yok). Değerler manifestte token
    /// 1.3 ile aynıdır (Aşama 0'da 184 ortak anahtarda fark yok); token'da olmayan kit ve parça anahtarları (<c>k_*</c>,
    /// <c>r_*</c>) yalnız manifestte durur. Değerleri birebir aynı anahtarlar tek malzemeyi paylaşır. Pürüzlülük → 1 − smoothness,
    /// saydamlık (<c>alpha</c>) → Transparent, ışıma (<c>emissive</c> × <c>ei</c>) → emission (bloom yok).
    /// Var olan malzeme yeniden yaratılmaz (GUID'ler korunur), değerleri yeniden yazılır.
    /// </summary>
    public static class Rev3MaterialSetup
    {
        public const string MaterialsDir = Rev3ModelImporter.ArtDir + "/Materials";
        public const string LibraryPath = MaterialsDir + "/Rev3MaterialLibrary.asset";

        static string Signature(Rev3Manifest.Mat m) =>
            string.Join("|", m.hex.ToUpperInvariant(), m.rough.ToString("R"), m.metal.ToString("R"), m.alpha?.ToString("R") ?? "",
                m.emissive ?? "", m.ei?.ToString("R") ?? "", (m.tex ?? "").Split(' ')[0]);

        public static Rev3MaterialLibrary Build(IEnumerable<Rev3Manifest> manifests, StringBuilder log)
        {
            var byKey = new Dictionary<string, Rev3Manifest.Mat>(StringComparer.Ordinal);
            foreach (var m in manifests)
                foreach (var mat in m.materials)
                    if (!byKey.ContainsKey(mat.key)) byKey[mat.key] = mat;
            // Modelde kullanılmayan ama swap durumu olan anahtarlar (alarmStepOn, readyLampOff, knockLampRed …) token'dan
            int fromTokens = 0;
            foreach (var mat in TokenMaterials())
                if (!byKey.ContainsKey(mat.key)) { byKey[mat.key] = mat; fromTokens++; }

            Directory.CreateDirectory(MaterialsDir);
            var lib = AssetDatabase.LoadAssetAtPath<Rev3MaterialLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<Rev3MaterialLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            lib.entries = new List<Rev3MaterialLibrary.Entry>();
            int created = 0, shared = 0;
            foreach (var group in byKey.Values.GroupBy(Signature).OrderBy(g => g.Min(x => x.key), StringComparer.Ordinal))
            {
                var keys = group.Select(x => x.key).OrderBy(k => k, StringComparer.Ordinal).ToList();
                var v = group.First();
                var shader = IsUnlit(v) ? unlitShader : lit;
                string path = MaterialsDir + "/" + keys[0] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = keys[0] };
                    AssetDatabase.CreateAsset(material, path);
                    created++;
                }
                else if (material.shader != shader) material.shader = shader;
                Apply(material, v);
                EditorUtility.SetDirty(material);
                if (keys.Count > 1) shared++;
                foreach (var k in keys)
                    lib.entries.Add(new Rev3MaterialLibrary.Entry { key = k, material = material, textureNote = v.tex ?? "", noShadow = CastsNoShadow(v) });
            }
            lib.Invalidate();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            // URP malzeme postprocessor'ı yeni malzemeyi ilk kayıtta “yükseltir” ve anahtar sözcükleri o anki değerlerden yeniden
            // kurar (ışıma kapalı kalabilir); değerler ikinci kez yazılır, ikinci kayıtta yükseltme yok.
            foreach (var group in byKey.Values.GroupBy(Signature))
            {
                var material = lib.Get(group.First().key);
                Apply(material, group.First());
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            log?.AppendLine($"Malzeme: {byKey.Count} anahtar ({fromTokens} yalnız token'da) → {lib.entries.Select(e => e.material).Distinct().Count()} malzeme " +
                            $"({shared} paylaşılan, {created} yeni); dokulu (şimdilik düz renk): {byKey.Values.Count(x => !string.IsNullOrEmpty(x.tex))}");
            return lib;
        }

        /// <summary>Token 1.3 <c>materials.*</c> ve <c>materialsExtra.*</c> (hex'li ve kaldırılmamış olanlar), manifest biçiminde.</summary>
        static IEnumerable<Rev3Manifest.Mat> TokenMaterials()
        {
            var root = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(SiginakThemeSetup.TokensPath));
            foreach (var group in new[] { "materials", "materialsExtra" })
            {
                if (!(root[group] is Newtonsoft.Json.Linq.JObject o)) continue;
                foreach (var p in o.Properties())
                {
                    if (p.Name.StartsWith("_") || !(p.Value is Newtonsoft.Json.Linq.JObject v) || v["hex"] == null || v["removed"] != null) continue;
                    float? F(string k) => v[k] != null && (v[k].Type == Newtonsoft.Json.Linq.JTokenType.Float || v[k].Type == Newtonsoft.Json.Linq.JTokenType.Integer) ? (float)v[k] : (float?)null;
                    string S(string k) => v[k] != null && v[k].Type == Newtonsoft.Json.Linq.JTokenType.String ? (string)v[k] : null;
                    yield return new Rev3Manifest.Mat
                    {
                        key = p.Name, hex = S("hex"), rough = F("roughness") ?? 0.8f, metal = F("metal") ?? 0, alpha = F("alpha"),
                        emissive = S("emissive"), ei = F("emissiveIntensity"), tex = S("texture"), note = S("note"),
                    };
                }
            }
        }

        /// <summary>Notu “Unlit” diyen anahtar (baskın asetatları ve halkaları: Unlit Transparent, ZWrite kapalı).</summary>
        public static bool IsUnlit(Rev3Manifest.Mat v) => (v.note ?? "").Contains("Unlit");

        /// <summary>Notu “gölgesiz” ya da Unlit diyen anahtar gölge vermez (leke, iz, işaret).</summary>
        public static bool CastsNoShadow(Rev3Manifest.Mat v) => IsUnlit(v) || (v.note ?? "").IndexOf("gölgesiz", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Doku notunun ilk sözcüğü (ör. “tx_counterDigits 128×1408: …”) <c>Art/Rev3/Textures</c>'ta varsa o doku.</summary>
        public static Texture2D TextureFor(Rev3Manifest.Mat v)
        {
            if (string.IsNullOrEmpty(v.tex)) return null;
            string name = v.tex.Split(' ')[0].TrimEnd(':');
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Rev3TextureBaker.Dir + "/" + name + ".png");
        }

        static Color Hex(string hex, Color fallback) => ColorUtility.TryParseHtmlString(hex ?? "", out var c) ? c : fallback;

        public static void Apply(Material m, Rev3Manifest.Mat v)
        {
            var baseColor = Hex(v.hex, Color.magenta);
            float alpha = Mathf.Clamp01(v.alpha ?? 1);
            baseColor.a = alpha;
            // Dokulu anahtar: doku rengi taşır, taban beyaz (alfa korunur); doku yoksa düz renk
            var texture = TextureFor(v);
            if (texture != null) baseColor = new Color(1, 1, 1, alpha);
            m.SetTexture("_BaseMap", texture);
            m.SetTexture("_MainTex", texture);
            m.SetFloat("_WorkflowMode", 1);
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_Smoothness", Mathf.Clamp01(1 - v.rough));
            m.SetFloat("_Metallic", Mathf.Clamp01(v.metal));

            if (alpha < 1)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 0);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                m.SetFloat("_Surface", 0);
                m.SetOverrideTag("RenderType", "Opaque");
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_ZWrite", 1);
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = -1;
            }

            if (!string.IsNullOrEmpty(v.emissive))
            {
                // Şiddet doğrusal uzayda çarpılır; SetColor gama rengi bekler
                var linear = Hex(v.emissive, Color.black).linear * (v.ei ?? 1);
                linear.a = 1;
                m.SetColor("_EmissionColor", linear.gamma);
                m.EnableKeyword("_EMISSION");
                // Projedeki ışımalı malzemelerle aynı: None'da anahtar sözcük test koşusunda bellekte sıfırlanıyor (Aşama 1)
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.SetColor("_EmissionColor", Color.black);
                m.DisableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }
}
