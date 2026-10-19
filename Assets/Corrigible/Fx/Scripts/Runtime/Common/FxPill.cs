using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Corrigible.Fx
{
    public sealed class FxPill : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image background;
        [SerializeField] public TMP_Text label;
        [SerializeField] public bool success=true;
        void OnEnable(){if(theme==null)return;theme.Apply(background,success?"fx_pill_9":"fx_frame_1",theme.Color(success?"mt":"cy"));label.color=theme.Color(success?"bg":"cy");}
    }
}
