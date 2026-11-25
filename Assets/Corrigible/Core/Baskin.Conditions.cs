using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis aşama 12 (Docs/TESIS-BASKINLARI-TASARIMI.md §10): iki tesis şartının tahta kuralları. Katar (na, "katar"): vagonların kuzey
    /// ve güney kenarı açıktır; dışarı itilen düşer (<see cref="FallOff"/>; tek giriş <see cref="Push"/>), dönüşte bağlantıya yakın
    /// makineler sonraki vagona geçer (<see cref="FollowerIds"/>), Teknisyen vagonu ayırabilir (Baskin.Chain.cs, <see cref="Decouple"/>).
    /// Yalpa (oc, "yalpa"): 2., 4. ve 6. tur sonunda zemin batıya, doğuya, batıya yatar; sabitlenmemiş her şey bir kare kayar
    /// (<see cref="EndTilt"/>). Her kural <c>Is(B, "katar")</c>, <c>Is(B, "yalpa")</c> ya da <see cref="Unit.Overboard"/> arkasındadır;
    /// bu kimlikler yalnız tesis planından ve tesis emrinden gelir (<see cref="FacilityMaps.EventFor"/>), örnek tahtada ve savunma tahtasında
    /// (olay durumu kurulmaz) hiçbiri çalışmaz. Hiçbiri RNG çekmez; tur içi durum BState/Unit içindedir (geri alma kendiliğinden getirir).
    /// </summary>
    public static partial class Baskin
    {
        // ---------- Katar: açık kenar ----------
        /// <summary>İtmenin varacağı satır Katar'ın açık kenarının dışında mı (kuzey ya da güney; doğu ve batı duvar kalır).</summary>
        static bool IsOpenEdgeRow(BState B, int ny) => Is(B, "katar") && (ny < 0 || ny >= N);

        /// <summary>Kare Katar odasının kuzey ya da güney satırında mı (buradan dışarı itilen düşer). Katar dışında, örnek tahtada ve
        /// savunmada false. Sunum hover'ı, bot ve testler okur.</summary>
        public static bool IsOpenEdge(BState B, int x, int y) => B != null && Is(B, "katar") && Inb(x, y) && (y == 0 || y == N - 1);

        /// <summary>Bina planı için: oda Katar'sa açık kenar satırları {0, N−1}, değilse boş (giriş vagonu şartsızdır).</summary>
        public static IReadOnlyList<int> OpenEdgeRows(FacilityRoom room) =>
            room != null && room.Event == "katar" ? new[] { 0, N - 1 } : Array.Empty<int>();

        /// <summary>
        /// Birim Katar'ın açık kenarından düştü (<see cref="Push"/>'tan; cıvatalı makine buraya gelmez). Makine ve tutsak bugünkü ölüm
        /// yolundan (Dead, can 0) yok olur, hurda bırakmaz; ekip üyesi ölmez: Dead + <see cref="Unit.Overboard"/> (gövde değil, tahtada
        /// değil), taşıdığı yük son karesinde kalır (<see cref="DropCargo"/>); köpek düşer (baskından sonra dinlenir), yükü son karesinde
        /// kalır; aygıt hurdasız yok olur. Fx "fall": X/Y son kare, Dx/Dy yön, Unit birim. Telsiz satırı. RNG çekmez.
        /// </summary>
        static void FallOff(BState B, Unit u, int[] d)
        {
            string name = UnitTypes[u.Type].Name;
            u.Dead = true;
            u.Hp = 0;
            AddFx(B, new Fx { K = "fall", X = u.X, Y = u.Y, Dx = d[0], Dy = d[1], Unit = u.Id });
            if (IsSpecialist(u) || IsTurncoat(u))
            {
                u.Overboard = true;
                Say(B, Loc.F("{0} vagondan düştü.", name), "bad");
                DropCargo(B, u);
                return;
            }
            Say(B, Loc.F("{0} kenardan düştü.", name), u.Side == 'e' ? "good" : IsDog(u) ? "bad" : "sys");
            if (IsDog(u)) DropCargo(B, u);
        }

        // ---------- Katar: takip ----------
        /// <summary>
        /// GEÇ şimdi verilirse sonraki vagona geçecek makineler (birim kimlikleri, tahtanın birim sırası = oda raporunun Machines sırası):
        /// dönüşte (kaçış) Katar odasında, AYIR verilmediyse, savunma dışında; ayaktaki makineler (tutsak, köpek ve aygıt ekip tarafındadır)
        /// arasından batı geçidine (bağlantıya) yürüme uzaklığı (<see cref="ExitSteps"/>; birimler sayılmaz, duvar arkası takip etmez)
        /// 0 ile <see cref="MoveRules.FollowRange"/> arasında olanlar. Koşul tutmazsa boş. Tek kaynak: rapor, sunum ve bot bunu okur. Saf.
        /// </summary>
        public static List<int> FollowerIds(BState B)
        {
            var o = new List<int>();
            if (B?.Room == null || !B.Escape || !Is(B, "katar") || B.Decoupled || B.Defense || B.Params?.Mode == "defense") return o;
            var s = ExitSteps(B);
            int range = B.Moves?.FollowRange ?? 2;
            foreach (var u in B.Units)
            {
                if (u.Side != 'e' || IsTurncoat(u) || u.Dead || u.Out || !Inb(u.X, u.Y)) continue;
                int k = s[u.Y, u.X];
                if (k >= 0 && k <= range) o.Add(u.Id);
            }
            return o;
        }

        /// <summary>
        /// Bu tahtaya takip ederek gelen makineler (birim kimlikleri, yerleşim sırasıyla): <see cref="ReturnBoard"/>'un önceki raporun
        /// <see cref="RoomReport.Follow"/>'undan koyduğu ve hâlâ ayakta olanlar (<see cref="Unit.Follower"/>). Makine sınırı ya da yer yüzünden
        /// gelmeyen takipçi burada yoktur; donmuş odanın kendi makineleri hiç sayılmaz. Gözcü satırı bunu okur (aşama 12 kapanışı). Saf.
        /// </summary>
        public static List<int> FollowedInIds(BState B)
        {
            var o = new List<int>();
            if (B?.Units == null) return o;
            foreach (var u in B.Units)
                if (u.Follower && u.Side == 'e' && !u.Dead && !u.Out) o.Add(u.Id);
            return o;
        }

        // ---------- Yalpa: takvim ----------
        /// <summary>Yalpa'nın yatış turu: 2., 4. ve 6. tur sonu, bütçe içinde (son turda ve bütçeyi aşan turda yok). Yalpa dışında false.</summary>
        public static bool TiltAt(BState B, int t) => B != null && Is(B, "yalpa") && (t == 2 || t == 4 || t == 6) && t < B.MaxTurns;

        /// <summary>Yatışın yönü: 4. turda doğu (+1), öbürlerinde batı (−1).</summary>
        public static int TiltDir(int t) => t == 4 ? 1 : -1;

        /// <summary>Oda bütçesinin (<paramref name="maxTurns"/>) yatış takvimi: (tur, yön) sırasıyla (5 tur: 2 batı, 4 doğu; 8 tur: ayrıca 6 batı).</summary>
        public static List<(int Turn, int Dir)> TiltSchedule(int maxTurns) =>
            new[] { 2, 4, 6 }.Where(t => t < maxTurns).Select(t => (t, TiltDir(t))).ToList();

        /// <summary>Karenin yokuş aşağı komşusu kapalı mı (duvar, nesne ya da tahta kenarı): makine aklının çarpma terimi.</summary>
        static bool TiltBlocked(BState B, Pt pos, int dir)
        {
            int nx = pos.X + dir;
            return !Inb(nx, pos.Y) || CellAt(B, nx, pos.Y).T == "wall" || CellAt(B, nx, pos.Y).O != null;
        }

        // ---------- Yalpa: yatış ----------
        /// <summary>
        /// Yatış (<see cref="EventEnd"/> içinde; olay adımı: her hasar olay hasarı, adım başına en çok 1, nesnelere işlemez). Sıra: Fx "ev"
        /// (Obj "yalpa", Dx yön, X/Y −1) ve telsiz; tahtadaki birimler (uzman, makine, köpek, tutsak, aygıt) yokuş aşağıdan başlayarak
        /// (batı yatışında x artan, doğuda x azalan, eşitlikte y artan; liste adımın başında) <see cref="Push"/>'un kayma kipiyle (cıvatalı
        /// sessiz tutar; duvar, tahta kenarı, nesne ve birim çarpması, kablo itmenin kuralıyla); sonra hurda (hedef boş zemin ve ekip
        /// güvencesi bozulmuyorsa); sonra yatan gövdeler ve yerdeki yük (hedef geçilebilir, kapı ve menfez değil, orada başka gövde ya da
        /// yük yok; birimler durdurmaz; hasarsız). Kayan her şey için Fx "slide" (X/Y başlangıç, Dx/Dy yön, N 0 çarptı / 1 kaydı; Unit ya da
        /// Obj "hurda", "body", "cargo"). Kapıdaki, tahliye edilen ve kabindeki birim tahtada değildir, kaymaz. Önlem defterinde itme
        /// sayılmaz. RNG çekmez.
        /// </summary>
        static void EndTilt(BState B, int t)
        {
            if (!TiltAt(B, t)) return;
            int dir = TiltDir(t);
            var d = dir < 0 ? Dirs[3] : Dirs[1];
            AddFx(B, new Fx { K = "ev", Obj = "yalpa", X = -1, Y = -1, Dx = dir });
            Say(B, dir < 0 ? Loc.T("Platform batıya yattı.") : Loc.T("Platform doğuya yattı."), "sys");
            int Down(int x) => dir < 0 ? x : -x;
            // 1) Birimler, yokuş aşağıdan (öndeki önce kayar: aynı sıradakiler birlikte kayar, öndeki çarptıysa arkadaki ona çarpar)
            foreach (var u in B.Units.Where(OnBoard).OrderBy(u => Down(u.X)).ThenBy(u => u.Y).ToList())
            {
                if (!OnBoard(u) || u.Type == "capa") continue;
                Push(B, u, d, slide: true);
            }
            // 2) Hurda (TESIS §10.2'nin kayanlar listesi; "bütün nesneler sabit" cümlesinin tek istisnası): boş zemine, bağ kuralıyla
            foreach (var p in AllTiles().Where(p => CellAt(B, p.X, p.Y).O?.K == "hurda").OrderBy(p => Down(p.X)).ThenBy(p => p.Y).ToList())
            {
                var src = CellAt(B, p.X, p.Y);
                var o = src.O;
                int nx = p.X + dir;
                if (o == null || o.K != "hurda" || !FreeFloor(B, nx, p.Y)) continue;
                var before = TeamLinks(B);
                src.O = null;
                CellAt(B, nx, p.Y).O = o;
                if (!TeamSafe(B, before)) { CellAt(B, nx, p.Y).O = null; src.O = o; continue; }
                AddFx(B, new Fx { K = "slide", X = p.X, Y = p.Y, Dx = dir, N = 1, Obj = "hurda" });
            }
            // 3) Yatan gövdeler ve yerdeki yük (vincin yük kuralı emsali): yokuş aşağıdan
            var loose = new List<(Unit Body, int X, int Y)>();
            foreach (var u in B.Units) if (LyingBody(u)) loose.Add((u, u.X, u.Y));
            if (B.CargoX >= 0) loose.Add((null, B.CargoX, B.CargoY));
            foreach (var (body, x0, y0) in loose.OrderBy(l => Down(l.X)).ThenBy(l => l.Y).ToList())
            {
                int nx = x0 + dir;
                if (!Passable(B, nx, y0)) continue;
                var c = CellAt(B, nx, y0);
                if (c.Door != 0 || c.Vent != 0) continue;
                if (B.CargoX == nx && B.CargoY == y0) continue;
                if (B.Units.Any(b => b != body && LyingBody(b) && b.X == nx && b.Y == y0)) continue;
                if (body != null) body.X = nx;
                else B.CargoX = nx;
                AddFx(B, new Fx { K = "slide", X = x0, Y = y0, Dx = dir, N = 1, Unit = body?.Id ?? 0, Obj = body != null ? "body" : "cargo" });
            }
        }

        /// <summary>
        /// Yatış turunda (<see cref="TiltAt"/>(B, B.Turn)) kayacak ve çarpacak şeyler: durumun kopyasında <see cref="EndTilt"/> uygulanır,
        /// her "slide" Fx'i bir hat olur (Kind "slide", X/Y başlangıç, X2/Y2 yöndeki kare, Hit: çarptı). Şimdiki yerlerden hesaplanır
        /// (makine evresi yatıştan önce işler: hat yaklaşıktır). Yatış turu değilse boş. Saf; RNG çekmez. Sunum hover'ı ve
        /// <see cref="Hazards"/> okur.
        /// </summary>
        public static List<HazardLine> Slides(BState B)
        {
            var o = new List<HazardLine>();
            if (B == null || !TiltAt(B, B.Turn)) return o;
            var C = B.Clone();
            C.FxList.Clear();
            C.InEvent = true;
            EndTilt(C, C.Turn);
            foreach (var f in C.FxList)
                if (f.K == "slide") o.Add(new HazardLine { X = f.X, Y = f.Y, X2 = f.X + f.Dx, Y2 = f.Y + f.Dy, Kind = "slide", Hit = f.N == 0 });
            return o;
        }

        // ---------- Test kancaları ----------
        /// <summary>Yalnız testler: bu turun olay adımı (<see cref="EventEnd"/>; olay yoksa hiçbir şey), makine evresi olmadan.</summary>
        internal static void TestEventStep(BState B) { if (B.Ev != null) EventEnd(B); }

        /// <summary>Yalnız testler: makine aklının olay konum terimi (<see cref="EventPos"/>).</summary>
        internal static double TestEventPos(BState B, Unit e, Pt pos) => EventPos(B, e, pos);
    }
}
