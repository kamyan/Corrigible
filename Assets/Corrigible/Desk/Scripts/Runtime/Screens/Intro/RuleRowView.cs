using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>Kural satırı (giriş dosyası): “X” kutusu, kalın etiket, altında açıklama; satırın tamamı tıklanır.</summary>
    public sealed class RuleRowView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] TMP_Text mark, label, hint;

        public string Command { get; private set; }
        public event Action<RuleRowView> Clicked;

        public void Bind(RuleVM vm)
        {
            Command = vm.Command;
            var T = DeskLoc.Instance;
            DeskUi.SetText(mark, vm.On ? "X" : "");
            DeskUi.SetText(label, T.T(vm.Label));
            DeskUi.SetText(hint, T.T(vm.Hint));
        }

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(this);
    }
}
