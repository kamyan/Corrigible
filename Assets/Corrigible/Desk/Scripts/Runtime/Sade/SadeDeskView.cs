using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>S-Masa1 köşe HUD'unu mevcut prefab referanslarına bağlar. Harita veya oyuncu komutu yönetmez.</summary>
    public sealed class SadeDeskView : MonoBehaviour
    {
        [SerializeField] MockSadeDeskDataSource source;
        [SerializeField] HudChipView fund, power;
        [SerializeField] GoalChipView goals;
        [SerializeField] ClockPlaqueView clock;
        [Tooltip("Sıra: küresel güven, bağımlılık, tekillik. Sağ köşeden ters sırayla dizilir.")]
        [SerializeField] HudChipView[] risks = new HudChipView[3];
        [SerializeField] SadeDockView dock;
        [SerializeField] RectTransform legendRoot, envelopeRoot;
        [SerializeField] TMP_Text legendLabel, envelopeTitle, envelopeHint;

        public SadeDeskViewModel Data { get; private set; }

        void OnEnable()
        {
            if (source != null) source.Changed += BindSource;
            DeskLoc.Instance.Changed += Refresh;
            DeskTheme.Changed += Refresh;
            if (source != null) BindSource(); else Refresh();
        }

        void OnDisable()
        {
            if (source != null) source.Changed -= BindSource;
            DeskLoc.Instance.Changed -= Refresh;
            DeskTheme.Changed -= Refresh;
        }

        void BindSource() { if (source != null) Bind(source.Current); }
        void Refresh() { if (Data != null) Bind(Data); }

        /// <summary>Editor önizlemesi ve durağan sahne: atanmış S1 verisini gösterir.</summary>
        public void ShowSample() { if (source != null) BindSource(); else Refresh(); }

        public void Bind(SadeDeskViewModel vm)
        {
            if (vm == null) return;
            Data = vm;
            BindChip(fund, Find(vm.Resources, "fund"));
            BindChip(power, Find(vm.Resources, "politicalPower"));
            if (goals != null) goals.Bind(vm.GoalTitle, vm.Goals);
            if (clock != null) clock.Bind(vm.Clock);
            for (int i = 0; i < risks.Length; i++) BindChip(risks[i], i < vm.Risks.Count ? vm.Risks[i] : null);
            if (dock != null) dock.Bind(vm.Dock);
            DeskUi.SetText(legendLabel, Corrigible.Core.Loc.T("LEJANT"));
            if (vm.KillSwitch != null)
            {
                DeskUi.SetText(envelopeTitle, DeskLoc.Instance.T(vm.KillSwitch.Title));
                DeskUi.SetText(envelopeHint, DeskLoc.Instance.T(vm.KillSwitch.Sub));
            }
            LayoutHud();
        }

        static SadeChipVM Find(System.Collections.Generic.List<SadeChipVM> values, string id) => values.Find(v => v.Id == id);

        static void BindChip(HudChipView view, SadeChipVM vm)
        {
            if (view == null) return;
            DeskUi.SetActive(view, vm != null);
            if (vm != null) view.Bind(vm);
        }

        /// <summary>1920×1080 referansında TSV köşeleri; daha geniş HUD metni komşusunu iter, harita ölçeği uygulanmaz.</summary>
        public void LayoutHud()
        {
            Anchor(fund, new Vector2(0, 1), new Vector2(16, -14));
            float powerX = Mathf.Max(196, fund != null ? 16 + fund.Width + 8 : 196);
            Anchor(power, new Vector2(0, 1), new Vector2(powerX, -14));
            float goalsX = Mathf.Max(376, power != null ? powerX + power.Width + 8 : 376);
            Anchor(goals, new Vector2(0, 1), new Vector2(goalsX, -14));
            Anchor(clock, new Vector2(0.5f, 1), new Vector2(0, -14));
            float right = 16;
            for (int i = risks.Length - 1; i >= 0; i--)
            {
                var chip = risks[i];
                if (chip == null || !chip.gameObject.activeSelf) continue;
                Anchor(chip, new Vector2(1, 1), new Vector2(-right, -14));
                right += chip.Width + 8;
            }
            Anchor(dock, Vector2.zero, new Vector2(16, 0));
            Anchor(legendRoot, new Vector2(1, 0), new Vector2(-282, 26));
            Anchor(envelopeRoot, new Vector2(1, 0), new Vector2(-16, 14));
            if (legendRoot != null) legendRoot.sizeDelta = new Vector2(98.3f, 36);
            if (envelopeRoot != null) envelopeRoot.sizeDelta = new Vector2(250, 62);
        }

        static void Anchor(Component component, Vector2 anchor, Vector2 position)
        {
            if (component == null) return;
            var rt = (RectTransform)component.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
        }
    }
}
