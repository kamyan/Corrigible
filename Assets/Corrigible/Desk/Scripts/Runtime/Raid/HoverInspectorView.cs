using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Desk
{
    public sealed class HoverInspectorView : MonoBehaviour
    {
        [SerializeField] Image thumbnail;
        [SerializeField] GameObject winBox;
        [SerializeField] TMP_Text winStamp,winText;
        [SerializeField] TMP_Text title;
        [SerializeField] Image[] pips, icons;
        [SerializeField] TMP_Text[] text, detail;
        [SerializeField] RectTransform[] rows;
        public void Render(RaidInspectorModel model)
        {
            gameObject.SetActive(model!=null);if(model==null)return;
            if(winBox!=null){winBox.SetActive(model.Win);winStamp.text=Corrigible.Core.Loc.T("HEDEF KIRILIR");winText.text=Corrigible.Core.Loc.T("operasyon kazanılır");}
            title.text=model.Title;DeskSprites.Apply(thumbnail,model.Thumb);thumbnail.enabled=!string.IsNullOrEmpty(model.Thumb);
            RaidHudView.Pips(pips,model.Hp,model.Max,"ink.red");
            ((RectTransform)transform).sizeDelta=new Vector2(258,Mathf.Max(90,60+30*model.Rows.Length)+(model.Win?36:0));
            for(int i=0;i<rows.Length;i++)
            {
                rows[i].gameObject.SetActive(i<model.Rows.Length);if(i>=model.Rows.Length)continue;
                var row=model.Rows[i];text[i].text=row.Text;detail[i].text=row.Detail;
                DeskSprites.Apply(icons[i],row.Icon);icons[i].color=DeskTheme.Active.Get(row.Good?"ink.green":"ink.red");
                detail[i].color=icons[i].color;
            }
        }
    }
}
