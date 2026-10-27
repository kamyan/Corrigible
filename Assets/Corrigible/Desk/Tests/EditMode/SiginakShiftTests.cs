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
        static Simulation NewLiberation(double fon = 100) => Simulation.NewGame(731, new Balance { startFon = fon });

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
        public void Report_CounterattackDuringShift_IsReportedAndSentToTelex()
        {
            var sim = NewLiberation();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success = true, Turns = 4 }).Ok);
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.B.liberationAlarmPerShift;
            var before = ShiftReportBuilder.Capture(sim);
            sim.Step();
            var d = ShiftReportBuilder.Build(ShiftReportBuilder.Input(sim, before, ShiftReportBuilder.Ledger(sim), null));
            string lost = Loc.F("{0} · {1}", Loc.T("Batı Avrupa"), Loc.T("Tedarik deposu"));
            Assert.AreEqual(Loc.F("{0} yeniden işgal altında · sivil güven −{1}", lost, sim.B.liberationCounterattackTrust), d.Counterattack);
            Assert.AreEqual(Loc.T("KARŞI SALDIRI"), d.Teams[1].Value);
            Assert.IsTrue(d.Teams[1].HideIcon, "YZ alarmı satırında ekip simgesi yok");
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

            // ÜRETİM: toplama +2,0 · tüketim −1,6 · zırh plakası 0/3 ±0 (döküm yarıda) · Erzak 31,0 → 31,4 / 40 +0,4
            Assert.AreEqual(4, d.Production.Count);
            Row(d.Production[0], Loc.T("Erzak toplama"), S(2), ReportTone.Good);
            Assert.AreEqual(Loc.F("yaşam alanı · {0} kişi", 2), d.Production[0].Note);
            Row(d.Production[1], Loc.T("Erzak tüketimi"), S(-1.6), ReportTone.Bad);
            Assert.AreEqual(Loc.F("{0} kişi × {1}", 8, DeskFormat.Number(0.2, 1)), d.Production[1].Note);
            Row(d.Production[2], Loc.F("Zırh plakası · {0}/{1}", 0, 3), "±0", ReportTone.Neutral);
            Assert.IsNull(d.Production[2].Badge);
            Assert.AreEqual(Loc.F("{0} kişi × {1} · plaka {2} malzeme", 2, DeskFormat.Number(0.25, 2), 4), d.Production[2].Note);
            Assert.AreEqual(0.5, input.after.plateProgress, 1e-9);
            Row(d.Production[3], Loc.F("Erzak {0} → {1} / {2}", N(31), N(31.4), 40), S(0.4), ReportTone.Good, true);

            Assert.AreEqual(Loc.T("→ revir onarılırsa yaralar sarılır"), d.Analyst);

            // EKİP: Teknisyen düştü (3 → 2 gün), yerine yedek çıkar; revir hasarlı
            // Ekiplerin altında YZ alarmı: vardiya +1, karşı saldırı yok
            Assert.AreEqual(2, d.Teams.Count);
            Assert.AreEqual(Loc.T("YZ alarmı"), d.Teams[1].Label);
            Assert.AreEqual(input.before.alarm + 1, input.after.alarm, 1e-9);
            StringAssert.EndsWith("/ " + DeskFormat.Number(input.after.alarmThreshold, 0), d.Teams[1].Value);
            Assert.IsNull(d.Counterattack);
            Assert.AreEqual(Loc.F("{0} · {1} yaralı", Loc.T("Alfa"), 1), d.Teams[0].Label);
            Assert.AreEqual(Loc.T("YEDEKLE HAZIR"), d.Teams[0].Value);
            StringAssert.Contains(Loc.T("Revir hasarlı: yara sarılmadı."), d.Teams[0].Detail);
            StringAssert.Contains(Loc.F("{0} yaralı: {1} gün.", Baskin.UnitTypes["tek"].Name, 2), d.Teams[0].Detail);
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

        /// <summary>Elle kurulan anlık görüntü: uzmanların yarası (T, E, A), Teknisyenin yatacağı gün, revir, baskına çıktı mı.</summary>
        static ShiftSnapshot Snap(int shift, double fon, double supplies, int[] wounds = null, int techDown = 0, int infirmaryLevel = 0,
            int infirmaryWorkers = 0, bool busy = false, bool canRaid = true)
        {
            var s = new ShiftSnapshot
            {
                shift = shift, fon = fon, supplies = supplies, suppliesCapacity = 40, materials = 10, materialsCapacity = 100,
                population = 8, idle = 0, freeRegions = 2, regions = 12, plates = 0, plateCapacity = 1,
            };
            s.rooms.Add(new ShiftRoomSnapshot { id = ShelterLogic.Living, level = 1, workers = 4 });
            s.rooms.Add(new ShiftRoomSnapshot { id = ShelterLogic.Radio, level = 1, workers = 2 });
            s.rooms.Add(new ShiftRoomSnapshot { id = ShelterLogic.Workshop, level = 1, workers = 2 });
            s.rooms.Add(new ShiftRoomSnapshot { id = ShelterLogic.Infirmary, level = infirmaryLevel, workers = infirmaryWorkers });
            var team = new ShiftTeamSnapshot { id = "alfa", name = "Alfa", busy = busy, canRaid = canRaid };
            for (int i = 0; i < Simulation.Roles.Length; i++)
                team.members.Add(new ShiftMemberSnapshot { role = Simulation.Roles[i], wounds = wounds?[i] ?? 0, down = i == 0 ? techDown : 0 });
            s.teams.Add(team);
            return s;
        }

        static ShiftReportInput Synthetic(ShiftSnapshot before, ShiftSnapshot after, params ShiftLedgerEntry[] ledger) => new ShiftReportInput
        {
            before = before, after = after, ledger = ledger.ToList(), incomeCentral = 2, incomeRegional = 1,
            rates = new ShiftRates
            {
                dailyFunds = 2, regionIncome = 0.5, suppliesPerWorker = 1, suppliesPerResident = 0.2, platesPerWorker = 0.25, plateMaterials = 4,
                healPerWorker = 0.34, starvingMultiplier = 0.5, workersPerLevel = 2, healingDays = 1, healingWorkers = 1, restHeal = 1,
            },
        };

        [Test]
        public void Report_Signs_CorrectionRows_AndLedgerLabels()
        {
            // Depo sınırı: 38 + 4 − 1,6 = 40,4 → 40 (−0,4 düzeltme); atölye bir plaka döktü (−4 malzeme); gelirden başka fon hareketi (“Diğer”)
            var before = Snap(20, 30, 38);
            var after = Snap(21, 34, 40);
            after.plates = 1; after.materials = 6;
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
            Row(d.Production[3], Loc.F("Zırh plakası · {0}/{1}", 1, 1), "+1", ReportTone.Good);
            Assert.IsNull(d.Production[3].Badge);
            Assert.AreEqual(Loc.F("{0} kişi × {1} · plaka {2} malzeme", 2, DeskFormat.Number(0.25, 2), 4), d.Production[3].Note);
            Assert.AreEqual(Loc.T("DEPO DOLU"), ShiftReportBuilder.Build(Synthetic(after, Snap(22, 36, 40))).Production.Single(r => r.Badge != null).Badge, "depo doluyken döküm bekler");
            Row(d.Production[4], Loc.F("Erzak {0} → {1} / {2}", N(38), N(40), 40), S(2), ReportTone.Good, true);
            Assert.AreEqual(("±0", ReportTone.Neutral), (ShiftReportBuilder.Delta(0.04, 1), ShiftReportBuilder.Tone(0.04, 1)), "yuvarlanınca sıfır");
            Assert.AreEqual(ReportTone.Bad, ShiftReportBuilder.Tone(-0.05, 1));
            Assert.IsNull(d.Analyst, "boşta kişi ve yaralı yok: öneri yok");
            Assert.AreEqual(Loc.F("{0} · hazır", Loc.T("Alfa")), d.Teams[0].Label);
        }

        [Test]
        public void Report_TeamWounds_RestInfirmaryAndReturn()
        {
            // Baskınsız vardiya: dinlenme her ayaktaki uzmanı bir toparlar (2), kalanını revir sarar (1)
            var d = ShiftReportBuilder.Build(Synthetic(Snap(9, 10, 20, new[] { 2, 1, 0 }, 0, 1, 1), Snap(10, 13, 21.4, new[] { 0, 0, 0 }, 0, 1, 1)));
            Assert.AreEqual(Loc.F("{0} · uzmanlar", Loc.T("Alfa")), d.Teams[0].Label);
            Assert.AreEqual(Loc.F("yara {0} → {1}", 3, 0), d.Teams[0].Value);
            StringAssert.Contains(Loc.F("Dinlenme {0} yara toparladı.", 2), d.Teams[0].Detail);
            StringAssert.Contains(Loc.F("Revir {0} yara sardı.", 1), d.Teams[0].Detail);
            Assert.IsNull(d.Analyst, "yara kalmadı, boşta kişi yok");

            // Baskın vardiyası, revirde kimse yok: yara kalır, neden yazılır
            d = ShiftReportBuilder.Build(Synthetic(Snap(9, 10, 20, new[] { 1, 0, 0 }, 0, 1, 0, true), Snap(10, 13, 21.4, new[] { 1, 0, 0 }, 0, 1, 0)));
            Assert.AreEqual(Loc.F("yara {0} → {1}", 1, 1), d.Teams[0].Value);
            StringAssert.Contains(Loc.T("Revirde görevli yok: yara sarılmadı."), d.Teams[0].Detail);

            // Teknisyen yatıyor, yedek yok: ekip bekler, ne zaman hazır olacağı yazılır
            d = ShiftReportBuilder.Build(Synthetic(Snap(9, 10, 20, null, 3, canRaid: false), Snap(10, 13, 21.4, null, 2, canRaid: false)));
            Assert.AreEqual(Loc.T("BEKLİYOR"), d.Teams[0].Value);
            StringAssert.Contains(Loc.F("Teknisyen kalkana kadar baskın yok: vardiya {0} hazır.", TrNumber.WithLocative(12)), d.Teams[0].Detail);
            Assert.IsFalse(d.TeamReturned);

            // Teknisyen kalktı: ekip cepheye döner (teleks)
            d = ShiftReportBuilder.Build(Synthetic(Snap(9, 10, 20, null, 1, canRaid: false), Snap(10, 13, 21.4, null, 0)));
            StringAssert.Contains(Loc.F("{0} ayağa kalktı.", Baskin.UnitTypes["tek"].Name), d.Teams[0].Detail);
            Assert.AreEqual(Loc.T("HAZIR"), d.Teams[0].Value);
            Assert.IsTrue(d.TeamReturned);
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
                // Ekip bölümü: Alfa + YZ alarmı; alarm satırı ekip simgesini gizleyebilsin
                var teamRows = view.FindProperty("teamRows");
                Assert.AreEqual(2, teamRows.arraySize);
                for (int i = 0; i < teamRows.arraySize; i++)
                {
                    var icon = (UnityEngine.UI.Image)teamRows.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue;
                    Assert.IsNotNull(icon, "ekip satırı simgesi " + i);
                    Assert.IsTrue(icon.transform.IsChildOf(((GameObject)teamRows.GetArrayElementAtIndex(i).FindPropertyRelative("root").objectReferenceValue).transform));
                }

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
