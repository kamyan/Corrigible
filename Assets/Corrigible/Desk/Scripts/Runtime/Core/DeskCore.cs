using System.Collections.Generic;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Ortak küçük yardımcılar: yalnızca değişeni yaz, sabit konum, tema rengi.</summary>
    public static class DeskUi
    {
        /// <summary>OnValidate içinden çağrılır: SetActive/sprite değişimi doğrulama sırasında yapılamadığı için bir sonraki editör karesine ertelenir.</summary>
        public static void Deferred(Object owner, System.Action action)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => { if (owner != null) action(); };
#else
            action();
#endif
        }

        public static void SetText(TMP_Text t, string s)
        {
            if (t != null && t.text != s) t.text = s;
        }

        public static void SetActive(Component c, bool on)
        {
            if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        public static void SetActive(GameObject g, bool on)
        {
            if (g != null && g.activeSelf != on) g.SetActive(on);
        }

        public static void SetColor(Graphic g, Color c)
        {
            if (g != null && g.color != c) g.color = c;
        }

        /// <summary>Tema anahtarıyla renk (+ alfa çarpanı). Graphic'te <see cref="ThemeTint"/> varsa onu da günceller.</summary>
        public static void Tint(Graphic g, string key, float alpha = 1)
        {
            if (g == null) return;
            var tint = g.GetComponent<ThemeTint>();
            if (tint != null) { tint.Set(key, alpha); return; }
            var theme = DeskTheme.Active;
            if (theme == null) return;
            var c = theme.Get(key); c.a *= alpha;
            SetColor(g, c);
        }

        /// <summary>Sol-üst köşeli sabit yerleşim (CSS left/top/width/height; y aşağı).</summary>
        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>uGUI otomatik gezinmesini kapatır (PROMPT §8: kısayollar komutlara bağlanır).</summary>
        public static void NoNavigation(Selectable s)
        {
            if (s.navigation.mode != Navigation.Mode.None) s.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        public static void SetY(RectTransform rt, float y)
        {
            if (rt != null && !Mathf.Approximately(rt.anchoredPosition.y, y)) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
        }

        /// <summary>
        /// Metnin CSS düzen genişliği: TMP tercih edilen genişlik + son harften sonraki harf aralığı
        /// (CSS letter-spacing son harfe de eklenir; TMP eklemez).
        /// </summary>
        public static float TextWidth(TMP_Text t, string s)
        {
            if (t == null || string.IsNullOrEmpty(s)) return 0;
            float w = t.GetPreferredValues(s, 10000, 1000).x;
            if (t.characterSpacing > 0) w += t.characterSpacing * 0.01f * t.fontSize;
            return w;
        }

        /// <summary>
        /// Bölüm başlığı çizgisi (CSS .secth: başlık · çizgi · sağ metin, gap 8): çizgi başlığın bittiği yerden sağ metne kadar.
        /// <paramref name="right"/> boş olabilir; <paramref name="width"/> satırın toplam genişliği (başlığın x'inden).
        /// </summary>
        public static void CapRule(TMP_Text cap, RectTransform rule, TMP_Text right, float width)
        {
            if (cap == null || rule == null) return;
            float x0 = cap.rectTransform.anchoredPosition.x;
            float capW = Mathf.Round(TextWidth(cap, cap.text));
            float rightW = right != null && right.gameObject.activeSelf ? Mathf.Round(TextWidth(right, right.text)) : 0;
            float x = x0 + capW + 8;
            rule.anchoredPosition = new Vector2(x, rule.anchoredPosition.y);
            rule.sizeDelta = new Vector2(Mathf.Max(0, x0 + width - (rightW > 0 ? rightW + 8 : 0) - x), rule.sizeDelta.y);
        }
    }

    /// <summary>
    /// Tuş fişi metinleri (PROMPT §8). Projede InputAction yok (<c>Keyboard.current</c>); fiş metni bu tablodan gelir
    /// ve <c>Loc</c> ile çevrilir (Space → “Boşluk”).
    /// </summary>
    public static class DeskKeys
    {
        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "Space", Loc.N("Boşluk") }, { "Enter", "Enter" }, { "Escape", "Esc" }, { "Esc", "Esc" },
            { "Plus", "+" }, { "Minus", "−" }, { "Tab", "Tab" },
        };

        public static string Label(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            return Names.TryGetValue(key, out var n) ? DeskLoc.Instance.T(n) : key;
        }
    }
}
