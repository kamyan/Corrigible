namespace Corrigible.Desk
{
    /// <summary>
    /// Rakamla yazılmış sayıya Türkçe hâl eki (“VARDİYA 015'İ”, “Vardiya 016'da”). Ek, sayının okunuşundaki son sözcüğe
    /// göre seçilir (ünlü uyumu, sert ünsüz benzeşmesi, ünlüyle bitende kaynaştırma “y”). İngilizcede ek yazılmaz.
    /// </summary>
    public static class TrNumber
    {
        static readonly string[] Ones = { "", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz" };
        static readonly string[] Tens = { "", "on", "yirmi", "otuz", "kırk", "elli", "altmış", "yetmiş", "seksen", "doksan" };

        /// <summary>Okunuşun son sözcüğü: 15 → “beş”, 20 → “yirmi”, 100 → “yüz”, 0 → “sıfır”.</summary>
        static string LastWord(long n)
        {
            if (n < 0) n = -n;
            if (n == 0) return "sıfır";
            if (n % 10 != 0) return Ones[n % 10];
            if (n / 10 % 10 != 0) return Tens[n / 10 % 10];
            if (n / 100 % 10 != 0) return "yüz";
            if (n / 1000 % 1000 != 0) return "bin";
            return "milyon";
        }

        static char LastVowel(string word)
        {
            for (int i = word.Length - 1; i >= 0; i--)
                if ("aeıioöuü".IndexOf(word[i]) >= 0) return word[i];
            return 'e';
        }

        static bool EndsWithVowel(string word) => "aeıioöuü".IndexOf(word[word.Length - 1]) >= 0;

        /// <summary>Belirtme hâli: 15 → “i”, 16 → “yı”, 19 → “u”. <paramref name="upper"/> ise “İ”, “YI”, “U”.</summary>
        public static string Accusative(long n, bool upper = false)
        {
            string w = LastWord(n);
            char v = LastVowel(w);
            string s = (EndsWithVowel(w) ? "y" : "") + (v == 'a' || v == 'ı' ? "ı" : v == 'e' || v == 'i' ? "i" : v == 'o' || v == 'u' ? "u" : "ü");
            return upper ? Upper(s) : s;
        }

        /// <summary>Bulunma hâli: 16 → “da”, 17 → “de”, 15 → “te”, 40 → “ta”.</summary>
        public static string Locative(long n, bool upper = false)
        {
            string w = LastWord(n);
            char v = LastVowel(w);
            bool hard = "çfhkpsşt".IndexOf(w[w.Length - 1]) >= 0;
            string s = (hard ? "t" : "d") + (v == 'a' || v == 'ı' || v == 'o' || v == 'u' ? "a" : "e");
            return upper ? Upper(s) : s;
        }

        /// <summary>Ayrılma hâli: 1 → “den”, 5 → “ten”, 9 → “dan”, 40 → “tan”.</summary>
        public static string Ablative(long n, bool upper = false)
        {
            string s = Locative(n) + "n";
            return upper ? Upper(s) : s;
        }

        /// <summary>
        /// Biçimli sayı + ek, gösterilen dilde: Türkçede “015'İ”, İngilizcede “015”. Çevirilerde yer tutucu aynı kalsın diye
        /// ek sayıyla tek argümandır (Loc.F("VARDİYA {0} BAŞLAT", …) → EN “START SHIFT {0}”).
        /// </summary>
        public static string WithAccusative(long n, string format = "000", bool upper = false) => With(n, format, Accusative(n, upper));

        /// <summary>Biçimli sayı + bulunma eki: Türkçede “016'da”, İngilizcede “016”.</summary>
        public static string WithLocative(long n, string format = "000", bool upper = false) => With(n, format, Locative(n, upper));

        /// <summary>Biçimli sayı + ayrılma eki: Türkçede “009’dan”, İngilizcede “009”. Kesme, belgelerdeki öteki eklerle aynı tipografik işarettir.</summary>
        public static string WithAblative(long n, string format = "000", bool upper = false) => With(n, format, Ablative(n, upper), "’");

        static string With(long n, string format, string suffix, string apostrophe = "'")
        {
            string s = n.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            return DeskLoc.Instance.Lang == DeskLang.Tr ? s + apostrophe + suffix : s;
        }

        static string Upper(string s) => s.Replace("i", "İ").Replace("ı", "I").ToUpperInvariant();
    }
}
