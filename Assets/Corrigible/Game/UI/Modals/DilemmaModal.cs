using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Olay kartı (ikilem): başlık, durum ve 2–3 seçenek; her seçeneğin altında bedeli ve etkisi.</summary>
    public sealed class DilemmaModal : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] TMP_Text eyebrow;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] ActionButtonView[] choices = Array.Empty<ActionButtonView>();

        public event Action<int> Chosen;

        void Awake()
        {
            for (int i = 0; i < choices.Length; i++) { int k = i; UiUtil.OnClick(choices[i].Styled.Button, () => Chosen?.Invoke(k)); }
        }

        public void Show(Simulation sim)
        {
            var p = sim.PendingEvent;
            if (p == null) return;
            var def = Simulation.Dilemma(p.Id);
            string region = GameData.Region(p.Region).Name;
            eyebrow.text = Loc.F("OLAY · GÜN {0}", sim.S.Day);
            title.text = Loc.F(def.Title, region);
            body.text = Loc.F(def.Body, region);
            for (int i = 0; i < choices.Length; i++)
            {
                bool on = i < def.Choices.Length;
                UiUtil.Show(choices[i], on);
                if (!on) continue;
                var c = def.Choices[i];
                string blocked = sim.ChoiceBlocked(i);
                choices[i].Setup(Loc.T(c.Label), blocked ?? Loc.T(c.Effect));
                choices[i].Styled.SetEnabled(blocked == null);
                choices[i].Sub.color = blocked != null ? theme.threat : theme.muted;
            }
        }
    }
}
