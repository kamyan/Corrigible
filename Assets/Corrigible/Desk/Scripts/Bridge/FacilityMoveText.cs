using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Yetenek kartındaki bir tesis hamlesi seçeneği (aşama 7): tür, sürüklemede gövdenin kimliği, kullanılabilirlik (kararı Core
    /// verir: <see cref="Baskin.CanAction"/> ve hedef var), ad, kullanılabilirkenki ayrıntı ve kullanılamıyorsa nedeni.
    /// </summary>
    public sealed class MoveOption
    {
        public string Kind; public int Other; public bool Usable;
        public string Name, Detail, Reason;
        /// <summary>Kartın ayrıntı sütunu: kullanılabilirken bedel ve değer, değilse nedeni.</summary>
        public string Shown => Usable ? Detail : Reason;
    }

    /// <summary>
    /// Tesis hamlelerinin oyuncu metinleri (aşama 7; Docs/TESIS-BASKINLARI-TASARIMI.md §4, §11.5): yetenek kartının hamle satırları
    /// (hangi hamle bu odada anlamlı, adı, bedeli, kullanılamıyorsa neden), kurulu hamlenin iki etkisi, telsiz fişleri (oyuncu
    /// hamlesi, yürüyüşte yük alımı, GEÇ notu, makine evresinin pusu ve yük satırı) ve karenin hover satırları (gövde, yük, sandık
    /// nişi, kepenk, tutuş, pusu hattı). Saf ve durumsuz: tahtayı okur, değiştirmez. Değerler <see cref="Baskin.BState.Moves"/>'tan
    /// ve dengeden (kural 7). Örnek tahtada (Room ya da Moves null) seçenek ve satır üretmez.
    /// </summary>
    public static class FacilityMoveText
    {
        /// <summary>Ekip uzmanının yürüyüşü (Core'un WalkRange kuralı): kişinin kartındaki değer, yoksa türün.</summary>
        public static int Range(Baskin.Unit u) => u.MoveRange > 0 ? u.MoveRange : Baskin.UnitTypes[u.Type].Move;

        /// <summary>Kartta ve fişte kısa ad: kişinin adı, kişisiz kadroda rolün adı.</summary>
        public static string Who(Baskin.Unit u) => u == null ? string.Empty : !string.IsNullOrEmpty(u.PersonName) ? u.PersonName : Baskin.UnitTypes[u.Type].Name;

        /// <summary>
        /// Son nefes (Core: <see cref="Baskin.Unit.Breath"/>; aşama 6): oda biterken bu evrede düşen ve ayakta çıkan ekiple giden gövde
        /// (Core'un oda raporunda geçişte Body 1, tesisin girişinde 2). Yalnız oda biterken yazılır; örnek tahtada hep false.
        /// </summary>
        public static bool GoesWithTeam(Baskin.Unit u) => u != null && u.Breath && Baskin.LyingBody(u);

        /// <summary>Tahtada yatan ve ekiple gitmeyen gövde (<see cref="Baskin.LyingBody"/>, son nefes değil): taşınmazsa ölür.</summary>
        public static bool Lying(Baskin.Unit u) => Baskin.LyingBody(u) && !u.Breath;

        static string Cell(Baskin.BState B, int x, int y) => RaidBoard3D.CellName(B?.Room?.Index ?? 0, x, y);
        static Baskin.Unit ById(Baskin.BState B, int id) => B.Units.FirstOrDefault(v => v.Id == id);
        static DeskLang Lang => Loc.Lang == Loc.English ? DeskLang.En : DeskLang.Tr;

        /// <summary>Hamle satırının simgesi (var olan sprite'lar): pusu ve koşu yıldırım, sürükleme ve menfez bot, fırlatma ve iş göz.</summary>
        public static string Icon(string kind) => FacilityGearText.Handles(kind) ? FacilityGearText.Icon(kind) : kind == Baskin.ActAmbush || kind == Baskin.ActRush ? "ic_bolt_s"
            : kind == Baskin.ActDrag || kind == Baskin.ActVent ? "ic_boots_s" : "ic_eye_s";

        // ---------- Seçenekler ----------
        /// <summary>
        /// Uzmanın bu odada anlamlı hamleleri, <see cref="Baskin.ActionKinds"/> sırasıyla; sürüklemede yanındaki her gövde ayrı seçenek
        /// (<see cref="Baskin.DragBodies"/> sırası; yanında gövde yoksa tek, kullanılamaz satır). Örnek tahtada ve tahtada olmayan
        /// uzmanda boş. Aşama 9: uzman olmayan birimde (köpek, tutsak) yalnız kendi komutları (<see cref="FacilityGearText.UnitKinds"/>);
        /// aygıtta boş. Uzmanın ekipman eylemleri burada yok (ayrı satır ve R: <see cref="FacilityGearText.GearOptions"/>).
        /// </summary>
        public static List<MoveOption> Options(Baskin.BState B, Baskin.Unit u)
        {
            var o = new List<MoveOption>();
            if (B?.Room == null || B.Moves == null || u == null || u.Side != 'p' || !Baskin.OnBoard(u)) return o;
            if (!Baskin.IsSpecialist(u)) { foreach (var k in FacilityGearText.UnitKinds(B, u)) o.Add(Make(B, u, k, 0)); return o; }
            foreach (var kind in Baskin.ActionKinds)
            {
                if (!Relevant(B, u, kind)) continue;
                if (kind != Baskin.ActDrag) { o.Add(Make(B, u, kind, 0)); continue; }
                var bodies = Baskin.DragBodies(B, u);
                if (bodies.Count == 0) o.Add(Make(B, u, kind, 0));
                else foreach (int b in bodies) o.Add(Make(B, u, kind, b));
            }
            return o;
        }

        /// <summary>
        /// Hamle bu odada anlamlı mı (kartta satırı var mı): pusu ve koşu her zaman; sürükleme tahtada yatan gövde varken; fırlatma
        /// taşıyıcıda; iki kişilik iş kapalı kepenk ya da açılmamış sandık nişi varken; menfez Analist'te ve menfezli odada.
        /// </summary>
        public static bool Relevant(Baskin.BState B, Baskin.Unit u, string kind)
        {
            switch (kind)
            {
                case Baskin.ActAmbush: case Baskin.ActRush: return true;
                case Baskin.ActDrag: return B.Units.Any(Baskin.LyingBody);
                case Baskin.ActThrow: return B.Carrier != 0 && B.Carrier == u.Id;
                case Baskin.ActJoint: return JointWorks(B).Count > 0;
                case Baskin.ActVent: return u.Type == "ana" && HasVents(B);
                default: return false;
            }
        }

        internal static MoveOption Make(Baskin.BState B, Baskin.Unit u, string kind, int other)
        {
            var targets = Baskin.ActionTargets(B, u, kind, other);
            bool usable = Baskin.CanAction(B, u, kind) && targets.Count > 0;
            return new MoveOption { Kind = kind, Other = other, Usable = usable, Name = Name(B, u, kind, other), Detail = Detail(B, u, kind), Reason = usable ? null : Reason(B, u, kind, other) };
        }

        /// <summary>İki kişilik işin kareleri: kapalı kepenk ve açılmamış büyük sandık nişi (tahta sırasıyla).</summary>
        public static List<Baskin.Pt> JointWorks(Baskin.BState B)
        {
            var o = new List<Baskin.Pt>();
            if (B?.Room == null) return o;
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = Baskin.CellAt(B, x, y);
                    if (c.Shutter && c.T == "wall" || c.Crate && !B.CrateOpen) o.Add(new Baskin.Pt(x, y));
                }
            return o;
        }

        static bool HasVents(Baskin.BState B)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (Baskin.CellAt(B, x, y).Vent > 0 && Baskin.CellAt(B, x, y).T != "wall") return true;
            return false;
        }

        /// <summary>İki kişilik iş bu uzman için sandık mı: bitişik (sekiz komşu) iş karesinin türü; bitişik yoksa kepenk varsa kepenk.</summary>
        static bool JointIsCrate(Baskin.BState B, Baskin.Unit u)
        {
            var works = JointWorks(B);
            var near = works.Where(p => Math.Max(Math.Abs(p.X - u.X), Math.Abs(p.Y - u.Y)) == 1).ToList();
            if (near.Count > 0) return Baskin.CellAt(B, near[0].X, near[0].Y).Crate;
            return !works.Any(p => Baskin.CellAt(B, p.X, p.Y).Shutter);
        }

        /// <summary>Hamlenin satır adı (sürüklemede gövdenin adıyla).</summary>
        public static string Name(Baskin.BState B, Baskin.Unit u, string kind, int other = 0)
        {
            switch (kind)
            {
                case Baskin.ActAmbush: return Loc.T("Pusu");
                case Baskin.ActRush: return Loc.T("Koşu darbesi");
                case Baskin.ActDrag:
                    var body = other != 0 ? ById(B, other) : null;
                    return body != null ? Loc.F("Sürükle: {0}", Who(body)) : Loc.T("Sürükle");
                case Baskin.ActThrow: return Loc.T("Fırlat");
                case Baskin.ActJoint: return JointIsCrate(B, u) ? Loc.T("Büyük sandığı aç") : Loc.T("Kepengi kaldır");
                case Baskin.ActVent: return Loc.T("Menfez");
                default: return FacilityGearText.Name(kind);
            }
        }

        /// <summary>Kullanılabilirkenki ayrıntı: bedel ve değer (dengeden).</summary>
        public static string Detail(Baskin.BState B, Baskin.Unit u, string kind)
        {
            var m = B.Moves;
            switch (kind)
            {
                case Baskin.ActAmbush: return Loc.F("yetenek yerine · hat {0} kare", Baskin.AmbushRangeFor(B, u));
                case Baskin.ActRush: return Loc.F("yürüyüş + yetenek · {0}+ kare koş · {1} iter", m.RushRun, Baskin.RushPushFor(B, u));
                case Baskin.ActDrag: return Loc.F("yürüyüş + yetenek · menzil {0}", Math.Max(0, Range(u) - Baskin.DragCostFor(B, u)));
                case Baskin.ActThrow: return Loc.F("yetenek yerine · düz hat {0} kare", Baskin.ThrowRangeFor(B, u));
                case Baskin.ActJoint:
                    var holder = B.HoldId != 0 ? ById(B, B.HoldId) : null;
                    return holder != null ? Loc.F("yetenek yerine · {0} tutuyor", Who(holder)) : Loc.T("yetenek yerine · iki uzman aynı tur");
                case Baskin.ActVent: return Loc.T("yürüyüş · yetenek kalır");
                default: return FacilityGearText.Detail(B, u, kind);
            }
        }

        /// <summary>
        /// Kullanılamayan hamlenin nedeni (ilk tutan; yalnız açıklama, kararı Core verir): yeteneğin yerine olanlarda yetenek kullanıldı;
        /// yürüyüş isteyenlerde kilitli, donmuş, yetenek kullanıldı, yürüdü; sonra türün hedefsizlik nedeni.
        /// </summary>
        public static string Reason(Baskin.BState B, Baskin.Unit u, string kind, int other = 0)
        {
            // Aşama 9: ekipman ve birim eylemlerinin nedenleri ayrı (hak, boş zemin, güvence, köpek ve tutsak)
            if (FacilityGearText.Handles(kind)) return FacilityGearText.Reason(B, u, kind);
            if (Baskin.CostOf(kind) == Baskin.ActionCost.Ability)
            {
                if (u.Acted) return Loc.T("yetenek kullanıldı");
                return kind == Baskin.ActAmbush ? Loc.T("çevre kapalı") : kind == Baskin.ActThrow ? Loc.F("{0} kare içinde alıcı yok", Baskin.ThrowRangeFor(B, u)) : Loc.T("bitişikte iş yok");
            }
            if (u.Locked) return Loc.T("kilitli");
            if (u.Frozen) return Loc.T("donmuş");
            if (u.Acted) return Loc.T("yetenek kullanıldı");
            if (u.Moved) return Loc.T("yürüdü · yürüyüş ister");
            switch (kind)
            {
                // Aşama 10: hattın makinesi cıvatalıysa koşu darbesi onu hedef almaz (Core); neden "cıvatalı: itilmez"
                case Baskin.ActRush: return FacilityCounterText.Reason(B, u, kind) ?? Loc.T("hatta makine yok");
                case Baskin.ActDrag: return other == 0 || !Baskin.DragBodies(B, u).Contains(other) ? Loc.T("yanında yatan yok") : Loc.T("yürüyüş yetmez");
                default: return Loc.T("menfeze ulaşılamaz");
            }
        }

        /// <summary>Kurulu hamlenin kart alt düğmeleri: iki etki ve tonları ("good" yeşil, "bad" kırmızı, "plain").</summary>
        public static (string[] Texts, string[] Tones) Effects(Baskin.BState B, Baskin.Unit u, string kind)
        {
            switch (kind)
            {
                case Baskin.ActAmbush: return (new[] { Loc.T("Hatta duran ilk makine · −1"), Loc.T("makine evresinde") }, new[] { "bad", "plain" });
                // Aşama 12: Katar vagonunda itme kenardan düşürebilir (Core Push; önizleme satırı karede)
                case Baskin.ActRush: return (new[] { Loc.F("Makineyi {0} kare iter", Baskin.RushPushFor(B, u)), FacilityConditionText.IsKatar(B) ? FacilityConditionText.RushEdgeEffect : Loc.T("çarparsa −1") }, new[] { "bad", "plain" });
                case Baskin.ActDrag: return (new[] { Loc.T("Gövde yanında gelir"), Loc.T("bu tur yetenek yok") }, new[] { "good", "plain" });
                case Baskin.ActThrow: return (new[] { Loc.T("Yük ekip üyesine geçer"), Loc.F("düz hat {0} kare", Baskin.ThrowRangeFor(B, u)) }, new[] { "good", "plain" });
                case Baskin.ActJoint: return (new[] { JointIsCrate(B, u) ? Loc.T("Sandık açılır") : Loc.T("Kepenk kalkar"), Loc.T("iki uzman aynı tur") }, new[] { "good", "plain" });
                case Baskin.ActVent: return (new[] { Loc.T("Öbür uçtan çıkar"), Loc.T("yetenek kalır") }, new[] { "good", "plain" });
                default: return FacilityGearText.Effects(B, u, kind);
            }
        }

        // ---------- Telsiz fişleri (sunum; kayda girmez) ----------
        /// <summary>
        /// Oyuncu hamlesinin telsiz fişi (hamleden sonraki durumdan ve efektlerinden): <paramref name="actor"/> hamleyi veren,
        /// (x, y) tıklanan hedef kare, <paramref name="other"/> sürüklenen gövde; <paramref name="nameOf"/> birim kimliğinden adı verir
        /// (yoksa <see cref="Who"/>). Tanınmayan türde null.
        /// </summary>
        public static string Slip(Baskin.BState after, string kind, int actor, int x, int y, int other = 0, Func<int, string> nameOf = null)
        {
            if (after == null) return null;
            string N(int id) => nameOf != null ? nameOf(id) : Who(ById(after, id));
            string who = N(actor);
            switch (kind)
            {
                case Baskin.ActAmbush:
                    var owner = ById(after, actor);
                    var line = owner != null ? Baskin.AmbushLine(after, owner) : new List<Baskin.Pt>();
                    var first = line.Count > 0 ? line[0] : new Baskin.Pt(x, y);
                    var last = line.Count > 0 ? line[line.Count - 1] : new Baskin.Pt(x, y);
                    return Loc.F("{0} · pusu · {1}–{2}", who, Cell(after, first.X, first.Y), Cell(after, last.X, last.Y));
                case Baskin.ActRush: return Loc.F("{0} · koşu darbesi → {1}", who, Cell(after, x, y));
                case Baskin.ActDrag: return Loc.F("{0} · {1} sürükleniyor → {2}", who, N(other), Cell(after, x, y));
                case Baskin.ActThrow: return Loc.F("{0} · yük → {1}", who, N(after.Carrier));
                case Baskin.ActJoint:
                    bool crate = Baskin.CellAt(after, x, y).Crate;
                    if (after.FxList.Any(f => f.K == "open")) return crate ? Loc.F("büyük sandık açıldı · {0}", Cell(after, x, y)) : Loc.F("kepenk kalktı · {0}", Cell(after, x, y));
                    return crate ? Loc.F("{0} · büyük sandık tutuluyor", who) : Loc.F("{0} · kepenk tutuluyor", who);
                case Baskin.ActVent: return Loc.F("{0} · menfez → {1}", who, Cell(after, x, y));
                default: return FacilityGearText.Slip(after, kind, actor, x, y, nameOf);
            }
        }

        /// <summary>Yürüyüşte yerdeki yükü alan uzmanın fişi (kare önekli).</summary>
        public static string PickupSlip(Baskin.BState B, string who, int x, int y) => Loc.F("{0} · yük alındı · {1}", who, Cell(B, x, y));

        /// <summary>Herkes kapıdayken GEÇ'in fişi; tahtada yatan gövde varsa sayısıyla uyarır (GEÇ onaysızdır; dönüşte toplanabilir).</summary>
        public static string PassSlip(Baskin.BState B)
        {
            int down = B?.Room == null ? 0 : B.Units.Count(Lying);
            // Aşama 11: savunmada GEÇ "İÇERİ ÇEKİL"dir (onaylı; oda terk edilir)
            if (Baskin.IsDefense(B)) return down > 0 ? Loc.F("ekip kapıda · yerde {0} gövde · İÇERİ ÇEKİL'e bas", down) : Loc.T("ekip kapıda · İÇERİ ÇEKİL'e bas");
            // Aşama 12: Katar'ın dönüş geçidinde Teknisyen vagonu ayırabilir (AYIR, Q)
            if (down == 0 && FacilityConditionText.PassSlip(B) is string katar) return katar;
            return down > 0 ? Loc.F("ekip kapıda · yerde {0} gövde · GEÇ'e bas", down) : Loc.T("ekip kapıda · GEÇ'e bas");
        }

        /// <summary>Makine evresi efektinin tesis fişi: pusu vuruşu (makinenin adı ve yeni karesi) ve yükün yere düşmesi; öbürlerinde null.
        /// <paramref name="before"/>: evrenin sunum kopyası (makinenin türü için).</summary>
        public static string EffectSlip(Baskin.Fx fx, Baskin.BState before)
        {
            if (fx == null || before == null) return null;
            if (fx.K == "ambush-hit")
            {
                var m = ById(before, fx.Unit);
                string name = m != null ? DeskText.Lower(Baskin.UnitTypes[m.Type].Name, Lang) : string.Empty;
                return Loc.F("pusu · {0} · {1}", name, Cell(before, fx.X, fx.Y));
            }
            if (fx.K == "cargo-drop") return Loc.F("yük yere düştü · {0}", Cell(before, fx.X, fx.Y));
            return null;
        }

        // ---------- Hover kartı ----------
        /// <summary>Karede tahtada yatan gövde (kapıda ve çıkarılmış olan değil); yoksa null.</summary>
        public static Baskin.Unit BodyAt(Baskin.BState B, int x, int y) => B?.Room == null ? null : B.Units.FirstOrDefault(u => Baskin.LyingBody(u) && u.X == x && u.Y == y);

        /// <summary>Birimsiz ve nesnesiz karenin tesis başlığı: sandık nişi (açık ya da kapalı), kalkık kepenk; öbürlerinde null.</summary>
        public static string TileTitle(Baskin.BState B, int x, int y)
        {
            if (B?.Room == null) return null;
            var c = Baskin.CellAt(B, x, y);
            if (c.Crate) return B.CrateOpen ? Loc.T("Açılmış büyük sandık") : Loc.T("Büyük sandık");
            if (c.Shutter && c.T != "wall") return Loc.T("Kalkık kepenk");
            return null;
        }

        /// <summary>
        /// Karenin tesis hover satırları (sırayla): yatan gövde (birimsiz karede iki satır, ayakta birimin altındaysa tek; ekiple giden son
        /// nefes tek satır), yerdeki yük, kapalı sandık nişi, kapalı ve kalkık kepenk, tutulan iş, pusu hattı. Örnek tahtada boş.
        /// </summary>
        public static List<RaidCardRow> TileRows(Baskin.BState B, int x, int y, Balance rules)
        {
            var o = new List<RaidCardRow>();
            if (B?.Room == null) return o;
            var body = BodyAt(B, x, y);
            if (GoesWithTeam(body)) o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Son nefeste düştü: ekip onu da götürür"), Good = true });
            else if (body != null)
            {
                if (Baskin.UnitAt(B, x, y) == null)
                {
                    o.Add(new RaidCardRow { Icon = "ic_x_s", Text = Loc.T("Yerde: taşınmazsa ölür") });
                    o.Add(new RaidCardRow { Icon = "ic_boots_s", Text = Loc.T("Yanındaki ya da üstündeki uzman E ile sürükler") });
                }
                else o.Add(new RaidCardRow { Icon = "ic_x_s", Text = Loc.F("Altında: {0} (yerde)", Who(body)) });
            }
            if (B.CargoX == x && B.CargoY == y) o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Yük yerde: kareye giren ilk ekip üyesi alır"), Good = true });
            var c = Baskin.CellAt(B, x, y);
            var r = rules ?? new Balance();
            if (c.Crate && !B.CrateOpen)
            {
                o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("İki uzman aynı tur bitişikten açar (E)") });
                o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.F("Kazanılırsa +{0} metal +{1} devre", r.facilityBigCrateMetal, r.facilityBigCrateCircuits), Good = true });
            }
            if (c.Shutter && c.T == "wall") o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("İki uzman aynı tur bitişikten kaldırır (E)") });
            else if (c.Shutter) o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Açık kalır; makineler de geçer") });
            if (B.HoldId != 0 && B.HoldX == x && B.HoldY == y) o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.F("Tutuluyor: {0} · ikinci uzman bu tur", Who(ById(B, B.HoldId))) });
            foreach (var u in B.Units.Where(v => v.Side == 'p' && v.AmbushDir >= 0))
                if (Baskin.AmbushLine(B, u).Any(p => p.X == x && p.Y == y))
                {
                    o.Add(new RaidCardRow { Icon = "ic_bolt_s", Text = Loc.F("Pusu hattı · {0}: makine evresinde burada duran ilk makine −1", Who(u)), Good = true });
                    break;
                }
            // Aşama 12: Katar'ın açık kenarı (kuzey ve güney satırı), Yalpa'nın kayma satırları (Core okuyucuları)
            if (FacilityConditionText.OpenEdgeRow(B, x, y, rules) is RaidCardRow edge) o.Add(edge);
            o.AddRange(FacilityConditionText.TiltRows(B, x, y));
            return o;
        }
    }
}
