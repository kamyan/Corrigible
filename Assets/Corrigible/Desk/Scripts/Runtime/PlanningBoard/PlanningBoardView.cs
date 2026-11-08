using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Sahneye kaydedilmiş görev alanları, depo ve üretim kâğıdı. Çalışırken hiyerarşi üretmez.</summary>
    public sealed class PlanningBoardView : MonoBehaviour
    {
        [SerializeField] Canvas canvas;
        [SerializeField] PlanningBoardCard[] productionMaterials = Array.Empty<PlanningBoardCard>();
        [SerializeField] GameObject[] trainingMarks = Array.Empty<GameObject>();
        public PlanningBoardCard[] ProductionMaterials => productionMaterials;
        public void TrainingChoice(int index){for(int i=0;i<trainingMarks.Length;i++)if(trainingMarks[i])trainingMarks[i].SetActive(i==index);}
        [SerializeField] PlanningBoardMotion motion;
        public PlanningBoardMotion Motion => motion;
        [SerializeField] CanvasGroup controls;
        [SerializeField] ScrollRect stockScroll, idleScroll;
        [SerializeField] Vector2 zoneCardGap = new Vector2(12, 16);
        [SerializeField] Vector2 idleCardGap = new Vector2(30, 55);
        [SerializeField] RectTransform dragPlane, workArea;
        [SerializeField] PlanningBoardSurface freeSurface;
        [SerializeField] PlanningBoardCard[] roleTags = Array.Empty<PlanningBoardCard>();
        [SerializeField] Vector2 equipmentOffset = new Vector2(58, -60);
        [SerializeField] Vector2 equipmentStackStep = new Vector2(8, -20);
        [SerializeField] float cardMargin = 6;
        [SerializeField] TMP_Text heading, supplyLine, planLine, detailTitle, detailBody, status, production, training, machineLabel;
        [SerializeField] PlanningBoardCard[] people = Array.Empty<PlanningBoardCard>();
        [SerializeField] PlanningBoardCard[] equipment = Array.Empty<PlanningBoardCard>();
        [SerializeField] PlanningBoardCard[] materials = Array.Empty<PlanningBoardCard>();
        [SerializeField] PlanningBoardCard[] recipes = Array.Empty<PlanningBoardCard>();
        [SerializeField] PlanningBoardDropZone[] zones = Array.Empty<PlanningBoardDropZone>();
        [SerializeField] Button[] traces = Array.Empty<Button>();
        [SerializeField] TMP_Text[] traceLabels = Array.Empty<TMP_Text>();
        [SerializeField] Button[] subjects = Array.Empty<Button>();
        [SerializeField] Button close, machine, cancelProduction, review, restrict, dismiss;
        [SerializeField] PlanningBoardCard receipt;
        [SerializeField] GameObject receiptRoot, coverOpen, coverClosed;
        [SerializeField] CanvasGroup fade;
        [SerializeField] Canvas admissionCanvas;
        [SerializeField] CanvasGroup admissionControls;
        [SerializeField] GameObject admissionsRoot;
        [SerializeField] TMP_Text admissionProtocol, admissionPapers, admissionFindings;
        [SerializeField] Button[] inspections = Array.Empty<Button>();
        [SerializeField] Button[] decisions = Array.Empty<Button>();
        [SerializeField] Button admissionClose;

        public PlanningBoardCard[] RoleTags => roleTags ?? Array.Empty<PlanningBoardCard>();
        public RectTransform WorkArea => workArea != null ? workArea : dragPlane;
        public PlanningBoardSurface FreeSurface => freeSurface;
        public Vector2 EquipmentOffset => equipmentOffset;
        public Vector2 EquipmentStackStep => equipmentStackStep;
        public PlanningBoardCard[] People => people;
        public PlanningBoardCard[] Equipment => equipment;
        public PlanningBoardCard[] Materials => materials;
        public PlanningBoardCard[] Recipes => recipes;
        public PlanningBoardDropZone[] Zones => zones;
        public Button[] Traces => traces;
        public Button[] Subjects => subjects;
        public PlanningBoardCard Receipt => receipt;
        public Button CloseButton => close;
        public Button MachineButton => machine;
        public Button CancelProductionButton => cancelProduction;
        public Button ReviewButton => review;
        public Button RestrictButton => restrict;
        public Button DismissButton => dismiss;
        public RectTransform DragPlane => dragPlane;
        public CanvasGroup Fade => fade;
        // Aşama 6 (D20): eski kabul penceresi sahneden silindi; alanlar boş/null kalır, erişimciler boş dizi döner
        public Button[] Inspections => inspections ?? Array.Empty<Button>();
        public Button[] Decisions => decisions ?? Array.Empty<Button>();
        public Button AdmissionClose => admissionClose;
        public ScrollRect StockScroll => stockScroll;
        public ScrollRect IdleScroll => idleScroll;

        public PlanningBoardDropZone JobZone(string job)
        {
            foreach (var zone in zones)
                if (zone && zone.Kind == PlanningDropKind.Job && zone.TargetId == job) return zone;
            return null;
        }

        public PlanningBoardDropZone FirstZone(PlanningDropKind kind)
        {
            foreach (var zone in zones) if (zone && zone.Kind == kind) return zone;
            return null;
        }

        public Vector2 PlanePosition(Transform item) => dragPlane.InverseTransformPoint(item.position);

        /// <summary>Tek görev tek kart taşır; boşta alanı ve depo okunur bir ızgarayla kaydırılır.</summary>
        public Vector2 ZonePosition(PlanningBoardCard card, PlanningBoardDropZone zone, int index = 0, int count = 1)
        {
            if (!card || !zone || !zone.CardAnchor) return Vector2.zero;
            var anchor = zone.CardAnchor;
            Vector2 local = anchor.rect.center;
            bool grid = zone.Kind == PlanningDropKind.Inventory || zone.Kind == PlanningDropKind.Job && zone.TargetId == "rest";
            if (grid)
            {
                var size = ((RectTransform)card.transform).rect.size;
                var gap = zone.Kind == PlanningDropKind.Inventory ? zoneCardGap : idleCardGap;
                int columns = Mathf.Max(1, Mathf.FloorToInt((anchor.rect.width + gap.x) / (size.x + gap.x)));
                float rowHeight = size.y + gap.y;
                local = new Vector2(anchor.rect.xMin + size.x * .5f + index % columns * (size.x + gap.x),
                    anchor.rect.yMax - size.y * .5f - index / columns * rowHeight);
            }
            return dragPlane.InverseTransformPoint(anchor.TransformPoint(local));
        }

        public void SetZoneCount(PlanningBoardDropZone zone, PlanningBoardCard card, int count)
        {
            if (!zone || !zone.CardAnchor || !card) return;
            var scroll = zone.Kind == PlanningDropKind.Inventory ? stockScroll : zone.TargetId == "rest" ? idleScroll : null;
            if (!scroll || scroll.content != zone.CardAnchor || !scroll.viewport) return;
            var size = ((RectTransform)card.transform).rect.size;
            var gap = zone.Kind == PlanningDropKind.Inventory ? zoneCardGap : idleCardGap;
            int columns = Mathf.Max(1, Mathf.FloorToInt((scroll.content.rect.width + gap.x) / (size.x + gap.x)));
            var contentSize = scroll.content.sizeDelta;
            contentSize.y = Mathf.Max(scroll.viewport.rect.height, Mathf.CeilToInt(count / (float)columns) * (size.y + gap.y));
            scroll.content.sizeDelta = contentSize;
        }

        /// <summary>Kaydırılan depodaki kartlar maskenin altında kalır; sürükleme sırasında kart DragPlane'a çıkar.</summary>
        public void PlaceInZone(PlanningBoardCard card, PlanningBoardDropZone zone, int index = 0, int count = 1)
        {
            if (!card || card.IsDragging || !zone || !zone.CardAnchor) return;
            Vector2 position = ZonePosition(card, zone, index, count);
            bool clipped = zone.Kind == PlanningDropKind.Inventory && stockScroll && stockScroll.content == zone.CardAnchor ||
                zone.Kind == PlanningDropKind.Job && zone.TargetId == "rest" && idleScroll && idleScroll.content == zone.CardAnchor;
            var parent = clipped ? zone.CardAnchor : dragPlane;
            if (card.transform.parent != parent) card.transform.SetParent(parent, false);
            var rect = (RectTransform)card.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.localPosition = parent.InverseTransformPoint(dragPlane.TransformPoint(position));
        }

        /// <summary>Kartları pano kenarı içinde tutar; konum ızgara veya görev yuvasına yuvarlanmaz.</summary>
        public Vector2 ClampPosition(PlanningBoardCard card, Vector2 position)
        {
            if (!DragPlane || !WorkArea || !card) return position;
            var corners = new Vector3[4]; WorkArea.GetWorldCorners(corners);
            Vector2 min = DragPlane.InverseTransformPoint(corners[0]), max = min;
            foreach (var corner in corners)
            {
                Vector2 point = DragPlane.InverseTransformPoint(corner);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            var rect = (RectTransform)card.transform;
            Vector2 half = Vector2.Scale(rect.rect.size, new Vector2(Mathf.Abs(rect.localScale.x), Mathf.Abs(rect.localScale.y))) * .5f;
            half += Vector2.one * Mathf.Max(0, cardMargin);
            if (max.x - min.x < half.x * 2) position.x = (min.x + max.x) * .5f;
            else position.x = Mathf.Clamp(position.x, min.x + half.x, max.x - half.x);
            if (max.y - min.y < half.y * 2) position.y = (min.y + max.y) * .5f;
            else position.y = Mathf.Clamp(position.y, min.y + half.y, max.y - half.y);
            return position;
        }

        public void SetCamera(Camera camera) { if (canvas) canvas.worldCamera = camera; if (admissionCanvas) admissionCanvas.worldCamera = camera; }
        public void SetFocused(bool focused)
        { if (motion) motion.Focus(focused); if (controls) { controls.interactable = focused; controls.blocksRaycasts = focused; } if (freeSurface) freeSurface.Interactable = focused; }
        public void SetHeader(string title, string food, string plan)
        { if (heading) heading.text = title; if (supplyLine) supplyLine.text = food; if (planLine) planLine.text = plan; }
        public void SetDetail(string title, string body) { if (detailTitle) detailTitle.text = title; if (detailBody) detailBody.text = body; }
        public void SetStatus(string text) { if (status) status.text = text; }
        public void SetStockCount(int count)
        {
            if (stockScroll == null || stockScroll.content == null || stockScroll.viewport == null) return;
            var size = stockScroll.content.sizeDelta;
            size.y = Mathf.Max(stockScroll.viewport.rect.height, Mathf.CeilToInt(count / 4f) * 75 + 8);
            stockScroll.content.sizeDelta = size;
        }
        public void SetProduction(string text) { if (production) production.text = text; }
        public void SetTraining(string text) { if (training) training.text = text; }
        public void SetTrace(int index, string text, bool enabled)
        { if (index < traceLabels.Length) traceLabels[index].text = text; if (index < traces.Length) traces[index].interactable = enabled; }
        public void SetReceipt(bool visible) { if (receiptRoot) receiptRoot.SetActive(visible); }
        public void SetCover(bool open, string label)
        { if (coverOpen) coverOpen.SetActive(open); if (coverClosed) coverClosed.SetActive(!open); if (machineLabel) machineLabel.text = label; }
        public void Highlight(PlanningBoardCard card) { foreach (var zone in zones) zone.ShowTarget(card); }
        /// <summary>Eski kabul penceresi (Aşama 6'da sahneden silindi, D20): alanlar boşken hiçbir şey yapmaz.</summary>
        public void SetAdmissions(bool shown, string protocol, string papers, string findings)
        {
            if (admissionsRoot) admissionsRoot.SetActive(shown);
            if (admissionControls) { admissionControls.interactable = shown; admissionControls.blocksRaycasts = shown; }
            if (admissionProtocol) admissionProtocol.text = protocol;
            if (admissionPapers) admissionPapers.text = papers;
            if (admissionFindings) admissionFindings.text = findings;
        }
    }
}
