using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Tasarlanmamış pencereler için ortak liste düzeni (açık karar 3, 7): klasör + krem sayfa içinde kaydırılabilir
    /// (RectMask2D + 4 px mürekkep kaydırma çubuğu) dikey satır listesi. Satırlar şablondan havuzlanır.
    /// </summary>
    public static class DeskList
    {
        /// <summary>Satırları üstten alta dizer ve içerik yüksekliğini ayarlar; satır yüksekliği <paramref name="heightOf"/>'tan.</summary>
        public static void Stack<T>(DeskPool<T> pool, int count, RectTransform content, float gap, System.Func<T, float> heightOf) where T : Component
        {
            float y = 0;
            for (int i = 0; i < count; i++)
            {
                var row = pool.Get(i);
                var rt = (RectTransform)row.transform;
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -y);
                float h = heightOf(row);
                rt.sizeDelta = new Vector2(0, h);
                y += h + gap;
            }
            pool.Trim(count);
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(0, y - gap));
        }
    }
}
