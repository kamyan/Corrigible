using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis baskınının oda içi zincir kuralları (Docs/TESIS-BASKINLARI-TASARIMI.md §3.2–3.5): kapı kareleri ve kapıda bekleme,
    /// GEÇ (<see cref="Pass"/>, evre "pass"), kademe eki ve dönüş odasının tahtası (<see cref="ReturnBoard"/>). Sıradaki oda, rapor
    /// ve sonuç zincir katmanınındır (RaidChain); motor yalnız tek odanın tahtasını bilir. Örnek tahtada (Room null) hiçbiri çalışmaz.
    /// </summary>
    public static partial class Baskin
    {
        /// <summary>Olay nesneleri (şartın parçası ya da ürünü): oda raporuna yazılmaz, dönüşte plandan kurulur.</summary>
        public static bool IsEventObject(string k) => k == "tup" || k == "kol" || k == "enkaz";

        /// <summary>Kapı kareleri: kaçıştan önce ileri geçit (G, A; H'de boş), kaçışta batı girişi. Örnek tahtada boş.</summary>
        public static IReadOnlyList<Pt> GateTiles(BState B) =>
            B.Room == null ? (IReadOnlyList<Pt>)Array.Empty<Pt>() : B.Escape ? B.Room.West : B.Room.Forward;

        /// <summary>Tesisin girişi: kaçıştaki giriş odasının (G dönüşü) batı kareleri tahliyedir (Out); GEÇ yoktur.</summary>
        public static bool IsFinalExit(BState B) => B.Room != null && B.Escape && B.Room.Type == RoomType.Entry;

        public static bool IsGateTile(BState B, int x, int y) => GateTiles(B).Any(t => t.X == x && t.Y == y);

        /// <summary>Uzman kapıya girer: kapıda bekler (tahtadan kalkar); tesisin girişinde tahliye edilir.</summary>
        static void GateIn(BState B, Unit u)
        {
            if (IsFinalExit(B)) { Evacuate(B, u); return; }
            u.AtGate = true;
            AddFx(B, new Fx { K = "gate", X = u.X, Y = u.Y, Unit = u.Id });
            Say(B, Loc.F("{0} kapıda bekliyor.", UnitTypes[u.Type].Name), "good");
        }

        /// <summary>
        /// Kapı karesinde (itilerek, çekilerek, kayarak ya da kurulumda) duran uzman kendi turunda adım harcamadan kapıya girer
        /// (tesisin girişinde tahliye olur). Moved/Acted'e bakmaz, değiştirmez. Geri alınır.
        /// </summary>
        public static bool EnterGate(BState B, int uid)
        {
            var u = ById(B, uid);
            if (u == null || B.Phase != "player" || !IsSpecialist(u) || !OnBoard(u) || !IsGateTile(B, u.X, u.Y)) return false;
            Snapshot(B);
            GateIn(B, u);
            CheckEscape(B);
            return true;
        }

        /// <summary>Ayaktaki bütün uzmanlar kapıda: GEÇ verilebilir (tesisin girişi hariç; orada tahliye vardır). Köpek ve tutsak
        /// nerede olursa olsun ekiple geçer, aygıt odada kalır (aşama 8): yalnız uzmanlar sayılır. Aşama 11: savunmada son turda içeri
        /// çekilinmez (K17).</summary>
        public static bool CanPass(BState B)
        {
            if (B.Phase != "player" || B.Room == null || IsFinalExit(B) || GateTiles(B).Count == 0) return false;
            if (B.Defense && B.Turn >= B.MaxTurns) return false;
            bool any = false;
            foreach (var u in B.Units)
            {
                if (!IsSpecialist(u) || u.Dead || u.Withdrew) continue;
                if (!u.AtGate) return false;
                any = true;
            }
            return any;
        }

        /// <summary>GEÇ: oda biter (evre "pass"), geri alınamaz (yeni odanın makineleri bedava keşif olurdu).</summary>
        public static bool Pass(BState B)
        {
            if (!CanPass(B)) return false;
            B.FxList.Clear();
            PassRoom(B);
            return true;
        }

        /// <summary>
        /// Tesis aşama 12 (Katar): AYIR verilemiyorsa nedeni, verilebiliyorsa null. "rule": Katar odasının dönüşü (kaçış) değil, oyuncu evresi
        /// değil, savunma ya da tesisin girişi; "tek": ayakta Teknisyen yok (düştü ya da kenardan düştü); "gate": ayaktaki bütün uzmanlar
        /// kapıda değil; "acted": Teknisyen bu tur yeteneğini kullandı. Saf (bot ve sunum nedeni buradan okur).
        /// </summary>
        public static string DecoupleBlock(BState B)
        {
            if (B?.Room == null || B.Phase != "player" || !B.Escape || !Is(B, "katar") || B.Defense || IsFinalExit(B)) return "rule";
            var techs = B.Units.Where(u => IsSpecialist(u) && u.Type == "tek" && !u.Dead).ToList();
            if (techs.Count == 0) return "tek";
            if (!CanPass(B) || !techs.Any(u => u.AtGate)) return "gate";
            if (!techs.Any(u => u.AtGate && !u.Acted)) return "acted";
            return null;
        }

        /// <summary>AYIR verilebilir mi: GEÇ verilebilir, Katar'ın dönüşü, Teknisyen kapıda ve bu tur yeteneğini kullanmadı (<see cref="DecoupleBlock"/>).</summary>
        public static bool CanDecouple(BState B) => CanPass(B) && DecoupleBlock(B) == null;

        /// <summary>
        /// AYIR (GEÇ'in seçeneği; aşama 12, belge §10.1): vagon ayrılır, takip olmaz (<see cref="BState.Decoupled"/>; oda raporunun
        /// Decouple'ı), Teknisyen'in o turki yeteneği harcanır, Fx "decouple" (Unit Teknisyen), telsiz satırı, oda geçilir
        /// (<see cref="PassRoom"/>). GEÇ gibi geri alınmaz. Verilemiyorsa false ve durum değişmez.
        /// </summary>
        public static bool Decouple(BState B)
        {
            if (!CanDecouple(B)) return false;
            var tech = B.Units.First(u => IsSpecialist(u) && u.Type == "tek" && !u.Dead && u.AtGate && !u.Acted);
            B.FxList.Clear();
            B.Decoupled = true;
            tech.Acted = true;
            AddFx(B, new Fx { K = "decouple", X = tech.X, Y = tech.Y, Unit = tech.Id });
            Say(B, Loc.T("Vagon ayrıldı; takip yok."), "good");
            PassRoom(B);
            return true;
        }

        /// <summary>Odanın geçilmesi (GEÇ ya da kendiliğinden geçiş, <see cref="EndTurn"/>): geçmiş silinir, efektler korunur.</summary>
        static void PassRoom(BState B)
        {
            B.Hist.Clear();
            ClearHold(B, false);
            // Aşama 11: savunmada henüz gelmemiş dalga sonraki odaya yeniden zamanlanır (kuyruktan düşer); takipçiler ve gelmiş ama
            // yerleşmemiş makineler kuyrukta kalır, oda raporu onları batı girişinde takipçi olarak yazar (RaidChain.Report)
            if (B.Defense) B.Incoming.RemoveAll(i => i.Wave != 0 && i.Arrive > B.Turn);
            B.Phase = "pass";
            Say(B, Loc.T("Ekip geçitten geçti."), "good");
        }

        /// <summary>Zorluk kademesinin makine eki: kademe başına bir, takviye bölgesinde bir, sertleştirilmiş sunucu görevinde ek.</summary>
        internal static int TierExtra(RaidParams p) =>
            Math.Max(0, p.Tier - 1) + (p.Traits.Contains("takviye") ? 1 : 0) + Math.Max(0, p.ExtraMachines);

        /// <summary>
        /// Dönüş odasının tahtası: yapı ve şart plandan baştan kurulur (takvim 1. turdan), nesneler, yıkılan duvarlar, makineler ve
        /// (aşama 8) odada kalan aygıtlar odanın son raporundan (<see cref="RoomSetup.Frozen"/>) gelir; ekiple gelen köpek ve tutsak
        /// önceki odanın raporundan (<see cref="RoomSetup.CarryUnits"/>). Ekip doğu geçidinden (ileri geçit kareleri) girer, kaçış
        /// baştan açıktır. Çekimler: boş çekim 1 (plan ve uyanma <see cref="CreateRoom"/>'da); yerleşim çekimi yok. Testler makinelerin
        /// (kaydırılmış) başlangıç yerini buradan okur (<see cref="CreateRoom"/>'un sonundaki plan onları yürütür; <see cref="RoomBoard"/> gibi).
        /// </summary>
        internal static BState ReturnBoard(RaidParams p, FacilityRoom room, RoomSetup setup)
        {
            var f = setup.Frozen ?? throw new ArgumentException("Dönüş odasının raporu yok: " + room.Id, nameof(setup));
            if (room.Event != null && !string.IsNullOrEmpty(p.Event) && p.Event != room.Event)
                throw new ArgumentException("Baskının olayı odanın olayı değil: " + p.Event + " / " + room.Event + " (" + room.Id + ")", nameof(p));
            var B = new BState
            {
                Params = p, Rs = setup.Seed, Room = room, Layout = room.NameKey, MaxTurns = setup.Turns,
                SpawnPerTurn = setup.NoWake ? 0 : p.Traits.Contains("kopya") ? 2 : 1,
                IsCore = p.IsCore && p.MissionKind != "loot",
                Signature = string.IsNullOrEmpty(p.Signature) ? null : p.Signature,
                EnemyHp = p.Traits.Contains("guclu") ? 1 : 0,
                EnemyMove = p.Traits.Contains("devriye") ? 1 : 0,
                Fog = p.Traits.Contains("sis"),
                Smart = p.Tactics > 0,
                Escape = true,
                Moves = setup.Moves ?? MoveRules.From(null),
            };
            Rnd(B);
            bool eventOn = room.Event != null && !string.IsNullOrEmpty(p.Event);
            var src = !eventOn ? room.BaseCells : p.Tier >= 3 ? room.Cells3 : room.Cells1;
            var ev = !eventOn ? null : p.Tier >= 3 ? room.Ev3 : room.Ev1;
            // Plan hücreleri; olay nesneleri (tüp, kol) plandaki yerinde can 1, öbür nesneler rapordan
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var c = src[y, x].Clone();
                    if (c.O != null) { if (IsEventObject(c.O.K)) c.O.Hp = c.O.Max = 1; else c.O = null; }
                    B.Cells[y, x] = c;
                }
            foreach (var o in f.Objects)
                if (Inb(o.X, o.Y)) CellAt(B, o.X, o.Y).O = new Obj { K = o.K, Hp = o.Hp, Max = o.Max, MachineBuilt = o.MachineBuilt };
            B.Exits = new List<Pt>(room.West);
            B.Ev = ev?.Clone();
            // Yıkılan duvarlar kalıcı; raf rayındaki moloz rafı durdurur (ShelfBroken'ın etkisi, satırsız)
            foreach (int k in f.Rubble)
            {
                int x = k % N, y = k / N;
                if (!Inb(x, y)) continue;
                var c = CellAt(B, x, y);
                c.T = "floor";
                B.Rubble.Add(new Pt(x, y));
                if (c.Shelf) { c.Shelf = false; if (B.Ev != null) B.Ev.Stop = true; }
            }
            // Bu ziyarette kalkan kepenkler açık kalır (bayrak kalır; moloz sayılmaz)
            foreach (int k in f.Opened)
            {
                int x = k % N, y = k / N;
                if (Inb(x, y) && CellAt(B, x, y).Shutter) CellAt(B, x, y).T = "floor";
            }
            // Tesis aşama 10: çift cidar gidişteki seçimle (plan hücrelerinden) yeniden kurulur, yalnız hâlâ duvar olanlar; koku yayıcı
            // kaplanmaz (rapordaki nesnelerden gelir, kırıldıysa yoktur)
            if (B.Moves.DoubleWall >= 1)
            {
                var plan = new Cell[N, N];
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) plan[y, x] = src[y, x].Clone();
                foreach (var t in DoubleWallTiles(plan, room, eventOn, B.Moves.DoubleWall, B.Moves.DoubleWalls))
                    if (CellAt(B, t.X, t.Y).T == "wall") CellAt(B, t.X, t.Y).Double = true;
            }
            if (B.Ev != null) SayEvent(B, B.Ev.Id);
            var excluded = eventOn ? new HashSet<int>(room.PartTiles(3)) : new HashSet<int>();
            var squad = SquadOf(p);
            var carry = setup.Carry;
            if (carry == null || carry.Count != squad.Count)
                throw new ArgumentException("Dönüş odasının kadrosu raporla uyuşmuyor: " + room.Id, nameof(setup));
            var standing = Enumerable.Range(0, squad.Count).Where(i => carry[i].State == "a").ToList();
            // Ekiple gelen düşenler (geçitten sürüklendi ya da atlanan temiz odadan toplandı) ayaktakilerden sonra yer alır. Sedye
            // (aşama 8, K24): ekiple gelen gövde tahtaya konmaz, rapor onu önceki rapordan taşır
            var carried = B.Moves.Stretcher ? new List<int>()
                : Enumerable.Range(0, squad.Count).Where(i => carry[i].State == "d" && carry[i].Body == 1).ToList();
            // Aşama 8: ekiple gelen köpekler ve tutsaklar ayaktakilerden sonra, gövdelerden önce
            var extra = EnteringUnits(B, squad, setup);
            var spots = SquadTiles(B, room, excluded, standing.Count + extra.Count + carried.Count, room.Forward);
            var taken = new HashSet<int>(spots.Where(t => t != null).Select(t => Key(t.Value)));
            // Makineler raporun sırasıyla (kadrodan önce: kimlik sırası RoomBoard ile eş), çekimsiz; geçersiz karedeki kayar
            foreach (var m in f.Machines)
            {
                if (!UnitTypes.ContainsKey(m.Type)) continue;
                var at = new Pt(m.X, m.Y);
                bool ok = Inb(at.X, at.Y) && Passable(B, at.X, at.Y) && UnitAt(B, at.X, at.Y) == null && !taken.Contains(Key(at))
                    && CellAt(B, at.X, at.Y).Door != 2;
                if (!ok)
                {
                    var alt = NearestOpen(B, at, taken, excluded);
                    if (alt == null) continue;
                    at = alt.Value;
                }
                var u = NewUnit(B, m.Type, at.X, at.Y);
                u.Hp = Math.Max(1, Math.Min(u.Max, m.Hp));
                u.Armor = m.Armor;
                // Aşama 10: sökülen cıvata dönüşte de sökük
                u.Unbolted = m.Unbolted;
                u.Assembled = m.Assembled;
                B.Units.Add(u);
            }
            // Aşama 8: odada kalan aygıtlar kendi karelerine (geçilemiyorsa en yakın açık kareye, yoksa konmaz; çekimsiz), kalan turuyla
            foreach (var g in f.Units)
            {
                if (g.Class != ClassGadget || g.State != "a" || !UnitTypes.ContainsKey(g.Type)) continue;
                var at = new Pt(g.X, g.Y);
                bool ok = Inb(at.X, at.Y) && Passable(B, at.X, at.Y) && UnitAt(B, at.X, at.Y) == null && !taken.Contains(Key(at))
                    && CellAt(B, at.X, at.Y).Door == 0 && CellAt(B, at.X, at.Y).Vent == 0;
                if (!ok)
                {
                    var alt = NearestOpen(B, at, taken, excluded);
                    if (alt == null) continue;
                    at = alt.Value;
                }
                B.Units.Add(NewGadget(B, g.Type, at.X, at.Y, g.Dir, g.Turns, g.Owner, g.Hp, g.Armor));
            }
            AddSquad(B, p, squad, standing, spots, carry);
            AddUnits(B, extra, spots.Skip(standing.Count).ToList());
            AddBodies(B, p, squad, carried, spots.Skip(standing.Count + extra.Count).ToList(), carry, room.Forward[0]);
            // Bu odada yatan düşenler kendi karelerine (plan karesi geçilemiyorsa en yakın açık kareye; çekimsiz)
            var lying = Enumerable.Range(0, squad.Count).Where(i => carry[i].State == "d" && carry[i].Body == 0 && carry[i].Fell == room.Index).ToList();
            var lyingAt = lying.Select(i =>
            {
                var t = new Pt(carry[i].X, carry[i].Y);
                return Passable(B, t.X, t.Y) ? t : NearestOpen(B, t, taken, excluded) ?? room.Forward[0];
            }).Select(t => (Pt?)t).ToList();
            AddBodies(B, p, squad, lying, lyingAt, carry, room.Forward[0]);
            // Aşama 12 (Katar'ın takibi): terk edilen vagonun takipçileri (raporun sırasıyla) hemen, kadrodan sonra, doğu geçidine (ileri
            // geçit kareleri, sonra ondan yürüme uzaklığı en küçük boş zemin; kadronun ve parçaların kareleri dolu); makine sınırı doluysa ya da
            // yer yoksa kalanlar gelmez. Can, zırh ve söküklük rapordan; niyet CreateRoom'un planında. Çekimsiz
            if (setup.Followers != null && setup.Followers.Count > 0)
            {
                var full = new HashSet<int>(taken);
                full.UnionWith(excluded);
                var steps = StepsFrom(B, room.Forward);
                foreach (var m in setup.Followers)
                {
                    if (m == null || m.Type == null || !UnitTypes.ContainsKey(m.Type)) continue;
                    var at = Machines(B) < MaxEnemies ? EntryTile(B, room.Forward, steps, full) : null;
                    if (at == null) break;
                    var u = NewUnit(B, m.Type, at.Value.X, at.Value.Y);
                    u.Hp = Math.Max(1, Math.Min(u.Max, m.Hp));
                    u.Armor = m.Armor;
                    u.Unbolted = m.Unbolted;
                    u.Assembled = m.Assembled;
                    u.Follower = true;
                    B.Units.Add(u);
                }
            }
            // Taşıyıcı: kadro sırasındaki uzman ya da (aşama 8, K33) sahibi o sırada olan köpek
            if (setup.Cargo >= 0) B.Carrier = CarrierOf(B, setup.Cargo, setup.CargoDog);
            Say(B, Loc.T("Dönüş: ekip odaya ileri geçitten girdi. Süre dolarken tahtada kalan uzman baskında düşmüş sayılır."), "sys");
            if (setup.NoWake) Say(B, Loc.T("Çağrı anteni kırık: dönüşte kopya dolapları makine uyandırmaz."), "good");
            return B;
        }

        /// <summary>Kadronun ayaktaki üyeleri (kadro sırasıyla) ayrılmış karelerine; taşınanlar (can, plaka, EMP hücresi, yetenek hakkı)
        /// önceki odanın raporundan. Kilit, donma, buz ve kabin geçişte kalkar.</summary>
        static void AddSquad(BState B, RaidParams p, List<SquadSlot> squad, List<int> standing, List<Pt?> spots, IReadOnlyList<RoomMember> carry)
        {
            for (int k = 0; k < standing.Count; k++)
            {
                int i = standing[k];
                if (!(spots[k] is Pt at)) continue;
                var u = SquadUnit(B, p, squad[i], at.X, at.Y);
                u.Slot = i;
                if (carry != null)
                {
                    var c = carry[i];
                    u.Hp = Math.Max(1, Math.Min(u.Max, c.Hp));
                    u.Armor = Math.Max(0, c.Armor);
                    u.EmpBoosts = Math.Max(0, c.Emp);
                    u.Charges = c.Charges;
                    // Aşama 8: ekipmanın kalan hakkı önceki odadan (köpekte −1)
                    if (u.Gear != null) u.GearLeft = c.Gear;
                }
                B.Units.Add(u);
            }
        }

        /// <summary>Düşen kişilerin gövdeleri (kadro sırası, Dead, can 0; plaka, EMP hücresi ve yetenek hakkı rapordan) verilen karelere,
        /// en sona eklenir (öbür birimlerin kimlik sırası değişmez). Karesi olmayan gövde <paramref name="fallback"/>'e konur (gövde
        /// kareyi kapatmaz).</summary>
        static void AddBodies(BState B, RaidParams p, List<SquadSlot> squad, List<int> who, List<Pt?> spots, IReadOnlyList<RoomMember> carry, Pt fallback)
        {
            for (int k = 0; k < who.Count; k++)
            {
                int i = who[k];
                var at = k < spots.Count && spots[k] is Pt t ? t : fallback;
                var u = SquadUnit(B, p, squad[i], at.X, at.Y);
                var c = carry[i];
                u.Slot = i;
                u.Dead = true;
                u.Hp = 0;
                u.Armor = Math.Max(0, c.Armor);
                u.EmpBoosts = Math.Max(0, c.Emp);
                u.Charges = c.Charges;
                if (u.Gear != null) u.GearLeft = c.Gear;
                B.Units.Add(u);
            }
        }

        /// <summary>
        /// Kaydırılan makinenin karesi: kaynaktan (geçilmez olsa da) başlayan BFS ile yürüyüş uzaklığı en küçük aday (eşitlikte küçük
        /// y, sonra küçük x); BFS'le aday yoksa Manhattan uzaklığı en küçük aday; o da yoksa null. Aday: geçilebilir, birim yok,
        /// geçit ve menfez değil, kadro karesi ve parça karesi değil.
        /// </summary>
        static Pt? NearestOpen(BState B, Pt from, HashSet<int> taken, HashSet<int> excluded)
        {
            bool Cand(int x, int y)
            {
                if (!Passable(B, x, y) || UnitAt(B, x, y) != null) return false;
                var c = CellAt(B, x, y);
                int k = y * N + x;
                return c.Door == 0 && c.Vent == 0 && !taken.Contains(k) && !excluded.Contains(k);
            }
            Pt? Better(Pt? best, Pt t, int[,] d) =>
                best == null || d[t.Y, t.X] < d[best.Value.Y, best.Value.X] ? t : best;
            var dist = new int[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) dist[y, x] = -1;
            if (Inb(from.X, from.Y))
            {
                var q = new Queue<Pt>();
                dist[from.Y, from.X] = 0;
                q.Enqueue(from);
                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    foreach (var d in Dirs)
                    {
                        int x = p.X + d[0], y = p.Y + d[1];
                        if (!Passable(B, x, y) || dist[y, x] >= 0) continue;
                        dist[y, x] = dist[p.Y, p.X] + 1;
                        q.Enqueue(new Pt(x, y));
                    }
                }
            }
            Pt? best = null;
            // AllTiles y, x sırasıyla: "küçükten büyüğe" karşılaştırmada ilk bulunan eşitliği kazanır
            foreach (var t in AllTiles())
                if (dist[t.Y, t.X] >= 0 && Cand(t.X, t.Y)) best = Better(best, t, dist);
            if (best != null) return best;
            var man = new int[N, N];
            foreach (var t in AllTiles()) man[t.Y, t.X] = Man(t.X, t.Y, from.X, from.Y);
            foreach (var t in AllTiles())
                if (Cand(t.X, t.Y)) best = Better(best, t, man);
            return best;
        }
    }
}
