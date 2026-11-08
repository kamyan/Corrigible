using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üzerine gelme etiketi (K1 notu › ekran katmanı): 9 dilim kâğıt, 1,5 px mürekkep, sert gölge; başlık CP 700 18, satır
    /// LF 400 15 (klavye kısayolu yazılmaz). 400 ms gecikmeyle imlecin sol üstünde açılır, ekrandan taşarsa yansır.
    /// Etiket açıkken sahibi değişirse ya da sahibinin başlığı/satırı değişirse metin hemen yenilenir (bırakma önizlemesi bir
    /// hedeften ötekine geçer). <see cref="ShowAt(IHoverSource, Vector2)"/> etiketi imleç yerine verilen ekran noktasına koyar
    /// (klavye yolu). Sahibi <see cref="IHoverFrame"/> ise isteğe bağlı çerçeve onun rengini alır, değilse gizlenir.
    /// </summary>
    public sealed class HoverTag : MonoBehaviour
    {
        [SerializeField] RectTransform paper;
        [SerializeField] TMP_Text title, line;
        [SerializeField] Canvas canvas;
        [Tooltip("İsteğe bağlı ≈ 2 px çerçeve (kâğıdın çocuğu): sahibi IHoverFrame ise rengini alır, değilse gizli.")]
        [SerializeField] Image frame;

        IHoverSource owner, shownOwner;
        float since;
        string shownTitle, shownLine;
        bool anchored, immediate;
        Vector2 anchor;

        /// <summary>Etiketi şu an isteyen kaynak (yoksa null).</summary>
        public IHoverSource Owner => owner;
        /// <summary>Kâğıt ekranda mı.</summary>
        public bool Visible => paper != null && paper.gameObject.activeSelf;

        public void Configure(RectTransform paperRoot, TMP_Text titleText, TMP_Text lineText, Canvas rootCanvas)
        {
            paper = paperRoot; title = titleText; line = lineText; canvas = rootCanvas;
        }

        /// <summary>Çerçeveyi bağlar (tek seferlik sahne script'i; null çerçevesiz etiket).</summary>
        public void ConfigureFrame(Image frameImage) => frame = frameImage;

        /// <summary>Etiketi imlecin yanında, gecikmeyle açar.</summary>
        public void Show(IHoverSource target) => Open(target, false, default, false);

        /// <summary>Etiketi <paramref name="screen"/> ekran noktasının (piksel) yanında, gecikmeyle açar (klavye yolu).</summary>
        public void ShowAt(IHoverSource target, Vector2 screen) => Open(target, true, screen, false);

        /// <summary>
        /// Etiketi ekran noktasının yanında açar; <paramref name="now"/> ise gecikme beklenmez (ör. reddedilen bırakmanın
        /// gerekçesi bırakılan yerde hemen görünür).
        /// </summary>
        public void ShowAt(IHoverSource target, Vector2 screen, bool now) => Open(target, true, screen, now);

        /// <summary>
        /// Açık ya da gecikmesini bekleyen etiketin ekran noktasını gecikmeyi yeniden başlatmadan günceller (sürüklenen kartın
        /// bırakma önizlemesi imleçle birlikte gider). Etiketin sahibi <paramref name="target"/> değilse hiçbir şey yapmaz.
        /// </summary>
        public void SetAnchor(IHoverSource target, Vector2 screen)
        {
            if (owner != target || target == null) return;
            anchored = true;
            anchor = screen;
        }

        public void Hide(IHoverSource target)
        {
            if (owner != target) return;
            owner = null;
            shownOwner = null;
            anchored = immediate = false;
            if (paper != null) paper.gameObject.SetActive(false);
        }

        void Open(IHoverSource target, bool atPoint, Vector2 screen, bool now)
        {
            owner = target;
            since = Time.unscaledTime;
            anchored = atPoint;
            anchor = screen;
            immediate = now;
        }

        static float Delay
        {
            get
            {
                var t = SiginakTheme.Active;
                return t != null ? t.NumberOf("motion.tooltipDelayS", 0.4f) : 0.4f;
            }
        }

        void LateUpdate()
        {
            if (owner == null || paper == null) return;
            bool visible = paper.gameObject.activeSelf;
            if (!visible && !immediate && Time.unscaledTime - since < Delay) return;
            if (!visible || owner != shownOwner) Fill();
            else
            {
                // Değeri değişen başlık ya da satır (dolabın sürgüsü, bırakma önizlemesinin hedefi)
                var t = owner.HoverTitle;
                if (t != shownTitle) SetTitle(t);
                var l = owner.Line;
                if (l != shownLine) SetLine(l);
                ApplyFrame();
            }
            if (!visible) paper.gameObject.SetActive(true);
            Place();
        }

        void Fill()
        {
            shownOwner = owner;
            SetTitle(owner.HoverTitle);
            SetLine(owner.Line);
            ApplyFrame();
        }

        void SetTitle(string text)
        {
            shownTitle = text;
            if (title != null) title.text = text ?? "";
        }

        void SetLine(string text)
        {
            shownLine = text;
            if (line == null) return;
            line.gameObject.SetActive(!string.IsNullOrEmpty(text));
            line.text = text ?? "";
        }

        void ApplyFrame()
        {
            if (frame == null) return;
            Color? tint = owner is IHoverFrame f ? f.Frame : (Color?)null;
            bool on = tint.HasValue;
            if (frame.enabled != on) frame.enabled = on;
            if (on && frame.color != tint.Value) frame.color = tint.Value;
        }

        void Place()
        {
            Vector2 point;
            if (anchored) point = anchor;
            else
            {
                var mouse = Mouse.current;
                if (mouse == null) return;
                point = mouse.position.ReadValue();
            }
            if (canvas == null) return;
            var area = (RectTransform)canvas.transform;
            float scale = canvas.scaleFactor;
            Vector2 cursor = point / scale;
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
