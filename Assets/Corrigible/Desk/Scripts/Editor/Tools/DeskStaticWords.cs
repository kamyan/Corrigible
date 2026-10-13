using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Prefab ve sahnelerdeki <see cref="DeskLocText"/> kaynak metinlerini <c>DeskStaticWords.cs</c>'e <c>Loc.N(...)</c> olarak yazar;
    /// böylece projenin çeviri aracı (yalnızca .cs'deki Loc.N'leri toplar) bu sabit metinleri de tabloya alır.
    /// Prefab'da sabit metin eklendikçe yeniden çalıştırılır.
    /// </summary>
    public static class DeskStaticWords
    {
        const string OutPath = DeskSpriteManifest.Root + "/Scripts/Runtime/Localization/DeskStaticWords.cs";

        [MenuItem("Corrigible/Desk UI/Sabit metinleri topla (DeskLocText)")]
        public static string Build()
        {
            var words = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { DeskSpriteManifest.Root }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var t in go.GetComponentsInChildren<DeskLocText>(true))
                {
                    var src = new SerializedObject(t).FindProperty("source").stringValue;
                    if (string.IsNullOrEmpty(src)) src = t.GetComponent<TMPro.TMP_Text>()?.text;
                    if (!string.IsNullOrWhiteSpace(src)) words.Add(src);
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine("using Corrigible.Core;");
            sb.AppendLine();
            sb.AppendLine("namespace Corrigible.Desk");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>OTOMATİK (Corrigible/Desk UI/Sabit metinleri topla): prefab'lardaki DeskLocText kaynakları; çeviri aracı Loc.N'leri toplar.</summary>");
            sb.AppendLine("    internal static class DeskStaticWords");
            sb.AppendLine("    {");
            sb.AppendLine("        internal static readonly string[] Words =");
            sb.AppendLine("        {");
            foreach (var w in words) sb.AppendLine("            Loc.N(\"" + w.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"),");
            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            File.WriteAllText(OutPath, sb.ToString());
            AssetDatabase.ImportAsset(OutPath);
            return words.Count + " metin";
        }
    }
}
