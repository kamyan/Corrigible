using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Bir sektör: iki ikmal deposu (yağma) ve bir sunucu merkezi (yıkım); sırayla geri alınır.</summary>
    [Serializable]
    public sealed class LiberationSectorState
    {
        public string Id, Region, MissionKind;
        public int Index, Attempts, CapturedDay = -1;
        public bool Captured;
        public string Name => Index == 0 ? Loc.T("Tedarik deposu") : Index == 1 ? Loc.T("Tahliye geçidi") : Loc.T("Sunucu merkezi");
    }

    [Serializable]
    public sealed class LiberationRegionState
    {
        public string Id;
        public List<LiberationSectorState> Sectors = new List<LiberationSectorState>();
        public int CapturedCount => Sectors.Count(s => s.Captured);
        public bool Completed => Sectors.Count > 0 && Sectors.All(s => s.Captured);
    }

    [Serializable]
    public sealed class LiberationCampaignState
    {
        public string CurrentRegion, ActiveSector;
        public List<LiberationRegionState> Regions = new List<LiberationRegionState>();
        public ShelterState Shelter;
    }

    /// <summary>Depo ve nüfus sınırları uygulandıktan sonra bu baskından gerçekten kazanılan kaynaklar.</summary>
    [Serializable]
    public sealed class LiberationRaidReward
    {
        public double Fon, Supplies, Materials;
        public int Residents;
        public bool RegionCompleted;
    }

    public sealed partial class Simulation
    {
        public LiberationRegionState LiberationRegion(string id) => S.Liberation?.Regions.FirstOrDefault(r => r.Id == id);
        public LiberationSectorState LiberationSector(string id) => S.Liberation?.Regions.SelectMany(r => r.Sectors).FirstOrDefault(s => s.Id == id);

        /// <summary>Tamamlanmış bölgelerin sınırı. Çıkmazda kalan bir bölge tüm harekâtı kilitlemez.</summary>
        public IEnumerable<string> LiberationFrontier
        {
            get
            {
                var cleared = new HashSet<string>(S.Liberation.Regions.Where(r => r.Completed).Select(r => r.Id));
                return GameData.Regions.Where(r => !cleared.Contains(r.Id) && GameData.Links.Any(l =>
                    l.A == r.Id && cleared.Contains(l.B) || l.B == r.Id && cleared.Contains(l.A))).Select(r => r.Id);
            }
        }

        public LiberationSectorState NextLiberationSector(string regionId) => LiberationRegion(regionId)?.Sectors.FirstOrDefault(s => !s.Captured);

        void SetupLiberation()
        {
            S.Liberation = new LiberationCampaignState { CurrentRegion = HQ, Shelter = ShelterLogic.Create(B) };
            foreach (var def in GameData.Regions)
            {
                var area = new LiberationRegionState { Id = def.Id };
                for (int i = 0; i < 3; i++) area.Sectors.Add(new LiberationSectorState
                {
                    Id = def.Id + "_" + (i + 1), Region = def.Id, Index = i, MissionKind = i < 2 ? "loot" : "server",
                });
                S.Liberation.Regions.Add(area);
            }
            // Tek başlangıç ekibi sığınakta
            S.Teams.Add(NewTeam(0));
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
            return ActResult.Success();
        }, "liberation_region", regionId);

        public string LiberationRaidCheck(string sectorId)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (S.Raid != null) return Loc.T("Zaten bir baskın sürüyor.");
            var sector = LiberationSector(sectorId);
            if (sector == null) return Loc.T("Bu bölgede açık sektör yok.");
            if (sector.Captured) return Loc.T("Bu sektör zaten geri alındı.");
            if (sector.Region != S.Liberation.CurrentRegion) return Loc.T("Yalnızca mevcut harekât bölgesine baskın yapılabilir.");
            if (NextLiberationSector(sector.Region)?.Id != sectorId) return Loc.T("Önce önceki sektörü geri al.");
            if (!S.Teams.Any(TeamReady)) return Loc.T("Hazır ekip yok; sığınakta toparlanmak için zamanı ilerlet.");
            if (S.Fon < B.liberationRaidCost) return Loc.F("Bu baskın {0} fon ister.", B.liberationRaidCost);
            return null;
        }

        public ActResult StartLiberationRaid(string sectorId) => RunCommand(() => StartLiberationRaidCore(sectorId), "liberation_raid", sectorId);

        ActResult StartLiberationRaidCore(string sectorId)
        {
            var why = LiberationRaidCheck(sectorId);
            if (why != null) return ActResult.Fail(why);
            var sector = LiberationSector(sectorId);
            var team = S.Teams.First(TeamReady);
            int clearedRegions = S.Liberation.Regions.Count(r => r.Completed);
            int seed = (int)Math.Floor(S.Rng.Next() * 2147483647);
            S.Fon -= B.liberationRaidCost;
            S.Raids++; sector.Attempts++;
            S.Liberation.ActiveSector = sector.Id;
            S.Raid = new RaidState
            {
                Team = team.Id, Region = sector.Region, N = S.Raids, Seed = seed,
                Params = new RaidParams
                {
                    Seed = seed, Region = sector.Region, RegionName = Def(sector.Region).Name, Compute = Def(sector.Region).Compute,
                    SectorId = sector.Id, MissionKind = sector.MissionKind,
                    Inf = B.liberationRaidThreat + clearedRegions * B.liberationRaidThreatPerRegion,
                    RaidNumber = Math.Min(3, clearedRegions + 1), IsCore = sector.MissionKind == "server" && clearedRegions == GameData.Regions.Length - 1,
                    // Harekât ilerledikçe makineler kendini kopyalar: baskında daha çok robot
                    Traits = clearedRegions < B.liberationCopyFromRegion ? new List<string>() : new List<string> { "kopya" },
                },
            };
            team.At = sector.Region; team.Busy = 1; team.Task = "baskin";
            S.Events.Add(new SimEvent { Type = SimEventType.Raid });
            return ActResult.Success();
        }

        ActResult ResolveLiberationRaidCore(RaidResult result)
        {
            var raid = S.Raid;
            if (raid == null) return ActResult.Fail(Loc.T("Süren baskın yok."));
            var sector = LiberationSector(S.Liberation.ActiveSector);
            if (sector == null || sector.Captured || raid.Params.SectorId != sector.Id) return ActResult.Fail(Loc.T("Baskın hedefi geçersiz."));
            var team = Team(raid.Team);
            S.Raid = null; S.Liberation.ActiveSector = null;
            int down = Math.Max(0, Math.Min(3, result.Down));
            if (team != null)
            {
                team.Task = null; team.Busy = 0;
                team.Down = down * B.liberationInjuryDays;
                team.DownWhy = team.Down > 0 ? Loc.T("yaralı") : null;
            }
            var reward = new LiberationRaidReward();
            S.RaidLog.Add(new RaidLogEntry
            {
                Day = S.Day, Region = raid.Region, Success = result.Success, Collateral = result.Collateral, Down = down,
                SectorId = sector.Id, MissionKind = sector.MissionKind, LiberationReward = reward,
            });
            if (!result.Success) return ActResult.Success(Loc.T("Ekip geri döndü. Sektör işgal altında; toparlandıktan sonra yeniden deneyebilirsin."));

            double oldFon = S.Fon, oldSupplies = Shelter.Supplies, oldMaterials = Shelter.Materials;
            int oldResidents = Shelter.Population;
            sector.Captured = true; sector.CapturedDay = S.Day;
            if (sector.MissionKind == "loot")
            {
                S.Fon += B.liberationLootFunds;
                ShelterLogic.RewardLoot(S, B);
            }
            else
            {
                S.Fon += B.liberationServerFunds;
                ShelterLogic.RewardServer(S, B);
            }
            var area = LiberationRegion(sector.Region);
            Moment("good", Loc.F("{0}: {1} geri alındı.", Def(sector.Region).Name, sector.Name));
            if (area.Completed)
            {
                S.Fon += B.liberationCaptureFunds;
                S.Events.Add(new SimEvent { Type = SimEventType.RegionLiberated, Key = sector.Region });
                if (S.Liberation.Regions.All(r => r.Completed)) End("silme");
            }
            reward.Fon = S.Fon - oldFon;
            reward.Supplies = Shelter.Supplies - oldSupplies;
            reward.Materials = Shelter.Materials - oldMaterials;
            reward.Residents = Shelter.Population - oldResidents;
            reward.RegionCompleted = area.Completed;
            return ActResult.Success(area.Completed
                ? Loc.F("{0} özgür. Sığınak güçlendi; sıradaki komşu bölgeyi seç.", Def(sector.Region).Name)
                : Loc.F("{0} geri alındı. Sonraki sektör operasyona açıldı.", sector.Name));
        }
    }
}
