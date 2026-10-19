using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Yazılmış panel parçalarının anlamsal kimlikleri; hiyerarşide ad araması yapılmaz.</summary>
    public sealed class FxPanelBindings : MonoBehaviour
    {
        [Serializable] public sealed class Slot
        {
            public string Key; public RectTransform Rect; public TMP_Text Text; public Image Image;
            public FxDeskCommandTarget Action; public OrbView Orb; public FxScale Scale; public FxLineGraphic Line;
        }
        [SerializeField] public Slot[] slots=Array.Empty<Slot>();
        Dictionary<string,Slot> lookup;
        public Slot this[string key]{get{if(lookup==null){lookup=new Dictionary<string,Slot>(slots.Length);foreach(var s in slots)lookup.Add(s.Key,s);}return lookup[key];}}
        public void Text(string key,string value){value=value??"";var t=this[key].Text;if(t.text!=value)t.text=value;}
        public void Active(string key,bool on){var r=this[key].Rect;if(r.gameObject.activeSelf!=on)r.gameObject.SetActive(on);}
        public void Tint(string key,Color value){var s=this[key];if(s.Image!=null)s.Image.color=value;if(s.Text!=null)s.Text.color=value;}
    }
}
