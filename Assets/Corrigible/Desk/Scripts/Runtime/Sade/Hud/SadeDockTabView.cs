using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Alt sekme (PROMPT §A.9): renkli kâğıt (köşe 7/7/0/0, alt kenar yok), iç üst parlaklık, sağda (2, 0) gölge. İçerik: simge
    /// 17 + etiket (LF 700 13, .06em) + tuş fişi 17. Kapalı 42 px, açık 50 px (alt iç boşluk 10, kenar 1,5 siyah).
    /// Rozet sağ-üstte: sarı sayı ya da kırmızı nokta (C CountBadgeView); merkezi sekmenin sağ-üstünden (−3, +2).
    /// </summary>
    public sealed class SadeDockTabView : Selectable, IPointerClickHandler
    {
        [SerializeField] RectTransform visual;
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        [SerializeField] Image highlight;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text label;
        [SerializeField] KeyChipView key;
        [SerializeField] CountBadgeView badge;

        [Header("Önizleme (edit mod; oyunda VM)")]
        [SerializeField] SadeDockTabVM sample = new SadeDockTabVM { Id = "research", Label = "ARAŞTIRMA", Key = "R" };
        [SerializeField] bool sampleOpen;

        const float ClosedH = 42, OpenH = 50, Gap = 8, IconSize = 17;
        bool open, bound;

        public string TabId { get; private set; }
        public bool IsOpen => open;
        public float Width => ((RectTransform)transform).sizeDelta.x;
        public event Action<SadeDockTabView> Clicked;

        public void OnPointerClick(PointerEventData e) { if (IsInteractable()) Clicked?.Invoke(this); }

        /// <summary>Sekme çubuğundaki sekmeyi çubuk çizer; kendi önizlemesini yalnız tek başınayken gösterir.</summary>
        bool InDock => transform.parent != null && transform.parent.GetComponentInParent<SadeDockView>(true) != null;

        protected override void OnEnable()
        {
            base.OnEnable();
            DeskUi.NoNavigation(this);
            if (!bound && !InDock) Render(sample, sampleOpen);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            DeskUi.Deferred(this, () => { if (!bound && !InDock) Render(sample, sampleOpen); });
        }
#endif

        /// <summary>Önizleme verisini çizer (kurucu ve galeri).</summary>
        public void ShowSample() => Render(sample, sampleOpen);

        public void Bind(SadeDockTabVM vm, bool isOpen)
        {
            bound = true;
            Render(vm, isOpen);
        }

        public void SetOpen(bool on)
        {
            if (open == on) return;
            open = on;
            ApplyFrame();
        }

        /// <summary>Sekme simgesi kimliğe göre (araştırma şişesi, haber, ekip, terminal, kitap).</summary>
        static string IconOf(string id) => id switch
        {
            "news" => "news", "teams" => "teams", "aiLog" => "term", "guide" => "book", _ => "flask",
        };

        void Render(SadeDockTabVM vm, bool isOpen)
        {
            if (vm == null || fill == null) return;
            TabId = vm.Id;
            open = isOpen;
            SadeUi.Sprite(fill, "sh_fill_r7_top", "sade.dockTabs." + vm.Id);
            SadeUi.Shadows(fill, "dockTab");
            SadeUi.Sprite(highlight, "sh_tab_highlight");
            SadeUi.Sprite(icon, "ic_" + IconOf(vm.Id) + "_l", "sade.ink");
            SadeUi.Type(label, "dockTab");
            DeskUi.Tint(label, "sade.ink");
            string text = DeskLoc.Instance.T(vm.Label);
            DeskUi.SetText(label, text);
            if (key != null) key.Set(vm.Key);

            bool hasBadge = !string.IsNullOrEmpty(vm.BadgeType) && badge != null && !DeskTheme.Illusion;
            DeskUi.SetActive(badge, hasBadge);
            if (hasBadge)
            {
                if (vm.BadgeType == "dot") { badge.SetDot(); badge.transform.localScale = Vector3.one * (14f / 16f); }
                else { badge.SetCount(vm.BadgeValue); badge.transform.localScale = Vector3.one; }
            }

            // Genişlik: kenar 1 + sol iç 11 + simge 17 + 8 + etiket + 8 + tuş + sağ iç 12 + kenar 1
            float labelW = SadeUi.Width(label, text);
            float keyW = key != null ? ((RectTransform)key.transform).sizeDelta.x : 17;
            float x = 12;
            SadeUi.Place(icon, x, 0, IconSize, IconSize); x += IconSize + Gap;
            SadeUi.Place(label, x, 0, labelW, 13); x += labelW + Gap;
            if (key != null) SadeUi.Place(key, x, 0, keyW, 17);
            x += keyW + 12 + 1;
            var rt = (RectTransform)transform;
            if (!Mathf.Approximately(rt.sizeDelta.x, x) || !Mathf.Approximately(rt.sizeDelta.y, OpenH)) rt.sizeDelta = new Vector2(x, OpenH);
            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (visual == null) return;
            float h = open ? OpenH : ClosedH;
            visual.anchorMin = new Vector2(0, 0); visual.anchorMax = new Vector2(1, 0); visual.pivot = new Vector2(0.5f, 0);
            visual.anchoredPosition = Vector2.zero;
            visual.sizeDelta = new Vector2(0, h);
            SadeUi.Sprite(frame, open ? "sh_frame_r7_top_1_5" : "sh_frame_r7_top_1", "sade.desk.dock.tabBorder", open ? 1 : 0.55f);

            // İçerik dikeyde: kapalıyken alt iç boşluk 4, açıkken 10; satır 17 px
            float border = open ? 1.5f : 1f, padBottom = open ? 10 : 4;
            float rowTop = border + (h - border - padBottom - 17) / 2;
            Shift(icon, rowTop); Shift(label, rowTop + 2); if (key != null) Shift(key, rowTop);
            if (badge != null && badge.gameObject.activeSelf)
            {
                var b = (RectTransform)badge.transform;
                b.anchorMin = b.anchorMax = new Vector2(1, 0);
                b.pivot = new Vector2(0.5f, 0.5f);
                b.anchoredPosition = new Vector2(-3, h - 2);
            }
        }

        static void Shift(Component c, float top)
        {
            if (c == null) return;
            var rt = (RectTransform)c.transform;
            // Öğeler görselin içinde sol-üst çapalı; görsel alta yaslı olduğundan konum görselin üstüne göre
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -top);
        }
    }
}
