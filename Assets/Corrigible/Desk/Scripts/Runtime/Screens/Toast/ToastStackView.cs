using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Uyarı fişleri (F8.3, taslak <c>drafts/uyari</c>): haritanın üst kenarından sarkan, teleksten yırtılmış şeritler. Her fiş
    /// kimliğiyle bir kez gösterilir, <see cref="seconds"/> sonra solar; tıklanınca kapanır. En yeni üstte, en çok üç fiş.
    /// Overlay canvas'ında; masa etkileşimini engellemez (yalnızca fişin kendisi tıklanır).
    /// </summary>
    public sealed class ToastStackView : MonoBehaviour
    {
        [SerializeField] RectTransform stack;
        [SerializeField] ToastSlipView template;
        [SerializeField] float seconds = 3.6f, fade = 0.2f, gap = 48, maxVisible = 3;

        /// <summary>Fiş kapatıldı (tıklama ya da süre): kimlik.</summary>
        public event Action<int> Dismissed;

        readonly List<ToastSlipView> live = new List<ToastSlipView>();
        readonly List<ToastSlipView> free = new List<ToastSlipView>();
        int lastShown;

        void Awake() { if (template != null) template.gameObject.SetActive(false); }

        public void Bind(List<ToastVM> toasts)
        {
            if (toasts == null) return;
            // Yeni fişler (kimliği gösterilenlerden büyük): eskiden yeniye eklenir, en yenisi üstte durur
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var t = toasts[i];
                if (t.Id <= lastShown) continue;
                lastShown = t.Id;
                Spawn(t);
            }
            Layout();
        }

        /// <summary>Yeni oyun: gösterilen kimlikler sıfırlanır.</summary>
        public void ResetIds() { lastShown = 0; foreach (var s in live.ToArray()) Recycle(s); }

        void Spawn(ToastVM vm)
        {
            var s = free.Count > 0 ? free[free.Count - 1] : Instantiate(template, stack);
            DeskUi.MarkPreview(s.gameObject);
            if (free.Count > 0) free.RemoveAt(free.Count - 1);
            s.gameObject.SetActive(true);
            s.Bind(vm, Time.unscaledTime);
            s.Clicked -= OnClicked; s.Clicked += OnClicked;
            live.Insert(0, s);
            while (live.Count > maxVisible) { var old = live[live.Count - 1]; Recycle(old); }
        }

        void OnClicked(ToastSlipView s)
        {
            Dismissed?.Invoke(s.Id);
            Recycle(s);
            Layout();
        }

        void Recycle(ToastSlipView s)
        {
            live.Remove(s);
            s.gameObject.SetActive(false);
            if (!free.Contains(s)) free.Add(s);
        }

        void Layout()
        {
            for (int i = 0; i < live.Count; i++)
            {
                var rt = (RectTransform)live[i].transform;
                rt.anchoredPosition = new Vector2(0, -i * gap);
                // Hafif eğiklik: kâğıt şeridi (taslak: 0, −0,6°, +0,4°)
                rt.localEulerAngles = new Vector3(0, 0, i == 0 ? 0 : i == 1 ? 0.6f : -0.4f);
            }
        }

        void Update()
        {
            if (live.Count == 0) return;
            float now = Time.unscaledTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var s = live[i];
                float age = now - s.Born;
                float a = age < fade ? age / fade : age > seconds ? 1 - (age - seconds) / fade : 1;
                s.SetAlpha(Mathf.Clamp01(a));
                if (age > seconds + fade) { Dismissed?.Invoke(s.Id); Recycle(s); Layout(); }
            }
        }
    }
}
