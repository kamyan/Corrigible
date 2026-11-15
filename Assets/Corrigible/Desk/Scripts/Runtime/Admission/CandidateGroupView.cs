using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// Aday grubunun tezgâhtaki görünümü (KOS aşama 2, tasarım §7): 2–3 aday kartı yan yana ve altlarında grup fişi. Kartlar tezgâh
    /// kanadının (Top) yerelinde durur; gevşek araçlar gibi kanat 30°'nin altına inince görünür (geliş animasyonu yok). Yalnız çizer ve
    /// olay yayar: komut yazmaz, ses çalmaz (<see cref="Sound"/>), simülasyonu okumaz; durum <see cref="CandidateGroupData"/>'dan gelir.
    /// <list type="bullet">
    /// <item>Tık: karta → <see cref="CardClicked"/> (okuma), fişin bir satırına → <see cref="ReplaceClicked"/> (yollanacak kişi), fişin
    /// geri kalanına → <see cref="SlipClicked"/>. Elde damga varken tık damgaya gider (<see cref="HeldStamp"/>).</item>
    /// <item>Damga: KABUL bir kartın üstüne (<see cref="AcceptStamp"/> hedef kartı saklar; <see cref="StampCandidateId"/>), GERİ ÇEVİR
    /// fişin üstüne. Basış pozu <see cref="StampPose"/>: hedefin iz merkezi, yüzeyin 0,2 mm üstü.</item>
    /// <item>Karar dizisi: <see cref="Stow"/> bütün kâğıtları evrak çekmecesine kaldırır (<c>docDropS</c>), <see cref="HideAll"/> gizler.</item>
    /// </list>
    /// Kendi <c>Update</c>'i yoktur: tek saat istasyondadır (<see cref="AdmissionStation"/> her karede <see cref="Advance"/> çağırır).
    /// Yerleşim ölçüleri temadan (<c>admission.candidates.*</c>). Çarpıştırıcılar yalnız etkileşimliyken ve kâğıt görünürken açık.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CandidateGroupView : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerClickHandler
    {
        public const int MaxCards = 3;
        /// <summary>Kartların tezgâhtaki dönüşü (derece; sıra dizinine göre).</summary>
        public static readonly float[] CardRot = { -2f, 1f, -1.5f };
        public const float SlipRot = 1f;
        /// <summary>Kâğıtların tezgâh yüzeyinden yüksekliği (Top yereli; belgeyle aynı).</summary>
        public const float SurfaceY = 0.0043f;
        /// <summary>Çekmecede üst üste binen kâğıtların yükseklik adımı (m).</summary>
        public const float StowStepM = 0.0006f;
        /// <summary>Damga bırakılırken kâğıt düzlemine en çok uzaklık (mm).</summary>
        public const float DropHeightMm = 30f;

        [SerializeField] HoverTag hoverTag;
        [Tooltip("Tezgâh kanadı (AdmissionCounter/Top): kartlar bunun yerelinde durur.")]
        [SerializeField] Transform frame;
        [Tooltip("Evrak çekmecesi (AdmissionPanel/Drawer): karar dizisinde kâğıtlar buraya kalkar.")]
        [SerializeField] Transform stowFrame;
        [SerializeField] Transform[] cardPapers = new Transform[MaxCards];
        [SerializeField] Transform slipPaper;

        /// <summary>Bir karta tıklandı (okuma); dizin kartın sırası.</summary>
        public event Action<int> CardClicked;
        /// <summary>Grup fişine (satır dışına) tıklandı (okuma).</summary>
        public event Action SlipClicked;
        /// <summary>YER YOK listesinin bir satırına tıklandı: kişi kimliği.</summary>
        public event Action<string> ReplaceClicked;
        public event Action<AdmissionSound> Sound;

        enum Part { None, Card, Slip }
        enum Target { None, Card, Slip }

        readonly AdmissionTween stowT = new AdmissionTween(1f);
        readonly Pose[] stowFrom = new Pose[MaxCards + 1];
        CandidateCardFace[] cardFaces = new CandidateCardFace[0];
        Collider[] cardHits = new Collider[0];
        CandidateGroupSlipFace slipFace;
        Collider slipHit;
        CandidateGroupData data;
        string key;
        bool initialized, shown, interactive, stowing, hovered;
        float now;
        Target target;
        int targetCard = -1, hoverCard = -1, hoverRow = -1;
        Part hoverPart;
        GameObject pressGo;

        public HoverTag Tag { get => hoverTag; set => hoverTag = value; }
        /// <summary>İstasyon verir: elde damga varken tık ona gider.</summary>
        public Func<StampDecision> HeldStamp { get; set; }
        /// <summary>Son kabul edilen KABUL hedefinin kart kimliği; yoksa null.</summary>
        public string StampCandidateId => target == Target.Card && data != null && targetCard >= 0 && targetCard < CardCount ? data.Cards[targetCard].Id : null;
        /// <summary>Kâğıtlar çekmeceye kalkıyor.</summary>
        public bool Moving => stowing && stowT.Moving;
        /// <summary>Kâğıtlar tezgâhta görünüyor (veri var ve kanat inik).</summary>
        public bool Shown => data != null && shown;
        public int CardCount => data != null ? Mathf.Min(data.Cards.Count, cardPapers.Length) : 0;
        public CandidateGroupData Data => data;
        public bool Interactive => interactive;
        public IReadOnlyList<Transform> CardPapers => cardPapers;
        public Transform SlipPaper => slipPaper;
        public CandidateGroupSlipFace SlipFace { get { EnsureInit(); return slipFace; } }

        public CandidateCardFace CardFace(int i)
        {
            EnsureInit();
            return i >= 0 && i < cardFaces.Length ? cardFaces[i] : null;
        }

        public string HoverTitle
        {
            get
            {
                if (data == null) return null;
                if (hoverPart == Part.Card) return hoverCard >= 0 && hoverCard < data.Cards.Count ? data.Cards[hoverCard].Name : null;
                if (hoverPart != Part.Slip) return null;
                var row = HoverRow;
                return row != null ? row.Name : data.SlipHoverTitle;
            }
        }

        public string Line
        {
            get
            {
                if (data == null) return null;
                if (hoverPart == Part.Card) return hoverCard >= 0 && hoverCard < data.Cards.Count ? data.Cards[hoverCard].HoverLine : null;
                if (hoverPart != Part.Slip) return null;
                var row = HoverRow;
                return row != null ? (row.Enabled ? data.RowHoverLine : row.Reason) : data.SlipHoverLine;
            }
        }

        ReplaceRowData HoverRow => data != null && hoverRow >= 0 && hoverRow < data.Replace.Count ? data.Replace[hoverRow] : null;
        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();

        // ------------------------------------------------------------------ token

        public static float CardScale => Mathf.Max(0.1f, BoardViewUtil.Num("admission.candidates.cardScale", 1f));
        public static float GapMm => BoardViewUtil.Num("admission.candidates.gapMm", 8f);
        public static float RowCenterX => BoardViewUtil.Num("admission.candidates.rowCenterX", -0.075f);
        public static float RowZ => BoardViewUtil.Num("admission.candidates.rowZ", -0.122f);
        public static float SlipX => BoardViewUtil.Num("admission.candidates.slipX", -0.075f);
        public static float SlipZ => BoardViewUtil.Num("admission.candidates.slipZ", -0.235f);

        /// <summary><paramref name="n"/> karttan <paramref name="i"/>.'nin yeri (Top yereli, m): sıra ortalanır, adım kart eni + boşluk.</summary>
        public static Vector3 CardLocal(int i, int n)
        {
            float pitch = (CandidateCardFace.WidthMm * CardScale + GapMm) / 1000f;
            return new Vector3(RowCenterX + (i - (Mathf.Max(1, n) - 1) / 2f) * pitch, SurfaceY, RowZ);
        }

        /// <summary>Grup fişinin yeri (Top yereli, m).</summary>
        public static Vector3 SlipLocal => new Vector3(SlipX, SurfaceY, SlipZ);

        // ------------------------------------------------------------------ kurulum

        /// <summary>Bağlar (tek seferlik sahne script'i ve testler): tezgâh kanadı, evrak çekmecesi, kart kökleri, fiş kökü.</summary>
        public void Configure(Transform top, Transform drawer, Transform[] cards, Transform slip)
        {
            frame = top; stowFrame = drawer; cardPapers = cards ?? new Transform[0]; slipPaper = slip;
            initialized = false;
            EnsureInit();
        }

        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            if (cardPapers == null) cardPapers = new Transform[0];
            cardFaces = new CandidateCardFace[cardPapers.Length];
            cardHits = new Collider[cardPapers.Length];
            for (int i = 0; i < cardPapers.Length; i++)
            {
                cardFaces[i] = Face<CandidateCardFace>(cardPapers[i]);
                cardHits[i] = Hit(cardPapers[i]);
            }
            slipFace = Face<CandidateGroupSlipFace>(slipPaper);
            slipHit = Hit(slipPaper);
        }

        static T Face<T>(Transform root) where T : Component
        {
            if (root == null) return null;
            var c = root.GetComponent<T>();
            return c != null ? c : root.GetComponentInChildren<T>(true);
        }

        static Collider Hit(Transform root)
        {
            if (root == null) return null;
            var c = root.GetComponent<Collider>();
            return c != null ? c : root.GetComponentInChildren<Collider>(true);
        }

        // ------------------------------------------------------------------ bağlama

        /// <summary>
        /// Grubu uygular: null hepsini gizler; yeni <see cref="CandidateGroupData.Key"/> yerleşimi ve damga hedefini sıfırlar. İz alanları
        /// (KABUL izi kartta, GERİ ÇEVİR izi fişte) yüzlere geçer; <paramref name="animate"/> basışın girişini oynatır.
        /// </summary>
        public void Bind(CandidateGroupData g, bool animate)
        {
            EnsureInit();
            if (g == null) { HideAll(); return; }
            if (g.Key != key)
            {
                key = g.Key;
                stowing = false;
                stowT.Snap(1f);
                target = Target.None;
                targetCard = -1;
            }
            data = g;
            // Önce görünürlük ve poz: yüzler açıkken bağlanırsa yerleşim hemen yapılır (kapalıyken bağlanan yüz sonraki kareyi bekler)
            Apply();
            int n = CardCount;
            for (int i = 0; i < cardFaces.Length; i++)
            {
                var face = cardFaces[i];
                if (face == null) continue;
                var card = i < n ? g.Cards[i] : null;
                bool had = face.ImpressionVisible;
                face.Bind(card);
                bool on = card != null && card.StampChoiceId != null;
                face.ShowImpression(on, animate && on && !had);
            }
            if (slipFace != null)
            {
                bool had = slipFace.StampVisible;
                slipFace.Bind(g);
                bool on = g.PassStampLines != null;
                slipFace.ShowStamp(on, animate && on && !had);
            }
            if (hoverPart == Part.Card && hoverCard >= n) EndHover();
        }

        /// <summary>İstasyonun gevşek araç görünürlüğü (kanat 30°'nin altında): kâğıtlar yalnız o zaman görünür.</summary>
        public void SetShown(bool on)
        {
            if (shown == on) { Apply(); return; }
            shown = on;
            if (!on) EndHover();
            Apply();
        }

        /// <summary>Çarpıştırıcılar: etkileşimli, görünür, veri var ve kâğıtlar kaldırılmıyorken açık.</summary>
        public void SetInteractive(bool on)
        {
            interactive = on;
            if (!on) EndHover();
            ApplyColliders();
        }

        /// <summary>Karar dizisi: görünen bütün kâğıtlar o anki pozundan evrak çekmecesine (<paramref name="seconds"/>).</summary>
        public void Stow(float seconds)
        {
            EnsureInit();
            if (data == null || !shown || stowing) return;
            for (int i = 0; i < stowFrom.Length; i++) stowFrom[i] = RestPose(i);
            stowing = true;
            stowT.Snap(0f);
            stowT.Go(1f, seconds, CardEase.EaseInOut, now);
            EndHover();
            Sound?.Invoke(AdmissionSound.DocDrop);
            Apply();
        }

        /// <summary>Çekmece kapandı (ya da grup kalktı): kâğıtlar gizlenir, veri ve damga hedefi sıfırlanır.</summary>
        public void HideAll()
        {
            EnsureInit();
            data = null;
            key = null;
            stowing = false;
            stowT.Snap(1f);
            target = Target.None;
            targetCard = -1;
            EndHover();
            foreach (var face in cardFaces) if (face != null) { face.Bind(null); face.ShowImpression(false, false); }
            if (slipFace != null) { slipFace.Bind(null); slipFace.ShowStamp(false, false); }
            Apply();
        }

        /// <summary>Kaldırma geçişini ilerletir ve kâğıtları kanadın o anki pozuna oturtur (istasyonun saati çağırır).</summary>
        public void Advance(float dt)
        {
            now += Mathf.Max(0f, dt);
            stowT.Tick(now);
            if (!stowing && !stowT.Moving && data == null) now = 0f;
            Apply();
        }

        // ------------------------------------------------------------------ pozlar

        static Pose InFrame(Transform f, Vector3 local, Quaternion rot) =>
            f == null ? new Pose(local, rot) : new Pose(f.TransformPoint(local), f.rotation * rot);

        /// <summary>Kâğıdın tezgâhtaki pozu; dizin <see cref="MaxCards"/> grup fişidir.</summary>
        Pose RestPose(int i)
        {
            if (i >= MaxCards) return InFrame(frame, SlipLocal, AdmissionStation.PaperRotation(Vector3.up, Vector3.forward, SlipRot));
            return InFrame(frame, CardLocal(i, CardCount), AdmissionStation.PaperRotation(Vector3.up, Vector3.forward, CardRot[i % CardRot.Length]));
        }

        Pose StowPose(int i)
        {
            var local = AdmissionStation.DocInDrawerLocal + new Vector3(0f, StowStepM * (i + 1), 0f);
            return InFrame(stowFrame != null ? stowFrame : frame, local, AdmissionStation.PaperRotation(Vector3.up, Vector3.forward, AdmissionStation.DocInDrawerRot));
        }

        Pose CurrentPose(int i)
        {
            if (!stowing) return RestPose(i);
            float t = stowT.Value;
            var a = stowFrom[i]; var b = StowPose(i);
            if (t >= 1f) return b;
            return new Pose(Vector3.LerpUnclamped(a.position, b.position, t), Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));
        }

        void Apply()
        {
            EnsureInit();
            bool on = data != null && shown;
            int n = CardCount;
            float scale = CardScale;
            for (int i = 0; i < cardPapers.Length; i++)
            {
                var p = cardPapers[i];
                if (p == null) continue;
                bool visible = on && i < n;
                if (visible)
                {
                    Place(p, CurrentPose(i));
                    var s = Vector3.one * scale;
                    if (p.localScale != s) p.localScale = s;
                }
                SetActive(p.gameObject, visible);
            }
            if (slipPaper != null)
            {
                if (on) Place(slipPaper, CurrentPose(MaxCards));
                SetActive(slipPaper.gameObject, on);
            }
            ApplyColliders();
        }

        void ApplyColliders()
        {
            bool on = interactive && data != null && shown && !stowing;
            int n = CardCount;
            for (int i = 0; i < cardHits.Length; i++) SetCollider(cardHits[i], on && i < n);
            SetCollider(slipHit, on);
        }

        static void Place(Transform t, Pose p)
        {
            if (t.position != p.position || t.rotation != p.rotation) t.SetPositionAndRotation(p.position, p.rotation);
        }

        static void SetActive(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        static void SetCollider(Collider c, bool on)
        {
            if (c != null && c.enabled != on) c.enabled = on;
        }

        /// <summary>
        /// Dünya noktasının kâğıt yerelindeki mm'si (kâğıdın sol üstü köken, x sağa, y aşağı; kart ölçeği düşülmüş) ve kâğıt düzlemine
        /// uzaklığı (mm).
        /// </summary>
        static Vector2 PaperMm(Pose pose, Vector3 world, Vector2 sizeMm, float scale, out float heightMm)
        {
            var d = world - pose.position;
            var right = pose.rotation * Vector3.right;
            var up = pose.rotation * Vector3.up;
            var normal = pose.rotation * Vector3.back;
            heightMm = Vector3.Dot(d, normal) * 1000f;
            float x = Vector3.Dot(d, right) * 1000f / scale, y = Vector3.Dot(d, up) * 1000f / scale;
            return new Vector2(x + sizeMm.x / 2f, sizeMm.y / 2f - y);
        }

        static bool Inside(Vector2 mm, Vector2 sizeMm) => mm.x >= 0f && mm.x <= sizeMm.x && mm.y >= 0f && mm.y <= sizeMm.y;

        static readonly Vector2 CardSize = new Vector2(CandidateCardFace.WidthMm, CandidateCardFace.HeightMm);
        static readonly Vector2 SlipSize = new Vector2(CandidateGroupSlipFace.WidthMm, CandidateGroupSlipFace.HeightMm);

        /// <summary>Dünya noktasının grup fişindeki mm'si (sol üst köken, y aşağı).</summary>
        public Vector2 SlipMm(Vector3 world) => PaperMm(CurrentPose(MaxCards), world, SlipSize, 1f, out _);

        // ------------------------------------------------------------------ damga

        /// <summary>
        /// Damga bu noktaya bırakılabilir mi (ve hedefi saklar): KABUL bir kartın üstüne, GERİ ÇEVİR grup fişinin üstüne (düzleme en çok
        /// 30 mm). Öteki damgalar ve kâğıtlar görünmüyorken / kalkarken false. Seçilebilirlik burada sorulmaz: Core denetimi sunucuda.
        /// </summary>
        public bool AcceptStamp(StampChoice c, Vector3 world)
        {
            EnsureInit();
            if (data == null || !shown || stowing) return false;
            if (c == StampChoice.Admit)
            {
                float scale = CardScale;
                for (int i = 0; i < CardCount; i++)
                {
                    var mm = PaperMm(RestPose(i), world, CardSize, scale, out float h);
                    if (Mathf.Abs(h) > DropHeightMm || !Inside(mm, CardSize)) continue;
                    target = Target.Card;
                    targetCard = i;
                    return true;
                }
                return false;
            }
            if (c == StampChoice.Reject)
            {
                var mm = PaperMm(RestPose(MaxCards), world, SlipSize, 1f, out float h);
                if (Mathf.Abs(h) > DropHeightMm || !Inside(mm, SlipSize)) return false;
                target = Target.Slip;
                targetCard = -1;
                return true;
            }
            return false;
        }

        /// <summary>Basılan damganın pozu: son kabul edilen hedefin iz merkezi, kâğıt yüzeyinin 0,2 mm üstü (damga kökü lastik yüzün ortası).</summary>
        public Pose StampPose(StampChoice c)
        {
            EnsureInit();
            bool slip = c == StampChoice.Reject || target == Target.Slip;
            int i = slip ? MaxCards : Mathf.Clamp(targetCard, 0, Mathf.Max(0, CardCount - 1));
            var pose = RestPose(i);
            Vector2 centre, size;
            float scale, tilt;
            if (slip)
            {
                centre = slipFace != null ? slipFace.StampCenterMm : SlipSize / 2f;
                size = SlipSize; scale = 1f; tilt = -CandidateGroupSlipFace.StampTiltDeg;
            }
            else
            {
                var face = i < cardFaces.Length ? cardFaces[i] : null;
                centre = face != null ? face.ImpressionCenterMm : CardSize / 2f;
                size = CardSize; scale = CardScale; tilt = AdmissionStation.ImpressionRot;
            }
            var right = pose.rotation * Vector3.right;
            var up = pose.rotation * Vector3.up;
            var normal = pose.rotation * Vector3.back;
            var p = pose.position + right * ((centre.x - size.x / 2f) * scale / 1000f) + up * ((size.y / 2f - centre.y) * scale / 1000f) + normal * 0.0002f;
            return new Pose(p, Quaternion.AngleAxis(tilt, normal) * Quaternion.LookRotation(up, normal));
        }

        /// <summary>Okunacak kâğıt: aday kartı (dizinle) ya da grup fişi; görünmüyorsa null.</summary>
        public Transform PaperOf(AdmissionPaper kind, int index)
        {
            EnsureInit();
            if (data == null || !shown) return null;
            if (kind == AdmissionPaper.GroupSlip) return slipPaper;
            if (kind == AdmissionPaper.Candidate) return index >= 0 && index < CardCount ? cardPapers[index] : null;
            return null;
        }

        // ------------------------------------------------------------------ etkileşim

        Part PartOf(GameObject go, out int card)
        {
            card = -1;
            if (go == null) return Part.None;
            for (int i = 0; i < cardHits.Length; i++)
                if (cardHits[i] != null && go == cardHits[i].gameObject) { card = i; return Part.Card; }
            if (slipHit != null && go == slipHit.gameObject) return Part.Slip;
            return Part.None;
        }

        int RowAtWorld(Vector3 world)
        {
            if (slipFace == null || data == null || data.Replace.Count == 0) return -1;
            return slipFace.RowAt(SlipMm(world));
        }

        public void OnPointerEnter(PointerEventData eventData) => UpdateHover(eventData);

        public void OnPointerMove(PointerEventData eventData) => UpdateHover(eventData);

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void UpdateHover(PointerEventData eventData)
        {
            if (!interactive || Blocked || eventData == null) { EndHover(); return; }
            var ray = eventData.pointerCurrentRaycast;
            var part = PartOf(ray.gameObject, out int card);
            if (part == Part.None) { EndHover(); return; }
            hoverPart = part;
            hoverCard = card;
            hoverRow = part == Part.Slip ? RowAtWorld(ray.worldPosition) : -1;
            if (hovered) return;
            hovered = true;
            if (hoverTag != null) hoverTag.Show(this);
        }

        void EndHover()
        {
            hoverPart = Part.None;
            hoverCard = hoverRow = -1;
            if (!hovered) return;
            hovered = false;
            if (hoverTag != null) hoverTag.Hide(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressGo = eventData != null ? eventData.pointerPressRaycast.gameObject : null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            EnsureInit();
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !interactive || Blocked) return;
            var ray = eventData.pointerCurrentRaycast;
            var go = ray.gameObject;
            if (go == null || (pressGo != null && go != pressGo)) return;
            var held = HeldStamp != null ? HeldStamp() : null;
            if (held != null) { held.ClickAtScreen(eventData.position); return; }
            var part = PartOf(go, out int card);
            if (part == Part.Card) ClickCard(card);
            else if (part == Part.Slip) ClickSlip(RowAtWorld(ray.worldPosition));
        }

        void ClickCard(int i)
        {
            if (data == null || stowing || i < 0 || i >= CardCount) return;
            CardClicked?.Invoke(i);
        }

        /// <summary>Fişe tık: <paramref name="row"/> geçerli ve seçilebilir bir satırsa yollanacak kişi, değilse okuma.</summary>
        public void ClickSlip(int row)
        {
            if (data == null || stowing || !interactive) return;
            if (row >= 0 && row < data.Replace.Count)
            {
                if (data.Replace[row].Enabled) ReplaceClicked?.Invoke(data.Replace[row].Id);
                return;
            }
            SlipClicked?.Invoke();
        }

        /// <summary>Çarpıştırıcısına göre tık (sentetik testler; işaretçi olayının dalıyla aynı, fişte satır dışı).</summary>
        public void ClickCollider(Collider c)
        {
            EnsureInit();
            if (c == null || !interactive) return;
            var part = PartOf(c.gameObject, out int card);
            if (part == Part.Card) ClickCard(card);
            else if (part == Part.Slip) ClickSlip(-1);
        }

        void OnDisable() => EndHover();
    }
}
