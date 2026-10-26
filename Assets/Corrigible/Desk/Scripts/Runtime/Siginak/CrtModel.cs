namespace Corrigible.Desk
{
    /// <summary>CRT ekranının hâli (K5 notu › durumlar): bekleniyor · kuruluyor · yayın açık · kesildi · devre dışı (ekip yaralı).</summary>
    public enum CrtState { Waiting, Establishing, Live, Cut, Disabled }

    /// <summary>CRT'deki bekleme ekranı (K5a). Metinler gösterilecek hâlde; fosfor tek renk, durum biçimle ayrılır.</summary>
    public sealed class CrtScreenData
    {
        public CrtState State;
        public string Channel, Title, Sub, TeamLine, FrontLine, Hint;
        /// <summary>Ekip satırı ters video (fosfor zemin, koyu yazı): yaralı ekip.</summary>
        public bool TeamInverse;
    }

    /// <summary>Baskın geçiş karesi (K5b) metinleri.</summary>
    public sealed class CrtTransitionData
    {
        public string Channel, Code, Title, Sync, Target, Camera, SkipKey, SkipWord, Footer, CutTitle, CutSub;
    }
}
