using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Eğitim katmanı (F8.4, taslak <c>drafts/egitim</c>). Adım: hedefin çevresinde delik bırakan dört karartma, sarı çerçeve,
    /// danışman notu dilinde sarı adım notu (sayaç, kutucuklar, başlık, metin, İleri + Enter, Baştan, Eğitimi atla) ve nottan
    /// hedefe kesik kalem oku. İpucu: küçük not, hedefin yanında, karartma yok. Karartma tıklamayı engellemez; oyuncu hedefle
    /// doğrudan etkileşir. Hedefler kimlikle (oyunun TutorialTarget adları) sahnedeki dikdörtgenlere bağlanır; yerleşim her
    /// karede yeniden hesaplanır (pencereler sürüklenebilir), bellek ayırmaz.
    /// </summary>
    public sealed class DeskTutorialView : MonoBehaviour
    {
        [Serializable]
        public struct Target
        {
            public string id;
            [Tooltip("Hedef alan bu dikdörtgenlerin birleşimi (ör. dosyanın eylem kartları).")]
            public RectTransform[] rects;
            [Tooltip("Hedef bir penceredeyse gösterilince açılır.")]
            public bool openWindow;
            public WindowId window;
            [Tooltip("Araştırma dosyasının bir kolu (0: Hizalama); -1 değilse alan koldan hesaplanır.")]
            public int researchBranch;
        }

        [SerializeField] GameObject root;
        [SerializeField] RectTransform area;
        [Tooltip("Üst, alt, sol, sağ karartma.")]
        [SerializeField] RectTransform[] dims = new RectTransform[4];
        [SerializeField] RectTransform hole;
        [Header("Adım notu")]
        [SerializeField] RectTransform stepNote;
        [SerializeField] TMP_Text stepCounter, stepTitle, stepBody;
        [SerializeField] RectTransform pips;
        [SerializeField] Image[] pipImages = Array.Empty<Image>();
        [SerializeField] RectTransform stepFooter;
        [SerializeField] DeskButton next, restart, skip;
        [SerializeField] KeyChipView nextKey;
        [SerializeField] DeskRow linksRow;
        [Header("İpucu notu")]
        [SerializeField] RectTransform tipNote;
        [SerializeField] TMP_Text tipCounter, tipTitle, tipBody;
        [SerializeField] DeskButton tipClose;
        [Header("Kalem oku")]
        [SerializeField] UILineGraphic arrow, arrowHead;
        [Header("Hedefler")]
        [SerializeField] Target[] targets = Array.Empty<Target>();
        [SerializeField] ResearchFolderView research;
        [Header("Yerleşim")]
        [SerializeField] float holePad = 4, noteGap = 56, tipGap = 18, margin = 16;
        [SerializeField] float stepBodyTop = 73.5f, stepFooterGap = 5, stepBottom = 11, tipBodyTop = 62, tipBottom = 10;

        TutorialVM vm;
        readonly Vector3[] corners = new Vector3[4];
        readonly List<Vector2> arrowPts = new List<Vector2>(3), headPts = new List<Vector2>(3);
        static readonly float[] Dash = { 5, 4 };
        int openedFor = -1;
        Vector2 lastFrom, lastTo;

        public static DeskTutorialView Instance { get; private set; }
        public bool IsOpen => root != null && root.activeSelf;
        public bool IsTip => vm != null && vm.IsTip;
        /// <summary>Enter'ın bir karşılığı var mı (İleri ya da ipucunu kapat).</summary>
        public bool HasNext => IsOpen && vm?.Next != null;
        public event Action<string> Invoked;

        void Awake()
        {
            Instance = this;
            next.onClick.AddListener(() => Invoked?.Invoke(vm?.Next?.Command));
            restart.onClick.AddListener(() => Invoked?.Invoke(vm?.Restart?.Command));
            skip.onClick.AddListener(() => Invoked?.Invoke(vm?.Skip?.Command));
            tipClose.onClick.AddListener(() => Invoked?.Invoke(vm?.Next?.Command));
            if (arrow != null) arrow.SetDash(Dash);
            foreach (var d in dims) d.anchorMin = d.anchorMax = d.pivot = new Vector2(0.5f, 0.5f);
            hole.anchorMin = hole.anchorMax = hole.pivot = new Vector2(0.5f, 0.5f);
            root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Enter: İleri / ipucunu kapat.</summary>
        public void PressNext() { if (vm?.Next != null) Invoked?.Invoke(vm.Next.Command); }

        /// <summary>Hedef masada şu an gösterilebilir mi (pencere açılabiliyorsa evet).</summary>
        public bool TargetAvailable(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            for (int i = 0; i < targets.Length; i++)
            {
                var t = targets[i];
                if (t.id != id) continue;
                if (t.openWindow) return true;
                return t.rects != null && t.rects.Length > 0 && t.rects[0] != null && t.rects[0].gameObject.activeInHierarchy;
            }
            return false;
        }

        public void Bind(TutorialVM model)
        {
            bool changed = !ReferenceEquals(model, vm);
            vm = model;
            root.SetActive(model != null);
            if (model == null) { openedFor = -1; return; }
            if (!changed) return;
            lastFrom = lastTo = default;
            var T = DeskLoc.Instance;
            bool tip = model.IsTip;
            DeskUi.SetActive(stepNote, !tip);
            DeskUi.SetActive(tipNote, tip);
            if (tip)
            {
                DeskUi.SetText(tipCounter, T.T(model.Counter));
                DeskUi.SetText(tipTitle, T.T(model.Title));
                DeskUi.SetText(tipBody, T.T(model.Body));
                FitBody(tipBody, tipNote, tipBodyTop, tipBottom);
            }
            else
            {
                DeskUi.SetText(stepCounter, T.T(model.Counter));
                // Kutucuklar sayaç metninin hemen sağında
                pips.anchoredPosition = new Vector2(stepCounter.rectTransform.anchoredPosition.x + Mathf.Round(DeskUi.TextWidth(stepCounter, stepCounter.text)) + 10, pips.anchoredPosition.y);
                for (int i = 0; i < pipImages.Length; i++)
                {
                    bool on = i < model.Steps;
                    DeskUi.SetActive(pipImages[i], on);
                    if (on) DeskSprites.Apply(pipImages[i], i < model.Step ? "sh_fill_r0" : "sh_frame_r0_1");
                }
                DeskUi.SetText(stepTitle, T.T(model.Title));
                DeskUi.SetText(stepBody, T.T(model.Body));
                DeskUi.SetActive(next, model.Next != null);
                DeskUi.SetActive(nextKey, model.Next != null);
                if (model.Next != null) next.SetLabel(T.T(model.Next.Label));
                restart.SetLabel(T.T(model.Restart?.Label));
                skip.SetLabel(T.T(model.Skip?.Label));
                if (linksRow != null) linksRow.Layout();
                float h = FitBody(stepBody, stepNote, stepBodyTop, 0);
                stepFooter.anchoredPosition = new Vector2(stepFooter.anchoredPosition.x, -(h + stepFooterGap));
                stepNote.sizeDelta = new Vector2(stepNote.sizeDelta.x, h + stepFooterGap + stepFooter.sizeDelta.y + stepBottom);
            }
            // Adımın penceresi (eski sağ panel sekmesi) ve hedefin penceresi bir kez açılır
            int key = model.Step * 100 + (tip ? 1 : 0);
            if (openedFor != key)
            {
                openedFor = key;
                if (model.OpenWindow >= 0) WindowManager.Instance?.Open((WindowId)model.OpenWindow);
                var t = Find(model.Target);
                if (t.HasValue && t.Value.openWindow) WindowManager.Instance?.Open(t.Value.window);
            }
            Layout();
        }

        /// <summary>Metni sarar, notun yüksekliğini metne göre verir; metnin alt kenarını (notun üstünden) döner.</summary>
        static float FitBody(TMP_Text body, RectTransform note, float top, float bottom)
        {
            body.ForceMeshUpdate();
            float h = Mathf.Ceil(body.preferredHeight);
            var r = body.rectTransform;
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, -top);
            r.sizeDelta = new Vector2(r.sizeDelta.x, h);
            if (bottom > 0) note.sizeDelta = new Vector2(note.sizeDelta.x, top + h + bottom);
            return top + h;
        }

        Target? Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < targets.Length; i++) if (targets[i].id == id) return targets[i];
            return null;
        }

        void LateUpdate() { if (IsOpen && vm != null) Layout(); }

        /// <summary>Hedefin alanı katmanın yerel uzayında (y yukarı); bulunamazsa false.</summary>
        bool TargetRect(string id, out Rect r)
        {
            r = default;
            var t = Find(id);
            if (!t.HasValue) return false;
            var b = new Bounds2();
            if (t.Value.researchBranch >= 0 && research != null && research.BranchSpan(t.Value.researchBranch, out var broot, out float top, out float bottom))
            {
                var br = broot.rect;
                b.Add(area.InverseTransformPoint(broot.TransformPoint(new Vector3(br.xMin, br.yMax - top))));
                b.Add(area.InverseTransformPoint(broot.TransformPoint(new Vector3(br.xMax, br.yMax - bottom))));
            }
            else if (t.Value.rects != null)
            {
                foreach (var rt in t.Value.rects)
                {
                    if (rt == null || !rt.gameObject.activeInHierarchy) continue;
                    rt.GetWorldCorners(corners);
                    for (int i = 0; i < 4; i++) b.Add(area.InverseTransformPoint(corners[i]));
                }
            }
            if (!b.Any) return false;
            r = Rect.MinMaxRect(b.XMin, b.YMin, b.XMax, b.YMax);
            return true;
        }

        /// <summary>Noktaların sınırlayıcı kutusu (kapanış ayırmadan).</summary>
        struct Bounds2
        {
            public bool Any;
            public float XMin, YMin, XMax, YMax;

            public void Add(Vector3 p)
            {
                if (!Any) { XMin = XMax = p.x; YMin = YMax = p.y; Any = true; return; }
                XMin = Mathf.Min(XMin, p.x); YMin = Mathf.Min(YMin, p.y); XMax = Mathf.Max(XMax, p.x); YMax = Mathf.Max(YMax, p.y);
            }
        }

        void Layout()
        {
            var a = area.rect;
            bool tip = vm.IsTip;
            bool hasTarget = TargetRect(vm.Target, out var target);
            var h = hasTarget ? Rect.MinMaxRect(target.xMin - holePad, target.yMin - holePad, target.xMax + holePad, target.yMax + holePad) : new Rect(a.center, Vector2.zero);

            // Karartma yalnız adımda: hedefin çevresindeki dört dikdörtgen
            for (int i = 0; i < dims.Length; i++) DeskUi.SetActive(dims[i], !tip);
            DeskUi.SetActive(hole, !tip && hasTarget);
            if (!tip)
            {
                Place(dims[0], Rect.MinMaxRect(a.xMin, h.yMax, a.xMax, a.yMax));
                Place(dims[1], Rect.MinMaxRect(a.xMin, a.yMin, a.xMax, h.yMin));
                Place(dims[2], Rect.MinMaxRect(a.xMin, h.yMin, h.xMin, h.yMax));
                Place(dims[3], Rect.MinMaxRect(h.xMax, h.yMin, a.xMax, h.yMax));
                if (hasTarget) Place(hole, h);
            }

            var note = tip ? tipNote : stepNote;
            var size = note.sizeDelta;
            float gap = tip ? tipGap : noteGap, m = margin;
            Vector2 c;
            bool inside = false;
            if (!hasTarget) c = a.center;
            else
            {
                var below = new Vector2(h.center.x, h.yMin - gap - size.y / 2);
                var above = new Vector2(h.center.x, h.yMax + gap + size.y / 2);
                var right = new Vector2(h.xMax + gap + size.x / 2, h.center.y);
                var left = new Vector2(h.xMin - gap - size.x / 2, h.center.y);
                // İpucu hedefin yanına (sağ, sol; dikeyde sığıyorsa), adım notu altına ya da üstüne
                bool sideFits = h.center.y - size.y / 2 >= a.yMin + m && h.center.y + size.y / 2 <= a.yMax - m;
                if (tip && sideFits && right.x + size.x / 2 <= a.xMax - m) c = right;
                else if (tip && sideFits && left.x - size.x / 2 >= a.xMin + m) c = left;
                else if (below.y - size.y / 2 >= a.yMin + m) c = below;
                else if (above.y + size.y / 2 <= a.yMax - m) c = above;
                else if (right.x + size.x / 2 <= a.xMax - m) c = right;
                else if (left.x - size.x / 2 >= a.xMin + m) c = left;
                // Hedef ekranın çoğunu kaplıyorsa (harita) not hedefin sol alt köşesine, içeri
                else { c = new Vector2(h.xMin + m + 24 + size.x / 2, h.yMin + m + 24 + size.y / 2); inside = true; }
                c.x = Mathf.Clamp(c.x, a.xMin + m + size.x / 2, a.xMax - m - size.x / 2);
                c.y = Mathf.Clamp(c.y, a.yMin + m + size.y / 2, a.yMax - m - size.y / 2);
            }
            var pos = new Vector2(Mathf.Round(c.x), Mathf.Round(c.y));
            if (note.anchoredPosition != pos) note.anchoredPosition = pos;

            // Kalem oku: adım notunun kenarından hedefin kenarına (not hedefin içindeyse yok)
            bool showArrow = !tip && hasTarget && !inside && arrow != null;
            DeskUi.SetActive(arrow, showArrow);
            DeskUi.SetActive(arrowHead, showArrow);
            if (!showArrow) return;
            var noteRect = new Rect(pos - size / 2, size);
            Vector2 from, to;
            if (noteRect.yMax <= h.yMin) { from = new Vector2(pos.x, noteRect.yMax); to = new Vector2(Mathf.Clamp(pos.x + 80, h.xMin + 8, h.xMax - 8), h.yMin - 4); }
            else if (noteRect.yMin >= h.yMax) { from = new Vector2(pos.x, noteRect.yMin); to = new Vector2(Mathf.Clamp(pos.x + 80, h.xMin + 8, h.xMax - 8), h.yMax + 4); }
            else if (noteRect.xMin >= h.xMax) { from = new Vector2(noteRect.xMin, pos.y); to = new Vector2(h.xMax + 4, Mathf.Clamp(pos.y, h.yMin + 8, h.yMax - 8)); }
            else { from = new Vector2(noteRect.xMax, pos.y); to = new Vector2(h.xMin - 4, Mathf.Clamp(pos.y, h.yMin + 8, h.yMax - 8)); }
            if (from == lastFrom && to == lastTo) return;
            lastFrom = from; lastTo = to;
            var d = to - from;
            var ctrl = from + d * 0.5f + new Vector2(-d.y, d.x).normalized * Mathf.Min(40, d.magnitude * 0.25f);
            arrowPts.Clear(); arrowPts.Add(from); arrowPts.Add(ctrl); arrowPts.Add(to);
            arrow.SetPoints(arrowPts);
            var dir = (to - ctrl).normalized;
            var n = new Vector2(-dir.y, dir.x);
            headPts.Clear(); headPts.Add(to - dir * 10 + n * 6); headPts.Add(to); headPts.Add(to - dir * 10 - n * 6);
            arrowHead.SetPoints(headPts);
        }

        /// <summary>Ortaya çapalı (prefab'da öyle) dikdörtgeni yerleştirir; yalnız değişince yazar.</summary>
        static void Place(RectTransform rt, Rect r)
        {
            var s = new Vector2(Mathf.Max(0, r.width), Mathf.Max(0, r.height));
            if (rt.sizeDelta != s) rt.sizeDelta = s;
            if (rt.anchoredPosition != r.center) rt.anchoredPosition = r.center;
        }
    }
}
