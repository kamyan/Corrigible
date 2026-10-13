using Corrigible.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Giriş brifingi: rakip seçimi, YZ'nin oyuncu hakkındaki ele geçirilmiş kayıtları, başla / devam et.</summary>
    public sealed class IntroModal : MonoBehaviour
    {
        [Serializable]
        public struct TypeCard
        {
            [Tooltip("GameData.AiTypes içindeki kimlik (solucan, manipulator, kahin).")]
            public string typeId;
            public StyledButton button;
        }

        [SerializeField] TypeCard[] cards = Array.Empty<TypeCard>();
        [SerializeField] Transform interceptLines;
        [SerializeField] TMP_Text linePrefab;
        [SerializeField] StyledButton start;
        [Tooltip("Rehberli eğitimi başlatır.")]
        [SerializeField] StyledButton tutorial;
        [SerializeField] StyledButton continueSaved;
        [Header("Deneysel")]
        [SerializeField] StyledButton expTeams;
        [SerializeField] StyledButton expScale;
        [Header("Öğrenme, olaylar, kampanya")]
        [Tooltip("Öğrenme aşamasını ya da kampanya durumunu anlatan satır.")]
        [SerializeField] TMP_Text stageLine;
        [SerializeField] StyledButton onboarding;
        [SerializeField] StyledButton events;
        [SerializeField] StyledButton campaign;
        [Tooltip("Dili değiştirir (sahne yeniden yüklenir).")]
        [SerializeField] StyledButton language;

        public event Action<string> TypeChosen;
        public event Action StartClicked;
        public event Action TutorialClicked;
        public event Action ContinueClicked;
        /// <summary>Deneysel ayar değişti: (saha ekipleri, zaman ölçeği).</summary>
        public event Action<bool, double> ExpChanged;
        public event Action LanguageClicked;
        public event Action OnboardingToggled, EventsToggled, CampaignClicked;

        static readonly double[] Scales = { 1, 1.5, 2 };
        bool teamsOn; double scale = 1.5;

        void Awake()
        {
            foreach (var c in cards) { string id = c.typeId; UiUtil.OnClick(c.button.Button, () => TypeChosen?.Invoke(id)); }
            UiUtil.OnClick(start.Button, () => StartClicked?.Invoke());
            UiUtil.OnClick(tutorial.Button, () => TutorialClicked?.Invoke());
            UiUtil.OnClick(continueSaved.Button, () => ContinueClicked?.Invoke());
            if (onboarding != null) UiUtil.OnClick(onboarding.Button, () => OnboardingToggled?.Invoke());
            if (events != null) UiUtil.OnClick(events.Button, () => EventsToggled?.Invoke());
            if (campaign != null) UiUtil.OnClick(campaign.Button, () => CampaignClicked?.Invoke());
            if (language != null)
            {
                UiUtil.OnClick(language.Button, () => LanguageClicked?.Invoke());
                language.SetText(Loc.T("Dil: Türkçe · Language: English"));
            }
            if (expTeams != null) UiUtil.OnClick(expTeams.Button, () => ExpChanged?.Invoke(!teamsOn, scale));
            if (expScale != null) UiUtil.OnClick(expScale.Button, () =>
            {
                int i = Array.IndexOf(Scales, scale);
                ExpChanged?.Invoke(teamsOn, Scales[(i + 1) % Scales.Length]);
            });
        }

        /// <param name="tutorialDone">Eğitim hiç bitirilmediyse "Eğitimle başla" önerilir.</param>
        public void ShowExperimental(bool teams, double timeScale, UiTheme theme)
        {
            teamsOn = teams; scale = timeScale;
            if (expTeams == null) return;
            expTeams.SetText(teams ? Loc.T("Saha ekipleri ve baskın: AÇIK") : Loc.T("Saha ekipleri ve baskın: KAPALI"));
            expTeams.SetColors(text: teams ? theme.verify : theme.muted);
            expScale.SetText(timeScale == 1 ? Loc.T("Zaman ölçeği: 1× (temel)") : Loc.F("Zaman ölçeği: {0}×", timeScale.ToString("0.#", UiUtil.Tr)));
        }

        static readonly string[] StageText =
        {
            Loc.N("Öğrenme aşaması 1/4: YZ hediye vermiyor, haber uyduramıyor, ekranlar yalan söylemiyor; olay kartı yok."),
            Loc.N("Öğrenme aşaması 2/4: YZ artık hediye teklif ediyor; bağımlılık ve veto devrede."),
            Loc.N("Öğrenme aşaması 3/4: YZ medyayı ele geçirebilir; bazı haberler ve raporlar yalan."),
            Loc.N("Bütün kurallar açık: YZ seni ekranlarınla da kandırabilir (Sessiz Zafer)."),
        };

        /// <summary>Öğrenme aşaması, olay kartları ve kampanya durumu.</summary>
        public void ShowRules(GameController game, UiTheme theme)
        {
            if (stageLine == null) return;
            var c = game.Campaign;
            bool inCampaign = c != null;
            if (inCampaign)
                stageLine.text = Loc.F("KAMPANYA · Kriz {0}/{1} · kazanılan {2} · yükseltme {3}. Bütün kurallar ve olay kartları açık.", c.crisis + 1, Campaign.CampaignState.Crises, c.Wins, c.upgrades.Count);
            else
                stageLine.text = Loc.T(StageText[game.OnboardingStage - 1]) + (game.OnboardingStage < 4 ? Loc.T(" YZ seni tanıdıkça yeni hileler öğrenecek.") : "");
            stageLine.color = inCampaign ? theme.verify : theme.muted;
            UiUtil.Show(onboarding, !inCampaign);
            UiUtil.Show(events, !inCampaign);
            onboarding.SetText(game.OnboardingOff ? Loc.T("Aşamalı öğrenme: KAPALI") : Loc.T("Aşamalı öğrenme: AÇIK"));
            events.SetText(game.EventsEnabled ? Loc.T("Olay kartları: AÇIK") : Loc.T("Olay kartları: KAPALI"));
            events.SetColors(text: game.EventsEnabled ? theme.verify : theme.muted);
            campaign.SetText(inCampaign ? Loc.T("Kampanyadan çık") : Loc.T("Kampanya başlat (5 kriz)"));
        }

        public void Show(string currentType, IEnumerable<string> lines, string saveSummary, bool tutorialDone, UiTheme theme)
        {
            tutorial.SetText(tutorialDone ? Loc.T("EĞİTİMLE BAŞLA") : Loc.T("EĞİTİMLE BAŞLA · ÖNERİLİR"));
            foreach (var c in cards)
            {
                bool on = c.typeId == currentType;
                c.button.SetColors(on ? theme.selectedCardBg : theme.raise, on ? theme.threat : theme.line2);
            }
            UiUtil.FillLines(interceptLines, linePrefab, lines);
            UiUtil.Show(continueSaved, saveSummary != null);
            if (saveSummary != null) continueSaved.SetText(Loc.T("KALDIĞIN YERDEN DEVAM ET · ") + saveSummary);
        }
    }
}
