using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kapının kâğıdı (Overlay, ekranın solu): kim olduğu, kapıdan duyulan ve görülen, seçenek düğmeleri ve kısayol satırı.
    /// Düğmeler kurucunun hazırladığı yuvalara yazılır (çalışırken hiyerarşi yaratılmaz); fazla yuvalar gizlenir. Koyu
    /// mürekkep düğme asıl eylemdir, ötekiler çerçeveli kâğıt; seçilemeyen düğme soluk ve gerekçesini altında yazar.
    /// </summary>
    public sealed class DoorCardView : MonoBehaviour
    {
        [Serializable]
        public sealed class ButtonSlot
        {
            public GameObject root;
            public Button button;
            public CanvasGroup group;
            public Image fill;
            public TMP_Text key, label, detail;
            [Tooltip("Kısayol kutusunun çerçevesi.")]
            public Image keyBox;
        }

        [SerializeField] RectTransform paper;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text head, code, title, body, hint;
        [SerializeField] GameObject stamp;
        [SerializeField] TMP_Text stampText;
        [SerializeField] ButtonSlot[] buttons = Array.Empty<ButtonSlot>();
        [SerializeField, Min(0.01f)] float showSeconds = 0.3f;

        readonly string[] ids = new string[8];
        Vector2 restPosition;
        bool rested;
        float shownAt = -1;

        public bool IsOpen => gameObject.activeSelf;
        public bool Animating => IsOpen && shownAt >= 0 && Time.unscaledTime - shownAt < showSeconds;
        public int Slots => buttons.Length;
        /// <summary>Düğmeye basıldı (seçeneğin kimliği).</summary>
        public event Action<string> Clicked;

        public void Configure(RectTransform paperRoot, CanvasGroup canvasGroup, TMP_Text headText, TMP_Text codeText, TMP_Text titleText,
            TMP_Text bodyText, TMP_Text hintText, GameObject stampRoot, TMP_Text stampLabel, ButtonSlot[] slots)
        {
            paper = paperRoot; group = canvasGroup; head = headText; code = codeText; title = titleText; body = bodyText; hint = hintText;
            stamp = stampRoot; stampText = stampLabel; buttons = slots;
        }

        void Awake()
        {
            Rest();
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                buttons[i].button.onClick.AddListener(() => Press(k));
            }
        }

        void Rest()
        {
            if (rested) return;
            rested = true;
            restPosition = paper.anchoredPosition;
        }

        /// <summary>Kâğıdı açar (açıksa yerinde yeniden yazar); <paramref name="animate"/> soldan kayarak gelir.</summary>
        public void Show(DoorCardData data, bool animate)
        {
            Rest();
            bool was = gameObject.activeSelf;
            gameObject.SetActive(true);
            Bind(data);
            if (!was && animate && Application.isPlaying) { shownAt = Time.unscaledTime; Pose(0); }
            else if (!was) { shownAt = -1; Pose(1); }
        }

        public void Bind(DoorCardData d)
        {
            var t = SiginakTheme.Active;
            head.text = d.Head ?? "";
            code.text = d.Code ?? "";
            title.text = d.Title ?? "";
            body.text = d.Body ?? "";
            hint.text = d.Hint ?? "";
            hint.gameObject.SetActive(!string.IsNullOrEmpty(d.Hint));
            stamp.SetActive(!string.IsNullOrEmpty(d.Stamp));
            stampText.text = d.Stamp ?? "";
            for (int i = 0; i < buttons.Length; i++)
            {
                var slot = buttons[i];
                bool on = i < d.Buttons.Count;
                slot.root.SetActive(on);
                ids[i] = on ? d.Buttons[i].Id : null;
                if (!on) continue;
                var b = d.Buttons[i];
                slot.key.text = (i + 1).ToString();
                slot.label.text = b.Label;
                slot.detail.gameObject.SetActive(!string.IsNullOrEmpty(b.Detail));
                slot.detail.text = b.Detail ?? "";
                // Bedel ya da gerekçe kısalmaz; yer yetmezse düğme yazısı üç noktayla kısalır
                var detailLayout = slot.detail.GetComponent<LayoutElement>();
                if (detailLayout != null) detailLayout.minWidth = string.IsNullOrEmpty(b.Detail) ? 0 : slot.detail.GetPreferredValues(b.Detail).x;
                slot.button.interactable = b.Enabled;
                slot.group.alpha = b.Enabled ? 1 : 0.5f;
                // Koyu mürekkep düğme: asıl eylem; ötekiler çerçeveli kâğıt
                Color fill = t.ColorOf(b.Primary ? "ui.inkButton" : "ui.frameButton"), ink = t.ColorOf(b.Primary ? "ui.inkButtonText" : "ink");
                slot.fill.color = fill;
                slot.label.color = slot.key.color = ink;
                slot.detail.color = b.Primary ? ink : t.ColorOf("ink2");
                if (slot.keyBox != null) slot.keyBox.color = ink;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(paper);
        }

        /// <summary>Kısayol (1–n): düğme açık ve seçilebilirse ona basar.</summary>
        public bool PressKey(int index)
        {
            if (index < 0 || index >= buttons.Length || ids[index] == null || !buttons[index].button.interactable) return false;
            Press(index);
            return true;
        }

        void Press(int index)
        {
            if (Animating || ids[index] == null) return;
            Clicked?.Invoke(ids[index]);
        }

        public void Hide()
        {
            shownAt = -1;
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (shownAt < 0) return;
            float p = Mathf.Clamp01((Time.unscaledTime - shownAt) / showSeconds);
            Pose(p);
            if (p >= 1) shownAt = -1;
        }

        /// <summary>Soldan kayarak gelir ve hafifçe döner; 0 → 1.</summary>
        void Pose(float p)
        {
            float e = 1 - (1 - p) * (1 - p);
            paper.anchoredPosition = restPosition + new Vector2(-90, 0) * (1 - e);
            paper.localRotation = Quaternion.Euler(0, 0, -1.2f + 2 * (1 - e));
            group.alpha = Mathf.Clamp01(p * 2.5f);
        }
    }
}
