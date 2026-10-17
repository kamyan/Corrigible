using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>Etkin Sade örneğinde iki dilin yazı kuralları ve TMP taşmaları; dili deneme sonunda geri alır.</summary>
    public static class SadeTextChecker
    {
        [MenuItem("Corrigible/Sade UI/Check Text")]
        static void Menu() => Debug.Log(Run());

        public static string Run(string phase = "F7")
        {
            if(Object.FindFirstObjectByType<SadeDeskPreview>()==null && Object.FindFirstObjectByType<RaidHudView>()==null && Object.FindFirstObjectByType<DeskPresenter>()==null)return "Sade masa ya da Baskın sahnesini açın.";
            var language=DeskLoc.Instance.Lang;var report=new StringBuilder();int errors=0,checkedTexts=0;
            try
            {
                foreach(var lang in new[]{DeskLang.Tr,DeskLang.En})
                {
                    DeskLoc.Instance.SetLanguage(lang);
                    RefreshCurrent();
                    errors+=CheckCurrent(lang.ToString(),report,out int count);checkedTexts+=count;
                }
            }
            finally{DeskLoc.Instance.SetLanguage(language);RefreshCurrent();}
            string summary=$"{phase} TR/EN: {checkedTexts} metin, {errors} hata.";
            Directory.CreateDirectory(SadeCapture.DirectoryPath);
            File.WriteAllText(Path.Combine(SadeCapture.DirectoryPath,phase+"_metin_denetimi.txt"),summary+"\n"+report);
            return summary+(errors>0?"\n"+report:"");
        }

        static void RefreshCurrent()
        {
            Object.FindFirstObjectByType<SadeDeskPreview>()?.Refresh();
            var presenter=Object.FindFirstObjectByType<DeskPresenter>();
            if(presenter?.Source!=null)presenter.Bind(presenter.Source.Current);
            Object.FindFirstObjectByType<SadeDeskInteraction>()?.RefreshLayout();
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>O anki dili/durumu değiştirmeden denetler; toplu en-boy yakalayıcısı da aynı kuralları kullanır.</summary>
        public static int CheckCurrent(string context,StringBuilder report,out int checkedTexts)
        {
            int errors=0;checkedTexts=0;
            foreach(var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if(!text.isActiveAndEnabled||string.IsNullOrEmpty(text.text)||text.name=="Ghost")continue;
                checkedTexts++;text.ForceMeshUpdate();string problem=null;
                if(text.enableAutoSizing)problem="Auto Size";
                else if((text.fontStyle&(FontStyles.UpperCase|FontStyles.SmallCaps|FontStyles.LowerCase))!=0)problem="Case stili";
                else if(text.fontSize<11)problem="11 px altı";
                else if((text.font==DeskTheme.Active.kalamBold||text.font==DeskTheme.Active.kalamRegular)&&text.fontSize<15)problem="Kalam 15 px altı";
                else if(text.isTextTruncated||InkOverflows(text))problem="Metin taşması";
                if(problem==null)continue;
                errors++;report.Append(context).Append(" · ").Append(text.transform.parent.name).Append('/').Append(text.name).Append(" · ").Append(problem).Append(" · ").AppendLine(text.GetParsedText());
            }
            return errors;
        }

        static bool InkOverflows(TMP_Text text)
        {
            var rect = text.rectTransform.rect;
            const float tolerance = 1.5f;
            // textBounds SDF'nin saydam örnekleme payını da içerir (Kalam '?' için ~14 px).
            // TMP'nin son yerleşimdeki origin/baseLine değerleri + gerçek glif metrikleri bu payı dışlar.
            var info = text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible || character.textElement == null) continue;
                var metrics = character.textElement.glyph.metrics;
                float left = character.origin + metrics.horizontalBearingX * character.scale;
                float top = character.baseLine + metrics.horizontalBearingY * character.scale;
                float right = left + metrics.width * character.scale;
                float bottom = top - metrics.height * character.scale;
                if (left < rect.xMin - tolerance || right > rect.xMax + tolerance
                    || bottom < rect.yMin - tolerance || top > rect.yMax + tolerance) return true;
            }
            return false;
        }
    }
}
