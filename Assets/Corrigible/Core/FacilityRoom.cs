using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Tesis odasının tipi; değer kanattaki sırasıdır (pafta n/3).</summary>
    public enum RoomType { Entry = 1, Middle = 2, Target = 3 }

    /// <summary>
    /// Tesis odası (Docs/TESIS-BASKINLARI-TASARIMI.md): bir kanadın giriş (G), ara (A) ya da hedef (H) odasının üretimden sonra
    /// değişmeyen planı (<see cref="FacilityMaps"/>). Taktik tahta (<see cref="Baskin.BState.Room"/>) onu paylaşır; bugünkü tek
    /// tahtalı baskında null. Yapı, sabit nesneler ve şart parçaları burada durur; makineler ve kadro oda kurulurken
    /// (<see cref="Baskin.CreateRoom"/>) yerleşir.
    /// </summary>
    public sealed class FacilityRoom
    {
        /// <summary>Oda kimliği: "{sektör}.{sıra}", örneğin "weu_2.1" (1 giriş, 2 ara oda, 3 hedef oda).</summary>
        public string Id { get; internal set; }
        /// <summary>Bölge ("weu") ve sektör ("weu_2") kimliği.</summary>
        public string Region { get; internal set; }
        public string SectorId { get; internal set; }
        /// <summary>Kanat (1..3) ve kanattaki sıra (1..3, <see cref="Type"/> ile aynı).</summary>
        public int Wing { get; internal set; }
        public int Index { get; internal set; }
        public RoomType Type { get; internal set; }
        /// <summary>Kanadın görevi: "loot" (kanat 1–2) ya da "server" (kanat 3).</summary>
        public string Mission { get; internal set; }
        /// <summary>Gerçekten üretilen harita türü (<see cref="RaidMaps.Kinds"/>).</summary>
        public string MapKind { get; internal set; }
        /// <summary>Harita türü kanadın atadığı tür değil: atanan tür tükendi, yedek türe düşüldü.</summary>
        public bool KindFallback { get; internal set; }
        /// <summary>Odanın adının kaynağı (<see cref="RaidMaps"/> ad tablosundan; gösterirken <see cref="Name"/>).</summary>
        public string NameKey { get; internal set; }
        public string Name => Loc.T(NameKey);
        /// <summary>Donanım: G'de "pano" ya da "anten", A'da "vana" ya da "anten", H'de null.</summary>
        public string Device { get; internal set; }
        /// <summary>Bölge olayı (şart): A ve H'de bölgenin olayı (<see cref="Baskin.RegionEvents"/>'te varsa), G'de null.</summary>
        public string Event { get; internal set; }
        /// <summary>Geçerli odayı veren deneme sayacı (0'dan; türler boyunca sürer).</summary>
        public int Attempt { get; internal set; }

        /// <summary>Batı girişi (çıkışlar): G'de 3, A ve H'de 2 bitişik kare, y artan.</summary>
        public IReadOnlyList<Baskin.Pt> West { get; internal set; } = Array.Empty<Baskin.Pt>();
        /// <summary>
        /// Sürüm 10: odanın dönüşü (0–3). Oda kanonik düzende (giriş batıda) üretilip döndürülerek kurulur; bütün kareler (West dahil)
        /// döndürülmüş, gerçek koordinatlardadır. 0 döndürülmemiş; 1 kanonik batı güneye (x, y) → (y, 7 − x); 2 doğuya (7 − x, 7 − y);
        /// 3 kuzeye (7 − y, x). Kanonik koordinat (girişe göre "uzak yarı" gibi kurallar) <see cref="ToCanon"/> ile okunur.
        /// </summary>
        public int Turn { get; internal set; }
        /// <summary>Gerçek karenin kanonik (giriş batıda) karşılığı.</summary>
        public Baskin.Pt ToCanon(Baskin.Pt a) => ToCanon(Turn, a);
        /// <summary>Kanonik karenin gerçek karşılığı.</summary>
        public Baskin.Pt FromCanon(Baskin.Pt c) => FromCanon(Turn, c);
        public static Baskin.Pt FromCanon(int turn, Baskin.Pt c)
        {
            const int M = Baskin.N - 1;
            switch (turn & 3)
            {
                case 1: return new Baskin.Pt(c.Y, M - c.X);
                case 2: return new Baskin.Pt(M - c.X, M - c.Y);
                case 3: return new Baskin.Pt(M - c.Y, c.X);
                default: return c;
            }
        }
        public static Baskin.Pt ToCanon(int turn, Baskin.Pt a)
        {
            const int M = Baskin.N - 1;
            switch (turn & 3)
            {
                case 1: return new Baskin.Pt(M - a.Y, a.X);
                case 2: return new Baskin.Pt(M - a.X, M - a.Y);
                case 3: return new Baskin.Pt(a.Y, M - a.X);
                default: return a;
            }
        }
        /// <summary>İleri geçit: G ve A'da kenarda 2 bitişik kare; H'de boş.</summary>
        public IReadOnlyList<Baskin.Pt> Forward { get; internal set; } = Array.Empty<Baskin.Pt>();
        /// <summary>A'da menfez çiftinin iki ucu: [batı ucu, ileri uç]; G ve H'de boş.</summary>
        public IReadOnlyList<Baskin.Pt> Vents { get; internal set; } = Array.Empty<Baskin.Pt>();
        /// <summary>A'da ağır kepenk (duvar kare).</summary>
        public Baskin.Pt? Shutter { get; internal set; }
        /// <summary>A'da kepenk bölme kepengi mi (yolun üstüne duvar–kepenk–duvar; var olan duvarda aday yokken). Yalnız bölme
        /// kepenginde kapalı yol <see cref="FacilityMaps.GateClosedMaxSteps"/>'a kadar uzayabilir.</summary>
        public bool ShutterPartition { get; internal set; }
        /// <summary>A'da kepengin karşılıklı iki geçilebilir komşusu (iki kişilik işin yeri).</summary>
        public IReadOnlyList<Baskin.Pt> ShutterDoors { get; internal set; } = Array.Empty<Baskin.Pt>();
        /// <summary>Donanımın karesi (G, A), donanımın tek geçilebilir komşusu (A'da, varsa) ve hedef dolabın karesi (H).</summary>
        public Baskin.Pt? DeviceAt { get; internal set; }
        public Baskin.Pt? DeviceAccess { get; internal set; }
        public Baskin.Pt? TargetAt { get; internal set; }
        /// <summary>İkmal hedef odasında büyük sandığın duvar nişi (<see cref="Baskin.Cell.Crate"/>; yer yoksa ya da başka odada null).</summary>
        public Baskin.Pt? BigCrate { get; internal set; }
        /// <summary>Şartın ve bakım robotunun odağı: H'de hedef, G ve A'da donanım.</summary>
        public Baskin.Pt Focus { get; internal set; }
        /// <summary>Ayrılmış kareler: ileri geçit, menfez, donanım erişimi ve kepengin iki yanı (y, x sırası, tekrarsız).</summary>
        public IReadOnlyList<Baskin.Pt> Reserved { get; internal set; } = Array.Empty<Baskin.Pt>();

        /// <summary>Şartsız tahtanın dökümü (serpiştirme, kepenk ve menfez sonrası; karakter sözlüğü <see cref="FacilityMaps"/>).</summary>
        public IReadOnlyList<string> Rows { get; internal set; } = Array.Empty<string>();
        /// <summary>Şartın en yüksek kademedeki parçaları; her biri görüneceği en düşük kademeyi taşır. Şartsız odada boş.</summary>
        public IReadOnlyList<PlanPart> Parts { get; internal set; } = Array.Empty<PlanPart>();
        /// <summary>Kademede görünen parçalar (1–2. kademe bir, 3. kademe ve üstü öbür hâl).</summary>
        public IEnumerable<PlanPart> PartsAt(int tier) => Parts.Where(p => p.MinTier <= TierKey(tier));

        /// <summary>Kademedeki fiziksel parçaların kareleri (y*8+x): makine ve kadro bu karelerde başlamaz.</summary>
        public IReadOnlyCollection<int> PartTiles(int tier)
        {
            var o = new HashSet<int>();
            foreach (var p in PartsAt(tier)) if (FacilityMaps.IsPhysicalPart(p.Kind)) o.Add(p.At.Y * Baskin.N + p.At.X);
            return o;
        }

        static int TierKey(int tier) => tier >= 3 ? 3 : 1;

        /// <summary>
        /// Bina planının tahtası (aşama 5): şartlıysa kademedeki hâli (<paramref name="events"/> = bölge olayları açık ve şart var),
        /// değilse şartsız tahta; baskın emrinin <c>RoomBoard</c>'da kurduğu hücrelerin aynısı. Robot, niyet ve uyanma karesi
        /// yoktur (baskın tohumundan gelir). Kopya döner: okuyan değiştirse de oda bozulmaz.
        /// </summary>
        public Baskin.Cell[,] PlanCells(int tier, bool events)
        {
            var src = !events || Event == null ? BaseCells : tier >= 3 ? Cells3 : Cells1;
            if (src == null) return null;
            var o = new Baskin.Cell[Baskin.N, Baskin.N];
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++) o[y, x] = src[y, x].Clone();
            return o;
        }

        /// <summary>Giydirilmiş taslak (serpiştirme öncesi): olay kurulumu (tavan) ve testler okur.</summary>
        internal string[] DraftRows;
        /// <summary>Kasa odası türünün iç kareleri (y*8+x); öbür türlerde boş.</summary>
        internal HashSet<int> Inner = new HashSet<int>();
        /// <summary>Şartsız tahta; kademe 1–2; kademe 3 ve üstü (şart tüp ya da tavan değilse Cells1 ile Cells3 aynı başvuru).</summary>
        internal Baskin.Cell[,] BaseCells, Cells1, Cells3;
        /// <summary>Aynı iki hâlin olay durumu; şartsız odada null.</summary>
        internal Baskin.EvState Ev1, Ev3;
    }

    /// <summary>
    /// Şartın bir parçası: türü (meme, raf, catlak, tup, hol, turnike…), karesi, turnike kenarının öbür karesi, yönü (tüpte uçuş
    /// yönü, kolda ilk kaynak yönü) ve görüneceği en düşük kademe.
    /// </summary>
    public sealed class PlanPart
    {
        public string Kind;
        public Baskin.Pt At;
        public Baskin.Pt? To;
        public int Dir = -1;
        public int MinTier = 1;
    }

    /// <summary>
    /// Odanın baskın değerleri: oda tohumu, tur, başlangıç makinesi ve donanım canı (dengeden, <see cref="FacilityMaps.Setup"/>);
    /// oda zincirinde ayrıca taşınanlar, hazırlık ve dönüş (raporlardan, <c>RaidChain.Setup</c>). Ekler varsayılanındayken kurulum
    /// aşama 1'in aynısıdır.
    /// </summary>
    public sealed class RoomSetup
    {
        public int Seed, Turns, Machines, DeviceHp;
        /// <summary>Dönüş ziyareti: ekip doğu geçidinden girer, kaçış baştan açık; <see cref="Frozen"/> odanın son raporu.</summary>
        public bool Return;
        public RoomReport Frozen;
        /// <summary>Odaya giren kadro (önceki odanın raporu, kadro sırasıyla); null: emirdeki kadro (ilk oda).</summary>
        public IReadOnlyList<RoomMember> Carry;
        /// <summary>İkmal yükü (<see cref="RoomReport.Cargo"/> anlamıyla): k ≥ 0 ise o kadro sırasındaki birim taşıyıcıdır.</summary>
        public int Cargo = -1;
        /// <summary>Hazırlık (hedef odası, gidiş): uyanmasız tur (pano), eksik makine (anten), hedef dolabın eksik canı (vana, sunucu),
        /// makinelerin yürümediği tur (vana, ikmal). Dönüşte <see cref="NoWake"/>: kopya dolapları uyandırmaz (anten).</summary>
        public int QuietTurns, MachinesLess, TargetHpLess, StillTurns;
        public bool NoWake;
        /// <summary>Tesis hamlelerinin değerleri (dengeden, <see cref="FacilityMaps.Setup"/>; oda zincirinde kazanımlarla,
        /// <c>RaidChain.Setup</c>); null ise varsayılanlar.</summary>
        public MoveRules Moves;
        /// <summary>Tesis aşama 8: önceki odanın raporundan ekiple gelen birimler (köpekler hep, ayaktaki tutsaklar; aygıtlar asla) ve
        /// yükü köpek taşıyor mu (<see cref="Cargo"/> sahibin kadro sırası). İlk odada null/false.</summary>
        public IReadOnlyList<RoomUnit> CarryUnits;
        public bool CargoDog;
        /// <summary>Tesis aşama 11: odanın emirdeki izleri (<see cref="RaidParams.Marks"/>'tan bu odanınkiler, liste sırasıyla; ilk ziyaret
        /// ve savunma tahtası uygular, <see cref="FacilityMarks.Apply"/>). Null ya da boş: izsiz tahta (aşama 10'un aynısı).</summary>
        public IReadOnlyList<FacilityMark> Marks;
        /// <summary>
        /// Tesis aşama 11, üç odalı savunma (<c>RaidChain.SetupDefense</c>; <see cref="Baskin.CreateRoom"/> buna göre savunma tahtasını kurar):
        /// savunma odası mı; odanın başlangıç turu (tek sayaç: önceki odanın son turu + 1; <see cref="Turns"/> toplam süredir); önceki
        /// odanın ayaktaki makineleri (takipçiler, başlangıç turu + 1'de gelir); terk edilen oda sayısı; saha rölesinin canı (yalnız hedef
        /// odasında kullanılır); bu odaya düşen dalgalar (<see cref="Baskin.DefenseSchedule"/>; gecikme dahil). Varsayılanlar örnek tahtayı
        /// değiştirmez.
        /// </summary>
        public bool Defense;
        public int StartTurn = 1;
        public IReadOnlyList<RoomMachine> Followers;
        public int Abandoned;
        public int RelayHp;
        public IReadOnlyList<Baskin.DefenseWave> Waves;
    }

    /// <summary>
    /// Harekâtın 108 tesis odası (12 bölge × 3 kanat × 3 oda). Bölgeler istendikçe, her biri kendi kilidiyle üretilir; bölge akışları
    /// birbirinden bağımsız olduğu için üretim sırası sonucu değiştirmez. Salt okunurdur ve paylaşılır.
    /// </summary>
    public sealed class FacilityPlan
    {
        public uint Seed { get; }
        /// <summary>Tesis aşama 12: planın şart eşlemesi (<see cref="FacilityMaps.EventFor"/>'un iki alt bayrağı; önbellek anahtarı). Digest'e
        /// girmez, serileşmez.</summary>
        public bool Katar { get; }
        public bool Yalpa { get; }
        readonly uint root;
        // Bölge dizini → kilit ve üretilmiş odalar (dizi elemanı ayrı ayrı yazılır; sözlük kurulumdan sonra değişmez)
        readonly Dictionary<string, int> slot = new Dictionary<string, int>();
        readonly object[] locks;
        readonly List<FacilityRoom>[] regions;
        List<FacilityRoom> all;

        internal FacilityPlan(uint seed, bool katar = true, bool yalpa = true)
        {
            Seed = seed;
            Katar = katar;
            Yalpa = yalpa;
            root = FacilityMaps.Root(seed);
            locks = new object[GameData.Regions.Length];
            regions = new List<FacilityRoom>[GameData.Regions.Length];
            for (int i = 0; i < GameData.Regions.Length; i++) { slot[GameData.Regions[i].Id] = i; locks[i] = new object(); }
        }

        /// <summary>Odanın planı; kimlik çözülmezse ya da bölge yoksa null.</summary>
        public FacilityRoom Room(string roomId) =>
            FacilityMaps.ParseRoomId(roomId, out var sector, out int index) ? Room(sector, index) : null;

        /// <summary>Sektörün <paramref name="index"/>. odası (1 G, 2 A, 3 H); yoksa null.</summary>
        public FacilityRoom Room(string sectorId, int index)
        {
            var wing = Wing(sectorId);
            return wing != null && index >= 1 && index <= wing.Count ? wing[index - 1] : null;
        }

        /// <summary>Sektörün (kanadın) üç odası: G, A, H; yoksa null.</summary>
        public IReadOnlyList<FacilityRoom> Wing(string sectorId)
        {
            int cut = sectorId?.LastIndexOf('_') ?? -1;
            if (cut <= 0) return null;
            var rooms = Region(sectorId.Substring(0, cut));
            if (rooms == null) return null;
            var wing = rooms.Where(r => r.SectorId == sectorId).ToList();
            return wing.Count == 3 ? wing : null;
        }

        /// <summary>Bölgenin 9 odası (kanat × oda sırasıyla); bölge yoksa null.</summary>
        public IReadOnlyList<FacilityRoom> Region(string regionId)
        {
            if (regionId == null || !slot.TryGetValue(regionId, out int i)) return null;
            lock (locks[i])
            {
                if (regions[i] == null) regions[i] = FacilityMaps.GenerateRegion(root, GameData.Regions[i], Katar, Yalpa);
                return regions[i];
            }
        }

        /// <summary>Bütün odalar: <see cref="GameData.Regions"/> sırası × kanat × oda (hepsini üretir).</summary>
        public IReadOnlyList<FacilityRoom> Rooms
        {
            get
            {
                var a = all;
                if (a != null) return a;
                a = GameData.Regions.SelectMany(def => Region(def.Id)).ToList();
                all = a;
                return a;
            }
        }
    }
}
