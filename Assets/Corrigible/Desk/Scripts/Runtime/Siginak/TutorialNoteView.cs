using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Eğitim notunun gösterime hazır hâli: başlık, adım sayacı, metin; bilgi adımında “Boşluk sonraki”.</summary>
    public sealed class TutorialNoteData
    {
        public string Head, Step, Title, Body, Ok, Skip;
        /// <summary>“Boşluk sonraki” (bilgi adımı); yoksa adım oyuncunun eylemini bekler.</summary>
        public bool ShowOk;
    }

    /// <summary>
    /// Analistin eğitim notu (Revizyon 2 · S1/S6): sağ altta el notu kâğıdı, başlıkta “ANALİST · NOT” ve “ADIM n/12”, metin
    /// Kalam; altta “Boşluk sonraki · Esc notları kapat”. Baskında EMRİ İLET'in üstünde (“BASKIN · ADIM n/30”). Kâğıdın boyu
    /// metne göre uzar. Kâğıt tıklamayı yutmaz, yalnız düğmeler tıklanır. Yalnız gösterir, adımı <c>SiginakTutorial</c>
    /// yürütür. Yeni adımda kısa bir kayma.
    /// </summary>
    public sealed class TutorialNoteView : MonoBehaviour
    {
        [SerializeField] RectTransform card;
        [Tooltip("Kartın köşe yeri (sağ alt / sağ üst) bu nesneyle taşınır.")]
        [SerializeField] RectTransform anchor;
        [Tooltip("Masada sağ alt köşeden; baskında sağ üst köşeden (raid_tokens.json → trainingNote: 1560 × 796, 336 px).")]
        [SerializeField] Vector2 bottomOffset = new Vector2(-16, 16), raidOffset = new Vector2(-24, -796);
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text head, step, title, body, okLabel, skipLabel;
        [SerializeField] Button ok, skip;
        [Tooltip("Metnin üstünde ve altında kalan boy (başlık, alt satır, kenar boşlukları); kâğıdın boyu metinle uzar.")]
        [SerializeField, Min(0)] float chromeHeight = 92;

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
            if (step != null) step.text = d.Step ?? "";
            if (title != null) title.text = d.Title ?? "";
            body.text = d.Body;
            // Kâğıt metne göre uzar
            float h = body.GetPreferredValues(d.Body, body.rectTransform.rect.width, 0).y;
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, Mathf.Ceil(h + chromeHeight));
            okLabel.text = d.Ok;
            skipLabel.text = d.Skip;
            ok.gameObject.SetActive(d.ShowOk);
            visible = true;
        }

        public void Hide() => visible = false;

        /// <summary>Baskında sağ üst köşeden (EMRİ İLET'in üstü), öteki her yerde sağ alt.</summary>
        public void SetRaid(bool raid)
        {
            if (anchor == null) return;
            var a = new Vector2(1, raid ? 1 : 0);
            if (anchor.anchorMin == a && anchor.pivot == a) return;
            anchor.anchorMin = anchor.anchorMax = anchor.pivot = a;
            anchor.anchoredPosition = raid ? raidOffset : bottomOffset;
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
