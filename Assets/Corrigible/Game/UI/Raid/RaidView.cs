using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Deneysel mod: baskın ekranı (Baskin canvas'ı). Taktik savaşın kuralları Core'daki Baskin sınıfında; burada çizim ve girdi.
    /// Strateji simülasyonu bir baskın başlatınca (SimEventType.Raid) açılır; bitince sonuç GameController.ResolveRaid ile döner.
    /// </summary>
    public sealed class RaidView : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] ToastView toast;
        [SerializeField] UiTheme theme;
        [SerializeField] GameObject root;

        [Header("Üst")]
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text turn;

        [Header("Tahta")]
        [Tooltip("64 kare; sıra: satır satır (y), soldan sağa (x).")]
        [SerializeField] RaidTileView[] tiles = new RaidTileView[64];
        [SerializeField] RaidOverlayGraphic overlay;
        [SerializeField] RectTransform labels;
        [SerializeField] TMP_Text labelPrefab;

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

        Baskin.BState B;
        int sel = -1;
        bool actMode, retreatArmed;
        float retreatArmedUntil;
        (int x, int y)? hover;
        readonly List<TMP_Text> labelPool = new List<TMP_Text>();

        public bool IsOpen => root != null && root.activeSelf;

        static readonly string[] DirTr = { Loc.N("kuzeye"), Loc.N("doğuya"), Loc.N("güneye"), Loc.N("batıya") };
        static readonly Dictionary<string, string> ObjShort = new Dictionary<string, string>
        { ["core"] = Loc.N("ÇEKİRDEK"), ["node"] = Loc.N("DÜĞÜM"), ["backup"] = Loc.N("YEDEK"), ["civ"] = Loc.N("SİVİL"), ["cool"] = Loc.N("SOĞUTMA"), ["spawner"] = Loc.N("KOPYA") };

        void Awake()
        {
            root.SetActive(false);
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].X = i % Baskin.N; tiles[i].Y = i / Baskin.N;
                tiles[i].Clicked += OnTileClick;
                tiles[i].Hovered += (x, y) => { hover = (x, y); RenderOverlay(); RenderInfo(); };
            }
            for (int i = 0; i < unitCards.Length; i++) { int k = i; UiUtil.OnClick(unitCards[i].Button.Button, () => SelectPlayer(k)); }
            UiUtil.OnClick(undo.Button, DoUndo);
            UiUtil.OnClick(endTurn.Button, DoEndTurn);
            UiUtil.OnClick(retreat.Button, DoRetreat);
            UiUtil.OnClick(briefStart.Button, () => { brief.SetActive(false); Render(); });
            UiUtil.OnClick(resultBack.Button, Finish);
        }

        void Start()
        {
            game.EventsRaised += OnEvents;
            game.GameLoaded += SyncWithSim;
            game.NewGameStarted += SyncWithSim;
            SyncWithSim();
        }

        void OnDestroy()
        {
            if (game == null) return;
            game.EventsRaised -= OnEvents;
            game.GameLoaded -= SyncWithSim;
            game.NewGameStarted -= SyncWithSim;
        }

        void OnEvents(List<SimEvent> events)
        {
            if (events.Any(e => e.Type == SimEventType.Raid) && game.Sim.S.Raid != null) Open();
        }

        // Kayıttan yüklenen oyunda süren bir baskın varsa baştan açılır; yeni oyunda ekran kapanır
        void SyncWithSim()
        {
            if (game.Sim != null && game.Sim.S.Raid != null) Open();
            else if (IsOpen) { root.SetActive(false); game.Hold = false; }
        }

        void Open()
        {
            B = Baskin.Create(game.Sim.S.Raid.Params);
            sel = -1; actMode = false; hover = null; retreatArmed = false;
            game.SetSpeed(0);
            game.Hold = true;
            root.SetActive(true);
            resultPanel.SetActive(false);
            brief.SetActive(true);
            var p = B.Params;
            title.text = Loc.F("SAHA OPERASYONU · <color=#{0}>{1}</color> · {2}", ColorUtility.ToHtmlStringRGB(theme.verify), UiUtil.Up(p.RegionName), UiUtil.Up(Loc.T(B.Layout)));
            briefEyebrow.text = Loc.F("SAHA OPERASYONU · GÜN {0} · {1}", game.Sim.S.Day, UiUtil.Up(p.RegionName));
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
            var res = Baskin.Result(B);
            B = null;
            root.SetActive(false);
            game.Hold = false;
            var r = game.ResolveRaid(res);
            if (r.Msg != null) toast.Show(r.Msg, res.Success);
        }

        // ---------- Girdi ----------
        Baskin.Unit Selected => B == null || sel < 0 ? null : B.Units.FirstOrDefault(u => u.Id == sel && !u.Dead);
        List<Baskin.Unit> Players => B.Units.Where(u => u.Side == 'p').ToList();

        void SelectPlayer(int index)
        {
            if (B == null || brief.activeSelf) return;
            var ps = Players;
            if (index >= ps.Count || ps[index].Dead) return;
            sel = ps[index].Id;
            actMode = !Baskin.CanMove(B, ps[index]);
            Render();
        }

        void OnTileClick(int x, int y)
        {
            if (B == null || B.Phase != "player" || brief.activeSelf) return;
            var u = Selected;
            if (u != null)
            {
                bool canMoveHere = !actMode && Baskin.CanMove(B, u) && Baskin.Reachable(B, u).Any(t => t.X == x && t.Y == y);
                if (canMoveHere) { Baskin.DoMove(B, u.Id, x, y); actMode = true; Render(); return; }
                if (Baskin.CanAct(B, u) && Baskin.Targets(B, u).Any(t => t.X == x && t.Y == y)) { Baskin.DoAct(B, u.Id, x, y); sel = -1; Render(); return; }
            }
            var v = Baskin.UnitAt(B, x, y);
            if (v != null && v.Side == 'p') { sel = v.Id; actMode = !Baskin.CanMove(B, v); }
            else sel = -1;
            Render();
        }

        void DoUndo() { if (B != null && Baskin.Undo(B)) { actMode = false; Render(); } }
        void DoEndTurn() { if (B == null || brief.activeSelf) return; if (Baskin.EndTurn(B)) { sel = -1; actMode = false; Render(); } }

        void DoRetreat()
        {
            if (B == null || B.Phase != "player") return;
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
            var moves = u0 != null && !actMode && Baskin.CanMove(B, u0) ? Baskin.Reachable(B, u0) : new List<Baskin.Pt>();
            var targets = u0 != null && Baskin.CanAct(B, u0) ? Baskin.Targets(B, u0) : new List<Baskin.Pt>();
            var intents = Baskin.Intents(B);
            var danger = new Dictionary<int, bool>(); // anahtar → kesin mi
            var heal = new HashSet<int>();
            foreach (var i in intents)
            {
                if (!i.HasTarget || i.TargetNone) continue;
                int k = i.TargetY * Baskin.N + i.TargetX;
                if (i.Kind == "repair") heal.Add(k);
                else danger[k] = (danger.TryGetValue(k, out var sure) && sure) || !i.Uncertain;
            }
            var byUnit = intents.ToDictionary(i => i.Unit);
            var spawn = new HashSet<int>(B.Spawns.Select(s => s.Y * Baskin.N + s.X));

            foreach (var t in tiles)
            {
                int key = t.Y * Baskin.N + t.X;
                var c = Baskin.CellAt(B, t.X, t.Y);
                t.Ground.color = c.T == "wall" ? Palette.Hex("#2a3e47") : c.T == "cable" ? Palette.Hex("#231e10") : Palette.Hex("#0f181c");
                t.GroundMark.text = c.T == "cable" ? "≈" : "";
                bool mv = moves.Any(m => m.X == t.X && m.Y == t.Y), tg = !mv && targets.Any(m => m.X == t.X && m.Y == t.Y);
                Color tint = Color.clear;
                if (mv) tint = Palette.Alpha(theme.verify, 0.24f);
                if (danger.TryGetValue(key, out var sureD)) tint = sureD ? Palette.Alpha(theme.threat, 0.26f) : Palette.Alpha(theme.warn, 0.2f);
                else if (heal.Contains(key)) tint = Palette.Alpha(theme.good, 0.22f);
                t.Tint.color = tint;
                t.TargetFrame.color = tg ? theme.warn : Color.clear;

                if (c.O != null)
                {
                    bool tgtObj = Baskin.IsTarget(c.O.K);
                    var bg = tgtObj ? Palette.Hex("#5a1f1a") : c.O.K == "civ" ? Palette.Hex("#172a33") : c.O.K == "cool" ? Palette.Hex("#10262b") : Palette.Hex("#24110f");
                    var fg = tgtObj ? Palette.Hex("#ffd7d2") : c.O.K == "civ" ? Palette.Hex("#a9c8d3") : c.O.K == "cool" ? theme.verify : Palette.Hex("#ff8a7d");
                    t.SetObj(true, bg, Loc.T(ObjShort[c.O.K]), Pips(c.O.Hp, c.O.Max), fg);
                }
                else t.SetObj(false, default, null, null, default);

                var un = Baskin.UnitAt(B, t.X, t.Y);
                if (un != null)
                {
                    var T = Baskin.UnitTypes[un.Type];
                    if (un.Side == 'p')
                    {
                        bool spent = !Baskin.CanAct(B, un);
                        t.SetUnit(true, ShapeGraphic.Kind.Disc, Palette.Hex("#0d2f36"), un.Id == sel ? theme.fg : theme.verify, un.Id == sel ? 3.5f : 2f, T.Short, Pips(un.Hp, un.Max), Palette.Hex("#c9f3fa"), spent ? 0.55f : 1f);
                        t.SetBadges(null, null, default);
                    }
                    else
                    {
                        t.SetUnit(true, un.Type == "dron" ? ShapeGraphic.Kind.Triangle : ShapeGraphic.Kind.Square, un.Type == "dron" ? theme.threat : Palette.Hex("#5a1f1a"), un.Type == "dron" ? Color.clear : theme.threat, 2f,
                            un.Type == "dron" ? "" : T.Short, Pips(un.Hp, un.Max), un.Type == "dron" ? theme.inkOnThreat : Palette.Hex("#ffc9c2"), 1f);
                        byUnit.TryGetValue(un.Id, out var iv);
                        t.SetBadges(iv != null ? iv.Order.ToString() : null, iv != null && iv.Uncertain ? "?" : iv != null && iv.IsScanned ? "✓" : null, iv != null && iv.Uncertain ? theme.warn : theme.verify);
                    }
                }
                else { t.SetUnit(false, default, default, default, 0, null, null, default, 1); t.SetBadges(null, null, default); }
                t.SetSpawn(spawn.Contains(key) && un == null);
            }
            RenderOverlay();
            RenderSide();
            RenderInfo();
            if (B.Phase != "player" && !resultPanel.activeSelf) ShowResult();
        }

        void RenderOverlay()
        {
            if (B == null) return;
            overlay.Clear();
            foreach (var l in labelPool) l.gameObject.SetActive(false);
            int used = 0;
            foreach (var i in Baskin.Intents(B))
            {
                if (!i.HasTarget || i.TargetNone) continue;
                overlay.Add(new RaidOverlayGraphic.Arrow
                {
                    From = new Vector2(i.FromX + 0.5f, i.FromY + 0.5f), To = new Vector2(i.TargetX + 0.5f, i.TargetY + 0.5f),
                    Color = i.Kind == "repair" ? theme.good : theme.threat, Dashed = i.Uncertain,
                });
            }
            // Önizleme: seçili birimin yeteneği üzerine gelinen kareye uygulanırsa
            var u = Selected;
            if (u == null || hover == null || B.Phase != "player" || !Baskin.CanAct(B, u)) return;
            var (hx, hy) = hover.Value;
            bool isMove = !actMode && Baskin.CanMove(B, u) && Baskin.Reachable(B, u).Any(t => t.X == hx && t.Y == hy);
            if (isMove || !Baskin.Targets(B, u).Any(t => t.X == hx && t.Y == hy)) return;
            var pv = Baskin.Preview(B, u.Id, hx, hy);
            var dmg = new Dictionary<int, (int x, int y, int n, string label)>();
            foreach (var f in pv.Fx)
            {
                int k = f.Y * Baskin.N + f.X;
                if (f.K == "dmg") { dmg.TryGetValue(k, out var d); dmg[k] = (f.X, f.Y, d.n + f.N, null); }
                if (f.K == "armor") dmg[k] = (f.X, f.Y, 0, Loc.T("zırh"));
            }
            foreach (var m in pv.Moves)
            {
                overlay.Add(new RaidOverlayGraphic.Arrow { From = new Vector2(m.From.X + 0.5f, m.From.Y + 0.5f), To = new Vector2(m.To.X + 0.5f, m.To.Y + 0.5f), Color = theme.fg });
                overlay.Ghost(m.To.X, m.To.Y, theme.fg);
            }
            foreach (var d in dmg.Values) Label(ref used, d.x, d.y, d.label ?? "−" + d.n, d.label != null ? theme.good : theme.threat);
            if (pv.Won) Label(ref used, 3.5f, 0.18f, Loc.T("HEDEF KIRILIR"), theme.good);
        }

        void Label(ref int used, float x, float y, string text, Color color)
        {
            if (used == labelPool.Count) labelPool.Add(Instantiate(labelPrefab, labels));
            var t = labelPool[used++];
            t.gameObject.SetActive(true);
            t.text = text; t.color = color;   // prefab'ın kalın koyu dış çizgisi kırmızı robotun üstünde de okutur
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2((x + 0.5f) / 8f, 1 - (y + 0.22f) / 8f);
            rt.anchoredPosition = Vector2.zero;
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
