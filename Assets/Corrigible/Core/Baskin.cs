using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Baskın: hücre tabanlı taktik savaş (Into the Breach esinli).
    /// Durum (BState) kopyalanabilir: geri alma ve önizleme kopya üzerinde çalışır. Rastgelelik yalnızca durumdaki
    /// mulberry32 (Rs) ile; aynı parametreler + aynı hamleler = aynı savaş.
    ///
    /// Tur yapısı: oyuncu turu (düşman niyetleri önceden görünür) → düşman evresi: niyetler sırayla uygulanır,
    /// işaretli karelerden yeni robotlar uyanır, düşmanlar yer değiştirip yeni niyet seçer, yeni uyanma kareleri işaretlenir.
    /// </summary>
    public static partial class Baskin
    {
        public const int N = 8;
        const int MaxEnemies = 7;
        public static readonly int[][] Dirs = { new[] { 0, -1 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { -1, 0 } };

        // '.' zemin, '#' duvar, '~' elektrikli kablo kanalı, 'C' hedef dolap, 'Y' ve 'S' sivil sunucu,
        // 'K' soğutma ünitesi, 'U' kopya dolabı, 'E' ekip girişi, 'd' dron, 'm' bakım robotu
        public static readonly (string Name, string[] Rows)[] Layouts =
        {
            (Loc.N("Ana sunucu salonu"), new[] { "S.~...U.", "..~.#..S", "E.#.K#..", "E.#..#C.", "E..~~...", "..#..#dm", ".S#.d.Y.", "....K..U" }),
            (Loc.N("Soğutma koridoru"), new[] { "E..S#..U", "E...#.d.", "E.K.~..S", "..#.~##.", "..#.~.C.", ".S..#.mK", "....#..d", "S.~~.Y.U" }),
            (Loc.N("Yedekli veri kasası"), new[] { ".S..U..S", "..#.d...", "E.#K..#.", "E.~.##C.", "E.#....m", "..#.Y#.d", "..S.K.#.", ".~..U..S" }),
        };

        public sealed class UnitType
        {
            public string Id, Short, Ability; public char Side; public int Hp, Move;
            /// <summary>Makine: başlangıç zırhı, niyet türleri (shoot, melee, repair, lob, blast, lock, shield, clone) ve uzaktan mı vurur.</summary>
            public int Armor; public string[] Kinds = Array.Empty<string>(); public bool Ranged;
            string name, desc;
            /// <summary>Gösterim adı (geçerli dilde).</summary>
            public string Name { get => Loc.T(name); set => name = value; }
            /// <summary>Makinenin ne yaptığı (ipucu kartı); oyuncu birimlerinde boş.</summary>
            public string Desc { get => string.IsNullOrEmpty(desc) ? "" : Loc.T(desc); set => desc = value; }
        }

        public static readonly Dictionary<string, UnitType> UnitTypes = new Dictionary<string, UnitType>
        {
            ["tek"] = new UnitType { Id = "tek", Side = 'p', Name = Loc.N("Teknisyen"), Short = "T", Hp = 3, Move = 3, Ability = "sabotaj" },
            ["emp"] = new UnitType { Id = "emp", Side = 'p', Name = Loc.N("EMP uzmanı"), Short = "E", Hp = 3, Move = 3, Ability = "emp" },
            ["ana"] = new UnitType { Id = "ana", Side = 'p', Name = Loc.N("Analist"), Short = "A", Hp = 2, Move = 4, Ability = "kanca" },
            ["maden"] = new UnitType { Id = "maden", Side = 'p', Name = Loc.N("Madenci"), Short = "M", Hp = 3, Move = 3, Ability = "dinamit" },
            ["duman"] = new UnitType { Id = "duman", Side = 'p', Name = Loc.N("Sis uzmanı"), Short = "S", Hp = 2, Move = 3, Ability = "duman" },
            ["dron"] = new UnitType { Id = "dron", Side = 'e', Name = Loc.N("Güvenlik dronu"), Short = "D", Hp = 2, Move = 3, Kinds = new[] { "shoot" }, Ranged = true,
                Desc = Loc.N("Düz hatta ilk engele ateş eder: 1 hasar.") },
            ["bakim"] = new UnitType { Id = "bakim", Side = 'e', Name = Loc.N("Bakım robotu"), Short = "B", Hp = 2, Move = 2, Kinds = new[] { "melee", "repair" },
                Desc = Loc.N("Bitişik kareye vurur (2 hasar) ya da hasarlı hedef dolabı onarır.") },
            // Bölgelerin imza düşmanları (her biri iki bölgede)
            ["zdron"] = new UnitType { Id = "zdron", Side = 'e', Name = Loc.N("Zırhlı dron"), Short = "Z", Hp = 2, Move = 3, Armor = 1, Kinds = new[] { "shoot" }, Ranged = true,
                Desc = Loc.N("Dron gibi düz hatta ateş eder; zırhı ilk hasarı karşılar.") },
            ["havan"] = new UnitType { Id = "havan", Side = 'e', Name = Loc.N("Havan robotu"), Short = "H", Hp = 2, Move = 1, Kinds = new[] { "lob" }, Ranged = true,
                Desc = Loc.N("Üç kare öteye, engellerin üstünden atar: 1 hasar.") },
            ["bomba"] = new UnitType { Id = "bomba", Side = 'e', Name = Loc.N("Patlayan robot"), Short = "P", Hp = 1, Move = 3, Kinds = new[] { "blast" },
                Desc = Loc.N("Yanına sokulur ve patlar: çevresindeki dört kareye 1 hasar.") },
            ["kilit"] = new UnitType { Id = "kilit", Side = 'e', Name = Loc.N("Kilitleyici"), Short = "K", Hp = 2, Move = 2, Kinds = new[] { "lock" }, Ranged = true,
                Desc = Loc.N("Düz hatta dört kareye kadar kilit atar: vurulan uzman bir tur yürüyemez.") },
            ["kalkan"] = new UnitType { Id = "kalkan", Side = 'e', Name = Loc.N("Kalkan dronu"), Short = "Q", Hp = 2, Move = 3, Kinds = new[] { "shield", "shoot" }, Ranged = true,
                Desc = Loc.N("Yanındaki makineye zırh verir; verecek kimse yoksa ateş eder.") },
            ["cogalt"] = new UnitType { Id = "cogalt", Side = 'e', Name = Loc.N("Çoğaltıcı"), Short = "Ç", Hp = 2, Move = 2, Kinds = new[] { "clone" },
                Desc = Loc.N("Yanındaki boş kareye yeni bir dron uyandırır.") },
            ["alev"] = new UnitType { Id = "alev", Side = 'e', Name = Loc.N("Alev robotu"), Short = "A", Hp = 2, Move = 2,
                Kinds = new[] { "flame" }, Desc = Loc.N("Önündeki kareleri yakar. Alev, basınçlı tüpü tetikler.") },
            ["muhur"] = new UnitType { Id = "muhur", Side = 'e', Name = Loc.N("Mühürleyici"), Short = "Ü", Hp = 2, Move = 2,
                Kinds = new[] { "seal" }, Desc = Loc.N("Bitişik zemine kırılabilir kalas kurar. Geçişleri kapatamaz.") },
            ["tampon"] = new UnitType { Id = "tampon", Side = 'e', Name = Loc.N("Tampon"), Short = "T", Hp = 2, Move = 2,
                Kinds = new[] { "ram" }, Desc = Loc.N("Düz hatta koşup karşısına çıkan birimi iter.") },
            ["montaj"] = new UnitType { Id = "montaj", Side = 'e', Name = Loc.N("Montajcı"), Short = "M", Hp = 2, Move = 2,
                Kinds = new[] { "build", "melee" }, Desc = Loc.N("Hurdadan dron kurar, yanında hurda yoksa vurur.") },
            ["kirag"] = new UnitType { Id = "kirag", Side = 'e', Name = Loc.N("Kırağı dronu"), Short = "I", Hp = 2, Move = 3, Ranged = true,
                Kinds = new[] { "frost" }, Desc = Loc.N("Buzlu alana giren birimin yürüyüşü orada biter.") },
            ["capa"] = new UnitType { Id = "capa", Side = 'e', Name = Loc.N("Çapa robotu"), Short = "Ç", Hp = 2, Move = 2, Ranged = true,
                Kinds = new[] { "pull" }, Desc = Loc.N("Hattındaki ilk birimi kendine çeker. Platform yatınca kaymaz.") },
            // Tesis aşama 8: ekibin uzman olmayan birimleri (Unit.Class ile; can ve yürüyüş MoveRules'tan yazılır). Aygıtlar yürümez.
            ["taret"] = new UnitType { Id = "taret", Side = 'p', Name = Loc.N("Yaylı taret"), Short = "R", Hp = 2, Move = 0 },
            ["yem"] = new UnitType { Id = "yem", Side = 'p', Name = Loc.N("Yürüyen yem"), Short = "Y", Hp = 2, Move = 0 },
            ["vinc"] = new UnitType { Id = "vinc", Side = 'p', Name = Loc.N("Makaralı vinç"), Short = "V", Hp = 2, Move = 0 },
            ["kopek"] = new UnitType { Id = "kopek", Side = 'p', Name = Loc.N("Köpek"), Short = "Ö", Hp = 2, Move = 5 },
        };

        /// <summary>
        /// Bölge özellikleri (<see cref="RegionDef.Trait"/>): taktik tahtayı değiştirir. Baskın parametresinin Traits listesinde
        /// kimlikle gelir; "kopya" ve "guclu" bölgeye değil zorluk kademesine bağlıdır.
        /// </summary>
        public static readonly Dictionary<string, (string Name, string Desc)> RegionTraits = new Dictionary<string, (string, string)>
        {
            ["guvenli"] = (Loc.N("Güvenli giriş"), Loc.N("İlk tur hiçbir robot uyanmaz.")),
            ["dar"] = (Loc.N("Dar koridor"), Loc.N("Fazladan duvarlar: atış hatları kısa, yollar uzun.")),
            ["kisa"] = (Loc.N("Kısa nöbet"), Loc.N("Hedef odasının ilk ziyareti bir tur kısa sürer.")),
            ["takviye"] = (Loc.N("Takviye"), Loc.N("Başta bir robot fazla.")),
            ["acik"] = (Loc.N("Açık arazi"), Loc.N("Duvarların bir kısmı yıkık: atış hatları uzun.")),
            ["sis"] = (Loc.N("Kum fırtınası"), Loc.N("Uyanacak robotların yeri görünmez.")),
            ["sogutma"] = (Loc.N("Soğutma ağı"), Loc.N("İki soğutma ünitesi fazla; kırılan ünite çevresine patlar.")),
            ["kalabalik"] = (Loc.N("Kalabalık şehir"), Loc.N("İki sivil sunucu fazla; GÜVEN kolay düşer.")),
            ["zirhli"] = (Loc.N("Zırhlı dolap"), Loc.N("Hedef dolapların bir canı fazla.")),
            ["sebeke"] = (Loc.N("Elektrik şebekesi"), Loc.N("Fazladan elektrikli kanal: üstüne düşen 1 hasar alır.")),
            ["fabrika"] = (Loc.N("Kopya fabrikası"), Loc.N("Bir kopya dolabı fazla.")),
            ["devriye"] = (Loc.N("Hızlı devriye"), Loc.N("Robotlar bir kare fazla yürür.")),
        };

        public static readonly Dictionary<string, (string Name, string Desc)> Abilities = new Dictionary<string, (string, string)>
        {
            ["sabotaj"] = (Loc.N("Sabotaj"), Loc.N("Bitişik kare. Dolaba 2 hasar; robota 1 hasar ve 1 kare itme.")),
            ["emp"] = (Loc.N("EMP atışı"), Loc.N("Düz hatta 2–5 kare uzağa, engellerin üstünden. Hedefteki robota ya da sıradan dolaba 1 hasar (zırhlı hedef dolaplara işlemez); çevresindeki 4 karedeki robotları dışarı iter.")),
            ["kanca"] = (Loc.N("Kanca"), Loc.N("Düz hatta 5 kare. Çarptığı robota 1 hasar verir ve onu 1 kare kendine çeker (dost uzmanı hasarsız çeker); duvara ya da dolaba çarparsa Analist onun yanına atılır. Analistin 3 kare çevresindeki robotların niyeti kesin görünür.")),
            ["dinamit"] = (Loc.N("Dinamit"), Loc.N("Bitişik kare. Duvarı kalıcı olarak yıkar ve bir dinamit harcar: moloz aynı yönde bir kare öteye sıçrar, oradaki robota ya da sıradan nesneye 1 hasar verir. Robota ya da sıradan nesneye atılırsa 1 hasar (dinamit harcamaz). Hedef dolaba, ikmal kasasına ve dost uzmana atılmaz.")),
            ["duman"] = (Loc.N("Sis bombası"), Loc.N("Düz hatta 1–4 kare, engellerin üstünden; bir sis bombası harcar. Sis bu turun makine evresi boyunca nişanlı ateşi keser: atış ve kilit sise girince kaybolur (sisli kare ve arkası vurulmaz), bakım robotu ve havan sisli kareye vuramaz. Patlamayı, uyanmayı ve hareketi kesmez.")),
        };

        public static readonly Dictionary<string, string> ObjNames = new Dictionary<string, string>
        {
            ["core"] = Loc.N("Çekirdek dolabı"), ["node"] = Loc.N("YZ düğüm dolabı"),
            ["civ"] = Loc.N("Sivil sunucu"), ["cool"] = Loc.N("Soğutma ünitesi"), ["spawner"] = Loc.N("Kopya dolabı"),
            // Bölge olaylarının nesneleri (Baskin.Events.cs)
            ["enkaz"] = Loc.N("Enkaz yığını"), ["tup"] = Loc.N("Basınçlı tüp"), ["kol"] = Loc.N("Kaynak kolu"),
            // Tesis odalarının donanımları (giriş ve ara oda; Baskin.Rooms.cs)
            ["pano"] = Loc.N("Sigorta panosu"), ["anten"] = Loc.N("Çağrı anteni"), ["vana"] = Loc.N("Soğutma vanası"),
            // Tesis aşama 8: örme setinin engelleri ve aygıt hurdası (Baskin.Gear.cs)
            ["tel"] = Loc.N("Tel örgü"), ["kalas"] = Loc.N("Kalas duvar"), ["hurda"] = Loc.N("Hurda"),
            // Tesis aşama 10: köpek önleminin nesnesi (Baskin.Counters.cs; olay nesnesi, engel ve donanım değil)
            ["yayici"] = Loc.N("Koku yayıcı"),
            // Tesis aşama 11: savunmanın saha rölesi (Baskin.Defense.cs, RelayKind; sunum adı buradan okur)
            ["role"] = Loc.N("Saha rölesi"),
        };

        public static bool IsTarget(string k) => k == "core" || k == "node";
        /// <summary>İkmal görevi (kasa, taşıyıcı, kaçış kuralları). Aşama 11: savunmada hiçbir görev kuralı yoktur (kip "defense").</summary>
        public static bool IsLoot(BState state) => state.Params?.MissionKind == "loot" && state.Params.Mode != "defense";

        // ---------- Durum ----------
        public sealed class Obj { public string K; public int Hp, Max; public bool MachineBuilt; public Obj Clone() => (Obj)MemberwiseClone(); }

        public sealed class Cell
        {
            public string T; // floor, wall, cable
            public Obj O;
            /// <summary>Sis bombası: bu turun makine evresinde nişanlı ateşi keser, evrenin sonunda dağılır.</summary>
            public bool Smoke;
            /// <summary>Bölge olaylarının kare bayrakları (Baskin.Events.cs): söndürme bulutu, yanıcı gaz, tavan çatlağı, hidrolik
            /// yağ, arşiv rafının rayı. Phase: şebeke olayında kablonun fazı (1 A, 2 B; 0 faz yok).</summary>
            public bool Cloud, Gas, Crack, Oil, Shelf;
            public int Phase;
            public int FrostUntil;
            /// <summary>Tesis odası: Door 1 batı girişi / giriş geçidi, 2 ileri geçit (ikisi de zemin); Vent menfez çiftinin numarası
            /// (zemin, 0 yok); Shutter ağır kepenk (T = "wall"; aşama 6'da kalkınca T = "floor", bayrak kalır). Bugünkü tahtada hep 0/false.</summary>
            public int Door, Vent;
            public bool Shutter;
            /// <summary>Tesis odası: büyük sandığın duvar nişi (ikmal hedef odası; T = "wall" kalır, iki kişilik iş açar). Plan bayrağıdır,
            /// açılınca <see cref="BState.CrateOpen"/>. Örnek tahtada hep false.</summary>
            public bool Crate;
            /// <summary>Tesis aşama 10: çift cidarlı duvar (yıkım önlemi; T = "wall" kalır, lokum tek hamlede birden çok hak ister). Plan
            /// kaplamasıdır (<see cref="ApplyCounterCells"/>), yıkılınca false. Örnek tahtada hep false.</summary>
            public bool Double;
            public Cell Clone() { var c = (Cell)MemberwiseClone(); c.O = O?.Clone(); return c; }
        }

        public sealed class Intent { public string Kind; public int Dir, Range; public Intent Clone() => (Intent)MemberwiseClone(); }

        public sealed class Unit
        {
            /// <summary>Armor: zırh plakası, ilk hasarı karşılar ve kırılır (yalnız ekip).</summary>
            public int Id, X, Y, Hp, Max, Order, Armor;
            public int MoveRange, Power;
            internal bool PulseContact;
            public int EmpBoosts, EmpBoostDamage;
            public string PersonId, PersonName;
            public string Type; public char Side;
            /// <summary>Locked: kilitleyici vurdu, bu oyuncu turu yürüyemez (yeteneğini kullanabilir). Out: kaçışta bir çıkıştan
            /// tahliye edildi, tahtada değil. Left: süre dolarken tahtada kaldı; baskında düşmüş sayılır (Dead da işaretli).</summary>
            public bool Moved, Acted, Dead, Locked, Out, Left;
            /// <summary>Bölge olayları: Frozen kriyo hattında dondu (sonraki sırasında yürüyemez, eylemini yapar), Ice buz kabuğu
            /// (hasarı plakadan önce karşılar), Cab yük asansörünün kabininde (tahtada değil, hedef alınmaz, eylem yapmaz).</summary>
            public bool Frozen, Cab;
            public int Ice;
            /// <summary>Tesis odası: uzman kapı karesine kendi yürüyüşüyle girdi (ya da <see cref="EnterGate"/>), kapıda bekliyor: tahtada
            /// değil, hedef alınmaz, eylem yapmaz, baskında sayılır. Örnek tahtada hep false.</summary>
            public bool AtGate;
            /// <summary>Tesis odası, giriş ya da ara oda: süre dolarken kapıya varamadı ve ekip kapıdakilerle geçti; sağ geri çekildi
            /// (tahtada değil, sonraki odalara girmez, baskın sonunda canıyla döner; oda raporunda "w"). Örnek tahtada hep false.</summary>
            public bool Withdrew;
            /// <summary>Kadro sırası (<see cref="RaidParams.Squad"/> dizini); makinede ve örnek tahtada −1. Oda raporu kişiyi bununla izler.</summary>
            public int Slot = -1;
            /// <summary>Sınırlı yeteneğin kalan hakkı (Madenci: duvar yıkan dinamit, Sis uzmanı: sis bombası); −1 sınırsız.</summary>
            public int Charges = -1;
            /// <summary>Pusu (tesis hamlesi): hattın yönü (<see cref="Dirs"/> dizini; −1 yok) ve kurulduğu kare. Makine evresinde
            /// ateşlenir, sonra bütün birimlerde silinir. Örnek tahtada hep −1.</summary>
            public int AmbushDir = -1, AmbushX = -1, AmbushY = -1;
            /// <summary>Tesis odası, "son nefes" (<see cref="LastBreath"/>): oyuncunun hamle sırası dışında (makine evresi, tur sonu
            /// olay ya da hasar) düştü ve oda oyuncu bir daha hamle yapamadan bitti; ayakta çıkan ekip gövdeyi götürür (oda raporu:
            /// geçişte Body 1, tesisin girişinde 2). Yalnız oda biterken yazılır. Örnek tahtada hep false.</summary>
            public bool Breath;
            /// <summary>
            /// Tesis aşama 8 (Baskin.Gear.cs): birimin sınıfı; null uzman (örnek tahta ve kadro), <see cref="ClassDog"/>,
            /// <see cref="ClassGadget"/>, <see cref="ClassCaptive"/>. Köpek, aygıt ve tutsak Side 'p' ve Slot −1'dir. Gear/GearLeft:
            /// uzmanın taşıdığı ekipman (<see cref="SquadSlot.Gear"/>) ve kalan baskın hakkı (köpekte −1). Owner: köpeğin sahibi, aygıtı
            /// kuranın, tutsağı kelepçeleyenin kadro sırası. Dir: aygıtın yönü (<see cref="Dirs"/> dizini). Turns: aygıtın kalan çalışma
            /// turu, tutsağın kalan oyuncu turu. Bark: köpek bu tur havladı (planda okunur, sonra silinir). Idle: bir sonraki planı atlanan
            /// makine. Örnek tahtada hepsi varsayılanda.
            /// </summary>
            public string Class, Gear;
            public int GearLeft = -1, Owner = -1, Dir = -1, Turns;
            public bool Bark, Idle;
            /// <summary>Tesis aşama 10: Söküm anahtarlı sabotaj makinenin cıvatasını söktü (<see cref="Bolted"/> artık tutmaz; dönüşte de).
            /// Örnek tahtada hep false.</summary>
            public bool Unbolted;
            /// <summary>Tesis aşama 12 (Katar): ekip üyesi açık kenardan düştü (<see cref="FallOff"/>); her zaman Dead ile birlikte (Hp 0, Out
            /// false): tahtada değil, gövde değil, sürüklenmez, son nefes almaz; oda raporunda "k", ölmez, yatar. Örnek tahtada hep false.</summary>
            public bool Overboard;
            /// <summary>Tesis aşama 12 kapanışı (Katar): makine bu dönüş tahtasına önceki vagondan takip ederek geldi (<see cref="ReturnBoard"/>
            /// koyar; okuyucu <see cref="FollowedInIds"/>). Yalnız sunum okur; kural, imza ve rapor bunu kullanmaz. Örnek tahtada hep false.</summary>
            public bool Follower;
            public bool Assembled;
            public FacilityTitle Titles, Work;
            public int Dragged, DogCommands;
            public Intent Intent;
            public Unit Clone() { var u = (Unit)MemberwiseClone(); u.Intent = Intent?.Clone(); return u; }
        }

        public struct Pt { public int X, Y; public Pt(int x, int y) { X = x; Y = y; } }

        /// <summary>Oynanış efekti: arayüz önizleme ve canlandırma için okur.</summary>
        public sealed class Fx
        {
            public string K; public int X, Y, N, Unit, Dx, Dy;
            public string Obj;
        }

        public sealed class LogLine { public int Turn; public string Text, Kind; }

        public sealed class BState
        {
            public RaidParams Params;
            public int Rs, Turn = 1, MaxTurns, SpawnPerTurn, Collateral, NextId = 1;
            public string Phase = "player", Layout, OutcomeReason;
            public bool IsCore, OutcomeSuccess, Retreated;
            /// <summary>Kaçış: görevin hedefi tamam (ikmal kasası açıldı ya da hedef dolapların hepsi kırıldı). Tahtadaki her uzman
            /// bir çıkıştan çıkmalı; baskın tahtada uzman kalmayınca ya da süre dolunca biter, geri çekilme yoktur.</summary>
            public bool Escape;
            /// <summary>Bölge ve kademe: imza düşmanı, makinelerin ek canı ve yürüyüşü, uyanma yerlerinin gizli olması (kum fırtınası).</summary>
            public string Signature;
            public int EnemyHp, EnemyMove;
            public bool Fog;
            /// <summary>Gelişmiş makine aklı (<see cref="RaidParams.Tactics"/>).</summary>
            public bool Smart;
            /// <summary>İkmal görevi: kasayı açan uzman (her uzman açabilir) malzemeyi taşır (birim kimliği; 0 ise kasa açılmadı). Malzeme ancak
            /// taşıyıcı bir çıkıştan çıkarsa alınır; taşıyıcı düşerse ya da süre dolarken tahtadaysa görev kaybedilir.</summary>
            public int Carrier;
            /// <summary>Çıkışlar: ekibin giriş kareleri (her görevde); kaçışta üstüne basan uzman tahliye edilir.</summary>
            public List<Pt> Exits = new List<Pt>();
            public Cell[,] Cells = new Cell[N, N]; // [y, x]
            public List<Unit> Units = new List<Unit>();
            public List<Pt> Spawns = new List<Pt>();
            /// <summary>Madenci'nin yıktığı duvarlar (sunumun moloz izi).</summary>
            public List<Pt> Rubble = new List<Pt>();
            public List<LogLine> Log = new List<LogLine>();
            public List<Fx> FxList = new List<Fx>();
            public List<BState> Hist = new List<BState>();
            /// <summary>Bölge olayı (<see cref="RaidParams.Event"/>; yoksa null). InEvent: olay adımı ya da zinciri sürüyor (hasar
            /// tavanı, nesne muafiyeti). EvDamageTeam/EvDamageMachines: olayların ekibe ve makinelere verdiği hasar (bot ölçümü).</summary>
            public EvState Ev;
            public bool InEvent;
            public int EvDamageTeam, EvDamageMachines;
            /// <summary>Tesis hazırlığı: bu tura kadar (dahil) uyanma yeri seçilmez (sigorta panosu); bu tura kadar makine planı yürüyüş
            /// içermez (soğutma vanası, ikmal). Örnek tahtada 0.</summary>
            public int QuietTurns, StillTurns;
            /// <summary>Tesis odası (salt okunur plan; paylaşılır); örnek tahtada (<see cref="Create"/>) null.</summary>
            public FacilityRoom Room;
            /// <summary>Sürüm 10: plan tahtasının (oda kurulurken; <see cref="Room"/> henüz null) odası; yalnız kanonik x için okunur
            /// (<see cref="CanonX(BState, Pt)"/>: döndürülmüş odada olay ve bölge sezgileri giriş batıdaymış gibi çalışır). Oyun tahtasında null.</summary>
            internal FacilityRoom PlanRoom;
            /// <summary>Tesis hamlelerinin değerleri (salt okunur, paylaşılır; Baskin.Actions.cs). Örnek tahtada null: yeni hamle yok.</summary>
            public MoveRules Moves;
            /// <summary>Tesis odası: yerdeki yük (taşıyıcı düştü; −1 yok). Kareye kendi hareketiyle giren ilk ekip üyesi alır.</summary>
            public int CargoX = -1, CargoY = -1;
            /// <summary>İki kişilik iş: işi tutan uzman (0 yok) ve işin karesi (kepenk ya da büyük sandık). Tur sonunda boşa gider.</summary>
            public int HoldId, HoldX = -1, HoldY = -1;
            /// <summary>Büyük sandık bu ziyarette açıldı (oda raporunun Crate'i).</summary>
            public bool CrateOpen;
            /// <summary>Tesis aşama 8: bu ziyarette köpeğin kokladığı oda sırası (1..3; 0 yok). Oda raporunun Sniff'i.</summary>
            public int Sniffed;
            /// <summary>Tesis aşama 10: bu ziyaretin sınıf sayaçları (<see cref="FacilityCounters.Classes"/> sırası; oda raporunun Tally'si).
            /// Yalnız <see cref="MoveRules.Tally"/> iken ve sınıfın kademesi 2'den küçükken artar (<see cref="CountTally"/>). Örnek tahtada 0.</summary>
            public int TallyPush, TallyBreach, TallyBuild, TallyCapture, TallyDog, TallyGadget;
            /// <summary>
            /// Tesis aşama 11, üç odalı savunma (Baskin.Defense.cs): savunma tahtası mı; odanın başlangıç turu (tek tur sayacı, <see cref="Turn"/>
            /// buradan başlar; ilk tur barikat turudur, makine yok); terk edilen oda sayısı; yerleşmeyi bekleyen makineler (takipçiler ve
            /// dalgalar, geliş turuyla; yer ya da makine sınırı yoksa kuyrukta kalır); bu odanın dalga takvimi (salt okunur, kurulumda yazılır).
            /// Eski ve saldırı tahtasında false/1/0/boş.
            /// </summary>
            public bool Defense;
            public int StartTurn = 1, Abandoned;
            public List<Incoming> Incoming = new List<Incoming>();
            public List<DefenseWave> Waves = new List<DefenseWave>();
            /// <summary>Tesis aşama 12 (Katar): bu odadan AYIR'la çıkıldı (<see cref="Decouple"/>; takip yok, oda raporunun Decouple'ı). Eski
            /// tahtada hep false.</summary>
            public bool Decoupled;

            /// <summary>Sayaçlar, sınıf sırasıyla (yeni dizi).</summary>
            public int[] TallyArray() => new[] { TallyPush, TallyBreach, TallyBuild, TallyCapture, TallyDog, TallyGadget };

            /// <summary>Derin kopya (geçmiş hariç).</summary>
            public BState Clone()
            {
                var c = (BState)MemberwiseClone();
                c.Cells = new Cell[N, N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) c.Cells[y, x] = Cells[y, x].Clone();
                c.Units = Units.Select(u => u.Clone()).ToList();
                c.Spawns = new List<Pt>(Spawns);
                c.Exits = new List<Pt>(Exits);
                c.Rubble = new List<Pt>(Rubble);
                c.Log = new List<LogLine>(Log);
                c.FxList = new List<Fx>(FxList);
                c.Ev = Ev?.Clone();
                c.Incoming = Incoming.Select(i => i.Clone()).ToList();
                c.Waves = new List<DefenseWave>(Waves);
                c.Hist = new List<BState>();
                return c;
            }
        }

        // ---------- Yardımcılar ----------
        static double Rnd(BState B)
        {
            unchecked
            {
                B.Rs = B.Rs + 0x6D2B79F5;
                int t = (B.Rs ^ (int)((uint)B.Rs >> 15)) * (1 | B.Rs);
                t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
                return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
            }
        }

        static bool Inb(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;
        public static Cell CellAt(BState B, int x, int y) => B.Cells[y, x];
        /// <summary>Tahtadaki birim (düşen ve tahliye edilen sayılmaz).</summary>
        public static Unit UnitAt(BState B, int x, int y) => B.Units.FirstOrDefault(u => OnBoard(u) && u.X == x && u.Y == y);
        static int Man(int ax, int ay, int bx, int by) => Math.Abs(ax - bx) + Math.Abs(ay - by);
        /// <summary>Tahtadaki birimler (düşen ve tahliye edilen sayılmaz).</summary>
        public static List<Unit> Alive(BState B, char side) => B.Units.Where(u => OnBoard(u) && u.Side == side).ToList();
        /// <summary>Tahtada: düşmedi, tahliye edilmedi, asansör kabininde ve tesis kapısında değil.</summary>
        public static bool OnBoard(Unit u) => !u.Dead && !u.Out && !u.Cab && !u.AtGate && !u.Withdrew;
        /// <summary>Baskındaki makineler (asansör kabinindeki dahil): makine sınırı (<see cref="MaxEnemies"/>) bunu sayar. Ele geçirilen
        /// makine (tutsak, aşama 8) de sayılır.</summary>
        static int Machines(BState B) => B.Units.Count(u => (u.Side == 'e' && !IsTurncoat(u) || IsCaptive(u)) && !u.Dead && !u.Out);
        static void AddFx(BState B, Fx f) => B.FxList.Add(f);
        static void Say(BState B, string text, string kind = "sys") => B.Log.Add(new LogLine { Turn = B.Turn, Text = text, Kind = kind });
        static Obj NewObj(string k, int hp) => new Obj { K = k, Hp = hp, Max = hp };
        public static int Sign(int v) => v > 0 ? 1 : v < 0 ? -1 : 0;

        static Unit NewUnit(BState B, string type, int x, int y)
        {
            var T = UnitTypes[type];
            int hp = T.Hp + (T.Side == 'e' ? B.EnemyHp : 0);
            return new Unit { Id = B.NextId++, Type = type, Side = T.Side, X = x, Y = y, Hp = hp, Max = hp, Armor = T.Armor };
        }

        /// <summary>Uyanan ya da başta eklenen makine: bölgenin imza düşmanı varsa üçte biri o (tek zar, sıra korunur).</summary>
        static string WakeType(BState B, double r) =>
            B.Signature == null ? (r < 0.6 ? "dron" : "bakim") : r < 0.4 ? "dron" : r < 0.7 ? "bakim" : B.Signature;

        /// <summary>Ekip girişinden bütün hedef dolaplara yol var mı (duvar ve nesne eklerken tahta kapanmasın).</summary>
        static bool Connected(BState B, List<Pt> entries)
        {
            var seen = new bool[N, N];
            var q = new Queue<Pt>();
            foreach (var e in entries) { seen[e.Y, e.X] = true; q.Enqueue(e); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (!Inb(x, y) || seen[y, x] || CellAt(B, x, y).T == "wall" || CellAt(B, x, y).O != null) continue;
                    seen[y, x] = true; q.Enqueue(new Pt(x, y));
                }
            }
            return TargetsLeft(B).All(t => Dirs.Any(d => Inb(t.X + d[0], t.Y + d[1]) && seen[t.Y + d[1], t.X + d[0]]));
        }

        /// <summary>Bölge özelliği tahtaya ekler: girişin yanı boş kalır, yol kapanmaz; olmazsa o parça eklenmez. <paramref name="ok"/>
        /// (tesis odası) verilirse parça ayrıca o yüklemi bozmamalı; verilmezse bugünkü baskınla aynı.</summary>
        static void Scatter(BState B, List<Pt> entries, int count, Action<Cell> place, Func<Pt, bool> where = null, Func<BState, bool> ok = null)
        {
            var spots = FreeTiles(B).Where(t => CellAt(B, t.X, t.Y).T == "floor" && !entries.Any(e => Man(e.X, e.Y, t.X, t.Y) <= 1) && (where == null || where(t))).ToList();
            for (int placed = 0; placed < count && spots.Count > 0;)
            {
                int k = (int)Math.Floor(Rnd(B) * spots.Count);
                var t = spots[k]; spots.RemoveAt(k);
                var cell = CellAt(B, t.X, t.Y);
                var before = cell.Clone();
                place(cell);
                if (Connected(B, entries) && (ok == null || ok(B))) placed++;
                else B.Cells[t.Y, t.X] = before;
            }
        }

        /// <summary>
        /// Bölge özellikleri tahtaya serpiştirilir. Tesis odası için: <paramref name="allow"/> serpiştirmenin kare süzgecidir (geçit
        /// ve menfez yerleri), <paramref name="ok"/> her parçadan sonra tahtanın yapısını sınar. "acik" dalı bunları kullanmaz (duvar
        /// yıkmak yolu yalnız açar). İkisi de verilmezse bugünkü baskınla aynı çekimler.
        /// </summary>
        static void ApplyRegionTraits(BState B, RaidParams p, List<Pt> entries, Func<Pt, bool> allow = null, Func<BState, bool> ok = null)
        {
            var t = p.Traits;
            Func<Pt, bool> Allowed(Func<Pt, bool> w) => allow == null ? w : w == null ? allow : pt => w(pt) && allow(pt);
            if (t.Contains("dar")) Scatter(B, entries, 3, c => c.T = "wall", Allowed(null), ok);
            if (t.Contains("acik"))
            {
                var walls = new List<Pt>();
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (CellAt(B, x, y).T == "wall") walls.Add(new Pt(x, y));
                for (int i = 0; i < 3 && walls.Count > 0; i++)
                {
                    int k = (int)Math.Floor(Rnd(B) * walls.Count);
                    CellAt(B, walls[k].X, walls[k].Y).T = "floor"; walls.RemoveAt(k);
                }
            }
            if (t.Contains("sebeke")) Scatter(B, entries, 3, c => c.T = "cable", Allowed(null), ok);
            if (t.Contains("sogutma")) Scatter(B, entries, 2, c => c.O = NewObj("cool", 1), Allowed(null), ok);
            if (t.Contains("kalabalik")) Scatter(B, entries, 2, c => c.O = NewObj("civ", Math.Max(1, p.CivilianHp)), Allowed(null), ok);
            if (t.Contains("fabrika")) Scatter(B, entries, 1, c => c.O = NewObj("spawner", 2), Allowed(pt => CanonX(B, pt) >= 4), ok);
        }

        static List<Pt> FreeTiles(BState B)
        {
            var o = new List<Pt>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                if (CellAt(B, x, y).T != "wall" && CellAt(B, x, y).O == null && UnitAt(B, x, y) == null) o.Add(new Pt(x, y));
            return o;
        }

        // ---------- Kurulum ----------
        /// <summary>
        /// Örnek/test tahtası: elle çizilmiş üç haritadan (<see cref="Layouts"/>) biri, kademe, bölge ve baskın tohumuyla. Kampanyada
        /// kullanılmaz (harekât baskını tesis odalarında oynanır, <see cref="CreateRoom"/>); örnek baskın, editör önizlemesi ve motor
        /// testleri içindir. <see cref="RaidParams.MapSeed"/> ve <see cref="RaidParams.Rooms"/> okunmaz; oda kuralları
        /// (<see cref="BState.Room"/>, <see cref="BState.Moves"/>) null kalır.
        /// </summary>
        public static BState Create(RaidParams p)
        {
            var B = new BState
            {
                Params = p, Rs = p.Seed,
                MaxTurns = p.Traits.Contains("kisa") ? 5 : 6,
                SpawnPerTurn = p.Traits.Contains("kopya") ? 2 : 1,
                IsCore = p.IsCore && p.MissionKind != "loot",
                Signature = string.IsNullOrEmpty(p.Signature) ? null : p.Signature,
                EnemyHp = p.Traits.Contains("guclu") ? 1 : 0,
                EnemyMove = p.Traits.Contains("devriye") ? 1 : 0,
                Fog = p.Traits.Contains("sis"),
                Smart = p.Tactics > 0,
            };
            Rnd(B);
            int cabinet = p.Traits.Contains("zirhli") ? 1 : 0;
            // Örnek/test tahtası: elle çizilmiş üç harita, kademe, bölge ve baskın tohumuyla
            var L = Layouts[(p.Compute + p.RaidNumber + (p.Seed & 7)) % Layouts.Length];
            B.Layout = L.Name;
            var entries = new List<Pt>();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    char ch = L.Rows[y][x];
                    var c = new Cell { T = ch == '#' ? "wall" : ch == '~' ? "cable" : "floor" };
                    if (ch == 'C') c.O = NewObj(B.IsCore ? "core" : "node", (B.IsCore ? 5 : 4) + cabinet + (IsLoot(B) || B.IsCore ? 0 : p.TargetHpBonus));
                    else if (ch == 'Y' || ch == 'S') c.O = NewObj("civ", Math.Max(1, p.CivilianHp));
                    else if (ch == 'K') c.O = NewObj("cool", 1);
                    else if (ch == 'U') c.O = NewObj("spawner", 2);
                    B.Cells[y, x] = c;
                    if (ch == 'E') entries.Add(new Pt(x, y));
                    if (ch == 'd') B.Units.Add(NewUnit(B, "dron", x, y));
                    if (ch == 'm') B.Units.Add(NewUnit(B, "bakim", x, y));
                }
            ApplyRegionTraits(B, p, entries);
            B.Exits = new List<Pt>(entries);
            var squad = SquadOf(p);
            for (int i = 0; i < squad.Count && i < entries.Count; i++) B.Units.Add(SquadUnit(B, p, squad[i], entries[i].X, entries[i].Y));
            AddTierMachines(B, p);
            if (B.Signature != null) ApplySignature(B, p, p.Tier >= 3 ? 2 : 1);
            // Bölge olayı: bütün RNG çekimlerinden sonra kurulur ve RNG çekmez (olaysız baskın aynı kalır); saha raporu telsize düşer
            if (!string.IsNullOrEmpty(p.Event) && RegionEvents.ContainsKey(p.Event)) EventSetup(B, p, L.Rows);
            SayOpening(B, p);
            PlanEnemies(B);
            // Güvenli giriş: ilk tur uyanma yok
            if (!p.Traits.Contains("guvenli")) PlaceSpawns(B);
            B.FxList.Clear();
            return B;
        }

        /// <summary>Baskının açılış satırı: ikmal görevinde kural, sunucu görevinde YZ'nin sözü (tesis odasında yalnız hedef oda).</summary>
        static void SayOpening(BState B, RaidParams p)
        {
            string tgt = B.IsCore ? Loc.T("çekirdeğimin") : Loc.T("sunucularımın");
            Say(B, IsLoot(B) ? Loc.T("İkmal kasasını herhangi bir uzmanla bitişik kareden aç; sonra bütün ekip girişten çıkmalı. Kasayı imha etme.") : p.Tier > 1 ? Loc.T("Yine sen. Bu sefer kapıda daha çok makine var.") : Loc.F("Demek {0} yerini buldun. Bakalım içeri girebilecek misin.", tgt), IsLoot(B) ? "sys" : "ai");
        }

        /// <summary>Kadro sığınaktan gelir (yaralı uzman eksik canla, yedek acemi, takılı plaka); verilmezse tam ve sağlam üç uzman.</summary>
        internal static List<SquadSlot> SquadOf(RaidParams p) => p.Squad != null && p.Squad.Count > 0 ? p.Squad
            : new[] { "tek", "emp", "ana" }.Select(t => new SquadSlot { Type = t, Hp = UnitTypes[t].Hp }).ToList();

        /// <summary>Kadro üyesinin birimi (can kırpılır, yürüyüş, güç, kişi, EMP, plaka, yetenek hakkı); tahtaya eklenmez.</summary>
        static Unit SquadUnit(BState B, RaidParams p, SquadSlot s, int x, int y)
        {
            var u = NewUnit(B, s.Type, x, y);
            if (s.MaxHp > 0) u.Max = s.MaxHp;
            u.Hp = Math.Max(1, Math.Min(u.Max, s.Hp));
            u.MoveRange = s.Move;
            u.Power = s.Power;
            u.PersonId = s.PersonId;
            u.PersonName = s.PersonName;
            u.EmpBoosts = s.EmpBoosts;
            u.EmpBoostDamage = s.EmpBoostDamage;
            u.Armor = Math.Max(0, s.Armor);
            u.Charges = StartCharges(p, u.Type);
            if (B.Moves?.Titles == true) u.Titles = s.Titles;
            // Tesis aşama 8: yardımcı yuvadaki ekipman ve baskın hakkı (yalnız tesis odasında; örnek tahtada ekipman yok)
            if (B.Room != null && !string.IsNullOrEmpty(s.Gear)) { u.Gear = s.Gear; u.GearLeft = s.GearCharges; }
            return u;
        }

        /// <summary>
        /// Zorluk kademesi: kademe başına bir makine fazla (takviye bölgesinde ve sertleştirilmiş sunucu görevinde fazladan), uzak
        /// yarıda (<see cref="TierExtra"/>). <paramref name="allow"/> (tesis odası) aday kareleri süzer; verilmezse bugünkü baskınla aynı.
        /// </summary>
        /// <paramref name="less"/>: tesis hazırlığı (çağrı anteni kırık) eki bu kadar azaltır; örnek tahtada 0.
        static void AddTierMachines(BState B, RaidParams p, Func<Pt, bool> allow = null, int less = 0)
        {
            int extra = Math.Max(0, TierExtra(p) - less);
            for (int i = 0; i < extra && Machines(B) < MaxEnemies; i++)
            {
                var spot = FreeTiles(B).Where(t => CanonX(B, t) >= 4 && (allow == null || allow(t))).OrderByDescending(t => CanonX(B, t)).ToList();
                if (spot.Count > 0)
                {
                    var t = spot[(int)Math.Floor(Rnd(B) * Math.Min(6, spot.Count))];
                    B.Units.Add(NewUnit(B, WakeType(B, Rnd(B)), t.X, t.Y));
                }
            }
        }

        /// <summary>İmza düşmanı: tahtada en az <paramref name="want"/> bölge makinesi olur; eksikse son eklenen makineler döner.</summary>
        static void ApplySignature(BState B, RaidParams p, int want)
        {
            var foes = Alive(B, 'e').Where(u => u.Type != B.Signature).ToList();
            int have = Alive(B, 'e').Count(u => u.Type == B.Signature);
            int convert = Math.Min(foes.Count, Math.Max(0, want - have));
            for (int i = 0; i < convert; i++)
            {
                var u = foes[foes.Count - 1 - i];
                var T = UnitTypes[B.Signature];
                u.Type = B.Signature; u.Hp = u.Max = T.Hp + B.EnemyHp; u.Armor = T.Armor;
            }
        }

        /// <summary>Sınırlı yeteneğin baskın başına hakkı (kampanya dengeden verir); öteki uzmanlarda sınırsız.</summary>
        static int StartCharges(RaidParams p, string type) =>
            type == "maden" ? Math.Max(0, p.DynamiteCharges) : type == "duman" ? Math.Max(0, p.SmokeCharges) : -1;

        /// <summary>
        /// Kadro raporunun satırı: baskına giren uzman (canı tahtadaki gibi kırpılmış), yatan uzmanın yerine giren yedek
        /// (<see cref="Rookie"/>) ya da yedeği olmadığı için boş kalan rol (<see cref="Missing"/>).
        /// </summary>
        public sealed class SquadEntry
        {
            public string Type; public int Hp, Max, Armor; public bool Rookie, Missing;
            /// <summary>Asıl uzman tam iyileşmeden girdi (yedeğin eksik canı yara değildir).</summary>
            public bool Wounded => !Missing && !Rookie && Hp < Max;
        }

        /// <summary>
        /// Baskına giren kadronun durumu, kadro sırasıyla: seçilen roller (<see cref="RaidParams.Roles"/>; yoksa kadronun
        /// türleri, üçten azsa ekip kartları gibi Teknisyen, EMP uzmanı ve Analist'le tamamlanır; kadro da yoksa tam ve sağlam
        /// üç uzman). RNG kullanmaz; görev fişinin kadro raporu (baskın emrinden önce) okur.
        /// </summary>
        public static List<SquadEntry> SquadReport(RaidParams p)
        {
            var squad = p.Squad != null && p.Squad.Count > 0 ? p.Squad
                : new[] { "tek", "emp", "ana" }.Select(t => new SquadSlot { Type = t, Hp = UnitTypes[t].Hp }).ToList();
            var roles = p.Roles != null && p.Roles.Count > 0 ? p.Roles : squad.Select(s => s.Type).ToList();
            if (p.Roles == null || p.Roles.Count == 0)
                foreach (var r in new[] { "tek", "emp", "ana" }) if (roles.Count < 3 && !roles.Contains(r)) roles.Add(r);
            var list = new List<SquadEntry>();
            foreach (var role in Simulation.Roles.Where(roles.Contains))
            {
                var slot = squad.FirstOrDefault(s => s.Type == role);
                int max = slot != null && slot.MaxHp > 0 ? slot.MaxHp : UnitTypes[role].Hp;
                list.Add(slot == null
                    ? new SquadEntry { Type = role, Max = max, Missing = true }
                    : new SquadEntry { Type = role, Hp = Math.Max(1, Math.Min(max, slot.Hp)), Max = max, Armor = Math.Max(0, slot.Armor), Rookie = slot.Rookie });
            }
            return list;
        }

        // ---------- Hasar, itme ----------
        static void Hurt(BState B, Unit u, int n, string why)
        {
            if (u.Dead || n <= 0) return;
            // Hasar tavanı: olaylardan (zinciri dahil) birim son olay adımından beri en çok 1 hasar alır
            if (B.InEvent && B.Ev != null)
            {
                if (B.Ev.Hurt.Contains(u.Id)) return;
                B.Ev.Hurt.Add(u.Id);
                n = 1;
            }
            if (u.Ice > 0)
            {
                // Buz kabuğu (kriyo hattı) hasarı plakadan önce karşılar
                int a = Math.Min(u.Ice, n);
                u.Ice -= a; n -= a;
                AddFx(B, new Fx { K = "ice", X = u.X, Y = u.Y, Unit = u.Id });
                Say(B, Loc.F("{0}: buz kabuğu darbeyi karşıladı.", UnitTypes[u.Type].Name), "sys");
                if (n <= 0) return;
            }
            if (u.Armor > 0)
            {
                // Zırh plakası ilk hasarı karşılar ve kırılır
                int a = Math.Min(u.Armor, n);
                u.Armor -= a; n -= a;
                AddFx(B, new Fx { K = "plate", X = u.X, Y = u.Y, Unit = u.Id });
                Say(B, Loc.F("{0}: zırh plakası darbeyi karşıladı ve kırıldı.", UnitTypes[u.Type].Name), "sys");
                if (n <= 0) return;
            }
            u.Hp -= n;
            AddFx(B, new Fx { K = "dmg", X = u.X, Y = u.Y, N = n, Unit = u.Id });
            if (B.InEvent) { if (IsSpecialist(u)) B.EvDamageTeam += n; else if (u.Side == 'e') B.EvDamageMachines += n; }
            if (u.Hp > 0) return;
            u.Dead = true; u.Hp = 0;
            // Tesis aşama 8: kırılan aygıt hurdaya döner, köpek tahtadan çekilir (gövde değildir); tutsak makine gibi devre dışı
            if (IsGadget(u)) { GadgetDown(B, u, true); return; }
            AddFx(B, new Fx { K = IsDog(u) ? "dog-down" : "die", X = u.X, Y = u.Y, Unit = u.Id });
            string w = why != null ? " (" + why + ")" : "";
            if (IsDog(u)) Say(B, Loc.T("Köpek yaralandı: tahtadan çekildi."), "bad");
            else if (IsSpecialist(u)) Say(B, Loc.F("{0} yere düştü{1}.", UnitTypes[u.Type].Name, w), "bad");
            else Say(B, Loc.F("{0} devre dışı{1}.", UnitTypes[u.Type].Name, w), "good");
            MachineScrap(B, u);
            if (DropCargo(B, u)) return;
            // Kaçışta taşıyıcı düştü: malzeme gitti, tahtada kalanlar yine de çıkmalı (baskın tahtada uzman kalmayınca biter)
            if (u.Id == B.Carrier && B.Phase == "player")
                Say(B, Alive(B, 'p').Any(IsSpecialist) ? Loc.F("{0} düştü: ikmal malzemeleri kaybedildi. Ekibin geri kalanı yine de çıkmalı.", UnitTypes[u.Type].Name) : Loc.F("{0} düştü: ikmal malzemeleri kaybedildi.", UnitTypes[u.Type].Name), "bad");
        }

        /// <summary>
        /// Tesis odası: taşıyıcı düşünce yük düştüğü kareye düşer; kareye kendi hareketiyle giren ilk ekip üyesi alır (aşama 6). Kare
        /// geçilemiyorsa (raf kaymasının duvarı) yük en yakın açık kareye düşer (çekimsiz; dönüş tahtasının gövde kuralı). Aşama 12: Katar
        /// kenarından düşen taşıyıcının yükü son karesinde kalır (<see cref="FallOff"/>). Taşıyıcı değilse ya da tesis ikmali değilse false.
        /// </summary>
        static bool DropCargo(BState B, Unit u)
        {
            if (u.Id != B.Carrier || B.Room == null || !IsLoot(B)) return false;
            B.Carrier = 0;
            var at = new Pt(u.X, u.Y);
            if (!Passable(B, at.X, at.Y)) at = NearestOpen(B, at, new HashSet<int>(), new HashSet<int>()) ?? at;
            B.CargoX = at.X; B.CargoY = at.Y;
            AddFx(B, new Fx { K = "cargo-drop", X = at.X, Y = at.Y, Unit = u.Id });
            Say(B, Loc.F("{0} düştü: yük yere düştü. Kareye giren ilk ekip üyesi alır.", UnitTypes[u.Type].Name), "bad");
            return true;
        }

        /// <param name="tech">Teknisyen'in sabotajı: soğutma vanasını yalnız o kapatır (tesis odası).</param>
        static void HurtObj(BState B, int x, int y, int n, bool tech = false)
        {
            var c = CellAt(B, x, y);
            if (c.O == null || n <= 0) return;
            // Olay ve zinciri nesnelere işlemez; basınçlı tüp ve kaynak kolu darbeyi kendi kuralıyla karşılar
            if (B.Ev != null && (B.InEvent || EventObjectHit(B, x, y, c.O))) return;
            var o = c.O;
            // Soğutma vanasını yalnız Teknisyen'in sabotajı kapatır (örnek tahtada vana yok)
            if (o.K == "vana" && !tech)
            {
                AddFx(B, new Fx { K = "armor", X = x, Y = y });
                Say(B, Loc.T("Soğutma vanasını yalnız Teknisyen kapatabilir."), "sys");
                return;
            }
            o.Hp -= n;
            AddFx(B, new Fx { K = "dmg", X = x, Y = y, N = n, Obj = o.K });
            if (o.Hp > 0) return;
            c.O = null;
            AddFx(B, new Fx { K = "break", X = x, Y = y, Obj = o.K });
            if (o.K == "civ") { B.Collateral++; Say(B, Loc.T("Sivil sunucu kırıldı: hastane ve banka hizmetleri kesildi."), "bad"); }
            else if (o.K == "spawner") Say(B, Loc.T("Kopya dolabı kırıldı: oradan robot uyanmayacak."), "good");
            else if (o.K == "cool")
            {
                Say(B, Loc.T("Soğutma ünitesi patladı."), "sys");
                foreach (var d in Dirs) { Blasted(B, x + d[0], y + d[1]); DamageAt(B, x + d[0], y + d[1], 1, Loc.T("patlama")); }
            }
            else if (IsTarget(o.K))
            {
                if (IsLoot(B)) Lose(B, Loc.T("İkmal kasası imha edildi. Malzemeler kurtarılamadı."));
                else { Say(B, Loc.F("{0} kırıldı.", Loc.T(ObjNames[o.K])), "good"); CheckWin(B); }
            }
            else if (IsDevice(o.K)) Say(B, B.Defense ? Loc.F("{0} kırıldı.", Loc.T(ObjNames[o.K])) : Loc.F("{0} devre dışı: etkisi hedef odasında görülecek.", Loc.T(ObjNames[o.K])), "good");
            // Aşama 11: saha rölesi kırıldı, savunma kaybedildi (her kaynak kırar)
            else if (o.K == RelayKind) RelayBroken(B, x, y);
            else if (o.K == "yayici") Say(B, Loc.F("{0} kırıldı.", Loc.T(ObjNames[o.K])), "good");
        }

        static void DamageAt(BState B, int x, int y, int n, string why)
        {
            if (!Inb(x, y)) return;
            var u = UnitAt(B, x, y);
            if (u != null) Hurt(B, u, n, why);
            else if (CellAt(B, x, y).O != null) HurtObj(B, x, y, n);
        }

        /// <summary>
        /// Birimi bir kare iter (sabotaj, EMP, koşu darbesi, tüp; aşama 13'te tampon): cıvatalı makine tutar; Katar'da kuzey ya da güney
        /// dışına itilen düşer (<see cref="FallOff"/>; o itme için "push" Fx'i yazılmaz); turnike, duvar ve tahta kenarı, nesne ve birim
        /// çarpması 1 hasar; sonra kablo ve yağ. <paramref name="slide"/> (aşama 12, Yalpa'nın yatışı, <see cref="EndTilt"/>): cıvatalı
        /// makine sessizce tutar (Fx ve telsiz yok), "push" yerine "slide" Fx'i (N: 0 çarptı, 1 kaydı) yazılır; kural öbür hâllerde aynı.
        /// </summary>
        static void Push(BState B, Unit u, int[] d, bool slide = false)
        {
            if (u.Dead) return;
            // Tesis aşama 10: cıvatalı ayaklı makine yerinden oynamaz (çarpma hasarı da yok); Yalpa'nın kaymasında sessizce tutar
            if (Bolted(B, u)) { if (!slide) BoltHold(B, u); return; }
            int nx = u.X + d[0], ny = u.Y + d[1];
            // Aşama 12: Katar'ın açık kenarı (kuzey ve güney; cıvatadan sonra, turnike ve duvardan önce)
            if (!Inb(nx, ny) && IsOpenEdgeRow(B, ny)) { FallOff(B, u, d); return; }
            var fx = new Fx { K = slide ? "slide" : "push", X = u.X, Y = u.Y, Dx = d[0], Dy = d[1], Unit = u.Id };
            AddFx(B, fx);
            // Turnike: ters yönde itilen kenara çarpar ve yerinde kalır (olay hasarı)
            if (B.Ev != null && Inb(nx, ny) && !CanStep(B, u.X, u.Y, nx, ny)) { EventHurt(B, u, Loc.T("turnike")); return; }
            if (!Inb(nx, ny) || CellAt(B, nx, ny).T == "wall") { Hurt(B, u, 1, Loc.T("duvara çarptı")); Struck(B, u.X, u.Y, nx, ny); return; }
            if (CellAt(B, nx, ny).O != null) { Hurt(B, u, 1, IsBarrier(CellAt(B, nx, ny).O.K) ? Loc.T("engele çarptı") : Loc.T("dolaba çarptı")); HurtObj(B, nx, ny, 1); Struck(B, u.X, u.Y, nx, ny); return; }
            var o = UnitAt(B, nx, ny);
            if (o != null) { Hurt(B, u, 1, Loc.T("çarpışma")); Hurt(B, o, 1, Loc.T("çarpışma")); Struck(B, u.X, u.Y, nx, ny); return; }
            u.X = nx; u.Y = ny;
            if (slide) fx.N = 1;
            CableHurt(B, u);
            Slide(B, u, d);
        }

        /// <summary>Yalnız testler: gerçek hasar yolu (buz, plaka, düşüş, yükün düşmesi).</summary>
        internal static void TestHurt(BState B, Unit u, int n) => Hurt(B, u, n, null);

        /// <summary>Uzman bir çıkıştan tahliye edildi: tahtadan kalkar (Out).</summary>
        static void Evacuate(BState B, Unit u)
        {
            u.Out = true;
            AddFx(B, new Fx { K = "evac", X = u.X, Y = u.Y, Unit = u.Id });
            Say(B, Loc.F("{0} tahliye edildi.", UnitTypes[u.Type].Name), "good");
        }

        /// <summary>
        /// Her hamleden sonra: kaçışta bir çıkışta duran uzman tahliye edilir (yürüyerek, itilerek ya da kaçış başlarken orada
        /// durarak; tesis odasında bunun yerine kapı kuralı, <see cref="GateIn"/>). Tahtada uzman kalmadıysa (kabindeki ve kapıdaki
        /// baskında sayılır) kaçışta baskın biter (<see cref="FinishEscape"/>), kaçıştan önce kaybedilir.
        /// </summary>
        static void CheckEscape(BState B)
        {
            if (B.Phase != "player") return;
            if (B.Escape && B.Room == null)
                foreach (var u in Alive(B, 'p').Where(IsSpecialist))
                {
                    if (!B.Exits.Any(p => p.X == u.X && p.Y == u.Y)) continue;
                    Evacuate(B, u);
                }
            // Asansör kabinindeki ve tesis kapısında bekleyen uzman tahtada değil ama baskında. Köpek, aygıt ve tutsak sayılmaz (aşama 8)
            if (Alive(B, 'p').Any(IsSpecialist) || B.Units.Any(u => IsSpecialist(u) && !u.Dead && (u.Cab || u.AtGate))) return;
            if (B.Escape) FinishEscape(B);
            else Lose(B, Loc.T("Ekibin tamamı yere düştü."));
        }

        /// <summary>Hedef dolapların hepsi kırıldı: sunucu görevinde kaçış başlar (ikmal görevinde kasa açılınca başlar).</summary>
        static void CheckWin(BState B)
        {
            // Aşama 11: savunmada hedef ve kaçış yoktur
            if (B.Defense) return;
            if (TargetsLeft(B).Count > 0 || IsLoot(B) || B.Escape) return;
            B.Escape = true;
            Say(B, Loc.T("Hedef kırıldı. Şimdi bütün ekip girişten çıkmalı: tahtada kalan uzman baskında düşmüş sayılır."), "good");
        }

        /// <summary>
        /// Kaçışın sonu (tahtada uzman kalmadı ya da süre doldu): sunucu görevi hedef kırıldığı için kazanılır, ikmal görevi
        /// malzemeyi taşıyan Teknisyen çıktıysa. Çıkamayan uzmanlar <see cref="EndTurn"/>'de düşmüş sayılır.
        /// </summary>
        static void FinishEscape(BState B)
        {
            var carrier = B.Carrier != 0 ? ById(B, B.Carrier) : null;
            bool won = !IsLoot(B) || carrier != null && carrier.Out;
            // Tesis odasının tahtası önceki odalarda düşenleri taşımaz: kadro sayılır
            // Tesisin girişinden sürüklenerek çıkarılan düşen (Dead + Out) çıkan uzman sayılmaz (örnek tahtada Dead + Out yok)
            int all = B.Room != null ? SquadOf(B.Params).Count : B.Units.Count(IsSpecialist), gone = B.Units.Count(u => IsSpecialist(u) && u.Out && !u.Dead);
            bool full = gone == all;
            B.Phase = won ? "won" : "lost";
            B.OutcomeSuccess = won;
            B.OutcomeReason = !won
                ? carrier != null && carrier.Left ? Loc.T("Süre doldu: ekip malzemelerle çıkamadı.")
                : IsDog(carrier) && !carrier.Dead ? Loc.T("Köpek yükle tahtada kaldı: ikmal malzemeleri kaybedildi.")
                : carrier == null && B.Room != null ? Loc.T("Yük odada kaldı: ikmal malzemeleri kaybedildi.")
                : carrier != null ? Loc.F("{0} düştü: ikmal malzemeleri kaybedildi.", UnitTypes[carrier.Type].Name) : Loc.T("Yük kayboldu: ikmal malzemeleri kaybedildi.")
                : IsLoot(B) ? full ? Loc.T("İkmal malzemeleri alındı. Ekip tahliye edildi.") : Loc.F("İkmal malzemeleri alındı. Ekipten {0}/{1} uzman çıktı.", gone, all)
                : B.IsCore ? full ? Loc.T("Çekirdek yok edildi. Ekip tahliye edildi.") : Loc.F("Çekirdek yok edildi. Ekipten {0}/{1} uzman çıktı.", gone, all)
                : full ? Loc.T("YZ düğümü kırıldı. Ekip tahliye edildi.") : Loc.F("YZ düğümü kırıldı. Ekipten {0}/{1} uzman çıktı.", gone, all);
        }

        public static List<(int X, int Y, Obj O)> TargetsLeft(BState B)
        {
            var o = new List<(int, int, Obj)>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { var ob = CellAt(B, x, y).O; if (ob != null && IsTarget(ob.K)) o.Add((x, y, ob)); }
            return o;
        }

        // ---------- Hareket ----------
        /// <summary>Dost birimlerin içinden geçilebilir ama üstünde durulamaz; düşman, dolap ve duvar geçilmez.</summary>
        public static List<Pt> Reachable(BState B, Unit u)
        {
            var T = UnitTypes[u.Type];
            int move = (u.Side == 'p' && u.MoveRange > 0 ? u.MoveRange : T.Move) + (u.Side == 'e' ? B.EnemyMove : 0);
            var seen = new Dictionary<int, int> { [u.Y * N + u.X] = 0 };
            var q = new Queue<Pt>();
            q.Enqueue(new Pt(u.X, u.Y));
            var o = new List<Pt>();
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int d = seen[p.Y * N + p.X];
                if (d >= move) continue;
                foreach (var dd in Dirs)
                {
                    int nx = p.X + dd[0], ny = p.Y + dd[1];
                    if (!Inb(nx, ny) || seen.ContainsKey(ny * N + nx)) continue;
                    var c = CellAt(B, nx, ny);
                    if (c.T == "wall" || c.O != null) continue;
                    if (B.Ev != null && !CanStep(B, p.X, p.Y, nx, ny)) continue;
                    var other = UnitAt(B, nx, ny);
                    if (other != null && other.Side != u.Side) continue;
                    seen[ny * N + nx] = d + 1;
                    if (!Frosted(B, nx, ny)) q.Enqueue(new Pt(nx, ny));
                    if (other == null) o.Add(new Pt(nx, ny));
                }
            }
            return o;
        }

        // ---------- Oyuncu yetenekleri ----------
        public static List<Pt> Targets(BState B, Unit u)
        {
            var o = new List<Pt>();
            var ab = UnitTypes[u.Type].Ability;
            // Söndürme bulutu: buluta ve buluttan nişan alınmaz; bitişik nesneye iş (sabotaj) serbest
            bool clouded = InCloud(B, u.X, u.Y);
            if (ab == "sabotaj")
            {
                foreach (var d in Dirs)
                {
                    int x = u.X + d[0], y = u.Y + d[1];
                    if (!Inb(x, y)) continue;
                    if (UnitAt(B, x, y) != null ? !clouded && !InCloud(B, x, y) : CellAt(B, x, y).O != null) o.Add(new Pt(x, y));
                }
            }
            else if (ab == "emp")
            {
                if (!clouded)
                    foreach (var d in Dirs) for (int k = 2; k <= 5; k++) { int x = u.X + d[0] * k, y = u.Y + d[1] * k; if (Inb(x, y) && !InCloud(B, x, y)) o.Add(new Pt(x, y)); }
            }
            else if (ab == "kanca")
            {
                foreach (var d in Dirs) { var h = HookTrace(B, u, d); if (h != null) o.Add(new Pt(h.X, h.Y)); }
            }
            else if (ab == "dinamit")
            {
                if (!clouded)
                    foreach (var d in Dirs) { int x = u.X + d[0], y = u.Y + d[1]; if (Inb(x, y) && DynamiteTarget(B, u, x, y) && !InCloud(B, x, y)) o.Add(new Pt(x, y)); }
            }
            else if (ab == "duman" && u.Charges != 0)
            {
                foreach (var d in Dirs)
                    for (int k = 1; k <= SmokeRange; k++)
                    {
                        int x = u.X + d[0] * k, y = u.Y + d[1] * k;
                        if (Inb(x, y) && CellAt(B, x, y).T != "wall" && !CellAt(B, x, y).Smoke) o.Add(new Pt(x, y));
                    }
            }
            // İkmal kasası her uzmanın işidir: bitişik kasa yeteneğin hedefidir (sıra korunur, sona eklenir; buluttaki iş serbest)
            foreach (var d in Dirs)
            {
                int x = u.X + d[0], y = u.Y + d[1];
                if (CabinetAt(B, u, x, y) && !o.Any(t => t.X == x && t.Y == y)) o.Add(new Pt(x, y));
            }
            return o;
        }

        /// <summary>İkmal görevinde uzmanın bitişiğindeki kapalı ikmal kasası (köpek, tutsak, aygıt ve sızmacı açamaz).</summary>
        static bool CabinetAt(BState B, Unit u, int x, int y) =>
            IsLoot(B) && IsSpecialist(u) && Inb(x, y) && Man(u.X, u.Y, x, y) == 1 && UnitAt(B, x, y) == null
            && CellAt(B, x, y).O is Obj o && IsTarget(o.K);

        /// <summary>Kasayı açan uzman malzemeleri alır ve taşıyıcı olur; kaçış başlar. Yeteneğin hakkını harcar, şarj harcamaz.</summary>
        static void OpenCabinet(BState B, Unit u, int x, int y)
        {
            var cargo = CellAt(B, x, y).O;
            CellAt(B, x, y).O = null;
            B.Carrier = u.Id;
            B.Escape = true;
            AddFx(B, new Fx { K = "loot", X = x, Y = y, Obj = cargo.K });
            Say(B, Loc.F("{0} malzemeleri aldı. Şimdi bütün ekip girişten çıkmalı: tahtada kalan uzman baskında düşmüş sayılır.", UnitTypes[u.Type].Name), "good");
        }

        /// <summary>Sis bombasının menzili (düz hat, engellerin üstünden).</summary>
        const int SmokeRange = 4;

        /// <summary>Dinamit: bitişik duvar (hak kaldıysa), robot ya da sıradan nesne; hedef dolap, ikmal kasası ve dost uzman değil.
        /// Tesis odasının ağır kepengi ve büyük sandığın nişi iki kişilik iştir, dinamitle açılmaz (örnek tahtada bayraklar hep false).</summary>
        static bool DynamiteTarget(BState B, Unit u, int x, int y)
        {
            var wc = CellAt(B, x, y);
            if (wc.T == "wall") return u.Charges != 0 && !wc.Shutter && !wc.Crate && !wc.Double;
            var t = UnitAt(B, x, y);
            if (t != null) return t.Side == 'e';
            var o = CellAt(B, x, y).O;
            return o != null && !IsTarget(o.K);
        }

        sealed class Hit { public int X, Y, K; public bool Edge, None; }

        static Hit HookTrace(BState B, Unit u, int[] d)
        {
            // Kanca buluttan atılmaz, buluta girince kesilir
            if (InCloud(B, u.X, u.Y)) return null;
            for (int k = 1; k <= 5; k++)
            {
                int x = u.X + d[0] * k, y = u.Y + d[1] * k;
                if (!Inb(x, y)) return k > 1 ? new Hit { X = x - d[0], Y = y - d[1], K = k - 1, Edge = true } : null;
                if (InCloud(B, x, y)) return null;
                if (CellAt(B, x, y).T == "wall" || CellAt(B, x, y).O != null || UnitAt(B, x, y) != null) return new Hit { X = x, Y = y, K = k };
            }
            return null;
        }

        static void ApplyAbility(BState B, Unit u, int x, int y)
        {
            var ab = UnitTypes[u.Type].Ability;
            var d = new[] { Sign(x - u.X), Sign(y - u.Y) };
            // Aynı yetenek komutu ve önizleme yolu kullanılır; ikmal görevinde bitişik kasa kırılmaz, açılır (her uzman)
            if (CabinetAt(B, u, x, y)) { OpenCabinet(B, u, x, y); return; }
            if (ab == "sabotaj")
            {
                var t = UnitAt(B, x, y);
                if (t != null) { Hurt(B, t, Math.Max(1, u.Power), Loc.T("sabotaj")); Unbolt(B, t); Push(B, t, d); }
                else if (CellAt(B, x, y).O != null) HurtObj(B, x, y, 2, tech: true);
            }
            else if (ab == "emp")
            {
                MarkPulse(B, x, y);
                int empDamage = 1 + ApplyPlanningEmpBoost(B, u);
                AddFx(B, new Fx { K = "emp", X = x, Y = y });
                Fire(B, x, y);
                var o = CellAt(B, x, y).O;
                if (o != null && IsTarget(o.K) && UnitAt(B, x, y) == null) AddFx(B, new Fx { K = "armor", X = x, Y = y });
                // Tesis aşama 8: oyuncunun EMP'si aygıta işlemez (hasar ve itme yok)
                else if (!IsGadget(UnitAt(B, x, y))) DamageAt(B, x, y, empDamage, Loc.T("EMP"));
                foreach (var dd in Dirs) { var t = UnitAt(B, x + dd[0], y + dd[1]); if (t != null && !IsGadget(t)) Push(B, t, dd); }
            }
            else if (ab == "kanca")
            {
                var h = HookTrace(B, u, d);
                if (h == null) return;
                AddFx(B, new Fx { K = "hook", X = h.X, Y = h.Y });
                var t = !h.Edge ? UnitAt(B, h.X, h.Y) : null;
                if (t != null)
                {
                    // Kanca robota 1 hasar verir; sağ kalan robot ve dost uzman çekilir
                    if (t.Side != u.Side) Hurt(B, t, 1, Loc.T("kanca"));
                    // Tesis aşama 10: cıvatalı makine çekilmez (hasar yine 1)
                    if (!t.Dead && h.K > 1 && Bolted(B, t)) BoltHold(B, t);
                    else if (!t.Dead && h.K > 1)
                    {
                        // Turnikede ters yönde çekilen kenara çarpar
                        if (B.Ev != null && !CanStep(B, t.X, t.Y, t.X - d[0], t.Y - d[1])) EventHurt(B, t, Loc.T("turnike"));
                        else
                        {
                            t.X -= d[0]; t.Y -= d[1];
                            AddFx(B, new Fx { K = "pull", Unit = t.Id });
                            CableHurt(B, t);
                            Slide(B, t, new[] { -d[0], -d[1] });
                        }
                    }
                }
                else if (h.K > 1 || h.Edge)
                {
                    int tx = h.Edge ? h.X : h.X - d[0], ty = h.Edge ? h.Y : h.Y - d[1];
                    // Atılma turnikenin kapalı kenarında durur
                    bool gate = false;
                    if (B.Ev != null)
                        for (int cx = u.X, cy = u.Y; !(cx == tx && cy == ty); cx += d[0], cy += d[1])
                            if (!CanStep(B, cx, cy, cx + d[0], cy + d[1])) { tx = cx; ty = cy; gate = true; break; }
                    if (!(tx == u.X && ty == u.Y)) { u.X = tx; u.Y = ty; CableHurt(B, u); Slide(B, u, d); }
                    if (gate) EventHurt(B, u, Loc.T("turnike"));
                }
            }
            else if (ab == "dinamit")
            {
                AddFx(B, new Fx { K = "dynamite", X = x, Y = y, Unit = u.Id });
                var cell = CellAt(B, x, y);
                if (cell.T != "wall") { DamageAt(B, x, y, 1, Loc.T("dinamit")); return; }
                if (u.Charges > 0) u.Charges--;
                Breach(B, x, y, d, Loc.T("Madenci duvarı yıktı."));
            }
            else if (ab == "duman")
            {
                CellAt(B, x, y).Smoke = true;
                if (u.Charges > 0) u.Charges--;
                AddFx(B, new Fx { K = "smoke", X = x, Y = y, Unit = u.Id });
            }
        }

        /// <summary>
        /// Duvar yıkılır (dinamit ve aşama 8'in dinamit lokumu; hak sayacı çağıranda): kalıcı zemin, moloz izi, telsiz satırı
        /// (<paramref name="say"/>), raf ve tavan, moloz bir kare öteye sıçrar (duvarda ve tahta kenarında durur, hedef dolaba ve ikmal
        /// kasasına işlemez). RNG çekmez.
        /// </summary>
        static void Breach(BState B, int x, int y, int[] d, string say)
        {
            CellAt(B, x, y).T = "floor";
            B.Rubble.Add(new Pt(x, y));
            AddFx(B, new Fx { K = "breach", X = x, Y = y, Dx = d[0], Dy = d[1] });
            Say(B, say, "good");
            ShelfBroken(B, x, y);
            Struck(B, x, y, x, y);
            int rx = x + d[0], ry = y + d[1];
            if (!Inb(rx, ry) || CellAt(B, rx, ry).T == "wall") return;
            Struck(B, rx, ry, rx, ry);
            AddFx(B, new Fx { K = "debris", X = rx, Y = ry, Dx = d[0], Dy = d[1] });
            var v = UnitAt(B, rx, ry);
            var ro = CellAt(B, rx, ry).O;
            if (v != null) Hurt(B, v, 1, Loc.T("moloz"));
            else if (ro != null && !IsTarget(ro.K)) HurtObj(B, rx, ry, 1);
        }

        static void Snapshot(BState B) => B.Hist.Add(B.Clone());

        /// <summary>Ekip birimi yürüyebilir / eylem yapabilir mi (aygıt hiçbirini yapamaz, aşama 8).</summary>
        public static bool CanMove(BState B, Unit u) => B.Phase == "player" && OnBoard(u) && u.Side == 'p' && !IsGadget(u) && !u.Moved && !u.Acted;
        public static bool CanAct(BState B, Unit u) => B.Phase == "player" && OnBoard(u) && u.Side == 'p' && !IsGadget(u) && !u.Acted;
        static Unit ById(BState B, int id) => B.Units.FirstOrDefault(v => v.Id == id);

        public static bool DoMove(BState B, int uid, int x, int y)
        {
            var u = ById(B, uid);
            if (u == null || !CanMove(B, u)) return false;
            if (!Reachable(B, u).Any(t => t.X == x && t.Y == y)) return false;
            Snapshot(B);
            u.X = x; u.Y = y; u.Moved = true;
            // Tesis odası: yerdeki yükün karesine yürüyen alır (kapı karesindeyse yükle kapıya girer)
            PickCargo(B, u);
            // Tesis odası: kendi yürüyüşüyle kapı karesine giren uzman kapıya girer (tesisin girişinde tahliye olur). Köpek kapıya
            // girmez, yalnız tesisin girişinde tahliye olur; tutsak hiç (aşama 8)
            if (IsGateTile(B, x, y))
            {
                if (IsSpecialist(u)) GateIn(B, u);
                else if (IsDog(u) && IsFinalExit(B)) Evacuate(B, u);
            }
            CheckEscape(B);
            return true;
        }

        public static bool DoAct(BState B, int uid, int x, int y)
        {
            var u = ById(B, uid);
            if (u == null || !CanAct(B, u)) return false;
            if (!Targets(B, u).Any(t => t.X == x && t.Y == y)) return false;
            Snapshot(B);
            B.FxList.Clear();
            ApplyAbility(B, u, x, y);
            u.Acted = true; u.Moved = true;
            CheckEscape(B);
            return true;
        }

        /// <summary>Turdaki son hamleyi geri alır. Durum yerinde değiştirilir.</summary>
        public static bool Undo(BState B)
        {
            if (B.Hist.Count == 0 || B.Phase != "player") return false;
            var prev = B.Hist[B.Hist.Count - 1];
            var hist = B.Hist;
            hist.RemoveAt(hist.Count - 1);
            CopyInto(prev, B);
            B.Hist = hist;
            return true;
        }

        static void CopyInto(BState src, BState dst)
        {
            var c = src.Clone();
            dst.Params = c.Params; dst.Rs = c.Rs; dst.Turn = c.Turn; dst.MaxTurns = c.MaxTurns; dst.SpawnPerTurn = c.SpawnPerTurn;
            dst.Collateral = c.Collateral; dst.NextId = c.NextId; dst.Phase = c.Phase; dst.Layout = c.Layout; dst.OutcomeReason = c.OutcomeReason;
            dst.IsCore = c.IsCore; dst.OutcomeSuccess = c.OutcomeSuccess; dst.Retreated = c.Retreated; dst.Escape = c.Escape;
            dst.Signature = c.Signature; dst.EnemyHp = c.EnemyHp; dst.EnemyMove = c.EnemyMove; dst.Fog = c.Fog;
            dst.Carrier = c.Carrier; dst.Exits = c.Exits; dst.Smart = c.Smart; dst.Rubble = c.Rubble;
            dst.Cells = c.Cells; dst.Units = c.Units; dst.Spawns = c.Spawns; dst.Log = c.Log; dst.FxList = c.FxList;
            dst.Ev = c.Ev; dst.InEvent = c.InEvent; dst.EvDamageTeam = c.EvDamageTeam; dst.EvDamageMachines = c.EvDamageMachines;
            dst.Room = c.Room; dst.PlanRoom = c.PlanRoom; dst.QuietTurns = c.QuietTurns; dst.StillTurns = c.StillTurns;
            dst.Moves = c.Moves; dst.CargoX = c.CargoX; dst.CargoY = c.CargoY; dst.HoldId = c.HoldId; dst.HoldX = c.HoldX; dst.HoldY = c.HoldY;
            dst.CrateOpen = c.CrateOpen; dst.Sniffed = c.Sniffed;
            dst.TallyPush = c.TallyPush; dst.TallyBreach = c.TallyBreach; dst.TallyBuild = c.TallyBuild; dst.TallyCapture = c.TallyCapture;
            dst.TallyDog = c.TallyDog; dst.TallyGadget = c.TallyGadget;
            dst.Defense = c.Defense; dst.StartTurn = c.StartTurn; dst.Abandoned = c.Abandoned; dst.Incoming = c.Incoming; dst.Waves = c.Waves;
            dst.Decoupled = c.Decoupled;
        }

        public sealed class PreviewResult
        {
            public List<Fx> Fx;
            public List<(int Unit, Pt From, Pt To)> Moves;
            public int Collateral;
            /// <summary>Won: hamle baskını bitirir (son uzman çıkar); Escape: hamle kaçışı başlatır (kasa açılır, son hedef kırılır).</summary>
            public bool Won, Escape;
            /// <summary>Hamleden sonra makinelerin niyetleri (sise kesilen atış, dinamitin açtığı hat).</summary>
            public List<IntentView> Intents;
            /// <summary>Hamleden sonra bölge olayının işaretleri ve birim uyarıları (olay yoksa boş).</summary>
            public HazardView Hazards;
            /// <summary>Tesis hamleleri (<see cref="PreviewAction"/>): pusu hattının kareleri (yoksa boş), iki kişilik iş bu hamlede
            /// açılıyor mu, hamleden sonraki taşıyıcı (birim kimliği; 0 yok).</summary>
            public List<Pt> Line = new List<Pt>();
            public bool Opens;
            public int Carrier;
        }

        /// <summary>Bir yeteneğin sonucu: durumun kopyasında uygulanır, değişen her şey listelenir.</summary>
        public static PreviewResult Preview(BState B, int uid, int x, int y)
        {
            var C = B.Clone();
            C.FxList.Clear();
            var u = ById(C, uid);
            ApplyAbility(C, u, x, y);
            CheckEscape(C);
            return Diff(B, C);
        }

        /// <summary>Hamleden önceki (<paramref name="B"/>) ve sonraki (<paramref name="C"/>, kopya) durumun farkı: önizlemenin gövdesi.</summary>
        static PreviewResult Diff(BState B, BState C)
        {
            var moves = new List<(int, Pt, Pt)>();
            foreach (var cu in C.Units)
            {
                var ou = ById(B, cu.Id);
                if (ou != null && (ou.X != cu.X || ou.Y != cu.Y)) moves.Add((cu.Id, new Pt(ou.X, ou.Y), new Pt(cu.X, cu.Y)));
            }
            return new PreviewResult { Fx = C.FxList, Moves = moves, Collateral = C.Collateral - B.Collateral, Won = C.Phase == "won", Escape = C.Escape && !B.Escape, Intents = Baskin.Intents(C), Hazards = Hazards(C), Carrier = C.Carrier };
        }

        // ---------- Düşman ----------
        /// <summary>Smoked: niyet sise kesildi (None ile birlikte; X, Y sisli kare). Cloud: söndürme bulutuna kesildi ya da
        /// makine buluttan nişan alamıyor.</summary>
        sealed class ITarget { public int X, Y, K; public bool None, Smoked, Cloud; }

        /// <summary>Kilitleyicinin ve havanın menzili.</summary>
        const int LockRange = 4, LobRange = 3;

        /// <summary>
        /// Niyetin vuracağı kare: ateş ve kilit düz hatta ilk engele (kilit en çok 4 kare), havan 3 kare öteye engellerin
        /// üstünden, patlama makinenin kendi karesine (çevresi <see cref="BlastArea"/>), öteki niyetler bitişik kareye.
        /// Sis nişanlı ateşi keser: atış ve kilit sise girince, yakın vuruş ve havan sisli kareye kesilir (Smoked). Sis yalnız
        /// makine evresinde durur, plan onu görmez.
        /// </summary>
        static ITarget IntentTarget(BState B, Unit e, string kind, int dir, int px, int py)
        {
            int dx = Dirs[dir][0], dy = Dirs[dir][1];
            if (IsTurncoat(e) && e.Intent?.Kind == kind) return FieldTarget(B, e, dir);
            if (IsSignatureIntent(kind)) return SignatureTarget(B, e, kind, dir, px, py);
            if (kind == "shoot" || kind == "lock")
            {
                int range = kind == "lock" ? LockRange : N;
                // Söndürme bulutundan nişan alınmaz
                if (InCloud(B, px, py)) return Inb(px + dx, py + dy) ? new ITarget { X = px + dx, Y = py + dy, K = 1, None = true, Cloud = true } : null;
                for (int k = 1; k < N; k++)
                {
                    int x = px + dx * k, y = py + dy * k;
                    if (!Inb(x, y) || k > range) return new ITarget { X = x - dx, Y = y - dy, K = k - 1, None = true };
                    if (CellAt(B, x, y).Smoke) return new ITarget { X = x, Y = y, K = k, None = true, Smoked = true };
                    if (InCloud(B, x, y)) return new ITarget { X = x, Y = y, K = k, None = true, Cloud = true };
                    if (CellAt(B, x, y).T == "wall" || StopsShot(CellAt(B, x, y).O)) return new ITarget { X = x, Y = y, K = k };
                    var u = UnitAt(B, x, y);
                    if (u != null && u.Id != e.Id) return new ITarget { X = x, Y = y, K = k };
                }
                return null;
            }
            if (kind == "lob")
            {
                int lx = px + dx * LobRange, ly = py + dy * LobRange;
                if (!Inb(lx, ly)) return null;
                if (InCloud(B, px, py) || InCloud(B, lx, ly)) return new ITarget { X = lx, Y = ly, K = LobRange, None = true, Cloud = true };
                bool hidden = CellAt(B, lx, ly).Smoke;
                return new ITarget { X = lx, Y = ly, K = LobRange, None = hidden, Smoked = hidden };
            }
            if (kind == "blast") return new ITarget { X = px, Y = py, K = 0 };
            int tx = px + dx, ty = py + dy;
            if (!Inb(tx, ty)) return null;
            // Onarım, kalkan ve çoğaltma saldırı değil: sis ve bulut yalnız yakın vuruşu keser
            if (kind == "melee" && (InCloud(B, px, py) || InCloud(B, tx, ty))) return new ITarget { X = tx, Y = ty, K = 1, None = true, Cloud = true };
            bool smoked = kind == "melee" && CellAt(B, tx, ty).Smoke;
            return new ITarget { X = tx, Y = ty, K = 1, None = smoked, Smoked = smoked };
        }

        /// <summary>Patlamanın vurduğu kareler: makinenin çevresindeki dört kare.</summary>
        public static List<Pt> BlastArea(int x, int y) =>
            Dirs.Select(d => new Pt(x + d[0], y + d[1])).Where(p => Inb(p.X, p.Y)).ToList();

        /// <param name="hostage">Gelişmiş akılda makine bu tur ekibin hiçbir üyesine ulaşamıyor: sivil sunucuları hedef alır.</param>
        /// <param name="stuck">Tesis odası (aşama 8): makine bu tur ekibe ulaşamıyor: önündeki engeli (tel, kalas, hurda) kırmaya yönelir
        /// (sivilden önce, K8). Örnek tahtada hep false.</param>
        static double ScoreTarget(BState B, Unit e, string kind, int dir, int px, int py, bool hostage = false, bool stuck = false)
        {
            var t = IntentTarget(B, e, kind, dir, px, py);
            if (IsSignatureIntent(kind)) return SignatureScore(B, e, kind, dir, px, py, t, stuck);
            if (t == null || t.None) return 0;
            if (CellAt(B, t.X, t.Y).T == "wall") return kind == "shield" || kind == "clone" ? -1 : 0;
            var u = UnitAt(B, t.X, t.Y);
            // Onarım yalnızca hasarlı hedef dolaba anlamlı; birime yöneltilen onarım hiçbir şey yapmaz
            if (kind == "repair")
            {
                var ro = u == null || u.Id == e.Id ? CellAt(B, t.X, t.Y).O : null;
                return ro != null && IsTarget(ro.K) && ro.Hp < ro.Max ? 14 : -1;
            }
            // Kalkan: yanındaki zırhsız makineye; çoğaltma: yanındaki boş zemine (makine sınırı dolmadıysa)
            if (kind == "shield") return u != null && u.Id != e.Id && u.Side == 'e' && u.Armor == 0 ? 12 : -1;
            if (kind == "clone")
                return u == null && CellAt(B, t.X, t.Y).T == "floor" && CellAt(B, t.X, t.Y).O == null && Machines(B) < MaxEnemies ? 7 : -1;
            // Kilit hasar vermez: yalnız ekip üyesine anlamlı
            if (kind == "lock") return u != null && u.Id != e.Id && IsSpecialist(u) && !u.Locked ? 9 + (u.Type == "tek" ? 3 : 0) : 0;
            // Patlama: çevredeki ekip üyeleri çeker; makine, hedef dolap ve sivil sunucu iter (yön tek: dir 0)
            if (kind == "blast")
            {
                if (dir != 0) return -100;
                double sum = 0;
                foreach (var a in BlastArea(px, py))
                {
                    var v = UnitAt(B, a.X, a.Y);
                    // Aşama 8: yem ve havlayan köpek ekip gibi (9), öbür ekip tarafı birimler 2 (aygıt, köpek, tutsak)
                    if (v != null && v.Id != e.Id) { sum += v.Side != 'p' ? -3 : IsSpecialist(v) ? 9 + (v.Type == "tek" ? 3 : 0) : TeamLike(B, e, v) ? 9 : 2; continue; }
                    var ao = CellAt(B, a.X, a.Y).O;
                    if (ao == null) continue;
                    sum += IsTarget(ao.K) ? -20 : ao.K == RelayKind ? RelayBlastScore : ao.K == "spawner" || IsDevice(ao.K) || ao.K == "yayici" ? -10 : ao.K == "civ" ? (hostage ? 6 : -2) : ao.K == "cool" ? 1 : 0;
                }
                return sum;
            }
            if (u != null && u.Id != e.Id)
            {
                if (u.Side != 'p') return -6;
                if (IsSpecialist(u))
                    return 10 + (u.Type == "tek" ? 3 : 0) + (u.Hp <= (kind == "melee" ? 2 : 1) ? 4 : 0) + (B.Smart && u.Id == B.Carrier ? 8 : 0)
                        + (B.Smart && u.Type == "tek" && !IsLoot(B) && TargetsLeft(B).Any(t => Man(t.X, t.Y, u.X, u.Y) == 1) ? 4 : 0);
                // Aşama 8 (K9): yem ve havlayan köpek ekip üyesi gibi, öbür ekip tarafı birimler (taret, vinç, köpek, tutsak) düşük
                return TeamLike(B, e, u) ? 10 + (u.Hp <= (kind == "melee" ? 2 : 1) ? 4 : 0) : 3;
            }
            var o = CellAt(B, t.X, t.Y).O;
            if (o == null) return 0;
            // Aşama 8 (K8): ekibe ulaşamayan makine engeli kırar (sivil sunucunun 9'undan önce); ulaşabilen engelle uğraşmaz
            if (IsBarrier(o.K)) return stuck ? 11 : 0;
            // Aşama 11: saha rölesi (savunmanın hedef odası) makinelerin hedefidir: uzmanın 10'u ile yaralı uzmanın 14'ü arası
            if (o.K == RelayKind) return RelayHitScore;
            if (IsTarget(o.K)) return -20;
            if (o.K == "spawner") return -10;
            // Tesis odasının donanımını makineler bilerek hedef almaz (kaçan atış ve patlama yine kırar)
            if (IsDevice(o.K) || o.K == "yayici") return -10;
            // Temel akılda makineler sivil sunucuya bilerek ateş etmez (hedefi kaçınca atış arkadaki sunucuyu yine vurabilir);
            // gelişmiş akılda ekibe ulaşamayan makine sivilleri hedef alır (sivil güven): uzman araya girip korur
            if (o.K == "civ") return hostage ? 9 : -2;
            if (o.K == "cool")
                return 2 + 4 * Dirs.Count(d => { var v = UnitAt(B, t.X + d[0], t.Y + d[1]); return IsSpecialist(v); });
            return 0;
        }

        /// <summary>
        /// Gelişmiş akılda makinelerin tuttuğu kareler ve ağırlıkları: kasa alınmadan hedef dolapların yanındaki boş kareler
        /// (Teknisyen'in duracağı yer); kasa alındıysa taşıyıcının çıkışa en kısa yolu ve çıkışlar.
        /// </summary>
        static Dictionary<int, double> KeyTiles(BState B)
        {
            var keys = new Dictionary<int, double>();
            bool Open(int x, int y) => Inb(x, y) && CellAt(B, x, y).T != "wall" && CellAt(B, x, y).O == null;
            void Add(int x, int y, double w) { if (Open(x, y)) keys[y * N + x] = Math.Max(keys.TryGetValue(y * N + x, out var v) ? v : 0, w); }
            var carrier = B.Carrier != 0 ? ById(B, B.Carrier) : null;
            if (carrier == null || !OnBoard(carrier))
            {
                // Sunucu görevinde Teknisyen hedefin yanında birkaç tur çalışır: önü daha değerli
                foreach (var t in TargetsLeft(B)) foreach (var d in Dirs) Add(t.X + d[0], t.Y + d[1], IsLoot(B) ? 3 : 4);
                return keys;
            }
            // Çıkışlardan geriye uzaklık (duvar ve dolap geçilmez, birimler sayılmaz); taşıyıcının yolu uzaklığı azalan kareler
            var dist = new int[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) dist[y, x] = -1;
            var q = new Queue<Pt>();
            foreach (var p in B.Exits) if (Open(p.X, p.Y)) { dist[p.Y, p.X] = 0; q.Enqueue(p); Add(p.X, p.Y, 4); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Dirs)
                {
                    int nx = p.X + d[0], ny = p.Y + d[1];
                    // Geriye doğru arama: taşıyıcı (nx, ny)'den p'ye adım atabilmeli (turnike)
                    if (!Open(nx, ny) || dist[ny, nx] >= 0 || B.Ev != null && !CanStep(B, nx, ny, p.X, p.Y)) continue;
                    dist[ny, nx] = dist[p.Y, p.X] + 1; q.Enqueue(new Pt(nx, ny));
                }
            }
            int cx = carrier.X, cy = carrier.Y;
            for (int step = 0; step < 7 && dist[cy, cx] > 0; step++)
            {
                var next = Dirs.Select(d => new Pt(cx + d[0], cy + d[1])).Where(p => Inb(p.X, p.Y) && dist[p.Y, p.X] >= 0 && dist[p.Y, p.X] < dist[cy, cx]
                    && (B.Ev == null || CanStep(B, cx, cy, p.X, p.Y))).ToList();
                if (next.Count == 0) break;
                cx = next[0].X; cy = next[0].Y;
                Add(cx, cy, 4);
            }
            return keys;
        }

        /// <summary>
        /// Gelişmiş akılda niyetin tuttuğu kilit kareler: ateş ve kilit hattı ilk engele kadar geçtiği kareleri (makine sırası
        /// gelince yeniden nişan alır, hatta adım atan vurulur), yakın vuruş ve havan hedef kareyi, patlama çevresini tutar.
        /// Kendi dolabını ya da makineyi vuracak niyet bölge tutmaz. Önceki makinelerin tuttuğu kareler sayılmaz: makineler
        /// kilit kareleri paylaşır.
        /// </summary>
        static double Zone(BState B, Unit e, string kind, int dir, int px, int py, Dictionary<int, double> keys, HashSet<int> covered, double hit)
        {
            if (keys.Count == 0 || hit < 0) return 0;
            double sum = 0;
            double Key(int x, int y) => Inb(x, y) && !covered.Contains(y * N + x) && keys.TryGetValue(y * N + x, out var w) ? w : 0;
            if (kind == "shoot" || kind == "lock")
            {
                int range = kind == "lock" ? LockRange : N;
                // Söndürme bulutundan nişan alınmaz, hat buluta girince kesilir
                for (int k = 1; k <= range && !InCloud(B, px, py); k++)
                {
                    int x = px + Dirs[dir][0] * k, y = py + Dirs[dir][1] * k;
                    if (!Inb(x, y) || CellAt(B, x, y).T == "wall" || StopsShot(CellAt(B, x, y).O) || InCloud(B, x, y)) break;
                    var u = UnitAt(B, x, y);
                    if (u != null && u.Id != e.Id) break;
                    sum += Key(x, y);
                }
                sum = Math.Min(sum, 8);
            }
            else if (kind == "melee" || kind == "lob")
            {
                var t = IntentTarget(B, e, kind, dir, px, py);
                if (t != null && !t.Cloud && UnitAt(B, t.X, t.Y) == null) sum = 0.8 * Key(t.X, t.Y);
            }
            else if (kind == "blast" && dir == 0) sum = 0.5 * BlastArea(px, py).Sum(a => Key(a.X, a.Y));
            // Birimi zaten vuran niyette bölge yalnız ek
            return hit > 0 ? 0.5 * sum : sum;
        }

        /// <summary>Niyetin tuttuğu kareler (eşgüdüm): hattın ilk engele kadar geçtiği kareler ve vurduğu kare, yakın vuruşun ve
        /// havanın hedefi, patlamanın çevresi.</summary>
        static IEnumerable<int> Covers(BState B, Unit e, string kind, int dir, int px, int py)
        {
            if (kind == "shoot" || kind == "lock")
            {
                int range = kind == "lock" ? LockRange : N;
                if (InCloud(B, px, py)) yield break;
                for (int k = 1; k <= range; k++)
                {
                    int x = px + Dirs[dir][0] * k, y = py + Dirs[dir][1] * k;
                    if (!Inb(x, y) || InCloud(B, x, y)) yield break;
                    yield return y * N + x;
                    if (CellAt(B, x, y).T == "wall" || StopsShot(CellAt(B, x, y).O)) yield break;
                    var u = UnitAt(B, x, y);
                    if (u != null && u.Id != e.Id) yield break;
                }
            }
            else if (IsSignatureThreat(kind))
            {
                var t = IntentTarget(B, e, kind, dir, px, py);
                foreach (var a in SignatureArea(kind, dir, px, py, t)) yield return Key(a);
            }
            else if (kind == "blast") { foreach (var a in BlastArea(px, py)) yield return a.Y * N + a.X; }
            else
            {
                var t = IntentTarget(B, e, kind, dir, px, py);
                if (t != null && !t.None) yield return t.Y * N + t.X;
            }
        }

        /// <summary>Makine gidebileceği bir kareden ekibin bir üyesini vurabilir ya da kilitleyebilir mi (gelişmiş akılda sivil kararı).</summary>
        static bool CanReachTeam(BState B, Unit e, List<Pt> spots)
        {
            foreach (var pos in spots)
                foreach (var kind in UnitTypes[e.Type].Kinds)
                {
                    if (kind == "blast")
                    {
                        if (BlastArea(pos.X, pos.Y).Any(a => UnitAt(B, a.X, a.Y) is Unit v && TeamLike(B, e, v))) return true;
                        continue;
                    }
                    if (kind != "shoot" && kind != "melee" && kind != "lob" && kind != "lock" && !IsSignatureThreat(kind)) continue;
                    for (int dir = 0; dir < 4; dir++)
                    {
                        var t = IntentTarget(B, e, kind, dir, pos.X, pos.Y);
                        if (t == null || t.None) continue;
                        if (IsSignatureThreat(kind))
                        {
                            if (SignatureArea(kind, dir, pos.X, pos.Y, t).Any(a => UnitAt(B, a.X, a.Y) is Unit u && u.Id != e.Id && TeamLike(B, e, u))) return true;
                        }
                        else if (UnitAt(B, t.X, t.Y) is Unit v && v.Id != e.Id && TeamLike(B, e, v)) return true;
                    }
                }
            return false;
        }

        static void PlanEnemies(BState B)
        {
            int order = 0;
            var keys = B.Smart ? KeyTiles(B) : null;
            var covered = new HashSet<int>();
            var carrier = B.Smart && B.Carrier != 0 ? ById(B, B.Carrier) : null;
            if (carrier != null && !OnBoard(carrier)) carrier = null;
            var tek = B.Smart ? Alive(B, 'p').FirstOrDefault(u => IsSpecialist(u) && u.Type == "tek") : null;
            // Aşama 8: tahtada engel (tel, kalas, hurda) varsa ekibe ulaşamayan makine onu kırar (K8); yoksa hesap da yok
            bool barriers = B.Room != null && AnyBarrier(B);
            // Turda en çok bir makine sivillere yönelir (niyeti görünür: uzman araya girer ya da onu düşürür)
            bool civTaken = false;
            // Bölge olayında önceki makinelerin seçtiği tetikler (kola darbe, tüp, gaz): sonrakiler aynısını yeniden saymaz
            var claims = B.Smart && B.Ev != null ? new EventClaims() : null;
            // Tesis aşama 10, makine aklı (aygıt 2. kademe): planın başındaki pusu ve taret hatları; hattaki varış karesi cezalı (çekimsiz)
            var danger = B.Moves != null && B.Moves.Jam >= 2 ? new HashSet<int>(DangerTiles(B).Select(Key)) : null;
            // Aşama 11: savunmanın hedef odasında saha rölesi ilerleme odağıdır (gelişmiş akıldan bağımsız); başka yerde null
            var relay = B.Defense ? RelayAt(B) : null;
            foreach (var e in Alive(B, 'e'))
            {
                // Aşama 8: niyetsiz makine (bırakılan tutsak, komutla kilitlenen) bu planda planlanmaz; çekim de yok
                if (e.Idle) { e.Idle = false; e.Intent = null; e.Order = 0; continue; }
                if (IsTurncoat(e)) { PlanTurncoat(B, e, ref order); continue; }
                var type = UnitTypes[e.Type];
                var kinds = type.Kinds;
                var spots = new List<Pt> { new Pt(e.X, e.Y) };
                // Kriyo hattında donan makine bu plan yürüyemez (niyeti bulunduğu kareden); tesis hazırlığında (soğutma vanası) ilk
                // StillTurns turun planı da yürüyüşsüz
                if (!e.Frozen && B.Turn > B.StillTurns) spots.AddRange(Reachable(B, e));
                // Konum tercihi ekip üyelerine bakar: uzmanlar, yem ve (erimdeki makine için) havlayan köpek (aşama 8)
                var players = B.Units.Where(u => OnBoard(u) && TeamLike(B, e, u)).ToList();
                bool hostage = B.Smart && !civTaken && !CanReachTeam(B, e, spots);
                bool stuck = barriers && !CanReachTeam(B, e, spots);
                var best = new List<(Pt pos, string kind, int dir)>();
                double bestScore = double.NegativeInfinity;
                foreach (var pos in spots)
                {
                    // Konum tercihi: uzaktan vuranlar mesafede durmayı, ötekiler yaklaşmayı sever
                    int near = players.Count > 0 ? players.Min(p => Man(p.X, p.Y, pos.X, pos.Y)) : 0;
                    double posScore = type.Ranged ? (near <= 1 ? -2 : 0) : -0.2 * near;
                    if (danger != null && danger.Contains(pos.Y * N + pos.X)) posScore -= B.Moves.LinePenalty;
                    if (B.Smart)
                    {
                        // Yakın dövüşçü kilit kareyi bedeniyle tutar; herkes taşıyıcıyı (yoksa Teknisyen'i) kovalar
                        var focus = carrier ?? tek;
                        if (!type.Ranged && keys.TryGetValue(pos.Y * N + pos.X, out var stand)) posScore += stand;
                        if (focus != null && relay == null) posScore -= (type.Ranged ? 0.1 : 0.25) * Man(focus.X, focus.Y, pos.X, pos.Y) * (carrier != null ? 2 : 1);
                        // Bölge olayının sonraki adımı (kaynak karesi, canlı kanal, çökecek tavan…)
                        if (B.Ev != null) posScore += EventPos(B, e, pos);
                    }
                    if (relay is Pt rl) posScore -= (type.Ranged ? 0.1 : 0.25) * Man(rl.X, rl.Y, pos.X, pos.Y);
                    foreach (var kind in kinds)
                        for (int dir = 0; dir < 4; dir++)
                        {
                            double hit = ScoreTarget(B, e, kind, dir, pos.X, pos.Y, hostage, stuck);
                            double sc = hit + posScore + (B.Smart ? Zone(B, e, kind, dir, pos.X, pos.Y, keys, covered, hit) : 0)
                                + (claims != null && hit > -50 ? EventAim(B, e, kind, dir, pos.X, pos.Y, claims) : 0);
                            if (sc > bestScore + 1e-9) { bestScore = sc; best.Clear(); best.Add((pos, kind, dir)); }
                            else if (Math.Abs(sc - bestScore) < 1e-9) best.Add((pos, kind, dir));
                        }
                }
                var pk = best[(int)Math.Floor(Rnd(B) * best.Count)];
                e.X = pk.pos.X; e.Y = pk.pos.Y;
                e.Intent = new Intent { Kind = pk.kind, Dir = pk.dir };
                if (B.Smart) covered.UnionWith(Covers(B, e, pk.kind, pk.dir, e.X, e.Y));
                if (claims != null) Claim(B, e, pk.kind, pk.dir, claims);
                if (hostage)
                {
                    var aim = IntentTarget(B, e, pk.kind, pk.dir, e.X, e.Y);
                    bool Civ(int x, int y) => Inb(x, y) && UnitAt(B, x, y) == null && CellAt(B, x, y).O?.K == "civ";
                    civTaken = pk.kind == "blast" ? BlastArea(e.X, e.Y).Any(a => Civ(a.X, a.Y)) : aim != null && !aim.None && Civ(aim.X, aim.Y);
                }
                e.Order = ++order;
            }
            // Havlama yalnız bu planı etkiler (aşama 8)
            foreach (var u in B.Units) u.Bark = false;
        }

        static void PlaceSpawns(BState B)
        {
            B.Spawns = new List<Pt>();
            if (B.Phase != "player") return;
            // Tesis hazırlığı (sigorta panosu kırık): ilk QuietTurns tur uyanma yeri seçilmez, çekim de yok
            if (B.Turn <= B.QuietTurns) return;
            int room = MaxEnemies - Machines(B);
            var cand = new List<Pt>();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var o = CellAt(B, x, y).O;
                    if (o == null || o.K != "spawner") continue;
                    foreach (var d in Dirs)
                    {
                        int nx = x + d[0], ny = y + d[1];
                        if (Inb(nx, ny) && CellAt(B, nx, ny).T != "wall" && CellAt(B, nx, ny).O == null && UnitAt(B, nx, ny) == null && !cand.Any(c => c.X == nx && c.Y == ny))
                            cand.Add(new Pt(nx, ny));
                    }
                }
            for (int i = 0; i < Math.Min(B.SpawnPerTurn, room) && cand.Count > 0; i++)
            {
                int k = (int)Math.Floor(Rnd(B) * cand.Count);
                B.Spawns.Add(cand[k]);
                cand.RemoveAt(k);
            }
        }

        public sealed class IntentView
        {
            public int Unit, Order, Dir, FromX, FromY, TargetX, TargetY;
            public string Kind;
            public bool HasTarget, TargetNone;
            /// <summary>Niyet sise kesildi: hedef sisli kare, kimse vurulmaz.</summary>
            public bool Smoked;
            /// <summary>Niyet söndürme bulutuna kesildi ya da makine buluttan nişan alamıyor (TargetNone ile birlikte).</summary>
            public bool Cloud;
            /// <summary>Patlamanın vuracağı kareler (öteki niyetlerde boş).</summary>
            public List<Pt> Area = new List<Pt>();
        }

        /// <summary>Arayüz için: her düşmanın gösterilen niyeti (yalan olabilir).</summary>
        public static List<IntentView> Intents(BState B)
        {
            var list = new List<IntentView>();
            foreach (var e in Alive(B, 'e').Where(u => u.Intent != null).OrderBy(u => u.Order))
            {
                int dir = e.Intent.Dir;
                var t = IntentTarget(B, e, e.Intent.Kind, dir, e.X, e.Y);
                list.Add(new IntentView
                {
                    Unit = e.Id, Order = e.Order, Kind = e.Intent.Kind, Dir = dir,
                    FromX = e.X, FromY = e.Y, HasTarget = t != null, TargetNone = t != null && t.None, Smoked = t != null && t.Smoked, Cloud = t != null && t.Cloud, TargetX = t?.X ?? 0, TargetY = t?.Y ?? 0,
                    Area = e.Intent.Kind == "emp" && t != null && !t.None ? BlastArea(t.X, t.Y).Concat(new[] { new Pt(t.X, t.Y) }).ToList() :
                        e.Intent.Kind == "blast" ? BlastArea(e.X, e.Y) : SignatureArea(e.Intent.Kind, dir, e.X, e.Y, t),
                });
            }
            return list;
        }

        static void Lose(BState B, string reason) { B.Phase = "lost"; B.OutcomeSuccess = false; B.OutcomeReason = reason; }

        /// <summary>Bir makinenin niyeti (düşman evresinde, sırası gelince).</summary>
        static void ApplyIntent(BState B, Unit e, ITarget t)
        {
            if (IsTurncoat(e))
            {
                if (e.Intent.Kind != "sabotaj" || UnitAt(B, t.X, t.Y) != null) ApplyAbility(B, e, t.X, t.Y);
                return;
            }
            if (IsSignatureIntent(e.Intent.Kind)) { ApplySignatureIntent(B, e, t); return; }
            switch (e.Intent.Kind)
            {
                case "repair":
                {
                    var o = CellAt(B, t.X, t.Y).O;
                    if (o != null && IsTarget(o.K) && o.Hp < o.Max) { o.Hp++; AddFx(B, new Fx { K = "heal", X = t.X, Y = t.Y }); Say(B, Loc.F("Bakım robotu {0} onardı.", LowerName(Loc.T(ObjNames[o.K]))), "bad"); }
                    break;
                }
                case "melee":
                {
                    var v = UnitAt(B, t.X, t.Y);
                    MachineDamageAt(B, t.X, t.Y, e.Type == "montaj" ? SignatureRules(B).BuildDamage : 2,
                        e.Type == "montaj" ? UnitTypes[e.Type].Name : Loc.T("bakım robotu"));
                    HitSlide(B, v, e.Intent.Dir);
                    break;
                }
                case "lob":
                {
                    var v = UnitAt(B, t.X, t.Y);
                    Blasted(B, t.X, t.Y);
                    MachineDamageAt(B, t.X, t.Y, 1, Loc.T("havan"));
                    HitSlide(B, v, e.Intent.Dir);
                    break;
                }
                case "lock":
                {
                    var v = UnitAt(B, t.X, t.Y);
                    // Aşama 8: kilit aygıta işlemez
                    if (v == null || v.Side != 'p' || IsGadget(v)) break;
                    v.Locked = true;
                    AddFx(B, new Fx { K = "lock", X = v.X, Y = v.Y, Unit = v.Id });
                    Say(B, Loc.F("{0} kilitlendi: bu tur yürüyemez.", UnitTypes[v.Type].Name), "bad");
                    break;
                }
                case "shield":
                {
                    var v = UnitAt(B, t.X, t.Y);
                    if (v == null || v.Side != 'e' || v.Armor > 0) break;
                    v.Armor = 1;
                    AddFx(B, new Fx { K = "shield", X = v.X, Y = v.Y, Unit = v.Id });
                    Say(B, Loc.F("Kalkan dronu {0} zırhladı.", LowerName(UnitTypes[v.Type].Name)), "bad");
                    break;
                }
                case "clone":
                {
                    // Uyanma gibi: karede biri varsa uyanma engellenir ama duran 1 hasar alır
                    var v = UnitAt(B, t.X, t.Y);
                    if (v != null) { Hurt(B, v, 1, Loc.T("uyanmayı engelledi")); break; }
                    if (CellAt(B, t.X, t.Y).T == "wall" || CellAt(B, t.X, t.Y).O != null || Machines(B) >= MaxEnemies) break;
                    B.Units.Add(NewUnit(B, "dron", t.X, t.Y));
                    AddFx(B, new Fx { K = "spawn", X = t.X, Y = t.Y });
                    Say(B, Loc.T("Çoğaltıcı yeni bir dron uyandırdı."), "bad");
                    break;
                }
                case "blast":
                {
                    AddFx(B, new Fx { K = "blast", X = e.X, Y = e.Y, Unit = e.Id });
                    Say(B, Loc.T("Patlayan robot infilak etti."), "bad");
                    foreach (var a in BlastArea(e.X, e.Y)) { Blasted(B, a.X, a.Y); MachineDamageAt(B, a.X, a.Y, 1, Loc.T("patlama")); }
                    if (!e.Dead) { e.Dead = true; e.Hp = 0; AddFx(B, new Fx { K = "die", X = e.X, Y = e.Y, Unit = e.Id }); MachineScrap(B, e); }
                    break;
                }
                default:
                {
                    // Atış geçtiği ve vurduğu kareleri ateşler (yanıcı gaz); yağdaki hedef isabet yönünde kayar
                    if (B.Ev != null) for (int k = 1; k <= t.K; k++) Fire(B, e.X + Dirs[e.Intent.Dir][0] * k, e.Y + Dirs[e.Intent.Dir][1] * k);
                    var v = UnitAt(B, t.X, t.Y);
                    MachineDamageAt(B, t.X, t.Y, 1, Loc.T("dron ateşi"));
                    HitSlide(B, v, e.Intent.Dir);
                    break;
                }
            }
        }

        public static bool EndTurn(BState B) => EndTurn(B, true);

        // Plan puanındaki ileri bakışta henüz görülmeyen saha belirtileri ve kimlik kullanılmaz.
        internal static bool ForecastTurn(BState B) => EndTurn(B, false);

        static bool EndTurn(BState B, bool fieldEvents)
        {
            // Tesis odası, son nefes: turun başında düşmüş olanlar (oyuncunun hamle sırasında ya da önceki turlarda düşen) kurala girmez
            var down = B.Phase == "player" && B.Room != null && B.Moves != null && B.Moves.LastBreath
                ? new HashSet<int>(B.Units.Where(u => IsSpecialist(u) && u.Dead).Select(u => u.Id)) : null;
            bool done = EndTurnCore(B, fieldEvents);
            if (done && down != null && B.Phase != "player") LastBreath(B, down);
            return done;
        }

        /// <summary>
        /// Son nefes (belge §4.3): bu makine evresinde düşen gövde, oda oyuncu bir daha hamle yapamadan bittiyse (kendiliğinden geçiş,
        /// süre ya da kaçış) ve ekipten biri ayakta çıktıysa (geçişte kapıda, tesisin girişinde tahliye) ekiple gider. Geri çekilme ve
        /// kaçıştan önce kaybedilen oda değişmez (yerde yatan ölür). Oyuncuya bir tur kalan gövde turun başında <paramref name="down"/>
        /// içindedir: eski kural. RNG çekmez.
        /// </summary>
        static void LastBreath(BState B, HashSet<int> down)
        {
            bool pass = B.Phase == "pass", exit = !pass && IsFinalExit(B);
            if (!pass && !exit) return;
            bool standing = B.Units.Any(u => IsSpecialist(u) && !u.Dead && (pass ? u.AtGate : u.Out));
            if (!standing) return;
            foreach (var u in B.Units)
            {
                if (!LyingBody(u) || down.Contains(u.Id)) continue;
                u.Breath = true;
                Say(B, Loc.F("{0} son nefeste düştü; ekip onu da götürdü.", UnitTypes[u.Type].Name), "good");
            }
        }

        static bool EndTurnCore(BState B, bool fieldEvents)
        {
            if (B.Phase != "player") return false;
            // Tesis odası: ayaktaki bütün uzmanlar kapıdayken tur bitmez (yapılacak iş GEÇ, geri alma ya da geri çekilme)
            if (CanPass(B)) return false;
            B.FxList.Clear(); B.Hist.Clear();
            // Aşama 11: savunmada kapı sığınak değildir; kapıda bekleyen uzman ve gövde tahtaya döner (bütün ekip kapıdaysa yukarıda GEÇ)
            if (B.Defense) ReturnFromGate(B);
            // Tesis odası: bu tur ikinci uzman gelmeyen iki kişilik iş boşa gider (örnek tahtada hiçbir şey yapmaz)
            ClearHold(B);
            // Geçen turun kilidi çözülür; bu evrede vurulan uzman gelecek tur yürüyemez
            foreach (var u in B.Units) if (u.Side == 'p') u.Locked = false;
            if (fieldEvents)
            {
                ApplyPulseContacts(B);
                FieldMachinePhase(B);
            }
            Say(B, Loc.F("— Tur {0}: makineler hareket ediyor —", B.Turn), "turn");
            // 1) Niyetler sırayla (itilen robot niyetini yeni yerinden uygular)
            foreach (var e in Alive(B, 'e').OrderBy(u => u.Order).ToList())
            {
                if (e.Dead || e.Intent == null || B.Phase != "player") continue;
                var t = IntentTarget(B, e, e.Intent.Kind, e.Intent.Dir, e.X, e.Y);
                AddFx(B, new Fx { K = "attack", Unit = e.Id, X = t?.X ?? e.X, Y = t?.Y ?? e.Y });
                if (t != null && t.Smoked)
                {
                    AddFx(B, new Fx { K = "smoked", Unit = e.Id, X = t.X, Y = t.Y });
                    Say(B, Loc.F("{0} sisli kareye saldırdı: isabet yok.", UnitTypes[e.Type].Name), "good");
                }
                if (t != null && t.Cloud)
                {
                    AddFx(B, new Fx { K = "clouded", Unit = e.Id, X = t.X, Y = t.Y });
                    Say(B, Loc.F("{0} bulutta nişan alamadı.", UnitTypes[e.Type].Name), "sys");
                }
                if (t == null || t.None) continue;
                ApplyIntent(B, e, t);
            }
            if (fieldEvents) ApplyPulseContacts(B);
            // Sis makine evresinin sonunda dağılır: yeni plan onu görmez
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) B.Cells[y, x].Smoke = false;
            // Tesis aşama 8 (K3): aygıtlar niyetlerden ve sisin dağılmasından sonra, kaçış denetiminden ve uyanmadan önce çalışır
            RunDevices(B);
            // Kaçışta makinelerin itmesi uzmanı çıkışa atabilir (tahliye edilir); tahtada uzman kalmadıysa baskın biter. Kaçıştan
            // önce ekibin düşmesi uyanmadan sonra sayılır (eski baskınlar aynı kalır).
            if (B.Escape) CheckEscape(B);
            if (B.Phase != "player") return true;
            // 2) Uyanma: işaretli karede biri duruyorsa uyanma engellenir ama duran hasar alır
            foreach (var s in B.Spawns)
            {
                var u = UnitAt(B, s.X, s.Y);
                if (u != null) { Hurt(B, u, 1, Loc.T("uyanmayı engelledi")); Say(B, Loc.T("Uyanan kopya engellendi."), u.Side == 'p' ? "good" : "sys"); continue; }
                if (CellAt(B, s.X, s.Y).O != null) continue;
                var tp = WakeType(B, Rnd(B));
                B.Units.Add(NewUnit(B, tp, s.X, s.Y));
                AddFx(B, new Fx { K = "spawn", X = s.X, Y = s.Y });
                Say(B, Loc.F("Yeni bir {0} uyandı.", LowerName(UnitTypes[tp].Name)), "bad");
            }
            B.Spawns = new List<Pt>();
            // 3) Bölge olayı: takvimli adımlar ve olgunlaşan fitiller (son turda olay adımı yok); plan olaydan sonraki tahtayı görür
            if (B.Ev != null && B.Turn < B.MaxTurns) EventEnd(B);
            CheckEscape(B);
            if (B.Phase != "player") return true;
            // Tesis odası: makine evresi tahtadaki son uzmanı düşürdü, kalanlar kapıda: oda kendiliğinden geçilir
            if (CanPass(B)) { PassRoom(B); return true; }
            B.Turn++;
            if (B.Turn > B.MaxTurns)
            {
                // Aşama 11: savunmanın süresi doldu: ayakta biri varsa kanat tutuldu
                if (B.Defense) { DefenseTimeUp(B); return true; }
                if (!B.Escape)
                {
                    // Tesisin giriş ve ara odası: süre dolarken kapıda bekleyen varsa ekip onlarla geçer, kapıya varamayan uzman sağ geri
                    // çekilir; kapıda kimse yoksa ekip geri çekilmiş sayılır (baskın biter)
                    if (B.Room != null && B.Room.Type != RoomType.Target && B.Units.Any(u => IsSpecialist(u) && !u.Dead && u.AtGate))
                    {
                        foreach (var u in B.Units.Where(v => IsSpecialist(v) && !v.Dead && !v.AtGate && !v.Out).ToList())
                        {
                            u.Withdrew = true; u.Cab = false;
                            AddFx(B, new Fx { K = "withdraw", X = u.X, Y = u.Y, Unit = u.Id });
                            Say(B, Loc.F("{0} kapıya yetişemedi ve geri çekildi; ekip onsuz devam ediyor.", UnitTypes[u.Type].Name), "bad");
                        }
                        PassRoom(B);
                        return true;
                    }
                    if (B.Room != null && B.Room.Type != RoomType.Target)
                    {
                        Lose(B, Loc.T("Süre doldu: ekip geri çekildi."));
                        B.Retreated = true;
                        return true;
                    }
                    Lose(B, IsLoot(B) ? Loc.T("Süre doldu: YZ ikmal deposunu kilitledi.") : B.IsCore ? Loc.T("Süre doldu: YZ çekirdeğini binadan taşıdı.") : Loc.T("Süre doldu: YZ sunucularını yedekledi ve binayı kilitledi."));
                    return true;
                }
                // Kaçışta süre doldu: tahtada kalan uzman çıkamadı, baskında düşmüş sayılır (köpek, aygıt ve tutsak değil)
                foreach (var u in Alive(B, 'p').Where(IsSpecialist))
                {
                    u.Left = true; u.Dead = true; u.Hp = 0;
                    AddFx(B, new Fx { K = "die", X = u.X, Y = u.Y, Unit = u.Id });
                    Say(B, Loc.F("{0} çıkamadı: baskında düşmüş sayılır.", UnitTypes[u.Type].Name), "bad");
                }
                // Tesis odası (hedef ve ara oda kaçışta): kapıda bekleyenler geçer; tesisin girişinde ya da kapıda kimse yokken biter
                if (CanPass(B)) { PassRoom(B); return true; }
                FinishEscape(B);
                return true;
            }
            // 4) Yeni plan
            // Kilitli ya da donmuş uzman bu tur yürüyemez (yeteneği serbest)
            foreach (var u in B.Units) if (u.Side == 'p') { u.Moved = u.Locked || u.Frozen; u.Acted = false; u.DogCommands = 0; }
            // Aşama 11: savunmada gelen makineler (takipçiler, dalgalar) yerleşir; aynı tur planlanır
            if (B.Defense) PlaceIncoming(B);
            // Tesis aşama 8: tutsakların süresi (planlamadan önce; bırakılan makine bu planı niyetsiz geçirir)
            AdvanceCaptives(B);
            PlanEnemies(B);
            // Tesis odası: pusu hatları makineler yeni yerlerine geçtikten sonra, uyanmadan önce ateşlenir (örnek tahtada hiçbir şey)
            FireAmbush(B);
            if (fieldEvents) FieldDogReport(B);
            PlaceSpawns(B);
            if (B.Turn == B.MaxTurns)
                Say(B, B.Escape ? Loc.T("Son tur: bu tur çıkamayan uzman baskında düşmüş sayılır.")
                    : B.Room != null && B.Room.Type != RoomType.Target && !B.Defense ? Loc.T("Son tur: kapıya varan geçer, varamayan geri çekilir; kapıda kimse yoksa ekip döner.")
                    : Loc.T("Son tur."), "turn");
            return true;
        }

        static readonly System.Globalization.CultureInfo UiCulture = new System.Globalization.CultureInfo("tr-TR");

        /// <summary>Cümle içi ad: oyun dilinin küçük harfi (Türkçede “İ → i”); kısaltmalar (YZ, AI) korunur.</summary>
        static string LowerName(string name)
        {
            var culture = Loc.IsSource ? UiCulture : System.Globalization.CultureInfo.InvariantCulture;
            return string.Join(" ", name.Split(' ').Select(w => w.Length > 1 && w.ToUpper(culture) == w ? w : w.ToLower(culture)));
        }

        /// <summary>Geri çekilme: kaçıştan önce ekip olduğu gibi döner, baskın kaybedilir. Kaçışta yoktur: ekip çıkışa yürür.</summary>
        public static bool Retreat(BState B)
        {
            if (B.Phase != "player" || B.Escape) return false;
            Lose(B, Loc.T("Ekip geri çekildi."));
            B.Retreated = true;
            return true;
        }

        public static RaidResult Result(BState B)
        {
            var team = B.Units.Where(u => IsSpecialist(u) || IsTurncoat(u)).ToList();
            return new RaidResult
            {
                Success = B.Phase == "won", Retreat = B.Retreated, Collateral = B.Collateral,
                Down = team.Count(u => u.Dead || IsTurncoat(u)), Turns = Math.Min(B.Turn, B.MaxTurns),
                Reason = B.OutcomeReason ?? "",
                CiviliansLeft = Enumerable.Range(0, N * N).Count(i => CellAt(B, i % N, i / N).O?.K == "civ"),
                Hp = team.Select(u => u.Dead || IsTurncoat(u) ? 0 : u.Hp).ToList(),
                Armor = team.Select(u => u.Armor).ToList(),
                Turned = team.Where(IsTurncoat).Select(u => u.Slot).ToList(),
                Subdued = team.Where(u => IsTurncoat(u) && u.Dead && !u.Overboard && !u.Out).Select(u => u.Slot).ToList(),
                Left = team.Select((u, i) => (u, i)).Where(x => x.u.Left || IsTurncoat(x.u) && (!x.u.Dead || x.u.Overboard || x.u.Out)).Select(x => x.i).ToList(),
            };
        }

        /// <summary>Gerileme testleri için durumun özeti (RaidEngineTests).</summary>
        public static string Signature(BState B)
        {
            var objs = new List<string>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { var o = CellAt(B, x, y).O; if (o != null) objs.Add($"{o.K}{x}{y}:{o.Hp}{(o.MachineBuilt ? "M" : "")}"); }
            // Yıkılan duvar, sis, sınırlı yetenek hakkı, tahliye ("o"), geride kalma ("l") ve tesis kapısı ("g") yalnız varsa eklenir
            // (eski imzalar değişmez)
            var smoke = new List<string>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (CellAt(B, x, y).Smoke) smoke.Add($"{x}{y}");
            var opened = new List<string>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (CellAt(B, x, y).Shutter && CellAt(B, x, y).T != "wall") opened.Add($"{x}{y}");
            var doubles = new List<string>();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (CellAt(B, x, y).Double) doubles.Add($"{x}{y}");
            return $"{B.Phase}|t{B.Turn}|c{B.Collateral}|rs{B.Rs}|" + (B.Carrier != 0 ? $"k{B.Carrier}|" : "") + (B.Escape && B.Carrier == 0 ? "e|" : "")
                + string.Join(",", B.Units.Select(u => $"{u.Type}{u.X}{u.Y}:{u.Hp}{(u.Dead ? "x" : "")}{(u.Out ? "o" : "")}{(u.Left ? "l" : "")}{(u.Charges >= 0 ? "c" + u.Charges : "")}"
                    + $"{(u.Cab ? "b" : "")}{(u.Frozen ? "f" : "")}{(u.Ice > 0 ? "i" : "")}{(u.AtGate ? "g" : "")}{(u.AmbushDir >= 0 ? $"a{u.AmbushDir}{u.AmbushX}{u.AmbushY}" : "")}{(u.Breath ? "n" : "")}"
                    // Aşama 8 birim ekleri yalnız varsa: sınıf, ekipman ve hakkı, sahip, yön, kalan tur, havlama, niyetsiz
                    + $"{(u.Class != null ? "C" + u.Class[0] : "")}{(u.Gear != null ? $"q{u.Gear}{u.GearLeft}" : "")}{(u.Owner >= 0 ? "w" + u.Owner : "")}{(u.Dir >= 0 ? "d" + u.Dir : "")}"
                    + $"{(u.Turns != 0 ? "u" + u.Turns : "")}{(u.Bark ? "v" : "")}{(u.Idle ? "z" : "")}"
                    // Aşama 10: sökülen cıvata yalnız varsa ("u" kalan tur olduğu için büyük harf)
                    + $"{(u.Unbolted ? "U" : "")}"
                    // Aşama 12: kenardan düşen (Katar) yalnız varsa
                    + $"{(u.Overboard ? "K" : "")}{(u.Assembled ? "M" : "")}"
                    + (u.Titles != FacilityTitle.None || u.Work != FacilityTitle.None || u.Dragged != 0 || u.DogCommands != 0
                        ? $"J{(int)u.Titles}.{(int)u.Work}.{u.Dragged}.{u.DogCommands}" : ""))) + "|" + string.Join(",", objs)
                + (B.Rubble.Count > 0 ? "|r" + string.Join(",", B.Rubble.Select(p => $"{p.X}{p.Y}")) : "") + (smoke.Count > 0 ? "|s" + string.Join(",", smoke) : "")
                // Bölge olayı yalnız varsa (eski imzalar değişmez)
                + (B.Ev != null ? "|v" + EventSignature(B) : "")
                // Tesis hazırlığı yalnız varsa; oda eki sonda kalır (eski ve aşama 1 imzaları değişmez)
                + (B.QuietTurns > 0 || B.StillTurns > 0 ? "|p" + B.QuietTurns + "." + B.StillTurns : "")
                // Tesis hamleleri yalnız varsa: yerdeki yük, iki kişilik işin tutuşu, kalkan kepenkler, açılan büyük sandık
                + (B.CargoX >= 0 ? $"|y{B.CargoX}{B.CargoY}" : "") + (B.HoldId != 0 ? $"|h{B.HoldId}.{B.HoldX}{B.HoldY}" : "")
                + (opened.Count > 0 ? "|k" + string.Join(",", opened) : "") + (B.CrateOpen ? "|B" : "")
                // Aşama 8: köpeğin kokladığı oda yalnız varsa
                + (B.Sniffed != 0 ? "|n" + B.Sniffed : "")
                // Aşama 10: sınıf sayaçları ve çift cidar yalnız varsa
                + (B.TallyArray().Any(v => v != 0) ? "|T" + string.Join(".", B.TallyArray()) : "") + (doubles.Count > 0 ? "|D" + string.Join(",", doubles) : "")
                // Aşama 11: savunma yalnız varsa (başlangıç turu, terk, bekleyen makineler; oda ekinden önce)
                + (B.Defense ? "|V" + DefenseSignature(B) : "")
                // Aşama 12: AYIR yalnız varsa (oda ekinden önce)
                + (B.Decoupled ? "|A" : "")
                + FrostSignature(B)
                // Tesis odası yalnız varsa (eski imzalar değişmez)
                + (B.Room != null ? "|R" + B.Room.Id : "");
        }
    }
}
