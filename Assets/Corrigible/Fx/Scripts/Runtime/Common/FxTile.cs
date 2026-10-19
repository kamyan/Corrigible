using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxTile : MonoBehaviour
    {
        [SerializeField] public RawImage image;
        [SerializeField] public Vector2 period=new Vector2(6,2);
        void OnEnable()=>Refresh();
        void OnRectTransformDimensionsChange()=>Refresh();
        public void Refresh(){if(image==null)return;var r=image.rectTransform.rect;image.uvRect=new Rect(0,0,r.width/Mathf.Max(.001f,period.x),r.height/Mathf.Max(.001f,period.y));}
    }
}
