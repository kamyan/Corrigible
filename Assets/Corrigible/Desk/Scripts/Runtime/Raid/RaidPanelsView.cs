using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Desk
{
    /// <summary>
    /// F6 durum kartları. İçerik, tehdit ve sonuç veri kaynağından gelir. Sonuç kartı (Revizyon 2 · S7; 500 px, x 1290 ·
    /// y 318, +0,8°): üstte “OPERASYON SONUCU · TUR 6/8”, başlık yerinde tek damga (çift kenar; başarıda mavi, değilse
    /// kırmızı), yer satırı, satırlar ve tek düğme. Son hamleden sonra tahta 0,4 s'de kararır, kart sağdan kayar (0,35–0,6 s),
    /// damga 0,12 s'de basılır (başarıda zil, değilse ağır “tak”); kart gelene kadar girdi kilitli (veri kaynağı).
    /// </summary>
    public sealed class RaidPanelsView : MonoBehaviour
    {
        [SerializeField] GameObject eventSlip,confirmNote,logDrawer,rulesCard,retreatConfirm,resultMemo,pauseCard;
        [SerializeField] TMP_Text eventTitle,eventText,speedLabel,confirmTitle,confirmFoot,logTitle,rulesTitle,retreatTitle,retreatBody,retreatFoot,pauseLabel;
        [Header("Sonuç kartı (S7)")]
        [SerializeField] RectTransform resultCard,resultStamp,continueFace;
        [SerializeField] TMP_Text resultHead,resultTurn,resultTitle,resultPlace;
        [SerializeField] Image resultStampFrame,continueShadow;
        [SerializeField] TMP_Text[] resultLabels,resultValues;
        [SerializeField] UiHoverTarget continueHover;
        [SerializeField] RectTransform legendRoot;
        [SerializeField] TMP_Text[] queueLabels,unusedRows,ruleRows,legendRows;
        [SerializeField] Image[] queueFills,queueCancel;
        [SerializeField] Button closeLog,closeRules,cancelRetreat,retreatAgain,continueButton,resumeButton;
        [SerializeField] TMP_Text retreatAgainLabel,continueLabel,resumeLabel;
        [SerializeField] ScrollRect logScroll;
        [SerializeField] RectTransform logContent;
        [SerializeField] RaidLogRowView logRowPrefab;
        [SerializeField] SpotlightGraphic dim;
        [Header("Kadro raporu (baskına girerken)")]
        [SerializeField] GameObject squadReport;
        [SerializeField] TMP_Text squadTitle;
        [SerializeField] TMP_Text[] squadNames,squadStatus;
        IRaidCommands commands;RaidViewModel current;readonly List<RaidLogRowView> logRows=new List<RaidLogRowView>();string logKey;
        // Sonuç dizisi: kararma 0–0,4 s, kart 0,35–0,6 s, damga 0,6–0,72 s
        const float DimSeconds=.4f,CardAt=.35f,CardSeconds=.25f,StampSeconds=.12f,DimAlpha=.74f;
        float finishedAt=-1;Vector2 cardRest;bool cardRested,stamped;
        /// <summary>Sonuç damgası basıldı (başarılı mı): bağlayıcı sesi çalar.</summary>
        public event System.Action<bool> ResultStamped;
        static readonly string[] Legend={"legend.move","legend.target","legend.hit","legend.channel","legend.wake","legend.types","legend.flag","legend.armor"};
        public void Bind(IRaidCommands value)
        {
            commands=value;Button(closeLog,commands.ToggleLog);Button(closeRules,commands.ToggleRules);Button(cancelRetreat,commands.CancelPending);Button(retreatAgain,commands.RequestRetreat);Button(continueButton,commands.ContinueToReport);Button(resumeButton,commands.CancelPending);
        }
        static void Button(Button b,UnityEngine.Events.UnityAction action){b.onClick.RemoveAllListeners();b.onClick.AddListener(action);}
        public void Render(RaidViewModel vm)
        {
            current=vm;eventSlip.SetActive(vm.EventVisible&&!vm.Finished);confirmNote.SetActive(vm.EndConfirm&&!vm.Finished);logDrawer.SetActive(vm.OpenPanel=="log"&&!vm.Finished);rulesCard.SetActive(vm.OpenPanel=="rules"&&!vm.Finished);retreatConfirm.SetActive(vm.RetreatArmed&&!vm.Finished);retreatAgain.gameObject.SetActive(vm.RetreatArmed&&!vm.Finished);resultMemo.SetActive(vm.Finished);pauseCard.SetActive(vm.Paused&&!vm.Finished);
            // Revizyon 2 · S6: teleks kâğıdı fişi (anten + TELSİZ · K-07); makinelerin sırası tahtadaki bayraklarda, hamle sayısı kilitli kâğıtta
            eventTitle.text=Loc.T("TELSİZ · K-07");eventText.text=vm.EventLine;if(speedLabel!=null)speedLabel.text=vm.FastMachines?Loc.T("Boşluk · normal hız"):Loc.T("Boşluk · hızlandır");
            // Sıra kutuları başlığın ardından dizilir: başlığın genişliği dile göre değişir
            float qx=Mathf.Max(54,eventTitle.rectTransform.anchoredPosition.x+eventTitle.GetPreferredValues(eventTitle.text).x+6);
            for(int i=0;i<queueFills.Length;i++){var qr=queueFills[i].rectTransform;qr.anchoredPosition=new Vector2(qx+i*25,qr.anchoredPosition.y);}
            for(int i=0;i<queueLabels.Length;i++)
            {
                bool on=i<vm.Queue.Length;queueFills[i].gameObject.SetActive(on);if(!on)continue;var q=vm.Queue[i];
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
            // Madenci ve Sis uzmanı kadrodaysa yetenekleri seçim satırının altında
            if(ruleRows.Length>1&&vm.Team!=null)
            {
                if(vm.Team.Any(t=>t.Letter=='M'))ruleRows[1].text+="\n"+Loc.T("Madenci: dinamit bitişik duvarı yıkar (baskında sınırlı); moloz bir kare öteye 1 hasar. Açılan delik makinelerin atış hattını da açar.");
                if(vm.Team.Any(t=>t.Letter=='S'))ruleRows[1].text+="\n"+Loc.T("Sis: bu turun makine evresinde atış ve kilit sise girince kaybolur; bakım robotu ve havan sisli kareye vuramaz. Patlamayı kesmez.");
            }
            if(vm.IsLootMission&&ruleRows.Length>5)ruleRows[5].text=Loc.T("İkmal görevi: Teknisyen ile kasayı aç, sonra bütün ekip giriş karelerinden çıksın; malzeme Teknisyen çıkınca alınır. Süre dolarken tahtada kalan uzman baskında düşmüş sayılır. Kasa imha olursa ya da Teknisyen çıkamazsa görev kaybedilir.");
            float ry=52;foreach(var t in ruleRows){float h=Mathf.Max(30,t.GetPreferredValues(t.text,366,float.PositiveInfinity).y+8);var r=(RectTransform)t.transform.parent;r.anchoredPosition=new Vector2(14,-ry);r.sizeDelta=new Vector2(392,h);t.rectTransform.sizeDelta=new Vector2(366,h);ry+=h;}
            if(legendRoot!=null){legendRoot.anchoredPosition=new Vector2(14,-ry-8);((RectTransform)rulesCard.transform).sizeDelta=new Vector2(420,Mathf.Max(431.5f,ry+136));}
            retreatTitle.text=RaidText.Get("raid.retreat.title");retreatBody.text=RaidText.Get("raid.retreat.body");retreatFoot.text=RaidText.Get("raid.retreat.foot");retreatAgainLabel.text=Loc.T("EMİN MİSİN? TEKRAR BAS");
            continueLabel.text=Loc.T("OPERASYON RAPORUNA GEÇ");resumeLabel.text=Loc.T("Devam et [Esc]");pauseLabel.text=Loc.T("DURAKLATILDI");
            dim.gameObject.SetActive(vm.Finished||vm.RetreatArmed||vm.Paused);dim.color=DeskTheme.Active.Get("sade.plaque.bg");var col=dim.color;col.a=vm.Finished ? DimAlpha*Dim() : .32f;dim.color=col;
            if(!vm.Finished)dim.SetSpot(Vector2.zero,150,14,Color.clear,false);
            if(!vm.Finished)finishedAt=-1;
            else
            {
                if(finishedAt<0){finishedAt=Time.unscaledTime;stamped=false;Card();}
                RenderResult(vm);
            }
            if(logDrawer.activeSelf)RenderLog(vm.Log);
            if(squadReport!=null){squadReport.SetActive(vm.SquadReportVisible&&!vm.Finished&&!vm.Paused&&!vm.RetreatArmed);if(squadReport.activeSelf)RenderSquad(vm.SquadReport);}
        }
        /// <summary>
        /// Kadro raporu (432 px, öteki notlar gibi 1,27 ölçekli; sol altta birim kartının yerinde): başlık, uzman başına ad ve durum (sağlam yeşil, yaralı, yedek
        /// ya da boş rol kırmızı). Satırlar durumun satır sayısına göre dizilir; kâğıt yukarı doğru uzar.
        /// </summary>
        void RenderSquad(RaidSquadRow[] rows)
        {
            squadTitle.text=Loc.T("KADRO RAPORU");
            float y=40;
            for(int i=0;i<squadNames.Length&&i<squadStatus.Length;i++)
            {
                bool on=i<rows.Length;squadNames[i].gameObject.SetActive(on);squadStatus[i].gameObject.SetActive(on);if(!on)continue;
                var row=rows[i];squadNames[i].text=row.Name;squadStatus[i].text=row.Status;squadStatus[i].color=RaidHudView.C(row.Good?"ink.green":"ink.red");
                float w=squadStatus[i].rectTransform.sizeDelta.x,h=Mathf.Max(22,squadStatus[i].GetPreferredValues(row.Status,w,float.PositiveInfinity).y+6);
                squadNames[i].rectTransform.anchoredPosition=new Vector2(squadNames[i].rectTransform.anchoredPosition.x,-y);
                squadStatus[i].rectTransform.anchoredPosition=new Vector2(squadStatus[i].rectTransform.anchoredPosition.x,-y);
                squadStatus[i].rectTransform.sizeDelta=new Vector2(w,h);y+=h+2;
            }
            var card=(RectTransform)squadReport.transform;card.sizeDelta=new Vector2(card.sizeDelta.x,y+10);
        }
        /// <summary>Kartın içeriği: başlık yerinde tek damga (başarıda mavi, değilse kırmızı), yer satırı, satırlar.</summary>
        void RenderResult(RaidViewModel vm)
        {
            resultHead.text=Loc.T("OPERASYON SONUCU");resultTurn.text=vm.ResultTurn;resultTitle.text=vm.ResultTitle;resultPlace.text=vm.ResultPlace;
            var ink=RaidHudView.C(vm.Success?"ink.blue":"ink.red");resultTitle.color=ink;resultStampFrame.color=ink;
            for(int i=0;i<resultLabels.Length;i++)
            {
                bool on=i<vm.ResultRows.Length;resultLabels[i].gameObject.SetActive(on);resultValues[i].gameObject.SetActive(on);if(!on)continue;
                var row=vm.ResultRows[i];resultLabels[i].text=row.Label;resultValues[i].text=row.Value;resultValues[i].color=RaidHudView.C(row.Good?"ink.green":"ink.ink");
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(resultCard);
        }
        float Dim()=>finishedAt<0?1:Mathf.Clamp01((Time.unscaledTime-finishedAt)/DimSeconds);
        void Update()
        {
            if(current==null||!current.Finished||finishedAt<0)return;
            var col=dim.color;col.a=DimAlpha*Dim();dim.color=col;
            Card();
            // RAPORA GEÇ: üzerinde 2 px kalkar (gölge 5), basılı 2 px iner (gölge 1)
            if(continueHover!=null){bool pressed=continueHover.Pressed,over=continueHover.Over&&!pressed;float lift=pressed?-2:over?2:0,drop=pressed?1:over?5:3;
                continueFace.anchoredPosition=new Vector2(0,lift);continueShadow.rectTransform.anchoredPosition=new Vector2(0,lift-drop);}
        }
        /// <summary>Kart sağdan kayar, sonra damga basılır (1,25 → 1).</summary>
        void Card()
        {
            if(!cardRested){cardRested=true;cardRest=resultCard.anchoredPosition;}
            float t=Time.unscaledTime-finishedAt,p=Application.isPlaying?Mathf.Clamp01((t-CardAt)/CardSeconds):1,e=1-(1-p)*(1-p);
            resultCard.anchoredPosition=cardRest+new Vector2(760*(1-e),0);
            float s=Application.isPlaying?Mathf.Clamp01((t-CardAt-CardSeconds)/StampSeconds):1;
            if(s>0&&!stamped&&Application.isPlaying){stamped=true;ResultStamped?.Invoke(current!=null&&current.Success);}
            resultStamp.localScale=Vector3.one*(s<=0?0:Mathf.Lerp(1.25f,1,s));
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
