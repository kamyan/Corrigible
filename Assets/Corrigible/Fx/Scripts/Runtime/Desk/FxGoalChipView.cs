using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxGoalChipView : MonoBehaviour
    {
        [SerializeField] public TMP_Text[] labels,values;
        [SerializeField] public FxLedBar[] bars;
        public void Bind(List<SadeGoalVM> goals){for(int i=0;i<goals.Count&&i<bars.Length;i++){labels[i].text=Loc.T(goals[i].Label);values[i].text=DeskFormat.Percent(goals[i].Percent);bars[i].value=goals[i].Percent/100;bars[i].dangerFrom=1;bars[i].low=bars[i].high=0;bars[i].Refresh();bars[i].pointer.gameObject.SetActive(false);bars[i].pointerGlow.gameObject.SetActive(false);bars[i].uncertainty.gameObject.SetActive(false);}}
    }
}
