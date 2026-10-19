using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxBreakdownTooltipView : MonoBehaviour
    {
        [SerializeField] public FxPanelBindings ui;
        [SerializeField] public FxHistoryView history;
        public void Bind(JObject data)
        {
            bool fund=(string)data["target"]=="fund"&&data["history"]!=null;ui.Active("fundBody",fund);ui.Active("genericBody",!fund);
            ui.Text("title",FxDeskText.T(FxDeskText.S(data,"title",fund?"FON":"HEDEF")));float net=FxDeskText.N(data,"net");ui.Text("net",DeskFormat.Signed(net,1));
            if(!fund&&data["rows"] is JArray rows){var lines=new List<string>();if(data["total"]!=null)lines.Add(FxDeskText.T((string)data["total"]));if(data["note"]!=null)lines.Add(FxDeskText.T((string)data["note"]));foreach(var row in rows)lines.Add(Loc.F("{0} · {1}",FxDeskText.T((string)row["label"]),FxDeskText.T((string)row["value"])));if(data["totalRight"]!=null)lines.Add(FxDeskText.T((string)data["totalRight"]));ui.Text("genericBody",string.Join("\n",lines));ui["genericBody"].Rect.sizeDelta=new Vector2(268,Mathf.Max(56,ui["genericBody"].Text.GetPreferredValues(ui["genericBody"].Text.text,268,float.PositiveInfinity).y+8));return;}
            if(!fund){ui.Text("genericBody",data["message"]!=null?FxDeskText.T((string)data["message"]):Loc.F("{0} · {1}",FxDeskText.Value(data["value"],FxDeskText.S(data,"trust","verified")),FxDeskText.Trust(FxDeskText.S(data,"trust","verified"))));return;}
            float income=0,expense=0;foreach(var row in data["income"]??new JArray())income+=FxDeskText.N(row,"value");foreach(var row in data["expense"]??new JArray())expense+=FxDeskText.N(row,"value");
            ui.Text("income",DeskFormat.Signed(income,1));ui.Text("expense",DeskFormat.Signed(-expense,1));ui.Text("netLine",Loc.F("Net {0} /gün",DeskFormat.Signed(net,1)));ui.Text("runway",data["runwayDays"]?.Type==JTokenType.Null?"":Loc.F("0’a ≈{0} gün",(int?)data["runwayDays"]??0));
            var inc=ui["incomeBar"].Rect;inc.anchorMax=new Vector2(expense>0?Mathf.Clamp01(income/expense):0,1);history.Bind(data["history"] as JObject,true);
            ui.Text("historyStart",Loc.F("G-{0:000}",(int?)data["history"]?["fromDay"]??0));ui.Text("historyEnd",Loc.F("bugün · {0}",FxDeskText.Num((double?)data["history"]?["values"]?.Last??0,1)));
        }
    }
}
