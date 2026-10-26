using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kapaklı vardiya şalteri (Karar 2/A, K6 notu): 1. basış kapağı kaldırır (0,25 s; bağlayıcı teleks ön kontrol satırı
    /// basar), 4 s içinde 2. basış kolu indirir ve <see cref="Thrown"/> çıkar. Süre dolar ya da Esc gelirse kapak kapanır.
    /// Devre dışıyken (baskın, açık rapor) sarı-siyah “KİLİTLİ” mühür bandı görünür, hazır lambası söner.
    /// </summary>
    public sealed class ShiftSwitch : MonoBehaviour
    {
        public enum State { Idle, CoverOpen, Thrown, Disabled }

        [SerializeField] Transform coverHinge, lever;
        [SerializeField] Renderer readyLamp;
        [SerializeField] Material lampOn, lampOff;
        [SerializeField] GameObject lockBand;
        [SerializeField] DeskTarget target;
        [Tooltip("Açık kapak açısı (Unity x; three.js −112°).")]
        [SerializeField] float coverOpenDeg = 112;
        [Tooltip("Kol: vardiya sürerken geride (+35), inince operatöre doğru (−35).")]
        [SerializeField] float leverUpDeg = 35, leverDownDeg = -35;

        State state = State.Idle;
        float stateSince, cover, coverGoal, leverAngle, leverGoal;
        bool disabled;

        public State Current => state;
        public DeskTarget Target => target;
        /// <summary>Kapak kalktı (ön kontrol satırı zamanı).</summary>
        public event Action Armed;
        /// <summary>Kol indi: vardiya bitirilir.</summary>
        public event Action Thrown;
        public event Action Cancelled;

        public void Configure(Transform hinge, Transform leverPivot, Renderer lamp, Material on, Material off, GameObject band, DeskTarget clickTarget)
        {
            coverHinge = hinge; lever = leverPivot; readyLamp = lamp; lampOn = on; lampOff = off; lockBand = band; target = clickTarget;
        }

        void Awake()
        {
            leverAngle = leverGoal = leverUpDeg;
            if (target != null) target.Clicked += Press;
            Apply();
        }

        /// <summary>Devre dışı (baskın sürerken ya da rapor açıkken); açık kapak kapanır.</summary>
        public void SetDisabled(bool on, string reason = null)
        {
            if (target != null) target.LineOverride = on ? reason : null;
            if (disabled == on) return;
            disabled = on;
            if (on && state == State.CoverOpen) Close(false);
            if (on && state == State.Idle) Enter(State.Disabled);
            if (!on && state == State.Disabled) Enter(State.Idle);
            Apply();
        }

        /// <summary>Tıklama ya da Enter: kapak → kol. Devre dışıyken ya da kol inikken yok sayılır.</summary>
        public void Press()
        {
            if (disabled) return;
            if (state == State.Idle) { Enter(State.CoverOpen); coverGoal = coverOpenDeg; Armed?.Invoke(); }
            else if (state == State.CoverOpen) { Enter(State.Thrown); leverGoal = leverDownDeg; Thrown?.Invoke(); }
        }

        /// <summary>Esc: açık kapağı kapatır. Bir şey yaptıysa true.</summary>
        public bool Cancel()
        {
            if (state != State.CoverOpen) return false;
            Close(true);
            return true;
        }

        /// <summary>Vardiya dizisi bitti: kol kalkar, kapak kapanır.</summary>
        public void Release()
        {
            if (state != State.Thrown) return;
            leverGoal = leverUpDeg;
            coverGoal = 0;
            Enter(disabled ? State.Disabled : State.Idle);
        }

        void Close(bool notify)
        {
            coverGoal = 0;
            Enter(disabled ? State.Disabled : State.Idle);
            if (notify) Cancelled?.Invoke();
        }

        void Enter(State next) { state = next; stateSince = Time.unscaledTime; Apply(); }

        void Update()
        {
            var t = SiginakTheme.Active;
            if (state == State.CoverOpen && Time.unscaledTime - stateSince > t.Number("motion.coverAutoCloseS", 4)) Close(true);
            float coverSpeed = coverOpenDeg / Mathf.Max(0.01f, t.Number("motion.coverLiftS", 0.25f));
            cover = Mathf.MoveTowards(cover, coverGoal, coverSpeed * Time.unscaledDeltaTime);
            leverAngle = Mathf.MoveTowards(leverAngle, leverGoal, (leverUpDeg - leverDownDeg) / 0.15f * Time.unscaledDeltaTime);
            if (coverHinge != null) coverHinge.localRotation = Quaternion.Euler(cover, 0, 0);
            if (lever != null) lever.localRotation = Quaternion.Euler(leverAngle, 0, 0);
        }

        void Apply()
        {
            bool ready = !disabled && state != State.Thrown;
            if (readyLamp != null) readyLamp.sharedMaterial = ready ? lampOn : lampOff;
            if (lockBand != null) lockBand.SetActive(disabled);
            if (target != null) target.Interactable = !disabled;
        }
    }
}
