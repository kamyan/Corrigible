using Corrigible.Core;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>FX prefab metnini projenin ortak TR/EN tablosundan yeniler.</summary>
    public sealed class FxLabel : MonoBehaviour
    {
        [SerializeField] public TMP_Text text;
        [SerializeField,TextArea] public string source;
        void OnEnable(){DeskLoc.Instance.Changed+=Refresh;Refresh();}
        void OnDisable()=>DeskLoc.Instance.Changed-=Refresh;
        public void Refresh(){if(text!=null)text.text=Loc.T(source);}
        public void SetSource(string value){source=value;Refresh();}
    }
}
