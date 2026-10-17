using System;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Teleks sayfası: haberlerin gerçekliğini hesaplamaz; modelde verilen güven işaretini ve metni çizer.</summary>
    public sealed class SadeNewsDrawerView : MonoBehaviour
    {
        [Serializable] public sealed class ItemSlot
        {
            public RectTransform Root, DayHeader, RegionChip;
            public TMP_Text Day, Source, Headline, Status, Region;
            public Image Mark, RegionDot, Strike;
            public TMP_Text Question, FakeStamp;
            public DeskButton Audit;
            public SadeDrawerCommandView Action, AuditAction;
        }
        [SerializeField] TMP_Text allLabel, unverifiedLabel, stamp;
        [SerializeField] Image allFill, unverifiedFill;
        [SerializeField] SadeDrawerCommandView allAction, unverifiedAction;
        [SerializeField] ItemSlot[] items=Array.Empty<ItemSlot>();
        [SerializeField] RectTransform content;
        [SerializeField] ScrollRect scroll;
        string filter="all";
        JObject model;
        SadeDrawerFolderView owner;
        public void SetFilter(string value) { if(model==null)return;var filtered=(JObject)model.DeepClone();filtered["filter"]=value;Bind(filtered,owner); }
        public void Bind(JObject data,SadeDrawerFolderView folder)
        {
            if(data==null)return;model=data;owner=folder;filter=SadeDrawerData.S(data,"filter","all");
            allAction?.Bind(folder,"newsFilter","all");unverifiedAction?.Bind(folder,"newsFilter","unverified");
            SadeDrawerData.Text(allLabel,Loc.F("Tümü {0}",SadeDrawerData.I(data["counts"],"all")));
            SadeDrawerData.Text(unverifiedLabel,Loc.F("● Doğrulanmamış {0}",SadeDrawerData.I(data["counts"],"unverified")));
            SadeDrawerData.Text(stamp,SadeDrawerData.S(data,"stamp"));
            allFill.color=filter=="all"?ColorOf("#1E1B16"):ColorOf("#FBF7EC");unverifiedFill.color=filter!="all"?ColorOf("#1E1B16"):ColorOf("#FBF7EC");
            allLabel.color=filter=="all"?ColorOf("#F4ECD8"):ColorOf("#1E1B16");unverifiedLabel.color=filter!="all"?ColorOf("#F4ECD8"):ColorOf("#1E1B16");
            float y=0;string lastDay=null;int n=0;var rows=data["items"] as JArray;
            if(rows!=null)foreach(var row in rows)
            {
                string trust=SadeDrawerData.S(row,"trust");if(filter!="all"&&!SadeDrawerData.B(row,"canAudit"))continue;if(n>=items.Length)break;
                var slot=items[n++];DeskUi.SetActive(slot.Root,true);
                string day=SadeDrawerData.S(row,"day");bool header=day!=lastDay;
                DeskUi.SetActive(slot.DayHeader,header);
                if(header){if(lastDay!=null)y+=8;SadeUi.Place(slot.DayHeader,0,y,420,22);SadeDrawerData.Text(slot.Day,Loc.F("{0}{1}",day,lastDay==null?SadeDrawerData.T(Loc.N(" · BUGÜN")):""));y+=24;lastDay=day;}
                float h=BindItem(slot,row,trust,folder);
                SadeUi.Place(slot.Root,0,y,420,h);y+=h;
            }
            for(;n<items.Length;n++){DeskUi.SetActive(items[n].Root,false);DeskUi.SetActive(items[n].DayHeader,false);}
            SadeDrawerData.Size(content,y+14);
        }
        float BindItem(ItemSlot s,JToken row,string trust,SadeDrawerFolderView folder)
        {
            string region=SadeDrawerData.S(row,"region"), id=SadeDrawerData.S(row,"id");bool fake=trust=="fake";
            SadeDrawerData.Text(s.Source,Loc.F("{0} · {1}",SadeDrawerData.T(SadeDrawerData.S(row,"source")),SadeDrawerData.S(row,"time")));
            SadeDrawerData.Text(s.Region,SadeDrawerData.Region(region));float chip=Mathf.Max(70,s.Region.GetPreferredValues(s.Region.text).x+27);
            SadeUi.Place(s.RegionChip,411-chip,10,chip,20);SadeUi.Place(s.Region,20,3,chip-24,14);
            if(s.RegionDot!=null)s.RegionDot.color=RegionColor(region);
            string headline=SadeDrawerData.T(SadeDrawerData.S(row,"headline"));
            float headlineH=SadeDrawerData.Wrapped(s.Headline,headline,362,18);headlineH=Mathf.Max(18,headlineH);
            SadeUi.Place(s.Headline,49,35,362,headlineH);
            s.Headline.color=ColorOf(fake?"#675F50":"#1E1B16");
            float bottom=35+headlineH+5,h=Mathf.Max(91,bottom+32);
            SadeUi.Place(s.Status,49,bottom+4,fake?320:260,18);
            string customStatus=row["statusText"]!=null?SadeDrawerData.T(SadeDrawerData.S(row,"statusText")):null;
            SadeDrawerData.Text(s.Status,customStatus??SadeDrawerData.T(Status(trust)));
            s.Status.color=ColorOf(trust=="verified"?"#1A4789":trust=="report"?"#514672":trust=="suspicious"?"#854005":"#675F50");
            DeskUi.SetActive(s.Question,trust=="suspicious"||trust=="unknown");
            DeskUi.SetActive(s.FakeStamp,fake);DeskUi.SetActive(s.Strike,fake);
            if(fake){SadeUi.Place(s.Strike,49,45,Mathf.Min(280,s.Headline.GetPreferredValues(headline).x),2);SadeDrawerData.Text(s.FakeStamp,SadeDrawerData.T(Loc.N("SAHTE")));}
            string mark=trust=="verified"?"node_badge_seal_20":trust=="report"?"legend_report":trust=="suspicious"?"mark_suspicious":trust=="fake"?"mark_fake_24":"ring_dash_24";
            SadeUi.Sprite(s.Mark,mark);s.Mark.color=Color.white;
            if(trust=="unknown"){s.Mark.color=ColorOf("#4A4640");SadeDrawerData.Text(s.Question,"?");}
            else if(trust=="suspicious")SadeDrawerData.Text(s.Question,"?");
            bool audit=SadeDrawerData.B(row,"canAudit");DeskUi.SetActive(s.Audit,audit);
            if(audit){s.Audit.SetLabel(SadeDrawerData.T(Loc.N("Denetle")));SadeUi.Place(s.Audit,332,bottom,79,22);s.AuditAction?.Bind(folder,"newsAudit",id);}
            s.Action?.Bind(folder,"newsSelect",id,region,hovered:SadeDrawerData.B(row,"hover"));
            return h;
        }
        public static string Status(string trust)=>trust switch
        {
            "verified"=>Loc.N("ONAYLANDI · ekip B · saha"),"report"=>Loc.N("rapor · doğrulanmadı"),"suspicious"=>Loc.N("şüpheli · kaynaklar çelişiyor"),"fake"=>Loc.N("Ekip B yalanladı · G-074"),_=>Loc.N("bilinmiyor · tek kaynak")
        };
        public static Color RegionColor(string region)=>ColorOf(region switch {"DoA" or "cn" or "jk"=>"#E58D77","AV" or "ru" or "eeu"=>"#B63728","GDA" or "sea" or "in"=>"#8B241B","BA" or "weu"=>"#EEC3B4",_=>"#D88974"});
        static Color ColorOf(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
    }
}
