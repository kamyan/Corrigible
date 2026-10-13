using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Olay bildirimi kâğıdı (PROMPT §7.3, CSS .memo): başlık + çift çizgi, 2×2 alan ızgarası (ÖNCELİK kırmızı damga), büyük başlık,
    /// alt başlık, paragraf, KAYNAKLAR. Yükseklik içerikten: paragrafın satır sayısı ve kaynak sayısı (CSS akışı).
    /// Koordinatlar kâğıdın sol-üstüne göre (y aşağı).
    /// </summary>
    public sealed class EventMemoView : MonoBehaviour
    {
        [Serializable]
        public struct FieldRefs
        {
            public TMP_Text key, value;
            public StampView stamp;
        }

        [SerializeField] RectTransform paper;
        [SerializeField] TMP_Text headerTitle, headerRight;
        [SerializeField] FieldRefs[] fields = Array.Empty<FieldRefs>();
        [SerializeField] TMP_Text title, subtitle, body;
        [SerializeField] RectTransform sources;
        [SerializeField] TMP_Text sourcesCap, sourcesCount;
        [SerializeField] RectTransform sourcesLine;
        [SerializeField] EventSourceRow rowTemplate;
        [Tooltip("Paragraf satır yüksekliği (LF 14,5 × 1,52 = 22).")]
        [SerializeField] float bodyLineHeight = 22;
        [SerializeField] float bodyGap = 14, sourcesHeadHeight = 20, rowHeight = 41.5f, lastRowHeight = 40.5f, bottom = 19, innerRight = 683;

        [Tooltip("Alan ızgarası (CSS .mf: auto 1fr auto 1fr, column-gap 12): içerik sol kenarı ve sütun aralığı.")]
        [SerializeField] float fieldsLeft = 29, fieldGap = 12;

        DeskPool<EventSourceRow> rows;

        static float Half(float v) => Mathf.Round(v * 2) / 2;

        /// <summary>
        /// 2×2 alan ızgarası: etiket sütunları en geniş etiket kadar, değer sütunları kalan genişliği paylaşır. TR'de ölçülen konumları
        /// (78,5 / 354,5 / 419) aynen verir; EN'de uzun etiket (PRIORITY) değeri ve damgayı sağa iter.
        /// </summary>
        void LayoutFields()
        {
            float a0 = 0, a1 = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                var k = fields[i].key;
                if (k == null || !k.gameObject.activeSelf) continue;
                float w = DeskUi.TextWidth(k, k.text);
                if (i % 2 == 0) a0 = Mathf.Max(a0, w); else a1 = Mathf.Max(a1, w);
            }
            float fr = (innerRight - fieldsLeft - a0 - a1 - 3 * fieldGap) / 2;
            float v0 = Half(fieldsLeft + a0 + fieldGap), k1 = Half(fieldsLeft + a0 + fieldGap + fr + fieldGap), v1 = Half(k1 + a1 + fieldGap);
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                float kx = i % 2 == 0 ? fieldsLeft : k1, vx = i % 2 == 0 ? v0 : v1;
                if (f.key != null) Place(f.key.rectTransform, kx);
                if (f.value != null)
                {
                    Place(f.value.rectTransform, vx);
                    f.value.rectTransform.sizeDelta = new Vector2(Half(fr), f.value.rectTransform.sizeDelta.y);
                }
                if (f.stamp != null)
                {
                    var r = (RectTransform)f.stamp.transform;
                    Place(r, vx + r.pivot.x * r.sizeDelta.x);
                }
            }
        }

        static void Place(RectTransform r, float x) => r.anchoredPosition = new Vector2(x, r.anchoredPosition.y);

        float subtitleY = float.NaN, bodyY, titleY, titleH;

        /// <summary>
        /// Uzun başlık (oyun verisi, EN) kâğıdın iç sağ kenarında satır kırar; alt başlık ve paragraf satır yüksekliği kadar iner.
        /// Tasarımdaki kısa başlıkta (tek satır) yerleşim değişmez. Otomatik boyut kullanılmaz (PROMPT §4.7).
        /// </summary>
        void WrapTitle()
        {
            var sr = subtitle.rectTransform; var br = body.rectTransform; var tr = title.rectTransform;
            if (float.IsNaN(subtitleY)) { subtitleY = sr.anchoredPosition.y; bodyY = br.anchoredPosition.y; titleY = tr.anchoredPosition.y; titleH = tr.sizeDelta.y; }
            tr.sizeDelta = new Vector2(innerRight - tr.anchoredPosition.x, titleH);
            // Sığıyorsa tasarımdaki gibi tek satır (sarma kapalı); sığmıyorsa önce bir boy küçülür (belge taslakları 40),
            // yine sığmıyorsa satır kırar
            if (titleFontSize <= 0) titleFontSize = title.fontSize;
            if (!Mathf.Approximately(title.fontSize, titleFontSize)) title.fontSize = titleFontSize;
            bool wrap = DeskUi.TextWidth(title, title.text) > tr.sizeDelta.x;
            if (wrap && shrinkTitleTo > 0)
            {
                title.fontSize = shrinkTitleTo;
                wrap = DeskUi.TextWidth(title, title.text) > tr.sizeDelta.x;
                if (wrap) title.fontSize = titleFontSize;
            }
            title.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            title.ForceMeshUpdate();
            int lines = wrap ? Mathf.Max(1, title.textInfo.lineCount) : 1;
            float extra = lines > 1 ? (lines - 1) * title.textInfo.lineInfo[0].lineHeight : 0;
            // Kutu aşağı doğru uzar: üst kenar sabit (pivot ne olursa olsun); son satırın inişi de sığsın
            tr.sizeDelta = new Vector2(tr.sizeDelta.x, wrap ? Mathf.Max(titleH + extra, title.preferredHeight) : titleH);
            tr.anchoredPosition = new Vector2(tr.anchoredPosition.x, titleY - (1 - tr.pivot.y) * extra);
            sr.anchoredPosition = new Vector2(sr.anchoredPosition.x, subtitleY - extra);
            br.anchoredPosition = new Vector2(br.anchoredPosition.x, bodyY - extra);
        }

        [Tooltip("Uzun başlık önce bu boya küçülür (0: küçülmez, doğrudan satır kırar).")]
        [SerializeField] float shrinkTitleTo = 40;
        float titleFontSize;

        [Header("Belge kipi (F8.3)")]
        [SerializeField] EventDocView doc;
        [SerializeField] float docGap = 14;
        [Tooltip("Yırtık alt kenar: kâğıt renginde dişli şerit (belge kipinde).")]
        [SerializeField] UIPolygonGraphic tornEdge;
        [SerializeField] float tornDepth = 10, tornStep = 18;

        public EventDocView Doc => doc;
        static readonly List<Vector2> tornTop = new List<Vector2>(), tornBottom = new List<Vector2>();

        /// <summary>Bildirim kâğıdı (yeri sahnede; iplik raptiyesi buna göre).</summary>
        public RectTransform Paper => paper;

        public void Bind(EventMemoVM m) => Bind(m, null);

        public void Bind(EventMemoVM m, EventDocVM docVm)
        {
            if (m == null) return;
            var T = DeskLoc.Instance;
            DeskUi.SetText(headerTitle, T.T(m.Header?.Title));
            DeskUi.SetText(headerRight, T.T(m.Header?.Right));
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                bool on = i < m.Fields.Count;
                DeskUi.SetActive(f.key, on);
                if (!on) { DeskUi.SetActive(f.value, false); DeskUi.SetActive(f.stamp, false); continue; }
                var vm = m.Fields[i];
                DeskUi.SetText(f.key, T.T(vm.K));
                bool stamped = vm.Stamp != null && f.stamp != null;
                DeskUi.SetActive(f.value, !stamped);
                DeskUi.SetActive(f.stamp, stamped);
                if (stamped) f.stamp.Set(T.T(vm.Stamp.Text), StampView.ParseInk(vm.Stamp.Color), StampSize.Normal, vm.Stamp.Rot, "event.field." + i, vm.Stamp.FontSize);
                else DeskUi.SetText(f.value, T.T(vm.V));
            }
            LayoutFields();
            DeskUi.SetText(title, T.T(m.Title));
            DeskUi.SetText(subtitle, T.T(m.Subtitle));
            DeskUi.SetText(body, T.T(m.Body));
            WrapTitle();

            // Paragraf yüksekliği satır sayısından (CSS line-height 1,52)
            body.ForceMeshUpdate();
            // Paragrafsız belge (ör. operasyon raporu): kaynaklar alt başlığın hemen altından başlar
            float bodyH = string.IsNullOrEmpty(m.Body) ? 0 : Mathf.Max(1, body.textInfo.lineCount) * bodyLineHeight;
            var br = body.rectTransform;
            br.sizeDelta = new Vector2(br.sizeDelta.x, bodyH);
            float y = -br.anchoredPosition.y + bodyH + (bodyH > 0 ? bodyGap : 0);

            // Kaynak yoksa başlık ve çizgisi de yok (oyundaki kaynaksız olay kartları, belge kipi)
            bool hasSources = m.SourcesHeader != null;
            DeskUi.SetActive(sourcesCap, hasSources);
            DeskUi.SetActive(sourcesCount, hasSources);
            DeskUi.SetActive(sourcesLine, hasSources);
            DeskUi.SetText(sourcesCap, T.T(m.SourcesHeader?.Title));
            DeskUi.SetText(sourcesCount, m.SourcesHeader?.Right);
            float capW = Mathf.Round(DeskUi.TextWidth(sourcesCap, sourcesCap.text));
            float countW = Mathf.Round(DeskUi.TextWidth(sourcesCount, sourcesCount.text));
            float lineX = sourcesCap.rectTransform.anchoredPosition.x + capW + 8;
            sourcesLine.anchoredPosition = new Vector2(lineX, sourcesLine.anchoredPosition.y);
            sourcesLine.sizeDelta = new Vector2(innerRight - sources.anchoredPosition.x - countW - 8 - lineX, sourcesLine.sizeDelta.y);
            sources.anchoredPosition = new Vector2(sources.anchoredPosition.x, -y);

            rows ??= new DeskPool<EventSourceRow>(rowTemplate, sources);
            int n = m.Sources.Count;
            float ry = hasSources ? sourcesHeadHeight : 0;
            for (int i = 0; i < n; i++)
            {
                var r = rows.Get(i);
                var rt = (RectTransform)r.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(0, -ry);
                r.Bind(m.Sources[i], i, i == n - 1);
                ry += i == n - 1 ? lastRowHeight : rowHeight;
            }
            rows.Trim(n);
            float docH = 0;
            if (doc != null)
            {
                DeskUi.SetActive(doc, docVm != null);
                if (docVm != null)
                {
                    var dr = (RectTransform)doc.transform;
                    float top = y + ry + (hasSources ? docGap : 0);
                    dr.anchoredPosition = new Vector2(dr.anchoredPosition.x, -Mathf.Round(top));
                    docH = (hasSources ? docGap : 0) + doc.Bind(docVm);
                }
            }
            float h = Mathf.Round((y + ry + docH + bottom) * 2) / 2;
            paper.sizeDelta = new Vector2(paper.sizeDelta.x, h);
            Tear(docVm != null && docVm.Torn, h);
        }

        /// <summary>Yırtık alt kenar: kâğıdın alt çizgisini örten, aşağı sarkan dişler (taslak .torn).</summary>
        void Tear(bool on, float h)
        {
            if (tornEdge == null) return;
            DeskUi.SetActive(tornEdge, on);
            if (!on) return;
            var tr = tornEdge.rectTransform;
            float w = paper.sizeDelta.x;
            tr.anchorMin = tr.anchorMax = new Vector2(0, 1); tr.pivot = new Vector2(0, 1);
            tr.anchoredPosition = new Vector2(0, -h + 2);   // kâğıdın çocuğu
            tr.sizeDelta = new Vector2(w, tornDepth + 2);
            tornTop.Clear(); tornBottom.Clear();
            int k = Mathf.Max(2, Mathf.RoundToInt(w / tornStep));
            for (int i = 0; i <= k; i++)
            {
                float x = w * i / k;
                tornTop.Add(new Vector2(x, 0));
                // Düzensiz dişler: çift adımda derin, tekte sığ (sabit desen; titremesin)
                float d = (i % 2 == 0 ? tornDepth : tornDepth * 0.25f) + ((i * 7) % 3);
                tornBottom.Add(new Vector2(x, -2 - d));
            }
            tornEdge.SetBand(tornTop, tornBottom);
        }
    }
}
