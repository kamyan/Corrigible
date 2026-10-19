using Corrigible.Core;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxTeamTokenView : MonoBehaviour
    {
        [SerializeField] public string teamId;
        [SerializeField] public GameObject highlight;
        public void SetHighlighted(bool value){if(highlight!=null)highlight.SetActive(value);}
        [SerializeField] public FxMapView map;
        [SerializeField] public SadeMarkerAnchor anchor;
        [SerializeField] public Vector2 samplePoint;
        [SerializeField] public GameObject etaTag,route;
        [SerializeField] public TMP_Text eta;
        [SerializeField] public FxLineGraphic routeLine,arrow;
        SadeMapTeamVM model;
        void OnEnable(){if(map!=null)map.cover.Changed+=Refresh;}
        void OnDisable(){if(map!=null)map.cover.Changed-=Refresh;}
        public void Bind(SadeMapTeamVM value){model=value;Refresh();}
        public void Refresh()
        {
            if(model==null)return;var point=model.HasPoint?new Vector2(model.X,model.Y):samplePoint;anchor.SetPoint(point,Vector2.zero);
            bool travelling=model.Status=="travelling";etaTag.SetActive(travelling);route.SetActive(travelling);if(!travelling)return;
            eta.text=Loc.F("{0} gün",model.EtaDays);var target=map.Find(model.To);if(target==null){route.SetActive(false);return;}
            var p=FxRegionRules.Route(map.cover.ToCanvas(point),map.cover.ToCanvas(target.Point),(float)target.Node.Radius);
            routeLine.points=new[]{new Vector2(p[0].x,-p[0].y),new Vector2(p[1].x,-p[1].y),new Vector2(p[2].x,-p[2].y)};routeLine.SetVerticesDirty();
            var q=new Vector2(p[2].x,-p[2].y);var d=(new Vector2(p[2].x-p[1].x,-p[2].y+p[1].y)).normalized;var n=new Vector2(-d.y,d.x);
            arrow.points=new[]{q-d*8-n*5,q,q-d*8+n*5};arrow.SetVerticesDirty();
        }
    }
}
