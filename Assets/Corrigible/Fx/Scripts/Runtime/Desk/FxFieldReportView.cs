using System;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public sealed class FxFieldReportView : MonoBehaviour
    {
        [SerializeField] public Button collect;
        [SerializeField] public TMP_Text title,reward;
        [SerializeField] public SadeMarkerAnchor anchor;
        [SerializeField] public GameObject expiry;
        public FieldReportVM Data {get;private set;}
        public event Action<string> Clicked;
        void Awake()=>collect.onClick.AddListener(Collect);
        void OnDestroy()=>collect.onClick.RemoveListener(Collect);
        public void Bind(FieldReportVM value){Data=value;anchor.SetPoint(new Vector2(value.X,value.Y),Vector2.zero);title.text=value.Title;reward.text=value.RewardText;collect.interactable=!string.IsNullOrEmpty(value.Command);expiry.SetActive(value.Expiring);}
        void Collect(){if(Data==null||!collect.interactable)return;collect.interactable=false;Clicked?.Invoke(Data.Command);}
    }
}
