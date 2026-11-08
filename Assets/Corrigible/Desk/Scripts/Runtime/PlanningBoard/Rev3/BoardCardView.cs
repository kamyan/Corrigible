using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kartlı panodaki bir kart (Revizyon 3 · R15/R16): kişi, eşya, hammadde, fiş ya da (Aşama 5) tarif. Prefab kökündedir (<c>Prefabs/Rev3Board/*Card</c>):
    /// altında iç içe kart modeli (<c>Model</c>, geçersiz kılma yok), yüz tuvali (<c>Face</c>, <see cref="CardFace"/>) ve kişide ışık
    /// alan portre dörtgeni ile 3B İNCELE sözcüğü.
    /// <list type="bullet">
    /// <item><b>Kimlik:</b> havuzdan alınır, <c>Bind</c> ile Core kimliğine bağlanır (kişi <c>sakin-N</c>, eşya <c>esya-N</c>, hammadde
    /// türü, fiş <c>chip:tür:i</c>). Yüz yalnız değişen veride yeniden yazılır.</item>
    /// <item><b>Poz:</b> yerel konum ve z ekseni çevresinde açı (ebeveyn <c>Planning board/Kartlar</c>, model yereli). <see cref="Place"/>
    /// anında, <see cref="MoveTo"/> ve <see cref="ArcTo"/> o anki pozdan ara geçişle; kalkış (<see cref="SetLift"/>, kameraya doğru −z,
    /// düzlem içi eğim) ve ret sarsıntısı (<see cref="Shake"/>, yanal x) poza eklenir. Hepsi <c>Time.unscaledDeltaTime</c> ile ilerler.</item>
    /// <item><b>Girdi:</b> kutu çarpıştırıcısı (kart mm × 4 mm, önde) mevcut PhysicsRaycaster'a görünür; işaretçi olayları
    /// <see cref="Controller"/>'a aktarılır, kart kendisi karar vermez.</item>
    /// <item><b>Görünüm:</b> türün ışık alan yüz malzemesi model gövdesinin <c>cardFace</c> yuvasına <c>Awake</c>'te yazılır (iç içe
    /// prefab'da geçersiz kılma olmasın diye); eşya kartı yuvasına göre üç malzemeden birini alır. Modelin <c>Tape</c> parçası hep
    /// gizli (bant yüze çizilir), <c>Flag</c> yalnız İNCELE'de görünür, mıknatıs <see cref="SetMagnetVisible"/> ile.</item>
    /// </list>
    /// Sunum simülasyonu okumaz: veriler anlık görüntüden (<see cref="BoardSnapshot"/>) gelir, zaten çevrilmiştir.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardCardView : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] BoardCardKind kind;
        [SerializeField] Rev3Model model;
        [Tooltip("Model gövdesi (Body) ve yüz malzemesinin yuvası (cardFace).")]
        [SerializeField] MeshRenderer body;
        [SerializeField] int faceMaterialSlot = 1;
        [Tooltip("Kişi, hammadde ve fiş kartının ışık alan yüz malzemesi.")]
        [SerializeField] Material faceMaterial;
        [Tooltip("Eşya kartı: yuva sırasıyla kit, zırh, yardımcı (ItemSlotKind).")]
        [SerializeField] Material[] itemFaceMaterials = new Material[3];
        [SerializeField] CardFace face;
        [SerializeField] BoxCollider hit;
        [Tooltip("Modelin swap parçaları (fişte mıknatıs yok; bayrak ve bant yalnız kişide).")]
        [SerializeField] PartDriver magnet;
        [SerializeField] PartDriver flag, tape;
        [Tooltip("Kişi: 44 × 48,2 mm portre dörtgeni.")]
        [SerializeField] MeshRenderer portrait;
        [SerializeField] PortraitLibrary portraits;

        string id, hoverTitle, hoverLine, portraitKey;
        bool initialised, magnetWanted = true, magnetBlocked;
        Material appliedFace;

        // poz (ara geçişin çıktısı) ve hedefi
        Vector3 basePos, fromPos, targetPos;
        float baseAngle, fromAngle, targetAngle;
        bool tweening, arc, arcOwnsLift;
        float tweenElapsed, tweenSeconds, tweenDelay, arcLiftMm, arcLiftStart, arcTiltStart;
        CardEase tweenEase;

        // kalkış ve eğim (poza göre)
        float liftMm, tiltDeg, liftFrom, tiltFrom, liftTo, tiltTo, liftElapsed, liftSeconds;
        bool lifting;

        // ret sarsıntısı
        bool shaking;
        float shakeMm, shakeElapsed, shakeSeconds, shakeOffsetMm;

        static BoardWords localWords;
        static string localWordsLang;

        /// <summary>Bağlı Core kimliği (hammadde kartında tür).</summary>
        public string Id => id;
        public BoardCardKind Kind => kind;
        /// <summary>Sürükleme denetçisi (BoardView3D enjekte eder); işaretçi olayları ona gider, yoksa hiçbir şey olmaz.</summary>
        public BoardDragController Controller { get; set; }

        /// <summary>Portre kitaplığı (yalnız kişi kartı; BoardView3D enjekte eder).</summary>
        public PortraitLibrary Portraits
        {
            get => portraits;
            set { if (portraits != value) { portraits = value; portraitKey = null; } }
        }

        /// <summary>Kartların dile bağlı sözcükleri (anlık görüntünün <c>Words</c>'ü); verilmezse <see cref="DefaultWords"/>.</summary>
        public BoardWords Words { get; set; }

        /// <summary>Anlık görüntünün dili (EN değer hücreleri daha geniş); verilmezse oyunun dili.</summary>
        public bool? English { get; set; }

        /// <summary>Bir <see cref="MoveTo"/> ya da <see cref="ArcTo"/> sürüyor (bekleme dahil).</summary>
        public bool Moving => tweening;
        /// <summary>Süren ya da son ara geçişin bittiği konum (model yereli).</summary>
        public Vector3 Target => targetPos;
        public float TargetAngle => targetAngle;
        /// <summary>Şu anki kalkış (mm, kameraya doğru) ve düzlem içi eğim (derece).</summary>
        public float LiftMm => liftMm;
        public float TiltDeg => tiltDeg;
        public bool MagnetVisible => magnet != null && magnet.gameObject.activeSelf;

        public CardFace Face => face;
        public Rev3Model Model => model;
        public BoxCollider Collider => hit;
        public MeshRenderer Body => body;
        public MeshRenderer PortraitRenderer => portrait;

        public string HoverTitle => hoverTitle;
        public string Line => hoverLine;

        /// <summary>Bir <see cref="MoveTo"/>/<see cref="ArcTo"/> bittiğinde (son karesinde ya da Place/gizleme onu kestiğinde).</summary>
        public event Action<BoardCardView> Arrived;

        /// <summary>
        /// Karta işaretçi girdi ya da tıklandı (denetçiye aktarılmadan önce). Aşama 5 (R18d, D9-A): yeni ürünün YENİ etiketi ilk
        /// dokunuşta düşer; <see cref="BoardView3D"/> dinler. Komut yazmaz.
        /// </summary>
        public event Action<BoardCardView> Touched;

        bool IsEnglish => English ?? Corrigible.Core.Loc.Lang == Corrigible.Core.Loc.English;

        /// <summary>
        /// Sözcükler verilmediğinde kullanılan, oyunun diline çevrilmiş varsayılanlar (<see cref="BoardWords"/>'ün Türkçe kaynakları).
        /// </summary>
        public static BoardWords DefaultWords
        {
            get
            {
                string lang = Corrigible.Core.Loc.Lang;
                if (localWords != null && localWordsLang == lang) return localWords;
                var tr = new BoardWords();
                localWords = new BoardWords
                {
                    Injured = Corrigible.Core.Loc.T(tr.Injured), Bedridden = Corrigible.Core.Loc.T(tr.Bedridden),
                    Restricted = Corrigible.Core.Loc.T(tr.Restricted), Review = Corrigible.Core.Loc.T(tr.Review),
                    Lost = Corrigible.Core.Loc.T(tr.Lost), Free = Corrigible.Core.Loc.T(tr.Free),
                    Kit = Corrigible.Core.Loc.T(tr.Kit), Armor = Corrigible.Core.Loc.T(tr.Armor), Aid = Corrigible.Core.Loc.T(tr.Aid),
                };
                localWordsLang = lang;
                return localWords;
            }
        }

        void Awake() => EnsureInit();

        /// <summary>
        /// İlk kurulum (Awake'te; Awake çalışmamış kartta ilk çağrıda): poz dönüşümden okunur, bant ve bayrak gizlenir, yüz malzemesi
        /// gövdeye yazılır. Modelin swap parçaları prefab'da görünür gelir (C §8-12).
        /// </summary>
        void EnsureInit()
        {
            if (initialised) return;
            initialised = true;
            basePos = targetPos = transform.localPosition;
            baseAngle = targetAngle = SignedAngle(transform.localEulerAngles.z);
            if (tape != null) tape.SetVisible(false);
            if (flag != null) flag.SetVisible(false);
            ApplyMagnet();
            SetFaceMaterial(kind == BoardCardKind.Item ? ItemMaterial(ItemSlotKind.Kit) : faceMaterial);
        }

        // ------------------------------------------------------------------ bağlama

        public void Bind(PersonCardData d)
        {
            if (d == null || !Expect(BoardCardKind.Person)) return;
            EnsureInit();
            id = d.Id;
            hoverTitle = string.IsNullOrEmpty(d.HoverTitle) ? d.Name : d.HoverTitle;
            hoverLine = d.HoverLine;
            if (face is PersonCardFace pf)
            {
                pf.Bind(d, Words ?? DefaultWords, IsEnglish);
                // KISITLI ve KAYIP köşe şeridi mıknatısın yerini alır (R16a: köşeli kartta mıknatıs çizilmez; sözcüğü örtmesin)
                SetMagnetBlocked(pf.NameUsesMagnetSpace || d.Restricted || d.Lost);
            }
            BindPortrait(string.IsNullOrEmpty(d.PortraitKey) ? d.Name : d.PortraitKey);
        }

        public void Bind(ItemCardData d)
        {
            if (d == null || !Expect(BoardCardKind.Item)) return;
            EnsureInit();
            id = d.Id;
            hoverTitle = string.IsNullOrEmpty(d.HoverTitle) ? d.Name : d.HoverTitle;
            hoverLine = d.HoverLine;
            if (face is ItemCardFace f) f.Bind(d);
            SetFaceMaterial(ItemMaterial(d.Slot));
        }

        public void Bind(MaterialCardData d)
        {
            if (d == null || !Expect(BoardCardKind.Material)) return;
            EnsureInit();
            id = d.Kind;
            hoverTitle = string.IsNullOrEmpty(d.HoverTitle) ? d.Name : d.HoverTitle;
            hoverLine = d.HoverLine;
            if (face is MaterialCardFace f) f.Bind(d, (Words ?? DefaultWords).Free);
        }

        public void Bind(ChipData d)
        {
            if (d == null || !Expect(BoardCardKind.Chip)) return;
            EnsureInit();
            id = d.Id;
            hoverTitle = d.Name;
            hoverLine = null;
            if (face is ChipCardFace f) f.Bind(d);
        }

        /// <summary>
        /// Tarif kartı (Aşama 5 · R18b): defterden alınan kopya (kimlik = tarif) ya da iş emrinin TARİF kutusundaki kart (kimlik = iş
        /// emri). Üzerine gelme başlığı tarifin adı.
        /// </summary>
        public void Bind(RecipeCardData d)
        {
            if (d == null || !Expect(BoardCardKind.Recipe)) return;
            EnsureInit();
            id = d.Id;
            hoverTitle = d.Name;
            hoverLine = null;
            if (face is RecipeCardFace f) f.Bind(d);
        }

        /// <summary>Havuza dönen kart: kimlik ve yüz önbelleği silinir (sonraki bağlama her şeyi yazar).</summary>
        public void Unbind()
        {
            id = null; hoverTitle = null; hoverLine = null;
            if (face != null) face.Invalidate();
        }

        bool Expect(BoardCardKind k)
        {
            if (kind == k) return true;
            Debug.LogError("[BoardCardView] " + name + ": " + kind + " kartına " + k + " verisi bağlanamaz.", this);
            return false;
        }

        void BindPortrait(string key)
        {
            if (portrait == null || portraits == null || key == portraitKey) return;
            var m = portraits.For(key);
            if (m != null && portrait.sharedMaterial != m) portrait.sharedMaterial = m;
            portraitKey = key;
        }

        Material ItemMaterial(ItemSlotKind slot)
        {
            int i = (int)slot;
            return itemFaceMaterials != null && i >= 0 && i < itemFaceMaterials.Length ? itemFaceMaterials[i] : faceMaterial;
        }

        void SetFaceMaterial(Material m)
        {
            if (m == null || body == null || m == appliedFace) return;
            var mats = body.sharedMaterials;
            if (faceMaterialSlot < 0 || faceMaterialSlot >= mats.Length) return;
            appliedFace = m;
            if (mats[faceMaterialSlot] == m) return;
            mats[faceMaterialSlot] = m;
            body.sharedMaterials = mats;
        }

        // ------------------------------------------------------------------ poz

        /// <summary>Kartı anında koyar (model yereli, z çevresinde açı). Süren ara geçiş kesilir ve <see cref="Arrived"/> çağrılır.</summary>
        public void Place(Vector3 local, float angleDeg)
        {
            EnsureInit();
            bool was = tweening;
            StopTween();
            basePos = targetPos = local;
            baseAngle = targetAngle = angleDeg;
            Apply();
            if (was) Arrived?.Invoke(this);
        }

        /// <summary>
        /// O anki pozdan hedefe <paramref name="seconds"/> içinde, <paramref name="delay"/> bekledikten sonra (deste açılışı) gider.
        /// Süren ara geçişi yerine geçer (onun için <see cref="Arrived"/> çağrılmaz). Süre ve bekleme 0 ise anında koyar ve hemen bildirir.
        /// </summary>
        public void MoveTo(Vector3 local, float angleDeg, float seconds, CardEase ease, float delay = 0f)
        {
            EnsureInit();
            if (arcOwnsLift) ReleaseArcLift(Mathf.Max(0, seconds) + Mathf.Max(0, delay));
            if (seconds <= 0 && delay <= 0)
            {
                StopTween();
                basePos = targetPos = local;
                baseAngle = targetAngle = angleDeg;
                Apply();
                Arrived?.Invoke(this);
                return;
            }
            StartTween(local, angleDeg, seconds, ease, delay, false, 0);
        }

        /// <summary>
        /// Yay: düzlemde in-out, z'de <paramref name="liftMm"/>·sin(πt) şişkinlik; o anki kalkış ve eğim sona doğru 0'a iner (geçersiz
        /// bırakmada 10 mm, takasta 6 mm). Kalkış süreklidir: tepe = en büyük(o anki kalkış, <paramref name="liftMm"/>).
        /// </summary>
        public void ArcTo(Vector3 local, float angleDeg, float seconds, float liftMm)
        {
            EnsureInit();
            if (seconds <= 0)
            {
                lifting = false; arcOwnsLift = false;
                this.liftMm = 0; tiltDeg = 0;
                MoveTo(local, angleDeg, 0, CardEase.Linear);
                return;
            }
            StartTween(local, angleDeg, seconds, CardEase.EaseInOut, 0, true, Mathf.Max(0, liftMm));
            arcLiftStart = this.liftMm;
            arcTiltStart = tiltDeg;
            arcOwnsLift = true;
            lifting = false;
        }

        /// <summary>Poza göre kalkış (mm, kameraya doğru −z) ve düzlem içi eğim (derece); ease-out ile <paramref name="seconds"/> içinde.</summary>
        public void SetLift(float mm, float tiltDegrees, float seconds)
        {
            EnsureInit();
            arcOwnsLift = false;
            if (seconds <= 0)
            {
                lifting = false;
                liftMm = mm; tiltDeg = tiltDegrees;
                Apply();
                return;
            }
            liftFrom = liftMm; tiltFrom = tiltDeg; liftTo = mm; tiltTo = tiltDegrees;
            liftElapsed = 0; liftSeconds = seconds;
            lifting = true;
        }

        /// <summary>Kilitli planda ret: yanal x'te <paramref name="mm"/> genlikli üç yarım dalga, <paramref name="seconds"/> içinde.</summary>
        public void Shake(float mm, float seconds)
        {
            EnsureInit();
            if (seconds <= 0 || Mathf.Approximately(mm, 0)) return;
            shaking = true;
            shakeMm = mm; shakeElapsed = 0; shakeSeconds = seconds; shakeOffsetMm = 0;
        }

        void StartTween(Vector3 local, float angleDeg, float seconds, CardEase ease, float delay, bool isArc, float lift)
        {
            fromPos = basePos; fromAngle = baseAngle;
            targetPos = local; targetAngle = angleDeg;
            tweenSeconds = Mathf.Max(0, seconds); tweenDelay = Mathf.Max(0, delay); tweenElapsed = 0;
            tweenEase = ease;
            arc = isArc; arcLiftMm = lift;
            tweening = true;
        }

        void StopTween()
        {
            if (arcOwnsLift) { liftMm = 0; tiltDeg = 0; arcOwnsLift = false; }
            tweening = false;
            arc = false;
        }

        /// <summary>Yayın tuttuğu kalkış, yerine geçen hareketle birlikte 0'a iner.</summary>
        void ReleaseArcLift(float seconds)
        {
            arcOwnsLift = false;
            if (seconds <= 0) { lifting = false; liftMm = 0; tiltDeg = 0; return; }
            liftFrom = liftMm; tiltFrom = tiltDeg; liftTo = 0; tiltTo = 0;
            liftElapsed = 0; liftSeconds = seconds;
            lifting = true;
        }

        void Update()
        {
            if (!tweening && !lifting && !shaking) return;
            float dt = Time.unscaledDeltaTime;
            bool arrived = tweening && StepTween(dt);
            if (lifting) StepLift(dt);
            if (shaking) StepShake(dt);
            Apply();
            if (arrived) Arrived?.Invoke(this);
        }

        bool StepTween(float dt)
        {
            tweenElapsed += dt;
            float t = tweenElapsed < tweenDelay ? 0 : tweenSeconds > 0 ? Mathf.Clamp01((tweenElapsed - tweenDelay) / tweenSeconds) : 1;
            float e = CardEasing.Evaluate(tweenEase, t);
            basePos = Vector3.LerpUnclamped(fromPos, targetPos, e);
            baseAngle = Mathf.LerpAngle(fromAngle, targetAngle, e);
            if (arc && arcOwnsLift)
            {
                liftMm = Mathf.Max(arcLiftStart * (1 - e), arcLiftMm * Mathf.Sin(Mathf.PI * t));
                tiltDeg = arcTiltStart * (1 - e);
            }
            if (t < 1) return false;
            tweening = false;
            basePos = targetPos;
            baseAngle = targetAngle;
            if (arc && arcOwnsLift) { liftMm = 0; tiltDeg = 0; arcOwnsLift = false; }
            arc = false;
            return true;
        }

        void StepLift(float dt)
        {
            liftElapsed += dt;
            float t = liftSeconds > 0 ? Mathf.Clamp01(liftElapsed / liftSeconds) : 1;
            float e = CardEasing.Evaluate(CardEase.EaseOut, t);
            liftMm = Mathf.LerpUnclamped(liftFrom, liftTo, e);
            tiltDeg = Mathf.LerpUnclamped(tiltFrom, tiltTo, e);
            if (t >= 1) lifting = false;
        }

        void StepShake(float dt)
        {
            shakeElapsed += dt;
            float u = shakeSeconds > 0 ? shakeElapsed / shakeSeconds : 1;
            if (u >= 1) { shaking = false; shakeOffsetMm = 0; return; }
            shakeOffsetMm = shakeMm * Mathf.Sin(3 * Mathf.PI * u);
        }

        void Apply()
        {
            var p = basePos;
            p.x += shakeOffsetMm * 0.001f;
            p.z -= liftMm * 0.001f;
            var t = transform;
            if (t.localPosition != p) t.localPosition = p;
            var r = Quaternion.Euler(0, 0, baseAngle + tiltDeg);
            if (t.localRotation != r) t.localRotation = r;
        }

        static float SignedAngle(float deg)
        {
            deg %= 360f;
            return deg > 180f ? deg - 360f : deg < -180f ? deg + 360f : deg;
        }

        // ------------------------------------------------------------------ durum

        /// <summary>Çarpıştırıcı (K4'e varınca açık; sürüklenen kartta kapalı).</summary>
        public void SetInteractive(bool on)
        {
            EnsureInit();
            if (hit != null && hit.enabled != on) hit.enabled = on;
        }

        /// <summary>
        /// Kartı gösterir ya da gizler. Gizlenen kart dinlenme pozuna döner (süren hareket hedefinde biter, kalkış, eğim ve sarsıntı 0);
        /// kesilen bir ara geçiş için <see cref="Arrived"/> çağrılır. Havuzdaki kart böylece eski bir hareketi sürdürmez.
        /// </summary>
        public void SetVisible(bool on)
        {
            EnsureInit();
            if (!on)
            {
                bool was = tweening;
                if (tweening) { basePos = targetPos; baseAngle = targetAngle; }
                tweening = false; arc = false; arcOwnsLift = false;
                lifting = false; liftMm = 0; tiltDeg = 0;
                shaking = false; shakeOffsetMm = 0;
                Apply();
                if (gameObject.activeSelf) gameObject.SetActive(false);
                if (was) Arrived?.Invoke(this);
                return;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        /// <summary>Mıknatıs düğmesi: panoda görünür; arkaya sıkışan eşyada, elde ve dosyada gizli. Fişte mıknatıs yok.</summary>
        public void SetMagnetVisible(bool on)
        {
            magnetWanted = on;
            ApplyMagnet();
        }

        /// <summary>Ad mıknatıs alanına taşıyor (D2) ya da köşe şeridi var: mıknatıs, istenmiş olsa da gizli kalır.</summary>
        void SetMagnetBlocked(bool blocked)
        {
            if (magnetBlocked == blocked) return;
            magnetBlocked = blocked;
            ApplyMagnet();
        }

        void ApplyMagnet()
        {
            if (magnet != null) magnet.SetVisible(magnetWanted && !magnetBlocked);
        }

        // ------------------------------------------------------------------ işaretçi (denetçiye aktarılır)

        public void OnPointerEnter(PointerEventData eventData)
        {
            Touched?.Invoke(this);
            if (Controller != null) Controller.OnCardPointerEnter(this, eventData);
        }
        public void OnPointerExit(PointerEventData eventData) { if (Controller != null) Controller.OnCardPointerExit(this, eventData); }
        public void OnPointerDown(PointerEventData eventData) { if (Controller != null) Controller.OnCardPointerDown(this, eventData); }
        public void OnPointerUp(PointerEventData eventData) { if (Controller != null) Controller.OnCardPointerUp(this, eventData); }
        public void OnPointerClick(PointerEventData eventData)
        {
            Touched?.Invoke(this);
            if (Controller != null) Controller.OnCardClick(this, eventData);
        }
        public void OnBeginDrag(PointerEventData eventData) { if (Controller != null) Controller.OnCardBeginDrag(this, eventData); }
        public void OnDrag(PointerEventData eventData) { if (Controller != null) Controller.OnCardDrag(this, eventData); }
        public void OnEndDrag(PointerEventData eventData) { if (Controller != null) Controller.OnCardEndDrag(this, eventData); }
    }
}
