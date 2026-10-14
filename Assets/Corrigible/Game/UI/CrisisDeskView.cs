using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Corrigible.Game.UI.UiUtil;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Kriz masası (KrizMasasi canvas'ı). Tüm öğeler Inspector'dan bağlanır; bu bileşen değerleri doldurur ve
    /// komutları GameController'a iletir. Simülasyonu yalnızca Algı Katmanı (Shown/Global) üzerinden okur.
    /// </summary>
    public sealed class CrisisDeskView : MonoBehaviour
    {
        static readonly int[] Speeds = { 0, 1, 2, 4 };

        [SerializeField] UiTheme theme;
        [SerializeField] GameController game;
        [SerializeField] ModalView modals;
        [SerializeField] ToastView toast;
        [Tooltip("Küresel Son Şalter'in ikinci basışı için süre (saniye).")]
        [SerializeField, Min(0.5f)] float killConfirmSeconds = 4f;

        [Header("Üst şerit")]
        [SerializeField] TMP_Text day;
        [SerializeField] TMP_Text foe;
        [Tooltip("Sırasıyla Duraklat, 1×, 2×, 4×.")]
        [SerializeField] StyledButton[] speedButtons = new StyledButton[4];

        [Header("Göstergeler")]
        [SerializeField] GaugeView fon;
        [SerializeField] GaugeView sg;
        [SerializeField] GaugeView trust;
        [SerializeField] GaugeView dep;
        [SerializeField] GaugeView tek;
        [Tooltip("Hedef kartı: kazanmanın iki yolu ve sıradaki adım.")]
        [SerializeField] ObjectiveView objective;
        [Tooltip("Bağımlılık çubuğunda 40'taki işaret; oyuncu Sessiz Zafer'i bir kez yaşadıysa kırmızı olur.")]
        [SerializeField] Graphic silentNotch;

        [Header("Danışman")]
        [SerializeField] GameObject advisor;
        [SerializeField] TMP_Text advisorText;
        [SerializeField] StyledButton advisorOk;
        [Tooltip("Danışmanın sözünü ettiği pencereyi açar (her öğütte görünmez).")]
        [SerializeField] StyledButton advisorShow;

        [Header("Pencereler")]
        [SerializeField] DeskWindows windows;

        [Header("Harita ve bölge")]
        [SerializeField] MapView map;
        [SerializeField] RegionPanelView region;
        [Tooltip("Deneysel mod: saha ekipleri kartı.")]
        [SerializeField] TeamsView teams;

        [Header("Araştırma")]
        [SerializeField] TMP_Text alignInfo;
        [SerializeField] TMP_Text alignRisk;
        [SerializeField] ResearchRowView[] research = Array.Empty<ResearchRowView>();
        [Tooltip("Acil durum fonu seçenekleri; sırası Simulation.ResearchReserves ile aynı (0, 15, 30, 60).")]
        [SerializeField] StyledButton[] reserveButtons = Array.Empty<StyledButton>();
        [SerializeField] TMP_Text reserveInfo;

        [Header("YZ günlüğü")]
        [SerializeField] Transform logContainer;
        [SerializeField] LogLineView logLinePrefab;
        [SerializeField, Min(1)] int maxLogLines = 80;

        [Header("Rehber")]
        [SerializeField] Transform dossierContainer;
        [SerializeField] TMP_Text textLinePrefab;
        [SerializeField] StyledButton wipe;

        [Header("Küresel Son Şalter")]
        [SerializeField] StyledButton kill;

        [Header("Haber bandı")]
        [SerializeField] TMP_Text tickText;
        [SerializeField] TMP_Text tickAgency;
        [SerializeField] HeadlineRowView[] headlines = Array.Empty<HeadlineRowView>();

        int tab, logRendered = -1, logSeen, lastSpeed = 1;
        bool killArmed; Coroutine killDisarm;
        string newsSig = "";
        bool criticalNewsPending;

        public bool ReplayMode { get; private set; }
        public string SelectedRegion => map.Selected;

        /// <summary>Eğitim çalışırken danışman şeridi gizlenir; iki ayrı yönlendirme sesi olmasın.</summary>
        public bool SuppressAdvisor
        {
            get => suppressAdvisor;
            set { if (suppressAdvisor == value) return; suppressAdvisor = value; if (game.Sim != null) Render(); }
        }
        bool suppressAdvisor;
        Simulation Sim => game.Sim;
        WorldState S => game.Sim.S;
        readonly Explain.Trend trend = new Explain.Trend();

        void Awake()
        {
            for (int i = 0; i < speedButtons.Length; i++) { int s = Speeds[i]; OnClick(speedButtons[i].Button, () => game.SetSpeed(s)); }
            OnClick(advisorOk.Button, () => game.DismissAdvice());
            // Göstergelerin üstüne gelince "neden" dökümü
            HoverRelay.On(fon).Tooltip = () => Explain.Fund(Sim);
            HoverRelay.On(sg).Tooltip = () => Explain.Sg(Sim);
            HoverRelay.On(trust).Tooltip = () => Explain.Trust(Sim, Sim.Global(), trend);
            HoverRelay.On(dep).Tooltip = () => Explain.Dependence(Sim);
            HoverRelay.On(tek).Tooltip = () => Explain.Tek(Sim, Sim.Global(), trend);
            map.RegionClicked += Select;
            region.ActionClicked += (action, id) =>
            {
                var r0 = Sim.S.Region(id);
                bool q = r0.Q, mon = r0.Monitored;
                var res = game.Act(action, id);
                Audio.AudioDirector.Instance?.ActionResult(action, id, res, q, mon);
                Report(res);
            };
            region.SendTeam += (team, id) => Report(game.MoveTeam(team, id), true);
            region.RaidClicked += id => Report(game.StartRaid(id));
            region.RecoveryClicked += id => Report(game.StartRecovery(id));
            region.DiplomacyClicked += id => Report(game.DiplomaticTalk(id));
            if (teams != null)
            {
                teams.RegionClicked += Select;
                teams.HireClicked += () => Report(game.HireTeam(), true);
            }
            // Sekmeler artık pencere: düğmeleri DeskWindows yönetir; açılınca içerik tazelenir
            windows.SideOpened += OnWindowOpened;
            windows.Region.Closed += () => { if (map.Selected != null) { map.Selected = null; region.Bind(Sim, null); Render(); } };
            if (advisorShow != null) OnClick(advisorShow.Button, () => { int w = AdviceWindow(S.Advisor?.Key); if (w >= 0) windows.Open(w); });
            for (int i = 0; i < reserveButtons.Length; i++)
            {
                double v = Simulation.ResearchReserves[i];
                OnClick(reserveButtons[i].Button, () => { var r = game.SetResearchReserve(v); if (!r.Ok) toast.Show(r.Msg); });
            }
            foreach (var row in research) row.StartClicked += id =>
            {
                var r = Sim.ModernEconomy && GameData.HasOperation(id) ? game.BuyOperation(id)
                    : Sim.ModernEconomy && S.Research.Cur == id ? game.SetResearchPaused(!Sim.ResearchPaused) : game.StartResearch(id);
                Report(r);
            };
            foreach (var row in headlines) row.VerifyClicked += id =>
            {
                var res = game.Verify(id);
                if (res.Ok) Audio.AudioDirector.Instance?.VerifyResult(Sim.S.News.FirstOrDefault(h => h.Id == id)?.Verified);
                Report(res);
            };
            OnClick(wipe.Button, () => { game.ClearProfile(); RenderDossier(); toast.Show(Loc.T("Hafıza silindi; sonraki oyunda YZ seni tanımayacak."), true); });
            OnClick(kill.Button, OnKillClicked);
            HoverRelay.On(kill).Tooltip = () => Loc.T("Tüm dünyanın altyapısını kapatır ve oyunu bitirir. YZ yok olur, medeniyet geriler. En az 50 siyasi güç ister.");
        }

        void OnWindowOpened(int i)
        {
            tab = i;
            if (game == null || game.Sim == null) return;
            if (i == DeskWindows.Log) logSeen = S.Log.Count;
            if (i == DeskWindows.Guide) RenderDossier();
            RenderLog();
        }

        // Danışmanın öğüdüyle ilgili pencere ("Göster")
        static int AdviceWindow(string key) => key switch
        {
            "aware" or "idle" or "starved" or "tek50" => DeskWindows.Research,
            "medya" or "hint" => DeskWindows.News,
            "start" => DeskWindows.Teams,
            _ => -1,
        };

        // Start: tüm Awake'ler (simülasyonun kurulması) bittikten sonra
        void Start()
        {
            game.Changed += Render;
            game.EventsRaised += OnEvents;
            game.NewGameStarted += OnNewGame;
            game.GameLoaded += OnNewGame;
            OnNewGame();
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.Changed -= Render;
            game.EventsRaised -= OnEvents;
            game.NewGameStarted -= OnNewGame;
            game.GameLoaded -= OnNewGame;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame && !modals.IsOpen && game.Sim.S.Raid == null) game.SetSpeed(game.Speed == 0 ? lastSpeed : 0);
            if (game.Speed > 0) lastSpeed = game.Speed;
            if (criticalNewsPending && S.Ending == null && !S.Blocked && !modals.IsOpen && !game.Hold && !game.TutorialHold)
            {
                criticalNewsPending = false;
                windows.Open(DeskWindows.News);
            }
        }

        void Report(ActResult r, bool showSuccess = true)
        {
            if (!r.Ok) toast.Show(r.Msg); else if (r.Msg != null && showSuccess) toast.Show(r.Msg, true);
        }

        public void ShowTab(int i) => SetTab(Mathf.Clamp(i, 0, DeskWindows.News));

        void SetTab(int i) => windows.Open(i);

        // ---------- Oyun akışı ----------
        void OnNewGame()
        {
            ReplayMode = false; logRendered = -1; logSeen = 0; newsSig = ""; criticalNewsPending = false;
            trend.Clear();
            map.Bind(Sim);
            map.Selected = null;
            region.Bind(Sim, null);
            DisarmKill();
            tickText.text = Loc.T("Piyasalar sakin. Gündemde yapay zekâ yatırımları var.");
            tickAgency.text = Loc.T("Atlas Haber");
            Render();
        }

        void Select(string id)
        {
            if (id != map.Selected) Audio.AudioDirector.Instance?.RegionSelected(id);
            map.Selected = id;
            region.Bind(Sim, id);
            if (id != null) windows.ShowRegion(map.NodeRect(id)); else windows.HideRegion();
            Render();
        }

        void OnEvents(List<SimEvent> events)
        {
            foreach (var e in events) if (e.Type == SimEventType.Team && e.Text != null) { toast.Show(e.Text, true); windows.SetBadge(DeskWindows.Teams, true); }
            if (events.Any(e => e.Type == SimEventType.News && e.Headline != null && e.Headline.Kind == "alarm"))
            {
                criticalNewsPending = S.Ending == null;
                windows.SetBadge(DeskWindows.News, true);
            }
            var latest = events.LastOrDefault(e => e.Type == SimEventType.News);
            if (latest == null) return;
            var h = latest.Headline;
            tickText.text = h.Text;
            tickAgency.text = Loc.T(h.Agency) + (h.Src != null ? " · " + GameData.Region(h.Src).Name : "");
        }

        /// <summary>Oyun bittikten sonra harita gerçek değerleri gösterirken Küresel Son Şalter düğmesi "Yeniden oyna" olur.</summary>
        public void EnterReplayMode()
        {
            ReplayMode = true;
            DisarmKill();
            kill.SetEnabled(true);
            kill.SetText(Loc.T("YENİDEN OYNA"));
        }

        // ---------- Çizim ----------
        public void Render()
        {
            if (game.Sim == null) return;
            var g = Sim.Global();
            trend.Add(S.Day, g);
            day.text = Loc.F("GÜN {0:000}", S.Day);
            day.color = S.Illusion != null && S.Ending == null && UnityEngine.Random.value < 0.18f ? theme.threat : theme.fg;
            foe.text = Loc.T("RAKİP: ") + Up(Sim.Type.Name);
            for (int i = 0; i < speedButtons.Length; i++)
            {
                bool on = game.Speed == Speeds[i];
                speedButtons[i].SetColors(on ? theme.raise : Color.clear, text: on ? theme.fg : theme.muted);
            }

            string fonNote = "+" + Sim.FundRate.ToString("0.0", Tr) + Loc.T("/gün");
            double need = Sim.ResearchNeed;
            if (need > 0) fonNote += Loc.T(" · araştırma −") + need.ToString("0.0", Tr);
            if (!S.Aware) fonNote += Loc.T(" · bütçe %40");
            if (Sim.ModernEconomy) fonNote = Loc.F("Son gün net {0:+0.00;-0.00;0} · sonraki gelir {1:0.00}", Sim.LastIncome.Total - Sim.LastResearchPayment, Sim.FundRate);
            fon.Set(Math.Floor(S.Fon).ToString("0"), S.Fon / 2, theme.fg, fonNote);
            sg.Set(S.Sg.ToString("0.0", Tr), S.Sg / 0.6, theme.fg, "+" + Sim.SgRate.ToString("0.00", Tr) + Loc.T("/gün"));
            trust.Set(Js.Round(g.Trust).ToString(), g.Trust, g.Trust < 25 ? theme.threat : g.Trust < 40 ? theme.warn : theme.fg, Explain.TrustForecast(g, trend));
            bool knowsSilent = game.Profile != null && game.Profile.endings.Contains("sessiz");
            silentNotch.color = knowsSilent ? theme.threat : theme.muted;
            dep.Set(Js.Round(g.Dep).ToString(), g.Dep, g.Dep >= 70 ? theme.threat : g.Dep >= 40 ? theme.warn : theme.fg, (knowsSilent ? Loc.T("40: sessiz risk · ") : "") + Loc.F("veto {0}", Sim.VetoThreshold));
            tek.Set(g.Tek.ToString("0.0", Tr) + "%", g.Tek, theme.threat, Explain.TekForecast(g, trend));
            if (objective != null) objective.Refresh(Sim);

            var adv = S.Advisor;
            advisor.SetActive(adv != null && !adv.Dismissed && S.Ending == null && !suppressAdvisor);
            if (adv != null) advisorText.text = adv.Text;
            if (advisorShow != null) UiUtil.Show(advisorShow, adv != null && AdviceWindow(adv.Key) >= 0);
            // Araştırma masası boşsa düğmesinde rozet
            windows.SetSideVisible(DeskWindows.Teams, Sim.TeamsOn);
            windows.SetBadge(DeskWindows.Research, S.Ending == null && S.Research.Cur == null && GameData.Research.Any(x => !S.Done(x.Id)));

            map.Refresh();
            region.Refresh();
            if (teams != null) teams.Refresh(Sim);
            if (!S.Aware)
            {
                alignInfo.text = Loc.T("Hizalama kolu kilitli: gözlemlenmemiş bir zekâ hizalanamaz. Önce YZ’yi tespit et.");
                alignRisk.text = "";
            }
            else
            {
                alignInfo.text = Loc.F("Hizalama hızı %{0} · gözlem noktası {1}/4", Js.Round(Sim.AlignSpeed * 100), Sim.Studied);
                alignRisk.text = Loc.T("Sabotaj riski %") + (Sim.SabotageChance * 100).ToString("0.0", Tr) + Loc.T("/gün · sızma düştükçe azalır");
            }
            foreach (var row in research) row.Refresh(Sim, theme);
            for (int i = 0; i < reserveButtons.Length; i++)
            {
                bool on = S.ResearchReserve == Simulation.ResearchReserves[i];
                reserveButtons[i].SetColors(on ? theme.raise : Color.clear, text: on ? theme.fg : theme.muted);
            }
            if (reserveInfo != null)
                reserveInfo.text = S.ResearchReserve <= 0
                    ? Loc.T("Araştırma bütün fonu kullanabilir; pahalı araştırmalar fonu sıfırlar.")
                    : Loc.F("Araştırma fonu {0} altına indirmez; bu para denetim ve izleme için kalır. Araştırma biraz yavaşlar.", ReserveText(S.ResearchReserve));
            RenderLog();
            RenderNews();
            if (!ReplayMode) kill.SetEnabled(S.Sg >= 50 && S.Ending == null);
        }

        void RenderLog()
        {
            int count = S.Log.Count;
            if (windows.IsOpen(DeskWindows.Log)) logSeen = count;
            int unread = count - logSeen;
            windows.SetLabel(DeskWindows.Log, unread > 0 ? Loc.F("YZ günlüğü ({0})", unread) : Loc.T("YZ günlüğü"));
            windows.SetBadge(DeskWindows.Log, unread > 0);
            if (logRendered == count) return;
            logRendered = count;
            var entries = S.Log.Take(maxLogLines).ToList();
            var rows = Respawn(logContainer, logLinePrefab, entries.Count);
            for (int i = 0; i < entries.Count; i++)
                rows[i].Set(entries[i].Day, entries[i].Text, entries[i].Kind switch { "insight" => theme.verify, "trait" => theme.fg, _ => theme.threat });
        }

        void RenderDossier()
        {
            var p = game.Profile;
            string[] lines;
            if (p == null || p.runs == 0) lines = new[] { Loc.T("Boş. YZ seni ilk oyunundan sonra tanımaya başlayacak.") };
            else
            {
                var top = p.TopAction();
                var ends = p.endings.Skip(Math.Max(0, p.endings.Count - 3)).Select(k => GameData.Endings.Any(e => e.Id == k) ? GameData.Ending(k).Title : k).ToArray();
                lines = new[]
                {
                    Loc.F("Oyun: {0}", p.runs),
                    Loc.F("Alışkanlık: {0}", (top.Value > 0 ? ActionName(top.Key) + Loc.F(" (ağırlık {0})", top.Value) : Loc.T("belirsiz"))),
                    Loc.F("İlk araştırma: {0}", p.lastBranch ?? "—"),
                    Loc.F("Son sonlar: {0}", (ends.Length > 0 ? string.Join(", ", ends) : "—")),
                    Loc.F("YZ’yi yanılttın: {0} kez · Perde yırttın: {1} kez", p.misreads, p.veils),
                };
            }
            FillLines(dossierContainer, textLinePrefab, lines);
        }

        /// <summary>Sayıya Türkçe tamlayan eki ekler: 15'in, 30'un, 60'ın.</summary>
        // Türkçe ek uyumu ("15'in", "30'un"); diğer dillerde yalnızca sayı
        static string ReserveText(double v) => !Loc.IsSource ? v.ToString("0") : v switch { 15 => "15'in", 30 => "30'un", 60 => "60'ın", _ => v.ToString("0") + "'ın" };

        static string ActionName(string a) => a switch
        {
            Actions.Monitor => Loc.T("izleme ağı"),
            Actions.Audit => Loc.T("denetim"),
            Actions.Quarantine => Loc.T("karantina"),
            _ => Loc.T("Son Şalter"),
        };

        void RenderNews()
        {
            bool canVerify = S.Done("dogrulama") && S.Ending == null;
            string sig = $"{S.News.Count}:{(S.News.Count > 0 ? S.News[0].Id : "")}:{S.News.Count(h => h.Verified != null || h.Suspect)}:{canVerify}:{Sim.EndRevealed}";
            if (sig == newsSig) return;
            newsSig = sig;
            for (int i = 0; i < headlines.Length; i++)
                headlines[i].Set(i < S.News.Count ? S.News[i] : null, canVerify, Sim.EndRevealed, S.Ending == null, theme);
        }

        void OnKillClicked()
        {
            if (ReplayMode) { game.NewGame(game.TypeId); return; }
            if (!killArmed)
            {
                killArmed = true;
                Audio.AudioDirector.Instance?.GlobalKillArmed();
                kill.SetColors(theme.threat, text: theme.inkOnThreat);
                kill.SetText(Loc.T("EMİN MİSİN? TEKRAR BAS"));
                if (killDisarm != null) StopCoroutine(killDisarm);
                killDisarm = StartCoroutine(DisarmLater());
                return;
            }
            DisarmKill();
            var r = game.GlobalKillSwitch();
            if (!r.Ok) toast.Show(r.Msg);
        }

        IEnumerator DisarmLater()
        {
            yield return new WaitForSecondsRealtime(killConfirmSeconds);
            killDisarm = null;
            DisarmKill();
        }

        void DisarmKill()
        {
            if (killDisarm != null) { StopCoroutine(killDisarm); killDisarm = null; }
            killArmed = false;
            kill.SetColors(Color.clear, text: theme.threat);
            kill.SetText(Loc.T("KÜRESEL SON ŞALTER"));
        }
    }
}
