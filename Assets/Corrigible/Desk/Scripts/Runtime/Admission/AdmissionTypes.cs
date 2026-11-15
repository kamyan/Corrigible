namespace Corrigible.Desk
{
    // Revizyon 3 · Aşama 6 · kapı ve kabul (R9, R17; PLAN §1.1, §3 W0). Sayılar asset'lerde saklanabilir: yeni değer yalnız sona eklenir.
    // KOS aşama 7: eski tek adaylı kabulün araçları (büyüteç, interkom, buğu camı, talep defteri), bulgu fişleri ve talimat şeridi söküldü.

    /// <summary>Kabul tezgâhının kanadı: katlı (duvarda, KABUL şablonu odaya), iniyor, inik (çalışma yüzeyi), katlanıyor.</summary>
    public enum CounterState { Folded, Lowering, Down, Folding }

    /// <summary>
    /// Karar damgaları, Core seçimleriyle aynı sırada: <c>admit</c>, <c>restricted</c>, <c>reject</c>, <c>aid</c>. Sahnede yalnız KABUL (0) ve
    /// GERİ ÇEVİR (2) durur; KISITLI ve YARDIM eski tek adaylı kabule aitti (aşama 7'de söküldü). Sayılar sahnede
    /// (<c>StampDecision.choice</c>) ve <see cref="AdmissionSnapshot.Stamps"/> dizininde kullanıldığı için korunur.
    /// </summary>
    public enum StampChoice { Admit, Restricted, Reject, Aid }

    /// <summary>
    /// Kabul görünümlerinin ses olayları (görünümler ses çalmaz, olay yayar; <c>SiginakAdmission.CueOf</c> Cue'ya eşler, PLAN §1.13).
    /// Hiçbir yerde sayıyla saklanmaz.
    /// </summary>
    public enum AdmissionSound
    {
        CounterDown, Drawer, StampLift, CounterFold, StampPress, StampReturn, Deny, DocDrop, Knock, Open, Close,
    }

    /// <summary>
    /// Tıklanabilen (okunabilen) kâğıt türleri. KOS aşama 2: aday kartı ve grup fişi. İlk beş değer (kimlik belgesi, talimat şeridi, bulgu
    /// fişi, talep yaprağı, karbon kopya) eski tek adaylı kabule aitti (aşama 7'de söküldü; sayılar korunur, üretilmez).
    /// </summary>
    public enum AdmissionPaper { Document, Strip, Slip, Request, Carbon, Candidate, GroupSlip }

    /// <summary>Damga sıralarının Core kimlikleri (komut bağımsız değişkenleri; metin değil).</summary>
    public static class AdmissionIds
    {
        static readonly string[] Choices = { "admit", "restricted", "reject", "aid" };

        /// <summary>Seçimin Core kimliği (iz rengi anahtarı <c>admission.stampMarks.{kimlik}</c>).</summary>
        public static string ChoiceOf(StampChoice choice) => Choices[(int)choice];

        public static bool TryChoice(string choice, out StampChoice stamp)
        {
            int i = System.Array.IndexOf(Choices, choice);
            stamp = i >= 0 ? (StampChoice)i : StampChoice.Admit;
            return i >= 0;
        }
    }
}
