using System.Collections.Generic;
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
    /// <para>Tesis odası (aşama 7): Hareket / yetenek / plaka satırlarının altına bu odada anlamlı her hamle için bir satır gelir
    /// (<see cref="RaidViewModel.ActionRows"/>). Satırlar yetenek satırının (Row 1) çalışma anı kopyalarıdır: prefab değişmez,
    /// parçalar şablonun referanslarından kardeş sırası yoluyla bulunur (ad araması yok). Kopyalar yalnız gerektiğinde kurulur ve
    /// havuzda kapatılır (örnek tahtada hiç kurulmaz). Kopyanın Q fişi E fişi olur (yalnız <see cref="RaidViewModel.ActionKey"/>
    /// satırında); satıra basmak o hamleyi kurar.</para>
    /// <para>Aşama 9: uzmanın ekipman satırı (<see cref="RaidViewModel.GearRows"/>, en çok bir) aynı kopya havuzunun ilk satırıdır
    /// (plakadan sonra, hamlelerden önce; ekipman yeteneğin yerine kullanılır): fişi R (yalnız <see cref="RaidViewModel.GearKey"/>
    /// satırında), tıkı <see cref="IRaidCommands.ChooseGear"/>. Hamle satırları onu izler, dizinleri değişmez.</para>
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

        /// <summary>Hamle satırının kopyası: yetenek satırının parçalarının karşılıkları.</summary>
        sealed class ActionRow
        {
            public RectTransform Root; public Button Button; public Image Fill, Frame, Icon; public TMP_Text Name, Detail;
            public GameObject Key; public TMP_Text KeyText;
        }
        readonly List<ActionRow> actionRows = new List<ActionRow>();
        IRaidCommands commands;
        /// <summary>Son çizimde havuzun başındaki ekipman satırı sayısı (tık eşlemesi: önce ekipman, sonra hamle).</summary>
        int gearCount;
        /// <summary>Kurulmuş hamle satırı kopyaları (etkin olsun olmasın; testler için).</summary>
        public int ActionRowCopies => actionRows.Count;

        public void Bind(IRaidCommands commands)
        {
            this.commands = commands;
            if (rowButton.Length < 2) return;
            rowButton[1].onClick.RemoveAllListeners();
            rowButton[1].onClick.AddListener(commands.ArmAbility);
            for (int i = 0; i < actionRows.Count; i++) BindAction(i);
        }

        void BindAction(int i)
        {
            var b = actionRows[i].Button;
            if (b == null) return;
            b.onClick.RemoveAllListeners();
            if (commands == null) return;
            int index = i;
            b.onClick.AddListener(() => { if (index < gearCount) commands.ChooseGear(index); else commands.ChooseAction(index - gearCount); });
        }

        /// <summary>Şablonun (Row 1 altındaki bir parça) kopyadaki karşılığı: şablon kökünden kardeş sırası yolu kopyada izlenir.</summary>
        static T Mirror<T>(T templ, Transform templRoot, Transform copyRoot) where T : Component
        {
            if (templ == null) return null;
            var path = new List<int>();
            for (var t = templ.transform; t != templRoot; t = t.parent)
            {
                if (t == null) return null;
                path.Add(t.GetSiblingIndex());
            }
            var c = copyRoot;
            for (int i = path.Count - 1; i >= 0; i--) c = c.GetChild(path[i]);
            return c.GetComponent<T>();
        }

        /// <summary>Gereken sayıda hamle satırı kopyası (yetenek satırından; alt düğmelerin hemen önüne).</summary>
        void EnsureActionRows(int count)
        {
            if (rows.Length < 2 || rows[1] == null) return;
            var templ = rows[1];
            while (actionRows.Count < count)
            {
                var go = Instantiate(templ.gameObject, templ.parent);
                go.name = "Action row " + actionRows.Count;
                if (effectRow != null && effectRow.transform.parent == templ.parent) go.transform.SetSiblingIndex(effectRow.transform.GetSiblingIndex());
                var root = (RectTransform)go.transform;
                var row = new ActionRow
                {
                    Root = root,
                    Button = Mirror(rowButton.Length > 1 ? rowButton[1] : null, templ, root),
                    Fill = Mirror(rowFill.Length > 1 ? rowFill[1] : null, templ, root),
                    Frame = Mirror(rowFrame.Length > 1 ? rowFrame[1] : null, templ, root),
                    Icon = Mirror(rowIcon.Length > 1 ? rowIcon[1] : null, templ, root),
                    Name = Mirror(rowName.Length > 1 ? rowName[1] : null, templ, root),
                    Detail = Mirror(rowDetail.Length > 1 ? rowDetail[1] : null, templ, root),
                    KeyText = Mirror(abilityKeyText, templ, root),
                };
                var key = abilityKey != null ? Mirror(abilityKey.transform, templ, root) : null;
                row.Key = key != null ? key.gameObject : null;
                actionRows.Add(row);
                BindAction(actionRows.Count - 1);
            }
        }

        /// <summary>Satırın boyası: hazır mürekkep zemin; kullanılamaz kesik kenar, gri; normal kâğıt beyazı.</summary>
        void PaintRow(Image fill, Image frame, Image icon, TMP_Text name, TMP_Text detail, RaidCardRow row)
        {
            name.text = row.Text;
            detail.text = row.Detail;
            if (!string.IsNullOrEmpty(row.Icon)) DeskSprites.Apply(icon, row.Icon);
            bool armed = row.Armed, used = row.Used;
            fill.color = C(armed ? "ui.inkButton" : used ? "panel.ozalit.paper" : "labels.hpTag.paper");
            frame.sprite = used ? frameDashed : frameSolid;
            frame.type = used ? Image.Type.Tiled : Image.Type.Sliced;
            frame.color = C(used ? "ink.ink3" : "ui.inkButton");
            var ink = C(armed ? "ui.inkButtonText" : used ? "ink.ink3" : "ink.ink");
            name.color = ink;
            icon.color = ink;
            detail.color = armed ? C("ui.inkButtonText") : C(used ? "ink.ink3" : "report.summaryLine");
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
                // Hazır yetenek mürekkep zemin; kullanılmış hak kesik kenar, gri; normal kâğıt beyazı
                PaintRow(rowFill[i], rowFrame[i], rowIcon[i], rowName[i], rowDetail[i], vm.UnitRows[i]);
                rowButton[i].interactable = i == 1 && unit.Ability && !vm.Locked;
            }
            if (abilityKey != null)
            {
                abilityKey.SetActive(vm.UnitRows.Length > 1);
                abilityKeyText.text = "Q";
            }
            // Tesis hamleleri (aşama 7): kopyalar yalnız gerektiğinde kurulur; fazlası kapanır (örnek tahtada ActionRows boş). Aşama 9:
            // havuzun başında ekipman satırı (R), sonra hamleler (E); örnek tahtada ve ekipmansızda GearRows boş
            var gears = vm.GearRows ?? System.Array.Empty<RaidCardRow>();
            var acts = vm.ActionRows ?? System.Array.Empty<RaidCardRow>();
            gearCount = gears.Length;
            int total = gears.Length + acts.Length;
            if (total > 0) EnsureActionRows(total);
            for (int i = 0; i < actionRows.Count; i++)
            {
                var a = actionRows[i];
                bool on = i < total;
                if (a.Root.gameObject.activeSelf != on) a.Root.gameObject.SetActive(on);
                if (!on) continue;
                bool gear = i < gears.Length;
                var row = gear ? gears[i] : acts[i - gears.Length];
                PaintRow(a.Fill, a.Frame, a.Icon, a.Name, a.Detail, row);
                if (a.Button != null) a.Button.interactable = !vm.Locked && !row.Used;
                if (a.Key != null) a.Key.SetActive(gear ? i == vm.GearKey : i - gears.Length == vm.ActionKey);
                if (a.KeyText != null) a.KeyText.text = gear ? "R" : "E";
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
