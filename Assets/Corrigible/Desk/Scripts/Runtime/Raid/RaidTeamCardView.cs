using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Alt sıradaki birim kartı (Revizyon 2 · S6; 237 × 84 ozalit, kontrplak şeridin içinde): sol üstte kısayol fişi, disk
    /// (harf; hakkı bitince koyu), ad, pipler (kaybedilen kırmızı kenarlı boş; yaralıda YARALI, plakalıda kalkan + PLAKA) ve
    /// hak satırı (HAREKET + YETENEK yeşil · ✓ TUR BİTTİ · DÜŞTÜ kırmızı; kilit niyetinin hedefindeyse KİLİT TEHDİDİ · #n
    /// kırmızı). Seçili: #EFE3A2 + 3 px sarı çerçeve, 6 px yukarı;
    /// üzerinde 2 px. Düşen uzmanın kartı kırmızı kesik kenarlı.
    /// </summary>
    public sealed class RaidTeamCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] RectTransform visual;
        [SerializeField] CanvasGroup group;
        [SerializeField] Button button;
        [SerializeField] Image paper, selectFrame, downFrame;
        [SerializeField] TMP_Text key, nameLabel, status, wound, plateLabel, stats, equipment;
        [SerializeField] Image disc;
        [SerializeField] TMP_Text discLetter;
        [SerializeField] Image plateIcon;
        [SerializeField] Image[] pips = System.Array.Empty<Image>();

        RaidTeamModel model; IRaidCommands commands; bool hover; float lift;
        public Button Button => button;
        public event System.Action<char> Hovered;

        static Color C(string k) => RaidHudView.C(k);

        public void Bind(IRaidCommands value, int index) { commands = value; key.text = (index + 1).ToString(); button.onClick.AddListener(Select); }
        void Select() { if (model != null) commands?.SelectUnit(model.Letter); }

        public void Render(RaidTeamModel value, bool locked)
        {
            model = value;
            nameLabel.text = value.Rookie ? Loc.F("{0} · YEDEK", value.Name) : value.Name;
            if(stats)stats.text=Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}",value.Hp,value.Max,value.MoveRange,value.Power);
            if(equipment)equipment.text=value.Equipment??string.Empty;
            discLetter.text = value.Letter.ToString();
            bool done = !value.Down && !value.Out && !value.Move && !value.Ability;
            disc.color = C(value.Down || value.Out || done ? "panel.teamCard.discDone" : "panel.teamCard.disc");
            RaidHudView.Pips(pips, value.Down ? 0 : value.Hp, value.Max, C("labels.hpTag.colors.team"));
            bool hurt = !value.Down && value.Hp < value.Max;
            wound.gameObject.SetActive(hurt);
            wound.text = Loc.T("YARALI");
            bool plate = !value.Down && value.Armor > 0;
            plateIcon.gameObject.SetActive(plate);
            plateLabel.gameObject.SetActive(plate && !hurt);
            plateLabel.text = Loc.T("PLAKA");

            // Hak satırı: biçim + kelime + renk
            if (value.Down)
            {
                status.text = value.Absent ? Loc.T("SIĞINAKTA YARALI") : value.Left ? Loc.T("ÇIKAMADI") : Loc.T("DÜŞTÜ");
                status.color = C("ink.red");
            }
            else if (value.Out)
            {
                // Kaçışta çıkıştan tahliye edildi: tahtada değil, kart kapalı
                status.text = Loc.T("✓ TAHLİYE EDİLDİ");
                status.color = C("ink.green");
            }
            else if (!string.IsNullOrEmpty(value.Threat))
            {
                status.text = value.Threat;
                status.color = C("ink.red");
            }
            else if (done)
            {
                status.text = Loc.T("✓ TUR BİTTİ");
                status.color = C("ink.ink3");
            }
            else
            {
                status.text = value.Move && value.Ability ? Loc.T("HAREKET + YETENEK") : value.Move ? Loc.T("HAREKET") : Loc.T("YETENEK");
                // Emir iletilirken hakkı duran uzman uyarılır
                status.color = C(value.Warn ? "ink.red" : "ink.green");
            }
            paper.color = C(value.Selected ? "panel.teamCard.selected" : "panel.ozalit.paper");
            selectFrame.enabled = value.Selected;
            downFrame.enabled = value.Down;
            if (group != null) group.alpha = value.Down || value.Out ? 0.75f : 1;
            button.interactable = !locked && !value.Down && !value.Out;
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; if (model != null && !model.Down && !model.Out) Hovered?.Invoke(model.Letter); }
        public void OnPointerExit(PointerEventData e) { hover = false; commands?.ClearHover(); }

        void Update()
        {
            // Seçim 0,1 s'de 6 px; üzerinde 2 px
            float target = model != null && model.Selected ? 6 : hover && button.interactable ? 2 : 0;
            lift = Mathf.MoveTowards(lift, target, Time.unscaledDeltaTime * 60);
            visual.anchoredPosition = new Vector2(0, lift);
        }
    }
}
