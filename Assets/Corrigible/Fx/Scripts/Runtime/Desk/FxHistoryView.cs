using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxHistoryView : MonoBehaviour
    {
        [SerializeField] public FxLineGraphic line,field;
        [SerializeField] public FxHistoryFillGraphic area;
        [SerializeField] public RectTransform end;
        [SerializeField] public FxLineGraphic[] events;
        public void Bind(JObject data,bool fund)
        {
            var values=fund?data?["values"] as JArray:data?["report"] as JArray;var rect=((RectTransform)transform).rect;
            bool visible=values!=null&&values.Count>1;line.gameObject.SetActive(visible);area.gameObject.SetActive(visible);end.gameObject.SetActive(visible);
            foreach(var e in events)e.gameObject.SetActive(false);field.gameObject.SetActive(false);if(!visible)return;
            float min=0,max=60;if(fund){min=max=(float)values[0];foreach(var v in values){min=Mathf.Min(min,(float)v);max=Mathf.Max(max,(float)v);}min-=2;max+=2;}
            var points=new Vector2[values.Count];for(int n=0;n<points.Length;n++){float value=fund?(float)values[n]:(float)values[n][1];float x=fund?(float)n/(points.Length-1):((float)values[n][0]-44)/30;points[n]=new Vector2(x*rect.width,-rect.height*(1-(value-min)/Mathf.Max(1,max-min)));}
            line.points=points;line.SetVerticesDirty();area.points=points;area.SetVerticesDirty();end.anchoredPosition=points[points.Length-1];
            if(!fund&&data?["field"] is JArray sample&&sample.Count>0){var v=sample[0];float x=((float)v[0]-44)/30*rect.width,y=-rect.height*(1-((float)v[1]-min)/(max-min));field.points=new[]{new Vector2(x-3,y),new Vector2(x,y+3),new Vector2(x+3,y),new Vector2(x,y-3),new Vector2(x-3,y)};field.gameObject.SetActive(true);field.SetVerticesDirty();}
            if(fund&&data?["events"] is JArray ev)for(int n=0;n<events.Length&&n<ev.Count;n++){float x=((float)ev[n]["day"]-(float)data["fromDay"])/(points.Length-1)*rect.width;events[n].points=new[]{new Vector2(x,0),new Vector2(x,-rect.height)};events[n].gameObject.SetActive(true);events[n].SetVerticesDirty();}
        }
    }
}
