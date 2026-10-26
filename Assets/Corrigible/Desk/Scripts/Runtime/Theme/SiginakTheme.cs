using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sığınak komuta odası teması (Docs/UI_Siginak › tokens_siginak.json): renkler, sayılar, metin değerleri, yazı tablosu
    /// ve gölgeler JSON yoluyla. “colors” dalı öneksiz (“brass.metal”), öteki dallar adıyla (“materials.brass.roughness”,
    /// “cameras.K1.pos[0]”). Sade'den aynen kullanılan anahtarlar (reuse: ink, paper, status.*…) burada bulunmazsa
    /// <see cref="SadeTheme"/>'den okunur. İçeriği “Corrigible/Sığınak/Tokenları içe aktar” doldurur.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Siginak Theme", fileName = "SiginakTheme")]
    public sealed class SiginakTheme : TokenTheme
    {
        [Serializable] public struct NamedString { public string key; public string value; }

        public List<SadeTheme.TypeStyle> type = new List<SadeTheme.TypeStyle>();
        public List<SadeTheme.NamedShadow> shadows = new List<SadeTheme.NamedShadow>();
        /// <summary>Renk ya da sayı olmayan değerler: damga kenarı (“stamp.free.edge” = round), ışık türü, doku adı.</summary>
        public List<NamedString> strings = new List<NamedString>();

        Dictionary<string, SadeTheme.TypeStyle> typeMap;
        Dictionary<string, SadeTheme.ShadowLayer[]> shadowMap;
        Dictionary<string, string> stringMap;

        static SiginakTheme active;
        public static SiginakTheme Active
        {
            get { if (active == null) active = Resources.Load<SiginakTheme>("SiginakTheme"); return active; }
            set => active = value;
        }

        public override void Invalidate() { base.Invalidate(); typeMap = null; shadowMap = null; stringMap = null; }

        /// <summary>Anahtarın rengi; burada yoksa Sade temasından, orada da yoksa eflatun.</summary>
        public Color ColorOf(string key)
        {
            if (TryGet(key, out var c)) return c;
            var sade = SadeTheme.Active;
            return sade != null && sade.TryGet(key, out c) ? c : Color.magenta;
        }

        /// <summary>Sayı; burada yoksa Sade temasından.</summary>
        public float NumberOf(string key, float fallback = 0)
        {
            if (TryNumber(key, out var v)) return v;
            var sade = SadeTheme.Active;
            return sade != null && sade.TryNumber(key, out v) ? v : fallback;
        }

        /// <summary>“[0]”, “[1]” … ile düzleşmiş üç öğeli diziyi vektör olarak okur (konum, hedef).</summary>
        public Vector3 Vector(string key) => new Vector3(Number(key + "[0]"), Number(key + "[1]"), Number(key + "[2]"));

        public Vector2 Vector2Of(string key) => new Vector2(Number(key + "[0]"), Number(key + "[1]"));

        public bool TryText(string key, out string value)
        {
            if (stringMap == null)
            {
                stringMap = new Dictionary<string, string>(strings.Count);
                foreach (var s in strings) stringMap[s.key] = s.value;
            }
            return stringMap.TryGetValue(key, out value);
        }

        public string Text(string key) => TryText(key, out var v) ? v : null;

        public bool TryType(string role, out SadeTheme.TypeStyle style)
        {
            if (typeMap == null)
            {
                typeMap = new Dictionary<string, SadeTheme.TypeStyle>(type.Count);
                foreach (var t in type) typeMap[t.role] = t;
            }
            if (typeMap.TryGetValue(role, out style)) return true;
            var sade = SadeTheme.Active;
            return sade != null && sade.TryType(role, out style);
        }

        /// <summary>Yazı rolünü TMP metnine uygular (font, boyut, harf aralığı em × 100). Rol yoksa dokunmaz.</summary>
        public bool ApplyType(TMP_Text t, string role)
        {
            if (t == null || string.IsNullOrEmpty(role) || !TryType(role, out var s)) return false;
            var desk = DeskTheme.Active;
            var font = desk != null ? desk.Font(s.font) : null;
            if (font != null && t.font != font) t.font = font;
            if (s.size > 0 && !Mathf.Approximately(t.fontSize, s.size)) t.fontSize = s.size;
            float spacing = s.letterSpacingEm * 100f;
            if (!Mathf.Approximately(t.characterSpacing, spacing)) t.characterSpacing = spacing;
            if (t.enableAutoSizing) t.enableAutoSizing = false;
            return true;
        }

        /// <summary>Gölge katmanları; burada yoksa Sade temasından (card, hoverCard, plaque, eventSlip, topButton…).</summary>
        public SadeTheme.ShadowLayer[] Shadow(string key)
        {
            if (shadowMap == null)
            {
                shadowMap = new Dictionary<string, SadeTheme.ShadowLayer[]>(shadows.Count);
                foreach (var s in shadows) shadowMap[s.key] = s.layers;
            }
            if (shadowMap.TryGetValue(key, out var l)) return l;
            var sade = SadeTheme.Active;
            return sade != null ? sade.Shadow(key) : Array.Empty<SadeTheme.ShadowLayer>();
        }
    }
}
