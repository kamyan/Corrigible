using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Üst ray (C1 §7.1): sol grup (plaka, rakip etiketi), orta grup (takvim, saat, hız tuşları), sağ grup (canlılık, gizlilik
    /// damgası, ayar tuşu, Esc fişi). Yerleşim prefab'da; metin genişliğine bağlı satırlar <see cref="DeskRow"/> ile dizilir.
    /// </summary>
    public sealed class TopRailView : MonoBehaviour
    {
        [SerializeField] TMP_Text plaqueTitle, plaqueSub;
        [SerializeField] DeskRow rivalPlate;
        [SerializeField] StampView rivalStamp;
        [SerializeField] TMP_Text rivalName, rivalProfile;
        [SerializeField] TMP_Text calendarHeader, calendarDay, clock;
        [Tooltip("Sırasıyla: duraklat, 1×, 2×, 4×.")]
        [SerializeField] SpeedKeyView[] speeds = System.Array.Empty<SpeedKeyView>();
        [SerializeField] DeskRow livePlate;
        [SerializeField] Image lamp;
        [SerializeField] TMP_Text liveText;
        [SerializeField] DeskRow classPlate;
        [SerializeField] StampView classification;
        [SerializeField] KeyChipView escKey;
        [SerializeField] DeskRow rightGroup;
        [Tooltip("⚙ tuşu: ayarlar kartını açar/kapatır (F8.3).")]
        [SerializeField] UnityEngine.UI.Button settings;

        public UnityEngine.UI.Button Settings => settings;

        static readonly string[] SpeedIds = { "pause", "1x", "2x", "4x" };
        /// <summary>“00:00”…“23:59” önbelleği: saat her kare ilerlerken bellek ayırmaz.</summary>
        static string[] times;

        float minutes, rate;
        int shownMinute = -1;

        static string TimeText(int m)
        {
            if (times == null)
            {
                times = new string[24 * 60];
                for (int i = 0; i < times.Length; i++) times[i] = (i / 60).ToString("00") + ":" + (i % 60).ToString("00");
            }
            return times[((m % times.Length) + times.Length) % times.Length];
        }

        static int ParseMinutes(string t) =>
            t != null && t.Length == 5 && int.TryParse(t.Substring(0, 2), out int h) && int.TryParse(t.Substring(3, 2), out int m) ? h * 60 + m : -1;

        void Update()
        {
            if (rate <= 0) return;
            minutes += rate * Time.unscaledDeltaTime;
            int m = Mathf.Min(24 * 60 - 1, Mathf.FloorToInt(minutes));
            if (m == shownMinute) return;
            shownMinute = m;
            DeskUi.SetText(clock, TimeText(m));
        }

        public SpeedKeyView[] Speeds => speeds;

        public void Bind(RailVM rail, ClockVM clockVm)
        {
            if (rail != null)
            {
                DeskUi.SetText(plaqueTitle, DeskLoc.Instance.T(rail.PlaqueTitle));
                DeskUi.SetText(plaqueSub, DeskLoc.Instance.T(rail.PlaqueSub));
                if (rail.Rival != null)
                {
                    rivalStamp.Set(DeskLoc.Instance.T(rail.Rival.Stamp), StampInk.Red, StampSize.Small, rail.Rival.StampRot, "rail.rival");
                    DeskUi.SetText(rivalName, rail.Rival.Name);
                    DeskUi.SetText(rivalProfile, DeskLoc.Instance.T(rail.Rival.Profile));
                    rivalPlate.Layout();
                }
                if (rail.Live != null)
                {
                    DeskSprites.Apply(lamp, rail.Live.On ? "deco_lamp_on" : "deco_lamp_off");
                    DeskUi.SetText(liveText, DeskLoc.Instance.T(rail.Live.Text));
                }
                classification.Set(DeskLoc.Instance.T(rail.Classification), StampInk.Red, StampSize.Small, 0, "rail.class");
                escKey.Set(rail.EscKey ?? "Esc");
                livePlate.Layout();
                classPlate.Layout();
                rightGroup.Layout();
            }
            if (clockVm != null)
            {
                DeskUi.SetText(calendarHeader, DeskLoc.Instance.T(clockVm.DayLabel));
                DeskUi.SetText(calendarDay, clockVm.DayText);
                DeskUi.SetText(clock, clockVm.Time);
                // Oyunda saat bağlamalar arasında yerelde ilerler (gün sonunda 23:59'da durur, yeni günde VM sıfırlar)
                int start = ParseMinutes(clockVm.Time);
                rate = clockVm.Paused || start < 0 ? 0 : clockVm.MinutesPerSecond;
                minutes = start; shownMinute = start;
                for (int i = 0; i < speeds.Length && i < SpeedIds.Length; i++)
                    speeds[i].SetOn(clockVm.Paused ? i == 0 : clockVm.Speed == SpeedIds[i]);
            }
        }
    }
}
