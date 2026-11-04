using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Emir belgesi (K7, Overlay 760 px): KYGA başlığı, EMİR NO. 001, KİME/KONU, GİZLİ damgası, gövde (Courier Prime,
    /// daktilo), imza, BAŞLA (Enter); arkada manila dosya ve sağ kenarında KAYITTAN DEVAM sekmesi (kayıt yoksa soluk +
    /// “KAYIT YOK”). Açılışta teleks yönünden kayıp haritaya düşer (motion.briefDropS), okuma görünümüne kalkar
    /// (motion.briefLiftS); BAŞLA'da ALINDI damgası iner, belge küçülerek EVRAK tepsisine kayar (motion.reportFileS).
    /// Okuma kipinde (H, tepsi) dosya ve sekme yoktur, düğme “Kapat”. Tuşlar bağlayıcıdadır (SiginakBriefing).
    /// </summary>
    public sealed class BriefingView : MonoBehaviour
    {
        [Header("Belge")]
        [SerializeField] RectTransform document;
        [SerializeField] CanvasGroup documentGroup;
        [SerializeField] TMP_Text agency, number, title, toLabel, to, reLabel, re, stamp;
        [SerializeField] TMP_Text[] paragraphs = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text closing, signOffice, signLine;
        [SerializeField] RectTransform received;
        [SerializeField] TMP_Text receivedText;
        [SerializeField] Button action;
        [SerializeField] TMP_Text actionLabel;
        [SerializeField] GameObject actionFocus;
        [Header("Dosya ve sekme")]
        [SerializeField] CanvasGroup dossier;
        [SerializeField] RectTransform tab;
        [SerializeField] Button tabButton;
        [SerializeField] PointerHover tabHover;
        [SerializeField] Image tabFill;
        [SerializeField] GameObject tabLabel, tabHintRow, tabFocus, noSave;
        [SerializeField] TMP_Text tabTitle, tabSummary, tabHint, noSaveText;

        enum Phase { Idle, Drop, Lift, Stamp, File }
        Phase phase;
        float phaseAt, slide;
        Vector2 restPosition, tabRest;
        float restRotation;
        Vector3 fileFrom, fileTo;
        bool rested, hasSave;
        int focus;
        Action filed;

        public bool IsOpen => gameObject.activeSelf && phase != Phase.File;
        public bool Animating => phase != Phase.Idle;
        /// <summary>Klavye odağı: 0 BAŞLA / Kapat, 1 KAYITTAN DEVAM sekmesi.</summary>
        public int Focused => focus;
        public event Action ActionClicked, TabClicked;

        public void Configure(RectTransform documentRoot, CanvasGroup documentCanvasGroup, TMP_Text[] head, TMP_Text[] body, TMP_Text closingLine,
            TMP_Text office, TMP_Text line, RectTransform receivedStamp, TMP_Text receivedLabel, Button actionButton, TMP_Text actionText, GameObject actionRing,
            CanvasGroup dossierGroup, RectTransform tabRoot, Button tabClick, PointerHover hover, Image tabPaper, GameObject label, GameObject hintRow,
            GameObject tabRing, GameObject noSaveStamp, TMP_Text[] tabTexts)
        {
            document = documentRoot; documentGroup = documentCanvasGroup;
            agency = head[0]; number = head[1]; title = head[2]; toLabel = head[3]; to = head[4]; reLabel = head[5]; re = head[6]; stamp = head[7];
            paragraphs = body; closing = closingLine; signOffice = office; signLine = line; received = receivedStamp; receivedText = receivedLabel;
            action = actionButton; actionLabel = actionText; actionFocus = actionRing;
            dossier = dossierGroup; tab = tabRoot; tabButton = tabClick; tabHover = hover; tabFill = tabPaper; tabLabel = label; tabHintRow = hintRow;
            tabFocus = tabRing; noSave = noSaveStamp;
            tabTitle = tabTexts[0]; tabSummary = tabTexts[1]; tabHint = tabTexts[2]; noSaveText = tabTexts[3];
        }

        void Awake()
        {
            Rest();
            action.onClick.AddListener(() => { if (phase == Phase.Idle) ActionClicked?.Invoke(); });
            tabButton.onClick.AddListener(() => { if (phase == Phase.Idle && hasSave) TabClicked?.Invoke(); });
        }

        void Rest()
        {
            if (rested) return;
            rested = true;
            restPosition = document.anchoredPosition;
            float z = document.localEulerAngles.z;
            restRotation = z > 180 ? z - 360 : z;
            tabRest = tab.anchoredPosition;
        }

        /// <summary>Belgeyi açar; <paramref name="animate"/> ise teleks yönünden düşer ve okuma görünümüne kalkar.</summary>
        /// <summary>Ayarlar dolabı açıkken belge görünmez ve tıklanmaz (ölçek 0); durumu ve canlandırması değişmez.</summary>
        public void SetVeiled(bool on) => transform.localScale = on ? Vector3.zero : Vector3.one;

        public void Show(BriefingData data, bool animate)
        {
            Rest();
            gameObject.SetActive(true);
            filed = null;
            Bind(data);
            received.gameObject.SetActive(false);
            SetFocus(0, false);
            if (animate && Application.isPlaying)
            {
                phase = Phase.Drop;
                phaseAt = Time.unscaledTime;
                Pose(0, 0);
            }
            else
            {
                phase = Phase.Idle;
                Pose(1, 1);
            }
        }

        public void Bind(BriefingData d)
        {
            var t = SiginakTheme.Active;
            agency.text = d.Agency; number.text = d.Number; title.text = d.Title;
            toLabel.text = d.ToLabel; to.text = d.To; reLabel.text = d.ReLabel; re.text = d.Re; stamp.text = d.Stamp;
            for (int i = 0; i < paragraphs.Length; i++)
            {
                bool on = i < d.Paragraphs.Length;
                paragraphs[i].gameObject.SetActive(on);
                if (on) paragraphs[i].text = d.Paragraphs[i];
            }
            closing.text = d.Closing;
            signOffice.text = d.SignOffice;
            signLine.text = d.SignLine;
            receivedText.text = d.Received;
            actionLabel.text = d.Action;

            dossier.gameObject.SetActive(!d.Reading && !d.NoDossier);
            hasSave = d.HasSave;
            tabFill.color = t.ColorOf(d.HasSave ? "ui.folder[0]" : "ui.folderOff");
            tabLabel.SetActive(d.HasSave);
            tabHintRow.SetActive(d.HasSave);
            noSave.SetActive(!d.HasSave);
            tabTitle.text = d.TabTitle; tabSummary.text = d.TabSummary; tabHint.text = d.TabHint; noSaveText.text = d.NoSave;
            tabButton.interactable = d.HasSave;
            LayoutRebuilder.ForceRebuildLayoutImmediate(document);
        }

        /// <summary>Klavye odağı (Tab): sarı çerçeve; sekme odakta ya da üzerindeyken dışarı kayar.</summary>
        public void SetFocus(int index, bool ring)
        {
            focus = index == 1 && hasSave ? 1 : 0;
            actionFocus.SetActive(ring && focus == 0);
            tabFocus.SetActive(ring && focus == 1);
        }

        /// <summary>ALINDI damgası iner (motion.stampS), sonra <paramref name="done"/>.</summary>
        public void StampReceived(Action done)
        {
            received.gameObject.SetActive(true);
            actionFocus.SetActive(false);
            tabFocus.SetActive(false);
            filed = done;
            phase = Phase.Stamp;
            phaseAt = Time.unscaledTime;
            if (!Application.isPlaying) { phase = Phase.Idle; received.localScale = Vector3.one; Done(); }
        }

        /// <summary>EVRAK tepsisine kayar (dünya noktası ekrana izdüşer), sonra kapanır.</summary>
        public void File(Vector3 world, Camera view, Action done = null)
        {
            if (!gameObject.activeSelf) { done?.Invoke(); return; }
            filed = done;
            if (!Application.isPlaying || view == null) { Close(); return; }
            Pose(1, 1);
            fileFrom = document.position;
            var screen = view.WorldToScreenPoint(world);
            fileTo = screen.z > 0 ? new Vector3(screen.x, screen.y, 0) : fileFrom + Vector3.right * 1200;
            phase = Phase.File;
            phaseAt = Time.unscaledTime;
        }

        public void Hide()
        {
            filed = null;
            phase = Phase.Idle;
            gameObject.SetActive(false);
        }

        void Close()
        {
            phase = Phase.Idle;
            Pose(1, 1);
            gameObject.SetActive(false);
            Done();
        }

        void Done()
        {
            var d = filed;
            filed = null;
            d?.Invoke();
        }

        void Update()
        {
            var t = SiginakTheme.Active;
            float now = Time.unscaledTime;
            switch (phase)
            {
                case Phase.Drop:
                {
                    float p = Mathf.Clamp01((now - phaseAt) / Mathf.Max(0.01f, t.Number("motion.briefDropS", 0.35f)));
                    Pose(p, 0);
                    if (p >= 1) { phase = Phase.Lift; phaseAt = now; }
                    break;
                }
                case Phase.Lift:
                {
                    float p = Mathf.Clamp01((now - phaseAt) / Mathf.Max(0.01f, t.Number("motion.briefLiftS", 0.25f)));
                    Pose(1, p);
                    if (p >= 1) phase = Phase.Idle;
                    break;
                }
                case Phase.Stamp:
                {
                    float p = Mathf.Clamp01((now - phaseAt) / Mathf.Max(0.01f, t.Number("motion.stampS", 0.1f)));
                    received.localScale = Vector3.one * Mathf.Lerp(1.6f, 1, p * p);
                    if (p >= 1) { phase = Phase.Idle; Done(); }
                    break;
                }
                case Phase.File:
                {
                    float p = Mathf.Clamp01((now - phaseAt) / Mathf.Max(0.01f, t.Number("motion.reportFileS", 0.4f)));
                    float e = p * p;
                    document.position = Vector3.Lerp(fileFrom, fileTo, e);
                    document.localScale = Vector3.one * Mathf.Lerp(1, 0.16f, e);
                    document.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(restRotation, restRotation - 8, e));
                    documentGroup.alpha = 1 - Mathf.Clamp01((p - 0.6f) / 0.4f);
                    dossier.alpha = 1 - Mathf.Clamp01(p / 0.4f);
                    if (p >= 1) Close();
                    break;
                }
            }
            // Sekme: üzerinde ya da odakta 6 px dışarı
            float goal = phase == Phase.Idle && hasSave && (tabHover != null && tabHover.Over || focus == 1) ? 1 : 0;
            slide = Mathf.MoveTowards(slide, goal, Time.unscaledDeltaTime / 0.12f);
            tab.anchoredPosition = tabRest + new Vector2(6 * slide, 0);
        }

        /// <summary>
        /// <paramref name="drop"/>: haritaya düşüş (soldan, teleks yönü; masada yatan A4 gibi küçük), <paramref name="lift"/>:
        /// okuma görünümüne kalkış (büyür, dosya belirir). 1, 1 = dinlenme.
        /// </summary>
        void Pose(float drop, float lift)
        {
            float d = 1 - (1 - drop) * (1 - drop), l = lift * lift * (3 - 2 * lift);
            const float lying = 0.55f;
            document.anchoredPosition = restPosition + new Vector2(-560, 80) * (1 - d) + new Vector2(0, -40) * (1 - l);
            document.localRotation = Quaternion.Euler(0, 0, restRotation + 7 * (1 - d) + 2 * (1 - l));
            document.localScale = Vector3.one * Mathf.Lerp(lying, 1, l);
            documentGroup.alpha = Mathf.Clamp01(drop * 4);
            dossier.alpha = l;
        }
    }
}
