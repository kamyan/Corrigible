using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxScale : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image track,report,field,band;
        [SerializeField] public bool lineMarkers;
        [SerializeField] public Sprite markerSprite;
        [SerializeField,Range(0,1)] public float reportValue=.58f,fieldValue=.44f;
        void OnEnable()=>Refresh();
        public void Refresh(){if(theme==null)return;theme.Apply(track,"fx_scale_gradient",Color.white);theme.Apply(report,"fx_tri_down_12",theme.Color("am"));theme.Apply(field,"fx_tri_down_12",theme.Color("mt"));Position(report.rectTransform,reportValue);Position(field.rectTransform,fieldValue);if(lineMarkers){foreach(var mark in new[]{report,field}){mark.sprite=markerSprite;mark.type=Image.Type.Simple;var r=mark.rectTransform;r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(3,26);r.anchoredPosition=new Vector2(0,-4);}}FxLedBar.Range(band.rectTransform,Mathf.Min(reportValue,fieldValue),Mathf.Max(reportValue,fieldValue));var c=theme.Color("vi");c.a=.35f;band.color=c;}
        static void Position(RectTransform rt,float value){rt.anchorMin=rt.anchorMax=new Vector2(value,1);rt.anchoredPosition=new Vector2(0,6);}
    }
}
