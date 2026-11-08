using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kişi dosyasının üst kenarındaki kıskaçlı etiket (<c>FileTag</c> modeli, <c>TXT_Tag</c> yuvası 46 × 20 mm; Revizyon 3 · Aşama 6,
    /// R17f, PLAN §1.16): <see cref="FolderData.TagText"/> ("İNCELEME" ya da plan eki "KISITLI · n V"); null ya da boşsa etiket gizli.
    /// Metin koddan (çevrilmiş) gelir; yuvanın 3B TMP'sine yazılır.
    /// <para>Sığdırma: yazı önce yuvanın puntosunda (prefab, cap 6 mm) denenir; sözcük bölünmeden yuvaya (genişlik ve yükseklik) sığana dek
    /// punto küçülür (en çok <see cref="MinScale"/>'e). Satır sarar; " · " ayırıcısı önceki sözcüğe bağlanır ("KISITLI ·" / "3 V").
    /// Ölçü çizilen yazının sınırından (<c>textBounds</c>): 3B TMP'nin <c>GetPreferredValues</c>'i 0,01 birime (10 mm) yuvarlar
    /// (GELISTIRME tuzağı, <c>CardStateOverlay.Fit</c>).</para>
    /// Bileşen sahnede FileTag örneğinin köküne eklenir; kendi nesnesini açıp kapar.
    /// </summary>
    public sealed class FileTagView : MonoBehaviour
    {
        /// <summary>Punto en çok bu orana küçülür.</summary>
        public const float MinScale = 0.55f;
        const float Step = 0.025f, Tolerance = 0.0002f;

        [Tooltip("Etiketin yazısı (FileTag/TXT_Tag; 3B TMP, yuva 0,046 × 0,02 m).")]
        [SerializeField] TMP_Text label;

        float designSize = -1f;
        string current;

        /// <summary>Etiket yuvadan taştı (en küçük puntoda bile).</summary>
        public bool Overflowing { get; private set; }
        /// <summary>Gösterilen metin (null: etiket gizli).</summary>
        public string Text => current;
        public TMP_Text Label => label;
        /// <summary>Son sığdırmanın punto oranı (1 = yuvanın puntosu).</summary>
        public float Scale { get; private set; } = 1f;

        /// <summary>Kurucunun (tek seferlik editör script'i, sahne) ve testlerin bağlaması.</summary>
        public void Configure(TMP_Text tagLabel)
        {
            label = tagLabel;
            designSize = -1f;
        }

        public void Bind(string tagText)
        {
            bool show = !string.IsNullOrEmpty(tagText);
            current = show ? tagText : null;
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
            if (!show) { Overflowing = false; return; }
            Fit(tagText);
        }

        void Fit(string text)
        {
            if (label == null) { Overflowing = false; return; }
            if (designSize <= 0f) designSize = label.fontSize;
            if (label.enableAutoSizing) label.enableAutoSizing = false;
            if (label.textWrappingMode != TextWrappingModes.Normal) label.textWrappingMode = TextWrappingModes.Normal;
            if (!label.richText) label.richText = true;
            string value = KeepSeparator(text);
            if (label.text != value) label.text = value;
            var box = label.rectTransform.rect.size;
            for (float s = 1f; ; s -= Step)
            {
                float scale = Mathf.Max(MinScale, s);
                float size = designSize * scale;
                if (!Mathf.Approximately(label.fontSize, size)) label.fontSize = size;
                label.ForceMeshUpdate(true, true);
                Scale = scale;
                if (Fits(box)) { Overflowing = false; return; }
                if (scale <= MinScale) { Overflowing = true; return; }
            }
        }

        /// <summary>" · " ayırıcısı önceki sözcükle birlikte kalır (satır başında nokta olmasın).</summary>
        static string KeepSeparator(string text)
        {
            int i = text.IndexOf(" · ", System.StringComparison.Ordinal);
            if (i <= 0) return text;
            int start = text.LastIndexOf(' ', i - 1) + 1;
            return text.Substring(0, start) + "<nobr>" + text.Substring(start, i + 2 - start) + "</nobr>" + text.Substring(i + 2);
        }

        bool Fits(Vector2 box)
        {
            var b = label.textBounds.size;
            if (b.x > box.x + Tolerance || b.y > box.y + Tolerance) return false;
            // Sözcük bölünmesin: her satır bir boşluktan sonra başlamalı (ve ayırıcıyla başlamamalı)
            var info = label.textInfo;
            for (int l = 1; l < info.lineCount; l++)
            {
                int first = info.lineInfo[l].firstCharacterIndex;
                if (first <= 0 || first >= info.characterCount) continue;
                char prev = info.characterInfo[first - 1].character;
                if (!char.IsWhiteSpace(prev)) return false;
                if (info.characterInfo[first].character == '·') return false;   // ayırıcı satır başına düşmesin
            }
            return true;
        }
    }
}
