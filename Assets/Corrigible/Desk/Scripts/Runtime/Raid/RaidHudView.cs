using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Baskın HUD'u (Revizyon 2 · S6): kenar panelleri tahtanın malzemesinden (kontrplak şerit + ozalit pafta + bant). Sol üstte
    /// tur şeridi (TUR 1/8, 34 px hücreler; son iki tur kesik kırmızı, son turda hücre kırmızı ve SON TUR damgası), evre çipi
    /// (SIRA SENDE daire · MAKİNELER baklava · OPERASYON BİTTİ kare) ve görev çipi (görev + bölge özelliği; hedef çizimi, adı,
    /// karesi, amacı, CAN; GÜVEN + sivil sunucu); sağ üstte TELSİZ (okunmamış sayısı) · KURALLAR · GERİ ÇEKİL; sağ altta Geri
    /// al + EMRİ İLET (son turda “SON TUR”), makine evresinde yerine kilitli kâğıt (“MAKİNELER HAREKET EDİYOR · girdi kilitli ·
    /// 2/3 hamle”). Tahta kurallarını hesaplamaz; veri kaynağı ve komut sınırına bağlanır. Yerleşim prefab'da (yuvalar hazır).
    /// </summary>
    public sealed class RaidHudView : MonoBehaviour
    {
        [SerializeField] MonoBehaviour dataSource, commandSource;
        [SerializeField] RaidPanelsView panels;
        public RaidPanelsView Panels => panels;
        [SerializeField] CanvasGroup cornerGroup;
        [Header("Tur şeridi")]
        [SerializeField] TMP_Text turnLabel;
        [SerializeField] TMP_Text turnNumber, lastTwoLabel;
        [SerializeField] Image[] turnFills = System.Array.Empty<Image>(), turnFrames = System.Array.Empty<Image>();
        [SerializeField] TMP_Text[] turnBoxes = System.Array.Empty<TMP_Text>();
        [SerializeField] Sprite cellSolid, cellDashed;
        [Tooltip("SON TUR damgası (son turda şeridin üstüne basılır).")]
        [SerializeField] RectTransform lastStamp;
        [SerializeField] TMP_Text lastStampText;
        const float StampPad = 9;
        float lastStampWidth = -1;
        [Header("Evre çipi")]
        [SerializeField] Image phaseFrame;
        [SerializeField] Image phaseMark;
        [SerializeField] TMP_Text phase;
        [SerializeField] Sprite markCircle, markDiamond, markSquare;
        [Header("Görev çipi")]
        [SerializeField] TMP_Text objective;
        [SerializeField] Button abilityChip;
        [SerializeField] Image traitGlyph;
        [SerializeField] TMP_Text abilityLabel;
        [SerializeField] GameObject[] targetRows = System.Array.Empty<GameObject>();
        [SerializeField] Image[] targetThumbs = System.Array.Empty<Image>();
        [SerializeField] TMP_Text[] targetNames = System.Array.Empty<TMP_Text>(), targetGoals = System.Array.Empty<TMP_Text>(), targetCan = System.Array.Empty<TMP_Text>();
        [Tooltip("Hedef başına eşit sayıda can pipi (sırayla).")]
        [SerializeField] Image[] targetPips = System.Array.Empty<Image>();
        [SerializeField] TMP_Text[] brokenStamps = System.Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text civilianLabel, trustValue, civilianTitle, civilianCount;
        [SerializeField] Image[] civilians = System.Array.Empty<Image>();
        [Header("Üst düğmeler")]
        [SerializeField] Button log;
        [SerializeField] Button rules, retreat;
        [SerializeField] TMP_Text logLabel, rulesLabel, retreatLabel;
        [SerializeField] Image unread;
        [SerializeField] TMP_Text unreadCount;
        [SerializeField] CanvasGroup retreatGroup;
        [Header("Komut")]
        [SerializeField] Button undo;
        [SerializeField] Button end;
        [SerializeField] TMP_Text undoLabel, endLabel, endLastTurn;
        [SerializeField] CanvasGroup undoGroup;
        [SerializeField] RectTransform endFace;
        [SerializeField] Image endShadow;
        [SerializeField] UiHoverTarget endHover;
        [Tooltip("Makine evresinde EMRİ İLET'in yerine kilitli kâğıt.")]
        [SerializeField] GameObject lockedPaper;
        [SerializeField] TMP_Text lockedTitle, lockedLine;
        [Header("Kartlar")]
        [SerializeField] RaidTeamCardView[] team = System.Array.Empty<RaidTeamCardView>();
        [SerializeField] UnitCardView unitCard;
        [SerializeField] HoverInspectorView hoverCard;
        [SerializeField] GameObject statusCard;
        [SerializeField] TMP_Text statusText;

        IRaidDataSource source; IRaidCommands commands; bool abilityTipVisible, wasLast;
        float lastAt = -1;
        public RaidViewModel Current => source?.Current;
        public event System.Action<char> TeamHovered;

        /// <summary>Renk: baskın teması (raid_tokens.json), yoksa masa teması, yoksa sığınak teması.</summary>
        public static Color C(string key)
        {
            if (RaidTheme.Active != null && RaidTheme.Active.TryGet(key, out var c)) return c;
            if (DeskTheme.Active != null && DeskTheme.Active.TryGet(key, out c)) return c;
            return SiginakTheme.Active.ColorOf(key);
        }

        void OnEnable()
        {
            source = dataSource as IRaidDataSource; commands = commandSource as IRaidCommands;
            if (source == null || commands == null) return;
            source.Changed += Render; DeskLoc.Instance.Changed += Refresh;
            log.onClick.AddListener(commands.ToggleLog); rules.onClick.AddListener(commands.ToggleRules); retreat.onClick.AddListener(commands.RequestRetreat);
            undo.onClick.AddListener(commands.Undo); end.onClick.AddListener(commands.EndTurn);
            abilityChip.onClick.AddListener(ShowAbilityTip);
            for (int i = 0; i < team.Length; i++) { team[i].Bind(commands, i); team[i].Hovered += OnTeamHover; }
            unitCard.Bind(commands); if (panels != null) panels.Bind(commands); Refresh();
        }

        void OnDisable()
        {
            if (source != null) source.Changed -= Render; DeskLoc.Instance.Changed -= Refresh;
            log.onClick.RemoveAllListeners(); rules.onClick.RemoveAllListeners(); retreat.onClick.RemoveAllListeners(); undo.onClick.RemoveAllListeners();
            end.onClick.RemoveAllListeners(); abilityChip.onClick.RemoveAllListeners();
            foreach (var card in team) { card.Button.onClick.RemoveAllListeners(); card.Hovered -= OnTeamHover; }
        }

        void OnTeamHover(char letter) => TeamHovered?.Invoke(letter);
        public void ShowAbilityTip() { abilityTipVisible = true; Refresh(); }
        public void HideAbilityTip() { abilityTipVisible = false; Refresh(); }
        void Refresh() { if (source?.Current != null) Render(source.Current); }

        public void Render(RaidViewModel vm)
        {
            Turns(vm);
            Phase(vm);
            Mission(vm);
            for (int i = 0; i < team.Length; i++)
            {
                team[i].gameObject.SetActive(i < vm.Team.Length);
                if (i < vm.Team.Length) team[i].Render(vm.Team[i], vm.Locked || vm.Paused);
            }
            Buttons(vm);
            unitCard.Render(vm); hoverCard.Render(vm.HoverCard);
            if (cornerGroup != null) { cornerGroup.blocksRaycasts = !vm.Finished; cornerGroup.interactable = !vm.Finished; }
            if (panels != null) panels.Render(vm);
            statusCard.SetActive(abilityTipVisible || (panels == null && !string.IsNullOrEmpty(vm.Status)));
            statusText.text = abilityTipVisible ? vm.AbilityTip : vm.Status;
        }

        /// <summary>
        /// Tur şeridi (S6 › D): şimdiki hücre mürekkep dolu (son turda kırmızı), geçmiş #B9C3BE, gelecek mavi kenar; son iki
        /// tur önceden kesik kırmızı kenarlı ve üstünde “SON 2 TUR”. Son turda şeritte SON TUR damgası (0,12 s).
        /// </summary>
        void Turns(RaidViewModel vm)
        {
            turnLabel.text = Loc.T("TUR");
            turnNumber.text = Loc.F("{0}/{1}", vm.Turn, vm.Turns);
            turnNumber.color = C(vm.LastTurn ? "turnStrip.lastCurrent" : "turnStrip.current");
            lastTwoLabel.text = Loc.T("SON 2 TUR");
            lastTwoLabel.gameObject.SetActive(!vm.LastTurn && vm.Turns > 2);
            for (int i = 0; i < turnBoxes.Length; i++)
            {
                bool on = i < vm.Turns;
                turnFills[i].transform.parent.gameObject.SetActive(on);
                if (!on) continue;
                int n = i + 1;
                bool current = n == vm.Turn, past = n < vm.Turn, lastTwo = n > vm.Turns - 2;
                // Bölge olayının işlediği turların numarası altı çizili (ne olduğu görev çipinin ipucunda ve tahtada); şebekede turun
                // canlı fazı numaranın yanında küçük harfle
                string ev = vm.TurnEvents != null && n < vm.TurnEvents.Length ? vm.TurnEvents[n] : null;
                turnBoxes[i].text = ev == null ? n.ToString() : ev.Length == 1 ? Loc.F("{0}<size=55%>{1}</size>", n, ev) : Loc.F("<u>{0}</u>", n);
                turnFills[i].enabled = current || past;
                turnFills[i].color = C(current ? (vm.LastTurn ? "turnStrip.lastCurrent" : "turnStrip.current") : "turnStrip.past");
                // Son iki tur kesik kırmızı kenar (geçmişe ya da şimdiye dönünce kenar kalkar)
                turnFrames[i].enabled = !current && !past;
                turnFrames[i].sprite = lastTwo ? cellDashed : cellSolid;
                turnFrames[i].type = lastTwo ? Image.Type.Tiled : Image.Type.Sliced;
                turnFrames[i].color = C(lastTwo ? "turnStrip.lastTwo[0]" : "turnStrip.future");
                turnBoxes[i].color = current ? C("ui.inkButtonText") : past ? C("ink.ink3") : C(lastTwo ? "turnStrip.lastTwo[0]" : "turnStrip.future");
            }
            if (lastStamp == null) return;
            lastStamp.gameObject.SetActive(vm.LastTurn && !vm.Finished);
            lastStampText.text = Loc.T("SON TUR");
            // Damga yazıya göre genişler (EN “LAST TURN” tasarımın 124 px'ine sığmıyor); en dar hâli tasarımınki
            if (lastStampWidth < 0) lastStampWidth = lastStamp.sizeDelta.x;
            lastStamp.sizeDelta = new Vector2(Mathf.Max(lastStampWidth, lastStampText.GetPreferredValues(lastStampText.text).x + 2 * StampPad), lastStamp.sizeDelta.y);
            if (vm.LastTurn && !wasLast && Application.isPlaying) lastAt = Time.unscaledTime;
            wasLast = vm.LastTurn;
        }

        /// <summary>Evre çipi (S6 › phaseChip): biçim + kelime + renk — SIRA SENDE yeşil daire, MAKİNELER kırmızı baklava, bitti kare.</summary>
        void Phase(RaidViewModel vm)
        {
            string key = vm.Finished ? "end" : vm.Phase == "enemy" ? "ai" : "you";
            var color = C("phaseChip." + key + "[1]");
            phase.text = key == "end" ? Loc.T("OPERASYON BİTTİ") : key == "ai" ? Loc.T("MAKİNELER") : Loc.T("SIRA SENDE");
            phase.color = color;
            phaseFrame.color = color;
            phaseMark.sprite = key == "end" ? markSquare : key == "ai" ? markDiamond : markCircle;
            phaseMark.color = color;
            phaseMark.rectTransform.localRotation = Quaternion.Euler(0, 0, key == "ai" ? 45 : 0);
        }

        /// <summary>Görev çipi üç satır: GÖREV · İKMAL + bölge özelliği; hedef (çizim, ad · kare, amaç, CAN); GÜVEN + sivil sunucu.</summary>
        void Mission(RaidViewModel vm)
        {
            objective.text = vm.IsLootMission ? Loc.T("GÖREV · İKMAL") : Loc.T("GÖREV · SUNUCU");
            abilityLabel.text = vm.AbilityLabel;
            traitGlyph.gameObject.SetActive(!string.IsNullOrEmpty(vm.TraitGlyph));
            if (!string.IsNullOrEmpty(vm.TraitGlyph)) DeskSprites.Apply(traitGlyph, vm.TraitGlyph);
            int per = targetPips.Length / Mathf.Max(1, targetRows.Length);
            for (int i = 0; i < targetRows.Length; i++)
            {
                bool show = i < vm.Targets.Length;
                targetRows[i].SetActive(show);
                if (!show) continue;
                var t = vm.Targets[i];
                DeskSprites.Apply(targetThumbs[i], t.Thumb);
                targetNames[i].text = string.IsNullOrEmpty(t.Cell) ? t.Name : Loc.F("{0} · {1}", t.Name, t.Cell);
                targetNames[i].fontStyle = t.Broken ? FontStyles.Strikethrough : FontStyles.Normal;
                targetGoals[i].gameObject.SetActive(i == 0 && !string.IsNullOrEmpty(vm.Goal));
                targetGoals[i].text = vm.Goal ?? "";
                targetCan[i].text = Loc.T("CAN");
                targetCan[i].gameObject.SetActive(!t.Broken);
                if (i < brokenStamps.Length)
                {
                    brokenStamps[i].gameObject.SetActive(t.Broken);
                    brokenStamps[i].text = vm.IsLootMission && vm.CargoLost ? Loc.T("KAYBOLDU") : vm.IsLootMission && (vm.Success || vm.Escaping) ? Loc.T("ALINDI") : Loc.T("KIRILDI");
                }
                for (int j = 0; j < per; j++)
                {
                    var pip = targetPips[i * per + j];
                    pip.gameObject.SetActive(!t.Broken && j < t.Max);
                    Pip(pip, j < t.Hp, C("labels.hpTag.colors.aiOwned"));
                }
            }
            civilianLabel.text = vm.TrustMax > 0 ? Loc.T("GÜVEN") : Loc.T("SİVİL");
            trustValue.gameObject.SetActive(vm.TrustMax > 0);
            trustValue.text = vm.Trust.ToString();
            civilianTitle.text = Loc.T("SİVİL SUNUCU");
            int civShown = Mathf.Min(vm.Civilians, civilians.Length), civBroken = Mathf.Min(vm.CivilianBroken, civShown);
            for (int i = 0; i < civilians.Length; i++)
            {
                civilians[i].gameObject.SetActive(i < civShown);
                DeskSprites.Apply(civilians[i], i >= civShown - civBroken ? "civ_broken_20" : "civ_ok_20");
            }
            civilianCount.text = (vm.Civilians - vm.CivilianBroken).ToString();
        }

        void Buttons(RaidViewModel vm)
        {
            logLabel.text = Loc.T("TELSİZ"); rulesLabel.text = Loc.T("KURALLAR"); retreatLabel.text = vm.RetreatArmed ? Loc.T("EMİN MİSİN?") : Loc.T("GERİ ÇEKİL");
            unread.gameObject.SetActive(vm.LogUnread && vm.LogUnreadCount > 0);
            unreadCount.text = vm.LogUnreadCount > 9 ? "9+" : vm.LogUnreadCount.ToString();
            // Makine evresinde GERİ ÇEKİL ve Geri al soluk; EMRİ İLET'in yerinde kilitli kâğıt. Kaçışta geri çekilme yok.
            bool machines = vm.Phase == "enemy" && !vm.Finished;
            if (retreatGroup != null) retreatGroup.alpha = machines || !vm.CanRetreat ? 0.4f : 1;
            retreat.interactable = !machines && vm.CanRetreat;
            undoLabel.text = Loc.T("Geri al");
            undo.interactable = vm.CanUndo && !vm.Locked && !vm.Paused;
            if (undoGroup != null) undoGroup.alpha = undo.interactable ? 1 : 0.45f;
            end.gameObject.SetActive(!machines);
            lockedPaper.SetActive(machines);
            lockedTitle.text = Loc.T("MAKİNELER HAREKET EDİYOR");
            lockedLine.text = vm.MachineMoves > 0 ? Loc.F("girdi kilitli · {0}/{1} hamle", Mathf.Max(1, vm.MachineMove), vm.MachineMoves) : Loc.T("girdi kilitli");
            endLabel.text = vm.EndConfirm ? Loc.T("EMİN MİSİN?") : Loc.T("EMRİ İLET");
            endLastTurn.gameObject.SetActive(vm.LastTurn && !vm.EndConfirm);
            endLastTurn.text = Loc.T("SON TUR");
            end.interactable = !vm.Locked && !vm.Paused;
        }

        /// <summary>Pip (S9 › B): dolu = var, kırmızı kenarlı boş = kayıp.</summary>
        public static void Pip(Image pip, bool full, Color ink)
        {
            DeskSprites.Apply(pip, full ? "pip_fill" : "pip_empty");
            pip.color = full ? ink : C("labels.hpTag.colors.aiOwned");
        }

        /// <summary>Uzman pipleri: can kadar dolu (mürekkep), kaybedilen kırmızı kenarlı boş.</summary>
        public static void Pips(Image[] pips, int hp, int max, Color ink)
        {
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i].gameObject.SetActive(i < max);
                if (i < max) Pip(pips[i], i < hp, ink);
            }
        }

        void Update()
        {
            if (cornerGroup != null)
            {
                float alpha = Mathf.MoveTowards(cornerGroup.alpha, Current?.Finished == true ? 0 : 1, Time.unscaledDeltaTime / .2f);
                if (cornerGroup.alpha != alpha) cornerGroup.alpha = alpha;
            }
            if (lastStamp != null && lastAt >= 0)
            {
                float p = Mathf.Clamp01((Time.unscaledTime - lastAt) / 0.12f);
                lastStamp.localScale = Vector3.one * Mathf.Lerp(1.5f, 1, p);
                if (p >= 1) lastAt = -1;
            }
            EndButton();
            var k = Keyboard.current; if (k == null || commands == null) return;
            // 1–3 kartların sırası: Teknisyen ve seçilen iki uzman
            var roster = Current?.Team;
            if (roster != null) for (int i = 0; i < roster.Length && i < 3; i++)
                if ((i == 0 ? k.digit1Key : i == 1 ? k.digit2Key : k.digit3Key).wasPressedThisFrame) commands.SelectUnit(roster[i].Letter);
            if (k.qKey.wasPressedThisFrame) commands.ArmAbility(); if (k.zKey.wasPressedThisFrame) commands.Undo(); if (k.enterKey.wasPressedThisFrame) commands.EndTurn();
            if (k.escapeKey.wasPressedThisFrame || Mouse.current?.rightButton.wasPressedThisFrame == true) commands.CancelPending();
            if (k.lKey.wasPressedThisFrame) commands.ToggleLog();
            if (k.f1Key.wasPressedThisFrame && !k.shiftKey.isPressed && !k.ctrlKey.isPressed || k.slashKey.wasPressedThisFrame && k.shiftKey.isPressed) commands.ToggleRules();
            if (k.spaceKey.wasPressedThisFrame) commands.SpeedUpMachines(!(Current?.FastMachines ?? false));
        }

        /// <summary>EMRİ İLET: üzerinde 2 px kalkar, basılı 2 px iner (S6 › etkileşim).</summary>
        void EndButton()
        {
            if (endFace == null || endHover == null || !end.gameObject.activeInHierarchy) return;
            bool active = end.interactable, pressed = active && endHover.Pressed, over = active && endHover.Over && !pressed;
            float lift = pressed ? -2 : over ? 2 : 0, drop = pressed ? 1 : over ? 5 : 3;
            endFace.anchoredPosition = new Vector2(0, lift);
            if (endShadow != null) endShadow.rectTransform.anchoredPosition = new Vector2(0, lift - drop);
        }
    }
}
