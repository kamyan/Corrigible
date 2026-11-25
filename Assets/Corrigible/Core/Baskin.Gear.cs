using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis baskınının ekipmanı ve ekibin uzman olmayan birimleri (aşama 8; Docs/TESIS-BASKINLARI-TASARIMI.md §5): birim sınıfları
    /// (<see cref="Unit.Class"/>: köpek, aygıt, tutsak), ekip ve engel yüklemleri, ekipman eylemleri (örme seti: tel ve kalas, dinamit
    /// lokumu, yaylı taret, yürüyen yem, makaralı vinç, kumanda kelepçesi), aygıtların makine evresindeki çalışması ve hurda; köpeğin
    /// komutları (havla, getir, kokla), tutsağın süresi ve kendi eylemi (komuta), odaya ekiple giren köpek ve tutsak. Her kural tesis
    /// odasına bağlıdır (<see cref="BState.Room"/>) ya da yalnız <see cref="Unit.Class"/> dolu birimlerde çalışır: örnek tahtada Class
    /// hep null, bütün süzgeçler bugünkü <c>Side == 'p'</c> ile aynı sonucu verir. Kurallar RNG çekmez.
    /// </summary>
    public static partial class Baskin
    {
        // ---------- Sınıflar ----------
        /// <summary>Birim sınıfları (<see cref="Unit.Class"/>): null uzman; köpek, aygıt (taret, yem, vinç), ele geçirilen makine.</summary>
        public const string ClassDog = "kopek", ClassGadget = "aygit", ClassCaptive = "tutsak";

        /// <summary>Ekip uzmanı: kadro birimi (örnek tahtada her Side 'p' birim).</summary>
        public static bool IsSpecialist(Unit u) => u != null && u.Side == 'p' && u.Class == null;
        /// <summary>Aygıt (taret, yem, vinç): yürümez, eylem vermez, sonuç listelerine, kaçışa ve GEÇ'e girmez. Eski
        /// <see cref="IsDevice"/> (pano, anten, vana: "donanım") ile karıştırılmasın.</summary>
        public static bool IsGadget(Unit u) => u?.Class == ClassGadget;
        public static bool IsDog(Unit u) => u?.Class == ClassDog;
        public static bool IsCaptive(Unit u) => u?.Class == ClassCaptive;

        /// <summary>
        /// Makine aklında "ekip üyesi" (§5.6, K9): uzman; tesis odasında ayrıca yürüyen yem ve (makinenin plan öncesi karesinden
        /// <see cref="MoveRules.BarkRange"/> içinde) bu tur havlayan köpek. Örnek tahtada yalnız uzman.
        /// </summary>
        static bool TeamLike(BState B, Unit e, Unit u)
        {
            if (u == null || u.Side != 'p') return false;
            if (u.Class == null) return true;
            if (B.Room == null) return false;
            if (IsGadget(u)) return u.Type == "yem";
            return IsDog(u) && u.Bark && e != null && B.Moves != null && Man(e.X, e.Y, u.X, u.Y) <= B.Moves.BarkRange;
        }

        /// <summary>Engel nesneleri (tel örgü, kalas duvar, hurda): yürüyüşü keser, oda raporunun nesnesidir.</summary>
        public static bool IsBarrier(string k) => k == "tel" || k == "kalas" || k == "hurda";

        /// <summary>Nişanlı atışı (makine ateşi ve kilidi, pusu, taret) durduran nesne: tel örgü dışındaki her nesne.</summary>
        static bool StopsShot(Obj o) => o != null && o.K != "tel";

        static bool AnyBarrier(BState B)
        {
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (B.Cells[y, x].O is Obj o && IsBarrier(o.K)) return true;
            return false;
        }

        // ---------- Eylem kimlikleri ----------
        /// <summary>Aşama 8 eylem kimlikleri: ekipman (tel, kalas, lokum, taret, yem, vinç, kelepçe) ve birim (havla, getir, kokla,
        /// komuta). <see cref="ActionKinds"/> aşama 6'nın altısı olarak kalır (K2).</summary>
        public const string ActWire = "tel", ActPlank = "kalas", ActCharge = "lokum", ActTurret = "taret", ActDecoy = "yem", ActWinch = "vinc",
            ActClamp = "kelepce", ActBark = "havla", ActFetch = "getir", ActSniff = "kokla", ActCommand = "komuta";

        /// <summary>Uzmanın ekipman eylemleri (sabit sıra; kimin hangisini verebileceği <see cref="Unit.Gear"/>'a bağlı).</summary>
        public static readonly string[] GearActionKinds = { ActWire, ActPlank, ActCharge, ActTurret, ActDecoy, ActWinch, ActClamp };
        /// <summary>Uzman olmayan birimlerin eylemleri: köpeğin üç komutu ve tutsağın kendi eylemi.</summary>
        public static readonly string[] UnitActionKinds = { ActBark, ActFetch, ActSniff, ActCommand };
        /// <summary>Bot adayları için hepsi: <see cref="ActionKinds"/> + <see cref="GearActionKinds"/> + <see cref="UnitActionKinds"/>
        /// (sabitlerle yazılır: kısmi sınıfların alan ilklendirme sırasına bağlı kalmasın).</summary>
        public static readonly string[] AllActionKinds =
        {
            ActAmbush, ActRush, ActDrag, ActThrow, ActJoint, ActVent,
            ActWire, ActPlank, ActCharge, ActTurret, ActDecoy, ActWinch, ActClamp,
            ActBark, ActFetch, ActSniff, ActCommand,
        };

        /// <summary>Ekipman eyleminin gerektirdiği eşya (<see cref="FacilityGear"/> kimliği); öbür eylemlerde null.</summary>
        public static string GearOf(string kind)
        {
            switch (kind)
            {
                case ActWire: case ActPlank: return FacilityGear.Wire;
                case ActCharge: return FacilityGear.Charge;
                case ActTurret: return FacilityGear.Turret;
                case ActDecoy: return FacilityGear.Decoy;
                case ActWinch: return FacilityGear.Winch;
                case ActClamp: return FacilityGear.Clamp;
                default: return null;
            }
        }

        // ---------- Köpek ve tutsak: önkoşul, hedef, kural (parça C) ----------
        /// <summary>Köpeğin ve tutsağın eylem önkoşulu (ortak önkoşuldan sonra, <see cref="CanAction"/>): köpek yalnız havla, getir ve
        /// kokla; tutsak yalnız komuta (K15, K13). Öbür sınıflar hiçbirini veremez.</summary>
        static bool UnitCanAction(BState B, Unit u, string kind)
        {
            // Aşama 10: erimde ayakta koku yayıcı varsa köpek komut alamaz (yürüyüş serbest)
            if (IsDog(u)) return (kind == ActBark || kind == ActFetch || kind == ActSniff) && !EmitterBlocks(B, u);
            if (IsCaptive(u)) return kind == ActCommand;
            return false;
        }

        /// <summary>Köpeğin ve tutsağın eylem hedefleri ve uzmanın kelepçesi (<see cref="GearTargets"/>'ın bilinmeyen türü).</summary>
        static List<Pt> UnitTargets(BState B, Unit u, string kind)
        {
            switch (kind)
            {
                case ActClamp: return ClampTargets(B, u);
                case ActBark: return new List<Pt> { new Pt(u.X, u.Y) };
                case ActFetch: return FetchTargets(B, u);
                case ActSniff: return SniffTargets(B, u);
                case ActCommand: return CommandTargets(B, u);
                default: return new List<Pt>();
            }
        }

        /// <summary>Köpeğin, tutsağın ve kelepçenin kuralı (<see cref="ApplyGear"/>'ın bilinmeyen türü; bedel <see cref="ApplyAction"/>'da).</summary>
        static void ApplyUnitAction(BState B, Unit u, string kind, int x, int y)
        {
            switch (kind)
            {
                case ActClamp: ApplyClamp(B, u, x, y); break;
                case ActBark: ApplyBark(B, u); break;
                case ActFetch: ApplyFetch(B, u, x, y); break;
                case ActSniff: ApplySniff(B, u, x, y); break;
                case ActCommand: ApplyCommand(B, u, x, y); break;
            }
        }

        // ---------- Kumanda kelepçesi ve tutsak (belge §5.4) ----------
        /// <summary>Kelepçenin hedefi: dik komşu ayakta makine (tahtada; kabindeki ve kapıdaki <see cref="UnitAt"/>'ta yok).</summary>
        static List<Pt> ClampTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            foreach (var d in Dirs)
            {
                int x = u.X + d[0], y = u.Y + d[1];
                if (Inb(x, y) && UnitAt(B, x, y) is Unit target && target.Side == 'e' && !IsTurncoat(target)) o.Add(new Pt(x, y));
            }
            return o;
        }

        /// <summary>
        /// Kelepçe (K12): makine ekibe geçer (Side 'p', sınıf tutsak, sahibi kelepçeleyenin kadro sırası), niyeti silinir, yakalandığı
        /// tur oynamaz; <see cref="MoveRules.ClampTurns"/> oyuncu turu oynar (<see cref="AdvanceCaptives"/>). Kelepçe tükenir (hak 0).
        /// Fx "capture": X/Y kare, Unit tutsak, N uzman.
        /// </summary>
        static void ApplyClamp(BState B, Unit u, int x, int y)
        {
            var e = UnitAt(B, x, y);
            e.Side = 'p';
            e.Class = ClassCaptive;
            e.Owner = u.Slot;
            e.Turns = ClampTurnsFor(B, u);
            e.Intent = null;
            e.Order = 0;
            e.Idle = false;
            e.Locked = false;
            e.Moved = e.Acted = true;
            u.GearLeft = 0;
            AddFx(B, new Fx { K = "capture", X = x, Y = y, Unit = e.Id, N = u.Id, Obj = e.Type });
            Say(B, Loc.F("{0} {1} makinesini ele geçirdi.", UnitTypes[u.Type].Name, LowerName(UnitTypes[e.Type].Name)), "good");
            CountTally(B, FacilityCounters.Capture);
        }

        /// <summary>
        /// Tutsakların süresi (adım 4, <see cref="PlanEnemies"/>'ten önce; K12): kalan turu 0 olan tutsak bırakılır (yeniden makine,
        /// bu planı niyetsiz geçirir: <see cref="Unit.Idle"/>), öbürlerinin kalan turu bir azalır. Tutsaksız tahtada hiçbir şey yapmaz;
        /// RNG çekmez. Fx "release": X/Y kare, Unit makine.
        /// </summary>
        static void AdvanceCaptives(BState B)
        {
            foreach (var u in B.Units)
            {
                if (!IsCaptive(u) || u.Dead || u.Out) continue;
                if (u.Turns > 0) { u.Turns--; continue; }
                u.Side = 'e';
                u.Class = null;
                u.Owner = -1;
                u.Intent = null;
                u.Locked = false;
                // Aşama 10, kısa bağ 2. kademe: bırakılan makine aynı planda niyet alır (planın bir çekimi fazla)
                u.Idle = !(B.Moves != null && B.Moves.ShortClamp >= 2);
                AddFx(B, new Fx { K = "release", X = u.X, Y = u.Y, Unit = u.Id, Obj = u.Type });
                Say(B, Loc.F("{0} yeniden YZ'nin.", UnitTypes[u.Type].Name), "bad");
            }
        }

        /// <summary>Tutsağın kendi eyleminin vurabileceği kare: ayakta makine ya da nesne (ikmal kasası ve yalnız Teknisyen'in kapattığı
        /// soğutma vanası hariç). Ekip tarafındaki birim hedef verilmez.</summary>
        static bool CommandHits(BState B, int x, int y)
        {
            if (!Inb(x, y)) return false;
            var v = UnitAt(B, x, y);
            if (v != null) return v.Side == 'e';
            var o = CellAt(B, x, y).O;
            return o != null && !(IsTarget(o.K) && IsLoot(B)) && o.K != "vana";
        }

        /// <summary>Komutla plaka alabilecek uzman: tahtada, plakasız.</summary>
        static bool PlateFits(Unit v) => IsSpecialist(v) && v.Armor == 0;

        /// <summary>
        /// Tutsağın komutunun hedefleri (K13): dron ve zırhlı dron dört yönde atışın vurduğu kare (tel geçer, sis ve bulut keser);
        /// bakım robotu dik komşu makine ya da nesne (2 hasar; bulutta makineye vurulmaz) ya da plakasız dik komşu uzman (plaka); havan
        /// üç kare öte (engellerin üstünden); patlayan robot kendi karesi (çevresinde ekip birimi yoksa ve vuracak bir şey varsa);
        /// kilitleyici düz hatta dört kare içindeki ilk makine; kalkan dronu plakasız dik komşu uzman, yoksa dron gibi; çoğaltıcının
        /// eylemi yok.
        /// </summary>
        static List<Pt> CommandTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            void Shots()
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    var t = IntentTarget(B, u, "shoot", dir, u.X, u.Y);
                    if (t != null && !t.None && CommandHits(B, t.X, t.Y)) o.Add(new Pt(t.X, t.Y));
                }
            }
            switch (u.Type)
            {
                case "dron": case "zdron": Shots(); break;
                case "kalkan":
                    foreach (var d in Dirs) if (Inb(u.X + d[0], u.Y + d[1]) && PlateFits(UnitAt(B, u.X + d[0], u.Y + d[1]))) o.Add(new Pt(u.X + d[0], u.Y + d[1]));
                    if (o.Count == 0) Shots();
                    break;
                case "bakim":
                    foreach (var d in Dirs)
                    {
                        int x = u.X + d[0], y = u.Y + d[1];
                        if (!Inb(x, y)) continue;
                        var v = UnitAt(B, x, y);
                        if (PlateFits(v) || CommandHits(B, x, y) && (v == null || !InCloud(B, u.X, u.Y) && !InCloud(B, x, y))) o.Add(new Pt(x, y));
                    }
                    break;
                case "havan":
                    for (int dir = 0; dir < 4; dir++)
                    {
                        var t = IntentTarget(B, u, "lob", dir, u.X, u.Y);
                        if (t != null && !t.None && CommandHits(B, t.X, t.Y)) o.Add(new Pt(t.X, t.Y));
                    }
                    break;
                case "bomba":
                {
                    var area = BlastArea(u.X, u.Y);
                    if (!area.Any(a => UnitAt(B, a.X, a.Y)?.Side == 'p') && area.Any(a => CommandHits(B, a.X, a.Y))) o.Add(new Pt(u.X, u.Y));
                    break;
                }
                case "kilit":
                    for (int dir = 0; dir < 4; dir++)
                    {
                        var t = IntentTarget(B, u, "lock", dir, u.X, u.Y);
                        if (t != null && !t.None && UnitAt(B, t.X, t.Y)?.Side == 'e') o.Add(new Pt(t.X, t.Y));
                    }
                    break;
            }
            return o;
        }

        /// <summary>Tutsağın komutu (K13; hasar bugünkü <see cref="DamageAt"/>/<see cref="Hurt"/> yoluyla, zincirler aynen). Fx "command":
        /// X/Y hedef, Unit tutsak, Obj tür (patlamada sonra "blast" ve "die", plakada "plate-give").</summary>
        static void ApplyCommand(BState B, Unit u, int x, int y)
        {
            string name = LowerName(UnitTypes[u.Type].Name);
            AddFx(B, new Fx { K = "command", X = x, Y = y, Unit = u.Id, Obj = u.Type });
            var v = UnitAt(B, x, y);
            if ((u.Type == "bakim" || u.Type == "kalkan") && PlateFits(v))
            {
                v.Armor = 1;
                AddFx(B, new Fx { K = "plate-give", X = x, Y = y, Unit = v.Id, N = u.Id });
                Say(B, Loc.F("Tutsak {0}, {1} için zırh plakası taktı.", name, LowerName(UnitTypes[v.Type].Name)), "good");
                return;
            }
            switch (u.Type)
            {
                case "bakim":
                    Say(B, Loc.F("Tutsak {0} vurdu.", name), "good");
                    DamageAt(B, x, y, 2, Loc.T("tutsak"));
                    break;
                case "havan":
                    Say(B, Loc.F("Tutsak {0} ateş etti.", name), "good");
                    Blasted(B, x, y);
                    DamageAt(B, x, y, 1, Loc.T("tutsak"));
                    break;
                case "bomba":
                    AddFx(B, new Fx { K = "blast", X = u.X, Y = u.Y, Unit = u.Id });
                    Say(B, Loc.F("Tutsak {0} infilak etti.", name), "good");
                    foreach (var a in BlastArea(u.X, u.Y)) { Blasted(B, a.X, a.Y); DamageAt(B, a.X, a.Y, 1, Loc.T("patlama")); }
                    if (!u.Dead) { u.Dead = true; u.Hp = 0; AddFx(B, new Fx { K = "die", X = u.X, Y = u.Y, Unit = u.Id }); }
                    break;
                case "kilit":
                    v.Idle = true;
                    AddFx(B, new Fx { K = "lock", X = x, Y = y, Unit = v.Id });
                    Say(B, Loc.F("Tutsak {0}, {1} makinesini kilitledi: bir sonraki planında niyeti yok.", name, LowerName(UnitTypes[v.Type].Name)), "good");
                    break;
                default:
                    Say(B, Loc.F("Tutsak {0} ateş etti.", name), "good");
                    DamageAt(B, x, y, 1, Loc.T("tutsak"));
                    break;
            }
        }

        // ---------- Köpek (belge §5.5) ----------
        /// <summary>Havla: bu <see cref="EndTurn"/>'ün planında erimdeki makineler köpeği ekip üyesi gibi puanlar (<see cref="TeamLike"/>).
        /// Fx "bark": X/Y köpeğin karesi, Unit köpek.</summary>
        static void ApplyBark(BState B, Unit u)
        {
            u.Bark = true;
            AddFx(B, new Fx { K = "bark", X = u.X, Y = u.Y, Unit = u.Id });
            Say(B, Loc.T("Köpek havladı: yakındaki makineler ona yönelecek."), "good");
            CountTally(B, FacilityCounters.Dog);
        }

        /// <summary>Getir: yerdeki yük köpeğin karesinde ya da dik komşusundaysa hedef onun karesi (köpek yürüyerek yük almaz, K15).</summary>
        static List<Pt> FetchTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            if (B.CargoX >= 0 && Man(B.CargoX, B.CargoY, u.X, u.Y) <= 1) o.Add(new Pt(B.CargoX, B.CargoY));
            return o;
        }

        /// <summary>Köpek yükü alır (taşıyıcı köpek; düşerse yük bugünkü kuralla düşer, tesisin girişinde tahliye olursa yük çıkar). Fx
        /// "fetch": X/Y yükün karesi, Unit köpek.</summary>
        static void ApplyFetch(BState B, Unit u, int x, int y)
        {
            B.Carrier = u.Id;
            B.CargoX = B.CargoY = -1;
            AddFx(B, new Fx { K = "fetch", X = x, Y = y, Unit = u.Id });
            Say(B, Loc.T("Köpek yükü kaptı."), "good");
            CountTally(B, FacilityCounters.Dog);
        }

        /// <summary>Kokla: kaçıştan önce, odada henüz koklanmadıysa ve odanın ileri geçidi varsa (hedef odada yok), köpeğin karesindeki ya
        /// da dik komşusundaki ileri geçit kareleri.</summary>
        static List<Pt> SniffTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            if (B.Escape || B.Sniffed != 0 || B.Room == null) return o;
            foreach (var f in B.Room.Forward) if (Man(f.X, f.Y, u.X, u.Y) <= 1) o.Add(f);
            return o;
        }

        /// <summary>Köpek sıradaki odanın kokusunu alır: <see cref="BState.Sniffed"/> = sıradaki oda (oda raporunun Sniff'i; planı
        /// <c>Simulation.AdvanceRaidRoom</c> öğrenir, K30). Fx "sniff": X/Y geçit karesi, Unit köpek, N sıradaki oda.</summary>
        static void ApplySniff(BState B, Unit u, int x, int y)
        {
            B.Sniffed = B.Room.Index + 1;
            AddFx(B, new Fx { K = "sniff", X = x, Y = y, Unit = u.Id, N = B.Sniffed });
            Say(B, Loc.T("Köpek sıradaki odanın kokusunu aldı: plan çıktı."), "good");
            CountTally(B, FacilityCounters.Dog);
        }

        // ---------- Oda girişi: köpek ve tutsak (§5.8, §5.9) ----------
        /// <summary>
        /// Odaya ekiple giren uzman olmayan birimlerin kayıtları (sıra: köpekler sahibin kadro sırasıyla, sonra tutsaklar): ilk odada
        /// (<see cref="RoomSetup.Carry"/> null) emirde köpeği olan her kişinin köpeği, sonraki odalarda önceki raporun ayaktaki köpekleri
        /// ve tutsakları (<see cref="RoomSetup.CarryUnits"/>; düşen köpek yalnız raporda taşınır). Ekipmansız kadroda boş.
        /// </summary>
        static List<RoomUnit> EnteringUnits(BState B, List<SquadSlot> squad, RoomSetup setup)
        {
            var o = new List<RoomUnit>();
            if (setup.Carry == null)
            {
                for (int i = 0; i < squad.Count; i++)
                    if (squad[i].Gear == FacilityGear.Dog) o.Add(new RoomUnit { Class = ClassDog, Type = "kopek", Owner = i, State = "a", Hp = B.Moves.DogHp });
                return o;
            }
            if (setup.CarryUnits == null) return o;
            foreach (var r in setup.CarryUnits)
                if (r.State == "a" && (r.Class == ClassDog || r.Class == ClassCaptive) && r.Type != null && UnitTypes.ContainsKey(r.Type)) o.Add(r);
            return o;
        }

        /// <summary>Giren birimler ayrılmış karelerine (birim sonuna; karesi olmayan konmaz). Köpek: can en çok <see cref="MoveRules.DogHp"/>
        /// (Kürk dahil), yürüyüş <see cref="MoveRules.DogMove"/>. Tutsak: türün birimi, ekip tarafı, kalan oyuncu turu ve can rapordan.</summary>
        static void AddUnits(BState B, List<RoomUnit> units, List<Pt?> spots)
        {
            for (int k = 0; k < units.Count; k++)
            {
                if (k >= spots.Count || !(spots[k] is Pt at)) continue;
                var r = units[k];
                var u = NewUnit(B, r.Type, at.X, at.Y);
                u.Class = r.Class;
                u.Assembled = r.Assembled;
                u.Owner = r.Owner;
                u.Side = 'p';
                if (r.Class == ClassDog)
                {
                    u.Max = B.Moves.DogHp;
                    u.MoveRange = B.Moves.DogMove;
                    if (B.Moves.Titles && r.Owner >= 0 && r.Owner < (B.Params?.Squad?.Count ?? 0))
                        u.Titles = B.Params.Squad[r.Owner].Titles & FacilityTitle.Handler;
                }
                else u.Turns = Math.Max(0, r.Turns);
                u.Hp = Math.Max(1, Math.Min(u.Max, r.Hp));
                u.Armor = Math.Max(0, r.Armor);
                B.Units.Add(u);
            }
        }

        /// <summary>Dönüş tahtasının taşıyıcısı: yükü köpek taşıyorsa (<see cref="RoomSetup.CargoDog"/>) sahibi <paramref name="cargo"/> olan
        /// köpek, değilse o kadro sırasındaki uzman; yoksa 0.</summary>
        static int CarrierOf(BState B, int cargo, bool dog) =>
            B.Units.FirstOrDefault(u => dog ? IsDog(u) && u.Owner == cargo : u.Side == 'p' && u.Slot == cargo)?.Id ?? 0;

        // ---------- Hedefler ve uygulama ----------
        /// <summary>Ekipman eylemlerinin hedefleri (<see cref="ActionTargets"/>'ın aşama 8 dalı).</summary>
        static List<Pt> GearTargets(BState B, Unit u, string kind)
        {
            switch (kind)
            {
                case ActWire: case ActPlank: return BarrierTargets(B, u);
                case ActCharge: return ChargeTargets(B, u);
                case ActTurret: case ActDecoy: case ActWinch: return GadgetTargets(B, u);
                default: return UnitTargets(B, u, kind);
            }
        }

        /// <summary>Ekipman eylemlerinin kuralı ve hak bedeli (<see cref="ApplyAction"/>'ın aşama 8 dalı).</summary>
        static void ApplyGear(BState B, Unit u, string kind, int x, int y)
        {
            switch (kind)
            {
                case ActWire: case ActPlank: ApplyBarrier(B, u, kind, x, y); break;
                case ActCharge: ApplyCharge(B, u, x, y); break;
                case ActTurret: case ActDecoy: case ActWinch: ApplyGadget(B, u, kind, x, y); break;
                default: ApplyUnitAction(B, u, kind, x, y); break;
            }
        }

        /// <summary>
        /// Engelin ve aygıtın konabileceği kare: tahtada, zemin (kablo değil), nesne, ayakta birim, yatan gövde ve yerdeki yük yok, kapı
        /// ve menfez karesi değil (K7).
        /// </summary>
        static bool FreeFloor(BState B, int x, int y)
        {
            if (!Inb(x, y)) return false;
            var c = CellAt(B, x, y);
            if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || UnitAt(B, x, y) != null) return false;
            if (B.CargoX == x && B.CargoY == y) return false;
            return !B.Units.Any(b => LyingBody(b) && b.X == x && b.Y == y);
        }

        /// <summary>
        /// Ekip güvencesinin bağları (K7, belge §11.6 "engel ekibi çıkıştan koparamaz"): tahtadaki her uzman için (birim sırasıyla)
        /// geçilebilir karelerden (birimler sayılmaz) odanın batı karelerinden birine, kaçıştan önce ileri geçidin bir karesine (varsa) ve
        /// hedef odada kaçıştan önce kalan her hedefin bir dik komşusuna ulaşıyor mu. Örnek tahtada boş.
        /// </summary>
        static List<bool> TeamLinks(BState B)
        {
            var links = new List<bool>();
            if (B.Room == null) return links;
            var comp = new int[N * N];
            for (int i = 0; i < comp.Length; i++) comp[i] = -1;
            int next = 0;
            foreach (var t in AllTiles())
            {
                if (comp[Key(t)] >= 0 || !Passable(B, t.X, t.Y)) continue;
                var q = new Queue<Pt>();
                comp[Key(t)] = next;
                q.Enqueue(t);
                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    foreach (var d in Dirs)
                    {
                        int x = p.X + d[0], y = p.Y + d[1];
                        if (!Passable(B, x, y) || comp[y * N + x] >= 0) continue;
                        comp[y * N + x] = next;
                        q.Enqueue(new Pt(x, y));
                    }
                }
                next++;
            }
            bool forward = !B.Escape && B.Room.Forward.Count > 0;
            var targets = B.Room.Type == RoomType.Target && !B.Escape ? TargetsLeft(B) : new List<(int X, int Y, Obj O)>();
            foreach (var s in B.Units)
            {
                if (!IsSpecialist(s) || !OnBoard(s)) continue;
                int c = Inb(s.X, s.Y) ? comp[Key(new Pt(s.X, s.Y))] : -1;
                links.Add(c >= 0 && B.Room.West.Any(w => comp[Key(w)] == c));
                if (forward) links.Add(c >= 0 && B.Room.Forward.Any(f => comp[Key(f)] == c));
                foreach (var t in targets)
                    links.Add(c >= 0 && Dirs.Any(d => Inb(t.X + d[0], t.Y + d[1]) && comp[(t.Y + d[1]) * N + t.X + d[0]] == c));
            }
            return links;
        }

        /// <summary>Değişiklikten önce tutan her bağ sonra da tutuyor mu (<see cref="ExitSafe"/> kalıbı: önceden kopuk bağ yasaklamaz).</summary>
        static bool TeamSafe(BState B, List<bool> before)
        {
            var after = TeamLinks(B);
            if (after.Count != before.Count) return false;
            for (int i = 0; i < before.Count; i++) if (before[i] && !after[i]) return false;
            return true;
        }

        /// <summary>Karede <paramref name="k"/> nesnesi ekip güvencesini bozmaz mı (tahtada dener, geri koyar).</summary>
        static bool BarrierFits(BState B, int x, int y, string k, List<bool> before)
        {
            var c = CellAt(B, x, y);
            var was = c.O;
            c.O = NewObj(k, 1);
            bool ok = TeamSafe(B, before);
            c.O = was;
            return ok;
        }

        // ---------- Örme seti: tel ve kalas (belge §5.3) ----------
        /// <summary>Tel ve kalasın hedefi: uzmanın dört dik komşusundan boş zemin (<see cref="FreeFloor"/>) olup ekip güvencesini bozmayanlar.</summary>
        static List<Pt> BarrierTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            List<bool> before = null;
            foreach (var d in Dirs)
            {
                int x = u.X + d[0], y = u.Y + d[1];
                if (!FreeFloor(B, x, y)) continue;
                before = before ?? TeamLinks(B);
                if (BarrierFits(B, x, y, "kalas", before)) o.Add(new Pt(x, y));
            }
            return o;
        }

        /// <summary>Tel örgü (1 hak) ya da kalas duvar (<see cref="MoveRules.PlankCost"/> hak) kurulur. Fx "build": X/Y kare, Obj tür,
        /// Unit uzman.</summary>
        static void ApplyBarrier(BState B, Unit u, string kind, int x, int y)
        {
            bool plank = kind == ActPlank;
            string k = plank ? "kalas" : "tel";
            // Aşama 10: Çelik tel seti (cevap) telin canını artırır (kalas değil)
            CellAt(B, x, y).O = NewObj(k, plank ? B.Moves.PlankHp : B.Moves.WireHp + B.Moves.SteelWireHp);
            u.GearLeft -= plank ? B.Moves.PlankCost : 1;
            AddFx(B, new Fx { K = "build", X = x, Y = y, Unit = u.Id, Obj = k });
            Say(B, plank ? Loc.F("{0} kalas duvar kurdu.", UnitTypes[u.Type].Name) : Loc.F("{0} tel ördü.", UnitTypes[u.Type].Name), "good");
            CountTally(B, FacilityCounters.Build);
        }

        // ---------- Dinamit lokumu (belge §5.2) ----------
        /// <summary>Lokumun hedefi: dik komşu duvar; kepenk ve büyük sandığın nişi değil (D10), buluttan ve buluta değil (K31).</summary>
        static List<Pt> ChargeTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            if (InCloud(B, u.X, u.Y)) return o;
            foreach (var d in Dirs)
            {
                int x = u.X + d[0], y = u.Y + d[1];
                if (!Inb(x, y) || InCloud(B, x, y)) continue;
                var c = CellAt(B, x, y);
                // Aşama 10: çift cidar tek hamlede bedelinin tamamını ister (yetmezse hedef değil)
                if (c.T == "wall" && !c.Shutter && !c.Crate && (!c.Double || u.GearLeft >= DoubleWallCost(B))) o.Add(new Pt(x, y));
            }
            return o;
        }

        /// <summary>Lokum duvarı yıkar (dinamitin duvar kuralı, <see cref="Breach"/>; bir hak). Fx "charge" sonra "breach".</summary>
        static void ApplyCharge(BState B, Unit u, int x, int y)
        {
            var d = new[] { Sign(x - u.X), Sign(y - u.Y) };
            AddFx(B, new Fx { K = "charge", X = x, Y = y, Unit = u.Id });
            var c = CellAt(B, x, y);
            if (c.Double)
            {
                // Aşama 10: çift cidar (Delici şarjla daha az hak) bir hamlede yıkılır
                u.GearLeft -= DoubleWallCost(B);
                c.Double = false;
                AddFx(B, new Fx { K = "dwall", X = x, Y = y, Unit = u.Id });
                Say(B, Loc.T("Çift cidar yıkıldı."), "good");
            }
            else u.GearLeft--;
            Breach(B, x, y, d, Loc.F("{0} lokumla duvarı yıktı.", UnitTypes[u.Type].Name));
            CountTally(B, FacilityCounters.Breach);
        }

        // ---------- Aygıtlar: taret, yem, vinç (belge §5.2) ----------
        /// <summary>Aygıtın kurulacağı kare: uzmanın dik komşusu, boş zemin (<see cref="FreeFloor"/>). Yön uzmandan kareye (K32).</summary>
        static List<Pt> GadgetTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            foreach (var d in Dirs) if (FreeFloor(B, u.X + d[0], u.Y + d[1])) o.Add(new Pt(u.X + d[0], u.Y + d[1]));
            return o;
        }

        /// <summary>Aygıt birimi (tahtaya eklenmez): sınıf, yön, kalan tur, kuranın kadro sırası; can en çok <see cref="MoveRules.DeviceHp"/>.</summary>
        static Unit NewGadget(BState B, string type, int x, int y, int dir, int turns, int owner, int hp, int armor)
        {
            var g = NewUnit(B, type, x, y);
            g.Class = ClassGadget;
            g.Max = B.Moves?.DeviceHp ?? Math.Max(1, hp);
            g.Hp = Math.Max(1, Math.Min(g.Max, hp));
            g.Armor = Math.Max(0, armor);
            g.Dir = dir;
            g.Turns = turns;
            g.Owner = owner;
            return g;
        }

        /// <summary>Aygıt kurulur (birim sonuna eklenir, bir hak). Fx "gadget": X/Y kare, Unit aygıt, N uzman, Dx/Dy yön.</summary>
        static void ApplyGadget(BState B, Unit u, string kind, int x, int y)
        {
            int dir = Array.FindIndex(Dirs, d => d[0] == x - u.X && d[1] == y - u.Y);
            // Aşama 10: parazit (aygıt önlemi) süreyi kısaltır, Çift zemberek (cevap) uzatır
            int turns = DeviceTurnsFor(B, u);
            var g = NewGadget(B, kind, x, y, dir, turns, u.Slot, B.Moves.DeviceHp, 0);
            B.Units.Add(g);
            u.GearLeft--;
            AddFx(B, new Fx { K = "gadget", X = x, Y = y, Unit = g.Id, N = u.Id, Dx = Dirs[dir][0], Dy = Dirs[dir][1], Obj = kind });
            Say(B, Loc.F("{0} {1} kurdu.", UnitTypes[u.Type].Name, LowerName(UnitTypes[kind].Name)), "good");
            if (B.Moves.Jam >= 1 && B.Moves.JamTurns > 0)
            {
                AddFx(B, new Fx { K = "jam", X = x, Y = y, Unit = g.Id, N = turns });
                Say(B, Loc.F("Parazit: aygıt {0} tur çalışacak.", turns), "bad");
            }
            CountTally(B, FacilityCounters.Gadget);
            RecordWork(B, u, FacilityTitle.Builder);
        }

        /// <summary>
        /// Aygıtların çalışması (K3; makine evresi: niyetlerden ve sisin dağılmasından sonra, kaçış denetiminden ve uyanmadan önce).
        /// Birim sırasıyla her ayaktaki aygıt kuralını uygular, sonra bir turu düşer; süresi biten hurdaya döner. Örnek tahtada hiçbir
        /// şey yapmaz. RNG çekmez.
        /// </summary>
        static void RunDevices(BState B)
        {
            if (B.Room == null || B.Moves == null) return;
            foreach (var g in B.Units.ToList())
            {
                if (B.Phase != "player") return;
                if (!IsGadget(g) || !OnBoard(g)) continue;
                if (g.Dir >= 0)
                {
                    if (g.Type == "taret") RunTurret(B, g);
                    else if (g.Type == "yem") RunDecoy(B, g);
                    else if (g.Type == "vinc") RunWinch(B, g);
                }
                if (g.Dead) continue;
                g.Turns--;
                if (g.Turns <= 0) GadgetDown(B, g, false);
            }
        }

        /// <summary>Taret: hat 1..<see cref="MoveRules.TurretRange"/> (tahta dışı, duvar, tel dışındaki nesne ya da bulutta biter); ilk
        /// ayakta birim makineyse 1 hasar, ekip tarafındaysa atmaz. Fx "turret-hit": X/Y kare, Unit makine, N taret.</summary>
        static void RunTurret(BState B, Unit g)
        {
            var d = Dirs[g.Dir];
            for (int k = 1; k <= B.Moves.TurretRange; k++)
            {
                int x = g.X + d[0] * k, y = g.Y + d[1] * k;
                if (!Inb(x, y) || CellAt(B, x, y).T == "wall" || StopsShot(CellAt(B, x, y).O) || InCloud(B, x, y)) return;
                var t = UnitAt(B, x, y);
                if (t == null) continue;
                if (t.Side == 'e')
                {
                    AddFx(B, new Fx { K = "turret-hit", X = x, Y = y, Unit = t.Id, N = g.Id });
                    Say(B, Loc.F("Taret: {0} vuruldu.", UnitTypes[t.Type].Name), "good");
                    Hurt(B, t, 1, Loc.T("taret"));
                }
                return;
            }
        }

        /// <summary>Yem: yönünde <see cref="MoveRules.DecoyStep"/> kez bir kare yürür; kare tahtada, geçilebilir, ayakta birimsiz, kapı ve
        /// menfez değilse (turnike açık). Kablo ve yağ bugünkü kural. Fx "decoy-step": X/Y eski kare, Dx/Dy adım, Unit yem.</summary>
        static void RunDecoy(BState B, Unit g)
        {
            var d = Dirs[g.Dir];
            for (int i = 0; i < B.Moves.DecoyStep && !g.Dead; i++)
            {
                int nx = g.X + d[0], ny = g.Y + d[1];
                if (!Passable(B, nx, ny) || UnitAt(B, nx, ny) != null || CellAt(B, nx, ny).Door != 0 || CellAt(B, nx, ny).Vent != 0) return;
                if (B.Ev != null && !CanStep(B, g.X, g.Y, nx, ny)) return;
                AddFx(B, new Fx { K = "decoy-step", X = g.X, Y = g.Y, Dx = d[0], Dy = d[1], Unit = g.Id });
                g.X = nx; g.Y = ny;
                CableHurt(B, g);
                Slide(B, g, d);
            }
        }

        /// <summary>
        /// Vinç: hat 1..<see cref="MoveRules.WinchRange"/> (tahta dışı, duvar ya da her nesnede biter). İlk dolu kare: ayakta birim
        /// (aygıt hattı keser) ya da yoksa o karedeki gövdeler ve yük (D16'ya tek istisna, K34); <see cref="MoveRules.WinchPull"/> kez bir
        /// kare vince doğru çekilir. Çekilen yük almaz, kapıya girmez.
        /// </summary>
        static void RunWinch(BState B, Unit g)
        {
            var d = Dirs[g.Dir];
            var back = new[] { -d[0], -d[1] };
            for (int k = 1; k <= B.Moves.WinchRange; k++)
            {
                int x = g.X + d[0] * k, y = g.Y + d[1] * k;
                if (!Inb(x, y) || CellAt(B, x, y).T == "wall" || CellAt(B, x, y).O != null) return;
                var t = UnitAt(B, x, y);
                if (t != null)
                {
                    // Aşama 10: cıvatalı makine çekilmez; hat yine orada biter
                    if (Bolted(B, t)) BoltHold(B, t);
                    else if (WinchPulls(B, t)) PullUnit(B, g, t, back);
                    return;
                }
                var bodies = B.Units.Where(b => LyingBody(b) && b.X == x && b.Y == y).ToList();
                bool cargo = B.CargoX == x && B.CargoY == y;
                if (bodies.Count == 0 && !cargo) continue;
                PullLoad(B, g, bodies, cargo, x, y, back);
                return;
            }
        }

        /// <summary>Vinç hattındaki ilk ayakta birim çekilir mi: aygıt hattı keser, cıvatalı makine (aşama 10) direnir; ikisinde de hat
        /// orada biter. Sunumun önizlemesi (<c>FacilityGearView.WinchLine</c>) aynı kuralı buradan okur.</summary>
        public static bool WinchPulls(BState B, Unit t) => t != null && !IsGadget(t) && !Bolted(B, t);

        /// <summary>Ayakta birimin çekilmesi: varış tahtada, geçilebilir, ayakta birimsiz (vincin karesi dahil), turnike açık; kablo ve
        /// yağ bugünkü kural. Fx "winch-pull": X/Y eski kare, Dx/Dy adım, Unit çekilen, N vinç.</summary>
        static void PullUnit(BState B, Unit g, Unit t, int[] back)
        {
            bool moved = false;
            for (int i = 0; i < B.Moves.WinchPull && !t.Dead; i++)
            {
                int nx = t.X + back[0], ny = t.Y + back[1];
                if (!Passable(B, nx, ny) || UnitAt(B, nx, ny) != null) break;
                if (B.Ev != null && !CanStep(B, t.X, t.Y, nx, ny)) break;
                AddFx(B, new Fx { K = "winch-pull", X = t.X, Y = t.Y, Dx = back[0], Dy = back[1], Unit = t.Id, N = g.Id });
                t.X = nx; t.Y = ny;
                moved = true;
                CableHurt(B, t);
                Slide(B, t, back);
            }
            if (moved) Say(B, Loc.F("Vinç {0} çekti.", LowerName(UnitTypes[t.Type].Name)), t.Side == 'e' ? "good" : "sys");
            // Aşama 10: yalnız makine çekişi itme sayılır (gövde, yük ve ekip çekişi kurtarmadır)
            if (moved && t.Side == 'e') CountTally(B, FacilityCounters.Push);
        }

        /// <summary>Gövdelerin ve yükün çekilmesi: varış ayrıca kapı ve menfez karesi değil (K34). Fx "winch-pull": gövdede Unit gövde,
        /// yükte Obj "cargo".</summary>
        static void PullLoad(BState B, Unit g, List<Unit> bodies, bool cargo, int x, int y, int[] back)
        {
            bool moved = false;
            for (int i = 0; i < B.Moves.WinchPull; i++)
            {
                int nx = x + back[0], ny = y + back[1];
                if (!Passable(B, nx, ny) || UnitAt(B, nx, ny) != null || CellAt(B, nx, ny).Door != 0 || CellAt(B, nx, ny).Vent != 0) break;
                foreach (var b in bodies)
                {
                    AddFx(B, new Fx { K = "winch-pull", X = x, Y = y, Dx = back[0], Dy = back[1], Unit = b.Id, N = g.Id });
                    b.X = nx; b.Y = ny;
                }
                if (cargo)
                {
                    AddFx(B, new Fx { K = "winch-pull", X = x, Y = y, Dx = back[0], Dy = back[1], N = g.Id, Obj = "cargo" });
                    B.CargoX = nx; B.CargoY = ny;
                }
                x = nx; y = ny;
                moved = true;
            }
            if (moved) Say(B, cargo && bodies.Count == 0 ? Loc.T("Vinç yükü çekti.") : Loc.T("Vinç düşeni çekti."), "good");
        }

        /// <summary>
        /// Aygıtın sonu (K11): süresi bitti (<paramref name="broken"/> false) ya da kırıldı (<see cref="Hurt"/>). Birim düşer; karesine
        /// <see cref="MoveRules.ScrapHp"/> canlı hurda konur. Konmaz (aygıt yine kalkar): zemin olmayan, kapı ya da menfez karesi, yatan
        /// gövde ya da yerdeki yük karesi, ya da ekip güvencesi bozuluyor. Fx "device-break" / "scrap".
        /// </summary>
        static void GadgetDown(BState B, Unit g, bool broken)
        {
            g.Dead = true;
            g.Hp = 0;
            AddFx(B, new Fx { K = broken ? "device-break" : "scrap", X = g.X, Y = g.Y, Unit = g.Id });
            Say(B, broken ? Loc.F("{0} kırıldı.", UnitTypes[g.Type].Name) : Loc.F("{0} durdu.", UnitTypes[g.Type].Name), "sys");
            if (!FreeFloor(B, g.X, g.Y) || !BarrierFits(B, g.X, g.Y, "hurda", TeamLinks(B))) return;
            CellAt(B, g.X, g.Y).O = NewObj("hurda", B.Moves?.ScrapHp ?? 1);
            Say(B, Loc.T("Aygıt hurdaya döndü."), "sys");
        }

        // ---------- Test kancaları ----------
        /// <summary>Yalnız testler: makinenin bulunduğu kareden niyet puanı (<see cref="ScoreTarget"/>).</summary>
        internal static double TestScore(BState B, Unit e, string kind, int dir, bool stuck = false) => ScoreTarget(B, e, kind, dir, e.X, e.Y, false, stuck);

        /// <summary>Yalnız testler: makine aklının ekip yüklemi (<see cref="TeamLike"/>).</summary>
        internal static bool TestTeamLike(BState B, Unit e, Unit u) => TeamLike(B, e, u);
    }
}
