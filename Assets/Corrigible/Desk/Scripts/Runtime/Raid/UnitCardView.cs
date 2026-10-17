using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Desk
{
    public sealed class UnitCardView : MonoBehaviour
    {
        [SerializeField] Image thumb, pointer;
        [SerializeField] TMP_Text title, effects;
        [SerializeField] Image[] pips, rowFill, rowFrame, rowCheck;
        [SerializeField] TMP_Text[] effectLabels;
        [SerializeField] Image[] effectIcons;
        [SerializeField] TMP_Text[] rowName, rowDetail;
        [SerializeField] Button[] rowButton;
        [SerializeField] RectTransform[] rows;
        public void Bind(IRaidCommands commands) { rowButton[1].onClick.RemoveAllListeners(); rowButton[1].onClick.AddListener(commands.ArmAbility); }
        public void Render(RaidViewModel vm)
        {
            gameObject.SetActive(vm.UnitCard!=null); if(vm.UnitCard==null)return;
            var unit=vm.UnitCard; title.text=unit.Name; DeskSprites.Apply(thumb,"thumb_standee_"+unit.Letter+"_card");
            RaidHudView.Pips(pips,unit.Hp,unit.Max,"ink.ink");
            var rect=(RectTransform)transform;float height=unit.Letter=='A'?268:216;
            rect.sizeDelta=new Vector2(360,height);
            // Kart her ekip sütununa bağlanır; alt kenar ve işaretçi sabit kalır.
            rect.anchoredPosition=new Vector2(16+206*System.Array.FindIndex(vm.Team,x=>x.Letter==unit.Letter),120+height);
            for(int i=0;i<rows.Length;i++)
            {
                rows[i].gameObject.SetActive(i<vm.UnitRows.Length);if(i>=vm.UnitRows.Length)continue;
                var row=vm.UnitRows[i];rowName[i].text=row.Text;rowDetail[i].text=row.Detail;
                rowCheck[i].enabled=row.Used;
                DeskSprites.Apply(rowFrame[i],row.Used?"sh_dash_r2_1":row.Armed?"sh_frame_r2_2":"sh_frame_r2_1");
                rowFill[i].color=DeskTheme.Active.Get(row.Armed?"sade.highlighter":"sade.index");
                rowName[i].color=DeskTheme.Active.Get(row.Used?"ink.ink3":"ink.ink");
                rowButton[i].interactable=i==1&&unit.Ability&&!vm.Locked;
            }
            effects.text="";
            for(int i=0;i<effectLabels.Length;i++){effectLabels[i].transform.parent.gameObject.SetActive(i<vm.Effects.Length);if(i<vm.Effects.Length)effectLabels[i].text=vm.Effects[i];effectIcons[i].enabled=unit.Letter=='A'||i==1;effectIcons[i].rectTransform.localScale=new Vector3(unit.Letter=='A'?-1:1,1,1);var er=(RectTransform)effectLabels[i].transform.parent;er.anchoredPosition=new Vector2(52+i*108,-height+38);}
            var e=(RectTransform)effects.transform;e.anchoredPosition=new Vector2(52,-height+36);
        }
    }
}
