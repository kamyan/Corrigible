using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Baskın sunumu ve geçici taktik durum. Canlı sonuç yalnız GameController üzerinden raporlanır.</summary>
    public abstract class RaidDataSource : MonoBehaviour, IRaidDataSource
    {
        [SerializeField] protected RaidBoard3D board;
        [SerializeField] protected RaidHudView hud;
        [SerializeField] protected SadeTheme theme;
        [SerializeField] protected RaidPreviewOverlay previewOverlay;
        [SerializeField] protected bool animateActions = true;
        [SerializeField] protected bool confirmUnusedActions = true;
        public RaidViewModel Current { get; private set; }
        public event Action<RaidViewModel> Changed;
        public Baskin.BState State { get; protected set; }
        public char Selected { get; protected set; }
        public bool Armed { get; protected set; }
        protected bool confirm,retreat,fast,paused,unread=true;protected string panel;
        protected bool resolving,executing;
        protected readonly List<(int x,int y,string kind,int max)> targetForms=new List<(int,int,string,int)>();
        protected int civilianCount;
        Baskin.BState machineBefore;
        List<Baskin.IntentView> roster=new List<Baskin.IntentView>();
        readonly Dictionary<int,string> queue=new Dictionary<int,string>();
        int currentOrder,visibleLogs=int.MaxValue;string eventLine;float eventUntil;
        protected Vector2Int? hover;protected float hoverAt;protected bool hoverVisible;
        protected Baskin.PreviewResult preview;
        protected virtual void OnEnable(){DeskLoc.Instance.Changed+=Publish;if(board!=null){board.TileClicked+=Click;board.HoverChanged+=BoardHover;}if(hud!=null)hud.TeamHovered+=TeamHover;if(board!=null)board.EffectPlaying+=OnEffect;}
        protected virtual void OnDisable(){DeskLoc.Instance.Changed-=Publish;if(board!=null){board.TileClicked-=Click;board.HoverChanged-=BoardHover;}if(hud!=null)hud.TeamHovered-=TeamHover;if(board!=null)board.EffectPlaying-=OnEffect;}
        protected abstract void Start();
        protected virtual void Update()
        {
            if(hover.HasValue&&!hoverVisible&&Time.unscaledTime-hoverAt>=theme.Number("raid.hoverCard.delayMs",150)/1000f){hoverVisible=true;Publish(false);}
        }
        protected void OpenState(Baskin.BState value)
        {
            StopAllCoroutines();State=value;Selected='\0';Armed=confirm=retreat=fast=paused=resolving=executing=false;panel=null;hover=null;hoverVisible=false;preview=null;unread=true;visibleLogs=int.MaxValue;eventLine=null;
            targetForms.Clear();civilianCount=0;
            foreach(var t in Baskin.TargetsLeft(State))targetForms.Add((t.X,t.Y,t.O.K,t.O.Max));
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(State.Cells[y,x].O?.K=="civ")civilianCount++;
            CaptureRoster();if(board!=null){board.FastMachines=false;board.MachineDelay=.25f;board.Build(State);}Publish();
        }
        void CaptureRoster(){roster=Baskin.Intents(State);queue.Clear();foreach(var i in roster)queue[i.Unit]="pending";}
        public bool Busy=>resolving||(board!=null&&board.Busy);
        public void Select(char letter)
        {
            if(!CanInput)return;var u=Unit(letter);if(u==null||u.Dead)return;
            Selected=letter;Armed=false;confirm=retreat=false;ClearHover(false);Publish();
        }
        bool CanInput=>State!=null&&State.Phase=="player"&&!paused&&!Busy;
        Baskin.Unit Unit(char letter)=>State?.Units.FirstOrDefault(u=>u.Side=='p'&&Baskin.UnitTypes[u.Type].Short==letter.ToString());
        Baskin.Unit Unit()=>Unit(Selected);
        public void ClearSelection(){if(Busy)return;Selected='\0';Armed=false;confirm=false;ClearHover(false);Publish();}
        public void Hover(Vector2Int tile)
        {
            if(Busy||retreat||paused)return;
            if(tile.x<0||tile.y<0||tile.x>=8||tile.y>=8){ClearHover();return;}
            if(hover==tile)return;hover=tile;hoverAt=Time.unscaledTime;hoverVisible=false;Publish();
        }
        public void ClearHover()=>ClearHover(true);
        void ClearHover(bool publish){if(board!=null)board.SetExternalHover(null);hover=null;hoverVisible=false;preview=null;if(publish)Publish();}
        void BoardHover((int x,int y)? tile){if(tile.HasValue)Hover(new Vector2Int(tile.Value.x,tile.Value.y));else ClearHover();}
        void TeamHover(char letter){var u=Unit(letter);if(u!=null&&board!=null)board.SetExternalHover((u.X,u.Y));}
        void Click(int x,int y)
        {
            if(!CanInput)return;var tile=new Vector2Int(x,y);
            if(Armed){Use(tile);return;}
            var u=Baskin.UnitAt(State,x,y);if(u!=null&&u.Side=='p'){Select(Baskin.UnitTypes[u.Type].Short[0]);return;}
            Move(tile);
        }
        public void Move(Vector2Int tile)
        {
            var u=Unit();if(!CanInput||u==null)return;if(!Baskin.DoMove(State,u.Id,tile.x,tile.y))return;
            confirm=retreat=false;ClearHover(false);
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(MoveRoutine(u.Id,tile));else Publish();
        }
        IEnumerator MoveRoutine(int id,Vector2Int tile)
        {resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);yield return board.AnimateMove(id,tile.x,tile.y);resolving=false;Publish();}
        public void Arm(){var u=Unit();if(!CanInput||u==null||!Baskin.CanAct(State,u))return;Armed=!Armed;confirm=false;Publish();}
        public void Use(Vector2Int tile)
        {
            var u=Unit();if(!CanInput||u==null||!Armed)return;if(!Baskin.DoAct(State,u.Id,tile.x,tile.y))return;
            Armed=false;confirm=retreat=false;ClearHover(false);
            foreach(var fx in State.FxList.Where(f=>f.K=="die"))if(queue.ContainsKey(fx.Unit))queue[fx.Unit]="cancel";
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(ActRoutine());else Publish();
        }
        IEnumerator ActRoutine(){resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);yield return board.Animate(State,new List<Baskin.Fx>(State.FxList),false);resolving=false;Publish();}
        public void Undo(){if(!CanInput)return;if(Baskin.Undo(State)){Armed=false;confirm=false;CaptureRoster();ClearHover(false);Publish();}}
        public void EndTurn()
        {
            if(State!=null&&(State.Phase=="won"||State.Phase=="lost")){Continue();return;}
            if(!CanInput)return;
            if(confirmUnusedActions&&!confirm&&State.Units.Any(u=>u.Side=='p'&&!u.Dead&&(!u.Moved||!u.Acted))){confirm=true;Publish(false);return;}
            if(!Application.isPlaying||board==null){confirm=false;Selected='\0';Armed=false;State.Phase="enemy";ClearHover(false);Publish();return;}
            StartCoroutine(Machines());
        }
        IEnumerator Machines()
        {
            confirm=retreat=false;Selected='\0';Armed=false;ClearHover(false);Publish();
            machineBefore=State.Clone();executing=resolving=true;currentOrder=0;visibleLogs=State.Log.Count+1;
            eventLine=Loc.T("niyetler sırayla uygulanıyor");board.InputEnabled=false;unread=true;
            if(!Baskin.EndTurn(State)){executing=resolving=false;yield break;}Publish(false);
            yield return board.Animate(State,new List<Baskin.Fx>(State.FxList),true);
            foreach(var key in queue.Keys.ToArray())if(queue[key]!="cancel")queue[key]="done";
            executing=resolving=false;machineBefore=null;visibleLogs=int.MaxValue;eventUntil=Time.unscaledTime+.3f;Publish();
            yield return new WaitForSecondsRealtime(.3f);eventLine=null;if(State.Phase=="player")CaptureRoster();Publish(false);
        }
        void OnEffect(Baskin.Fx fx,int index,int count)
        {
            if(!executing)return;
            if(fx.K=="attack")
            {
                foreach(var id in queue.Keys.ToArray())if(queue[id]=="now")queue[id]="done";
                var i=roster.FirstOrDefault(x=>x.Unit==fx.Unit);if(i!=null){currentOrder=i.Order;queue[i.Unit]="now";}
                // Gerçek hedef yalnız yürütülen efekt başladıktan sonra açıklanır.
                var victim=Baskin.UnitAt(machineBefore,fx.X,fx.Y);
                eventLine=i?.Kind=="repair"?Loc.F("№{0} · hedef dolabı onarılıyor",currentOrder):Loc.F("№{0} · saldırı → {1}",currentOrder,victim?.Side=='p'?Name(Baskin.UnitTypes[victim.Type].Short[0]):Loc.T("hedef kare"));
            }
            // Canlandırılan anın sunum kopyası ilerler; strateji ve taktik sonuç durumu değiştirilmez.
            if(fx.K=="dmg")
            {
                var u=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);
                if(u!=null)u.Hp=Math.Max(0,u.Hp-fx.N);else if(machineBefore.Cells[fx.Y,fx.X].O is Baskin.Obj o)o.Hp=Math.Max(0,o.Hp-fx.N);
            }
            if(fx.K=="die"){var u=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);if(u!=null)u.Dead=true;if(queue.ContainsKey(fx.Unit))queue[fx.Unit]="cancel";}
            if(fx.K=="break"){if(machineBefore.Cells[fx.Y,fx.X].O?.K=="civ")machineBefore.Collateral++;machineBefore.Cells[fx.Y,fx.X].O=null;}
            if(fx.K=="heal"&&machineBefore.Cells[fx.Y,fx.X].O is Baskin.Obj healed)healed.Hp=Math.Min(healed.Max,healed.Hp+1);
            if(fx.K=="dmg")eventLine=Loc.F("{0} · −{1}",eventLine,fx.N);
            if(fx.K=="heal")eventLine=Loc.F("№{0} · onarım +1",currentOrder);
            if(fx.K=="spawn")eventLine=Loc.T("Yeni bir robot uyandı.");
            if(fx.K=="die"||fx.K=="break"||fx.K=="heal"||fx.K=="spawn")visibleLogs=Math.Min(State.Log.Count,visibleLogs+1);
            Publish(false);
        }
        public void Cancel()
        {
            if(Busy){if(panel!=null){panel=null;Publish(false);}return;}
            if(confirm||retreat){confirm=retreat=false;Publish(false);return;}
            if(Armed){Armed=false;Publish();return;}
            if(Selected!='\0'){ClearSelection();return;}
            if(panel!=null){panel=null;Publish(false);return;}
            paused=!paused;Publish(false);
        }
        public void Speed(bool on){if(!executing&&State?.Phase!="enemy")return;fast=on;if(board!=null){board.FastMachines=on;board.MachineDelay=on ? .08f : .25f;}Publish(false);}
        public void Retreat()
        {if(!CanInput)return;panel=null;if(!retreat){retreat=true;Publish(false);return;}Baskin.Retreat(State);retreat=false;Publish();}
        public void Panel(string id){if(retreat||State==null||State.Phase=="won"||State.Phase=="lost")return;panel=panel==id?null:id;if(id=="log")unread=false;Publish(false);}
        public void Continue(){if(Busy||State==null||State.Phase=="player"||State.Phase=="enemy")return;Finish();}
        protected abstract void Finish();
        protected void Publish()=>Publish(true);
        protected void Publish(bool redraw)
        {
            if(State==null)return;
            var display=executing?machineBefore:State;
            var unit=Unit();var moves=unit!=null&&!Armed&&Baskin.CanMove(State,unit)?Baskin.Reachable(State,unit):new List<Baskin.Pt>();
            var targets=unit!=null&&Armed&&Baskin.CanAct(State,unit)?Baskin.Targets(State,unit):new List<Baskin.Pt>();
            preview=hover.HasValue&&targets.Any(p=>p.X==hover.Value.x&&p.Y==hover.Value.y)?Baskin.Preview(State,unit.Id,hover.Value.x,hover.Value.y):null;
            bool loot=Baskin.IsLoot(State);
            var vm=new RaidViewModel{IsLootMission=loot,Turn=display.Turn,Turns=display.MaxTurns,Phase=executing?"enemy":State.Phase,Locked=State.Phase!="player"||Busy,AbilityArmed=Armed,CanUndo=State.Hist.Count>0,
                EndConfirm=confirm,RetreatArmed=retreat,FastMachines=fast,Paused=paused,OpenPanel=panel,LogUnread=unread,
                Civilians=civilianCount,CivilianBroken=display.Collateral,AbilityLabel=loot?Loc.T("İKMAL GÖREVİ"):Loc.T("KOPYALAMA"),AbilityTip=loot?Loc.T("Teknisyen ile ikmal kasasına yaklaş. Q ile yeteneği seç, kasaya tıkla ve malzemeleri al. Kasayı imha etme."):Loc.T("Her tur yeni robotlar uyanabilir."),
                Moves=moves.Select(p=>new Vector2Int(p.X,p.Y)).ToArray(),AbilityTargets=targets.Select(p=>new Vector2Int(p.X,p.Y)).ToArray()};
            vm.Team=new[]{'T','E','A'}.Select(letter=>
            {var u=display.Units.FirstOrDefault(x=>x.Side=='p'&&Baskin.UnitTypes[x.Type].Short==letter.ToString());if(u==null)return new RaidTeamModel{Letter=letter,Name=Name(letter),Thumb="thumb_standee_"+letter+"_done",Down=true};return new RaidTeamModel{Letter=letter,Name=Name(letter),Thumb="thumb_standee_"+letter+(u.Dead||u.Acted?"_done":letter=='E'&&u.Hp<u.Max?"_damaged":""),Hp=u.Hp,Max=u.Max,MoveRange=Baskin.UnitTypes[u.Type].Move,Selected=Selected==letter,Move=!Busy&&Baskin.CanMove(State,u),Ability=!Busy&&Baskin.CanAct(State,u),Armed=Selected==letter&&Armed,Down=u.Dead};}).ToArray();
            vm.Targets=targetForms.Select(t=>{var o=display.Cells[t.y,t.x].O;return new RaidTargetModel{Name=loot?Loc.T("İkmal kasası"):t.kind=="node"?Loc.T("Düğüm"):Loc.T("Çekirdek"),Thumb="thumb_cabinet_"+t.kind,Hp=o?.Hp??0,Max=t.max,Armor=true,Broken=o==null};}).ToArray();
            if(unit!=null)
            {
                vm.UnitCard=vm.Team.First(u=>u.Selected);var rows=new List<RaidCardRow>{new RaidCardRow{Icon="ic_boots_s",Text=Loc.T("Hareket"),Detail=Loc.F("{0} kare",vm.UnitCard.MoveRange),Used=!vm.UnitCard.Move},new RaidCardRow{Icon="ic_bolt_s",Text=Selected=='T'?(loot?Loc.T("İkmal alma"):Loc.T("Sabotaj")):Selected=='E'?Loc.T("EMP atışı"):Loc.T("Kanca"),Detail=Selected=='T'?Loc.T("bitişik kare"):Selected=='E'?Loc.T("düz hat, 2–5 kare"):Loc.T("düz hat, 5 kare"),Armed=Armed,Used=!vm.UnitCard.Ability}};
                vm.UnitRows=rows.ToArray();
                vm.Effects=Selected=='T'?new[]{loot?Loc.T("kasayı al"):Loc.T("−2 dolap"),Loc.T("−1 robot")}:Selected=='E'?new[]{Loc.T("−1 hedef"),Loc.T("itme")}:new[]{Loc.T("çeker")};
            }
            if(preview!=null)vm.CollisionTiles=preview.Fx.Where(f=>f.K=="push"&&!preview.Moves.Any(m=>m.Unit==f.Unit)).Select(f=>new Vector2Int(f.X+f.Dx,f.Y+f.Dy)).ToArray();
            if(hoverVisible&&hover.HasValue)vm.HoverCard=Inspector(hover.Value);
            vm.Status=confirm?Loc.T("Kullanılmamış haklar var. Enter ile turu bitir; Esc ile vazgeç."):retreat?Loc.T("Geri çekilmek için tekrar bas. Esc ile vazgeç."):paused?Loc.T("DURAKLATILDI · Esc ile devam"):panel=="log"?Loc.T("OPERASYON GÜNLÜĞÜ · Ekip harekete hazır."):panel=="rules"?Loc.T("1 / 2 / 3: seç · Q: yetenek · Z: geri al · Enter: turu bitir · Esc: vazgeç"):null;
            Enrich(vm,display);ApplyFixture(vm);Current=vm;if(redraw&&board!=null&&!Busy)Draw(vm);Changed?.Invoke(vm);
        }
        protected virtual void ApplyFixture(RaidViewModel vm){}
        RaidInspectorModel Inspector(Vector2Int tile)
        {
            var u=Baskin.UnitAt(State,tile.x,tile.y);var o=State.Cells[tile.y,tile.x].O;
            bool loot=Baskin.IsLoot(State);
            var card=new RaidInspectorModel{IsLootMission=loot,Title=u!=null?(u.Side=='p'?Name(Baskin.UnitTypes[u.Type].Short[0]):u.Type=="dron"?Loc.T("GÜVENLİK DRONU"):Loc.T("BAKIM ROBOTU")):o!=null?(loot&&Baskin.IsTarget(o.K)?Loc.T("İkmal kasası"):Loc.T(Baskin.ObjNames[o.K])):Loc.T("Boş kare"),
                Thumb=u!=null?(u.Side=='p'?"thumb_standee_"+Baskin.UnitTypes[u.Type].Short:u.Type=="dron"?"thumb_drone":"thumb_maint"):o!=null?(o.K=="cool"?"thumb_cooling":"thumb_cabinet_"+o.K):null,Hp=u?.Hp??o?.Hp??0,Max=u?.Max??o?.Max??0};
            var rows=new List<RaidCardRow>();
            if(preview!=null)
            {
                if(preview.Fx.Any(f=>f.K=="armor"))rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=Loc.T("Zırh: hasar işlemez")});
                var push=preview.Fx.FirstOrDefault(f=>f.K=="push");
                if(push!=null)rows.Add(new RaidCardRow{Icon="ic_push_s",Text=preview.Moves.Any(m=>m.Unit==push.Unit)?Loc.T("itme"):Loc.T("İtilir, üniteye çarpar"),Detail=Loc.T("−1")});
                if(preview.Fx.Any(f=>f.K=="break"&&f.Obj=="cool"))rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Ünite patlar"),Detail=Loc.T("−1")});
                foreach(var dead in preview.Fx.Where(f=>f.K=="die").Take(1))
                {
                    var intent=Baskin.Intents(State).FirstOrDefault(i=>i.Unit==dead.Unit);
                    rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Ateşi iptal"),Detail=intent!=null?Loc.F("<s>{0}</s>",intent.Order):null,Good=true});
                }
                if(rows.Count==0)foreach(var fx in preview.Fx.Where(f=>f.K=="dmg").Take(2))rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Öngörülen hasar"),Detail=Loc.F("−{0}",fx.N)});
                if(preview.Won)card.Win=true;
                if(preview.Fx.Any(f=>f.K=="loot"))rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Malzemeler alınır"),Good=true});
                if(loot&&preview.Fx.Any(f=>f.K=="break"&&Baskin.IsTarget(f.Obj)))rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Kasa imha olur; görev kaybedilir")});
            }
            else if(u!=null&&u.Side=='e')
            {
                var intent=Baskin.Intents(State).FirstOrDefault(i=>i.Unit==u.Id);
                if(intent!=null)rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.F("Niyet · sıra {0}",intent.Order),Detail=intent.Kind=="repair"?Loc.T("onarım"):Loc.T("ateş")});
            }
            else if(o!=null&&Baskin.IsTarget(o.K))rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=loot?Loc.T("Teknisyen ile bitişik kareden aç"):Loc.T("Hedef dolabı")});
            card.Rows=rows.Take(5).ToArray();return card;
        }
        void Draw(RaidViewModel vm)
        {
            var marks=new RaidBoard3D.Marks{Selected=Unit()?.Id??-1};foreach(var p in vm.Moves)marks.Moves.Add(p.y*8+p.x);foreach(var p in vm.AbilityTargets)marks.Targets.Add(p.y*8+p.x);
            foreach(var u in State.Units)if(u.Acted)marks.Spent.Add(u.Id);
            foreach(var p in State.Spawns)marks.Spawns.Add(p.Y*8+p.X);
            foreach(var i in Baskin.Intents(State))if(i.HasTarget&&!i.TargetNone){int key=i.TargetY*8+i.TargetX;if(i.Kind=="repair")marks.Heal.Add(key);else marks.Danger.Add(key);marks.Arrows.Add((i.FromX,i.FromY,i.TargetX,i.TargetY));}
            if(preview!=null)foreach(var fx in preview.Fx.Where(f=>f.K=="dmg"))marks.Danger.Add(fx.Y*8+fx.X);
            board.Sync(State,marks);
            if(previewOverlay!=null)previewOverlay.Render(vm.CollisionTiles);
            board.ClearPreview();if(preview!=null)
            {
                var labels=preview.Fx.Where(f=>f.K=="dmg").GroupBy(f=>(f.X,f.Y)).Select(g=>((float)g.Key.X,(float)g.Key.Y,Loc.F("−{0}",g.Sum(f=>f.N)),DeskTheme.Active.Get("sade.status.threat"))).ToList();
                foreach(var fx in preview.Fx.Where(f=>f.K=="armor"||f.K=="break"&&f.Obj=="cool"))labels.Add((fx.X,fx.Y,fx.K=="armor"?Loc.T("ZIRH"):Loc.T("PATLAR"),DeskTheme.Active.Get("sade.status.threat")));
                var pushes=preview.Fx.Where(f=>f.K=="push").ToArray();
                var arrows=pushes.Select(f=>(f.X,f.Y,f.X+f.Dx,f.Y+f.Dy)).ToList();
                var blocked=new HashSet<int>(Enumerable.Range(0,pushes.Length).Where(i=>!preview.Moves.Any(m=>m.Unit==pushes[i].Unit)));
                foreach(var fx in preview.Fx.Where(f=>f.K=="die"))board.SetIntentPresentation(fx.Unit,"cancel");
                
                board.SetPreview(labels,arrows,blocked);
            }
            board.InputEnabled=State.Phase=="player"&&!paused&&!Busy&&!retreat;
        }
        void Enrich(RaidViewModel vm,Baskin.BState display)
        {
            vm.LastTurn=display.Turn>=display.MaxTurns;vm.Finished=!Busy&&(State.Phase=="won"||State.Phase=="lost");
            vm.Queue=roster.Select(i=>new RaidQueueItem{Order=i.Order,State=queue.TryGetValue(i.Unit,out var q)?q:"pending"}).ToArray();
            vm.EventLine=eventLine;vm.EventVisible=executing||eventLine!=null&&Time.unscaledTime<eventUntil;
            vm.Log=State.Log.Take(Math.Min(visibleLogs,State.Log.Count)).Select(l=>new RaidLogModel{Text=l.Text,Kind=l.Kind}).ToArray();
            if(confirm)
            {
                vm.Unused=State.Units.Where(u=>u.Side=='p'&&!u.Dead&&(!u.Moved||!u.Acted)).Select(u=>new RaidUnusedModel{Letter=Baskin.UnitTypes[u.Type].Short[0],Name=Name(Baskin.UnitTypes[u.Type].Short[0]),Move=!u.Moved,Ability=!u.Acted,
                    ThreatOrder=Baskin.Intents(State).FirstOrDefault(i=>i.Kind!="repair"&&i.HasTarget&&!i.TargetNone&&i.TargetX==u.X&&i.TargetY==u.Y)?.Order??0}).ToArray();
                foreach(var t in vm.Team)t.Warn=vm.Unused.Any(u=>u.Letter==t.Letter);
            }
            if(vm.Finished)
            {
                var r=Baskin.Result(State);vm.Success=r.Success;vm.ResultTitle=r.Success?(Baskin.IsLoot(State)?Loc.T("İKMAL ALINDI"):Loc.T("HEDEF KIRILDI")):r.Retreat?Loc.T("GERİ ÇEKİLDİ"):State.Units.Where(u=>u.Side=='p').All(u=>u.Dead)?Loc.T("EKİP DÜŞTÜ"):Baskin.IsLoot(State)&&Baskin.TargetsLeft(State).Count==0?Loc.T("İKMAL KAYBEDİLDİ"):Loc.T("SÜRE DOLDU");
                vm.ResultSubtitle=Loc.F("{0} · {1} · Tur {2}/{3}",GameData.Regions.FirstOrDefault(d=>d.Id==State.Params.Region)?.Name??State.Params.RegionName??Loc.T("Saha"),Loc.T(State.Layout),r.Turns,State.MaxTurns);
                vm.ResultReason=r.Reason;vm.Standing=State.Units.Count(u=>u.Side=='p'&&!u.Dead);vm.TeamCount=State.Units.Count(u=>u.Side=='p');
                if(targetForms.Count>0){var t=targetForms[0];vm.Spot=new Vector3(t.x,t.y,r.Success?.15f:.45f);}
            }
        }
        static string Name(char letter)=>letter=='T'?Loc.T("TEKNİSYEN"):letter=='E'?Loc.T("EMP UZMANI"):Loc.T("ANALİST");
    }
}
