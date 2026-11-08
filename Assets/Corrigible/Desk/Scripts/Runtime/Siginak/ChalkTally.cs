using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Arka duvardaki tebeşir vardiya çentiği (Revizyon 2 · S1): her vardiya bir çizgi, beşincisi dördünü çaprazlar. Çizgiler
    /// kapalı şablon çizgiden çoğaltılır; vardiya çoğalınca gruplar genişliğe sığsın diye sıklaşır. Değeri bağlayıcı yazar.
    /// Revizyon 3 (<see cref="marks"/> doluysa): modelin 16 çentik yeri, her biri bir beşli grup; atlas dokusunun hücresi (1–5
    /// çentik) property block ile seçilir, son grup doluysa sonrakiler gösterilmez (80 vardiyadan sonra son yer dolu kalır).
    /// </summary>
    public sealed class ChalkTally : MonoBehaviour
    {
        [Tooltip("Şablon çizgi (kapalı Quad; yerel ölçeği çizginin kalınlığı ve boyu).")]
        [SerializeField] Transform stroke;
        [Tooltip("Etiket (“vardiya”); hiç çizgi yokken gizli.")]
        [SerializeField] GameObject label;
        [Tooltip("Çizgilerin sığacağı genişlik (m); sol kenar bu nesnenin yerel x = 0'ı.")]
        [SerializeField, Min(0.05f)] float width = 0.46f;
        [Tooltip("Beşli grubun aralığı ve gruptaki çizgi aralığı (m).")]
        [SerializeField, Min(0.005f)] float groupPitch = 0.0625f, strokePitch = 0.0108f;

        [Tooltip("Revizyon 3 çetele modelinin çentik yerleri (okuma sırasıyla); doluysa şablon çizgi kullanılmaz.")]
        [SerializeField] Renderer[] marks = new Renderer[0];
        [Tooltip("Çentik atlasının hücre sayısı (1–5 çentik).")]
        [SerializeField, Min(1)] int atlasCells = 5;

        static readonly int StId = Shader.PropertyToID("_BaseMap_ST");
        readonly List<Transform> pool = new List<Transform>();
        MaterialPropertyBlock block;
        int shown = -1;

        public int Shown => shown;

        /// <summary>Grup başına gösterilen çentik sayısı (model kipi; test ve bağlantı için).</summary>
        public static int CellFor(int shifts, int mark, int markCount) =>
            mark == markCount - 1 && shifts > 5 * markCount ? 5 : Mathf.Clamp(shifts - 5 * mark, 0, 5);

        public void Show(int shifts)
        {
            shifts = Mathf.Max(0, shifts);
            if (shifts == shown) return;
            if (marks.Length > 0) { shown = shifts; ShowMarks(shifts); return; }
            if (stroke == null) return;
            shown = shifts;
            int groups = (shifts + 4) / 5;
            float k = groups * groupPitch > width ? width / (groups * groupPitch) : 1;
            float h = stroke.localScale.y, baseY = stroke.localPosition.y;
            while (pool.Count < shifts)
            {
                var s = Instantiate(stroke, transform);
                s.name = "Stroke " + pool.Count;
                pool.Add(s);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var s = pool[i];
                bool on = i < shifts;
                if (s.gameObject.activeSelf != on) s.gameObject.SetActive(on);
                if (!on) continue;
                int g = i / 5, j = i % 5;
                float gx = g * groupPitch * k;
                if (j < 4)
                {
                    // Elle çekilmiş: hafif kayma ve eğim (sıraya bağlı, her çizimde aynı)
                    s.localPosition = new Vector3(gx + j * strokePitch * k + ((i % 3) - 1) * 0.0008f, baseY, stroke.localPosition.z);
                    s.localRotation = Quaternion.Euler(0, 0, ((i % 2) * 2 - 1) * 2.5f);
                    s.localScale = stroke.localScale;
                }
                else
                {
                    // Çapraz: dördün solundan sağına, alttan üste
                    var from = new Vector2(gx - 0.006f * k, baseY - h * 0.35f);
                    var to = new Vector2(gx + (3 * strokePitch + 0.008f) * k, baseY + h * 0.35f);
                    var d = to - from;
                    s.localPosition = new Vector3((from.x + to.x) / 2, (from.y + to.y) / 2, stroke.localPosition.z - 0.0002f);
                    s.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90);
                    s.localScale = new Vector3(stroke.localScale.x, d.magnitude, 1);
                }
            }
            if (label != null) label.SetActive(shifts > 0);
        }

        void ShowMarks(int shifts)
        {
            block ??= new MaterialPropertyBlock();
            for (int k = 0; k < marks.Length; k++)
            {
                var m = marks[k];
                if (m == null) continue;
                int cell = CellFor(shifts, k, marks.Length);
                bool on = cell > 0;
                if (m.gameObject.activeSelf != on) m.gameObject.SetActive(on);
                if (!on) continue;
                m.GetPropertyBlock(block);
                block.SetVector(StId, new Vector4(1f / atlasCells, 1, (cell - 1) / (float)atlasCells, 0));
                m.SetPropertyBlock(block);
            }
            if (label != null) label.SetActive(shifts > 0);
        }
    }
}
