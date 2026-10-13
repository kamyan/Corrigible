using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Hedef kartı (C1 §7.1): dizin kartı; başlık satırı (tutamaç, HEDEF, alt başlık, küçült), kazanma yolları (ad, 7 kutu,
    /// yüzde) ve “Sıradaki:” satırı (ad, süre, Araştırma › bağlantısı). Küçült düğmesi kartı başlık satırına (33 px) indirir
    /// (PROMPT §8). Kart dolgusundan tutulup taşınır (<see cref="DeskDraggablePanel"/>); yeri sahnede.
    /// </summary>
    public sealed class GoalCardView : MonoBehaviour
    {
        [SerializeField] DeskRow headRow;
        [SerializeField] TMP_Text title, subtitle;
        [SerializeField] TMP_Text[] trackNames = System.Array.Empty<TMP_Text>();
        [SerializeField] ProgressBoxesView[] trackBoxes = System.Array.Empty<ProgressBoxesView>();
        [SerializeField] TMP_Text[] trackPercents = System.Array.Empty<TMP_Text>();
        [SerializeField] DeskRow nextRow;
        [SerializeField] TMP_Text nextLabel, nextName, nextEta;
        [SerializeField] DeskButton nextLink;
        [Tooltip("Sağa yaslı düz metin (C2: “Onay bekleniyor”); bağlantının yerine.")]
        [SerializeField] TMP_Text nextRight;
        [SerializeField] TMP_Text nextDot;
        [Tooltip("Başlıktaki küçük damga (C2: TEMİZ); küçült düğmesinin 8 px solunda, sağa yaslı.")]
        [SerializeField] StampView stamp;
        [SerializeField] float stampRight = 339;
        [SerializeField] DeskButton minimize;
        [Tooltip("Küçültülünce gizlenen gövde parçaları (çizgiler, başlık altı kural).")]
        [SerializeField] GameObject[] body = System.Array.Empty<GameObject>();
        [SerializeField] float collapsedHeight = 33;

        GoalVM vm;
        float expandedHeight;
        bool collapsed;

        public DeskButton NextLink => nextLink;
        public bool Collapsed => collapsed;
        public event System.Action<bool> CollapsedChanged;

        void Awake()
        {
            expandedHeight = ((RectTransform)transform).sizeDelta.y;
            if (minimize != null) minimize.onClick.AddListener(() => SetCollapsed(!collapsed));
        }

        public void SetCollapsed(bool on)
        {
            if (collapsed == on) return;
            collapsed = on;
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, on ? collapsedHeight : expandedHeight);
            if (minimize != null) minimize.SetIcon(on ? "ic_arrow_down_12b" : "ic_minimize_12");
            ApplyVisibility();
            CollapsedChanged?.Invoke(on);
        }

        void ApplyVisibility()
        {
            foreach (var g in body) DeskUi.SetActive(g, !collapsed);
            for (int i = 0; i < trackNames.Length; i++)
            {
                bool on = !collapsed && vm?.Tracks != null && i < vm.Tracks.Count;
                DeskUi.SetActive(trackNames[i], on);
                DeskUi.SetActive(trackBoxes[i], on);
                DeskUi.SetActive(trackPercents[i], on);
            }
            bool next = !collapsed && vm?.Next != null;
            bool plain = next && !string.IsNullOrEmpty(vm.Next.Right);
            DeskUi.SetActive(nextRow, next);
            DeskUi.SetActive(nextLink, next && !plain);
            DeskUi.SetActive(nextRight, plain);
        }

        public void Bind(GoalVM g)
        {
            if (g == null) return;
            vm = g;
            DeskUi.SetText(title, DeskLoc.Instance.T(g.Title));
            DeskUi.SetText(subtitle, DeskLoc.Instance.T(g.Subtitle));
            headRow.Layout();
            DeskUi.SetActive(stamp, g.Stamp != null);
            if (g.Stamp != null)
            {
                stamp.Set(DeskLoc.Instance.T(g.Stamp.Text), StampView.ParseInk(g.Stamp.Color), StampSize.Small, g.Stamp.Rot, "goal.stamp");
                var sr = (RectTransform)stamp.transform;
                sr.anchorMin = sr.anchorMax = new Vector2(0, 1); sr.pivot = new Vector2(1, 0.5f);
                sr.anchoredPosition = new Vector2(stampRight, -17.5f);
            }
            for (int i = 0; i < trackNames.Length; i++)
            {
                if (g.Tracks == null || i >= g.Tracks.Count) continue;
                var t = g.Tracks[i];
                DeskUi.SetText(trackNames[i], DeskLoc.Instance.T(t.Name));
                trackBoxes[i].Set(t.Percent / 100f);
                DeskUi.SetText(trackPercents[i], DeskFormat.Percent(t.Percent));
            }
            if (g.Next != null)
            {
                DeskUi.SetText(nextLabel, DeskLoc.Instance.T(g.Next.Label));
                DeskUi.SetText(nextName, DeskLoc.Instance.T(g.Next.Name));
                bool plain = !string.IsNullOrEmpty(g.Next.Right);
                DeskUi.SetActive(nextEta, !plain && !string.IsNullOrEmpty(g.Next.Eta));
                DeskUi.SetActive(nextDot, !plain && !string.IsNullOrEmpty(g.Next.Eta));
                DeskUi.SetText(nextEta, DeskLoc.Instance.T(g.Next.Eta));
                if (!plain) nextLink.SetLabel(g.Next.Link);
                else DeskUi.SetText(nextRight, DeskLoc.Instance.T(g.Next.Right));
                nextRow.Layout();
            }
            ApplyVisibility();
        }
    }
}
