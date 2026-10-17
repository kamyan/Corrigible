using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bölge adı (CP 700 12). Normal etikette paylaşılan TMP Underlay materyali, seçimde halesiz yazı + sarı çip kullanır.
    /// Materyal ön ayarları sahnede bağlıdır; çalışma anında materyal ya da hiyerarşi yaratılmaz.
    /// </summary>
    public sealed class SadeRegionLabelView : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Image selectedChip;
        [SerializeField] Material haloMaterial, plainMaterial;

        public RegionNodeVM Data { get; private set; }
        public TMP_Text Label => label;

        public void Bind(RegionNodeVM vm)
        {
            Data = vm;
            if (vm == null || label == null) return;
            string text = DeskLoc.Instance.T(string.IsNullOrEmpty(vm.Label) ? vm.Name : vm.Label);
            DeskUi.SetText(label, text);
            SadeUi.Type(label, "regionLabel");
            DeskUi.Tint(label, "sade.map.labelInk");
            var material = vm.Selected ? plainMaterial : haloMaterial;
            if (material != null && label.fontSharedMaterial != material) label.fontSharedMaterial = material;
            float width = Mathf.Ceil(SadeUi.Width(label, text));
            var rt = (RectTransform)transform;
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(width + 10, 18);
            SadeUi.Place(label, 5, -2, width, 14);
            label.alignment = TextAlignmentOptions.Center;
            DeskUi.SetActive(selectedChip, vm.Selected);
            if (vm.Selected) SadeUi.Sprite(selectedChip, "sh_fill_r0", "sade.highlighter");
        }
    }
}
