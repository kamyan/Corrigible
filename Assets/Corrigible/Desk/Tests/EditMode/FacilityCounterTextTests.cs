using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 10, parça D (aşama 10 tasarımı §9.1–9.4, §9.6–9.8; K37–K41, K45, K46): önlem defterinin saf metin kurucuları. ÖNLEM
    /// teleksi her sınıf ve kademede TR/EN; görev fişinin ÖNLEM satırı (görünürlük, değer, sığma eşiği, kırmızı, üzerine gelme, YZ gözlemi);
    /// sonuç teleksi (başarı, geri çekilme, bölge özgür; uzunluk); telsiz günlüğü; paftanın kırmızı kalemi; defterin cevap sayfası ve ödül
    /// kâğıdının cevap satırı. Önlem defteri alt bayrağı kapalıyken hiçbiri metin üretmez. Kurulum <see cref="FacilityDeskKit"/>; önlem kademeleri ve sayaçlar
    /// elle yazılır (kayda girmeyen kurulum; öğrenmenin kendisi Core'un <c>FacilityCounterTests</c>'inde).
    /// </summary>
    public sealed class FacilityCounterTextTests
    {
        const string TablePath = "Assets/Corrigible/Resources/Localization/strings.txt";

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void Restore() => Loc.SetLanguage(Loc.Turkish);

        static void UseEnglish()
        {
            var table = Loc.ParseTable(File.ReadAllText(TablePath));
            table.TryGetValue(Loc.English, out var en);
            Loc.SetLanguage(Loc.English, en);
        }

        /// <summary>Tesis açık oyun; önlem kademeleri ve sayaçlar (sınıf sırası) elle.</summary>
        internal static Simulation With(int[] levels, int[] tally = null, Balance bal = null)
        {
            var sim = Simulation.NewGame(79, bal ?? FacilityDeskKit.Bal());
            sim.Facility.Counter.Clear();
            sim.Facility.Counter.AddRange(levels);
            sim.Facility.Tally.Clear();
            sim.Facility.Tally.AddRange(tally ?? new int[6]);
            return sim;
        }

        static int[] Level(string cls, int level)
        {
            var l = new int[6];
            l[FacilityCounters.Index(cls)] = level;
            return l;
        }

        // ================================================================== ÖNLEM teleksi

        [Test]
        public void Telex_EveryClassAndLevel_TrEn_Filled()
        {
            foreach (bool english in new[] { false, true })
            {
                if (english) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                foreach (var cls in FacilityCounters.Classes)
                    for (int level = 1; level <= 2; level++)
                    {
                        var sim = With(Level(cls, level));
                        var (first, second) = FacilityCounterText.Telex(sim, new FacilityCounterEntry { Day = 3, Region = "weu", Class = cls, Level = level });
                        string where = (english ? "EN " : "TR ") + cls + level;
                        Assert.IsFalse(string.IsNullOrEmpty(first), where);
                        Assert.IsFalse(string.IsNullOrEmpty(second), where);
                        StringAssert.Contains(FacilityCounters.Name(cls, level), first, where);
                        StringAssert.DoesNotContain("{", first + second, where);
                        Assert.IsFalse(first.EndsWith("."), where + ": etkinin noktası atılır");
                        if (english) Assert.IsFalse((first + second).Any(c => "çğışöüÇĞİŞÖÜ".IndexOf(c) >= 0), where + ": " + first + " / " + second);
                        string answer = FacilityCounters.AnswerOf(cls);
                        if (answer != null) StringAssert.Contains(FacilityGear.RecipeName(answer), second, where);
                    }
                Assert.AreEqual(english ? "AI COUNTERMEASURE" : "YZ ÖNLEMİ", FacilityCounterText.TelexStatus);
            }
            Loc.SetLanguage(Loc.Turkish);
            var push = With(Level("itme", 1));
            var line = FacilityCounterText.Telex(push, new FacilityCounterEntry { Class = "itme", Level = 1 });
            Assert.AreEqual("Cıvatalı ayak · 1. kademe · İmza makineleri itilmez ve çekilmez", line.First);
            Assert.AreEqual("Kalan YZ'lerde geçerli · cevap: Söküm anahtarı", line.Second);
            Assert.AreEqual("Kalan YZ'lerde geçerli · yayıcıyı kır",
                FacilityCounterText.Telex(With(Level("kopek", 1)), new FacilityCounterEntry { Class = "kopek", Level = 1 }).Second);
            StringAssert.StartsWith("Makine aklı · 2. kademe",
                FacilityCounterText.Telex(With(Level("aygit", 2)), new FacilityCounterEntry { Class = "aygit", Level = 2 }).First);
        }

        [Test]
        public void Telex_SubFlagOff_OrUnknownEntry_Empty()
        {
            var entry = new FacilityCounterEntry { Class = "itme", Level = 1 };
            var sub = FacilityDeskKit.Bal();
            sub.facilityCountermeasures = false;
            Assert.IsNull(FacilityCounterText.Telex(With(Level("itme", 1), null, sub), entry).First, "alt bayrak kapalı");
            var sim = With(Level("itme", 1));
            Assert.IsNull(FacilityCounterText.Telex(sim, null).First);
            Assert.IsNull(FacilityCounterText.Telex(sim, new FacilityCounterEntry { Class = "yok", Level = 1 }).First);
            Assert.IsNull(FacilityCounterText.Telex(sim, new FacilityCounterEntry { Class = "itme", Level = 0 }).First);
        }

        // ================================================================== ÖNLEM satırı

        [Test]
        public void SlipField_OnlyWithCounterOrTally_ValueAlertHover()
        {
            Assert.IsNull(FacilityCounterText.SlipField(With(new int[6])), "önlem ve sayaç yok: satır yok");
            Assert.IsNull(FacilityCounterText.SlipField(FacilityDeskKit.New()), "yeni oyun (boş defter): satır yok");

            // Yalnız sayaç: "yok", kırmızı değil, üzerine gelince YZ gözlemi
            var seen = With(new int[6], new[] { 4, 0, 2, 0, 0, 0 });
            var f = FacilityCounterText.SlipField(seen);
            Assert.IsNotNull(f);
            Assert.AreEqual("ÖNLEM", f.Label);
            Assert.AreEqual("yok", f.Value);
            Assert.IsFalse(f.Alert);
            Assert.AreEqual("ÖNLEM · 0", f.HoverTitle);
            Assert.AreEqual("YZ gözlemi: itme 4/6 · örme 2/6", f.HoverLine);

            // Tek önlem: adı, kırmızı
            f = FacilityCounterText.SlipField(With(new[] { 1, 0, 0, 0, 0, 0 }));
            Assert.AreEqual("Cıvatalı ayak", f.Value);
            Assert.IsTrue(f.Alert, "önlem varken değer kırmızı");
            Assert.AreEqual("Kesici kol · Parazit", FacilityCounterText.SlipField(With(new[] { 0, 0, 1, 0, 0, 1 })).Value, "kısa iki ad sığar");

            // İki uzun ad: sayı (KAZANIM'ın 24 harf eşiği), adlar üzerine gelince; kademe 2'deki sınıfın sayacı gözlemde yok
            var two = With(new[] { 1, 0, 0, 0, 2, 0 }, new[] { 0, 3, 0, 0, 5, 0 });
            f = FacilityCounterText.SlipField(two);
            Assert.AreEqual("2 önlem", f.Value, "\"Cıvatalı ayak · Koku yayıcı\" 27 harf");
            Assert.IsTrue(f.Alert);
            Assert.AreEqual("ÖNLEM · 2", f.HoverTitle);
            var lines = f.HoverLine.Split('\n');
            Assert.AreEqual(3, lines.Length, f.HoverLine);
            Assert.AreEqual("Cıvatalı ayak (1): İmza makineleri itilmez ve çekilmez · cevap: Söküm anahtarı", lines[0]);
            StringAssert.EndsWith("· cevap: yayıcıyı kır", lines[1]);
            Assert.AreEqual("YZ gözlemi: yıkım 3/6", lines[2], "kademe 2'deki köpek sayılmaz");

            // Cevap açık
            two.Facility.Recipes.Add(FacilityGear.AnsSokum);
            StringAssert.EndsWith("· cevap açık: Söküm anahtarı", FacilityCounterText.SlipField(two).HoverLine.Split('\n')[0]);

            // Sığma eşiği: uzun liste yerine sayı
            f = FacilityCounterText.SlipField(With(new[] { 1, 1, 1, 0, 0, 0 }));
            Assert.AreEqual("3 önlem", f.Value);
            Assert.AreEqual(3, f.HoverLine.Split('\n').Length);
        }

        [Test]
        public void SlipField_EnglishFitsAndFlagOff()
        {
            UseEnglish();
            var f = FacilityCounterText.SlipField(With(new[] { 1, 0, 0, 0, 2, 0 }, new[] { 2, 0, 0, 0, 0, 0 }));
            Assert.AreEqual("COUNTER", f.Label);
            Assert.LessOrEqual(f.Value.Length, FacilityCounterText.SlipListMax, f.Value);
            Assert.IsFalse(f.HoverLine.Any(c => "çğışöüÇĞİŞÖÜ".IndexOf(c) >= 0), f.HoverLine);
            Loc.SetLanguage(Loc.Turkish);
            var sub = FacilityDeskKit.Bal();
            sub.facilityCountermeasures = false;
            Assert.IsNull(FacilityCounterText.SlipField(With(new[] { 1, 0, 0, 0, 0, 0 }, null, sub)), "alt bayrak kapalı: satır yok");
        }

        /// <summary>K37: ÖNLEM KAZANIM'dan sonra boş yuvaya; beş yuva doluysa KAZANIM'la tek satır ("KAZANIM 2 · ÖNLEM 1", hover ikisi).</summary>
        [Test]
        public void Slip_CounterRow_AfterPerks_MergesWhenFiveSlotsFull()
        {
            var sim = With(new[] { 1, 0, 0, 0, 0, 0 });
            string region = sim.S.Liberation.CurrentRegion;
            var d = LiberationMapBuilder.Slip(sim, region);
            Assert.AreEqual("ÖNLEM", d.Fields.Last().Label);
            Assert.IsTrue(d.Fields.Last().Alert);
            Assert.LessOrEqual(d.Fields.Count, LiberationMapBuilder.SlipFieldsMax);
            int baseRows = d.Fields.Count - 1;
            sim.Facility.Perks.Add(FacilityGear.Ear);
            sim.Facility.Perks.Add(FacilityGear.Sight);
            d = LiberationMapBuilder.Slip(sim, region);
            Assert.LessOrEqual(d.Fields.Count, LiberationMapBuilder.SlipFieldsMax, "fişin beş SAHA yuvası");
            if (baseRows + 2 > LiberationMapBuilder.SlipFieldsMax)
            {
                var merged = d.Fields.Last();
                Assert.AreEqual("KAZANIM", merged.Label);
                Assert.AreEqual("2 · ÖNLEM 1", merged.Value);
                Assert.IsTrue(merged.Alert);
                Assert.AreEqual("KAZANIM · 2 · ÖNLEM · 1", merged.HoverTitle);
                var lines = merged.HoverLine.Split('\n');
                Assert.AreEqual(FacilityRewardText.PerkList(sim), lines[0], "önce kazanımlar");
                StringAssert.StartsWith("Cıvatalı ayak (1)", lines[1]);
            }
            else
            {
                Assert.AreEqual("KAZANIM", d.Fields[d.Fields.Count - 2].Label, "ÖNLEM KAZANIM'dan sonra");
                Assert.AreEqual("ÖNLEM", d.Fields.Last().Label);
            }
            // Dört taban satırı (bölge, olay, imza, tehdit) olan bölge: birleşme sınanır
            var full = sim.S.Liberation.Regions.Select(r => r.Id).FirstOrDefault(id =>
                LiberationMapBuilder.Slip(With(new int[6]), id).Fields.Count == 4);
            Assert.IsNotNull(full, "dört taban satırlı bölge");
            var four = With(new[] { 1, 0, 0, 0, 0, 0 });
            four.Facility.Perks.Add(FacilityGear.Ear);
            var fd = LiberationMapBuilder.Slip(four, full);
            Assert.AreEqual(LiberationMapBuilder.SlipFieldsMax, fd.Fields.Count);
            Assert.AreEqual("1 · ÖNLEM 1", fd.Fields.Last().Value);
            // Önlemsiz: satır yok
            var plain = FacilityDeskKit.New();
            Assert.IsFalse(LiberationMapBuilder.Slip(plain, plain.S.Liberation.CurrentRegion).Fields.Any(x => x.Label == "ÖNLEM"));
            Assert.IsFalse(LiberationMapBuilder.Slip(plain, plain.S.Liberation.CurrentRegion).Fields.Any(x => x.Alert), "öbür satırlar kırmızı değil");
        }

        // ================================================================== sonuç teleksi

        [Test]
        public void ResultTelex_Success_RegionFree_Retreat_Length()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            FacilityDeskKit.WinRaid(sim);
            var log = sim.S.RaidLog.Last();
            var (status, message, kind) = OperationReportBuilder.Telex(sim, log);
            Assert.AreEqual("SEKTÖR GERİ ALINDI", status);
            Assert.AreEqual(TelexKind.Done, kind);
            var result = RaidPaperFacts.Result(sim, log);
            var sector = sim.LiberationSector(log.SectorId);
            Assert.AreEqual(string.Format("{0} · {1} · {2} tur · ekip 3/3 ayakta", sim.RegionName(log.Region), sector.Name, result.Turns), message);
            Assert.LessOrEqual(message.Length, 80, message);

            // Bölgeyi tamamlayan baskın: "· bölge özgür"
            var free = FacilityDeskKit.New();
            FacilityDeskKit.CompleteRegion(free);
            var last = OperationReportBuilder.Telex(free, free.S.RaidLog.Last());
            StringAssert.EndsWith("· bölge özgür", last.Message);
            Assert.LessOrEqual(last.Message.Length, 90, last.Message);

            // Geri çekilme: "GERİ ÇEKİLDİ" (Failed)
            var back = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(back);
            var B = back.OpenRaidRoom();
            FacilityDeskKit.Calm(B);
            Assert.IsTrue(Baskin.Retreat(B));
            Assert.IsTrue(back.AdvanceRaidRoom(back.RaidRoomReport(B)).Ok);
            Assert.IsTrue(back.RaidRoomsClosed);
            Assert.IsTrue(back.ResolveRaid(back.FacilityRaidResult()).Ok);
            var retreat = OperationReportBuilder.Telex(back, back.S.RaidLog.Last());
            Assert.AreEqual("GERİ ÇEKİLDİ", retreat.Status);
            Assert.AreEqual(TelexKind.Failed, retreat.Kind);
            StringAssert.DoesNotContain("bölge özgür", retreat.Message);

            UseEnglish();
            var en = OperationReportBuilder.Telex(sim, log);
            Assert.AreNotEqual("SEKTÖR GERİ ALINDI", en.Status, "EN durum çevrilir");
            StringAssert.Contains("turns · team 3/3 standing", en.Message);
        }

        // ================================================================== telsiz günlüğü ve pafta

        [Test]
        public void RadioLog_LearnedLine_AfterThatDaysRaid()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            FacilityDeskKit.WinRaid(sim);
            int day = sim.S.RaidLog.Last().Day;
            sim.Facility.Counter.Clear(); sim.Facility.Counter.AddRange(new[] { 1, 0, 0, 0, 0, 0 });
            sim.Facility.Learned.Add(new FacilityCounterEntry { Day = day, Region = "weu", Class = "itme", Level = 1 });
            var lines = FacilityCounterText.RadioLines(sim);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("YZ önlemi · Cıvatalı ayak", lines[0].Text);
            var entries = RadioLogBuilder.Entries(sim, false).Select(e => e.text).ToList();
            int raid = entries.FindIndex(t => t.Contains(" döndü · "));
            int counter = entries.IndexOf("YZ önlemi · Cıvatalı ayak");
            Assert.GreaterOrEqual(counter, 0, string.Join(" | ", entries));
            Assert.Greater(counter, raid, "önlem satırı baskının dönüşünden sonra");
            var sub = FacilityDeskKit.Bal();
            sub.facilityCountermeasures = false;
            var off = With(new[] { 1, 0, 0, 0, 0, 0 }, null, sub);
            off.Facility.Learned.Add(new FacilityCounterEntry { Day = 0, Class = "itme", Level = 1 });
            Assert.IsEmpty(FacilityCounterText.RadioLines(off), "alt bayrak kapalı");
        }

        [Test]
        public void PlanLines_PerKind_SubFlagOff()
        {
            var sim = With(new[] { 0, 1, 0, 0, 1, 0 });
            var lines = FacilityCounterText.PlanLines(sim, true, true);
            CollectionAssert.AreEqual(new[]
            {
                "Önlem (kırmızı kalem): Çift cidar · bu duvar 2 hak ister",
                "Önlem (kırmızı kalem): Koku yayıcı · 3 kare içinde köpek komut alamaz",
            }, lines);
            Assert.IsEmpty(FacilityCounterText.PlanLines(sim, false, false));
            var sub = FacilityDeskKit.Bal();
            sub.facilityCountermeasures = false;
            Assert.IsEmpty(FacilityCounterText.PlanLines(Simulation.NewGame(79, sub), true, true), "önlem defteri alt bayrağı kapalı");
        }

        // ================================================================== defter ve ödül kâğıdı

        /// <summary>Lokum (af) ve Delici (eeu) gerçek server_reward komutlarıyla; yıkım önlemi 1. kademede (kurulum).</summary>
        static Simulation Piercer()
        {
            var sim = FacilityDeskKit.New();
            sim.Facility.Counter.Clear(); sim.Facility.Counter.AddRange(new[] { 0, 1, 0, 0, 0, 0 });
            FacilityDeskKit.Reward(sim, "af");
            return sim;
        }

        [Test]
        public void RewardPaper_AnswerRow_DescAndInForce_ChangedPageLine()
        {
            var sim = Piercer();
            FacilityDeskKit.Reward(sim, "eeu", null);
            var paper = FacilityRewardText.Paper(sim);
            Assert.AreEqual("TARİF · Delici şarj", paper.Rows[0].Name);
            Assert.AreEqual("Çift cidarı 1 hakla açar · önlem yürürlükte", paper.Rows[0].Cost);
            Assert.IsNull(paper.Rows[0].Risk, "temel açık: not yok");
            Assert.IsNull(FacilityRewardText.NewPageLine(sim, "eeu"), "seçilmeden sayfa dönüşmez");
            var r = sim.ChooseServerReward("eeu", FacilityGear.ChoiceRecipe);
            Assert.IsTrue(r.Ok, r.Msg);
            int page = sim.BoardRecipes.FindIndex(x => x.Id == FacilityGear.Charge);
            Assert.AreEqual(string.Format("Defterde TARİF {0:00} değişti: Delici şarj", page + 1), FacilityRewardText.NewPageLine(sim, "eeu"));

            // Önlem yürürlükte değil: yalnız etki; alt bayrak kapalı: aşama 9'un sözcüğü
            var calm = FacilityDeskKit.New();
            FacilityDeskKit.Reward(calm, "eeu", null);
            Assert.AreEqual("Çift cidarı 1 hakla açar.", FacilityRewardText.Paper(calm).Rows[0].Cost);
            var bal = FacilityDeskKit.Bal();
            bal.facilityCountermeasures = false;
            var off = Simulation.NewGame(79, bal);
            FacilityDeskKit.Reward(off, "eeu", null);
            Assert.AreEqual("Cevap tarifi", FacilityRewardText.Paper(off).Rows[0].Cost);
            Assert.AreEqual("Temel tarif açık değil: bu harekâtta etkisiz", FacilityRewardText.Paper(off).Rows[0].Risk);

            // Çift zemberek: birden çok sayfa
            var spring = FacilityDeskKit.New();
            FacilityDeskKit.Reward(spring, "sa");
            FacilityDeskKit.Reward(spring, "sea");
            FacilityDeskKit.Reward(spring, "oc");
            StringAssert.StartsWith("Defterde TARİF ", FacilityRewardText.NewPageLine(spring, "oc"));
            StringAssert.Contains(", ", FacilityRewardText.NewPageLine(spring, "oc"), "taret ve vinç sayfaları");
        }

        [Test]
        public void Book_AnswerPage_SameIndex_NoteSource_BookLine_UseLine()
        {
            var sim = Piercer();
            var before = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), false, false, false);
            int index = before.Recipes.FindIndex(x => x.Id == FacilityGear.Charge);
            Assert.IsNull(before.Recipes[index].Answer);
            FacilityDeskKit.Reward(sim, "eeu");
            var s = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), false, false, false);
            var page = s.Recipes[index];
            Assert.AreEqual(FacilityGear.Charge, page.Id, "aynı kimlik, aynı dizin");
            Assert.AreEqual(FacilityGear.AnsPiercer, page.Answer);
            Assert.AreEqual("Delici şarj", page.Name);
            Assert.AreEqual("Cevap tarifi · Çift cidar önlemine karşı · +1 devre", page.Note);
            Assert.AreEqual(sim.RegionName("af") + " · cevap: " + sim.RegionName("eeu"), page.Source);
            Assert.IsTrue(page.Inputs.Any(i => i.Kind == "devre"), "devre girdisi");
            StringAssert.Contains("1 cevap", s.BookLine);
            Assert.AreEqual("baskında " + FacilityGear.RaidCharges(sim.B, FacilityGear.Charge, sim.Facility.Perks) + " hak · duvar yıkar · çift cidar 1 hak",
                FacilityBoardText.UseLine(sim, FacilityGear.Charge));
            // Söküm: Teknisyen kitinin sayfası (temel baştan açık: kaynak yalnız cevap)
            FacilityDeskKit.Reward(sim, "na");
            s = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState(), false, false, false);
            var kit = s.Recipes.Single(x => x.Id == "kit_tek");
            Assert.AreEqual(FacilityGear.AnsSokum, kit.Answer);
            Assert.AreEqual("cevap: " + sim.RegionName("na"), kit.Source);
            StringAssert.Contains("Cıvatalı ayak önlemine karşı", kit.Note);
        }
    }
}
