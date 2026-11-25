using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis hamlelerinin değerleri (Docs/TESIS-BASKINLARI-TASARIMI.md §4, §11.1): dengeden okunur, salt okunurdur ve odanın
    /// tahtaları paylaşır (<see cref="Baskin.BState.Moves"/>, <see cref="RoomSetup.Moves"/>). Örnek tahtada yoktur (null).
    /// </summary>
    public sealed class MoveRules
    {
        /// <summary>Pusu hattı, koşu darbesinin en kısa koşusu ve itmesi, fırlatma menzili, sürükleyenin yürüyüş kaybı.</summary>
        public readonly int AmbushRange, RushRun, RushPush, ThrowRange, DragCost;
        /// <summary>"Son nefes" (<see cref="Baskin.Unit.Breath"/>): oyuncunun hamle sırası dışında düşen ve oyuncuya tur kalmadan
        /// biten odada ekiple giden uzman. Dengede <c>facilityLastBreathDies</c> ile kapanır.</summary>
        public readonly bool LastBreath;
        /// <summary>
        /// Tesis aşama 8 (Baskin.Gear.cs): tel ve kalas canı, kalasın hak bedeli, hurda canı; aygıtın çalışma turu ve canı; taret ve vinç
        /// hattı, vincin çekişi, yemin adımı; tutsağın oyuncu turu; köpeğin canı, yürüyüşü, havlamanın erimi.
        /// </summary>
        public readonly int WireHp, PlankHp, PlankCost, ScrapHp, DeviceTurns, DeviceHp, TurretRange, WinchRange, WinchPull, DecoyStep, ClampTurns,
            DogHp, DogMove, BarkRange;
        /// <summary>Sedye (aşama 8, K24): ekiple taşınan gövde odalar arasında yere konmaz; ekipten biri ayakta bitirirse çıkarılır.</summary>
        public readonly bool Stretcher;
        /// <summary>
        /// Tesis aşama 10 (önlem defteri; Baskin.Counters.cs). Tally: tahta sınıf sayaçlarını artırır (emirde önlem kademeleri yazılı);
        /// TallyAmbush: pusu isabeti de "aygıt"a sayılır (ölçüm anahtarı). Emirdeki kademeler (0..2, <see cref="FacilityCounters.Classes"/>
        /// sırası): cıvatalı ayak, çift cidar, kesici kol, kısa bağ, koku yayıcı, parazit (2. kademe makine aklı). Varsayılan 0/false:
        /// önlem yok (örnek tahta ve önlemsiz emir).
        /// </summary>
        public readonly bool Tally, TallyAmbush;
        public readonly int Bolt, DoubleWall, Cutter, ShortClamp, Emitter, Jam;
        /// <summary>Önlemlerin sayıları (dengeden): odadaki çift cidar ve hak bedeli, kesici kolun ek hasarı, kısa bağın kısalttığı tur,
        /// yayıcının canı ve erimi, parazitin kısalttığı tur, makine aklının hat cezası.</summary>
        public readonly int DoubleWalls, DoubleWallCharges, CutterDamage, ShortClampTurns, EmitterHp, EmitterRange, JamTurns;
        public readonly double LinePenalty;
        /// <summary>Cevap tarifleri (ekip düzeyinde; emirdeki <see cref="RaidParams.Answers"/>): Söküm anahtarı (Teknisyen sabotajı cıvatayı
        /// söker), Delici şarj (çift cidarın bedeli <see cref="PiercerCharges"/>), Çelik tel seti (tele can), Yedek röle (kelepçeye tur),
        /// Çift zemberek (aygıtlara tur). Cevap yoksa 0/false.</summary>
        public readonly bool Unbolt, Piercer;
        public readonly int SteelWireHp, RelayTurns, SpringTurns, PiercerCharges;
        /// <summary>Tesis aşama 12 (Katar'ın takibi, <see cref="Baskin.FollowerIds"/>): dönüşte batı geçidine (bağlantıya) yürüme uzaklığı en
        /// çok bu kadar olan makine sonraki vagona geçer (dengeden <c>facilityFollowRange</c>; varsayılan 2).</summary>
        public readonly int FollowRange;
        internal readonly int FlameRange, FlameDamage, RamRange, RamPush, AnchorRange, AnchorPull, FrostRange, FrostTurns, SealHp, BuildDamage;
        internal readonly bool Titles;
        internal readonly int TitleAmbushBonus, TitleRushBonus, TitleThrowBonus, TitleDeviceBonus, TitleDogCommands;

        /// <summary>Aşama 8'in değerleri isteğe bağlıdır; varsayılanları <see cref="Balance"/>'ınkiyle aynıdır.</summary>
        public MoveRules(int ambushRange, int rushRun, int rushPush, int throwRange, int dragCost, bool lastBreath = true,
            int wireHp = 3, int plankHp = 3, int plankCost = 2, int scrapHp = 1, int deviceTurns = 3, int deviceHp = 2, int turretRange = 4,
            int winchRange = 4, int winchPull = 1, int decoyStep = 2, int clampTurns = 3, int dogHp = 2, int dogMove = 5, int barkRange = 3,
            bool stretcher = false, int followRange = 2)
        {
            AmbushRange = Math.Max(0, ambushRange);
            RushRun = Math.Max(1, rushRun);
            RushPush = Math.Max(0, rushPush);
            ThrowRange = Math.Max(0, throwRange);
            DragCost = Math.Max(0, dragCost);
            LastBreath = lastBreath;
            WireHp = Math.Max(1, wireHp);
            PlankHp = Math.Max(1, plankHp);
            PlankCost = Math.Max(1, plankCost);
            ScrapHp = Math.Max(1, scrapHp);
            DeviceTurns = Math.Max(1, deviceTurns);
            DeviceHp = Math.Max(1, deviceHp);
            TurretRange = Math.Max(1, turretRange);
            WinchRange = Math.Max(1, winchRange);
            WinchPull = Math.Max(1, winchPull);
            DecoyStep = Math.Max(0, decoyStep);
            ClampTurns = Math.Max(1, clampTurns);
            DogHp = Math.Max(1, dogHp);
            DogMove = Math.Max(1, dogMove);
            BarkRange = Math.Max(0, barkRange);
            Stretcher = stretcher;
            FollowRange = Math.Max(0, followRange);
            // Aşama 10: önlem yok; sayılar dengenin varsayılanları (From(null) ile aynı döküm)
            DoubleWalls = 2; DoubleWallCharges = 2; CutterDamage = 1; ShortClampTurns = 1; EmitterHp = 1; EmitterRange = 3; JamTurns = 1;
            LinePenalty = 6.0;
        }

        /// <summary>Aşama 10: <paramref name="m"/>'nin değerleri + emirdeki önlem kademeleri ve cevaplar (sayılar dengeden).</summary>
        MoveRules(MoveRules m, Balance b, IList<int> counters, IList<string> answers)
        {
            AmbushRange = m.AmbushRange; RushRun = m.RushRun; RushPush = m.RushPush; ThrowRange = m.ThrowRange; DragCost = m.DragCost;
            LastBreath = m.LastBreath;
            WireHp = m.WireHp; PlankHp = m.PlankHp; PlankCost = m.PlankCost; ScrapHp = m.ScrapHp; DeviceTurns = m.DeviceTurns; DeviceHp = m.DeviceHp;
            TurretRange = m.TurretRange; WinchRange = m.WinchRange; WinchPull = m.WinchPull; DecoyStep = m.DecoyStep; ClampTurns = m.ClampTurns;
            DogHp = m.DogHp; DogMove = m.DogMove; BarkRange = m.BarkRange; Stretcher = m.Stretcher;
            FollowRange = m.FollowRange;
            FlameRange = MachineRange(b.facilityFlameRange); FlameDamage = Math.Max(1, b.facilityFlameDamage);
            RamRange = MachineRange(b.facilityRamRange); RamPush = MachineRange(b.facilityRamPush);
            AnchorRange = MachineRange(b.facilityAnchorRange); AnchorPull = MachineRange(b.facilityAnchorPull);
            FrostRange = MachineRange(b.facilityFrostRange); FrostTurns = Math.Max(1, b.facilityFrostTurns);
            SealHp = Math.Max(1, b.facilitySealHp); BuildDamage = Math.Max(1, b.facilityBuildDamage);
            Titles = b.facilityTitlesEnabled;
            TitleAmbushBonus = Math.Max(0, b.facilityTitleAmbushBonus);
            TitleRushBonus = Math.Max(0, b.facilityTitleRushBonus);
            TitleThrowBonus = Math.Max(0, b.facilityTitleThrowBonus);
            TitleDeviceBonus = Math.Max(0, b.facilityTitleDeviceBonus);
            TitleDogCommands = Math.Max(1, b.facilityTitleDogCommands);
            int Lv(int i) => counters != null && counters.Count == FacilityCounters.Classes.Length ? Math.Max(0, Math.Min(2, counters[i])) : 0;
            Tally = counters != null && counters.Count == FacilityCounters.Classes.Length;
            TallyAmbush = Tally && b.facilityTallyAmbush;
            Bolt = Lv(0); DoubleWall = Lv(1); Cutter = Lv(2); ShortClamp = Lv(3); Emitter = Lv(4); Jam = Lv(5);
            DoubleWalls = Math.Max(0, b.facilityDoubleWalls);
            DoubleWallCharges = Math.Max(1, b.facilityDoubleWallCharges);
            CutterDamage = Math.Max(0, b.facilityCutterDamage);
            ShortClampTurns = Math.Max(0, b.facilityShortClampTurns);
            EmitterHp = Math.Max(1, b.facilityEmitterHp);
            EmitterRange = Math.Max(0, b.facilityEmitterRange);
            JamTurns = Math.Max(0, b.facilityJamTurns);
            LinePenalty = b.facilityCounterLinePenalty;
            bool Has(string a) => answers != null && answers.Contains(a);
            Unbolt = Has(FacilityGear.AnsSokum);
            Piercer = Has(FacilityGear.AnsPiercer);
            PiercerCharges = Piercer ? Math.Max(1, b.facilityPiercerCharges) : 0;
            SteelWireHp = Has(FacilityGear.AnsSteelWire) ? Math.Max(0, b.facilitySteelWireHp) : 0;
            RelayTurns = Has(FacilityGear.AnsRelay) ? Math.Max(0, b.facilityRelayTurns) : 0;
            SpringTurns = Has(FacilityGear.AnsSpring) ? Math.Max(0, b.facilitySpringTurns) : 0;
        }

        /// <summary>Sınıfın (<see cref="FacilityCounters.Classes"/> dizini) emirdeki kademesi; bilinmeyen dizinde 0.</summary>
        public int Level(int index)
        {
            switch (index)
            {
                case 0: return Bolt;
                case 1: return DoubleWall;
                case 2: return Cutter;
                case 3: return ShortClamp;
                case 4: return Emitter;
                case 5: return Jam;
                default: return 0;
            }
        }

        static int MachineRange(int value) => Math.Max(1, Math.Min(Baskin.N - 1, value));

        public int Level(string cls) => Level(FacilityCounters.Index(cls));

        /// <summary>
        /// Dengenin tesis hamlesi değerleri; <paramref name="b"/> null ise <c>new Balance()</c> varsayılanları. <paramref name="p"/>
        /// verilirse emirdeki kazanımlar (<see cref="RaidParams.Perks"/>, aşama 8) uygulanır: Nişan pusu hattına, Makara vincin çekişine,
        /// Kürk köpeğin canına, Sedye (ya da ölçüm anahtarı <c>facilityStretcherAlways</c>) gövde kuralına. Kazanım payı yalnız oda
        /// zincirinden gelir (<c>RaidChain.Setup</c>). Aşama 10: emirdeki önlem kademeleri (<see cref="RaidParams.Counters"/>) ve cevaplar
        /// (<see cref="RaidParams.Answers"/>) da buradan iner; ikisi de boşsa önlem yok.
        /// </summary>
        public static MoveRules From(Balance b, RaidParams p = null)
        {
            b = b ?? new Balance();
            var perks = p?.Perks ?? new List<string>();
            return new MoveRules(Base(b, perks), b, p?.Counters, p?.Answers);
        }

        static MoveRules Base(Balance b, List<string> perks)
        {
            return new MoveRules(b.facilityAmbushRange + (perks.Contains(FacilityGear.Sight) ? Math.Max(0, b.facilitySightRange) : 0),
                b.facilityRushRun, b.facilityRushPush, b.facilityThrowRange, b.facilityDragMoveCost, !b.facilityLastBreathDies,
                b.facilityWireHp, b.facilityPlankHp, b.facilityPlankCost, b.facilityScrapHp, b.facilityDeviceTurns, b.facilityDeviceHp,
                b.facilityTurretRange, b.facilityWinchRange, perks.Contains(FacilityGear.Pulley) ? b.facilityPulleyPull : b.facilityWinchPull,
                b.facilityDecoyStep, b.facilityClampTurns, b.facilityDogHp + (perks.Contains(FacilityGear.Fur) ? Math.Max(0, b.facilityFurHp) : 0),
                b.facilityDogMove, b.facilityBarkRange, b.facilityStretcherAlways || perks.Contains(FacilityGear.Stretcher), b.facilityFollowRange);
        }
    }

    /// <summary>
    /// Tesis odasının yeni hamleleri (aşama 6; belge §4): pusu, koşu darbesi, sürükleme, fırlatma, iki kişilik iş, menfez; ayrıca
    /// düşen kişi (gövde) ve yerdeki yük. Eski API (<see cref="Reachable"/>, <see cref="Targets"/>, <see cref="DoMove"/>,
    /// <see cref="DoAct"/>, <see cref="Preview"/>) uzmanın tek yeteneğinin sözleşmesidir ve değişmez; yeni hamleler yalnız buradan
    /// girer. Her kural tesis odasına bağlıdır (<see cref="BState.Room"/> ve <see cref="BState.Moves"/> dolu); örnek tahtada hiçbir
    /// eylem verilemez. Hamleler RNG çekmez; tur içi durumları BState/Unit/Cell içinde durur (geri alma kendiliğinden getirir).
    /// </summary>
    public static partial class Baskin
    {
        // ---------- Eylem kimlikleri ----------
        /// <summary>Eylem kimlikleri (simülasyon dizgileri; kayda girmez, bot ve sunum bunlarla konuşur).</summary>
        public const string ActAmbush = "pusu", ActRush = "kosu", ActDrag = "surukle", ActThrow = "firlat", ActJoint = "ortak", ActVent = "menfez";

        /// <summary>Sabit sıra: bot adayları ve sunum tuş sırası bunu izler.</summary>
        public static readonly string[] ActionKinds = { ActAmbush, ActRush, ActDrag, ActThrow, ActJoint, ActVent };

        /// <summary>Eylemin bedeli: yeteneğin yerine (Ability), yürüyüş ve yetenek (MoveAndAbility), yalnız yürüyüş (Move).</summary>
        public enum ActionCost { Ability, MoveAndAbility, Move }

        /// <summary>pusu, firlat, ortak: Ability; kosu, surukle: MoveAndAbility; menfez: Move; aşama 8'in ekipman ve birim eylemleri:
        /// Ability. Bilinmeyen tür ArgumentException.</summary>
        public static ActionCost CostOf(string kind)
        {
            switch (kind)
            {
                case ActAmbush: case ActThrow: case ActJoint: return ActionCost.Ability;
                case ActRush: case ActDrag: return ActionCost.MoveAndAbility;
                case ActVent: return ActionCost.Move;
                // Aşama 8: ekipman ve birim eylemleri yeteneğin yerinedir (önce yürünebilir)
                case ActWire: case ActPlank: case ActCharge: case ActTurret: case ActDecoy: case ActWinch: case ActClamp:
                case ActBark: case ActFetch: case ActSniff: case ActCommand: return ActionCost.Ability;
                default: throw new ArgumentException("Bilinmeyen eylem: " + kind, nameof(kind));
            }
        }

        /// <summary>
        /// Eylemin hedeften bağımsız önkoşulu: tesis odası, evre "player", tahtadaki ekip birimi; Ability bedelinde
        /// <see cref="CanAct"/>, öbürlerinde <see cref="CanMove"/>. Fırlatmada uzman taşıyıcıdır, menfezde Analist'tir. Aşama 8: aygıt
        /// hiçbir eylem veremez; uzman <see cref="ActionKinds"/>'ı ve taşıdığı ekipmanın eylemini (hakkı yetiyorsa) verir; köpek ve
        /// tutsak yalnız kendi eylemlerini (<see cref="UnitActionKinds"/>).
        /// </summary>
        public static bool CanAction(BState B, Unit u, string kind)
        {
            if (B == null || u == null || B.Room == null || B.Moves == null || Array.IndexOf(AllActionKinds, kind) < 0) return false;
            if (B.Phase != "player" || u.Side != 'p' || !OnBoard(u) || IsGadget(u)) return false;
            if (!(CostOf(kind) == ActionCost.Ability ? CanAct(B, u) : CanMove(B, u))) return false;
            if (!IsSpecialist(u)) return UnitCanAction(B, u, kind);
            if (Array.IndexOf(UnitActionKinds, kind) >= 0) return false;
            if (Array.IndexOf(GearActionKinds, kind) >= 0) return u.Gear == GearOf(kind) && u.GearLeft >= (kind == ActPlank ? B.Moves.PlankCost : 1);
            if (kind == ActThrow && B.Carrier != u.Id) return false;
            if (kind == ActVent && u.Type != "ana") return false;
            return true;
        }

        /// <summary>
        /// Eylemin hedef kareleri (türün kendi deterministik sırası). <paramref name="other"/>: sürüklenen düşenin birim kimliği
        /// (yalnız <see cref="ActDrag"/>; uygun gövde değilse boş). Örnek tahtada ve önkoşul tutmazsa boş.
        /// </summary>
        public static List<Pt> ActionTargets(BState B, Unit u, string kind, int other = 0)
        {
            if (!CanAction(B, u, kind)) return new List<Pt>();
            switch (kind)
            {
                case ActAmbush: return AmbushTargets(B, u);
                case ActRush: return RushTargets(B, u);
                case ActDrag: return DragTargets(B, u, other);
                case ActThrow: return ThrowTargets(B, u);
                case ActJoint: return JointTargets(B, u);
                case ActVent: return VentTargets(B, u);
                default: return GearTargets(B, u, kind);
            }
        }

        /// <summary>Eylemi uygular: doğrula → geri alma görüntüsü → efektler temizlenir → kural → bedel → kaçış denetimi. Geçersizse
        /// false ve durum değişmez.</summary>
        public static bool DoAction(BState B, int uid, string kind, int x, int y, int other = 0)
        {
            var u = B == null ? null : ById(B, uid);
            if (u == null || !ActionTargets(B, u, kind, other).Any(t => t.X == x && t.Y == y)) return false;
            Snapshot(B);
            B.FxList.Clear();
            ApplyAction(B, u, kind, x, y, other);
            CheckEscape(B);
            return true;
        }

        /// <summary>Eylemin önizlemesi: durumun kopyasında aynı kural (geri alma görüntüsü yok), kaçış denetimi; sonuç
        /// <see cref="Preview"/> biçiminde. Geçersiz eylemde null; asıl durum değişmez.</summary>
        public static PreviewResult PreviewAction(BState B, int uid, string kind, int x, int y, int other = 0)
        {
            var u0 = B == null ? null : ById(B, uid);
            if (u0 == null || !ActionTargets(B, u0, kind, other).Any(t => t.X == x && t.Y == y)) return null;
            var C = B.Clone();
            C.FxList.Clear();
            var u = ById(C, uid);
            ApplyAction(C, u, kind, x, y, other);
            CheckEscape(C);
            var r = Diff(B, C);
            // Pusu hattı (kurulduğu kareden, bugünkü duvar, nesne ve bulutla; makineler hatta yürüyebilir) ve iki kişilik işin açılması
            if (kind == ActAmbush) r.Line = AmbushLine(C, u);
            if (kind == ActJoint) r.Opens = C.CrateOpen != B.CrateOpen || CellAt(C, x, y).T != CellAt(B, x, y).T;
            return r;
        }

        /// <summary>Kuralın kendisi (doğrulanmış eylem) ve bedeli.</summary>
        static void ApplyAction(BState B, Unit u, string kind, int x, int y, int other)
        {
            switch (kind)
            {
                case ActAmbush: ApplyAmbush(B, u, x, y); break;
                case ActRush: ApplyRush(B, u, x, y); break;
                case ActDrag: ApplyDrag(B, u, x, y, other); break;
                case ActThrow: ApplyThrow(B, u, x, y); break;
                case ActJoint: ApplyJoint(B, u, x, y); break;
                case ActVent: ApplyVent(B, u, x, y); break;
                default: ApplyGear(B, u, kind, x, y); break;
            }
            if (CostOf(kind) == ActionCost.Move) u.Moved = true;
            else
            {
                u.Moved = true;
                int commands = IsDog(u) ? DogCommandsFor(B, u) : 1;
                u.Acted = commands == 1 || ++u.DogCommands >= commands;
            }
        }

        // ---------- Düşen kişi (gövde) ----------
        /// <summary>Tesis gövdesi: kadronun düşen üyesi (ekip, Dead, geride kalmadı, kadro sırası var). Kapıdaki (AtGate) ve tesisten
        /// çıkarılan (Out) da gövdedir. Aşama 12: Katar'da kenardan düşen (Overboard) gövde değildir.</summary>
        public static bool IsBody(Unit u) => u != null && IsSpecialist(u) && u.Dead && !u.Left && !u.Overboard && u.Slot >= 0;

        /// <summary>Tahtada yatan gövde: kapıya sürüklenmedi, tesisten çıkarılmadı.</summary>
        public static bool LyingBody(Unit u) => IsBody(u) && !u.AtGate && !u.Out;

        /// <summary>Sürüklenebilir düşenler: uzmanın karesinde ya da dik komşusunda yatan gövdeler, birim sırasıyla. Örnek tahtada boş.</summary>
        public static List<int> DragBodies(BState B, Unit u)
        {
            if (B?.Room == null || u == null || !IsSpecialist(u) || !OnBoard(u)) return new List<int>();
            return B.Units.Where(b => LyingBody(b) && Man(b.X, b.Y, u.X, u.Y) <= 1).Select(b => b.Id).ToList();
        }

        /// <summary>Ekip uzmanının yürüyüşü (<see cref="Reachable"/>'ın menzili): kişinin kartındaki değer, yoksa türün.</summary>
        static int WalkRange(Unit u) => u.MoveRange > 0 ? u.MoveRange : UnitTypes[u.Type].Move;

        /// <summary>
        /// <see cref="Reachable"/>'ın kuralıyla (aynı geçişler, <see cref="Dirs"/> sırası) <paramref name="move"/> adımda varılan
        /// kareler; <paramref name="parent"/> verilirse her karenin BFS ebeveyni (ilk bulan) yazılır. Ekip uzmanında
        /// <c>Walk(B, u, WalkRange(u))</c> == <c>Reachable(B, u)</c>.
        /// </summary>
        static List<Pt> Walk(BState B, Unit u, int move, Dictionary<int, int> parent = null)
        {
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
                    if (parent != null) parent[ny * N + nx] = p.Y * N + p.X;
                    if (!Frosted(B, nx, ny)) q.Enqueue(new Pt(nx, ny));
                    if (other == null) o.Add(new Pt(nx, ny));
                }
            }
            return o;
        }

        /// <summary>
        /// Uzmanın <paramref name="move"/> adımlık yürüyüşüyle (<see cref="Reachable"/>'ın kuralı) karesinden (x, y)'ye en kısa yol:
        /// başlangıç ve varış dahil kare listesi (BFS, <see cref="Dirs"/> sırası, ilk bulan ebeveyn). Varılamıyorsa null.
        /// </summary>
        public static List<Pt> WalkPath(BState B, Unit u, int move, int x, int y)
        {
            var parent = new Dictionary<int, int>();
            if (!Walk(B, u, move, parent).Any(t => t.X == x && t.Y == y)) return null;
            var path = new List<Pt>();
            int start = u.Y * N + u.X;
            for (int k = y * N + x; ; k = parent[k])
            {
                path.Add(new Pt(k % N, k / N));
                if (k == start) break;
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Yürüyüşün yolu (geliştirici, 2026-11-24: hareket emrinde fareyle gezilen ulaşılabilir kareye yol çizilir): <see cref="DoMove"/>'un
        /// kuralıyla (<see cref="Reachable"/>, kişinin menzili) birimin karesinden (x, y)'ye <see cref="WalkPath"/>; yürüyemiyorsa ya da
        /// kareye varılamıyorsa null. Salt okuma: durum yazmaz, RNG çekmez. Sunum parçayı da bu yoldan yürütür.
        /// </summary>
        public static List<Pt> MovePath(BState B, Unit u, int x, int y) =>
            B == null || u == null || !CanMove(B, u) || (u.X == x && u.Y == y) ? null : WalkPath(B, u, WalkRange(u), x, y);

        /// <summary>Sürüklemenin varış kareleri: <see cref="Reachable"/>'ın kuralıyla, menzil yürüyüş − <see cref="MoveRules.DragCost"/>
        /// (1'den azsa yok). Menfezden geçilmez. <paramref name="body"/> uzmanın sürükleyebileceği gövde değilse boş.</summary>
        static List<Pt> DragTargets(BState B, Unit u, int body)
        {
            if (!DragBodies(B, u).Contains(body)) return new List<Pt>();
            int range = WalkRange(u) - DragCostFor(B, u);
            return range < 1 ? new List<Pt>() : Walk(B, u, range);
        }

        /// <summary>
        /// Sürükleme (belge §4.3): uzman varış karesine yürür, gövde onun yolda bıraktığı kareye (yolun varıştan önceki karesi; tek
        /// adımda uzmanın başlangıç karesi; kapı kareleri atlanır) gelir. Varış kapı karesiyse uzman kapıya girer ve gövde de onunla kapıdadır (karesi
        /// uzmanınki); tesisin girişinde ikisi de çıkar (gövde Out: tesisten çıkarıldı). Varış karesindeki yerdeki yükü uzman alır.
        /// Fx "drag": Unit gövde, X/Y eski kare, Dx/Dy yeni kareye fark.
        /// </summary>
        static void ApplyDrag(BState B, Unit u, int x, int y, int bodyId)
        {
            var body = ById(B, bodyId);
            var path = WalkPath(B, u, WalkRange(u) - DragCostFor(B, u), x, y);
            bool gate = IsGateTile(B, x, y);
            // Gövde kapıya yalnız varışta (sürükleyenle birlikte, AtGate) girer: yol kapı karesinden geçiyorsa gövde yoldaki kapı olmayan
            // son kareye bırakılır (yoksa yerinde kalır); kapının üstünde AtGate olmadan yatan gövde oyuncuyu yanıltırdı
            int i = path.Count - 2;
            while (i >= 0 && IsGateTile(B, path[i].X, path[i].Y)) i--;
            var left = i >= 0 ? path[i] : new Pt(body.X, body.Y);
            int bx = gate ? x : left.X, by = gate ? y : left.Y;
            if (B.Moves.Titles && u.Slot >= 0 && !string.IsNullOrEmpty(u.PersonId) && body.Slot >= 0 && body.Slot < 31 &&
                (gate || body.X != bx || body.Y != by)) u.Dragged |= 1 << body.Slot;
            AddFx(B, new Fx { K = "drag", Unit = body.Id, X = body.X, Y = body.Y, Dx = bx - body.X, Dy = by - body.Y });
            u.X = x; u.Y = y;
            body.X = bx; body.Y = by;
            PickCargo(B, u);
            if (!gate) return;
            GateIn(B, u);
            if (u.Out) body.Out = true;
            else body.AtGate = true;
        }

        // ---------- Yük ----------
        /// <summary>Yükün yerde olduğu karede kendi hareketiyle duran ekip uzmanı yükü alır (tesis odası; itilen, çekilen, kayan almaz:
        /// çağıranlar yalnız yürüyüş, koşu, sürükleme ve menfez varışıdır).</summary>
        static void PickCargo(BState B, Unit u)
        {
            // Aşama 8: yalnız uzman (köpek "getir" komutuyla alır)
            if (B.Room == null || B.CargoX < 0 || !IsSpecialist(u) || !OnBoard(u) || u.X != B.CargoX || u.Y != B.CargoY) return;
            B.Carrier = u.Id;
            B.CargoX = B.CargoY = -1;
            AddFx(B, new Fx { K = "cargo-pick", X = u.X, Y = u.Y, Unit = u.Id });
            Say(B, Loc.F("{0} yükü yerden aldı.", UnitTypes[u.Type].Name), "good");
        }

        /// <summary>Fırlatmanın alıcıları: her yönde 1..<see cref="MoveRules.ThrowRange"/> kare; aradaki kareler geçilebilir ve ayakta
        /// birimsiz (gövde kesmez), ilk ayakta birim ekip uzmanıysa alıcıdır; duvar, nesne ya da makine hattı keser. Buluttan ve
        /// buluta atılmaz; kapıdaki uzman tahtada değildir.</summary>
        static List<Pt> ThrowTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            if (InCloud(B, u.X, u.Y)) return o;
            foreach (var d in Dirs)
                for (int k = 1; k <= ThrowRangeFor(B, u); k++)
                {
                    int x = u.X + d[0] * k, y = u.Y + d[1] * k;
                    if (!Inb(x, y) || InCloud(B, x, y)) break;
                    var t = UnitAt(B, x, y);
                    // Alıcı uzman ya da köpek (aşama 8); aygıt ve tutsak hattı keser
                    if (t != null) { if (IsSpecialist(t) || IsDog(t)) o.Add(new Pt(x, y)); break; }
                    var c = CellAt(B, x, y);
                    if (c.T == "wall" || c.O != null) break;
                }
            return o;
        }

        /// <summary>Fırlatma: yük alıcıya geçer (taşıyıcı değişir). Fx "throw": X/Y alıcının karesi, Unit atan, N alıcı.</summary>
        static void ApplyThrow(BState B, Unit u, int x, int y)
        {
            var to = UnitAt(B, x, y);
            B.Carrier = to.Id;
            RecordWork(B, u, FacilityTitle.Thrower);
            AddFx(B, new Fx { K = "throw", X = x, Y = y, Unit = u.Id, N = to.Id });
            Say(B, Loc.F("{0} yükü fırlattı; yükü artık {1} taşıyor.", UnitTypes[u.Type].Name, UnitTypes[to.Type].Name), "good");
        }

        // ---------- Pusu ----------
        /// <summary>Pusunun hedefi: uzmanın dört dik komşusundan tahtada ve geçilebilir olanlar (hattın ilk karesi; yön ondan çıkar).
        /// Buluttaki uzman pusu kuramaz. İlk karesinde ayakta ekip uzmanı duran ya da bulutlu yön verilmez (hat orada biterdi: hamle
        /// boşa giderdi); makine ve gövde ilk kareyi kapatmaz.</summary>
        static List<Pt> AmbushTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            if (InCloud(B, u.X, u.Y)) return o;
            foreach (var d in Dirs)
            {
                int x = u.X + d[0], y = u.Y + d[1];
                if (!Inb(x, y) || CellAt(B, x, y).T == "wall" || StopsShot(CellAt(B, x, y).O) || InCloud(B, x, y)) continue;
                if (UnitAt(B, x, y)?.Side == 'p') continue;
                o.Add(new Pt(x, y));
            }
            return o;
        }

        /// <summary>Pusu kurulur: hattın yönü ve kurulduğu kare uzmanda durur; makine evresinde ateşlenir (<see cref="FireAmbush"/>).
        /// Fx "ambush": X/Y kurulduğu kare, Dx/Dy yön, Unit uzman.</summary>
        static void ApplyAmbush(BState B, Unit u, int x, int y)
        {
            int dx = x - u.X, dy = y - u.Y;
            u.AmbushDir = Array.FindIndex(Dirs, d => d[0] == dx && d[1] == dy);
            u.AmbushX = u.X; u.AmbushY = u.Y;
            AddFx(B, new Fx { K = "ambush", X = u.X, Y = u.Y, Dx = dx, Dy = dy, Unit = u.Id });
            Say(B, Loc.F("{0} pusu kurdu.", UnitTypes[u.Type].Name), "sys");
        }

        /// <summary>Pusu hattının kareleri (kurulduğu kareden 1..<see cref="MoveRules.AmbushRange"/>): tahta dışı, duvar, nesne ya da
        /// bulut karesinde biter (birimler sayılmaz: makineler hatta yürüyebilir). Pusu yoksa boş.</summary>
        public static List<Pt> AmbushLine(BState B, Unit u)
        {
            var o = new List<Pt>();
            if (B?.Moves == null || u == null || u.AmbushDir < 0) return o;
            var d = Dirs[u.AmbushDir];
            for (int k = 1; k <= AmbushRangeFor(B, u); k++)
            {
                int x = u.AmbushX + d[0] * k, y = u.AmbushY + d[1] * k;
                if (!Inb(x, y) || CellAt(B, x, y).T == "wall" || StopsShot(CellAt(B, x, y).O) || InCloud(B, x, y)) break;
                o.Add(new Pt(x, y));
            }
            return o;
        }

        // ---------- Tur sonu ----------
        /// <summary>
        /// Pusu hatları (makine evresi: plandan sonra, uyanmadan önce; makineler yeni yerlerinde). Birim sırasıyla, sahibi tahtadaysa
        /// hat kurulduğu kareden yürür: tahta dışı, duvar, nesne ya da bulut karesinde biter; ilk ayakta birim makineyse 1 hasar alır,
        /// ekiptense hat vurmadan biter (sis kesmez). Sonra bütün birimlerde hat silinir. Örnek tahtada hiçbir şey yapmaz. RNG çekmez.
        /// </summary>
        static void FireAmbush(BState B)
        {
            if (B.Room == null) return;
            foreach (var u in B.Units.ToList())
            {
                if (!IsSpecialist(u) || u.AmbushDir < 0 || !OnBoard(u) || B.Moves == null) continue;
                var d = Dirs[u.AmbushDir];
                for (int k = 1; k <= AmbushRangeFor(B, u); k++)
                {
                    int x = u.AmbushX + d[0] * k, y = u.AmbushY + d[1] * k;
                    // Tel örgü atışı geçirir (aşama 8)
                    if (!Inb(x, y) || CellAt(B, x, y).T == "wall" || StopsShot(CellAt(B, x, y).O) || InCloud(B, x, y)) break;
                    var t = UnitAt(B, x, y);
                    if (t == null) continue;
                    if (t.Side == 'e')
                    {
                        AddFx(B, new Fx { K = "ambush-hit", X = x, Y = y, Unit = t.Id, N = u.Id });
                        Say(B, Loc.F("Pusu: {0} vuruldu.", UnitTypes[t.Type].Name), "good");
                        // Aşama 10: pusu isabeti yalnız ölçüm anahtarıyla "aygıt"a sayılır (K6)
                        if (B.Moves.TallyAmbush) CountTally(B, FacilityCounters.Gadget);
                        RecordWork(B, u, FacilityTitle.Marksman);
                        Hurt(B, t, 1, Loc.T("pusu"));
                    }
                    break;
                }
            }
            foreach (var u in B.Units) u.AmbushDir = u.AmbushX = u.AmbushY = -1;
        }

        // ---------- Koşu darbesi ----------
        /// <summary>
        /// Koşu darbesinin hedefi: her yönde düz koşu; kare tahta dışı, duvar, nesne, turnikenin kapalı yönü ya da ayakta ekip uzmanıysa
        /// o yönde hedef yok. İlk ayakta makine L + 1. karedeyse ve <see cref="MoveRules.RushRun"/> ≤ L ≤ max(RushRun, yürüyüş) ise
        /// makinenin karesi hedeftir. Gövde, kablo ve bulut koşuyu kesmez.
        /// </summary>
        static List<Pt> RushTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            int max = Math.Max(B.Moves.RushRun, WalkRange(u));
            foreach (var d in Dirs)
            {
                int px = u.X, py = u.Y;
                for (int k = 1; k <= max + 1; k++)
                {
                    int x = u.X + d[0] * k, y = u.Y + d[1] * k;
                    if (!Inb(x, y) || CellAt(B, x, y).T == "wall" || CellAt(B, x, y).O != null) break;
                    if (B.Ev != null && !CanStep(B, px, py, x, y)) break;
                    var t = UnitAt(B, x, y);
                    if (t != null)
                    {
                        // Aşama 10: cıvatalı makine koşu darbesinin hedefi değil (itilmez; boşa hamle olurdu)
                        if (t.Side == 'e' && k - 1 >= B.Moves.RushRun && !Bolted(B, t)) o.Add(new Pt(x, y));
                        break;
                    }
                    px = x; py = y;
                }
            }
            return o;
        }

        /// <summary>
        /// Koşu darbesi (belge §4.2): uzman makinenin önündeki kareye koşar (kablo hasarı yok, geçit karesinde kapıya girmez, menfezde
        /// ışınlanmaz; yerdeki yükü alır), makine aynı yönde <see cref="MoveRules.RushPush"/> kez itilir (bugünkü itme kuralı: çarpışma
        /// 1 hasar, kablo, yağ, turnike). İtme makineyi yerinden oynatmadıysa ya da makine düştüyse kalan itme yapılmaz. Fx "rush": X/Y
        /// başlangıç, Dx/Dy koşunun farkı, Unit uzman, N makine.
        /// </summary>
        static void ApplyRush(BState B, Unit u, int x, int y)
        {
            var e = UnitAt(B, x, y);
            int dx = Math.Sign(x - u.X), dy = Math.Sign(y - u.Y);
            int ex = x - dx, ey = y - dy;
            AddFx(B, new Fx { K = "rush", X = u.X, Y = u.Y, Dx = ex - u.X, Dy = ey - u.Y, Unit = u.Id, N = e.Id });
            Say(B, Loc.F("{0} koşu darbesi indirdi.", UnitTypes[u.Type].Name), "good");
            CountTally(B, FacilityCounters.Push);
            RecordWork(B, u, FacilityTitle.Ram);
            u.X = ex; u.Y = ey;
            PickCargo(B, u);
            var d = Dirs.First(v => v[0] == dx && v[1] == dy);
            for (int i = 0; i < RushPushFor(B, u) && !e.Dead; i++)
            {
                int ox = e.X, oy = e.Y;
                Push(B, e, d);
                if (e.X == ox && e.Y == oy) break;
            }
        }

        // ---------- İki kişilik iş ----------
        /// <summary>İki kişilik işin kareleri (y, x sırası): kapalı ağır kepenk ya da açılmamış büyük sandık; uzman karenin sekiz
        /// komşusundan birinde.</summary>
        static List<Pt> JointTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            for (int y = Math.Max(0, u.Y - 1); y <= Math.Min(N - 1, u.Y + 1); y++)
                for (int x = Math.Max(0, u.X - 1); x <= Math.Min(N - 1, u.X + 1); x++)
                    if ((x != u.X || y != u.Y) && IsJointWork(B, x, y)) o.Add(new Pt(x, y));
            return o;
        }

        /// <summary>Kare iki kişilik iş: kapalı kepenk ya da açılmamış büyük sandık.</summary>
        static bool IsJointWork(BState B, int x, int y)
        {
            var c = CellAt(B, x, y);
            return c.Shutter && c.T == "wall" || c.Crate && !B.CrateOpen;
        }

        static bool Beside(Unit u, int x, int y) => Math.Max(Math.Abs(u.X - x), Math.Abs(u.Y - y)) == 1;

        /// <summary>
        /// İki kişilik iş (belge §4.5): başka bir uzman bu karede tutuyorsa (tahtada ve hâlâ karenin sekiz komşusunda) iş açılır
        /// (kepenk kalkar: T = "floor", bayrak kalır; sandık açılır), tutuş biter. Değilse uzman tutar (öncekini ezer); ikinci uzman bu
        /// tur gelmezse tur sonunda boşa gider. Fx "open" / "hold": X/Y iş karesi, Unit uzman, N açılışta tutan.
        /// </summary>
        static void ApplyJoint(BState B, Unit u, int x, int y)
        {
            bool crate = CellAt(B, x, y).Crate;
            var holder = B.HoldId != 0 && B.HoldId != u.Id && B.HoldX == x && B.HoldY == y ? ById(B, B.HoldId) : null;
            if (holder != null && OnBoard(holder) && Beside(holder, x, y))
            {
                if (crate) B.CrateOpen = true;
                else CellAt(B, x, y).T = "floor";
                B.HoldId = 0;
                B.HoldX = B.HoldY = -1;
                AddFx(B, new Fx { K = "open", X = x, Y = y, Unit = u.Id, N = holder.Id });
                Say(B, crate ? Loc.T("Büyük sandık açıldı.") : Loc.T("Kepenk kalktı."), "good");
                return;
            }
            B.HoldId = u.Id;
            B.HoldX = x; B.HoldY = y;
            AddFx(B, new Fx { K = "hold", X = x, Y = y, Unit = u.Id });
            Say(B, crate ? Loc.F("{0} büyük sandığı tutuyor: ikinci uzman bu tur gelmeli.", UnitTypes[u.Type].Name)
                : Loc.F("{0} kepengi tutuyor: ikinci uzman bu tur gelmeli.", UnitTypes[u.Type].Name), "sys");
        }

        // ---------- Menfez ----------
        /// <summary>
        /// Menfezin hedefi (yalnız Analist): uzman bir menfez ucundaysa (uzaklık 0) ya da ucu yürüyüşüyle d adımda varılabiliyorsa ve
        /// d &lt; yürüyüş, aynı numaralı öbür uç (üstünde ayakta birim yoksa) hedeftir. Gövde ve yük öbür ucu kapatmaz.
        /// </summary>
        static List<Pt> VentTargets(BState B, Unit u)
        {
            var o = new List<Pt>();
            int range = WalkRange(u);
            var dist = WalkDist(B, u, range);
            foreach (var near in AllTiles())
            {
                int n = CellAt(B, near.X, near.Y).Vent;
                if (n == 0 || !dist.TryGetValue(near.Y * N + near.X, out int d) || d >= range) continue;
                foreach (var far in AllTiles())
                {
                    if ((far.X == near.X && far.Y == near.Y) || CellAt(B, far.X, far.Y).Vent != n) continue;
                    if (far.X == u.X && far.Y == u.Y || !Passable(B, far.X, far.Y) || UnitAt(B, far.X, far.Y) != null) continue;
                    if (!o.Any(t => t.X == far.X && t.Y == far.Y)) o.Add(far);
                }
            }
            return o;
        }

        /// <summary>Uzmanın karesi (0) ve <see cref="Walk"/>'ın vardığı karelerin BFS uzaklığı (y*8+x → adım).</summary>
        static Dictionary<int, int> WalkDist(BState B, Unit u, int move)
        {
            var parent = new Dictionary<int, int>();
            var o = new Dictionary<int, int> { [u.Y * N + u.X] = 0 };
            int start = u.Y * N + u.X;
            foreach (var t in Walk(B, u, move, parent))
            {
                int d = 0;
                for (int k = t.Y * N + t.X; k != start; k = parent[k]) d++;
                o[t.Y * N + t.X] = d;
            }
            return o;
        }

        /// <summary>
        /// Menfez (belge §4.6): Analist öbür uca geçer (yürüyüşü orada biter, yeteneği kullanılabilir kalır); öbür uçtaki yükü alır.
        /// Fx "vent": X/Y yakın uç, Dx/Dy öbür uca fark, Unit uzman.
        /// </summary>
        static void ApplyVent(BState B, Unit u, int x, int y)
        {
            int n = CellAt(B, x, y).Vent;
            var dist = WalkDist(B, u, WalkRange(u));
            var near = AllTiles().Where(t => (t.X != x || t.Y != y) && CellAt(B, t.X, t.Y).Vent == n && dist.ContainsKey(t.Y * N + t.X))
                .OrderBy(t => dist[t.Y * N + t.X]).First();
            AddFx(B, new Fx { K = "vent", X = near.X, Y = near.Y, Dx = x - near.X, Dy = y - near.Y, Unit = u.Id });
            Say(B, Loc.F("{0} menfezden geçti.", UnitTypes[u.Type].Name), "good");
            u.X = x; u.Y = y;
            PickCargo(B, u);
        }

        /// <summary>İki kişilik işin tutuşu bırakılır (tur sonu: telsize "boşa gitti"; GEÇ: sessiz). Örnek tahtada hiçbir şey yapmaz.</summary>
        static void ClearHold(BState B, bool say = true)
        {
            if (B.Room == null || B.HoldId == 0) return;
            B.HoldId = 0;
            B.HoldX = B.HoldY = -1;
            if (say) Say(B, Loc.T("Tutuş boşa gitti."), "sys");
        }
    }
}
