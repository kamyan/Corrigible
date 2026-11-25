using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Bölge olayları (Docs/BOLGE-OLAYLARI.md, Core/Baskin.Events.cs): her bölgenin kendi olayı (her tesis odasında yer bulması FacilityRoomTests'te),
    /// RNG'nin aynı kalması, her olayın kuralı, simetri (kural tarafa bakmaz), hasar tavanı, nesne muafiyeti ve önizleme.
    /// </summary>
    public class RaidEventTests
    {
        /// <summary>Boş bir tahta: her yer zemin, makineler devre dışı; ekip köşelerde (RegionIdentityTests ile aynı).</summary>
        static Baskin.BState Empty(string ev)
        {
            var B = Baskin.Create(new RaidParams { Seed = 11, MissionKind = "server" });
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) B.Cells[y, x] = new Baskin.Cell { T = "floor" };
            B.Cells[0, 7].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
            foreach (var u in B.Units.Where(u => u.Side == 'e')) u.Dead = true;
            var team = B.Units.Where(u => u.Side == 'p').ToArray();
            team[0].X = 0; team[0].Y = 0; team[1].X = 0; team[1].Y = 7; team[2].X = 7; team[2].Y = 7;
            B.Spawns.Clear();
            B.Ev = new Baskin.EvState { Id = ev };
            return B;
        }

        static Baskin.Unit Machine(Baskin.BState B, string type, int x, int y, string kind = null, int dir = 0, int hp = 0)
        {
            var T = Baskin.UnitTypes[type];
            var u = new Baskin.Unit { Id = B.NextId++, Type = type, Side = 'e', X = x, Y = y, Hp = hp > 0 ? hp : T.Hp, Max = hp > 0 ? hp : T.Hp, Armor = T.Armor, Order = 1,
                Intent = kind == null ? null : new Baskin.Intent { Kind = kind, Dir = dir } };
            B.Units.Add(u);
            return u;
        }

        static Baskin.Unit Team(Baskin.BState B, string type, int x, int y)
        {
            var u = B.Units.First(v => v.Type == type);
            u.X = x; u.Y = y;
            return u;
        }

        static Baskin.Pt P(int x, int y) => new Baskin.Pt(x, y);

        /// <summary>Makinelerin planı (yer değiştirme) testin kurduğu kareleri bozmasın: niyetsiz makineleri yerine koyar.</summary>
        static void Hold(Baskin.BState B, params (Baskin.Unit U, int X, int Y)[] at)
        {
            foreach (var (u, x, y) in at) { u.X = x; u.Y = y; u.Intent = null; }
        }

        // ---------- Kimlik ve kurulum ----------
        [Test]
        public void EveryRegion_HasOwnBoardEvent()
        {
            var events = GameData.Regions.Select(r => r.Event).ToList();
            CollectionAssert.AllItemsAreUnique(events, "her bölgenin olayı kendine");
            CollectionAssert.IsSubsetOf(events, Baskin.RegionEvents.Keys);
            // Aşama 12: 12 bölge olayı + 2 tesis olayı (na Katar, oc Yalpa); her bölgenin tesis şartı da tekil, alt bayraklar kapalıyken olay
            var facility = GameData.Regions.Select(r => FacilityMaps.EventFor(r, true, true)).ToList();
            CollectionAssert.AllItemsAreUnique(facility, "her bölgenin tesis şartı kendine");
            CollectionAssert.IsSubsetOf(facility, Baskin.RegionEvents.Keys);
            Assert.AreEqual(GameData.Regions.Length + 2, Baskin.RegionEvents.Count);
            CollectionAssert.AreEquivalent(Baskin.RegionEvents.Keys, events.Union(facility));
            CollectionAssert.AreEqual(new[] { "katar", "yalpa" }, GameData.Regions.Where(r => r.FacilityEvent != null).Select(r => r.FacilityEvent).ToArray());
            CollectionAssert.AreEqual(events, GameData.Regions.Select(r => FacilityMaps.EventFor(r, false, false)).ToList());
            foreach (var e in Baskin.RegionEvents.Values)
            {
                Assert.IsNotEmpty(e.Name); Assert.IsNotEmpty(e.Desc); Assert.IsNotEmpty(e.Tag);
            }
        }

        [Test]
        public void CampaignRaid_CarriesRegionEvent_UnlessSwitchedOff()
        {
            var sim = LegacySimulation.NewGame(3, new Balance { shelterEventsEnabled = false, startFon = 100 });
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual("bulut", sim.S.Raid.Params.Event);
            var off = LegacySimulation.NewGame(3, new Balance { shelterEventsEnabled = false, startFon = 100, liberationRegionEvents = false });
            Assert.IsTrue(off.StartLiberationRaid("weu_1").Ok);
            Assert.IsNull(off.S.Raid.Params.Event);
        }

        [Test]
        public void Events_DrawNoRandomness()
        {
            // Olay kurulumu RNG çekmez: aynı tohumla olaylı ve olaysız baskının RNG durumu aynı kalır (güvenli girişte uyanma yok)
            for (int seed = 1; seed <= 30; seed++)
                foreach (var ev in Baskin.RegionEvents.Keys)
                {
                    var p = new RaidParams { Seed = seed, Traits = new List<string> { "guvenli" }, Tactics = 1 };
                    var plain = Baskin.Create(p);
                    p.Event = ev;
                    var withEvent = Baskin.Create(p);
                    Assert.AreEqual(plain.Rs, withEvent.Rs, ev + " " + seed);
                }
        }

        /// <summary>
        /// Oda başına takvim (Docs/TESIS-BASKINLARI-TASARIMI.md §2.4): olay adımı yalnız son turdan önce işler, bu yüzden 5 turluk
        /// tesis odasında (ara oda) adımlar 8 turluk baskının önekidir; 9 turluk hedef odanın (tek kip denge turundan beri) adımları da bu tablonun devamıdır, ayrıca kilitli değil. Kaynak, tavan ve tüp darbe ve
        /// evre güdümlüdür: takvimleri boş. Kural değişirse bu tablo bilinçli güncellenir.
        /// </summary>
        [Test]
        public void EventSchedule_PerRoomBudget()
        {
            var expected = new Dictionary<string, (int[] Five, int[] Eight)>
            {
                ["bulut"] = (new[] { 1, 3 }, new[] { 1, 3, 5, 7 }),
                ["raf"] = (new[] { 3 }, new[] { 3, 5 }),
                ["gaz"] = (new[] { 1, 3, 4 }, new[] { 1, 3, 4, 6, 7 }),
                ["asansor"] = (new[] { 2 }, new[] { 2, 4, 6 }),
                ["kriyo"] = (new[] { 2 }, new[] { 2, 4, 6 }),
                ["turnike"] = (new[] { 2 }, new[] { 2, 4 }),
                ["suzgec"] = (new[] { 3 }, new[] { 3, 6 }),
                ["yag"] = (new[] { 2 }, new[] { 2 }),
                ["faz"] = (new[] { 3 }, new[] { 3, 6 }),
                ["kaynak"] = (new int[0], new int[0]),
                ["tavan"] = (new int[0], new int[0]),
                ["tup"] = (new int[0], new int[0]),
                // Aşama 12: Katar'ın adımı yok; Yalpa 2., 4., 6. tur sonu (bütçe içinde)
                ["katar"] = (new int[0], new int[0]),
                ["yalpa"] = (new[] { 2, 4 }, new[] { 2, 4, 6 }),
            };
            CollectionAssert.AreEquivalent(Baskin.RegionEvents.Keys, expected.Keys);
            foreach (var kv in expected)
            {
                CollectionAssert.AreEqual(kv.Value.Five, Baskin.EventStepTurns(kv.Key, 5), kv.Key + " 5 tur");
                CollectionAssert.AreEqual(kv.Value.Eight, Baskin.EventStepTurns(kv.Key, 8), kv.Key + " 8 tur");
            }
        }

        [Test]
        public void Signature_HasEventSuffix_OnlyWithEvent()
        {
            StringAssert.DoesNotContain("|v", Baskin.Signature(Baskin.Create(new RaidParams { Seed = 7 })));
            StringAssert.Contains("|vkaynak", Baskin.Signature(Baskin.Create(new RaidParams { Seed = 7, Event = "kaynak" })));
        }

        [Test]
        public void Undo_RestoresEventState()
        {
            var B = Empty("kaynak");
            B.Ev.P = P(3, 3); B.Ev.Mode = 1;
            B.Cells[3, 3].O = new Baskin.Obj { K = "kol", Hp = 1, Max = 1 };
            var tek = Team(B, "tek", 3, 2);
            string before = Baskin.Signature(B);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 3, 3), "kola sabotaj");
            Assert.AreEqual(1, B.Ev.Count);
            Assert.AreEqual(1, B.Cells[3, 3].O.Hp, "kaide hasar almaz");
            Assert.IsTrue(Baskin.Undo(B));
            Assert.AreEqual(before, Baskin.Signature(B));
            Assert.AreEqual(0, B.Ev.Count);
        }

        // ---------- Batı Avrupa: söndürme bulutu ----------
        [Test]
        public void Cloud_BlocksAimInAndOut_ButWorkOnObjectsGoesOn()
        {
            var B = Empty("bulut");
            var tek = Team(B, "tek", 3, 3);
            B.Cells[3, 3].Cloud = true;
            Machine(B, "dron", 0, 3, "shoot", 1);
            int hp = tek.Hp;
            var iv = Baskin.Intents(B).Single();
            Assert.IsTrue(iv.TargetNone && iv.Cloud, "buluta kesilen atış");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(hp, tek.Hp);
            Assert.IsTrue(B.FxList.Any(f => f.K == "clouded"));

            var C = Empty("bulut");
            var tek2 = Team(C, "tek", 6, 0);
            C.Cells[0, 6].Cloud = true;
            var robot = Machine(C, "bakim", 5, 0);
            var targets = Baskin.Targets(C, tek2);
            Assert.IsTrue(targets.Any(t => t.X == 7 && t.Y == 0), "buluttan hedefe sabotaj serbest");
            Assert.IsFalse(targets.Any(t => t.X == robot.X && t.Y == robot.Y), "buluttan birime sabotaj yok");
            Machine(C, "dron", 6, 3, "shoot", 0);
            Assert.IsTrue(Baskin.Intents(C).Last().Cloud, "buluttaki birim nişan alınmaz");
            var emp = Team(C, "emp", 0, 7);
            C.Cells[5, 0].Cloud = true;
            Assert.IsFalse(Baskin.Targets(C, emp).Any(t => t.X == 0 && t.Y == 5), "EMP buluta atılmaz");
        }

        [Test]
        public void Cloud_FillsOnOddTurns_AlternatingNozzles()
        {
            var B = Empty("bulut");
            B.Ev.P = P(2, 2); B.Ev.Area = new List<Baskin.Pt> { P(2, 2), P(2, 3) };
            B.Ev.Q = P(5, 5); B.Ev.Area2 = new List<Baskin.Pt> { P(5, 5), P(5, 6) };
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(B.Cells[3, 2].Cloud && !B.Cells[5, 5].Cloud, "1. tur sonu A");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsFalse(B.Cells[3, 2].Cloud, "2. tur sonu dağılır");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(B.Cells[5, 5].Cloud && !B.Cells[2, 2].Cloud, "3. tur sonu B");
        }

        // ---------- Doğu Avrupa: kayar arşiv rafı ----------
        static void Rail(Baskin.BState B)
        {
            B.Ev.Area = new List<Baskin.Pt> { P(3, 2), P(3, 3), P(3, 4), P(3, 5) };
            for (int i = 0; i < 4; i++) { B.Cells[2 + i, 3].Shelf = true; B.Cells[2 + i, 3].T = i == 0 ? "floor" : "wall"; }
        }

        [Test]
        public void Shelf_SlidesOnTurnThree_CrushingAndPushingWhoeverIsInTheGap()
        {
            var B = Empty("raf");
            Rail(B);
            var tek = Team(B, "tek", 3, 2);
            int hp = tek.Hp;
            B.Turn = 3;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(hp - 1, tek.Hp);
            Assert.AreEqual((2, 2), (tek.X, tek.Y), "dikey rayda batıya itilir");
            Assert.AreEqual("wall", B.Cells[2, 3].T);
            Assert.AreEqual("floor", B.Cells[5, 3].T, "geçit öbür uçta");
            Assert.AreEqual(3, B.Ev.Mode);
        }

        [Test]
        public void Shelf_Jams_WhenBothSidesAreBlocked_AndAMachineIsCrushedTheSame()
        {
            var B = Empty("raf");
            Rail(B);
            B.Cells[2, 2].T = "wall"; B.Cells[2, 4].T = "wall";
            var m = Machine(B, "bakim", 3, 2, hp: 3);
            B.Turn = 3;
            Assert.IsTrue(Baskin.EndTurn(B));
            Hold(B);
            Assert.AreEqual(2, m.Hp, "makine de 1 hasar");
            Assert.AreEqual("floor", B.Cells[2, 3].T, "raf sıkıştı, kaymadı");
            Assert.AreEqual(0, B.Ev.Mode);
        }

        // ---------- Orta Doğu: yanıcı gaz ----------
        [Test]
        public void Gas_IgnitedByEmp_FlaresNextTurn_HittingBothSides()
        {
            var B = Empty("gaz");
            B.Ev.P = P(3, 3); B.Ev.Area = new List<Baskin.Pt> { P(3, 3), P(4, 3), P(5, 3) };
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(B.Cells[3, 5].Gas, "1. tur sonu dolar");
            var emp = Team(B, "emp", 1, 3);
            var tek = Team(B, "tek", 5, 3);
            Assert.IsTrue(Baskin.DoAct(B, emp.Id, 3, 3));
            Assert.AreEqual(3, B.Ev.Due, "fitil: bir sonraki turun evre sonu");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(B.Cells[3, 3].Gas, "2. tur sonu parlamaz");
            var m = Machine(B, "dron", 4, 3);
            Team(B, "tek", 5, 3);
            int t = tek.Hp, mh = m.Hp;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(t - 1, tek.Hp);
            Assert.AreEqual(mh - 1, m.Hp, "parlama tarafa bakmaz");
            Assert.IsFalse(B.Cells[3, 4].Gas, "bulut silinir");
        }

        // ---------- Afrika: çatlak tavan ----------
        [Test]
        public void Ceiling_CollapsesTurnAfterAnImpact_LeavingRubble()
        {
            var B = Empty("tavan");
            B.Cells[4, 4].Crack = true;
            B.Cells[3, 4].T = "wall";
            var tek = Team(B, "tek", 2, 3);
            var m = Machine(B, "bakim", 3, 3, hp: 3);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 3, 3), "sabotaj: duvara çarpan robot darbe");
            Assert.AreEqual(2, B.Ev.Fuses.Single().Due);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsFalse(B.Cells[4, 4].O?.K == "enkaz", "aynı evrede çökmez");
            Hold(B, (m, 3, 3));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual("enkaz", B.Cells[4, 4].O?.K);
            Assert.IsFalse(B.Cells[4, 4].Crack);
        }

        [Test]
        public void Ceiling_FallsOnWhoeverStandsUnder()
        {
            var B = Empty("tavan");
            B.Cells[4, 4].Crack = true;
            B.Ev.Fuses.Add(new Baskin.Fuse { X = 4, Y = 4, Due = 1 });
            var m = Machine(B, "bakim", 4, 4, hp: 3);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(2, m.Hp);
            Assert.IsNull(B.Cells[4, 4].O, "altında birim varsa enkaz yok");
        }

        // ---------- Kuzey Amerika: yük asansörü ----------
        [Test]
        public void Lift_CarriesWhoeverStandsOnTheStop_AndHurtsTheBlocker()
        {
            var B = Empty("asansor");
            B.Ev.P = P(5, 5); B.Ev.Q = P(2, 2);
            var m = Machine(B, "bakim", 5, 5, hp: 3);
            B.Turn = 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(m.Cab && !Baskin.OnBoard(m), "kabinde, tahtada yok");
            Assert.IsNull(Baskin.UnitAt(B, 5, 5));
            var tek = Team(B, "tek", 2, 2);
            int hp = tek.Hp;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(hp - 1, tek.Hp, "iniş durağındaki 1 hasar");
            Assert.IsTrue(m.Cab, "kabin bir evre bekler");
            Team(B, "tek", 1, 2);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsFalse(m.Cab);
            Assert.IsTrue(B.FxList.Any(f => f.K == "ev" && f.Obj == "kabin_in" && f.Unit == m.Id && f.X == 2 && f.Y == 2), "B durağına iner (sonra planda yürür)");
        }

        [Test]
        public void Lift_SpecialistInCab_KeepsTheRaidGoing()
        {
            var B = Empty("asansor");
            B.Ev.P = P(5, 5); B.Ev.Q = P(2, 2);
            foreach (var u in B.Units.Where(u => u.Side == 'p' && u.Type != "tek")) u.Dead = true;
            var tek = Team(B, "tek", 5, 5);
            B.Turn = 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual("player", B.Phase, "kabindeki uzman baskında");
            Assert.IsFalse(Baskin.CanMove(B, tek));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((2, 2), (tek.X, tek.Y));
            Assert.IsTrue(Baskin.CanMove(B, tek));
        }

        // ---------- Rusya: kriyo hattı ----------
        [Test]
        public void Freeze_StopsWalkingNotActing_AndIceSoaksTheFirstHit()
        {
            var B = Empty("kriyo");
            B.Ev.Area = new List<Baskin.Pt> { P(3, 3), P(4, 3) };
            var tek = Team(B, "tek", 3, 3);
            var m = Machine(B, "dron", 4, 3);
            B.Turn = 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(tek.Frozen && m.Frozen, "kural tarafa bakmaz");
            Assert.AreEqual((4, 3), (m.X, m.Y), "donmuş makine planda yürümez");
            Assert.IsFalse(Baskin.CanMove(B, tek));
            Assert.IsTrue(Baskin.CanAct(B, tek));
            Hold(B, (m, 6, 3));
            m.Intent = new Baskin.Intent { Kind = "shoot", Dir = 3 };
            int hp = tek.Hp;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(hp, tek.Hp, "buz kabuğu karşıladı");
            Assert.IsFalse(tek.Frozen, "evre sonunda çözülür");
        }

        // ---------- Hindistan: vardiya turnikeleri ----------
        [Test]
        public void Gates_OneWay_ReversePushHitsTheEdge()
        {
            var B = Empty("turnike");
            // Hol (4, 3) çevresinde; (2, 3) turnikeli dış kare
            B.Ev.Q = P(4, 3);
            B.Ev.Area2 = new List<Baskin.Pt> { P(2, 3) };
            Assert.IsTrue(Baskin.CanStep(B, 2, 3, 3, 3), "içeri vardiyası");
            Assert.IsFalse(Baskin.CanStep(B, 3, 3, 2, 3));
            var tek = Team(B, "tek", 4, 3);
            var m = Machine(B, "bakim", 3, 3, hp: 4);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 3, 3));
            Assert.AreEqual((3, 3), (m.X, m.Y), "ters yönde itilen yerinde kalır");
            Assert.AreEqual(2, m.Hp, "sabotaj 1 + turnike 1");
            B.Turn = 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(1, B.Ev.Mode, "2. tur sonu döner");
            Assert.IsTrue(Baskin.CanStep(B, 3, 3, 2, 3));
            B.Turn = B.MaxTurns - 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(B.Ev.Stop && B.Ev.Mode == 1, "tahliye: hep dışarı");
        }

        [Test]
        public void Gates_DeadEndBehindTheHall_StaysOpenBothWays()
        {
            // x 6 sütunu duvar, tek açıklık (6, 3) holün içinde: sağdaki cebe yalnız holden girilir, kenarı turnikeli olmaz
            var B = Baskin.Create(new RaidParams { Seed = 11, MissionKind = "server" });
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) B.Cells[y, x] = new Baskin.Cell { T = x == 6 && y != 3 ? "wall" : "floor" };
            B.Cells[3, 5].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
            B.Exits = new List<Baskin.Pt> { P(0, 3) };
            B.Ev = new Baskin.EvState { Id = "turnike" };
            var setup = typeof(Baskin).GetMethod("SetupGates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsTrue((bool)setup.Invoke(null, new object[] { B, P(5, 3) }));
            Assert.IsFalse(B.Ev.Area2.Any(p => p.X == 7), "cep turnikeli dış kare değil");
            foreach (int mode in new[] { 0, 1 })
            {
                B.Ev.Mode = mode;
                Assert.IsTrue(Baskin.CanStep(B, 7, 3, 6, 3) && Baskin.CanStep(B, 6, 3, 7, 3), "cebin kenarı iki yöne açık");
            }
            B.Ev.Mode = 1;
            Assert.IsFalse(Baskin.CanStep(B, 3, 3, 4, 3), "dışarı vardiyasında hole girilmez");
            Assert.IsTrue(Baskin.CanStep(B, 4, 3, 3, 3));
        }

        [Test]
        public void Gates_EscapeStartsEvacuation()
        {
            // İçeri vardiyasında kasa açıldı: holdeki Teknisyen o evrenin sonunda dışarı çıkabilir, son iki turu beklemez
            var B = Empty("turnike");
            B.Ev.Q = P(4, 3);
            B.Ev.Area2 = new List<Baskin.Pt> { P(2, 3) };
            B.Exits = new List<Baskin.Pt> { P(0, 3) };
            Team(B, "tek", 3, 3);
            B.Turn = 3; B.Escape = true;
            Assert.AreEqual(Loc.T("TAHLİYE MODU"), Baskin.Hazards(B).Turns[3], "şerit tahliyeyi bu tura yazar");
            var edges = Baskin.Hazards(B).Edges;
            Assert.IsTrue(edges.Count > 0 && edges.All(e => e.Soon), "içeri oklar dönecek");
            B.Ev.Mode = 1;
            Assert.IsFalse(Baskin.Hazards(B).Edges.Any(e => e.Soon), "dışarı vardiyasında tahliye yönü değiştirmez");
            B.Ev.Mode = 0;
            Assert.IsFalse(Baskin.CanStep(B, 3, 3, 2, 3));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(B.Ev.Stop && B.Ev.Mode == 1, "kaçış başlayınca tahliye");
            Assert.IsTrue(Baskin.CanStep(B, 3, 3, 2, 3));
            Assert.IsNull(Baskin.Hazards(B).Turns[4], "tahliyeden sonra vardiya yok");
        }

        // ---------- Çin: kaynak kolu ----------
        [Test]
        public void Arm_WeldsEveryPhase_AndHitsAddRotation_SameForBothSides()
        {
            var B = Empty("kaynak");
            B.Ev.P = P(3, 3); B.Ev.Mode = 1;
            B.Cells[3, 3].O = new Baskin.Obj { K = "kol", Hp = 1, Max = 1 };
            var tek = Team(B, "tek", 4, 3);
            int hp = tek.Hp;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(hp - 1, tek.Hp);
            Assert.AreEqual(2, B.Ev.Mode, "saat yönünde bir adım");
            var m = Machine(B, "bakim", 3, 4, hp: 3);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(2, m.Hp, "makine de yanar");
            Assert.AreEqual(3, B.Ev.Mode);
            Hold(B, (m, 6, 6));
            Team(B, "tek", 3, 2);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 3, 3), "kola sabotaj");
            Assert.AreEqual(3, B.Ev.Mode, "bu evrenin karesi değişmez");
            B.Cells[3, 2].O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(1, B.Ev.Mode, "darbe dönüşe bir adım ekler");
            Assert.AreEqual(1, B.Cells[3, 2].O.Hp, "kaynak nesneye işlemez");
        }

        // ---------- Japonya-Kore: şebeke fazı ----------
        [Test]
        public void Phase_LiveChannelBurns_DeadChannelIsFloor_AndCapHolds()
        {
            var B = Empty("faz");
            B.Cells[3, 3].T = "cable"; B.Cells[3, 3].Phase = 1;
            B.Cells[3, 4].T = "cable"; B.Cells[3, 4].Phase = 2;
            var m = Machine(B, "bakim", 2, 3, hp: 5);
            var tek = Team(B, "tek", 1, 3);
            var emp = Team(B, "emp", 4, 3);
            int eh = emp.Hp;
            // Sabotajla canlı A kanalına itilen makine 1 hasar alır, evre sonunda ayrıca yanmaz (hasar tavanı)
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 2, 3));
            Assert.AreEqual((3, 3), (m.X, m.Y));
            Assert.AreEqual(3, m.Hp, "sabotaj 1 + canlı kanal 1");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(3, m.Hp, "evre sonunda ayrıca yanmaz");
            Assert.AreEqual(eh, emp.Hp, "sönük B kanalı güvenli");
            Hold(B, (m, 3, 3));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(2, m.Hp, "canlı kanalda evreyi bitiren yanar");
            Hold(B, (m, 3, 3));
            B.Turn = 4;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(2, m.Hp, "4. turda A sönük");
            Assert.AreEqual(eh - 1, emp.Hp, "B canlı");
        }

        // ---------- Güneydoğu Asya: süzgeç ----------
        [Test]
        public void Drain_PullsNorthFirst_ThenCollides()
        {
            var B = Empty("suzgec");
            B.Ev.P = P(3, 3);
            var tek = Team(B, "tek", 3, 2);
            var m = Machine(B, "bakim", 4, 3, hp: 3);
            int hp = tek.Hp;
            B.Turn = 3;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((3, 3), (tek.X, tek.Y), "kuzeydeki önce girer");
            Assert.AreEqual(hp - 1, tek.Hp);
            Assert.AreEqual(2, m.Hp, "çarpışma iki tarafa");
        }

        // ---------- Güney Amerika: basınçlı tüp ----------
        [Test]
        public void Tube_WhistlesWhenHit_FliesNextTurn_IntoTheFirstUnit()
        {
            var B = Empty("tup");
            B.Ev.Fuses = new List<Baskin.Fuse> { new Baskin.Fuse { X = 1, Y = 3, Dir = 1 } };
            B.Cells[3, 1].O = new Baskin.Obj { K = "tup", Hp = 1, Max = 1 };
            var tek = Team(B, "tek", 1, 2);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 1, 3));
            Assert.AreEqual(2, B.Ev.Fuses[0].Due);
            Assert.AreEqual("tup", B.Cells[3, 1].O?.K, "tüp kırılmaz, ıslık çalar");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsNotNull(B.Cells[3, 1].O, "aynı evrede uçmaz");
            var m = Machine(B, "bakim", 5, 3, hp: 3);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsNull(B.Cells[3, 1].O);
            Assert.AreEqual(2, m.Hp);
            Assert.IsTrue(B.FxList.Any(f => f.K == "push" && f.Unit == m.Id && f.X == 5 && f.Y == 3 && f.Dx == 1), "aynı yönde 1 kare itilir");
        }

        [Test]
        public void Tube_IntoAnObject_FallsWithoutHurtingIt()
        {
            var B = Empty("tup");
            B.Ev.Fuses = new List<Baskin.Fuse> { new Baskin.Fuse { X = 1, Y = 3, Dir = 1, Due = 1 } };
            B.Cells[3, 1].O = new Baskin.Obj { K = "tup", Hp = 1, Max = 1 };
            B.Cells[3, 4].O = new Baskin.Obj { K = "civ", Hp = 1, Max = 1 };
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsNull(B.Cells[3, 1].O);
            Assert.AreEqual(1, B.Cells[3, 4].O.Hp, "olay nesnelere işlemez");
            Assert.AreEqual(0, B.Collateral);
        }

        [Test]
        public void Tube_BreaksAnotherTube_WithoutChaining()
        {
            var B = Empty("tup");
            B.Ev.Fuses = new List<Baskin.Fuse> { new Baskin.Fuse { X = 1, Y = 3, Dir = 1, Due = 1 }, new Baskin.Fuse { X = 4, Y = 3, Dir = 1 } };
            B.Cells[3, 1].O = new Baskin.Obj { K = "tup", Hp = 1, Max = 1 };
            B.Cells[3, 4].O = new Baskin.Obj { K = "tup", Hp = 1, Max = 1 };
            var m = Machine(B, "bakim", 6, 3, hp: 3);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsNull(B.Cells[3, 4].O, "çarpılan tüp kırılır");
            Assert.IsEmpty(B.Ev.Fuses, "ve uçmaz");
            Assert.AreEqual(3, m.Hp);
        }

        // ---------- Okyanusya: hidrolik yağ ----------
        [Test]
        public void Oil_SpillsOnTurnTwo_PushedUnitsSlide_AndCrashIntoEachOther()
        {
            var B = Empty("yag");
            B.Ev.P = P(2, 3); B.Ev.Area = new List<Baskin.Pt> { P(2, 3), P(3, 3), P(4, 3), P(5, 3) };
            B.Turn = 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.IsTrue(B.Cells[3, 4].Oil);
            var tek = Team(B, "tek", 1, 3);
            var m = Machine(B, "bakim", 2, 3, hp: 4);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 2, 3));
            Assert.AreEqual((6, 3), (m.X, m.Y), "yağ bitene kadar kayar");
            Assert.AreEqual(3, m.Hp, "yalnız sabotaj hasarı");

            var C = Empty("yag");
            C.Ev.Stop = true;
            foreach (var x in new[] { 2, 3, 4, 5 }) C.Cells[3, x].Oil = true;
            var t2 = Team(C, "tek", 1, 3);
            var m1 = Machine(C, "bakim", 2, 3, hp: 4);
            var m2 = Machine(C, "bakim", 5, 3, hp: 4);
            Assert.IsTrue(Baskin.DoAct(C, t2.Id, 2, 3));
            Assert.AreEqual((4, 3), (m1.X, m1.Y), "önü kapanınca durur");
            Assert.AreEqual(2, m1.Hp, "sabotaj 1 + çarpma 1");
            Assert.AreEqual(3, m2.Hp, "çarptığı da 1");
        }

        [Test]
        public void Oil_HitByShot_SlidesInTheShotDirection()
        {
            var B = Empty("yag");
            B.Ev.Stop = true;
            foreach (var x in new[] { 3, 4 }) B.Cells[3, x].Oil = true;
            var tek = Team(B, "tek", 3, 3);
            Machine(B, "dron", 0, 3, "shoot", 1);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((5, 3), (tek.X, tek.Y), "isabet yönünde kayar");
        }

        // ---------- Olay nişanı (tesis aşama 10, A0) ----------
        /// <summary>Tesis odası (G, olaysız, düz zemin; tek uzman köşede) ve elle kurulan olay; makine (2, 3)'te.</summary>
        static (Baskin.BState B, Baskin.Unit E) FacilityEvent(string ev)
        {
            var B = FacilityKit.Open(FacilityKit.Params(FacilityKit.Wings[0], 41, 1, false), new List<RoomReport>(), FacilityKit.Bal(events: false));
            FacilityKit.Flat(B, P(0, 7));
            Assert.IsNotNull(B.Room);
            B.Ev = new Baskin.EvState { Id = ev, P = P(3, 3), Area = new List<Baskin.Pt> { P(3, 3) } };
            return (B, FacilityKit.AddMachine(B, "dron", P(2, 3)));
        }

        /// <summary>(3, 3)'e kurulan birimin olay niyeti puanı: gazda patlama, yağda doğuya yakın vuruş (arkası duvar: kayar ve çarpar).</summary>
        static double AimAt(string ev, System.Func<Baskin.BState, Baskin.Unit> put)
        {
            var (B, e) = FacilityEvent(ev);
            if (ev == "gaz") B.Cells[3, 3].Gas = true;
            else { B.Cells[3, 3].Oil = true; B.Cells[3, 4].T = "wall"; }
            var v = put(B);
            Assert.AreSame(v, Baskin.UnitAt(B, 3, 3), "birim kendi karesinde");
            return ev == "gaz" ? Baskin.TestEventAim(B, e, "blast", 0) : Baskin.TestEventAim(B, e, "melee", FacilityKit.DirOf(new[] { 1, 0 }));
        }

        /// <summary>
        /// Olay niyetinin ekip değeri makine aklının ekip yüklemine bakar (aşama 10 K3; on birinci tur 51, on ikinci tur 34): tesiste uzman,
        /// yürüyen yem ve erimde havlayan köpek ekip; susan köpek, taret, vinç ve tutsak hedef değil (0, makine cezası da değil). Eski
        /// tahtada sınıf yok: her Side 'p' birim ekip, değer değişmez.
        /// </summary>
        [Test]
        public void EventAim_Gas_CountsOnlyTeamLikeUnits()
        {
            var A = Empty("gaz");
            A.Ev.P = P(3, 3); A.Ev.Area = new List<Baskin.Pt> { P(3, 3) };
            A.Cells[3, 3].Gas = true;
            var ea = Machine(A, "dron", 2, 3);
            Team(A, "tek", 3, 3);
            Assert.AreEqual(2, Baskin.TestEventAim(A, ea, "blast", 0), "örnek tahta: uzman");
            Team(A, "tek", 6, 6);
            Machine(A, "dron", 3, 3);
            Assert.AreEqual(-1, Baskin.TestEventAim(A, ea, "blast", 0), "örnek tahta: makine");

            int[] east = { 1, 0 };
            Assert.AreEqual(2, AimAt("gaz", B => { var u = FacilityKit.BySlot(B, 0); FacilityKit.Place(u, P(3, 3)); return u; }), "uzman");
            Assert.AreEqual(2, AimAt("gaz", B => FacilityKit.AddGadget(B, "yem", P(3, 3), east)), "yem");
            Assert.AreEqual(0, AimAt("gaz", B => FacilityKit.AddGadget(B, "taret", P(3, 3), east)), "taret");
            Assert.AreEqual(0, AimAt("gaz", B => FacilityKit.AddGadget(B, "vinc", P(3, 3), east)), "vinç");
            Assert.AreEqual(0, AimAt("gaz", B => FacilityKit.AddDog(B, 0, P(3, 3))), "susan köpek");
            Assert.AreEqual(2, AimAt("gaz", B => { var d = FacilityKit.AddDog(B, 0, P(3, 3)); d.Bark = true; return d; }), "erimde havlayan köpek");
            Assert.AreEqual(0, AimAt("gaz", B => FacilityKit.Captive(FacilityKit.AddMachine(B, "dron", P(3, 3)))), "tutsak");
            Assert.AreEqual(-1, AimAt("gaz", B => FacilityKit.AddMachine(B, "dron", P(3, 3))), "makine");
        }

        /// <summary>Yağda kaydırıp çarptırmak yalnız uzmana değer (aşama 10 K3): köpek, yem ve tutsak 0; örnek tahtada (Baskin.Create) uzman aynı.</summary>
        [Test]
        public void EventAim_Oil_OnlySpecialist()
        {
            var A = Empty("yag");
            A.Ev.Stop = true;
            A.Cells[3, 3].Oil = true; A.Cells[3, 4].T = "wall";
            var ea = Machine(A, "dron", 2, 3);
            Team(A, "tek", 3, 3);
            Assert.AreEqual(2, Baskin.TestEventAim(A, ea, "melee", 1), "örnek tahta: uzman");

            int[] east = { 1, 0 };
            Assert.AreEqual(2, AimAt("yag", B => { var u = FacilityKit.BySlot(B, 0); FacilityKit.Place(u, P(3, 3)); return u; }), "uzman");
            Assert.AreEqual(0, AimAt("yag", B => FacilityKit.AddGadget(B, "yem", P(3, 3), east)), "yem");
            Assert.AreEqual(0, AimAt("yag", B => { var d = FacilityKit.AddDog(B, 0, P(3, 3)); d.Bark = true; return d; }), "havlayan köpek");
            Assert.AreEqual(0, AimAt("yag", B => FacilityKit.Captive(FacilityKit.AddMachine(B, "dron", P(3, 3)))), "tutsak");
        }

        // ---------- Önizleme ----------
        [Test]
        public void Hazards_WarnTheUnitOnTheWeldTile_WithoutTouchingTheState()
        {
            var B = Empty("kaynak");
            B.Ev.P = P(3, 3); B.Ev.Mode = 1;
            B.Cells[3, 3].O = new Baskin.Obj { K = "kol", Hp = 1, Max = 1 };
            var tek = Team(B, "tek", 4, 3);
            string before = Baskin.Signature(B);
            var hz = Baskin.Hazards(B);
            Assert.AreEqual("kaynak", hz.Event);
            Assert.IsTrue(hz.Marks.Any(m => m.Kind == "weld" && m.X == 4 && m.Y == 3));
            var w = hz.Warnings.Single(x => x.Unit == tek.Id);
            Assert.AreEqual(1, w.Damage);
            Assert.AreEqual(before, Baskin.Signature(B), "önizleme durumu değiştirmez");
            var away = Baskin.PreviewMove(B, tek.Id, 5, 5);
            Assert.IsFalse(away.Warnings.Any(x => x.Unit == tek.Id), "kaynak karesinden çıkınca uyarı yok");
            Assert.AreEqual(before, Baskin.Signature(B));
        }

        [Test]
        public void Hazards_EmptyWithoutEvent()
        {
            var hz = Baskin.Hazards(Baskin.Create(new RaidParams { Seed = 3 }));
            Assert.IsNull(hz.Event);
            Assert.IsEmpty(hz.Marks);
        }

        /// <summary>Teknisyen hedef dolabın bitişiğine yürüyerek varabilir mi (makineler yok sayılır, turnike yönü sayılır).</summary>
        static bool Reaches(Baskin.BState B, Baskin.Unit from)
        {
            var seen = new HashSet<(int, int)> { (from.X, from.Y) };
            var q = new Queue<(int X, int Y)>(); q.Enqueue((from.X, from.Y));
            var goals = Baskin.TargetsLeft(B).SelectMany(t => Baskin.Dirs.Select(d => (t.X + d[0], t.Y + d[1]))).ToHashSet();
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (goals.Contains(p)) return true;
                foreach (var d in Baskin.Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N || !seen.Add((x, y))) continue;
                    if (B.Cells[y, x].T == "wall" || B.Cells[y, x].O != null || !Baskin.CanStep(B, p.X, p.Y, x, y)) continue;
                    q.Enqueue((x, y));
                }
            }
            return false;
        }
    }
}
