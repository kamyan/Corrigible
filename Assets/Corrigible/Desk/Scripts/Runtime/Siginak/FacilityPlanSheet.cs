using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bina planının bir paftası (tesis aşama 5; §7.1): ozalit kâğıt, başlık ("PAFTA 1/3 · GİRİŞ"), 8 × 8 çizim alanı ve antet
    /// (oda adı; şart ya da donanım). Bilinen odada çizim <see cref="FacilityPlanSketch"/>'in yedi katmanıdır (ızgara, duvar, menfez
    /// bağı, yapı ve nesneler, şart alanları, şart parçaları, aşama 10'da önlemlerin kırmızı kalemi); bilinmeyende yalnız dış çerçeve ve "PLAN YOK" damgası. Renkler
    /// <c>plan.*</c> token'larından (tokens_siginak.json). Üzerine gelince ortak etiket planı sözle verir. Kararı vermez, yalnız çizer.
    /// </summary>
    public sealed class FacilityPlanSheet : MonoBehaviour
    {
        [SerializeField] Image shadow, fill, edge;
        [SerializeField] UiHoverTarget hover;
        [SerializeField] TMP_Text title, caption, note;
        [Tooltip("Çizim alanı (8 kare); katmanlar onu doldurur.")]
        [SerializeField] RectTransform drawing;
        [SerializeField] UILineGraphic grid, walls, links, lines, areas, parts;
        [Tooltip("Tesis aşama 10 (K41): YZ'nin yapısal önlemleri, kırmızı kalem (plan.counter); parts'ın üstünde.")]
        [SerializeField] UILineGraphic counter;
        [SerializeField] RectTransform stamp;
        [SerializeField] Image stampFrame;
        [SerializeField] TMP_Text stampWord;
        [SerializeField] Sprite frameSolid, frameDashed;

        /// <summary>Son çizilen pafta (testler ve üzerine gelme okur).</summary>
        public FacilityPlanSheetData Data { get; private set; }
        /// <summary>Son çizim (katman yolları).</summary>
        public FacilityPlanSketch Sketch { get; private set; }

        public void Render(FacilityPlanSheetData d)
        {
            Data = d;
            var t = SiginakTheme.Active;
            bool known = d != null && d.Known;
            shadow.color = t.ColorOf("plan.shadow");
            fill.color = t.ColorOf("plan.paper");
            edge.color = t.ColorOf("plan.edge");
            title.text = d?.Title ?? "";
            title.color = caption.color = note.color = t.ColorOf("plan.line");
            caption.gameObject.SetActive(known && !string.IsNullOrEmpty(d.Caption));
            caption.text = known ? d.Caption ?? "" : "";
            note.gameObject.SetActive(known && !string.IsNullOrEmpty(d.Note));
            note.text = known ? d.Note ?? "" : "";
            note.color = t.ColorOf(known && d.NoteIsEvent ? "plan.part" : "plan.line");
            hover.Title = d?.HoverTitle;
            hover.Line = d?.HoverLine;

            float cell = drawing.rect.width / FacilityPlanSketch.N;
            Sketch = FacilityPlanSketch.Draw(d, cell);
            Layer(grid, Sketch.Grid, "plan.grid");
            Layer(walls, Sketch.Walls, "plan.wall");
            walls.Thickness = cell;
            Layer(links, Sketch.Links, "plan.line");
            links.SetDash(new[] { 1.5f, 3f });
            // Tesis aşama 11 (K39-6): izler mürekkep katmanında (sahne değişmez)
            Layer(lines, Sketch.Traces.Count == 0 ? Sketch.Lines : new List<List<Vector2>>(Sketch.Lines.Concat(Sketch.Traces)), "plan.line");
            Layer(areas, Sketch.Areas, "plan.part");
            areas.SetDash(new[] { 3f, 2.5f });
            Layer(parts, Sketch.Parts, "plan.part");
            if (counter != null) Layer(counter, Sketch.Counter, "plan.counter");

            stamp.gameObject.SetActive(!known && d != null);
            if (!known && d != null) Stamp(t, d.Stamp ?? "");
        }

        void Layer(UILineGraphic g, List<List<Vector2>> paths, string key)
        {
            g.color = SiginakTheme.Active.ColorOf(key);
            g.gameObject.SetActive(paths.Count > 0);
            g.SetPathsTopLeft(paths, Vector2.zero);
        }

        // Damga: OperationSlip'teki kalıp (renk + kenar deseni token'dan; CP 700 14; kenar 2 px, iç pay 8/4 px)
        void Stamp(SiginakTheme t, string word)
        {
            const string key = "stamp.noPlan";
            var c = t.ColorOf(key);
            bool dashed = t.Text(key + ".edge") == "dashed";
            stampFrame.sprite = dashed ? frameDashed : frameSolid;
            stampFrame.type = dashed ? Image.Type.Tiled : Image.Type.Sliced;
            stampFrame.fillCenter = !dashed;
            stampFrame.color = c;
            stampWord.color = c;
            stampWord.text = word;
            stampWord.textWrappingMode = TextWrappingModes.NoWrap;
            stamp.sizeDelta = stampWord.GetPreferredValues(word) + new Vector2(16, 8);
        }
    }
}
