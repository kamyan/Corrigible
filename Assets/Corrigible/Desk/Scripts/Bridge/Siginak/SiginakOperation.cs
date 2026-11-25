using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Baskından dönüşün oyun bağlantısı: operasyon raporu (Revizyon 2 · S7c/S7d). Baskın sahnesi kapanınca kamera CRT'den
    /// girişteki yolun tersiyle başlangıç pozuna döner (RaidLauncher → SiginakCrt.Depart; o sırada başlatıcı geçişte sayılır);
    /// harita pozunda değilse oraya geçer (0,6 s; karartma yok), kamera varınca rapor yukarıdan iner (0,4 s), iplik raporun iğnesinden ekip figürüne
    /// gerilir (0,25 s) ve figürün çevresine kırmızı halka çizilir (0,2 s). Sayaçlar o ana kadar eski değerde bekler, sonra
    /// döner; başarılı raporda ± fişleri asılır ve haritada geri alınan sektörün işareti mavi onaya döner. Enter, düğme ya
    /// da Esc: rapor EVRAK tepsisine kayar, fişler kalkar, kamera başlangıç pozuna döner, rapor okundu sayılır. Rapor
    /// açıkken masa kilitlidir. Oyun bittiyse rapor açılmaz (son rapor açılır).
    /// </summary>
    public sealed class SiginakOperation : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakDesk desk;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] SiginakMap map;
        [SerializeField] CounterBank counters;
        [SerializeField] OperationReportView report;
        [SerializeField] EvrakTray tray;
        [SerializeField] RaidLauncher launcher;
        [Tooltip("Tesis aşama 9: bölgeyi tamamlayan baskının raporu dosyalanınca sunucu ödülünün kâğıdı ele gelir.")]
        [SerializeField] SiginakServerReward serverReward;
        [Tooltip("Tesis aşama 10 (K40): rapor inerken sonucun teleks satırı (CRT'nin \"SONUÇ TELEKSTE\" sözü; yalnız tesis kipinde).")]
        [SerializeField] TelexFeed telex;
        [Header("Hareket (S7)")]
        [Tooltip("Kamera harita pozunda değilse oraya geçiş; rapor kamera varınca iner (en geç bu sürede).")]
        [SerializeField, Min(0.01f)] float cameraSeconds = 0.6f;
        [SerializeField, Min(0.01f)] float stringSeconds = 0.25f;
        [SerializeField, Min(0.01f)] float ringSeconds = 0.2f;
        [Tooltip("MASAYA DÖN: kamera başlangıç pozuna.")]
        [SerializeField, Min(0.01f)] float deskSeconds = 0.6f;

        OperationReportData data;
        /// <summary>Sonuç teleksinin satırı (Begin'de kurulur, rapor inince bir kez basılır; tesis kipi değilse boş).</summary>
        (string Status, string Message, TelexKind Kind) resultLine;
        /// <summary>Tesis aşama 11: başarısız baskının kurduğu kuşatmanın teleks satırı (sonuç satırından sonra, bir kez); yoksa boş.</summary>
        (string Status, string Message, TelexKind Kind) siegeLine;
        float startedAt = -1, shownAt;
        int countersFor = -1;
        bool shown;

        Simulation Sim => game != null ? game.Sim : null;
        bool Pending => Sim != null && Sim.S.Ending == null && surface != null && surface.RaidReportPending;
        public bool ReportOpen => report != null && report.IsOpen;
        /// <summary>Okunmamış rapor ya da açık/kayan kâğıt: masa kilitli (LiberationDeskView.InputBlocked).</summary>
        public bool Busy => Pending || report != null && report.gameObject.activeSelf;
        /// <summary>Sayaçlar rapor inene kadar baskından önceki değerde bekler (SiginakDesk.Refresh yazmaz).</summary>
        public bool HoldCounters => Pending && countersFor != Sim.S.RaidLog.Count;

        void OnEnable()
        {
            if (game != null) { game.GameLoaded += Abort; game.NewGameStarted += Abort; }
            DeskLoc.Instance.Changed += Relabel;
            report.ActionClicked += File;
        }

        void OnDisable()
        {
            if (game != null) { game.GameLoaded -= Abort; game.NewGameStarted -= Abort; }
            DeskLoc.Instance.Changed -= Relabel;
            report.ActionClicked -= File;
            Abort();
        }

        void Update()
        {
            var sim = Sim;
            if (sim == null) return;
            if (startedAt < 0)
            {
                if (Pending && !RaidLauncher.IsRaidLoaded && (launcher == null || !launcher.IsTransitioning)) Begin(sim);
                return;
            }
            float t = Time.unscaledTime - startedAt;
            if (!shown)
            {
                if (t < cameraSeconds && rig.InTransition) return;
                shown = true;
                shownAt = Time.unscaledTime;
                report.Show(data, true);
                if (resultLine.Status != null) telex.Post(surface.Shift, resultLine.Status, resultLine.Message, resultLine.Kind);
                if (siegeLine.Status != null) telex.Post(surface.Shift, siegeLine.Status, siegeLine.Message, siegeLine.Kind);
                resultLine = default;
                siegeLine = default;
            }
            float settled = Time.unscaledTime - shownAt - report.DropSeconds;
            if (settled >= 0 && countersFor != sim.S.RaidLog.Count) ReleaseCounters(sim);
            if (!report.IsOpen) return;
            Thread(settled);
            if (report.Animating) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) File();
        }

        /// <summary>Kamera harita pozuna döner; rapor kamera varınca iner.</summary>
        void Begin(Simulation sim)
        {
            data = OperationReportBuilder.Build(sim);
            if (data == null) { surface.MarkRaidReportSeen(); return; }
            // Tesis aşama 10 (K40, S3): sonuç teleksi raporla birlikte, RaidLog girdisi başına bir kez (kayıttan devamda rapor okunmuş
            // sayılır, satır yinelenmez); rapor inerken basılır (kamera harita pozunda: kâğıt kadraj dışındaysa satır şeritte de
            // görünür)
            var last = sim.S.RaidLog[sim.S.RaidLog.Count - 1];
            resultLine = telex != null ? OperationReportBuilder.Telex(sim, last) : default;
            // Tesis aşama 11 (§8.1): baskın eşiğe vardırıp kuşatma kurduysa "KARŞI SALDIRI BEKLİYOR" (Alarm); savunma kapalıyken boş
            siegeLine = telex != null && FacilityDefenseText.On(sim) && !string.IsNullOrEmpty(last.LiberationReward?.Siege)
                ? (FacilityDefenseText.PendingStatus, FacilityDefenseText.PendingLine(sim, last.LiberationReward.Siege), TelexKind.Alarm) : default;
            startedAt = Time.unscaledTime;
            shown = false;
            rig.Locked = true;
            rig.Focus(SiginakPose.Map, cameraSeconds);
        }

        /// <summary>Rapor yerine oturdu: sayaçlar döner; başarılıda ± fişleri ve sektör işaretinin mavi onayı.</summary>
        void ReleaseCounters(Simulation sim)
        {
            countersFor = sim.S.RaidLog.Count;
            desk.ShowCounters(true, SiginakTheme.Active.NumberOf("motion.counterWheelDelayS", 0.12f), -1, false);
            // Tesis aşama 11: tutulan kanadın sonucu kazanç ve geri alma değildir (fiş yok, işaret zaten ✓)
            if (!data.Success || data.Defense) return;
            var ink = SiginakTheme.Active.ColorOf(ReportTones.ColorKey(ReportTone.Good));
            counters.Pin(data.Slips, new[] { ink, ink, ink, ink });
            var log = sim.S.RaidLog[sim.S.RaidLog.Count - 1];
            var sector = sim.LiberationSector(log.SectorId);
            if (sector != null && sim.LiberationRegion(log.Region)?.Completed == false) map.PlayRetaken(log.Region, sector.Index, 0);
        }

        /// <summary>İplik iğneden figüre gerilir, sonra halka çizilir; figür gizliyse ikisi de yok.</summary>
        void Thread(float settled)
        {
            var figure = map.FigureWorld;
            bool on = settled >= 0 && figure.HasValue;
            map.SetReportString(on ? report.PinScreen : (Vector2?)null, Mathf.Clamp01(settled / stringSeconds));
            Vector2? screen = null;
            if (on)
            {
                var p = rig.View.WorldToScreenPoint(figure.Value);
                if (p.z > 0) screen = p;
            }
            report.SetRing(screen, Mathf.Clamp01((settled - stringSeconds) / ringSeconds));
        }

        /// <summary>Enter, düğme ya da Esc: rapor tepsiye kayar, kamera başlangıç pozuna döner, rapor okundu sayılır.</summary>
        void File()
        {
            if (!report.IsOpen || report.Animating) return;
            report.File(tray.DropPoint, rig.View);
            Release();
            rig.Focus(SiginakPose.Desk, deskSeconds);
            surface.MarkRaidReportSeen();
            // Tesis aşama 9 (§7.6): bekleyen sunucu ödülü varsa kâğıdı ele gelir (yoksa hiçbir şey yapmaz)
            if (serverReward != null && serverReward.isActiveAndEnabled) serverReward.Present();
        }

        void Release()
        {
            map.SetReportString(null);
            counters.Unpin();
            if (startedAt >= 0) rig.Locked = false;
            startedAt = -1;
            shown = false;
            data = null;
            resultLine = default;
            siegeLine = default;
        }

        /// <summary>Yeni oyun ya da kayıttan devam: kâğıt bildirmeden kapanır.</summary>
        void Abort()
        {
            if (report != null) report.Hide();
            if (map != null && counters != null && rig != null) Release();
        }

        void Relabel()
        {
            if (!shown || data == null || Sim == null) return;
            data = OperationReportBuilder.Build(Sim);
            if (data != null) report.Bind(data);
        }
    }
}
