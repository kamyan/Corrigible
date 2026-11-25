using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Tesis aşama 11: üç odalı savunmanın tahtası (Docs/TESIS-BASKINLARI-TASARIMI.md §9.2; uygulama tasarımı §2.3–2.9). Savunma
    /// emri (<see cref="RaidParams.Mode"/> "defense") aynı oda zinciriyle oynanır; oda kurulumu <c>RaidChain.SetupDefense</c>'tır,
    /// tahta <see cref="DefenseBoard"/>. Tek tur sayacı (oda değişince sıfırlanmaz), barikat turu (odanın ilk turu makinesiz), dalgalar
    /// batı girişinden (<see cref="DefenseSchedule"/>; terk edilen oda başına bir tur gecikme), terk edilen odanın takipçileri, hedef
    /// odasında saha rölesi (kırılırsa kayıp), kapı sığınak değildir (<see cref="ReturnFromGate"/>), son turda içeri çekilinmez, süre
    /// dolunca ayakta biri varsa kanat tutulur. Uyanma, şart takvimi ve tetikleri, hazırlık yoktur; şart parçaları yerindedir. Her kural
    /// <see cref="BState.Defense"/> arkasındadır: saldırı ve örnek tahta değişmez. Dalga türleri dalga başına ayrı akıştan çekilir
    /// (<c>Rng.Mix</c>); tahtanın kendi akışı (<c>Rnd</c>) yalnız makine planında çekilir. S.Rng'ye dokunmaz.
    /// </summary>
    public static partial class Baskin
    {
        /// <summary>Saha rölesinin nesne kimliği (savunmanın hedef odasında hedef dolabın yerinde).</summary>
        public const string RelayKind = "role";
        /// <summary>Makine aklı (belge §15): röleye vuruşun puanı (uzmanın 10'u ile yaralı uzmanın 14'ü arası) ve patlama alanındaki
        /// rölenin katkısı. Kod sabiti; ölçümle doğrulanır.</summary>
        const double RelayHitScore = 11, RelayBlastScore = 6;
        /// <summary>Dalga türlerinin akış tuzu (dalga numarası eklenir): aynı dalga hangi odaya gelirse gelsin aynı makineler.</summary>
        const uint WaveSalt = 0x57415645u;

        /// <summary>Yerleşmeyi bekleyen makine: dalga numarası (0 takipçi), tür, can, zırh, sökük cıvata ve geliş turu.</summary>
        public sealed class Incoming
        {
            public int Wave;
            public string Type;
            public int Hp, Armor;
            public bool Unbolted, Assembled;
            public int Arrive;
            public Incoming Clone() => (Incoming)MemberwiseClone();
        }

        /// <summary>
        /// Savunma dalgası: numarası (1'den), çıkış turu, geliş turu (gecikme dahil), makine sayısı ve geldiği oda sırası; okuyucuda
        /// (<see cref="DefenseWaves"/>) ayrıca yerleşen ve kuyrukta bekleyen makine sayısı.
        /// </summary>
        public sealed class DefenseWave
        {
            public int N, Depart, Arrive, Count, Room;
            public int Arrived, Queued;
        }

        /// <summary>Sonraki turlarda gelecek makine (<see cref="DefenseIncoming"/>): geliş turu, dalga (0 takipçi), tür ve tahmini karesi
        /// (yalnız sonraki tur gelecekler için; yer yoksa ya da daha sonra gelecekse −1).</summary>
        public sealed class IncomingView
        {
            public int Turn, Wave, X = -1, Y = -1;
            public string Type;
        }

        // ---------- Dalga takvimi ----------
        /// <summary>Bir dalganın makine sayısı: taban (en az 0) + en çok 3 olmak üzere kademe (K14; kademe 1/2/3/4 → taban 1 ile 2/3/4/4).</summary>
        public static int DefenseWaveSize(int baseSize, int tier) => Math.Max(0, baseSize) + Math.Min(Math.Max(0, tier), 3);

        /// <summary>
        /// Dalga takvimi (saf, çekimsiz). Dalga k (1..<paramref name="waves"/>) <c>D = first + (k−1) × interval</c> turunda çıkar ve
        /// <c>baseSize + min(tier, 3)</c> makinedir. <paramref name="starts"/>: oynanan odaların başlangıç turları (S1 = 1; sonraki
        /// odanınki önceki odanın son turu + 1); son öğe bulunulan odadır. Dalganın j. odaya (0'dan) gelişi <c>max(D + j, S_j + 1)</c>:
        /// terk edilen her oda bir tur geciktirir, odanın ilk turu (barikat) makinesizdir. Bir dalga tek odaya gelir: ekip odadan dalga
        /// gelmeden çekildiyse (geliş ≥ sonraki odanın başlangıcı) dalga sonraki odaya yeniden zamanlanır. Her dalga, geldiği odayla
        /// (<see cref="DefenseWave.Room"/>, 1'den) bir kez listelenir; bulunulan odanınkiler son odanın sırasını taşır.
        /// </summary>
        public static List<DefenseWave> DefenseSchedule(int first, int interval, int waves, int baseSize, int tier, IReadOnlyList<int> starts)
        {
            var o = new List<DefenseWave>();
            if (starts == null || starts.Count == 0) starts = new[] { 1 };
            first = Math.Max(1, first);
            interval = Math.Max(1, interval);
            int size = DefenseWaveSize(baseSize, tier);
            for (int k = 1; k <= waves; k++)
            {
                int d = first + (k - 1) * interval;
                for (int j = 0; j < starts.Count; j++)
                {
                    int a = Math.Max(d + j, starts[j] + 1);
                    if (j < starts.Count - 1 && a >= starts[j + 1]) continue;
                    o.Add(new DefenseWave { N = k, Depart = d, Arrive = a, Count = size, Room = j + 1 });
                    break;
                }
            }
            return o;
        }

        // ---------- Kurulum ----------
        /// <summary>
        /// Savunma odasının tahtası (<see cref="CreateRoom"/> <see cref="RoomSetup.Defense"/> iken; <see cref="RoomBoard"/> kalıbı): tur
        /// başlangıç turundan, süre toplam (Kısa nöbet uygulanmaz), uyanma yok, olay durumu yok ama hücreler kademenin şartlı hâlinden
        /// (parçalar yerinde, nesne olan parça sıradan nesne), önlem kaplaması ve izler (plan = emirdeki tahta), hedef odasında hedef
        /// dolabın yerine saha rölesi (<see cref="RoomSetup.RelayHp"/>). Kadro ilk odada ileri geçidin önünde (geçit karesinde değil),
        /// iç odalarda batı geçidinden (gidiş gibi); köpek, tutsak ve taşınan gövdeler ardından. Başlangıç makinesi yok (barikat turu);
        /// takipçiler ve bu odaya düşen dalgalar <see cref="BState.Incoming"/>'de, dalga türleri dalga başına ayrı akıştan. Tahtanın
        /// kendi akışından yalnız boş çekim 1.
        /// </summary>
        internal static BState DefenseBoard(RaidParams p, FacilityRoom room, RoomSetup setup)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (room.Event != null && !string.IsNullOrEmpty(p.Event) && p.Event != room.Event)
                throw new ArgumentException("Baskının olayı odanın olayı değil: " + p.Event + " / " + room.Event + " (" + room.Id + ")", nameof(p));
            bool target = room.Type == RoomType.Target;
            int start = Math.Max(1, setup.StartTurn);
            var B = new BState
            {
                Params = p, Rs = setup.Seed, Room = room, Layout = room.NameKey,
                Turn = start, MaxTurns = Math.Max(1, setup.Turns), SpawnPerTurn = 0, IsCore = false,
                Signature = string.IsNullOrEmpty(p.Signature) ? null : p.Signature,
                EnemyHp = p.Traits.Contains("guclu") ? 1 : 0,
                EnemyMove = p.Traits.Contains("devriye") ? 1 : 0,
                Fog = p.Traits.Contains("sis"),
                Smart = p.Tactics > 0,
                Moves = setup.Moves ?? MoveRules.From(null),
                Defense = true, StartTurn = start, Abandoned = Math.Max(0, setup.Abandoned),
            };
            Rnd(B);
            bool eventOn = room.Event != null && !string.IsNullOrEmpty(p.Event);
            var src = !eventOn ? room.BaseCells : p.Tier >= 3 ? room.Cells3 : room.Cells1;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var c = src[y, x].Clone();
                    var o = c.O;
                    if (o != null)
                    {
                        int hp = o.Hp;
                        if (IsTarget(o.K)) { o.K = "node"; hp = 4; }
                        else if (o.K == "civ") hp = Math.Max(1, p.CivilianHp);
                        else if (o.K == "cool" || o.K == "tup" || o.K == "kol") hp = 1;
                        else if (o.K == "spawner") hp = 2;
                        else if (IsDevice(o.K)) hp = Math.Max(1, setup.DeviceHp);
                        o.Hp = o.Max = hp;
                    }
                    B.Cells[y, x] = c;
                }
            // Önlem kaplaması ve izler saldırı tahtasının sırasıyla (hedef dolap henüz dolap: plan = emirdeki tahta)
            ApplyCounterCells(B.Cells, room, eventOn, B.Moves.DoubleWall, B.Moves.Emitter, B.Moves.DoubleWalls, B.Moves.EmitterHp);
            if (setup.Marks != null && setup.Marks.Count > 0) FacilityMarks.Apply(B.Cells, room, setup.Marks, B.Rubble, eventOn);
            // Saha rölesi yalnız hedef odasında (sunucu dolabı ya da ikmal kasası yerine)
            if (target)
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                        if (B.Cells[y, x].O is Obj t && IsTarget(t.K)) B.Cells[y, x].O = NewObj(RelayKind, Math.Max(1, setup.RelayHp));
            B.Exits = new List<Pt>(room.West);
            var excluded = eventOn ? new HashSet<int>(room.PartTiles(3)) : new HashSet<int>();
            var squad = SquadOf(p);
            var carry = setup.Carry;
            if (carry != null && carry.Count != squad.Count)
                throw new ArgumentException("Odaya giren kadro emirdeki kadroyla uyuşmuyor: " + room.Id, nameof(setup));
            var standing = Enumerable.Range(0, squad.Count).Where(i => carry == null || carry[i].State == "a").ToList();
            var carried = carry == null || B.Moves.Stretcher ? new List<int>()
                : Enumerable.Range(0, squad.Count).Where(i => carry[i].State == "d" && carry[i].Body == 1).ToList();
            var extra = EnteringUnits(B, squad, setup);
            int count = standing.Count + extra.Count + carried.Count;
            // İlk odada ekip ileri geçidin önünde bekler (dalgalar batıdan gelir); iç odalara batı geçidinden girer
            var spots = carry == null && room.Forward.Count > 0 ? FrontTiles(B, room, excluded, count) : SquadTiles(B, room, excluded, count);
            AddSquad(B, p, squad, standing, spots, carry);
            AddUnits(B, extra, spots.Skip(standing.Count).ToList());
            AddBodies(B, p, squad, carried, spots.Skip(standing.Count + extra.Count).ToList(), carry, room.West[0]);
            // Gelecekler: önce takipçiler (önceki odanın ayaktaki makineleri, raporun sırasıyla), sonra dalgalar (numara ve sıra)
            if (setup.Followers != null)
                foreach (var m in setup.Followers)
                    if (m != null && m.Type != null && UnitTypes.ContainsKey(m.Type))
                        B.Incoming.Add(new Incoming { Wave = 0, Type = m.Type, Hp = m.Hp, Armor = m.Armor, Unbolted = m.Unbolted, Assembled = m.Assembled, Arrive = start + 1 });
            if (setup.Waves != null)
                foreach (var w in setup.Waves.Where(w => w != null).OrderBy(w => w.N))
                {
                    B.Waves.Add(new DefenseWave { N = w.N, Depart = w.Depart, Arrive = w.Arrive, Count = w.Count, Room = w.Room });
                    var r = new Rng(Rng.Mix(unchecked((uint)p.Seed), WaveSalt + (uint)w.N));
                    for (int i = 0; i < w.Count; i++)
                    {
                        var tp = WakeType(B, r.Next());
                        B.Incoming.Add(new Incoming { Wave = w.N, Type = tp, Hp = UnitTypes[tp].Hp + B.EnemyHp, Armor = UnitTypes[tp].Armor, Arrive = w.Arrive });
                    }
                }
            Say(B, Loc.F("Savunma: {0}. turun sonuna kadar dayan. Bu tur barikat turu: makine yok.", B.MaxTurns), "sys");
            if (target) Say(B, Loc.T("Saha rölesini koru: kırılırsa kanat düşer."), "sys");
            return B;
        }

        /// <summary>İlk savunma odasında kadronun kareleri, kadro sırasıyla: ileri geçide yürüme uzaklığı en küçük boş zemin (geçit,
        /// menfez ve parça karesi değil; eşitlikte küçük y, sonra küçük x). Yer yoksa null.</summary>
        static List<Pt?> FrontTiles(BState B, FacilityRoom room, HashSet<int> excluded, int count)
        {
            var s = Steps(B, room.Forward);
            var o = new List<Pt?>();
            var taken = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                Pt? best = null;
                foreach (var t in AllTiles())
                {
                    var c = CellAt(B, t.X, t.Y);
                    if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || excluded.Contains(Key(t)) || taken.Contains(Key(t)) || s[t.Y, t.X] < 0) continue;
                    if (best == null || s[t.Y, t.X] < s[best.Value.Y, best.Value.X]) best = t;
                }
                o.Add(best);
                if (best != null) taken.Add(Key(best.Value));
            }
            return o;
        }

        // ---------- Tur sonu ----------
        /// <summary>
        /// Gelen makinelerin yerleşimi (<see cref="EndTurn"/>'de tur arttıktan sonra, plandan önce; çekimsiz): geliş turu gelmiş girdiler
        /// sırayla (takipçiler, sonra dalgalar). Kare: batı girişi sırasıyla boş olan, yoksa batıdan yürüme uzaklığı en küçük boş zemin
        /// (<see cref="IncomingTile"/>). Yer yoksa ya da makine sınırı doluysa girdi ve ardındakiler kuyrukta kalır, sonraki tur yeniden
        /// denenir. Fx "spawn" (her makine) ve "wave" (dalganın bu turda ilk yerleşeninde; N dalga, Unit adet).
        /// </summary>
        static void PlaceIncoming(BState B)
        {
            if (B.Incoming.Count == 0 || B.Phase != "player" || B.Room == null) return;
            var keep = new List<Incoming>();
            var placed = new List<(Incoming In, Unit U)>();
            bool blocked = false;
            foreach (var inc in B.Incoming)
            {
                if (blocked || inc.Arrive > B.Turn) { keep.Add(inc); continue; }
                var at = Machines(B) < MaxEnemies ? IncomingTile(B, null) : null;
                if (at == null) { blocked = true; keep.Add(inc); continue; }
                var u = NewUnit(B, inc.Type, at.Value.X, at.Value.Y);
                u.Hp = Math.Max(1, Math.Min(u.Max, inc.Hp));
                u.Armor = Math.Max(0, inc.Armor);
                u.Unbolted = inc.Unbolted;
                u.Assembled = inc.Assembled;
                B.Units.Add(u);
                AddFx(B, new Fx { K = "spawn", X = u.X, Y = u.Y });
                placed.Add((inc, u));
            }
            B.Incoming = keep;
            foreach (var (inc, u) in placed.Where(x => x.In.Wave == 0))
                Say(B, Loc.F("{0} terk edilen odadan geldi.", UnitTypes[u.Type].Name), "bad");
            foreach (var g in placed.Where(x => x.In.Wave > 0).GroupBy(x => x.In.Wave))
            {
                var first = g.First().U;
                int n = g.Count();
                AddFx(B, new Fx { K = "wave", X = first.X, Y = first.Y, N = g.Key, Unit = n });
                Say(B, Loc.F("Dalga {0}: {1} makine giriş kapısında.", g.Key, n), "bad");
            }
        }

        /// <summary>Gelen makinenin karesi: batı girişi sırasıyla boş olan (geçilebilir, ayakta birim yok); yoksa batıdan yürüme uzaklığı
        /// en küçük boş zemin (kablo, kapı, geçit ve menfez değil; eşitlikte küçük y, x). <paramref name="taken"/> ayrıca dolu sayılır.</summary>
        static Pt? IncomingTile(BState B, HashSet<int> taken) => EntryTile(B, B.Room.West, ExitSteps(B), taken);

        /// <summary>
        /// Gelen makinenin karesi (ortak yerleştirici: savunmanın gelenleri <see cref="IncomingTile"/>, aşama 12'de Katar'ın takipçileri
        /// <see cref="ReturnBoard"/>): <paramref name="gates"/> sırasıyla boş olan (geçilebilir, ayakta birim yok); yoksa <paramref name="s"/>
        /// uzaklığı en küçük boş zemin (kablo, kapı, geçit ve menfez değil; eşitlikte küçük y, x). <paramref name="taken"/> ayrıca dolu sayılır.
        /// </summary>
        static Pt? EntryTile(BState B, IReadOnlyList<Pt> gates, int[,] s, HashSet<int> taken)
        {
            bool Free(int x, int y) => Passable(B, x, y) && UnitAt(B, x, y) == null && (taken == null || !taken.Contains(y * N + x));
            foreach (var w in gates) if (Free(w.X, w.Y)) return w;
            Pt? best = null;
            foreach (var t in AllTiles())
            {
                var c = CellAt(B, t.X, t.Y);
                if (s[t.Y, t.X] < 0 || c.T != "floor" || c.Door != 0 || c.Vent != 0 || !Free(t.X, t.Y)) continue;
                if (best == null || s[t.Y, t.X] < s[best.Value.Y, best.Value.X]) best = t;
            }
            return best;
        }

        /// <summary>
        /// Savunmada kapı sığınak değildir (K17): turun sonunda kapıda bekleyen uzman ve kapıdaki gövde tahtaya döner: kendi geçit
        /// karesine, orada ayakta birim ya da yatan gövde varsa en yakın boş kareye (<see cref="NearestOpen"/>; geçit karesi değil).
        /// Bütün ayaktakiler kapıdayken tur bitmez (yalnız GEÇ); bu yalnız kısmi kapıyı kapatır. Fx "gate-out". Çekimsiz.
        /// </summary>
        static void ReturnFromGate(BState B, Unit only = null)
        {
            foreach (var u in B.Units)
            {
                if (only != null && only != u || !u.AtGate || u.Out || !IsSpecialist(u)) continue;
                var at = new Pt(u.X, u.Y);
                bool busy = !Passable(B, at.X, at.Y) || UnitAt(B, at.X, at.Y) != null || B.Units.Any(b => b != u && LyingBody(b) && b.X == at.X && b.Y == at.Y);
                if (busy)
                {
                    var bodies = new HashSet<int>(B.Units.Where(b => b != u && LyingBody(b)).Select(b => b.Y * N + b.X));
                    var alt = NearestOpen(B, at, bodies, new HashSet<int>());
                    if (alt == null) continue;
                    at = alt.Value;
                }
                u.AtGate = false;
                u.X = at.X;
                u.Y = at.Y;
                AddFx(B, new Fx { K = "gate-out", X = at.X, Y = at.Y, Unit = u.Id });
                if (!u.Dead) Say(B, Loc.F("{0} kapıdan tahtaya döndü.", UnitTypes[u.Type].Name), "sys");
            }
        }

        /// <summary>Savunmanın süresi doldu (son turun makine evresinden sonra): ekipten biri ayaktaysa (tahtada ya da kapıda) kanat tutuldu;
        /// röle kırılınca baskın zaten kaybedilmiştir.</summary>
        static void DefenseTimeUp(BState B)
        {
            if (B.Units.Any(u => IsSpecialist(u) && !u.Dead && !u.Out))
            {
                B.Phase = "won";
                B.OutcomeSuccess = true;
                B.OutcomeReason = Loc.T("Kanat tutuldu.");
                Say(B, B.OutcomeReason, "good");
                return;
            }
            Lose(B, Loc.T("Ekibin tamamı yere düştü."));
        }

        /// <summary>Saha rölesi kırıldı (<see cref="HurtObj"/>; her kaynak): savunma kaybedilir. Fx "relay-break".</summary>
        static void RelayBroken(BState B, int x, int y)
        {
            AddFx(B, new Fx { K = "relay-break", X = x, Y = y, Obj = RelayKind });
            if (B.Phase != "player") return;
            Say(B, Loc.T("Saha rölesi kırıldı."), "bad");
            Lose(B, Loc.T("Saha rölesi kırıldı."));
        }

        static Pt? RelayAt(BState B)
        {
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (B.Cells[y, x].O?.K == RelayKind) return new Pt(x, y);
            return null;
        }

        static string DefenseSignature(BState B) =>
            B.StartTurn + ":" + B.Abandoned + ":" + string.Join(",", B.Incoming.Select(i => $"{i.Wave}{i.Type}{i.Hp}.{i.Armor}{(i.Unbolted ? "U" : "")}{(i.Assembled ? "M" : "")}@{i.Arrive}"));

        // ---------- Okuyucular (saf; sunum ve bot) ----------
        /// <summary>Savunma tahtası mı.</summary>
        public static bool IsDefense(BState B) => B != null && B.Defense;

        /// <summary>Bu odanın dalgaları (numara sırasıyla; gecikme dahil): çıkış ve geliş turu, makine sayısı, yerleşen ve geliş turu
        /// gelmiş ama kuyrukta bekleyen makine. Savunma değilse boş.</summary>
        public static List<DefenseWave> DefenseWaves(BState B)
        {
            var o = new List<DefenseWave>();
            if (!IsDefense(B)) return o;
            foreach (var w in B.Waves)
            {
                int left = B.Incoming.Count(i => i.Wave == w.N);
                o.Add(new DefenseWave
                {
                    N = w.N, Depart = w.Depart, Arrive = w.Arrive, Count = w.Count, Room = w.Room,
                    Arrived = B.Turn >= w.Arrive ? Math.Max(0, w.Count - left) : 0,
                    Queued = B.Incoming.Count(i => i.Wave == w.N && i.Arrive <= B.Turn),
                });
            }
            return o;
        }

        /// <summary>
        /// Bu turdan sonra gelecek makineler, bekleme sırasıyla: geliş turu (kuyruktaki için sonraki tur), dalga, tür; sonraki tur
        /// gelecekler için yerleşimin bugünkü tahtadaki tahmini karesi (makine sınırı ve yer; makineler bu arada yer değiştirir). Savunma
        /// değilse boş. Çekimsiz, tahtayı değiştirmez.
        /// </summary>
        public static List<IncomingView> DefenseIncoming(BState B)
        {
            var o = new List<IncomingView>();
            if (!IsDefense(B) || B.Room == null) return o;
            var taken = new HashSet<int>();
            int room = MaxEnemies - Machines(B);
            foreach (var inc in B.Incoming)
            {
                var v = new IncomingView { Turn = Math.Max(inc.Arrive, B.Turn + 1), Wave = inc.Wave, Type = inc.Type };
                if (v.Turn == B.Turn + 1 && room > 0)
                {
                    var at = IncomingTile(B, taken);
                    if (at != null) { v.X = at.Value.X; v.Y = at.Value.Y; taken.Add(at.Value.Y * N + at.Value.X); room--; }
                    else room = 0;
                }
                o.Add(v);
            }
            return o;
        }

        /// <summary>Saha rölesinin canı ve azami canı; tahtada röle yoksa −1.</summary>
        public static int RelayHp(BState B) => RelayAt(B) is Pt t ? CellAt(B, t.X, t.Y).O.Hp : -1;
        public static int RelayMax(BState B) => RelayAt(B) is Pt t ? CellAt(B, t.X, t.Y).O.Max : -1;

        /// <summary>Savunmada odanın ilk turu (barikat turu: makine yok).</summary>
        public static bool BarricadeTurn(BState B) => IsDefense(B) && B.Turn == B.StartTurn;

        /// <summary>TUR BİTİR verilebilir: oyuncu evresi ve bütün ayaktakiler kapıda değil (öyleyse yalnız GEÇ).</summary>
        public static bool CanEndTurn(BState B) => B != null && B.Phase == "player" && !CanPass(B);

        /// <summary>Savunmada terk edilen oda sayısı ve odanın başlangıç turu (savunma değilse 0 ve 1).</summary>
        public static int Abandoned(BState B) => B?.Abandoned ?? 0;
        public static int StartTurn(BState B) => B?.StartTurn ?? 1;

        /// <summary>Yalnız testler: makine planı (<see cref="PlanEnemies"/>).</summary>
        internal static void TestPlan(BState B) => PlanEnemies(B);
    }
}
