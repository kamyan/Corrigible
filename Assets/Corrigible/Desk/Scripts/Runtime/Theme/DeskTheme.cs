using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Sıra serileştirilir (tema rolleri int tutar): yeni font yalnız sona eklenir.</summary>
    public enum DeskFont { CpRegular, CpBold, LfRegular, LfMedium, LfSemiBold, LfBold, LfExtraBold, KalamRegular, KalamBold, StencilExtraBold }

    public enum ShadowPreset { None, S1, S2, S3, S5 }

    /// <summary>
    /// Masa teması (PROMPT §4.3): <c>spec/tokens.json</c>'dan doldurulur (<c>Corrigible/Desk UI/Temayı tokens.json'dan doldur</c>).
    /// Renkler anahtarla alınır (“paper.page”, “ink.red”…).
    /// Tek örnek <c>Resources/DeskTheme</c>; bileşenler <see cref="Active"/> üzerinden okur, palet değişince <see cref="Changed"/> yayılır.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Desk Theme", fileName = "DeskTheme")]
    public sealed class DeskTheme : ScriptableObject
    {
        [Serializable]
        public struct NamedColor
        {
            public string key;
            public Color color;
        }

        [Serializable]
        public struct ShadowSpec
        {
            public Vector2 offset;
            public Color color;
        }

        [Header("Renkler")]
        public List<NamedColor> colors = new List<NamedColor>();

        [Header("Fontlar")]
        public TMP_FontAsset cpRegular, cpBold, lfRegular, lfMedium, lfSemiBold, lfBold, lfExtraBold, kalamRegular, kalamBold;
        [Tooltip("Big Shoulders Stencil Display 800 (Revizyon 3 şablon yazıları); yoksa CP Bold.")]
        public TMP_FontAsset stencilExtraBold;
        public TMP_StyleSheet styleSheet;

        [Header("Gölgeler (x, y aşağı pozitif)")]
        public ShadowSpec s1 = new ShadowSpec { offset = new Vector2(1, 2), color = new Color(0.353f, 0.275f, 0.118f, 0.25f) };
        public ShadowSpec s2 = new ShadowSpec { offset = new Vector2(2, 3), color = new Color(0, 0, 0, 0.32f) };
        public ShadowSpec s3 = new ShadowSpec { offset = new Vector2(3, 4), color = new Color(0, 0, 0, 0.40f) };
        public ShadowSpec s5 = new ShadowSpec { offset = new Vector2(5, 6), color = new Color(0, 0, 0, 0.50f) };

        [Header("Hareket (sn)")]
        public float hoverLift = 0.08f;
        public float press = 0.06f;

        // ------------------------------------------------------------------ etkin tema
        const string ResourcePath = "DeskTheme";
        static DeskTheme active;

        public static DeskTheme Active
        {
            get
            {
                if (active == null) active = Resources.Load<DeskTheme>(ResourcePath);
                return active;
            }
            set { active = value; Changed?.Invoke(); }
        }

        /// <summary>Tema değişti; <see cref="ThemeTint"/> gibi bileşenler renklerini yeniden uygular.</summary>
        public static event Action Changed;

        // ------------------------------------------------------------------ erişim
        Dictionary<string, Color> map;

        void OnEnable() => map = null;
        void OnValidate() { map = null; Changed?.Invoke(); }

        void Build()
        {
            map = new Dictionary<string, Color>();
            foreach (var c in colors) if (!string.IsNullOrEmpty(c.key)) map[c.key] = c.color;
        }

        public bool TryGet(string key, out Color color)
        {
            // Sade arayüz anahtarları (“sade.status.threat”) kendi temasından
            if (key != null && key.StartsWith(SadeTheme.KeyPrefix, StringComparison.Ordinal))
            {
                var sade = SadeTheme.Active;
                if (sade != null && sade.TryGet(key.Substring(SadeTheme.KeyPrefix.Length), out color)) return true;
                color = default;
                return false;
            }
            if (map == null) Build();
            return map.TryGetValue(key, out color);
        }

        /// <summary>Anahtarın rengi; yoksa eflatun (eksik anahtar hemen görünsün).</summary>
        public Color Get(string key)
        {
            if (TryGet(key, out var c)) return c;
            if (Application.isPlaying) Debug.LogWarning("[Desk] Tema rengi yok: " + key);
            return Color.magenta;
        }

        public TMP_FontAsset Font(DeskFont f) => f switch
        {
            DeskFont.CpRegular => cpRegular, DeskFont.CpBold => cpBold,
            DeskFont.LfRegular => lfRegular, DeskFont.LfMedium => lfMedium, DeskFont.LfSemiBold => lfSemiBold,
            DeskFont.LfBold => lfBold, DeskFont.LfExtraBold => lfExtraBold,
            DeskFont.KalamRegular => kalamRegular, DeskFont.KalamBold => kalamBold,
            DeskFont.StencilExtraBold => stencilExtraBold != null ? stencilExtraBold : cpBold,
            _ => lfRegular,
        };

        public ShadowSpec Shadow(ShadowPreset p) => p switch
        {
            ShadowPreset.S1 => s1, ShadowPreset.S2 => s2, ShadowPreset.S3 => s3, ShadowPreset.S5 => s5,
            _ => default,
        };
    }
}
