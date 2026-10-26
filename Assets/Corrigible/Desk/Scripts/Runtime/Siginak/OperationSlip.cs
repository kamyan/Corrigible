using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Görev fişi (K2 notu): haritanın 2 mm üstünde World Space dizin kâğıdı, −1° dönük. Başlık (OP kodu · bölge ·
    /// ilerleme), üç sektör satırı (işaret + no + ad + tür + damga; hazır satır sarı), görev, BEDEL / ÖDÜL, ekip satırı,
    /// mürekkep düğme “Enter · BASKIN EMRİ VER · −12 fon”. Cephe dışındaki bölgede bilgi fişi (eylem yok). Esc kapatır.
    /// İçerik <see cref="OperationSlipData"/> olarak gelir; fiş kararı vermez, düğme olayını bağlayıcı işler.
    /// </summary>
    public sealed class OperationSlip : MonoBehaviour
    {
        [Serializable] public sealed class Row
        {
            public RectTransform root;
            public Image highlight, mark, stampFrame;
            public TMP_Text no, name, kind, stamp;
        }

        [SerializeField] RectTransform paper;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text code, region, progress;
        [SerializeField] Button close;
        [SerializeField] Row[] rows = Array.Empty<Row>();
        [SerializeField] GameObject taskBlock, costBlock, teamBlock, actionBlock, infoBlock;
        [SerializeField] TMP_Text taskHead, task, cost, reward, teamName, teamMembers, teamStamp, info, reason;
        [SerializeField] Image teamStampFrame;
        [SerializeField] Button action;
        [SerializeField] Image actionFill;
        [SerializeField] TMP_Text actionLabel, actionCost;
        [SerializeField] RectTransform orderStamp;
        [SerializeField] RectTransform pin;
        [SerializeField] Sprite markDone, markReady, markLocked, frameSolid, frameDashed;

        float openAt = -1, stampAt = -1;
        Vector2 rest;

        public event Action ActionClicked, CloseClicked;
        public bool IsOpen => gameObject.activeSelf;
        public string RegionId { get; private set; }
        public bool HasAction { get; private set; }
        public bool ActionEnabled => action != null && action.interactable;
        /// <summary>Raptiye (fişin sol üstü); kırmızı ip buraya bağlanır.</summary>
        public Vector3 PinWorld => pin.position;
        /// <summary>Kâğıdın boyu (fiş pikseli).</summary>
        public Vector2 PaperSize => paper.rect.size;

        public void Configure(RectTransform paperRoot, CanvasGroup fade, TMP_Text codeText, TMP_Text regionText, TMP_Text progressText, Button closeButton,
            Row[] sectorRows, GameObject task, GameObject cost, GameObject team, GameObject actionRoot, GameObject infoRoot,
            TMP_Text taskHeadText, TMP_Text taskText, TMP_Text costText, TMP_Text rewardText, TMP_Text teamNameText, TMP_Text teamMembersText,
            TMP_Text teamStampText, TMP_Text infoText, TMP_Text reasonText, Image teamFrame, Button actionButton, Image actionBack,
            TMP_Text actionText, TMP_Text actionCostText, RectTransform order, RectTransform pinPoint, Sprite done, Sprite ready, Sprite locked,
            Sprite solid, Sprite dashed)
        {
            paper = paperRoot; group = fade; code = codeText; region = regionText; progress = progressText; close = closeButton; rows = sectorRows;
            taskBlock = task; costBlock = cost; teamBlock = team; actionBlock = actionRoot; infoBlock = infoRoot;
            taskHead = taskHeadText; this.task = taskText; this.cost = costText; reward = rewardText; teamName = teamNameText;
            teamMembers = teamMembersText; teamStamp = teamStampText; info = infoText; reason = reasonText; teamStampFrame = teamFrame;
            action = actionButton; actionFill = actionBack; actionLabel = actionText; actionCost = actionCostText; orderStamp = order;
            pin = pinPoint; markDone = done; markReady = ready; markLocked = locked; frameSolid = solid; frameDashed = dashed;
        }

        void Awake()
        {
            rest = paper.anchoredPosition;
            action.onClick.AddListener(() => ActionClicked?.Invoke());
            close.onClick.AddListener(() => CloseClicked?.Invoke());
        }

        public void Show(OperationSlipData d)
        {
            bool reopen = !IsOpen || RegionId != d.RegionId;
            RegionId = d.RegionId;
            HasAction = d.HasAction;
            var t = SiginakTheme.Active;
            gameObject.SetActive(true);
            // EN metin %20–30 uzar: fiş 436 → 452 px açılır, satır yükseklikleri sabit
            float width = DeskFormat.Lang == DeskLang.En ? t.Number("space.slipW.en", 452) : t.Number("space.slipW.tr", 436);
            paper.sizeDelta = new Vector2(width, paper.sizeDelta.y);

            code.text = d.Code;
            region.text = d.Region;
            progress.text = d.Progress;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var s = i < d.Sectors.Count ? d.Sectors[i] : null;
                row.root.gameObject.SetActive(s != null);
                if (s == null) continue;
                row.no.text = s.No;
                row.name.text = s.Name;
                row.kind.text = s.Kind;
                row.highlight.enabled = s.Highlight;
                row.mark.sprite = s.Mark == SectorMark.Retaken ? markDone : s.Mark == SectorMark.Ready ? markReady : markLocked;
                Stamp(row.stampFrame, row.stamp, s.Mark switch
                {
                    SectorMark.Retaken => ("stamp.retaken", Corrigible.Core.Loc.T("GERİ ALINDI")),
                    SectorMark.Ready => ("stamp.ready", Corrigible.Core.Loc.T("BASKINA HAZIR")),
                    _ => ("stamp.locked", Corrigible.Core.Loc.T("KİLİTLİ")),
                });
            }

            bool infoOnly = !string.IsNullOrEmpty(d.Info);
            infoBlock.SetActive(infoOnly);
            info.text = d.Info ?? "";
            taskBlock.SetActive(!infoOnly && !string.IsNullOrEmpty(d.Task));
            taskHead.text = d.TaskHead ?? "";
            task.text = d.Task ?? "";
            costBlock.SetActive(!infoOnly && !string.IsNullOrEmpty(d.Cost));
            cost.text = d.Cost ?? "";
            reward.text = d.Reward ?? "";
            teamBlock.SetActive(!infoOnly && !string.IsNullOrEmpty(d.TeamName));
            teamName.text = d.TeamName ?? "";
            teamMembers.text = d.TeamMembers ?? "";
            Stamp(teamStampFrame, teamStamp, (d.TeamReady ? "stamp.teamReady" : "stamp.injured", d.TeamStamp ?? ""));

            actionBlock.SetActive(d.HasAction);
            action.interactable = d.ActionEnabled;
            actionLabel.text = d.Action ?? "";
            actionCost.text = d.ActionCost ?? "";
            // Devre dışı düğme: soluk dolgu + kesik çizgi rengi, gerekçe altında (renk tek başına taşımaz)
            actionFill.color = t.ColorOf(d.ActionEnabled ? "ui.inkButton" : "ui.disabledFill");
            actionLabel.color = actionCost.color = t.ColorOf(d.ActionEnabled ? "ui.inkButtonText" : "ui.disabledText");
            reason.gameObject.SetActive(d.HasAction && !d.ActionEnabled && !string.IsNullOrEmpty(d.Reason));
            reason.text = d.Reason ?? "";
            if (reopen)
            {
                orderStamp.gameObject.SetActive(false);
                openAt = Time.unscaledTime;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(paper);
            Update();
        }

        public void Hide()
        {
            RegionId = null;
            gameObject.SetActive(false);
        }

        /// <summary>Baskın emri verildi: kırmızı “EMİR VERİLDİ” damgası 0,1 s'de iner (−6°).</summary>
        public void StampOrder()
        {
            orderStamp.gameObject.SetActive(true);
            stampAt = Time.unscaledTime;
            Update();
        }

        /// <summary>Baskın bitti: emir damgası kalkar (fiş açık kalabilir).</summary>
        public void ClearOrder()
        {
            if (orderStamp.gameObject.activeSelf) orderStamp.gameObject.SetActive(false);
            stampAt = -1;
        }

        void Stamp(Image frame, TMP_Text text, (string key, string word) s)
        {
            var t = SiginakTheme.Active;
            var c = t.ColorOf(s.key);
            bool dashed = t.Text(s.key + ".edge") == "dashed";
            frame.sprite = dashed ? frameDashed : frameSolid;
            frame.type = dashed ? Image.Type.Tiled : Image.Type.Sliced;
            frame.fillCenter = !dashed;
            frame.color = c;
            text.color = c;
            text.text = s.word;
        }

        void Update()
        {
            var t = SiginakTheme.Active;
            float now = Time.unscaledTime;
            if (openAt >= 0)
            {
                // Açılış: 12 px yukarıdan kayar (0,14 s)
                float p = Mathf.Clamp01((now - openAt) / Mathf.Max(0.01f, t.NumberOf("motion.slipOpenS", 0.14f)));
                float e = 1 - (1 - p) * (1 - p);
                paper.anchoredPosition = rest + new Vector2(0, 12 * (1 - e));
                group.alpha = e;
                if (p >= 1) openAt = -1;
            }
            if (stampAt >= 0)
            {
                float p = Mathf.Clamp01((now - stampAt) / Mathf.Max(0.01f, t.NumberOf("motion.stampS", 0.1f)));
                orderStamp.localScale = Vector3.one * Mathf.Lerp(1.4f, 1, p);
                if (p >= 1) stampAt = -1;
            }
        }
    }
}
