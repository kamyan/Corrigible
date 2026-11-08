using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üzerine gelme etiketinin isteğe bağlı çerçeve rengi (<see cref="HoverTag"/>): bırakma önizlemesi mavi, uyarıda amber
    /// çerçeve ister. <c>null</c> çerçeveyi gizler; bu arayüzü taşımayan kaynaklarda çerçeve hiç görünmez.
    /// </summary>
    public interface IHoverFrame
    {
        Color? Frame { get; }
    }
}
