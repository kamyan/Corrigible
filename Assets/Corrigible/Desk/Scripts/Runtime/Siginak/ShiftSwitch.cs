using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kapaklı vardiya şalteri (Karar 2/A, K6 notu): 1. basış kapağı kaldırır (0,25 s; bağlayıcı teleks ön kontrol satırı
    /// basar), 4 s içinde 2. basış kolu indirir ve <see cref="Thrown"/> çıkar. Süre dolar ya da Esc gelirse kapak kapanır.
    /// Devre dışıyken (baskın, açık rapor) sarı-siyah “KİLİTLİ” mühür bandı görünür, hazır lambası söner.
    /// <para>
    /// Revizyon 3 · R19 kipi (<c>shiftClose</c>; yalnız odadaki şalter, ayarlar dolabı ve menü kapalı bırakır; PLAN §1.4): süreler ve
    /// açılar token'dan (<c>motion.shiftClose.coverS</c> 0,25 s açılış ease-out, <c>.coverCloseS</c> 0,3 s kapanış, <c>.leverS</c>
    /// 0,3 s iki aşamalı kol, <c>.rejectLeverS</c> 0,4 s ret dönüşü, <c>shiftClose.states.cover.autoCloseS</c> 4 s), iç saat
    /// <see cref="Advance"/>'a verilen sürelerin toplamıdır (EditMode testleri süreyi böyle ölçer). Durum <see cref="Phase"/> olarak
    /// yayınlanır, plakanın ikinci satırını ve üzerine gelme satırını (<see cref="DeskTarget.LineOverride"/>) yalnız
    /// <see cref="ShiftPlate"/> yazar. Hazırken üzerine gelince kapağın ön kenarı 3 mm kalkar; kilitliyken tık ya da Enter ön kenarı
    /// 2 mm titretir ve <see cref="Denied"/> çıkar (durum değişmez); basım reddinde <see cref="RejectThrow"/> kolu indirip 0,4 s'de
    /// geri kaldırır (kapak açık kalır, 4 s sayacı yeniden başlar). Runtime ses çalamadığı için sesler <see cref="Sound"/> olayıyla
    /// köprüye gider. Kapalıyken (eski kip) bugünkü davranış aynen: kapak iki yönde <c>motion.coverLiftS</c>, kol 0,15 s,
    /// kilitliyken tık yok, plaka yok, ses ve R19 olayları yok.
    /// </para>
    /// <para>
    /// Kurulum tembeldir: Edit modda <c>Awake</c> çalışmaz, ilk çağrı (<see cref="Advance"/>, <see cref="Press"/>,
    /// <see cref="SetAwaitingReceipt(bool, bool)"/>, <see cref="SetDisabled(bool, string, string)"/> …) kurar; <c>Awake</c> bir kez
    /// kurulduysa açıları yeniden yazmaz (önce gelen bekleme duruşu korunur).
    /// </para>
    /// </summary>
    public sealed class ShiftSwitch : MonoBehaviour
    {
        public enum State { Idle, CoverOpen, Thrown, Disabled }

        [SerializeField] Transform coverHinge, lever;
        [SerializeField] Transform pushButton;
        [SerializeField] float buttonTravel = .008f;
        Vector3 buttonRest;
        float buttonPressedUntil;
        [SerializeField] Renderer readyLamp;
        [SerializeField] Material lampOn, lampOff;
        [SerializeField] GameObject lockBand;
        [SerializeField] DeskTarget target;
        [Tooltip("Açık kapak açısı (Unity x; three.js −112°). R19 kipinde token shiftClose.states.cover.cover önce gelir.")]
        [SerializeField] float coverOpenDeg = 112;
        [Tooltip("Kol: vardiya sürerken geride (+35), inince operatöre doğru (−35). R19 kipinde token shiftClose.states.ready/waiting.lever önce gelir.")]
        [SerializeField] float leverUpDeg = 35, leverDownDeg = -35;
        [SerializeField] float leverSeconds = .15f;
        [Tooltip("Ayarlar dolabında kilitli tık kapağı sallar; eski şalterlerin davranışı değişmez.")]
        [SerializeField] bool lockedFeedback;
        [Header("Revizyon 3 · R19 vardiya kapanışı")]
        [Tooltip("R19 kipi (odadaki vardiya şalteri). Ayarlar dolabının MENÜYE DÖN kolu ve menünün son şalteri kapalı bırakır.")]
        [SerializeField] bool shiftClose;
        [Tooltip("Plakanın ikinci satırı (R19 kipi); boşsa plaka yazılmaz.")]
        [SerializeField] ShiftPlate plate;
        [Tooltip("Kapağın ön kenarının menteşe eksenine uzaklığı (m); üzerine gelme kalkışı ve titreme mm'den açıya bununla çevrilir.")]
        [SerializeField] float coverRadiusM = 0.159f;

        /// <summary>
        /// Üzerine gelme kalkışının süresi (s): <see cref="DeskTarget"/>'ın kalkış süresiyle aynı (<c>DeskTarget.cs</c> 0,08 s);
        /// temada genel bir anahtar yok.
        /// </summary>
        const float HoverLiftS = 0.08f;
        /// <summary>
        /// İki aşamalı kolun (“klak-klunk”) bekleme aralığı, kol süresinin oranı: 0–0,14 s ilk yarı (ease-out), 0,14–0,16 s bekleme,
        /// 0,16–0,30 s ikinci yarı (ease-in-out). Eğri tasarımda yok (PLAN §1.4 T3, UNVERIFIED).
        /// </summary>
        const float DetentFrom = 0.14f / 0.30f, DetentTo = 0.16f / 0.30f;

        enum Curve { Out, InOut, TwoStage }

        /// <summary>Tek bir açının <c>from → to</c> zamanlı hareketi (R19 kipi).</summary>
        struct Motion
        {
            public bool Active;
            public float From, To, Elapsed, Seconds;
            public Curve Curve;

            public static Motion Start(float from, float to, float seconds, Curve curve) =>
                new Motion { Active = true, From = from, To = to, Elapsed = 0, Seconds = Mathf.Max(0, seconds), Curve = curve };

            /// <summary>Süreyi ilerletir ve açıyı döner; bitince <see cref="Active"/> false olur ve tam hedefi döner.</summary>
            public float Step(float dt)
            {
                Elapsed += dt;
                if (Seconds <= 0 || Elapsed >= Seconds) { Active = false; return To; }
                return From + (To - From) * Ease(Curve, Elapsed / Seconds);
            }
        }

        State state = State.Idle;
        float stateSince, cover, coverGoal, leverAngle, leverGoal;
        bool disabled, awaitingReceipt;
        bool initialized;

        // R19 kipi
        float clock;
        bool rejecting;
        Motion coverMove, leverMove;
        float hoverDeg, jiggleDeg, jiggleElapsed, jiggleSeconds, jiggleAmpDeg;
        bool jiggling;
        ShiftPhase notifiedPhase;

        public State Current => state;
        public DeskTarget Target => target;
        /// <summary>R19 kipi açık mı (odadaki şalter).</summary>
        public bool ShiftCloseMode => shiftClose;
        /// <summary>Plakanın ikinci satırı (R19 kipi; boş olabilir).</summary>
        public ShiftPlate Plate => plate;
        /// <summary>Kapak kalktı (ön kontrol satırı zamanı).</summary>
        public event Action Armed;
        /// <summary>Kol indi: vardiya bitirilir.</summary>
        public event Action Thrown;
        public event Action Cancelled;

        /// <summary>R19: <see cref="Phase"/> değişti (kayıttan anında oturuşta da; durum bildirimi, eylem değil).</summary>
        public event Action<ShiftPhase> PhaseChanged;
        /// <summary>R19: kol durdu; true aşağıda (−35°, fiş çıkışı), false yukarıda (+35°). Anında oturuşlarda çıkmaz.</summary>
        public event Action<bool> LeverArrived;
        /// <summary>R19: kilitliyken tık ya da Enter (kapak 2 mm titrer, durum değişmez).</summary>
        public event Action Denied;
        /// <summary>R19: ses olayı (köprü <c>Sfx</c>'e çevirir).</summary>
        public event Action<ShiftSound> Sound;

        /// <summary>
        /// Şalterin R19 durumu: fiş bekleniyorsa <see cref="ShiftPhase.Waiting"/>, basım reddinde kol inip kalkarken
        /// <see cref="ShiftPhase.Rejecting"/>, kol inikse (köprü henüz karar vermedi) <see cref="ShiftPhase.Waiting"/>, devre dışıysa
        /// <see cref="ShiftPhase.Locked"/>, kapak açıksa <see cref="ShiftPhase.CoverUp"/>, değilse <see cref="ShiftPhase.Ready"/>.
        /// </summary>
        public ShiftPhase Phase => awaitingReceipt ? ShiftPhase.Waiting
            : rejecting ? ShiftPhase.Rejecting
            : state == State.Thrown ? ShiftPhase.Waiting
            : disabled ? ShiftPhase.Locked
            : state == State.CoverOpen ? ShiftPhase.CoverUp
            : ShiftPhase.Ready;
        /// <summary>Kol aşağıda (−35°) durdu.</summary>
        public bool LeverDown => !leverMove.Active && Mathf.Approximately(leverAngle, DownDeg) && Mathf.Approximately(leverGoal, DownDeg);
        /// <summary>Kilitliyken plakanın kısa nedeni (o anki dilde); kilit yoksa null.</summary>
        public string LockPlate { get; private set; }
        /// <summary>Kilitliyken üzerine gelme satırının uzun nedeni (o anki dilde); kilit yoksa null.</summary>
        public string LockLine { get; private set; }

        /// <summary>Kapağın çizilen açısı (°, Unity x): hareket + üzerine gelme kalkışı + titreme.</summary>
        public float CoverDeg => cover + hoverDeg + jiggleDeg;
        /// <summary>Kolun çizilen açısı (°, Unity x).</summary>
        public float LeverDeg => leverAngle;
        /// <summary>Kapağın hedef açısı (°; titreme ve üzerine gelme hariç).</summary>
        public float CoverGoalDeg => coverGoal;
        /// <summary>Kolun hedef açısı (°).</summary>
        public float LeverGoalDeg => leverGoal;
        /// <summary>Kapak ya da kol hareket ediyor (R19 kipi).</summary>
        public bool Moving => coverMove.Active || leverMove.Active;

        public void Configure(Transform hinge, Transform leverPivot, Renderer lamp, Material on, Material off, GameObject band, DeskTarget clickTarget)
        {
            if (initialized && target != null) target.Clicked -= Press;
            coverHinge = hinge; lever = leverPivot; readyLamp = lamp; lampOn = on; lampOff = off; lockBand = band; target = clickTarget;
            if (initialized && target != null) target.Clicked += Press;
        }

        /// <summary>R19 kipini ve plakayı bağlar (tek seferlik sahne script'i ve testler).</summary>
        public void ConfigureShiftClose(ShiftPlate plateView, bool on = true)
        {
            plate = plateView;
            shiftClose = on;
            if (initialized) Apply();
        }

        void Awake() => EnsureInit();

        /// <summary>
        /// İlk kurulum (Awake'te; Awake çalışmamışsa ilk çağrıda): düğme yeri, kol yukarıda, tık aboneliği. Bir kez çalışır; önce gelen
        /// <see cref="SetAwaitingReceipt(bool, bool)"/> duruşu sonradan gelen Awake'te ezilmez (A §7).
        /// </summary>
        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            if (pushButton) buttonRest = pushButton.localPosition;
            leverAngle = leverGoal = UpDeg;
            if (target != null) target.Clicked += Press;
            notifiedPhase = Phase;
            Apply();
        }

        /// <summary>Devre dışı (baskın sürerken ya da rapor açıkken); açık kapak kapanır.</summary>
        public void SetDisabled(bool on, string reason = null) => SetDisabled(on, reason, null);

        /// <summary>
        /// Devre dışı; açık kapak kapanır. <paramref name="reason"/> üzerine gelme satırının uzun nedeni,
        /// <paramref name="plateReason"/> plakanın kısa nedeni (o anki dilde). R19 kipinde üzerine gelme satırını bu yöntem yazmaz
        /// (tek yazıcı <see cref="ShiftPlate"/>), nedenleri plakaya iletir. Her karede çağrılabilir.
        /// </summary>
        public void SetDisabled(bool on, string reason, string plateReason)
        {
            EnsureInit();
            LockLine = on ? reason : null;
            LockPlate = on ? plateReason : null;
            if (target != null && !shiftClose) target.LineOverride = on ? reason : null;
            if (disabled == on) { if (shiftClose) ShowPlate(); return; }
            disabled = on;
            if (on && state == State.CoverOpen) Close(false);
            if (on && state == State.Idle) Enter(State.Disabled);
            if (!on && state == State.Disabled) Enter(State.Idle);
            Apply();
        }

        /// <summary>
        /// Tıklama ya da Enter: kapak → kol. Eski kipte devre dışıyken ya da kol inikken yok sayılır. R19 kipinde fiş beklenirken ve
        /// ret sürerken yok sayılır, kilitliyken <see cref="Denied"/> çıkar ve kapak titrer.
        /// </summary>
        public void Press()
        {
            EnsureInit();
            if (!shiftClose)
            {
                if (disabled) { if (lockedFeedback) Deny(); return; }
                if (awaitingReceipt) return;
                if (state == State.Idle) { Enter(State.CoverOpen); coverGoal = coverOpenDeg; Armed?.Invoke(); }
                else if (state == State.CoverOpen) { buttonPressedUntil = Now + .16f; Enter(State.Thrown); leverGoal = leverDownDeg; Thrown?.Invoke(); }
                return;
            }
            if (awaitingReceipt || rejecting || state == State.Thrown) return;
            if (disabled) { Deny(); return; }
            if (state == State.Idle)
            {
                Enter(State.CoverOpen);
                MoveCover(OpenDeg, CoverS, Curve.Out);
                Sound?.Invoke(ShiftSound.CoverOpen);
                Armed?.Invoke();
            }
            else if (state == State.CoverOpen)
            {
                buttonPressedUntil = Now + .16f;
                Enter(State.Thrown);
                MoveLever(DownDeg, LeverS, Curve.TwoStage);
                Sound?.Invoke(ShiftSound.Lever);
                Thrown?.Invoke();
            }
        }

        /// <summary>
        /// Basılı devir fişi EVRAK'a girene kadar şalter inik kalır (kapak açık, kol aşağıda) ve tıklanmaz; fiş girince kol
        /// kalkar, kapak kapanır. Kayıttan basılı fişle açılınca da aynı duruşa geçer (olay çıkarmaz).
        /// </summary>
        public void SetAwaitingReceipt(bool waiting) => SetAwaitingReceipt(waiting, false);

        /// <summary>
        /// <paramref name="waiting"/> true: fiş bekliyor duruşu (kapak açık, kol aşağıda, tıklanmaz; R19 kipinde süren kol hareketi
        /// kesilmez, varınca <see cref="LeverArrived"/> true çıkar); false: dosyalama sonu, kol kalkar ve kapak kapanır (R19: 0,3 / 0,3 s,
        /// <see cref="ShiftSound.CoverShut"/>). <paramref name="instant"/>: duruşa anında oturur (kayıttan yükleme; eylem olayı ve ses yok).
        /// </summary>
        public void SetAwaitingReceipt(bool waiting, bool instant)
        {
            EnsureInit();
            if (awaitingReceipt == waiting) return;
            awaitingReceipt = waiting;
            if (!shiftClose)
            {
                if (waiting) { coverGoal = coverOpenDeg; leverGoal = leverDownDeg; Enter(State.Thrown); }
                else Release();
                if (instant) Snap();
                Apply();
                return;
            }
            if (waiting)
            {
                rejecting = false;
                Enter(State.Thrown);
                if (instant) { coverGoal = OpenDeg; leverGoal = DownDeg; Snap(); }
                else
                {
                    if (!Mathf.Approximately(coverGoal, OpenDeg)) MoveCover(OpenDeg, CoverS, Curve.Out);
                    if (!Mathf.Approximately(leverGoal, DownDeg)) MoveLever(DownDeg, LeverS, Curve.TwoStage);
                }
            }
            else Lift(!instant);
            Apply();
        }

        /// <summary>
        /// R19 basım reddi: kol −35°'ye varır (<see cref="LeverArrived"/> true; köprü boş ağız sesini çalar), sonra
        /// <c>motion.shiftClose.rejectLeverS</c> (0,4 s) içinde +35°'ye kalkar (<see cref="LeverArrived"/> false); kapak açık kalır ve
        /// 4 s sayacı yeniden başlar. Kol kalktığında şalter kilitlendiyse kapak kapanır. Yalnız kol inikken (köprünün
        /// <see cref="Thrown"/> işleyicisinde) anlamlıdır; eski kipte <see cref="Release()"/> gibi davranır.
        /// </summary>
        public void RejectThrow()
        {
            EnsureInit();
            if (!shiftClose) { Release(); return; }
            if (state != State.Thrown || awaitingReceipt || rejecting) return;
            rejecting = true;
            if (!Mathf.Approximately(leverGoal, DownDeg)) MoveLever(DownDeg, LeverS, Curve.TwoStage);
            else if (!leverMove.Active) leverMove = Motion.Start(leverAngle, DownDeg, 0, Curve.InOut);   // zaten aşağıda: sonraki karede “vardı”
            Apply();
        }

        /// <summary>Esc: açık kapağı kapatır. Bir şey yaptıysa true.</summary>
        public bool Cancel()
        {
            EnsureInit();
            if (state != State.CoverOpen) return false;
            Close(true);
            return true;
        }

        /// <summary>Vardiya dizisi bitti: kol kalkar, kapak kapanır. Basılı fiş beklenirken inik kalır.</summary>
        public void Release() => Release(true);

        /// <summary>
        /// Kol kalkar, kapak kapanır; <paramref name="animate"/> false ise anında. Basılı fiş beklenirken inik kalır (no-op);
        /// dosyalama sonu <see cref="SetAwaitingReceipt(bool, bool)"/> ile kalkar. R19 kipinde süren bir reddi de bitirir.
        /// </summary>
        public void Release(bool animate)
        {
            EnsureInit();
            if (state != State.Thrown || awaitingReceipt) return;
            if (shiftClose) { Lift(animate); return; }
            leverGoal = leverUpDeg;
            coverGoal = 0;
            Enter(disabled ? State.Disabled : State.Idle);
            if (!animate) Snap();
        }

        /// <summary>R19: kol yukarı, kapak kapalı (0,3 / 0,3 s, kapanış sesi) ya da anında.</summary>
        void Lift(bool animate)
        {
            rejecting = false;
            Enter(disabled ? State.Disabled : State.Idle);
            if (animate)
            {
                MoveLever(UpDeg, LeverS, Curve.InOut);
                MoveCover(0, CoverCloseS, Curve.InOut);
                Sound?.Invoke(ShiftSound.CoverShut);
            }
            else { leverGoal = UpDeg; coverGoal = 0; Snap(); }
        }

        void Close(bool notify)
        {
            coverGoal = 0;
            if (shiftClose) { MoveCover(0, CoverCloseS, Curve.InOut); Sound?.Invoke(ShiftSound.CoverShut); }
            Enter(disabled ? State.Disabled : State.Idle);
            if (notify) Cancelled?.Invoke();
        }

        /// <summary>R19: kilitliyken tık. Kapağın ön kenarı <c>motion.board.denyShakeMm</c> kalkıp iner (panonun titremesiyle aynı süre).</summary>
        void Deny()
        {
            jiggleAmpDeg = MmToDeg(DenyMm);
            jiggleSeconds = DenyS;
            jiggleElapsed = 0;
            jiggling = jiggleSeconds > 0 && jiggleAmpDeg > 0;
            if (!jiggling) jiggleDeg = 0;
            Sound?.Invoke(ShiftSound.Denied);
            Denied?.Invoke();
        }

        void Enter(State next) { state = next; stateSince = Now; Apply(); }

        /// <summary>Eski kipte gerçek zaman (bugünkü gibi), R19 kipinde <see cref="Advance"/>'ın iç saati.</summary>
        float Now => shiftClose ? clock : Time.unscaledTime;

        void MoveCover(float goal, float seconds, Curve curve)
        {
            coverGoal = goal;
            coverMove = Motion.Start(cover, goal, seconds, curve);
        }

        void MoveLever(float goal, float seconds, Curve curve)
        {
            leverGoal = goal;
            leverMove = Motion.Start(leverAngle, goal, seconds, curve);
        }

        /// <summary>Kapak ve kol hedef açılarına anında oturur (süren hareket, kalkış ve titreme biter).</summary>
        void Snap()
        {
            coverMove.Active = false;
            leverMove.Active = false;
            hoverDeg = 0;
            jiggling = false;
            jiggleDeg = 0;
            cover = coverGoal;
            leverAngle = leverGoal;
            Place();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        /// <summary>
        /// Hareketi <paramref name="dt"/> saniye ilerletir (<c>Update</c> gerçek zamanla çağırır; EditMode testleri doğrudan).
        /// </summary>
        public void Advance(float dt)
        {
            EnsureInit();
            if (!shiftClose)
            {
                var t = SiginakTheme.Active;
                if (state == State.CoverOpen && Now - stateSince > t.Number("motion.coverAutoCloseS", 4)) Close(true);
                float coverSpeed = coverOpenDeg / Mathf.Max(0.01f, t.Number("motion.coverLiftS", 0.25f));
                cover = Mathf.MoveTowards(cover, coverGoal, coverSpeed * dt);
                leverAngle = Mathf.MoveTowards(leverAngle, leverGoal, (leverUpDeg - leverDownDeg) / Mathf.Max(.01f,leverSeconds) * dt);
                AdvanceJiggle(Mathf.Max(0,dt));
                Place();
                return;
            }

            if (dt < 0) dt = 0;
            clock += dt;
            if (state == State.CoverOpen && clock - stateSince > AutoCloseS) Close(true);
            if (coverMove.Active) cover = coverMove.Step(dt);
            if (leverMove.Active)
            {
                leverAngle = leverMove.Step(dt);
                if (!leverMove.Active) LeverStopped();
            }

            float hoverMax = MmToDeg(HoverLiftMm);
            float hoverGoal = HoverWanted ? hoverMax : 0;
            hoverDeg = Mathf.MoveTowards(hoverDeg, hoverGoal, hoverMax / HoverLiftS * dt);

            AdvanceJiggle(dt);
            Place();
        }

        void AdvanceJiggle(float dt)
        {
            if (jiggling)
            {
                jiggleElapsed += dt;
                float u = jiggleSeconds > 0 ? jiggleElapsed / jiggleSeconds : 1;
                if (u >= 1) { jiggling = false; jiggleDeg = 0; }
                // Panonun titremesiyle aynı dalga (3 yarım dalga, BoardCardView.StepShake); kapak yuvasına oturduğu için katlanır,
                // ön kenar hiç aşağı inmez.
                else jiggleDeg = jiggleAmpDeg * Mathf.Abs(Mathf.Sin(3 * Mathf.PI * u));
            }
        }

        /// <summary>Hazır, üzerinde ve kapak yerinde duruyor: ön kenar kalkar (R19 T0).</summary>
        bool HoverWanted => Phase == ShiftPhase.Ready && target != null && target.IsHovered && !coverMove.Active && Mathf.Approximately(coverGoal, 0);

        void LeverStopped()
        {
            bool down = Mathf.Approximately(leverGoal, DownDeg);
            if (!rejecting) { LeverArrived?.Invoke(down); return; }
            if (down)
            {
                LeverArrived?.Invoke(true);
                if (rejecting) MoveLever(UpDeg, RejectLeverS, Curve.InOut);
                return;
            }
            rejecting = false;
            if (disabled) Close(false);
            else Enter(State.CoverOpen);
            LeverArrived?.Invoke(false);
        }

        void Place()
        {
            if (coverHinge != null) coverHinge.localRotation = Quaternion.Euler(cover + hoverDeg + jiggleDeg, 0, 0);
            if (lever != null) lever.localRotation = Quaternion.Euler(leverAngle, 0, 0);
            if (pushButton) pushButton.localPosition = buttonRest + Vector3.down * (state == State.Thrown || Now < buttonPressedUntil ? buttonTravel : 0);
        }

        void Apply()
        {
            if (!shiftClose)
            {
                bool ready = !disabled && !awaitingReceipt && state != State.Thrown;
                if (readyLamp != null) readyLamp.sharedMaterial = ready ? lampOn : lampOff;
                if (lockBand != null) lockBand.SetActive(disabled);
                if (target != null) target.Interactable = (!disabled || lockedFeedback) && !awaitingReceipt;
                return;
            }
            var phase = Phase;
            bool lit = phase == ShiftPhase.Ready || phase == ShiftPhase.CoverUp || phase == ShiftPhase.Rejecting;
            if (readyLamp != null) readyLamp.sharedMaterial = lit ? lampOn : lampOff;
            if (lockBand != null) lockBand.SetActive(phase == ShiftPhase.Locked);
            // Kilitliyken tık ulaşır (yalnız titreme); fiş beklenirken ve ret sürerken ulaşmaz.
            if (target != null) target.Interactable = phase == ShiftPhase.Ready || phase == ShiftPhase.CoverUp || phase == ShiftPhase.Locked;
            ShowPlate();
            if (phase != notifiedPhase)
            {
                notifiedPhase = phase;
                PhaseChanged?.Invoke(phase);
            }
        }

        void ShowPlate()
        {
            if (plate != null) plate.Show(Phase, LockPlate, LockLine);
        }

        // ------------------------------------------------------------------ tema (R19 kipi)

        static float Num(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        float OpenDeg => shiftClose ? Num("shiftClose.states.cover.cover", coverOpenDeg) : coverOpenDeg;
        float UpDeg => shiftClose ? Num("shiftClose.states.ready.lever", leverUpDeg) : leverUpDeg;
        float DownDeg => shiftClose ? Num("shiftClose.states.waiting.lever", leverDownDeg) : leverDownDeg;
        static float CoverS => Num("motion.shiftClose.coverS", 0.25f);
        static float CoverCloseS => Num("motion.shiftClose.coverCloseS", 0.3f);
        static float LeverS => Num("motion.shiftClose.leverS", 0.3f);
        static float RejectLeverS => Num("motion.shiftClose.rejectLeverS", 0.4f);
        static float AutoCloseS => Num("shiftClose.states.cover.autoCloseS", 4f);
        static float HoverLiftMm => Num("motion.shiftClose.hoverLiftMm", 3f);
        static float DenyMm => Num("motion.board.denyShakeMm", 2f);
        static float DenyS => Num("motion.board.denyShakeS", 0.18f);

        /// <summary>Kapağın ön kenarının mm kalkışı → kapak açısı (°): <c>atan(mm / coverRadiusM)</c>.</summary>
        float MmToDeg(float mm) => Mathf.Atan2(mm / 1000f, Mathf.Max(1e-4f, coverRadiusM)) * Mathf.Rad2Deg;

        static float Ease(Curve curve, float u)
        {
            u = Mathf.Clamp01(u);
            switch (curve)
            {
                case Curve.Out: return Out(u);
                case Curve.TwoStage:
                    if (u < DetentFrom) return 0.5f * Out(u / DetentFrom);
                    if (u < DetentTo) return 0.5f;
                    return 0.5f + 0.5f * InOut((u - DetentTo) / (1f - DetentTo));
                default: return InOut(u);
            }
        }

        /// <summary>Kübik çıkış.</summary>
        static float Out(float t)
        {
            float v = 1f - t;
            return 1f - v * v * v;
        }

        /// <summary>Kübik gir-çık.</summary>
        static float InOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
