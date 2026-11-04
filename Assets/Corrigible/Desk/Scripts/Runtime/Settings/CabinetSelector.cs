using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ayarlar dolabının döner kademe anahtarı (Katman 4; telsizin bant anahtarı gibi): işaretçili düğme, her konum bir
    /// açıda. Tıklama sonraki konuma çevirir (0,12 s, ağır “klonk”); düğme sürüklenerek de çevrilir (imlecin düğmenin
    /// merkezine göre açısı en yakın konuma oturur). Konumların yazıları kendi dillerinde ve sabittir (çevrilmez). Yalnız
    /// dolap açıkken çalışır.
    /// </summary>
    public sealed class CabinetSelector : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] DeskTarget target;
        [Tooltip("Düğme (yerel z ekseninde döner).")]
        [SerializeField] Transform knob;
        [Tooltip("Konumların açıları (yerel z, derece); sıra konum sırasıdır.")]
        [SerializeField] float[] angles = { 40, -40 };

        int index;
        float angle;
        bool dragged;

        public int Index => index;
        public int Count => angles.Length;
        public DeskTarget Target => target;
        /// <summary>Oyuncu çevirdi (yeni konum).</summary>
        public event Action<int> Changed;

        public void Set(int i, bool instant = false)
        {
            index = Mathf.Clamp(i, 0, angles.Length - 1);
            if (instant) { angle = angles[index]; Apply(); }
        }

        /// <summary>Klavye: ±1 konum.</summary>
        public void Nudge(int direction) => Select((index + direction + angles.Length) % angles.Length);

        void Awake()
        {
            angle = angles.Length > 0 ? angles[index] : 0;
            if (target != null) target.Clicked += Clicked;
            Apply();
        }

        void OnDestroy() { if (target != null) target.Clicked -= Clicked; }

        bool Usable => target == null || target.Interactable && (target.BlockedOverride == null || !target.BlockedOverride());

        void Clicked()
        {
            if (dragged) { dragged = false; return; }
            Nudge(1);
        }

        void Select(int i)
        {
            if (!Usable || i == index || angles.Length == 0) return;
            index = i;
            Changed?.Invoke(index);
        }

        public void OnBeginDrag(PointerEventData e) => dragged = false;

        public void OnDrag(PointerEventData e)
        {
            var cam = e.pressEventCamera;
            if (cam == null || knob == null || e.button != PointerEventData.InputButton.Left) return;
            Vector2 center = cam.WorldToScreenPoint(knob.position);
            var d = e.position - center;
            if (d.sqrMagnitude < 100) return;
            dragged = true;
            // Ekrandaki açı (yukarı 0, saat yönünün tersi artı) → en yakın konum
            float deg = Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg;
            int best = index;
            for (int i = 0; i < angles.Length; i++)
                if (Mathf.Abs(Mathf.DeltaAngle(deg, angles[i])) < Mathf.Abs(Mathf.DeltaAngle(deg, angles[best]))) best = i;
            Select(best);
        }

        void Update()
        {
            if (angles.Length == 0) return;
            float goal = angles[index];
            if (Mathf.Approximately(angle, goal)) return;
            angle = Mathf.MoveTowardsAngle(angle, goal, 80 / 0.12f * Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            if (knob != null) knob.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
