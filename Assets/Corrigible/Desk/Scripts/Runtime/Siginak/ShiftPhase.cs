namespace Corrigible.Desk
{
    /// <summary>
    /// Vardiya şalterinin görünür durumu (Revizyon 3 · R19a, token <c>shiftClose.states</c>). Yalnız R19 kipinde
    /// (<c>ShiftSwitch.shiftClose</c>) anlamlıdır; eski <see cref="ShiftSwitch.State"/> korunur, bu ondan ve bayraklardan türetilir.
    /// <list type="bullet">
    /// <item><see cref="Ready"/>: kapak kapalı (0°), kol yukarıda (+35°), hazır lambası yanık; plaka “kapağı kaldır”.</item>
    /// <item><see cref="CoverUp"/>: kapak kalkık (112°), kol yukarıda; plaka “kolu indir”; 4 s ikinci tık gelmezse kapak kapanır.</item>
    /// <item><see cref="Waiting"/>: kol aşağıda (−35°), kapak açık, lamba sönük, tıklanmaz; plaka kırmızı “fişi EVRAK’a koy”.</item>
    /// <item><see cref="Locked"/>: kapak kapalı, KİLİTLİ bandı görünür, lamba sönük; tık yalnız 2 mm titretir; plaka kısa nedeni yazar.</item>
    /// <item><see cref="Rejecting"/>: basım reddedildi; kol inip 0,4 s'de geri kalkar, plaka “fiş basılamadı”.</item>
    /// </list>
    /// </summary>
    public enum ShiftPhase { Ready, CoverUp, Waiting, Locked, Rejecting }
}
