using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxScreenScanlines : MonoBehaviour
    {
        [SerializeField] public RawImage image;
        void OnEnable()=>Refresh();
        void OnRectTransformDimensionsChange()=>Refresh();
        public void Refresh(){if(image==null||image.canvas==null)return;var size=image.canvas.pixelRect.size;image.uvRect=new Rect(0,0,size.x,size.y/3);}
    }
}
