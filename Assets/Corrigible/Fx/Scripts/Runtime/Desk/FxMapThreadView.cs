using Corrigible.Desk;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxMapThreadView : MonoBehaviour
    {
        [SerializeField] public string fromId,toId;
        [SerializeField] public FxMapView map;
        [SerializeField] public FxLineGraphic[] strokes;
        void OnEnable(){if(map!=null&&map.cover!=null)map.cover.Changed+=Refresh;Refresh();}
        void OnDisable(){if(map!=null&&map.cover!=null)map.cover.Changed-=Refresh;}
        public void Refresh()
        {
            if(map==null)return;var a=map.Find(fromId);var b=map.Find(toId);if(a==null||b==null)return;
            var p=FxRegionRules.Spread(map.cover.ToCanvas(a.Point),map.cover.ToCanvas(b.Point),(float)a.Node.Radius,(float)b.Node.Radius);
            foreach(var line in strokes){line.points=new[]{new Vector2(p[0].x,-p[0].y),new Vector2(p[1].x,-p[1].y),new Vector2(p[2].x,-p[2].y)};line.SetVerticesDirty();}
        }
    }
}
