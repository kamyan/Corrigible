using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// İpucu katmanı (PROMPT §7.2, F10): C2'deki dikkatli oyuncu ipuçlarını pembe numaralı işaretlerle gösterir
    /// (<c>C2_illusion_clues.png</c>). Konumlar VM'den (24 px dairenin sol-üstü, 1920×1080); üstüne gelince açıklama ipucunda.
    /// Yalnızca Editor/Development; oyuncu sürümünde kendini kapatır.
    /// </summary>
    public sealed class ClueOverlay : MonoBehaviour
    {
        [SerializeField] RectTransform markerTemplate;
        [SerializeField] RectTransform root;

        DeskPool<RectTransform> pool;
        readonly List<string> texts = new List<string>();

        void Awake()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            gameObject.SetActive(false);
#endif
        }

        public void Bind(List<ClueVM> clues)
        {
            if (!Application.isPlaying || markerTemplate == null) return;
            pool ??= new DeskPool<RectTransform>(markerTemplate, root);
            texts.Clear();
            int k = 0;
            if (clues != null)
                foreach (var c in clues)
                    foreach (var m in c.Markers)
                    {
                        var r = pool.Get(k);
                        r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
                        r.anchoredPosition = new Vector2(m[0], -m[1]);
                        DeskUi.SetText(r.GetComponentInChildren<TMP_Text>(true), c.N.ToString());
                        texts.Add(c.N + ". " + DeskLoc.Instance.T(c.Text));
                        if (r.TryGetComponent<DeskHover>(out var h) && k >= hooked) Hook(h, k);
                        k++;
                    }
            pool.Trim(k);
        }

        int hooked;

        void Hook(DeskHover h, int index)
        {
            hooked = Mathf.Max(hooked, index + 1);
            h.Entered += _ => { if (index < texts.Count) DeskTooltip.ShowText(h, texts[index]); };
            h.Exited += _ => DeskTooltip.Hide(h);
        }
    }
}
