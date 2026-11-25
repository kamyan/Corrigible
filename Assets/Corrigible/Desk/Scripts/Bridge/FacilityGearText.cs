using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis baskınının ekipmanı ve uzman olmayan ekip birimlerinin oyuncu metinleri (aşama 9; Docs/TESIS-BASKINLARI-TASARIMI.md §5,
    /// §11.5): uzmanın ekipman satırı ve R döngüsünün seçenekleri, köpeğin ve tutsağın komutları (E satırları), eylemlerin adı, ayrıntısı,
    /// kullanılamama nedeni, kurulu eylemin iki etkisi, telsiz fişleri; tutsağın kalan turu, aygıtın kalan çalışması; hover kartının
    /// aygıt, köpek, tutsak, niyetsiz makine ve engel satırları. Saf ve durumsuz: tahtayı okur, değiştirmez; kararı Core verir
    /// (<see cref="Baskin.CanAction"/>, <see cref="Baskin.ActionTargets"/>), metin yalnız açıklar. Değerler
    /// <see cref="Baskin.BState.Moves"/>'tan (kural 7). Örnek tahtada (Room ya da Moves null) seçenek ve satır üretmez.
    /// </summary>
    public static class FacilityGearText
    {
        static string Cell(Baskin.BState B, int x, int y) => RaidBoard3D.CellName(B?.Room?.Index ?? 0, x, y);
        static Baskin.Unit ById(Baskin.BState B, int id) => B?.Units.FirstOrDefault(v => v.Id == id);
        static DeskLang Lang => Loc.Lang == Loc.English ? DeskLang.En : DeskLang.Tr;
        static string Lower(string s) => DeskText.Lower(s, Lang);
        static RaidCardRow Row(string icon, string text, bool good = false) => new RaidCardRow { Icon = icon, Text = text, Good = good };

        /// <summary>Aşama 8'in ekipman ya da birim eylemi mi (<see cref="Baskin.GearActionKinds"/>, <see cref="Baskin.UnitActionKinds"/>):
        /// <see cref="FacilityMoveText"/> bu türlerin metnini buraya devreder.</summary>
        public static bool Handles(string kind) => kind != null && (Array.IndexOf(Baskin.GearActionKinds, kind) >= 0 || Array.IndexOf(Baskin.UnitActionKinds, kind) >= 0);

        /// <summary>Satır simgesi (var olan sprite'lar): ekipman ve komuta yıldırım, getir bot, havla ve kokla göz.</summary>
        public static string Icon(string kind) => kind == Baskin.ActFetch ? "ic_boots_s" : kind == Baskin.ActBark || kind == Baskin.ActSniff ? "ic_eye_s" : "ic_bolt_s";

        // ---------- Birimler ----------
        /// <summary>Kadro sırası <paramref name="slot"/> olan uzman (köpeğin sahibi, aygıtı kuran, kelepçeleyen); yoksa null.</summary>
        public static Baskin.Unit OwnerOf(Baskin.BState B, int slot) => slot < 0 ? null : B?.Units.FirstOrDefault(v => Baskin.IsSpecialist(v) && v.Slot == slot);

        /// <summary>Sahibin kısa adı (kişinin adı; kişisiz kadroda rolün adı); sahip yoksa boş.</summary>
        public static string OwnerName(Baskin.BState B, Baskin.Unit u) => FacilityMoveText.Who(OwnerOf(B, u?.Owner ?? -1));

        /// <summary>Köpeğin telsiz ve kart adı: "KÖPEK · {sahip}" (sahip yoksa "KÖPEK").</summary>
        public static string DogName(Baskin.BState B, Baskin.Unit dog)
        {
            string owner = OwnerName(B, dog);
            return string.IsNullOrEmpty(owner) ? Loc.T("KÖPEK") : Loc.F("KÖPEK · {0}", owner);
        }

        /// <summary>
        /// Tutsağın oynayacağı oyuncu turu sayısı (K10): yakalandığı turda <see cref="Baskin.Unit.Turns"/> (kelepçenin tam süresi),
        /// sonraki turlarda Turns + 1 (Core her tur sonunda bir düşer, 0 iken bir tur daha oynar, sonra bırakır). 1: son tur.
        /// </summary>
        public static int CaptiveLeft(Baskin.BState B, Baskin.Unit u)
        {
            if (u == null) return 0;
            // Aşama 10: kelepçenin verdiği tur Core'dan (kısa bağ, Yedek röle; Baskin.ClampTurnsFor; kuralsız tahtada dengenin değeri)
            int full = Baskin.ClampTurnsFor(B, u);
            return u.Turns >= full ? u.Turns : u.Turns + 1;
        }

        /// <summary>Tutsağın kalan tur notu: "TUTSAK · {k} TUR", son turda "SON TUR".</summary>
        public static string CaptiveText(Baskin.BState B, Baskin.Unit u)
        {
            int k = CaptiveLeft(B, u);
            return k <= 1 ? Loc.T("SON TUR") : Loc.F("TUTSAK · {0} TUR", k);
        }

        /// <summary>Aygıtın kalan çalışması (makine evresi sayısı; Core her evrenin sonunda bir düşer, 0'da hurda): "{0} TUR".</summary>
        public static string DeviceTurnsText(Baskin.Unit u) => Loc.F("{0} TUR", Math.Max(0, u?.Turns ?? 0));

        /// <summary>Yönün adı (<see cref="Baskin.Dirs"/> dizini: 0 kuzey, 1 doğu, 2 güney, 3 batı).</summary>
        public static string DirName(int dir) => dir switch { 0 => Loc.T("kuzey"), 1 => Loc.T("doğu"), 2 => Loc.T("güney"), 3 => Loc.T("batı"), _ => string.Empty };

        // ---------- Seçenekler ----------
        /// <summary>
        /// Uzman olmayan birimin bu odada anlamlı komutları (<see cref="Baskin.UnitActionKinds"/> sırası): köpekte havla (her zaman),
        /// getir (yük yerdeyken), kokla (ileri geçitli odada kaçıştan önce; koklandıysa kullanılamaz satır kalır); tutsakta komuta;
        /// aygıtta ve uzmanda boş.
        /// </summary>
        public static List<string> UnitKinds(Baskin.BState B, Baskin.Unit u)
        {
            var o = new List<string>();
            if (B?.Room == null || u == null) return o;
            if (Baskin.IsDog(u))
            {
                o.Add(Baskin.ActBark);
                if (B.CargoX >= 0) o.Add(Baskin.ActFetch);
                if (!B.Escape && B.Room.Forward.Count > 0) o.Add(Baskin.ActSniff);
            }
            else if (Baskin.IsCaptive(u)) o.Add(Baskin.ActCommand);
            return o;
        }

        /// <summary>
        /// Uzmanın ekipman eylemleri (<see cref="Baskin.GearActionKinds"/> sırası, taşıdığı ekipmanınkiler: örme setinde tel ve kalas,
        /// öbürlerinde tek eylem). Kullanılabilirlik Core'dan (önkoşul ve hedef). Örnek tahtada, ekipmansız uzmanda ve uzman olmayanda boş.
        /// </summary>
        public static List<MoveOption> GearOptions(Baskin.BState B, Baskin.Unit u)
        {
            var o = new List<MoveOption>();
            if (B?.Room == null || B.Moves == null || !Baskin.IsSpecialist(u) || !Baskin.OnBoard(u) || string.IsNullOrEmpty(u.Gear)) return o;
            foreach (var kind in Baskin.GearActionKinds)
                if (Baskin.GearOf(kind) == u.Gear) o.Add(FacilityMoveText.Make(B, u, kind, 0));
            return o;
        }

        /// <summary>Eylemin satır adı.</summary>
        public static string Name(string kind) => kind switch
        {
            Baskin.ActWire => Loc.T("Tel ör"),
            Baskin.ActPlank => Loc.T("Kalas kur"),
            Baskin.ActCharge => Loc.T("Lokumla yık"),
            Baskin.ActTurret => Loc.T("Taret kur"),
            Baskin.ActDecoy => Loc.T("Yem sal"),
            Baskin.ActWinch => Loc.T("Vinç kur"),
            Baskin.ActClamp => Loc.T("Kelepçele"),
            Baskin.ActBark => Loc.T("Havla"),
            Baskin.ActFetch => Loc.T("Getir"),
            Baskin.ActSniff => Loc.T("Kokla"),
            Baskin.ActCommand => Loc.T("Komut ver"),
            _ => kind,
        };

        /// <summary>Tutsağın komutunun kısa etkisi, makine türüne göre (K13).</summary>
        public static string CommandDetail(string type) => type switch
        {
            "bakim" => Loc.T("vuruş · −2 ya da plaka"),
            "havan" => Loc.T("havan · −1"),
            "bomba" => Loc.T("patlar · çevresine −1"),
            "kilit" => Loc.T("kilit · sonraki planı niyetsiz"),
            "kalkan" => Loc.T("plaka ya da ateş · −1"),
            "cogalt" => Loc.T("bu makinenin komutu yok"),
            _ => Loc.T("ateş · −1"),
        };

        /// <summary>Kullanılabilirkenki ayrıntı: bedel ve değer (<see cref="Baskin.BState.Moves"/>'tan).</summary>
        public static string Detail(Baskin.BState B, Baskin.Unit u, string kind)
        {
            var m = B?.Moves;
            if (m == null) return string.Empty;
            switch (kind)
            {
                case Baskin.ActWire: case Baskin.ActCharge: return Loc.F("yetenek yerine · {0} hak", 1);
                case Baskin.ActPlank: return Loc.F("yetenek yerine · {0} hak", m.PlankCost);
                case Baskin.ActTurret: return Loc.F("yetenek yerine · hat {0} kare · {1} tur", m.TurretRange, Baskin.DeviceTurnsFor(B, u));
                case Baskin.ActWinch: return Loc.F("yetenek yerine · hat {0} kare · {1} tur", m.WinchRange, Baskin.DeviceTurnsFor(B, u));
                case Baskin.ActDecoy: return Loc.F("yetenek yerine · evre başına {0} kare · {1} tur", m.DecoyStep, Baskin.DeviceTurnsFor(B, u));
                case Baskin.ActClamp: return Loc.F("yetenek yerine · {0} tur ele geçirir", Baskin.ClampTurnsFor(B, u));
                case Baskin.ActBark: return Loc.F("erim {0} kare", m.BarkRange);
                case Baskin.ActFetch: return Loc.T("bitişikteki yükü kapar");
                case Baskin.ActSniff: return Loc.T("ileri geçitte · sıradaki pafta");
                case Baskin.ActCommand: return CommandDetail(u?.Type);
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Kullanılamayan eylemin nedeni (ilk tutan; Core neden döndürmez, sıra <see cref="Baskin.CanAction"/>'ınki): yetenek kullanıldı,
        /// hak kalmadı, kalasın bedeli; sonra türün hedefsizlik nedeni (tel ve kalasta boş zemin yok ile güvence ayrımı
        /// <see cref="FreeFloor"/>'dan, yalnız metin için).
        /// </summary>
        public static string Reason(Baskin.BState B, Baskin.Unit u, string kind)
        {
            if (B == null || u == null) return string.Empty;
            if (B.Phase == "player" && !Baskin.CanAct(B, u)) return Loc.T("yetenek kullanıldı");
            // Aşama 10: önlemin kestiği eylem (koku yayıcı yakında, çift cidarın hakkı)
            if (FacilityCounterText.Reason(B, u, kind) is string counter) return counter;
            var m = B.Moves;
            if (Baskin.GearOf(kind) != null && m != null)
            {
                if (u.GearLeft <= 0) return Loc.T("hak kalmadı");
                if (kind == Baskin.ActPlank && u.GearLeft < m.PlankCost) return Loc.F("kalas için {0} hak gerekir", m.PlankCost);
            }
            switch (kind)
            {
                case Baskin.ActWire: case Baskin.ActPlank:
                    return FreeNeighbors(B, u).Count == 0 ? Loc.T("bitişikte boş zemin yok") : Loc.T("engel ekibi çıkıştan koparır");
                case Baskin.ActTurret: case Baskin.ActDecoy: case Baskin.ActWinch: return Loc.T("bitişikte boş zemin yok");
                case Baskin.ActCharge: return Loc.T("bitişikte yıkılacak duvar yok");
                case Baskin.ActClamp: return Loc.T("bitişikte makine yok");
                case Baskin.ActFetch: return Loc.T("yük yanında değil");
                case Baskin.ActSniff:
                    return B.Sniffed != 0 ? Loc.T("bu oda koklandı") : B.Escape ? Loc.T("kaçışta koklanmaz") : Loc.T("ileri geçidin yanında değil");
                case Baskin.ActCommand: return u.Type == "cogalt" ? Loc.T("bu makinenin komutu yok") : Loc.T("vuracak hedef yok");
                default: return Loc.T("yetenek kullanıldı");
            }
        }

        /// <summary>
        /// Ekipman satırı (uzman, <see cref="Baskin.Unit.Gear"/> dolu; örnek tahtada ve ekipmansızda null): ad ekipmanın adı; ayrıntı kip
        /// kapalıyken kullanılabilir eylemlerin özeti ve kalan hak (örme setinde "tel 1 · kalas 2 · {n} hak"), kip açıkken kurulu eylem ve
        /// kalan hak, hiçbir eylem kullanılamıyorsa ilk eylemin nedeni. <paramref name="armedKind"/>: kurulu eylem (kip kapalıyken null).
        /// </summary>
        public static RaidCardRow GearRow(Baskin.BState B, Baskin.Unit u, string armedKind)
        {
            var options = GearOptions(B, u);
            if (options.Count == 0) return null;
            var usable = options.Where(o => o.Usable).ToList();
            string detail = armedKind != null ? Loc.F("{0} · {1} hak", Name(armedKind), Math.Max(0, u.GearLeft))
                : usable.Count == 0 ? options[0].Reason
                : GearLine(B, u, usable);
            return new RaidCardRow { Icon = "ic_bolt_s", Text = PlanningBoardData.EquipmentName(u.Gear), Detail = detail, Armed = armedKind != null, Used = usable.Count == 0 && armedKind == null };
        }

        /// <summary>Kip kapalıyken ekipman satırının ayrıntısı (kart satırı iki satıra sığsın diye "yetenek yerine" yazılmaz; ekipman
        /// satırının hepsi yeteneğin yerinedir): örme setinde iki eylemin bedeli ve kalan hak; öbürlerinde eylemin değeri ve kalan hak.</summary>
        public static string GearLine(Baskin.BState B, Baskin.Unit u, List<MoveOption> usable = null)
        {
            var m = B?.Moves;
            if (m == null || u == null) return string.Empty;
            int left = Math.Max(0, u.GearLeft);
            switch (u.Gear)
            {
                case FacilityGear.Wire: return Loc.F("tel {0} · kalas {1} · {2} hak", 1, m.PlankCost, left);
                case FacilityGear.Charge: return Loc.F("{0} · {1} hak", Loc.T("bitişik duvarı yıkar"), left);
                case FacilityGear.Turret: return Loc.F("{0} · {1} hak", Loc.F("hat {0} kare · {1} tur", m.TurretRange, Baskin.DeviceTurnsFor(B, u)), left);
                case FacilityGear.Winch: return Loc.F("{0} · {1} hak", Loc.F("hat {0} kare · {1} tur", m.WinchRange, Baskin.DeviceTurnsFor(B, u)), left);
                case FacilityGear.Decoy: return Loc.F("{0} · {1} hak", Loc.F("evre başına {0} kare · {1} tur", m.DecoyStep, Baskin.DeviceTurnsFor(B, u)), left);
                case FacilityGear.Clamp: return Loc.F("{0} · {1} hak", Loc.F("{0} tur ele geçirir", Baskin.ClampTurnsFor(B, u)), left);
                default: return string.Empty;
            }
        }

        /// <summary>Kurulu eylemin kart alt düğmeleri: iki etki ve tonları ("good" yeşil, "bad" kırmızı, "plain").</summary>
        public static (string[] Texts, string[] Tones) Effects(Baskin.BState B, Baskin.Unit u, string kind)
        {
            var m = B?.Moves;
            if (m == null) return (Array.Empty<string>(), Array.Empty<string>());
            switch (kind)
            {
                case Baskin.ActWire: return (new[] { Loc.T("Yürüyüşü keser"), Loc.T("atışı geçirir") }, new[] { "good", "plain" });
                case Baskin.ActPlank: return (new[] { Loc.T("Yürüyüşü ve atışı keser"), Loc.F("Can {0}", m.PlankHp) }, new[] { "good", "plain" });
                case Baskin.ActCharge: return (new[] { Loc.T("Duvarı yık"), Loc.T("Moloz · −1") }, new[] { "good", "bad" });
                case Baskin.ActTurret: return (new[] { Loc.T("Hattaki ilk makine · −1"), Loc.F("{0} tur", Baskin.DeviceTurnsFor(B, u)) }, new[] { "bad", "plain" });
                case Baskin.ActDecoy: return (new[] { Loc.T("Makineler ona yönelir"), Loc.F("{0} tur", Baskin.DeviceTurnsFor(B, u)) }, new[] { "good", "plain" });
                case Baskin.ActWinch: return (new[] { Loc.F("Hattaki ilkini {0} kare çeker", m.WinchPull), Loc.F("{0} tur", Baskin.DeviceTurnsFor(B, u)) }, new[] { "good", "plain" });
                case Baskin.ActClamp: return (new[] { Loc.F("Makine {0} tur senin", Baskin.ClampTurnsFor(B, u)), Loc.T("kelepçe tükenir") }, new[] { "good", "plain" });
                case Baskin.ActBark: return (new[] { Loc.T("Makineler köpeğe yönelir"), Loc.T("sonraki planda") }, new[] { "good", "plain" });
                case Baskin.ActFetch: return (new[] { Loc.T("Köpek yükü taşır"), Loc.T("çıkışta yük çıkar") }, new[] { "good", "plain" });
                case Baskin.ActSniff: return (new[] { Loc.T("Sıradaki odanın planı çıkar"), Loc.T("robot göstermez") }, new[] { "good", "plain" });
                case Baskin.ActCommand: return (new[] { CommandDetail(u?.Type), Loc.T("bu tur") }, new[] { "bad", "plain" });
                default: return (Array.Empty<string>(), Array.Empty<string>());
            }
        }

        // ---------- Boş zemin (yalnız metin; kararı Core verir) ----------
        /// <summary>
        /// Engelin ve aygıtın konabileceği kare, Core'un private <c>FreeFloor</c> kuralının public parçalarla kopyası: tahtada, zemin
        /// (kablo değil), nesne, ayakta birim, yatan gövde ve yerdeki yük yok, kapı ve menfez karesi değil. Yalnız neden metninin "boş
        /// zemin yok" ile "güvence" ayrımı için; eylem yine <see cref="Baskin.ActionTargets"/>'a bağlıdır.
        /// </summary>
        public static bool FreeFloor(Baskin.BState B, int x, int y)
        {
            if (B == null || x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) return false;
            var c = Baskin.CellAt(B, x, y);
            if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || Baskin.UnitAt(B, x, y) != null) return false;
            if (B.CargoX == x && B.CargoY == y) return false;
            return !B.Units.Any(b => Baskin.LyingBody(b) && b.X == x && b.Y == y);
        }

        /// <summary>Uzmanın dört dik komşusundan boş zemin olanlar (<see cref="Baskin.Dirs"/> sırası).</summary>
        public static List<Baskin.Pt> FreeNeighbors(Baskin.BState B, Baskin.Unit u)
        {
            var o = new List<Baskin.Pt>();
            if (B == null || u == null) return o;
            foreach (var d in Baskin.Dirs) if (FreeFloor(B, u.X + d[0], u.Y + d[1])) o.Add(new Baskin.Pt(u.X + d[0], u.Y + d[1]));
            return o;
        }

        // ---------- Telsiz fişleri (sunum; kayda girmez) ----------
        /// <summary>
        /// Ekipman ya da birim eyleminin telsiz fişi (eylemden sonraki durumdan): <paramref name="actor"/> eylemi veren, (x, y) tıklanan
        /// hedef kare; <paramref name="nameOf"/> birim kimliğinden adı verir. Tanınmayan türde null.
        /// </summary>
        public static string Slip(Baskin.BState after, string kind, int actor, int x, int y, Func<int, string> nameOf = null)
        {
            if (after == null) return null;
            var a = ById(after, actor);
            string who = nameOf != null ? nameOf(actor) : FacilityMoveText.Who(a);
            string cell = Cell(after, x, y);
            switch (kind)
            {
                case Baskin.ActWire: return Loc.F("{0} · tel · {1}", who, cell);
                case Baskin.ActPlank: return Loc.F("{0} · kalas · {1}", who, cell);
                case Baskin.ActCharge: return Loc.F("{0} · lokum · {1}", who, cell);
                case Baskin.ActTurret: case Baskin.ActDecoy: case Baskin.ActWinch:
                    return Loc.F("{0} · {1} kuruldu · {2}", who, Lower(Baskin.UnitTypes[kind].Name), cell);
                case Baskin.ActClamp:
                    var e = Baskin.UnitAt(after, x, y);
                    return Loc.F("{0} · {1} ele geçirildi · {2}", who, e != null ? Lower(Baskin.UnitTypes[e.Type].Name) : string.Empty, cell);
                case Baskin.ActBark: return Loc.T("Köpek · havladı");
                case Baskin.ActFetch: return Loc.F("Köpek · yük alındı · {0}", cell);
                case Baskin.ActSniff: return Loc.F("Köpek · koklandı · PAFTA {0}", after.Sniffed);
                case Baskin.ActCommand: return Loc.F("Tutsak {0} · {1}", a != null ? Lower(Baskin.UnitTypes[a.Type].Name) : string.Empty, cell);
                default: return null;
            }
        }

        // ---------- Hover kartı ----------
        /// <summary>Uzman olmayan ekip biriminin hover başlığı: aygıtın adı, "Köpek", "Tutsak · {makine}"; öbürlerinde null.</summary>
        public static string UnitTitle(Baskin.Unit u)
        {
            if (Baskin.IsGadget(u)) return Baskin.UnitTypes[u.Type].Name;
            if (Baskin.IsDog(u)) return Loc.T("Köpek");
            if (Baskin.IsCaptive(u)) return Loc.F("Tutsak · {0}", Baskin.UnitTypes[u.Type].Name);
            return null;
        }

        /// <summary>Aygıtın ve köpeğin küçük resmi (sprite'lar parça 2'de üretilir); öbürlerinde null.</summary>
        public static string UnitThumb(Baskin.Unit u)
        {
            if (Baskin.IsDog(u)) return "thumb_dog";
            if (!Baskin.IsGadget(u)) return null;
            return u.Type == "taret" ? "thumb_turret" : u.Type == "yem" ? "thumb_decoy" : "thumb_winch";
        }

        /// <summary>Engelin ve koku yayıcının (aşama 10) küçük resmi (tel, kalas, hurda, yayıcı); öbürlerinde null.</summary>
        public static string ObjectThumb(string k) => k == "tel" ? "thumb_wire" : k == "kalas" ? "thumb_plank" : k == "hurda" ? "thumb_scrap"
            : k == "yayici" ? "thumb_emitter" : null;

        /// <summary>
        /// Birimin tesis hover satırları (aşama 9, §5.3): aygıtta kuran, yön, kalan çalışma, türün kuralı ve bağışıklık; köpekte sahibi,
        /// yürüyüş ve can, havlama, yük; tutsakta kelepçeleyen, kalan tur ve komut; kilitli makinede (Idle) "Sonraki planında niyetsiz", niyetsiz geçen makinede (Intent yok) "Bu planı niyetsiz geçiriyor".
        /// Uzmanda ve örnek tahtada boş.
        /// </summary>
        public static List<RaidCardRow> UnitRows(Baskin.BState B, Baskin.Unit u)
        {
            var o = new List<RaidCardRow>();
            if (B?.Room == null || u == null) return o;
            var m = B.Moves;
            if (Baskin.IsGadget(u))
            {
                string owner = OwnerName(B, u);
                if (!string.IsNullOrEmpty(owner)) o.Add(Row("ic_eye_s", Loc.F("Kuran: {0}", owner)));
                if (u.Dir >= 0) o.Add(Row("ic_eye_s", Loc.F("Yön: {0}", DirName(u.Dir))));
                o.Add(Row("ic_eye_s", Loc.F("Çalışır: {0} evre daha", Math.Max(0, u.Turns))));
                if (m != null)
                {
                    if (u.Type == "taret") o.Add(Row("ic_bolt_s", Loc.F("Hattaki ilk makine −1 · hat {0}", m.TurretRange), true));
                    else if (u.Type == "yem") o.Add(Row("ic_eye_s", Loc.T("Makineler onu ekip gibi hedef alır"), true));
                    else if (u.Type == "vinc") o.Add(Row("ic_eye_s", Loc.F("Hattaki ilk birimi {0} kare çeker", m.WinchPull), true));
                }
                o.Add(Row("ic_eye_s", Loc.T("EMP, kilit ve donma işlemez")));
            }
            else if (Baskin.IsDog(u))
            {
                string owner = OwnerName(B, u);
                if (!string.IsNullOrEmpty(owner)) o.Add(Row("ic_eye_s", Loc.F("Sahibi: {0}", owner)));
                o.Add(Row("ic_boots_s", Loc.F("Yürüyüş {0} · can {1}/{2}", FacilityMoveText.Range(u), u.Hp, u.Max)));
                if (u.Bark && m != null) o.Add(Row("ic_eye_s", Loc.F("Havlıyor: {0} kare içindeki makineler köpeğe yönelir", m.BarkRange), true));
                if (B.Carrier != 0 && B.Carrier == u.Id) o.Add(Row("ic_eye_s", Loc.T("Yükü taşıyor"), true));
            }
            else if (Baskin.IsCaptive(u))
            {
                string owner = OwnerName(B, u);
                if (!string.IsNullOrEmpty(owner)) o.Add(Row("ic_eye_s", Loc.F("Kelepçeleyen: {0}", owner)));
                o.Add(CaptiveLeft(B, u) <= 1 ? Row("ic_x_s", Loc.T("Son tur: tur sonunda serbest")) : Row("ic_eye_s", Loc.F("Kalan: {0} tur", CaptiveLeft(B, u))));
                o.Add(Row("ic_bolt_s", Loc.F("{0} · {1}", Name(Baskin.ActCommand), CommandDetail(u.Type)), true));
            }
            // Niyetsiz makine (Core): tutsağın kilidi Idle yazar, bu turun niyeti yine uygulanır, sonraki plan atlanır; bırakılan tutsak
            // ve kilitli makine planda Idle'ı bırakıp bu planı niyetsiz (Intent yok) geçirir
            else if (u.Side == 'e' && !u.Dead && u.Idle) o.Add(Row("ic_x_s", Loc.T("Sonraki planında niyetsiz"), true));
            else if (u.Side == 'e' && !u.Dead && u.Intent == null && B.Phase == "player") o.Add(Row("ic_x_s", Loc.T("Bu planı niyetsiz geçiriyor"), true));
            return o;
        }

        /// <summary>Engelin (tel, kalas, hurda) hover satırları: yürüyüş ve atış kuralı, ekibin kırabileceği; öbürlerinde boş.</summary>
        public static List<RaidCardRow> ObjectRows(Baskin.BState B, Baskin.Obj o)
        {
            var rows = new List<RaidCardRow>();
            if (B?.Room == null || o == null || !Baskin.IsBarrier(o.K)) return rows;
            rows.Add(Row("ic_eye_s", o.K == "tel" ? Loc.T("Yürüyüşü keser · atışı geçirir") : Loc.T("Yürüyüşü ve atışı keser")));
            rows.Add(Row("ic_bolt_s", Loc.T("Ekip kırabilir (sabotaj 2)")));
            return rows;
        }
    }
}
