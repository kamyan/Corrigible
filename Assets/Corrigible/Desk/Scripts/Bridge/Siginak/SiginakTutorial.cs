using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Rehberli ilk vardiyalar: yeni harekâtta (eğitim daha önce bitmediyse) analistin notunu gösterir ve
    /// <see cref="TutorialFlow"/>'u masa, baskın tahtası, raporlar ve sığınağın hâliyle ilerletir. Simülasyonu yalnız okur.
    /// Odanın kökünün dışında durur: baskın sahnesi yüklüyken de çalışır. Bitirilen ya da atlanan eğitim bir daha açılmaz
    /// (PlayerPrefs; editörde Corrigible › Eğitim › Eğitimi sıfırla). Kayıttan devam eden oyuna eğitim dayatılmaz; yalnız kartlı
    /// rehberle başlamış aynı harekât (aynı tohum, adım PlayerPrefs'te) rehberi kaldığı adımdan sürdürür (<c>Resume</c>).
    /// <para>
    /// KOS aşama 8: kartlı rehber ilk vardiyadan sonra da sürer (hedef satırı, ilk olay tutanağı, ilk aday grubu, İFŞA, son şalter;
    /// bkz. <see cref="PlanningTutorialFlow"/>). Grup, tutanak ve kararlar simülasyondan ve kayıttan okunur; tutanağın elde olduğu
    /// olay sunumundan (<c>shelterEvent</c>). Rehber sürerken son şalter mühürlüdür (<c>SiginakLastSwitch</c> <see cref="Active"/>'i okur).
    /// </para>
    /// </summary>
    public sealed class SiginakTutorial : MonoBehaviour
    {
        public const string DoneKey = "Corrigible.TutorialDone";

        [SerializeField] GameController game;
        [SerializeField] SiginakBriefing briefing;
        [SerializeField] SiginakMap map;
        [SerializeField] SiginakShift shift;
        [SerializeField] SiginakOperation operation;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] RaidLauncher launcher;
        [SerializeField] TutorialNoteView note;

        [SerializeField] PlanningBoardPresenter planningBoard;
        [SerializeField] SiginakShiftClose shiftClose;
        [Tooltip("KOS aşama 8: olay tutanağı elde mi (rehberin \"satır seç\" adımı).")]
        [SerializeField] SiginakShelterEvent shelterEvent;
        [Tooltip("2026-11-24: adımın hedefini (sahnedeki TutorialAnchor, pano cebi, baskın karesi) çerçeveleyen işaretçi.")]
        [SerializeField] TutorialPointerView pointer;
        [Tooltip("Pano ceplerini ve masadaki 3B hedefleri izdüşüren kamera (masa kamerası).")]
        [SerializeField] Camera deskCamera;
        public const string PlanningStepIdKey="Corrigible.PlanningTutorialStepId";
        public const string FirstNotesKey="Corrigible.PlanningTutorialFirstNotes";
        /// <summary>Gösterilmekte olan "ilk kez" notu (yoksa null) ve görülenler (PlayerPrefs; kayda yazılmaz).</summary>
        PlanningTutorialStep firstNote;
        readonly System.Collections.Generic.HashSet<string> firstSeen=new System.Collections.Generic.HashSet<string>();
        /// <summary>Rehberin ilk baskını bitene dek vardiya şalteri mühürlüdür (2026-11-24: ilk baskın atlanmasın); EĞİTİMİ BİTİR açar.</summary>
        public bool SealsShift=>planningFlow!=null&&!planningFlow.Finished&&(game.Sim?.S.RaidLog.Count??1)==0&&planningFlow.Index<PlanningTutorialFlow.IndexOf("rest");
        /// <summary>Gösterilen "ilk kez" notunun kimliği (testler).</summary>
        public string FirstNoteId=>firstNote?.Id;
        public TutorialPointerView Pointer=>pointer;
        public const string PlanningDoneKey="Corrigible.PlanningTutorialDone";
        public const string PlanningSeedKey="Corrigible.PlanningTutorialSeed";
        public const string PlanningStepKey="Corrigible.PlanningTutorialStep";
        PlanningTutorialFlow planningFlow;
        PlanningTutorialContext planningContext=new PlanningTutorialContext();
        bool loaded,planningSaved;
        // Kayıt yalnız büyür (komut silinmez): yeni komutlar bir kez taranır; her karede bütün kaydı taramaz.
        GameRecord scannedRecord;int scannedCommands;bool cookAssigned,eventDecided;
        public static bool PlanningDone{get=>PlayerPrefs.GetInt(PlanningDoneKey,0)==1;set{PlayerPrefs.SetInt(PlanningDoneKey,value?1:0);PlayerPrefs.Save();}}
        public string CurrentStepId=>planningFlow!=null?planningFlow.Current?.Id:flow?.Current?.Id;
        TutorialFlow flow;
        RaidDataSource raid;
        readonly TutorialContext context = new TutorialContext();

        public bool Active => planningFlow!=null?!planningFlow.Finished:flow!=null&&!flow.Finished;

        public static bool Done
        {
            get
            {
                try
                {
                    return PlayerPrefs.GetInt(DoneKey, 0) == 1;
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    PlayerPrefs.SetInt(DoneKey, value ? 1 : 0);
                    PlayerPrefs.Save();
                }
                catch
                {
                }
            }
        }

        void OnEnable()
        {
            game.NewGameStarted += Begin;
            game.GameLoaded += Resume;
            note.OkClicked += Ok;
            note.SkipClicked += Skip;
            DeskLoc.Instance.Changed += Show;
        }

        void OnDisable()
        {
            game.NewGameStarted -= Begin;
            game.GameLoaded -= Resume;
            note.OkClicked -= Ok;
            note.SkipClicked -= Skip;
            DeskLoc.Instance.Changed -= Show;
        }

        // Oyun GameController.Awake'te kurulur; olay bu bileşen dinlemeye başlamadan geçmiş olabilir
        System.Collections.IEnumerator Start()
        {
            // Menüden devam kararı ve GameLoaded, bütün Start çağrılarında işlensin.
            yield return null;
            var sim = game.Sim;
            if (!loaded&&flow==null&&planningFlow==null&&sim!=null&&sim.S.Day==0&&(sim.Board!=null||(sim.Record?.commands.Count??0)==0)) Begin();
        }

        void Begin()
        {
            loaded=false;planningSaved=false;
            if(game.Sim?.Board!=null)
            {
                flow=null;planningContext=new PlanningTutorialContext();planningFlow=PlanningDone?null:new PlanningTutorialFlow();
                firstNote=null;firstSeen.Clear();if(planningFlow!=null){PlayerPrefs.DeleteKey(FirstNotesKey);PlayerPrefs.DeleteKey(PlanningStepIdKey);}
                note.Hide();pointer?.Hide();return;
            }
            planningFlow=null;
            flow = Done ? null : new TutorialFlow();
            if (flow == null) note.Hide();
        }

        void Resume()
        {
            loaded=true;flow=null;planningFlow=null;note.Hide();
            // Eski oyuncuya eğitim dayatılmaz; yalnız bu rehberle başlayan aynı harekât devam eder.
            var sim=game.Sim;
            if(sim?.Board==null||PlanningDone||!PlayerPrefs.HasKey(PlanningStepKey)||PlayerPrefs.GetString(PlanningSeedKey)!=sim.S.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture))return;
            // Adım kimlikle saklanır (adım eklenince dizin kayar); eski kayıtta yalnız dizin vardır
            int at=PlayerPrefs.HasKey(PlanningStepIdKey)?PlanningTutorialFlow.IndexOf(PlayerPrefs.GetString(PlanningStepIdKey)):-1;
            planningContext=new PlanningTutorialContext();planningFlow=new PlanningTutorialFlow(at>=0?at:PlanningTutorialFlow.FromLegacyIndex(PlayerPrefs.GetInt(PlanningStepKey)));planningSaved=true;
            firstNote=null;firstSeen.Clear();
            foreach(var id in PlayerPrefs.GetString(FirstNotesKey,"").Split(','))if(id.Length>0)firstSeen.Add(id);
        }
        void SaveProgress()
        {
            if(planningFlow==null)return;
            planningSaved=true;
            PlayerPrefs.SetString(PlanningSeedKey,game.Sim.S.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(PlanningStepKey,planningFlow.Index);
            if(planningFlow.Current!=null)PlayerPrefs.SetString(PlanningStepIdKey,planningFlow.Current.Id);
            PlayerPrefs.SetString(FirstNotesKey,string.Join(",",firstSeen));PlayerPrefs.Save();
        }
        void UpdatePlanning()
        {
            var sim=game.Sim;var c=planningContext;
            bool raidLoaded=RaidLauncher.IsRaidLoaded;
            if(!raidLoaded)raid=null;else if(raid==null)raid=FindFirstObjectByType<RaidDataSource>();
            c.Blocked=sim==null||sim.S.Ending!=null||game.StoryHold||briefing.Busy||shift.Playing||
                launcher!=null&&launcher.IsTransitioning||SettingsCabinet.Instance!=null&&SettingsCabinet.Instance.IsOpen||
                FinalReportView.Instance!=null&&FinalReportView.Instance.IsOpen||!raidLoaded&&rig!=null&&rig.InTransition||shiftClose!=null&&shiftClose.Filing;
            c.BoardOpen=planningBoard!=null&&planningBoard.IsOpen;
            c.PersonSelected|=planningBoard!=null&&planningBoard.SelectedPersonId!=null;
            ScanRecord(sim);
            c.AssignedCook=cookAssigned;
            c.SlipOpen=map!=null&&map.SlipOpen;c.RaidOrdered=sim?.S.Raid!=null;c.RaidLoaded=raidLoaded&&raid?.State!=null;
            var state=c.RaidLoaded?raid.State:null;
            // Tesis baskınında yürüyerek çıkışta son evre "pass" kalır: sonuç kartı (Finished) da bitiş sayılır.
            c.RaidOver=state!=null&&(state.Phase=="won"||state.Phase=="lost")||raid?.Current?.Finished==true;
            if(state!=null)
            {
                c.RaidSelected|=raid.Selected!='\0';c.RaidMoved|=state.Units.Any(u=>u.Side=='p'&&u.Moved);
                c.RaidActed|=state.Units.Any(u=>u.Side=='p'&&u.Acted);c.RaidTurnEnded|=state.Turn>1;
                // İlk baskının ilerleyişi durumdan (devamda oda baştan açılır; mandal değil): kapıda biri, pafta, kaçış, bitiş
                int sheet=state.Room?.Index??1;
                int progress=c.RaidOver?5:state.Escape?4:sheet>=3?3:sheet>=2?2:state.Units.Any(u=>Baskin.IsSpecialist(u)&&u.AtGate)?1:0;
                c.RaidProgress=Mathf.Max(c.RaidProgress,progress);
                bool last=state.Phase=="player"&&state.Turn>=state.MaxTurns&&state.MaxTurns>1;
                bool hard=state.Escape||state.Room?.Type==RoomType.Target;
                c.RaidLastTurn=last&&!hard;c.RaidLastTurnHard=last&&hard;
                // Oyunda düşen (süre dolunca geride kalan ve kenardan düşen değil)
                c.RaidDown=state.Units.Any(u=>Baskin.IsSpecialist(u)&&u.Dead&&!u.Left&&!u.Overboard);
                c.RaidWake=state.Phase=="player"&&state.Spawns.Count>0;
                c.RaidHazard=state.Ev!=null;
                c.RaidCarrier=state.Carrier!=0;
            }
            else{c.RaidLastTurn=c.RaidLastTurnHard=c.RaidDown=c.RaidWake=c.RaidHazard=c.RaidCarrier=false;if(!c.RaidOrdered)c.RaidProgress=0;}
            c.SquadOpen=map!=null&&map.SquadReportOpen;
            c.ReportOpen=operation!=null&&operation.ReportOpen;c.Printed=sim?.Board?.Printed==true;
            c.Raids=sim?.S.RaidLog.Count??0;c.Day=sim?.S.Day??0;
            // KOS aşama 8: aday grubu ve olay tutanağı (yalnız okunur; "bir kez yeter" bilgisi kayıttan türetilir, devamda da doğru)
            var adm=sim?.Admission;
            c.GroupWaiting=adm!=null&&adm.Group.Count>0;
            c.GroupDone=adm==null||adm.Passed.Count>0||adm.Files.Any(f=>!f.Founder);
            c.AtCounter=rig!=null&&(rig.Current==SiginakPose.Admission||rig.Current==SiginakPose.AdmissionRead);
            c.EventOpen=sim?.Board!=null&&sim.PendingShelterEvent!=null;
            c.EventInHand=shelterEvent!=null&&shelterEvent.View!=null&&shelterEvent.View.InHand;
            c.EventDone=sim?.ShelterEvents==null||!sim.B.shelterEventsEnabled||eventDecided;
            if(!c.Blocked&&!planningSaved)SaveProgress();
            if(planningFlow.Update(c))SaveProgress();
            if(planningFlow.Finished){Finish();return;}
            // Baskın bitti ya da kapandı: açık "ilk kez" notu masaya taşınmaz (görülmedi sayılır)
            if(firstNote!=null&&(!c.RaidLoaded||c.RaidOver))firstNote=null;
            // "İlk kez" notu: rehber sürerken baskında bir durum ilk kez görülünce (bir kez; SONRAKİ kapatır)
            if(firstNote==null&&!c.Blocked&&c.RaidLoaded&&!c.RaidOver&&state?.Phase=="player"&&planningFlow.Index<PlanningTutorialFlow.GoalIndex)
                firstNote=PlanningTutorialFlow.FirstNotes.FirstOrDefault(n=>!firstSeen.Contains(n.Id)&&n.Topic(c));
            // Konu bölümünde baskın tahtasında not gizlenir (bekleme ve kapı/olay adımları masanındır).
            if(c.Blocked||planningFlow.Index>=PlanningTutorialFlow.GoalIndex&&c.RaidLoaded){note.Hide();pointer?.Hide();}else{Show();UpdatePointer();}
            if(Keyboard.current?.spaceKey.wasPressedThisFrame==true&&!c.Blocked&&!c.RaidLoaded&&(planningFlow.Current.Info||firstNote!=null))Ok();
        }
        /// <summary>
        /// İşaretçi (2026-11-24): adımın hedefini (<see cref="TutorialTargets.For"/>) ekrana izdüşürüp çerçeveler; "ilk kez" notu açıkken,
        /// hedef yokken ya da ekranda değilken gizler. Hedef: sahnedeki <see cref="TutorialAnchor"/>, panonun cebi (masa kamerası) ya da
        /// baskın tahtasının karesi (tahtanın kamerası). Yalnız okur.
        /// </summary>
        void UpdatePointer()
        {
            if(pointer==null)return;
            var c=planningContext;
            var spec=firstNote!=null?null:TutorialTargets.For(planningFlow.Current,c,c.RaidLoaded?raid?.State:null,c.RaidLoaded?raid?.SelectedUnit:null);
            if(spec==null||!TryScreen(spec,out var rect)){pointer.Hide();note.SetLeft(false);return;}
            pointer.Point(rect,spec.Label);
            // Hedef notun altında kalmasın: çakışırsa not sol köşeye kaçar (hedef sola gelirse geri döner)
            var paper=note.ScreenRect;
            if(paper.Overlaps(rect))note.SetLeft(rect.center.x>Screen.width*.5f);
        }

        readonly Vector3[] pocketCorners=new Vector3[4];
        bool TryScreen(TutorialTargetSpec spec,out Rect rect)
        {
            rect=default;
            if(spec.Anchor!=null)return TutorialAnchor.TryRect(spec.Anchor,out rect);
            if(spec.Pocket!=null)
            {
                var cam=deskCamera!=null?deskCamera:Camera.main;
                var b3=planningBoard!=null?planningBoard.Board3D:null;
                return cam!=null&&b3!=null&&planningBoard.IsOpen&&b3.PocketCorners(spec.Pocket,pocketCorners)&&TutorialAnchor.Project(cam,pocketCorners,out rect);
            }
            if(spec.Tile is Baskin.Pt t&&raid!=null&&raid.Board!=null&&raid.Board.Camera!=null)
            {
                var tr=raid.Board.transform;
                var b=new Bounds(tr.TransformPoint(RaidBoard3D.W(t.X,t.Y)),Vector3.zero);
                b.Encapsulate(tr.TransformPoint(RaidBoard3D.W(t.X-.45f,t.Y-.45f)));b.Encapsulate(tr.TransformPoint(RaidBoard3D.W(t.X+.45f,t.Y+.45f)));
                return TutorialAnchor.Project(raid.Board.Camera,b,out rect);
            }
            return false;
        }

        /// <summary>Kaydın yeni komutlarını tarar: aşçı ataması ve olay kararı bir kez görülünce kalır.</summary>
        void ScanRecord(Simulation sim)
        {
            var r=sim?.Record;
            if(!ReferenceEquals(r,scannedRecord)){scannedRecord=r;scannedCommands=0;cookAssigned=eventDecided=false;}
            if(r?.commands==null)return;
            for(;scannedCommands<r.commands.Count;scannedCommands++)
            {
                var x=r.commands[scannedCommands];
                if(x.kind=="board_assign"&&x.b=="cook")cookAssigned=true;
                else if(x.kind=="event_decide")eventDecided=true;
            }
        }
        void Stop()
        {
            flow = null;planningFlow=null;firstNote=null;
            note.Hide();pointer?.Hide();
        }

        void Ok()
        {
            if (!Active) return;
            if(planningFlow!=null)
            {
                if(firstNote!=null){firstSeen.Add(firstNote.Id);firstNote=null;SaveProgress();Show();return;}
                planningFlow.Acknowledge();SaveProgress();if(planningFlow.Finished)Finish();else Show();return;
            }
            flow.Acknowledge();
            if (flow.Finished) Finish();
            else Show();
        }

        /// <summary>Esc: not açıkken notlar kapanır (eğitim biter); masada başka kâğıt açık değilse çağrılır.</summary>
        public bool HandleEscape()
        {
            if(planningFlow!=null)return false;
            if (!Active || context.Blocked) return false;
            Skip();
            return true;
        }

        void Skip()
        {
            if (!Active) return;
            if(planningFlow!=null){planningFlow.Skip();Finish();return;}
            flow.Skip();
            Finish();
        }

        void Finish()
        {
            if(planningFlow!=null){PlanningDone=true;PlayerPrefs.DeleteKey(PlanningSeedKey);PlayerPrefs.DeleteKey(PlanningStepKey);PlayerPrefs.DeleteKey(PlanningStepIdKey);PlayerPrefs.DeleteKey(FirstNotesKey);PlayerPrefs.Save();}
            else Done = true;
            firstNote=null;
            note.Hide();pointer?.Hide();
        }

        void Update()
        {
            if (!Active) return;
            if(planningFlow!=null){UpdatePlanning();return;}
            Read();
            if (flow.Update(context) && flow.Finished)
            {
                Finish();
                return;
            }

            if (context.Blocked) note.Hide();
            else Show();
            // Revizyon 2: masada sağ altta (şalter plakasının altı), baskında EMRİ İLET'in üstünde
            //note.SetRaid(context.RaidLoaded);
            // Boşluk: bilgi adımında sonraki (baskında Boşluk makineleri hızlandırır)
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame && !context.Blocked && !context.RaidLoaded && flow.Current.Info) Ok();
        }

        void Show()
        {
            if (!Active) return;
            if(planningFlow!=null)
            {
                var p=planningFlow.Current;
                note.SetRaid(planningContext.RaidLoaded);
                if(firstNote!=null)
                {
                    note.Show(new TutorialNoteData{Head=Loc.T("REHBER"),Step=Loc.T("İLK KEZ"),Title=Loc.T(firstNote.Title),Body=Loc.T(firstNote.Body),ShowOk=true,Ok=Loc.T("SONRAKİ"),Skip=Loc.T("EĞİTİMİ BİTİR")});return;
                }
                // Rehber birkaç vardiya sürer: başlık hep "REHBER" (eski "İLK VARDİYA · REHBER" notta iki satıra kırılıp adım başlığına
                // değiyordu; ilk adımın başlığı zaten "İLK VARDİYAN"). Konu bölümünde sayaç adım numarası yerine "REHBER · KONU".
                note.Show(new TutorialNoteData{Head=Loc.T("REHBER"),Step=planningFlow.InTopics?Loc.T("REHBER · KONU"):Loc.F("ADIM {0}/{1}",planningFlow.Index+1,PlanningTutorialFlow.Steps.Length),Title=Loc.T(p.Title),Body=PlanningTutorialFlow.BodyText(p,game.Sim),ShowOk=p.Info,Ok=Loc.T("SONRAKİ"),Skip=Loc.T("EĞİTİMİ BİTİR")});return;
            }
            var step = flow.Current;
            string title = Loc.T(step.Title), body = TutorialFlow.BodyText(step, game != null ? game.Sim : null), counter = Loc.F("ADIM {0}/{1}", flow.Index + 1, TutorialFlow.Count);
            note.Show(new TutorialNoteData
            {
                Head = Loc.T("ANALİST · NOT"), Step = counter,
                Title = title, Body = body,
                ShowOk = step.Info, Ok = Loc.T("sonraki"), Skip = Loc.T("notları kapat"),
            });
        }

        /// <summary>Notun okuduğu durum: masa, açık belgeler, baskın tahtası ve sığınak.</summary>
        void Read()
        {
            var sim = game.Sim;
            var c = context;
            bool loaded = RaidLauncher.IsRaidLoaded;
            if (!loaded) raid = null;
            else if (raid == null) raid = FindFirstObjectByType<RaidDataSource>();
            var settings = SettingsCabinet.Instance;
            var over = FinalReportView.Instance;
            c.Blocked = briefing.Busy || shift.Playing || launcher != null && launcher.IsTransitioning || settings != null && settings.IsOpen ||
                        over != null && over.IsOpen || sim == null || sim.S.Ending != null;
            c.SlipOpen = map != null && map.SlipOpen;
            c.RaidOrdered = sim?.S.Raid != null;
            c.RaidLoaded = loaded && raid != null && raid.State != null;
            var state = c.RaidLoaded ? raid.State : null;
            // Tesis baskınında yürüyerek çıkışta son evre "pass" kalır: sonuç kartı (Finished) da bitiş sayılır.
            c.RaidOver = state != null && (state.Phase == "won" || state.Phase == "lost") || raid?.Current?.Finished == true;
            c.ReportOpen = operation != null && operation.ReportOpen;
            c.ShelterPose = rig != null && rig.Current == SiginakPose.Shelf;
            c.ShiftReportOpen = shift.ReportOpen;
            var workshop = sim?.Shelter?.Room(ShelterLogic.Workshop);
            c.WorkshopLevel = workshop?.Level ?? 0;
            c.WorkshopWorkers = workshop?.Workers ?? 0;
            c.Day = sim?.S.Day ?? 0;
            c.Raids = sim?.S.RaidLog.Count ?? 0;
        }
    }
}
