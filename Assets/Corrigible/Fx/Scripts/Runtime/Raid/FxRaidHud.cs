using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk;
using Corrigible.Game.Raid;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>F6 FX görünümü. Ortak Baskın VM/komut mantığını kullanır; bütün UI parçaları sahnede yazılıdır.</summary>
    public sealed class FxRaidHud : FxTimedBehaviour,IRaidCommands
    {
        [Serializable] public sealed class Slot {public string key;public RectTransform rect;public TMP_Text text;public Image image;public Graphic graphic;public FxPulseGraphic pulse;public Button button;public CanvasGroup group;public FxPopupView popup;}
        [SerializeField] public Slot[] slots;
        [SerializeField] public FxRaidDataSource source;
        [SerializeField] public FxRaidBoard board;
        [SerializeField] public FxTheme theme;
        [SerializeField] public Material teamMaterial;
        [SerializeField] public Canvas canvas;
        [SerializeField] public CanvasGroup corners;
        [SerializeField] public FxRaidSpot dim;
        [SerializeField] public OrbView stateOrb,collapsedOrb;
        [SerializeField] public ScrollRect logScroll;
        [SerializeField] public RectTransform logContent;
        static readonly string[] TeamKeys={"team.0","team.1","team.2"}, TeamEdges={"team.0.edge","team.1.edge","team.2.edge"}, TopKeys={"log","rules","retreat"}, BottomKeys={"end","undo"};
        Material authoredTeamMaterial;
        readonly Dictionary<Renderer,Material[]> authoredMaterials=new Dictionary<Renderer,Material[]>();
        void CreateTeamMaterial(){if(!Application.isPlaying||teamMaterial==null)return;authoredTeamMaterial=teamMaterial;teamMaterial=new Material(teamMaterial){hideFlags=HideFlags.DontSave};foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){var mats=renderer.sharedMaterials;if(!Array.Exists(mats,x=>x==authoredTeamMaterial))continue;authoredMaterials.Add(renderer,mats);renderer.sharedMaterials=Array.ConvertAll(mats,x=>x==authoredTeamMaterial?teamMaterial:x);}}
        void ReleaseTeamMaterial(){if(authoredTeamMaterial==null)return;foreach(var pair in authoredMaterials)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;authoredMaterials.Clear();Destroy(teamMaterial);teamMaterial=authoredTeamMaterial;authoredTeamMaterial=null;}
        Dictionary<string,Slot> map;double enteredAt;bool rendering;Slot currentQueue;RaidViewModel model;double resultAt,lastTick;bool finished;char teamHover;Vector2Int? hovered;float width,height;readonly List<RaycastResult> hits=new List<RaycastResult>();PointerEventData pointer;
        public RaidViewModel Model=>model;
        public int Screen=>source.Screen;
        public bool BoardBlocked=>model==null||model.Locked||model.Paused||model.RetreatArmed||model.EndConfirm||model.Finished;
        Slot U(string key){if(map==null){map=new Dictionary<string,Slot>(slots.Length);foreach(var s in slots)map.Add(s.key,s);}return map[key];}
        void Text(string key,string value){var t=U(key).text;if(t.text!=value)t.text=value??"";}
        void Active(string key,bool on){var g=U(key).rect.gameObject;if(g.activeSelf!=on)g.SetActive(on);}
        Color Ink(string key)=>theme.Color(key);
        void Color(string key,string ink,float alpha=1){var s=U(key);var c=Ink(ink);c.a=alpha;if(s.image!=null)s.image.color=c;if(s.graphic!=null)s.graphic.color=c;if(s.text!=null)s.text.color=c;}
        void Button(string key,bool on){U(key).button.interactable=on;U(key).group.alpha=on?1:.4f;}
        protected override void OnEnable(){base.OnEnable();CreateTeamMaterial();enteredAt=clock.Now;source.Changed+=Render;foreach(var slot in slots)if(slot.button!=null){string key=slot.key;slot.button.onClick.AddListener(()=>Route(key));}if(source.Current!=null)Render(source.Current);}
        protected override void OnDisable(){ReleaseTeamMaterial();source.Changed-=Render;foreach(var slot in slots)if(slot.button!=null)slot.button.onClick.RemoveAllListeners();base.OnDisable();}
        public void LoadScreen(int screen){if(source is FxRaidSource mock)mock.LoadPreset(screen); }
        public void LoadBoard(string id){if(source is FxRaidSource mock)mock.LoadBoardPreset(id); }
        public void Render(RaidViewModel vm)
        {
            if(rendering)return;rendering=true;try{
            model=vm;if(vm.Finished||vm.Paused||vm.RetreatArmed)U("tip").popup.Close();if(teamMaterial!=null)teamMaterial.SetFloat("_Saturation",vm.Locked?.4f:1);board.BindState(source.State,vm,source.IntentStates,source.FixtureRaw,source.Preview);board.ShowEffect(source.PresentedEffect);
            if(vm.Finished&&!finished)resultAt=clock.Now;finished=vm.Finished;
            Text("turn.number",Loc.F("{0}/{1}",vm.Turn,vm.Turns));Text("phase",vm.Phase=="enemy"?Loc.T("MAKİNELER"):Loc.T("SIRA SENDE"));Color("phase",vm.Phase=="enemy"?"rd":"mt");Color("phase.lamp",vm.Phase=="enemy"?"rd":"mt");
            U("turn").rect.sizeDelta=new Vector2(vm.LastTurn?470:376,62);U("phase.box").rect.anchoredPosition=new Vector2(vm.LastTurn?330:237,-16);Active("last",vm.LastTurn);
            for(int i=0;i<8;i++){string k="turn."+i;Active(k,i<vm.Turns);if(i>=vm.Turns)continue;Text(k+".label",(i+1).ToString(System.Globalization.CultureInfo.InvariantCulture));Color(k,i+1==vm.Turn?(vm.LastTurn?"rd":"cy"):"cy",i+1==vm.Turn?1:i+1<vm.Turn?.12f:.03f);Color(k+".label",i+1==vm.Turn?"bg":i+1==vm.Turns?"rd2":"tx2");}
            Text("objective.sub",vm.Targets.Length>1?Loc.T("ikisini de kır"):Loc.T("kır"));Text("ai.state",vm.LastTurn?Loc.T("HÂL 3"):Loc.T("HÂL 2"));Text("ai.ability",vm.AbilityLabel);stateOrb.level=board.nodeOrb.level;
            for(int i=0;i<2;i++){string k="target."+i;Active(k,i<vm.Targets.Length);if(i>=vm.Targets.Length)continue;var t=vm.Targets[i];Text(k+".name",t.Name);Sprite(k+".thumb",t.Thumb);U(k+".thumb").image.color=new UnityEngine.Color(1,1,1,t.Broken?.6f:1);Active(k+".armor",t.Armor);Active(k+".broken",t.Broken);Pips(k+".hp",t.Hp,t.Broken?0:t.Max,"rd");U(k+".armor").rect.anchoredPosition=new Vector2(52+Mathf.Min(64,U(k+".name").text.preferredWidth+4),-11);}
            Active("target.plus",vm.Targets.Length>1);for(int i=0;i<5;i++){Active("civil."+i,i<vm.Civilians);Sprite("civil."+i,i>=vm.Civilians-vm.CivilianBroken?"ic_fx_civil_x":"ic_fx_civil");}
            bool player=!vm.Locked&&!vm.Paused&&!vm.RetreatArmed;
            for(int i=0;i<3;i++){
                var t=vm.Team[i];string k="team."+i;Text(k+".name",t.Letter=='E'&&DeskLoc.Instance.Lang==DeskLang.En?Loc.T("EMP UZM."):t.Name);Color(k+".name",t.Down?"tx3":t.Selected?"am2":"tx");U(k).group.alpha=vm.Locked?.5f:t.Down?1:!t.Move&&!t.Ability?.6f:1;Color(k+".edge",t.Down?"rd":t.Selected||t.Warn?"am":"mt",t.Selected?1:.5f);Sprite(k+".thumb",t.Thumb);U(k+".thumb").image.color=new UnityEngine.Color(1,1,1,t.Down?.45f:1);Active(k+".down",t.Down);Active(k+".cross",t.Down);Pips(k+".hp",t.Hp,t.Max,t.Down?"rd":"mt");U(k).button.interactable=player&&!t.Down;
                for(int j=0;j<2;j++){bool usable=j==0?t.Move:t.Ability;Active(k+".right."+j,!t.Down);Color(k+".right."+j,t.Armed&&j==1?"raid.flagInk":"cy",usable?1:.45f);Color(k+".rightbg."+j,t.Armed&&j==1?"am":"cy",t.Armed&&j==1?1:.08f);}
            }
            Active("unread",vm.LogUnread);Button("undo",player&&vm.CanUndo);Button("end",player);Text("end.label",vm.EndConfirm?Loc.T("EMİN MİSİN? [Enter]"):vm.Locked?Loc.T("KİLİTLİ"):Loc.T("TURU BİTİR [Enter]"));Color("end.fill",vm.EndConfirm?"am":"cy");
            Button("log",!vm.Paused&&!vm.RetreatArmed&&!vm.Finished);Button("rules",!vm.Paused&&!vm.RetreatArmed&&!vm.Finished);Button("retreat",player);Active("retreat",!vm.RetreatArmed);Color("log.edge",vm.OpenPanel=="log"?"cy":"line");Color("rules.edge",vm.OpenPanel=="rules"?"cy":"line");
            Unit(vm);Inspector(vm.HoverCard);Event(vm);Confirm(vm);Log(vm);Rules();
            Popup("unit",vm.UnitCard!=null&&!vm.Finished&&!vm.RetreatArmed);Popup("hover",vm.HoverCard!=null&&!vm.Finished&&!vm.RetreatArmed);Popup("event",vm.EventVisible&&!vm.Finished);Popup("confirm",vm.EndConfirm&&!vm.Finished);Popup("log.panel",vm.OpenPanel=="log"&&!vm.Finished);Popup("rules.panel",vm.OpenPanel=="rules"&&!vm.Finished);Popup("retreat.panel",vm.RetreatArmed&&!vm.Finished);Popup("pause",vm.Paused&&!vm.Finished);Popup("result",vm.Finished&&(clock.Static||clock.Now-resultAt>=.5));
            Active("retreat.again",vm.RetreatArmed&&!vm.Finished);Active("result.stamp",vm.Finished);Active("result.ring",vm.Finished);Active("result.outer",vm.Finished);Active("result.glow",vm.Finished);dim.gameObject.SetActive(vm.RetreatArmed||vm.Finished||vm.Paused);
            corners.blocksRaycasts=corners.interactable=!vm.Finished;collapsedOrb.gameObject.SetActive(vm.Finished);if(vm.Finished){Result(vm);collapsedOrb.kind=Orb.OrbKind.Unknown;collapsedOrb.level=0;}
            RefreshLayout(true);Tick(clock);
            }finally{rendering=false;}
        }
        void Pips(string key,int hp,int max,string color){for(int j=0;j<4;j++){string k=key+"."+j;Active(k,j<max);Color(k,color,j<hp?1:.16f);}}
        void Sprite(string key,string sourceKey){var image=U(key).image;string name=sourceKey;
            if(sourceKey!=null&&sourceKey.StartsWith("thumb_standee_",StringComparison.Ordinal)){var letter=sourceKey[14];name="thumb_fx_standee_"+letter+(sourceKey.EndsWith("_done",StringComparison.Ordinal)?"_done":sourceKey.EndsWith("_damaged",StringComparison.Ordinal)?"_dmg":"_ready");}
            else if(sourceKey=="thumb_cabinet_node"||sourceKey=="thumb_cabinet_core")name="thumb_fx_node";else if(sourceKey=="thumb_cabinet_backup")name="thumb_fx_backup";else if(sourceKey=="thumb_debris_backup")name="thumb_fx_debris_backup";else if(sourceKey=="thumb_drone")name="thumb_fx_drone";else if(sourceKey=="thumb_maint")name="fx_robot_mini";
            var sprite=theme.sprites.Find(x=>x.key==name).sprite;image.enabled=sprite!=null;if(sprite!=null){image.sprite=sprite;image.preserveAspect=true;}
        }
        void Unit(RaidViewModel vm){if(vm.UnitCard==null)return;var u=vm.UnitCard;Text("unit.name",u.Name);Sprite("unit.thumb",u.Thumb);Pips("unit.hp",u.Hp,u.Max,"mt");
            for(int i=0;i<3;i++){string k="unit.row."+i;Active(k,i<vm.UnitRows.Length);if(i>=vm.UnitRows.Length)continue;var r=vm.UnitRows[i];Text(k+".name",r.Text);Text(k+".detail",r.Detail);Active(k+".check",r.Used);Color(k+".fill",r.Armed?"am":"cy",r.Armed?.24f:.03f);Color(k+".edge",r.Armed?"am":"line",r.Armed?1:.5f);Color(k+".name",r.Used?"tx3":r.Armed?"am2":"tx");U(k).button.interactable=i==1&&u.Ability&&!BoardBlocked;}
            for(int i=0;i<3;i++){Active("unit.effect."+i,i<vm.Effects.Length);if(i<vm.Effects.Length)Text("unit.effect."+i,vm.Effects[i]);}
        }
        void Inspector(RaidInspectorModel vm){if(vm==null)return;Text("hover.name",vm.Title);Sprite("hover.thumb",vm.Thumb);Pips("hover.hp",vm.Hp,vm.Max,"rd");
            for(int i=0;i<5;i++){string k="hover.row."+i;Active(k,i<vm.Rows.Length);if(i>=vm.Rows.Length)continue;var r=vm.Rows[i];Text(k+".name",r.Text);Text(k+".detail",r.Detail);Color(k+".detail",r.Good?"mt":"rd");}
            Active("hover.win",vm.Win);
        }
        void Event(RaidViewModel vm){currentQueue=null;Text("event.text",vm.EventLine);Text("event.speed",vm.FastMachines?Loc.T("Boşluk · normal hız"):Loc.T("Boşluk · hızlandır"));for(int i=0;i<8;i++){string k="queue."+i;Active(k,i<vm.Queue.Length);if(i>=vm.Queue.Length)continue;var q=vm.Queue[i];if(U(k).pulse!=null)U(k).pulse.SetPulse(false);if(q.State=="now")currentQueue=U(k);Text(k+".label",q.State=="done"?Loc.F("<s>{0}</s>",q.Order):q.Order.ToString(System.Globalization.CultureInfo.InvariantCulture));Color(k,q.State=="now"?"rd":"cy",q.State=="now"?1:.1f);Color(k+".label",q.State=="now"?"raid.flagInk":q.State=="done"?"tx3":"rd2");Active(k+".cancel",q.State=="cancel");}}
        void Confirm(RaidViewModel vm){for(int i=0;i<3;i++){string k="unused."+i;Active(k,i<vm.Unused.Length);if(i>=vm.Unused.Length)continue;var u=vm.Unused[i];var rights=u.Move&&u.Ability?Loc.T("hareket + yetenek"):u.Move?Loc.T("hareket"):Loc.T("yetenek");Text(k,u.ThreatOrder>0?Loc.F("{0} · {1} kullanılmadı\n#{2} dron onu vuracak · −1",u.Name,rights,u.ThreatOrder):Loc.F("{0} · {1} kullanılmadı",u.Name,rights));}}
        void Log(RaidViewModel vm){if(vm.OpenPanel!="log")return;bool follow=logScroll.verticalNormalizedPosition<.05f;float y=0;for(int i=0;i<128;i++){string k="log.row."+i;Active(k,i<vm.Log.Length);if(i>=vm.Log.Length)continue;var row=vm.Log[i];bool intercepted=row.Kind=="ai";Text(k+".header",intercepted?Loc.T("YZ · ELE GEÇİRİLEN SATIR"):"");Active(k+".header",intercepted);Text(k+".text",row.Text);Color(k+".text",row.Kind=="good"?"mt":row.Kind=="bad"||intercepted?"rd2":row.Kind=="sep"||row.Kind=="turn"?"cy":"tx2");var t=U(k+".text").text;float h=Mathf.Max(26,t.GetPreferredValues(row.Text,338,float.PositiveInfinity).y+12+(intercepted?23:0));U(k).rect.sizeDelta=new Vector2(344,h);U(k).rect.anchoredPosition=new Vector2(0,-y);t.rectTransform.anchoredPosition=new Vector2(4,intercepted?-24:-4);t.rectTransform.sizeDelta=new Vector2(336,h-(intercepted?28:8));y+=h+7;}float offset=Mathf.Max(0,logScroll.viewport.rect.height-y);if(offset>0)for(int i=0;i<vm.Log.Length&&i<128;i++)U("log.row."+i).rect.anchoredPosition-=Vector2.up*offset;logContent.sizeDelta=new Vector2(0,Mathf.Max(logScroll.viewport.rect.height,y));if(follow)logScroll.verticalNormalizedPosition=0;Text("log.clock",Loc.F("T{0} · 14:32",vm.Turn));}
        void Rules(){for(int i=0;i<6;i++)Text("rule."+i,RaidText.Get("rules."+(i+1)));string[] names={"legend.move","legend.target","legend.hit","legend.lie","legend.channel","legend.wake","legend.types","legend.flag","legend.scanned","legend.armor"};for(int i=0;i<names.Length;i++)Text("legend."+i,RaidText.Get(names[i]));float y=50;for(int i=0;i<6;i++){var text=U("rule."+i).text;float h=Mathf.Max(40,text.GetPreferredValues(text.text,376,float.PositiveInfinity).y+10);text.rectTransform.sizeDelta=new Vector2(376,h);Place("rule."+i,40,y);Place("rule.icon."+i,14,y+12);y+=h;}Place("legend.title",14,y+10);for(int i=0;i<10;i++){float top=y+44+(i/2)*34;Place("legend."+i,54+(i%2)*204,top);Place("legend.sample."+i,14+(i%2)*204,top+4);Place("legend.icon."+i,22+(i%2)*204,top+5);}U("rules.panel").rect.sizeDelta=new Vector2(430,y+44+5*34+14);}
        void Result(RaidViewModel vm){string accent=vm.Success?"mt":"rd";Text("result.title",vm.ResultTitle);Text("result.stamp.text",vm.ResultTitle);Color("result.title",accent);Color("result.stamp.text",accent);Text("result.status",vm.Success?Loc.T("BAŞARILI"):Loc.T("BAŞARISIZ"));Text("result.subtitle",vm.ResultSubtitle);var rows=vm.Targets.Select(t=>Loc.F("{0} · {1}",t.Name,t.Broken?Loc.T("kırıldı"):Loc.F("{0}/{1}",t.Hp,t.Max))).ToList();rows.Add(Loc.F("Kırılan sivil sunucu · {0}",vm.CivilianBroken));rows.Add(Loc.F("Ekip · {0}/{1} ayakta",vm.Standing,vm.TeamCount));Text("result.rows",string.Join("\n",rows));Text("result.foot",string.IsNullOrEmpty(vm.ResultReason)?Loc.T("Operasyon raporu bölgeye etkisini ve ekibin durumunu gösterecek."):vm.ResultReason);Color("result.edge",accent,.7f);Color("result.stamp.edge",accent);Color("result.ring",accent);Color("result.outer",accent,.55f);Color("result.glow",accent,.15f);}
        void Popup(string key,bool show){var p=U(key).popup;if(show)p.Show(new Vector2(p.rect.anchoredPosition.x,-p.rect.anchoredPosition.y));else p.Close();}
        public void CompleteAnimations(){foreach(var s in slots)if(s.popup!=null)s.popup.Complete();Tick(clock);}
        public void RefreshLayout(bool force=false)
        {
            var r=((RectTransform)canvas.transform).rect;if(!force&&width==r.width&&height==r.height)return;width=r.width;height=r.height;
            Place("unread",width-(model?.RetreatArmed==true?466:321),9);Place("event",16,196);Place("log",width-(model?.RetreatArmed==true?588:443),14);Place("rules",width-(model?.RetreatArmed==true?448:303),14);Place("retreat",width-147,14);Place("retreat.again",width-292,14);
            for(int i=0;i<3;i++)Place(TeamKeys[i],16+i*212,height-90);
            Place("end",width-234,height-76);Place("undo",width-376,height-66);
            Place("log.panel",width-396,74);Place("rules.panel",width-446,74);Place("retreat.panel",width-396,74);Place("confirm",width-352,height-100-Mathf.Max(176,84+(model?.Unused.Length??1)*54));
            U("confirm").rect.sizeDelta=new Vector2(336,Mathf.Max(176,84+(model?.Unused.Length??1)*54));
            float uh=model?.UnitCard?.Letter=='A'?285:233;for(int i=0;i<3;i++)Place("unit.effect."+i,14+i*110,uh-40);U("unit").rect.sizeDelta=new Vector2(360,uh);int index=model?.UnitCard==null?0:Array.FindIndex(model.Team,x=>x.Letter==model.UnitCard.Letter);Place("unit",16+index*212,height-103-uh);
            float hh=model?.HoverCard?.Win==true?195:Mathf.Max(163,70+(model?.HoverCard?.Rows.Length??0)*30);U("hover").rect.sizeDelta=new Vector2(258,hh);Place("hover",width-274,height-(model?.HoverCard?.Win==true?94:99)-hh);Place("hover.win",12,hh-50);
            Place("result",width*.5f+280,height*.5f);Place("pause",width*.5f-160,height*.5f-70);
            if(model?.Finished==true){var local=board.Project(RaidBoard3D.W(model.Spot.x,model.Spot.y)+Vector3.up*model.Spot.z);dim.SetSpot(local,150,0,Ink("mt"));var point=new Vector2(width*.5f+local.x,height*.5f-local.y);Place("result.ring",point.x,point.y);Place("result.outer",point.x,point.y);Place("result.glow",point.x,point.y);Place("result.stamp",point.x-190,point.y+114);collapsedOrb.image.rectTransform.anchoredPosition=new Vector2(point.x,-point.y+70);}
        }
        void Place(string key,float x,float y){var s=U(key);if(s.popup!=null)s.popup.MoveTo(new Vector2(x,y));else s.rect.anchoredPosition=new Vector2(x,-y);}
        public void Hover(string key,bool on){if(on&&(model==null||model.Finished||model.Paused||model.RetreatArmed))return;if(key.StartsWith("team.",StringComparison.Ordinal)){teamHover=on?model.Team[int.Parse(key.Substring(5))].Letter:'\0';return;}if(key=="tip.state"||key=="tip.ability"){Text("tip.text",key=="tip.state"?Loc.T("YZ düğümünün hâli"):model.AbilityTip);if(on)U("tip").popup.Show(new Vector2(16,196));else U("tip").popup.Close();}}
        public void Route(string key)
        {
            if(model==null)return;
            if(key=="cancel"||key=="close.log"||key=="close.rules"||key=="resume"){CancelPending();return;}
            if(key=="continue"){ContinueToReport();return;}
            if(model.Finished||model.Paused)return;
            if(model.RetreatArmed){if(key=="retreat.again")RequestRetreat();return;}
            if(key=="log"){ToggleLog();return;}if(key=="rules"){ToggleRules();return;}
            if(model.Locked)return;
            if(key.StartsWith("team.",StringComparison.Ordinal))SelectUnit(model.Team[int.Parse(key.Substring(5))].Letter);
            else if(key=="undo")Undo();else if(key=="end")EndTurn();else if(key=="retreat")RequestRetreat();else if(key.StartsWith("unit.row.",StringComparison.Ordinal))ArmAbility();
        }
        public void SelectUnit(char letter){if(!BoardBlocked)source.Select(letter);}public void ClearSelection(){if(!BoardBlocked)source.ClearSelection();}
        public void HoverTile(Vector2Int tile){if(!BoardBlocked)source.Hover(tile);}public void ClearHover()=>source.ClearHover();
        public void MoveSelectedTo(Vector2Int tile){if(!BoardBlocked)source.Move(tile);}public void ArmAbility(){if(!BoardBlocked)source.Arm();}public void UseAbilityOn(Vector2Int tile){if(!BoardBlocked)source.Use(tile);}
        public void Undo(){if(!BoardBlocked)source.Undo();}public void EndTurn(){if(model?.Finished==true){ContinueToReport();return;}if(model!=null&&!model.Locked&&!model.Paused&&!model.RetreatArmed)source.EndWithFx(clock.Now);}
        public void CancelPending(){if(model==null||model.Finished)return;if(model.Locked){if(model.OpenPanel!=null)source.Panel(model.OpenPanel);return;}source.Cancel();}
        public void SpeedUpMachines(bool on)=>source.Speed(on);public void RequestRetreat(){if(model!=null&&!model.Locked&&!model.Paused)source.Retreat();}public void ToggleLog(){if(model!=null&&!model.Paused&&!model.RetreatArmed&&!model.Finished)source.Panel("log");}public void ToggleRules(){if(model!=null&&!model.Paused&&!model.RetreatArmed&&!model.Finished)source.Panel("rules");}public void ContinueToReport(){if(model?.Finished==true)source.Continue();}
        public override void Tick(FxClock c)
        {
            if(model==null)return;RefreshLayout();if(!rendering)source.TickMachines(c.Now);float dt=lastTick==0?0:(float)Math.Max(0,c.Now-lastTick);lastTick=c.Now;
            float target=model.Finished?0:1;corners.alpha=c.Static?target:Mathf.MoveTowards(corners.alpha,target,dt/.2f);
            if(dim.gameObject.activeSelf){var col=Ink("bg");col.a=model.Finished?.72f*(c.Static?1:Mathf.Clamp01((float)(c.Now-resultAt))):model.RetreatArmed?.55f:.5f;dim.color=col;if(!model.Finished)dim.SetSpot(Vector2.zero,150,0,UnityEngine.Color.clear,false);}
            Entrance("turn",c,.7f,0,new Vector2(16,-14),new Vector2(0,14));Entrance("objective",c,.7f,.08f,new Vector2(16,-88),new Vector2(0,14));
            foreach(var key in TopKeys)Entrance(key,c,.6f,.12f,U(key).rect.anchoredPosition,Vector2.zero);foreach(var key in BottomKeys)Entrance(key,c,.6f,.15f,U(key).rect.anchoredPosition,Vector2.zero);
            for(int i=0;i<3;i++){var t=model.Team[i];var rect=U(TeamKeys[i]).rect;rect.sizeDelta=new Vector2(204,t.Selected?82:74);U(TeamEdges[i]).rect.sizeDelta=rect.sizeDelta;var point=new Vector2(16+i*212,-height+90+(t.Selected?8:teamHover==t.Letter?2:0));float p=FxTween.Progress(c.Now,enteredAt,.6f,.1f+.06f*i,c.CompleteEntrances);point.y-=16*(1-p);U(TeamKeys[i]).group.alpha=(model.Locked?.5f:t.Down?1:!t.Move&&!t.Ability?.6f:1)*p;if(rect.anchoredPosition!=point)rect.anchoredPosition=point;}
            float pulse=c.Static?1:.72f+.28f*(.5f+.5f*Mathf.Sin((float)c.Now*Mathf.PI*2/1.2f));Pulse(U("unread"),true,pulse);Pulse(U("phase.lamp"),true,pulse);Pulse(U("end.fill"),model.EndConfirm,pulse);Pulse(U("retreat.again.fill"),true,pulse);if(currentQueue!=null)Pulse(currentQueue,true,pulse);
            if(finished){if(c.Static||c.Now-resultAt>=.5)Popup("result",true);float p=c.Static?1:Mathf.Clamp01((float)(c.Now-resultAt-.3)/2.4f);collapsedOrb.image.color=new UnityEngine.Color(1,1,1,!c.Static&&c.Now-resultAt<.3?0:Mathf.Lerp(1,.25f,p));collapsedOrb.transform.localScale=Vector3.one*Mathf.Lerp(1,.7f,FxTween.Ease(p));float stamp=c.Static?1:Mathf.Clamp01((float)(c.Now-resultAt-.4)/.6f);U("result.stamp").group.alpha=stamp;U("result.stamp").rect.localScale=Vector3.one*(stamp<.8f?Mathf.Lerp(1.6f,.96f,FxTween.Ease(stamp/.8f)):Mathf.Lerp(.96f,1,(stamp-.8f)/.2f));U("result.outer").rect.localRotation=Quaternion.Euler(0,0,c.Static?0:(float)c.Now/14*360);}
            if(Application.isPlaying&&!rendering){Keyboard(c);Pointer();}
        }
        static void Pulse(Slot slot,bool on,float value){if(slot.pulse!=null){slot.pulse.SetPulse(on);slot.image.canvasRenderer.SetAlpha(1);}else slot.image.canvasRenderer.SetAlpha(on?value:1);}
        void Entrance(string key,FxClock c,float duration,float delay,Vector2 position,Vector2 offset){var slot=U(key);float p=FxTween.Progress(c.Now,enteredAt,duration,delay,c.CompleteEntrances);slot.group.alpha=p*(slot.button!=null&&!slot.button.interactable?.4f:1);slot.rect.anchoredPosition=position+offset*(1-p);}
        void Keyboard(FxClock c){var k=UnityEngine.InputSystem.Keyboard.current;if(k==null)return;if(source is FxRaidSource&&!FxDebugMenu.FunctionKeysOwned&&k.shiftKey.isPressed){for(int i=0;i<9;i++)if(k[(Key)((int)Key.F1+i)].wasPressedThisFrame){LoadScreen(i+1);return;}}if(k.digit1Key.wasPressedThisFrame)SelectUnit('T');if(k.digit2Key.wasPressedThisFrame)SelectUnit('E');if(k.digit3Key.wasPressedThisFrame)SelectUnit('A');if(k.qKey.wasPressedThisFrame)ArmAbility();if(k.zKey.wasPressedThisFrame)Undo();if(k.enterKey.wasPressedThisFrame)EndTurn();if(k.escapeKey.wasPressedThisFrame)CancelPending();if(k.lKey.wasPressedThisFrame)ToggleLog();if(!FxDebugMenu.FunctionKeysOwned&&k.f1Key.wasPressedThisFrame||k.slashKey.wasPressedThisFrame&&k.shiftKey.isPressed)ToggleRules();if(k.spaceKey.wasPressedThisFrame)SpeedUpMachines(!model.FastMachines);}
        void Pointer(){var m=Mouse.current;if(m==null)return;if(m.rightButton.wasPressedThisFrame){CancelPending();return;}if(BoardBlocked)return;
            if(EventSystem.current!=null){if(pointer==null)pointer=new PointerEventData(EventSystem.current);pointer.position=m.position.ReadValue();hits.Clear();EventSystem.current.RaycastAll(pointer,hits);if(hits.Count>0){if(hovered.HasValue){hovered=null;ClearHover();}return;}}
            var ray=board.boardCamera.ScreenPointToRay(m.position.ReadValue());var plane=new Plane(Vector3.up,Vector3.zero);if(!plane.Raycast(ray,out float distance))return;var p=ray.GetPoint(distance);int x=Mathf.FloorToInt(p.x+4),y=Mathf.FloorToInt(4-p.z);if(x<0||x>=8||y<0||y>=8){if(hovered.HasValue){hovered=null;ClearHover();}return;}var tile=new Vector2Int(x,y);if(hovered!=tile){hovered=tile;HoverTile(tile);}if(m.leftButton.wasPressedThisFrame){var unit=Baskin.UnitAt(source.State,x,y);if(model.AbilityArmed)UseAbilityOn(tile);else if(unit?.Side=='p')SelectUnit(Baskin.UnitTypes[unit.Type].Short[0]);else MoveSelectedTo(tile);}}
    }
}
