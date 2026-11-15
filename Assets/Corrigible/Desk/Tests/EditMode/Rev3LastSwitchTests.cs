using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using K = Corrigible.Desk.Tests.KosDeskKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 6 (sunum · son şalter): kilidin öncelik sırası, çekilirse gelecek çeşidin önceden gösterilmesi, ÇEKİLME / TAHLİYE EMRİ
    /// kâğıdı (TR/EN) ve paylaşılan <see cref="ShiftSwitch"/>'in token öneki (masa şalteri ve dolap kolu değişmez). Saf okuma: kilit ve
    /// kâğıt komut yazmaz, RNG çekmez.
    /// </summary>
    public sealed class Rev3LastSwitchTests
    {
        const string TurkishOnly = "çğıöşüÇĞİÖŞÜ";

        [SetUp] public void Turkish() => DeskLoc.Instance.SetLanguage(DeskLang.Tr);
        [TearDown] public void Back() => DeskLoc.Instance.SetLanguage(DeskLang.Tr);

        static LastSwitchViewBuilder.LockInputs Ui(bool raid = false, bool tutorial = false, bool story = false, bool busy = false)
            => new LastSwitchViewBuilder.LockInputs { RaidLoaded = raid, Tutorial = tutorial, Story = story, DeskBusy = busy };

        [Test]
        public void Lock_PriorityOrder_AndShiftFive()
        {
            var sim = K.New();
            var (plate, line) = LastSwitchViewBuilder.Lock(sim, Ui());
            Assert.AreEqual(Loc.F("mühürlü · {0}. vardiya", sim.B.lastSwitchMinShift), plate, "1. vardiya: Core gerekçesi");
            Assert.AreEqual(sim.LastSwitchCheck(), line);
            Assert.AreEqual(Loc.T("masa meşgul"), LastSwitchViewBuilder.Lock(sim, Ui(busy: true)).plate, "masa meşgul Core gerekçesinden önce");
            Assert.AreEqual(Loc.T("belge açık"), LastSwitchViewBuilder.Lock(sim, Ui(story: true, busy: true)).plate);
            Assert.AreEqual(Loc.T("mühürlü · rehber"), LastSwitchViewBuilder.Lock(sim, Ui(tutorial: true, story: true, busy: true)).plate);
            Assert.AreEqual(Loc.T("Rehber bitene kadar son şalter mühürlü."), LastSwitchViewBuilder.Lock(sim, Ui(tutorial: true)).line);
            Assert.AreEqual(Loc.T("baskın sürüyor"), LastSwitchViewBuilder.Lock(sim, Ui(raid: true, tutorial: true, story: true, busy: true)).plate);

            K.CloseTo(sim, sim.B.lastSwitchMinShift - 1);
            Assert.IsNotNull(LastSwitchViewBuilder.Lock(sim, Ui()).plate, "4. vardiya kilitli");
            K.Close(sim);
            Assert.IsNull(LastSwitchViewBuilder.Lock(sim, Ui()).plate, "5. vardiya açık");
            Assert.AreEqual(Loc.T("mühürlü · rehber"), LastSwitchViewBuilder.Lock(sim, Ui(tutorial: true)).plate, "rehber sürerken 5. vardiyada da mühürlü");

            // Açık olay, basılmış fiş engel değildir (K6)
            sim.ShelterEvents.Pending = new ShelterEventInstance { Kind = "anma", OpenedShift = sim.S.Day, PersonId = sim.Board.People.First(p => p.Alive).Id };
            Assert.IsNull(LastSwitchViewBuilder.Lock(sim, Ui()).plate, "açık olay engel değil");
            sim.ShelterEvents.Pending = null;
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.IsNull(LastSwitchViewBuilder.Lock(sim, Ui()).plate, "basılmış fiş engel değil");

            sim.S.Ending = "salter";
            Assert.AreEqual(Loc.T("harekât bitti"), LastSwitchViewBuilder.Lock(sim, Ui(raid: true)).plate, "harekât bitti en önce");
        }

        [Test]
        public void Lock_And_Order_ArePure()
        {
            var sim = K.New();
            K.CloseTo(sim, sim.B.lastSwitchMinShift);
            uint rng = sim.S.Rng.State;
            int commands = sim.Record.commands.Count;
            string board = JsonUtility.ToJson(sim.Board);
            LastSwitchViewBuilder.Lock(sim, Ui());
            LastSwitchViewBuilder.Variant(sim);
            LastSwitchViewBuilder.Order(sim);
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(commands, sim.Record.commands.Count);
            Assert.AreEqual(board, JsonUtility.ToJson(sim.Board));
        }

        [Test]
        public void Variant_EachReasonAlone_AndTogether_ElseWithdrawal()
        {
            var sim = K.New();
            K.CloseTo(sim, sim.B.lastSwitchMinShift);
            var none = LastSwitchViewBuilder.Variant(sim);
            Assert.IsFalse(none.evac);
            Assert.AreEqual(Loc.T("çekilirse: ÇEKİLME"), none.plate);
            Assert.AreEqual(Loc.T("Çekilirse: düzenli çekilme. Yenilgi sayılmaz."), none.line);

            sim.S.Trust = sim.B.evacuationTrustBelow - 1;
            var trust = LastSwitchViewBuilder.Variant(sim);
            Assert.IsTrue(trust.evac);
            Assert.AreEqual(Loc.T("çekilirse: TAHLİYE"), trust.plate);
            Assert.AreEqual(Loc.F("Çekilirse: TAHLİYE ({0}). Yenilgi sayılır.", Loc.F("güven {0}", sim.S.Trust)), trust.line);
            sim.S.Trust = sim.B.liberationTrustStart;

            sim.Shelter.Exposure = sim.B.exposureThreshold;
            Assert.AreEqual(Loc.F("Çekilirse: TAHLİYE ({0}). Yenilgi sayılır.", Loc.T("konum çözüldü")), LastSwitchViewBuilder.Variant(sim).line);
            sim.Shelter.Exposure = 0;

            K.Understaffed(sim, 2);
            Assert.AreEqual(Loc.F("Çekilirse: TAHLİYE ({0}). Yenilgi sayılır.", Loc.F("sığınak eksik {0}/{1}", sim.UnderstaffedAtClose, sim.B.abandonUnderstaffedShifts)),
                LastSwitchViewBuilder.Variant(sim).line);

            sim.Shelter.Exposure = sim.B.exposureThreshold;
            sim.S.Trust = 1;
            var all = LastSwitchViewBuilder.Variant(sim).line;
            StringAssert.Contains(Loc.T("konum çözüldü"), all);
            StringAssert.Contains(Loc.F("güven {0}", 1), all);
            StringAssert.Contains(Loc.F("sığınak eksik {0}/{1}", sim.UnderstaffedAtClose, sim.B.abandonUnderstaffedShifts), all);
        }

        [Test]
        public void Order_Withdrawal_And_Evacuation_TrEn([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var sim = K.New();
            K.CloseTo(sim, sim.B.lastSwitchMinShift);
            var down = sim.Board.People.First(p => p.Alive);
            down.Down = 2;
            var w = LastSwitchViewBuilder.Order(sim);
            Assert.AreEqual(Loc.T("ÇEKİLME EMRİ"), w.Title);
            Assert.AreEqual(lang == DeskLang.En ? "WITHDRAWN" : "ÇEKİLDİ", w.Received, "sabit beklenen (paylaşılan anahtar yakalansın)");
            Assert.AreEqual(Loc.T("DAMGALA"), w.Action);
            Assert.AreEqual(Loc.T("Bu karar geri alınmaz."), w.Closing);
            Assert.IsTrue(w.NoDossier);
            Assert.LessOrEqual(w.Paragraphs.Length, 3, "emir belgesinde üç paragraf yuvası var");
            StringAssert.Contains(Loc.F("{0} (sedyeyle)", down.Name), w.Paragraphs[0]);
            StringAssert.StartsWith(Loc.F("Çıkacaklar ({0}): {1}", sim.Board.People.Count(p => p.Alive), ""), w.Paragraphs[0]);
            Assert.AreEqual(Loc.F("{0}\n{1}", Loc.T("Düzenli çekilme yenilgi sayılmaz."), Loc.T("Damgalamadan vazgeç: Esc ya da sağ tık.")), w.Paragraphs[2],
                "görünür vazgeçiş satırı");

            sim.S.Trust = 1;
            sim.Shelter.Exposure = sim.B.exposureThreshold;
            var e = LastSwitchViewBuilder.Order(sim);
            Assert.AreEqual(Loc.T("TAHLİYE EMRİ"), e.Title);
            Assert.AreEqual(lang == DeskLang.En ? "EVACUATED" : "TAHLİYE", e.Received, "baskının EXTRACTED'ı değil");
            StringAssert.Contains(Loc.T("Konum çözüldü: YZ kapıya yürüyor."), e.Paragraphs[2]);
            StringAssert.Contains(Loc.F("Güven {0}: halk sırtını döndü.", 1), e.Paragraphs[2]);
            StringAssert.EndsWith(Loc.F("{0}\n{1}", Loc.T("Tahliye yenilgi sayılır."), Loc.T("Damgalamadan vazgeç: Esc ya da sağ tık.")), e.Paragraphs[2]);
            if (lang == DeskLang.En)
                foreach (var s in new[] { e.Title, e.To, e.Re, e.Paragraphs[1], e.Paragraphs[2], e.Closing, e.SignOffice, e.Received, e.Action })
                    Assert.IsFalse(s.Any(c => TurkishOnly.IndexOf(c) >= 0), "İngilizcede Türkçe harf: " + s);
        }

        /// <summary>
        /// Gerekçe plakası: ilk satır şalterin adı (o anki dilde; İngilizcede oyunun adı "CORRIGIBLE" değil "LAST SWITCH"), kapak kalkıkken
        /// ve kol inikken de çeşit yazılır.
        /// </summary>
        [Test]
        public void Plate_NameLine_AndVariantWhileArmed([Values(DeskLang.Tr, DeskLang.En)] DeskLang lang)
        {
            DeskLoc.Instance.SetLanguage(lang);
            var go = new GameObject("plate test");
            try
            {
                var l1 = new GameObject("l1").AddComponent<TMPro.TextMeshPro>(); l1.transform.SetParent(go.transform);
                var l2 = new GameObject("l2").AddComponent<TMPro.TextMeshPro>(); l2.transform.SetParent(go.transform);
                var plate = go.AddComponent<LastSwitchPlate>();
                plate.Configure(l2, null, l1);
                plate.Show(ShiftPhase.CoverUp, null, null, Loc.T("çekilirse: TAHLİYE"), "x", true);
                Assert.AreEqual(lang == DeskLang.En ? "LAST SWITCH" : "SON ŞALTER", l1.text);
                Assert.AreEqual(lang == DeskLang.En ? "EVACUATE · pull" : "TAHLİYE · kolu indir", l2.text);
                plate.Show(ShiftPhase.Waiting, null, null, Loc.T("çekilirse: ÇEKİLME"), "x", false);
                Assert.AreEqual(Loc.T("ÇEKİLME · damgala"), l2.text);
                plate.Show(ShiftPhase.Ready, null, null, Loc.T("çekilirse: ÇEKİLME"), "x", false);
                Assert.AreEqual(Loc.T("çekilirse: ÇEKİLME"), l2.text);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Names_TruncatesWithPlusN()
        {
            var names = Enumerable.Range(1, 12).Select(i => "Uzunadlıkişi " + i).ToList();
            string s = FinalReportBuilder.Names(names, 60);
            Assert.LessOrEqual(s.Length, 70);
            StringAssert.Contains("… +", s);
            Assert.AreEqual("—", FinalReportBuilder.Names(new string[0], 60));
            Assert.AreEqual("A, B", FinalReportBuilder.Names(new[] { "A", "B" }, 60));
        }

        [Test]
        public void ShiftSwitch_StateTokens_DefaultUnchanged_LastSwitchFromTokens()
        {
            var theme = SiginakTheme.Active;
            Assume.That(theme != null, "SiginakTheme");
            var go = new GameObject("switch test");
            try
            {
                var hinge = new GameObject("cover").transform; hinge.SetParent(go.transform);
                var lever = new GameObject("lever").transform; lever.SetParent(go.transform);
                var sw = go.AddComponent<ShiftSwitch>();
                sw.Configure(hinge, lever, null, null, null, null, null);
                sw.ConfigureShiftClose(null, true);
                Assert.AreEqual("shiftClose", sw.StateTokens, "varsayılan önek: masa şalteri ve dolap kolu bugünkü anahtarları okur");
                sw.Advance(0);
                Assert.AreEqual(theme.NumberOf("shiftClose.states.ready.lever", 35), sw.LeverDeg, 0.01f);

                var so = new SerializedObject(sw);
                so.FindProperty("stateTokens").stringValue = "lastSwitch";
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual("lastSwitch", sw.StateTokens);
                sw.Press();
                sw.Advance(2);
                Assert.AreEqual(115, sw.CoverGoalDeg, 0.01f, "lastSwitch.states.cover.cover");
                sw.Press();
                sw.Advance(2);
                Assert.AreEqual(-55, sw.LeverGoalDeg, 0.01f, "lastSwitch.states.waiting.lever");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
