using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// KİŞİ DOSYASI (R15f; <c>PersonFolder</c> model örneği, tel sepette): kapakta seçili kişinin adı (ya da seçim yokken
    /// soluk ipucu), tıklayınca (ya da kişi kartına çift tıklayınca, sunucu çağırır) 0,35 s'de okuma pozuna gelip açılır.
    /// Sayfalar (<c>TXT_PageL/R</c> yüzeylerindeki dünya tuvalleri, 1 birim = 1 mm) <see cref="FolderData"/>'dan çalışma anında
    /// bir TMP satır şablonuyla kurulur. Sekmeler: İNCELE, ERİŞİM (tıklama), TAHLİYE (1,2 s basılı tutma, damga düşer; klavyede
    /// sunucu <see cref="BeginDismissHold"/>/<see cref="EndDismissHold"/> ile sürer). Kapalı sekmeye tıklayınca nedeni sayfada
    /// yazar. Kapak dönüşü modelin −176°'sine +θ yönünden (öne doğru) gider ve sonda parçaya oturur (C §8-7).
    /// </summary>
    public sealed class PersonFolderView : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Model (PersonFolder)")]
        [Tooltip("Okuma pozuna giden kök (boşsa bu nesne).")]
        [SerializeField] Transform root;
        [SerializeField] PartDriver cover;
        [Tooltip("Pano modelinin kökü: okuma pozu (personFile.readPose) bu çerçevededir.")]
        [SerializeField] Transform frame;

        [Header("Kapak")]
        [Tooltip("Kapak 1. satırı (KİŞİ DOSYASI; sabit, DeskLocText). Yalnız etiket başlığı için okunur.")]
        [SerializeField] TMP_Text coverTitle;
        [Tooltip("Kapak 2. satırı: seçili kişinin adı ya da ipucu (dinamik).")]
        [SerializeField] TMP_Text coverLine;
        [Tooltip("Kapalı dosyanın tıklama çarpıştırıcısı (bu nesnenin çocuğu, DeskTarget'sız).")]
        [SerializeField] Collider coverHit;
        [SerializeField] HoverTag hoverTag;

        [Header("Sayfalar")]
        [SerializeField] RectTransform pageLeft;
        [SerializeField] RectTransform pageRight;
        [Tooltip("Sayfa satırı şablonu (TextMeshProUGUI, pasif, raycastTarget kapalı).")]
        [SerializeField] TMP_Text rowTemplate;
        [SerializeField] PersonDossierFace dossier;
        [SerializeField] Collider previousPage, nextPage;
        [Tooltip("Sağ sayfada sekmelerin üstündeki not satırı (kapalı sekmenin nedeni, Core'un iletisi).")]
        [SerializeField] TMP_Text note;

        [Header("Sekmeler (bu nesnenin çocukları, DeskTarget'sız)")]
        [SerializeField] Collider reviewTab;
        [SerializeField] Collider accessTab;
        [SerializeField] Collider dismissTab;
        [SerializeField] TMP_Text reviewLabel, accessLabel, dismissLabel;
        [Tooltip("Aşama 10 (D-F, R10): sekmelerin alt satırları; sekme kapalıyken adıyla birlikte soluklaşır.")]
        [SerializeField] TMP_Text reviewSub, accessSub, dismissSub;
        [Tooltip("TAHLİYE damgası (basılı tutarken iner, 1,2 s'de oturur).")]
        [SerializeField] CanvasGroup stamp;
        [SerializeField] TMP_Text stampText;

        [Header("Etiket (Aşama 6, R17f)")]
        [Tooltip("Dosyanın üst kenarındaki kıskaçlı etiket (FileTag modeli): İNCELEME ya da KISITLI · n V; yoksa gizli.")]
        [SerializeField] FileTagView fileTag;

        FolderMotion motion;
        PageRows leftRows, rightRows;
        FolderData current;
        bool open, interactive = true, hovered;
        float coverNameSize;
        Color coverNameColour;
        TMP_FontAsset coverNameFont;
        bool coverCaptured;

        bool holding, holdByPointer, landed;
        float holdStart, landedAt;
        PointerEventData holdEvent;
        GameObject pressGo;

        public event Action Review, Access, Dismiss;
        /// <summary>Kapalı dosyanın kapağına tıklandı (sunucu seçili kişinin dosyasıyla <see cref="Open"/> der).</summary>
        public event Action Clicked;

        public bool IsOpen => open;
        /// <summary>Kapalı dosyanın çarpıştırıcısı (sürükleme denetleyicisi kişi kartını dosyaya bırakmayı buna ışınla çözer).</summary>
        public Collider Hit => coverHit;
        /// <summary>Dosya hareket ediyor (öne gelme / geri dönme).</summary>
        public bool Moving => motion != null && motion.Moving;
        public HoverTag Tag { get => hoverTag; set => hoverTag = value; }
        /// <summary>Kıskaçlı dosya etiketi (sahnede bağlanmamışsa null; <see cref="Open"/> verinin <c>TagText</c>'ini yazar).</summary>
        public FileTagView FileTag => fileTag;
        public string HoverTitle => coverTitle != null ? coverTitle.text : null;
        public string Line => coverLine != null && !string.IsNullOrEmpty(coverLine.text) ? coverLine.text : null;

        static Color Ink => BoardViewUtil.Col("ink", new Color32(0x1E, 0x1B, 0x16, 0xFF));
        static Color Mute => new Color32(0x5F, 0x57, 0x4A, 0xFF);
        static Color Red => BoardViewUtil.Col("cards.material.reservedRing", new Color32(0x9A, 0x1E, 0x14, 0xFF));
        static Color Blue => BoardViewUtil.Col("pen.signature", new Color32(0x1A, 0x2F, 0x5A, 0xFF));

        void Awake()
        {
            EnsureMotion();
            CaptureCover();
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            SetPagesActive(false);
            ResetStamp();
            SetTag(null);
            UpdateColliders();
        }

        void EnsureMotion()
        {
            if (motion != null) return;
            motion = new FolderMotion(root != null ? root : transform, cover, frame);
            if (pageLeft != null && rowTemplate != null) leftRows = new PageRows(pageLeft, rowTemplate);
            if (pageRight != null && rowTemplate != null) rightRows = new PageRows(pageRight, rowTemplate);
        }

        void CaptureCover()
        {
            if (coverCaptured || coverLine == null) return;
            coverCaptured = true;
            coverNameSize = coverLine.fontSize;
            coverNameColour = coverLine.color;
            coverNameFont = coverLine.font;
        }

        // ------------------------------------------------------------------ kapak

        public void SetCover(string line2) => SetCover(line2, false);

        /// <summary>Kapağın 2. satırı. <paramref name="hint"/>: seçim yok ("— kart seç —"): Courier, küçük ve soluk.</summary>
        public void SetCover(string line2, bool hint)
        {
            if (coverLine == null) return;
            CaptureCover();
            BoardViewUtil.SetText(coverLine, line2);
            TMP_FontAsset font = coverNameFont;
            float size = coverNameSize;
            Color colour = coverNameColour;
            if (hint)
            {
                var desk = DeskTheme.Active;
                var cp = desk != null ? desk.Font(DeskFont.CpBold) : null;
                if (cp != null) font = cp;
                float nameMm = BoardViewUtil.Num("personFile.cover.nameMm", 15.71f);
                float hintMm = BoardViewUtil.Num("personFile.cover.hintMm", 9.4f);
                size = coverNameSize * (nameMm > 0 ? hintMm / nameMm : 1f);
                colour.a *= BoardViewUtil.Num("personFile.cover.hintAlpha", 0.6f);
            }
            if (font != null && coverLine.font != font) { coverLine.font = font; coverLine.fontSharedMaterial = font.material; }
            if (!Mathf.Approximately(coverLine.fontSize, size)) coverLine.fontSize = size;
            if (coverLine.color != colour) coverLine.color = colour;
        }

        // ------------------------------------------------------------------ aç / kapa

        /// <summary>Dosyayı bu verilerle açar; zaten açıksa yalnız sayfaları yeniler (aynı kişide not satırı korunur).</summary>
        public void Open(FolderData d)
        {
            if (d == null) return;
            EnsureMotion();
            bool samePerson = current != null && current.PersonId == d.PersonId;
            current = d;
            SetPagesActive(true);   // önce: satır kopyaları etkin hiyerarşide uyansın (TMP ölçüsü)
            Build(d, samePerson);
            if (open) { UpdateColliders(); return; }
            open = true;
            EndHover();
            motion.SetOpen(true);
            UpdateColliders();
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            // Etiket açık dosyanın verisini taşır; kapanınca kalkar (seçim değişince bayat kalmasın; sunucu SetTag ile yeniden takabilir)
            SetTag(null);
            CancelHold();
            EnsureMotion();
            motion.SetOpen(false);
            UpdateColliders();
        }

        /// <summary>K4'e varınca açık; başka pozlarda çarpıştırıcılar kapalı.</summary>
        public void SetInteractive(bool on)
        {
            interactive = on;
            if (!on) { CancelHold(); EndHover(); }
            UpdateColliders();
        }

        void UpdateColliders()
        {
            if (coverHit != null) coverHit.enabled = interactive && !open;
            bool tabs = interactive && open;
            if (reviewTab != null) reviewTab.enabled = tabs;
            if (accessTab != null) accessTab.enabled = tabs;
            if (dismissTab != null) dismissTab.enabled = tabs;
            // Sayfa yüzeyleri (işleyicisiz; tık bu bileşene çıkar) yalnız dosya açıkken: kapalıyken ya da defter kipinde (Aşama 5)
            // kapağın önündeki ışını yutup arkadaki pano tıkını engelliyorlardı
            pageSurfaces ??= OtherColliders();
            foreach (var c in pageSurfaces) if (c != null) c.enabled = tabs;
            if (previousPage) previousPage.enabled = tabs && dossier && dossier.Page > 0;
            if (nextPage) nextPage.enabled = tabs && dossier && dossier.Page + 1 < dossier.PageCount;
            if (previousPage) previousPage.gameObject.SetActive(dossier && dossier.PageCount > 1);
            if (nextPage) nextPage.gameObject.SetActive(dossier && dossier.PageCount > 1);
        }

        Collider[] pageSurfaces;

        /// <summary>Kendi alt ağacındaki kapak ve sekmeler dışındaki çarpıştırıcılar (sayfa yüzeyleri).</summary>
        Collider[] OtherColliders()
        {
            var list = new System.Collections.Generic.List<Collider>();
            foreach (var c in GetComponentsInChildren<Collider>(true))
                if (c != coverHit && c != reviewTab && c != accessTab && c != dismissTab && c != previousPage && c != nextPage) list.Add(c);
            return list.ToArray();
        }

        void SetPagesActive(bool on)
        {
            if (pageLeft != null && pageLeft.gameObject.activeSelf != on) pageLeft.gameObject.SetActive(on);
            if (pageRight != null && pageRight.gameObject.activeSelf != on) pageRight.gameObject.SetActive(on);
        }

        // ------------------------------------------------------------------ sayfalar

        void Build(FolderData d, bool keepNote)
        {
            float margin = BoardViewUtil.Num("personFile.page.marginMm", 8f);
            float labelW = BoardViewUtil.Num("personFile.page.labelMm", 38f);
            float bodyMm = BoardViewUtil.Num("personFile.page.bodyMm", 4.2f);
            float labelMm = BoardViewUtil.Num("personFile.page.labelMm2", 3.2f);
            float nameMm = BoardViewUtil.Num("personFile.page.nameMm", 7f);
            float sectionMm = BoardViewUtil.Num("personFile.page.sectionMm", 3.4f);
            float gap = BoardViewUtil.Num("personFile.page.gapMm", 1.4f);
            float tabsMm = BoardViewUtil.Num("personFile.page.tabsMm", 34f);
            var desk = DeskTheme.Active;
            var fBold = desk != null ? desk.Font(DeskFont.LfBold) : null;
            var fBody = desk != null ? desk.Font(DeskFont.LfRegular) : null;
            var fMono = desk != null ? desk.Font(DeskFont.CpBold) : null;

            if (dossier != null) dossier.Bind(d);
            if (dossier == null && leftRows != null)
            {
                leftRows.Begin(margin);
                float h = leftRows.Put(d.Name, margin, leftRows.Width - 2 * margin, nameMm, Ink, fBold, FontStyles.Normal, true, leftRows.Height - margin);
                if (h > 0f) leftRows.Advance(h + gap * 2f);
                PutLines(leftRows, d.Left, margin, labelW, bodyMm, labelMm, sectionMm, gap, fBody, fMono, leftRows.Height - margin);
                leftRows.End();
            }
            if (dossier == null && rightRows != null)
            {
                rightRows.Begin(margin);
                PutLines(rightRows, d.Right, margin, labelW, bodyMm, labelMm, sectionMm, gap, fBody, fMono, rightRows.Height - tabsMm);
                rightRows.End();
            }

            SetTag(d.TagText);
            SetTab(reviewLabel, d.ReviewLabel, d.CanReview);
            SetTab(accessLabel, d.RestrictLabel, d.CanRestrict);
            SetTab(dismissLabel, d.DismissLabel, d.CanDismiss);
            SetTab(reviewSub, d.ReviewSub, d.CanReview);
            SetTab(accessSub, d.RestrictSub, d.CanRestrict);
            SetTab(dismissSub, d.DismissSub, d.CanDismiss);
            BoardViewUtil.SetText(stampText, d.DismissLabel);
            if (!keepNote) { BoardViewUtil.SetText(note, ""); ResetStamp(); }
        }

        static void PutLines(PageRows rows, List<FolderLine> lines, float margin, float labelW, float bodyMm, float labelMm,
            float sectionMm, float gap, TMP_FontAsset fBody, TMP_FontAsset fMono, float limit)
        {
            float width = rows.Width - 2 * margin;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Section)
                {
                    rows.Advance(gap);
                    string text = string.IsNullOrEmpty(line.Label) ? line.Value : line.Label;
                    float h = rows.Put(text, margin, width, sectionMm, Red, fMono, FontStyles.Underline, false, limit);
                    if (h < 0) return;
                    rows.Advance(h + gap);
                    continue;
                }
                Color tone = ToneColour(line.Tone);
                if (string.IsNullOrEmpty(line.Label))
                {
                    float h = rows.Put(line.Value, margin, width, bodyMm, tone, fBody, FontStyles.Normal, true, limit);
                    if (h < 0) return;
                    rows.Advance(h + gap);
                    continue;
                }
                float valueX = margin + labelW + 2f;
                float valueW = Mathf.Max(10f, rows.Width - margin - valueX);
                float hv = rows.Measure(line.Value, valueW, bodyMm, fBody, true);
                float hl = rows.Measure(line.Label, labelW, labelMm, fMono, true);
                float rowH = Mathf.Max(hv, hl);
                if (rows.Y + rowH > limit) return;
                rows.Put(line.Label, margin, labelW, labelMm, Mute, fMono, FontStyles.Normal, true, limit, 0.6f);
                rows.Put(line.Value, valueX, valueW, bodyMm, tone, fBody, FontStyles.Normal, true, limit);
                rows.Advance(rowH + gap);
            }
        }

        static Color ToneColour(StripTone tone)
        {
            switch (tone)
            {
                case StripTone.Blue: return Blue;
                case StripTone.Red: return Red;
                case StripTone.Mute: return Mute;
                default: return Ink;
            }
        }

        static void SetTab(TMP_Text label, string text, bool enabled)
        {
            if (label == null) return;
            BoardViewUtil.SetText(label, text);
            float a = enabled ? 1f : BoardViewUtil.Num("personFile.tab.disabledAlpha", 0.4f);
            if (!Mathf.Approximately(label.alpha, a)) label.alpha = a;
        }

        /// <summary>
        /// Kıskaçlı dosya etiketi (Aşama 6, R17f; <see cref="FolderData.TagText"/>): "İNCELEME" ya da "KISITLI · n V"; null gizler.
        /// Dosya açılınca verisinden kendiliğinden takılır, kapanınca kalkar.
        /// </summary>
        public void SetTag(string text)
        {
            if (fileTag != null) fileTag.Bind(text);
        }

        /// <summary>Sağ sayfanın not satırına yazar (ör. Core'un reddi); TAHLİYE damgası kalkar.</summary>
        public void ShowNote(string text)
        {
            BoardViewUtil.SetText(note, text);
            ResetStamp();
        }

        // ------------------------------------------------------------------ TAHLİYE basılı tutma

        /// <summary>Klavye yolu: dosya açıkken Enter basılınca başlar (tutma 1,2 s).</summary>
        public void BeginDismissHold()
        {
            if (!open || current == null) return;
            if (!current.CanDismiss) { BoardViewUtil.SetText(note, current.DismissNote); return; }
            StartHold(false, null);
        }

        /// <summary>Klavye yolu: Enter bırakıldı; 1,2 s dolmadıysa iptal.</summary>
        public void EndDismissHold() { if (holding) CancelHold(); }

        void StartHold(bool pointer, PointerEventData e)
        {
            holding = true; holdByPointer = pointer; holdEvent = e; landed = false;
            holdStart = Time.unscaledTime;
            ShowStamp(0f, 0f);
        }

        void CancelHold()
        {
            if (!holding) return;
            holding = false; holdEvent = null;
            ResetStamp();
        }

        void ResetStamp()
        {
            landed = false;
            if (stamp == null) return;
            stamp.alpha = 0f;
            stamp.transform.localScale = Vector3.one;
        }

        void ShowStamp(float alpha, float drop)
        {
            if (stamp == null) return;
            stamp.alpha = alpha;
            float s = Mathf.Lerp(BoardViewUtil.Num("personFile.stamp.liftScale", 1.35f), 1f, drop);
            stamp.transform.localScale = new Vector3(s, s, 1f);
        }

        // ------------------------------------------------------------------ işaretçi

        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();

        bool Is(Collider c, GameObject go) => c != null && go != null && c.gameObject == go;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactive || open || Blocked) return;
            hovered = true;
            if (hoverTag != null) hoverTag.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        void EndHover()
        {
            if (!hovered) return;
            hovered = false;
            if (hoverTag != null) hoverTag.Hide(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressGo = eventData.pointerPressRaycast.gameObject;
            if (eventData.button != PointerEventData.InputButton.Left || !interactive || Blocked || !open || current == null) return;
            if (!Is(dismissTab, pressGo)) return;
            if (!current.CanDismiss) { BoardViewUtil.SetText(note, current.DismissNote); return; }
            StartHold(true, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (holding && holdByPointer) CancelHold();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !interactive || Blocked) return;
            var go = eventData.pointerCurrentRaycast.gameObject;
            if (go == null || go != pressGo) return;
            if (!open)
            {
                if (Is(coverHit, go)) { EndHover(); Clicked?.Invoke(); }
                return;
            }
            if (current == null) return;
            if (Is(previousPage, go) || Is(nextPage, go))
            {
                if (dossier) dossier.Turn(Is(nextPage, go) ? 1 : -1);
                UpdateColliders();
            }
            else if (Is(reviewTab, go))
            {
                if (current.CanReview) Review?.Invoke();
                else BoardViewUtil.SetText(note, current.ReviewNote);
            }
            else if (Is(accessTab, go))
            {
                if (current.CanRestrict) Access?.Invoke();
                else BoardViewUtil.SetText(note, current.RestrictNote);
            }
        }

        // ------------------------------------------------------------------ kare

        void Update()
        {
            float now = Time.unscaledTime;
            if (motion != null && motion.Tick(Time.unscaledDeltaTime) && !open)
            {
                SetPagesActive(false);
                ResetStamp();
            }

            if (hovered && (Blocked || !interactive || open)) EndHover();

            if (holding)
            {
                bool stillOn = !holdByPointer || (holdEvent != null && Is(dismissTab, holdEvent.pointerCurrentRaycast.gameObject));
                if (!stillOn || !open) CancelHold();
                else
                {
                    float p = (now - holdStart) / Mathf.Max(0.01f, BoardViewUtil.Num("motion.board.dismissHoldS", 1.2f));
                    if (p >= 1f)
                    {
                        holding = false; holdEvent = null;
                        landed = true; landedAt = now;
                        Dismiss?.Invoke();
                    }
                    else ShowStamp(0.15f + 0.45f * p, 0.35f * p);
                }
            }
            if (landed)
            {
                float s = BoardViewUtil.Num("motion.stampS", 0.1f);
                float t = s <= 0 ? 1f : Mathf.Clamp01((now - landedAt) / s);
                ShowStamp(Mathf.Lerp(0.6f, 1f, t), Mathf.Lerp(0.35f, 1f, BoardViewUtil.Out(t)));
            }
        }

        void OnDisable()
        {
            CancelHold();
            EndHover();
        }
    }

    /// <summary>
    /// Dosya modelinin (PersonFolder) öne gelip açılması: kök kapalı pozla okuma pozu (<c>personFile.readPose</c>, pano
    /// çerçevesinde) arasında, kapak 0 ile açık açı arasında aynı ilerlemeyle (0,35 s, gir-çık) gider. Kapak +θ yönünde
    /// (öne doğru) döndürülür ve uçta parçaya oturtulur; ters yönden açmak kenarı panonun içinden geçirirdi (C §8-7).
    /// Yön ortada değişirse kaldığı yerden döner.
    /// </summary>
    internal sealed class FolderMotion
    {
        readonly Transform root, frame;
        readonly PartDriver cover;
        readonly Vector3 closedPos;
        readonly Quaternion closedRot;
        Vector3 readPos;
        Quaternion readRot;
        float progress, target;
        bool settled = true;

        public FolderMotion(Transform root, PartDriver cover, Transform frame)
        {
            this.root = root; this.cover = cover; this.frame = frame;
            closedPos = root.localPosition;
            closedRot = root.localRotation;
            readPos = closedPos; readRot = closedRot;
        }

        public float Progress => progress;
        public bool Moving => !settled;

        public void SetOpen(bool open)
        {
            if (open) ComputeReadPose();
            target = open ? 1f : 0f;
            settled = Mathf.Approximately(progress, target);
        }

        void ComputeReadPose()
        {
            var pos = new Vector3(BoardViewUtil.Num("personFile.readPose.pos[0]", 0.015f), BoardViewUtil.Num("personFile.readPose.pos[1]", 1.203f),
                BoardViewUtil.Num("personFile.readPose.pos[2]", -0.680f));
            float tilt = BoardViewUtil.Num("personFile.readPose.tiltX", -8f);
            var frameRot = frame != null ? frame.rotation : Quaternion.identity;
            Vector3 world = frame != null ? frame.TransformPoint(pos) : pos;
            Quaternion worldRot = frameRot * Quaternion.Euler(tilt, 0, 0);
            var parent = root.parent;
            readPos = parent != null ? parent.InverseTransformPoint(world) : world;
            readRot = parent != null ? Quaternion.Inverse(parent.rotation) * worldRot : worldRot;
        }

        float OpenAngle
        {
            get
            {
                if (cover == null) return 0f;
                float end = Mathf.Abs(cover.RangeTo) > Mathf.Abs(cover.RangeFrom) ? cover.RangeTo : cover.RangeFrom;
                return end < 0f ? 360f + end : end;
            }
        }

        float CoverAngle(float p) => OpenAngle * BoardViewUtil.InOut(p);

        void ApplyCover(float angle)
        {
            if (cover == null) return;
            var axis = cover.Axis == PartAxis.X ? Vector3.right : cover.Axis == PartAxis.Y ? Vector3.up : Vector3.forward;
            var t = cover.transform;
            t.localPosition = cover.PivotLocal;
            t.localRotation = Quaternion.AngleAxis(angle, axis);
        }

        /// <summary>Bir kare ilerletir; hareket bu karede bittiyse true.</summary>
        public bool Tick(float dt)
        {
            if (settled) return false;
            float seconds = BoardViewUtil.Num("motion.board.folderS", 0.35f);
            progress = seconds <= 0f ? target : Mathf.MoveTowards(progress, target, dt / seconds);
            float e = BoardViewUtil.InOut(progress);
            root.localPosition = Vector3.LerpUnclamped(closedPos, readPos, e);
            root.localRotation = Quaternion.SlerpUnclamped(closedRot, readRot, e);
            if (!Mathf.Approximately(progress, target))
            {
                ApplyCover(CoverAngle(progress));
                return false;
            }
            settled = true;
            if (cover != null)
            {
                float end = Mathf.Abs(cover.RangeTo) > Mathf.Abs(cover.RangeFrom) ? cover.RangeTo : cover.RangeFrom;
                cover.Set(target >= 1f ? end : 0f);   // açıkta modelin kendi uç değeri (−176°), kapalıda 0
            }
            if (target <= 0f) { root.localPosition = closedPos; root.localRotation = closedRot; }
            return true;
        }
    }

    /// <summary>
    /// Bir sayfa tuvaline (1 birim = 1 mm) yukarıdan aşağı satır dizer; satırlar şablon TMP'nin kopyalarıdır ve yeniden
    /// kullanılır (sayfa yenilenince fazlası gizlenir).
    /// </summary>
    internal sealed class PageRows
    {
        readonly RectTransform page;
        readonly TMP_Text template;
        readonly List<TMP_Text> pool = new List<TMP_Text>();
        int used;
        float y;

        public PageRows(RectTransform page, TMP_Text template) { this.page = page; this.template = template; }

        public float Width => page.rect.width;
        public float Height => page.rect.height;
        public float Y => y;

        public void Begin(float top) { used = 0; y = top; }
        public void Advance(float dy) => y += dy;
        public void Cursor(float at) => y = at;

        public void End()
        {
            for (int i = used; i < pool.Count; i++) if (pool[i] != null && pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);
        }

        /// <summary>Sıradaki satır kopyası (etkinleştirilir: TMP ölçüsü için uyanmış olmalı; sayfa etkin olmalı).</summary>
        TMP_Text Next()
        {
            TMP_Text t;
            if (used < pool.Count && pool[used] != null) t = pool[used];
            else
            {
                t = UnityEngine.Object.Instantiate(template, page, false);
                t.raycastTarget = false;
                t.enableAutoSizing = false;
                if (used < pool.Count) pool[used] = t; else pool.Add(t);
            }
            used++;
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            return t;
        }

        static void Style(TMP_Text t, float size, TMP_FontAsset font, bool wrap)
        {
            if (font != null && t.font != font) { t.font = font; t.fontSharedMaterial = font.material; }
            if (!Mathf.Approximately(t.fontSize, size)) t.fontSize = size;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        }

        /// <summary>Metnin bu genişlikteki yüksekliği (mm), yazmadan; şablonun ölçüsüyle.</summary>
        public float Measure(string text, float width, float size, TMP_FontAsset font, bool wrap)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            var t = Next();
            used--;   // yalnız ölçü için ödünç
            Style(t, size, font, wrap);
            return t.GetPreferredValues(text, width, 0f).y;
        }

        /// <summary>
        /// Satırı imlecin satırına, <paramref name="x"/>'ten <paramref name="width"/> genişlikte yazar (imleci ilerletmez; çağıran
        /// <see cref="Advance"/> der). Sayfanın <paramref name="limit"/>'i aşılırsa yazmaz ve −1 döner; yoksa yüksekliği (mm).
        /// </summary>
        public float Put(string text, float x, float width, float size, Color colour, TMP_FontAsset font, FontStyles style, bool wrap,
            float limit, float yOffset = 0f)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            var t = Next();
            Style(t, size, font, wrap);
            float h = t.GetPreferredValues(text, width, 0f).y;
            if (y + h > limit) { used--; return -1f; }
            t.fontStyle = style;
            t.color = colour;
            t.overflowMode = TextOverflowModes.Overflow;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.text = text;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(width, h);
            rt.anchoredPosition3D = new Vector3(x, -(y + yOffset), 0f);
            return h;
        }
    }
}
