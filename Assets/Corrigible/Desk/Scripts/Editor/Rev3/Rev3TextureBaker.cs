using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Revizyon 3 dokuları (IMPLEMENTATION_MAP › Soru 20): paketin three.js sahnesi dokuları kodla çiziyor
    /// (<c>threejs/r3/prints.js</c>, <c>dress_room_*.js</c>); burada aynı tarifle C#'ta üretilir ya da projedeki Revizyon 2
    /// dokusundan türetilir. Çıktı <c>Art/Rev3/Textures/&lt;tx_ad&gt;.png</c>; <see cref="Rev3MaterialSetup"/> malzeme anahtarının
    /// doku notundaki adla bulup bağlar. Kelime yoktur (yazılar TMP). Rastgelelik sabit tohumla, çıktı her seferinde aynı.
    /// </summary>
    public static class Rev3TextureBaker
    {
        public const string Dir = Rev3ModelImporter.ArtDir + "/Textures";
        const string SiginakSprites = DeskSpriteManifest.Root + "/Art/Siginak/Sprites";

        [MenuItem("Corrigible/Revizyon 3/Dokuları üret")]
        static void BakeMenu() => Debug.Log("[Rev3] " + BakeAll());

        public static string BakeAll()
        {
            Directory.CreateDirectory(Dir);
            seed = 20261106;
            // Sayaç tekeri: dress_room_desk.js › digitStrip() tarifiyle, rakamlar Courier Prime Bold atlasından
            Save("tx_counterDigits", DigitStrip(false), false);
            Save("tx_counterDigitsInv", DigitStrip(true), false);
            Save("tx_chalkTally", ChalkTally(), false);
            Save("tx_poster", Poster(), false);
            Save("tx_coffeeRing", CoffeeRing(), false);
            Save("tx_drip", Drip(), false);
            Save("tx_hazard", Hazard(), true);
            Save("tx_dialScale", DialScale(), false);
            Save("tx_meterFace", MeterFace(), false);
            CopyTexture(SiginakSprites + "/map_stamp_free.png", "tx_stampRing");
            foreach (var key in KeyIcons) Save("tx_keyIcon_" + key, KeyIconTile(key), false);
            AssetDatabase.Refresh();
            foreach (var name in new[] { "tx_counterDigits", "tx_counterDigitsInv", "tx_chalkTally", "tx_poster", "tx_coffeeRing", "tx_drip", "tx_hazard", "tx_stampRing", "tx_dialScale", "tx_meterFace" })
                Import(name, name == "tx_hazard");
            foreach (var key in KeyIcons) Import("tx_keyIcon_" + key, false);
            return "Revizyon 3 dokuları üretildi → " + Dir;
        }

        // ---------------------------------------------------------------- tuval (sol-üst orijin, kayıtta satırlar çevrilir)
        static long seed;
        static float Rnd() { seed = seed * 16807 % 2147483647; return (seed - 1) / 2147483646f; }

        sealed class Canvas
        {
            public readonly int W, H;
            public readonly Color[] Px;
            public Canvas(int w, int h, Color fill) { W = w; H = h; Px = new Color[w * h]; for (int i = 0; i < Px.Length; i++) Px[i] = fill; }

            public void Blend(int x, int y, Color c, float a)
            {
                if (x < 0 || y < 0 || x >= W || y >= H || a <= 0) return;
                int i = y * W + x;
                var d = Px[i];
                a = Mathf.Clamp01(a * c.a);
                float outA = a + d.a * (1 - a);
                if (outA <= 0) { Px[i] = new Color(0, 0, 0, 0); return; }
                Px[i] = new Color((c.r * a + d.r * d.a * (1 - a)) / outA, (c.g * a + d.g * d.a * (1 - a)) / outA, (c.b * a + d.b * d.a * (1 - a)) / outA, outA);
            }

            public void Rect(float x, float y, float w, float h, Color c, float a = 1)
            {
                for (int j = Mathf.FloorToInt(y); j < Mathf.CeilToInt(y + h); j++)
                    for (int i = Mathf.FloorToInt(x); i < Mathf.CeilToInt(x + w); i++) Blend(i, j, c, a);
            }

            /// <summary>Yuvarlak uçlu kalın çizgi (kenarı 1 px yumuşatılmış).</summary>
            public void Line(Vector2 p, Vector2 q, float width, Color c, float a = 1)
            {
                float r = width / 2;
                int x0 = Mathf.FloorToInt(Mathf.Min(p.x, q.x) - r - 1), x1 = Mathf.CeilToInt(Mathf.Max(p.x, q.x) + r + 1);
                int y0 = Mathf.FloorToInt(Mathf.Min(p.y, q.y) - r - 1), y1 = Mathf.CeilToInt(Mathf.Max(p.y, q.y) + r + 1);
                var d = q - p; float len2 = Mathf.Max(d.sqrMagnitude, 1e-6f);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        var v = new Vector2(x + 0.5f, y + 0.5f) - p;
                        float t = Mathf.Clamp01(Vector2.Dot(v, d) / len2);
                        float dist = (v - d * t).magnitude;
                        float cover = Mathf.Clamp01(r + 0.5f - dist);
                        if (cover > 0) Blend(x, y, c, a * cover);
                    }
            }

            public void Polyline(Vector2[] pts, float width, Color c, float a = 1)
            {
                for (int i = 1; i < pts.Length; i++) Line(pts[i - 1], pts[i], width, c, a);
            }

            public void Arc(Vector2 center, float radius, float from, float to, float width, Color c, float a = 1)
            {
                int n = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(to - from) * radius / 3));
                var pts = new Vector2[n + 1];
                for (int i = 0; i <= n; i++) { float t = Mathf.Lerp(from, to, i / (float)n); pts[i] = center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * radius; }
                Polyline(pts, width, c, a);
            }

            public static Vector2[] Bezier(Vector2 p0, Vector2 c1, Vector2 c2, Vector2 p1, int n = 24)
            {
                var pts = new Vector2[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n, u = 1 - t;
                    pts[i] = u * u * u * p0 + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * p1;
                }
                return pts;
            }

            public Texture2D ToTexture()
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                var flipped = new Color[Px.Length];
                for (int y = 0; y < H; y++) Array.Copy(Px, y * W, flipped, (H - 1 - y) * W, W);
                tex.SetPixels(flipped);
                tex.Apply();
                return tex;
            }
        }

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static void Save(string name, Canvas canvas, bool repeat)
        {
            var tex = canvas.ToTexture();
            File.WriteAllBytes(Dir + "/" + name + ".png", tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void CopyTexture(string source, string name) => File.Copy(source, Dir + "/" + name + ".png", true);

        static void Import(string name, bool repeat)
        {
            string path = Dir + "/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            if (ti == null) return;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.npotScale = TextureImporterNPOTScale.None;   // 128 × 1408 şerit 1024'e örneklenmesin (hücreler korunur ama çözünürlük düşer)
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }

        // ---------------------------------------------------------------- dokular

        /// <summary>
        /// dress_room_desk.js › digitStrip(inv): 128 × 1408, 11 hücre (0–9 + ikinci 0, 0 üstte), koyu zeminde krem rakam
        /// (ters: açık zeminde koyu), 150 px Courier Prime 700 hücrenin ortasında, iki kenarda 5 px gölge bandı. Rakam şekli
        /// projenin CP-Bold SDF atlasından örneklenir (uzaklık alanı, kenar 0,5).
        /// </summary>
        static Canvas DigitStrip(bool inverse)
        {
            const int W = 128, Cell = 128;
            var canvas = new Canvas(W, Cell * 11, Hex(inverse ? "#E9DDBE" : "#1C150E"));
            var ink = Hex(inverse ? "#1C150E" : "#EFDCAE");
            var font = DeskTheme.Active.Font(DeskFont.CpBold);
            var atlas = ReadableAtlas(font.atlasTexture);
            int aw = atlas.width, ah = atlas.height;
            var ap = atlas.GetPixels();
            float sample(float x, float y)   // atlas pikseli (alt-sol orijin), çift doğrusal
            {
                x = Mathf.Clamp(x - 0.5f, 0, aw - 1.001f); y = Mathf.Clamp(y - 0.5f, 0, ah - 1.001f);
                int x0 = (int)x, y0 = (int)y; float fx = x - x0, fy = y - y0;
                float a = ap[y0 * aw + x0].a, b = ap[y0 * aw + x0 + 1].a, c = ap[(y0 + 1) * aw + x0].a, d = ap[(y0 + 1) * aw + x0 + 1].a;
                return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
            }
            float scale = 150f / font.faceInfo.pointSize;          // atlas pikseli → tuval pikseli
            float spread = font.atlasPadding;                      // SDF yayılımı (atlas pikseli)
            for (int k = 0; k < 11; k++)
            {
                var g = font.characterLookupTable[(uint)('0' + k % 10)].glyph;
                var m = g.metrics; var r = g.glyphRect;
                // Yatay: ilerleme kutusu ortada; dikey: rakamın kutusu hücrenin ortasında (canvas 'middle' + 70 ≈ görsel orta)
                float left = W / 2f - m.horizontalAdvance * scale / 2 + m.horizontalBearingX * scale;
                float top = k * Cell + Cell / 2f - m.height * scale / 2 + 3;
                int pad = Mathf.CeilToInt(spread * scale) + 1;
                for (int py = Mathf.FloorToInt(top) - pad; py <= Mathf.CeilToInt(top + m.height * scale) + pad; py++)
                    for (int px = Mathf.FloorToInt(left) - pad; px <= Mathf.CeilToInt(left + m.width * scale) + pad; px++)
                    {
                        if (px < 0 || px >= W || py < k * Cell || py >= (k + 1) * Cell) continue;
                        // tuval (üst-sol) → glif kutusu (alt-sol) → atlas
                        float gx = (px + 0.5f - left) / scale, gy = m.height - (py + 0.5f - top) / scale;
                        float a = sample(r.x + gx, r.y + gy);
                        float dist = (a - 0.5f) * 2 * spread * scale;   // tuval pikseli, içeride artı
                        float cover = Mathf.Clamp01(dist + 0.5f);
                        if (cover > 0) canvas.Blend(px, py, ink, cover);
                    }
            }
            var rim = new Color(0, 0, 0, 1);
            canvas.Rect(0, 0, 5, canvas.H, rim, 0.35f);
            canvas.Rect(W - 5, 0, 5, canvas.H, rim, 0.35f);
            UnityEngine.Object.DestroyImmediate(atlas);
            return canvas;
        }

        /// <summary>R11 asetatlarının kelimesiz tarama ve kenar baskısı; renkler model malzemesinden gelir.</summary>
        public static string BakeStage9()
        {
            var lib=AssetDatabase.LoadAssetAtPath<Rev3MaterialLibrary>(Rev3MaterialSetup.LibraryPath);
            foreach(bool hit in new[]{false,true})
            {
                var color=lib.Get(hit?"rp_acetateHit":"rp_acetateMove").GetColor("_BaseColor");color.a=1;
                var c=new Canvas(256,256,Color.clear);
                c.Rect(0,0,256,256,color,hit?.12f:.16f);
                int step=hit?22:29;
                for(int n=0;n<256;n+=step)
                    c.Line(hit?new Vector2(n,0):new Vector2(0,n),hit?new Vector2(n,256):new Vector2(256,n),hit?3.5f:3,color,hit?.46f:.34f);
                int edge=18;c.Polyline(new[]{new Vector2(edge,edge),new Vector2(256-edge,edge),new Vector2(256-edge,256-edge),new Vector2(edge,256-edge),new Vector2(edge,edge)},hit?4.6f:3.8f,color,.85f);
                string name=hit?"tx_rev3_raid_hit":"tx_rev3_raid_move";Save(name,c,false);AssetDatabase.ImportAsset(Dir+"/"+name+".png",ImportAssetOptions.ForceUpdate);Import(name,false);
            }
            return "R11 tarama asetatları üretildi";
        }

        /// <summary>R11 kâğıdının kelimesiz kesik kenarı ve 24 px ızgarası; metinler TMP kalır.</summary>
        public static string BakeRaidPaper()
        {
            Directory.CreateDirectory(Dir);
            var paper=Hex("#D7D5C9");var ink=Hex("#2C4C8E");
            var edge=new Canvas(96,96,Color.clear);
            for(int y=0;y<96;y++)for(int x=0;x<96;x++)
            {
                int left=2+(y/11%3),right=94-(y/17%3),bottom=2+(x/13%3),top=94-(x/19%3);
                if(x>=left&&x<right&&y>=bottom&&y<top)edge.Blend(x,y,paper,1);
            }
            var grid=new Canvas(24,24,Color.clear);grid.Line(new Vector2(0,0),new Vector2(24,0),1,ink);grid.Line(new Vector2(0,0),new Vector2(0,24),1,ink);
            Save("tx_rev3_raid_paper",edge,false);Save("tx_rev3_raid_grid",grid,true);
            foreach(string n in new[]{"tx_rev3_raid_paper","tx_rev3_raid_grid"})
            {
                string path=Dir+"/"+n+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var ti=(TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=100;
                ti.spriteBorder=n.EndsWith("paper")?Vector4.one*12:Vector4.zero;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;
                ti.textureCompression=TextureImporterCompression.Uncompressed;ti.wrapMode=n.EndsWith("grid")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;ti.SaveAndReimport();
            }
            return "R11 kâğıt kenarı ve ızgarası hazır";
        }

        public static string BakeStage8()
        {
            var theme=SiginakTheme.Active;
            var grid=new Canvas(512,1024,Color.white);
            Color lines=theme.ColorOf("menu.cardGrid"),ink=theme.ColorOf("menu.cardNumbers");
            float px=512f/90f,py=1024f/184f;
            grid.Rect(5*px,60*py,80*px,1.5f*py,lines);
            for(int c=0;c<=10;c++)grid.Rect((7+c*7.6f)*px,64.75f*py,.18f*px,84*py,lines,.65f);
            for(int r=0;r<=8;r++)grid.Rect(7*px,(64.75f+r*10.5f)*py,76*px,.18f*py,lines,.65f);
            var strip=DigitStrip(true);var bg=Hex("#E9DDBE");var dark=Hex("#1C150E");
            for(int n=1;n<=80;n++)
            {
                string text=n.ToString();float x=(10.8f+(n-1)%10*7.6f)*px-text.Length*7;
                float y=(65.5f+(n-1)/10*10.5f)*py;
                foreach(char ch in text)
                {
                    int digit=ch-'0';
                    for(int yy=0;yy<22;yy++)for(int xx=0;xx<14;xx++)
                    {
                        var color=strip.Px[(digit*128+Mathf.Clamp((int)((yy+.5f)/22*128),0,127))*128+Mathf.Clamp((int)((xx+.5f)/14*128),0,127)];
                        float a=Mathf.Clamp01((bg.r-color.r)/(bg.r-dark.r));grid.Blend((int)x+xx,(int)y+yy,ink,a);
                    }
                    x+=14;
                }
            }
            Save("tx_shiftCardGrid",grid,false);
            var tile=new Canvas(256,256,Color.white);var grout=theme.ColorOf("menu.tileGrout");
            tile.Rect(0,0,4,256,grout);tile.Rect(0,0,256,4,grout);
            tile.Rect(5,5,1,250,Color.white,.4f);
            var normal=DigitStrip(false);var drum=new Canvas(1280,256,Hex("#1C150E"));
            for(int d=0;d<10;d++)for(int y=0;y<256;y++)for(int x=0;x<128;x++)
            {
                float sx=(y-128)/1.2f+64,sy=-(x-64)/1.2f+64;
                if(sx<0||sx>=128||sy<0||sy>=128)continue;
                drum.Px[y*1280+d*128+x]=normal.Px[(d*128+(int)sy)*128+(int)sx];
            }
            Save("tx_menuDrums",drum,true);
            var dial=new Canvas(512,512,Color.white);var darkInk=theme.ColorOf("ink");
            for(int mark=0;mark<60;mark++)
            {
                float a=mark*Mathf.PI/30;var v=new Vector2(Mathf.Sin(a),-Mathf.Cos(a));
                dial.Line(new Vector2(256,256)+v*(mark%5==0?203:218),new Vector2(256,256)+v*235,mark%5==0?5:2,darkInk);
            }
            for(int n=1;n<=12;n++)
            {
                float a=n*Mathf.PI/6;var v=new Vector2(Mathf.Sin(a),-Mathf.Cos(a));Number(dial,strip,n.ToString(),256+v.x*174,256+v.y*174-17,34,darkInk);
            }
            Save("tx_clockFace",dial,false);
            var gauge=new Canvas(512,350,Color.white);
            gauge.Arc(new Vector2(256,310),245,Mathf.PI*1.15f,Mathf.PI*1.85f,5,darkInk);
            for(int n=0;n<=10;n++)
            {
                float a=Mathf.Lerp(Mathf.PI*1.15f,Mathf.PI*1.85f,n/10f);var v=new Vector2(Mathf.Cos(a),Mathf.Sin(a));gauge.Line(new Vector2(256,310)+v*(n%5==0?211:225),new Vector2(256,310)+v*245,4,darkInk);
            }
            Number(gauge,strip,"0",60,215,30,darkInk);Number(gauge,strip,"5",256,23,30,darkInk);Number(gauge,strip,"10",448,215,30,darkInk);Save("tx_cabGauge",gauge,false);
            var scale=new Canvas(96,512,Color.white);
            for(int n=0;n<=10;n++)scale.Line(new Vector2(n%5==0?22:42,24+n*46),new Vector2(90,24+n*46),3,darkInk);
            Number(scale,strip,"10",20,8,24,darkInk);Number(scale,strip,"5",20,238,24,darkInk);Number(scale,strip,"0",20,468,24,darkInk);Save("tx_cabScale",scale,false);
            Save("tx_menuTile150",tile,true);AssetDatabase.Refresh();Import("tx_shiftCardGrid",false);Import("tx_menuTile150",true);Import("tx_menuDrums",true);Import("tx_clockFace",false);Import("tx_cabGauge",false);Import("tx_cabScale",false);
            return "R1 karo ve R2 zımba ızgarası üretildi";
        }

        static void Number(Canvas target,Canvas digits,string text,float cx,float top,int height,Color ink)
        {
            float x=cx-text.Length*height*.3f;var bg=Hex("#E9DDBE");var dark=Hex("#1C150E");int width=Mathf.RoundToInt(height*.6f);
            foreach(char ch in text)
            {
                int d=ch-'0';
                for(int y=0;y<height;y++)for(int xx=0;xx<width;xx++)
                {
                    var c=digits.Px[(d*128+Mathf.Min(127,y*128/height))*128+Mathf.Min(127,xx*128/width)];
                    target.Blend((int)x+xx,(int)top+y,ink,Mathf.Clamp01((bg.r-c.r)/(bg.r-dark.r)));
                }
                x+=width;
            }
        }

        /// <summary>Font atlası okunur olmayabilir: GPU'dan okunur bir kopya (alfa kanalı uzaklık alanı).</summary>
        static Texture2D ReadableAtlas(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        /// <summary>Tuş başlığı ikonları (token keyIcon: krem karo, koyu çizgi #1E1B16); şekil Revizyon 2'nin saydam ikonlarından.</summary>
        public static readonly string[] KeyIcons = { "shelter", "missions", "door", "desk" };

        static Canvas KeyIconTile(string key)
        {
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(DeskSpriteManifest.Root + "/Art/Siginak/Textures/tx_key_" + key + ".png"));
            var canvas = new Canvas(src.width, src.height, Hex("#E9DDBE"));
            var ink = Hex("#1E1B16");
            var px = src.GetPixels();   // alt-sol orijin
            for (int y = 0; y < src.height; y++)
                for (int x = 0; x < src.width; x++)
                    canvas.Blend(x, src.height - 1 - y, ink, px[y * src.width + x].a);
            UnityEngine.Object.DestroyImmediate(src);
            return canvas;
        }

        /// <summary>dress_room_desk.js › chalkMat(k): 5 hücre (1–5 çentik, 5'te çapraz), 256 × 240 hücre, şeffaf zemin.</summary>
        static Canvas ChalkTally()
        {
            var canvas = new Canvas(256 * 5, 240, new Color(1, 1, 1, 0));
            var chalk = new Color(232 / 255f, 228 / 255f, 214 / 255f);
            for (int k = 1; k <= 5; k++)
            {
                float ox = (k - 1) * 256;
                for (int i = 0; i < Mathf.Min(k, 4); i++)
                {
                    float xx = 52 + i * 44;
                    canvas.Line(new Vector2(ox + xx + (i % 3) - 1, 30), new Vector2(ox + xx + 4 + (i % 2) * 3, 212), 13, chalk, 0.9f);
                }
                if (k >= 5) canvas.Line(new Vector2(ox + 24, 186), new Vector2(ox + 232, 58), 13, chalk, 0.9f);
            }
            // tebeşir dokusu: kenar boyunca seyrek boşluklar
            for (int i = 0; i < 3500; i++)
            {
                int x = (int)(Rnd() * canvas.W), y = (int)(Rnd() * canvas.H), idx = y * canvas.W + x;
                if (canvas.Px[idx].a > 0) canvas.Px[idx].a *= 0.35f + Rnd() * 0.5f;
            }
            return canvas;
        }

        /// <summary>
        /// dress_room_shelf.js › dialScale(): telsiz kanal penceresi 1000 × 300, turuncu degrade, 51 çentik (beşte bir uzun).
        /// Kanal rakamları (01–11) sahnede TMP; malzeme sahneye özel (UV metre cinsinden, döşeme UV sınırından).
        /// </summary>
        static Canvas DialScale()
        {
            var canvas = new Canvas(1000, 300, Color.white);
            Color top = Hex("#F0BD62"), bottom = Hex("#C4802F");
            for (int y = 0; y < canvas.H; y++)
            {
                var c = Color.Lerp(top, bottom, y / (canvas.H - 1f));
                for (int x = 0; x < canvas.W; x++) canvas.Px[y * canvas.W + x] = c;
            }
            var ink = Hex("#3A240C");
            for (int i = 0; i <= 50; i++) canvas.Rect(50 + i * 18 - 2, 190, 4, i % 5 == 0 ? 80 : 40, ink);
            return canvas;
        }

        /// <summary>dress_room_shelf.js › meterFace(): SİNYAL göstergesi 400 × 350, krem zemin, yay ve 11 çentik (kelime TMP).</summary>
        static Canvas MeterFace()
        {
            var canvas = new Canvas(400, 350, Hex("#EFE4BC"));
            var ink = Hex("#2A1E10");
            var center = new Vector2(200, 300);
            const float R = 230;
            canvas.Arc(center, R, Mathf.PI * 1.23f, Mathf.PI * 1.77f, 6, ink);
            for (int i = 0; i <= 10; i++)
            {
                float a = Mathf.PI * (1.23f + 0.054f * i), r2 = i % 5 != 0 ? R - 26 : R - 42;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                canvas.Line(center + dir * R, center + dir * r2, i % 5 != 0 ? 4 : 7, ink);
            }
            return canvas;
        }

        /// <summary>dress_room_desk.js › posterMat(): 440 × 600, iki kat (880 × 1200); krem bordür, kırmızı zemin, fişi çekilmiş kablo, ayırıcı.</summary>
        static Canvas Poster()
        {
            const float k = 2;
            var canvas = new Canvas(880, 1200, Hex("#EAD9C0"));
            canvas.Rect(18 * k, 18 * k, (440 - 36) * k, (600 - 36) * k, Hex("#9A1E14"));
            var cream = Hex("#EFE2CF");
            Vector2 P(float x, float y) => new Vector2(x * k, y * k);
            canvas.Polyline(Canvas.Bezier(P(85, 150), P(130, 150), P(150, 190), P(180, 190)), 13 * k, cream);
            canvas.Rect(175 * k, 170 * k, 45 * k, 40 * k, cream);
            canvas.Rect(220 * k, 176 * k, 20 * k, 7 * k, cream);
            canvas.Rect(220 * k, 197 * k, 20 * k, 7 * k, cream);
            canvas.Polyline(Canvas.Bezier(P(355, 150), P(310, 150), P(290, 190), P(270, 190)), 13 * k, cream);
            canvas.Rect(260 * k, 170 * k, 15 * k, 40 * k, cream);
            canvas.Line(P(235, 125), P(250, 150), 7 * k, cream);
            canvas.Line(P(250, 235), P(235, 260), 7 * k, cream);
            canvas.Rect(60 * k, 440 * k, (440 - 120) * k, 4 * k, cream);
            for (int i = 0; i < 2500 * 4; i++)
            {
                var c = Rnd() < 0.5f ? Color.black : new Color(1, 240 / 255f, 220 / 255f);
                canvas.Rect(Rnd() * canvas.W, Rnd() * canvas.H, 2 * k, 2 * k, c, Rnd() * 0.06f);
            }
            return canvas;
        }

        /// <summary>dress_room_desk.js › ringDecal(): 256², iki yay, şeffaf zemin.</summary>
        static Canvas CoffeeRing()
        {
            var canvas = new Canvas(256, 256, new Color(74 / 255f, 46 / 255f, 26 / 255f, 0));
            var ink = new Color(74 / 255f, 46 / 255f, 26 / 255f);
            canvas.Arc(new Vector2(128, 128), 92, 0.3f, 6.0f, 9, ink, 0.55f);
            canvas.Arc(new Vector2(131, 126), 84, 0, 7, 3, ink, 0.55f);
            return canvas;
        }

        /// <summary>Vana altındaki pas ve su akıntısı: dikey, üstte koyu, aşağı doğru saydamlaşan birkaç iz (alfa).</summary>
        static Canvas Drip()
        {
            var rust = Hex("#5A4630");
            var canvas = new Canvas(128, 512, new Color(rust.r, rust.g, rust.b, 0));
            for (int s = 0; s < 7; s++)
            {
                float x = 24 + Rnd() * 80, w = 3 + Rnd() * 9, len = 180 + Rnd() * 320, wobble = Rnd() * 6;
                for (int y = 0; y < (int)len && y < canvas.H; y++)
                {
                    float t = y / len, cx = x + Mathf.Sin(y * 0.03f + s) * wobble;
                    float alpha = Mathf.Lerp(0.85f, 0, t) * (0.6f + 0.4f * Rnd());
                    for (int i = Mathf.FloorToInt(cx - w); i <= Mathf.CeilToInt(cx + w); i++)
                        canvas.Blend(i, y, rust, alpha * Mathf.Clamp01(1 - Mathf.Abs(i - cx) / w));
                }
            }
            canvas.Rect(0, 0, canvas.W, 18, rust, 0.5f);   // vananın hemen altındaki koyu bant
            return canvas;
        }

        // ---------------------------------------------------------------- devir fişi (Revizyon 3 · Aşama 4, R19c)

        /// <summary>Devir fişinin kâğıt dokusu (<see cref="Dir"/>'da; 480 × 720, 4,8 px/mm, sözcüksüz).</summary>
        // ---------------------------------------------------------------- buğu camı (Aşama 6, R17b)

        public const string FogTexture = "tx_fog";
        /// <summary>Buğu dokusunun kenarı (piksel).</summary>
        public const int FogSize = 512;

        [MenuItem("Corrigible/Revizyon 3/Buğu dokusunu üret")]
        static void BakeFogMenu() => Debug.Log("[Rev3] " + BakeFog());

        /// <summary>
        /// Buğu camının dokusu <c>tx_fog.png</c> (Aşama 6, PLAN §1.12; DA:76–80 <c>dress_room_admission.js</c>): 512² RGBA, iki
        /// çemberli radyal degrade (başlangıç merkez (256; 290) r 20, bitiş merkez (256; 256) r 256; duraklar 0 → rgba(238,242,240,.94),
        /// 0,5 → (232,238,236,.80), 0,82 → (222,230,228,.30), 1 → (222,230,228,0)) ve üstünde 420 beyaz damla (Park-Miller tohum 7:
        /// açı, yarıçap √rnd·230, alfa 0,15 + rnd·0,45, damla yarıçapı 1,5 + rnd·4,5 px; çizim sırası three.js ile aynı). İçe aktarma
        /// sRGB, alfa saydamlık, mip açık, kenar sıkıştırma (Clamp). Aynı baytları her seferinde üretir.
        /// </summary>
        public static string BakeFog()
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Dir + "/" + FogTexture + ".png", FogPng());
            AssetDatabase.Refresh();
            Import(FogTexture, false);
            return "Buğu dokusu " + Dir + "/" + FogTexture + ".png (" + FogSize + " × " + FogSize + ")";
        }

        /// <summary>Buğu dokusunun PNG baytları (diske yazmadan; belirlenimcilik testi).</summary>
        public static byte[] FogPng()
        {
            var tex = Fog().ToTexture();
            try { return tex.EncodeToPNG(); }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }

        /// <summary>Buğu dokusunun pikselleri, sol üst orijin, satır satır (<see cref="FogSize"/>²).</summary>
        public static Color[] FogPixels() => (Color[])Fog().Px.Clone();

        static Canvas Fog()
        {
            const int N = FogSize;
            var canvas = new Canvas(N, N, new Color(0, 0, 0, 0));
            // canvas createRadialGradient(256, 290, 20, 256, 256, 256): her piksel için en büyük t (r(t) ≥ 0) çözülür, [0, 1]'e kıstırılır
            var c0 = new Vector2(256f, 290f);
            var d = new Vector2(256f, 256f) - c0;
            const float r0 = 20f, dr = 256f - 20f;
            float a = Vector2.Dot(d, d) - dr * dr;
            var stops = new[] { 0f, 0.5f, 0.82f, 1f };
            var colours = new[]
            {
                new Color(238 / 255f, 242 / 255f, 240 / 255f, 0.94f), new Color(232 / 255f, 238 / 255f, 236 / 255f, 0.80f),
                new Color(222 / 255f, 230 / 255f, 228 / 255f, 0.30f), new Color(222 / 255f, 230 / 255f, 228 / 255f, 0f),
            };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f) - c0;
                    float b = -2f * (Vector2.Dot(q, d) + r0 * dr);
                    float c = Vector2.Dot(q, q) - r0 * r0;
                    float disc = Mathf.Max(0f, b * b - 4f * a * c);
                    float t = (-b - Mathf.Sqrt(disc)) / (2f * a);   // a < 0: büyük kök
                    t = Mathf.Clamp01(t);
                    int k = 0;
                    while (k < stops.Length - 2 && t > stops[k + 1]) k++;
                    float u = Mathf.InverseLerp(stops[k], stops[k + 1], t);
                    canvas.Px[y * N + x] = Color.Lerp(colours[k], colours[k + 1], u);
                }
            // damlalar (Park-Miller: seed = seed · 16807 mod 2³¹−1; rnd = seed / (2³¹−1))
            long seedFog = 7;
            float rnd() { seedFog = seedFog * 16807 % 2147483647; return seedFog / 2147483647f; }
            var white = Color.white;
            for (int i = 0; i < 420; i++)
            {
                float ang = rnd() * Mathf.PI * 2f, rad = Mathf.Sqrt(rnd()) * 230f;
                float cx = 256f + Mathf.Cos(ang) * rad, cy = 256f + Mathf.Sin(ang) * rad;
                float alpha = 0.15f + rnd() * 0.45f;
                float r = 1.5f + rnd() * 4.5f;
                for (int py = Mathf.FloorToInt(cy - r - 1); py <= Mathf.CeilToInt(cy + r + 1); py++)
                    for (int px = Mathf.FloorToInt(cx - r - 1); px <= Mathf.CeilToInt(cx + r + 1); px++)
                    {
                        float dist = new Vector2(px + 0.5f - cx, py + 0.5f - cy).magnitude;
                        float cover = Mathf.Clamp01(r + 0.5f - dist);
                        if (cover > 0) canvas.Blend(px, py, white, alpha * cover);
                    }
            }
            return canvas;
        }

        public const string HandoverSlipTexture = "tx_handoverSlip";
        /// <summary>Fiş yüzünün sprite'ları (beyaz + alfa; Image rengi boyar).</summary>
        public const string SlipSpriteDir = Rev3ModelImporter.ArtDir + "/Sprites/slip";
        /// <summary>Sprite çözünürlüğü: tasarımın 1× pikselinin 5 katı (24 px/mm).</summary>
        public const float SlipSpritePxPerMm = 24f;
        /// <summary>Takım diski dolgusu ve halkası, nokta çizgisi hücresi, uyarı üçgeni, eylem oku.</summary>
        public static readonly string[] SlipSprites = { "slip_disc_fill", "slip_disc_ring", "slip_leader", "slip_warning", "slip_arrow" };

        [MenuItem("Corrigible/Revizyon 3/Devir fişi dokusunu üret")]
        static void BakeHandoverSlipMenu() => Debug.Log("[Rev3] " + BakeHandoverSlip());

        /// <summary>
        /// Devir fişinin dokusu ve yüz sprite'ları (tasarım HTML'i <c>kagit_kaynak/devir_fisi_*.html</c>, C §7.2):
        /// <list type="bullet">
        /// <item><c>tx_handoverSlip.png</c> 480 × 720 RGBA: kâğıt #EFE4BC (token <c>materials.telexPaper</c>), sol/sağ/alt 1 px
        /// kenar #C9B98A (gövde y 6'dan), üstte testere dişi (adım 8 px, derinlik 5 px; alfa yalnız burada), delik sırası y 8–14
        /// (r 1,4 px, 1,8'de söner; adım 9 × 6; rgba(120,104,70,0,35)). İçe aktarma: NPOT küçültme yok, mip açık, sRGB.</item>
        /// <item><see cref="SlipSprites"/> (<see cref="SlipSpriteDir"/>): beyaz şekiller, <see cref="SlipSpritePxPerMm"/>
        /// çözünürlükte; nokta çizgisi döşenir (Repeat).</item>
        /// </list>
        /// Aynı çıktıyı her seferinde üretir (rastgelelik yok).
        /// </summary>
        public static string BakeHandoverSlip()
        {
            Directory.CreateDirectory(Dir);
            Directory.CreateDirectory(SlipSpriteDir);
            Save(HandoverSlipTexture, HandoverSlipPaper(), false);
            SaveAt(SlipSpriteDir + "/slip_disc_fill.png", SlipDisc(false));
            SaveAt(SlipSpriteDir + "/slip_disc_ring.png", SlipDisc(true));
            SaveAt(SlipSpriteDir + "/slip_leader.png", SlipLeader());
            SaveAt(SlipSpriteDir + "/slip_warning.png", SlipWarningIcon());
            SaveAt(SlipSpriteDir + "/slip_arrow.png", SlipArrow());
            AssetDatabase.Refresh();
            Import(HandoverSlipTexture, false);
            foreach (var name in SlipSprites) ImportSlipSprite(SlipSpriteDir + "/" + name + ".png", name == "slip_leader");
            return "Devir fişi dokusu " + Dir + "/" + HandoverSlipTexture + ".png (480 × 720) ve " + SlipSprites.Length + " sprite → " + SlipSpriteDir;
        }

        static void SaveAt(string path, Canvas canvas)
        {
            var tex = canvas.ToTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void ImportSlipSprite(string path, bool repeat)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.textureType = TextureImporterType.Sprite;
            s.spriteMode = (int)SpriteImportMode.Single;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteGenerateFallbackPhysicsShape = false;
            s.spritePixelsPerUnit = 100f * SlipSpritePxPerMm;   // tuvalin referansı 100 birim: 1 mm = 24 px
            s.spriteBorder = Vector4.zero;
            s.mipmapEnabled = true;
            s.sRGBTexture = true;
            s.alphaSource = TextureImporterAlphaSource.FromInput;
            s.alphaIsTransparency = true;
            s.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            s.filterMode = FilterMode.Trilinear;
            s.npotScale = TextureImporterNPOTScale.None;
            ti.SetTextureSettings(s);
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        static Color SlipToken(string key, string fallback)
        {
            var theme = SiginakTheme.Active;
            return theme != null && theme.TryGet(key, out var c) ? new Color(c.r, c.g, c.b, 1) : Hex(fallback);
        }

        /// <summary>Kâğıt: kesik kenar, kenar çizgisi ve delik sırası (CSS pikseli = doku pikseli).</summary>
        static Canvas HandoverSlipPaper()
        {
            const int W = 480, H = 720, BodyTop = 6;
            var paper = SlipToken("materials.telexPaper", "#EFE4BC");
            var edge = Hex("#C9B98A");
            var canvas = new Canvas(W, H, new Color(paper.r, paper.g, paper.b, 0));
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float cover = 1f;
                    if (y < BodyTop)
                    {
                        // testere dişi: tepe y 0 (x = 4 + 8k), dip y 5 (x = 8k); 4 × 4 alt örnekle kenar yumuşar
                        cover = 0f;
                        for (int sy = 0; sy < 4; sy++)
                            for (int sx = 0; sx < 4; sx++)
                            {
                                float fx = x + (sx + 0.5f) / 4f, fy = y + (sy + 0.5f) / 4f;
                                float m = Mathf.Repeat(fx, 8f);
                                float tooth = 5f * Mathf.Abs(m - 4f) / 4f;
                                if (fy >= tooth) cover += 1f / 16f;
                            }
                    }
                    canvas.Px[y * W + x] = new Color(paper.r, paper.g, paper.b, cover);
                    bool border = y >= BodyTop && (x == 0 || x == W - 1 || y == H - 1);
                    if (border) canvas.Px[y * W + x] = edge;
                }
            // delik sırası: gövdenin dolgu kutusu x 1'den başlar; hücre 9 × 6, merkez (1 + 4,5 + 9k; 8 + 3)
            var punch = new Color(120 / 255f, 104 / 255f, 70 / 255f, 1f);
            for (float cx = 1 + 4.5f; cx < W - 1; cx += 9f)
                for (int y = 8; y < 14; y++)
                    for (int x = Mathf.FloorToInt(cx - 2); x <= Mathf.CeilToInt(cx + 2); x++)
                    {
                        if (x < 1 || x >= W - 1) continue;
                        float a = 0f;
                        for (int sy = 0; sy < 4; sy++)
                            for (int sx = 0; sx < 4; sx++)
                            {
                                float d = new Vector2(x + (sx + 0.5f) / 4f - cx, y + (sy + 0.5f) / 4f - 11f).magnitude;
                                a += Mathf.Clamp01((1.8f - d) / 0.4f) / 16f;
                            }
                        canvas.Blend(x, y, punch, 0.35f * a);
                    }
            return canvas;
        }

        static int SlipPx(float cssPx) => Mathf.RoundToInt(cssPx / 4.8f * SlipSpritePxPerMm);

        /// <summary>Takım diski Ø 22 px: dolgu ya da 1,5 px halka (CSS border-box: halka dairenin içinde).</summary>
        static Canvas SlipDisc(bool ring)
        {
            int n = SlipPx(22);
            float r = n / 2f, inner = r - 1.5f / 22f * n;
            var canvas = new Canvas(n, n, new Color(1, 1, 1, 0));
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude;
                    float outer = Mathf.Clamp01(r - d + 0.5f);
                    float a = ring ? Mathf.Min(outer, Mathf.Clamp01(d - inner + 0.5f)) : outer;
                    canvas.Px[y * n + x] = new Color(1, 1, 1, a);
                }
            return canvas;
        }

        /// <summary>Nokta çizgisi hücresi 5 × 3 px: ortada r 1 px nokta, 1,4 px'te söner (yatay döşenir).</summary>
        static Canvas SlipLeader()
        {
            int w = SlipPx(5), h = SlipPx(3);
            float k = w / 5f;
            var canvas = new Canvas(w, h, new Color(1, 1, 1, 0));
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = new Vector2(x + 0.5f - w / 2f, y + 0.5f - h / 2f).magnitude / k;
                    canvas.Px[y * w + x] = new Color(1, 1, 1, Mathf.Clamp01((1.4f - d) / 0.4f));
                }
            return canvas;
        }

        /// <summary>Uyarı üçgeni 16 px (viewBox 26): kontur 2,2, ünlem çubuğu 2,4, nokta r 1,5.</summary>
        static Canvas SlipWarningIcon()
        {
            int n = SlipPx(16);
            float s = n / 26f;
            var canvas = new Canvas(n, n, new Color(1, 1, 1, 0));
            var white = Color.white;
            Vector2 P(float x, float y) => new Vector2(x * s, y * s);
            canvas.Polyline(new[] { P(13, 3), P(24, 22), P(2, 22), P(13, 3) }, 2.2f * s, white);
            canvas.Line(P(13, 10), P(13, 15.5f), 2.4f * s, white);
            var c = P(13, 19);
            float rr = 1.5f * s;
            for (int y = Mathf.FloorToInt(c.y - rr - 1); y <= Mathf.CeilToInt(c.y + rr + 1); y++)
                for (int x = Mathf.FloorToInt(c.x - rr - 1); x <= Mathf.CeilToInt(c.x + rr + 1); x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - c).magnitude;
                    canvas.Blend(x, y, white, Mathf.Clamp01(rr - d + 0.5f));
                }
            return canvas;
        }

        /// <summary>Eylem oku 20 px (viewBox 20): "M10 2 V13 M5 9 L10 14 L15 9 M3 17 H17", kontur 2,2, yuvarlak uç.</summary>
        static Canvas SlipArrow()
        {
            int n = SlipPx(20);
            float s = n / 20f;
            var canvas = new Canvas(n, n, new Color(1, 1, 1, 0));
            var white = Color.white;
            Vector2 P(float x, float y) => new Vector2(x * s, y * s);
            canvas.Line(P(10, 2), P(10, 13), 2.2f * s, white);
            canvas.Polyline(new[] { P(5, 9), P(10, 14), P(15, 9) }, 2.2f * s, white);
            canvas.Line(P(3, 17), P(17, 17), 2.2f * s, white);
            return canvas;
        }

        /// <summary>prints.js › hazardTex(): sarı zemin, 45° koyu şerit (1024 × 128, adım 64), aşınma lekeleri.</summary>
        static Canvas Hazard()
        {
            int w = 1024, h = 128, p = 64;
            var canvas = new Canvas(w, h, Hex("#E3B23C"));
            var dark = Hex("#1E1B16");
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // three.js paralelkenarı: tabanda [i, i + p/2], tepede [i + h, i + p/2 + h] → x + y (aşağı) ekseninde şerit
                    float u = ((x + y - h) % p + p) % p;
                    if (u < p / 2f) canvas.Px[y * w + x] = dark;
                }
            for (int i = 0; i < 70; i++)
            {
                var chip = new Color(70 / 255f, 70 / 255f, 62 / 255f);
                float cx = Rnd() * w, cy = Rnd() * h, rx = 1 + Rnd() * 5, ry = 1 + Rnd() * 3, a = 0.3f + Rnd() * 0.5f;
                for (int y = Mathf.FloorToInt(cy - ry); y <= cy + ry; y++)
                    for (int x = Mathf.FloorToInt(cx - rx); x <= cx + rx; x++)
                        if (Mathf.Pow((x - cx) / rx, 2) + Mathf.Pow((y - cy) / ry, 2) <= 1) canvas.Blend(x, y, chip, a);
            }
            return canvas;
        }
    }
}
