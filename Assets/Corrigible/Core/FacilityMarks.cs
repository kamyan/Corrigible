using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis aşama 11: kalıcı tahta (Docs/TESIS-BASKINLARI-TASARIMI.md §9.1; uygulama tasarımı §2.1–2.2). Bir baskında bırakılan izler
    /// (<see cref="FacilityMark"/>) sonraki baskının ilk ziyaret tahtasında, savunma tahtasında ve bina planında aynı fonksiyonla uygulanır
    /// (<see cref="Apply"/>; plan = emirdeki tahta); <c>raidres</c>'te oynanan odaların raporlarından türetilir (<see cref="Derive"/>) ve
    /// birleştirilir (<see cref="Merge"/>). Hepsi saf ve RNG'sizdir; durum <see cref="FacilityState.Marks"/>'tadır, kayda girmez.
    /// </summary>
    public static class FacilityMarks
    {
        /// <summary>İz türleri (kısa ASCII kimlik; JSON'da durur): kalıcı türler (yıkılan duvar, açık kepenk, kırık donanım, kırılan
        /// soğutma ünitesi ve kopya dolabı) ve engeller (tel, kalas, hurda; kalan canıyla).</summary>
        public const string Wall = "duvar", Shutter = "kepenk", Device = "donanim", Cool = "cool", Spawner = "spawner",
            Wire = "tel", Plank = "kalas", Scrap = "hurda";

        /// <summary>Bütün türler, sabit sıra.</summary>
        public static readonly string[] Kinds = { Wall, Shutter, Device, Cool, Spawner, Wire, Plank, Scrap };

        /// <summary>Engel türü (tel, kalas, hurda): tahtada nesnedir, son ziyaretten türetilir, kırılınca iz düşer.</summary>
        public static bool IsObjectKind(string k) => k == Wire || k == Plank || k == Scrap;

        /// <summary>Kalıcı tür (oyunda geri alınamaz; yalnız YZ onarımı ya da terk siler).</summary>
        public static bool IsPermanent(string k) => k == Wall || k == Shutter || k == Device || k == Cool || k == Spawner;

        /// <summary>Oda kimliği ("weu_2.1") kanadın (sektörün) odası mı.</summary>
        public static bool InWing(string roomId, string sectorId) =>
            sectorId != null && FacilityMaps.ParseRoomId(roomId, out var s, out _) && s == sectorId;

        /// <summary>Odanın izleri, liste sırasıyla.</summary>
        public static List<FacilityMark> Of(IEnumerable<FacilityMark> marks, string roomId) =>
            marks == null ? new List<FacilityMark>() : marks.Where(m => m != null && m.Room == roomId).ToList();

        /// <summary>İzin kopyası.</summary>
        public static FacilityMark Copy(FacilityMark m) => new FacilityMark { Room = m.Room, Kind = m.Kind, X = m.X, Y = m.Y, Hp = m.Hp, Max = m.Max, Day = m.Day };

        // ---------- Uygulama ----------
        /// <summary>
        /// Odanın izlerini hücrelere uygular (saf, çekimsiz; <paramref name="cells"/> yerinde değişir). Önce kalıcı türler, sonra engeller,
        /// her biri liste sırasıyla. Yıkılan duvar zemin olur (çift cidar ve raf bayrağı silinir, kare <paramref name="rubble"/>'a eklenir);
        /// kepenk açık kalır (bayrak kalır); karedeki donanım, soğutma ünitesi ve kopya dolabı kalkar; tel, kalas ve hurda kalan canıyla
        /// konur. Uygulanamayan iz atlanır ve döner (liste sırasıyla): kalıcı türde kare artık o şey değil; engelde kare zemin değil, dolu
        /// (şart parçası, yayıcı, hedef, sivil, nesne), kapı, geçit ya da menfez karesi, ya da konunca batı girişinden ileri geçidin bir
        /// karesine ya da odağın (hedef, donanım) bir komşusuna önceden tutan yürüme bağı kopuyor (duvar ve nesne geçilmez, birimler
        /// sayılmaz; K11). <paramref name="events"/>: odada şart kurulu (parça kareleri dışlanır; oda tahtasının dışlamasıyla aynı).
        /// </summary>
        public static List<FacilityMark> Apply(Baskin.Cell[,] cells, FacilityRoom room, IEnumerable<FacilityMark> marks, List<Baskin.Pt> rubble, bool events)
        {
            var skipped = new List<FacilityMark>();
            if (cells == null || room == null || marks == null) return skipped;
            var mine = Of(marks, room.Id);
            if (mine.Count == 0) return skipped;
            var parts = events && room.Event != null ? new HashSet<int>(room.PartTiles(3)) : new HashSet<int>();
            foreach (var m in mine) if (!IsObjectKind(m.Kind) && !ApplyPermanent(cells, m, rubble)) skipped.Add(m);
            foreach (var m in mine) if (IsObjectKind(m.Kind) && !ApplyObject(cells, room, m, parts)) skipped.Add(m);
            return mine.Where(m => skipped.Contains(m)).ToList();
        }

        static bool In(int x, int y) => x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N;

        static bool ApplyPermanent(Baskin.Cell[,] cells, FacilityMark m, List<Baskin.Pt> rubble)
        {
            if (!In(m.X, m.Y)) return false;
            var c = cells[m.Y, m.X];
            switch (m.Kind)
            {
                case Wall:
                    if (c.T != "wall" || c.Shutter || c.Crate) return false;
                    c.T = "floor";
                    c.Double = false;
                    c.Shelf = false;
                    rubble?.Add(new Baskin.Pt(m.X, m.Y));
                    return true;
                case Shutter:
                    if (!c.Shutter || c.T != "wall") return false;
                    c.T = "floor";
                    return true;
                case Device:
                    if (c.O == null || !Baskin.IsDevice(c.O.K)) return false;
                    c.O = null;
                    return true;
                case Cool:
                case Spawner:
                    if (c.O?.K != m.Kind) return false;
                    c.O = null;
                    return true;
                default:
                    return false;
            }
        }

        static bool ApplyObject(Baskin.Cell[,] cells, FacilityRoom room, FacilityMark m, HashSet<int> parts)
        {
            if (!In(m.X, m.Y)) return false;
            var c = cells[m.Y, m.X];
            if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || parts.Contains(m.Y * Baskin.N + m.X)) return false;
            var before = Links(cells, room);
            int hp = Math.Max(1, m.Hp);
            c.O = new Baskin.Obj { K = m.Kind, Hp = hp, Max = Math.Max(hp, m.Max) };
            var after = Links(cells, room);
            for (int i = 0; i < before.Count; i++)
                if (before[i] && !after[i]) { c.O = null; return false; }
            return true;
        }

        /// <summary>
        /// Odanın yürüme bağları (birimler sayılmaz; duvar ve nesne geçilmez): batı girişinden ileri geçidin her karesine, ve odağa (hedef
        /// odasında hedef, öbür odalarda donanım): odak karesi geçilebilirse kendisine, değilse bir dik komşusuna ulaşılıyor mu.
        /// </summary>
        public static List<bool> Links(Baskin.Cell[,] cells, FacilityRoom room)
        {
            var seen = new bool[Baskin.N, Baskin.N];
            bool Open(int x, int y) => In(x, y) && cells[y, x].T != "wall" && cells[y, x].O == null;
            var q = new Queue<Baskin.Pt>();
            foreach (var w in room.West) if (Open(w.X, w.Y) && !seen[w.Y, w.X]) { seen[w.Y, w.X] = true; q.Enqueue(w); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Baskin.Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (!Open(x, y) || seen[y, x]) continue;
                    seen[y, x] = true;
                    q.Enqueue(new Baskin.Pt(x, y));
                }
            }
            var links = new List<bool>();
            foreach (var f in room.Forward) links.Add(In(f.X, f.Y) && seen[f.Y, f.X]);
            var t = room.Focus;
            links.Add(Open(t.X, t.Y) ? seen[t.Y, t.X] : Baskin.Dirs.Any(d => In(t.X + d[0], t.Y + d[1]) && seen[t.Y + d[1], t.X + d[0]]));
            return links;
        }

        // ---------- Türetme ve birleştirme ----------
        /// <summary>
        /// Odanın bu baskındaki ziyaretlerinden (<paramref name="visits"/>, oynanış sırasıyla; dönüş dahil) iz kümesi (saf). Kalıcı türler:
        /// yıkılan duvar = ziyaretlerin molozu ∩ plandaki duvarlar; açık kepenk = ziyaretlerin açtığı kepenkler; kırık donanım = herhangi
        /// ziyaretin Device'ı doluysa planın donanım karesi; soğutma ünitesi ve kopya dolabı = planda olup son ziyaretin nesnelerinde
        /// olmayan. Engeller = son ziyaretin nesnelerindeki tel, kalas, hurda (kalan canıyla). İz değil: makineler, sivil, hedef ve röle,
        /// şart nesneleri, koku yayıcı, büyük sandık. Tekrar yok (tür ve kare). <paramref name="plan"/>: odanın emirdeki plan hücreleri
        /// (<see cref="FacilityRoom.PlanCells"/>; null ise şartsız plan). Sıra: duvar, kepenk, donanım, soğutma/kopya dolabı (y, x), engeller.
        /// </summary>
        public static List<FacilityMark> Derive(FacilityRoom room, IReadOnlyList<RoomReport> visits, int day, Baskin.Cell[,] plan = null)
        {
            var o = new List<FacilityMark>();
            if (room == null || visits == null || visits.Count == 0) return o;
            plan = plan ?? room.BaseCells;
            if (plan == null) return o;
            var last = visits[visits.Count - 1];
            void Add(string kind, int x, int y, int hp = 0, int max = 0)
            {
                if (!In(x, y) || o.Any(m => m.Kind == kind && m.X == x && m.Y == y)) return;
                o.Add(new FacilityMark { Room = room.Id, Kind = kind, X = x, Y = y, Hp = hp, Max = max, Day = day });
            }
            foreach (var v in visits)
                foreach (int k in v.Rubble ?? new List<int>())
                {
                    int x = k % Baskin.N, y = k / Baskin.N;
                    if (In(x, y) && plan[y, x].T == "wall" && !plan[y, x].Shutter) Add(Wall, x, y);
                }
            foreach (var v in visits)
                foreach (int k in v.Opened ?? new List<int>())
                {
                    int x = k % Baskin.N, y = k / Baskin.N;
                    if (In(x, y) && plan[y, x].Shutter) Add(Shutter, x, y);
                }
            if (room.Device != null && room.DeviceAt is Baskin.Pt dv && visits.Any(v => !string.IsNullOrEmpty(v.Device))) Add(Device, dv.X, dv.Y);
            var objects = last.Objects ?? new List<RoomObject>();
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var po = plan[y, x].O;
                    if (po == null || po.K != Cool && po.K != Spawner) continue;
                    if (!objects.Any(ob => ob.K == po.K && ob.X == x && ob.Y == y)) Add(po.K, x, y);
                }
            foreach (var ob in objects.OrderBy(ob => ob.Y).ThenBy(ob => ob.X))
                if (!ob.MachineBuilt && IsObjectKind(ob.K)) Add(ob.K, ob.X, ob.Y, ob.Hp, ob.Max);
            return o;
        }

        /// <summary>
        /// Odanın türetilmiş izlerini kalıcı tahtaya birleştirir (<paramref name="marks"/> yerinde değişir; saf, çekimsiz). Kalıcı izler
        /// yerinde kalır (gün ve sıra korunur; uygulanamayan da); kümede yeni olan kalıcı iz sona eklenir. Engel izlerinden kümede aynı
        /// (tür, kare) olan yerinde kalır, canı güncellenir; kümede olmayan silinir (makine ya da oyuncu kırdı, uygulanamadı: K29); kümede
        /// yeni olan sona eklenir. Yeni izler kümedeki günle (<see cref="Derive"/>'ın günü) eklenir. Öbür odaların izleri değişmez.
        /// Dönüş: eklenen ve silinen iz sayısı.
        /// </summary>
        public static (int New, int Gone) Merge(List<FacilityMark> marks, string roomId, IEnumerable<FacilityMark> derived)
        {
            if (marks == null) return (0, 0);
            var set = (derived ?? Enumerable.Empty<FacilityMark>()).Where(d => d != null).ToList();
            bool Same(FacilityMark a, FacilityMark b) => a.Kind == b.Kind && a.X == b.X && a.Y == b.Y;
            int added = 0, gone = 0;
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                var m = marks[i];
                if (m == null || m.Room != roomId || !IsObjectKind(m.Kind)) continue;
                var d = set.FirstOrDefault(x => Same(x, m));
                if (d == null) { marks.RemoveAt(i); gone++; continue; }
                m.Hp = d.Hp;
                m.Max = d.Max;
            }
            foreach (var d in set)
            {
                if (marks.Any(m => m != null && m.Room == roomId && Same(m, d))) continue;
                var c = Copy(d);
                c.Room = roomId;
                marks.Add(c);
                added++;
            }
            return (added, gone);
        }

        /// <summary>Odanın bütün izlerini siler (savunmada terk edilen oda, K32). Silinen iz sayısı.</summary>
        public static int Wipe(List<FacilityMark> marks, string roomId) => marks?.RemoveAll(m => m != null && m.Room == roomId) ?? 0;
    }
}
