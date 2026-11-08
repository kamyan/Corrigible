using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Tarif defteri sayfasının (Revizyon 3 · Aşama 5, R18a; PLAN §1.5) sprite'sız küçük şekilleri: tarif kutusunun dört fotoğraf
    /// köşesi ve sayfa köşesi sekmesinin dik üçgenleri, sekmelerdeki ok üçgenleri ve noktalı kılavuz çizgisi. Tasarım kaynağının
    /// (<c>kagit_kaynak/tarif_acilim_n</c>) SVG yolları ve <c>radial-gradient</c> noktaları; dikdörtgen içinde çizilir, rengi
    /// <see cref="Graphic.color"/>. Işın almaz (sayfa tuvalinde <c>GraphicRaycaster</c> yok).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BookPageShape : MaskableGraphic
    {
        /// <summary>Dik üçgenler dik açının köşesiyle adlanır; oklar sivri ucun yönüyle; noktalar yatay sıra.</summary>
        public enum Shape { CornerTopLeft, CornerTopRight, CornerBottomRight, CornerBottomLeft, ArrowRight, ArrowLeft, Dots }

        [SerializeField] Shape shape;
        [Tooltip("Noktalı kılavuz: nokta adımı ve nokta kenarı (tuval birimi = mm).")]
        [SerializeField] float pitch = 0.875f, dot = 0.35f;

        public Shape Kind
        {
            get => shape;
            set { if (shape == value) return; shape = value; SetVerticesDirty(); }
        }

        public float Pitch => pitch;
        public float Dot => dot;

        /// <summary>Noktalı kılavuzun adımı ve nokta boyu (mm).</summary>
        public void SetDots(float pitchMm, float dotMm)
        {
            pitchMm = Mathf.Max(0.05f, pitchMm);
            dotMm = Mathf.Clamp(dotMm, 0.01f, pitchMm);
            if (Mathf.Approximately(pitch, pitchMm) && Mathf.Approximately(dot, dotMm)) return;
            pitch = pitchMm; dot = dotMm;
            SetVerticesDirty();
        }

        /// <summary>Kılavuzdaki nokta sayısı (dikdörtgenin genişliğine sığan).</summary>
        public int DotCount(float width) => width < dot ? 0 : Mathf.FloorToInt((width - dot) / pitch + 1e-4f) + 1;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Color32 c = color;
            Vector2 tl = new Vector2(r.xMin, r.yMax), tr = new Vector2(r.xMax, r.yMax), br = new Vector2(r.xMax, r.yMin), bl = new Vector2(r.xMin, r.yMin);
            switch (shape)
            {
                case Shape.CornerTopLeft: Tri(vh, tl, tr, bl, c); break;
                case Shape.CornerTopRight: Tri(vh, tl, tr, br, c); break;
                case Shape.CornerBottomRight: Tri(vh, tr, br, bl, c); break;
                case Shape.CornerBottomLeft: Tri(vh, tl, br, bl, c); break;
                case Shape.ArrowRight: Tri(vh, tl, new Vector2(r.xMax, r.center.y), bl, c); break;
                case Shape.ArrowLeft: Tri(vh, tr, new Vector2(r.xMin, r.center.y), br, c); break;
                case Shape.Dots:
                {
                    int n = DotCount(r.width);
                    float half = dot * 0.5f, y = r.center.y;
                    for (int i = 0; i < n; i++)
                    {
                        float x = r.xMin + i * pitch;
                        Quad(vh, new Vector2(x, y - half), new Vector2(x + dot, y + half), c);
                    }
                    break;
                }
            }
        }

        static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 d, Color32 c)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, c, Vector2.zero);
            vh.AddVert(b, c, Vector2.zero);
            vh.AddVert(d, c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }

        static void Quad(VertexHelper vh, Vector2 min, Vector2 max, Color32 c)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), c, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), c, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), c, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
