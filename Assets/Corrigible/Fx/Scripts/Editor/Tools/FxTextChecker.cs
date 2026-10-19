using System;
using System.IO;
using System.Text;
using Corrigible.Desk;
using TMPro;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Fx.EditorTools
{
    /// <summary>Bütün etkin FX metinleri, batch kaynakları ve dünya etiketleri dahil; iki dilde yazı kabulü.</summary>
    public static class FxTextChecker
    {
        [MenuItem("Tools/FX UI/Check Text")]
        static void Menu()=>Debug.Log(Run());
        public static string Run()
        {
            var before=DeskLoc.Instance.Lang;var log=new StringBuilder();int errors=0,total=0;
            try{foreach(var lang in new[]{DeskLang.Tr,DeskLang.En}){DeskLoc.Instance.SetLanguage(lang);Refresh();errors+=CheckCurrent(lang.ToString(),log,out int count);total+=count;}}
            finally{DeskLoc.Instance.SetLanguage(before);Refresh();}
            string summary=string.Format(System.Globalization.CultureInfo.InvariantCulture,"FX TR/EN: {0} metin, {1} hata.",total,errors);Directory.CreateDirectory("Docs/UI_Fx/captures/F7");File.WriteAllText("Docs/UI_Fx/captures/F7/text-check.txt",summary+"\n"+log);return summary;
        }
        public static void Refresh()
        {
            var desk=UnityEngine.Object.FindFirstObjectByType<FxDeskInteraction>();if(desk!=null){if(desk.source!=null)desk.LoadSample(desk.source.ActiveSample);else desk.Render(false);desk.panels.CompleteAnimations();}
            var raid=UnityEngine.Object.FindFirstObjectByType<FxRaidHud>();if(raid!=null){raid.source.RefreshPresentation();raid.Render(raid.source.Current);raid.CompleteAnimations();}
            Canvas.ForceUpdateCanvases();foreach(var b in UnityEngine.Object.FindObjectsByType<FxGraphicBatch>(FindObjectsSortMode.None))b.Refresh();Canvas.ForceUpdateCanvases();
        }
        static float PixelSize(TMP_Text text)
        {
            if(text is TextMeshProUGUI)return text.fontSize*(text.canvas!=null?text.canvas.scaleFactor:1);
            var camera=Camera.main;if(camera==null)return text.fontSize;var origin=text.transform.position;var up=text.transform.TransformVector(Vector3.up*text.fontSize*.1f);return Vector2.Distance(camera.WorldToScreenPoint(origin),camera.WorldToScreenPoint(origin+up));
        }
        public static int CheckCurrent(string context,StringBuilder report,out int count)
        {
            int errors=0;count=0;foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)){
                if(!text.isActiveAndEnabled||string.IsNullOrEmpty(text.text)||text.GetComponentInParent<CanvasGroup>()?.alpha==0)continue;count++;text.ForceMeshUpdate();string issue=null;
                if(text.enableAutoSizing)issue="Auto Size";else if((text.fontStyle&(FontStyles.UpperCase|FontStyles.LowerCase|FontStyles.SmallCaps))!=0)issue="Case stili";else if(PixelSize(text)<11)issue="11 px altı";
                else if(text.isTextTruncated||text.isTextOverflowing||text.textWrappingMode==TextWrappingModes.NoWrap&&text.GetPreferredValues(text.text).x>text.rectTransform.rect.width+1)issue="Metin taşması";
                if(issue==null)for(int i=0;i<text.textInfo.characterCount;i++){var c=text.textInfo.characterInfo[i];if(!c.isVisible||c.textElement==null)continue;if(c.textElement.unicode==0x25A1&&(c.index>=text.text.Length||text.text[c.index]!='\u25A1')){issue="Eksik glif";break;}}
                if(issue==null)continue;errors++;report.Append(context).Append(" · ").Append(text.name).Append(" · ").Append(issue).Append(" · ").Append(text.fontSize).Append(" · ").Append(text.rectTransform.rect.size).Append(" · ").AppendLine(text.GetParsedText());
            }return errors;
        }
    }
}
