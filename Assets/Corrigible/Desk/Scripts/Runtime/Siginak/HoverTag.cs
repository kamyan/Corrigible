using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üzerine gelme etiketi (K1 notu › ekran katmanı): 9 dilim kâğıt, 1,5 px mürekkep, sert gölge; başlık CP 700 18, satır
    /// LF 400 15 (klavye kısayolu yazılmaz). 400 ms gecikmeyle imlecin sol üstünde açılır, ekrandan taşarsa yansır.
    /// </summary>
    public sealed class HoverTag : MonoBehaviour
    {
        [SerializeField] RectTransform paper;
        [SerializeField] TMP_Text title, line;
        [SerializeField] Canvas canvas;

        IHoverSource owner;
        float since;
        string shownLine;

        public void Configure(RectTransform paperRoot, TMP_Text titleText, TMP_Text lineText, Canvas rootCanvas)
        {
            paper = paperRoot; title = titleText; line = lineText; canvas = rootCanvas;
        }

        public void Show(IHoverSource target)
        {
            owner = target;
            since = Time.unscaledTime;
        }

        public void Hide(IHoverSource target)
        {
            if (owner != target) return;
            owner = null;
            paper.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (owner == null) return;
            if (Time.unscaledTime - since < SiginakTheme.Active.NumberOf("motion.tooltipDelayS", 0.4f)) return;
            if (!paper.gameObject.activeSelf)
            {
                title.text = owner.HoverTitle;
                SetLine(owner.Line);
                paper.gameObject.SetActive(true);
            }
            else if (owner.Line != shownLine) SetLine(owner.Line);   // değeri değişen satır (dolabın sürgüsü)
            Place();
        }

        void SetLine(string text)
        {
            shownLine = text;
            line.gameObject.SetActive(!string.IsNullOrEmpty(text));
            line.text = text ?? "";
        }

        void Place()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var area = (RectTransform)canvas.transform;
            float scale = canvas.scaleFactor;
            Vector2 cursor = mouse.position.ReadValue() / scale;
            var size = paper.rect.size;
            // Varsayılan: imlecin sol üstü; sığmazsa yatayda/dikeyde yansır
            float x = cursor.x - 12 - size.x, y = cursor.y + 12;
            if (x < 8) x = cursor.x + 18;
            if (y + size.y > area.rect.height - 8) y = cursor.y - 18 - size.y;
            paper.anchorMin = paper.anchorMax = Vector2.zero;
            paper.pivot = Vector2.zero;
            paper.anchoredPosition = new Vector2(x, y);
        }
    }
}
