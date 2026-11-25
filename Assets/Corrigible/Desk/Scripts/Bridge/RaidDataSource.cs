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
        /// <summary>Tahta (rehberin işaretçisi kareleri buradan ekrana izdüşürür; yalnız okur).</summary>
        public RaidBoard3D Board => board;
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
        /// <summary>
        /// Tesis hamlesi kipi (aşama 7): E ya da kart satırı seçili uzmanın bir hamlesini kurar; tahtadaki hedefe tık
        /// <see cref="Baskin.DoAction"/>'ı verir. Q, Esc, sağ tık, başka seçim, Z ve başarılı hamle kipi kapatır. Yetenek kipiyle
        /// (<see cref="Armed"/>) aynı anda açık olmaz. Örnek tahtada (Room null) hiç açılmaz.
        /// </summary>
        public bool ActionArmed { get; protected set; }
        string actionKind;int actionOther;
        /// <summary>Kurulu hamlenin türü (<see cref="Baskin.ActionKinds"/>) ve sürüklenen gövde; kip kapalıyken null / 0.</summary>
        public string ActionKind=>ActionArmed?actionKind:null;
        public int ActionOther=>ActionArmed?actionOther:0;
        /// <summary>
        /// Tesis aşama 9: seçili uzman olmayan birim (köpek ya da tutsak; birim kimliği, 0 yok). Uzmanlar harfle (<see cref="Selected"/>)
        /// seçilir; ikisi aynı anda dolu olmaz. Aygıt seçilmez. Örnek tahtada hep 0.
        /// </summary>
        public int SelectedOther { get; protected set; }
        /// <summary>
        /// Tesis aşama 9: ekipman kipi (R ya da kartın ekipman satırı): seçili uzmanın ekipman eylemi kurulu; tahtadaki hedefe tık
        /// <see cref="Baskin.DoAction"/>'ı verir. Hamle kipiyle (<see cref="ActionArmed"/>) ve yetenek kipiyle aynı anda açık olmaz;
        /// onları kapatan her yer bunu da kapatır. Örnek tahtada (Room null) hiç açılmaz.
        /// </summary>
        public bool GearArmed { get; protected set; }
        string gearKind;
        /// <summary>Kurulu ekipman eylemi (<see cref="Baskin.GearActionKinds"/>); kip kapalıyken null.</summary>
        public string GearKind=>GearArmed?gearKind:null;
        /// <summary>Hamle ya da ekipman kipi açık (tahtaya tık eylemi verir); türü ve sürüklenen gövde.</summary>
        bool Kip=>ActionArmed||GearArmed;
        /// <summary>Yetenek kartının en çok satırı (aşama 9, §3.1: 1080'de kartın tepesi durum panelinin altında kalır).</summary>
        public const int MaxCardRows=10;
        /// <summary>Kart katlandıysa (aşama 9) hamle satırının seçenek dizini (−1: "Diğer"); katlanmamışsa null (satır = seçenek).</summary>
        int[] actionRowMap;
        string KipKind=>GearArmed?gearKind:ActionArmed?actionKind:null;
        int KipOther=>GearArmed?0:ActionArmed?actionOther:0;
        /// <summary>Seçimi kaldırır (uzman ve uzman olmayan birim).</summary>
        void Deselect(){Selected='\0';SelectedOther=0;}
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
        /// <summary>“YAYINDA” lambası (§7.3): emir telsizle gitti (Machines ya da Pass), gözcünün ilk rapor satırı gelmedi. Yalnız sunum;
        /// süre tutmaz (en az yanık kalma görünümde).</summary>
        bool onAir;
        protected Vector2Int? hover;protected float hoverAt;protected bool hoverVisible;
        protected Baskin.PreviewResult preview;
        /// <summary>Bölge olayının katmanı ve birim uyarıları (yetenek önizlemesinde hamleden sonrası, yürüyüş karesinin üstündeyken
        /// birim oraya yürürse olacak); olay yoksa null.</summary>
        protected Baskin.HazardView hazards;
        // ---------- Tesis baskını: oda zinciri (aşama 3). Örnek tahtada Session null: aşağıdaki her dal kapalıdır. ----------
        /// <summary>Tesis baskınının oturumu (oda açma, rapor, sonuç); örnek tahtada null.</summary>
        protected FacilityRaidSession Session { get; private set; }
        /// <summary>Önceki odaların telsiz satırları ve gözcü raporları (bu odanın Core satırlarından önce).</summary>
        readonly List<RaidLogModel> chainLog=new List<RaidLogModel>();
        /// <summary>Son odanın kapanış satırları (bu odanın Core satırlarından sonra; sonuç kartında).</summary>
        readonly List<RaidLogModel> endLog=new List<RaidLogModel>();
        bool chainClosed,passing,reportFiled,passOrdered;string chainError;
        /// <summary>
        /// Tesis aşama 12 (Katar; tasarım K37–K39): AYIR görünüm bayrağı. Katar'ın dönüş geçidinde herkes kapıdayken Q ya da Teknisyen'in kartı
        /// (harf, rakam ya da tık) çevirir; EMRİ İLET (Enter) açıkken <see cref="Baskin.Decouple"/>, kapalıyken GEÇ verir. Onay yok. Yalnız
        /// sunum: Core'a yalnız komutla gider; verilemez olunca kendiliğinden kapanır.
        /// </summary>
        bool detach;
        /// <summary>AYIR açık mı (EMRİ İLET AYIR verir); testler okur.</summary>
        public bool Detach=>detach;
        /// <summary>AYIR şimdi verilebilir mi (tesis odası, GEÇ ve Core <see cref="Baskin.CanDecouple"/>).</summary>
        bool CanDetach=>Session!=null&&State!=null&&Baskin.CanPass(State)&&Baskin.CanDecouple(State);
        /// <summary>AYIR bayrağını çevirir (verilemiyorsa false, bayrak değişmez).</summary>
        bool ToggleDetach(){if(!CanDetach)return false;detach=!detach;confirm=false;ClearHover(false);Publish();return true;}
        /// <summary>Açık odadan önce dosyalanmış raporların kırdığı sivil (güven: açık odanınki tahtadan eklenir).</summary>
        int collateralBefore;
        /// <summary>Hedef odasının görev satırları (oda kapanırken; sonuç kartı onları yazar).</summary>
        RaidTargetModel[] targetCache;
        // Önbellekteki hedef satırlarının kaynak (Türkçe) adları: sonuç kartı dil değişince yeniden çevirir (targetCache'in adları açılıştaki dilde donar)
        string[] targetCacheKeys;
        RoomReport lastReport;IReadOnlyList<int> lastSkipped=Array.Empty<int>();
        /// <summary>Oda geçişi sürüyor (rapor dosyalandı, pafta çevriliyor); testler bekler.</summary>
        public bool Passing=>passing;
        /// <summary>Baskın bitti (sonuç kartı): tesiste son oda dosyalandı, örnek tahtada evre won/lost.</summary>
        protected bool Over=>Session!=null?chainClosed:State!=null&&(State.Phase=="won"||State.Phase=="lost");
        /// <summary>Tesisin giriş ya da ara odasında, kaçıştan önce son tur: süre dolunca ekip geri çekilir.</summary>
        /// <para>Tesis aşama 11: savunmada süre dolunca geri çekilme yoktur (kanat tutulur); son turun onayı <see cref="DefenseLastTurn"/>.</para>
        bool TimeoutEnd=>State!=null&&State.Phase=="player"&&State.Room!=null&&!State.Escape&&!Baskin.IsDefense(State)&&State.Room.Type!=RoomType.Target&&State.Turn>=State.MaxTurns;
        /// <summary>Tesis aşama 11: savunmanın son turu (EMRİ İLET onay sorar: “SON TUR · SAVUNMA BİTER”). Saldırıda ve örnek tahtada false.</summary>
        bool DefenseLastTurn=>Session!=null&&FacilityDefenseRaidText.On(State)&&State.Phase=="player"&&State.Turn>=State.MaxTurns;
        /// <summary>Tesis aşama 11: savunmada oda başına güven bedeli ve kanadın düşüşünün bedeli (Core okuyucuları); saldırıda 0.</summary>
        int RoomTrust=>Session?.Sim!=null?Session.Sim.DefenseRoomTrust:0;
        int FallTrust=>Session?.Sim?.B!=null?System.Math.Max(0,Session.Sim.B.liberationCounterattackTrust):0;
        /// <summary>Paftadaki kare adı; tesis odasında pafta önekiyle ("2-C3").</summary>
        string Cell(int x,int y)=>RaidBoard3D.CellName(State?.Room?.Index??0,x,y);
        /// <summary>Tesis baskınında dengenin değerleri (donanımın etkisi); örnek tahtada null.</summary>
        protected Balance Rules=>Session?.Sim?.B;
        protected virtual void OnEnable(){DeskLoc.Instance.Changed+=Publish;if(board!=null){board.TileClicked+=Click;board.HoverChanged+=BoardHover;}if(hud!=null)hud.TeamHovered+=TeamHover;if(board!=null)board.EffectPlaying+=OnEffect;if(hud!=null&&hud.Panels!=null)hud.Panels.ResultStamped+=ResultStamped;if(board!=null&&hud!=null)board.LabelKeepOuts=hud.CollectLabelKeepOuts;}
        protected virtual void OnDisable(){DeskLoc.Instance.Changed-=Publish;if(board!=null){board.TileClicked-=Click;board.HoverChanged-=BoardHover;}if(hud!=null)hud.TeamHovered-=TeamHover;if(board!=null)board.EffectPlaying-=OnEffect;if(hud!=null&&hud.Panels!=null)hud.Panels.ResultStamped-=ResultStamped;if(board!=null)board.LabelKeepOuts=null;}
        // Sonuç damgası (S7): başarıda kısa zil, değilse boğuk “tak”
        static void ResultStamped(bool success)=>Sfx.Play(success?Cue.ResultBell:Cue.StampHeavy,.9f);
        protected abstract void Start();
        protected virtual void Update()
        {
            if(hover.HasValue&&!hoverVisible&&Time.unscaledTime-hoverAt>=theme.Number("raid.hoverCard.delayMs",150)/1000f){hoverVisible=true;Publish(false);}
        }
        protected void OpenState(Baskin.BState value){StopAllCoroutines();ResetBoard(value,true);}
        /// <summary>
        /// Tahtayı açar. first: baskının ilk tahtası (OpenState); false: tesis baskınında sıradaki oda (eşyordamlar durmaz, okunmamış
        /// telsiz sayacı sürer, bölgenin sesi yeniden kurulmaz).
        /// </summary>
        void ResetBoard(Baskin.BState value,bool first)
        {
            State=value;finishedAt=-1;Deselect();ActionArmed=GearArmed=false;Armed=confirm=retreat=fast=paused=resolving=executing=onAir=false;panel=null;hover=null;hoverVisible=false;preview=null;unread=true;if(first)seenLogs=0;visibleLogs=int.MaxValue;eventLine=null;
            reportFiled=passOrdered=detach=false;chainError=null;collateralBefore=Session!=null?Session.CollateralBefore:0;
            targetForms.Clear();civilianCount=0;
            foreach(var t in Baskin.TargetsLeft(State))targetForms.Add((t.X,t.Y,t.O.K,t.O.Max));
            // Tesis odasının donanımı görev satırında (gidişte, sağlamken); örnek tahtada Room null
            // Tesis aşama 11: savunmada donanımın etkisi yok (hazırlık kapalı): satırı yok; hedef odasında saha rölesi. Saldırıda izle kırık
            // gelen donanım KIRILDI damgasıyla (eskiden yalnız sağlamı eklenirdi)
            bool defense=FacilityDefenseRaidText.On(State);
            if(!defense&&State.Room?.DeviceAt is Baskin.Pt d&&!State.Escape&&Baskin.CellAt(State,d.X,d.Y).O is Baskin.Obj dev&&dev.K==State.Room.Device)targetForms.Add((d.X,d.Y,dev.K,dev.Max));
            if(!defense&&FacilityDefenseRaidText.MarkedDevice(State) is Baskin.Pt md)targetForms.Add((md.X,md.Y,State.Room.Device,1));
            if(defense&&FacilityDefenseRaidText.RelayAt(State) is Baskin.Pt relay)targetForms.Add((relay.X,relay.Y,Baskin.RelayKind,Baskin.RelayMax(State)));
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
            // Binanın sesi bölgeden (kitten değil): bölgenin ortam yatağı
            if(first)Sfx.RaidSite(State.Params?.Region,true);
            // Tesis odası açılınca gözcü raporu: telsiz kaydında satırlar, fişte özet (figürleri ve girdiyi geciktirmez)
            // Tesis aşama 11: savunmanın ve izlerin gözcü satırları (barikat turu ve ilk dalga, takipçiler, saha rölesi, önceki baskının izleri);
            // savunmanın barikat turunda fişte gözcü özeti yerine barikat satırı (Enrich her yayında gösterilen dilde yazar)
            // Tesis aşama 12: takipli geçişten sonra yeni vagonun gözcü satırı (önceki raporun takipçileri: tahtaya en son konan makineler)
            if(Session!=null){chainLog.AddRange(FacilityRaidText.Spotter(State));chainLog.AddRange(FacilityConditionText.SpotterFollow(State));chainLog.AddRange(FacilityDefenseRaidText.Spotter(State));eventLine=defense&&Baskin.BarricadeTurn(State)?null:FacilityRaidText.SpotterSlip(State);eventUntil=Time.unscaledTime+2f;Sfx.Play(Cue.RadioTune,.6f);}
            Publish();
        }
        /// <summary>Tesis baskınını açar: kayıttan devamda önceki odaların telsiz satırları, sonra sıradaki oda (devamda baştan).</summary>
        protected void OpenFacility(FacilityRaidSession session)
        {
            Session=session;chainLog.Clear();endLog.Clear();chainClosed=passing=false;targetCache=null;targetCacheKeys=null;lastReport=null;lastSkipped=Array.Empty<int>();
            if(session.Resumed)
            {
                chainLog.AddRange(FacilityRaidText.History(session.Params,session.Filed,session.Sim?.B));
                chainLog.Add(new RaidLogModel{Text=Loc.F("Kayıttan devam: PAFTA {0}/3 baştan açıldı.",session.NextStep.Value.Room),Kind="sys"});
            }
            OpenState(session.Open());
        }
        /// <summary>Oturumu kapatır (örnek tahta açılmadan önce: tesis kalıntısı görünmesin).</summary>
        protected void CloseFacility(){Session=null;chainLog.Clear();endLog.Clear();chainClosed=passing=false;targetCache=null;targetCacheKeys=null;lastReport=null;lastSkipped=Array.Empty<int>();}
        void CaptureRoster(){roster=Baskin.Intents(State);queue.Clear();foreach(var i in roster)queue[i.Unit]="pending";}
        public bool Busy=>resolving||(board!=null&&board.Busy);
        /// <summary>Makineler oynuyor (tur bitirildi, düşman hamleleri canlandırılıyor).</summary>
        public bool Executing=>executing;
        public void Select(char letter)
        {
            if(!CanInput)return;var u=Unit(letter);
            // Tesis aşama 12 (Katar): kapıdaki Teknisyen'in kartı (harf, rakam ya da tık) AYIR'ı çevirir (Q'nun eşi)
            if(u!=null&&u.Type=="tek"&&u.AtGate&&ToggleDetach())return;
            if(u==null||!Baskin.OnBoard(u))return;
            Selected=letter;SelectedOther=0;Armed=ActionArmed=GearArmed=false;confirm=retreat=false;ClearHover(false);
            Publish();
        }
        bool CanInput=>State!=null&&State.Phase=="player"&&!paused&&!Busy;
        // Harf yalnız uzmanı bulur (aşama 9: tutsak dronun "D"si ve köpeğin "Ö"sü karışmaz); uzman olmayan seçili birim kimliğiyle
        Baskin.Unit Unit(char letter)=>State?.Units.FirstOrDefault(u=>Baskin.IsSpecialist(u)&&Baskin.UnitTypes[u.Type].Short==letter.ToString());
        Baskin.Unit Unit()=>SelectedOther>0?State?.Units.FirstOrDefault(u=>u.Id==SelectedOther&&u.Side=='p'):Unit(Selected);
        /// <summary>Seçili birim (uzman, köpek ya da tutsak; yoksa null). Rehberin işaretçisi okur.</summary>
        public Baskin.Unit SelectedUnit=>Unit();
        public void ClearSelection(){if(Busy)return;Deselect();Armed=ActionArmed=GearArmed=false;confirm=false;ClearHover(false);Publish();}
        /// <summary>Tahtada seçilebilen uzman olmayan birim (aşama 9): ayaktaki köpek ve tutsak (yürür, komut verir). Aygıt seçilmez.</summary>
        static bool Selectable(Baskin.Unit u)=>u!=null&&u.Side=='p'&&(Baskin.IsDog(u)||Baskin.IsCaptive(u))&&Baskin.OnBoard(u);
        /// <summary>
        /// Uzman olmayan birimi kimliğiyle seçer (aşama 9: dördüncü kart, 4 tuşu, tahtaya tık). <paramref name="cycle"/>: aynı köpek
        /// seçiliyken tahtadaki sıradaki köpeğe geçer (sahibin kadro sırası). Kipleri kapatır. Örnek tahtada etkisiz.
        /// </summary>
        public void SelectId(int id,bool cycle=false)
        {
            if(!CanInput||State.Room==null)return;
            var u=State.Units.FirstOrDefault(v=>v.Id==id);if(!Selectable(u))return;
            if(cycle&&SelectedOther==id&&Baskin.IsDog(u))
            {
                var dogs=State.Units.Where(v=>Baskin.IsDog(v)&&Baskin.OnBoard(v)).OrderBy(v=>v.Owner).ThenBy(v=>v.Id).ToList();
                u=dogs[(dogs.FindIndex(v=>v.Id==id)+1)%dogs.Count];
            }
            Selected='\0';SelectedOther=u.Id;Armed=ActionArmed=GearArmed=false;confirm=retreat=false;ClearHover(false);Publish();
        }
        /// <summary>Kipte ya da seçimde tıklanan ekip birimini seçer: uzman harfle, köpek ve tutsak kimlikle; aygıt seçilmez.</summary>
        void SelectMate(Baskin.Unit mate){if(Baskin.IsSpecialist(mate))Select(Baskin.UnitTypes[mate.Type].Short[0]);else if(Selectable(mate))SelectId(mate.Id);}
        public void Hover(Vector2Int tile)
        {
            if(Busy||retreat||paused)return;
            if(tile.x<0||tile.y<0||tile.x>=8||tile.y>=8){ClearHover();return;}
            if(hover==tile)return;hover=tile;hoverAt=Time.unscaledTime;hoverVisible=false;Publish();
        }
        public void ClearHover()=>ClearHover(true);
        void ClearHover(bool publish){if(board!=null)board.SetExternalHover(null);hover=null;hoverVisible=false;preview=null;if(publish)Publish();}
        void BoardHover((int x,int y)? tile){if(tile.HasValue)Hover(new Vector2Int(tile.Value.x,tile.Value.y));else ClearHover();}
        void TeamHover(RaidTeamModel card){if(card==null)return;var u=card.UnitId>0?State?.Units.FirstOrDefault(v=>v.Id==card.UnitId):Unit(card.Letter);if(u!=null&&Baskin.OnBoard(u)&&board!=null)board.SetExternalHover((u.X,u.Y));}
        void Click(int x,int y)
        {
            if(!CanInput)return;var tile=new Vector2Int(x,y);
            // Tesis hamlesi kurulu (aşama 7): hedefe tık hamleyi verir; sürüklemede yanındaki başka bir gövdeye tık gövdeyi değiştirir;
            // ayaktaki ekip üyesine tık onu seçer (kip kapanır); hedef dışı tık yok sayılır
            // Aşama 9: ekipman kipi aynı yoldan (sürükleme gövdesi yalnız hamle kipinde)
            if(Kip)
            {
                var a=Unit();if(a==null){ActionArmed=GearArmed=false;Publish();return;}
                if(Baskin.ActionTargets(State,a,KipKind,KipOther).Any(p=>p.X==x&&p.Y==y)){UseAction(tile);return;}
                var body=ActionArmed&&actionKind==Baskin.ActDrag?FacilityMoveText.BodyAt(State,x,y):null;
                if(body!=null&&body.Id!=actionOther&&Baskin.DragBodies(State,a).Contains(body.Id)&&Baskin.ActionTargets(State,a,actionKind,body.Id).Count>0){actionOther=body.Id;ClearHover(false);Publish();return;}
                var mate=Baskin.UnitAt(State,x,y);
                if(mate!=null&&mate.Side=='p')SelectMate(mate);
                return;
            }
            if(Armed){Use(tile);return;}
            var u=Baskin.UnitAt(State,x,y);
            if(u!=null&&u.Side=='p')
            {
                // Aşama 9: köpek ve tutsak kimlikle seçilir; aygıt seçilmez (tık hover kartını gösterir, yürüyüşe düşmez)
                if(!Baskin.IsSpecialist(u)){if(Selectable(u))SelectId(u.Id);return;}
                // Tesis odası: kapı karesinde duran seçili uzmana yeniden tıklamak onu adım harcamadan kapıya sokar (örnek tahtada kapı yok)
                if(u==Unit()&&Baskin.IsGateTile(State,x,y)){EnterGate();return;}
                Select(Baskin.UnitTypes[u.Type].Short[0]);return;
            }
            Move(tile);
        }
        public void Move(Vector2Int tile)
        {
            var u=Unit();if(!CanInput||u==null)return;
            // Yürüyüşte yerdeki yükün alımı Carrier farkından okunur (DoMove FxList'i temizlemez: bayat efekt okunmaz)
            int carrierBefore=State.Carrier;
            // Yol DoMove'dan önce (varıştan sonra doluluk değişir): parça çizilen yoldan yürür (2026-11-24; duvarın içinden kaymaz)
            var path=Baskin.MovePath(State,u,tile.x,tile.y);
            if(!Baskin.DoMove(State,u.Id,tile.x,tile.y))return;
            confirm=retreat=ActionArmed=GearArmed=false;ClearHover(false);
            bool picked=State.Room!=null&&State.Carrier==u.Id&&carrierBefore!=u.Id;
            if(picked){eventLine=FacilityMoveText.PickupSlip(State,NameOf(u.Id),tile.x,tile.y);eventUntil=Time.unscaledTime+1.2f;}
            // Çıkışa varan (ya da tesis odasında kapıya giren) uzman tahtadan kalkar: seçim de kalkar
            if(u.Out||u.AtGate){Deselect();Armed=false;}
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(MoveRoutine(u.Id,tile,picked,path));else{Publish();CheckRoomEnd();}
        }
        IEnumerator MoveRoutine(int id,Vector2Int tile,bool picked=false,List<Baskin.Pt> path=null)
        {
            resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);
            if(path!=null&&path.Count>1)yield return board.AnimateWalk(id,path.Skip(1).Select(p=>(p.X,p.Y)).ToList());else yield return board.AnimateMove(id,tile.x,tile.y);
            // Tesis odası (aşama 7): yürüyüşle yerdeki yük alındıysa parça kalkar (Carrier farkından; DoMove FxList'i temizlemez)
            if(picked)yield return board.PickCargo(tile.x,tile.y);
            // Kaçışta çıkışa varan uzman tahtadan ayrılır; telsiz notu ayrılırken görünür, süresi dolunca kalkar
            var mover=State.Units.FirstOrDefault(u=>u.Id==id);
            if(mover?.Out==true){eventLine=Loc.F("{0} tahliye edildi",NameOf(mover.Id));eventUntil=Time.unscaledTime+1.2f;Publish(false);yield return board.AnimateLeave(id);}
            // Tesis odası: kapıya giren uzman tahtadan kalkar (örnek tahtada AtGate hep false)
            else if(mover?.AtGate==true)yield return GateLeave(mover);
            resolving=false;Publish();CheckRoomEnd();
            if(eventLine!=null&&!executing){yield return new WaitForSecondsRealtime(Mathf.Max(0,eventUntil-Time.unscaledTime));if(!executing)Publish(false);}
        }
        /// <summary>Kapıya giren uzmanın telsiz notu ve tahtadan kalkışı; herkes kapıdaysa GEÇ'in notu.</summary>
        IEnumerator GateLeave(Baskin.Unit u)
        {
            eventLine=Loc.F("{0} kapıda",NameOf(u.Id));eventUntil=Time.unscaledTime+1.2f;Publish(false);
            yield return board.AnimateLeave(u.Id,true);
            if(Baskin.CanPass(State)){eventLine=FacilityMoveText.PassSlip(State);eventUntil=Time.unscaledTime+2f;}
        }
        /// <summary>Seçili uzman kapı karesinde duruyorsa (itilen, çekilen ya da kayan) adım harcamadan kapıya girer (tesis odası;
        /// belge §3.3); tesisin girişinde tahliye olur. Örnek tahtada kapı karesi yok.</summary>
        public void EnterGate()
        {
            var u=Unit();if(!CanInput||u==null||State.Room==null)return;
            if(!Baskin.EnterGate(State,u.Id))return;
            confirm=retreat=false;Deselect();Armed=ActionArmed=GearArmed=false;ClearHover(false);
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(GateRoutine(u.Id));else{Publish();CheckRoomEnd();}
        }
        IEnumerator GateRoutine(int id)
        {
            resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);
            var u=State.Units.FirstOrDefault(x=>x.Id==id);
            if(u?.AtGate==true)yield return GateLeave(u);
            else if(u?.Out==true){eventLine=Loc.F("{0} tahliye edildi",NameOf(u.Id));eventUntil=Time.unscaledTime+1.2f;Publish(false);yield return board.AnimateLeave(id);}
            resolving=false;Publish();CheckRoomEnd();
        }
        /// <summary>GEÇ: ayaktaki herkes kapıdayken oda biter (geri alınmaz), rapor dosyalanır ve sıradaki pafta açılır. Yalnız tesis odası.</summary>
        public void Pass()
        {
            if(!CanInput||Session==null||!Baskin.CanPass(State))return;
            if(!Baskin.Pass(State))return;
            passOrdered=true;detach=false;Deselect();Armed=ActionArmed=GearArmed=confirm=retreat=false;ClearHover(false);
            // GEÇ de emirdir: lamba yanar, gözcünün geçiş satırıyla (oda dosyalanınca) söner
            onAir=true;Sfx.Play(Cue.RadioSend);Publish();CheckRoomEnd();
        }
        /// <summary>Tesis aşama 12 · AYIR (Katar'ın dönüş geçidi, GEÇ'in seçeneği): vagon ayrılır, takip olmaz, Teknisyen'in yeteneği harcanır
        /// (<see cref="Baskin.Decouple"/>); GEÇ gibi geri alınmaz, rapor dosyalanır ve sıradaki pafta açılır.</summary>
        public void Decouple()
        {
            if(!CanInput||Session==null||!Baskin.CanDecouple(State))return;
            if(!Baskin.Decouple(State))return;
            passOrdered=true;detach=false;Deselect();Armed=ActionArmed=GearArmed=confirm=retreat=false;ClearHover(false);
            onAir=true;Sfx.Play(Cue.RadioSend);Publish();CheckRoomEnd();
        }
        public void Arm()
        {
            var u=Unit();if(!CanInput)return;
            // Tesis aşama 12 (Katar): seçili birim yokken (herkes kapıda) Q AYIR'ı çevirir
            if(u==null){ToggleDetach();return;}
            // Q hamle ve ekipman kipini kapatır, sonra yeteneği bugünkü gibi çevirir (yetenek kullanılamıyorsa yalnız kapatır). Aşama 9:
            // köpek ve tutsakta yetenek yok (Q yalnız kipi kapatır)
            if(Kip){ActionArmed=GearArmed=false;if(!Baskin.CanAct(State,u)||!Baskin.IsSpecialist(u)){Publish();return;}}
            if(!Baskin.IsSpecialist(u))return;
            if(!Baskin.CanAct(State,u))return;
            Armed=!Armed;confirm=false;Publish();
        }
        public void Use(Vector2Int tile)
        {
            var u=Unit();if(!CanInput||u==null||!Armed)return;
            if(!Baskin.DoAct(State,u.Id,tile.x,tile.y))return;
            Armed=false;confirm=retreat=false;ClearHover(false);
            if(!Baskin.OnBoard(u))Deselect();
            foreach(var fx in State.FxList.Where(f=>f.K=="die"||f.K=="fall"))if(queue.ContainsKey(fx.Unit))queue[fx.Unit]="cancel";
            // Tesis aşama 12 (Katar): kenardan düşen birimin telsiz fişi
            if(FallSlip(State.FxList) is string fall){eventLine=fall;eventUntil=Time.unscaledTime+1.6f;Sfx.Play(Cue.RadioReport,.6f);}
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(ActRoutine());else{Publish();CheckRoomEnd();}
        }
        // ---------- Tesis hamleleri (aşama 7): kip, kart satırı, tahtaya tık. Örnek tahtada (Room null) hiçbiri açılmaz. ----------
        /// <summary>E: seçili uzmanın kullanılabilir hamlelerinde sıradakini kurar (<see cref="Baskin.ActionKinds"/> sırası; sürüklemede
        /// her gövde ayrı); sonuncudan sonra kip kapanır. Yetenek kipini kapatır. Örnek tahtada etkisiz.</summary>
        public void ArmAction()
        {
            var u=Unit();if(!CanInput||u==null||State.Room==null)return;
            var usable=FacilityMoveText.Options(State,u).Where(o=>o.Usable).ToList();
            int i=ActionArmed?usable.FindIndex(o=>o.Kind==actionKind&&o.Other==actionOther):-1;
            // Tek kip açık (aşama 9 §2.3): döngü kapanırken ekipman kipi de kapanır (kullanılabilir hamle yokken E onu da kapatır)
            if(usable.Count==0||i+1>=usable.Count)ActionArmed=GearArmed=false;
            else{ActionArmed=true;Armed=GearArmed=false;actionKind=usable[i+1].Kind;actionOther=usable[i+1].Other;}
            confirm=false;ClearHover(false);Publish();
        }
        /// <summary>Kart satırına tık (<see cref="RaidViewModel.ActionRows"/> sırası): o hamleyi kurar; kurulu satıra yeniden tık kipi
        /// kapatır. Kullanılamayan satır bir şey yapmaz.</summary>
        public void ChooseAction(int index)
        {
            var u=Unit();if(!CanInput||u==null||State.Room==null)return;
            // Katlanmış kartta satır dizini seçeneğe çevrilir ("Diğer" satırı bir şey yapmaz; aşama 9)
            if(actionRowMap!=null){if(index<0||index>=actionRowMap.Length)return;index=actionRowMap[index];}
            var options=FacilityMoveText.Options(State,u);if(index<0||index>=options.Count||!options[index].Usable)return;
            var o=options[index];
            if(ActionArmed&&actionKind==o.Kind&&actionOther==o.Other)ActionArmed=false;
            else{ActionArmed=true;Armed=GearArmed=false;actionKind=o.Kind;actionOther=o.Other;}
            confirm=false;ClearHover(false);Publish();
        }
        /// <summary>
        /// R (aşama 9): seçili uzmanın kullanılabilir ekipman eylemlerinde sıradakini kurar (<see cref="FacilityGearText.GearOptions"/>:
        /// örme setinde tel → kalas, öbürlerinde tek eylem); sonuncudan sonra kip kapanır. Hamle ve yetenek kipini kapatır; kullanılabilir
        /// eylem yoksa (hak bitti) yalnız açık kipi kapatır. Örnek tahtada, köpek ve tutsakta ve ekipmansız uzmanda etkisiz.
        /// </summary>
        public void ArmGear()
        {
            var u=Unit();if(!CanInput||u==null||State.Room==null||!Baskin.IsSpecialist(u))return;
            var options=FacilityGearText.GearOptions(State,u);if(options.Count==0)return;
            var usable=options.Where(o=>o.Usable).ToList();
            int i=GearArmed?usable.FindIndex(o=>o.Kind==gearKind):-1;
            // Tek kip açık: ekipmanlı uzmanda döngü kapanırken hamle kipi de kapanır (kullanılabilir eylem yokken R onu da kapatır)
            if(usable.Count==0||i+1>=usable.Count)GearArmed=ActionArmed=false;
            else{GearArmed=true;Armed=ActionArmed=false;gearKind=usable[i+1].Kind;}
            confirm=false;ClearHover(false);Publish();
        }
        /// <summary>Kartın ekipman satırına tık (tek satır): R ile aynı döngü. Kullanılamaz satır bir şey yapmaz.</summary>
        public void ChooseGear(int index){if(index!=0)return;ArmGear();}
        /// <summary>
        /// Kurulu hamleyi hedef karede verir (<see cref="Baskin.DoAction"/>; geri alma görüntüsü Core'da). Yürüyüş yolu hamleden önce
        /// alınır (sürükleme, menfezin yakın ucu). Başarıda kip kapanır, telsiz fişi yazılır; tahtadan kalkan (kapı, çıkış) uzmanın
        /// seçimi kalkar; düşen makinenin niyeti iptal olur.
        /// </summary>
        public void UseAction(Vector2Int tile)
        {
            var u=Unit();if(!CanInput||u==null||!Kip)return;
            string kind=KipKind;int other=KipOther;
            List<Baskin.Pt> path=null;
            if(kind==Baskin.ActDrag)path=Baskin.WalkPath(State,u,FacilityMoveText.Range(u)-Baskin.DragCostFor(State,u),tile.x,tile.y);
            else if(kind==Baskin.ActVent)
            {
                var vent=Baskin.PreviewAction(State,u.Id,kind,tile.x,tile.y,other)?.Fx.FirstOrDefault(f=>f.K=="vent");
                if(vent!=null&&(vent.X!=u.X||vent.Y!=u.Y))path=Baskin.WalkPath(State,u,FacilityMoveText.Range(u),vent.X,vent.Y);
            }
            if(!Baskin.DoAction(State,u.Id,kind,tile.x,tile.y,other))return;
            ActionArmed=GearArmed=false;confirm=retreat=false;ClearHover(false);
            if(!Baskin.OnBoard(u))Deselect();
            foreach(var fx in State.FxList.Where(f=>f.K=="die"||f.K=="fall"))if(queue.ContainsKey(fx.Unit))queue[fx.Unit]="cancel";
            eventLine=FacilityMoveText.Slip(State,kind,u.Id,tile.x,tile.y,other,NameOf);eventUntil=Time.unscaledTime+1.2f;
            // Tesis aşama 12 (Katar): hamle birimi kenardan düşürdüyse fiş düşmeyi yazar
            if(FallSlip(State.FxList) is string fall){eventLine=fall;eventUntil=Time.unscaledTime+1.6f;Sfx.Play(Cue.RadioReport,.6f);}
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(ActionRoutine(u.Id,kind,path,other));else{Publish();CheckRoomEnd();}
        }
        /// <summary>Tesis aşama 12: efektlerdeki son kenardan düşmenin telsiz fişi (ekip üyesi "vagondan", öbürleri "kenardan"); düşme yoksa null.</summary>
        string FallSlip(IEnumerable<Baskin.Fx> fx)
        {
            var f=fx?.LastOrDefault(x=>x.K=="fall");if(f==null)return null;
            var u=State.Units.FirstOrDefault(v=>v.Id==f.Unit);
            return FacilityConditionText.FallSlip(u!=null&&Baskin.IsSpecialist(u),NameOf(f.Unit),Cell(f.X,f.Y));
        }
        /// <summary>Birim kimliğinden telsiz adı (ekipte kişinin adı ve rolü).</summary>
        /// Aşama 9: köpek "KÖPEK · sahibi", tutsak "TUTSAK · makine", aygıt ve makine türün adı.
        string NameOf(int id)
        {
            var v=State?.Units.FirstOrDefault(x=>x.Id==id);if(v==null)return string.Empty;
            if(Baskin.IsSpecialist(v))return PersonalName(Baskin.UnitTypes[v.Type].Short[0]);
            if(Baskin.IsDog(v))return FacilityGearText.DogName(State,v);
            if(Baskin.IsCaptive(v))return Loc.F("TUTSAK · {0}",DeskText.Upper(Baskin.UnitTypes[v.Type].Name));
            return DeskText.Upper(Baskin.UnitTypes[v.Type].Name);
        }
        /// <summary>
        /// Hamlenin canlandırması (aşama 7; belge §6.2). Sürüklemede sürükleyen yolu kare kare yürür, gövde bir adım arkasından gelir;
        /// menfezde Analist önce yakın uca yürür (yol hamleden önce alındı). Ardından Core'un efektleri (koşu, itme, fırlatma, tutuş,
        /// açılış, menfez, yük alımı…); kapıya ya da çıkışa giren uzman ve gövde tahtadan kalkar. Yalnız sunum; sonra Sync.
        /// </summary>
        IEnumerator ActionRoutine(int id,string kind,List<Baskin.Pt> path,int other)
        {
            resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);
            var fx=new List<Baskin.Fx>(State.FxList);
            // Yol başlangıç karesini de içerir (WalkPath); yürüyüş ondan sonraki karelerden
            var steps=path!=null&&path.Count>1?path.Skip(1).Select(p=>(p.X,p.Y)).ToList():null;
            if(kind==Baskin.ActDrag&&steps!=null)
            {
                var drag=fx.FirstOrDefault(f=>f.K=="drag");
                yield return board.AnimateWalk(id,steps,drag?.Unit??0,drag!=null?((int,int)?)(drag.X+drag.Dx,drag.Y+drag.Dy):null);
            }
            else if(kind==Baskin.ActVent&&steps!=null)yield return board.AnimateWalk(id,steps);
            yield return board.Animate(State,fx,false);
            var actor=State.Units.FirstOrDefault(v=>v.Id==id);
            // Tesisin girişinde Core çıkışı "evac" efektiyle bildirir (Animate tahtadan kaldırdı): ikinci kez kaldırılmaz
            bool evacuated=fx.Any(f=>f.K=="evac"&&f.Unit==id);
            if(actor?.Out==true){eventLine=Loc.F("{0} tahliye edildi",NameOf(actor.Id));eventUntil=Time.unscaledTime+1.2f;Publish(false);if(!evacuated)yield return board.AnimateLeave(id);}
            else if(actor?.AtGate==true)yield return GateLeave(actor);
            // Sürüklenen gövde de kapıda (ekiple geçer) ya da tesisten çıkarıldı
            var body=kind==Baskin.ActDrag?State.Units.FirstOrDefault(v=>v.Id==other):null;
            if(body!=null&&(body.AtGate||body.Out))yield return board.AnimateLeave(other,body.AtGate);
            resolving=false;Publish();CheckRoomEnd();
            if(eventLine!=null&&!executing){yield return new WaitForSecondsRealtime(Mathf.Max(0,eventUntil-Time.unscaledTime));if(!executing)Publish(false);}
        }
        IEnumerator ActRoutine(){resolving=true;board.InputEnabled=false;board.ClearPreview();Publish(false);yield return board.Animate(State,new List<Baskin.Fx>(State.FxList),false);resolving=false;Publish();CheckRoomEnd();}
        public void Undo(){if(!CanInput)return;if(Baskin.Undo(State)){Armed=ActionArmed=GearArmed=false;
                // Geri alma birim listesini yeniler: seçili uzman olmayan birim (yeni kurulan tutsak) artık yoksa seçim kalkar
                if(SelectedOther>0&&!Selectable(State.Units.FirstOrDefault(v=>v.Id==SelectedOther)))SelectedOther=0;confirm=false;CaptureRoster();ClearHover(false);Publish();}}
        public void EndTurn()
        {
            if(State!=null&&Over){Continue();return;}
            // Tesis odası: reddedilen oda raporu (olmamalı) Enter ile yeniden denenir
            if(Session!=null&&chainError!=null&&!Busy){RetryRoom();return;}
            if(!CanInput)return;
            // Tesis odasında herkes kapıdayken EMRİ İLET GEÇ olur (onay sorulmaz; EditMode'un makinesiz dalından önce)
            // Tesis aşama 11: savunmada GEÇ “İÇERİ ÇEKİL”dir ve ilk basış onay sorar (oda terk edilir: güven, izler, yerde yatanlar)
            // Tesis aşama 12 (Katar): AYIR açıksa EMRİ İLET vagonu ayırır (onaysız, K38)
            if(Session!=null&&Baskin.CanPass(State)){if(FacilityDefenseRaidText.On(State)&&!confirm){confirm=true;Publish(false);return;}if(detach&&Baskin.CanDecouple(State)){Decouple();return;}Pass();return;}
            // Tesisin giriş ve ara odasında son tur da onaylanır (süre dolunca ekip geri çekilir; örnek tahtada TimeoutEnd hep false)
            if(!confirm&&(FatalEnd||TimeoutEnd||DefenseLastTurn||confirmUnusedActions&&State.Units.Any(u=>Baskin.IsSpecialist(u)&&Baskin.OnBoard(u)&&(!u.Moved||!u.Acted)))){confirm=true;Publish(false);return;}
            if(!Application.isPlaying||board==null){confirm=false;Deselect();Armed=ActionArmed=GearArmed=false;State.Phase="enemy";ClearHover(false);Publish();return;}
            StartCoroutine(Machines());
        }
        IEnumerator Machines()
        {
            confirm=retreat=false;Deselect();Armed=ActionArmed=GearArmed=false;ClearHover(false);Publish();
            // Görünür telsiz satırları: evrenin tur başlığı (+1); tesiste bu tur ikinci uzmanı gelmeyen tutuş başlıktan önce "boşa gitti"
            // yazar (+1; örnek tahtada HoldId hep 0)
            machineBefore=State.Clone();executing=resolving=true;currentOrder=0;visibleLogs=State.Log.Count+1+(machineBefore.HoldId!=0?1:0);
            // Emir telsizle sahaya gider; makinelerin hamlesini gözcü tek tek bildirir (OnEffect)
            eventLine=Loc.T("emir iletildi · gözcü bildiriyor");board.InputEnabled=false;unread=true;onAir=true;Sfx.Play(Cue.RadioSend);
            if(!Baskin.EndTurn(State)){executing=resolving=onAir=false;yield break;}Publish(false);
            var fx=new List<Baskin.Fx>(State.FxList);
            // Tesis odası (aşama 7): pusu makineler yeni yerlerine geçtikten sonra ateşlenir (Core sırası). Evre ilk pusu vuruşunda bölünür:
            // önce saldırılar ve düşüşler, sonra makine yürüyüşü (pusuyla düşecekler de), sonra pusu ve ardındakiler. Pusu yoksa (ve eski
            // tahtada) akış bugünkü
            int cut=fx.FindIndex(f=>f.K=="ambush-hit");
            yield return board.Animate(State,cut<0?fx:fx.GetRange(0,cut),true,0,fx.Count);
            foreach(var key in queue.Keys.ToArray())if(queue[key]!="cancel")queue[key]="done";
            var ambushed=cut<0?null:new HashSet<int>(fx.Skip(cut).Where(f=>f.K=="ambush-hit").Select(f=>f.Unit));
            // Core robotları yeni plana doğrudan taşır; tahtada yeni yerlerine kare kare, tek tek yürürler
            if(board.MachinesWillMove(State,ambushed)){eventLine=Loc.T("gözcü · makineler yer değiştiriyor");onAir=false;Publish(false);yield return board.AnimateMachineMoves(State,ambushed);}
            if(cut>=0)yield return board.Animate(State,fx.GetRange(cut,fx.Count-cut),true,cut,fx.Count);
            // Son nefes (Core: Unit.Breath; oda bu evrede bitti, bu evrede düşen gövde ekiple gider): parça ekiple tahtadan kalkar
            // (geçişte kapıya, tesisin girişinde tahliye; Sync onu artık çizmez). Core'un telsiz satırı kayıtta. Örnek tahtada Breath hep false
            foreach(var gone in State.Units.Where(FacilityMoveText.GoesWithTeam).ToList())yield return board.AnimateLeave(gone.Id,State.Phase=="pass");
            // Rapor satırı gelmeden biten evre (efektsiz tur): lamba evre sonunda söner
            executing=resolving=onAir=false;machineBefore=null;visibleLogs=int.MaxValue;eventUntil=Time.unscaledTime+.3f;
            Publish();
            yield return new WaitForSecondsRealtime(.3f);eventLine=null;if(State.Phase=="player")CaptureRoster();Publish(false);
            // Tesis odası: makine evresi odayı bitirdiyse (kendiliğinden geçiş, süre sonu, ekip düştü) rapor dosyalanır
            CheckRoomEnd();
        }
        void OnEffect(Baskin.Fx fx,int index,int count)
        {
            if(!executing)return;
            // Gözcünün ilk rapor satırı (fişteki satırı değiştiren ilk efekt) “YAYINDA” lambasını söndürür
            var lineBefore=eventLine;
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
                string cell=Cell(fx.X,fx.Y);
                string to=victim?.Side=='p'?Loc.F("{0} · {1}",NameOf(victim.Id),cell):cell;
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
            // Aşama 9: düşen köpek ve çöken aygıt sunum kopyasında da düşer (köpeğin kartı TAHTADAN ÇEKİLDİ olur)
            if(fx.K=="dog-down"||fx.K=="scrap"||fx.K=="device-break"){var u=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);if(u!=null){u.Dead=true;u.Hp=0;}}
            if(fx.K=="evac"){var u=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);if(u!=null){u.Out=true;eventLine=Loc.F("{0} tahliye edildi",NameOf(u.Id));}}
            // Tesis aşama 12 (Katar): kenardan düşen sunum kopyasında da düşer (makine yok olur, ekip üyesi kenardan düşmüş: kart VAGONDAN DÜŞTÜ)
            if(fx.K=="fall")
            {
                var u=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);if(u!=null){u.Dead=true;u.Hp=0;if(Baskin.IsSpecialist(u))u.Overboard=true;}
                if(queue.ContainsKey(fx.Unit))queue[fx.Unit]="cancel";
                eventLine=FallSlip(new[]{fx});Sfx.Play(Cue.RadioReport,.6f);
            }
            // Yalpa'nın kayması: sunum kopyası Fx'in varış karesine geçer (birim ve gövde, yerdeki yük, hurda); çarpan yerinde kalır
            if(fx.K=="slide"&&fx.N==1)
            {
                int nx=fx.X+fx.Dx,ny=fx.Y+fx.Dy;
                if(fx.Obj=="cargo"){machineBefore.CargoX=nx;machineBefore.CargoY=ny;}
                else if(fx.Obj=="hurda"){var from=machineBefore.Cells[fx.Y,fx.X];if(from.O!=null&&nx>=0&&nx<Baskin.N){machineBefore.Cells[ny,nx].O=from.O;from.O=null;}}
                else{var u=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);if(u!=null){u.X=nx;u.Y=ny;}}
            }
            if(fx.K=="break"){if(machineBefore.Cells[fx.Y,fx.X].O?.K=="civ")machineBefore.Collateral++;machineBefore.Cells[fx.Y,fx.X].O=null;}
            if(fx.K=="heal"&&machineBefore.Cells[fx.Y,fx.X].O is Baskin.Obj healed)healed.Hp=Math.Min(healed.Max,healed.Hp+1);
            if(fx.K=="dmg")eventLine=Loc.F("{0} · −{1}",eventLine,fx.N);
            if(fx.K=="smoked")eventLine=Loc.F("№{0} · sise kesildi",currentOrder);
            if(fx.K=="heal")eventLine=Loc.F("№{0} · onarım +1",currentOrder);
            // Tesis aşama 11: savunmada makine uyanmaz, gelir (dalga ya da terk edilen odadan); dalganın kendi satırı "wave"te
            bool defenseFx=FacilityDefenseRaidText.On(State);
            if(fx.K=="spawn")eventLine=defenseFx?FacilityDefenseRaidText.ArrivalSlip(Cell(fx.X,fx.Y)):Loc.F("Yeni bir robot uyandı: {0}.",Cell(fx.X,fx.Y));
            if(fx.K=="wave"){eventLine=FacilityDefenseRaidText.WaveSlip(fx.N,fx.Unit);Sfx.Play(Cue.RadioReport,.7f);}
            if(fx.K=="clouded")eventLine=Loc.F("№{0} · buluta kesildi",currentOrder);
            // Bölge olayının adımı: telsiz olayın adını ve karesini bildirir; kabin yolcusu sunum kopyasında da tahtadan iner, çıkar
            if(fx.K=="ev")
            {
                // Bütün tahtaya işleyen adımın (bulutun dağılması, faz değişimi) karesi yok
                string evName=State.Ev!=null?Loc.T(Baskin.RegionEvents[State.Ev.Id].Name):null;
                // Aşama 12: Yalpa'nın yatışı karesiz ("Yalpa · batıya")
                if(fx.Obj=="yalpa")eventLine=FacilityConditionText.TiltSlip(fx.Dx);
                else if(evName!=null)eventLine=fx.Obj=="bulut_dagil"||fx.Obj=="faz"?evName:Loc.F("{0} · {1}",evName,Cell(fx.X,fx.Y));
                var rider=fx.Unit!=0?machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit):null;
                if(rider!=null&&fx.Obj=="kabin_kalk")rider.Cab=true;
                if(rider!=null&&fx.Obj=="kabin_in"){rider.Cab=false;rider.X=fx.X;rider.Y=fx.Y;}
                Sfx.Play(Cue.RadioReport,.5f);
            }
            // Tesis odası (aşama 7): pusu vuruşu makinenin yeni karesinde (sunum kopyası da oraya geçer); taşıyıcı düşünce yük yere düşer.
            // İkisi de Core'un kendi telsiz satırını yazar (örnek tahtada bu efektler gelmez)
            if(fx.K=="ambush-hit")
            {
                var m=machineBefore.Units.FirstOrDefault(x=>x.Id==fx.Unit);if(m!=null){m.X=fx.X;m.Y=fx.Y;}
                eventLine=FacilityMoveText.EffectSlip(fx,machineBefore);Sfx.Play(Cue.RadioReport,.6f);
            }
            if(fx.K=="cargo-drop"){machineBefore.Carrier=0;machineBefore.CargoX=fx.X;machineBefore.CargoY=fx.Y;eventLine=FacilityMoveText.EffectSlip(fx,machineBefore);}
            if(fx.K=="die"||fx.K=="break"||fx.K=="heal"||fx.K=="spawn"&&!defenseFx||fx.K=="wave"||fx.K=="evac"||fx.K=="ev"||fx.K=="ambush-hit"||fx.K=="cargo-drop"||fx.K=="fall")visibleLogs=Math.Min(State.Log.Count,visibleLogs+1);
            if(eventLine!=lineBefore)onAir=false;
            Publish(false);
        }
        public void Cancel()
        {
            if(Busy){if(panel!=null){panel=null;Publish(false);}return;}
            if(confirm||retreat){confirm=retreat=false;Publish(false);return;}
            // Tesis hamlesi kipi önce kapanır (aşama 7; örnek tahtada hiç açılmaz)
            if(Kip){ActionArmed=GearArmed=false;Publish();return;}
            if(Armed){Armed=false;Publish();return;}
            if((Selected!='\0'||SelectedOther>0)){ClearSelection();return;}
            if(panel!=null){panel=null;Publish(false);return;}
            paused=!paused;Publish(false);
        }
        public void Speed(bool on){if(!executing&&State?.Phase!="enemy")return;fast=on;if(board!=null){board.FastMachines=on;board.MachineDelay=on ? .08f : .25f;}Publish(false);}
        public void Retreat()
        {if(!CanInput||State.Escape)return;panel=null;if(!retreat){retreat=true;if(Kip){ActionArmed=GearArmed=false;Publish();return;}Publish(false);return;}Baskin.Retreat(State);retreat=false;Publish();CheckRoomEnd();}
        public void Panel(string id){if(retreat||State==null||Over)return;panel=panel==id?null:id;if(id=="log"){unread=false;seenLogs=LogCount;}Publish(false);}
        /// <summary>Telsiz kaydının satır sayısı (tesiste önceki odaların ve kapanışın satırları dahil).</summary>
        int LogCount=>chainLog.Count+State.Log.Count+endLog.Count;
        // Sonuç kartı gelene kadar (S7: kararma 0,4 s + kayma 0,25 s) girdi kilitli
        public void Continue(){if(Busy||State==null||!Over||finishedAt<0||Time.unscaledTime-finishedAt<ResultLockSeconds)return;Finish();}
        // ---------- Tesis odasının sonu: rapor, geçiş, sonuç (Session null iken hiçbiri çalışmaz) ----------
        /// <summary>
        /// Oda bittiyse (evre pass, won ya da lost) raporu dosyalar (raid_room); zincir sürüyorsa sıradaki paftayı açar (oyunda çevirerek,
        /// EditMode'da eşzamanlı), son odaysa sonuç kartı. Odanın bittiği her yerden açıkça çağrılır; birden çok çağrı zararsız.
        /// </summary>
        void CheckRoomEnd()
        {
            if(Session==null||State?.Room==null||chainClosed||passing||reportFiled)return;
            if(State.Phase!="pass"&&State.Phase!="won"&&State.Phase!="lost")return;
            if(executing||resolving||(board!=null&&board.Busy))return;
            // Hedef odasının görev satırları sonuç kartı için (H'den ayrılınca tahtada yoklar)
            if(State.Room.Type==RoomType.Target){targetCache=Current?.Targets?.Where(t=>t.Max>0).ToArray();bool l=Baskin.IsLoot(State);targetCacheKeys=targetForms.Where(t=>t.max>0).Select(t=>Baskin.IsDevice(t.kind)||t.kind==Baskin.RelayKind?Baskin.ObjNames[t.kind]:l?"İkmal kasası":t.kind=="node"?"Düğüm":"Çekirdek").ToArray();}
            RoomReport report;
            try{report=Session.Preview(State);}catch(InvalidOperationException e){FailRoom(e.Message);return;}
            var before=Session.Filed.Count>0?Session.Filed[Session.Filed.Count-1]:null;
            int taped=TapeSheets().Length;
            var filed=Session.File(report);
            if(!filed.Ok){FailRoom(filed.Msg);return;}
            // Gözcünün geçiş ya da kapanış satırı aşağıda yazılır: GEÇ'in yaktığı lamba söner
            reportFiled=true;chainError=null;lastReport=report;onAir=false;
            // Teyp: oda kayda geçti (Core odayı bilinenlere ekledi), etiket yazılırken kalem sesi
            if(TapeSheets().Length>taped)Sfx.Play(Cue.Pencil,.6f);
            lastSkipped=FacilityRaidSession.Skipped(report,Session.NextStep);
            if(Session.Closed)
            {
                chainClosed=true;
                endLog.AddRange(FacilityRaidText.Closed(State.Params,report,lastSkipped,before,State.Room.BigCrate,Rules));
                endLog.AddRange(FacilityDefenseRaidText.Closed(State.Params,report,RoomTrust));
                if(report.End=="pass"){eventLine=FacilityRaidText.WalkedSlip(lastSkipped,true);eventUntil=Time.unscaledTime+1.6f;}
                Publish();return;
            }
            lastBefore=before;
            if(animateActions&&Application.isPlaying&&board!=null)StartCoroutine(PassRoutine(!passOrdered));
            else OpenNextRoom();
        }
        RoomReport lastBefore;
        /// <summary>
        /// Makaralı teybin kaydı (§7.2–7.3): bu baskında raporu dosyalanan odalardan planı Core'da bilinenler (<see cref="Simulation.FacilityKnown"/>;
        /// oda dosyalanınca <c>AdvanceRaidRoom</c> öğrenir), ilk dosyalanış sırasıyla, pafta numarası. Raporlar simülasyondan okunduğu için
        /// kayıttan devamda da aynı; evrakla ya da dinlemeyle önceden bilinen ama girilmemiş oda teypte yoktur. Örnek tahtada boş.
        /// </summary>
        int[] TapeSheets()
        {
            var rooms=Session?.Params?.Rooms;if(rooms==null)return Array.Empty<int>();
            var o=new List<int>();
            foreach(var r in Session.Filed)if(r!=null&&r.Room>=1&&r.Room<=rooms.Count&&!o.Contains(r.Room)&&Session.Sim.FacilityKnown(rooms[r.Room-1]))o.Add(r.Room);
            return o.ToArray();
        }
        /// <summary>Pafta kayar: biten oda geçidin tersine çıkar, sıradaki oda (raporlardan) kurulur ve geçidin yönünden gelir. Girdi boyunca kilitli.</summary>
        IEnumerator PassRoutine(bool auto)
        {
            passing=true;resolving=true;board.InputEnabled=false;
            eventLine=lastSkipped.Count>0?FacilityRaidText.WalkedSlip(lastSkipped,false):auto?Loc.T("gözcü · kapıdakiler geçti"):Loc.T("ekip geçitten geçti");
            // Tesis aşama 12 (Katar): AYIR'da "vagon ayrıldı · takip yok" (kol sesi, pafta çevrilmeden önce), takipte "1 makine takip ediyor"
            if(FacilityConditionText.PassedSlip(lastReport) is string chained){eventLine=chained;if(lastReport.Decouple)Sfx.Play(Cue.EvArmClank,.9f);}
            eventUntil=Time.unscaledTime+1.2f;Publish(false);
            Sfx.Play(Cue.PaperSlide,.8f);
            // Biten pafta kullanılan geçidin tersine kayıp çıkar, yeni pafta geçidin yönünden gelir (gidişte ileri geçit doğuda,
            // kaçışta ve dönüşte batı girişi; sürüm 10'da yeni odanın girişi geçide simetrik)
            var dir=RaidBoard3D.GateDirection(Baskin.GateTiles(State));
            yield return board.PageOut(dir);
            bool opened=OpenNextRoom();
            if(opened)yield return board.PageIn(dir);
            passing=false;Publish();
        }
        /// <summary>Sıradaki oda: biten odanın satırları telsiz kaydına geçer, yeni tahta kurulur (ResetBoard).</summary>
        bool OpenNextRoom()
        {
            Baskin.BState next;
            try{next=Session.Open();}
            // Açılamadı (olmamalı): biten oda yeniden çizilir (Build pafta pozunu ve tahtanın kilidini sıfırlar), Enter yeniden dener
            catch(InvalidOperationException e){passing=resolving=false;if(board!=null)board.Build(State);FailRoom(e.Message);return false;}
            chainLog.AddRange(FacilityRaidText.Log(State.Log));
            chainLog.AddRange(FacilityRaidText.Closed(State.Params,lastReport,lastSkipped,lastBefore,State.Room?.BigCrate,Rules));
            chainLog.AddRange(FacilityDefenseRaidText.Closed(State.Params,lastReport,RoomTrust));
            // Geçişte eşyordam sürer: Machines'in kuyruğu ya da yürüyüşün notu yeni odayı bozmasın diye ResetBoard eşyordam durdurmaz
            ResetBoard(next,false);
            return true;
        }
        /// <summary>Oda raporu reddedildi ya da oda açılamadı (olmamalı): baskın durur, Enter yeniden dener.</summary>
        void FailRoom(string why)
        {
            chainError=why;onAir=false;Debug.LogError("Tesis baskını: "+why);
            eventLine=Loc.F("Oda raporu reddedildi: {0} · Enter ile yeniden dene",why);eventUntil=float.MaxValue;Publish(false);
        }
        void RetryRoom(){chainError=null;eventLine=null;if(reportFiled)OpenNextRoom();else CheckRoomEnd();}
        const float ResultLockSeconds=.6f;
        float finishedAt=-1;
        /// <summary>Sonuç kartının yer satırı; harekâtta bölge ve sektör (“Doğu Avrupa · 02 Dosya katı”).</summary>
        protected virtual string ResultPlace=>Loc.F("{0} · {1}",RegionTitle??Loc.T("Saha"),Baskin.RoomName(State));
        /// <summary>
        /// Bölgenin adı (YZ adı, çevrilmez): baskının parametresinden; yoksa paketin YZ adı.
        /// Canlı baskın harekâtın haritasından okur (gösterilen dilde). Bölgesiz baskında null.
        /// </summary>
        protected virtual string RegionTitle=>!string.IsNullOrEmpty(State.Params.RegionName)?State.Params.RegionName:GameData.Regions.FirstOrDefault(d=>d.Id==State.Params.Region)?.Ai;
        protected virtual int BlueprintSheet=>Math.Max(1,Math.Min(3,State.Params.RaidNumber));
        protected virtual int BlueprintShift=>0;
        protected virtual string CrateContents=>string.Empty;
        /// <summary>Tesis odasında tahtada yatan gövdeler (aşama 7; ekiple giden son nefes değil); örnek tahtada boş.</summary>
        IEnumerable<Baskin.Unit> LyingBodies()=>State?.Room==null?Enumerable.Empty<Baskin.Unit>():State.Units.Where(FacilityMoveText.Lying);
        bool FatalEnd=>State!=null&&State.Escape&&State.Turn>=State.MaxTurns&&State.Units.Any(u=>Baskin.IsSpecialist(u)&&Baskin.OnBoard(u));
        protected abstract void Finish();
        /// <summary>Harekâtın sivil güveni (baskından önceki değer, tavan, sunucu başına bedel); harekât yoksa tavan 0.</summary>
        protected virtual (int Trust,int Max,int PerCivilian) TrustStake=>(0,0,0);
        protected void Publish()=>Publish(true);
        protected void Publish(bool redraw)
        {
            if(State==null)return;
            // Tesis aşama 12: AYIR verilemez olduysa (tur, geçiş, geri alma) bayrak kapanır
            if(detach&&!CanDetach)detach=false;
            var display=executing?machineBefore:State;
            var unit=Unit();var moves=unit!=null&&!Armed&&!Kip&&Baskin.CanMove(State,unit)?Baskin.Reachable(State,unit):new List<Baskin.Pt>();
            // Tesis hamlesi ya da ekipman eylemi kuruluyken (aşama 7, 9) hedefler ve önizleme eylemin; yürüyüş boyası kapalı
            var targets=unit==null?new List<Baskin.Pt>():Kip?Baskin.ActionTargets(State,unit,KipKind,KipOther):Armed&&Baskin.CanAct(State,unit)?Baskin.Targets(State,unit):new List<Baskin.Pt>();
            bool onTarget=hover.HasValue&&targets.Any(p=>p.X==hover.Value.x&&p.Y==hover.Value.y);
            preview=!onTarget?null:Kip?Baskin.PreviewAction(State,unit.Id,KipKind,hover.Value.x,hover.Value.y,KipOther):Baskin.Preview(State,unit.Id,hover.Value.x,hover.Value.y);
            var walk=preview==null&&hover.HasValue&&moves.Any(p=>p.X==hover.Value.x&&p.Y==hover.Value.y)?hover:null;
            hazards=State.Ev==null?null:preview!=null?preview.Hazards:walk.HasValue?Baskin.PreviewMove(State,unit.Id,walk.Value.x,walk.Value.y):Baskin.Hazards(State);
            bool loot=Baskin.IsLoot(State);
            // Taşıyıcı ve kaçış sunum kopyasından: makine evresinde düşüş ya da tahliye efekti oynayınca görünür
            var carrier=display.Carrier!=0?display.Units.FirstOrDefault(u=>u.Id==display.Carrier):null;
            var vm=new RaidViewModel{IsLootMission=loot,Escaping=display.Escape&&State.Phase!="won"&&State.Phase!="lost",CargoLost=carrier!=null&&carrier.Dead,CanRetreat=!State.Escape,Turn=display.Turn,Turns=display.MaxTurns,Phase=executing?"enemy":State.Phase,Locked=State.Phase!="player"||Busy,AbilityArmed=Armed,CanUndo=State.Hist.Count>0,
                EndConfirm=confirm,RetreatArmed=retreat,FastMachines=fast,Paused=paused,OpenPanel=panel,LogUnread=unread,ActionArmed=ActionArmed,GearArmed=GearArmed,CargoOnFloor=Session!=null&&display.CargoX>=0,
                Civilians=civilianCount,CivilianBroken=display.Collateral,AbilityLabel=RegionLabel(loot),AbilityTip=RegionTip(loot),
                Moves=moves.Select(p=>new Vector2Int(p.X,p.Y)).ToArray(),AbilityTargets=targets.Select(p=>new Vector2Int(p.X,p.Y)).ToArray()};
            // Fare yürüyüş karesinde: seçili birimin oraya yolu (Core'un yürüyüş kuralı; 2026-11-24, tahta çizer, parça bu yoldan yürür)
            if(walk.HasValue&&Baskin.MovePath(State,unit,walk.Value.x,walk.Value.y) is List<Baskin.Pt> walkPath)vm.MovePath=walkPath.Select(p=>new Vector2Int(p.X,p.Y)).ToArray();
            // Tesiste önceki odaların kırdıkları açılışta donar (collateralBefore; örnek tahtada 0): son oda dosyalanınca çift sayılmaz
            var stake=TrustStake;vm.TrustMax=stake.Max;vm.Trust=Math.Max(0,stake.Trust-(collateralBefore+display.Collateral)*stake.PerCivilian);
            var region=GameData.Regions.FirstOrDefault(r=>r.Id==State.Params.Region);
            vm.CrateContents=loot?CrateContents:string.Empty;vm.BlueprintSeed=State.Params.Seed;vm.BlueprintSheet=State.Room!=null?State.Room.Index:BlueprintSheet;vm.BlueprintShift=BlueprintShift;
            // Tesis odası: kanadın numarası ve dönüş (antet “KANAT 01 · PAFTA 2/3 · DÖNÜŞ”); örnek tahtada 0 / false
            vm.BlueprintWing=State.Room?.Wing??0;vm.BlueprintReturn=FacilityRaidText.IsReturnBoard(State);
            // Tesis aşama 11: üç odalı savunma (antet, görev çipi, GEÇ'in yazısı ve onayı, geri çekilme metni, kurallar kartı); saldırıda false
            bool defense=FacilityDefenseRaidText.On(State);vm.BlueprintDefense=vm.Defense=defense;
            if(defense){vm.PassLabel=FacilityDefenseRaidText.PassLabel;vm.PassNote=FacilityDefenseRaidText.PassNote(RoomTrust);
                vm.RetreatBody=FacilityDefenseRaidText.RetreatBody(FallTrust+RoomTrust*Baskin.Abandoned(State));vm.RulesDefense=FacilityDefenseRaidText.Rules(State.MaxTurns,RoomTrust);}
            // Tesis aşama 12 (Katar'ın dönüş geçidi, herkes kapıda): AYIR açık "AYIR · takip yok · Teknisyen'in yeteneği"; kapalı GEÇ ve takipçi sayısı
            else if(Session!=null&&FacilityConditionText.IsKatarReturn(State)&&Baskin.CanPass(State))
            {if(detach){vm.PassLabel=FacilityConditionText.DetachLabel;vm.PassNote=FacilityConditionText.DetachNote;}else vm.PassNote=FacilityConditionText.FollowNote(Baskin.FollowerIds(State).Count);}
            vm.BlueprintBuilding=region==null?Loc.T("Saha"):region.Building;var sectorIndex=GameData.SectorIndex(State.Params.SectorId);vm.BlueprintRoom=region!=null&&sectorIndex>=0?region.Sector(sectorIndex):Loc.T("Saha");
            // Tesis aşama 12: antetin notu şarta bağlı (Simulation.RegionNote: na/oc'de Katar ve Yalpa'nın tesis notu); savunmada binanın statik
            // notu (savunma şart kurmaz; masadaki kuşatma fişi gibi)
            vm.BlueprintRegion=RegionTitle;vm.BlueprintNote=region==null?string.Empty:Session?.Sim!=null&&!defense?Session.Sim.RegionNote(region.Id)??region.BuildingNote:region.BuildingNote;
            // Kilit niyetinin hedef karesindeki uzman: kartında “KİLİT TEHDİDİ · #n” (S6c)
            var locks=State.Phase=="player"?Baskin.Intents(State).Where(i=>i.Kind=="lock"&&i.HasTarget&&!i.TargetNone).ToList():new List<Baskin.IntentView>();
            // Bölge olayının uyarısı (“KAYNAK: 1 hasar”): kilit tehdidinden sonra; asansör kabinindeki ve donmuş uzman da kartında
            string Threat(Baskin.Unit u){if(u.Cab&&!u.Dead)return Loc.T("KABİNDE · bir evre tahtada yok");var i=!Baskin.OnBoard(u)?null:locks.FirstOrDefault(x=>x.TargetX==u.X&&x.TargetY==u.Y);
                if(i!=null)return Loc.F("KİLİT TEHDİDİ · #{0}",i.Order);var w=Baskin.OnBoard(u)?hazards?.Warnings.FirstOrDefault(x=>x.Unit==u.Id):null;return w!=null?w.Text:u.Frozen&&Baskin.OnBoard(u)?Loc.T("DONMUŞ · bu tur yürüyemez"):null;}
            vm.Team=Roster().Select(letter=>
            // Kadroda olmayan uzman sığınakta yaralı (yedek de yok): kart kapalı, YARALI damgalı
            {var u=display.Units.FirstOrDefault(x=>Baskin.IsSpecialist(x)&&Baskin.UnitTypes[x.Type].Short==letter.ToString());if(u==null)return FallenCard(letter)??new RaidTeamModel{Letter=letter,Name=PersonalName(letter),Thumb="thumb_standee_"+letter+"_done",Down=true,Absent=true};
            // Süre dolarken kapıya yetişemeyen uzman (Withdrew): geçiş kayarken de kartı kapalı, sonraki odadaki FallenCard ile aynı
            if(u.Withdrew)return new RaidTeamModel{Letter=letter,Name=PersonalName(letter),Thumb="thumb_standee_"+letter+"_done",Down=true,DownText=Loc.T("GERİ ÇEKİLDİ")};return new RaidTeamModel{Letter=letter,Name=PersonalName(letter),Thumb="thumb_standee_"+letter+(u.Dead||u.Out||u.Acted||u.AtGate?"_done":letter=='E'&&u.Hp<u.Max?"_damaged":""),Hp=u.Hp,Max=u.Max,MoveRange=u.MoveRange>0?u.MoveRange:Baskin.UnitTypes[u.Type].Move,Power=Math.Max(1,u.Power),Equipment=EquipmentLabels(u),Selected=Selected==letter,Move=!Busy&&Baskin.CanMove(State,u),Ability=!Busy&&Baskin.CanAct(State,u),Armed=Selected==letter&&(Armed||Kip),Down=u.Dead,DownText=BodyText(u),Out=u.Out,Left=u.Left,Armor=u.Armor,Rookie=Rookie(u),Threat=Threat(u),AtGate=u.AtGate};}).ToArray();
            // Tesis aşama 12 (Katar): AYIR verilebilirken kapıdaki Teknisyen'in kartı tıklanır, hak satırı "AYIR · Q"
            if(CanDetach)foreach(var t in vm.Team)if(t.AtGate&&!t.Down&&Unit(t.Letter)?.Type=="tek")t.Detach=true;
            // Tesis aşama 9: köpeğin dördüncü kartı (örnek tahtada ve köpeksiz kadroda yok)
            if(DogCard(display,Threat) is RaidTeamModel dogCard)vm.Team=vm.Team.Append(dogCard).ToArray();
            // Tesis odasının donanımı önce sınanır (giriş odası ikmal görevindedir); kırık donanımın damgası (vana KAPANDI). Örnek tahtada
            // targetForms'ta donanım yok
            // Tesis aşama 11: savunmanın saha rölesi (zırhsız, kırılınca KIRILDI)
            vm.Targets=targetForms.Select(t=>{var o=display.Cells[t.y,t.x].O;bool device=Baskin.IsDevice(t.kind),relay=t.kind==Baskin.RelayKind;
                if(relay)return new RaidTargetModel{Name=FacilityDefenseRaidText.RelayName,Thumb="thumb_relay",Hp=o?.K==t.kind?o.Hp:0,Max=t.max,Armor=false,Broken=o?.K!=t.kind,Cell=Cell(t.x,t.y),Stamp=o?.K!=t.kind?Loc.T("KIRILDI"):null};
                return new RaidTargetModel{Name=device?Loc.T(Baskin.ObjNames[t.kind]):loot?Loc.T("İkmal kasası"):t.kind=="node"?Loc.T("Düğüm"):Loc.T("Çekirdek"),
                Thumb=device?"thumb_cooling":loot?"thumb_crate":"thumb_cabinet_"+t.kind,Hp=o?.Hp??0,Max=t.max,Armor=!device,Broken=device?o?.K!=t.kind:o==null,Cell=Cell(t.x,t.y),
                Stamp=device&&o?.K!=t.kind?(t.kind=="vana"?Loc.T("KAPANDI"):Loc.T("KIRILDI")):null};}).ToArray();
            // Geçit satırı (can taşımaz) görev çipinin başında: amaç satırı onun altında (hedefsiz dönüş odasında da görünür)
            var gate=FacilityRaidText.GateRow(State);if(gate!=null)vm.Targets=new[]{gate}.Concat(vm.Targets).ToArray();
            // Tesis aşama 11: savunmada bu odaya gelecek ilk dalga (“DALGA 2/3 · 3 makine · 5. tur”; can taşımaz)
            if(defense&&FacilityDefenseRaidText.WaveRow(State,Rules) is RaidTargetModel wave)vm.Targets=vm.Targets.Append(wave).ToArray();
            // Görev çipi (Revizyon 2 · S6): bölge özelliğinin işareti, amaç satırı; telsizin okunmamış satırları; makine hamlesi
            var trait=State.Params?.Traits?.FirstOrDefault(t=>Baskin.RegionTraits.ContainsKey(t));
            vm.TraitGlyph=trait!=null?SiginakGlyphs.Trait(trait,true):null;
            vm.Place=State.Room!=null?DeskText.Upper(State.Room.Name):Room!=null?DeskText.Upper(Room):null;
            // Tesis aşama 11: savunmada şerit dalga turlarını taşır (Core DefenseWaves, terk gecikmesi dahil; bölge olayı yok)
            vm.TurnEvents=defense?FacilityDefenseRaidText.TurnEvents(State):hazards?.Turns;
            // Kurallar kartının lejantında kanalın binadaki adı (varsayılan kitte yok)
            vm.CableName=RaidKitText.Cable(KitId);
            // Kaçışta amaç ekibi girişten çıkarmak (taşıyıcı tahtadaysa malzemeyle). Sunum kopyasından: makine evresinde yükün düşüşü
            // cargo-drop oynayınca görünür (görev çipi damgası ve taşıyıcı gibi)
            vm.Goal=defense?FacilityDefenseRaidText.Goal(State):FacilityRaidText.Goal(display,carrier!=null&&Baskin.OnBoard(carrier))??(vm.Escaping?(carrier!=null&&Baskin.OnBoard(carrier)?Loc.T("Amaç: malzemeyle girişten çık"):Loc.T("Amaç: ekibi girişten çıkar")):loot?Loc.T("Amaç: kasadan ikmal al"):vm.Targets.Length>1?Loc.T("Amaç: ikisini de kır"):Loc.T("Amaç: kır"));
            vm.LogUnreadCount=unread?Math.Max(1,LogCount-seenLogs):0;
            // Tesis odasında herkes kapıda: EMRİ İLET GEÇ olur (örnek tahtada hep false)
            vm.CanPass=Session!=null&&Baskin.CanPass(State)&&!Busy;
            // Telsiz masası (§7.3): yayın lambası ve teybin kayıtlı paftaları (örnek tahtada teyp boş)
            vm.OnAir=onAir;vm.TapeSheets=TapeSheets();
            vm.MachineMoves=roster.Count;vm.MachineMove=Math.Min(roster.Count,queue.Values.Count(q=>q=="done"||q=="cancel")+(queue.Values.Any(q=>q=="now")?1:0));
            if(unit!=null)
            {
                // Aşama 9: köpek ve tutsak kendi kartıyla (başlık, disk harfi; yalnız Hareket satırı, Q ve yetenek yok)
                bool specialist=Baskin.IsSpecialist(unit);
                vm.UnitCard=specialist?vm.Team.First(u=>u.Selected&&u.UnitId==0):OtherCard(unit,vm.Team);var ability=Baskin.UnitTypes[unit.Type].Ability;
                var rows=new List<RaidCardRow>{new RaidCardRow{Icon="ic_boots_s",Text=Loc.T("Hareket"),Detail=Loc.F("{0} kare",vm.UnitCard.MoveRange),Used=!vm.UnitCard.Move}};
                if(specialist)rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=AbilityName(ability,loot),Detail=AbilityRange(ability,unit.Charges),Armed=Armed,Used=!vm.UnitCard.Ability});
                // Kartta üç satır yeri var: yedek olduğu başlıkta (“· YEDEK”), plaka üçüncü satırda
                if(specialist)rows.AddRange(KitRows(unit,false));
                vm.UnitRows=rows.ToArray();
                // Revizyon 2 · S6 (Soru 7'nin ara cevabı): yalnız adlar; görev eylemi yeşil, robota vuran kırmızı
                vm.Effects=ability switch{"sabotaj"=>new[]{loot?Loc.T("Kasadan ikmal al"):Loc.T("Dolaba vur · −2 can"),Math.Max(1,unit.Power)==1?Loc.T("Robota vur · −1 can"):Loc.F("Robota vur · −{0} can",Math.Max(1,unit.Power))},"emp"=>new[]{unit.EmpBoosts>0?Loc.F("−{0} hedef",1+Math.Max(0,unit.EmpBoostDamage)):Loc.T("−1 hedef"),Loc.T("itme")},
                    "dinamit"=>new[]{Loc.T("Duvarı yık"),Loc.T("Robota vur · −1 can")},"duman"=>new[]{Loc.T("Nişanlı ateşi keser"),Loc.T("bu tur")},_=>new[]{Loc.T("Robota vur · −1 can"),Loc.T("çeker")}};
                vm.EffectTones=ability=="sabotaj"||ability=="dinamit"?new[]{"good","bad"}:ability=="duman"?new[]{"good","plain"}:new[]{"bad","plain"};
                if(!specialist){vm.Effects=Array.Empty<string>();vm.EffectTones=Array.Empty<string>();}
                // Tesis aşama 9: uzmanın ekipman satırı (tek satır; R fişi kullanılabilirken ya da kip açıkken). Örnek tahtada ve ekipmansızda yok
                if(FacilityGearText.GearRow(State,unit,GearArmed?gearKind:null) is RaidCardRow gearRow){vm.GearRows=new[]{gearRow};vm.GearKey=GearArmed||!gearRow.Used?0:-1;}
                // Tesis odası (aşama 7): bu odada anlamlı hamleler; kullanılamayan soluk ve nedeniyle. E fişi kip kapalıyken E'nin kuracağı
                // satırda, açıkken kurulu satırda. Kurulu hamlenin iki etkisi alt düğmelerde (örnek tahtada seçenek yok: hepsi bugünkü)
                var options=FacilityMoveText.Options(State,unit);
                actionRowMap=null;
                if(options.Count>0)
                {
                    // Aşama 9 (§3.1 yedeği): kartta en çok MaxCardRows satır; aşınca kullanılamayan hamleler tek "Diğer" satırına iner,
                    // yine aşarsa sürüklemenin sonraki gövde satırları gizlenir (E döngüsünde ve tahta tıkıyla yine seçilir)
                    var shown=Enumerable.Range(0,options.Count).ToList();int folded=0;
                    int room=MaxCardRows-vm.UnitRows.Length-vm.GearRows.Length;
                    if(shown.Count>room)
                    {
                        var unusable=shown.Where(i=>!options[i].Usable).ToList();
                        if(unusable.Count>=2){shown=shown.Where(i=>options[i].Usable).ToList();folded=unusable.Count;}
                        var drags=shown.Where(i=>options[i].Kind==Baskin.ActDrag).Skip(1).ToList();
                        for(int k=drags.Count-1;k>=0&&shown.Count+(folded>0?1:0)>room;k--)shown.Remove(drags[k]);
                    }
                    var rowsOut=shown.Select(i=>options[i]).Select(o=>new RaidCardRow{Icon=FacilityMoveText.Icon(o.Kind),Text=o.Name,Detail=o.Shown,Armed=ActionArmed&&o.Kind==actionKind&&o.Other==actionOther,Used=!o.Usable}).ToList();
                    if(folded>0)rowsOut.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.F("Diğer · {0} hamle kullanılamaz",folded),Detail=string.Empty,Used=true});
                    vm.ActionRows=rowsOut.ToArray();
                    int key=ActionArmed?options.FindIndex(o=>o.Kind==actionKind&&o.Other==actionOther):options.FindIndex(o=>o.Usable);
                    vm.ActionKey=shown.IndexOf(key);
                    if(folded>0||shown.Count<options.Count)actionRowMap=shown.Concat(folded>0?new[]{-1}:Array.Empty<int>()).ToArray();
                }
                if(Kip){var fx=FacilityMoveText.Effects(State,unit,KipKind);vm.Effects=fx.Texts;vm.EffectTones=fx.Tones;}
            }
            if(preview!=null)vm.CollisionTiles=preview.Fx.Where(f=>f.K=="push"&&!preview.Moves.Any(m=>m.Unit==f.Unit)).Select(f=>new Vector2Int(f.X+f.Dx,f.Y+f.Dy)).ToArray();
            if(hoverVisible&&hover.HasValue)vm.HoverCard=Inspector(hover.Value);
            vm.Status=confirm?Loc.T("Kullanılmamış haklar var. Enter ile emri ilet; Esc ile vazgeç."):retreat?Loc.T("Geri çekilmek için tekrar bas. Esc ile vazgeç."):paused?Loc.T("DURAKLATILDI · Esc ile devam"):panel=="log"?(Room!=null?Loc.F("TELSİZ KAYDI · Ekip {0} girişinde.",Room):Loc.T("TELSİZ KAYDI · Ekip harekete hazır.")):panel=="rules"?RulesLine(vm):null;
            // Tesis aşama 12 (Katar'ın dönüş geçidi): "Enter: GEÇ · Q: AYIR (Teknisyen)" ya da AYIR'ın neden verilemediği
            if(vm.Status==null&&Session!=null&&!Busy)vm.Status=FacilityConditionText.DetachStatus(State);
            Enrich(vm,display);ApplyFixture(vm);Current=vm;if(board!=null)board.LabelsHidden=vm.Finished||board.Turning||passing;if(redraw&&board!=null&&!Busy)Draw(vm);Changed?.Invoke(vm);
        }
        protected virtual void ApplyFixture(RaidViewModel vm){}
        /// <summary>Kurallar panelinin durum satırı: örnek tahtada bugünkü; tesis odasında E; aşama 9: ekipmanlı kadroda R, köpekte 1–4.</summary>
        string RulesLine(RaidViewModel vm)
        {
            if(State.Room==null)return Loc.T("1 / 2 / 3: seç · Q: yetenek · Z: geri al · Enter: emri ilet · Esc: vazgeç");
            if(vm.Team.Any(t=>t.Dog))return Loc.T("1–4: seç · Q: yetenek · E: hamle · R: ekipman · Z: geri al · Enter: emri ilet · Esc: vazgeç");
            if(State.Units.Any(u=>Baskin.IsSpecialist(u)&&!string.IsNullOrEmpty(u.Gear)))return Loc.T("1 / 2 / 3: seç · Q: yetenek · E: hamle · R: ekipman · Z: geri al · Enter: emri ilet · Esc: vazgeç");
            return Loc.T("1 / 2 / 3: seç · Q: yetenek · E: hamle · Z: geri al · Enter: emri ilet · Esc: vazgeç");
        }
        /// <summary>
        /// Köpeğin dördüncü ekip kartı (aşama 9, K2): seçili köpek, yoksa ayaktaki ilk, yoksa ilk köpek (sahibin kadro sırası). Ad KÖPEK,
        /// eşya satırı sahibi (birden çok köpekte "+n köpek"), düşünce TAHTADAN ÇEKİLDİ. Örnek tahtada ve köpeksiz kadroda null.
        /// </summary>
        RaidTeamModel DogCard(Baskin.BState display,System.Func<Baskin.Unit,string> threat)
        {
            if(State.Room==null)return null;
            var dogs=display.Units.Where(Baskin.IsDog).OrderBy(d=>d.Owner).ThenBy(d=>d.Id).ToList();if(dogs.Count==0)return null;
            var u=dogs.FirstOrDefault(d=>d.Id==SelectedOther)??dogs.FirstOrDefault(Baskin.OnBoard)??dogs[0];
            var live=State.Units.FirstOrDefault(d=>d.Id==u.Id)??u;
            string owner=FacilityGearText.OwnerName(State,u);
            string equipment=string.IsNullOrEmpty(owner)?null:dogs.Count>1?Loc.F("Sahibi: {0} · +{1} köpek",owner,dogs.Count-1):Loc.F("Sahibi: {0}",owner);
            int commands=Baskin.DogCommandsFor(State,u);
            if(commands>1) equipment=Loc.F("{0} · {1}/{2} komut",equipment,commands-u.DogCommands,commands);
            bool selected=SelectedOther==u.Id;
            return new RaidTeamModel{Letter=Loc.T("Ö")[0],UnitId=u.Id,Dog=true,Name=Loc.T("KÖPEK"),Thumb=u.Dead||u.Out||u.Acted?"thumb_dog_done":"thumb_dog",Hp=u.Hp,Max=u.Max,MoveRange=FacilityMoveText.Range(u),
                Equipment=equipment,Selected=selected,Move=!Busy&&Baskin.CanMove(State,live),Ability=!Busy&&Baskin.CanAct(State,live),Armed=selected&&Kip,Down=u.Dead,DownText=u.Dead?Loc.T("TAHTADAN ÇEKİLDİ"):null,Out=u.Out,Threat=u.Dead?null:threat(u)};
        }
        /// <summary>Seçili uzman olmayan birimin yetenek kartı başlığı (aşama 9): köpekte "KÖPEK · sahibi" (dördüncü kartın modeli),
        /// tutsakta "MAKİNE · TUTSAK · {k} TUR" / "… · SON TUR" ve makinenin kısa harfi.</summary>
        RaidTeamModel OtherCard(Baskin.Unit u,RaidTeamModel[] team)
        {
            var card=team.FirstOrDefault(t=>t.UnitId==u.Id);
            string name=Baskin.IsDog(u)?FacilityGearText.DogName(State,u):Loc.F("{0} · {1}",DeskText.Upper(Baskin.UnitTypes[u.Type].Name),FacilityGearText.CaptiveText(State,u));
            return new RaidTeamModel{Letter=card?.Letter??Baskin.UnitTypes[u.Type].Short[0],UnitId=u.Id,Dog=Baskin.IsDog(u),Name=name,Hp=u.Hp,Max=u.Max,MoveRange=FacilityMoveText.Range(u),Selected=true,
                Move=!Busy&&Baskin.CanMove(State,u),Ability=!Busy&&Baskin.CanAct(State,u),Armed=Kip,Armor=u.Armor,Down=u.Dead};
        }
        /// <summary>Kadro sığınaktan gelir: yedek uzman (acemi) ve takılı zırh plakası.</summary>
        protected virtual string EquipmentLabels(Baskin.Unit u)
        {
            var items=new List<string>();
            if(!string.IsNullOrEmpty(u.PersonId))items.Add(PlanningBoardData.EquipmentName("kit_"+u.Type));
            if(u.Armor>0)items.Add(PlanningBoardData.EquipmentName("armor"));
            if(u.EmpBoosts>0)items.Add(PlanningBoardData.EquipmentName("emp_cell"));
            // Tesis aşama 9: ekipman ve kalan hakkı (örnek tahtada Gear null)
            if(!string.IsNullOrEmpty(u.Gear))items.Add(GearLabel(u,u.Gear));
            return string.Join(Loc.T(" · "),items);
        }
        /// <summary>Ekip kartının ekipman yazısı (aşama 9): "{ad} · {n} hak", hak bitince üstü çizili; köpek (sürekli) yalnız adı; uzmanın
        /// baskına götürmediği eşya (dinlenen köpek) adı.</summary>
        protected static string GearLabel(Baskin.Unit u,string kind)
        {
            string name=PlanningBoardData.EquipmentName(kind);
            if(u.Gear!=kind||kind==FacilityGear.Dog||u.GearLeft<0)return name;
            return u.GearLeft<=0?Loc.F("<s>{0}</s>",Loc.F("{0} · {1} hak",name,0)):Loc.F("{0} · {1} hak",name,u.GearLeft);
        }
        /// <summary>Tesis odasında düşenin ekip kartı durumu (aşama 7): tesisten çıkarıldı, gövde kapıda ya da yerde (karesiyle). Son nefes
        /// (Core: <see cref="Baskin.Unit.Breath"/>, oda biterken bu evrede düşüp ekiple giden) yerde değil: Core'un oda raporundaki gibi
        /// tesisin girişinde çıkarıldı (Body 2), geçişte ekiple taşınıyor (Body 1). Örnek tahtada ve ayaktakinde null (kart bugünkü DÜŞTÜ'yü
        /// yazar).</summary>
        /// <para>Tesis aşama 12 (Katar): kenardan düşen (<see cref="Baskin.Unit.Overboard"/>) "VAGONDAN DÜŞTÜ" (gövde değil).</para>
        string BodyText(Baskin.Unit u)=>State?.Room==null?null:u.Overboard?FacilityConditionText.CardOverboard():!Baskin.IsBody(u)?null:u.Out||u.Breath&&Baskin.IsFinalExit(State)?Loc.T("ÇIKARILDI"):u.AtGate?Loc.T("GÖVDE KAPIDA"):u.Breath?Loc.T("EKİPLE TAŞINIYOR"):Loc.F("YERDE · {0}",Cell(u.X,u.Y));
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
            // Tesis odası (aşama 7): ayakta birimin olmadığı karede yatan gövde kişinin kartıyla (örnek tahtada null)
            var body=u==null&&o==null?FacilityMoveText.BodyAt(State,tile.x,tile.y):null;
            // Aşama 9: aygıt, köpek ve tutsak kendi başlığı ve küçük resmiyle (uzman harfle, makine türüyle)
            var card=new RaidInspectorModel{IsLootMission=loot,Title=u!=null?(Baskin.IsTurncoat(u)?Loc.F("Sızmacı · {0}",u.PersonName):Baskin.IsSpecialist(u)?PersonalName(Baskin.UnitTypes[u.Type].Short[0]):FacilityGearText.UnitTitle(u)??DeskText.Upper(Baskin.UnitTypes[u.Type].Name)):o!=null?(loot&&Baskin.IsTarget(o.K)?Loc.T("İkmal kasası"):Loc.T(Baskin.ObjNames[o.K])):body!=null?PersonalName(Baskin.UnitTypes[body.Type].Short[0]):Loc.T("Boş kare"),
                Thumb=u!=null?(Baskin.IsSpecialist(u)||Baskin.IsTurncoat(u)?"thumb_standee_"+Baskin.UnitTypes[u.Type].Short:FacilityGearText.UnitThumb(u)??EnemyThumb(u.Type)):o!=null?(loot&&Baskin.IsTarget(o.K)?"thumb_crate":ObjectThumb(o.K)):body!=null?"thumb_standee_"+Baskin.UnitTypes[body.Type].Short+"_done":null,Hp=u?.Hp??o?.Hp??0,Max=u?.Max??o?.Max??body?.Max??0};
            var rows=new List<RaidCardRow>();
            // Tesis hamlesinin önizleme satırları önce (karar anında en önemlisi; 5 satır sınırı)
            var actor=Unit();
            if(preview!=null&&Kip&&actor!=null)rows.AddRange(FacilityMoveView.Preview(State,actor,KipKind,KipOther,tile.x,tile.y,preview,Rules,NextKnown()).Rows);
            int lead=rows.Count;
            var cell=State.Cells[tile.y,tile.x];
            if(u==null&&o==null&&cell.T=="wall")card.Title=Loc.T("Duvar");
            // Aşama 10: çift cidarlı duvar (başlık ve lokum bedeli)
            if(u==null&&o==null&&FacilityCounterText.WallTitle(State,tile.x,tile.y) is string wallTitle){card.Title=wallTitle;rows.AddRange(FacilityCounterText.WallRows(State,tile.x,tile.y));}
            if(u==null&&o==null&&cell.T=="cable")card.Title=Loc.T("Elektrikli kanal");
            // Tesis odası (örnek tahtada Shutter false, Vent 0): ağır kepenk duvardır (duvarın satırı kalır); menfez ve öbür ucu
            if(u==null&&o==null&&cell.Shutter&&cell.T=="wall")card.Title=Loc.T("Ağır kepenk");
            if(u==null&&o==null&&cell.Vent>0&&cell.T!="wall")
            {
                card.Title=Loc.T("Menfez");var other=FacilityRaidText.VentOther(State,tile.x,tile.y);
                if(other!=null)rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.F("Havalandırma ızgarası · öbür ucu {0}",other)});
            }
            // Tesis odası (aşama 7): sandık nişi ve kalkık kepenk başlığı; gövde, yük, iş, tutuş ve pusu hattı satırları (örnek tahtada yok)
            if(u==null&&o==null&&body==null&&FacilityMoveText.TileTitle(State,tile.x,tile.y) is string workTitle)card.Title=workTitle;
            rows.AddRange(FacilityMoveText.TileRows(State,tile.x,tile.y,Rules));
            // Tesis aşama 11: önceki baskının izi (yıkık duvar, açık kepenk, engel…), saha rölesi, savunmada batı girişi (dalganın geliş turu)
            rows.AddRange(FacilityDefenseRaidText.TraceRows(State,tile.x,tile.y));
            if(o!=null)rows.AddRange(FacilityDefenseRaidText.RelayRows(o));
            if(u==null&&o==null&&FacilityDefenseRaidText.WestEntry(State,tile.x,tile.y) is var west&&west.HasValue){card.Title=west.Value.Title;rows.Add(west.Value.Row);}
            // Aşama 9: aygıt, köpek, tutsak, niyetsiz makine ve engel satırları (örnek tahtada boş)
            if(u!=null)rows.AddRange(FacilityGearText.UnitRows(State,u));
            if(o!=null)rows.AddRange(FacilityGearText.ObjectRows(State,o));
            // Aşama 10: makinenin önlemi (cıvatalı ya da sökük) ve nesnenin önlemi (kesici kol, koku yayıcı)
            if(u!=null)rows.AddRange(FacilityCounterText.UnitRows(State,u));
            // Tesis aşama 12 (Katar): dönüşte bağlantıya menzil içindeki makine (GEÇ'te takip eder)
            if(FacilityConditionText.FollowRow(State,u) is RaidCardRow follow)rows.Add(follow);
            if(o!=null)rows.AddRange(FacilityCounterText.ObjectRows(State,o));
            if(Baskin.IsSpecialist(u))rows.AddRange(KitRows(u,true));
            // Sis bu turun makine evresinde nişanlı ateşi keser
            if(cell.Smoke)rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Sisli kare: nişanlı ateş kesilir"),Detail=Loc.T("bu tur"),Good=true});
            // Bölge olayının karesi (bulut, kaynak, çatlak…): ne olduğu ve bu tur sonu ne olacağı
            if(hazards!=null)foreach(var hm in hazards.Marks.Where(m=>m.X==tile.x&&m.Y==tile.y&&m.Kind!="ghost").Take(2))
            {var row=HazardRow(hm.Kind);if(row.Text!=null)rows.Add(new RaidCardRow{Icon=row.Bad?"ic_bolt_s":"ic_eye_s",Text=row.Text,Detail=hm.Label});}
            if(o!=null&&o.K=="kol")rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Kaide hasar almaz; her darbe kolu bir adım fazla döndürür")});
            if(o!=null&&o.K=="tup")rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Vurulursa bir tur sonra çizili hattında uçar")});
            // Tesis donanımı (giriş ve ara oda): kırılınca (vana: kapatılınca) hedef odasında ne olur; makineler ona nişan almaz
            if(o!=null&&Baskin.IsDevice(o.K))
            {
                var fx=FacilityDefenseRaidText.On(State)?null:FacilityRaidText.DeviceEffect(o.K,Rules,loot);if(fx!=null)rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=fx,Good=true});
                rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Loc.T("Makineler buna nişan almaz.")});
            }
            if(preview!=null)
            {
                // Tesis aşama 12 (Katar): kenardan düşme önizlemesi (Core'un fall efektleri) karar anında üstte
                rows.InsertRange(Math.Min(lead,rows.Count),FacilityConditionText.FallRows(State,preview.Fx,Rules));
                if(preview.Fx.Any(f=>f.K=="armor"))rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=Loc.T("Zırh: hasar işlemez")});
                var push=preview.Fx.FirstOrDefault(f=>f.K=="push");
                if(push!=null)rows.Add(new RaidCardRow{Icon="ic_push_s",Text=preview.Moves.Any(m=>m.Unit==push.Unit)?Loc.T("itme"):Loc.T("İtilir, üniteye çarpar"),Detail=Loc.T("−1")});
                if(preview.Fx.Any(f=>f.K=="break"&&f.Obj=="cool"))rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Ünite patlar"),Detail=Loc.T("−1")});
                // Lokum (aşama 8 ekipmanı) kalan hakkı yazar; çift cidarda bedel Core'dan (aşama 10: Baskin.DoubleWallCost)
                bool charge=Kip&&KipKind==Baskin.ActCharge;int cost=charge&&Baskin.IsDoubleWall(State,tile.x,tile.y)?Baskin.DoubleWallCost(State):1;
                if(preview.Fx.Any(f=>f.K=="breach"))rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Duvar yıkılır"),Detail=Loc.F("kalan {0}",Math.Max(0,(charge?Unit()?.GearLeft??cost:Unit()?.Charges??1)-cost)),Good=true});
                foreach(var fx in preview.Fx.Where(f=>f.K=="debris"))if(preview.Fx.Any(f=>f.K=="dmg"&&f.X==fx.X&&f.Y==fx.Y))rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Moloz"),Detail=Loc.T("−1")});
                // Sise kesilecek saldırılar: hamleden önce kesik olmayan niyetler
                if(preview.Intents!=null)foreach(var cut in preview.Intents.Where(i=>i.Smoked&&!Baskin.Intents(State).Any(j=>j.Unit==i.Unit&&(j.Smoked||j.Cloud))).Take(2))
                    rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Sise kesilir"),Detail=Loc.F("<s>{0}</s>",cut.Order),Good=true});
                foreach(var dead in preview.Fx.Where(f=>f.K=="die"||f.K=="fall"&&State.Units.Any(v=>v.Id==f.Unit&&v.Side=='e')).Take(1))
                {
                    var intent=Baskin.Intents(State).FirstOrDefault(i=>i.Unit==dead.Unit);
                    rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Ateşi iptal"),Detail=intent!=null?Loc.F("<s>{0}</s>",intent.Order):null,Good=true});
                }
                if(rows.Count==lead)foreach(var fx in preview.Fx.Where(f=>f.K=="dmg").Take(2))rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=Loc.T("Öngörülen hasar"),Detail=Loc.F("−{0}",fx.N)});
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
                rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=Baskin.IsTurncoat(u)?AbilityName(Baskin.UnitTypes[u.Type].Ability,false):Baskin.UnitTypes[u.Type].Desc});
            }
            else if(o!=null&&Baskin.IsTarget(o.K))rows.Add(new RaidCardRow{Icon="mk_armor_13",Text=loot?Loc.T("Herhangi bir uzmanla bitişik kareden aç; sonra bütün ekip girişten çıkar"):Loc.T("Hedef dolabı: kırılınca bütün ekip girişten çıkar")});
            // Dinamit kepengi ve sandık nişini hedef almaz (Core): onların satırları TileRows'ta
            else if(u==null&&o==null&&cell.T=="wall"&&!cell.Shutter&&!cell.Crate)rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=State.Units.Any(v=>v.Side=='p'&&Baskin.OnBoard(v)&&v.Type=="maden")?Loc.T("Madenci dinamitle yıkabilir"):Loc.T("Geçilmez; atışı durdurur")});
            // Kanalın kuralı (Baskin.CableHurt): üstüne yürümek güvenli, itilen ya da çekilen 1 hasar alır; şebeke olayında yalnız canlı
            // fazın kanalı yakar (sönük kanal sıradan zemin; evreyi canlı kanalda bitirenin hasarı olay satırında)
            else if(u==null&&o==null&&cell.T=="cable")rows.Add(new RaidCardRow{Icon="ic_bolt_s",Text=State.Ev?.Id=="faz"?Loc.T("Yalnız canlı fazda üstüne itilen ya da çekilen birim 1 hasar alır."):Loc.T("Üstünden yürünür; üstüne itilen ya da çekilen birim 1 hasar alır.")});
            // Kaçış: çıkış karesi; seçili uzman oraya yürürse tahtadan çıkar. Son uzmanın çıkışı baskını bitirir (ikmalde malzeme
            // dışarıdaysa kazanır)
            // Mekânın binadaki adı başlığın altında, gri (kural adı başlıkta kalır): duvar, kanal, gedik; varsayılan kitte yok
            card.Subtitle=RaidKitText.Subtitle(State,tile.x,tile.y,KitId);
            // Örnek tahtada ve tesisin girişinde (giriş odasının dönüşü) bugünkü çıkış; öbür tesis odalarında kapı kareleri
            if((State.Room==null||Baskin.IsFinalExit(State))&&State.Escape&&State.Exits.Any(p=>p.X==tile.x&&p.Y==tile.y))
            {
                if(u==null&&o==null){card.Title=Loc.T("Çıkış");card.Subtitle=null;}
                var carrier=State.Carrier!=0?State.Units.FirstOrDefault(v=>v.Id==State.Carrier):null;
                bool cargoInside=carrier!=null&&Baskin.OnBoard(carrier);
                // Tesiste yük fırlatılabilir: malzemeyi taşıyanın adı (örnek tahtada Teknisyen)
                rows.Add(new RaidCardRow{Icon="ic_eye_s",Text=!cargoInside?Loc.T("Buradan çıkan uzman tahliye edilir"):State.Room!=null?Loc.F("Buradan çıkan uzman tahliye edilir; malzeme {0} ile çıkar",FacilityMoveText.Who(carrier)):Loc.T("Buradan çıkan uzman tahliye edilir; malzeme Teknisyen'le çıkar"),Good=true});
                // Tesis odası: yük yerdeyken (taşıyıcı düştü) çıkan ekip onu bırakır (örnek tahtada CargoX hep −1)
                if(FacilityRaidText.CargoLeftRow(State) is RaidCardRow left)rows.Add(left);
                var sel=Unit();
                if(sel!=null&&Baskin.CanMove(State,sel)&&Baskin.Reachable(State,sel).Any(p=>p.X==tile.x&&p.Y==tile.y))
                {
                    // Aşama 9: sayılan uzmanlar (köpek, aygıt ve tutsak çıkışa sayılmaz; örnek tahtada aynı)
                    int alive=Baskin.Alive(State,'p').Count(Baskin.IsSpecialist);
                    bool last=alive==1;
                    if(last&&(!loot||carrier!=null&&(carrier.Out||carrier==sel)))card.Win=true;
                    // Aşama 9: yükü taşıyan köpek tahtadayken son uzman çıkarsa baskın biter, yük kaybedilir (Core FinishEscape)
                    else if(last&&cargoInside&&Baskin.IsDog(carrier))rows.Add(new RaidCardRow{Icon="ic_x_s",Text=Loc.T("Köpek yükle kalırsa yük kaybedilir")});
                    else if(!last)rows.Add(new RaidCardRow{Icon="ic_boots_s",Text=Loc.F("Tahtada kalan: {0}",alive-1),Detail=Loc.F("{0} tur",State.MaxTurns-State.Turn+1)});
                }
            }
            else if(State.Room!=null&&Baskin.IsGateTile(State,tile.x,tile.y))
            {
                if(u==null&&o==null){card.Title=FacilityRaidText.GateTitle(State);card.Subtitle=null;}
                rows.AddRange(FacilityRaidText.GateRows(State,Unit(),tile.x,tile.y));
            }
            card.Rows=rows.Take(5).ToArray();return card;
        }
        void Draw(RaidViewModel vm)
        {
            board.Sync(State,MarksFor(vm));
            DrawPreview(vm);
        }
        /// <summary>
        /// Tahtanın işaretleri (seçim, kapı, kâğıt notları, yürüyüş ve hedef boyası, harcanan, uyanma, sis, niyet okları, tehlike) görünüm
        /// modelinden ve canlı durumdan. Tahtasız da çağrılabilir (EditMode testleri; aşama 7). Durum yazmaz.
        /// </summary>
        public RaidBoard3D.Marks MarksFor(RaidViewModel vm)
        {
            var marks=new RaidBoard3D.Marks{Selected=Unit()?.Id??-1,Carrier=State.Carrier!=0?State.Carrier:-1};
            // Kaçışta çıkışlar (giriş kareleri) yeşil kesik çerçeveyle
            // Tesis odasında kapı kareleri (ileri geçit; kaçışta batı geçidi): aynı çerçeve. Kâğıt etiketleri (GEÇİT, MENFEZ, KEPENK)
            if(State.Phase=="player"){var exits=State.Room!=null?Baskin.GateTiles(State):State.Escape?(IReadOnlyList<Baskin.Pt>)State.Exits:Array.Empty<Baskin.Pt>();foreach(var p in exits)marks.Exits.Add(p.Y*8+p.X);}
            if(State.Room!=null)marks.Notes.AddRange(FacilityRaidText.BoardNotes(State));
            foreach(var p in vm.Moves)marks.Moves.Add(p.y*8+p.x);foreach(var p in vm.AbilityTargets)marks.Targets.Add(p.y*8+p.x);
            foreach(var u in State.Units)if(u.Acted&&!Baskin.IsGadget(u))marks.Spent.Add(u.Id);
            // Kum fırtınası: uyanacak makinelerin yeri görünmez (çoğaltıcının niyeti yine görünür)
            if(!State.Fog)foreach(var p in State.Spawns)marks.Spawns.Add(p.Y*8+p.X);
            // Tesis aşama 11: savunmada gelecek tur gelecek makinelerin tahmini kareleri (Core DefenseIncoming) aynı işaretle
            if(State.Phase=="player")foreach(var p in FacilityDefenseRaidText.IncomingTiles(State))marks.Spawns.Add(p.Y*8+p.X);
            // Sis: canlı durumdaki sisli kareler; önizlemede atılacak sis ve hamleden sonraki niyetler (sise kesilen, dinamitin açtığı hat)
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(State.Cells[y,x].Smoke)marks.Smoke.Add(y*8+x);
            if(preview!=null)foreach(var fx in preview.Fx.Where(f=>f.K=="smoke"))marks.SmokePreview.Add(fx.Y*8+fx.X);
            var shown=preview?.Intents!=null&&preview.Fx.Any(f=>f.K=="smoke"||f.K=="breach")?preview.Intents:Baskin.Intents(State);
            foreach(var i in shown)if(i.Smoked||i.Cloud)marks.SmokedArrows.Add((i.FromX,i.FromY,i.TargetX,i.TargetY,i.Kind,i.Cloud));
            foreach(var i in shown)if(i.HasTarget&&!i.TargetNone)
            {
                int key=i.TargetY*8+i.TargetX;
                if(i.Kind=="emp"||i.Kind=="blast"||i.Kind=="flame"||i.Kind=="frost"||i.Kind=="ram")
                    foreach(var a in i.Area)marks.Danger.Add(a.Y*8+a.X);
                if(i.Kind=="blast")continue;
                if(i.Kind=="repair"||i.Kind=="shield")marks.Heal.Add(key);else if(i.Kind=="clone"||i.Kind=="build"||i.Kind=="seal")marks.Clones.Add(key);else marks.Danger.Add(key);
                marks.Arrows.Add((i.FromX,i.FromY,i.TargetX,i.TargetY));
            }
            if(preview!=null)foreach(var fx in preview.Fx.Where(f=>f.K=="dmg"))marks.Danger.Add(fx.Y*8+fx.X);
            marks.Hazards=hazards;
            // Tesis odası (aşama 7): kurulu pusu hatları (kurulduğu kareden hattın son karesine; hat boşsa yönündeki ilk kare), yerdeki
            // yük, hamle kipinin odağı. Örnek tahtada üçü de boş
            if(State.Room!=null)
            {
                foreach(var u in State.Units.Where(u=>u.Side=='p'&&u.AmbushDir>=0))
                {
                    var line=Baskin.AmbushLine(State,u);var d=Baskin.Dirs[u.AmbushDir];
                    var last=line.Count>0?line[line.Count-1]:new Baskin.Pt(u.AmbushX+d[0],u.AmbushY+d[1]);
                    marks.Ambush.Add((u.AmbushX,u.AmbushY,last.X,last.Y,Baskin.OnBoard(u)));
                }
                if(State.CargoX>=0&&State.CargoY>=0)marks.Cargo=State.CargoY*8+State.CargoX;
                marks.Focus=FocusKey();
                GearMarks(marks);
                // Tesis aşama 12 (Katar; K39): GEÇ verilebilirken takip edecek makinelerde "TAKİP EDER" (acil); AYIR açılınca kalkar
                if(State.Phase=="player"&&Baskin.CanPass(State)&&!detach)foreach(var id in Baskin.FollowerIds(State))marks.Tags[id]=(FacilityConditionText.FollowTag,true);
            }
            return marks;
        }
        /// <summary>
        /// Tesis ekipmanının tahta işaretleri (aşama 9; tasarım §5.2): ayaktaki taret ve vincin hattı (FacilityGearView; Core'un kuralının
        /// kopyası), yemin bir sonraki evredeki yolu, havlayan köpeğin erimi; uzman olmayan birimlerin etiket adı (aygıtın kalan çalışması,
        /// KÖPEK, tutsağın kalan turu; son tur kırmızı); oyuncu turunda niyetsiz makinenin ve koklanan odanın ileri geçidinin kâğıt notu.
        /// Yalnız tesis odasında çağrılır; ekipmansız odada hepsi boş.
        /// </summary>
        void GearMarks(RaidBoard3D.Marks marks)
        {
            static int K(Baskin.Pt p)=>p.Y*8+p.X;
            foreach(var g in State.Units.Where(v=>Baskin.IsGadget(v)&&Baskin.OnBoard(v)))
            {
                marks.Tags[g.Id]=(FacilityGearText.DeviceTurnsText(g),false);
                if(g.Dir<0)continue;
                if(g.Type=="taret"){var (line,hit)=FacilityGearView.TurretLine(State,g.X,g.Y,g.Dir);if(line.Count>0)marks.Devices.Add((g.X,g.Y,line[line.Count-1].X,line[line.Count-1].Y,"taret",hit>=0?K(line[hit]):-1));}
                else if(g.Type=="vinc"){var (line,hit,_)=FacilityGearView.WinchLine(State,g.X,g.Y,g.Dir);if(line.Count>0)marks.Devices.Add((g.X,g.Y,line[line.Count-1].X,line[line.Count-1].Y,"vinc",hit>=0?K(line[hit]):-1));}
                else if(g.Type=="yem"){var path=FacilityGearView.DecoyPath(State,g.X,g.Y,g.Dir,1,g);if(path.Count>1)marks.DecoyPaths.Add(path.Select(K).ToArray());}
            }
            foreach(var d in State.Units.Where(v=>Baskin.IsDog(v)&&Baskin.OnBoard(v)))
            {
                marks.Tags[d.Id]=(Loc.T("KÖPEK"),false);
                if(d.Bark)marks.Bark.UnionWith(FacilityGearView.BarkArea(State,d.X,d.Y));
            }
            // Aşama 10 (K42): cıvatalı makinenin etiketi "CIVATALI" (kırmızı); sökülünce kalkar. Öbür önlemler bütün makinelere işler: etiket yok
            foreach(var e in State.Units.Where(v=>v.Side=='e'&&Baskin.OnBoard(v)))
            {var bolt=FacilityCounterText.BoltTag(State,e);if(bolt!=null)marks.Tags[e.Id]=(bolt,true);}
            foreach(var c in State.Units.Where(v=>Baskin.IsCaptive(v)&&Baskin.OnBoard(v)))
            {
                int left=FacilityGearText.CaptiveLeft(State,c);
                marks.Tags[c.Id]=(left<=1?Loc.T("SON TUR"):Loc.F("TUTSAK · {0}",left),left<=1);
            }
            // Niyetsiz makine (Core Intents onu listelemez): bırakılan tutsak ve kilitli makine bu planı niyetsiz geçirir (Intent yok; Core
            // planda Idle'ı siler): NİYETSİZ. Kilitlenen makine (Idle) bu turun niyetini yine uygular, sonraki planı atlar: KİLİTLİ (niyet
            // okuyla yan yana "niyetsiz" yazmasın). Oyuncu turunda kâğıt notu
            if(State.Phase=="player")foreach(var e in State.Units.Where(v=>v.Side=='e'&&Baskin.OnBoard(v)&&(v.Idle||v.Intent==null)))marks.Notes.Add((e.Y*8+e.X,e.Idle?Loc.T("KİLİTLİ"):Loc.T("NİYETSİZ")));
            if(State.Sniffed>0&&!State.Escape&&State.Room.Forward.Count>0)marks.Notes.Add((K(State.Room.Forward[State.Room.Forward.Count-1]),Loc.T("KOKLANDI")));
        }
        /// <summary>Hamle kipinin odak karesi: sürüklemede sürüklenecek gövde, iki kişilik işte üzerine gelinen iş karesi; yoksa −1.</summary>
        /// <summary>Köpeğin koklayacağı sıradaki odanın planı zaten biliniyor mu (aşama 9; önizleme satırı). Örnek tahtada false.</summary>
        bool NextKnown()
        {
            var rooms=Session?.Params?.Rooms;int next=State?.Room?.Index??0;
            return rooms!=null&&next>=1&&next<rooms.Count&&Session.Sim.FacilityKnown(rooms[next]);
        }
        int FocusKey()
        {
            if(!ActionArmed)return -1;
            if(actionKind==Baskin.ActDrag){var b=State.Units.FirstOrDefault(v=>v.Id==actionOther);return b!=null&&Baskin.LyingBody(b)?b.Y*8+b.X:-1;}
            if(actionKind==Baskin.ActJoint&&preview!=null&&hover.HasValue)return hover.Value.y*8+hover.Value.x;
            return -1;
        }
        static readonly (int,int,int,int)[] NoMoves=new (int,int,int,int)[0];
        void DrawPreview(RaidViewModel vm)
        {
            if(previewOverlay!=null)previewOverlay.Render(vm.CollisionTiles);
            board.ClearPreview();if(preview!=null)
            {
                var labels=preview.Fx.Where(f=>f.K=="dmg").GroupBy(f=>(f.X,f.Y)).Select(g=>((float)g.Key.X,(float)g.Key.Y,Loc.F("−{0}",g.Sum(f=>f.N)),DeskTheme.Active.Get("sade.status.threat"))).ToList();
                foreach(var fx in preview.Fx.Where(f=>f.K=="armor"||f.K=="break"&&f.Obj=="cool"))labels.Add((fx.X,fx.Y,fx.K=="armor"?Loc.T("ZIRH"):Loc.T("PATLAR"),DeskTheme.Active.Get("sade.status.threat")));
                var pushes=preview.Fx.Where(f=>f.K=="push").ToArray();
                var arrows=pushes.Select(f=>(f.X,f.Y,f.X+f.Dx,f.Y+f.Dy)).ToList();
                var blocked=new HashSet<int>(Enumerable.Range(0,pushes.Length).Where(i=>!preview.Moves.Any(m=>m.Unit==pushes[i].Unit)));
                foreach(var fx in preview.Fx.Where(f=>f.K=="die"||f.K=="fall"))board.SetIntentPresentation(fx.Unit,"cancel");
                // Tesis aşama 12 (Katar): kenardan düşecek birimin kâğıdı kenarın dışında yarım karede (kırmızı DÜŞER)
                var falls=preview.Fx.Where(f=>f.K=="fall").ToList();
                foreach(var fx in falls)labels.Add((fx.X+fx.Dx*.8f,fx.Y+fx.Dy*.8f,FacilityConditionText.FallsTag,DeskTheme.Active.Get("sade.status.threat")));
                // Tesis hamlesi (aşama 7): hamleye özgü çizgiler, hayaletler ve kâğıt etiketleri (FacilityMoveView; itme okları ve hasar
                // etiketleri bugünkü dalda)
                var actor=Unit();
                var move=Kip&&actor!=null&&hover.HasValue?FacilityMoveView.Preview(State,actor,KipKind,KipOther,hover.Value.x,hover.Value.y,preview,Rules,NextKnown()):null;
                if(move!=null)foreach(var l in move.Labels)labels.Add(((float)l.X,(float)l.Y,l.Text,DeskTheme.Active.Get(l.Color)));
                board.SetPreview(labels,arrows,blocked);
                foreach(var fx in falls)board.AddPreviewGhostAt(fx.Unit,fx.X+fx.Dx*.8f,fx.Y+fx.Dy*.8f);
                if(move!=null)
                {
                    foreach(var st in move.Strokes)board.AddPreviewStroke(st.Points.Select(p=>(p.X,p.Y)).ToList(),st.Color,st.Dashed,st.Arc,st.Tip);
                    foreach(var g in move.Ghosts)board.AddPreviewGhost(g.Unit,g.X,g.Y);
                    // Aşama 9: alan boyası (havlamanın erimi)
                    if(move.Area.Count>0)board.AddPreviewArea(move.Area);
                }
            }
            // Kaçışta çıkışa yürüyüşün önizlemesi: kâğıtta ÇIKAR, ok seçili uzmandan
            else if(hover.HasValue&&(State.Room==null||Baskin.IsFinalExit(State))&&State.Escape&&vm.Moves.Contains(hover.Value)&&State.Exits.Any(p=>p.X==hover.Value.x&&p.Y==hover.Value.y)&&Unit() is Baskin.Unit mover&&(Baskin.IsSpecialist(mover)||Baskin.IsDog(mover)&&Baskin.IsFinalExit(State)))
                board.SetPreview(new[]{((float)hover.Value.x,(float)hover.Value.y,Loc.T("ÇIKAR"),DeskTheme.Active.Get("ink.green"))},NoMoves);
            // Tesis odası: kapıya yürüyüşün önizlemesi (kâğıtta KAPIYA)
            else if(hover.HasValue&&State.Room!=null&&!Baskin.IsFinalExit(State)&&vm.Moves.Contains(hover.Value)&&Baskin.IsGateTile(State,hover.Value.x,hover.Value.y)&&Unit() is Baskin.Unit gateWalker&&Baskin.IsSpecialist(gateWalker))
                board.SetPreview(new[]{((float)hover.Value.x,(float)hover.Value.y,Loc.T("KAPIYA"),DeskTheme.Active.Get("ink.green"))},NoMoves);
            // Yürüyüş karesinde bölge olayının bu tur sonu ona yapacağı (kaynak, çökecek tavan, canlı kanal…)
            else if(hover.HasValue&&vm.Moves.Contains(hover.Value)&&Unit() is Baskin.Unit walker&&hazards?.Warnings.FirstOrDefault(w=>w.Unit==walker.Id) is Baskin.HazardWarn hw)
                board.SetPreview(new[]{((float)hover.Value.x,(float)hover.Value.y,hw.Damage>0?Loc.F("−{0}",hw.Damage):Loc.T("OLAY"),DeskTheme.Active.Get(hw.Damage>0?"sade.status.threat":"ink.ink2"))},NoMoves);
            // Yürüyüşün yolu (etiketli dallarda da düz ok yerine): SetPreview'den sonra, çünkü o önizlemeyi temizler
            if(preview==null&&vm.MovePath.Length>1)board.SetMovePath(vm.MovePath.Select(p=>(p.x,p.y)).ToList());
            board.InputEnabled=State.Phase=="player"&&!paused&&!Busy&&!retreat;
        }
        void Enrich(RaidViewModel vm,Baskin.BState display)
        {
            vm.LastTurn=display.Turn>=display.MaxTurns;vm.Finished=!Busy&&Over;
            vm.Queue=roster.Select(i=>new RaidQueueItem{Order=i.Order,State=queue.TryGetValue(i.Unit,out var q)?q:"pending"}).ToArray();
            vm.EventLine=eventLine;vm.EventVisible=executing||eventLine!=null&&Time.unscaledTime<eventUntil;
            // Tesiste önceki odaların satırları önce, son odanın kapanışı sonra (örnek tahtada ikisi de boş)
            vm.Log=chainLog.Concat(State.Log.Take(Math.Min(visibleLogs,State.Log.Count)).Select(l=>new RaidLogModel{Text=l.Text,Kind=l.Kind})).Concat(endLog).ToArray();
            if(FatalEnd)
            {
                vm.FatalWarning=string.Join(Loc.T("\n"),State.Units.Where(u=>Baskin.IsSpecialist(u)&&Baskin.OnBoard(u)).Select(u=>
                {
                    var equipment=EquipmentLabels(u);
                    return string.IsNullOrEmpty(equipment)?Loc.F("{0} geride kalırsa kalıcı olarak ölür.",u.PersonName??PersonalName(Baskin.UnitTypes[u.Type].Short[0])):
                        Loc.F("{0} geride kalırsa kalıcı olarak ölür.\n{1} kaybolur.",u.PersonName??PersonalName(Baskin.UnitTypes[u.Type].Short[0]),equipment);
                }));
                // Tesis odasında kapıdaki uzman güvende (örnek tahtada AtGate hep false)
                foreach(var t in vm.Team)if(!t.Down&&!t.Out&&!t.AtGate&&!t.Dog)t.Threat=Loc.T("GERİDE KALIRSA ÖLÜR");
                // Aşama 9: yükü taşıyan köpek tahtada kalırsa yük kaybedilir (örnek tahtada köpek yok)
                var cargoDog=State.Carrier!=0?State.Units.FirstOrDefault(v=>v.Id==State.Carrier&&Baskin.IsDog(v)&&Baskin.OnBoard(v)):null;
                if(cargoDog!=null)vm.FatalWarning=string.Join(Loc.T("\n"),new[]{vm.FatalWarning,Loc.T("Köpek yükle kalırsa yük kaybedilir")}.Where(l=>!string.IsNullOrEmpty(l)));
                // Tesis odası (aşama 7): yatan gövdeler de taşınmazsa ölür (örnek tahtada gövde yok)
                var down=LyingBodies().Select(b=>Loc.F("{0} yerde: taşınmazsa ölür.",string.IsNullOrEmpty(b.PersonName)?PersonalName(Baskin.UnitTypes[b.Type].Short[0]):b.PersonName)).ToList();
                if(down.Count>0)vm.FatalWarning=string.Join(Loc.T("\n"),new[]{vm.FatalWarning}.Concat(down).Where(l=>!string.IsNullOrEmpty(l)));
            }
            // Tesis aşama 11: savunmada içeri çekilmenin onayı (ilk basış) ve son tur (süre dolunca kanat tutulur); barikat turunun fişi
            if(vm.Defense)
            {
                if(confirm&&Baskin.CanPass(State)){vm.ConfirmTitle=FacilityDefenseRaidText.PassTitle;vm.FatalWarning=FacilityDefenseRaidText.PassWarning(RoomTrust,LyingBodies().Select(b=>string.IsNullOrEmpty(b.PersonName)?PersonalName(Baskin.UnitTypes[b.Type].Short[0]):b.PersonName));}
                else if(DefenseLastTurn){vm.ConfirmTitle=FacilityDefenseRaidText.LastTurnTitle;vm.FatalWarning=FacilityDefenseRaidText.LastTurnWarning;}
                if(Baskin.BarricadeTurn(State)&&State.Phase=="player"&&!executing&&!passing&&!vm.EventVisible){vm.EventLine=FacilityDefenseRaidText.BarricadeSlip;vm.EventVisible=true;}
            }
            // Tesisin giriş ve ara odasında son tur: süre dolunca ekip geri çekilir (kartlara ölüm uyarısı yazılmaz; örnek tahtada hiç)
            if(TimeoutEnd&&State.Units.Any(u=>Baskin.IsSpecialist(u)&&!u.Dead&&u.AtGate)){vm.ConfirmTitle=Loc.T("SON TUR · KAPIDAKİLER GEÇER");vm.FatalWarning=Loc.T("Süre dolunca kapıdakiler geçer; kapıya varamayan uzman geri çekilir ve baskına dönmez.");}
            else if(TimeoutEnd){vm.ConfirmTitle=Loc.T("SON TUR · GERİ ÇEKİLME");vm.FatalWarning=Loc.T("Süre dolunca ekip geri çekilir ve baskın başarısız biter. Kapıdakiler de döner.");
                if(LyingBodies().Any())vm.FatalWarning=Loc.F("{0} {1}",vm.FatalWarning,Loc.T("Yerde yatanlar ölür."));}
            if(confirm)
            {
                vm.Unused=State.Units.Where(u=>Baskin.IsSpecialist(u)&&Baskin.OnBoard(u)&&(!u.Moved||!u.Acted)).Select(u=>new RaidUnusedModel{Letter=Baskin.UnitTypes[u.Type].Short[0],Name=PersonalName(Baskin.UnitTypes[u.Type].Short[0]),Move=!u.Moved,Ability=!u.Acted,
                    ThreatOrder=Baskin.Intents(State).FirstOrDefault(i=>Threatens(i,u.X,u.Y))?.Order??0}).ToArray();
                foreach(var t in vm.Team)t.Warn=!t.Dog&&vm.Unused.Any(u=>u.Letter==t.Letter);
            }
            if(vm.Finished)
            {
                if(finishedAt<0)finishedAt=Time.unscaledTime;
                if(Session!=null){if(vm.Defense)DefenseCard(vm);else ChainCard(vm);return;}
                // Revizyon 2 · S7: damga tek ve başlık yerinde (başarıda SEKTÖR GERİ ALINDI); altında yer satırı ve üç satır
                var r=Baskin.Result(State);bool loot=Baskin.IsLoot(State);vm.Success=r.Success;
                vm.ResultTitle=r.Success?Loc.T("SEKTÖR GERİ ALINDI"):r.Retreat?Loc.T("GERİ ÇEKİLDİ"):State.Units.Where(Baskin.IsSpecialist).All(u=>u.Dead)?Loc.T("EKİP DÜŞTÜ"):loot&&Baskin.TargetsLeft(State).Count==0?Loc.T("İKMAL KAYBEDİLDİ"):Loc.T("SÜRE DOLDU");
                vm.ResultTurn=Loc.F("TUR {0}/{1}",r.Turns,State.MaxTurns);vm.ResultPlace=ResultPlace;
                // Aşama 9: sayılar uzmanlardan (Core sonucu uzmanla sayar; örnek tahtada her Side 'p' uzman)
                int standing=State.Units.Count(u=>Baskin.IsSpecialist(u)&&!u.Dead),gone=State.Units.Count(u=>Baskin.IsSpecialist(u)&&u.Out&&!u.Dead),team=State.Units.Count(Baskin.IsSpecialist);
                // Kaçış olduysa ekip satırı çıkanları sayar (çıkamayan düşmüş sayılır)
                var squad=State.Escape?new RaidResultRow{Label=Loc.T("Tahliye edilen"),Value=Loc.F("{0}/{1}",gone,team),Good=gone==team}:new RaidResultRow{Label=Loc.T("Ekip"),Value=Loc.F("{0}/{1} ayakta",standing,team)};
                vm.ResultRows=vm.Targets.Select(t=>new RaidResultRow{Label=t.Name,Value=t.Broken?(loot&&r.Success?Loc.T("alındı"):loot&&vm.CargoLost?Loc.T("kayboldu"):Loc.T("kırıldı")):loot?Loc.T("geride kaldı"):Loc.F("{0}/{1}",t.Hp,t.Max),Good=t.Broken&&r.Success})
                    .Append(new RaidResultRow{Label=Loc.T("Kırılan sivil sunucu"),Value=display.Collateral.ToString()}).Append(squad).ToArray();
                // Spot sonucun nedenini açıkta bırakır: hedef (alınan ya da kırılan kasa, dolap)
                if(targetForms.Count>0){var t=targetForms[0];vm.Spot=new Vector3(t.x,t.y,r.Success?.15f:.45f);}
            }
        }
        /// <summary>Önceki bir tesis odasında düşen ya da çıkamayan uzmanın kartı (tahtada yok); örnek tahtada ve ayaktakinde null.</summary>
        RaidTeamModel FallenCard(char letter)
        {
            var squad=Session!=null?State.Params?.Squad:null;if(squad==null)return null;
            int slot=squad.FindIndex(s=>s.Type!=null&&Baskin.UnitTypes.TryGetValue(s.Type,out var t)&&t.Short[0]==letter);if(slot<0)return null;
            var m=Session.Member(slot);
            var traitor=State.Units.FirstOrDefault(u=>Baskin.IsTurncoat(u)&&u.Slot==slot);
            if(traitor!=null||m?.FieldTurned==true)
                return new RaidTeamModel{Letter=letter,Name=squad[slot].PersonName,Thumb="thumb_standee_"+letter+"_done",Down=true,
                    DownText=Loc.T("TARAF DEĞİŞTİRDİ")};
            if(m==null||m.State=="a")return null;
            string name=string.IsNullOrEmpty(squad[slot].PersonName)?Name(letter):Loc.F("{0} · {1}",squad[slot].PersonName,Baskin.UnitTypes[squad[slot].Type].Name);
            return new RaidTeamModel{Letter=letter,Name=name,Thumb="thumb_standee_"+letter+"_done",Down=true,Left=m.State=="l",DownText=m.State=="w"?Loc.T("GERİ ÇEKİLDİ"):m.State=="k"?FacilityConditionText.CardOverboard(m.Fell):m.Body==2?Loc.T("ÇIKARILDI"):m.State=="d"?Loc.F("DÜŞTÜ · PAFTA {0}",m.Fell):null};
        }
        /// <summary>Tesis baskınının sonuç kartı: üç odanın toplamı (raporlardan), son pafta, hedef odasının satırları.</summary>
        void ChainCard(RaidViewModel vm)
        {
            var r=Session.Result();var reps=Session.Filed;if(r==null||reps.Count==0)return;
            var last=reps[reps.Count-1];var hRep=reps.FirstOrDefault(x=>x.Room==3&&!x.Return);bool loot=Baskin.IsLoot(State);
            vm.Success=r.Success;
            vm.ResultTitle=r.Success?Loc.T("SEKTÖR GERİ ALINDI"):r.Retreat?Loc.T("GERİ ÇEKİLDİ"):last.Team.All(m=>m.State!="a")?Loc.T("EKİP DÜŞTÜ")
                :loot&&hRep!=null&&!hRep.Objects.Any(o=>Baskin.IsTarget(o.K))?Loc.T("İKMAL KAYBEDİLDİ"):Loc.T("SÜRE DOLDU");
            vm.ResultTurn=Loc.F("TUR {0} · PAFTA {1}/3",r.Turns,reps.Max(x=>x.Room));vm.ResultPlace=ResultPlace;
            IEnumerable<RaidResultRow> rows;
            if(targetCache!=null&&targetCache.Length>0)
                rows=targetCache.Select((t,i)=>new RaidResultRow{Label=targetCacheKeys!=null&&i<targetCacheKeys.Length?Loc.T(targetCacheKeys[i]):t.Name,Value=t.Broken?(loot&&r.Success?Loc.T("alındı"):loot&&(hRep!=null&&hRep.Cargo>=0||last.Cargo==-2)?Loc.T("kayboldu"):Loc.T("kırıldı")):loot?Loc.T("geride kaldı"):Loc.F("{0}/{1}",t.Hp,t.Max),Good=t.Broken&&r.Success});
            else rows=new[]{new RaidResultRow{Label=loot?Loc.T("İkmal kasası"):Loc.T("Hedef dolap"),Value=hRep==null?Loc.T("ulaşılamadı"):"—"}};
            int a=last.Team.Count(m=>m.State=="a"),n=last.Team.Count;
            bool escaped=reps.Any(x=>x.Return)||hRep?.End=="pass"||State.Escape;
            // Tesisten çıkarılan gövdeler ekip satırının değerinde (kartta dört satır yeri var)
            int carried=last.Team.Count(m=>m.Body==2);
            var squad=escaped?new RaidResultRow{Label=Loc.T("Tahliye edilen"),Value=carried>0?Loc.F("{0}/{1} · {2} çıkarıldı",a,n,carried):Loc.F("{0}/{1}",a,n),Good=a==n}:new RaidResultRow{Label=Loc.T("Ekip"),Value=Loc.F("{0}/{1} ayakta",a,n)};
            // Tesis aşama 12 (Katar): kenardan düşenler ekip satırının ekinde ("· 1 vagondan düştü")
            if(FacilityConditionText.ResultOverboard(last.Team.Count(m=>m.State=="k")) is string overboard)squad.Value=Loc.F("{0} {1}",squad.Value,overboard);
            // Süre dolarken kapıya varamayan ve sağ dönenler ekip satırının ekinde
            int withdrew=last.Team.Count(m=>m.State=="w");if(withdrew>0)squad.Value=Loc.F("{0} · {1} geri çekildi",squad.Value,withdrew);
            vm.ResultRows=rows.Append(new RaidResultRow{Label=Loc.T("Kırılan sivil sunucu"),Value=r.Collateral.ToString()}).Append(squad).ToArray();
            // Spot: hedef odasında hedef, öbür odalarda geçit (sonucun yeri)
            if(State.Room.Type==RoomType.Target&&targetForms.Count>0){var t=targetForms[0];vm.Spot=new Vector3(t.x,t.y,r.Success?.15f:.45f);}
            else{var at=Baskin.GateTiles(State).Concat(State.Room.West).FirstOrDefault();vm.Spot=new Vector3(at.X,at.Y,.45f);}
        }
        /// <summary>
        /// Tesis aşama 11: savunmanın sonuç kartı (raporlardan ve son tahtadan): KANAT TUTULDU / RÖLE KIRILDI / KANAT DÜŞTÜ, “TUR 10/10 ·
        /// PAFTA 2/3”, satırlar saha rölesi, terk edilen oda, kırılan sivil sunucu, ekip (“3/3 ayakta · 1 çıkarıldı”). Spot röle ya da batı girişi.
        /// </summary>
        void DefenseCard(RaidViewModel vm)
        {
            var r=Session.Result();var reps=Session.Filed;if(r==null||reps.Count==0)return;
            var last=reps[reps.Count-1];
            var relay=targetForms.FirstOrDefault(t=>t.kind==Baskin.RelayKind);
            bool reached=State.Room.Type==RoomType.Target&&relay.kind!=null;
            int hp=reached?Baskin.RelayHp(State):-1,max=reached?relay.max:Session.Sim.DefenseRelayHpFor(State.Params.Perks);
            vm.Success=r.Success;
            vm.ResultTitle=FacilityDefenseRaidText.ResultTitle(r.Success,reached&&hp<0);
            vm.ResultTurn=FacilityDefenseRaidText.ResultTurn(r.Turns,State.MaxTurns,reps.Max(x=>x.Room));vm.ResultPlace=ResultPlace;
            vm.ResultRows=FacilityDefenseRaidText.ResultRows(r.Success,hp,max,reached,RaidChain.Abandoned(State.Params,reps),RoomTrust,r.Collateral,
                last.Team.Count(m=>m.State=="a"),last.Team.Count,r.Extracted.Count);
            if(reached)vm.Spot=new Vector3(relay.x,relay.y,r.Success?.15f:.45f);
            else{var at=State.Room.West.FirstOrDefault();vm.Spot=new Vector3(at.X,at.Y,.45f);}
        }
        string PersonalName(char letter)
        {
            var person = State?.Units.FirstOrDefault(u => Baskin.IsSpecialist(u) && Baskin.UnitTypes[u.Type].Short == letter.ToString());
            return string.IsNullOrEmpty(person?.PersonName) ? Name(letter) : Loc.F("{0} · {1}", person.PersonName, Baskin.UnitTypes[person.Type].Name);
        }
        static string PersonalAbilityRange(Baskin.Unit unit, bool loot)
        {
            string ability = Baskin.UnitTypes[unit.Type].Ability;
            string range = AbilityRange(ability, unit.Charges);
            if (string.IsNullOrEmpty(unit.PersonId) && unit.Power <= 1 && unit.EmpBoosts <= 0) return range;
            if (ability == "sabotaj") return loot ? Loc.F("{0} · kasadan ikmal alır, robota {1} hasar", range, Math.Max(1, unit.Power))
                : Loc.F("{0} · robota {1} hasar, dolaba 2", range, Math.Max(1, unit.Power));
            if (ability == "emp") return Loc.F("{0} · hedefe {1} hasar · güçlendirme {2}", range, 1 + (unit.EmpBoosts > 0 ? Math.Max(0, unit.EmpBoostDamage) : 0), Math.Max(0, unit.EmpBoosts));
            return range;
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
        static bool Threatens(Baskin.IntentView i,int x,int y)=>i.HasTarget&&!i.TargetNone&&i.Kind!="repair"&&i.Kind!="shield"&&i.Kind!="clone"&&i.Kind!="build"&&i.Kind!="seal"&&
            (i.Area.Count>0?i.Area.Any(a=>a.X==x&&a.Y==y):i.TargetX==x&&i.TargetY==y);
        static string IntentName(string kind)=>kind switch{"emp" or "kanca" or "sabotaj"=>AbilityName(kind,false),"repair"=>Loc.T("onarım"),"melee"=>Loc.T("vuruş"),"lob"=>Loc.T("havan"),"blast"=>Loc.T("patlama"),
            "lock"=>Loc.T("kilit"),"shield"=>Loc.T("kalkan"),"clone"=>Loc.T("çoğaltma"),"flame"=>Loc.T("alev"),"ram"=>Loc.T("koşu darbesi"),"pull"=>Loc.T("çekiş"),
            "seal"=>Loc.T("kalas kurma"),"build"=>Loc.T("dron montajı"),"frost"=>Loc.T("kırağı"),_=>Loc.T("ateş")};
        /// <summary>Havada olan makineler dron resmiyle, yerdekiler bakım robotu resmiyle.</summary>
        /// <summary>Nesnenin küçük resmi: dolaplar, soğutma ünitesi ve olay nesneleri (tüp, kaynak kolu kaidesi, tavan enkazı) kendi
        /// prefab'larından çizilir (SiginakTextureBaker.PieceThumbs).</summary>
        static string ObjectThumb(string kind)=>FacilityGearText.ObjectThumb(kind)??kind switch{"cool"=>"thumb_cooling","pano" or "anten" or "vana"=>"thumb_cooling","tup"=>"thumb_tube","kol"=>"thumb_weld","enkaz"=>"thumb_debris","civ"=>"thumb_civ","spawner"=>"thumb_spawner",Baskin.RelayKind=>"thumb_relay",_=>"thumb_cabinet_"+kind};
        static string EnemyThumb(string type)=>type switch{"bakim"=>"thumb_maint","zdron"=>"thumb_armordrone","kalkan"=>"thumb_shielddrone","havan"=>"thumb_mortar","bomba"=>"thumb_bomber","kilit"=>"thumb_locker","cogalt" or "montaj"=>"thumb_replicator","alev"=>"thumb_mortar",
            "muhur" or "capa"=>"thumb_locker","tampon"=>"thumb_maint","kirag"=>"thumb_shielddrone",_=>"thumb_drone"};
        /// <summary>Bölge olayı karesinin hover satırı (<see cref="Baskin.HazardMark.Kind"/>); Bad: üstünde durana zarar verebilir.</summary>
        static (string Text,bool Bad) HazardRow(string kind)=>kind switch
        {
            "cloud"=>(Loc.T("Söndürme bulutu: buraya ve buradan nişan alınmaz"),false),"cloud_next"=>(Loc.T("Bu tur sonunda bulut dolar"),false),"nozzle"=>(Loc.T("Söndürme memesi"),false),
            "rail"=>(Loc.T("Arşiv rafı: kayar duvar"),false),"rail_gap"=>(Loc.T("Rafın geçidi"),false),"rail_close"=>(Loc.T("Bu tur sonunda raf kapanır: 1 hasar ve itme"),true),"rail_open"=>(Loc.T("Bu tur sonunda geçit açılır"),false),
            "gas"=>(Loc.T("Yanıcı gaz: ateş değerse bir tur sonra parlar"),false),"gas_next"=>(Loc.T("Bu tur sonunda gaz dolar"),false),"gas_lit"=>(Loc.T("Alev aldı: parlayınca 1 hasar"),true),"leak"=>(Loc.T("Gaz sızıntısı"),false),
            "crack"=>(Loc.T("Çatlak tavan: yanındaki darbe çökertir"),false),"crack_due"=>(Loc.T("Tavan çökecek: altındakine 1 hasar, boşsa enkaz"),true),
            "stop"=>(Loc.T("Asansör durağı: kabin kalkınca üstündeki yolculuk eder"),false),"stop_cab"=>(Loc.T("Kabin bu durakta"),false),
            "frost"=>(Loc.T("Buzlu kare: yürüyüş burada biter"),true),"freeze"=>(Loc.T("Kriyo bölgesi"),false),"freeze_next"=>(Loc.T("Bu tur sonunda donar: yürüyemez, buz 1 hasar emer"),true),"hol"=>(Loc.T("Turnike holü: kenarlar tek yöne döner"),false),
            "weld"=>(Loc.T("Bu evre kaynak: 1 hasar"),true),"weld_next"=>(Loc.T("Sıradaki kaynak karesi"),false),
            "live"=>(Loc.T("Canlı kanal: evre sonunda 1 hasar"),true),"dead"=>(Loc.T("Sönük kanal: güvenli"),false),
            "drain"=>(Loc.T("Süzgeç: 3. ve 6. tur sonunda çevresini çeker"),false),"drain_next"=>(Loc.T("Bu tur sonunda süzgeç çeker"),true),
            "tube_hot"=>(Loc.T("Tüp ıslık çalıyor: çizili hattında uçacak"),true),
            "oil"=>(Loc.T("Yağ: itilen, çekilen ya da vurulan kayar"),false),"oil_next"=>(Loc.T("2. tur sonunda yağ dökülür"),false),"hose"=>(Loc.T("Hidrolik hortum"),false),
            // Tesis aşama 12 (Yalpa): yokuş aşağı kenarın etiket karesi
            "tilt"=>(Loc.T("Bu tur sonunda zemin yatar: sabitlenmemiş her şey 1 kare kayar"),true),"tilt_next"=>(Loc.T("Sonraki tur sonunda zemin yatar"),false),
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
            // Tesis odası: pafta ve odanın adı (örnek tahtada Room null)
            if(State.Room!=null)parts.Add(Loc.F("Pafta {0}/3 · {1}",State.Room.Index,State.Room.Name));
            var trait=p?.Traits?.FirstOrDefault(t=>Baskin.RegionTraits.ContainsKey(t));
            if(trait==null){parts.Add(loot?Loc.T("Bir uzmanla ikmal kasasına yaklaş. Q ile yeteneği seç, kasaya tıkla ve malzemeleri al. Kasayı imha etme."):Loc.T("Her tur yeni robotlar uyanabilir."));return string.Join("\n",parts);}
            parts.Add(Loc.F("{0}: {1}",Loc.T(Baskin.RegionTraits[trait].Name),Loc.T(Baskin.RegionTraits[trait].Desc)));
            // Bölge olayı (tur şeridinde altı çizili turlar olayın işlediği turlar)
            if(State.Ev!=null)parts.Add(Loc.F("Olay · {0}: {1}",Loc.T(Baskin.RegionEvents[State.Ev.Id].Name),Loc.T(Baskin.RegionEvents[State.Ev.Id].Desc)));
            // Makinenin ne yaptığı ipucu kartında (üstüne gel); burada yalnız adı
            if(State.Signature!=null)parts.Add(Loc.F("İmza makinesi: {0}",Baskin.UnitTypes[State.Signature].Name));
            if(p!=null)parts.Add(TierText(p.Tier));
            return string.Join("\n",parts);
        }
        /// <summary>Zorluk kademesi cümlesi (görev çipi, görev fişi). Payda yazılmaz: harekâtın ulaşabildiği en yüksek kademe
        /// dengeye bağlıdır (<c>Simulation.MaxTier</c>; haritadaki “TEHDİT n/m” onu okur).</summary>
        public static string TierText(int tier)=>tier switch
        {
            1=>Loc.F("Tehdit {0}: makineler alışılmış düzende.",1),
            2=>Loc.F("Tehdit {0}: başta bir makine fazla.",2),
            3=>Loc.F("Tehdit {0}: iki makine fazla, turda iki uyanma.",3),
            _=>Loc.F("Tehdit {0}: üç makine fazla, turda iki uyanma, canları +1.",4),
        };
    }
}
