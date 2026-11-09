namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın oyun yüzeyi (sığınak odası): klavyenin oda kısayolları buraya gider. Kamera pozlarını (2, 3, Home, F)
    /// <see cref="SiginakCameraRig"/> kendisi işler.
    /// </summary>
    public interface IDeskSurface
    {
        /// <summary>Hikâye, baskına geçiş, açık kâğıt ya da modal varken oda girdisi kapalı.</summary>
        bool InputBlocked { get; }
        /// <summary>Harita tıklaması veya 3 kısayolu: harita pozu ve görev fişi.</summary>
        void ShowPlanning();
        /// <summary>Emir belgesi (okuma kipi): EVRAK tepsisi, rapor yokken.</summary>
        void OpenGuide();
        /// <summary>Esc: açık kâğıdı kapatır, yoksa ayarlar.</summary>
        void Escape();
    }
}
