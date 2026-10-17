using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Can noktaları: dolu ve boş noktalar yan yana (pip_full / pip_empty, 8 ya da 10 px). Renk tema anahtarı: ekip ink, robot
    /// ve hedef kırmızı; boş noktanın kenarı ayrı anahtarla (ör. sade.raid.pipAiEmpty). Noktalar prefab'da hazır (en çok
    /// <c>pips.Length</c>); çalışırken nesne yaratılmaz.
    /// </summary>
    public sealed class PipRowView : MonoBehaviour
    {
        [SerializeField] Image[] pips = System.Array.Empty<Image>();
        [SerializeField] int size = 10;
        [SerializeField] float gap = 3;
        [SerializeField] int current = 3, max = 4;
        [SerializeField] string fullKey = "sade.status.threat";
        [SerializeField] string emptyKey = "sade.status.threat";

        public float Width => max <= 0 ? 0 : max * size + (max - 1) * gap;

        void OnEnable() => Apply();
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(int value, int maximum, string full = null, string empty = null)
        {
            current = value; max = Mathf.Min(maximum, pips.Length);
            if (full != null) fullKey = full;
            if (empty != null) emptyKey = empty;
            Apply();
        }

        public void Apply()
        {
            for (int i = 0; i < pips.Length; i++)
            {
                var p = pips[i];
                bool shown = i < max;
                DeskUi.SetActive(p, shown);
                if (!shown) continue;
                bool on = i < current;
                SadeUi.Sprite(p, (on ? "pip_full_" : "pip_empty_") + size, on ? fullKey : emptyKey);
                SadeUi.Place(p, i * (size + gap), 0, size, size);
            }
            var rt = (RectTransform)transform;
            var s = new Vector2(Width, size);
            if (rt.sizeDelta != s) rt.sizeDelta = s;
        }
    }
}
