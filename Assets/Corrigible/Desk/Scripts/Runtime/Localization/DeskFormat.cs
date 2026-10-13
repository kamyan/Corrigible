using System;
using System.Globalization;
using System.Text;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sayı ve kod biçimleri (PROMPT §6.7). İşletim sistemi kültüründen bağımsız ve deterministik:
    /// TR “47,3”, “%37,2”, “G-074”; EN “47.3”, “37.2%”, “D-074”. Eksi işareti her zaman U+2212.
    /// </summary>
    public static class DeskFormat
    {
        public const char Minus = '−';
        public const char Approx = '≈';

        static readonly NumberFormatInfo TrNum = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = ".", NegativeSign = "-" };
        static readonly NumberFormatInfo EnNum = new NumberFormatInfo { NumberDecimalSeparator = ".", NumberGroupSeparator = ",", NegativeSign = "-" };

        public static DeskLang Lang => DeskLoc.Instance.Lang;

        static NumberFormatInfo Nf(DeskLang lang) => lang == DeskLang.En ? EnNum : TrNum;

        static string Fixed(double v, int decimals, DeskLang lang)
        {
            // -0,0 yazılmasın: yuvarlanmış değer sıfırsa işaretsiz
            double r = Math.Round(v, decimals, MidpointRounding.AwayFromZero);
            if (r == 0) r = 0;
            var s = Math.Abs(r).ToString("F" + decimals, Nf(lang));
            return r < 0 ? Minus + s : s;
        }

        /// <summary>Ondalık sayı: 47,3 / 47.3; negatifte U+2212.</summary>
        public static string Number(double v, int decimals, DeskLang lang) => Fixed(v, decimals, lang);
        public static string Number(double v, int decimals = 1) => Number(v, decimals, Lang);

        /// <summary>İşaretli değişim: +0,7 / −6,4 / 0,0.</summary>
        public static string Signed(double v, int decimals, DeskLang lang)
        {
            var s = Fixed(v, decimals, lang);
            return s[0] == Minus || Math.Round(v, decimals, MidpointRounding.AwayFromZero) == 0 ? s : "+" + s;
        }
        public static string Signed(double v, int decimals = 1) => Signed(v, decimals, Lang);

        /// <summary>Yüzde: TR “%37,2”, EN “37.2%”.</summary>
        public static string Percent(double v, int decimals, DeskLang lang)
        {
            var s = Fixed(v, decimals, lang);
            if (lang == DeskLang.En) return s + "%";
            // TR: işaret yüzde işaretinden önce gelir (−%6)
            return s[0] == Minus ? Minus + "%" + s.Substring(1) : "%" + s;
        }
        public static string Percent(double v, int decimals = 0) => Percent(v, decimals, Lang);

        /// <summary>Yaklaşık (RAPOR) değer: “≈41,8”.</summary>
        public static string Approximate(string formatted) => Approx + formatted;

        /// <summary>Gün kodu: TR “G-074”, EN “D-074”.</summary>
        public static string DayCode(int day, DeskLang lang) => (lang == DeskLang.En ? "D-" : "G-") + day.ToString("000", CultureInfo.InvariantCulture);
        public static string DayCode(int day) => DayCode(day, Lang);

        /// <summary>Tamsayı (binlik ayırıcısız): 118, 2000.</summary>
        public static string Int(int v) => v < 0 ? Minus + (-v).ToString(CultureInfo.InvariantCulture) : v.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Türkçe büyük/küçük harf (PROMPT §6.5). Dizeler tabloda zaten doğru harfle durur; dönüşüm gerekirse yalnızca bu.
    /// TMP <c>FontStyles.UpperCase</c> sabit kültür kullanır (i → I), kullanılmaz.
    /// </summary>
    public static class DeskText
    {
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        public static string Upper(string s, DeskLang lang) => s?.ToUpper(lang == DeskLang.Tr ? Tr : En);
        public static string Upper(string s) => Upper(s, DeskFormat.Lang);
        public static string Lower(string s, DeskLang lang) => s?.ToLower(lang == DeskLang.Tr ? Tr : En);

        /// <summary>İlk harfi büyüt; birleşik nokta (U+0307) üretmez (“şebekeyi” → “Şebekeyi”).</summary>
        public static string Capitalize(string s, DeskLang lang)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            sb.Append(Upper(s.Substring(0, 1), lang));
            sb.Append(s, 1, s.Length - 1);
            return sb.ToString();
        }
    }
}
