using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Tek uyarı fişi: mühür (ok) ya da damga (yapılamadı/sonuç), gün, metin, “kapat ×”.</summary>
    public sealed class ToastSlipView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Image seal;
        [SerializeField] StampView stamp;
        [SerializeField] TMP_Text day, text, close;
        [SerializeField] DeskRow row;
        [SerializeField] float padX = 16, height = 40;

        static readonly string CloseText = Corrigible.Core.Loc.N("kapat ×"), FailText = Corrigible.Core.Loc.N("YAPILAMADI"), InfoText = Corrigible.Core.Loc.N("SONUÇ");

        public int Id { get; private set; }
        public float Born { get; private set; }
        public event Action<ToastSlipView> Clicked;

        public void Bind(ToastVM vm, float born)
        {
            Id = vm.Id; Born = born;
            var T = DeskLoc.Instance;
            bool ok = vm.Kind == "ok";
            DeskUi.SetActive(seal, ok);
            DeskUi.SetActive(stamp, !ok);
            if (!ok) stamp.Set(T.T(vm.Stamp ?? (vm.Kind == "fail" ? FailText : InfoText)), vm.Kind == "fail" ? StampInk.Red : StampInk.Blue, StampSize.Small, -4, "toast." + vm.Id);
            DeskUi.SetText(day, vm.Day);
            DeskUi.SetActive(day, !string.IsNullOrEmpty(vm.Day));
            DeskUi.SetText(text, DeskText.Upper(T.T(vm.Text)));
            DeskUi.SetText(close, T.T(CloseText));
            row.Layout();
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(Mathf.Round(row.Width + 2 * padX), height);
            SetAlpha(0);
        }

        public void SetAlpha(float a) { if (!Mathf.Approximately(group.alpha, a)) group.alpha = a; }

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(this);
    }
}
