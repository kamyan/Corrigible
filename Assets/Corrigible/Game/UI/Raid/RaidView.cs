using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

        [Header("Yan panel")]
        [SerializeField] TMP_Text objectives;
        [SerializeField] RaidUnitCard[] unitCards = new RaidUnitCard[3];
        [SerializeField] TMP_Text info;
        [SerializeField] StyledButton undo;
        [SerializeField] StyledButton endTurn;
        [SerializeField] StyledButton retreat;
        [SerializeField] Transform logLines;
        [SerializeField] TMP_Text logLinePrefab;

        [Header("Brifing ve sonuç")]
        [SerializeField] GameObject brief;
        [SerializeField] TMP_Text briefEyebrow;
        [SerializeField] TMP_Text briefBody;
        [SerializeField] StyledButton briefStart;
        [SerializeField] GameObject resultPanel;
        [SerializeField] TMP_Text resultEyebrow;
        [SerializeField] TMP_Text resultTitle;
        [SerializeField] TMP_Text resultBody;
        [SerializeField] StyledButton resultBack;

        GameController game;
        ToastView toast;
        Baskin.BState B;
        int sel = -1;
        bool actMode, retreatArmed, finished;
        float retreatArmedUntil;
        (int x, int y)? hover;

        public bool IsOpen => root != null && root.activeSelf && B != null;
        bool Busy => board.Busy;

        static readonly string[] DirTr = { Loc.N("kuzeye"), Loc.N("doğuya"), Loc.N("güneye"), Loc.N("batıya") };
        static readonly Dictionary<string, string> ObjShort = new Dictionary<string, string>
        { ["core"] = Loc.N("ÇEKİRDEK"), ["node"] = Loc.N("DÜĞÜM"), ["backup"] = Loc.N("YEDEK"), ["civ"] = Loc.N("SİVİL"), ["cool"] = Loc.N("SOĞUTMA"), ["spawner"] = Loc.N("KOPYA") };

        void Awake()
        {
            board.TileClicked += OnTileClick;
            board.HoverChanged += h => { hover = h; RenderOverlay(); RenderInfo(); };
            for (int i = 0; i < unitCards.Length; i++) { int k = i; UiUtil.OnClick(unitCards[i].Button.Button, () => SelectPlayer(k)); }
            UiUtil.OnClick(undo.Button, DoUndo);
            UiUtil.OnClick(endTurn.Button, DoEndTurn);
            UiUtil.OnClick(retreat.Button, DoRetreat);
            UiUtil.OnClick(briefStart.Button, () => { brief.SetActive(false); Render(); });
            UiUtil.OnClick(resultBack.Button, Finish);
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

        void OnDestroy() { if (game != null && !finished) game.Hold = false; }

        static RaidParams DemoParams() => new RaidParams
        {
            Seed = (int)(Time.realtimeSinceStartup * 1000) & 0xffff, Region = "cn", RegionName = GameData.Region("cn").Name,
            Compute = 5, IsCore = true, Inf = 60, Traits = new List<string> { "aldatma" },
        };

        void Open(RaidParams p, int day)
        {
            B = Baskin.Create(p);
            sel = -1; actMode = false; hover = null; retreatArmed = false; finished = false;
            if (game != null) { game.SetSpeed(0); game.Hold = true; }
            root.SetActive(true);
            board.Build(B);
            resultPanel.SetActive(false);
            brief.SetActive(true);
            title.text = Loc.F("SAHA OPERASYONU · <color=#{0}>{1}</color> · {2}", ColorUtility.ToHtmlStringRGB(theme.verify), UiUtil.Up(p.RegionName), UiUtil.Up(Loc.T(B.Layout)));
            briefEyebrow.text = Loc.F("SAHA OPERASYONU · GÜN {0} · {1}", day, UiUtil.Up(p.RegionName));
            var mods = new List<string>();
            if (B.Deceptive) mods.Add(Loc.T("Aldatma ya da Medya Kontrolü: bazı niyet okları yalan; Analist yaklaştıkça kesinleşir."));
            if (p.Traits.Contains("kopya")) mods.Add(Loc.T("Kendini Kopyalama: her tur 2 robot uyanır."));
            if (B.HasBackup) mods.Add(Loc.T("Yedekleme: binada bir de yedek çekirdek var; ikisi de kırılmalı."));
            if (p.Traits.Contains("hizlanma")) mods.Add(Loc.T("Hızlanma: 1 tur eksik."));
            briefBody.text =
                Loc.F("Hedef: {0} {1} tur içinde kır{2}. Binayı YZ’nin ele geçirdiği makineler koruyor.\n\n", (B.IsCore ? Loc.T("<b>YZ’nin çekirdek dolabını</b>") : Loc.T("<b>YZ’nin düğüm dolabını</b>")), B.MaxTurns, (B.HasBackup ? Loc.T(", <b>yedek çekirdeği</b> de") : "")) +
                Loc.T("• <b>Kırmızı oklar</b> robotların bu tur ne yapacağını gösterir; hamlenden sonra sırayla uygulanır (numaralar sıra). Robotu itersen saldırısı yeni yerinden yapılır.\n") +
                Loc.T("• <b>Birim seç</b> (tıkla ya da 1–3), mavi kareye taşı, turuncu çerçeveli hedefe yeteneğini kullan. Hedefin üstüne gelince sonucu önizlersin; <b>Geri al</b> turdaki hamleleri geri alır.\n") +
                Loc.T("• <b>Hedef dolaplar zırhlıdır:</b> yalnızca Teknisyen’in sabotajı, dolaba itilen robot ve patlayan soğutma ünitesi hasar verir.\n") +
                Loc.T("• <b>Sivil sunucuları koru:</b> kırılan her biri bölgenin ekonomisine ve güvenine işler. Elektrikli kanala itilen 1 hasar alır.") +
                (mods.Count > 0 ? Loc.T("\n\n<b>YZ’nin yetenekleri bu binayı değiştirdi:</b>\n") + string.Join("\n", mods.Select(m => "– " + m)) : "");
            Render();
        }

        void Finish()
        {
            if (finished || B == null) return;
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
            if (B == null || brief.activeSelf || Busy) return;
            var ps = Players;
            if (index >= ps.Count || ps[index].Dead) return;
            sel = ps[index].Id;
            actMode = !Baskin.CanMove(B, ps[index]);
            Render();
        }

        void OnTileClick(int x, int y)
        {
            if (B == null || B.Phase != "player" || brief.activeSelf || Busy) return;
            var u = Selected;
            if (u != null)
            {
                bool canMoveHere = !actMode && Baskin.CanMove(B, u) && Baskin.Reachable(B, u).Any(t => t.X == x && t.Y == y);
                if (canMoveHere) { StartCoroutine(MoveRoutine(u.Id, x, y)); return; }
                if (Baskin.CanAct(B, u) && Baskin.Targets(B, u).Any(t => t.X == x && t.Y == y)) { StartCoroutine(ActRoutine(u.Id, x, y)); return; }
            }
            var v = Baskin.UnitAt(B, x, y);
            if (v != null && v.Side == 'p') { sel = v.Id; actMode = !Baskin.CanMove(B, v); }
            else sel = -1;
            Render();
        }

        IEnumerator MoveRoutine(int id, int x, int y)
        {
            Baskin.DoMove(B, id, x, y);
            actMode = true;
            board.ClearPreview();
            yield return board.AnimateMove(id, x, y);
            Render();
        }

        IEnumerator ActRoutine(int id, int x, int y)
        {
            Baskin.DoAct(B, id, x, y);
            sel = -1;
            yield return board.Animate(B, new List<Baskin.Fx>(B.FxList), false);
            Render();
        }

        IEnumerator EndTurnRoutine()
        {
            sel = -1; actMode = false;
            Render();   // seçim halkaları ve önizleme kalksın; niyet okları görünür kalır
            if (!Baskin.EndTurn(B)) yield break;
            yield return board.Animate(B, new List<Baskin.Fx>(B.FxList), true);
            Render();
        }

        void DoUndo() { if (B != null && !Busy && Baskin.Undo(B)) { actMode = false; Render(); } }
        void DoEndTurn() { if (B == null || brief.activeSelf || Busy || B.Phase != "player") return; StartCoroutine(EndTurnRoutine()); }

        void DoRetreat()
        {
            if (B == null || B.Phase != "player" || Busy) return;
            if (!retreatArmed || Time.unscaledTime > retreatArmedUntil) { retreatArmed = true; retreatArmedUntil = Time.unscaledTime + 3; retreat.SetText(Loc.T("Emin misin? Tekrar bas")); return; }
            Baskin.Retreat(B);
            Render();
        }

        void Update()
        {
            if (!IsOpen || B == null) return;
            if (retreatArmed && Time.unscaledTime > retreatArmedUntil) { retreatArmed = false; retreat.SetText(Loc.T("Geri çekil")); }
            var kb = Keyboard.current;
            if (kb == null) return;
            if (brief.activeSelf) { if (kb.enterKey.wasPressedThisFrame) { brief.SetActive(false); Render(); } return; }
            if (resultPanel.activeSelf) { if (kb.enterKey.wasPressedThisFrame) Finish(); return; }
            if (Busy) return;
            if (kb.digit1Key.wasPressedThisFrame) SelectPlayer(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectPlayer(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectPlayer(2);
            if (kb.zKey.wasPressedThisFrame) DoUndo();
            if (kb.qKey.wasPressedThisFrame && Selected != null) { actMode = true; Render(); }
            if (kb.enterKey.wasPressedThisFrame) DoEndTurn();
            if (kb.escapeKey.wasPressedThisFrame) { sel = -1; Render(); }
        }

        // ---------- Çizim ----------
        static string Pips(int hp, int max) => new string('■', Mathf.Max(0, hp)) + new string('□', Mathf.Max(0, max - hp));

        void Render()
        {
            if (B == null) return;
            var u0 = Selected;
            var m = new RaidBoard3D.Marks { Selected = sel };
            if (u0 != null && !actMode && Baskin.CanMove(B, u0)) foreach (var p in Baskin.Reachable(B, u0)) m.Moves.Add(p.Y * Baskin.N + p.X);
            if (u0 != null && Baskin.CanAct(B, u0)) foreach (var p in Baskin.Targets(B, u0)) m.Targets.Add(p.Y * Baskin.N + p.X);
            foreach (var i in Baskin.Intents(B))
            {
                m.Badges[i.Unit] = (i.Order.ToString(), i.Uncertain ? "?" : i.IsScanned ? "✓" : null, i.Uncertain ? theme.warn : theme.verify);
                if (!i.HasTarget || i.TargetNone) continue;
                int k = i.TargetY * Baskin.N + i.TargetX;
                if (i.Kind == "repair") m.Heal.Add(k);
                else m.Danger[k] = (m.Danger.TryGetValue(k, out var sure) && sure) || !i.Uncertain;
                m.Arrows.Add((i.FromX, i.FromY, i.TargetX, i.TargetY, i.Kind == "repair" ? theme.good : theme.threat, i.Uncertain));
            }
            foreach (var s in B.Spawns) if (Baskin.UnitAt(B, s.X, s.Y) == null) m.Spawns.Add(s.Y * Baskin.N + s.X);
            foreach (var u in B.Units) if (u.Side == 'p' && !u.Dead && !Baskin.CanAct(B, u)) m.Spent.Add(u.Id);
            board.Sync(B, m);
            RenderOverlay();
            RenderSide();
            RenderInfo();
            if (B.Phase != "player" && !resultPanel.activeSelf) ShowResult();
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
                if (f.K == "armor") dmg[k] = (f.X, f.Y, 0, Loc.T("zırh"));
            }
            board.SetPreview(
                dmg.Values.Select(d => ((float)d.x, (float)d.y, d.label ?? "−" + d.n, d.label != null ? theme.good : theme.threat)).ToList(),
                pv.Moves.Select(mv => (mv.From.X, mv.From.Y, mv.To.X, mv.To.Y)).ToList());
            if (pv.Won && winLabel != null) { winLabel.text = Loc.T("HEDEF KIRILIR"); winLabel.gameObject.SetActive(true); }
        }

        void RenderSide()
        {
            bool playing = B.Phase == "player";
            turn.text = playing ? Loc.F("Tur {0}/{1}", B.Turn, B.MaxTurns) : B.Phase == "won" ? Loc.T("Başarılı") : Loc.T("Başarısız");
            turn.color = playing && B.Turn == B.MaxTurns ? theme.threat : theme.fg;
            var left = Baskin.TargetsLeft(B);
            var lines = left.Count > 0 ? left.Select(t => Loc.F("{0}: <b>{1}/{2} can</b>", Loc.T(Baskin.ObjNames[t.O.K]), t.O.Hp, t.O.Max)).ToList() : new List<string> { Loc.T("Hedef dolaplar: <b>kırıldı</b>") };
            lines.Add(Loc.F("Kırılan sivil sunucu: <b>{0}</b>", B.Collateral));
            lines.Add(Loc.F("Kalan tur: <b>{0}</b>", Mathf.Max(0, B.MaxTurns - B.Turn + (playing ? 1 : 0))));
            objectives.text = string.Join("\n", lines);
            var ps = Players;
            for (int i = 0; i < unitCards.Length && i < ps.Count; i++)
            {
                var u = ps[i]; var T = Baskin.UnitTypes[u.Type]; var A = Baskin.Abilities[T.Ability];
                string st = u.Dead ? Loc.T("düştü") : !Baskin.CanAct(B, u) ? Loc.T("tur bitti") : Baskin.CanMove(B, u) ? Loc.T("hareket + yetenek") : Loc.T("yetenek");
                unitCards[i].Set(T.Short, $"{i + 1}. {T.Name}", Loc.F("{0}/{1} can · {2}", u.Hp, u.Max, st), $"<b>{Loc.T(A.Name)}:</b> {Loc.T(A.Desc)}", u.Id == sel, u.Dead, theme);
            }
            undo.SetEnabled(playing && B.Hist.Count > 0);
            endTurn.SetEnabled(playing);
            retreat.SetEnabled(playing);
            var log = B.Log.AsEnumerable().Reverse().Take(14).ToList();
            var rows = UiUtil.Respawn(logLines, logLinePrefab, log.Count);
            for (int i = 0; i < log.Count; i++)
            {
                rows[i].text = log[i].Text;
                rows[i].color = log[i].Kind switch { "ai" => theme.threat, "good" => theme.good, "bad" => theme.warn, "turn" => theme.dim, _ => theme.muted };
            }
        }

        void RenderInfo()
        {
            if (B == null) return;
            var u = Selected;
            string txt = u != null
                ? Loc.F("<b>{0}</b> seçili. ", Baskin.UnitTypes[u.Type].Name) + (!actMode && Baskin.CanMove(B, u) ? Loc.T("Mavi kareye taşı ya da turuncu hedefe yeteneğini kullan (Q: yetenek).") : Baskin.CanAct(B, u) ? Loc.T("Turuncu hedefe yeteneğini kullan.") : Loc.T("Bu birim bu tur işini bitirdi."))
                : Loc.T("Bir birim seç (1–3). Robotların niyetleri kırmızı; sırayla uygulanır. Enter: turu bitir · Z: geri al.");
            if (hover != null)
            {
                var (x, y) = hover.Value;
                var c = Baskin.CellAt(B, x, y);
                var v = Baskin.UnitAt(B, x, y);
                if (v != null && v.Side == 'e')
                {
                    var i = Baskin.Intents(B).FirstOrDefault(z => z.Unit == v.Id);
                    string what = i == null ? Loc.T("bekliyor") : i.Kind == "repair" ? Loc.T("hedef dolabı onaracak") : i.Kind == "shoot" ? Loc.F("{0} ateş edecek (1 hasar)", Loc.T(DirTr[i.Dir])) : Loc.F("{0} saldıracak (2 hasar)", Loc.T(DirTr[i.Dir]));
                    txt = Loc.F("<b>{0}</b> · {1}/{2} can · sıra {3}: {4}.", Baskin.UnitTypes[v.Type].Name, v.Hp, v.Max, (i != null ? i.Order.ToString() : "—"), what)
                        + (i != null && i.Uncertain ? Loc.F(" <color=#{0}>Bu niyet yalan olabilir; Analisti 3 kare yaklaştır.</color>", ColorUtility.ToHtmlStringRGB(theme.warn)) : i != null && i.IsScanned ? Loc.T(" Analist doğruladı.") : "");
                }
                else if (v != null) txt = Loc.F("<b>{0}</b> · {1}/{2} can.", Baskin.UnitTypes[v.Type].Name, v.Hp, v.Max);
                else if (c.O != null)
                    txt = Loc.F("<b>{0}</b> · {1}/{2} can.", Loc.T(Baskin.ObjNames[c.O.K]), c.O.Hp, c.O.Max) + (Baskin.IsTarget(c.O.K) ? Loc.T(" Zırhlı: yalnızca sabotaj, çarpma ve patlama işler.") : c.O.K == "civ" ? Loc.T(" Kırılırsa yan hasar.") : c.O.K == "cool" ? Loc.T(" Kırılınca çevresindeki 4 kareye 1 hasar.") : Loc.T(" Kırılırsa buradan robot uyanmaz."));
                else if (c.T == "cable") txt = Loc.T("<b>Elektrikli kanal</b> · üstüne itilen 1 hasar alır.");
                else if (c.T == "wall") txt = Loc.T("<b>Duvar</b>");
            }
            info.text = txt;
        }

        void ShowResult()
        {
            var res = Baskin.Result(B);
            resultPanel.SetActive(true);
            resultEyebrow.text = Loc.F("SAHA OPERASYONU · {0} · {1}. TUR", UiUtil.Up(B.Params.RegionName), res.Turns);
            resultTitle.text = res.Success ? Loc.T("Operasyon başarılı") : Loc.T("Operasyon başarısız");
            resultTitle.color = res.Success ? theme.good : theme.threat;
            resultBody.text = Loc.F("{0}\n\nHedef: <b>{1}</b> · Kırılan sivil sunucu: <b>{2}</b> · Düşen: <b>{3}/3</b> · Tur: <b>{4}</b>", res.Reason, (res.Success ? Loc.T("kırıldı") : Loc.T("ayakta")), res.Collateral, res.Down, res.Turns);
        }
    }
}
