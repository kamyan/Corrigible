using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Vardiya raporunun okuma görünümü (K6b, Overlay 600 px): manila teleks kâğıdı, iki yanda delik sırası, üstte kopma
    /// çizgisi, KAPANDI damgası; FON, ÜRETİM VE TÜKETİM (+ analist notu), EKİP TOPARLANMASI, YAPILAMADI bölümleri;
    /// “Enter · VARDİYA 015'İ BAŞLAT” ve “Esc · rapor EVRAK tepsisine dosyalanır”. Teleksten koparılıp 3° dönerek düşer
    /// (motion.reportDropS), dosyalanınca EVRAK tepsisine kayar (motion.reportFileS). Tepsiden açılınca ← → ile son 5 rapor.
    /// Satırlar kurucunun hazırladığı yuvalara yazılır (çalışırken hiyerarşi yaratılmaz).
    /// </summary>
    public sealed class ShiftReportView : MonoBehaviour
    {
        [Serializable]
        public sealed class RowSlot
        {
            public GameObject root;
            public TMP_Text label, value;
            public LayoutElement labelLayout;
            public GameObject badge;
            public TMP_Text badgeText;
            [Tooltip("Toplam satırının üst çizgisi.")]
            public GameObject rule;
        }

        [Serializable]
        public sealed class TeamSlot
        {
            public GameObject root;
            public TMP_Text label, value, detail;
            /// <summary>Satırın ekip simgesi; YZ alarmı satırında gizlenir (yer tutar, hizalama bozulmaz).</summary>
            public Image icon;
        }

        [SerializeField] RectTransform paper;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text head, code, title, sub, stamp;
        [SerializeField] TMP_Text fundsTitle, productionTitle, teamTitle, failedTitle;
        [SerializeField] RowSlot[] fundsRows = Array.Empty<RowSlot>();
        [SerializeField] RowSlot[] productionRows = Array.Empty<RowSlot>();
        [SerializeField] TMP_Text analyst;
        [SerializeField] GameObject teamSection, failedSection;
        [SerializeField] TeamSlot[] teamRows = Array.Empty<TeamSlot>();
        [SerializeField] TMP_Text[] failures = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text moreFailures;
        [SerializeField] Button action;
        [SerializeField] TMP_Text actionLabel, hint;
        [SerializeField] Button previous, next;
        [Tooltip("Satır genişliği (kâğıt 600 − iki yan 46 px).")]
        [SerializeField] float rowWidth = 508;
        [SerializeField] float rowSpacing = 12;

        enum Phase { Idle, Drop, File }
        Phase phase;
        float phaseAt;
        Vector2 restPosition;
        float restRotation;
        Vector3 fileFrom, fileTo;
        bool rested;
        Action filed;

        public bool IsOpen => gameObject.activeSelf && phase != Phase.File;
        public bool Animating => phase != Phase.Idle;
        public event Action ActionClicked, PreviousClicked, NextClicked;

        public void Configure(RectTransform paperRoot, CanvasGroup canvasGroup, TMP_Text headText, TMP_Text codeText, TMP_Text titleText, TMP_Text subText,
            TMP_Text stampText, TMP_Text[] sectionTitles, RowSlot[] funds, RowSlot[] production, TMP_Text analystNote, GameObject teams, TeamSlot[] teamSlots,
            GameObject failed, TMP_Text[] failureLines, TMP_Text more, Button actionButton, TMP_Text actionText, TMP_Text hintText, Button prev, Button nextButton,
            float width, float spacing)
        {
            paper = paperRoot; group = canvasGroup; head = headText; code = codeText; title = titleText; sub = subText; stamp = stampText;
            fundsTitle = sectionTitles[0]; productionTitle = sectionTitles[1]; teamTitle = sectionTitles[2]; failedTitle = sectionTitles[3];
            fundsRows = funds; productionRows = production; analyst = analystNote; teamSection = teams; teamRows = teamSlots;
            failedSection = failed; failures = failureLines; moreFailures = more; action = actionButton; actionLabel = actionText; hint = hintText;
            previous = prev; next = nextButton; rowWidth = width; rowSpacing = spacing;
        }

        void Awake()
        {
            Rest();
            action.onClick.AddListener(() => { if (phase == Phase.Idle) ActionClicked?.Invoke(); });
            previous.onClick.AddListener(() => { if (phase == Phase.Idle) PreviousClicked?.Invoke(); });
            next.onClick.AddListener(() => { if (phase == Phase.Idle) NextClicked?.Invoke(); });
        }

        void Rest()
        {
            if (rested) return;
            rested = true;
            restPosition = paper.anchoredPosition;
            restRotation = paper.localEulerAngles.z > 180 ? paper.localEulerAngles.z - 360 : paper.localEulerAngles.z;
        }

        /// <summary>Raporu açar; <paramref name="browse"/> tepsiden okuma (← → gezinme), <paramref name="drop"/> teleksten düşme.</summary>
        public void Show(ShiftReportData data, bool browse, bool drop)
        {
            Rest();
            gameObject.SetActive(true);
            filed = null;
            Bind(data, browse);
            if (drop && Application.isPlaying)
            {
                phase = Phase.Drop;
                phaseAt = Time.unscaledTime;
                Pose(0);
            }
            else
            {
                phase = Phase.Idle;
                Pose(1);
            }
        }

        /// <summary>Açıkken dil değişti ya da tepside başka rapora geçildi: yerinde yeniden yazar.</summary>
        public void Bind(ShiftReportData d, bool browse)
        {
            var t = SiginakTheme.Active;
            head.text = d.Head;
            code.text = d.Code;
            title.text = d.Title;
            sub.text = d.Sub;
            stamp.text = d.Stamp;
            fundsTitle.text = d.FundsTitle;
            productionTitle.text = d.ProductionTitle;
            teamTitle.text = d.TeamTitle;
            failedTitle.text = d.FailedTitle;
            Rows(fundsRows, d.Funds, t);
            Rows(productionRows, d.Production, t);
            analyst.gameObject.SetActive(!string.IsNullOrEmpty(d.Analyst));
            analyst.text = d.Analyst ?? "";

            teamSection.SetActive(d.Teams.Count > 0);
            for (int i = 0; i < teamRows.Length; i++)
            {
                var slot = teamRows[i];
                bool on = i < d.Teams.Count;
                slot.root.SetActive(on);
                if (!on) continue;
                slot.label.text = d.Teams[i].Label;
                slot.value.text = d.Teams[i].Value;
                if (slot.icon != null) slot.icon.enabled = !d.Teams[i].HideIcon;
                slot.detail.gameObject.SetActive(!string.IsNullOrEmpty(d.Teams[i].Detail));
                slot.detail.text = d.Teams[i].Detail ?? "";
            }

            failedSection.SetActive(d.Failures.Count > 0);
            string ink3 = ColorUtility.ToHtmlStringRGB(t.ColorOf("ink3"));
            for (int i = 0; i < failures.Length; i++)
            {
                bool on = i < d.Failures.Count;
                failures[i].gameObject.SetActive(on);
                if (on) failures[i].text = "<b><color=#" + ink3 + ">" + d.Failures[i].code + "</color></b> " + d.Failures[i].message;
            }
            moreFailures.gameObject.SetActive(!string.IsNullOrEmpty(d.MoreFailures));
            moreFailures.text = d.MoreFailures ?? "";

            actionLabel.text = d.Action;
            hint.text = d.Hint;
            previous.gameObject.SetActive(browse);
            next.gameObject.SetActive(browse);
            LayoutRebuilder.ForceRebuildLayoutImmediate(paper);
        }

        void Rows(RowSlot[] slots, System.Collections.Generic.List<ReportRow> rows, SiginakTheme t)
        {
            string ink2 = ColorUtility.ToHtmlStringRGB(t.ColorOf("ink2"));
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                bool on = i < rows.Count;
                slot.root.SetActive(on);
                if (!on) continue;
                var r = rows[i];
                slot.label.text = string.IsNullOrEmpty(r.Note) ? r.Label : r.Label + " <size=15><color=#" + ink2 + ">" + r.Note + "</color></size>";
                slot.label.fontStyle = r.Total ? FontStyles.Bold : FontStyles.Normal;
                slot.value.text = r.Value;
                slot.value.color = Tone(r.Tone, t);
                slot.rule.SetActive(r.Total);
                bool badge = !string.IsNullOrEmpty(r.Badge);
                slot.badge.SetActive(badge);
                if (badge) slot.badgeText.text = r.Badge;
                // Ad doğal genişliğinde (damga hemen yanına gelir); sığmazsa sarar, değer sağda kalır
                float valueW = slot.value.GetPreferredValues(r.Value).x;
                float badgeW = badge ? slot.badgeText.GetPreferredValues(r.Badge).x + 14 : 0;
                float room = rowWidth - valueW - badgeW - rowSpacing * (badge ? 3 : 2);
                slot.labelLayout.preferredWidth = Mathf.Min(slot.label.GetPreferredValues(slot.label.text).x + 1, Mathf.Max(80, room));
            }
        }

        static Color Tone(ReportTone tone, SiginakTheme t) => tone switch
        {
            ReportTone.Good => t.ColorOf("status.good"),
            ReportTone.Bad => t.ColorOf("status.threat"),
            ReportTone.Neutral => t.ColorOf("ink3"),
            _ => t.ColorOf("ink"),
        };

        /// <summary>EVRAK tepsisine kayar (dünya noktası ekrana izdüşer), sonra kapanır.</summary>
        public void File(Vector3 world, Camera view, Action done = null)
        {
            if (!gameObject.activeSelf) { done?.Invoke(); return; }
            filed = done;
            if (!Application.isPlaying || view == null) { Close(); return; }
            Pose(1);
            fileFrom = paper.position;
            var screen = view.WorldToScreenPoint(world);
            fileTo = screen.z > 0 ? new Vector3(screen.x, screen.y, 0) : fileFrom + Vector3.right * 1200;
            phase = Phase.File;
            phaseAt = Time.unscaledTime;
        }

        /// <summary>Animasyonsuz kapanır (yeni oyun, kayıttan devam).</summary>
        public void Hide()
        {
            filed = null;
            phase = Phase.Idle;
            gameObject.SetActive(false);
        }

        void Close()
        {
            phase = Phase.Idle;
            Pose(1);
            gameObject.SetActive(false);
            var done = filed;
            filed = null;
            done?.Invoke();
        }

        void Update()
        {
            var t = SiginakTheme.Active;
            if (phase == Phase.Drop)
            {
                float p = (Time.unscaledTime - phaseAt) / Mathf.Max(0.01f, t.Number("motion.reportDropS", 0.35f));
                Pose(Mathf.Clamp01(p));
                if (p >= 1) phase = Phase.Idle;
            }
            else if (phase == Phase.File)
            {
                float p = Mathf.Clamp01((Time.unscaledTime - phaseAt) / Mathf.Max(0.01f, t.Number("motion.reportFileS", 0.4f)));
                float e = p * p;
                paper.position = Vector3.Lerp(fileFrom, fileTo, e);
                paper.localScale = Vector3.one * Mathf.Lerp(1, 0.18f, e);
                paper.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(restRotation, restRotation + 8, e));
                group.alpha = 1 - Mathf.Clamp01((p - 0.6f) / 0.4f);
                if (p >= 1) Close();
            }
        }

        /// <summary>Düşme: teleks tarafından (sol üst) 3° fazla dönük gelir, yerine oturur; 0 → 1.</summary>
        void Pose(float p)
        {
            float e = 1 - (1 - p) * (1 - p);
            paper.anchoredPosition = restPosition + new Vector2(-70, 60) * (1 - e);
            paper.localRotation = Quaternion.Euler(0, 0, restRotation - 3 * (1 - e));
            paper.localScale = Vector3.one * Mathf.Lerp(1.03f, 1, e);
            group.alpha = Mathf.Clamp01(p * 3);
        }
    }
}
