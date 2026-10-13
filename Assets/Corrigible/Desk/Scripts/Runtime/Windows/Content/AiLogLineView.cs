using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>YZ günlüğü satırı: zaman (Courier 13 ink3) + kırmızı daktilo metni (Courier 700 14); okunmuş satır soluk.</summary>
    public sealed class AiLogLineView : MonoBehaviour
    {
        [SerializeField] TMP_Text time, text;
        [SerializeField] float textX = 104;

        public float Height { get; private set; }

        public void Bind(AiLogLineVM l, float width)
        {
            DeskUi.SetText(time, l.Time);
            DeskUi.SetText(text, DeskLoc.Instance.T(l.Text));
            DeskUi.Tint(time, "ink.ink3", l.Read ? 0.6f : 1);
            DeskUi.Tint(text, "ink.red", l.Read ? 0.55f : 1);
            float th = text.GetPreferredValues(text.text, width - textX, 1000).y;
            Height = Mathf.Max(22, Mathf.Ceil(th) + 6);
        }
    }
}
