using System;
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

        public event Action AgainClicked, WipeClicked, LookClicked;

        void Awake()
        {
            UiUtil.OnClick(again.Button, () => AgainClicked?.Invoke());
            UiUtil.OnClick(wipe.Button, () => WipeClicked?.Invoke());
            UiUtil.OnClick(look.Button, () => LookClicked?.Invoke());
        }

        public void Show(Simulation sim)
        {
            var s = sim.S;
            var e = GameData.Ending(s.Ending);
            eyebrow.text = $"OYUN SONU · GÜN {s.Day} · RAKİP: {UiUtil.Up(sim.Type.Name)} · TOHUM {s.Seed}";
            title.text = e.Title;
            title.color = e.Kind == EndingKind.Win ? theme.good : e.Kind == EndingKind.Loss ? theme.threat : theme.warn;
            body.text = e.Text;
            UiUtil.Show(illusion, s.Illusion != null);
            if (s.Illusion != null) illusion.text = $"İllüzyon {s.Illusion.Start}. günde başladı. O günden sonra ekrandaki sızma, güven ve Tekillik değerleri ve bütün haberler sahteydi.";
            day.text = s.Day.ToString();
            tek.text = $"{Js.Round(s.Tek)}%";
            inf.text = Js.Round(sim.MeanInf).ToString();
            dep.text = Js.Round(sim.MeanDep).ToString();
            UiUtil.FillLines(learnedLines, linePrefab, Profiling.LearnedLines(s));
        }

        /// <summary>Grafik, pencere yerleştikten sonra (boyutu belliyken) doldurulur.</summary>
        public void ShowChart(WorldState s) => chart.SetData(s.Hist, s.AwareDay);
    }
}
