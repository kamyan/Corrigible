using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kapının kâğıdı (Revizyon 2 · S4; Overlay, sol 40 · üst 64, 470 px, CSS −0,8°): başlık, kim olduğu, kapıdan duyulan;
    /// gözetleme deliğinden görülen ve interkomun cevabı ayrı bloklarda (göz işareti + GÖZETLEME DELİĞİ); seçenek satırları
    /// (50 px, sağda Courier bedel/etki ya da neden) ve Esc satırı. Sonuçta seçim satırı, metin ve tek geniş Enter düğmesi;
    /// ölümcülde kâğıt −1,4°, kırmızı çift çizgili damga ve son cümle, düğme damgadan 0,6 s sonra. Satırlar ve bloklar
    /// sahnede hazır durur (çalışırken hiyerarşi yaratılmaz); fazla yuvalar gizlenir.
    /// </summary>
    public sealed class DoorCardView : MonoBehaviour
    {
        [Serializable]
        public sealed class OptionSlot
        {
            public GameObject root;
            [Tooltip("Satırın yüksekliği: en az 50 px, yazı sarınca uzar.")]
            public LayoutElement layout;
            public Button button;
            public UiHoverTarget hover;
            [Tooltip("Yüz (zemin, kenar, yazılar): üzerinde 2 px kalkar, basılıyken 1 px iner; gölge altında kalır.")]
            public RectTransform face;
            public Image fill, edge, shadow;
            [Tooltip("Kısayol fişi: çerçeve (normal) ve pirinç zemin (üzerinde).")]
            public Image keyBox, keyFill;
            public TMP_Text key, label, detail;
            [Tooltip("Kullanılmış seçeneğin damgası (BAKILDI).")]
            public RectTransform used;
            public Image usedFrame;
            public TMP_Text usedText;
        }

        [Serializable]
        public sealed class Observation
        {
            public GameObject root;
            public Image icon;
            public TMP_Text label, text;
        }

        [Tooltip("Kayan ve dönen kâğıt (gölge + kâğıt); pivot üst orta.")]
        [SerializeField] RectTransform sheet;
        [Tooltip("Kâğıdın içeriği (dikey düzen; boyu içeriğe göre).")]
        [SerializeField] RectTransform paper;
        [SerializeField] RectTransform shadow;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text head, code, title, choice, body, epilogue;
        [SerializeField] Observation[] observations = Array.Empty<Observation>();
        [SerializeField] Sprite eyeIcon, intercomIcon;
        [SerializeField] GameObject stampBlock;
        [SerializeField] RectTransform stamp;
        [SerializeField] TMP_Text stampText;
        [SerializeField] GameObject optionBlock;
        [SerializeField] OptionSlot[] buttons = Array.Empty<OptionSlot>();
        [Tooltip("Sonuçtaki geniş düğme (Enter).")]
        [SerializeField] GameObject actionBlock;
        [SerializeField] Button action;
        [SerializeField] UiHoverTarget actionHover;
        [SerializeField] RectTransform actionFace;
        [SerializeField] Image actionShadow;
        [SerializeField] TMP_Text actionLabel;
        [SerializeField] GameObject hintRow;
        [SerializeField] TMP_Text hintKey, hint;
        [SerializeField] Sprite frameSolid, frameDashed;

        const float ShowS = 0.22f, HideS = 0.18f, ShakeS = 0.1f, UsedS = 0.1f, StampS = 0.12f, ActionDelayS = 0.6f;
        const float PeekTypeS = 0.25f, IntercomCharS = 0.03f, IntercomHissS = 0.2f, DetailMaxW = 210;

        readonly string[] ids = new string[12];
        DoorButtonData[] data = Array.Empty<DoorButtonData>();
        readonly string[] obsShown = new string[4];
        readonly float[] typeAt = { -1, -1, -1, -1 }, typeSeconds = new float[4];
        float[] usedAt = Array.Empty<float>();
        string actionId;
        Vector2 rest;
        bool rested, fatal;
        float angle = 0.8f, showAt = -1, hideAt = -1, shakeAt = -1, fatalAt = -1;

        public bool IsOpen => gameObject.activeSelf && hideAt < 0;
        public bool Animating => showAt >= 0 || hideAt >= 0;
        public int Slots => buttons.Length;
        /// <summary>Geniş düğme görünür (ölümcülde damgadan 0,6 s sonra).</summary>
        public bool ActionReady => IsOpen && actionId != null && actionBlock.activeSelf;
        /// <summary>İnterkomun cevabı hâlâ yazılıyor (amber nokta yanık kalır).</summary>
        public bool IntercomTyping { get; private set; }
        /// <summary>Düğmeye basıldı (seçeneğin ya da geniş düğmenin kimliği).</summary>
        public event Action<string> Clicked;

        static SiginakTheme T => SiginakTheme.Active;
        static Color C(string key) => T.ColorOf(key);

        void Awake()
        {
            Rest();
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                buttons[i].button.onClick.AddListener(() => Press(k));
            }
            action.onClick.AddListener(() => PressAction());
        }

        void Rest()
        {
            if (rested) return;
            rested = true;
            rest = sheet.anchoredPosition;
        }

        /// <summary>Kâğıdı açar (açıksa yerinde yeniden yazar); <paramref name="animate"/> soldan 0,22 s'de kayarak gelir.</summary>
        public void Show(DoorCardData d, bool animate)
        {
            Rest();
            bool was = IsOpen;
            hideAt = -1;
            gameObject.SetActive(true);
            if (!was)
            {
                // Yeni ziyaret: bloklar yazılmadan dolu gelir, damgalar yerinde, ölümcül an henüz yok
                Array.Clear(obsShown, 0, obsShown.Length);
                for (int i = 0; i < typeAt.Length; i++) typeAt[i] = -1;
                fatalAt = -1;
            }
            Bind(d, was);
            if (!was && animate && Application.isPlaying) { showAt = Time.unscaledTime; Pose(0); }
            else if (!was) { showAt = -1; Pose(1); }
        }

        public void Bind(DoorCardData d) => Bind(d, true);

        void Bind(DoorCardData d, bool animateChanges)
        {
            bool live = animateChanges && Application.isPlaying;
            float now = Time.unscaledTime;
            head.text = d.Head ?? "";
            code.text = d.Code ?? "";
            title.text = d.Title ?? "";
            Line(choice, d.Choice);
            Line(body, d.Body);

            for (int i = 0; i < observations.Length; i++)
            {
                var o = observations[i];
                var od = i < d.Observations.Count ? d.Observations[i] : null;
                o.root.SetActive(od != null);
                if (od == null) { obsShown[i] = null; continue; }
                o.icon.sprite = od.Intercom ? intercomIcon : eyeIcon;
                o.label.text = od.Label;
                o.text.text = od.Text;
                if (obsShown[i] != od.Text)
                {
                    // Yeni blok: gözetleme 0,25 s'de dolar; interkom cızırtıdan sonra daktiloyla 0,03 s/harf
                    bool type = live && obsShown[i] == null;
                    obsShown[i] = od.Text;
                    typeAt[i] = type ? now + (od.Intercom ? IntercomHissS : 0) : -1;
                    typeSeconds[i] = od.Intercom ? Mathf.Max(0.05f, od.Text.Length * IntercomCharS) : PeekTypeS;
                    o.text.maxVisibleCharacters = type ? 0 : 99999;
                }
            }

            fatal = d.Fatal;
            // CSS rotate(−0,8deg) saat yönünün tersi: uGUI'de +z
            angle = fatal ? 1.4f : 0.8f;
            stampText.text = d.Stamp ?? "";
            epilogue.text = d.Epilogue ?? "";
            bool landed = !fatal || fatalAt >= 0;
            stampBlock.SetActive(fatal && landed && !string.IsNullOrEmpty(d.Stamp));
            epilogue.gameObject.SetActive(fatal && landed && !string.IsNullOrEmpty(d.Epilogue));

            optionBlock.SetActive(d.Buttons.Count > 0);
            if (data.Length != buttons.Length) { data = new DoorButtonData[buttons.Length]; usedAt = new float[buttons.Length]; }
            for (int i = 0; i < buttons.Length; i++)
            {
                var slot = buttons[i];
                var b = i < d.Buttons.Count ? d.Buttons[i] : null;
                slot.root.SetActive(b != null);
                if (i < ids.Length) ids[i] = b?.Id;
                // Kullanılan seçeneğin damgası 0,1 s'de iner
                bool newlyUsed = b?.Used != null && (data[i] == null || data[i].Id != b.Id || data[i].Used == null);
                if (b?.Used == null) usedAt[i] = -1;
                else if (newlyUsed) usedAt[i] = live ? now : -1;
                data[i] = b;
                if (b == null) continue;
                slot.key.text = (i + 1).ToString();
                slot.label.text = b.Label;
                slot.used.gameObject.SetActive(b.Used != null);
                slot.usedText.text = b.Used ?? "";
                bool detail = b.Used == null && !string.IsNullOrEmpty(b.Detail);
                slot.detail.gameObject.SetActive(detail);
                slot.detail.text = detail ? b.Detail : "";
                slot.detail.fontSize = b.State == SlipButtonState.Disabled ? 15 : 16;
                slot.button.interactable = b.Enabled;
                Paint(slot, b);
            }

            actionId = d.ActionId;
            actionLabel.text = d.Action ?? "";
            actionBlock.SetActive(d.Action != null && (!fatal || fatalAt >= 0 && now - fatalAt >= ActionDelayS));
            hintRow.SetActive(!string.IsNullOrEmpty(d.Hint));
            hintKey.text = d.HintKey ?? "";
            hint.text = d.Hint ?? "";
            Layout();
        }

        static void Line(TMP_Text text, string value)
        {
            text.gameObject.SetActive(!string.IsNullOrEmpty(value));
            text.text = value ?? "";
        }

        /// <summary>Satır yükseklikleri (yazı sarınca 50 px'ten uzar), sonra kâğıdın düzeni ve gölgesi.</summary>
        void Layout()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var slot = buttons[i];
                if (!slot.root.activeSelf) continue;
                float detailW = 0, detailH = 0;
                if (slot.detail.gameObject.activeSelf)
                {
                    detailW = Mathf.Min(DetailMaxW, slot.detail.GetPreferredValues(slot.detail.text).x + 1);
                    detailH = slot.detail.GetPreferredValues(slot.detail.text, detailW, 0).y;
                }
                else if (slot.used.gameObject.activeSelf) detailW = slot.usedText.GetPreferredValues(slot.usedText.text).x + 14;
                var detailLayout = slot.detail.GetComponent<LayoutElement>();
                if (detailLayout != null) detailLayout.preferredWidth = slot.detail.gameObject.activeSelf ? detailW : -1;
                // Satır: sol 10 + fiş 24 + 10 + yazı + (10 + sağ sütun) + sağ 12
                float labelW = paper.rect.width - 38 - 22 - 34 - (detailW > 0 ? detailW + 10 : 0);
                float labelH = slot.label.GetPreferredValues(slot.label.text, Mathf.Max(40, labelW), 0).y;
                slot.layout.preferredHeight = slot.layout.minHeight = Mathf.Max(50, Mathf.Max(labelH, detailH) + 10);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(paper);
            shadow.sizeDelta = new Vector2(shadow.sizeDelta.x, paper.rect.height);
        }

        /// <summary>
        /// Satırın hâli (S4 › etkileşim): normal kâğıt beyazı, gölge 2 px; üzerinde mürekkep zemin, 2 px kalkar, fiş pirinç;
        /// basılı gölgesiz 1 px iner; devre dışı ve kullanılmış kesik kenarlı, gri.
        /// </summary>
        void Paint(OptionSlot s, DoorButtonData b)
        {
            bool off = !b.Enabled;
            bool pressed = !off && s.hover.Pressed, over = !off && (s.hover.Over || pressed);
            s.fill.color = C(off ? "doorPaper.optionDisabled" : over ? "ui.inkButton" : "ui.frameButton");
            s.edge.sprite = off ? frameDashed : frameSolid;
            s.edge.type = off ? Image.Type.Tiled : Image.Type.Sliced;
            s.edge.color = C(off ? "doorPaper.blockEdge" : over ? "ui.inkButton" : "doorPaper.optionEdge");
            s.shadow.enabled = !off && !pressed;
            s.shadow.color = C(over ? "ink2" : "doorPaper.edge");
            s.label.color = C(off ? "doorPaper.optionDisabledText" : over ? "ui.inkButtonText" : "ink");
            s.keyFill.enabled = over;
            s.keyFill.color = C("slip.buttonKey");
            s.keyBox.enabled = !over;
            s.keyBox.color = C(off ? "slip.buttonDisabled" : "ink2");
            s.key.color = C(off ? "doorPaper.optionDisabledText" : "ink");
            s.detail.color = C(off ? "doorPaper.optionDisabledDetail" : over ? "slip.buttonKey" : "ink2");
            s.usedFrame.color = C("slip.buttonDisabled");
            s.usedText.color = C("doorPaper.optionDisabledDetail");
            float lift = pressed ? -1 : over ? 2 : 0;
            s.face.anchoredPosition = new Vector2(0, lift);
            s.shadow.rectTransform.anchoredPosition = new Vector2(0, over ? -1 : -2);
        }

        /// <summary>Kısayol (1–n): satır açık ve seçilebilirse ona basar.</summary>
        public bool PressKey(int index)
        {
            if (index < 0 || index >= buttons.Length || index >= ids.Length || ids[index] == null || !buttons[index].button.interactable) return false;
            Press(index);
            return true;
        }

        /// <summary>Enter: geniş düğme görünürse ona basar.</summary>
        public bool PressAction()
        {
            if (!ActionReady || Animating) return false;
            Clicked?.Invoke(actionId);
            return true;
        }

        void Press(int index)
        {
            if (Animating || index >= ids.Length || ids[index] == null) return;
            Clicked?.Invoke(ids[index]);
        }

        /// <summary>Yapılamadı (yetersiz kaynak): kâğıt 0,1 s titrer.</summary>
        public void Shake() => shakeAt = Time.unscaledTime;

        /// <summary>Ölümcül an (atış): kâğıt sarsılır, damga 0,12 s'de iner, son cümle görünür; düğme 0,6 s sonra.</summary>
        public void PlayFatal()
        {
            if (!fatal || fatalAt >= 0) return;
            fatalAt = Time.unscaledTime;
            shakeAt = fatalAt;
            stampBlock.SetActive(!string.IsNullOrEmpty(stampText.text));
            epilogue.gameObject.SetActive(!string.IsNullOrEmpty(epilogue.text));
            stamp.localScale = Vector3.one * 1.6f;
            Layout();
        }

        /// <summary>Kâğıdı kapatır; <paramref name="animate"/> sola 0,18 s'de çekilir.</summary>
        public void Hide(bool animate = false)
        {
            showAt = -1;
            if (animate && Application.isPlaying && gameObject.activeSelf)
            {
                hideAt = Time.unscaledTime;
                return;
            }
            hideAt = -1;
            IntercomTyping = false;
            gameObject.SetActive(false);
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (hideAt >= 0)
            {
                float h = Mathf.Clamp01((now - hideAt) / HideS);
                Pose(1 - h);
                if (h >= 1) { hideAt = -1; IntercomTyping = false; gameObject.SetActive(false); }
                return;
            }
            if (showAt >= 0)
            {
                float p = Mathf.Clamp01((now - showAt) / ShowS);
                Pose(p);
                if (p >= 1) showAt = -1;
            }
            else Pose(1);

            bool typing = false;
            for (int i = 0; i < observations.Length && i < typeAt.Length; i++)
            {
                if (typeAt[i] < 0 || !observations[i].root.activeSelf) continue;
                var text = observations[i].text;
                bool intercom = observations[i].icon.sprite == intercomIcon;
                if (intercom) typing = true;
                if (now < typeAt[i]) { text.maxVisibleCharacters = 0; continue; }
                float p = Mathf.Clamp01((now - typeAt[i]) / typeSeconds[i]);
                text.maxVisibleCharacters = p >= 1 ? 99999 : Mathf.CeilToInt(p * text.text.Length);
                if (p >= 1) typeAt[i] = -1;
            }
            IntercomTyping = typing;

            for (int i = 0; i < buttons.Length && i < data.Length; i++)
            {
                if (data[i] == null || !buttons[i].root.activeSelf) continue;
                Paint(buttons[i], data[i]);
                if (usedAt[i] < 0) continue;
                float p = Mathf.Clamp01((now - usedAt[i]) / UsedS);
                buttons[i].used.localScale = Vector3.one * Mathf.Lerp(1.4f, 1, p);
                if (p >= 1) usedAt[i] = -1;
            }

            if (fatalAt >= 0)
            {
                float p = Mathf.Clamp01((now - fatalAt) / StampS);
                stamp.localScale = Vector3.one * Mathf.Lerp(1.6f, 1, 1 - (1 - p) * (1 - p));
                if (!actionBlock.activeSelf && actionId != null && now - fatalAt >= ActionDelayS) { actionBlock.SetActive(true); Layout(); }
            }
            ActionButton();
        }

        /// <summary>Geniş mürekkep düğme: üzerinde 2 px kalkar (gölge 4), basılı 1 px iner (gölge 1).</summary>
        void ActionButton()
        {
            if (!actionBlock.activeSelf) return;
            bool pressed = actionHover.Pressed, over = actionHover.Over && !pressed;
            float lift = pressed ? -1 : over ? 2 : 0, drop = pressed ? 1 : over ? 4 : 3;
            actionFace.anchoredPosition = new Vector2(0, lift);
            actionShadow.rectTransform.anchoredPosition = new Vector2(0, lift - drop);
        }

        /// <summary>Soldan kayarak gelir (0 → 1); titreme ±4 px. Dönüş üst ortadan.</summary>
        void Pose(float p)
        {
            float e = 1 - (1 - p) * (1 - p), x = 0;
            if (shakeAt >= 0)
            {
                float s = Mathf.Clamp01((Time.unscaledTime - shakeAt) / ShakeS);
                x = Mathf.Sin(s * Mathf.PI * 4) * 4 * (1 - s);
                if (s >= 1) shakeAt = -1;
            }
            sheet.anchoredPosition = rest + new Vector2(-90 * (1 - e) + x, 0);
            sheet.localRotation = Quaternion.Euler(0, 0, angle + 1.5f * (1 - e));
            group.alpha = Mathf.Clamp01(p * 2.5f);
        }
    }
}
