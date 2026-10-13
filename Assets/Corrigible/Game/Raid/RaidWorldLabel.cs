using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>Kameraya dönük, piksel boyutu korunan kâğıt etiket. Yalnız sunum verisi taşır.</summary>
    public sealed class RaidWorldLabel : MonoBehaviour
    {
        Camera view;
        TextMeshPro text;
        Mesh paper;
        MeshRenderer paperRenderer;
        Color paperColor, edgeColor;
        float pixels = 12, minimumPixels = 11, opacity = 1;
        bool dashed;
        Vector2 preferred;
        float glyphHeight;
        Color textColor;
        bool dirty = true;
        float lastPixelHeight;
        Vector3 anchor;
        Shader overlayShader;
        int textRenderQueue;
        readonly Dictionary<Material, Material> overlayMaterials = new Dictionary<Material, Material>();

        Material OverlayMaterial(Material source)
        {
            if (source == null) return null;
            if (overlayMaterials.ContainsValue(source)) return source;
            if (overlayMaterials.TryGetValue(source, out var cached)) return cached;
            var material = new Material(source) { name = source.name + " Raid world overlay", hideFlags = HideFlags.DontSave };
            if (overlayShader != null) material.shader = overlayShader;
            material.renderQueue = textRenderQueue;
            overlayMaterials[source] = material;
            return material;
        }

        void RefreshSubmeshMaterials()
        {
            // Fallback glifleri de kâğıtla aynı derinlik davranışına uyar (✓ gibi).
            foreach (var submesh in text.GetComponentsInChildren<TMP_SubMesh>(true))
                submesh.sharedMaterial = OverlayMaterial(submesh.sharedMaterial);
        }

        public void SetAnchor(Vector3 value) { anchor = value; transform.position = value; }
        public void ResetAnchor() { transform.position = anchor; }

        public Rect ScreenBounds
        {
            get
            {
                Refresh();
                if (view == null || glyphHeight <= 0) return default;
                var center = view.WorldToScreenPoint(transform.position);
                var size = preferred * (lastPixelHeight / glyphHeight) + new Vector2(10, 6);
                return new Rect((Vector2)center - size * 0.5f, size);
            }
        }

        public void OffsetPixels(Vector2 offset)
        {
            if (view == null) return;
            var screen = view.WorldToScreenPoint(anchor);
            transform.position = view.ScreenToWorldPoint(screen + new Vector3(offset.x, offset.y, 0));
        }

        public void Configure(Camera camera, TMP_FontAsset font, Material fontMaterial, Material paperMaterial, Color paperInk, float fontPixels = 12, float minimumFontPixels = 11, Shader overlayFontShader = null)
        {
            view = camera; paperColor = paperInk; pixels = fontPixels; minimumPixels = minimumFontPixels;
            overlayShader = overlayFontShader != null ? overlayFontShader : Shader.Find("TextMeshPro/Distance Field Overlay");
            textRenderQueue = Mathf.Max(3101, paperMaterial != null ? paperMaterial.renderQueue + 1 : 3101);
            if (text == null)
            {
                var body = new GameObject("Text", typeof(TextMeshPro));
                body.transform.SetParent(transform, false);
                text = body.GetComponent<TextMeshPro>();
            }
            text.font = font;
            text.fontSharedMaterial = OverlayMaterial(fontMaterial != null ? fontMaterial : font != null ? font.material : text.fontSharedMaterial);
            text.fontStyle = FontStyles.Bold;
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.sortingOrder = 54;
            if (paper == null)
            {
                var back = new GameObject("Paper", typeof(MeshFilter), typeof(MeshRenderer));
                back.transform.SetParent(transform, false);
                paper = new Mesh { name = "Raid label paper" };
                back.GetComponent<MeshFilter>().sharedMesh = paper;
                paperRenderer = back.GetComponent<MeshRenderer>();
            }
            paperRenderer.sharedMaterial = paperMaterial;
            paperRenderer.sortingOrder = 53;
            paperRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            paperRenderer.receiveShadows = false;
            preferred = default; dirty = true;
        }

        public void Show(string value, Color ink, Color edge, bool dashedEdge = false)
        {
            if (text.text == value && textColor == ink && edgeColor == edge && dashed == dashedEdge && preferred.y > 0)
            {
                gameObject.SetActive(true); Refresh(); return;
            }
            textColor = ink; edgeColor = edge; dashed = dashedEdge;
            dirty = true;
            text.text = value;
            text.color = ink;
            preferred = text.GetPreferredValues(value);
            preferred.x = Mathf.Max(preferred.x, 0.2f); preferred.y = Mathf.Max(preferred.y, 0.1f);
            // Satır metriğinin tam sınırında kırpılmaya güvenmeyelim; kâğıt yine preferred boyutunda kalır.
            text.rectTransform.sizeDelta = preferred + new Vector2(2, 2);
            gameObject.SetActive(true);
            text.ForceMeshUpdate();
            RefreshSubmeshMaterials();
            glyphHeight = Mathf.Max(0.1f, text.textBounds.size.y);
            Refresh();
        }

        public void SetOpacity(float value) { if (Mathf.Approximately(opacity, value)) return; opacity = value; dirty = true; Refresh(); }
        public void SetPaperColor(Color value) { if (paperColor == value) return; paperColor = value; dirty = true; Refresh(); }

        void LateUpdate() { if (view != null && text != null) Refresh(); }

        void Refresh()
        {
            if (view == null || text == null || preferred.y <= 0) return;
            transform.rotation = view.transform.rotation;
            float canvasScale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            float pixelHeight = Mathf.Max(minimumPixels, pixels * canvasScale);
            float unitsPerPixel = view.orthographic ? 2 * view.orthographicSize / Mathf.Max(1, view.pixelHeight) : 0.012f;
            // TMP satır yüksekliği yerine gerçek glif sınırı: dünya yazısı gerçekten en az 11 piksel olsun.
            float scale = pixelHeight * unitsPerPixel / glyphHeight;
            transform.localScale = Vector3.one * scale;
            if (!dirty && Mathf.Approximately(lastPixelHeight, pixelHeight)) return;
            dirty = false; lastPixelHeight = pixelHeight;
            float onePixel = glyphHeight / pixelHeight;
            float x = preferred.x * 0.5f + onePixel * 5, y = preferred.y * 0.5f + onePixel * 3;
            var verts = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
            void Rect(float x0, float y0, float x1, float y1, float z, Color c)
            {
                int start = verts.Count; c.a *= opacity;
                verts.Add(new Vector3(x0, y0, z)); verts.Add(new Vector3(x1, y0, z)); verts.Add(new Vector3(x1, y1, z)); verts.Add(new Vector3(x0, y1, z));
                for (int i = 0; i < 4; i++) colors.Add(c);
                triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
            Rect(-x + onePixel, -y - onePixel, x + onePixel, y - onePixel, 0.04f, new Color(0, 0, 0, 0.28f));
            Rect(-x, -y, x, y, 0.025f, paperColor);
            float stroke = onePixel * 1.2f;
            if (dashed)
            {
                for (float t = -x; t < x; t += onePixel * 7)
                { float end = Mathf.Min(x, t + onePixel * 4); Rect(t, -y, end, -y + stroke, 0.015f, edgeColor); Rect(t, y - stroke, end, y, 0.015f, edgeColor); }
                for (float t = -y; t < y; t += onePixel * 7)
                { float end = Mathf.Min(y, t + onePixel * 4); Rect(-x, t, -x + stroke, end, 0.015f, edgeColor); Rect(x - stroke, t, x, end, 0.015f, edgeColor); }
            }
            else
            {
                Rect(-x, -y, x, -y + stroke, 0.015f, edgeColor); Rect(-x, y - stroke, x, y, 0.015f, edgeColor);
                Rect(-x, -y, -x + stroke, y, 0.015f, edgeColor); Rect(x - stroke, -y, x, y, 0.015f, edgeColor);
            }
            paper.Clear(); paper.SetVertices(verts); paper.SetColors(colors); paper.SetTriangles(triangles, 0); paper.RecalculateBounds();
            text.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a * opacity);
        }

        void OnDestroy()
        {
            if (paper != null) Destroy(paper);
            foreach (var material in overlayMaterials.Values) if (material != null) Destroy(material);
            overlayMaterials.Clear();
        }
    }
}
