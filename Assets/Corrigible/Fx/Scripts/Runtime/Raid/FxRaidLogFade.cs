using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Günlük üstündeki 46 piksel solma; metinleri ve simgeleri aynı biçimde örter.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FxRaidLogFade : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var clear=color;clear.a=0;
            vh.AddVert(new Vector2(r.xMin,r.yMin),clear,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);vh.AddVert(r.max,color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),clear,Vector2.zero);vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
        }
    }
}
