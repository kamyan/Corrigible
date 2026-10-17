using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Alt sol sekmeler (PROMPT §A.9): sol 16, alta yaslı, aralık 6. Sekmeler prefab'da hazır (araştırma, haberler, ekipler,
    /// YZ günlüğü, rehber); VM her birinin etiketini, tuşunu ve rozetini verir, açık çekmecenin sekmesi “açık” durur (50 px).
    /// </summary>
    public sealed class SadeDockView : MonoBehaviour
    {
        [SerializeField] SadeDockTabView[] tabs = Array.Empty<SadeDockTabView>();
        [SerializeField] float gap = 6;

        [Header("Önizleme (edit mod; oyunda VM)")]
        [SerializeField] SadeDockVM sample = new SadeDockVM
        {
            Tabs = new List<SadeDockTabVM>
            {
                new SadeDockTabVM { Id = "research", Label = "ARAŞTIRMA", Key = "R" },
                new SadeDockTabVM { Id = "news", Label = "HABERLER", Key = "N", BadgeType = "count", BadgeValue = 3 },
                new SadeDockTabVM { Id = "teams", Label = "EKİPLER", Key = "T" },
                new SadeDockTabVM { Id = "aiLog", Label = "YZ GÜNLÜĞÜ", Key = "L", BadgeType = "dot" },
                new SadeDockTabVM { Id = "guide", Label = "REHBER", Key = "H" },
            },
        };

        bool bound;

        public IReadOnlyList<SadeDockTabView> Tabs => tabs;
        /// <summary>Sekmeye basıldı (kimlik: research, news, teams, aiLog, guide).</summary>
        public event Action<string> TabClicked;

        void Awake()
        {
            foreach (var t in tabs) if (t != null) t.Clicked += v => TabClicked?.Invoke(v.TabId);
        }

        void OnEnable() { if (!bound) Render(sample); }
        void OnValidate() => DeskUi.Deferred(this, () => { if (!bound) Render(sample); });

        /// <summary>Önizleme verisini çizer (kurucu ve galeri).</summary>
        public void ShowSample() => Render(sample);

        public void Bind(SadeDockVM vm)
        {
            bound = true;
            Render(vm);
        }

        void Render(SadeDockVM vm)
        {
            if (vm == null) return;
            float x = 0;
            for (int i = 0; i < tabs.Length; i++)
            {
                var t = tabs[i];
                bool on = i < vm.Tabs.Count;
                DeskUi.SetActive(t, on);
                if (!on) continue;
                t.Bind(vm.Tabs[i], vm.Open == vm.Tabs[i].Id);
                var rt = (RectTransform)t.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                rt.anchoredPosition = new Vector2(x, 0);
                x += t.Width + gap;
            }
            var self = (RectTransform)transform;
            float w = Mathf.Max(0, x - gap);
            if (!Mathf.Approximately(self.sizeDelta.x, w)) self.sizeDelta = new Vector2(w, 50);
        }
    }
}
