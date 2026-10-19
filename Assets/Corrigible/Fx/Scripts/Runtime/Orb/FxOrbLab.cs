using System;
using Corrigible.Core;
using Corrigible.Desk;
using Corrigible.Fx.Orb;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Geliştirme sahnesi; otomatik ve elle geçiş aynı FxClock'u kullanır.</summary>
    public sealed class FxOrbLab : FxTimedBehaviour
    {
        [SerializeField] public OrbClock orbClock;
        [SerializeField] public OrbView live;
        [SerializeField] public OrbView[] states;
        [SerializeField] public OrbLevelSlider slider;
        [SerializeField] public FxButton automaticButton;
        [SerializeField] public Button languageButton;
        [SerializeField] public Toggle reducedToggle,freezeToggle;
        [SerializeField] public TMP_Text valueLabel,liveTag;
        [SerializeField] public RectTransform marker;
        [SerializeField] public GameObject developerPanel;
        [SerializeField] public Button developerToggle;
        [SerializeField] public Slider radiusSlider,filamentSlider,fpsSlider;
        [SerializeField] public TMP_InputField seedInput;
        [SerializeField] public TMP_Text developerStats;
        [SerializeField] public Button goldenButton;
        public event Action GoldenRequested;
        public bool Automatic {get;private set;}=true;
        public double TargetLevel {get;private set;}
        double markerStart;
        float markerFrom,markerTo,markerValue;
        int shownNumber=-1;
        protected override void OnEnable()
        {
            base.OnEnable();slider.onValueChanged.AddListener(Manual);automaticButton.button.onClick.AddListener(ToggleAutomatic);languageButton.onClick.AddListener(Language);
            reducedToggle.onValueChanged.AddListener(Reduce);freezeToggle.onValueChanged.AddListener(Freeze);DeskLoc.Instance.Changed+=RefreshLanguage;
            radiusSlider.onValueChanged.AddListener(Radius);filamentSlider.onValueChanged.AddListener(Filament);fpsSlider.onValueChanged.AddListener(Fps);seedInput.onEndEdit.AddListener(Seed);
            developerToggle.onClick.AddListener(ToggleDeveloper);goldenButton.onClick.AddListener(Golden);
            developerPanel.SetActive(false);developerToggle.gameObject.SetActive(Application.isEditor||Debug.isDebugBuild);Automatic=true;live.autoSweep=true;RefreshLanguage();
        }
        protected override void OnDisable()
        {
            base.OnDisable();slider.onValueChanged.RemoveListener(Manual);automaticButton.button.onClick.RemoveListener(ToggleAutomatic);languageButton.onClick.RemoveListener(Language);reducedToggle.onValueChanged.RemoveListener(Reduce);freezeToggle.onValueChanged.RemoveListener(Freeze);DeskLoc.Instance.Changed-=RefreshLanguage;
            radiusSlider.onValueChanged.RemoveListener(Radius);filamentSlider.onValueChanged.RemoveListener(Filament);fpsSlider.onValueChanged.RemoveListener(Fps);seedInput.onEndEdit.RemoveListener(Seed);developerToggle.onClick.RemoveListener(ToggleDeveloper);goldenButton.onClick.RemoveListener(Golden);
        }
        public void SetManual(double level)=>Manual((float)level);
        void Manual(float value)
        {
            Automatic=false;TargetLevel=OrbMath.Clamp(value,0,100);live.autoSweep=false;live.level=TargetLevel;slider.SetValueWithoutNotify((float)TargetLevel);markerStart=clock.Now;markerFrom=markerValue;markerTo=(float)TargetLevel/100;automaticButton.kind=FxButtonKind.Normal;automaticButton.Refresh();RefreshValue();SetMarker(markerFrom);
        }
        void ToggleAutomatic(){Automatic=!Automatic;live.autoSweep=Automatic;if(Automatic){automaticButton.kind=FxButtonKind.Primary;automaticButton.Refresh();}else Manual((float)TargetLevel);}
        void Language()=>DeskLoc.Instance.Toggle();
        void Reduce(bool value)=>clock.Reduced=value;
        void Freeze(bool value){if(value)clock.Freeze();else clock.Resume();}
        void ToggleDeveloper()=>developerPanel.SetActive(!developerPanel.activeSelf);
        public void SetKind(int value){live.kind=(OrbKind)value;live.ResetModel();}
        void Radius(float value){live.radius=value;live.image.rectTransform.sizeDelta=Vector2.one*(float)Math.Round(value*4);live.ResetModel();}
        void Filament(float value){live.filamentScale=value;if(live.Simulation!=null)live.Simulation.FilamentScale=value;}
        void Fps(float value){orbClock.SetFrameCap(Mathf.RoundToInt(value));}
        void Seed(string value){if(int.TryParse(value,out int seed)){live.seed=seed;live.ResetModel();}}
        void Golden()=>GoldenRequested?.Invoke();
        void RefreshLanguage(){if(liveTag!=null)liveTag.characterSpacing=DeskLoc.Instance.Lang==DeskLang.En?0:16;shownNumber=-1;RefreshValue();}
        void RefreshValue(){int v=OrbMath.Round(TargetLevel);if(v==shownNumber)return;shownNumber=v;valueLabel.text=Loc.F("sızma {0}",v);}
        void SetMarker(float value){markerValue=value;var min=marker.anchorMin;var max=marker.anchorMax;min.x=max.x=value;marker.anchorMin=min;marker.anchorMax=max;marker.anchoredPosition=new Vector2(0,marker.anchoredPosition.y);}
        public override void Tick(FxClock source)
        {
            if(source.Frozen)return;
            if(Automatic){TargetLevel=OrbMath.Sweep(orbClock.Elapsed);live.autoSweep=true;slider.SetValueWithoutNotify((float)TargetLevel);SetMarker((float)TargetLevel/100);RefreshValue();}
            else{float p=FxTween.Progress(source.Now,markerStart,.25f,0,source.CompleteEntrances);SetMarker(Mathf.Lerp(markerFrom,markerTo,p));}
            if(developerPanel.activeSelf)developerStats.text=Loc.F("Küre {0} · FPS {1} · CPU {2:0.00} ms · RT {3:0.0} MiB",orbClock.Count,source.Reduced?orbClock.settings.reducedFps:orbClock.CurrentFps,orbClock.LastCpuMs,orbClock.TextureBytes/1048576.0);
        }
    }
}
