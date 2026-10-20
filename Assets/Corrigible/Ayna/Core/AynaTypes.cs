using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Corrigible.Ayna.Core
{
    public enum Side { None = -1, Player = 0, Ayna = 1 }
    public enum Architecture { Swarm, Fortress, Ghost }
    public enum DataFamily { Vision, Movement, Energy, Network, Language }
    public enum UnitClass { Air, Ground, Cyber }
    public enum UnitKind { Scout, Sniper, Guardian, Spider, Emp, Overload, Intrusion, Relay, Decoy, PhantomOrder }
    public enum OrderKind { Expand, March, Attack, BuildWall, Train, Produce, NewArmy, Repair, Hide, Feint, MoveCore, StartSingularity, Wait }
    public enum Knowledge { Unknown, Estimate, Exact }
    public enum EndReason { None, Shutdown, Erasure, Singularity, Dominance, Time }
    public enum LearningLevel { Off, Light, Full }
    public enum RejectReason { None, InvalidTarget, InvalidAmount, Unavailable, Duplicate, InsufficientCompute, LostCondition }

    public static class Sides
    {
        public static Side Other(this Side side) => side == Side.Player ? Side.Ayna : Side.Player;
        public static int Index(this Side side)
        {
            if (side != Side.Player && side != Side.Ayna) throw new ArgumentOutOfRangeException(nameof(side));
            return (int)side;
        }
    }

    [Serializable]
    public sealed class Order
    {
        public OrderKind Kind;
        public string Region;
        public int ArmyId, UnitId;
        public int Amount = 1;
        public DataFamily Family;
        public UnitKind Unit;
        public Order Copy() => (Order)MemberwiseClone();
        public static Order Expand(string region, int amount) => new Order { Kind = OrderKind.Expand, Region = region, Amount = amount };
        public static Order Attack(string region, int army) => new Order { Kind = OrderKind.Attack, Region = region, ArmyId = army };
    }

    [Serializable]
    public sealed class Plan
    {
        public List<Order> Orders = new List<Order>();
        public Plan() { }
        public Plan(IEnumerable<Order> orders) { Orders = (orders ?? Enumerable.Empty<Order>()).Select(x => x?.Copy()).ToList(); }
        public Plan Copy() => new Plan(Orders ?? new List<Order>());
    }

    [Serializable]
    public sealed class StartChoice
    {
        public Architecture Architecture;
        public string Region;
        public List<string> RankedRegions = new List<string>();
        public StartChoice Copy() => new StartChoice { Architecture = Architecture, Region = Region, RankedRegions = new List<string>(RankedRegions ?? new List<string>()) };
    }

    [Serializable]
    public sealed class AynaSettings
    {
        public LearningLevel Learning = LearningLevel.Full;
        public AynaSettings Copy() => (AynaSettings)MemberwiseClone();
    }

    [Serializable]
    public sealed class StartSetup
    {
        public uint Seed = 1;
        public StartChoice Player = new StartChoice { Region = "weu" };
        public StartChoice Ayna = new StartChoice { Region = "cn" };
        public AynaSettings Settings = new AynaSettings();
        public List<OpeningCounter> PlayerCounters = new List<OpeningCounter>(), AynaCounters = new List<OpeningCounter>();
        public StartSetup Copy() => new StartSetup { Seed = Seed, Player = Player.Copy(), Ayna = Ayna.Copy(), Settings = Settings.Copy(), PlayerCounters = PlayerCounters.Select(c => c.Copy()).ToList(), AynaCounters = AynaCounters.Select(c => c.Copy()).ToList() };
    }

    public sealed class RegionDefinition
    {
        public string Id { get; }
        public int Key { get; }
        public int Capacity { get; }
        public DataFamily Family { get; }
        public double Longitude { get; }
        public double Latitude { get; }
        internal RegionDefinition(string id, int key, int capacity, DataFamily family, double lon, double lat)
        { Id = id; Key = key; Capacity = capacity; Family = family; Longitude = lon; Latitude = lat; }
    }

    public sealed class LinkDefinition
    {
        public string A { get; }
        public string B { get; }
        public bool Cable { get; }
        public bool Wrap { get; }
        public bool Satellite { get; }
        public string Id => string.CompareOrdinal(A, B) < 0 ? A + ":" + B : B + ":" + A;
        internal LinkDefinition(string a, string b, bool cable = false, bool wrap = false, bool satellite = false)
        { A = a; B = b; Cable = cable; Wrap = wrap; Satellite = satellite; }
        public string Other(string region) => A == region ? B : B == region ? A : null;
    }

    // Eski oyundan yalnız coğrafya kopyalandı; bu katalog hiçbir eski kural tipini çağırmaz.
    public static class AynaWorldData
    {
        public static readonly ReadOnlyCollection<RegionDefinition> Regions = Array.AsReadOnly(new[] {
            new RegionDefinition("na", 1, 8, DataFamily.Language, -100, 40),
            new RegionDefinition("sa", 2, 6, DataFamily.Energy, -60, -15),
            new RegionDefinition("weu", 3, 7, DataFamily.Language, 5, 48),
            new RegionDefinition("eeu", 4, 5, DataFamily.Network, 27, 51),
            new RegionDefinition("ru", 5, 6, DataFamily.Energy, 90, 60),
            new RegionDefinition("me", 6, 5, DataFamily.Energy, 45, 28),
            new RegionDefinition("af", 7, 4, DataFamily.Network, 20, 5),
            new RegionDefinition("in", 8, 8, DataFamily.Network, 78, 22),
            new RegionDefinition("cn", 9, 8, DataFamily.Movement, 105, 35),
            new RegionDefinition("jk", 10, 5, DataFamily.Movement, 135, 37),
            new RegionDefinition("sea", 11, 6, DataFamily.Vision, 110, 5),
            new RegionDefinition("oc", 12, 4, DataFamily.Vision, 135, -25)
        });
        public static readonly ReadOnlyCollection<LinkDefinition> Links = Array.AsReadOnly(new[] {
            new LinkDefinition("na","weu",true), new LinkDefinition("na","sa"), new LinkDefinition("na","jk",true,true),
            new LinkDefinition("sa","af",true), new LinkDefinition("weu","eeu"), new LinkDefinition("weu","af",true),
            new LinkDefinition("weu","me"), new LinkDefinition("eeu","ru"), new LinkDefinition("eeu","me"),
            new LinkDefinition("ru","cn"), new LinkDefinition("me","in",true), new LinkDefinition("me","af"),
            new LinkDefinition("in","cn"), new LinkDefinition("in","sea",true), new LinkDefinition("cn","jk",true),
            new LinkDefinition("cn","sea"), new LinkDefinition("sea","oc",true), new LinkDefinition("jk","oc",true)
        });
        public static readonly ReadOnlyCollection<LinkDefinition> Satellites = Array.AsReadOnly(new[] {
            new LinkDefinition("na","ru",satellite:true), new LinkDefinition("weu","in",satellite:true),
            new LinkDefinition("sa","oc",wrap:true,satellite:true), new LinkDefinition("af","in",satellite:true),
            new LinkDefinition("sa","weu",satellite:true)
        });
        public static int TotalServers => Regions.Sum(x => x.Capacity);
        public static RegionDefinition Region(string id) => Regions.FirstOrDefault(x => x.Id == id);
        public static IEnumerable<string> Neighbours(string id, bool satellites = false) => Links.Concat(satellites ? Satellites : Enumerable.Empty<LinkDefinition>())
            .Select(x => x.Other(id)).Where(x => x != null).OrderBy(x => x, StringComparer.Ordinal);
        static readonly Dictionary<(string From, string To), int> distances = BuildDistances();
        // Coğrafya sabittir. Tekrarlanan BFS yerine aynı sonuçları veren salt okunur tablo kullanılır.
        static Dictionary<(string From, string To), int> BuildDistances()
        {
            var result = new Dictionary<(string From, string To), int>();
            foreach (var start in Regions)
            {
                var q = new Queue<string>(); var seen = new Dictionary<string, int> { [start.Id] = 0 }; q.Enqueue(start.Id);
                while (q.Count > 0)
                {
                    var current = q.Dequeue(); result[(start.Id, current)] = seen[current];
                    foreach (var next in Neighbours(current)) if (!seen.ContainsKey(next)) { seen[next] = seen[current] + 1; q.Enqueue(next); }
                }
            }
            return result;
        }
        public static int Distance(string from, string to) => from != null && to != null && distances.TryGetValue((from, to), out int distance) ? distance : int.MaxValue;
        public static uint BoardSeed(uint seed, int cycle, string region) => seed ^ (uint)cycle ^ (uint)(Region(region) ?? throw new ArgumentException(nameof(region))).Key;
    }

    public sealed class UnitDefinition
    {
        public UnitKind Kind { get; }
        public UnitClass Class { get; }
        public DataFamily Family { get; }
        public int Tier { get; }
        internal UnitDefinition(UnitKind kind, UnitClass type, DataFamily family, int tier) { Kind = kind; Class = type; Family = family; Tier = tier; }
    }

    public static class AynaUnits
    {
        public static readonly ReadOnlyCollection<UnitDefinition> All = Array.AsReadOnly(new[] {
            new UnitDefinition(UnitKind.Scout, UnitClass.Air, DataFamily.Vision, 1), new UnitDefinition(UnitKind.Sniper, UnitClass.Air, DataFamily.Vision, 2),
            new UnitDefinition(UnitKind.Guardian, UnitClass.Ground, DataFamily.Movement, 1), new UnitDefinition(UnitKind.Spider, UnitClass.Ground, DataFamily.Movement, 2),
            new UnitDefinition(UnitKind.Emp, UnitClass.Air, DataFamily.Energy, 1), new UnitDefinition(UnitKind.Overload, UnitClass.Air, DataFamily.Energy, 2),
            new UnitDefinition(UnitKind.Intrusion, UnitClass.Cyber, DataFamily.Network, 1), new UnitDefinition(UnitKind.Relay, UnitClass.Cyber, DataFamily.Network, 2),
            new UnitDefinition(UnitKind.Decoy, UnitClass.Cyber, DataFamily.Language, 1), new UnitDefinition(UnitKind.PhantomOrder, UnitClass.Cyber, DataFamily.Language, 2)
        });
        public static UnitDefinition Get(UnitKind kind) => All[(int)kind];
        public static bool Beats(UnitClass a, UnitClass b) => a == UnitClass.Ground && b == UnitClass.Air || a == UnitClass.Air && b == UnitClass.Cyber || a == UnitClass.Cyber && b == UnitClass.Ground;
        public static Architecture Counter(Architecture architecture) => architecture == Architecture.Ghost ? Architecture.Swarm : architecture == Architecture.Swarm ? Architecture.Fortress : Architecture.Ghost;
    }
}
