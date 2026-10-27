using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    [Serializable]
    public sealed class HistPoint
    {
        public int Day;
        /// <summary>İşgal oranı (%): geri alınmamış sektörlerin payı.</summary>
        public double Real;
    }

    /// <summary>Son rapor zaman çizelgesindeki bir kırılma anı (yalnızca metin; kararları etkilemez).</summary>
    [Serializable]
    public sealed class Moment
    {
        public int Day;
        public string Kind;
        public string Text;
    }

    public enum SimEventType { Raid, RegionLiberated, End }

    public sealed class SimEvent
    {
        public SimEventType Type;
        public string Key;
    }

    [Serializable]
    public sealed class TeamState
    {
        public string Id, Name, At, Task, DownWhy;
        /// <summary>Busy: görevde (baskın) kalan gün; Down: yaralı, toparlanana kadar kalan gün.</summary>
        public int Busy, Down;
    }

    /// <summary>Taktik haritayı belirleyen her şey: baskın bu parametrelerle deterministik kurulur.</summary>
    [Serializable]
    public sealed class RaidParams
    {
        public int Seed, Compute = 3, RaidNumber = 1;
        public string Region, RegionName = Loc.T("Bölge");
        /// <summary>loot: ikmal kasası alınır; server: sunucu çekirdeği yıkılır.</summary>
        public string MissionKind = "server", SectorId;
        public double Inf = 30;
        public bool IsCore;
        public List<string> Traits = new List<string>();
    }

    [Serializable]
    public sealed class RaidState
    {
        public string Team, Region;
        public int N, Seed;
        public RaidParams Params;
    }

    /// <summary>Baskının harekâta dönen sonucu. Kayıtta tek satır metin olarak saklanır.</summary>
    [Serializable]
    public sealed class RaidResult
    {
        public bool Success, Retreat;
        public int Collateral, Down, Turns;
        public string Reason;

        public string Encode() => $"{(Success ? 1 : 0)}|{(Retreat ? 1 : 0)}|{Collateral}|{Down}|{Turns}";

        public static RaidResult Decode(string s)
        {
            var p = s.Split('|');
            return new RaidResult
            {
                Success = p[0] == "1", Retreat = p[1] == "1",
                Collateral = int.Parse(p[2]), Down = int.Parse(p[3]), Turns = int.Parse(p[4]),
            };
        }
    }

    [Serializable]
    public sealed class RaidLogEntry
    {
        public int Day, Collateral, Down;
        public string Region;
        public bool Success;
        public string SectorId, MissionKind;
        public LiberationRaidReward LiberationReward;
    }

    /// <summary>Harekâtın bütün durumu: gün, fon, ekipler, baskın, bölgeler ve sektörler, sığınak.</summary>
    [Serializable]
    public sealed class WorldState
    {
        public uint Seed;
        public Rng Rng;
        public int Day;
        public double Fon;
        public IncomeBreakdown LastIncome;
        public int LastIncomeDay;
        public List<EconomyTransaction> TransactionLedger = new List<EconomyTransaction>();

        public List<TeamState> Teams = new List<TeamState>();
        public RaidState Raid;
        public int Raids;
        public List<RaidLogEntry> RaidLog = new List<RaidLogEntry>();
        public LiberationCampaignState Liberation;

        public List<HistPoint> Hist = new List<HistPoint>();
        public List<Moment> Moments = new List<Moment>();
        public string Ending;

        /// <summary>Vardiya ilerleyemez: baskın sürüyor.</summary>
        public bool Blocked => Raid != null;

        [NonSerialized] public List<SimEvent> Events = new List<SimEvent>();
    }

    public sealed class ActResult
    {
        public bool Ok;
        public string Msg;
        public static ActResult Fail(string msg) => new ActResult { Ok = false, Msg = msg };
        public static ActResult Success(string msg = null) => new ActResult { Ok = true, Msg = msg };
    }
}
