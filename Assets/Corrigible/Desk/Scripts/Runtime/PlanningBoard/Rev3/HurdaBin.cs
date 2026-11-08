using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kartlı panonun HURDA bidonu (Revizyon 3 · R15b, R16b): geçerli bir eşya kartı ön yüzün üstüne gelince kapak aralanır
    /// (≈ 4,5°, 76 mm kapakta 6 mm); kart bırakılınca kapak 0,15 s'de 28° kalkar ("tunk"), kart yarığa düşüp gizlenir, kapak
    /// 0,10 s bekleyip 0,15 s'de kapanır. Kapak <see cref="PartDriver"/>'ı yumuşatılmış kendi aranımıyla sürülür (parçanın kendi
    /// hareketi sabit hızlıdır). Bırakma hedefi bidonun bütün ön yüzüdür (<see cref="Front"/>, ışınla çözülür; pano yüzünün
    /// dikdörtgeni değil, 75 mm öne çıkıntı yüzünden). Klavye odağı kapak aralığı + ön yüzdeki çerçevedir
    /// (<see cref="SetFocused"/>). Simülasyona dokunmaz; komutu köprü verir.
    /// </summary>
    public sealed class HurdaBin : MonoBehaviour
    {
        [Tooltip("PlanningBoard › HurdaLid (dönme, x, 0 → 28°; +28° ön kenarı kaldırır).")]
        [SerializeField] PartDriver lid;
        [Tooltip("Bidonun ön yüzü (130 × 126 mm, z −0,089): bırakma ve tıklama hedefi.")]
        [SerializeField] Collider front;
        [Tooltip("Kapaktaki yarık (Hurda_Slot çapası); yutulan kart buraya iner. Boşsa (1077, 464) mm, z −0,074.")]
        [SerializeField] Transform slot;
        [Tooltip("İsteğe bağlı klavye odağı çerçevesi (ön yüz düzleminde, z −0,0895); yoksa odak yalnız kapak aralığıdır.")]
        [SerializeField] GameObject focusFrame;

        const int EaseLinear = 0, EaseIn = 1, EaseOut = 2;

        float lidFrom, lidTo, lidStart, lidSeconds;
        int lidEase;
        bool lidActive;
        float queuedAt = -1, queuedSeconds;
        int queuedEase;
        bool queuedToRest;

        BoardCardView swallowCard;
        Action swallowDone;
        float swallowHideAt;

        public Collider Front => front;
        public PartDriver Lid => lid;
        /// <summary>Geçerli bir eşya kartı üstünde (kapak aralık).</summary>
        public bool Targeted { get; private set; }
        /// <summary>Klavye odağı bidonda.</summary>
        public bool Focused { get; private set; }
        /// <summary>Bir kart yutuluyor (kapak açık ya da kapanıyor).</summary>
        public bool Swallowing => swallowCard != null;
        public event Action<BoardSound> Sound;

        static float Num(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        static float LidS => Num("motion.board.hurdaLidS", 0.15f);
        static float HoldS => Num("motion.board.hurdaHoldS", 0.10f);
        static float AjarDeg => Num("board.hurdaAjarDeg", 4.5f);
        static float OpenDeg => Num("board.hurdaOpenDeg", 28f);

        /// <summary>Editör kurucusu ve testler bağlar (çalışma anında hiyerarşi aranmaz).</summary>
        public void Configure(PartDriver lidPart, Collider frontFace, Transform slotAnchor, GameObject focus)
        {
            lid = lidPart; front = frontFace; slot = slotAnchor; focusFrame = focus;
        }

        /// <summary>Geçerli bir kart üstündeyken kapak aralanır; kalkınca kapanır. Yutma sürerken kapağa dokunmaz.</summary>
        public void SetTargeted(bool on)
        {
            if (Targeted == on) return;
            Targeted = on;
            if (Swallowing) return;
            AnimateLid(on ? AjarDeg : 0, LidS, on ? EaseOut : EaseIn);
        }

        /// <summary>Klavye odağının çerçevesi (kapak aralığı ayrıca <see cref="SetTargeted"/> ile).</summary>
        public void SetFocused(bool on)
        {
            Focused = on;
            if (focusFrame != null && focusFrame.activeSelf != on) focusFrame.SetActive(on);
        }

        /// <summary>
        /// Kartı yutar: ses, kapak 28°, kart 0,15 s'de yarığa iner ve gizlenir, sonra <paramref name="done"/> çağrılır (kartın
        /// havuza dönüşü çağıranındır). Kapak 0,10 s bekleyip kapanır. Önceki yutma bitmeden yenisi gelirse önceki hemen biter.
        /// </summary>
        public void Swallow(BoardCardView c, Action done)
        {
            if (Swallowing) FinishSwallow();
            if (c == null) { done?.Invoke(); return; }
            swallowCard = c;
            swallowDone = done;
            float s = LidS;
            swallowHideAt = Time.unscaledTime + s;
            Sound?.Invoke(BoardSound.Hurda);
            AnimateLid(OpenDeg, s, EaseOut);
            QueueLid(s + HoldS, s, EaseIn);
            c.MoveTo(SlotLocal(c), 0, s, CardEase.EaseIn);
            c.SetLift(0, 0, s);
        }

        Vector3 SlotLocal(BoardCardView c)
        {
            var parent = c.transform.parent;
            if (slot != null) return parent != null ? parent.InverseTransformPoint(slot.position) : slot.position;
            return BoardLayout.MmToLocal(new Vector2(1077, 464), -0.074f);
        }

        void AnimateLid(float to, float seconds, int ease)
        {
            queuedAt = -1;
            if (lid == null) return;
            if (seconds <= 0) { lid.Set(to); lidActive = false; return; }
            lidFrom = lid.Value;
            lidTo = to;
            lidStart = Time.unscaledTime;
            lidSeconds = seconds;
            lidEase = ease;
            lidActive = true;
        }

        /// <summary>Kapağı <paramref name="delay"/> sonra dinlenmeye (hedefliyse aralığa) götürür.</summary>
        void QueueLid(float delay, float seconds, int ease)
        {
            queuedAt = Time.unscaledTime + delay;
            queuedSeconds = seconds;
            queuedEase = ease;
            queuedToRest = true;
        }

        static float Ease(int kind, float t)
        {
            switch (kind)
            {
                case EaseIn: return t * t;
                case EaseOut: return 1 - (1 - t) * (1 - t);
                default: return t;
            }
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (lidActive && lid != null)
            {
                float t = Mathf.Clamp01((now - lidStart) / lidSeconds);
                lid.Set(Mathf.LerpUnclamped(lidFrom, lidTo, Ease(lidEase, t)));
                if (t >= 1) lidActive = false;
            }
            if (swallowCard != null && now >= swallowHideAt) FinishSwallow();
            if (queuedAt >= 0 && now >= queuedAt && !Swallowing)
            {
                float to = queuedToRest && Targeted ? AjarDeg : 0;
                AnimateLid(to, queuedSeconds, queuedEase);
            }
        }

        void FinishSwallow()
        {
            var c = swallowCard;
            var done = swallowDone;
            swallowCard = null;
            swallowDone = null;
            if (c != null) c.SetVisible(false);
            done?.Invoke();
        }

        void OnDisable()
        {
            if (Swallowing) FinishSwallow();
            lidActive = false;
            queuedAt = -1;
            Targeted = false;
            SetFocused(false);
            if (lid != null) lid.Set(0);
        }
    }
}
