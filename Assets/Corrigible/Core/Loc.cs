using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Corrigible.Core
{
    /// <summary>
    /// Metin çevirisi (gettext tarzı). Kaynak dil Türkçe: kodda metin Türkçe yazılır ve anahtar olarak kullanılır,
    /// çeviri tablosu (<c>Resources/Localization/strings.txt</c>) kaynağı diğer dillere eşler. Tabloda olmayan metin
    /// kaynak hâliyle gösterilir ve <see cref="Missing"/> olayı tetiklenir.
    /// <para><c>T</c>: düz metin. <c>F</c>: biçimli metin (<c>{0}</c> yer tutucuları, dilin sayı biçimiyle).
    /// <c>N</c>: çevirmeden işaretler (statik veride; gösterirken <c>T</c> ile çevrilir). Üçünün de ilk argümanı bir
    /// dizgi sabiti olmalı ki <c>LocalizationTool</c> metni toplayabilsin.</para>
    /// Simülasyon dili hiçbir kararda kullanmaz: dil değişse de RNG ve oyun aynı kalır.
    /// </summary>
    public static class Loc
    {
        public const string Turkish = "tr", English = "en";
        public static readonly string[] Languages = { Turkish, English };

        public static string Lang { get; private set; } = Turkish;
        public static CultureInfo Culture { get; private set; } = CultureFor(Turkish);
        public static bool IsSource => Lang == Turkish;

        /// <summary>Tabloda bulunamayan kaynak metin (geliştirme sırasında uyarı için).</summary>
        public static event Action<string> Missing;

        static Dictionary<string, string> table = new Dictionary<string, string>();

        /// <summary>Dili ve o dilin tablosunu (kaynak → çeviri) ayarlar. Türkçe için tablo gerekmez.</summary>
        public static void SetLanguage(string lang, IDictionary<string, string> translations = null)
        {
            Lang = Array.IndexOf(Languages, lang) >= 0 ? lang : Turkish;
            Culture = CultureFor(Lang);
            table = translations != null && !IsSource ? new Dictionary<string, string>(translations) : new Dictionary<string, string>();
        }

        public static string T(string source)
        {
            if (IsSource || string.IsNullOrEmpty(source)) return source;
            if (table.TryGetValue(source, out var t) && t.Length > 0) return t;
            Missing?.Invoke(source);
            return source;
        }

        public static string F(string source, params object[] args) => string.Format(Culture, T(source), args);

        /// <summary>Çevirmeden işaretler (statik veride kullanılır; gösterimde <see cref="T"/> ile çevrilir).</summary>
        public static string N(string source) => source;

        /// <summary>Sayı biçimi dile göre ("1,5" / "1.5").</summary>
        public static string Num(double v, string format = "0.#") => v.ToString(format, Culture);

        static CultureInfo CultureFor(string lang)
        {
            // Yalnızca sayı biçimi için: tr-TR'nin büyük/küçük harf kuralları burada kullanılmaz.
            var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            if (lang == Turkish) { c.NumberFormat.NumberDecimalSeparator = ","; c.NumberFormat.NumberGroupSeparator = "."; }
            return c;
        }

        // ---------------------------------------------------------------- tablo dosyası
        // Biçim: sekmeyle ayrılmış, ilk satır dil kodları ("tr<TAB>en"). Hücrelerde \n, \t ve \\ kaçış dizileridir.
        // '#' ile başlayan satırlar yorumdur.

        /// <summary>Tablo metnini okur: dil → (kaynak → çeviri).</summary>
        public static Dictionary<string, Dictionary<string, string>> ParseTable(string text)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            string[] header = null;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                var cells = line.Split('\t');
                if (header == null)
                {
                    header = cells;
                    for (int i = 1; i < header.Length; i++) result[header[i]] = new Dictionary<string, string>();
                    continue;
                }
                var src = Unescape(cells[0]);
                for (int i = 1; i < header.Length && i < cells.Length; i++)
                    if (cells[i].Length > 0) result[header[i]][src] = Unescape(cells[i]);
            }
            return result;
        }

        public static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "");

        public static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    sb.Append(n == 'n' ? '\n' : n == 't' ? '\t' : n);
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
