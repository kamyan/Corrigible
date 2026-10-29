using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Corrigible.Desk
{
    /// <summary>Baskın HUD'u. Tahta kurallarını hesaplamaz; veri kaynağı ve komut sınırına bağlanır.</summary>
    public sealed class RaidHudView : MonoBehaviour
    {
        [SerializeField] MonoBehaviour dataSource, commandSource;
        [SerializeField] RaidPanelsView panels;
        [SerializeField] CanvasGroup cornerGroup;
        [SerializeField] TMP_Text lastStamp;
        [SerializeField] TMP_Text[] brokenStamps;
        [SerializeField] Image[] buttonIcons;
        [SerializeField] Image phaseLamp;
        [SerializeField] RectTransform turnPlaque,phaseCard;
        [SerializeField] TMP_Text turnLabel,turnNumber,phase,objective,objectiveSub,civilianLabel,abilityLabel;
        [SerializeField] TMP_Text[] turnBoxes,targetNames;
        [SerializeField] Image[] turnFills,targetThumbs,targetArmor,civilians;
        [SerializeField] Image[] targetPips;
        [SerializeField] Image unread;
        [SerializeField] RaidTeamCardView[] team;
        [SerializeField] Button log,rules,retreat,undo,end,abilityChip;
        [SerializeField] TMP_Text logLabel,rulesLabel,retreatLabel,undoLabel,endLabel;
        [SerializeField] UnitCardView unitCard;
        [SerializeField] HoverInspectorView hoverCard;
        [SerializeField] GameObject statusCard;
        [SerializeField] TMP_Text statusText;
        IRaidDataSource source;IRaidCommands commands;bool abilityTipVisible;
        public RaidViewModel Current => source?.Current;
        public event System.Action<char> TeamHovered;
        void OnEnable()
        {
            source=dataSource as IRaidDataSource;commands=commandSource as IRaidCommands;
            if(source==null||commands==null)return;source.Changed+=Render;DeskLoc.Instance.Changed+=Refresh;
            log.onClick.AddListener(commands.ToggleLog);rules.onClick.AddListener(commands.ToggleRules);retreat.onClick.AddListener(commands.RequestRetreat);
            undo.onClick.AddListener(commands.Undo);end.onClick.AddListener(commands.EndTurn);
            abilityChip.onClick.AddListener(ShowAbilityTip);
            for(int i=0;i<team.Length;i++){team[i].Bind(commands,i);team[i].Hovered+=OnTeamHover;}
            unitCard.Bind(commands);if(panels!=null)panels.Bind(commands);Refresh();
        }
        void OnDisable()
        {
            if(source!=null)source.Changed-=Render;DeskLoc.Instance.Changed-=Refresh;
            log.onClick.RemoveAllListeners();rules.onClick.RemoveAllListeners();retreat.onClick.RemoveAllListeners();undo.onClick.RemoveAllListeners();end.onClick.RemoveAllListeners();abilityChip.onClick.RemoveAllListeners();
            foreach(var card in team){card.Button.onClick.RemoveAllListeners();card.Hovered-=OnTeamHover;}
        }
        void OnTeamHover(char letter)=>TeamHovered?.Invoke(letter);
        public void ShowAbilityTip(){abilityTipVisible=true;Refresh();}
        public void HideAbilityTip(){abilityTipVisible=false;Refresh();}
        void Refresh(){if(source?.Current!=null)Render(source.Current);}
        public void Render(RaidViewModel vm)
        {
            turnLabel.text=Loc.T("TUR");turnNumber.text=Loc.F("{0}/{1}",vm.Turn,vm.Turns);
            // Makine evresinde komuta telsizi dinler: gözcü makinelerin hamlesini bildirir
            phase.text=vm.Phase=="enemy"?Loc.T("DİNLEMEDE"):Loc.T("SIRA SENDE");
            // Plaka 6 kutuya göre çizildi; uzun (ikmal) ya da kısa baskında kutu başına 26 px kayar
            float extra=(Mathf.Clamp(vm.Turns,1,turnBoxes.Length)-6)*26f;
            if(turnPlaque!=null)turnPlaque.sizeDelta=new Vector2((vm.LastTurn?476.8f:vm.Phase=="enemy"?388:397.4f)+extra,62);
            if(lastStamp!=null){lastStamp.gameObject.SetActive(vm.LastTurn);lastStamp.text=Loc.T("SON TUR");lastStamp.rectTransform.anchoredPosition=new Vector2(239+extra,lastStamp.rectTransform.anchoredPosition.y);}
            if(phaseCard!=null){phaseCard.anchoredPosition=new Vector2((vm.LastTurn?330:251)+extra,-14);phaseCard.GetComponent<Image>().color=DeskTheme.Active.Get(vm.Phase=="enemy"?"sade.raid.phaseAiBg":"sade.paper");}
            phase.color=DeskTheme.Active.Get(vm.Phase=="enemy"?"ink.red":"ink.blue");if(phaseLamp!=null)phaseLamp.color=DeskTheme.Active.Get(vm.Phase=="enemy"?"ink.red":"ink.green");
            for(int i=0;i<turnBoxes.Length;i++)
            {
                turnFills[i].gameObject.SetActive(i<vm.Turns);turnBoxes[i].gameObject.SetActive(i<vm.Turns);turnBoxes[i].text=(i+1).ToString();
                turnFills[i].color=DeskTheme.Active.Get(i+1==vm.Turn?(vm.LastTurn?"sade.raid.phaseAiBg":"sade.paper"):i+1<vm.Turn?"sade.plaque.border":"sade.plaque.bg");
                turnBoxes[i].color=DeskTheme.Active.Get(i+1==vm.Turn?(vm.LastTurn?"ink.red":"sade.status.info"):"sade.plaque.brass");
            }
            objective.text=vm.IsLootMission?Loc.T("İKMAL"):Loc.T("HEDEF");objectiveSub.text=vm.IsLootMission?(vm.Escaping?Loc.T("girişe dön"):Loc.T("kasayı al")):vm.Targets.Length>1?Loc.T("ikisini de kır"):Loc.T("kır");civilianLabel.text=vm.TrustMax>0?Loc.F("GÜVEN {0}",vm.Trust):Loc.T("SİVİL");abilityLabel.text=vm.AbilityLabel;
            for(int i=0;i<targetNames.Length;i++)
            {
                bool show=i<vm.Targets.Length;targetThumbs[i].gameObject.SetActive(show);targetNames[i].gameObject.SetActive(show);targetArmor[i].enabled=show&&vm.Targets[i].Armor;
                if(brokenStamps!=null&&i<brokenStamps.Length){brokenStamps[i].gameObject.SetActive(show&&vm.Targets[i].Broken);brokenStamps[i].text=vm.IsLootMission&&(vm.Success||vm.Escaping)?Loc.T("ALINDI"):Loc.T("KIRILDI");}
                if(show){targetNames[i].fontStyle=vm.Targets[i].Broken?FontStyles.Strikethrough:FontStyles.Normal;targetNames[i].text=vm.Targets[i].Name;DeskSprites.Apply(targetThumbs[i],vm.Targets[i].Thumb);}
                // Hedef başına nokta sayısı diziden (8: sertleştirilmiş düğüm ve çekirdek 6–7 can)
                int per=targetPips.Length/Mathf.Max(1,targetNames.Length);
                for(int j=0;j<per;j++){var pip=targetPips[i*per+j];pip.gameObject.SetActive(show&&!vm.Targets[i].Broken&&j<vm.Targets[i].Max);if(show)DeskSprites.Apply(pip,j<vm.Targets[i].Hp?"pip_full_10":"pip_empty_10");}
            }
            int civShown=Mathf.Min(vm.Civilians,civilians.Length),civBroken=Mathf.Min(vm.CivilianBroken,civShown);for(int i=0;i<civilians.Length;i++){civilians[i].gameObject.SetActive(i<civShown);DeskSprites.Apply(civilians[i],i>=civShown-civBroken?"civ_broken_20":"civ_ok_20");}
            for(int i=0;i<team.Length;i++)team[i].Render(vm.Team[i],vm.Locked||vm.Paused);
            logLabel.text=Loc.T("TELSİZ");rulesLabel.text=Loc.T("KURALLAR");retreatLabel.text=Loc.T("GERİ ÇEKİL");retreat.gameObject.SetActive(!vm.RetreatArmed);
            ((RectTransform)rules.transform).anchoredPosition=new Vector2(vm.RetreatArmed?-262.3f:-155.1f,-14);((RectTransform)log.transform).anchoredPosition=new Vector2(vm.RetreatArmed?-424.1f:-316.9f,-14);
            undoLabel.text=Loc.T("Geri al");endLabel.text=vm.EndConfirm?Loc.T("EMİN MİSİN?"):Loc.T("EMRİ İLET");
            undo.interactable=vm.CanUndo&&!vm.Locked&&!vm.Paused;end.interactable=!vm.Locked&&!vm.Paused;
            ((RectTransform)end.transform).sizeDelta=new Vector2(vm.Locked?245:vm.EndConfirm?231.7f:218,56);
            endLabel.fontSize=vm.EndConfirm?(Loc.IsSource?16:14):17;endLabel.rectTransform.sizeDelta=new Vector2(vm.EndConfirm?118:104,56);
            ((RectTransform)undo.transform).anchoredPosition=new Vector2(-(vm.Locked?271:vm.EndConfirm?257.7f:244),18);
            end.GetComponent<Image>().color=DeskTheme.Active.Get(vm.EndConfirm?"sade.highlighter":vm.Locked?"ink.ink3":"ink.ink");endLabel.color=DeskTheme.Active.Get(vm.EndConfirm?"ink.ink":"sade.paper");
            if(buttonIcons!=null)for(int i=0;i<buttonIcons.Length;i++)DeskSprites.Apply(buttonIcons[i],vm.Locked?"ic_lock_s":i==0?"ic_undo_s":"ic_enter_s");
            unread.enabled=vm.LogUnread;unread.rectTransform.anchoredPosition=new Vector2(vm.RetreatArmed?-431.2f:-324,-10);unitCard.Render(vm);hoverCard.Render(vm.HoverCard);
            if(cornerGroup!=null){cornerGroup.blocksRaycasts=!vm.Finished;cornerGroup.interactable=!vm.Finished;}if(panels!=null)panels.Render(vm);
            statusCard.SetActive(abilityTipVisible||(panels==null&&!string.IsNullOrEmpty(vm.Status)));statusText.text=abilityTipVisible?vm.AbilityTip:vm.Status;
        }
        public static void Pips(Image[] pips,int hp,int max,string color)
        { for(int i=0;i<pips.Length;i++){pips[i].gameObject.SetActive(i<max);DeskSprites.Apply(pips[i],i<hp?"pip_full_10":"pip_empty_10");pips[i].color=DeskTheme.Active.Get(color);} }
        /// <summary>Zırh plakası: son can noktasının sağında, mavi bir nokta (can değil; ilk hasarı karşılar).</summary>
        public static void ArmorPip(Image armor,Image[] pips,int max,int plates)
        {
            if(armor==null)return;bool on=plates>0&&max>0&&pips.Length>1;armor.gameObject.SetActive(on);if(!on)return;
            var first=pips[0].rectTransform.anchoredPosition;var step=pips[1].rectTransform.anchoredPosition-first;
            armor.rectTransform.anchoredPosition=first+step*Mathf.Min(max,pips.Length);
            DeskSprites.Apply(armor,"pip_full_10");armor.color=DeskTheme.Active.Get("sade.status.info");
        }
        void Update()
        {
            if(cornerGroup!=null)
            {
                float alpha=Mathf.MoveTowards(cornerGroup.alpha,Current?.Finished==true?0:1,Time.unscaledDeltaTime/.2f);
                if(cornerGroup.alpha!=alpha)cornerGroup.alpha=alpha;
            }
            var k=Keyboard.current;if(k==null||commands==null)return;
            if(k.digit1Key.wasPressedThisFrame)commands.SelectUnit('T');if(k.digit2Key.wasPressedThisFrame)commands.SelectUnit('E');if(k.digit3Key.wasPressedThisFrame)commands.SelectUnit('A');
            if(k.qKey.wasPressedThisFrame)commands.ArmAbility();if(k.zKey.wasPressedThisFrame)commands.Undo();if(k.enterKey.wasPressedThisFrame)commands.EndTurn();
            if(k.escapeKey.wasPressedThisFrame||Mouse.current?.rightButton.wasPressedThisFrame==true)commands.CancelPending();
            if(k.lKey.wasPressedThisFrame)commands.ToggleLog();if(k.f1Key.wasPressedThisFrame&&!k.shiftKey.isPressed&&!k.ctrlKey.isPressed||k.slashKey.wasPressedThisFrame&&k.shiftKey.isPressed)commands.ToggleRules();
            if(k.spaceKey.wasPressedThisFrame)commands.SpeedUpMachines(!(Current?.FastMachines??false));
        }
    }
}
