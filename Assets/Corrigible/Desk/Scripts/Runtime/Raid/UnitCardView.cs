using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Seçili uzmanın yetenek kartı (Revizyon 2 · S6; sol alt, 432 px, kontrplak + ozalit): başlıkta disk, ad ve pipler (plaka
    /// kalkanı); satırlar 44 px (Hareket “3 kare”, yetenek + Q fişi + menzil; üçüncü satır plaka ya da yedek); altta iki alt
    /// düğme (görev eylemi yeşil, robota vuran kırmızı; hedef yokken kesik, varken düz kenar). Kart alt kenarından büyür.
    /// Yetenek satırına basmak (ya da Q) yeteneği hazırlar; hazır satır mürekkep zeminli.
    /// </summary>
    public sealed class UnitCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text title, discLetter;
        [SerializeField] Image disc, plateIcon;
        [SerializeField] Image[] pips = System.Array.Empty<Image>();
        [SerializeField] RectTransform[] rows = System.Array.Empty<RectTransform>();
        [SerializeField] Button[] rowButton = System.Array.Empty<Button>();
        [SerializeField] Image[] rowFill = System.Array.Empty<Image>(), rowFrame = System.Array.Empty<Image>(), rowIcon = System.Array.Empty<Image>();
        [SerializeField] TMP_Text[] rowName = System.Array.Empty<TMP_Text>(), rowDetail = System.Array.Empty<TMP_Text>();
        [Tooltip("Yetenek satırındaki Q fişi.")]
        [SerializeField] GameObject abilityKey;
        [SerializeField] TMP_Text abilityKeyText;
        [SerializeField] GameObject effectRow;
        [SerializeField] Image[] effectFrame = System.Array.Empty<Image>();
        [SerializeField] TMP_Text[] effectLabels = System.Array.Empty<TMP_Text>();
        [SerializeField] Sprite frameSolid, frameDashed;

        static Color C(string k) => RaidHudView.C(k);

        public void Bind(IRaidCommands commands)
        {
            if (rowButton.Length < 2) return;
            rowButton[1].onClick.RemoveAllListeners();
            rowButton[1].onClick.AddListener(commands.ArmAbility);
        }

        public void Render(RaidViewModel vm)
        {
            gameObject.SetActive(vm.UnitCard != null);
            if (vm.UnitCard == null) return;
            var unit = vm.UnitCard;
            title.text = unit.Rookie ? Loc.F("{0} · YEDEK", unit.Name) : unit.Name;
            discLetter.text = unit.Letter.ToString();
            disc.color = C("panel.teamCard.disc");
            RaidHudView.Pips(pips, unit.Hp, unit.Max, C("labels.hpTag.colors.team"));
            plateIcon.gameObject.SetActive(unit.Armor > 0);

            for (int i = 0; i < rows.Length; i++)
            {
                bool on = i < vm.UnitRows.Length;
                rows[i].gameObject.SetActive(on);
                if (!on) continue;
                var row = vm.UnitRows[i];
                rowName[i].text = row.Text;
                rowDetail[i].text = row.Detail;
                if (!string.IsNullOrEmpty(row.Icon)) DeskSprites.Apply(rowIcon[i], row.Icon);
                // Hazır yetenek mürekkep zemin; kullanılmış hak kesik kenar, gri; normal kâğıt beyazı
                bool armed = row.Armed, used = row.Used;
                rowFill[i].color = C(armed ? "ui.inkButton" : used ? "panel.ozalit.paper" : "labels.hpTag.paper");
                rowFrame[i].sprite = used ? frameDashed : frameSolid;
                rowFrame[i].type = used ? Image.Type.Tiled : Image.Type.Sliced;
                rowFrame[i].color = C(used ? "ink.ink3" : "ui.inkButton");
                var ink = C(armed ? "ui.inkButtonText" : used ? "ink.ink3" : "ink.ink");
                rowName[i].color = ink;
                rowIcon[i].color = ink;
                rowDetail[i].color = armed ? C("ui.inkButtonText") : C(used ? "ink.ink3" : "report.summaryLine");
                rowButton[i].interactable = i == 1 && unit.Ability && !vm.Locked;
            }
            if (abilityKey != null)
            {
                abilityKey.SetActive(vm.UnitRows.Length > 1);
                abilityKeyText.text = "Q";
            }

            // Alt düğmeler: görev eylemi yeşil, robota vuran kırmızı; hedef varken düz, yokken kesik kenar
            bool targets = vm.AbilityTargets.Length > 0;
            effectRow.SetActive(vm.Effects.Length > 0);
            for (int i = 0; i < effectLabels.Length; i++)
            {
                bool on = i < vm.Effects.Length;
                effectFrame[i].transform.parent.gameObject.SetActive(on);
                if (!on) continue;
                string tone = i < vm.EffectTones.Length ? vm.EffectTones[i] : "plain";
                var color = C(tone == "good" ? "ink.green" : tone == "bad" ? "ink.red" : "ink.ink");
                effectLabels[i].text = vm.Effects[i];
                effectLabels[i].color = color;
                effectFrame[i].sprite = targets ? frameSolid : frameDashed;
                effectFrame[i].type = targets ? Image.Type.Sliced : Image.Type.Tiled;
                effectFrame[i].color = color;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }
    }
}
