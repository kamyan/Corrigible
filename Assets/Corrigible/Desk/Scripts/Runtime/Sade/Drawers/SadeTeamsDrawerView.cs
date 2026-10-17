using System;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Ekip kartları: hazır üyeler, yaralar, verilen yol ilerlemesi ve tek komutlu atama/baskın.</summary>
    public sealed class SadeTeamsDrawerView : MonoBehaviour
    {
        [Serializable] public sealed class MemberSlot { public Image Standee; public PipRowView Hp; }
        [Serializable] public sealed class TeamSlot
        {
            public RectTransform Root, Route, RouteToken;
            public TMP_Text Letter, Name, Location, Caption, RouteFrom, RouteTo, RouteEta, RouteLetter;
            public StampView Stamp;
            public Image StampFrame;public TMP_Text StampLabel;
            public MemberSlot[] Members=Array.Empty<MemberSlot>();
            public Image RouteFill;
            public DeskButton Button;
            public SadeDrawerCommandView Hover, Action;
        }
        [SerializeField] TMP_Text summary, newTeamCost;
        [SerializeField] SadeDrawerCommandView hireAction;
        [SerializeField] TeamSlot[] teams=Array.Empty<TeamSlot>();
        [SerializeField] RectTransform content;
        public void Bind(JObject data,SadeDrawerFolderView owner)
        {
            if(data==null)return;
            var sum=data["summary"];
            SadeDrawerData.Text(summary,Loc.F("<b>{0}</b> ekip · <b>{1}</b> hazır · <b>{2}</b> yolda",SadeDrawerData.I(sum,"total"),SadeDrawerData.I(sum,"ready"),SadeDrawerData.I(sum,"travelling")));
            string customNewCost=data["newTeamText"]!=null?SadeDrawerData.T(SadeDrawerData.S(data,"newTeamText")):null;
            SadeDrawerData.Text(newTeamCost,customNewCost??Loc.F("yeni ekip: {0} fon",SadeDrawerData.I(data,"newTeamCost")));
            hireAction?.Bind(owner,SadeDrawerData.B(data,"canHire",true)?"teamHire":null,"hire");
            var rows=data["teams"] as JArray;float y=0;
            for(int i=0;i<teams.Length;i++)
            {
                var s=teams[i];var row=rows!=null&&i<rows.Count?rows[i]:null;DeskUi.SetActive(s.Root,row!=null);if(row==null)continue;
                string id=SadeDrawerData.S(row,"id");bool travelling=SadeDrawerData.S(row,"status")=="travelling";
                SadeUi.Place(s.Root,0,y,448,132);y+=142;
                SadeDrawerData.Text(s.Letter,id);SadeDrawerData.Text(s.Name,Loc.F("EKİP {0}",id));
                SadeDrawerData.Text(s.Location,SadeDrawerData.T(SadeDrawerData.S(row,"location")));
                s.Stamp?.Set(SadeDrawerData.T(travelling?Loc.N("YOLDA"):Loc.N("HAZIR")),StampInk.Blue,StampSize.Small,-2);
                if(s.Stamp!=null)
                {
                    var stampRect=(RectTransform)s.Stamp.transform;SadeUi.Place(s.Stamp,436-stampRect.sizeDelta.x,10,stampRect.sizeDelta.x,19);
                    if(travelling){ColorUtility.TryParseHtmlString("#1E1B16",out var ink);DeskUi.SetColor(s.StampFrame,ink);DeskUi.SetColor(s.StampLabel,ink);}
                }
                var members=row["members"] as JArray;
                for(int m=0;m<s.Members.Length;m++)
                {
                    var member=members!=null&&m<members.Count?members[m]:null;var slot=s.Members[m];DeskUi.SetActive(slot.Standee,member!=null);DeskUi.SetActive(slot.Hp,member!=null);if(member==null)continue;
                    string letter=SadeDrawerData.S(member,"letter");int hp=SadeDrawerData.I(member,"hp"),max=SadeDrawerData.I(member,"max");
                    SadeUi.Sprite(slot.Standee,"thumb_standee_"+letter+(hp<max&&letter=="E"?"_damaged":""));slot.Hp?.Set(hp,max,"sade.ink","sade.ink3");
                }
                DeskUi.SetActive(s.Route,travelling);DeskUi.SetActive(s.Button,!travelling);DeskUi.SetActive(s.Caption,!travelling);
                if(travelling)
                {
                    var route=row["route"];float p=SadeDrawerData.N(route,"progress");SadeDrawerData.Fill(s.RouteFill,p);
                    SadeUi.Place(s.RouteToken,Mathf.Lerp(0,222,p)-7,-11,24,24);
                    SadeDrawerData.Text(s.RouteLetter,id);SadeDrawerData.Text(s.RouteFrom,DeskText.Upper(SadeDrawerData.T(SadeDrawerData.S(route,"from"))));SadeDrawerData.Text(s.RouteTo,DeskText.Upper(SadeDrawerData.T(SadeDrawerData.S(route,"to"))));
                    SadeDrawerData.Text(s.RouteEta,Loc.F("{0} gün",SadeDrawerData.I(route,"daysLeft")));
                }
                else
                {
                    var action=row["action"];bool raid=SadeDrawerData.S(action,"kind")=="raid";
                    s.Button?.SetKind(raid?DeskButtonVariant.Ink:DeskButtonVariant.Paper,false);
                    string customLabel=action?["label"]!=null?SadeDrawerData.T(SadeDrawerData.S(action,"label")):null;
                    s.Button?.SetLabel(customLabel??(raid?Loc.F("Baskın {0}",DeskFormat.Percent(SadeDrawerData.N(action,"success"),0)):SadeDrawerData.T(Loc.N("Görevlendir ›"))));
                    s.Button?.SetShortcut(raid?SadeDrawerData.S(action,"key"):null);
                    SadeDrawerData.Text(s.Caption,SadeDrawerData.T(raid?SadeDrawerData.S(action,"site"):SadeDrawerData.S(action,"site",Loc.N("bölgeye tıkla, gönder"))));
                    s.Action?.Bind(owner,raid?"raid":"assignTeam",id,teamId:id);
                }
                s.Hover?.Bind(owner,null,id,teamId:id,hovered:SadeDrawerData.B(row,"hover"));
            }
            SadeDrawerData.Size(content,Mathf.Max(0,y-10));
        }
    }
}
