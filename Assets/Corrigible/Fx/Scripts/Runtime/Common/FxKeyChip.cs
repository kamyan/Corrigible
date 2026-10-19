using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxKeyChip : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image frame;
        [SerializeField] public TMP_Text label;
        [SerializeField] public string key="Esc";
        void OnEnable(){if(theme==null)return;var c=theme.Color("cy");c.a=.35f;theme.Apply(frame,"fx_frame_1",c);label.font=theme.Font("IBMPlexMono-SemiBold");label.fontSize=11;label.color=theme.Color("tx3");label.text=key;}
    }
}
