using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis baskını oda zinciri testlerinin ortak kurulumu (aşama 2): plan (<c>FacilityMaps.For(79)</c>), olaysız denge, kanadın
    /// baskın değerleri, yeni oyun ve baskın emri, makinesiz tahta, kapıya giriş, kaçışın başlatılması ve oda geçişi. Simülasyonla
    /// oynanan baskın için betikli bir sürücü (<see cref="PlayRaid"/>): yalnız komut kullanır, kayıttan aynen yeniden oynanır.
    /// </summary>
    internal static class FacilityKit
    {
        internal static FacilityPlan Plan79 => FacilityMaps.For(79);

        /// <summary>Olaysız denge (tesis tek baskın kipi; sahadaki sızmacı C# varsayılanıyla açık).</summary>
        internal static Balance Bal(bool events = true) =>
            new Balance { shelterEventsEnabled = false, liberationRegionEvents = events };

        /// <summary>Planın 36 kanadı (G, A, H), bölge ve kanat sırasıyla.</summary>
        internal static List<IReadOnlyList<FacilityRoom>> Wings =>
            Plan79.Rooms.GroupBy(r => r.SectorId).Select(g => (IReadOnlyList<FacilityRoom>)g.OrderBy(r => r.Index).ToList()).ToList();

        internal static IReadOnlyList<FacilityRoom> Wing(string sectorId) => Plan79.Wing(sectorId);

        /// <summary>FacilityRoomTests.Params kalıbı + Rooms (kanadın kimlikleri), Seed, MapSeed = 79, Tactics 1; kadro varsayılan üç
        /// uzman. Olaysızda Event boş.</summary>
        internal static RaidParams Params(IReadOnlyList<FacilityRoom> wing, int seed, int tier = 1, bool events = true)
        {
            var room = wing[0];
            var def = GameData.Region(room.Region);
            return new RaidParams
            {
                Seed = seed, Region = room.Region, SectorId = room.SectorId, MissionKind = room.Mission, Traits = new List<string> { def.Trait },
                // Aşama 12: tesis şartı OrderRaid gibi FacilityMaps.EventFor'dan (plan 79 iki alt bayrak açık)
                Signature = def.Enemy, Event = events ? FacilityMaps.EventFor(def, true, true) : null, Tier = tier, RaidNumber = tier, Tactics = 1, MapSeed = 79,
                Rooms = wing.Select(r => r.Id).ToList(),
            };
        }

        /// <summary>Raporlardan sıradaki odanın tahtası (<see cref="RaidChain.Open"/>, plan 79).</summary>
        internal static Baskin.BState Open(RaidParams p, IReadOnlyList<RoomReport> reps, Balance b = null) =>
            RaidChain.Open(b ?? Bal(), Plan79, p, reps);

        /// <summary>Yeni oyun (<see cref="ShelterEventKit.New"/>), kadro (FillSquad), <paramref name="before"/> (aşama 8: panoya eşya ve
        /// takma, kazanım), baskın emri (StartRaid).</summary>
        internal static Simulation Sim(uint seed = 79, Action<Balance> tune = null, Action<Simulation> before = null)
        {
            var b = Bal();
            tune?.Invoke(b);
            var sim = ShelterEventKit.New(b, seed);
            ShelterEventKit.FillSquad(sim);
            before?.Invoke(sim);
            ShelterEventKit.StartRaid(sim);
            return sim;
        }

        /// <summary>Aşama 8: bekleyen sunucu ödülünü komutla seçer (yoksa false; seçim reddedilirse Assert).</summary>
        internal static bool ChooseReward(Simulation sim, string choice = FacilityGear.ChoiceRecipe)
        {
            string region = sim.Facility?.PendingReward;
            if (string.IsNullOrEmpty(region)) return false;
            var r = sim.ChooseServerReward(region, choice);
            Assert.IsTrue(r.Ok, r.Msg);
            return true;
        }

        /// <summary>
        /// Aşama 8: süren tesis baskınını kesin kazanır (her odada makineler kalkar; hedef odasında kaçış başlar, ekip kapıdan geçer) ve
        /// sonucu yazar. Yalnız komut ve tahta kullanır; kayıttan aynen yeniden oynanır.
        /// </summary>
        internal static void WinRaid(Simulation sim)
        {
            for (int guard = 0; guard < 6 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                Calm(B);
                if (B.Room.Type == RoomType.Target && !B.Escape) StartEscape(B);
                ToGate(B);
                if (B.Phase == "player") Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
                var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                Assert.IsTrue(ok.Ok, ok.Msg);
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            var res = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(res.Ok, res.Msg);
            Assert.IsTrue(sim.S.RaidLog.Last().Success, "WinRaid kazanmalı");
        }

        /// <summary>
        /// Süren baskını oda zinciriyle bitirir (tek kip: ResolveRaid elle verilen sonucu yok sayar); kadronun kaderi
        /// <paramref name="fates"/>'te, kadro sırasıyla (verilmeyen 0):
        /// <list type="bullet">
        /// <item>0: ayakta döner (kapıdan çıkar).</item>
        /// <item>1: düşer ve tesiste kalır, ölür (tesiste "ekip ayakta döndüyse düşen kurtarılır" kuralı yok). Başarıda hedef odasında,
        /// başarısızlıkta geri çekilinen giriş odasında düşer.</item>
        /// <item>2: geride kalır (kaçışta süre doldu, çıkamadı; ölür). Yalnız başarılı baskında: hedef odasında öbürleri kapıda beklerken
        /// süre biter.</item>
        /// <item>3: düşer ve tesisten çıkarılır (ekipçe kurtarıldı, yatar). Yalnız başarılı baskında: giriş odasında düşer, ekip çıkarken
        /// temiz odadan alır.</item>
        /// </list>
        /// Başarı: her odada makineler ve kopya dolapları kalkar (geçilen odalar temiz kalır, dönüş yürüyerek olur); hedef odasında kaçış
        /// başlar (ikmalde taşıyıcı 0 kaderli ilk uzman, önce Teknisyen). Başarısızlık: giriş odasında geri çekilme. Yalnız komut ve tahta
        /// kullanır (OpenRaidRoom → tahta → AdvanceRaidRoom → ResolveRaid); kayıttan aynen yeniden oynanır.
        /// </summary>
        internal static void ResolveRaid(Simulation sim, bool success, params int[] fates) => ResolveRaid(sim, success, fates, null, null);

        /// <summary>
        /// <see cref="ResolveRaid(Simulation, bool, int[])"/> + <paramref name="hurt"/>: kadro sırasıyla, giriş odasının ilk ziyaretinde
        /// gerçek hasar yoluyla alınan hasar (önce plaka; öldürmemeli, düşürmek kaderle) ve <paramref name="act"/>: her odanın açılışında,
        /// makineler kalktıktan sonra çağrılır (test düzeni: ör. kırılan sivil sayısı, ayaktaki siviller).
        /// </summary>
        internal static void ResolveRaid(Simulation sim, bool success, int[] fates, int[] hurt, Action<Baskin.BState> act)
        {
            fates = fates ?? new int[0];
            int count = RaidChain.SquadCount(sim.S.Raid.Params);
            int Fate(int slot) => slot < fates.Length ? fates[slot] : 0;
            var slots = Enumerable.Range(0, count).ToList();
            Assert.IsTrue(success || slots.All(i => Fate(i) == 0 || Fate(i) == 1), "geri çekilmede yalnız 0 (döner) ve 1 (düşer, ölür) olur");
            Assert.IsTrue(!success || slots.Any(i => Fate(i) == 0), "başarılı baskında en az bir uzman ayakta dönmeli");
            for (int guard = 0; guard < 8 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                Calm(B);
                Unspawn(B);
                if (hurt != null && B.Room.Type == RoomType.Entry && !B.Escape)
                    foreach (var u in Standing(B).Where(u => u.Slot < hurt.Length && hurt[u.Slot] > 0).ToList())
                    {
                        Baskin.TestHurt(B, u, hurt[u.Slot]);
                        Assert.IsFalse(u.Dead, "hasar öldürmemeli (düşürmek için kader): kadro " + u.Slot);
                    }
                act?.Invoke(B);
                if (!success)
                {
                    foreach (int i in slots.Where(i => Fate(i) == 1)) DownSlot(B, i);
                    if (B.Phase == "player") Assert.IsTrue(Baskin.Retreat(B), "geri çekilme: " + B.Room.Id);
                }
                else
                {
                    if (B.Room.Type == RoomType.Entry && !B.Escape)
                        foreach (int i in slots.Where(i => Fate(i) == 3)) DownSlot(B, i);
                    bool target = B.Room.Type == RoomType.Target && !B.Escape;
                    if (target)
                    {
                        foreach (int i in slots.Where(i => Fate(i) == 1)) DownSlot(B, i);
                        foreach (var t in Baskin.TargetsLeft(B)) Baskin.CellAt(B, t.X, t.Y).O = null;
                        if (Baskin.IsLoot(B))
                        {
                            var carriers = Standing(B).Where(u => Fate(u.Slot) == 0).ToList();
                            B.Carrier = (carriers.FirstOrDefault(u => u.Type == "tek") ?? carriers.First()).Id;
                        }
                        B.Escape = true;
                    }
                    // Geride kalacak uzman kapı karesindeyse kapı dışındaki ilk boş kareye çekilir (öbürlerine kare kalsın; test düzeni)
                    if (target)
                        foreach (var u in Standing(B).Where(u => Fate(u.Slot) == 2 && Baskin.IsGateTile(B, u.X, u.Y)))
                        {
                            var spot = Enumerable.Range(0, Baskin.N * Baskin.N).Select(k => new Baskin.Pt(k % Baskin.N, k / Baskin.N))
                                .First(t => FreeAt(B, t.X, t.Y) && !Baskin.IsGateTile(B, t.X, t.Y));
                            Place(u, spot);
                        }
                    foreach (var u in Standing(B).Where(u => !target || Fate(u.Slot) != 2))
                        if (B.Phase == "player") ToGate(B, u);
                    // Geride kalan (2): öbürleri kapıda beklerken süre biter; çıkamayan düşmüş sayılır, kapıdakiler geçer
                    for (int t = 0; t < 40 && B.Phase == "player" && Standing(B).Any(); t++)
                    {
                        Assert.IsTrue(Baskin.EndTurn(B), "EndTurn t" + B.Turn);
                        Calm(B);
                    }
                    if (B.Phase == "player") Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
                }
                var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                Assert.IsTrue(ok.Ok, ok.Msg);
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            var res = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(res.Ok, res.Msg);
            Assert.AreEqual(success, sim.S.RaidLog.Last().Success, "ResolveRaid sonucu");
        }

        /// <summary>Kadro sırasıyla uzmanı düşürür (tahtada ayaktaysa; <see cref="Down"/>).</summary>
        static void DownSlot(Baskin.BState B, int slot)
        {
            var u = B.Units.FirstOrDefault(v => Baskin.IsSpecialist(v) && v.Slot == slot && Baskin.OnBoard(v));
            if (u != null) Down(B, u);
        }

        /// <summary>Kopya dolaplarını kaldırır: makinesiz oda temiz sayılır (<see cref="RaidChain.IsClean"/>), dönüşte oynanmaz.</summary>
        static void Unspawn(Baskin.BState B)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (Baskin.CellAt(B, x, y).O?.K == "spawner") Baskin.CellAt(B, x, y).O = null;
        }

        /// <summary>
        /// Aşama 8: bulunulan bölgeyi komutlarla tamamlar (bölge bittiyse sınırdan ilk bölge seçilir): kadro, baskın emri, kesin kazanılan
        /// baskın, vardiya kapanışı. Bölgeyi tamamlayan baskından sonra kapanış yapılmaz (bekleyen ödül açıkta kalır). Bölge kimliği döner.
        /// </summary>
        internal static string CompleteRegion(Simulation sim)
        {
            var lib = sim.S.Liberation;
            if (sim.LiberationRegion(lib.CurrentRegion).Completed)
                Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            string region = lib.CurrentRegion;
            for (int guard = 0; guard < 6; guard++)
            {
                ShelterEventKit.FillSquad(sim);
                var sector = sim.NextLiberationSector(region);
                var started = sim.StartLiberationRaid(sector.Id);
                Assert.IsTrue(started.Ok, started.Msg);
                WinRaid(sim);
                if (sim.LiberationRegion(region).Completed) return region;
                ShelterEventKit.Close(sim);
            }
            Assert.Fail("bölge tamamlanmadı: " + region);
            return region;
        }

        /// <summary>
        /// Aşama 12 (§6.5): yeni oyunu (tesis açık denge; <paramref name="tune"/>) yalnız komutlarla <paramref name="regionId"/>'ye getirir:
        /// bulunulan bölge sırayla tamamlanır (<see cref="CompleteRegion"/>), bekleyen sunucu ödülü seçilir (hep tarif), vardiya kapanır,
        /// sınırdan hedef seçilir; hedef sınırda değilse hedefe en az bağlantıyla (<see cref="Simulation.Links"/>) yaklaşan sınır bölgesi.
        /// Dönen simülasyonda hedef seçili, baskın yok; bütün adımlar kayıtta.
        /// </summary>
        internal static Simulation SimAt(uint seed, string regionId, Action<Balance> tune = null)
        {
            var b = Bal();
            tune?.Invoke(b);
            var sim = ShelterEventKit.New(b, seed);
            for (int guard = 0; guard < 2 * GameData.Regions.Length; guard++)
            {
                var lib = sim.S.Liberation;
                if (lib.CurrentRegion == regionId && !sim.LiberationRegion(regionId).Completed) return sim;
                if (sim.LiberationRegion(lib.CurrentRegion).Completed)
                {
                    var frontier = sim.LiberationFrontier.ToList();
                    Assert.IsNotEmpty(frontier, "sınır boş: " + regionId);
                    string pick = frontier.Contains(regionId) ? regionId : frontier.OrderBy(r => Hops(sim, r, regionId)).First();
                    var sel = sim.SelectLiberationRegion(pick);
                    Assert.IsTrue(sel.Ok, sel.Msg);
                    continue;
                }
                CompleteRegion(sim);
                ChooseReward(sim);
                ShelterEventKit.Close(sim);
            }
            Assert.Fail("bölgeye varılamadı: " + regionId);
            return sim;
        }

        /// <summary>İki bölge arasındaki en az bağlantı sayısı (<see cref="Simulation.Links"/>; bağlantı yoksa int.MaxValue).</summary>
        static int Hops(Simulation sim, string from, string to)
        {
            var dist = new Dictionary<string, int> { [from] = 0 };
            var queue = new Queue<string>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                if (at == to) return dist[at];
                foreach (var l in sim.Links)
                {
                    string next = l.A == at ? l.B : l.B == at ? l.A : null;
                    if (next == null || dist.ContainsKey(next)) continue;
                    dist[next] = dist[at] + 1;
                    queue.Enqueue(next);
                }
            }
            return int.MaxValue;
        }

        /// <summary>
        /// Makineleri devre dışı bırakır (Dead), uyanma işaretlerini siler ve uyanmayı kapatır (SpawnPerTurn = 0): testin kurduğu
        /// kareler bozulmasın. <paramref name="keep"/> makine (önce dronlar, birim sırasıyla) ayakta kalır.
        /// </summary>
        internal static void Calm(Baskin.BState B, int keep = 0)
        {
            var foes = B.Units.Where(u => u.Side == 'e' && !u.Dead).Select((u, i) => (u, i))
                .OrderBy(x => x.u.Type == "dron" ? 0 : 1).ThenBy(x => x.i).Select(x => x.u).ToList();
            foreach (var e in foes.Skip(keep)) { e.Dead = true; e.Intent = null; }
            B.Spawns.Clear();
            B.SpawnPerTurn = 0;
        }

        /// <summary>Kapı karelerinde duran makineleri devre dışı bırakır (öbür makineler kalır).</summary>
        internal static void ClearGate(Baskin.BState B)
        {
            foreach (var e in B.Units.Where(u => u.Side == 'e' && Baskin.OnBoard(u) && Baskin.IsGateTile(B, u.X, u.Y)))
            {
                e.Dead = true;
                e.Intent = null;
            }
        }

        /// <summary>Tahtadaki ayaktaki uzmanlar, kadro sırasıyla (aşama 8: köpek, aygıt ve tutsak değil).</summary>
        internal static List<Baskin.Unit> Standing(Baskin.BState B) =>
            B.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).OrderBy(u => u.Slot).ToList();

        internal static Baskin.Unit BySlot(Baskin.BState B, int slot) => B.Units.FirstOrDefault(u => u.Side == 'p' && u.Slot == slot);

        /// <summary>Uzmanı birimsiz bir kapı karesine koyar ve EnterGate verir (Assert).</summary>
        internal static void ToGate(Baskin.BState B, Baskin.Unit u)
        {
            var free = Baskin.GateTiles(B).Where(g => { var o = Baskin.UnitAt(B, g.X, g.Y); return o == null || o == u; }).ToList();
            Assert.IsTrue(free.Count > 0, "boş kapı karesi yok: " + B.Room?.Id);
            u.X = free[0].X;
            u.Y = free[0].Y;
            Assert.IsTrue(Baskin.EnterGate(B, u.Id), "EnterGate: " + B.Room?.Id);
        }

        /// <summary>Ayaktaki her uzmanı birimsiz bir kapı karesine koyar ve EnterGate verir (Assert).</summary>
        internal static void ToGate(Baskin.BState B)
        {
            foreach (var u in Standing(B))
                if (B.Phase == "player") ToGate(B, u);
        }

        /// <summary>Kaçışı başlatır: sunucuda hedef dolap kaldırılır, ikmalde kasa kaldırılır ve Carrier = Teknisyen (yoksa ayaktaki
        /// ilk uzman); Escape = true.</summary>
        internal static void StartEscape(Baskin.BState B)
        {
            foreach (var t in Baskin.TargetsLeft(B)) Baskin.CellAt(B, t.X, t.Y).O = null;
            if (Baskin.IsLoot(B))
            {
                var team = Standing(B);
                B.Carrier = (team.FirstOrDefault(u => u.Type == "tek") ?? team.First()).Id;
            }
            B.Escape = true;
        }

        /// <summary>Biten tahtanın raporu (<see cref="RaidChain.Report"/>); listeye eklenir.</summary>
        internal static RoomReport File(RaidParams p, List<RoomReport> reps, Baskin.BState B)
        {
            var r = RaidChain.Report(p, reps, B);
            reps.Add(r);
            return r;
        }

        /// <summary>ToGate + Pass + Report; raporu listeye ekler ve döndürür (saf zincir testleri).</summary>
        internal static RoomReport PassRoom(RaidParams p, List<RoomReport> reps, Baskin.BState B)
        {
            ToGate(B);
            Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room?.Id);
            return File(p, reps, B);
        }

        /// <summary>Elle rapor: oda, yön, bitiş ve donanım; kadro ayakta, canı 3.</summary>
        internal static RoomReport Rep(int room, bool ret = false, string end = "pass", string device = "", int squad = 3)
        {
            var r = new RoomReport { Room = room, Return = ret, End = end, Device = device };
            for (int i = 0; i < squad; i++) r.Team.Add(new RoomMember { State = "a", Hp = 3 });
            return r;
        }

        /// <summary>
        /// Betikli oda oynayışı (kayıt ve devam testleri): <paramref name="idle"/> tur hamlesiz geçer (makineler oynar); sonra hedef
        /// odasında kaçış başlar ve makineler kalkar, dönüş odasında makineler kalkar, giriş ve ara odada yalnız kapı karesindekiler;
        /// ayaktakiler kapıya girer ve GEÇ verilir (tesisin girişinde tahliye).
        /// </summary>
        internal static void PlayRoom(Baskin.BState B, int idle = 1)
        {
            for (int i = 0; i < idle && B.Phase == "player"; i++) Assert.IsTrue(Baskin.EndTurn(B), "EndTurn");
            if (B.Phase != "player") return;
            if (B.Room.Type == RoomType.Target && !B.Escape) StartEscape(B);
            if (B.Escape) Calm(B);
            else ClearGate(B);
            ToGate(B);
            if (B.Phase == "player") Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
        }

        /// <summary>Süren tesis baskınını oda oda oynar (<see cref="PlayRoom"/>), raporları dosyalar ve sonucu yazar. Raporları döndürür.</summary>
        internal static List<RoomReport> PlayRaid(Simulation sim, int idle = 1)
        {
            for (int guard = 0; guard < 6 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                PlayRoom(B, idle);
                var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                Assert.IsTrue(ok.Ok, ok.Msg);
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "oda zinciri bitmedi");
            var reps = sim.S.Raid.Reports.ToList();
            var res = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(res.Ok, res.Msg);
            return reps;
        }

        /// <summary>Yalnız komutla baskın emri: bölge bittiyse sınırdan ilk bölge, sıradaki sektör; emir verilemezse false.</summary>
        internal static bool TryStartRaid(Simulation sim)
        {
            if (sim.S.Raid != null || sim.S.Ending != null) return false;
            // Aşama 8: bekleyen sunucu ödülü baskın emrini kilitler; kayıt testleri "hep tarif" seçer
            ChooseReward(sim);
            var lib = sim.S.Liberation;
            if (sim.LiberationRegion(lib.CurrentRegion).Completed)
            {
                var f = sim.LiberationFrontier.FirstOrDefault();
                if (f == null || !sim.SelectLiberationRegion(f).Ok) return false;
            }
            var sector = sim.NextLiberationSector(lib.CurrentRegion);
            if (sector == null || sim.LiberationRaidCheck(sector.Id) != null) return false;
            return sim.StartLiberationRaid(sector.Id).Ok;
        }

        /// <summary>Simülasyonun karşılaştırma özeti: gün, fon, güven, alarm, RNG, baskın günlüğü (başarı, oda raporları), kişiler ve bina
        /// planı (bilinen odalar, dinleme puanı; aşama 5).</summary>
        internal static string State(Simulation sim) =>
            string.Join("\n", new[]
            {
                $"d{sim.S.Day} f{sim.S.Fon:R} t{sim.S.Trust} a{sim.S.Alarm:R} rng{sim.S.Rng.State} raids{sim.S.RaidLog.Count}",
                string.Join("\n", sim.S.RaidLog.Select(l => (l.Success ? "W " : "L ") + l.SectorId + " " + string.Join(" / ", l.Rooms))),
                string.Join(";", sim.Board.People.Select(p => $"{p.Id}:{(p.Alive ? 1 : 0)}:{p.Hp}:{p.Down}")),
                $"plan {string.Join(",", sim.S.Liberation.Facility.Known)} i{sim.S.Liberation.Facility.Intel}",
                // Aşama 8: açılan tarifler, kazanımlar, bekleyen ödül, ödülü verilen bölgeler ve pano ekipmanı
                $"odul {string.Join(",", sim.S.Liberation.Facility.Recipes)} | {string.Join(",", sim.S.Liberation.Facility.Perks)} | {sim.S.Liberation.Facility.PendingReward}"
                    + $" | {string.Join(",", sim.S.Liberation.Regions.Where(x => x.Rewarded).Select(x => x.Id))}",
                "esya " + string.Join(";", sim.Board.Equipment.Select(e => $"{e.Id}:{e.Kind}:{e.OwnerId ?? "-"}:{e.Charges}:{e.Rest}")),
                // Aşama 10: önlem defteri (sayaçlar, kademeler, öğrenme günlüğü) ve iş emirleri (cevap sayfası, taslak iptali)
                CounterLine(sim.S.Liberation.Facility),
                "is " + string.Join(";", sim.Board.Orders.Select(o => $"{o.Id}:{o.RecipeId}:{o.Remaining}:{(o.Started ? 1 : 0)}:"
                    + string.Join(",", o.Reserves.Select(m => m.Kind + m.Quantity)))),
                // Aşama 11: kalıcı tahta (izler, onarım günlüğü), kuşatma günlüğü, karşı saldırı sayaçları, alınmış sektörler ve günlüğün kipi
                MarkLine(sim.S.Liberation.Facility),
                SiegeLine(sim),
            });

        /// <summary>Aşama 11: izlerin ve onarım günlüğünün karşılaştırma satırı (Camp8 State ile aynı biçim).</summary>
        internal static string MarkLine(FacilityState f) =>
            "iz " + string.Join(";", f.Marks.Select(MarkKey)) + " | " + string.Join(";", f.Repairs.Select(MarkKey));

        internal static string MarkKey(FacilityMark m) => $"{m.Room}:{m.Kind}:{m.X}:{m.Y}:{m.Hp}:{m.Max}:{m.Day}";

        /// <summary>Aşama 11: kuşatma günlüğü, karşı saldırı sayaçları, alınmış sektörler ve baskın günlüğünün kipleri.</summary>
        internal static string SiegeLine(Simulation sim) =>
            "kusatma " + string.Join(";", sim.S.Liberation.Facility.Sieges.Select(e => $"{e.Sector}:{e.Region}:{e.Day}:{e.Due}:{e.Outcome}:{e.Closed}:{e.Abandoned}"))
            + $" | c{sim.S.Counterattacks}:{sim.S.LastCounterattack ?? "-"}:{sim.S.LastCounterattackDay} | "
            + string.Join(",", sim.S.Liberation.Regions.SelectMany(r => r.Sectors).Where(x => x.Captured).Select(x => x.Id + "@" + x.CapturedDay))
            + " | " + string.Join("", sim.S.RaidLog.Select(l => l.Mode == "defense" ? "D" : "R"));

        // ---------- Aşama 11: kuşatma ve savunma ----------
        /// <summary>
        /// Kuşatmayı kapanışta kurar (test kurulumu, durum doğrudan: kayıttan yeniden oynanmaz): alarm eşiğin bir vardiya altına getirilir
        /// ve vardiya kapanır. Bekleyen kuşatma döner (Assert).
        /// </summary>
        internal static FacilitySiegeEntry ForceSiege(Simulation sim)
        {
            sim.S.Alarm = Math.Max(0, sim.B.liberationAlarmThreshold - sim.AlarmPerShift);
            ShelterEventKit.Close(sim);
            var e = sim.PendingSiege;
            Assert.IsNotNull(e, "kuşatma kurulmadı");
            return e;
        }

        /// <summary>Sıradaki sektörü komutlarla kesin alır (kadro, emir, <see cref="WinRaid"/>) ve vardiyayı kapatır. Sektör döner.</summary>
        internal static LiberationSectorState CaptureNext(Simulation sim, bool close = true)
        {
            ShelterEventKit.FillSquad(sim);
            var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            var started = sim.StartLiberationRaid(sector.Id);
            Assert.IsTrue(started.Ok, started.Msg);
            WinRaid(sim);
            Assert.IsTrue(sector.Captured);
            if (close) ShelterEventKit.Close(sim);
            return sector;
        }

        /// <summary>Kadroyu doldurur ve savunma emrini verir (Assert).</summary>
        internal static void Defend(Simulation sim)
        {
            ShelterEventKit.FillSquad(sim);
            var r = sim.DefendSector();
            Assert.IsTrue(r.Ok, r.Msg);
            Assert.IsTrue(RaidChain.IsDefense(sim.S.Raid.Params));
        }

        /// <summary>
        /// Süren savunmayı yalnız komut ve tahtayla oynar: ilk <paramref name="abandon"/> oda içeri çekilerek terk edilir (kapı + GEÇ);
        /// sonraki odada <paramref name="hold"/> ise süre dolana kadar tutulur (her tur sonu makineler kalkar), değilse geri çekilinir
        /// (kayıp). <paramref name="act"/> her odanın açılışında çağrılır (ör. bir uzmanı düşürmek). Sonucu yazar; raporları döndürür.
        /// Kayıttan aynen yeniden oynanır.
        /// </summary>
        internal static List<RoomReport> PlayDefense(Simulation sim, int abandon = 0, bool hold = true, Action<Baskin.BState> act = null)
        {
            for (int guard = 0; guard < 4 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                Assert.IsTrue(Baskin.IsDefense(B), "savunma tahtası");
                Calm(B);
                act?.Invoke(B);
                if (B.Room.Index <= abandon)
                {
                    ToGate(B);
                    Assert.IsTrue(Baskin.Pass(B), "GEÇ: " + B.Room.Id);
                }
                else if (!hold) Assert.IsTrue(Baskin.Retreat(B), "geri çekilme: " + B.Room.Id);
                else
                    for (int t = 0; t < 40 && B.Phase == "player"; t++)
                    {
                        Assert.IsTrue(Baskin.EndTurn(B), "EndTurn t" + B.Turn);
                        Calm(B);
                    }
                var ok = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                Assert.IsTrue(ok.Ok, ok.Msg);
            }
            Assert.IsTrue(sim.RaidRoomsClosed, "savunmanın oda zinciri bitmedi");
            var reps = sim.S.Raid.Reports.ToList();
            var res = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(res.Ok, res.Msg);
            return reps;
        }

        /// <summary>Aşama 10: önlem defterinin karşılaştırma satırı (Camp8 State ile aynı biçim).</summary>
        internal static string CounterLine(FacilityState f) =>
            "onlem " + string.Join(",", f.Tally) + " | " + string.Join(",", f.Counter) + " | "
            + string.Join(",", f.Learned.Select(e => e.Day + ":" + e.Region + ":" + e.Class + ":" + e.Level));

        // ---------- Aşama 6: tam döküm, geri alma, gövde ve yük ----------
        /// <summary>
        /// Durumun yansımalı tam dökümü: BState'in bütün public örnek alanları (Hist hariç), iç nesneleriyle (Units, Cells, Obj, Intent,
        /// Ev, Fx, Log, MoveRules) özyinelemeli. Paylaşılan salt okunur nesneler (RaidParams, FacilityRoom) kimlikleriyle yazılır;
        /// kümeler sıralanır. Geri alma testleri bununla CopyInto'ya unutulan alanı yakalar.
        /// </summary>
        internal static string FullDump(Baskin.BState B)
        {
            var sb = new System.Text.StringBuilder();
            Dump(sb, B, 0, skipHist: true);
            return sb.ToString();
        }

        /// <summary>Herhangi bir nesnenin aynı kuralla yansımalı dökümü (önizleme sonucu karşılaştırmaları).</summary>
        internal static string DumpAny(object o)
        {
            var sb = new System.Text.StringBuilder();
            Dump(sb, o, 0);
            return sb.ToString();
        }

        static void Dump(System.Text.StringBuilder sb, object o, int depth, bool skipHist = false)
        {
            if (depth > 12) { sb.Append("…"); return; }
            if (o == null) { sb.Append("null"); return; }
            var t = o.GetType();
            if (t.IsPrimitive || o is string || t.IsEnum || o is decimal) { sb.Append(Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture)); return; }
            if (o is RaidParams || o is FacilityRoom) { sb.Append(t.Name).Append('#').Append(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o)); return; }
            if (o is Array arr)
            {
                sb.Append('[');
                foreach (var e in arr) { Dump(sb, e, depth + 1); sb.Append(';'); }
                sb.Append(']');
                return;
            }
            if (o is System.Collections.IDictionary dict)
            {
                var parts = new List<string>();
                foreach (System.Collections.DictionaryEntry kv in dict)
                {
                    var k = new System.Text.StringBuilder(); Dump(k, kv.Key, depth + 1); k.Append("=>"); Dump(k, kv.Value, depth + 1);
                    parts.Add(k.ToString());
                }
                parts.Sort(StringComparer.Ordinal);
                sb.Append('{').Append(string.Join(";", parts)).Append('}');
                return;
            }
            if (o is System.Collections.IEnumerable seq)
            {
                var parts = new List<string>();
                foreach (var e in seq) { var k = new System.Text.StringBuilder(); Dump(k, e, depth + 1); parts.Add(k.ToString()); }
                // Kümelerin sırası kopyada değişebilir
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>)) parts.Sort(StringComparer.Ordinal);
                sb.Append('[').Append(string.Join(";", parts)).Append(']');
                return;
            }
            sb.Append(t.Name).Append('{');
            foreach (var f in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).OrderBy(f => f.Name, StringComparer.Ordinal))
            {
                if (skipHist && f.Name == "Hist") continue;
                sb.Append(f.Name).Append('=');
                Dump(sb, f.GetValue(o), depth + 1);
                sb.Append(',');
            }
            sb.Append('}');
        }

        /// <summary>Hamle (<paramref name="act"/> true dönmeli) ve geri alma: tam döküm ve imza hamleden önceki gibi; geçmiş bir öğe kısalır.</summary>
        internal static void AssertUndoRestores(Baskin.BState B, Func<Baskin.BState, bool> act, string what = "")
        {
            string dump = FullDump(B), sig = Baskin.Signature(B);
            int hist = B.Hist.Count;
            Assert.IsTrue(act(B), "hamle: " + what);
            Assert.AreNotEqual(dump, FullDump(B), "hamle durumu değiştirmedi: " + what);
            Assert.IsTrue(Baskin.Undo(B), "geri alma: " + what);
            Assert.AreEqual(sig, Baskin.Signature(B), "imza: " + what);
            Assert.AreEqual(dump, FullDump(B), "tam döküm: " + what);
            Assert.AreEqual(hist, B.Hist.Count);
        }

        /// <summary>Uzmanı gerçek hasar yoluyla düşürür (buz, plaka, can; taşıyıcıysa tesiste yük yere düşer).</summary>
        internal static void Down(Baskin.BState B, Baskin.Unit u)
        {
            Baskin.TestHurt(B, u, 99);
            Assert.IsTrue(u.Dead);
        }

        /// <summary>Birimi kareye koyar (geri alma görüntüsü yok; test kurulumu).</summary>
        internal static void Place(Baskin.Unit u, Baskin.Pt at) { u.X = at.X; u.Y = at.Y; }

        /// <summary>Kare geçilebilir (duvar ve nesne yok) ve üstünde ayakta birim yok.</summary>
        internal static bool FreeAt(Baskin.BState B, int x, int y) =>
            x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N && Baskin.CellAt(B, x, y).T != "wall" && Baskin.CellAt(B, x, y).O == null && Baskin.UnitAt(B, x, y) == null;

        /// <summary>
        /// Düz boş hat: <paramref name="len"/> + 1 karelik (başlangıç dahil) geçilebilir, birimsiz, kapı ve menfez karesi olmayan hat
        /// (y, x, yön sırasıyla ilk). <paramref name="ok"/> başlangıcı süzer. Yoksa null.
        /// </summary>
        internal static (Baskin.Pt At, int[] D)? FindLine(Baskin.BState B, int len, Func<Baskin.Pt, bool> ok = null)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    foreach (var d in Baskin.Dirs)
                    {
                        bool good = true;
                        for (int k = 0; k <= len && good; k++)
                        {
                            int cx = x + d[0] * k, cy = y + d[1] * k;
                            good = FreeAt(B, cx, cy) && Baskin.CellAt(B, cx, cy).Door == 0 && Baskin.CellAt(B, cx, cy).Vent == 0 && !Baskin.IsGateTile(B, cx, cy);
                        }
                        if (good && (ok == null || ok(new Baskin.Pt(x, y)))) return (new Baskin.Pt(x, y), d);
                    }
            return null;
        }

        internal static Baskin.Pt Step(Baskin.Pt at, int[] d, int k) => new Baskin.Pt(at.X + d[0] * k, at.Y + d[1] * k);

        // ---------- Aşama 8: ekipman ve aygıt ----------
        /// <summary>Uzmana ekipman ve kalan hak yazar (test kurulumu; emirdeki <see cref="SquadSlot.Gear"/>'ın tahtadaki karşılığı).</summary>
        internal static void Gear(Baskin.Unit u, string kind, int left) { u.Gear = kind; u.GearLeft = left; }

        /// <summary><see cref="Baskin.Dirs"/> dizini.</summary>
        internal static int DirOf(int[] d) => Array.FindIndex(Baskin.Dirs, v => v[0] == d[0] && v[1] == d[1]);

        /// <summary>Kareye aygıt ekler (birim sonuna; can 2, kuran kadro sırası 0; geri alma görüntüsü yok).</summary>
        internal static Baskin.Unit AddGadget(Baskin.BState B, string type, Baskin.Pt at, int[] d, int turns = 3)
        {
            var g = new Baskin.Unit
            {
                Id = B.NextId++, Type = type, Side = 'p', Class = Baskin.ClassGadget, X = at.X, Y = at.Y, Hp = 2, Max = 2,
                Dir = DirOf(d), Turns = turns, Owner = 0,
            };
            B.Units.Add(g);
            return g;
        }

        /// <summary>Kareye köpek ekler (birim sonuna; sahibinin kadro sırası, can ve yürüyüş tahtanın hamle değerlerinden; geri alma
        /// görüntüsü yok).</summary>
        internal static Baskin.Unit AddDog(Baskin.BState B, int owner, Baskin.Pt at)
        {
            int hp = B.Moves?.DogHp ?? 2;
            var d = new Baskin.Unit
            {
                Id = B.NextId++, Type = "kopek", Side = 'p', Class = Baskin.ClassDog, X = at.X, Y = at.Y, Hp = hp, Max = hp,
                MoveRange = B.Moves?.DogMove ?? 5, Owner = owner,
            };
            B.Units.Add(d);
            return d;
        }

        /// <summary>Kareye makine ekler (birim sonuna, niyetsiz; geri alma görüntüsü yok).</summary>
        internal static Baskin.Unit AddMachine(Baskin.BState B, string type, Baskin.Pt at, int hp = 3)
        {
            var e = new Baskin.Unit { Id = B.NextId++, Type = type, Side = 'e', X = at.X, Y = at.Y, Hp = hp, Max = hp };
            B.Units.Add(e);
            return e;
        }

        /// <summary>Makineyi tutsak yapar (kelepçenin sonucu, oynayabilir hâlde: Moved/Acted false; geri alma görüntüsü yok).</summary>
        internal static Baskin.Unit Captive(Baskin.Unit e, int owner = 0, int turns = 3)
        {
            e.Side = 'p'; e.Class = Baskin.ClassCaptive; e.Owner = owner; e.Turns = turns; e.Intent = null; e.Order = 0;
            e.Moved = e.Acted = false;
            return e;
        }

        /// <summary>Tahtayı düz zemine çevirir (duvar, nesne, kepenk, sandık nişi, menfez ve sis yok; kapı karelerinin plan listeleri
        /// kalır). Makineler kalkar (<see cref="Calm"/>), uzmanlar <paramref name="spots"/>'a (verilmeyen uzman tahtadan kalkar: Out).</summary>
        internal static void Flat(Baskin.BState B, params Baskin.Pt[] spots)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = Baskin.CellAt(B, x, y);
                    c.T = "floor"; c.O = null; c.Shutter = false; c.Crate = false; c.Vent = 0; c.Smoke = false;
                }
            Calm(B);
            var team = B.Units.Where(Baskin.IsSpecialist).OrderBy(u => u.Slot).ToList();
            for (int i = 0; i < team.Count; i++)
            {
                if (i < spots.Length) Place(team[i], spots[i]);
                else team[i].Out = true;
            }
            B.MaxTurns = 30;
        }

        /// <summary>Önizleme uygulamayla aynı (efektler, yer değişimleri) ve asıl durumu değiştirmez; sonra eylem uygulanır (Assert).</summary>
        internal static void ActPreviewed(Baskin.BState B, Baskin.Unit u, string kind, Baskin.Pt at)
        {
            string dump = FullDump(B);
            var pv = Baskin.PreviewAction(B, u.Id, kind, at.X, at.Y);
            Assert.IsNotNull(pv, "önizleme: " + kind);
            Assert.AreEqual(dump, FullDump(B), "önizleme durumu değiştirdi: " + kind);
            var before = B.Units.ToDictionary(v => v.Id, v => (v.X, v.Y));
            Assert.IsTrue(Baskin.DoAction(B, u.Id, kind, at.X, at.Y), "eylem: " + kind);
            CollectionAssert.AreEqual(pv.Fx.Select(f => f.K + f.X + f.Y + f.Unit + f.Obj), B.FxList.Select(f => f.K + f.X + f.Y + f.Unit + f.Obj), "önizleme efektleri: " + kind);
            var moved = B.Units.Where(v => before.ContainsKey(v.Id) && before[v.Id] != (v.X, v.Y)).Select(v => v.Id).OrderBy(i => i);
            CollectionAssert.AreEqual(moved, pv.Moves.Select(m => m.Unit).OrderBy(i => i), "önizleme yer değişimleri: " + kind);
        }

        /// <summary>Birimin tahtadaki güncel nesnesi (geri alma Units'i kopyalarla değiştirir: eski başvuru eylemin sonucunu taşır).</summary>
        internal static Baskin.Unit Fresh(Baskin.BState B, Baskin.Unit u) => B.Units.First(v => v.Id == u.Id);

        internal static bool Has(List<Baskin.Pt> list, Baskin.Pt p) => list.Any(t => t.X == p.X && t.Y == p.Y);

        /// <summary>Emirdeki kadroyu açık listeye çevirir ve <paramref name="slot"/>'a ekipman yazar (köpekte hak −1).</summary>
        internal static RaidParams WithGear(RaidParams p, int slot, string gear)
        {
            p.Squad = Baskin.SquadOf(p).ToList();
            p.Squad[slot].Gear = gear;
            p.Squad[slot].GearCharges = gear == FacilityGear.Dog ? -1 : 1;
            return p;
        }

        /// <summary>Kareden <paramref name="dir"/> yönündeki ilk boş, geçilebilir komşu (kapı karesi değil); yoksa null.</summary>
        internal static Baskin.Pt? FreeNeighbor(Baskin.BState B, Baskin.Pt at, bool gateToo = false)
        {
            foreach (var d in Baskin.Dirs)
            {
                int x = at.X + d[0], y = at.Y + d[1];
                if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) continue;
                var c = Baskin.CellAt(B, x, y);
                if (c.T == "wall" || c.O != null || Baskin.UnitAt(B, x, y) != null) continue;
                if (!gateToo && (Baskin.IsGateTile(B, x, y) || B.Room.West.Any(w => w.X == x && w.Y == y))) continue;
                return new Baskin.Pt(x, y);
            }
            return null;
        }
    }
}
