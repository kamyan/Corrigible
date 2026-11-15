using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.ShelterEventKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Sığınak olayları (biçim 16, aşama 3): çekim sırası ve sayısı, gizlilik, olay düzeni (aralık, öncelik, tekrar sınırı, hedefi
    /// giden olay), sızma zinciri ve tecrit, kilitsizlik, komut koşulları ve kayıttan devam. Olayların tek tek etkileri, açlık,
    /// tayın kipi ve kişi durumları <see cref="ShelterEventCaseTests"/> içindedir.
    /// </summary>
    public class ShelterEventTests
    {
        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- çekim sırası

        /// <summary>
        /// Varsayılan denge; yalnız alarm kapalı, başlangıç erzağı az ve ilk gruptan sonraki adaylar sızmacı. İlk iki gruptan
        /// birer aday alınır, sonrakiler geri çevrilir; her olayda güvenli seçenek seçilir. Değerler gerçek Core'dan üretildi.
        /// Aşama 8 dengesiyle (2026-11-15: tüketim 0,40, olay aralığı 2–3) yeniden üretildi; başlangıç erzağı 11 → 13 ki baskınsız
        /// sığınak eskisi gibi 9. vardiya dolayında tükensin ve aynı olay türleri görülsün (kurallar ve çekim noktaları aynı, yalnız sayılar).
        /// </summary>
        static List<string> Timeline(uint seed, int closes)
        {
            var b = new Balance { liberationAlarmPerShift = 0, boardStartSupplies = 13 };
            b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = 1;
            var sim = Simulation.NewGame(seed, b);
            var lines = new List<string>();
            for (int i = 0; i < closes; i++)
            {
                var e = sim.PendingShelterEvent;
                if (e != null) Decide(sim, ShelterEventData.SafeOption(e.Kind));
                if (sim.Admission.Group.Count > 0)
                {
                    if (sim.Admission.GeneratedGroups <= 2) Assert.IsTrue(sim.ChooseCandidate(sim.Admission.Group[0].Resident.Id).Ok);
                    else Assert.IsTrue(sim.PassCandidates().Ok);
                }
                uint before = sim.S.Rng.State;
                Close(sim);
                e = sim.PendingShelterEvent;
                lines.Add("vardiya " + sim.S.Day + " · cekim " + Draws(before, sim.S.Rng.State) + " · rng " + sim.S.Rng.State
                    + " · erzak " + sim.Shelter.Supplies.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                    + " · grup " + sim.Admission.GeneratedGroups + (sim.Admission.Group.Count > 0 ? "+" + sim.Admission.Group.Count : "")
                    + " · sonraki " + sim.ShelterEvents.NextEventShift
                    + " · olay " + (e == null ? "-" : e.Kind + ":" + (e.PersonId ?? "-") + ":" + (e.Area ?? e.Resource ?? "-") + ":" + e.Amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)));
            }
            lines.Add("gunluk " + string.Join(" ", sim.ShelterEvents.Log.Select(l => l.Shift + ":" + l.Kind + (l.Option != null ? "/" + l.Option : ""))));
            lines.Add("ifsa " + sim.Shelter.Exposure + " · ac " + sim.Shelter.HungryStreak + " · guven " + sim.S.Trust + " · canli " + sim.Board.People.Count(p => p.Alive));
            return lines;
        }

        [Test]
        public void Draws_Seed79_TwoGroupsThreeEvents_GoldenTimeline()
        {
            var seen = Timeline(79, 16);
            var expected = new[]
            {
                "vardiya 1 · cekim 0 · rng 440012158 · erzak 11.9 · grup 0 · sonraki 2 · olay -",
                "vardiya 2 · cekim 0 · rng 440012158 · erzak 10.8 · grup 0 · sonraki 2 · olay -",
                "vardiya 3 · cekim 0 · rng 440012158 · erzak 9.7 · grup 0 · sonraki 2 · olay -",
                "vardiya 4 · cekim 32 · rng 3215543326 · erzak 8.6 · grup 1+2 · sonraki 2 · olay -",
                "vardiya 5 · cekim 2 · rng 2583707656 · erzak 7.35 · grup 1 · sonraki 7 · olay erzak_azaliyor:-:-:0",
                "vardiya 6 · cekim 0 · rng 2583707656 · erzak 6.1 · grup 1 · sonraki 7 · olay iz:resident-9:nobet:0",
                "vardiya 7 · cekim 34 · rng 432435858 · erzak 2.85 · grup 2+2 · sonraki 9 · olay hirsizlik:sakin-5:erzak:2",
                "vardiya 8 · cekim 2 · rng 4095567484 · erzak 1.2 · grup 2 · sonraki 9 · olay -",
                "vardiya 9 · cekim 2 · rng 3463731814 · erzak 0 · grup 2 · sonraki 12 · olay takas:-:-:0",
                "vardiya 10 · cekim 0 · rng 3463731814 · erzak 0 · grup 2 · sonraki 12 · olay -",
                "vardiya 11 · cekim 32 · rng 1944295686 · erzak 0 · grup 3+2 · sonraki 12 · olay -",
                "vardiya 12 · cekim 2 · rng 1312460016 · erzak 0 · grup 3 · sonraki 15 · olay kacak:sakin-1:-:0",
                "vardiya 13 · cekim 0 · rng 1312460016 · erzak 0 · grup 3 · sonraki 15 · olay -",
                "vardiya 14 · cekim 32 · rng 4087991184 · erzak 0 · grup 4+2 · sonraki 15 · olay -",
                "vardiya 15 · cekim 2 · rng 3456155514 · erzak 0 · grup 4 · sonraki 18 · olay kacak:sakin-1:-:0",
                "vardiya 16 · cekim 0 · rng 3456155514 · erzak 0 · grup 4 · sonraki 18 · olay -",
                "gunluk 5:erzak_azaliyor/tam 6:ifsa/iz 6:iz/yoksay 7:hirsizlik/affet 9:takas/reddet 11:ayrilma 12:ayrilma 12:kacak/ikna 13:ayrilma 14:ayrilma 15:ayrilma 15:kacak/ikna 16:ayrilma",
                "ifsa 1 · ac 8 · guven 10 · canli 4",
            };
            CollectionAssert.AreEqual(expected, seen, "gözlenen:\n" + string.Join("\n", seen.Select(x => "                \"" + x + "\",")));
            // Çizelgenin okunuşu: grup üretimi 32 çekim (4, 7, 11, 14), olağan olay açılışı 2 (5, 7, 9, 12, 15; aralık 2–3), affın vadeli
            // kaydı 2 (8); iz (6), ayrılma (11–16) ve karar çekimsiz. Aynı kapanışta önce kapı, sonra olay çekilir (7: 34).
        }

        /// <summary>
        /// Kapanışın çekimleri: kapı grubu 32, olağan olay açılışı 2 (büyük hırsızlık dahil: aralığa uyar), zincir açılışı 0.
        /// </summary>
        [Test]
        public void Draws_OrdinaryOpeningTakesTwo_BigTheftToo_ChainTakesNone()
        {
            // Olağan olay: tek başına 2 çekim; hedefli (kaçak) ve hedefsiz (konvoy) aynı
            foreach (string kind in new[] { "konvoy", "kacak", "erzak_azaliyor", "takas" })
            {
                var sim = New(For(kind));
                Arrange(sim, kind);
                Assert.AreEqual(2, CloseDraws(sim), kind);
                Assert.AreEqual(kind, Pending(sim));
            }
            // Olay açılmayan kapanış çekim yapmaz
            var quiet = New(Quiet());
            for (int i = 0; i < 4; i++) Assert.AreEqual(0, CloseDraws(quiet));

            // Zincir: iz, ikinci iz ve sabotaj açılışı çekimsiz
            var chain = New(Robots(Quiet(1)));
            Admit(chain);
            ShutDoor(chain);
            Assert.AreEqual(0, CloseDraws(chain));
            Assert.AreEqual("iz", Pending(chain));
            Decide(chain, "yoksay");
            Assert.AreEqual(0, CloseDraws(chain));
            Assert.AreEqual("ikinci_iz", Pending(chain));
            Decide(chain, "yoksay");
            Assert.AreEqual(0, CloseDraws(chain));
            Assert.AreEqual(0, CloseDraws(chain));
            Assert.AreEqual("sabotaj", Pending(chain));

            // Büyük hırsızlık: iki çekim kararın ardından ilk kapanışta (vadeli kayıt); açılışı olağan açılış gibi iki çekim (aralık ve atılan hedef)
            var b = Limit(Quiet(), "hirsizlik", 1);
            b.bigTheftChance = 1;
            b.bigTheftDelayMin = b.bigTheftDelayMax = 2;
            var theft = New(b);
            Arrange(theft, "hirsizlik");
            CloseTo(theft, "hirsizlik");
            Decide(theft, "affet");
            theft.Shelter.Supplies = 30;
            Assert.AreEqual(2, CloseDraws(theft), "vadeli kaydın iki çekimi");
            Assert.AreEqual("-", Pending(theft));
            Assert.AreEqual(2, CloseDraws(theft), "büyük hırsızlık açılışı aralığı çeker");
            Assert.AreEqual("buyuk_hirsizlik", Pending(theft));

            // Kapı grubu ile olağan olay aynı kapanışta: 32 + 2, önce kapı
            var both = For("konvoy");
            both.admissionKnockMin = both.admissionKnockMax = 1;
            var door = New(both);
            Arrange(door, "konvoy");
            Assert.AreEqual(34, CloseDraws(door));
            Assert.AreEqual(1, door.Admission.GeneratedGroups);
            Assert.AreEqual("konvoy", Pending(door));
        }

        /// <summary>Vadeli sonuç kaydı her zaman iki çekim yapar; tecrit bitişi her kişi için bir. Olasılık değeri sayıyı değiştirmez.</summary>
        [Test]
        public void Draws_ChangedChances_SameDrawCount()
        {
            var delayed = new[]
            {
                new[] { "hirsizlik", "affet" }, new[] { "agir_yarali", "beklet" }, new[] { "kavga", "karisma" },
                new[] { "anma", "devam" }, new[] { "cagri", "gonderme" },
            };
            foreach (double chance in new[] { 0.0, 0.5, 1.0 })
            {
                foreach (var pair in delayed)
                {
                    var sim = Open(pair[0], b =>
                    {
                        Limit(b, pair[0], 1);
                        b.bigTheftChance = b.woundWorsenChance = b.feudInjuryChance = b.griefChance = b.callRefuseChance = chance;
                    });
                    uint rng = sim.S.Rng.State;
                    Decide(sim, pair[1]);
                    Assert.AreEqual(rng, sim.S.Rng.State, "karar çekim yapmaz");
                    Assert.AreEqual(2, CloseDraws(sim), pair[0] + "/" + pair[1] + " olasılık " + chance);
                    // Kayıt bir kez çekilir: sonraki kapanışlar çekimsiz
                    for (int i = 0; i < 3; i++)
                    {
                        if (sim.PendingShelterEvent != null) Decide(sim, ShelterEventData.SafeOption(sim.PendingShelterEvent.Kind));
                        Assert.AreEqual(0, CloseDraws(sim), pair[0] + " sonraki kapanış " + i);
                    }
                }

                // Tecrit: insan ve sızmacı için, bulgu olasılığı ne olursa olsun bitiş kapanışında tek çekim
                foreach (bool robot in new[] { true, false })
                {
                    var b = robot ? Robots(Quiet(1)) : Humans(Quiet(1), 1, 1);
                    b.isolationFindChance = b.isolationFindChanceTwo = chance;
                    var sim = New(b);
                    Admit(sim);
                    ShutDoor(sim);
                    CloseTo(sim, "iz");
                    Decide(sim, "tecrit");
                    Assert.AreEqual(0, CloseDraws(sim));
                    Assert.AreEqual(0, CloseDraws(sim));
                    Assert.AreEqual(1, CloseDraws(sim), (robot ? "sızmacı" : "insan") + " olasılık " + chance);
                    Assert.AreEqual(1, Log(sim, "bulgu").Count + Log(sim, "bulgu_yok").Count);
                }
            }
        }

        // ---------------------------------------------------------------- komutlar ve okuyucular RNG çekmez

        [Test]
        public void Commands_AndReaders_DrawNothing_AndFailedDecisionsAreNotRecorded()
        {
            foreach (string kind in ShelterEventData.Kinds)
            {
                var sim = Open(kind);
                uint rng = sim.S.Rng.State;
                int commands = sim.Record.commands.Count;
                var e = sim.PendingShelterEvent;
                for (int i = 0; i < 5; i++)
                {
                    Assert.AreSame(e, sim.PendingShelterEvent);
                    Assert.IsNotEmpty(sim.ShelterEventBody(e));
                    Assert.IsNotEmpty(ShelterEventData.Title(kind));
                    var options = sim.ShelterEventOptions();
                    CollectionAssert.AreEqual(ShelterEventData.Options(kind), options.Select(o => o.Id).ToArray());
                    foreach (var o in options)
                    {
                        Assert.IsNotEmpty(o.Name, kind + "/" + o.Id);
                        Assert.IsNotEmpty(o.Cost, kind + "/" + o.Id);
                        Assert.AreEqual(o.Open, sim.ShelterEventDecisionCheck(o.Id) == null);
                        Assert.AreEqual(o.Open, o.ClosedReason == null);
                        Assert.AreEqual(o.Id == ShelterEventData.SafeOption(kind), o.Safe);
                    }
                    Assert.IsNotNull(sim.ShelterEventPrintCheck());
                    Assert.IsNotNull(sim.BoardReceiptPreview);
                    Assert.GreaterOrEqual(sim.IsolationFree, 0);
                    Assert.GreaterOrEqual(sim.BoardConsumption, 0);
                    Assert.GreaterOrEqual(sim.HalfRationShiftsLeft, 0);
                    Assert.IsFalse(sim.HalfRationActive);
                    Assert.IsFalse(sim.BlackoutActive);
                    Assert.IsNull(sim.NextDeserter);
                    Assert.IsNotNull(sim.ShelterEvents);
                }
                Assert.AreEqual(rng, sim.S.Rng.State, kind + ": okuyucular çekim yapmaz");

                // Reddedilen kararlar: bilinmeyen, boş ve başka olayın seçeneği
                Assert.AreEqual(Loc.T("Bu olayda böyle bir seçenek yok."), sim.DecideShelterEvent("yok").Msg);
                Assert.IsFalse(sim.DecideShelterEvent(null).Ok);
                Assert.IsFalse(sim.DecideShelterEvent("").Ok);
                Assert.IsFalse(sim.DecideShelterEvent(kind == "takas" ? "tecrit" : "metal").Ok);
                Assert.IsFalse(sim.PrintShift().Ok, "açık olay fişi kilitler");
                Assert.AreEqual(commands, sim.Record.commands.Count, "başarısız komut kayda yazılmaz");
                Assert.AreEqual(rng, sim.S.Rng.State);
                Assert.AreSame(e, sim.PendingShelterEvent);

                string option = ShelterEventData.SafeOption(kind);
                Decide(sim, option);
                Assert.AreEqual(rng, sim.S.Rng.State, kind + ": karar çekim yapmaz");
                Assert.AreEqual(commands + 1, sim.Record.commands.Count);
                var saved = sim.Record.commands.Last();
                Assert.AreEqual("event_decide", saved.kind);
                Assert.AreEqual(option, saved.a);
                Assert.IsNull(saved.b);
                Assert.AreEqual(sim.S.Day, saved.day);
                var log = sim.ShelterEvents.Log.Last(l => l.Kind == kind);
                Assert.AreEqual(option, log.Option);
                Assert.AreEqual(e.PersonId, log.PersonId);
                Assert.AreEqual(sim.S.Day, log.Shift);

                Assert.AreEqual(Loc.T("Karar bekleyen olay yok."), sim.DecideShelterEvent(option).Msg);
                Assert.AreEqual(0, sim.ShelterEventOptions().Count);
                Assert.IsNull(sim.ShelterEventBody(null));
                Assert.IsNull(sim.ShelterEventPrintCheck());
                Assert.AreEqual(commands + 1, sim.Record.commands.Count);
            }
        }

        [Test]
        public void EventData_CoversFourteenKinds_EachWithOneSafeOption()
        {
            Assert.AreEqual(14, ShelterEventData.Kinds.Length);
            CollectionAssert.AreEquivalent(ShelterEventData.Kinds, ShelterEventData.Chain.Concat(ShelterEventData.Ordinary).ToArray());
            Assert.IsTrue(ShelterEventData.IsOrdinary("buyuk_hirsizlik"), "büyük hırsızlık aralık bütçesine uyar (yalnız zincir dışındadır)");
            Assert.AreEqual(ShelterEventData.Priority("hirsizlik"), ShelterEventData.Priority("buyuk_hirsizlik"), "kişiler önceliği");
            int last = -1;
            foreach (string kind in ShelterEventData.Chain.Concat(ShelterEventData.Ordinary))
            {
                Assert.GreaterOrEqual(ShelterEventData.Priority(kind), last, kind + ": öncelik sırası");
                last = ShelterEventData.Priority(kind);
                var options = ShelterEventData.Options(kind);
                Assert.GreaterOrEqual(options.Length, 2, kind);
                Assert.AreEqual(options.Length, options.Distinct().Count(), kind);
                CollectionAssert.Contains(options, ShelterEventData.SafeOption(kind), kind);
                Assert.AreNotEqual(kind, ShelterEventData.Title(kind), kind + ": başlık");
                foreach (string option in options) Assert.AreNotEqual(option, ShelterEventData.OptionName(kind, option), kind + "/" + option);
                Assert.AreEqual(ShelterEventData.Chain.Contains(kind), ShelterEventData.IsChain(kind));
                Assert.AreEqual(ShelterEventData.Ordinary.Contains(kind), ShelterEventData.IsOrdinary(kind));
            }
            Assert.AreEqual(0, ShelterEventData.Options("yok").Length);
            Assert.IsNull(ShelterEventData.SafeOption("yok"));
            Assert.IsFalse(ShelterEventData.NeedsLivingTarget("anma"), "anmanın hedefi ölüdür");
        }

        [Test]
        public void English_EventTexts_AreTranslated()
        {
            var missing = new HashSet<string>();
            Action<string> onMissing = x => missing.Add(x);
            Loc.Missing += onMissing;
            try
            {
                Loc.SetLanguage(Loc.English, Loc.ParseTable(System.IO.File.ReadAllText(Corrigible.EditorTools.LocalizationTool.TableFile))[Loc.English]);
                var texts = new List<string>();
                foreach (string kind in ShelterEventData.Kinds)
                {
                    // Uç durum: kapalılık gerekçeleri de okunur
                    foreach (bool extreme in new[] { false, true })
                    {
                        var sim = Open(kind);
                        if (extreme)
                        {
                            sim.Shelter.Supplies = 0;
                            sim.S.Trust = 1;
                            sim.S.Alarm = sim.B.liberationAlarmThreshold - 1;
                            foreach (var m in sim.Board.Materials) m.Quantity = 0;
                            var other = sim.Board.People.First(p => p.Alive && p.Id != sim.PendingShelterEvent.PersonId);
                            other.Isolated = true;
                            sim.Admission.File(other.Id).IsolatedUntil = sim.S.Day + 3;
                        }
                        var e = sim.PendingShelterEvent;
                        texts.Add(ShelterEventData.Title(kind));
                        texts.Add(sim.ShelterEventBody(e));
                        foreach (var o in sim.ShelterEventOptions())
                        {
                            texts.Add(o.Name);
                            texts.Add(o.Cost);
                            if (o.Risk != null) texts.Add(o.Risk);
                            if (!o.Open) texts.Add(o.ClosedReason);
                        }
                        texts.Add(sim.ShelterEventPrintCheck());
                        texts.Add(sim.PrintShift().Msg);
                        texts.Add(sim.DecideShelterEvent("yok").Msg);
                        var open = sim.ShelterEventOptions().First(o => o.Open);
                        texts.Add(sim.DecideShelterEvent(open.Id).Msg);
                        texts.Add(sim.DecideShelterEvent(open.Id).Msg);
                    }
                }
                // Kişi durumlarının gerekçeleri ve fişin açlık satırları
                var theft = Open("hirsizlik");
                string thief = theft.PendingShelterEvent.PersonId;
                Decide(theft, "kisitla");
                texts.Add(theft.AssignResident(thief, "cook").Msg);
                var trace = Open("iz");
                string traced = trace.PendingShelterEvent.PersonId;
                Decide(trace, "tecrit");
                texts.Add(trace.AssignResident(traced, "cook").Msg);
                var deserter = Open("kacak", b => { b.desertStreak = 3; Limit(b, "kacak", 1); Limit(b, "erzak_azaliyor", 9); });
                string idle = deserter.PendingShelterEvent.PersonId;
                Decide(deserter, "ikna");
                texts.Add(deserter.AssignResident(idle, "cook").Msg);
                texts.AddRange(deserter.BoardReceiptPreview.Split('\n').Where(line => line.StartsWith("RATIONS")));
                deserter.Shelter.Supplies = 0;
                CloseTo(deserter, "erzak_azaliyor");
                Decide(deserter, "yarim");
                texts.AddRange(deserter.BoardReceiptPreview.Split('\n').Where(line => line.StartsWith("RATIONS")));
                var feud = Open("kavga");
                Decide(feud, "ayir");
                Close(feud);
                FillSquad(feud);
                texts.Add(feud.PlanningRaidCheck());

                Assert.IsEmpty(missing, "Tabloda olmayan metinler:\n" + string.Join("\n", missing));
                Assert.Greater(texts.Distinct().Count(), 90);
                Assert.AreEqual(4, texts.Count(t => t != null && t.StartsWith("RATIONS ·")), "fişin açlık satırları çevrildi");
                var turkish = texts.Where(t => string.IsNullOrEmpty(t) || System.Text.RegularExpressions.Regex.IsMatch(t, "[ğüşıöçĞÜŞİÖÇ]")).Distinct().ToList();
                Assert.IsEmpty(turkish, "Türkçe kalan metinler:\n" + string.Join("\n", turkish));
            }
            finally
            {
                Loc.Missing -= onMissing;
                Loc.SetLanguage(Loc.Turkish);
            }
        }

        // ---------------------------------------------------------------- gizlilik

        /// <summary>Oyuncunun görebildiği her şey: açık olay, seçenekleri, pano JSON'u ve sayaçlar.</summary>
        static string Visible(Simulation sim)
        {
            var e = sim.PendingShelterEvent;
            var parts = new List<string>
            {
                "olay:" + (e == null ? "-" : e.Kind + "|" + e.PersonId + "|" + e.OtherId + "|" + e.Area + "|" + e.Resource + "|" + e.Amount + "|" + e.OpenedShift),
                "govde:" + sim.ShelterEventBody(e),
                "secenek:" + string.Join(";", sim.ShelterEventOptions().Select(o => o.Id + "|" + o.Name + "|" + o.Cost + "|" + o.Risk + "|" + o.Open + "|" + o.ClosedReason + "|" + o.Safe)),
                "fis:" + sim.BoardReceiptPreview,
                "sayac:" + sim.S.Rng.State + "|" + sim.Shelter.Supplies + "|" + sim.S.Fon + "|" + sim.S.Alarm + "|" + sim.S.Trust + "|" + sim.Shelter.Exposure
                    + "|" + sim.Shelter.HungryStreak + "|" + sim.Shelter.Deserted + "|" + sim.IsolationFree + "|" + sim.S.Ending,
                BoardJson(sim),
            };
            return string.Join("\n", parts);
        }

        static Balance Identity(int variant, int delay)
        {
            switch (variant)
            {
                case 0: return Humans(Quiet(1), 0, 0, delay);       // kimse sızmacı değil, kimse iz bırakmaz
                case 1: return Robots(Quiet(1), delay);             // herkes sızmacı
                case 2: return Humans(Quiet(1), 1, 1, delay);       // herkes iz bırakan insan
                default:
                    // Varsayılan olasılıklar, aynı gecikme
                    var b = Quiet(1);
                    b.traceDelayMin = b.traceDelayMax = delay;
                    b.secondTraceGapMin = b.secondTraceGapMax = 1;
                    return b;
            }
        }

        /// <summary>
        /// 1. parça: aynı tohumda adayın gizli çekimleri zorla çevrilir (sızmacı, iz bırakan insan, hiçbiri, varsayılan). Kişi alınır,
        /// çalıştırılır ve ilk izin vadesinden bir önceki kapanışa dek her adımda bütün public durum birebir aynıdır.
        /// </summary>
        [Test]
        public void HiddenIdentity_FlippedDraws_PublicStateIdenticalUntilTheFirstTrace()
        {
            const int delay = 4;
            int traced = 0;
            for (uint seed = 1; seed <= 10; seed++)
            {
                var stories = new List<List<string>>();
                var last = new List<string>();
                for (int variant = 0; variant < 4; variant++)
                {
                    var sim = New(Identity(variant, delay), seed);
                    var story = new List<string> { Visible(sim) };
                    string id = Admit(sim);
                    story.Add(Visible(sim));
                    story.Add("uretim:" + sim.AssignResident(id, "craft").Ok);
                    for (int i = 0; i < delay - 1; i++)
                    {
                        if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
                        Close(sim);
                        story.Add(Visible(sim));
                        Assert.AreEqual("-", Pending(sim), "test düzeni: vadeden önce olay yok");
                    }
                    stories.Add(story);
                    if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
                    Close(sim);
                    last.Add(Pending(sim));
                }
                for (int variant = 1; variant < 4; variant++)
                    CollectionAssert.AreEqual(stories[0], stories[variant], "tohum " + seed + " çeşit " + variant);
                // Düzenin canlı olduğunun kanıtı: vadede sızmacı ve iz bırakan insan iz verir, öteki vermez
                Assert.AreEqual("-", last[0], "tohum " + seed);
                Assert.AreEqual("iz", last[1], "tohum " + seed);
                Assert.AreEqual("iz", last[2], "tohum " + seed);
                if (last[3] == "iz") traced++;
            }
            Assert.Less(traced, 10, "varsayılan olasılıklarla her aday iz bırakmaz");
        }

        /// <summary>
        /// 2. parça: aynı vardiyada iz bırakan insan ile sızmacı için olay kâğıdı, seçeneklerin açıklığı, her seçeneğin sonucu ve
        /// ifşa değişimi aynıdır; ikinci iz de öyle. İkisi yalnız sabotajda ayrışır.
        /// </summary>
        [Test]
        public void HiddenIdentity_TraceMoment_HumanAndInfiltrator_SamePaperOptionsAndExposure()
        {
            for (uint seed = 1; seed <= 6; seed++)
            {
                foreach (string first in new[] { "tecrit", "yolla", "yoksay" })
                {
                    var pair = new[] { Robots(Quiet(1), 2), Humans(Quiet(1), 1, 1, 2) }.Select(b =>
                    {
                        // Bulgu olasılığı 0: tecrit sonucu da ayrışmaz (bulgu kimliği açan tek tecrit sonucudur)
                        b.isolationFindChance = b.isolationFindChanceTwo = 0;
                        return New(b, seed);
                    }).ToArray();
                    var ids = pair.Select(sim => Admit(sim)).ToArray();
                    Assert.AreEqual(ids[0], ids[1]);
                    foreach (var sim in pair) { sim.AssignResident(ids[0], "cook"); if (sim.Admission.Group.Count > 0) sim.PassCandidates(); Close(sim); }
                    Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "iz öncesi, tohum " + seed);
                    foreach (var sim in pair) { if (sim.Admission.Group.Count > 0) sim.PassCandidates(); Close(sim); Assert.AreEqual("iz", Pending(sim)); }
                    Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "iz anı, tohum " + seed);
                    Assert.AreEqual(1, pair[0].Admission.File(ids[0]).ClueCount);

                    foreach (var sim in pair) Decide(sim, first);
                    Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), first + " sonrası, tohum " + seed);
                    Assert.AreEqual(first == "yoksay" ? 1 : 0, pair[0].Shelter.Exposure);
                    Assert.AreEqual(pair[0].Shelter.Exposure, pair[1].Shelter.Exposure);

                    // Tecrit üç kapanış sürer; sonra (ya da görmezden gelinince hemen) ikinci iz gelir
                    int closes = first == "tecrit" ? 4 : 1;
                    for (int i = 0; i < closes; i++)
                    {
                        foreach (var sim in pair) { if (sim.Admission.Group.Count > 0) sim.PassCandidates(); Close(sim); }
                        Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), first + " + " + (i + 1) + " kapanış, tohum " + seed);
                    }
                    if (first == "yolla")
                    {
                        Assert.AreEqual("-", Pending(pair[0]));
                        continue;
                    }
                    Assert.AreEqual("ikinci_iz", Pending(pair[0]), first);
                    Assert.AreEqual("ikinci_iz", Pending(pair[1]), first);
                    foreach (var sim in pair) Decide(sim, "yoksay");
                    Assert.AreEqual(Visible(pair[0]), Visible(pair[1]));
                    // Müdahale vardiyası: hâlâ aynı
                    foreach (var sim in pair) { if (sim.Admission.Group.Count > 0) sim.PassCandidates(); Close(sim); }
                    Assert.AreEqual(Visible(pair[0]), Visible(pair[1]), "müdahale vardiyası");
                    // Sabotaj kimliği açan ilk public olaydır
                    foreach (var sim in pair) { if (sim.Admission.Group.Count > 0) sim.PassCandidates(); Close(sim); }
                    Assert.AreEqual("sabotaj", Pending(pair[0]));
                    Assert.AreEqual("-", Pending(pair[1]), "insan sabotaj yapmaz");
                }
            }
        }

        /// <summary>Olay hedefi çekimi ve açlıktan ayrılma sırası kimliğe bakmaz: sızmacı da herkes gibi hedef olur ve ayrılır.</summary>
        [Test]
        public void HiddenIdentity_TargetsAndDesertionOrder_IgnoreIdentity()
        {
            for (uint seed = 1; seed <= 6; seed++)
            {
                var stories = new List<List<string>>();
                for (int variant = 0; variant < 3; variant++)
                {
                    // İz vadesi uzak: zincir araya girmez
                    var b = Identity(variant, 40);
                    Limit(b, "kacak", 9);
                    Limit(b, "hirsizlik", 9);
                    b.desertStreak = 3;
                    var sim = New(b, seed);
                    var story = new List<string>();
                    Admit(sim, 1);
                    Admit(sim, 0);
                    ShutDoor(sim);
                    for (int i = 0; i < 9; i++)
                    {
                        if (sim.PendingShelterEvent != null) Decide(sim, ShelterEventData.SafeOption(sim.PendingShelterEvent.Kind));
                        // İki tok kapanış hırsızlığı, sonrakiler açlığı tetikler
                        sim.Shelter.Supplies = i < 2 ? sim.BoardConsumption + sim.B.theftSupplies + 0.5 : 0;
                        story.Add("ayrilacak:" + sim.NextDeserter?.Id);
                        Close(sim);
                        story.Add(Visible(sim));
                    }
                    Assert.Greater(sim.Shelter.Deserted, 3, "test düzeni: ayrılmalar oldu");
                    Assert.IsTrue(Log(sim, "kacak").Count > 0 && Log(sim, "hirsizlik").Count > 0, "test düzeni: hedefli olaylar açıldı");
                    stories.Add(story);
                }
                CollectionAssert.AreEqual(stories[0], stories[1], "tohum " + seed + ": sızmacı");
                CollectionAssert.AreEqual(stories[0], stories[2], "tohum " + seed + ": iz bırakan insan");
            }
        }

        [Test]
        public void PublicJson_CarriesNoHiddenField()
        {
            // Zengin durum: saati işleyen sızmacı, açılmış izler, çekilmiş ve bekleyen vadeli kayıt, tecrit, sabotaj
            var b = Robots(Quiet(1));
            Limit(b, "hirsizlik", 1);
            b.bigTheftChance = 1;
            b.bigTheftDelayMin = b.bigTheftDelayMax = 5;
            b.isolationFindChance = 0;
            var sim = New(b);
            string first = Admit(sim);
            sim.AssignResident(first, "cook");
            string second = Admit(sim, 0);
            ShutDoor(sim);
            var jsons = new List<string> { BoardJson(sim) };
            var kinds = new List<string>();
            for (int i = 0; i < 9; i++)
            {
                var e = sim.PendingShelterEvent;
                if (e != null)
                {
                    kinds.Add(e.Kind);
                    Decide(sim, e.Kind == "iz" && e.PersonId == second ? "tecrit" : e.Kind == "sabotaj" && sim.IsolationFree > 0 ? "sorgula" : ShelterEventData.SafeOption(e.Kind));
                }
                // Hırsızlık görülene dek tetiği kurulur (zincir önce açılır); sonra erzak bol
                if (kinds.Contains("hirsizlik")) sim.Shelter.Supplies = 40; else Arrange(sim, "hirsizlik");
                Close(sim);
                jsons.Add(BoardJson(sim));
            }
            CollectionAssert.IsSupersetOf(kinds, new[] { "iz", "ikinci_iz", "sabotaj", "hirsizlik" }, "test düzeni: " + string.Join(",", kinds));
            Assert.IsTrue(sim.Admission.File(first).Revealed);
            sim.Record.day = sim.S.Day;
            jsons.Add(UnityEngine.JsonUtility.ToJson(sim.Record));

            string last = jsons[jsons.Count - 2];
            foreach (string visible in new[] { "\"ShelterEvents\"", "\"Pending\"", "\"NextEventShift\"", "\"Counts\"", "\"Log\"", "\"Feuds\"", "\"Pardoned\"", "\"Revealed\"", "\"IsolatedUntil\"", "\"IdleUntil\"", "\"TraceAreas\"", "\"Isolated\"" })
                StringAssert.Contains(visible, last, visible);
            foreach (string json in jsons)
                foreach (string hidden in new[]
                {
                    "\"Clock\"", "TraceClock", "IsolationFound", "\"Delayed\"", "LastAccessJob", "Sabotaged", "TraceDelay", "SecondTraceGap", "HumanTrace",
                    "HumanSecondTrace", "\"Occurs\"", "\"Drawn\"", "\"Pick\"", "\"DueShift\"", "\"DecidedShift\"", "\"Mates\"", "\"Ready\"",
                })
                    StringAssert.DoesNotContain(hidden, json, hidden);
            // Kayıttaki denge alanları ("candidateTier0Robot") dışında hiçbir yerde kimlik adı geçmez
            foreach (string json in jsons.Take(jsons.Count - 1)) StringAssert.DoesNotContain("Robot", json);

            // Yansıma: gizli alanların hiçbiri public değildir
            const BindingFlags pub = BindingFlags.Instance | BindingFlags.Public, hid = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (string field in new[] { "Clock", "TraceClock", "IsolationFound", "LastAccessJob", "Sabotaged" })
            {
                Assert.IsNull(typeof(ResidentAdmissionFile).GetField(field, pub), field);
                Assert.IsNotNull(typeof(ResidentAdmissionFile).GetField(field, hid), field);
            }
            Assert.IsNull(typeof(ResidentAdmissionFile).GetField("ActiveShifts", pub | hid), "eski saat alanı silindi");
            Assert.IsNull(typeof(ShelterEventState).GetField("Delayed", pub));
            Assert.IsNotNull(typeof(ShelterEventState).GetField("Delayed", hid));
            Assert.AreEqual(0, typeof(ShelterDelayed).GetFields(pub).Length, "vadeli kaydın public alanı yok");
            Assert.AreEqual(0, typeof(AdmissionIdentity).GetFields(pub).Length);
            // Açık olay ve günlük yalnız kimlik ve sayı taşır
            foreach (var type in new[] { typeof(ShelterEventInstance), typeof(ShelterEventLogEntry), typeof(ShelterEventCount), typeof(ShelterFeud) })
                foreach (var f in type.GetFields(pub))
                    Assert.IsTrue(f.FieldType == typeof(string) || f.FieldType == typeof(int) || f.FieldType == typeof(double), type.Name + "." + f.Name);
        }

        // ---------------------------------------------------------------- olay düzeni

        [Test]
        public void Events_OneOpenAtATime_GapCountsFromOpeningToOpening()
        {
            var b = On("konvoy", "takas");
            b.convoyTrustAt = 0;
            b.eventGapMin = b.eventGapMax = 3;
            var sim = New(b);
            int start = sim.S.Day;
            Assert.AreEqual(start + 3, sim.ShelterEvents.NextEventShift, "ilk olağan olay en erken aralık kadar sonra");
            var opened = new List<int>();
            for (int i = 0; i < 13; i++)
            {
                // Takasın tetiği de hep sağlanır; yine de aynı anda tek olay açılır
                sim.Shelter.Supplies = sim.BoardConsumption + 1;
                sim.AddBoardMaterial("metal", 2);
                Close(sim);
                var e = sim.PendingShelterEvent;
                if (e == null) continue;
                Assert.AreEqual(sim.S.Day, e.OpenedShift);
                Assert.AreEqual(sim.S.Day + 3, sim.ShelterEvents.NextEventShift);
                opened.Add(sim.S.Day - start);
                Assert.AreEqual(Loc.T("Karar bekleyen olay var: önce olay tutanağını karara bağla."), sim.PrintShift().Msg);
                Decide(sim, "reddet");
            }
            CollectionAssert.AreEqual(new[] { 3, 6, 9, 12 }, opened);
            CollectionAssert.AreEqual(new[] { "takas", "takas", "takas", "takas" }, sim.ShelterEvents.Log.Select(l => l.Kind).ToArray(), "aynı öncelikte tablo sırası: takas konvoydan önce");

            // Aralık açılışta tek çekimle belirlenir: 2–3 vardiya, ikisi de görülür
            var gaps = new HashSet<int>();
            for (uint seed = 1; seed <= 16; seed++)
            {
                var d = On("konvoy");
                d.convoyTrustAt = 0;
                d.eventGapMin = 2;
                d.eventGapMax = 3;
                var game = New(d, seed);
                Close(game);
                CloseTo(game, "konvoy");
                gaps.Add(game.ShelterEvents.NextEventShift - game.S.Day);
            }
            CollectionAssert.AreEquivalent(new[] { 2, 3 }, gaps);
        }

        /// <summary>Koşulu sağlanan birden çok olay: hayatta kalma, kişiler, fırsatlar; aynı öncelikte tablo sırası. Sınır 1: her kapanış sıradakini açar.</summary>
        [Test]
        public void Events_Priority_SurvivalPeopleOpportunity_TableOrderWithin()
        {
            // Aç kapanışlar: kaçak, erzak, karartma, takas ve konvoyun tetiği birlikte
            var b = Quiet();
            foreach (string kind in ShelterEventData.Ordinary) Limit(b, kind, 1);
            b.eventDeserterStreak = 1;
            b.convoyTrustAt = 0;
            var sim = New(b);
            var seen = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                if (sim.PendingShelterEvent != null) Decide(sim, ShelterEventData.SafeOption(sim.PendingShelterEvent.Kind));
                sim.Shelter.Supplies = 0;
                sim.S.Alarm = sim.B.liberationAlarmThreshold - 1;
                sim.AddBoardMaterial("metal", 2);
                Close(sim);
                seen.Add(Pending(sim));
            }
            CollectionAssert.AreEqual(new[] { "kacak", "erzak_azaliyor", "karartma", "takas", "konvoy", "-", "-" }, seen);

            // Tok ama az erzak: hırsızlık (kişiler) erzak ve karartmadan sonra, takastan önce
            sim = New(b);
            seen.Clear();
            for (int i = 0; i < 6; i++)
            {
                if (sim.PendingShelterEvent != null) Decide(sim, ShelterEventData.SafeOption(sim.PendingShelterEvent.Kind));
                sim.Shelter.Supplies = sim.BoardConsumption + 2.5;
                sim.S.Alarm = sim.B.liberationAlarmThreshold - 1;
                sim.AddBoardMaterial("metal", 2);
                Close(sim);
                seen.Add(Pending(sim));
            }
            CollectionAssert.AreEqual(new[] { "erzak_azaliyor", "karartma", "hirsizlik", "takas", "konvoy", "-" }, seen);

            // Baskın dönüşü: ağır yaralı, kavga, anma (kişiler), sonra takas, çağrı, konvoy (fırsatlar)
            var r = Quiet();
            foreach (string kind in ShelterEventData.Ordinary) Limit(r, kind, 1);
            r.convoyTrustAt = 0;
            r.boardStartResidents = r.boardResidentCapacity = 24;
            r.woundWorsenChance = r.feudInjuryChance = r.griefChance = r.callRefuseChance = 0;
            sim = New(r);
            seen.Clear();
            for (int i = 0; i < 7; i++)
            {
                sim.Shelter.Supplies = 90;
                // Başarılı ama kayıplı baskın: Teknisyen geride kalır (kiti de gider), EMP uzmanı düşer
                Raid(sim, true, 2, 1, 0);
                if (sim.PendingShelterEvent != null) Decide(sim, ShelterEventData.SafeOption(sim.PendingShelterEvent.Kind));
                Close(sim);
                seen.Add(Pending(sim));
            }
            CollectionAssert.AreEqual(new[] { "agir_yarali", "kavga", "anma", "takas", "cagri", "konvoy", "-" }, seen);
        }

        [TestCase("kacak")]
        [TestCase("erzak_azaliyor")]
        [TestCase("karartma")]
        [TestCase("hirsizlik")]
        [TestCase("agir_yarali")]
        [TestCase("kavga")]
        [TestCase("anma")]
        [TestCase("takas")]
        [TestCase("cagri")]
        [TestCase("konvoy")]
        public void Events_RepeatLimitAndCooldown(string kind)
        {
            var b = For(kind);
            Limit(b, kind, 2, 3);
            b.boardStartResidents = b.boardResidentCapacity = 24;
            b.bigTheftChance = b.woundWorsenChance = b.feudInjuryChance = b.griefChance = b.callRefuseChance = 0;
            var sim = New(b);
            int start = sim.S.Day;
            var opened = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                Arrange(sim, kind);
                if (sim.PendingShelterEvent != null) Decide(sim, ShelterEventData.SafeOption(kind));
                Close(sim);
                if (sim.PendingShelterEvent == null) continue;
                Assert.AreEqual(kind, Pending(sim));
                opened.Add(sim.S.Day - start);
                Assert.AreEqual(opened.Count, Count(sim, kind));
                Assert.AreEqual(sim.S.Day, sim.ShelterEvents.Count(kind).LastShift);
            }
            // Tetik her kapanışta sağlanır: ilk açılış, bekleme (3) dolunca ikincisi, sonra sınır (2) kapatır
            CollectionAssert.AreEqual(new[] { 1, 4 }, opened, kind);

            // Sınır 0: olay hiç açılmaz
            var none = For(kind);
            Limit(none, kind, 0);
            none.boardStartResidents = none.boardResidentCapacity = 24;
            sim = New(none);
            for (int i = 0; i < 3; i++)
            {
                Arrange(sim, kind);
                CloseTo(sim, "-");
            }
        }

        /// <summary>Zincir olayı aralığı beklemez ve yeniden başlatmaz; olağan olaylardan önce açılır.</summary>
        [Test]
        public void Chain_EventsStayOutsideTheGapBudget_AndOpenFirst()
        {
            var b = Robots(Quiet(1));
            Limit(b, "konvoy", 9);
            b.convoyTrustAt = 0;
            b.eventGapMin = b.eventGapMax = 4;
            var sim = New(b);
            var seen = new List<string>();
            string id = null;
            for (int i = 0; i < 12; i++)
            {
                var e = sim.PendingShelterEvent;
                if (e != null) Decide(sim, ShelterEventData.SafeOption(e.Kind));
                if (sim.Admission.Group.Count > 0)
                {
                    if (id == null && sim.Admission.GeneratedGroups == 2) { id = sim.Admission.Group[0].Resident.Id; Assert.IsTrue(sim.ChooseCandidate(id).Ok); ShutDoor(sim); }
                    else Assert.IsTrue(sim.PassCandidates().Ok);
                }
                int next = sim.ShelterEvents.NextEventShift;
                Close(sim);
                e = sim.PendingShelterEvent;
                seen.Add(sim.S.Day + ":" + Pending(sim));
                if (e != null && ShelterEventData.IsChain(e.Kind))
                    Assert.AreEqual(next, sim.ShelterEvents.NextEventShift, e.Kind + " aralığı yeniden başlatmaz");
                else if (e != null)
                    Assert.AreEqual(sim.S.Day + 4, sim.ShelterEvents.NextEventShift);
            }
            // Kabul 2. vardiyada; iz 3, ikinci iz 4 (konvoyun vadesiyle aynı kapanış: zincir önce, konvoy bir sonraki kapanışta),
            // müdahale vardiyası 5'te konvoy açılır, sabotaj 6; sonraki konvoy açılışından dört vardiya sonra (9)
            CollectionAssert.AreEqual(new[] { "1:-", "2:-", "3:iz", "4:ikinci_iz", "5:konvoy", "6:sabotaj", "7:-", "8:-", "9:konvoy", "10:-", "11:-", "12:-" }, seen, string.Join(" ", seen));
            Assert.AreEqual(2, Count(sim, "konvoy"));
        }

        // ---------------------------------------------------------------- hedefi giden olay

        [Test]
        public void Events_TargetGone_ClosesAndRefundsGapAndLimit_SlipUnblocks()
        {
            // (a) Dolu sığınakta aday için yollanan kişi: olay kapanır, sınır ve aralık geri gelir
            var b = For("kacak");
            Limit(b, "kacak", 1, 5);
            b.admissionKnockMin = b.admissionKnockMax = 1;
            b.boardResidentCapacity = b.boardStartResidents;
            b.eventGapMin = b.eventGapMax = 3;
            var sim = New(b);
            Close(sim); Close(sim);
            if (sim.Admission.Group.Count > 0) Assert.IsTrue(sim.PassCandidates().Ok);
            sim.Shelter.Supplies = 0;
            var e = CloseTo(sim, "kacak");
            Assert.Greater(sim.Admission.Group.Count, 0, "test düzeni: aynı kapanışta grup geldi");
            Assert.AreEqual(1, Count(sim, "kacak"));
            Assert.AreEqual(sim.S.Day + 3, sim.ShelterEvents.NextEventShift);
            Assert.AreEqual(sim.S.Day, e.PrevNextEventShift);
            Assert.AreEqual(-1, e.PrevLastShift);
            uint rng = sim.S.Rng.State;
            Assert.IsFalse(sim.PrintShift().Ok);
            Assert.IsTrue(sim.ChooseCandidate(sim.Admission.Group[0].Resident.Id, e.PersonId).Ok);
            Assert.IsNull(sim.PendingShelterEvent, "hedefi giden olay kapanır");
            Assert.AreEqual(0, Count(sim, "kacak"), "tekrar sınırı tüketilmez");
            Assert.AreEqual(-1, sim.ShelterEvents.Count("kacak").LastShift, "bekleme başlamaz");
            Assert.AreEqual(sim.S.Day, sim.ShelterEvents.NextEventShift, "aralık tüketilmez");
            Assert.AreEqual(rng, sim.S.Rng.State, "yapılmış çekim geri alınmaz, yeni çekim yapılmaz");
            var closed = sim.ShelterEvents.Log.Last();
            Assert.AreEqual("kapandi", closed.Kind);
            Assert.AreEqual("kacak", closed.Option);
            Assert.AreEqual(e.PersonId, closed.PersonId);
            Assert.AreEqual(0, Log(sim, "kacak").Count, "karar verilmedi");
            // Fiş kilidi kalktı; sınır 1 olduğu hâlde olay yeniden açılabilir
            sim.Shelter.Supplies = 0;
            var again = CloseTo(sim, "kacak");
            Assert.AreNotEqual(e.PersonId, again.PersonId);
            Assert.AreEqual(1, Count(sim, "kacak"));

            // (b) Hedef baskında ölür: olay baskın çözülünce kapanır; çalınan erzak geri gelmez
            sim = Open("hirsizlik");
            e = sim.PendingShelterEvent;
            double supplies = sim.Shelter.Supplies;
            Assert.AreEqual(2, e.Amount);
            if (sim.Board.Person(e.PersonId).Job != "tek") Assert.IsTrue(sim.AssignResident(e.PersonId, "tek").Ok);
            FillSquad(sim);
            StartRaid(sim);
            Assert.AreSame(e, sim.PendingShelterEvent, "baskın emri olayı kapatmaz");
            ResolveRaid(sim, false, 2, 0, 0);
            Assert.IsFalse(sim.Board.Person(e.PersonId).Alive);
            Assert.IsNull(sim.PendingShelterEvent);
            Assert.AreEqual("kapandi", sim.ShelterEvents.Log.Last().Kind);
            Assert.AreEqual(2, sim.ShelterEvents.Log.Last().Amount);
            Assert.AreEqual(supplies, sim.Shelter.Supplies);
            Assert.AreEqual(0, Count(sim, "hirsizlik"));
            Assert.AreEqual(e.PrevNextEventShift, sim.ShelterEvents.NextEventShift);
            Close(sim);

            // (c) Kapıda yer değiştirme: büyük hırsızlığın hedefi yolluğuyla gider, olay kapanır; tükettiği aralık geri gelir
            sim = Open("buyuk_hirsizlik");
            e = sim.PendingShelterEvent;
            Assert.Greater(sim.ShelterEvents.NextEventShift, e.PrevNextEventShift, "açılış aralığı yeniden başlattı");
            Assert.IsTrue(ReplaceAtTheDoor(sim, e.PersonId).Ok);
            Assert.IsNull(sim.PendingShelterEvent);
            Assert.AreEqual(0, Count(sim, "buyuk_hirsizlik"));
            Assert.AreEqual(e.PrevNextEventShift, sim.ShelterEvents.NextEventShift);
            Close(sim);

            // (d) Kavgada iki kişiden biri giderse olay kapanır; fiş, basılırken yetim olayı kendisi kapatır
            sim = Open("kavga");
            e = sim.PendingShelterEvent;
            sim.Board.Person(e.OtherId).Alive = false;
            Assert.AreSame(e, sim.PendingShelterEvent);
            Close(sim);
            Assert.AreEqual("kapandi", sim.ShelterEvents.Log.Last().Kind);
            Assert.AreEqual(e.OtherId, sim.ShelterEvents.Log.Last().OtherId);
            Assert.AreEqual(0, Count(sim, "kavga"));

            // (e) Zincir olayı: yollanan izli kişinin kâğıdı kapanır
            sim = Open("iz");
            e = sim.PendingShelterEvent;
            Assert.IsTrue(ReplaceAtTheDoor(sim, e.PersonId).Ok);
            Assert.IsNull(sim.PendingShelterEvent);
            Assert.AreEqual(0, Count(sim, "iz"));
            for (int i = 0; i < 4; i++) CloseTo(sim, "-");

            // (f) Anmanın hedefi zaten ölüdür: olay kapanmaz, karar ister
            sim = Open("anma");
            e = sim.PendingShelterEvent;
            Assert.IsFalse(sim.Board.Person(e.PersonId).Alive);
            Assert.IsFalse(sim.PrintShift().Ok);
            Assert.AreSame(e, sim.PendingShelterEvent);
        }

        // ---------------------------------------------------------------- komut koşulları

        [Test]
        public void Events_OpenEvent_BlocksTheSlip_NotTheRaidOrder_DecisionPassesOnALockedPlan()
        {
            var sim = Open("konvoy");
            int commands = sim.Record.commands.Count;
            Assert.AreEqual(Loc.T("Karar bekleyen olay var: önce olay tutanağını karara bağla."), sim.ShelterEventPrintCheck());
            var printed = sim.PrintShift();
            Assert.IsFalse(printed.Ok);
            Assert.AreEqual(sim.ShelterEventPrintCheck(), printed.Msg);
            Assert.IsFalse(sim.Board.Printed);
            Assert.IsFalse(sim.Board.Locked);
            Assert.IsNull(sim.BoardPlanCheck(), "plan denetimi olaya bakmaz");
            Assert.AreEqual(commands, sim.Record.commands.Count);

            // Açık olay baskın emrini engellemez
            FillSquad(sim);
            Assert.IsNull(sim.PlanningRaidCheck());
            StartRaid(sim);
            Assert.AreEqual("konvoy", Pending(sim));

            // Baskın sürerken karar geçmez
            Assert.AreEqual(Loc.T("Baskın sürüyor."), sim.ShelterEventDecisionCheck("kabul"));
            Assert.AreEqual(Loc.T("Baskın sürüyor."), sim.DecideShelterEvent("kabul").Msg);
            Assert.AreEqual("konvoy", Pending(sim));
            ResolveRaid(sim, false, 0, 0, 0);

            // Baskın çözüldü: plan kilitli, fiş hâlâ kapalı, karar geçer
            Assert.IsTrue(sim.Board.Locked);
            Assert.IsFalse(sim.PrintShift().Ok);
            Assert.IsNull(sim.ShelterEventDecisionCheck("kabul"));
            double supplies = sim.Shelter.Supplies;
            Decide(sim, "kabul");
            Assert.AreEqual(supplies + sim.B.convoySupplies, sim.Shelter.Supplies);
            Assert.IsTrue(sim.Board.Locked, "karar plan kilidine dokunmaz");

            // Fişten sonra karar geçmez
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.AreEqual(Loc.T("Fiş basıldı; olay kararı bu vardiya verilemez."), sim.ShelterEventDecisionCheck("kabul"));
            Assert.IsFalse(sim.DecideShelterEvent("kabul").Ok);
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);

            // Oyun bittiyse karar geçmez
            sim = Open("konvoy");
            sim.S.Ending = "silme";
            Assert.AreEqual(Loc.T("Oyun bitti."), sim.ShelterEventDecisionCheck("kabul"));
            Assert.IsFalse(sim.DecideShelterEvent("kabul").Ok);

            // Pano yoksa olay durumu da yoktur
            var plain = Simulation.NewGame(79, new Balance { planningBoardEnabled = false });
            Assert.IsNull(plain.ShelterEvents);
            Assert.IsNull(plain.PendingShelterEvent);
            Assert.AreEqual(0, plain.ShelterEventOptions().Count);
            Assert.IsNull(plain.ShelterEventPrintCheck());
            Assert.AreEqual(Loc.T("Bu kayıtta sığınak olayı yok."), plain.DecideShelterEvent("kabul").Msg);
        }

        /// <summary>Anahtar kapalı: olay, zincir, vadeli çekim ve ayrılma yok; açlık sayacı yine sayılır; eski elle kısıt çalışır.</summary>
        [Test]
        public void Events_Disabled_NoEventNoChainNoDrawNoDesertion()
        {
            // Anahtar açık olsaydı her kapanışta olay açılır, sızmacı iz bırakır ve aç kapanışlarda ayrılma başlardı
            var b = Robots(Quiet(1));
            foreach (string kind in ShelterEventData.Ordinary) Limit(b, kind, 9);
            b.shelterEventsEnabled = false;
            b.convoyTrustAt = 0;
            b.eventDeserterStreak = 1;
            b.desertStreak = 1;
            b.perkSupplies = 0;
            var sim = New(b);
            Assert.IsNotNull(sim.ShelterEvents, "olay durumu kapalıyken de kuruludur");
            string id = Admit(sim);
            Assert.IsTrue(sim.AssignResident(id, "craft").Ok);
            ShutDoor(sim);
            double metal = sim.BoardMaterialFree("metal");
            for (int i = 0; i < 8; i++)
            {
                sim.Shelter.Supplies = 0;
                sim.S.Alarm = b.liberationAlarmThreshold - 1;
                Assert.IsNull(sim.NextDeserter);
                Assert.AreEqual(0, CloseDraws(sim), "kapı kapalı ve olaylar kapalıyken kapanış çekim yapmaz");
                Assert.IsNull(sim.PendingShelterEvent);
                Assert.IsTrue(sim.Shelter.Starving);
                Assert.AreEqual(i + 1, sim.Shelter.HungryStreak, "açlık sayacı anahtardan bağımsız sayılır");
            }
            Assert.AreEqual(0, sim.ShelterEvents.Log.Count);
            Assert.AreEqual(0, sim.ShelterEvents.Counts.Count);
            Assert.AreEqual(0, sim.Shelter.Deserted, "kâğıtsız ayrılma olmaz");
            Assert.AreEqual(9, sim.Board.People.Count(p => p.Alive));
            Assert.AreEqual(0, sim.Shelter.Exposure);
            var f = sim.Admission.File(id);
            Assert.AreEqual(0, f.ClueCount, "eski sızma zinciri de sessiz");
            Assert.IsFalse(f.Revealed);
            Assert.AreEqual(metal, sim.BoardMaterialFree("metal"), "sabotaj yok");
            Assert.IsFalse(sim.HalfRationActive);
            Assert.IsFalse(sim.BlackoutActive);
            Assert.IsFalse(sim.BoardReceiptPreview.Contains(Loc.T("ERZAK · aç vardiya: {0}.").Substring(0, 5)), "fişin açlık satırları yalnız olaylar açıkken");

            // Kısıtı yalnız olay kararı kurar (eski elle kısıt aşama 7'de silindi): kapalıyken kimse kısıtlı olmaz, görev kuralı aynı kalır
            Assert.IsTrue(sim.Board.People.All(p => !p.Restricted));
            Assert.IsTrue(sim.Admission.Files.All(x => x.RestrictedUntil == -1));
        }

        // ---------------------------------------------------------------- sızma zinciri

        [Test]
        public void Chain_IgnoredInfiltrator_TraceSecondTraceInterventionShiftSabotage()
        {
            var sim = New(Robots(Quiet(1), 2, 2));
            string id = Admit(sim);
            int admitted = sim.S.Day;
            Assert.IsTrue(sim.AssignResident(id, "craft").Ok);
            ShutDoor(sim);
            var f = sim.Admission.File(id);
            var person = f.Resident;
            uint rng = sim.S.Rng.State;

            // Gecikme 2: birinci iz ikinci kapanışta
            CloseTo(sim, "-");
            Assert.AreEqual(0, f.ClueCount);
            var e = CloseTo(sim, "iz");
            Assert.AreEqual(admitted + 2, sim.S.Day);
            Assert.AreEqual(id, e.PersonId);
            Assert.AreEqual("uretim", e.Area, "iz yeri kapanan vardiyanın görevinden gelir");
            Assert.AreEqual(1, f.ClueCount, "iz, kâğıdı açıldığında dosyaya düşer");
            CollectionAssert.AreEqual(new[] { "uretim" }, f.TraceAreas);
            Assert.AreEqual(Loc.F("{0}: üretim dolabında yetkisiz erişim kaydı var.", person.Name), sim.ShelterEventBody(e));
            Assert.IsTrue(sim.ShelterEventOptions().All(o => o.Open));
            Assert.AreEqual(0, sim.Shelter.Exposure);
            Decide(sim, "yoksay");
            Assert.AreEqual(1, sim.Shelter.Exposure, "görmezden gelinen iz ifşayı artırır");
            Assert.AreEqual(1, Log(sim, "ifsa").Single().Amount);
            Assert.IsFalse(f.Revealed);
            Assert.AreEqual("craft", person.Job, "iz kısıt getirmez");

            // İkinci iz aralığı 2: bir boş kapanış, sonra ikinci iz
            CloseTo(sim, "-");
            Assert.IsTrue(sim.AssignResident(id, "cook").Ok);
            e = CloseTo(sim, "ikinci_iz");
            Assert.AreEqual(admitted + 4, sim.S.Day);
            Assert.AreEqual("erzak", e.Area);
            Assert.AreEqual(2, f.ClueCount);
            CollectionAssert.AreEqual(new[] { "uretim", "erzak" }, f.TraceAreas);
            Assert.AreEqual(Loc.F("Kişi sızmacıysa sabotaj gelir: ifşa +{0}.", sim.B.exposureSabotage), Option(sim, "yoksay").Risk);
            Decide(sim, "yoksay");
            Assert.AreEqual(2, sim.Shelter.Exposure);

            // Müdahale vardiyası: olay yok, kayıp yok
            double supplies = sim.Shelter.Supplies;
            CloseTo(sim, "-");
            Assert.IsFalse(f.Revealed);
            Assert.AreEqual(supplies + sim.Board.LastSuppliesChange, sim.Shelter.Supplies);

            // Sabotaj: kayıp uygulanmış, ifşa +2, kişi ortaya çıkmış ve yer tutmadan bekletiliyor
            supplies = sim.Shelter.Supplies;
            double eaten = -sim.BoardFoodPreview;
            e = CloseTo(sim, "sabotaj");
            Assert.AreEqual(admitted + 6, sim.S.Day);
            Assert.AreEqual("erzak", e.Resource, "son görevi Mutfak: erzak kaybı");
            Assert.AreEqual(sim.B.admissionSabotageRations, e.Amount);
            Assert.AreEqual(supplies - eaten - 3, sim.Shelter.Supplies, 1e-9);
            Assert.AreEqual(4, sim.Shelter.Exposure);
            Assert.IsTrue(f.Revealed);
            Assert.IsTrue(person.Isolated);
            Assert.AreEqual("rest", person.Job);
            Assert.AreEqual(-1, f.IsolatedUntil);
            Assert.AreEqual(1, sim.IsolationFree, "karar bekleyen sızmacı tecrit yerini tutmaz");
            Assert.AreEqual(Loc.F("{0} sabotaj yaptı ve ortaya çıktı: {1:0.##} erzak kaybedildi.", person.Name, 3.0), sim.ShelterEventBody(e));
            Assert.AreEqual(rng, sim.S.Rng.State, "zincir hiç çekim yapmaz");

            // Sök: kişi listeden çıkar, yolluk ve malzeme yok
            supplies = sim.Shelter.Supplies;
            double materials = sim.Shelter.Materials;
            Decide(sim, "sok");
            Assert.IsNull(sim.Board.Person(id));
            Assert.IsTrue(f.Departed);
            Assert.AreEqual("sokuldu", f.DepartReason);
            Assert.AreEqual(sim.S.Day, f.DepartShift);
            Assert.AreEqual(supplies, sim.Shelter.Supplies);
            Assert.AreEqual(materials, sim.Shelter.Materials);
            Assert.AreEqual(4, sim.Shelter.Exposure);
            CollectionAssert.AreEqual(new[] { "iz/yoksay", "ikinci_iz/yoksay", "sabotaj/sok" },
                sim.ShelterEvents.Log.Where(l => l.Option != null && l.Kind != "ifsa").Select(l => l.Kind + "/" + l.Option).ToArray());
            // İfşa satırları kaynağı ve kişiyi taşır (son raporu)
            CollectionAssert.AreEqual(new[] { "iz:1", "ikinci_iz:1", "sabotaj:2" },
                sim.ShelterEvents.Log.Where(l => l.Kind == "ifsa").Select(l => l.Option + ":" + l.Amount).ToArray());
            Assert.IsTrue(sim.ShelterEvents.Log.Where(l => l.Kind == "ifsa").All(l => l.PersonId == id));
            for (int i = 0; i < 4; i++) CloseTo(sim, "-");
        }

        /// <summary>Saat görev ayırmaz: Boşta ya da Eğitimde tutulan sızmacı da aynı kapanışta iz bırakır.</summary>
        [Test]
        public void Chain_IdleOrTrainingInfiltrator_ClockStillRuns()
        {
            foreach (string job in new[] { "rest", "train", "cook", "tek" })
            {
                var sim = New(Robots(Quiet(1), 3));
                string id = Admit(sim);
                int admitted = sim.S.Day;
                ShutDoor(sim);
                var person = sim.Board.Person(id);
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual("-", Pending(sim));
                    if (job == "train") Assert.IsTrue(sim.TrainResident(id, person.TrainingSubject == "cook" ? "craft" : "cook").Ok);
                    else if (job != "rest" && person.Job != job) Assert.IsTrue(sim.AssignResident(id, job).Ok);
                    Close(sim);
                }
                var e = sim.PendingShelterEvent;
                Assert.IsNotNull(e, job + ": iz üçüncü kapanışta");
                Assert.AreEqual("iz", e.Kind);
                Assert.AreEqual(admitted + 3, sim.S.Day);
                Assert.AreEqual(job == "cook" ? "erzak" : job == "tek" ? "uretim" : "nobet", e.Area, job);
            }
        }

        [Test]
        public void Chain_BedAndIsolation_StopTheClock()
        {
            // Yatış: iki vardiya yatan sızmacının izi iki kapanış gecikir
            var sim = New(Robots(Quiet(1), 2));
            string id = Admit(sim);
            ShutDoor(sim);
            sim.Board.Person(id).Down = 2;
            CloseTo(sim, "-"); CloseTo(sim, "-"); CloseTo(sim, "-");
            CloseTo(sim, "iz");

            // Tecrit: üç kapanış saat durur; "bulgu yok"tan sonra kaldığı yerden sürer
            var b = Robots(Quiet(1), 2, 2);
            b.isolationFindChance = b.isolationFindChanceTwo = 0;
            sim = New(b);
            id = Admit(sim);
            ShutDoor(sim);
            CloseTo(sim, "-");
            CloseTo(sim, "iz");
            Decide(sim, "tecrit");
            CloseTo(sim, "-"); CloseTo(sim, "-"); CloseTo(sim, "-");
            Assert.AreEqual(1, Log(sim, "bulgu_yok").Count);
            Assert.IsFalse(sim.Board.Person(id).Isolated);
            // İkinci iz aralığı 2: tecrit olmasa iki kapanış sonra gelirdi; tecritten çıkınca iki kapanış daha ister
            CloseTo(sim, "-");
            CloseTo(sim, "ikinci_iz");
        }

        /// <summary>Aynı kapanışta iki adımın vadesi gelirse dosya sırasıyla biri açılır, öteki sonraki kapanışı bekler.</summary>
        [Test]
        public void Chain_TwoDueSteps_OneOpens_TheOtherWaitsForTheNextClosing()
        {
            // Zincir sırasını sınar, sonu değil: ifşa eşiği (dusus ve tavan) kapalı
            var balance = Robots(Quiet(1), 2);
            balance.exposureThreshold = 0;
            var sim = New(balance);
            string a = Admit(sim);
            // İlk sızmacı bir vardiya yatar: iki sızmacının ilk izi aynı kapanışa denk gelir
            sim.Board.Person(a).Down = 1;
            string b = Admit(sim, 0);
            Assert.AreEqual(sim.Admission.File(a).AcceptedShift + 1, sim.Admission.File(b).AcceptedShift);
            ShutDoor(sim);
            var seen = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                var e = sim.PendingShelterEvent;
                if (e != null) Decide(sim, e.Kind == "sabotaj" ? "sok" : "yoksay");
                Close(sim);
                e = sim.PendingShelterEvent;
                seen.Add(e == null ? "-" : e.Kind + ":" + (e.PersonId == a ? "a" : "b"));
                if (seen.Count == 2)
                {
                    Assert.AreEqual(1, sim.Admission.File(a).ClueCount);
                    Assert.AreEqual(0, sim.Admission.File(b).ClueCount, "sıradaki iz, kâğıdı açılana dek dosyaya düşmez");
                }
            }
            CollectionAssert.AreEqual(new[] { "-", "iz:a", "ikinci_iz:a", "iz:b", "sabotaj:a", "ikinci_iz:b", "-", "sabotaj:b" }, seen, string.Join(" ", seen));
            Assert.AreEqual(8, sim.Shelter.Exposure, "dört görmezden gelinen iz + iki sabotaj");
        }

        [Test]
        public void Chain_Human_TraceOnlyWhenDrawn_NeverSabotages()
        {
            // İki iz bırakan insan: iz, ikinci iz, sonra hiçbir şey
            var sim = New(Humans(Quiet(1), 1, 1));
            string id = Admit(sim);
            ShutDoor(sim);
            double metal = sim.BoardMaterialFree("metal");
            CloseTo(sim, "iz");
            Decide(sim, "yoksay");
            CloseTo(sim, "ikinci_iz");
            Decide(sim, "yoksay");
            for (int i = 0; i < 8; i++) CloseTo(sim, "-");
            var f = sim.Admission.File(id);
            Assert.AreEqual(2, f.ClueCount);
            Assert.IsFalse(f.Revealed);
            Assert.IsFalse(f.Departed);
            Assert.AreEqual(2, sim.Shelter.Exposure, "görmezden gelinen insan izi de ifşayı artırır; sabotajın +2'si yok");
            Assert.AreEqual(metal, sim.BoardMaterialFree("metal"));

            // Yalnız birinci izi bırakan insan
            sim = New(Humans(Quiet(1), 1, 0));
            id = Admit(sim);
            ShutDoor(sim);
            CloseTo(sim, "iz");
            Decide(sim, "yoksay");
            for (int i = 0; i < 8; i++) CloseTo(sim, "-");
            Assert.AreEqual(1, sim.Admission.File(id).ClueCount);

            // İz çekimi olmayan insan
            sim = New(Humans(Quiet(1), 0, 1));
            id = Admit(sim);
            ShutDoor(sim);
            for (int i = 0; i < 8; i++) CloseTo(sim, "-");
            Assert.AreEqual(0, sim.Admission.File(id).ClueCount);
            Assert.AreEqual(0, sim.ShelterEvents.Log.Count);
        }

        /// <summary>İlk grup sızmacı olamaz (insan izi bırakabilir); kurucular hiç iz bırakmaz.</summary>
        [Test]
        public void Chain_FirstGroupCannotBeInfiltrators_FoundersLeaveNoTrace()
        {
            // Sızmacı olasılığı 1, insan izi 0: ilk gruptan alınan kişi hiç iz bırakmaz, ikinci gruptan alınan bırakır
            var sim = New(Robots(Quiet(1)));
            string first = Admit(sim, 0);
            string second = Admit(sim, 0);
            ShutDoor(sim);
            var people = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                var e = sim.PendingShelterEvent;
                if (e != null) { people.Add(e.PersonId); Decide(sim, e.Kind == "sabotaj" ? "sok" : "yoksay"); }
                Close(sim);
            }
            CollectionAssert.AreEqual(new[] { second, second, second }, people);
            Assert.AreEqual(0, sim.Admission.File(first).ClueCount);
            Assert.IsFalse(sim.Admission.File(first).Departed);
            Assert.IsTrue(sim.Admission.File(second).Revealed);

            // İnsan izi 1: ilk gruptan alınan insan iz bırakır ama sabotaj yapmaz
            sim = New(Humans(Quiet(1), 1, 1));
            first = Admit(sim, 0);
            ShutDoor(sim);
            CloseTo(sim, "iz");
            Assert.AreEqual(first, sim.PendingShelterEvent.PersonId);
            Decide(sim, "yoksay");
            CloseTo(sim, "ikinci_iz");
            Decide(sim, "yoksay");
            for (int i = 0; i < 6; i++) CloseTo(sim, "-");
            foreach (var f in sim.Admission.Files.Where(x => x.Founder))
            {
                Assert.AreEqual(0, f.ClueCount, "kurucu iz bırakmaz");
                Assert.AreEqual(0, f.TraceAreas.Count);
                Assert.IsFalse(f.Revealed);
            }
        }

        [TestCase("craft", 1, "metal", 1)]
        [TestCase("craft", 5, "metal", 2)]
        [TestCase("craft", 0, "metal", 0)]
        [TestCase("ana", 5, "metal", 2)]
        [TestCase("cook", 1.25, "erzak", 1.25)]
        [TestCase("cook", 20, "erzak", 3)]
        [TestCase("rest", 20, "erzak", 3)]
        [TestCase("rest", 0, "erzak", 0)]
        public void Chain_SabotageLoss_MetalOrSupplies_CappedByStock(string job, double stock, string resource, double loss)
        {
            var sim = Open("ikinci_iz", b => b.boardSuppliesPerResident = 0);
            string id = sim.PendingShelterEvent.PersonId;
            Decide(sim, "yoksay");
            if (job != "rest") Assert.IsTrue(sim.AssignResident(id, job).Ok);
            CloseTo(sim, "-");
            var metal = sim.Board.Material("metal");
            if (resource == "metal") metal.Quantity = (int)stock; else sim.Shelter.Supplies = stock;
            int metalBefore = metal.Quantity;
            double suppliesBefore = sim.Shelter.Supplies;
            var e = CloseTo(sim, "sabotaj");
            Assert.AreEqual(resource, e.Resource);
            Assert.AreEqual(loss, e.Amount, "kâğıt gerçek kaybı yazar");
            Assert.AreEqual(metalBefore - (resource == "metal" ? (int)loss : 0), metal.Quantity);
            Assert.AreEqual(suppliesBefore - (resource == "erzak" ? loss : 0), sim.Shelter.Supplies, 1e-9);
            StringAssert.Contains(resource == "metal" ? ((int)loss).ToString() + " metal" : "erzak", sim.ShelterEventBody(e));
        }

        [Test]
        public void Sabotage_Interrogate_NeedsNoRoom_ThenRemoves_ExposureHook()
        {
            var sim = Open("sabotaj");
            var e = sim.PendingShelterEvent;
            string id = e.PersonId;
            var f = sim.Admission.File(id);
            Assert.AreEqual(4, sim.Shelter.Exposure);
            Assert.IsTrue(Option(sim, "sok").Safe);
            Decide(sim, "sorgula");
            Assert.AreEqual(3, sim.Shelter.Exposure, "sorgu ifşayı 1 azaltır");
            Assert.AreEqual(-1, sim.ShelterEvents.Log.Last(l => l.Kind == "ifsa").Amount);
            Assert.AreEqual(sim.S.Day + 3, f.IsolatedUntil);
            Assert.AreEqual(1, sim.IsolationFree, "ikinci tur kararı: sorgu tecrit yeri tutmaz (kişi yer tutmadan bekletilir)");
            Assert.IsNotNull(sim.Board.Person(id), "sorgu sürerken kişi sığınakta ve yer");
            Assert.AreEqual(Loc.T("Tecritteki sakin görev alamaz."), sim.AssignResident(id, "cook").Msg);
            uint rng = sim.S.Rng.State;
            CloseTo(sim, "-"); CloseTo(sim, "-");
            Assert.IsNotNull(sim.Board.Person(id));
            CloseTo(sim, "-");
            Assert.AreEqual(rng, sim.S.Rng.State, "ortaya çıkmış kişi çekimsiz sökülür");
            Assert.IsNull(sim.Board.Person(id));
            Assert.AreEqual("sokuldu", f.DepartReason);
            Assert.AreEqual(1, sim.IsolationFree);
            Assert.AreEqual(id, Log(sim, "sokuldu").Single().PersonId);
            Assert.AreEqual(0, Log(sim, "bulgu").Count + Log(sim, "bulgu_yok").Count, "sorgu bulgu kâğıdı üretmez");
            Assert.AreEqual(3, sim.Shelter.Exposure);

            // Tecrit yeri doluyken de sorgu açık (ikinci tur kararı); bedel satırı yer tutmadığını söyler; yer dolu kalır
            sim = Open("sabotaj");
            var other = sim.Board.People.First(p => p.Alive && p.Id != sim.PendingShelterEvent.PersonId);
            other.Isolated = true;
            sim.Admission.File(other.Id).IsolatedUntil = sim.S.Day + 2;
            Assert.AreEqual(0, sim.IsolationFree);
            var option = Option(sim, "sorgula");
            Assert.IsTrue(option.Open);
            Assert.IsNull(option.ClosedReason);
            Assert.AreEqual(Loc.F("{0} vardiya yer tutmadan bekletilir, sonra sökülür; ifşa {1}.", sim.B.isolationShifts, sim.B.exposureInterrogation), option.Cost);
            Decide(sim, "sorgula");
            Assert.AreEqual(0, sim.IsolationFree, "sorgulanan kişi yeri doldurmaz, öteki yeri tutmaya devam eder");
        }

        // ---------------------------------------------------------------- tecrit

        [Test]
        public void Isolation_ThreeShifts_NoWorkNoGearStillEats_OneRoom()
        {
            var b = Robots(Quiet(1), 2);
            b.isolationFindChance = b.isolationFindChanceTwo = 0;
            var sim = New(b);
            string a = Admit(sim);
            sim.Board.Person(a).Down = 1;
            string second = Admit(sim, 0);
            ShutDoor(sim);
            // Bağlı olmayan zırh ve kişiye bağlı kit: tecritte yalnız bağlı olan kalır
            var armor = sim.AddBoardEquipment("armor", a);
            var kit = sim.AddBoardEquipment("kit_ana", a, true);
            CloseTo(sim, "-");
            var e = CloseTo(sim, "iz");
            Assert.AreEqual(a, e.PersonId);
            Assert.IsTrue(sim.AssignResident(a, "cook").Ok);
            Assert.AreEqual(1, sim.IsolationFree);
            StringAssert.Contains(Loc.F("{0} vardiya çalışmaz, yer; tecrit yerini tutar.", 3), Option(sim, "tecrit").Cost);
            double eaten = sim.BoardConsumption;
            Decide(sim, "tecrit");
            var person = sim.Board.Person(a);
            var f = sim.Admission.File(a);
            Assert.IsTrue(person.Isolated);
            Assert.AreEqual("rest", person.Job);
            Assert.AreEqual(sim.S.Day + 3, f.IsolatedUntil);
            Assert.AreEqual(0, sim.IsolationFree);
            Assert.IsNull(armor.OwnerId, "bağsız teçhizat depoya döner");
            Assert.AreEqual(a, kit.OwnerId, "bağlı teçhizat kişide kalır");
            Assert.AreEqual(0, sim.Shelter.Exposure, "tecrit ifşayı artırmaz");
            Assert.AreEqual(Loc.T("Tecritteki sakin görev alamaz."), sim.AssignResident(a, "cook").Msg);
            Assert.AreEqual(Loc.T("Tecritteki sakin görev alamaz."), sim.TrainResident(a, "cook").Msg);
            Assert.AreEqual(Loc.T("Bu sakin teçhizat kullanamıyor."), sim.EquipResident(armor.Id, a).Msg);
            Assert.IsTrue(sim.AssignResident(a, "rest").Ok || person.Job == "rest");
            Assert.Greater(sim.BoardConsumption, eaten, "tecritteki kişi yer; aşçılığı ise durur");
            Assert.IsNull(sim.BoardPlanCheck());

            // Tek tecrit yeri: ikinci sızmacının izinde tecrit kapalı
            e = CloseTo(sim, "iz");
            Assert.AreEqual(second, e.PersonId);
            Assert.IsFalse(Option(sim, "tecrit").Open);
            Assert.AreEqual(Loc.T("Tecrit yeri dolu."), Option(sim, "tecrit").ClosedReason);
            Assert.AreEqual(Loc.T("Tecrit yeri dolu."), sim.DecideShelterEvent("tecrit").Msg);
            Assert.IsTrue(Option(sim, "yolla").Open);
            Decide(sim, "yoksay");

            // Üçüncü kapanışta biter: bulgu yok, kişi Boşta döner, izler dosyada kalır
            Close(sim);
            Assert.IsTrue(person.Isolated);
            if (sim.PendingShelterEvent != null) Decide(sim, "yoksay");
            Close(sim);
            Assert.IsFalse(person.Isolated);
            Assert.AreEqual(-1, f.IsolatedUntil);
            Assert.AreEqual("rest", person.Job);
            Assert.AreEqual(1, sim.IsolationFree);
            Assert.AreEqual(a, Log(sim, "bulgu_yok").Single().PersonId);
            Assert.AreEqual(1, f.ClueCount, "bulgu yok kimseyi temize çıkarmaz");
            Assert.IsFalse(f.Revealed);
            if (sim.PendingShelterEvent != null) Decide(sim, "yoksay");
            Assert.IsTrue(sim.AssignResident(a, "cook").Ok, "tecritten çıkan kişi yeniden görev alır");
        }

        [Test]
        public void Isolation_Infiltrator_BothOutcomes_SecondTraceRaisesTheChance()
        {
            // Bulgu: sızmacı sökülür, yolluk yok
            var sim = Open("iz", b => b.isolationFindChance = 1);
            string id = sim.PendingShelterEvent.PersonId;
            var f = sim.Admission.File(id);
            Decide(sim, "tecrit");
            CloseTo(sim, "-"); CloseTo(sim, "-");
            double supplies = sim.Shelter.Supplies + sim.BoardFoodPreview;
            CloseTo(sim, "-");
            Assert.IsNull(sim.Board.Person(id));
            Assert.IsTrue(f.Departed);
            Assert.IsTrue(f.Revealed);
            Assert.AreEqual("sokuldu", f.DepartReason);
            Assert.AreEqual(id, Log(sim, "bulgu").Single().PersonId);
            Assert.AreEqual(supplies, sim.Shelter.Supplies, 1e-9, "sökülen kişi yolluk almaz");
            Assert.AreEqual(1, sim.IsolationFree);
            Assert.AreEqual(0, sim.Shelter.Exposure);
            for (int i = 0; i < 5; i++) CloseTo(sim, "-");

            // Bulgu yok: kişi kalır, saat sürer; ikinci izde tecrit yeniden seçilebilir ve iki izin olasılığı ayrıdır
            sim = Open("iz", b => { b.isolationFindChance = 0; b.isolationFindChanceTwo = 1; });
            id = sim.PendingShelterEvent.PersonId;
            f = sim.Admission.File(id);
            Decide(sim, "tecrit");
            CloseTo(sim, "-"); CloseTo(sim, "-"); CloseTo(sim, "-");
            Assert.AreEqual(1, Log(sim, "bulgu_yok").Count);
            Assert.IsFalse(f.Departed);
            Assert.IsFalse(f.Revealed);
            CloseTo(sim, "ikinci_iz");
            Assert.IsTrue(Option(sim, "tecrit").Open);
            Decide(sim, "tecrit");
            CloseTo(sim, "-"); CloseTo(sim, "-"); CloseTo(sim, "-");
            Assert.AreEqual(1, Log(sim, "bulgu").Count);
            Assert.IsTrue(f.Departed);
            Assert.AreEqual(0, sim.Shelter.Exposure, "iki tecrit: iz hiç görmezden gelinmedi, sabotaj olmadı");
        }

        [Test]
        public void Isolation_Human_AlwaysNoFinding_TracesStay()
        {
            var sim = New(Humans(Quiet(1), 1, 1));
            sim.B.isolationFindChance = sim.B.isolationFindChanceTwo = 1;
            string id = Admit(sim);
            ShutDoor(sim);
            var f = sim.Admission.File(id);
            foreach (string kind in new[] { "iz", "ikinci_iz" })
            {
                CloseTo(sim, kind);
                Decide(sim, "tecrit");
                CloseTo(sim, "-"); CloseTo(sim, "-");
                Close(sim);
                Assert.IsFalse(f.Departed);
                Assert.IsFalse(f.Revealed);
                Assert.IsFalse(f.Resident.Isolated);
            }
            Assert.AreEqual(2, Log(sim, "bulgu_yok").Count);
            Assert.AreEqual(0, Log(sim, "bulgu").Count);
            Assert.AreEqual(2, f.ClueCount);
            CollectionAssert.AreEqual(new[] { "nobet", "nobet" }, f.TraceAreas);
        }

        // ---------------------------------------------------------------- kilitsizlik

        static readonly string[] Extremes = { "canli", "erzak", "guven", "tecrit", "hepsi" };

        /// <summary>Uç durumu açık olayın üstüne doğrudan yazar.</summary>
        static void Extreme(Simulation sim, string extreme)
        {
            var e = sim.PendingShelterEvent;
            if (extreme == "canli" || extreme == "hepsi")
            {
                // Yalnız olayın kişileri sağ kalır (hedefsiz olayda ve anmada ilk canlı kişi)
                var keep = new HashSet<string>();
                if (ShelterEventData.NeedsLivingTarget(e.Kind)) { keep.Add(e.PersonId); if (e.OtherId != null) keep.Add(e.OtherId); }
                else keep.Add(sim.Board.People.First(p => p.Alive).Id);
                foreach (var p in sim.Board.People.Where(p => !keep.Contains(p.Id))) p.Alive = false;
            }
            if (extreme == "erzak" || extreme == "hepsi")
            {
                sim.Shelter.Supplies = 0;
                foreach (var m in sim.Board.Materials) m.Quantity = 0;
            }
            if (extreme == "guven" || extreme == "hepsi")
            {
                sim.S.Trust = 1;
                sim.S.Alarm = sim.B.liberationAlarmThreshold - 0.5;
            }
            if (extreme == "tecrit")
            {
                var other = sim.Board.People.First(p => p.Alive && p.Id != e.PersonId && p.Id != e.OtherId);
                other.Isolated = true;
                other.Job = "rest";
                sim.Admission.File(other.Id).IsolatedUntil = sim.S.Day + 3;
            }
        }

        /// <summary>
        /// Her olay × uç durum (canlı 1, erzak ve malzeme 0, güven 1 ve alarm eşikte, tecrit yeri dolu, hepsi birden): en az bir açık
        /// seçenek vardır, güvenli seçenek (tek istisna dışında) açıktır ve hiçbir açık seçenek karar anında son üretmez; fiş basılır.
        /// </summary>
        [Test]
        public void Deadlock_EveryEvent_EveryExtreme_HasAnOpenOption_AndNoEndingOnDecision()
        {
            int decided = 0;
            foreach (string kind in ShelterEventData.Kinds)
            {
                foreach (string extreme in Extremes)
                {
                    string[] ids = ShelterEventData.Options(kind);
                    int open = 0;
                    for (int k = 0; k < ids.Length; k++)
                    {
                        string where = kind + " × " + extreme + " × " + ids[k];
                        var sim = Open(kind);
                        Extreme(sim, extreme);
                        Assert.IsNull(sim.S.Ending, where);
                        Assert.AreEqual(kind, Pending(sim), where);
                        int alive = sim.Board.People.Count(p => p.Alive);
                        var options = sim.ShelterEventOptions();
                        var option = options[k];
                        var safe = options.Single(o => o.Safe);
                        // Güvenli seçenek kapanmaz; tek istisna: son canlı kişi ortaya çıkmış sızmacıysa "sök" kapalı, "sorgula" açıktır
                        if (kind == "sabotaj" && alive == 1) Assert.IsTrue(options.Single(o => o.Id == "sorgula").Open, where);
                        else Assert.IsTrue(safe.Open, where + ": güvenli seçenek " + safe.Id + " kapalı (" + safe.ClosedReason + ")");
                        int commands = sim.Record.commands.Count;
                        if (!option.Open)
                        {
                            Assert.IsNotEmpty(option.ClosedReason, where);
                            var refused = sim.DecideShelterEvent(option.Id);
                            Assert.IsFalse(refused.Ok, where);
                            Assert.AreEqual(option.ClosedReason, refused.Msg, where);
                            Assert.AreEqual(kind, Pending(sim), where);
                            Assert.AreEqual(commands, sim.Record.commands.Count, where);
                            continue;
                        }
                        open++;
                        int trust = sim.S.Trust;
                        Decide(sim, option.Id);
                        decided++;
                        Assert.IsNull(sim.S.Ending, where + ": karar anında son");
                        Assert.Greater(sim.S.Trust, 0, where);
                        Assert.GreaterOrEqual(sim.S.Trust, Math.Min(trust, 1), where);
                        Assert.GreaterOrEqual(sim.Board.People.Count(p => p.Alive), 1, where + ": son kişi sığınaktan çıkarılamaz");
                        Assert.GreaterOrEqual(sim.Shelter.Supplies, 0, where);
                        Assert.AreEqual(0, sim.S.Counterattacks, where + ": karar karşı saldırı üretmez");
                        Assert.IsTrue(sim.Board.Materials.All(m => m.Quantity >= 0), where);
                        // Fiş kilidi kalkar ve vardiya kapanır
                        var printed = sim.PrintShift();
                        Assert.IsTrue(printed.Ok, where + ": " + printed.Msg);
                        Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok, where);
                    }
                    Assert.Greater(open, 0, kind + " × " + extreme + ": açık seçenek yok");
                }
            }
            Assert.Greater(decided, 120);
        }

        // ---------------------------------------------------------------- kayıt ve yeniden oynatma

        /// <summary>Yalnız komutlarla oynanan rastgele politika: aday al ya da çevir, kadroyu doldur, baskın, olay kararı, fiş.</summary>
        static void Play(Simulation sim, Random rng, int shifts, double win = 0.6, int collateral = 2)
        {
            for (int shift = 0; shift < shifts && sim.S.Ending == null; shift++)
            {
                if (rng.Next(2) == 0) DecideRandomly(sim, rng);
                var a = sim.Admission;
                if (a.Group.Count > 0)
                {
                    int k = rng.Next(a.Group.Count + 1);
                    if (k == a.Group.Count) Assert.IsTrue(sim.PassCandidates().Ok);
                    else
                    {
                        var alive = sim.Board.People.Where(p => p.Alive).ToList();
                        string replace = alive.Count >= sim.B.boardResidentCapacity ? alive[rng.Next(alive.Count)].Id : null;
                        var chosen = sim.ChooseCandidate(a.Group[k].Resident.Id, replace);
                        Assert.IsTrue(chosen.Ok, chosen.Msg);
                    }
                }
                if (sim.LiberationRegion(sim.S.Liberation.CurrentRegion).Completed)
                    sim.SelectLiberationRegion(sim.LiberationFrontier.First());
                foreach (string job in new[] { "tek", "emp", "ana", "cook", "craft" })
                {
                    if (sim.Board.People.Any(p => p.Alive && p.Down <= 0 && p.Hp > 0 && p.Job == job)) continue;
                    foreach (var p in sim.Board.People.Where(p => p.Alive && p.Job == "rest").ToList())
                        if (sim.AssignResident(p.Id, job).Ok) break;
                }
                foreach (string role in PlanningBoardData.FieldJobs)
                {
                    var p = sim.Board.People.FirstOrDefault(x => x.Alive && x.Job == role);
                    if (p == null || sim.Board.Equipment.Any(x => x.OwnerId == p.Id && x.Kind == "kit_" + role)) continue;
                    var kit = sim.Board.Equipment.FirstOrDefault(x => x.Kind == "kit_" + role && !x.Bound);
                    if (kit != null) sim.EquipResident(kit.Id, p.Id);
                }
                var sector = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
                if (sector != null && sim.LiberationRaidCheck(sector.Id) == null)
                {
                    Assert.IsTrue(sim.StartLiberationRaid(sector.Id).Ok);
                    var squad = sim.S.Raid.Params.Squad;
                    var result = new RaidResult { Success = rng.NextDouble() < win, CiviliansLeft = 3, Collateral = rng.Next(collateral) };
                    for (int i = 0; i < squad.Count; i++)
                    {
                        int roll = rng.Next(8);
                        result.Hp.Add(roll <= 1 ? 0 : Math.Max(1, squad[i].Hp - rng.Next(2)));
                        result.Armor.Add(squad[i].Armor);
                        if (roll == 0) result.Left.Add(i);
                        if (roll <= 1) result.Down++;
                    }
                    Assert.IsTrue(sim.ResolveRaid(result).Ok);
                    if (sim.S.Ending != null) return;
                }
                DecideRandomly(sim, rng);
                if (!sim.PrintShift().Ok)
                {
                    foreach (var o in sim.Board.Orders.ToList()) sim.CancelProduction(o.Id);
                    foreach (var p in sim.Board.People.Where(p => p.Alive && p.Job != "rest").ToList())
                        if (sim.BoardPlanCheck() != null) sim.AssignResident(p.Id, "rest");
                    var printed = sim.PrintShift();
                    Assert.IsTrue(printed.Ok, "vardiya " + sim.S.Day + ": " + printed.Msg);
                }
                Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            }
        }

        static void DecideRandomly(Simulation sim, Random rng)
        {
            if (sim.PendingShelterEvent == null) return;
            var open = sim.ShelterEventOptions().Where(o => o.Open).ToList();
            Assert.Greater(open.Count, 0, sim.PendingShelterEvent.Kind + ": açık seçenek yok");
            uint before = sim.S.Rng.State;
            Decide(sim, open[rng.Next(open.Count)].Id);
            Assert.AreEqual(before, sim.S.Rng.State);
            Assert.IsNull(sim.S.Ending, "karar son üretmez");
        }

        static string Signature(Simulation sim) => string.Join("|", sim.S.Day, sim.S.Rng.State, sim.S.Ending, sim.Shelter.Supplies, sim.S.Trust, sim.S.Alarm, sim.S.Fon,
            sim.Shelter.Exposure, sim.Shelter.HungryStreak, sim.Shelter.Deserted, sim.Shelter.HalfRationUntil, sim.Shelter.BlackoutShift) + "\n" + BoardJson(sim);

        static GameRecord RoundTrip(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            return UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
        }

        /// <summary>Varsayılan denge (olaylar açık); kapı sık çalar ki zincir de oynansın.</summary>
        static Balance Campaign() => new Balance { admissionKnockMin = 2, admissionKnockMax = 3, candidateTier1Robot = 0.5, candidateTier2Robot = 0.8 };

        [Test]
        public void Replay_EventDecide_JsonRoundTrip_AndContinuesIdentically()
        {
            var allKinds = new HashSet<string>();
            foreach (uint seed in new uint[] { 79, 5, 31, 1004 })
            {
                var sim = Simulation.NewGame(seed, Campaign());
                Play(sim, new Random((int)seed), 30);
                var decisions = sim.Record.commands.Where(c => c.kind == "event_decide").ToList();
                Assert.Greater(decisions.Count, 4, "tohum " + seed + ": olay kararı verildi");
                // Kayıt, kararı günlükteki olayla aynı sırada taşır
                var logged = sim.ShelterEvents.Log.Where(l => ShelterEventData.Kinds.Contains(l.Kind)).ToList();
                Assert.AreEqual(decisions.Count, logged.Count);
                for (int i = 0; i < decisions.Count; i++)
                {
                    Assert.AreEqual(logged[i].Option, decisions[i].a);
                    Assert.AreEqual(logged[i].Shift, decisions[i].day);
                    Assert.IsNull(decisions[i].b);
                    CollectionAssert.Contains(ShelterEventData.Options(logged[i].Kind), decisions[i].a);
                    allKinds.Add(logged[i].Kind);
                }

                var record = RoundTrip(sim);
                Assert.AreEqual(GameRecord.CurrentVersion, record.version);
                Assert.AreEqual(17, record.version);
                Assert.AreEqual(sim.Record.commands.Count, record.commands.Count);
                Assert.IsTrue(record.balance.shelterEventsEnabled);
                Assert.AreEqual(sim.B.isolationFindChance, record.balance.isolationFindChance, "yeni denge alanları kayıtla taşınır");
                Assert.AreEqual(sim.B.eventGapMax, record.balance.eventGapMax);
                var saved = record.commands.Where(c => c.kind == "event_decide").ToList();
                CollectionAssert.AreEqual(decisions.Select(c => c.a + "@" + c.day).ToArray(), saved.Select(c => c.a + "@" + c.day).ToArray());
                Assert.IsTrue(saved.All(c => string.IsNullOrEmpty(c.b)), "JSON gidiş-dönüşünde null, boş dizgi olur");

                var restored = Replay.Restore(record);
                Assert.AreEqual(Signature(sim), Signature(restored), "tohum " + seed);
                Assert.AreEqual(Pending(sim), Pending(restored));

                // Kayıttan devam: vadeli (gizli) kayıtlar da yeniden kurulmuştur; iki oyun aynı komutlarla aynı yere varır
                Play(sim, new Random(7), 15);
                Play(restored, new Random(7), 15);
                Assert.AreEqual(Signature(sim), Signature(restored), "devam, tohum " + seed);
                Assert.AreEqual(sim.Record.commands.Count, restored.Record.commands.Count);
            }
            Assert.Greater(allKinds.Count, 5, "test düzeni: çeşitli olaylar oynandı (" + string.Join(",", allKinds) + ")");
        }

        /// <summary>
        /// Altmış tohum, rastgele politika: kayıttan yeniden kurulan oyun aynı duruma ve aynı sona varır. Aşama 8 dengesiyle
        /// (2026-11-14) büyük hırsızlık 1000–1049'da hiç açılmadı (yalnız 1052'de); aralık on dört olayın hepsini görsün diye 1060'a uzadı.
        /// </summary>
        [Test]
        public void Replay_CampaignWithEvents_SameStateAndEnding()
        {
            var endings = new Dictionary<string, int>();
            var kinds = new HashSet<string>();
            int decisions = 0;
            for (uint seed = 1000; seed < 1060; seed++)
            {
                var sim = Simulation.NewGame(seed, Campaign());
                // Tek tohumlar sert oynar (çok sivil kaybı): bazı oyunlar sonla biter
                if (seed % 2 == 0) Play(sim, new Random((int)seed), 45); else Play(sim, new Random((int)seed), 45, 0.35, 5);
                string ending = sim.S.Ending ?? "suruyor";
                endings.TryGetValue(ending, out int n);
                endings[ending] = n + 1;
                decisions += sim.Record.commands.Count(c => c.kind == "event_decide");
                foreach (var l in sim.ShelterEvents.Log) kinds.Add(l.Kind);
                var restored = Replay.Restore(RoundTrip(sim));
                Assert.AreEqual(sim.S.Ending, restored.S.Ending, "tohum " + seed);
                Assert.AreEqual(Signature(sim), Signature(restored), "tohum " + seed);
            }
            Assert.Greater(decisions, 300, "test düzeni: olaylar oynandı");
            CollectionAssert.IsSubsetOf(ShelterEventData.Kinds, kinds, "test düzeni: on dört olayın hepsi görüldü; görülen: " + string.Join(",", kinds));
            CollectionAssert.IsSubsetOf(new[] { "bulgu", "bulgu_yok", "ayrilma", "kapandi", "ifsa", "vadeli", "sokuldu" }, kinds);
            Assert.Greater(endings.Count, 1, "test düzeni: biten ve süren oyunlar var: " + string.Join(",", endings.Select(x => x.Key + " " + x.Value)));
        }

        /// <summary>Açık olay ve basılmış fiş ortasında alınan kayıt aynı yere döner.</summary>
        [Test]
        public void Replay_MidShift_RestoresThePendingEvent()
        {
            var b = Robots(Quiet(1));
            var sim = New(b);
            Admit(sim);
            for (int i = 0; i < 6 && sim.PendingShelterEvent == null; i++) { if (sim.Admission.Group.Count > 0) sim.PassCandidates(); Close(sim); }
            Assert.AreEqual("iz", Pending(sim));
            var restored = Replay.Restore(RoundTrip(sim));
            Assert.AreEqual(Signature(sim), Signature(restored));
            Assert.AreEqual("iz", Pending(restored));
            Assert.AreEqual(sim.PendingShelterEvent.PersonId, restored.PendingShelterEvent.PersonId);
            Assert.IsFalse(restored.PrintShift().Ok);
            foreach (var game in new[] { sim, restored })
            {
                Decide(game, "tecrit");
                if (game.Admission.Group.Count > 0) game.PassCandidates();
                Close(game); Close(game); Close(game);
            }
            Assert.AreEqual(Signature(sim), Signature(restored), "tecrit bulgusunun çekimi kayıttan devamda aynı");
            Assert.AreEqual(1, Log(restored, "bulgu").Count + Log(restored, "bulgu_yok").Count);
        }

        // ---------------------------------------------------------------- denge

        [Test]
        public void Balance_Clone_IsIndependent_AndNewFieldsAreFlat()
        {
            var fields = typeof(Balance).GetFields(BindingFlags.Instance | BindingFlags.Public);
            string[] added =
            {
                "shelterEventsEnabled", "eventGapMin", "eventGapMax", "isolationShifts", "isolationCapacity", "interventionShifts", "isolationFindChance",
                "isolationFindChanceTwo", "eventRestrictShifts", "eventRestrictLongShifts", "exposureIgnoredTrace", "exposureSabotage", "exposureInterrogation",
                "desertStreak", "eventDeserterStreak", "rationWarnShifts", "halfRationShifts", "halfRationRaidHp", "halfRationFactor", "theftSupplies",
                "bigTheftSupplies", "bigTheftChance", "bigTheftDelayMin", "bigTheftDelayMax", "reviveSupplies", "woundWorsenChance", "reviveHp", "feudShifts",
                "feudInjuryChance", "memorialSupplies", "griefChance", "memorialTrust", "blackoutAlarm", "blackoutAlarmMargin", "tradeMetal", "tradeCircuits",
                "tradeMetalSupplies", "tradeSupplies", "tradeLowSupplies", "tradeHighSupplies", "callSupplies", "callRefuseChance", "callMinSupplies", "callTrust",
                "convoySupplies", "convoyAlarm", "convoyTrustAt", "eventBigTheftMax", "eventBigTheftCooldown", "eventDeserterMax", "eventDeserterCooldown",
                "eventLowRationMax", "eventLowRationCooldown", "eventTheftMax", "eventTheftCooldown", "eventWoundedMax", "eventWoundedCooldown", "eventFeudMax",
                "eventFeudCooldown", "eventMemorialMax", "eventMemorialCooldown", "eventBlackoutMax", "eventBlackoutCooldown", "eventTradeMax", "eventTradeCooldown",
                "eventCallMax", "eventCallCooldown", "eventConvoyMax", "eventConvoyCooldown",
            };
            Assert.AreEqual(69, added.Length);
            foreach (string name in added) Assert.IsTrue(fields.Any(f => f.Name == name), name);

            var source = new Balance();
            Assert.IsTrue(source.shelterEventsEnabled, "C# varsayılanı: olaylar açık");
            var copy = source.Clone();
            Assert.AreNotSame(source, copy);
            foreach (var f in fields)
            {
                Assert.IsTrue(f.FieldType.IsValueType || f.FieldType == typeof(string), f.Name + ": denge alanları düz kalmalı (sığ kopya yeter)");
                object original = f.GetValue(source);
                Assert.AreEqual(original, f.GetValue(copy), f.Name);
                object changed;
                if (f.FieldType == typeof(int)) changed = (int)original + 1;
                else if (f.FieldType == typeof(double)) changed = (double)original + 1;
                else if (f.FieldType == typeof(bool)) changed = !(bool)original;
                else if (f.FieldType == typeof(string)) changed = original + "x";
                else continue;
                f.SetValue(copy, changed);
                Assert.AreEqual(original, f.GetValue(source), f.Name + ": kopya kaynağı etkiledi");
                Assert.AreNotEqual(original, f.GetValue(copy), f.Name);
            }

            // Kayıt, oyun kurulurken verilen dengenin kopyasını taşır
            var balance = Quiet();
            var sim = New(balance);
            balance.shelterEventsEnabled = false;
            balance.isolationShifts = 9;
            balance.eventConvoyMax = 7;
            Assert.IsTrue(sim.Record.balance.shelterEventsEnabled);
            Assert.AreEqual(3, sim.Record.balance.isolationShifts);
            Assert.AreEqual(0, sim.Record.balance.eventConvoyMax);

            // Varsayılanlar (tasarım belgesi §5)
            var d = new Balance();
            Assert.AreEqual(2, d.eventGapMin);
            Assert.AreEqual(3, d.eventGapMax, "aşama 8 dengesi: 3 kaldı (2 denendi, sunum kuralıyla olay/harekât bandı aşıyordu)");
            Assert.AreEqual(3, d.isolationShifts);
            Assert.AreEqual(1, d.isolationCapacity);
            Assert.AreEqual(0.60, d.isolationFindChance);
            Assert.AreEqual(0.85, d.isolationFindChanceTwo);
            Assert.AreEqual(3, d.desertStreak);
            Assert.AreEqual(4, d.halfRationShifts);
            Assert.AreEqual(0.5, d.halfRationFactor);
            Assert.AreEqual(17, GameRecord.CurrentVersion);
        }
    }
}
