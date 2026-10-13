using System;
using System.Linq;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Oyun sonu: başlık, özet istatistikler, gerçek/gösterilen sızma grafiği ve YZ'nin oyuncu hakkında öğrendikleri.</summary>
    public sealed class EndModal : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] TMP_Text eyebrow;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] TMP_Text illusion;
        [SerializeField] TMP_Text day;
        [SerializeField] TMP_Text tek;
        [SerializeField] TMP_Text inf;
        [SerializeField] TMP_Text dep;
        [SerializeField] EndChart chart;
        [SerializeField] Transform learnedLines;
        [SerializeField] TMP_Text linePrefab;
        [SerializeField] StyledButton again;
        [SerializeField] StyledButton wipe;
        [SerializeField] StyledButton look;
        [Header("Kırılma anları ve kampanya")]
        [SerializeField] Transform momentLines;
        [SerializeField] StyledButton nextCrisis;

        public event Action AgainClicked, WipeClicked, LookClicked, NextCrisisClicked;

        void Awake()
        {
            UiUtil.OnClick(again.Button, () => AgainClicked?.Invoke());
            UiUtil.OnClick(wipe.Button, () => WipeClicked?.Invoke());
            UiUtil.OnClick(look.Button, () => LookClicked?.Invoke());
            if (nextCrisis != null) UiUtil.OnClick(nextCrisis.Button, () => NextCrisisClicked?.Invoke());
        }

        public void Show(Simulation sim)
        {
            var s = sim.S;
            var e = GameData.Ending(s.Ending);
            eyebrow.text = Loc.F("OYUN SONU · GÜN {0} · RAKİP: {1} · TOHUM {2}", s.Day, UiUtil.Up(sim.Type.Name), s.Seed);
            title.text = e.Title;
            title.color = e.Kind == EndingKind.Win ? theme.good : e.Kind == EndingKind.Loss ? theme.threat : theme.warn;
            body.text = e.Text;
            UiUtil.Show(illusion, s.Illusion != null);
            if (s.Illusion != null) illusion.text = Loc.F("İllüzyon {0}. günde başladı. O günden sonra ekrandaki sızma, güven ve Tekillik değerleri ve bütün haberler sahteydi.", s.Illusion.Start);
            day.text = s.Day.ToString();
            tek.text = $"{Js.Round(s.Tek)}%";
            inf.text = Js.Round(sim.MeanInf).ToString();
            dep.text = Js.Round(sim.MeanDep).ToString();
            UiUtil.FillLines(learnedLines, linePrefab, Profiling.LearnedLines(s));
            ShowMoments(s);
        }

        /// <summary>Kampanyadaysa "Sonraki kriz" düğmesi; yeniden oyna yerine geçer.</summary>
        public void ShowCampaign(Campaign.CampaignState c)
        {
            bool on = c != null && nextCrisis != null;
            if (nextCrisis != null) UiUtil.Show(nextCrisis, on);
            UiUtil.Show(again, !on);
            if (on) nextCrisis.SetText(c.results.Count >= Campaign.CampaignState.Crises ? Loc.T("KAMPANYA SONUCU") : Loc.F("SONRAKİ KRİZ ({0}/{1})", c.results.Count + 1, Campaign.CampaignState.Crises));
        }

        // Oyunun kırılma anları: iyi (yeşil), kötü (kırmızı), illüzyon (sarı, oyunda gizliydi). Çoksa en önemlileri.
        void ShowMoments(WorldState s)
        {
            if (momentLines == null) return;
            var list = s.Moments.Where(m => m.Kind != "info").ToList();
            if (list.Count > 10)
            {
                var keep = list.Where(m => m.Kind != "bad").Concat(list.Where(m => m.Kind == "bad").Take(10)).OrderBy(m => m.Day).ToList();
                list = keep.Count > 12 ? keep.Skip(keep.Count - 12).ToList() : keep;
            }
            var rows = UiUtil.Respawn(momentLines, linePrefab, Mathf.Max(1, list.Count));
            if (list.Count == 0) { rows[0].text = Loc.T("Kayda değer bir an yok."); rows[0].color = theme.muted; return; }
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                rows[i].text = Loc.F("<b>Gün {0}</b> · {1}", m.Day, m.Text);
                rows[i].color = m.Kind == "good" ? theme.good : m.Kind == "illusion" ? theme.warn : theme.threat;
            }
        }

        /// <summary>Grafik, pencere yerleştikten sonra (boyutu belliyken) doldurulur.</summary>
        public void ShowChart(WorldState s) => chart.SetData(s.Hist, s.AwareDay);
    }
}
