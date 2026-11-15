using System;

namespace Corrigible.Core
{
    /// <summary>Sığınak olaylarının statik verisi: kimlikler kayıtta ve günlükte saklanır, metinler gösterimde çevrilir.</summary>
    public static class ShelterEventData
    {
        /// <summary>Tablo sırası (tasarım belgesi §3).</summary>
        public static readonly string[] Kinds =
        {
            "iz", "ikinci_iz", "sabotaj", "buyuk_hirsizlik", "kacak", "erzak_azaliyor", "hirsizlik",
            "agir_yarali", "kavga", "anma", "karartma", "takas", "cagri", "konvoy",
        };

        /// <summary>Sızma zinciri: aralık bütçesinin dışındadır.</summary>
        public static readonly string[] Chain = { "iz", "ikinci_iz", "sabotaj" };

        /// <summary>
        /// Olağan olaylar (aralık bütçesine uyar), öncelik sırasıyla: hayatta kalma, kişiler, fırsatlar; aynı öncelikte tablo
        /// sırası. Büyük hırsızlık vadeli bir sonuçtur ama zincir değildir: aralığı bekler ve yeniden başlatır.
        /// </summary>
        public static readonly string[] Ordinary =
        {
            "kacak", "erzak_azaliyor", "karartma",
            "buyuk_hirsizlik", "hirsizlik", "agir_yarali", "kavga", "anma",
            "takas", "cagri", "konvoy",
        };

        public static bool IsChain(string kind) => Array.IndexOf(Chain, kind) >= 0;
        public static bool IsOrdinary(string kind) => Array.IndexOf(Ordinary, kind) >= 0;

        /// <summary>Öncelik: 0 zincir, 1 hayatta kalma, 2 kişiler, 3 fırsatlar.</summary>
        public static int Priority(string kind)
        {
            switch (kind)
            {
                case "iz": case "ikinci_iz": case "sabotaj": return 0;
                case "kacak": case "erzak_azaliyor": case "karartma": return 1;
                case "buyuk_hirsizlik": case "hirsizlik": case "agir_yarali": case "kavga": case "anma": return 2;
                default: return 3;
            }
        }

        /// <summary>Hedefi sığınakta ve canlı olmak zorunda olan olaylar (anmanın hedefi ölüdür).</summary>
        public static bool NeedsLivingTarget(string kind)
        {
            switch (kind)
            {
                case "iz": case "ikinci_iz": case "sabotaj": case "buyuk_hirsizlik":
                case "kacak": case "hirsizlik": case "agir_yarali": case "kavga": return true;
                default: return false;
            }
        }

        /// <summary>Seçenek kimlikleri, kâğıttaki sırayla.</summary>
        public static string[] Options(string kind)
        {
            switch (kind)
            {
                case "iz": case "ikinci_iz": return new[] { "tecrit", "yolla", "yoksay" };
                case "sabotaj": return new[] { "sok", "sorgula" };
                case "buyuk_hirsizlik": return new[] { "yolla", "kisitla" };
                case "kacak": return new[] { "ikna", "birak", "kilitle" };
                case "erzak_azaliyor": return new[] { "yarim", "tam" };
                case "hirsizlik": return new[] { "yolla", "affet", "kisitla" };
                case "agir_yarali": return new[] { "kaldir", "beklet" };
                case "kavga": return new[] { "ayir", "karisma" };
                case "anma": return new[] { "an", "devam" };
                case "karartma": return new[] { "karart", "devam" };
                case "takas": return new[] { "metal", "erzak", "reddet" };
                case "cagri": return new[] { "gonder", "gonderme" };
                case "konvoy": return new[] { "kabul", "reddet" };
                default: return new string[0];
            }
        }

        /// <summary>
        /// Güvenli seçenek: kararda son üretmez; tek istisna dışında hiçbir koşulda kapanmaz. İstisna: ortaya çıkan sızmacı son canlı
        /// kişiyse "sok" kapalı, "sorgula" açıktır (iki seçenek de onu çıkarır; sığınak zaten yalnız ondan ibarettir). Kişi
        /// çıkarabilen öteki seçenekler ("yolla", "birak", bulguyla çıkarabilen "tecrit") son kişide kapalıdır.
        /// </summary>
        public static string SafeOption(string kind)
        {
            switch (kind)
            {
                case "iz": case "ikinci_iz": return "yoksay";
                case "sabotaj": return "sok";
                case "buyuk_hirsizlik": return "kisitla";
                case "kacak": return "ikna";
                case "erzak_azaliyor": return "tam";
                case "hirsizlik": return "affet";
                case "agir_yarali": return "beklet";
                case "kavga": return "karisma";
                case "anma": case "karartma": return "devam";
                case "takas": case "konvoy": return "reddet";
                case "cagri": return "gonderme";
                default: return null;
            }
        }

        public static string Title(string kind)
        {
            switch (kind)
            {
                case "iz": return Loc.T("İZ");
                case "ikinci_iz": return Loc.T("İKİNCİ İZ");
                case "sabotaj": return Loc.T("SABOTAJ");
                case "buyuk_hirsizlik": return Loc.T("BÜYÜK HIRSIZLIK");
                case "kacak": return Loc.T("KAÇAK");
                case "erzak_azaliyor": return Loc.T("ERZAK AZALIYOR");
                case "hirsizlik": return Loc.T("HIRSIZLIK");
                case "agir_yarali": return Loc.T("AĞIR YARALI");
                case "kavga": return Loc.T("KAVGA");
                case "anma": return Loc.T("ANMA");
                case "karartma": return Loc.T("KARARTMA");
                case "takas": return Loc.T("TAKAS");
                case "cagri": return Loc.T("ÇAĞRI");
                case "konvoy": return Loc.T("KONVOY");
                default: return kind;
            }
        }

        /// <summary>Seçeneğin adı; aynı kimlik ("devam", "reddet", "yolla") olaya göre aynı ya da farklı ad taşıyabilir.</summary>
        public static string OptionName(string kind, string option)
        {
            switch (option)
            {
                case "tecrit": return Loc.T("Tecrit et");
                case "yolla": return Loc.T("Yolla");
                case "yoksay": return Loc.T("Görmezden gel");
                case "sok": return Loc.T("Sök");
                case "sorgula": return Loc.T("Sorgula");
                case "kisitla": return Loc.T("Kısıtla");
                case "affet": return Loc.T("Affet");
                case "ikna": return Loc.T("İkna et");
                case "birak": return Loc.T("Bırak");
                case "kilitle": return Loc.T("Kapıyı kilitle");
                case "yarim": return Loc.T("Yarım tayın");
                case "tam": return Loc.T("Tam tayın");
                case "kaldir": return Loc.T("Erzakla ayağa kaldır");
                case "beklet": return Loc.T("Beklet");
                case "ayir": return Loc.T("Ayır");
                case "karisma": return Loc.T("Karışma");
                case "an": return Loc.T("Anma yap");
                case "devam": return kind == "anma" ? Loc.T("İşe devam") : Loc.T("Çalışmaya devam");
                case "karart": return Loc.T("Karart");
                case "metal": return Loc.T("Metal ver");
                case "erzak": return Loc.T("Erzak ver");
                case "reddet": return Loc.T("Teklifi geri çevir");
                case "gonder": return Loc.T("Erzak yolla");
                case "gonderme": return Loc.T("Yollama");
                case "kabul": return Loc.T("Kabul et");
                default: return option;
            }
        }
    }
}
