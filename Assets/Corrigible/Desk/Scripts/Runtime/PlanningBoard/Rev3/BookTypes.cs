namespace Corrigible.Desk
{
    /// <summary>
    /// Tarif defterinin durumu (Revizyon 3 · Aşama 5, R18a; PLAN §1.4): kancada kapalı → öne geliyor (kapak açılıyor) → okuma
    /// pozunda açık → sayfa çevriliyor → [B] tarif tutulurken alçakta → kapanıyor (kancaya dönüyor).
    /// </summary>
    public enum BookState { Closed, Opening, Open, Turning, Lowered, Closing }

    /// <summary>
    /// Açık defterde bir sayfa noktasının bölgesi (PLAN §1.4 "Sayfa bölgeleri"): sağ alt köşe sekmesi (sonraki), sol alt köşe sekmesi
    /// (önceki), sol ya da sağ sayfanın tarif kutusu ([B] basılı tutup sürükleme), sayfanın geri kalanı, sayfa dışı (kapatır).
    /// </summary>
    public enum BookZone { None, Prev, Next, RecipeL, RecipeR, Page, Outside }
}
