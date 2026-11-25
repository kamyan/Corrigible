using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 11, parça P1 (Docs/TESIS-BASKINLARI-TASARIMI.md §9.2; uygulama tasarımı §2.3–2.9, §5.2): üç odalı savunmanın tahtası,
    /// sahte emirle (<see cref="RaidParams.Mode"/> "defense") ve plan 79 ile oda zinciri düzeyinde. Barikat turu, dalga takvimi ve
    /// gecikme, takipçiler ve kuyruk, tek tur sayacı, kapı (GEÇ, son tur, kısmi kapı), saha rölesi ve Siper, kazanma ve kaybetme, kaçışın
    /// ve ikmalin kapalı olması, şart takviminin ve uyanmanın işlememesi, makine aklının röleye yönelmesi, dalga türlerinin akışı, tahta
    /// çekimleri, geri alma, kayıttan devam ve gövde kancası (<see cref="RaidChain.IsExtracted"/>).
    /// </summary>
    public class FacilityDefenseTests
    {
        static Balance B0 => Bal(events: false);

        /// <summary>Kanadın savunma emri (olaysız varsayılan).</summary>
        static RaidParams DP(int wing = 0, int seed = 41, int tier = 1, bool events = false)
        {
            var p = Params(Wings[wing], seed, tier, events);
            p.Mode = "defense";
            return p;
        }

        static Baskin.BState First(RaidParams p, Balance b = null) => Open(p, new List<RoomReport>(), b ?? B0);

        static List<Baskin.Unit> Foes(Baskin.BState B) => B.Units.Where(u => u.Side == 'e' && !u.Dead).ToList();

        /// <summary>Makinelerin niyeti silinir (bu makine evresinde vurmazlar; plan yeniden kurar).</summary>
        static void Passive(Baskin.BState B) { foreach (var e in Foes(B)) e.Intent = null; }

        /// <summary>
        /// Tur sonu (Assert true): bu turda eklenen makineler (yaratılış sırasıyla) döner; <paramref name="calm"/> ise sonra bütün makineler
        /// devre dışı kalır (ekip hasar almaz, sayım temiz kalır).
        /// </summary>
        static List<Baskin.Unit> End(Baskin.BState B, bool calm = true)
        {
            var before = new HashSet<int>(B.Units.Select(u => u.Id));
            Assert.IsTrue(Baskin.EndTurn(B), "EndTurn t" + B.Turn);
            var fresh = B.Units.Where(u => u.Side == 'e' && !before.Contains(u.Id)).OrderBy(u => u.Id).ToList();
            if (calm) foreach (var e in Foes(B)) { e.Dead = true; e.Intent = null; }
            return fresh;
        }

        /// <summary>Son EndTurn'ün yerleşim kareleri (Fx "spawn", sırayla).</summary>
        static List<Baskin.Pt> Spawned(Baskin.BState B) => B.FxList.Where(f => f.K == "spawn").Select(f => new Baskin.Pt(f.X, f.Y)).ToList();

        /// <summary>Tahtaya pasif makine ekler (birim sonuna; niyetsiz).</summary>
        static Baskin.Unit AddFoe(Baskin.BState B, Baskin.Pt at, string type = "dron")
        {
            var T = Baskin.UnitTypes[type];
            var u = new Baskin.Unit { Id = B.NextId++, Type = type, Side = 'e', X = at.X, Y = at.Y, Hp = T.Hp, Max = T.Hp, Armor = T.Armor };
            B.Units.Add(u);
            return u;
        }

        /// <summary>Boş zemin kareleri (geçilebilir, birimsiz, kapı ve menfez değil), y, x sırasıyla.</summary>
        static List<Baskin.Pt> FreeTiles(Baskin.BState B) =>
            Enumerable.Range(0, Baskin.N * Baskin.N).Select(i => new Baskin.Pt(i % Baskin.N, i / Baskin.N))
                .Where(t => FreeAt(B, t.X, t.Y) && Baskin.CellAt(B, t.X, t.Y).T == "floor" && Baskin.CellAt(B, t.X, t.Y).Door == 0 && Baskin.CellAt(B, t.X, t.Y).Vent == 0).ToList();

        static bool Same(Baskin.Pt a, Baskin.Pt b) => a.X == b.X && a.Y == b.Y;

        /// <summary>Kanadın ilk iki odasını <paramref name="t1"/> ve <paramref name="t2"/> turunda (makinesiz) geçip hedef odasını açar.</summary>
        static (List<RoomReport> Reps, Baskin.BState B) Target(RaidParams p, int t1 = 1, int t2 = 2, Balance b = null)
        {
            var reps = new List<RoomReport>();
            var B = First(p, b);
            while (B.Turn < t1) End(B);
            PassRoom(p, reps, B);
            var C = Open(p, reps, b ?? B0);
            while (C.Turn < t2) End(C);
            PassRoom(p, reps, C);
            return (reps, Open(p, reps, b ?? B0));
        }

        // ---------- Kurulum ve takvim ----------
        [Test]
        public void Barricade_FirstRoom_NoMachines_TeamBeforeForwardGate()
        {
            var p = DP();
            var B = First(p);
            Assert.IsTrue(Baskin.IsDefense(B));
            Assert.AreEqual(1, B.Turn);
            Assert.AreEqual(1, Baskin.StartTurn(B));
            Assert.AreEqual(0, Baskin.Abandoned(B));
            Assert.AreEqual(10, B.MaxTurns);
            Assert.IsTrue(Baskin.BarricadeTurn(B));
            Assert.IsEmpty(Foes(B), "barikat turunda makine yok");
            Assert.AreEqual(0, B.SpawnPerTurn);
            Assert.IsEmpty(B.Spawns);
            Assert.IsNull(B.Ev);
            Assert.AreEqual(-1, Baskin.RelayHp(B), "giriş odasında röle yok");
            // Ekip ileri geçidin önünde: geçit karesinde değil, ileri geçide bitişik ya da en yakın
            foreach (var u in Standing(B))
            {
                Assert.IsFalse(Baskin.IsGateTile(B, u.X, u.Y));
                Assert.AreEqual(0, Baskin.CellAt(B, u.X, u.Y).Door);
            }
            Assert.IsTrue(Standing(B).Any(u => B.Room.Forward.Any(f => Math.Abs(f.X - u.X) + Math.Abs(f.Y - u.Y) == 1)), "biri geçidin dibinde");
            // Üç dalga 2/5/8, ikişer makine (kademe 1)
            var waves = Baskin.DefenseWaves(B);
            CollectionAssert.AreEqual(new[] { 2, 5, 8 }, waves.Select(w => w.Arrive));
            CollectionAssert.AreEqual(new[] { 2, 2, 2 }, waves.Select(w => w.Count));
            Assert.AreEqual(6, B.Incoming.Count);
            StringAssert.Contains("|V1:0:", Baskin.Signature(B));
            // Sonraki tur gelecekler: dalga 1, batı girişinin ilk iki karesi; sonrakiler karesiz
            var view = Baskin.DefenseIncoming(B);
            Assert.AreEqual(6, view.Count);
            Assert.IsTrue(Same(B.Room.West[0], new Baskin.Pt(view[0].X, view[0].Y)) && Same(B.Room.West[1], new Baskin.Pt(view[1].X, view[1].Y)));
            Assert.IsTrue(view.Take(2).All(v => v.Turn == 2 && v.Wave == 1));
            Assert.IsTrue(view.Skip(2).All(v => v.X == -1 && v.Turn > 2));
        }

        [Test]
        public void Waves_ArriveAt_2_5_8_AtWestEntry_ThenWingHeld()
        {
            var p = DP();
            var B = First(p);
            var arrived = new Dictionary<int, int>();
            while (B.Phase == "player")
            {
                var f = End(B);
                if (f.Count > 0)
                {
                    arrived[B.Turn] = f.Count;
                    // Yerleşim kareleri batı girişi sırasıyla (boş)
                    var at = Spawned(B);
                    for (int i = 0; i < at.Count; i++) Assert.IsTrue(Same(B.Room.West[i], at[i]), "batı girişi sırası");
                    Assert.IsTrue(B.FxList.Any(x => x.K == "wave" && x.Unit == f.Count), "dalga efekti");
                }
            }
            CollectionAssert.AreEqual(new[] { 2, 5, 8 }, arrived.Keys.OrderBy(k => k));
            Assert.IsTrue(arrived.Values.All(v => v == 2));
            Assert.AreEqual("won", B.Phase);
            Assert.IsTrue(B.OutcomeSuccess);
            Assert.AreEqual(Loc.T("Kanat tutuldu."), B.OutcomeReason);
            Assert.AreEqual(11, B.Turn);
            var reps = new List<RoomReport>();
            var r = File(p, reps, B);
            Assert.IsTrue(r.Last && r.Success);
            Assert.AreEqual(10, r.Turns);
            Assert.AreEqual(10, RaidChain.Result(p, reps).Turns);
            Assert.AreEqual(0, RaidChain.Abandoned(p, reps));
        }

        [Test]
        public void WaveSize_ByTier_2_3_4_4()
        {
            var b = new Balance();
            foreach (var (tier, size) in new[] { (1, 2), (2, 3), (3, 4), (4, 4) })
            {
                var w = Baskin.DefenseSchedule(b.facilityDefenseFirstWave, b.facilityDefenseWaveTurns, b.facilityDefenseWaves, b.facilityDefenseWaveBase, tier, new[] { 1 });
                Assert.AreEqual(3, w.Count);
                Assert.IsTrue(w.All(x => x.Count == size), "kademe " + tier);
            }
            // Tahtada da (kademe 3 → 4 makine)
            var B = First(DP(tier: 3));
            Assert.IsTrue(Baskin.DefenseWaves(B).All(x => x.Count == 4));
            Assert.AreEqual(4, End(B).Count);
        }

        [Test]
        public void Schedule_DelayPerAbandonedRoom_OneRoomPerWave()
        {
            var b = new Balance();
            List<Baskin.DefenseWave> S(params int[] starts) =>
                Baskin.DefenseSchedule(b.facilityDefenseFirstWave, b.facilityDefenseWaveTurns, b.facilityDefenseWaves, b.facilityDefenseWaveBase, 1, starts);
            string D(List<Baskin.DefenseWave> w) => string.Join(" ", w.Select(x => $"{x.N}:{x.Room}@{x.Arrive}"));
            Assert.AreEqual("1:1@2 2:1@5 3:1@8", D(S(1)));
            // G'den 3. turda çekildi (A 4'te başlar): dalga 1 G'ye geldi; 2 ve 3 A'ya bir tur gecikmeyle
            Assert.AreEqual("1:1@2 2:2@6 3:2@9", D(S(1, 4)));
            // G'den 1. turda çekildi: dalga 1 henüz gelmemişti, A'ya gelir (3: barikat turu 2 makinesiz)
            Assert.AreEqual("1:2@3 2:2@6 3:2@9", D(S(1, 2)));
            // A'dan da çekildi: H'ye iki tur gecikmeyle; iç odanın ilk turu makinesiz
            Assert.AreEqual("1:2@3 2:3@7 3:3@10", D(S(1, 2, 6)));
            Assert.AreEqual("1:2@3 2:3@7 3:3@10", D(S(1, 2, 5)));
            // A'dan dalga 1 gelmeden (3. turdan önce) çekildi: dalga 1 H'ye (başlangıç 3, geliş 4)
            Assert.AreEqual("1:3@4 2:3@7 3:3@10", D(S(1, 2, 3)));
        }

        // ---------- İç oda, takipçiler, kuyruk ----------
        [Test]
        public void InnerRoom_SingleCounter_TeamFromWest_FollowersBeforeWave()
        {
            var p = DP();
            var reps = new List<RoomReport>();
            var B = First(p);
            Assert.AreEqual(2, End(B, calm: false).Count);
            // Dalga 1 ayakta, pasif; G'den 4. turda çekilinir
            while (B.Turn < 4) { Passive(B); End(B, calm: false); }
            ClearGate(B);
            int foes = Foes(B).Count;
            var types = Foes(B).Select(u => u.Type).ToList();
            var r = PassRoom(p, reps, B);
            Assert.AreEqual("pass", B.Phase);
            Assert.AreEqual(4, B.Turn, "GEÇ turu bitirir, makine evresi yok");
            Assert.AreEqual(4, r.Turns, "tek sayaç");
            Assert.AreEqual(foes, r.Machines.Count);
            var C = Open(p, reps);
            Assert.AreEqual(5, C.Turn, "oda değişince tur sıfırlanmaz");
            Assert.AreEqual(5, Baskin.StartTurn(C));
            Assert.AreEqual(1, Baskin.Abandoned(C));
            Assert.IsTrue(Baskin.BarricadeTurn(C));
            Assert.IsEmpty(Foes(C), "iç odanın ilk turu makinesiz");
            // Ekip batı geçidinden girer
            var team = Standing(C);
            for (int i = 0; i < Math.Min(team.Count, C.Room.West.Count); i++) Assert.IsTrue(Same(C.Room.West[i], new Baskin.Pt(team[i].X, team[i].Y)));
            // Takipçiler 6. turda (başlangıç + 1); dalga 2 de 6'da (5 + 1 gecikme): önce takipçiler
            var wv = Baskin.DefenseWaves(C);
            CollectionAssert.AreEqual(new[] { "2@6", "3@9" }, wv.Select(w => w.N + "@" + w.Arrive));
            var expect = C.Incoming.Where(i => i.Arrive <= 6).Select(i => i.Type).ToList();
            CollectionAssert.AreEqual(types, C.Incoming.Where(i => i.Wave == 0).Select(i => i.Type));
            // 5. turun sonunda tur 6'ya geçer: takipçiler ve dalga 2 aynı tur, önce takipçiler
            var placed = End(C).Select(u => u.Type).ToList();
            Assert.AreEqual(6, C.Turn);
            CollectionAssert.AreEqual(expect, placed, "takipçiler önce, sonra dalga");
            Assert.IsEmpty(C.Incoming.Where(i => i.Arrive <= 6));
            Assert.IsTrue(C.Log.Any(l => l.Text == Loc.F("{0} terk edilen odadan geldi.", Baskin.UnitTypes[types[0]].Name)));
            Assert.IsTrue(C.Log.Any(l => l.Text == Loc.F("Dalga {0}: {1} makine giriş kapısında.", 2, 2)));
        }

        [Test]
        public void Queue_AtMaxEnemies_PlacedLater_AndFollowsOnPass()
        {
            var p = DP();
            var B = First(p);
            // Altı pasif makine (batı girişinde değil): dalga 1'in biri yerleşir, biri kuyrukta kalır
            var spots = FreeTiles(B).Where(t => !B.Room.West.Any(w => Same(w, t)) && t.X >= 1 && t.X <= 4).ToList();
            Assert.GreaterOrEqual(spots.Count, 6);
            for (int i = 0; i < 6; i++) AddFoe(B, spots[i]);
            Assert.AreEqual(1, End(B, calm: false).Count);
            Assert.AreEqual(2, B.Turn);
            var w1 = Baskin.DefenseWaves(B)[0];
            Assert.AreEqual(1, w1.Arrived);
            Assert.AreEqual(1, w1.Queued);
            Passive(B);
            Assert.AreEqual(0, End(B, calm: false).Count, "sınır dolu: kuyrukta kalır");
            Assert.AreEqual(1, B.Incoming.Count(i => i.Wave == 1));
            // Kuyruk tahmini: sınır dolu → karesiz
            var view = Baskin.DefenseIncoming(B);
            Assert.AreEqual(-1, view.First(v => v.Wave == 1).X);
            Assert.AreEqual(B.Turn + 1, view.First(v => v.Wave == 1).Turn);
            // GEÇ: kuyruktaki dalga 1 makinesi takipçi olur (batı girişinin ilk karesiyle); gelmemiş dalgalar düşer
            ClearGate(B);
            int onBoard = Foes(B).Count;
            var reps = new List<RoomReport>();
            var r = PassRoom(p, reps, B);
            Assert.AreEqual(onBoard + 1, r.Machines.Count);
            var last = r.Machines[r.Machines.Count - 1];
            Assert.AreEqual((B.Room.West[0].X, B.Room.West[0].Y), (last.X, last.Y));
            Assert.AreEqual(1, B.Incoming.Count, "dalga 2 ve 3 sonraki odaya yeniden zamanlandı");
            var C = Open(p, reps);
            Assert.AreEqual(onBoard + 1, C.Incoming.Count(i => i.Wave == 0));
            // Bir makine düşünce kuyruk yerleşir
            var D = First(p);
            for (int i = 0; i < 6; i++) AddFoe(D, spots[i]);
            End(D, calm: false);
            Passive(D);
            Foes(D)[0].Dead = true;
            Assert.AreEqual(1, End(D, calm: false).Count);
            Assert.AreEqual(0, D.Incoming.Count(i => i.Wave == 1));
        }

        [Test]
        public void EntryOccupied_NearestFreeTileFromWest()
        {
            var B = First(DP());
            foreach (var w in B.Room.West) AddFoe(B, w);
            // Beklenen: batıdan yürüme uzaklığı en küçük boş zemin (y, x eşitlikte)
            var dist = new int[Baskin.N, Baskin.N];
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) dist[y, x] = -1;
            var q = new Queue<Baskin.Pt>();
            foreach (var w in B.Room.West) { dist[w.Y, w.X] = 0; q.Enqueue(w); }
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in Baskin.Dirs)
                {
                    int x = c.X + d[0], y = c.Y + d[1];
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N || dist[y, x] >= 0) continue;
                    if (Baskin.CellAt(B, x, y).T == "wall" || Baskin.CellAt(B, x, y).O != null) continue;
                    dist[y, x] = dist[c.Y, c.X] + 1;
                    q.Enqueue(new Baskin.Pt(x, y));
                }
            }
            var cands = FreeTiles(B).Where(t => dist[t.Y, t.X] >= 0).OrderBy(t => dist[t.Y, t.X]).ThenBy(t => t.Y).ThenBy(t => t.X).ToList();
            Passive(B);
            End(B, calm: false);
            var at = Spawned(B);
            Assert.AreEqual(2, at.Count);
            Assert.IsTrue(Same(cands[0], at[0]) && Same(cands[1], at[1]), $"{cands[0].X}{cands[0].Y}/{at[0].X}{at[0].Y}");
        }

        // ---------- Kapı ----------
        [Test]
        public void AllAtGate_EndTurnRefused_PassEndsTurn()
        {
            var p = DP();
            var B = First(p);
            ToGate(B);
            Assert.IsTrue(Baskin.CanPass(B));
            Assert.IsFalse(Baskin.CanEndTurn(B));
            Assert.IsFalse(Baskin.EndTurn(B), "bütün ekip kapıdayken tur yalnız GEÇ ile biter");
            Assert.AreEqual(1, B.Turn);
            Assert.IsTrue(Baskin.Pass(B));
            Assert.AreEqual("pass", B.Phase);
            Assert.AreEqual(1, B.Turn);
        }

        [Test]
        public void PartialGate_ReturnsToBoardAtTurnEnd()
        {
            var B = First(DP());
            var u = Standing(B)[0];
            ToGate(B, u);
            Assert.IsTrue(u.AtGate);
            var gate = new Baskin.Pt(u.X, u.Y);
            Assert.IsTrue(Baskin.CanEndTurn(B));
            End(B);
            u = Fresh(B, u);
            Assert.IsFalse(u.AtGate, "savunmada kapı sığınak değildir");
            Assert.IsTrue(Baskin.OnBoard(u));
            Assert.IsTrue(Same(gate, new Baskin.Pt(u.X, u.Y)), "kendi geçit karesine döner");
            Assert.IsTrue(B.FxList.Any(f => f.K == "gate-out" && f.Unit == u.Id));
            // Geçit karesi doluysa en yakın boş kareye (geçit karesi değil)
            var C = First(DP());
            var v = Standing(C)[0];
            ToGate(C, v);
            var g = new Baskin.Pt(v.X, v.Y);
            AddFoe(C, g);
            Assert.IsTrue(Baskin.EndTurn(C));
            v = Fresh(C, v);
            Assert.IsFalse(v.AtGate);
            Assert.IsTrue(Baskin.OnBoard(v));
            Assert.IsFalse(Same(g, new Baskin.Pt(v.X, v.Y)), "geçit karesi dolu");
            Assert.AreEqual(0, Baskin.CellAt(C, v.X, v.Y).Door, "geçit karesine değil");
        }

        [Test]
        public void LastTurn_NoPass_WingHeldWithTeamOnBoard()
        {
            var B = First(DP());
            while (B.Turn < 10) End(B);
            ToGate(B);
            Assert.IsFalse(Baskin.CanPass(B), "son turda içeri çekilinmez");
            Assert.IsFalse(Baskin.Pass(B));
            Assert.IsTrue(Baskin.CanEndTurn(B));
            End(B);
            Assert.AreEqual("won", B.Phase);
            Assert.IsTrue(B.Units.Where(Baskin.IsSpecialist).All(u => !u.AtGate), "kapıdakiler tahtaya döndü");
        }

        [Test]
        public void TargetRoom_NoGate_RelayOnlyThere_CoverAddsOne()
        {
            var p = DP();
            var (reps, H) = Target(p);
            Assert.AreEqual(RoomType.Target, H.Room.Type);
            Assert.AreEqual(2, Baskin.Abandoned(H));
            Assert.AreEqual(3, H.Turn);
            Assert.IsEmpty(Baskin.GateTiles(H));
            Assert.IsFalse(Baskin.CanPass(H));
            Assert.AreEqual(4, Baskin.RelayHp(H));
            Assert.AreEqual(4, Baskin.RelayMax(H));
            Assert.IsEmpty(Baskin.TargetsLeft(H), "röle hedef dolap değildir");
            var t = H.Room.TargetAt.Value;
            Assert.AreEqual(Baskin.RelayKind, Baskin.CellAt(H, t.X, t.Y).O.K);
            // Kapanış (inceleme bulgusu 4): rölenin adı Core'un nesne adlarında; tahtadaki her nesnenin adı var (genel ObjNames[o.K] yolu atmaz)
            Assert.AreEqual("Saha rölesi", Baskin.ObjNames[Baskin.RelayKind]);
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var o = Baskin.CellAt(H, x, y).O;
                    if (o != null) Assert.IsTrue(Baskin.ObjNames.ContainsKey(o.K), "adsız nesne: " + o.K);
                }
            Assert.AreEqual(2, RaidChain.Abandoned(p, reps));
            // Siper: +1
            var pc = DP();
            pc.Perks = new List<string> { FacilityGear.Cover };
            Assert.AreEqual(5, Baskin.RelayHp(Target(pc).B));
            // Giriş ve ara odada röle yok
            Assert.AreEqual(-1, Baskin.RelayHp(First(p)));
        }

        // ---------- Kazanma ve kaybetme ----------
        [Test]
        public void RelayBroken_Loses()
        {
            var (_, H) = Target(DP());
            var t = H.Room.TargetAt.Value;
            Baskin.CellAt(H, t.X, t.Y).O.Hp = 1;
            var tek = Standing(H).First(u => u.Type == "tek");
            var n = FreeNeighbor(H, t);
            Assert.IsNotNull(n);
            Place(tek, n.Value);
            Assert.IsTrue(Baskin.DoAct(H, tek.Id, t.X, t.Y));
            Assert.AreEqual("lost", H.Phase);
            Assert.AreEqual(Loc.T("Saha rölesi kırıldı."), H.OutcomeReason);
            Assert.IsTrue(H.FxList.Any(f => f.K == "relay-break"));
            Assert.AreEqual(-1, Baskin.RelayHp(H));
        }

        [Test]
        public void Lose_TeamDown_And_Retreat()
        {
            var B = First(DP());
            foreach (var u in Standing(B)) Down(B, u);
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual("lost", B.Phase);
            Assert.AreEqual(Loc.T("Ekibin tamamı yere düştü."), B.OutcomeReason);
            var C = First(DP());
            End(C);
            Assert.IsTrue(Baskin.Retreat(C), "savunmada geri çekilme verilebilir");
            Assert.AreEqual("lost", C.Phase);
            Assert.IsTrue(C.Retreated);
            var p = DP();
            var reps = new List<RoomReport>();
            var r = File(p, reps, C);
            Assert.IsTrue(r.Last);
            Assert.IsFalse(r.Success);
            Assert.IsFalse(RaidChain.Result(p, reps).Success);
        }

        [Test]
        public void HeldWithOneStanding()
        {
            var B = First(DP());
            foreach (var u in Standing(B).Skip(1)) Down(B, u);
            while (B.Phase == "player") End(B);
            Assert.AreEqual("won", B.Phase);
        }

        [Test]
        public void CheckWin_NoEscape_IsLoot_False()
        {
            var p = DP(0);
            Assert.AreEqual("loot", p.MissionKind);
            var B = First(p);
            Assert.IsFalse(Baskin.IsLoot(B), "savunmada ikmal kuralı yok");
            Assert.IsTrue(Baskin.IsLoot(First(Params(Wings[0], 41, 1, false))), "saldırıda ikmal");
            // Hedef dolap tahtaya konup kırılsa da kaçış başlamaz
            var tek = Standing(B).First(u => u.Type == "tek");
            var n = FreeNeighbor(B, new Baskin.Pt(tek.X, tek.Y));
            Assert.IsNotNull(n);
            Baskin.CellAt(B, n.Value.X, n.Value.Y).O = new Baskin.Obj { K = "node", Hp = 1, Max = 1 };
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, n.Value.X, n.Value.Y));
            Assert.IsNull(Baskin.CellAt(B, n.Value.X, n.Value.Y).O);
            Assert.IsFalse(B.Escape, "CheckWin savunmada kaçış başlatmaz");
            Assert.AreEqual("player", B.Phase);
        }

        // ---------- Şart, uyanma ----------
        [Test]
        public void EventRoom_NoSchedule_PartsInPlace_NoWake()
        {
            int w = Wings.FindIndex(x => x[1].Event != null);
            Assert.GreaterOrEqual(w, 0);
            var p = DP(w, tier: 3, events: true);
            var b = Bal(events: true);
            var reps = new List<RoomReport>();
            PassRoom(p, reps, First(p, b));
            var C = Open(p, reps, b);
            Assert.IsNull(C.Ev, "şart takvimi ve tetikler yok");
            var plan = C.Room.PlanCells(3, true);
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = C.Cells[y, x];
                    Assert.AreEqual(plan[y, x].T, c.T, $"{x},{y}");
                    Assert.AreEqual(plan[y, x].O?.K, c.O?.K, $"{x},{y}");
                }
            int made = 0;
            while (C.Phase == "player") made += End(C).Count;
            Assert.AreEqual("won", C.Phase);
            Assert.AreEqual(Baskin.DefenseWaves(C).Sum(x => x.Count), made, "uyanma yok: makineler yalnız dalga");
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++) Assert.AreEqual(plan[y, x].T, C.Cells[y, x].T, "tahta yapısı değişmez");
        }

        // ---------- Makine aklı ----------
        [Test]
        public void MachineAi_PrefersRelay_AndWalksToIt()
        {
            var (_, H) = Target(DP());
            var t = H.Room.TargetAt.Value;
            // Röleye bitişik dron röleye nişan alır: 11; sağlam EMP uzmanına 10
            var n = FreeNeighbor(H, t);
            Assert.IsNotNull(n);
            var dron = AddFoe(H, n.Value);
            int toRelay = Array.FindIndex(Baskin.Dirs, d => n.Value.X + d[0] == t.X && n.Value.Y + d[1] == t.Y);
            Assert.AreEqual(11.0, Baskin.TestScore(H, dron, "shoot", toRelay));
            var emp = Standing(H).First(u => u.Type == "emp");
            emp.Hp = emp.Max = 3;
            var free = Enumerable.Range(0, 4).Where(d => d != toRelay && FreeAt(H, n.Value.X + Baskin.Dirs[d][0], n.Value.Y + Baskin.Dirs[d][1])).ToList();
            Assert.IsNotEmpty(free);
            Place(emp, new Baskin.Pt(n.Value.X + Baskin.Dirs[free[0]][0], n.Value.Y + Baskin.Dirs[free[0]][1]));
            Assert.AreEqual(10.0, Baskin.TestScore(H, dron, "shoot", free[0]));
            // Patlama alanındaki röle +6
            var bomba = AddFoe(H, new Baskin.Pt(0, 0), "bomba");
            Place(bomba, n.Value);
            dron.Dead = true;
            double withRelay = Baskin.TestScore(H, bomba, "blast", 0);
            var keep = Baskin.CellAt(H, t.X, t.Y).O;
            Baskin.CellAt(H, t.X, t.Y).O = null;
            double without = Baskin.TestScore(H, bomba, "blast", 0);
            Baskin.CellAt(H, t.X, t.Y).O = keep;
            Assert.AreEqual(6.0, withRelay - without, 1e-9);
            bomba.Dead = true;
            // Ekip yokken (tahtada uzman yok) bakım robotu röleye yürür
            foreach (var u in H.Units.Where(Baskin.IsSpecialist)) { u.Dead = true; u.Hp = 0; }
            var far = FreeTiles(H).Where(x => Math.Abs(x.X - t.X) + Math.Abs(x.Y - t.Y) >= 4).OrderByDescending(x => Math.Abs(x.X - t.X) + Math.Abs(x.Y - t.Y)).ToList();
            Assert.IsNotEmpty(far);
            var bot = AddFoe(H, far[0], "bakim");
            int d0 = Math.Abs(bot.X - t.X) + Math.Abs(bot.Y - t.Y);
            int best = Baskin.Reachable(H, bot).Select(x => Math.Abs(x.X - t.X) + Math.Abs(x.Y - t.Y)).DefaultIfEmpty(d0).Min();
            Assert.Less(best, d0, "röleye yaklaşan kare var");
            Baskin.TestPlan(H);
            Assert.AreEqual(best, Math.Abs(bot.X - t.X) + Math.Abs(bot.Y - t.Y), "röleye en çok yaklaşan kareye yürür (ilerleme terimi)");
        }

        // ---------- RNG ----------
        [Test]
        public void WaveTypes_SameStreamWhicheverRoom()
        {
            var p = DP(seed: 77, tier: 3);
            var B = First(p);
            var room1 = B.Incoming.Where(i => i.Wave == 1).Select(i => i.Type).ToList();
            var reps = new List<RoomReport>();
            PassRoom(p, reps, B);
            var C = Open(p, reps);
            Assert.AreEqual(1, Baskin.DefenseWaves(C)[0].N, "dalga 1 A'ya geldi");
            CollectionAssert.AreEqual(room1, C.Incoming.Where(i => i.Wave == 1).Select(i => i.Type));
            // Başka tohum başka türler (en az bir dalgada)
            var q = DP(seed: 78, tier: 3);
            var E = First(q);
            Assert.AreNotEqual(string.Join(",", B.Incoming.Select(i => i.Type)), string.Join(",", E.Incoming.Select(i => i.Type)));
        }

        [Test]
        public void BoardDraws_OnlyMachinePlan()
        {
            var B = First(DP());
            End(B, calm: false);
            for (int k = 0; k < 4 && B.Phase == "player"; k++)
            {
                int rs = B.Rs;
                End(B, calm: false);
                if (B.Phase != "player") break;
                int n = B.Units.Count(u => u.Side == 'e' && !u.Dead && u.Intent != null);
                Assert.AreEqual(unchecked((uint)(n * 0x6D2B79F5)), unchecked((uint)(B.Rs - rs)), "çekim = planlanan makine (t" + B.Turn + ")");
            }
        }

        // ---------- Geri alma ve kayıttan devam ----------
        [Test]
        public void Undo_RestoresDefenseState()
        {
            var p = DP();
            var reps = new List<RoomReport>();
            var B = First(p);
            End(B, calm: false);
            Passive(B);
            ClearGate(B);
            PassRoom(p, reps, B);
            var C = Open(p, reps);
            Assert.IsNotEmpty(C.Incoming);
            var u = Standing(C)[0];
            var to = Baskin.Reachable(C, u).First(t => !Baskin.IsGateTile(C, t.X, t.Y));
            AssertUndoRestores(C, s => Baskin.DoMove(s, u.Id, to.X, to.Y), "yürüyüş");
            var v = Standing(C)[1];
            var g = C.Room.Forward[0];
            Place(v, g);
            AssertUndoRestores(C, s => Baskin.EnterGate(s, v.Id), "kapı");
        }

        [Test]
        public void CopyInto_CoversEveryField_DefenseBoard()
        {
            var p = DP();
            var reps = new List<RoomReport>();
            var B = First(p);
            End(B, calm: false);
            Passive(B);
            ClearGate(B);
            PassRoom(p, reps, B);
            var C = Open(p, reps);
            var u = Standing(C)[0];
            var to = Baskin.Reachable(C, u).First(t => !Baskin.IsGateTile(C, t.X, t.Y));
            string before = FullDump(C);
            Assert.IsTrue(Baskin.DoMove(C, u.Id, to.X, to.Y));
            int changed = 0;
            foreach (var f in typeof(Baskin.BState).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (f.Name == "Hist" || f.Name == "Phase" || f.IsInitOnly) continue;
                var val = f.GetValue(C);
                if (f.FieldType == typeof(int)) f.SetValue(C, (int)val + 7);
                else if (f.FieldType == typeof(bool)) f.SetValue(C, !(bool)val);
                else if (f.FieldType == typeof(string)) f.SetValue(C, (string)val + "x");
                else if (f.FieldType == typeof(double)) f.SetValue(C, (double)val + 1);
                else if (!f.FieldType.IsValueType) f.SetValue(C, null);
                else Assert.Fail("sınanmayan alan türü: " + f.Name);
                changed++;
            }
            Assert.GreaterOrEqual(changed, 44);
            Assert.IsTrue(Baskin.Undo(C));
            Assert.AreEqual(before, FullDump(C), "CopyInto bir alanı atlıyor");
        }

        [Test]
        public void Resume_SameBoardFromDecodedReports()
        {
            var p = DP(seed: 5, tier: 2);
            var reps = new List<RoomReport>();
            var B = First(p);
            End(B, calm: false);
            Passive(B);
            End(B, calm: false);
            ClearGate(B);
            PassRoom(p, reps, B);
            var decoded = reps.Select(r => RoomReport.Decode(r.Encode())).ToList();
            var C1 = Open(p, reps);
            var C2 = Open(p, decoded);
            Assert.AreEqual(Baskin.Signature(C1), Baskin.Signature(C2));
            StringAssert.Contains("|V4:1:", Baskin.Signature(C1));
            // İki tahta aynı oynanır
            End(C1); End(C2);
            Assert.AreEqual(Baskin.Signature(C1), Baskin.Signature(C2));
        }

        // ---------- Gövde kancası ----------
        [Test]
        public void IsExtracted_DefenseHook()
        {
            var p = DP();
            var a = new RaidParams();
            RoomMember M(string st, int body, int fell) => new RoomMember { State = st, Body = body, Fell = fell };
            var won = new RoomReport { Room = 2, End = "won", Success = true, Last = true };
            var lost = new RoomReport { Room = 2, End = "lost", Success = false, Last = true };
            Assert.IsTrue(RaidChain.IsExtracted(p, won, M("d", 0, 2)), "ekibin odasında yatan");
            Assert.IsTrue(RaidChain.IsExtracted(p, won, M("d", 1, 1)), "ekiple gelen");
            Assert.IsFalse(RaidChain.IsExtracted(p, won, M("d", 0, 1)), "terk edilen odada yatan");
            Assert.IsFalse(RaidChain.IsExtracted(p, won, M("a", 0, 0)));
            Assert.IsFalse(RaidChain.IsExtracted(p, lost, M("d", 0, 2)), "kayıpta kimse");
            Assert.IsFalse(RaidChain.IsExtracted(p, lost, M("d", 1, 1)));
            // Saldırı dalı aynen: yalnız Body 2
            Assert.IsFalse(RaidChain.IsExtracted(a, won, M("d", 0, 2)));
            Assert.IsTrue(RaidChain.IsExtracted(a, won, M("d", 2, 2)));
            // Uçtan uca: A'da düşen uzman kazanılan savunmada çıkarılır, G'de bırakılan değil
            var reps = new List<RoomReport>();
            var B = First(p);
            var left = Standing(B)[2];
            Down(B, left);
            PassRoom(p, reps, B);
            var C = Open(p, reps);
            var fallen = Standing(C)[1];
            Down(C, fallen);
            while (C.Phase == "player") End(C);
            Assert.AreEqual("won", C.Phase);
            File(p, reps, C);
            var res = RaidChain.Result(p, reps);
            CollectionAssert.AreEqual(new[] { fallen.Slot }, res.Extracted);
            Assert.AreEqual(2, res.Down);
        }
    }
}
