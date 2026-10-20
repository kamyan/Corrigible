using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Corrigible.Core;
using Newtonsoft.Json;

namespace Corrigible.Ayna.Core
{
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class RegionState
    {
        internal string Id;
        internal int Free;
        internal int[] Owned = new int[2];
        internal int[] Wall = new int[2];
    }
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class UnitState
    {
        internal int Id;
        internal UnitKind Kind;
        internal double Hp;
    }
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class ArmyState
    {
        internal int Id;
        internal string Region;
        internal List<UnitState> Units = new List<UnitState>();
    }
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class ProgressState
    {
        internal DataFamily Family;
        internal int Tier, Progress;
        internal string Region;
        internal List<DataFamily> Families = new List<DataFamily>();
    }
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class SideState
    {
        internal Architecture Architecture;
        internal double Compute;
        internal string CoreRegion;
        internal bool CoreDestroyed;
        internal int[] Tiers = new int[5];
        internal List<ArmyState> Armies = new List<ArmyState>();
        internal ProgressState Training, CoreMove, Singularity;
        internal double LastNoise, LastIncome;
        internal int BattleCount;
        internal int NextArmyId, NextUnitId;
    }
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class KnownRegion
    {
        internal string Id;
        internal int EnemyServers, EnemyWall, SeenCycle = -1;
        internal List<ArmyView> Armies = new List<ArmyView>();
    }
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class KnowledgeState
    {
        internal int NextObservationId = 1, ReceiptCycle;
        internal List<KnownRegion> Regions = new List<KnownRegion>();
        internal string EnemyCore;
        internal int CoreSeenCycle = -1;
        internal Architecture? EnemyArchitecture;
        internal List<Observation> Observations = new List<Observation>();
        internal List<OrderReceipt> Receipts = new List<OrderReceipt>();
        internal List<BattleView> Battles = new List<BattleView>();
        internal List<BoardLearningObservation> Boards = new List<BoardLearningObservation>();
    }
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class OutageState
    {
        internal Side Side;
        internal string Link;
        internal int Until;
    }
    [JsonObject(MemberSerialization.Fields)]
    internal sealed class WorldState
    {
        internal ForecastState[] Forecasts = { new ForecastState(), new ForecastState() };
        internal bool[] Surprise = new bool[2], FirstStrike = new bool[2];
        internal string[] FirstStrikeRegion = new string[2];
        internal List<double>[] Readability = { new List<double>(), new List<double>() };
        internal uint Seed;
        internal int Cycle = 1, Battles;
        internal double Awareness;
        internal Rng WorldRng;
        internal List<RegionState> Regions = new List<RegionState>();
        internal SideState[] Sides = { new SideState(), new SideState() };
        internal KnowledgeState[] Knowledge = { new KnowledgeState(), new KnowledgeState() };
        internal List<OutageState> Outages = new List<OutageState>();
        internal string ScannedRegion;
        internal int ScanUntil;
        internal AynaSimulation.ResolutionState Pending;
        internal Outcome Outcome = new Outcome();
        internal RegionState Region(string id) => Regions.FirstOrDefault(x => x.Id == id);
        internal int Servers(int side) => Regions.Sum(x => x.Owned[side]);
        internal int Data(int side, DataFamily family) => Regions.Where(x => AynaWorldData.Region(x.Id).Family == family).Sum(x => x.Owned[side]);
        internal string Hash()
        {
            // Dil/metin ve duvar saati bulunmayan kanonik durum; bütün listeler kimlik/komut sırasındadır.
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this)))).Replace("-", "").ToLowerInvariant();
        }
    }
}
