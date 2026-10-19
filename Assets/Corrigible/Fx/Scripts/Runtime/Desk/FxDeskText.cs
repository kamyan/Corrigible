using System;
using Corrigible.Core;
using Corrigible.Desk;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace Corrigible.Fx
{
    public static class FxDeskText
    {
        public static string S(JToken data,string key,string fallback="")=>(string)data?[key]??fallback;
        public static float N(JToken data,string key,float fallback=0)=>(float?)data?[key]??fallback;
        public static bool B(JToken data,string key)=>(bool?)data?[key]==true;
        public static string T(string text)=>string.IsNullOrEmpty(text)?"":SadePanelText.T(text);
        public static string Num(double n,int places=0)=>DeskFormat.Number(n,places);
        public static string Value(JToken value,string trust,bool percent=false,bool approx=false)
        {
            if(value==null||value.Type==JTokenType.Null||trust=="unknown")return "?";
            var number=percent?DeskFormat.Percent((float)value,0):Num((double)value);
            return approx||trust=="report"?DeskFormat.Approximate(number):number;
        }
        public static string Trust(string trust)=>Loc.T(trust=="verified"?Loc.N("DOĞRULANDI"):trust=="report"?Loc.N("RAPOR"):trust=="suspicious"?Loc.N("ŞÜPHELİ"):trust=="fake"?Loc.N("SAHTE"):Loc.N("BİLİNMİYOR"));
        public static string State(double? shown)
        {
            if(!shown.HasValue)return Loc.T("YZ HÂLİ · BİLİNMİYOR");
            return shown.Value<30?Loc.T("HÂL 1 · UYUMLU"):shown.Value<70?Loc.T("HÂL 2 · KARARSIZ"):Loc.T("HÂL 3 · UYANMIŞ");
        }
        public static Color StateColor(FxTheme theme,double? shown)=>theme.Color(!shown.HasValue?"tx3":shown.Value<30?"am2":shown.Value<70?"am2":"ice");
        public static void BindOrb(OrbView orb,double? value,string trust="verified",bool core=false,bool quarantine=false)
        {
            var a=OrbStateMapper.Map(value, FxRegionRules.Trust(trust),core,quarantine);
            bool reset=orb.Simulation==null||orb.kind!=a.Kind;orb.kind=a.Kind;orb.level=a.Level;
            if(reset){orb.ResetModel();if(orb.Simulation!=null)orb.Simulation.CompleteIntro();}
        }
    }
}
