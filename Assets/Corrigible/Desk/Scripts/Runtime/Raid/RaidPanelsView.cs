using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Desk
{
    /// <summary>F6 durum kartları. İçerik, tehdit ve sonuç veri kaynağından gelir.</summary>
    public sealed class RaidPanelsView : MonoBehaviour
    {
        [SerializeField] GameObject eventSlip,confirmNote,logDrawer,rulesCard,retreatConfirm,resultMemo,pauseCard;
        [SerializeField] TMP_Text eventTitle,eventText,speedLabel,confirmTitle,confirmFoot,logTitle,rulesTitle,retreatTitle,retreatBody,retreatFoot,resultEyebrow,resultTitle,resultSubtitle,resultRows,resultFoot,bigStamp,pauseLabel;
        [SerializeField] RectTransform legendRoot;
        [SerializeField] TMP_Text[] queueLabels,unusedRows,ruleRows,legendRows;
        [SerializeField] Image[] queueFills,queueCancel;
        [SerializeField] Button closeLog,closeRules,cancelRetreat,retreatAgain,continueButton,resumeButton;
        [SerializeField] TMP_Text retreatAgainLabel,continueLabel,resumeLabel;
        [SerializeField] ScrollRect logScroll;
        [SerializeField] RectTransform logContent;
        [SerializeField] RaidLogRowView logRowPrefab;
        [SerializeField] SpotlightGraphic dim;
        [SerializeField] RectTransform stampRect;
        IRaidCommands commands;RaidViewModel current;readonly List<RaidLogRowView> logRows=new List<RaidLogRowView>();string logKey;
        static readonly string[] Legend={"legend.move","legend.target","legend.hit","legend.lie","legend.channel","legend.wake","legend.types","legend.flag","legend.scanned","legend.armor"};
        public void Bind(IRaidCommands value)
        {
            commands=value;Button(closeLog,commands.ToggleLog);Button(closeRules,commands.ToggleRules);Button(cancelRetreat,commands.CancelPending);Button(retreatAgain,commands.RequestRetreat);Button(continueButton,commands.ContinueToReport);Button(resumeButton,commands.CancelPending);
        }
        static void Button(Button b,UnityEngine.Events.UnityAction action){b.onClick.RemoveAllListeners();b.onClick.AddListener(action);}
        public void Render(RaidViewModel vm)
        {
            current=vm;eventSlip.SetActive(vm.EventVisible&&!vm.Finished);confirmNote.SetActive(vm.EndConfirm&&!vm.Finished);logDrawer.SetActive(vm.OpenPanel=="log"&&!vm.Finished);rulesCard.SetActive(vm.OpenPanel=="rules"&&!vm.Finished);retreatConfirm.SetActive(vm.RetreatArmed&&!vm.Finished);retreatAgain.gameObject.SetActive(vm.RetreatArmed&&!vm.Finished);resultMemo.SetActive(vm.Finished);stampRect.gameObject.SetActive(vm.Finished);pauseCard.SetActive(vm.Paused&&!vm.Finished);
            eventTitle.text=Loc.T("SIRA");eventText.text=vm.EventLine;speedLabel.text=vm.FastMachines?Loc.T("Boşluk · normal hız"):Loc.T("Boşluk · hızlandır");
            for(int i=0;i<queueLabels.Length;i++)
            {
                bool on=i<vm.Queue.Length;queueFills[i].gameObject.SetActive(on);if(!on)continue;var q=vm.Queue[i];
                queueLabels[i].text=q.State=="done"?Loc.F("<s>{0}</s>",q.Order):q.Order.ToString();queueFills[i].color=DeskTheme.Active.Get(q.State=="now"?"ink.red":q.State=="cancel"?"sade.onion":"sade.index");queueLabels[i].color=DeskTheme.Active.Get(q.State=="now"?"sade.index":q.State=="done"?"ink.ink3":"ink.red");queueCancel[i].enabled=q.State=="cancel";
            }
            confirmTitle.text=Loc.T("HAKKI DURAN BİRİM");confirmFoot.text=Loc.T("Enter yine de bitir · Esc vazgeç");
            for(int i=0;i<unusedRows.Length;i++)
            {
                unusedRows[i].gameObject.SetActive(i<vm.Unused.Length);if(i>=vm.Unused.Length)continue;var u=vm.Unused[i];
                string rights=u.Move&&u.Ability?Loc.T("hareket + yetenek"):u.Move?Loc.T("hareket"):Loc.T("yetenek");
                unusedRows[i].text=u.ThreatOrder>0?Loc.F("{0} · {1} kullanılmadı\n#{2} dron onu vuracak · −1",u.Name,rights,u.ThreatOrder):Loc.F("{0} · {1} kullanılmadı",u.Name,rights);
            }
            ((RectTransform)confirmNote.transform).sizeDelta=new Vector2(330,Mathf.Max(143,72+vm.Unused.Length*52));
            logTitle.text=Loc.T("OPERASYON GÜNLÜĞÜ");rulesTitle.text=Loc.T("KURALLAR KARTI");for(int i=0;i<ruleRows.Length;i++)ruleRows[i].text=RaidText.Get("rules."+(i+1));for(int i=0;i<legendRows.Length;i++)legendRows[i].text=RaidText.Get(Legend[i]);
            if(vm.IsLootMission&&ruleRows.Length>2)ruleRows[2].text=Loc.T("Teknisyen kasayı açar. İtme ve patlamalar kasaya hasar verebilir; EMP kasayı açmaz.");
            if(vm.IsLootMission&&ruleRows.Length>5)ruleRows[5].text=Loc.T("İkmal görevi: Teknisyen ile kasaya yaklaş, Q ile yeteneği seç ve kasayı aç. İmha edilen kasa görevi kaybettirir.");
            float ry=52;foreach(var t in ruleRows){float h=Mathf.Max(30,t.GetPreferredValues(t.text,366,float.PositiveInfinity).y+8);var r=(RectTransform)t.transform.parent;r.anchoredPosition=new Vector2(14,-ry);r.sizeDelta=new Vector2(392,h);t.rectTransform.sizeDelta=new Vector2(366,h);ry+=h;}
            if(legendRoot!=null){legendRoot.anchoredPosition=new Vector2(14,-ry-8);((RectTransform)rulesCard.transform).sizeDelta=new Vector2(420,Mathf.Max(455.5f,ry+160));}
            retreatTitle.text=RaidText.Get("raid.retreat.title");retreatBody.text=RaidText.Get("raid.retreat.body");retreatFoot.text=RaidText.Get("raid.retreat.foot");retreatAgainLabel.text=Loc.T("EMİN MİSİN? TEKRAR BAS");
            continueLabel.text=Loc.T("Operasyon raporuna geç");resumeLabel.text=Loc.T("Devam et [Esc]");pauseLabel.text=Loc.T("DURAKLATILDI");
            dim.gameObject.SetActive(vm.Finished||vm.RetreatArmed||vm.Paused);dim.color=DeskTheme.Active.Get("sade.plaque.bg");var col=dim.color;col.a=vm.Finished ? .74f : .32f;dim.color=col;
            if(!vm.Finished)dim.SetSpot(Vector2.zero,150,14,Color.clear,false);
            if(vm.Finished)
            {
                resultEyebrow.text=vm.Success?Loc.T("OPERASYON SONUCU · BAŞARILI"):Loc.T("OPERASYON SONUCU · BAŞARISIZ");resultTitle.text=bigStamp.text=vm.ResultTitle;resultSubtitle.text=vm.ResultSubtitle;
                resultTitle.color=bigStamp.color=DeskTheme.Active.Get(vm.Success?"ink.green":"ink.red");
                var targetRows=vm.Targets.Select(t=>Loc.F("{0} · {1}",t.Name,t.Broken?(vm.IsLootMission&&vm.Success?Loc.T("alındı"):Loc.T("kırıldı")):Loc.F("{0}/{1}",t.Hp,t.Max))).ToList();targetRows.Add(Loc.F("Kırılan sivil sunucu · {0}",vm.CivilianBroken));targetRows.Add(Loc.F("Ekip · {0}/{1} ayakta",vm.Standing,vm.TeamCount));resultRows.text=string.Join("\n",targetRows);resultFoot.text=vm.ResultReason??Loc.T("Operasyon raporu bölgeye etkisini ve ekibin durumunu gösterecek.");
            }
            if(logDrawer.activeSelf)RenderLog(vm.Log);
        }
        void RenderLog(RaidLogModel[] lines)
        {
            string key=Loc.Lang+"|"+string.Join("|",lines.Select(l=>l.Kind+l.Text));if(key==logKey)return;logKey=key;
            bool follow=logScroll.verticalNormalizedPosition<.05f;float width=logContent.rect.width;float total=0;var heights=new float[lines.Length];
            while(logRows.Count<lines.Length)logRows.Add(Instantiate(logRowPrefab,logContent));
            for(int i=0;i<logRows.Count;i++){logRows[i].gameObject.SetActive(i<lines.Length);if(i<lines.Length){heights[i]=logRows[i].Set(lines[i],width);total+=heights[i]+3;}}
            float contentHeight=Mathf.Max(logScroll.viewport.rect.height,total);logContent.sizeDelta=new Vector2(0,contentHeight);float y=contentHeight-total;
            for(int i=0;i<lines.Length;i++){((RectTransform)logRows[i].transform).anchoredPosition=new Vector2(0,-y);y+=heights[i]+3;}if(follow)logScroll.verticalNormalizedPosition=0;
        }
    }
}
