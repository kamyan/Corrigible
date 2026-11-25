using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis baskınının oyuncu metinleri (aşama 3; Docs/TESIS-BASKINLARI-TASARIMI.md §7.3, §11.5): oda açılışında gözcü raporu, oda
    /// kapanınca telsiz satırları (ayraç, düşenler, yürünen temiz odalar, yürüyerek çıkış), geçidin görev satırı ve amaç, tahtanın
    /// kâğıt etiketleri, hover satırları. Saf ve durumsuz: tahtayı ve raporları okur, değiştirmez; UnityEngine kullanmaz. Kareler
    /// pafta önekiyle ("2-C3"). Örnek tahtada (Room null) metin üretmez.
    /// </summary>
    public static class FacilityRaidText
    {
        /// <summary>Paftadaki kare adı, pafta önekiyle ("2-C3"); oda ≤ 0 öneksiz.</summary>
        public static string Cell(int room, int x, int y) => RaidBoard3D.CellName(room, x, y);

        /// <summary>Dönüş odası: kaçışta (hedeften sonra) batıya geri geçilen giriş ya da ara oda.</summary>
        public static bool IsReturnBoard(Baskin.BState B) => B?.Room != null && B.Escape && B.Room.Type != RoomType.Target;

        /// <summary>Core'un telsiz satırlarının sunum kopyası.</summary>
        public static List<RaidLogModel> Log(IEnumerable<Baskin.LogLine> lines) =>
            lines == null ? new List<RaidLogModel>() : lines.Select(l => new RaidLogModel { Text = l.Text, Kind = l.Kind }).ToList();

        static int RoomOf(Baskin.BState B) => B?.Room?.Index ?? 0;
        static DeskLang Lang => Loc.Lang == Loc.English ? DeskLang.En : DeskLang.Tr;
        static string Lower(string s) => DeskText.Lower(s, Lang);
        static string Cells(int room, IEnumerable<Baskin.Pt> tiles) => string.Join(Loc.T(" · "), tiles.Select(t => Cell(room, t.X, t.Y)));
        static RaidLogModel Line(string text, string kind) => new RaidLogModel { Text = text, Kind = kind };
        static IEnumerable<Baskin.Pt> Tiles(Baskin.BState B, System.Func<Baskin.Cell, bool> where)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (where(Baskin.CellAt(B, x, y))) yield return new Baskin.Pt(x, y);
        }
        static List<Baskin.Unit> Machines(Baskin.BState B) => B.Units.Where(u => u.Side == 'e' && !u.Dead && !u.Out).ToList();

        /// <summary>
        /// Gözcü raporu (oda açılışında; yalnız bulunan satırlar): başlık, makineler türüne göre (birim sırası), kopya dolapları,
        /// hedef (hedef odasına gidiş), sağlam donanım (gidiş), sivil sunucu sayısı, kapı kareleri, menfez çifti, ağır kepenk.
        /// Hazırlık satırları Core'un kendi açılış satırlarıdır, yinelenmez.
        /// </summary>
        public static List<RaidLogModel> Spotter(Baskin.BState B)
        {
            var o = new List<RaidLogModel>();
            if (B?.Room == null) return o;
            int room = RoomOf(B);
            bool back = IsReturnBoard(B);
            o.Add(Line(back ? Loc.F("— DÖNÜŞ · PAFTA {0}/3 · {1} —", room, B.Room.Name) : Loc.F("— PAFTA {0}/3 · {1} —", room, B.Room.Name), "turn"));
            var foes = Machines(B);
            foreach (var g in foes.GroupBy(u => u.Type))
                o.Add(Line(Loc.F("Gözcü · {0}: {1}", Lower(Baskin.UnitTypes[g.Key].Name), Cells(room, g.Select(u => new Baskin.Pt(u.X, u.Y)))), "bad"));
            if (foes.Count == 0) o.Add(Line(Loc.T("Gözcü · ayakta makine yok."), "sys"));
            var spawners = Tiles(B, c => c.O?.K == "spawner").ToList();
            if (spawners.Count > 0) o.Add(Line(Loc.F("Gözcü · kopya dolabı: {0}", Cells(room, spawners)), "bad"));
            if (B.Room.Type == RoomType.Target && !B.Escape)
            {
                bool loot = Baskin.IsLoot(B);
                foreach (var g in Baskin.TargetsLeft(B).GroupBy(t => t.O.K))
                {
                    string name = loot ? Loc.T("İkmal kasası") : Loc.T(Baskin.ObjNames[g.Key]);
                    o.Add(Line(Loc.F("Gözcü · {0}: {1}", Lower(name), Cells(room, g.Select(t => new Baskin.Pt(t.X, t.Y)))), "sys"));
                }
            }
            if (!back && B.Room.Device != null && B.Room.DeviceAt is Baskin.Pt d && Baskin.CellAt(B, d.X, d.Y).O?.K == B.Room.Device)
                o.Add(Line(Loc.F("Gözcü · {0}: {1} · yan hedef", Lower(Loc.T(Baskin.ObjNames[B.Room.Device])), Cell(room, d.X, d.Y)), "good"));
            int civilians = Tiles(B, c => c.O?.K == "civ").Count();
            if (civilians > 0) o.Add(Line(Loc.F("Gözcü · sivil sunucu: {0}", civilians), "sys"));
            var gates = Baskin.GateTiles(B);
            if (gates.Count > 0)
            {
                string cells = Cells(room, gates);
                o.Add(Line(Baskin.IsFinalExit(B) ? Loc.F("Gözcü · tesisin girişi: {0}", cells) : B.Escape ? Loc.F("Gözcü · giriş geçidi: {0}", cells)
                    : Loc.F("Gözcü · ileri geçit: {0}", cells), "sys"));
            }
            var vents = B.Room.Vents;
            if (vents.Count == 2 && vents.All(v => Baskin.CellAt(B, v.X, v.Y).Vent > 0 && Baskin.CellAt(B, v.X, v.Y).T != "wall"))
                o.Add(Line(Loc.F("Gözcü · menfez: {0} ↔ {1}", Cell(room, vents[0].X, vents[0].Y), Cell(room, vents[1].X, vents[1].Y)), "sys"));
            if (B.Room.Shutter is Baskin.Pt s && Baskin.CellAt(B, s.X, s.Y).T == "wall")
                o.Add(Line(Loc.F("Gözcü · ağır kepenk: {0}", Cell(room, s.X, s.Y)), "sys"));
            // Aşama 7: kapalı büyük sandık nişi (iki uzman açar) ve odaya taşınan ya da yatan gövdeler (taşınmazsa ölür)
            foreach (var niche in Tiles(B, cell => cell.Crate))
                if (!B.CrateOpen) o.Add(Line(Loc.F("Gözcü · büyük sandık: {0} · iki uzman", Cell(room, niche.X, niche.Y)), "good"));
            foreach (var body in B.Units.Where(FacilityMoveText.Lying))
                o.Add(Line(Loc.F("Gözcü · yerde: {0} · {1}", FacilityMoveText.Who(body), Cell(room, body.X, body.Y)), "bad"));
            // Aşama 10: YZ'nin önlemleri (çift cidar, koku yayıcı, cıvatalı makine; yalnız bulunanlar)
            o.AddRange(FacilityCounterText.Spotter(B));
            // Aşama 12: Yalpa odasının yatış takvimi (takip satırı önceki raporu bilen veri kaynağında: FacilityConditionText.SpotterFollow)
            o.AddRange(FacilityConditionText.SpotterTilt(B));
            return o;
        }

        /// <summary>Oda açılışında telsiz fişindeki kısa özet: pafta ve ayaktaki makine sayısı; örnek tahtada null.</summary>
        public static string SpotterSlip(Baskin.BState B)
        {
            if (B?.Room == null) return null;
            int n = Machines(B).Count;
            return IsReturnBoard(B) ? Loc.F("dönüş · PAFTA {0}/3 · {1} makine", RoomOf(B), n) : Loc.F("gözcü · PAFTA {0}/3 · {1} makine", RoomOf(B), n);
        }

        /// <summary>
        /// Oda kapanınca telsiz satırları: ayraç (pafta, tur), bu ziyarette düşen ya da çıkamayan kişiler (önekli kare), yürünerek
        /// geçilen temiz odalar, yürüyerek çıkış. <paramref name="previous"/>: bu rapordan önceki rapor (verilirse orada zaten düşmüş
        /// kişi yeniden yazılmaz; ara odada düşen dönüşte aynı odanın raporunda da "d" ve aynı Fell ile taşınır).
        /// </summary>
        /// <para>Aşama 7: bugünkü satırlardan sonra gövdeler (tesisten çıkarılan; ekiple taşınan, önceki raporda taşınmıyorduysa), bu
        /// ziyarette kalkan kepenkler ve açılan büyük sandık (<paramref name="crate"/>: odanın sandık nişi; yoksa karesiz satır).</para>
        /// <para>Aşama 12: kenardan düşen (<c>"k"</c>) "Vagondan düştü: … · kare · {n} vardiya" (<paramref name="rules"/>: yatışın dengesi; verilmezse
        /// C# varsayılanı); AYIR ve takip satırı (<see cref="FacilityConditionText.ClosedChain"/>).</para>
        public static List<RaidLogModel> Closed(RaidParams p, RoomReport r, IReadOnlyList<int> skipped, RoomReport previous = null, Baskin.Pt? crate = null, Balance rules = null)
        {
            var o = new List<RaidLogModel>();
            if (r == null) return o;
            o.Add(Line(r.Return ? Loc.F("— DÖNÜŞ · PAFTA {0}/3 kapandı · {1} tur —", r.Room, r.Turns) : Loc.F("— PAFTA {0}/3 kapandı · {1} tur —", r.Room, r.Turns), "turn"));
            for (int i = 0; i < r.Team.Count; i++)
            {
                var m = r.Team[i];
                var was = previous != null && i < previous.Team.Count ? previous.Team[i] : null;
                // Süre dolarken kapıya varamadı, ekip onsuz geçti (yalnız geri çekildiği odanın raporunda)
                if (m.State == "w") { if (was == null || was.State == "a") o.Add(Line(Loc.F("{0} geri çekildi · {1}", PersonName(p, i), Cell(r.Room, m.X, m.Y)), "bad")); continue; }
                if (m.State == "a" || m.Fell != r.Room) continue;
                if (was != null && was.State != "a") continue;
                string at = Cell(r.Room, m.X, m.Y);
                if (m.State == "k") { o.Add(Line(FacilityConditionText.ClosedOverboard(PersonName(p, i), at, rules ?? new Balance()), "bad")); continue; }
                o.Add(Line(m.State == "l" ? Loc.F("{0} çıkamadı · {1}", PersonName(p, i), at) : Loc.F("{0} düştü · {1}", PersonName(p, i), at), "bad"));
            }
            o.AddRange(FacilityConditionText.ClosedChain(r));
            if (skipped != null) foreach (int k in skipped) o.Add(Line(Loc.F("Gözcü · PAFTA {0}/3 temiz: ekip yürüyerek geçti.", k), "good"));
            if (r.Last && r.End == "pass") o.Add(Line(Loc.T("Gözcü · ekip tesisten yürüyerek çıktı."), "good"));
            for (int i = 0; i < r.Team.Count; i++)
            {
                var m = r.Team[i];
                var was = previous != null && i < previous.Team.Count ? previous.Team[i] : null;
                if (m.Body == 2 && was?.Body != 2) o.Add(Line(Loc.F("{0} tesisten çıkarıldı", PersonName(p, i)), "good"));
                else if (m.Body == 1 && was?.Body != 1) o.Add(Line(Loc.F("{0} ekiple taşınıyor", PersonName(p, i)), "sys"));
            }
            if (r.Opened != null) foreach (int k in r.Opened) o.Add(Line(Loc.F("Kepenk kalktı · {0}", Cell(r.Room, k % Baskin.N, k / Baskin.N)), "good"));
            if (r.Crate) o.Add(Line(crate is Baskin.Pt c ? Loc.F("Büyük sandık açıldı · {0}", Cell(r.Room, c.X, c.Y)) : Loc.T("Büyük sandık açıldı."), "good"));
            // Aşama 9: geçişte ekiple giden köpek ve tutsaklar (kalan oyuncu turuyla), köpeğin kokladığı oda. Ekipmansız raporda yok
            if (r.End == "pass" && !r.Last && r.Units != null)
                foreach (var u in r.Units.Where(u => u.State == "a"))
                {
                    if (u.Class == Baskin.ClassDog) o.Add(Line(Loc.T("Köpek ekiple geçer"), "sys"));
                    else if (u.Class == Baskin.ClassCaptive && u.Type != null && Baskin.UnitTypes.ContainsKey(u.Type))
                        o.Add(Line(Loc.F("Tutsak {0} ekiple geçer · {1} tur", Lower(Baskin.UnitTypes[u.Type].Name), u.Turns + 1), "sys"));
                }
            if (r.Sniff > 0) o.Add(Line(Loc.F("Koklandı: PAFTA {0} planı çıktı", r.Sniff), "good"));
            return o;
        }

        /// <summary>Kayıttan devamda önceki odaların telsiz satırları: her rapor için kapanış ayracı ve düşenler (atlanan odalar yazılmaz).</summary>
        public static List<RaidLogModel> History(RaidParams p, IReadOnlyList<RoomReport> filed, Balance rules = null)
        {
            var o = new List<RaidLogModel>();
            if (filed == null) return o;
            for (int i = 0; i < filed.Count; i++) o.AddRange(Closed(p, filed[i], null, i > 0 ? filed[i - 1] : null, null, rules));
            return o;
        }

        /// <summary>Telsiz fişi: yürüyerek çıkış ya da yürünerek geçilen ilk temiz oda; ikisi de yoksa null.</summary>
        public static string WalkedSlip(IReadOnlyList<int> skipped, bool walkOut)
        {
            if (walkOut) return Loc.T("gözcü · ekip tesisten çıktı");
            return skipped != null && skipped.Count > 0 ? Loc.F("gözcü · PAFTA {0}/3 temiz, geçildi", skipped[0]) : null;
        }

        /// <summary>Kapı karelerinin adı: tesisin girişi (giriş odasının dönüşü), batı geçidi (kaçış), ileri geçit.</summary>
        public static string GateTitle(Baskin.BState B) =>
            Baskin.IsFinalExit(B) ? Loc.T("Tesisin girişi") : B.Escape ? Loc.T("Giriş geçidi") : Loc.T("İleri geçit");

        /// <summary>
        /// Görev çipinin geçit satırı (can taşımaz, Max 0): ad ve kareler ("1-H4/H5": ilki önekli). Örnek tahtada, hedef odasına
        /// gidişte ve kapı karesi yokken null.
        /// </summary>
        public static RaidTargetModel GateRow(Baskin.BState B)
        {
            if (B?.Room == null || B.Room.Type == RoomType.Target && !B.Escape) return null;
            var gates = Baskin.GateTiles(B);
            if (gates.Count == 0) return null;
            int room = RoomOf(B);
            string cell = Cell(room, gates[0].X, gates[0].Y) + string.Concat(gates.Skip(1).Select(g => "/" + RaidBoard3D.CellName(g.X, g.Y)));
            return new RaidTargetModel { Name = GateTitle(B), Thumb = "ic_arrow_s", Hp = 0, Max = 0, Armor = false, Broken = false, Cell = cell };
        }

        /// <summary>
        /// Görev çipinin amaç satırı: giriş ve ara odaya gidişte geçide götür; hedeften kaçışta ve ara odanın dönüşünde batı geçidi
        /// (taşıyıcı tahtadaysa malzemeyle). Örnek tahtada, hedef odasına gidişte ve tesisin girişinde null (bugünkü amaç).
        /// </summary>
        public static string Goal(Baskin.BState B, bool carrierOnBoard)
        {
            if (B?.Room == null) return null;
            // Aşama 7: yük yerdeyse (taşıyıcı düştü) amaç onu almaktır; tesisin girişi dahil
            if (B.CargoX >= 0) return Loc.F("Amaç: yükü yerden al ({0})", Cell(RoomOf(B), B.CargoX, B.CargoY));
            if (Baskin.IsFinalExit(B)) return null;
            if (!B.Escape) return B.Room.Type == RoomType.Target ? null : Loc.T("Amaç: ekibi geçide götür, sonra GEÇ");
            return carrierOnBoard ? Loc.T("Amaç: malzemeyle giriş geçidinden geç") : Loc.T("Amaç: ekibi giriş geçidinden geçir");
        }

        static (int atGate, int standing) GateCount(Baskin.BState B)
        {
            // Aşama 9: KAPIDA paydası uzmanlar (Core GEÇ'i uzmanla açar; köpek, aygıt ve tutsak sayılmaz)
            var team = B.Units.Where(u => Baskin.IsSpecialist(u) && !u.Dead && !u.Withdrew).ToList();
            return (team.Count(u => u.AtGate), team.Count);
        }

        /// <summary>
        /// Tahtanın kâğıt etiketleri (kare anahtarı y*8+x): ilk kapı karesinde GEÇİT ya da "KAPIDA n/m" (tesisin girişinde yok), menfez
        /// karelerinde MENFEZ, kepenkte (hâlâ duvarsa) KEPENK; aşama 7: tutulan iş karesinde TUTULUYOR, kapalı sandık nişinde SANDIK,
        /// yerdeki yükte YÜK. Örnek tahtada boş.
        /// </summary>
        public static List<(int key, string text)> BoardNotes(Baskin.BState B)
        {
            var o = new List<(int key, string text)>();
            if (B?.Room == null) return o;
            var gates = Baskin.GateTiles(B);
            if (gates.Count > 0 && !Baskin.IsFinalExit(B))
            {
                var (atGate, standing) = GateCount(B);
                o.Add((gates[0].Y * Baskin.N + gates[0].X, atGate > 0 ? Loc.F("KAPIDA {0}/{1}", atGate, standing) : Loc.T("GEÇİT")));
            }
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = Baskin.CellAt(B, x, y);
                    if (c.Vent > 0 && c.T != "wall") o.Add((y * Baskin.N + x, Loc.T("MENFEZ")));
                    else if (B.HoldId != 0 && B.HoldX == x && B.HoldY == y) o.Add((y * Baskin.N + x, Loc.T("TUTULUYOR")));
                    else if (c.Shutter && c.T == "wall") o.Add((y * Baskin.N + x, Loc.T("KEPENK")));
                    else if (c.Crate && !B.CrateOpen) o.Add((y * Baskin.N + x, Loc.T("SANDIK")));
                }
            if (B.CargoX >= 0) o.Add((B.CargoY * Baskin.N + B.CargoX, Loc.T("YÜK")));
            return o;
        }

        /// <summary>Kapı karesinin hover satırları (en çok 4); tesisin girişinde ve kapı karesi değilse boş. Yerde kalan gövdeler ve yük
        /// satırı en sonda; yer açmak için önce "geri çıkılmaz", sonra ilk açıklama satırı düşer.</summary>
        public static List<RaidCardRow> GateRows(Baskin.BState B, Baskin.Unit selected, int x, int y)
        {
            var o = new List<RaidCardRow>();
            if (B?.Room == null || Baskin.IsFinalExit(B) || !Baskin.IsGateTile(B, x, y)) return o;
            var (atGate, standing) = GateCount(B);
            // Aşama 11: savunmada kapı sığınak değildir (K17): kapıdaki uzman tur sonunda tahtaya döner; herkes gelince İÇERİ ÇEKİL (oda terk edilir)
            bool defense = Baskin.IsDefense(B);
            o.Add(defense ? new RaidCardRow { Icon = "ic_x_s", Text = Loc.T("Savunmada kapı sığınak değil: kapıdaki uzman tur sonunda tahtaya döner.") }
                : new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Buraya yürüyen uzman kapıda bekler: tahtadan kalkar, hedef alınmaz."), Good = true });
            o.Add(new RaidCardRow { Icon = "ic_boots_s", Text = defense ? Loc.F("Kapıda: {0}/{1} · herkes gelince İÇERİ ÇEKİL (Enter): oda terk edilir", atGate, standing)
                : Loc.F("Kapıda: {0}/{1} · herkes gelince GEÇ (Enter)", atGate, standing) });
            o.Add(new RaidCardRow { Icon = "ic_x_s", Text = Loc.T("Kapıdan geri çıkılmaz; yalnız bu turun hamlesi geri alınır (Z).") });
            if (selected != null && Baskin.OnBoard(selected) && selected.X == x && selected.Y == y)
                o.Add(new RaidCardRow { Icon = "ic_boots_s", Text = Loc.T("Uzmana yeniden tıkla: adım harcamadan kapıya girer."), Good = true });
            // Aşama 7: GEÇ onaysızdır; yerde kalan gövdeler ve yerdeki yük kapının kartında (en çok 4 satır: yer açılır)
            var extra = new List<RaidCardRow>();
            var down = B.Units.Where(FacilityMoveText.Lying).Select(FacilityMoveText.Who).ToList();
            if (down.Count > 0) extra.Add(new RaidCardRow { Icon = "ic_x_s", Text = Loc.F("Yerde kalan: {0}", string.Join(Loc.T(" · "), down)) });
            if (CargoLeftRow(B) is RaidCardRow cargo) extra.Add(cargo);
            foreach (int drop in new[] { 2, 0 }) if (o.Count + extra.Count > 4) o.RemoveAt(drop);
            o.AddRange(extra);
            return o;
        }

        /// <summary>Çıkış ve kapı karesinin yük satırı: yük yerdeyse (taşıyıcı düştü, kimse almadı) buradan çıkan ekip onu bırakır
        /// (oda raporunda yük kaybı). Örnek tahtada ve yük yerde değilken null.</summary>
        public static RaidCardRow CargoLeftRow(Baskin.BState B) =>
            B?.Room == null || B.CargoX < 0 ? null : new RaidCardRow { Icon = "ic_x_s", Text = Loc.F("Yük yerde kalır · {0}", Cell(RoomOf(B), B.CargoX, B.CargoY)) };

        /// <summary>Menfez karesinin öbür ucu (aynı menfez numaralı öbür kare), önekli; yoksa null.</summary>
        public static string VentOther(Baskin.BState B, int x, int y)
        {
            if (B?.Room == null) return null;
            int vent = Baskin.CellAt(B, x, y).Vent;
            if (vent <= 0) return null;
            for (int oy = 0; oy < Baskin.N; oy++)
                for (int ox = 0; ox < Baskin.N; ox++)
                    if ((ox != x || oy != y) && Baskin.CellAt(B, ox, oy).Vent == vent) return Cell(RoomOf(B), ox, oy);
            return null;
        }

        /// <summary>Donanım kırılınca (vana: kapatılınca) hedef odasında ne olur (dengeden); denge yoksa ya da tür donanım değilse null.</summary>
        public static string DeviceEffect(string kind, Balance b, bool loot)
        {
            if (b == null) return null;
            switch (kind)
            {
                case "pano": return Loc.F("Kırılırsa hedef odada kopya dolapları ilk {0} tur makine uyandırmaz.", b.facilityPanelQuietTurns);
                case "anten": return Loc.F("Kırılırsa hedef odaya {0} makine eksik gelir; dönüşte uyanma olmaz.", b.facilityAntennaMachines);
                case "vana":
                    return loot ? Loc.F("Yalnız Teknisyen kapatır: hedef odada makineler ilk {0} tur yürüyemez.", b.facilityValveStillTurns)
                        : Loc.F("Yalnız Teknisyen kapatır: hedef dolap {0} can eksik başlar.", b.facilityValveTargetHp);
                default: return null;
            }
        }

        /// <summary>Kadronun <paramref name="slot"/>. kişisinin adı; kişisiz kadroda rolün adı (kadro verilmezse Teknisyen, EMP uzmanı,
        /// Analist).</summary>
        public static string PersonName(RaidParams p, int slot)
        {
            var squad = p?.Squad;
            if (squad != null && squad.Count > 0)
            {
                if (slot < 0 || slot >= squad.Count) return null;
                var s = squad[slot];
                if (!string.IsNullOrEmpty(s.PersonName)) return s.PersonName;
                return Baskin.UnitTypes.TryGetValue(s.Type ?? "", out var t) ? t.Name : s.Type;
            }
            var roles = new[] { "tek", "emp", "ana" };
            return slot >= 0 && slot < roles.Length ? Baskin.UnitTypes[roles[slot]].Name : null;
        }
    }
}
