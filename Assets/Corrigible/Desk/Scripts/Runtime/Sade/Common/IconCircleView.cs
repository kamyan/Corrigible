using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Simge dairesi (etiketlerin solunda): dizin kâğıdı dolgu + 1,5 px halka + simge. Tehdit kipinde halka ve simge kırmızı
    /// (TEKİLLİK). Boyutlar prefab'da; simge adı ve kip veriden.
    /// </summary>
    public sealed class IconCircleView : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] Image ring;
        [SerializeField] Image icon;
        [SerializeField] string fillSprite = "circle_36", ringSprite = "ring_36_w1_5";
        [Tooltip("Simgenin adı, boy eki olmadan (fund → ic_fund_l).")]
        [SerializeField] string iconName = "fund";
        [SerializeField] bool threat;
        [Tooltip("≤16 px simgede _s, üstünde _l (PROMPT §4.6).")]
        [SerializeField] bool largeIcon = true;

        void OnEnable() => Apply();
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(string iconId, bool isThreat)
        {
            if (iconName == iconId && threat == isThreat) return;
            iconName = iconId; threat = isThreat;
            Apply();
        }

        public void Apply()
        {
            string ink = threat ? "sade.status.threat" : "sade.ink";
            SadeUi.Sprite(fill, fillSprite, "sade.index");
            SadeUi.Sprite(ring, ringSprite, ink);
            if (icon != null)
            {
                DeskUi.SetActive(icon, !string.IsNullOrEmpty(iconName));
                if (!string.IsNullOrEmpty(iconName)) SadeUi.Sprite(icon, "ic_" + iconName + (largeIcon ? "_l" : "_s"), ink);
            }
        }
    }
}
