using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// GÖRÜŞME aracı (kapıdaki iç interkom; PLAN §1.7): tık → <see cref="Used"/> (komutu sunucu yazar). <see cref="Play(Action)"/>:
    /// <c>Button</c> 0 → 4 mm basılır (<c>motion.board.cardLiftS</c> 0,08), <c>Led</c> amber (hat açık; <c>DoorProp.SetIntercom</c> ile aynı
    /// malzemeler), cızırtı <c>intercomCrackleS</c> 0,2 (<see cref="AdmissionSound.IntercomCrackle"/>), sonra düğme bırakılır ve
    /// <c>done</c>. Hat, verilen süre boyunca açık kalır (<see cref="Play(float, Action)"/>: cızırtı + döküm <c>len × 0,03</c> s).
    /// Komut yazmaz, ses çalmaz; edit modda <see cref="Advance"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IntercomTool : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("IntercomBox/Button (kayma z 0 … 0,004).")]
        [SerializeField] PartDriver button;
        [Tooltip("IntercomBox/Led renderer'ı; açık/sönük malzemeler DoorProp'unkilerle aynı.")]
        [SerializeField] Renderer led;
        [SerializeField] Material ledOn, ledOff;
        [SerializeField] Collider hit;

        public static float CrackleS => BoardViewUtil.Num("admission.motion.intercomCrackleS", 0.2f);
        public static float PressS => BoardViewUtil.Num("motion.board.cardLiftS", 0.08f);
        public static float PerCharS => BoardViewUtil.Num("admission.motion.transcriptPerCharS", 0.03f);

        /// <summary>Görüşme dökümünün süresi: harf sayısı × <c>transcriptPerCharS</c> (145 harf → 4,35 s).</summary>
        public static float TranscriptSeconds(string body) => (body ?? "").Length * PerCharS;

        public event Action Used;
        public event Action<AdmissionSound> Sound;

        ToolData data;
        bool initialized, interactive, hovered, lineOpen;
        readonly AdmissionClock clock = new AdmissionClock();
        readonly AdmissionTween press = new AdmissionTween(0f);
        float appliedPress = float.NaN;
        int gen;

        public bool LineOpen => lineOpen;
        public bool Playing => clock.Pending || press.Moving;
        /// <summary>Düğmenin basılma kayması (m; 0 … 0,004).</summary>
        public float ButtonOffset => press.Value;
        public bool Interactive => interactive;
        public Collider Hit => hit;
        public HoverTag Tag { get; set; }
        public string HoverTitle => data?.Title;
        public string Line => data?.Reason;
        float PressedValue => button != null && button.RangeTo != 0 ? button.RangeTo : 0.004f;

        public void Configure(PartDriver buttonPart, Renderer ledRenderer, Material on, Material off, Collider collider)
        {
            button = buttonPart; led = ledRenderer; ledOn = on; ledOff = off; hit = collider;
            initialized = false;
            EnsureInit();
        }

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            press.Snap(0f);
            appliedPress = float.NaN;
            ApplyCollider();
        }

        public void Bind(ToolData d)
        {
            EnsureInit();
            data = d;
        }

        /// <summary>Basış, hat ve cızırtı; hat yalnız cızırtı boyunca açık.</summary>
        public void Play(Action done) => Play(CrackleS, done);

        /// <summary>
        /// Basış, cızırtı ve hat: hat <paramref name="lineSeconds"/> boyunca açık (en az cızırtı kadar), <paramref name="done"/> cızırtı
        /// bitince (fiş ve döküm sunucunun).
        /// </summary>
        public void Play(float lineSeconds, Action done)
        {
            EnsureInit();
            int g = ++gen;
            clock.Clear();
            float t0 = clock.Now;
            press.Go(PressedValue, PressS, CardEase.EaseOut, t0);
            SetLine(true);
            Sound?.Invoke(AdmissionSound.IntercomCrackle);
            clock.At(t0 + CrackleS, at =>
            {
                if (g != gen) return;
                press.Go(0f, PressS, CardEase.EaseIn, at);
                done?.Invoke();
            });
            clock.At(t0 + Mathf.Max(CrackleS, lineSeconds), at => { if (g == gen) SetLine(false); });
            Apply();
        }

        /// <summary>Hattı hemen kapatır, düğmeyi bırakır (dil değişimi, kayıttan devam).</summary>
        public void Stop()
        {
            EnsureInit();
            gen++;
            clock.Clear();
            press.Snap(0f);
            SetLine(false);
            Apply();
        }

        void SetLine(bool on)
        {
            lineOpen = on;
            if (led == null) return;
            var m = on ? ledOn : ledOff;
            if (m != null && led.sharedMaterial != m) led.sharedMaterial = m;
        }

        public void SetInteractive(bool on)
        {
            EnsureInit();
            interactive = on;
            if (!on) EndHover();
            ApplyCollider();
        }

        void ApplyCollider()
        {
            if (hit != null && hit.enabled != interactive) hit.enabled = interactive;
        }

        public void Advance(float dt)
        {
            EnsureInit();
            clock.Tick(dt);
            press.Tick(clock.Now);
            Apply();
            if (!clock.Pending && !press.Moving) clock.Rebase();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        void Apply()
        {
            float v = press.Value;
            if (button != null && appliedPress != v) { appliedPress = v; button.Set(v); }
        }

        // ------------------------------------------------------------------ işaretçi

        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactive || Blocked || hovered) return;
            hovered = true;
            if (Tag != null) Tag.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void EndHover()
        {
            if (!hovered) return;
            hovered = false;
            if (Tag != null) Tag.Hide(this);
        }

        void OnDisable() => EndHover();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !interactive || Blocked) return;
            if (data == null || !data.Enabled) { Sound?.Invoke(AdmissionSound.Deny); return; }
            if (Playing) return;
            Used?.Invoke();
        }
    }
}
