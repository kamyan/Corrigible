using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Danışman bandı (C1 §7.1): sarı not şeridi, iki bant parçası, “DANIŞMAN NOTU” (kırmızı; İllüzyonda yeşil), tek satır
    /// mesaj ve sağda bağlantı düğmeleri. Mesaj küçültülmez; sığmazsa “…” ile kesilir (açık karar 11).
    /// </summary>
    public sealed class AdvisorBandView : MonoBehaviour, IIllusionAware
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text message;
        [SerializeField] DeskRow actionsRow;
        [SerializeField] DeskButton[] actions = System.Array.Empty<DeskButton>();

        [SerializeField] DeskHover messageHover;
        [Tooltip("Eylem düğmeleri arası: yalnız bağlantılar 4 px (C1), mürekkep düğme varsa 6 px (C2 “4× hızlandır”). Veriye bağlıdır;\n" +
                 "İllüzyon anahtarı tek başına yerleşimi değiştirmez.")]
        [SerializeField] float actionGap = 4, actionGapButtons = 6;

        public IReadOnlyList<DeskButton> Actions => actions;
        /// <summary>Tıklanan eylemin komutu (ör. “FocusRegion:DAS”, “DismissAdvisor”).</summary>
        public event System.Action<string> ActionInvoked;
        AdvisorVM vm;

        void Awake()
        {
            for (int i = 0; i < actions.Length; i++)
            {
                int k = i;
                actions[i].onClick.AddListener(() => { if (vm != null && k < vm.Actions.Count) ActionInvoked?.Invoke(vm.Actions[k].Command); });
            }
            if (messageHover != null)
            {
                // Açık karar 11: mesaj sığmazsa “…” ile kesilir, tam metin ipucunda
                messageHover.Entered += _ => { if (message.isTextTruncated) DeskTooltip.ShowText(this, message.text); };
                messageHover.Exited += _ => DeskTooltip.Hide(this);
            }
        }

        public void Bind(AdvisorVM a)
        {
            vm = a;
            gameObject.SetActive(a != null);
            if (a == null) return;
            DeskUi.SetText(title, DeskLoc.Instance.T(a.Title));
            DeskUi.Tint(title, DeskTheme.Illusion ? "advisor.title" : a.TitleColor == "green" ? "ink.green" : "advisor.title");
            DeskUi.SetText(message, DeskLoc.Instance.T(a.Text));
            bool buttons = false;
            for (int i = 0; i < actions.Length; i++)
            {
                bool on = a.Actions != null && i < a.Actions.Count;
                if (on && !string.IsNullOrEmpty(a.Actions[i].Kind) && a.Actions[i].Kind != "link") buttons = true;
                DeskUi.SetActive(actions[i], on);
                if (!on) continue;
                // Tür VM'den: bağlantı (C1) ya da küçük mürekkep düğme (“4× hızlandır”, C2)
                actions[i].SetKind(string.IsNullOrEmpty(a.Actions[i].Kind) ? "link" : a.Actions[i].Kind);
                actions[i].SetLabel(a.Actions[i].Label); actions[i].SetShortcut(a.Actions[i].Key);
            }
            actionsRow.Gap = buttons ? actionGapButtons : actionGap;
            actionsRow.Layout();
        }

        public void SetIllusion(bool on) { if (vm != null) Bind(vm); }
    }
}
