using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Onay kartı (açık karar 6; tasarımda çizilmedi): karartma + bildirim kâğıdı (çift çizgili başlık, büyük başlık, gövde)
    /// + mürekkep “Kararı imzala” (Enter) ve kâğıt “Vazgeç” (Esc). Küresel Son Şalter ve tehlikeli bölge eylemi için.
    /// Enter/Esc'yi <see cref="DeskInput"/> iletir (aynı karede Esc'nin pencereyi de kapatmaması için tek girdi sahibi).
    /// </summary>
    public sealed class ConfirmModalView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text header, title, body;
        [SerializeField] DeskButton sign, cancel;
        [SerializeField] DeskRow buttons;
        [Tooltip("Karartma (150 ms'de belirir).")]
        [SerializeField] CanvasGroup scrim;
        [Tooltip("Kâğıt (8 px düşerek 180 ms'de belirir).")]
        [SerializeField] CanvasGroup card;
        [SerializeField] float dropDistance = 8;
        [Tooltip("Kâğıt boyu = gövde üstü + gövde metni + boşluk + düğmeler + alt kenar.")]
        [SerializeField] float bodyTop = 98, bodyGap = 28, buttonsHeight = 30, bottomPad = 24;

        Action onSign;
        DeskTween scrimIn, cardIn;
        float cardY;

        public static ConfirmModalView Instance { get; private set; }
        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            Instance = this;
            root.SetActive(false);
            if (card != null) cardY = ((RectTransform)card.transform).anchoredPosition.y;
            sign.onClick.AddListener(Sign);
            cancel.onClick.AddListener(Cancel);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Çeviri tablosuna toplanan sabit kelimeler.</summary>
        internal static readonly string[] Words = { Loc.N("Kararı imzala"), Loc.N("Vazgeç") };

        public void Show(string titleText, string bodyText, Action onConfirm, string signLabel = "Kararı imzala", string cancelLabel = "Vazgeç")
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(header, T.T(Loc.N("KYGA · KARAR")));
            DeskUi.SetText(title, T.T(titleText));
            DeskUi.SetText(body, T.T(bodyText));
            sign.SetLabel(signLabel); sign.SetShortcut("Enter");
            cancel.SetLabel(cancelLabel); cancel.SetShortcut("Esc");
            onSign = onConfirm;
            root.SetActive(true);
            buttons.Layout();
            if (card != null)
            {
                // Kâğıt içeriğe göre: gövde metninin yüksekliği kadar uzar
                float bodyH = Mathf.Max(20, body.GetPreferredValues(body.text, body.rectTransform.rect.width, 0).y);
                var br = body.rectTransform;
                br.sizeDelta = new Vector2(br.sizeDelta.x, Mathf.Ceil(bodyH));
                var cr = (RectTransform)card.transform;
                cr.sizeDelta = new Vector2(cr.sizeDelta.x, Mathf.Round(bodyTop + bodyH + bodyGap + buttonsHeight + bottomPad));
            }
            var theme = DeskTheme.Active;
            scrimIn.Snap(0); scrimIn.To(1, theme != null ? theme.modalScrim : 0.15f);
            cardIn.Snap(0); cardIn.To(1, theme != null ? theme.modalDrop : 0.18f);
            Animate();
        }

        public void Sign()
        {
            var a = onSign;
            Close();
            a?.Invoke();
        }

        public void Cancel() => Close();

        void Close() { root.SetActive(false); onSign = null; }

        void Animate()
        {
            if (scrim != null) scrim.alpha = scrimIn.Value;
            if (card == null) return;
            card.alpha = cardIn.Value;
            var rt = (RectTransform)card.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, cardY + dropDistance * (1 - cardIn.Value));
        }

        void Update()
        {
            if (!IsOpen) return;
            bool a = scrimIn.Tick(Time.unscaledDeltaTime);
            if (cardIn.Tick(Time.unscaledDeltaTime) || a) Animate();
        }
    }
}
