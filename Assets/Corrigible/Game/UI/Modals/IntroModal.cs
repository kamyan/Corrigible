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

        public event Action<string> TypeChosen;
        public event Action StartClicked;
        public event Action TutorialClicked;
        public event Action ContinueClicked;
        /// <summary>Deneysel ayar değişti: (saha ekipleri, zaman ölçeği).</summary>
        public event Action<bool, double> ExpChanged;

        static readonly double[] Scales = { 1, 1.5, 2 };
        bool teamsOn; double scale = 1.5;

        void Awake()
        {
            foreach (var c in cards) { string id = c.typeId; UiUtil.OnClick(c.button.Button, () => TypeChosen?.Invoke(id)); }
            UiUtil.OnClick(start.Button, () => StartClicked?.Invoke());
            UiUtil.OnClick(tutorial.Button, () => TutorialClicked?.Invoke());
            UiUtil.OnClick(continueSaved.Button, () => ContinueClicked?.Invoke());
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
            expTeams.SetText(teams ? "Saha ekipleri ve baskın: AÇIK" : "Saha ekipleri ve baskın: KAPALI");
            expTeams.SetColors(text: teams ? theme.verify : theme.muted);
            expScale.SetText(timeScale == 1 ? "Zaman ölçeği: 1× (temel)" : $"Zaman ölçeği: {timeScale.ToString("0.#", UiUtil.Tr)}×");
        }

        public void Show(string currentType, IEnumerable<string> lines, string saveSummary, bool tutorialDone, UiTheme theme)
        {
            tutorial.SetText(tutorialDone ? "EĞİTİMLE BAŞLA" : "EĞİTİMLE BAŞLA · ÖNERİLİR");
            foreach (var c in cards)
            {
                bool on = c.typeId == currentType;
                c.button.SetColors(on ? theme.selectedCardBg : theme.raise, on ? theme.threat : theme.line2);
            }
            UiUtil.FillLines(interceptLines, linePrefab, lines);
            UiUtil.Show(continueSaved, saveSummary != null);
            if (saveSummary != null) continueSaved.SetText("KALDIĞIN YERDEN DEVAM ET · " + saveSummary);
        }
    }
}
