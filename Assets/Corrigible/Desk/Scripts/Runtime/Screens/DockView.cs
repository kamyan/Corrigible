using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Alt sekmeler (C1 §7.1): sekmeler alta yaslı, aralık 6, soldan dizilir.</summary>
    public sealed class DockView : MonoBehaviour, IIllusionAware
    {
        [SerializeField] DockTabView[] tabs = System.Array.Empty<DockTabView>();
        [SerializeField] float gap = 6;

        public IReadOnlyList<DockTabView> Tabs => tabs;

        List<DockTabVM> last;

        /// <summary>İllüzyonda rozet yok (PROMPT §7.2).</summary>
        public void SetIllusion(bool on) { if (last != null) Bind(last); }

        public void Bind(List<DockTabVM> list)
        {
            last = list;
            float x = 0;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = list != null && i < list.Count;
                DeskUi.SetActive(tabs[i], on);
                if (!on) continue;
                tabs[i].Bind(list[i]);
                var rt = (RectTransform)tabs[i].transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0);
                rt.anchoredPosition = new Vector2(x, 0);
                x += tabs[i].Width + gap;
            }
        }
    }
}
