using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>Rakip kartı (giriş dosyası): dizin kartı, tuş fişi, ad, açıklama, başlangıç yeteneği, zorluk damgası; seçiliyse kalkar.</summary>
    public sealed class RivalCardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] RectTransform visual;
        [SerializeField] PaperPanel paper;
        [SerializeField] UnityEngine.UI.Image selectedFrame;
        [SerializeField] KeyChipView key;
        [SerializeField] TMP_Text title, desc, start;
        [SerializeField] StampView difficulty, selected;

        static readonly string SelectedText = Corrigible.Core.Loc.N("SEÇİLDİ");

        public string Command { get; private set; }
        public event Action<RivalCardView> Clicked;

        public void Bind(RivalOptionVM vm)
        {
            Command = vm.Command;
            var T = DeskLoc.Instance;
            key.Set(vm.Key);
            DeskUi.SetText(title, vm.Name);
            DeskUi.SetText(desc, T.T(vm.Desc));
            DeskUi.SetText(start, T.T(vm.Start));
            difficulty.Set(T.T(vm.Difficulty), StampView.ParseInk(vm.DifficultyColor), StampSize.Small, vm.Selected ? 2 : -3, "rival.d." + vm.Key);
            DeskUi.SetActive(selected, vm.Selected);
            if (vm.Selected) selected.Set(T.T(SelectedText), StampInk.Red, StampSize.Normal, -8, "rival.sel", 16);
            DeskUi.SetActive(selectedFrame, vm.Selected);
            // Seçili kart 3 px kalkar ve gölgesi büyür (taslak .rival.on)
            visual.anchoredPosition = new Vector2(0, vm.Selected ? 3 : 0);
            paper.SetShadow(vm.Selected ? new Vector2(4, 6) : new Vector2(2, 2), new Color(0.235f, 0.176f, 0.078f, vm.Selected ? 0.45f : 0.35f));
        }

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(this);
    }
}
