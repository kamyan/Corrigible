using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Desk
{
    /// <summary>
    /// F6 durum kartları. İçerik, tehdit ve sonuç veri kaynağından gelir. Olay fişi telsiz şerididir: baskın boyunca açık,
    /// son konuşmaları gösterir (en yenisi altta, daktilo gibi yazılır); sıra kutuları ve hız yalnız makine evresinde.
    /// </summary>
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
        const int RadioLines=4;const float TypeSpeed=90;
        string radioKey,radioOlder;float typed;int typedTo;
        static readonly string[] Legend={"legend.move","legend.target","legend.hit","legend.channel","legend.wake","legend.types","legend.flag","legend.armor"};
        public void Bind(IRaidCommands value)
        {
            commands=value;Button(closeLog,commands.ToggleLog);Button(closeRules,commands.ToggleRules);Button(cancelRetreat,commands.CancelPending);Button(retreatAgain,commands.RequestRetreat);Button(continueButton,commands.ContinueToReport);Button(resumeButton,commands.CancelPending);
        }
        static void Button(Button b,UnityEngine.Events.UnityAction action){b.onClick.RemoveAllListeners();b.onClick.AddListener(action);}
        public void Render(RaidViewModel vm)
        {
            current=vm;eventSlip.SetActive(vm.Radio.Length>0&&!vm.Finished);confirmNote.SetActive(vm.EndConfirm&&!vm.Finished);logDrawer.SetActive(vm.OpenPanel=="log"&&!vm.Finished);rulesCard.SetActive(vm.OpenPanel=="rules"&&!vm.Finished);retreatConfirm.SetActive(vm.RetreatArmed&&!vm.Finished);retreatAgain.gameObject.SetActive(vm.RetreatArmed&&!vm.Finished);resultMemo.SetActive(vm.Finished);stampRect.gameObject.SetActive(vm.Finished);pauseCard.SetActive(vm.Paused&&!vm.Finished);
            eventTitle.text=Loc.T("TELSİZ");RenderRadio(vm.Radio);speedLabel.text=vm.FastMachines?Loc.T("Boşluk · normal hız"):Loc.T("Boşluk · hızlandır");speedLabel.gameObject.SetActive(vm.EventVisible);
            // Sıra kutuları başlığın ardından dizilir: başlığın genişliği dile göre değişir
            float qx=Mathf.Max(54,eventTitle.rectTransform.anchoredPosition.x+eventTitle.GetPreferredValues(eventTitle.text).x+6);
            for(int i=0;i<queueFills.Length;i++){var qr=queueFills[i].rectTransform;qr.anchoredPosition=new Vector2(qx+i*25,qr.anchoredPosition.y);}
            for(int i=0;i<queueLabels.Length;i++)
            {
                bool on=vm.EventVisible&&i<vm.Queue.Length;queueFills[i].gameObject.SetActive(on);if(!on)continue;var q=vm.Queue[i];
                queueLabels[i].text=q.State=="done"?Loc.F("<s>{0}</s>",q.Order):q.Order.ToString();queueFills[i].color=DeskTheme.Active.Get(q.State=="now"?"ink.red":q.State=="cancel"?"sade.onion":"sade.index");queueLabels[i].color=DeskTheme.Active.Get(q.State=="now"?"sade.index":q.State=="done"?"ink.ink3":"ink.red");queueCancel[i].enabled=q.State=="cancel";
            }
            confirmTitle.text=Loc.T("HAKKI DURAN BİRİM");confirmFoot.text=Loc.T("Enter yine de ilet · Esc vazgeç");
            for(int i=0;i<unusedRows.Length;i++)
            {
                unusedRows[i].gameObject.SetActive(i<vm.Unused.Length);if(i>=vm.Unused.Length)continue;var u=vm.Unused[i];
                string rights=u.Move&&u.Ability?Loc.T("hareket + yetenek"):u.Move?Loc.T("hareket"):Loc.T("yetenek");
                unusedRows[i].text=u.ThreatOrder>0?Loc.F("{0} · {1} kullanılmadı\n#{2} dron onu vuracak · −1",u.Name,rights,u.ThreatOrder):Loc.F("{0} · {1} kullanılmadı",u.Name,rights);
            }
            ((RectTransform)confirmNote.transform).sizeDelta=new Vector2(330,Mathf.Max(143,72+vm.Unused.Length*52));
            logTitle.text=Loc.T("TELSİZ KAYDI");rulesTitle.text=Loc.T("KURALLAR KARTI");for(int i=0;i<ruleRows.Length;i++)ruleRows[i].text=RaidText.Get("rules."+(i+1));for(int i=0;i<legendRows.Length&&i<Legend.Length;i++)legendRows[i].text=RaidText.Get(Legend[i]);
            if(vm.IsLootMission&&ruleRows.Length>2)ruleRows[2].text=Loc.T("Teknisyen kasayı açar ve malzemeyi taşır. İtme ve patlamalar kasaya hasar verebilir; EMP kasayı açmaz.");
            if(vm.IsLootMission&&ruleRows.Length>5)ruleRows[5].text=Loc.T("İkmal görevi: Teknisyen ile kasayı aç, sonra malzemeyi giriş karelerinden birine geri taşı. Kasa imha olursa, Teknisyen düşerse ya da süre biterse görev kaybedilir.");
            float ry=52;foreach(var t in ruleRows){float h=Mathf.Max(30,t.GetPreferredValues(t.text,366,float.PositiveInfinity).y+8);var r=(RectTransform)t.transform.parent;r.anchoredPosition=new Vector2(14,-ry);r.sizeDelta=new Vector2(392,h);t.rectTransform.sizeDelta=new Vector2(366,h);ry+=h;}
            if(legendRoot!=null){legendRoot.anchoredPosition=new Vector2(14,-ry-8);((RectTransform)rulesCard.transform).sizeDelta=new Vector2(420,Mathf.Max(431.5f,ry+136));}
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
        static string RadioLine(RaidRadioLine l)=>Loc.F("<b>{0} ›</b> {1}",l.Who,l.Text);
        /// <summary>Telsiz şeridi: son satırlar; eskiler soluk. Yeni satır geldiyse daktilo baştan o satırdan yazar.</summary>
        void RenderRadio(RaidRadioLine[] lines)
        {
            var shown=lines.Skip(Math.Max(0,lines.Length-RadioLines)).ToArray();
            // TMP'de alfa etiketinin kapanışı yok: eski satırlar soluk açılır, en yeni satır tam alfayla
            string older=string.Join("\n",shown.Take(Math.Max(0,shown.Length-1)).Select(l=>"<alpha=#8C>"+RadioLine(l)));
            string text=shown.Length==0?"":(older.Length>0?older+"\n":"")+"<alpha=#FF>"+RadioLine(shown[shown.Length-1]);
            string key=Loc.Lang+"|"+text;if(key==radioKey)return;
            bool newLine=older!=radioOlder;radioKey=key;radioOlder=older;
            eventText.text=older;eventText.ForceMeshUpdate();int from=older.Length>0?eventText.textInfo.characterCount:0;
            eventText.text=text;eventText.ForceMeshUpdate();typedTo=eventText.textInfo.characterCount;
            // Aynı satır uzadıysa (hasar eklendi) daktilo kaldığı yerden sürer
            typed=newLine?from:Mathf.Min(typed,typedTo);
            eventText.maxVisibleCharacters=Mathf.FloorToInt(typed);
            float h=Mathf.Max(43,eventText.GetPreferredValues(text,eventText.rectTransform.rect.width,float.PositiveInfinity).y);
            eventText.rectTransform.sizeDelta=new Vector2(eventText.rectTransform.sizeDelta.x,h);
            ((RectTransform)eventSlip.transform).sizeDelta=new Vector2(((RectTransform)eventSlip.transform).sizeDelta.x,-eventText.rectTransform.anchoredPosition.y+h+10);
        }
        void Update()
        {
            if(eventText==null||typed>=typedTo)return;
            typed=Mathf.Min(typedTo,typed+TypeSpeed*Time.unscaledDeltaTime);eventText.maxVisibleCharacters=Mathf.FloorToInt(typed);
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
