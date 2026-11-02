namespace Corrigible.Desk
{
    /// <summary>Üzerine gelme etiketinin içeriği (<see cref="HoverTag"/>): gösterilecek (çevrilmiş) başlık ve satır.</summary>
    public interface IHoverSource
    {
        string HoverTitle { get; }
        string Line { get; }
    }
}
