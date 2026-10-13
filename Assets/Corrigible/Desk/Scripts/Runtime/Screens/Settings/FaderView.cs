using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ses kaydırıcısı (F8.3, taslak <c>drafts/ayarlar</c>): cetvel çizgisi, değere kadar taranmış dolgu, çeyrek çentikler ve
    /// pirinç düğme. Çubuğun herhangi bir yerine basıp sürüklemek değeri değiştirir; ←/→ yerine fare ile kullanılır.
    /// </summary>
    public sealed class FaderView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform track;
        [SerializeField] Image fill;
        [SerializeField] RectTransform knob;
        [Range(0, 1)] [SerializeField] float value = 0.8f;

        public float Value => value;
        /// <summary>Değer değişti (sürüklerken her adımda).</summary>
        public event Action<float> Changed;
        /// <summary>Bırakıldı (kayıt için).</summary>
        public event Action Released;

        public void Set(float v)
        {
            value = Mathf.Clamp01(v);
            Apply();
        }

        void Apply()
        {
            float w = track.rect.width;
            if (fill != null) { var f = fill.rectTransform; f.sizeDelta = new Vector2(Mathf.Round(w * value), f.sizeDelta.y); }
            if (knob != null) knob.anchoredPosition = new Vector2(Mathf.Round(w * value), knob.anchoredPosition.y);
        }

        void OnEnable() => Apply();
        void OnValidate() { if (track != null) Apply(); }

        void FromPointer(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out var local)) return;
            float v = Mathf.Clamp01((local.x - track.rect.xMin) / Mathf.Max(1, track.rect.width));
            // %5 adım: değer yazısı ve kayıt kararlı
            v = Mathf.Round(v * 20) / 20;
            if (Mathf.Approximately(v, value)) return;
            value = v;
            Apply();
            Changed?.Invoke(value);
        }

        public void OnPointerDown(PointerEventData e) => FromPointer(e);
        public void OnDrag(PointerEventData e) => FromPointer(e);
        public void OnPointerUp(PointerEventData e) => Released?.Invoke();
    }
}
