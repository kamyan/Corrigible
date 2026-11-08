using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Vardiya şalterinin ağzından çıkan devir fişi (gerçek dünya kâğıdı).
    /// <para>
    /// Revizyon 3 · R19b (devir fişi, PLAN §1.5–§1.6): kol inince ağızdan dile çıkar (<see cref="BeginFeed"/>,
    /// <c>motion.shiftClose.slipExitS</c>, ease-out; Karar D12: ağzın 40 mm içinden başlar, yol 160 mm), tıkla ele gelir
    /// (<see cref="TakeToHand"/>; kameranın önünde ekran yüksekliğinin %67'si, canlı kamerayı izler), tık / Esc / sağ tık dile
    /// döndürür (<see cref="PutBack"/>; tık ve tuşları köprü okur, kâğıt yalnız <see cref="Clicked"/> yayar), EVRAK üstüne
    /// bırakılınca tepsinin üst kâğıdına iner (<see cref="FileToTray"/>), EVRAK dışına ya da reddedilen bırakışta 0,25 s kavisle dile
    /// döner (<see cref="ReturnToTongue"/>; bulgu 17: hiçbir bırakış fişi askıda bırakmaz). Arşivdeki fiş elde salt okunur gösterilir
    /// (<see cref="ReadArchived"/>, Karar D7-A). Yüzü <see cref="HandoverSlipFace"/> yazar.
    /// </para>
    /// <para>
    /// Zaman <see cref="Advance"/> ile ilerler: oyunda <c>LateUpdate</c> gerçek zamanla (kamera rig'inden sonra, el pozu o karenin
    /// kamerasına oturur), EditMode testlerinde doğrudan. Kökün çerçevesi HandoverSlip modelinin çerçevesidir (yüz −z, üst +y).
    /// Sunum kararı vermez: bırakışın kabulüne <see cref="ShiftReceiptMachine"/>, tıkın anlamına köprü karar verir.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ShiftReceiptPaper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        /// <summary>Fişin yeri ve hareketi.</summary>
        public enum Where { Hidden, Feeding, Tongue, ToHand, Hand, Dragging, ToTray, Tray, Returning, Reading }

        /// <summary>Fişin görünüşü: <see cref="Instant"/> dilde anında (yükleme, dil, aynı kimlik), <see cref="AfterLever"/> kol inince çıkar.</summary>
        public enum Entry { Instant, AfterLever }

        enum Goal { Fixed, Tongue, Hand }

        [SerializeField] Transform output, dragSurface;
        [SerializeField] Collider hitArea;
        [SerializeField] TMP_Text heading, body;
        [SerializeField] Camera view;
        [Header("Revizyon 3 · R19 devir fişi")]
        [Tooltip("Fiş ağzının iç ucu (SlipMouth/SlipStart): çıkışın başı. Dil pozu output'tan (SlipExit).")]
        [SerializeField] Transform start;
        [SerializeField] HandoverSlipFace face;
        [SerializeField] EvrakTray tray;
        [Tooltip("Dil pozu: ağzın dış ucundan ofset (şalter yereli, m) ve dönüş (fiş üstü ağızdan uzakta, yüz yukarı).")]
        [SerializeField] Vector3 tongueOffset = new Vector3(0, -0.0044f, 0), tongueEuler = new Vector3(90, -90, 0);
        [Tooltip("Çıkışın başında fişin SlipStart'tan ağız içine doğru kalan payı (m; Karar D12, ilk karede kesik görünmez).")]
        [SerializeField] float feedHiddenM = 0.04f;
        [Tooltip("El pozu: fişin ekran yüksekliğindeki payı (R19b #3: %67).")]
        [SerializeField, Range(0.3f, 0.9f)] float handHeightFraction = 0.67f;
        [Tooltip("El pozu: kamera eksenlerinde ofset (m; sağ, yukarı). Kamera eksenleri bilinçli: el canlı kamerayı izler.")]
        [SerializeField] Vector2 handOffset = new Vector2(0.02f, -0.004f);
        [Tooltip("El pozu: kameraya göre eğim (derece; Karar D5: x 0°, z −2,9°).")]
        [SerializeField] Vector3 handEuler = new Vector3(0, 0, -2.9f);

        /// <summary>El pozunun varsayılan ofseti (kamera sağ/yukarı, m) ve eğimi (Karar D5).</summary>
        public static readonly Vector2 DefaultHandOffset = new Vector2(0.02f, -0.004f);
        public static readonly Vector3 DefaultHandEuler = new Vector3(0, 0, -2.9f);

        Where where = Where.Hidden;
        bool feedPending, readOnly, dragActive;
        HandoverSlipFacts liveFacts, archivedFacts;

        // hareket (o anki pozdan hedefe)
        bool moving;
        Goal goal;
        Pose moveFrom, moveTo;
        float moveElapsed, moveSeconds, moveArcM;
        CardEase moveEase;
        Where arriveAs;
        bool arriveSound;
        ShiftSound arriveCue;

        // sürükleme
        Vector2 dragScreen;
        Pose dragFrom;
        float dragElapsed;

        // arşiv okuma
        Pose readFrom;

        public string ReceiptId { get; private set; }
        /// <summary>Kullanıcı fişi sürüklüyor (bırakış kararı verilene dek <see cref="State"/> <see cref="Where.Dragging"/> kalır).</summary>
        public bool IsDragging => dragActive;
        /// <summary>Tık ve sürükleme alınır (köprü her karede yazar: dosyalama sürmüyor, masa kilitli değil).</summary>
        public bool Interactable { get; set; } = true;
        /// <summary>Sürükleme başladı (true) / bitti (false). Bitişte <see cref="Released"/>'tan önce gelir.</summary>
        public event Action<bool> DragChanged;
        /// <summary>Sürükleme bırakıldı (ekran konumu); dinleyen dosyalar ya da dile döndürür, dinleyen yoksa fiş dile döner.</summary>
        public event Action<Vector2> Released;
        /// <summary>R19: sürüklemesiz sol tık (ele al / dile döndür; anlamını köprü verir).</summary>
        public event Action Clicked;
        /// <summary>R19: sürüklerken işaretçinin ekran konumu (EVRAK hedef ışığı için).</summary>
        public event Action<Vector2> Dragged;
        /// <summary>R19: ses olayı (köprü <c>Sfx</c>'e çevirir).</summary>
        public event Action<ShiftSound> Sound;
        /// <summary>R19: <see cref="State"/> değişti (makine fiş ağzı lambasını buna göre yakar).</summary>
        public event Action<Where> StateChanged;

        /// <summary>Fişin yeri.</summary>
        public Where State => where;
        /// <summary>Arşivden okunan fiş (sürüklenmez, dosyalanmaz; <see cref="PutBack"/> tepsiye döndürüp gizler).</summary>
        public bool ReadOnly => readOnly;
        /// <summary>Basılan fiş kol aşağıya varmayı bekliyor (<see cref="Present(string, HandoverSlipFacts, Entry)"/> AfterLever).</summary>
        public bool FeedPending => feedPending;
        /// <summary>
        /// Canlı fiş masada ve EVRAK'a girmedi (ağızda, dilde, elde, sürüklenirken ya da dile dönerken): fiş ağzının lambası yanar.
        /// Bekleyen besleme, tepsiye iniş, tepside duruş ve arşiv okuma sayılmaz.
        /// </summary>
        public bool LiveSlipOut => !readOnly && !string.IsNullOrEmpty(ReceiptId)
            && where != Where.Hidden && where != Where.ToTray && where != Where.Tray;
        /// <summary>Yüzde yazan veri (arşiv okunurken arşivdeki fiş).</summary>
        public HandoverSlipFacts Facts => readOnly ? archivedFacts : liveFacts;
        public Camera View => view;

        /// <summary>Dil pozu (ağzın dış ucu + ofset; fiş üstü ağızdan uzakta, yüz yukarı).</summary>
        public Pose TongueTarget => TonguePose(output, tongueOffset, tongueEuler);
        /// <summary>El pozu o anki kameradan (kamera yoksa şimdiki poz).</summary>
        public Pose HandTarget => view != null
            ? HandPose(view.transform.position, view.transform.rotation, view.fieldOfView, handHeightFraction, SlipHeightM, handOffset, handEuler)
            : Current;
        /// <summary>
        /// Çıkışın başladığı nokta: SlipStart çapası + ağız içine <c>feedHiddenM</c> (Karar D12). SlipStart yoksa ağzın ekseninde
        /// (<c>output.right</c>) <c>shiftClose.slip.travelMm</c> + pay. Dönüş dil dönüşüdür.
        /// </summary>
        public Vector3 FeedFrom
        {
            get
            {
                if (output == null) return transform.position;
                var exit = output.position;
                var anchor = start != null ? start.position : exit + output.right * TravelM;
                var inward = anchor - exit;
                inward = inward.sqrMagnitude > 1e-10f ? inward.normalized : output.right;
                return anchor + inward * Mathf.Max(0, feedHiddenM);
            }
        }
        /// <summary>Çıkış yolunun uzunluğu (m; çıkış başından dil pozuna). D12 ile ≈ 0,16.</summary>
        public float FeedPathM => Vector3.Distance(FeedFrom, TongueTarget.position);

        Pose Current => new Pose(transform.position, transform.rotation);

        // ------------------------------------------------------------------ tema

        static float Num(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        static float ExitS => Num("motion.shiftClose.slipExitS", 0.35f);
        static float ToHandS => Num("motion.shiftClose.slipToHandS", 0.25f);
        static float ToTrayS => Num("motion.shiftClose.slipToTrayS", 0.2f);
        static float ReturnArcS => Num("motion.board.returnArcS", 0.25f);
        /// <summary>Dönüş kavsinin tepe yüksekliği: panonun geri kavsiyle aynı (<c>cards.heldLiftMm</c>; tasarımda kavis yok).</summary>
        static float ReturnArcM => Num("cards.heldLiftMm", 10f) * 0.001f;
        /// <summary>Sürükleme başında fişin EVRAK üstü dönüşüne geçişi (temada yok; yedek 0,1 s, PLAN §1.5).</summary>
        static float PickS => Num("motion.shiftClose.slipPickS", 0.1f);
        static float TravelM => Num("shiftClose.slip.travelMm", 120f) * 0.001f;
        static float SlipHeightM => Num("shiftClose.slip.sizeMm[1]", 150f) * 0.001f;

        // ------------------------------------------------------------------ kurulum

        public void Configure(Transform origin, Transform surface, Collider hit, TMP_Text title, TMP_Text text, Camera camera)
        { output = origin; dragSurface = surface; hitArea = hit; heading = title; body = text; view = camera; }

        /// <summary>R19 bağları (tek seferlik sahne script'i ve testler).</summary>
        public void ConfigureSlip(Transform startAnchor, HandoverSlipFace slipFace, EvrakTray inbox)
        { start = startAnchor; face = slipFace; tray = inbox; }

        /// <summary>Revizyon 2 kâğıdı: başlık ve gövde metniyle (veri yok, kol inince çıkar).</summary>
        public void Present(string id, string title, string text)
        {
            if (heading) heading.text = title;
            if (body) body.text = text;
            Present(id, null, Entry.AfterLever);
        }

        /// <summary>
        /// Fişi gösterir ya da (<paramref name="id"/> boşsa) gizler. Kimlik değiştiyse (ya da fiş gizlenmişse)
        /// <paramref name="entry"/>'ye göre kol inişini bekler ya da dilde belirir; aynı kimlik yalnız yüzü yeniler (dil değişimi,
        /// tazeleme) ve fişi bulunduğu yerde bırakır. Arşiv okunurken boş kimlik okumayı bozmaz.
        /// </summary>
        public void Present(string id, HandoverSlipFacts facts, Entry entry)
        {
            bool has = !string.IsNullOrEmpty(id);
            if (readOnly)
            {
                ReceiptId = id; liveFacts = facts;
                if (!has) return;
                EndReading();
                if (face) face.Bind(facts);
                ShowLive(entry);
                return;
            }
            bool changed = ReceiptId != id || (has && where == Where.Hidden && !feedPending);
            ReceiptId = id; liveFacts = facts;
            if (face) face.Bind(facts);
            if (!has) { HideNow(); return; }
            if (changed) ShowLive(entry);
        }

        void ShowLive(Entry entry)
        {
            CancelDrag();
            moving = false;
            if (entry == Entry.Instant)
            {
                feedPending = false;
                Show();
                Place(TongueTarget);
                SetWhere(Where.Tongue);
                return;
            }
            feedPending = true;
            SetWhere(Where.Hidden);
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        /// <summary>Kol aşağıya vardı: bekleyen fiş ağızdan dile çıkar (başta yazıcı, sonda kopma sesi). Bekleyen fiş yoksa yok sayılır.</summary>
        public void BeginFeed()
        {
            if (!feedPending || readOnly || string.IsNullOrEmpty(ReceiptId)) return;
            feedPending = false;
            Show();
            var to = TongueTarget;
            var from = new Pose(FeedFrom, to.rotation);
            Place(from);
            SetWhere(Where.Feeding);
            Emit(ShiftSound.SlipPrint);
            StartMove(from, Goal.Tongue, default, ExitS, CardEase.EaseOut, 0, Where.Tongue, true, ShiftSound.SlipTear);
        }

        /// <summary>
        /// Fiş ele gelir (0,25 s, ease-in-out; hedef her karede canlı kameradan, kamera K6'ya giderken izler). Çıkarken, dildeyken ya
        /// da dile dönerken geçerli; çıkış kesilirse kopma sesi gelir.
        /// </summary>
        public void TakeToHand()
        {
            if (readOnly || string.IsNullOrEmpty(ReceiptId) || view == null) return;
            if (where != Where.Feeding && where != Where.Tongue && where != Where.Returning) return;
            if (where == Where.Feeding) Emit(ShiftSound.SlipTear);
            var from = Current;
            SetWhere(Where.ToHand);
            Emit(ShiftSound.SlipLift);
            StartMove(from, Goal.Hand, default, ToHandS, CardEase.EaseInOut, 0, Where.Hand, false, default);
        }

        /// <summary>Eldeki fiş dile döner (0,25 s); arşivden okunuyorsa tepsiye döner ve gizlenir. Başka yerde yok sayılır.</summary>
        public void PutBack()
        {
            if (readOnly)
            {
                if (where != Where.Reading) return;
                var back = Current;
                SetWhere(Where.Returning);
                Emit(ShiftSound.SlipReturn);
                StartMove(back, Goal.Fixed, readFrom, ToHandS, CardEase.EaseInOut, 0, Where.Hidden, false, default);
                return;
            }
            if (where != Where.Hand && where != Where.ToHand) return;
            var from = Current;
            SetWhere(Where.Returning);
            Emit(ShiftSound.SlipReturn);
            StartMove(from, Goal.Tongue, default, ToHandS, CardEase.EaseInOut, 0, Where.Tongue, false, default);
        }

        /// <summary>Kabul edilen bırakış: fiş EVRAK tepsisinin üst kâğıdına iner (0,2 s, ease-out; varınca “şap”).</summary>
        public void FileToTray()
        {
            if (readOnly || string.IsNullOrEmpty(ReceiptId)) return;
            if (where == Where.Hidden || where == Where.ToTray || where == Where.Tray) return;
            FinishDragState();
            var from = Current;
            var to = tray != null ? tray.SlipDropPose : from;
            SetWhere(Where.ToTray);
            StartMove(from, Goal.Fixed, to, ToTrayS, CardEase.EaseOut, 0, Where.Tray, true, ShiftSound.SlipFile);
        }

        /// <summary>
        /// Reddedilen bırakış ya da dosyalama reddi (bulgu 17): fiş bulunduğu yerden dile döner (<c>motion.board.returnArcS</c>,
        /// ortada <c>cards.heldLiftMm</c> kavis). Gizlenmiş canlı fiş dilde belirir; çıkan fiş çıkışını bitirir; arşiv fişi tepsiye döner.
        /// </summary>
        public void ReturnToTongue()
        {
            if (readOnly) { PutBack(); return; }
            if (string.IsNullOrEmpty(ReceiptId)) return;
            switch (where)
            {
                case Where.Feeding:
                    return;
                case Where.Hidden:
                    if (feedPending) return;
                    Show(); Place(TongueTarget); SetWhere(Where.Tongue);
                    return;
                case Where.Tongue:
                    if (!moving) return;
                    break;
                case Where.Returning:
                    if (moving && goal == Goal.Tongue) return;
                    break;
            }
            FinishDragState();
            var from = Current;
            SetWhere(Where.Returning);
            Emit(ShiftSound.SlipReturn);
            StartMove(from, Goal.Tongue, default, ReturnArcS, CardEase.EaseInOut, ReturnArcM, Where.Tongue, false, default);
        }

        /// <summary>
        /// Fiş gizlenir (dosyalama başarısında tam karanlıkta): canlı fiş kâğıttan düşer (kimlik boşalır; Core fişi zaten tüketti),
        /// böylece EVRAK'tan arşiv okuması hemen açılabilir. Okunan arşiv fişi de kapanır, tepsideki kopya geri gelir.
        /// </summary>
        public void Hide()
        {
            bool wasReading = readOnly;
            readOnly = false;
            ReceiptId = null;
            liveFacts = null;
            HideNow();
            if (wasReading && tray) tray.SetFiledHidden(false);
        }

        /// <summary>
        /// Arşivdeki fişi <paramref name="from"/> pozundan (tepsideki kopya) ele getirir; salt okunur, sürüklenmez. Okunurken
        /// tepsideki kopya gizlenir. Canlı (basılı) fiş varken yok sayılır.
        /// </summary>
        public void ReadArchived(HandoverSlipFacts facts, Pose from)
        {
            if (facts == null || view == null || !string.IsNullOrEmpty(ReceiptId)) return;
            CancelDrag();
            feedPending = false;
            readOnly = true;
            archivedFacts = facts;
            readFrom = from;
            if (face) face.Bind(facts);
            if (tray) tray.SetFiledHidden(true);
            Show();
            Place(from);
            SetWhere(Where.Reading);
            Emit(ShiftSound.SlipLift);
            StartMove(from, Goal.Hand, default, ToHandS, CardEase.EaseInOut, 0, Where.Reading, false, default);
        }

        // ------------------------------------------------------------------ pozlar (saf)

        /// <summary>
        /// El pozu: fiş kameranın önünde, yüksekliği ekranın <paramref name="heightFraction"/> kesri olacak uzaklıkta
        /// (<c>d = slipHeightM / (heightFraction · 2 · tan(fov / 2))</c>; K6'da ≈ 0,29 m), kamera eksenlerinde hafif sağda ve
        /// aşağıda (<see cref="DefaultHandOffset"/>), yüzü kameraya dönük, düzlem içinde −2,9° (<see cref="DefaultHandEuler"/>).
        /// Saf: yalnız verilen kameradan hesaplanır.
        /// </summary>
        public static Pose HandPose(Vector3 camPos, Quaternion camRot, float fovDeg, float heightFraction = 0.67f, float slipHeightM = 0.15f)
            => HandPose(camPos, camRot, fovDeg, heightFraction, slipHeightM, DefaultHandOffset, DefaultHandEuler);

        /// <summary><see cref="HandPose(Vector3, Quaternion, float, float, float)"/>; ofset (m, kamera sağ/yukarı) ve eğim (derece) verilir.</summary>
        public static Pose HandPose(Vector3 camPos, Quaternion camRot, float fovDeg, float heightFraction, float slipHeightM,
            Vector2 offsetM, Vector3 tiltEuler)
        {
            float d = slipHeightM / (Mathf.Max(0.01f, heightFraction) * 2 * Mathf.Tan(Mathf.Clamp(fovDeg, 1, 179) * 0.5f * Mathf.Deg2Rad));
            // Kökün +z'si kameranın ileri ekseni: fişin yüzü (−z) kameraya bakar, üstü (+y) kameranın yukarısı.
            return new Pose(camPos + camRot * new Vector3(offsetM.x, offsetM.y, d), camRot * Quaternion.Euler(tiltEuler));
        }

        /// <summary>Dil pozu: ağzın dış ucu + <paramref name="offset"/> (ağız yereli), dönüş ağzın dönüşü · <paramref name="euler"/>.</summary>
        public static Pose TonguePose(Transform output, Vector3 offset, Vector3 euler)
        {
            if (output == null) return Pose.identity;
            return new Pose(output.position + output.rotation * offset, output.rotation * Quaternion.Euler(euler));
        }

        // ------------------------------------------------------------------ zaman

        void LateUpdate() => Advance(Time.unscaledDeltaTime);

        /// <summary>Fiş hareketini <paramref name="dt"/> saniye ilerletir (oyunda <c>LateUpdate</c> gerçek zamanla; EditMode testleri doğrudan).</summary>
        public void Advance(float dt)
        {
            if (dt < 0) dt = 0;
            if (moving) { StepMove(dt); return; }
            if (where == Where.Dragging) { StepDrag(dt); return; }
            if (where == Where.Hand || where == Where.Reading) Place(HandTarget);
        }

        void StartMove(Pose from, Goal g, Pose fixedTo, float seconds, CardEase ease, float arcM, Where arrive, bool sound, ShiftSound cue)
        {
            moving = true;
            goal = g;
            moveFrom = from;
            moveTo = fixedTo;
            moveElapsed = 0;
            moveSeconds = Mathf.Max(0, seconds);
            moveEase = ease;
            moveArcM = arcM;
            arriveAs = arrive;
            arriveSound = sound;
            arriveCue = cue;
            if (moveSeconds <= 0) StepMove(0);
        }

        Pose GoalPose => goal == Goal.Tongue ? TongueTarget : goal == Goal.Hand ? HandTarget : moveTo;

        void StepMove(float dt)
        {
            moveElapsed += dt;
            float t = moveSeconds > 0 ? Mathf.Clamp01(moveElapsed / moveSeconds) : 1;
            var to = GoalPose;
            if (t >= 1)
            {
                moving = false;
                Place(to);
                Arrive();
                return;
            }
            float e = CardEasing.Evaluate(moveEase, t);
            var p = Vector3.LerpUnclamped(moveFrom.position, to.position, e) + Vector3.up * (moveArcM * Mathf.Sin(Mathf.PI * t));
            Place(new Pose(p, Quaternion.SlerpUnclamped(moveFrom.rotation, to.rotation, e)));
        }

        void Arrive()
        {
            var arrived = arriveAs;
            bool sound = arriveSound;
            var cue = arriveCue;
            if (readOnly && arrived == Where.Hidden) { FinishReading(); return; }
            SetWhere(arrived);
            if (sound) Emit(cue);
        }

        void StepDrag(float dt)
        {
            dragElapsed += dt;
            var to = DragPose(dragScreen, out bool hit);
            if (!hit) return;
            float t = PickS > 0 ? Mathf.Clamp01(dragElapsed / PickS) : 1;
            float e = CardEasing.Evaluate(CardEase.EaseOut, t);
            Place(new Pose(Vector3.LerpUnclamped(dragFrom.position, to.position, e), Quaternion.SlerpUnclamped(dragFrom.rotation, to.rotation, e)));
        }

        /// <summary>Sürükleme pozu: işaretçi ışını ∩ EVRAK üstü çapasından (<c>Slip over</c>) geçen yatay düzlem; dönüş çapanınki.</summary>
        Pose DragPose(Vector2 screen, out bool hit)
        {
            hit = false;
            Pose anchor = dragSurface != null ? new Pose(dragSurface.position, dragSurface.rotation)
                : tray != null ? tray.SlipOverPose : Current;
            if (view == null) return Current;
            var plane = new Plane(Vector3.up, anchor.position);
            var ray = view.ScreenPointToRay(screen);
            if (!plane.Raycast(ray, out float distance)) return Current;
            hit = true;
            return new Pose(ray.GetPoint(distance), anchor.rotation);
        }

        // ------------------------------------------------------------------ durum yardımcıları

        void Place(Pose p)
        {
            var t = transform;
            if (t.position != p.position || t.rotation != p.rotation) t.SetPositionAndRotation(p.position, p.rotation);
        }

        void Show() { if (!gameObject.activeSelf) gameObject.SetActive(true); }

        void HideNow()
        {
            CancelDrag();
            moving = false;
            feedPending = false;
            SetWhere(Where.Hidden);
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        void SetWhere(Where w)
        {
            if (where == w) return;
            where = w;
            StateChanged?.Invoke(w);
        }

        void Emit(ShiftSound s) => Sound?.Invoke(s);

        void EndReading()
        {
            readOnly = false;
            moving = false;
            if (tray) tray.SetFiledHidden(false);
        }

        void FinishReading()
        {
            EndReading();
            if (face) face.Bind(liveFacts);
            if (!string.IsNullOrEmpty(ReceiptId) && !feedPending) { Place(TongueTarget); SetWhere(Where.Tongue); return; }
            SetWhere(Where.Hidden);
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        /// <summary>Sürükleme bitti ama bırakış kararı henüz verilmedi (Dragging durumu): poz o anki hâliyle kalır, hareket başlar.</summary>
        void FinishDragState()
        {
            if (dragActive) CancelDrag();
        }

        /// <summary>Süren sürüklemeyi bırakış olayı olmadan bitirir (gizleme, yeniden sunma, devre dışı kalma).</summary>
        void CancelDrag()
        {
            if (!dragActive) return;
            dragActive = false;
            if (hitArea) hitArea.enabled = true;
            DragChanged?.Invoke(false);
        }

        // ------------------------------------------------------------------ girdi

        bool CanDrag(PointerEventData e)
        {
            if (!Interactable || readOnly || e == null || e.button != PointerEventData.InputButton.Left || string.IsNullOrEmpty(ReceiptId)) return false;
            return where == Where.Feeding || where == Where.Tongue || where == Where.ToHand || where == Where.Hand || where == Where.Returning;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!CanDrag(e)) return;
            if (where == Where.Feeding) Emit(ShiftSound.SlipTear);
            moving = false;
            dragActive = true;
            dragFrom = Current;
            dragElapsed = 0;
            dragScreen = e.position;
            SetWhere(Where.Dragging);
            if (hitArea) hitArea.enabled = false;
            DragChanged?.Invoke(true);
            Dragged?.Invoke(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragActive || e == null) return;
            dragScreen = e.position;
            Dragged?.Invoke(e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragActive) return;
            if (e != null) dragScreen = e.position;
            dragActive = false;
            if (hitArea) hitArea.enabled = true;
            // Sıra bilerek: önce DragChanged(false) (köprünün sürükleme bayrağı iner, CanFile masa kilidini görmez), sonra Released.
            DragChanged?.Invoke(false);
            Released?.Invoke(dragScreen);
            if (where == Where.Dragging) ReturnToTongue();   // kimse karar vermediyse fiş askıda kalmaz (bulgu 17)
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!Interactable || e == null || e.button != PointerEventData.InputButton.Left || e.dragging) return;
            if (where == Where.Hidden || where == Where.Dragging || where == Where.ToTray || where == Where.Tray) return;
            Clicked?.Invoke();
        }

        /// <summary>Eski adıyla: fiş dil pozuna anında oturur (sürükleme ve hareket kesilir).</summary>
        public void ReturnToOutput()
        {
            if (readOnly) return;
            CancelDrag();
            moving = false;
            Place(TongueTarget);
            if (!string.IsNullOrEmpty(ReceiptId) && where != Where.Hidden) SetWhere(Where.Tongue);
        }

        void OnDisable()
        {
            bool wasDragging = dragActive;
            CancelDrag();
            if (hitArea) hitArea.enabled = true;
            if (wasDragging || where == Where.Dragging) { moving = false; Place(TongueTarget); SetWhere(Where.Tongue); }
        }
    }
}
