using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Kalan altı kitin motifleri (BASKIN-MEKANLARI Katman 3 tablosu): in, cn, jk, sea, sa, oc. Ölçek ve kurallar
    /// RaidKitTextureBaker.Motifs.cs'deki gibi: dikey çizgiler ≥ 6 px, yatay çizgiler ≥ 3 px; yan dokuda dünyadaki bir daire
    /// yatayda 1,87 kat geniş çizilir.
    /// <para>Kanal dokusu karonun uzayında: göbek UV 0,2–0,8 karesi (dokuda <see cref="HubMin"/>–<see cref="HubMax"/>);
    /// kuzey–güney kolları u 0,2–0,8 bandında v 0–0,2 ve 0,8–1, doğu–batı kolları v 0,2–0,8 bandında. Bu yüzden yöne bağlı içerik
    /// (şerit kablo, kablo zinciri, kablo demeti, baklava sac kenar) dikey bantta dikey, yatay bantta yatay çizilir ve kollar hangi
    /// yönde olursa olsun kendi boylarınca okunur; göbek kendi içeriğini alır. Bakır koşuların ekseni u (ya da v) 0,38 / 0,5 / 0,62
    /// (<see cref="CopperAt"/>); içerik bakırın altında kalır.</para>
    /// </summary>
    public static partial class RaidKitTextureBaker
    {
        /// <summary>Kanaldaki üç bakır koşunun dokudaki ekseni (x = 0,5 ± 0,12 birim).</summary>
        static readonly float[] CopperAt = { SquareSize * 0.38f, Mid, SquareSize * 0.62f };

        // ================================================================ in · Çağrı merkezi: bölme paneli ve dosya dolabı istifi

        /// <summary>Üst katta ayakta bölme panelleri (kumaş yüz, alüminyum çerçeve, aralarında bağlantı dikmesi); alt katta yan yana üç
        /// yatay dosya dolabı (iki çekmece: gömme kulp, etiket çerçevesi; ortadakinde kilit).</summary>
        static void InSide(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            var (ut, ub) = Layers[0];
            PartitionPanels(c, a, b, ut, ub, new[] { -40f, 220f, 470f, 730f });
            var (lt, lb) = Layers[1];
            float[] cab = { 0, 171, 342, SideW };
            for (int k = 0; k + 1 < cab.Length; k++)
            {
                FilingCabinet(c, b, cab[k], lt, cab[k + 1] - cab[k], lb - lt, 2, 0.5f);
                if (k == 1) c.Circle(cab[k] + (cab[k + 1] - cab[k]) - 22, lt + 9, 3.6f, Color.black, 0.45f);
            }
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Ayakta iki dikey dosya dolabı yan yana (her biri dört çekmece; katlar çekmece aralığına denk), üstte kilit.</summary>
        static void InColumn(Canvas c, Palette p)
        {
            var b = p.B;
            c.Fill(p.West);
            for (int k = 0; k < 2; k++)
            {
                float x = k * SideW / 2f + (k == 0 ? 0 : 3), w = SideW / 2f - 3;
                for (int i = 0; i < 2; i++)
                {
                    var (top, bottom) = Layers[i];
                    FilingCabinet(c, b, x, top, w, bottom - top, 2, 0.32f);
                }
                c.Circle(x + w - 26, 8, 3.6f, Color.black, 0.45f);
            }
            c.Rect(SideW / 2f - 3, 0, 6, SideH, Color.black, 0.4f);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Üstte boylu boyunca yatırılmış iki bölme paneli (kumaş yüz, alüminyum çerçeve, uçlarda bağlantı ayağı), üstünde bir
        /// dosya klasörü ve iki kâğıt.</summary>
        static void InTop(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.Top);
            for (int k = 0; k < 2; k++)
            {
                float y = k * 128f;
                c.Rect(0, y, SquareSize, 128, b, 0.7f);
                c.Bevel(0, y, SquareSize, 128, 3, 0.2f, 0.3f);
                c.Rect(12, y + 10, SquareSize - 24, 108, a, 0.9f);
                c.Inset(12, y + 10, SquareSize - 24, 108, 2.5f, 0.1f, 0.3f);
                c.Speckle(14, y + 12, SquareSize - 28, 104, 500, 0.6f, 1.4f, Color.white, 0.03f, 0.08f);
                foreach (float x in new[] { 0f, SquareSize - 14f }) { c.Rect(x, y + 40, 14, 48, b, 0.8f); c.Bevel(x, y + 40, 14, 48, 2, 0.15f, 0.3f); }
            }
            // Klasör ve kâğıtlar
            c.Rect(70, 50, 86, 62, Color.black, 0.16f);
            c.Rect(66, 46, 86, 62, b, 0.85f);
            c.Bevel(66, 46, 86, 62, 2, 0.12f, 0.25f);
            c.Rect(76, 56, 34, 12, Color.white, 0.2f);
            c.Rect(150, 150, 54, 68, Color.black, 0.12f);
            c.Rect(146, 146, 54, 68, Color.white, 0.4f);
            c.Rect(160, 140, 50, 64, Color.white, 0.32f);
            for (float y = 158; y < 200; y += 8) c.Rect(166, y, 34, 2, Color.black, 0.08f);
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Kolların boyunca yassı gri şerit kablolar (ince sırtlı), rayın hemen içinde pirinç kenar profili; göbekte şeritler
        /// dikine kesişir.</summary>
        static void InChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail; var brass = p.WellDetail;
            c.Fill(p.Channel);
            c.Grain(5, 32);
            // Önce yatay (doğu–batı) sonra dikey (kuzey–güney) şeritler: göbekte dikine kesişirler
            (float at, float w)[] ribbons = { (62, 14), (79, 11), (106, 13), (137, 13), (166, 11), (180, 14) };
            foreach (bool vertical in new[] { false, true })
                foreach (var (at, w) in ribbons) Ribbon(c, d, at, w, vertical, 0, SquareSize);
            // Pirinç kenar profili: yalnız kollarda, rayların (u 0,1975–0,2225 / 0,7775–0,8025) iç yanında
            foreach (float at in new[] { 57f, 194f })
                foreach (var (from, len) in new[] { (0f, HubMin), (HubMax, SquareSize - HubMax) })
                {
                    c.Rect(at, from, 5, len, brass, 0.85f); c.Rect(at, from, 1.5f, len, Color.white, 0.18f);
                    c.Rect(from, at, len, 5, brass, 0.85f); c.Rect(from, at, len, 1.5f, Color.white, 0.18f);
                }
            c.Noise(4);
        }

        /// <summary>Halı karo içinde döşeme priz kutusu: pirinç çerçeve, koyu iç, iki priz bloğu ve kablo rakoru.</summary>
        static void InWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            c.Grain(6, 16);
            c.Speckle(0, 0, SquareSize, SquareSize, 600, 0.6f, 1.2f, Color.white, 0.03f, 0.07f);
            float m = HubMin + 10, s = HubSize - 20, rim = 8;
            c.Rect(m, m, s, s, d, 0.9f);
            c.Bevel(m, m, s, s, 2.5f, 0.22f, 0.3f);
            c.Rect(m + rim, m + rim, s - 2 * rim, s - 2 * rim, Color.black, 0.55f);
            c.Inset(m + rim, m + rim, s - 2 * rim, s - 2 * rim, 3, 0.06f, 0.3f);
            foreach (var (x, y) in new[] { (m + 16, m + 16), (m + s - 56, m + s - 46) })
            {
                Pillow(c, x, y, 40, 30, 6, 6, Color.white, 0.22f, 0.1f, 0.2f);
                foreach (float dx in new[] { 12f, 28f }) c.Rect(x + dx - 1.5f, y + 9, 3, 9, Color.black, 0.5f);
            }
            c.Ring(m + s - 34, m + 34, 7, 12, d, 0.7f);
            c.Circle(m + s - 34, m + 34, 7, Color.black, 0.5f);
            c.Noise(4);
        }

        /// <summary>Bölme panelleri: <paramref name="edges"/> arasında birer panel (alüminyum çerçeve, kumaş yüz, altta kablo yarığı),
        /// birleşimde bağlantı dikmesi.</summary>
        static void PartitionPanels(Canvas c, Color fabric, Color alu, float top, float bottom, float[] edges)
        {
            float h = bottom - top;
            for (int k = 0; k + 1 < edges.Length; k++)
            {
                float x = edges[k], w = edges[k + 1] - x;
                c.Rect(x, top, w, h, alu, 0.75f);
                c.Bevel(x, top, w, h, 2, 0.18f, 0.28f);
                c.Rect(x + 12, top + 6, w - 24, h - 16, fabric, 0.92f);
                c.Inset(x + 12, top + 6, w - 24, h - 16, 2, 0.1f, 0.3f);
                // Kumaşın dokusu: seyrek açık lif ve hafif dikey kumaş katları
                c.Speckle(x + 14, top + 8, w - 28, h - 20, (int)(w * 0.9f), 0.6f, 1.3f, Color.white, 0.03f, 0.08f);
                for (float xx = x + 40; xx < x + w - 30; xx += 64) c.Rect(xx, top + 8, 8, h - 20, Color.white, 0.035f);
                c.Rect(x + w / 2 - 20, bottom - 8, 40, 4, Color.black, 0.4f);
            }
            for (int k = 1; k + 1 < edges.Length; k++)
            {
                c.Rect(edges[k] - 7, top, 14, h, alu, 0.9f);
                c.Bevel(edges[k] - 7, top, 14, h, 2, 0.2f, 0.3f);
                for (float y = top + 12; y < bottom - 6; y += 22) c.Circle(edges[k], y, 1.8f, Color.black, 0.4f);
            }
        }

        /// <summary>Dosya dolabı cephesi: <paramref name="drawers"/> çekmece (aralık, kabartma, gömme kulp, etiket çerçevesi).</summary>
        static void FilingCabinet(Canvas c, Color steel, float x, float y, float w, float h, int drawers, float handleWidth)
        {
            c.Rect(x, y, w, h, steel, 0.6f);
            c.Bevel(x, y, w, h, 2, 0.16f, 0.3f);
            float dh = (h - 4) / drawers;
            for (int k = 0; k < drawers; k++)
            {
                float dy = y + 2 + k * dh;
                c.Rect(x + 4, dy, w - 8, 2.5f, Color.black, 0.35f);
                c.Bevel(x + 6, dy + 3, w - 12, dh - 4, 2, 0.14f, 0.24f);
                float hw = w * handleWidth, hx = x + (w - hw) / 2, hy = dy + dh * 0.5f;
                c.Rect(hx, hy, hw, 5, Color.black, 0.45f);
                c.Rect(hx, hy + 5, hw, 1.5f, Color.white, 0.2f);
                c.Rect(hx + hw * 0.3f, hy - 12, hw * 0.4f, 8, Color.white, 0.16f);
                c.Inset(hx + hw * 0.3f, hy - 12, hw * 0.4f, 8, 1, 0.05f, 0.25f);
            }
        }

        /// <summary>Yassı şerit kablo: açık gri bant, ince boyuna sırtlar, koyu kenar.</summary>
        static void Ribbon(Canvas c, Color d, float at, float w, bool vertical, float from, float len)
        {
            if (len <= 0) return;
            void R(float o, float ww, Color col, float alpha)
            {
                if (vertical) c.Rect(at + o, from, ww, len, col, alpha);
                else c.Rect(from, at + o, len, ww, col, alpha);
            }
            R(0, w, d, 0.85f);
            for (float o = 2; o < w - 1; o += 3) R(o, 1, Color.black, 0.18f);
            R(0, 1.5f, Color.white, 0.14f);
            R(w - 1.5f, 1.5f, Color.black, 0.35f);
        }

        // ================================================================ cn · Montaj fabrikası: istiflenmiş parça kasaları

        /// <summary>Her katta yan yana plastik parça kasaları (şaşırtmalı): dudaklı ağız, dik kaburgalar, tutamak yuvaları, etiket cebi,
        /// taban ayağı.</summary>
        static void CnSide(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            for (int i = 0; i < 2; i++)
            {
                var (top, bottom) = Layers[i];
                c.Rect(0, top, SideW, bottom - top, Color.black, 0.3f);
                float w = 128, off = i == 0 ? -64 : 0;
                for (float x = off; x < SideW; x += w) Bin(c, a, b, x + 2, top + 1.5f, w - 4, bottom - top - 3, false);
            }
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Dik kasa kulesi: dört sıra, sırada üç kasa uçtan (şaşırtmalı; kaburgalı uç, tutamak oyuğu).</summary>
        static void CnColumn(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            c.Rect(0, 0, SideW, SideH, Color.black, 0.3f);
            const float w = 172;
            for (int i = 0; i < 2; i++)
            {
                var (top, bottom) = Layers[i];
                float rh = (bottom - top) / 2;
                for (int r = 0; r < 2; r++)
                    for (float x = ((i * 2 + r) % 2) * -w / 2; x < SideW; x += w)
                        Bin(c, a, b, x + 2, top + r * rh + 1.5f, w - 4, rh - 3, true);
            }
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Üstte 2 × 2 açık kasa: kalın dudak, koyu iç, içinde tek tür iri parça (pul, somun ya da cıvata).</summary>
        static void CnTop(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.Top);
            for (int j = 0; j < 2; j++)
                for (int i = 0; i < 2; i++)
                {
                    float x = i * 128f, y = j * 128f;
                    c.Rect(x + 1, y + 1, 126, 126, a, 0.85f);
                    c.Bevel(x + 1, y + 1, 126, 126, 3, 0.2f, 0.32f);
                    c.Rect(x + 13, y + 13, 102, 102, Color.black, 0.26f);
                    c.Inset(x + 13, y + 13, 102, 102, 5, 0.08f, 0.3f);
                    c.Rect(x + 48, y + 4, 32, 6, Color.black, 0.35f);
                    // Kasada tek tür parça, iri ve seyrek (oyun ölçeğinde ≥ 6 px): pullar, somunlar ya da cıvatalar
                    int kind = (i + 2 * j) % 3;
                    for (int n = 0; n < 6; n++)
                    {
                        float px = x + 28 + (n % 3) * 36 + Rnd(-6, 6), py = y + 40 + (n / 3) * 46 + Rnd(-6, 6);
                        switch (kind)
                        {
                            case 0: c.Ring(px + 2, py + 2, 5, 12, Color.black, 0.22f); c.Ring(px, py, 5, 12, b, 0.5f); break;
                            case 1: c.Circle(px + 2, py + 2, 11, Color.black, 0.22f); c.Circle(px, py, 11, b, 0.45f); c.Circle(px, py, 5, Color.black, 0.35f); break;
                            default:
                            {
                                // Altıgen başlı cıvata yandan: gövde ve ona dik kalın baş (T)
                                float ang = Rnd(0.3f, 1.2f), ux = Mathf.Cos(ang), uy = Mathf.Sin(ang), dx = ux * 14, dy = uy * 14;
                                c.Line(px - dx + 2, py - dy + 2, px + dx + 2, py + dy + 2, 7, Color.black, 0.22f);
                                c.Line(px - dx, py - dy, px + dx, py + dy, 7, b, 0.45f);
                                float hx = px - dx - ux * 3, hy = py - dy - uy * 3;
                                c.Line(hx + uy * 9, hy - ux * 9, hx - uy * 9, hy + ux * 9, 8, b, 0.55f);
                                break;
                            }
                        }
                    }
                }
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Kolların boyunca kablo zinciri (yan baklaları, mafsal pimleri, sık köprüleri); göbekte zinciri tutan cıvatalı
        /// kılavuz plakası.</summary>
        static void CnChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(5, 32);
            foreach (bool vertical in new[] { false, true })
            {
                void R(float u, float v, float w, float h, Color col, float alpha)
                {
                    if (vertical) c.Rect(u, v, w, h, col, alpha); else c.Rect(v, u, h, w, col, alpha);
                }
                float u0 = 64, u1 = 192;
                R(u0, 0, u1 - u0, SquareSize, Color.black, 0.25f);
                for (float v = 0; v < SquareSize; v += 32)
                {
                    foreach (float u in new[] { u0, u1 - 14 })
                    {
                        R(u, v + 1, 14, 30, d, 0.95f);
                        R(u, v + 1, 14, 2, Color.white, 0.2f);
                        R(u, v + 29, 14, 2, Color.black, 0.4f);
                    }
                    foreach (float dv in new[] { 6f, 22f }) { R(u0 + 14, v + dv, u1 - u0 - 28, 5, d, 0.85f); R(u0 + 14, v + dv, u1 - u0 - 28, 1.5f, Color.white, 0.14f); }
                }
            }
            // Göbek: kılavuz plakası
            c.Rect(HubMin, HubMin, HubSize, HubSize, p.Channel, 1);
            float m = HubMin + 8, s = HubSize - 16;
            c.Rect(m, m, s, s, d, 0.8f);
            c.Bevel(m, m, s, s, 3, 0.16f, 0.32f);
            foreach (var (x, y) in new[] { (m + 14, m + 14), (m + s - 14, m + 14), (m + 14, m + s - 14), (m + s - 14, m + s - 14) })
            {
                c.Circle(x + 1, y + 1, 6, Color.black, 0.4f);
                c.Circle(x, y, 5.5f, d, 1);
                c.Circle(x - 1, y - 1, 2.5f, Color.white, 0.2f);
            }
            c.Noise(4);
        }

        /// <summary>Robotun döşeme ankraj plakası: cıvatalı çelik plaka, ortada kablo çıkış deliği.</summary>
        static void CnWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            c.Grain(6, 32);
            float m = HubMin + 6, s = HubSize - 12;
            c.Rect(m, m, s, s, d, 0.85f);
            c.Bevel(m, m, s, s, 3, 0.18f, 0.32f);
            foreach (var (x, y) in new[] { (m + 18, m + 18), (m + s - 18, m + 18), (m + 18, m + s - 18), (m + s - 18, m + s - 18) })
            {
                c.Circle(x + 1.5f, y + 1.5f, 9, Color.black, 0.35f);
                c.Circle(x, y, 8.5f, d, 1);
                c.Bevel(x - 6, y - 6, 12, 12, 2, 0.2f, 0.3f);
                c.Circle(x, y, 3, Color.black, 0.4f);
            }
            c.Circle(Mid, Mid, 40, Color.black, 0.65f);
            c.Ring(Mid, Mid, 40, 46, Color.black, 0.25f);
            c.Ring(Mid, Mid, 37, 40, Color.white, 0.1f);
            c.Noise(4);
        }

        /// <summary>Plastik parça kasası: üstte dudak ve altında gölge, dik kaburgalar, tutamak yuvası, etiket cebi, taban ayağı.
        /// <paramref name="end"/>: kasanın ucu (iri tutamak oyuğu, iki kaburga).</summary>
        static void Bin(Canvas c, Color plastic, Color label, float x, float y, float w, float h, bool end)
        {
            c.Rect(x, y, w, h, plastic, 0.92f);
            c.Rect(x, y, w, 6, Color.white, 0.12f);
            c.Rect(x, y + 6, w, 2.5f, Color.black, 0.32f);
            c.Rect(x, y + h - 4, w, 4, Color.black, 0.25f);
            c.Rect(x, y, 6, h, Color.white, 0.08f);
            c.Rect(x + w - 6, y, 6, h, Color.black, 0.2f);
            float[] ribs = end ? new[] { 0.06f, 0.2f, 0.34f, 0.66f, 0.8f, 0.94f } : new[] { 0.08f, 0.28f, 0.72f, 0.92f };
            foreach (float t in ribs)
            {
                float rx = x + w * t - 4;
                c.Rect(rx, y + 9, 7, h - 13, Color.white, 0.1f);
                c.Rect(rx + 7, y + 9, 3, h - 13, Color.black, 0.18f);
            }
            if (end)
            {
                // Uçta kaburgalar arasında tutamak oyuğu (dudağın hemen altında)
                Pillow(c, x + w / 2 - 34, y + 10, 68, Mathf.Max(6, h * 0.26f), 14, 4, Color.black, 0.6f, 0, 0);
            }
            else
            {
                // Uzun yanda ortada etiket cebi
                c.Rect(x + w / 2 - 18, y + 13, 36, h * 0.32f, label, 0.55f);
                c.Inset(x + w / 2 - 18, y + 13, 36, h * 0.32f, 1.5f, 0.05f, 0.3f);
            }
        }

        // ================================================================ jk · Çip fabrikası: sökülmüş pano gövdeleri

        /// <summary>Her katta yatırılmış iki pano gövdesi (kapak yüzü dışarıda): gövde kenarı, kapı çizgisi, panjur yarıkları, menteşe ve
        /// kilit; alt katın dibinde kaide.</summary>
        static void JkSide(Canvas c, Palette p)
        {
            var a = p.A;
            c.Fill(p.West);
            var (ut, ub) = Layers[0];
            Housing(c, a, -30, ut, 262, ub - ut, true);
            Housing(c, a, 232, ut, 310, ub - ut, false);
            var (lt, lb) = Layers[1];
            Housing(c, a, 0, lt, 300, lb - lt - 8, false);
            Housing(c, a, 300, lt, 212, lb - lt - 8, true);
            c.Rect(0, lb - 8, SideW, 8, a, 0.7f);
            c.Rect(0, lb - 8, SideW, 1.5f, Color.white, 0.12f);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Ayakta tek pano: iki kanat (ortada kapı çizgisi), her kanatta üstte ve altta panjur, kollu kilit, kaide.</summary>
        static void JkColumn(Canvas c, Palette p)
        {
            var a = p.A;
            c.Fill(p.West);
            float post = 22, plinth = 10;
            foreach (float x in new[] { 0f, SideW - post }) { c.Rect(x, 0, post, SideH, a, 0.3f); c.Bevel(x, 0, post, SideH, 2, 0.15f, 0.28f); }
            c.Rect(0, 0, SideW, 7, a, 0.3f);
            float mid = SideW / 2f;
            foreach (var (x0, x1) in new[] { (post, mid - 3), (mid + 3, SideW - post) })
            {
                c.Bevel(x0 + 3, 9, x1 - x0 - 6, SideH - plinth - 12, 2.5f, 0.18f, 0.3f);
                float lx = (x0 + x1) / 2 - 50;
                Louvre(c, lx, 18, 100, 4, 8);
                Louvre(c, lx, SideH - plinth - 46, 100, 4, 8);
            }
            c.Rect(mid - 3, 6, 6, SideH - plinth - 6, Color.black, 0.45f);
            foreach (float hx in new[] { mid - 22, mid + 14 })
            {
                c.Rect(hx, 62, 8, 34, a, 0.85f);
                c.Rect(hx, 62, 2, 34, Color.white, 0.2f);
                c.Circle(hx + 4, 58, 4, Color.black, 0.4f);
            }
            c.Rect(0, SideH - plinth, SideW, plinth, a, 0.75f);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Panonun tavanı: kenar kabartması, köşelerde dört kaldırma mapası, ortada panjurlu havalandırma başlığı, bir kenarda
        /// rakor delikli kablo giriş plakası.</summary>
        static void JkTop(Canvas c, Palette p)
        {
            var a = p.A;
            c.Fill(p.Top);
            c.Bevel(0, 0, SquareSize, SquareSize, 7, 0.16f, 0.26f);
            c.Inset(16, 16, SquareSize - 32, SquareSize - 32, 2, 0.08f, 0.2f);
            foreach (var (x, y) in new[] { (34f, 34f), (222f, 34f), (34f, 222f), (222f, 222f) })
            {
                c.Ring(x + 2, y + 2, 6, 12, Color.black, 0.25f);
                c.Ring(x, y, 6, 12, a, 0.85f);
                c.Ring(x - 1, y - 1, 9, 11, Color.white, 0.15f);
            }
            c.Rect(70, 82, 116, 70, a, 0.3f);
            c.Bevel(70, 82, 116, 70, 3, 0.18f, 0.32f);
            Louvre(c, 80, 92, 96, 6, 9);
            c.Rect(56, 182, 144, 34, a, 0.28f);
            c.Bevel(56, 182, 144, 34, 2, 0.15f, 0.3f);
            for (int k = 0; k < 4; k++) { c.Ring(80 + k * 32, 199, 6, 10, Color.black, 0.3f); c.Circle(80 + k * 32, 199, 6, a, 0.5f); }
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Koyu taban; bakır koşuların altında krem izolatör blokları (kollarda kolun ortasında, göbekte 3 × 3) ve onları
        /// taşıyan koyu çelik traversler.</summary>
        static void JkChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(5, 32);
            void Traverse(float cx, float cy, bool acrossX)
            {
                if (acrossX) { c.Rect(HubMin + 18, cy - 4, HubSize - 36, 8, Color.white, 0.1f); c.Rect(HubMin + 18, cy + 4, HubSize - 36, 2, Color.black, 0.4f); }
                else { c.Rect(cx - 4, HubMin + 18, 8, HubSize - 36, Color.white, 0.1f); c.Rect(cx + 4, HubMin + 18, 2, HubSize - 36, Color.black, 0.4f); }
            }
            // Kollar: kolun ortasında enine travers ve üstünde üç izolatör (kuzey–güney kolunda yatay, doğu–batı kolunda dikey)
            foreach (float v in new[] { 26f, SquareSize - 26f })
            {
                Traverse(0, v, true); Traverse(v, 0, false);
                foreach (float u in CopperAt) { Insulator(c, d, u, v, true); Insulator(c, d, v, u, false); }
            }
            foreach (float v in CopperAt) foreach (float u in CopperAt) Insulator(c, d, u, v, true);
            c.Noise(4);
        }

        /// <summary>Temiz oda yükseltilmiş döşemesinin delikli karosu: çerçeve, sık delik ızgarası, deliklerin altı koyu.</summary>
        static void JkWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            c.Grain(5, 32);
            float m = HubMin + 4, s = HubSize - 8, rim = 10;
            c.Rect(m, m, s, s, d, 0.85f);
            c.Bevel(m, m, s, s, 3, 0.2f, 0.32f);
            c.Rect(m + rim, m + rim, s - 2 * rim, s - 2 * rim, d, 0.5f);
            c.Inset(m + rim, m + rim, s - 2 * rim, s - 2 * rim, 2, 0.08f, 0.3f);
            for (float y = m + rim + 6; y < m + s - rim - 3; y += 9)
                for (float x = m + rim + 6; x < m + s - rim - 3; x += 9) c.Circle(x, y, 2.6f, Color.black, 0.6f);
            c.Noise(4);
        }

        /// <summary>Pano gövdesi (kapak yüzü): gövde kenarı, kapı çizgisi, bir uçta iki sütun panjur, öbür uçta menteşeler, kilit.</summary>
        static void Housing(Canvas c, Color dark, float x, float y, float w, float h, bool louvreRight)
        {
            c.Bevel(x, y, w, h, 3, 0.2f, 0.32f);
            float dx = x + 9, dy = y + 7, dw = w - 18, dh = h - 14;
            c.Rect(dx, dy, dw, 3, Color.black, 0.32f);
            c.Rect(dx, dy + dh - 3, dw, 3, Color.black, 0.32f);
            c.Rect(dx, dy, 6, dh, Color.black, 0.26f);
            c.Rect(dx + dw - 6, dy, 6, dh, Color.black, 0.26f);
            c.Bevel(dx + 6, dy + 3, dw - 12, dh - 6, 2, 0.12f, 0.2f);
            float lx = louvreRight ? dx + dw - 104 : dx + 20;
            Louvre(c, lx, dy + 9, 40, Mathf.Max(2, (int)((dh - 18) / 9)), 9);
            Louvre(c, lx + 48, dy + 9, 40, Mathf.Max(2, (int)((dh - 18) / 9)), 9);
            float hx = louvreRight ? dx + 10 : dx + dw - 18;
            foreach (float hy in new[] { dy + 8, dy + dh - 20 }) c.Rect(hx, hy, 8, 12, dark, 0.6f);
            float kx = louvreRight ? dx + dw * 0.4f : dx + dw * 0.62f, ky = dy + dh / 2;
            c.Circle(kx, ky, 5.5f, dark, 0.8f);
            c.Rect(kx - 1.5f, ky - 4, 3, 8, Color.black, 0.5f);
        }

        /// <summary>Panjur: <paramref name="rows"/> sıra yatay yarık (koyu yarık, altında açık dudak).</summary>
        static void Louvre(Canvas c, float x, float y, float w, int rows, float pitch)
        {
            for (int k = 0; k < rows; k++)
            {
                float yy = y + k * pitch;
                c.Rect(x, yy, w, 4, Color.black, 0.5f);
                c.Rect(x, yy + 4, w, 2, Color.white, 0.22f);
            }
        }

        /// <summary>Krem izolatör bloğu (bakırın altında görünen uçları), gölgesiyle.</summary>
        static void Insulator(Canvas c, Color cream, float cx, float cy, bool acrossX)
        {
            float w = acrossX ? 20 : 12, h = acrossX ? 12 : 20;
            c.Rect(cx - w / 2 + 1, cy - h / 2 + 3, w, h, Color.black, 0.4f);
            c.Rect(cx - w / 2, cy - h / 2, w, h, cream, 0.9f);
            c.Bevel(cx - w / 2, cy - h / 2, w, h, 1.5f, 0.2f, 0.35f);
        }

        // ================================================================ sea · Elektronik fabrikası: kum torbası barikatı

        /// <summary>Dört sıra kum torbası (katta iki sıra, şaşırtmalı): yastık gölgesi, örgü dokusu, bağlı uçta kıvrım; aralar koyu;
        /// alt katın dibinde dalgalı kenarlı ıslak koyu bant.</summary>
        static void SeaSide(Canvas c, Palette p)
        {
            c.Fill(p.West);
            c.Rect(0, 0, SideW, SideH, Color.black, 0.4f);
            for (int i = 0; i < 2; i++)
            {
                var (top, bottom) = Layers[i];
                float ch = (bottom - top) / 2, len = 150;
                for (int j = 0; j < 2; j++)
                {
                    float y = top + j * ch, off = ((i * 2 + j) % 2) * len / 2 - Rnd(0, 24);
                    int n = 0;
                    for (float x = off - len; x < SideW + len; x += len, n++)
                        Sandbag(c, p.B, x + 2, y + 1.5f, len - 4, ch - 3, 1.87f, (n + j) % 2 == 0);
                }
            }
            WetBand(c, p.A, LowerBottom - 46, LowerBottom, SideW);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Dik torba kulesi: dört sıra, torbalar uçtan (oval ağızlar, şaşırtmalı), dipte ıslak bant.</summary>
        static void SeaColumn(Canvas c, Palette p)
        {
            c.Fill(p.West);
            c.Rect(0, 0, SideW, SideH, Color.black, 0.4f);
            for (int i = 0; i < 2; i++)
            {
                var (top, bottom) = Layers[i];
                float ch = (bottom - top) / 2, w = 92;
                for (int j = 0; j < 2; j++)
                {
                    float y = top + j * ch, off = ((i * 2 + j) % 2) * w / 2;
                    for (float x = off - w; x < SideW + w; x += w)
                    {
                        Pillow(c, x + 2, y + 1.5f, w - 4, ch - 3, (w - 4) / 2 * 0.8f, (ch - 3) / 2 * 0.8f, p.B, 0.95f, 0.22f, 0.42f);
                        Disc(c, x + w / 2, y + ch / 2, 9, 5, Color.black, 0.18f);
                    }
                }
            }
            WetBand(c, p.A, LowerBottom - 46, LowerBottom, SideW);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Üst sıra torbalar yukarıdan: iki sıra boyuna yatmış torba (şaşırtmalı uçlar, kıvrımlı bağ), örgü, birkaç ıslak leke.</summary>
        static void SeaTop(Canvas c, Palette p)
        {
            c.Fill(p.Top);
            c.Rect(0, 0, SquareSize, SquareSize, Color.black, 0.38f);
            float len = 172;
            for (int j = 0; j < 2; j++)
            {
                float y = j * 128f, off = j * len / 2 - 40;
                int n = 0;
                for (float x = off - len; x < SquareSize + len; x += len, n++)
                    Sandbag(c, p.B, x + 3, y + 4, len - 6, 120, 1, (n + j) % 2 == 0);
            }
            for (int i = 0; i < 4; i++) c.Stain(Rnd(20, 236), Rnd(20, 236), Rnd(16, 30), p.A, 0.18f);
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Koyu reçine su: yavaş alaca, birkaç halka, suyun altında kıvrılan düşmüş bakır kablo (dikişsiz).</summary>
        static void SeaChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(6, 64);
            WavyCable(c, d, 0.42f, 128, 34, 1, true);
            WavyCable(c, d, 0.36f, 76, 22, 2, false);
            foreach (var (x, y) in new[] { (60f, 190f), (196f, 70f) })
                for (int k = 0; k < 3; k++) c.Ring(x, y, 8 + k * 11, 9.5f + k * 11, Color.white, 0.06f - k * 0.015f);
            c.Speckle(0, 0, SquareSize, SquareSize, 40, 0.6f, 1.4f, Color.white, 0.04f, 0.1f);
            c.Noise(3);
        }

        /// <summary>Su dolu çukur: ıslak beton kenar, koyu su, ortada yer süzgeci (halka ve yarıklar), çevresinde halkalar.</summary>
        static void SeaWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            c.Grain(6, 64);
            float m = HubMin, s = HubSize, rim = 12;
            c.Rect(m, m, s, s, d, 0.6f);
            c.Bevel(m, m, s, s, 3, 0.14f, 0.3f);
            c.Rect(m + rim, m + rim, s - 2 * rim, s - 2 * rim, p.Well, 1);
            c.Rect(m + rim, m + rim, s - 2 * rim, s - 2 * rim, Color.black, 0.2f);
            c.Inset(m + rim, m + rim, s - 2 * rim, s - 2 * rim, 3, 0.04f, 0.3f);
            c.Ring(Mid, Mid, 30, 42, d, 0.75f);
            c.Circle(Mid, Mid, 30, Color.black, 0.45f);
            for (int k = 0; k < 8; k++)
            {
                float ang = k * Mathf.PI / 4;
                c.Line(Mid + Mathf.Cos(ang) * 14, Mid + Mathf.Sin(ang) * 14, Mid + Mathf.Cos(ang) * 28, Mid + Mathf.Sin(ang) * 28, 4, d, 0.6f);
            }
            for (int k = 0; k < 2; k++) c.Ring(Mid, Mid, 52 + k * 12, 54 + k * 12, Color.white, 0.06f);
            c.Noise(3);
        }

        /// <summary>Kum torbası: yastık gölgeli dolu gövde, ince örgü çizgileri, bağlı ucunda iki kıvrım ve bağ.
        /// <paramref name="aspect"/>: yatay / dikey doku ölçeği (yan doku 1,87, üst doku 1).</summary>
        static void Sandbag(Canvas c, Color bag, float x, float y, float w, float h, float aspect, bool tiedRight)
        {
            float ry = h / 2 * 0.85f, rx = Mathf.Min(w / 2, ry * aspect);
            Pillow(c, x, y, w, h, rx, ry, bag, 0.95f, 0.24f, 0.45f, true);
            // Bağlı uç: ağız büzülür, iki kıvrım uca doğru daralır
            float ex = tiedRight ? x + w : x, sgn = tiedRight ? -1 : 1, cy = y + h / 2, inX = rx * 1.15f, tipX = rx * 0.25f;
            c.Line(ex + sgn * inX, cy - h * 0.3f, ex + sgn * tipX, cy - h * 0.05f, 3.5f, Color.black, 0.24f);
            c.Line(ex + sgn * inX, cy + h * 0.3f, ex + sgn * tipX, cy + h * 0.05f, 3.5f, Color.black, 0.24f);
        }

        /// <summary>Islak bant: <paramref name="top"/> ile <paramref name="bottom"/> arasında dalgalı üst kenarlı koyu ıslaklık, kenarında
        /// koyu su çizgisi.</summary>
        static void WetBand(Canvas c, Color wet, float top, float bottom, float w)
        {
            float ph1 = Rnd(0, 6.28f), ph2 = Rnd(0, 6.28f);
            for (int x = 0; x < w; x++)
            {
                float edge = top + 8 + 6 * Mathf.Sin(x / 37f + ph1) + 3.5f * Mathf.Sin(x / 11f + ph2);
                for (int y = Mathf.FloorToInt(edge) - 2; y < bottom; y++)
                {
                    float t = Mathf.Clamp01((y - edge) / (bottom - edge));
                    float alpha = y < edge ? 0.4f * (1 - (edge - y) / 2f) : 0.32f + 0.16f * t;
                    c.Blend(x, y, wet, alpha);
                    if (y >= edge - 1 && y < edge + 2) c.Blend(x, y, Color.black, 0.2f);
                }
            }
        }

        /// <summary>Suyun altında kıvrılan kablo: dokunun boyunca bir tam dalga (sarmalı dokuda dikişsiz).</summary>
        static void WavyCable(Canvas c, Color copper, float alpha, float at, float amp, int waves, bool alongX)
        {
            Vector2 P(float t)
            {
                float s = t * SquareSize, o = at + amp * Mathf.Sin(t * waves * 2 * Mathf.PI) + amp * 0.3f * Mathf.Sin(t * 6 * Mathf.PI);
                return alongX ? new Vector2(s, o) : new Vector2(o, s);
            }
            int n = 96;
            for (int i = 0; i < n; i++)
            {
                var a0 = P(i / (float)n); var a1 = P((i + 1) / (float)n);
                c.Line(a0.x + 1.5f, a0.y + 1.5f, a1.x + 1.5f, a1.y + 1.5f, 7, Color.black, alpha * 0.4f);
                c.Line(a0.x, a0.y, a1.x, a1.y, 6, copper, alpha);
                c.Line(a0.x - 1, a0.y - 1, a1.x - 1, a1.y - 1, 1.5f, Color.white, alpha * 0.2f);
            }
        }

        // ================================================================ sa · Hidroelektrik santrali: takoz üstünde çelik levha ve kalıp

        /// <summary>Üst katta yatık istiflenmiş çelik kalıp panelleri (kenar profilinde oluk ve bağ delikleri), alt katta ince çelik
        /// levha istifi (uçları hafif kaçık); iki kat da ahşap takozlar üstünde; yer yer pas.</summary>
        static void SaSide(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            var (ut, ub) = Layers[0];
            float chock = 12;
            FormPanels(c, ut + 1, ub - chock, 4);
            Chocks(c, a, ub - chock, chock);
            var (lt, lb) = Layers[1];
            Plates(c, lt + 1, lb - chock);
            Chocks(c, a, lb - chock, chock);
            for (int i = 0; i < 6; i++)
            {
                float x = Rnd(20, SideW - 20), y = Rnd(6, SideH - 40);
                c.VerticalFadeRect(x, y, 6, Rnd(14, 30), b, 0.3f, 0);
            }
            c.Speckle(0, 0, SideW, SideH, 120, 0.8f, 2f, b, 0.15f, 0.35f);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Ayakta (yan yana dikilmiş) çelik levhalar: dikey levha kenarları, iki çelik bağlama kayışı, dipte takozlar.</summary>
        static void SaColumn(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.West);
            float chock = 12, x = 0;
            while (x < SideW)
            {
                float w = Mathf.Min(Rnd(24, 36), SideW - x), top = Rnd(0, 6);
                if (SideW - (x + w) < 20) w = SideW - x;
                c.Rect(x, 0, w, top, Color.black, 0.35f);
                c.Rect(x, top, 4, SideH - chock - top, Color.white, 0.18f);
                c.Rect(x + w - 6, top, 6, SideH - chock - top, Color.black, 0.3f);
                c.Rect(x, top, w, SideH - chock - top, Rnd() < 0.5f ? Color.white : Color.black, Rnd(0.02f, 0.06f));
                x += w;
            }
            foreach (float y in new[] { 30f, 112f })
            {
                c.Rect(0, y, SideW, 9, a, 0.55f);
                c.Rect(0, y, SideW, 2.5f, Color.white, 0.18f);
                c.Rect(0, y + 6.5f, SideW, 2.5f, Color.black, 0.3f);
            }
            Chocks(c, a, SideH - chock, chock);
            c.Speckle(0, 0, SideW, SideH, 120, 0.8f, 2f, b, 0.15f, 0.35f);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>İstifin üstündeki levha: hadde tufalı alacası, kesik kenar, iki kaldırma deliği, kaynak dikişi, üstüne bırakılmış
        /// küçük bir artık levha, pas lekeleri.</summary>
        static void SaTop(Canvas c, Palette p)
        {
            var b = p.B;
            c.Fill(p.Top);
            for (int i = 0; i < 14; i++)
            {
                bool up = Rnd() < 0.5f;
                c.Stain(Rnd(0, SquareSize), Rnd(0, SquareSize), Rnd(20, 50), up ? Color.white : Color.black, up ? 0.08f : 0.1f);
            }
            c.Bevel(0, 0, SquareSize, SquareSize, 6, 0.2f, 0.3f);
            foreach (float x in new[] { 40f, 216f })
            {
                c.Circle(x, 128, 10, Color.black, 0.5f);
                c.Ring(x, 128, 10, 13, Color.white, 0.14f);
            }
            for (float x = 14; x < SquareSize - 14; x += 5)
            {
                c.Circle(x, 86 + Mathf.Sin(x * 0.3f) * 0.8f, 3, Color.white, 0.12f);
                c.Circle(x + 1, 88, 2, Color.black, 0.12f);
            }
            c.Rect(132, 152, 92, 64, Color.black, 0.2f);
            c.Rect(126, 146, 92, 64, p.Top, 1);
            c.Rect(126, 146, 92, 64, Color.white, 0.06f);
            c.Bevel(126, 146, 92, 64, 3, 0.2f, 0.32f);
            for (int i = 0; i < 9; i++) c.Stain(Rnd(10, 246), Rnd(10, 246), Rnd(6, 14), b, 0.3f);
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Kollarda kolun boyunca kalın lastik kılıflı jeneratör kabloları ve aralıklı bağlama kelepçeleri; göbekte çelik
        /// ızgara (taşıyıcı lamalar ve bağlantı çubukları).</summary>
        static void SaChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(5, 32);
            (float at, float w)[] cables = { (60, 28), (104, 16), (136, 16), (168, 28) };
            foreach (bool vertical in new[] { false, true })
            {
                foreach (var (at, w) in cables)
                {
                    if (vertical) { c.Rect(at, 0, w, SquareSize, Color.black, 0.35f); c.Cylinder(at, 0, w, SquareSize, false, 0.16f, 0.3f); }
                    else { c.Rect(0, at, SquareSize, w, Color.black, 0.35f); c.Cylinder(0, at, SquareSize, w, true, 0.16f, 0.3f); }
                }
                for (float v = 12; v < SquareSize; v += 64)
                {
                    if (vertical) { c.Rect(58, v, 140, 6, d, 0.9f); c.Rect(58, v, 140, 1.5f, Color.white, 0.18f); }
                    else { c.Rect(v, 58, 6, 140, d, 0.9f); c.Rect(v, 58, 1.5f, 140, Color.white, 0.18f); }
                }
            }
            Grating(c, p.Channel, p.WellDetail, HubMin + 4, HubSize - 8, 0.75f);
            c.Noise(4);
        }

        /// <summary>Çelik ızgara kapaklı çukur: çerçeve, taşıyıcı lamalar, altı koyu.</summary>
        static void SaWell(Canvas c, Palette p)
        {
            c.Fill(p.Well);
            c.Grain(5, 32);
            Grating(c, p.Well, p.WellDetail, HubMin + 4, HubSize - 8, 0.85f);
            c.Noise(4);
        }

        /// <summary>Çelik ızgara: koyu boşluk üstünde 9 px aralıklı taşıyıcı lamalar, 36 px aralıklı bağlantı çubukları, çerçeve.</summary>
        static void Grating(Canvas c, Color floor, Color steel, float m, float s, float alpha)
        {
            c.Rect(m, m, s, s, floor, 1);
            c.Rect(m, m, s, s, Color.black, 0.45f);
            for (float x = m + 6; x < m + s - 4; x += 9) { c.Rect(x, m, 3.5f, s, steel, alpha); c.Rect(x, m, 1, s, Color.white, 0.12f); }
            for (float y = m + 18; y < m + s - 4; y += 36) { c.Rect(m, y, s, 3, steel, alpha * 0.85f); c.Rect(m, y + 3, s, 1.5f, Color.black, 0.3f); }
            c.Rect(m, m, s, 5, steel, alpha); c.Rect(m, m + s - 5, s, 5, steel, alpha);
            c.Rect(m, m, 5, s, steel, alpha); c.Rect(m + s - 5, m, 5, s, steel, alpha);
            c.Bevel(m, m, s, s, 2, 0.16f, 0.32f);
        }

        /// <summary>Yatık kalıp panelleri: her biri bir kenar profili (üstü açık, altı gölgeli), ortasında oluk ve bağ delikleri.</summary>
        static void FormPanels(Canvas c, float top, float bottom, int count)
        {
            float h = (bottom - top) / count;
            for (int k = 0; k < count; k++)
            {
                float y = top + k * h, x0 = Rnd(0, 14), x1 = SideW - Rnd(0, 14);
                c.Rect(0, y, x0, h, Color.black, 0.35f); c.Rect(x1, y, SideW - x1, h, Color.black, 0.35f);
                c.Rect(x0, y, x1 - x0, 3, Color.white, 0.2f);
                c.Rect(x0, y + h - 3, x1 - x0, 3, Color.black, 0.35f);
                c.Rect(x0, y + h / 2 - 1.5f, x1 - x0, 3, Color.black, 0.18f);
                for (float x = x0 + 20 + (k % 2) * 21; x < x1 - 10; x += 42) Disc(c, x, y + h / 2, 4.5f, 2.6f, Color.black, 0.5f);
            }
        }

        /// <summary>İnce çelik levha istifi: 5–8 px'lik levhalar, uçları kaçık, her birinin üstü açık altı koyu, ton farkları.</summary>
        static void Plates(Canvas c, float top, float bottom)
        {
            float y = top;
            while (y < bottom - 3)
            {
                float h = Mathf.Min(Rnd(9, 13), bottom - y), x0 = Rnd(0, 22), x1 = SideW - Rnd(0, 22);
                if (bottom - (y + h) < 6) h = bottom - y;
                c.Rect(0, y, x0, h, Color.black, 0.32f); c.Rect(x1, y, SideW - x1, h, Color.black, 0.32f);
                c.Rect(x0, y, x1 - x0, h, Rnd() < 0.5f ? Color.white : Color.black, Rnd(0.02f, 0.07f));
                c.Rect(x0, y, x1 - x0, 2.5f, Color.white, 0.2f);
                c.Rect(x0, y + h - 3, x1 - x0, 3, Color.black, 0.32f);
                y += h;
            }
        }

        /// <summary>Ahşap takozlar: üç blok (yıpranmış koyu ahşap, uç kesitinde halka izi), aralarında koyu boşluk.</summary>
        static void Chocks(Canvas c, Color wood, float y, float h)
        {
            c.Rect(0, y, SideW, h, Color.black, 0.45f);
            foreach (float x in new[] { 22f, 226f, 430f })
            {
                c.Rect(x, y, 62, h, wood, 0.95f);
                c.Bevel(x, y, 62, h, 1.5f, 0.15f, 0.3f);
                c.Rect(x + 8, y + h / 2 - 1, 46, 2, Color.black, 0.15f);
            }
        }

        // ================================================================ oc · Maden limanı: baklava sac kızak üstünde yedek parça kasası

        /// <summary>Her katta iki iri çelik yedek parça kasası (köşe dökümleri, iki yatay kaburga, mandal, künye levhası, dipte kızıl
        /// toz); alt katın dibinde çatal cepli baklava sac kızak.</summary>
        static void OcSide(Canvas c, Palette p)
        {
            c.Fill(p.West);
            float skid = 16;
            var (ut, ub) = Layers[0];
            c.Rect(0, ut, SideW, ub - ut, Color.black, 0.3f);
            foreach (var (x0, x1) in new[] { (-60f, 236f), (236f, 532f) }) Crate(c, p, x0, ut, x1 - x0, ub - ut, false);
            var (lt, lb) = Layers[1];
            c.Rect(0, lt, SideW, lb - lt, Color.black, 0.3f);
            foreach (var (x0, x1) in new[] { (0f, 280f), (280f, (float)SideW) }) Crate(c, p, x0, lt, x1 - x0, lb - skid - lt, false);
            Skid(c, p, lb - skid, skid);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Kasa kulesi: üst katta iki, alt katta bir kasa (uçtan: köşe dökümleri, kaburga, kulp), dipte kızak.</summary>
        static void OcColumn(Canvas c, Palette p)
        {
            c.Fill(p.West);
            c.Rect(0, 0, SideW, SideH, Color.black, 0.3f);
            float skid = 16;
            var (ut, ub) = Layers[0];
            float h = (ub - ut) / 2;
            Crate(c, p, 0, ut, SideW, h, true);
            Crate(c, p, 0, ut + h, SideW, h, true);
            var (lt, lb) = Layers[1];
            Crate(c, p, 0, lt, SideW, lb - skid - lt, true);
            Skid(c, p, lb - skid, skid);
            c.Grain(3, 16);
            c.Noise(3);
        }

        /// <summary>Kasanın kapağı: baklava sac, kenar çerçevesi, köşe dökümleri, oluklarda kızıl toz.</summary>
        static void OcTop(Canvas c, Palette p)
        {
            var a = p.A; var b = p.B;
            c.Fill(p.Top);
            Checker(c, 14, 14, SquareSize - 28, SquareSize - 28, 16, 4);
            for (int i = 0; i < 7; i++) c.Stain(Rnd(20, 236), Rnd(20, 236), Rnd(18, 40), b, 0.16f);
            c.Rect(0, 0, SquareSize, 12, a, 0.4f); c.Rect(0, SquareSize - 12, SquareSize, 12, a, 0.4f);
            c.Rect(0, 12, 12, SquareSize - 24, a, 0.4f); c.Rect(SquareSize - 12, 12, 12, SquareSize - 24, a, 0.4f);
            c.Bevel(0, 0, SquareSize, SquareSize, 4, 0.18f, 0.3f);
            c.Inset(12, 12, SquareSize - 24, SquareSize - 24, 2, 0.1f, 0.25f);
            foreach (var (x, y) in new[] { (0f, 0f), (SquareSize - 28f, 0f), (0f, SquareSize - 28f), (SquareSize - 28f, SquareSize - 28f) })
            {
                c.Rect(x, y, 28, 28, a, 0.85f);
                c.Bevel(x, y, 28, 28, 2.5f, 0.15f, 0.32f);
                Disc(c, x + 14, y + 14, 6, 4, Color.black, 0.5f);
            }
            c.Grain(3, 32);
            c.Noise(3);
        }

        /// <summary>Kolların iki dış yanında baklava sac kapak şeritleri (ortada açık oluk, bakır görünür); göbekte şeritler kare bir
        /// çerçeve olur.</summary>
        static void OcChannel(Canvas c, Palette p)
        {
            var d = p.ChannelDetail;
            c.Fill(p.Channel);
            c.Grain(5, 32);
            foreach (bool vertical in new[] { false, true })
                foreach (float at in new[] { 56f, 168f })
                {
                    float w = 32;
                    if (vertical) CheckerStrip(c, d, at, 0, w, SquareSize);
                    else CheckerStrip(c, d, 0, at, SquareSize, w);
                }
            c.Rect(HubMin, HubMin, HubSize, HubSize, p.Channel, 1);
            CheckerStrip(c, d, 56, HubMin, 32, HubSize);
            CheckerStrip(c, d, 168, HubMin, 32, HubSize);
            CheckerStrip(c, d, 88, 56, 80, 32);
            CheckerStrip(c, d, 88, 168, 80, 32);
            c.Noise(4);
        }

        /// <summary>Baklava sac menhol kapağı, hafif aralık: çerçeve, baklava desen, kaldırma yuvası, köşe cıvataları, bir kenarda koyu
        /// aralık.</summary>
        static void OcWell(Canvas c, Palette p)
        {
            var d = p.WellDetail;
            c.Fill(p.Well);
            c.Grain(5, 32);
            float m = HubMin + 4, s = HubSize - 8;
            c.Rect(m, m, s, s, d, 0.8f);
            c.Bevel(m, m, s, s, 3, 0.18f, 0.32f);
            c.Rect(m + 8, m + 8, s - 16, s - 16, d, 0.6f);
            Checker(c, m + 10, m + 10, s - 20, s - 20, 16, 4);
            c.Rect(m + s - 14, m + 8, 6, s - 16, Color.black, 0.65f);
            Pillow(c, m + 18, Mid - 7, 40, 14, 7, 7, Color.black, 0.55f, 0, 0);
            foreach (var (x, y) in new[] { (m + 14, m + 14), (m + s - 24, m + 14), (m + 14, m + s - 14), (m + s - 24, m + s - 14) })
                c.Circle(x, y, 3.5f, Color.black, 0.45f);
            c.Noise(4);
        }

        /// <summary>Çelik yedek parça kasası: kabartma gövde, köşe dökümleri, iki yatay kaburga, üstte mandal, künye levhası, dipte
        /// kızıl toz. <paramref name="end"/>: kasanın ucu (kulp, tek kaburga).</summary>
        static void Crate(Canvas c, Palette p, float x, float y, float w, float h, bool end)
        {
            var a = p.A; var b = p.B;
            c.Rect(x + 2, y + 1.5f, w - 4, h - 3, p.West, 1);
            c.Bevel(x + 2, y + 1.5f, w - 4, h - 3, 3, 0.2f, 0.32f);
            float cw = 22, chh = Mathf.Min(13, h * 0.3f);
            foreach (var (cx, cy) in new[] { (x + 2, y + 1.5f), (x + w - 2 - cw, y + 1.5f), (x + 2, y + h - 1.5f - chh), (x + w - 2 - cw, y + h - 1.5f - chh) })
            {
                c.Rect(cx, cy, cw, chh, a, 0.85f);
                c.Bevel(cx, cy, cw, chh, 1.5f, 0.14f, 0.3f);
                Disc(c, cx + cw / 2, cy + chh / 2, 4.5f, 2.5f, Color.black, 0.45f);
            }
            float[] ribs = end ? new[] { 0.5f } : new[] { 0.36f, 0.68f };
            foreach (float t in ribs)
            {
                float ry = y + h * t;
                c.Rect(x + cw + 4, ry, w - 2 * cw - 8, 4, Color.white, 0.16f);
                c.Rect(x + cw + 4, ry + 4, w - 2 * cw - 8, 3, Color.black, 0.26f);
            }
            if (end)
            {
                float hx = x + w / 2;
                c.Line(hx - 30, y + h * 0.26f, hx + 30, y + h * 0.26f, 4, a, 0.85f);
                c.Rect(hx - 34, y + h * 0.18f, 6, h * 0.12f, a, 0.85f); c.Rect(hx + 28, y + h * 0.18f, 6, h * 0.12f, a, 0.85f);
            }
            else
            {
                c.Rect(x + w / 2 - 12, y + 3, 24, Mathf.Min(14, h * 0.28f), a, 0.85f);
                c.Bevel(x + w / 2 - 12, y + 3, 24, Mathf.Min(14, h * 0.28f), 1.5f, 0.15f, 0.3f);
                c.Rect(x + cw + 24, y + h * 0.12f, 34, Mathf.Min(14, h * 0.18f), Color.white, 0.14f);
            }
            c.VerticalFadeRect(x + 2, y + h * 0.55f, w - 4, h * 0.45f - 1.5f, b, 0, 0.3f);
        }

        /// <summary>Çelik kızak: C profil kiriş (üstte baklava sac döşemenin açık kenarı), iki çatal cebi, kızıl toz.</summary>
        static void Skid(Canvas c, Palette p, float y, float h)
        {
            var a = p.A; var b = p.B;
            c.Rect(0, y, SideW, h, a, 0.8f);
            c.Rect(0, y, SideW, 3, Color.white, 0.22f);
            c.Rect(0, y + h - 3, SideW, 3, Color.black, 0.3f);
            foreach (float x in new[] { 96f, 346f })
            {
                c.Rect(x, y + 4, 72, h - 7, Color.black, 0.55f);
                c.Inset(x, y + 4, 72, h - 7, 1.5f, 0.1f, 0.3f);
            }
            c.Speckle(0, y, SideW, h, 160, 0.8f, 1.8f, b, 0.2f, 0.45f);
        }

        /// <summary>Baklava sac şeridi: plaka, kenar kabartması, sık baklava deseni.</summary>
        static void CheckerStrip(Canvas c, Color steel, float x, float y, float w, float h)
        {
            c.Rect(x, y, w, h, steel, 0.85f);
            Checker(c, x + 2, y + 2, w - 4, h - 4, 12, 3);
            c.Bevel(x, y, w, h, 2, 0.14f, 0.35f);
        }

        /// <summary>Baklava (gözyaşı) deseni: <paramref name="pitch"/> aralıklı kabarık çubuklar, komşu gözlerde dik yönlerde (çapraz),
        /// ışık sol üstten.</summary>
        static void Checker(Canvas c, float x, float y, float w, float h, float pitch, float width)
        {
            for (int j = 0; j * pitch < h; j++)
                for (int i = 0; i * pitch < w; i++)
                {
                    float cx = x + (i + 0.5f) * pitch, cy = y + (j + 0.5f) * pitch;
                    if (cx > x + w || cy > y + h) continue;
                    float l = pitch * 0.33f, dx = l * 0.7071f, dy = ((i + j) % 2 == 0 ? 1 : -1) * l * 0.7071f;
                    c.Line(cx - dx + 1, cy - dy + 1, cx + dx + 1, cy + dy + 1, width, Color.black, 0.28f);
                    c.Line(cx - dx, cy - dy, cx + dx, cy + dy, width, Color.white, 0.16f);
                }
        }

        // ================================================================ ortak

        /// <summary>
        /// Yastık: köşeleri elips yuvarlak dolu dikdörtgen (rx, ry; yan dokuda rx = ry × 1,87 dünyada yuvarlak), ışık sol üstten:
        /// üstü açık, altı ve kenarlara doğru koyu (dolgun gövde). <paramref name="weave"/>: ince yatay örgü çizgileri.
        /// </summary>
        static void Pillow(Canvas c, float x, float y, float w, float h, float rx, float ry, Color col, float alpha, float light, float dark, bool weave = false)
        {
            rx = Mathf.Max(0.5f, Mathf.Min(rx, w / 2)); ry = Mathf.Max(0.5f, Mathf.Min(ry, h / 2));
            float cx = x + w / 2, cy = y + h / 2, hx = w / 2 - rx, hy = h / 2 - ry, soft = Mathf.Min(rx, ry);
            for (int yy = Mathf.FloorToInt(y) - 1; yy <= Mathf.CeilToInt(y + h) + 1; yy++)
                for (int xx = Mathf.FloorToInt(x) - 1; xx <= Mathf.CeilToInt(x + w) + 1; xx++)
                {
                    float px = xx + 0.5f - cx, py = yy + 0.5f - cy;
                    float qx = Mathf.Max(Mathf.Abs(px) - hx, 0) / rx, qy = Mathf.Max(Mathf.Abs(py) - hy, 0) / ry;
                    float d = Mathf.Sqrt(qx * qx + qy * qy);
                    float cov = Mathf.Clamp01((1 - d) * soft + 0.5f);
                    if (cov <= 0) continue;
                    c.Blend(xx, yy, col, alpha * cov);
                    if (light <= 0 && dark <= 0) continue;
                    float t = py / (h / 2), s = px / (w / 2);
                    float shade = -t * 0.75f - s * 0.15f - d * d * 0.7f + 0.15f;
                    if (shade >= 0) c.Blend(xx, yy, Color.white, light * shade * cov);
                    else c.Blend(xx, yy, Color.black, dark * -shade * cov);
                    if (weave && ((yy % 3) == 0 || (xx % 7) == 0)) c.Blend(xx, yy, Color.black, 0.05f * cov);
                }
        }
    }
}
