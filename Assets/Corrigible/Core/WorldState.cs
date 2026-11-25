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

    /// <summary>Simülasyon olayları (kayda girmez). Siege (aşama 11): bekleyen karşı saldırı kuruldu (Key = sektör); sona eklendi.</summary>
    public enum SimEventType { Raid, RegionLiberated, End, Counterattack, Door, Siege }

    public sealed class SimEvent
    {
        public SimEventType Type;
        public string Key;
    }

    /// <summary>Saha uzmanı ya da yedek uzman: baskında aldığı hasar yara olarak kalır, düşerse yatar.</summary>
    [Serializable]
    public sealed class SpecialistState
    {
        /// <summary>Rol (Baskin birim türü: tek, emp, ana, maden, duman); yedek uzmanda boş, çıktığı baskında eksik rolü doldurur.</summary>
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
        /// <summary>Katılmış uzmanlar, rol sırasıyla (<see cref="Simulation.Roles"/>): Teknisyen, EMP uzmanı, Analist; atölyeyle Madenci ve Sis uzmanı.</summary>
        public List<SpecialistState> Members = new List<SpecialistState>();
        /// <summary>Son baskına Teknisyen'in yanında seçilen roller: varsayılan seçim ve dinlenme (çıkmayanlar dinlenir).</summary>
        public List<string> LastPick = new List<string>();
    }

    /// <summary>Baskına çıkan bir uzman: tahtadaki birimin türü, canı ve zırh plakası.</summary>
    [Serializable]
    public sealed class SquadSlot
    {
        public string Type;
        public int Hp, Armor;
        /// <summary>Kişi kartının sabit kimliği ve baskına kilitlenen gerçek değerleri (eski kadroda boş/sıfır).</summary>
        public string PersonId, PersonName;
        public int MaxHp, Move, Power;
        public int EmpBoosts, EmpBoostDamage;
        /// <summary>Rolün yerine çıkan yedek uzman (sığınaktaki yedek sırası); asıl uzmanda −1.</summary>
        public int Reserve = -1;
        public bool Rookie => Reserve >= 0;
        /// <summary>Tesis aşama 8: kişinin yardımcı yuvasındaki ekipman (<see cref="FacilityGear.Kinds"/>) ve baskın hakkı
        /// (<see cref="FacilityGear.RaidCharges"/>; köpek −1). Ekipmansız ve dinlenen köpekte boş/0 (JsonUtility null
        /// dizgiyi "" yazar).</summary>
        public string Gear;
        public int GearCharges;
        public FacilityTitle Titles;
    }

    /// <summary>Taktik haritayı belirleyen her şey: baskın bu parametrelerle deterministik kurulur.</summary>
    [Serializable]
    public sealed class RaidParams
    {
        /// <summary>Emirdeki sızmacı ve belirti kadro maskeleri ile dönüş turu. Sunum gizli kimliği okuyamaz.</summary>
        internal int Turncoats, Tells, FieldTurn;
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
        /// <summary>Bölge olayı (<c>Baskin.RegionEvents</c>, <see cref="RegionDef.Event"/>); boşsa olaysız. Harekât emrinde
        /// tesisin şartı (<see cref="FacilityMaps.EventFor"/>); örnek baskınlar vermez.</summary>
        public string Event;
        /// <summary>Kadro (sırasıyla giriş kareleri); boşsa tam ve sağlam üç uzman (Teknisyen, EMP uzmanı, Analist).</summary>
        public List<SquadSlot> Squad;
        /// <summary>Baskına seçilen roller (Teknisyen ve iki uzman, kadro sırasıyla): yedeği olmadığı için çıkamayan da dahil; sunum
        /// boş kalan kartı adlandırır. Boşsa kadronun türleri.</summary>
        public List<string> Roles;
        /// <summary>Harekât baskınının işareti ve tohumu (yeni oyunun tohumu, <c>S.Seed</c>); 0 = örnek/test tahtası. Core okumaz;
        /// sunum tesis odasının bina kitini bununla seçer (0 ise varsayılan kit). Silme ya da 0 yazma: tahtalar sessizce varsayılan kite düşer.</summary>
        public uint MapSeed;
        /// <summary>Tesis baskını: kanadın üç odasının kimliği, sırayla G, A, H (<see cref="FacilityMaps.RoomId"/>). Harekât emrinde hep üç
        /// oda; koddan kurulan örnek tahtada null. JsonUtility null listeyi boş yazar: tesis olup olmadığı <see cref="RaidChain.IsFacility"/>
        /// ile (Count == 3) sınanır.</summary>
        public List<string> Rooms;
        /// <summary>Makinelerin aklı: 0 temel (örnek baskın, kilitli motor testleri); 1 kampanyanın gelişmiş aklı
        /// (hedefin önünü ve kaçış yolunu tutar, ateş hattını Teknisyen'in geçeceği karelere çevirir, taşıyıcıyı kovalar).</summary>
        public int Tactics;
        /// <summary>Sunucu görevinin sertliği (dengeden, <c>liberationServerTargetHp/Machines</c>): düğüm dolabının ek canı (çekirdek
        /// hariç) ve baştaki ek makine; ikmal görevinde, savunmada ve örnek tahtada 0.</summary>
        public int TargetHpBonus, ExtraMachines;
        /// <summary>Sivil sunucunun canı (dengeden <c>liberationCivilianHp</c>); 0 ise 1.</summary>
        public int CivilianHp;
        /// <summary>Baskın başına hak (dengeden <c>liberationDynamiteCharges/SmokeCharges</c>): Madenci'nin duvar yıkan dinamiti ve
        /// Sis uzmanının sis bombası.</summary>
        public int DynamiteCharges = 2, SmokeCharges = 3;
        /// <summary>Tesis aşama 8: emir anındaki kazanımlar (<see cref="FacilityState.Perks"/>'in kopyası). Baskın
        /// içi kazanım etkileri buradan okunur: oda zinciri (<see cref="RaidChain"/>) saf kalır, sonradan seçilen kazanım süren baskını
        /// değiştirmez.</summary>
        public List<string> Perks = new List<string>();
        /// <summary>Tesis aşama 10: emir anındaki önlem kademeleri (<c>FacilityState.Counter</c>'ın kopyası, <see cref="FacilityCounters.Classes"/>
        /// sırasıyla altı öğe) ve açık cevap tarifleri (<see cref="FacilityGear.Answers"/>). Önlem defteri kapalıyken ikisi de boş. Dolu
        /// <c>Counters</c> tahtada sayımı açar (<see cref="MoveRules.Tally"/>); sonradan öğrenilen önlem ya da seçilen cevap süren baskını
        /// değiştirmez (<see cref="Perks"/> kalıbı).</summary>
        public List<int> Counters = new List<int>();
        public List<string> Answers = new List<string>();
        /// <summary>Tesis aşama 11: baskının kipi: "" saldırı, "defense" üç odalı savunma (<see cref="RaidChain.IsDefense"/>). Savunmada
        /// <see cref="MissionKind"/> sektörünki kalır (plan ve günlük okur); tahta kipi buradan okur.</summary>
        public string Mode = "";
        /// <summary>Tesis aşama 11: emir anındaki kanadın izleri (<see cref="FacilityState.Marks"/>'tan kanadın üç odası, liste sırasıyla;
        /// <see cref="Perks"/> kalıbı: sonraki onarım süren baskını değiştirmez). İzler kapalıyken boş; boş liste tahtayı değiştirmez.</summary>
        public List<FacilityMark> Marks = new List<FacilityMark>();
    }

    [Serializable]
    public sealed class RaidState
    {
        public string Team, Region;
        public int N, Seed;
        public RaidParams Params;
        /// <summary>Tesis baskınında biten odaların raporları (raid_room sırasıyla; <see cref="RoomReport.Decode"/>(<see cref="RoomReport.Encode"/>)
        /// kopyaları). Oynanan oda, yön ve taşınanlar bunlardan türetilir (<see cref="RaidChain.Next"/>, <c>Simulation.RaidRoom</c>).
        /// Örnek tahtada (oda kimliği olmayan emir) boş.</summary>
        public List<RoomReport> Reports = new List<RoomReport>();
    }

    /// <summary>Baskının harekâta dönen sonucu. Kayıtta tek satır metin olarak saklanır.</summary>
    [Serializable]
    public sealed class RaidResult
    {
        public bool Success, Retreat;
        public int Collateral, Down, Turns;
        /// <summary>Baskın bitince tahtada ayakta kalan sivil sunucu sayısı (kazanılan baskında güven geri kazandırır).</summary>
        public int CiviliansLeft;
        public string Reason;
        /// <summary>Kadronun sırasıyla kalan canı (düşen 0) ve kalan zırh plakası. Left: kaçışta süre dolarken tahtada kalan
        /// uzmanların kadro sırası (canı 0, düşmüş sayılır; <c>Down</c>'a dahil).</summary>
        public List<int> Hp = new List<int>(), Armor = new List<int>(), Left = new List<int>();
        /// <summary>Tesis baskını (aşama 6): tesisten çıkarılan düşenlerin kadro sırası (<c>RaidChain.IsExtracted</c>); tesiste yalnız
        /// bunlar yaşar, öbür düşenler ölür. Boş: kimse.</summary>
        public List<int> Extracted = new List<int>();
        /// <summary>Tesis aşama 12 (Katar): açık kenardan düşenlerin kadro sırası (oda raporunda "k"); ölmezler, yatarlar. Boş: kimse.</summary>
        public List<int> Overboard = new List<int>();
        public List<int> Turned = new List<int>(), Subdued = new List<int>();

        /// <summary>Eski kayıtlar sonda <c>Left</c> olmadan çözülür (kimse geride kalmamış). <c>Extracted</c> ya da <c>Overboard</c> doluysa
        /// <c>Left</c> boş da olsa yazılır, ardından <c>Extracted</c> ve (doluysa) <c>Overboard</c>; ikisi de boşsa dizgi eskisinin aynısı.</summary>
        public string Encode()
        {
            bool turned = Turned != null && Turned.Count > 0;
            bool over = Overboard != null && Overboard.Count > 0 || turned;
            bool extracted = Extracted != null && Extracted.Count > 0 || over;
            return $"{(Success ? 1 : 0)}|{(Retreat ? 1 : 0)}|{Collateral}|{Down}|{Turns}|{string.Join(",", Hp)}|{string.Join(",", Armor)}|{CiviliansLeft}"
                + (Left != null && Left.Count > 0 || extracted ? "|" + string.Join(",", Left ?? new List<int>()) : "")
                + (extracted ? "|" + string.Join(",", Extracted ?? new List<int>()) : "")
                + (over ? "|" + string.Join(",", Overboard ?? new List<int>()) : "")
                + (turned ? "|" + string.Join(",", Turned) + "|" + string.Join(",", Subdued ?? new List<int>()) : "");
        }

        public static RaidResult Decode(string s)
        {
            var p = s.Split('|');
            return new RaidResult
            {
                Success = p[0] == "1", Retreat = p[1] == "1",
                Collateral = int.Parse(p[2]), Down = int.Parse(p[3]), Turns = int.Parse(p[4]),
                Hp = p.Length > 5 ? Ints(p[5]) : new List<int>(), Armor = p.Length > 6 ? Ints(p[6]) : new List<int>(),
                CiviliansLeft = p.Length > 7 ? int.Parse(p[7]) : 0,
                Left = p.Length > 8 ? Ints(p[8]) : new List<int>(),
                Extracted = p.Length > 9 ? Ints(p[9]) : new List<int>(),
                Turned = p.Length > 11 ? Ints(p[11]) : new List<int>(),
                Subdued = p.Length > 12 ? Ints(p[12]) : new List<int>(),
                Overboard = p.Length > 10 ? Ints(p[10]) : new List<int>(),
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
        /// <summary>Tesis baskınının oda raporları (<see cref="RoomReport.Encode"/>, sırayla). Sunum ve bot okur.</summary>
        public List<string> Rooms = new List<string>();
        /// <summary>Tesis aşama 11: baskının kipi (<see cref="RaidParams.Mode"/>): "" saldırı, "defense" savunma. Sunumun günlük okuyucuları
        /// savunma girdisini ayırır.</summary>
        public string Mode = "";
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
        /// <summary>Son karşı saldırının günü (hiç olmadıysa −1).</summary>
        public int LastCounterattackDay = -1;
        public IncomeBreakdown LastIncome;
        public int LastIncomeDay;
        public List<EconomyTransaction> TransactionLedger = new List<EconomyTransaction>();

        public List<TeamState> Teams = new List<TeamState>();
        public RaidState Raid;
        public int Raids;
        public List<RaidLogEntry> RaidLog = new List<RaidLogEntry>();
        public LiberationCampaignState Liberation;
        /// <summary>Sığınağın kapısı: bekleyen ziyaretçi, sıradaki vuruş, bayraklar ve ziyaretler.</summary>
        public DoorState Door;

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
