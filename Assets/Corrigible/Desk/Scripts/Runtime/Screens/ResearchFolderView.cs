using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Araştırma klasörü (C1 §7.1): sayfa başlığı, özet (aktif projeler, fon akışı, fon + mühür), acil durum fonu kutusu ve
    /// dallar (başlık + satırlar). Dal blokları ve satırlar şablondan havuzlanır; dikey yerleşim veriyle hesaplanır.
    /// </summary>
    public sealed class ResearchFolderView : MonoBehaviour
    {
        [SerializeField] FolderWindow window;
        [SerializeField] TMP_Text sheetTitle, day;
        [Tooltip("Sayfa başlığının 10 px sağındaki küçük damga (C2: TEMİZ).")]
        [SerializeField] StampView headerStamp;
        [Tooltip("Başlık satırı damgayla 15 → 19 px uzar (CSS .rs-h ai center): başlık öğeleri yarısı, alttakiler tamamı kadar iner.")]
        [SerializeField] RectTransform[] headerItems = System.Array.Empty<RectTransform>();
        [SerializeField] RectTransform[] belowHeader = System.Array.Empty<RectTransform>();
        [SerializeField] float headerBaseHeight = 15;

        float appliedHeaderShift;
        [SerializeField] TMP_Text summary;
        [SerializeField] DeskRow fundRow;
        [SerializeField] TMP_Text fundLabel, fundValue;
        [SerializeField] TrustMarkView fundTrust;
        [SerializeField] TMP_Text emergencyTitle, emergencyHint;
        [SerializeField] DeskRow optionsRow;
        [SerializeField] RadioCheckView[] options = System.Array.Empty<RadioCheckView>();
        [Tooltip("Dal başlığı şablonu: cap + çizgi + ilerleme.")]
        [SerializeField] RectTransform branchTemplate;
        [SerializeField] ResearchRowView rowTemplate;
        [SerializeField] RectTransform branchesRoot;
        [SerializeField] float branchGap = 10;
        [SerializeField] float headerHeight = 20;

        DeskPool<RectTransform> branchPool;
        readonly List<Vector2> branchSpans = new List<Vector2>();

        /// <summary>Dalın dikey aralığı (dallar kökünde, y aşağı): eğitim hedefi (ör. Hizalama kolu). Yoksa false.</summary>
        public bool BranchSpan(int index, out RectTransform root, out float top, out float bottom)
        {
            root = branchesRoot; top = bottom = 0;
            if (index < 0 || index >= branchSpans.Count || !gameObject.activeInHierarchy) return false;
            top = branchSpans[index].x; bottom = branchSpans[index].y;
            return true;
        }
        DeskPool<ResearchRowView> rowPool;
        readonly List<ResearchRowView> rows = new List<ResearchRowView>();

        public FolderWindow Window => window;
        public event System.Action<string, bool> ResearchAction;
        public event System.Action<int> FundSelected;

        void Awake()
        {
            for (int i = 0; i < options.Length; i++)
            {
                int k = i;
                options[i].Clicked += _ => FundSelected?.Invoke(k);
            }
        }
        public RadioCheckView[] Options => options;
        public IReadOnlyList<ResearchRowView> Rows => rows;

        public void Bind(ResearchVM r)
        {
            if (r == null) return;
            window.SetTitle(r.Title, null, r.Key);
            DeskUi.SetText(sheetTitle, DeskLoc.Instance.T(r.SheetTitle));
            DeskUi.SetText(day, r.Day);
            DeskUi.SetActive(headerStamp, r.HeaderStamp != null);
            float shift = r.HeaderStamp != null ? Mathf.Max(0, 19 - headerBaseHeight) : 0;
            if (!Mathf.Approximately(shift, appliedHeaderShift))
            {
                float d = shift - appliedHeaderShift;
                foreach (var h in headerItems) if (h != null) h.anchoredPosition += new Vector2(0, -d / 2);
                foreach (var b in belowHeader) if (b != null) b.anchoredPosition += new Vector2(0, -d);
                appliedHeaderShift = shift;
            }
            if (r.HeaderStamp != null)
            {
                headerStamp.Set(DeskLoc.Instance.T(r.HeaderStamp.Text), StampView.ParseInk(r.HeaderStamp.Color), StampSize.Small, r.HeaderStamp.Rot, "research.header");
                // Başlığın 10 px sağında, dikeyde başlıkla ortalı (CSS .rs-h gap 10, ai center)
                var t = sheetTitle.rectTransform;
                var sr = (RectTransform)headerStamp.transform;
                sr.pivot = new Vector2(0, 0.5f);
                float left = t.localPosition.x + t.rect.xMin + Mathf.Round(DeskUi.TextWidth(sheetTitle, sheetTitle.text)) + 10;
                sr.localPosition = new Vector3(left, t.localPosition.y + t.rect.center.y, 0);
            }
            if (r.Summary != null)
            {
                DeskUi.SetText(summary, DeskLoc.Instance.T(r.Summary.Text));
                DeskUi.SetText(fundLabel, DeskLoc.Instance.T(r.Summary.FundLabel));
                DeskUi.SetText(fundValue, r.Summary.Fund);
                fundTrust.Set(DeskUi.ParseTrust(r.Summary.FundTrust), false);
                fundRow.Layout();
            }
            if (r.EmergencyFund != null)
            {
                var f = r.EmergencyFund;
                DeskUi.SetText(emergencyTitle, DeskLoc.Instance.T(f.Title));
                DeskUi.SetText(emergencyHint, DeskLoc.Instance.T(f.Hint));
                for (int i = 0; i < options.Length; i++)
                {
                    bool on = i < f.Options.Count;
                    DeskUi.SetActive(options[i], on);
                    if (on) options[i].Set(f.Options[i].Label, f.Options[i].Cost, i == f.Selected);
                }
                optionsRow.Layout();
            }

            if (!Application.isPlaying && !DeskUi.EditPreview) return;   // Dinamik listeler yalnızca çalışırken kurulur
            branchPool ??= new DeskPool<RectTransform>(branchTemplate, branchesRoot);
            rowPool ??= new DeskPool<ResearchRowView>(rowTemplate, branchesRoot);
            rows.Clear();
            branchSpans.Clear();
            float y = 0;
            int ri = 0;
            for (int b = 0; b < r.Branches.Count; b++)
            {
                var br = r.Branches[b];
                float branchTop = y;
                var header = branchPool.Get(b);
                Place(header, y);
                var texts = header.GetComponentsInChildren<TMP_Text>(true);
                DeskUi.SetText(texts[0], DeskLoc.Instance.T(br.Title));
                DeskUi.SetText(texts[1], br.Progress);
                var line = header.GetComponentInChildren<Image>(true);
                float capW = Mathf.Round(DeskUi.TextWidth(texts[0], texts[0].text)), progW = Mathf.Round(DeskUi.TextWidth(texts[1], texts[1].text));
                texts[0].rectTransform.sizeDelta = new Vector2(capW, texts[0].rectTransform.sizeDelta.y);
                var lr = line.rectTransform;
                lr.anchoredPosition = new Vector2(capW + 8, lr.anchoredPosition.y);
                lr.sizeDelta = new Vector2(header.rect.width - capW - progW - 16, 1);
                y += headerHeight;
                for (int p = 0; p < br.Projects.Count; p++)
                {
                    bool fresh = ri >= rowPool.Count;
                    var row = rowPool.Get(ri++);
                    if (fresh) row.Action += (id, start) => ResearchAction?.Invoke(id, start);
                    row.Bind(br.Projects[p], r.LockedLabel, p == br.Projects.Count - 1);
                    Place((RectTransform)row.transform, y);
                    y += row.Height;
                    rows.Add(row);
                }
                branchSpans.Add(new Vector2(branchTop, y));
                y += branchGap;
            }
            branchPool.Trim(r.Branches.Count);
            rowPool.Trim(ri);
        }

        static void Place(RectTransform rt, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -y);
        }
    }
}
