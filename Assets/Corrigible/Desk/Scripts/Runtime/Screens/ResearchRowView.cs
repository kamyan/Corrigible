using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Araştırma satırı (C1 §7.1 tablo): durum ikonu, ad bloğu (ad + alt satır) ve sağ blok.
    /// bitti = yeşil ✓ + TAMAM damgası; sürüyor = ▶ + taralı çubuk + yüzde + ⏸; başlatılabilir = ○ + süre + Başlat düğmesi;
    /// kilitli = kilit + “Kilitli”; durdu (İllüzyon ipucu) = ⏸ + çubuk + yüzde + ▶.
    /// </summary>
    public sealed class ResearchRowView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text subText;
        [SerializeField] DeskRow right;
        [SerializeField] StampView doneStamp;
        [SerializeField] HatchBarView bar;
        [SerializeField] TMP_Text percent;
        [SerializeField] DeskButton pauseButton;
        [SerializeField] TMP_Text days;
        [SerializeField] DeskButton startButton;
        [SerializeField] TMP_Text locked;
        [SerializeField] Image separator;

        /// <summary>(proje, başlat?) — başlat ve duraklatılmışta sürdür (▶) true, duraklat (⏸) false.</summary>
        public event System.Action<string, bool> Action;

        void Awake()
        {
            startButton.onClick.AddListener(() => Action?.Invoke(ProjectId, true));
            pauseButton.onClick.AddListener(() => Action?.Invoke(ProjectId, paused));
        }

        public DeskButton StartButton => startButton;
        public DeskButton PauseButton => pauseButton;
        public string ProjectId { get; private set; }
        bool paused;

        /// <summary>Satır yüksekliği (TSV): alt satırı olan 33 (+1 ayraç), olmayan en az 31.</summary>
        public float Height { get; private set; }

        public void Bind(ResearchProjectVM p, string lockedLabel, bool last)
        {
            ProjectId = p.Id ?? p.Name;
            var state = p.State ?? "available";
            paused = state == "paused" || state == "stopped";   // kendiliğinden durmuş (C2) duraklatılmış gibi çizilir
            bool hasSub = !string.IsNullOrEmpty(p.Sub);

            DeskSprites.Apply(icon, state switch
            {
                "done" => "ic_check_14b", "running" => "ic_play_14", "locked" => "ic_lock_14", "paused" or "stopped" => "ic_pause_14", _ => "ic_circle_14",
            });
            DeskUi.Tint(icon, state switch { "done" => "ink.green", "locked" => "ink.ink3", _ => "ink.ink" });

            DeskUi.SetText(nameText, DeskLoc.Instance.T(p.Name));
            DeskUi.Tint(nameText, state switch { "done" => "ink.ink2", "locked" => "ink.ink3", _ => "ink.ink" });
            DeskUi.SetActive(subText, hasSub);
            if (hasSub) DeskUi.SetText(subText, DeskLoc.Instance.T(p.Sub));

            bool done = state == "done", run = state == "running" || paused, avail = state == "available", lck = state == "locked";
            DeskUi.SetActive(doneStamp, done);
            DeskUi.SetActive(bar, run);
            DeskUi.SetActive(percent, run);
            DeskUi.SetActive(pauseButton, run && p.CanPause);
            DeskUi.SetActive(days, avail);
            DeskUi.SetActive(startButton, avail);
            DeskUi.SetActive(locked, lck);
            if (done) doneStamp.Set(DeskLoc.Instance.T("TAMAM"), StampInk.Green, StampSize.Small, p.StampRot, "research." + ProjectId);
            if (run)
            {
                bar.Set(p.Progress);
                DeskUi.SetText(percent, DeskFormat.Percent(p.Progress * 100));
                pauseButton.SetIcon(paused ? "ic_play_12" : "ic_pause_12");
            }
            if (avail)
            {
                DeskUi.SetText(days, DeskLoc.Instance.T(p.Days));
                startButton.SetLabel(p.StartLabel);
                startButton.SetDisabled(p.DisabledReason);
            }
            if (lck) DeskUi.SetText(locked, DeskLoc.Instance.T(lockedLabel));
            right.Layout();

            DeskUi.SetActive(separator, !last);
            float border = last ? 0 : 1;
            Height = Mathf.Max(31, (hasSub ? 29 : 15.6f) + 4 + border);
            var rt = (RectTransform)transform;
            if (!Mathf.Approximately(rt.sizeDelta.y, Height)) rt.sizeDelta = new Vector2(rt.sizeDelta.x, Height);

            // Dikey yerleşim (TSV): ikon ve sağ blok içerik alanında ortalı; ad alt satır varsa üstten 2, yoksa ortalı
            float content = Height - border;
            Top(icon.rectTransform, 0, -content / 2, new Vector2(0, 0.5f));
            Top((RectTransform)right.transform, 0, -content / 2, new Vector2(1, 0.5f), true);
            Top(nameText.rectTransform, 22, hasSub ? -2 : -(content - 15.6f) / 2, new Vector2(0, 1));
            if (hasSub) Top(subText.rectTransform, 22, -17.5f, new Vector2(0, 1));
        }

        static void Top(RectTransform c, float x, float y, Vector2 pivot, bool fromRight = false)
        {
            c.anchorMin = c.anchorMax = new Vector2(fromRight ? 1 : 0, 1);
            c.pivot = pivot;
            c.anchoredPosition = new Vector2(fromRight ? -x : x, y);
        }
    }
}
