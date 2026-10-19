using Corrigible.Core;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    public enum FxTrust { Verified, Report, Suspicious, Unknown, Fake }
    public sealed class FxTrustBadge : MonoBehaviour
    {
        [SerializeField] public FxTheme theme;
        [SerializeField] public Image mark,frame,fill;
        [SerializeField] public TMP_Text label;
        [SerializeField] public FxLoop pulse;
        [SerializeField] public FxTrust trust;
        public static string Caption(FxTrust value) {switch(value){case FxTrust.Verified:return Loc.T("DOĞRULANDI");case FxTrust.Report:return Loc.T("RAPOR");case FxTrust.Suspicious:return Loc.T("ŞÜPHELİ");case FxTrust.Unknown:return Loc.T("BİLİNMİYOR");default:return Loc.T("SAHTE");}}
        void OnEnable(){DeskLoc.Instance.Changed+=Refresh;Refresh();}
        void OnDisable()=>DeskLoc.Instance.Changed-=Refresh;
        public void Refresh()
        {
            if(theme==null)return;
            string key=trust==FxTrust.Verified?"fx_tr_ok":trust==FxTrust.Report?"fx_tr_rap":trust==FxTrust.Suspicious?"fx_tr_sus":"fx_tr_unk";
            if(mark!=null){mark.enabled=trust!=FxTrust.Fake;if(mark.enabled){var e=theme.SpriteInfo(key);theme.Apply(mark,key,Color.white);mark.rectTransform.sizeDelta=e.size;}}
            bool fake=trust==FxTrust.Fake;if(frame!=null){frame.enabled=fake;if(fake)theme.Apply(frame,"fx_frame_1",theme.Color("rd"));}if(fill!=null){fill.enabled=fake;var c=theme.Color("rd");c.a=.16f;fill.color=c;}
            if(label!=null){label.text=Caption(trust);label.color=theme.Color(fake?"rd":trust==FxTrust.Verified?"mt":trust==FxTrust.Report?"vi":trust==FxTrust.Suspicious?"am":"tx3");}
            if(pulse!=null)pulse.enabled=trust==FxTrust.Suspicious;
        }
    }
}
