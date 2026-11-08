using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// SOĞUK GÖZLEM aracı (kanattaki buğu camı, <c>DoorLeaf/Hinge</c> çocuğu; K8G; PLAN §1.12): tık → <see cref="Used"/> (komutu sunucu
    /// yazar) ve her tıkta <see cref="Clicked"/> (K8G'de cama tık geri götürür). <see cref="Play"/>: <c>Cover</c> 0 → 105°
    /// (<c>glassCoverS</c> 0,2; <see cref="AdmissionSound.GlassCover"/>), buğu dairesi alfa 0 → 1 (<c>fogInS</c> 0,6;
    /// <see cref="AdmissionSound.Fog"/>), tutmadan 1 → 0 ve ölçek 1 → 0,85 ("kenardan söner"; <c>fogOutS</c> 2,5; özel gölgelendirici yok,
    /// D9). Core sonucu "clear" ise (<c>fog</c> false) alfa hep 0, cam berrak kalır. <c>done</c> buğu tepesindeyken (berrakta kapak
    /// açılınca) çağrılır. <see cref="Close"/> kapağı 0,2 s'de kapatır. Prefab'ın <c>Fog</c> parçası (fogFrost) gizli kalır; buğu
    /// <c>Cold fog.mat</c>'lı daire Quad'dır (alfa MaterialPropertyBlock ile). Komut yazmaz, ses çalmaz.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColdGlassView : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("ColdGlass/Cover (dönme x 0 … 105).")]
        [SerializeField] PartDriver cover;
        [Tooltip("Buğu dairesi (Ø 108 mm Quad, Cold fog.mat, Transparent).")]
        [SerializeField] Renderer fog;
        [SerializeField] Collider hit;

        /// <summary>Sönerken buğunun vardığı ölçek ("kenardan söner").</summary>
        public const float FogEndScale = 0.85f;

        public static float CoverS => BoardViewUtil.Num("admission.motion.glassCoverS", 0.2f);
        public static float FogInS => BoardViewUtil.Num("admission.motion.fogInS", 0.6f);
        public static float FogOutS => BoardViewUtil.Num("admission.motion.fogOutS", 2.5f);

        public event Action Used;
        public event Action<AdmissionSound> Sound;
        /// <summary>Cama her sol tık (kullanılmış ya da kilitli olsa da; K8G'den çıkış).</summary>
        public event Action Clicked;

        ToolData data;
        bool initialized, interactive, hovered;
        readonly AdmissionClock clock = new AdmissionClock();
        readonly AdmissionTween open = new AdmissionTween(0f), alpha = new AdmissionTween(0f), scale = new AdmissionTween(1f);
        Vector3 fogBaseScale = Vector3.one;
        Color fogBaseColor = Color.white;
        MaterialPropertyBlock block;
        float appliedOpen = float.NaN, appliedAlpha = float.NaN, appliedScale = float.NaN;
        int gen;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>Kapağın açısı (derece) ve buğunun alfası (0 … 1), ölçeği.</summary>
        public float CoverAngle => open.Value;
        public float FogAlpha => alpha.Value;
        public float FogScale => scale.Value;
        public bool Playing => clock.Pending || open.Moving || alpha.Moving;
        public bool Interactive => interactive;
        public Collider Hit => hit;
        public PartDriver Cover => cover;
        public Renderer Fog => fog;
        public HoverTag Tag { get; set; }
        public string HoverTitle => data?.Title;
        public string Line => data?.Reason;
        float OpenDeg => cover != null && cover.RangeTo != 0 ? cover.RangeTo : 105f;

        public void Configure(PartDriver coverPart, Renderer fogRenderer, Collider collider)
        {
            cover = coverPart; fog = fogRenderer; hit = collider;
            initialized = false;
            EnsureInit();
        }

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            if (fog != null)
            {
                fogBaseScale = fog.transform.localScale;
                var m = fog.sharedMaterial;
                if (m != null && m.HasProperty(BaseColorId)) fogBaseColor = m.GetColor(BaseColorId);
            }
            open.Snap(0f); alpha.Snap(0f); scale.Snap(1f);
            appliedOpen = appliedAlpha = appliedScale = float.NaN;
            Apply();
            ApplyCollider();
        }

        public void Bind(ToolData d)
        {
            EnsureInit();
            data = d;
        }

        /// <summary>Kapağı açar ve buğuyu oynatır; <paramref name="fogSeen"/> false ise cam berrak kalır.</summary>
        public void Play(bool fogSeen, Action done)
        {
            EnsureInit();
            int g = ++gen;
            clock.Clear();
            float t0 = clock.Now;
            alpha.Snap(0f); scale.Snap(1f);
            open.Go(OpenDeg, CoverS, CardEase.EaseOut, t0);
            Sound?.Invoke(AdmissionSound.GlassCover);
            if (!fogSeen)
            {
                clock.At(t0 + CoverS, at => { if (g == gen) done?.Invoke(); });
                Apply();
                return;
            }
            clock.At(t0 + CoverS, at =>
            {
                if (g != gen) return;
                alpha.Go(1f, FogInS, CardEase.EaseOut, at);
                Sound?.Invoke(AdmissionSound.Fog);
            });
            clock.At(t0 + CoverS + FogInS, at =>
            {
                if (g != gen) return;
                // Tepe: belirme planlanmış zamanında tam 1'dir (adım, geçişin son karesindeki Tick'ten önce çalışır; aksi hâlde sönüş
                // bir önceki karenin 1'in altındaki değerinden başlardı). Tutma yok: sönüş 1'den hemen başlar (PLAN).
                alpha.Snap(1f);
                alpha.Go(0f, FogOutS, CardEase.EaseIn, at);
                scale.Go(FogEndScale, FogOutS, CardEase.EaseIn, at);
                done?.Invoke();
            });
            Apply();
        }

        /// <summary>Kapağı kapatır (K8G'den çıkış; 0,2 s). Süren buğu sönmeye devam eder.</summary>
        public void Close()
        {
            EnsureInit();
            if (open.Target == 0f && !open.Moving) return;
            open.Go(0f, CoverS, CardEase.EaseIn, clock.Now);
            Sound?.Invoke(AdmissionSound.GlassCover);
        }

        /// <summary>Anında kapalı ve berrak (kayıttan devam, dil değişimi).</summary>
        public void ResetClosed()
        {
            EnsureInit();
            gen++;
            clock.Clear();
            open.Snap(0f); alpha.Snap(0f); scale.Snap(1f);
            Apply();
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
            float now = clock.Now;
            open.Tick(now); alpha.Tick(now); scale.Tick(now);
            Apply();
            if (!clock.Pending && !open.Moving && !alpha.Moving && !scale.Moving) clock.Rebase();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        void Apply()
        {
            float o = open.Value;
            if (cover != null && appliedOpen != o) { appliedOpen = o; cover.Set(o); }
            if (fog == null) return;
            float a = alpha.Value, s = scale.Value;
            if (appliedAlpha != a)
            {
                appliedAlpha = a;
                bool visible = a > 0f;
                if (fog.enabled != visible) fog.enabled = visible;
                if (block == null) block = new MaterialPropertyBlock();
                fog.GetPropertyBlock(block);
                var c = fogBaseColor;
                c.a = fogBaseColor.a * a;
                block.SetColor(BaseColorId, c);
                fog.SetPropertyBlock(block);
            }
            if (appliedScale != s)
            {
                appliedScale = s;
                fog.transform.localScale = new Vector3(fogBaseScale.x * s, fogBaseScale.y * s, fogBaseScale.z);
            }
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
            Clicked?.Invoke();
            if (data == null || !data.Enabled || Playing) return;
            Used?.Invoke();
        }
    }
}
