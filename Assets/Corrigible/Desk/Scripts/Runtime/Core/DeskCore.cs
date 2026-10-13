using System.Collections.Generic;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Bir değerin ne kadar güvenilir olduğu; çizim tarzıyla gösterilir (damga / karbon / kalem halkası / boş alan).</summary>
    public enum TrustLevel { Verified, Report, Suspicious, Unknown }

    /// <summary>Durum kanalı: mürekkep rengi + ikon + kelime. Güvenle karışmaz.</summary>
    public enum StatusKind { Neutral, Threat, Warning, Info, Good }

    public enum StampInk { Blue, Red, Green }

    public enum StampSize { Small, Normal, Large }

    /// <summary>Ortak küçük yardımcılar: yalnızca değişeni yaz, sabit konum, tema rengi.</summary>
    public static class DeskUi
    {
        /// <summary>
        /// Edit modda masa önizlemesi bağlanıyor (editördeki DeskEditPreview ayarlar): dinamik listeler (harita, araştırma,
        /// teleks, pencere içerikleri) edit modda da kurulur; kopyalar sahneye kaydedilmez (<see cref="MarkPreview"/>).
        /// </summary>
        public static bool EditPreview;

        /// <summary>Çalışırken eklenen bileşen; edit mod önizlemesinde eklenirse sahneye kaydedilmez.</summary>
        public static T AddTransient<T>(GameObject go) where T : Component
        {
            var c = go.AddComponent<T>();
            if (!Application.isPlaying) c.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            return c;
        }

        /// <summary>
        /// Edit modda yaratılan kopya (önizleme içeriği; çalışırken etkisiz): sahneye kaydedilmez ve düzenlenemez. Play'de ve her
        /// önizlemede veriden yeniden kurulur; elle taşınsa kaybolurdu (kalıcı düzenleme şablonda ve prefab'da).
        /// </summary>
        public static void MarkPreview(GameObject go)
        {
            if (Application.isPlaying || go == null) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
                t.hideFlags |= HideFlags.NotEditable;
            }
        }

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

        /// <summary>
        /// Yerleşimi sahnede duran kâğıt (olay günlüğü, analist notu): konum, genişlik ve dönüş sahneden, yükseklik içerikten.
        /// Dönüşümsüz kutunun üst kenarı yerinde kalır (CSS: left/top sabit, dönüş merkez etrafında); dikey çapalar tek nokta olmalı.
        /// </summary>
        public static void SetHeightKeepTop(RectTransform r, float height)
        {
            if (r == null) return;
            float top = r.anchoredPosition.y + (1 - r.pivot.y) * r.sizeDelta.y;
            if (!Mathf.Approximately(r.sizeDelta.y, height)) r.sizeDelta = new Vector2(r.sizeDelta.x, height);
            var pos = new Vector2(r.anchoredPosition.x, top - (1 - r.pivot.y) * height);
            if (r.anchoredPosition != pos) r.anchoredPosition = pos;
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

        public static void SetWidth(RectTransform rt, float w)
        {
            if (rt != null && !Mathf.Approximately(rt.sizeDelta.x, w)) rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
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

        public static string TrustKey(TrustLevel t) => t switch
        {
            TrustLevel.Verified => "trust.verified.ink",
            TrustLevel.Report => "trust.report.ink",
            TrustLevel.Suspicious => "trust.suspicious.ink",
            _ => "trust.unknown.ink",
        };

        public static string StatusKey(StatusKind s) => s switch
        {
            StatusKind.Threat => "status.threat.ink",
            StatusKind.Warning => "status.warning.ink",
            StatusKind.Info => "status.info.ink",
            StatusKind.Good => "status.good.ink",
            _ => "ink.ink",
        };

        public static string StampKey(StampInk ink) => ink switch
        {
            StampInk.Red => "ink.red",
            StampInk.Green => "ink.green",
            _ => "ink.blue",
        };

        public static TrustLevel ParseTrust(string s) => s switch
        {
            "verified" => TrustLevel.Verified,
            "report" => TrustLevel.Report,
            "suspicious" => TrustLevel.Suspicious,
            _ => TrustLevel.Unknown,
        };

        public static StatusKind ParseStatus(string s) => s switch
        {
            "threat" => StatusKind.Threat,
            "warning" => StatusKind.Warning,
            "info" => StatusKind.Info,
            "good" => StatusKind.Good,
            _ => StatusKind.Neutral,
        };
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
