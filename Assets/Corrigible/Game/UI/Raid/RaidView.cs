using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Audio;
using Corrigible.Game.Raid;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Deneysel mod: Baskin sahnesinin yöneticisi (panel, brifing, sonuç, girdi). Kurallar Core'daki Baskin sınıfında,
    /// 3D tahta <see cref="RaidBoard3D"/>'de. Sahneyi ana sahnenin üstüne <see cref="RaidLauncher"/> yükler; bitince sonuç
    /// GameController.ResolveRaid ile döner ve sahne kapanır. Sahne tek başına açılırsa (editörde) örnek bir baskın kurulur.
    /// </summary>
    public sealed class RaidView : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] GameObject root;
        [SerializeField] RaidBoard3D board;
        [Tooltip("Önizlemede \"HEDEF KIRILIR\" yazısı.")]
        [SerializeField] TMP_Text winLabel;

        [Header("Üst")]
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text turn;
        [SerializeField] TMP_Text phase;
        [SerializeField] TMP_Text turnRemaining;
        [SerializeField] GameObject lastTurnStamp;

        [Header("Yan panel")]
        [SerializeField] TMP_Text objectives;
        [SerializeField] RaidUnitCard[] unitCards = new RaidUnitCard[3];
        [SerializeField] TMP_Text info;
        [SerializeField] StyledButton undo;
        [SerializeField] StyledButton endTurn;
        [SerializeField] StyledButton retreat;
        [SerializeField] StyledButton ability;
        [SerializeField] Transform logLines;
        [SerializeField] TMP_Text logLinePrefab;
        [SerializeField] RaidLogLineView styledLogLinePrefab;
        [SerializeField] ScrollRect logScroll;
        [SerializeField] TMP_Text targetModifiers;

        [Header("Kurallar kartı ve geri çekilme fişi")]
        [SerializeField] GameObject rulesPanel;
        [SerializeField] RectTransform rulesCard;
        [SerializeField] StyledButton rulesToggle;
        [SerializeField] TMP_Text rulesBody;
        [SerializeField] TMP_Text rulesSubtitle;
        [SerializeField] GameObject retreatConfirm;
        [SerializeField] StyledButton retreatCancel;

        [Header("Brifing ve sonuç")]
        [SerializeField] GameObject brief;
        [SerializeField] TMP_Text briefEyebrow;
        [SerializeField] TMP_Text briefBody;
        [SerializeField] StyledButton briefStart;
        [SerializeField] GameObject resultPanel;
        [SerializeField] TMP_Text resultEyebrow;
        [SerializeField] TMP_Text resultTitle;
        [SerializeField] TMP_Text resultBody;
        [SerializeField] TMP_Text resultStamp;
        [SerializeField] Graphic[] resultStampBorders = new Graphic[0];
        [SerializeField] StyledButton resultBack;

        GameController game;
        ToastView toast;
        Baskin.BState B;
        int sel = -1;
        bool actMode, retreatArmed, finished;
        bool resolvingAction, executingMachines, rulesOpen;
        int machineTurn, machineOrder, machineCount, machineLogStart, machineLogEnd, visibleLogCount = int.MaxValue;
        string machineAction;
        List<Baskin.IntentView> machineIntents;
        List<Baskin.LogLine> renderedLog;
        int renderedLogCount = -1, renderedLogFirst;
        List<RaidLogLineView> styledLogRows;
        List<TMP_Text> plainLogRows;
        string renderedLanguage;
        readonly List<(int x, int y, string kind, int max)> targetForms = new List<(int, int, string, int)>();
        int civilianCount;
        float retreatArmedUntil;
        (int x, int y)? hover;

        public bool IsOpen => root != null && root.activeSelf && B != null;
        bool Busy => resolvingAction || board.Busy;
        bool BriefOpen => brief != null && brief.activeSelf;

        static readonly string[] DirTr = { Loc.N("kuzeye"), Loc.N("doğuya"), Loc.N("güneye"), Loc.N("batıya") };
        static readonly Dictionary<string, string> ObjShort = new Dictionary<string, string>
        { ["core"] = Loc.N("ÇEKİRDEK"), ["node"] = Loc.N("DÜĞÜM"), ["backup"] = Loc.N("YEDEK"), ["civ"] = Loc.N("SİVİL"), ["cool"] = Loc.N("SOĞUTMA"), ["spawner"] = Loc.N("KOPYA") };

        void Awake()
        {
            board.TileClicked += OnTileClick;
            board.HoverChanged += OnHoverChanged;
            board.EffectPlaying += OnEffectPlaying;
            for (int i = 0; i < unitCards.Length; i++) { int k = i; UiUtil.OnClick(unitCards[i].Button.Button, () => SelectPlayer(k)); }
            UiUtil.OnClick(undo.Button, DoUndo);
            UiUtil.OnClick(endTurn.Button, DoEndTurn);
            UiUtil.OnClick(retreat.Button, DoRetreat);
            UiUtil.OnClick(briefStart.Button, StartRaid);
            UiUtil.OnClick(resultBack.Button, Finish);
            if (ability != null) UiUtil.OnClick(ability.Button, SelectAbility);
            if (rulesToggle != null) UiUtil.OnClick(rulesToggle.Button, ToggleRules);
            if (retreatCancel != null) UiUtil.OnClick(retreatCancel.Button, CancelRetreat);
            ConfigureLogMask();
        }

        void ConfigureLogMask()
        {
            if (logScroll == null || logScroll.viewport == null) return;
            var mask = logScroll.viewport.GetComponent<Mask>();
            if (mask == null || mask.graphic == null || mask.showMaskGraphic) return;
            // Gizli Mask renk yazmaz; alfa .001 ise Color32'de 0'a yuvarlanır ve stencil de çizilmez.
            var color = mask.graphic.color; color.a = 1f;
            mask.graphic.color = color;
        }

        void Start()
        {
            // Sahne tek başına açıldıysa (ana sahne yok) arayüz girdisi için bir EventSystem kur
            if (UnityEngine.EventSystems.EventSystem.current == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            game = FindFirstObjectByType<GameController>();
            toast = FindFirstObjectByType<ToastView>();
            if (game != null && game.Sim != null && game.Sim.S.Raid != null) Open(game.Sim.S.Raid.Params, game.Sim.S.Day);
            else Open(DemoParams(), 0);   // sahne tek başına açıldı: örnek baskın (geliştirme için)
        }

        void OnDestroy()
        {
            board.TileClicked -= OnTileClick;
            board.HoverChanged -= OnHoverChanged;
            board.EffectPlaying -= OnEffectPlaying;
            if (game != null && !finished) game.Hold = false;
        }

        static RaidParams DemoParams() => new RaidParams
        {
            Seed = (int)(Time.realtimeSinceStartup * 1000) & 0xffff, Region = "cn", RegionName = GameData.Region("cn").Name,
            Compute = 5, IsCore = true, Inf = 60, Traits = new List<string> { "aldatma" },
        };

        void Open(RaidParams p, int day)
        {
            B = Baskin.Create(p);
            sel = -1; actMode = false; hover = null; retreatArmed = false; finished = false;
            resolvingAction = false; executingMachines = false; visibleLogCount = int.MaxValue;
            renderedLog = null; renderedLogCount = -1; renderedLanguage = Loc.Lang;
            targetForms.Clear();
            foreach (var t in Baskin.TargetsLeft(B)) targetForms.Add((t.X, t.Y, t.O.K, t.O.Max));
            civilianCount = 0;
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (Baskin.CellAt(B, x, y).O?.K == "civ") civilianCount++;
            if (game != null) { game.SetSpeed(0); game.Hold = true; }
            root.SetActive(true);
            board.Build(B);
            resultPanel.SetActive(false);
            if (retreatConfirm != null) retreatConfirm.SetActive(false);
            title.text = Loc.F("SAHA OPERASYONU · {0} · {1}", UiUtil.Up(p.RegionName), UiUtil.Up(Loc.T(B.Layout)));
            if (briefEyebrow != null) briefEyebrow.text = Loc.F("SAHA OPERASYONU · GÜN {0} · {1}", day, UiUtil.Up(p.RegionName));
            if (briefBody != null) briefBody.text = Loc.F("Hedef: bütün hedef dolapları {0} tur içinde kır.\n\n{1}\n\n{2}", B.MaxTurns, RulesText(), ModifiersText());
            if (rulesBody != null) rulesBody.text = RulesText();
            if (targetModifiers != null) targetModifiers.text = ModifiersText();
            // Operasyon emri masada onaylandı; taktik sahnesinde kurallar aynı karttan katlanır.
            if (brief != null) brief.SetActive(rulesPanel == null);
            rulesOpen = true;
            RenderRules();
            undo.SetText(Loc.T("Geri al"));
            endTurn.SetText(Loc.T("Turu bitir"));
            retreat.SetText(Loc.T("Geri çekil"));
            if (ability != null) ability.SetText(Loc.T("Yetenek"));
            resultBack.SetText(Loc.T("Operasyon raporuna geç"));
            Render();
            if (!BriefOpen) StartCoroutine(IntentLocks(0.7f));
        }

        string ModifiersText()
        {
            var mods = new List<string>();
            if (B.Deceptive) mods.Add(Loc.T("Aldatma: bazı oklar yalan; Analist 3 karede kesinleştirir."));
            if (B.Params.Traits.Contains("kopya")) mods.Add(Loc.T("Kendini Kopyalama: her tur 2 robot uyanır."));
            if (B.HasBackup) mods.Add(Loc.T("Yedekleme: yedek çekirdek de kırılmalı."));
            if (B.Params.Traits.Contains("hizlanma")) mods.Add(Loc.T("Hızlanma: 1 tur eksik."));
            return string.Join("\n", mods);
        }

        static string RulesText() => Loc.F("{0}\n{1}\n{2}\n{3}\n{4}\n{5}",
            Loc.T("Kırmızı ok robotun bu turdaki hamlesi; bayraktaki sayı sırası. Kesik ok ve ? yalan olabilir: Analist 3 kare yaklaşınca kesinleşir."),
            Loc.T("Seç, taşı, kullan: 1–3 ya da tıkla, mavi kareye taşı, Q ile yetenek. Z turdaki hamleyi geri alır."),
            Loc.T("Zırh: hedef dolaplara yalnız sabotaj, itilen robot ve patlayan soğutma ünitesi işler."),
            Loc.T("İtme: çarpan da çarpılan da 1 hasar alır; kanala düşen de 1 hasar alır."),
            Loc.T("Sivilleri koru: kırılan sivil sunucu bölgenin ekonomisine ve güvenine işler."),
            Loc.T("Kazan: tur bitmeden bütün hedef dolapları kır."));

        void ToggleRules() { rulesOpen = !rulesOpen; RenderRules(); }

        void RenderRules()
        {
            if (rulesPanel != null) rulesPanel.SetActive(rulesOpen);
            if (rulesCard != null) rulesCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rulesOpen ? 300f : 40f);
            if (rulesToggle != null) rulesToggle.SetText(rulesOpen ? Loc.T("Katla") : Loc.T("Aç"));
            if (rulesSubtitle != null) rulesSubtitle.text = rulesOpen ? Loc.T("operasyon emrinden") : Loc.T("oklar · seçim · zırh · siviller");
        }

        void Finish()
        {
            if (finished || B == null || Busy || B.Phase == "player" || !resultPanel.activeSelf) return;
            finished = true;
            var res = Baskin.Result(B);
            B = null;
            if (game == null || game.Sim?.S.Raid == null)
            {
                SceneManager.LoadScene(gameObject.scene.name);   // tek başına açılmış sahne: yeni örnek baskın
                return;
            }
            game.Hold = false;
            var r = game.ResolveRaid(res);
            if (r.Msg != null && toast != null) toast.Show(r.Msg, res.Success);
            // ResolveRaid S.Raid'i temizler; RaidLauncher sahneyi kapatır. Yine de kapanmazsa kendimiz kapatırız.
            if (gameObject.scene.isLoaded && SceneManager.sceneCount > 1) SceneManager.UnloadSceneAsync(gameObject.scene);
        }

        // ---------- Girdi ----------
        Baskin.Unit Selected => B == null || sel < 0 ? null : B.Units.FirstOrDefault(u => u.Id == sel && !u.Dead);
        List<Baskin.Unit> Players => B.Units.Where(u => u.Side == 'p').ToList();

        void SelectPlayer(int index)
        {
            if (B == null || B.Phase != "player" || BriefOpen || Busy) return;
            var ps = Players;
            if (index < 0 || index >= ps.Count || ps[index].Dead) return;
            CancelRetreat();
            sel = ps[index].Id;
            Sfx.Play(Cue.RaidSelect, 1, board.Pan(RaidBoard3D.W(ps[index].X, ps[index].Y)));
            actMode = !Baskin.CanMove(B, ps[index]);
            Render();
        }

        void OnHoverChanged((int x, int y)? position)
        {
            hover = position; RenderOverlay(); RenderInfo();
        }

        void OnTileClick(int x, int y)
        {
            if (B == null || B.Phase != "player" || BriefOpen || Busy) return;
            var u = Selected;
            if (u != null)
            {
                bool canMoveHere = !actMode && Baskin.CanMove(B, u) && Baskin.Reachable(B, u).Any(t => t.X == x && t.Y == y);
                if (canMoveHere) { StartCoroutine(MoveRoutine(u.Id, x, y)); return; }
                if (Baskin.CanAct(B, u) && Baskin.Targets(B, u).Any(t => t.X == x && t.Y == y)) { StartCoroutine(ActRoutine(u.Id, x, y)); return; }
            }
            var v = Baskin.UnitAt(B, x, y);
            if (v != null && v.Side == 'p') { sel = v.Id; actMode = !Baskin.CanMove(B, v); Sfx.Play(Cue.RaidSelect, 1, board.Pan(RaidBoard3D.W(x, y))); }
            else sel = -1;
            Render();
        }

        void StartRaid()
        {
            if (brief != null) brief.SetActive(false);
            Sfx.Play(Cue.RaidBreach);
            Render();
            StartCoroutine(IntentLocks(0.7f));
        }

        // Robotların planı: her niyet bir "kilit" sesi, perdesi sıra numarasıyla yükselir; yalan olabilecek niyet titrer
        IEnumerator IntentLocks(float delay)
        {
            int expectedTurn = B.Turn;
            yield return new WaitForSecondsRealtime(delay);
            if (B == null || B.Phase != "player" || Busy || B.Turn != expectedTurn) yield break;
            foreach (var i in Baskin.Intents(B).OrderBy(z => z.Order))
            {
                if (B == null || Busy || B.Turn != expectedTurn) yield break;
                Sfx.Play(i.Uncertain ? Cue.IntentLockFuzzy : Cue.IntentLock, 0.8f, board.Pan(RaidBoard3D.W(i.FromX, i.FromY)), 1f + 0.12f * (i.Order - 1));
                yield return new WaitForSecondsRealtime(0.16f);
            }
        }

        IEnumerator MoveRoutine(int id, int x, int y)
        {
            if (!Baskin.DoMove(B, id, x, y)) yield break;
            CancelRetreat();
            resolvingAction = true; actMode = true;
            board.InputEnabled = false;
            board.ClearPreview();
            RenderSide();
            yield return board.AnimateMove(id, x, y);
            resolvingAction = false;
            Render();
        }

        IEnumerator ActRoutine(int id, int x, int y)
        {
            var who = B.Units.FirstOrDefault(z => z.Id == id);
            if (who != null) Sfx.Play(who.Type == "tek" ? Cue.RaidCutter : who.Type == "emp" ? Cue.RaidEmpCharge : Cue.RaidHookThrow, 1, board.Pan(RaidBoard3D.W(who.X, who.Y)));
            if (!Baskin.DoAct(B, id, x, y)) yield break;
            CancelRetreat();
            resolvingAction = true; sel = -1;
            board.InputEnabled = false;
            RenderSide();
            yield return board.Animate(B, new List<Baskin.Fx>(B.FxList), false);
            resolvingAction = false;
            Render();
        }

        IEnumerator EndTurnRoutine()
        {
            CancelRetreat();
            sel = -1; actMode = false;
            Render();   // Seçim kalkar, bu turun niyetleri canlandırma bitene kadar görünür kalır.
            machineIntents = Baskin.Intents(B);
            machineTurn = B.Turn; machineCount = machineIntents.Count; machineOrder = 0;
            machineAction = Loc.T("niyetler sırayla uygulanıyor");
            machineLogStart = B.Log.Count;
            Sfx.Play(Cue.RaidEndTurn);
            if (!Baskin.EndTurn(B)) yield break;
            resolvingAction = true; executingMachines = true;
            machineLogEnd = B.Log.Count;
            for (int i = machineLogStart + 1; i < B.Log.Count; i++) if (B.Log[i].Turn > machineTurn) { machineLogEnd = i; break; }
            visibleLogCount = Mathf.Min(machineLogEnd, machineLogStart + 1);
            board.InputEnabled = false;
            RenderSide(); RenderInfo();
            yield return board.Animate(B, new List<Baskin.Fx>(B.FxList), true);
            resolvingAction = false; executingMachines = false; visibleLogCount = int.MaxValue;
            Render();
            if (B.Phase == "player") StartCoroutine(IntentLocks(0.2f));
        }

        void OnEffectPlaying(Baskin.Fx fx, int index, int count)
        {
            if (!executingMachines || B == null) return;
            if (fx.K == "attack")
            {
                var intent = machineIntents.FirstOrDefault(i => i.Unit == fx.Unit);
                var actor = B.Units.FirstOrDefault(u => u.Id == fx.Unit);
                if (intent != null) machineOrder = intent.Order;
                machineAction = actor != null
                    ? intent != null && intent.Kind == "repair" ? Loc.F("{0}: {1}", Baskin.UnitTypes[actor.Type].Name, Loc.T("hedef dolabı onaracak")) : Loc.F("{0}: saldırı", Baskin.UnitTypes[actor.Type].Name)
                    : Loc.T("saldırı");
            }
            else if (fx.K == "heal")
            {
                var intent = machineIntents.FirstOrDefault(i => i.Kind == "repair" && i.Order > machineOrder && i.TargetX == fx.X && i.TargetY == fx.Y);
                if (intent != null) machineOrder = intent.Order;
                machineAction = Loc.T("Bakım robotu: onarım +1");
            }
            // Bu efektler Core'da bir günlük satırı üretir. İlerleme oranı gelecek turun ayıracını
            // erken açmaz; etkisi olmayan/uyanmayı engelleyen kalan satırlar animasyon bitince görünür.
            if (fx.K == "die" || fx.K == "break" || fx.K == "heal" || fx.K == "spawn")
                visibleLogCount = Mathf.Min(machineLogEnd, visibleLogCount + 1);
            RenderLog(); RenderInfo();
        }

        void DoUndo()
        {
            if (B == null || B.Phase != "player" || BriefOpen || Busy) return;
            if (Baskin.Undo(B)) { CancelRetreat(); actMode = false; Render(); }
        }

        void DoEndTurn()
        {
            if (B == null || B.Phase != "player" || BriefOpen || Busy) return;
            StartCoroutine(EndTurnRoutine());
        }

        void SelectAbility()
        {
            if (B == null || B.Phase != "player" || BriefOpen || Busy || Selected == null || !Baskin.CanAct(B, Selected)) return;
            actMode = true;
            Render();
        }

        void DoRetreat()
        {
            if (B == null || B.Phase != "player" || BriefOpen || Busy) return;
            if (!retreatArmed || Time.unscaledTime > retreatArmedUntil)
            {
                retreatArmed = true; retreatArmedUntil = Time.unscaledTime + 3;
                if (retreatConfirm != null) retreatConfirm.SetActive(true);
                RenderRetreat(); RenderInfo();
                return;
            }
            Sfx.Play(Cue.RaidRadio);
            Baskin.Retreat(B);
            CancelRetreat();
            Render();
        }

        void CancelRetreat()
        {
            retreatArmed = false;
            if (retreatConfirm != null) retreatConfirm.SetActive(false);
            RenderRetreat();
            if (B != null) RenderInfo();
        }

        void RenderRetreat()
        {
            retreat.SetText(retreatArmed ? Loc.T("Emin misin? Tekrar bas") : Loc.T("Geri çekil"));
            retreat.SetColors(retreatArmed ? theme.threat : theme.raise, theme.threat, retreatArmed ? theme.raise : theme.threat);
            var rect = retreat.transform as RectTransform;
            if (rect != null) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, retreatArmed ? 236f : 150f);
        }

        void Update()
        {
            if (!IsOpen || B == null) return;
            if (retreatArmed && Time.unscaledTime > retreatArmedUntil) CancelRetreat();
            if (renderedLanguage != Loc.Lang)
            {
                renderedLanguage = Loc.Lang; renderedLog = null;
                title.text = Loc.F("SAHA OPERASYONU · {0} · {1}", UiUtil.Up(B.Params.RegionName), UiUtil.Up(Loc.T(B.Layout)));
                if (rulesBody != null) rulesBody.text = RulesText();
                if (targetModifiers != null) targetModifiers.text = ModifiersText();
                RenderRules(); RenderRetreat(); RenderSide(); RenderInfo();
                if (!Busy) Render();
                if (resultPanel.activeSelf) RenderResult();
            }
            var kb = Keyboard.current;
            if (kb == null) return;
            if (BriefOpen) { if (kb.enterKey.wasPressedThisFrame) StartRaid(); return; }
            if (resultPanel.activeSelf) { if (kb.enterKey.wasPressedThisFrame) Finish(); return; }
            if (Busy) return;
            if (kb.digit1Key.wasPressedThisFrame) SelectPlayer(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectPlayer(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectPlayer(2);
            if (kb.zKey.wasPressedThisFrame) DoUndo();
            if (kb.qKey.wasPressedThisFrame) SelectAbility();
            if (kb.enterKey.wasPressedThisFrame) { DoEndTurn(); return; }
            if (kb.escapeKey.wasPressedThisFrame) { if (retreatArmed) CancelRetreat(); else { sel = -1; Render(); } }
        }

        // ---------- Çizim ----------
        static string Pips(int hp, int max) => new string('■', Mathf.Max(0, hp)) + new string('□', Mathf.Max(0, max - hp));

        void Render()
        {
            if (B == null) return;
            board.InputEnabled = B.Phase == "player" && !BriefOpen && !Busy;
            var u0 = Selected;
            var m = new RaidBoard3D.Marks { Selected = sel };
            if (u0 != null && !actMode && Baskin.CanMove(B, u0)) foreach (var p in Baskin.Reachable(B, u0)) m.Moves.Add(p.Y * Baskin.N + p.X);
            if (u0 != null && Baskin.CanAct(B, u0)) foreach (var p in Baskin.Targets(B, u0)) m.Targets.Add(p.Y * Baskin.N + p.X);
            foreach (var i in Baskin.Intents(B))
            {
                m.Badges[i.Unit] = (i.Order.ToString(), i.Uncertain ? "?" : i.IsScanned ? "✓" : null, i.Uncertain ? theme.warn : i.IsScanned ? theme.verify : theme.threat);
                if (!i.HasTarget || i.TargetNone) continue;
                int k = i.TargetY * Baskin.N + i.TargetX;
                if (i.Kind == "repair") m.Heal.Add(k);
                else m.Danger[k] = (m.Danger.TryGetValue(k, out var sure) && sure) || !i.Uncertain;
                m.Arrows.Add((i.FromX, i.FromY, i.TargetX, i.TargetY, theme.threat, i.Uncertain));
            }
            foreach (var s in B.Spawns) if (Baskin.UnitAt(B, s.X, s.Y) == null) m.Spawns.Add(s.Y * Baskin.N + s.X);
            foreach (var u in B.Units) if (u.Side == 'p' && !u.Dead && !Baskin.CanAct(B, u)) m.Spent.Add(u.Id);
            board.Sync(B, m);
            RenderOverlay();
            RenderSide();
            RenderInfo();
            if (B.Phase != "player" && !Busy && !resultPanel.activeSelf) ShowResult();
        }

        void RenderOverlay()
        {
            if (B == null) return;
            if (winLabel != null) winLabel.gameObject.SetActive(false);
            // Önizleme: seçili birimin yeteneği üzerine gelinen kareye uygulanırsa
            var u = Selected;
            if (u == null || hover == null || B.Phase != "player" || !Baskin.CanAct(B, u) || Busy) { board.ClearPreview(); return; }
            var (hx, hy) = hover.Value;
            bool isMove = !actMode && Baskin.CanMove(B, u) && Baskin.Reachable(B, u).Any(t => t.X == hx && t.Y == hy);
            if (isMove || !Baskin.Targets(B, u).Any(t => t.X == hx && t.Y == hy)) { board.ClearPreview(); return; }
            var pv = Baskin.Preview(B, u.Id, hx, hy);
            var dmg = new Dictionary<int, (int x, int y, int n, string label)>();
            foreach (var f in pv.Fx)
            {
                int k = f.Y * Baskin.N + f.X;
                if (f.K == "dmg") { dmg.TryGetValue(k, out var d); dmg[k] = (f.X, f.Y, d.n + f.N, null); }
                if (f.K == "armor") dmg[k] = (f.X, f.Y, 0, Loc.T("ZIRH"));
            }
            board.SetPreview(
                dmg.Values.Select(d => ((float)d.x, (float)d.y, d.label ?? Loc.F("−{0}", d.n), d.label != null ? theme.fg : theme.threat)).ToList(),
                pv.Moves.Select(mv => (mv.From.X, mv.From.Y, mv.To.X, mv.To.Y)).ToList());
            if (pv.Won && winLabel != null) { winLabel.text = Loc.T("HEDEF KIRILIR"); winLabel.color = theme.threat; winLabel.gameObject.SetActive(true); }
        }

        string TargetLine((int x, int y, string kind, int max) t, bool report)
        {
            var ob = Baskin.CellAt(B, t.x, t.y).O;
            int hp = ob != null && ob.K == t.kind ? ob.Hp : 0;
            string name = Loc.T(Baskin.ObjNames[t.kind]);
            if (hp <= 0) return Loc.F("<color=#{0}>✓ {1} · {2}</color>", ColorUtility.ToHtmlStringRGB(theme.good), name, Loc.T("KIRILDI"));
            return report
                ? Loc.F("{0} · {1} {2}/{3}", name, Loc.T("ayakta"), hp, t.max)
                : Loc.F("{0}: <b>{1}/{2}</b> · {3}", name, hp, t.max, Loc.T("ZIRHLI"));
        }

        void RenderSide()
        {
            bool playing = B.Phase == "player";
            bool interactive = playing && !BriefOpen && !Busy;
            int displayTurn = executingMachines ? machineTurn : Mathf.Min(B.Turn, B.MaxTurns);
            Sfx.SetLoop(LoopCue.RaidClock, playing && !executingMachines && B.Turn == B.MaxTurns);
            turn.text = Loc.F("{0}/{1}", displayTurn, B.MaxTurns);
            turn.color = playing && displayTurn == B.MaxTurns ? theme.threat : theme.fg;
            if (phase != null)
            {
                phase.text = executingMachines ? Loc.T("MAKİNELER HAREKET EDİYOR · girdi kilitli") : playing ? Loc.T("SIRA SENDE") : Loc.T("OPERASYON BİTTİ");
                phase.color = executingMachines ? theme.threat : playing ? theme.verify : theme.muted;
            }
            int remaining = playing || executingMachines ? Mathf.Max(0, B.MaxTurns - displayTurn + 1) : 0;
            if (turnRemaining != null) turnRemaining.text = playing || executingMachines ? Loc.F("kalan {0} tur", remaining) : Loc.T("operasyon bitti");
            if (lastTurnStamp != null) lastTurnStamp.SetActive(playing && displayTurn == B.MaxTurns && !executingMachines);
            var lines = targetForms.Select(t => TargetLine(t, false)).ToList();
            lines.Add(Loc.F("{0}: <b>{1}/{2}</b> · {3}", Loc.T("Kırılan sivil sunucu"), B.Collateral, civilianCount, B.Collateral > 0 ? Loc.T("YAN HASAR") : Loc.T("temiz")));
            lines.Add(Loc.F("{0}: <b>{1}</b> · {2}", Loc.T("Kalan tur"), remaining, playing || executingMachines ? Loc.T("bu tur dahil") : Loc.T("operasyon bitti")));
            objectives.text = string.Join("\n", lines);
            var ps = Players;
            for (int i = 0; i < unitCards.Length && i < ps.Count; i++)
            {
                var u = ps[i]; var T = Baskin.UnitTypes[u.Type];
                bool spent = executingMachines || !Baskin.CanAct(B, u);
                string state = u.Dead ? Loc.T("DÜŞTÜ") : spent ? Loc.T("TUR BİTTİ") : Baskin.CanMove(B, u) ? Loc.T("HAREKET + YETENEK") : Loc.T("YETENEK");
                unitCards[i].Set(T.Short, Loc.F("{0}. {1}", i + 1, UiUtil.Up(T.Name)), Loc.F("{0}/{1} can · {2}", u.Hp, u.Max, state),
                    AbilityText(u), u.Id == sel, u.Dead, theme, spent, interactive, u.Hp, u.Max, T.Move, state);
            }
            undo.SetText(Loc.T("Geri al")); endTurn.SetText(Loc.T("Turu bitir"));
            undo.SetEnabled(interactive && B.Hist.Count > 0);
            endTurn.SetEnabled(interactive);
            retreat.SetEnabled(interactive);
            if (ability != null)
            {
                ability.SetText(Loc.T("Yetenek"));
                ability.SetEnabled(interactive && Selected != null && Baskin.CanAct(B, Selected));
            }
            RenderRetreat();
            RenderLog();
        }

        static string AbilityText(Baskin.Unit u)
        {
            if (u.Type == "tek") return Loc.F("<b>{0}</b> · {1}\n{2}", Loc.T("Sabotaj"), Loc.T("bitişik kare"), Loc.T("Dolaba 2 hasar; robota 1 hasar ve 1 kare itme."));
            if (u.Type == "emp") return Loc.F("<b>{0}</b> · {1}\n{2}", Loc.T("EMP atışı"), Loc.T("düz hat, 2–5 kare"), Loc.T("Hedefe 1 hasar; çevresindeki robotları dışarı iter. Zırha işlemez."));
            return Loc.F("<b>{0}</b> · {1}\n{2}\n{3}", Loc.T("Kanca"), Loc.T("düz hat, 5 kare"),
                Loc.T("Robotu 1 kare çeker; duvarda ya da dolapta yanına atılır."), Loc.T("Tarama · 3 kare çevresindeki niyetler kesinleşir."));
        }

        void RenderLog()
        {
            int count = Mathf.Min(B.Log.Count, visibleLogCount);
            if (renderedLog == B.Log && renderedLogCount == count) return;
            bool follow = logScroll == null || logScroll.verticalNormalizedPosition < 0.05f || renderedLog == null;
            int previous = renderedLog == B.Log ? renderedLogCount : -1;
            int first = Mathf.Max(0, count - 64);
            var log = B.Log.Skip(first).Take(count - first).ToList();
            bool rebuild = renderedLog != B.Log || first != renderedLogFirst || count < renderedLogCount;
            if (styledLogLinePrefab != null)
            {
                if (rebuild || styledLogRows == null)
                {
                    styledLogRows = UiUtil.Respawn(logLines, styledLogLinePrefab, log.Count);
                    for (int i = 0; i < log.Count; i++) styledLogRows[i].Set(log[i], theme, previous >= 0 && first + i >= previous && executingMachines);
                }
                else for (int i = styledLogRows.Count; i < log.Count; i++)
                {
                    var row = Instantiate(styledLogLinePrefab, logLines);
                    styledLogRows.Add(row);
                    row.Set(log[i], theme, executingMachines);
                }
            }
            else
            {
                if (rebuild || plainLogRows == null) plainLogRows = UiUtil.Respawn(logLines, logLinePrefab, log.Count);
                else while (plainLogRows.Count < log.Count) plainLogRows.Add(Instantiate(logLinePrefab, logLines));
                for (int i = 0; i < log.Count; i++)
                {
                    plainLogRows[i].text = log[i].Text;
                    plainLogRows[i].color = log[i].Kind switch { "ai" => theme.threat, "good" => theme.good, "bad" => theme.threat, "turn" => theme.muted, _ => theme.fg };
                    plainLogRows[i].fontStyle = log[i].Kind == "ai" || log[i].Kind == "turn" ? FontStyles.Bold : FontStyles.Normal;
                }
            }
            renderedLog = B.Log; renderedLogCount = count; renderedLogFirst = first;
            if (follow && logScroll != null) StartCoroutine(ScrollLogToEnd());
        }

        IEnumerator ScrollLogToEnd()
        {
            yield return null;
            if (logScroll != null && logScroll.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(logScroll.content);
            Canvas.ForceUpdateCanvases();
            if (logScroll != null) logScroll.verticalNormalizedPosition = 0;
        }

        void RenderInfo()
        {
            if (B == null) return;
            info.color = theme.fg;
            if (executingMachines)
            {
                info.text = Loc.F("<b>Makineler hareket ediyor · sıra {0}/{1}:</b> {2}. Girdi kilitli; sıra sana dönünce düğmeler açılır.", machineOrder, machineCount, machineAction);
                return;
            }
            if (Busy) { info.text = Loc.T("Hamle uygulanıyor; bitince düğmeler açılır."); return; }
            if (B.Phase != "player")
            {
                info.text = B.Phase == "won" ? Loc.T("<b>Hedef kırıldı.</b> Operasyon başarılı. Rapora geçmek için Enter.") : Loc.F("<b>Operasyon başarısız.</b> {0} Rapora geçmek için Enter.", B.OutcomeReason);
                return;
            }
            if (retreatArmed)
            {
                info.text = Loc.T("<b>Geri çekilme:</b> emin misin? Tekrar basarsan ekip binadan çıkar ve operasyon başarısız sayılır.");
                return;
            }
            var u = Selected;
            string txt = u != null
                ? Loc.F("<b>{0}</b> seçili. {1}", Baskin.UnitTypes[u.Type].Name,
                    !actMode && Baskin.CanMove(B, u) ? Loc.T("Mavi kareye taşı ya da hedefe yeteneğini kullan (Q: yetenek).") : Baskin.CanAct(B, u) ? Loc.T("Yeşil hedefe yeteneğini kullan.") : Loc.T("Bu birim bu tur işini bitirdi."))
                : Loc.T("<b>Sıra sende.</b> Bir birim seç: 1–3 ya da tahtada tıkla. Kırmızı oklar robotların bu turdaki hamleleri, bayraktaki sayı sıraları.");
            if (hover != null)
            {
                var (x, y) = hover.Value;
                var c = Baskin.CellAt(B, x, y);
                var v = Baskin.UnitAt(B, x, y);
                if (v != null && v.Side == 'e')
                {
                    var intent = Baskin.Intents(B).FirstOrDefault(z => z.Unit == v.Id);
                    string what = intent == null ? Loc.T("bekliyor") : intent.Kind == "repair" ? Loc.T("hedef dolabı onaracak") : intent.Kind == "shoot" ? Loc.F("{0} ateş edecek (1 hasar)", Loc.T(DirTr[intent.Dir])) : Loc.F("{0} saldıracak (2 hasar)", Loc.T(DirTr[intent.Dir]));
                    string certainty = intent != null && intent.Uncertain ? Loc.T("Bu niyet yalan olabilir; Analisti 3 kare yaklaştır.") : intent != null && intent.IsScanned ? Loc.T("Analist doğruladı.") : "";
                    txt = Loc.F("<b>{0}</b> · {1}/{2} can · sıra {3}: {4}. {5}", Baskin.UnitTypes[v.Type].Name, v.Hp, v.Max, intent != null ? intent.Order.ToString() : "—", what, certainty);
                }
                else if (v != null) txt = Loc.F("<b>{0}</b> · {1}/{2} can.", Baskin.UnitTypes[v.Type].Name, v.Hp, v.Max);
                else if (c.O != null)
                {
                    string detail = Baskin.IsTarget(c.O.K) ? Loc.T("Zırhlı: yalnızca sabotaj, çarpma ve patlama işler.") : c.O.K == "civ" ? Loc.T("Kırılırsa yan hasar.") : c.O.K == "cool" ? Loc.T("Kırılınca çevresindeki 4 kareye 1 hasar.") : Loc.T("Kırılırsa buradan robot uyanmaz.");
                    txt = Loc.F("<b>{0}</b> · {1}/{2} can. {3}", Loc.T(Baskin.ObjNames[c.O.K]), c.O.Hp, c.O.Max, detail);
                }
                else if (c.T == "cable") txt = Loc.T("<b>Elektrikli kanal</b> · üstüne itilen 1 hasar alır.");
                else if (c.T == "wall") txt = Loc.T("<b>Duvar</b>");
            }
            info.text = txt;
        }

        void ShowResult()
        {
            resultPanel.SetActive(true);
            board.InputEnabled = false;
            CancelRetreat();
            Sfx.SetLoop(LoopCue.RaidClock, false);
            Sfx.Play(B.Phase == "won" ? Cue.RaidWin : Cue.RaidFail);
            RenderResult();
        }

        void RenderResult()
        {
            var res = Baskin.Result(B);
            var heading = res.Success ? Loc.T("HEDEF KIRILDI") : res.Retreat ? Loc.T("GERİ ÇEKİLDİ") : Players.All(u => u.Dead) ? Loc.T("EKİP DÜŞTÜ") : B.Turn >= B.MaxTurns ? Loc.T("SÜRE DOLDU") : Loc.T("OPERASYON BAŞARISIZ");
            var color = res.Success ? theme.good : theme.threat;
            resultEyebrow.text = Loc.F("OPERASYON SONUCU · {0} · {1}. TUR", res.Success ? Loc.T("BAŞARILI") : Loc.T("BAŞARISIZ"), res.Turns);
            resultTitle.text = heading;
            resultTitle.color = color;
            if (resultStamp != null) { resultStamp.text = heading; resultStamp.color = color; }
            if (resultStampBorders != null) foreach (var border in resultStampBorders) if (border != null) border.color = color;
            var rows = targetForms.Select(t => TargetLine(t, true)).ToList();
            rows.Add(Loc.F("{0}: <b>{1}</b>", Loc.T("Kırılan sivil sunucu"), res.Collateral));
            rows.Add(Loc.F("{0}: <b>{1}/{2} ayakta</b>", Loc.T("Ekip"), Players.Count - res.Down, Players.Count));
            resultBody.text = Loc.F("{0} · {1} · Tur {2}/{3}\n\n{4}\n\n{5}", B.Params.RegionName, Loc.T(B.Layout), res.Turns, B.MaxTurns, string.Join("\n", rows), res.Reason);
            resultBack.SetText(Loc.T("Operasyon raporuna geç"));
            resultBack.SetEnabled(true);
        }
    }
}
