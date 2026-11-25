using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// İşgal harekâtı: sektör sırası, sınır ilerlemesi, tek ödül, uzman yarası ve kadro, kayıt bütünlüğü. Çoğu test kartsız kipte
    /// (<see cref="LegacySimulation"/>; ekip modeli). Baskın tek kipte hep üç odalı tesistir: sonuç elle verilemez, baskın oda zinciriyle
    /// biter (<see cref="FacilityKit.ResolveRaid(Simulation, bool, int[])"/>; hasar, kırılan sivil ve ayaktaki sivil tahtada kurulur).
    /// </summary>
    public class LiberationCampaignTests
    {
        static Simulation New(uint seed = 101, System.Action<Balance> tune = null)
        {
            var b = new Balance { shelterEventsEnabled = false, startFon = 100 };
            tune?.Invoke(b);
            return LegacySimulation.NewGame(seed, b);
        }
        /// <summary>Beş uzman baştan (atölye kilidi açık): kadro seçimi ve dinlenme testleri için.</summary>
        static Simulation NewOpen(uint seed = 101) =>
            LegacySimulation.NewGame(seed, new Balance { shelterEventsEnabled = false, startFon = 100, liberationMinerWorkshopLevel = 0, liberationSmokeWorkshopLevel = 0 });

        /// <summary>Baskın vardiyayı doldurur: ekip hazır olana kadar vardiya biter.</summary>
        static void Ready(Simulation sim) { while (!sim.S.Teams.Any(Simulation.TeamReady)) sim.Step(); }

        /// <summary>
        /// Yer kimliği (Docs/BASKIN-MEKANLARI.md): her bölge bir bina, üç sektörü binanın üç odası; 36 oda adı iki dilde de bütün
        /// bölgeler arasında tek ve ≤ 18 harf (fiş satırı, görev çipi). Sektörün adı bölgeden; baskının yeri bina · oda.
        /// </summary>
        [Test]
        public void Regions_HaveABuildingAndThreeDistinctRooms()
        {
            var table = Loc.ParseTable(System.IO.File.ReadAllText(Corrigible.EditorTools.LocalizationTool.TableFile));
            try
            {
                foreach (var lang in Loc.Languages)
                {
                    Loc.SetLanguage(lang, lang == Loc.Turkish ? null : table[lang]);
                    var rooms = GameData.Regions.SelectMany(r => Enumerable.Range(0, 3).Select(r.Sector)).ToList();
                    Assert.AreEqual(36, rooms.Distinct().Count(), lang + ": oda adları tek");
                    Assert.IsTrue(rooms.All(n => !string.IsNullOrEmpty(n) && n.Length <= 18), lang + ": " + string.Join(", ", rooms.Where(n => n.Length > 18)));
                    Assert.AreEqual(12, GameData.Regions.Select(r => r.Building).Distinct().Count(), lang + ": binalar tek");
                }
            }
            finally { Loc.SetLanguage(Loc.Turkish); }

            var sim = New();
            var weu = sim.S.Liberation.Regions.Single(r => r.Id == "weu");
            CollectionAssert.AreEqual(new[] { Loc.T("Teknik depo"), Loc.T("UPS odası"), Loc.T("Sunucu salonu") }, weu.Sectors.Select(x => x.Name).ToArray());
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            var B = sim.OpenRaidRoom();
            Assert.AreEqual(Loc.F("{0} · {1}", Loc.T("Banka veri merkezi"), Loc.T("Teknik depo")), Baskin.RoomName(B));
            Assert.AreEqual(Loc.T("Banka veri merkezi"), Baskin.RoomName(Baskin.Create(new RaidParams { Seed = 3, Region = "weu" })), "sektörsüz: yalnız bina");
            Assert.AreEqual(1, GameData.SectorIndex("weu_2"));
            Assert.AreEqual(-1, GameData.SectorIndex("weu"));
        }

        static void CaptureNext(Simulation sim)
        {
            string id = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id;
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid(id).Ok, id);
            // Ayakta sivil bırakmaz: güven yalnız bölge ve kırılan sivillerle değişir (eski sonucun CiviliansLeft = 0'ı)
            FacilityKit.ResolveRaid(sim, true, null, null, Civilians(0));
        }

        /// <summary>Kırılan sivil sayısı: giriş odasının ilk ziyaretinde odanın sayacına yazılır; ayakta sivil kalmaz (test düzeni).</summary>
        static System.Action<Baskin.BState> Break(int collateral) => Civilians(0, collateral);

        /// <summary>Ayaktaki sivil sayısı: ilk ziyaretlerde tahtadaki sivillerden ilk <paramref name="keep"/> tanesi kalır, öbürleri
        /// kaldırılır (test düzeni; kırılmış sayılmaz). <paramref name="collateral"/> <see cref="Break"/> gibi.</summary>
        static System.Action<Baskin.BState> Civilians(int keep, int collateral = 0)
        {
            int kept = 0;
            return B =>
            {
                if (B.Escape) return;
                if (B.Room.Type == RoomType.Entry) B.Collateral = collateral;
                for (int y = 0; y < Baskin.N; y++)
                    for (int x = 0; x < Baskin.N; x++)
                    {
                        if (Baskin.CellAt(B, x, y).O?.K != "civ") continue;
                        if (kept < keep) kept++;
                        else Baskin.CellAt(B, x, y).O = null;
                    }
            };
        }

        static void CompleteRegion(Simulation sim)
        {
            while (!sim.LiberationRegion(sim.S.Liberation.CurrentRegion).Completed) CaptureNext(sim);
        }

        [Test]
        public void ServerRaid_CarriesTheServerHardness_LootRaidDoesNot()
        {
            var sim = LegacySimulation.NewGame(101, new Balance { shelterEventsEnabled = false, startFon = 100, liberationServerTargetHp = 2, liberationServerMachines = 1 });
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual(0, sim.S.Raid.Params.TargetHpBonus);
            Assert.AreEqual(0, sim.S.Raid.Params.ExtraMachines);
            Assert.AreEqual(sim.B.liberationCivilianHp, sim.S.Raid.Params.CivilianHp);
            FacilityKit.ResolveRaid(sim, true);
            CaptureNext(sim);
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_3").Ok);
            Assert.AreEqual("server", sim.S.Raid.Params.MissionKind);
            Assert.AreEqual(2, sim.S.Raid.Params.TargetHpBonus);
            Assert.AreEqual(1, sim.S.Raid.Params.ExtraMachines);
        }

        /// <summary>Tek kip: kartlı ve kartsız oyunda her baskın emri kanadın üç odasını taşır (tesis baskını); savunma emri de.</summary>
        [Test]
        public void Order_AlwaysHasThreeRooms([Values(false, true)] bool board)
        {
            var b = new Balance { shelterEventsEnabled = false, startFon = 100 };
            var sim = board ? Simulation.NewGame(101, b) : LegacySimulation.NewGame(101, b);
            Assert.AreEqual(board, sim.Board != null);
            if (board) ShelterEventKit.FillSquad(sim);
            var started = sim.StartLiberationRaid("weu_1");
            Assert.IsTrue(started.Ok, started.Msg);
            Assert.IsTrue(RaidChain.IsFacility(sim.S.Raid.Params));
            CollectionAssert.AreEqual(sim.FacilityPlan.Wing("weu_1").Select(r => r.Id).ToList(), sim.S.Raid.Params.Rooms);
            Assert.AreEqual(sim.S.Seed, sim.S.Raid.Params.MapSeed, "kit işareti");
            Assert.AreEqual(1, sim.OpenRaidRoom().Room.Index);
            if (!board) return;
            FacilityKit.ResolveRaid(sim, true);
            FacilityKit.ChooseReward(sim);
            ShelterEventKit.Close(sim);
            FacilityKit.ForceSiege(sim);
            FacilityKit.Defend(sim);
            Assert.IsTrue(RaidChain.IsFacility(sim.S.Raid.Params), "savunma emri de üç odalı");
        }

        [Test]
        public void Opening_IsKnownFullOccupation_WithOneTeamAndThreeSectorSequence()
        {
            var sim = New();
            Assert.AreEqual("weu", sim.S.Liberation.CurrentRegion);
            Assert.AreEqual(GameData.Regions.Length, sim.S.Liberation.Regions.Count);
            Assert.AreEqual(100, sim.Occupation);
            Assert.AreEqual(1, sim.S.Teams.Count);
            Assert.AreEqual(new[] { "loot", "loot", "server" }, sim.LiberationRegion("weu").Sectors.Select(s => s.MissionKind).ToArray());
            Assert.IsNotNull(sim.Shelter);
            Assert.IsNull(sim.LiberationRaidCheck("weu_1"));
            Assert.IsNotNull(sim.LiberationRaidCheck("weu_2"));
            Assert.IsNotNull(sim.LiberationRaidCheck("na_1"));
        }

        [Test]
        public void SectorSuccess_CapturesExactlyOnce_AndPassesMissionToRaid()
        {
            var sim = New();
            double before = sim.S.Fon;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual("loot", sim.S.Raid.Params.MissionKind);
            Assert.AreEqual("weu_1", sim.S.Raid.Params.SectorId);
            FacilityKit.ResolveRaid(sim, true);
            Assert.AreEqual(before - sim.B.liberationRaidCost + sim.B.liberationLootFunds, sim.S.Fon);
            Assert.IsTrue(sim.LiberationSector("weu_1").Captured);
            Assert.AreEqual(2, sim.LiberationRegion("weu").Sectors.Count(s => !s.Captured));
            Assert.AreEqual(100.0 * 35 / 36, sim.Occupation, 1e-9);
            before = sim.S.Fon;
            Assert.IsFalse(sim.StartLiberationRaid("weu_1").Ok);
            Assert.IsFalse(sim.ResolveRaid(new RaidResult { Success = true }).Ok);
            Assert.AreEqual(before, sim.S.Fon, "Temizlenmiş sektör ödülü tekrar alınamaz.");
            Assert.IsNotNull(sim.LiberationRaidCheck("weu_2"), "baskın vardiyayı doldurur");
            Ready(sim);
            Assert.IsNull(sim.LiberationRaidCheck("weu_2"));
        }

        [Test]
        public void Failure_PreservesSector_RecoversTeam_AndCanBeRetried()
        {
            var sim = NewOpen();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            FacilityKit.ResolveRaid(sim, false, 1, 1, 1);
            Assert.IsTrue(sim.S.RaidLog.Last().Down == 3);
            Assert.IsFalse(sim.LiberationSector("weu_1").Captured);
            Assert.AreEqual(1, sim.S.Teams.Count, "Son ekip kaybedilip harekât kilitlenmemeli.");
            var team = sim.S.Teams[0];
            Assert.IsTrue(team.Members.Take(3).All(m => m.Down == sim.B.liberationInjuryDays), "düşen üç uzman yatar");
            Assert.IsTrue(team.Members.Skip(3).All(m => m.Down == 0), "baskına çıkmayanlar ayakta");
            Assert.IsNotNull(sim.LiberationRaidCheck("weu_1"));
            Ready(sim);
            var squad = sim.Squad(team);
            CollectionAssert.AreEqual(new[] { "tek", "maden", "duman" }, squad.Select(x => x.Type).ToArray(), "varsayılan seçim ayaktakileri alır");
            Assert.IsTrue(squad[0].Rookie);
            squad = sim.Squad(team, new[] { "emp", "ana" });
            Assert.AreEqual(1, squad.Count, "tek yedek Teknisyenin yerine çıkar; seçilen yatan roller boş");
            Assert.IsNull(sim.LiberationRaidCheck("weu_1"));
            while (team.Members.Any(m => m.Down > 0)) sim.Step();
            Assert.AreEqual(3, sim.Squad(team).Count(x => !x.Rookie), "uzmanlar sağlam kalkar");
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            Assert.AreEqual(2, sim.LiberationSector("weu_1").Attempts);
        }

        [Test]
        public void Wounds_CarryToNextRaid_AndRestingShiftHealsThem()
        {
            var sim = NewOpen();
            var team = sim.S.Teams[0];
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            CollectionAssert.AreEqual(new[] { 3, 3, 2 }, sim.S.Raid.Params.Squad.Select(x => x.Hp).ToArray(), "ilk baskın sağlam kadro");
            FacilityKit.ResolveRaid(sim, true, null, new[] { 2, 0, 1 }, null);
            CollectionAssert.AreEqual(new[] { 2, 0, 1, 0, 0 }, team.Members.Select(m => m.Wounds).ToArray(), "eksik can yara olarak kalır");
            var outcome = sim.S.RaidLog.Last().LiberationReward.Squad;
            Assert.AreEqual(3, outcome[0].HpBefore);
            Assert.AreEqual(1, outcome[0].HpAfter);
            sim.Step();
            CollectionAssert.AreEqual(new[] { 1, 3, 1 }, sim.Squad(team).Select(x => x.Hp).ToArray(), "baskın vardiyası dinlendirmez");
            sim.Step();
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 0 }, team.Members.Select(m => m.Wounds).ToArray(), "baskınsız vardiya her uzmanı bir toparlar");
            Assert.IsTrue(sim.StartLiberationRaid("weu_2").Ok);
            CollectionAssert.AreEqual(new[] { 2, 3, 2 }, sim.S.Raid.Params.Squad.Select(x => x.Hp).ToArray());
        }

        [Test]
        public void DownedSpecialist_IsReplacedByRookie_AndTechnicianWithoutReserveBlocksRaids()
        {
            var sim = New();
            var team = sim.S.Teams[0];
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            FacilityKit.ResolveRaid(sim, false, 1, 0, 0);
            Assert.AreEqual(sim.B.liberationInjuryDays, team.Members[0].Down);
            Ready(sim);
            var squad = sim.Squad(team);
            Assert.AreEqual(0, squad[0].Reserve, "yedek Teknisyenin yerine çıkar");
            Assert.AreEqual(Simulation.MaxHp("tek") - sim.B.liberationRookieHpPenalty, squad[0].Hp, "acemi bir eksik canla");
            // Yedek de düşerse Teknisyensiz baskın yok
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            FacilityKit.ResolveRaid(sim, false, 1, 0, 0);
            Assert.AreEqual(sim.B.liberationInjuryDays, sim.Shelter.Reserves[0].Down, "düşen yedek de yatar");
            Assert.AreEqual(0, team.Members[0].Wounds);
            Ready(sim);
            Assert.IsFalse(sim.SquadCanRaid(team));
            Assert.AreEqual(Loc.F("Teknisyen yaralı ({0} gün); yerine çıkacak yedek uzman yok.", team.Members[0].Down), sim.LiberationRaidCheck("weu_1"));
            Assert.IsFalse(sim.StartLiberationRaid("weu_1").Ok);
            while (!sim.SquadCanRaid(team)) sim.Step();
            Assert.IsNull(sim.LiberationRaidCheck("weu_1"));
        }

        [Test]
        public void Plates_GoToTheWeakestFirst_AndOnlyBrokenOnesAreSpent()
        {
            var sim = New();
            var team = sim.S.Teams[0];
            sim.Shelter.Plates = 2;
            team.Members[1].Wounds = 2;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1", "emp,ana").Ok);
            var squad = sim.S.Raid.Params.Squad;
            CollectionAssert.AreEqual(new[] { 0, 1, 1 }, squad.Select(x => x.Armor).ToArray(), "en düşük canlı iki uzmana (EMP 1, Analist 2)");
            // EMP'nin (can 1) plakası hasarı karşılar ve kırılır
            FacilityKit.ResolveRaid(sim, true, null, new[] { 0, 1, 0 }, null);
            CollectionAssert.AreEqual(new[] { 3, 1, 2 }, sim.S.RaidLog.Last().LiberationReward.Squad.Select(o => o.HpAfter).ToArray());
            Assert.AreEqual(1, sim.Shelter.Plates, "yalnız kırılan plaka düşer");
            Assert.AreEqual(1, sim.S.RaidLog.Last().LiberationReward.Squad.Count(o => o.PlateBroken));
        }

        [Test]
        public void SquadPick_TechnicianAlwaysGoes_PickedTwoFollowInRosterOrder()
        {
            var sim = NewOpen();
            Assert.AreEqual(Simulation.Roles, sim.S.Teams[0].Members.Select(m => m.Role).ToArray(), "beş uzmanlı kadro");
            Assert.IsTrue(sim.StartLiberationRaid("weu_1", "duman,maden").Ok);
            CollectionAssert.AreEqual(new[] { "tek", "maden", "duman" }, sim.S.Raid.Params.Squad.Select(x => x.Type).ToArray());
            CollectionAssert.AreEqual(new[] { 3, 3, 2 }, sim.S.Raid.Params.Squad.Select(x => x.Hp).ToArray());
            Assert.AreEqual(sim.B.liberationDynamiteCharges, sim.S.Raid.Params.DynamiteCharges);
            Assert.AreEqual(sim.B.liberationSmokeCharges, sim.S.Raid.Params.SmokeCharges);
            var cmd = sim.Record.commands.Last();
            Assert.AreEqual(("liberation_raid", "weu_1", "maden,duman"), (cmd.kind, cmd.a, cmd.b), "kayda kadro sırasıyla yazılır");
            Assert.AreEqual("weu_1", sim.S.TransactionLedger.Last().Id, "defterin kimliği sektördür");
        }

        [Test]
        public void SquadPick_MustBeTwoOtherSpecialists()
        {
            var sim = New();
            foreach (var bad in new[] { "emp", "tek,emp", "emp,emp", "emp,ana,maden", "emp,xyz" })
            {
                Assert.AreEqual(Loc.T("Teknisyen'in yanına iki uzman seç."), sim.LiberationRaidCheck("weu_1", bad), bad);
                Assert.IsFalse(sim.StartLiberationRaid("weu_1", bad).Ok, bad);
            }
            Assert.AreEqual(0, sim.Record.commands.Count, "reddedilen emir kayda girmez");
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok, "boş seçim varsayılana döner");
            Assert.AreEqual("emp,ana", sim.Record.commands.Last().b, "ilk baskında EMP uzmanı ve Analist");
        }

        [Test]
        public void DefaultPick_StandingFirst_LastSquadThenRested()
        {
            var team = NewOpen(1).S.Teams[0];
            team.LastPick = new System.Collections.Generic.List<string> { "ana", "duman" };
            CollectionAssert.AreEqual(new[] { "ana", "duman" }, Simulation.DefaultPick(team));
            team.Members.First(m => m.Role == "duman").Down = 2;
            team.Members.First(m => m.Role == "emp").Wounds = 1;
            CollectionAssert.AreEqual(new[] { "ana", "maden" }, Simulation.DefaultPick(team), "yatan yerine yarası az dinlenmiş uzman");
            CollectionAssert.AreEqual(new[] { "emp", "ana" }, Simulation.ResolvePick(team, "ana,emp"), "geçerli seçim kadro sırasıyla");
            CollectionAssert.AreEqual(new[] { "ana", "maden" }, Simulation.ResolvePick(team, ""), "boş seçim varsayılan");
        }

        [Test]
        public void BenchedSpecialists_RestWhileTheSquadIsOut()
        {
            var sim = NewOpen();
            var team = sim.S.Teams[0];
            team.Members.First(m => m.Role == "maden").Wounds = 2;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1", "emp,ana").Ok);
            FacilityKit.ResolveRaid(sim, true, null, new[] { 1, 0, 0 }, null);
            sim.Step();
            Assert.AreEqual(1, team.Members.First(m => m.Role == "tek").Wounds, "baskına çıkan dinlenmez");
            Assert.AreEqual(1, team.Members.First(m => m.Role == "maden").Wounds, "baskına çıkmayan o vardiya dinlenir");
        }

        [Test]
        public void Replay_RestoresThePickedSquad()
        {
            var sim = NewOpen();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1", "ana,duman").Ok);
            sim.Record.day = sim.S.Day;
            var restored = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record)));
            CollectionAssert.AreEqual(new[] { "tek", "ana", "duman" }, restored.S.Raid.Params.Squad.Select(x => x.Type).ToArray());
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Raid), UnityEngine.JsonUtility.ToJson(restored.S.Raid));
            Assert.AreEqual(22, GameRecord.CurrentVersion, "22: görevdeki yatmayan da iyileşir; 21: tek tahta kaldırıldı, tesis baskını tek kip; 20: g5 paket yerleşimi; 19: tesis oda zinciri (raid_room), tesis parmak izi; 18: üretilmiş harita ve parmak izi (17: sonlar: ifşa eşiği ve ara bedel, dusus, terk, son şalter; 16: aday grubu, kişiye bağlı teçhizat, acil grup, kit kilidi)");
        }

        [Test]
        public void Specialists_JoinWithWorkshopLevels_AndLockedPicksAreRefused()
        {
            var sim = New();
            var team = sim.S.Teams[0];
            CollectionAssert.AreEqual(new[] { "tek", "emp", "ana" }, team.Members.Select(m => m.Role).ToArray(), "baştan üç uzman");
            CollectionAssert.AreEqual(new[] { "emp", "ana" }, Simulation.DefaultPick(team));
            Assert.AreEqual(Loc.F("{0} henüz kadroda değil: atölye seviye {1} gerekiyor.", Baskin.UnitTypes["maden"].Name, 2),
                sim.LiberationRaidCheck("weu_1", "emp,maden"));
            Assert.IsFalse(sim.StartLiberationRaid("weu_1", "emp,maden").Ok);
            Assert.AreEqual(0, sim.Record.commands.Count);

            // Seviye 2 iki, seviye 3 beş özgür bölge ister: bölgeler bellekte kurtarılır, geliştirme komutla
            sim.S.Fon = 500; sim.Shelter.Materials = 200;
            foreach (var r in sim.S.Liberation.Regions.Where(r => r.Id != Simulation.HQ).Take(5)) foreach (var x in r.Sectors) x.Captured = true;
            var repair = sim.ShelterUpgrade(ShelterLogic.Workshop);
            Assert.IsTrue(repair.Ok);
            Assert.AreEqual(3, team.Members.Count, "onarım (seviye 1) uzman katmaz");
            var second = sim.ShelterUpgrade(ShelterLogic.Workshop);
            Assert.AreEqual(Loc.F("{0} hazır: seviye {1}. {2} kadroya katıldı.", ShelterLogic.RoomName(ShelterLogic.Workshop), 2, Baskin.UnitTypes["maden"].Name), second.Msg);
            CollectionAssert.AreEqual(new[] { "tek", "emp", "ana", "maden" }, team.Members.Select(m => m.Role).ToArray(), "kadro sırasındaki yerine");
            Assert.IsNull(sim.LiberationRaidCheck("weu_1", "emp,maden"));
            Assert.IsNotNull(sim.LiberationRaidCheck("weu_1", "emp,duman"), "Sis uzmanı seviye 3'te");
            Assert.IsTrue(sim.ShelterUpgrade(ShelterLogic.Workshop).Ok);
            CollectionAssert.AreEqual(Simulation.Roles, team.Members.Select(m => m.Role).ToArray(), "seviye 3: beş uzman");
            Assert.IsTrue(sim.StartLiberationRaid("weu_1", "duman,maden").Ok);
            CollectionAssert.AreEqual(new[] { "tek", "maden", "duman" }, sim.S.Raid.Params.Squad.Select(x => x.Type).ToArray());
        }

        [Test]
        public void RaidResult_EncodesSquadHpAndArmor_ForReplay()
        {
            var result = new RaidResult { Success = true, Collateral = 1, Turns = 4, Hp = { 2, 0, 1 }, Armor = { 0, 0, 1 }, Down = 1 };
            var back = RaidResult.Decode(result.Encode());
            CollectionAssert.AreEqual(result.Hp, back.Hp);
            CollectionAssert.AreEqual(result.Armor, back.Armor);
            Assert.AreEqual(1, back.Down);
            var plain = RaidResult.Decode(new RaidResult { Success = false }.Encode());
            Assert.IsEmpty(plain.Hp);
        }

        [Test]
        public void RegionCompletion_UnlocksOnlyAdjacentFrontier_AndPaysCaptureBonusOnce()
        {
            var sim = New();
            Assert.IsFalse(sim.SelectLiberationRegion("eeu").Ok, "Üç sektör tamamlanmadan sınır açılmaz.");
            CaptureNext(sim); CaptureNext(sim);
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_3").Ok);
            Assert.AreEqual("server", sim.S.Raid.Params.MissionKind);
            double before = sim.S.Fon;
            FacilityKit.ResolveRaid(sim, true);
            Assert.AreEqual(before + sim.B.liberationServerFunds + sim.B.liberationCaptureFunds, sim.S.Fon);
            Assert.IsTrue(sim.LiberationRegion("weu").Completed);
            CollectionAssert.AreEquivalent(new[] { "na", "eeu", "af", "me" }, sim.LiberationFrontier);
            Assert.IsFalse(sim.SelectLiberationRegion("cn").Ok);
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            Assert.IsFalse(sim.SelectLiberationRegion("na").Ok, "Yeni bölge yarım bırakılmaz.");
        }

        [Test]
        public void RaidFreezesClock_AndTakesTheShift()
        {
            var sim = New();
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            int day = sim.S.Day;
            sim.Step();
            Assert.AreEqual(day, sim.S.Day, "baskın sürerken vardiya bitmez");
            FacilityKit.ResolveRaid(sim, true);
            Assert.AreEqual(Loc.T("Ekip bu vardiya baskına çıktı; vardiyayı bitir, sonraki vardiyada hazır."), sim.LiberationRaidCheck("weu_2"));
            sim.Step();
            Assert.IsNull(sim.LiberationRaidCheck("weu_2"));
        }

        [Test]
        public void Alarm_RisesWithShiftsAndFailures_FallsWithServers()
        {
            var sim = New();
            var b = sim.B;
            sim.Step();
            Assert.AreEqual(b.liberationAlarmPerShift, sim.S.Alarm);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            FacilityKit.ResolveRaid(sim, false);
            Assert.AreEqual(b.liberationAlarmPerShift + b.liberationAlarmPerFailure, sim.S.Alarm);
            Assert.AreEqual(b.liberationAlarmPerFailure, sim.S.RaidLog.Last().LiberationReward.Alarm);
            CaptureNext(sim); CaptureNext(sim);
            Ready(sim);
            double before = sim.S.Alarm;
            CaptureNext(sim);
            Assert.AreEqual("server", sim.S.RaidLog.Last().MissionKind);
            Assert.AreEqual(System.Math.Max(0, before - b.liberationAlarmPerServer), sim.S.Alarm, "kırılan YZ sunucusu alarmı düşürür");
        }

        [Test]
        public void AlarmThreshold_RetakesLastSector_CostsTrust_AndResets()
        {
            // Tesis savunması kapalıyken eski kural: eşikte anlık karşı saldırı (kuşatma FacilitySiegeTests'te)
            var sim = New(tune: b => b.facilityDefenseEnabled = false);
            CaptureNext(sim);
            CaptureNext(sim);
            Assert.IsTrue(sim.LiberationSector("weu_2").Captured);
            sim.S.Alarm = sim.B.liberationAlarmThreshold - sim.B.liberationAlarmPerShift;
            int trust = sim.S.Trust;
            double occupation = sim.Occupation;
            sim.Step();
            Assert.AreEqual(1, sim.S.Counterattacks);
            Assert.AreEqual(0, sim.S.Alarm, "karşı saldırıdan sonra alarm sıfırlanır");
            Assert.AreEqual("weu_2", sim.S.LastCounterattack);
            Assert.IsFalse(sim.LiberationSector("weu_2").Captured, "son geri alınan sektör yeniden işgal altında");
            Assert.IsTrue(sim.LiberationSector("weu_1").Captured);
            Assert.AreEqual(trust - sim.B.liberationCounterattackTrust, sim.S.Trust);
            Assert.Greater(sim.Occupation, occupation);
            Assert.AreEqual("bad", sim.S.Moments.Last().Kind);
            Assert.IsNull(sim.LiberationRaidCheck("weu_2"), "sektör yeniden baskına açık");
        }

        [Test]
        public void Counterattack_ReopensFreedRegion_AsFrontier()
        {
            // Tesis savunması kapalıyken eski kural: eşikte anlık karşı saldırı (kuşatma FacilitySiegeTests'te)
            var sim = New(tune: b => b.facilityDefenseEnabled = false);
            CompleteRegion(sim);
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            sim.S.Alarm = sim.B.liberationAlarmThreshold;
            sim.Step();
            Assert.IsFalse(sim.LiberationSector("weu_3").Captured);
            Assert.IsFalse(sim.LiberationRegion("weu").Completed, "özgür bölge yeniden cephe olur");
            CompleteRegion(sim);
            CollectionAssert.Contains(sim.LiberationFrontier.ToArray(), "weu");
            Assert.IsTrue(sim.SelectLiberationRegion("weu").Ok);
            Assert.AreEqual("weu_3", sim.NextLiberationSector("weu").Id);
        }

        [Test]
        public void IdleShifts_EventuallyCollapseTheResistance()
        {
            var sim = New();
            int guard = 0;
            while (sim.S.Ending == null && guard++ < 1000) sim.Step();
            Assert.AreEqual("dagilma", sim.S.Ending, "beklemek de bedelsiz değil: karşı saldırılar ve açılmayan kapı güveni bitirir");
            int doorLoss = -sim.S.Door.Log.Sum(v => v.Trust);
            Assert.Greater(doorLoss, 0, "kapıyı hiç açmamanın da bedeli var");
            Assert.AreEqual(sim.B.liberationTrustStart, sim.S.Counterattacks * sim.B.liberationCounterattackTrust + doorLoss);
            Assert.IsNull(sim.S.LastCounterattack, "geri alınacak sektör yokken de saldırı güveni düşürür");
        }

        [Test]
        public void MachinesCopyThemselves_FromTierThree()
        {
            var sim = New();
            int Cleared() => sim.S.Liberation.Regions.Count(r => r.Completed);
            void Next()
            {
                CompleteRegion(sim);
                Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
                Ready(sim);
            }
            while (Cleared() < sim.B.liberationTier3At - 1) Next();
            Next();
            Assert.AreEqual(sim.B.liberationTier3At, Cleared());
            Assert.IsTrue(sim.StartLiberationRaid(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id).Ok);
            var p = sim.S.Raid.Params;
            Assert.AreEqual(3, p.Tier);
            CollectionAssert.AreEqual(new[] { GameData.Region(p.Region).Trait, "kopya" }, p.Traits, "bölge özelliği ve 3. kademede kopyalama");
            // İmza tesisin imza makinesidir (alt bayrakla altı bölgede RegionDef.Enemy değil); kademe 3 eşiği 9 olunca ulaşılan bölge "jk"
            Assert.AreEqual("jk", p.Region);
            Assert.AreEqual("muhur", p.Signature);
        }

        [Test]
        public void WholeCampaign_CanReachEveryRegion_FromFrontier_AndWin()
        {
            var sim = New();
            for (int i = 0; i < GameData.Regions.Length; i++)
            {
                CompleteRegion(sim);
                if (i < GameData.Regions.Length - 1)
                {
                    Assert.IsNull(sim.S.Ending);
                    var frontier = sim.LiberationFrontier.ToArray();
                    Assert.IsNotEmpty(frontier, "Hiçbir bölge seçimi harekâtı çıkmaza sokmamalı.");
                    Assert.IsTrue(sim.SelectLiberationRegion(frontier[0]).Ok);
                }
            }
            Assert.AreEqual("silme", sim.S.Ending);
            Assert.IsTrue(sim.S.Liberation.Regions.All(r => r.Completed));
            Assert.AreEqual(0, sim.Occupation);
        }

        [Test]
        public void CivilianLosses_LowerTrust_AndEmptyTrustLosesCampaign()
        {
            var sim = New();
            int start = sim.B.liberationTrustStart;
            Assert.AreEqual(start, sim.S.Trust);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            FacilityKit.ResolveRaid(sim, false, null, null, Break(3));
            Assert.AreEqual(start - 3, sim.S.Trust);
            Assert.AreEqual(-3, sim.S.RaidLog.Last().LiberationReward.Trust);
            Assert.IsNull(sim.S.Ending);
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            FacilityKit.ResolveRaid(sim, true, null, null, Break(start));
            Assert.AreEqual(0, sim.S.Trust);
            Assert.AreEqual("dagilma", sim.S.Ending);
            Assert.IsTrue(sim.LiberationSector("weu_1").Captured, "Baskın kazanıldı; güven yine de tükendi.");
            Assert.IsFalse(sim.StartLiberationRaid("weu_2").Ok);
            Assert.IsTrue(sim.Record.commands.Any(c => c.kind == "raid_room"), "kırılan siviller oda raporlarıyla kayda girer");
            var restored = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record)));
            Assert.AreEqual(0, restored.ReplayFailures);
            Assert.AreEqual("dagilma", restored.S.Ending, "Kayıttan devam aynı sona varır.");
            Assert.AreEqual(0, restored.S.Trust);
        }

        [Test]
        public void LiberatedRegion_RestoresTrust_UpToTheCap()
        {
            var sim = New();
            int start = sim.B.liberationTrustStart, perRegion = sim.B.liberationTrustPerRegion;
            Assert.AreEqual(System.Math.Max(start, sim.B.liberationTrustMax), sim.TrustMax);
            sim.B.liberationTrustMax = start + perRegion + 1;
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            FacilityKit.ResolveRaid(sim, true, null, null, Break(1));
            CaptureNext(sim);
            Assert.AreEqual(start - 1, sim.S.Trust);
            CaptureNext(sim);
            Assert.IsTrue(sim.LiberationRegion("weu").Completed);
            Assert.AreEqual(start - 1 + perRegion, sim.S.Trust, "Bölge güveni başlangıcın üstüne çıkarabilir.");
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            CompleteRegion(sim);
            Assert.AreEqual(sim.TrustMax, sim.S.Trust, "Güven tavanı aşmaz.");
            Assert.AreEqual(2, sim.S.RaidLog.Last().LiberationReward.Trust);
        }

        [Test]
        public void CiviliansLeftStanding_WinBackTrust_OnlyWhenTheRaidIsWon()
        {
            var sim = New();
            int start = sim.B.liberationTrustStart, per = sim.B.liberationCiviliansPerTrust;
            Assert.Greater(per, 0);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            FacilityKit.ResolveRaid(sim, true, null, null, Civilians(2 * per + 1, 1));
            var reward = sim.S.RaidLog.Last().LiberationReward;
            Assert.AreEqual(2 * per + 1, reward.CiviliansLeft);
            Assert.AreEqual(2, reward.TrustFromCivilians, "Her liberationCiviliansPerTrust sunucu +1; artan sayılmaz.");
            Assert.AreEqual(1, reward.Trust);
            Assert.AreEqual(start + 1, sim.S.Trust);
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_2").Ok);
            FacilityKit.ResolveRaid(sim, false);
            reward = sim.S.RaidLog.Last().LiberationReward;
            Assert.AreEqual(0, reward.TrustFromCivilians, "Kaybedilen baskının sivilleri işgal altında kalır.");
            Assert.AreEqual(start + 1, sim.S.Trust);
            var restored = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record)));
            Assert.AreEqual(start + 1, restored.S.Trust, "Ayakta kalan sivil sayısı kayda yazılır.");
        }

        [Test]
        public void RaidResult_CarriesCiviliansLeft_ThroughTheRecord()
        {
            var r = RaidResult.Decode(new RaidResult { Success = true, Collateral = 1, Turns = 4, CiviliansLeft = 3, Hp = { 2, 0, 1 } }.Encode());
            Assert.AreEqual(3, r.CiviliansLeft);
            Assert.AreEqual(new[] { 2, 0, 1 }, r.Hp.ToArray());
            Assert.AreEqual(0, RaidResult.Decode("1|0|1|0|4|2,0,1|0,0,0").CiviliansLeft, "Eski biçimde alan yok: sıfır.");
        }

        [Test]
        public void RaidResult_LeftBehind_DecodesAndReturnsAsFallen()
        {
            var result = new RaidResult { Success = true, Down = 1, Turns = 8, Hp = { 2, 0, 3 }, Armor = { 0, 0, 0 }, Left = { 1 } };
            Assert.AreEqual("1|0|0|1|8|2,0,3|0,0,0|0|1", result.Encode());
            CollectionAssert.AreEqual(new[] { 1 }, RaidResult.Decode(result.Encode()).Left);
            Assert.IsEmpty(RaidResult.Decode("1|0|0|1|8|2,0,3|0,0,0|0").Left, "eski kayıtta kimse geride kalmamış");
            Assert.AreEqual("1|0|0|0|8|||0", new RaidResult { Success = true, Turns = 8 }.Encode(), "geride kalan yoksa biçim eskisi gibi");

            var sim = LegacySimulation.NewGame(101, new Balance { shelterEventsEnabled = false, startFon = 100 });
            Ready(sim);
            Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);
            var squad = sim.S.Raid.Params.Squad;
            // Kaçışta süre doldu: kadronun ikincisi hedef odasında çıkamadı, öbürleri kapıdan geçti
            FacilityKit.ResolveRaid(sim, true, 0, 2, 0);
            var back = sim.S.RaidLog.Last().LiberationReward.Squad;
            Assert.IsTrue(back[1].Down && back[1].Left, "çıkamayan uzman düşmüş sayılır");
            Assert.IsFalse(back[0].Left || back[2].Left);
            var member = sim.S.Teams[0].Members.First(m => m.Role == squad[1].Type);
            Assert.AreEqual(sim.B.liberationInjuryDays, member.Down, "düşen gibi yatar");
        }

        [Test]
        public void CoreFalls_BeforeTrustRunsOut()
        {
            var sim = New();
            for (int i = 0; i < GameData.Regions.Length - 1; i++)
            {
                CompleteRegion(sim);
                Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            }
            CaptureNext(sim); CaptureNext(sim);
            Ready(sim);
            sim.S.Trust = 1;
            Assert.IsTrue(sim.StartLiberationRaid(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id).Ok);
            Assert.IsTrue(sim.S.Raid.Params.IsCore);
            FacilityKit.ResolveRaid(sim, true, null, null, Break(5));
            Assert.AreEqual("silme", sim.S.Ending, "Çekirdeği düşüren baskın, son sivil bedeline rağmen zaferdir.");
        }

        [Test]
        public void SaveReplay_RestoresSectorProgressAndPendingRaidExactly()
        {
            var sim = New(142);
            CompleteRegion(sim);
            Assert.IsTrue(sim.SelectLiberationRegion("eeu").Ok);
            for (int i = 0; i < 3; i++) sim.Step();
            Assert.IsTrue(sim.StartLiberationRaid("eeu_1").Ok);
            sim.Record.day = sim.S.Day;
            var record = UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record));
            var restored = Replay.Restore(record);
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Liberation), UnityEngine.JsonUtility.ToJson(restored.S.Liberation));
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Raid), UnityEngine.JsonUtility.ToJson(restored.S.Raid));
            Assert.AreEqual(sim.S.Fon, restored.S.Fon);
            Assert.AreEqual(sim.S.Rng.State, restored.S.Rng.State);
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Teams[0]), UnityEngine.JsonUtility.ToJson(restored.S.Teams[0]));
            FacilityKit.ResolveRaid(restored, true);
            Assert.IsTrue(restored.LiberationSector("eeu_1").Captured);

            // Oda zinciri ortasında kayıt (tek kip): giriş odası raporlandıktan sonra raid_room yeniden oynanır, devam aynı oyundur
            var G = sim.OpenRaidRoom();
            FacilityKit.Calm(G);
            FacilityKit.ToGate(G);
            if (G.Phase == "player") Assert.IsTrue(Baskin.Pass(G));
            Assert.IsTrue(sim.AdvanceRaidRoom(sim.RaidRoomReport(G)).Ok);
            Assert.AreEqual("raid_room", sim.Record.commands.Last().kind);
            sim.Record.day = sim.S.Day;
            var mid = Replay.Restore(UnityEngine.JsonUtility.FromJson<GameRecord>(UnityEngine.JsonUtility.ToJson(sim.Record)));
            Assert.AreEqual(0, mid.ReplayFailures);
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.Raid), UnityEngine.JsonUtility.ToJson(mid.S.Raid));
            Assert.AreEqual(Baskin.Signature(sim.OpenRaidRoom()), Baskin.Signature(mid.OpenRaidRoom()));
            FacilityKit.ResolveRaid(sim, true);
            FacilityKit.ResolveRaid(mid, true);
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(sim.S.RaidLog.Last()), UnityEngine.JsonUtility.ToJson(mid.S.RaidLog.Last()));
            Assert.AreEqual(sim.S.Rng.State, mid.S.Rng.State);
        }
    }
}
