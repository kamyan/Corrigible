using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sade kâğıdı: dolgu + kenar (9-dilim) + sert gölge katmanları. Renkler tema anahtarı, gölge tokens_sade.shadows anahtarı.
    /// Etiket kâğıdı, plaka, kart ve düğme zeminleri bununla kurulur.
    /// </summary>
    public sealed class SadePaper : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        [SerializeField] string fillSprite = "sh_fill_r2";
        [SerializeField] string frameSprite = "sh_frame_r2_1";
        [SerializeField] string fillKey = "sade.paper";
        [SerializeField] string frameKey = "sade.edge";
        [Tooltip("tokens_sade.shadows anahtarı (chip, card, plaque…); boşsa gölge yok.")]
        [SerializeField] string shadowKey = "chip";

        public Image Fill => fill;

        void OnEnable() => Apply();
        void OnValidate() => DeskUi.Deferred(this, Apply);

        /// <summary>Renk ve gölgeyi değiştirir (ör. üstüne gelme: sade.panels.hoverPaper).</summary>
        public void Set(string fillColorKey, string frameColorKey, string shadow)
        {
            fillKey = fillColorKey; frameKey = frameColorKey; shadowKey = shadow;
            Apply();
        }

        public void SetSprites(string fillName, string frameName)
        {
            fillSprite = fillName; frameSprite = frameName;
            Apply();
        }

        public void Apply()
        {
            if (fill != null)
            {
                SadeUi.Sprite(fill, fillSprite, fillKey);
                SadeUi.Shadows(fill, shadowKey);
            }
            if (frame != null)
            {
                bool on = !string.IsNullOrEmpty(frameSprite) && !string.IsNullOrEmpty(frameKey);
                DeskUi.SetActive(frame, on);
                if (on) SadeUi.Sprite(frame, frameSprite, frameKey);
            }
        }
    }
}
