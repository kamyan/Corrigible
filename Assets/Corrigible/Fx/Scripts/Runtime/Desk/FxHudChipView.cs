using Corrigible.Core;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxHudChipView : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public TMP_Text caption,value,delta,uncertainty;
        [SerializeField] public FxTrustBadge badge;
        [SerializeField] public FxHexIcon icon;
        [SerializeField] public FxLedBar bar;
        [SerializeField] public float minimumWidth;
        public float Width {get;private set;}
        public void Bind(SadeChipVM vm)
        {
            caption.text=Loc.T(vm.Label);value.text=vm.Percent?DeskFormat.Percent(vm.Value,vm.Decimals):DeskFormat.Number(vm.Value,vm.Decimals);if(vm.Approx)value.text=DeskFormat.Approximate(value.text);
            delta.text=Loc.F("{0}{1}",vm.Delta<0?"↓":"↑",DeskFormat.Number(System.Math.Abs(vm.Delta),1));delta.color=theme.Color(vm.DeltaGood?"mt":"rd");
            uncertainty.text=vm.Uncertainty>0?Loc.F("±{0}",DeskFormat.Number(vm.Uncertainty,0)):"";
            value.color=theme.Color(vm.Trust=="report"?"vi":"tx");badge.trust=FxRegionRules.Trust(vm.Trust);badge.Refresh();badge.label.enabled=false;
            string symbol=vm.Icon;if(string.IsNullOrEmpty(symbol))symbol=vm.Id=="fund"?"fund":vm.Id=="politicalPower"?"power":vm.Id=="globalTrust"?"globe":vm.Id=="dependence"?"chain":"chip";icon.iconKey="ic_"+symbol+"_s";icon.accent=vm.ThreatIcon?"rd":"cy";icon.Refresh();
            float bw=badge.trust==FxTrust.Report?17.2f:badge.trust==FxTrust.Verified?15:16;
            var br=(RectTransform)badge.transform;br.anchoredPosition=new Vector2(caption.rectTransform.anchoredPosition.x+caption.preferredWidth+6,caption.rectTransform.anchoredPosition.y-7.5f);
            var pos=delta.rectTransform.anchoredPosition;pos.x=value.rectTransform.anchoredPosition.x+value.preferredWidth+8+(vm.Uncertainty>0?uncertainty.preferredWidth+8:0);delta.rectTransform.anchoredPosition=pos;
            var up=uncertainty.rectTransform.anchoredPosition;up.x=value.rectTransform.anchoredPosition.x+value.preferredWidth+8;uncertainty.rectTransform.anchoredPosition=up;
            Width=Mathf.Max(minimumWidth,65+Mathf.Max(caption.preferredWidth+bw+6,value.preferredWidth+delta.preferredWidth+16+uncertainty.preferredWidth)+16);((RectTransform)transform).sizeDelta=new Vector2(Width,66);
            bar.gameObject.SetActive(vm.Bar!=null);if(vm.Bar==null)return;
            bar.value=Mathf.Max(0,vm.Bar.Fill>=0?vm.Bar.Fill:0)/100;bar.dangerFrom=1;bar.low=bar.high=0;bar.accent=vm.Id=="singularity"?"ai":"cy";
            if(vm.Bar.Uncertainty.Length==2){bar.low=vm.Bar.Uncertainty[0]/100;bar.high=vm.Bar.Uncertainty[1]/100;}bar.Refresh();
            if(vm.Bar.Hazard.Length==2){var r=bar.danger.rectTransform;r.anchorMin=new Vector2(vm.Bar.Hazard[0]/100,0);r.anchorMax=new Vector2(vm.Bar.Hazard[1]/100,1);r.offsetMin=r.offsetMax=Vector2.zero;}
            bar.pointer.gameObject.SetActive(vm.Bar.Marker>=0);bar.pointerGlow.gameObject.SetActive(vm.Bar.Marker>=0);if(vm.Bar.Marker>=0){bar.pointer.anchorMin=bar.pointer.anchorMax=new Vector2(vm.Bar.Marker/100,.5f);bar.pointerGlow.rectTransform.anchorMin=bar.pointerGlow.rectTransform.anchorMax=new Vector2(vm.Bar.Marker/100,.5f);}
            bar.uncertainty.gameObject.SetActive(vm.Bar.Uncertainty.Length==2);
        }
    }
}
