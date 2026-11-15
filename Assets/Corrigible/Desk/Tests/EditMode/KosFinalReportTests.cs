using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using UnityEngine;
using K = Corrigible.Desk.Tests.KosDeskKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 6 (sunum · sonlar): son raporunun yeni sonları (düzenli çekilme, tahliye, düşüş, terk) ve zaferin tonları; ölçüt kutusu
    /// ve SİCİL; başarım eşlemesi; gizli bilginin yalnız son raporunda okunması (kaynak taraması ve son yokken kurucuların aynılığı).
    /// </summary>
    public sealed class KosFinalReportTests
    {
        [SetUp] public void Turkish() => DeskLoc.Instance.SetLanguage(DeskLang.Tr);
        [TearDown] public void Back() => DeskLoc.Instance.SetLanguage(DeskLang.Tr);

        static string Everything(FinalReportData d) => string.Join(" | ", new[] { d.Stamp, d.Reason, d.Headline, d.Sub, d.ToneLine, d.CriteriaTitle, d.CriteriaRule }
            .Concat(d.Indicators.SelectMany(i => new[] { i.Label, i.Value, i.Sub }))
            .Concat(d.Moments.Select(m => m.Text))
            .Concat(d.Criteria.SelectMany(c => new[] { c.Label, c.Value, c.Limit, c.Verdict }))
            .Concat(d.Record.SelectMany(r => new[] { r.Title, r.Body })).Where(s => s != null));

        static Simulation AtShiftFive()
        {
            var sim = K.New();
            K.CloseTo(sim, sim.B.lastSwitchMinShift);
            return sim;
        }

        [Test]
        public void Withdrawal_IsNotADefeat_Anywhere([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = AtShiftFive();
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            Assert.AreEqual("cekilme", sim.EndReport.SwitchKind);
            var d = FinalReportBuilder.Build(sim);
            Assert.AreEqual(FinalOutcome.Withdrawn, d.Outcome);
            Assert.IsFalse(d.Won);
            Assert.AreEqual(lang == DeskLang.En ? "WITHDRAWN" : "ÇEKİLDİ", d.Stamp, "sabit beklenen");
            Assert.AreEqual(lang == DeskLang.En ? "WITHDRAWAL" : "ÇEKİLME", d.Reason);
            Assert.AreEqual(Loc.F("Son şalter {0}. vardiyada indi; Sığınak 07 düzenli çekildi.", sim.S.Day + 1), d.Headline);
            Assert.AreEqual(GameData.Ending("salter").Text, d.Sub, "çekilme emri metni");
            Assert.AreEqual("report.withdrawn", FinalReportView.StampInk(d.Outcome), "mavi damga");
            string all = Everything(d);
            Assert.IsFalse(all.Contains(Loc.T("KAYBEDİLDİ")), "düzenli çekilme hiçbir yerde KAYBEDİLDİ almaz: " + all);
            Assert.IsFalse(all.Contains(Loc.T("kişi kaldı")));
            Assert.IsFalse(d.Moments.Any(m => m.Alert), "kırmızı an yok");
            Assert.AreEqual(OperationTone.Plain, d.Indicators[3].Tone, "nüfus kırmızı değil");
            Assert.AreEqual(Loc.T("kişi çıktı"), d.Indicators[3].Sub);
            Assert.AreEqual(Loc.T("Son şalter · çekilme"), d.Moments.Last().Text);
            Assert.AreEqual(Loc.T("ÇEŞİT"), d.CriteriaTitle);
            Assert.AreEqual(4, d.Criteria.Count, "konum, sığınak eksik, açlık, güven");
            Assert.IsFalse(d.Criteria.Any(c => c.Alert || c.Verdict != null), "neden yok");
            Assert.AreEqual(Loc.F("Kurtarılan {0} kişi · {1} özgür bölge bırakıldı.", sim.EndReport.Survivors.Count, sim.EndReport.RegionsFree), d.ToneLine);
            Assert.AreEqual(3, d.Record.Count, "SİCİL üç sütun");
            StringAssert.StartsWith(Loc.F("{0} ({1}): {2}", Loc.T("Kurtarılanlar"), sim.EndReport.Survivors.Count, ""), d.Record[0].Body);
            StringAssert.Contains(Loc.F("Alınan aday: {0}", sim.EndReport.Admitted), d.Record[1].Body);
            StringAssert.Contains(Loc.T("Ortaya çıkmamış sızmacı yok."), d.Record[2].Body);
            Assert.AreEqual(Loc.T("HAREKÂT"), d.MainTab);
            Assert.AreEqual(Loc.T("SİCİL"), d.RecordTab);
        }

        [Test]
        public void Evacuation_ByTrust_SaysEvacuation_NotWithdrawalOrder([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = AtShiftFive();
            sim.S.Trust = sim.B.evacuationTrustBelow - 1;
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            var d = FinalReportBuilder.Build(sim);
            Assert.AreEqual(FinalOutcome.Lost, d.Outcome);
            // Sabit beklenen: paylaşılan anahtar (baskının "TAHLİYE" → EXTRACTED açılır yazısı) yakalansın
            Assert.AreEqual(lang == DeskLang.En ? "EVACUATED" : "TAHLİYE", d.Stamp);
            Assert.AreEqual(Loc.T("GÜVEN DÜŞÜKTÜ"), d.Reason);
            Assert.AreEqual(Loc.F("Son şalter {0}. vardiyada indi; Sığınak 07 tahliye edildi.", sim.S.Day + 1), d.Headline);
            Assert.AreNotEqual(GameData.Ending("salter").Text, d.Sub, "tahliyede çekilme emri metni yazılmaz");
            StringAssert.Contains(Loc.F("güven {0}", sim.S.Trust), d.ToneLine);
            var trust = d.Criteria.Single(c => c.Alert);
            Assert.AreEqual(Loc.T("TAHLİYE NEDENİ"), trust.Verdict);
            Assert.AreEqual(Loc.T("Güven"), trust.Label);
            Assert.AreEqual(Loc.T("Son şalter · tahliye"), d.Moments.Last().Text);
            Assert.IsTrue(d.Moments.Last().Alert);
            Assert.AreEqual(OperationTone.Alert, d.Indicators[3].Tone);
        }

        [Test]
        public void Evacuation_ByLocation_AddsWarningMoment()
        {
            var sim = AtShiftFive();
            K.Expose(sim, "iz", sim.B.exposureThreshold);
            K.Close(sim);   // uyarı kurulur ("konum")
            Assert.IsTrue(sim.Shelter.FallPending);
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            var d = FinalReportBuilder.Build(sim);
            Assert.AreEqual(Loc.T("KONUM ÇÖZÜLMÜŞTÜ"), d.Reason);
            Assert.IsTrue(d.Moments.Any(m => m.Text == Loc.T("KONUM ÇÖZÜLDÜ uyarısı")), string.Join(" | ", d.Moments.Select(m => m.Text)));
            Assert.AreEqual(Loc.T("Şalter indi, tahliye emri damgalandı. Sığınak 07 karardı; çıkanlar konumu çözülmüş sığınaktan karanlığa yürüdü."), d.Sub);
        }

        [Test]
        public void Fall_RealCloses_ReasonMomentsAndExposureSteps([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = K.New();
            K.Close(sim);
            K.Expose(sim, "iz", sim.B.exposureThreshold - 1);
            K.Expose(sim, "sabotaj", 1);
            K.Close(sim);
            K.Close(sim);
            Assert.AreEqual("dusus", sim.S.Ending);
            var d = FinalReportBuilder.Build(sim);
            Assert.AreEqual(Loc.T("KAYBEDİLDİ"), d.Stamp);
            Assert.AreEqual(Loc.T("SIĞINAK DÜŞTÜ"), d.Reason);
            Assert.AreEqual(Loc.T("Konum çözüldü · Sığınak 07 düştü"), d.Moments.Last().Text);
            Assert.IsTrue(d.Moments.Last().Alert);
            Assert.IsTrue(d.Moments.Any(m => m.Text == Loc.T("KONUM ÇÖZÜLDÜ uyarısı")), "zorunlu ek an");
            Assert.AreEqual(Loc.T("NEDEN"), d.CriteriaTitle);
            Assert.AreEqual(Loc.F("İfşa {0}/{1}: {2} görmezden gelinen iz, {3} sabotaj. İçeride {4} kişi kaldı · {5} bölge özgürdü.",
                sim.B.exposureThreshold, sim.B.exposureThreshold, 1, 1, sim.EndReport.Survivors.Count, sim.EndReport.RegionsFree), d.ToneLine);
            var steps = d.Record[2].Body;
            StringAssert.Contains(Loc.T("görmezden gelinen iz"), steps);
            StringAssert.Contains(Loc.F("= {0} / eşik {1}", sim.B.exposureThreshold, sim.B.exposureThreshold), steps);
            Assert.AreEqual(sim.EndReport.ExposureSteps.Sum(x => x.Delta), sim.EndReport.Exposure, "adımların toplamı ifşa");
        }

        [Test]
        public void Abandon_ReasonAndFirstDesertionMoment()
        {
            var sim = AtShiftFive();
            var gone = sim.Board.People.First(p => p.Alive);
            sim.ShelterEvents.Log.Add(new ShelterEventLogEntry { Shift = 2, Kind = "ayrilma", PersonId = gone.Id });
            sim.S.Ending = "terk";
            var d = FinalReportBuilder.Build(sim);
            Assert.AreEqual(Loc.T("SIĞINAK BOŞALDI"), d.Reason);
            Assert.AreEqual(Loc.T("Sığınak boşaldı"), d.Moments.Last().Text);
            Assert.IsTrue(d.Moments.Any(m => m.Text == Loc.F("İlk açlık ayrılması: {0}", gone.Name) && m.Code == Loc.F("V.{0:000}", 3)));
            StringAssert.StartsWith(Loc.F("Açlıktan ayrılan {0} · kalan {1} · sığınak eksik {2}/{3}.", sim.Shelter.Deserted, sim.EndReport.Survivors.Count, sim.Shelter.UnderstaffedStreak, sim.B.abandonUnderstaffedShifts), d.ToneLine);
            // NEDEN kutusu gerçek veriyle: dolgu satırı yok, kalanların sınırı acil eşik
            CollectionAssert.AreEqual(new[] { Loc.T("Açlıktan ayrılan"), Loc.T("Kalan"), Loc.T("Sığınak eksik") }, d.Criteria.Select(c => c.Label).ToArray());
            var kept = d.Criteria[1];
            Assert.AreEqual(DeskFormat.Int(sim.EndReport.Survivors.Count), kept.Value);
            Assert.AreEqual(Loc.F("acil eşik < {0}", sim.B.candidateEmergencyBelow), kept.Limit);
        }

        [Test]
        public void Victory_Clean_ToneLineAndCriteria([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = AtShiftFive();
            sim.Shelter.MinTrust = sim.B.victoryCleanMinTrust;
            sim.S.Ending = "silme";
            var r = sim.EndReport;
            Assert.AreEqual("temiz", r.Tone);
            var d = FinalReportBuilder.Build(sim);
            Assert.AreEqual(FinalOutcome.Won, d.Outcome);
            Assert.AreEqual(Loc.T("TEMİZ"), d.Reason);
            StringAssert.StartsWith(Loc.F("Temiz zafer: {0} kurucu sağ: {1}.", r.Survivors.Count(p => p.Founder), "").TrimEnd('.'), d.ToneLine);
            Assert.AreEqual(Loc.T("TON ÖLÇÜTLERİ"), d.CriteriaTitle);
            Assert.AreEqual(r.Criteria.Count, d.Criteria.Count, "ölçüt satırları EndReport.Criteria ile birebir");
            for (int i = 0; i < r.Criteria.Count; i++) Assert.AreEqual(r.Criteria[i].Met, d.Criteria[i].Met, r.Criteria[i].Id);
            Assert.IsTrue(d.Criteria.All(c => c.Verdict == Loc.T("TUTTU")));

            sim.Shelter.Exposure = sim.B.victoryCleanMaxExposure + 2;
            var costly = FinalReportBuilder.Build(sim);
            Assert.AreEqual(Loc.T("BEDELLİ"), costly.Reason);
            Assert.AreEqual(Loc.F("Bedelli zafer: {0}.", Loc.F("ifşa {0}", sim.Shelter.Exposure)), costly.ToneLine);
            Assert.AreEqual(Loc.T("TUTMADI"), costly.Criteria.Single(c => c.Met == false).Verdict);
        }

        [Test]
        public void EveryEnding_TrEn_BuildsWithoutException()
        {
            foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
            {
                DeskLoc.Instance.SetLanguage(lang);
                foreach (var ending in GameData.Endings)
                {
                    var sim = K.New();
                    sim.S.Ending = ending.Id;
                    var d = FinalReportBuilder.Build(sim);
                    Assert.IsNotNull(d, ending.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(d.Stamp), ending.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(d.Headline), ending.Id);
                    Assert.AreEqual(3, d.Record.Count, ending.Id + ": SİCİL");
                    if (ending.Id == "dusus" || ending.Id == "terk" || ending.Id == "salter" || ending.Id == "silme")
                    {
                        Assert.IsFalse(string.IsNullOrEmpty(d.ToneLine), ending.Id + ": ton satırı");
                        Assert.Greater(d.Criteria.Count, 0, ending.Id + ": ölçüt kutusu");
                    }
                }
            }
        }

        // ---------------------------------------------------------------- aşama 6 düzeltmeleri

        /// <summary>
        /// Şalter raporunun ÇEŞİT satırları Core'un çekimde yazdığı nedenlerle (SwitchReasons) çelişmez: her senaryoda canlı okuyucular
        /// (konum, sayaç, güven) kayıtlı nedenle aynı şeyi söyler ve satırın değeri hükmüyle uyumludur. Core ileride sondan sonra bir
        /// alanı değiştirirse bu test kırılır.
        /// </summary>
        [Test]
        public void Switch_LiveReadersAgreeWithRecordedReasons([Values("yok", "konum", "kadro", "aclik", "guven", "hepsi")] string scenario)
        {
            var sim = AtShiftFive();
            if (scenario == "konum" || scenario == "hepsi") { K.Expose(sim, "iz", sim.B.exposureThreshold); K.Close(sim); }
            if (scenario == "kadro" || scenario == "hepsi") K.Understaffed(sim, 1);
            if (scenario == "aclik" || scenario == "hepsi")
            {
                // Kurulum: kitin dengesi açlıktan ayrılmayı kapatır (desertStreak 0); açık hâli ve aç sığınak
                sim.B.desertStreak = 3;
                sim.Shelter.HungryStreak = sim.B.desertStreak;
                sim.Shelter.Starving = true;
            }
            if (scenario == "guven" || scenario == "hepsi") sim.S.Trust = sim.B.evacuationTrustBelow - 1;
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            var reasons = sim.EndReport.SwitchReasons;
            Assert.AreEqual(reasons.Contains("konum"), sim.LocationCompromised, "konum");
            Assert.AreEqual(reasons.Contains("kadro"), sim.UnderstaffedCounting, "kadro");
            Assert.AreEqual(reasons.Contains("aclik"), sim.DesertionsOngoing, "aclik");
            Assert.AreEqual(reasons.Contains("guven"), sim.S.Trust < sim.B.evacuationTrustBelow, "guven");
            Assert.AreEqual(scenario == "yok", reasons.Count == 0);
            var d = FinalReportBuilder.Build(sim);
            var rows = d.Criteria;
            Assert.AreEqual(reasons.Contains("konum"), rows[0].Alert);
            Assert.AreEqual(reasons.Contains("konum") ? Loc.T("evet") : Loc.T("hayır"), rows[0].Value);
            Assert.AreEqual(reasons.Contains("kadro"), rows[1].Alert);
            Assert.AreEqual(reasons.Contains("kadro"), rows[1].Value != Loc.T("işlemiyor"), "sayaç satırı hükmüyle uyumlu: " + rows[1].Value);
            Assert.AreEqual(reasons.Contains("aclik"), rows[2].Alert);
            Assert.AreEqual(reasons.Contains("aclik"), rows[2].Value != Loc.T("yok"), "açlık satırı hükmüyle uyumlu: " + rows[2].Value);
            Assert.AreEqual(reasons.Contains("guven"), rows[3].Alert);
        }

        /// <summary>
        /// Core'un yazabildiği her tahliye nedeni ve ifşa kaynağı sunumda eşlenmiş (Core'un kaynağı taranır: yeni bir kimlik eklenirse,
        /// ör. "aclik", test kırılır); bilinmeyen kimlik ham gösterilmez, çevrilmiş genel metne düşer.
        /// </summary>
        [Test]
        public void Ids_CoreIdsMapped_UnknownIdsNeverRaw([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            string endings = File.ReadAllText("Assets/Corrigible/Core/Simulation.Endings.cs");
            var body = endings.Substring(endings.IndexOf("public List<string> LastSwitchReasons()"));
            body = body.Substring(0, body.IndexOf("public ActResult PullLastSwitch()"));
            var coreReasons = Regex.Matches(body, @"list\.Add\(""(\w+)""\)").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            CollectionAssert.AreEquivalent(LastSwitchViewBuilder.KnownReasons, coreReasons, "Core'un tahliye nedenleri");
            string events = File.ReadAllText("Assets/Corrigible/Core/Simulation.ShelterEvents.cs");
            var causes = Regex.Matches(events, @"ChangeExposure\([^,]+,\s*""(\w+)""").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            causes.AddRange(new[] { "iz", "ikinci_iz" });   // yoksay kararı olayın türünü (e.Kind) yazar
            CollectionAssert.IsSubsetOf(causes.Distinct().ToList(), FinalReportBuilder.KnownExposureCauses, "Core'un ifşa kaynakları");

            var sim = AtShiftFive();
            foreach (var id in LastSwitchViewBuilder.KnownReasons)
            {
                Assert.AreNotEqual(id, LastSwitchViewBuilder.ReasonShort(sim, id), id);
                Assert.AreNotEqual(id, LastSwitchViewBuilder.ReasonText(sim, id), id);
                Assert.AreNotEqual(LastSwitchViewBuilder.EvacuatedStamp, LastSwitchViewBuilder.ReasonStamp(id), id + ": kendi damgası");
            }
            foreach (var id in FinalReportBuilder.KnownExposureCauses)
            {
                // Eşlenmiş: genel metne düşmez (Türkçede "sabotaj" kimliğiyle aynı yazılır; İngilizcede ham kimlik görünmez)
                Assert.AreNotEqual(Loc.T("başka kaynak"), FinalReportBuilder.Cause(id), id);
                Assert.AreNotEqual(Loc.T("başka kaynak"), ShelterEventSnapshotBuilder.ExposureSource(id), id);
                if (lang == DeskLang.En)
                {
                    Assert.AreNotEqual(id, FinalReportBuilder.Cause(id), id);
                    Assert.AreNotEqual(id, ShelterEventSnapshotBuilder.ExposureSource(id), id);
                }
            }
            foreach (var unknown in new[] { "zzz", "xyz" })
            {
                Assert.AreEqual(Loc.T("başka neden"), LastSwitchViewBuilder.ReasonShort(sim, unknown));
                Assert.AreEqual(Loc.T("Başka bir tahliye nedeni."), LastSwitchViewBuilder.ReasonText(sim, unknown));
                Assert.AreEqual(LastSwitchViewBuilder.EvacuatedStamp, LastSwitchViewBuilder.ReasonStamp(unknown));
                Assert.AreEqual(Loc.T("başka kaynak"), FinalReportBuilder.Cause(unknown));
                Assert.AreEqual(Loc.T("başka kaynak"), ShelterEventSnapshotBuilder.ExposureSource(unknown));
                Assert.AreEqual(Loc.T("başka kaynak"), FinalReportBuilder.Cause(null));
            }
            if (lang == DeskLang.En)
            {
                Assert.AreEqual("other cause", LastSwitchViewBuilder.ReasonShort(sim, "zzz"));
                Assert.AreEqual("other source", FinalReportBuilder.Cause("zzz"));
            }
        }

        /// <summary>
        /// Paylaşılan anahtarlar İngilizcede yanlış metin üretmez (beklenenler sabit dizgi): şalterin adı oyunun adı değil, tahliye
        /// damgası baskının çıkış sözcüğü değil.
        /// </summary>
        [Test]
        public void English_SwitchNameAndEvacuationStamp_NotSharedWithGameTitleOrRaid()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.En);
            Assert.AreEqual("LAST SWITCH", LastSwitchViewBuilder.SwitchName);
            Assert.AreEqual("EVACUATED", LastSwitchViewBuilder.EvacuatedStamp);
            Assert.AreNotEqual(Loc.T("TAHLİYE"), LastSwitchViewBuilder.EvacuatedStamp, "baskının açılır yazısından ayrı");
            Assert.AreNotEqual(Loc.T("SON ŞALTER"), LastSwitchViewBuilder.SwitchName, "oyunun adından ayrı");
            Assert.AreEqual("WITHDRAW · pull", Loc.T("ÇEKİLME · kolu indir"));
            Assert.AreEqual("EVACUATE · pull", Loc.T("TAHLİYE · kolu indir"));
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            Assert.AreEqual("SON ŞALTER", LastSwitchViewBuilder.SwitchName, "Türkçede görünüş aynı");
            Assert.AreEqual("TAHLİYE", LastSwitchViewBuilder.EvacuatedStamp);
        }

        /// <summary>SİCİL'de tek izli gizli kişi İngilizcede tekil ("1 trace"), birden çok izde çoğul.</summary>
        [Test]
        public void Record_HiddenTraceCount_SingularAndPlural()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.En);
            var sim = AtShiftFive();
            var file = sim.Admission.Files.First(f => f.Resident.Alive);
            var id = file.Identity ?? (file.Identity = new AdmissionIdentity());
            typeof(AdmissionIdentity).GetField("Robot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(id, true);
            file.Founder = false;
            file.ClueCount = 1;
            Assert.IsTrue(sim.PullLastSwitch().Ok);
            string body = FinalReportBuilder.Build(sim).Record[2].Body;
            StringAssert.Contains("· 1 trace (", body);
            Assert.IsFalse(body.Contains("1 traces"), body);
            file.ClueCount = 2;
            StringAssert.Contains("· 2 traces (", FinalReportBuilder.Build(sim).Record[2].Body);
        }

        // ---------------------------------------------------------------- başarımlar

        [Test]
        public void Achievements_MapEveryEnding()
        {
            CollectionAssert.AreEqual(new[] { Achievements.Eradication, Achievements.EradicationClean }, Achievements.ForEnding("silme", null, "temiz"));
            CollectionAssert.AreEqual(new[] { Achievements.Eradication }, Achievements.ForEnding("silme", null, "bedelli"));
            CollectionAssert.AreEqual(new[] { Achievements.Eradication }, Achievements.ForEnding("silme"), "eski oda yolu: yalnız kimlik");
            CollectionAssert.AreEqual(new[] { Achievements.Withdrawal }, Achievements.ForEnding("salter", "cekilme"));
            CollectionAssert.AreEqual(new[] { Achievements.Evacuation }, Achievements.ForEnding("salter", "tahliye"));
            CollectionAssert.AreEqual(new[] { Achievements.ShelterFallen }, Achievements.ForEnding("dusus"));
            CollectionAssert.AreEqual(new[] { Achievements.ShelterAbandoned }, Achievements.ForEnding("terk"));
            foreach (var id in new[] { "topluluk", "dagilma", "vuruldu", "sizma", null }) Assert.IsEmpty(Achievements.ForEnding(id), id ?? "null");
            foreach (var e in GameData.Endings) Assert.IsNotNull(Achievements.ForEnding(e.Id), e.Id);
        }

        // ---------------------------------------------------------------- gizlilik

        [Test]
        public void Privacy_NoEnding_NoReport_AndHiddenReadersOnlyInFinalReportBuilder()
        {
            var sim = K.New();
            Assert.IsNull(FinalReportBuilder.Build(sim), "son yokken rapor yok");
            Assert.IsNull(sim.EndReport);
            var allowed = new[] { "FinalReportBuilder.cs" };
            var rx = new Regex(@"\.Identity\b|\.Robot\b|\bEndReport\b|\bVictoryTone\b|\bHiddenInfiltrator\b");
            var offenders = Directory.GetFiles("Assets/Corrigible/Desk/Scripts/Bridge", "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/Corrigible/Desk/Scripts/Runtime", "*.cs", SearchOption.AllDirectories))
                .Where(f => !allowed.Contains(Path.GetFileName(f)))
                .SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, i, l)))
                .Where(x => !x.l.TrimStart().StartsWith("//") && rx.IsMatch(Regex.Replace(x.l, "//.*$", "")))
                .Select(x => Path.GetFileName(x.f) + ":" + (x.i + 1) + " " + x.l.Trim()).ToList();
            Assert.IsEmpty(offenders, "gizli kimlik ve son raporu verisi sunumda yalnız FinalReportBuilder'da:\n" + string.Join("\n", offenders));
        }

        [Test]
        public void Privacy_PresentationReaders_SameForRobotAndHuman_BeforeEnding()
        {
            // Aynı tohum, aynı kararlar; yalnız bir dosyanın gizli kimliği farklı (hata ayıklama kurulumu, yansımayla)
            Simulation Make(bool robot)
            {
                var sim = K.New();
                K.CloseTo(sim, sim.B.lastSwitchMinShift);
                var file = sim.Admission.Files.First(f => f.Resident.Alive);
                var id = file.Identity ?? (file.Identity = new AdmissionIdentity());
                typeof(AdmissionIdentity).GetField("Robot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(id, robot);
                file.Founder = false;
                file.ClueCount = 1;
                K.Expose(sim, "iz", 2, file.PersonId);
                return sim;
            }
            var a = Make(true);
            var b = Make(false);
            string Snap(Simulation s) => string.Join("\n", new[]
            {
                JsonUtility.ToJson(HandoverSlipFactsBuilder.Build(s)),
                ShelterEventSnapshotBuilder.ExposureHint(s), ShelterEventSnapshotBuilder.ExposureLevel(s).ToString(),
                ShelterEventSnapshotBuilder.CrtHint(s) ?? "-",
                string.Join("/", ShelterEventSnapshotBuilder.TelexLines(s, 0, out _).Select(t => t.Status + t.Message)),
                LastSwitchViewBuilder.Lock(s, default).plate ?? "-", LastSwitchViewBuilder.Variant(s).line,
                string.Join("/", LastSwitchViewBuilder.Order(s).Paragraphs), LastSwitchViewBuilder.Order(s).Title,
                FinalReportBuilder.Build(s) == null ? "rapor yok" : "rapor var",
            });
            Assert.AreEqual(Snap(a), Snap(b), "son yokken sunum gizli kimliği ele vermez");

            // Son varken yalnız SİCİL'in ORTAYA ÇIKMAMIŞ bölümü (ve gölge ölçütü) ayrışır
            a.S.Ending = b.S.Ending = "salter";
            var ra = FinalReportBuilder.Build(a);
            var rb = FinalReportBuilder.Build(b);
            Assert.AreEqual(ra.Record[1].Body, rb.Record[1].Body, "KAPI aynı");
            Assert.AreNotEqual(ra.Record[2].Body, rb.Record[2].Body, "ORTAYA ÇIKMAMIŞ yalnız son raporunda");
        }
    }
}
