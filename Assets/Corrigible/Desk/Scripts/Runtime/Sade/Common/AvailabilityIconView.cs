using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    public enum AvailabilityState { Open, Used, Armed }

    /// <summary>
    /// Hak simgesi (baskın ekip kartı; tokens_sade.raid.teamChip): 24 px daire + 1,5 px halka + 14 px simge. Açık: mavi halka,
    /// açık mavi dolgu; kullanılmış: soluk halka, dolgu yok, soluk simge; hazır (Q): ink halka, fosforlu sarı dolgu.
    /// </summary>
    public sealed class AvailabilityIconView : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] Image ring;
        [SerializeField] Image icon;
        [Tooltip("Simgenin adı, boy eki olmadan (boots = hareket, bolt = yetenek).")]
        [SerializeField] string iconName = "boots";
        [SerializeField] AvailabilityState state;

        public AvailabilityState State => state;

        void OnEnable() => Apply();
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(AvailabilityState s)
        {
            if (state == s) return;
            state = s;
            Apply();
        }

        public void Apply()
        {
            string ringKey, iconKey, fillKey;
            switch (state)
            {
                case AvailabilityState.Used: ringKey = "sade.raid.availOffRing"; iconKey = "sade.raid.availOffIcon"; fillKey = null; break;
                case AvailabilityState.Armed: ringKey = "sade.ink"; iconKey = "sade.ink"; fillKey = "sade.highlighter"; break;
                default: ringKey = "sade.raid.availOnRing"; iconKey = "sade.raid.availOnRing"; fillKey = "sade.raid.availOnFill"; break;
            }
            DeskUi.SetActive(fill, fillKey != null);
            if (fillKey != null) SadeUi.Sprite(fill, "circle_24", fillKey);
            SadeUi.Sprite(ring, "ring_24_w1_5", ringKey);
            SadeUi.Sprite(icon, "ic_" + iconName + "_s", iconKey);
        }
    }
}
