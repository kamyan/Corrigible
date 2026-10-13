using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Metin ve kural denetleyicisi (PROMPT §9.4, §4.7, §12). Play modunda sandbox'ta TR ve EN için dört ön ayarı (C1'de bütün
    /// pencereler açık) kurar ve görünen her TMP metnini tarar:
    /// taşma (kabının dışına ya da sarılı metinde kutusunun altına), kardeş metinlerle çakışma, 11 px altı yazı, 15 px altı Kalam,
    /// Auto Size, UpperCase/SmallCaps. Ayrıca sprite'sız Image, Outline, etkileşimsiz raycast ve LayoutGroup/ContentSizeFitter.
    /// Rapor <c>Docs/UI_C/captures/F7_metin_denetimi.txt</c>.
    /// </summary>
    public static class DeskTextChecker
    {
        const string ReportPath = "Docs/UI_C/captures/F7_metin_denetimi.txt";
        /// <summary>Tasarımın kendisinde 15 px altı Kalam (TSV): güven kelimesi “şüpheli” tabloda ve teleksde 14 px.</summary>
        static readonly HashSet<string> KalamDesignExceptions = new HashSet<string> { "şüpheli", "suspect", "? şüpheli", "? suspect" };
        /// <summary>11 px altı ikon glifleri (tasarım): çekirdek rozetindeki harf.</summary>
        static readonly HashSet<string> SmallGlyphNames = new HashSet<string> { "CoreLetter", "Letter" };
        /// <summary>Kabın zeminini taşıyan çocuk adları (zemin ayrı kardeşte: kart dolgusu, lejant kutusu).</summary>
        static readonly string[] BackgroundNames = { "Fill", "Box" };

        [MenuItem("Corrigible/Desk UI/Metin denetimi (TR + EN, Play)")]
        public static string Run()
        {
            if (!Application.isPlaying) return "Play modunda çalıştırın (DeskUI_Sandbox).";
            var menu = Object.FindFirstObjectByType<DeskDebugMenu>();
            var src = Object.FindFirstObjectByType<MockDeskDataSource>();
            var wm = WindowManager.Instance;
            var sb = new StringBuilder();
            int errors = 0, warnings = 0;
            var startLang = DeskLoc.Instance.Lang;
            foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
            {
                DeskLoc.Instance.SetLanguage(lang);
                for (int p = 0; p < src.PresetCount; p++)
                {
                    menu.Preset(p);
                    var passes = p == 0
                        ? new[] { new WindowId[0], new[] { WindowId.News, WindowId.Teams }, new[] { WindowId.Guide, WindowId.AiLog } }
                        : new[] { new WindowId[0] };
                    foreach (var open in passes)
                    {
                        // Animasyonsuz aç: açılış solması (alfa 0) metinleri denetimden düşürürdü
                        foreach (var id in open) wm.Open(id, false);
                        Canvas.ForceUpdateCanvases();
                        string ctx = $"{lang} · C{p + 1}" + (open.Length > 0 ? " · " + string.Join("+", open) : "");
                        CheckTexts(ctx, sb, ref errors, ref warnings);
                        // Açtıklarını kapat: sonraki geçiş ve yakalamalar ön ayarın kendi pencere düzenini görsün
                        foreach (var id in open) wm.Close(id);
                    }
                    if (p == 0) { wm.Open(WindowId.Research, false); wm.Open(WindowId.Dossier, false); }
                }
            }
            DeskLoc.Instance.SetLanguage(startLang);
            menu.Preset(0);
            CheckRules(sb, ref errors, ref warnings);
            var head = $"Desk metin denetimi · {System.DateTime.Now:yyyy-MM-dd HH:mm} · {errors} hata, {warnings} uyarı\n\n";
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, head + sb);
            return head.Trim();
        }

        /// <summary>
        /// Oyun sahnesi (F8): o anki durumu TR ve EN'de denetler (ön ayar yok). Sonuç <c>captures/F8_metin_denetimi.txt</c>'ye eklenir.
        /// </summary>
        public static string RunCurrent(string label) => RunCurrent(label, 3);

        /// <param name="langs">1 TR, 2 EN, 3 ikisi. Oyun içi metinler (haber, günlük) üretildikleri dilde kalır: EN'yi EN başlatılmış oyunda denetle.</param>
        public static string RunCurrent(string label, int langs)
        {
            if (!Application.isPlaying) return "Play modunda çalıştırın.";
            var pres = Object.FindFirstObjectByType<DeskPresenter>();
            var sb = new StringBuilder();
            int errors = 0, warnings = 0;
            var startLang = DeskLoc.Instance.Lang;
            foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
            {
                if ((langs & (lang == DeskLang.Tr ? 1 : 2)) == 0) continue;
                DeskLoc.Instance.SetLanguage(lang);
                pres.Bind(pres.Source.Current);
                foreach (var m in Object.FindObjectsByType<EventModalView>(FindObjectsSortMode.None)) m.CompleteAnimation();
                Canvas.ForceUpdateCanvases();
                CheckTexts($"{lang} · {label}", sb, ref errors, ref warnings);
            }
            DeskLoc.Instance.SetLanguage(startLang);
            pres.Bind(pres.Source.Current);
            const string path = "Docs/UI_C/captures/F8_metin_denetimi.txt";
            var head = $"[{System.DateTime.Now:HH:mm}] {label}: {errors} hata, {warnings} uyarı\n";
            File.AppendAllText(path, head + sb + "\n");
            return head.Trim() + (sb.Length > 0 ? "\n" + sb : "");
        }

        static readonly Vector3[] corners = new Vector3[4];

        static Rect WorldRect(RectTransform r)
        {
            r.GetWorldCorners(corners);
            float xmin = corners.Min(c => c.x), xmax = corners.Max(c => c.x), ymin = corners.Min(c => c.y), ymax = corners.Max(c => c.y);
            return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
        }

        /// <summary>Metnin çizilen sınırları (dünya uzayı, eksene hizalı).</summary>
        static Rect TextWorldBounds(TMP_Text t)
        {
            t.ForceMeshUpdate();
            var b = t.textBounds;
            var rt = t.rectTransform;
            var pts = new[] { new Vector3(b.min.x, b.min.y), new Vector3(b.max.x, b.min.y), new Vector3(b.min.x, b.max.y), new Vector3(b.max.x, b.max.y) }
                .Select(p => rt.TransformPoint(p)).ToArray();
            return Rect.MinMaxRect(pts.Min(p => p.x), pts.Min(p => p.y), pts.Max(p => p.x), pts.Max(p => p.y));
        }

        /// <summary>Metnin kabı: zemin grafiği olan en yakın ata (kendi Image/RawImage'ı ya da “Fill” çocuğu olan).</summary>
        static RectTransform Container(Transform t)
        {
            for (var p = t.parent; p != null; p = p.parent)
            {
                if (p.GetComponent<Canvas>() != null) return null;
                if (p.GetComponent<ScrollRect>() != null || p.GetComponent<RectMask2D>() != null) return (RectTransform)p;
                if (p.GetComponent<Image>() is Image img && img.enabled && img.sprite != null) return (RectTransform)p;
                foreach (var bg in BackgroundNames)
                {
                    var fill = p.Find(bg);
                    if (fill != null && fill.GetComponent<Graphic>() != null) return (RectTransform)p;
                }
            }
            return null;
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var p = t; p != null && p.GetComponent<Canvas>() == null; p = p.parent) parts.Add(p.name);
            parts.Reverse();
            return string.Join("/", parts.Skip(System.Math.Max(0, parts.Count - 5)));
        }

        static string Short(string s) => s == null ? "" : (s.Length > 48 ? s.Substring(0, 48) + "…" : s).Replace("\n", " ⏎ ");

        static void CheckTexts(string ctx, StringBuilder sb, ref int errors, ref int warnings)
        {
            var root = GameObject.Find("DeskUI");
            var texts = root.GetComponentsInChildren<TMP_Text>(false).Where(t => t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text)).ToList();
            var theme = DeskTheme.Active;
            var lines = new List<string>();
            var bounds = new Dictionary<TMP_Text, Rect>();
            bool en = DeskLoc.Instance.Lang == DeskLang.En;
            // Metnin üstüne binmemesi gereken işaretler: damgalar ve tuş çipleri (döndürülmüş olabilir: eksene hizalı kutu, 2 px pay)
            var marks = root.GetComponentsInChildren<StampView>(false).Select(m => (Component)m)
                .Concat(root.GetComponentsInChildren<KeyChipView>(false))
                .Where(m => m.gameObject.activeInHierarchy && !m.GetComponentsInParent<CanvasGroup>().Any(g => g.alpha <= 0.01f)).ToList();
            foreach (var t in texts)
            {
                if (t.GetComponentsInParent<CanvasGroup>().Any(g => g.alpha <= 0.01f)) continue;
                if (t.name == "Ghost") continue;   // karbon kopyanın gölgesi
                if (t.canvas != null && !t.canvas.rootCanvas.enabled) continue;   // oyun sonunda gizlenen masa canvas'ları
                string id = $"{PathOf(t.transform)} «{Short(t.GetParsedText())}»";
                if (t.enableAutoSizing) lines.Add("HATA Auto Size: " + id);
                if ((t.fontStyle & (FontStyles.UpperCase | FontStyles.SmallCaps | FontStyles.LowerCase)) != 0) lines.Add("HATA UpperCase/SmallCaps: " + id);
                if (t.fontSize < 11 - 0.01f && !SmallGlyphNames.Contains(t.name)) lines.Add($"HATA {t.fontSize} px < 11: " + id);
                bool kalam = t.font == theme.kalamRegular || t.font == theme.kalamBold;
                if (kalam && t.fontSize < 15 - 0.01f)
                {
                    if (KalamDesignExceptions.Contains(t.GetParsedText().Trim())) { }
                    else lines.Add($"HATA Kalam {t.fontSize} px < 15: " + id);
                }

                // Dil adları bilerek kendi dillerinde (“Türkçe”, dil düğmesi)
                if (en && MockEnglish.LooksTurkish(t.text) && !t.text.Contains("Türkçe")) lines.Add("HATA çevrilmemiş: " + id);
                if (t.isTextTruncated) lines.Add("UYARI metin kısaltıldı: " + id);

                var tb = TextWorldBounds(t);
                bounds[t] = tb;
                var own = WorldRect(t.rectTransform);
                float scale = t.canvas != null ? t.canvas.scaleFactor : 1;
                float tol = 1.5f * scale;
                if (t.textWrappingMode != TextWrappingModes.NoWrap && t.overflowMode == TextOverflowModes.Overflow && tb.yMin < own.yMin - tol)
                    lines.Add($"HATA taşma (alt {(own.yMin - tb.yMin) / scale:F0} px): " + id);
                var box = Container(t.transform);
                // Kırpan kap (teleks şeridi, kaydırmalı pencere): içerik bilerek dışarı taşar ve kırpılır
                bool clipped = box != null && (box.GetComponent<RectMask2D>() != null || box.GetComponent<ScrollRect>() != null);
                if (box != null && !clipped && t.overflowMode == TextOverflowModes.Overflow && Mathf.Approximately(t.rectTransform.eulerAngles.z, 0))
                {
                    var cr = WorldRect(box);
                    float l = cr.xMin - tb.xMin, r = tb.xMax - cr.xMax, bo = cr.yMin - tb.yMin, to = tb.yMax - cr.yMax;
                    float worst = Mathf.Max(l, r, bo, to);
                    if (worst > tol) lines.Add($"HATA kabından taşma {worst / scale:F0} px ({box.name}): " + id);
                }
                foreach (var m in marks)
                {
                    if (t.transform.IsChildOf(m.transform) || Container(m.transform) != box) continue;
                    var mr = WorldRect((RectTransform)m.transform);
                    float pad = 2 * scale;
                    float ox = Mathf.Min(tb.xMax, mr.xMax - pad) - Mathf.Max(tb.xMin, mr.xMin + pad);
                    float oy = Mathf.Min(tb.yMax, mr.yMax - pad) - Mathf.Max(tb.yMin, mr.yMin + pad);
                    if (ox > tol && oy > tol) lines.Add($"HATA çakışma ({m.GetType().Name} {m.name}): " + id);
                }
            }
            // Kardeş metinler çakışmasın (aynı ebeveyn; döndürülmemiş)
            foreach (var grp in bounds.Keys.GroupBy(t => t.transform.parent))
            {
                var list = grp.Where(t => Mathf.Approximately(t.rectTransform.eulerAngles.z, 0)).ToList();
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var a = bounds[list[i]]; var b = bounds[list[j]];
                        float scale = list[i].canvas != null ? list[i].canvas.scaleFactor : 1;
                        var ov = Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
                        if (ov.width > 1.5f * scale && ov.height > 1.5f * scale)
                            lines.Add($"HATA çakışma: {PathOf(list[i].transform)} «{Short(list[i].GetParsedText())}» ↔ «{Short(list[j].GetParsedText())}»");
                    }
            }
            foreach (var l in lines.Distinct())
            {
                sb.Append('[').Append(ctx).Append("] ").AppendLine(l);
                if (l.StartsWith("HATA")) errors++; else warnings++;
            }
        }

        static void CheckRules(StringBuilder sb, ref int errors, ref int warnings)
        {
            var root = GameObject.Find("DeskUI");
            sb.AppendLine().AppendLine("— Kurallar (§4.7, §12) —");
            foreach (var img in root.GetComponentsInChildren<Image>(true))
                if (img.sprite == null) { sb.AppendLine("HATA sprite'sız Image: " + PathOf(img.transform)); errors++; }
            foreach (var o in root.GetComponentsInChildren<Outline>(true)) { sb.AppendLine("HATA Outline: " + PathOf(o.transform)); errors++; }
            foreach (var lg in root.GetComponentsInChildren<LayoutGroup>(true)) { sb.AppendLine("UYARI LayoutGroup: " + PathOf(lg.transform)); warnings++; }
            foreach (var f in root.GetComponentsInChildren<ContentSizeFitter>(true)) { sb.AppendLine("UYARI ContentSizeFitter: " + PathOf(f.transform)); warnings++; }
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (!g.raycastTarget) continue;
                bool interactive = g.GetComponentInParent<Selectable>(true) != null || g.GetComponentInParent<ScrollRect>(true) != null
                    || g.GetComponentsInParent<MonoBehaviour>(true).Any(m => m is IPointerEnterHandler || m is IPointerClickHandler || m is IPointerDownHandler || m is IBeginDragHandler)
                    || g is DeskHitArea || g is SpotlightGraphic
                    || g.name == "Scrim";   // modal karartması: arka planı bilerek engeller
                if (!interactive) { sb.AppendLine("UYARI etkileşimsiz raycast: " + PathOf(g.transform)); warnings++; }
            }
        }
    }
}
