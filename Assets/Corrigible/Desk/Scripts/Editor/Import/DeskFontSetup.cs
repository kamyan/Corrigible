using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Masa font asset'leri (PROMPT §6.5): her TTF için statik SDF atlas (<c>charset.txt</c>) + aynı TTF'den dinamik yedek,
    /// DejaVu sembol yedeği, ağırlık tabloları ve <c>DeskStyles</c> style sheet'i. Asset'ler <c>Resources/Fonts &amp; Materials</c>
    /// altında durur; böylece <c>&lt;font="…"&gt;</c> etiketi TMP Settings'e dokunmadan çalışır.
    /// Var olan asset yeniden yaratılmaz (GUID ve referanslar korunur); yedek, ağırlık ve malzeme ayarları her çalıştırmada yeniden uygulanır.
    /// </summary>
    public static class DeskFontSetup
    {
        public const string FontsDir = DeskSpriteManifest.Root + "/Fonts";
        public const string AssetDir = DeskSpriteManifest.Root + "/Resources/Fonts & Materials";
        public const string StyleSheetPath = DeskSpriteManifest.Root + "/Data/Theme/DeskStyles.asset";

        const int Padding = 9;
        const string FallbackGlyphs = "←↑→↓↔✓✗⠿№";

        sealed class Spec
        {
            public string Ttf, Name; public int Weight, Atlas = 1024; public bool Symbol;
            public Spec(string ttf, string name, int weight) { Ttf = ttf; Name = name; Weight = weight; }
        }

        static readonly Spec[] Specs =
        {
            new Spec("CourierPrime-Regular", "CP-Regular", 400), new Spec("CourierPrime-Bold", "CP-Bold", 700),
            new Spec("LibreFranklin-Regular", "LF-Regular", 400), new Spec("LibreFranklin-Medium", "LF-Medium", 500),
            new Spec("LibreFranklin-SemiBold", "LF-SemiBold", 600), new Spec("LibreFranklin-Bold", "LF-Bold", 700),
            new Spec("LibreFranklin-ExtraBold", "LF-ExtraBold", 800),
            new Spec("Kalam-Regular", "Kalam-Regular", 400), new Spec("Kalam-Bold", "Kalam-Bold", 700),
            new Spec("DejaVuSans", "DejaVuSans", 400) { Atlas = 512, Symbol = true },
            new Spec("DejaVuSans-Bold", "DejaVuSans-Bold", 700) { Atlas = 512, Symbol = true },
        };

        public static string AssetPath(string name) => AssetDir + "/" + name + " SDF.asset";
        static string DynPath(string name) => AssetDir + "/" + name + " Dyn SDF.asset";

        static bool refill;

        [MenuItem("Corrigible/Desk UI/Font asset'lerini kur")]
        public static void Build() => Build(false);

        /// <param name="refillExisting">Var olan statik asset'leri de yeniden doldur (kerning, karakter seti değişince).</param>
        public static void Build(bool refillExisting)
        {
            refill = refillExisting;
            var charset = File.ReadAllText(FontsDir + "/charset.txt").Replace("\n", "").Replace("\r", "");
            var fonts = Specs.ToDictionary(s => s.Name, s => AssetDatabase.LoadAssetAtPath<Font>(FontsDir + "/" + s.Ttf + ".ttf"));

            var has = Specs.ToDictionary(s => s.Name, s => Glyphs(fonts[s.Name], charset + FallbackGlyphs));

            // Ana fontlarda bulunmayan karakterler DejaVu'ya gider
            var missingInMain = new HashSet<char>(FallbackGlyphs);
            foreach (var s in Specs.Where(s => !s.Symbol))
                foreach (var c in charset) if (!has[s.Name].Contains(c)) missingInMain.Add(c);

            var assets = new Dictionary<string, TMP_FontAsset>();
            var log = new StringBuilder();
            foreach (var s in Specs)
            {
                // Boşluk (U+0020) da statik atlasta olmalı: yoksa her boşluklu metin dinamik yedekten bir alt ağ (ayrı draw call) açar
                var source = s.Symbol ? missingInMain.ToArray() : (" " + charset).ToCharArray();
                var chars = new string(source.Where(c => c >= ' ' && (c == ' ' || has[s.Name].Contains(c))).Distinct().ToArray());
                assets[s.Name] = StaticAsset(fonts[s.Name], s, chars, log);
                assets[s.Name + " Dyn"] = DynamicAsset(fonts[s.Name], s.Name);
            }

            // Yedek zinciri: kendi dinamik kopyası → DejaVu (400–500 Regular, 600+ Bold)
            foreach (var s in Specs)
            {
                var dejavu = s.Weight >= 600 ? "DejaVuSans-Bold" : "DejaVuSans";
                var main = assets[s.Name];
                var dyn = assets[s.Name + " Dyn"];
                main.fallbackFontAssetTable = s.Symbol ? new List<TMP_FontAsset> { dyn } : new List<TMP_FontAsset> { dyn, assets[dejavu] };
                dyn.fallbackFontAssetTable = s.Symbol ? new List<TMP_FontAsset>() : new List<TMP_FontAsset> { assets[dejavu] };
                EditorUtility.SetDirty(main); EditorUtility.SetDirty(dyn);
            }

            // Ağırlık tabloları: <b> sahte kalın değil gerçek asset'e gider
            Weights(assets, new[] { ("CP-Regular", 4), ("CP-Bold", 7) });
            Weights(assets, new[] { ("LF-Regular", 4), ("LF-Medium", 5), ("LF-SemiBold", 6), ("LF-Bold", 7), ("LF-ExtraBold", 8) });
            Weights(assets, new[] { ("Kalam-Regular", 4), ("Kalam-Bold", 7) });
            Weights(assets, new[] { ("DejaVuSans", 4), ("DejaVuSans-Bold", 7) });

            StyleSheet();
            AssetDatabase.SaveAssets();
            Debug.Log("[Desk] Font asset'leri hazır.\n" + log);
        }

        /// <summary>
        /// F7 düzeltmesi: mevcut statik asset'lere eksik boşluk karakterini ekler (atlas yeniden çizilmez; boşluğun bitmap'i yok)
        /// ve dinamik yedeklerin önbelleğini temizler.
        /// </summary>
        [MenuItem("Corrigible/Desk UI/Statik fontlara boşluk ekle")]
        public static string EnsureSpace()
        {
            var log = new StringBuilder();
            foreach (var s in Specs.Where(s => !s.Symbol))
            {
                var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath(s.Name));
                if (fa == null) continue;
                if (!fa.HasCharacter(' ', false, false))
                {
                    fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    bool ok = fa.TryAddCharacters(" ", out string miss, false);
                    fa.atlasPopulationMode = AtlasPopulationMode.Static;
                    EditorUtility.SetDirty(fa);
                    log.AppendLine($"{s.Name}: boşluk {(ok ? "eklendi" : "EKLENEMEDİ " + miss)} ({fa.characterTable.Count} karakter)");
                }
                var dyn = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DynPath(s.Name));
                if (dyn != null && dyn.characterTable.Count > 0) { dyn.ClearFontAssetData(); EditorUtility.SetDirty(dyn); }
            }
            AssetDatabase.SaveAssets();
            return log.Length == 0 ? "Hepsinde boşluk var." : log.ToString();
        }

        static HashSet<char> Glyphs(Font font, string chars)
        {
            FontEngine.LoadFontFace(font, 90);
            var set = new HashSet<char>();
            foreach (var c in chars) if (FontEngine.TryGetGlyphIndex(c, out uint gi) && gi != 0) set.Add(c);
            return set;
        }

        static TMP_FontAsset StaticAsset(Font font, Spec s, string chars, StringBuilder log)
        {
            var path = AssetPath(s.Name);
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
            {
                // Yerinde yeniden doldur (GUID ve referanslar korunur); kerning çiftleri de alınır
                if (refill || existing.fontFeatureTable.glyphPairAdjustmentRecords.Count == 0)
                {
                    existing.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    existing.ClearFontAssetData();
                    existing.TryAddCharacters(chars, out string miss, true);
                    existing.atlasPopulationMode = AtlasPopulationMode.Static;
                    EditorUtility.SetDirty(existing);
                }
                Material(existing);
                log.AppendLine($"{s.Name}: {existing.characterTable.Count} karakter, {existing.fontFeatureTable.glyphPairAdjustmentRecords.Count} kerning çifti");
                return existing;
            }

            // Tüm karakterleri tek atlasa sığdıran en büyük örnekleme boyu (90'dan aşağı)
            for (int pt = 90; pt >= 24; pt -= 2)
            {
                var fa = TMP_FontAsset.CreateFontAsset(font, pt, Padding, GlyphRenderMode.SDFAA, s.Atlas, s.Atlas, AtlasPopulationMode.Dynamic, false);
                if (fa.TryAddCharacters(chars, out string missing, true) || string.IsNullOrEmpty(missing))
                {
                    Save(fa, s.Name + " SDF", path);
                    fa.atlasPopulationMode = AtlasPopulationMode.Static;
                    Material(fa);
                    log.AppendLine($"{s.Name}: örnekleme {pt}, {fa.characterTable.Count} karakter, atlas {s.Atlas}²");
                    return fa;
                }
                Object.DestroyImmediate(fa.material); Object.DestroyImmediate(fa.atlasTexture); Object.DestroyImmediate(fa);
            }
            throw new System.Exception("Atlas'a sığmadı: " + s.Name);
        }

        static TMP_FontAsset DynamicAsset(Font font, string name)
        {
            var path = DynPath(name);
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) { Material(existing); return existing; }
            var fa = TMP_FontAsset.CreateFontAsset(font, 72, Padding, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, true);
            Save(fa, name + " Dyn SDF", path);
            var so = new SerializedObject(fa);
            so.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            Material(fa);
            return fa;
        }

        static void Save(TMP_FontAsset fa, string assetName, string path)
        {
            fa.name = assetName;
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTexture.name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            fa.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }

        /// <summary>
        /// Linear renk uzayı telafisi (KARARLAR F1): Linear'da ince yazının kenar yumuşatması tarayıcıdan açık kalıyor.
        /// Kalınlık 0,4 + keskinlik 0,8, 11–13 px yazıda mürekkep miktarını referansla eşitler (C5 ölçümü, oran ≈1,00).
        /// </summary>
        public const float LinearWeight = 0.4f, LinearSharpness = 0.8f;

        /// <summary>Masa yalnızca TMP'nin kendi SDF shader'ını kullanır (Sharpness ayarı için mobil değil tam sürüm).</summary>
        static void Material(TMP_FontAsset fa)
        {
            var m = fa.material;
            var sh = Shader.Find("TextMeshPro/Distance Field");
            if (m.shader != sh) m.shader = sh;
            m.SetFloat("_WeightNormal", LinearWeight);
            m.SetFloat("_Sharpness", LinearSharpness);
            EditorUtility.SetDirty(m);
        }

        static void Weights(Dictionary<string, TMP_FontAsset> assets, (string name, int index)[] family)
        {
            foreach (var (name, _) in family)
            {
                var fa = assets[name];
                var table = fa.fontWeightTable;   // dizinin kendisi döner; yerinde düzenlenir
                foreach (var (other, idx) in family) table[idx].regularTypeface = assets[other];
                EditorUtility.SetDirty(fa);
            }
        }

        /// <summary>Mock dizelerindeki etiketler: num = Courier 700 + mürekkep, numr = Courier 400, semi = Franklin 600 + mürekkep, hand = Kalam.</summary>
        static void StyleSheet()
        {
            var sheet = AssetDatabase.LoadAssetAtPath<TMP_StyleSheet>(StyleSheetPath);
            if (sheet == null)
            {
                sheet = ScriptableObject.CreateInstance<TMP_StyleSheet>();
                AssetDatabase.CreateAsset(sheet, StyleSheetPath);
            }
            var defs = new[]
            {
                ("num", "<font=\"CP-Bold SDF\"><color=#1E1B16>", "</color></font>"),
                ("numr", "<font=\"CP-Regular SDF\">", "</font>"),
                ("semi", "<font=\"LF-SemiBold SDF\"><color=#1E1B16>", "</color></font>"),
                ("hand", "<font=\"Kalam-Regular SDF\">", "</font>"),
            };
            var so = new SerializedObject(sheet);
            var list = so.FindProperty("m_StyleList");
            list.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("m_Name").stringValue = defs[i].Item1;
                e.FindPropertyRelative("m_OpeningDefinition").stringValue = defs[i].Item2;
                e.FindPropertyRelative("m_ClosingDefinition").stringValue = defs[i].Item3;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            sheet.RefreshStyles();
            EditorUtility.SetDirty(sheet);
        }
    }
}
