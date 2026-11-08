using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>
    /// KAYIPLAR zarfı (D5): kişi dosyasının arkasında, tel sepette, +50 mm sağda duran kapalı ikinci bir <c>PersonFolder</c>
    /// örneği. Görünen 50 × 180 mm şeritte dikey "KAYIPLAR · n" yazar; kayıp yoksa zarf görünmez. Tıklayınca (sunucu
    /// <see cref="Open"/> der) dosya gibi okuma pozuna gelip açılır, sağ sayfada ad ve "Kayıp · V.nnn" listesi. Ölen kişinin
    /// kartı pano görünürken bu zarfa gider (<see cref="SlotWorld"/>), sonra havuza döner.
    /// </summary>
    public sealed class LostEnvelopeView : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerClickHandler
    {
        [Header("Model (PersonFolder)")]
        [Tooltip("Okuma pozuna giden kök (boşsa bu nesne).")]
        [SerializeField] Transform root;
        [SerializeField] PartDriver cover;
        [Tooltip("Pano modelinin kökü: okuma pozu (personFile.readPose) bu çerçevededir.")]
        [SerializeField] Transform frame;
        [Tooltip("Kayıp yokken gizlenen görsel kök (model gövdesi; bu bileşenin nesnesi olmamalı).")]
        [SerializeField] GameObject body;

        [Header("Kapak ve liste")]
        [Tooltip("Görünen şeritteki dikey yazı (\"KAYIPLAR · n\"; dinamik).")]
        [SerializeField] TMP_Text coverText;
        [Tooltip("Kapalı zarfın tıklama çarpıştırıcısı (bu nesnenin çocuğu, DeskTarget'sız).")]
        [SerializeField] Collider hit;
        [SerializeField] HoverTag hoverTag;
        [Tooltip("Liste sayfası (TXT_PageR yüzeyindeki dünya tuvali, 1 birim = 1 mm).")]
        [SerializeField] RectTransform page;
        [Tooltip("Sol sayfa (kapağın içi) başlığı; boşsa yok.")]
        [SerializeField] TMP_Text pageTitle;
        [Tooltip("Satır şablonu (TextMeshProUGUI, pasif, raycastTarget kapalı).")]
        [SerializeField] TMP_Text rowTemplate;
        [Tooltip("Ölen kartın gittiği nokta, bu nesnenin yerelinde (görünen şeridin ortası).")]
        [SerializeField] Vector3 slotLocal = new Vector3(0.105f, 0.09f, -0.003f);

        FolderMotion motion;
        PageRows rows;
        bool open, interactive = true, hovered, shown = true;
        GameObject pressGo;

        public event Action Clicked;
        public bool IsOpen => open;
        public bool Visible => shown;
        /// <summary>Kapalı zarfın çarpıştırıcısı.</summary>
        public Collider Hit => hit;
        public HoverTag Tag { get => hoverTag; set => hoverTag = value; }
        public string HoverTitle => coverText != null ? coverText.text : null;
        public string Line => null;

        /// <summary>Ölen kişinin kartının zarfa girdiği nokta (dünya).</summary>
        public Vector3 SlotWorld => (root != null ? root : transform).TransformPoint(slotLocal);

        static Color Ink => BoardViewUtil.Col("ink", new Color32(0x1E, 0x1B, 0x16, 0xFF));
        static Color Mute => new Color32(0x5F, 0x57, 0x4A, 0xFF);

        void Awake()
        {
            EnsureMotion();
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            SetPageActive(false);
            UpdateCollider();
        }

        void EnsureMotion()
        {
            if (motion != null) return;
            motion = new FolderMotion(root != null ? root : transform, cover, frame);
            if (page != null && rowTemplate != null) rows = new PageRows(page, rowTemplate);
        }

        /// <summary>Şeridin yazısı ("KAYIPLAR · 2"); boşsa zarf gizlenir (açıksa anında kapanır).</summary>
        public void SetCover(string text)
        {
            BoardViewUtil.SetText(coverText, text);
            BoardViewUtil.SetText(pageTitle, text);
            bool want = !string.IsNullOrEmpty(text);
            if (want == shown) return;
            shown = want;
            if (!want && open) Close();
            if (body != null && body != gameObject && body.activeSelf != want) body.SetActive(want);
            if (coverText != null && coverText.gameObject.activeSelf != want) coverText.gameObject.SetActive(want);
            if (!want) EndHover();
            UpdateCollider();
        }

        /// <summary>Listeyi kurar ve zarfı açar; açıksa yalnız listeyi yeniler.</summary>
        public void Open(IReadOnlyList<LostEntry> list)
        {
            if (!shown) return;
            EnsureMotion();
            SetPageActive(true);
            Build(list);
            if (open) return;
            open = true;
            EndHover();
            motion.SetOpen(true);
            UpdateCollider();
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            EnsureMotion();
            motion.SetOpen(false);
            UpdateCollider();
        }

        /// <summary>K4'e varınca açık; başka pozlarda çarpıştırıcı kapalı.</summary>
        public void SetInteractive(bool on)
        {
            interactive = on;
            if (!on) EndHover();
            UpdateCollider();
        }

        void UpdateCollider()
        {
            if (hit != null) hit.enabled = interactive && shown && !open;
            // Liste sayfasının yüzeyleri yalnız açıkken (bkz. PersonFolderView.UpdateColliders)
            pageSurfaces ??= OtherColliders();
            foreach (var c in pageSurfaces) if (c != null) c.enabled = interactive && open;
        }

        Collider[] pageSurfaces;

        Collider[] OtherColliders()
        {
            var list = new System.Collections.Generic.List<Collider>();
            foreach (var c in GetComponentsInChildren<Collider>(true)) if (c != hit) list.Add(c);
            return list.ToArray();
        }

        void SetPageActive(bool on)
        {
            if (page != null && page.gameObject.activeSelf != on) page.gameObject.SetActive(on);
        }

        void Build(IReadOnlyList<LostEntry> list)
        {
            if (rows == null) return;
            float margin = BoardViewUtil.Num("personFile.page.marginMm", 8f);
            float bodyMm = BoardViewUtil.Num("personFile.page.bodyMm", 4.2f);
            float lineMm = BoardViewUtil.Num("personFile.page.labelMm2", 3.2f);
            float gap = BoardViewUtil.Num("personFile.page.gapMm", 1.4f);
            var desk = DeskTheme.Active;
            var fBold = desk != null ? desk.Font(DeskFont.LfBold) : null;
            var fMono = desk != null ? desk.Font(DeskFont.CpBold) : null;
            float width = rows.Width - 2 * margin, limit = rows.Height - margin;
            rows.Begin(margin);
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e == null) continue;
                    float h = rows.Put(e.Name, margin, width, bodyMm, Ink, fBold, FontStyles.Normal, false, limit);
                    if (h < 0f) break;
                    rows.Advance(h + 0.4f);
                    h = rows.Put(e.Line, margin, width, lineMm, Mute, fMono, FontStyles.Normal, true, limit);
                    if (h < 0f) break;
                    rows.Advance(h + gap * 2f);
                }
            rows.End();
        }

        // ------------------------------------------------------------------ işaretçi

        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactive || open || !shown || Blocked) return;
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

        public void OnPointerDown(PointerEventData eventData) => pressGo = eventData.pointerPressRaycast.gameObject;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !interactive || open || !shown || Blocked) return;
            var go = eventData.pointerCurrentRaycast.gameObject;
            if (hit == null || go == null || go != pressGo || go != hit.gameObject) return;
            EndHover();
            Clicked?.Invoke();
        }

        void Update()
        {
            if (motion != null && motion.Tick(Time.unscaledDeltaTime) && !open) SetPageActive(false);
            if (hovered && (Blocked || !interactive || open)) EndHover();
        }

        void OnDisable() => EndHover();
    }
}
