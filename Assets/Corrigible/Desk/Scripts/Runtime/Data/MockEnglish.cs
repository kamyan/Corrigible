using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sahte verinin İngilizcesi (F7, yalnızca geliştirme/QA): oyunda metni veri kaynağı seçili dilde verir; sahte kaynak da
    /// kurulan görünüm modelini İngilizceye çevirir. Sözlük <c>Data/Mock/mock_en.txt</c> (TR ⇥ EN; <c>re:</c> ile başlayan
    /// satırlar düzenli ifade). Ardından sayı ve gün kodu EN biçimine geçer (47,3 → 47.3, %38 → 38%, G-074 → D-074).
    /// Kimlik, güven, durum, renk, tuş gibi alanlara dokunulmaz. Oyunun çeviri tablosunu şişirmez.
    /// </summary>
    public sealed class MockEnglish
    {
        static readonly HashSet<string> SkipFields = new HashSet<string>
        {
            "Id", "RegionId", "Region", "Trust", "State", "Icon", "Color", "TitleColor", "LabelColor", "LabelSide", "LabelAlign", "Kind",
            "Key", "Keys", "Style", "Type", "Command", "Screen", "Code", "RingVariant", "SelectedRegion", "Sprite", "Dir", "Status", "Speed",
            "Speeds", "From", "To", "Side", "FundTrust",
        };

        readonly Dictionary<string, string> exact = new Dictionary<string, string>();
        readonly List<(Regex re, string en)> patterns = new List<(Regex, string)>();
        readonly HashSet<string> misses = new HashSet<string>();

        static readonly Regex Decimal = new Regex(@"(?<=\d),(?=\d)");
        static readonly Regex Percent = new Regex(@"%(\d+(?:\.\d+)?)");
        static readonly Regex Day = new Regex(@"\bG-(\d{3})\b");

        public IReadOnlyCollection<string> Misses => misses;

        public MockEnglish(string tsv)
        {
            foreach (var raw in tsv.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                string tr = Unescape(line.Substring(0, tab)), en = Unescape(line.Substring(tab + 1));
                if (tr.StartsWith("re:")) patterns.Add((new Regex("^" + tr.Substring(3) + "$"), en));
                else exact[tr] = en;
            }
        }

        static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t");

        public string Translate(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (exact.TryGetValue(s, out var en)) return en;
            foreach (var (re, rep) in patterns)
                if (re.IsMatch(s))
                {
                    var r = Format(re.Replace(s, rep));
                    if (LooksTurkish(r)) misses.Add(s);   // genel kalıp yakaladı ama Türkçe kelime kaldı
                    return r;
                }
            if (Regex.IsMatch(s, "[a-zçğıöşüA-ZÇĞİÖŞÜ]{3}") && !IsEnglishAlready(s)) misses.Add(s);
            return Format(s);
        }

        static readonly Regex Tags = new Regex("<[^>]*>");
        static readonly Regex TurkishLetters = new Regex("[çğıöşüÇĞİÖŞÜ]");
        // Kesme işaretinden sonra gelen parça (I've, it's) kelime sayılmaz
        static readonly Regex TurkishWords = new Regex(@"(?<!['’])\b(gün|fon|gider|gelir|bölge|yok|ve|ile|proje|güven|kaldı|sonra)\b", RegexOptions.IgnoreCase);

        /// <summary>EN metinde Türkçe kalıntı (harf ya da sık kelime); biçim etiketleri yok sayılır. Metin denetimi de kullanır.</summary>
        public static bool LooksTurkish(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = Tags.Replace(s, "");
            return TurkishLetters.IsMatch(s) || TurkishWords.IsMatch(s);
        }

        /// <summary>EN sayı ve gün biçimi.</summary>
        public static string Format(string s)
        {
            s = Decimal.Replace(s, ".");
            s = Percent.Replace(s, "$1%");
            s = Day.Replace(s, "D-$1");
            return s;
        }

        static bool IsEnglishAlready(string s) => !Regex.IsMatch(s, "[çğıöşüÇĞİÖŞÜ]") && Regex.IsMatch(s, @"\b(the|and|of|to|day|days|region|team|report)\b", RegexOptions.IgnoreCase);

        /// <summary>Görünüm modelinin bütün metin alanlarını (iç içe nesneler, listeler, sözlükler) çevirir.</summary>
        public void Apply(object root)
        {
            visited.Clear();
            Walk(root, 0);
        }

        // Paylaşılan nesneler (ör. teleks ve Haberler aynı öğeleri kullanır) bir kez çevrilir
        readonly HashSet<object> visited = new HashSet<object>(RefEq.Instance);

        void Walk(object o, int depth)
        {
            if (o == null || depth > 12 || !visited.Add(o)) return;
            var type = o.GetType();
            if (type.Namespace != typeof(MockEnglish).Namespace) return;
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (SkipFields.Contains(f.Name)) continue;
                var v = f.GetValue(o);
                switch (v)
                {
                    case null: break;
                    case string s: f.SetValue(o, Translate(s)); break;
                    case List<string> ls:
                        for (int i = 0; i < ls.Count; i++) ls[i] = Translate(ls[i]);
                        break;
                    case IDictionary dict:
                        foreach (var k in dict.Keys) Walk(dict[k], depth + 1);
                        break;
                    case IList list:
                        foreach (var item in list) Walk(item, depth + 1);
                        break;
                    default:
                        if (!f.FieldType.IsPrimitive && !f.FieldType.IsArray && !f.FieldType.IsValueType) Walk(v, depth + 1);
                        break;
                }
            }
        }

        /// <summary>Geliştirici: sözlükte olmayan metinler (sekmeyle ayrılmış iskelet).</summary>
        public string DumpMisses()
        {
            var sb = new StringBuilder();
            foreach (var m in misses) sb.Append(m.Replace("\n", "\\n")).Append('\t').Append('\n');
            return sb.ToString();
        }
    }

    sealed class RefEq : IEqualityComparer<object>
    {
        public static readonly RefEq Instance = new RefEq();
        public new bool Equals(object a, object b) => ReferenceEquals(a, b);
        public int GetHashCode(object o) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);
    }
}
