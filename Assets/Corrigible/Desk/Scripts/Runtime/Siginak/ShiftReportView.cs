using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Vardiya raporunun okuma görünümü (Revizyon 2 · S5; Overlay, sol 24 · üst 274, 540 px, CSS −0,5°): teleks kâğıdı
    /// (#EFE4BC), sol kenarda delik şeridi, KAPANDI damgası; karşı saldırıda başta çift çizgili kutu; üç göz (ERZAK · YZ
    /// ALARMI ölçekle · FON) ve alarm notu; FON, ÜRETİM VE TÜKETİM (+ analist notu), EKİP, YAPILAMADI; “Enter · VARDİYA
    /// 015'İ BAŞLAT” ve “Esc rapor EVRAK tepsisine dosyalanır”. Yukarıdan iner (motion.reportDropS), başlık daktiloyla
    /// yazılır, gözler sırayla belirir (motion.summaryStaggerS), kutu damga gibi basılır (motion.fatalStampS); dosyalanınca
    /// EVRAK tepsisine kayar (motion.reportFileS). Tepsiden açılınca ← → ile son 5 rapor. Satırlar sahnede hazır yuvalara
    /// yazılır (çalışırken hiyerarşi yaratılmaz).
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
            /// <summary>Satırın ekip simgesi; kapı satırında gizlenir (yer tutar, hizalama bozulmaz).</summary>
            public Image icon;
            [Tooltip("Sağdaki damganın kenarı (HAZIR yeşil, öteki mürekkep).")]
            public Image valueFrame;
        }

        [Serializable]
        public sealed class Eye
        {
            public GameObject root;
            public CanvasGroup group;
            public UiHoverTarget hover;
            public TMP_Text label, delta, value, line;
        }

        [Tooltip("Kayan ve dönen kâğıt (gölge + kâğıt); pivot üst orta.")]
        [SerializeField] RectTransform sheet;
        [SerializeField] RectTransform paper, shadow;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text head, code, title, sub, stamp;
        [Header("Karşı saldırı")]
        [SerializeField] GameObject counterBox;
        [SerializeField] RectTransform counterStamp;
        [SerializeField] UiHoverTarget counterHover;
        [SerializeField] TMP_Text counterTitle, counterCode, counterText;
        [Header("Özet")]
        [SerializeField] Eye[] eyes = Array.Empty<Eye>();
        [Tooltip("Alarm ölçeğinin hücreleri, soldan sağa.")]
        [SerializeField] Image[] alarmCells = Array.Empty<Image>();
        [SerializeField] Sprite cellFill, cellFrame;
        [SerializeField] TMP_Text alarmNote;
        [Header("Döküm")]
        [SerializeField] TMP_Text fundsTitle, productionTitle, teamTitle, failedTitle;
        [SerializeField] RowSlot[] fundsRows = Array.Empty<RowSlot>();
        [SerializeField] RowSlot[] productionRows = Array.Empty<RowSlot>();
        [SerializeField] TMP_Text analyst;
        [SerializeField] GameObject teamSection, failedSection;
        [SerializeField] TeamSlot[] teamRows = Array.Empty<TeamSlot>();
        [SerializeField] TMP_Text[] failures = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text moreFailures;
        [Header("Düğme")]
        [SerializeField] Button action;
        [SerializeField] UiHoverTarget actionHover;
        [SerializeField] RectTransform actionFace;
        [SerializeField] Image actionShadow;
        [SerializeField] TMP_Text actionLabel, hint;
        [SerializeField] Button previous, next;
        [Tooltip("Satır genişliği (kâğıt 540 − sol 24 − sağ 20).")]
        [SerializeField] float rowWidth = 496;
        [SerializeField] float rowSpacing = 12;

        enum Phase { Idle, Drop, File }
        Phase phase;
        float phaseAt, typeAt = -1, eyesAt = -1, stampAt = -1;
        Vector2 restPosition;
        float restRotation;
        Vector3 fileFrom, fileTo;
        bool rested, hasCounter, counterOver;
        readonly bool[] eyeOver = new bool[3];
        Action filed;

        static SiginakTheme T => SiginakTheme.Active;

        public bool IsOpen => gameObject.activeSelf && phase != Phase.File;
        public bool Animating => phase != Phase.Idle;
        public event Action ActionClicked, PreviousClicked, NextClicked;
        /// <summary>Özet gözünün üzerine gelindi ya da çıkıldı (0 ERZAK, 1 YZ ALARMI, 2 FON).</summary>
        public event Action<int, bool> EyeHovered;
        /// <summary>Karşı saldırı kutusunun üzerine gelindi.</summary>
        public event Action CounterHovered;
        /// <summary>Karşı saldırı kutusu basıldı (ağır “tak”).</summary>
        public event Action CounterStamped;

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
            restPosition = sheet.anchoredPosition;
            restRotation = sheet.localEulerAngles.z > 180 ? sheet.localEulerAngles.z - 360 : sheet.localEulerAngles.z;
        }

        /// <summary>
        /// Raporu açar; <paramref name="browse"/> tepsiden okuma (← → gezinme), <paramref name="drop"/> yukarıdan inme. Daktilo,
        /// gözlerin sırası ve kutunun damgası yalnız teleksten yeni kopan raporda oynar.
        /// </summary>
        public void Show(ShiftReportData data, bool browse, bool drop)
        {
            Rest();
            gameObject.SetActive(true);
            filed = null;
            Bind(data, browse);
            if (drop && Application.isPlaying && browse)
            {
                phase = Phase.Drop;
                phaseAt = Time.unscaledTime;
                Settle();
                Pose(0);
            }
            else if (drop && Application.isPlaying)
            {
                phase = Phase.Drop;
                phaseAt = Time.unscaledTime;
                float drop0 = Mathf.Max(0.01f, T.Number("motion.reportDropS", 0.35f));
                // Başlık iner inmez daktiloyla; gözler sırayla; kutu en son damga gibi
                typeAt = phaseAt + drop0;
                eyesAt = typeAt + title.text.Length * 0.03f;
                stampAt = hasCounter ? eyesAt + 3 * T.Number("motion.summaryStaggerS", 0.12f) : -1;
                title.maxVisibleCharacters = 0;
                foreach (var e in eyes) if (e.group != null) e.group.alpha = 0;
                if (hasCounter) counterStamp.localScale = Vector3.zero;
                Pose(0);
            }
            else
            {
                phase = Phase.Idle;
                Settle();
                Pose(1);
            }
        }

        /// <summary>Başlık, gözler ve kutu son hâlinde (canlandırmasız).</summary>
        void Settle()
        {
            typeAt = eyesAt = stampAt = -1;
            title.maxVisibleCharacters = 99999;
            foreach (var e in eyes) if (e.group != null) e.group.alpha = 1;
            counterStamp.localScale = Vector3.one;
        }

        /// <summary>Açıkken dil değişti ya da tepside başka rapora geçildi: yerinde yeniden yazar.</summary>
        public void Bind(ShiftReportData d, bool browse)
        {
            head.text = d.Head;
            code.text = d.Code;
            title.text = d.Title;
            sub.text = d.Sub;
            stamp.text = d.Stamp;

            hasCounter = !string.IsNullOrEmpty(d.CounterTitle);
            counterBox.SetActive(hasCounter);
            counterTitle.text = d.CounterTitle ?? "";
            counterCode.text = d.CounterCode ?? "";
            counterText.text = d.CounterText ?? "";

            BindEye(0, d.Supplies);
            BindEye(1, d.Alarm);
            BindEye(2, d.FundsEye);
            for (int i = 0; i < alarmCells.Length; i++)
            {
                var cell = alarmCells[i];
                bool on = i < d.AlarmSteps;
                cell.gameObject.SetActive(on);
                if (!on) continue;
                bool filled = i < d.AlarmCells, high = i + 1 >= d.AlarmHighFrom;
                // Dolu hücre mürekkep (kırmızı bölgede kırmızı); boş hücre çerçeve, kırmızı bölgede kırmızı kenar (10 px'te kesik
                // desen okunmuyor, kenar düz)
                cell.sprite = filled ? cellFill : cellFrame;
                cell.type = Image.Type.Sliced;
                cell.fillCenter = filled;
                cell.color = T.ColorOf(high ? "report.counterAttackLine" : filled ? "ink" : "report.summaryLine");
            }
            alarmNote.gameObject.SetActive(!string.IsNullOrEmpty(d.AlarmNote));
            alarmNote.text = d.AlarmNote ?? "";

            fundsTitle.text = d.FundsTitle;
            productionTitle.text = d.ProductionTitle;
            teamTitle.text = d.TeamTitle;
            failedTitle.text = d.FailedTitle;
            Rows(fundsRows, d.Funds);
            Rows(productionRows, d.Production);
            analyst.gameObject.SetActive(!string.IsNullOrEmpty(d.Analyst));
            analyst.text = d.Analyst ?? "";

            teamSection.SetActive(d.Teams.Count > 0);
            for (int i = 0; i < teamRows.Length; i++)
            {
                var slot = teamRows[i];
                bool on = i < d.Teams.Count;
                slot.root.SetActive(on);
                if (!on) continue;
                var row = d.Teams[i];
                slot.label.text = row.Label;
                slot.value.text = row.Value;
                var ink = T.ColorOf(row.Tone == ReportTone.Good ? "report.positive" : "ink2");
                slot.value.color = ink;
                if (slot.valueFrame != null) slot.valueFrame.color = ink;
                if (slot.icon != null) slot.icon.enabled = !row.HideIcon;
                slot.detail.gameObject.SetActive(!string.IsNullOrEmpty(row.Detail));
                slot.detail.text = row.Detail ?? "";
            }

            failedSection.SetActive(d.Failures.Count > 0);
            string ink3 = ColorUtility.ToHtmlStringRGB(T.ColorOf("report.neutral"));
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
            shadow.sizeDelta = new Vector2(shadow.sizeDelta.x, paper.rect.height);
        }

        void BindEye(int index, ReportEye e)
        {
            if (index >= eyes.Length || e == null) return;
            var slot = eyes[index];
            slot.label.text = e.Label ?? "";
            slot.delta.text = e.Delta ?? "";
            slot.delta.color = T.ColorOf(ReportTones.ColorKey(e.DeltaTone));
            slot.value.text = e.Value ?? "";
            slot.value.color = T.ColorOf(e.Alert ? "report.counterAttackLine" : "ink");
            slot.line.text = e.Line ?? "";
            if (index == 1)
            {
                // Alarm satırı: normalde LF 15 gri; eşikte CP 700 15 kırmızı
                slot.line.color = T.ColorOf(e.Alert ? "report.counterAttackLine" : "report.summaryLine");
                slot.line.fontStyle = e.Alert ? FontStyles.Bold : FontStyles.Normal;
            }
            if (slot.hover != null) slot.hover.Title = e.Hover;
        }

        void Rows(RowSlot[] slots, System.Collections.Generic.List<ReportRow> rows)
        {
            string note = ColorUtility.ToHtmlStringRGB(T.ColorOf("report.neutral"));
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                bool on = i < rows.Count;
                slot.root.SetActive(on);
                if (!on) continue;
                var r = rows[i];
                slot.label.text = string.IsNullOrEmpty(r.Note) ? r.Label : r.Label + " <size=15><color=#" + note + ">" + r.Note + "</color></size>";
                slot.label.fontStyle = r.Total ? FontStyles.Bold : FontStyles.Normal;
                slot.value.text = r.Value;
                slot.value.color = T.ColorOf(ReportTones.ColorKey(r.Tone));
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

        /// <summary>EVRAK tepsisine kayar (dünya noktası ekrana izdüşer), sonra kapanır.</summary>
        public void File(Vector3 world, Camera view, Action done = null)
        {
            if (!gameObject.activeSelf) { done?.Invoke(); return; }
            filed = done;
            if (!Application.isPlaying || view == null) { Close(); return; }
            Pose(1);
            fileFrom = sheet.position;
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
            HoverOff();
            gameObject.SetActive(false);
        }

        void Close()
        {
            phase = Phase.Idle;
            Pose(1);
            HoverOff();
            gameObject.SetActive(false);
            var done = filed;
            filed = null;
            done?.Invoke();
        }

        void HoverOff()
        {
            for (int i = 0; i < eyeOver.Length; i++)
                if (eyeOver[i]) { eyeOver[i] = false; EyeHovered?.Invoke(i, false); }
            counterOver = false;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (phase == Phase.Drop)
            {
                float p = (now - phaseAt) / Mathf.Max(0.01f, T.Number("motion.reportDropS", 0.35f));
                Pose(Mathf.Clamp01(p));
                if (p >= 1) phase = Phase.Idle;
            }
            else if (phase == Phase.File)
            {
                float p = Mathf.Clamp01((now - phaseAt) / Mathf.Max(0.01f, T.Number("motion.reportFileS", 0.4f)));
                float e = p * p;
                sheet.position = Vector3.Lerp(fileFrom, fileTo, e);
                sheet.localScale = Vector3.one * Mathf.Lerp(1, 0.18f, e);
                sheet.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(restRotation, restRotation + 8, e));
                group.alpha = 1 - Mathf.Clamp01((p - 0.6f) / 0.4f);
                if (p >= 1) Close();
                return;
            }
            if (typeAt >= 0)
            {
                title.maxVisibleCharacters = now < typeAt ? 0 : Mathf.FloorToInt((now - typeAt) / 0.03f) + 1;
                if (now - typeAt >= title.text.Length * 0.03f) { title.maxVisibleCharacters = 99999; typeAt = -1; }
            }
            if (eyesAt >= 0)
            {
                float step = Mathf.Max(0.01f, T.Number("motion.summaryStaggerS", 0.12f));
                for (int i = 0; i < eyes.Length; i++)
                    if (eyes[i].group != null) eyes[i].group.alpha = Mathf.Clamp01((now - eyesAt - i * step) / step);
                if (now - eyesAt >= eyes.Length * step) eyesAt = -1;
            }
            if (stampAt >= 0 && now >= stampAt)
            {
                float s = Mathf.Clamp01((now - stampAt) / Mathf.Max(0.01f, T.Number("motion.fatalStampS", 0.12f)));
                if (counterStamp.localScale == Vector3.zero) CounterStamped?.Invoke();
                counterStamp.localScale = Vector3.one * Mathf.Lerp(1.25f, 1, s);
                if (s >= 1) stampAt = -1;
            }
            Hovers();
            ActionButton();
        }

        /// <summary>Özet gözü ve karşı saldırı kutusunun üzerine gelme (bağlayıcıya bildirilir).</summary>
        void Hovers()
        {
            for (int i = 0; i < eyes.Length && i < eyeOver.Length; i++)
            {
                bool over = eyes[i].hover != null && eyes[i].hover.Over;
                if (over == eyeOver[i]) continue;
                eyeOver[i] = over;
                EyeHovered?.Invoke(i, over);
            }
            bool c = hasCounter && counterHover != null && counterHover.Over;
            if (c && !counterOver) CounterHovered?.Invoke();
            counterOver = c;
        }

        /// <summary>BAŞLAT: üzerinde 2 px kalkar (gölge 5), basılı 2 px iner (gölge 1).</summary>
        void ActionButton()
        {
            bool pressed = actionHover.Pressed, over = actionHover.Over && !pressed;
            float lift = pressed ? -2 : over ? 2 : 0, drop = pressed ? 1 : over ? 5 : 3;
            actionFace.anchoredPosition = new Vector2(0, lift);
            actionShadow.rectTransform.anchoredPosition = new Vector2(0, lift - drop);
        }

        /// <summary>İniş: yukarıdan 60 px, hafif dönük ve büyük gelir, yerine oturur; 0 → 1.</summary>
        void Pose(float p)
        {
            float e = 1 - (1 - p) * (1 - p);
            sheet.anchoredPosition = restPosition + new Vector2(0, 60) * (1 - e);
            sheet.localRotation = Quaternion.Euler(0, 0, restRotation + 1.5f * (1 - e));
            sheet.localScale = Vector3.one * Mathf.Lerp(1.02f, 1, e);
            group.alpha = Mathf.Clamp01(p * 3);
        }
    }
}
