using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üst ortadaki zaman plakası (PROMPT §A.7): koyu plaka; takvim (60×42, kırmızı “GÜN” başlığı + gün numarası), saat
    /// (CP 700 22, .04em, pirinç), dikey ayraç, dört hız tuşu (C SpeedKeyView; 30 px, aralık 6; seçili olan basılı).
    /// Saat bağlamalar arasında yerelde ilerler (C TopRailView gibi); veri <see cref="ClockVM"/>.
    /// </summary>
    public sealed class ClockPlaqueView : MonoBehaviour
    {
        [SerializeField] SadePaper plaque;
        [SerializeField] Image calendarPaper;
        [SerializeField] Graphic calendarHead;
        [SerializeField] TMP_Text calendarLabel;
        [SerializeField] TMP_Text calendarDay;
        [SerializeField] TMP_Text clock;
        [SerializeField] Graphic divider;
        [SerializeField] SpeedKeyView[] speeds = new SpeedKeyView[4];

        [Header("Önizleme (edit mod; oyunda VM)")]
        [SerializeField] ClockVM sample = new ClockVM { DayLabel = "GÜN", DayText = "074", Time = "14:32", Speed = "1x" };

        static readonly string[] SpeedIds = { "pause", "1x", "2x", "4x" };
        float rate, minutes;
        int shownMinute = -1;
        bool bound;

        public SpeedKeyView[] Speeds => speeds;
        /// <summary>Hız tuşuna basıldı: 0 duraklat, 1–3 = 1× / 2× / 4×.</summary>
        public event Action<int> SpeedClicked;

        void Awake()
        {
            for (int i = 0; i < speeds.Length; i++)
            {
                int k = i;
                if (speeds[i] != null) speeds[i].onClick.AddListener(() => SpeedClicked?.Invoke(k));
            }
        }

        void OnEnable() { if (!bound) Render(sample); }
        void OnValidate() => DeskUi.Deferred(this, () => { if (!bound) Render(sample); });

        /// <summary>Önizleme verisini çizer (kurucu ve galeri).</summary>
        public void ShowSample() => Render(sample);

        public void Bind(ClockVM vm)
        {
            bound = true;
            Render(vm);
        }

        void Render(ClockVM vm)
        {
            if (vm == null) return;
            if (plaque != null) plaque.Apply();
            SadeUi.Sprite(calendarPaper, "sh_fill_r2", "sade.paper");
            DeskUi.Tint(calendarHead, "sade.status.threat");
            SadeUi.Type(calendarLabel, "calHead");
            DeskUi.Tint(calendarLabel, "sade.desk.time.calHeadText");
            DeskUi.SetText(calendarLabel, DeskLoc.Instance.T(string.IsNullOrEmpty(vm.DayLabel) ? "GÜN" : vm.DayLabel));
            SadeUi.Type(calendarDay, "calNum");
            DeskUi.Tint(calendarDay, "sade.ink");
            DeskUi.SetText(calendarDay, vm.DayText);
            SadeUi.Type(clock, "clock");
            DeskUi.Tint(clock, "sade.plaque.clock");
            DeskUi.SetText(clock, vm.Time);
            DeskUi.Tint(divider, "sade.plaque.divider");

            int start = ParseMinutes(vm.Time);
            rate = vm.Paused || start < 0 ? 0 : vm.MinutesPerSecond;
            minutes = start; shownMinute = start;
            for (int i = 0; i < speeds.Length && i < SpeedIds.Length; i++)
                if (speeds[i] != null) speeds[i].SetOn(vm.Paused ? i == 0 : vm.Speed == SpeedIds[i]);
        }

        void Update()
        {
            if (rate <= 0) return;
            minutes += rate * Time.unscaledDeltaTime;
            int m = Mathf.Min(24 * 60 - 1, Mathf.FloorToInt(minutes));
            if (m == shownMinute) return;
            shownMinute = m;
            DeskUi.SetText(clock, (m / 60).ToString("00") + ":" + (m % 60).ToString("00"));
        }

        static int ParseMinutes(string t) =>
            t != null && t.Length == 5 && int.TryParse(t.Substring(0, 2), out int h) && int.TryParse(t.Substring(3, 2), out int m) ? h * 60 + m : -1;
    }
}
