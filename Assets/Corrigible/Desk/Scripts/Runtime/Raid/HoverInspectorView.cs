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
        /// <summary>Satır en az 30 px; uzun tarif (niyet, hedef) alt satıra sarar, satır ve kart onunla uzar.</summary>
        const float RowHeight = 30, RowPad = 8;
        public void Render(RaidInspectorModel model)
        {
            gameObject.SetActive(model!=null);if(model==null)return;
            if(winBox!=null){winBox.SetActive(model.Win);winStamp.text=model.IsLootMission?Corrigible.Core.Loc.T("İKMAL ALINIR"):Corrigible.Core.Loc.T("HEDEF KIRILIR");winText.text=Corrigible.Core.Loc.T("operasyon kazanılır");}
            title.text=model.Title;DeskSprites.Apply(thumbnail,model.Thumb);thumbnail.enabled=!string.IsNullOrEmpty(model.Thumb);
            RaidHudView.Pips(pips,model.Hp,model.Max,RaidHudView.C("labels.hpTag.colors.aiOwned"));
            float y=rows.Length>0?-rows[0].anchoredPosition.y:50;
            for(int i=0;i<rows.Length;i++)
            {
                rows[i].gameObject.SetActive(i<model.Rows.Length);if(i>=model.Rows.Length)continue;
                var row=model.Rows[i];text[i].text=row.Text;detail[i].text=row.Detail;
                DeskSprites.Apply(icons[i],row.Icon);icons[i].color=DeskTheme.Active.Get(row.Good?"ink.green":"ink.red");
                detail[i].color=icons[i].color;
                text[i].textWrappingMode=TextWrappingModes.Normal;text[i].overflowMode=TextOverflowModes.Overflow;
                float h=Mathf.Max(RowHeight,text[i].GetPreferredValues(row.Text,text[i].rectTransform.rect.width,0).y+RowPad);
                rows[i].anchoredPosition=new Vector2(rows[i].anchoredPosition.x,-y);rows[i].sizeDelta=new Vector2(rows[i].sizeDelta.x,h);
                text[i].rectTransform.sizeDelta=new Vector2(text[i].rectTransform.sizeDelta.x,h);detail[i].rectTransform.sizeDelta=new Vector2(detail[i].rectTransform.sizeDelta.x,h);
                y+=h;
            }
            var rt=(RectTransform)transform;rt.sizeDelta=new Vector2(rt.sizeDelta.x,Mathf.Max(90,y+10)+(model.Win?36:0));
        }
    }
}
