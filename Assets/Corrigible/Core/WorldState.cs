using System;
using System.Collections.Generic;
using System.Linq;

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

    public enum SimEventType { Raid, RegionLiberated, End, Counterattack }

    public sealed class SimEvent
    {
        public SimEventType Type;
        public string Key;
    }

    /// <summary>Saha uzmanı ya da yedek uzman: baskında aldığı hasar yara olarak kalır, düşerse yatar.</summary>
    [Serializable]
    public sealed class SpecialistState
    {
        /// <summary>Rol (Baskin birim türü: tek, emp, ana); yedek uzmanda boş, çıktığı baskında eksik rolü doldurur.</summary>
        public string Role;
        /// <summary>Wounds: eksik can; Down: yaralı, kalkana kadar kalan vardiya.</summary>
        public int Wounds, Down;
    }

    [Serializable]
    public sealed class TeamState
    {
        public string Id, Name, At, Task;
        /// <summary>Görevde (baskın) kalan vardiya: baskın vardiyayı doldurur.</summary>
        public int Busy;
        /// <summary>Teknisyen, EMP uzmanı, Analist (rol sırasıyla).</summary>
        public List<SpecialistState> Members = new List<SpecialistState>();
    }

    /// <summary>Baskına çıkan bir uzman: tahtadaki birimin türü, canı ve zırh plakası.</summary>
    [Serializable]
    public sealed class SquadSlot
    {
        public string Type;
        public int Hp, Armor;
        /// <summary>Rolün yerine çıkan yedek uzman (sığınaktaki yedek sırası); asıl uzmanda −1.</summary>
        public int Reserve = -1;
        public bool Rookie => Reserve >= 0;
    }

    /// <summary>Taktik haritayı belirleyen her şey: baskın bu parametrelerle deterministik kurulur.</summary>
    [Serializable]
    public sealed class RaidParams
    {
        /// <summary>RaidNumber: yerleşim seçimine katılır (kademe); Tier: zorluk kademesi 1–4 (kademe başına bir makine fazla).</summary>
        public int Seed, Compute = 3, RaidNumber = 1, Tier = 1;
        public string Region, RegionName = Loc.T("Bölge");
        /// <summary>loot: ikmal kasası alınır; server: sunucu çekirdeği yıkılır.</summary>
        public string MissionKind = "server", SectorId;
        public bool IsCore;
        /// <summary>Bölge özelliği (<c>Baskin.RegionTraits</c>) ve kademenin getirdikleri ("kopya", "guclu").</summary>
        public List<string> Traits = new List<string>();
        /// <summary>Bölgenin imza düşmanı (Baskin birim türü); boşsa yalnız dron ve bakım robotu.</summary>
        public string Signature;
        /// <summary>Kadro (sırasıyla giriş kareleri); boşsa tam ve sağlam üç uzman.</summary>
        public List<SquadSlot> Squad;
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
        /// <summary>Kadronun sırasıyla kalan canı (düşen 0) ve kalan zırh plakası.</summary>
        public List<int> Hp = new List<int>(), Armor = new List<int>();

        public string Encode() => $"{(Success ? 1 : 0)}|{(Retreat ? 1 : 0)}|{Collateral}|{Down}|{Turns}|{string.Join(",", Hp)}|{string.Join(",", Armor)}";

        public static RaidResult Decode(string s)
        {
            var p = s.Split('|');
            return new RaidResult
            {
                Success = p[0] == "1", Retreat = p[1] == "1",
                Collateral = int.Parse(p[2]), Down = int.Parse(p[3]), Turns = int.Parse(p[4]),
                Hp = p.Length > 5 ? Ints(p[5]) : new List<int>(), Armor = p.Length > 6 ? Ints(p[6]) : new List<int>(),
            };
        }

        static List<int> Ints(string s) => s.Length == 0 ? new List<int>() : s.Split(',').Select(int.Parse).ToList();
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
        /// <summary>Sivil güven: kırılan sivil sunucularla düşer, özgürleşen bölgelerle döner; sıfırda harekât kaybedilir.</summary>
        public int Trust;
        /// <summary>YZ alarmı: eşiğe ulaşınca YZ karşı saldırır ve alarm sıfırlanır.</summary>
        public double Alarm;
        public int Counterattacks;
        /// <summary>Son karşı saldırıda yeniden işgal edilen sektör (hiç yoksa ya da geri alınacak sektör yoksa null).</summary>
        public string LastCounterattack;
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
