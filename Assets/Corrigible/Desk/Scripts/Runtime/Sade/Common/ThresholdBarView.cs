using System;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Eşik çubuğu (tokens_sade.desk.bar): 7 px; dizin kâğıdı zemin + 1 px ink2 çerçeve. Parçalar çerçevenin içinde (iç
    /// yükseklik 5): tehlike bölgesi (tx_hazard döşemesi), dolgu, belirsizlik (tx_carbon_hatch), 3×13 işaretçi (iç üstten −4).
    /// Değerler yüzde (0–100); konumlar veriden hesaplanır, hesap kuralı yok. Zemin ve dolgu <see cref="DeskSolidGraphic"/>:
    /// gerilmiş Image dolgu sprite'ının kenarını atlas boşluğuyla karıştırıp dolgunun başında ~2 px soluk şerit bırakır.
    /// </summary>
    public sealed class ThresholdBarView : MonoBehaviour
    {
        [SerializeField] Graphic background;
        [SerializeField] Image frame;
        [SerializeField] RawImage hazard;
        [SerializeField] Graphic fill;
        [SerializeField] RawImage uncertainty;
        [SerializeField] Image marker;

        [SerializeField] float width = 132;
        [SerializeField] float height = 7;
        [Tooltip("Tehlike bölgesi [başlangıç, bitiş] %; boşsa yok.")]
        [SerializeField] float[] hazardRange = Array.Empty<float>();
        [Tooltip("Dolgu %; < 0 ise yok.")]
        [SerializeField] float fillPercent = -1;
        [SerializeField] string fillKey = "sade.desk.bar.fill.singularity";
        [SerializeField] float[] uncertaintyRange = Array.Empty<float>();
        [Tooltip("İşaretçi %; < 0 ise yok.")]
        [SerializeField] float markerPercent = -1;

        const float HazardTile = 4.808f, HatchTile = 4.525f;

        public float Width => width;

        void OnEnable() => Apply();
        void OnValidate() => DeskUi.Deferred(this, Apply);

        public void Set(SadeBarVM vm)
        {
            if (vm == null) return;
            if (vm.Width > 0) width = vm.Width;
            hazardRange = vm.Hazard ?? Array.Empty<float>();
            fillPercent = vm.Fill;
            if (!string.IsNullOrEmpty(vm.FillKey)) fillKey = vm.FillKey;
            uncertaintyRange = vm.Uncertainty ?? Array.Empty<float>();
            markerPercent = vm.Marker;
            Apply();
        }

        /// <summary>Yalnız dolgu (HEDEF satırı): genişlik, yüzde ve renk.</summary>
        public void SetFill(float w, float percent, string colorKey)
        {
            width = w; fillPercent = percent; fillKey = colorKey;
            hazardRange = Array.Empty<float>(); uncertaintyRange = Array.Empty<float>(); markerPercent = -1;
            Apply();
        }

        public void Apply()
        {
            var rt = (RectTransform)transform;
            if (!Mathf.Approximately(rt.sizeDelta.x, width) || !Mathf.Approximately(rt.sizeDelta.y, height)) rt.sizeDelta = new Vector2(width, height);
            float inner = width - 2, innerH = height - 2;
            float X(float pct) => 1 + inner * Mathf.Clamp(pct, 0, 100) / 100f;

            DeskUi.Tint(background, "sade.index");
            SadeUi.Place(background, 0, 0, width, height);
            SadeUi.Sprite(frame, "rect_frame_1", "sade.ink2");
            SadeUi.Place(frame, 0, 0, width, height);

            bool hz = hazardRange != null && hazardRange.Length >= 2 && hazardRange[1] > hazardRange[0];
            DeskUi.SetActive(hazard, hz);
            if (hz) Tile(hazard, "tx_hazard", X(hazardRange[0]), 1, X(hazardRange[1]) - X(hazardRange[0]), innerH, HazardTile);

            bool f = fillPercent >= 0;
            DeskUi.SetActive(fill, f);
            if (f)
            {
                DeskUi.Tint(fill, fillKey);
                SadeUi.Place(fill, 1, 1, X(fillPercent) - 1, innerH);
            }

            bool u = uncertaintyRange != null && uncertaintyRange.Length >= 2 && uncertaintyRange[1] > uncertaintyRange[0];
            DeskUi.SetActive(uncertainty, u);
            if (u) Tile(uncertainty, "tx_carbon_hatch", X(uncertaintyRange[0]), 1, X(uncertaintyRange[1]) - X(uncertaintyRange[0]), innerH, HatchTile);

            bool m = markerPercent >= 0;
            DeskUi.SetActive(marker, m);
            if (m)
            {
                SadeUi.Sprite(marker, "sh_fill_r1", "sade.ink");
                SadeUi.Place(marker, X(markerPercent) - 1.5f, -3, 3, 13);
            }
        }

        static void Tile(RawImage img, string texture, float x, float y, float w, float h, float period)
        {
            var tex = DeskSprites.Instance != null ? DeskSprites.Instance.Texture(texture) : null;
            if (img.texture != tex) img.texture = tex;
            if (img.color != Color.white) img.color = Color.white;
            SadeUi.Place(img, x, y, w, h);
            // Döşeme: bir periyot = period tasarım px; desen sol-üstten başlar
            var uv = new Rect(0, 0, w / period, h / period);
            if (img.uvRect != uv) img.uvRect = uv;
        }
    }
}
