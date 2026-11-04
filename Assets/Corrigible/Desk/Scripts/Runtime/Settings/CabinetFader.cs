using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ayarlar dolabının kollu dikey sürgüsü (Docs/AYARLAR-DOLABI-VE-MENU.md Katman 2): yarıkta T başlı kol, yukarısı yüksek.
    /// Kol ya da yarığın herhangi bir yeri tutulup sürüklenir (imlecin ışını panel düzlemine düşürülür, yarığın eksenine
    /// izdüşürülür); yarığa tıklamak kolu oraya kaydırır. Değer %5 adımla; her %10'da çentik (<see cref="Detent"/>). Değer
    /// üç yoldan okunur: yarığın yanındaki ölçek, kolun altındaki sayaç tekerleri ve üzerine gelme etiketinin satırı. Üstteki
    /// ibre değeri yaylı bir gecikmeyle izler; lamba değer 0'dan büyükken ve ses açıkken yanar. Tekerlek ve (dolap odağındayken)
    /// ↑/↓ ±%5; çift tıklama varsayılana döndürür. Yalnız dolap açıkken çalışır (<see cref="DeskTarget.BlockedOverride"/>).
    /// </summary>
    public sealed class CabinetFader : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler, IPointerClickHandler
    {
        public const float Step = 0.05f;

        [SerializeField] DeskTarget target;
        [Tooltip("Kol (yerel y ekseninde kayar).")]
        [SerializeField] Transform handle;
        [Tooltip("Kolun yerel y aralığı: %0 ve %100.")]
        [SerializeField] float travelMin = -0.08f, travelMax = 0.08f;
        [SerializeField, Range(0, 1)] float defaultValue = 1;
        [Tooltip("İbre (yerel z ekseninde döner) ve açı aralığı: %0 ve %100.")]
        [SerializeField] Transform needle;
        [SerializeField] float needleMinDeg = 50, needleMaxDeg = -50;
        [Tooltip("Sayaç tekerleri: yüzler, onlar, birler.")]
        [SerializeField] CounterWheel[] digits = Array.Empty<CounterWheel>();
        [SerializeField] Renderer lamp;
        [SerializeField] Material lampOn, lampOff;

        float value = 1, shown = 1, needleAngle, needleVelocity;
        int digitsShown = -1;
        bool dragging, dimmed, ready;

        public float Value => value;
        public DeskTarget Target => target;
        /// <summary>Değer değişti (sürüklerken her adımda).</summary>
        public event Action<float> Changed;
        /// <summary>Bırakıldı (kayıt için).</summary>
        public event Action Released;
        /// <summary>Çentik: true büyük (%10), false yarım (%5).</summary>
        public event Action<bool> Detent;

        // ---------- Saf hesaplar (testli) ----------

        /// <summary>%5 adıma yuvarlar, 0–1'e kıstırır.</summary>
        public static float Quantize(float v) => Mathf.Clamp01(Mathf.Round(Mathf.Clamp01(v) / Step) * Step);

        /// <summary>Kolun yerel y konumundan değer (adımsız).</summary>
        public static float ValueAt(float localY, float min, float max) => Mathf.Clamp01((localY - min) / Mathf.Max(1e-4f, max - min));

        /// <summary>İki değer arasında çentik var mı: büyük (%10'un katı) ya da yarım (%5).</summary>
        public static bool? DetentBetween(float from, float to)
        {
            int a = Mathf.RoundToInt(from / Step), b = Mathf.RoundToInt(to / Step);
            if (a == b) return null;
            return b % 2 == 0;
        }

        // ---------- Durum ----------

        /// <summary>Değeri kurar (dolap açılırken; olay çıkarmaz).</summary>
        public void Set(float v, bool instant = false)
        {
            value = Quantize(v);
            if (instant || !ready) { shown = value; needleAngle = Angle(value); needleVelocity = 0; }
            Digits(true);
            Apply();
        }

        /// <summary>Sessiz mod: lamba söner, ibre sıfıra düşer; kol yerinde kalır.</summary>
        public void SetDimmed(bool on)
        {
            dimmed = on;
            Lamp();
        }

        /// <summary>Klavye ya da tekerlek: ±adım.</summary>
        public void Nudge(int steps) => SetFromUser(value + steps * Step, true);

        void Awake()
        {
            ready = true;
            Digits(true);
            Apply();
        }

        void SetFromUser(float v, bool release)
        {
            v = Quantize(v);
            if (Mathf.Approximately(v, value)) return;
            var detent = DetentBetween(value, v);
            value = v;
            if (detent.HasValue) Detent?.Invoke(detent.Value);
            Digits(false);
            Lamp();
            Label();
            Changed?.Invoke(value);
            if (release) Released?.Invoke();
        }

        bool Usable => target == null || target.Interactable && (target.BlockedOverride == null || !target.BlockedOverride());

        // ---------- Fare ----------

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !Usable) return;
            dragging = true;
            FromPointer(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (dragging) FromPointer(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            Released?.Invoke();
        }

        public void OnScroll(PointerEventData e)
        {
            if (!Usable || Mathf.Approximately(e.scrollDelta.y, 0)) return;
            Nudge(e.scrollDelta.y > 0 ? 1 : -1);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && e.clickCount == 2 && Usable) SetFromUser(defaultValue, true);
        }

        void FromPointer(PointerEventData e)
        {
            var cam = e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera;
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(e.position);
            var plane = new Plane(transform.forward, transform.position);
            if (!plane.Raycast(ray, out float d)) return;
            var local = transform.InverseTransformPoint(ray.GetPoint(d));
            SetFromUser(ValueAt(local.y, travelMin, travelMax), false);
        }

        // ---------- Çizim ----------

        float Angle(float v) => Mathf.Lerp(needleMinDeg, needleMaxDeg, v);

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // Kol tıklanan yere 0,08 s'de kayar, sürüklerken imleci izler
            shown = dragging ? value : Mathf.MoveTowards(shown, value, dt / 0.08f);
            // İbre: hafif aşmalı yay (sessizde sıfıra düşer)
            float goal = Angle(dimmed ? 0 : value);
            needleVelocity += (goal - needleAngle) * 180 * dt;
            needleVelocity *= Mathf.Exp(-14 * dt);
            needleAngle += needleVelocity * dt;
            Apply();
        }

        void Apply()
        {
            if (handle != null)
            {
                var p = handle.localPosition;
                p.y = Mathf.Lerp(travelMin, travelMax, shown);
                handle.localPosition = p;
            }
            if (needle != null) needle.localRotation = Quaternion.Euler(0, 0, needleAngle);
            Lamp();
            Label();
        }

        void Lamp()
        {
            if (lamp == null || lampOn == null || lampOff == null) return;
            var m = value > 0 && !dimmed ? lampOn : lampOff;
            if (lamp.sharedMaterial != m) lamp.sharedMaterial = m;
        }

        void Label()
        {
            if (target != null) target.LineOverride = DeskFormat.Percent(Mathf.RoundToInt(value * 100));
        }

        void Digits(bool instant)
        {
            int n = Mathf.RoundToInt(value * 100);
            int direction = n >= digitsShown ? 1 : -1;
            digitsShown = n;
            int[] d = { n / 100 % 10, n / 10 % 10, n % 10 };
            for (int i = 0; i < digits.Length && i < 3; i++)
            {
                if (digits[i] == null) continue;
                if (instant || !Application.isPlaying) digits[i].Set(d[i]);
                else digits[i].RollTo(d[i], direction, 0.12f, 0);
            }
        }
    }
}
