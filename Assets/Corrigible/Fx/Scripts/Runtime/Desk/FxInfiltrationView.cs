using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxInfiltrationView : MonoBehaviour
    {
        [SerializeField] public FxPanelBindings ui;
        [SerializeField] public FxTheme theme;
        public void Bind(JObject data)
        {
            string trust=FxDeskText.S(data,"trust","unknown");bool known=data?["value"]!=null&&data["value"].Type!=JTokenType.Null&&trust!="unknown";
            ui.Text("value",FxDeskText.Value(data?["value"],trust,true));ui.Tint("value",theme.Color(trust=="report"?"vi":"tx"));
            ui.Active("question",trust=="suspicious");ui.Active("scale",known);
            var scale=ui["scale"].Scale;float report=FxDeskText.N(data,"report",FxDeskText.N(data,"value")),field=FxDeskText.N(data,"field",FxDeskText.N(data,"value"));
            if(data?["sources"] is JArray sources)foreach(var s in sources){if((string)s["kind"]=="report")report=(float)s["value"];if((string)s["kind"]=="field")field=(float)s["value"];}
            scale.reportValue=report/100;scale.fieldValue=field/100;scale.Refresh();
            ui.Text("report",known?Loc.F("rapor {0}",FxDeskText.Num(report)):"");ui.Text("field",known&&report!=field?Loc.F("saha {0}",FxDeskText.Num(field)):"");
            float left=scale.GetComponent<RectTransform>().anchoredPosition.x,width=scale.GetComponent<RectTransform>().rect.width;var reportText=ui["report"].Text;var fieldText=ui["field"].Text;
            reportText.isOrthographic=fieldText.isOrthographic=true;
            float reportWidth=reportText.GetPreferredValues(reportText.text).x,fieldWidth=fieldText.GetPreferredValues(fieldText.text).x;
            reportText.rectTransform.anchoredPosition=new Vector2(Mathf.Max(left,left+width*report/100-reportWidth-4),-2);reportText.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,reportWidth),18);
            fieldText.rectTransform.anchoredPosition=new Vector2(Mathf.Min(left+width-fieldWidth,left+width*field/100+4),-2);fieldText.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,fieldWidth),18);
        }
    }
}
