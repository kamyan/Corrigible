using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.EditorTools
{
    /// <summary>
    /// Çeviri tablosunun bakımı. Kaynak metinleri dört yerden toplar: koddaki <c>Loc.T/F/N("...")</c> sabitleri,
    /// <see cref="TutorialSequence"/> asset'leri, prefab'lardaki ve sahnelerdeki <see cref="LocalizedText"/> bileşenleri.
    /// Tabloyu (<c>Resources/Localization/strings.txt</c>) bunlara göre günceller: yeni metinler boş çeviriyle eklenir,
    /// artık kullanılmayanlar dosyanın sonunda saklanır (çeviri kaybolmasın).
    /// </summary>
    public static class LocalizationTool
    {
        public const string TableFile = "Assets/Corrigible/Resources/Localization/strings.txt";
        const string Root = "Assets/Corrigible";
        const string UnusedMarker = "# --- Kullanılmayan çeviriler (koddan silinmiş metinler; gerekirse geri alınır) ---";

        static readonly Regex LocCall = new Regex(@"Loc\.[TFN]\(\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);

        /// <summary>Kaynak metin → nerede geçtiği (ilk görüldüğü sırayla).</summary>
        public static Dictionary<string, List<string>> CollectSources()
        {
            var found = new Dictionary<string, List<string>>();
            void Add(string src, string origin)
            {
                if (string.IsNullOrEmpty(src)) return;
                if (!found.TryGetValue(src, out var list)) found[src] = list = new List<string>();
                if (!list.Contains(origin)) list.Add(origin);
            }

            foreach (var file in Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories).OrderBy(f => f))
            {
                var path = file.Replace('\\', '/');
                if (path.Contains("/Editor/") || path.Contains("/Tests/")) continue;
                foreach (Match m in LocCall.Matches(File.ReadAllText(path))) Add(UnescapeCs(m.Groups[1].Value), path);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:TutorialSequence", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var seq = AssetDatabase.LoadAssetAtPath<TutorialSequence>(path);
                foreach (var s in seq.steps) { Add(s.title, path); Add(s.body, path); }
                foreach (var t in seq.tips) { Add(t.title, path); Add(t.body, path); }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var lt in go.GetComponentsInChildren<LocalizedText>(true)) Add(lt.Source, path);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                WithScene(path, scene =>
                {
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var lt in root.GetComponentsInChildren<LocalizedText>(true)) Add(lt.Source, path);
                });
            }
            return found;
        }

        /// <summary>Sahneyi açık değilse geçici olarak ekler, işi yapar, kapatır.</summary>
        static void WithScene(string path, System.Action<Scene> work)
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = false;
            if (!scene.isLoaded) { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened = true; }
            try { work(scene); }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        // ------------------------------------------------------------------ tablo
        public sealed class Table
        {
            public List<string> Langs = new List<string>();
            /// <summary>Kaynak → (dil → çeviri); dosyadaki sırayla.</summary>
            public List<(string Source, Dictionary<string, string> Tr)> Rows = new List<(string, Dictionary<string, string>)>();
            public Dictionary<string, string> Get(string src) => Rows.FirstOrDefault(r => r.Source == src).Tr;
        }

        public static Table ReadTable()
        {
            var t = new Table();
            if (!File.Exists(TableFile)) { t.Langs.Add(Loc.English); return t; }
            foreach (var raw in File.ReadAllLines(TableFile))
            {
                if (raw.Length == 0 || raw[0] == '#') continue;
                var cells = raw.Split('\t');
                if (t.Langs.Count == 0) { t.Langs.AddRange(cells.Skip(1)); continue; }
                var tr = new Dictionary<string, string>();
                for (int i = 1; i < cells.Length && i <= t.Langs.Count; i++) tr[t.Langs[i - 1]] = Loc.Unescape(cells[i]);
                t.Rows.Add((Loc.Unescape(cells[0]), tr));
            }
            return t;
        }

        [MenuItem("Corrigible/Lokalizasyon/Çeviri tablosunu güncelle")]
        public static string UpdateTable()
        {
            var sources = CollectSources();
            var old = ReadTable();
            var langs = old.Langs.Count > 0 ? old.Langs : new List<string> { Loc.English };
            var sb = new StringBuilder();
            sb.AppendLine("# Corrigible çeviri tablosu. Sütunlar sekmeyle ayrılır; ilk sütun Türkçe kaynak (koddaki metnin aynısı).");
            sb.AppendLine("# Kaçış dizileri: \\n satır sonu, \\t sekme, \\\\ ters bölü. {0}, {1:0.0} gibi yer tutucular çeviride de olmalı.");
            sb.AppendLine("# Bu dosyayı \"Corrigible > Lokalizasyon > Çeviri tablosunu güncelle\" üretir; yalnızca çeviri sütunlarını düzenle.");
            sb.AppendLine("tr\t" + string.Join("\t", langs));

            int added = 0;
            string lastOrigin = null;
            foreach (var kv in sources.OrderBy(k => OriginOrder(k.Value[0])))
            {
                string origin = kv.Value[0];
                if (origin != lastOrigin) { sb.AppendLine("# " + origin); lastOrigin = origin; }
                var tr = old.Get(kv.Key);
                if (tr == null) added++;
                tr = Seed(kv.Key, tr, langs);
                sb.AppendLine(Row(kv.Key, langs, tr));
            }
            var unused = old.Rows.Where(r => !sources.ContainsKey(r.Source)).ToList();
            if (unused.Count > 0)
            {
                sb.AppendLine(UnusedMarker);
                foreach (var r in unused) sb.AppendLine("#" + Row(r.Source, langs, r.Tr));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(TableFile));
            File.WriteAllText(TableFile, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(TableFile);
            string msg = $"Çeviri tablosu: {sources.Count} metin, {added} yeni, {unused.Count} kullanılmayan. Eksik çeviri: {Validate().Count}";
            Debug.Log(msg);
            return msg;
        }

        /// <summary>Analist masası (UI_C) dize tablosu: Türkçe → taslak İngilizce. Boş çeviriler buradan tohumlanır.</summary>
        const string DeskStringsCsv = "Docs/UI_C/spec/strings_tr_en.csv";
        static Dictionary<string, string> deskSeeds;

        static Dictionary<string, string> Seed(string src, Dictionary<string, string> tr, List<string> langs)
        {
            if (!langs.Contains(Loc.English) || (tr != null && tr.TryGetValue(Loc.English, out var en) && !string.IsNullOrEmpty(en))) return tr;
            if (deskSeeds == null)
            {
                deskSeeds = new Dictionary<string, string>();
                if (File.Exists(DeskStringsCsv))
                    foreach (var line in File.ReadAllLines(DeskStringsCsv).Skip(1))
                    {
                        var cells = CsvCells(line);
                        if (cells.Count >= 3 && !string.IsNullOrEmpty(cells[1]) && !deskSeeds.ContainsKey(cells[1])) deskSeeds[cells[1]] = cells[2];
                    }
            }
            if (!deskSeeds.TryGetValue(src, out var seed)) return tr;
            var copy = tr != null ? new Dictionary<string, string>(tr) : new Dictionary<string, string>();
            copy[Loc.English] = seed;
            return copy;
        }

        /// <summary>Tırnaklı alanları destekleyen basit CSV satırı ayrıştırıcı.</summary>
        static List<string> CsvCells(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else sb.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { cells.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            cells.Add(sb.ToString());
            return cells;
        }

        // Kod dosyalarındaki sıra korunur; asset'ler sona
        static string OriginOrder(string origin) => (origin.EndsWith(".cs") ? "0" : origin.EndsWith(".asset") ? "1" : origin.EndsWith(".prefab") ? "2" : "3") + origin;

        static string Row(string src, List<string> langs, Dictionary<string, string> tr) =>
            Loc.Escape(src) + "\t" + string.Join("\t", langs.Select(l => tr != null && tr.TryGetValue(l, out var v) ? Loc.Escape(v) : ""));

        /// <summary>Eksik ya da hatalı çeviriler: (kaynak, sorun).</summary>
        [MenuItem("Corrigible/Lokalizasyon/Çevirileri doğrula")]
        public static List<string> ValidateMenu() { var p = Validate(); Debug.Log(p.Count == 0 ? "Çeviriler tam." : string.Join("\n", p)); return p; }

        public static List<string> Validate()
        {
            var problems = new List<string>();
            var table = ReadTable();
            var sources = CollectSources();
            foreach (var src in sources.Keys)
            {
                var tr = table.Get(src);
                foreach (var lang in table.Langs)
                {
                    if (tr == null || !tr.TryGetValue(lang, out var t) || t.Length == 0) { problems.Add($"[{lang}] çeviri yok: {Short(src)}"); continue; }
                    if (!SamePlaceholders(src, t)) problems.Add($"[{lang}] yer tutucular farklı: {Short(src)} → {Short(t)}");
                    if (Tags(src) != Tags(t)) problems.Add($"[{lang}] zengin metin etiketleri farklı: {Short(src)} → {Short(t)}");
                }
            }
            return problems;
        }

        static string Short(string s) => (s.Length > 80 ? s.Substring(0, 80) + "…" : s).Replace("\n", "\\n");

        static readonly Regex Placeholder = new Regex(@"(?<!\{)\{(\d+)(?:[,:][^}]*)?\}(?!\})");
        static bool SamePlaceholders(string a, string b) =>
            Placeholder.Matches(a).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x)
                .SequenceEqual(Placeholder.Matches(b).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x));

        static readonly Regex Tag = new Regex(@"</?(b|i|color|size)\b");
        static string Tags(string s) => string.Join(",", Tag.Matches(s).Cast<Match>().Select(m => m.Value).OrderBy(x => x));

        static string UnescapeCs(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\' || i + 1 >= s.Length) { sb.Append(s[i]); continue; }
                char n = s[++i];
                switch (n)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '0': sb.Append('\0'); break;
                    case 'u': sb.Append((char)System.Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; break;
                    default: sb.Append(n); break;
                }
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ LocalizedText ekleme
        /// <summary>Harf içeren ve henüz LocalizedText'i olmayan her TMP metnine bileşeni ekler (prefab'lar + Main sahnesi).</summary>
        [MenuItem("Corrigible/Lokalizasyon/Sahne ve prefab'lardaki metinlere LocalizedText ekle")]
        public static string AddLocalizedTexts()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = PrefabUtility.LoadPrefabContents(path);
                int added = AddTo(go);
                if (added > 0) PrefabUtility.SaveAsPrefabAsset(go, path);
                PrefabUtility.UnloadPrefabContents(go);
                n += added;
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                WithScene(path, scene =>
                {
                    int added = 0;
                    foreach (var root in scene.GetRootGameObjects()) added += AddTo(root);
                    if (added > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                    n += added;
                });
            }
            return $"{n} metne LocalizedText eklendi";
        }

        static readonly Regex Letters = new Regex(@"\p{L}{2,}");

        static int AddTo(GameObject root)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!Letters.IsMatch(t.text ?? "")) continue;
                var lt = t.GetComponent<LocalizedText>();
                if (lt != null && lt.Source == t.text) continue;
                // Prefab örneğinde metin üzerine yazılmışsa kaynak da örnekte üzerine yazılır
                if (lt == null) lt = t.gameObject.AddComponent<LocalizedText>();
                var so = new SerializedObject(lt);
                so.FindProperty("source").stringValue = t.text;
                so.ApplyModifiedPropertiesWithoutUndo();
                n++;
            }
            return n;
        }
    }
}
