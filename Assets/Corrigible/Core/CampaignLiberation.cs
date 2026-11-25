using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Bir sektör: bölgenin binasında bir oda; iki ikmal (yağma) ve bir sunucu (yıkım) odası, sırayla geri alınır.</summary>
    [Serializable]
    public sealed class LiberationSectorState
    {
        public string Id, Region, MissionKind;
        public int Index, Attempts, CapturedDay = -1;
        public bool Captured;
        /// <summary>Binadaki oda (<see cref="RegionDef.Sector"/>); kayda yazılmaz, bölgeden okunur.</summary>
        public string Name => GameData.Region(Region).Sector(Index);
    }

    [Serializable]
    public sealed class LiberationRegionState
    {
        public string Id;
        public List<LiberationSectorState> Sectors = new List<LiberationSectorState>();
        /// <summary>Tesis aşama 8: bölgenin sunucu ödülü verildi (bölge ilk kez tamamlandığında; karşı saldırıyla düşüp yeniden alınan
        /// bölgede ikinci ödül yok). Bölge fonu ve güveni buna bağlı değildir (aşama 11).</summary>
        public bool Rewarded;
        public int CapturedCount => Sectors.Count(s => s.Captured);
        public bool Completed => Sectors.Count > 0 && Sectors.All(s => s.Captured);
    }

    [Serializable]
    public sealed class LiberationCampaignState
    {
        public string CurrentRegion, ActiveSector;
        public List<LiberationRegionState> Regions = new List<LiberationRegionState>();
        public ShelterState Shelter;
        /// <summary>Tesis baskınlarının durumu (bina planı bilinen odalar, dinleme puanı, kazanımlar, önlemler, izler, kuşatmalar).</summary>
        public FacilityState Facility = new FacilityState();
    }

    /// <summary>Depo ve nüfus sınırları uygulandıktan sonra bu baskından gerçekten kazanılan kaynaklar.</summary>
    [Serializable]
    public sealed class LiberationRaidReward
    {
        public double Fon, Supplies, Materials;
        public int Residents;
        public bool RegionCompleted;
        /// <summary>Sivil güvenin bu baskındaki net değişimi (kırılan ve ayakta kalan sunucular, özgürleşen bölge, karşı saldırı).</summary>
        public int Trust;
        /// <summary>Kazanılan baskında ayakta kalan sivil sunucular ve geri kazandırdıkları güven (tavandan önce).</summary>
        public int CiviliansLeft, TrustFromCivilians;
        /// <summary>YZ alarmının bu baskındaki değişimi (karşı saldırının sıfırlaması hariç) ve karşı saldırıda geri alınan sektör.</summary>
        public double Alarm;
        public string Counterattack;
        /// <summary>Kadronun baskından dönüşü (kadro sırasıyla).</summary>
        public List<SquadOutcome> Squad = new List<SquadOutcome>();
        /// <summary>Tesis baskını (kartlı oyun): kazanılan ikmal baskınında büyük sandık açıldı, ek malzeme verildi.</summary>
        public bool BigCrate;
        /// <summary>Tesis aşama 8: bu baskınla tamamlanan bölgenin seçim bekleyen sunucu ödülü (bölge kimliği; yoksa null). Sunum için;
        /// kayda girmez.</summary>
        public string ServerReward;
        /// <summary>Tesis aşama 10: bu baskınla tamamlanan bölgede YZ'nin öğrendiği önlem ("sınıf:kademe", ör. "itme:1"); yoksa null.
        /// Sunum için; kayda girmez.</summary>
        public string Countermeasure;
        /// <summary>Tesis aşama 11 (sunum ve bot; kayda girmez): sonuç bir savunmanın mı, kanat tutuldu mu, terk edilen oda sayısı; bu
        /// sonucun kurduğu kuşatmanın kanadı (başarısız baskın eşiğe vardırdıysa; yoksa null); izlerin defteri (yeni, silinen, terkle
        /// silinen); yeniden tamamlanan bölge (fon ve bölge güveni ikinci kez verilmedi, belge §9.3).</summary>
        public bool Defense, Held;
        public int Abandoned;
        public string Siege;
        public int MarksNew, MarksGone, MarksWiped;
        public bool RegionRepeat;
    }

    /// <summary>Bir uzmanın baskın sonucu: çıktığı ve döndüğü can, düştü mü (Left: kaçışta çıkamadı, düşmüş sayıldı), zırh
    /// plakası kırıldı mı.</summary>
    [Serializable]
    public sealed class SquadOutcome
    {
        public string Type;
        public string PersonId, PersonName;
        public bool Dead;
        public bool Rookie, Down, Left, PlateBroken, Turned, Subdued;
        public int HpBefore, HpAfter;
        /// <summary>Tesis aşama 8 (sunum; kayda girmez, oda raporlarından türer): kumanda kelepçesi bu baskında kullanıldı ve panodan
        /// silindi; köpeği baskında düştü ve dinleniyor.</summary>
        public bool ClampSpent, DogDown;
        /// <summary>Tesis aşama 12 (sunum; kayda girmez, sonuçtan türer): Katar'da açık kenardan düştü; yaşar,
        /// <see cref="Balance.facilityFallInjuryTurns"/> vardiya yatar (<see cref="Down"/> da true).</summary>
        public bool Overboard;
        public FacilityTitle NewTitles;
    }

    public sealed partial class Simulation
    {
        public LiberationRegionState LiberationRegion(string id) => S.Liberation?.Regions.FirstOrDefault(r => r.Id == id);
        public LiberationSectorState LiberationSector(string id) => S.Liberation?.Regions.SelectMany(r => r.Sectors).FirstOrDefault(s => s.Id == id);
        /// <summary>Sivil güvenin tavanı (sayaçlar ve rapor bunu gösterir).</summary>
        public int TrustMax => Math.Max(B.liberationTrustStart, B.liberationTrustMax);

        /// <summary>
        /// Tamamlanmış bölgelerin sınırı (<see cref="Links"/>: üretilmiş harita ya da dünya haritası), <c>GameData.Regions</c>
        /// (paket) sırasıyla. Çıkmazda kalan bir bölge tüm harekâtı kilitlemez.
        /// </summary>
        public IEnumerable<string> LiberationFrontier
        {
            get
            {
                var cleared = new HashSet<string>(S.Liberation.Regions.Where(r => r.Completed).Select(r => r.Id));
                var links = Links;
                return GameData.Regions.Where(r => !cleared.Contains(r.Id) && links.Any(l =>
                    l.A == r.Id && cleared.Contains(l.B) || l.B == r.Id && cleared.Contains(l.A))).Select(r => r.Id);
            }
        }

        public LiberationSectorState NextLiberationSector(string regionId) => LiberationRegion(regionId)?.Sectors.FirstOrDefault(s => !s.Captured);

        void SetupLiberation()
        {
            S.Liberation = new LiberationCampaignState { CurrentRegion = HQ, Shelter = ShelterLogic.Create(B) };
            S.Trust = B.liberationTrustStart;
            S.Liberation.Shelter.MinTrust = S.Trust;
            foreach (var def in GameData.Regions)
            {
                var area = new LiberationRegionState { Id = def.Id };
                for (int i = 0; i < 3; i++) area.Sectors.Add(new LiberationSectorState
                {
                    Id = def.Id + "_" + (i + 1), Region = def.Id, Index = i, MissionKind = i < 2 ? "loot" : "server",
                });
                S.Liberation.Regions.Add(area);
            }
            // Tek başlangıç ekibi sığınakta; atölyeyle açılan uzmanlar sonra katılır
            S.Teams.Add(NewTeam(0));
            SyncSpecialists();
            SetupDoor();
        }

        public string SelectLiberationRegionCheck(string regionId)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (S.Raid != null) return Loc.T("Önce süren baskını bitir.");
            if (LiberationRegion(regionId) == null) return Loc.T("Geçersiz bölge.");
            if (!LiberationRegion(S.Liberation.CurrentRegion).Completed) return Loc.T("Önce mevcut bölgenin bütün sektörlerini geri al.");
            if (!LiberationFrontier.Contains(regionId)) return Loc.T("Yeni hedef, geri alınmış bir bölgeye komşu olmalı.");
            return null;
        }

        public ActResult SelectLiberationRegion(string regionId) => RunCommand(() =>
        {
            var why = SelectLiberationRegionCheck(regionId);
            if (why != null) return ActResult.Fail(why);
            S.Liberation.CurrentRegion = regionId;
            foreach (var t in S.Teams.Where(TeamReady)) t.At = regionId;
            // Plan defteri (tesis aşama 8): yeni cephenin ilk kanadının giriş odası bilinir; dinleme puanından önce (puan boşa gitmesin)
            if (Facility != null && Facility.Perks.Contains(FacilityGear.PlanBook))
            {
                var first = NextLiberationSector(regionId);
                if (first != null) LearnFacilityRoom(FacilityMaps.RoomId(first.Id, 1));
            }
            // Dinleme nöbetinin biriken puanı yeni bölgenin ilk bilinmeyen odasını hemen açar (çekimsiz)
            SpendFacilityIntel();
            return ActResult.Success();
        }, "liberation_region", regionId);

        /// <param name="pick">Teknisyen'in yanına seçilen roller ("emp,maden"); boşsa varsayılan seçim.</param>
        public string LiberationRaidCheck(string sectorId, string pick = null)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (S.Raid != null) return Loc.T("Zaten bir baskın sürüyor.");
            // Tesis aşama 11 (§9.2): karar vardiyasında baskın emri kilitli; kurulduğu vardiya serbest
            if (SiegeDecisionDue) return Loc.T("Önce karşı saldırıya karar ver: savun ya da bırak.");
            var sector = LiberationSector(sectorId);
            if (sector == null) return Loc.T("Bu bölgede açık sektör yok.");
            // Komutla bırakılan kanada o vardiya baskın yok (kendiliğinden bırakma kilitlemez; K10)
            if (YieldedToday(sectorId)) return Loc.T("Bu vardiya bırakılan kanada baskın verilemez.");
            if (sector.Captured) return Loc.T("Bu sektör zaten geri alındı.");
            if (sector.Region != S.Liberation.CurrentRegion) return Loc.T("Yalnızca mevcut harekât bölgesine baskın yapılabilir.");
            if (NextLiberationSector(sector.Region)?.Id != sectorId) return Loc.T("Önce önceki sektörü geri al.");
            var team = S.Teams.FirstOrDefault(TeamReady);
            if (team == null) return Loc.T("Ekip bu vardiya baskına çıktı; vardiyayı bitir, sonraki vardiyada hazır.");
            if (Board != null)
            {
                // Tesis aşama 8: sunucu ödülü sonraki baskın emrinden önce seçilir (vardiya ve pano kilitlenmez)
                if (!string.IsNullOrEmpty(Facility?.PendingReward)) return Loc.T("Önce sunucu ödülünü seç: tarif ya da kazanım.");
                if (S.Fon < B.liberationRaidCost) return Loc.F("Bu baskın {0} fon ister.", B.liberationRaidCost);
                return PlanningRaidCheck();
            }
            if (!SquadCanRaid(team))
                return Loc.F("Teknisyen yaralı ({0} gün); yerine çıkacak yedek uzman yok.", team.Members.First(m => m.Role == "tek").Down);
            if (S.Fon < B.liberationRaidCost) return Loc.F("Bu baskın {0} fon ister.", B.liberationRaidCost);
            if (!string.IsNullOrEmpty(pick) && !ValidPick(ParsePick(pick))) return Loc.T("Teknisyen'in yanına iki uzman seç.");
            foreach (var role in ParsePick(pick))
                if (!HasRole(team, role))
                    return Loc.F("{0} henüz kadroda değil: atölye seviye {1} gerekiyor.", Baskin.UnitTypes[role].Name, UnlockLevel(role, B));
            return null;
        }

        /// <summary>Baskın emri: kayda çözülmüş seçim yazılır (boş seçim o anki varsayılana döner); defterin kimliği sektördür.</summary>
        public ActResult StartLiberationRaid(string sectorId, string pick = null)
        {
            if (string.IsNullOrEmpty(pick) || ValidPick(ParsePick(pick))) pick = PickKey(ResolvePick(S.Teams.FirstOrDefault(TeamReady), pick));
            return RunCommand(() => StartLiberationRaidCore(sectorId, pick), "liberation_raid", sectorId, pick, sectorId);
        }

        /// <summary>
        /// Baskın emrinin çıkaracağı kadro ve roller, emirden önce (görev fişinin kadro raporu): <see cref="StartLiberationRaid"/> ile
        /// aynı seçim çözümü ve plaka dağılımı. Durumu değiştirmez, RNG çekmez; hazır ekip yoksa boş kadro.
        /// </summary>
        public RaidParams LiberationSquadPreview(string pick = null)
        {
            var team = S.Teams.FirstOrDefault(TeamReady);
            if (team == null) return new RaidParams();
            var roles = ResolvePick(team, pick);
            var squad = Squad(team, roles);
            if (Board == null) FitPlates(squad, Shelter?.Plates ?? 0);
            return new RaidParams { Squad = squad, Roles = Board != null ? PlanningBoardData.FieldJobs.ToList() : new List<string> { "tek" }.Concat(roles).ToList(),
                Perks = RaidPerks(), Counters = RaidCounters(), Answers = RaidAnswers() };
        }

        /// <summary>Zırh plakaları en düşük canlı uzmandan başlayarak birer tane takılır (eşitlikte rol sırası).</summary>
        static void FitPlates(List<SquadSlot> squad, int plates)
        {
            foreach (var slot in squad.OrderBy(x => x.Hp))
                if (plates > 0) { slot.Armor = 1; plates--; }
        }

        /// <summary>Bölgenin emirde, savunmada ve görev fişinde kullanılan imza makinesi: tesisin imza makinesi
        /// (<see cref="Balance.facilitySignatureMachines"/>), alt bayrak kapalıyken <see cref="RegionDef.Enemy"/>.</summary>
        public string RegionSignature(string id)
        {
            if (B.facilitySignatureMachines)
            {
                switch (id)
                {
                    case "sa": return "alev";
                    case "jk": return "muhur";
                    case "na": return "tampon";
                    case "cn": return "montaj";
                    case "ru": return "kirag";
                    case "oc": return "capa";
                }
            }
            return Def(id).Enemy;
        }

        /// <summary>Emre kopyalanan kazanımlar (tesis aşama 8, K23): <see cref="FacilityState.Perks"/>'in kopyası; tesis durumu yoksa boş.</summary>
        List<string> RaidPerks() => Facility != null ? new List<string>(Facility.Perks) : new List<string>();

        ActResult StartLiberationRaidCore(string sectorId, string pick)
        {
            var why = LiberationRaidCheck(sectorId, pick);
            if (why != null) return ActResult.Fail(why);
            var sector = LiberationSector(sectorId);
            var team = S.Teams.First(TeamReady);
            var roles = ResolvePick(team, pick);
            int seed = (int)Math.Floor(S.Rng.Next() * 2147483647);
            S.Fon -= B.liberationRaidCost;
            S.Raids++; sector.Attempts++;
            OrderRaid(sector, team, roles, seed, false);
            return ActResult.Success();
        }

        /// <summary>
        /// Baskın emrinin ortak gövdesi (saldırı ve tesis aşama 11 savunması): etkin sektör, emir değerleri, tesis odaları, plakalar ya da
        /// pano kilidi ve EMP hücresi, ekibin görevi, olay. Tohum, fon ve sayaçlar çağıranda (saldırı tohumu S.Rng'den çeker, savunma
        /// çekmez). Savunmada (<see cref="RaidParams.Mode"/> "defense") hedef eki ve ek makine yoktur; kanadın izleri iki kipte de
        /// emre kopyalanır (<see cref="MarksOn"/>). RNG çekmez.
        /// </summary>
        void OrderRaid(LiberationSectorState sector, TeamState team, List<string> roles, int seed, bool defense)
        {
            int clearedRegions = S.Liberation.Regions.Count(r => r.Completed);
            int tier = RaidTier(clearedRegions);
            var def = Def(sector.Region);
            // Bölgenin özelliği her baskında; kademe 3'ten makineler kopyalanır, 4'te güçlenir
            var traits = new List<string> { def.Trait };
            if (tier >= 3) traits.Add("kopya");
            if (tier >= 4) traits.Add("guclu");
            S.Liberation.ActiveSector = sector.Id;
            S.Raid = new RaidState
            {
                Team = team.Id, Region = sector.Region, N = S.Raids, Seed = seed,
                Params = new RaidParams
                {
                    Seed = seed, Region = sector.Region, RegionName = RegionName(sector.Region), Compute = def.Compute,
                    SectorId = sector.Id, MissionKind = sector.MissionKind,
                    RaidNumber = tier, Tier = tier, IsCore = !defense && sector.MissionKind == "server" && clearedRegions == GameData.Regions.Length - 1,
                    Traits = traits, Signature = RegionSignature(sector.Region),
                    // Aşama 12: tesisin şartı FacilityMaps.EventFor'dan (na Katar, oc Yalpa; alt bayraklarla)
                    Event = !B.liberationRegionEvents ? null : FacilityMaps.EventFor(def, B.facilityKatar, B.facilityYalpa),
                    Squad = Squad(team, roles), Roles = Board != null ? PlanningBoardData.FieldJobs.ToList() : new List<string> { "tek" }.Concat(roles).ToList(), MapSeed = S.Seed, Tactics = 1,
                    TargetHpBonus = defense ? 0 : sector.MissionKind == "server" ? B.liberationServerTargetHp : 0,
                    ExtraMachines = defense ? 0 : sector.MissionKind == "server" ? B.liberationServerMachines : 0,
                    CivilianHp = B.liberationCivilianHp,
                    DynamiteCharges = B.liberationDynamiteCharges, SmokeCharges = B.liberationSmokeCharges,
                    Perks = RaidPerks(),
                    // Tesis aşama 10: önlem kademeleri ve cevaplar emir anında kopyalanır (sonradan öğrenilen önlem süren baskını değiştirmez)
                    Counters = RaidCounters(), Answers = RaidAnswers(),
                },
            };
            // Tesis aşama 11: savunma kipi ve kanadın izlerinin emir kopyası (K23 kalıbı; alt bayrak kapalıyken boş kalır)
            if (defense) S.Raid.Params.Mode = DefenseMode;
            if (MarksOn) S.Raid.Params.Marks = WingMarks(sector.Id);
            // Tesis baskını: kanadın üç odası (kimlikler plan üretmeden; plan ilk OpenRaidRoom'da tembel üretilir). S.Rng çekmez
            S.Raid.Params.Rooms = Enumerable.Range(1, 3).Select(i => FacilityMaps.RoomId(sector.Id, i)).ToList();
            SnapshotFieldIdentities(S.Raid.Params);
            // Zırh plakaları baskın emrinde takılır; kırılan baskın dönüşünde düşer
            if (Board == null) FitPlates(S.Raid.Params.Squad, Shelter.Plates);
            else
            {
                Board.Locked = true;
                foreach (var slot in S.Raid.Params.Squad.Where(p => p.EmpBoosts > 0))
                {
                    var cell = Board.Equipment.FirstOrDefault(e => e.OwnerId == slot.PersonId && e.Kind == "emp_cell");
                    if (cell != null) Board.Equipment.Remove(cell);
                }
            }
            team.At = sector.Region; team.Busy = 1; team.Task = "baskin"; team.LastPick = roles;
            S.Events.Add(new SimEvent { Type = SimEventType.Raid });
        }

        ActResult ResolveLiberationRaidCore(RaidResult result)
        {
            var raid = S.Raid;
            if (raid == null) return ActResult.Fail(Loc.T("Süren baskın yok."));
            // Tesis aşama 11: savunmanın sonucu (hedef alınmış kanattır; Simulation.Siege.cs)
            if (RaidChain.IsDefense(raid.Params)) return ResolveDefense(raid, result);
            var sector = LiberationSector(S.Liberation.ActiveSector);
            if (sector == null || sector.Captured || raid.Params.SectorId != sector.Id) return ActResult.Fail(Loc.T("Baskın hedefi geçersiz."));
            var team = Team(raid.Team);
            S.Raid = null; S.Liberation.ActiveSector = null;
            int down = Math.Max(0, Math.Min(3, result.Down));
            var reward = new LiberationRaidReward();
            if (team != null)
            {
                // Baskın vardiyayı doldurur: ekip sonraki vardiyada hazır (Busy adımda düşer)
                team.Task = null; team.Busy = 1;
                if (Board == null) ReturnSquad(team, raid.Params.Squad, result, reward);
                else
                {
                    bool firstCapture = FacilityTitlesOn && result.Success && !S.RaidLog.Any(l => l.Success && l.SectorId == sector.Id && l.Mode != DefenseMode);
                    ReturnPlanningSquad(raid.Params.Squad, result, reward, raid.Params.Perks, raid.Reports, titleEligible: firstCapture);
                    // Baskında ölen kişinin açık olayı kendiliğinden kapanır.
                    CloseOrphanShelterEvent();
                }
            }
            S.RaidLog.Add(new RaidLogEntry
            {
                Day = S.Day, Region = raid.Region, Success = result.Success, Collateral = result.Collateral, Down = down,
                SectorId = sector.Id, MissionKind = sector.MissionKind, LiberationReward = reward,
                Rooms = raid.Reports.Select(r => r.Encode()).ToList(),
            });
            // Önlem defteri (tesis aşama 10): YZ baskının araç kullanımını sayar; başarısız baskın da sayar (araç kullanıldı). RNG çekmez
            if (CountermeasuresOn) AddRaidTally(raid.Reports);
            // Kalıcı tahta (tesis aşama 11, §9.1): oynanan odaların izleri; başarısız baskın da iz bırakır. RNG çekmez
            if (MarksOn) UpdateFacilityMarks(raid.Params, raid.Reports, reward);
            int civilianLoss = Math.Max(0, result.Collateral) * B.liberationTrustPerCivilian;
            if (!result.Success)
            {
                ChangeAlarm(B.liberationAlarmPerFailure, reward);
                ChangeTrust(reward, -civilianLoss);
                return ActResult.Success(S.Ending != null ? Loc.T("Sivil güven tükendi. Halk direnişe sırtını döndü.")
                    : Loc.T("Ekip geri döndü. Sektör işgal altında; toparlandıktan sonra yeniden deneyebilirsin."));
            }

            double oldFon = S.Fon, oldSupplies = Shelter.Supplies, oldMaterials = Shelter.Materials;
            int oldResidents = Shelter.Population;
            sector.Captured = true; sector.CapturedDay = S.Day;
            // Evrak: alınan kanattan aynı tesisin sıradaki kanadının giriş odası bilinir (çekimsiz)
            LearnFacilityPapers(sector.Region);
            if (sector.MissionKind == "loot")
            {
                S.Fon += B.liberationLootFunds;
                if (Board == null) ShelterLogic.RewardLoot(S, B);
                else
                {
                    PlanningRaidReward(true);
                    // Tesis: ziyaretlerden birinde büyük sandık açıldıysa ek malzeme (aşama 6; kartsız yolda yok)
                    if (raid.Reports.Any(r => r.Crate))
                    {
                        AddBoardMaterial("metal", B.facilityBigCrateMetal);
                        AddBoardMaterial("devre", B.facilityBigCrateCircuits);
                        reward.BigCrate = true;
                    }
                }
            }
            else
            {
                S.Fon += B.liberationServerFunds;
                if (Board == null) ShelterLogic.RewardServer(S, B);
                else PlanningRaidReward(false);
                ChangeAlarm(-B.liberationAlarmPerServer, reward);
            }
            var area = LiberationRegion(sector.Region);
            Moment("good", Loc.F("{0}: {1} geri alındı.", RegionName(sector.Region), sector.Name));
            // Bölge ilk kez tamamlandı: sunucu ödülü ve önlem öğrenmesi bir kez (karşı saldırıdan sonra yeniden alınan bölgede yok)
            bool first = false;
            // Yeniden alınan bölge (tesis aşama 11, §9.3; savunma alt bayrağıyla, K3): bölge fonu ve bölge güveni bölge başına bir kez.
            // Rewarded yazılmadan önce okunur; savunma kapalıyken her tamamlanışta (aşama 10)
            bool firstRegion = true;
            if (area.Completed)
            {
                if (DefenseOn && area.Rewarded) { firstRegion = false; reward.RegionRepeat = true; }
                // Sunucu ödülü (tesis aşama 8, belge §6.2): bölge ilk kez tamamlanınca bir kez; zaferde seçim kurulmaz
                first = !area.Rewarded && Facility != null;
                if (first) area.Rewarded = true;
                if (firstRegion) S.Fon += B.liberationCaptureFunds;
                S.Events.Add(new SimEvent { Type = SimEventType.RegionLiberated, Key = sector.Region });
                if (S.Liberation.Regions.All(r => r.Completed)) End("silme");
                if (first && S.Ending == null && FacilityGear.Reward(sector.Region).Perk != null)
                {
                    Facility.PendingReward = sector.Region;
                    reward.ServerReward = sector.Region;
                }
            }
            reward.Fon = S.Fon - oldFon;
            reward.Supplies = Shelter.Supplies - oldSupplies;
            reward.Materials = Shelter.Materials - oldMaterials;
            reward.Residents = Shelter.Population - oldResidents;
            reward.RegionCompleted = area.Completed;
            // Bölgeyi özgürleştiren baskının sivil bedeli, bölgenin geri verdiği güvenle birlikte sayılır; çekirdek düştüyse zafer önce gelir.
            // Ayakta kalan siviller güveni geri kazandırır (her liberationCiviliansPerTrust sunucu +1)
            reward.CiviliansLeft = Math.Max(0, result.CiviliansLeft);
            reward.TrustFromCivilians = B.liberationCiviliansPerTrust > 0 ? reward.CiviliansLeft / B.liberationCiviliansPerTrust : 0;
            ChangeTrust(reward, (area.Completed && firstRegion ? B.liberationTrustPerRegion : 0) + reward.TrustFromCivilians - civilianLoss);
            // Önlem öğrenmesi (tesis aşama 10, belge §6.3): bölge ilk kez tamamlanınca, oyun bitmediyse (zaferde ve dağılmada yok)
            if (first && CountermeasuresOn && S.Ending == null) LearnCountermeasure(sector.Region, reward);
            if (S.Ending == "dagilma") return ActResult.Success(Loc.T("Sivil güven tükendi. Halk direnişe sırtını döndü."));
            return ActResult.Success(area.Completed
                ? Loc.F("{0} özgür. Sığınak güçlendi; sıradaki komşu bölgeyi seç.", RegionName(sector.Region))
                : Loc.F("{0} geri alındı. Sonraki sektör operasyona açıldı.", sector.Name));
        }

        /// <summary>
        /// Kadro döner: ayaktaki uzmanın eksik canı yara olarak kalır, düşen uzman (ya da yedek; kaçışta çıkamayan da düşmüş
        /// sayılır) yatar ve sağlam kalkar; kırılan
        /// zırh plakası sığınaktan düşer. Sonuçta can bilgisi yoksa (test, fikstür) kadronun ilk <c>Down</c> üyesi düşer, ötekiler
        /// çıktıkları canla ve plakalarıyla döner.
        /// </summary>
        void ReturnSquad(TeamState team, List<SquadSlot> squad, RaidResult result, LiberationRaidReward reward)
        {
            if (squad == null) return;
            var hps = result.Hp != null && result.Hp.Count > 0 ? result.Hp : squad.Select((x, i) => i < result.Down ? 0 : x.Hp).ToList();
            var armor = result.Armor != null && result.Armor.Count > 0 ? result.Armor : squad.Select(x => x.Armor).ToList();
            for (int i = 0; i < squad.Count && i < hps.Count; i++)
            {
                var slot = squad[i];
                int hp = Math.Max(0, hps[i]);
                bool broken = slot.Armor > 0 && i < armor.Count && armor[i] < slot.Armor;
                if (broken) Shelter.Plates = Math.Max(0, Shelter.Plates - 1);
                var member = slot.Rookie ? Shelter.Reserves[slot.Reserve] : team.Members.First(m => m.Role == slot.Type);
                if (hp <= 0) { member.Down = B.liberationInjuryDays; member.Wounds = 0; }
                else if (!slot.Rookie) member.Wounds = Math.Max(0, MaxHp(slot.Type) - hp);
                bool left = result.Left != null && result.Left.Contains(i);
                reward.Squad.Add(new SquadOutcome { Type = slot.Type, Rookie = slot.Rookie, HpBefore = slot.Hp, HpAfter = hp, Down = hp <= 0, Left = left && hp <= 0, PlateBroken = broken });
            }
        }

        /// <summary>Sivil güveni değiştirir (<see cref="TrustMax"/> aşılmaz); sıfıra inerse direniş dağılır.</summary>
        void ChangeTrust(LiberationRaidReward reward, int delta)
        {
            int before = S.Trust;
            S.Trust = Math.Max(0, Math.Min(TrustMax, S.Trust + delta));
            if (Shelter != null) Shelter.MinTrust = Math.Min(Shelter.MinTrust, S.Trust);
            if (reward != null) reward.Trust += S.Trust - before;
            if (S.Trust <= 0) End("dagilma");
        }

        /// <summary>YZ alarmını değiştirir; eşiğe ulaşınca alarm sıfırlanır ve YZ karşı saldırır. Tesis aşama 11 (<see cref="DefenseOn"/>):
        /// saldırı anlık değil, kuşatma kurulur (<see cref="BeginSiege"/>; kanat karar vardiyasında savunulur ya da bırakılır).</summary>
        void ChangeAlarm(double delta, LiberationRaidReward reward)
        {
            if (S.Ending != null) return;
            double before = S.Alarm;
            S.Alarm = Math.Max(0, S.Alarm + delta);
            if (reward != null) reward.Alarm += S.Alarm - before;
            if (S.Alarm < B.liberationAlarmThreshold) return;
            S.Alarm = 0;
            if (DefenseOn) BeginSiege(reward);
            else Counterattack(reward);
        }

        /// <summary>Karşı saldırı: YZ son geri alınan sektörü yeniden işgal eder (bölge özgürse yeniden cephe olur), güven düşer.</summary>
        void Counterattack(LiberationRaidReward reward) => FallSector(PickCounterTarget(), reward);

        /// <summary>Karşı saldırının hedefi: son geri alınan sektör (alınma günü, sonra sektör sırası azalan); yoksa null. Saf.</summary>
        LiberationSectorState PickCounterTarget() => S.Liberation.Regions.SelectMany(r => r.Sectors).Where(x => x.Captured)
            .OrderByDescending(x => x.CapturedDay).ThenByDescending(x => x.Index).FirstOrDefault();

        /// <summary>
        /// Kanat düşer (anlık karşı saldırı; tesis aşama 11: bırakılan, kendiliğinden bırakılan ya da kaybedilen savunmanın kanadı):
        /// karşı saldırı sayacı ve son saldırı yazılır, sektör yeniden işgal edilir (null ise yalnız sayaç ve güven), olay, güven
        /// −<see cref="Balance.liberationCounterattackTrust"/>. Satır sırası aşama 10'un Counterattack'ıyla aynı (savunma alt bayrağı kapalıyken bayt eşitliği).
        /// </summary>
        void FallSector(LiberationSectorState sector, LiberationRaidReward reward)
        {
            S.Counterattacks++;
            S.LastCounterattackDay = S.Day;
            S.LastCounterattack = sector?.Id;
            if (sector != null)
            {
                sector.Captured = false; sector.CapturedDay = -1;
                Moment("bad", Loc.F("{0}: {1} yeniden işgal altında.", RegionName(sector.Region), sector.Name));
            }
            if (reward != null) reward.Counterattack = sector?.Id;
            S.Events.Add(new SimEvent { Type = SimEventType.Counterattack, Key = sector?.Id });
            ChangeTrust(reward, -B.liberationCounterattackTrust);
        }

        /// <summary>Zorluk kademesi (1–4): özgür bölge sayısıyla artar (<c>Balance.liberationTier*At</c>).</summary>
        public int RaidTier(int clearedRegions) =>
            clearedRegions >= B.liberationTier4At ? 4 : clearedRegions >= B.liberationTier3At ? 3 : clearedRegions >= B.liberationTier2At ? 2 : 1;

        /// <summary>Şimdiki zorluk kademesi.</summary>
        public int Tier => RaidTier(S.Liberation.Regions.Count(r => r.Completed));

        /// <summary>
        /// Bu harekâtta ulaşılabilen en yüksek kademe (göstergelerin paydası, "TEHDİT n/m"). Son bölge özgürleşince harekât biter,
        /// bu yüzden kademe en çok bölge sayısının bir eksiğiyle hesaplanır: 12 bölge ve <c>liberationTier4At</c> = 12 iken 3.
        /// Türetilmiş okuyucu; RNG çekmez, durum yazmaz.
        /// </summary>
        public int MaxTier => RaidTier(Math.Max(0, S.Liberation.Regions.Count - 1));

        /// <summary>
        /// Vardiya alarmı: telsiz odasındaki her görevli yükselişi yavaşlatır (eksiye inmez); ifşa ara bedeli
        /// (<see cref="ExposureAlarmBonus"/>) üstüne eklenir.
        /// </summary>
        public double AlarmPerShift => Math.Max(0, B.liberationAlarmPerShift - ShelterLogic.RadioWorkers(Shelter) * B.shelterRadioAlarmPerWorker)
            + ExposureAlarmBonus;

        /// <summary>Vardiya sonu: alarm bir vardiyalık yükselir (adım çağırır).</summary>
        void StepAlarm() => ChangeAlarm(AlarmPerShift, null);
    }
}
