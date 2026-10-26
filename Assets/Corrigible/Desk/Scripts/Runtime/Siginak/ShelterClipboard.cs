using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sığınak özeti panosu (K4 notu): kıskaçlı panodaki World Space kâğıt (600 × 800). Üstte özet (TOPLULUK · ENERJİ ·
    /// ERZAK · SON VARDİYA), altta seçili odanın dizin kartı −1,2°: ad + SEÇİLİ, seviye/personel, açıklama, sarı sonuç notu,
    /// maliyet, hazırlık, eylemler (ONAR / GELİŞTİR / EN ÜST SEVİYE · − PERS. · + PERS.). Düğmeler her zaman tıklanır:
    /// yapılamayan eylem komutu dener, düğme ±3 px sarsılır ve gerekçe teleksten basılır (K4 › etkileşim).
    /// </summary>
    public sealed class ShelterClipboard : MonoBehaviour
    {
        [Serializable] public sealed class SlipButton
        {
            public Button button;
            public RectTransform root;
            public Image fill, edge, stampFrame;
            public TMP_Text label, stamp;
            public bool ink;
        }

        [SerializeField] TMP_Text title, shift;
        [SerializeField] TMP_Text[] labels = Array.Empty<TMP_Text>(), values = Array.Empty<TMP_Text>();
        [SerializeField] RectTransform summaryStamp;
        [SerializeField] TMP_Text summaryStampText;
        [SerializeField] RectTransform card;
        [SerializeField] CanvasGroup cardGroup;
        [SerializeField] TMP_Text roomName, chip, roomLine, description, note, cost, readiness;
        [SerializeField] GameObject noteRoot;
        [SerializeField] SlipButton action, minus, plus;
        [SerializeField] RectTransform doneStamp;
        [SerializeField] Sprite frameSolid, frameDashed;

        string roomId;
        float swapAt = -1, stampAt = -1, shakeAt = -1;
        SlipButton shaking;
        Vector2 cardRest, shakeRest;

        public event Action ActionClicked, MinusClicked, PlusClicked;

        public void Configure(TMP_Text titleText, TMP_Text shiftText, TMP_Text[] rowLabels, TMP_Text[] rowValues, RectTransform stampRoot, TMP_Text stampWord,
            RectTransform cardRoot, CanvasGroup cardFade, TMP_Text nameText, TMP_Text chipText, TMP_Text lineText, TMP_Text descriptionText,
            GameObject noteBox, TMP_Text noteText, TMP_Text costText, TMP_Text readinessText, SlipButton actionButton, SlipButton minusButton,
            SlipButton plusButton, RectTransform done, Sprite solid, Sprite dashed)
        {
            title = titleText; shift = shiftText; labels = rowLabels; values = rowValues; summaryStamp = stampRoot; summaryStampText = stampWord;
            card = cardRoot; cardGroup = cardFade; roomName = nameText; chip = chipText; roomLine = lineText; description = descriptionText;
            noteRoot = noteBox; note = noteText; cost = costText; readiness = readinessText; action = actionButton; minus = minusButton; plus = plusButton;
            doneStamp = done; frameSolid = solid; frameDashed = dashed;
        }

        void Awake()
        {
            action.button.onClick.AddListener(() => ActionClicked?.Invoke());
            minus.button.onClick.AddListener(() => MinusClicked?.Invoke());
            plus.button.onClick.AddListener(() => PlusClicked?.Invoke());
        }

        public void Bind(ShelterBoardData d)
        {
            var t = SiginakTheme.Active;
            title.text = d.Title;
            shift.text = d.Shift;
            for (int i = 0; i < labels.Length; i++)
            {
                bool on = i < d.Summary.Count;
                labels[i].gameObject.SetActive(on);
                values[i].gameObject.SetActive(on);
                if (!on) continue;
                labels[i].text = d.Summary[i].label;
                values[i].text = d.Summary[i].value;
            }
            summaryStamp.gameObject.SetActive(!string.IsNullOrEmpty(d.SummaryStamp));
            summaryStampText.text = d.SummaryStamp ?? "";

            if (roomId != d.RoomId)
            {
                // Panodaki kart kayarak değişir (0,14 s); damga yeni kartta görünmez
                if (roomId != null) swapAt = Time.unscaledTime;
                roomId = d.RoomId;
                doneStamp.gameObject.SetActive(false);
            }
            roomName.text = d.RoomName;
            chip.text = d.SelectedChip;
            roomLine.text = d.RoomLine;
            description.text = d.Description;
            noteRoot.SetActive(!string.IsNullOrEmpty(d.Note));
            note.text = d.Note ?? "";
            cost.text = d.Cost;
            readiness.text = d.Readiness;
            readiness.color = t.ColorOf(d.ReadinessOk ? "status.good" : "status.threat");
            Style(action, d.Action);
            Style(minus, d.Minus);
            Style(plus, d.Plus);
            // Kartın yeri düzen grubundan gelir; kayma bu yere göre ofsettir (düzen konumu ezilmesin)
            if (swapAt >= 0 && cardRest != default) card.anchoredPosition = cardRest;
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)card.parent);
            cardRest = card.anchoredPosition;
            Update();
        }

        void Style(SlipButton b, ShelterButtonData d)
        {
            var t = SiginakTheme.Active;
            b.label.text = d.Label;
            bool normal = d.State == SlipButtonState.Normal;
            // K8: mürekkep düğme #1E1B16 · çerçeve düğme #FBF7EC + mürekkep kenar · devre dışı soluk zemin + kesik kenar ·
            // yetersiz kaynak kırmızı düz kenar + damga
            b.fill.color = t.ColorOf(normal ? (b.ink ? "ui.inkButton" : "ui.frameButton") : d.State == SlipButtonState.Disabled ? "ui.disabledFill" : "ui.frameButton");
            b.label.color = t.ColorOf(normal ? (b.ink ? "ui.inkButtonText" : "ink") : "ui.disabledText");
            bool dashed = d.State == SlipButtonState.Disabled;
            b.edge.sprite = dashed ? frameDashed : frameSolid;
            b.edge.type = dashed ? Image.Type.Tiled : Image.Type.Sliced;
            // Kesik kenar yalnız kenarda döşenir (orta dolgu ızgara gibi görünüyordu)
            b.edge.fillCenter = !dashed;
            b.edge.color = t.ColorOf(d.State == SlipButtonState.Short ? "status.threat" : dashed ? "ui.disabledLine" : "ink");
            b.edge.enabled = !(normal && b.ink);
            bool stamped = d.State == SlipButtonState.Short && !string.IsNullOrEmpty(d.Stamp);
            var stampRoot = b.stampFrame.transform.parent.gameObject;
            if (stampRoot.activeSelf != stamped) stampRoot.SetActive(stamped);
            if (stamped) b.stamp.text = d.Stamp;
        }

        /// <summary>Yapılamadı: düğme ±3 px sarsılır (0,2 s).</summary>
        public void Shake(int which)
        {
            var b = which == 0 ? action : which == 1 ? minus : plus;
            if (shaking != null) shaking.root.anchoredPosition = shakeRest;
            shaking = b;
            shakeRest = b.root.anchoredPosition;
            shakeAt = Time.unscaledTime;
        }

        /// <summary>ONAR / GELİŞTİR uygulandı: kartta “EMİR UYGULANDI” damgası iner.</summary>
        public void StampDone()
        {
            doneStamp.gameObject.SetActive(true);
            stampAt = Time.unscaledTime;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (swapAt >= 0)
            {
                float p = Mathf.Clamp01((now - swapAt) / Mathf.Max(0.01f, SiginakTheme.Active.NumberOf("motion.slipOpenS", 0.14f)));
                float e = 1 - (1 - p) * (1 - p);
                card.anchoredPosition = cardRest + new Vector2(24 * (1 - e), 0);
                cardGroup.alpha = 0.4f + 0.6f * e;
                if (p >= 1) swapAt = -1;
            }
            if (stampAt >= 0)
            {
                float p = Mathf.Clamp01((now - stampAt) / Mathf.Max(0.01f, SiginakTheme.Active.NumberOf("motion.stampS", 0.1f)));
                doneStamp.localScale = Vector3.one * Mathf.Lerp(1.4f, 1, p);
                if (p >= 1) stampAt = -1;
            }
            if (shakeAt >= 0 && shaking != null)
            {
                float p = Mathf.Clamp01((now - shakeAt) / 0.2f);
                shaking.root.anchoredPosition = shakeRest + new Vector2(Mathf.Sin(p * Mathf.PI * 6) * 3 * (1 - p), 0);
                if (p >= 1) { shaking.root.anchoredPosition = shakeRest; shakeAt = -1; shaking = null; }
            }
        }
    }
}
