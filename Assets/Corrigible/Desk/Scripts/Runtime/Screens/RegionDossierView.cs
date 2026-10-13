using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bölge dosyası (C1 §7.1): klasör sekmesi “DOSYA R-10”; kapak etiketi (ad, kod, damga, meta + izleme durumu), sızma
    /// sayfası (güven, büyük değer + kalem halkası, ±, aralık çubuğu, kaynak tablosu), 3 istatistik kartı, 2×2 eylem kartı,
    /// önizleme notu, saha bölümü (ekip satırı, baskın düğmesi, not). Yerleşim prefab'da sabit; metne bağlı satırlar DeskRow.
    /// </summary>
    public sealed class RegionDossierView : MonoBehaviour, IIllusionAware
    {
        [SerializeField] FolderWindow window;

        [Header("Kapak")]
        [SerializeField] DeskRow coverTitleRow;
        [SerializeField] TMP_Text coverName, coverCode;
        [SerializeField] StampView coverStamp;
        [SerializeField] DeskRow coverMetaRow;
        [SerializeField] TMP_Text coverMeta;
        [SerializeField] Image monitoringIcon;
        [SerializeField] TMP_Text monitoringText;

        [Header("Sızma")]
        [SerializeField] DeskRow infHeadRow;
        [SerializeField] TMP_Text infCap;
        [SerializeField] TrustMarkView infTrust;
        [Tooltip("Doğrulanmış sızmada başlıkta küçük ONAYLANDI damgası (C2); öteki güvenlerde işaret.")]
        [SerializeField] StampView infStamp;
        [SerializeField] TMP_Text infRight;
        [SerializeField] TMP_Text infValue;
        [SerializeField] HandRingView infRing;
        [SerializeField] TMP_Text infQuestion;
        [SerializeField] TMP_Text infPlusMinus;
        [SerializeField] TMP_Text infNote;
        [SerializeField] RangeBarView range;
        [SerializeField] TMP_Text[] sourceHeads = System.Array.Empty<TMP_Text>();
        [Tooltip("Kaynak satırları: ad, değer, gün, güven (TrustMark), damga (ONAYLANDI), ayraç.")]
        [SerializeField] SourceRowRefs[] sourceRows = System.Array.Empty<SourceRowRefs>();

        [Header("İstatistik, eylemler")]
        [SerializeField] StatRefs[] stats = System.Array.Empty<StatRefs>();
        [SerializeField] TMP_Text actionsCap, actionsRight;
        [SerializeField] ActionCardView[] actions = System.Array.Empty<ActionCardView>();

        [Header("Önizleme")]
        [SerializeField] GameObject previewNote;
        [SerializeField] TMP_Text previewCap, previewWhat, previewWhen;
        [SerializeField] DeskRow previewHeadRow;
        [SerializeField] DeskRow previewTrustRow;
        [SerializeField] TMP_Text previewTrustLabel;
        [SerializeField] TrustMarkView previewFrom;
        [SerializeField] StampView previewTo;
        [SerializeField] TMP_Text previewMono;

        [Header("Saha")]
        [SerializeField] TMP_Text fieldCap, fieldRight;
        [Tooltip("Saha başlığının çizgisi: sağdaki metne kadar uzanır (aralık 8 px).")]
        [SerializeField] RectTransform fieldRule;
        [SerializeField] GameObject teamCard;
        [SerializeField] TMP_Text teamLetter, teamName, teamWhere, moraleLabel, lastContact;
        [SerializeField] Image[] morale = System.Array.Empty<Image>();
        [SerializeField] DeskButton raid;
        [Tooltip("Bölgede ekip yokken baskın düğmesinin yerinde: “Ekip gönder” ve hazır her ekip için bir fiş (en yakını mürekkep, B).")]
        [SerializeField] DeskRow sendRow;
        [SerializeField] TMP_Text sendLabel;
        [SerializeField] DeskButton[] sendChips = System.Array.Empty<DeskButton>();
        [SerializeField] TMP_Text fieldNote;

        [Header("Dikey yığın (CSS .fbody: flex sütun, aralık 6; C2'de tek kaynak satırıyla bloklar yukarı kayar)")]
        [SerializeField] RectTransform coverPanel;
        [SerializeField] RectTransform infPanel;
        [Tooltip("Sızma sayfasında başlığın altındaki öğeler (başlık damgası 1 px uzunsa birlikte kayar).")]
        [SerializeField] RectTransform[] infBelowHead = System.Array.Empty<RectTransform>();
        [SerializeField] RectTransform[] statsRow = System.Array.Empty<RectTransform>();
        [SerializeField] RectTransform[] actionsHead = System.Array.Empty<RectTransform>();
        [SerializeField] RectTransform[] fieldHead = System.Array.Empty<RectTransform>();
        [SerializeField] float stackTop = 40, gap = 6, coverHeight = 64, infBaseHeight = 208.5f, infHeadBase = 18, sourceRowHeight = 25,
            statsHeight = 52, actionsHeadAdvance = 20.5f, cardHeight = 66, dangerCardHeight = 68, cardGap = 7, previewAdvance = 84,
            fieldHeadAdvance = 21, teamAdvance = 55, raidAdvance = 42;

        float appliedHeadShift;
        RectTransform previewArrow;

        [System.Serializable]
        public struct SourceRowRefs
        {
            public RectTransform root;
            public CarbonText name, value;
            public TMP_Text day;
            public TrustMarkView mark;
            public StampView stamp;
        }

        [System.Serializable]
        public struct StatRefs
        {
            public PaperPanel panel;
            public TMP_Text label;
            public DeskRow valueRow;
            public CarbonText value;
            public TrustMarkView mark;
        }

        public FolderWindow Window => window;
        public DossierVM Data { get; private set; }

        /// <summary>Üstüne gelinen eylemin önizlemesi; −1 = varsayılan (dosyanın kendi önizlemesi ya da ilk eylem).</summary>
        public void ShowPreviewFor(int index)
        {
            if (Data == null) return;
            PreviewVM p = index >= 0 && index < Data.Actions.Count ? Data.Actions[index].Preview : null;
            BindPreview(p ?? Data.Preview ?? (Data.Actions.Count > 0 ? Data.Actions[0].Preview : null));
        }
        public IReadOnlyList<ActionCardView> Actions => actions;
        public DeskButton Raid => raid;
        public IReadOnlyList<DeskButton> SendChips => sendChips;
        /// <summary>Seçilen ekip fişinin komutu (“Team:Send:alfa:DAS”).</summary>
        public event System.Action<string> SendInvoked;

        void Awake()
        {
            for (int i = 0; i < sendChips.Length; i++)
            {
                int k = i;
                sendChips[i].onClick.AddListener(() =>
                {
                    var send = Data?.Field?.Send;
                    if (send != null && k < send.Count) SendInvoked?.Invoke(send[k].Command);
                });
            }
        }

        /// <summary>İllüzyon: el halkası ve “?” gizli, damgalar düz; aynı VM yeniden çizilir.</summary>
        public void SetIllusion(bool on) { if (Data != null) Bind(Data); }

        public void Bind(DossierVM d)
        {
            Data = d;
            if (d == null) return;
            var T = DeskLoc.Instance;
            window.SetTitle(d.Tab?.Title, d.Tab?.Subtitle);

            if (d.Cover != null)
            {
                DeskUi.SetText(coverName, d.Cover.Name);
                DeskUi.SetText(coverCode, d.Cover.Code);
                if (d.Cover.Stamp != null)
                    coverStamp.Set(T.T(d.Cover.Stamp.Text), StampView.ParseInk(d.Cover.Stamp.Color), StampSize.Normal, d.Cover.Stamp.Rot, "cover." + d.RegionId, d.Cover.Stamp.FontSize);
                coverTitleRow.Layout();
                DeskUi.SetText(coverMeta, T.T(d.Cover.Meta));
                var mon = d.Cover.Monitoring;
                DeskUi.SetActive(monitoringIcon, mon != null && !mon.Present);
                DeskUi.SetText(monitoringText, mon != null ? T.T(mon.Text) : "");
                DeskUi.Tint(monitoringText, mon != null && !mon.Present ? "trust.suspicious.ink" : "ink.ink2");
                coverMetaRow.Layout();
            }

            var inf = d.Infiltration;
            if (inf != null)
            {
                DeskUi.SetText(infCap, T.T(inf.Cap));
                var infT = DeskUi.ParseTrust(inf.Trust);
                bool stamped = infT == TrustLevel.Verified && infStamp != null;
                DeskUi.SetActive(infTrust, !stamped);
                DeskUi.SetActive(infStamp, stamped);
                if (stamped) infStamp.Set(T.T("ONAYLANDI"), StampInk.Blue, StampSize.Small, null, "inf." + d.RegionId);
                else infTrust.Set(infT);
                DeskUi.SetText(infRight, T.T(inf.Right));
                infHeadRow.Layout();
                DeskUi.SetText(infValue, inf.Value);
                DeskUi.SetActive(infRing, inf.HandRing && !DeskTheme.Illusion);
                DeskUi.SetActive(infQuestion, inf.QuestionMark && !DeskTheme.Illusion);
                DeskUi.SetText(infPlusMinus, inf.PlusMinus);
                DeskUi.SetActive(infPlusMinus, !string.IsNullOrEmpty(inf.PlusMinus));
                DeskUi.SetText(infNote, T.T(inf.Note));
                if (inf.Range != null)
                {
                    var markers = new List<RangeBarView.Marker>();
                    foreach (var m in inf.Range.Markers)
                    {
                        ColorUtility.TryParseHtmlString(m.Color, out var c);
                        ColorUtility.TryParseHtmlString(m.LabelColor ?? m.Color, out var lc);
                        markers.Add(new RangeBarView.Marker { at = m.At, color = c, label = T.T(m.Label), labelColor = lc, labelLeft = m.LabelSide == "left" });
                    }
                    range.Set(inf.Range.Min, inf.Range.Max, inf.Range.Ticks,
                        inf.Range.Band != null && inf.Range.Band.Length == 2 ? new Vector2(inf.Range.Band[0], inf.Range.Band[1]) : (Vector2?)null, markers);
                }
            }

            if (d.Sources != null)
            {
                for (int i = 0; i < sourceHeads.Length && i < d.Sources.Columns.Count; i++) DeskUi.SetText(sourceHeads[i], T.T(d.Sources.Columns[i]));
                for (int i = 0; i < sourceRows.Length; i++)
                {
                    var r = sourceRows[i];
                    bool on = i < d.Sources.Rows.Count;
                    DeskUi.SetActive(r.root, on);
                    if (!on) continue;
                    var s = d.Sources.Rows[i];
                    var trust = DeskUi.ParseTrust(s.Trust);
                    bool report = trust == TrustLevel.Report;
                    r.name.Set(T.T(s.Name), report);
                    r.value.Set(s.Value, report);
                    DeskUi.SetText(r.day, s.Day);
                    DeskUi.SetActive(r.stamp, trust == TrustLevel.Verified);
                    DeskUi.SetActive(r.mark, trust != TrustLevel.Verified);
                    if (trust == TrustLevel.Verified) r.stamp.Set(T.T("ONAYLANDI"), StampInk.Blue, StampSize.Small, s.StampRot, "source." + i);
                    else r.mark.Set(trust);
                }
            }

            for (int i = 0; i < stats.Length; i++)
            {
                var st = stats[i];
                bool on = i < d.Stats.Count;
                DeskUi.SetActive(st.panel, on);
                if (!on) continue;
                var s = d.Stats[i];
                var trust = DeskUi.ParseTrust(s.Trust);
                st.panel.SetKind(trust == TrustLevel.Report ? PaperKind.Carbon : PaperKind.Card);
                DeskUi.SetText(st.label, T.T(s.Label));
                st.value.Set(s.Value, trust == TrustLevel.Report);
                st.mark.Set(trust, trust == TrustLevel.Report, trust == TrustLevel.Report ? "~" : null);
                st.valueRow.Layout();
                st.value.Apply();
            }

            DeskUi.SetText(actionsCap, T.T(d.ActionsHeader?.Title));
            DeskUi.SetText(actionsRight, d.ActionsHeader?.Right);
            for (int i = 0; i < actions.Length; i++)
            {
                bool on = i < d.Actions.Count;
                DeskUi.SetActive(actions[i], on);
                if (on) { actions[i].Index = i; actions[i].Bind(d.Actions[i]); }
            }

            ShowPreviewFor(-1);

            if (d.Field != null)
            {
                DeskUi.SetText(fieldCap, T.T(d.Field.Cap));
                DeskUi.SetText(fieldRight, T.T(d.Field.Right));
                if (fieldRule != null && fieldRight != null)
                {
                    // CSS: başlık · 8 · çizgi · 8 · sağdaki metin; “Bölgede ekip yok” gibi uzun metin çizginin üstüne binmesin
                    var rr = fieldRight.rectTransform;
                    float rightLeft = rr.anchoredPosition.x + (1 - rr.pivot.x) * rr.rect.width - DeskUi.TextWidth(fieldRight, fieldRight.text);
                    float w = Mathf.Max(0, Mathf.Round((rightLeft - 8 - fieldRule.anchoredPosition.x) * 2) / 2);
                    if (!Mathf.Approximately(fieldRule.sizeDelta.x, w)) fieldRule.sizeDelta = new Vector2(w, fieldRule.sizeDelta.y);
                }
                var t = d.Field.Team;
                DeskUi.SetActive(teamCard, t != null);
                if (t != null)
                {
                    DeskUi.SetText(teamLetter, t.Id);
                    DeskUi.SetText(teamName, T.T(t.Name));
                    DeskUi.SetText(teamWhere, T.T(t.Where));
                    DeskUi.SetText(moraleLabel, T.T(t.MoraleLabel));
                    DeskUi.SetText(lastContact, T.T(t.LastContact));
                    for (int i = 0; i < morale.Length; i++)
                    {
                        DeskUi.SetActive(morale[i], i < t.MoraleMax);
                        DeskSprites.Apply(morale[i], i < t.Morale ? "sh_fill_r0" : "sh_frame_r0_1");
                    }
                }
                if (d.Field.Raid != null) { raid.SetLabel(d.Field.Raid.Label); raid.SetShortcut(d.Field.Raid.Key); }
                // Komutun tuşu yoksa kapalı (VM karar verir: ekip yok, kaynak yetmiyor…); nedeni notta
                raid.SetDisabled(string.IsNullOrEmpty(d.Field.Raid?.Key) ? (t == null ? Corrigible.Core.Loc.N("Saha ekibi yok") : T.T(d.Field.Raid?.Label)) : null);
                BindSend(d.Field);
                DeskUi.SetText(fieldNote, T.T(d.Field.Note));
            }
            Restack(d);
        }

        /// <summary>Ekip fişleri: gönderilebilecek ekip varsa baskın düğmesinin yerini alır (seçim oyuncunun; B en yakını).</summary>
        void BindSend(FieldVM f)
        {
            bool on = sendRow != null && f.Send != null && f.Send.Count > 0;
            DeskUi.SetActive(raid, !on);
            if (sendRow == null) return;
            DeskUi.SetActive(sendRow, on);
            if (!on) return;
            DeskUi.SetText(sendLabel, DeskLoc.Instance.T(f.SendLabel));
            for (int i = 0; i < sendChips.Length; i++)
            {
                bool chip = i < f.Send.Count;
                DeskUi.SetActive(sendChips[i], chip);
                if (!chip) continue;
                var c = f.Send[i];
                sendChips[i].SetKind(string.IsNullOrEmpty(c.Kind) ? "paper-small" : c.Kind);
                sendChips[i].SetLabel(c.Label);
                sendChips[i].SetShortcut(c.Key);
                sendChips[i].SetDisabled(string.IsNullOrEmpty(c.Command) ? c.Label : null);
            }
            sendRow.Layout();
        }

        // ------------------------------------------------------------------ dikey yığın
        /// <summary>
        /// Bölümleri CSS'teki gibi üst üste dizer: kapak, sızma sayfası (kaynak satırı sayısına göre uzar), istatistikler,
        /// eylemler (tehlikeli kart satırı 68), önizleme, saha. Bölüm içindeki öğelerin birbirine göre konumu korunur.
        /// Etkin yükseklikler TSV'den (C1 ve C2 aynı formülü verir).
        /// </summary>
        void Restack(DossierVM d)
        {
            if (coverPanel == null || infPanel == null) return;
            float y = stackTop;
            SetTop(coverPanel, y);
            y += coverHeight + gap;

            // Sızma sayfası: başlıktaki işaret (şüpheli 18, damga 19) altını kaydırır; her kaynak satırı 25
            float headH = ((RectTransform)(infStamp != null && infStamp.gameObject.activeSelf ? infStamp.transform : infTrust.transform)).rect.height;
            float headShift = Mathf.Max(0, Mathf.Round(headH) - infHeadBase);
            if (!Mathf.Approximately(headShift, appliedHeadShift))
            {
                foreach (var r in infBelowHead) if (r != null) r.anchoredPosition += new Vector2(0, -(headShift - appliedHeadShift));
                appliedHeadShift = headShift;
            }
            int rows = d.Sources?.Rows.Count ?? 0;
            float infH = infBaseHeight + headShift - sourceRowHeight * (sourceRows.Length - Mathf.Min(rows, sourceRows.Length));
            SetTop(infPanel, y);
            infPanel.sizeDelta = new Vector2(infPanel.sizeDelta.x, infH);
            y += infH + gap;

            SetSection(statsRow, y);
            y += statsHeight + gap;
            SetSection(actionsHead, y);
            y += actionsHeadAdvance;

            // Kart ızgarası: iki sütun; satır yüksekliği o satırdaki en uzun kart (tehlikeli kartın 3 px şeridi → 68)
            for (int row = 0; row * 2 < actions.Length; row++)
            {
                float h = cardHeight;
                for (int c = 0; c < 2; c++)
                {
                    int i = row * 2 + c;
                    if (i < actions.Length && i < d.Actions.Count && d.Actions[i].Danger && string.IsNullOrEmpty(d.Actions[i].DisabledReason)) h = dangerCardHeight;
                }
                for (int c = 0; c < 2; c++)
                {
                    int i = row * 2 + c;
                    if (i >= actions.Length) continue;
                    var r = (RectTransform)actions[i].transform;
                    SetTop(r, y);
                    r.sizeDelta = new Vector2(r.sizeDelta.x, h);
                }
                y += h + (row * 2 + 2 < actions.Length ? cardGap : 0);
            }
            y += gap;

            if (previewNote != null) { SetTop((RectTransform)previewNote.transform, y); y += previewAdvance; }
            SetSection(fieldHead, y);
            y += fieldHeadAdvance;
            if (teamCard != null && teamCard.activeSelf) { SetTop((RectTransform)teamCard.transform, y); y += teamAdvance; }
            SetTop((RectTransform)raid.transform, y);
            if (sendRow != null) SetTop((RectTransform)sendRow.transform, y);
            y += raidAdvance;
            SetTop(fieldNote.rectTransform, y);
        }

        /// <summary>Sol-üst çapalı, pivotu üstte öğenin üst kenarını <paramref name="top"/>'a koyar.</summary>
        static void SetTop(RectTransform r, float top)
        {
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, -top - (1 - r.pivot.y) * r.rect.height);
        }

        /// <summary>Bölümü (aynı satırdaki öğeler) birlikte taşır: en üstteki öğe <paramref name="top"/>'a, ötekiler ona göreli kalır.</summary>
        static void SetSection(RectTransform[] items, float top)
        {
            if (items.Length == 0 || items[0] == null) return;
            // Bölümün üstü en üstteki öğedir (ör. sağdaki “1–4” başlıktan 1,5 px yukarıda)
            float first = float.MaxValue;
            foreach (var r in items) if (r != null) first = Mathf.Min(first, Top(r));
            float dy = top - first;
            if (Mathf.Approximately(dy, 0)) return;
            foreach (var r in items) if (r != null) r.anchoredPosition += new Vector2(0, -dy);
        }

        static float Top(RectTransform r) => -(r.anchoredPosition.y + (1 - r.pivot.y) * r.rect.height);

        public void BindPreview(PreviewVM p)
        {
            DeskUi.SetActive(previewNote, p != null);
            if (p == null) return;
            var T = DeskLoc.Instance;
            DeskUi.SetText(previewCap, T.T(p.Cap));
            DeskUi.SetText(previewWhat, T.T(p.What));
            DeskUi.SetText(previewWhen, T.T(p.When));
            previewHeadRow.Layout();
            PreviewRowVM trustRow = null, mono = null;
            foreach (var r in p.Rows) { if (r.Kind == "trustChange" || r.Kind == "text") trustRow = r; else if (r.Kind == "mono") mono = r; }
            DeskUi.SetActive(previewTrustRow, trustRow != null);
            if (trustRow != null)
            {
                // trustChange: “Sızma kaynağı ? şüpheli → ONAYLANDI” · text (C2): “Değişiklik yok: bölge zaten ONAYLANDI”
                bool change = trustRow.Kind == "trustChange";
                if (previewArrow == null) previewArrow = previewTrustRow.transform.Find("Arrow") as RectTransform;
                DeskUi.SetText(previewTrustLabel, T.T(change ? trustRow.Label : trustRow.Text));
                DeskUi.SetActive(previewFrom, change);
                DeskUi.SetActive(previewArrow, change);
                if (change) previewFrom.Set(DeskUi.ParseTrust(trustRow.From));
                string stampText = change ? "ONAYLANDI" : trustRow.Stamp;
                DeskUi.SetActive(previewTo, !string.IsNullOrEmpty(stampText));
                if (!string.IsNullOrEmpty(stampText)) previewTo.Set(T.T(stampText), StampInk.Blue, StampSize.Small, 0, "preview");
                previewTrustRow.Layout();
            }
            DeskUi.SetActive(previewMono, mono != null);
            if (mono != null) DeskUi.SetText(previewMono, T.T(mono.Text));
        }
    }
}
