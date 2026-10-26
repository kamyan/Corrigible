using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Yeni cephe onay fişi (K3 notu): 404 px dizin kâğıdı, +1,2° dönük, raptiye sağ üstte. “YENİ CEPHE · ONAY”, bölge,
    /// komşu satırı, bölgenin üç sektörü (baştan kilitli), tek cephe kuralı, “Enter · SEÇİLİ BÖLGEDE CEPHE AÇ” ve
    /// “Esc · Vazgeç”. Yalnız bir aday seçiliyken açılır; kararı bağlayıcı verir.
    /// </summary>
    public sealed class FrontSlip : MonoBehaviour
    {
        [SerializeField] RectTransform paper;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text head, region, neighbor, occupation, rule, reason;
        [SerializeField] TMP_Text[] sectors = Array.Empty<TMP_Text>();
        [SerializeField] Button action, cancel;
        [SerializeField] Image actionFill;
        [SerializeField] TMP_Text actionLabel, cancelLabel;
        [SerializeField] RectTransform pin;

        float openAt = -1;
        Vector2 rest;

        public event Action ActionClicked, CancelClicked;
        public bool IsOpen => gameObject.activeSelf;
        public string RegionId { get; private set; }
        public Vector3 PinWorld => pin.position;
        public Vector2 PaperSize => paper.rect.size;

        public void Configure(RectTransform paperRoot, CanvasGroup fade, TMP_Text headText, TMP_Text regionText, TMP_Text neighborText,
            TMP_Text occupationText, TMP_Text ruleText, TMP_Text reasonText, TMP_Text[] sectorLines, Button actionButton, Button cancelButton,
            Image actionBack, TMP_Text actionText, TMP_Text cancelText, RectTransform pinPoint)
        {
            paper = paperRoot; group = fade; head = headText; region = regionText; neighbor = neighborText; occupation = occupationText;
            rule = ruleText; reason = reasonText; sectors = sectorLines; action = actionButton; cancel = cancelButton; actionFill = actionBack;
            actionLabel = actionText; cancelLabel = cancelText; pin = pinPoint;
        }

        void Awake()
        {
            rest = paper.anchoredPosition;
            action.onClick.AddListener(() => ActionClicked?.Invoke());
            cancel.onClick.AddListener(() => CancelClicked?.Invoke());
        }

        public void Show(FrontSlipData d)
        {
            bool reopen = !IsOpen || RegionId != d.RegionId;
            RegionId = d.RegionId;
            var t = SiginakTheme.Active;
            gameObject.SetActive(true);
            head.text = d.Head;
            region.text = d.Region;
            neighbor.text = d.Neighbor;
            occupation.text = d.Occupation;
            for (int i = 0; i < sectors.Length; i++)
            {
                var s = i < d.Sectors.Count ? d.Sectors[i] : null;
                sectors[i].gameObject.SetActive(s != null);
                // Tür ink3 rengiyle (zengin metin); biçim çevrilir, renk etiketi çeviriye girmez
                if (s != null) sectors[i].text = Corrigible.Core.Loc.F("{0}  {1}  ·  {2}", s.No, s.Name,
                    "<color=#" + ColorUtility.ToHtmlStringRGB(t.ColorOf("ink3")) + ">" + s.Kind + "</color>");
            }
            rule.text = d.Rule;
            action.interactable = d.ActionEnabled;
            actionLabel.text = d.Action;
            cancelLabel.text = d.Cancel;
            actionFill.color = t.ColorOf(d.ActionEnabled ? "ui.inkButton" : "ui.disabledFill");
            actionLabel.color = t.ColorOf(d.ActionEnabled ? "ui.inkButtonText" : "ui.disabledText");
            reason.gameObject.SetActive(!d.ActionEnabled && !string.IsNullOrEmpty(d.Reason));
            reason.text = d.Reason ?? "";
            if (reopen) openAt = Time.unscaledTime;
            LayoutRebuilder.ForceRebuildLayoutImmediate(paper);
            Update();
        }

        public void Hide()
        {
            RegionId = null;
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (openAt < 0) return;
            float p = Mathf.Clamp01((Time.unscaledTime - openAt) / Mathf.Max(0.01f, SiginakTheme.Active.NumberOf("motion.slipOpenS", 0.14f)));
            float e = 1 - (1 - p) * (1 - p);
            paper.anchoredPosition = rest + new Vector2(0, 12 * (1 - e));
            group.alpha = e;
            if (p >= 1) openAt = -1;
        }
    }
}
