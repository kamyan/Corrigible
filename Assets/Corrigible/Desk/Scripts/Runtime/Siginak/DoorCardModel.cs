using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kapı kâğıdındaki bir seçenek satırı (Revizyon 2 · S4): kimlik, yazı, sağda Courier bedel/etki ya da gerekçe, hâl.
    /// Normal satır kâğıt beyazı (üzerine gelince mürekkep); devre dışı satır kesik kenarlı, gri ve nedeni sağda; yetersiz
    /// kaynakta satır açık kalır, bedel üstü çizik ve “erzak yetersiz” yazar; kullanılmış satır (bakıldı, soruldu) gri ve
    /// damgalı, seçilmez.
    /// </summary>
    public sealed class DoorButtonData
    {
        public string Id, Label, Detail;
        public SlipButtonState State;
        /// <summary>Kullanılmış seçeneğin damgası (“BAKILDI”); yoksa null.</summary>
        public string Used;
        public bool Enabled => State != SlipButtonState.Disabled && Used == null;
    }

    /// <summary>Gözetleme deliğinden görülen ya da interkomdan duyulan: başlık (GÖZETLEME DELİĞİ) ve metin.</summary>
    public sealed class DoorObservation
    {
        public string Label, Text;
        /// <summary>İnterkom cevabı daktiloyla harf harf yazılır; gözetleme bloğu bir anda dolar.</summary>
        public bool Intercom;
    }

    /// <summary>
    /// Kapı kâğıdının gösterime hazır hâli (Revizyon 2 · S4): <see cref="DoorCardView"/> yalnız bunu çizer. Kapalı ve açık
    /// kapıda seçenek satırları ve Esc satırı; sonuçta seçim satırı, metin, (ölümcülde) çift çizgili damga ve son cümle,
    /// altında tek geniş düğme (Enter).
    /// </summary>
    public sealed class DoorCardData
    {
        public string Head, Code, Title;
        /// <summary>Sonuçta yapılan seçim: “SEÇİM · 3 KAPIYI AÇ”; yoksa null.</summary>
        public string Choice;
        public string Body;
        public readonly List<DoorObservation> Observations = new List<DoorObservation>();
        /// <summary>Harekâtı bitiren kapı (VURULDU, SIZMA): oda ve lamba kırmızı, kâğıt −1,4°, yalnız Enter.</summary>
        public bool Fatal;
        /// <summary>Kırmızı çift çizgili damga ve altındaki son cümle (yalnız ölümcülde); yoksa null.</summary>
        public string Stamp, Epilogue;
        public readonly List<DoorButtonData> Buttons = new List<DoorButtonData>();
        /// <summary>Sonuçtaki tek geniş düğme (Enter): kimlik ve yazı; yoksa null.</summary>
        public string ActionId, Action;
        /// <summary>Kısayol satırı: tuş (“Esc”) ve açıklama; yoksa null.</summary>
        public string HintKey, Hint;
    }
}
