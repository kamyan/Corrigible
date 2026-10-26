using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Odanın yazısız dokularını üretir (Docs/UI_Siginak/reference/scene_threejs/textures.js'in C# karşılığı): kalıp izli
    /// beton (arka duvar, yan duvar, zemin), linolyum, klavye, telsiz kadranı ve afiş piktogramı. Taban renkleri
    /// tokenlardan; leke, iz ve gürültü oranları JS'teki sabitlerdir. Rastgelelik sabit tohumlu, çıktı her çalıştırmada aynı.
    /// Yazılar dokuya basılmaz (TMP + çeviri). Menü: Corrigible › Sığınak › Dokuları üret.
    /// </summary>
    public static class SiginakTextureBaker
    {
        public static readonly string[] Outputs =
            { "tx_concrete_back", "tx_concrete_side", "tx_concrete_floor", "tx_lino", "tx_keyboard", "tx_radio_dial", "tx_poster_pictogram" };

        [MenuItem("Corrigible/Sığınak/Dokuları üret")]
        public static string Bake()
        {
            var t = SiginakTheme.Active;
            seed = 1234567;
            // room.js › wallTex, sideTex, floorTex
            Save("tx_concrete_back", Concrete(4.4f, 2.5f, 420, t.ColorOf("bunker.concrete"), true, 40,
                new[] { new Vector4(0.18f, 0.08f, 0.16f, 0.5f), new Vector4(0.75f, 0.05f, 0.22f, 0.45f), new Vector4(0.55f, 0.95f, 0.25f, 0.35f), new Vector4(0.05f, 0.6f, 0.12f, 0.35f) }, true));
            Save("tx_concrete_side", Concrete(3.0f, 2.5f, 420, t.ColorOf("bunker.concreteSide"), true, 40,
                new[] { new Vector4(0.5f, 0.1f, 0.25f, 0.4f) }, true));
            Save("tx_concrete_floor", Concrete(4.4f, 3.0f, 200, t.ColorOf("bunker.floor"), false, 0,
                new[] { new Vector4(0.5f, 0.5f, 0.4f, 0.3f) }, false));
            Save("tx_lino", Lino(t.ColorOf("materials.lino")));
            Save("tx_keyboard", Keyboard(t.ColorOf("materialsExtra.keyboard"), t.ColorOf("materialsExtra.keyboardKey")));
            Save("tx_radio_dial", RadioDial(t.ColorOf("light.radioDial")));
            Save("tx_poster_pictogram", Pictogram(t.ColorOf("materialsExtra.posterInk")));
            AssetDatabase.Refresh();
            foreach (var name in Outputs)
                AssetDatabase.ImportAsset(SiginakSpriteManifest.TexturesDir + "/" + name + ".png", ImportAssetOptions.ForceUpdate);
            return Outputs.Length + " doku üretildi";
        }

        // ---------------------------------------------------------------- lib.js › rnd()
        static long seed;
        static float Rnd() { seed = seed * 16807 % 2147483647; return (seed - 1) / 2147483646f; }

        /// <summary>Sol-üst orijinli RGBA tuval (JS canvas karşılığı); kaydederken satırlar ters çevrilir.</summary>
        sealed class Canvas
        {
            public readonly int W, H;
            public readonly Color[] Px;
            public Canvas(int w, int h, Color fill) { W = w; H = h; Px = new Color[w * h]; for (int i = 0; i < Px.Length; i++) Px[i] = fill; }

            public void Blend(int x, int y, Color c, float alpha)
            {
                if (x < 0 || y < 0 || x >= W || y >= H || alpha <= 0) return;
                int i = y * W + x;
                var d = Px[i];
                float a = Mathf.Clamp01(alpha);
                float outA = a + d.a * (1 - a);
                Px[i] = outA <= 0 ? Color.clear : new Color(
                    (c.r * a + d.r * d.a * (1 - a)) / outA, (c.g * a + d.g * d.a * (1 - a)) / outA, (c.b * a + d.b * d.a * (1 - a)) / outA, outA);
            }

            public void Rect(float x, float y, float w, float h, Color c, float alpha)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(x)), y0 = Mathf.Max(0, Mathf.FloorToInt(y));
                int x1 = Mathf.Min(W, Mathf.CeilToInt(x + w)), y1 = Mathf.Min(H, Mathf.CeilToInt(y + h));
                for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++) Blend(xx, yy, c, alpha);
            }

            public void Circle(float cx, float cy, float r, Color c, float alpha)
            {
                for (int yy = Mathf.FloorToInt(cy - r); yy <= Mathf.CeilToInt(cy + r); yy++)
                for (int xx = Mathf.FloorToInt(cx - r); xx <= Mathf.CeilToInt(cx + r); xx++)
                {
                    float d = Mathf.Sqrt((xx + 0.5f - cx) * (xx + 0.5f - cx) + (yy + 0.5f - cy) * (yy + 0.5f - cy));
                    Blend(xx, yy, c, alpha * Mathf.Clamp01(r - d + 0.5f));
                }
            }

            public void Ellipse(float cx, float cy, float rx, float ry, float rot, Color c, float alpha)
            {
                float r = Mathf.Max(rx, ry), cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
                for (int yy = Mathf.FloorToInt(cy - r); yy <= Mathf.CeilToInt(cy + r); yy++)
                for (int xx = Mathf.FloorToInt(cx - r); xx <= Mathf.CeilToInt(cx + r); xx++)
                {
                    float dx = xx + 0.5f - cx, dy = yy + 0.5f - cy;
                    float u = (dx * cs + dy * sn) / rx, v = (-dx * sn + dy * cs) / ry;
                    if (u * u + v * v <= 1) Blend(xx, yy, c, alpha);
                }
            }

            /// <summary>Radyal leke: merkezde α, %60'ta α·.45, kenarda 0 (textures.js › stains).</summary>
            public void Stain(float cx, float cy, float r, Color c, float alpha)
            {
                for (int yy = Mathf.Max(0, (int)(cy - r)); yy < Mathf.Min(H, (int)(cy + r) + 1); yy++)
                for (int xx = Mathf.Max(0, (int)(cx - r)); xx < Mathf.Min(W, (int)(cx + r) + 1); xx++)
                {
                    float t = Mathf.Sqrt((xx - cx) * (xx - cx) + (yy - cy) * (yy - cy)) / r;
                    if (t >= 1) continue;
                    float a = t < 0.6f ? Mathf.Lerp(alpha, alpha * 0.45f, t / 0.6f) : Mathf.Lerp(alpha * 0.45f, 0, (t - 0.6f) / 0.4f);
                    Blend(xx, yy, c, a);
                }
            }

            /// <summary>Dikey doğrusal geçiş: y0'da a0, y1'de a1 (y1 &lt; y0 olabilir).</summary>
            public void VerticalFade(float x, float w, float y0, float y1, Color c, float a0, float a1)
            {
                int top = Mathf.Max(0, (int)Mathf.Min(y0, y1)), bottom = Mathf.Min(H, (int)Mathf.Max(y0, y1) + 1);
                for (int yy = top; yy < bottom; yy++)
                {
                    float a = Mathf.Lerp(a0, a1, Mathf.InverseLerp(y0, y1, yy));
                    for (int xx = Mathf.Max(0, (int)x); xx < Mathf.Min(W, (int)(x + w) + 1); xx++) Blend(xx, yy, c, a);
                }
            }

            public void Noise(float amount)
            {
                for (int i = 0; i < Px.Length; i++)
                {
                    float n = (Rnd() - 0.5f) * amount / 255f;
                    var p = Px[i];
                    Px[i] = new Color(Mathf.Clamp01(p.r + n), Mathf.Clamp01(p.g + n), Mathf.Clamp01(p.b + n), p.a);
                }
            }

            /// <summary>Kalın çizgi: noktalara daire basarak (lineCap round).</summary>
            public void Stroke(Vector2[] points, float width, Color c)
            {
                for (int i = 0; i + 1 < points.Length; i++)
                {
                    float len = Vector2.Distance(points[i], points[i + 1]);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(len / 1.5f));
                    for (int s = 0; s <= steps; s++)
                    {
                        var p = Vector2.Lerp(points[i], points[i + 1], s / (float)steps);
                        Circle(p.x, p.y, width / 2, c, 1);
                    }
                }
            }
        }

        static Vector2[] Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, int n = 48)
        {
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n, u = 1 - t;
                pts[i] = u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
            }
            return pts;
        }

        static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

        // ---------------------------------------------------------------- textures.js › concreteTex
        static Canvas Concrete(float wM, float hM, int ppm, Color baseColor, bool ties, int streaks, Vector4[] stains, bool damp)
        {
            var x = new Canvas(Mathf.RoundToInt(wM * ppm), Mathf.RoundToInt(hM * ppm), baseColor);
            float board = 0.15f * ppm;
            for (float y = 0; y < x.H; y += board)
            {
                float v = (Rnd() - 0.5f) * 16;
                x.Rect(0, y, x.W, board, v > 0 ? Color.white : Color.black, Mathf.Abs(v) / 255f * 1.4f);
                x.Rect(0, y, x.W, 2, Rgb(20, 22, 18), 0.22f);
                x.Rect(0, y + 2, x.W, 1, Color.white, 0.05f);
            }
            if (ties)
                for (float y = 0.45f * ppm; y < x.H; y += 0.6f * ppm)
                for (float xx = 0.3f * ppm; xx < x.W; xx += 0.6f * ppm)
                {
                    x.Circle(xx, y, 0.011f * ppm, Rgb(25, 25, 22), 0.55f);
                    x.Circle(xx + 1, y + 1.5f, 0.011f * ppm, Color.white, 0.06f);
                }
            x.Noise(26);
            foreach (var s in stains) x.Stain(s.x * x.W, s.y * x.H, s.z * x.W, Rgb(30, 36, 28), s.w);
            if (damp) x.VerticalFade(0, x.W, x.H, x.H - 0.5f * ppm, Rgb(26, 30, 24), 0.55f, 0);
            for (int i = 0; i < streaks; i++)
            {
                float sx = Rnd() * x.W, sy = Rnd() * x.H * 0.5f, len = (0.2f + Rnd() * 0.8f) * x.H * 0.5f;
                x.VerticalFade(sx, 2 + Rnd() * 5, sy, sy + len, Rgb(28, 32, 26), 0.18f, 0);
            }
            return x;
        }

        // textures.js › linoTex (taban rengi token: materials.lino)
        static Canvas Lino(Color baseColor)
        {
            var x = new Canvas(1024, 1024, baseColor);
            for (int i = 0; i < 900; i++)
            {
                var c = Rnd() > 0.5f ? Color.white : Color.black;
                float a = 0.02f + Rnd() * 0.04f;
                x.Ellipse(Rnd() * 1024, Rnd() * 1024, 4 + Rnd() * 30, 2 + Rnd() * 10, Rnd() * 3, c, a);
            }
            x.Noise(14);
            return x;
        }

        // textures.js › keyboardTex
        static Canvas Keyboard(Color body, Color key)
        {
            var x = new Canvas(840, 300, body);
            for (int r = 0; r < 5; r++)
            for (int k = 0; k < 14; k++)
            {
                float kx = 24 + k * 57 + (r % 2) * 14, ky = 22 + r * 54;
                if (kx > 800) continue;
                x.Rect(kx, ky, 48, 44, key, 1);
                x.Rect(kx, ky + 38, 48, 6, Color.black, 0.25f);
            }
            return x;
        }

        // textures.js › radioDialTex (yazısız; rakamlar TMP)
        static Canvas RadioDial(Color dial)
        {
            var x = new Canvas(512, 96, dial);
            x.VerticalFade(0, 512, 0, 96, Rgb(184, 116, 42), 0, 1);
            for (int i = 0; i < 40; i++) x.Rect(20 + i * 12, 60, 2, i % 5 == 0 ? 28 : 14, Rgb(58, 36, 12), 1);
            x.Rect(300, 8, 4, 84, Rgb(154, 30, 20), 1);
            return x;
        }

        /// <summary>textures.js › posterTex piktogramı (fişi çekilmiş kablo), saydam zemin; afişin y 230–550 bandı.</summary>
        static Canvas Pictogram(Color ink)
        {
            var x = new Canvas(880, 320, Color.clear);
            const float oy = 230;
            Vector2 P(float px, float py) => new Vector2(px, py - oy);
            x.Stroke(Bezier(P(170, 300), P(260, 300), P(300, 380), P(360, 380)), 26, ink);
            x.Rect(350, 340 - oy, 90, 80, ink, 1); x.Rect(440, 352 - oy, 40, 14, ink, 1); x.Rect(440, 394 - oy, 40, 14, ink, 1);
            x.Stroke(Bezier(P(710, 300), P(620, 300), P(580, 380), P(540, 380)), 26, ink);
            x.Rect(520, 340 - oy, 30, 80, ink, 1);
            x.Stroke(new[] { P(470, 250), P(500, 300) }, 14, ink);
            x.Stroke(new[] { P(500, 470), P(470, 520) }, 14, ink);
            return x;
        }

        /// <summary>
        /// CRT geçiş camının köşe örtüsü (K5b › border-radius 74 / 64): 256 px, yarıçap 72 px dışı opak beyaz, içi saydam,
        /// 1,5 px yumuşak kenar. 9 dilim (kenar 96) olarak cam dikdörtgeninin üstüne çerçeve rengiyle gerilir.
        /// </summary>
        public static string GlassCover()
        {
            const int size = 256;
            const float r = 72;
            var c = new Canvas(size, size, Color.clear);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // En yakın köşe merkezine uzaklık (yalnız köşe bölgesinde)
                    float cx = x < r ? r : x >= size - r ? size - r : x + 0.5f;
                    float cy = y < r ? r : y >= size - r ? size - r : y + 0.5f;
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01((d - r) / 1.5f + 0.5f);
                    c.Px[y * size + x] = new Color(1, 1, 1, a);
                }
            Save("ui_crt_glass_cover", c, SiginakSpriteManifest.SpritesDir);
            return SiginakSpriteManifest.SpritesDir + "/ui_crt_glass_cover.png";
        }

        static void Save(string name, Canvas c) => Save(name, c, SiginakSpriteManifest.TexturesDir);

        static void Save(string name, Canvas c, string dir)
        {
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false);
            try
            {
                var flipped = new Color[c.Px.Length];
                for (int y = 0; y < c.H; y++) Array.Copy(c.Px, y * c.W, flipped, (c.H - 1 - y) * c.W, c.W);
                tex.SetPixels(flipped);
                tex.Apply();
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }
    }
}
