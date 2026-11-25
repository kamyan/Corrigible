using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Olay tutanağının masadaki görünümü (KOS aşama 4, tasarım §7). Dışarıdan gelen kâğıt kapının yanındaki belge çekmecesinin tablasına
    /// gelir (<see cref="doorAnchor"/>, <see cref="PassDrawer"/>; Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A); okunup bırakılınca
    /// (Esc) yine tablaya döner, karar verilince tablaya konur (<see cref="Where.Outbox"/>) ve şalter çekilince gizlenir (<see cref="Send"/>).
    /// Çapası olmayan kâğıt eskisi gibi EVRAK tepsisinin dinlenme çapasına (<see cref="restAnchor"/>) iner. Tablada (çekmece açıkken,
    /// <see cref="DoorOpen"/>) ya da tepside tıklanınca ele gelir (yeni kamera pozu yok: el pozu her karede canlı kameradan,
    /// <see cref="ShiftReceiptPaper.HandPose(Vector3, Quaternion, float, float, float, Vector2, Vector3)"/>). Elde satıra tık
    /// <see cref="RowClicked"/>, KARAR kutusuna tık <see cref="ConfirmClicked"/>, kâğıdın başka yerine tık <see cref="PutBackClicked"/>.
    /// Yalnız çizer ve olay yayar: komut yazmaz, ses çalmaz (<see cref="Sound"/>), simülasyonu okumaz; durum
    /// <see cref="ShelterEventPaperData"/>'dan gelir.
    /// <list type="bullet">
    /// <item>Durumlar: <see cref="Where.Hidden"/>, <see cref="Where.Arriving"/> (tepsiye ya da kapı panosuna iniş), <see cref="Where.Tray"/>,
    /// <see cref="Where.Door"/> (çekmecenin tablasında bekliyor), <see cref="Where.Outbox"/> (karara bağlandı, tablada şalteri bekliyor),
    /// <see cref="Where.ToHand"/>, <see cref="Where.Hand"/>, <see cref="Where.Stamping"/> (karar izi elde), <see cref="Where.ToTray"/>
    /// (tepsiye ya da tablaya dönüş), <see cref="Where.Filing"/> (karardan sonra tablaya iniş; çapasızda tepsiye iniş ve gizlenme).</item>
    /// <item>Görsel gizleme kâğıdın Quad'ını ve yüz tuvalini kapatır, nesneyi kapatmaz (TMP ölçüsü uyanık yazı ister).</item>
    /// <item>Çarpıştırıcı yalnız veri varken, etkileşimliyken ve kâğıt tepside, kapı panosunda ya da eldeyken açık; kaydedilmiş sahnede
    /// kapalıdır.</item>
    /// <item>Tepside üzerine gelme etiketini tepsinin kendi hedefi verir (kâğıt tepsinin çocuğudur; işaretçi girişi ona da ulaşır);
    /// kapı panosunda kâğıt panonun çocuğu olur (tepsinin etiketi karışmasın) ve etiketi köprünün yazdığı <see cref="DoorTitle"/> ile
    /// <see cref="DoorLine"/>'dır; tepsiye dönerken yeniden tepsinin çocuğu olur. Elde etiket satırın adını ve bedelini ya da kapalı
    /// gerekçesini yazar.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShelterEventPaperView : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerClickHandler
    {
        public enum Where { Hidden, Arriving, Tray, ToHand, Hand, Stamping, ToTray, Filing, Door, Outbox }

        [SerializeField] ShelterEventPaperFace face;
        [Tooltip("Kâğıdın çarpıştırıcısı (kökte; kaydedilmiş sahnede kapalı).")]
        [SerializeField] Collider hitArea;
        [Tooltip("Kâğıdın Quad'ı (gizlenince kapanır).")]
        [SerializeField] Renderer paper;
        [Tooltip("Yüz tuvali (gizlenince kapanır; nesne açık kalır).")]
        [SerializeField] Canvas faceCanvas;
        [Tooltip("Tepsideki dinlenme çapası (EVRAK tepsisinin çocuğu).")]
        [SerializeField] Transform restAnchor;
        [Tooltip("Kapının yanındaki panoda dinlenme çapası: dışarıdan gelen kâğıt önce buraya iner. Boşsa kâğıt doğrudan tepsiye gelir.")]
        [SerializeField] Transform doorAnchor;
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
        /// <summary>Kâğıdın sahnedeki ebeveyni (tepsi); kapı panosundan dönünce yeniden ona bağlanır.</summary>
        Transform home, wantParent;
        bool homeKnown, parentPending;

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
        public Transform DoorAnchor { get => doorAnchor; set => doorAnchor = value; }
        /// <summary>Kâğıt çekmecenin tablasında okunmayı bekliyor ya da oraya iniyor (Esc'le dönüş dahil).</summary>
        public bool AtDoor => where == Where.Door || (where == Where.Arriving || where == Where.ToTray) && arriveAs == Where.Door;
        /// <summary>Kâğıt EVRAK tepsisinde ya da oraya iniyor (karar dosyalanışı sayılmaz).</summary>
        public bool InTray => where == Where.Tray || (where == Where.ToTray || where == Where.Arriving) && arriveAs == Where.Tray;
        /// <summary>Karara bağlanan kâğıt tablada (ya da oraya iniyor): şalter çekilince duvarın öbür yanına geçer.</summary>
        public bool InOutbox => where == Where.Outbox || where == Where.Filing && arriveAs == Where.Outbox;
        /// <summary>Çekmece tam açık: tabladaki kâğıt tıklanır (köprü her karede yazar; çekmecesiz sahnede hep true).</summary>
        public bool DoorOpen { get; set; } = true;
        /// <summary>Kapı panosundaki kâğıdın üzerine gelme başlığı ve satırı (köprü yazar; çevrilmiş metin).</summary>
        public string DoorTitle { get; set; }
        public string DoorLine { get; set; }
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
        public void Configure(ShelterEventPaperFace paperFace, Collider hit, Renderer quad, Canvas canvas, Transform rest, Camera camera, HoverTag tag,
            Transform door = null)
        {
            face = paperFace; hitArea = hit; paper = quad; faceCanvas = canvas; restAnchor = rest; view = camera; hoverTag = tag; doorAnchor = door;
        }

        void Awake()
        {
            if (where == Where.Hidden) SetVisuals(false);
            ApplyCollider();
        }

        // ------------------------------------------------------------------ pozlar

        public Pose RestPose => restAnchor != null ? new Pose(restAnchor.position, restAnchor.rotation) : new Pose(transform.position, transform.rotation);

        /// <summary>Kapı panosundaki dinlenme pozu (çapa yoksa tepsi pozu).</summary>
        public Pose DoorPose => doorAnchor != null ? new Pose(doorAnchor.position, doorAnchor.rotation) : RestPose;

        /// <summary>
        /// Kâğıt kapı panosunun (true) ya da tepsinin (false) çocuğu olur; dünya pozu korunur. Oyunda ebeveyn değişimi kâğıdın sonraki
        /// <c>Update</c>'ine kalır: köprüler <c>OnEnable</c>'da (oda baskından dönüp yeniden açılırken) kâğıdı bağlayabilir ve Unity etkinleşen
        /// bir hiyerarşide ebeveyn değişimini reddeder. Edit modda (testler) hemen uygulanır.
        /// </summary>
        void Attach(bool door)
        {
            if (!homeKnown) { home = transform.parent; homeKnown = true; }
            wantParent = door && doorAnchor != null ? doorAnchor.parent : home;
            parentPending = true;
            if (!Application.isPlaying) ApplyParent();
        }

        void ApplyParent()
        {
            if (!parentPending) return;
            parentPending = false;
            if (transform.parent != wantParent) transform.SetParent(wantParent, true);
        }

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
        /// Tutanağı uygular: null kâğıdı hemen gizler. Gizliyken gelen veri <paramref name="animate"/> ise dinlenme yerinin üstünden iner,
        /// değilse orada belirir; dinlenme yeri <paramref name="atDoor"/> ise kapı panosu (çapası yoksa tepsi), değilse tepsi. Görünür kâğıtta
        /// yalnız yüz yenilenir (yeri değişmez).
        /// </summary>
        public void Bind(ShelterEventPaperData d, bool animate, bool atDoor = false)
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
                bool door = atDoor && doorAnchor != null;
                Attach(door);
                var rest = door ? DoorPose : RestPose;
                var at = door ? Where.Door : Where.Tray;
                if (animate)
                {
                    var from = new Pose(rest.position + Vector3.up * DropFromM, rest.rotation);
                    Place(from);
                    SetWhere(Where.Arriving);
                    StartMove(from, Goal.Fixed, rest, DropS, CardEase.EaseOut, at);
                }
                else
                {
                    moving = false;
                    Place(rest);
                    SetWhere(at);
                }
            }
            ApplyCollider();
        }

        /// <summary>Kayıttan devam: veri animasyonsuz kapı panosunda (<paramref name="atDoor"/>) ya da tepside (null gizler).</summary>
        public void Restore(ShelterEventPaperData d, bool atDoor = false)
        {
            HideNow();
            Bind(d, false, atDoor);
        }

        /// <summary>Tepsideki, kapı panosundaki (ya da inen) kâğıt ele gelir; geldiyse true.</summary>
        public bool ToHand()
        {
            if (data == null || (where != Where.Tray && where != Where.Door && where != Where.Arriving && where != Where.ToTray)) return false;
            SetWhere(Where.ToHand);
            StartMove(Current, Goal.Hand, default, ToHandS, CardEase.EaseInOut, Where.Hand);
            Sound?.Invoke(ShelterEventSound.ToHand);
            return true;
        }

        /// <summary>Eldeki kâğıt çekmecenin tablasına döner (çapası yoksa EVRAK tepsisine); döndüyse true.</summary>
        public bool PutBack()
        {
            if (where != Where.Hand && where != Where.ToHand) return false;
            bool door = doorAnchor != null;
            Attach(door);
            SetWhere(Where.ToTray);
            StartMove(Current, Goal.Fixed, door ? DoorPose : RestPose, ToTrayS, CardEase.EaseInOut, door ? Where.Door : Where.Tray);
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

        /// <summary>
        /// Karara bağlanan kâğıt çekmecenin tablasına konur ve şalteri bekler (<see cref="Where.Outbox"/>); çapası yoksa tepsiye iner ve
        /// gizlenir (<see cref="FileAndHide"/>).
        /// </summary>
        public void FileToDrawer()
        {
            if (where == Where.Hidden) return;
            if (doorAnchor == null) { FileAndHide(); return; }
            Attach(true);
            SetWhere(Where.Filing);
            StartMove(Current, Goal.Fixed, DoorPose, ToTrayS, CardEase.EaseIn, Where.Outbox);
            Sound?.Invoke(ShelterEventSound.ToTray);
        }

        /// <summary>Tabladaki karara bağlı kâğıt duvarın öbür yanına geçti (şalter): hemen gizlenir. Gönderildiyse true.</summary>
        public bool Send()
        {
            if (!InOutbox) return false;
            HideNow();
            return true;
        }

        /// <summary>Karara bağlanan kâğıt tepsiye iner ve gizlenir (dosyalandı; çapasız sahne).</summary>
        public void FileAndHide()
        {
            if (where == Where.Hidden) return;
            Attach(false);
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
            if (homeKnown) Attach(false);
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
            bool on = Interactable && data != null && (where == Where.Tray || where == Where.Door && DoorOpen || where == Where.Hand);
            if (hitArea != null && hitArea.enabled != on) hitArea.enabled = on;
            if (!on || where != Where.Hand && where != Where.Door) EndHover();
        }

        // ------------------------------------------------------------------ kare

        void Update()
        {
            ApplyParent();
            Advance(Time.unscaledDeltaTime);
        }

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

        /// <summary>Elde kâğıttaki bir noktaya tık (sentetik testler; işaretçi olayının dalıyla aynı). Tepside ve kapı panosunda <see cref="Clicked"/>.</summary>
        public void ClickAt(Vector2 mm)
        {
            if (data == null || !Interactable) return;
            if (where == Where.Tray || where == Where.Door && DoorOpen) { Clicked?.Invoke(); return; }
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

        // ------------------------------------------------------------------ üzerine gelme (elde ve kapı panosunda)

        public string HoverTitle
        {
            get
            {
                if (data != null && where == Where.Door) return DoorTitle;
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
                if (data != null && where == Where.Door) return DoorLine;
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
            if (!Interactable || where != Where.Hand && where != Where.Door || eventData == null || face == null) { EndHover(); return; }
            var ray = eventData.pointerCurrentRaycast;
            if (ray.gameObject == null || hitArea == null || ray.gameObject != hitArea.gameObject) { EndHover(); return; }
            if (where == Where.Door)
            {
                // Panoda bütün kâğıt tek hedef: köprünün yazdığı başlık ve satır
                hoverRow = -1;
                hoverConfirm = false;
                if (string.IsNullOrEmpty(DoorTitle)) { EndHover(); return; }
                if (hovered) return;
                hovered = true;
                if (hoverTag != null) hoverTag.Show(this);
                return;
            }
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
