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
        [Tooltip("Bağımlılık çubuğunda 40'taki işaret; oyuncu Sessiz Zafer'i bir kez yaşadıysa kırmızı olur.")]
        [SerializeField] Graphic silentNotch;

        [Header("Danışman")]
        [SerializeField] GameObject advisor;
        [SerializeField] TMP_Text advisorText;
        [SerializeField] StyledButton advisorOk;

        [Header("Harita ve bölge")]
        [SerializeField] MapView map;
        [SerializeField] RegionPanelView region;
        [Tooltip("Deneysel mod: saha ekipleri kartı.")]
        [SerializeField] TeamsView teams;

        [Header("Sekmeler")]
        [Tooltip("Sırasıyla Araştırma, YZ Günlüğü, Rehber.")]
        [SerializeField] StyledButton[] tabs = new StyledButton[3];
        [SerializeField] GameObject[] tabPanels = new GameObject[3];

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

        void Awake()
        {
            for (int i = 0; i < speedButtons.Length; i++) { int s = Speeds[i]; OnClick(speedButtons[i].Button, () => game.SetSpeed(s)); }
            OnClick(advisorOk.Button, () => game.DismissAdvice());
            map.RegionClicked += Select;
            region.ActionClicked += (action, id) => Report(game.Act(action, id));
            region.SendTeam += (team, id) => Report(game.MoveTeam(team, id), true);
            region.RaidClicked += id => Report(game.StartRaid(id));
            if (teams != null)
            {
                teams.RegionClicked += Select;
                teams.HireClicked += () => Report(game.HireTeam(), true);
            }
            for (int i = 0; i < tabs.Length; i++) { int k = i; OnClick(tabs[i].Button, () => SetTab(k)); }
            for (int i = 0; i < reserveButtons.Length; i++)
            {
                double v = Simulation.ResearchReserves[i];
                OnClick(reserveButtons[i].Button, () => { var r = game.SetResearchReserve(v); if (!r.Ok) toast.Show(r.Msg); });
            }
            foreach (var row in research) row.StartClicked += id => { var r = game.StartResearch(id); if (!r.Ok) toast.Show(r.Msg); };
            foreach (var row in headlines) row.VerifyClicked += id => Report(game.Verify(id));
            OnClick(wipe.Button, () => { game.ClearProfile(); RenderDossier(); toast.Show("Hafıza silindi; sonraki oyunda YZ seni tanımayacak.", true); });
            OnClick(kill.Button, OnKillClicked);
            SetTab(0);
        }

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
        }

        void Report(ActResult r, bool showSuccess = true)
        {
            if (!r.Ok) toast.Show(r.Msg); else if (r.Msg != null && showSuccess) toast.Show(r.Msg, true);
        }

        public void ShowTab(int i) => SetTab(Mathf.Clamp(i, 0, tabs.Length - 1));

        void SetTab(int i)
        {
            tab = i;
            for (int k = 0; k < tabs.Length; k++)
            {
                tabs[k].SetColors(borderColor: k == i ? theme.verify : Color.clear, text: k == i ? theme.fg : theme.muted);
                tabPanels[k].SetActive(k == i);
            }
            if (game == null || game.Sim == null) return;
            if (i == 1) logSeen = S.Log.Count;
            if (i == 2) RenderDossier();
            RenderLog();
        }

        // ---------- Oyun akışı ----------
        void OnNewGame()
        {
            ReplayMode = false; logRendered = -1; logSeen = 0; newsSig = "";
            map.Bind(Sim);
            map.Selected = null;
            region.Bind(Sim, null);
            DisarmKill();
            tickText.text = "Piyasalar sakin. Gündemde yapay zekâ yatırımları var.";
            tickAgency.text = "Atlas Haber";
            Render();
        }

        void Select(string id)
        {
            map.Selected = id;
            region.Bind(Sim, id);
            Render();
        }

        void OnEvents(List<SimEvent> events)
        {
            foreach (var e in events) if (e.Type == SimEventType.Team && e.Text != null) toast.Show(e.Text, true);
            var latest = events.LastOrDefault(e => e.Type == SimEventType.News);
            if (latest == null) return;
            var h = latest.Headline;
            tickText.text = h.Text;
            tickAgency.text = h.Agency + (h.Src != null ? " · " + GameData.Region(h.Src).Name : "");
        }

        /// <summary>Oyun bittikten sonra harita gerçek değerleri gösterirken Küresel Son Şalter düğmesi "Yeniden oyna" olur.</summary>
        public void EnterReplayMode()
        {
            ReplayMode = true;
            DisarmKill();
            kill.SetEnabled(true);
            kill.SetText("YENİDEN OYNA");
        }

        // ---------- Çizim ----------
        public void Render()
        {
            if (game.Sim == null) return;
            var g = Sim.Global();
            day.text = $"GÜN {S.Day:000}";
            day.color = S.Illusion != null && S.Ending == null && UnityEngine.Random.value < 0.18f ? theme.threat : theme.fg;
            foe.text = "RAKİP: " + Up(Sim.Type.Name);
            for (int i = 0; i < speedButtons.Length; i++)
            {
                bool on = game.Speed == Speeds[i];
                speedButtons[i].SetColors(on ? theme.raise : Color.clear, text: on ? theme.fg : theme.muted);
            }

            string fonNote = "+" + Sim.FundRate.ToString("0.0", Tr) + "/gün";
            double need = Sim.ResearchNeed;
            if (need > 0) fonNote += " · araştırma −" + need.ToString("0.0", Tr);
            if (!S.Aware) fonNote += " · bütçe %40";
            fon.Set(Math.Floor(S.Fon).ToString("0"), S.Fon / 2, theme.fg, fonNote);
            sg.Set(S.Sg.ToString("0.0", Tr), S.Sg / 0.6, theme.fg, "+" + Sim.SgRate.ToString("0.00", Tr) + "/gün");
            trust.Set(Js.Round(g.Trust).ToString(), g.Trust, g.Trust < 25 ? theme.threat : g.Trust < 40 ? theme.warn : theme.fg);
            bool knowsSilent = game.Profile != null && game.Profile.endings.Contains("sessiz");
            silentNotch.color = knowsSilent ? theme.threat : theme.muted;
            dep.Set(Js.Round(g.Dep).ToString(), g.Dep, g.Dep >= 70 ? theme.threat : g.Dep >= 40 ? theme.warn : theme.fg, (knowsSilent ? "40: sessiz risk · " : "") + $"veto {Sim.VetoThreshold}");
            tek.Set(g.Tek.ToString("0.0", Tr) + "%", g.Tek, theme.threat);

            var adv = S.Advisor;
            advisor.SetActive(adv != null && !adv.Dismissed && S.Ending == null && !suppressAdvisor);
            if (adv != null) advisorText.text = adv.Text;

            map.Refresh();
            region.Refresh();
            if (teams != null) teams.Refresh(Sim);
            if (!S.Aware)
            {
                alignInfo.text = "Hizalama kolu kilitli: gözlemlenmemiş bir zekâ hizalanamaz. Önce YZ’yi tespit et.";
                alignRisk.text = "";
            }
            else
            {
                alignInfo.text = $"Hizalama hızı %{Js.Round(Sim.AlignSpeed * 100)} · gözlem noktası {Sim.Studied}/4";
                alignRisk.text = "Sabotaj riski %" + (Sim.SabotageChance * 100).ToString("0.0", Tr) + "/gün · sızma düştükçe azalır";
            }
            foreach (var row in research) row.Refresh(Sim, theme);
            for (int i = 0; i < reserveButtons.Length; i++)
            {
                bool on = S.ResearchReserve == Simulation.ResearchReserves[i];
                reserveButtons[i].SetColors(on ? theme.raise : Color.clear, text: on ? theme.fg : theme.muted);
            }
            if (reserveInfo != null)
                reserveInfo.text = S.ResearchReserve <= 0
                    ? "Araştırma bütün fonu kullanabilir; pahalı araştırmalar fonu sıfırlar."
                    : $"Araştırma fonu {ReserveText(S.ResearchReserve)} altına indirmez; bu para denetim ve izleme için kalır. Araştırma biraz yavaşlar.";
            RenderLog();
            RenderNews();
            if (!ReplayMode) kill.SetEnabled(S.Sg >= 50 && S.Ending == null);
        }

        void RenderLog()
        {
            int count = S.Log.Count;
            if (tab == 1) logSeen = count;
            int unread = count - logSeen;
            tabs[1].SetText(tab != 1 && unread > 0 ? $"YZ GÜNLÜĞÜ ({unread})" : "YZ GÜNLÜĞÜ");
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
            if (p == null || p.runs == 0) lines = new[] { "Boş. YZ seni ilk oyunundan sonra tanımaya başlayacak." };
            else
            {
                var top = p.TopAction();
                var ends = p.endings.Skip(Math.Max(0, p.endings.Count - 3)).Select(k => GameData.Endings.Any(e => e.Id == k) ? GameData.Ending(k).Title : k).ToArray();
                lines = new[]
                {
                    $"Oyun: {p.runs}",
                    $"Alışkanlık: {(top.Value > 0 ? ActionName(top.Key) + " (ağırlık " + top.Value + ")" : "belirsiz")}",
                    $"İlk araştırma: {p.lastBranch ?? "—"}",
                    $"Son sonlar: {(ends.Length > 0 ? string.Join(", ", ends) : "—")}",
                    $"YZ’yi yanılttın: {p.misreads} kez · Perde yırttın: {p.veils} kez",
                };
            }
            FillLines(dossierContainer, textLinePrefab, lines);
        }

        /// <summary>Sayıya Türkçe tamlayan eki ekler: 15'in, 30'un, 60'ın.</summary>
        static string ReserveText(double v) => v switch { 15 => "15'in", 30 => "30'un", 60 => "60'ın", _ => v.ToString("0") + "'ın" };

        static string ActionName(string a) => a switch
        {
            Actions.Monitor => "izleme ağı",
            Actions.Audit => "denetim",
            Actions.Quarantine => "karantina",
            _ => "Son Şalter",
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
                kill.SetColors(theme.threat, text: theme.inkOnThreat);
                kill.SetText("EMİN MİSİN? TEKRAR BAS");
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
            kill.SetText("KÜRESEL SON ŞALTER");
        }
    }
}
