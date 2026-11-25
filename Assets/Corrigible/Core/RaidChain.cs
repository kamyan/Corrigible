using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Zincirde oynanacak oda: sırası (1 G, 2 A, 3 H) ve dönüş mü.</summary>
    public readonly struct RoomStep
    {
        public readonly int Room;
        public readonly bool Return;
        public RoomStep(int room, bool ret) { Room = room; Return = ret; }
    }

    /// <summary>
    /// Tesis baskınının oda zinciri (Docs/TESIS-BASKINLARI-TASARIMI.md §3.5, §11): saf, durumsuz, RNG'siz. Girdi her zaman
    /// <see cref="RaidParams"/> ve biten odaların raporlarıdır (+ plan ve denge). Sıradaki oda (temiz oda atlanır), odanın kurulum
    /// değerleri (taşınanlar, hazırlık, dönüşte donmuş oda), odayı açma, biten tahtanın raporu ve baskının sonucu buradan gelir.
    /// Simülasyon (<c>Simulation.AdvanceRaidRoom</c>), sunum ve bot aynı fonksiyonları çağırır; canlı oyun, kaydet-devam ve yeniden
    /// oynatma her odayı raporlardan kurar.
    /// </summary>
    public static class RaidChain
    {
        /// <summary>Tesis baskını: emirde kanadın üç odası yazılı. Harekât emrinde hep doğru; bekçi olarak kalır (emir yok ya da koddan
        /// kurulan örnek tahta).</summary>
        public static bool IsFacility(RaidParams p) => p?.Rooms != null && p.Rooms.Count == 3;

        /// <summary>Tesis aşama 11: üç odalı savunma emri (<see cref="RaidParams.Mode"/> "defense").</summary>
        public static bool IsDefense(RaidParams p) => p?.Mode == "defense";

        /// <summary>Savunmada terk edilen (içeri çekilinen) oda sayısı: geçişle biten oda raporları. Saldırıda 0.</summary>
        public static int Abandoned(RaidParams p, IReadOnlyList<RoomReport> reports) =>
            IsDefense(p) && reports != null ? reports.Count(r => r.End == "pass") : 0;

        /// <summary>Emirdeki kırık donanım izlerinin türleri (<see cref="RaidParams.Marks"/>, "donanim"; plandaki odanın donanımı). Kırık
        /// donanım baskınlar arasında kırık kalır: hedef odasının hazırlığı sürer (belge §9.1, K30). Plan ya da iz yoksa boş.</summary>
        public static HashSet<string> MarkedDevices(RaidParams p, FacilityPlan plan)
        {
            var o = new HashSet<string>();
            if (p?.Marks == null || plan == null) return o;
            foreach (var m in p.Marks)
                if (m != null && m.Kind == FacilityMarks.Device && plan.Room(m.Room)?.Device is string d) o.Add(d);
            return o;
        }

        /// <summary>Baskının kadro boyu (raporun Team satırı sayısı).</summary>
        public static int SquadCount(RaidParams p) => Baskin.SquadOf(p).Count;

        /// <summary>Oda tohumu: baskın tohumundan türetme (gidiş G 2, A 4, H 6; dönüş G 3, A 5). S.Rng'ye dokunmaz.</summary>
        public static int RoomSeed(int raidSeed, int room, bool returning) =>
            unchecked((int)Rng.Mix((uint)raidSeed, (uint)(room * 2 + (returning ? 1 : 0))));

        /// <summary>Odanın son ziyaretinin raporu; oynanmadıysa null.</summary>
        public static RoomReport LastVisit(IReadOnlyList<RoomReport> reports, int room) => reports.LastOrDefault(r => r.Room == room);

        /// <summary>Temiz oda (belge §3.5): son ziyaretinde ayakta makine yok ve sağlam kopya dolabı yok. Temiz oda oynanmaz.</summary>
        public static bool IsClean(RoomReport visit) =>
            visit != null && visit.Machines.Count == 0 && !visit.Objects.Any(o => o.K == "spawner");

        /// <summary>Kırılan donanımların türleri (raporların Device alanları).</summary>
        public static HashSet<string> Broken(IReadOnlyList<RoomReport> reports) =>
            new HashSet<string>(reports.Where(r => !string.IsNullOrEmpty(r.Device)).Select(r => r.Device));

        /// <summary>
        /// Sıradaki oda: ilk oda G; gidişte geçilen odadan sonraki; hedeften kaçışta ve dönüşte batıya doğru temiz olmayan ilk oda
        /// (aşama 12: Katar'da takipçili geçişte batıdaki komşu oda, temiz olsa da).
        /// Baskın bittiyse (son rapor <see cref="RoomReport.Last"/> ya da geçişsiz bitti) ya da tesisin girişine yürünerek
        /// varıldıysa null.
        /// </summary>
        public static RoomStep? Next(RaidParams p, IReadOnlyList<RoomReport> reports)
        {
            if (reports.Count == 0) return new RoomStep(1, false);
            var last = reports[reports.Count - 1];
            if (last.Last || last.End != "pass") return null;
            if (!last.Return && last.Room < 3) return new RoomStep(last.Room + 1, false);
            // Aşama 12 (Katar): takipçili geçiş sonraki vagonu (batıdaki komşu) temiz olsa da oynatır
            if (last.Follow != null && last.Follow.Count > 0 && last.Room > 1) return new RoomStep(last.Room - 1, true);
            // Hedeften kaçış ya da ara odanın dönüşünden: batıya, temiz olmayan ilk oda
            for (int k = last.Room - 1; k >= 1; k--)
                if (!IsClean(LastVisit(reports, k))) return new RoomStep(k, true);
            return null;
        }

        /// <summary>Rapor baskının son odası mı: geçişsiz biten oda, ya da geçişten sonra oynanacak oda kalmadı (yürüyerek çıkış).</summary>
        public static bool EndsHere(RaidParams p, IReadOnlyList<RoomReport> before, RoomReport r)
        {
            if (r.End != "pass") return true;
            // Next, Last'ı gördüğü an durur: kopyada Last kapalı
            var probe = RoomReport.Decode(r.Encode());
            probe.Last = false;
            return Next(p, before.Concat(new[] { probe }).ToList()) == null;
        }

        /// <summary>
        /// Odanın kurulumu: dengeden oda değerleri (<see cref="FacilityMaps.Setup"/>) ve oda tohumu; raporlardan taşınanlar (son
        /// raporun kadrosu, yükü ve aşama 8'in ekiple gelen birimleri), dönüşte odanın son raporu ve dönüş süresi, hedef odasında kırılan
        /// donanımların hazırlığı. Aşama 8: emirdeki kazanımlar (<see cref="RaidParams.Perks"/>) hamle değerlerine
        /// (<see cref="MoveRules.From(Balance, RaidParams)"/>), Eşik gidişteki giriş odasının, Açık hat dönüş odasının süresine işler.
        /// Aşama 11: ilk ziyarette emirdeki odanın izleri (<see cref="RoomSetup.Marks"/>); <paramref name="plan"/> verilirse emirdeki kırık
        /// donanım izleri de hedef odası hazırlığına girer (<see cref="MarkedDevices"/>). Savunma emri <see cref="SetupDefense"/>'a dallanır.
        /// </summary>
        public static RoomSetup Setup(Balance b, FacilityRoom room, RaidParams p, IReadOnlyList<RoomReport> reports, bool returning, FacilityPlan plan = null)
        {
            if (IsDefense(p)) return SetupDefense(b, room, p, reports);
            int idx = room.Index;
            var s = FacilityMaps.Setup(b, room, RoomSeed(p.Seed, idx, returning));
            var perks = p.Perks ?? new List<string>();
            s.Moves = MoveRules.From(b, p);
            var last = reports.Count > 0 ? reports[reports.Count - 1] : null;
            s.Carry = last == null ? null : Collect(last, new RoomStep(idx, returning));
            s.Cargo = last?.Cargo ?? -1;
            s.CarryUnits = last == null ? null : CollectUnits(last);
            s.CargoDog = last != null && last.CargoDog;
            if (!returning && room.Type == RoomType.Entry && perks.Contains(FacilityGear.Threshold)) s.Turns += Math.Max(0, b.facilityThresholdTurns);
            var broken = Broken(reports);
            // Aşama 11: izler yalnız ilk ziyarette (dönüş tahtası odanın raporundan kurulur); izsiz emirde hiçbir şey
            bool marked = p.Marks != null && p.Marks.Count > 0;
            if (marked && !returning)
            {
                var mine = p.Marks.Where(m => m != null && m.Room == room.Id).ToList();
                if (mine.Count > 0) s.Marks = mine;
                if (room.Type == RoomType.Target) broken.UnionWith(MarkedDevices(p, plan));
            }
            if (returning)
            {
                s.Return = true;
                s.Turns = b.facilityReturnTurns + (perks.Contains(FacilityGear.OpenLine) ? Math.Max(0, b.facilityOpenLineTurns) : 0);
                s.Frozen = LastVisit(reports, idx);
                s.NoWake = broken.Contains("anten");
                // Aşama 12 (Katar): terk edilen vagonun takipçileri (önceki raporun Follow dizinleri; aralık dışı dizin yok sayılır)
                if (last != null && last.Follow != null && last.Follow.Count > 0)
                    s.Followers = last.Follow.Where(i => i >= 0 && i < last.Machines.Count).Select(i => last.Machines[i]).ToList();
            }
            else if (room.Type == RoomType.Target)
            {
                if (broken.Contains("pano")) s.QuietTurns = b.facilityPanelQuietTurns;
                if (broken.Contains("anten")) s.MachinesLess = b.facilityAntennaMachines;
                if (broken.Contains("vana"))
                {
                    if (p.MissionKind == "loot") s.StillTurns = b.facilityValveStillTurns;
                    else s.TargetHpLess = b.facilityValveTargetHp;
                }
            }
            return s;
        }

        /// <summary>
        /// Savunma odasının kurulumu (aşama 11, belge §9.2): oda değerleri ve oda tohumu (gidiş), toplam süre
        /// (<see cref="Balance.facilityDefenseTurns"/>), başlangıç turu (önceki odanın son turu + 1; ilk odada 1), taşınanlar (önceki
        /// raporun kadrosu ve birimleri; yük yok), takipçiler (önceki raporun ayaktaki makineleri), terk edilen oda sayısı, saha rölesinin
        /// canı (Siper eki), odanın izleri ve bu odaya düşen dalgalar (<see cref="Baskin.DefenseSchedule"/>; başlangıç turları raporlardan:
        /// kayıttan devamda aynı takvim). Hazırlık, uyanmasız dönüş ve donmuş oda yoktur. Değerler sıkıştırılır (tur, aralık, can ≥ 1;
        /// dalga, taban ≥ 0).
        /// </summary>
        static RoomSetup SetupDefense(Balance b, FacilityRoom room, RaidParams p, IReadOnlyList<RoomReport> reports)
        {
            int idx = room.Index;
            var s = FacilityMaps.Setup(b, room, RoomSeed(p.Seed, idx, false));
            var perks = p.Perks ?? new List<string>();
            s.Moves = MoveRules.From(b, p);
            s.Defense = true;
            s.Machines = 0;
            s.Turns = DefenseTurns(b);
            var last = reports.Count > 0 ? reports[reports.Count - 1] : null;
            s.StartTurn = last != null ? last.Turns + 1 : 1;
            s.Carry = last == null ? null : Collect(last, new RoomStep(idx, false));
            s.CarryUnits = last == null ? null : CollectUnits(last);
            s.Cargo = -1;
            s.Followers = last?.Machines;
            s.Abandoned = idx - 1;
            s.RelayHp = DefenseRelayHp(b, perks);
            if (p.Marks != null && p.Marks.Count > 0)
            {
                var mine = p.Marks.Where(m => m != null && m.Room == room.Id).ToList();
                if (mine.Count > 0) s.Marks = mine;
            }
            // Başlangıç turları: S1 = 1, sonraki her oda önceki raporun son turu + 1 (savunmanın raporları sırayla 1..idx−1)
            var starts = new List<int> { 1 };
            foreach (var r in reports) starts.Add(r.Turns + 1);
            s.Waves = DefenseSchedule(b, p.Tier, starts).Where(w => w.Room == starts.Count).ToList();
            return s;
        }

        /// <summary>Savunmanın toplam süresi (tek tur sayacı): <see cref="Balance.facilityDefenseTurns"/>, en az 1.</summary>
        public static int DefenseTurns(Balance b) => Math.Max(1, b.facilityDefenseTurns);

        /// <summary>Savunmanın dalga sayısı: <see cref="Balance.facilityDefenseWaves"/>, en az 0.</summary>
        public static int DefenseWaveCount(Balance b) => Math.Max(0, b.facilityDefenseWaves);

        /// <summary>Dengeden dalga takvimi (<see cref="Baskin.DefenseSchedule"/>; <paramref name="starts"/> null ise yalnız giriş odası:
        /// emirden önceki önizleme).</summary>
        public static List<Baskin.DefenseWave> DefenseSchedule(Balance b, int tier, IReadOnlyList<int> starts) =>
            Baskin.DefenseSchedule(b.facilityDefenseFirstWave, b.facilityDefenseWaveTurns, DefenseWaveCount(b), b.facilityDefenseWaveBase, tier, starts);

        /// <summary>Saha rölesinin canı: <see cref="Balance.facilityDefenseRelayHp"/> (en az 1) + Siper'le <see cref="Balance.facilityCoverRelayHp"/>.</summary>
        public static int DefenseRelayHp(Balance b, IReadOnlyCollection<string> perks) =>
            Math.Max(1, b.facilityDefenseRelayHp) + (perks != null && perks.Contains(FacilityGear.Cover) ? Math.Max(0, b.facilityCoverRelayHp) : 0);

        /// <summary>Sıradaki odanın tahtası (<see cref="Baskin.CreateRoom"/>). Emrin odası yoksa, odalar bittiyse ya da oda planda
        /// yoksa InvalidOperationException.</summary>
        public static Baskin.BState Open(Balance b, FacilityPlan plan, RaidParams p, IReadOnlyList<RoomReport> reports)
        {
            if (!IsFacility(p)) throw new InvalidOperationException("Baskının odaları yok");
            var step = Next(p, reports) ?? throw new InvalidOperationException("Baskının odaları bitti");
            string id = p.Rooms[step.Room - 1];
            var room = plan?.Room(id) ?? throw new InvalidOperationException("Oda yok: " + id);
            return Baskin.CreateRoom(p, room, Setup(b, room, p, reports, step.Return, plan));
        }

        /// <summary>
        /// Odaya giren kadro (saf): <paramref name="last"/>'ın kadrosunun kopyası; dönüşte atlanan temiz odalarda
        /// (<paramref name="step"/>'in odasıyla son raporun odası arası) yatan düşenler ekiple gelir (Body 1, Picked 1; belge §3.5
        /// "temiz odadan toplanan").
        /// </summary>
        public static List<RoomMember> Collect(RoomReport last, RoomStep step)
        {
            var team = last.Team.Select(Copy).ToList();
            if (!step.Return) return team;
            foreach (var m in team)
                if (m.State == "d" && m.Body == 0 && m.Fell > step.Room && m.Fell < last.Room) { m.Body = 1; m.Picked = 1; }
            return team;
        }

        /// <summary>Önceki odanın raporundan ekiple gelen birimler (saf, kopya): köpekler hep (düşen "d" de), ayaktaki tutsaklar; aygıtlar
        /// odada kalır.</summary>
        public static List<RoomUnit> CollectUnits(RoomReport last) =>
            (last.Units ?? new List<RoomUnit>()).Where(u => u.Class == Baskin.ClassDog || u.Class == Baskin.ClassCaptive && u.State == "a").Select(CopyUnit).ToList();

        /// <summary>Düşen kişi tesisten çıkarıldı mı (yaşar): son raporda gövdesi çıkarıldı (Body 2). Savunma kancası (aşama 11, K22):
        /// kazanılan savunmada ekiple gelen (Body 1) ve ekibin bulunduğu son odada yatan (Body 0, Fell = son oda) çıkarılmış sayılır;
        /// kaybedilen ya da geri çekilinen savunmada kimse, terk edilen odada yatan hiç çıkarılmaz. Aşama 12: Katar'da kenardan düşen ("k")
        /// çıkarılmış sayılmaz; <see cref="RaidResult.Overboard"/>'dadır (ölmez, yatar).</summary>
        public static bool IsExtracted(RaidParams p, RoomReport last, RoomMember m)
        {
            if (IsDefense(p)) return m.State == "d" && last != null && last.Success && (m.Body == 1 || m.Body == 0 && m.Fell == last.Room);
            return m.State == "d" && m.Body == 2;
        }

        /// <summary>
        /// Biten tahtanın (evre pass, won ya da lost) raporu: kadro sırasıyla kişiler (tahtada olmayan, önceki odalarda düşen ve kenardan
        /// düşen kişi önceki rapordan aynen), yük, ayaktaki makineler, olay nesneleri dışındaki bütün nesneler, yıkılan duvarlar, kırılan
        /// donanım; son oda bilgisi ve başarı. Sonuç <see cref="RoomReport.Decode"/>(<see cref="RoomReport.Encode"/>) kopyasıdır
        /// (yeniden oynatmadaki nesnenin aynısı).
        /// </summary>
        public static RoomReport Report(RaidParams p, IReadOnlyList<RoomReport> reports, Baskin.BState B)
        {
            if (B.Phase != "pass" && B.Phase != "won" && B.Phase != "lost") throw new InvalidOperationException("Oda bitmedi: " + B.Phase);
            var step = Next(p, reports) ?? throw new InvalidOperationException("Baskının odaları bitti");
            if (B.Room == null || B.Room.Index != step.Room) throw new InvalidOperationException("Tahta beklenen oda değil");
            var prev = reports.Count > 0 ? reports[reports.Count - 1] : null;
            var carry = prev == null ? null : Collect(prev, step);
            var r = new RoomReport
            {
                Room = step.Room, Return = step.Return, Turns = Math.Min(B.Turn, B.MaxTurns), End = B.Phase,
                Retreat = B.Retreated, Collateral = B.Collateral,
            };
            int count = SquadCount(p);
            for (int i = 0; i < count; i++)
            {
                var u = B.Units.FirstOrDefault(v => v.Slot == i && (Baskin.IsSpecialist(v) || Baskin.IsTurncoat(v)));
                if (u == null)
                {
                    var was = carry != null && i < carry.Count ? carry[i] : throw new InvalidOperationException("Kadro yerleşmedi: " + i);
                    r.Team.Add(Copy(was));
                    continue;
                }
                string st = Baskin.IsTurncoat(u) ? (u.Dead && !u.Overboard && !u.Out ? "x" : "t") : u.Left ? "l" : u.Overboard ? "k" : u.Dead ? "d" : u.Withdrew ? "w" : "a";
                r.Team.Add(new RoomMember
                {
                    State = st, Hp = st == "a" || st == "w" ? u.Hp : 0, Armor = u.Armor, Emp = u.EmpBoosts, Charges = u.Charges, Gear = u.Gear != null ? u.GearLeft : -1,
                    Fell = st == "a" || st == "w" ? 0 : step.Room, X = u.X, Y = u.Y,
                    // Gövde: tesisten çıkarıldı (2) ya da geçilen kapıda ekiple (1); kapıda ama oda geçilmeden bittiyse yerde (0).
                    // Son nefes (Unit.Breath, yalnız geçişte ya da tesisin girişinde yazılır): ekiple gider (geçişte 1, girişte 2)
                    Body = st != "d" ? 0 : u.Out || u.Breath && Baskin.IsFinalExit(B) ? 2 : (u.AtGate || u.Breath) && B.Phase == "pass" ? 1 : 0,
                    Picked = carry != null && i < carry.Count ? carry[i].Picked : 0,
                    Work = u.Work, Dragged = u.Dragged,
                });
            }
            if (!Baskin.IsLoot(B)) r.Cargo = -1;
            else if (B.Carrier != 0)
            {
                var c = B.Units.FirstOrDefault(v => v.Id == B.Carrier);
                // Aşama 8 (K33): yükü köpek taşıyorsa Cargo sahibin kadro sırasıdır; tesisin girişinde tahliye olmadan tahtada kalan
                // köpeğin yükü kaybedilir (taşıyıcı çıkmadı; köpek yaşar)
                bool lost = c == null || c.Dead || Baskin.IsDog(c) && Baskin.IsFinalExit(B) && !c.Out;
                r.Cargo = lost ? -2 : Baskin.IsDog(c) ? c.Owner : c.Slot;
                r.CargoDog = !lost && Baskin.IsDog(c);
            }
            // Tesis odası: yük yerde kaldı (taşıyıcı düştü, kimse almadı): baskın ikmal olarak kaybedilir
            else if (B.CargoX >= 0) r.Cargo = -2;
            else { r.Cargo = prev?.Cargo ?? -1; r.CargoDog = r.Cargo >= 0 && prev != null && prev.CargoDog; }
            r.CargoAt = B.CargoX >= 0 ? B.CargoY * Baskin.N + B.CargoX : -1;
            foreach (var m in B.Units)
                if (m.Side == 'e' && !Baskin.IsTurncoat(m) && !m.Dead && !m.Out) r.Machines.Add(new RoomMachine { Type = m.Type, X = m.X, Y = m.Y, Hp = m.Hp, Armor = m.Armor, Unbolted = m.Unbolted, Assembled = m.Assembled });
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var o = Baskin.CellAt(B, x, y).O;
                    if (o == null || Baskin.IsEventObject(o.K)) continue;
                    r.Objects.Add(new RoomObject { K = o.K, X = x, Y = y, Hp = o.Hp, Max = o.Max, MachineBuilt = o.MachineBuilt });
                    if (o.K == "civ") r.Civilians++;
                }
            foreach (var t in B.Rubble) r.Rubble.Add(t.Y * Baskin.N + t.X);
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (Baskin.CellAt(B, x, y).Shutter && Baskin.CellAt(B, x, y).T != "wall") r.Opened.Add(y * Baskin.N + x);
            r.Crate = B.CrateOpen;
            r.Units = ReportUnits(p, B, prev);
            r.Sniff = B.Sniffed;
            // Aşama 10: bu ziyaretin sınıf sayaçları (Core sayımı; ResolveRaid tavanla toplar)
            r.Tally = B.TallyArray().ToList();
            r.Device = B.Room.Device != null && B.Room.DeviceAt is Baskin.Pt d && Baskin.CellAt(B, d.X, d.Y).O?.K != B.Room.Device ? B.Room.Device : "";
            // Aşama 11: savunmada geçişte kuyruktaki makineler (takipçiler, gelmiş ama yerleşmemiş dalga) sonraki odanın takipçileridir:
            // batı girişinin ilk karesiyle yazılır (rapora alan eklenmez)
            bool defense = IsDefense(p) && B.Defense;
            if (defense && B.Phase == "pass" && B.Room.West.Count > 0)
                foreach (var q in B.Incoming)
                    r.Machines.Add(new RoomMachine { Type = q.Type, X = B.Room.West[0].X, Y = B.Room.West[0].Y, Hp = q.Hp, Armor = q.Armor, Unbolted = q.Unbolted, Assembled = q.Assembled });
            // Aşama 12 (Katar): geçişte takip eden makinelerin Machines dizinleri ve AYIR; EndsHere'den önce (sonda takip odayı oynatır)
            if (r.End == "pass" && !defense)
            {
                var follow = new HashSet<int>(Baskin.FollowerIds(B));
                int at = 0;
                foreach (var m in B.Units)
                {
                    if (m.Side != 'e' || Baskin.IsTurncoat(m) || m.Dead || m.Out) continue;
                    if (follow.Contains(m.Id)) r.Follow.Add(at);
                    at++;
                }
                r.Decouple = B.Decoupled;
            }
            r.Last = EndsHere(p, reports, r);
            // Yürüyerek çıkış (son oda geçildi, batıdaki odalar temiz): kapıdan ekiple geçen gövde ve batıdaki temiz odalarda yatan
            // düşen tesisten çıkarılır (belge §3.6)
            if (r.Last && r.End == "pass" && !defense)
                foreach (var m in r.Team)
                {
                    if (m.State != "d") continue;
                    if (m.Body == 1) m.Body = 2;
                    else if (m.Body == 0 && m.Fell > 0 && m.Fell < step.Room) { m.Body = 2; m.Picked = 1; }
                }
            // Sedye (aşama 8, K24; belge §5.10): baskının son raporunda ekiple taşınan gövde, ekipten en az biri ayakta bitirdiyse
            // çıkarılır; kimse ayakta değilse bu odada yerde kalır (ölür)
            if (r.Last && B.Moves != null && B.Moves.Stretcher && !defense)
            {
                bool up = r.Team.Any(m => m.State == "a");
                foreach (var m in r.Team)
                {
                    if (m.State != "d" || m.Body != 1) continue;
                    if (up) m.Body = 2;
                    else { m.Body = 0; m.Fell = step.Room; }
                }
            }
            r.Success = r.End == "pass" ? r.Last && (p.MissionKind != "loot" || r.Cargo >= 0) : r.End == "won";
            return RoomReport.Decode(r.Encode());
        }

        /// <summary>
        /// Baskının sonucu, yalnız raporlardan: başarı ve geri çekilme son rapordan; kırılan sivil ve tur toplam; ayaktaki sivil her
        /// odanın son ziyaretinden toplam; can, plaka, geride kalan ve düşen son raporun kadrosundan. Son rapor Last değilse
        /// InvalidOperationException.
        /// </summary>
        public static RaidResult Result(RaidParams p, IReadOnlyList<RoomReport> reports)
        {
            var last = reports.Count > 0 ? reports[reports.Count - 1] : null;
            if (last == null || !last.Last) throw new InvalidOperationException("Baskının son odası raporlanmadı");
            var res = new RaidResult
            {
                Success = last.Success, Retreat = last.Retreat,
                // Savunmanın tur sayacı tektir: süre son raporun turudur (aşama 11)
                Collateral = reports.Sum(r => r.Collateral), Turns = IsDefense(p) ? last.Turns : reports.Sum(r => r.Turns),
                CiviliansLeft = reports.GroupBy(r => r.Room).Sum(g => g.Last().Civilians),
                Reason = "",
            };
            for (int i = 0; i < last.Team.Count; i++)
            {
                var m = last.Team[i];
                bool up = m.State == "a";
                // Geri çekilen ("w") canıyla döner; düşmüş sayılmaz
                res.Hp.Add(up || m.State == "w" ? m.Hp : 0);
                res.Armor.Add(m.Armor);
                if (m.State == "l" || m.State == "t") res.Left.Add(i);
                if (m.FieldTurned) res.Turned.Add(i);
                if (m.State == "x") res.Subdued.Add(i);
                if (m.State == "k") res.Overboard.Add(i);
                if (!up && m.State != "w") res.Down++;
                if (IsExtracted(p, last, m)) res.Extracted.Add(i);
            }
            return res;
        }

        static RoomMember Copy(RoomMember m) => new RoomMember
        {
            State = m.State, Hp = m.Hp, Armor = m.Armor, Emp = m.Emp, Charges = m.Charges, Fell = m.Fell, X = m.X, Y = m.Y,
            Body = m.Body, Picked = m.Picked, Gear = m.Gear,
        };

        static RoomUnit CopyUnit(RoomUnit u) => new RoomUnit
        {
            Class = u.Class, Type = u.Type, Owner = u.Owner, State = u.State, Hp = u.Hp, Armor = u.Armor, X = u.X, Y = u.Y, Dir = u.Dir, Turns = u.Turns,
            Assembled = u.Assembled,
        };

        /// <summary>
        /// Raporun birimleri (aşama 8): kadronun köpekli her kişisi için (kadro sırasıyla) tahtadaki köpek, tahtada yoksa önceki
        /// rapordan taşınan kaydı aynen; sonra tahtadaki ayaktaki tutsaklar ve aygıtlar (birim sırasıyla). Ekipmansız baskında boş.
        /// </summary>
        static List<RoomUnit> ReportUnits(RaidParams p, Baskin.BState B, RoomReport prev)
        {
            var o = new List<RoomUnit>();
            var squad = Baskin.SquadOf(p);
            var carried = prev == null ? new List<RoomUnit>() : CollectUnits(prev);
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad[i].Gear != FacilityGear.Dog) continue;
                var d = B.Units.FirstOrDefault(v => Baskin.IsDog(v) && v.Owner == i);
                if (d != null) o.Add(Snap(d, d.Dead ? "d" : "a"));
                else if (carried.FirstOrDefault(c => c.Class == Baskin.ClassDog && c.Owner == i) is RoomUnit was) o.Add(was);
            }
            foreach (var u in B.Units)
                if (Baskin.IsCaptive(u) && !u.Dead && !u.Out) o.Add(Snap(u, "a"));
            foreach (var u in B.Units)
                if (Baskin.IsGadget(u) && !u.Dead && !u.Out) o.Add(Snap(u, "a"));
            return o;
        }

        static RoomUnit Snap(Baskin.Unit u, string state) => new RoomUnit
        {
            Class = u.Class, Type = u.Type, Owner = u.Owner, State = state, Hp = state == "a" ? u.Hp : 0, Armor = u.Armor, X = u.X, Y = u.Y, Dir = u.Dir, Turns = u.Turns,
            Assembled = u.Assembled,
        };
    }
}
