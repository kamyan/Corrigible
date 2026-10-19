using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxNodeTooltipView : MonoBehaviour
    {
        [SerializeField] public FxPanelBindings ui;
        [SerializeField] public FxTheme theme;
        public void Bind(JObject data)
        {
            string id=(string)data["regionId"],trust=FxDeskText.S(data,"trust","unknown");bool known=data["value"]!=null&&data["value"].Type!=JTokenType.Null&&trust!="unknown";
            ui.Text("title",FxDeskText.T("region."+id));ui.Text("trust",FxDeskText.Trust(trust));ui.Tint("trust",theme.Color(trust=="report"?"vi":trust=="verified"?"mt":"am"));
            ui.Text("value",FxDeskText.Value(data["value"],trust));ui.Tint("value",theme.Color(trust=="report"?"vi":"tx"));float delta=FxDeskText.N(data,"deltaPerDay");
            ui.Text("delta",known&&data["deltaPerDay"]!=null?Loc.F("{0}{1} /gün",delta<0?"↓":"↑",FxDeskText.Num(System.Math.Abs(delta),1)):"");
            ui.Text("state",FxDeskText.State(known?(double?)data["value"]:null));ui.Active("scale",known);if(known){var s=ui["scale"].Scale;s.reportValue=s.fieldValue=(float)data["value"]/100;s.Refresh();}
        }
    }
}
