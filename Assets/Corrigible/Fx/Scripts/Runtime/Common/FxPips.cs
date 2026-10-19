using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxPips : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image[] pips;
        [SerializeField] public int value=3;
        [SerializeField] public bool enemy;
        void OnEnable()=>Set(value);
        public void Set(int count){value=Mathf.Clamp(count,0,pips.Length);if(theme==null)return;for(int i=0;i<pips.Length;i++)theme.Apply(pips[i],i<value?"fx_pip_9":"fx_pip_9_line",theme.Color(enemy?"rd":"mt"));}
    }
}
