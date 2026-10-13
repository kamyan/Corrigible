using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sahte zafer (F8.3, taslak <c>drafts/sahte_zafer_a/b</c>): masanın kâğıtlarına bilerek benzemeyen kusursuz beyaz resmî
    /// duyuru. <see cref="FakeWinVM.HoldSeconds"/> inandırır, sonra <see cref="FakeWinVM.GlitchSeconds"/> bozulur: renk kayması,
    /// kayan dilimler, damganın üstüne YALAN, YZ satırı. Bitince <see cref="Finished"/> (köprü oyun sonu raporunu açar).
    /// Bozulma adımları 90 ms'de bir değişir; her kare bellek ayırmaz.
    /// </summary>
    public sealed class FakeWinView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] RectTransform letter;
        [SerializeField] TMP_Text crest, org, orgSub, title, sub, signature, signatureRole;
        [SerializeField] TMP_Text[] paragraphs = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text[] statCaps = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text[] statValues = Array.Empty<TMP_Text>();
        [SerializeField] StampView stamp, lie;
        [Tooltip("Rakamlar: başlık ve değer alt alta, sütunlar arası boşluk (CSS gap 40); x metne göre.")]
        [SerializeField] float statsLeft = 40, statsGap = 40;
        [Header("Bozulma")]
        [SerializeField] TMP_Text fringeRed, fringeBlue;
        [SerializeField] RectTransform[] slices = Array.Empty<RectTransform>();
        [Tooltip("Kayan dilimlerdeki metin kopyaları: 0 başlık, 1 ikinci paragraf.")]
        [SerializeField] TMP_Text[] sliceTexts = Array.Empty<TMP_Text>();
        [SerializeField] Graphic scan;
        [SerializeField] GameObject aiCard;
        [SerializeField] TMP_Text aiLine;
        [SerializeField] float step = 0.09f;

        FakeWinVM vm;
        float started = -1, nextStep;
        int phase;   // 0 inandırır, 1 bozulur, 2 bitti
        uint seed = 17;
        Vector2[] sliceBase;

        public bool IsOpen => root != null && root.activeSelf;
        public static FakeWinView Instance { get; private set; }
        /// <summary>Bozulma başladı (ses: duyuru kırılır).</summary>
        public event Action Glitched;
        public event Action Finished;

        void Awake()
        {
            Instance = this;
            sliceBase = new Vector2[slices.Length];
            for (int i = 0; i < slices.Length; i++) sliceBase[i] = slices[i].anchoredPosition;
            root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Bind(FakeWinVM model)
        {
            bool fresh = model != null && vm == null;
            vm = model;
            root.SetActive(model != null);
            if (model == null) { started = -1; return; }
            var T = DeskLoc.Instance;
            DeskUi.SetText(crest, model.Crest);
            DeskUi.SetText(org, T.T(model.Org));
            DeskUi.SetText(orgSub, T.T(model.OrgSub));
            DeskUi.SetText(title, T.T(model.Title));
            DeskUi.SetText(fringeRed, T.T(model.Title));
            DeskUi.SetText(fringeBlue, T.T(model.Title));
            DeskUi.SetText(sub, T.T(model.Sub));
            for (int i = 0; i < paragraphs.Length; i++)
            {
                bool on = i < model.Paragraphs.Count;
                DeskUi.SetActive(paragraphs[i], on);
                if (on) DeskUi.SetText(paragraphs[i], T.T(model.Paragraphs[i]));
            }
            float x = statsLeft;
            for (int i = 0; i < statCaps.Length; i++)
            {
                bool on = i < model.Stats.Count;
                DeskUi.SetActive(statCaps[i], on);
                DeskUi.SetActive(statValues[i], on);
                if (!on) continue;
                DeskUi.SetText(statCaps[i], T.T(model.Stats[i].Label));
                DeskUi.SetText(statValues[i], model.Stats[i].Value);
                // Sütun genişliği başlığın ya da değerin genişi (çeviride başlık uzayabilir)
                float w = Mathf.Max(DeskUi.TextWidth(statCaps[i], statCaps[i].text), DeskUi.TextWidth(statValues[i], statValues[i].text));
                SetX(statCaps[i].rectTransform, x, w); SetX(statValues[i].rectTransform, x, w);
                x += Mathf.Round(w) + statsGap;
            }
            if (sliceTexts.Length > 0) DeskUi.SetText(sliceTexts[0], T.T(model.Title));
            if (sliceTexts.Length > 1) DeskUi.SetText(sliceTexts[1], model.Paragraphs.Count > 1 ? T.T(model.Paragraphs[1]) : "");
            DeskUi.SetText(signature, model.Signature);
            DeskUi.SetText(signatureRole, T.T(model.SignatureRole));
            stamp.Set(T.T(model.Stamp), StampInk.Green, StampSize.Large, -7, "fakewin.stamp");
            lie.Set(T.T(model.LieStamp), StampInk.Red, StampSize.Large, -11, "fakewin.lie", 44);
            DeskUi.SetText(aiLine, model.AiLine);
            if (fresh) Begin();
        }

        static void SetX(RectTransform r, float x, float w)
        {
            r.anchoredPosition = new Vector2(x, r.anchoredPosition.y);
            r.sizeDelta = new Vector2(Mathf.Ceil(w), r.sizeDelta.y);
        }

        void Begin()
        {
            started = Time.unscaledTime; phase = 0; nextStep = 0;
            SetGlitch(false);
        }

        /// <summary>Yakalama ve testler için: bozulma karesine atla (<paramref name="glitched"/>) ya da temiz kare.</summary>
        public void ShowFrame(bool glitched)
        {
            started = -1;
            SetGlitch(glitched);
            if (glitched) Jitter();
        }

        void SetGlitch(bool on)
        {
            DeskUi.SetActive(fringeRed, on);
            DeskUi.SetActive(fringeBlue, on);
            DeskUi.SetActive(lie, on);
            DeskUi.SetActive(aiCard, on);
            if (scan != null) DeskUi.SetActive(scan, on);
            foreach (var s in slices) DeskUi.SetActive(s, on);
            letter.localEulerAngles = new Vector3(0, 0, on ? -1 : 0);
            stamp.GetComponent<CanvasGroup>().alpha = on ? 0.5f : 1;
        }

        float Rand() { seed = seed * 1664525 + 1013904223; return (seed >> 8) / 16777216f; }

        void Jitter()
        {
            for (int i = 0; i < slices.Length; i++)
                slices[i].anchoredPosition = sliceBase[i] + new Vector2(Mathf.Round((Rand() - 0.5f) * 60), 0);
            ((RectTransform)fringeRed.transform).anchoredPosition = new Vector2(3 + Mathf.Round(Rand() * 3), 0);
            ((RectTransform)fringeBlue.transform).anchoredPosition = new Vector2(-3 - Mathf.Round(Rand() * 3), 0);
        }

        void Update()
        {
            if (started < 0 || vm == null) return;
            float t = Time.unscaledTime - started;
            if (phase == 0 && t >= vm.HoldSeconds) { phase = 1; SetGlitch(true); Glitched?.Invoke(); }
            if (phase == 1)
            {
                if (Time.unscaledTime >= nextStep) { nextStep = Time.unscaledTime + step; Jitter(); }
                if (t >= vm.HoldSeconds + vm.GlitchSeconds) { phase = 2; started = -1; Finished?.Invoke(); }
            }
        }
    }
}
