using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxCloseButton : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Button button;
        [SerializeField] public Image frame,icon;
        void OnEnable(){if(theme==null)return;theme.Apply(frame,"fx_frame_1",theme.Color("line"));theme.Apply(icon,"ic_x_s",theme.Color("tx2"));}
    }
}
