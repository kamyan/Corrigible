using System;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Dört dalın hazır araştırma ağacı; durumlar ve açılma koşulları yalnızca görünüm modelinden gelir.</summary>
    public sealed class ResearchTreeView : MonoBehaviour
    {
        [Serializable] public sealed class NodeSlot
        {
            public GameObject Root;
            public Image Halo, Shadow, Disc, Ring, Track, Progress, Glyph, NameHighlight, StatusIcon;
            public TMP_Text Name, Status, Percent;
            public SadeDrawerCommandView Action;
        }
        [Serializable] public sealed class BranchSlot
        {
            public GameObject Root, Goal;
            public TMP_Text Name, Done, Percent;
            public Image GoalFill, GoalIcon;
            public NodeSlot[] Nodes = Array.Empty<NodeSlot>();
            public Image[] Links = Array.Empty<Image>();
        }
        [SerializeField] BranchSlot[] branches = Array.Empty<BranchSlot>();
        public void Bind(JArray data, SadeDrawerFolderView owner)
        {
            for (int i = 0; i < branches.Length; i++)
            {
                var slot = branches[i]; var row = data != null && i < data.Count ? data[i] : null;
                DeskUi.SetActive(slot.Root, row != null); if (row == null) continue;
                SadeDrawerData.Text(slot.Name, SadeDrawerData.T(SadeDrawerData.S(row, "nameKey")));
                SadeDrawerData.Text(slot.Done, Loc.F("{0}/{1} bitti", SadeDrawerData.I(row,"done"), SadeDrawerData.I(row,"total")));
                bool goal = row["goalPercent"]?.Type != JTokenType.Null && row["goalPercent"] != null;
                DeskUi.SetActive(slot.Goal, goal);
                if (goal)
                {
                    string tint = SadeDrawerData.S(row,"goalColor") == "good" ? "sade.status.good" : "sade.status.info";
                    DeskUi.Tint(slot.Percent, tint); DeskUi.Tint(slot.GoalFill, tint); DeskUi.Tint(slot.GoalIcon, tint);
                    float p = SadeDrawerData.N(row,"goalPercent");
                    SadeDrawerData.Text(slot.Percent, DeskFormat.Percent(p,0)); SadeDrawerData.Fill(slot.GoalFill,p/100);
                }
                var nodes = SadeDrawerData.A(row,"nodes");
                for (int n = 0; n < slot.Nodes.Length; n++) BindNode(slot.Nodes[n], nodes != null && n < nodes.Count ? nodes[n] : null, owner);
                for (int n = 0; n < slot.Links.Length; n++)
                {
                    bool shown = nodes != null && n+1 < nodes.Count; DeskUi.SetActive(slot.Links[n],shown);
                    if (!shown) continue;
                    bool locked = SadeDrawerData.S(nodes[n+1],"state") == "locked";
                    SadeUi.Sprite(slot.Links[n], locked ? "ln_dash_h" : "sh_fill_r0");
                    slot.Links[n].color = locked ? ColorOf("#8E8573") : ColorOf("#463F34");
                }
            }
        }
        static Color ColorOf(string hex) { ColorUtility.TryParseHtmlString(hex,out var c); return c; }
        static void BindNode(NodeSlot slot, JToken node, SadeDrawerFolderView owner)
        {
            DeskUi.SetActive(slot.Root,node != null); if(node == null) return;
            string state = SadeDrawerData.S(node,"state"), id = SadeDrawerData.S(node,"id");
            bool done=state=="done", running=state=="running", paused=state=="paused", locked=state=="locked", selected=SadeDrawerData.B(node,"selected");
            DeskUi.SetActive(slot.Shadow,done||running||paused); DeskUi.SetActive(slot.Halo,selected); DeskUi.SetActive(slot.NameHighlight,selected);
            slot.Disc.color=ColorOf(done?"#24592E":locked?"#E3DACA":"#FBF7EC");
            SadeUi.Sprite(slot.Ring, state=="available"?"rn_ring_dash_42":"rn_ring_42_w1_5");
            slot.Ring.color=ColorOf(done?"#163A1D":locked?"#A79D88":"#463F34");
            DeskUi.SetActive(slot.Track,running||paused); DeskUi.SetActive(slot.Progress,running||paused); DeskUi.SetActive(slot.Percent,running); DeskUi.SetActive(slot.Glyph,!running);
            if(running||paused)
            {
                SadeDrawerData.Fill(slot.Progress,SadeDrawerData.N(node,"progress"));
                if(slot.Progress!=null)slot.Progress.color=ColorOf(paused?"#675F50":"#1A4789");
                SadeDrawerData.Text(slot.Percent,DeskFormat.Percent(SadeDrawerData.N(node,"progress")*100,0));
            }
            if(!running)
            { float glyphSize=done?26:locked||paused?18:20;SadeUi.Place(slot.Glyph,(52-glyphSize)/2,(52-glyphSize)/2,glyphSize,glyphSize);SadeUi.Sprite(slot.Glyph,done?"mk_check_bold_26":locked?"ic_lock_l":paused?"ic_pause_l":"mk_plus_bold_20"); slot.Glyph.color=done?Color.white:ColorOf(locked?"#8E8573":"#463F34"); }
            string name=SadeDrawerData.T(SadeDrawerData.S(node,"name"));
            float h=Mathf.Min(30,SadeDrawerData.Wrapped(slot.Name,name,118,14));
            slot.Name.color=ColorOf(locked?"#675F50":done?"#463F34":"#1E1B16");
            SadeUi.Place(slot.Name,-33,55,118,h);
            if(slot.NameHighlight!=null)SadeUi.Place(slot.NameHighlight,-37,54,126,h+2);
            string status=done?SadeDrawerData.T(Loc.N("bitti")):locked?SadeResearchDrawerView.RequirementText(node["requirement"]):paused?SadeDrawerData.T(Loc.N("duraklatıldı")):node["statusText"]!=null?SadeDrawerData.T(SadeDrawerData.S(node,"statusText")):Loc.F("{0} gün",SadeDrawerData.I(node,running?"daysLeft":"days"));
            SadeDrawerData.Text(slot.Status,status); slot.Status.color=ColorOf(done?"#24592E":running?"#1A4789":"#675F50");
            float statusWidth=slot.Status!=null?slot.Status.GetPreferredValues(status).x:30;float statusX=26-(statusWidth+15)/2;
            SadeUi.Place(slot.Status,statusX+15,55+h+3,statusWidth+1,12);
            SadeUi.Place(slot.StatusIcon,statusX,55+h+3,11,11);
            SadeUi.Sprite(slot.StatusIcon,done?"ic_check_s":locked?"ic_lock_s":paused?"ic_pause_s":"ic_clock_s"); if(slot.StatusIcon!=null)slot.StatusIcon.color=slot.Status.color;
            slot.Action?.Bind(owner,"researchSelect",id);
        }
    }
}
