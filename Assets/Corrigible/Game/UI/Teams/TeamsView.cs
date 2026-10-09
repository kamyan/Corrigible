using System;
using System.Collections.Generic;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Deneysel mod: saha ekipleri kartı. Ekip listesi ve yeni ekip kurma düğmesi; mod kapalıysa gizlenir.</summary>
    public sealed class TeamsView : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] TMP_Text count;
        [SerializeField] Transform rows;
        [SerializeField] TeamRowView rowPrefab;
        [SerializeField] StyledButton hire;

        public event Action<string> RegionClicked;
        public event Action HireClicked;

        readonly List<TeamRowView> pool = new List<TeamRowView>();

        void Awake() => UiUtil.OnClick(hire.Button, () => HireClicked?.Invoke());

        public void Refresh(Simulation sim)
        {
            bool on = sim != null && sim.TeamsOn;
            if (gameObject.activeSelf != on) gameObject.SetActive(on);
            if (!on) return;
            var teams = sim.S.Teams;
            count.text = $"{teams.Count}/{Simulation.TeamMax}";
            while (pool.Count < teams.Count)
            {
                var r = Instantiate(rowPrefab, rows);
                r.Clicked += id => RegionClicked?.Invoke(id);
                pool.Add(r);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                bool used = i < teams.Count;
                if (pool[i].gameObject.activeSelf != used) pool[i].gameObject.SetActive(used);
                if (used) pool[i].Set(teams[i], theme);
            }
            UiUtil.Show(hire, teams.Count < Simulation.TeamMax);
            hire.SetText($"Yeni ekip kur · {Simulation.TeamHireFon} fon");
            hire.SetEnabled(sim.S.Ending == null && sim.S.Fon >= Simulation.TeamHireFon);
        }
    }
}
