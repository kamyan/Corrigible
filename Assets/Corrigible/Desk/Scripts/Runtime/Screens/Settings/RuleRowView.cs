using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>Onay satırı (ayarlar: sesi kapat): “X” kutusu, kalın etiket, altında açıklama; satırın tamamı tıklanır.</summary>
    public sealed class RuleRowView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] TMP_Text mark, label, hint;

        public event Action<RuleRowView> Clicked;

        /// <summary>Etiket ve açıklama Türkçe kaynaktır, gösterirken çevrilir.</summary>
        public void Bind(string labelSource, string hintSource, bool on)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(mark, on ? "X" : "");
            DeskUi.SetText(label, T.T(labelSource));
            DeskUi.SetText(hint, T.T(hintSource));
        }

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(this);
    }
}
