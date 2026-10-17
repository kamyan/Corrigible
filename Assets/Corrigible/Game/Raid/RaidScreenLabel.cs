using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.Raid
{
    /// <summary>Dünya çapalı ekran etiketi: can noktaları veya sıra/olay yazısı. Görsel hiyerarşi prefab'dadır.</summary>
    public sealed class RaidScreenLabel : MonoBehaviour
    {
        [SerializeField] RectTransform root;
        [SerializeField] Image paper, target, armor;
        [SerializeField] Outline border;
        [SerializeField] TMP_Text caption;
        [SerializeField] Image[] pips;
        [SerializeField] Sprite fullPip, emptyPip;
        [SerializeField] float pipSize = 8, gap = 2, healthHeight = 15, padding = 3;

        Camera view;
        Canvas canvas;
        Vector3 position, lastPosition;
        Matrix4x4 lastCamera;
        float lastScale, opacity = 1;
        float requestedFontPixels = 12, minimumFontPixels = 11;
        Vector2 lastPixels;
        Color ink, paperColor, edge;
        bool dirty = true;

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
            IsHealth = false; ink = textInk; edge = edgeInk;
            caption.gameObject.SetActive(true); target.gameObject.SetActive(false); armor.gameObject.SetActive(false);
            foreach (var pip in pips) pip.gameObject.SetActive(false);
            caption.text = value;
            requestedFontPixels = pixels;
            caption.fontSize = Mathf.Max(pixels, minimumFontPixels / Mathf.Max(.01f, canvas.scaleFactor));
            root.sizeDelta = caption.GetPreferredValues(value) + new Vector2(10, 6);
            gameObject.SetActive(true); dirty = true; Refresh(position);
        }

        public void ShowHealth(int hp, int max, Color color, bool isTarget, bool isArmored)
        {
            IsHealth = true; Health = Mathf.Clamp(hp, 0, max); Maximum = max; ink = edge = color;
            caption.gameObject.SetActive(false); target.gameObject.SetActive(isTarget); armor.gameObject.SetActive(isArmored);
            float x = padding;
            void Icon(Image image)
            {
                if (!image.gameObject.activeSelf) return;
                image.rectTransform.anchoredPosition = new Vector2(x, 0);
                x += image.rectTransform.sizeDelta.x + gap;
            }
            Icon(target); Icon(armor);
            for (int i = 0; i < pips.Length; i++)
            {
                var pip = pips[i]; pip.gameObject.SetActive(i < max);
                if (i >= max) continue;
                pip.sprite = i < Health ? fullPip : emptyPip;
                pip.rectTransform.sizeDelta = Vector2.one * pipSize;
                pip.rectTransform.anchoredPosition = new Vector2(x, 0); x += pipSize + gap;
            }
            root.sizeDelta = new Vector2(x - gap + padding, healthHeight);
            gameObject.SetActive(true); dirty = true; Refresh(position);
        }

        public void Refresh(Vector3 worldPosition)
        {
            position = worldPosition;
            if (view == null || canvas == null) return;
            var pixels = new Vector2(view.pixelWidth, view.pixelHeight);
            var matrix = view.projectionMatrix * view.worldToCameraMatrix;
            if (!dirty && lastPosition == position && lastCamera == matrix && lastScale == canvas.scaleFactor && lastPixels == pixels) return;
            lastPosition = position; lastCamera = matrix; lastScale = canvas.scaleFactor; lastPixels = pixels; dirty = false;
            if (!IsHealth)
            {
                caption.fontSize = Mathf.Max(requestedFontPixels, minimumFontPixels / Mathf.Max(.01f, canvas.scaleFactor));
                root.sizeDelta = caption.GetPreferredValues(caption.text) + new Vector2(10, 6);
            }
            Vector3 screen = view.WorldToScreenPoint(position);
            var layer = (RectTransform)canvas.transform;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, uiCamera, out var point)) root.anchoredPosition = point;
            Color Fade(Color c) => new Color(c.r, c.g, c.b, c.a * opacity);
            paper.color = Fade(paperColor); border.effectColor = Fade(edge); caption.color = Fade(ink);
            target.color = armor.color = Fade(ink);
            foreach (var pip in pips) pip.color = Fade(ink);
        }
    }
}
