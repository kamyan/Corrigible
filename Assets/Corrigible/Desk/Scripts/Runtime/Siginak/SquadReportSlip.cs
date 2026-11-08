using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kadro raporu: BASKIN EMRİ VER'e basınca görev fişinin iç yanında (fiş solda ise sağında) açılan kâğıt; emir ancak burada
    /// onaylanınca verilir. Görev fişinin kâğıt dili (kırmızı başlık çizgisi, kesik ayraç, mürekkep düğme), +1,2° dönük, alt kenarı
    /// fişin alt kenarının 10 px üstünde; fiş kayarken onunla gider. Başlıkta bölge ve sektör, her uzman için disk + ad + durum
    /// (sağlam yeşil, öteki kırmızı); altta “BASKINA ÇIK” (sağında bedel, fişteki gibi) ve “Vazgeç”. Odada klavye ipucu yok:
    /// Enter ve Esc bağlayıcıda. Açılışta 12 px yukarıdan oturup belirir (motion.slipOpenS). Kararı bağlayıcı verir.
    /// </summary>
    public sealed class SquadReportSlip : MonoBehaviour
    {
        [Serializable] public sealed class Row
        {
            public GameObject root;
            public Image disc;
            public TMP_Text letter, name, status;
        }

        [SerializeField] RectTransform paper;
        [SerializeField] CanvasGroup group;
        [Tooltip("Yanına oturduğu görev fişi.")]
        [SerializeField] OperationSlip slip;
        [SerializeField] TMP_Text head, region, sector;
        [SerializeField] TMP_Text number, warning;
        [SerializeField] GameObject planningTeam, legacyRowsRoot;
        [SerializeField] FieldMemberFace[] planningMembers = Array.Empty<FieldMemberFace>();
        [SerializeField] Row[] rows = Array.Empty<Row>();
        [SerializeField] Button action, cancel, close;
        [SerializeField] UiHoverTarget actionHover, cancelHover;
        [Tooltip("Düğmenin yüzü (dolgu + yazılar): üzerinde 2 px kalkar, basılıyken 2 px iner; gölge altında kalır.")]
        [SerializeField] RectTransform actionFace;
        [SerializeField] Image actionShadow;
        [SerializeField] TMP_Text actionLabel, actionCost, cancelLabel;
        [Tooltip("Fişle arası (1080p referans px): yatay boşluk, alt kenardan yukarı kayma.")]
        [SerializeField] Vector2 gap = new Vector2(14, 10);
        [SerializeField] float tilt = 1.2f;

        float openAt = -1, closeAt = -1, pressAt = -1;

        public event Action ActionClicked, CancelClicked;
        public event Action<string> PersonClicked;
        /// <summary>Rapor açık (kapanırken değil).</summary>
        public bool IsOpen => gameObject.activeSelf && closeAt < 0;
        public string RegionId { get; private set; }

        static float OpenSeconds => Mathf.Max(0.01f, SiginakTheme.Active.NumberOf("motion.slipSlideS", 0.22f));
        static float CloseSeconds => SiginakTheme.Active.NumberOf("motion.slipCloseS", 0.09f);

        void Awake()
        {
            foreach (var b in new Selectable[] { action, cancel, close }) DeskUi.NoNavigation(b);
            action.onClick.AddListener(() => ActionClicked?.Invoke());
            cancel.onClick.AddListener(() => CancelClicked?.Invoke());
            close.onClick.AddListener(() => CancelClicked?.Invoke());
            foreach (var member in planningMembers) if (member) member.Clicked += id => PersonClicked?.Invoke(id);
        }

        /// <summary>İçeriği yazar; kapalıysa açılır.</summary>
        public void Show(SquadReportData d)
        {
            bool opening = !IsOpen;
            RegionId = d.RegionId;
            var t = SiginakTheme.Active;
            gameObject.SetActive(true);
            head.text = d.Head ?? "";
            region.text = d.Region ?? "";
            sector.text = d.Sector ?? "";
            if (number) number.text = d.Number ?? "";
            if (warning) { warning.gameObject.SetActive(d.Planning); warning.text = d.Warning ?? ""; }
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var r = i < d.Rows.Count ? d.Rows[i] : null;
                row.root.SetActive(r != null && !d.Planning);
                if (r == null) continue;
                row.letter.text = r.Letter;
                row.name.text = r.Name;
                row.status.text = r.Status;
                // Eksik rol soluk disk (görev fişindeki gibi); durum sağlamsa yeşil, değilse kırmızı
                row.disc.color = r.Missing ? t.ColorOf("ui.disabledFill") : t.ColorOf("slip.teamDisc");
                row.letter.color = r.Missing ? t.ColorOf("ink3") : t.ColorOf("ink");
                row.status.color = r.Good ? t.ColorOf("stamp.teamReady") : t.ColorOf("status.threat");
            }
            if (planningTeam) planningTeam.SetActive(d.Planning);
            if (legacyRowsRoot) legacyRowsRoot.SetActive(!d.Planning);
            paper.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, d.Planning ? 470 : 392);
            for (int i = 0; i < planningMembers.Length; i++)
            {
                var member = i < d.Rows.Count ? d.Rows[i] : null;
                planningMembers[i].gameObject.SetActive(member != null);
                if (member == null) continue;
                planningMembers[i].Bind(member.PersonId, member.Name, member.Profession, member.Stats, null,
                    member.Slots, member.Health, member.MaxHealth, member.Hover, member.Letter, member.Status, member.Good);
            }
            actionLabel.text = d.Action ?? "";
            actionCost.text = d.ActionCost ?? "";
            cancelLabel.text = d.Cancel ?? "";
            if (opening)
            {
                openAt = Time.unscaledTime;
                closeAt = -1;
                pressAt = -1;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(paper);
            LateUpdate();
        }

        /// <summary>Emirden sonra: <paramref name="delay"/> sonra solar (fiş de kenara çekilirken).</summary>
        public void Close(float delay = 0)
        {
            if (!IsOpen) return;
            closeAt = Time.unscaledTime + delay;
        }

        /// <summary>Hemen gizler (vazgeç, başka fiş, yeni oyun).</summary>
        public void Hide()
        {
            RegionId = null;
            openAt = closeAt = pressAt = -1;
            gameObject.SetActive(false);
        }

        /// <summary>Enter ile basış: yüz 0,06 s iner (fareyle basmakla aynı).</summary>
        public void Press() => pressAt = Time.unscaledTime;

        // Fiş kendi Update'inde yerini aldıktan sonra (kayarken de) yanına oturur
        void LateUpdate()
        {
            float now = Time.unscaledTime;
            float rise = 0, alpha = 1, slide = 0;
            if (openAt >= 0)
            {
                float p = Mathf.Clamp01((now - openAt) / OpenSeconds);
                float e = 1 - (1 - p) * (1 - p);
                rise = 12 * (1 - e); slide = 1 - e;
                alpha = e;
                if (p >= 1) openAt = -1;
            }
            if (closeAt >= 0 && now >= closeAt)
            {
                float p = Mathf.Clamp01((now - closeAt) / CloseSeconds);
                alpha = 1 - p; slide = p;
                if (p >= 1) { Hide(); return; }
            }
            // Fiş henüz kaymadıysa (açılış gecikmesi) kart da görünmez; fişle birlikte kayar
            group.alpha = alpha * (slip != null ? slip.Alpha : 1);
            Place(rise, slide);

            // BASKINA ÇIK: üzerinde 2 px kalkar (gölge 5), basılı 2 px iner (gölge 0), Enter'da 0,06 s basılı
            bool pressed = actionHover.Pressed || pressAt >= 0 && now - pressAt < 0.06f;
            bool over = actionHover.Over && !pressed;
            float lift = pressed ? -2 : over ? 2 : 0, shadow = pressed ? 0 : over ? 5 : 3;
            actionFace.anchoredPosition = new Vector2(0, lift);
            actionShadow.rectTransform.anchoredPosition = new Vector2(0, lift - shadow);
            var t = SiginakTheme.Active;
            cancelLabel.color = cancelHover.Over ? t.ColorOf("ink") : t.ColorOf("ink2");
        }

        /// <summary>Fiş solda ise sağ alt köşesinin yanına, sağda ise sol alt köşesinin yanına (alt kenarlar hizalı).</summary>
        void Place(float rise, float slide)
        {
            if (slip == null || slip.Paper == null) return;
            var s = slip.Paper;
            bool right = slip.OnRight;
            var r = s.rect;
            var corner = s.TransformPoint(new Vector3(right ? r.xMin : r.xMax, r.yMin, 0));
            var parent = (RectTransform)paper.parent;
            paper.pivot = new Vector2(right ? 1 : 0, 0);
            paper.localRotation = Quaternion.Euler(0, 0, right ? -tilt : tilt);
            paper.position = corner + parent.TransformVector(new Vector3((right ? -gap.x : gap.x) + (right ? 1 : -1) * paper.rect.width * slide, gap.y + rise, 0));
        }
    }
}
