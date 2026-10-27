namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın oyun yüzeyi (sığınak odası): klavyenin oda kısayolları buraya gider. Kamera pozlarını (1, 2, 3, Home, F)
    /// <see cref="SiginakCameraRig"/> kendisi işler.
    /// </summary>
    public interface IDeskSurface
    {
        /// <summary>Hikâye, baskına geçiş, açık kâğıt ya da modal varken oda girdisi kapalı.</summary>
        bool InputBlocked { get; }
        /// <summary>1 ve 3: harita pozu ve görev fişi.</summary>
        void ShowPlanning();
        /// <summary>H: emir belgesi (okuma kipi).</summary>
        void OpenGuide();
        /// <summary>Esc: açık kâğıdı kapatır, yoksa ayarlar.</summary>
        void Escape();
    }
}
