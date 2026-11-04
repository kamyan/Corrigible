using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Görev fişi (Revizyon 2 · S2): ekran katmanında 432 px kâğıt, sol masa kenarında (Kuzey ya da Güney Amerika seçiliyse
    /// sağda), −0,6° dönük. Başlık (OP kodu · bölge · ilerleme), SAHA satırları (bölge özelliği, imza makinesi, tehdit:
    /// işaret + etiket + değer), üç sektör satırı (işaret + no + ad + tür, damga 138 px sütunda; seçili satır sarı + ok
    /// ucu), görev, BEDEL / ÖDÜL 2 × 2, ekip diskleri, BASKIN EMRİ VER (açık · üzerinde · basılı · devre dışı · yetersiz
    /// kaynak). Cephe dışındaki bölgede bilgi fişi (eylem yok). İçerik <see cref="OperationSlipData"/> olarak gelir; fiş
    /// kararı vermez, düğme olayını bağlayıcı işler. Açılış ve kapanış dizisini (iğne, iplik) bağlayıcı yürütür; fiş yalnız
    /// kendi kayışını canlandırır.
    /// </summary>
    public sealed class OperationSlip : MonoBehaviour
    {
        [Serializable] public sealed class Field
        {
            public GameObject root;
            public Image glyph;
            public TMP_Text label, value;
            public UiHoverTarget hover;
        }

        [Serializable] public sealed class Row
        {
            public RectTransform root;
            public Image highlight, pointer, mark, stampFrame;
            public RectTransform stamp;
            public TMP_Text no, name, kind, stampText;
            public UiHoverTarget hover;
        }

        [Serializable] public sealed class Reward
        {
            public GameObject root;
            public TMP_Text amount, unit;
        }

        [Serializable] public sealed class Member
        {
            public GameObject root;
            public Image disc;
            public TMP_Text letter, hp;
        }

        /// <summary>
        /// Kadro seçicinin bir uzmanı: tıklanır (Teknisyen ve eğitim baskını kilitli); seçili sarı, üzerinde açık sarı. Üst satırda
        /// disk + can, altında çipin bütün genişliğinde durum (SABİT, YEDEK, n GÜN; CP 700 14).
        /// </summary>
        [Serializable] public sealed class PickChip
        {
            public GameObject root;
            public Button button;
            public UiHoverTarget hover;
            public Image fill, disc;
            public TMP_Text letter, hp, status;
        }

        [Tooltip("Kâğıt: −0,6° dönük, kayar; çocukları dikey dizilir.")]
        [SerializeField] RectTransform paper;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text code, region, progress;
        [Tooltip("Bölgenin binası: bölge adının altında (LF 600 18), baskınlar bu binanın odalarında geçer.")]
        [SerializeField] TMP_Text building;
        [Tooltip("Bina satırının üzerine gelme etiketi: binanın notu (bina ne, olay neden orada).")]
        [SerializeField] UiHoverTarget buildingHover;
        [SerializeField] Button close;
        [SerializeField] Image closeEdge;
        [SerializeField] UiHoverTarget closeHover;
        [SerializeField] GameObject fieldBlock;
        [SerializeField] Field[] fields = Array.Empty<Field>();
        [SerializeField] Row[] rows = Array.Empty<Row>();
        [SerializeField] GameObject taskBlock, costBlock, teamBlock, actionBlock, infoBlock;
        [SerializeField] TMP_Text taskHead, task, cost, info;
        [SerializeField] Reward[] rewards = Array.Empty<Reward>();
        [SerializeField] TMP_Text teamName, teamExtra, teamStamp;
        [SerializeField] RectTransform teamStampRoot;
        [Tooltip("Damganın sütunu: uzun damgada (SONRAKİ VARDİYA, TEKNİSYEN YARALI) genişler, en çok 160 px; sığmazsa iki satır.")]
        [SerializeField] LayoutElement teamStampHolder;
        [SerializeField] Image teamStampFrame;
        [SerializeField] Member[] members = Array.Empty<Member>();
        [Tooltip("KADRO satırı: Teknisyen sabit, öteki dört uzmandan ikisi seçilir.")]
        [SerializeField] GameObject pickBlock;
        [SerializeField] TMP_Text pickHead;
        [Tooltip("Başlığın üstüne gelince seçimin notu (Teknisyen sabit, eksik seçim, eğitim baskını).")]
        [SerializeField] UiHoverTarget pickHover;
        [SerializeField] PickChip[] pickChips = Array.Empty<PickChip>();
        [SerializeField] Button action;
        [SerializeField] UiHoverTarget actionHover;
        [Tooltip("Düğmenin yüzü (dolgu + yazılar): üzerinde 2 px kalkar, basılıyken 2 px iner; gölge altında kalır.")]
        [SerializeField] RectTransform actionFace;
        [SerializeField] Image actionFill, actionShadow;
        [SerializeField] TMP_Text actionLabel, actionCost, actionNote, shortfall;
        [Tooltip("İğne (ipin fiş ucu): solda sağ üst köşede, sağda sol üst köşede.")]
        [SerializeField] RectTransform pin;
        [SerializeField] Sprite markDone, markReady, markLocked, markReoccupied, frameSolid, frameDashed;
        [Tooltip("Tehdit kademesi işaretleri (threat_tier_1…4).")]
        [SerializeField] Sprite[] tierGlyphs = Array.Empty<Sprite>();
        [Tooltip("Ekran kenarından ve üstten uzaklık (1080p referans px).")]
        [SerializeField] Vector2 margin = new Vector2(22, 236);
        [Tooltip("İğnenin kâğıdın üst köşesinden uzaklığı (px).")]
        [SerializeField] Vector2 pinInset = new Vector2(13, 60);

        enum Phase { Hidden, Opening, Shown, Closing }

        Phase phase = Phase.Hidden;
        float phaseAt, slideDelay, pressAt = -1;
        bool right;
        SlipActionState actionState;
        readonly bool[] rowLocked = new bool[3], rowSelected = new bool[3];
        bool[] chipPicked = Array.Empty<bool>(), chipLocked = Array.Empty<bool>();

        public event Action ActionClicked, CloseClicked;
        /// <summary>Kadro seçicide bir uzmana tıklandı (rol kimliği).</summary>
        public event Action<string> PickClicked;
        string[] chipRoles = Array.Empty<string>();
        /// <summary>Fiş açık (kapanırken değil).</summary>
        public bool IsOpen => gameObject.activeSelf && phase != Phase.Closing;
        public string RegionId { get; private set; }
        public bool HasAction { get; private set; }
        public bool ActionEnabled => actionState == SlipActionState.Ready;
        public SlipActionState ActionState => actionState;
        /// <summary>Kâğıt sağda (Kuzey ya da Güney Amerika).</summary>
        public bool OnRight => right;
        /// <summary>Kâğıdın kendisi (kadro raporu yanına oturur, kayarken onunla gider).</summary>
        public RectTransform Paper => paper;
        /// <summary>Kâğıdın görünürlüğü (açılış gecikmesinde 0); kadro raporu bununla birlikte belirir.</summary>
        public float Alpha => group.alpha;

        /// <summary>İğnenin ekran noktası, kâğıt yerine oturmuşken (açılırken ip önce oraya çekilir).</summary>
        public Vector2 PinScreenAtRest
        {
            get
            {
                var parent = (RectTransform)paper.parent;
                var offset = parent.TransformVector(paper.anchoredPosition - RestPosition);
                return (Vector2)(pin.position - offset);
            }
        }

        static float SlideSeconds => SiginakTheme.Active.NumberOf("motion.slipSlideS", 0.22f);
        static float CloseSeconds => 0.18f;

        Vector2 RestPosition => new Vector2(right ? -(margin.x + paper.rect.width * 0.5f) : margin.x + paper.rect.width * 0.5f, -margin.y);

        void Awake()
        {
            // Tıklanan düğme EventSystem'de seçili kalmasın: Enter'ı yalnız masa okur (yoksa Submit de basar, rapor aynı karede onaylanır)
            DeskUi.NoNavigation(action);
            action.onClick.AddListener(() => ActionClicked?.Invoke());
            close.onClick.AddListener(() => CloseClicked?.Invoke());
            chipRoles = new string[pickChips.Length];
            chipPicked = new bool[pickChips.Length];
            chipLocked = new bool[pickChips.Length];
            for (int i = 0; i < pickChips.Length; i++)
            {
                int k = i;
                pickChips[i].button.onClick.AddListener(() => { if (!chipLocked[k] && chipRoles[k] != null) PickClicked?.Invoke(chipRoles[k]); });
            }
        }

        /// <summary>
        /// İçeriği yazar. Kapalıysa açılır: kâğıt <paramref name="slideDelay"/> sonra kenardan kayar (iğne ve iplik önce,
        /// bağlayıcıda). <paramref name="onRight"/> kâğıdın tarafı.
        /// </summary>
        public void Show(OperationSlipData d, bool onRight, float slideAfter = 0)
        {
            bool opening = !IsOpen;
            RegionId = d.RegionId;
            HasAction = d.HasAction;
            var t = SiginakTheme.Active;
            gameObject.SetActive(true);
            if (opening || right != onRight) Place(onRight);

            code.text = d.Code;
            region.text = d.Region;
            building.gameObject.SetActive(!string.IsNullOrEmpty(d.Building));
            building.text = d.Building ?? "";
            buildingHover.Title = d.BuildingHoverTitle;
            buildingHover.Line = d.BuildingHoverLine;
            progress.text = d.Progress;
            Fields(d, t);
            Sectors(d, t);

            bool infoOnly = !string.IsNullOrEmpty(d.Info);
            infoBlock.SetActive(infoOnly);
            info.text = d.Info ?? "";
            taskBlock.SetActive(!infoOnly && !string.IsNullOrEmpty(d.Task));
            taskHead.text = d.TaskHead ?? "";
            task.text = d.Task ?? "";
            costBlock.SetActive(!infoOnly && !string.IsNullOrEmpty(d.Cost));
            cost.text = d.Cost ?? "";
            for (int i = 0; i < rewards.Length; i++)
            {
                var r = i < d.Rewards.Count ? d.Rewards[i] : null;
                rewards[i].root.SetActive(r != null);
                if (r == null) continue;
                rewards[i].amount.text = r.Amount;
                rewards[i].unit.text = r.Unit;
            }
            Team(d, t);
            Pick(d, t);
            Action(d, t);

            if (opening)
            {
                phase = Phase.Opening;
                phaseAt = Time.unscaledTime;
                slideDelay = slideAfter;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(paper);
            Update();
        }

        /// <summary>Kapanış: kâğıt kenara çekilir (0,18 s), sonra gizlenir. İpi bağlayıcı önce sarar (<paramref name="delay"/>).</summary>
        public void Close(float delay = 0)
        {
            if (!gameObject.activeSelf || phase == Phase.Closing) return;
            phase = Phase.Closing;
            phaseAt = Time.unscaledTime + delay;
        }

        /// <summary>Hemen gizler (yeni oyun, kayıttan devam).</summary>
        public void Hide()
        {
            RegionId = null;
            phase = Phase.Hidden;
            gameObject.SetActive(false);
        }

        /// <summary>Enter ile basış: yüz 0,06 s iner (fareyle basmakla aynı).</summary>
        public void Press() => pressAt = Time.unscaledTime;

        void Place(bool onRight)
        {
            right = onRight;
            paper.anchorMin = paper.anchorMax = new Vector2(right ? 1 : 0, 1);
            paper.pivot = new Vector2(0.5f, 1);
            paper.anchoredPosition = RestPosition;
            float w = paper.rect.width;
            pin.anchorMin = pin.anchorMax = new Vector2(0, 1);
            pin.anchoredPosition = new Vector2(right ? pinInset.x : w - pinInset.x, -pinInset.y);
        }

        void Fields(OperationSlipData d, SiginakTheme t)
        {
            fieldBlock.SetActive(d.Fields.Count > 0);
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                var data = i < d.Fields.Count ? d.Fields[i] : null;
                f.root.SetActive(data != null);
                if (data == null) continue;
                if (data.Tier > 0 && tierGlyphs.Length > 0)
                {
                    f.glyph.sprite = tierGlyphs[Mathf.Clamp(data.Tier, 1, tierGlyphs.Length) - 1];
                    f.glyph.color = Color.white;
                }
                else
                {
                    DeskSprites.Apply(f.glyph, data.Glyph);
                    f.glyph.color = t.ColorOf("ink");
                }
                f.label.text = data.Label;
                f.value.text = data.Value;
                // Tehdit değeri kırmızı CP 700 20; öteki değerler LF 600 18 mürekkep
                bool tier = data.Tier > 0;
                t.ApplyType(f.value, tier ? "costValue" : "sahaValue");
                f.value.color = tier ? t.ColorOf("status.threat") : t.ColorOf("ink");
                f.hover.Title = data.HoverTitle;
                f.hover.Line = data.HoverLine;
            }
        }

        void Sectors(OperationSlipData d, SiginakTheme t)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var s = i < d.Sectors.Count ? d.Sectors[i] : null;
                row.root.gameObject.SetActive(s != null);
                if (s == null) continue;
                // No CP 700 15 ink3, ad LF 600 18 (kilitli satırda ink2)
                row.no.text = s.No;
                row.name.text = s.Name;
                row.name.color = s.Mark == SectorMark.Locked ? t.ColorOf("ink2") : t.ColorOf("ink");
                row.kind.text = s.Kind;
                rowLocked[i] = s.Mark == SectorMark.Locked;
                rowSelected[i] = s.Highlight;
                row.pointer.gameObject.SetActive(s.Highlight);
                row.mark.sprite = s.Reoccupied ? markReoccupied : s.Mark == SectorMark.Retaken ? markDone : s.Mark == SectorMark.Ready ? markReady : markLocked;
                var (key, word, angle) = s.Mark switch
                {
                    SectorMark.Retaken => ("stamp.retaken", Corrigible.Core.Loc.T("GERİ ALINDI"), 4f),
                    SectorMark.Ready => ("stamp.ready", Corrigible.Core.Loc.T("BASKINA HAZIR"), 3f),
                    _ => ("stamp.locked", Corrigible.Core.Loc.T("KİLİTLİ"), 2f),
                };
                Stamp(row.stamp, row.stampFrame, row.stampText, key, word, angle);
                row.hover.Title = s.Hover != null ? word : null;
                row.hover.Line = s.Hover;
            }
        }

        void Team(OperationSlipData d, SiginakTheme t)
        {
            bool show = !string.IsNullOrEmpty(d.TeamName) && string.IsNullOrEmpty(d.Info);
            teamBlock.SetActive(show);
            if (!show) return;
            teamName.text = d.TeamName;
            for (int i = 0; i < members.Length; i++)
            {
                var m = members[i];
                var data = i < d.Members.Count ? d.Members[i] : null;
                m.root.SetActive(data != null);
                if (data == null) continue;
                m.letter.text = data.Letter;
                m.hp.text = data.Rookie ? Corrigible.Core.Loc.F("{0} yedek", data.Hp) : data.Hp;
                // Eksik rol soluk disk; yedek uzmanın diski aynı, canın yanında “yedek”
                m.disc.color = data.Missing ? t.ColorOf("ui.disabledFill") : t.ColorOf("slip.teamDisc");
                m.hp.color = data.Missing ? t.ColorOf("ink3") : t.ColorOf("ink");
            }
            teamExtra.gameObject.SetActive(!string.IsNullOrEmpty(d.TeamExtra));
            teamExtra.text = d.TeamExtra ?? "";
            Stamp(teamStampRoot, teamStampFrame, teamStamp, d.TeamReady ? "stamp.teamReady" : "stamp.injured",
                d.TeamStamp ?? "", 3, 160);
            teamStampHolder.preferredWidth = teamStampHolder.minWidth = Mathf.Max(80, teamStampRoot.sizeDelta.x + 6);
        }

        /// <summary>KADRO: disk + can (altında durum); seçili sarı dolgu, seçilmemişin diski soluk, kilitli olan tıklanmaz.</summary>
        void Pick(OperationSlipData d, SiginakTheme t)
        {
            bool show = pickBlock != null && d.Pick.Count > 0 && string.IsNullOrEmpty(d.Info);
            if (pickBlock != null) pickBlock.SetActive(show);
            if (!show) return;
            pickHead.text = d.PickHead ?? "";
            for (int i = 0; i < pickChips.Length; i++)
            {
                var c = pickChips[i];
                var p = i < d.Pick.Count ? d.Pick[i] : null;
                c.root.SetActive(p != null);
                chipRoles[i] = p?.Role;
                if (p == null) continue;
                chipPicked[i] = p.Picked;
                chipLocked[i] = p.Locked;
                c.button.interactable = !p.Locked;
                c.letter.text = p.Letter;
                c.hp.text = p.Hp;
                // Durum canın altında değil çipin bütün genişliğinde (dar can sütununa sığmıyordu, yandaki çipe taşıyordu)
                c.status.gameObject.SetActive(!string.IsNullOrEmpty(p.Status));
                c.status.text = p.Status ?? "";
                c.disc.color = p.Picked ? t.ColorOf("slip.teamDisc") : t.ColorOf("ui.disabledFill");
                c.letter.color = p.Picked ? t.ColorOf("ink") : t.ColorOf("ink3");
                c.hp.color = c.status.color = p.Picked ? (p.Down ? t.ColorOf("status.threat") : t.ColorOf("ink")) : t.ColorOf("ink3");
                c.hover.Title = p.HoverTitle;
                c.hover.Line = p.HoverLine;
            }
            if (pickHover != null) { pickHover.Title = d.PickHead; pickHover.Line = d.PickNote; }
        }

        void Action(OperationSlipData d, SiginakTheme t)
        {
            actionBlock.SetActive(d.HasAction);
            actionState = d.ActionState;
            // Yetersiz kaynakta düğme açık kalır (basınca komut çağrılmaz); devre dışında Button kapalı
            action.interactable = d.HasAction && d.ActionState != SlipActionState.Disabled;
            actionLabel.text = d.Action ?? "";
            bool disabled = d.ActionState == SlipActionState.Disabled;
            // Bedel: yetersizse üstü çizik; devre dışında yerine kısa gerekçe
            actionCost.gameObject.SetActive(!disabled);
            actionCost.text = d.ActionState == SlipActionState.Shortfall ? "<s>" + d.ActionCost + "</s>" : d.ActionCost ?? "";
            actionNote.gameObject.SetActive(disabled && !string.IsNullOrEmpty(d.ActionNote));
            actionNote.text = d.ActionNote ?? "";
            shortfall.gameObject.SetActive(d.ActionState == SlipActionState.Shortfall);
            shortfall.text = d.Shortfall ?? "";
            actionFill.color = t.ColorOf(disabled ? "slip.buttonDisabled" : "ui.inkButton");
            actionLabel.color = actionNote.color = t.ColorOf(disabled ? "slip.buttonDisabledText" : "ui.inkButtonText");
            actionCost.color = t.ColorOf("slip.buttonKey");
            actionShadow.gameObject.SetActive(!disabled);
            actionHover.Title = d.ActionState == SlipActionState.Ready ? null : disabled ? d.ActionNote : Corrigible.Core.Loc.T("FON YETERSİZ");
            actionHover.Line = d.Reason;
        }

        void Stamp(RectTransform root, Image frame, TMP_Text text, string key, string word, float angle, float maxWidth = 0)
        {
            var t = SiginakTheme.Active;
            var c = t.ColorOf(key);
            bool dashed = t.Text(key + ".edge") == "dashed";
            frame.sprite = dashed ? frameDashed : frameSolid;
            frame.type = dashed ? Image.Type.Tiled : Image.Type.Sliced;
            frame.fillCenter = !dashed;
            frame.color = c;
            text.color = c;
            text.text = word;
            // Damga yazıya göre (CP 700 14, kenar 2 px, iç pay 2/6 px); −2…−4° dönük. Sütuna sığmayan iki satıra kırılır.
            var pad = new Vector2(16, 8);
            var size = text.GetPreferredValues(word);
            bool wrap = maxWidth > 0 && size.x + pad.x > maxWidth;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            if (wrap) size = new Vector2(maxWidth - pad.x, text.GetPreferredValues(word, maxWidth - pad.x, 0).y);
            root.sizeDelta = size + pad;
            root.localRotation = Quaternion.Euler(0, 0, angle);
        }

        void Update()
        {
            var t = SiginakTheme.Active;
            float now = Time.unscaledTime;
            // Kayış: açılırken kenardan oturur, kapanırken kenara çekilir
            float away = (paper.rect.width + margin.x + 24) * (right ? 1 : -1);
            switch (phase)
            {
                case Phase.Opening:
                {
                    float p = Mathf.Clamp01((now - phaseAt - slideDelay) / SlideSeconds);
                    float e = 1 - (1 - p) * (1 - p);
                    paper.anchoredPosition = RestPosition + new Vector2(away * (1 - e), 0);
                    group.alpha = now - phaseAt < slideDelay ? 0 : 1;
                    if (p >= 1) phase = Phase.Shown;
                    break;
                }
                case Phase.Closing:
                {
                    float p = Mathf.Clamp01((now - phaseAt) / CloseSeconds);
                    paper.anchoredPosition = RestPosition + new Vector2(away * p * p, 0);
                    if (p >= 1) Hide();
                    return;
                }
                case Phase.Shown:
                    paper.anchoredPosition = RestPosition;
                    group.alpha = 1;
                    break;
            }

            // Sektör satırı: seçili sarı %34 + ok ucu; üzerinde sarı %18 (kilitli satır seçilmez)
            for (int i = 0; i < rows.Length && i < rowLocked.Length; i++)
            {
                var row = rows[i];
                if (!row.root.gameObject.activeSelf) continue;
                bool rowOver = row.hover.Over && !rowLocked[i];
                var c = rowSelected[i] ? t.ColorOf("slip.rowSelected") : rowOver ? t.ColorOf("slip.rowHover") : Color.clear;
                row.highlight.enabled = c.a > 0;
                row.highlight.color = c;
            }

            // Kadro: seçili sarı %34, üzerinde sarı %18 (kilitli uzman seçilmez)
            for (int i = 0; i < pickChips.Length && i < chipPicked.Length; i++)
            {
                var c = pickChips[i];
                if (!c.root.activeSelf) continue;
                bool chipOver = c.hover.Over && !chipLocked[i];
                var fill = chipPicked[i] ? t.ColorOf("slip.rowSelected") : chipOver ? t.ColorOf("slip.rowHover") : Color.clear;
                c.fill.color = fill;
            }

            // Esc: üzerinde çerçeve koyulaşır
            closeEdge.color = closeHover.Over ? t.ColorOf("ink") : t.ColorOf("ink2");

            // BASKIN EMRİ VER: üzerinde 2 px kalkar (gölge 5), basılı 2 px iner (gölge 0), Enter'da 0,06 s basılı
            bool disabled = actionState == SlipActionState.Disabled;
            bool pressed = !disabled && (actionHover.Pressed || pressAt >= 0 && now - pressAt < 0.06f);
            bool over = !disabled && actionHover.Over && !pressed;
            float lift = pressed ? -2 : over ? 2 : 0, shadow = pressed ? 0 : over ? 5 : 3;
            actionFace.anchoredPosition = new Vector2(0, lift);
            actionShadow.rectTransform.anchoredPosition = new Vector2(0, lift - shadow);
        }
    }
}
