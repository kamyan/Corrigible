using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Eğitim notunun gösterime hazır hâli: başlık, metin, adım sayacı; bilgi adımında TAMAM.</summary>
    public sealed class TutorialNoteData
    {
        public string Head, Title, Body, Ok, Skip;
        /// <summary>TAMAM düğmesi (bilgi adımı); yoksa adım oyuncunun eylemini bekler.</summary>
        public bool ShowOk;
    }

    /// <summary>
    /// Analistin eğitim notu: ekranın sağında kâğıt bir kart (başlık, metin, adım sayacı, TAMAM ve “Eğitimi atla”). Sağ altta
    /// durur; masa pozunda (K1) vardiya şalteri, baskın bitince sonuç kartı sağ altta olduğundan sağ üste çıkar. Kâğıt tıklamayı yutmaz, yalnız düğmeler
    /// tıklanır. Yalnız gösterir, adımı <c>SiginakTutorial</c> yürütür. Yeni adımda kısa bir kayma.
    /// </summary>
    public sealed class TutorialNoteView : MonoBehaviour
    {
        [SerializeField] RectTransform card;
        [Tooltip("Kartın köşe yeri (sağ alt / sağ üst) bu nesneyle taşınır.")]
        [SerializeField] RectTransform anchor;
        [SerializeField] Vector2 bottomOffset = new Vector2(-16, 96), topOffset = new Vector2(-16, -64);
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text head, title, body, okLabel, skipLabel;
        [SerializeField] Button ok, skip;

        public event Action OkClicked, SkipClicked;

        string shownTitle;
        float shownAt = -1;
        Vector2 rest;
        bool visible;

        void Awake()
        {
            rest = card.anchoredPosition;
            ok.onClick.AddListener(() => OkClicked?.Invoke());
            skip.onClick.AddListener(() => SkipClicked?.Invoke());
            group.alpha = 0;
            group.blocksRaycasts = false;
        }

        public void Show(TutorialNoteData d)
        {
            if (d.Title != shownTitle) { shownTitle = d.Title; shownAt = Time.unscaledTime; }
            head.text = d.Head;
            title.text = d.Title;
            body.text = d.Body;
            okLabel.text = d.Ok;
            skipLabel.text = d.Skip;
            ok.gameObject.SetActive(d.ShowOk);
            visible = true;
        }

        public void Hide() => visible = false;

        /// <summary>Sağ üst (masa pozu: şalter sağ altta) ya da sağ alt.</summary>
        public void SetTop(bool top)
        {
            if (anchor == null) return;
            var a = new Vector2(1, top ? 1 : 0);
            if (anchor.anchorMin == a && anchor.pivot == a) return;
            anchor.anchorMin = anchor.anchorMax = anchor.pivot = a;
            anchor.anchoredPosition = top ? topOffset : bottomOffset;
            shownAt = Time.unscaledTime;
        }

        void Update()
        {
            float target = visible ? 1 : 0;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / 0.18f);
            group.blocksRaycasts = group.interactable = visible;
            // Yeni adım: kart 24 px sağdan kayarak gelir (0,22 s)
            float t = shownAt < 0 ? 1 : Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.22f);
            card.anchoredPosition = rest + new Vector2(24 * (1 - Mathf.SmoothStep(0, 1, t)), 0);
        }
    }
}
