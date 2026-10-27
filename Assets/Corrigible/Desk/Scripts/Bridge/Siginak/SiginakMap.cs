using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kurtuluş haritasının oyun bağlantısı (Docs/UI_Siginak Aşama 5; K2, K3, Ekip figürü). Bölge durumu, görev fişi,
    /// yeni cephe onay fişi, kırmızı ip, ekip figürü ve K3 dizisi (damga → silinme → adaylar) buradan sürülür.
    /// Simülasyonu yalnız okur; komutlar <see cref="LiberationDeskView"/>'den geçer (baskın emri, cephe açma), sonuç ve
    /// gerekçe teleksten basılır. Figür sürükleme bu komutlara giden ikinci yoldur: adaya bırakmak adaya tıklamakla aynı.
    /// Enter: açık fişin asıl eylemi. Esc: taşımayı bırak → onay fişinden vazgeç → görev fişini kapat.
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
        [SerializeField] TeamFigure figure;
        [SerializeField] TeamFigureDrag drag;
        [SerializeField] DeskTarget figureTarget;

        string pending, revealQueued;
        float unlockedSince = -1;
        bool primed, figureDisplaced, figureHovered;
        readonly HashSet<string> knownFree = new HashSet<string>();
        LiberationMapState state;

        Simulation Sim => game != null ? game.Sim : null;
        bool Locked => surface == null || surface.InputBlocked;
        int Shift => surface != null ? surface.Shift : 1;
        public LiberationMapState State => state;
        public bool SlipOpen => slip.IsOpen;
        public bool FrontSlipOpen => frontSlip.IsOpen;
        public string Pending => pending;
        /// <summary>Vardiya dizisi (K6): adımdan sonra figür yerinde bekler; rapor düşerken <see cref="ReleaseFigure"/>.</summary>
        public bool HoldFigure { get; set; }

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
            DeskLoc.Instance.Changed += Refresh;
            layer.Blocked = () => Locked || drag.Carrying;
            layer.RegionClicked += RegionClicked;
            slip.ActionClicked += SlipAction;
            slip.CloseClicked += CloseSlip;
            frontSlip.ActionClicked += ConfirmFront;
            frontSlip.CancelClicked += CancelFront;
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
            DeskLoc.Instance.Changed -= Refresh;
            layer.Blocked = null;
            layer.RegionClicked -= RegionClicked;
            slip.ActionClicked -= SlipAction;
            slip.CloseClicked -= CloseSlip;
            frontSlip.ActionClicked -= ConfirmFront;
            frontSlip.CancelClicked -= CancelFront;
            drag.CanPick = null;
            drag.Refused -= Refused;
            drag.Dropped -= Dropped;
            drag.Clicked -= FigureClicked;
            drag.Over -= layer.SetHovered;
            figureTarget.Hovered -= FigureHovered;
        }

        /// <summary>Yeni oyun / kayıttan devam: fişler kapanır, figür yerine konur, özgürleşme dizisi yeniden hazırlanır.</summary>
        void ResetState()
        {
            primed = false;
            pending = null;
            revealQueued = null;
            figureDisplaced = false;
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
            if (sim.S.Raid == null) slip.ClearOrder();
            PlaceFigure(!figure.Placed);
            if (slip.IsOpen) slip.Show(LiberationMapBuilder.Slip(sim, slip.RegionId));
            if (frontSlip.IsOpen && pending != null) frontSlip.Show(LiberationMapBuilder.Front(sim, pending));
            UpdateString();
        }

        void Update()
        {
            if (Sim == null) return;
            // K3 dizisi kilit (baskın sahnesi, operasyon raporu modalı) kalktıktan kısa süre sonra başlar; rapor henüz
            // açılmadıysa onu da bekler (modal bir sonraki bağlamada gelir)
            if (Locked || RaidLauncher.IsRaidLoaded || surface.RaidReportPending) unlockedSince = -1;
            else if (unlockedSince < 0) unlockedSince = Time.unscaledTime;
            if (revealQueued != null && unlockedSince >= 0 && Time.unscaledTime - unlockedSince >= 0.35f) PlayReveal();
            // Fişler harita pozunda okunur; kamera başka istasyona dönünce kapanır (K1'de katlı etiket yok, Aşama 6+)
            if ((slip.IsOpen || frontSlip.IsOpen) && rig.Current != SiginakPose.Map && !rig.InputSuspended)
            {
                if (frontSlip.IsOpen) CancelFront();
                if (slip.IsOpen) CloseSlip();
            }
            figure.SetHighlighted(figureHovered || pending != null || drag.Carrying);
        }

        // ---------- Bölge tıklaması, fişler ----------

        void RegionClicked(string id)
        {
            if (Locked || state == null) return;
            var r = state.Find(id);
            if (r == null) return;
            rig.FocusMap();
            if (r.Kind == MapRegionKind.Candidate) SelectCandidate(id);
            else OpenSlip(id);
        }

        /// <summary>3 GÖREVLER: harita pozu ve cephenin görev fişi (cephe özgürse adaylar beklenir).</summary>
        public void ShowMissions()
        {
            if (Locked || state == null) return;
            rig.FocusMap();
            if (state.FrontOpen) OpenSlip(state.Front);
        }

        void OpenSlip(string id)
        {
            if (frontSlip.IsOpen) CancelFront();
            slip.Show(LiberationMapBuilder.Slip(Sim, id));
            Refresh();
            PlaceSlip(slip.transform, slip.PaperSize, id, false);
            UpdateString();
        }

        void CloseSlip()
        {
            if (!slip.IsOpen) return;
            slip.Hide();
            Refresh();
        }

        void SelectCandidate(string id)
        {
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
            // K3 adım 7: yağlı kalem halkası çizilir, raptiye iner; fiş görev fişine döner, figür yeni cepheye kayar
            layer.PlayFrontOpened(id);
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
                var result = surface.LaunchSector(next.Id);
                if (result?.Ok == true) slip.StampOrder();
            }
            // Cephe açıkken komşu bölgeyi cephe yapmak: komut denenir, gerekçe teleksten basılır (“cephe zaten açık”)
            else if (state != null && state.FrontOpen && id != state.Front && sim.LiberationFrontier.Contains(id))
                surface.OpenFront(id);
        }

        /// <summary>Enter: açık fişin asıl eylemi. Fiş yoksa false (şalter).</summary>
        public bool HandleEnter()
        {
            if (drag.Carrying) return true;
            if (frontSlip.IsOpen) { ConfirmFront(); return true; }
            if (slip.IsOpen) { SlipAction(); return true; }
            return false;
        }

        /// <summary>Esc: taşımayı bırak → onaydan vazgeç → fişi kapat. Bir şey yaptıysa true.</summary>
        public bool HandleEscape()
        {
            if (drag.Cancel()) return true;
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

        void UpdateString()
        {
            if (frontSlip.IsOpen && pending != null)
                layer.SetString(true, layer.WorldToMap(frontSlip.PinWorld), layer.Marks(pending).RingPoint);
            else if (slip.IsOpen && layer.Marks(slip.RegionId) is LiberationRegionMarks m)
                layer.SetString(true, layer.WorldToMap(slip.PinWorld), m.State?.Kind == MapRegionKind.Front ? m.PinPoint : m.LabelPoint);
            else layer.SetString(false);
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
            telex.Post(Shift, Loc.T("BÖLGE ÖZGÜR"), Loc.F("{0} · {1} fon", Loc.T(GameData.Region(id).Name),
                DeskFormat.Signed(sim.B.liberationCaptureFunds, 0)), TelexKind.Done);
            var candidates = state.Regions.Where(r => r.Kind == MapRegionKind.Candidate).Select(r => r.Id).ToList();
            if (candidates.Count == 0) return;
            telex.Post(Shift, Loc.T("BEKLENİYOR"), Loc.T("Sıradaki cepheyi seç"), TelexKind.Info);
            layer.PlayCandidates(candidates, 0.7f);
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
