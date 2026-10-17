using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Etiket satırındaki küçük güven işareti (tokens_sade.desk.chip.trustMark): ONAYLANDI 14 px mühür, RAPOR 15 px yüksek
    /// “~” fişi (kesik kenar, karbon), ŞÜPHELİ Kalam 700 18 turuncu “?” (2 px yukarıda). BİLİNMİYOR'da işaret yok.
    /// Kendi genişliğini ayarlar; satırı kuran bileşen yan yana dizer.
    /// </summary>
    public sealed class ChipTrustMarkView : MonoBehaviour
    {
        [SerializeField] TrustLevel level = TrustLevel.Verified;
        [SerializeField] Image seal;
        [SerializeField] Image slipFill;
        [SerializeField] Image slipFrame;
        [SerializeField] TMP_Text slipText;
        [SerializeField] TMP_Text question;
        [Tooltip("Satır yüksekliği (etiket satırı 14 px); işaretler buna göre dikeyde yerleşir.")]
        [SerializeField] float rowHeight = 14;

        public TrustLevel Level => level;
        /// <summary>İşaretin genişliği (yoksa 0).</summary>
        public float Width { get; private set; }

        void OnEnable() => Apply();
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(TrustLevel t)
        {
            level = t;
            Apply();
        }

        public void Apply()
        {
            DeskUi.SetActive(seal, level == TrustLevel.Verified);
            bool slip = level == TrustLevel.Report;
            DeskUi.SetActive(slipFill, slip); DeskUi.SetActive(slipFrame, slip); DeskUi.SetActive(slipText, slip);
            DeskUi.SetActive(question, level == TrustLevel.Suspicious);
            var rt = (RectTransform)transform;
            float w = 0;
            switch (level)
            {
                case TrustLevel.Verified:
                    SadeUi.Sprite(seal, "mark_seal_14");
                    DeskUi.SetColor(seal, Color.white);
                    SadeUi.Place(seal, 0, (rowHeight - 14) / 2, 14, 14);
                    w = 14;
                    break;
                case TrustLevel.Report:
                {
                    SadeUi.Type(slipText, "chipSlip");
                    DeskUi.Tint(slipText, "sade.carbon");
                    DeskUi.SetText(slipText, "~");
                    float tw = SadeUi.Width(slipText, "~");
                    w = Mathf.Round((tw + 8 + 2) * 10) / 10;   // iç boşluk 4 + kenar 1, iki yanda
                    float y = (rowHeight - 15) / 2;
                    SadeUi.Sprite(slipFill, "sh_fill_r2", "sade.onion");
                    SadeUi.Sprite(slipFrame, "sh_dash_r2_1", "sade.carbon");
                    SadeUi.Place(slipFill, 0, y, w, 15);
                    SadeUi.Place(slipFrame, 0, y, w, 15);
                    SadeUi.Place(slipText, 0, y, w, 15);
                    slipText.alignment = TextAlignmentOptions.Center;
                    break;
                }
                case TrustLevel.Suspicious:
                {
                    SadeUi.Type(question, "chipQuestion");
                    DeskUi.Tint(question, "sade.status.warning");
                    DeskUi.SetText(question, "?");
                    w = Mathf.Ceil(SadeUi.Width(question, "?"));
                    SadeUi.Place(question, 0, (rowHeight - 18) / 2 - 2, w, 18);
                    break;
                }
            }
            Width = w;
            if (!Mathf.Approximately(rt.sizeDelta.x, w) || !Mathf.Approximately(rt.sizeDelta.y, rowHeight)) rt.sizeDelta = new Vector2(w, rowHeight);
        }
    }
}
