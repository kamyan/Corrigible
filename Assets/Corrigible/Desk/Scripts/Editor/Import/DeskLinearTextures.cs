using System.IO;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Linear renk uzayı telafisi (KARARLAR F3/F4): alfası dokuya gömülü yarı saydam desenler Linear karışımda açık kalır ve
    /// renk tonuyla 1'in üstüne çıkarılamaz. Bu araç kaynak dokudan, her pikselin alfasını sRGB karışımına eşdeğer Linear
    /// alfaya çeviren bir kardeş doku üretir (<c>…_linear.png</c>). Şimdilik: C4 “yalan payı” taraması (kâğıt ≈ 0,94 üstünde).
    /// </summary>
    public static class DeskLinearTextures
    {
        static readonly (string source, float background)[] Jobs = { ("tx_hatch_red", 0.94f) };

        [MenuItem("Corrigible/Desk UI/Linear telafili dokuları üret")]
        public static void Build()
        {
            foreach (var (name, bg) in Jobs)
            {
                var src = Path.Combine(DeskSpriteManifest.TexturesDir, name + ".png");
                var dst = Path.Combine(DeskSpriteManifest.TexturesDir, name + "_linear.png");
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(File.ReadAllBytes(src));
                var px = tex.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    var c = (Color)px[i];
                    if (c.a <= 0 || c.a >= 1) continue;
                    c.a = DeskMath.LinearOverlayAlpha(c, c.a, bg);
                    px[i] = c;
                }
                tex.SetPixels32(px);
                File.WriteAllBytes(dst, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            }
            DeskSpriteRegistry.Build();
        }
    }
}
