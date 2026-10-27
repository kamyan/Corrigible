using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine;

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
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] RaidLauncher launcher;
        [SerializeField] TutorialNoteView note;

        TutorialFlow flow;
        RaidDataSource raid;
        readonly TutorialContext context = new TutorialContext();

        public bool Active => flow != null && !flow.Finished;
        /// <summary>Eğitim baskın adımında: açılan baskın senaryolu oynanır (<see cref="TutorialRaid"/>).</summary>
        public bool AtRaid => Active && flow.Current.Id == "raid";

        public static bool Done
        {
            get { try { return PlayerPrefs.GetInt(DoneKey, 0) == 1; } catch { return false; } }
            set { try { PlayerPrefs.SetInt(DoneKey, value ? 1 : 0); PlayerPrefs.Save(); } catch { } }
        }

        void OnEnable()
        {
            game.NewGameStarted += Begin;
            game.GameLoaded += Stop;
            note.OkClicked += Ok;
            note.SkipClicked += Skip;
            DeskLoc.Instance.Changed += Show;
        }

        void OnDisable()
        {
            game.NewGameStarted -= Begin;
            game.GameLoaded -= Stop;
            note.OkClicked -= Ok;
            note.SkipClicked -= Skip;
            DeskLoc.Instance.Changed -= Show;
        }

        // Oyun GameController.Awake'te kurulur; olay bu bileşen dinlemeye başlamadan geçmiş olabilir
        void Start()
        {
            var sim = game.Sim;
            if (flow == null && sim != null && sim.S.Day == 0 && (sim.Record?.commands.Count ?? 0) == 0) Begin();
        }

        void Begin()
        {
            flow = Done ? null : new TutorialFlow();
            if (flow == null) note.Hide();
        }

        void Stop()
        {
            flow = null;
            note.Hide();
        }

        void Ok()
        {
            if (!Active) return;
            flow.Acknowledge();
            if (flow.Finished) Finish();
            else Show();
        }

        void Skip()
        {
            if (!Active) return;
            flow.Skip();
            Finish();
        }

        void Finish()
        {
            Done = true;
            note.Hide();
        }

        void Update()
        {
            if (!Active) return;
            Read();
            if (flow.Update(context) && flow.Finished) { Finish(); return; }
            if (context.Blocked) note.Hide();
            else Show();
            // Masa pozunda vardiya şalteri, baskın bitince sonuç kartı sağ altta: not sağ üste çıkar
            note.SetTop(context.RaidOver || !context.RaidLoaded && rig != null && rig.Current == SiginakPose.Desk);
        }

        void Show()
        {
            if (!Active) return;
            var step = flow.Current;
            string title = Loc.T(step.Title), body = Loc.T(step.Body);
            // Senaryolu ilk baskın: not rehberin sıradaki adımını yazar (talimat, altında dersi)
            var guide = step.Id == "raid" && context.RaidLoaded ? raid.Guide : null;
            if (guide != null && !guide.Done)
            {
                title = Loc.F("BASKIN · ADIM {0}/{1}", guide.Index + 1, guide.Steps.Count);
                var s = guide.Current;
                body = raid.Executing ? Loc.T("Makineler sırayla oynuyor: bayraktaki sırayı ve okları izle.")
                    : s.Lesson == null ? TutorialRaid.Instruction(s) : TutorialRaid.Instruction(s) + "\n\n" + Loc.T(s.Lesson);
            }
            note.Show(new TutorialNoteData
            {
                Head = Loc.F("ANALİST NOTU · {0}/{1}", flow.Index + 1, TutorialFlow.Count),
                Title = title, Body = body,
                ShowOk = step.Info, Ok = Loc.T("TAMAM"), Skip = Loc.T("Eğitimi atla"),
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
            var settings = SettingsView.Instance;
            var over = GameOverView.Instance;
            var modal = EventModalView.Instance;
            c.Blocked = briefing.Busy || shift.Playing || launcher != null && launcher.IsTransitioning || settings != null && settings.IsOpen ||
                        over != null && over.IsOpen || sim == null || sim.S.Ending != null;
            c.SlipOpen = map != null && map.SlipOpen;
            c.RaidOrdered = sim?.S.Raid != null;
            c.RaidLoaded = loaded && raid != null && raid.State != null;
            var state = c.RaidLoaded ? raid.State : null;
            c.RaidOver = state != null && (state.Phase == "won" || state.Phase == "lost");
            c.ReportOpen = modal != null && modal.IsOpen;
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
