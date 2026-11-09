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
    /// (PlayerPrefs; editörde Corrigible › Eğitim › Eğitimi sıfırla). Kayıttan devam eden oyunda eğitim yoktur.
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
        public const string PlanningDoneKey="Corrigible.PlanningTutorialDone";
        public const string PlanningSeedKey="Corrigible.PlanningTutorialSeed";
        public const string PlanningStepKey="Corrigible.PlanningTutorialStep";
        PlanningTutorialFlow planningFlow;
        PlanningTutorialContext planningContext=new PlanningTutorialContext();
        bool loaded,planningSaved;
        public static bool PlanningDone{get=>PlayerPrefs.GetInt(PlanningDoneKey,0)==1;set{PlayerPrefs.SetInt(PlanningDoneKey,value?1:0);PlayerPrefs.Save();}}
        public string CurrentStepId=>planningFlow!=null?planningFlow.Current?.Id:flow?.Current?.Id;
        TutorialFlow flow;
        RaidDataSource raid;
        readonly TutorialContext context = new TutorialContext();

        public bool Active => planningFlow!=null?!planningFlow.Finished:flow!=null&&!flow.Finished;

        /// <summary>Eğitim baskın adımında: açılan baskın senaryolu oynanır (<see cref="TutorialRaid"/>).</summary>
        public bool AtRaid => flow!=null&&!flow.Finished&&flow.Current.Id=="raid";

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
                note.Hide();return;
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
            planningContext=new PlanningTutorialContext();planningFlow=new PlanningTutorialFlow(PlayerPrefs.GetInt(PlanningStepKey));planningSaved=true;
        }
        void SaveProgress()
        {
            if(planningFlow==null)return;
            planningSaved=true;
            PlayerPrefs.SetString(PlanningSeedKey,game.Sim.S.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(PlanningStepKey,planningFlow.Index);PlayerPrefs.Save();
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
            c.AssignedCook=sim?.Record?.commands.Any(x=>x.kind=="board_assign"&&x.b=="cook")==true;
            c.SlipOpen=map!=null&&map.SlipOpen;c.RaidOrdered=sim?.S.Raid!=null;c.RaidLoaded=raidLoaded&&raid?.State!=null;
            var state=c.RaidLoaded?raid.State:null;
            c.RaidOver=state!=null&&(state.Phase=="won"||state.Phase=="lost");
            if(state!=null)
            {
                c.RaidSelected|=raid.Selected!='\0';c.RaidMoved|=state.Units.Any(u=>u.Side=='p'&&u.Moved);
                c.RaidActed|=state.Units.Any(u=>u.Side=='p'&&u.Acted);c.RaidTurnEnded|=state.Turn>1;
            }
            c.ReportOpen=operation!=null&&operation.ReportOpen;c.Printed=sim?.Board?.Printed==true;
            c.Raids=sim?.S.RaidLog.Count??0;c.Day=sim?.S.Day??0;
            if(!c.Blocked&&!planningSaved)SaveProgress();
            if(planningFlow.Update(c))SaveProgress();
            if(planningFlow.Finished){Finish();return;}
            if(c.Blocked)note.Hide();else Show();
            if(Keyboard.current?.spaceKey.wasPressedThisFrame==true&&!c.Blocked&&!c.RaidLoaded&&planningFlow.Current.Info)Ok();
        }
        void Stop()
        {
            flow = null;planningFlow=null;
            note.Hide();
        }

        void Ok()
        {
            if (!Active) return;
            if(planningFlow!=null){planningFlow.Acknowledge();SaveProgress();if(planningFlow.Finished)Finish();else Show();return;}
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
            if(planningFlow!=null){PlanningDone=true;PlayerPrefs.DeleteKey(PlanningSeedKey);PlayerPrefs.DeleteKey(PlanningStepKey);PlayerPrefs.Save();}
            else Done = true;
            note.Hide();
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
                note.Show(new TutorialNoteData{Head=Loc.T("İLK VARDİYA · REHBER"),Step=Loc.F("ADIM {0}/{1}",planningFlow.Index+1,PlanningTutorialFlow.Steps.Length),Title=Loc.T(p.Title),Body=Loc.T(p.Body),ShowOk=p.Info,Ok=Loc.T("SONRAKİ"),Skip=Loc.T("EĞİTİMİ BİTİR")});return;
            }
            var step = flow.Current;
            string title = Loc.T(step.Title), body = Loc.T(step.Body), counter = Loc.F("ADIM {0}/{1}", flow.Index + 1, TutorialFlow.Count);
            // Senaryolu ilk baskın: not rehberin sıradaki adımını yazar (talimat, altında dersi)
            var guide = step.Id == "raid" && context.RaidLoaded ? raid.Guide : null;
            if (guide != null && !guide.Done)
            {
                counter = Loc.F("BASKIN · ADIM {0}/{1}", guide.Index + 1, guide.Steps.Count);
                var s = guide.Current;
                body = raid.Executing ? Loc.T("Makineler sırayla oynuyor: bayraktaki sırayı ve okları izle.")
                    : s.Lesson == null ? TutorialRaid.Instruction(s) : TutorialRaid.Instruction(s) + "\n\n" + Loc.T(s.Lesson);
            }

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
            c.RaidOver = state != null && (state.Phase == "won" || state.Phase == "lost");
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
