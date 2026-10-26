using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Sığınak Aşama 8: vardiya raporu iki anlık görüntüden (K6b'deki V.014 → 015 sayıları, işaretler, düzeltme satırları),
    /// YAPILAMADI satırları, Türkçe sayı ekleri, dizinin atlanması, yan dosya biçimi ve sahne bağlantıları.
    /// </summary>
    public class SiginakShiftTests
    {
        static Simulation NewLiberation(double fon = 100) => Simulation.NewGame(null, "manipulator", 731, new Balance { startFon = fon },
            rules: new RuleOptions { LiberationCampaign = true });

        static string N(double v) => DeskFormat.Number(v, 1);
        static string S(double v) => DeskFormat.Signed(v, 1);

        /// <summary>K6 fikstürü: V.014, sığınak PROMPT §7'deki gibi, Doğu Avrupa 02 baskını başarısız; kol iner.</summary>
        static (ShiftReportInput input, ShiftReportData data) K6(IEnumerable<TelexLine> telex = null)
        {
            var sim = NewLiberation();
            SiginakFixtures.ApplyV014(sim);
            SiginakFixtures.ApplyShelterV014(sim);
            SiginakFixtures.ApplyFailedRaid(sim);
            var before = ShiftReportBuilder.Capture(sim);
            var ledger = ShiftReportBuilder.Ledger(sim);
            var failures = ShiftReportBuilder.Failures(telex ?? Enumerable.Empty<TelexLine>(), before.shift);
            sim.Step();
            var input = ShiftReportBuilder.Input(sim, before, ledger, failures);
            input.code = "014/10";
            return (input, ShiftReportBuilder.Build(input));
        }

        static void Row(ReportRow row, string label, string value, ReportTone tone, bool total = false)
        {
            Assert.AreEqual(label, row.Label);
            Assert.AreEqual(value, row.Value, label);
            Assert.AreEqual(tone, row.Tone, label);
            Assert.AreEqual(total, row.Total, label);
        }

        [Test]
        public void Report_V014_MatchesK6b()
        {
            var (input, d) = K6();
            Assert.AreEqual(14, input.before.shift);
            Assert.AreEqual(15, input.after.shift);
            Assert.AreEqual(Loc.F("VARDİYA RAPORU · {0:000}", 14), d.Title);
            Assert.AreEqual(Loc.F("{0:000} → {1:000} · bir gün ilerledi", 14, 15), d.Sub);
            Assert.AreEqual("014/10", d.Code);

            // FON: açılış 62,0 · baskın emri OP-02 −12,0 · vardiya geliri 2 + 0,5 × 1 · kapanış 52,5
            Assert.AreEqual(4, d.Funds.Count);
            Row(d.Funds[0], Loc.T("Açılış"), N(62), ReportTone.Plain);
            Row(d.Funds[1], Loc.F("Baskın emri · OP-{0:00}", 2), S(-12), ReportTone.Bad);
            Row(d.Funds[2], Loc.T("Vardiya geliri"), S(2.5), ReportTone.Good);
            Assert.AreEqual(Loc.F("{0} + {1} × {2} özgür bölge", DeskFormat.Number(2, 0), DeskFormat.Number(0.5, 1), 1), d.Funds[2].Note);
            Row(d.Funds[3], Loc.T("Kapanış"), N(52.5), ReportTone.Plain, true);
            Assert.AreEqual(62, d.Opening, 1e-9);
            Assert.AreEqual(52.5, d.Closing, 1e-9);
            Assert.AreEqual(d.Opening + input.ledger.Sum(e => e.fon) + d.Income, d.Closing, 1e-9, "satırlar kapanışa çıkar");

            // ÜRETİM: toplama +2,0 · tüketim −1,6 · atölye ENERJİSİZ 4/6 ±0 · Erzak 31,0 → 31,4 / 40 +0,4
            Assert.AreEqual(4, d.Production.Count);
            Row(d.Production[0], Loc.T("Erzak toplama"), S(2), ReportTone.Good);
            Assert.AreEqual(Loc.F("yaşam alanı · {0} kişi", 2), d.Production[0].Note);
            Row(d.Production[1], Loc.T("Erzak tüketimi"), S(-1.6), ReportTone.Bad);
            Assert.AreEqual(Loc.F("{0} kişi × {1}", 8, DeskFormat.Number(0.2, 1)), d.Production[1].Note);
            Row(d.Production[2], Loc.T("Malzeme · atölye"), "±0", ReportTone.Neutral);
            Assert.AreEqual(Loc.F("ENERJİSİZ {0}/{1}", 4, 6), d.Production[2].Badge);
            Row(d.Production[3], Loc.F("Erzak {0} → {1} / {2}", N(31), N(31.4), 40), S(0.4), ReportTone.Good, true);

            Assert.AreEqual(Loc.F("→ jeneratöre {0} kişi, atölye döner", 2), d.Analyst);

            // EKİP: Alfa yaralı 2 → 1 gün; revir hasarlı; vardiya 016'da hazır
            Assert.AreEqual(1, d.Teams.Count);
            Assert.AreEqual(Loc.F("{0} · yaralı", Loc.T("Alfa")), d.Teams[0].Label);
            Assert.AreEqual(Loc.F("{0} → {1} gün", 2, 1), d.Teams[0].Value);
            StringAssert.Contains(Loc.T("Revir hasarlı: toparlanma hızlanmadı."), d.Teams[0].Detail);
            StringAssert.Contains(Loc.F("Vardiya {0} baskına hazır.", TrNumber.WithLocative(16)), d.Teams[0].Detail);
            Assert.IsFalse(d.TeamReturned);

            // Sayaç fişleri: +1 · +2,5 · +0,4 · ±0
            Assert.AreEqual(("+1", ReportTone.Good), d.Slips[0]);
            Assert.AreEqual((S(2.5), ReportTone.Good), d.Slips[1]);
            Assert.AreEqual((S(0.4), ReportTone.Good), d.Slips[2]);
            Assert.AreEqual(("±0", ReportTone.Neutral), d.Slips[3]);
            Assert.AreEqual(Loc.F("VARDİYA {0} BAŞLAT", TrNumber.WithAccusative(15, "000", true)), d.Action);
        }

        [Test]
        public void Report_FailedTelexLines_OnlyClosingShift()
        {
            var telex = new[]
            {
                new TelexLine { code = "013/04", status = "YAPILAMADI", message = "dün", kind = TelexKind.Failed },
                new TelexLine { code = "014/07", status = "EMİR UYGULANDI", message = "baskın", kind = TelexKind.Done },
                new TelexLine { code = "014/08", status = "YAPILAMADI", message = "Revir onarımı: malzeme yetersiz (24 gerekli / 19 var)", kind = TelexKind.Failed },
            };
            var (_, d) = K6(telex);
            Assert.AreEqual(1, d.Failures.Count);
            Assert.AreEqual(("014/08", "Revir onarımı: malzeme yetersiz (24 gerekli / 19 var)"), d.Failures[0]);
            Assert.IsNull(d.MoreFailures);

            var many = Enumerable.Range(1, 6).Select(i => new TelexLine { code = $"014/{i:00}", message = "x", kind = TelexKind.Failed });
            (_, d) = K6(many);
            Assert.AreEqual(ShiftReportBuilder.MaxFailures, d.Failures.Count);
            Assert.AreEqual(Loc.F("… ve {0} satır daha", 2), d.MoreFailures);
        }

        static ShiftSnapshot Snap(int shift, double fon, double supplies, int down, int infirmaryLevel = 0, int infirmaryWorkers = 0)
        {
            var s = new ShiftSnapshot
            {
                shift = shift, fon = fon, supplies = supplies, suppliesCapacity = 40, materials = 10, materialsCapacity = 100,
                population = 8, idle = 0, energyCapacity = 8, energyDemand = 4, freeRegions = 2, regions = 12,
            };
            s.rooms.Add(new ShiftRoomSnapshot { id = ShelterLogic.Living, level = 1, workers = 4 });
            s.rooms.Add(new ShiftRoomSnapshot { id = ShelterLogic.Generator, level = 1, workers = 2 });
            s.rooms.Add(new ShiftRoomSnapshot { id = ShelterLogic.Workshop, level = 1, workers = 2 });
            s.rooms.Add(new ShiftRoomSnapshot { id = ShelterLogic.Infirmary, level = infirmaryLevel, workers = infirmaryWorkers });
            s.teams.Add(new ShiftTeamSnapshot { id = "alfa", name = "Alfa", down = down });
            return s;
        }

        static ShiftReportInput Synthetic(ShiftSnapshot before, ShiftSnapshot after, params ShiftLedgerEntry[] ledger) => new ShiftReportInput
        {
            before = before, after = after, ledger = ledger.ToList(), incomeCentral = 2, incomeRegional = 1,
            rates = new ShiftRates
            {
                dailyFunds = 2, regionIncome = 0.5, suppliesPerWorker = 1, suppliesPerResident = 0.2, materialsPerWorker = 0.5,
                starvingMultiplier = 0.5, workerEnergy = 1, workersPerLevel = 2, healingDays = 1, healingWorkers = 1,
            },
        };

        [Test]
        public void Report_Signs_CorrectionRows_AndLedgerLabels()
        {
            // Depo sınırı: 38 + 4 − 1,6 = 40,4 → 40 (−0,4 düzeltme); malzeme +1,0; gelirden başka fon hareketi (“Diğer”)
            var before = Snap(20, 30, 38, 0);
            var after = Snap(21, 34, 40, 0);
            after.materials = 11;
            var input = Synthetic(before, after,
                new ShiftLedgerEntry { kind = "shelter_upgrade", id = ShelterLogic.Infirmary, fon = -12, number = 1 },
                new ShiftLedgerEntry { kind = "shelter_upgrade", id = ShelterLogic.Workshop, fon = -10, number = 2 },
                new ShiftLedgerEntry { kind = "raidres", id = "eeu_1", fon = 22, number = 1 });
            var d = ShiftReportBuilder.Build(input);

            Assert.AreEqual(30 - (-12 - 10 + 22), d.Opening, 1e-9);
            Row(d.Funds[1], Loc.F("Onarım · {0}", ShelterLogic.RoomName(ShelterLogic.Infirmary)), S(-12), ReportTone.Bad);
            Row(d.Funds[2], Loc.F("Geliştirme · {0} SV{1}", ShelterLogic.RoomName(ShelterLogic.Workshop), 2), S(-10), ReportTone.Bad);
            Row(d.Funds[3], Loc.F("Baskın ödülü · OP-{0:00}", 1), S(22), ReportTone.Good);
            Row(d.Funds[4], Loc.T("Vardiya geliri"), S(3), ReportTone.Good);
            Row(d.Funds[5], Loc.T("Diğer"), S(1), ReportTone.Good);
            Row(d.Funds[6], Loc.T("Kapanış"), N(34), ReportTone.Plain, true);

            Row(d.Production[2], Loc.F("Depo sınırı · {0}", 40), S(-0.4), ReportTone.Bad);
            Row(d.Production[3], Loc.T("Malzeme · atölye"), S(1), ReportTone.Good);
            Assert.IsNull(d.Production[3].Badge);
            Assert.AreEqual(Loc.F("{0} kişi × {1}", 2, DeskFormat.Number(0.5, 1)), d.Production[3].Note);
            Row(d.Production[4], Loc.F("Erzak {0} → {1} / {2}", N(38), N(40), 40), S(2), ReportTone.Good, true);
            Assert.AreEqual(("±0", ReportTone.Neutral), (ShiftReportBuilder.Delta(0.04, 1), ShiftReportBuilder.Tone(0.04, 1)), "yuvarlanınca sıfır");
            Assert.AreEqual(ReportTone.Bad, ShiftReportBuilder.Tone(-0.05, 1));
            Assert.IsNull(d.Analyst, "boşta kişi ve yaralı yok: öneri yok");
            Assert.AreEqual(Loc.F("{0} · hazır", Loc.T("Alfa")), d.Teams[0].Label);
        }

        [Test]
        public void Report_TeamRecovery_InfirmaryAndReturn()
        {
            // Revir çalışıyor: 3 → 1 (1 gün hızlı), hazır olma vardiyası 1 / 2 gün = +1
            var d = ShiftReportBuilder.Build(Synthetic(Snap(9, 10, 20, 3, 1, 1), Snap(10, 13, 21.4, 1, 1, 1)));
            StringAssert.Contains(Loc.F("Revir çalışıyor: toparlanma {0} gün hızlandı.", 1), d.Teams[0].Detail);
            StringAssert.Contains(Loc.F("Vardiya {0} baskına hazır.", TrNumber.WithLocative(11)), d.Teams[0].Detail);
            Assert.IsFalse(d.TeamReturned);

            // Revirde kimse yok, ekip yine de toparlandı (1 → 0): neden cümlesi yazılmaz
            d = ShiftReportBuilder.Build(Synthetic(Snap(9, 10, 20, 1, 1, 0), Snap(10, 13, 21.4, 0, 1, 0)));
            Assert.AreEqual(Loc.F("{0} · toparlandı", Loc.T("Alfa")), d.Teams[0].Label);
            Assert.AreEqual(Loc.T("Ekip cepheye döndü."), d.Teams[0].Detail);
            Assert.IsTrue(d.TeamReturned);

            // Revirde kimse yok, hâlâ yaralı (3 → 2): neden yazılır
            d = ShiftReportBuilder.Build(Synthetic(Snap(9, 10, 20, 3, 1, 0), Snap(10, 13, 21.4, 2, 1, 0)));
            StringAssert.Contains(Loc.T("Revirde görevli yok: toparlanma hızlanmadı."), d.Teams[0].Detail);
        }

        [Test]
        public void Ledger_RaidOrderRepairAndReward_FromSimulation()
        {
            var sim = NewLiberation();
            sim.Shelter.Materials = 50;
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Infirmary).Ok);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true, Turns = 5 }).Ok);
            var ledger = ShiftReportBuilder.Ledger(sim);
            Assert.AreEqual(new[] { "shelter_upgrade", "liberation_raid", "raidres" }, ledger.Select(e => e.kind).ToArray());
            Assert.AreEqual(1, ledger[0].number, "revir 0 → 1: onarım");
            Assert.AreEqual(1, ledger[1].number, "OP-01");
            Assert.AreEqual("weu_1", ledger[2].id);
            Assert.AreEqual(1, ledger[2].number);
            Assert.AreEqual(sim.B.liberationLootFunds, ledger[2].fon, 1e-9);
        }

        [Test]
        public void TrNumber_PicksSuffixFromSpokenLastWord()
        {
            Assert.AreEqual("İ", TrNumber.Accusative(15, true));
            Assert.AreEqual("YI", TrNumber.Accusative(16, true));
            Assert.AreEqual("U", TrNumber.Accusative(19, true));
            Assert.AreEqual("Yİ", TrNumber.Accusative(20, true));
            Assert.AreEqual("Ü", TrNumber.Accusative(3, true));
            Assert.AreEqual("da", TrNumber.Locative(16));
            Assert.AreEqual("de", TrNumber.Locative(17));
            Assert.AreEqual("te", TrNumber.Locative(15));
            Assert.AreEqual("ta", TrNumber.Locative(40));
            Assert.AreEqual("da", TrNumber.Locative(10));
            Assert.AreEqual("de", TrNumber.Locative(100));
            var lang = DeskLoc.Instance.Lang;
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.Tr);
                Assert.AreEqual("015'İ", TrNumber.WithAccusative(15, "000", true));
                Assert.AreEqual("016'da", TrNumber.WithLocative(16));
                DeskLoc.Instance.SetLanguage(DeskLang.En);
                Assert.AreEqual("015", TrNumber.WithAccusative(15, "000", true), "İngilizcede ek yok");
            }
            finally { DeskLoc.Instance.SetLanguage(lang); }
        }

        [Test]
        public void Sequence_Skip_FiresRemainingCuesInOrder()
        {
            var go = new GameObject("Sequence");
            try
            {
                var seq = go.AddComponent<ShiftSequence>();
                var fired = new List<(ShiftSequence.Cue cue, bool skipped)>();
                seq.Play((c, s) => fired.Add((c, s)));
                Assert.IsTrue(seq.Playing);
                seq.Skip();
                Assert.IsFalse(seq.Playing);
                Assert.AreEqual(new[] { ShiftSequence.Cue.Counters, ShiftSequence.Cue.Telex, ShiftSequence.Cue.Generator, ShiftSequence.Cue.Camera,
                    ShiftSequence.Cue.Report, ShiftSequence.Cue.End }, fired.Select(f => f.cue).ToArray());
                Assert.IsTrue(fired.All(f => f.skipped));
                Assert.AreEqual(1, ShiftSequence.AmbientScale, "oda ışığı geri geldi");
                seq.Skip();
                Assert.AreEqual(6, fired.Count, "ikinci atlama bir şey yapmaz");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ReportInput_SurvivesJsonRoundTrip()
        {
            var (input, d) = K6(new[] { new TelexLine { code = "014/08", status = "YAPILAMADI", message = "m", kind = TelexKind.Failed } });
            var copy = JsonUtility.FromJson<ShiftReportInput>(JsonUtility.ToJson(input));
            var again = ShiftReportBuilder.Build(copy);
            Assert.AreEqual(d.Funds.Select(r => r.Label + r.Value), again.Funds.Select(r => r.Label + r.Value));
            Assert.AreEqual(d.Production.Select(r => r.Label + r.Value + r.Badge), again.Production.Select(r => r.Label + r.Value + r.Badge));
            Assert.AreEqual(d.Teams[0].Detail, again.Teams[0].Detail);
            Assert.AreEqual(d.Failures, again.Failures);
            Assert.AreEqual(d.Analyst, again.Analyst);
        }

        [Test]
        public void DeskGameScene_ShiftIsWired()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var shift = roots.SelectMany(g => g.GetComponentsInChildren<SiginakShift>(true)).Single();
                var so = new SerializedObject(shift);
                foreach (var f in new[] { "game", "surface", "desk", "rig", "map", "shiftSwitch", "telex", "counters", "sequence", "report", "tray", "view" })
                    Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
                var desk = roots.SelectMany(g => g.GetComponentsInChildren<SiginakDesk>(true)).Single();
                Assert.AreSame(shift, new SerializedObject(desk).FindProperty("shift").objectReferenceValue);

                var report = (ShiftReportView)so.FindProperty("report").objectReferenceValue;
                Assert.IsFalse(report.gameObject.activeSelf, "rapor kapalı başlar");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, report.GetComponentInParent<Canvas>(true).renderMode);
                var view = new SerializedObject(report);
                Assert.AreEqual(8, view.FindProperty("fundsRows").arraySize);
                Assert.AreEqual(6, view.FindProperty("productionRows").arraySize);
                Assert.AreEqual(4, view.FindProperty("failures").arraySize);

                var sequence = (ShiftSequence)so.FindProperty("sequence").objectReferenceValue;
                var amber = (Light)new SerializedObject(sequence).FindProperty("amber").objectReferenceValue;
                Assert.IsNotNull(amber);
                Assert.IsFalse(amber.enabled, "amber ışık yalnız dizide");
                Assert.AreEqual(LightType.Point, amber.type);

                var tray = (EvrakTray)so.FindProperty("tray").objectReferenceValue;
                Assert.IsFalse(((GameObject)new SerializedObject(tray).FindProperty("sheet").objectReferenceValue).activeSelf, "rapor yokken tepsi boş");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
