namespace Corrigible.Desk
{
    // Revizyon 3 · Aşama 6 · kapı ve kabul (R9, R17; PLAN §1.1, §3 W0). Sayılar asset'lerde saklanabilir: yeni değer yalnız sona eklenir.

    /// <summary>Kabul tezgâhının kanadı: katlı (duvarda, KABUL şablonu odaya), iniyor, inik (çalışma yüzeyi), katlanıyor.</summary>
    public enum CounterState { Folded, Lowering, Down, Folding }

    /// <summary>Dört inceleme aracı, Core türleriyle aynı sırada: <c>papers</c>, <c>interview</c>, <c>breath</c>, <c>record</c>.</summary>
    public enum AdmissionTool { Papers, Interview, Breath, Record }

    /// <summary>Dört karar damgası, Core seçimleriyle aynı sırada: <c>admit</c>, <c>restricted</c>, <c>reject</c>, <c>aid</c>.</summary>
    public enum StampChoice { Admit, Restricted, Reject, Aid }

    /// <summary>
    /// Bulgu fişinin sonuç damgası: EŞLEŞİYOR (yeşil), UYUŞMUYOR (kahverengi), nötr (görüşme), BEKLENİYOR (kesikli), KAYIT BULUNDU,
    /// KAYIT FARKLI, İNCELE (inceleme fişi).
    /// </summary>
    public enum SlipVerdict { Match, Mismatch, Neutral, Pending, Found, Differs, Review }

    /// <summary>
    /// Kabul görünümlerinin ses olayları (görünümler ses çalmaz, olay yayar; <c>SiginakAdmission.CueOf</c> Cue'ya eşler, PLAN §1.13).
    /// </summary>
    public enum AdmissionSound
    {
        CounterDown, Drawer, TinsOut, Marker, SlipOut, IntercomCrackle, GlassCover, Fog, PadTear, StampLift, CounterFold,
        StampPress, StampReturn, Deny, DocDrop, Knock, Open, Close,
    }

    /// <summary>
    /// Tıklanabilen (okunabilen) kâğıt türleri: kimlik/sevk belgesi, talimat şeridi, bulgu fişi, talep yaprağı, karbon kopya. (PLAN
    /// "PaperKind" der; o ad PaperPanel.cs'deki eski uGUI kâğıt türünde kullanılıyor.)
    /// </summary>
    public enum AdmissionPaper { Document, Strip, Slip, Request, Carbon }

    /// <summary>Araç ve damga sıralarının Core kimlikleri (komut bağımsız değişkenleri; metin değil).</summary>
    public static class AdmissionIds
    {
        static readonly string[] Kinds = { "papers", "interview", "breath", "record" };
        static readonly string[] Choices = { "admit", "restricted", "reject", "aid" };

        /// <summary><c>InspectApplicant</c>'ın türü.</summary>
        public static string KindOf(AdmissionTool tool) => Kinds[(int)tool];

        /// <summary><c>DecideApplicant</c>'ın seçimi.</summary>
        public static string ChoiceOf(StampChoice choice) => Choices[(int)choice];

        public static bool TryTool(string kind, out AdmissionTool tool)
        {
            int i = System.Array.IndexOf(Kinds, kind);
            tool = i >= 0 ? (AdmissionTool)i : AdmissionTool.Papers;
            return i >= 0;
        }

        public static bool TryChoice(string choice, out StampChoice stamp)
        {
            int i = System.Array.IndexOf(Choices, choice);
            stamp = i >= 0 ? (StampChoice)i : StampChoice.Admit;
            return i >= 0;
        }
    }
}
