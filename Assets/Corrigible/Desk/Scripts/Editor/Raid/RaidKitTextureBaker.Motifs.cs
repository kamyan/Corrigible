using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Kitlerin motifleri (BASKIN-MEKANLARI Katman 3 tablosu): duvar, binanın kendi taşınabilir eşyasından yığılmış barikattır;
    /// ayrık blok aynı eşyanın dik istifidir. Motif renkleri <c>kits.&lt;id&gt;</c> tokenlarından; dokunun ortalaması çizimden sonra
    /// yüzün rengine eşitlenir, bu yüzden motif tabanı yüz rengiyle doldurup ayrıntıyı az kontrastla (ΔL* ≤ 15) ekler.
    /// Ölçek: oyunda (1080p) yan yüzün bir katı ≈ 30 px yüksek, ≈ 88 px geniş; dokuda bir ekran pikseli yatayda ≈ 6, dikeyde ≈ 3
    /// doku pikseli. Dikey çizgiler ≥ 6 px, yatay çizgiler ≥ 3 px çizilir, yoksa mipmap'te kaybolur.
    /// </summary>
    public static partial class RaidKitTextureBaker
    {
        /// <summary>Kitler; yeni kit buraya bir satır ve beş motif işleviyle eklenir.</summary>
        static readonly Motif[] Motifs =
        {
            new Motif("weu", WeuSide, WeuColumn, WeuTop, WeuChannel, WeuWell),
            new Motif("eeu", EeuSide, EeuColumn, EeuTop, EeuChannel, EeuWell),
            new Motif("me", MeSide, MeColumn, MeTop, MeChannel, MeWell),
            new Motif("af", AfSide, AfColumn, AfTop, AfChannel, AfWell),
            new Motif("na", NaSide, NaColumn, NaTop, NaChannel, NaWell),
            new Motif("ru", RuSide, RuColumn, RuTop, RuChannel, RuWell),
            // Motifs2.cs
            new Motif("in", InSide, InColumn, InTop, InChannel, InWell),
            new Motif("cn", CnSide, CnColumn, CnTop, CnChannel, CnWell),
            new Motif("jk", JkSide, JkColumn, JkTop, JkChannel, JkWell),
            new Motif("sea", SeaSide, SeaColumn, SeaTop, SeaChannel, SeaWell) { ChannelSmoothness = 0.35f, WellSmoothness = 0.35f },
            new Motif("sa", SaSide, SaColumn, SaTop, SaChannel, SaWell),
            new Motif("oc", OcSide, OcColumn, OcTop, OcChannel, OcWell),
        };

        /// <summary>Yan dokuda iki katın satır aralığı (üstten: üst kat, alt kat).</summary>
        static readonly (float top, float bottom)[] Layers = { (UpperTop, UpperBottom), (LowerTop, LowerBottom) };

        /// <summary>Kanalın ve kuyunun görünen bölgesi: göbek 0,6 × 0,6 (UV 0,2–0,8).</summary>
        const float HubMin = SquareSize * 0.2f, HubMax = SquareSize * 0.8f, HubSize = HubMax - HubMin, Mid = SquareSize / 2f;

        // ================================================================ weu · Banka veri merkezi: sökülmüş rack kabinleri

        /// <summary>Yatırılmış iki rack kabini üst üste: delikli montaj raylı uç dikmeleri, iki delikli kapı (iri şaşırtmalı delikler:
        /// oyun ölçeğinde ≈ 5 px aralık, mipmap'te kaybolmaz), aralarında kapı çizgisi ve dik kollar.</summary>
        static void WeuSide(Canvas c, Palette p)
        {
            var a = p.A;
            c.Fill(p.West);
            float[] seams = { 0.46f, 0.56f };
            for (int i = 0; i < 2; i++)
            {
                var (top, bottom) = Layers[i];
                float h = bottom - top, seam = SideW * seams[i];
                RackFrame(c, a, 0, top, SideW, h, 18);
                foreach (var (x0, x1) in new[] { (24f, seam - 4), (seam + 4, SideW - 24f) })
                {
                    c.Bevel(x0, top + 6, x1 - x0, h - 12, 3, 0.2f, 0.28f);
                    Perforation(c, a, x0 + 10, top + 12, x1 - x0 - 20, h - 24, i);
                }
                c.Rect(seam - 3.5f, top + 5, 7, h - 10, a, 0.65f);
                Handle(c, a, seam - 20, top + h / 2 - 14, 8, 28);
                Handle(c, a, seam + 12, top + h / 2 - 14, 8, 28);
            }
            c.Grain(4, 16);
            c.Noise(3);
        }

        /// <summary>Ayakta tek rack kabini (iki katı boydan boya): montaj raylı uç dikmeleri, ortada boydan kapı çizgisi, iki uzun
        /// delikli kapı, dik kollar.</summary>
        static void WeuColumn(Canvas c, Palette p)
        {
            var a = p.A;
            c.Fill(p.West);
            RackFrame(c, a, 0, 0, SideW, SideH, 26);
            float seam = SideW / 2f;
            foreach (var (x0, x1) in new[] { (32f, seam - 4), (seam + 4, SideW - 32f) })
            {
                c.Bevel(x0, 8, x1 - x0, SideH - 16, 3, 0.2f, 0.28f);
                Perforation(c, a, x0 + 12, 16, x1 - x0 - 24, SideH - 32, 0);
            }
            c.Rect(seam - 3.5f, 6, 7, SideH - 12, a, 0.65f);
            Handle(c, a, seam - 22, 56, 8, 48);
            Handle(c, a, seam + 14, 56, 8, 48);
            c.Grain(4, 16);
            c.Noise(3);
        }

        /// <summary>Kabinin üstü: kenarı kabartmalı düz panel, ortasında boydan boya tel kablo tavası ve içinde kablo demeti, iki
        /// kablo giriş yarığı.</summary>
        static void WeuTop(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.Top);
            c.Bevel(0, 0, SquareSize, SquareSize, 8, 0.16f, 0.22f);
            c.Inset(14, 14, SquareSize - 28, SquareSize - 28, 3, 0.12f, 0.18f);
            // Kablo tavası (x boyunca): galvaniz kenar rayları, tel basamaklar, yuvarlak kablolar
            float t0 = 92, t1 = 164;
            c.Rect(0, t0, SquareSize, t1 - t0, b, 0.5f);
            for (float x = 6; x < SquareSize; x += 14) c.Rect(x, t0 + 5, 4, t1 - t0 - 10, a, 0.28f);
            for (int k = 0; k < 4; k++)
            {
                float y = t0 + 10 + k * 13 + Rnd(-1f, 1f);
                c.Cylinder(0, y, SquareSize, 10, true, 0.16f, 0.5f);
                c.Rect(0, y, SquareSize, 10, a, 0.35f);
                c.Rect(0, y + 2, SquareSize, 2, Color.white, 0.18f);
            }
            c.Rect(0, t0, SquareSize, 5, b, 0.7f); c.Bevel(0, t0, SquareSize, 5, 1.5f, 0.2f, 0.4f);
            c.Rect(0, t1 - 5, SquareSize, 5, b, 0.7f); c.Bevel(0, t1 - 5, SquareSize, 5, 1.5f, 0.2f, 0.4f);
            // Kablo giriş yarıkları (fırçalı)
            foreach (float x in new[] { 40f, 176f })
            {
                c.Rect(x, 40, 44, 12, a, 0.45f);
                for (float xx = x + 2; xx < x + 44; xx += 4) c.Rect(xx, 41, 1.5f, 10, Color.white, 0.12f);
            }
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Tel örgü tava: koyu taban üstünde 32 px aralıklı galvaniz tel ızgara ve kaynak noktaları (dikişsiz).</summary>
        static void WeuChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(6, 32);
            for (float k = 0; k < SquareSize; k += 32)
            {
                c.Rect(k, 0, 3.5f, SquareSize, d, 0.85f);
                c.Rect(0, k, SquareSize, 3.5f, d, 0.85f);
            }
            for (float y = 0; y < SquareSize; y += 32)
                for (float x = 0; x < SquareSize; x += 32) c.Circle(x + 1.75f, y + 1.75f, 3, d, 0.9f);
            c.Noise(4);
        }

        /// <summary>Kapağı açılmış döşeme kuyusu: yükseltilmiş döşeme karosunun kenarı, içi koyu boşluk, dört ayak ve gevşek kablo.</summary>
        static void WeuWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            float m = HubMin, s = HubSize, rim = 13;
            c.Rect(m, m, s, s, d, 0.85f);
            c.Bevel(m, m, s, s, 4, 0.2f, 0.3f);
            c.Rect(m + rim, m + rim, s - 2 * rim, s - 2 * rim, Color.black, 0.55f);
            c.Inset(m + rim, m + rim, s - 2 * rim, s - 2 * rim, 4, 0.05f, 0.3f);
            foreach (var (x, y) in new[] { (m + 22, m + 22), (m + s - 38, m + 22), (m + 22, m + s - 38), (m + s - 38, m + s - 38) })
            {
                c.Rect(x, y, 16, 16, d, 0.55f);
                c.Bevel(x, y, 16, 16, 2, 0.15f, 0.3f);
            }
            c.Grain(5, 32);
            c.Noise(4);
        }

        /// <summary>Kabin gövdesi: üst ve alt kayıt, iki uçta montaj raylı dikme (kare delik sırası).</summary>
        static void RackFrame(Canvas c, Color a, float x, float y, float w, float h, float post)
        {
            c.Rect(x, y, w, 4, a, 0.3f); c.Rect(x, y + h - 4, w, 4, a, 0.42f);
            foreach (float px in new[] { x, x + w - post })
            {
                c.Rect(px, y, post, h, a, 0.32f);
                c.Bevel(px, y, post, h, 2, 0.16f, 0.24f);
                for (float yy = y + 5; yy < y + h - 6; yy += 9) c.Rect(px + post / 2 - 3, yy, 6, 4, Color.black, 0.42f);
            }
        }

        /// <summary>Delikli kapı paneli: iri şaşırtmalı delikler (dokuda 26 × 13 px aralık; yüzde daire görünsün diye yatayda geniş
        /// elips), panelin kenarı oyuk.</summary>
        static void Perforation(Canvas c, Color a, float x, float y, float w, float h, int phase)
        {
            c.Rect(x, y, w, h, a, 0.08f);
            c.Inset(x, y, w, h, 2, 0.1f, 0.22f);
            int row = 0;
            for (float yy = y + 6.5f; yy < y + h - 4; yy += 13, row++)
                for (float xx = x + 13 + ((row + phase) % 2) * 13; xx < x + w - 8; xx += 26)
                {
                    Disc(c, xx, yy, 5.2f, 3.1f, a, 0.75f);
                    Disc(c, xx + 0.8f, yy + 1.2f, 3.6f, 1.6f, Color.black, 0.3f);
                }
        }

        static void Handle(Canvas c, Color a, float x, float y, float w, float h)
        {
            c.Rect(x, y, w, h, a, 0.7f);
            c.Rect(x, y, 2, h, Color.white, 0.2f);
            c.Rect(x + w - 2, y, 2, h, Color.black, 0.25f);
        }

        // ================================================================ eeu · Devlet arşivi: çelik arşiv rafı ve dosya kutuları

        /// <summary>Her katta bir raf gözü: üstte raf dudağı, altta raf tablası, uçlarda ve ortada delikli dikme, aralarda dik duran
        /// kurşuni ve krem arşiv kutuları (etiket ve parmak deliğiyle).</summary>
        static void EeuSide(Canvas c, Palette p)
        {
            c.Fill(p.West);
            for (int i = 0; i < 2; i++)
            {
                var (top, bottom) = Layers[i];
                ShelfBay(c, p, top, bottom, new[] { 0f, 248f, 496f }, 16, true);
            }
            c.Grain(4, 16);
            c.Noise(3);
        }

        /// <summary>Dar, uzun raf ucu: iki yanda boydan dikme, dört raf tablası ve aralarında yatık istiflenmiş kutular.</summary>
        static void EeuColumn(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            float post = 20;
            // Raf gözleri: her katta iki göz, kutular yatık (sırtı görünür)
            float[] shelves = { 0, 39, UpperBottom, 120, LowerBottom };
            for (int k = 0; k + 1 < shelves.Length; k++)
            {
                float top = shelves[k] + (k == 2 ? LowerTop - UpperBottom : 0), bottom = shelves[k + 1];
                c.Rect(post, top, SideW - 2 * post, bottom - top, Color.black, 0.16f);
                float y = bottom - 5;
                int n = 0;
                while (y - 13 > top + 6 && n < 3)
                {
                    float bh = Rnd(11, 14), x0 = post + Rnd(6, 26), x1 = SideW - post - Rnd(6, 30);
                    var col = Rnd() < 0.5f ? a : b;
                    c.Rect(x0, y - bh, x1 - x0, bh, col, 0.62f);
                    c.Bevel(x0, y - bh, x1 - x0, bh, 2, 0.14f, 0.24f);
                    c.Rect((x0 + x1) / 2 - 22, y - bh + 3, 44, bh - 6, Color.white, 0.14f);
                    y -= bh + 1; n++;
                }
                ShelfBoard(c, p.West, bottom - 5, 5, 0, SideW);
            }
            ShelfBoard(c, p.West, 0, 5, 0, SideW);
            foreach (float x in new[] { 0f, SideW - post }) Upright(c, a, x, 0, post, SideH);
            c.Grain(4, 16);
            c.Noise(3);
        }

        /// <summary>Rafın üst tablası: kenar dudağı, delik sırası ve üstüne bırakılmış iki krem bir kurşuni kutu.</summary>
        static void EeuTop(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.Top);
            c.Bevel(0, 0, SquareSize, SquareSize, 7, 0.16f, 0.24f);
            for (float x = 18; x < SquareSize - 12; x += 16) { c.Rect(x, 12, 6, 3, a, 0.4f); c.Rect(x, SquareSize - 15, 6, 3, a, 0.4f); }
            foreach (var (x, y, w, h, col) in new[] { (26f, 34f, 96f, 66f, b), (134f, 30f, 92f, 70f, a), (60f, 128f, 120f, 78f, b) })
            {
                c.Rect(x + 4, y + 5, w, h, Color.black, 0.14f);
                c.Rect(x, y, w, h, col, 0.6f);
                c.Bevel(x, y, w, h, 3, 0.14f, 0.24f);
                c.Rect(x + 6, y + h * 0.22f, w - 12, 3, a, 0.28f);
                c.Rect(x + w * 0.3f, y + h * 0.45f, w * 0.4f, h * 0.24f, Color.white, 0.14f);
            }
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Döküm ızgara kenarlı eski kanal: 64 px'lik döküm plakalar, her birinde 3 × 3 yuvarlak yarık (dikişsiz).</summary>
        static void EeuChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(6, 32);
            for (float y = 0; y < SquareSize; y += 64)
                for (float x = 0; x < SquareSize; x += 64)
                {
                    c.Rect(x + 3, y + 3, 58, 58, d, 0.7f);
                    c.Bevel(x + 3, y + 3, 58, 58, 3, 0.1f, 0.3f);
                    for (int j = 0; j < 3; j++)
                        for (int i = 0; i < 3; i++)
                        {
                            float sx = x + 11 + i * 16, sy = y + 11 + j * 16;
                            c.Rect(sx, sy, 10, 10, Color.black, 0.55f);
                            c.Rect(sx, sy + 8, 10, 2, Color.white, 0.06f);
                        }
                }
            c.Noise(4);
        }

        /// <summary>Dökme muayene kapağı: kenar çemberi, kabartma baklava deseni, iki kaldırma yarığı.</summary>
        static void EeuWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            c.Grain(6, 32);
            float r = 70;
            c.Circle(Mid, Mid, r, d, 0.85f);
            c.Ring(Mid, Mid, r - 8, r, Color.white, 0.12f);
            c.Ring(Mid, Mid, r, r + 3, Color.black, 0.4f);
            for (float k = -2 * r; k <= 2 * r; k += 15)
            {
                ClippedLine(c, Mid, Mid, r - 10, Mid + k - r, Mid - r, Mid + k + r, Mid + r, 2.5f, Color.white, 0.12f);
                ClippedLine(c, Mid, Mid, r - 10, Mid + k + r, Mid - r, Mid + k - r, Mid + r, 2.5f, Color.black, 0.18f);
            }
            foreach (float dx in new[] { -38f, 38f }) c.Rect(Mid + dx - 7, Mid - 2.5f, 14, 5, Color.black, 0.6f);
            c.Noise(4);
        }

        static void ShelfBay(Canvas c, Palette p, float top, float bottom, float[] posts, float postW, bool boxes)
        {
            var a = p.A; var b = p.B;
            // Rafın arkası (kutuların üstünde görünen boşluk)
            c.Rect(0, top, SideW, bottom - top, Color.black, 0.14f);
            if (boxes)
                for (int k = 0; k + 1 < posts.Length; k++)
                {
                    float x = posts[k] + postW + 2, end = posts[k + 1] - 2, floorY = bottom - 6;
                    while (x < end - 20)
                    {
                        float w = Mathf.Min(Rnd(32, 46), end - x), h = Rnd(50, 62);
                        if (end - (x + w) < 22) w = end - x;
                        var col = Rnd() < 0.55f ? a : b;
                        c.Rect(x, floorY - h, w - 2, h, col, 0.6f);
                        c.Bevel(x, floorY - h, w - 2, h, 2.5f, 0.14f, 0.26f);
                        // Etiket ve parmak deliği
                        c.Rect(x + 5, floorY - h + 8, w - 12, 10, Color.white, 0.16f);
                        c.Circle(x + (w - 2) / 2, floorY - h + 27, 3.2f, Color.black, 0.4f);
                        c.Rect(x + w - 2, floorY - h, 2, h, Color.black, 0.35f);
                        x += w;
                    }
                }
            ShelfBoard(c, p.West, top, 6, 0, SideW);
            ShelfBoard(c, p.West, bottom - 6, 6, 0, SideW);
            foreach (float x in posts) Upright(c, a, x, top, postW, bottom - top);
        }

        static void ShelfBoard(Canvas c, Color steel, float y, float h, float x, float w)
        {
            c.Rect(x, y, w, h, steel, 0.9f);
            c.Rect(x, y, w, 1.5f, Color.white, 0.22f);
            c.Rect(x, y + h - 1.5f, w, 1.5f, Color.black, 0.32f);
        }

        static void Upright(Canvas c, Color a, float x, float y, float w, float h)
        {
            c.Rect(x, y, w, h, a, 0.28f);
            c.Bevel(x, y, w, h, 2, 0.16f, 0.24f);
            for (float yy = y + 5; yy < y + h - 4; yy += 9) c.Rect(x + w / 2 - 1.5f, yy, 3, 5, Color.black, 0.4f);
        }

        static void ClippedLine(Canvas c, float cx, float cy, float r, float x0, float y0, float x1, float y1, float width, Color col, float alpha)
        {
            // Çemberin içinde kalan parçayı örnekleyerek çizer
            int n = 64;
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                float ax = Mathf.Lerp(x0, x1, t0), ay = Mathf.Lerp(y0, y1, t0), bx = Mathf.Lerp(x0, x1, t1), by = Mathf.Lerp(y0, y1, t1);
                float mx = (ax + bx) / 2 - cx, my = (ay + by) / 2 - cy;
                if (mx * mx + my * my < r * r) c.Line(ax, ay, bx, by, width, col, alpha);
            }
        }

        // ================================================================ me · Körfez rafinerisi: kutu sehpada boru demeti

        /// <summary>Boru istifi uçtan: her katta iki sıra şaşırtmalı boru ağzı (kesik kenar parlak, içi koyu, alt-sağda gölge), kimi
        /// ağızda cıvatalı koyu çelik flanş; her katın dibinde sehpanın çelik kirişi. Yüz üstünde dokuda daireler yatayda 1,87 kat
        /// geniş çizilir (512 px / 0,94 birim, 160 px / 0,547 birim).</summary>
        static void MeSide(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            c.Rect(0, 0, SideW, SideH, Color.black, 0.1f);
            const float aspect = (SideW / 0.94f) / (SideH / 0.547f);
            for (int i = 0; i < 2; i++)
            {
                var (top, bottom) = Layers[i];
                float beam = 9, ry = (bottom - top - beam) / 4f, rx = ry * aspect;
                for (int row = 0; row < 2; row++)
                {
                    float cy = top + ry * (1 + 2 * row) + 0.5f, off = (row + i) % 2 == 0 ? rx : 0;
                    int col = 0;
                    for (float cx = off - rx; cx < SideW + rx; cx += 2 * rx, col++)
                        PipeEnd(c, p, cx, cy, rx, ry, b, (col * 5 + row * 3 + i * 7) % 7 == 0);
                }
                float yb = bottom - beam;
                c.Rect(0, yb, SideW, beam, a, 0.85f);
                c.Bevel(0, yb, SideW, beam, 2, 0.12f, 0.24f);
                for (float x = 12; x < SideW; x += 30) c.Circle(x, yb + beam / 2, 1.6f, Color.black, 0.35f);
            }
            c.Grain(3, 16);
            c.Noise(2);
        }

        /// <summary>Boru ağzı (elips): gölge, parlak kesik kenar, et kalınlığı, koyu iç (üst dudağın gölgesiyle); flanşlıda çevresinde
        /// cıvatalı çelik bilezik.</summary>
        static void PipeEnd(Canvas c, Palette p, float cx, float cy, float rx, float ry, Color steel, bool flange)
        {
            Disc(c, cx + rx * 0.08f, cy + ry * 0.12f, rx, ry, Color.black, 0.15f);
            if (flange)
            {
                Disc(c, cx, cy, rx * 0.98f, ry * 0.98f, steel, 0.9f);
                for (int k = 0; k < 6; k++)
                {
                    float ang = k * Mathf.PI / 3 + Mathf.PI / 6;
                    c.Circle(cx + Mathf.Cos(ang) * rx * 0.86f, cy + Mathf.Sin(ang) * ry * 0.86f, 1.8f, Color.black, 0.35f);
                }
                rx *= 0.76f; ry *= 0.76f;
            }
            Disc(c, cx, cy, rx * 0.95f, ry * 0.95f, p.West, 1);
            Disc(c, cx - rx * 0.05f, cy - ry * 0.07f, rx * 0.9f, ry * 0.88f, Color.white, 0.16f);
            // Boru içi: en koyu yer (üst dudağın gölgesi), alt iç duvar biraz aydınlık
            Disc(c, cx, cy, rx * 0.7f, ry * 0.7f, Color.black, 0.72f);
            Disc(c, cx + rx * 0.04f, cy + ry * 0.22f, rx * 0.5f, ry * 0.4f, p.West, 0.22f);
        }

        /// <summary>Dolu elips (kenarı yumuşak).</summary>
        static void Disc(Canvas c, float cx, float cy, float rx, float ry, Color col, float alpha)
        {
            float soft = Mathf.Min(rx, ry);
            for (int yy = Mathf.FloorToInt(cy - ry - 1); yy <= Mathf.CeilToInt(cy + ry + 1); yy++)
                for (int xx = Mathf.FloorToInt(cx - rx - 1); xx <= Mathf.CeilToInt(cx + rx + 1); xx++)
                {
                    float dx = (xx + 0.5f - cx) / rx, dy = (yy + 0.5f - cy) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float cov = Mathf.Clamp01((1 - d) * soft + 0.5f);
                    if (cov > 0) c.Blend(xx, yy, col, alpha * cov);
                }
        }

        /// <summary>Dik duran boru demeti: üç kalın boru boydan boya, iki yatay flanş bandı, tabanda sehpa levhası.</summary>
        static void MeColumn(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            float w = SideW / 3f, baseH = 12;
            for (int k = 0; k < 3; k++)
            {
                float x = k * w;
                Pipe(c, p, x, 0, w, SideH - baseH, false);
                foreach (float fy in new[] { 26f, 102f }) Flange(c, b, x - 1, fy + (k % 2) * 12, w + 2, 18, false);
            }
            c.Rect(0, SideH - baseH, SideW, baseH, a, 0.8f);
            c.Bevel(0, SideH - baseH, SideW, baseH, 2, 0.12f, 0.22f);
            c.Grain(3, 16);
            c.Noise(2);
        }

        /// <summary>Demetin üstü: x boyunca dört boru (yuvarlak gölge, parlak sırt, aralarında koyu çizgi), üstlerinden geçen iki ince
        /// çelik kelepçe.</summary>
        static void MeTop(Canvas c, Palette p)
        {
            var b = p.B;
            c.Fill(p.Top);
            float h = SquareSize / 4f;
            for (int k = 0; k < 4; k++) Pipe(c, p, 0, k * h, SquareSize, h, true);
            foreach (float x in new[] { 36f, 206f })
            {
                c.Rect(x, 0, 12, SquareSize, b, 0.5f);
                c.Rect(x, 0, 2, SquareSize, Color.white, 0.12f); c.Rect(x + 10, 0, 2, SquareSize, Color.black, 0.2f);
                for (float y = h / 2; y < SquareSize; y += h) c.Circle(x + 6, y, 2.2f, Color.black, 0.3f);
            }
            c.Grain(3, 32);
            c.Noise(2);
        }

        /// <summary>Boru: eksene dik yönde yumuşak yuvarlak gölge, sırtta ince parıltı, komşu boruyla arasında ince koyu çizgi.</summary>
        static void Pipe(Canvas c, Palette p, float x, float y, float w, float h, bool horizontal)
        {
            float span = horizontal ? h : w;
            int n = Mathf.CeilToInt(span);
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / span;
                // Yuvarlak gövde: ışık üstten (yatay boru) ya da soldan (dik boru); ince parlak sırt
                float shade = Mathf.Cos((t - 0.3f) * Mathf.PI * 1.05f);
                var col = shade >= 0 ? Color.white : Color.black;
                float alpha = shade >= 0 ? 0.22f * shade : -0.4f * shade;
                float glint = Mathf.Exp(-Mathf.Pow((t - 0.2f) / 0.03f, 2)) * 0.22f;
                if (horizontal)
                {
                    c.Rect(x, y + i, w, Mathf.Min(1, h - i), col, alpha);
                    c.Rect(x, y + i, w, Mathf.Min(1, h - i), Color.white, glint);
                }
                else
                {
                    c.Rect(x + i, y, Mathf.Min(1, w - i), h, col, alpha);
                    c.Rect(x + i, y, Mathf.Min(1, w - i), h, Color.white, glint);
                }
            }
            if (horizontal) c.Rect(x, y + h - 2, w, 2, Color.black, 0.45f);
            else c.Rect(x + w - 3, y, 3, h, Color.black, 0.45f);
        }

        /// <summary>Flanş halkası: borudan biraz taşan koyu çelik bilezik, kabartma kenar, cıvata başları.</summary>
        static void Flange(Canvas c, Color steel, float x, float y, float w, float h, bool acrossHorizontalPipe)
        {
            c.Rect(x, y, w, h, steel, 0.85f);
            c.Cylinder(x, y, w, h, acrossHorizontalPipe, 0.12f, 0.2f);
            c.Bevel(x, y, w, h, 2, 0.1f, 0.22f);
            if (acrossHorizontalPipe) for (float by = y + 5; by < y + h - 3; by += 9) c.Circle(x + w / 2, by, 2f, Color.black, 0.4f);
            else for (float bx = x + 8; bx < x + w - 4; bx += 18) c.Circle(bx, y + h / 2, 2f, Color.black, 0.4f);
        }

        /// <summary>Boru kanalı: koyu çelik taban, yağ lekeleri; göbekte cıvatalı flanş halkası (kesişimde bağlantı plakası örter).</summary>
        static void MeChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(6, 32);
            for (int i = 0; i < 7; i++) c.Stain(Rnd(0, SquareSize), Rnd(0, SquareSize), Rnd(20, 44), Color.black, 0.18f);
            c.Ring(Mid, Mid, 40, 56, d, 0.85f);
            c.Ring(Mid, Mid, 40, 43, Color.black, 0.35f);
            c.Ring(Mid, Mid, 53, 56, Color.white, 0.08f);
            for (int k = 0; k < 8; k++)
            {
                float ang = k * Mathf.PI / 4 + Mathf.PI / 8;
                c.Circle(Mid + Mathf.Cos(ang) * 48, Mid + Mathf.Sin(ang) * 48, 3, Color.white, 0.14f);
            }
            c.Noise(4);
        }

        /// <summary>Açık boru ağzı: koyu boru içi, çevresinde cıvatalı flanş.</summary>
        static void MeWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            c.Grain(6, 32);
            c.Ring(Mid, Mid, 46, 70, d, 0.9f);
            c.Ring(Mid, Mid, 66, 70, Color.black, 0.3f);
            c.Ring(Mid, Mid, 46, 49, Color.white, 0.08f);
            c.Circle(Mid, Mid, 46, Color.black, 0.55f);
            for (int k = 0; k < 8; k++)
            {
                float ang = k * Mathf.PI / 4 + Mathf.PI / 8;
                c.Circle(Mid + Mathf.Cos(ang) * 58, Mid + Mathf.Sin(ang) * 58, 3.4f, Color.white, 0.16f);
            }
            c.Noise(4);
        }

        // ================================================================ af · Cevher hangarı: oluklu galvaniz sac bunker

        /// <summary>Dikey oluklu galvaniz sac (32 px adım), katlarda bindirme kenarı ve cıvata sırası, galvaniz alacası, cıvata
        /// altlarında hafif pas izi, dipte cevher tozu.</summary>
        static void AfSide(Canvas c, Palette p)
        {
            var a = p.A;
            c.Fill(p.West);
            Spangle(c, p.B, 0, 0, SideW, SideH, 70);
            Corrugate(c, 0, 0, SideW, SideH, 32, true, 0.16f, 0.2f);
            for (int i = 0; i < 2; i++)
            {
                var (top, bottom) = Layers[i];
                c.Rect(0, top, SideW, 3, Color.white, 0.18f);
                c.Rect(0, bottom - 3, SideW, 3, Color.black, 0.3f);
                for (float x = 16; x < SideW; x += 64)
                    foreach (float y in new[] { top + 8, bottom - 9 })
                    {
                        c.Circle(x, y, 2.4f, Color.black, 0.45f);
                        c.Circle(x - 0.6f, y - 0.6f, 1.1f, Color.white, 0.3f);
                    }
                for (int k = 0; k < 3; k++)
                {
                    float x = 16 + 64 * RndInt(0, 8);
                    for (float y = top + 11; y < top + 11 + Rnd(16, 34); y += 1) c.Rect(x - 1.5f, y, 3, 1, a, 0.14f * (1 - (y - top - 11) / 34f));
                }
            }
            c.Rect(0, SideH - 7, SideW, 7, a, 0.35f);
            c.Speckle(0, SideH - 9, SideW, 8, 160, 0.8f, 1.8f, a, 0.3f, 0.6f);
            c.Noise(3);
        }

        /// <summary>Yatay oluklu sac silo: oluklar yatay (16 px adım), iki uçta köşe profili ve cıvataları.</summary>
        static void AfColumn(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            Spangle(c, b, 0, 0, SideW, SideH, 70);
            Corrugate(c, 0, 0, SideW, SideH, 16, false, 0.16f, 0.2f);
            foreach (float x in new[] { 0f, SideW - 20f })
            {
                c.Rect(x, 0, 20, SideH, b, 0.4f);
                c.Bevel(x, 0, 20, SideH, 2.5f, 0.16f, 0.28f);
                for (float y = 10; y < SideH; y += 24) c.Circle(x + 10, y, 2.3f, Color.black, 0.45f);
            }
            c.Rect(0, UpperBottom - 2, SideW, 3, Color.black, 0.22f);
            c.Rect(0, SideH - 6, SideW, 6, a, 0.32f);
            c.Noise(3);
        }

        /// <summary>Bunkerin açık üstü: galvaniz kenar, içinde basık cevher yığını (ortası tepe, tane ve iri topaklar).</summary>
        static void AfTop(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.Top);
            float rim = 16;
            c.Rect(rim, rim, SquareSize - 2 * rim, SquareSize - 2 * rim, a, 0.7f);
            c.Stain(Mid + 10, Mid - 6, 110, Color.white, 0.12f);
            c.Speckle(rim, rim, SquareSize - 2 * rim, SquareSize - 2 * rim, 1400, 0.8f, 2.2f, Color.black, 0.1f, 0.3f);
            c.Speckle(rim, rim, SquareSize - 2 * rim, SquareSize - 2 * rim, 900, 0.8f, 2f, Color.white, 0.08f, 0.22f);
            for (int i = 0; i < 26; i++)
            {
                float x = Rnd(rim + 8, SquareSize - rim - 8), y = Rnd(rim + 8, SquareSize - rim - 8), r = Rnd(3.5f, 7.5f);
                c.Circle(x + 1.5f, y + 1.5f, r, Color.black, 0.25f);
                c.Circle(x, y, r, a, 0.85f);
                c.Circle(x - r * 0.3f, y - r * 0.3f, r * 0.45f, Color.white, 0.18f);
            }
            // Galvaniz kenar (bunker sacının üst kıvrımı)
            foreach (var (x, y, w, h) in new[] { (0f, 0f, (float)SquareSize, rim), (0f, SquareSize - rim, (float)SquareSize, rim), (0f, rim, rim, SquareSize - 2 * rim), (SquareSize - rim, rim, rim, SquareSize - 2 * rim) })
            {
                c.Rect(x, y, w, h, b, 0.6f);
                Spangle(c, b, x, y, w, h, 6);
            }
            c.Bevel(0, 0, SquareSize, SquareSize, 4, 0.16f, 0.22f);
            c.Inset(rim, rim, SquareSize - 2 * rim, SquareSize - 2 * rim, 3, 0.1f, 0.3f);
            c.Noise(3);
        }

        /// <summary>Cevher tozlu koyu taban: ince taneler ve alaca (dikişsiz).</summary>
        static void AfChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(8, 32);
            c.Speckle(0, 0, SquareSize, SquareSize, 1300, 0.7f, 2.2f, d, 0.35f, 0.8f);
            c.Speckle(0, 0, SquareSize, SquareSize, 300, 0.6f, 1.4f, Color.white, 0.05f, 0.12f);
            c.Noise(4);
        }

        /// <summary>Sac kenarlı çukur, köşelerine yığılmış cevher.</summary>
        static void AfWell(Canvas c, Palette p)
        {
            var d = p.WellDetail; var a = p.A;
            c.Fill(p.Well);
            c.Grain(8, 32);
            float m = HubMin, s = HubSize, rim = 11;
            c.Rect(m, m, s, s, d, 0.8f);
            c.Bevel(m, m, s, s, 3, 0.18f, 0.3f);
            c.Rect(m + rim, m + rim, s - 2 * rim, s - 2 * rim, Color.black, 0.5f);
            foreach (var (x, y) in new[] { (m + rim, m + rim), (m + s - rim, m + s - rim), (m + s - rim, m + rim) })
            {
                c.Stain(x, y, 34, a, 0.45f);
                c.Speckle(x - 24, y - 24, 48, 48, 90, 0.8f, 2.2f, a, 0.3f, 0.7f);
            }
            c.Noise(4);
        }

        /// <summary>Galvaniz alacası: düşük kontrastlı açık ve koyu lekeler.</summary>
        static void Spangle(Canvas c, Color light, float x, float y, float w, float h, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float cx = x + Rnd() * w, cy = y + Rnd() * h, r = Rnd(6, 18);
                bool up = Rnd() < 0.5f;
                c.Stain(cx, cy, r, up ? light : Color.black, up ? 0.12f : 0.06f);
            }
        }

        /// <summary>Oluk gölgesi: sinüs profili (dikey oluk: sütun boyunca, yatay oluk: satır boyunca).</summary>
        static void Corrugate(Canvas c, float x, float y, float w, float h, float period, bool vertical, float light, float dark)
        {
            int n = Mathf.CeilToInt(vertical ? w : h);
            for (int i = 0; i < n; i++)
            {
                float s = Mathf.Sin((i + 0.5f) / period * 2 * Mathf.PI);
                var col = s >= 0 ? Color.white : Color.black;
                float a = s >= 0 ? light * s : -dark * s;
                if (vertical) c.Rect(x + i, y, 1, h, col, a);
                else c.Rect(x, y + i, w, 1, col, a);
            }
        }

        // ================================================================ na · Dağıtım merkezi: paletli, streç filmli koli yükü

        /// <summary>Alt katın dibinde ahşap palet (üst tahta, takozlar, çatal boşlukları); üstünde iri koliler (kapak bandı, kenar
        /// gölgesi); hepsinin üstünde streç film: açık örtü, geniş eğik parlama, sargı kenarları.</summary>
        static void NaSide(Canvas c, Palette p)
        {
            c.Fill(p.West);
            float palletTop = LowerBottom - 22;
            Cartons(c, p, UpperTop, UpperBottom - 1, new[] { 0f, 168f, 344f, SideW });
            Cartons(c, p, LowerTop + 1, palletTop, new[] { 0f, 180f, 352f, SideW });
            StretchFilm(c, 0, 0, SideW, palletTop, 0.25f);
            Pallet(c, p.A, 0, palletTop, SideW, LowerBottom - palletTop + 1);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Koli kulesi: paletin üstünde iki sütun iri koli (üst katta iki sıra), köşe koruyucuları, dik çember bantları, film.</summary>
        static void NaColumn(Canvas c, Palette p)
        {
            var a = p.A;
            c.Fill(p.West);
            float palletTop = LowerBottom - 22;
            float[] cols = { 0, SideW / 2f, SideW };
            Cartons(c, p, UpperTop, UpperBottom / 2, cols);
            Cartons(c, p, UpperBottom / 2, UpperBottom - 1, cols);
            Cartons(c, p, LowerTop + 1, palletTop, cols);
            StretchFilm(c, 0, 0, SideW, palletTop, 0.6f);
            foreach (float x in new[] { 0f, SideW - 18f }) { c.Rect(x, 0, 18, palletTop, a, 0.6f); c.Bevel(x, 0, 18, palletTop, 2, 0.14f, 0.26f); }
            foreach (float x in new[] { 118f, 384f }) { c.Rect(x, 0, 10, palletTop, Color.white, 0.22f); c.Rect(x + 8, 0, 2, palletTop, Color.black, 0.22f); }
            Pallet(c, a, 0, palletTop, SideW, LowerBottom - palletTop + 1);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Yükün üstü: 2 × 2 iri koli kapağı (kapak çizgisi ve bant; yönleri karışık), köşe koruyucuları, film parlaması.</summary>
        static void NaTop(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.Top);
            float s = SquareSize / 2f;
            for (int j = 0; j < 2; j++)
                for (int i = 0; i < 2; i++)
                {
                    float x = i * s, y = j * s;
                    c.Rect(x + 2, y + 2, s - 4, s - 4, b, 0.45f);
                    c.Inset(x, y, s, s, 3, 0.14f, 0.4f);
                    bool along = (i + j) % 2 == 0;
                    if (along) { c.Rect(x + 3, y + s / 2 - 9, s - 6, 18, Color.white, 0.24f); c.Rect(x + 3, y + s / 2 - 1, s - 6, 2, Color.black, 0.3f); }
                    else { c.Rect(x + s / 2 - 9, y + 3, 18, s - 6, Color.white, 0.24f); c.Rect(x + s / 2 - 1, y + 3, 2, s - 6, Color.black, 0.3f); }
                }
            StretchFilm(c, 0, 0, SquareSize, SquareSize, 0.4f);
            foreach (var (x, y) in new[] { (0f, 0f), (SquareSize - 26f, 0f), (0f, SquareSize - 26f), (SquareSize - 26f, SquareSize - 26f) })
            {
                c.Rect(x, y, 26, 26, a, 0.65f);
                c.Bevel(x, y, 26, 26, 2, 0.12f, 0.26f);
            }
            c.Noise(3);
        }

        /// <summary>Koyu cilalı beton: alaca ve rastgele yönlü ince lastik izleri (dikişsiz).</summary>
        static void NaChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(7, 32);
            for (int i = 0; i < 40; i++)
            {
                float x = Rnd(0, SquareSize), y = Rnd(0, SquareSize), ang = Rnd(0, Mathf.PI), len = Rnd(10, 30);
                c.Line(x, y, x + Mathf.Cos(ang) * len, y + Mathf.Sin(ang) * len, 2, d, 0.5f);
            }
            c.Noise(4);
        }

        /// <summary>Döşeme priz kutusu: çelik çerçeve, koyu iç, menteşe çizgisi, iki kablo rakoru.</summary>
        static void NaWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            c.Grain(7, 32);
            float m = HubMin, s = HubSize, rim = 10;
            c.Rect(m, m, s, s, d, 0.85f);
            c.Bevel(m, m, s, s, 3, 0.18f, 0.3f);
            c.Rect(m + rim, m + rim, s - 2 * rim, s - 2 * rim, Color.black, 0.5f);
            c.Rect(m + rim, m + rim + 4, s - 2 * rim, 3, d, 0.6f);
            foreach (float x in new[] { m + s * 0.3f, m + s * 0.7f })
            {
                c.Circle(x, m + s - 30, 11, d, 0.7f);
                c.Circle(x, m + s - 30, 6, Color.black, 0.6f);
            }
            c.Noise(4);
        }

        /// <summary>Bir sıra iri koli: sınırlar <paramref name="edges"/> (x), her kolinin kenar ışığı ve gölgesi, ortada kapak bandı.</summary>
        static void Cartons(Canvas c, Palette p, float top, float bottom, float[] edges)
        {
            var b = p.B;
            float h = bottom - top;
            for (int k = 0; k + 1 < edges.Length; k++)
            {
                float x = edges[k], w = edges[k + 1] - x;
                c.Rect(x, top, w, h, b, 0.5f);
                c.Rect(x, top, 4, h, Color.white, 0.12f);
                c.Rect(x + w - 5, top, 5, h, Color.black, 0.3f);
                c.Rect(x, top, w, 2.5f, Color.white, 0.12f);
                c.Rect(x, bottom - 3, w, 3, Color.black, 0.3f);
                // Kapak bandı: üst kenardan aşağı inen açık şerit
                c.Rect(x + w / 2 - 9, top + 2, 18, h * 0.42f, Color.white, 0.22f);
            }
        }

        /// <summary>Streç film: açık örtü, geniş eğik parlama (yüzün <paramref name="at"/> oranında), yatay sargı kenarları.</summary>
        static void StretchFilm(Canvas c, float x, float y, float w, float h, float at)
        {
            c.Rect(x, y, w, h, Color.white, 0.1f);
            float cx = x + w * at, slope = 0.45f;
            for (int yy = Mathf.FloorToInt(y); yy < y + h; yy++)
            {
                float mid = cx + (yy - y - h / 2) * slope;
                for (int i = -40; i <= 40; i++)
                {
                    float t = Mathf.Abs(i) / 40f;
                    c.Blend(Mathf.RoundToInt(mid + i), yy, Color.white, 0.16f * (1 - t * t));
                }
            }
            for (float yy = y + 12; yy < y + h; yy += 26) { c.Rect(x, yy, w, 2, Color.white, 0.14f); c.Rect(x, yy + 2, w, 1, Color.black, 0.06f); }
        }

        static void Pallet(Canvas c, Color wood, float x, float y, float w, float h)
        {
            // Üst tahta, üç takoz, çatal boşlukları, alt tahta
            c.Rect(x, y, w, h, Color.black, 0.32f);
            c.Rect(x, y, w, 6, wood, 0.8f); c.Bevel(x, y, w, 6, 1.5f, 0.15f, 0.25f);
            foreach (float bx in new[] { 0f, w / 2 - 28, w - 56 })
            {
                c.Rect(x + bx, y + 6, 56, h - 10, wood, 0.75f);
                c.Bevel(x + bx, y + 6, 56, h - 10, 2, 0.1f, 0.24f);
            }
            c.Rect(x, y + h - 4, w, 4, wood, 0.7f);
            for (float k = 30; k < w; k += 70) c.Rect(x + k, y + 1, 2, 4, Color.black, 0.2f);
        }

        // ================================================================ ru · Sibirya veri merkezi: sandviç panel kabin

        /// <summary>Sandviç panel: ince yatay mikro nervürler, boydan iki dikey derz (koyu oluk + açık kapak), üst ve alt
        /// kaplama, katlar arası kuşak, dipte ve derz diplerinde kırağı.</summary>
        static void RuSide(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            for (float y = 4; y < SideH; y += 9) { c.Rect(0, y, SideW, 1.5f, Color.white, 0.09f); c.Rect(0, y + 1.5f, SideW, 1.5f, Color.black, 0.07f); }
            foreach (float x in new[] { 170f, 342f }) Joint(c, a, x, 0, SideH, true);
            c.Rect(0, 0, SideW, 6, b, 0.25f); c.Rect(0, 5, SideW, 1.5f, Color.black, 0.2f);
            c.Rect(0, UpperBottom - 5, SideW, 5, a, 0.3f); c.Rect(0, LowerTop, SideW, 5, b, 0.2f);
            c.Rect(0, SideH - 9, SideW, 9, a, 0.38f);
            c.Bevel(0, SideH - 9, SideW, 9, 1.5f, 0.15f, 0.2f);
            Frost(c, b, 0, SideH - 32, SideW, 23, 260);
            foreach (float x in new[] { 170f, 342f }) Frost(c, b, x - 10, SideH - 60, 26, 52, 60);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Kabin köşesi: dik nervürler, iki uçta köşe kaplaması, katlar arasında yatay derz, dipte kırağı.</summary>
        static void RuColumn(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            for (float x = 4; x < SideW; x += 14) { c.Rect(x, 0, 3, SideH, Color.white, 0.1f); c.Rect(x + 3, 0, 3, SideH, Color.black, 0.08f); }
            foreach (float x in new[] { 0f, SideW - 22f }) { c.Rect(x, 0, 22, SideH, b, 0.3f); c.Bevel(x, 0, 22, SideH, 2.5f, 0.15f, 0.26f); }
            Joint(c, a, 0, UpperBottom - 6, SideW, false);
            c.Rect(0, SideH - 9, SideW, 9, a, 0.38f);
            Frost(c, b, 0, SideH - 32, SideW, 23, 260);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Kabin çatısı: x boyunca trapez nervürler, kenar kaplaması, kar ve kırağı lekeleri, küçük havalandırma kapağı.</summary>
        static void RuTop(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.Top);
            for (float y = 18; y < SquareSize - 10; y += 46)
            {
                c.Rect(0, y, SquareSize, 6, Color.white, 0.2f);
                c.Rect(0, y + 6, SquareSize, 8, b, 0.1f);
                c.Rect(0, y + 14, SquareSize, 4, Color.black, 0.22f);
            }
            for (int i = 0; i < 9; i++) c.Stain(Rnd(0, SquareSize), Rnd(0, SquareSize), Rnd(16, 34), b, 0.16f);
            c.Rect(176, 186, 40, 30, a, 0.45f);
            c.Bevel(176, 186, 40, 30, 2, 0.14f, 0.3f);
            for (float y = 192; y < 212; y += 5) c.Rect(180, y, 32, 2, Color.black, 0.3f);
            c.Bevel(0, 0, SquareSize, SquareSize, 7, 0.18f, 0.24f);
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Isıtıcı hattının yalıtım keçesi: lifli koyu taban, seyrek kırağı (dikişsiz).</summary>
        static void RuChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail; var b = p.B;
            c.Fill(p.Channel);
            c.Grain(6, 32);
            for (int i = 0; i < 260; i++)
            {
                float x = Rnd(0, SquareSize), y = Rnd(0, SquareSize), ang = Rnd(0, Mathf.PI), len = Rnd(4, 12);
                c.Line(x, y, x + Mathf.Cos(ang) * len, y + Mathf.Sin(ang) * len, 1.4f, d, 0.55f);
            }
            c.Speckle(0, 0, SquareSize, SquareSize, 70, 0.6f, 1.6f, b, 0.08f, 0.2f);
            c.Noise(4);
        }

        /// <summary>Kırağılı yalıtımlı çukur: çerçevenin çevresinde kırağı, içi koyu keçe.</summary>
        static void RuWell(Canvas c, Palette p)
        {
            var d = p.WellDetail; var b = p.B;
            c.Fill(p.Well);
            c.Grain(6, 32);
            float m = HubMin, s = HubSize, rim = 10;
            c.Rect(m, m, s, s, d, 0.55f);
            c.Bevel(m, m, s, s, 3, 0.12f, 0.3f);
            c.Rect(m + rim, m + rim, s - 2 * rim, s - 2 * rim, Color.black, 0.5f);
            Frost(c, b, m, m, s, rim + 6, 120);
            Frost(c, b, m, m + s - rim - 6, s, rim + 6, 120);
            Frost(c, b, m, m, rim + 6, s, 120);
            Frost(c, b, m + s - rim - 6, m, rim + 6, s, 120);
            c.Noise(4);
        }

        static void Joint(Canvas c, Color a, float at, float from, float length, bool vertical)
        {
            if (vertical)
            {
                c.Rect(at - 3, from, 6, length, a, 0.58f);
                c.Rect(at + 3, from, 7, length, Color.white, 0.14f);
                for (float y = from + 10; y < from + length; y += 24) c.Circle(at + 6.5f, y, 1.8f, a, 0.5f);
            }
            else
            {
                c.Rect(from, at, length, 4, a, 0.55f);
                c.Rect(from, at + 4, length, 4, Color.white, 0.14f);
            }
        }

        static void Frost(Canvas c, Color frost, float x, float y, float w, float h, int count)
        {
            c.Speckle(x, y, w, h, count, 0.7f, 2.2f, frost, 0.1f, 0.3f);
            c.VerticalFadeRect(x, y, w, h, frost, 0, 0.16f);
        }
    }
}
