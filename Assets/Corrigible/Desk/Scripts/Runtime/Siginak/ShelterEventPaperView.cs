using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Olay tutanağının masadaki görünümü (KOS aşama 4, tasarım §7; soru 8: EVRAK tepsisinde). Kâğıt tepsinin dinlenme çapasında
    /// (<see cref="restAnchor"/>) durur; tıklanınca ele gelir (yeni kamera pozu yok: el pozu her karede canlı kameradan,
    /// <see cref="ShiftReceiptPaper.HandPose(Vector3, Quaternion, float, float, float, Vector2, Vector3)"/>). Elde satıra tık
    /// <see cref="RowClicked"/>, KARAR kutusuna tık <see cref="ConfirmClicked"/>, kâğıdın başka yerine tık <see cref="PutBackClicked"/>.
    /// Yalnız çizer ve olay yayar: komut yazmaz, ses çalmaz (<see cref="Sound"/>), simülasyonu okumaz; durum
    /// <see cref="ShelterEventPaperData"/>'dan gelir.
    /// <list type="bullet">
    /// <item>Durumlar: <see cref="Where.Hidden"/>, <see cref="Where.Arriving"/> (tepsinin üstünden düşüş), <see cref="Where.Tray"/>,
    /// <see cref="Where.ToHand"/>, <see cref="Where.Hand"/>, <see cref="Where.Stamping"/> (karar izi elde), <see cref="Where.ToTray"/>
    /// (tepsiye dönüş), <see cref="Where.Filing"/> (karardan sonra tepsiye iniş ve gizlenme).</item>
    /// <item>Görsel gizleme kâğıdın Quad'ını ve yüz tuvalini kapatır, nesneyi kapatmaz (TMP ölçüsü uyanık yazı ister).</item>
    /// <item>Çarpıştırıcı yalnız veri varken, etkileşimliyken ve kâğıt tepside ya da eldeyken açık; kaydedilmiş sahnede kapalıdır.</item>
    /// <item>Tepside üzerine gelme etiketini tepsinin kendi hedefi verir (kâğıt tepsinin çocuğudur; işaretçi girişi ona da ulaşır);
    /// elde etiket satırın adını ve bedelini ya da kapalı gerekçesini yazar.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShelterEventPaperView : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerClickHandler
    {
        public enum Where { Hidden, Arriving, Tray, ToHand, Hand, Stamping, ToTray, Filing }

        [SerializeField] ShelterEventPaperFace face;
        [Tooltip("Kâğıdın çarpıştırıcısı (kökte; kaydedilmiş sahnede kapalı).")]
        [SerializeField] Collider hitArea;
        [Tooltip("Kâğıdın Quad'ı (gizlenince kapanır).")]
        [SerializeField] Renderer paper;
        [Tooltip("Yüz tuvali (gizlenince kapanır; nesne açık kalır).")]
        [SerializeField] Canvas faceCanvas;
        [Tooltip("Tepsideki dinlenme çapası (EVRAK tepsisinin çocuğu).")]
        [SerializeField] Transform restAnchor;
        [Tooltip("El pozunun kamerası (canlı).")]
        [SerializeField] Camera view;
        [SerializeField] HoverTag hoverTag;
        [Tooltip("El pozu: kamera eksenlerinde ofset (m; sağ, yukarı) ve eğim (derece).")]
        [SerializeField] Vector2 handOffset = new Vector2(0f, -0.004f);
        [SerializeField] Vector3 handEuler = new Vector3(0, 0, -1.5f);

        enum Goal { Fixed, Hand }

        Where where = Where.Hidden;
        ShelterEventPaperData data;
        bool moving, hovered;
        Goal goal;
        Pose moveFrom, moveTo;
        float moveElapsed, moveSeconds;
        CardEase moveEase;
        Where arriveAs;
        int hoverRow = -1;
        bool hoverConfirm;
        GameObject pressGo;

        /// <summary>Tepsideki kâğıda tıklandı (köprü ele alır).</summary>
        public event Action Clicked;
        /// <summary>Elde bir satıra tıklandı (dizin).</summary>
        public event Action<int> RowClicked;
        /// <summary>Elde KARAR kutusuna tıklandı.</summary>
        public event Action ConfirmClicked;
        /// <summary>Elde kâğıdın satır ve kutu dışına tıklandı (tepsiye dön).</summary>
        public event Action PutBackClicked;
        public event Action<ShelterEventSound> Sound;

        public Where State => where;
        /// <summary>Kâğıt eldedir ya da ele/tepsiye hareket ediyor: masa kilitli.</summary>
        public bool Busy => where == Where.ToHand || where == Where.Hand || where == Where.Stamping || where == Where.ToTray || where == Where.Filing;
        /// <summary>Kâğıt elde (ya da ele geliyor, iz basılıyor).</summary>
        public bool InHand => where == Where.ToHand || where == Where.Hand || where == Where.Stamping;
        /// <summary>Kâğıt görünüyor.</summary>
        public bool Visible => where != Where.Hidden;
        public bool Moving => moving;
        /// <summary>Tık ve üzerine gelme alınır (köprü her karede yazar: masanın öteki kilitleri yok).</summary>
        public bool Interactable { get; set; } = true;
        public ShelterEventPaperData Data => data;
        public ShelterEventPaperFace Face => face;
        public Collider HitArea => hitArea;
        public Transform RestAnchor => restAnchor;
        public Camera View { get => view; set => view = value; }
        public HoverTag Tag { get => hoverTag; set => hoverTag = value; }

        // ------------------------------------------------------------------ tema

        static float Num(string key, float fallback) => BoardViewUtil.Num(key, fallback);
        static float DropS => Num("shelterEvent.motion.dropS", 0.35f);
        static float ToHandS => Num("shelterEvent.motion.toHandS", 0.3f);
        static float ToTrayS => Num("shelterEvent.motion.toTrayS", 0.3f);
        static float DropFromM => Num("shelterEvent.tray.dropFromM", 0.08f);
        static float HandHeightFraction => Num("shelterEvent.hand.heightFraction", 0.8f);
        static float PaperHeightM => Num("shelterEvent.paper.heightMm", ShelterEventPaperFace.HeightMm) * 0.001f;

        // ------------------------------------------------------------------ kurulum

        /// <summary>Bağlar (tek seferlik sahne script'i ve testler).</summary>
        public void Configure(ShelterEventPaperFace paperFace, Collider hit, Renderer quad, Canvas canvas, Transform rest, Camera camera, HoverTag tag)
        {
            face = paperFace; hitArea = hit; paper = quad; faceCanvas = canvas; restAnchor = rest; view = camera; hoverTag = tag;
        }

        void Awake()
        {
            if (where == Where.Hidden) SetVisuals(false);
            ApplyCollider();
        }

        // ------------------------------------------------------------------ pozlar

        public Pose RestPose => restAnchor != null ? new Pose(restAnchor.position, restAnchor.rotation) : new Pose(transform.position, transform.rotation);

        /// <summary>
        /// El pozu o anki kameradan (kamera yoksa tepsi pozu). Fiziksel kameranın lens kaydırması (K1'de dikey −0,324) görüntünün
        /// merkezini taşır: kâğıt o uzaklıkta kaydırılmış merkeze oturur (yeni kamera pozu yok; K1 ve K6'dan aynı görünür).
        /// </summary>
        public Pose HandTarget
        {
            get
            {
                if (view == null) return RestPose;
                var cam = view.transform;
                float frac = HandHeightFraction, height = PaperHeightM;
                var pose = ShiftReceiptPaper.HandPose(cam.position, cam.rotation, view.fieldOfView, frac, height, handOffset, handEuler);
                if (!view.usePhysicalProperties || view.lensShift == Vector2.zero) return pose;
                float d = height / (Mathf.Max(0.01f, frac) * 2 * Mathf.Tan(Mathf.Clamp(view.fieldOfView, 1, 179) * 0.5f * Mathf.Deg2Rad));
                float frustumH = 2f * d * Mathf.Tan(Mathf.Clamp(view.fieldOfView, 1, 179) * 0.5f * Mathf.Deg2Rad);
                var shift = view.lensShift;
                pose.position += cam.rotation * new Vector3(shift.x * frustumH * view.aspect, shift.y * frustumH, 0f);
                return pose;
            }
        }

        Pose Current => new Pose(transform.position, transform.rotation);

        void Place(Pose p)
        {
            if (transform.position != p.position || transform.rotation != p.rotation) transform.SetPositionAndRotation(p.position, p.rotation);
        }

        void StartMove(Pose from, Goal to, Pose fixedTo, float seconds, CardEase ease, Where arrive)
        {
            moveFrom = from; goal = to; moveTo = fixedTo; moveSeconds = Mathf.Max(0f, seconds); moveEase = ease; arriveAs = arrive;
            moveElapsed = 0f;
            moving = true;
            if (moveSeconds <= 0f) Advance(0f);
        }

        // ------------------------------------------------------------------ bağlama

        /// <summary>
        /// Tutanağı uygular: null kâğıdı hemen gizler. Gizliyken gelen veri <paramref name="animate"/> ise tepsinin üstünden düşer, değilse
        /// tepside belirir; görünür kâğıtta yalnız yüz yenilenir.
        /// </summary>
        public void Bind(ShelterEventPaperData d, bool animate)
        {
            if (d == null) { HideNow(); return; }
            data = d;
            if (face != null)
            {
                face.Bind(d);
                face.ShowStamp(d.StampLines != null, false);
            }
            if (where == Where.Hidden)
            {
                SetVisuals(true);
                if (animate)
                {
                    var rest = RestPose;
                    var from = new Pose(rest.position + Vector3.up * DropFromM, rest.rotation);
                    Place(from);
                    SetWhere(Where.Arriving);
                    StartMove(from, Goal.Fixed, rest, DropS, CardEase.EaseOut, Where.Tray);
                }
                else
                {
                    moving = false;
                    Place(RestPose);
                    SetWhere(Where.Tray);
                }
            }
            ApplyCollider();
        }

        /// <summary>Kayıttan devam: veri animasyonsuz tepside (null gizler).</summary>
        public void Restore(ShelterEventPaperData d)
        {
            HideNow();
            Bind(d, false);
        }

        /// <summary>Tepsideki (ya da düşen) kâğıt ele gelir; geldiyse true.</summary>
        public bool ToHand()
        {
            if (data == null || (where != Where.Tray && where != Where.Arriving && where != Where.ToTray)) return false;
            SetWhere(Where.ToHand);
            StartMove(Current, Goal.Hand, default, ToHandS, CardEase.EaseInOut, Where.Hand);
            Sound?.Invoke(ShelterEventSound.ToHand);
            return true;
        }

        /// <summary>Eldeki kâğıt tepsiye döner; döndüyse true.</summary>
        public bool PutBack()
        {
            if (where != Where.Hand && where != Where.ToHand) return false;
            SetWhere(Where.ToTray);
            StartMove(Current, Goal.Fixed, RestPose, ToTrayS, CardEase.EaseInOut, Where.Tray);
            Sound?.Invoke(ShelterEventSound.ToTray);
            return true;
        }

        /// <summary>Karar izi elde basılır (<paramref name="lines"/>: "KARARA BAĞLANDI", seçenek adı).</summary>
        public void Stamp(string[] lines)
        {
            if (data == null) return;
            data.StampLines = lines;
            if (face != null) { face.Bind(data); face.ShowStamp(lines != null, true); }
            if (InHand) { moving = moving && where == Where.ToHand; SetWhere(Where.Stamping); if (!moving) Place(HandTarget); }
            ApplyCollider();
        }

        /// <summary>Karara bağlanan kâğıt tepsiye iner ve gizlenir (dosyalandı).</summary>
        public void FileAndHide()
        {
            if (where == Where.Hidden) return;
            SetWhere(Where.Filing);
            StartMove(Current, Goal.Fixed, RestPose, ToTrayS, CardEase.EaseIn, Where.Hidden);
            Sound?.Invoke(ShelterEventSound.ToTray);
        }

        /// <summary>Kâğıt hemen gizlenir (olay kalktı, yükleme).</summary>
        public void HideNow()
        {
            moving = false;
            data = null;
            if (face != null) { face.ShowStamp(false, false); face.Bind(null); }
            SetWhere(Where.Hidden);
            SetVisuals(false);
            EndHover();
            ApplyCollider();
        }

        void SetWhere(Where w)
        {
            where = w;
            if (!InHand) hoverRow = -1;
        }

        void SetVisuals(bool on)
        {
            if (paper != null && paper.enabled != on) paper.enabled = on;
            if (faceCanvas != null && faceCanvas.enabled != on) faceCanvas.enabled = on;
        }

        void ApplyCollider()
        {
            bool on = Interactable && data != null && (where == Where.Tray || where == Where.Hand);
            if (hitArea != null && hitArea.enabled != on) hitArea.enabled = on;
            if (!on || where != Where.Hand) EndHover();
        }

        // ------------------------------------------------------------------ kare

        void Update() => Advance(Time.unscaledDeltaTime);

        /// <summary>Hareketi ilerletir ve eldeki kâğıdı canlı kameraya oturtur (edit modda testler sürer).</summary>
        public void Advance(float dt)
        {
            if (face != null && !Application.isPlaying) face.Advance(dt);
            if (moving)
            {
                moveElapsed += Mathf.Max(0f, dt);
                float u = moveSeconds > 0f ? Mathf.Clamp01(moveElapsed / moveSeconds) : 1f;
                var to = goal == Goal.Hand ? HandTarget : moveTo;
                if (u >= 1f)
                {
                    moving = false;
                    Place(to);
                    Arrive();
                }
                else
                {
                    float e = CardEasing.Evaluate(moveEase, u);
                    Place(new Pose(Vector3.LerpUnclamped(moveFrom.position, to.position, e), Quaternion.SlerpUnclamped(moveFrom.rotation, to.rotation, e)));
                }
            }
            else if (where == Where.Hand || where == Where.Stamping) Place(HandTarget);
            ApplyCollider();
        }

        void Arrive()
        {
            if (arriveAs == Where.Hidden) { HideNow(); return; }
            // ToHand sırasında iz basıldıysa kâğıt elde izle kalır
            if (where == Where.Stamping) return;
            SetWhere(arriveAs);
        }

        // ------------------------------------------------------------------ tık eşlemesi

        /// <summary>Dünya noktasının kâğıttaki mm'si (sol üst köken, x sağa, y aşağı; kök kâğıdın ortası).</summary>
        public Vector2 PaperMm(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            return new Vector2(local.x * 1000f + ShelterEventPaperFace.WidthMm / 2f, ShelterEventPaperFace.HeightMm / 2f - local.y * 1000f);
        }

        /// <summary>Elde kâğıttaki bir noktaya tık (sentetik testler; işaretçi olayının dalıyla aynı). Tepside <see cref="Clicked"/>.</summary>
        public void ClickAt(Vector2 mm)
        {
            if (data == null || !Interactable) return;
            if (where == Where.Tray) { Clicked?.Invoke(); return; }
            if (where != Where.Hand) return;
            int row = face != null ? face.RowAt(mm) : -1;
            if (row >= 0) { RowClicked?.Invoke(row); return; }
            if (face != null && face.ConfirmContains(mm)) { ConfirmClicked?.Invoke(); return; }
            PutBackClicked?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressGo = eventData != null ? eventData.pointerPressRaycast.gameObject : null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !Interactable) return;
            var ray = eventData.pointerCurrentRaycast;
            if (ray.gameObject == null || hitArea == null || ray.gameObject != hitArea.gameObject || (pressGo != null && ray.gameObject != pressGo)) return;
            ClickAt(PaperMm(ray.worldPosition));
        }

        // ------------------------------------------------------------------ üzerine gelme (yalnız elde)

        public string HoverTitle
        {
            get
            {
                if (data == null || where != Where.Hand) return null;
                var row = HoverRowData;
                if (row != null) return row.Name;
                return hoverConfirm ? data.Title : null;
            }
        }

        public string Line
        {
            get
            {
                if (data == null || where != Where.Hand) return null;
                var row = HoverRowData;
                if (row != null) return row.Enabled ? row.Cost : row.Closed ?? data.Note;
                return hoverConfirm ? data.Confirm : null;
            }
        }

        ShelterEventRowData HoverRowData => data != null && hoverRow >= 0 && hoverRow < data.Rows.Count ? data.Rows[hoverRow] : null;

        public void OnPointerEnter(PointerEventData eventData) => UpdateHover(eventData);

        public void OnPointerMove(PointerEventData eventData) => UpdateHover(eventData);

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void UpdateHover(PointerEventData eventData)
        {
            if (!Interactable || where != Where.Hand || eventData == null || face == null) { EndHover(); return; }
            var ray = eventData.pointerCurrentRaycast;
            if (ray.gameObject == null || hitArea == null || ray.gameObject != hitArea.gameObject) { EndHover(); return; }
            var mm = PaperMm(ray.worldPosition);
            hoverRow = face.RowAt(mm);
            hoverConfirm = hoverRow < 0 && face.ConfirmContains(mm);
            if (hoverRow < 0 && !hoverConfirm) { EndHover(); return; }
            if (hovered) return;
            hovered = true;
            if (hoverTag != null) hoverTag.Show(this);
        }

        void EndHover()
        {
            hoverRow = -1;
            hoverConfirm = false;
            if (!hovered) return;
            hovered = false;
            if (hoverTag != null) hoverTag.Hide(this);
        }

        void OnDisable() => EndHover();
    }
}
