using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Giriş: görev dosyası (F8.3, taslak <c>drafts/giris</c>). Manila klasörde iki sayfa: rakip seçimi + YZ'nin oyuncu
    /// profili (ele geçirilmiş kayıt); kurallar, kampanya, devam/eğitim/başla, ayarlar ve dil. Bütün düğmeler VM'deki komut
    /// dizisini <see cref="Invoked"/> ile yayınlar; 1/2/3 rakip seçer, Enter göreve başlatır (<see cref="DeskInput"/>).
    /// </summary>
    public sealed class IntroView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text folderTitle, folderSub, paperTitle, paperRight, rivalsCap, rivalsRight, rulesTitle, rulesRight;
        [SerializeField] RectTransform rivalsRule;
        [SerializeField] float rivalsWidth = 710;
        [Tooltip("Klasör sekmesi: başlık ve alt yazı yan yana (sekme genişliği metne göre).")]
        [SerializeField] DeskRow folderRow;
        [SerializeField] RivalCardView[] rivals = Array.Empty<RivalCardView>();
        [SerializeField] TMP_Text interceptHeader, interceptRight, interceptFooter;
        [SerializeField] TMP_Text[] interceptLines = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text stageNote;
        [SerializeField] RuleRowView[] rules = Array.Empty<RuleRowView>();
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] RadioCheckView[] timeOptions = Array.Empty<RadioCheckView>();
        [SerializeField] TMP_Text campaignCap, campaignRight, campaignText;
        [SerializeField] DeskButton campaignButton, continueButton, tutorialButton, startButton;
        [Tooltip("“Kaldığın yerden devam et” solda, kayıt özeti sağda (CSS space-between); düğmenin kendi etiketi boş.")]
        [SerializeField] TMP_Text continueLabel, continueRight;
        [Tooltip("“Eğitimle başla” + ÖNERİLİR damgası düğmenin ortasında birlikte (düğmenin kendi etiketi boş).")]
        [SerializeField] TMP_Text tutorialLabel;
        [SerializeField] DeskRow tutorialRow;
        [SerializeField] StampView tutorialStamp;
        [SerializeField] DeskButton[] links = Array.Empty<DeskButton>();
        [SerializeField] DeskRow linksRow;

        static readonly string RecommendedText = Corrigible.Core.Loc.N("ÖNERİLİR");

        IntroVM vm;
        public bool IsOpen => root != null && root.activeSelf;
        public static IntroView Instance { get; private set; }
        public event Action<string> Invoked;

        void Awake()
        {
            Instance = this;
            foreach (var r in rivals) r.Clicked += c => Invoked?.Invoke(c.Command);
            foreach (var r in rules) r.Clicked += c => Invoked?.Invoke(c.Command);
            for (int i = 0; i < timeOptions.Length; i++) { int k = i; timeOptions[i].Clicked += _ => { if (vm?.TimeScale != null && k < vm.TimeScale.Options.Count) Invoked?.Invoke(vm.TimeScale.Options[k].Command); }; }
            campaignButton.onClick.AddListener(() => Invoked?.Invoke(vm?.CampaignButton?.Command));
            continueButton.onClick.AddListener(() => Invoked?.Invoke(vm?.Continue?.Command));
            tutorialButton.onClick.AddListener(() => Invoked?.Invoke(vm?.Tutorial?.Command));
            startButton.onClick.AddListener(Begin);
            for (int i = 0; i < links.Length; i++) { int k = i; links[i].onClick.AddListener(() => { if (vm != null && k < vm.Links.Count) Invoked?.Invoke(vm.Links[k].Command); }); }
            root.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Enter: göreve başla.</summary>
        public void Begin() => Invoked?.Invoke(vm?.Start?.Command);

        /// <summary>1/2/3: rakip.</summary>
        public void Choose(int i) { if (vm != null && i >= 0 && i < vm.Rivals.Count) Invoked?.Invoke(vm.Rivals[i].Command); }

        public void Bind(IntroVM model)
        {
            vm = model;
            root.SetActive(model != null);
            if (model == null) return;
            var T = DeskLoc.Instance;
            DeskUi.SetText(folderTitle, T.T(model.Folder?.Title));
            DeskUi.SetText(folderSub, T.T(model.Folder?.Right));
            if (folderRow != null) folderRow.Layout();
            DeskUi.SetText(paperTitle, T.T(model.Paper?.Title));
            DeskUi.SetText(paperRight, T.T(model.Paper?.Right));
            DeskUi.SetText(rivalsCap, T.T(model.RivalsHeader?.Title));
            DeskUi.SetText(rivalsRight, T.T(model.RivalsHeader?.Right));
            DeskUi.CapRule(rivalsCap, rivalsRule, rivalsRight, rivalsWidth);
            DeskUi.SetText(rulesTitle, T.T(model.RulesHeader?.Title));
            DeskUi.SetText(rulesRight, T.T(model.RulesHeader?.Right));
            for (int i = 0; i < rivals.Length; i++)
            {
                bool on = i < model.Rivals.Count;
                DeskUi.SetActive(rivals[i], on);
                if (on) rivals[i].Bind(model.Rivals[i]);
            }
            var log = model.Intercept;
            DeskUi.SetText(interceptHeader, T.T(log?.Header));
            DeskUi.SetText(interceptRight, log?.Right);
            DeskUi.SetText(interceptFooter, T.T(log?.Footer));
            for (int i = 0; i < interceptLines.Length; i++)
            {
                bool on = log != null && i < log.Lines.Count;
                DeskUi.SetActive(interceptLines[i], on);
                if (on) DeskUi.SetText(interceptLines[i], log.Lines[i]);
            }
            DeskUi.SetText(stageNote, T.T(model.StageNote));
            for (int i = 0; i < rules.Length; i++)
            {
                bool on = i < model.Rules.Count;
                DeskUi.SetActive(rules[i], on);
                if (on) rules[i].Bind(model.Rules[i]);
            }
            DeskUi.SetText(timeLabel, T.T(model.TimeScale?.Label));
            for (int i = 0; i < timeOptions.Length; i++)
            {
                bool on = model.TimeScale != null && i < model.TimeScale.Options.Count;
                DeskUi.SetActive(timeOptions[i], on);
                if (on) timeOptions[i].Set(model.TimeScale.Options[i].Label, null, model.TimeScale.Options[i].On);
            }
            DeskUi.SetText(campaignCap, T.T(model.CampaignBox?.Title));
            DeskUi.SetText(campaignRight, T.T(model.CampaignBox?.Right));
            DeskUi.SetText(campaignText, T.T(model.CampaignText));
            campaignButton.SetLabel(T.T(model.CampaignButton?.Label));
            DeskUi.SetActive(continueButton, model.Continue != null);
            DeskUi.SetActive(continueRight, model.Continue != null);
            if (model.Continue != null)
            {
                if (continueLabel != null) DeskUi.SetText(continueLabel, T.T(model.Continue.Label));
                else continueButton.SetLabel(T.T(model.Continue.Label));
                DeskUi.SetText(continueRight, model.ContinueRight);
            }
            if (tutorialLabel != null) DeskUi.SetText(tutorialLabel, T.T(model.Tutorial?.Label));
            else tutorialButton.SetLabel(T.T(model.Tutorial?.Label));
            DeskUi.SetActive(tutorialStamp, model.TutorialRecommended);
            if (model.TutorialRecommended) tutorialStamp.Set(T.T(RecommendedText), StampInk.Green, StampSize.Small, -3, "intro.tutorial");
            if (tutorialRow != null)
            {
                // Etiket ve damga birlikte düğmenin ortasında
                tutorialRow.Layout();
                var tr = (RectTransform)tutorialRow.transform;
                float bw = ((RectTransform)tutorialButton.transform).sizeDelta.x;
                tr.anchoredPosition = new Vector2(Mathf.Round((bw - tutorialRow.Width) / 2), tr.anchoredPosition.y);
            }
            startButton.SetLabel(T.T(model.Start?.Label));
            for (int i = 0; i < links.Length; i++)
            {
                bool on = i < model.Links.Count;
                DeskUi.SetActive(links[i], on);
                if (on) links[i].SetLabel(T.T(model.Links[i].Label));
            }
            if (linksRow != null) linksRow.Layout();
        }
    }
}
