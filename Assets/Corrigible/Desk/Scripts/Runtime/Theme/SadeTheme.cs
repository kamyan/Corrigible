using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sade arayüz teması (Docs/UI_Sade › tokens_sade.json): renkler ve sayılar JSON yoluyla, yazı tablosu, gölgeler, sızma
    /// rampası. C'nin <see cref="DeskTheme"/>'i üstüne kurulur: fontlar ve C renkleri oradan gelir; “sade.” önekli tema
    /// anahtarları (ThemeTint, DeskUi.Tint) buradan okunur. İçeriği “Corrigible/Sade UI/Tokenları içe aktar” doldurur.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Sade Theme", fileName = "SadeTheme")]
    public sealed class SadeTheme : TokenTheme
    {
        public const string KeyPrefix = "sade.";

        /// <summary>Yazı tablosu girdisi: tokens_sade.type › [aile, ağırlık, px, harf aralığı em].</summary>
        [Serializable] public struct TypeStyle { public string role; public DeskFont font; public float size; public float letterSpacingEm; }
        [Serializable] public struct ShadowLayer { public Vector2 offset; public Color color; }
        [Serializable] public struct NamedShadow { public string key; public ShadowLayer[] layers; }
        [Serializable] public struct RampStep { public float from; public Color fill; }

        public List<TypeStyle> type = new List<TypeStyle>();
        public List<NamedShadow> shadows = new List<NamedShadow>();
        public RampStep[] infiltrationRamp = Array.Empty<RampStep>();
        public Color rampTextLight = new Color(0.118f, 0.106f, 0.086f), rampTextDark = new Color(1f, 0.973f, 0.933f);
        public float rampTextDarkFrom = 40;

        Dictionary<string, TypeStyle> typeMap;
        Dictionary<string, ShadowLayer[]> shadowMap;

        static SadeTheme active;
        public static SadeTheme Active
        {
            get { if (active == null) active = Resources.Load<SadeTheme>("SadeTheme"); return active; }
            set => active = value;
        }

        public override void Invalidate() { base.Invalidate(); typeMap = null; shadowMap = null; }

        public bool TryType(string role, out TypeStyle style)
        {
            if (typeMap == null)
            {
                typeMap = new Dictionary<string, TypeStyle>(type.Count);
                foreach (var t in type) typeMap[t.role] = t;
            }
            return typeMap.TryGetValue(role, out style);
        }

        public ShadowLayer[] Shadow(string key)
        {
            if (shadowMap == null)
            {
                shadowMap = new Dictionary<string, ShadowLayer[]>(shadows.Count);
                foreach (var s in shadows) shadowMap[s.key] = s.layers;
            }
            return shadowMap.TryGetValue(key, out var l) ? l : Array.Empty<ShadowLayer>();
        }

        /// <summary>Yazı tablosundaki rolü TMP metnine uygular (font, boyut, harf aralığı em × 100). Rol yoksa dokunmaz.</summary>
        public bool ApplyType(TMP_Text t, string role)
        {
            if (t == null || string.IsNullOrEmpty(role) || !TryType(role, out var s)) return false;
            var desk = DeskTheme.Active;
            var font = desk != null ? desk.Font(s.font) : null;
            if (font != null && t.font != font) t.font = font;
            if (!Mathf.Approximately(t.fontSize, s.size)) t.fontSize = s.size;
            float spacing = s.letterSpacingEm * 100f;
            if (!Mathf.Approximately(t.characterSpacing, spacing)) t.characterSpacing = spacing;
            if (t.enableAutoSizing) t.enableAutoSizing = false;
            return true;
        }

        /// <summary>Sızma rampasının rengi (0–100); sınırlar VM'den gelirse onlar kullanılır.</summary>
        public Color RampFill(float infiltration)
        {
            if (infiltrationRamp.Length == 0) return Color.magenta;
            var c = infiltrationRamp[0].fill;
            foreach (var s in infiltrationRamp) if (infiltration >= s.from) c = s.fill;
            return c;
        }

        /// <summary>Rampa üstündeki değerin rengi: 40'tan itibaren krem, altında mürekkep.</summary>
        public Color RampText(float infiltration) => infiltration >= rampTextDarkFrom ? rampTextDark : rampTextLight;
    }
}
