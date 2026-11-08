using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using Corrigible.Game.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 4 (R19) vardiya kapanışının akışı (PLAN W5): şalter kilidinin nedeni ve önceliği, ses eşlemesi, sayaç ve
    /// teleks süreleri, dosyalama zaman çizelgesi, EVRAK arşivinin yan dosyası, masa kilidinin fişi dışarıda tutması, gerçek bir
    /// kartlı oyunda basım / ret / ön kontrol / kilit ve K6'nın Esc sırası. Sahneye bağlı değildir; kayıtlar geçici klasöre yazılır.
    /// </summary>
    public sealed class Rev3ShiftCloseFlowTests
    {
        const string StorageKey = "Corrigible.TestStorage";
        string originalStorage, directory;
        readonly List<Object> created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Loc.SetLanguage(Loc.Turkish);
            originalStorage = SessionState.GetString(StorageKey, "");
            directory = Path.Combine(Path.GetTempPath(), "Corrigible-ShiftClose-" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(StorageKey, directory);
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
            Loc.SetLanguage(Loc.Turkish);
            if (string.IsNullOrEmpty(originalStorage)) SessionState.EraseString(StorageKey);
            else SessionState.SetString(StorageKey, originalStorage);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            created.Add(go);
            return go.AddComponent<T>();
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            Assert.IsNotNull(p, target.GetType().Name + "." + field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            Assert.IsNotNull(p, target.GetType().Name + "." + field);
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SiginakTheme Theme => SiginakTheme.Active;

        static HandoverSlipFacts Facts(int shift) => new HandoverSlipFacts
        {
            receiptId = "vardiya-" + (shift - 1), shift = shift, alive = 8, capacity = 10,
            supplies = 24, suppliesCapacity = 60, suppliesAfter = 21.9,
            field =
            {
                new SlipFieldRow { role = "tek", name = "Ayla", items = { "kit_tek", "armor" } },
                new SlipFieldRow { role = "emp" },
                new SlipFieldRow { role = "ana", name = "Kerem", items = { "medkit" } },
            },
            shelter = { new SlipShelterRow { name = "Deniz", job = "train", subject = "tek", state = SlipPersonState.Restricted, stateShifts = 2 } },
            training = { active = true, name = "Deniz", subject = "tek", xpBefore = 1, xpAfter = 3, tierBefore = 0, tierAfter = 1 },
            production = { kind = SlipProductionKind.Progress, itemType = "kit_tek", maker = "Ayla", done = 1, after = 2, turns = 3 },
            openApplications = 1,
            warnings = { new SlipWarning { kind = SlipWarningKind.Door }, new SlipWarning { kind = SlipWarningKind.FileClues, name = "Deniz" } },
        };

        /// <summary>Gerçek kartlı oyun (GameController, tohum 79) ve ona bağlı kapanış; şalter R19 kipinde, teleks ve masa yüzü bağlı.</summary>
        (GameController game, SiginakShiftClose close, TelexFeed telex, ShiftSwitch sw) BoardGame()
        {
            var game = Make<GameController>("Game");
            var surface = Make<LiberationDeskView>("Surface");
            Set(surface, "game", game);
            var telex = Make<TelexFeed>("Telex");
            var sw = Make<ShiftSwitch>("Shift switch");
            SetBool(sw, "shiftClose", true);
            var close = Make<SiginakShiftClose>("Shift close");
            Set(close, "game", game);
            Set(close, "surface", surface);
            Set(close, "telex", telex);
            Set(close, "shiftSwitch", sw);
            game.NewGame(79);
            close.Restore();
            return (game, close, telex, sw);
        }

        // ---------------------------------------------------------------- kilit nedeni

        [Test]
        public void LockReason_PriorityAndTexts()
        {
            var raid = SiginakShiftClose.LockReason(true, false, false, false, false, null);
            Assert.AreEqual("baskın sürüyor", raid.plate);
            Assert.AreEqual("Baskın sürüyor: vardiya bitirilemez.", raid.line);
            Assert.AreEqual(Theme.Text("shiftClose.states.locked.plate2.tr"), raid.plate, "token R19a kilitli");
            Assert.AreEqual(("harekât bitti", "Harekât bitti."), SiginakShiftClose.LockReason(false, true, false, false, false, null));
            Assert.AreEqual(("belge açık", "Önce açılış belgesini kapat."), SiginakShiftClose.LockReason(false, false, true, false, false, null));
            Assert.AreEqual(("rapor açık", "Rapor açık: önce EVRAK tepsisine dosyala."), SiginakShiftClose.LockReason(false, false, false, true, false, null));
            const string issue = "Yatıyor: Ayla göreve konamaz.";
            Assert.AreEqual(("plan düzeltilmeli", issue), SiginakShiftClose.LockReason(false, false, false, false, false, issue),
                "D1-A: plan hatası kilit; uzun satır Core'un iletisi");

            // Öncelik: baskın > harekât bitti > belge açık > rapor açık > plan
            Assert.AreEqual(raid, SiginakShiftClose.LockReason(true, true, true, true, false, issue));
            Assert.AreEqual("harekât bitti", SiginakShiftClose.LockReason(false, true, true, true, false, issue).plate);
            Assert.AreEqual("belge açık", SiginakShiftClose.LockReason(false, false, true, true, false, issue).plate);
            Assert.AreEqual("rapor açık", SiginakShiftClose.LockReason(false, false, false, true, false, issue).plate);

            Assert.AreEqual(((string)null, (string)null), SiginakShiftClose.LockReason(false, false, false, false, false, null));
            Assert.AreEqual(((string)null, (string)null), SiginakShiftClose.LockReason(false, false, false, false, false, ""), "boş ileti plan hatası değil");

            // D2: plaka kısa (236 mm yuvaya 6,4 mm büyük harfle); metinler Loc'tan geçer
            var en = new Dictionary<string, string>
            {
                { "baskın sürüyor", "raid in progress" }, { "harekât bitti", "campaign over" }, { "belge açık", "document open" },
                { "rapor açık", "report open" }, { "plan düzeltilmeli", "plan needs fixing" },
                { "Baskın sürüyor: vardiya bitirilemez.", "Raid in progress: the shift cannot end." },
            };
            foreach (var lang in new[] { Loc.Turkish, Loc.English })
            {
                Loc.SetLanguage(lang, lang == Loc.English ? en : null);
                foreach (var flags in new[] { (true, false, false, false), (false, true, false, false), (false, false, true, false), (false, false, false, true) })
                {
                    var r = SiginakShiftClose.LockReason(flags.Item1, flags.Item2, flags.Item3, flags.Item4, false, null);
                    Assert.LessOrEqual(r.plate.Length, 24, lang + " " + r.plate);
                }
                Assert.LessOrEqual(SiginakShiftClose.LockReason(false, false, false, false, false, issue).plate.Length, 24);
            }
            Loc.SetLanguage(Loc.English, en);
            var raidEn = SiginakShiftClose.LockReason(true, false, false, false, false, null);
            Assert.AreEqual("raid in progress", raidEn.plate);
            Assert.AreEqual(Theme.Text("shiftClose.states.locked.plate2.en"), raidEn.plate, "token R19a kilitli EN");
            Assert.AreEqual("Raid in progress: the shift cannot end.", raidEn.line);
            Assert.AreEqual(("plan needs fixing", issue), SiginakShiftClose.LockReason(false, false, false, false, false, issue), "Core iletisi zaten çevrili");
        }

        [Test]
        public void LockReason_NoneWhilePrinted()
        {
            // Basılı fiş beklenirken şalter kilitlenmez: fiş EVRAK'a konmalı (PLAN §1.9)
            for (int mask = 0; mask < 16; mask++)
                foreach (var issue in new[] { null, "plan hatası" })
                {
                    var r = SiginakShiftClose.LockReason((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0, true, issue);
                    Assert.IsNull(r.plate, "maske " + mask);
                    Assert.IsNull(r.line, "maske " + mask);
                }
        }

        // ---------------------------------------------------------------- ses

        [Test]
        public void CueOf_MapsEveryShiftSound()
        {
            var expected = new Dictionary<ShiftSound, (Cue cue, int number)>
            {
                { ShiftSound.CoverOpen, (Cue.ShiftCoverOpen, 107) }, { ShiftSound.CoverShut, (Cue.ShiftCoverShut, 108) },
                { ShiftSound.Lever, (Cue.ShiftLever, 109) }, { ShiftSound.SlipPrint, (Cue.SlipPrint, 110) },
                { ShiftSound.SlipTear, (Cue.SlipTear, 111) }, { ShiftSound.SlipFile, (Cue.SlipFile, 112) },
                { ShiftSound.Gong, (Cue.ShiftGong, 113) }, { ShiftSound.MouthEmpty, (Cue.SlipMouthEmpty, 114) },
                { ShiftSound.Denied, (Cue.SwitchClack, 92) }, { ShiftSound.SlipLift, (Cue.BoardLift, 97) },
                { ShiftSound.SlipReturn, (Cue.BoardReturn, 99) },
            };
            var all = (ShiftSound[])Enum.GetValues(typeof(ShiftSound));
            Assert.AreEqual(all.Length, expected.Count, "her ShiftSound eşlenir");
            foreach (var sound in all)
            {
                Assert.IsTrue(expected.ContainsKey(sound), sound.ToString());
                var cue = SiginakShiftClose.CueOf(sound);
                Assert.AreEqual(expected[sound].cue, cue, sound.ToString());
                Assert.AreEqual(expected[sound].number, (int)cue, sound + " numarası (SoundBank girişleri int saklar)");
            }
            var fresh = all.Select(SiginakShiftClose.CueOf).Where(c => (int)c >= 107).ToList();
            Assert.AreEqual(8, fresh.Distinct().Count(), "yeni cue'lar ayrı");
            Assert.Throws<ArgumentOutOfRangeException>(() => SiginakShiftClose.CueOf((ShiftSound)99));
        }

        // ---------------------------------------------------------------- sayaç ve teleks süreleri

        [Test]
        public void CounterBank_RollAppliesToAllModules()
        {
            float roll = Theme.Number("motion.shiftClose.countersS", -1);
            Assert.AreEqual(0.4f, roll, 1e-5f, "R19b2 sayaçlar 0,4 s");
            Assert.AreEqual(Theme.Number("shiftClose.afterDrop.countersS", -1), roll, 1e-5f);
            foreach (CounterBank.Slot slot in Enum.GetValues(typeof(CounterBank.Slot)))
            {
                Assert.AreEqual(roll, CounterBank.RollOf(slot, roll, -1), 1e-6f, slot + ": bütün modüller aynı süre");
                Assert.AreEqual(-1, CounterBank.RollOf(slot, -1, -1), 1e-6f, slot + ": verilmezse modülün token'ı");
            }
            // Vardiya dizisi (eski yol): VARDİYA'nın kendi süresi önceliklidir, öteki modüller roll'u izler
            Assert.AreEqual(0.6f, CounterBank.RollOf(CounterBank.Slot.Shift, roll, 0.6f), 1e-6f);
            Assert.AreEqual(roll, CounterBank.RollOf(CounterBank.Slot.Funds, roll, 0.6f), 1e-6f);
            Assert.AreEqual(-1, CounterBank.RollOf(CounterBank.Slot.Supplies, -1, 0.6f), 1e-6f);
        }

        [Test]
        public void Teleprinter_TypeSecondsSpreadOverLine()
        {
            float total = Theme.Number("motion.shiftClose.telexPrintS", -1);
            Assert.AreEqual(1.2f, total, 1e-5f, "R19b2 teleks 1,2 s");
            Assert.AreEqual(Theme.Number("shiftClose.afterDrop.telexPrintS", -1), total, 1e-5f);
            float perChar = Theme.Number("motion.telexCharS", 0.03f);
            // Satır: kod + 2 boşluk + durum + 2 boşluk + ileti (Teleprinter.Plain); TR ve EN aynı sürede biter
            foreach (var line in new[]
                     {
                         "002/01  VARDİYA DEVREDİLDİ  Devir fişi EVRAK kutusuna kondu. Yeni vardiya başladı.",
                         "002/01  SHIFT HANDED OVER  Handover slip filed in PAPERS. New shift started.",
                     })
            {
                int n = line.Length;
                Assert.AreEqual(0, Teleprinter.TypedChars(0, n, total, perChar));
                Assert.AreEqual(n / 2, Teleprinter.TypedChars(total / 2, n, total, perChar), "yarı sürede yarısı");
                Assert.Less(Teleprinter.TypedChars(total - 0.01f, n, total, perChar), n);
                Assert.AreEqual(n, Teleprinter.TypedChars(total, n, total, perChar), "1,2 s'de satırın tamamı");
                Assert.AreEqual(n, Teleprinter.TypedChars(total * 3, n, total, perChar));
                // Süre verilmezse harf başına token: bu uzunlukta 1,2 s'ye sığmaz (A §4.1)
                Assert.Less(Teleprinter.TypedChars(total, n, -1, perChar), n, "harf başına 0,03 s ile 1,2 s yetmez");
                Assert.AreEqual(10, Teleprinter.TypedChars(perChar * 10.5f, n, -1, perChar));
            }
            Assert.AreEqual(0, Teleprinter.TypedChars(1, 0, total, perChar), "boş satır");
            Assert.AreEqual(100, Teleprinter.TypedChars(1, 100, -1, 0), "harf süresi en az 5 ms");
        }

        // ---------------------------------------------------------------- dosyalama zaman çizelgesi

        [Test]
        public void FilingTimeline_FromTokens()
        {
            var t = SiginakShiftClose.FilingTimeline.From(Theme);
            Assert.AreEqual(0.2f, t.ToTrayS, 1e-5f, "fiş tepsiye 0,2 s");
            Assert.AreEqual(0.1f, t.DarkInS, 1e-5f, "kararma 0,1 s (darkS / 2)");
            Assert.AreEqual(0.1f, t.DarkOutS, 1e-5f, "açılma 0,1 s");
            Assert.AreEqual(0.3f, t.FileAtS, 1e-5f, "tam karanlıkta FileBoardShift");
            Assert.AreEqual(0.4f, t.LightAtS, 1e-5f, "şalter, sayaç ve teleks");
            Assert.AreEqual(0.4f, t.CountersS, 1e-5f);
            Assert.AreEqual(1.2f, t.TelexS, 1e-5f);
            // Tasarım bloğu (shiftClose.*) ve hareket bloğu (motion.shiftClose.*) aynı sayıyı söyler
            Assert.AreEqual(Theme.Number("shiftClose.slip.toTrayS", -1), t.ToTrayS, 1e-5f);
            Assert.AreEqual(Theme.Number("shiftClose.afterDrop.darkS", -1), t.DarkInS + t.DarkOutS, 1e-5f);

            Assert.AreEqual(0, t.Alpha(0), 1e-5f);
            Assert.AreEqual(0, t.Alpha(0.2f), 1e-5f, "tepsiye inerken karanlık yok");
            Assert.AreEqual(0.5f, t.Alpha(0.25f), 1e-3f);
            Assert.AreEqual(1, t.Alpha(0.3f), 1e-5f, "tepe");
            Assert.AreEqual(0.5f, t.Alpha(0.35f), 1e-3f);
            Assert.AreEqual(0, t.Alpha(0.4f), 1e-5f);
            Assert.AreEqual(0, t.Alpha(2), 1e-5f);
            for (float s = 0; s < 0.5f; s += 0.01f) Assert.That(t.Alpha(s), Is.InRange(0f, 1f));

            var zero = new SiginakShiftClose.FilingTimeline(0.2f, 0, 0.4f, 1.2f);
            Assert.That(zero.Alpha(0.2005f), Is.InRange(0f, 1f), "sıfır kararma sıfıra bölmez");
        }

        // ---------------------------------------------------------------- EVRAK arşivi (D7-A)

        [Test]
        public void DeskStore_SlipsRoundTrip_OldFileCompatible()
        {
            var record = new GameRecord { seed = 79 };
            var slips = Enumerable.Range(1, 7).Select(Facts).ToList();
            slips.Insert(3, null);
            var telex = new[] { new TelexLine { code = "001/01", status = "ÖN KONTROL", message = "x", kind = TelexKind.Info } };
            SiginakDeskStore.Save(record, telex, null, "Desk", slips);

            var back = SiginakDeskStore.LoadSlips(record);
            Assert.IsNotNull(back);
            Assert.AreEqual(SiginakDeskStore.SlipKeep, back.Count, "son 5 fiş");
            CollectionAssert.AreEqual(new[] { 3, 4, 5, 6, 7 }, back.Select(f => f.shift).ToArray(), "eskiden yeniye");
            var f7 = back[4];
            Assert.AreEqual("vardiya-6", f7.receiptId);
            Assert.AreEqual(1, f7.copy);
            Assert.AreEqual(8, f7.alive);
            Assert.AreEqual(10, f7.capacity);
            Assert.AreEqual(24, f7.supplies, 1e-9);
            Assert.AreEqual(60, f7.suppliesCapacity, 1e-9);
            Assert.AreEqual(21.9, f7.suppliesAfter, 1e-9);
            Assert.AreEqual(3, f7.field.Count);
            Assert.AreEqual("tek", f7.field[0].role);
            Assert.AreEqual("Ayla", f7.field[0].name);
            CollectionAssert.AreEqual(new[] { "kit_tek", "armor" }, f7.field[0].items);
            Assert.IsTrue(string.IsNullOrEmpty(f7.field[1].name), "boş cep (JsonUtility null → boş dizgi)");
            Assert.AreEqual(SlipPersonState.Restricted, f7.shelter[0].state);
            Assert.AreEqual(2, f7.shelter[0].stateShifts);
            Assert.AreEqual("train", f7.shelter[0].job);
            Assert.IsTrue(f7.training.active);
            Assert.AreEqual(3, f7.training.xpAfter);
            Assert.AreEqual(1, f7.training.tierAfter);
            Assert.AreEqual(SlipProductionKind.Progress, f7.production.kind);
            Assert.AreEqual(2, f7.production.after);
            Assert.AreEqual(1, f7.openApplications);
            CollectionAssert.AreEqual(new[] { SlipWarningKind.Door, SlipWarningKind.FileClues }, f7.warnings.Select(w => w.kind).ToArray());
            Assert.AreEqual(1, SiginakDeskStore.LoadTelex(record).Count, "teleks aynı dosyada");
            Assert.AreEqual("Desk", SiginakDeskStore.LoadPose(record));
            Assert.IsNull(SiginakDeskStore.LoadSlips(new GameRecord { seed = 80 }), "başka oyunun dosyası yok sayılır");

            // Aşama 4'ten önceki dosya: slips alanı yok → boş arşiv, öteki alanlar okunur
            File.WriteAllText(Path.Combine(SaveStore.StorageDirectory, SiginakDeskStore.FileName),
                "{\"seed\":79,\"telex\":[{\"code\":\"001/01\",\"status\":\"ÖN KONTROL\",\"message\":\"x\",\"kind\":0}],\"reports\":[],\"pose\":\"Map\"}");
            var old = SiginakDeskStore.LoadSlips(record);
            Assert.IsNotNull(old, "eski dosya da bu oyunun");
            Assert.AreEqual(0, old.Count);
            Assert.AreEqual(1, SiginakDeskStore.LoadTelex(record).Count);
            Assert.AreEqual("Map", SiginakDeskStore.LoadPose(record));

            // Arşivsiz çağrı (eski imza) boş arşiv yazar
            SiginakDeskStore.Save(record, telex, null, "Desk");
            Assert.AreEqual(0, SiginakDeskStore.LoadSlips(record).Count);
            File.Delete(Path.Combine(SaveStore.StorageDirectory, SiginakDeskStore.FileName));
            Assert.IsNull(SiginakDeskStore.LoadSlips(record), "dosya yok");
        }

        [Test]
        public void Restore_ReadsArchive_LastFiledSlipOnTray()
        {
            var (game, close, _, _) = BoardGame();
            var tray = Make<EvrakTray>("Paper tray");
            Set(close, "tray", tray);
            SiginakDeskStore.Save(game.Sim.Record, null, null, "Desk", Enumerable.Range(1, 7).Select(Facts).ToList());
            close.Restore();
            Assert.AreEqual(SiginakDeskStore.SlipKeep, close.Archive.Count);
            Assert.AreEqual(3, close.Archive[0].shift);
            Assert.AreEqual(7, close.Archive[close.Archive.Count - 1].shift);
            Assert.IsNotNull(tray.LastFiled, "son fiş tepside üst kâğıt");
            Assert.AreEqual(7, tray.LastFiled.shift);
            Assert.IsFalse(close.OpenTray(), "fiş görünümü bağlı değilken okunmaz (çağıran emir belgesini açar)");
            Assert.IsFalse(close.Busy);
        }

        // ---------------------------------------------------------------- masa kilidi

        [Test]
        public void BlockedExceptShift_IgnoresOnlyTheShiftClose()
        {
            var surface = Make<LiberationDeskView>("Surface");
            var desk = Make<SiginakDesk>("Desk controller");
            var shift = Make<SiginakShift>("Shift controller");
            var close = Make<SiginakShiftClose>("Shift close");
            Set(surface, "siginak", desk);
            Set(desk, "shift", shift);
            Set(desk, "shiftClose", close);
            Set(shift, "close", close);
            Assert.IsFalse(surface.InputBlocked);
            Assert.IsFalse(surface.BlockedExceptShift);

            // Dosyalama (ya da fiş elde): masa kilitli, fişin kendisi değil
            var filing = typeof(SiginakShiftClose).GetField("filing", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(filing);
            filing.SetValue(close, true);
            Assert.IsTrue(close.Busy);
            Assert.IsTrue(shift.Busy, "SiginakShift.Busy kapanışı sayar");
            Assert.IsTrue(desk.ShiftBusy);
            // Masa denetleyicisinin kilidi yalnız etkinken sayılır (LiberationDeskView: siginak.isActiveAndEnabled)
            Assert.AreEqual(desk.isActiveAndEnabled, surface.InputBlocked);
            Assert.AreEqual(desk.isActiveAndEnabled, surface.BlockedExceptDoor);
            Assert.IsFalse(surface.BlockedExceptShift, "fişin kendisi tıklanır ve bırakılır kalır");

            // Başka bir kilit (açılış belgesi) fişi de kilitler
            var story = typeof(LiberationDeskView).GetField("storyOpen", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(story);
            story.SetValue(surface, true);
            Assert.IsTrue(surface.BlockedExceptShift);
            filing.SetValue(close, false);
            Assert.IsTrue(surface.BlockedExceptShift);
            Assert.IsTrue(surface.InputBlocked);
        }

        // ---------------------------------------------------------------- gerçek oyunda basım, ret, kilit

        [Test]
        public void PreCheckAndThrow_PrintOnce_SecondThrowRejectsWithFailedTelex()
        {
            var (game, close, telex, _) = BoardGame();
            var sim = game.Sim;
            Assert.IsNotNull(sim.Board, "kartlı oyun");
            Assert.IsNull(sim.BoardPlanCheck(), "V.001 planı geçerli");
            int commands = sim.Record.commands.Count, day = sim.S.Day;

            close.PreCheck();
            var pre = telex.Lines[telex.Lines.Count - 1];
            Assert.AreEqual("001/01", pre.code);
            Assert.AreEqual(Loc.T("ÖN KONTROL"), pre.status);
            Assert.AreEqual(Loc.T("Plan hazır. Devir fişini çıkar."), pre.message);
            Assert.AreEqual(commands, sim.Record.commands.Count, "ön kontrol komut yazmaz");

            close.Throw();
            Assert.IsTrue(sim.Board.Printed);
            Assert.AreEqual(commands + 1, sim.Record.commands.Count, "tam bir board_print");
            var print = sim.Record.commands[sim.Record.commands.Count - 1];
            Assert.AreEqual("board_print", print.kind);
            Assert.AreEqual(day, print.day);
            Assert.AreEqual(day, sim.S.Day, "basım günü ilerletmez");
            Assert.AreEqual(1, telex.Lines.Count, "başarılı basım teleks satırı basmaz");
            Assert.IsFalse(close.Busy);
            Assert.IsFalse(close.HoldCounters);

            // İki basış aynı karede (P8b): Core reddeder, kol boş ağızla kalkar, teleks YAPILAMADI
            uint rng = sim.S.Rng.State;
            close.Throw();
            Assert.AreEqual(commands + 1, sim.Record.commands.Count, "ret komut yazmaz");
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(day, sim.S.Day);
            var fail = telex.Lines[telex.Lines.Count - 1];
            Assert.AreEqual(Loc.T("YAPILAMADI"), fail.status);
            Assert.AreEqual(TelexKind.Failed, fail.kind);
            Assert.IsFalse(string.IsNullOrEmpty(fail.message), "Core'un nedeni");
        }

        [Test]
        public void UpdateLock_StoryAndInvalidPlanLock_PrintedNeverLocks()
        {
            var (game, close, _, sw) = BoardGame();
            close.UpdateLock();
            Assert.IsNull(sw.LockPlate, "V.001 hazır");
            Assert.AreNotEqual(ShiftPhase.Locked, sw.Phase);

            game.StoryHold = true;
            close.UpdateLock();
            Assert.AreEqual(Loc.T("belge açık"), sw.LockPlate);
            Assert.AreEqual(Loc.T("Önce açılış belgesini kapat."), sw.LockLine);
            Assert.AreEqual(ShiftPhase.Locked, sw.Phase);
            game.StoryHold = false;
            close.UpdateLock();
            Assert.IsNull(sw.LockPlate);
            Assert.IsNull(sw.LockLine);

            // D1-A: girdisiz iş emri planı bozar → kilit, uzun satır Core'un iletisi (önbellek komut / yükleme / dilde tazelenir)
            Assert.IsTrue(game.ExecuteBoardCommand(s => s.QueueProduction("emp_cell")).Ok);
            string issue = game.Sim.BoardPlanCheck();
            Assert.IsNotNull(issue, "girdisiz emir");
            close.Restore();
            close.UpdateLock();
            Assert.AreEqual(Loc.T("plan düzeltilmeli"), sw.LockPlate);
            Assert.AreEqual(issue, sw.LockLine);
            Assert.AreEqual(ShiftPhase.Locked, sw.Phase);
        }

        [Test]
        public void UpdateLock_WhilePrinted_NoLockEvenWithStoryHold()
        {
            var (game, close, _, sw) = BoardGame();
            close.Throw();
            Assert.IsTrue(game.Sim.Board.Printed);
            game.StoryHold = true;
            close.UpdateLock();
            Assert.IsNull(sw.LockPlate, "fiş bekliyor: kilit uygulanmaz");
            Assert.AreNotEqual(ShiftPhase.Locked, sw.Phase);
        }

        // ---------------------------------------------------------------- K6'nın Esc ve sağ tık sırası (el dışı)

        [Test]
        public void EscapeAndBack_OutOfHand_CloseCoverThenGoToDesk()
        {
            var root = new GameObject("Rig root");
            created.Add(root);
            Transform Anchor(string name, Vector3 pos)
            {
                var t = new GameObject(name).transform;
                t.SetParent(root.transform);
                t.position = pos;
                return t;
            }
            var camGo = new GameObject("Camera", typeof(Camera));
            camGo.transform.SetParent(root.transform);
            var rig = camGo.AddComponent<SiginakCameraRig>();
            rig.Configure(camGo.GetComponent<Camera>(), null, new[]
            {
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Desk, anchor = Anchor("K1", new Vector3(0, 1.52f, -0.15f)), fieldOfView = 56.7f, lensShiftY = -0.324f },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.ShiftClose, anchor = Anchor("K6", new Vector3(0.40f, 1.28f, 0.42f)), fieldOfView = 42 },
            }, new Vector2(36, 20.25f), 0.45f, 0.15f);
            var sw = Make<ShiftSwitch>("Shift switch");
            SetBool(sw, "shiftClose", true);
            var close = Make<SiginakShiftClose>("Shift close");
            Set(close, "rig", rig);
            Set(close, "shiftSwitch", sw);

            rig.Snap(SiginakPose.ShiftClose);
            sw.Press();
            Assert.AreEqual(ShiftSwitch.State.CoverOpen, sw.Current);
            Assert.IsTrue(close.HandleEscape(), "kapak açık → kapanır");
            Assert.AreEqual(ShiftSwitch.State.Idle, sw.Current);
            Assert.AreEqual(SiginakPose.ShiftClose, rig.Current, "kapak kapanınca kamera K6'da kalır (D6-A)");
            Assert.IsTrue(close.HandleEscape(), "kapak kapalı → K1");
            Assert.AreEqual(SiginakPose.Desk, rig.Current);
            Assert.IsFalse(close.HandleEscape(), "K1'de kapanışın işi yok (Esc öteki işleyicilere geçer)");

            rig.Snap(SiginakPose.ShiftClose);
            sw.Press();
            close.HandleBack();
            Assert.AreEqual(ShiftSwitch.State.Idle, sw.Current, "sağ tık aynı sıra: önce kapak");
            Assert.AreEqual(SiginakPose.ShiftClose, rig.Current);
            close.HandleBack();
            Assert.AreEqual(SiginakPose.Desk, rig.Current);
        }

        [Test]
        public void Unwired_CallsAreSafe()
        {
            var close = Make<SiginakShiftClose>("Shift close");
            Assert.DoesNotThrow(() =>
            {
                close.Restore();
                close.UpdateLock();
                close.PreCheck();
                close.Throw();
                close.HandleBack();
            });
            Assert.IsFalse(close.HandleEscape());
            Assert.IsFalse(close.OpenTray());
            Assert.IsFalse(close.Busy);
            Assert.IsFalse(close.Holding);
            Assert.IsFalse(close.HoldCounters);
            Assert.IsFalse(close.Filing);
            Assert.AreEqual(0, close.Archive.Count);
        }
    }
}
