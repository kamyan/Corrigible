using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Sade haritanın serileştirilmiş katmanlarını yalnız gösterilen görünüm modeline bağlar.</summary>
    public sealed class SadeMapView : MonoBehaviour, IIllusionAware
    {
        [Serializable] public sealed class RegionSlot
        {
            public string Id;
            public Vector2 NodePoint;
            public RegionMeshGraphic Fill;
            public GameObject Quarantine;
            public GameObject QuarantineOutline;
            public GameObject Ripple;
            public GameObject Alert;
            public SadeRegionNodeView Node;
            public SadeRegionLabelView Label;
            public GameObject Selection;
            public GameObject Highlight;
            public GameObject QuarantineHatch;
            public UILineGraphic[] QuarantineLines = Array.Empty<UILineGraphic>();
        }

        [SerializeField] RegionSlot[] regions = Array.Empty<RegionSlot>();
        [SerializeField] SadeSpreadThreadView[] threads = Array.Empty<SadeSpreadThreadView>();
        [SerializeField] SadeTeamTokenView[] teams = Array.Empty<SadeTeamTokenView>();
        [SerializeField] Texture fog;
        [SerializeField] MapCoverFitter cover;
        [SerializeField] GameObject sharedAlert;
        [SerializeField] UILineGraphic[] teamRouteLines = Array.Empty<UILineGraphic>();
        [SerializeField] RectTransform[] teamRouteArrows = Array.Empty<RectTransform>();
        [SerializeField] FieldReportView[] reports = Array.Empty<FieldReportView>();
        bool illusion;
        bool liberationStyle;
        Color liberationLand;
        SadeMapStateVM last;
        public RegionSlot[] Regions => regions;
        /// <summary>Sığınak görünümünün kurtuluş haritası: Sade işaretleri gizli, bölgeler düz kâğıt (çizimi başka katman yapar).</summary>
        public bool LiberationStyle => liberationStyle;
        public SadeTeamTokenView[] Teams => teams;
        public FieldReportView[] Reports => reports;
        public event Action<string> ReportClicked;

        void Awake()
        {
            foreach (var r in reports)
                if (r != null) r.Clicked += id => ReportClicked?.Invoke(id);
        }

        public float WorldScale => (cover != null ? cover.Scale : 1f) * transform.lossyScale.x;

        public Vector3 RegionWorld(string id)
        {
            var slot = Find(id);
            if (slot != null && slot.Node != null) return slot.Node.transform.position;
            return slot != null ? MapToWorld(slot.NodePoint.x, slot.NodePoint.y) : transform.position;
        }

        public Vector3 MapToWorld(float x, float y)
        {
            var rt = (RectTransform)transform;
            var p = cover != null ? cover.ToCanvas(new Vector2(x, y)) : new Vector2(x, y);
            var size = cover != null ? cover.ViewportSize : rt.rect.size;
            return rt.TransformPoint(new Vector3(p.x - size.x * 0.5f, size.y * 0.5f - p.y, 0f));
        }

        public void SetIllusion(bool on)
        {
            if (illusion == on) return;
            illusion = on;
            foreach (var slot in regions)
                foreach (var line in slot.QuarantineLines)
                    if (line != null) line.SetDash(on ? Array.Empty<float>() : new[] { 7f, 4f });
        }

        /// <summary>
        /// Kurtuluş biçimi (Docs/UI_Siginak Aşama 5): düğüm, Sade etiketi, karantina, iplik, ekip jetonu, rapor ve uyarı
        /// gizlenir; bölge dolgusu <paramref name="land"/> rengini alır. Tıklama alanı olarak dolgular etkin kalır.
        /// </summary>
        public void SetLiberationStyle(bool on, Color land)
        {
            if (liberationStyle == on && liberationLand == land) return;
            liberationStyle = on;
            liberationLand = land;
            if (last != null) Bind(last);
        }

        public void HighlightRegion(string id)
        {
            foreach (var slot in regions) DeskUi.SetActive(slot.Highlight, slot.Id == id);
        }

        public void HighlightTeam(string id)
        {
            foreach (var token in teams) if (token != null) token.SetHighlighted(token.TeamId == id);
        }

        public void Bind(SadeMapStateVM vm)
        {
            if (vm == null) return;
            last = vm;
            if (liberationStyle) { BindLiberation(vm); return; }
            var theme = SadeTheme.Active;
            foreach (var slot in regions)
            {
                RegionNodeVM value = null;
                foreach (var r in vm.Regions) if (r.Id == slot.Id) { value = r; break; }
                bool visible = value != null;
                if (slot.Fill != null) slot.Fill.gameObject.SetActive(visible);
                if (slot.Node != null) slot.Node.gameObject.SetActive(visible);
                if (slot.Label != null) slot.Label.gameObject.SetActive(visible);
                if (!visible)
                {
                    DeskUi.SetActive(slot.Quarantine, false); DeskUi.SetActive(slot.QuarantineOutline, false);
                    DeskUi.SetActive(slot.Ripple, false); DeskUi.SetActive(slot.Alert, false);
                    DeskUi.SetActive(slot.Selection, false); DeskUi.SetActive(slot.Highlight, false);
                    continue;
                }
                bool unknown = !value.Value.HasValue || value.Trust == "unknown";
                if (slot.Fill != null)
                    slot.Fill.SetAppearance(unknown ? Color.white : theme.RampFill(value.Value.Value), unknown ? fog : null, unknown ? 9.899f : 0);
                DeskUi.SetActive(slot.Quarantine, value.Quarantine);
                DeskUi.SetActive(slot.QuarantineHatch, value.Quarantine);
                DeskUi.SetActive(slot.QuarantineOutline, value.Quarantine);
                DeskUi.SetActive(slot.Ripple, value.Core && !illusion);
                DeskUi.SetActive(slot.Alert, vm.AlertRegions.Contains(slot.Id) && !value.Selected && !illusion);
                DeskUi.SetActive(slot.Selection, value.Selected);
                if (slot.Node != null) slot.Node.Bind(value);
                if (slot.Label != null) slot.Label.Bind(value);
            }
            if (sharedAlert != null)
            {
                string alertId = vm.AlertRegions != null && vm.AlertRegions.Count > 0 ? vm.AlertRegions[0] : null;
                var alertSlot = !string.IsNullOrEmpty(alertId) ? Find(alertId) : null;
                bool showSharedAlert = alertSlot != null && !illusion;
                if (showSharedAlert)
                {
                    foreach (var r in vm.Regions) if (r.Id == alertId && r.Selected) { showSharedAlert = false; break; }
                }
                DeskUi.SetActive(sharedAlert, showSharedAlert);
                if (showSharedAlert)
                {
                    sharedAlert.GetComponent<SadeMarkerAnchor>()?.SetPoint(alertSlot.NodePoint, new Vector2(-47f, -47f));
                    sharedAlert.GetComponent<SadePointerTarget>()?.SetId(alertId);
                }
            }
            for (int i = 0; i < threads.Length; i++)
            {
                var thread = threads[i];
                if (thread == null) continue;
                bool visible = false, core = false;
                string fromId = thread.From, toId = thread.To;
                if (!string.IsNullOrEmpty(fromId))
                {
                    foreach (var pair in vm.SpreadThreads)
                        if (pair.From == fromId && pair.To == toId) { visible = true; break; }
                }
                else if (i < vm.SpreadThreads.Count)
                {
                    fromId = vm.SpreadThreads[i].From;
                    toId = vm.SpreadThreads[i].To;
                    visible = true;
                }
                DeskUi.SetActive(thread, visible && !illusion);
                if (!visible || illusion) continue;
                var from = Find(fromId); var to = Find(toId);
                if (from == null || to == null) continue;
                foreach (var r in vm.Regions) if (r.Id == fromId) { core = r.Core; break; }
                thread.Bind(from.NodePoint, to.NodePoint, core);
            }
            for (int i = 0; i < teams.Length; i++)
            {
                var token = teams[i];
                if (token == null) continue;
                SadeMapTeamVM value = null;
                foreach (var team in vm.Teams) if (team.Id == token.TeamId) { value = team; break; }
                DeskUi.SetActive(token, value != null);
                if (value != null)
                {
                    if (value.HasPoint)
                        token.GetComponent<SadeMarkerAnchor>()?.SetPoint(new Vector2(value.X, value.Y), Vector2.zero);
                    token.Bind(value);
                }
                if (i < teamRouteLines.Length && teamRouteLines[i] != null)
                {
                    bool route = value != null && value.HasPoint && value.Status == "travelling";
                    DeskUi.SetActive(teamRouteLines[i], route);
                    if (i < teamRouteArrows.Length && teamRouteArrows[i] != null)
                        DeskUi.SetActive(teamRouteArrows[i], route);
                    if (route)
                    {
                        var p0 = new Vector2(value.FromX, -value.FromY);
                        var p1 = new Vector2(value.ToX, -value.ToY);
                        teamRouteLines[i].SetPoints(new[] { p0, p1 });
                        if (i < teamRouteArrows.Length && teamRouteArrows[i] != null)
                        {
                            teamRouteArrows[i].anchorMin = teamRouteArrows[i].anchorMax = new Vector2(0f, 1f);
                            teamRouteArrows[i].anchoredPosition = Vector2.Lerp(p0, p1, 0.72f);
                            var dir = p1 - p0;
                            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                            teamRouteArrows[i].localRotation = Quaternion.Euler(0f, 0f, ang);
                        }
                    }
                }
            }
            for (int i = 0; i < reports.Length; i++)
            {
                var report = reports[i];
                if (report == null) continue;
                var r = vm.Reports != null && i < vm.Reports.Count ? vm.Reports[i] : null;
                report.Bind(r);
                DeskUi.SetActive(report, r != null);
                if (r != null)
                    report.GetComponent<SadeMarkerAnchor>()?.SetPoint(new Vector2(r.X, r.Y), Vector2.zero);
            }
        }

        void BindLiberation(SadeMapStateVM vm)
        {
            foreach (var slot in regions)
            {
                bool visible = false;
                foreach (var r in vm.Regions) if (r.Id == slot.Id) { visible = true; break; }
                if (slot.Fill != null)
                {
                    slot.Fill.gameObject.SetActive(visible);
                    slot.Fill.SetAppearance(liberationLand);
                }
                if (slot.Node != null) slot.Node.gameObject.SetActive(false);
                if (slot.Label != null) slot.Label.gameObject.SetActive(false);
                DeskUi.SetActive(slot.Quarantine, false); DeskUi.SetActive(slot.QuarantineHatch, false);
                DeskUi.SetActive(slot.QuarantineOutline, false); DeskUi.SetActive(slot.Ripple, false);
                DeskUi.SetActive(slot.Alert, false); DeskUi.SetActive(slot.Selection, false); DeskUi.SetActive(slot.Highlight, false);
            }
            if (sharedAlert != null) DeskUi.SetActive(sharedAlert, false);
            foreach (var thread in threads) if (thread != null) DeskUi.SetActive(thread, false);
            foreach (var token in teams) if (token != null) DeskUi.SetActive(token, false);
            foreach (var line in teamRouteLines) if (line != null) DeskUi.SetActive(line, false);
            foreach (var arrow in teamRouteArrows) if (arrow != null) DeskUi.SetActive(arrow, false);
            foreach (var report in reports) if (report != null) DeskUi.SetActive(report, false);
        }

        RegionSlot Find(string id)
        {
            foreach (var r in regions) if (r.Id == id) return r;
            return null;
        }
    }
}
