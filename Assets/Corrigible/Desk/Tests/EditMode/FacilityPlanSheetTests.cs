using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 5 (Docs/TESIS-BASKINLARI-TASARIMI.md §7.1): görev fişinin yanındaki bina planının üç paftası. Kurucu yalnız tesis
    /// odasının planını okur (robot, niyet, uyanma yok; baskın tohumundan bağımsız; RNG çekmez), bilinen oda çizilir, bilinmeyende
    /// PLAN YOK; simülasyon ya da bölge yokken veri yok. Sahne: SiginakHud'da kadro raporunun altında, fişe bağlı.
    /// </summary>
    public class FacilityPlanSheetTests
    {
        static readonly string[] Forbidden = { "machine", "robot", "intent", "spawn", "wake", "enemy", "unit", "foe", "seed" };

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        static string Region(Simulation sim) => sim.S.Liberation.CurrentRegion;

        [Test]
        public void NoSimOrRegion_NoPlanData()
        {
            Assert.IsNull(FacilityPlanBuilder.Build(FacilityDeskKit.New(), null));
            Assert.IsNull(FacilityPlanBuilder.Build(null, "weu"));
        }

        [Test]
        public void NothingKnown_ThreeSheetsWithPlanYok()
        {
            var sim = FacilityDeskKit.New();
            Assert.AreEqual(0, sim.Facility.Known.Count, "harekât başında bilinen oda yok");
            var d = FacilityPlanBuilder.Build(sim, Region(sim));
            Assert.IsNotNull(d);
            var next = sim.NextLiberationSector(Region(sim));
            Assert.AreEqual(next.Id, d.SectorId);
            Assert.AreEqual(next.Index + 1, d.Wing);
            Assert.IsTrue(d.Front);
            Assert.AreEqual(3, d.Sheets.Count);
            var wing = sim.FacilityPlan.Wing(next.Id);
            for (int i = 0; i < 3; i++)
            {
                var s = d.Sheets[i];
                Assert.AreEqual(wing[i].Id, s.RoomId);
                Assert.AreEqual(i + 1, s.Index);
                Assert.IsFalse(s.Known);
                Assert.AreEqual("PLAN YOK", s.Stamp);
                Assert.AreEqual(Loc.F("PAFTA {0}/3 · {1}", i + 1, FacilityPlanBuilder.TypeWord(wing[i].Type)), s.Title);
                Assert.IsEmpty(s.Tiles, "bilinmeyen odada çizim yok");
                Assert.IsEmpty(s.Marks);
                Assert.IsNull(s.Caption, "oda adı bilinmez (türünü ele verir)");
                Assert.IsNull(s.Note);
                StringAssert.DoesNotContain(wing[i].Name, s.HoverTitle + s.HoverLine);
            }
            CollectionAssert.AreEqual(new[] { "PAFTA 1/3 · GİRİŞ", "PAFTA 2/3 · ARA ODA", "PAFTA 3/3 · HEDEF" }, d.Sheets.Select(s => s.Title));
        }

        [Test]
        public void TapeKnowsTheEntry_OnlyTheFirstSheetIsDrawn()
        {
            var sim = FacilityDeskKit.New();
            string sector = FacilityDeskKit.StartFirst(sim);
            var B = sim.OpenRaidRoom();
            FacilityDeskKit.PlayRoom(B);
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
            var room = sim.FacilityPlan.Room(sector, 1);
            Assert.IsTrue(sim.FacilityKnown(room.Id), "teyp: girilen oda bilinir");

            var d = FacilityPlanBuilder.Build(sim, Region(sim));
            Assert.AreEqual(sector, d.SectorId, "baskın sürerken de aynı kanat");
            CollectionAssert.AreEqual(new[] { true, false, false }, d.Sheets.Select(s => s.Known));
            var first = d.Sheets[0];
            Assert.IsNull(first.Stamp);
            Assert.AreEqual(64, first.Tiles.Count);
            Assert.AreEqual(room.Name, first.Caption);
            Assert.IsFalse(first.NoteIsEvent, "giriş odasında şart yok");
            Assert.AreEqual(Loc.T(Baskin.ObjNames[room.Device]), first.Note, "antette donanım");

            // Planın kendisi: giriş odası şartsız, kareler satır dökümüyle bire bir (tahtayla aynı yön: y = 0 kuzey)
            foreach (var t in first.Tiles)
            {
                char ch = room.Rows[t.Y][t.X];
                Assert.AreEqual(ch == '#' ? PlanGround.Wall : ch == '~' ? PlanGround.Cable : ch == '=' ? PlanGround.Shutter : PlanGround.Floor, t.Ground, $"{ch} @ {t.X},{t.Y}");
                Assert.AreEqual(ch == 'E' ? 1 : ch == 'F' ? 2 : 0, t.Door, $"{ch} @ {t.X},{t.Y}");
                Assert.AreEqual(ch == 'o', t.Vent != 0, $"{ch} @ {t.X},{t.Y}");
                var expected = ch switch
                {
                    'C' => PlanItem.Target, 'P' => PlanItem.Panel, 'R' => PlanItem.Antenna, 'V' => PlanItem.Valve,
                    'S' => PlanItem.Civilian, 'K' => PlanItem.Cooler, 'U' => PlanItem.Copy, _ => PlanItem.None,
                };
                Assert.AreEqual(expected, t.Item, $"{ch} @ {t.X},{t.Y}");
            }
            Assert.IsTrue(first.Tiles.Any(t => t.Door == 1), "batı girişi");
            Assert.IsTrue(first.Tiles.Any(t => t.Door == 2), "ileri geçit");
            StringAssert.Contains(Loc.F("Donanım: {0}", Loc.T(Baskin.ObjNames[room.Device])), first.HoverLine);
        }

        [Test]
        public void Data_HasNoRobotFields_AndIgnoresTheRaidSeed()
        {
            // Model sınıflarında robot, niyet, uyanma ya da tohum alanı yok
            foreach (var type in new[] { typeof(FacilityPlanData), typeof(FacilityPlanSheetData), typeof(PlanTile), typeof(PlanMark) })
                foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    foreach (var word in Forbidden)
                        Assert.IsFalse(f.Name.ToLowerInvariant().Contains(word), type.Name + "." + f.Name);
            // Tesis aşama 11: önceki baskından kalan engel (tel, kalas) ve hurda planın nesnesidir (kalıcı tahta)
            CollectionAssert.AreEquivalent(new[] { "None", "Target", "Copy", "Cooler", "Civilian", "Panel", "Antenna", "Valve", "Debris", "Tank", "Arm", "Barrier", "Scrap" },
                System.Enum.GetNames(typeof(PlanItem)), "planın nesneleri sabit nesneler ve izlerin engelleridir");

            var a = FacilityDeskKit.New();
            var b = FacilityDeskKit.New();
            b.S.Rng.Next(); b.S.Rng.Next(); b.S.Rng.Next();
            foreach (var sim in new[] { a, b })
            {
                var next = sim.NextLiberationSector(Region(sim));
                foreach (var room in sim.FacilityPlan.Wing(next.Id)) sim.Facility.Known.Add(room.Id);
            }
            string before = Dump(FacilityPlanBuilder.Build(a, Region(a)));
            FacilityDeskKit.StartFirst(a);
            FacilityDeskKit.StartFirst(b);
            Assert.AreNotEqual(a.S.Raid.Params.Seed, b.S.Raid.Params.Seed, "iki ayrı baskın tohumu");
            string da = Dump(FacilityPlanBuilder.Build(a, Region(a))), db = Dump(FacilityPlanBuilder.Build(b, Region(b)));
            Assert.AreEqual(da, db, "plan baskın tohumundan bağımsız");
            Assert.AreEqual(before, da, "baskın emri planı değiştirmez");
            StringAssert.DoesNotContain("PLAN YOK", da);
        }

        /// <summary>
        /// Tesis aşama 10 (K41): yapısal önlemler kırmızı kalemle. Çift cidar ve koku yayıcı işaretleri planın kaplamasıyla birebir
        /// (<c>Simulation.FacilityPlanCells</c> = emirdeki tahta); kademe 0'da hiçbiri, 1'de hedef odada çift cidar yok, 2'de var; yayıcı her
        /// odada kademe kadar. Çizimin Counter katmanı yalnız işaret varken dolu; hover kırmızı kalemi söyler; bilinmeyen paftada yok.
        /// </summary>
        [Test]
        public void Counters_RedPen_FollowTheLevel_MatchThePlanCells()
        {
            foreach (int level in new[] { 0, 1, 2 })
            {
                var sim = FacilityDeskKit.New();
                sim.Facility.Counter.Clear();
                sim.Facility.Counter.AddRange(new[] { 0, level, 0, 0, level, 0 });
                var next = sim.NextLiberationSector(Region(sim));
                var wing = sim.FacilityPlan.Wing(next.Id);
                foreach (var room in wing.Take(2)) sim.Facility.Known.Add(room.Id);
                var d = FacilityPlanBuilder.Build(sim, Region(sim));
                int targetWalls = -1;
                for (int i = 0; i < 3; i++)
                {
                    var s = d.Sheets[i];
                    if (!s.Known)
                    {
                        Assert.IsEmpty(s.Counters, "bilinmeyen paftada kalem yok");
                        sim.Facility.Known.Add(wing[i].Id);
                        s = FacilityPlanBuilder.Build(sim, Region(sim)).Sheets[i];
                    }
                    var cells = sim.FacilityPlanCells(wing[i]);
                    var walls = new List<(int, int)>();
                    var emitters = new List<(int, int)>();
                    for (int y = 0; y < Baskin.N; y++)
                        for (int x = 0; x < Baskin.N; x++)
                        {
                            if (cells[y, x].T == "wall" && cells[y, x].Double) walls.Add((x, y));
                            if (cells[y, x].O?.K == "yayici") emitters.Add((x, y));
                        }
                    CollectionAssert.AreEquivalent(walls, s.Counters.Where(m => m.Kind == "cidar").Select(m => (m.X, m.Y)), wing[i].Id + " çift cidar");
                    CollectionAssert.AreEquivalent(emitters, s.Counters.Where(m => m.Kind == "yayici").Select(m => (m.X, m.Y)), wing[i].Id + " yayıcı");
                    Assert.AreEqual(level, emitters.Count, wing[i].Id + ": yayıcı her odada kademe kadar");
                    if (i == 2) targetWalls = walls.Count;
                    else if (level >= 1) Assert.Greater(walls.Count, 0, wing[i].Id + ": giriş ve ara odada çift cidar");
                    Assert.IsFalse(s.Tiles.Any(t => t.Item != PlanItem.None && emitters.Contains((t.X, t.Y))), "yayıcı nesne katmanında değil");
                    var sketch = FacilityPlanSketch.Draw(s, 22);
                    Assert.AreEqual(s.Counters.Count > 0, sketch.Counter.Count > 0, "Counter katmanı yalnız kalem varken");
                    Assert.AreEqual(walls.Count > 0, s.HoverLine.Contains("Önlem (kırmızı kalem): Çift cidar"), s.HoverLine);
                    Assert.AreEqual(emitters.Count > 0, s.HoverLine.Contains("Önlem (kırmızı kalem): Koku yayıcı"), s.HoverLine);
                }
                Assert.AreEqual(level >= 2, targetWalls > 0, "hedef odada çift cidar yalnız 2. kademede");
            }
            // Bilinmeyen pafta: çerçeve, kalem yok
            var unknown = FacilityPlanSketch.Draw(new FacilityPlanSheetData { Index = 1, Known = false, Stamp = "PLAN YOK" }, 22);
            Assert.IsEmpty(unknown.Counter);
            // Alt bayrak kapalı: kaplama ve kalem yok
            var bal = FacilityDeskKit.Bal();
            bal.facilityCountermeasures = false;
            var off = Simulation.NewGame(79, bal);
            off.Facility.Counter.AddRange(new[] { 0, 2, 0, 0, 2, 0 });
            var w = off.FacilityPlan.Wing(off.NextLiberationSector(Region(off)).Id);
            foreach (var room in w) off.Facility.Known.Add(room.Id);
            Assert.IsTrue(FacilityPlanBuilder.Build(off, Region(off)).Sheets.All(x => x.Counters.Count == 0), "alt bayrak kapalı");
        }

        /// <summary>
        /// Tesis aşama 11 (§8.4, K39-6): kuşatma kipinde paftalar savunulan (alınmış) kanadındır; izler planın kareleriyle birebir
        /// (<c>Simulation.FacilityPlanCells</c> izleri uygular): yıkık duvar karesi zemin ve mürekkep katmanında moloz taraması, tel plan
        /// nesnesi (Barrier); hover "İz: …", YZ onarımı ve giriş/ara odada "Terk edilirse izler silinir." Savunmada terk edilen odanın izi
        /// silinir (pafta izsiz). İz alt bayrağı kapalıyken iz yok.
        /// </summary>
        [Test]
        public void Traces_MatchThePlanCells_DefendedWing_AbandonedRoomWiped()
        {
            var (sim, sector) = FacilitySiegeDeskKit.NewSieged();
            var wing = sim.FacilityPlan.Wing(sector.Id);
            // Kurulum (kayda girmez): giriş odasına bir yıkık duvar ve bir tel izi
            var entry = wing[0];
            var cells = sim.FacilityPlanCells(entry);
            (int x, int y) wall = (-1, -1);
            for (int y = 0; y < Baskin.N && wall.x < 0; y++)
                for (int x = 1; x < Baskin.N - 1 && wall.x < 0; x++)
                    if (cells[y, x].T == "wall" && !cells[y, x].Shutter && !cells[y, x].Crate) wall = (x, y);
            Assert.GreaterOrEqual(wall.x, 0, "duvar karesi");
            sim.Facility.Marks.Add(new FacilityMark { Room = entry.Id, Kind = FacilityMarks.Wall, X = wall.x, Y = wall.y, Day = sim.S.Day });
            FacilityMark wire = null;
            for (int y = 0; y < Baskin.N && wire == null; y++)
                for (int x = 0; x < Baskin.N && wire == null; x++)
                {
                    if (cells[y, x].T != "floor" || cells[y, x].O != null || cells[y, x].Door != 0 || cells[y, x].Vent != 0) continue;
                    var m = new FacilityMark { Room = entry.Id, Kind = FacilityMarks.Wire, X = x, Y = y, Hp = 2, Max = 3, Day = sim.S.Day };
                    sim.Facility.Marks.Add(m);
                    if (sim.FacilityPlanCells(entry)[y, x].O?.K == FacilityMarks.Wire) wire = m;
                    else sim.Facility.Marks.Remove(m);
                }
            Assert.IsNotNull(wire, "tel izi uygulanabilir bir kare");
            sim.Facility.Repairs.Add(new FacilityMark { Room = entry.Id, Kind = FacilityMarks.Wall, X = 0, Y = 0, Day = sim.S.Day - 1 });

            var d = FacilityPlanBuilder.Build(sim, sector.Region);
            Assert.AreEqual(sector.Id, d.SectorId, "kuşatma kipinde savunulan kanadın paftaları");
            Assert.IsTrue(d.Front);
            for (int i = 0; i < 3; i++)
            {
                var s = d.Sheets[i];
                Assert.IsTrue(s.Known, wing[i].Id + ": oynanan oda bilinir");
                var plan = sim.FacilityPlanCells(wing[i]);
                foreach (var m in sim.FacilityMarksOf(wing[i].Id))
                {
                    var t = s.Traces.SingleOrDefault(x => x.Kind == m.Kind && x.X == m.X && x.Y == m.Y);
                    Assert.IsNotNull(t, wing[i].Id + " " + m.Kind);
                    var tile = s.Tiles.Single(x => x.X == m.X && x.Y == m.Y);
                    if (m.Kind == FacilityMarks.Wall) Assert.AreEqual(PlanGround.Floor, tile.Ground, "yıkık duvar planda zemin");
                    if (m.Kind == FacilityMarks.Wire) { Assert.AreEqual(PlanItem.Barrier, tile.Item); Assert.AreEqual(2, t.Hp); }
                    Assert.AreEqual(plan[m.Y, m.X].T, tile.Ground == PlanGround.Floor ? "floor" : plan[m.Y, m.X].T);
                }
                Assert.AreEqual(sim.FacilityMarksOf(wing[i].Id).Count, s.Traces.Count, wing[i].Id + ": izler = uygulanan izler");
                var sketch = FacilityPlanSketch.Draw(s, 22);
                Assert.AreEqual(s.Traces.Any(m => !FacilityMarks.IsObjectKind(m.Kind)), sketch.Traces.Count > 0, wing[i].Id + ": mürekkep izi");
                Assert.AreEqual(i < 2, s.HoverLine.Contains(FacilityDefenseText.PlanAbandonNote), wing[i].Id + ": terk notu (hedef odada yok)");
            }
            StringAssert.Contains(Loc.F("İz: {0}", Loc.F("{0} {1}", 1, Loc.T("yıkık duvar"))), d.Sheets[0].HoverLine);
            StringAssert.Contains(Loc.F("{0} {1} ({2} can)", 1, Loc.T("tel"), 2), d.Sheets[0].HoverLine);
            StringAssert.Contains(Loc.F("YZ onardı · V.{0:000}", sim.S.Day), d.Sheets[0].HoverLine);

            // Savunma: giriş odası terk edilir → izleri silinir; savunulan kanadın paftası (sektör yüklemesi) izsiz
            FacilitySiegeDeskKit.Defend(sim);
            FacilitySiegeDeskKit.PlayDefense(sim, 1, true);
            Assert.IsEmpty(sim.FacilityMarksOf(entry.Id), "terk edilen odanın izi silindi");
            var after = FacilityPlanBuilder.Build(sim, sector.Region, sector.Id);
            Assert.AreEqual(sector.Id, after.SectorId);
            Assert.IsEmpty(after.Sheets[0].Traces, "pafta 1 izsiz");
            Assert.IsFalse(after.Sheets[0].HoverLine.Contains(Loc.F("İz: {0}", "")), after.Sheets[0].HoverLine);

            // Alt bayrak kapalı: aynı iz kurulumu planda görünmez
            var (off, offSector) = FacilitySiegeDeskKit.NewSieged(tune: b => b.facilityMarks = false);
            var offEntry = off.FacilityPlan.Wing(offSector.Id)[0];
            off.Facility.Marks.Add(new FacilityMark { Room = offEntry.Id, Kind = FacilityMarks.Wall, X = wall.x, Y = wall.y });
            Assert.IsTrue(FacilityPlanBuilder.Build(off, offSector.Region).Sheets.All(x => x.Traces.Count == 0), "iz alt bayrağı kapalı");
        }

        [Test]
        public void Builder_IsAPureRead()
        {
            var sim = FacilityDeskKit.New();
            var next = sim.NextLiberationSector(Region(sim));
            sim.Facility.Known.Add(sim.FacilityPlan.Wing(next.Id)[1].Id);
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count;
            string lib = JsonUtility.ToJson(sim.S.Liberation);
            foreach (var r in sim.S.Liberation.Regions) FacilityPlanBuilder.Build(sim, r.Id);
            Assert.AreEqual(rng, sim.S.Rng.State, "RNG çekilmez");
            Assert.AreEqual(commands, sim.Record.commands.Count, "komut yazılmaz");
            Assert.AreEqual(lib, JsonUtility.ToJson(sim.S.Liberation), "durum yazılmaz");
        }

        [Test]
        public void Events_PartsFollowTheTier_AndTurnOffWithRegionEvents()
        {
            foreach (bool events in new[] { true, false })
            {
                var bal = FacilityDeskKit.Bal();
                bal.liberationRegionEvents = events;
                var sim = Simulation.NewGame(79, bal);
                var next = sim.NextLiberationSector(Region(sim));
                var wing = sim.FacilityPlan.Wing(next.Id);
                foreach (var room in wing) sim.Facility.Known.Add(room.Id);
                var d = FacilityPlanBuilder.Build(sim, Region(sim));
                for (int i = 0; i < 3; i++)
                {
                    var room = wing[i];
                    var s = d.Sheets[i];
                    bool on = events && room.Event != null;
                    Assert.AreEqual(on ? room.PartsAt(sim.Tier).Count() : 0, s.Marks.Count, room.Id);
                    Assert.AreEqual(on, s.NoteIsEvent, room.Id);
                    if (on) Assert.AreEqual(Loc.T(Baskin.RegionEvents[room.Event].Name), s.Note, room.Id);
                    var cells = sim.FacilityPlanCells(room);
                    foreach (var t in s.Tiles)
                        Assert.AreEqual(cells[t.Y, t.X].T == "wall", t.Ground == PlanGround.Wall || t.Ground == PlanGround.Shutter, room.Id + " duvar " + t.X + "," + t.Y);
                }
                Assert.IsTrue(wing.Any(r => r.Event != null), "karargâhın kanadında şartlı oda var (A ve H)");
            }
        }

        [Test]
        public void InfoSlipRegion_ShowsItsNextWingOnlyWhenARoomIsKnown()
        {
            var sim = FacilityDeskKit.New();
            string other = sim.S.Liberation.Regions.Select(r => r.Id).First(id => id != Region(sim));
            Assert.IsNull(FacilityPlanBuilder.Build(sim, other), "cephe dışında hiçbir odası bilinmeyen kanat: üç boş pafta haritayı örtmez");
            var wing = sim.FacilityPlan.Wing(sim.NextLiberationSector(other).Id);
            sim.Facility.Known.Add(wing[2].Id);
            var d = FacilityPlanBuilder.Build(sim, other);
            Assert.IsNotNull(d);
            Assert.IsFalse(d.Front, "cephe dışı: bilgi fişi");
            Assert.AreEqual(sim.NextLiberationSector(other).Id, d.SectorId);
            CollectionAssert.AreEqual(new[] { false, false, true }, d.Sheets.Select(s => s.Known));
        }

        [Test]
        public void Sketch_UnknownIsAFrame_KnownDrawsTheLayers()
        {
            var unknown = FacilityPlanSketch.Draw(new FacilityPlanSheetData { Index = 1, Known = false, Stamp = "PLAN YOK" }, 22);
            Assert.AreEqual(1, unknown.Lines.Count, "yalnız dış çerçeve");
            Assert.AreEqual(5, unknown.Lines[0].Count);
            Assert.IsEmpty(unknown.Grid); Assert.IsEmpty(unknown.Walls); Assert.IsEmpty(unknown.Links); Assert.IsEmpty(unknown.Areas); Assert.IsEmpty(unknown.Parts);

            var bal = FacilityDeskKit.Bal();
            var sim = Simulation.NewGame(79, bal);
            var sheets = new List<FacilityPlanSheetData>();
            foreach (var r in sim.S.Liberation.Regions)
                for (int w = 0; w < 3; w++)
                {
                    var wing = sim.FacilityPlan.Wing(r.Sectors[w].Id);
                    foreach (var room in wing) if (!sim.Facility.Known.Contains(room.Id)) sim.Facility.Known.Add(room.Id);
                }
            foreach (var r in sim.S.Liberation.Regions) sheets.AddRange(FacilityPlanBuilder.Build(sim, r.Id).Sheets);
            Assert.AreEqual(sim.S.Liberation.Regions.Count * 3, sheets.Count, "her bölgenin sıradaki kanadı");
            foreach (var s in sheets)
            {
                var k = FacilityPlanSketch.Draw(s, 22);
                Assert.AreEqual(14, k.Grid.Count, "7 + 7 iç ızgara çizgisi");
                int wallTiles = s.Tiles.Count(t => t.Ground == PlanGround.Wall);
                Assert.AreEqual(wallTiles * 22f, k.Walls.Sum(p => p[1].x - p[0].x), 0.01f, s.RoomId + ": duvar sıraları bütün duvar karelerini kaplar");
                Assert.AreEqual(s.Tiles.Count(t => t.Vent != 0) / 2, k.Links.Count, "menfez çifti bir bağ");
                foreach (var path in k.Grid.Concat(k.Walls).Concat(k.Lines).Concat(k.Areas).Concat(k.Parts).SelectMany(p => p))
                {
                    Assert.GreaterOrEqual(path.x, -0.01f); Assert.LessOrEqual(path.x, 176.01f);
                    Assert.GreaterOrEqual(path.y, -0.01f); Assert.LessOrEqual(path.y, 176.01f);
                }
                if (s.Marks.Count > 0) Assert.IsTrue(k.Parts.Count + k.Areas.Count > 0, s.RoomId + ": şart çizildi");
                // Geçit kenarı: giriş bir kenarda (sürüm 10: oda döndürülebilir, giriş önceki odanın ileri geçidine simetrik); ileri
                // geçidin iki karesi aynı kenarda açılır (köşedeki kare de)
                var entry = s.Tiles.Where(t => t.Door == 1).ToList();
                int ee = k.EdgeOf(1);
                Assert.IsTrue(ee >= 0 && entry.All(t => ee == 0 ? t.Y == 0 : ee == 1 ? t.X == 7 : ee == 2 ? t.Y == 7 : t.X == 0), s.RoomId + ": giriş kenarı " + ee);
                var forward = s.Tiles.Where(t => t.Door == 2).ToList();
                if (forward.Count == 0) Assert.AreEqual(-1, k.EdgeOf(2), s.RoomId);
                else
                {
                    int e = k.EdgeOf(2);
                    Assert.IsTrue(forward.All(t => e == 0 ? t.Y == 0 : e == 1 ? t.X == 7 : e == 2 ? t.Y == 7 : t.X == 0), s.RoomId + ": ileri geçit kenarı " + e);
                }
            }
        }

        /// <summary>Çizimin <paramref name="layer"/> katmanında y = <paramref name="y"/> üzerinde yatan yatay parçaların toplam boyu.</summary>
        static float EdgeLength(List<List<Vector2>> layer, float y)
        {
            float sum = 0;
            foreach (var path in layer)
                for (int i = 1; i < path.Count; i++)
                    if (Mathf.Abs(path[i - 1].y - y) < 0.01f && Mathf.Abs(path[i].y - y) < 0.01f) sum += Mathf.Abs(path[i].x - path[i - 1].x);
            return sum;
        }

        /// <summary>Kenar satırındaki geçit kareleri o kenarda açılıyorsa sayısı (çerçevenin boşluğu).</summary>
        static int DoorGaps(FacilityPlanSheetData s, FacilityPlanSketch k, int edge, int row) =>
            s.Tiles.Count(t => t.Y == row && t.Door != 0 && k.EdgeOf(t.Door) == edge);

        /// <summary>
        /// Tesis aşama 12 (§11.1, §11.6): Katar'ın ara ve hedef paftasında kuzey ve güney dış çerçevesi yok (Core Baskin.OpenEdgeRows); yerine
        /// şart mürekkebiyle (Parts) her duvarsız kenar karesinde iki kesik parça ve bir tırnak. Giriş paftası tam çerçeveli. Not "Katar",
        /// hover açık kenar satırı; Yalpa paftasında hover yatış takvimi (ara oda 6 tur: 6. tur son tur, yatış yok; hedef 10 tur). Alt bayrak kapalıyken
        /// açık kenar yok.
        /// </summary>
        [Test]
        public void Katar_OpenEdgesDrawnDashed_EntryFramed_YalpaHoverHasTheCalendar()
        {
            const float cell = 22f, w = cell * 8;
            var sim = FacilityDeskKit.New();
            foreach (var region in new[] { "na", "oc" })
                foreach (var room in sim.FacilityPlan.Wing(sim.NextLiberationSector(region).Id)) sim.Facility.Known.Add(room.Id);

            var na = FacilityPlanBuilder.Build(sim, "na");
            Assert.IsNotNull(na);
            for (int i = 0; i < 3; i++)
            {
                var s = na.Sheets[i];
                var k = FacilityPlanSketch.Draw(s, cell);
                var room = sim.FacilityPlan.Room(s.RoomId);
                if (i == 0)
                {
                    Assert.IsEmpty(s.OpenEdges, "giriş vagonu şartsız");
                    Assert.IsFalse(k.IsOpenEdge(0) || k.IsOpenEdge(2));
                    Assert.AreEqual((8 - DoorGaps(s, k, 0, 0)) * cell, EdgeLength(k.Lines, 0), 0.01f, "giriş: kuzey çerçevesi tam");
                    Assert.AreEqual((8 - DoorGaps(s, k, 2, 7)) * cell, EdgeLength(k.Lines, w), 0.01f, "giriş: güney çerçevesi tam");
                    Assert.AreEqual(0f, EdgeLength(k.Parts, 0) + EdgeLength(k.Parts, w), 0.01f, "giriş: kesik yok");
                    StringAssert.DoesNotContain(FacilityConditionText.PlanOpenEdge(sim.B), s.HoverLine);
                    continue;
                }
                Assert.AreEqual("katar", room.Event, s.RoomId);
                CollectionAssert.AreEqual(new[] { 0, 7 }, s.OpenEdges, s.RoomId);
                CollectionAssert.AreEqual(Baskin.OpenEdgeRows(room), s.OpenEdges, s.RoomId);
                Assert.IsTrue(k.IsOpenEdge(0) && k.IsOpenEdge(2) && !k.IsOpenEdge(1) && !k.IsOpenEdge(3), s.RoomId);
                Assert.AreEqual(0f, EdgeLength(k.Lines, 0), 0.01f, s.RoomId + ": kuzey çerçevesi yok");
                Assert.AreEqual(0f, EdgeLength(k.Lines, w), 0.01f, s.RoomId + ": güney çerçevesi yok");
                foreach (var (row, y) in new[] { (0, 0f), (7, w) })
                {
                    int open = s.Tiles.Count(t => t.Y == row && t.Ground != PlanGround.Wall);
                    Assert.Greater(open, 0, s.RoomId);
                    Assert.AreEqual(open * cell * 0.68f, EdgeLength(k.Parts, y), 0.05f, s.RoomId + ": duvarsız her kenar karesinde iki kesik parça");
                    // Tırnak: kenara bakan ok başı, çizim alanının içinde
                    int ticks = k.Parts.Count(p => p.Count == 3 && Mathf.Abs(p[1].y - (row == 0 ? cell * 0.07f : w - cell * 0.07f)) < 0.01f);
                    Assert.AreEqual(open, ticks, s.RoomId + ": tırnaklar");
                }
                Assert.AreEqual(3, k.EdgeOf(1), s.RoomId + ": batı girişi");
                if (s.Tiles.Any(t => t.Door == 2)) Assert.AreEqual(1, k.EdgeOf(2), s.RoomId + ": Katar'da ileri geçit doğuda");
                Assert.AreEqual(Loc.T(Baskin.RegionEvents["katar"].Name), s.Note);
                Assert.IsTrue(s.NoteIsEvent);
                StringAssert.Contains(FacilityConditionText.PlanOpenEdge(sim.B), s.HoverLine, s.RoomId);
                StringAssert.Contains("ekip üyesi 5 vardiya yatar", s.HoverLine);
                foreach (var pt in k.Parts.SelectMany(p => p))
                {
                    Assert.GreaterOrEqual(pt.y, -0.01f); Assert.LessOrEqual(pt.y, w + 0.01f);
                }
            }

            var oc = FacilityPlanBuilder.Build(sim, "oc");
            Assert.IsNotNull(oc);
            Assert.IsEmpty(oc.Sheets.SelectMany(s => s.OpenEdges), "Yalpa'da açık kenar yok");
            StringAssert.DoesNotContain(Loc.T("Yalpa"), oc.Sheets[0].HoverLine ?? "", "giriş odası şartsız");
            Assert.AreEqual("Yalpa", oc.Sheets[1].Note);
            StringAssert.Contains("Yalpa: 2. tur batı, 4. tur doğu · sabitlenmemiş her şey 1 kare kayar", oc.Sheets[1].HoverLine);
            StringAssert.DoesNotContain("6. tur", oc.Sheets[1].HoverLine, "6 turluk ara odada 6. tur son tur: yazılmaz");
            StringAssert.Contains("Yalpa: 2. tur batı, 4. tur doğu, 6. tur batı · sabitlenmemiş her şey 1 kare kayar", oc.Sheets[2].HoverLine);
            foreach (var s in oc.Sheets)
            {
                var k = FacilityPlanSketch.Draw(s, cell);
                Assert.AreEqual((8 - DoorGaps(s, k, 0, 0)) * cell, EdgeLength(k.Lines, 0), 0.01f, s.RoomId + ": kuzey çerçevesi tam");
            }

            // Alt bayrak kapalı: na'nın odaları bugünkü olayla, çerçeve tam
            var bal = FacilityDeskKit.Bal();
            bal.facilityKatar = false;
            var sub = Simulation.NewGame(79, bal);
            foreach (var room in sub.FacilityPlan.Wing(sub.NextLiberationSector("na").Id)) sub.Facility.Known.Add(room.Id);
            foreach (var s in FacilityPlanBuilder.Build(sub, "na").Sheets)
            {
                Assert.IsEmpty(s.OpenEdges, s.RoomId);
                var k = FacilityPlanSketch.Draw(s, cell);
                Assert.AreEqual((8 - DoorGaps(s, k, 0, 0)) * cell, EdgeLength(k.Lines, 0), 0.01f, s.RoomId);
                StringAssert.DoesNotContain(FacilityConditionText.PlanOpenEdge(sub.B), s.HoverLine, s.RoomId);
            }
        }

        [Test]
        public void TitlesAndCaptions_FitTheSheet_InBothLanguages()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            var probe = new GameObject("Ölçü", typeof(RectTransform));
            try
            {
                var plans = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FacilityPlanSheets>(true)).Single();
                var sheet = new SerializedObject(plans).FindProperty("sheets").GetArrayElementAtIndex(0).objectReferenceValue;
                var so = new SerializedObject(sheet);
                var title = (TMP_Text)so.FindProperty("title").objectReferenceValue;
                var caption = (TMP_Text)so.FindProperty("caption").objectReferenceValue;
                var stampWord = (TMP_Text)so.FindProperty("stampWord").objectReferenceValue;
                var drawing = (RectTransform)so.FindProperty("drawing").objectReferenceValue;
                var text = probe.AddComponent<TextMeshProUGUI>();
                text.textWrappingMode = TextWrappingModes.NoWrap;
                float Width(TMP_Text like, string s)
                {
                    text.font = like.font; text.fontSize = like.fontSize; text.characterSpacing = like.characterSpacing;
                    return text.GetPreferredValues(s).x;
                }
                var table = Loc.ParseTable(File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"));
                var sim = FacilityDeskKit.New();
                var rooms = sim.FacilityPlan.Rooms;
                var bad = new List<string>();
                foreach (var lang in Loc.Languages)
                {
                    Loc.SetLanguage(lang, lang == Loc.Turkish ? null : table[lang]);
                    foreach (RoomType type in System.Enum.GetValues(typeof(RoomType)))
                    {
                        string s = Loc.F("PAFTA {0}/3 · {1}", (int)type, FacilityPlanBuilder.TypeWord(type));
                        if (Width(title, s) > title.rectTransform.rect.width + 0.5f) bad.Add(lang + " başlık: " + s);
                    }
                    var lines = rooms.Select(r => r.Name)
                        .Concat(Baskin.RegionEvents.Values.Select(e => Loc.T(e.Name)))
                        .Concat(new[] { "pano", "anten", "vana" }.Select(k => Loc.T(Baskin.ObjNames[k]))).Distinct();
                    foreach (var s in lines)
                        if (Width(caption, s) > caption.rectTransform.rect.width + 0.5f) bad.Add(lang + " antet: " + s + " (" + Width(caption, s) + ")");
                    if (Width(stampWord, Loc.T("PLAN YOK")) + 16 > drawing.rect.width) bad.Add(lang + " damga");
                }
                Assert.IsEmpty(bad, string.Join("\n", bad));
                Assert.GreaterOrEqual(title.fontSize, 14f);
                Assert.GreaterOrEqual(caption.fontSize, 14f);
            }
            finally
            {
                Loc.SetLanguage(Loc.Turkish);
                Object.DestroyImmediate(probe);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Sahne: SiginakHud'da kadro raporunun altında (fiş ve rapor üstte kalır), fişe ve SiginakMap'e bağlı, kapalı.</summary>
        [Test]
        public void DeskGameScene_PlansSitBesideTheSlip()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var map = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakMap>(true)).Single();
                var ms = new SerializedObject(map);
                var plans = (FacilityPlanSheets)ms.FindProperty("plans").objectReferenceValue;
                var slip = (OperationSlip)ms.FindProperty("slip").objectReferenceValue;
                var report = (SquadReportSlip)ms.FindProperty("squadReport").objectReferenceValue;
                Assert.IsNotNull(plans, "SiginakMap.plans bağlı");
                Assert.AreEqual(1, scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FacilityPlanSheets>(true)).Count());
                var so = new SerializedObject(plans);
                Assert.AreSame(slip, so.FindProperty("slip").objectReferenceValue, "paftaların yaslandığı fiş");
                AllReferencesSet(so);
                var sheets = so.FindProperty("sheets");
                Assert.AreEqual(3, sheets.arraySize);
                var hud = slip.transform.parent;
                var tag = hud.GetComponent<HoverTag>();
                for (int i = 0; i < 3; i++)
                {
                    var sheet = (FacilityPlanSheet)sheets.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsNotNull(sheet, "pafta " + i);
                    var sso = new SerializedObject(sheet);
                    AllReferencesSet(sso);
                    var hover = (UiHoverTarget)sso.FindProperty("hover").objectReferenceValue;
                    Assert.AreSame(tag, new SerializedObject(hover).FindProperty("hoverTag").objectReferenceValue, "ortak üzerine gelme etiketi");
                    Assert.IsTrue(((UnityEngine.UI.Image)sso.FindProperty("fill").objectReferenceValue).raycastTarget, "kâğıt ışın hedefi (üzerine gelme)");
                    foreach (var f in new[] { "frameSolid", "frameDashed" })
                        StringAssert.EndsWith(SiginakSpriteManifest.SoftSuffix, ((Sprite)sso.FindProperty(f).objectReferenceValue).name, f);
                    foreach (var tmp in sheet.GetComponentsInChildren<TMP_Text>(true))
                    {
                        Assert.GreaterOrEqual(tmp.fontSize, 14f, tmp.name);
                        Assert.IsFalse(tmp.raycastTarget, tmp.name);
                    }
                }
                Assert.IsFalse(plans.gameObject.activeSelf, "fiş açılana kadar kapalı");
                Assert.AreSame(hud, plans.transform.parent);
                Assert.IsNotNull(plans.GetComponent<Canvas>());
                Assert.IsNotNull(plans.GetComponent<UnityEngine.UI.GraphicRaycaster>());
                Assert.Less(plans.transform.GetSiblingIndex(), report.transform.GetSiblingIndex(), "kadro raporu ve fiş paftaların üstünde");
                Assert.AreEqual(slip.transform.GetSiblingIndex() - 1, report.transform.GetSiblingIndex(), "R6 sırası korunur");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        static void AllReferencesSet(SerializedObject so)
        {
            var it = so.GetIterator();
            it.NextVisible(true);
            while (it.NextVisible(false))
                if (it.propertyType == SerializedPropertyType.ObjectReference) Assert.IsNotNull(it.objectReferenceValue, so.targetObject.name + "." + it.name);
        }

        static string Dump(FacilityPlanData d)
        {
            var sb = new StringBuilder();
            sb.Append(d.RegionId).Append('|').Append(d.SectorId).Append('|').Append(d.Wing).Append('|').Append(d.Front).Append('\n');
            foreach (var s in d.Sheets)
            {
                sb.Append(s.RoomId).Append(s.Index).Append(s.Known).Append(s.Title).Append(s.Caption).Append(s.Note).Append(s.NoteIsEvent)
                    .Append(s.Stamp).Append(s.HoverTitle).Append(s.HoverLine).Append('\n');
                foreach (var t in s.Tiles) sb.Append(t.X).Append(',').Append(t.Y).Append(t.Ground).Append(t.Phase).Append(t.Door).Append(t.Vent).Append(t.Item).Append(';');
                sb.Append('\n');
                foreach (var m in s.Marks) sb.Append(m.Kind).Append(m.X).Append(m.Y).Append(m.ToX).Append(m.ToY).Append(m.Dir).Append(';');
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
