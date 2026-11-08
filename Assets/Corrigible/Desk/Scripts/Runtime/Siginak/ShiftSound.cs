namespace Corrigible.Desk
{
    /// <summary>
    /// Vardiya kapanışının ses olayları (Revizyon 3 · R19, PLAN §1.11). Sunum (şalter, fiş, makine) yalnız olayı yayar; köprü
    /// (<c>SiginakShiftClose.CueOf</c>) bunu <c>Cue</c>'ya çevirip çalar, çünkü sunum assembly'si Game'i görmez.
    /// <list type="bullet">
    /// <item><see cref="CoverOpen"/> kapağın menteşe “tık”ı · <see cref="CoverShut"/> kapağın kapanışı.</item>
    /// <item><see cref="Lever"/> kolun “klak-klunk”u (tek klip) · <see cref="Denied"/> kilitliyken tık (“klak”).</item>
    /// <item><see cref="SlipPrint"/> yazıcının “tırrt”ı (çıkış başı) · <see cref="SlipTear"/> kopmanın “çıt”ı (dile varış).</item>
    /// <item><see cref="SlipLift"/> fiş ele gelir · <see cref="SlipReturn"/> fiş dile ya da tepsiden geri döner.</item>
    /// <item><see cref="SlipFile"/> fiş EVRAK'a düşer (“şap”) · <see cref="MouthEmpty"/> basım reddinde boş ağzın “tık”ı.</item>
    /// <item><see cref="Gong"/> yeni vardiyanın kısık “gong”u (tam karanlıkta).</item>
    /// </list>
    /// </summary>
    public enum ShiftSound { CoverOpen, CoverShut, Lever, Denied, SlipPrint, SlipTear, SlipLift, SlipReturn, SlipFile, MouthEmpty, Gong }
}
