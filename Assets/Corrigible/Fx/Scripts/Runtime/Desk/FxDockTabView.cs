using System;
using Corrigible.Core;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxDockTabView : MonoBehaviour
    {
        [SerializeField] public string id;
        [SerializeField] public FxTheme theme;
        [SerializeField] public TMP_Text label,key,badgeText;
        [SerializeField] public Button button;
        [SerializeField] public Image edge,accent,badge;
        [SerializeField] public string accentKey;
        [SerializeField] public float minimumWidth;
        public event Action<string> Clicked;
        public float Width {get;private set;}
        void OnEnable()=>button.onClick.AddListener(Click);
        void OnDisable()=>button.onClick.RemoveListener(Click);
        void Click()=>Clicked?.Invoke(id);
        public void Bind(SadeDockTabVM vm,bool selected)
        {
            label.text=Loc.T(vm.Label);key.text=vm.Key;Width=Mathf.Max(minimumWidth,13+17+9+label.preferredWidth+9+18+14);((RectTransform)transform).sizeDelta=new Vector2(Width,44);
            key.rectTransform.anchoredPosition=new Vector2(Width-33,-13);accent.color=theme.Color(accentKey);edge.color=theme.Color(selected?accentKey:"line");
            badge.gameObject.SetActive(!string.IsNullOrEmpty(vm.BadgeType));badge.color=theme.Color(vm.BadgeType=="count"?"am":"rd");badgeText.text=vm.BadgeType=="count"?vm.BadgeValue.ToString():"";badge.rectTransform.anchoredPosition=new Vector2(Width-9,vm.BadgeType=="count"?8:5);badge.rectTransform.sizeDelta=Vector2.one*(vm.BadgeType=="count"?18:12);
        }
    }
}
