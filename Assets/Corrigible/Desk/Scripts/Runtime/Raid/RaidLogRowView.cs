using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Desk
{
    public sealed class RaidLogRowView : MonoBehaviour
    {
        [SerializeField] TMP_Text text,header;
        [SerializeField] Image background,rule;
        public float Set(RaidLogModel model,float width)
        {
            bool intercepted=model.Kind=="ai",separator=model.Kind=="sep"||model.Kind=="turn";
            header.gameObject.SetActive(intercepted);header.text=Loc.T("YZ · ELE GEÇİRİLEN SATIR");
            text.text=model.Text;text.color=DeskTheme.Active.Get(model.Kind=="bad"||intercepted?"ink.red":model.Kind=="good"?"ink.green":"ink.ink2");
            text.fontStyle=separator||intercepted?FontStyles.Bold:FontStyles.Normal;
            text.rectTransform.anchoredPosition=new Vector2(8,intercepted?-25:-4);text.rectTransform.sizeDelta=new Vector2(width-16,100);
            float h=Mathf.Max(26,text.GetPreferredValues(model.Text,width-16,float.PositiveInfinity).y+(intercepted?31:10));text.rectTransform.sizeDelta=new Vector2(width-16,h-(intercepted?27:8));
            background.rectTransform.sizeDelta=new Vector2(width,h);rule.rectTransform.sizeDelta=new Vector2(width,1);background.enabled=intercepted;rule.enabled=intercepted||separator;
            ((RectTransform)transform).sizeDelta=new Vector2(width,h);return h;
        }
    }
}
