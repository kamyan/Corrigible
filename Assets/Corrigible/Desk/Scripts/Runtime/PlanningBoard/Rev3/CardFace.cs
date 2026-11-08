using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kart yüzünün değişen katmanı (Revizyon 3 · R16; PROMPT §5 <c>CardFace</c>): kartın önündeki World Space tuvalde (1 birim =
    /// 1 mm, sol üst köşe orijin, y aşağı) yazılar, pipler, durum katmanları ve seçim çerçevesi. Kâğıt, çizgiler ve desenler
    /// tuvalde değildir: kartın ışık alan 3B yüzündeki (<c>cardFace</c> yuvası) yazısız taban dokusundadır.
    /// <list type="bullet">
    /// <item>Bağlama yalnız değişince iş yapar: alt sınıfın <c>Bind</c>'ı önbellekteki değerlerle karşılaştırır, aynıysa hemen döner.
    /// Gerçek her yeniden bağlama <see cref="ChangedBinds"/>'ı bir artırır (boşta 0 kalmalı; test ve performans ölçümü).</item>
    /// <item>Yazılarda otomatik punto yok, tek satırlık alanlar sarmaz, <c>raycastTarget</c> kapalı, font malzemesi paylaşılır
    /// (<c>font.material</c>); tuvalde <c>GraphicRaycaster</c> ve <c>Mask</c> yoktur. Tıklama kartın kutu çarpıştırıcısından gelir.</item>
    /// <item>Metinler anlık görüntüden gelir (zaten çevrilmiş); yüz <c>Loc</c> çağırmaz, kart yazılarında <c>DeskLocText</c> yoktur.</item>
    /// </list>
    /// Punto taraması (<c>SiginakTextScan</c>) alanları sınıflarına göre buradan okur: ad (≥ 18 px), değerler (≥ 14 px) ve gerekçeli
    /// küçük yazılar.
    /// </summary>
    public abstract class CardFace : MonoBehaviour
    {
        /// <summary>Bütün kart yüzlerinde gerçekleşen (önbellekten dönmeyen) bağlama sayısı.</summary>
        public static int ChangedBinds;

        /// <summary>Bu yüzün kart türü.</summary>
        public abstract BoardCardKind Kind { get; }

        /// <summary>18 px sınıfındaki ad (yalnız kişi kartında; ötekilerde null).</summary>
        public abstract TMP_Text NameText { get; }

        /// <summary>14 px sınıfındaki değer ve etiket yazıları.</summary>
        public abstract IEnumerable<TMP_Text> ValueTexts { get; }

        /// <summary>Eşiğin altında kalması kabul edilen yazılar ve gerekçeleri (her birinin ≥ 14 px bir taşıyıcısı vardır).</summary>
        public abstract IEnumerable<(TMP_Text text, string reason)> SmallTexts { get; }

        /// <summary>Kartın tek satırlık alanları ve izin verilen en geniş yazı genişliği (mm); taşma testi bunları ölçer.</summary>
        public abstract IEnumerable<(TMP_Text text, float maxWidthMm)> SingleLineFields { get; }

        /// <summary>Yüzün bütün yazıları (gizliler dahil).</summary>
        public TMP_Text[] AllTexts => GetComponentsInChildren<TMP_Text>(true);

        protected static void CountBind() => ChangedBinds++;

        /// <summary>Yazıyı yazar (TMP aynı metinde zaten iş yapmaz).</summary>
        protected static void SetText(TMP_Text t, string value)
        {
            if (t != null) t.text = value ?? "";
        }

        protected static void SetColor(Graphic g, Color c)
        {
            if (g != null && g.color != c) g.color = c;
        }

        protected static void SetActive(Component c, bool on)
        {
            if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        protected static void SetSprite(Image image, Sprite sprite, Color tint)
        {
            if (image == null) return;
            if (image.sprite != sprite) image.sprite = sprite;
            SetColor(image, tint);
            if (image.enabled != (sprite != null)) image.enabled = sprite != null;
        }

        /// <summary>Metnin bu yazıdaki tercih edilen genişliği (yazının birimiyle: tuvalde mm, 3B yazıda metre).</summary>
        protected static float PreferredWidth(TMP_Text t, string value) =>
            t == null || string.IsNullOrEmpty(value) ? 0 : t.GetPreferredValues(value, float.PositiveInfinity, float.PositiveInfinity).x;

        protected static bool Same(string[] cache, string[] values)
        {
            if (values == null) return cache == null;
            if (cache == null || cache.Length != values.Length) return false;
            for (int i = 0; i < values.Length; i++) if (cache[i] != values[i]) return false;
            return true;
        }

        protected static bool Same(int[] cache, int[] values)
        {
            if (values == null) return cache == null;
            if (cache == null || cache.Length != values.Length) return false;
            for (int i = 0; i < values.Length; i++) if (cache[i] != values[i]) return false;
            return true;
        }

        protected static void Copy(ref string[] cache, string[] values)
        {
            if (values == null) { cache = null; return; }
            if (cache == null || cache.Length != values.Length) cache = new string[values.Length];
            System.Array.Copy(values, cache, values.Length);
        }

        protected static void Copy(ref int[] cache, int[] values)
        {
            if (values == null) { cache = null; return; }
            if (cache == null || cache.Length != values.Length) cache = new int[values.Length];
            System.Array.Copy(values, cache, values.Length);
        }

        /// <summary>Önbelleği boşaltır: sonraki bağlama her şeyi yeniden yazar (havuzdan yeni kimliğe verilen kart).</summary>
        public abstract void Invalidate();
    }
}
