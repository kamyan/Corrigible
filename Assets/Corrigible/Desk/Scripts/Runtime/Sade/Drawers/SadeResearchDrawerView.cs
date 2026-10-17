using System;
using System.Globalization;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Araştırma özeti, fon rezervi ve seçili düğüm şeridi. Tüm tutarlar gösterim modelinin değerleridir.</summary>
    public sealed class SadeResearchDrawerView : MonoBehaviour
    {
        [Serializable] public sealed class ReserveSlot { public TMP_Text Value, Caption; public Image Fill; public SadeDrawerCommandView Action; }
        [SerializeField] TMP_Text summary, dailyCost, reserveTitle;
        [SerializeField] ReserveSlot[] reserves = Array.Empty<ReserveSlot>();
        [SerializeField] ResearchTreeView tree;
        [SerializeField] TMP_Text detailName, detailBranch, detailPercent, detailDays, detailCost, detailUnlock;
        [SerializeField] Image detailProgress;
        [SerializeField] Image detailArrow, detailLock;
        [SerializeField] DeskButton detailButton;
        [SerializeField] SadeDrawerCommandView detailAction;
        [SerializeField] RectTransform content, detailRoot;
        public void Bind(JObject data, SadeDrawerFolderView owner)
        {
            if(data==null)return;
            SadeDrawerData.Text(summary,Loc.F("<b>{0} proje</b> sürüyor",SadeDrawerData.I(data,"activeCount")));
            SadeDrawerData.Text(dailyCost,Loc.F("{0} fon/gün",DeskFormat.Number(SadeDrawerData.N(data,"costPerDay"),1)));
            SadeDrawerData.Text(reserveTitle,SadeDrawerData.T(Loc.N("FON")));
            var reserve=data["reserve"] as JObject; var options=reserve?["options"] as JArray;
            float selected=(float?)reserve?["selected"]??15;
            for(int i=0;i<reserves.Length;i++)
            {
                var slot=reserves[i]; var option=options!=null&&i<options.Count?options[i]:null;
                float value=option is JObject?SadeDrawerData.N(option,"value",i==0?0:i==1?15:30):option?.Type==JTokenType.Integer||option?.Type==JTokenType.Float?(float)option:i==0?0:i==1?15:30;
                string caption=option is JObject?SadeDrawerData.S(option,"label"):null;
                bool active=Mathf.Approximately(selected,value);
                slot.Fill.color=active?ColorOf("#1E1B16"):Color.clear;
                slot.Value.color=active?ColorOf("#F4ECD8"):ColorOf("#1E1B16");slot.Caption.color=active?ColorOf("#CFC4AC"):ColorOf("#675F50");
                SadeDrawerData.Text(slot.Value,Loc.F("{0} fon",DeskFormat.Number(value,0)));
                SadeDrawerData.Text(slot.Caption,SadeDrawerData.T(caption??Loc.N("acil durum fonu")));
                slot.Action?.Bind(owner,"reserve",value.ToString(CultureInfo.InvariantCulture));
            }
            var branchArr = data["branches"] as JArray;
            tree?.Bind(branchArr,owner);
            var detail=data["detail"];
            if(detail!=null)
            {
                SadeDrawerData.Text(detailName,SadeDrawerData.T(SadeDrawerData.S(detail,"name")));
                SadeDrawerData.Text(detailBranch,Loc.F("{0} · {1}",SadeDrawerData.T(SadeDrawerData.S(detail,"branchKey")),SadeDrawerData.S(detail,"step")));
                float p=SadeDrawerData.N(detail,"progress"); SadeDrawerData.Fill(detailProgress,p);
                SadeDrawerData.Text(detailPercent,DeskFormat.Percent(p*100,0));
                bool canPause=SadeDrawerData.B(detail,"canPause");
                string state=SadeDrawerData.S(detail,"state",canPause?"running":"available");
                bool locked=state=="locked",done=state=="done",paused=state=="paused";
                bool running=state=="running";
                int days=SadeDrawerData.I(detail,"daysLeft",SadeDrawerData.I(detail,"days"));
                string condition=RequirementText(detail["requirement"]);
                string customDays=detail["dayText"]!=null?SadeDrawerData.T(SadeDrawerData.S(detail,"dayText")):null;
                string dayText=customDays??(locked?condition:done?SadeDrawerData.T(Loc.N("bitti")):running?Loc.F("{0} gün kaldı",days):Loc.F("{0} gün",days));
                SadeDrawerData.Text(detailDays,dayText);
                if(detailDays!=null)
                {
                    detailDays.fontSize=locked?11:14;
                    detailDays.textWrappingMode=locked?TextWrappingModes.Normal:TextWrappingModes.NoWrap;
                    detailDays.color=ColorOf(locked||paused?"#675F50":done?"#24592E":"#1A4789");
                    SadeUi.Place(detailDays,342,locked?40:42,92,locked?28:18);
                }
                string customCost=detail["costText"]!=null?SadeDrawerData.T(SadeDrawerData.S(detail,"costText")):null;
                SadeDrawerData.Text(detailCost,customCost??Loc.F("{0} fon/gün",DeskFormat.Number(SadeDrawerData.N(detail,"costPerDay"),1)));
                float costWidth=Mathf.Max(83,Mathf.Ceil(SadeUi.Width(detailCost,detailCost.text)));
                SadeUi.Place(detailCost,39,72,costWidth,18);
                float arrowX=39+costWidth+6;
                SadeUi.Place(detailArrow,arrowX,74,12,12);
                SadeUi.Place(detailLock,arrowX+19,74,12,12);
                SadeUi.Place(detailUnlock,arrowX+34,72,Mathf.Max(40,350-arrowX-34),18);
                SadeDrawerData.Text(detailUnlock,SadeDrawerData.T(SadeDrawerData.S(detail,"unlocks")));
                string btnLabel=detail["buttonLabel"]!=null?SadeDrawerData.T(SadeDrawerData.S(detail,"buttonLabel")):SadeDrawerData.T(running?Loc.N("Duraklat"):paused?Loc.N("Devam et"):done?Loc.N("Tamamlandı"):Loc.N("Başlat"));
                detailButton?.SetLabel(btnLabel);
                string disabledReason=detail["disabledReason"]!=null?SadeDrawerData.T(SadeDrawerData.S(detail,"disabledReason")):(locked?condition:done?SadeDrawerData.T(Loc.N("Tamamlandı")):null);
                detailButton?.SetDisabled(disabledReason);
                bool canAct=string.IsNullOrEmpty(disabledReason)&&!locked&&!done;
                detailAction?.Bind(owner,canAct?(running?"researchPause":"researchStart"):null,SadeDrawerData.S(detail,"nodeId"));
            }
            int branchCount = branchArr?.Count ?? 4;
            int extraBranches = Mathf.Max(0, branchCount - 4);
            if (extraBranches > 0 && detailRoot != null)
                SadeUi.Place(detailRoot, 0, 502 + extraBranches * 116, 448, 120);
            SadeDrawerData.Size(content, 622 + extraBranches * 116);
        }
        public static string RequirementText(JToken requirement)
        {
            if(requirement==null||requirement.Type==JTokenType.Null)return SadeDrawerData.T(Loc.N("kilitli"));
            if(requirement.Type==JTokenType.String)return SadeDrawerData.T((string)requirement);
            if(SadeDrawerData.S(requirement,"type")=="politicalPower")return Loc.F("SG {0}",SadeDrawerData.I(requirement,"min"));
            return SadeDrawerData.T(SadeDrawerData.S(requirement,"text",Loc.N("Önceki düğüm gerekli")));
        }
        static Color ColorOf(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
    }
}
