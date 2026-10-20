using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AynaHeatGraphic : MaskableGraphic
    {
        [SerializeField] Color coldColor = new Color(.025f, .09f, .14f, 1), hotColor = new Color(.16f, .47f, .9f, 1);
        readonly double[] heat = new double[64];
        public void SetValues(double[] source)
        {
            Array.Clear(heat, 0, heat.Length);
            if (source != null) for (int i = 0; i < source.Length; i++) heat[i % 64] += source[i];
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect(); double max = heat.Max(); float halfW = r.width * .058f, halfH = r.height * .055f;
            Vector2 origin = new Vector2(r.center.x, r.yMax - halfH);
            for (int row = 0; row < 8; row++) for (int col = 0; col < 8; col++)
            {
                Vector2 p = origin + new Vector2((col - row) * halfW, -(col + row) * halfH); Color c = Color.Lerp(coldColor, hotColor, max <= 0 ? 0 : (float)(heat[row * 8 + col] / max)); int i = vh.currentVertCount;
                vh.AddVert(p + new Vector2(0, halfH * .92f), c, Vector2.zero); vh.AddVert(p + new Vector2(halfW * .92f, 0), c, Vector2.zero); vh.AddVert(p - new Vector2(0, halfH * .92f), c, Vector2.zero); vh.AddVert(p - new Vector2(halfW * .92f, 0), c, Vector2.zero); vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
            }
        }
    }
}
