using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Vardiya geçişinin oyun bağlantısı (Docs/UI_Siginak Aşama 8, K6). Kol inince: kapanış öncesi anlık görüntü ve
    /// vardiyanın fon hareketleri alınır, vardiya bitirilir (simülasyon hemen ilerler ve kaydedilir), sonra sunum
    /// <see cref="ShiftSequence"/>'ın çizelgesiyle yetişir: sayaçlar 0,40'ta döner (o ana kadar eski değerde bekler),
    /// teleks 0,60'ta “VARDİYA 014 KAPANDI” basar, kamera 1,60'ta harita pozuna geçer, rapor 2,20'de düşer, fişler asılır,
    /// iyileşen ekibin figürü S-07'den cepheye kayar (“ALFA CEPHEDE”). Boşluk / ATLA diziyi keser. Rapor açıkken masa
    /// kilitlidir (şalter devre dışı); Enter, düğme ya da Esc raporu EVRAK tepsisine dosyalar, kamera masa pozuna döner.
    /// Tepsiye tıklayınca son 5 rapor okunur (← →). Raporlar kayda girmez, yan dosyada saklanır. Karşı saldırıda (Revizyon
    /// 2 · S5b) rapordan önce duvardaki alarm lambası üç kez yanar, teleks “YZ KARŞI SALDIRISI” basar (kodu raporun kutusunda),
    /// kutu basılınca haritada sektöre ✕ çizilir ve YENİDEN İŞGAL damgası iner. Özet gözünün üzerine gelince sayacı kalkar.
    /// <para>
    /// Kartlı oyunda (<c>Board != null</c>) yukarıdakilerin hiçbiri çalışmaz: kol <see cref="SiginakShiftClose.Throw"/>'a gider
    /// (devir fişi, EVRAK, dosyalama; Revizyon 3 · R19), tepsi son devir fişini okutur, kayıttan devamın rapor parlaması atlanır
    /// ve EVRAK'a Revizyon 2 raporu eklenmez (Karar D7-A). Kapanış meşgulken masa kilitlidir (<see cref="Busy"/>).
    /// </para>
    /// </summary>
    public sealed class SiginakShift : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakDesk desk;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] SiginakMap map;
        [SerializeField] ShiftSwitch shiftSwitch;
        [SerializeField] TelexFeed telex;
        [SerializeField] CounterBank counters;
        [SerializeField] ShiftSequence sequence;
        [SerializeField] ShiftReportView report;
        [SerializeField] EvrakTray tray;
        [SerializeField] Camera view;
        [Tooltip("Tepside ilk rapordan önce (←) emir belgesi.")]
        [SerializeField] SiginakBriefing briefing;
        [Tooltip("Kartlı oyunun vardiya kapanışı (Revizyon 3 · R19): şalterin kolu, devir fişi, EVRAK arşivi.")]
        [SerializeField] SiginakShiftClose close;

        ShiftReportInput pending, shown;
        int closing, browseIndex = -1;
        bool holdCounters, fresh;
        float flashUntil;
        System.Action flashDone;

        Simulation Sim => game != null ? game.Sim : null;
        public bool Playing => sequence != null && sequence.Playing;
        public bool ReportOpen => report != null && report.IsOpen;
        /// <summary>Dizi, rapor ya da kartlı oyunun kapanışı (fiş elde, sürükleniyor, dosyalanıyor): masa kilitli (LiberationDeskView.InputBlocked).</summary>
        public bool Busy => Playing || ReportOpen || report != null && report.Animating || flashDone != null || close != null && close.Busy;
        /// <summary>Sayaçlar dizinin 0,40'ına kadar eski değerde bekler (SiginakDesk.Refresh yazmaz).</summary>
        public bool HoldCounters => holdCounters;
        public EvrakTray Tray => tray;

        public void Configure(GameController controller, LiberationDeskView deskView, SiginakDesk deskControls, SiginakCameraRig cameraRig, SiginakMap liberationMap,
            ShiftSwitch lever, TelexFeed feed, CounterBank bank, ShiftSequence shiftSequence, ShiftReportView reportView, EvrakTray paperTray, Camera camera,
            SiginakBriefing orderPaper)
        {
            game = controller; surface = deskView; desk = deskControls; rig = cameraRig; map = liberationMap; shiftSwitch = lever; telex = feed;
            counters = bank; sequence = shiftSequence; report = reportView; tray = paperTray; view = camera; briefing = orderPaper;
        }

        void OnEnable()
        {
            if (game != null) { game.GameLoaded += Restore; game.NewGameStarted += NewGame; }
            DeskLoc.Instance.Changed += Relabel;
            shiftSwitch.Thrown += Throw;
            report.ActionClicked += File;
            report.PreviousClicked += Previous;
            report.NextClicked += Next;
            report.EyeHovered += EyeHovered;
            report.CounterHovered += map.BlinkCounterattack;
            report.CounterStamped += CounterStamped;
            Restore();
        }

        void OnDisable()
        {
            if (game != null) { game.GameLoaded -= Restore; game.NewGameStarted -= NewGame; }
            DeskLoc.Instance.Changed -= Relabel;
            shiftSwitch.Thrown -= Throw;
            report.ActionClicked -= File;
            report.PreviousClicked -= Previous;
            report.NextClicked -= Next;
            report.EyeHovered -= EyeHovered;
            report.CounterHovered -= map.BlinkCounterattack;
            report.CounterStamped -= CounterStamped;
            Abort();
        }

        // ---------- Dizi ----------

        void Throw()
        {
            var sim = Sim;
            if (sim == null) { shiftSwitch.Release(); return; }
            // Kartlı oyun: basım, fiş ve dosyalama SiginakShiftClose'ta (Revizyon 3 · R19); eski dizi çalışmaz
            if (sim.Board != null)
            {
                if (close != null) close.Throw();
                else shiftSwitch.Release();
                return;
            }
            closing = surface.Shift;
            var before = ShiftReportBuilder.Capture(sim);
            var ledger = ShiftReportBuilder.Ledger(sim);
            var failures = ShiftReportBuilder.Failures(telex.Lines, closing).ToList();
            // Adımın Changed bildirimi sayaçları ve figürü yazmasın: sunum diziyle yetişir
            holdCounters = true;
            map.HoldFigure = true;
            if (!surface.AdvanceShift())
            {
                holdCounters = false;
                map.ReleaseFigure(true);
                telex.Post(closing, Loc.T("YAPILAMADI"), Loc.T("Vardiya şu anda bitirilemez."), TelexKind.Failed);
                shiftSwitch.Release();
                return;
            }
            pending = ShiftReportBuilder.Input(sim, before, ledger, failures);
            rig.Locked = true;
            rig.Focus(SiginakPose.Shift, SiginakTheme.Active.Number("motion.shift.approachS", 0.4f));
            sequence.Play(Cue);
        }

        void Cue(ShiftSequence.Cue cue, bool skipped)
        {
            var t = SiginakTheme.Active;
            switch (cue)
            {
                case ShiftSequence.Cue.Counters:
                    holdCounters = false;
                    desk.ShowCounters(!skipped, t.Number("motion.shift.moduleStaggerS", 0.3f), t.Number("motion.shift.shiftWheelS", 0.6f), false);
                    break;
                case ShiftSequence.Cue.Telex:
                    if (pending != null)
                        pending.code = telex.Post(closing, Loc.F("VARDİYA {0:000} KAPANDI", closing), Loc.T("Rapor basılıyor…"), TelexKind.Info).code;
                    // Karşı saldırı: rapordan önce duvardaki alarm lambası üç kez yanar (alçak uğultu)
                    if (pending != null && pending.after.counterattacks > pending.before.counterattacks && !skipped)
                    {
                        desk.FlashAlarm();
                        Sfx.Play(Corrigible.Game.Audio.Cue.AlarmHum, 0.7f);
                    }
                    break;
                case ShiftSequence.Cue.Camera:
                    if (skipped) rig.Focus(SiginakPose.Map, true);
                    else rig.Focus(SiginakPose.Map, t.Number("motion.shift.cameraS", 0.6f));
                    break;
                case ShiftSequence.Cue.Report:
                    ShowFresh(skipped);
                    break;
                case ShiftSequence.Cue.End:
                    // Atlandıysa dönmekte olan çarklar da son değerine oturur
                    if (skipped) desk.ShowCounters(false, 0, -1, false);
                    shiftSwitch.Release();
                    break;
            }
        }

        /// <summary>2,20: rapor teleksten koparılır, fişler asılır, iyileşen ekip cepheye kayar.</summary>
        void ShowFresh(bool skipped)
        {
            if (pending == null) return;
            var data = ShiftReportBuilder.Build(pending);
            // Karşı saldırı: teleks satırı önce basılır, kodu raporun kutusuna yazılır (dosyalanan raporda da)
            if (data.Counterattack != null)
            {
                pending.counterCode = telex.Post(surface.Shift, Loc.T("YZ KARŞI SALDIRISI"), data.Counterattack, TelexKind.Alarm).code;
                data = ShiftReportBuilder.Build(pending);
            }
            tray.Add(pending);
            desk.SaveNotes();
            shown = pending;
            pending = null;
            fresh = true;
            browseIndex = tray.Reports.Count - 1;
            tray.SetSheetOut(true);
            report.Show(data, false, !skipped);
            PinSlips(data);
            map.ReleaseFigure(skipped);
            if (data.Counterattack != null && skipped) map.PlayCounterattack(0);
            if (data.TeamReturned && Sim?.S.Teams.FirstOrDefault() is TeamState team)
            {
                string name = DeskText.Upper(LiberationMapBuilder.TeamName(team));
                bool front = map.State != null && map.State.FrontOpen;
                telex.Post(surface.Shift, front ? Loc.F("{0} CEPHEDE", name) : Loc.F("{0} HAZIR", name),
                    front ? Loc.F("Ekip toparlandı · {0}", GameData.Region(map.State.Front).Name) : Loc.T("Ekip toparlandı · sıradaki cepheyi seç"),
                    TelexKind.Done);
            }
        }

        void PinSlips(ShiftReportData data)
        {
            var t = SiginakTheme.Active;
            var texts = new string[data.Slips.Length];
            var inks = new Color[data.Slips.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i] = data.Slips[i].text;
                // ± fişleri raporla aynı kural: artı yeşil, eksi mürekkep, ±0 gri (Revizyon 2 · S5)
                inks[i] = t.ColorOf(ShiftReportBuilder.ToneColor(data.Slips[i].tone));
            }
            counters.Pin(texts, inks);
        }

        /// <summary>Karşı saldırı kutusu basıldı: ağır “tak”, haritada ✕ ve YENİDEN İŞGAL damgası.</summary>
        void CounterStamped()
        {
            Sfx.Play(Corrigible.Game.Audio.Cue.StampHeavy, 0.9f);
            if (fresh) map.PlayCounterattack(0.12f);
        }

        /// <summary>Özet gözünün üzerine gelince ilgili sayaç (ve fişi) kalkar: ERZAK, FON; alarm gözü duvardaki panoyu anar.</summary>
        void EyeHovered(int eye, bool on)
        {
            if (eye == 0) counters[CounterBank.Slot.Supplies].Raise(on);
            else if (eye == 2) counters[CounterBank.Slot.Funds].Raise(on);
        }

        // ---------- Rapor ve tepsi ----------

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (Playing)
            {
                if (kb.spaceKey.wasPressedThisFrame) Skip();
                return;
            }
            if (flashDone != null)
            {
                if (Time.unscaledTime < flashUntil || report.Animating) return;
                var done = flashDone;
                flashDone = null;
                report.File(tray.DropPoint, view, () => { tray.SetSheetOut(false); done(); });
                return;
            }
            if (!ReportOpen || report.Animating) return;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame) File();
            else if (!fresh && kb.leftArrowKey.wasPressedThisFrame) Previous();
            else if (!fresh && kb.rightArrowKey.wasPressedThisFrame) Next();
        }

        /// <summary>Boşluk ya da ATLA plakası: dizi doğrudan son duruma geçer.</summary>
        public void Skip()
        {
            if (Playing) sequence.Skip();
        }

        /// <summary>
        /// EVRAK tepsisi: son raporu okuma görünümünde açar (rapor yoksa false). Kartlı oyunda son dosyalanan devir fişi elde salt
        /// okunur (<see cref="SiginakShiftClose.OpenTray"/>; Karar D7-A).
        /// </summary>
        public bool OpenTray()
        {
            if (Sim?.Board != null) return close != null && close.OpenTray();
            if (Busy || surface.InputBlocked || tray.Reports.Count == 0) return false;
            fresh = false;
            browseIndex = tray.Reports.Count - 1;
            ShowArchived(true);
            rig.Locked = true;
            return true;
        }

        /// <summary>
        /// KAYITTAN DEVAM (K7): dosyadaki son rapor <paramref name="seconds"/> görünür, sonra tepsiye döner ve
        /// <paramref name="done"/>. Rapor yoksa false. Kartlı oyunda rapor yoktur, parlama atlanır (Karar D7-A).
        /// </summary>
        public bool FlashLatest(float seconds, System.Action done)
        {
            if (Sim?.Board != null || tray.Reports.Count == 0 || Busy) return false;
            fresh = false;
            browseIndex = tray.Reports.Count - 1;
            shown = tray.Reports[browseIndex];
            tray.SetSheetOut(true);
            report.Show(Archived(shown), true, false);
            flashUntil = Time.unscaledTime + seconds;
            flashDone = done;
            return true;
        }

        void ShowArchived(bool open)
        {
            shown = tray.Reports[browseIndex];
            var data = Archived(shown);
            if (open) { tray.SetSheetOut(browseIndex == tray.Reports.Count - 1); report.Show(data, true, true); }
            else report.Bind(data, true);
        }

        ShiftReportData Archived(ShiftReportInput input)
        {
            var data = ShiftReportBuilder.Build(input);
            data.Action = Loc.T("TEPSİYE KOY");
            // ← → ve Esc satırda fiş olarak durur
            data.Hint = Loc.F("rapor {0}/{1} · tepsiye koy", browseIndex + 1, tray.Reports.Count);
            return data;
        }

        void Previous()
        {
            if (fresh || flashDone != null) return;
            if (browseIndex <= 0)
            {
                // Tepside raporların altında emir belgesi
                if (briefing == null || !tray.HasBriefing) return;
                report.Hide();
                tray.SetSheetOut(false);
                rig.Locked = false;
                briefing.OpenReading();
                return;
            }
            browseIndex--;
            tray.SetSheetOut(false);
            ShowArchived(false);
        }

        void Next()
        {
            if (fresh || flashDone != null || browseIndex >= tray.Reports.Count - 1) return;
            browseIndex++;
            tray.SetSheetOut(browseIndex == tray.Reports.Count - 1);
            ShowArchived(false);
        }

        /// <summary>Enter, düğme ya da Esc: rapor tepsiye kayar, fişler kalkar; yeni raporda kamera masa pozuna döner.</summary>
        void File()
        {
            if (!ReportOpen || report.Animating) return;
            bool wasFresh = fresh;
            fresh = false;
            report.File(tray.DropPoint, view, () => tray.SetSheetOut(false));
            counters.Unpin();
            rig.Locked = false;
            if (wasFresh) rig.ResetView();
        }

        // ---------- Durum ----------

        void NewGame() { Abort(); tray.Clear(); }

        /// <summary>
        /// Kayıttan devam: dizi ve rapor kapanır, EVRAK raporları yan dosyadan. Kartlı oyunda EVRAK devir fişlerini tutar
        /// (<see cref="SiginakShiftClose"/> kendi <c>GameLoaded</c>'ıyla geri koyar); Revizyon 2 raporu yüklenmez (Karar D7-A).
        /// </summary>
        void Restore()
        {
            Abort();
            tray.Restore(Sim?.Board != null ? null : SiginakDeskStore.LoadReports(Sim?.Record));
        }

        /// <summary>Dizi ve rapor bildirmeden kapanır; tutulan sayaç ve figür serbest kalır.</summary>
        void Abort()
        {
            if (sequence != null && sequence.Playing) sequence.Stop();
            if (report != null) report.Hide();
            if (rig != null) rig.Locked = false;
            if (counters != null) counters.Unpin();
            if (holdCounters) { holdCounters = false; if (desk != null) desk.ShowCounters(false); }
            if (map != null && map.HoldFigure) map.ReleaseFigure(true);
            // Basılı fiş beklenirken şalter inik kalır (Release no-op; kayıttan yüklemede şalter kalkmaz)
            if (shiftSwitch != null) shiftSwitch.Release();
            pending = shown = null;
            fresh = false;
            flashDone = null;
        }

        void Relabel()
        {
            tray.Relabel();
            if (!ReportOpen || shown == null) return;
            if (fresh) report.Bind(ShiftReportBuilder.Build(shown), false);
            else report.Bind(Archived(shown), true);
        }
    }
}
