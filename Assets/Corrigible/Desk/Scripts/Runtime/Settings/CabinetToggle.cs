using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ayarlar dolabının yarasa kollu anahtarı (Katman 3): yukarısı AÇIK, aşağısı KAPALI; tıklama kolu 0,08 s'de çevirir,
    /// kol yukarı-aşağı sürüklenerek de çevrilir. Üstündeki amber lamba açıkken yanar. Etiketin satırını dolap yazar
    /// (<see cref="SettingsCabinet"/>). Yalnız dolap açıkken çalışır.
    /// </summary>
    public sealed class CabinetToggle : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        [SerializeField] DeskTarget target;
        [Tooltip("Kolun pivotu (yerel x ekseninde döner).")]
        [SerializeField] Transform handle;
        [SerializeField] float onDeg = -28, offDeg = 28;
        [SerializeField] Renderer lamp;
        [SerializeField] Material lampOn, lampOff;

        bool on = true, dragged;
        float angle;

        public bool On => on;
        public DeskTarget Target => target;
        /// <summary>Oyuncu çevirdi.</summary>
        public event Action<bool> Changed;

        public void Set(bool value, bool instant = false)
        {
            on = value;
            if (instant) angle = Goal;
            Apply();
        }

        /// <summary>Klavye: yukarı açar, aşağı kapatır.</summary>
        public void Nudge(int direction)
        {
            if (direction > 0 && !on || direction < 0 && on) Flip();
        }

        float Goal => on ? onDeg : offDeg;

        void Awake()
        {
            angle = Goal;
            if (target != null) target.Clicked += Clicked;
            Apply();
        }

        void OnDestroy() { if (target != null) target.Clicked -= Clicked; }

        bool Usable => target == null || target.Interactable && (target.BlockedOverride == null || !target.BlockedOverride());

        void Clicked()
        {
            // Sürükleyerek çevrildiyse bırakıştaki tık ikinci kez çevirmesin
            if (dragged) { dragged = false; return; }
            Flip();
        }

        public void Flip()
        {
            if (!Usable) return;
            on = !on;
            Apply();
            Changed?.Invoke(on);
        }

        public void OnBeginDrag(PointerEventData e) => dragged = false;

        public void OnDrag(PointerEventData e)
        {
            if (dragged || e.button != PointerEventData.InputButton.Left) return;
            float dy = e.position.y - e.pressPosition.y;
            if (Mathf.Abs(dy) < 12) return;
            dragged = true;
            if (dy > 0 != on) Flip();
        }

        void Update()
        {
            if (Mathf.Approximately(angle, Goal)) return;
            angle = Mathf.MoveTowards(angle, Goal, Mathf.Abs(onDeg - offDeg) / 0.08f * Time.unscaledDeltaTime);
            if (handle != null) handle.localRotation = Quaternion.Euler(angle, 0, 0);
        }

        void Apply()
        {
            if (handle != null) handle.localRotation = Quaternion.Euler(angle, 0, 0);
            if (lamp != null && lampOn != null && lampOff != null) lamp.sharedMaterial = on ? lampOn : lampOff;
        }
    }
}
