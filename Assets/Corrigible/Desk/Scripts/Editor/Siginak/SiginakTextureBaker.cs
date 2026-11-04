using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Game.Raid;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Odanın yazısız dokularını üretir (Docs/UI_Siginak/reference/scene_threejs/textures.js'in C# karşılığı): kalıp izli
    /// beton (arka duvar, yan duvar, zemin), linolyum, telsiz kadranı, afiş piktogramı ve merdiven fayansı. Taban renkleri
    /// tokenlardan; leke, iz ve gürültü oranları JS'teki sabitlerdir. Rastgelelik sabit tohumlu, çıktı her çalıştırmada aynı.
    /// Yazılar dokuya basılmaz (TMP + çeviri). Menü: Corrigible › Sığınak › Dokuları üret.
    /// </summary>
    public static class SiginakTextureBaker
    {
        public static readonly string[] Outputs =
            { "tx_concrete_back", "tx_concrete_side", "tx_concrete_floor", "tx_lino", "tx_radio_dial", "tx_poster_pictogram", "tx_stair_tiles" };

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
            Save("tx_radio_dial", RadioDial(t.ColorOf("light.radioDial")));
            Save("tx_poster_pictogram", Pictogram(t.ColorOf("materialsExtra.posterInk")));
            Save("tx_stair_tiles", StairTiles(t.ColorOf("door.stairTile")));
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

        /// <summary>Yalnız merdiven fayansını üretir (öteki dokulara dokunmaz).</summary>
        public static string BakeStairTiles()
        {
            Save("tx_stair_tiles", StairTiles(SiginakTheme.Active.ColorOf("door.stairTile")));
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(SiginakSpriteManifest.TexturesDir + "/tx_stair_tiles.png", ImportAssetOptions.ForceUpdate);
            return SiginakSpriteManifest.TexturesDir + "/tx_stair_tiles.png";
        }

        /// <summary>
        /// Merdiven boşluğunun fayansı (Revizyon 2 · S4d): 4 × 4 kare fayans (#DDE3DD), 3 px koyu derz, fayans başına hafif
        /// ton farkı ve kir. Malzeme metre başına 4 fayans olacak şekilde döşer.
        /// </summary>
        static Canvas StairTiles(Color tile)
        {
            const int size = 256, n = 4, grout = 3;
            // Kendi tohumuyla: tek başına üretilince de Bake'tekiyle aynı
            seed = 7654321;
            var x = new Canvas(size, size, new Color(tile.r * 0.62f, tile.g * 0.62f, tile.b * 0.62f, 1));
            for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                float k = 0.94f + Rnd() * 0.08f;
                var shade = new Color(tile.r * k, tile.g * k, tile.b * k, 1);
                x.Rect(c * 64 + grout, r * 64 + grout, 64 - grout, 64 - grout, shade, 1);
                // Fayansın üst kenarı ışığı yakalar
                x.Rect(c * 64 + grout, r * 64 + grout, 64 - grout, 2, Color.white, 0.18f);
            }
            for (int i = 0; i < 24; i++) x.Ellipse(Rnd() * size, Rnd() * size, 6 + Rnd() * 20, 4 + Rnd() * 10, Rnd() * 3, Color.black, 0.012f + Rnd() * 0.015f);
            x.Noise(6);
            return x;
        }

        // tex2.js › radioFace kadran penceresi (560 × 170; yazısız: kanal rakamları ve KANAL TMP)
        static Canvas RadioDial(Color dial)
        {
            var x = new Canvas(560, 170, Rgb(240, 189, 98));
            x.VerticalFade(0, 560, 0, 170, Rgb(196, 128, 47), 0, 1);
            for (int i = 0; i < 46; i++) x.Rect(24 + i * 11.6f, 106, 3, i % 5 == 0 ? 46 : 22, Rgb(58, 36, 12), 1);
            x.Rect(254, 10, 6, 150, Rgb(154, 30, 20), 1);
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

        /// <summary>
        /// Görev fişindeki ekip diski (Revizyon 2 · S2 › ekip diskleri): Ø 22 px, #C9A36B, 1,5 px kenar #5C4826; 2× çizilir
        /// (44 px), yarı boy gösterilir. Renkli sprite (boyanmaz); harfi TMP.
        /// </summary>
        public static string TeamDisc()
        {
            const int size = 44;
            var t = SiginakTheme.Active;
            var c = new Canvas(size, size, Color.clear);
            c.Circle(size / 2f, size / 2f, size / 2f - 0.5f, t.ColorOf("slip.teamDiscRim"), 1);
            c.Circle(size / 2f, size / 2f, size / 2f - 3.5f, t.ColorOf("slip.teamDisc"), 1);
            Save("ui_team_disc", c, SiginakSpriteManifest.SpritesDir);
            return SiginakSpriteManifest.SpritesDir + "/ui_team_disc.png";
        }

        /// <summary>İstasyon simgeleri: masadaki tuşların başlığı ve üst şeridin tuş kutuları (rayın sırası, en sonda AYARLAR).</summary>
        public static readonly string[] KeyIconNames = { "tx_key_shelter", "tx_key_missions", "tx_key_door", "tx_key_desk", "tx_key_settings" };

        /// <summary>
        /// İstasyon tuşlarının simgeleri (tuş adının yerine): 128 px, beyaz çizgi saydam zeminde (renk malzemeden ya da
        /// RawImage'dan), Revizyon 2 işaretlerinin çizgisi (resmin ~%72'si, kalınlık ~%8, yuvarlak uç). Zemin beyaz ve saydam:
        /// mip seviyelerinde kenar kararmaz. SIĞINAK yer altındaki odalar, GÖREVLER görev fişi, KAPI çarklı çelik kapı, MASA
        /// lambalı masa, AYARLAR dişli. Menü: Corrigible › Sığınak › Tuş simgelerini üret.
        /// </summary>
        [MenuItem("Corrigible/Sığınak/Tuş simgelerini üret")]
        public static string KeyIcons()
        {
            const float w = 10;
            var ink = Color.white;
            var blank = new Color(1, 1, 1, 0);

            var shelter = new Canvas(128, 128, blank);
            shelter.Stroke(new[] { new Vector2(14, 30), new Vector2(114, 30) }, w, ink);
            shelter.Stroke(new[] { new Vector2(42, 30), new Vector2(42, 48) }, w, ink);
            // Toprak katmanı: zeminle odalar arasında taramalı
            foreach (float x in new[] { 58f, 76f, 94f })
                shelter.Stroke(new[] { new Vector2(x, 42), new Vector2(x + 8, 36) }, w * 0.5f, ink);
            Outline(shelter, 26, 48, 102, 108, w, ink);
            shelter.Stroke(new[] { new Vector2(26, 78), new Vector2(102, 78) }, w, ink);
            shelter.Stroke(new[] { new Vector2(64, 48), new Vector2(64, 108) }, w, ink);

            var missions = new Canvas(128, 128, blank);
            Outline(missions, 30, 26, 98, 112, w, ink);
            missions.Rect(48, 16, 32, 20, ink, 1);
            foreach (float y in new[] { 56f, 74f, 92f }) missions.Stroke(new[] { new Vector2(46, y), new Vector2(82, y) }, w * 0.8f, ink);

            var door = new Canvas(128, 128, blank);
            Outline(door, 30, 14, 98, 114, w, ink);
            Ring(door, 64, 62, 17, w * 0.8f, ink);
            door.Stroke(new[] { new Vector2(52, 50), new Vector2(76, 74) }, w * 0.7f, ink);
            door.Stroke(new[] { new Vector2(76, 50), new Vector2(52, 74) }, w * 0.7f, ink);

            var desk = new Canvas(128, 128, blank);
            desk.Stroke(new[] { new Vector2(14, 72), new Vector2(114, 72) }, w, ink);
            desk.Stroke(new[] { new Vector2(26, 72), new Vector2(26, 112) }, w, ink);
            desk.Stroke(new[] { new Vector2(102, 72), new Vector2(102, 112) }, w, ink);
            desk.Stroke(new[] { new Vector2(40, 72), new Vector2(46, 36), new Vector2(70, 26) }, w * 0.8f, ink);
            desk.Stroke(new[] { new Vector2(64, 22), new Vector2(84, 46) }, w * 1.3f, ink);
            desk.Rect(30, 64, 22, 8, ink, 1);

            var settings = new Canvas(128, 128, blank);
            Ring(settings, 64, 64, 30, w, ink);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                settings.Stroke(new[] { new Vector2(64, 64) + dir * 36, new Vector2(64, 64) + dir * 46 }, w * 1.2f, ink);
            }
            Ring(settings, 64, 64, 10, w * 0.8f, ink);

            var all = new[] { shelter, missions, door, desk, settings };
            for (int i = 0; i < all.Length; i++) Save(KeyIconNames[i], all[i]);
            AssetDatabase.Refresh();
            foreach (var name in KeyIconNames)
                AssetDatabase.ImportAsset(SiginakSpriteManifest.TexturesDir + "/" + name + ".png", ImportAssetOptions.ForceUpdate);
            return KeyIconNames.Length + " simge üretildi";
        }

        static void Outline(Canvas c, float x0, float y0, float x1, float y1, float width, Color ink) =>
            c.Stroke(new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), new Vector2(x0, y0) }, width, ink);

        static void Ring(Canvas c, float cx, float cy, float r, float width, Color ink)
        {
            var points = new Vector2[49];
            for (int i = 0; i < points.Length; i++)
            {
                float a = i * 2 * Mathf.PI / (points.Length - 1);
                points[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            c.Stroke(points, width, ink);
        }

        /// <summary>Yumuşak kenarlı türevi üretilen 9 dilim zeminler ve çerçeveler (<see cref="SoftEdges"/>).</summary>
        public static readonly string[] SoftSources =
            { "ui_fill_r0", "ui_fill_r2", "ui_fill_r3", "ui_frame_1px_r2", "ui_frame_1_5px_r2", "ui_frame_2px_r3", "ui_frame_3px_r0", "ui_frame_double_6px",
              "ui_frame_dashed_2px" };
        /// <summary>İnce çizgilerin yumuşak kenarlı sprite'ları: yatay ve dikey düz çizgi, kesik çizgi (<see cref="SoftEdges"/>).</summary>
        public static readonly string[] SoftLines = { "ui_line_h_aa", "ui_line_v_aa", "ui_dash_h_aa" };

        /// <summary>
        /// Yumuşak kenarlı 9 dilim türevleri (<c>*_aa</c>): paketteki düz zemin ve çerçevenin çevresine 1 px saydam pay (dilim kenarı
        /// tabloda +1). Eğik kâğıtlarda (görev fişi −0,6°, emir belgesi +0,8°, eğitim notu −1,5°…) dokunun kenarına kadar opak
        /// sprite'ın kenarını üçgenin kendisi çizer ve ekran katmanı kenar yumuşatmadığı için her ~70 px'de bir piksellik basamak
        /// kalır (koyu düğmede "kırık kenar"); payla kenarı doku süzgeci çizer, eğik kenar düz görünür. Görsel 1 px içeride biter.
        /// Paket yeniden içe aktarılınca yeniden çalıştır. Menü: Corrigible › Sığınak › Yumuşak kenarlı sprite'ları üret.
        /// </summary>
        [MenuItem("Corrigible/Sığınak/Yumuşak kenarlı sprite'ları üret")]
        public static string SoftEdges()
        {
            foreach (var name in SoftSources)
            {
                var src = SiginakSpriteManifest.Get(name);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!tex.LoadImage(File.ReadAllBytes(src.AssetPath))) throw new IOException("okunamadı: " + src.AssetPath);
                    int w = tex.width, h = tex.height;
                    // GetPixels alttan üste; tuval üstten alta (Save çevirir)
                    var px = tex.GetPixels();
                    var c = new Canvas(w + 2, h + 2, Color.clear);
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                            c.Px[(h - y) * c.W + x + 1] = px[y * w + x];
                    Save(name + "_aa", c, SiginakSpriteManifest.SpritesDir);
                }
                finally { UnityEngine.Object.DestroyImmediate(tex); }
            }
            // İnce çizgiler (1–2 px kurallar, ayırıcılar, kesik ayraçlar): çizgi kalınlığına 1 px pay sığmaz (dilim kenarı çizgiden
            // kalın olur); çizgi ortada, iki yanında 1 px saydam satır, çizginin görseli kurala 2 px kalın bir çocukta çizilir
            var lineH = new Canvas(4, 3, Color.clear);
            for (int x = 0; x < 4; x++) lineH.Px[1 * 4 + x] = Color.white;
            Save("ui_line_h_aa", lineH, SiginakSpriteManifest.SpritesDir);
            var lineV = new Canvas(3, 4, Color.clear);
            for (int y = 0; y < 4; y++) lineV.Px[y * 3 + 1] = Color.white;
            Save("ui_line_v_aa", lineV, SiginakSpriteManifest.SpritesDir);
            var dash = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                var src = SiginakSpriteManifest.Get("ui_dash_h");
                if (!dash.LoadImage(File.ReadAllBytes(src.AssetPath))) throw new IOException("okunamadı: " + src.AssetPath);
                int w = dash.width, dh = dash.height;
                var px = dash.GetPixels();
                var c = new Canvas(w, dh + 2, Color.clear);
                for (int y = 0; y < dh; y++)
                    for (int x = 0; x < w; x++)
                        c.Px[(dh - y) * w + x] = px[y * w + x];
                Save("ui_dash_h_aa", c, SiginakSpriteManifest.SpritesDir);
            }
            finally { UnityEngine.Object.DestroyImmediate(dash); }
            AssetDatabase.Refresh();
            foreach (var name in SoftSources.Select(n => n + "_aa").Concat(SoftLines))
                AssetDatabase.ImportAsset(SiginakSpriteManifest.SpritesDir + "/" + name + ".png", ImportAssetOptions.ForceUpdate);
            return SoftSources.Length + SoftLines.Length + " yumuşak kenarlı sprite üretildi";
        }

        const string LibraryPath = "Assets/Corrigible/Settings/RaidPieceLibrary.asset";

        /// <summary>
        /// Baskın parçalarının küçük resimleri: ad, kütüphanedeki parça, boyut (px), çerçeve (ortografik yarı yükseklik; 0 = parçaya
        /// oturur) ve bakış noktasının yüksekliği.
        /// </summary>
        static readonly (string name, Func<RaidPieceLibrary, RaidPiece> piece, int w, int h, float size, float lookY)[] PieceThumbSpecs =
        {
            // İkmal sandığı (hedef listesi ve inceleme kartı): düğüm dolabının küçük resmiyle aynı ölçek ve çerçeve
            ("thumb_crate", l => l.lootCrate, 92, 116, 0.76f, 0.32f),
            // İnceleme kartının resim kutusu 40 × 46 px, 2× çizilir; parça çerçeveyi doldurur
            ("thumb_tube", l => l.tube, 80, 92, 0, 0),
            ("thumb_weld", l => l.weldArm, 80, 92, 0, 0),
            ("thumb_debris", l => l.debris, 80, 92, 0, 0),
            ("thumb_civ", l => l.civilian, 80, 92, 0, 0),
            ("thumb_spawner", l => l.spawner, 80, 92, 0, 0),
        };

        /// <summary>Parça küçük resimlerinin adları (sprite tablosunda üretilenler; baskın kartları kayıtta adla arar).</summary>
        public static string[] PieceThumbNames => PieceThumbSpecs.Select(s => s.name).ToArray();

        /// <summary>
        /// Baskın parçalarının küçük resimleri: ikmal sandığı (<c>thumb_crate</c>, 92 × 116), basınçlı tüp, kaynak kolu kaidesi, tavan
        /// enkazı, sivil sunucu ve kopya dolabı (<c>thumb_tube</c>, <c>thumb_weld</c>, <c>thumb_debris</c>, <c>thumb_civ</c>,
        /// <c>thumb_spawner</c>; 80 × 92). Prefab'lar <see cref="Isolated"/> ile baskının açısından ve ışığıyla çizilir. Parça
        /// değişince yeniden çalıştır; kütüphanede atanmamış parça atlanır.
        /// </summary>
        [MenuItem("Corrigible/Sığınak/Baskın parçalarının küçük resimlerini üret")]
        public static string PieceThumbs()
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);
            if (lib == null) return "parça kütüphanesi yok";
            var made = new List<string>();
            var missing = new List<string>();
            foreach (var s in PieceThumbSpecs)
            {
                var piece = s.piece(lib);
                if (piece == null) { missing.Add(s.name); continue; }
                Save(s.name, Isolated(s.w, s.h, s.size, Vector3.up * s.lookY, new[] { (piece.gameObject, Vector3.zero) }), SiginakSpriteManifest.SpritesDir);
                made.Add(s.name);
            }
            AssetDatabase.Refresh();
            foreach (var name in made)
                AssetDatabase.ImportAsset(SiginakSpriteManifest.SpritesDir + "/" + name + ".png", ImportAssetOptions.ForceUpdate);
            return made.Count + " küçük resim üretildi" + (missing.Count > 0 ? "; parçası yok: " + string.Join(", ", missing) : "");
        }

        /// <summary>
        /// Denetim çizimi (ör. <c>Temp/…png</c>): prefab'lar verilen konumlarda (kare merkezi, y = 0 zemin) aynı açı ve ışıkla, düz
        /// zemin rengi üstünde. size ≤ 0 ise çerçeve parçalara oturur. Yazdığı dosyanın tam yolunu döndürür.
        /// </summary>
        public static string RenderPieces(string path, int w, int h, float size, Vector3 look, Color background, params (GameObject prefab, Vector3 at)[] pieces)
        {
            var c = Isolated(w, h, size, look, pieces);
            for (int i = 0; i < c.Px.Length; i++) { var p = c.Px[i]; c.Px[i] = Color.Lerp(background, new Color(p.r, p.g, p.b, 1), p.a); }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            WritePng(path, c);
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Parçaları baskın kamerasının açısından (35° / 45° ortografik) ve baskının ışığıyla (masa lambası + soğuk dolgu, üç renkli
        /// ortam) yalıtılmış katmanda çizer; etiketleri (Labels) çizmez. Saydamlık siyah ve beyaz zeminde iki çizimin farkından çıkar
        /// (boru hattından bağımsız); ss × ss kutu süzgeciyle küçültülür. size ≤ 0: çerçeve görünen mesh köşelerine oturur (%10 pay),
        /// bakış noktası onların ortası; değilse bakış noktası kökten <paramref name="look"/>. Sahnedeki ışıklar çizim boyunca
        /// kapatılır, ortam ışığı geri yüklenir.
        /// </summary>
        static Canvas Isolated(int w, int h, float size, Vector3 look, IEnumerable<(GameObject prefab, Vector3 at)> pieces)
        {
            const int ss = 4, layer = 31;
            var root = new GameObject("Piece thumb") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(0, -500, 0);
            foreach (var (prefab, at) in pieces)
            {
                var piece = UnityEngine.Object.Instantiate(prefab, root.transform.position + at, Quaternion.identity, root.transform);
                var labels = piece.transform.Find("Labels");
                if (labels != null) labels.gameObject.SetActive(false);
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.transform.SetParent(root.transform, false);
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(35, 45, 0);
            if (size <= 0) (size, look) = Fit(root.transform, cam.transform, (float)w / h);
            cam.orthographicSize = size;
            cam.transform.position = root.transform.position + look - cam.transform.forward * 10;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 50;
            cam.cullingMask = 1 << layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            Light Lamp(Color c, float intensity, Vector3 euler)
            {
                var l = new GameObject("Light").AddComponent<Light>();
                l.transform.SetParent(root.transform, false);
                l.type = LightType.Directional; l.color = c; l.intensity = intensity; l.transform.rotation = Quaternion.Euler(euler);
                l.cullingMask = 1 << layer; l.shadows = LightShadows.None;
                return l;
            }
            // Baskin sahnesinin ışığı: Desk Lamp ve Cool Fill, üç renkli ortam
            Lamp(new Color(1f, 0.89f, 0.69f), 1.15f, new Vector3(50, 45, 0));
            Lamp(new Color(0.62f, 0.71f, 0.78f), 0.35f, new Vector3(40, -135, 0));
            var others = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.transform.root != root.transform && l.enabled).ToList();
            foreach (var l in others) l.enabled = false;
            var ambient = (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.218f, 0.247f, 0.275f);
            RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
            RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
            var rt = RenderTexture.GetTemporary(w * ss, h * ss, 24, RenderTextureFormat.ARGB32);
            var read = new Texture2D(w * ss, h * ss, TextureFormat.RGBA32, false);
            try
            {
                Color[] Render(Color bg)
                {
                    cam.backgroundColor = bg;
                    cam.targetTexture = rt;
                    cam.Render();
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    read.ReadPixels(new Rect(0, 0, w * ss, h * ss), 0, 0);
                    read.Apply();
                    RenderTexture.active = prev;
                    return read.GetPixels();
                }
                var black = Render(Color.black);
                var white = Render(Color.white);
                // Siyah ve beyaz zemin farkından alfa; renk siyah zemindekinin alfaya bölümü; ss × ss kutu süzgeciyle küçültülür
                var c = new Canvas(w, h, Color.clear);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float a = 0; var col = Color.black;
                        for (int j = 0; j < ss; j++)
                            for (int i = 0; i < ss; i++)
                            {
                                int k = (y * ss + j) * w * ss + x * ss + i;
                                var b = black[k]; var wh = white[k];
                                float pa = Mathf.Clamp01(1 - ((wh.r - b.r) + (wh.g - b.g) + (wh.b - b.b)) / 3f);
                                a += pa; col += new Color(b.r, b.g, b.b, 0);
                            }
                        a /= ss * ss;
                        col /= ss * ss;
                        // GetPixels alttan üste; tuval üstten alta
                        c.Px[(h - 1 - y) * w + x] = a > 0.001f ? new Color(Mathf.Clamp01(col.r / a), Mathf.Clamp01(col.g / a), Mathf.Clamp01(col.b / a), a) : Color.clear;
                    }
                return c;
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(read);
                UnityEngine.Object.DestroyImmediate(root);
                foreach (var l in others) if (l != null) l.enabled = true;
                (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor) = ambient;
            }
        }

        /// <summary>Çerçeve: etkin mesh köşelerinin kamera düzlemindeki kutusu, %10 payla; bakış noktası kutunun ortası (kökten).</summary>
        static (float size, Vector3 look) Fit(Transform root, Transform cam, float aspect)
        {
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            foreach (var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var r = f.GetComponent<MeshRenderer>();
                if (f.sharedMesh == null || r == null || !r.enabled || f.GetComponent<TMPro.TMP_Text>() != null) continue;
                foreach (var v in f.sharedMesh.vertices)
                {
                    var d = f.transform.TransformPoint(v) - root.position;
                    float x = Vector3.Dot(d, cam.right), y = Vector3.Dot(d, cam.up);
                    x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
                }
            }
            if (x0 > x1) return (0.76f, Vector3.up * 0.32f);
            float half = Mathf.Max((y1 - y0) / 2, (x1 - x0) / 2 / aspect) * 1.1f;
            return (half, cam.right * ((x0 + x1) / 2) + cam.up * ((y0 + y1) / 2));
        }

        static void Save(string name, Canvas c) => Save(name, c, SiginakSpriteManifest.TexturesDir);

        static void Save(string name, Canvas c, string dir)
        {
            Directory.CreateDirectory(dir);
            WritePng(dir + "/" + name + ".png", c);
        }

        static void WritePng(string path, Canvas c)
        {
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false);
            try
            {
                var flipped = new Color[c.Px.Length];
                for (int y = 0; y < c.H; y++) Array.Copy(c.Px, y * c.W, flipped, (c.H - 1 - y) * c.W, c.W);
                tex.SetPixels(flipped);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }
    }
}
