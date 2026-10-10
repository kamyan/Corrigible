using Corrigible.Game.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Zemin, çerçeve ve yazıdan oluşan düğme. Kod durum değişince (seçili, devre dışı, kurulu) bu parçaları boyar.
    /// Sekmelerde "border" alt çizgidir.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class StyledButton : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] Button button;
        [SerializeField] Graphic background;
        [SerializeField] Graphic border;
        [SerializeField] TMP_Text label;
        [SerializeField] CanvasGroup group;
        [SerializeField, Range(0, 1)] float disabledAlpha = 0.42f;

        [Tooltip("Tıklama ve üzerine gelme sesi.")]
        [SerializeField] bool sounds = true;

        public Button Button => button;

        void Awake() { if (sounds && button != null) button.onClick.AddListener(() => Sfx.Play(Cue.Click)); }

        public void OnPointerEnter(PointerEventData e) { if (sounds && button != null && button.interactable) Sfx.Play(Cue.Hover); }
        public TMP_Text Label => label;
        public Graphic Background => background;
        public Graphic Border => border;

        public void SetColors(Color? bg = null, Color? borderColor = null, Color? text = null)
        {
            if (bg.HasValue && background != null) background.color = bg.Value;
            if (borderColor.HasValue && border != null) border.color = borderColor.Value;
            if (text.HasValue && label != null) label.color = text.Value;
        }

        public void SetEnabled(bool on)
        {
            button.interactable = on;
            if (group != null) group.alpha = on ? 1f : disabledAlpha;
        }

        public void SetText(string text) { if (label != null) label.text = text; }
    }
}
