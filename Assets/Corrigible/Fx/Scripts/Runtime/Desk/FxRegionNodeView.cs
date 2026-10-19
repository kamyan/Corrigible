using System;
using Corrigible.Core;
using Corrigible.Desk;
using Corrigible.Fx.Orb;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Corrigible.Fx
{
    /// <summary>Küre, değer ve adın tek sahibi. Hit alanı küre yarıçapıdır; etiketler haritayla büyümez.</summary>
    public sealed class FxRegionNodeView : Selectable,IPointerClickHandler,ICanvasRaycastFilter,ISubmitHandler
    {
        [SerializeField] public string regionId;
        [SerializeField] public int regionIndex;
        [SerializeField] public OrbView orb;
        [SerializeField] public FxRegionLayer regionLayer;
        [SerializeField] public TMP_Text valueText,label;
        [SerializeField] public RectTransform chip;
        [SerializeField] public Image chipFill,chipEdge,labelFill,reticle,hoverRing;
        [SerializeField] public FxTrustBadge badge;
        [SerializeField] public FxTheme theme;
        [SerializeField] public Material hoverMaterial;
        [SerializeField] public bool eastEurope,detachedMarkers;
        [SerializeField] public MapCoverFitter cover;
        [SerializeField] public Vector2 mapPoint;
        RegionNodeVM model;
        bool hovered,focused;
        public event Action<string> Clicked;
        public event Action<string,bool> Hovered;
        public double Radius {get;private set;}
        public bool Unknown=>model==null||!model.Value.HasValue||model.Trust=="unknown";
        protected override void OnEnable(){base.OnEnable();orb.DisplayedLevelChanged+=Displayed;DeskLoc.Instance.Changed+=RefreshText;if(cover!=null)cover.Changed+=RefreshLayout;}
        protected override void OnDisable(){orb.DisplayedLevelChanged-=Displayed;DeskLoc.Instance.Changed-=RefreshText;if(cover!=null)cover.Changed-=RefreshLayout;base.OnDisable();}
        public void Bind(RegionNodeVM vm)
        {
            bool first=model==null;model=vm;var a=OrbStateMapper.Map(vm.Value,FxRegionRules.Trust(vm.Trust),vm.Core,vm.Quarantine);Radius=a.Radius;
            bool reset=orb.Simulation==null||orb.kind!=a.Kind;orb.kind=a.Kind;orb.radius=Radius;orb.level=a.Level;
            orb.image.rectTransform.sizeDelta=Vector2.one*OrbMath.Round(Radius*4.4);
            if(reset){orb.ResetModel();if(!first&&orb.Simulation!=null)orb.Simulation.CompleteIntro();}
            var size=(RectTransform)transform;size.sizeDelta=Vector2.one*(float)(Math.Max(Radius,26)*2);
            SetOffset(chip,FxRegionRules.ChipOffset(Radius,eastEurope));SetOffset(label.rectTransform,FxRegionRules.LabelOffset(Radius,eastEurope));
            reticle.rectTransform.sizeDelta=Vector2.one*(float)Radius*3.2f;hoverRing.rectTransform.sizeDelta=Vector2.one*(float)Radius*2.5f;
            badge.trust=FxRegionRules.Trust(vm.Trust);badge.Refresh();badge.label.enabled=false;
            RefreshLayout();RefreshText();RefreshState();Displayed(orb.DisplayedLevel);
        }
        void SetOffset(RectTransform rect,Vector2 point){if(detachedMarkers&&cover!=null){point+=cover.ToCanvas(mapPoint);rect.anchorMin=rect.anchorMax=new Vector2(0,1);}rect.anchoredPosition=new Vector2(point.x,-point.y);}
        public void RefreshLayout(){if(!detachedMarkers)return;double radius=Radius>0?Radius:orb.radius;SetOffset(orb.image.rectTransform,Vector2.zero);SetOffset(chip,FxRegionRules.ChipOffset(radius,eastEurope));SetOffset(label.rectTransform,FxRegionRules.LabelOffset(radius,eastEurope));labelFill.rectTransform.anchorMin=labelFill.rectTransform.anchorMax=new Vector2(0,1);labelFill.rectTransform.anchoredPosition=label.rectTransform.anchoredPosition;}
        void RefreshText()
        {
            if(model==null)return;valueText.text=Unknown?"?":(model.Trust=="report"?Loc.F("≈{0}",model.Value.Value):model.Value.Value.ToString());label.text=Loc.T(model.Label??model.Name);
            float bw=badge.trust==FxTrust.Report?17.2f:badge.trust==FxTrust.Verified?15:16;float vw=valueText.preferredWidth;
            chip.sizeDelta=new Vector2(vw+bw+22,22);var br=(RectTransform)badge.transform;br.anchoredPosition=new Vector2(8+vw+5,-11);
            valueText.rectTransform.sizeDelta=new Vector2(vw,22);label.rectTransform.sizeDelta=new Vector2(label.preferredWidth+12,20);
            labelFill.rectTransform.sizeDelta=label.rectTransform.sizeDelta;labelFill.rectTransform.anchoredPosition=label.rectTransform.anchoredPosition;
            bool hot=model.Value>=60;chipEdge.color=hot?new Color(61/255f,139/255f,1,.7f):new Color(91/255f,228/255f,1,.32f);valueText.color=theme.Color(Unknown?"tx3":hot?"nodeHotText":"tx");
        }
        void Displayed(double level){if(model!=null)regionLayer.Set(regionIndex,level,Unknown,model.Quarantine,model.Selected);}
        public void SetSelected(bool value){if(model==null)return;model.Selected=value;RefreshState();Displayed(orb.DisplayedLevel);}
        void RefreshState()
        {
            bool selected=model!=null&&model.Selected;reticle.gameObject.SetActive(selected||focused);hoverRing.gameObject.SetActive(hovered);labelFill.enabled=selected;label.color=theme.Color(selected?"bg":"cy2");orb.image.material=hovered?hoverMaterial:orb.orbRenderer.settings.composite;
        }
        public bool IsRaycastLocationValid(Vector2 point,Camera camera){if(!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,point,camera,out var local))return false;return local.sqrMagnitude<=Math.Pow(Math.Max(Radius,26),2);}
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&IsInteractable())Clicked?.Invoke(regionId);}
        public void OnSubmit(BaseEventData e){if(IsInteractable())Clicked?.Invoke(regionId);}
        public override void OnPointerEnter(PointerEventData e){base.OnPointerEnter(e);hovered=true;RefreshState();Hovered?.Invoke(regionId,true);}
        public override void OnPointerExit(PointerEventData e){base.OnPointerExit(e);hovered=false;RefreshState();Hovered?.Invoke(regionId,false);}
        public override void OnSelect(BaseEventData e){base.OnSelect(e);focused=true;RefreshState();}
        public override void OnDeselect(BaseEventData e){base.OnDeselect(e);focused=false;RefreshState();}
    }
}
