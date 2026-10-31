using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Operasyon raporunun görünümü (Revizyon 2 · S7c/S7d; Overlay, sol 24 · üst 274, 560 px, CSS −0,4°): kâğıt #F1E9D6,
    /// üstte 3 px çift çizgi, sağ üstte iğne; künye (TARİH, SONUÇ damgası, HEDEF, SÜRE), sonucu söyleyen başlık (geri
    /// alındıysa mavi, işgal altındaysa kırmızı) ve tek cümle, başarılıda KAZANÇ kutusu (dört göz, 32 px yeşil), SONUÇLAR
    /// (başarısızda YZ alarmı ölçekle) ve MASAYA DÖN. Yukarıdan iner (0,4 s), KAZANÇ gözleri sırayla belirir (0,12 s);
    /// dosyalanınca EVRAK tepsisine kayar (0,25 s). Haritadaki ekip figürünün çevresindeki kırmızı halkayı da bu kâğıdın
    /// katmanı çizer (konumu bağlayıcı verir). Satırlar sahnede hazır yuvalara yazılır.
    /// </summary>
    public sealed class OperationReportView : MonoBehaviour
    {
        [Serializable]
        public sealed class GainSlot
        {
            public CanvasGroup group;
            public TMP_Text value, label;
        }

        [Serializable]
        public sealed class RowSlot
        {
            public GameObject root;
            public TMP_Text label, value;
            [Tooltip("Alarm ölçeğinin kökü ve hücreleri (yalnız başarısız raporun alarm satırında).")]
            public GameObject cellsRoot;
            public Image[] cells = Array.Empty<Image>();
        }

        [Tooltip("Kayan ve dönen kâğıt (gölge + kâğıt + iğne); pivot üst orta.")]
        [SerializeField] RectTransform sheet;
        [SerializeField] RectTransform paper, shadow;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text head, number;
        [Header("Künye")]
        [SerializeField] TMP_Text dateLabel;
        [SerializeField] TMP_Text date, resultLabel, resultStamp, targetLabel, target, durationLabel, duration;
        [SerializeField] Image resultStampFrame;
        [Tooltip("Künyenin sağ sütunu (damga ve süre): damga kadar geniş; BAŞARILI'da hedef tek satıra sığar.")]
        [SerializeField] LayoutElement stampColumn, durationColumn;
        [Header("Başlık")]
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text sentence;
        [Header("KAZANÇ ve SONUÇLAR")]
        [SerializeField] GameObject gainsSection;
        [SerializeField] TMP_Text gainsTitle, resultsTitle;
        [SerializeField] GainSlot[] gains = Array.Empty<GainSlot>();
        [SerializeField] RowSlot[] rows = Array.Empty<RowSlot>();
        [SerializeField] Sprite cellFill, cellFrame;
        [Header("Düğme")]
        [SerializeField] Button action;
        [SerializeField] UiHoverTarget actionHover;
        [SerializeField] RectTransform actionFace;
        [SerializeField] Image actionShadow;
        [SerializeField] TMP_Text actionLabel;
        [Header("İğne ve halka")]
        [Tooltip("İpliğin raporda bağlandığı iğne (Ø17 #2A2622).")]
        [SerializeField] RectTransform pin;
        [Tooltip("Ekip figürünün çevresindeki halka (76 × 54, #B8241A); radyal dolguyla çizilir.")]
        [SerializeField] Image ring;
        [Header("Süreler (S7 › hareket)")]
        [SerializeField, Min(0.01f)] float dropSeconds = 0.4f;
        [SerializeField, Min(0.01f)] float fileSeconds = 0.25f;
        [SerializeField, Min(0.01f)] float gainStagger = 0.12f;

        enum Phase { Idle, Drop, File }
        Phase phase;
        float phaseAt, gainsAt = -1;
        Vector2 restPosition;
        float restRotation;
        Vector3 fileFrom, fileTo;
        bool rested;
        Action filed;

        static SiginakTheme T => SiginakTheme.Active;

        public bool IsOpen => gameObject.activeSelf && phase != Phase.File;
        public bool Animating => phase != Phase.Idle;
        public float DropSeconds => dropSeconds;
        public event Action ActionClicked;

        /// <summary>İğnenin ekran noktası (kâğıt yerindeyken).</summary>
        public Vector2 PinScreen => RectTransformUtility.WorldToScreenPoint(null, pin.position);

        void Awake()
        {
            Rest();
            action.onClick.AddListener(() => { if (phase == Phase.Idle) ActionClicked?.Invoke(); });
        }

        void Rest()
        {
            if (rested) return;
            rested = true;
            restPosition = sheet.anchoredPosition;
            restRotation = sheet.localEulerAngles.z > 180 ? sheet.localEulerAngles.z - 360 : sheet.localEulerAngles.z;
        }

        /// <summary>Raporu açar; <paramref name="drop"/> yukarıdan iner, KAZANÇ gözleri sırayla belirir.</summary>
        public void Show(OperationReportData data, bool drop)
        {
            Rest();
            gameObject.SetActive(true);
            filed = null;
            SetRing(null, 0);
            Bind(data);
            if (drop && Application.isPlaying)
            {
                phase = Phase.Drop;
                phaseAt = Time.unscaledTime;
                gainsAt = phaseAt + dropSeconds;
                foreach (var g in gains) if (g.group != null) g.group.alpha = 0;
                Pose(0);
            }
            else
            {
                phase = Phase.Idle;
                gainsAt = -1;
                foreach (var g in gains) if (g.group != null) g.group.alpha = 1;
                Pose(1);
            }
        }

        /// <summary>Açıkken dil değişti: yerinde yeniden yazar.</summary>
        public void Bind(OperationReportData d)
        {
            head.text = d.Head;
            number.text = d.Number;
            dateLabel.text = d.DateLabel;
            date.text = d.Date;
            resultLabel.text = d.ResultLabel;
            resultStamp.text = d.ResultStamp;
            targetLabel.text = d.TargetLabel;
            target.text = d.Target;
            durationLabel.text = d.DurationLabel;
            duration.text = d.Duration;
            float right = Mathf.Ceil(Mathf.Max(resultStamp.GetPreferredValues(d.ResultStamp).x + 20, duration.GetPreferredValues(d.Duration).x));
            stampColumn.minWidth = stampColumn.preferredWidth = durationColumn.minWidth = durationColumn.preferredWidth = right;
            // Damga: BAŞARILI yeşil, BAŞARISIZ kırmızı; başlık: geri alındıysa mavi, işgal altındaysa kırmızı
            var stampInk = T.ColorOf(d.Success ? "report.positive" : "report.counterAttackLine");
            resultStamp.color = stampInk;
            resultStampFrame.color = stampInk;
            title.text = d.Title;
            title.color = T.ColorOf(d.Success ? "status.info" : "report.counterAttackLine");
            sentence.text = d.Sentence;

            gainsSection.SetActive(d.Gains.Count > 0);
            gainsTitle.text = d.GainsTitle;
            for (int i = 0; i < gains.Length; i++)
            {
                bool on = i < d.Gains.Count;
                gains[i].group.gameObject.SetActive(on);
                if (!on) continue;
                gains[i].value.text = d.Gains[i].Value;
                gains[i].value.color = T.ColorOf(d.Gains[i].Zero ? "report.neutral" : "report.positive");
                gains[i].label.text = d.Gains[i].Label;
            }

            resultsTitle.text = d.ResultsTitle;
            for (int i = 0; i < rows.Length; i++)
            {
                var slot = rows[i];
                bool on = i < d.Results.Count;
                slot.root.SetActive(on);
                if (!on) continue;
                var r = d.Results[i];
                slot.label.text = r.Label;
                slot.value.text = r.Value;
                slot.value.color = T.ColorOf(ToneColor(r.Tone));
                Cells(slot, r);
            }
            actionLabel.text = d.Action;
            LayoutRebuilder.ForceRebuildLayoutImmediate(paper);
            shadow.sizeDelta = new Vector2(shadow.sizeDelta.x, paper.rect.height);
        }

        /// <summary>Renk kuralı: düz mürekkep, artı yeşil, değişmedi gri, geri alındı mavi, işgal/eşik/düşen kırmızı.</summary>
        public static string ToneColor(OperationTone tone) => tone switch
        {
            OperationTone.Good => "report.positive",
            OperationTone.Neutral => "report.neutral",
            OperationTone.Retaken => "status.info",
            OperationTone.Alert => "report.counterAttackLine",
            _ => "ink",
        };

        /// <summary>
        /// Alarm ölçeği (S7d): dolu hücre mürekkep; boş hücre çerçeve, kırmızı bölgede kırmızı (10 px'te kesik desen okunmuyor,
        /// kenar düz).
        /// </summary>
        void Cells(RowSlot slot, OperationRow r)
        {
            if (slot.cellsRoot == null) return;
            slot.cellsRoot.SetActive(r.AlarmSteps > 0);
            for (int i = 0; i < slot.cells.Length; i++)
            {
                var cell = slot.cells[i];
                bool on = i < r.AlarmSteps;
                cell.gameObject.SetActive(on);
                if (!on) continue;
                bool filled = i < r.AlarmCells, high = i + 1 >= r.AlarmHighFrom;
                cell.sprite = filled ? cellFill : cellFrame;
                cell.type = Image.Type.Sliced;
                cell.fillCenter = filled;
                cell.color = T.ColorOf(filled ? "ink" : high ? "report.counterAttackLine" : "report.summaryLine");
            }
        }

        /// <summary>
        /// Ekip figürünün çevresindeki halka: <paramref name="screen"/> figürün ekran noktası (null gizler), <paramref name="drawn"/>
        /// 0 → 1 çizilir.
        /// </summary>
        public void SetRing(Vector2? screen, float drawn)
        {
            if (ring == null) return;
            bool on = screen.HasValue && drawn > 0 && gameObject.activeSelf;
            ring.gameObject.SetActive(on);
            if (!on) return;
            var parent = (RectTransform)ring.rectTransform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen.Value, null, out var local)) ring.rectTransform.anchoredPosition = local;
            ring.fillAmount = Mathf.Clamp01(drawn);
        }

        /// <summary>EVRAK tepsisine kayar (dünya noktası ekrana izdüşer), sonra kapanır.</summary>
        public void File(Vector3 world, Camera view, Action done = null)
        {
            if (!gameObject.activeSelf) { done?.Invoke(); return; }
            filed = done;
            SetRing(null, 0);
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
            SetRing(null, 0);
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
            float now = Time.unscaledTime;
            if (phase == Phase.Drop)
            {
                float p = (now - phaseAt) / dropSeconds;
                Pose(Mathf.Clamp01(p));
                if (p >= 1) phase = Phase.Idle;
            }
            else if (phase == Phase.File)
            {
                float p = Mathf.Clamp01((now - phaseAt) / fileSeconds), e = p * p;
                sheet.position = Vector3.Lerp(fileFrom, fileTo, e);
                sheet.localScale = Vector3.one * Mathf.Lerp(1, 0.18f, e);
                sheet.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(restRotation, restRotation + 8, e));
                group.alpha = 1 - Mathf.Clamp01((p - 0.6f) / 0.4f);
                if (p >= 1) Close();
                return;
            }
            if (gainsAt >= 0)
            {
                for (int i = 0; i < gains.Length; i++)
                    if (gains[i].group != null) gains[i].group.alpha = Mathf.Clamp01((now - gainsAt - i * gainStagger) / gainStagger);
                if (now - gainsAt >= gains.Length * gainStagger) gainsAt = -1;
            }
            ActionButton();
        }

        /// <summary>MASAYA DÖN: üzerinde 2 px kalkar (gölge 5), basılı 2 px iner (gölge 1).</summary>
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
