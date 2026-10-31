namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın tam ekran kâğıdı: harekât sonu raporu. Oda, sayaçlar ve operasyon raporu simülasyonu doğrudan okur; burada yalnız
    /// son raporun içeriği durur (kapıda biten kararın kâğıdı kapanana kadar bekler). Konumlar 1920×1080 tasarım pikseli.
    /// </summary>
    public sealed class DeskViewModel
    {
        /// <summary>Harekât bitti: son rapor harita pozunda, karartılmış haritanın üstünde açılır.</summary>
        public FinalReportData FinalReport;
    }
}
