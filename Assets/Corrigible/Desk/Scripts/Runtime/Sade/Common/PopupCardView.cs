using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Yalın açılır kart: başlık + 2 px kırmızı başlık çizgisi + kapat + gövde metni. Galeride kabın görünüşünü gösterir; özel
    /// kartlar (bölge kartı, lejant kartı, birim kartı…) <see cref="PopupCardBase"/>'den türer.
    /// </summary>
    public sealed class PopupCardView : PopupCardBase
    {
        [SerializeField] SadePaper paper;
        [SerializeField] Graphic rule;
        [SerializeField] TMP_Text body;
        [SerializeField] string titleRole = "legendTitle";
        [SerializeField] string sampleTitle = "LEJANT";
        [SerializeField] string sampleBody = "Kart kabı: kâğıt, başlık, başlık çizgisi ve kapat düğmesi.";

        bool bound;

        void OnEnable() { if (!bound) Render(sampleTitle, sampleBody); }
        void OnValidate() => DeskUi.Deferred(this, () => { if (!bound) Render(sampleTitle, sampleBody); });

        /// <summary>Önizleme verisini çizer (kurucu ve galeri).</summary>
        public void ShowSample() => Render(sampleTitle, sampleBody);

        public void Bind(string titleText, string bodyText)
        {
            bound = true;
            Render(titleText, bodyText);
        }

        void Render(string titleText, string bodyText)
        {
            var T = DeskLoc.Instance;
            if (paper != null) paper.Apply();
            SadeUi.Type(title, titleRole);
            DeskUi.Tint(title, "sade.ink");
            DeskUi.SetText(title, T.T(titleText));
            DeskUi.Tint(rule, "sade.ruleRed");
            SadeUi.Type(body, "legendItem");
            DeskUi.Tint(body, "sade.ink2");
            DeskUi.SetText(body, T.T(bodyText));
        }
    }
}
