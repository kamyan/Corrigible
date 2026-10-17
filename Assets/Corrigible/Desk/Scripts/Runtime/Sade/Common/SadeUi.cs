using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Sade bileşenlerinin ortak yardımcıları: yazı tablosu, sprite + tema rengi, sert gölge katmanları, biçimlendirme.</summary>
    public static class SadeUi
    {
        /// <summary>tokens_sade.type rolünü uygular (font, boyut, harf aralığı).</summary>
        public static void Type(TMP_Text t, string role)
        {
            var theme = SadeTheme.Active;
            if (theme != null) theme.ApplyType(t, role);
        }

        /// <summary>Sprite (ve varsa tema rengi). Simgeler beyaz çizildi; rengi tema anahtarı verir.</summary>
        public static void Sprite(Image img, string spriteName, string tintKey = null, float alpha = 1)
        {
            if (img == null) return;
            DeskSprites.Apply(img, spriteName);
            if (tintKey != null) DeskUi.Tint(img, tintKey, alpha);
        }

        /// <summary>
        /// Gölge katmanlarını (tokens_sade.shadows) grafiğin Shadow bileşenlerine yazar; fazla bileşenler kapanır.
        /// Bileşen eksikse yalnız <paramref name="allowAdd"/> ile eklenir (prefab kurucusu); çalışırken eklenmez.
        /// </summary>
        public static void Shadows(Graphic g, string key, bool allowAdd = false)
        {
            if (g == null) return;
            var theme = SadeTheme.Active;
            var layers = theme != null && !string.IsNullOrEmpty(key) ? theme.Shadow(key) : System.Array.Empty<SadeTheme.ShadowLayer>();
            var have = g.GetComponents<Shadow>();
            if (allowAdd)
                for (int i = have.Length; i < layers.Length; i++) g.gameObject.AddComponent<Shadow>();
            have = g.GetComponents<Shadow>();
            for (int i = 0; i < have.Length; i++)
            {
                bool on = i < layers.Length;
                if (have[i].enabled != on) have[i].enabled = on;
                if (!on) continue;
                var d = new Vector2(layers[i].offset.x, -layers[i].offset.y);
                if (have[i].effectDistance != d) have[i].effectDistance = d;
                if (have[i].effectColor != layers[i].color) have[i].effectColor = layers[i].color;
                if (!have[i].useGraphicAlpha) have[i].useGraphicAlpha = true;
            }
        }

        /// <summary>Etiket değeri: “47,3”, “≈41,8”, “≈%37”; ondalık ve yüzde VM'den.</summary>
        public static string Value(float value, int decimals, bool percent, bool approx)
        {
            string s = percent ? DeskFormat.Percent(value, decimals) : DeskFormat.Number(value, decimals);
            return approx ? DeskFormat.Approximate(s) : s;
        }

        /// <summary>Metnin TMP ölçülen genişliği (tasarım px).</summary>
        public static float Width(TMP_Text t, string text) => t == null || string.IsNullOrEmpty(text) ? 0 : DeskUi.TextWidth(t, text);

        /// <summary>Sol-üst köşeli yerleşim (tasarım px; y aşağı).</summary>
        public static void Place(Component c, float x, float y, float w, float h)
        {
            if (c != null) DeskUi.Place((RectTransform)c.transform, x, y, w, h);
        }
    }
}
