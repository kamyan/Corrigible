using System;
using System.Collections.Generic;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
namespace Corrigible.Fx
{
    /// <summary>Sunum verisi önbelleği; gerçek sızmaya veya oyun RNG'sine erişmez.</summary>
    public sealed class FxDeskDataCache
    {
        public readonly Dictionary<string,JObject> Drawers=new Dictionary<string,JObject>();
        public JObject CardTemplate,DossierTemplate,FundTemplate,NodeTemplate;
        public SadeDeskData GameData;
        public void Load(MockSadeDeskDataSource source)
        {
            GameData=null;Drawers.Clear();CardTemplate=(JObject)source.ReadExample(2)["regionCard"]?.DeepClone();
            DossierTemplate=(JObject)source.ReadExample(9)["dossier"]?.DeepClone();FundTemplate=(JObject)source.ReadExample(3)["tooltip"]?.DeepClone();NodeTemplate=(JObject)source.ReadExample(8)["nodeTooltip"]?.DeepClone();
            if(DossierTemplate?["infiltration"] is JObject infiltration&&CardTemplate?["infiltration"]?["sources"] is JArray sources)infiltration["sources"]=sources.DeepClone();
            for(int n=3;n<=7;n++){var d=source.ReadExample(n)["drawer"] as JObject;if(d!=null)Drawers[(string)d["id"]]=(JObject)d.DeepClone();}
        }
        public void Load(SadeDeskData data){GameData=data;CardTemplate=DossierTemplate=FundTemplate=NodeTemplate=null;Drawers.Clear();foreach(var d in data.Drawers)Drawers[d.Key]=(JObject)d.Value.DeepClone();}
        public static RegionNodeVM Shown(SadeDeskViewModel vm,string id){foreach(var r in vm.Map.Regions)if(r.Id==id)return r;return null;}
        public JObject Card(SadeDeskViewModel vm,string id,bool illusion)
        {
            if(GameData!=null&&GameData.Cards.TryGetValue(id,out var actual))return (JObject)actual.DeepClone();
            var r=Shown(vm,id);var d=CardTemplate!=null?(JObject)CardTemplate.DeepClone():new JObject();d["regionId"]=id;d["nameKey"]="region."+id;
            int index=vm.Map.Regions.IndexOf(r);d["code"]="R-"+(index+1).ToString("00");
            if(id!="DoA"||illusion||!(d["infiltration"] is JObject)){d["infiltration"]=new JObject{["value"]=r?.Value,["trust"]=r?.Trust,["sources"]=new JArray()};d["stats"]=new JArray();d["team"]=null;d["status"]=new JObject{["trust"]=r?.Trust,["textKey"]="trust."+r?.Trust};}
            if(illusion)d["illusion"]=true;return d;
        }
        public JObject Dossier(SadeDeskViewModel vm,string id,bool illusion)
        {
            if(GameData!=null&&GameData.Dossiers.TryGetValue(id,out var actual))return (JObject)actual.DeepClone();
            var d=DossierTemplate!=null?(JObject)DossierTemplate.DeepClone():new JObject();d["regionId"]=id;d["nameKey"]="region."+id;
            if(id!="DoA"||illusion||!(d["infiltration"] is JObject)){var c=Card(vm,id,illusion);d["code"]=c["code"];d["infiltration"]=c["infiltration"];d["sources"]=new JArray();d["history"]=new JObject();d["links"]=new JArray();d["meta"]=new JObject();d["team"]=null;}
            foreach(var link in d["links"]??new JArray()){var shown=Shown(vm,(string)link["region"]);link["value"]=shown?.Value;link["trust"]=shown?.Trust;}
            return d;
        }
        public JObject Tooltip(SadeDeskViewModel vm,string id,bool illusion)
        {
            if(GameData!=null&&GameData.NodeTooltips.TryGetValue(id,out var actual))return (JObject)actual.DeepClone();
            var r=Shown(vm,id);var d=id=="GuA"&&NodeTemplate!=null?(JObject)NodeTemplate.DeepClone():new JObject();d["regionId"]=id;d["value"]=r?.Value;d["trust"]=r?.Trust;d["approx"]=r?.Trust=="report";if(illusion)d["deltaPerDay"]=0;return d;
        }
        public JObject Breakdown(SadeDeskViewModel vm,string id,bool illusion)
        {
            if(GameData!=null&&GameData.Breakdowns.TryGetValue(id,out var actual))return (JObject)actual.DeepClone();
            if(id=="fund"&&!illusion&&FundTemplate!=null)return (JObject)FundTemplate.DeepClone();
            SadeChipVM chip=null;foreach(var c in vm.Resources)if(c.Id==id)chip=c;foreach(var c in vm.Risks)if(c.Id==id)chip=c;
            return new JObject{["target"]=id,["title"]=chip?.Label??"HEDEF",["value"]=chip?.Value,["trust"]=chip?.Trust??"verified",["net"]=chip?.Delta??0,["income"]=new JArray(),["expense"]=new JArray()};
        }
        public JObject Drawer(string id)=>Drawers.TryGetValue(id,out var d)?d[id] as JObject:null;
    }
}
