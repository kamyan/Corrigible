using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Açılır kartların ortak kabı (PROMPT §4.5): kâğıt kart + başlık + başlık çizgisi + kapat. Açılış 120 ms (8 px yukarıdan +
    /// opaklık), kapanış 90 ms (tokens_sade.motion). Yerleşim <see cref="PopupPlacement"/> ile hesaplanır; kart kendi sol-üst
    /// köşesinden konumlanır (çapa sol-üst, pivot sol-üst). Kartın içeriğini alt sınıflar bağlar.
    /// </summary>
    public abstract class PopupCardBase : MonoBehaviour
    {
        [Tooltip("Hareket eden kök (yoksa bu nesne).")]
        [SerializeField] protected RectTransform card;
        [SerializeField] protected CanvasGroup group;
        [SerializeField] protected TMP_Text title;
        [SerializeField] protected DeskButton close;
        [SerializeField] float openingSeconds = -1;
        [SerializeField] float openingOffsetY = 8;

        /// <summary>Kapat düğmesi ya da Esc: kartı sahibi kapatır (veri tarafı seçimi kaldırır).</summary>
        public event Action CloseRequested;

        DeskTween show;
        Vector2 rest;
        bool closing;
        DeskTween moveX, moveY;
        Vector2 destination;

        public bool IsOpen { get; private set; }
        public bool IsAnimating => show.Running || closing || moveX.Running || moveY.Running;
        protected RectTransform Card => card != null ? card : (RectTransform)transform;

        protected virtual void Awake()
        {
            if (close != null) close.onClick.AddListener(RequestClose);
        }

        public void RequestClose() => CloseRequested?.Invoke();

        /// <summary>Kartı sol-üst köşesi <paramref name="topLeft"/> (tasarım px, y aşağı) olacak şekilde açar.</summary>
        public void Open(Vector2 topLeft, bool animate = true)
        {
            rest = new Vector2(topLeft.x, -topLeft.y);
            destination = rest;
            moveX.Snap(rest.x); moveY.Snap(rest.y);
            closing = false;
            IsOpen = true;
            DeskUi.SetActive(this, true);
            var th = SadeTheme.Active;
            if (animate && Application.isPlaying)
            {
                show.To(1, openingSeconds >= 0 ? openingSeconds : th != null ? th.Number("motion.popOpenS", 0.12f) : 0.12f);
            }
            else if (!animate || !Application.isPlaying) show.Snap(1);
            ApplyMotion();
        }

        /// <summary>Açıkken yer değiştirir (başka bölge seçildi): kart kayar.</summary>
        public void MoveTo(Vector2 topLeft)
        {
            var next = new Vector2(topLeft.x, -topLeft.y);
            if (destination == next) return;
            destination = next;
            if (Application.isPlaying && IsOpen)
            {
                moveX.To(next.x, .12f); moveY.To(next.y, .12f);
            }
            else { rest = next; moveX.Snap(next.x); moveY.Snap(next.y); }
            ApplyMotion();
        }

        public void CompleteAnimation()
        {
            rest = destination; moveX.Snap(rest.x); moveY.Snap(rest.y);
            show.Snap(IsOpen ? 1 : 0); closing = false;
            ApplyMotion();
            if (!IsOpen) DeskUi.SetActive(this, false);
        }

        public void Close(bool animate = true)
        {
            if (!IsOpen) return;
            IsOpen = false;
            var th = SadeTheme.Active;
            if (animate && Application.isPlaying && gameObject.activeInHierarchy)
            {
                closing = true;
                show.To(0, th != null ? th.Number("motion.popCloseS", 0.09f) : 0.09f);
            }
            else { show.Snap(0); DeskUi.SetActive(this, false); }
        }

        protected virtual void Update()
        {
            bool changed = show.Tick(Time.unscaledDeltaTime);
            bool moved = moveX.Tick(Time.unscaledDeltaTime);
            moved |= moveY.Tick(Time.unscaledDeltaTime);
            if (moved) rest = new Vector2(moveX.Value, moveY.Value);
            if (!changed && !moved) return;
            ApplyMotion();
            if (closing && !show.Running && show.Value <= 0) { closing = false; DeskUi.SetActive(this, false); }
        }

        void ApplyMotion()
        {
            var rt = Card;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            // Açılırken 8 px yukarıdan iner
            var p = rest + new Vector2(0, openingOffsetY * (1 - show.Value));
            if (rt.anchoredPosition != p) rt.anchoredPosition = p;
            if (group != null && !Mathf.Approximately(group.alpha, show.Value)) group.alpha = show.Value;
        }
    }

    /// <summary>
    /// Açılır kartın yeri (PROMPT §A.11): çapanın (düğüm) sağı, sığmazsa solu; dikeyde güvenli alana sıkıştırılır. Kaçınılacak
    /// dikdörtgenleri (düğüm, etiket, açık çekmece) örterse önce dikeyde kaydırır, olmazsa öteki yana geçer; iki yana da sığmazsa
    /// sağ sütuna sabitlenir (açık karar 3). Tüm değerler tasarım px'i, sol-üst orijin, y aşağı.
    /// </summary>
    public static class PopupPlacement
    {
        public static Vector2 BesideAnchor(Vector2 anchor, Vector2 size, float gapX, float raiseY, Rect safe, IList<Rect> avoid,
            float fallbackX)
        {
            float w = size.x, h = size.y;
            foreach (bool right in new[] { true, false })
            {
                float x = right ? anchor.x + gapX : anchor.x - gapX - w;
                if (x < safe.xMin || x + w > safe.xMax) continue;
                float y = Mathf.Clamp(anchor.y - raiseY, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - h));
                if (Fits(x, ref y, w, h, safe, avoid)) return new Vector2(x, y);
            }
            float fy = Mathf.Clamp(anchor.y - raiseY, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - h));
            return new Vector2(fallbackX, fy);
        }

        /// <summary>Örtüşme varsa kartı engelin altına ya da üstüne kaydırmayı dener.</summary>
        static bool Fits(float x, ref float y, float w, float h, Rect safe, IList<Rect> avoid)
        {
            if (avoid == null || avoid.Count == 0) return true;
            float y0 = y;
            if (Free(x, y0, w, h, avoid)) return true;
            foreach (var a in avoid)
            {
                foreach (var cand in new[] { a.yMax + 8, a.yMin - 8 - h })
                {
                    if (cand < safe.yMin || cand + h > safe.yMax) continue;
                    if (Free(x, cand, w, h, avoid)) { y = cand; return true; }
                }
            }
            return false;
        }

        static bool Free(float x, float y, float w, float h, IList<Rect> avoid)
        {
            var r = new Rect(x, y, w, h);
            foreach (var a in avoid) if (r.Overlaps(a)) return false;
            return true;
        }
    }
}
