using System.Globalization;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>R11: tıklanmayan ozalit anteti, üç sektörün anahtar planı ve bina notunun 400 ms hover kâğıdı.</summary>
    public sealed class RaidBlueprintView : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField] TMP_Text heading,building,room,region,scale,stamp,noteTitle,noteText;
        [SerializeField] Image[] planSelections;
        [SerializeField] RectTransform stampRoot;
        [SerializeField] GameObject note;
        float stampedAt=-1,enteredAt;
        bool stamped,over;
        static RaidTheme T=>RaidTheme.Active;

        void OnEnable(){stamped=false;stampedAt=-1;over=false;if(note)note.SetActive(false);}
        public void Render(RaidViewModel vm)
        {
            var culture=CultureInfo.GetCultureInfo(Loc.Lang==Loc.Turkish?"tr-TR":"en-US");
            heading.text=Loc.F("OZALİT KOPYA · PAFTA {0}/3",vm.BlueprintSheet);
            building.text=(vm.BlueprintBuilding??string.Empty).ToUpper(culture);
            room.text=Loc.F("{0:00} {1}",vm.BlueprintSheet,vm.BlueprintRoom);
            region.text=vm.BlueprintRegion;
            scale.text=vm.BlueprintShift>0?Loc.F("Ölçek 1:50 · V.{0:000}",vm.BlueprintShift):Loc.T("Ölçek 1:50");
            stamp.text=Loc.T("ASLI GİBİDİR");
            noteTitle.text=Loc.F("BİNA · {0}",vm.BlueprintBuilding);noteText.text=vm.BlueprintNote;
            for(int i=0;i<planSelections.Length;i++)planSelections[i].enabled=i==vm.BlueprintSheet-1;
            if(!stamped){stamped=true;stampedAt=Time.unscaledTime;}
        }
        public void OnPointerEnter(PointerEventData e){over=true;enteredAt=Time.unscaledTime;}
        public void OnPointerExit(PointerEventData e){over=false;if(note)note.SetActive(false);}
        void Update()
        {
            if(stampedAt>=0&&stampRoot){float t=Mathf.Clamp01((Time.unscaledTime-stampedAt)/T.Number("motion.blueprintStampS",.12f));stampRoot.localScale=Vector3.one*Mathf.Lerp(1.6f,1,t*t);if(t>=1)stampedAt=-1;}
            if(note)note.SetActive(over&&Time.unscaledTime-enteredAt>=T.Number("blueprint.hoverS",.4f)&&!string.IsNullOrEmpty(noteText.text));
        }
        void OnDisable(){over=false;if(note)note.SetActive(false);}
    }
}
