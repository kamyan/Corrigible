using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Saha ekibi kartı (dosyadaki ekip satırıyla aynı): jeton, ad, yer · durum, moral kutuları, son temas.</summary>
    public sealed class TeamCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text letter, teamName, where, moraleLabel, lastContact;
        [SerializeField] Image[] morale = System.Array.Empty<Image>();

        public void Bind(TeamRowVM t)
        {
            var T = DeskLoc.Instance;
            DeskUi.SetText(letter, t.Id);
            DeskUi.SetText(teamName, T.T(t.Name));
            DeskUi.SetText(where, T.T(t.Where));
            DeskUi.SetText(moraleLabel, T.T(t.MoraleLabel));
            DeskUi.SetText(lastContact, T.T(t.LastContact));
            for (int i = 0; i < morale.Length; i++)
            {
                DeskUi.SetActive(morale[i], i < t.MoraleMax);
                DeskSprites.Apply(morale[i], i < t.Morale ? "sh_fill_r0" : "sh_frame_r0_1");
            }
        }
    }
}
