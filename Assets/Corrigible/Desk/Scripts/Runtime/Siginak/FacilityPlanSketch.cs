using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Paftanın çizimi (saf geometri; <see cref="FacilityPlanSheet"/> çizer, testler okur): <see cref="FacilityPlanSheetData"/>'dan
    /// katman katman yol listeleri. Koordinat paftanın çizim alanında, sol üst orijin, y aşağı (kare (x, y) → [x·c, (x+1)·c] ×
    /// [y·c, (y+1)·c]; y = 0 tahtanın kuzeyi, sütun 'A' + x batıdan). Bilinmeyen odada yalnız dış çerçeve.
    /// <list type="bullet">
    /// <item><b>Grid</b>: ince ızgara (iç çizgiler).</item>
    /// <item><b>Walls</b>: duvar sıraları; kalınlık bir kare (yatay parçalar).</item>
    /// <item><b>Links</b>: menfez çiftinin iki ucunu bağlayan noktalı çizgi.</item>
    /// <item><b>Lines</b>: dış çerçeve (geçitlerde açık), geçit okları, kablo dalgası, kepenk, menfez ve nesne işaretleri.</item>
    /// <item><b>Areas</b>: şart alanlarının sınırı (bulut, gaz, kriyo, hol, yağ) ve iki yöne açık turnike (kesik).</item>
    /// <item><b>Parts</b>: şart parçaları (meme, sızıntı, çatlak, raf, durak, turnike, kol, tüp, süzgeç, hortum, faz); tesis aşama 12
    /// (Katar): açık kenarın kesik çizgisi ve dışa bakan tırnakları (o kenarda <b>Lines</b>'ın dış çerçevesi yok).</item>
    /// <item><b>Counter</b>: tesis aşama 10 (K41), YZ'nin yapısal önlemleri kırmızı kalemle: çift cidarlı duvarda çapraz tarama ve iç
    /// kontur; koku yayıcıda küçük halka ve eriminin baklava sınırı. Önlemsiz paftada boş.</item>
    /// </list>
    /// </summary>
    public sealed class FacilityPlanSketch
    {
        public const int N = 8;
        public readonly List<List<Vector2>> Grid = new List<List<Vector2>>();
        public readonly List<List<Vector2>> Walls = new List<List<Vector2>>();
        public readonly List<List<Vector2>> Links = new List<List<Vector2>>();
        public readonly List<List<Vector2>> Lines = new List<List<Vector2>>();
        public readonly List<List<Vector2>> Areas = new List<List<Vector2>>();
        public readonly List<List<Vector2>> Parts = new List<List<Vector2>>();
        public readonly List<List<Vector2>> Counter = new List<List<Vector2>>();
        /// <summary>Tesis aşama 11 (K39-6): kalıcı tahtanın izleri, mürekkep katmanında (<see cref="Lines"/> ile aynı renk ve kalınlık)
        /// ayırt edici çizimle: yıkık duvarda moloz taraması, açık kepenkte kalkık kanat, kırık donanım ve nesnede eski işaret ve çarpı.
        /// Engel ve hurda nesne işaretidir (<see cref="Lines"/>). İzsiz paftada boş.</summary>
        public readonly List<List<Vector2>> Traces = new List<List<Vector2>>();

        /// <summary>Tahtanın yönleri (Core <c>Baskin.Dirs</c>): 0 kuzey, 1 doğu, 2 güney, 3 batı; y aşağı.</summary>
        static readonly Vector2Int[] Dirs = { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

        readonly float c;
        readonly PlanTile[] tiles = new PlanTile[N * N];
        /// <summary>Tesis aşama 12 (Katar): kuzey (0) ve güney (2) kenarı açık mı (<see cref="FacilityPlanSheetData.OpenEdges"/>).</summary>
        readonly bool[] open = new bool[4];

        /// <summary>Kenar açık mı (0 kuzey, 1 doğu, 2 güney, 3 batı): Katar'ın açık kenarı; dış çerçevesi çizilmez.</summary>
        public bool IsOpenEdge(int edge) => edge >= 0 && edge < 4 && open[edge];

        FacilityPlanSketch(float cell) { c = cell; }

        public static FacilityPlanSketch Draw(FacilityPlanSheetData d, float cell)
        {
            var s = new FacilityPlanSketch(cell);
            if (d == null) return s;
            if (!d.Known || d.Tiles.Count == 0)
            {
                float w = cell * N;
                s.Lines.Add(new List<Vector2> { new Vector2(0, 0), new Vector2(w, 0), new Vector2(w, w), new Vector2(0, w), new Vector2(0, 0) });
                return s;
            }
            foreach (var t in d.Tiles) if (t.X >= 0 && t.X < N && t.Y >= 0 && t.Y < N) s.tiles[t.Y * N + t.X] = t;
            foreach (int row in d.OpenEdges)
            {
                if (row == 0) s.open[0] = true;
                else if (row == N - 1) s.open[2] = true;
            }
            s.FindDoorEdges();
            s.DrawGrid();
            s.DrawWalls();
            s.DrawFrame();
            s.DrawTiles();
            s.DrawMarks(d.Marks);
            s.DrawOpenEdges();
            s.DrawCounters(d.Counters, d.CounterRange);
            s.DrawTraces(d.Traces);
            return s;
        }

        PlanTile At(int x, int y) => x < 0 || y < 0 || x >= N || y >= N ? null : tiles[y * N + x];
        Vector2 Center(int x, int y) => new Vector2((x + 0.5f) * c, (y + 0.5f) * c);
        float R => c * 0.32f;

        void DrawGrid()
        {
            float w = c * N;
            for (int i = 1; i < N; i++)
            {
                Grid.Add(new List<Vector2> { new Vector2(i * c, 0), new Vector2(i * c, w) });
                Grid.Add(new List<Vector2> { new Vector2(0, i * c), new Vector2(w, i * c) });
            }
        }

        // Duvar sırası: yatay parça, kalınlık bir kare (UILineGraphic'in düz ucu karenin kenarında biter)
        void DrawWalls()
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if (At(x, y)?.Ground != PlanGround.Wall) continue;
                    int end = x;
                    while (end + 1 < N && At(end + 1, y)?.Ground == PlanGround.Wall) end++;
                    float cy = (y + 0.5f) * c;
                    Walls.Add(new List<Vector2> { new Vector2(x * c, cy), new Vector2((end + 1) * c, cy) });
                    x = end;
                }
        }

        /// <summary>Geçit türünün (1 batı girişi, 2 ileri geçit) açıldığı kenar: geçidin bütün karelerinin değdiği kenar (köşedeki kare
        /// iki kenara değer; komşu geçit karesi hangisini paylaşıyorsa o). Batı girişinde önce batı, ileri geçitte doğu, kuzey, güney.</summary>
        readonly int[] doorEdges = { -1, -1, -1 };

        /// <summary>Geçidin açıldığı kenar (0 kuzey, 1 doğu, 2 güney, 3 batı; −1 yok): <paramref name="door"/> 1 batı girişi, 2 ileri geçit.</summary>
        public int EdgeOf(int door) => door >= 1 && door <= 2 ? doorEdges[door] : -1;

        static readonly int[][] EdgeOrder = { null, new[] { 3, 0, 2, 1 }, new[] { 1, 0, 2, 3 } };

        static bool Touches(PlanTile t, int edge) => edge == 0 ? t.Y == 0 : edge == 1 ? t.X == N - 1 : edge == 2 ? t.Y == N - 1 : t.X == 0;

        void FindDoorEdges()
        {
            for (int door = 1; door <= 2; door++)
            {
                var cells = new List<PlanTile>();
                foreach (var t in tiles) if (t != null && t.Door == door) cells.Add(t);
                if (cells.Count == 0) continue;
                foreach (int edge in EdgeOrder[door])
                    if (cells.TrueForAll(t => Touches(t, edge))) { doorEdges[door] = edge; break; }
                // Geçidin kareleri ortak bir kenara değmiyorsa her kare kendi ilk kenarında açılır
            }
        }

        int DoorEdge(PlanTile t)
        {
            if (t == null || t.Door < 1 || t.Door > 2) return -1;
            if (doorEdges[t.Door] >= 0) return doorEdges[t.Door];
            foreach (int edge in EdgeOrder[t.Door]) if (Touches(t, edge)) return edge;
            return -1;
        }

        // Dış çerçeve; geçit karelerinde açık (ozalit planda kapı boşluğu)
        void DrawFrame()
        {
            float w = c * N;
            for (int edge = 0; edge < 4; edge++)
            {
                // Tesis aşama 12 (Katar): açık kenarın çerçevesi yok (DrawOpenEdges kesik çizer)
                if (open[edge]) continue;
                List<Vector2> run = null;
                for (int i = 0; i < N; i++)
                {
                    var t = edge == 0 ? At(i, 0) : edge == 1 ? At(N - 1, i) : edge == 2 ? At(i, N - 1) : At(0, i);
                    bool open = DoorEdge(t) == edge;
                    if (open) { run = null; continue; }
                    Vector2 a, b;
                    switch (edge)
                    {
                        case 0: a = new Vector2(i * c, 0); b = new Vector2((i + 1) * c, 0); break;
                        case 1: a = new Vector2(w, i * c); b = new Vector2(w, (i + 1) * c); break;
                        case 2: a = new Vector2(i * c, w); b = new Vector2((i + 1) * c, w); break;
                        default: a = new Vector2(0, i * c); b = new Vector2(0, (i + 1) * c); break;
                    }
                    if (run == null) { run = new List<Vector2> { a }; Lines.Add(run); }
                    if (run.Count > 1) run.RemoveAt(run.Count - 1);
                    run.Add(b);
                }
            }
        }

        /// <summary>
        /// Tesis aşama 12 (Katar): açık kenar şart mürekkebiyle (<see cref="Parts"/>, plan.part): kenar boyunca her duvarsız karede iki kesik
        /// parça ve kenara bakan kısa tırnak (düşüş yönünü gösteren küçük ok başı; çizim alanının içinde kalır). Duvar karesinde kenar
        /// duvarın kendisidir (duvar katmanı), kesik yok.
        /// </summary>
        void DrawOpenEdges()
        {
            float w = c * N;
            for (int edge = 0; edge <= 2; edge += 2)
            {
                if (!open[edge]) continue;
                int row = edge == 0 ? 0 : N - 1;
                float y = edge == 0 ? 0 : w, sign = edge == 0 ? -1 : 1;
                for (int i = 0; i < N; i++)
                {
                    if (At(i, row)?.Ground == PlanGround.Wall) continue;
                    float x0 = i * c;
                    Parts.Add(Seg(new Vector2(x0 + c * 0.08f, y), new Vector2(x0 + c * 0.42f, y)));
                    Parts.Add(Seg(new Vector2(x0 + c * 0.58f, y), new Vector2(x0 + c * 0.92f, y)));
                    // Tırnak: kenara bakan ok başı, kenardan biraz içeride
                    float tip = y - sign * c * 0.07f, back = y - sign * c * 0.2f, cx = x0 + c * 0.5f, half = c * 0.1f;
                    Parts.Add(new List<Vector2> { new Vector2(cx - half, back), new Vector2(cx, tip), new Vector2(cx + half, back) });
                }
            }
        }

        void DrawTiles()
        {
            var vents = new Dictionary<int, Vector2>();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var t = At(x, y);
                    if (t == null) continue;
                    var p = Center(x, y);
                    if (t.Ground == PlanGround.Cable) Wave(p);
                    if (t.Ground == PlanGround.Shutter) Shutter(x, y);
                    if (t.Door != 0) Door(t, p);
                    if (t.Vent != 0)
                    {
                        Circle(Lines, p, R * 0.8f);
                        Circle(Lines, p, R * 0.25f, 6);
                        if (vents.TryGetValue(t.Vent, out var other)) Links.Add(new List<Vector2> { other, p });
                        else vents[t.Vent] = p;
                    }
                    Item(t.Item, p);
                }
        }

        void Wave(Vector2 p)
        {
            float half = c * 0.5f - 3, a = c * 0.14f;
            var path = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                float u = i / 8f;
                path.Add(new Vector2(p.x - half + 2 * half * u, p.y + a * Mathf.Sin(u * Mathf.PI * 2)));
            }
            Lines.Add(path);
        }

        void Shutter(int x, int y)
        {
            float x0 = x * c + 2, y0 = y * c + 2, x1 = (x + 1) * c - 2, y1 = (y + 1) * c - 2, cy = (y + 0.5f) * c;
            Lines.Add(Rect(x0, y0, x1, y1));
            Lines.Add(new List<Vector2> { new Vector2(x0, cy - 3), new Vector2(x1, cy - 3) });
            Lines.Add(new List<Vector2> { new Vector2(x0, cy + 3), new Vector2(x1, cy + 3) });
        }

        // Geçit oku: batı girişi içeri, ileri geçit dışarı (açık kenara doğru)
        void Door(PlanTile t, Vector2 p)
        {
            int edge = DoorEdge(t);
            if (edge < 0) return;
            var outward = (Vector2)Dirs[edge];
            var dir = t.Door == 1 ? -outward : outward;
            var tip = p + dir * (c * 0.3f);
            var side = new Vector2(-dir.y, dir.x) * (c * 0.18f);
            var back = tip - dir * (c * 0.22f);
            Lines.Add(new List<Vector2> { back + side, tip, back - side });
        }

        void Item(PlanItem item, Vector2 p)
        {
            float r = R;
            switch (item)
            {
                case PlanItem.Target:
                    Lines.Add(Square(p, r));
                    Lines.Add(new List<Vector2> { p + new Vector2(-r, -r), p + new Vector2(r, r) });
                    Lines.Add(new List<Vector2> { p + new Vector2(-r, r), p + new Vector2(r, -r) });
                    break;
                case PlanItem.Copy:
                    Lines.Add(Square(p, r));
                    Lines.Add(Square(p, r * 0.45f));
                    break;
                case PlanItem.Cooler:
                    Circle(Lines, p, r);
                    Lines.Add(new List<Vector2> { p + new Vector2(-r * 0.6f, 0), p + new Vector2(r * 0.6f, 0) });
                    Lines.Add(new List<Vector2> { p + new Vector2(0, -r * 0.6f), p + new Vector2(0, r * 0.6f) });
                    break;
                case PlanItem.Civilian:
                    Lines.Add(Rect(p.x - r * 0.65f, p.y - r, p.x + r * 0.65f, p.y + r));
                    Lines.Add(new List<Vector2> { p + new Vector2(-r * 0.65f, -r * 0.33f), p + new Vector2(r * 0.65f, -r * 0.33f) });
                    Lines.Add(new List<Vector2> { p + new Vector2(-r * 0.65f, r * 0.33f), p + new Vector2(r * 0.65f, r * 0.33f) });
                    break;
                case PlanItem.Panel:
                    Lines.Add(Square(p, r));
                    Lines.Add(new List<Vector2> { p + new Vector2(r * 0.25f, -r * 0.75f), p + new Vector2(-r * 0.3f, r * 0.05f), p + new Vector2(r * 0.3f, -r * 0.05f), p + new Vector2(-r * 0.25f, r * 0.75f) });
                    break;
                case PlanItem.Antenna:
                    Lines.Add(new List<Vector2> { p + new Vector2(0, -r), p + new Vector2(-r * 0.8f, r), p + new Vector2(r * 0.8f, r), p + new Vector2(0, -r) });
                    Lines.Add(new List<Vector2> { p + new Vector2(0, -r), p + new Vector2(0, r) });
                    break;
                case PlanItem.Valve:
                    Lines.Add(new List<Vector2> { p + new Vector2(-r, -r * 0.6f), p + new Vector2(r, r * 0.6f), p + new Vector2(r, -r * 0.6f), p + new Vector2(-r, r * 0.6f), p + new Vector2(-r, -r * 0.6f) });
                    break;
                case PlanItem.Debris:
                    Lines.Add(new List<Vector2> { p + new Vector2(-r * 0.7f, -r * 0.7f), p + new Vector2(r * 0.7f, r * 0.7f) });
                    Lines.Add(new List<Vector2> { p + new Vector2(-r * 0.7f, r * 0.7f), p + new Vector2(r * 0.7f, -r * 0.7f) });
                    break;
                case PlanItem.Tank:
                    Circle(Parts, p, r * 0.6f);
                    break;
                case PlanItem.Arm:
                    Circle(Parts, p, r * 0.5f);
                    break;
                case PlanItem.Barrier:
                    // Örülen engel (tel, kalas): iki direk ve aralarında zikzak
                    Lines.Add(Seg(p + new Vector2(-r, -r * 0.8f), p + new Vector2(-r, r * 0.8f)));
                    Lines.Add(Seg(p + new Vector2(r, -r * 0.8f), p + new Vector2(r, r * 0.8f)));
                    Lines.Add(new List<Vector2> { p + new Vector2(-r, -r * 0.5f), p + new Vector2(-r * 0.33f, r * 0.5f), p + new Vector2(r * 0.33f, -r * 0.5f), p + new Vector2(r, r * 0.5f) });
                    break;
                case PlanItem.Scrap:
                    // Hurda yığını: alçak üçgen ve içinde kısa çizgi
                    Lines.Add(new List<Vector2> { p + new Vector2(-r, r * 0.6f), p + new Vector2(0, -r * 0.6f), p + new Vector2(r, r * 0.6f), p + new Vector2(-r, r * 0.6f) });
                    Lines.Add(Seg(p + new Vector2(-r * 0.35f, r * 0.15f), p + new Vector2(r * 0.35f, r * 0.15f)));
                    break;
            }
        }

        /// <summary>
        /// Tesis aşama 11 (K39-6): izler mürekkeple. Yıkık duvar (artık zemin): karenin içinde üç kısa çapraz moloz çizgisi ve iki nokta;
        /// açık kepenk: kepengin çerçevesi ve üst kenarda kalkık kanat; kırık donanım, soğutma ünitesi ve kopya dolabı: eski işaret ve
        /// karenin çarpısı. Engeller (tel, kalas, hurda) planın nesnesidir (<see cref="Item"/>); burada çizilmez.
        /// </summary>
        void DrawTraces(List<PlanMark> marks)
        {
            if (marks == null || marks.Count == 0) return;
            foreach (var m in marks)
            {
                if (m.X < 0 || m.Y < 0 || m.X >= N || m.Y >= N) continue;
                float x0 = m.X * c, y0 = m.Y * c, x1 = x0 + c, y1 = y0 + c;
                var p = Center(m.X, m.Y);
                float r = R;
                switch (m.Kind)
                {
                    case "duvar":
                        for (int k = 0; k < 3; k++)
                        {
                            float u = (k + 1) / 4f;
                            Traces.Add(Seg(new Vector2(x0 + c * u - r * 0.3f, y0 + c * 0.7f), new Vector2(x0 + c * u + r * 0.3f, y0 + c * 0.3f)));
                        }
                        Circle(Traces, p + new Vector2(-r * 0.6f, r * 0.75f), c * 0.04f, 5);
                        Circle(Traces, p + new Vector2(r * 0.7f, -r * 0.75f), c * 0.04f, 5);
                        break;
                    case "kepenk":
                        Traces.Add(Rect(x0 + 2, y0 + 2, x1 - 2, y1 - 2));
                        Traces.Add(Seg(new Vector2(x0 + 2, y0 + 5), new Vector2(x1 - 2, y0 + 5)));
                        break;
                    case "donanim": case "cool": case "spawner":
                    {
                        int before = Lines.Count;
                        Item(m.Item, p);
                        // Eski işaret mürekkebin değil izin katmanında (kırık)
                        for (int i = before; i < Lines.Count; i++) Traces.Add(Lines[i]);
                        Lines.RemoveRange(before, Lines.Count - before);
                        Traces.Add(Seg(new Vector2(x0 + 3, y0 + 3), new Vector2(x1 - 3, y1 - 3)));
                        Traces.Add(Seg(new Vector2(x0 + 3, y1 - 3), new Vector2(x1 - 3, y0 + 3)));
                        break;
                    }
                }
            }
        }

        void DrawMarks(List<PlanMark> marks)
        {
            if (marks == null || marks.Count == 0) return;
            var areas = new Dictionary<string, HashSet<int>>();
            foreach (var m in marks)
            {
                if (m.X < 0 || m.Y < 0 || m.X >= N || m.Y >= N) continue;
                var p = Center(m.X, m.Y);
                float r = R, x0 = m.X * c, y0 = m.Y * c, x1 = x0 + c, y1 = y0 + c;
                switch (m.Kind)
                {
                    case "bulut": case "gaz": case "kriyo": case "hol": case "yag":
                        if (!areas.TryGetValue(m.Kind, out var set)) areas[m.Kind] = set = new HashSet<int>();
                        set.Add(m.Y * N + m.X);
                        if (m.Kind == "bulut") Circle(Parts, p, r * 0.3f, 6);
                        else if (m.Kind == "gaz") { Parts.Add(Seg(p + new Vector2(-r * 0.6f, r * 0.2f), p + new Vector2(-r * 0.2f, -r * 0.2f))); Parts.Add(Seg(p + new Vector2(r * 0.2f, r * 0.2f), p + new Vector2(r * 0.6f, -r * 0.2f))); }
                        else if (m.Kind == "kriyo") for (int k = 0; k < 3; k++) { float a = k * Mathf.PI / 3; var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.45f; Parts.Add(Seg(p - v, p + v)); }
                        else if (m.Kind == "yag") Parts.Add(new List<Vector2> { p + new Vector2(-r * 0.6f, 0), p + new Vector2(-r * 0.2f, -r * 0.25f), p + new Vector2(r * 0.2f, r * 0.25f), p + new Vector2(r * 0.6f, 0) });
                        break;
                    case "meme":
                        Circle(Parts, p, r * 0.45f, 8);
                        Circle(Parts, p, r * 0.15f, 5);
                        break;
                    case "sizinti":
                        Parts.Add(new List<Vector2> { p + new Vector2(0, -r * 0.7f), p + new Vector2(-r * 0.6f, r * 0.5f), p + new Vector2(r * 0.6f, r * 0.5f), p + new Vector2(0, -r * 0.7f) });
                        break;
                    case "hortum":
                        Circle(Parts, p, r * 0.35f, 8);
                        Parts.Add(Seg(p + new Vector2(r * 0.35f, 0), p + new Vector2(r, r * 0.5f)));
                        break;
                    case "raf":
                        Parts.Add(Seg(new Vector2(x0 + 2, y0 + c * 0.5f), new Vector2(x0 + c * 0.5f, y0 + 2)));
                        Parts.Add(Seg(new Vector2(x0 + 2, y1 - 2), new Vector2(x1 - 2, y0 + 2)));
                        Parts.Add(Seg(new Vector2(x0 + c * 0.5f, y1 - 2), new Vector2(x1 - 2, y0 + c * 0.5f)));
                        break;
                    case "catlak":
                        Parts.Add(new List<Vector2> { new Vector2(x0 + 3, y0 + 4), p + new Vector2(-2, -3), p + new Vector2(2, 2), new Vector2(x1 - 3, y1 - 4) });
                        break;
                    case "durak":
                        Parts.Add(Square(p, r));
                        Parts.Add(Seg(p + new Vector2(0, -r), p + new Vector2(0, r)));
                        break;
                    case "turnike": case "turnike-acik":
                        if (m.ToX < 0 || m.ToY < 0) break;
                        var edge = Edge(m.X, m.Y, m.ToX, m.ToY);
                        if (edge != null) (m.Kind == "turnike" ? Parts : Areas).Add(edge);
                        break;
                    case "kol":
                        if (m.Dir >= 0 && m.Dir < 4)
                        {
                            var d = (Vector2)Dirs[m.Dir];
                            var end = p + d * (c * 0.45f);
                            var side = new Vector2(-d.y, d.x) * (r * 0.4f);
                            Parts.Add(Seg(p + d * (r * 0.5f), end));
                            Parts.Add(Seg(end - side, end + side));
                        }
                        break;
                    case "tup":
                        if (m.Dir >= 0 && m.Dir < 4)
                        {
                            var d = (Vector2)Dirs[m.Dir];
                            var tip = p + d * (c * 0.46f);
                            var side = new Vector2(-d.y, d.x) * (r * 0.35f);
                            Parts.Add(Seg(p + d * (r * 0.6f), tip));
                            Parts.Add(new List<Vector2> { tip - d * (r * 0.45f) + side, tip, tip - d * (r * 0.45f) - side });
                        }
                        break;
                    case "suzgec":
                        Circle(Parts, p, r * 0.75f);
                        Parts.Add(Seg(p + new Vector2(-r * 0.5f, -r * 0.5f), p + new Vector2(r * 0.5f, r * 0.5f)));
                        Parts.Add(Seg(p + new Vector2(-r * 0.5f, r * 0.5f), p + new Vector2(r * 0.5f, -r * 0.5f)));
                        break;
                    case "faz-a": case "faz-b":
                        // A tek, B çift çentik (kablonun üstünde)
                        int ticks = m.Kind == "faz-a" ? 1 : 2;
                        for (int k = 0; k < ticks; k++)
                        {
                            float tx = p.x + (ticks == 1 ? 0 : (k == 0 ? -3 : 3));
                            Parts.Add(Seg(new Vector2(tx, y0 + 2), new Vector2(tx, y0 + c * 0.3f)));
                        }
                        break;
                }
            }
            foreach (var set in areas.Values) Outline(set);
        }

        /// <summary>
        /// Kırmızı kalem (tesis aşama 10): çift cidarlı duvar karesinde iç kontur ve üç çapraz tarama çizgisi (duvar dolgusunun üstünde
        /// okunur); koku yayıcıda küçük halka, içinde nokta ve Manhattan <paramref name="range"/> eriminin sınırı (baklava; tahta
        /// kenarında kesilir).
        /// </summary>
        void DrawCounters(List<PlanMark> marks, int range)
        {
            if (marks == null || marks.Count == 0) return;
            foreach (var m in marks)
            {
                if (m.X < 0 || m.Y < 0 || m.X >= N || m.Y >= N) continue;
                float x0 = m.X * c, y0 = m.Y * c, x1 = x0 + c, y1 = y0 + c, inset = c * 0.14f;
                var p = Center(m.X, m.Y);
                switch (m.Kind)
                {
                    case "cidar":
                        Counter.Add(Rect(x0 + inset, y0 + inset, x1 - inset, y1 - inset));
                        for (int k = 1; k <= 3; k++)
                        {
                            float u = k / 4f;
                            Counter.Add(Seg(new Vector2(x0 + inset, y0 + inset + (c - 2 * inset) * u), new Vector2(x0 + inset + (c - 2 * inset) * u, y0 + inset)));
                            Counter.Add(Seg(new Vector2(x0 + inset + (c - 2 * inset) * u, y1 - inset), new Vector2(x1 - inset, y0 + inset + (c - 2 * inset) * u)));
                        }
                        break;
                    case "yayici":
                    {
                        Circle(Counter, p, R * 0.7f);
                        Circle(Counter, p, R * 0.18f, 6);
                        var area = new HashSet<int>();
                        for (int y = 0; y < N; y++)
                            for (int x = 0; x < N; x++)
                                if (Mathf.Abs(x - m.X) + Mathf.Abs(y - m.Y) <= range) area.Add(y * N + x);
                        Outline(area, Counter);
                        break;
                    }
                }
            }
        }

        /// <summary>Alanın sınırı: kümedeki karenin kümede olmayan komşusuna bakan her kenarı.</summary>
        void Outline(HashSet<int> set) => Outline(set, Areas);

        void Outline(HashSet<int> set, List<List<Vector2>> into)
        {
            foreach (int k in set)
            {
                int x = k % N, y = k / N;
                float x0 = x * c, y0 = y * c, x1 = x0 + c, y1 = y0 + c;
                if (!set.Contains(k - N) || y == 0) into.Add(Seg(new Vector2(x0, y0), new Vector2(x1, y0)));
                if (!set.Contains(k + N) || y == N - 1) into.Add(Seg(new Vector2(x0, y1), new Vector2(x1, y1)));
                if (x == 0 || !set.Contains(k - 1)) into.Add(Seg(new Vector2(x0, y0), new Vector2(x0, y1)));
                if (x == N - 1 || !set.Contains(k + 1)) into.Add(Seg(new Vector2(x1, y0), new Vector2(x1, y1)));
            }
        }

        /// <summary>İki bitişik karenin ortak kenarı (turnike); bitişik değillerse null.</summary>
        List<Vector2> Edge(int ax, int ay, int bx, int by)
        {
            if (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) != 1) return null;
            if (ay == by)
            {
                float x = Mathf.Max(ax, bx) * c;
                return Seg(new Vector2(x, ay * c + 2), new Vector2(x, (ay + 1) * c - 2));
            }
            float y = Mathf.Max(ay, by) * c;
            return Seg(new Vector2(ax * c + 2, y), new Vector2((ax + 1) * c - 2, y));
        }

        static List<Vector2> Seg(Vector2 a, Vector2 b) => new List<Vector2> { a, b };

        static List<Vector2> Rect(float x0, float y0, float x1, float y1) =>
            new List<Vector2> { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), new Vector2(x0, y0) };

        static List<Vector2> Square(Vector2 p, float r) => Rect(p.x - r, p.y - r, p.x + r, p.y + r);

        static void Circle(List<List<Vector2>> into, Vector2 p, float r, int steps = 12)
        {
            var path = new List<Vector2>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.PI * 2 / steps;
                path.Add(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            into.Add(path);
        }
    }
}
