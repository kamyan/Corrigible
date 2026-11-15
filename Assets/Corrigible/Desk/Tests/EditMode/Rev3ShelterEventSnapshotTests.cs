using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 4 (sunum · olaylar): olay tutanağının anlık görüntüsü Core'un public okuyucularını birebir yansıtır, saf okumadır;
    /// şalter kilidi, teleks satırları, devir fişinin ERZAK kutusu ve kişi durumları, pano kartının tecrit / görev yok etiketleri,
    /// eski dosya sekmelerinin kapanması ve karartmanın üretim tahmini. "Kâğıt" testleri olayı hata ayıklama kurulumuyla
    /// (<c>ShelterEvents.Pending</c>) açar: kurulum komut değildir, kayda girmez. Tutanak yüzü prefab'dan (TR ve EN, 14 tür).
    /// </summary>
    public sealed class Rev3ShelterEventSnapshotTests
    {
        const string PaperPrefab = "Assets/Corrigible/Desk/Prefabs/Siginak/ShelterEventPaper.prefab";
        static readonly string[] Personless = { "erzak_azaliyor", "karartma", "takas", "cagri", "konvoy" };

        [SetUp]
        public void Turkish() => DeskLoc.Instance.SetLanguage(DeskLang.Tr);

        [TearDown]
        public void RestoreTurkish() => DeskLoc.Instance.SetLanguage(DeskLang.Tr);

        /// <summary>Olaylar açık kartlı oyun; kapıya kimse gelmez, alarm artmaz.</summary>
        static Simulation Ready(uint seed = 79) => Simulation.NewGame(seed, new Balance
        {
            shelterEventsEnabled = true, planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 100, admissionKnockMax = 100,
        });

        static ResidentState[] Alive(Simulation sim) => sim.Board.People.Where(p => p.Alive).ToArray();

        /// <summary>Hata ayıklama kurulumu: türün açık olayı (canlı kişiler; kavgada iki kişi; izde alan; sabotajda kaynak).</summary>
        static ShelterEventInstance Open(Simulation sim, string kind)
        {
            var alive = Alive(sim);
            var e = new ShelterEventInstance { Kind = kind, OpenedShift = sim.S.Day, Amount = 3, Resource = "erzak", Area = "uretim" };
            if (!Personless.Contains(kind)) e.PersonId = alive[0].Id;
            if (kind == "kavga") e.OtherId = alive[1].Id;
            sim.ShelterEvents.Pending = e;
            return e;
        }

        // ---------------------------------------------------------------- anlık görüntü

        [Test]
        public void Build_NullWithoutEvent_AndForOrphanOrEnded()
        {
            var sim = Ready();
            Assert.IsNull(ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi()), "olay yok");
            var e = Open(sim, "hirsizlik");
            Assert.IsNotNull(ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi()));
            sim.Board.Person(e.PersonId).Alive = false;
            Assert.IsNull(ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi()), "hedefi giden (yetim) olay kâğıda çıkmaz");
            Assert.IsNull(ShelterEventSnapshotBuilder.PrintIssue(sim), "yetim olay fişi kilitlemez (Core PrintShift kapatıp geçer)");
            Open(sim, "anma").PersonId = e.PersonId;
            Assert.IsNotNull(ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi()), "anmanın hedefi ölüdür");
        }

        [Test]
        public void Build_Rows_MirrorCoreOptions_ForAllFourteenKinds([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = Ready();
            foreach (string kind in ShelterEventData.Kinds)
            {
                var e = Open(sim, kind);
                var d = ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi());
                var options = sim.ShelterEventOptions();
                Assert.IsNotNull(d, kind);
                Assert.AreEqual(ShelterEventData.Title(kind), d.Title, kind);
                Assert.AreEqual(sim.ShelterEventBody(e), d.Body, kind);
                Assert.AreEqual(Loc.F("V.{0:000}", sim.S.Day + 1), d.Shift);
                Assert.AreEqual(Personless.Contains(kind), d.Person == null, kind + ": kişi satırı");
                Assert.AreEqual(options.Count, d.Rows.Count, kind);
                for (int i = 0; i < options.Count; i++)
                {
                    var o = options[i];
                    var r = d.Rows[i];
                    Assert.AreEqual(o.Id, r.Id);
                    Assert.AreEqual(Loc.F("{0} · {1}", i + 1, o.Name), r.Name);
                    // KOS aşama 6: ifşayı değiştiren karara şerit önizlemesi alt satırda eklenir
                    string preview = ShelterEventSnapshotBuilder.ExposurePreview(sim, o.Id);
                    Assert.AreEqual(preview == null ? o.Cost : Loc.F("{0}\n{1}", o.Cost, preview), r.Cost, kind + "/" + o.Id);
                    Assert.AreEqual(string.IsNullOrEmpty(o.Risk) ? null : Loc.F("Risk: {0}", o.Risk), r.Risk);
                    Assert.AreEqual(string.IsNullOrEmpty(o.KitLock) ? null : o.KitLock, r.KitLock);
                    Assert.AreEqual(o.Safe, r.Safe);
                    Assert.AreEqual(o.Safe ? Loc.T("GÜVENLİ") : null, r.SafeTag);
                    Assert.AreEqual(o.Open, r.Enabled, kind + "/" + o.Id);
                    Assert.AreEqual(o.Open ? null : Loc.F("KAPALI · {0}", o.ClosedReason), r.Closed);
                    Assert.IsFalse(r.Selected);
                }
                Assert.IsFalse(d.ConfirmReady);
            }
        }

        [Test]
        public void Build_IsPure_AndSelectionIsPresentationOnly()
        {
            var sim = Ready();
            Open(sim, "iz");
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count;
            string board = JsonUtility.ToJson(sim.Board);
            var ui = new ShelterEventUi();
            var d = ShelterEventSnapshotBuilder.Build(sim, ui);
            ui.Align(d.Key);
            ui.SelectedId = "yoksay";
            var selected = ShelterEventSnapshotBuilder.Build(sim, ui);
            ShelterEventSnapshotBuilder.StatusLine(sim);
            ShelterEventSnapshotBuilder.PrintIssue(sim);
            ShelterEventSnapshotBuilder.TelexLines(sim, 0, out _);
            Assert.AreEqual(rng, sim.S.Rng.State, "okuyucu RNG çekmez");
            Assert.AreEqual(commands, sim.Record.commands.Count, "okuyucu komut yazmaz");
            Assert.AreEqual(board, JsonUtility.ToJson(sim.Board), "okuyucu panoyu değiştirmez");
            Assert.IsTrue(selected.ConfirmReady);
            Assert.AreEqual("yoksay", selected.Selected.Id);
            Assert.AreEqual(Loc.F("{0} · damgala", ShelterEventData.OptionName("iz", "yoksay")), selected.Confirm);
            ui.Align("başka kâğıt");
            Assert.IsFalse(ShelterEventSnapshotBuilder.Build(sim, ui).ConfirmReady, "seçim yalnız aynı kâğıtta geçerli");
        }

        [Test]
        public void ClosedOption_DisabledWithReason_AndGeneralLockDisablesAll()
        {
            var sim = Ready();
            sim.S.Alarm = sim.B.liberationAlarmThreshold - 1;
            Open(sim, "konvoy");
            var d = ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi());
            Assert.IsFalse(d.Rows[0].Enabled, "kabul: alarm eşiğe çok yakın");
            Assert.AreEqual(Loc.T("KAPALI"), d.Rows[0].ClosedStamp);
            StringAssert.StartsWith(Loc.F("KAPALI · {0}", ""), d.Rows[0].Closed);
            Assert.IsTrue(d.Rows[1].Enabled && d.Rows[1].Safe, "güvenli seçenek açık");
            // KARAR kutusunun ipucu yalnız açık satırları sayar (1. satır kapalı)
            Assert.AreEqual(Loc.F("önce bir satır seç ({0})", "2"), d.Confirm);

            var held = ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi(), storyHold: true);
            Assert.IsTrue(held.Rows.All(r => !r.Enabled), "açılış belgesi açıkken hiçbir satır seçilmez");
            Assert.AreEqual(Loc.T("Önce açılış belgesini kapat."), held.Note);
            Assert.AreEqual(Loc.T("karar verilemez"), held.Confirm, "açık satır yoksa ipucu numara saymaz");

            sim.S.Alarm = 0;
            Assert.AreEqual(Loc.F("önce bir satır seç ({0})", "1, 2"), ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi()).Confirm, "hepsi açık");
        }

        [Test]
        public void Decision_IsOneCommand_AndUnlocksThePrint()
        {
            var sim = Ready();
            Open(sim, "erzak_azaliyor");
            // Core gerekçesi ve tutanağın yeri (kayıttan devamda OLAY teleksi yeniden basılmaz)
            string issue = Loc.F("{0} Tutanak EVRAK tepsisinde.", sim.ShelterEventPrintCheck());
            Assert.AreEqual(issue, ShelterEventSnapshotBuilder.PrintIssue(sim));
            var (plate, line) = SiginakShiftClose.LockReason(false, false, false, false, false, null, ShelterEventSnapshotBuilder.PrintIssue(sim));
            Assert.AreEqual(Loc.T("olay bekliyor"), plate);
            Assert.AreEqual(issue, line);
            int before = sim.Record.commands.Count;
            Assert.IsTrue(sim.DecideShelterEvent("tam").Ok);
            Assert.AreEqual(before + 1, sim.Record.commands.Count);
            Assert.IsNull(ShelterEventSnapshotBuilder.PrintIssue(sim));
            var lines = ShelterEventSnapshotBuilder.TelexLines(sim, 0, out int next);
            Assert.AreEqual(sim.ShelterEvents.Log.Count, next);
            Assert.AreEqual(Loc.T("KARAR"), lines.Single().Status);
            Assert.AreEqual(Loc.F("{0}: {1}", ShelterEventData.Title("erzak_azaliyor"), ShelterEventData.OptionName("erzak_azaliyor", "tam")), lines.Single().Message);
        }

        [Test]
        public void LockReason_EventIssue_ComesAfterThePlan()
        {
            Assert.AreEqual(Loc.T("plan düzeltilmeli"), SiginakShiftClose.LockReason(false, false, false, false, false, "plan", "olay").plate);
            Assert.AreEqual(Loc.T("olay bekliyor"), SiginakShiftClose.LockReason(false, false, false, false, false, null, "olay").plate);
            Assert.IsNull(SiginakShiftClose.LockReason(false, false, false, false, true, null, "olay").plate, "basılıyken kilit yok");
            Assert.IsNull(SiginakShiftClose.LockReason(false, false, false, false, false, null).plate);
        }

        [Test]
        public void TelexLines_EachLogKind_IfsaSkipped([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = Ready();
            string id = Alive(sim)[0].Id, name = Alive(sim)[0].Name;
            var log = sim.ShelterEvents.Log;
            void Add(string kind, string option = null, string person = null) =>
                log.Add(new ShelterEventLogEntry { Shift = sim.S.Day, Kind = kind, Option = option, PersonId = person });
            Add("bulgu", null, id); Add("bulgu_yok", null, id); Add("sokuldu", null, id); Add("ayrilma", null, id);
            Add("kapandi", "hirsizlik", id); Add("vadeli", "yatis_uzar", id); Add("vadeli", "kavga_yara", id); Add("vadeli", "yas", id);
            Add("vadeli", "cagri_guven"); Add("ifsa"); Add("iz", "tecrit", id);
            var lines = ShelterEventSnapshotBuilder.TelexLines(sim, 0, out int next);
            Assert.AreEqual(log.Count, next);
            Assert.AreEqual(10, lines.Count, "ifşa (aşama 6) atlanır");
            CollectionAssert.AreEqual(new[] { "TECRİT", "TECRİT", "TECRİT", "ERZAK", "OLAY", "OLAY", "OLAY", "OLAY", "OLAY", "KARAR" }.Select(Loc.T).ToArray(),
                lines.Select(l => l.Status).ToArray());
            Assert.AreEqual(Loc.F("{0}: tecrit bitti, bulgu yok. Görev alabilir.", name), lines[1].Message);
            Assert.AreEqual(Loc.F("{0} tutanağı kapandı: {1} artık sığınakta değil.", ShelterEventData.Title("hirsizlik"), name), lines[4].Message);
            Assert.AreEqual(Loc.T("Özgür bölge yardımsız kaldı: güven −1."), lines[8].Message);
            Assert.IsEmpty(ShelterEventSnapshotBuilder.TelexLines(sim, next, out _), "okunan satır yeniden gelmez");
        }

        // ---------------------------------------------------------------- göstergeler

        [Test]
        public void StatusTags_HungerHalfRationBlackout_OnlyWithEvents()
        {
            var sim = Ready();
            Assert.IsNull(ShelterEventSnapshotBuilder.StatusLine(sim));
            sim.Shelter.HungryStreak = 2;
            sim.Shelter.HalfRationUntil = sim.S.Day + 3;
            sim.Shelter.BlackoutShift = sim.S.Day + 1;
            Assert.AreEqual(Loc.F("{0} · {1}", Loc.F("{0} · {1}", Loc.F("AÇ {0}", 2), Loc.F("YARIM TAYIN {0} V", 3)), Loc.T("KARARTMA")),
                ShelterEventSnapshotBuilder.StatusLine(sim));
            Assert.AreEqual(Loc.F("AÇ {0}", 2), ShelterEventSnapshotBuilder.CounterTag(sim, out bool alarm));
            Assert.IsTrue(alarm);
            StringAssert.StartsWith(Loc.T("KARARTMA · üretim ve mutfak durdu"), ShelterEventSnapshotBuilder.CrtHint(sim));
            StringAssert.Contains(ShelterEventSnapshotBuilder.StatusLine(sim), PlanningBoardSnapshotBuilder.Build(sim, null, false, false, false).Header);
            Assert.AreEqual(ProductionStop.Blackout, ProductionForecast.StopOf(sim, ProductionForecast.Producer(sim.Board)), "karartmada üretim durur");

            sim.B.shelterEventsEnabled = false;
            Assert.IsNull(ShelterEventSnapshotBuilder.StatusLine(sim), "olaylar kapalıyken gösterge yok");
            Assert.IsNull(ShelterEventSnapshotBuilder.CounterTag(sim, out _));
        }

        [Test]
        public void SlipAndBoard_IsolatedIdleAndRations()
        {
            var sim = Ready();
            var alive = Alive(sim);
            var iso = alive[0];
            iso.Isolated = true;
            iso.Job = "rest";
            sim.Admission.File(iso.Id).IsolatedUntil = sim.S.Day + 3;
            var idle = alive[1];
            idle.Job = "rest";
            sim.Admission.File(idle.Id).IdleUntil = sim.S.Day + 1;
            sim.Shelter.HungryStreak = 1;

            var facts = HandoverSlipFactsBuilder.Build(sim);
            var isoRow = facts.shelter.Single(r => r.name == iso.Name);
            Assert.AreEqual(SlipPersonState.Isolated, isoRow.state);
            Assert.AreEqual(3, isoRow.stateShifts);
            Assert.AreEqual(Loc.F("tecritte {0}", 3), HandoverSlipFormat.StateWord(isoRow.state, isoRow.stateShifts));
            var idleRow = facts.shelter.Single(r => r.name == idle.Name);
            Assert.AreEqual(SlipPersonState.Idle, idleRow.state);
            Assert.AreEqual(1, idleRow.stateShifts);
            // DOSYA kutuları eski kabul yoluyla kalktı (aşama 7): iki iz de olsa fişte DOSYA kutusu yok
            var clued = sim.Admission.File(alive[2].Id);
            clued.ClueCount = 2;
            facts = HandoverSlipFactsBuilder.Build(sim);
            Assert.IsTrue(facts.warnings.All(w => System.Enum.IsDefined(typeof(SlipWarningKind), w.kind)), "DOSYA kutusu yok (türleri de söküldü)");
            clued.ClueCount = 0;
            var box = HandoverSlipFormat.Format(facts).Warnings.Single(w => w.Kind == SlipWarningKind.Rations);
            Assert.AreEqual(Loc.T("ERZAK ·"), box.Prefix);
            StringAssert.StartsWith(Loc.F("aç {0} V", 1), box.Text);

            var s = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState { SelectedId = iso.Id }, false, false, false);
            var card = s.Person(iso.Id);
            Assert.IsTrue(card.Isolated);
            Assert.AreEqual(Loc.F("Tecritte · {0} v.", 3), card.Strip);
            Assert.AreEqual(Loc.F("Görev yok · {0} v.", 1), s.Person(idle.Id).Strip);
            CollectionAssert.IsSubsetOf(new[] { iso.Id, idle.Id }, s.DeckOrder.Skip(s.DeckOrder.Count - 2).ToArray(), "tecritte ve görevsiz destenin altında");
            Assert.IsTrue((CardStateOverlay.StatesOf(card) & PersonStates.Isolated) != 0);
            Assert.AreEqual(Loc.F("TECRİT · {0} V", 3), s.Folder.TagText);
            // Eski kabul satırları (ERİŞİM · TAM ERİŞİM, İNCELEME · yok) olay modelinde yazılmaz; kısıtlı kişide ERİŞİM kalır
            var labels = s.Folder.Right.Concat(s.Folder.DossierRight).Select(l => l.Label).ToList();
            CollectionAssert.DoesNotContain(labels, Loc.T("İNCELEME"), "İNCELEME satırı yok");
            CollectionAssert.DoesNotContain(labels, Loc.T("ERİŞİM"), "kısıtsız kişide ERİŞİM satırı yok");
            alive[2].Restricted = true;
            var restricted = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState { SelectedId = alive[2].Id }, false, false, false).Folder;
            Assert.IsTrue(restricted.DossierRight.Any(l => l.Label == Loc.T("ERİŞİM") && l.Value == Loc.T("KISITLI")), "olay kısıtı dosyada okunur");
            sim.B.shelterEventsEnabled = false;
            var legacy = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState { SelectedId = idle.Id }, false, false, false).Folder;
            Assert.IsFalse(legacy.Right.Any(l => l.Label == Loc.T("İNCELEME")), "eski satırlar olaylar kapalıyken de yok (aşama 7)");
            sim.B.shelterEventsEnabled = true;
        }

        // ---------------------------------------------------------------- gizlilik ve eşlik

        /// <summary>Olaylar açık, olağan olaylar susturulmuş; iz gecikmesi 1. <paramref name="robots"/>: her aday sızmacı; değilse insan izi.</summary>
        static Balance Chain(bool robots)
        {
            var b = new Balance
            {
                shelterEventsEnabled = true, planningBoardEnabled = true, liberationAlarmPerShift = 0, liberationAlarmPerFailure = 0,
                boardStartSupplies = 60, boardSuppliesCapacity = 100, startFon = 900, eventGapMin = 1, eventGapMax = 1, desertStreak = 0,
                candidateEmergencyBelow = 0, candidateTier0CostChance = 0,
                traceDelayMin = 1, traceDelayMax = 1, secondTraceGapMin = 1, secondTraceGapMax = 1,
            };
            foreach (string kind in ShelterEventData.Ordinary)
            {
                var name = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "buyuk_hirsizlik", "BigTheft" }, { "kacak", "Deserter" }, { "erzak_azaliyor", "LowRation" }, { "hirsizlik", "Theft" },
                    { "agir_yarali", "Wounded" }, { "kavga", "Feud" }, { "anma", "Memorial" }, { "karartma", "Blackout" },
                    { "takas", "Trade" }, { "cagri", "Call" }, { "konvoy", "Convoy" },
                }[kind];
                typeof(Balance).GetField("event" + name + "Max").SetValue(b, 0);
            }
            double robot = robots ? 1 : 0;
            b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = robot;
            b.humanTraceChance = robots ? 0 : 1;
            b.humanSecondTraceChance = 0;
            return b;
        }

        static void Close(Simulation sim)
        {
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
        }

        /// <summary>İlk grup geri çevrilir, ikincinin ilk adayı alınır, iz kâğıdı açılana dek kapanış (yalnız komut).</summary>
        static string ToIz(Simulation sim)
        {
            for (int g = 0; sim.Admission.Group.Count == 0 && g < 10; g++) Close(sim);
            Assert.IsTrue(sim.PassCandidates().Ok);
            for (int g = 0; sim.Admission.Group.Count == 0 && g < 10; g++) Close(sim);
            string id = sim.Admission.Group[0].Resident.Id;
            Assert.IsTrue(sim.ChooseCandidate(id).Ok);
            for (int g = 0; sim.PendingShelterEvent == null && g < 8; g++) Close(sim);
            Assert.AreEqual("iz", sim.PendingShelterEvent?.Kind, "iz kâğıdı açılmadı (gün " + sim.S.Day + ")");
            return id;
        }

        static string Paper(ShelterEventPaperData d) => string.Join("\n", new[] { d.Key, d.Kind, d.Shift, d.Title, d.Person, d.Body, d.Confirm, d.Note, d.HoverLine }
            .Concat(d.Rows.SelectMany(r => new[] { r.Id, r.Name, r.SafeTag, r.Cost, r.Risk, r.KitLock, r.Closed, r.ClosedStamp, r.Enabled.ToString(), r.Safe.ToString() })));

        /// <summary>
        /// Gizlilik (tasarım §7): aynı tohum ve aynı komutlarla sızmacının ve iz bırakan insanın iz kâğıdı, kart şeridi, dosya etiketi ve
        /// tecrit kararından sonraki şerit ve fiş satırı birebir aynıdır (sunum gizli kimliği okumaz).
        /// </summary>
        [Test]
        public void Privacy_IzPaper_SameForHumanAndInfiltrator([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var robot = Simulation.NewGame(79, Chain(true));
            var human = Simulation.NewGame(79, Chain(false));
            string a = ToIz(robot), b = ToIz(human);
            Assert.AreEqual(a, b, "aynı aday");
            Assert.AreEqual(robot.S.Day, human.S.Day, "iz aynı kapanışta");
            Assert.AreEqual(Paper(ShelterEventSnapshotBuilder.Build(robot, new ShelterEventUi())), Paper(ShelterEventSnapshotBuilder.Build(human, new ShelterEventUi())), "iz kâğıdı");
            System.Func<Simulation, string> board = sim =>
            {
                var s = PlanningBoardSnapshotBuilder.Build(sim, new BoardUiState { SelectedId = a }, false, false, false);
                var c = s.Person(a);
                return c.Strip + "|" + c.Isolated + "|" + s.Folder.TagText + "|" + s.Header + "|" + string.Join(",", s.DeckOrder);
            };
            Assert.AreEqual(board(robot), board(human), "kart ve dosya (iz)");
            Assert.AreEqual(Loc.F("İZ {0}", 1), PlanningBoardSnapshotBuilder.Build(robot, new BoardUiState { SelectedId = a }, false, false, false).Folder.TagText);

            Assert.IsTrue(robot.DecideShelterEvent("tecrit").Ok);
            Assert.IsTrue(human.DecideShelterEvent("tecrit").Ok);
            Assert.AreEqual(board(robot), board(human), "kart ve dosya (tecrit)");
            Assert.AreEqual(string.Join("/", ShelterEventSnapshotBuilder.TelexLines(robot, 0, out _).Select(t => t.Status + t.Message)),
                string.Join("/", ShelterEventSnapshotBuilder.TelexLines(human, 0, out _).Select(t => t.Status + t.Message)), "teleks satırları");
            var rf = HandoverSlipFactsBuilder.Build(robot).shelter.Single(r => r.name == robot.Board.Person(a).Name);
            var hf = HandoverSlipFactsBuilder.Build(human).shelter.Single(r => r.name == human.Board.Person(a).Name);
            Assert.AreEqual(rf.state + "/" + rf.stateShifts, hf.state + "/" + hf.stateShifts, "fiş satırı");
            Assert.AreEqual(SlipPersonState.Isolated, rf.state);
        }

        /// <summary>Pano bırakış kuralı Core'un görev denetimiyle aynı: tecrit, görev yok, olay kısıtı (yalnız Eğitim ve Boşta).</summary>
        [Test]
        public void DropRules_JobCheck_MatchesCore_ForIsolatedIdleAndRestricted()
        {
            var sim = Ready();
            var alive = Alive(sim);
            var iso = alive[0];
            iso.Isolated = true;
            sim.Admission.File(iso.Id).IsolatedUntil = sim.S.Day + 3;
            var idle = alive[1];
            sim.Admission.File(idle.Id).IdleUntil = sim.S.Day + 1;
            var res = alive[2];
            res.Restricted = true;
            sim.Admission.File(res.Id).RestrictedUntil = sim.S.Day + 3;
            foreach (var p in new[] { iso, idle, res })
            {
                p.Job = "rest";
                foreach (string job in new[] { "tek", "emp", "ana", "craft", "cook" })
                {
                    string core = sim.AssignResidentCheck(p.Id, job);
                    Assert.IsNotNull(core, p.Name + " → " + job + ": Core reddeder");
                    Assert.IsFalse(BoardDropRules.Valid(BoardCardKind.Person, p.Id, BoardTarget.Pocket(job), sim), p.Name + " → " + job + ": pano da reddeder");
                    Assert.AreEqual(core, BoardDropRules.Reason(BoardCardKind.Person, p.Id, BoardTarget.Pocket(job), sim), p.Name + " → " + job + ": aynı gerekçe");
                }
            }
        }

        // ---------------------------------------------------------------- dinlenme metni ve enum sırası

        /// <summary>ERZAK sayacının fark fişi: dinlenme metni varken görünür; geçici fiş kalkınca ona döner; null kaldırır.</summary>
        [Test]
        public void DeltaSlip_RestText_ReturnsAfterTransient_AndClears()
        {
            var go = new GameObject("DeltaSlip test");
            try
            {
                var text = new GameObject("Yazı").AddComponent<TMPro.TextMeshPro>();
                text.transform.SetParent(go.transform, false);
                var slip = go.AddComponent<DeltaSlip>();
                slip.Configure(null, text);
                go.SetActive(false);
                slip.SetRest("AÇ 2", Color.red);
                Assert.IsTrue(slip.Visible && slip.ShowingRest);
                Assert.AreEqual("AÇ 2", slip.Text);
                slip.Show("+3", Color.green);
                Assert.IsFalse(slip.ShowingRest, "geçici fiş üstte");
                Assert.AreEqual("+3", slip.Text);
                slip.SetRest("YARIM TAYIN 3 V", Color.black);
                Assert.AreEqual("+3", slip.Text, "geçici fiş sürerken dinlenme metni beklemede");
                slip.Hide();
                Assert.IsTrue(slip.ShowingRest, "geçici fiş kalkınca dinlenme metni");
                Assert.AreEqual("YARIM TAYIN 3 V", slip.Text);
                slip.Show("−1", Color.red, pin: true);
                slip.Release();
                slip.Hide();
                Assert.AreEqual("YARIM TAYIN 3 V", slip.Text, "iğne kalkınca da dinlenme metni");
                slip.SetRest(null, Color.black);
                Assert.IsFalse(slip.Visible, "null dinlenme metni fişi kaldırır");
                Assert.IsNull(slip.RestText);
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>Yan dosyada sayıyla saklanan enumlara yalnız sona eklendi (eski fişler bozulmaz).</summary>
        [Test]
        public void Enums_OnlyAppended()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { SlipPersonState.None, SlipPersonState.Hurt, SlipPersonState.Down, SlipPersonState.Restricted,
                SlipPersonState.RestrictionEnded, SlipPersonState.Isolated, SlipPersonState.Idle }.Select(x => (int)x).ToArray());
            // Aşama 7: eski kabulün uyarıları (0–3) söküldü; kalanların sayısı korunur
            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7 }, new[] { SlipWarningKind.DoorLastShift, SlipWarningKind.Rations, SlipWarningKind.Exposure,
                SlipWarningKind.ShelterShort }.Select(x => (int)x).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, new[] { ProductionStop.None, ProductionStop.NoMaker, ProductionStop.Starvation, ProductionStop.StoreFull,
                ProductionStop.Blackout }.Select(x => (int)x).ToArray());
        }

        // ---------------------------------------------------------------- yüz

        [Test]
        public void Face_FitsAllKinds_TrAndEn_AndMapsRowsAndConfirm([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var root = PrefabUtility.LoadPrefabContents(PaperPrefab);
            try
            {
                var face = root.GetComponent<ShelterEventPaperFace>();
                var view = root.GetComponent<ShelterEventPaperView>();
                Assert.IsNotNull(face);
                Assert.IsNotNull(view);
                Assert.IsFalse(view.HitArea.enabled, "kaydedilmiş prefab'da çarpıştırıcı kapalı");
                var sim = Ready();
                foreach (string kind in ShelterEventData.Kinds)
                {
                    Open(sim, kind);
                    var d = ShelterEventSnapshotBuilder.Build(sim, new ShelterEventUi());
                    d.Rows[0].Selected = true;
                    d.ConfirmReady = true;
                    d.Note = sim.ShelterEventPrintCheck();
                    d.StampLines = ShelterEventSnapshotBuilder.StampLines(kind, d.Rows[0].Id);
                    face.Bind(d);
                    face.ShowStamp(true, false);
                    Assert.IsFalse(face.Overflowing, lang + " " + kind + ": taşma (alt " + face.ContentBottomMm + " mm)");
                    Assert.AreEqual(d.Rows.Count, face.RowCount);
                    for (int i = 0; i < d.Rows.Count; i++) Assert.AreEqual(i, face.RowAt(face.RowRect(i).center), kind + " satır " + i);
                    Assert.IsTrue(face.ConfirmContains(face.ConfirmRect.center));
                    Assert.AreEqual(-1, face.RowAt(face.ConfirmRect.center), "KARAR kutusu satır değildir");
                    if (lang == DeskLang.En)
                        foreach (var t in face.Texts.Where(x => x.gameObject.activeSelf && !string.IsNullOrEmpty(x.text)))
                        {
                            // Sabit etiketler edit modda kaynak metinle kalır (DeskLocText yalnız Play'de çevirir): çevirisi denetlenir
                            var loc = t.GetComponent<DeskLocText>();
                            string text = loc != null ? DeskLoc.Instance.T(loc.Source) : t.text;
                            foreach (var p in sim.Board.People) text = text.Replace(p.Name, "");
                            Assert.IsFalse(text.Any(c => "çğıöşüÇĞİÖŞÜ".IndexOf(c) >= 0), kind + ": EN'de Türkçe harf: " + t.name + " = " + t.text);
                        }
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
