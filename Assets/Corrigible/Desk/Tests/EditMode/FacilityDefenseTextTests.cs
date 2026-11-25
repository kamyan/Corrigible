using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 11, parça P4a (aşama 11 tasarımı §8.1–8.6, §8.10): bekleyen karşı saldırının (kuşatma) ve savunmanın masa yüzeyleri, saf
    /// kurucularla. Görev fişinin kuşatma kipi (alanlar, SAVUN ve BIRAK, gerekçeler, kadro raporu), harita durumu, karar vardiyasında cephe
    /// fişinin notu, CRT, telsiz günlüğü, ön kontrol, devir fişinin uyarısı, savunma emri ve bırakmanın teleksi, pano kilidi, operasyon
    /// raporu ve sonuç teleksi (tutuldu, düştü; terk 0/1/2), vardiya raporu kutusu (kartsız: kuruldu, kendiliğinden ve komutla bırakıldı),
    /// TR/EN (EN'de Türkçe harf yok). Savunma alt bayrağı kapalıyken hiçbir yüzey metin üretmez. Kurulum
    /// <see cref="FacilitySiegeDeskKit"/>; sayılar Core okuyucularından.
    /// </summary>
    public sealed class FacilityDefenseTextTests
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

        static bool HasTurkish(string s) => s != null && s.Any(c => "çğışöüÇĞİŞÖÜ".IndexOf(c) >= 0);

        static void NoTurkish(string where, params string[] texts)
        {
            foreach (var t in texts) Assert.IsFalse(HasTurkish(t), where + ": " + t);
        }

        // ================================================================== savunma alt bayrağı kapalı

        [Test]
        public void FlagOff_InstantCounterattack_NoSiegeSurface()
        {
            // Savunma alt bayrağı kapalı: anlık karşı saldırı, kuşatma yüzeyi yok
            var b = FacilitySiegeDeskKit.Bal();
            b.facilityDefenseEnabled = false;
            var sim = Simulation.NewGame(79, b);
            var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsTrue(sim.StartLiberationRaid(sector.Id).Ok);
            FacilityDeskKit.WinRaid(sim);
            FacilitySiegeDeskKit.Close(sim);
            const string where = "alt bayrak kapalı";
            Assert.IsFalse(FacilityDefenseText.On(sim), where);
            Assert.AreEqual(1, sim.S.Counterattacks, where + ": anlık karşı saldırı");
            Assert.IsNull(sim.PendingSiege, where);
            Assert.IsNull(FacilityDefenseText.SlipSector(sim, sector.Region), where);
            Assert.IsNull(FacilityDefenseText.CrtHint(sim), where);
            Assert.IsNull(FacilityDefenseText.PreCheck(sim), where);
            Assert.IsNull(FacilityDefenseText.RadioToday(sim), where);
            Assert.IsEmpty(FacilityDefenseText.RadioLines(sim, s => s.Name).Where(x => x.Text.Contains("Karşı saldırı")), where);
            var slip = LiberationMapBuilder.Slip(sim, sector.Region);
            Assert.IsFalse(slip.Siege, where);
            Assert.IsFalse(slip.HasYield, where);
            Assert.IsFalse(slip.Sectors.Any(s => s.Siege), where);
            Assert.IsTrue(LiberationMapBuilder.Build(sim).Regions.All(r => r.Siege == -1 && !r.SiegeDue), where);
            var facts = HandoverSlipFactsBuilder.Build(sim);
            Assert.IsFalse(facts != null && facts.warnings.Any(w => w.kind == SlipWarningKind.Siege), where);
        }

        // ================================================================== görev fişi, harita

        [Test]
        public void SiegeSlip_ModeFieldsButtons_MapMark_TrEn()
        {
            foreach (bool english in new[] { false, true })
            {
                if (english) UseEnglish(); else Loc.SetLanguage(Loc.Turkish);
                var (sim, sector) = FacilitySiegeDeskKit.NewSieged();
                string lang = english ? "EN" : "TR";
                Assert.AreSame(sector, FacilityDefenseText.SlipSector(sim, sector.Region));
                var d = LiberationMapBuilder.Slip(sim, sector.Region);
                Assert.IsTrue(d.Siege, lang);
                StringAssert.Contains(FacilityDefenseText.SlipCode(sector), d.Code, lang);
                Assert.AreEqual(FacilityDefenseText.TaskHead(sector), d.TaskHead);
                StringAssert.Contains(sim.B.facilityDefenseTurns.ToString(), d.Task);
                Assert.AreEqual(FacilityDefenseText.Cost, d.Cost);
                Assert.IsEmpty(d.Rewards, "savunmanın ödülü yok");
                Assert.LessOrEqual(d.Fields.Count, LiberationMapBuilder.SlipFieldsMax, "beş yuva");
                var labels = d.Fields.Select(f => f.Label).ToList();
                CollectionAssert.IsSubsetOf(new[] { Loc.T("DALGA"), Loc.T("İZ"), Loc.T("RÖLE") }, labels, lang);
                var wave = d.Fields.Single(f => f.Label == Loc.T("DALGA"));
                Assert.AreEqual(Loc.F("{0} dalga · {1} makine", sim.B.facilityDefenseWaves, sim.DefenseWaveSize(sim.Tier)), wave.Value);
                var relay = d.Fields.Single(f => f.Label == Loc.T("RÖLE"));
                StringAssert.StartsWith(Loc.F("{0} can", sim.DefenseRelayHpFor(sim.Facility.Perks)), relay.Value);
                var marks = d.Fields.Single(f => f.Label == Loc.T("İZ"));
                Assert.AreEqual(Loc.F("{0} iz", sim.WingMarks(sector.Id).Count), marks.Value);
                Assert.IsTrue(d.Sectors.Single(s => s.Siege).Highlight, "kuşatılan satır seçili");
                Assert.AreEqual(1, d.Sectors.Count(s => s.Siege));
                // SAVUN: Core denetimiyle; BIRAK açık
                Assert.AreEqual(FacilityDefenseText.DefendAction, d.Action);
                Assert.AreEqual(sim.DefendSectorCheck() == null ? SlipActionState.Ready : SlipActionState.Disabled, d.ActionState, d.Reason);
                Assert.IsTrue(d.HasYield);
                Assert.IsTrue(d.YieldEnabled, d.YieldReason);
                Assert.AreEqual(FacilityDefenseText.YieldAction, d.Yield);
                Assert.AreEqual(FacilityDefenseText.YieldCost(sim), d.YieldCost);
                // Kadro raporu savunma emrini verir
                var report = LiberationMapBuilder.SquadReport(sim, sector.Region);
                Assert.IsTrue(report.Siege);
                Assert.AreEqual(FacilityDefenseText.ReportAction, report.Action);
                // Harita: işaret ve karar vardiyası
                var region = LiberationMapBuilder.Build(sim).Find(sector.Region);
                Assert.AreEqual(sector.Index, region.Siege);
                Assert.IsTrue(region.SiegeDue);
                // Kapanış (inceleme bulgusu 7): fişin metni sabit beklenenle (yardımcıyla karşılaştırma yalnız bağı sınar; metin değişince kırılmalı).
                // Varsayılan dengeyle: 10 tur, bırakma güven −1, kademe 1'de 3 dalga × 2 makine.
                Assert.AreEqual(10, sim.DefenseTurns, "varsayılan süre");
                Assert.AreEqual(english
                    ? "Hold the wing for 10 turns in three rooms; guard the relay. Yielding loses it, trust −1."
                    : "Kanadı üç odada savun: 10 tur dayan, röleyi koru. Bırakırsan kanat düşer, güven −1.", d.Task, lang);
                Assert.AreEqual(english ? "no funds · raid slot" : "fon yok · baskın hakkı", d.Cost, lang);
                Assert.AreEqual(english ? "trust −1" : "güven −1", d.YieldCost, lang);
                Assert.AreEqual(english ? "SURE? · trust −1" : "EMİN MİSİN? · güven −1", FacilityDefenseText.YieldConfirm(sim), lang);
                Assert.AreEqual(english ? $"{sim.DefenseWaveCount} waves · {sim.DefenseWaveSize(sim.Tier)} machines"
                    : $"{sim.DefenseWaveCount} dalga · {sim.DefenseWaveSize(sim.Tier)} makine", wave.Value, lang);
                StringAssert.StartsWith(english ? "DEFENCE · " : "SAVUNMA · ", d.TaskHead, lang);
                StringAssert.Contains(english ? "COUNTERATTACK · OP-" : "KARŞI SALDIRI · OP-", d.Code, lang);
                if (english)
                    NoTurkish(lang, d.Code.Replace(sim.RegionName(sector.Region), ""), d.Task, d.Cost, d.Action, d.Yield, d.YieldCost, report.Action,
                        FacilityDefenseText.YieldConfirm(sim), wave.Label, wave.Value, wave.HoverLine, relay.Label, relay.Value, relay.HoverLine, marks.Label,
                        marks.Value, FacilityDefenseText.SectorStamp, FacilityDefenseText.PendingStatus, FacilityDefenseText.DecideFirst);
            }
        }

        [Test]
        public void DecisionShift_FrontSlipLocked_WithCoreReason()
        {
            var (sim, sector) = FacilitySiegeDeskKit.NewSieged();
            // Cephe hâlâ aynı bölgeyse kuşatma kipi açılır; bölgenin sıradaki sektörünün baskın emri Core'da kilitli
            var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsNotNull(next);
            Assert.AreEqual(Loc.T("Önce karşı saldırıya karar ver: savun ya da bırak."), sim.LiberationRaidCheck(next.Id));
            // Kuşatma fişi değilken (bölge başka) cephe fişinin notu; burada kuşatılan bölge cephedir: kuşatma fişi
            Assert.IsTrue(LiberationMapBuilder.Slip(sim, sector.Region).Siege);
            // Kuşatma yokmuş gibi cephe fişi (savunma kapalı değil; kuşatma kipini atlayıp cephe fişinin notunu sınamak için alt fonksiyon yok):
            // karar verilince (bırak) kuşatma kipi kalkar, fiş baskın emrini Core gerekçesiyle kapalı gösterir
            Assert.IsTrue(sim.YieldSector().Ok);
            var front = LiberationMapBuilder.Slip(sim, sector.Region);
            Assert.IsFalse(front.Siege);
            Assert.IsFalse(front.HasYield);
            Assert.AreEqual(SlipActionState.Disabled, front.ActionState);
            Assert.AreEqual(Loc.T("Bu vardiya bırakılan kanada baskın verilemez."), front.Reason);
        }

        // ================================================================== komutların teleksi, CRT, telsiz, ön kontrol, devir fişi

        [Test]
        public void DecisionShift_Crt_Radio_PreCheck_HandoverWarning()
        {
            var (sim, sector) = FacilitySiegeDeskKit.NewSieged();
            Assert.AreEqual(Loc.T("KARŞI SALDIRI BEKLİYOR · SAVUN YA DA BIRAK"), CrtViewBuilder.Screen(sim, false).Hint);
            var radio = RadioLogBuilder.Entries(sim, false).Select(e => e.text).ToList();
            Assert.Contains(Loc.T("Karar bekleniyor: savun ya da bırak"), radio);
            Assert.IsTrue(FacilityDefenseText.RadioLines(sim, s => RadioLogBuilder.Short(sim, s.Region)).Any(x => x.Text.StartsWith(Loc.T("Karşı saldırı bekliyor · ").Trim())),
                "kurulma satırı");
            Assert.AreEqual(Loc.T("karşı saldırıya karar verilmedi: bırakılacak"), FacilityDefenseText.PreCheck(sim));
            var facts = HandoverSlipFactsBuilder.Build(sim);
            Assert.IsNotNull(facts);
            Assert.IsTrue(facts.warnings.Any(w => w.kind == SlipWarningKind.Siege), "devir fişinde KARŞI SALDIRI kutusu");
            Assert.AreEqual(sim.B.liberationCounterattackTrust, facts.siegeTrust);
            var lines = HandoverSlipFormat.Format(facts);
            var box = lines.Warnings.Single(w => w.Kind == SlipWarningKind.Siege);
            Assert.AreEqual(Loc.T("KARŞI SALDIRI ·"), box.Prefix);
            Assert.AreEqual(Loc.F("Karar verilmezse kanat bırakılır (güven −{0}).", sim.B.liberationCounterattackTrust), box.Text);
            Assert.AreEqual(Loc.F("Savunma emri: {0} · {1}", sim.RegionName(sector.Region), sector.Name), FacilityDefenseText.DefendDone(sim, sector.Id));
            Assert.AreEqual(Loc.F("Kanat bırakıldı: {0} · {1} · güven −{2}", sim.RegionName(sector.Region), sector.Name, sim.B.liberationCounterattackTrust),
                FacilityDefenseText.YieldDone(sim, sector.Id));
            // Karar verilince ipuçları kalkar
            Assert.IsTrue(sim.YieldSector().Ok);
            Assert.IsNull(FacilityDefenseText.PreCheck(sim));
            Assert.IsNull(FacilityDefenseText.CrtHint(sim));
            Assert.IsFalse(HandoverSlipFactsBuilder.Build(sim).warnings.Any(w => w.kind == SlipWarningKind.Siege));
            Assert.AreEqual(sector.Index, LiberationMapBuilder.Build(sim).Find(sector.Region).Reoccupied, "bırakılan kanat yeniden işgalde (✕)");
            Assert.AreEqual(-1, LiberationMapBuilder.Build(sim).Find(sector.Region).Siege);
        }

        // ================================================================== savunma: emir, pano kilidi, rapor, teleks, telsiz

        [Test]
        public void Defense_HeldAndLost_ReportTelexRadio_Abandon012()
        {
            foreach (bool hold in new[] { true, false })
                for (int abandon = 0; abandon <= 2; abandon++)
                {
                    string where = (hold ? "tutuldu" : "düştü") + " terk " + abandon;
                    var (sim, sector) = FacilitySiegeDeskKit.NewSieged();
                    FacilitySiegeDeskKit.Defend(sim);
                    // Savunma sürerken: fiş SAHADA, BIRAK yok; CRT geçişi "GÖREV: SAVUNMA"; pano kilidi
                    var d = LiberationMapBuilder.Slip(sim, sector.Region);
                    Assert.IsTrue(d.Siege, where);
                    Assert.IsFalse(d.HasYield, where);
                    Assert.AreEqual(SlipActionState.Disabled, d.ActionState, where);
                    StringAssert.EndsWith(Loc.F("GÖREV: {0}", Loc.T("SAVUNMA")), CrtViewBuilder.Transition(sim).Camera, where);
                    Assert.AreEqual(FacilityDefenseText.LockReason, Corrigible.Desk.Bridge.PlanningBoardSnapshotBuilder.LockReason(sim), where);
                    FacilitySiegeDeskKit.PlayDefense(sim, abandon, hold);
                    var log = sim.S.RaidLog.Last();
                    Assert.AreEqual(hold, log.Success, where);
                    Assert.AreEqual(abandon, log.LiberationReward.Abandoned, where);
                    Assert.AreEqual(FacilityDefenseText.LockReason, Corrigible.Desk.Bridge.PlanningBoardSnapshotBuilder.LockReason(sim), where + ": biten savunma");
                    // Operasyon raporu
                    var report = OperationReportBuilder.Build(sim);
                    Assert.IsTrue(report.Defense, where);
                    Assert.AreEqual(hold ? Loc.T("KANAT TUTULDU") : Loc.T("KANAT DÜŞTÜ"), report.Title, where);
                    Assert.AreNotEqual(Loc.T("SEKTÖR GERİ ALINDI"), report.Title);
                    Assert.IsEmpty(report.Gains, where + ": savunmanın kazancı yok");
                    Assert.AreEqual(hold ? Loc.T("TUTULDU") : Loc.T("KAYBEDİLDİ"), report.Results[0].Value, where);
                    var abandonRow = report.Results.FirstOrDefault(r => r.Label == Loc.T("Terk edilen oda"));
                    if (abandon == 0) Assert.IsNull(abandonRow, where);
                    else Assert.AreEqual(Loc.F("{0} · güven −{1}", abandon, abandon * sim.DefenseRoomTrust), abandonRow.Value, where);
                    Assert.LessOrEqual(report.Results.Count, OperationReportData.MaxResults, where);
                    Assert.IsFalse(report.Results.Any(r => r.Label == Loc.T("YZ karşı saldırısı")), where + ": başlık söyler");
                    // Sonuç teleksi
                    var telex = OperationReportBuilder.Telex(sim, log);
                    Assert.AreEqual(hold ? Loc.T("KANAT TUTULDU") : Loc.T("KANAT DÜŞTÜ"), telex.Status, where);
                    Assert.AreEqual(hold ? TelexKind.Done : TelexKind.Failed, telex.Kind, where);
                    StringAssert.Contains(Loc.F("{0} oda terk", abandon), telex.Message, where);
                    StringAssert.StartsWith(Loc.F("{0} · {1}", sim.RegionName(sector.Region), sector.Name), telex.Message, where);
                    // Telsiz
                    string team = LiberationMapBuilder.TeamName(sim.S.Teams.First());
                    var radio = RadioLogBuilder.Entries(sim, false).Select(e => e.text).ToList();
                    // Son dönüş satırı savunmanınki ("alındı" değil)
                    string back = radio.Last(t => t.StartsWith(team + " döndü · "));
                    StringAssert.EndsWith(hold ? " tutuldu" : " düştü", back, where + ": " + string.Join(" | ", radio));
                    // Kanat: tutulunca alınmış, düşünce yeniden işgalde; kuşatma bitti
                    Assert.AreEqual(hold, sector.Captured, where);
                    Assert.IsNull(sim.SiegeSector, where);
                    Assert.IsNull(FacilityDefenseText.SlipSector(sim, sector.Region), where);
                    // Defter: savunma fon hareketi yazmaz, eşleme kaymaz
                    Assert.IsFalse(ShiftReportBuilder.Ledger(sim).Any(e => e.kind == "raidres" && e.id == sector.Id), where);
                    Assert.IsTrue(RaidPaperFacts.MaterialGains(sim, log).All(x => x == 0), where + ": hammadde yok");
                }
        }

        [Test]
        public void Defense_ReportAndTelex_English_NoTurkishLetters()
        {
            UseEnglish();
            var (sim, sector) = FacilitySiegeDeskKit.NewSieged();
            FacilitySiegeDeskKit.Defend(sim);
            FacilitySiegeDeskKit.PlayDefense(sim, 1, true);
            var log = sim.S.RaidLog.Last();
            var report = OperationReportBuilder.Build(sim);
            var telex = OperationReportBuilder.Telex(sim, log);
            Assert.AreEqual("WING HELD", report.Title);
            Assert.AreEqual("WING HELD", telex.Status);
            string place = Loc.F("{0} · {1}", sim.RegionName(sector.Region), sector.Name);
            var pending = FacilitySiegeDeskKit.NewSieged().sim;
            NoTurkish("EN", report.Title, report.ResultStamp, report.Sentence, telex.Message.Replace(place, ""),
                string.Join(" ", report.Results.Where(r => r.Label != sector.Name && !r.Label.Contains("·")).Select(r => r.Label)),
                FacilityDefenseText.AbandonRow(sim, log.LiberationReward).Label, FacilityDefenseText.AbandonRow(sim, log.LiberationReward).Value,
                FacilityDefenseText.LockReason, FacilityDefenseText.PreCheck(pending), FacilityDefenseText.CrtHint(pending), FacilityDefenseText.RadioToday(pending),
                FacilityDefenseText.SlipWarning(1), FacilityDefenseText.Task(sim), FacilityDefenseText.Cost, FacilityDefenseText.PlanAbandonNote,
                FacilityDefenseText.AlarmHover(sim, "10", "1"), FacilityDefenseText.PendingLine(sim, sector.Id).Replace(place, ""),
                FacilityDefenseText.AutoYieldLine(sim, sector.Id).Replace(place, ""));
        }

        // ================================================================== başarısız baskının kurduğu kuşatma (operasyon raporu)

        [Test]
        public void FailedRaidSiege_ReportRow_ReplacesCounterattack()
        {
            var (sim, sector) = FacilitySiegeDeskKit.NewSieged();
            // Kayda girmeyen kurgu girdi: başarısız baskın eşiğe vardırıp kuşatma kurdu (reward.Siege)
            var log = new RaidLogEntry
            {
                Day = sim.S.Day, Region = sector.Region, SectorId = sector.Id, Success = false, MissionKind = sector.MissionKind,
                LiberationReward = new LiberationRaidReward { Siege = sector.Id, Alarm = 1 },
            };
            var report = OperationReportBuilder.Build(sim, log);
            var row = report.Results.Single(r => r.Label == Loc.T("Karşı saldırı bekliyor"));
            Assert.AreEqual(FacilityDefenseText.Place(sim, sector.Id), row.Value);
            Assert.AreEqual(OperationTone.Alert, row.Tone);
            Assert.IsFalse(report.Results.Any(r => r.Label == Loc.T("YZ karşı saldırısı")), "kuşatmada kanat düşmedi");
            Assert.AreEqual(Loc.T("EŞİK AŞILDI"), report.Results.Single(r => r.Label == Loc.T("YZ alarmı")).Value);
            Assert.AreEqual(Loc.T("SEKTÖR İŞGAL ALTINDA"), report.Title, "saldırı baskınının raporu");
        }

        // ================================================================== vardiya raporu kutusu (kartsız)

        static ShiftReportInput Closed()
        {
            var sim = LegacySimulation.NewGame(731, new Balance { shelterEventsEnabled = false, startFon = 100 });
            var before = ShiftReportBuilder.Capture(sim);
            sim.Step();
            return ShiftReportBuilder.Input(sim, before, ShiftReportBuilder.Ledger(sim), Enumerable.Empty<TelexLine>());
        }

        [Test]
        public void ShiftReportBox_PendingAutoAndCommand()
        {
            // Kurulan: kutu "KARŞI SALDIRI BEKLİYOR", teleks satırı, alarm gözü eşikte; karşı saldırı satırı ve harita dizisi yok
            var input = Closed();
            input.siege = "weu_1";
            var data = ShiftReportBuilder.Build(input);
            Assert.AreEqual(Loc.T("KARŞI SALDIRI BEKLİYOR"), data.CounterTitle);
            StringAssert.Contains(Loc.T("sonraki vardiya savun ya da bırak"), data.CounterText);
            Assert.AreEqual(data.CounterText, data.Siege);
            Assert.IsNull(data.Counterattack, "YZ KARŞI SALDIRISI basılmaz");
            Assert.IsFalse(data.CounterMap);
            Assert.IsTrue(data.Alarm.Alert, "eşik");
            Assert.IsTrue(ShiftReportBuilder.Alarmed(input));

            // Kendiliğinden bırakılan: "YZ KARŞI SALDIRISI" + "karar verilmedi"; eşik değil
            input = Closed();
            input.after.counterattacks = input.before.counterattacks + 1;
            input.after.lastCounterattack = "weu_1";
            input.siegeFall = "weu_1";
            input.siegeAuto = true;
            data = ShiftReportBuilder.Build(input);
            Assert.AreEqual(Loc.T("YZ KARŞI SALDIRISI"), data.CounterTitle);
            StringAssert.Contains(Loc.T("karar verilmedi"), data.CounterText);
            StringAssert.Contains(Loc.T("karar verilmedi"), data.Counterattack);
            Assert.IsTrue(data.CounterMap);
            Assert.IsFalse(data.Alarm.Alert, "kendiliğinden bırakma eşik değil");
            Assert.IsTrue(ShiftReportBuilder.Alarmed(input), "kanat düştü: lamba yanar");

            // Komutla bırakılan (vardiya içinde): kutu "Kanat bırakıldı"; teleks ve dizi komutta (burada yok)
            input = Closed();
            input.siegeFall = "weu_1";
            data = ShiftReportBuilder.Build(input);
            Assert.AreEqual(Loc.T("YZ KARŞI SALDIRISI"), data.CounterTitle);
            StringAssert.EndsWith(Loc.T("kanat bırakıldı."), data.CounterText);
            Assert.IsNull(data.Counterattack);
            Assert.IsFalse(data.CounterMap);

            // Savunma alt bayrağı kapalı (kuşatma alanları boş): anlık karşı saldırının kutusu
            input = Closed();
            input.after.counterattacks = input.before.counterattacks + 1;
            input.after.lastCounterattack = "weu_1";
            data = ShiftReportBuilder.Build(input);
            StringAssert.EndsWith(Loc.T("yeniden işgal altında."), data.CounterText);
            Assert.IsTrue(data.Alarm.Alert);
            Assert.IsTrue(data.CounterMap);
            Assert.IsNull(data.Siege);
        }

        // ================================================================== alarm panosu

        [Test]
        public void AlarmHover_SaysDefendOrYield()
        {
            var (sim, _) = FacilitySiegeDeskKit.NewSieged();
            string text = FacilityDefenseText.AlarmHover(sim, "10", "+1");
            StringAssert.Contains(Loc.T("sonraki vardiya savun ya da bırak").Split(' ')[0], text);
            StringAssert.Contains("10", text);
        }
    }
}
