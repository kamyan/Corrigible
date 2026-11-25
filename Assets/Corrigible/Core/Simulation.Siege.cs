using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis aşama 11 (Docs/TESIS-BASKINLARI-TASARIMI.md §9.1–9.3, §11.4): bekleyen karşı saldırı (kuşatma), savun ve bırak komutları,
    /// üç odalı savunmanın sonucu, kalıcı tahtanın (izler) toplanması ve YZ onarımı. Kuşatma eşikte kurulur (<see cref="BeginSiege"/>,
    /// <c>ChangeAlarm</c>'dan); karar vardiyasında (<see cref="FacilitySiegeEntry.Due"/>) baskın emri kilitlidir, <see cref="DefendSector"/>
    /// (siege_defend) ya da <see cref="YieldSector"/> (siege_yield) verilir; karar verilmezse vardiya kapanışında kanat kendiliğinden
    /// bırakılır (<see cref="AdvanceFacilitySiege"/>). Her kural <see cref="DefenseOn"/> ya da <see cref="MarksOn"/> arkasındadır; yeni
    /// S.Rng çekimi yoktur (savunmanın tohumu ayrı akıştan, <see cref="SiegeSalt"/>). Durum (<see cref="FacilityState.Marks"/>,
    /// <see cref="FacilityState.Sieges"/>, <see cref="FacilityState.Repairs"/>) kayda yazılmaz; komutlardan ve vardiyalardan yeniden kurulur.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>Savunma emrinin kipi (<see cref="RaidParams.Mode"/>).</summary>
        public const string DefenseMode = "defense";

        /// <summary>Kuşatma günlüğünün sonuç kimlikleri (<see cref="FacilitySiegeEntry.Outcome"/>; kısa ASCII, JSON'da durur).</summary>
        public const string SiegePending = "", SiegeDefending = "savun", SiegeHeld = "tutuldu", SiegeLost = "dustu",
            SiegeYielded = "birak", SiegeAuto = "kendi";

        /// <summary>Savunma tohumunun akış tuzu ("SIEG"; kuşatma girdisinin dizini eklenir, K12). S.Rng'den çekilmez.</summary>
        const uint SiegeSalt = 0x53494547u;

        /// <summary>Vardiya kapanışı sürüyor (<see cref="Step"/> içinde; kayda girmez). Kapanışta kurulan kuşatmanın karar vardiyası
        /// artmış gündür (ertesi vardiya), vardiya ortasında kurulanınki bir sonraki (K5).</summary>
        bool stepping;

        // ---------- Okuyucular (durum yazmaz, RNG çekmez; sunum ve bot, K40) ----------
        /// <summary>Kuşatma ve savunma işler: savunma alt bayrağı (<see cref="Balance.facilityDefenseEnabled"/>). Kapalıyken eşikte anlık
        /// karşı saldırı, savunma ve komutlar yoktur, yeniden alınan bölge fon ve güveni yeniden verir.</summary>
        public bool DefenseOn => B.facilityDefenseEnabled && Facility != null;

        /// <summary>Kalıcı tahta işler: iz alt bayrağı (<see cref="Balance.facilityMarks"/>). Kapalıyken iz, onarım, plandaki iz ve emirdeki
        /// iz kopyası yoktur (aşama 10 tahtaları).</summary>
        public bool MarksOn => B.facilityMarks && Facility != null;

        /// <summary>Karar bekleyen kuşatma (günlüğün sonuç bekleyen girdisi); yoksa ya da savunma kapalıyken null.</summary>
        public FacilitySiegeEntry PendingSiege =>
            DefenseOn ? Facility.Sieges.LastOrDefault(e => e != null && string.IsNullOrEmpty(e.Outcome)) : null;

        /// <summary>Bekleyen ya da süren (savunma emri verilmiş) kuşatma; yoksa null. En çok bir tane olur.</summary>
        public FacilitySiegeEntry ActiveSiege =>
            DefenseOn ? Facility.Sieges.LastOrDefault(e => e != null && (string.IsNullOrEmpty(e.Outcome) || e.Outcome == SiegeDefending)) : null;

        /// <summary>Bu vardiya kuşatmanın karar vardiyası: baskın emri kilitli, savun ya da bırak verilebilir (kurulduğu vardiyada false).</summary>
        public bool SiegeDecisionDue => PendingSiege is FacilitySiegeEntry e && S.Day >= e.Due;

        /// <summary>Bekleyen ya da süren kuşatmanın kanadı; yoksa null.</summary>
        public LiberationSectorState SiegeSector => ActiveSiege is FacilitySiegeEntry e ? LiberationSector(e.Sector) : null;

        /// <summary>Kanat bu vardiya komutla bırakıldı (o vardiya ona baskın verilmez; K10).</summary>
        public bool YieldedToday(string sectorId) => DefenseOn && sectorId != null
            && Facility.Sieges.Any(e => e != null && e.Outcome == SiegeYielded && e.Sector == sectorId && e.Closed == S.Day);

        /// <summary>Odanın izleri (kalıcı tahta), liste (onarım) sırasıyla; kopya değil, okuyan değiştirmemeli. Kapalıyken boş.</summary>
        public IReadOnlyList<FacilityMark> FacilityMarksOf(string roomId) => FacilityMarks.Of(MarksOn ? Facility.Marks : null, roomId);

        /// <summary>Kanadın üç odasının izlerinin kopyası, liste sırasıyla (baskın emrine kopyalanır).</summary>
        public List<FacilityMark> WingMarks(string sectorId) => Facility == null ? new List<FacilityMark>()
            : Facility.Marks.Where(m => m != null && FacilityMarks.InWing(m.Room, sectorId)).Select(FacilityMarks.Copy).ToList();

        /// <summary>Bölgenin sunucu ödülü verildi (ilk tamamlanış; <see cref="LiberationRegionState.Rewarded"/>).</summary>
        public bool RegionRewarded(string regionId) => LiberationRegion(regionId)?.Rewarded ?? false;

        /// <summary>Bölgeyi tamamlayan baskın bölge fonunu (<see cref="Balance.liberationCaptureFunds"/>) ve bölge güvenini verir mi: savunma
        /// açıkken yalnız ilk tamamlanışta (§9.3), kapalıyken her seferinde.</summary>
        public bool RegionFundsDue(string regionId) => !(DefenseOn && RegionRewarded(regionId));

        /// <summary>Savunmanın bir dalgasının makine sayısı (kademeyle; <see cref="Baskin.DefenseWaveSize"/>).</summary>
        public int DefenseWaveSize(int tier) => Baskin.DefenseWaveSize(B.facilityDefenseWaveBase, tier);

        /// <summary>Savunmanın toplam süresi (tek tur sayacı; <see cref="RaidChain.DefenseTurns"/>).</summary>
        public int DefenseTurns => RaidChain.DefenseTurns(B);

        /// <summary>Savunmanın dalga sayısı (<see cref="RaidChain.DefenseWaveCount"/>).</summary>
        public int DefenseWaveCount => RaidChain.DefenseWaveCount(B);

        /// <summary>Emirden önce, verilen kademede giriş odasının dalga takvimi (terk gecikmesiz; <see cref="RaidChain.DefenseSchedule"/>).
        /// Sunum (görev fişi) takvimi buradan okur, denge alanlarını kendisi sıkıştırmaz (K40).</summary>
        public List<Baskin.DefenseWave> DefenseSchedulePreview(int tier) => RaidChain.DefenseSchedule(B, tier, null);

        /// <summary>Saha rölesinin canı, verilen kazanımlarla (Siper eki; <see cref="RaidChain.DefenseRelayHp"/>).</summary>
        public int DefenseRelayHpFor(IReadOnlyCollection<string> perks) => RaidChain.DefenseRelayHp(B, perks);

        /// <summary>Savunmada terk edilen (içeri çekilinen) oda başına güven kaybı.</summary>
        public int DefenseRoomTrust => Math.Max(0, B.facilityDefenseRoomTrust);

        /// <summary>Emir bir savunma mı (<see cref="RaidChain.IsDefense"/>).</summary>
        public static bool IsDefense(RaidParams p) => RaidChain.IsDefense(p);

        // ---------- Kuşatmanın kurulması ----------
        /// <summary>
        /// Alarm eşiğinde (savunma açıkken; <c>ChangeAlarm</c>): hedef karşı saldırının hedefidir (son alınan kanat). Alınmış kanat yoksa ya
        /// da bekleyen/süren kuşatma varken (ikincisi kurulmaz, K7) bugünkü kanatsız sonuç: yalnız sayaç ve güven −1. Değilse günlüğe girdi
        /// (karar vardiyası: kapanışta kurulursa bu gün, vardiya ortasında kurulursa ertesi gün), an ve olay; sayaçlar kanat düşünce yazılır (K6).
        /// </summary>
        void BeginSiege(LiberationRaidReward reward)
        {
            var sector = PickCounterTarget();
            if (sector == null || ActiveSiege != null) { FallSector(null, reward); return; }
            Facility.Sieges.Add(new FacilitySiegeEntry
            {
                Sector = sector.Id, Region = sector.Region, Day = S.Day, Due = stepping ? S.Day : S.Day + 1, Outcome = SiegePending,
            });
            Moment("bad", Loc.F("{0}: {1} kuşatıldı; sonraki vardiya savun ya da bırak.", RegionName(sector.Region), sector.Name));
            S.Events.Add(new SimEvent { Type = SimEventType.Siege, Key = sector.Id });
            if (reward != null) reward.Siege = sector.Id;
        }

        // ---------- Komutlar ----------
        string SiegeCommandCheck()
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (!DefenseOn) return Loc.T("Kanat savunması kapalı.");
            if (S.Raid != null) return Loc.T("Zaten bir baskın sürüyor.");
            var e = PendingSiege;
            if (e == null) return Loc.T("Bekleyen karşı saldırı yok.");
            if (S.Day < e.Due) return Loc.T("Saldırı bu vardiya kuruldu; sonraki vardiya savun ya da bırak.");
            return null;
        }

        /// <summary>Savunma emri verilebilir mi (null) ya da gerekçe (Loc'lu): oyun sürüyor, savunma açık, süren baskın yok, karar
        /// vardiyası, kanat alınmış, hazır ekip; kartlıda bekleyen sunucu ödülü yok ve pano denetimi (kadro, kit, kilit), kartsızda
        /// Teknisyen ya da yedeği. Fon istenmez.</summary>
        public string DefendSectorCheck()
        {
            string why = SiegeCommandCheck();
            if (why != null) return why;
            var sector = LiberationSector(PendingSiege.Sector);
            if (sector == null || !sector.Captured) return Loc.T("Baskın hedefi geçersiz.");
            var team = S.Teams.FirstOrDefault(TeamReady);
            if (team == null) return Loc.T("Ekip bu vardiya baskına çıktı; vardiyayı bitir, sonraki vardiyada hazır.");
            if (Board != null)
            {
                if (!string.IsNullOrEmpty(Facility.PendingReward)) return Loc.T("Önce sunucu ödülünü seç: tarif ya da kazanım.");
                return PlanningRaidCheck();
            }
            if (!SquadCanRaid(team))
                return Loc.F("Teknisyen yaralı ({0} gün); yerine çıkacak yedek uzman yok.", team.Members.First(m => m.Role == "tek").Down);
            return null;
        }

        /// <summary>Kanat bırakılabilir mi (null) ya da gerekçe: oyun sürüyor, savunma açık, süren baskın yok, karar vardiyası.</summary>
        public string YieldSectorCheck() => SiegeCommandCheck();

        /// <summary>
        /// Savunma emri (siege_defend; argümansız): kuşatılan kanadın üç odasında üç odalı savunma (<see cref="RaidParams.Mode"/>
        /// "defense"). Tohum S.Rng'den çekilmez (<c>Rng.Mix(S.Seed, SiegeSalt + girdi dizini)</c>, K12); fon düşmez, sektörün denemesi
        /// artmaz, baskın sayısı artar (K9). Kadro, pano kilidi ve EMP hücresi baskın emrindeki gibi. Odalar ve sonuç baskınınkiyle aynı
        /// API'dir (raid_room, raidres).
        /// </summary>
        public ActResult DefendSector() => RunCommand(DefendSectorCore, "siege_defend");

        ActResult DefendSectorCore()
        {
            string why = DefendSectorCheck();
            if (why != null) return ActResult.Fail(why);
            var e = PendingSiege;
            int index = Facility.Sieges.IndexOf(e);
            var sector = LiberationSector(e.Sector);
            var team = S.Teams.First(TeamReady);
            var roles = ResolvePick(team, null);
            int seed = (int)(Rng.Mix(S.Seed, SiegeSalt + (uint)index) & 0x7FFFFFFF);
            S.Raids++;
            OrderRaid(sector, team, roles, seed, true);
            e.Outcome = SiegeDefending;
            return ActResult.Success(Loc.F("{0}: {1} savunuluyor.", RegionName(sector.Region), sector.Name));
        }

        /// <summary>
        /// Kanadı bırak (siege_yield; argümansız): kanat düşer (sayaç, olay, güven −<see cref="Balance.liberationCounterattackTrust"/>),
        /// günlüğe "birak" yazılır ve o vardiya o kanada baskın verilmez. Fon ve RNG değişmez.
        /// </summary>
        public ActResult YieldSector() => RunCommand(() =>
        {
            string why = YieldSectorCheck();
            if (why != null) return ActResult.Fail(why);
            var e = PendingSiege;
            e.Outcome = SiegeYielded;
            e.Closed = S.Day;
            var sector = LiberationSector(e.Sector);
            FallSector(sector != null && sector.Captured ? sector : null, null);
            if (S.Ending == "dagilma") return ActResult.Success(Loc.T("Sivil güven tükendi. Halk direnişe sırtını döndü."));
            return ActResult.Success(sector == null ? "" : Loc.F("{0}: {1} bırakıldı.", RegionName(sector.Region), sector.Name));
        }, "siege_yield");

        // ---------- Savunmanın sonucu ----------
        /// <summary>
        /// Üç odalı savunmanın sonucu (raidres; <c>ResolveLiberationRaidCore</c>'dan, §3.5): kadro döner (deneyim yalnız kazanınca; kazanınca
        /// ekibin odasında yatan ve ekiple gelen çıkarılır, kayıpta yatan ölür), günlük ve sayım; izler (son odanın raporundan, terk edilen
        /// odalarınki silinir); kaybedilirse kanat düşer. Güven: terk edilen oda başına −<see cref="DefenseRoomTrust"/> ve sivil kaybı
        /// (kayıpta ek olarak kanadın düşmesi). Alarm, fon, ganimet, evrak, ödül ve öğrenme yoktur. RNG çekmez.
        /// </summary>
        ActResult ResolveDefense(RaidState raid, RaidResult result)
        {
            var sector = LiberationSector(raid.Params.SectorId);
            var entry = ActiveSiege;
            if (sector == null || !sector.Captured || S.Liberation.ActiveSector != sector.Id || entry == null || entry.Sector != sector.Id)
                return ActResult.Fail(Loc.T("Baskın hedefi geçersiz."));
            var team = Team(raid.Team);
            S.Raid = null; S.Liberation.ActiveSector = null;
            int down = Math.Max(0, Math.Min(3, result.Down));
            int abandoned = RaidChain.Abandoned(raid.Params, raid.Reports);
            var reward = new LiberationRaidReward { Defense = true, Held = result.Success, Abandoned = abandoned };
            if (team != null)
            {
                team.Task = null; team.Busy = 1;
                if (Board == null) ReturnSquad(team, raid.Params.Squad, result, reward);
                else
                {
                    ReturnPlanningSquad(raid.Params.Squad, result, reward, raid.Params.Perks, raid.Reports,
                        experience: result.Success);
                    CloseOrphanShelterEvent();
                }
            }
            S.RaidLog.Add(new RaidLogEntry
            {
                Day = S.Day, Region = raid.Region, Success = result.Success, Collateral = result.Collateral, Down = down,
                SectorId = sector.Id, MissionKind = sector.MissionKind, LiberationReward = reward, Mode = DefenseMode,
                Rooms = raid.Reports.Select(r => r.Encode()).ToList(),
            });
            // Savunma da önlem sayımına girer (araç kullanıldı, K26); öğrenme yok (bölge tamamlanmaz)
            if (CountermeasuresOn) AddRaidTally(raid.Reports);
            if (MarksOn) UpdateFacilityMarks(raid.Params, raid.Reports, reward);
            entry.Outcome = result.Success ? SiegeHeld : SiegeLost;
            entry.Closed = S.Day;
            entry.Abandoned = abandoned;
            int civilianLoss = Math.Max(0, result.Collateral) * B.liberationTrustPerCivilian;
            if (result.Success) Moment("good", Loc.F("{0}: {1} tutuldu.", RegionName(sector.Region), sector.Name));
            else FallSector(sector, reward);
            ChangeTrust(reward, -DefenseRoomTrust * abandoned - civilianLoss);
            if (S.Ending == "dagilma") return ActResult.Success(Loc.T("Sivil güven tükendi. Halk direnişe sırtını döndü."));
            return ActResult.Success(result.Success ? Loc.F("{0}: {1} tutuldu.", RegionName(sector.Region), sector.Name)
                : Loc.F("{0}: {1} düştü.", RegionName(sector.Region), sector.Name));
        }

        // ---------- Kalıcı tahta (izler) ----------
        /// <summary>
        /// Baskının (saldırı ya da savunma) oda raporlarından izleri toplar (§9.1; raidres): oynanan her odanın ziyaretlerinden türetilir
        /// (<see cref="FacilityMarks.Derive"/>; plan emrin kademesi ve şartıyla) ve kalıcı tahtaya birleştirilir (<see cref="FacilityMarks.Merge"/>).
        /// Savunmada terk edilen odaların izleri silinir (<see cref="FacilityMarks.Wipe"/>, K32); oynanmamış odalar değişmez. Defter reward'a
        /// yazılır (yeni, silinen, terkle silinen). RNG çekmez.
        /// </summary>
        void UpdateFacilityMarks(RaidParams p, IReadOnlyList<RoomReport> reports, LiberationRaidReward reward)
        {
            var plan = FacilityPlan;
            if (plan == null || reports == null || reports.Count == 0 || p.Rooms == null) return;
            bool defense = RaidChain.IsDefense(p);
            int lastRoom = reports[reports.Count - 1].Room;
            bool events = !string.IsNullOrEmpty(p.Event);
            for (int idx = 1; idx <= p.Rooms.Count; idx++)
            {
                var room = plan.Room(p.Rooms[idx - 1]);
                if (room == null) continue;
                if (defense && idx < lastRoom)
                {
                    int wiped = FacilityMarks.Wipe(Facility.Marks, room.Id);
                    if (reward != null) reward.MarksWiped += wiped;
                    continue;
                }
                var visits = reports.Where(r => r.Room == idx).ToList();
                if (visits.Count == 0) continue;
                var derived = FacilityMarks.Derive(room, visits, S.Day, room.PlanCells(p.Tier, events));
                var (added, gone) = FacilityMarks.Merge(Facility.Marks, room.Id, derived);
                if (reward == null) continue;
                reward.MarksNew += added;
                reward.MarksGone += gone;
            }
        }

        // ---------- Vardiya kapanışı ----------
        /// <summary>
        /// Kendiliğinden bırakma (vardiya kapanışı, StepAlarm'dan önce; K11): biten vardiya bekleyen kuşatmanın karar vardiyasıydıysa
        /// ve karar verilmediyse kanat düşer (sayaç, olay, güven −1), günlüğe "kendi" ve biten vardiyanın günü yazılır. Bırak kilidi
        /// kurulmaz. Çekimsiz.
        /// </summary>
        void AdvanceFacilitySiege()
        {
            if (!DefenseOn || S.Ending != null) return;
            var e = PendingSiege;
            if (e == null || S.Day - 1 < e.Due) return;
            e.Outcome = SiegeAuto;
            e.Closed = S.Day - 1;
            var sector = LiberationSector(e.Sector);
            if (sector == null || !sector.Captured) return;
            Moment("bad", Loc.F("{0}: {1} için karar verilmedi; kanat bırakıldı.", RegionName(sector.Region), sector.Name));
            FallSector(sector, null);
        }

        /// <summary>
        /// YZ onarımı (vardiya kapanışı; §9.1, K31): işgaldeki (alınmamış) her kanatta, bölge ve sektör sırasıyla, kanadın üç odasının
        /// izlerinden listede en önde duran <see cref="Balance.facilityRepairPerShift"/> tanesi silinir ve onarım günlüğüne
        /// (<see cref="FacilityState.Repairs"/>) bugünün günüyle kopyalanır. Alınmış kanatta onarım yok. Çekimsiz.
        /// </summary>
        void AdvanceFacilityRepairs()
        {
            if (!MarksOn || Facility.Marks.Count == 0) return;
            int per = Math.Max(0, B.facilityRepairPerShift);
            if (per == 0) return;
            foreach (var region in S.Liberation.Regions)
                foreach (var sector in region.Sectors)
                {
                    if (sector.Captured) continue;
                    int left = per;
                    for (int i = 0; i < Facility.Marks.Count && left > 0;)
                    {
                        var m = Facility.Marks[i];
                        if (m == null || !FacilityMarks.InWing(m.Room, sector.Id)) { i++; continue; }
                        var c = FacilityMarks.Copy(m);
                        c.Day = S.Day;
                        Facility.Repairs.Add(c);
                        Facility.Marks.RemoveAt(i);
                        left--;
                    }
                }
        }
    }
}
