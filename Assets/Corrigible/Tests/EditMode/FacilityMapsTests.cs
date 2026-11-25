using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis odalarının üreticisi (<see cref="FacilityMaps"/>, aşama 1): harekâtın 108 odası, kimlikler, her odanın kendi
    /// denetimleri, determinizm ve altın özet, S.Rng'nin dokunulmazlığı, her şartın ara ve hedef odada yer bulması, ayrılmış
    /// kareler, kademe varyantı, donanım kuralı, satır sözlüğü, yapı kuralları (testin kendi BFS'iyle), bölge özellikleri ve hız.
    /// Plan bölge bölge tembel üretilir; testler gereken bölgeyi ister (<see cref="FacilityPlan.Region"/>), hepsini değil.
    /// </summary>
    public class FacilityMapsTests
    {
        /// <summary>Satır sözlüğü (<see cref="FacilityMaps"/>): döküm ve taslak bunun dışında harf taşımaz.</summary>
        const string Dict = ".#~EFo=CPRVSKU";

        static int Count(IEnumerable<string> rows, char c) => rows.Sum(r => r.Count(ch => ch == c));
        static int Man(int ax, int ay, int bx, int by) => Math.Abs(ax - bx) + Math.Abs(ay - by);
        static bool In(int x, int y) => x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N;

        /// <summary>Satırlar üzerinde yürüyüş uzaklığı (testin kendi BFS'i; <paramref name="walk"/> yürünür harf; ulaşılamayan −1).</summary>
        static int[,] Bfs(IReadOnlyList<string> rows, IEnumerable<(int x, int y)> from, Func<char, bool> walk)
        {
            var d = new int[Baskin.N, Baskin.N];
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) d[y, x] = -1;
            var q = new Queue<(int x, int y)>();
            foreach (var f in from) { d[f.y, f.x] = 0; q.Enqueue(f); }
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                foreach (var dd in Baskin.Dirs)
                {
                    int nx = x + dd[0], ny = y + dd[1];
                    if (!In(nx, ny) || d[ny, nx] >= 0 || !walk(rows[ny][nx])) continue;
                    d[ny, nx] = d[y, x] + 1; q.Enqueue((nx, ny));
                }
            }
            return d;
        }

        /// <summary>Hücreler üzerinde yürüyüş uzaklığı (testin kendi BFS'i: duvar ve nesne geçilmez; <paramref name="open"/> karesi
        /// geçilir: açık kepenk; ulaşılamayan −1).</summary>
        static int[,] CellBfs(Baskin.Cell[,] cells, IEnumerable<Baskin.Pt> from, Baskin.Pt? open = null)
        {
            bool Walk(int x, int y) => cells[y, x].T != "wall" && cells[y, x].O == null || open is Baskin.Pt o && o.X == x && o.Y == y;
            var d = new int[Baskin.N, Baskin.N];
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) d[y, x] = -1;
            var q = new Queue<(int x, int y)>();
            foreach (var f in from) if (Walk(f.X, f.Y) && d[f.Y, f.X] < 0) { d[f.Y, f.X] = 0; q.Enqueue((f.X, f.Y)); }
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                foreach (var dd in Baskin.Dirs)
                {
                    int nx = x + dd[0], ny = y + dd[1];
                    if (!In(nx, ny) || d[ny, nx] >= 0 || !Walk(nx, ny)) continue;
                    d[ny, nx] = d[y, x] + 1; q.Enqueue((nx, ny));
                }
            }
            return d;
        }

        static List<(int x, int y)> Tiles(IReadOnlyList<string> rows, Func<char, bool> pick)
        {
            var o = new List<(int x, int y)>();
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (pick(rows[y][x])) o.Add((x, y));
            return o;
        }

        /// <summary>Hücrenin yapısı (can hariç): zemin, nesne türü, geçit, menfez, kepenk ve şart bayrakları.</summary>
        static bool SameCell(Baskin.Cell a, Baskin.Cell b) =>
            a.T == b.T && a.O?.K == b.O?.K && a.Door == b.Door && a.Vent == b.Vent && a.Shutter == b.Shutter
            && a.Crack == b.Crack && a.Shelf == b.Shelf && a.Phase == b.Phase;

        // ---------- Kimlik ve şekil ----------
        [Test]
        public void Plan_Has108Rooms_WithStableIds([Values(1u, 79u, 4242u)] uint seed)
        {
            var plan = FacilityMaps.For(seed);
            var rooms = plan.Rooms;
            Assert.AreEqual(108, rooms.Count);
            CollectionAssert.AllItemsAreUnique(rooms.Select(r => r.Id).ToList());
            foreach (var r in rooms)
            {
                var def = GameData.Region(r.Region);
                Assert.AreEqual(FacilityMaps.RoomId(r.SectorId, r.Index), r.Id);
                Assert.AreEqual(r.Index, (int)r.Type, r.Id);
                Assert.AreSame(r, plan.Room(r.Id), r.Id);
                Assert.AreEqual(r.Wing <= 2 ? "loot" : "server", r.Mission, r.Id);
                if (r.Type == RoomType.Entry) Assert.IsNull(r.Event, r.Id);
                else Assert.AreEqual(FacilityMaps.EventFor(def, true, true), r.Event, r.Id);
                if (r.Type == RoomType.Entry) Assert.IsTrue(r.Device == "pano" || r.Device == "anten", r.Id + " " + r.Device);
                else if (r.Type == RoomType.Middle) Assert.IsTrue(r.Device == "vana" || r.Device == "anten", r.Id + " " + r.Device);
                else Assert.IsNull(r.Device, r.Id);
                if (r.Type != RoomType.Target) Assert.AreNotEqual(RaidMaps.Hostage, r.MapKind, r.Id);
            }
            foreach (var wing in rooms.GroupBy(r => r.SectorId))
            {
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, wing.Select(r => r.Index).ToArray(), wing.Key);
                CollectionAssert.AreEqual(new[] { RoomType.Entry, RoomType.Middle, RoomType.Target }, wing.Select(r => r.Type).ToArray(), wing.Key);
                CollectionAssert.AreEqual(wing.ToList(), plan.Wing(wing.Key).ToList(), wing.Key);
                Assert.AreEqual(3, wing.Select(r => r.MapKind).Distinct().Count(), "kanatta tür tekrarı " + wing.Key);
            }
            foreach (var region in rooms.GroupBy(r => r.Region))
            {
                Assert.AreEqual(9, region.Count(), region.Key);
                CollectionAssert.AllItemsAreUnique(region.Select(r => r.NameKey).ToList(), "bölgede ad tekrarı " + region.Key);
                foreach (var r in region)
                    if (r.Region == "na" || r.Region == "oc")
                        CollectionAssert.DoesNotContain(RaidMaps.Names[r.MapKind], r.NameKey, r.Id);
                    else
                        CollectionAssert.Contains(RaidMaps.Names[r.MapKind], r.NameKey, r.Id);
            }
        }

        [Test]
        public void Ids_DoNotParseAsSectors()
        {
            foreach (var r in FacilityMaps.For(79).Rooms)
            {
                Assert.AreEqual(-1, GameData.SectorIndex(r.Id), r.Id);
                Assert.IsTrue(FacilityMaps.ParseRoomId(r.Id, out var sector, out int index), r.Id);
                Assert.AreEqual(r.SectorId, sector, r.Id);
                Assert.AreEqual(r.Index, index, r.Id);
            }
            foreach (var bad in new[] { "weu_2.0", "weu_2.4", "weu_2", ".1", "weu_2.x", "", null })
                Assert.IsFalse(FacilityMaps.ParseRoomId(bad, out _, out _), bad ?? "null");
        }

        /// <summary>
        /// Recheck, üreticinin odayı kabul etmeden önce çalıştırdığı denetimin aynısıdır: burada yalnız planın üretimden sonra
        /// değişmediğini korur (önbellekteki plan paylaşılır). Bağımsız iddia: ileri geçide yol kuralı testin kendi BFS'iyle şartsız
        /// tahtada ve iki kademe hâlinde (kepenk kapalıyken en çok 8, yalnız bölme kepenginde en çok
        /// <see cref="FacilityMaps.GateClosedMaxSteps"/>; kepenk açıkken en çok 8).
        /// </summary>
        [Test]
        public void EveryRoom_PassesItsChecks([Range(1, 12)] int seed)
        {
            foreach (var r in FacilityMaps.For((uint)seed).Rooms)
            {
                string why = FacilityMaps.Recheck(r);
                Assert.IsNull(why, r.Id + ": " + why + "\n" + string.Join("\n", r.Rows));
                if (r.Type == RoomType.Entry) Assert.IsFalse(r.ShutterPartition, r.Id);
                else
                {
                    Assert.IsNotNull(r.Ev3, r.Id);
                    Assert.IsNotNull(r.Ev1, r.Id);
                }
                if (r.Type == RoomType.Target) continue;
                int closedMax = r.ShutterPartition ? FacilityMaps.GateClosedMaxSteps : FacilityMaps.GateMaxSteps;
                foreach (var (name, cells) in new[] { ("base", r.BaseCells), ("k1", r.Cells1), ("k3", r.Cells3) })
                {
                    if (cells == null) continue;
                    var closed = CellBfs(cells, r.West);
                    Assert.IsTrue(r.Forward.All(f => closed[f.Y, f.X] >= 0 && closed[f.Y, f.X] <= closedMax), $"{r.Id} {name}: geçit uzak (kepenk kapalı)");
                    if (r.Shutter is Baskin.Pt sh)
                    {
                        var open = CellBfs(cells, r.West, sh);
                        Assert.IsTrue(r.Forward.All(f => open[f.Y, f.X] >= 0 && open[f.Y, f.X] <= FacilityMaps.GateMaxSteps), $"{r.Id} {name}: geçit uzak");
                    }
                }
            }
        }

        // ---------- Determinizm ----------
        [Test]
        public void SameSeed_SameRooms_RegardlessOfOrder()
        {
            FacilityMaps.ClearCache();
            var first = FacilityMaps.For(17).Rooms.Select(FacilityMaps.Dump).ToList();
            FacilityMaps.ClearCache();
            var again = FacilityMaps.For(17).Rooms.Select(FacilityMaps.Dump).ToList();
            CollectionAssert.AreEqual(first, again);
            // Önce tek bölge, sonra hepsi: bölge akışları bağımsız, üretim sırası sonucu değiştirmez
            FacilityMaps.ClearCache();
            var plan = FacilityMaps.For(17);
            var oc = plan.Region("oc").Select(FacilityMaps.Dump).ToList();
            CollectionAssert.AreEqual(first, plan.Rooms.Select(FacilityMaps.Dump).ToList());
            CollectionAssert.AreEqual(first.Where(d => d.StartsWith("oc_", StringComparison.Ordinal)).ToList(), oc);
            FacilityMaps.ClearCache();
            var other = new HashSet<string>(FacilityMaps.For(18).Rooms.Select(FacilityMaps.Dump));
            Assert.AreEqual(0, first.Count(other.Contains), "For(17) ile For(18) ortak döküm");
        }

        /// <summary>Altın değer aşama 1'de (Core) Unity dışında, Mono koşum takımıyla yakalandı: 108 odanın dökümü (kimlik, tür, ad,
        /// donanım, şart, satırlar, parçalar). Değişirse üretim değişmiştir: bilinçliyse <see cref="FacilityMaps.GeneratorVersion"/>'ı
        /// artır ve değeri yeniden yakala.</summary>
        [Test]
        public void Golden_Seed79()
        {
            Assert.AreEqual(10, FacilityMaps.GeneratorVersion, "10: simetrik geçit ve döndürülmüş oda; 9: sahadaki sızmacı; 8: aşama 14 (kişisel işler ve unvanlar); 7: aşama 13 (imza makineleri); 6: aşama 12 (Katar ve Yalpa; na ve oc planı); 5: aşama 11 (kalıcı tahta, kuşatma ve savunma); 4: aşama 10 (önlem defteri); 3: aşama 8 (kurallar)");
            FacilityMaps.ClearCache();
            // Sürüm 10 (simetrik geçit, döndürülmüş oda): değer Mono'da Core ile yakalandı; önceki 0x784D4B42 / 0xBB1486CC (sürüm 9)
            Assert.AreEqual(0x8358DEEFu, FacilityMaps.Digest(FacilityMaps.For(79)), "sürüm 10: Katar ve Yalpa oda adları ve önceki şartlar");
            // İki alt bayrak kapalıyken plan (şartlar aşama 11'inki)
            Assert.AreEqual(0x15634C40u, FacilityMaps.Digest(FacilityMaps.For(79, false, false)), "alt bayraklar kapalı");
        }

        /// <summary>Aşama 12 (K4): önbellek anahtarı tohum ve iki alt bayrak; aynı tohum farklı bayrakla farklı plan nesnesi ve şart, öbür on
        /// bölgenin odaları aynı.</summary>
        [Test]
        public void Cache_KeyedBySeedAndSubFlags()
        {
            FacilityMaps.ClearCache();
            var on = FacilityMaps.For(79);
            Assert.AreSame(on, FacilityMaps.For(79, true, true));
            Assert.IsTrue(on.Katar && on.Yalpa);
            var off = FacilityMaps.For(79, false, false);
            Assert.AreNotSame(on, off);
            Assert.IsFalse(off.Katar || off.Yalpa);
            Assert.AreEqual("asansor", off.Room("na_1.2").Event);
            Assert.AreEqual("yag", off.Room("oc_1.2").Event);
            var katar = FacilityMaps.For(79, true, false);
            Assert.AreEqual("katar", katar.Room("na_1.2").Event);
            Assert.AreEqual("yag", katar.Room("oc_1.2").Event);
            var again = FacilityMaps.For(79);
            Assert.AreNotSame(on, again, "son istenen plan önbellekte");
            Assert.AreEqual("katar", again.Room("na_1.3").Event);
            Assert.AreEqual("yalpa", again.Room("oc_1.3").Event);
            foreach (var def in GameData.Regions.Where(d => d.Id != "na" && d.Id != "oc"))
                CollectionAssert.AreEqual(off.Region(def.Id).Select(FacilityMaps.Dump).ToList(), again.Region(def.Id).Select(FacilityMaps.Dump).ToList(), def.Id);
        }

        /// <summary>
        /// Aşama 12 (K6): Katar tesisinde G ve A'nın ileri geçidi yalnız doğu sütununda, açık kenar satırlarına değmeden (vagonlar arası
        /// bağlantı); 200 tohumda na ve oc
        /// üretilir (tükenen tohum yok: tükenirse For istisna atar). na'nın G ve A odalarının ortalama deneme sayısı çıktıya yazılır.
        /// </summary>
        [Test]
        public void Katar_ForwardGateEastColumn_AndNoExhaustedSeed_200Seeds()
        {
            int rooms = 0;
            long attempts = 0;
            for (uint seed = 1; seed <= 200; seed++)
            {
                var plan = new FacilityPlan(seed);
                foreach (var r in plan.Region("na"))
                {
                    if (r.Type == RoomType.Target) continue;
                    Assert.AreEqual(2, r.Forward.Count, r.Id);
                    Assert.IsTrue(r.Forward.All(f => f.X == Baskin.N - 1 && f.Y > 0 && f.Y < Baskin.N - 1), $"{seed} {r.Id}: ileri geçit doğu sütununun içinde değil");
                    rooms++;
                    attempts += r.Attempt + 1;
                }
                Assert.AreEqual(9, plan.Region("oc").Count, seed + " oc");
            }
            TestContext.WriteLine($"na G/A odaları: {rooms}, ortalama deneme {(double)attempts / rooms:0.00}");
        }

        /// <summary>
        /// Sürüm 10: A ve H'nin girişi kanadın önceki odasının ileri geçidine simetriktir: geçit doğu kenarındaysa giriş batıda aynı
        /// satırlarda, kuzeydeyse güneyde aynı sütunlarda (ve tersi). Oda kanonik üretilip döndürülür (<see cref="FacilityRoom.Turn"/>);
        /// kanonik girişi batıdadır. Katar (na) ve Yalpa'da (oc) geçit hep doğuda, oda döndürülmez. 60 tohumda 12 bölge üretilir (tükenen tohum yok:
        /// tükenirse istisna). Deneme ortalaması, yedek tür ve kenar dağılımı çıktıya yazılır.
        /// </summary>
        [Test]
        public void Mirror_EntryMatchesPreviousForwardGate_AllRegions_60Seeds()
        {
            int rooms = 0, fallbacks = 0, maxAttempt = 0;
            long attempts = 0;
            var edges = new Dictionary<string, int>();
            for (uint seed = 1; seed <= 60; seed++)
            {
                var plan = new FacilityPlan(seed);
                foreach (var def in GameData.Regions)
                {
                    var list = plan.Region(def.Id);
                    Assert.AreEqual(9, list.Count, $"{seed} {def.Id}");
                    for (int i = 0; i < list.Count; i++)
                    {
                        var r = list[i];
                        rooms++; attempts += r.Attempt + 1; maxAttempt = Math.Max(maxAttempt, r.Attempt + 1); if (r.KindFallback) fallbacks++;
                        Assert.IsTrue(r.West.All(w => Same(r.ToCanon(w).X, 0)), $"{seed} {r.Id}: kanonik giriş batıda değil");
                        if (r.Type != RoomType.Target)
                        {
                            string edge = r.Forward.All(f => f.X == Baskin.N - 1) ? "dogu" : r.Forward.All(f => f.X == 0) ? "bati" : r.Forward.All(f => f.Y == 0) ? "kuzey" : "guney";
                            edges[edge] = edges.TryGetValue(edge, out int n) ? n + 1 : 1;
                            if (def.Id == "na" || def.Id == "oc") Assert.AreEqual("dogu", edge, $"{seed} {r.Id}: Katar ve Yalpa'da geçit doğuda");
                        }
                        if (r.Type == RoomType.Entry) { Assert.AreEqual(0, r.Turn, r.Id + ": G döndürülmez"); continue; }
                        var prev = list[i - 1].Forward;
                        bool vertical = prev.All(f => f.X == 0) || prev.All(f => f.X == Baskin.N - 1);
                        var mirror = prev.Select(f => vertical ? (Baskin.N - 1 - f.X, f.Y) : (f.X, Baskin.N - 1 - f.Y)).OrderBy(q => q).ToList();
                        var entry = r.West.Select(w => (w.X, w.Y)).OrderBy(q => q).ToList();
                        CollectionAssert.AreEqual(mirror, entry, $"{seed} {r.Id}: giriş önceki ileri geçide simetrik değil");
                    }
                }
            }
            string dist = string.Join(", ", edges.OrderBy(kv => kv.Key).Select(kv => kv.Key + " " + kv.Value));
            UnityEngine.Debug.Log($"[ayna] odalar: {rooms}, ortalama deneme {(double)attempts / rooms:0.00}, en çok {maxAttempt}, yedek tür {fallbacks}; geçit: {dist}");
            TestContext.WriteLine($"odalar: {rooms}, ortalama deneme {(double)attempts / rooms:0.00}, en çok {maxAttempt}, yedek tür {fallbacks}; geçit: {dist}");
        }

        static bool Same(int a, int b) => a == b;

        static void Shift(Simulation s)
        {
            if (s.Board == null) { s.Step(); return; }
            var r = s.PrintShift();
            if (r.Ok) s.FileShift(s.Board.ReceiptId);
        }

        /// <summary>Bina planı S.Rng'ye dokunmaz: planın üretimi (önbellek boşken) ve vardiyalar boyunca planın okunması RNG durumunu
        /// değiştirmez (tek kipte karşılaştırılacak plansız oyun yok: plan hep üretilir).</summary>
        [Test]
        public void MainRng_Untouched([Values(5u, 79u)] uint seed)
        {
            FacilityMaps.ClearCache();
            var sim = Simulation.NewGame(seed, new Balance());
            uint before = sim.S.Rng.State;
            Assert.AreEqual(108, sim.FacilityPlan.Rooms.Count);
            Assert.AreEqual(before, sim.S.Rng.State, "plan üretimi S.Rng'yi değiştirdi");
            var twin = Simulation.NewGame(seed, new Balance());
            for (int i = 0; i < 6; i++)
            {
                Shift(sim);
                FacilityMaps.ClearCache();
                Assert.AreEqual(108, sim.FacilityPlan.Rooms.Count);
                Shift(twin);
                Assert.AreEqual(twin.S.Rng.State, sim.S.Rng.State, "vardiya " + (i + 1));
            }
            Assert.AreEqual(twin.Record.commands.Count, sim.Record.commands.Count);
        }

        // ---------- Her şart her oda tipinde ----------
        /// <summary>
        /// Belgenin sözleşmesi: her şart ara ve hedef oda türünde yer bulur. Kanca (<see cref="FacilityMaps.TryRoom"/>) türü ve şartı
        /// zorlar, 120 deneme yapar, yedek tür kullanmaz. Her (şart, tip, tür) üçlüsü 8 tohumun en az 6'sında; her (şart, tip)
        /// ikilisi yedek türler sırayla denenince 8/8. Tablo ve başarısızlıkların ret kodları test çıktısına yazılır.
        /// </summary>
        [Test]
        public void EveryEvent_FindsAPlace_InMiddleAndTargetRooms()
        {
            var failures = new List<string>();
            foreach (var ev in Baskin.RegionEvents.Keys)
                foreach (var type in new[] { RoomType.Middle, RoomType.Target })
                {
                    var kinds = RaidMaps.Kinds.Where(k => type == RoomType.Target || k != RaidMaps.Hostage).ToList();
                    var found = new HashSet<(string kind, uint seed)>();
                    var line = new List<string>();
                    foreach (var kind in kinds)
                    {
                        int got = 0;
                        var whys = new List<string>();
                        for (uint seed = 1; seed <= 8; seed++)
                        {
                            var room = FacilityMaps.TryRoom(seed, "weu", 1, type, kind, ev, out string why);
                            if (room == null) { whys.Add(seed + ":" + why); continue; }
                            got++;
                            found.Add((kind, seed));
                            Assert.AreEqual(kind, room.MapKind);
                            Assert.AreEqual(ev, room.Event);
                            Assert.IsNull(FacilityMaps.Recheck(room), $"{ev}/{type}/{kind}/{seed}");
                        }
                        line.Add($"{kind} {got}/8");
                        if (got < 6) failures.Add($"{ev}/{type}/{kind}: {got}/8 ({string.Join(" ", whys)})");
                    }
                    int pair = Enumerable.Range(1, 8).Count(s => kinds.Any(k => found.Contains((k, (uint)s))));
                    if (pair < 8) failures.Add($"{ev}/{type}: yedek türlerle {pair}/8");
                    TestContext.WriteLine($"{ev,-8} {type,-6}: {string.Join("  ", line)} | yedekle {pair}/8");
                }
            foreach (var f in failures) TestContext.WriteLine("EKSİK " + f);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // ---------- Ayrılmış kareler ve kademe ----------
        /// <summary>
        /// Ayrılmış kareler odanın kendi alanlarından yeniden kurulur (ileri geçit, menfez uçları, donanımın erişim karesi, kepengin
        /// iki yanı) ve <see cref="FacilityRoom.Reserved"/> ile aynı olmalı. Parça kuralı üreticinin son denetimindekinin (parca)
        /// yazılı karşılığıdır: o denetim gevşerse burada kırılır.
        /// </summary>
        [Test]
        public void ReservedTiles_StayFree([Range(1, 12)] int seed)
        {
            foreach (var r in FacilityMaps.For((uint)seed).Rooms)
            {
                if (r.Type == RoomType.Entry) continue;
                var reserved = new HashSet<(int, int)>(r.Reserved.Select(t => (t.X, t.Y)));
                var expected = new HashSet<(int, int)>(r.Forward.Concat(r.Vents).Concat(r.ShutterDoors).Select(t => (t.X, t.Y)));
                if (r.DeviceAccess is Baskin.Pt da) expected.Add((da.X, da.Y));
                Assert.IsTrue(expected.SetEquals(reserved), $"{r.Id}: ayrılmış kareler");
                foreach (var p in r.Parts)
                {
                    if (p.Kind == "hol" || p.Kind == "turnike" || p.Kind == "turnike-acik") continue;
                    Assert.IsFalse(reserved.Contains((p.At.X, p.At.Y)), $"{r.Id}: {p.Kind} {p.At.X},{p.At.Y} ayrılmış karede");
                    if (FacilityMaps.IsPhysicalPart(p.Kind))
                        Assert.IsTrue(r.West.All(w => Man(w.X, w.Y, p.At.X, p.At.Y) >= 2), $"{r.Id}: {p.Kind} {p.At.X},{p.At.Y} girişe bitişik");
                }
            }
        }

        /// <summary>
        /// Kademe varyantı: tavanda (af) ve tüpte (sa) en çok bir parça yalnız 3. kademede; 1–2. kademenin hâli 3. kademeninkinin
        /// o parça çıkmış hâli. Öbür şartlarda iki hâl aynı başvuru (tek kurulum). Öbür bölgelerden tohum başına biri sınanır (hepsi
        /// değil: süre).
        /// </summary>
        [Test]
        public void TierVariant_LowIsSubsetOfHigh([Range(1, 30)] int seed)
        {
            var plan = FacilityMaps.For((uint)seed);
            var others = GameData.Regions.Where(d => d.Id != "af" && d.Id != "sa").ToList();
            var regions = new[] { "af", "sa", others[seed % others.Count].Id };
            foreach (var r in regions.SelectMany(id => plan.Region(id)))
            {
                if (r.Type == RoomType.Entry) continue;
                var high = r.Parts.Where(p => p.MinTier == 3).ToList();
                if (r.Event != "tavan" && r.Event != "tup")
                {
                    Assert.AreEqual(0, high.Count, r.Id);
                    Assert.AreSame(r.Cells3, r.Cells1, r.Id);
                    Assert.AreSame(r.Ev3, r.Ev1, r.Id);
                    continue;
                }
                Assert.LessOrEqual(high.Count, 1, r.Id);
                var diff = new HashSet<(int, int)>(high.Select(p => (p.At.X, p.At.Y)));
                if (r.Event == "tavan")
                {
                    var low = r.PartsAt(1).Where(p => p.Kind == "catlak").Select(p => (p.At.X, p.At.Y)).OrderBy(t => t).ToList();
                    var cracks = new List<(int, int)>();
                    for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (r.Cells1[y, x].Crack) cracks.Add((x, y));
                    CollectionAssert.AreEqual(low, cracks.OrderBy(t => t).ToList(), r.Id);
                }
                else
                {
                    var low = r.PartsAt(1).Where(p => p.Kind == "tup").Select(p => (p.At.X, p.At.Y, p.Dir)).ToList();
                    CollectionAssert.AreEqual(low, r.Ev1.Fuses.Select(f => (f.X, f.Y, f.Dir)).ToList(), r.Id);
                    var tubes = new List<(int, int)>();
                    for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (r.Cells1[y, x].O?.K == "tup") tubes.Add((x, y));
                    CollectionAssert.AreEquivalent(low.Select(t => (t.X, t.Y)).ToList(), tubes, r.Id);
                }
                // Hücre hücre: fark karesi dışında aynı; fark karesinde düşük kademede o parça yok
                for (int y = 0; y < Baskin.N; y++)
                    for (int x = 0; x < Baskin.N; x++)
                    {
                        Baskin.Cell lo = r.Cells1[y, x], hi = r.Cells3[y, x];
                        if (!diff.Contains((x, y))) { Assert.IsTrue(SameCell(lo, hi), $"{r.Id} {x},{y}"); continue; }
                        if (r.Event == "tavan") Assert.IsTrue(hi.Crack && !lo.Crack && lo.T == hi.T && lo.O?.K == hi.O?.K, $"{r.Id} fark {x},{y}");
                        else Assert.IsTrue(hi.O?.K == "tup" && lo.O?.K != "tup" && lo.T == hi.T, $"{r.Id} fark {x},{y}");
                    }
            }
        }

        /// <summary>
        /// Donanım kuralı: her kanatta (giriş, ara oda) <see cref="FacilityMaps.DeviceTriples"/>'tan biri (aynı kanatta anten iki
        /// kez gelmez). 100 tohumda üç üçlünün her biri en az 10 kez görülür (eşit olasılık). Tohum başına bir bölge (sırayla).
        /// </summary>
        [Test]
        public void Devices_FollowTheWingRule()
        {
            var seen = FacilityMaps.DeviceTriples.ToDictionary(t => t, t => 0);
            for (int seed = 1; seed <= 100; seed++)
            {
                var region = GameData.Regions[seed % GameData.Regions.Length].Id;
                foreach (var wing in FacilityMaps.For((uint)seed).Region(region).GroupBy(r => r.SectorId))
                {
                    var triple = (wing.First(r => r.Type == RoomType.Entry).Device, wing.First(r => r.Type == RoomType.Middle).Device);
                    Assert.IsTrue(seen.ContainsKey(triple), $"{seed} {wing.Key}: {triple}");
                    seen[triple]++;
                }
            }
            TestContext.WriteLine(string.Join(", ", seen.Select(kv => $"{kv.Key.Entry}/{kv.Key.Middle} {kv.Value}")));
            foreach (var kv in seen) Assert.GreaterOrEqual(kv.Value, 10, $"{kv.Key.Entry}/{kv.Key.Middle}");
        }

        [Test]
        public void Plan_HasNoRobots([Values(1u, 79u)] uint seed)
        {
            foreach (var r in FacilityMaps.For(seed).Rooms)
            {
                Assert.AreEqual(Baskin.N, r.Rows.Count, r.Id);
                Assert.AreEqual(Baskin.N, r.DraftRows.Length, r.Id);
                foreach (var row in r.Rows) Assert.IsTrue(row.Length == Baskin.N && row.All(ch => Dict.IndexOf(ch) >= 0), r.Id + ": " + row);
                // Taslakta menfez ve kepenk yok (serpiştirmeden sonra konur)
                foreach (var row in r.DraftRows) Assert.IsTrue(row.Length == Baskin.N && row.All(ch => Dict.IndexOf(ch) >= 0 && ch != 'o' && ch != '='), r.Id + ": " + row);
                foreach (var robot in new[] { 'd', 'm', 'Y' })
                {
                    Assert.AreEqual(0, Count(r.Rows, robot), r.Id);
                    Assert.AreEqual(0, Count(r.DraftRows, robot), r.Id);
                }
            }
        }

        // ---------- Yapı ----------
        /// <summary>
        /// Yapı kuralları <see cref="FacilityRoom.Rows"/> üzerinden, testin kendi BFS'iyle (yürünür: '.', '~', 'E', 'F', 'o'):
        /// G ve A'da iki bitişik ileri geçit kenarda, x ≥ 4, donanımın 3×3'ünün dışında, girişe en çok 8 adım (kepenk kapalı). Yalnız
        /// bölme kepengi olan A odasında kapalıyken en çok <see cref="FacilityMaps.GateClosedMaxSteps"/> (gevşetme; geliştirici
        /// onayladı); kepenk açıkken her zaman en çok 8;
        /// tek kepenk ve açılınca en yakın geçide yol en az 2 kısalır; tam iki menfez ucu (batı x ≤ 3, öbürü x ≥ 4 ve geçide ya da
        /// donanıma en çok 2), uçlar arası yürüyüş en az 5. H'de hedefin bir komşusuna yol var.
        /// </summary>
        [Test]
        public void Structure_FollowsTheRules([Range(1, 12)] int seed)
        {
            foreach (var r in FacilityMaps.For((uint)seed).Rooms)
            {
                // Sürüm 10: kurallar kanonik düzende (giriş batıda) denetlenir; oda döndürülmüş olabilir (FacilityRoom.Turn)
                var rows = Enumerable.Range(0, Baskin.N).Select(y => new string(Enumerable.Range(0, Baskin.N)
                    .Select(x => { var a = r.FromCanon(new Baskin.Pt(x, y)); return r.Rows[a.Y][a.X]; }).ToArray())).ToList();
                bool Closed(char ch) => ".~EFo".IndexOf(ch) >= 0;
                bool Open(char ch) => Closed(ch) || ch == '=';
                var E = Tiles(rows, ch => ch == 'E');
                var F = Tiles(rows, ch => ch == 'F');
                var focus = Tiles(rows, ch => "CPRV".IndexOf(ch) >= 0).Single();
                var dist = Bfs(rows, E, Closed);
                Assert.AreEqual(r.Type == RoomType.Entry ? 3 : 2, E.Count, r.Id);
                Assert.IsTrue(E.All(t => t.x == 0), r.Id);
                if (r.Type == RoomType.Target)
                {
                    Assert.AreEqual(0, F.Count, r.Id);
                    Assert.IsTrue(Baskin.Dirs.Any(d => In(focus.x + d[0], focus.y + d[1]) && dist[focus.y + d[1], focus.x + d[0]] >= 0), r.Id + ": hedefe yol yok");
                    continue;
                }
                Assert.AreEqual(2, F.Count, r.Id);
                Assert.AreEqual(1, Man(F[0].x, F[0].y, F[1].x, F[1].y), r.Id + ": geçit bitişik değil");
                Assert.IsTrue(F.All(f => f.x >= 4 && (f.x == Baskin.N - 1 || f.y == 0 || f.y == Baskin.N - 1)), r.Id + ": geçit kenarda değil");
                Assert.IsTrue(F.All(f => Math.Max(Math.Abs(f.x - focus.x), Math.Abs(f.y - focus.y)) >= 2), r.Id + ": geçit donanımın 3×3'ünde");
                var open = Bfs(rows, E, Open);
                Assert.IsTrue(!r.ShutterPartition || r.Type == RoomType.Middle, r.Id + ": bölme kepengi A dışında");
                int closedMax = r.ShutterPartition ? FacilityMaps.GateClosedMaxSteps : FacilityMaps.GateMaxSteps;
                Assert.IsTrue(F.All(f => dist[f.y, f.x] >= 0 && dist[f.y, f.x] <= closedMax), r.Id + ": geçit uzak (kepenk kapalı)");
                Assert.IsTrue(F.All(f => open[f.y, f.x] >= 0 && open[f.y, f.x] <= FacilityMaps.GateMaxSteps), r.Id + ": geçit uzak");
                if (r.Type == RoomType.Entry)
                {
                    Assert.AreEqual(0, Count(rows, '='), r.Id);
                    Assert.AreEqual(0, Count(rows, 'o'), r.Id);
                    continue;
                }
                Assert.AreEqual(1, Count(rows, '='), r.Id + ": kepenk");
                int gain = F.Min(f => dist[f.y, f.x]) - F.Min(f => open[f.y, f.x]);
                Assert.GreaterOrEqual(gain, FacilityMaps.ShutterSaving, r.Id + ": kepenk kazancı");
                var vents = Tiles(rows, ch => ch == 'o');
                Assert.AreEqual(2, vents.Count, r.Id + ": menfez");
                var westEnd = vents.Where(v => v.x <= 3).ToList();
                var fwdEnd = vents.Where(v => v.x >= 4).ToList();
                Assert.IsTrue(westEnd.Count == 1 && fwdEnd.Count == 1, r.Id + ": menfez uçları");
                var fe = fwdEnd[0];
                Assert.IsTrue(F.Any(f => Man(f.x, f.y, fe.x, fe.y) <= 2) || Man(focus.x, focus.y, fe.x, fe.y) <= 2, r.Id + ": ileri menfez uzak");
                Assert.GreaterOrEqual(Bfs(rows, westEnd, Closed)[fe.y, fe.x], FacilityMaps.VentMinWalk, r.Id + ": menfez yürüyüşü");
            }
        }

        /// <summary>
        /// Bölge özellikleri planda (geliştirici kararı): duvar ve kablo ekleyen/yıkan özellikler (dar, sebeke, acik) her odada,
        /// nesne ekleyenler (sogutma, kalabalik, fabrika) yalnız hedef odada. Fark serpiştirme öncesi taslağa göre ölçülür. A odasının
        /// bölme kepengi (zeminden kurulan kepenk ve yola dik iki duvarı) özellik sayılmaz.
        /// </summary>
        [Test]
        public void RegionTraits_LiveInThePlan()
        {
            var deltas = new Dictionary<string, int>();
            for (uint seed = 1; seed <= 5; seed++)
            {
                var plan = FacilityMaps.For(seed);
                foreach (var id in new[] { "eeu", "jk", "ru", "in", "sea", "af" })
                    foreach (var r in plan.Region(id))
                    {
                        string trait = GameData.Region(id).Trait;
                        bool target = r.Type == RoomType.Target;
                        int walls = Count(r.Rows, '#') + Count(r.Rows, '=') - Count(r.DraftRows, '#') - PartitionWalls(r);
                        int cables = Count(r.Rows, '~') - Count(r.DraftRows, '~');
                        int cool = Count(r.Rows, 'K') - Count(r.DraftRows, 'K');
                        int civ = Count(r.Rows, 'S') - Count(r.DraftRows, 'S');
                        int spawner = Count(r.Rows, 'U') - Count(r.DraftRows, 'U');
                        int delta = trait == "dar" || trait == "acik" ? walls : trait == "sebeke" ? cables : trait == "sogutma" ? cool : trait == "kalabalik" ? civ : spawner;
                        string key = id + " " + r.Type;
                        deltas[key] = Math.Max(deltas.TryGetValue(key, out int m) ? m : int.MinValue, Math.Abs(delta));
                        switch (trait)
                        {
                            case "dar": Assert.IsTrue(walls >= 0 && walls <= 3, $"{r.Id}: dar {walls}"); break;
                            case "acik":
                                Assert.IsTrue(walls <= 0 && walls >= -3, $"{r.Id}: acik {walls}");
                                if (r.Type == RoomType.Middle)
                                {
                                    var sh = r.Shutter.Value;
                                    Assert.AreEqual('=', r.Rows[sh.Y][sh.X], r.Id + ": kepenk yerinde değil");
                                    Assert.IsTrue(r.BaseCells[sh.Y, sh.X].T == "wall" && r.BaseCells[sh.Y, sh.X].Shutter, r.Id);
                                }
                                break;
                            case "sebeke":
                                Assert.IsTrue(cables >= 0 && cables <= 3, $"{r.Id}: sebeke {cables}");
                                foreach (var t in r.Forward.Concat(r.Vents).Concat(r.West)) Assert.AreEqual("floor", r.BaseCells[t.Y, t.X].T, $"{r.Id}: {t.X},{t.Y} kablo");
                                break;
                            case "sogutma": Assert.IsTrue(target ? cool >= 0 && cool <= 2 : cool == 0, $"{r.Id}: sogutma {cool}"); break;
                            case "kalabalik": Assert.IsTrue(target ? civ >= 0 && civ <= 2 : civ == 0, $"{r.Id}: kalabalik {civ}"); break;
                            case "fabrika":
                                Assert.IsTrue(target ? spawner >= 0 && spawner <= 1 : spawner == 0, $"{r.Id}: fabrika {spawner}");
                                if (r.Type == RoomType.Entry) Assert.AreEqual(0, Count(r.Rows, 'U'), r.Id + ": G'de kopya dolabı");
                                break;
                        }
                        // Nesne ekleyen özellik G ve A'da tahtaya hiçbir şey eklemez; duvar/kablo ekleyen nesneye dokunmaz
                        if (trait == "dar" || trait == "acik" || trait == "sebeke") Assert.IsTrue(cool == 0 && civ == 0 && spawner == 0, r.Id);
                        if (!target && trait != "dar" && trait != "acik" && trait != "sebeke") Assert.IsTrue(walls == 0 && cables == 0, r.Id);
                    }
            }
            foreach (var kv in deltas.OrderBy(k => k.Key, StringComparer.Ordinal)) TestContext.WriteLine($"{kv.Key}: en büyük fark {kv.Value}");
            // Her özellik en az bir odada işler: duvar ve kablo her oda tipinde, nesneler hedef odada
            foreach (var id in new[] { "eeu", "jk", "af" })
                foreach (var type in new[] { RoomType.Entry, RoomType.Middle, RoomType.Target })
                    Assert.Greater(deltas[id + " " + type], 0, id + " " + type);
            foreach (var id in new[] { "ru", "in", "sea" }) Assert.Greater(deltas[id + " " + RoomType.Target], 0, id);
        }

        /// <summary>A odasının bölme kepengi (kepenk zeminden kurulduysa): kepenk ve yola dik iki komşusundan taslakta zemin olup
        /// dökümde duvar olanlar. Kepenk taslakta duvarsa 0.</summary>
        static int PartitionWalls(FacilityRoom r)
        {
            if (!(r.Shutter is Baskin.Pt sh) || r.DraftRows[sh.Y][sh.X] == '#') return 0;
            var a = r.ShutterDoors[0];
            int dx = Math.Sign(sh.X - a.X), dy = Math.Sign(sh.Y - a.Y);
            int n = 1;
            foreach (var (x, y) in new[] { (sh.X + dy, sh.Y + dx), (sh.X - dy, sh.Y - dx) })
                if (In(x, y) && r.DraftRows[y][x] != '#' && r.Rows[y][x] == '#') n++;
            return n;
        }

        // ---------- Hız ve ölçüm ----------
        /// <summary>
        /// 5 tohumda 108 oda, tohum başına ortalama 1500 ms'den az. Eşik ölçümün çok üstünde tutuldu: Unity dışında Mono ile bu
        /// test ~200 ms/tohum (iyileştirmeli derleme) ve ~325 ms/tohum (Debug derleme) ölçtü; editörün Mono'su daha yavaş olabilir,
        /// eşik Debug ölçümünün ~4,6 katı (en az 3 kat). Süre, ortalama deneme, yedek tür sayısı ve ret dağılımı yazılır.
        /// </summary>
        [Test]
        public void Generation_IsFastEnough()
        {
            var whys = new Dictionary<string, int>();
            FacilityMaps.RejectHook = (id, kind, why) => whys[why] = (whys.TryGetValue(why, out int n) ? n : 0) + 1;
            try
            {
                long total = 0;
                int rooms = 0, attempts = 0, fallback = 0;
                for (uint seed = 1; seed <= 5; seed++)
                {
                    FacilityMaps.ClearCache();
                    var sw = Stopwatch.StartNew();
                    var all = FacilityMaps.For(seed).Rooms;
                    sw.Stop();
                    total += sw.ElapsedMilliseconds;
                    rooms += all.Count;
                    attempts += all.Sum(r => r.Attempt + 1);
                    fallback += all.Count(r => r.KindFallback);
                }
                double avg = total / 5.0;
                TestContext.WriteLine($"Ortalama {avg:F0} ms/tohum; ortalama deneme {attempts / (double)rooms:F2}; yedek tür {fallback}/{rooms}");
                TestContext.WriteLine("Ret: " + string.Join(", ", whys.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}")));
                Assert.Less(avg, 1500.0);
            }
            finally { FacilityMaps.RejectHook = null; FacilityMaps.ClearCache(); }
        }

        /// <summary>
        /// Ölçüm (iddiasız, elle çalıştırılır): 200 tohumda oda tipi × tür × şart başına ortalama deneme, ret dağılımı, yedek tür
        /// oranı, bölme kepengi oranı ve geçit kenarı dağılımı. Mono'da ~50 sn sürer; editörde daha uzun.
        /// </summary>
        [Test, Explicit, Timeout(3600 * 1000)]
        public void Measure_Attempts_200Seeds()
        {
            var whys = new Dictionary<string, int>();
            FacilityMaps.RejectHook = (id, kind, why) => whys[why] = (whys.TryGetValue(why, out int n) ? n : 0) + 1;
            try
            {
                var cells = new Dictionary<string, List<int>>();
                var edges = new Dictionary<string, int>();
                int rooms = 0, fallback = 0, mids = 0, partitions = 0, exhausted = 0;
                for (uint seed = 1000; seed < 1200; seed++)
                {
                    FacilityMaps.ClearCache();
                    IReadOnlyList<FacilityRoom> all;
                    try { all = FacilityMaps.For(seed).Rooms; }
                    catch (InvalidOperationException e) { exhausted++; TestContext.WriteLine($"tohum {seed}: {e.Message}"); continue; }
                    foreach (var r in all)
                    {
                        rooms++;
                        if (r.KindFallback) fallback++;
                        string key = $"{r.Type} {r.MapKind} {r.Event ?? "-"}";
                        if (!cells.TryGetValue(key, out var list)) cells[key] = list = new List<int>();
                        list.Add(r.Attempt + 1);
                        if (r.Type == RoomType.Target) continue;
                        string edge = r.Forward.All(t => t.X == Baskin.N - 1) ? "dogu" : r.Forward.All(t => t.Y == 0) ? "kuzey" : "guney";
                        string ek = r.Type + " " + edge;
                        edges[ek] = (edges.TryGetValue(ek, out int en) ? en : 0) + 1;
                        if (r.Type != RoomType.Middle) continue;
                        mids++;
                        var sh = r.Shutter.Value;
                        if (r.DraftRows[sh.Y][sh.X] != '#') partitions++;
                    }
                }
                TestContext.WriteLine($"Tükenen tohum {exhausted}; yedek tür {fallback}/{rooms}; bölme kepengi {partitions}/{mids} A odası");
                TestContext.WriteLine("Geçit kenarı: " + string.Join(", ", edges.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key} {k.Value}")));
                TestContext.WriteLine("Ret: " + string.Join(", ", whys.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}")));
                foreach (var kv in cells.OrderBy(k => k.Key, StringComparer.Ordinal))
                    TestContext.WriteLine($"{kv.Key}: {kv.Value.Count} oda, ortalama deneme {kv.Value.Average():F1}, en çok {kv.Value.Max()}");
            }
            finally { FacilityMaps.RejectHook = null; FacilityMaps.ClearCache(); }
        }

        [Test]
        public void RegionHashes_AreDistinct()
        {
            CollectionAssert.AllItemsAreUnique(GameData.Regions.Select(r => FacilityMaps.Fnv1a32(r.Id)).ToList());
            Assert.AreEqual(12, GameData.Regions.Length);
        }
    }
}
