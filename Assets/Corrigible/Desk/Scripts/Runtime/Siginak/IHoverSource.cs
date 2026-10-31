namespace Corrigible.Desk
{
    /// <summary>Üzerine gelme etiketinin içeriği (<see cref="HoverTag"/>): gösterilecek (çevrilmiş) başlık, kısayol, satır.</summary>
    public interface IHoverSource
    {
        string HoverTitle { get; }
        string KeyHint { get; }
        string Line { get; }
    }
}
