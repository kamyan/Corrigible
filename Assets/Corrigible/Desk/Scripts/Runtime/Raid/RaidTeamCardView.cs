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
        /// <summary>Kartın üzerine gelindi (modeliyle: uzman harfle, köpek birim kimliğiyle).</summary>
        public event System.Action<RaidTeamModel> Hovered;

        static Color C(string k) => RaidHudView.C(k);

        public void Bind(IRaidCommands value, int index) { commands = value; key.text = (index + 1).ToString(); button.onClick.AddListener(Select); }
        // Aşama 9: dördüncü kart (köpek) birim kimliğiyle seçer
        void Select() { if (model == null) return; if (model.UnitId > 0) commands?.SelectUnitId(model.UnitId); else commands?.SelectUnit(model.Letter); }

        public void Render(RaidTeamModel value, bool locked)
        {
            model = value;
            nameLabel.text = value.Rookie ? Loc.F("{0} · YEDEK", value.Name) : value.Name;
            // Köpek kartında (aşama 9) GÜÇ yok
            if(stats)stats.text=value.Dog?Loc.F("CAN {0}/{1} · HAR {2}",value.Hp,value.Max,value.MoveRange):Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}",value.Hp,value.Max,value.MoveRange,value.Power);
            if(equipment)equipment.text=value.Equipment??string.Empty;
            discLetter.text = value.Letter.ToString();
            bool done = !value.Down && !value.Out && !value.Move && !value.Ability;
            // AtGate yalnız tesis odasında (kapıda bekleyen uzman); örnek tahtada hep false, aşağıdaki dallar bugünkü gibi
            bool away = value.Out || value.AtGate;
            disc.color = C(value.Down || away || done ? "panel.teamCard.discDone" : "panel.teamCard.disc");
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
                // DownText: önceki bir tesis odasında düşen (“DÜŞTÜ · PAFTA n”); örnek tahtada null, bugünkü DÜŞTÜ
                status.text = value.Absent ? Loc.T("SIĞINAKTA YARALI") : value.Left ? Loc.T("ÇIKAMADI") : !string.IsNullOrEmpty(value.DownText) ? value.DownText : Loc.T("DÜŞTÜ");
                status.color = C("ink.red");
            }
            else if (value.Out)
            {
                // Kaçışta çıkıştan tahliye edildi: tahtada değil, kart kapalı
                status.text = Loc.T("✓ TAHLİYE EDİLDİ");
                status.color = C("ink.green");
            }
            else if (value.AtGate)
            {
                // Tesis odası: kapıda bekliyor (tahtadan kalktı; kapıdan geri çıkılmaz, yalnız bu turun hamlesi geri alınır)
                // Tesis aşama 12 (Katar): AYIR verilebilirken Teknisyen'in kartı “AYIR · Q” (tık AYIR'ı çevirir)
                status.text = value.Detach ? Loc.T("AYIR · Q") : Loc.T("✓ KAPIDA");
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
                // Köpek (aşama 9) yetenek değil komut verir
                status.text = value.Dog ? (value.Move && value.Ability ? Loc.T("HAREKET + KOMUT") : value.Move ? Loc.T("HAREKET") : Loc.T("KOMUT"))
                    : value.Move && value.Ability ? Loc.T("HAREKET + YETENEK") : value.Move ? Loc.T("HAREKET") : Loc.T("YETENEK");
                // Emir iletilirken hakkı duran uzman uyarılır
                status.color = C(value.Warn ? "ink.red" : "ink.green");
            }
            paper.color = C(value.Selected ? "panel.teamCard.selected" : "panel.ozalit.paper");
            selectFrame.enabled = value.Selected;
            downFrame.enabled = value.Down;
            if (group != null) group.alpha = value.Down || away ? 0.75f : 1;
            button.interactable = !locked && !value.Down && (!away || value.Detach);
        }

        public void OnPointerEnter(PointerEventData e) { hover = true; if (model != null && !model.Down && !model.Out && !model.AtGate) Hovered?.Invoke(model); }
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
