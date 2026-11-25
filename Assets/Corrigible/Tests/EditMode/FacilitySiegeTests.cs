using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 11, parça P2 (Docs/TESIS-BASKINLARI-TASARIMI.md §9.2–9.3, §11.4; uygulama tasarımı §3, §5.3): bekleyen karşı saldırı
    /// (kuşatma) eşikte kurulur, karar vardiyasında baskın emri kilitlidir, <c>siege_defend</c> ve <c>siege_yield</c> komutları, karar
    /// verilmezse kapanışta kendiliğinden bırakma, üç odalı savunmanın sonucu (güven, kanat, kadro, deneyim, gövde, izler, günlük) ve
    /// kayıt (iki yeni komutla tam harekât yeniden oynanır). Savunma tahtasının kuralları <see cref="FacilityDefenseTests"/>'te.
    /// </summary>
    public class FacilitySiegeTests
    {
        static Simulation NewSim(uint seed = 79, Action<Balance> tune = null)
        {
            var b = Bal();
            tune?.Invoke(b);
            return ShelterEventKit.New(b, seed);
        }

        static Simulation Restore(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            var r = Replay.Restore(sim.Record);
            Assert.AreEqual(0, r.ReplayFailures);
            return r;
        }

        /// <summary>Süren tesis baskınını giriş odasında geri çekilerek kaybeder (komutlarla).</summary>
        static void LoseRaid(Simulation sim)
        {
            var B = sim.OpenRaidRoom();
            Calm(B);
            Assert.IsTrue(Baskin.Retreat(B));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(B)).Ok);
            var r = sim.ResolveRaid(sim.FacilityRaidResult());
            Assert.IsTrue(r.Ok, r.Msg);
            Assert.IsFalse(sim.S.RaidLog.Last().Success);
        }

        static string Decide => Loc.T("Önce karşı saldırıya karar ver: savun ya da bırak.");
        static string TooEarly => Loc.T("Saldırı bu vardiya kuruldu; sonraki vardiya savun ya da bırak.");

        static int ExperienceOf(Simulation sim, IEnumerable<string> ids) =>
            ids.Sum(id => sim.Board.Person(id).Experience.Sum(x => x.Points));

        // ---------- Kurulum ----------
        [Test]
        public void Threshold_AtClose_BeginsSiege_DecisionThisShift_NoFall()
        {
            var sim = NewSim();
            var w = CaptureNext(sim);
            int trust = sim.S.Trust, attacks = sim.S.Counterattacks;
            var e = ForceSiege(sim);
            Assert.AreEqual(w.Id, e.Sector);
            Assert.AreEqual(w.Region, e.Region);
            Assert.AreEqual(sim.S.Day, e.Day);
            Assert.AreEqual(sim.S.Day, e.Due, "kapanışta kurulan: karar vardiyası ertesi vardiya (artmış gün)");
            Assert.AreEqual("", e.Outcome);
            Assert.AreEqual(-1, e.Closed);
            Assert.AreEqual(0, sim.S.Alarm, "alarm sıfırlanır");
            Assert.IsTrue(w.Captured, "kanat henüz düşmez");
            Assert.AreEqual(trust, sim.S.Trust, "kurulumda güven değişmez");
            Assert.AreEqual(attacks, sim.S.Counterattacks, "sayaç kanat düşünce (K6)");
            Assert.IsTrue(sim.S.Events.Any(x => x.Type == SimEventType.Siege && x.Key == w.Id));
            Assert.IsFalse(sim.S.Events.Any(x => x.Type == SimEventType.Counterattack));
            Assert.IsTrue(sim.SiegeDecisionDue);
            Assert.AreSame(e, sim.ActiveSiege);
            Assert.AreSame(w, sim.SiegeSector);
            Assert.AreEqual(Decide, sim.LiberationRaidCheck(sim.NextLiberationSector(w.Region).Id), "karar vardiyasında baskın emri kilitli");
            Assert.IsTrue(sim.S.Moments.Last().Text.Contains(w.Name));
        }

        [Test]
        public void Threshold_NoCapturedWing_OnlyTrust()
        {
            var sim = NewSim();
            int trust = sim.S.Trust;
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.AlarmPerShift;
            ShelterEventKit.Close(sim);
            CollectionAssert.IsEmpty(sim.Facility.Sieges);
            Assert.AreEqual(1, sim.S.Counterattacks);
            Assert.IsNull(sim.S.LastCounterattack);
            Assert.AreEqual(trust - sim.B.liberationCounterattackTrust, sim.S.Trust);
            Assert.AreEqual(0, sim.S.Alarm);
        }

        /// <summary>
        /// Kapanış (inceleme bulgusu 5): kartsız kapının cevabı alarmı eşiğe getirince savunma açıkken ziyaret kuşatmayı bilir
        /// (<see cref="DoorVisit.Siege"/>), karşı saldırı ve sıfırlanan alarm etki diye yazılmaz; savunma kapalıyken eski hâl
        /// (<see cref="DoorVisit.Counterattack"/>).
        /// </summary>
        [Test]
        public void DoorAnswer_AtThreshold_VisitKnowsTheSiege([Values(true, false)] bool defense)
        {
            var sim = NewSim(tune: b => b.facilityDefenseEnabled = defense);
            var w = CaptureNext(sim);
            sim.S.Door ??= new DoorState { NextKnock = 999 };
            sim.S.Door.Visit = new DoorVisit { Day = sim.S.Day, Card = "teknisyen_casus" };
            Assert.Greater(DoorCards.Get("teknisyen_casus").Ignore.Alarm, 1, "kartın açılmama etkisi alarm");
            sim.S.Alarm = sim.B.liberationAlarmThreshold - 1;
            int sieges = sim.Facility.Sieges.Count, attacks = sim.S.Counterattacks;
            var r = sim.AnswerDoor(Simulation.DoorRefuse);
            Assert.IsTrue(r.Ok, r.Msg);
            var v = sim.S.Door.Log.Last();
            Assert.AreEqual(0, sim.S.Alarm, "alarm sıfırlandı");
            if (defense)
            {
                Assert.AreEqual(sieges + 1, sim.Facility.Sieges.Count, "kapı kuşatma kurdu");
                Assert.AreEqual(attacks, sim.S.Counterattacks);
                Assert.IsTrue(w.Captured, "kanat henüz düşmez");
                Assert.IsTrue(v.Siege, "ziyaret kuşatmayı bilir");
                Assert.IsFalse(v.Counterattack);
                Assert.AreEqual(0, v.Alarm, "sıfırlanan alarm etki değildir");
            }
            else
            {
                Assert.AreEqual(sieges, sim.Facility.Sieges.Count);
                Assert.AreEqual(attacks + 1, sim.S.Counterattacks);
                Assert.IsFalse(w.Captured, "anlık karşı saldırı kanadı düşürür");
                Assert.IsTrue(v.Counterattack);
                Assert.IsFalse(v.Siege);
            }
        }

        [Test]
        public void FailedRaid_MidShift_DecisionNextShift_ThenAutoYieldAtItsClose()
        {
            var sim = NewSim();
            var w = CaptureNext(sim);
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.B.liberationAlarmPerFailure;
            ShelterEventKit.FillSquad(sim);
            Assert.IsTrue(sim.StartLiberationRaid(sim.NextLiberationSector(w.Region).Id).Ok);
            LoseRaid(sim);
            var e = sim.PendingSiege;
            Assert.IsNotNull(e);
            Assert.AreEqual(sim.S.Day + 1, e.Due, "vardiya ortasında kurulan: bir sonraki vardiya");
            var reward = sim.S.RaidLog.Last().LiberationReward;
            Assert.AreEqual(w.Id, reward.Siege);
            Assert.IsNull(reward.Counterattack);
            Assert.IsFalse(sim.SiegeDecisionDue);
            Assert.AreEqual(TooEarly, sim.DefendSectorCheck());
            Assert.AreEqual(TooEarly, sim.YieldSectorCheck());
            Assert.AreNotEqual(Decide, sim.LiberationRaidCheck(sim.NextLiberationSector(w.Region).Id), "kurulduğu vardiya baskın serbest");
            int trust = sim.S.Trust, attacks = sim.S.Counterattacks;
            ShelterEventKit.Close(sim);
            Assert.AreEqual("", e.Outcome, "vardiya ortası kurulan saldırı o kapanışta düşmez");
            Assert.IsTrue(w.Captured);
            Assert.IsTrue(sim.SiegeDecisionDue);
            Assert.AreEqual(Decide, sim.LiberationRaidCheck(sim.NextLiberationSector(w.Region).Id));
            int day = sim.S.Day;
            ShelterEventKit.Close(sim);
            Assert.AreEqual("kendi", e.Outcome);
            Assert.AreEqual(day, e.Closed, "biten vardiyanın günü");
            Assert.IsFalse(w.Captured);
            Assert.AreEqual(attacks + 1, sim.S.Counterattacks);
            Assert.AreEqual(w.Id, sim.S.LastCounterattack);
            Assert.AreEqual(trust - sim.B.liberationCounterattackTrust, sim.S.Trust);
            Assert.IsNull(sim.PendingSiege);
            ShelterEventKit.FillSquad(sim);
            Assert.IsNull(sim.LiberationRaidCheck(w.Id), "kendiliğinden bırakma kilitlemez (K10)");
        }

        /// <summary>K11: karar verilmeyen kuşatma alarm adımından önce bırakılır; aynı kapanışın eşiği yeni kuşatmayı önceki kanada kurar.</summary>
        [Test]
        public void AutoYield_RunsBeforeStepAlarm()
        {
            var sim = NewSim();
            var a = CaptureNext(sim);
            var b2 = CaptureNext(sim);
            var e = ForceSiege(sim);
            Assert.AreEqual(b2.Id, e.Sector, "son alınan kanat");
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.AlarmPerShift;
            ShelterEventKit.Close(sim);
            Assert.AreEqual("kendi", e.Outcome);
            Assert.IsFalse(b2.Captured);
            Assert.AreEqual(2, sim.Facility.Sieges.Count, "bırakmadan sonra eşik yeni kuşatma kurar");
            Assert.AreEqual(a.Id, sim.PendingSiege.Sector);
        }

        /// <summary>K7: bekleyen kuşatma varken ikinci eşik: alarm sıfırlanır, kuşatma kurulmaz, güven −1.</summary>
        [Test]
        public void SecondThreshold_WhilePending_TrustOnly()
        {
            var sim = NewSim();
            var w = CaptureNext(sim);
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.B.liberationAlarmPerFailure;
            ShelterEventKit.FillSquad(sim);
            Assert.IsTrue(sim.StartLiberationRaid(sim.NextLiberationSector(w.Region).Id).Ok);
            LoseRaid(sim);
            Assert.IsNotNull(sim.PendingSiege);
            int trust = sim.S.Trust, attacks = sim.S.Counterattacks;
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.AlarmPerShift;
            ShelterEventKit.Close(sim);
            Assert.AreEqual(1, sim.Facility.Sieges.Count);
            Assert.AreEqual("", sim.PendingSiege.Outcome);
            Assert.AreEqual(attacks + 1, sim.S.Counterattacks);
            Assert.IsNull(sim.S.LastCounterattack);
            Assert.AreEqual(trust - sim.B.liberationCounterattackTrust, sim.S.Trust);
            Assert.AreEqual(0, sim.S.Alarm);
            Assert.IsTrue(w.Captured);
        }

        [Test]
        public void FlagOff_InstantCounterattack_AsStage10()
        {
            var sim = NewSim(tune: b => b.facilityDefenseEnabled = false);
            var w = CaptureNext(sim);
            int trust = sim.S.Trust;
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.AlarmPerShift;
            ShelterEventKit.Close(sim);
            Assert.IsFalse(sim.DefenseOn);
            CollectionAssert.IsEmpty(sim.Facility.Sieges);
            Assert.IsFalse(w.Captured, "anlık karşı saldırı");
            Assert.AreEqual(1, sim.S.Counterattacks);
            Assert.AreEqual(trust - 1, sim.S.Trust);
            Assert.IsNull(sim.PendingSiege);
            Assert.AreEqual(Loc.T("Kanat savunması kapalı."), sim.DefendSectorCheck());
            Assert.IsFalse(sim.YieldSector().Ok);
        }

        // ---------- Bırak ----------
        [Test]
        public void Yield_FallsWing_LocksThatWingThisShift_NoFundsNoRng()
        {
            var sim = NewSim();
            var w = CaptureNext(sim);
            var e = ForceSiege(sim);
            double fon = sim.S.Fon;
            uint rng = sim.S.Rng.State;
            int trust = sim.S.Trust, attacks = sim.S.Counterattacks, ledger = sim.S.TransactionLedger.Count;
            var r = sim.YieldSector();
            Assert.IsTrue(r.Ok, r.Msg);
            Assert.AreEqual(Loc.F("{0}: {1} bırakıldı.", sim.RegionName(w.Region), w.Name), r.Msg);
            Assert.AreEqual("birak", e.Outcome);
            Assert.AreEqual(sim.S.Day, e.Closed);
            Assert.IsFalse(w.Captured);
            Assert.AreEqual(attacks + 1, sim.S.Counterattacks);
            Assert.AreEqual(w.Id, sim.S.LastCounterattack);
            Assert.AreEqual(sim.S.Day, sim.S.LastCounterattackDay);
            Assert.AreEqual(trust - sim.B.liberationCounterattackTrust, sim.S.Trust);
            Assert.IsTrue(sim.S.Events.Any(x => x.Type == SimEventType.Counterattack && x.Key == w.Id));
            Assert.AreEqual(fon, sim.S.Fon);
            Assert.AreEqual(rng, sim.S.Rng.State);
            Assert.AreEqual(ledger, sim.S.TransactionLedger.Count, "defter satırı yok");
            Assert.AreEqual("siege_yield", sim.Record.commands.Last().kind);
            ShelterEventKit.FillSquad(sim);
            Assert.AreEqual(Loc.T("Bu vardiya bırakılan kanada baskın verilemez."), sim.LiberationRaidCheck(w.Id));
            Assert.IsFalse(sim.YieldSector().Ok, "ikinci karar yok");
            Assert.AreEqual(Loc.T("Bekleyen karşı saldırı yok."), sim.DefendSectorCheck());
            ShelterEventKit.Close(sim);
            ShelterEventKit.FillSquad(sim);
            Assert.IsNull(sim.LiberationRaidCheck(w.Id), "sonraki vardiya serbest");
        }

        /// <summary>Bırakılan kanat başka bölgedeyse bulunulan bölgenin baskın hakkı korunur (§9.2-2).</summary>
        [Test]
        public void Yield_OtherRegion_KeepsTheRaidRight()
        {
            var sim = NewSim();
            string region = CompleteRegion(sim);
            Assert.IsTrue(ChooseReward(sim));
            ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            var e = ForceSiege(sim);
            Assert.AreEqual(region, e.Region);
            Assert.IsTrue(sim.YieldSector().Ok);
            ShelterEventKit.FillSquad(sim);
            var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.AreNotEqual(region, next.Region);
            Assert.IsNull(sim.LiberationRaidCheck(next.Id));
            Assert.IsTrue(sim.StartLiberationRaid(next.Id).Ok);
        }

        // ---------- Savun ----------
        [Test]
        public void Defend_NoFunds_NoRng_NoAttempt_SeedFromTheLog_LocksTheBoard()
        {
            var sim = NewSim();
            var w = CaptureNext(sim);
            var e = ForceSiege(sim);
            ShelterEventKit.FillSquad(sim);
            double fon = sim.S.Fon;
            uint rng = sim.S.Rng.State;
            int raids = sim.S.Raids, attempts = w.Attempts, ledger = sim.S.TransactionLedger.Count;
            var r = sim.DefendSector();
            Assert.IsTrue(r.Ok, r.Msg);
            Assert.AreEqual(fon, sim.S.Fon, "fon istenmez");
            Assert.AreEqual(rng, sim.S.Rng.State, "S.Rng çekilmez");
            Assert.AreEqual(attempts, w.Attempts);
            Assert.AreEqual(raids + 1, sim.S.Raids);
            Assert.AreEqual(ledger, sim.S.TransactionLedger.Count);
            var p = sim.S.Raid.Params;
            Assert.AreEqual("defense", p.Mode);
            Assert.IsTrue(Simulation.IsDefense(p));
            Assert.AreEqual(w.Id, p.SectorId);
            Assert.AreEqual(w.MissionKind, p.MissionKind);
            CollectionAssert.AreEqual(Enumerable.Range(1, 3).Select(i => FacilityMaps.RoomId(w.Id, i)), p.Rooms);
            Assert.AreEqual((int)(Rng.Mix(sim.S.Seed, 0x53494547u) & 0x7FFFFFFF), p.Seed, "tuz: günlüğün dizini (0)");
            Assert.AreNotEqual(Rng.Mix(sim.S.Seed, 0x53494547u), Rng.Mix(sim.S.Seed, 0x53494548u), "dizin değişince tohum değişir");
            Assert.AreEqual(0, p.TargetHpBonus);
            Assert.AreEqual(0, p.ExtraMachines);
            Assert.IsFalse(p.IsCore);
            Assert.AreEqual(sim.S.Raids, sim.S.Raid.N);
            Assert.AreEqual(w.Id, sim.S.Liberation.ActiveSector);
            Assert.IsTrue(sim.Board.Locked);
            Assert.AreEqual("savun", e.Outcome);
            Assert.IsNull(sim.PendingSiege);
            Assert.AreSame(e, sim.ActiveSiege);
            Assert.AreEqual("siege_defend", sim.Record.commands.Last().kind);
            Assert.AreEqual("baskin", sim.S.Teams[0].Task);
            Assert.AreEqual(Loc.T("Zaten bir baskın sürüyor."), sim.DefendSectorCheck());
            Assert.IsTrue(Baskin.IsDefense(sim.OpenRaidRoom()));
        }

        [Test]
        public void Defend_Checks()
        {
            var sim = NewSim();
            Assert.AreEqual(Loc.T("Bekleyen karşı saldırı yok."), sim.DefendSectorCheck());
            Assert.AreEqual(Loc.T("Bekleyen karşı saldırı yok."), sim.YieldSectorCheck());
            CaptureNext(sim);
            ForceSiege(sim);
            ShelterEventKit.FillSquad(sim);
            Assert.IsNull(sim.DefendSectorCheck());
            Assert.IsNull(sim.YieldSectorCheck());
            // Bekleyen sunucu ödülü savunmayı da kilitler (K9; bırakma serbest)
            sim.Facility.PendingReward = "weu";
            Assert.AreEqual(Loc.T("Önce sunucu ödülünü seç: tarif ya da kazanım."), sim.DefendSectorCheck());
            Assert.IsNull(sim.YieldSectorCheck());
            sim.Facility.PendingReward = "";
            // Kit eksik: pano denetimi (baskınla aynı)
            var tek = sim.Board.People.First(p => p.Job == "tek");
            sim.Board.Equipment.RemoveAll(x => x.OwnerId == tek.Id && x.Kind == "kit_tek");
            Assert.IsNotNull(sim.PlanningRaidCheck());
            Assert.AreEqual(sim.PlanningRaidCheck(), sim.DefendSectorCheck());
            Assert.IsFalse(sim.DefendSector().Ok);
            Assert.AreEqual("", sim.PendingSiege.Outcome);
        }

        // ---------- Savunmanın sonucu ----------
        /// <summary>Güven: kazanma −1 × terk (0/−1/−2), kaybetme −1 −1 × terk (−1/−2/−3); kaybetmek hiçbir durumda az götürmez. Alarm ve fon
        /// değişmez; kazanınca kanat alınmış kalır, kaybedince düşer.</summary>
        [Test]
        public void Defense_TrustTable_HeldAndLost()
        {
            var deltas = new Dictionary<(bool, int), int>();
            foreach (bool hold in new[] { true, false })
                for (int left = 0; left <= 2; left++)
                {
                    var sim = NewSim();
                    var w = CaptureNext(sim);
                    var e = ForceSiege(sim);
                    Defend(sim);
                    int trust = sim.S.Trust, attacks = sim.S.Counterattacks;
                    double alarm = sim.S.Alarm, fon = sim.S.Fon;
                    PlayDefense(sim, left, hold);
                    string at = (hold ? "tut" : "kayıp") + " terk " + left;
                    var log = sim.S.RaidLog.Last();
                    var rw = log.LiberationReward;
                    Assert.AreEqual(hold, log.Success, at);
                    Assert.AreEqual("defense", log.Mode, at);
                    Assert.AreEqual(w.Id, log.SectorId, at);
                    Assert.AreEqual(left + 1, log.Rooms.Count, at);
                    Assert.IsTrue(rw.Defense, at);
                    Assert.AreEqual(hold, rw.Held, at);
                    Assert.AreEqual(left, rw.Abandoned, at);
                    Assert.AreEqual(hold ? "tutuldu" : "dustu", e.Outcome, at);
                    Assert.AreEqual(sim.S.Day, e.Closed, at);
                    Assert.AreEqual(left, e.Abandoned, at);
                    Assert.AreEqual(hold, w.Captured, at);
                    Assert.AreEqual(attacks + (hold ? 0 : 1), sim.S.Counterattacks, at);
                    Assert.AreEqual(hold ? null : w.Id, rw.Counterattack, at);
                    Assert.AreEqual(alarm, sim.S.Alarm, at + ": alarm değişmez");
                    Assert.AreEqual(fon, sim.S.Fon, at + ": fon yok");
                    Assert.AreEqual(sim.S.Trust - trust, rw.Trust, at);
                    deltas[(hold, left)] = sim.S.Trust - trust;
                    Assert.IsNull(sim.ActiveSiege, at);
                    Assert.IsFalse(rw.RegionCompleted, at);
                    Assert.IsNull(rw.ServerReward, at);
                    Assert.IsNull(rw.Countermeasure, at);
                    Assert.AreEqual(hold ? Loc.F("{0}: {1} tutuldu.", sim.RegionName(w.Region), w.Name)
                        : Loc.F("{0}: {1} yeniden işgal altında.", sim.RegionName(w.Region), w.Name), sim.S.Moments.Last().Text, at);
                }
            for (int left = 0; left <= 2; left++)
            {
                Assert.AreEqual(-left, deltas[(true, left)], "kazanma, terk " + left);
                Assert.AreEqual(-1 - left, deltas[(false, left)], "kaybetme, terk " + left);
                Assert.Less(deltas[(false, left)], deltas[(true, left)]);
            }
        }

        /// <summary>Deneyim yalnız kazanılan savunmada (K23); kayıpta ayakta dönen de deneyim almaz.</summary>
        [Test]
        public void Defense_Experience_OnlyWhenHeld()
        {
            foreach (bool hold in new[] { true, false })
            {
                var sim = NewSim();
                CaptureNext(sim);
                ForceSiege(sim);
                Defend(sim);
                var ids = sim.S.Raid.Params.Squad.Select(x => x.PersonId).ToList();
                int before = ExperienceOf(sim, ids);
                PlayDefense(sim, 0, hold);
                if (hold) Assert.Greater(ExperienceOf(sim, ids), before, "kazanınca deneyim");
                else Assert.AreEqual(before, ExperienceOf(sim, ids), "kayıpta deneyim yok");
                Assert.IsTrue(sim.S.RaidLog.Last().LiberationReward.Squad.All(x => !x.Dead), "ayakta dönenler yaşar");
            }
        }

        /// <summary>Gövde (K22): kazanınca ekibin odasında yatan çıkarılır ve yatar; kayıpta ölür.</summary>
        [Test]
        public void Defense_Fallen_ExtractedWhenHeld_DeadWhenLost()
        {
            foreach (bool hold in new[] { true, false })
            {
                var sim = NewSim();
                CaptureNext(sim);
                ForceSiege(sim);
                Defend(sim);
                string id = sim.S.Raid.Params.Squad[1].PersonId;
                PlayDefense(sim, 0, hold, B => Down(B, BySlot(B, 1)));
                var person = sim.Board.Person(id);
                var outcome = sim.S.RaidLog.Last().LiberationReward.Squad[1];
                Assert.IsTrue(outcome.Down);
                if (hold)
                {
                    Assert.IsTrue(person.Alive, "kazanınca ekibin odasında yatan çıkarılır");
                    Assert.IsFalse(outcome.Dead);
                    Assert.GreaterOrEqual(person.Down, 1);
                    Assert.AreEqual("rest", person.Job);
                }
                else
                {
                    Assert.IsFalse(person.Alive, "kayıpta yatan ölür");
                    Assert.IsTrue(outcome.Dead);
                }
            }
        }

        /// <summary>Terk edilen odada yatan ölür, kazanılsa da (K22).</summary>
        [Test]
        public void Defense_FallenInAbandonedRoom_Dies()
        {
            var sim = NewSim();
            CaptureNext(sim);
            ForceSiege(sim);
            Defend(sim);
            string id = sim.S.Raid.Params.Squad[2].PersonId;
            PlayDefense(sim, 1, true, B => { if (B.Room.Index == 1) Down(B, BySlot(B, 2)); });
            Assert.IsTrue(sim.S.RaidLog.Last().Success);
            Assert.IsFalse(sim.Board.Person(id).Alive);
        }

        // ---------- Kayıt ----------
        /// <summary>
        /// İki yeni komutla tam harekât: alınan kanat, kapanışta kuşatma, savunma (bir oda terk, tutuldu), sonraki kuşatmada bırakma, yeniden
        /// alma ve yeni kuşatma kendiliğinden bırakma. Kayıttan kurulan oyunun durumu (izler, kuşatma günlüğü, sayaçlar, sektörler, kişiler)
        /// canlıyla aynı; başarısız komut yok. Eşik düşük (her iki vardiyada bir kuşatma), alarm yalnız komutlarla değişir.
        /// </summary>
        [Test]
        public void Replay_FullCampaignWithBothCommands_SameState()
        {
            var sim = NewSim(tune: b => b.liberationAlarmThreshold = 2);
            var w = CaptureNext(sim);
            for (int i = 0; i < 4 && sim.PendingSiege == null; i++) ShelterEventKit.Close(sim);
            Assert.IsNotNull(sim.PendingSiege);
            if (!sim.SiegeDecisionDue) ShelterEventKit.Close(sim);
            Defend(sim);
            PlayDefense(sim, 1, true);
            Assert.IsTrue(sim.S.RaidLog.Last().Success);
            for (int i = 0; i < 4 && sim.PendingSiege == null; i++) ShelterEventKit.Close(sim);
            if (!sim.SiegeDecisionDue) ShelterEventKit.Close(sim);
            Assert.IsTrue(sim.YieldSector().Ok);
            ShelterEventKit.Close(sim);
            Assert.IsTrue(TryStartRaid(sim));
            PlayRaid(sim);
            for (int i = 0; i < 6 && sim.S.Ending == null; i++) ShelterEventKit.Close(sim);
            var log = sim.Facility.Sieges.Select(e => e.Outcome).ToList();
            CollectionAssert.Contains(log, "tutuldu");
            CollectionAssert.Contains(log, "birak");
            CollectionAssert.Contains(log, "kendi");
            Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "siege_defend"));
            Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "siege_yield"));
            var r = Restore(sim);
            Assert.AreEqual(State(sim), State(r));
            Assert.AreEqual("f10", r.Record.facility);
            Assert.IsNull(r.S.Raid);
        }

        /// <summary>Yeni komutlar Replay.Apply'da tanınır: geçersiz durumda verilen siege_defend/siege_yield başarısız sayılır (bilinmeyen
        /// tür gibi sessiz geçmez).</summary>
        [Test]
        public void Replay_SiegeCommands_AreKnownKinds()
        {
            var sim = NewSim();
            sim.Record.commands.Add(new SavedCommand { day = 0, kind = "siege_defend" });
            sim.Record.commands.Add(new SavedCommand { day = 0, kind = "siege_yield" });
            var r = Replay.Restore(sim.Record);
            Assert.AreEqual(2, r.ReplayFailures);
        }

        /// <summary>Savunmanın ortasında kayıttan devam: aynı oda, aynı tahta.</summary>
        [Test]
        public void Replay_MidDefense_SameBoard()
        {
            // Kuşatma eşik yoluyla (ForceSiege alarmı doğrudan yazar, kayıttan kurulmaz)
            var live = NewSim(tune: b => b.liberationAlarmThreshold = 2);
            CaptureNext(live);
            for (int i = 0; i < 4 && !live.SiegeDecisionDue; i++) ShelterEventKit.Close(live);
            Defend(live);
            var G = live.OpenRaidRoom();
            Calm(G);
            ToGate(G);
            Assert.IsTrue(Baskin.Pass(G));
            Assert.IsTrue(live.AdvanceRaidRoom(live.RaidRoomReport(G)).Ok);
            var r = Restore(live);
            Assert.AreEqual(Baskin.Signature(live.OpenRaidRoom()), Baskin.Signature(r.OpenRaidRoom()));
            Assert.AreEqual(State(live), State(r));
        }
    }
}
