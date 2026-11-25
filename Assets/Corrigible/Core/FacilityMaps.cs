using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis odalarının üreticisi (Docs/TESIS-BASKINLARI-TASARIMI.md): harekâtın 108 odası (12 bölge × 3 kanat × giriş, ara, hedef
    /// oda). Oda, <see cref="RaidMaps.Generate(string, Rng)"/>'in çıktısının oda tipine göre giydirilmesidir (<see cref="Dress"/>):
    /// robotlar kalkar, A ve H'de batı girişi 2 kareye iner, G ve A'da hedef dolabın yerine donanım ve doğu yarının kenarında
    /// ileri geçit gelir. Plan tahtası, bölge özelliği, kepenk, menfez ve şart <see cref="Baskin.BuildRoom"/>'dadır.
    /// Kendi akışlarını kullanır (harekât tohumundan <see cref="Rng.Mix"/> ile): <c>S.Rng</c>'den tüketmez, kayda yazılmaz.
    /// Kanat içinde üç tür farklıdır; rehine türü yalnız hedef odada olur. Türün bütün denemeleri tükenirse yedek türlere geçilir.
    /// <para>Satır sözlüğü (taslak ve <see cref="FacilityRoom.Rows"/>): '.' zemin, '#' duvar, '~' kablo, 'E' batı girişi, 'F' ileri
    /// geçit, 'o' menfez ucu, '=' ağır kepenk, 'C' hedef dolap, 'P' sigorta panosu, 'R' çağrı anteni, 'V' soğutma vanası, 'S' sivil,
    /// 'K' soğutma ünitesi, 'U' kopya dolabı. Robot harfi yoktur.</para>
    /// Üretim değişirse (RaidMaps kurucuları, giydirme, tuzlar, şart kurulumu) <see cref="GeneratorVersion"/> artar.
    /// </summary>
    public static class FacilityMaps
    {
        /// <summary>Tesis planının ve tesis kurallarının sürümü (kayıttaki iz "f" + sürüm). 2 (aşama 6): plan büyük sandığın nişini
        /// taşır; oda raporu ve baskın sonucu düşen kişiyi (gövde, çıkarılma) taşır ve sonucu değiştirir. 3 (aşama 8): ekipman, sunucu
        /// ödülü (server_reward; bekleyen ödülde baskın emri reddedilir) ve tesis kipinde üretim kuralları (açılan tarifler, erzak bedeli,
        /// emir açıkken eksik kit); plan üretimi aynı (Digest değişmedi). 4 (aşama 10): önlem defteri (oda raporunda sayaç kuyruğu ve sökük
        /// makine parçası, baskın sonunda sayım ve öğrenme, önlemli tahtalar, cevap tarifinin sayfası ve taslak emrin iptali); plan üretimi
        /// aynı (Digest değişmedi, kaplama oda üretimine girmez). 5 (aşama 11): kalıcı tahta (izler, YZ onarımı, izli tahtalar ve plan),
        /// eşikte bekleyen karşı saldırı (kuşatma; siege_defend, siege_yield), üç odalı savunma ve yeniden alınan bölgenin fonu ve güveninin
        /// bir kez verilmesi; plan üretimi aynı (Digest değişmedi). 6 (aşama 12): na ve oc tesis şartı Katar ve Yalpa (iki alt bayrakla;
        /// ikisi kapalıyken plan aşama 11'inki), Katar'da ileri geçit doğu sütununda; oda raporu kenardan düşen kişiyi ("k"), takibi ve AYIR'ı,
        /// baskın sonucu kenardan düşenleri taşır. 7: imza makineleri. 8: kişisel işler ve kazanılan unvanlar. 9: sahadaki sızmacı.
        /// 10: ileri geçit her tesiste doğu sütununda, A ve H'nin batı girişi kanadın önceki odasının ileri geçidiyle aynı satırlarda
        /// (geçit ile giriş simetrik); giriş ya da ara odada süre dolarken kapıdakiler geçer, kapıya varamayan geri çekilir ("w");
        /// ikmal kasasını her uzman açar.</summary>
        public const int GeneratorVersion = 10;
        /// <summary>İleri geçidin iki karesinin girişe en çok yürüyüş uzaklığı (kepenk kapalı).</summary>
        public const int GateMaxSteps = 8;
        /// <summary>
        /// Bölme kepengi olan A odasında kepenk kapalıyken ileri geçidin girişe en çok yürüyüş uzaklığı (açıkken
        /// <see cref="GateMaxSteps"/>). Gevşetme (§9.2/1'in ötesi; geliştirici onayladı, 2026-11-18): bölmede de kapalıyken 8 adım istenince
        /// kepenk taslakların yalnız %2,4'ünde yer buluyordu (geçit girişe 7–8 adımken 2 adımlık kestirme sığmaz). Var olan duvardan
        /// kepenkte kapalı sınır <see cref="GateMaxSteps"/> kalır (şart parçaları da yolu onun ötesine uzatamaz).
        /// </summary>
        public const int GateClosedMaxSteps = GateMaxSteps + 2;
        /// <summary>Kepenk açılınca ileri geçide yolun en az kısalması.</summary>
        public const int ShutterSaving = 2;
        /// <summary>Menfezin iki ucu arasındaki en az yürüyüş uzaklığı.</summary>
        public const int VentMinWalk = 5;
        /// <summary>Tür başına yerleşim denemesi.</summary>
        public const int AttemptsPerKind = 120;
        /// <summary>Planda en az bakım robotu ve dron yeri (başlangıç makineleri yer bulsun).</summary>
        public const int MinMaintSpots = 1, MinDroneSpots = 3;

        const int N = Baskin.N;

        // Akışların tuzları (ASCII etiketleri): kök "TESI", ad "NAM", kanat "WI", kanat seçimi "KIND", oda "ROO", yerleşim "L",
        // serpiştirme "T", yapı "S"
        const uint SaltRoot = 0x54455349u, SaltNames = 0x4E414D00u, SaltWing = 0x57490000u, SaltWingPick = 0x4B494E44u,
            SaltRoom = 0x524F4F00u, SaltLayout = 0x4C000000u, SaltTraits = 0x54000000u, SaltStructure = 0x53000000u;

        /// <summary>Kanadın donanımları (giriş, ara oda): tek çekim, eşit olasılık; aynı kanatta anten iki kez gelmez.</summary>
        public static readonly (string Entry, string Middle)[] DeviceTriples = { ("pano", "vana"), ("pano", "anten"), ("anten", "vana") };

        /// <summary>Duvar ve kablo ekleyen bölge özellikleri her odada işler; nesne ekleyenler (soğutma, kalabalık, fabrika) yalnız
        /// hedef odada.</summary>
        static readonly HashSet<string> StructureTraits = new HashSet<string> { "dar", "acik", "sebeke" };

        /// <summary>Kareye oturan şart parçaları (makine ve kadro bu karelerde başlamaz).</summary>
        static readonly HashSet<string> PhysicalKinds = new HashSet<string> { "meme", "raf", "sizinti", "catlak", "durak", "kol", "suzgec", "tup", "hortum" };

        public static bool IsPhysicalPart(string kind) => kind != null && PhysicalKinds.Contains(kind);

        // ---------- Kimlik ----------
        /// <summary>Oda kimliği: "{sektör}.{sıra}" ("weu_2.1"); nokta yüzünden sektör kimliği gibi çözülmez.</summary>
        public static string RoomId(string sectorId, int index) => sectorId + "." + index.ToString(CultureInfo.InvariantCulture);

        /// <summary>Oda kimliğini son noktadan böler; sıra 1..3 değilse false.</summary>
        public static bool ParseRoomId(string id, out string sectorId, out int index)
        {
            sectorId = null; index = 0;
            int cut = id?.LastIndexOf('.') ?? -1;
            if (cut <= 0 || !int.TryParse(id.Substring(cut + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n < 1 || n > 3) return false;
            sectorId = id.Substring(0, cut); index = n;
            return true;
        }

        /// <summary>32 bit FNV-1a (UTF-16 kod birimleri): bölge akışının tuzu ve testlerin özeti (string.GetHashCode değil).</summary>
        internal static uint Fnv1a32(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char ch in s) { h ^= ch; h *= 16777619u; }
                return h;
            }
        }

        internal static uint Root(uint campaignSeed) => Rng.Mix(campaignSeed, SaltRoot);

        // ---------- Önbellek ----------
        static readonly object cacheLock = new object();
        static FacilityPlan cached;

        /// <summary>
        /// Harekâtın planı: son istenen planı önbellekte tutulur (bölgeler istendikçe üretilir). Anahtar tohum ve aşama 12'nin iki alt
        /// bayrağıdır (<see cref="Balance.facilityKatar"/>, <see cref="Balance.facilityYalpa"/>; <see cref="EventFor"/>): ikisi kapalıyken
        /// plan aşama 11'inkidir. Varsayılanlar (true) yeni şartlardır.
        /// </summary>
        public static FacilityPlan For(uint campaignSeed, bool katar = true, bool yalpa = true)
        {
            lock (cacheLock)
            {
                if (cached == null || cached.Seed != campaignSeed || cached.Katar != katar || cached.Yalpa != yalpa)
                    cached = new FacilityPlan(campaignSeed, katar, yalpa);
                return cached;
            }
        }

        /// <summary>
        /// Tesis aşama 12, bölgenin tesis şartı (tek okuyucu: plan, baskın emri ve sunum): <see cref="RegionDef.FacilityEvent"/> "katar"
        /// ise <paramref name="katar"/> açıkken "katar", "yalpa" ise <paramref name="yalpa"/> açıkken "yalpa"; öbür hâllerde
        /// <see cref="RegionDef.Event"/>. Saf.
        /// </summary>
        public static string EventFor(RegionDef def, bool katar, bool yalpa)
        {
            if (def == null) return null;
            if (def.FacilityEvent == "katar") return katar ? "katar" : def.Event;
            if (def.FacilityEvent == "yalpa") return yalpa ? "yalpa" : def.Event;
            return def.Event;
        }

        /// <summary>Yalnız testler: önbelleği boşaltır.</summary>
        internal static void ClearCache() { lock (cacheLock) cached = null; }

        /// <summary>Yalnız ölçüm: her reddedilen denemede (oda, tür, ret kodu) çağrılır.</summary>
        internal static Action<string, string, string> RejectHook;

        // ---------- Bölge ve kanat ----------
        sealed class WingPick
        {
            public RegionDef Def;
            public int Wing;
            public uint Seed;
            public string SectorId;
            public string[] Order;
            public string H, G, A, EntryDevice, MiddleDevice;
        }

        static WingPick PickWing(uint region, RegionDef def, int w)
        {
            uint wing = Rng.Mix(region, SaltWing + (uint)w);
            var pick = new Rng(Rng.Mix(wing, SaltWingPick));
            var order = RaidMaps.Kinds.ToArray();
            for (int i = order.Length - 1; i > 0; i--) { int j = (int)Math.Floor(pick.Next() * (i + 1)); (order[i], order[j]) = (order[j], order[i]); }
            var dev = DeviceTriples[(int)Math.Floor(pick.Next() * DeviceTriples.Length)];
            string h = order[0];
            string g = order.First(k => k != h && k != RaidMaps.Hostage);
            string a = order.First(k => k != h && k != g && k != RaidMaps.Hostage);
            return new WingPick
            {
                Def = def, Wing = w, Seed = wing, SectorId = def.Id + "_" + w.ToString(CultureInfo.InvariantCulture), Order = order,
                H = h, G = g, A = a, EntryDevice = dev.Entry, MiddleDevice = dev.Middle,
            };
        }

        static readonly string[] trainRooms =
        {
            Loc.N("Lokomotif sahanlığı"), Loc.N("Yük vagonu geçidi"), Loc.N("Ön ikmal vagonu"),
            Loc.N("Servis platformu"), Loc.N("Orta vagon geçidi"), Loc.N("Arka ikmal vagonu"),
            Loc.N("Fren vagonu sahanlığı"), Loc.N("Kablo vagonu"), Loc.N("Kumanda vagonu"),
        };

        static readonly string[] platformRooms =
        {
            Loc.N("İskele girişi"), Loc.N("Alt güverte geçidi"), Loc.N("İskele ambarı"),
            Loc.N("Servis merdiveni"), Loc.N("Ana güverte geçidi"), Loc.N("Güverte ambarı"),
            Loc.N("Kumanda sahanlığı"), Loc.N("Kablo güvertesi"), Loc.N("Platform kumanda odası"),
        };

        /// <summary>Bölgenin 9 odası (kanat × oda sırasıyla) ve adları. Bölgenin akışı yalnız kampanya tohumuna ve bölge kimliğine bağlı.</summary>
        /// <remarks>Aşama 12: şart <see cref="EventFor"/>'dan (<paramref name="katar"/>, <paramref name="yalpa"/>); Katar tesisinde G ve A'nın
        /// ileri geçidi yalnız doğu sütunundadır (vagonlar arası bağlantı; kuzey ve güney kenarı açık).</remarks>
        internal static List<FacilityRoom> GenerateRegion(uint root, RegionDef def, bool katar = true, bool yalpa = true)
        {
            uint region = Rng.Mix(root, Fnv1a32(def.Id));
            string ev = EventFor(def, katar, yalpa);
            bool noCorners = ev == "katar";
            // Sürüm 10: Katar ve Yalpa odaları döndürülmez (açık kenar kuzey ve güneyde, yatış batı ve doğuya): ileri geçit doğuda
            bool eastOnly = noCorners || ev == "yalpa";
            var rooms = new List<FacilityRoom>();
            for (int w = 1; w <= 3; w++)
            {
                var wp = PickWing(region, def, w);
                // Sürüm 10: A ve H'nin girişi kanadın önceki odasının ileri geçidine simetrik (karşı kenarda, aynı kareler; G'nin girişi
                // batıda ve serbest). Oda kanonik düzende (giriş batıda) üretilir ve geçidin kenarına göre döndürülür (FacilityRoom.Turn).
                // Bir oda önceki odanın geçidine uyamazsa önceki oda sonraki denemesinden yeniden üretilir (geri izleme; deterministik)
                var wing = new FacilityRoom[4];
                var from = new int[4];
                int at = 1, backs = 0;
                while (at <= 3)
                {
                    int turn = 0;
                    int? entryTop = at == 1 ? (int?)null : EntryFor(wing[at - 1].Forward, out turn);
                    var whys = new Dictionary<string, int>();
                    var room = GenerateRoom(wp, (RoomType)at, ev, noCorners, entryTop, from[at], whys, turn, eastOnly);
                    if (room != null) { wing[at++] = room; if (at <= 3) from[at] = 0; continue; }
                    if (at == 1 || ++backs > MaxBacktracks)
                        throw new InvalidOperationException("Tesis odası üretilemedi: " + RoomId(wp.SectorId, at) + " (giriş "
                            + (entryTop?.ToString(CultureInfo.InvariantCulture) ?? "serbest") + "; geri izleme " + backs.ToString(CultureInfo.InvariantCulture) + "; "
                            + string.Join(",", whys.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + ":" + kv.Value.ToString(CultureInfo.InvariantCulture))) + ")");
                    at--;
                    from[at] = wing[at].Attempt + 1;
                }
                rooms.Add(wing[1]); rooms.Add(wing[2]); rooms.Add(wing[3]);
            }
            var themedNames = ev == "katar" ? trainRooms : ev == "yalpa" ? platformRooms : null;
            if (themedNames != null)
            {
                for (int i = 0; i < rooms.Count; i++) rooms[i].NameKey = themedNames[i];
                return rooms;
            }
            // Adlar: her türün beş adı bölgenin kendi karışımıyla; oda adı gerçek türden gelir, bölgede tekrar etmez
            var names = new Dictionary<string, string[]>();
            for (int k = 0; k < RaidMaps.Kinds.Length; k++)
            {
                var list = RaidMaps.Names[RaidMaps.Kinds[k]].ToArray();
                var r = new Rng(Rng.Mix(region, SaltNames + (uint)k));
                for (int i = list.Length - 1; i > 0; i--) { int j = (int)Math.Floor(r.Next() * (i + 1)); (list[i], list[j]) = (list[j], list[i]); }
                names[RaidMaps.Kinds[k]] = list;
            }
            var used = RaidMaps.Kinds.ToDictionary(k => k, k => 0);
            foreach (var room in rooms)
            {
                var list = names[room.MapKind];
                room.NameKey = list[used[room.MapKind]++ % list.Length];
            }
            return rooms;
        }

        /// <summary>Odanın şartı: A ve H'de bölgenin tesis şartı (<paramref name="ev"/>, <see cref="EventFor"/>; olay tablosunda varsa), G'de yok.</summary>
        static string RoomEvent(string ev, RoomType type) =>
            type == RoomType.Entry || string.IsNullOrEmpty(ev) || !Baskin.RegionEvents.ContainsKey(ev) ? null : ev;

        /// <summary>Atanan tür, sonra yedek türler (kanadın sırasıyla); her türde <see cref="AttemptsPerKind"/> deneme. Tükenirse istisna.
        /// <paramref name="noCorners"/>: Katar tesisi (aşama 12), ileri geçit doğu sütununun köşelerine değmez. <paramref name="entryTop"/>:
        /// A ve H'de batı girişinin üst satırı (önceki odanın ileri geçidinin üst satırı; sürüm 10).</summary>
        /// <paramref name="from"/>: ilk deneme sayacı (geri izlemede bulunan yerleşimin sonrasından sürer; deneme tohumu sayaçtan gelir).
        /// Tükenirse null (<paramref name="whys"/> ret kodlarını sayar).
        static FacilityRoom GenerateRoom(WingPick wp, RoomType type, string ev, bool noCorners, int? entryTop = null, int from = 0,
            Dictionary<string, int> whys = null, int turn = 0, bool eastOnly = false)
        {
            string assigned = type == RoomType.Entry ? wp.G : type == RoomType.Middle ? wp.A : wp.H;
            var kinds = new List<string> { assigned };
            kinds.AddRange(wp.Order.Where(k => k != wp.H && k != wp.G && k != wp.A && (type == RoomType.Target || k != RaidMaps.Hostage)));
            int a = 0;
            foreach (var kind in kinds)
            {
                if (a + AttemptsPerKind <= from) { a += AttemptsPerKind; continue; }
                int skip = Math.Max(0, from - a);
                a += skip;
                var fr = TryKind(wp, type, kind, assigned, RoomEvent(ev, type), ref a, AttemptsPerKind - skip, whys, noCorners, entryTop, turn, eastOnly);
                if (fr != null) return fr;
            }
            return null;
        }

        /// <summary>Sürüm 10: bir kanadın geri izleme sınırı (aşılırsa üretim istisna atar).</summary>
        const int MaxBacktracks = 64;

        /// <summary>
        /// Sürüm 10: önceki odanın (gerçek koordinatlı) ileri geçidine simetrik giriş: sonraki odanın dönüşü (<paramref name="turn"/>,
        /// <see cref="FacilityRoom.Turn"/>) ve kanonik girişin üst satırı. Doğu geçidi → batı girişi (dönüş 0), kuzey → güney (1), batı →
        /// doğu (2), güney → kuzey (3); giriş geçidin iki karesiyle aynı sütun ya da satırlarda.
        /// </summary>
        internal static int EntryFor(IReadOnlyList<Baskin.Pt> forward, out int turn)
        {
            if (forward.All(f => f.X == N - 1)) { turn = 0; return forward.Min(f => f.Y); }
            if (forward.All(f => f.X == 0)) { turn = 2; return N - 2 - forward.Min(f => f.Y); }
            if (forward.All(f => f.Y == 0)) { turn = 1; return forward.Min(f => f.X); }
            if (forward.All(f => f.Y == N - 1)) { turn = 3; return N - 2 - forward.Min(f => f.X); }
            throw new InvalidOperationException("İleri geçit bir kenarda değil");
        }

        /// <summary>Bir türün denemeleri; <paramref name="a"/> türler boyunca süren deneme sayacı (her denemenin tohumu).</summary>
        static FacilityRoom TryKind(WingPick wp, RoomType type, string kind, string assigned, string ev, ref int a, int attempts, Dictionary<string, int> whys,
            bool noCorners = false, int? entryTop = null, int turn = 0, bool eastOnly = false)
        {
            uint room = Rng.Mix(wp.Seed, SaltRoom + (uint)(int)type);
            string id = RoomId(wp.SectorId, (int)type);
            string device = type == RoomType.Entry ? wp.EntryDevice : type == RoomType.Middle ? wp.MiddleDevice : null;
            var traits = type == RoomType.Target || StructureTraits.Contains(wp.Def.Trait) ? new List<string> { wp.Def.Trait } : new List<string>();
            for (int n = 0; n < attempts; n++, a++)
            {
                var r = new Rng(Rng.Mix(room, SaltLayout + (uint)a));
                var inner = new HashSet<(int x, int y)>();
                string why;
                // Sürüm 10: A ve H'de ham girişin üç karesi önceki odanın ileri geçidini kapsar (üst satır 6 ise 5–7, giydirme 5'i kapatır)
                var raw = RaidMaps.Generate(kind, r, inner, entryTop is int top ? Math.Min(top, N - 3) : (int?)null);
                if (raw == null) { Reject(whys, id, kind, "uretim"); continue; }
                var g = Dress(raw, type, device, inner, r, out why, noCorners, entryTop, eastOnly || noCorners);
                if (g == null) { Reject(whys, id, kind, why); continue; }
                why = CheckDraft(g, type, kind, inner);
                if (why != null) { Reject(whys, id, kind, why); continue; }
                // Sürüm 10: denetlenen kanonik taslak geçidin kenarına göre döndürülür; oda döndürülmüş kareler üstünde kurulur
                if (turn != 0)
                {
                    var rg = new char[N, N];
                    for (int y = 0; y < N; y++)
                        for (int x = 0; x < N; x++) { var q = FacilityRoom.FromCanon(turn, new Baskin.Pt(x, y)); rg[q.Y, q.X] = g[y, x]; }
                    g = rg;
                    inner = new HashSet<(int x, int y)>(inner.Select(t => { var q = FacilityRoom.FromCanon(turn, new Baskin.Pt(t.x, t.y)); return (q.X, q.Y); }));
                }
                var draft = new Baskin.RoomDraft
                {
                    Id = id, Region = wp.Def.Id, SectorId = wp.SectorId, Wing = wp.Wing, Index = (int)type, Type = type, MapKind = kind,
                    Device = device, Event = ev, Mission = wp.Wing <= 2 ? "loot" : "server", Traits = new List<string>(traits),
                    Rows = Rows(g), Inner = new HashSet<int>(inner.Select(t => t.y * N + t.x)), Turn = turn,
                };
                var fr = Baskin.BuildRoom(draft, unchecked((int)Rng.Mix(room, SaltTraits + (uint)a)), new Rng(Rng.Mix(room, SaltStructure + (uint)a)), out why);
                if (fr == null) { Reject(whys, id, kind, why); continue; }
                fr.Attempt = a;
                fr.KindFallback = kind != assigned;
                return fr;
            }
            return null;
        }

        static void Reject(Dictionary<string, int> whys, string id, string kind, string why)
        {
            why = why ?? "?";
            if (whys != null) whys[why] = (whys.TryGetValue(why, out int n) ? n : 0) + 1;
            RejectHook?.Invoke(id, kind, why);
        }

        static string[] Rows(char[,] g) => Enumerable.Range(0, N).Select(y => new string(Enumerable.Range(0, N).Select(x => g[y, x]).ToArray())).ToArray();

        // ---------- Giydirme ----------
        static char DeviceLetter(string device) => device == "pano" ? 'P' : device == "anten" ? 'R' : device == "vana" ? 'V' : '?';
        static bool IsDeviceLetter(char ch) => ch == 'P' || ch == 'R' || ch == 'V';
        static bool In(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;
        static int Man((int x, int y) a, (int x, int y) b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        static bool Edge((int x, int y) t) => t.x == 0 || t.x == N - 1 || t.y == 0 || t.y == N - 1;

        static List<(int x, int y)> All(char[,] g, Func<char, bool> pick)
        {
            var o = new List<(int x, int y)>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (pick(g[y, x])) o.Add((x, y));
            return o;
        }

        /// <summary>Harf düzeyinde girişten yürüyüş uzaklığı (yürünür: '.', '~', 'E', 'F'; ulaşılamayan −1).</summary>
        static int[,] LetterSteps(char[,] g)
        {
            var dist = new int[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) dist[y, x] = -1;
            var q = new Queue<(int x, int y)>();
            foreach (var e in All(g, ch => ch == 'E')) { dist[e.y, e.x] = 0; q.Enqueue(e); }
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                foreach (var d in Baskin.Dirs)
                {
                    int nx = x + d[0], ny = y + d[1];
                    if (!In(nx, ny) || dist[ny, nx] >= 0 || !Walk(g[ny, nx])) continue;
                    dist[ny, nx] = dist[y, x] + 1; q.Enqueue((nx, ny));
                }
            }
            return dist;
        }

        static bool Walk(char ch) => ch == '.' || ch == '~' || ch == 'E' || ch == 'F';

        /// <summary>
        /// Harita türünün geçerli satırlarını oda tipine giydirir (bütün çekimler <paramref name="r"/>'den, sırayla): robotlar kalkar;
        /// A ve H'de batı girişinin bir ucu kapanır; G'de soğutma ve kopya dolapları, A'da soğutma kalkar, G ve A'da hedef dolabın
        /// yerine donanım gelir; G ve A'da ileri geçit (sürüm 10: yalnız doğu sütununda, girişten en çok <see cref="GateMaxSteps"/>
        /// adım, donanımın 3×3'ünün dışında); A'da geçide yakın kopya dolabı kalkar; siviller 1/2/3'e iner (önce geçidin yanındakiler,
        /// sonra kenardakiler). Olmazsa null ve <paramref name="why"/>. <paramref name="noCorners"/> (aşama 12, Katar tesisi): ileri
        /// geçit kuzey ve güney satırına (açık kenar) değmez. <paramref name="entryTop"/> (sürüm 10): A ve H'de batı girişinin iki karesi
        /// bu satırdan başlar (önceki odanın ileri geçidiyle simetrik); ham girişin üç karesi bunu kapsamıyorsa "ayna".
        /// </summary>
        internal static char[,] Dress(string[] raw, RoomType type, string device, HashSet<(int x, int y)> inner, Rng r, out string why, bool noCorners = false,
            int? entryTop = null, bool eastOnly = false)
        {
            why = null;
            var g = new char[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) g[y, x] = raw[y][x];
            // 1) Robotlar
            foreach (var t in All(g, ch => ch == 'd' || ch == 'm')) g[t.y, t.x] = '.';
            // 2) Batı girişi: A ve H'de iki bitişik kare
            if (type != RoomType.Entry)
            {
                int ey = All(g, ch => ch == 'E').Min(t => t.y);
                bool closeTop = r.Next() < 0.5;
                if (entryTop is int top)
                {
                    if (top == ey) closeTop = false;
                    else if (top == ey + 1) closeTop = true;
                    else { why = "ayna"; return null; }
                }
                if (closeTop) g[ey, 0] = '.'; else g[ey + 2, 0] = '.';
            }
            // 3) Nesneler
            if (type == RoomType.Entry) foreach (var t in All(g, ch => ch == 'K' || ch == 'U')) g[t.y, t.x] = '.';
            if (type == RoomType.Middle) foreach (var t in All(g, ch => ch == 'K')) g[t.y, t.x] = '.';
            if (type != RoomType.Target) foreach (var t in All(g, ch => ch == 'C')) g[t.y, t.x] = DeviceLetter(device);
            var fwd = new List<(int x, int y)>();
            if (type != RoomType.Target)
            {
                // 4) İleri geçit
                var dev = All(g, IsDeviceLetter).First();
                var dist = LetterSteps(g);
                var pairs = new List<((int x, int y) a, (int x, int y) b)>();
                // Kanonik düzende doğu, kuzey ya da güney kenarı (sürüm 10: sonraki oda bu kenara göre döndürülür, girişi simetrik).
                // Aşama 12: Katar'da yalnız doğu sütunu, köşe çiftleri de dışarıda (geçit karesi açık kenarda olmasın; oda döndürülmez)
                for (int y = noCorners ? 1 : 0; y <= N - (noCorners ? 3 : 2); y++) pairs.Add(((N - 1, y), (N - 1, y + 1)));
                if (!eastOnly)
                {
                    for (int x = 4; x <= N - 2; x++) pairs.Add(((x, 0), (x + 1, 0)));
                    for (int x = 4; x <= N - 2; x++) pairs.Add(((x, N - 1), (x + 1, N - 1)));
                }
                bool Fits((int x, int y) t) => g[t.y, t.x] == '.' && (inner == null || !inner.Contains(t))
                    && Math.Max(Math.Abs(t.x - dev.x), Math.Abs(t.y - dev.y)) >= 2 && dist[t.y, t.x] >= 0 && dist[t.y, t.x] <= GateMaxSteps;
                var cands = pairs.Where(p => Fits(p.a) && Fits(p.b)).ToList();
                if (cands.Count == 0) { why = "ileri"; return null; }
                var pick = cands[(int)Math.Floor(r.Next() * cands.Count)];
                g[pick.a.y, pick.a.x] = 'F'; g[pick.b.y, pick.b.x] = 'F';
                fwd.Add(pick.a); fwd.Add(pick.b);
            }
            if (type == RoomType.Middle)
            {
                // 5) Kopya dolabı: geçide yakın olan kalkar (ikisi de yakın ya da uzaksa biri rastgele)
                var us = All(g, ch => ch == 'U');
                var near = us.Where(u => fwd.Any(f => Man(u, f) <= 2)).ToList();
                var drop = near.Count == 1 ? near[0] : us[(int)Math.Floor(r.Next() * us.Count)];
                g[drop.y, drop.x] = '.';
                if (All(g, ch => ch == 'U').Any(u => fwd.Any(f => Man(u, f) < 3))) { why = "dolap-gecit"; return null; }
            }
            // 6) Siviller: G 1, A 2, H 3 (fazlası önce geçidin yanından, sonra kenardan; rehinede ortadaki üçü kalır)
            int want = type == RoomType.Entry ? 1 : type == RoomType.Middle ? 2 : 3;
            for (var civ = All(g, ch => ch == 'S'); civ.Count > want; civ = All(g, ch => ch == 'S'))
            {
                var pool = civ.Where(s => fwd.Any(f => Man(s, f) <= 1)).ToList();
                if (pool.Count == 0) pool = civ.Where(Edge).ToList();
                if (pool.Count == 0) pool = civ;
                var k = pool[(int)Math.Floor(r.Next() * pool.Count)];
                g[k.y, k.x] = '.';
            }
            return g;
        }

        /// <summary>
        /// Giydirilmiş taslağın harf düzeyinde denetimi: giriş ("giris"), sayılar ("sayi"), odağın girişe bağlı komşuları ("odak"),
        /// H'de türün yol aralığı ("yol"), G ve A'da ileri geçidin uzaklığı ("ileri"), kapalı cep ("cep"), kopya dolabının girişe
        /// bağlı komşusu ("dolap"). Geçerse null.
        /// </summary>
        internal static string CheckDraft(char[,] g, RoomType type, string kind, HashSet<(int x, int y)> inner)
        {
            int Count(char c) { int n = 0; for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (g[y, x] == c) n++; return n; }
            int wantE = type == RoomType.Entry ? 3 : 2;
            var es = Enumerable.Range(0, N).Where(y => g[y, 0] == 'E').ToList();
            if (es.Count != wantE || Count('E') != wantE || es[es.Count - 1] - es[0] != wantE - 1) return "giris";
            bool target = type == RoomType.Target;
            int devices = Count('P') + Count('R') + Count('V');
            if (Count('C') != (target ? 1 : 0) || devices != (target ? 0 : 1) || Count('F') != (target ? 0 : 2)) return "sayi";
            if (Count('S') != (type == RoomType.Entry ? 1 : type == RoomType.Middle ? 2 : 3)) return "sayi";
            if (Count('K') != (target ? 2 : 0) || Count('U') != (type == RoomType.Entry ? 0 : type == RoomType.Middle ? 1 : 2)) return "sayi";
            if (Count('d') + Count('m') + Count('Y') != 0) return "sayi";
            if (Count('#') < 5 || Count('#') > 13 || Count('~') < 2 || Count('~') > 8) return "sayi";
            var dist = LetterSteps(g);
            var focus = All(g, ch => ch == 'C' || IsDeviceLetter(ch)).First();
            var near = Baskin.Dirs.Select(d => (x: focus.x + d[0], y: focus.y + d[1])).Where(t => In(t.x, t.y) && dist[t.y, t.x] >= 0).ToList();
            if (near.Count < (target ? 2 : 1)) return "odak";
            if (target && !Baskin.Dirs.Any(d => In(focus.x + d[0], focus.y + d[1]) && g[focus.y + d[1], focus.x + d[0]] == '#')) return "odak";
            if (target)
            {
                int p = near.Min(t => dist[t.y, t.x]);
                var (min, max) = RaidMaps.PathRange(kind);
                if (p < min || p > max) return "yol";
            }
            else
            {
                var fs = All(g, ch => ch == 'F');
                if (fs.Any(f => dist[f.y, f.x] < 0) || fs.Max(f => dist[f.y, f.x]) > GateMaxSteps) return "ileri";
            }
            int pockets = 0;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (Walk(g[y, x]) && dist[y, x] < 0) pockets++;
            if (pockets > 2) return "cep";
            foreach (var u in All(g, ch => ch == 'U'))
                if (!Baskin.Dirs.Any(d => In(u.x + d[0], u.y + d[1]) && dist[u.y + d[1], u.x + d[0]] >= 0)) return "dolap";
            return null;
        }

        /// <summary>Tesis odalarının kayıttaki üretici izi (<see cref="GameRecord.facility"/>): saha kuralıyla
        /// (<see cref="Balance.infiltratorFieldEnabled"/>) f10, saha kapalıyken f8s10 (sürüm 10'da ikisi de değişti). Tesis kapalı ('') yok; boş iz yükleme süzgecinden geçmez
        /// (<c>b == null</c> yalnız savunma amaçlı ''). İçerik özeti değil sürümdür (<see cref="Digest"/> 108 odayı üretir; her yüklemede ödenmez).</summary>
        public static string FingerprintFor(Balance b) =>
            b == null ? "" : b.infiltratorFieldEnabled ? "f" + GeneratorVersion.ToString(CultureInfo.InvariantCulture) : "f8s10";

        // ---------- Oda değerleri ----------
        /// <summary>Odanın baskın değerleri (dengeden) ve oda tohumu.</summary>
        public static RoomSetup Setup(Balance b, FacilityRoom r, int seed) => new RoomSetup
        {
            Seed = seed,
            Turns = r.Type == RoomType.Entry ? b.facilityEntryTurns : r.Type == RoomType.Middle ? b.facilityMiddleTurns : b.facilityTargetTurns,
            Machines = r.Type == RoomType.Entry ? b.facilityEntryMachines : r.Type == RoomType.Middle ? b.facilityMiddleMachines : b.facilityTargetMachines,
            DeviceHp = r.Device == "pano" ? b.facilityPanelHp : r.Device == "anten" ? b.facilityAntennaHp : r.Device == "vana" ? b.facilityValveHp : 0,
            Moves = MoveRules.From(b),
        };

        // ---------- Test kancaları ve özet ----------
        /// <summary>Planın bütün hâllerini yeniden sınar (şartsız, kademe 1–2, kademe 3, rafın öbür konumu); geçerse null.</summary>
        internal static string Recheck(FacilityRoom r) => Baskin.RecheckRoom(r);

        /// <summary>
        /// Yalnız testler: bölgenin kanadında türü ve şartı zorlayarak tek oda üretir (<see cref="AttemptsPerKind"/> deneme, yedek tür
        /// yok). Olmazsa null; <paramref name="why"/> ret kodlarının sayımı ("kod:sayı,…").
        /// </summary>
        internal static FacilityRoom TryRoom(uint seed, string regionId, int wing, RoomType type, string kind, string ev, out string why)
        {
            var def = GameData.Region(regionId);
            var wp = PickWing(Rng.Mix(Root(seed), Fnv1a32(def.Id)), def, wing);
            var whys = new Dictionary<string, int>();
            int a = 0;
            // Aşama 12: zorlanan şart Katar'sa ileri geçit Katar tesisindeki gibi doğu sütununda
            var fr = TryKind(wp, type, kind, kind, ev, ref a, AttemptsPerKind, whys, ev == "katar", null, 0, ev == "katar" || ev == "yalpa");
            why = fr != null ? null : string.Join(",", whys.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + ":" + kv.Value));
            if (fr != null) fr.NameKey = RaidMaps.Names[kind][0];
            return fr;
        }

        /// <summary>Odanın dökümü (kimlik, tür, ad, donanım, şart, satırlar, parçalar); dile bağlı değil.</summary>
        public static string Dump(FacilityRoom r) => FormattableString.Invariant($"{r.Id}|{r.MapKind}|{r.NameKey}|{r.Device}|{r.Event}|")
            + string.Join("/", r.Rows) + "|"
            + string.Join(",", r.Parts.Select(p => FormattableString.Invariant($"{p.Kind}{p.At.X}{p.At.Y}{(p.To is Baskin.Pt t ? ">" + t.X.ToString(CultureInfo.InvariantCulture) + t.Y.ToString(CultureInfo.InvariantCulture) : "")}d{p.Dir}t{p.MinTier}")));

        /// <summary>Planın özeti: 108 odanın dökümünün FNV-1a'sı (Golden_Seed79 testi).</summary>
        public static uint Digest(FacilityPlan plan) => Fnv1a32(string.Join("\n", plan.Rooms.Select(Dump)));
    }
}
