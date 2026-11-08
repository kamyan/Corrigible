using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// DIŞ KAYIT aracı (talep defteri + GİDEN · TELSİZ ağzı; PLAN §1.7, §1.8, D11, D22): deftere tık ya da defteri GİDEN ağzına sürükle →
    /// <see cref="Used"/> (komutu sunucu yazar). <see cref="PlaySend"/>: <c>Sheet</c> görünümde +35° kalkar (<c>padTearS</c> 0,15; prefab
    /// aralığı 0 … −35° yaprağı bloğa gömer, görünüm işareti çevirir — D22), koparılır (<see cref="AdmissionSound.PadTear"/>), yaprak
    /// GİDEN ağzına gider (<c>slipS</c> 0,35; panel yereli (−0,26; 0,135; −0,046), normal (0; 0,26; −0,966), −2°, görünen 100 × 67 mm),
    /// karbon kopya defterde görünür. Gözetmensizken (araç kilitli) defter kalkmaz; üzerine gelme satırı kısa etiket ("gözetmen yok ·
    /// BOŞTA boş"). Yaprak GİDEN'deyken ona tık <see cref="PaperClicked"/>(Request), deftere tık (Carbon). Komut yazmaz, ses çalmaz.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RequestPadTool : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("RequestPad/Sheet (dönme x; görünümde 0 … +35).")]
        [SerializeField] PartDriver sheet;
        [Tooltip("Talep yaprağının kökü (Quad 100 × 133 + RequestFormFace; Sheet'in TXT_Sheet yüzey yuvasında).")]
        [SerializeField] Transform sheetPaper;
        [Tooltip("Karbon kopyanın kökü (defter gövdesinde, yaprağın altında; RequestFormFace karbon biçimi).")]
        [SerializeField] Transform carbonPaper;
        [Tooltip("AdmissionPanel model kökü (GİDEN pozu bunun çerçevesinde).")]
        [SerializeField] Transform panel;
        [Tooltip("Defterin çarpıştırıcısı.")]
        [SerializeField] Collider hit;
        [SerializeField] Camera view;

        /// <summary>Kalkış açısı (görünüm; prefab −35'in tersi).</summary>
        public const float LiftDeg = 35f;
        /// <summary>GİDEN'deki yaprağın görünen kısmının merkezi, normali, üstü, dönüşü (panel yereli; DA:167–168).</summary>
        public static readonly Vector3 OutgoingVisibleLocal = new Vector3(-0.26f, 0.135f, -0.046f);
        public static readonly Vector3 OutgoingNormal = new Vector3(0f, 0.26f, -0.966f);
        public static readonly Vector3 OutgoingUp = new Vector3(0f, 0.966f, 0.26f);
        public const float OutgoingRot = -2f;
        /// <summary>Yaprak 100 × 133 mm; GİDEN'de 67 mm'si görünür, üstü ağzın içinde.</summary>
        public static readonly Vector2 SheetSizeMm = new Vector2(100f, 133f);
        public const float OutgoingVisibleMm = 67f;
        /// <summary>GİDEN ağzının bırakma alanı (panel yereli z −0,036 düzleminde, merkez (−0,26; 0,17), 250 × 120 mm; ağız 250 × 34 + plaka).</summary>
        public static readonly Vector3 OutSlotLocal = new Vector3(-0.26f, 0.17f, -0.036f);
        public static readonly Vector2 OutSlotDropMm = new Vector2(250f, 120f);

        public static float TearS => BoardViewUtil.Num("admission.motion.padTearS", 0.15f);
        public static float FlyS => BoardViewUtil.Num("admission.motion.slipS", 0.35f);
        public static float HoverMm => BoardViewUtil.Num("motion.hoverLiftMm", 4f);
        public static float HoverS => BoardViewUtil.Num("motion.board.cardLiftS", 0.08f);
        public static float DenyMm => BoardViewUtil.Num("motion.board.denyShakeMm", 2f);
        public static float DenyS => BoardViewUtil.Num("motion.board.denyShakeS", 0.18f);

        public enum PadState { OnPad, Lifting, Flying, Outgoing }

        public event Action Used;
        public event Action<AdmissionSound> Sound;
        /// <summary>Okunabilir kâğıda tık: GİDEN'deki yaprak (<see cref="AdmissionPaper.Request"/>) ya da defterdeki karbon (<see cref="AdmissionPaper.Carbon"/>).</summary>
        public event Action<AdmissionPaper> PaperClicked;

        ToolData data;
        RequestData request;
        RequestFormFace sheetFace, carbonFace;
        Collider sheetHit;
        PadState state = PadState.OnPad;
        bool initialized, interactive, hovered, boundOnce, dragging;
        Vector3 sheetRestPos, carbonRestPos;
        Quaternion sheetRestRot, carbonRestRot;
        Pose from;
        Action pendingDone;
        readonly AdmissionClock clock = new AdmissionClock();
        readonly AdmissionTween lift = new AdmissionTween(0f), fly = new AdmissionTween(0f), hover = new AdmissionTween(0f);
        float shakeStart = -1f, appliedLift = float.NaN;
        Vector3 restLocalPos;
        Camera eventCamera;

        public PadState State => state;
        public bool Moving => clock.Pending || lift.Moving || fly.Moving;
        /// <summary>Sheet parçasının görünüm açısı (derece; + kalkık).</summary>
        public float SheetAngle => lift.Value;
        public bool Interactive => interactive;
        public Collider Hit => hit;
        public PartDriver Sheet => sheet;
        public Camera View { get => view; set => view = value; }
        /// <summary>GİDEN'deki yaprak (okuma pozu için; yaprak defterdeyse null).</summary>
        public Transform OutgoingPaper => state == PadState.Outgoing || state == PadState.Flying ? sheetPaper : null;
        /// <summary>Defterdeki karbon (yalnız gönderildikten sonra görünür).</summary>
        public Transform CarbonPaper => carbonPaper != null && carbonPaper.gameObject.activeSelf ? carbonPaper : null;
        public HoverTag Tag { get; set; }
        public string HoverTitle => data?.Title;
        public string Line => request != null && request.Sent && !string.IsNullOrEmpty(request.Warning) ? request.Warning : data?.Reason;

        public void Configure(PartDriver sheetPart, Transform sheetRoot, Transform carbonRoot, Transform panelModel, Collider collider, Camera camera)
        {
            sheet = sheetPart; sheetPaper = sheetRoot; carbonPaper = carbonRoot; panel = panelModel; hit = collider; view = camera;
            initialized = false;
            EnsureInit();
        }

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            restLocalPos = transform.localPosition;
            if (sheetPaper != null)
            {
                sheetRestPos = sheetPaper.localPosition;
                sheetRestRot = sheetPaper.localRotation;
                sheetFace = sheetPaper.GetComponent<RequestFormFace>();
                sheetHit = sheetPaper.GetComponent<Collider>();
            }
            if (carbonPaper != null)
            {
                carbonRestPos = carbonPaper.localPosition;
                carbonRestRot = carbonPaper.localRotation;
                carbonFace = carbonPaper.GetComponent<RequestFormFace>();
            }
            state = PadState.OnPad;
            lift.Snap(0f); fly.Snap(0f); hover.Snap(0f);
            appliedLift = float.NaN;
            ApplyPapers();
            ApplyCollider();
        }

        // ------------------------------------------------------------------ veri

        public void Bind(ToolData d)
        {
            EnsureInit();
            data = d;
        }

        /// <summary>Talep formu ve karbon (gözetmen adı canlı); gönderildiyse yaprak GİDEN'de görünür.</summary>
        public void Bind(RequestData d) => Bind(d, d != null && d.Sent);

        /// <summary>
        /// Talep formu ve karbon. <paramref name="outgoing"/>: yaprak GİDEN'de durur (gönderildi ve sonuç henüz gelmedi; sonuç gelince
        /// sunucu false verir, yaprak kalkar). İlk bağlamada anında oturur; sonra gönderim animasyonla (<see cref="PlaySend"/> aynı
        /// karede çağrılabilir), geri dönüş (yeni aday) anında.
        /// </summary>
        public void Bind(RequestData d, bool outgoing)
        {
            EnsureInit();
            bool first = !boundOnce;
            boundOnce = true;
            request = d;
            if (sheetFace != null && d != null) sheetFace.Bind(d, false);
            if (carbonFace != null && d != null) carbonFace.Bind(d, true);
            if (outgoing && (state == PadState.OnPad || state == PadState.Lifting))
            {
                if (first) SnapOutgoing();
                else if (state == PadState.OnPad) StartSend();
            }
            else if (!outgoing && state != PadState.OnPad && !(d != null && d.Sent && state != PadState.Outgoing)) SnapOnPad();
            ApplyPapers();
        }

        /// <summary>Yaprak GİDEN'e, karbon görünür (anında).</summary>
        public void SnapOutgoing()
        {
            EnsureInit();
            clock.Clear();
            state = PadState.Outgoing;
            lift.Snap(0f);
            fly.Snap(1f);
            ApplyPapers();
        }

        void SnapOnPad()
        {
            clock.Clear();
            Finish();
            state = PadState.OnPad;
            lift.Snap(0f);
            fly.Snap(0f);
            ApplyPapers();
        }

        /// <summary>Kalkış, koparma, GİDEN'e uçuş; bitince <paramref name="done"/>. Yaprak zaten GİDEN'deyse hemen.</summary>
        public void PlaySend(Action done)
        {
            EnsureInit();
            if (done != null) pendingDone += done;
            if (state == PadState.Outgoing) { Finish(); return; }
            if (state == PadState.OnPad) StartSend();
        }

        void StartSend()
        {
            state = PadState.Lifting;
            clock.Clear();
            float t0 = clock.Now;
            EndHover();
            lift.Go(LiftDeg, TearS, CardEase.EaseOut, t0);
            clock.At(t0 + TearS, at =>
            {
                if (state != PadState.Lifting) return;
                Sound?.Invoke(AdmissionSound.PadTear);
                lift.Snap(LiftDeg);   // koparma tam kalkık yapraktan (son kare atlanmaz)
                ApplyPapers();
                from = sheetPaper != null ? new Pose(sheetPaper.position, sheetPaper.rotation) : default;
                state = PadState.Flying;
                lift.Snap(0f);
                fly.Snap(0f);
                fly.Go(1f, FlyS, CardEase.EaseInOut, at);
            });
            clock.At(t0 + TearS + FlyS, at =>
            {
                if (state != PadState.Flying) return;
                state = PadState.Outgoing;
                Finish();
            });
            ApplyPapers();
        }

        void Finish()
        {
            var d = pendingDone;
            pendingDone = null;
            d?.Invoke();
        }

        public void SetInteractive(bool on)
        {
            EnsureInit();
            interactive = on;
            if (!on) { EndHover(); dragging = false; if (state == PadState.Lifting && !clock.Pending) lift.Snap(0f); }
            ApplyCollider();
        }

        void ApplyCollider()
        {
            if (hit != null && hit.enabled != interactive) hit.enabled = interactive;
            bool outOn = interactive && state == PadState.Outgoing;
            if (sheetHit != null && sheetHit.enabled != outOn) sheetHit.enabled = outOn;
        }

        // ------------------------------------------------------------------ pozlar

        /// <summary>GİDEN'deki yaprağın dünya pozu (yaprağın tam merkezi; üst 66 mm'si ağzın içinde).</summary>
        public Pose OutgoingPose
        {
            get
            {
                var rot = AdmissionStation.PaperRotation(OutgoingNormal, OutgoingUp, OutgoingRot);
                var center = OutgoingVisibleLocal + OutgoingUp.normalized * ((SheetSizeMm.y - OutgoingVisibleMm) * 0.5f / 1000f);
                return panel != null ? new Pose(panel.TransformPoint(center), panel.rotation * rot) : new Pose(center, rot);
            }
        }

        // ------------------------------------------------------------------ kare

        public void Advance(float dt)
        {
            EnsureInit();
            clock.Tick(dt);
            float now = clock.Now;
            lift.Tick(now); fly.Tick(now); hover.Tick(now);
            ApplyPapers();
            if (!clock.Pending && !lift.Moving && !fly.Moving && !hover.Moving && shakeStart < 0f) clock.Rebase();
        }

        void Update() => Advance(Time.unscaledDeltaTime);

        void ApplyPapers()
        {
            // Sheet: prefab aralığı dışına (+35) çıktığı için PartDriver.Set değil, parçanın pozu doğrudan (aralık kırpmasız)
            float a = lift.Value;
            if (sheet != null && appliedLift != a)
            {
                appliedLift = a;
                var axis = sheet.Axis == PartAxis.X ? Vector3.right : sheet.Axis == PartAxis.Y ? Vector3.up : Vector3.forward;
                sheet.transform.localPosition = sheet.PivotLocal;
                sheet.transform.localRotation = Quaternion.AngleAxis(a, axis);
            }
            if (sheetPaper != null)
            {
                // gönderilip sonucu gelen yaprak GİDEN'den kalkar (defterde yalnız karbon)
                bool show = request != null && !(request.Sent && state == PadState.OnPad);
                switch (state)
                {
                    case PadState.Flying:
                    {
                        var to = OutgoingPose;
                        float e = fly.Value;
                        Place(sheetPaper, new Pose(Vector3.LerpUnclamped(from.position, to.position, e), Quaternion.SlerpUnclamped(from.rotation, to.rotation, e)));
                        // ağza girerken üst kısım kaybolur (son yarıda; tasarımda ağızda yalnız alt parça görünür)
                        if (sheetFace != null) sheetFace.ShowBottom(Mathf.Lerp(SheetSizeMm.y, OutgoingVisibleMm, Mathf.Clamp01((e - 0.5f) * 2f)));
                        break;
                    }
                    case PadState.Outgoing:
                        Place(sheetPaper, OutgoingPose);
                        if (sheetFace != null) sheetFace.ShowBottom(OutgoingVisibleMm);
                        break;
                    default:
                        if (sheetFace != null) sheetFace.ShowBottom(SheetSizeMm.y);
                        if (sheetPaper.localPosition != sheetRestPos || sheetPaper.localRotation != sheetRestRot)
                            sheetPaper.SetLocalPositionAndRotation(sheetRestPos, sheetRestRot);
                        break;
                }
                SetActive(sheetPaper.gameObject, show);
            }
            if (carbonPaper != null)
            {
                bool carbon = request != null && (state == PadState.Flying || state == PadState.Outgoing || request.Sent);
                if (carbonPaper.localPosition != carbonRestPos || carbonPaper.localRotation != carbonRestRot) carbonPaper.SetLocalPositionAndRotation(carbonRestPos, carbonRestRot);
                SetActive(carbonPaper.gameObject, carbon);
            }
            // üzerine gelme ve ret titremesi: defter kökü
            var offset = Vector3.zero;
            if (hover.Value > 0f) offset += Vector3.up * (HoverMm / 1000f) * hover.Value;
            if (shakeStart >= 0f)
            {
                float u = DenyS > 0f ? (clock.Now - shakeStart) / DenyS : 1f;
                if (u >= 1f) shakeStart = -1f;
                else offset += Vector3.right * (DenyMm / 1000f * Mathf.Sin(3f * Mathf.PI * u));
            }
            var lp = restLocalPos + transform.localRotation * offset;
            if (transform.localPosition != lp) transform.localPosition = lp;
            ApplyCollider();
        }

        static void Place(Transform t, Pose p)
        {
            if (t.position != p.position || t.rotation != p.rotation) t.SetPositionAndRotation(p.position, p.rotation);
        }

        static void SetActive(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        // ------------------------------------------------------------------ işaretçi

        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();
        bool Enabled => data != null && data.Enabled;
        bool Usable(PointerEventData e) => e != null && e.button == PointerEventData.InputButton.Left && interactive && !Blocked;
        Camera RayCamera => view != null ? view : eventCamera != null ? eventCamera : Camera.main;

        /// <summary>Ekran noktasının ışını GİDEN ağzının bırakma alanına değiyor mu.</summary>
        public bool OverOutSlot(Vector2 screen)
        {
            var cam = RayCamera;
            if (cam == null || panel == null) return false;
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(panel.TransformDirection(Vector3.back), panel.TransformPoint(OutSlotLocal));
            if (!plane.Raycast(ray, out float d)) return false;
            var local = panel.InverseTransformPoint(ray.GetPoint(d)) - OutSlotLocal;
            return Mathf.Abs(local.x) * 1000f <= OutSlotDropMm.x * 0.5f && Mathf.Abs(local.y) * 1000f <= OutSlotDropMm.y * 0.5f;
        }

        void Deny()
        {
            shakeStart = clock.Now;
            Sound?.Invoke(AdmissionSound.Deny);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactive || Blocked || hovered) return;
            hovered = true;
            if (state == PadState.OnPad && Enabled) hover.Go(1f, HoverS, CardEase.Linear, clock.Now);
            if (Tag != null) Tag.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void EndHover()
        {
            if (hover.Target != 0f) hover.Go(0f, HoverS, CardEase.Linear, clock.Now);
            if (!hovered) return;
            hovered = false;
            if (Tag != null) Tag.Hide(this);
        }

        void OnDisable() => EndHover();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Usable(eventData) || eventData.dragging) return;
            var go = eventData.pointerCurrentRaycast.gameObject;
            if (state == PadState.Outgoing && sheetHit != null && go == sheetHit.gameObject) { PaperClicked?.Invoke(AdmissionPaper.Request); return; }
            if (state == PadState.Outgoing || (request != null && request.Sent)) { PaperClicked?.Invoke(AdmissionPaper.Carbon); return; }
            if (state != PadState.OnPad) return;
            if (!Enabled) { Deny(); return; }
            Used?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureInit();
            if (!Usable(eventData) || state != PadState.OnPad) return;
            if (eventData.pressEventCamera != null) eventCamera = eventData.pressEventCamera;
            if (!Enabled) { Deny(); return; }
            dragging = true;
            hover.Snap(0f);
            lift.Go(LiftDeg, TearS, CardEase.EaseOut, clock.Now);   // yaprak tutuldu: kalkar
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;
            bool accepted = eventData != null && OverOutSlot(eventData.position);
            if (state == PadState.OnPad) lift.Go(0f, TearS, CardEase.EaseIn, clock.Now);
            if (accepted) Used?.Invoke();
        }
    }
}
