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
        [SerializeField] RaidBlueprintView blueprint;
        [SerializeField] TMP_Text crateContents;
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
        /// <summary>Tesis aşama 11: şerit hücrelerinin yerleşim öğeleri ve prefab'daki genişliği (8'den çok turda daralır).</summary>
        LayoutElement[] cellLayouts;
        float cellWidth = -1, cellGap;
        /// <summary>Şeridin prefab'daki hücre sayısının genişliği (8 hücre; hedef odası 9, savunma 10 tur aynı genişliğe daralarak sığar).</summary>
        const int StripFit = 8;
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
        [Header("Telsiz masası (tesis aşama 5)")]
        [Tooltip("Corners › Radio desk: YAYINDA lambası ve makaralı teyp; yalnız tesis baskınında (BlueprintWing > 0) görünür.")]
        [SerializeField] GameObject radioDesk;
        [SerializeField] RaidOnAirLampView onAirLamp;
        [SerializeField] RaidTapeDeckView tapeDeck;
        public RaidOnAirLampView OnAirLamp => onAirLamp;
        public RaidTapeDeckView TapeDeck => tapeDeck;
        /// <summary>Telsiz masası görünüyor (tesis baskını).</summary>
        public bool RadioDeskShown => radioDesk != null && radioDesk.activeSelf;

        IRaidDataSource source; IRaidCommands commands; bool abilityTipVisible, wasLast;
        float lastAt = -1;
        public RaidViewModel Current => source?.Current;
        /// <summary>Ekip kartının üzerine gelindi (kartın modeli: uzman harfle, aşama 9'un köpek kartı birim kimliğiyle).</summary>
        public event System.Action<RaidTeamModel> TeamHovered;

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

        /// <summary>
        /// Köşe panellerinin (durum, düğmeler, komut, yetenek kartı, kadro, pafta) kapladığı ekran dikdörtgenleri: piksel, sol alt köken
        /// (kaplama tuval). Tahta, adı ve can etiketlerini bu alanların dışına iter; gölge payı için kenardan <see cref="LabelKeepOutMargin"/>.
        /// </summary>
        public void CollectLabelKeepOuts(System.Collections.Generic.List<Rect> rects)
        {
            if (cornerGroup == null) return;
            var root = cornerGroup.transform;
            float margin = LabelKeepOutMargin * root.lossyScale.x;
            for (int i = 0; i < root.childCount; i++)
            {
                if (!(root.GetChild(i) is RectTransform panel) || !panel.gameObject.activeInHierarchy) continue;
                panel.GetWorldCorners(PanelCorners);
                rects.Add(Rect.MinMaxRect(PanelCorners[0].x - margin, PanelCorners[0].y - margin, PanelCorners[2].x + margin, PanelCorners[2].y + margin));
            }
        }
        const float LabelKeepOutMargin = 6;
        static readonly Vector3[] PanelCorners = new Vector3[4];

        void OnTeamHover(RaidTeamModel card) => TeamHovered?.Invoke(card);
        public void ShowAbilityTip() { abilityTipVisible = true; Refresh(); }
        public void HideAbilityTip() { abilityTipVisible = false; Refresh(); }
        void Refresh() { if (source?.Current != null) Render(source.Current); }

        public void Render(RaidViewModel vm)
        {
            if(blueprint!=null)blueprint.Render(vm);
            RadioDesk(vm);
            if(crateContents){crateContents.text=vm.CrateContents;crateContents.gameObject.SetActive(!string.IsNullOrEmpty(vm.CrateContents));}
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
            FitCells(vm.Turns);
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

        /// <summary>
        /// Tesis aşama 11: savunmanın 10 turu (Cell 9/10) şeridin 8 hücrelik genişliğine sığar: hücreler daralır, evre çipi yerinde kalır.
        /// 8 ve altı turda hücreler prefab'daki genişlikte (bugünkü görünüm).
        /// </summary>
        void FitCells(int turns)
        {
            if (cellLayouts == null)
            {
                cellLayouts = new LayoutElement[turnFills.Length];
                for (int i = 0; i < turnFills.Length; i++) cellLayouts[i] = turnFills[i] != null ? turnFills[i].transform.parent.GetComponent<LayoutElement>() : null;
                if (cellLayouts.Length > 0 && cellLayouts[0] != null) cellWidth = cellLayouts[0].preferredWidth;
                var row = turnFills.Length > 0 && turnFills[0] != null ? turnFills[0].transform.parent.parent.GetComponent<HorizontalLayoutGroup>() : null;
                cellGap = row != null ? row.spacing : 2;
            }
            if (cellWidth <= 0) return;
            int shown = Mathf.Min(turns, cellLayouts.Length);
            float w = shown > StripFit ? (StripFit * cellWidth + (StripFit - shown) * cellGap) / shown : cellWidth;
            foreach (var le in cellLayouts)
            {
                if (le == null || Mathf.Approximately(le.preferredWidth, w)) continue;
                le.preferredWidth = w;
                if (le.minWidth > w) le.minWidth = w;
            }
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

        /// <summary>
        /// Görev çipi üç satır: oda (yoksa GÖREV) · İKMAL + bölge özelliği; hedef (çizim, ad · kare, amaç, CAN); GÜVEN + sivil
        /// sunucu.
        /// </summary>
        void Mission(RaidViewModel vm)
        {
            // Tesis aşama 11: savunmada “{oda} · SAVUNMA” (saldırıda ve örnek tahtada Defense false: bugünkü)
            objective.text = vm.Defense ? (string.IsNullOrEmpty(vm.Place) ? Loc.T("GÖREV · SAVUNMA") : Loc.F("{0} · SAVUNMA", vm.Place))
                : string.IsNullOrEmpty(vm.Place)
                ? vm.IsLootMission ? Loc.T("GÖREV · İKMAL") : Loc.T("GÖREV · SUNUCU")
                : vm.IsLootMission ? Loc.F("{0} · İKMAL", vm.Place) : Loc.F("{0} · SUNUCU", vm.Place);
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
                // Max 0: tesis odasının geçit satırı (can taşımaz). Örnek tahtada her hedefin Max'ı > 0 ve Stamp'i null: bugünkü hâl
                targetCan[i].gameObject.SetActive(!t.Broken && t.Max > 0);
                if (i < brokenStamps.Length)
                {
                    brokenStamps[i].gameObject.SetActive(t.Broken);
                    // Tesis odasında yük yerde (taşıyıcı düştü; aşama 7): YERDE (örnek tahtada CargoOnFloor hep false)
                    brokenStamps[i].text = t.Stamp ?? (vm.IsLootMission && vm.CargoOnFloor ? Loc.T("YERDE") : vm.IsLootMission && vm.CargoLost ? Loc.T("KAYBOLDU") : vm.IsLootMission && (vm.Success || vm.Escaping) ? Loc.T("ALINDI") : Loc.T("KIRILDI"));
                }
                for (int j = 0; j < per; j++)
                {
                    var pip = targetPips[i * per + j];
                    pip.gameObject.SetActive(!t.Broken && j < t.Max);
                    // İkmal sandığı tehdit değil (tahtadaki etiketi gibi): canı mürekkeple
                    var ink = vm.IsLootMission ? C("ink") : C("labels.hpTag.colors.aiOwned");
                    Pip(pip, j < t.Hp, ink, ink);
                }
            }
            civilianLabel.text = vm.TrustMax > 0 ? Loc.T("GÜVEN") : Loc.T("SİVİL");
            trustValue.gameObject.SetActive(vm.TrustMax > 0);
            trustValue.text = vm.Trust.ToString();
            civilianTitle.text = Loc.T("SİVİL SUNUCU");
            // Tahtada simge yuvasından çok sunucu olabilir: yuvalar dolar, kırılan da işaretlenir; gerçek sayıyı yandaki rakam söyler
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
            logLabel.text = Loc.T("TELSİZ"); rulesLabel.text = Loc.T("KURALLAR"); retreatLabel.text = vm.RetreatArmed ? DeskLoc.Instance.Ctx("retreat", "EMİN MİSİN?") : Loc.T("GERİ ÇEKİL");
            // İkinci onay şeridi KURALLAR yazısının üstüne biner: yazı yerini koruyarak görünmez olur (düğme içeriği kaymaz)
            rulesLabel.alpha = vm.RetreatArmed ? 0 : 1;
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
            // Tesis odasında herkes kapıdayken düğme (ve Enter) GEÇ olur, onay sorulmaz; altında GERİ ALINMAZ. Örnek tahtada CanPass
            // hep false: bugünkü EMRİ İLET / EMİN MİSİN? ve SON TUR
            // Tesis aşama 11 (savunma; PassLabel dolu): GEÇ “İÇERİ ÇEKİL · güven −1” olur ve ilk basış onay sorar (EMİN MİSİN?). Saldırıda
            // PassLabel null: bugünkü GEÇ, onay GEÇ'i örtmez
            endLabel.text = vm.CanPass ? (vm.PassLabel != null ? (vm.EndConfirm ? Loc.T("EMİN MİSİN?") : vm.PassLabel) : Loc.T("GEÇ")) : vm.EndConfirm ? Loc.T("EMİN MİSİN?") : Loc.T("EMRİ İLET");
            endLastTurn.gameObject.SetActive(vm.CanPass || vm.LastTurn && !vm.EndConfirm);
            endLastTurn.text = vm.CanPass ? vm.PassNote ?? Loc.T("GERİ ALINMAZ") : Loc.T("SON TUR");
            end.interactable = !vm.Locked && !vm.Paused;
        }

        /// <summary>
        /// Telsiz masası (§7.3): yalnız tesis baskınında görünür (örnek tahtada BlueprintWing 0: köşe bugünkü gibi boş, tahta
        /// etiketlerinin kaçınma alanı da değişmez). Lamba emirle yanar, teyp kayıtlı paftaları yazar.
        /// </summary>
        void RadioDesk(RaidViewModel vm)
        {
            bool show = vm.BlueprintWing > 0;
            if (radioDesk != null && radioDesk.activeSelf != show) radioDesk.SetActive(show);
            if (!show) return;
            if (onAirLamp != null) onAirLamp.Render(vm);
            if (tapeDeck != null) tapeDeck.Render(vm);
        }

        /// <summary>Pip (S9 › B): dolu = var, kırmızı kenarlı boş = kayıp.</summary>
        public static void Pip(Image pip, bool full, Color ink, Color? empty = null)
        {
            DeskSprites.Apply(pip, full ? "pip_fill" : "pip_empty");
            pip.color = full ? ink : empty ?? C("labels.hpTag.colors.aiOwned");
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
            // 1–3 kartların sırası: Teknisyen ve seçilen iki uzman; 4 tesis aşama 9'un köpek kartı (birim kimliğiyle; seçili köpekteyken
            // sıradaki köpek). Köpeksiz kadroda dördüncü kart yok: 4 etkisiz
            var roster = Current?.Team;
            if (roster != null) for (int i = 0; i < roster.Length && i < DigitKeys.Length; i++)
                if (k[DigitKeys[i]].wasPressedThisFrame) { if (roster[i].UnitId > 0) commands.SelectUnitId(roster[i].UnitId); else commands.SelectUnit(roster[i].Letter); }
            foreach (var (key, id) in LetterKeys) if (k[key].wasPressedThisFrame) Dispatch(id);
            if (k.enterKey.wasPressedThisFrame) commands.EndTurn();
            if (k.escapeKey.wasPressedThisFrame || Mouse.current?.rightButton.wasPressedThisFrame == true) commands.CancelPending();
            if (k.f1Key.wasPressedThisFrame && !k.shiftKey.isPressed && !k.ctrlKey.isPressed || k.slashKey.wasPressedThisFrame && k.shiftKey.isPressed) commands.ToggleRules();
            if (k.spaceKey.wasPressedThisFrame) commands.SpeedUpMachines(!(Current?.FastMachines ?? false));
        }

        /// <summary>
        /// Baskının harf tuşları (tek kaynak; sahne testi tekilliği ve rakam, Enter, Esc, F1, Boşluk ile çakışmamayı denetler): Q yetenek,
        /// E tesis hamlesi (aşama 7; örnek tahtada etkisiz), R ekipman eylemi (aşama 9; örnek tahtada etkisiz), Z geri al, L telsiz kaydı.
        /// </summary>
        public static readonly (Key key, string id)[] LetterKeys = { (Key.Q, "ability"), (Key.E, "action"), (Key.R, "gear"), (Key.Z, "undo"), (Key.L, "log") };
        /// <summary>Ekip kartlarının rakam tuşları (kart sırası; dördüncü kart aşama 9'un köpeği).</summary>
        public static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

        void Dispatch(string id)
        {
            switch (id)
            {
                case "ability": commands.ArmAbility(); break;
                case "action": commands.ArmAction(); break;
                case "gear": commands.ArmGear(); break;
                case "undo": commands.Undo(); break;
                case "log": commands.ToggleLog(); break;
            }
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
