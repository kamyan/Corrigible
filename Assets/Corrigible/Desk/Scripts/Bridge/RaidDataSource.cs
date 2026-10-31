using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Audio;
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
        /// <summary>Eğitim baskınının rehberi: yalnız sıradaki adıma izin verir (yoksa baskın serbest).</summary>
        public RaidGuide Guide { get; protected set; }
        bool Guided=>Guide!=null&&!Guide.Done;
        protected bool confirm,retreat,fast,paused,unread=true;protected string panel;
        /// <summary>Telsiz kaydı en son açıldığında kayıtta kaç satır vardı (okunmamış sayısı).</summary>
        int seenLogs;
        protected bool resolving,executing;
        protected readonly List<(int x,int y,string kind,int max)> targetForms=new List<(int,int,string,int)>();
        protected int civilianCount;
        Baskin.BState machineBefore;
        List<Baskin.IntentView> roster=new List<Baskin.IntentView>();
        readonly Dictionary<int,string> queue=new Dictionary<int,string>();
        int currentOrder,visibleLogs=int.MaxValue;string eventLine;float eventUntil;
        protected Vector2Int? hover;protected float hoverAt;protected bool hoverVisible;
        protected Baskin.PreviewResult preview;
        protected virtual void OnEnable(){DeskLoc.Instance.Changed+=Publish;if(board!=null){board.TileClicked+=Click;board.HoverChanged+=BoardHover;}if(hud!=null)hud.TeamHovered+=TeamHover;if(board!=null)board.EffectPlaying+=OnEffect;if(hud!=null&&hud.Panels!=null)hud.Panels.ResultStamped+=ResultStamped;}
        protected virtual void OnDisable(){DeskLoc.Instance.Changed-=Publish;if(board!=null){board.TileClicked-=Click;board.HoverChanged-=BoardHover;}if(hud!=null)hud.TeamHovered-=TeamHover;if(board!=null)board.EffectPlaying-=OnEffect;if(hud!=null&&hud.Panels!=null)hud.Panels.ResultStamped-=ResultStamped;}
        // Sonuç damgası (S7): başarıda kısa zil, değilse boğuk “tak”
        static void ResultStamped(bool success)=>Sfx.Play(success?Cue.ResultBell:Cue.StampHeavy,.9f);
        protected abstract void Start();
        protected virtual void Update()
        {
            if(hover.HasValue&&!hoverVisible&&Time.unscaledTime-hoverAt>=theme.Number("raid.hoverCard.delayMs",150)/1000f){hoverVisible=true;Publish(false);}
        }
        protected void OpenState(Baskin.BState value,RaidGuide guide=null)
        {
            StopAllCoroutines();State=value;Guide=guide;finishedAt=-1;Selected='\0';Armed=confirm=retreat=fast=paused=resolving=executing=false;panel=null;hover=null;hoverVisible=false;preview=null;unread=true;seenLogs=0;visibleLogs=int.MaxValue;eventLine=null;
            targetForms.Clear();civilianCount=0;
            foreach(var t in Baskin.TargetsLeft(State))targetForms.Add((t.X,t.Y,t.O.K,t.O.Max));
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(State.Cells[y,x].O?.K=="civ")civilianCount++;
            CaptureRoster();if(board!=null){board.FastMachines=false;board.MachineDelay=.25f;board.Build(State);}Publish();
        }
        void CaptureRoster(){roster=Baskin.Intents(State);queue.Clear();foreach(var i in roster)queue[i.Unit]="pending";}
        public bool Busy=>resolving||(board!=null&&board.Busy);
        /// <summary>Makineler oynuyor (tur bitirildi, düşman hamleleri canlandırılıyor).</summary>
        public bool Executing=>executing;
        public void Select(char letter)
        {
            if(!CanInput)return;var u=Unit(letter);if(u==null||u.Dead)return;
            if(Guided&&!Guide.Expects(RaidGuideStep.Select,letter))return;
            Selected=letter;Armed=false;confirm=retreat=false;ClearHover(false);
            if(Guided)Guide.Advance();
            Publish();
        }
        bool CanInput=>State!=null&&State.Phase=="player"&&!paused&&!Busy;
        Baskin.Unit Unit(char letter)=>State?.Units.FirstOrDefault(u=>u.Side=='p'&&Baskin.UnitTypes[u.Type].Short==letter.ToString());
        Baskin.Unit Unit()=>Unit(Selected);
        public void ClearSelection(){if(Busy||Guided)return;Selected='\0';Armed=false;confirm=false;ClearHover(false);Publish();}
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
            var u=Unit();if(!CanInput||u==null)return;
            if(Guided&&!Guide.Expects(RaidGuideStep.Move,Selected,tile.x,tile.y))return;
            if(!Baskin.DoMove(State,u.Id,tile.x,tile.y))return;
            if(Guided)Guide.Advance();
            confirm=retreat=false;ClearHover(false);
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(MoveRoutine(u.Id,tile));else Publish();
        }
        IEnumerator MoveRoutine(int id,Vector2Int tile)
        {resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);yield return board.AnimateMove(id,tile.x,tile.y);resolving=false;Publish();}
        public void Arm()
        {
            var u=Unit();if(!CanInput||u==null||!Baskin.CanAct(State,u))return;
            if(Guided){if(Armed||!Guide.Expects(RaidGuideStep.Arm,Selected))return;Guide.Advance();}
            Armed=!Armed;confirm=false;Publish();
        }
        public void Use(Vector2Int tile)
        {
            var u=Unit();if(!CanInput||u==null||!Armed)return;
            if(Guided&&!Guide.Expects(RaidGuideStep.Use,Selected,tile.x,tile.y))return;
            if(!Baskin.DoAct(State,u.Id,tile.x,tile.y))return;
            if(Guided)Guide.Advance();
            Armed=false;confirm=retreat=false;ClearHover(false);
            foreach(var fx in State.FxList.Where(f=>f.K=="die"))if(queue.ContainsKey(fx.Unit))queue[fx.Unit]="cancel";
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(ActRoutine());else Publish();
        }
        IEnumerator ActRoutine(){resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);yield return board.Animate(State,new List<Baskin.Fx>(State.FxList),false);resolving=false;Publish();}
        public void Undo(){if(!CanInput||Guided)return;if(Baskin.Undo(State)){Armed=false;confirm=false;CaptureRoster();ClearHover(false);Publish();}}
        public void EndTurn()
        {
            if(State!=null&&(State.Phase=="won"||State.Phase=="lost")){Continue();return;}
            if(!CanInput)return;
            if(Guided&&!Guide.Expects(RaidGuideStep.End))return;
            // Eğitim baskınında boşta kalan uzman senaryonun parçası: onay sorulmaz
            if(confirmUnusedActions&&!confirm&&!Guided&&State.Units.Any(u=>u.Side=='p'&&!u.Dead&&(!u.Moved||!u.Acted))){confirm=true;Publish(false);return;}
            if(!Application.isPlaying||board==null){confirm=false;Selected='\0';Armed=false;State.Phase="enemy";ClearHover(false);Publish();return;}
            StartCoroutine(Machines());
        }
        IEnumerator Machines()
        {
            confirm=retreat=false;Selected='\0';Armed=false;ClearHover(false);Publish();
            machineBefore=State.Clone();executing=resolving=true;currentOrder=0;visibleLogs=State.Log.Count+1;
            // Emir telsizle sahaya gider; makinelerin hamlesini gözcü tek tek bildirir (OnEffect)
            eventLine=Loc.T("emir iletildi · gözcü bildiriyor");board.InputEnabled=false;unread=true;Sfx.Play(Cue.RadioSend);
            if(!Baskin.EndTurn(State)){executing=resolving=false;yield break;}Publish(false);
            yield return board.Animate(State,new List<Baskin.Fx>(State.FxList),true);
            foreach(var key in queue.Keys.ToArray())if(queue[key]!="cancel")queue[key]="done";
            executing=resolving=false;machineBefore=null;visibleLogs=int.MaxValue;eventUntil=Time.unscaledTime+.3f;
            if(Guided&&Guide.Expects(RaidGuideStep.End))Guide.Advance();
            Publish();
            yield return new WaitForSecondsRealtime(.3f);eventLine=null;if(State.Phase=="player")CaptureRoster();Publish(false);
        }
        void OnEffect(Baskin.Fx fx,int index,int count)
        {
            if(!executing)return;
            if(fx.K=="attack"||fx.K=="spawn")Sfx.Play(Cue.RadioReport,.7f);
            if(fx.K=="attack")
            {
                foreach(var id in queue.Keys.ToArray())if(queue[id]=="now")queue[id]="done";
                var i=roster.FirstOrDefault(x=>x.Unit==fx.Unit);if(i!=null){currentOrder=i.Order;queue[i.Unit]="now";}
                // Gerçek hedef yalnız yürütülen efekt başladıktan sonra açıklanır.
                var victim=Baskin.UnitAt(machineBefore,fx.X,fx.Y);
                // Gözcü kareyi paftadaki adıyla bildirir ("C3")
                string cell=RaidBoard3D.CellName(fx.X,fx.Y);
                string to=victim?.Side=='p'?Loc.F("{0} · {1}",Name(Baskin.UnitTypes[victim.Type].Short[0]),cell):cell;
                eventLine=i?.Kind switch{"repair"=>Loc.F("№{0} · hedef dolabı onarılıyor",currentOrder),"shield"=>Loc.F("№{0} · makine zırhlanıyor",currentOrder),
                    "clone"=>Loc.F("№{0} · yeni dron uyanıyor",currentOrder),"blast"=>Loc.F("№{0} · patlama",currentOrder),"lock"=>Loc.F("№{0} · kilit → {1}",currentOrder,to),
                    _=>Loc.F("№{0} · saldırı → {1}",currentOrder,to)};
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
            if(fx.K=="spawn")eventLine=Loc.F("Yeni bir robot uyandı: {0}.",RaidBoard3D.CellName(fx.X,fx.Y));
            if(fx.K=="die"||fx.K=="break"||fx.K=="heal"||fx.K=="spawn")visibleLogs=Math.Min(State.Log.Count,visibleLogs+1);
            Publish(false);
        }
        public void Cancel()
        {
            if(Busy){if(panel!=null){panel=null;Publish(false);}return;}
            if(confirm||retreat){confirm=retreat=false;Publish(false);return;}
            // Eğitim baskınında seçim ve Q geri alınmaz (rehber adımı kayar); Esc yalnız paneli kapatır ya da duraklatır
            if(Armed&&!Guided){Armed=false;Publish();return;}
            if(Selected!='\0'&&!Guided){ClearSelection();return;}
            if(panel!=null){panel=null;Publish(false);return;}
            paused=!paused;Publish(false);
        }
        public void Speed(bool on){if(!executing&&State?.Phase!="enemy")return;fast=on;if(board!=null){board.FastMachines=on;board.MachineDelay=on ? .08f : .25f;}Publish(false);}
        public void Retreat()
        {if(!CanInput||Guided)return;panel=null;if(!retreat){retreat=true;Publish(false);return;}Baskin.Retreat(State);retreat=false;Publish();}
        public void Panel(string id){if(retreat||State==null||State.Phase=="won"||State.Phase=="lost")return;panel=panel==id?null:id;if(id=="log"){unread=false;seenLogs=State.Log.Count;}Publish(false);}
        // Sonuç kartı gelene kadar (S7: kararma 0,4 s + kayma 0,25 s) girdi kilitli
        public void Continue(){if(Busy||State==null||State.Phase=="player"||State.Phase=="enemy"||finishedAt<0||Time.unscaledTime-finishedAt<ResultLockSeconds)return;Finish();}
        const float ResultLockSeconds=.6f;
        float finishedAt=-1;
        /// <summary>Sonuç kartının yer satırı; harekâtta bölge ve sektör (“Doğu Avrupa · 02 Tahliye geçidi”).</summary>
        protected virtual string ResultPlace=>Loc.F("{0} · {1}",GameData.Regions.FirstOrDefault(d=>d.Id==State.Params.Region)?.Name??State.Params.RegionName??Loc.T("Saha"),Loc.T(State.Layout));
        protected abstract void Finish();
        /// <summary>Harekâtın sivil güveni (baskından önceki değer, tavan, sunucu başına bedel); harekât yoksa tavan 0.</summary>
        protected virtual (int Trust,int Max,int PerCivilian) TrustStake=>(0,0,0);
        protected void Publish()=>Publish(true);
        protected void Publish(bool redraw)
        {
            if(State==null)return;
            // Beklenen hamle tahtada yapılamıyorsa (olmamalı) rehber çekilir, baskın serbest oynanır
            if(Guided&&State.Phase=="player"&&!Busy&&!Guide.Possible(State,Selected)){Debug.LogWarning("Eğitim baskını: beklenen hamle yapılamıyor, rehber kalktı.");Guide=null;}
            var display=executing?machineBefore:State;
            var unit=Unit();var moves=unit!=null&&!Armed&&Baskin.CanMove(State,unit)?Baskin.Reachable(State,unit):new List<Baskin.Pt>();
            var targets=unit!=null&&Armed&&Baskin.CanAct(State,unit)?Baskin.Targets(State,unit):new List<Baskin.Pt>();
            preview=hover.HasValue&&targets.Any(p=>p.X==hover.Value.x&&p.Y==hover.Value.y)?Baskin.Preview(State,unit.Id,hover.Value.x,hover.Value.y):null;
            bool loot=Baskin.IsLoot(State);
            var vm=new RaidViewModel{IsLootMission=loot,Escaping=loot&&State.Carrier!=0&&State.Phase!="won"&&State.Phase!="lost",Turn=display.Turn,Turns=display.MaxTurns,Phase=executing?"enemy":State.Phase,Locked=State.Phase!="player"||Busy,AbilityArmed=Armed,CanUndo=State.Hist.Count>0&&!Guided,
                EndConfirm=confirm,RetreatArmed=retreat,FastMachines=fast,Paused=paused,OpenPanel=panel,LogUnread=unread,
                Civilians=civilianCount,CivilianBroken=display.Collateral,AbilityLabel=RegionLabel(loot),AbilityTip=RegionTip(loot),
                Moves=moves.Select(p=>new Vector2Int(p.X,p.Y)).ToArray(),AbilityTargets=targets.Select(p=>new Vector2Int(p.X,p.Y)).ToArray()};
            var stake=TrustStake;vm.TrustMax=stake.Max;vm.Trust=Math.Max(0,stake.Trust-display.Collateral*stake.PerCivilian);
            // Kilit niyetinin hedef karesindeki uzman: kartında “KİLİT TEHDİDİ · #n” (S6c)
            var locks=State.Phase=="player"?Baskin.Intents(State).Where(i=>i.Kind=="lock"&&i.HasTarget&&!i.TargetNone).ToList():new List<Baskin.IntentView>();
            string Threat(Baskin.Unit u){var i=u.Dead?null:locks.FirstOrDefault(x=>x.TargetX==u.X&&x.TargetY==u.Y);return i==null?null:Loc.F("KİLİT TEHDİDİ · #{0}",i.Order);}
            vm.Team=new[]{'T','E','A'}.Select(letter=>
            // Kadroda olmayan uzman sığınakta yaralı (yedek de yok): kart kapalı, YARALI damgalı
            {var u=display.Units.FirstOrDefault(x=>x.Side=='p'&&Baskin.UnitTypes[x.Type].Short==letter.ToString());if(u==null)return new RaidTeamModel{Letter=letter,Name=Name(letter),Thumb="thumb_standee_"+letter+"_done",Down=true,Absent=true};return new RaidTeamModel{Letter=letter,Name=Name(letter),Thumb="thumb_standee_"+letter+(u.Dead||u.Acted?"_done":letter=='E'&&u.Hp<u.Max?"_damaged":""),Hp=u.Hp,Max=u.Max,MoveRange=Baskin.UnitTypes[u.Type].Move,Selected=Selected==letter,Move=!Busy&&Baskin.CanMove(State,u),Ability=!Busy&&Baskin.CanAct(State,u),Armed=Selected==letter&&Armed,Down=u.Dead,Armor=u.Armor,Rookie=Rookie(u),Threat=Threat(u)};}).ToArray();
            vm.Targets=targetForms.Select(t=>{var o=display.Cells[t.y,t.x].O;return new RaidTargetModel{Name=loot?Loc.T("İkmal kasası"):t.kind=="node"?Loc.T("Düğüm"):Loc.T("Çekirdek"),Thumb="thumb_cabinet_"+t.kind,Hp=o?.Hp??0,Max=t.max,Armor=true,Broken=o==null,Cell=RaidBoard3D.CellName(t.x,t.y)};}).ToArray();
            // Görev çipi (Revizyon 2 · S6): bölge özelliğinin işareti, amaç satırı; telsizin okunmamış satırları; makine hamlesi
            var trait=State.Params?.Traits?.FirstOrDefault(t=>Baskin.RegionTraits.ContainsKey(t));
            vm.TraitGlyph=trait!=null?SiginakGlyphs.Trait(trait,true):null;
            vm.Goal=loot?(vm.Escaping?Loc.T("Amaç: malzemeyi girişe taşı"):Loc.T("Amaç: kasadan ikmal al")):vm.Targets.Length>1?Loc.T("Amaç: ikisini de kır"):Loc.T("Amaç: kır");
            vm.LogUnreadCount=unread?Math.Max(1,State.Log.Count-seenLogs):0;
            vm.MachineMoves=roster.Count;vm.MachineMove=Math.Min(roster.Count,queue.Values.Count(q=>q=="done"||q=="cancel")+(queue.Values.Any(q=>q=="now")?1:0));
            if(unit!=null)
            {
                vm.UnitCard=vm.Team.First(u=>u.Selected);var rows=new List<RaidCardRow>{new RaidCardRow{Icon="ic_boots_s",Text=Loc.T("Hareket"),Detail=Loc.F("{0} kare",vm.UnitCard.MoveRange),Used=!vm.UnitCard.Move},new RaidCardRow{Icon="ic_bolt_s",Text=Selected=='T'?(loot?Loc.T("İkmal alma"):Loc.T("Sabotaj")):Selected=='E'?Loc.T("EMP atışı"):Loc.T("Kanca"),Detail=Selected=='T'?Loc.T("bitişik kare"):Selected=='E'?Loc.T("düz hat, 2–5 kare"):Loc.T("düz hat, 5 kare"),Armed=Armed,Used=!vm.UnitCard.Ability}};
                // Kartta üç satır yeri var: yedek olduğu başlıkta (“· YEDEK”), plaka üçüncü satırda
                rows.AddRange(KitRows(unit,false));
                vm.UnitRows=rows.ToArray();
                // Revizyon 2 · S6 (Soru 7'nin ara cevabı): yalnız adlar; görev eylemi yeşil, robota vuran kırmızı
                vm.Effects=Selected=='T'?new[]{loot?Loc.T("Kasadan ikmal al"):Loc.T("Dolaba vur · −2 can"),Loc.T("Robota vur · −1 can")}:Selected=='E'?new[]{Loc.T("−1 hedef"),Loc.T("itme")}:new[]{Loc.T("Robota vur · −1 can"),Loc.T("çeker")};
                vm.EffectTones=Selected=='T'?new[]{"good","bad"}:new[]{"bad","plain"};
            }
            if(preview!=null)vm.CollisionTiles=preview.Fx.Where(f=>f.K=="push"&&!preview.Moves.Any(m=>m.Unit==f.Unit)).Select(f=>new Vector2Int(f.X+f.Dx,f.Y+f.Dy)).ToArray();
            if(hoverVisible&&hover.HasValue)vm.HoverCard=Inspector(hover.Value);
            vm.Status=confirm?Loc.T("Kullanılmamış haklar var. Enter ile emri ilet; Esc ile vazgeç."):retreat?Loc.T("Geri çekilmek için tekrar bas. Esc ile vazgeç."):paused?Loc.T("DURAKLATILDI · Esc ile devam"):panel=="log"?Loc.T("TELSİZ KAYDI · Ekip harekete hazır."):panel=="rules"?Loc.T("1 / 2 / 3: seç · Q: yetenek · Z: geri al · Enter: emri ilet · Esc: vazgeç"):null;
            Enrich(vm,display);ApplyFixture(vm);Current=vm;if(board!=null)board.LabelsHidden=vm.Finished;if(redraw&&board!=null&&!Busy)Draw(vm);Changed?.Invoke(vm);
        }
        protected virtual void ApplyFixture(RaidViewModel vm){}
        /// <summary>Kadro sığınaktan gelir: yedek uzman (acemi) ve takılı zırh plakası.</summary>
        bool Rookie(Baskin.Unit u)=>State?.Params?.Squad?.FirstOrDefault(s=>s.Type==u.Type)?.Rookie==true;
        IEnumerable<RaidCardRow> KitRows(Baskin.Unit u,bool rookie)
        {
            if(u.Armor>0)yield return new RaidCardRow{Icon="mk_armor_13",Text=Loc.T("Zırh plakası"),Detail=Loc.T("ilk hasarı karşılar"),Good=true};
            if(rookie&&Rookie(u))yield return new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Yedek uzman"),Detail=Loc.T("bir eksik canla")};
        }
        RaidInspectorModel Inspector(Vector2Int tile)
        {
            var u=Baskin.UnitAt(State,tile.x,tile.y);var o=State.Cells[tile.y,tile.x].O;
            bool loot=Baskin.IsLoot(State);
            var card=new RaidInspectorModel{IsLootMission=loot,Title=u!=null?(u.Side=='p'?Name(Baskin.UnitTypes[u.Type].Short[0]):DeskText.Upper(Baskin.UnitTypes[u.Type].Name)):o!=null?(loot&&Baskin.IsTarget(o.K)?Loc.T("İkmal kasası"):Loc.T(Baskin.ObjNames[o.K])):Loc.T("Boş kare"),
                Thumb=u!=null?(u.Side=='p'?"thumb_standee_"+Baskin.UnitTypes[u.Type].Short:EnemyThumb(u.Type)):o!=null?(o.K=="cool"?"thumb_cooling":"thumb_cabinet_"+o.K):null,Hp=u?.Hp??o?.Hp??0,Max=u?.Max??o?.Max??0};
            var rows=new List<RaidCardRow>();
            if(u!=null&&u.Side=='p')rows.AddRange(KitRows(u,true));
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
                if(intent!=null)rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.F("Niyet · sıra {0}",intent.Order),Detail=IntentName(intent.Kind)});
                if(u.Armor>0)rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=Loc.T("Zırh"),Detail=Loc.T("ilk hasarı karşılar")});
                if(u.Type==State.Signature)rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Bölgenin imza makinesi")});
                rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Baskin.UnitTypes[u.Type].Desc});
            }
            else if(o!=null&&Baskin.IsTarget(o.K))rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=loot?Loc.T("Teknisyen ile bitişik kareden aç, malzemeyi girişe taşı"):Loc.T("Hedef dolabı")});
            // Kaçış: çıkış karesi; taşıyıcı seçiliyken oraya yürümek baskını kazandırır
            if(State.Carrier!=0&&State.Exits.Any(p=>p.X==tile.x&&p.Y==tile.y))
            {
                if(u==null&&o==null)card.Title=Loc.T("Çıkış");
                rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Teknisyen malzemeyle buradan tahliye edilir"),Good=true});
                var carrier=State.Units.FirstOrDefault(v=>v.Id==State.Carrier);
                if(carrier!=null&&Unit()==carrier&&Baskin.CanMove(State,carrier)&&Baskin.Reachable(State,carrier).Any(p=>p.X==tile.x&&p.Y==tile.y))card.Win=true;
            }
            card.Rows=rows.Take(5).ToArray();return card;
        }
        void Draw(RaidViewModel vm)
        {
            var guideTile=Guided&&State.Phase=="player"?Guide.Tile(State):null;
            var marks=new RaidBoard3D.Marks{Selected=Unit()?.Id??-1,Guide=guideTile.HasValue?guideTile.Value.Y*8+guideTile.Value.X:-1,Carrier=State.Carrier!=0?State.Carrier:-1};
            // Kasa alındıysa çıkışlar (giriş kareleri) yeşil kesik çerçeveyle
            if(State.Carrier!=0&&State.Phase=="player")foreach(var p in State.Exits)marks.Exits.Add(p.Y*8+p.X);foreach(var p in vm.Moves)marks.Moves.Add(p.y*8+p.x);foreach(var p in vm.AbilityTargets)marks.Targets.Add(p.y*8+p.x);
            foreach(var u in State.Units)if(u.Acted)marks.Spent.Add(u.Id);
            // Kum fırtınası: uyanacak makinelerin yeri görünmez (çoğaltıcının niyeti yine görünür)
            if(!State.Fog)foreach(var p in State.Spawns)marks.Spawns.Add(p.Y*8+p.X);
            foreach(var i in Baskin.Intents(State))if(i.HasTarget&&!i.TargetNone)
            {
                int key=i.TargetY*8+i.TargetX;
                if(i.Kind=="blast"){foreach(var a in i.Area)marks.Danger.Add(a.Y*8+a.X);continue;}
                if(i.Kind=="repair"||i.Kind=="shield")marks.Heal.Add(key);else if(i.Kind=="clone")marks.Clones.Add(key);else marks.Danger.Add(key);
                marks.Arrows.Add((i.FromX,i.FromY,i.TargetX,i.TargetY));
            }
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
                    ThreatOrder=Baskin.Intents(State).FirstOrDefault(i=>Threatens(i,u.X,u.Y))?.Order??0}).ToArray();
                foreach(var t in vm.Team)t.Warn=vm.Unused.Any(u=>u.Letter==t.Letter);
            }
            if(vm.Finished)
            {
                if(finishedAt<0)finishedAt=Time.unscaledTime;
                // Revizyon 2 · S7: damga tek ve başlık yerinde (başarıda SEKTÖR GERİ ALINDI); altında yer satırı ve üç satır
                var r=Baskin.Result(State);bool loot=Baskin.IsLoot(State);vm.Success=r.Success;
                vm.ResultTitle=r.Success?Loc.T("SEKTÖR GERİ ALINDI"):r.Retreat?Loc.T("GERİ ÇEKİLDİ"):State.Units.Where(u=>u.Side=='p').All(u=>u.Dead)?Loc.T("EKİP DÜŞTÜ"):loot&&Baskin.TargetsLeft(State).Count==0?Loc.T("İKMAL KAYBEDİLDİ"):Loc.T("SÜRE DOLDU");
                vm.ResultTurn=Loc.F("TUR {0}/{1}",r.Turns,State.MaxTurns);vm.ResultPlace=ResultPlace;
                int standing=State.Units.Count(u=>u.Side=='p'&&!u.Dead),team=State.Units.Count(u=>u.Side=='p');
                vm.ResultRows=vm.Targets.Select(t=>new RaidResultRow{Label=t.Name,Value=t.Broken?(loot&&r.Success?Loc.T("alındı"):Loc.T("kırıldı")):Loc.F("{0}/{1}",t.Hp,t.Max),Good=t.Broken&&r.Success})
                    .Append(new RaidResultRow{Label=Loc.T("Kırılan sivil sunucu"),Value=display.Collateral.ToString()}).Append(new RaidResultRow{Label=Loc.T("Ekip"),Value=Loc.F("{0}/{1} ayakta",standing,team)}).ToArray();
                // Spot sonucun nedenini açıkta bırakır: alınan kasada Teknisyen, öteki hâllerde hedef
                var carrier=loot&&r.Success?State.Units.FirstOrDefault(u=>u.Side=='p'&&!u.Dead&&u.Type=="tek"):null;
                if(carrier!=null)vm.Spot=new Vector3(carrier.X,carrier.Y,.3f);
                else if(targetForms.Count>0){var t=targetForms[0];vm.Spot=new Vector3(t.x,t.y,r.Success?.15f:.45f);}
            }
        }
        static string Name(char letter)=>letter=='T'?Loc.T("TEKNİSYEN"):letter=='E'?Loc.T("EMP UZMANI"):Loc.T("ANALİST");
        /// <summary>Niyet bu kareye zarar verir mi (onarım, kalkan, çoğaltma vermez; patlama çevresine verir).</summary>
        static bool Threatens(Baskin.IntentView i,int x,int y)=>i.HasTarget&&!i.TargetNone&&i.Kind!="repair"&&i.Kind!="shield"&&i.Kind!="clone"&&
            (i.Kind=="blast"?i.Area.Any(a=>a.X==x&&a.Y==y):i.TargetX==x&&i.TargetY==y);
        static string IntentName(string kind)=>kind switch{"repair"=>Loc.T("onarım"),"melee"=>Loc.T("vuruş"),"lob"=>Loc.T("havan"),"blast"=>Loc.T("patlama"),
            "lock"=>Loc.T("kilit"),"shield"=>Loc.T("kalkan"),"clone"=>Loc.T("çoğaltma"),_=>Loc.T("ateş")};
        /// <summary>Havada olan makineler dron resmiyle, yerdekiler bakım robotu resmiyle.</summary>
        static string EnemyThumb(string type)=>type switch{"bakim"=>"thumb_maint","zdron"=>"thumb_armordrone","kalkan"=>"thumb_shielddrone","havan"=>"thumb_mortar","bomba"=>"thumb_bomber","kilit"=>"thumb_locker","cogalt"=>"thumb_replicator",_=>"thumb_drone"};
        /// <summary>Görev çipi: bölgenin özelliği (yoksa görev türü).</summary>
        string RegionLabel(bool loot)
        {
            var trait=State.Params?.Traits?.FirstOrDefault(t=>Baskin.RegionTraits.ContainsKey(t));
            return trait!=null?DeskText.Upper(Loc.T(Baskin.RegionTraits[trait].Name)):loot?Loc.T("İKMAL GÖREVİ"):Loc.T("KOPYALAMA");
        }
        /// <summary>Çipin açıklaması (kart dört satır): bölge özelliği, imza makinesi ve kademe; bölgesiz baskında görev ipucu.</summary>
        string RegionTip(bool loot)
        {
            var parts=new List<string>();
            var p=State.Params;
            var trait=p?.Traits?.FirstOrDefault(t=>Baskin.RegionTraits.ContainsKey(t));
            if(trait==null)return loot?Loc.T("Teknisyen ile ikmal kasasına yaklaş. Q ile yeteneği seç, kasaya tıkla ve malzemeleri al. Kasayı imha etme."):Loc.T("Her tur yeni robotlar uyanabilir.");
            parts.Add(Loc.F("{0}: {1}",Loc.T(Baskin.RegionTraits[trait].Name),Loc.T(Baskin.RegionTraits[trait].Desc)));
            // Makinenin ne yaptığı ipucu kartında (üstüne gel); burada yalnız adı
            if(State.Signature!=null)parts.Add(Loc.F("İmza makinesi: {0}",Baskin.UnitTypes[State.Signature].Name));
            if(p!=null)parts.Add(TierText(p.Tier));
            return string.Join("\n",parts);
        }
        /// <summary>Zorluk kademesi cümlesi (görev çipi, görev fişi).</summary>
        public static string TierText(int tier)=>tier switch
        {
            1=>Loc.F("Tehdit {0}/4: makineler alışılmış düzende.",1),
            2=>Loc.F("Tehdit {0}/4: başta bir makine fazla.",2),
            3=>Loc.F("Tehdit {0}/4: iki makine fazla, turda iki uyanma.",3),
            _=>Loc.F("Tehdit {0}/4: üç makine fazla, turda iki uyanma, canları +1.",4),
        };
    }
}
