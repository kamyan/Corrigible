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
        [Tooltip("Yalnız editörde: baskını bu bina kitiyle çiz (weu, eeu… ya da default); MapSeed 0'da da geçerli. Boşsa kit baskının " +
                 "bölgesinden (harekât baskını) ya da varsayılan (MapSeed 0).")]
        [SerializeField] protected string kitOverride;
        public RaidViewModel Current { get; private set; }
        /// <summary>Baskının bina kiti (kit adları için, <see cref="RaidKitText"/>): tahta varsa tahtanın kiti, yoksa aynı kuralla
        /// (<see cref="RaidKitText.KitFor"/>); varsayılan kitte "default".</summary>
        public string KitId { get; private set; } = RaidKitText.Default;
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
        /// <summary>Bölge olayının katmanı ve birim uyarıları (yetenek önizlemesinde hamleden sonrası, yürüyüş karesinin üstündeyken
        /// birim oraya yürürse olacak); olay yoksa null.</summary>
        protected Baskin.HazardView hazards;
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
            CaptureRoster();
            // Bina kiti tahtaya tek noktadan: harekât baskını bölgenin kitini alır, MapSeed 0 varsayılanı; editörde kitOverride zorlar.
            // Kit adları tahtanın kitinden (tahtasızken aynı kural)
            bool forced=Application.isEditor&&!string.IsNullOrEmpty(kitOverride);
            KitId=RaidKitText.KitFor(forced?kitOverride:State.Params?.Region,State.Params?.MapSeed??0,forced);
            if(board!=null)
            {
                board.SetDress(forced?kitOverride:State.Params?.Region,State.Params?.MapSeed??0,forced);
                if(board.Kit!=null)KitId=board.Kit.id;
                board.FastMachines=false;board.MachineDelay=.25f;board.Build(State);
            }
            // Binanın sesi bölgeden (kitten değil): bölgenin ortam yatağı; eğitimde seyrek ayrıntılar kapalı
            Sfx.RaidSite(State.Params?.Region,guide==null);
            Publish();
        }
        void CaptureRoster(){roster=Baskin.Intents(State);queue.Clear();foreach(var i in roster)queue[i.Unit]="pending";}
        public bool Busy=>resolving||(board!=null&&board.Busy);
        /// <summary>Makineler oynuyor (tur bitirildi, düşman hamleleri canlandırılıyor).</summary>
        public bool Executing=>executing;
        public void Select(char letter)
        {
            if(!CanInput)return;var u=Unit(letter);if(u==null||!Baskin.OnBoard(u))return;
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
        void TeamHover(char letter){var u=Unit(letter);if(u!=null&&Baskin.OnBoard(u)&&board!=null)board.SetExternalHover((u.X,u.Y));}
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
            // Çıkışa varan uzman tahtadan kalkar: seçim de kalkar
            if(u.Out){Selected='\0';Armed=false;}
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(MoveRoutine(u.Id,tile));else Publish();
        }
        IEnumerator MoveRoutine(int id,Vector2Int tile)
        {
            resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);yield return board.AnimateMove(id,tile.x,tile.y);
            // Kaçışta çıkışa varan uzman tahtadan ayrılır; telsiz notu ayrılırken görünür, süresi dolunca kalkar
            if(State.Units.FirstOrDefault(u=>u.Id==id)?.Out==true){eventLine=Loc.F("{0} tahliye edildi",Name(Baskin.UnitTypes[State.Units.First(u=>u.Id==id).Type].Short[0]));eventUntil=Time.unscaledTime+1.2f;Publish(false);yield return board.AnimateLeave(id);}
            resolving=false;Publish();
            if(eventLine!=null&&!executing){yield return new WaitForSecondsRealtime(Mathf.Max(0,eventUntil-Time.unscaledTime));if(!executing)Publish(false);}
        }
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
            if(!Baskin.OnBoard(u))Selected='\0';
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
            if(confirmUnusedActions&&!confirm&&!Guided&&State.Units.Any(u=>u.Side=='p'&&Baskin.OnBoard(u)&&(!u.Moved||!u.Acted))){confirm=true;Publish(false);return;}
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
            // Core robotları yeni plana doğrudan taşır; tahtada yeni yerlerine kare kare, tek tek yürürler
            if(board.MachinesWillMove(State)){eventLine=Loc.T("gözcü · makineler yer değiştiriyor");Publish(false);yield return board.AnimateMachineMoves(State);}
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
                // Gerçek hedef yalnız yürütülen efekt başladıktan sonra açıklanır; sise ya da buluta kesilen saldırı kimseyi adlandırmaz
                var next=index+1<State.FxList.Count?State.FxList[index+1]:null;
                bool cut=next!=null&&next.Unit==fx.Unit&&(next.K=="smoked"||next.K=="clouded");
                var victim=cut||machineBefore.Cells[fx.Y,fx.X].Smoke?null:Baskin.UnitAt(machineBefore,fx.X,fx.Y);
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
            if(fx.K=="die"){var u=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);if(u!=null){u.Dead=true;u.Left=State.Units.FirstOrDefault(x=>x.Id==fx.Unit)?.Left==true;}if(queue.ContainsKey(fx.Unit))queue[fx.Unit]="cancel";}
            if(fx.K=="evac"){var u=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);if(u!=null){u.Out=true;eventLine=Loc.F("{0} tahliye edildi",Name(Baskin.UnitTypes[u.Type].Short[0]));}}
            if(fx.K=="break"){if(machineBefore.Cells[fx.Y,fx.X].O?.K=="civ")machineBefore.Collateral++;machineBefore.Cells[fx.Y,fx.X].O=null;}
            if(fx.K=="heal"&&machineBefore.Cells[fx.Y,fx.X].O is Baskin.Obj healed)healed.Hp=Math.Min(healed.Max,healed.Hp+1);
            if(fx.K=="dmg")eventLine=Loc.F("{0} · −{1}",eventLine,fx.N);
            if(fx.K=="smoked")eventLine=Loc.F("№{0} · sise kesildi",currentOrder);
            if(fx.K=="heal")eventLine=Loc.F("№{0} · onarım +1",currentOrder);
            if(fx.K=="spawn")eventLine=Loc.F("Yeni bir robot uyandı: {0}.",RaidBoard3D.CellName(fx.X,fx.Y));
            if(fx.K=="clouded")eventLine=Loc.F("№{0} · buluta kesildi",currentOrder);
            // Bölge olayının adımı: telsiz olayın adını ve karesini bildirir; kabin yolcusu sunum kopyasında da tahtadan iner, çıkar
            if(fx.K=="ev")
            {
                // Bütün tahtaya işleyen adımın (bulutun dağılması, faz değişimi) karesi yok
                string evName=State.Ev!=null?Loc.T(Baskin.RegionEvents[State.Ev.Id].Name):null;
                if(evName!=null)eventLine=fx.Obj=="bulut_dagil"||fx.Obj=="faz"?evName:Loc.F("{0} · {1}",evName,RaidBoard3D.CellName(fx.X,fx.Y));
                var rider=fx.Unit!=0?machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit):null;
                if(rider!=null&&fx.Obj=="kabin_kalk")rider.Cab=true;
                if(rider!=null&&fx.Obj=="kabin_in"){rider.Cab=false;rider.X=fx.X;rider.Y=fx.Y;}
                Sfx.Play(Cue.RadioReport,.5f);
            }
            if(fx.K=="die"||fx.K=="break"||fx.K=="heal"||fx.K=="spawn"||fx.K=="evac"||fx.K=="ev")visibleLogs=Math.Min(State.Log.Count,visibleLogs+1);
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
        {if(!CanInput||Guided||State.Escape)return;panel=null;if(!retreat){retreat=true;Publish(false);return;}Baskin.Retreat(State);retreat=false;Publish();}
        public void Panel(string id){if(retreat||State==null||State.Phase=="won"||State.Phase=="lost")return;panel=panel==id?null:id;if(id=="log"){unread=false;seenLogs=State.Log.Count;}Publish(false);}
        // Sonuç kartı gelene kadar (S7: kararma 0,4 s + kayma 0,25 s) girdi kilitli
        public void Continue(){if(Busy||State==null||State.Phase=="player"||State.Phase=="enemy"||finishedAt<0||Time.unscaledTime-finishedAt<ResultLockSeconds)return;Finish();}
        const float ResultLockSeconds=.6f;
        float finishedAt=-1;
        /// <summary>Sonuç kartının yer satırı; harekâtta bölge ve sektör (“Doğu Avrupa · 02 Dosya katı”).</summary>
        protected virtual string ResultPlace=>Loc.F("{0} · {1}",GameData.Regions.FirstOrDefault(d=>d.Id==State.Params.Region)?.Name??State.Params.RegionName??Loc.T("Saha"),Baskin.RoomName(State));
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
            var walk=preview==null&&hover.HasValue&&moves.Any(p=>p.X==hover.Value.x&&p.Y==hover.Value.y)?hover:null;
            hazards=State.Ev==null?null:preview!=null?preview.Hazards:walk.HasValue?Baskin.PreviewMove(State,unit.Id,walk.Value.x,walk.Value.y):Baskin.Hazards(State);
            bool loot=Baskin.IsLoot(State);
            // Taşıyıcı ve kaçış sunum kopyasından: makine evresinde düşüş ya da tahliye efekti oynayınca görünür
            var carrier=display.Carrier!=0?display.Units.FirstOrDefault(u=>u.Id==display.Carrier):null;
            var vm=new RaidViewModel{IsLootMission=loot,Escaping=display.Escape&&State.Phase!="won"&&State.Phase!="lost",CargoLost=carrier!=null&&carrier.Dead,CanRetreat=!State.Escape&&!Guided,Turn=display.Turn,Turns=display.MaxTurns,Phase=executing?"enemy":State.Phase,Locked=State.Phase!="player"||Busy,AbilityArmed=Armed,CanUndo=State.Hist.Count>0&&!Guided,
                EndConfirm=confirm,RetreatArmed=retreat,FastMachines=fast,Paused=paused,OpenPanel=panel,LogUnread=unread,
                Civilians=civilianCount,CivilianBroken=display.Collateral,AbilityLabel=RegionLabel(loot),AbilityTip=RegionTip(loot),
                Moves=moves.Select(p=>new Vector2Int(p.X,p.Y)).ToArray(),AbilityTargets=targets.Select(p=>new Vector2Int(p.X,p.Y)).ToArray()};
            var stake=TrustStake;vm.TrustMax=stake.Max;vm.Trust=Math.Max(0,stake.Trust-display.Collateral*stake.PerCivilian);
            // Kilit niyetinin hedef karesindeki uzman: kartında “KİLİT TEHDİDİ · #n” (S6c)
            var locks=State.Phase=="player"?Baskin.Intents(State).Where(i=>i.Kind=="lock"&&i.HasTarget&&!i.TargetNone).ToList():new List<Baskin.IntentView>();
            // Bölge olayının uyarısı (“KAYNAK: 1 hasar”): kilit tehdidinden sonra; asansör kabinindeki ve donmuş uzman da kartında
            string Threat(Baskin.Unit u){if(u.Cab&&!u.Dead)return Loc.T("KABİNDE · bir evre tahtada yok");var i=!Baskin.OnBoard(u)?null:locks.FirstOrDefault(x=>x.TargetX==u.X&&x.TargetY==u.Y);
                if(i!=null)return Loc.F("KİLİT TEHDİDİ · #{0}",i.Order);var w=Baskin.OnBoard(u)?hazards?.Warnings.FirstOrDefault(x=>x.Unit==u.Id):null;return w!=null?w.Text:u.Frozen&&Baskin.OnBoard(u)?Loc.T("DONMUŞ · bu tur yürüyemez"):null;}
            vm.Team=Roster().Select(letter=>
            // Kadroda olmayan uzman sığınakta yaralı (yedek de yok): kart kapalı, YARALI damgalı
            {var u=display.Units.FirstOrDefault(x=>x.Side=='p'&&Baskin.UnitTypes[x.Type].Short==letter.ToString());if(u==null)return new RaidTeamModel{Letter=letter,Name=Name(letter),Thumb="thumb_standee_"+letter+"_done",Down=true,Absent=true};return new RaidTeamModel{Letter=letter,Name=Name(letter),Thumb="thumb_standee_"+letter+(u.Dead||u.Out||u.Acted?"_done":letter=='E'&&u.Hp<u.Max?"_damaged":""),Hp=u.Hp,Max=u.Max,MoveRange=Baskin.UnitTypes[u.Type].Move,Selected=Selected==letter,Move=!Busy&&Baskin.CanMove(State,u),Ability=!Busy&&Baskin.CanAct(State,u),Armed=Selected==letter&&Armed,Down=u.Dead,Out=u.Out,Left=u.Left,Armor=u.Armor,Rookie=Rookie(u),Threat=Threat(u)};}).ToArray();
            vm.Targets=targetForms.Select(t=>{var o=display.Cells[t.y,t.x].O;return new RaidTargetModel{Name=loot?Loc.T("İkmal kasası"):t.kind=="node"?Loc.T("Düğüm"):Loc.T("Çekirdek"),Thumb=loot?"thumb_crate":"thumb_cabinet_"+t.kind,Hp=o?.Hp??0,Max=t.max,Armor=true,Broken=o==null,Cell=RaidBoard3D.CellName(t.x,t.y)};}).ToArray();
            // Görev çipi (Revizyon 2 · S6): bölge özelliğinin işareti, amaç satırı; telsizin okunmamış satırları; makine hamlesi
            var trait=State.Params?.Traits?.FirstOrDefault(t=>Baskin.RegionTraits.ContainsKey(t));
            vm.TraitGlyph=trait!=null?SiginakGlyphs.Trait(trait,true):null;
            vm.Place=Room!=null?DeskText.Upper(Room):null;
            vm.TurnEvents=hazards?.Turns;
            // Kurallar kartının lejantında kanalın binadaki adı (varsayılan kitte yok)
            vm.CableName=RaidKitText.Cable(KitId);
            // Kaçışta amaç ekibi girişten çıkarmak (taşıyıcı tahtadaysa malzemeyle)
            vm.Goal=vm.Escaping?(carrier!=null&&Baskin.OnBoard(carrier)?Loc.T("Amaç: malzemeyle girişten çık"):Loc.T("Amaç: ekibi girişten çıkar")):loot?Loc.T("Amaç: kasadan ikmal al"):vm.Targets.Length>1?Loc.T("Amaç: ikisini de kır"):Loc.T("Amaç: kır");
            vm.LogUnreadCount=unread?Math.Max(1,State.Log.Count-seenLogs):0;
            vm.MachineMoves=roster.Count;vm.MachineMove=Math.Min(roster.Count,queue.Values.Count(q=>q=="done"||q=="cancel")+(queue.Values.Any(q=>q=="now")?1:0));
            if(unit!=null)
            {
                vm.UnitCard=vm.Team.First(u=>u.Selected);var ability=Baskin.UnitTypes[unit.Type].Ability;
                var rows=new List<RaidCardRow>{new RaidCardRow{Icon="ic_boots_s",Text=Loc.T("Hareket"),Detail=Loc.F("{0} kare",vm.UnitCard.MoveRange),Used=!vm.UnitCard.Move},new RaidCardRow{Icon="ic_bolt_s",Text=AbilityName(ability,loot),Detail=AbilityRange(ability,unit.Charges),Armed=Armed,Used=!vm.UnitCard.Ability}};
                // Kartta üç satır yeri var: yedek olduğu başlıkta (“· YEDEK”), plaka üçüncü satırda
                rows.AddRange(KitRows(unit,false));
                vm.UnitRows=rows.ToArray();
                // Revizyon 2 · S6 (Soru 7'nin ara cevabı): yalnız adlar; görev eylemi yeşil, robota vuran kırmızı
                vm.Effects=ability switch{"sabotaj"=>new[]{loot?Loc.T("Kasadan ikmal al"):Loc.T("Dolaba vur · −2 can"),Loc.T("Robota vur · −1 can")},"emp"=>new[]{Loc.T("−1 hedef"),Loc.T("itme")},
                    "dinamit"=>new[]{Loc.T("Duvarı yık"),Loc.T("Robota vur · −1 can")},"duman"=>new[]{Loc.T("Nişanlı ateşi keser"),Loc.T("bu tur")},_=>new[]{Loc.T("Robota vur · −1 can"),Loc.T("çeker")}};
                vm.EffectTones=ability=="sabotaj"||ability=="dinamit"?new[]{"good","bad"}:ability=="duman"?new[]{"good","plain"}:new[]{"bad","plain"};
            }
            if(preview!=null)vm.CollisionTiles=preview.Fx.Where(f=>f.K=="push"&&!preview.Moves.Any(m=>m.Unit==f.Unit)).Select(f=>new Vector2Int(f.X+f.Dx,f.Y+f.Dy)).ToArray();
            if(hoverVisible&&hover.HasValue)vm.HoverCard=Inspector(hover.Value);
            vm.Status=confirm?Loc.T("Kullanılmamış haklar var. Enter ile emri ilet; Esc ile vazgeç."):retreat?Loc.T("Geri çekilmek için tekrar bas. Esc ile vazgeç."):paused?Loc.T("DURAKLATILDI · Esc ile devam"):panel=="log"?(Room!=null?Loc.F("TELSİZ KAYDI · Ekip {0} girişinde.",Room):Loc.T("TELSİZ KAYDI · Ekip harekete hazır.")):panel=="rules"?Loc.T("1 / 2 / 3: seç · Q: yetenek · Z: geri al · Enter: emri ilet · Esc: vazgeç"):null;
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
                Thumb=u!=null?(u.Side=='p'?"thumb_standee_"+Baskin.UnitTypes[u.Type].Short:EnemyThumb(u.Type)):o!=null?(loot&&Baskin.IsTarget(o.K)?"thumb_crate":ObjectThumb(o.K)):null,Hp=u?.Hp??o?.Hp??0,Max=u?.Max??o?.Max??0};
            var rows=new List<RaidCardRow>();
            var cell=State.Cells[tile.y,tile.x];
            if(u==null&&o==null&&cell.T=="wall")card.Title=Loc.T("Duvar");
            if(u==null&&o==null&&cell.T=="cable")card.Title=Loc.T("Elektrikli kanal");
            if(u!=null&&u.Side=='p')rows.AddRange(KitRows(u,true));
            // Sis bu turun makine evresinde nişanlı ateşi keser
            if(cell.Smoke)rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Sisli kare: nişanlı ateş kesilir"),Detail=Loc.T("bu tur"),Good=true});
            // Bölge olayının karesi (bulut, kaynak, çatlak…): ne olduğu ve bu tur sonu ne olacağı
            if(hazards!=null)foreach(var hm in hazards.Marks.Where(m=>m.X==tile.x&&m.Y==tile.y&&m.Kind!="ghost").Take(2))
            {var row=HazardRow(hm.Kind);if(row.Text!=null)rows.Add(new RaidCardRow{Icon=row.Bad?"ic_bolt_s":"ic_eye_s",Text=row.Text,Detail=hm.Label});}
            if(o!=null&&o.K=="kol")rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Kaide hasar almaz; her darbe kolu bir adım fazla döndürür")});
            if(o!=null&&o.K=="tup")rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Vurulursa bir tur sonra çizili hattında uçar")});
            if(preview!=null)
            {
                if(preview.Fx.Any(f=>f.K=="armor"))rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=Loc.T("Zırh: hasar işlemez")});
                var push=preview.Fx.FirstOrDefault(f=>f.K=="push");
                if(push!=null)rows.Add(new RaidCardRow{Icon="ic_push_s",Text=preview.Moves.Any(m=>m.Unit==push.Unit)?Loc.T("itme"):Loc.T("İtilir, üniteye çarpar"),Detail=Loc.T("−1")});
                if(preview.Fx.Any(f=>f.K=="break"&&f.Obj=="cool"))rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Ünite patlar"),Detail=Loc.T("−1")});
                if(preview.Fx.Any(f=>f.K=="breach"))rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Duvar yıkılır"),Detail=Loc.F("kalan {0}",Math.Max(0,(Unit()?.Charges??1)-1)),Good=true});
                foreach(var fx in preview.Fx.Where(f=>f.K=="debris"))if(preview.Fx.Any(f=>f.K=="dmg"&&f.X==fx.X&&f.Y==fx.Y))rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Moloz"),Detail=Loc.T("−1")});
                // Sise kesilecek saldırılar: hamleden önce kesik olmayan niyetler
                if(preview.Intents!=null)foreach(var cut in preview.Intents.Where(i=>i.Smoked&&!Baskin.Intents(State).Any(j=>j.Unit==i.Unit&&(j.Smoked||j.Cloud))).Take(2))
                    rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Sise kesilir"),Detail=Loc.F("<s>{0}</s>",cut.Order),Good=true});
                foreach(var dead in preview.Fx.Where(f=>f.K=="die").Take(1))
                {
                    var intent=Baskin.Intents(State).FirstOrDefault(i=>i.Unit==dead.Unit);
                    rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Ateşi iptal"),Detail=intent!=null?Loc.F("<s>{0}</s>",intent.Order):null,Good=true});
                }
                if(rows.Count==0)foreach(var fx in preview.Fx.Where(f=>f.K=="dmg").Take(2))rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Öngörülen hasar"),Detail=Loc.F("−{0}",fx.N)});
                if(preview.Won)card.Win=true;else if(preview.Escape)card.Escape=true;
                if(preview.Fx.Any(f=>f.K=="loot"))rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Malzemeler alınır"),Good=true});
                if(preview.Escape)rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Kaçış başlar: bütün ekip girişten çıkmalı"),Good=true});
                if(loot&&preview.Fx.Any(f=>f.K=="break"&&Baskin.IsTarget(f.Obj)))rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Kasa imha olur; görev kaybedilir")});
            }
            else if(u!=null&&u.Side=='e')
            {
                var intent=Baskin.Intents(State).FirstOrDefault(i=>i.Unit==u.Id);
                if(intent!=null)rows.Add(intent.Smoked?new RaidCardRow{Icon="ic_x_s",Text=Loc.F("Niyet {0} sise kesildi",intent.Order),Good=true}
                    :intent.Cloud?new RaidCardRow{Icon="ic_x_s",Text=Loc.F("Niyet {0} buluta kesildi",intent.Order),Good=true}
                    :new RaidCardRow{Icon="ic_bolt_s",Text=Loc.F("Niyet · sıra {0}",intent.Order),Detail=IntentName(intent.Kind)});
                if(u.Armor>0)rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=Loc.T("Zırh"),Detail=Loc.T("ilk hasarı karşılar")});
                if(u.Type==State.Signature)rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Bölgenin imza makinesi")});
                rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Baskin.UnitTypes[u.Type].Desc});
            }
            else if(o!=null&&Baskin.IsTarget(o.K))rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=loot?Loc.T("Teknisyen ile bitişik kareden aç; sonra bütün ekip girişten çıkar"):Loc.T("Hedef dolabı: kırılınca bütün ekip girişten çıkar")});
            else if(u==null&&o==null&&cell.T=="wall")rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=State.Units.Any(v=>v.Side=='p'&&Baskin.OnBoard(v)&&v.Type=="maden")?Loc.T("Madenci dinamitle yıkabilir"):Loc.T("Geçilmez; atışı durdurur")});
            // Kanalın kuralı (Baskin.CableHurt): üstüne yürümek güvenli, itilen ya da çekilen 1 hasar alır; şebeke olayında yalnız canlı
            // fazın kanalı yakar (sönük kanal sıradan zemin; evreyi canlı kanalda bitirenin hasarı olay satırında)
            else if(u==null&&o==null&&cell.T=="cable")rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=State.Ev?.Id=="faz"?Loc.T("Yalnız canlı fazda üstüne itilen ya da çekilen birim 1 hasar alır."):Loc.T("Üstünden yürünür; üstüne itilen ya da çekilen birim 1 hasar alır.")});
            // Kaçış: çıkış karesi; seçili uzman oraya yürürse tahtadan çıkar. Son uzmanın çıkışı baskını bitirir (ikmalde malzeme
            // dışarıdaysa kazanır)
            // Mekânın binadaki adı başlığın altında, gri (kural adı başlıkta kalır): duvar, kanal, gedik; varsayılan kitte yok
            card.Subtitle=RaidKitText.Subtitle(State,tile.x,tile.y,KitId);
            if(State.Escape&&State.Exits.Any(p=>p.X==tile.x&&p.Y==tile.y))
            {
                if(u==null&&o==null){card.Title=Loc.T("Çıkış");card.Subtitle=null;}
                var carrier=State.Carrier!=0?State.Units.FirstOrDefault(v=>v.Id==State.Carrier):null;
                bool cargoInside=carrier!=null&&Baskin.OnBoard(carrier);
                rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=cargoInside?Loc.T("Buradan çıkan uzman tahliye edilir; malzeme Teknisyen'le çıkar"):Loc.T("Buradan çıkan uzman tahliye edilir"),Good=true});
                var sel=Unit();
                if(sel!=null&&Baskin.CanMove(State,sel)&&Baskin.Reachable(State,sel).Any(p=>p.X==tile.x&&p.Y==tile.y))
                {
                    bool last=Baskin.Alive(State,'p').Count==1;
                    if(last&&(!loot||carrier!=null&&(carrier.Out||carrier==sel)))card.Win=true;
                    else if(!last)rows.Add(new RaidCardRow{Icon="ic_boots_s",Text=Loc.F("Tahtada kalan: {0}",Baskin.Alive(State,'p').Count-1),Detail=Loc.F("{0} tur",State.MaxTurns-State.Turn+1)});
                }
            }
            card.Rows=rows.Take(5).ToArray();return card;
        }
        void Draw(RaidViewModel vm)
        {
            var guideTile=Guided&&State.Phase=="player"?Guide.Tile(State):null;
            var marks=new RaidBoard3D.Marks{Selected=Unit()?.Id??-1,Guide=guideTile.HasValue?guideTile.Value.Y*8+guideTile.Value.X:-1,Carrier=State.Carrier!=0?State.Carrier:-1};
            // Kaçışta çıkışlar (giriş kareleri) yeşil kesik çerçeveyle
            if(State.Escape&&State.Phase=="player")foreach(var p in State.Exits)marks.Exits.Add(p.Y*8+p.X);foreach(var p in vm.Moves)marks.Moves.Add(p.y*8+p.x);foreach(var p in vm.AbilityTargets)marks.Targets.Add(p.y*8+p.x);
            foreach(var u in State.Units)if(u.Acted)marks.Spent.Add(u.Id);
            // Kum fırtınası: uyanacak makinelerin yeri görünmez (çoğaltıcının niyeti yine görünür)
            if(!State.Fog)foreach(var p in State.Spawns)marks.Spawns.Add(p.Y*8+p.X);
            // Sis: canlı durumdaki sisli kareler; önizlemede atılacak sis ve hamleden sonraki niyetler (sise kesilen, dinamitin açtığı hat)
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(State.Cells[y,x].Smoke)marks.Smoke.Add(y*8+x);
            if(preview!=null)foreach(var fx in preview.Fx.Where(f=>f.K=="smoke"))marks.SmokePreview.Add(fx.Y*8+fx.X);
            var shown=preview?.Intents!=null&&preview.Fx.Any(f=>f.K=="smoke"||f.K=="breach")?preview.Intents:Baskin.Intents(State);
            foreach(var i in shown)if(i.Smoked||i.Cloud)marks.SmokedArrows.Add((i.FromX,i.FromY,i.TargetX,i.TargetY,i.Kind,i.Cloud));
            foreach(var i in shown)if(i.HasTarget&&!i.TargetNone)
            {
                int key=i.TargetY*8+i.TargetX;
                if(i.Kind=="blast"){foreach(var a in i.Area)marks.Danger.Add(a.Y*8+a.X);continue;}
                if(i.Kind=="repair"||i.Kind=="shield")marks.Heal.Add(key);else if(i.Kind=="clone")marks.Clones.Add(key);else marks.Danger.Add(key);
                marks.Arrows.Add((i.FromX,i.FromY,i.TargetX,i.TargetY));
            }
            if(preview!=null)foreach(var fx in preview.Fx.Where(f=>f.K=="dmg"))marks.Danger.Add(fx.Y*8+fx.X);
            marks.Hazards=hazards;
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
            // Kaçışta çıkışa yürüyüşün önizlemesi: kâğıtta ÇIKAR, ok seçili uzmandan
            else if(hover.HasValue&&State.Escape&&vm.Moves.Contains(hover.Value)&&State.Exits.Any(p=>p.X==hover.Value.x&&p.Y==hover.Value.y)&&Unit() is Baskin.Unit mover)
                board.SetPreview(new[]{((float)hover.Value.x,(float)hover.Value.y,Loc.T("ÇIKAR"),DeskTheme.Active.Get("ink.green"))},new[]{(mover.X,mover.Y,hover.Value.x,hover.Value.y)});
            // Yürüyüş karesinde bölge olayının bu tur sonu ona yapacağı (kaynak, çökecek tavan, canlı kanal…)
            else if(hover.HasValue&&vm.Moves.Contains(hover.Value)&&Unit() is Baskin.Unit walker&&hazards?.Warnings.FirstOrDefault(w=>w.Unit==walker.Id) is Baskin.HazardWarn hw)
                board.SetPreview(new[]{((float)hover.Value.x,(float)hover.Value.y,hw.Damage>0?Loc.F("−{0}",hw.Damage):Loc.T("OLAY"),DeskTheme.Active.Get(hw.Damage>0?"sade.status.threat":"ink.ink2"))},new[]{(walker.X,walker.Y,hover.Value.x,hover.Value.y)});
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
                vm.Unused=State.Units.Where(u=>u.Side=='p'&&Baskin.OnBoard(u)&&(!u.Moved||!u.Acted)).Select(u=>new RaidUnusedModel{Letter=Baskin.UnitTypes[u.Type].Short[0],Name=Name(Baskin.UnitTypes[u.Type].Short[0]),Move=!u.Moved,Ability=!u.Acted,
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
                int standing=State.Units.Count(u=>u.Side=='p'&&!u.Dead),gone=State.Units.Count(u=>u.Side=='p'&&u.Out),team=State.Units.Count(u=>u.Side=='p');
                // Kaçış olduysa ekip satırı çıkanları sayar (çıkamayan düşmüş sayılır)
                var squad=State.Escape?new RaidResultRow{Label=Loc.T("Tahliye edilen"),Value=Loc.F("{0}/{1}",gone,team),Good=gone==team}:new RaidResultRow{Label=Loc.T("Ekip"),Value=Loc.F("{0}/{1} ayakta",standing,team)};
                vm.ResultRows=vm.Targets.Select(t=>new RaidResultRow{Label=t.Name,Value=t.Broken?(loot&&r.Success?Loc.T("alındı"):loot&&vm.CargoLost?Loc.T("kayboldu"):Loc.T("kırıldı")):Loc.F("{0}/{1}",t.Hp,t.Max),Good=t.Broken&&r.Success})
                    .Append(new RaidResultRow{Label=Loc.T("Kırılan sivil sunucu"),Value=display.Collateral.ToString()}).Append(squad).ToArray();
                // Spot sonucun nedenini açıkta bırakır: hedef (alınan ya da kırılan kasa, dolap)
                if(targetForms.Count>0){var t=targetForms[0];vm.Spot=new Vector3(t.x,t.y,r.Success?.15f:.45f);}
            }
        }
        static string Name(char letter)=>letter switch{'T'=>Loc.T("TEKNİSYEN"),'E'=>Loc.T("EMP UZMANI"),'M'=>Loc.T("MADENCİ"),'S'=>Loc.T("SİS UZMANI"),_=>Loc.T("ANALİST")};
        /// <summary>Baskındaki kadronun harfleri (giriş sırasıyla): Teknisyen ve seçilen iki uzman; yedeği olmadığı için çıkamayan da kart alır.</summary>
        IEnumerable<char> Roster()
        {
            var p=State.Params;var roles=p?.Roles;
            if(roles==null||roles.Count==0)
            {
                // Elle kurulan baskın: kadronun türleri; eksik üçlü varsayılan uzmanlarla (yatıyor) tamamlanır (Baskin.SquadReport da aynı)
                roles=p?.Squad!=null?p.Squad.Select(s=>s.Type).ToList():new List<string>();
                foreach(var r in new[]{"tek","emp","ana"})if(roles.Count<3&&!roles.Contains(r))roles.Add(r);
                roles=roles.OrderBy(r=>Array.IndexOf(Simulation.Roles,r)).ToList();
            }
            return roles.Where(Baskin.UnitTypes.ContainsKey).Select(r=>Baskin.UnitTypes[r].Short[0]);
        }
        static string AbilityName(string ability,bool loot)=>ability switch{"sabotaj"=>loot?Loc.T("İkmal alma"):Loc.T("Sabotaj"),"emp"=>Loc.T("EMP atışı"),"dinamit"=>Loc.T("Dinamit"),"duman"=>Loc.T("Sis bombası"),_=>Loc.T("Kanca")};
        /// <summary>Yetenek satırının menzili; sınırlı yetenekte kalan hak.</summary>
        static string AbilityRange(string ability,int charges)=>ability switch{"sabotaj"=>Loc.T("bitişik kare"),"emp"=>Loc.T("düz hat, 2–5 kare"),
            "dinamit"=>Loc.F("bitişik · {0} dinamit",Math.Max(0,charges)),"duman"=>Loc.F("düz hat, 1–4 · {0} bomba",Math.Max(0,charges)),_=>Loc.T("düz hat, 5 kare")};
        /// <summary>Niyet bu kareye zarar verir mi (onarım, kalkan, çoğaltma vermez; patlama çevresine verir).</summary>
        static bool Threatens(Baskin.IntentView i,int x,int y)=>i.HasTarget&&!i.TargetNone&&i.Kind!="repair"&&i.Kind!="shield"&&i.Kind!="clone"&&
            (i.Kind=="blast"?i.Area.Any(a=>a.X==x&&a.Y==y):i.TargetX==x&&i.TargetY==y);
        static string IntentName(string kind)=>kind switch{"repair"=>Loc.T("onarım"),"melee"=>Loc.T("vuruş"),"lob"=>Loc.T("havan"),"blast"=>Loc.T("patlama"),
            "lock"=>Loc.T("kilit"),"shield"=>Loc.T("kalkan"),"clone"=>Loc.T("çoğaltma"),_=>Loc.T("ateş")};
        /// <summary>Havada olan makineler dron resmiyle, yerdekiler bakım robotu resmiyle.</summary>
        /// <summary>Nesnenin küçük resmi: dolaplar, soğutma ünitesi ve olay nesneleri (tüp, kaynak kolu kaidesi, tavan enkazı) kendi
        /// prefab'larından çizilir (SiginakTextureBaker.PieceThumbs).</summary>
        static string ObjectThumb(string kind)=>kind switch{"cool"=>"thumb_cooling","tup"=>"thumb_tube","kol"=>"thumb_weld","enkaz"=>"thumb_debris","civ"=>"thumb_civ","spawner"=>"thumb_spawner",_=>"thumb_cabinet_"+kind};
        static string EnemyThumb(string type)=>type switch{"bakim"=>"thumb_maint","zdron"=>"thumb_armordrone","kalkan"=>"thumb_shielddrone","havan"=>"thumb_mortar","bomba"=>"thumb_bomber","kilit"=>"thumb_locker","cogalt"=>"thumb_replicator",_=>"thumb_drone"};
        /// <summary>Bölge olayı karesinin hover satırı (<see cref="Baskin.HazardMark.Kind"/>); Bad: üstünde durana zarar verebilir.</summary>
        static (string Text,bool Bad) HazardRow(string kind)=>kind switch
        {
            "cloud"=>(Loc.T("Söndürme bulutu: buraya ve buradan nişan alınmaz"),false),"cloud_next"=>(Loc.T("Bu tur sonunda bulut dolar"),false),"nozzle"=>(Loc.T("Söndürme memesi"),false),
            "rail"=>(Loc.T("Arşiv rafı: kayar duvar"),false),"rail_gap"=>(Loc.T("Rafın geçidi"),false),"rail_close"=>(Loc.T("Bu tur sonunda raf kapanır: 1 hasar ve itme"),true),"rail_open"=>(Loc.T("Bu tur sonunda geçit açılır"),false),
            "gas"=>(Loc.T("Yanıcı gaz: ateş değerse bir tur sonra parlar"),false),"gas_next"=>(Loc.T("Bu tur sonunda gaz dolar"),false),"gas_lit"=>(Loc.T("Alev aldı: parlayınca 1 hasar"),true),"leak"=>(Loc.T("Gaz sızıntısı"),false),
            "crack"=>(Loc.T("Çatlak tavan: yanındaki darbe çökertir"),false),"crack_due"=>(Loc.T("Tavan çökecek: altındakine 1 hasar, boşsa enkaz"),true),
            "stop"=>(Loc.T("Asansör durağı: kabin kalkınca üstündeki yolculuk eder"),false),"stop_cab"=>(Loc.T("Kabin bu durakta"),false),
            "freeze"=>(Loc.T("Kriyo bölgesi"),false),"freeze_next"=>(Loc.T("Bu tur sonunda donar: yürüyemez, buz 1 hasar emer"),true),"hol"=>(Loc.T("Turnike holü: kenarlar tek yöne döner"),false),
            "weld"=>(Loc.T("Bu evre kaynak: 1 hasar"),true),"weld_next"=>(Loc.T("Sıradaki kaynak karesi"),false),
            "live"=>(Loc.T("Canlı kanal: evre sonunda 1 hasar"),true),"dead"=>(Loc.T("Sönük kanal: güvenli"),false),
            "drain"=>(Loc.T("Süzgeç: 3. ve 6. tur sonunda çevresini çeker"),false),"drain_next"=>(Loc.T("Bu tur sonunda süzgeç çeker"),true),
            "tube_hot"=>(Loc.T("Tüp ıslık çalıyor: çizili hattında uçacak"),true),
            "oil"=>(Loc.T("Yağ: itilen, çekilen ya da vurulan kayar"),false),"oil_next"=>(Loc.T("2. tur sonunda yağ dökülür"),false),"hose"=>(Loc.T("Hidrolik hortum"),false),
            _=>(null,false),
        };
        /// <summary>Görev çipi: bölgenin özelliği (yoksa görev türü).</summary>
        string RegionLabel(bool loot)
        {
            var trait=State.Params?.Traits?.FirstOrDefault(t=>Baskin.RegionTraits.ContainsKey(t));
            return trait!=null?DeskText.Upper(Loc.T(Baskin.RegionTraits[trait].Name)):loot?Loc.T("İKMAL GÖREVİ"):Loc.T("KOPYALAMA");
        }
        /// <summary>Çipin açıklaması (kart dört satır): bölge özelliği, imza makinesi ve kademe; bölgesiz baskında görev ipucu.</summary>
        /// <summary>Baskının odası (bölgenin binasında; harekât dışında ya da sektörsüz baskında null).</summary>
        string Room{get{var p=State?.Params;var region=GameData.Regions.FirstOrDefault(d=>d.Id==p?.Region);int i=GameData.SectorIndex(p?.SectorId);return region!=null&&i>=0?region.Sector(i):null;}}
        string RegionTip(bool loot)
        {
            var parts=new List<string>();
            var p=State.Params;
            // Yer: bina ve oda (görev çipinin başı odayı büyük harfle yazar)
            var region=GameData.Regions.FirstOrDefault(d=>d.Id==p?.Region);int sector=GameData.SectorIndex(p?.SectorId);
            if(region!=null&&sector>=0)parts.Add(Loc.F("Yer · {0} · {1:00} {2}",region.Building,sector+1,region.Sector(sector)));
            var trait=p?.Traits?.FirstOrDefault(t=>Baskin.RegionTraits.ContainsKey(t));
            if(trait==null){parts.Add(loot?Loc.T("Teknisyen ile ikmal kasasına yaklaş. Q ile yeteneği seç, kasaya tıkla ve malzemeleri al. Kasayı imha etme."):Loc.T("Her tur yeni robotlar uyanabilir."));return string.Join("\n",parts);}
            parts.Add(Loc.F("{0}: {1}",Loc.T(Baskin.RegionTraits[trait].Name),Loc.T(Baskin.RegionTraits[trait].Desc)));
            // Bölge olayı (tur şeridinde altı çizili turlar olayın işlediği turlar)
            if(State.Ev!=null)parts.Add(Loc.F("Olay · {0}: {1}",Loc.T(Baskin.RegionEvents[State.Ev.Id].Name),Loc.T(Baskin.RegionEvents[State.Ev.Id].Desc)));
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
