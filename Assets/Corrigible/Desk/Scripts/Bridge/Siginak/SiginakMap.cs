using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kurtuluş haritasının oyun bağlantısı (Docs/UI_Siginak Aşama 5; K2, K3, Ekip figürü). Bölge durumu, görev fişi,
    /// yeni cephe onay fişi, kırmızı ip, ekip figürü ve K3 dizisi (damga → silinme → adaylar) buradan sürülür.
    /// Simülasyonu yalnız okur; komutlar <see cref="LiberationDeskView"/>'den geçer (baskın emri, cephe açma), sonuç ve
    /// gerekçe teleksten basılır. Figür sürükleme bu komutlara giden ikinci yoldur: adaya bırakmak adaya tıklamakla aynı.
    /// Enter: açık fişin asıl eylemi. Esc: taşımayı bırak → kadro raporundan ya da onay fişinden vazgeç → görev fişini kapat.
    /// BASKIN EMRİ VER emri hemen vermez: görev fişinin yanında kadro raporu açılır, emir rapordaki BASKINA ÇIK'la (Enter) gider.
    /// Görev fişi (Revizyon 2 · S2) ekran katmanında, sol masa kenarında (Kuzey/Güney Amerika seçiliyse sağda); açılırken
    /// bölgeye iğne düşer, iplik bölgeden fişin iğnesine çekilir, fiş kenardan kayar; ip her karede kameradan yeniden izdüşer.
    /// </summary>
    public sealed class SiginakMap : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] TelexFeed telex;
        [SerializeField] SadeMapView sadeMap;
        [SerializeField] LiberationMapLayer layer;
        [SerializeField] OperationSlip slip;
        [SerializeField] FrontSlip frontSlip;
        [Tooltip("BASKIN EMRİ VER'den sonra görev fişinin yanında açılan kadro raporu; emir onunla verilir.")]
        [SerializeField] SquadReportSlip squadReport;
        [SerializeField] TeamFigure figure;
        [SerializeField] TeamFigureDrag drag;
        [SerializeField] DeskTarget figureTarget;
        [Tooltip("Yetersiz fonda BASKIN EMRİ VER'e basınca FON sayacı iki kez titrer (Karar 7).")]
        [SerializeField] CounterBank counters;
        [SerializeField] PlanningBoardPresenter planningBoard;

        // Görev fişinin dizisi (S2 › hareket): iğne 0,12 s → iplik 0,25 s → fiş 0,22 s; kapanış iplik 0,15 s → fiş 0,18 s
        const float StringCloseS = 0.15f;
        float slipAt = -1, slipCloseAt = -1;
        bool pinSounded, paperSounded;

        string pending, revealQueued;
        /// <summary>Kadro raporunun açıldığı kare: aynı karedeki ikinci Enter onay sayılmaz (rapor görülmeden emir verilmez).</summary>
        int reportFrame = -1;
        /// <summary>Fişteki kadro seçimi (seçim sırasıyla; en eski önce); null ise varsayılan seçim. Vardiya değişince sıfırlanır.</summary>
        List<string> pick;
        int pickDay = -1;
        [Tooltip("Eğitim sürerken ilk baskının kadrosu sabit (Teknisyen, EMP uzmanı, Analist).")]
        [SerializeField] SiginakTutorial tutorial;
        float unlockedSince = -1;
        bool primed, figureDisplaced, figureHovered, missionsOnArrival;
        readonly HashSet<string> knownFree = new HashSet<string>();
        LiberationMapState state;

        Simulation Sim => game != null ? game.Sim : null;
        bool Locked => surface == null || surface.InputBlocked;
        int Shift => surface != null ? surface.Shift : 1;
        public LiberationMapState State => state;
        public bool SlipOpen => slip.IsOpen;
        public bool FrontSlipOpen => frontSlip.IsOpen;
        public bool SquadReportOpen => squadReport.IsOpen;
        public string Pending => pending;
        /// <summary>Vardiya dizisi (K6): adımdan sonra figür yerinde bekler; rapor düşerken <see cref="ReleaseFigure"/>.</summary>
        public bool HoldFigure { get; set; }
        /// <summary>Ekip figürünün dünya noktası (operasyon raporunun ipliği ve halkası); figür gizliyse null.</summary>
        public Vector3? FigureWorld => figure != null && figure.gameObject.activeInHierarchy ? figure.Destination : (Vector3?)null;

        Vector2? reportPin;
        float reportDrawn;

        public void Configure(GameController controller, LiberationDeskView desk, SiginakCameraRig cameraRig, TelexFeed feed, SadeMapView map,
            LiberationMapLayer mapLayer, OperationSlip operationSlip, FrontSlip confirmSlip, TeamFigure teamFigure, TeamFigureDrag figureDrag,
            DeskTarget figureHover)
        {
            game = controller; surface = desk; rig = cameraRig; telex = feed; sadeMap = map; layer = mapLayer; slip = operationSlip;
            frontSlip = confirmSlip; figure = teamFigure; drag = figureDrag; figureTarget = figureHover;
        }

        void OnEnable()
        {
            if (game != null) { game.Changed += Refresh; game.GameLoaded += ResetState; game.NewGameStarted += ResetState; }
            rig.PoseChanged += PoseChanged;
            PoseChanged(rig.Current);
            DeskLoc.Instance.Changed += Refresh;
            layer.Blocked = () => Locked || drag.Carrying;
            layer.RegionClicked += MapRegionClicked;
            sadeMap.BackgroundClicked += MapBackgroundClicked;
            slip.ActionClicked += SlipAction;
            slip.CloseClicked += CloseSlip;
            slip.PickClicked += TogglePick;
            slip.PersonClicked += ShowPerson; squadReport.PersonClicked += ShowPerson;
            frontSlip.ActionClicked += ConfirmFront;
            frontSlip.CancelClicked += CancelFront;
            squadReport.ActionClicked += ConfirmSquad;
            squadReport.CancelClicked += CancelSquad;
            drag.CanPick = CanPick;
            drag.Refused += Refused;
            drag.Dropped += Dropped;
            drag.Clicked += FigureClicked;
            drag.Over += layer.SetHovered;
            figureTarget.Hovered += FigureHovered;
            sadeMap.SetLand(SiginakTheme.Active.ColorOf("mapLiberation.land"));
            Refresh();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Refresh; game.GameLoaded -= ResetState; game.NewGameStarted -= ResetState; }
            rig.PoseChanged -= PoseChanged;
            DeskLoc.Instance.Changed -= Refresh;
            layer.Blocked = null;
            layer.RegionClicked -= MapRegionClicked;
            sadeMap.BackgroundClicked -= MapBackgroundClicked;
            slip.ActionClicked -= SlipAction;
            slip.CloseClicked -= CloseSlip;
            slip.PickClicked -= TogglePick;
            slip.PersonClicked -= ShowPerson; squadReport.PersonClicked -= ShowPerson;
            frontSlip.ActionClicked -= ConfirmFront;
            frontSlip.CancelClicked -= CancelFront;
            squadReport.ActionClicked -= ConfirmSquad;
            squadReport.CancelClicked -= CancelSquad;
            drag.CanPick = null;
            drag.Refused -= Refused;
            drag.Dropped -= Dropped;
            drag.Clicked -= FigureClicked;
            drag.Over -= layer.SetHovered;
            figureTarget.Hovered -= FigureHovered;
        }

        /// <summary>Yeni oyun / kayıttan devam: fişler kapanır, figür yerine konur, özgürleşme dizisi yeniden hazırlanır.</summary>
        void ShowPerson(string id)
        {
            if (Locked || planningBoard == null) return;
            squadReport.Hide(); CloseSlip(); planningBoard.ShowPerson(id);
        }

        void ResetState()
        {
            primed = false;
            PoseChanged(rig.Current);
            pending = null;
            pick = null;
            revealQueued = null;
            figureDisplaced = false;
            squadReport.Hide();
            slip.Hide();
            frontSlip.Hide();
            Refresh();
            PlaceFigure(true);
        }

        void Refresh()
        {
            var sim = Sim;
            if (sim == null) return;
            // K3: yeni özgürleşen bölge (yükleme ve yeni oyunda dizi yok)
            var free = sim.S.Liberation.Regions.Where(r => r.Completed).Select(r => r.Id).ToList();
            if (primed) foreach (var id in free) if (!knownFree.Contains(id)) revealQueued = id;
            knownFree.Clear();
            foreach (var id in free) knownFree.Add(id);
            primed = true;

            state = LiberationMapBuilder.Build(sim, null, pending);
            if (pending != null && !state.IsCandidate(pending))
            {
                pending = null;
                frontSlip.Hide();
                if (figureDisplaced) { figureDisplaced = false; figure.SlideBack(TeamWorld()); }
                state = LiberationMapBuilder.Build(sim, null, null);
            }
            layer.Bind(state);
            PlaceFigure(!figure.Placed);
            if (slip.IsOpen)
            {
                var data = LiberationMapBuilder.Slip(sim, slip.RegionId, Pick, FixedSquad);
                slip.Show(data, slip.OnRight);
                // Rapor açıkken kadro değişirse yeniden yazılır; emir artık verilemiyorsa (vardiya, ekip, fon) kapanır
                if (squadReport.IsOpen)
                {
                    if (data.ActionState == SlipActionState.Ready && squadReport.RegionId == slip.RegionId)
                        squadReport.Show(LiberationMapBuilder.SquadReport(sim, slip.RegionId, Pick, FixedSquad));
                    else squadReport.Hide();
                }
            }
            else if (squadReport.IsOpen) squadReport.Hide();
            if (frontSlip.IsOpen && pending != null) frontSlip.Show(LiberationMapBuilder.Front(sim, pending));
            // Görev fişinin ipi diziyle birlikte çizilir (açılırken yarım)
            if (frontSlip.IsOpen || !slip.gameObject.activeSelf) UpdateString();
            else AnimateSlip();
        }

        void Update()
        {
            if (Sim == null) return;
            // K3 dizisi kilit (baskın sahnesi, operasyon raporu modalı) kalktıktan kısa süre sonra başlar; rapor henüz
            // açılmadıysa onu da bekler (modal bir sonraki bağlamada gelir)
            bool blocked = Locked || RaidLauncher.IsRaidLoaded || surface.RaidReportPending;
            if (blocked) unlockedSince = -1;
            else if (unlockedSince < 0) unlockedSince = Time.unscaledTime;
            if (revealQueued != null && unlockedSince >= 0 && Time.unscaledTime - unlockedSince >= 0.35f) PlayReveal();
            if (missionsOnArrival && !blocked && !rig.InTransition && !rig.InputSuspended && revealQueued == null)
            {
                missionsOnArrival = false;
                if (!SlipOpen && !FrontSlipOpen) OpenFrontMission();
            }
            // Fişler harita pozunda okunur; kamera başka istasyona dönünce kapanır (K1'de katlı etiket yok, Aşama 6+)
            if ((slip.IsOpen || frontSlip.IsOpen) && rig.Current != SiginakPose.Map && !rig.InputSuspended)
            {
                if (squadReport.IsOpen) squadReport.Hide();
                if (frontSlip.IsOpen) CancelFront();
                if (slip.IsOpen) CloseSlip();
            }
            figure.SetHighlighted(figureHovered || pending != null || drag.Carrying);
            AnimateSlip();
        }

        /// <summary>Açılış ve kapanış dizisinin iğne sesi, kâğıt sesi ve ipi (ip kamerayla birlikte her karede).</summary>
        void AnimateSlip()
        {
            if (frontSlip.IsOpen || !slip.gameObject.activeSelf)
            {
                if (!frontSlip.IsOpen && slipAt >= 0) { slipAt = -1; layer.SetString(false); }
                return;
            }
            var t = SiginakTheme.Active;
            float now = Time.unscaledTime, pinS = t.NumberOf("motion.pinDropS", 0.12f), stringS = t.NumberOf("motion.stringS", 0.25f);
            float e = now - slipAt;
            if (!pinSounded && e >= 0) { pinSounded = true; Sfx.Play(Cue.PinDrop, 0.8f); }
            if (!paperSounded && e >= pinS + stringS) { paperSounded = true; Sfx.Play(Cue.PaperSlide, 0.7f, slip.OnRight ? 0.4f : -0.4f); }
            float drawn = slipCloseAt >= 0 ? 1 - Mathf.Clamp01((now - slipCloseAt) / StringCloseS) : Mathf.Clamp01((e - pinS) / stringS);
            UpdateString(drawn);
        }

        // ---------- Bölge tıklaması, fişler ----------

        void PoseChanged(SiginakPose pose) => missionsOnArrival = pose == SiginakPose.Map;

        void MapBackgroundClicked()
        {
            if (!drag.Carrying) surface.ShowPlanning();
        }

        void MapRegionClicked(string id)
        {
            if (rig.IsFocused && !rig.InTransition) RegionClicked(id);
            else MapBackgroundClicked();
        }

        void RegionClicked(string id)
        {
            if (Locked || state == null) return;
            var r = state.Find(id);
            if (r == null) return;
            rig.FocusMap();
            missionsOnArrival = false;
            if (r.Kind == MapRegionKind.Candidate) SelectCandidate(id);
            else OpenSlip(id);
        }

        /// <summary>Harita tıklaması ve 3 kısayolu: harita pozu ve cephenin görev fişi (cephe özgürse adaylar beklenir).</summary>
        public void ShowMissions()
        {
            if (Locked || state == null) return;
            rig.FocusMap();
            missionsOnArrival = false;
            OpenFrontMission();
        }

        void OpenFrontMission()
        {
            if (state?.FrontOpen == true) OpenSlip(state.Front);
        }

        void OpenSlip(string id)
        {
            if (frontSlip.IsOpen) CancelFront();
            if (squadReport.IsOpen && squadReport.RegionId != id) squadReport.Hide();
            var t = SiginakTheme.Active;
            bool open = slip.IsOpen, sameRegion = open && slip.RegionId == id;
            // Kamera haritaya varınca dizi başlar: iğne düşer, iplik çekilir, sonra fiş kayar
            float start = rig.TransitionRemaining, pinS = t.NumberOf("motion.pinDropS", 0.12f), stringS = t.NumberOf("motion.stringS", 0.25f);
            // Karar 1 B: fiş sol masa kenarında; Kuzey ya da Güney Amerika seçiliyse sağda
            bool right = id == "na" || id == "sa";
            slip.Show(LiberationMapBuilder.Slip(Sim, id, Pick, FixedSquad), right, start + pinS + stringS);
            if (!sameRegion)
            {
                slipAt = Time.unscaledTime + start;
                slipCloseAt = -1;
                pinSounded = paperSounded = open;
                layer.PlayPinDrop(id, start);
            }
            Refresh();
            AnimateSlip();
        }

        void CloseSlip() => CloseSlip(0);

        /// <summary>Kapanış (S2): iplik 0,15 s'de sarılır, fiş 0,18 s'de kenara çekilir.</summary>
        void CloseSlip(float delay)
        {
            if (!slip.IsOpen) return;
            if (squadReport.IsOpen) squadReport.Close(delay);
            slipCloseAt = Time.unscaledTime + delay;
            slip.Close(delay + StringCloseS);
            Refresh();
        }

        /// <summary>Eğitim sürerken ilk baskının kadrosu değişmez (senaryo üç uzmana göre yazıldı).</summary>
        bool FixedSquad => tutorial != null && tutorial.Active && Sim != null && Sim.Board == null && Sim.S.Raids == 0;

        /// <summary>Seçim bu vardiyanın; vardiya değişince varsayılana döner.</summary>
        List<string> Pick
        {
            get
            {
                if (pick != null && Sim != null && pickDay != Sim.S.Day) pick = null;
                return pick;
            }
        }

        /// <summary>
        /// Kadro diskine tıklama: seçili uzman bırakılır; seçilmemiş uzman eklenir, iki seçili varken en eski seçimin yerine geçer.
        /// </summary>
        void TogglePick(string role)
        {
            var sim = Sim;
            if (sim == null || Locked || FixedSquad || role == "tek") return;
            var team = sim.S.Teams.FirstOrDefault();
            var list = Pick != null ? new List<string>(Pick) : Simulation.DefaultPick(team);
            if (list.Contains(role)) list.Remove(role);
            else
            {
                if (list.Count >= Simulation.PickCount) list.RemoveAt(0);
                list.Add(role);
            }
            pick = list;
            pickDay = sim.S.Day;
            Sfx.Play(Cue.Pencil, .6f);
            Refresh();
        }

        void SelectCandidate(string id)
        {
            squadReport.Hide();
            if (slip.IsOpen) slip.Hide();
            pending = id;
            frontSlip.Show(LiberationMapBuilder.Front(Sim, id));
            Refresh();
            PlaceSlip(frontSlip.transform, frontSlip.PaperSize, id, true);
            UpdateString();
        }

        void CancelFront()
        {
            pending = null;
            frontSlip.Hide();
            if (figureDisplaced) { figureDisplaced = false; figure.SlideBack(TeamWorld()); }
            Refresh();
        }

        void ConfirmFront()
        {
            if (pending == null || Locked) return;
            string id = pending;
            var result = surface.OpenFront(id);
            if (result?.Ok != true) return;
            pending = null;
            figureDisplaced = false;
            frontSlip.Hide();
            Refresh();
            // K3 adım 7: fiş görev fişine döner (iğne düşer, iplik çekilir), figür yeni cepheye kayar
            OpenSlip(id);
        }

        void SlipAction()
        {
            var sim = Sim;
            if (!slip.IsOpen || Locked || sim == null) return;
            string id = slip.RegionId;
            if (slip.HasAction)
            {
                var next = sim.NextLiberationSector(id);
                if (next == null) return;
                slip.Press();
                // Karar 7: eksik yalnız fonsa komut çağrılmaz; gerekçe teleksten, FON sayacı iki kez titrer
                if (slip.ActionState == SlipActionState.Shortfall && TelexReasons.RaidFundsOnly(sim, next.Id))
                {
                    telex.Post(Shift, Loc.T("YAPILAMADI"), TelexReasons.Raid(sim, next.Id), TelexKind.Failed);
                    if (counters != null) counters[CounterBank.Slot.Funds].Shake(2);
                    return;
                }
                // Emir hazırsa önce kadro raporu (emir onunla verilir); devre dışıysa komut denenir, gerekçe teleksten basılır
                if (slip.ActionState == SlipActionState.Ready)
                {
                    if (!squadReport.IsOpen)
                    {
                        Sfx.Play(Cue.PaperSlide, 0.6f, slip.OnRight ? 0.3f : -0.3f);
                        reportFrame = Time.frameCount;
                    }
                    squadReport.Show(LiberationMapBuilder.SquadReport(sim, id, Pick, FixedSquad));
                    return;
                }
                Launch(next.Id);
            }
            // Cephe açıkken komşu bölgeyi cephe yapmak: komut denenir, gerekçe teleksten basılır (“cephe zaten açık”)
            else if (state != null && state.FrontOpen && id != state.Front && sim.LiberationFrontier.Contains(id))
                surface.OpenFront(id);
        }

        /// <summary>Kadro raporunda BASKINA ÇIK: emir verilir; tutmazsa (gerekçe teleksten) rapor kapanır, fiş açık kalır.</summary>
        void ConfirmSquad()
        {
            var sim = Sim;
            if (!squadReport.IsOpen || Locked || sim == null || Time.frameCount == reportFrame) return;
            var next = sim.NextLiberationSector(squadReport.RegionId);
            if (next == null || !slip.IsOpen || slip.RegionId != squadReport.RegionId) { squadReport.Hide(); return; }
            // Düğme 0,06 s iner, sonra kart fişle birlikte solar (emir Refresh'i tetiklese de kapanış sürer)
            squadReport.Press();
            squadReport.Close(0.06f);
            if (!Launch(next.Id)) squadReport.Hide();
        }

        void CancelSquad()
        {
            if (!squadReport.IsOpen) return;
            squadReport.Hide();
            Sfx.Play(Cue.Pencil, .5f);
        }

        /// <summary>Baskın emri. Emir: düğme 0,06 s iner → rapor ve fiş kapanır → CRT'ye dalış.</summary>
        bool Launch(string sectorId)
        {
            // Seçim ikiden azsa emir verilmez (boş seçim komutta varsayılan kadroya döner, fişin dediği olmaz)
            if (!FixedSquad && Pick != null && Pick.Count < Simulation.PickCount)
            {
                telex.Post(Shift, Loc.T("YAPILAMADI"), Loc.T("Teknisyen'in yanına iki uzman seç."), TelexKind.Failed);
                return false;
            }
            var result = surface.LaunchSector(sectorId, FixedSquad ? null : Pick == null ? null : Simulation.PickKey(Pick));
            if (result?.Ok != true) return false;
            pick = null;
            CloseSlip(0.06f);
            return true;
        }

        /// <summary>Enter: açık fişin asıl eylemi (kadro raporu açıksa baskına çıkış). Fiş yoksa false (şalter).</summary>
        public bool HandleEnter()
        {
            if (drag.Carrying) return true;
            if (squadReport.IsOpen) { ConfirmSquad(); return true; }
            if (frontSlip.IsOpen) { ConfirmFront(); return true; }
            if (slip.IsOpen) { SlipAction(); return true; }
            return false;
        }

        /// <summary>Esc: taşımayı bırak → kadro raporundan ya da onaydan vazgeç → fişi kapat. Bir şey yaptıysa true.</summary>
        public bool HandleEscape()
        {
            if (drag.Cancel()) return true;
            if (squadReport.IsOpen) { CancelSquad(); return true; }
            if (frontSlip.IsOpen) { CancelFront(); return true; }
            if (slip.IsOpen) { CloseSlip(); return true; }
            return false;
        }

        // ---------- Yerleşim ----------

        /// <summary>
        /// Fiş, cepheyi, özgür bölgeleri, adayları ve figürü örtmeyen yere konur (K2: yarı, K3: çeyrek). Aday: örttüğü
        /// önemli nokta sayısı en az olan; eşitlikte hedef bölgeden uzak olan.
        /// </summary>
        void PlaceSlip(Transform root, Vector2 paperPx, string targetId, bool quadrant)
        {
            if (state == null) return;
            float k = root.lossyScale.x * layer.PixelsPerMeter;
            var size = paperPx * k;
            const float margin = 24;
            var target = layer.Marks(targetId);
            var targetPoint = target != null ? target.LabelPoint : MapProjection.DesignSize / 2;
            var avoid = new List<Vector2>();
            foreach (var r in state.Regions)
            {
                var m = layer.Marks(r.Id);
                if (m == null) continue;
                if (r.Kind != MapRegionKind.Occupied || r.Id == targetId) avoid.Add(m.LabelPoint);
            }
            if (!state.Team.Hidden) avoid.Add(layer.WorldToMap(figure.Destination));
            var options = new List<Vector2>();
            float left = margin + size.x / 2, right = MapProjection.DesignWidth - margin - size.x / 2;
            float top = margin + size.y / 2, bottom = MapProjection.DesignHeight - margin - size.y / 2;
            if (bottom < top) top = bottom = MapProjection.DesignHeight / 2;
            if (quadrant) { options.Add(new Vector2(right, bottom)); options.Add(new Vector2(right, top)); options.Add(new Vector2(left, bottom)); options.Add(new Vector2(left, top)); }
            else { options.Add(new Vector2(right, top)); options.Add(new Vector2(left, top)); }
            // Puan: önemli nokta ve lejant 10, öteki bölge etiketleri 1 (K2: Doğu Avrupa → sağ, Asya üstü)
            var others = state.Regions.Where(r => r.Kind == MapRegionKind.Occupied && r.Id != targetId)
                .Select(r => layer.Marks(r.Id)).Where(m => m != null).Select(m => m.LabelPoint).ToList();
            var legend = layer.LegendRect;
            Vector2 best = options[0];
            int bestScore = int.MaxValue;
            float bestDistance = -1;
            foreach (var o in options)
            {
                var rect = new Rect(o - size / 2 - Vector2.one * 20, size + Vector2.one * 40);
                int score = avoid.Count(rect.Contains) * 10 + (legend.width > 0 && rect.Overlaps(legend) ? 10 : 0) + others.Count(rect.Contains);
                float distance = Vector2.Distance(o, targetPoint);
                if (score < bestScore || score == bestScore && distance > bestDistance) { best = o; bestScore = score; bestDistance = distance; }
            }
            root.SetPositionAndRotation(layer.MapToWorld(best) + layer.MapPlane.normal * 0.002f, layer.MapRotation);
        }

        /// <summary>
        /// Operasyon raporu (S7): iplik raporun iğnesinden (<paramref name="pinScreen"/>, ekran) ekip figürüne gerilir
        /// (<paramref name="drawn"/> 0 → 1, iğneden); null ipliği kaldırır.
        /// </summary>
        public void SetReportString(Vector2? pinScreen, float drawn = 1)
        {
            reportPin = pinScreen;
            reportDrawn = drawn;
            UpdateString();
        }

        /// <summary>Operasyon raporu: geri alınan sektörün işareti 0,2 s'de mavi onaya döner.</summary>
        public void PlayRetaken(string region, int sector, float delay) => layer.PlayRetaken(region, sector, delay);

        void UpdateString(float drawn = 1)
        {
            if (reportPin.HasValue && FigureWorld is Vector3 team && ScreenToMap(reportPin.Value, out var reportPinMap))
                layer.SetString(true, layer.WorldToMap(team), reportPinMap, reportDrawn);
            else if (reportPin.HasValue) layer.SetString(false);
            else if (frontSlip.IsOpen && pending != null)
                layer.SetString(true, layer.WorldToMap(frontSlip.PinWorld), layer.Marks(pending).RingPoint);
            else if (slip.gameObject.activeSelf && layer.Marks(slip.RegionId) is LiberationRegionMarks m && ScreenToMap(slip.PinScreenAtRest, out var pin))
                layer.SetString(true, pin, m.State?.Kind == MapRegionKind.Front ? m.PinPoint : m.LabelPoint, drawn);
            else layer.SetString(false);
        }

        /// <summary>Ekran noktasının (görev fişi ekran katmanında) harita düzlemindeki karşılığı.</summary>
        bool ScreenToMap(Vector2 screen, out Vector2 map)
        {
            map = default;
            var cam = rig.View;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(screen);
            if (!layer.MapPlane.Raycast(ray, out float d)) return false;
            map = layer.WorldToMap(ray.GetPoint(d));
            return true;
        }

        // ---------- K3 dizisi ----------

        void PlayReveal()
        {
            string id = revealQueued;
            revealQueued = null;
            var sim = Sim;
            if (frontSlip.IsOpen) CancelFront();
            if (slip.IsOpen) CloseSlip();
            rig.FocusMap();
            // 0,2 s: mavi ÖZGÜR damgası iner, tarama 0,5 s'de silinir; teleks iki satır; adaylar 0,12 s arayla çizilir
            layer.PlayFreed(id, 0.2f);
            telex.Post(Shift, Loc.T("BÖLGE ÖZGÜR"), Loc.F("{0} · {1} fon", GameData.Region(id).Name,
                DeskFormat.Signed(sim.B.liberationCaptureFunds, 0)), TelexKind.Done);
            var candidates = state.Regions.Where(r => r.Kind == MapRegionKind.Candidate).Select(r => r.Id).ToList();
            if (candidates.Count == 0) return;
            telex.Post(Shift, Loc.T("BEKLENİYOR"), Loc.T("Sıradaki cepheyi seç"), TelexKind.Info);
            layer.PlayCandidates(candidates, 0.7f);
        }

        // ---------- Karşı saldırı (Revizyon 2 · S5b) ----------

        /// <summary>Son karşı saldırının bölgesi (sektör kimliği “eeu_1” → “eeu”); yoksa null.</summary>
        string CounterattackRegion()
        {
            string id = Sim?.S.LastCounterattack;
            int cut = id?.LastIndexOf('_') ?? -1;
            return cut > 0 ? id.Substring(0, cut) : null;
        }

        /// <summary>Vardiya raporu düşerken: yeniden işgal edilen sektöre ✕ çizilir, YENİDEN İŞGAL damgası iner.</summary>
        public void PlayCounterattack(float delay)
        {
            string region = CounterattackRegion();
            if (region != null) layer.PlayCounterattack(region, delay);
        }

        /// <summary>Raporun karşı saldırı kutusunun üzerine gelince ✕ iki kez yanıp söner.</summary>
        public void BlinkCounterattack()
        {
            string region = CounterattackRegion();
            if (region != null) layer.BlinkCounterattack(region);
        }

        // ---------- Ekip figürü ----------

        Vector3 TeamWorld() => layer.MapToWorld(layer.TeamPoint(state.Team));

        /// <summary>Figür oyun durumuna geçer: iyileşen ekip S-07'den cepheye 0,6 s'de kayar (atlanınca anında).</summary>
        public void ReleaseFigure(bool instant)
        {
            HoldFigure = false;
            PlaceFigure(instant);
        }

        void PlaceFigure(bool instant)
        {
            if (state == null || HoldFigure) return;
            var team = state.Team;
            if (figure.gameObject.activeSelf == team.Hidden) figure.gameObject.SetActive(!team.Hidden);
            if (team.Hidden) return;
            figure.SetDown(team.Down);
            figure.SetInjured(DeskText.Upper(LiberationMapBuilder.TeamName(Sim.S.Teams.FirstOrDefault())),
                team.AtShelter ? Loc.F("{0} GÜN", team.Days) : null);
            figureTarget.LineOverride = team.Ready
                ? Loc.F("{0} · {1}\n{2}", Loc.T("HAZIR"), team.Members, Loc.T("Sürükle: adaya taşı"))
                : Loc.F("{0}\n{1}", Loc.F("TEKNİSYEN YARALI · {0} GÜN", team.Days), team.Members);
            if (drag.Carrying || figureDisplaced) return;
            figure.Place(TeamWorld(), instant);
        }

        bool CanPick() => !Locked && Sim != null && TelexReasons.Pick(Sim) == null;

        void Refused()
        {
            figure.Wobble();
            var sim = Sim;
            string why = sim != null ? TelexReasons.Pick(sim) : null;
            if (why != null) telex.Post(Shift, Loc.T("YAPILAMADI"), why, TelexKind.Failed);
        }

        void FigureHovered(bool on) => figureHovered = on;

        /// <summary>Tık: figürün durduğu bölgenin fişi (sürükleme zorunlu değil).</summary>
        void FigureClicked()
        {
            if (state != null && !Locked) RegionClicked(state.Team.AtShelter ? state.Shelter : state.Team.Region);
        }

        void Dropped(Vector3 world)
        {
            var sim = Sim;
            layer.SetHovered(null);
            if (sim == null || state == null) return;
            string id = layer.RegionAt(world);
            string why = TelexReasons.Drop(sim, id);
            if (why != null)
            {
                figure.SlideBack(drag.Origin);
                telex.Post(Shift, Loc.T("YAPILAMADI"), why, TelexKind.Failed);
                return;
            }
            if (state.FrontOpen || id == state.Front)
            {
                // K2: cephe açıkken yalnız kendi cephesine; bırakınca görev fişi açılır
                figure.SlideBack(TeamWorld());
                if (state.FrontOpen) OpenSlip(id);
                return;
            }
            // K3: adaya bırakmak = o adayı seçmek; figür adayda bekler, Esc ile geri kayar
            figureDisplaced = true;
            figure.SlideBack(layer.MapToWorld(layer.TeamAnchor(id)));
            SelectCandidate(id);
        }
    }
}
