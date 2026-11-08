using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Dünya çapalı ekran etiketi (Revizyon 2 · S6): can etiketi (tür işareti 18 px → zırh → 11 px pipler; kaybedilen pip
    /// kırmızı kenarlı boş), sıra bayrağı (30 × 28 kâğıt, CP 700 20 rakam) ya da kısa yazı. Görsel hiyerarşi prefab'dadır.
    /// </summary>
    public sealed class RaidScreenLabel : MonoBehaviour
    {
        [SerializeField] RectTransform root;
        [SerializeField] Image paper, target, armor;
        [SerializeField] Outline border;
        [SerializeField] TMP_Text caption;
        [SerializeField] Image[] pips;
        [SerializeField] Sprite fullPip, emptyPip;
        [SerializeField] float pipSize = 11, gap = 3, healthHeight = 24, padding = 5;
        [SerializeField] float nameFontPixels = 14;
        [Tooltip("Tür işaretinin ve zırh simgesinin boyu (px); işaretle pipler arası.")]
        [SerializeField] float glyphSize = 18, glyphGap = 4;
        [Tooltip("Sıra bayrağının kâğıdı (px) ve rakamı.")]
        [SerializeField] Vector2 flagSize = new Vector2(30, 28);
        [SerializeField] float flagFontPixels = 20;
        [Tooltip("Etiketteki zırh / plaka kalkanının rengi (labels.hpTag.armorIcon.color).")]
        [SerializeField] Color edgeArmor = new Color32(0x46, 0x3F, 0x34, 0xFF);

        Camera view;
        Canvas canvas;
        Vector3 position, lastPosition;
        Matrix4x4 lastCamera;
        float lastScale, opacity = 1;
        float requestedFontPixels = 12, minimumFontPixels = 11;
        Vector2 lastPixels;
        Color ink, paperColor, edge, loss;
        Sprite targetSprite;
        bool dirty = true, flag, iconMode;
        string healthName;
        float stampAt = -1, stampSeconds;

        public bool IsHealth { get; private set; }
        public int Health { get; private set; }
        public int Maximum { get; private set; }
        public bool HasTarget => target.gameObject.activeSelf;
        public bool HasArmor => armor.gameObject.activeSelf;
        public Rect ScreenBounds
        {
            get
            {
                Refresh(position);
                var center = view.WorldToScreenPoint(position);
                return new Rect((Vector2)center - root.rect.size * canvas.scaleFactor * .5f, root.rect.size * canvas.scaleFactor);
            }
        }

        public void Configure(Camera camera, Canvas layer, TMP_FontAsset font, float minimumPixels = 11)
        {
            view = camera; canvas = layer; caption.font = font; minimumFontPixels = minimumPixels; dirty = true;
        }

        public void SetPaperColor(Color color) { paperColor = color; dirty = true; }
        public void SetOpacity(float value) { opacity = value; dirty = true; }

        public void ShowText(string value, Color textInk, Color edgeInk, float pixels)
        {
            IsHealth = false; flag = false; iconMode = false; ink = textInk; edge = edgeInk;
            caption.gameObject.SetActive(true); target.gameObject.SetActive(false); armor.gameObject.SetActive(false);
            caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = caption.rectTransform.pivot = new Vector2(.5f,.5f);
            caption.rectTransform.anchoredPosition = Vector2.zero;
            foreach (var pip in pips) pip.gameObject.SetActive(false);
            caption.text = value;
            requestedFontPixels = pixels;
            caption.fontSize = Mathf.Max(pixels, minimumFontPixels / Mathf.Max(.01f, canvas.scaleFactor));
            root.sizeDelta = caption.GetPreferredValues(value) + new Vector2(10, 6);
            gameObject.SetActive(true); dirty = true; Refresh(position);
        }

        /// <summary>
        /// Sıra bayrağı (S6 › orderFlag): sabit 30 × 28 kâğıt, CP 700 20 rakam (çizik ya da kırmızı zemin durumları metin ve
        /// kâğıt rengiyle). İğneyi tahta çizer.
        /// </summary>
        public void ShowFlag(string value, Color textInk, Color edgeInk)
        {
            ShowText(value, textInk, edgeInk, flagFontPixels);
            flag = true;
            root.sizeDelta = flagSize;
            dirty = true; Refresh(position);
        }

        /// <summary>Yalnız simge (niyet rozeti): kâğıtsız, verilen boyda (px).</summary>
        public void ShowIcon(Sprite icon, float size, Color tint)
        {
            IsHealth = false; flag = false; ink = tint; edge = new Color(0, 0, 0, 0); paperColor = new Color(0, 0, 0, 0);
            caption.gameObject.SetActive(false); armor.gameObject.SetActive(false);
            foreach (var pip in pips) pip.gameObject.SetActive(false);
            if (targetSprite == null) targetSprite = target.sprite;
            target.gameObject.SetActive(true); target.sprite = icon; target.preserveAspect = true;
            target.rectTransform.sizeDelta = Vector2.one * size; target.rectTransform.anchoredPosition = Vector2.zero;
            root.sizeDelta = Vector2.one * size;
            iconMode = true;
            gameObject.SetActive(true); dirty = true; Refresh(position);
        }

        /// <param name="glyph">Makine türünün işareti (önce gelir); uzmanda null.</param>
        /// <param name="lossInk">Kaybedilen pipin kenarı (kırmızı); verilmezse <paramref name="color"/>.</param>
        public void ShowHealth(int hp, int max, Color color, bool isTarget, bool isArmored, Sprite glyph = null, Color? lossInk = null, string personName = null)
        {
            // Kenar her etikette nötr (#463F34); renk piplerde ve işarette
            iconMode = false;
            IsHealth = true; flag = false; Health = Mathf.Clamp(hp, 0, max); Maximum = max; ink = color; edge = edgeArmor; loss = lossInk ?? color;
            if (targetSprite == null) targetSprite = target.sprite;
            healthName = personName;
            caption.gameObject.SetActive(!string.IsNullOrEmpty(healthName));
            target.gameObject.SetActive(isTarget || glyph != null); armor.gameObject.SetActive(isArmored);
            target.sprite = glyph != null ? glyph : targetSprite;
            target.preserveAspect = true;
            target.rectTransform.sizeDelta = Vector2.one * glyphSize;
            armor.rectTransform.sizeDelta = new Vector2(pipSize, pipSize * 1.14f);
            ArrangeHealth();
            gameObject.SetActive(true); dirty = true; Refresh(position);
        }

        void ArrangeHealth()
        {
            float x = padding;
            if (!string.IsNullOrEmpty(healthName))
            {
                caption.fontSize = Mathf.Max(nameFontPixels,14/Mathf.Max(.01f,canvas.scaleFactor));
                caption.text = healthName;
                caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = caption.rectTransform.pivot = new Vector2(0,.5f);
                caption.rectTransform.anchoredPosition = new Vector2(x,0);
                float width = caption.GetPreferredValues(healthName).x;
                caption.rectTransform.sizeDelta = new Vector2(width,healthHeight);
                x += width + glyphGap;
            }
            void Icon(Image image, float after)
            {
                if (!image.gameObject.activeSelf) return;
                image.rectTransform.anchoredPosition = new Vector2(x, 0);
                x += image.rectTransform.sizeDelta.x + after;
            }
            // Sıra: tür işareti → zırh → pipler; uzmanda (işaretsiz) plaka piplerden sonra
            bool plateAfter = !target.gameObject.activeSelf;
            Icon(target, glyphGap);
            if (!plateAfter) Icon(armor, glyphGap);
            for (int i = 0; i < pips.Length; i++)
            {
                var pip = pips[i]; pip.gameObject.SetActive(i < Maximum);
                if (i >= Maximum) continue;
                pip.sprite = i < Health ? fullPip : emptyPip;
                pip.rectTransform.sizeDelta = Vector2.one * pipSize;
                pip.rectTransform.anchoredPosition = new Vector2(x, 0); x += pipSize + gap;
            }
            if (plateAfter && armor.gameObject.activeSelf) { x += glyphGap - gap; Icon(armor, gap); }
            root.sizeDelta = new Vector2(x - gap + padding, healthHeight);
        }

        /// <summary>Damga gibi iner: verilen sürede 1,6 kattan bire (UYANDI, Revizyon 2 · S6).</summary>
        public void Stamp(float seconds) { stampAt = Time.unscaledTime; stampSeconds = Mathf.Max(0.01f, seconds); root.localScale = Vector3.one * 1.6f; }

        void Update()
        {
            if (stampAt < 0) return;
            float t = Mathf.Clamp01((Time.unscaledTime - stampAt) / stampSeconds);
            root.localScale = Vector3.one * Mathf.Lerp(1.6f, 1, t * t);
            if (t >= 1) stampAt = -1;
        }

        public void Refresh(Vector3 worldPosition)
        {
            position = worldPosition;
            if (view == null || canvas == null) return;
            var pixels = new Vector2(view.pixelWidth, view.pixelHeight);
            var matrix = view.projectionMatrix * view.worldToCameraMatrix;
            if (!dirty && lastPosition == position && lastCamera == matrix && lastScale == canvas.scaleFactor && lastPixels == pixels) return;
            lastPosition = position; lastCamera = matrix; lastScale = canvas.scaleFactor; lastPixels = pixels; dirty = false;
            if (!IsHealth && !iconMode)
            {
                caption.fontSize = Mathf.Max(requestedFontPixels, minimumFontPixels / Mathf.Max(.01f, canvas.scaleFactor));
                root.sizeDelta = flag ? flagSize : caption.GetPreferredValues(caption.text) + new Vector2(10, 6);
                caption.rectTransform.sizeDelta=root.sizeDelta-new Vector2(8,4);
            }
            else if (IsHealth && !string.IsNullOrEmpty(healthName)) ArrangeHealth();
            Vector3 screen = view.WorldToScreenPoint(position);
            var layer = (RectTransform)canvas.transform;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, uiCamera, out var point)) root.anchoredPosition = point;
            Color Fade(Color c) => new Color(c.r, c.g, c.b, c.a * opacity);
            paper.color = Fade(paperColor); border.effectColor = Fade(edge); caption.color = Fade(ink);
            target.color = Fade(ink);
            armor.color = Fade(IsHealth ? edgeArmor : ink);
            for (int i = 0; i < pips.Length; i++) pips[i].color = Fade(!IsHealth || i < Health ? ink : loss);
        }
    }
}
