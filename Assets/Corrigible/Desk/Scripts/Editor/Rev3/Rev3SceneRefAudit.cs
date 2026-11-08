using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Sahne referans denetimi (Revizyon 3 IMPLEMENTATION_MAP › Risk R8). Silinen nesneye işaret eden alan kayıttan sonra
    /// fileID 0 olur ve gözden kaçar. <see cref="Snapshot"/> sahnedeki her bileşenin dolu nesne referanslarını
    /// (yol | bileşen | özellik) yazar; <see cref="Compare"/> sonra boşalanları listeler. Kasıtlı boşaltılanlar
    /// (ör. eski Revizyon 2 parçası) raporda görünür; onları elle teyit et.
    /// </summary>
    public static class Rev3SceneRefAudit
    {
        /// <summary>Yol; aynı adlı kardeşlerde sıra eki (ör. “Wall[2]”), anahtarlar çakışmasın.</summary>
        public static string PathOf(Transform t)
        {
            string name = t.name;
            var parent = t.parent;
            int same = 0, index = 0;
            if (parent != null)
                for (int i = 0; i < parent.childCount; i++)
                {
                    var c = parent.GetChild(i);
                    if (c.name != name) continue;
                    if (c == t) index = same;
                    same++;
                }
            if (same > 1) name += "[" + index + "]";
            return parent == null ? name : PathOf(parent) + "/" + name;
        }

        /// <summary>Yüklü sahnenin dolu referansları: anahtar “yol|Bileşen#sıra|özellik” → hedefin yolu.</summary>
        public static Dictionary<string, string> Collect(Scene scene)
        {
            var map = new Dictionary<string, string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var counts = new Dictionary<System.Type, int>();
                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null || c is Transform) continue;
                        var type = c.GetType();
                        counts.TryGetValue(type, out int i); counts[type] = i + 1;
                        var so = new SerializedObject(c);
                        var p = so.GetIterator();
                        while (p.NextVisible(true))
                        {
                            if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                            if (p.propertyPath == "m_Script") continue;
                            var target = p.objectReferenceValue;
                            string where = target is Component tc ? PathOf(tc.transform) + "·" + tc.GetType().Name
                                : target is GameObject go ? (go.scene.IsValid() ? PathOf(go.transform) : "asset:" + go.name)
                                : "asset:" + target.name;
                            map[$"{PathOf(t)}|{type.Name}#{i}|{p.propertyPath}"] = where;
                        }
                    }
                }
            return map;
        }

        public static string Snapshot(string file)
        {
            var map = Collect(SceneManager.GetActiveScene());
            File.WriteAllLines(file, map.OrderBy(k => k.Key, System.StringComparer.Ordinal).Select(kv => kv.Key + "\t" + kv.Value));
            return $"{map.Count} dolu referans → {file}";
        }

        /// <summary>
        /// Anlık görüntüyle karşılaştırır: bileşeni hâlâ duran ama referansı boşalan ya da hedefi değişen alanları yazar.
        /// Bileşeni silinmiş anahtarlar yalnız sayılır.
        /// </summary>
        public static string Compare(string file, string onlyUnder = null)
        {
            var before = File.ReadAllLines(file).Select(l => l.Split('\t')).ToDictionary(a => a[0], a => a.Length > 1 ? a[1] : "");
            var now = Collect(SceneManager.GetActiveScene());
            var livingComponents = new HashSet<string>(now.Keys.Select(k => k.Substring(0, k.LastIndexOf('|'))));
            // Bileşeni yaşayan ama hiçbir dolu referansı kalmayanlar için de bileşen kümesi gerekir
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var counts = new Dictionary<System.Type, int>();
                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null || c is Transform) continue;
                        counts.TryGetValue(c.GetType(), out int i); counts[c.GetType()] = i + 1;
                        livingComponents.Add($"{PathOf(t)}|{c.GetType().Name}#{i}");
                    }
                }
            var sb = new StringBuilder();
            int gone = 0, emptied = 0, moved = 0;
            foreach (var kv in before.OrderBy(k => k.Key, System.StringComparer.Ordinal))
            {
                if (onlyUnder != null && !kv.Key.StartsWith(onlyUnder)) continue;
                string comp = kv.Key.Substring(0, kv.Key.LastIndexOf('|'));
                if (!livingComponents.Contains(comp)) { gone++; continue; }
                if (!now.TryGetValue(kv.Key, out var target)) { emptied++; sb.AppendLine("BOŞALDI  " + kv.Key + "   (önce: " + kv.Value + ")"); }
                else if (target != kv.Value) { moved++; sb.AppendLine("DEĞİŞTİ  " + kv.Key + "   " + kv.Value + " → " + target); }
            }
            return $"boşalan {emptied}, hedefi değişen {moved}, bileşeni silinen anahtar {gone}\n" + sb;
        }
    }
}
