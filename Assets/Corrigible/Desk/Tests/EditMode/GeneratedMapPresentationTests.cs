using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Üretilmiş harita (anahtar 1, <c>Balance.liberationMapGen</c> = 1) sunumu, sabit tohumla (<see cref="SampleMap.Seed"/> = 79):
    /// harita durumundaki ve fişlerdeki adlar <see cref="Simulation.RegionName"/> (aşama 3'ten beri YZ adı; haritanın havuz adı
    /// görünmez), komşu ve aday listeleri haritanın
    /// bağlantıları (<see cref="Simulation.Links"/>). Raporlar, CRT, telsiz defteri ve teleks gerekçeleri aynı adı yazar; fikstürler
    /// (SiginakFixtures) bu haritada da oynanır. Dünya haritasının (anahtar 0) testleri ayrı ve değişmedi.
    /// </summary>
    public class GeneratedMapPresentationTests
    {
        static Simulation NewGenerated(double fon = 100) =>
            LegacySimulation.NewGame(SampleMap.Seed, new Balance { shelterEventsEnabled = false, startFon = fon, liberationMapGen = 1 });

        static void CaptureNext(Simulation sim)
        {
            string id = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id;
            while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step();
            FacilityDeskKit.Raid(sim, id, true);
        }

        /// <summary>Karargâhın üç sektörü alınır, yeni cephe seçilmez.</summary>
        static Simulation HqFreed()
        {
            var sim = NewGenerated();
            while (!sim.LiberationRegion(Simulation.HQ).Completed) CaptureNext(sim);
            return sim;
        }

        /// <summary>Komşu listesinin beklenen hâli: bağlantılardan (A önce), özgür komşular, sırası tabloyla aynı.</summary>
        static List<string> ClearedNeighbors(Simulation sim, string id)
        {
            var cleared = new HashSet<string>(sim.S.Liberation.Regions.Where(r => r.Completed).Select(r => r.Id));
            return sim.Links.Where(l => l.A == id && cleared.Contains(l.B) || l.B == id && cleared.Contains(l.A))
                .Select(l => l.A == id ? l.B : l.A).Distinct().ToList();
        }

        [Test]
        public void MapState_Seed79_NamesComeFromTheGeneratedMap()
        {
            var sim = NewGenerated();
            Assert.IsNotNull(sim.Map, "anahtar 1: harita üretildi");
            Assert.AreEqual(SampleMap.Seed, sim.Map.Seed);
            var s = LiberationMapBuilder.Build(sim);
            CollectionAssert.AreEqual(GameData.Regions.Select(r => r.Id).ToArray(), s.Regions.Select(r => r.Id).ToArray(), "paket sırası");
            foreach (var r in s.Regions)
            {
                Assert.AreEqual(sim.RegionName(r.Id), r.Name, r.Id);
                Assert.AreEqual(GameData.Region(r.Id).Ai, r.Name, r.Id + ": YZ adı");
                Assert.AreNotEqual(GameData.Region(r.Id).Name, r.Name, r.Id + ": dünya adı değil");
                Assert.AreEqual(DeskText.Upper(sim.RegionName(r.Id)), LiberationMapBuilder.Slip(sim, r.Id).Region, r.Id + ": fiş başlığı");
            }
            CollectionAssert.AllItemsAreUnique(s.Regions.Select(r => r.Name));
            Assert.AreEqual(Simulation.HQ, s.Front);
            Assert.AreEqual(DeskText.Upper(sim.RegionName(Simulation.HQ)), LiberationMapBuilder.SquadReport(sim, Simulation.HQ).Region);
        }

        /// <summary>Cephe özgür: adaylar haritanın bağlantılarından; yeni cephe fişinin "Komşu:" satırı haritanın komşuları ve adları.</summary>
        [Test]
        public void Candidates_AndFrontSlipNeighbors_ComeFromTheMapLinks()
        {
            var sim = HqFreed();
            Assert.AreNotSame(GameData.Links, sim.Links);
            var s = LiberationMapBuilder.Build(sim);
            var candidates = s.Regions.Where(r => r.Kind == MapRegionKind.Candidate).Select(r => r.Id).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(sim.Map.Region(Simulation.HQ).Neighbors.OrderBy(x => x).ToArray(), candidates, "adaylar karargâhın harita komşuları");
            CollectionAssert.AreEqual(sim.LiberationFrontier.OrderBy(x => x).ToArray(), candidates);
            foreach (string id in candidates)
            {
                var d = LiberationMapBuilder.Front(sim, id);
                var expected = ClearedNeighbors(sim, id);
                CollectionAssert.AreEqual(new[] { Simulation.HQ }, expected, id + ": tek özgür komşu karargâh");
                Assert.AreEqual(Loc.F("Komşu: {0}", string.Join(", ", expected.Select(sim.RegionName))), d.Neighbor, id);
                Assert.AreEqual(DeskText.Upper(sim.RegionName(id)), d.Region, id);
                Assert.IsTrue(d.ActionEnabled, id);
            }
            // Komşu olmayan bölge: teleks gerekçesi haritanın adıyla
            string far = GameData.Regions.Select(r => r.Id).First(id => id != Simulation.HQ && !sim.Map.Adjacent(id, Simulation.HQ));
            StringAssert.StartsWith(sim.RegionName(far), TelexReasons.Front(sim, far));
            Assert.AreEqual(Loc.F("{0}: özgür bölgeye komşu değil", sim.RegionName(far)), TelexReasons.Drop(sim, far));

            // İkinci cephe özgür: komşu listesi iki özgür bölgeye bakar (tablo sırası)
            string second = candidates[0];
            Assert.IsTrue(sim.SelectLiberationRegion(second).Ok);
            while (!sim.LiberationRegion(second).Completed) CaptureNext(sim);
            foreach (string id in sim.LiberationFrontier)
                Assert.AreEqual(Loc.F("Komşu: {0}", string.Join(", ", ClearedNeighbors(sim, id).Select(sim.RegionName))),
                    LiberationMapBuilder.Front(sim, id).Neighbor, id);
        }

        /// <summary>CRT'nin ve telsiz defterinin kısa adı haritanın adından; vardiya raporu karşı saldırının bölgesini haritanın adıyla iki dilde yazar.</summary>
        [Test]
        public void Fixtures_ShortNames_AndShiftReport_UseTheMapNames()
        {
            // Anlık karşı saldırı savunma alt bayrağı kapalıyken (açıkken eşik kuşatma kurar)
            var sim = LegacySimulation.NewGame(SampleMap.Seed, new Balance
                { shelterEventsEnabled = false, startFon = 60, liberationMapGen = 1, facilityDefenseEnabled = false });
            SiginakFixtures.ApplyV014(sim);
            string front = sim.S.Liberation.CurrentRegion;
            Assert.AreNotEqual(Simulation.HQ, front);
            Assert.IsTrue(sim.Map.Adjacent(front, Simulation.HQ), "fikstürün ikinci cephesi karargâhın komşusu");
            Assert.AreEqual(1, sim.LiberationRegion(front).CapturedCount);
            Assert.AreEqual(Loc.F("{0} / {1:00}", CrtViewBuilder.ShortRegion(sim, front), 2), CrtViewBuilder.Screen(sim, false).FrontLine);
            StringAssert.StartsWith(CrtViewBuilder.ShortRegion(sim, front).Split(' ')[0], DeskText.Upper(sim.RegionName(front)));
            string team = LiberationMapBuilder.TeamName(sim.S.Teams[0]);
            var radio = RadioLogBuilder.Entries(sim, false);
            Assert.IsTrue(radio.Any(e => e.text == Loc.F("{0} hazır · cephe {1}", team, RadioLogBuilder.Short(sim, front))), string.Join(" | ", radio.Select(e => e.text)));

            var before = ShiftReportBuilder.Capture(sim);
            var ledger = ShiftReportBuilder.Ledger(sim);
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.B.liberationAlarmPerShift;
            sim.Step();
            Assert.AreEqual(front + "_1", sim.S.LastCounterattack, "karşı saldırı son alınan sektörü geri alır");
            var input = ShiftReportBuilder.Input(sim, before, ledger, Enumerable.Empty<TelexLine>());
            Assert.AreEqual(sim.RegionSourceName(front), input.after.lastCounterattackRegion);
            string sector = sim.LiberationSector(front + "_1").Name;
            Assert.AreEqual(Loc.F("{0} yeniden işgal altında.", Loc.F("{0} · {1:00} {2}", sim.RegionName(front), 1, sector)),
                ShiftReportBuilder.Build(input).CounterText);
            var table = Loc.ParseTable(File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"));
            try
            {
                Loc.SetLanguage(Loc.English, table[Loc.English]);
                // Kayıtlı rapor sonradan, başka dilde kurulur: ad kaynağından (YZ adı iki dilde aynı)
                StringAssert.StartsWith(sim.RegionName(front) + " · 01", ShiftReportBuilder.Build(input).CounterText);
                Assert.AreEqual(sim.RegionSourceName(front), sim.RegionName(front), "YZ adı çevrilmez");
            }
            finally { Loc.SetLanguage(Loc.Turkish); }
        }

        /// <summary>V.031 ve V.019 fikstürleri üretilmiş haritada sınırı izler: dünya haritasının sırası yoksa sınırdaki ilk bölge.</summary>
        [Test]
        public void Fixtures_V031_AndV019_FollowTheFrontier()
        {
            var sim = NewGenerated();
            SiginakFixtures.ApplyV031(sim);
            Assert.AreEqual(5, sim.S.Liberation.Regions.Count(r => r.Completed));
            Assert.AreEqual(30, sim.S.Day);
            var front = sim.LiberationRegion(sim.S.Liberation.CurrentRegion);
            Assert.IsFalse(front.Completed);
            Assert.AreEqual(1, front.CapturedCount);

            var v019 = NewGenerated(60);
            SiginakFixtures.ApplyV014(v019);
            SiginakFixtures.ApplyShelterV014(v019);   // Load("V014") gibi: alarm 4
            string second = v019.S.Liberation.CurrentRegion;
            SiginakFixtures.ApplyV019(v019);
            Assert.IsTrue(v019.LiberationRegion(second).Completed);
        }

        /// <summary>Bütün harekât üretilmiş haritada (sınırın ilk adayı): son raporun anları haritanın adlarıyla; karşı saldırı anı iki dilde tanınır.</summary>
        [Test]
        public void FinalReport_UsesTheMapNames_AndRecognisesTheCounterattackInBothLanguages()
        {
            var sim = NewGenerated();
            for (int i = 0; i < GameData.Regions.Length; i++)
            {
                while (!sim.LiberationRegion(sim.S.Liberation.CurrentRegion).Completed) CaptureNext(sim);
                if (i < GameData.Regions.Length - 1) Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            }
            Assert.AreEqual("silme", sim.S.Ending);
            var over = FinalReportBuilder.Build(sim);
            var first = sim.S.RaidLog.First(l => l.Success && l.LiberationReward?.RegionCompleted == true);
            Assert.AreEqual(Loc.F("{0} özgür, ilk bölge", sim.RegionName(first.Region)), over.Moments[0].Text);
            Assert.AreEqual(Loc.F("{0} özgür, son sunucu düştü", sim.RegionName(sim.S.RaidLog.Last().Region)), over.Moments.Last().Text);

            var lost = NewGenerated();
            CaptureNext(lost);
            lost.S.Alarm = lost.B.liberationAlarmThreshold;
            lost.Step();
            lost.S.Trust = 1;
            lost.S.Alarm = lost.B.liberationAlarmThreshold;
            lost.Step();
            Assert.AreEqual("dagilma", lost.S.Ending);
            string expected = Loc.F("YZ karşı saldırısı: {0} {1:00} yeniden işgal", lost.RegionName(Simulation.HQ), 1);
            Assert.AreEqual(1, FinalReportBuilder.Build(lost).Moments.Count(m => m.Text == expected),
                string.Join(" | ", FinalReportBuilder.Build(lost).Moments.Select(m => m.Text)));
            var lang = DeskLoc.Instance.Lang;
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.En);
                string english = Loc.F("YZ karşı saldırısı: {0} {1:00} yeniden işgal", lost.RegionName(Simulation.HQ), 1);
                var moments = FinalReportBuilder.Build(lost).Moments;
                Assert.AreEqual(1, moments.Count(m => m.Text == english), "an Türkçe yazıldı; dil değişince de tanınır: " + string.Join(" | ", moments.Select(m => m.Text)));
            }
            finally { DeskLoc.Instance.SetLanguage(lang); }
        }

        /// <summary>
        /// Telsiz defterinin ve CRT'nin kısa adları harita başına tekil (havuzda “Balıkçı Kıyısı” / “Buzul Kıyısı”, “Sand Shore” /
        /// “Stony Shore” aynı kısaltmaya düşer; çakışınca ad tam yazılır): 300 tohumda iki dilde. Dünya haritası da tekil.
        /// </summary>
        [Test]
        public void ShortNames_AreUniquePerMap_InBothLanguages()
        {
            var lang = DeskLoc.Instance.Lang;
            try
            {
                foreach (var l in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(l);
                    var world = LegacySimulation.NewGame(1, new Balance { shelterEventsEnabled = false });
                    CollectionAssert.AllItemsAreUnique(GameData.Regions.Select(r => RadioLogBuilder.Short(world, r.Id)), l + " dünya telsiz");
                    CollectionAssert.AllItemsAreUnique(GameData.Regions.Select(r => CrtViewBuilder.ShortRegion(world, r.Id)), l + " dünya CRT");
                    for (uint seed = 1; seed <= 300; seed++)
                    {
                        var sim = LegacySimulation.NewGame(seed, new Balance { shelterEventsEnabled = false, liberationMapGen = 1 });
                        var radio = GameData.Regions.Select(r => RadioLogBuilder.Short(sim, r.Id)).ToList();
                        var crt = GameData.Regions.Select(r => CrtViewBuilder.ShortRegion(sim, r.Id)).ToList();
                        CollectionAssert.AllItemsAreUnique(radio, l + " tohum " + seed + ": " + string.Join(", ", radio));
                        CollectionAssert.AllItemsAreUnique(crt, l + " tohum " + seed + ": " + string.Join(", ", crt));
                    }
                }
            }
            finally { DeskLoc.Instance.SetLanguage(lang); }
        }

        /// <summary>Harekâtsız örnekler (editör araçları): iki anahtarda da paketin YZ adı; harekât varsa onunki.</summary>
        [Test]
        public void SampleMap_IsLockedToTheSampleSeed()
        {
            foreach (var r in GameData.Regions)
            {
                Assert.AreEqual(r.Ai, SampleMap.RegionName(null, r.Id, true), r.Id);
                Assert.AreEqual(r.Ai, SampleMap.RegionName(null, r.Id, false), r.Id);
            }
            var sim = NewGenerated();
            Assert.AreEqual(sim.RegionName("cn"), SampleMap.RegionName(sim, "cn", false), "harekât varsa haritası");
            Assert.AreEqual(sim.RegionName("cn"), SampleMap.RegionName(null, "cn", true), "örnek tohum harekâtınkiyle aynı");
        }
    }
}
