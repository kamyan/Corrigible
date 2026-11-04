namespace Corrigible.Desk
{
    /// <summary>Emir belgesinin (K7) gösterime hazır metinleri; <see cref="BriefingView"/> yalnız bunu çizer.</summary>
    public sealed class BriefingData
    {
        public string Agency, Number, Title, ToLabel, To, ReLabel, Re, Stamp, Received;
        /// <summary>Gövde paragrafları (CP 400 20, satır 1,5) ve kalın son satır.</summary>
        public string[] Paragraphs = new string[0];
        public string Closing;
        public string SignOffice, SignLine;
        public string Action;
        /// <summary>Okuma kipi (H / EVRAK tepsisi): manila dosya ve KAYITTAN DEVAM sekmesi yok, düğme “Kapat”.</summary>
        public bool Reading;
        /// <summary>Menüden yeni harekât: manila dosya ve sekme yok (kayıt kararı kapıda verildi), düğme BAŞLA.</summary>
        public bool NoDossier;
        /// <summary>Kayıt var: sekme etkin, etikette vardiya ve özgür bölge; yoksa soluk sekme + “KAYIT YOK”.</summary>
        public bool HasSave;
        public string TabTitle, TabSummary, TabHint, NoSave;
    }
}
