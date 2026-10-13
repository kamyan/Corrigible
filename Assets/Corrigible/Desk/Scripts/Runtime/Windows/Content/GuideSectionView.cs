using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Rehber bölümü: başlık (.cap) + gövde metni (Franklin 13, satır 1,35) + isteğe bağlı bağlantı.</summary>
    public sealed class GuideSectionView : MonoBehaviour
    {
        [SerializeField] TMP_Text cap, body;
        [SerializeField] DeskButton action;

        GuideSectionVM vm;
        public float Height { get; private set; }
        public bool Wired { get; set; }
        public event Action<string> Invoked;

        void Awake() { if (action != null) action.onClick.AddListener(() => Invoked?.Invoke(vm?.Action?.Command)); }

        public void Bind(GuideSectionVM s, float width)
        {
            vm = s;
            DeskUi.SetText(cap, DeskLoc.Instance.T(s.Title));
            DeskUi.SetText(body, DeskLoc.Instance.T(s.Body));
            float bh = body.GetPreferredValues(body.text, width, 1000).y;
            Height = Mathf.Ceil(20 + bh + 4);
            bool link = s.Action != null && action != null;
            if (action != null) DeskUi.SetActive(action, link);
            if (!link) return;
            action.SetLabel(DeskLoc.Instance.T(s.Action.Label));
            var r = (RectTransform)action.transform;
            r.anchoredPosition = new Vector2(-6, -Height);   // bağlantının iç boşluğu metinle hizalı
            Height += 26;
        }
    }
}
