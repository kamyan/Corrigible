using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 2 · aday grubunun anlık görüntüsü (tasarım §2, §7, §8): gerçek kartlı simülasyondan 2 ve 3 kartlı grup (ad, ustalık,
    /// beden, kademe, şüphe satırı, özellikler Core metinleriyle birebir), TR/EN çeviri, bekleme süresi (2 → son vardiya → gitti), KABUL ve
    /// GERİ ÇEVİR damgalarının Core denetimleri (kapalı damganın gerekçesi Core iletisi), dolu sığınakta yollanabilir kişiler ve yolluk
    /// ön izlemesi, acil grup, damga izleri, emekli talimat ve araçlar. Kurucu saf: RNG çekmez, komut yazmaz, gizli kimliği okumaz
    /// (sızmacı olasılığı 0 ve 1 aynı görüntüyü verir). Sahneye bağlı değildir.
    /// </summary>
    public sealed class Rev3CandidateSnapshotTests
    {
        const string BuilderPath = "Assets/Corrigible/Desk/Scripts/Bridge/Siginak/AdmissionSnapshotBuilder.cs";

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- kurulum

        /// <summary>Kartlı oyun; <paramref name="knock"/> vardiyada bir grup. <paramref name="robot"/> ≥ 0 ise üç kademenin sızmacı olasılığı.</summary>
        static Simulation Game(uint seed = 79, int knock = 1, int capacity = 0, int emergencyBelow = -1, double robot = -1)
        {
            var b = new Balance { shelterEventsEnabled = false, planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = knock, admissionKnockMax = knock };
            if (capacity > 0) b.boardResidentCapacity = capacity;
            if (emergencyBelow >= 0) b.candidateEmergencyBelow = emergencyBelow;
            if (robot >= 0) b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = robot;
            return Simulation.NewGame(seed, b);
        }

        static void Close(Simulation sim)
        {
            var printed = sim.PrintShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        /// <summary>Kapıda grup olana dek kapatır (en çok 8 kapanış).</summary>
        static Simulation ToGroup(Simulation sim)
        {
            for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++) Close(sim);
            Assert.Greater(sim.Admission.Group.Count, 0, "kurulum: kapıda aday grubu");
            return sim;
        }

        static void Ok(ActResult r) => Assert.IsTrue(r.Ok, r.Msg);

        static AdmissionSnapshot Build(Simulation sim, AdmissionUi ui = null) => AdmissionSnapshotBuilder.Build(sim, ui ?? new AdmissionUi());

        static AdmissionUi Marked(Simulation sim, string personId) =>
            new AdmissionUi { ReplaceForGroup = AdmissionSnapshotBuilder.GroupKey(sim), ReplaceId = personId };

        /// <summary>Grubun (ve kartların, satırların) bütün public alanları; null ile "" ayrılır.</summary>
        static string Dump(object value)
        {
            var sb = new StringBuilder();
            Dump(value, sb);
            return sb.ToString();
        }

        static void Dump(object value, StringBuilder sb)
        {
            switch (value)
            {
                case null: sb.Append("∅"); return;
                case string s: sb.Append('"').Append(s).Append('"'); return;
                case IList list:
                    sb.Append('[');
                    foreach (var item in list) { Dump(item, sb); sb.Append(','); }
                    sb.Append(']');
                    return;
            }
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum) { sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            sb.Append(type.Name).Append('{');
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(x => x.MetadataToken))
            {
                sb.Append(field.Name).Append('=');
                Dump(field.GetValue(value), sb);
                sb.Append(';');
            }
            sb.Append('}');
        }

        /// <summary>Grubun oyuncuya görünen bütün metinleri (kimlik ve renk alanları hariç).</summary>
        static IEnumerable<string> Texts(AdmissionSnapshot s)
        {
            yield return s.CounterHoverTitle; yield return s.CounterHoverLine; yield return s.LockReason;
            foreach (var t in s.Stamps) { yield return t.Plate; yield return t.Cap; yield return t.Reason; yield return t.ShortReason; }
            var g = s.Group;
            if (g == null) yield break;
            foreach (string t in new[]
                     {
                         g.SlipHeader, g.SlipShift, g.SlipTitle, g.WaitLine, g.EmergencyLine, g.HintLine, g.RoomTitle, g.RoomHint, g.ReplaceLine, g.PassReason,
                         g.SlipHoverTitle, g.SlipHoverLine, g.RowHoverLine,
                     }) yield return t;
            if (g.PassStampLines != null) foreach (string t in g.PassStampLines) yield return t;
            foreach (var c in g.Cards)
            {
                foreach (string t in new[] { c.Name, c.Arrived, c.Mastery, c.Stats, c.TierName, c.Suspicion, c.HoverLine, c.ChooseReason }) yield return t;
                if (c.StampLines != null) foreach (string t in c.StampLines) yield return t;
                foreach (var p in c.Perks) { yield return p.Tag; yield return p.Name; yield return p.Effect; }
            }
            foreach (var r in g.Replace) { yield return r.Name; yield return r.Job; yield return r.Label; yield return r.Reason; }
        }

        static readonly System.Text.RegularExpressions.Regex TurkishLetters = new System.Text.RegularExpressions.Regex("[ğüşıöçĞÜŞİÖÇ]");

        // ---------------------------------------------------------------- kartlar

        /// <summary>
        /// İlk 40 tohumda 2 ve 3 kişilik gruplar: kart sayısı ve sırası Core grubuyla aynı; ad büyük harf, ustalık, beden, kademe adı ve
        /// rengi, şüphe satırı, özellik satırları (etiket GETİRİ / BEDEL, ad, etki) Core'un açık verisinden ve <c>CandidateData</c>
        /// metinlerinden birebir. Grup fişi: künye, geliş vardiyası, başlık, bekleme, ipucu, yer durumu.
        /// </summary>
        [Test]
        public void Group_TwoAndThreeCards_FieldsFromCore()
        {
            var sizes = new HashSet<int>();
            var tiers = new HashSet<int>();
            bool cost = false, twoPerks = false;
            for (uint seed = 1; seed <= 40; seed++)
            {
                var sim = ToGroup(Game(seed));
                var adm = sim.Admission;
                var s = Build(sim);
                var g = s.Group;
                Assert.IsNotNull(g, "tohum " + seed);
                Assert.IsTrue(s.Active);
                Assert.AreEqual(adm.Group.Count, g.Cards.Count, "tohum " + seed);
                Assert.That(g.Cards.Count, Is.InRange(2, 3), "tohum " + seed + ": grup 2–3 kişi");
                sizes.Add(g.Cards.Count);
                Assert.AreEqual("group:" + adm.GroupArrivalShift + ":" + adm.Group[0].Resident.Id, g.Key);
                Assert.AreEqual(AdmissionSnapshotBuilder.GroupKey(sim), g.Key);
                for (int i = 0; i < g.Cards.Count; i++)
                {
                    var c = adm.Group[i];
                    var r = c.Resident;
                    var card = g.Cards[i];
                    string at = "tohum " + seed + " kart " + i;
                    Assert.AreEqual(r.Id, card.Id, at);
                    Assert.AreEqual(DeskText.Upper(r.Name), card.Name, at);
                    Assert.AreEqual(Loc.F("V.{0:000}", c.ArrivalShift + 1), card.Arrived, at);
                    Assert.AreEqual(Loc.F("CAN {0}/{1} · HAR {2} · GÜÇ {3}", r.Hp, r.MaxHp, r.Move, r.Power), card.Stats, at);
                    StringAssert.StartsWith(PlanningBoardSnapshotBuilder.TopicLabel(r.TrainingSubject) + " · ", card.Mastery, at);
                    StringAssert.EndsWith(PlanningBoardSnapshotBuilder.TierWord(sim.BoardSkillTier(r, r.TrainingSubject)), card.Mastery, at);
                    Assert.AreEqual(c.Tier, card.Tier, at);
                    Assert.AreEqual(DeskText.Upper(CandidateData.TierName(c.Tier)), card.TierName, at);
                    Assert.AreEqual(new[] { "#24592E", "#854005", "#9A1E14" }[c.Tier], card.TierColor, at + ": kademe rengi temadan");
                    Assert.AreEqual(CandidateData.SuspicionLine(c.SuspicionKey, c.SuspicionArg), card.Suspicion, at);
                    Assert.IsFalse(string.IsNullOrEmpty(card.Suspicion), at);
                    CollectionAssert.Contains(CandidateData.Suspicions[c.Tier], c.SuspicionKey, at + ": şüphe satırı kademenin havuzundan");
                    Assert.AreEqual(PlanningBoardSnapshotBuilder.PortraitKeyOf(r.Name), card.PortraitKey, at);
                    Assert.AreEqual(c.Perks.Count, card.Perks.Count, at);
                    Assert.That(card.Perks.Count, Is.InRange(1, CandidateCardFace.MaxPerks), at + ": karta sığan özellik sayısı");
                    for (int k = 0; k < c.Perks.Count; k++)
                    {
                        var row = card.Perks[k];
                        Assert.AreEqual(c.Perks[k], row.Id, at);
                        Assert.AreEqual(CandidateData.IsCost(c.Perks[k]), row.Cost, at);
                        Assert.AreEqual(row.Cost ? "BEDEL" : "GETİRİ", row.Tag, at);
                        Assert.AreEqual(CandidateData.PerkName(c.Perks[k], sim.B), row.Name, at);
                        Assert.AreEqual(CandidateData.PerkEffect(c.Perks[k], sim.B, r.TrainingSubject), row.Effect, at);
                        cost |= row.Cost;
                    }
                    twoPerks |= c.Perks.Count == 2;
                    tiers.Add(c.Tier);
                    Assert.IsTrue(card.CanChoose, at + ": boş yer, kilit yok");
                    Assert.IsNull(card.ChooseReason, at);
                    Assert.AreEqual("KABUL damgasını bu karta bas.", card.HoverLine, at);
                    Assert.IsNull(card.StampChoiceId, at + ": iz yok");
                    Assert.IsNull(card.StampLines, at);
                }
                Assert.AreEqual(g.Cards.Count, g.Cards.Select(c => c.Id).Distinct().Count(), "tohum " + seed + ": kimlikler ayrı");
                Assert.Greater(g.Cards.Select(c => c.Tier).Distinct().Count(), 1, "tohum " + seed + ": grupta en az iki kademe");

                Assert.AreEqual("KYGA · KAPI FİŞİ", g.SlipHeader);
                Assert.AreEqual(Loc.F("V.{0:000}", adm.GroupArrivalShift + 1), g.SlipShift);
                Assert.AreEqual(g.Cards.Count + " ADAY", g.SlipTitle);
                Assert.AreEqual("bekleme: 2 vardiya", g.WaitLine);
                Assert.IsFalse(g.Emergency);
                Assert.IsNull(g.EmergencyLine);
                Assert.AreEqual("En çok biri alınır. KABUL: bir karta. GERİ ÇEVİR: bu fişe.", g.HintLine);
                Assert.IsFalse(g.Full);
                Assert.AreEqual("YER VAR · 8/10", g.RoomTitle);
                Assert.IsNull(g.RoomHint);
                Assert.IsEmpty(g.Replace, "boş yer varken yollanacak kişi listesi yok");
                Assert.IsNull(g.ReplaceId);
                Assert.IsNull(g.ReplaceLine);
                Assert.IsTrue(g.CanPass);
                Assert.IsNull(g.PassReason);
                Assert.AreEqual("KAPI FİŞİ", g.SlipHoverTitle);
                Assert.AreEqual("GERİ ÇEVİR damgasını bu fişe bas.", g.SlipHoverLine);
                Assert.IsNull(g.PassStampLines);
                Assert.AreEqual(CounterState.Down, s.Counter);
                Assert.AreEqual(g.Cards.Count + " aday bekliyor", s.CounterHoverLine);
            }
            CollectionAssert.AreEquivalent(new[] { 2, 3 }, sizes, "40 tohumda 2 ve 3 kişilik gruplar");
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, tiers, "üç kademe de görüldü");
            Assert.IsTrue(cost, "en az bir BEDEL satırı görüldü");
            Assert.IsTrue(twoPerks, "en az bir iki özellikli (şüpheli) kart görüldü");
        }

        /// <summary>İngilizcede grubun hiçbir metni Türkçe kalmaz (adlar hariç); TR ve EN görüntüleri aynı kimlikleri taşır.</summary>
        [Test]
        public void Group_TrEn_Translated_NoTurkishInEnglish()
        {
            foreach (var sim in new[] { ToGroup(Game(79)), ToGroup(Game(3)), ToGroup(Game(79, capacity: 8, emergencyBelow: 9)) })
            {
                string last = sim.Board.People.Last(p => p.Alive).Id;
                bool full = sim.Board.People.Count(p => p.Alive) >= sim.B.boardResidentCapacity;
                var ui = full ? Marked(sim, last) : new AdmissionUi();
                var tr = Build(sim, ui);
                AdmissionSnapshotBuilder.StampCard(tr.Group, tr.Group.Cards[0].Id, sim.B);
                AdmissionSnapshotBuilder.StampPass(tr.Group);
                Rev3AdmissionEnglish.Use();
                var en = Build(sim, ui);
                AdmissionSnapshotBuilder.StampCard(en.Group, en.Group.Cards[0].Id, sim.B);
                AdmissionSnapshotBuilder.StampPass(en.Group);
                Loc.SetLanguage(Loc.Turkish);

                CollectionAssert.AreEqual(tr.Group.Cards.Select(c => c.Id + c.Tier + c.TierColor), en.Group.Cards.Select(c => c.Id + c.Tier + c.TierColor));
                CollectionAssert.AreEqual(tr.Group.Replace.Select(r => r.Id + r.Selected + r.Enabled), en.Group.Replace.Select(r => r.Id + r.Selected + r.Enabled));
                var names = sim.Board.People.Select(p => p.Name).Concat(sim.Admission.Group.Select(c => c.Resident.Name)).OrderByDescending(n => n.Length).ToList();
                foreach (string text in Texts(en).Where(t => !string.IsNullOrEmpty(t)))
                {
                    string rest = text;
                    foreach (string n in names) rest = rest.Replace(n, "").Replace(DeskText.Upper(n, DeskLang.En), "").Replace(DeskText.Upper(n, DeskLang.Tr), "");
                    Assert.IsFalse(TurkishLetters.IsMatch(rest), "EN'de Türkçe kaldı: " + text);
                }
                Assert.AreEqual("KYGA · DOOR SLIP", en.Group.SlipHeader);
                Assert.AreEqual(en.Group.Cards.Count + " CANDIDATES", en.Group.SlipTitle);
                StringAssert.StartsWith("S.", en.Group.SlipShift);
                StringAssert.StartsWith("S.", en.Group.Cards[0].Arrived);
                Assert.AreEqual(en.Group.Cards.Count + " candidates waiting", en.CounterHoverLine);
                CollectionAssert.AreEqual(new[] { "ADMIT", "FULL ACCESS" }, en.Group.Cards[0].StampLines);
                CollectionAssert.AreEqual(new[] { "TURNED AWAY" }, en.Group.PassStampLines);
                Assert.AreEqual("TURN\nAWAY", en.Stamps[(int)StampChoice.Reject].Plate);
                Assert.AreEqual("AWAY", en.Stamps[(int)StampChoice.Reject].Cap);
                foreach (var c in en.Group.Cards)
                    foreach (var p in c.Perks) Assert.That(p.Tag, Is.EqualTo("GAIN").Or.EqualTo("COST"));
                if (full)
                {
                    StringAssert.StartsWith("NO ROOM · ", en.Group.RoomTitle);
                    StringAssert.Contains("travel rations", en.Group.ReplaceLine);
                    StringAssert.StartsWith("URGENT", en.Group.EmergencyLine);
                    Assert.AreEqual("YER YOK · 8/8", tr.Group.RoomTitle);
                }
                else StringAssert.StartsWith("ROOM · ", en.Group.RoomTitle);
            }
        }

        // ---------------------------------------------------------------- saflık ve gizlilik

        /// <summary>Kurucu saf: RNG durumu, komut sayısı ve grup değişmez; iki çağrı (ve dil gidiş-dönüşü) aynı dökümü verir.</summary>
        [Test]
        public void Build_IsPure_NoRngNoCommands()
        {
            foreach (var sim in new[] { ToGroup(Game(79)), ToGroup(Game(79, capacity: 8)) })
            {
                uint rng = sim.S.Rng.State;
                int commands = sim.Record.commands.Count, day = sim.S.Day, passed = sim.Admission.Passed.Count;
                string ids = string.Join(",", sim.Admission.Group.Select(c => c.Resident.Id));
                double supplies = sim.Shelter.Supplies;
                var ui = Marked(sim, sim.Board.People.First(p => p.Alive).Id);
                string first = Dump(Build(sim, ui).Group);
                for (int i = 0; i < 10; i++) Assert.AreEqual(first, Dump(Build(sim, ui).Group));
                Rev3AdmissionEnglish.Use();
                string english = Dump(Build(sim, ui).Group);
                Loc.SetLanguage(Loc.Turkish);
                Assert.AreNotEqual(first, english, "dil metinleri değiştirir");
                Assert.AreEqual(first, Dump(Build(sim, ui).Group), "dil dönünce aynı");
                Assert.AreEqual(rng, sim.S.Rng.State, "RNG çekilmedi");
                Assert.AreEqual(commands, sim.Record.commands.Count, "komut yazılmadı");
                Assert.AreEqual(day, sim.S.Day);
                Assert.AreEqual(passed, sim.Admission.Passed.Count);
                Assert.AreEqual(ids, string.Join(",", sim.Admission.Group.Select(c => c.Resident.Id)));
                Assert.AreEqual(supplies, sim.Shelter.Supplies, "yolluk ön izlemesi erzak harcamaz");
                Assert.AreEqual(sim.Board.People.First(p => p.Alive).Id, ui.ReplaceId, "kurucu sunum durumunu da yazmaz");
            }
        }

        /// <summary>
        /// §8 gizlilik: aynı tohumda sızmacı olasılıkları 0 ve 1 → art arda üç gelişte (her biri geri çevrilir) grup, fiş ve damgaların
        /// dökümü birebir aynı (TR ve EN). Kurucunun kaynağı gizli alan adlarını hiç anmaz.
        /// </summary>
        [Test]
        public void HiddenIdentity_SameSnapshot()
        {
            foreach (uint seed in new uint[] { 79, 3, 11 })
            {
                var human = Game(seed, robot: 0);
                var robot = Game(seed, robot: 1);
                for (int arrival = 0; arrival < 3; arrival++)
                {
                    ToGroup(human);
                    ToGroup(robot);
                    Assert.AreEqual(human.S.Day, robot.S.Day);
                    foreach (bool english in new[] { false, true })
                    {
                        if (english) Rev3AdmissionEnglish.Use(); else Loc.SetLanguage(Loc.Turkish);
                        var a = Build(human);
                        var b = Build(robot);
                        string at = "tohum " + seed + " geliş " + arrival + (english ? " EN" : " TR");
                        Assert.AreEqual(Dump(a.Group), Dump(b.Group), at);
                        Assert.AreEqual(Dump(a.Stamps), Dump(b.Stamps), at);
                        Assert.AreEqual(a.CounterHoverLine, b.CounterHoverLine, at);
                    }
                    Loc.SetLanguage(Loc.Turkish);
                    // bir vardiya bekletilir (son vardiya görüntüsü de aynı), sonra geri çevrilir
                    Close(human);
                    Close(robot);
                    Assert.AreEqual(Dump(Build(human).Group), Dump(Build(robot).Group), "tohum " + seed + " son vardiya");
                    Ok(human.PassCandidates());
                    Ok(robot.PassCandidates());
                }
            }

            Assert.IsTrue(File.Exists(BuilderPath), BuilderPath);
            string source = File.ReadAllText(BuilderPath);
            foreach (string hidden in new[] { "Identity", ".Robot", "TraceDelay", "HumanTrace", "SecondTraceGap", "Sabotaged", "ActiveShifts", "LastAccessJob", ".Passed", "Rng" })
                Assert.IsFalse(source.Contains(hidden), "Kabul kurucusu gizli durumu okumaz: " + hidden);
            Assert.IsFalse(source.Contains("NextArrivalShift"), "kurucu sıradaki gelişi okumaz");
            foreach (string command in new[] { "ChooseCandidate(", "PassCandidates(", "AssignResident(" })
                Assert.IsFalse(source.Contains(command), "kurucu komut çağırmaz: " + command);
        }

        /// <summary>Grubun, damgaların ve araçların hiçbir metni kimliği ele veren söz taşımaz (TR ve EN; sızmacı olasılığı 1).</summary>
        [Test]
        public void Snapshot_NeverSaysRobot_TrEn()
        {
            foreach (uint seed in new uint[] { 79, 3, 11, 5 })
            {
                var sim = Game(seed, robot: 1);
                ToGroup(sim);
                Ok(sim.PassCandidates());
                ToGroup(sim);   // ikinci grup: sızmacı taşıyabilir
                foreach (bool english in new[] { false, true })
                {
                    if (english) Rev3AdmissionEnglish.Use(); else Loc.SetLanguage(Loc.Turkish);
                    var s = Build(sim);
                    AdmissionSnapshotBuilder.StampCard(s.Group, s.Group.Cards[0].Id, sim.B);
                    AdmissionSnapshotBuilder.StampPass(s.Group);
                    foreach (string text in Texts(s).Where(t => !string.IsNullOrEmpty(t)))
                        foreach (string banned in new[] { "robot", "sızmacı", "sizmaci", "infiltrator", "R-4", "S-6", "android", "makine", "machine" })
                            Assert.IsTrue(text.IndexOf(banned, StringComparison.OrdinalIgnoreCase) < 0, (english ? "EN: " : "TR: ") + text + " ← " + banned);
                }
                Loc.SetLanguage(Loc.Turkish);
            }
        }

        // ---------------------------------------------------------------- bekleme

        /// <summary>
        /// Bekleme: geliş vardiyasında 2 vardiya ("bekleme: 2 vardiya"), sonraki vardiyada 1 ve SON VARDİYA; bir kapanış daha → grup yok,
        /// tezgâh katlı, "aday yok". Grup aynı kaldıkça anahtar da aynıdır.
        /// </summary>
        [Test]
        public void Wait_TwoThenLastShift_ThenGone()
        {
            var sim = ToGroup(Game(79, knock: 3));
            var g = Build(sim).Group;
            string key = g.Key;
            Assert.AreEqual(2, g.WaitLeft);
            Assert.IsFalse(g.LastShift);
            Assert.AreEqual("bekleme: 2 vardiya", g.WaitLine);
            Assert.IsFalse(AdmissionSnapshotBuilder.GroupLastShift(sim));

            Close(sim);
            g = Build(sim).Group;
            Assert.IsNotNull(g, "ikinci vardiya: grup bekliyor");
            Assert.AreEqual(key, g.Key, "aynı grup, aynı anahtar");
            Assert.AreEqual(1, g.WaitLeft);
            Assert.IsTrue(g.LastShift);
            Assert.AreEqual("SON VARDİYA · karar yoksa giderler", g.WaitLine);
            Assert.IsTrue(AdmissionSnapshotBuilder.GroupLastShift(sim));
            Assert.IsTrue(g.Cards.All(c => c.CanChoose), "son vardiyada da seçilebilir");
            Assert.AreEqual(g.Cards[0].Arrived, g.SlipShift, "geliş vardiyası kartta ve fişte aynı");

            Close(sim);
            var s = Build(sim);
            Assert.IsNull(s.Group, "süre doldu");
            Assert.AreEqual(CounterState.Folded, s.Counter);
            Assert.AreEqual("aday yok", s.CounterHoverLine);
            Assert.IsNull(AdmissionSnapshotBuilder.GroupKey(sim));
            Assert.IsFalse(AdmissionSnapshotBuilder.GroupWaiting(sim));
            Assert.IsFalse(AdmissionSnapshotBuilder.GroupLastShift(sim));
        }

        /// <summary>Tezgâh yalnız grup beklerken inik; karardan (seçim / geri çevirme) sonra ve harekât bitince katlı.</summary>
        [Test]
        public void CounterDown_OnlyWhileAGroupWaits()
        {
            var sim = Game(79);
            Assert.AreEqual(CounterState.Folded, Build(sim).Counter, "gün 0");
            Assert.IsNull(Build(sim).Group);
            ToGroup(sim);
            Assert.AreEqual(CounterState.Down, Build(sim).Counter);
            Ok(sim.PassCandidates());
            Assert.AreEqual(CounterState.Folded, Build(sim).Counter, "geri çevrildi");
            Close(sim);
            Assert.AreEqual(CounterState.Down, Build(sim).Counter, "yeni grup");
            Ok(sim.ChooseCandidate(sim.Admission.Group[0].Resident.Id));
            Assert.AreEqual(CounterState.Folded, Build(sim).Counter, "seçildi");
            Close(sim);
            Assert.IsNotNull(Build(sim).Group);
            sim.S.Ending = "vuruldu";
            var ended = Build(sim);
            Assert.IsNull(ended.Group, "harekât bitti: grup görünmez");
            Assert.AreEqual(CounterState.Folded, ended.Counter);
            Assert.IsFalse(AdmissionSnapshotBuilder.GroupWaiting(sim));

            Assert.IsNull(Build(LegacySimulation.NewGame(79)).Group, "eski kip: kabul yok");
            Assert.IsFalse(Build(LegacySimulation.NewGame(79)).Active);
        }

        /// <summary>
        /// Aşama 7: eski tek adaylı kabulün alanları (belge, talimat şeridi, araçlar, fişler, talep) anlık görüntüden söküldü; sözcüklerde
        /// yalnız çekmece, tezgâh şablonu ve MASA kalır.
        /// </summary>
        [Test]
        public void RetiredFields_Gone()
        {
            foreach (string name in new[] { "CandidateId", "Document", "DocumentOnCounter", "Orders", "Archive", "OrdersUnseen", "Tools", "Slips", "Request", "BreathFog", "MarkerRule" })
                Assert.IsNull(typeof(AdmissionSnapshot).GetField(name), "AdmissionSnapshot." + name);
            CollectionAssert.AreEquivalent(new[] { "Drawer", "Under", "Desk" }, typeof(AdmissionWords).GetFields().Select(f => f.Name));
            var s = Build(ToGroup(Game(79)));
            Assert.IsNotNull(s.Group);
        }

        // ---------------------------------------------------------------- damgalar

        /// <summary>
        /// KABUL ve GERİ ÇEVİR Core denetimleridir: boş yerde ikisi açık; fiş basılıyken ve baskında ikisi kapalı, gerekçe Core iletisi
        /// (kartlarda ve fişin etiketinde de); KISITLI ve YARDIM'ın damgası yok (dizide yeri metinsiz ve kapalı); grup yokken
        /// gerekçe "Kapıda aday yok.".
        /// </summary>
        [Test]
        public void Stamps_AdmitAndPass_AreCoreChecks()
        {
            var sim = ToGroup(Game(79));
            string id = sim.Admission.Group[0].Resident.Id;
            var s = Build(sim);
            var admit = s.Stamps[(int)StampChoice.Admit];
            var reject = s.Stamps[(int)StampChoice.Reject];
            Assert.IsTrue(admit.Enabled);
            Assert.IsNull(admit.Reason);
            Assert.IsNull(admit.ShortReason);
            Assert.IsTrue(reject.Enabled);
            Assert.IsNull(reject.Reason);
            Assert.IsNull(sim.CandidateChoiceCheck(id, null));
            Assert.IsNull(sim.CandidatePassCheck());
            Assert.IsFalse(s.Locked);
            Assert.AreEqual("KABUL ·\nTAM ERİŞİM", admit.Plate);
            Assert.AreEqual("GERİ\nÇEVİR", reject.Plate, "raf plakası (TXT_S3 ile aynı)");
            Assert.AreEqual("GERİ", reject.Cap);
            Assert.AreEqual("#24592E", admit.BandColor);
            Assert.AreEqual("#9A1E14", reject.BandColor);
            foreach (var retired in new[] { StampChoice.Restricted, StampChoice.Aid })
            {
                Assert.IsFalse(s.Stamps[(int)retired].Enabled, retired.ToString());
                Assert.IsNull(s.Stamps[(int)retired].Reason, retired + ": metin yok");
                Assert.IsNull(s.Stamps[(int)retired].BandColor, retired + ": renk yok");
                Assert.AreEqual("", s.Stamps[(int)retired].Plate, retired + ": raf plakası yok");
            }

            // Fiş basıldı: kapı bu vardiya kapalı
            Ok(sim.PrintShift());
            var printed = Build(sim);
            string why = sim.CandidatePassCheck();
            Assert.AreEqual("Fiş basıldı; kapı bu vardiya kapalı.", why);
            Assert.AreEqual(why, sim.CandidateChoiceCheck(id, null));
            Assert.IsTrue(printed.Locked);
            Assert.AreEqual(why, printed.LockReason);
            foreach (var choice in new[] { StampChoice.Admit, StampChoice.Reject })
            {
                Assert.IsFalse(printed.Stamps[(int)choice].Enabled, choice.ToString());
                Assert.AreEqual(why, printed.Stamps[(int)choice].Reason, choice + ": gerekçe Core iletisi");
                Assert.IsNull(printed.Stamps[(int)choice].ShortReason, "kilitte kısa neden yok (YER YOK değil)");
            }
            Assert.IsNotNull(printed.Group, "grup görünür kalır (okuma serbest)");
            Assert.IsTrue(printed.Group.Cards.All(c => !c.CanChoose && c.ChooseReason == why && c.HoverLine == why));
            Assert.IsFalse(printed.Group.CanPass);
            Assert.AreEqual(why, printed.Group.PassReason);
            Assert.AreEqual(why, printed.Group.SlipHoverLine);
            Assert.AreEqual(CounterState.Down, printed.Counter);
            Ok(sim.FileShift(sim.Board.ReceiptId));
            Assert.IsTrue(Build(sim).Stamps[(int)StampChoice.Admit].Enabled, "yeni vardiyada yine açık");

            // Baskın
            var raid = ToGroup(Game(79));
            Ok(raid.StartLiberationRaid("weu_1"));
            var during = Build(raid);
            Assert.IsTrue(during.Locked);
            Assert.AreEqual("Baskın sürüyor.", during.LockReason);
            Assert.IsTrue(new[] { StampChoice.Admit, StampChoice.Reject }.All(c => !during.Stamps[(int)c].Enabled && during.Stamps[(int)c].Reason == "Baskın sürüyor."));
            Assert.IsTrue(during.Group.Cards.All(c => !c.CanChoose && c.ChooseReason == "Baskın sürüyor."));

            // Grup yok
            var empty = Game(79);
            var none = Build(empty);
            Assert.AreEqual("Kapıda aday yok.", empty.CandidatePassCheck());
            foreach (var choice in new[] { StampChoice.Admit, StampChoice.Reject })
            {
                Assert.IsFalse(none.Stamps[(int)choice].Enabled, choice.ToString());
                Assert.AreEqual("Kapıda aday yok.", none.Stamps[(int)choice].Reason, choice.ToString());
            }

            // Eski kip
            var legacy = Build(LegacySimulation.NewGame(79));
            Assert.IsTrue(legacy.Stamps.All(x => !x.Enabled));
            Assert.AreEqual("Bu kayıtta topluluk kabulü yok.", legacy.Stamps[(int)StampChoice.Admit].Reason);
        }

        /// <summary>
        /// Dolu sığınak (kapasite 8, kurucular): YER YOK, sekiz satır (pano sırası, hepsi seçilebilir), KABUL kapalı ve kısa nedeni
        /// "YER YOK 8/8", kartların gerekçesi Core'un "Sığınak dolu…" iletisi; GERİ ÇEVİR açık. Bir kişi işaretlenince KABUL açılır,
        /// satır seçili görünür ve özet yolluğu (<c>DismissSuppliesPreview</c>) yazar. Panoda olmayan işaret düşer; başka grubun işareti sayılmaz.
        /// </summary>
        [Test]
        public void Full_AdmitLockedUntilReplaceChosen()
        {
            var sim = ToGroup(Game(79, capacity: 8));
            var alive = sim.Board.People.Where(p => p.Alive).ToList();
            Assert.AreEqual(8, alive.Count);
            string id = sim.Admission.Group[0].Resident.Id;
            var s = Build(sim);
            var g = s.Group;
            Assert.IsTrue(g.Full);
            Assert.AreEqual("YER YOK · 8/8", g.RoomTitle);
            Assert.AreEqual("Aday için birini yolla: satıra tıkla. Yolluk: 2 erzak.", g.RoomHint);
            Assert.AreEqual(2, sim.DismissSuppliesPreview, 1e-9);
            CollectionAssert.AreEqual(alive.Select(p => p.Id), g.Replace.Select(r => r.Id), "satırlar pano sırasıyla, yalnız canlılar");
            Assert.LessOrEqual(g.Replace.Count, CandidateGroupSlipFace.MaxRows);
            for (int i = 0; i < alive.Count; i++)
            {
                var row = g.Replace[i];
                Assert.AreEqual(alive[i].Name, row.Name);
                Assert.IsFalse(string.IsNullOrEmpty(row.Job), row.Name + ": yer");
                Assert.AreEqual(row.Name + " · " + row.Job, row.Label);
                Assert.IsTrue(row.Enabled, row.Name);
                Assert.IsNull(row.Reason);
                Assert.IsFalse(row.Selected);
                Assert.IsNull(sim.CandidateChoiceCheck(id, row.Id), row.Name + ": Core bu kişiyi yollanabilir sayar");
            }
            Assert.AreEqual("Tıkla: yollanacak kişi olarak işaretle.", g.RowHoverLine);
            Assert.IsNull(g.ReplaceId);
            Assert.IsNull(g.ReplaceLine);

            string full = sim.CandidateChoiceCheck(id, null);
            Assert.AreEqual("Sığınak dolu (8); aday için yollanacak kişiyi seç.", full);
            var admit = s.Stamps[(int)StampChoice.Admit];
            Assert.IsFalse(admit.Enabled);
            Assert.AreEqual(full, admit.Reason);
            Assert.AreEqual("YER YOK 8/8", admit.ShortReason);
            Assert.IsTrue(g.Cards.All(c => !c.CanChoose && c.ChooseReason == full && c.HoverLine == full));
            Assert.IsTrue(s.Stamps[(int)StampChoice.Reject].Enabled, "geri çevirme dolu sığınakta da açık");
            Assert.IsTrue(g.CanPass);
            Assert.IsFalse(s.Locked, "dolu sığınak kilit değildir");

            // İşaret
            var leaver = alive[4];
            var marked = Build(sim, Marked(sim, leaver.Id));
            Assert.AreEqual(leaver.Id, marked.Group.ReplaceId);
            Assert.IsTrue(marked.Group.Replace.Single(r => r.Selected).Id == leaver.Id);
            Assert.IsTrue(marked.Stamps[(int)StampChoice.Admit].Enabled);
            Assert.IsNull(marked.Stamps[(int)StampChoice.Admit].Reason);
            Assert.IsNull(marked.Stamps[(int)StampChoice.Admit].ShortReason);
            Assert.IsTrue(marked.Group.Cards.All(c => c.CanChoose && c.ChooseReason == null));
            Assert.AreEqual(leaver.Name + " yollanacak · yolluk 2 erzak", marked.Group.ReplaceLine);
            Assert.IsFalse(marked.Group.Replace.Single(r => r.Selected).BoundGear);

            // Yolluk stoktan büyük olamaz (ön izleme eldeki kadar)
            double supplies = sim.Shelter.Supplies;
            sim.Shelter.Supplies = 0.5;
            Assert.AreEqual(leaver.Name + " yollanacak · yolluk 0,5 erzak", Build(sim, Marked(sim, leaver.Id)).Group.ReplaceLine);
            sim.Shelter.Supplies = supplies;

            // Geçersiz işaretler
            Assert.IsNull(Build(sim, Marked(sim, "resident-yok")).Group.ReplaceId, "panoda olmayan kişi");
            Assert.IsNull(Build(sim, new AdmissionUi { ReplaceForGroup = "group:0:baska", ReplaceId = leaver.Id }).Group.ReplaceId, "başka grubun işareti");
            Assert.IsFalse(Build(sim, Marked(sim, "resident-yok")).Stamps[(int)StampChoice.Admit].Enabled);

            // Komut: Core aynı kişiyle kabul eder; sonraki görüntüde grup yok
            Ok(sim.ChooseCandidate(id, leaver.Id));
            Assert.IsNull(Build(sim, Marked(sim, leaver.Id)).Group);
            Assert.AreEqual(supplies - 2, sim.Shelter.Supplies, 1e-9);

            // Boş yer varken işaret yok sayılır (Core "Boş yer varken kimse yollanamaz." derdi)
            var roomy = ToGroup(Game(79));
            var ignored = Build(roomy, Marked(roomy, roomy.Board.People[0].Id));
            Assert.IsNull(ignored.Group.ReplaceId);
            Assert.IsEmpty(ignored.Group.Replace);
            Assert.IsTrue(ignored.Group.Cards.All(c => c.CanChoose));
        }

        /// <summary>
        /// Bağlı teçhizat: kitiyle alınmış bir kişi yollanacak olarak işaretlenince özet "bağlı teçhizatı onunla gider" der
        /// (<c>BoardEquipment.Bound</c>; açık durum).
        /// </summary>
        [Test]
        public void Full_BoundGear_SaidOnTheReplaceLine()
        {
            for (uint seed = 1; seed <= 60; seed++)
            {
                var sim = ToGroup(Game(seed, capacity: 9));
                var withGear = sim.Admission.Group.FirstOrDefault(c => c.Perks.Contains("kit") || c.Perks.Contains("zirh"));
                if (withGear == null) continue;
                string id = withGear.Resident.Id;
                Ok(sim.ChooseCandidate(id));
                Assert.IsTrue(sim.Board.Equipment.Any(e => e.OwnerId == id && e.Bound), "kurulum: kişiye bağlı teçhizat");
                Assert.AreEqual(9, sim.Board.People.Count(p => p.Alive));
                ToGroup(sim);
                var g = Build(sim, Marked(sim, id)).Group;
                Assert.IsTrue(g.Full);
                var row = g.Replace.Single(r => r.Id == id);
                Assert.IsTrue(row.BoundGear);
                Assert.IsTrue(row.Selected);
                Assert.AreEqual(withGear.Resident.Name + " yollanacak · yolluk 2 erzak · bağlı teçhizatı onunla gider", g.ReplaceLine);
                Assert.IsTrue(g.Replace.Where(r => r.Id != id).All(r => !r.BoundGear), "kurucuların teçhizatı bağlı değil");
                return;
            }
            Assert.Fail("60 tohumda kit ya da zırhla gelen aday bulunamadı");
        }

        /// <summary>
        /// Acil grup (canlı sayısı eşiğin altında): hep üç kart, ACİL satırı fişte, her kartta çekilen özelliklere ek olarak
        /// <c>kit</c> satırı ("Kendi kitiyle gelir"); özellik sayısı karta sığar (≤ 3).
        /// </summary>
        [Test]
        public void Emergency_ThreeCards_KitOnTop()
        {
            int three = 0;
            for (uint seed = 1; seed <= 20; seed++)
            {
                var sim = ToGroup(Game(seed, emergencyBelow: 9));
                Assert.IsTrue(sim.Admission.GroupEmergency, "tohum " + seed);
                var g = Build(sim).Group;
                Assert.IsTrue(g.Emergency);
                Assert.AreEqual("ACİL · kadro eksik: her aday kendi kitiyle geldi", g.EmergencyLine);
                Assert.AreEqual(3, g.Cards.Count, "acil grup hep üç kişi");
                Assert.AreEqual("3 ADAY", g.SlipTitle);
                foreach (var card in g.Cards)
                {
                    Assert.AreEqual("kit", card.Perks.Last().Id, card.Name + ": kit en sonda, ek olarak");
                    Assert.AreEqual("Kendi kitiyle gelir", card.Perks.Last().Name);
                    Assert.IsFalse(card.Perks.Last().Cost);
                    Assert.That(card.Perks.Count, Is.InRange(2, CandidateCardFace.MaxPerks), card.Name);
                    if (card.Perks.Count == 3) three++;
                }
            }
            Assert.Greater(three, 0, "üç özellikli (şüpheli + kit) kart görüldü: kartın en dolu hâli");
        }

        /// <summary>Damga izleri komuttan önceki görüntüye yazılır: KABUL izi yalnız seçilen kartta, GERİ ÇEVİR izi fişte; renkler temadan.</summary>
        [Test]
        public void StampCard_And_StampPass_WriteImpressions()
        {
            var sim = ToGroup(Game(3));
            var s = Build(sim);
            var g = s.Group;
            string id = g.Cards[1].Id;
            AdmissionSnapshotBuilder.StampCard(g, id, sim.B);
            Assert.AreEqual("admit", g.Cards[1].StampChoiceId);
            CollectionAssert.AreEqual(new[] { "KABUL", "TAM ERİŞİM" }, g.Cards[1].StampLines);
            Assert.AreEqual("#24592E", g.Cards[1].StampColor);
            Assert.IsTrue(g.Cards.Where(c => c.Id != id).All(c => c.StampChoiceId == null && c.StampLines == null && c.StampColor == null));
            Assert.IsNull(g.PassStampLines);

            AdmissionSnapshotBuilder.StampCard(g, "resident-yok", sim.B);
            AdmissionSnapshotBuilder.StampCard(null, id, sim.B);
            Assert.AreEqual(1, g.Cards.Count(c => c.StampChoiceId != null), "bilinmeyen kimlik iz bırakmaz");

            AdmissionSnapshotBuilder.StampPass(g);
            CollectionAssert.AreEqual(new[] { "GERİ ÇEVRİLDİ" }, g.PassStampLines);
            Assert.AreEqual("#9A1E14", g.PassStampColor);
            AdmissionSnapshotBuilder.StampPass(null);

            // İz yalnız görüntüde: Core'a dokunmaz, yeni görüntü izsiz
            Assert.AreEqual(g.Cards.Count, sim.Admission.Group.Count);
            var fresh = Build(sim).Group;
            Assert.IsTrue(fresh.Cards.All(c => c.StampChoiceId == null));
            Assert.IsNull(fresh.PassStampLines);
        }

        // ---------------------------------------------------------------- Rev3AdmissionSnapshotTests'ten taşınanlar (aşama 7)

        /// <summary>Kartlı pano yoksa kabul pasif: tezgâh katlı, grup yok, damgalar kapalı.</summary>
        [Test]
        public void Inactive_LegacyGame_GivesInertDefaults()
        {
            var sim = Simulation.NewGame(79, new Balance { shelterEventsEnabled = false, planningBoardEnabled = false, liberationAlarmPerShift = 0 });
            var s = Build(sim);
            Assert.IsFalse(s.Active);
            Assert.AreEqual(CounterState.Folded, s.Counter);
            Assert.IsNull(s.Group);
            Assert.AreEqual("EVRAK", s.Words.Drawer);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsNotNull(s.Stamps[i]);
                Assert.IsFalse(s.Stamps[i].Enabled);
            }
            Assert.IsNotNull(AdmissionSnapshotBuilder.Build(null, null), "oyun yokken de çizilebilir");
        }

        /// <summary>Kilit yalnız kapı komutlarınındır: grup yokken (fiş basılı da olsa) kilit yok; grup varken <c>CandidatePassCheck</c>.</summary>
        [Test]
        public void Lock_OnlyWhileAGroupWaits_FromThePassCheck()
        {
            var sim = Game(79, knock: 4);
            Assert.AreEqual(0, sim.Admission.Group.Count);
            Assert.IsFalse(Build(sim).Locked);
            Ok(sim.PrintShift());
            var none = Build(sim);
            Assert.IsFalse(none.Locked, "grup yokken kapıda komut yok");
            Assert.IsNull(none.LockReason);

            var g = ToGroup(Game(79));
            Assert.IsFalse(Build(g).Locked);
            Ok(g.PrintShift());
            var printed = Build(g);
            Assert.IsTrue(printed.Locked);
            Assert.AreEqual(g.CandidatePassCheck(), printed.LockReason);
            Assert.AreEqual("Fiş basıldı; kapı bu vardiya kapalı.", printed.LockReason);
        }

        /// <summary>Ayrılma eki sayının okunuşundaki son sözcüğe uyar; İngilizcede ek yazılmaz.</summary>
        [TestCase(1, "001’den")]
        [TestCase(3, "003’ten")]
        [TestCase(5, "005’ten")]
        [TestCase(6, "006’dan")]
        [TestCase(9, "009’dan")]
        [TestCase(10, "010’dan")]
        [TestCase(40, "040’tan")]
        [TestCase(100, "100’den")]
        public void Ablative_FollowsTheSpokenNumber(int n, string expected)
        {
            Assert.AreEqual(expected, TrNumber.WithAblative(n));
            Rev3AdmissionEnglish.Use();
            Assert.AreEqual(expected.Substring(0, 3), TrNumber.WithAblative(n));
        }

        /// <summary>Kullanılan iki damganın izi ve renkleri temadan; KISITLI ve YARDIM izi yok (eski karar yolu silindi).</summary>
        [Test]
        public void Impressions_AdmitAndReject_ColorsFromTheTheme()
        {
            var b = new Balance();
            CollectionAssert.AreEqual(new[] { "KABUL", "TAM ERİŞİM" }, AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Admit, b));
            CollectionAssert.AreEqual(new[] { "REDDET" }, AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Reject, b));
            Assert.IsEmpty(AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Restricted, b));
            Assert.IsEmpty(AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Aid, b));
            Assert.AreEqual("#24592E", AdmissionSnapshotBuilder.StampColorHex(StampChoice.Admit));
            Assert.AreEqual("#9A1E14", AdmissionSnapshotBuilder.StampColorHex(StampChoice.Reject));
            Rev3AdmissionEnglish.Use();
            CollectionAssert.AreEqual(new[] { "ADMIT", "FULL ACCESS" }, AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Admit, b));
        }

        [Test]
        public void Words_AreTranslated_InTheCurrentLanguage()
        {
            var tr = Build(Game()).Words;
            Assert.AreEqual("EVRAK", tr.Drawer);
            Assert.AreEqual("KABUL", tr.Under);
            Assert.AreEqual("MASA", tr.Desk);
            Rev3AdmissionEnglish.Use();
            var en = Build(Game()).Words;
            Assert.AreEqual("PAPERS", en.Drawer, "EN'de EVRAK = PAPERS (Aşama 4 D3)");
        }
    }
}
