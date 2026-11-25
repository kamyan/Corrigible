using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 10, parça A (Docs/TESIS-BASKINLARI-TASARIMI.md §6.3; uygulama tasarımı §1.4–1.6, §2, §6.1): önlem defterinin tahtası.
    /// Sınıf sayaçları (sayım noktaları, kademe 2 ve önlemsiz emir, geri alma, önizleme, makine evresi, pusu anahtarı, oda raporu), altı
    /// önlem (cıvatalı ayak, çift cidar, kesici kol, kısa bağ, koku yayıcı, parazit ve makine aklı) ve beş cevabın tahtadaki etkisi,
    /// kaplamanın yeri (plan = tahta, tohumdan bağımsız) ve dönüş tahtası. Kademeler emirden
    /// (<see cref="RaidParams.Counters"/>, <see cref="RaidParams.Answers"/>) oda zinciriyle iner. Olay nişanı (A0) RaidEventTests'te.
    /// </summary>
    public class FacilityCountermeasureTests
    {
        static Balance B0 => Bal(events: false);

        static Baskin.Pt Pt(int x, int y) => new Baskin.Pt(x, y);

        /// <summary>Kademeler, sınıf sırasıyla (itme, yıkım, örme, ele, köpek, aygıt).</summary>
        static List<int> Lv(int itme = 0, int yikim = 0, int orme = 0, int ele = 0, int kopek = 0, int aygit = 0) =>
            new List<int> { itme, yikim, orme, ele, kopek, aygit };

        /// <summary>Kanadın olaysız baskın değerleri + emirdeki kademeler ve cevaplar.</summary>
        static RaidParams P(List<int> counters, string[] answers = null, int wing = 0, int seed = 41, bool events = false)
        {
            var p = Params(Wings[wing], seed, 1, events);
            p.Counters = counters;
            p.Answers = answers?.ToList() ?? new List<string>();
            return p;
        }

        /// <summary>G'nin tahtası düz zeminde (makinesiz): Teknisyen, EMP uzmanı ve Analist alt sırada.</summary>
        static Baskin.BState FlatB(List<int> counters, string[] answers = null, Balance b = null)
        {
            var B = Open(P(counters, answers), new List<RoomReport>(), b ?? B0);
            Flat(B, Pt(0, 7), Pt(1, 7), Pt(2, 7));
            return B;
        }

        /// <summary>Kanadın odası (gidiş, ilk ziyaret) emirdeki değerlerle; kurulum oda zincirinin (<see cref="RaidChain.Setup"/>).</summary>
        static Baskin.BState RoomOf(RaidParams p, FacilityRoom room, Balance b = null) =>
            Baskin.CreateRoom(p, room, RaidChain.Setup(b ?? B0, room, p, new List<RoomReport>(), false));

        static List<Baskin.Pt> Doubles(Baskin.Cell[,] cells)
        {
            var o = new List<Baskin.Pt>();
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (cells[y, x].Double) o.Add(Pt(x, y));
            return o;
        }

        static List<Baskin.Pt> Emitters(Baskin.Cell[,] cells)
        {
            var o = new List<Baskin.Pt>();
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) if (cells[y, x].O?.K == "yayici") o.Add(Pt(x, y));
            return o;
        }

        static string Key(IEnumerable<Baskin.Pt> pts) => string.Join(",", pts.Select(t => t.X + "." + t.Y));

        static Baskin.Obj Obj(string k, int hp) => new Baskin.Obj { K = k, Hp = hp, Max = hp };

        /// <summary>Kare boş zemin: duvar, nesne, birim, kapı ve menfez yok.</summary>
        static bool HasObj(Baskin.BState B, Baskin.Pt t) =>
            t.X >= 0 && t.Y >= 0 && t.X < Baskin.N && t.Y < Baskin.N && Baskin.CellAt(B, t.X, t.Y).O != null;

        static bool Clear(Baskin.BState B, Baskin.Pt t) =>
            t.X >= 0 && t.Y >= 0 && t.X < Baskin.N && t.Y < Baskin.N && Baskin.CellAt(B, t.X, t.Y).T == "floor" && Baskin.CellAt(B, t.X, t.Y).O == null
            && Baskin.UnitAt(B, t.X, t.Y) == null && Baskin.CellAt(B, t.X, t.Y).Door == 0 && Baskin.CellAt(B, t.X, t.Y).Vent == 0;

        /// <summary>
        /// Eylem önizlemesi uygulamayla aynı ve asıl durumu değiştirmez; uygulanınca yalnız <paramref name="cls"/> sayacı bir artar; geri
        /// alma tam dökümü ve imzayı (sayaç dahil) geri getirir.
        /// </summary>
        static void Counts(Baskin.BState B, Baskin.Unit u, string kind, Baskin.Pt at, string cls)
        {
            string dump = FullDump(B), sig = Baskin.Signature(B);
            var before = B.TallyArray();
            ActPreviewed(B, u, kind, at);
            var after = B.TallyArray();
            int c = FacilityCounters.Index(cls);
            for (int i = 0; i < before.Length; i++) Assert.AreEqual(before[i] + (i == c ? 1 : 0), after[i], kind + ": " + FacilityCounters.Classes[i]);
            StringAssert.Contains("|T", Baskin.Signature(B), "sayaç imzada");
            Assert.IsTrue(Baskin.Undo(B), "geri alma: " + kind);
            Assert.AreEqual(sig, Baskin.Signature(B), "imza: " + kind);
            Assert.AreEqual(dump, FullDump(B), "tam döküm: " + kind);
        }

        // ---------- Sayım ----------
        [Test]
        public void Tally_EachCountPoint_PreviewAndUndo()
        {
            // itme: koşu darbesi
            var B = FlatB(Lv());
            Assert.IsTrue(B.Moves.Tally, "emirde kademe var: sayım açık");
            var tek = BySlot(B, 0);
            Place(tek, Pt(0, 3));
            AddMachine(B, "dron", Pt(4, 3), 5);
            Counts(B, tek, Baskin.ActRush, Pt(4, 3), FacilityCounters.Push);
            // yıkım: lokum
            B = FlatB(Lv());
            tek = BySlot(B, 0);
            Place(tek, Pt(3, 3));
            Gear(tek, FacilityGear.Charge, 2);
            B.Cells[3, 4].T = "wall";
            Counts(B, tek, Baskin.ActCharge, Pt(4, 3), FacilityCounters.Breach);
            // örme: tel ve kalas birer
            foreach (var kind in new[] { Baskin.ActWire, Baskin.ActPlank })
            {
                B = FlatB(Lv());
                tek = BySlot(B, 0);
                Place(tek, Pt(3, 3));
                Gear(tek, FacilityGear.Wire, 2);
                Counts(B, tek, kind, Pt(4, 3), FacilityCounters.Build);
            }
            // ele: kelepçe
            B = FlatB(Lv());
            tek = BySlot(B, 0);
            Place(tek, Pt(3, 3));
            Gear(tek, FacilityGear.Clamp, 1);
            AddMachine(B, "dron", Pt(4, 3));
            Counts(B, tek, Baskin.ActClamp, Pt(4, 3), FacilityCounters.Capture);
            // köpek: havla, getir, kokla
            B = FlatB(Lv());
            var d = AddDog(B, 0, Pt(3, 3));
            Counts(B, d, Baskin.ActBark, Pt(3, 3), FacilityCounters.Dog);
            B.CargoX = 4; B.CargoY = 3;
            Counts(B, Fresh(B, d), Baskin.ActFetch, Pt(4, 3), FacilityCounters.Dog);
            var f = B.Room.Forward[0];
            d = Fresh(B, d);
            Place(d, f);
            Counts(B, d, Baskin.ActSniff, f, FacilityCounters.Dog);
            // aygıt: taret, yem, vinç
            foreach (var (gear, kind) in new[] { (FacilityGear.Turret, Baskin.ActTurret), (FacilityGear.Decoy, Baskin.ActDecoy), (FacilityGear.Winch, Baskin.ActWinch) })
            {
                B = FlatB(Lv());
                tek = BySlot(B, 0);
                Place(tek, Pt(3, 3));
                Gear(tek, gear, 1);
                Counts(B, tek, kind, Pt(4, 3), FacilityCounters.Gadget);
            }
        }

        /// <summary>Kademe 2'deki sınıf sayılmaz; emirde kademe yoksa (önlem defteri kapalı) hiçbir sayaç artmaz.</summary>
        [Test]
        public void Tally_Level2AndNoCounters_NotCounted()
        {
            foreach (var counters in new[] { Lv(itme: 2), new List<int>(), Lv().Take(5).ToList() })
            {
                var B = FlatB(counters);
                Assert.AreEqual(counters.Count == 6, B.Moves.Tally);
                var tek = BySlot(B, 0);
                Place(tek, Pt(0, 3));
                AddMachine(B, "dron", Pt(4, 3), 5);
                Assert.IsTrue(Baskin.DoAction(B, tek.Id, Baskin.ActRush, 4, 3));
                CollectionAssert.AreEqual(new int[6], B.TallyArray(), "sayılmaz: " + counters.Count);
                StringAssert.DoesNotContain("|T", Baskin.Signature(B));
            }
            var G = FlatB(Lv(aygit: 2));
            var u = BySlot(G, 0);
            Place(u, Pt(3, 3));
            Gear(u, FacilityGear.Turret, 1);
            Assert.IsTrue(Baskin.DoAction(G, u.Id, Baskin.ActTurret, 4, 3));
            Assert.AreEqual(0, G.TallyGadget, "aygıt 2. kademede sayılmaz");
        }

        /// <summary>Vinç yalnız makine çekişini itme sayar (makine evresi); gövde ve ekip çekişi kurtarmadır.</summary>
        [Test]
        public void Tally_Winch_MachinePullCounts_BodyAndTeamDoNot()
        {
            var B = FlatB(Lv());
            B.StillTurns = 99;
            AddGadget(B, "vinc", Pt(3, 3), new[] { 1, 0 });
            var m = AddMachine(B, "dron", Pt(6, 3));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((5, 3), (m.X, m.Y), "makine çekildi");
            Assert.AreEqual(1, B.TallyPush);
            foreach (bool dead in new[] { true, false })
            {
                B = FlatB(Lv());
                B.StillTurns = 99;
                AddGadget(B, "vinc", Pt(3, 3), new[] { 1, 0 });
                var v = BySlot(B, 1);
                Place(v, Pt(6, 3));
                if (dead) Down(B, v);
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual((5, 3), (v.X, v.Y), dead ? "gövde çekildi" : "uzman çekildi");
                CollectionAssert.AreEqual(new int[6], B.TallyArray(), dead ? "gövde sayılmaz" : "ekip sayılmaz");
            }
        }

        /// <summary>Pusu isabeti yalnız ölçüm anahtarıyla (<c>facilityTallyAmbush</c>) "aygıt" sayılır (K6).</summary>
        [Test]
        public void Tally_AmbushHit_OnlyWithMeasurementKey()
        {
            foreach (bool key in new[] { false, true })
            {
                var b = Bal(events: false);
                b.facilityTallyAmbush = key;
                var B = FlatB(Lv(), b: b);
                Assert.AreEqual(key, B.Moves.TallyAmbush);
                B.StillTurns = 99;
                var emp = BySlot(B, 1);
                Place(emp, Pt(1, 3));
                AddMachine(B, "dron", Pt(4, 3), 5);
                Assert.IsTrue(Baskin.DoAction(B, emp.Id, Baskin.ActAmbush, 2, 3));
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.IsTrue(B.FxList.Any(x => x.K == "ambush-hit"), "pusu vurdu");
                Assert.AreEqual(key ? 1 : 0, B.TallyGadget);
            }
        }

        // ---------- Oda raporu ----------
        [Test]
        public void Report_Tally_RoundTrip_OldReportsZero_UnboltedPart()
        {
            var r = new RoomReport
            {
                Room = 1, End = "pass", Tally = new List<int> { 1, 0, 2, 0, 0, 3 },
                Machines = new List<RoomMachine> { new RoomMachine { Type = "zdron", X = 5, Y = 3, Hp = 2, Unbolted = true }, new RoomMachine { Type = "dron", X = 6, Y = 1, Hp = 2 } },
            };
            string s = r.Encode();
            StringAssert.EndsWith("|1:0:2:0:0:3", s);
            StringAssert.Contains("zdron:5:3:2:0:1,dron:6:1:2:0", s);
            var d = RoomReport.Decode(s);
            CollectionAssert.AreEqual(new[] { 1, 0, 2, 0, 0, 3 }, d.Tally);
            Assert.IsTrue(d.Machines[0].Unbolted);
            Assert.IsFalse(d.Machines[1].Unbolted);
            Assert.AreEqual(s, d.Encode());
            Assert.AreEqual(22, s.Split('|').Length, "kuyruk alanı 21");
            // Sayaçsız rapor: dizgi aynı kalır (eski 15 alan), çözülünce altı sıfır
            var old = new RoomReport { Room = 2, End = "pass", CargoDog = true };
            string o = old.Encode();
            Assert.AreEqual(21, o.Split('|').Length, "sıfır sayaç kuyruğa yazılmaz");
            CollectionAssert.AreEqual(new int[6], RoomReport.Decode(o).Tally);
            CollectionAssert.AreEqual(new int[6], RoomReport.Decode(new RoomReport { Room = 1 }.Encode()).Tally);
            Assert.AreEqual(15, new RoomReport { Room = 1, Tally = new List<int> { 0, 0, 0, 0, 0, 0 } }.Encode().Split('|').Length);
            // Baskın sayımı: tavanlı toplam
            var two = new[] { new RoomReport { Tally = new List<int> { 2, 0, 0, 1, 0, 5 } }, new RoomReport { Tally = new List<int> { 2, 1 } }, new RoomReport() };
            CollectionAssert.AreEqual(new[] { 3, 1, 0, 1, 0, 3 }, FacilityCounters.RaidTally(two, 3));
            CollectionAssert.AreEqual(new int[6], FacilityCounters.RaidTally(two, -1));
        }

        /// <summary>Oda zinciri tahtanın sayaçlarını rapora yazar (geçilen odanın raporu).</summary>
        [Test]
        public void Report_FromBoard_CarriesTally()
        {
            var p = P(Lv());
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B);
            var tek = BySlot(B, 0);
            var line = FindLine(B, 5);
            Assert.IsNotNull(line);
            Place(tek, line.Value.At);
            AddMachine(B, "dron", Step(line.Value.At, line.Value.D, 4), 9);
            Assert.IsTrue(Baskin.DoAction(B, tek.Id, Baskin.ActRush, Step(line.Value.At, line.Value.D, 4).X, Step(line.Value.At, line.Value.D, 4).Y));
            var r = PassRoom(p, reps, B);
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 0, 0 }, r.Tally);
            StringAssert.EndsWith("|1:0:0:0:0:0", r.Encode());
        }

        // ---------- itme · Cıvatalı ayak ----------
        [Test]
        public void Bolt_Sabotage_HurtsButDoesNotMove_DronIsPushed()
        {
            foreach (var type in new[] { "zdron", "dron" })
            {
                var B = FlatB(Lv(itme: 1));
                var tek = BySlot(B, 0);
                Place(tek, Pt(3, 3));
                var z = AddMachine(B, type, Pt(4, 3), 3);
                bool bolted = type != "dron";
                Assert.AreEqual(bolted, Baskin.Bolted(B, z));
                var pv = Baskin.Preview(B, tek.Id, 4, 3);
                Assert.IsTrue(Baskin.DoAct(B, tek.Id, 4, 3));
                CollectionAssert.AreEqual(pv.Fx.Select(f => f.K + f.X + f.Y), B.FxList.Select(f => f.K + f.X + f.Y), "önizleme = uygulama");
                Assert.AreEqual(2, z.Hp, "asıl hasar işler");
                Assert.AreEqual(bolted ? (4, 3) : (5, 3), (z.X, z.Y));
                Assert.AreEqual(bolted, B.FxList.Any(f => f.K == "bolted"));
                Assert.AreEqual(!bolted, B.FxList.Any(f => f.K == "push"));
            }
        }

        [Test]
        public void Bolt_Emp_SkipsBolted_PushesDron()
        {
            var B = FlatB(Lv(itme: 1));
            var emp = BySlot(B, 1);
            Place(emp, Pt(4, 0));
            var z = AddMachine(B, "havan", Pt(5, 3));
            var d = AddMachine(B, "dron", Pt(3, 3));
            Assert.IsTrue(Baskin.DoAct(B, emp.Id, 4, 3));
            Assert.AreEqual((5, 3), (z.X, z.Y), "cıvatalı yerinde");
            Assert.AreEqual((2, 3), (d.X, d.Y), "dron itildi");
            Assert.AreEqual(3, z.Hp, "çarpma hasarı yok");
        }

        [Test]
        public void Bolt_RushHookWinch_DoNotMove()
        {
            // Koşu darbesi: cıvatalı hedef değil; dron ve sökülen makine hedef
            var B = FlatB(Lv(itme: 1));
            var tek = BySlot(B, 0);
            Place(tek, Pt(0, 5));
            var z = AddMachine(B, "kilit", Pt(4, 5));
            Assert.IsFalse(Has(Baskin.ActionTargets(B, tek, Baskin.ActRush), Pt(4, 5)), "cıvatalı koşu hedefi değil");
            z.Unbolted = true;
            Assert.IsTrue(Has(Baskin.ActionTargets(B, tek, Baskin.ActRush), Pt(4, 5)), "sökülen hedef");
            z.Unbolted = false; z.Type = "dron";
            Assert.IsTrue(Has(Baskin.ActionTargets(B, tek, Baskin.ActRush), Pt(4, 5)), "dron hedef");
            // Kanca: hasar 1, çekiş yok
            B = FlatB(Lv(itme: 1));
            var ana = BySlot(B, 2);
            Place(ana, Pt(0, 4));
            z = AddMachine(B, "zdron", Pt(4, 4), 3);
            Assert.IsTrue(Baskin.DoAct(B, ana.Id, 4, 4));
            Assert.AreEqual((4, 4, 2), (z.X, z.Y, z.Hp));
            Assert.IsTrue(B.FxList.Any(f => f.K == "bolted"));
            Assert.AreEqual((0, 4), (ana.X, ana.Y), "Analist yerinde");
            B = FlatB(Lv(itme: 1));
            ana = BySlot(B, 2);
            Place(ana, Pt(0, 4));
            z = AddMachine(B, "dron", Pt(4, 4), 3);
            Assert.IsTrue(Baskin.DoAct(B, ana.Id, 4, 4));
            Assert.AreEqual((3, 4), (z.X, z.Y), "dron çekildi");
            // Vinç: cıvatalı çekilmez, hat orada biter
            B = FlatB(Lv(itme: 1));
            B.StillTurns = 99;
            AddGadget(B, "vinc", Pt(3, 3), new[] { 1, 0 });
            z = AddMachine(B, "bomba", Pt(6, 3));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((6, 3), (z.X, z.Y));
            Assert.IsTrue(B.FxList.Any(f => f.K == "bolted" && f.Unit == z.Id));
            Assert.AreEqual(0, B.TallyPush, "çekilmeyen makine sayılmaz");
            // Okuyucu (aşama 10 E; sunumun vinç önizlemesi bunu okur): cıvatalı ve aygıt çekilmez, sökülen ve dron çekilir
            Assert.IsFalse(Baskin.WinchPulls(B, z), "cıvatalı");
            Assert.IsFalse(Baskin.WinchPulls(B, B.Units.First(u => Baskin.IsGadget(u))), "aygıt");
            Assert.IsFalse(Baskin.WinchPulls(B, null));
            z.Unbolted = true;
            Assert.IsTrue(Baskin.WinchPulls(B, z), "sökülen");
            z.Unbolted = false; z.Type = "dron";
            Assert.IsTrue(Baskin.WinchPulls(B, z), "dron");
        }

        /// <summary>Bakım robotu kademe 1'de itilir, 2'de itilmez; tutsak cıvatasız, bırakılınca yeniden cıvatalı.</summary>
        [Test]
        public void Bolt_MaintenanceOnlyAtLevel2_CaptiveUnbolted()
        {
            foreach (int level in new[] { 1, 2 })
            {
                var B = FlatB(Lv(itme: level));
                var tek = BySlot(B, 0);
                Place(tek, Pt(3, 3));
                var m = AddMachine(B, "bakim", Pt(4, 3));
                Assert.AreEqual(level >= 2, Baskin.Bolted(B, m));
                Assert.IsTrue(Baskin.DoAct(B, tek.Id, 4, 3));
                Assert.AreEqual(level >= 2 ? (4, 3) : (5, 3), (m.X, m.Y));
            }
            var C = FlatB(Lv(itme: 1));
            var c = Captive(AddMachine(C, "zdron", Pt(4, 3)));
            Assert.IsFalse(Baskin.Bolted(C, c), "tutsak cıvatasız");
            c.Side = 'e'; c.Class = null;
            Assert.IsTrue(Baskin.Bolted(C, c), "bırakılan yeniden cıvatalı");
        }

        /// <summary>Cıvatalı makine yağda kaymaz (isabet yönünde); cıvatasız kayar.</summary>
        [Test]
        public void Bolt_OilSlide_None()
        {
            foreach (var type in new[] { "zdron", "dron" })
            {
                var B = FlatB(Lv(itme: 1));
                B.Smart = false;
                B.StillTurns = 99;
                B.Ev = new Baskin.EvState { Id = "yag", P = Pt(7, 0), Area = Enumerable.Range(2, 5).Select(x => Pt(x, 3)).ToList() };
                for (int x = 2; x <= 6; x++) B.Cells[3, x].Oil = true;
                var shooter = AddMachine(B, "dron", Pt(1, 3));
                shooter.Intent = new Baskin.Intent { Kind = "shoot", Dir = DirOf(new[] { 1, 0 }) };
                shooter.Order = 1;
                var v = AddMachine(B, type, Pt(4, 3), 5);
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual(4, v.Hp, "vuruldu");
                Assert.AreEqual(type == "dron" ? (7, 3) : (4, 3), (v.X, v.Y));
            }
        }

        /// <summary>Söküm anahtarı (cevap): Teknisyen'in sabotajı cıvatayı söker ve aynı vuruşta iter; geri alma tam durumu getirir.</summary>
        [Test]
        public void Sokum_Unbolts_AndPushes_Undo()
        {
            var B = FlatB(Lv(itme: 1), new[] { FacilityGear.AnsSokum });
            Assert.IsTrue(B.Moves.Unbolt);
            var tek = BySlot(B, 0);
            Place(tek, Pt(3, 3));
            var z = AddMachine(B, "zdron", Pt(4, 3), 3);
            AssertUndoRestores(B, s => Baskin.DoAct(s, tek.Id, 4, 3), "söküm");
            z = Fresh(B, z);
            Assert.IsFalse(z.Unbolted);
            var pv = Baskin.Preview(B, tek.Id, 4, 3);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 4, 3));
            z = Fresh(B, z);
            CollectionAssert.AreEqual(pv.Fx.Select(f => f.K), B.FxList.Select(f => f.K));
            Assert.IsTrue(z.Unbolted);
            Assert.IsFalse(Baskin.Bolted(B, z));
            Assert.AreEqual((5, 3), (z.X, z.Y), "aynı vuruşta itildi");
            var ks = B.FxList.Select(f => f.K).ToList();
            Assert.Less(ks.IndexOf("unbolt"), ks.IndexOf("push"));
            StringAssert.Contains("zdron53:2U", Baskin.Signature(B));
            // Söküm'süz sabotaj sökmez
            var C = FlatB(Lv(itme: 1));
            var t2 = BySlot(C, 0);
            Place(t2, Pt(3, 3));
            var z2 = AddMachine(C, "zdron", Pt(4, 3), 3);
            Assert.IsTrue(Baskin.DoAct(C, t2.Id, 4, 3));
            Assert.IsFalse(z2.Unbolted);
        }

        // ---------- yikim · Çift cidar ----------
        /// <summary>Kademe 1'de G ve A'da iki çift duvar, H'de yok; kademe 2'de H'de de. Seçim tohumdan bağımsız, plan = tahta.</summary>
        [Test]
        public void DoubleWall_PerRoomAndLevel_SeedIndependent_PlanEqualsBoard()
        {
            int full = 0, rooms = 0;
            foreach (bool events in new[] { false, true })
                for (int w = 0; w < Wings.Count; w++)
                    foreach (var room in Wings[w])
                    {
                        var b = Bal(events);
                        var c1 = RoomOf(P(Lv(yikim: 1), wing: w, events: events), room, b);
                        var c2 = RoomOf(P(Lv(yikim: 2), wing: w, seed: 97, events: events), room, b);
                        var d1 = Doubles(c1.Cells);
                        var d2 = Doubles(c2.Cells);
                        if (room.Type == RoomType.Target) Assert.IsEmpty(d1, room.Id + ": hedef odada kademe 1 yok");
                        else Assert.AreEqual(Key(d1), Key(d2), room.Id + ": kademe 2 aynı seçim (tohumdan bağımsız)");
                        Assert.LessOrEqual(d2.Count, b.facilityDoubleWalls);
                        rooms++;
                        if (d2.Count == b.facilityDoubleWalls) full++;
                        foreach (var t in d2)
                        {
                            var c = c2.Cells[t.Y, t.X];
                            Assert.AreEqual("wall", c.T);
                            Assert.IsFalse(c.Shutter || c.Crate, room.Id);
                        }
                        // Plan = tahta (bina planının kopyası aynı kaplamayı uygular)
                        bool ev = events && room.Event != null;
                        var plan = room.PlanCells(1, ev);
                        Baskin.ApplyCounterCells(plan, room, ev, Lv(yikim: 2), b);
                        Assert.AreEqual(Key(d2), Key(Doubles(plan)), room.Id + ": plan = tahta");
                        // Önlemsiz tahtada çift duvar yok
                        Assert.IsEmpty(Doubles(RoomOf(P(Lv(), wing: w, events: events), room, b).Cells));
                    }
            Assert.Greater(full, rooms * 9 / 10, "odaların çoğunda iki çift duvar");
        }

        /// <summary>Çift duvar tek hamlede tam bedeli ister (1 hakla hedef değil, 2 hakla yıkılır); Delici şarj 1 hak; dinamit hedef almaz.</summary>
        [Test]
        public void DoubleWall_Charge_FullCost_Piercer_DynamiteNot()
        {
            foreach (bool piercer in new[] { false, true })
            {
                var p = P(Lv(yikim: 1), piercer ? new[] { FacilityGear.AnsPiercer } : null);
                var B = Open(p, new List<RoomReport>(), B0);
                Calm(B);
                var dw = Doubles(B.Cells);
                Assert.AreEqual(2, dw.Count, B.Room.Id);
                Baskin.Pt? stand = null, wall = null;
                foreach (var t in dw)
                    foreach (var dd in Baskin.Dirs)
                        if (stand == null && Clear(B, Pt(t.X + dd[0], t.Y + dd[1]))) { stand = Pt(t.X + dd[0], t.Y + dd[1]); wall = t; }
                Assert.IsNotNull(stand, "çift duvarın boş komşusu");
                var u = BySlot(B, 2);
                Place(u, stand.Value);
                int cost = piercer ? B0.facilityPiercerCharges : B0.facilityDoubleWallCharges;
                Assert.AreEqual(cost, Baskin.DoubleWallCost(B));
                Gear(u, FacilityGear.Charge, cost - 1);
                if (cost > 1) Assert.IsFalse(Has(Baskin.ActionTargets(B, u, Baskin.ActCharge), wall.Value), "hak yetmez: hedef değil");
                Gear(u, FacilityGear.Charge, cost);
                Assert.IsTrue(Has(Baskin.ActionTargets(B, u, Baskin.ActCharge), wall.Value));
                Assert.IsTrue(Baskin.IsDoubleWall(B, wall.Value.X, wall.Value.Y));
                AssertUndoRestores(B, s => Baskin.DoAction(s, u.Id, Baskin.ActCharge, wall.Value.X, wall.Value.Y), "çift cidar");
                u = Fresh(B, u);
                ActPreviewed(B, u, Baskin.ActCharge, wall.Value);
                var c = B.Cells[wall.Value.Y, wall.Value.X];
                Assert.AreEqual(("floor", false), (c.T, c.Double), "tek hamlede yıkıldı");
                Assert.AreEqual(0, Fresh(B, u).GearLeft);
                Assert.IsTrue(B.FxList.Any(f => f.K == "dwall"));
                Assert.AreEqual(1, B.TallyBreach, "bir duvar bir sayaç");
            }
            // Dinamit (Madenci) çift duvarı hedef almaz
            var M = Open(P(Lv(yikim: 1)), new List<RoomReport>(), B0);
            Calm(M);
            foreach (var t in Doubles(M.Cells))
                foreach (var dd in Baskin.Dirs)
                {
                    var n = Pt(t.X + dd[0], t.Y + dd[1]);
                    if (!Clear(M, n)) continue;
                    var m = BySlot(M, 0);
                    m.Type = "maden"; m.Charges = 2;
                    Place(m, n);
                    Assert.IsFalse(Has(Baskin.Targets(M, m), t), "dinamit çift duvara atılmaz");
                    M.Cells[t.Y, t.X].Double = false;
                    Assert.IsTrue(Has(Baskin.Targets(M, m), t), "tek duvar dinamitle yıkılır");
                    M.Cells[t.Y, t.X].Double = true;
                }
        }

        // ---------- Dönüş tahtası ----------
        /// <summary>
        /// Dönüşte: yıkılan çift duvar moloz, yıkılmayan yine çift; kırılan koku yayıcı yok (kaplama dönüşte yeniden kurulmaz); sökülen
        /// cıvata sökük kalır (raporun makine parçası).
        /// </summary>
        [Test]
        public void Return_BrokenDoubleIsRubble_EmitterStaysBroken_UnboltedStays()
        {
            var p = P(Lv(itme: 1, yikim: 1, kopek: 1), new[] { FacilityGear.AnsSokum });
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B);
            var dw = Doubles(B.Cells);
            var em = Emitters(B.Cells);
            Assert.AreEqual((2, 1), (dw.Count, em.Count), B.Room.Id);
            // Analist lokumla bir çift duvarı yıkar
            var ana = BySlot(B, 2);
            Baskin.Pt? stand = null, wall = null;
            // Moloz duvarın öbür yanına sıçrar: oradaki nesneye (yayıcı, donanım) değmeyen yön seçilir (aşama 12: na_1.1'in planı değişti)
            foreach (var t in dw)
                foreach (var dd in Baskin.Dirs)
                    if (stand == null && Clear(B, Pt(t.X + dd[0], t.Y + dd[1])) && !HasObj(B, Pt(t.X - dd[0], t.Y - dd[1]))) { stand = Pt(t.X + dd[0], t.Y + dd[1]); wall = t; }
            Assert.IsNotNull(stand);
            Place(ana, stand.Value);
            Gear(ana, FacilityGear.Charge, 2);
            Assert.IsTrue(Baskin.DoAction(B, ana.Id, Baskin.ActCharge, wall.Value.X, wall.Value.Y));
            var other = dw.First(t => t.X != wall.Value.X || t.Y != wall.Value.Y);
            // EMP uzmanı yayıcıyı kırar
            var emp = BySlot(B, 1);
            var e0 = em[0];
            Baskin.Pt? from = null;
            foreach (var dd in Baskin.Dirs)
                for (int k = 2; k <= 5 && from == null; k++)
                    if (Clear(B, Pt(e0.X + dd[0] * k, e0.Y + dd[1] * k))) from = Pt(e0.X + dd[0] * k, e0.Y + dd[1] * k);
            Assert.IsNotNull(from);
            Place(emp, from.Value);
            Assert.IsTrue(Baskin.DoAct(B, emp.Id, e0.X, e0.Y));
            Assert.IsNull(B.Cells[e0.Y, e0.X].O, "yayıcı kırıldı");
            Assert.IsTrue(B.FxList.Any(f => f.K == "break" && f.Obj == "yayici"));
            // Teknisyen imza makinesinin cıvatasını söker
            var tek = BySlot(B, 0);
            Baskin.Pt? zat = null, tat = null;
            for (int y = 0; y < Baskin.N && zat == null; y++)
                for (int x = 0; x < Baskin.N && zat == null; x++)
                    foreach (var dd in Baskin.Dirs)
                    {
                        var t = Pt(x, y);
                        var a = Pt(x + dd[0], y + dd[1]);
                        var b2 = Pt(x + 2 * dd[0], y + 2 * dd[1]);
                        if (zat != null || !Clear(B, t) || !Clear(B, a) || !Clear(B, b2)) continue;
                        if (Baskin.IsGateTile(B, a.X, a.Y) || Baskin.IsGateTile(B, b2.X, b2.Y)) continue;
                        tat = t; zat = a;
                    }
            Assert.IsNotNull(zat);
            Place(tek, tat.Value);
            var z = AddMachine(B, "zdron", zat.Value, 9);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, zat.Value.X, zat.Value.Y));
            Assert.IsTrue(z.Unbolted);
            var r = PassRoom(p, reps, B);
            Assert.IsTrue(r.Machines.Single(m => m.Type == "zdron").Unbolted, "rapor sökük yazar");
            Assert.IsFalse(r.Objects.Any(o => o.K == "yayici"));
            Assert.AreEqual(1, r.Tally[FacilityCounters.Index(FacilityCounters.Breach)]);
            reps.Add(Rep(2));
            reps.Add(Rep(3));
            Assert.AreEqual(new RoomStep(1, true), RaidChain.Next(p, reps).Value);
            var R = Open(p, reps, B0);
            Assert.AreEqual(("floor", false), (R.Cells[wall.Value.Y, wall.Value.X].T, R.Cells[wall.Value.Y, wall.Value.X].Double), "yıkılan moloz");
            Assert.IsTrue(R.Rubble.Any(t => t.X == wall.Value.X && t.Y == wall.Value.Y));
            Assert.AreEqual(("wall", true), (R.Cells[other.Y, other.X].T, R.Cells[other.Y, other.X].Double), "yıkılmayan yine çift");
            Assert.IsEmpty(Emitters(R.Cells), "kırılan yayıcı dönüşte yok");
            var rz = R.Units.Single(u => u.Type == "zdron" && !u.Dead);
            Assert.IsTrue(rz.Unbolted);
            Assert.IsFalse(Baskin.Bolted(R, rz));
            CollectionAssert.AreEqual(new int[6], R.TallyArray(), "dönüş tahtası sıfırdan sayar");
        }

        /// <summary>Kırılmayan yayıcı dönüşte rapordan gelir (kaplama yeniden uygulanmaz).</summary>
        [Test]
        public void Return_UnbrokenEmitterFromReport()
        {
            var p = P(Lv(kopek: 1));
            var reps = new List<RoomReport>();
            var B = Open(p, reps, B0);
            Calm(B, 1);
            var em = Emitters(B.Cells);
            Assert.AreEqual(1, em.Count);
            var r = PassRoom(p, reps, B);
            if (RaidChain.IsClean(r)) Assert.Inconclusive("G temiz kaldı");
            Assert.AreEqual(1, r.Objects.Count(o => o.K == "yayici"));
            reps.Add(Rep(2));
            reps.Add(Rep(3));
            var R = Open(p, reps, B0);
            Assert.AreEqual(Key(em), Key(Emitters(R.Cells)));
        }

        // ---------- orme · Kesici kol ----------
        /// <summary>Makinenin niyeti (sırası 1) uygulanır: kalan nesne canı (kırıldıysa −1).</summary>
        static int MachineHit(List<int> counters, string type, string kind, int dir, Baskin.Pt at, string objK, Baskin.Pt objAt, int objHp, string[] answers = null)
        {
            var B = FlatB(counters, answers);
            B.StillTurns = 99;
            B.Cells[objAt.Y, objAt.X].O = Obj(objK, objHp);
            var e = AddMachine(B, type, at);
            e.Intent = new Baskin.Intent { Kind = kind, Dir = dir };
            e.Order = 1;
            Assert.IsTrue(Baskin.EndTurn(B));
            var o = B.Cells[objAt.Y, objAt.X].O;
            Assert.AreEqual(counters[2] >= 1 && (objK == "tel" || objK == "kalas" && counters[2] >= 2) && kind != "shoot", B.FxList.Any(f => f.K == "cut"), type + " " + objK);
            return o == null ? -1 : o.Hp;
        }

        [Test]
        public void Cutter_MachineIntents_WireAndPlank()
        {
            int W = DirOf(new[] { -1, 0 });
            // Yakın vuruş (bakım 2): kademe 0'da tel 3 → 1, kademe 1'de kesilir
            Assert.AreEqual(1, MachineHit(Lv(), "bakim", "melee", W, Pt(4, 3), "tel", Pt(3, 3), 3));
            Assert.AreEqual(-1, MachineHit(Lv(orme: 1), "bakim", "melee", W, Pt(4, 3), "tel", Pt(3, 3), 3));
            // Kalas yalnız kademe 2'de +1
            Assert.AreEqual(1, MachineHit(Lv(orme: 1), "bakim", "melee", W, Pt(4, 3), "kalas", Pt(3, 3), 3));
            Assert.AreEqual(-1, MachineHit(Lv(orme: 2), "bakim", "melee", W, Pt(4, 3), "kalas", Pt(3, 3), 3));
            // Havan ve patlama +1
            Assert.AreEqual(2, MachineHit(Lv(), "havan", "lob", W, Pt(4, 3), "tel", Pt(1, 3), 3));
            Assert.AreEqual(1, MachineHit(Lv(orme: 1), "havan", "lob", W, Pt(4, 3), "tel", Pt(1, 3), 3));
            Assert.AreEqual(1, MachineHit(Lv(orme: 1), "bomba", "blast", 0, Pt(4, 3), "tel", Pt(3, 3), 3));
            // Atış teli geçer (hasar yok)
            Assert.AreEqual(3, MachineHit(Lv(orme: 2), "dron", "shoot", W, Pt(5, 3), "tel", Pt(4, 3), 3));
            // Hurda kesici kolun nesnesi değil
            Assert.AreEqual(1, MachineHit(Lv(orme: 2), "bakim", "melee", W, Pt(4, 3), "hurda", Pt(3, 3), 3));
        }

        /// <summary>Çelik tel seti (cevap): tel 3 + 1 can; kesici kollu bakım (3) tek vuruşta kesemez.</summary>
        [Test]
        public void SteelWire_FourHp_CutterCannotCutInOne()
        {
            var B = FlatB(Lv(orme: 1), new[] { FacilityGear.AnsSteelWire });
            var tek = BySlot(B, 0);
            Place(tek, Pt(3, 3));
            Gear(tek, FacilityGear.Wire, 2);
            AssertUndoRestores(B, s => Baskin.DoAction(s, tek.Id, Baskin.ActWire, 4, 3), "çelik tel");
            tek = Fresh(B, tek);
            ActPreviewed(B, tek, Baskin.ActWire, Pt(4, 3));
            Assert.AreEqual(B0.facilityWireHp + B0.facilitySteelWireHp, B.Cells[3, 4].O.Hp);
            var C = FlatB(Lv(), new[] { FacilityGear.AnsSteelWire });
            var t2 = BySlot(C, 0);
            Place(t2, Pt(3, 3));
            Gear(t2, FacilityGear.Wire, 2);
            Assert.IsTrue(Baskin.DoAction(C, t2.Id, Baskin.ActPlank, 4, 3));
            Assert.AreEqual(B0.facilityPlankHp, C.Cells[3, 4].O.Hp, "kalas değişmez");
            Assert.AreEqual(1, MachineHit(Lv(orme: 1), "bakim", "melee", DirOf(new[] { -1, 0 }), Pt(4, 3), "tel", Pt(3, 3), 4, new[] { FacilityGear.AnsSteelWire }));
        }

        // ---------- ele · Kısa bağ ----------
        [Test]
        public void ShortClamp_Turns_Relay()
        {
            foreach (var (level, relay, want) in new[] { (0, false, 3), (1, false, 2), (1, true, 3), (0, true, 4), (2, false, 2) })
            {
                var B = FlatB(Lv(ele: level), relay ? new[] { FacilityGear.AnsRelay } : null);
                var tek = BySlot(B, 0);
                Place(tek, Pt(3, 3));
                Gear(tek, FacilityGear.Clamp, 1);
                var e = AddMachine(B, "dron", Pt(4, 3));
                Assert.AreEqual(want, Baskin.ClampTurnsFor(B, tek), $"k{level} röle {relay}");
                AssertUndoRestores(B, s => Baskin.DoAction(s, tek.Id, Baskin.ActClamp, 4, 3), "kelepçe");
                tek = Fresh(B, tek);
                ActPreviewed(B, tek, Baskin.ActClamp, Pt(4, 3));
                Assert.AreEqual(want, Fresh(B, e).Turns, $"k{level} röle {relay}");
            }
        }

        /// <summary>Kademe 2: süresi biten tutsak aynı planda niyet alır; plan bir çekim fazla yapar (belgelenen fark).</summary>
        [Test]
        public void ShortClamp_Level2_ReleasedMachinePlansSameTurn_OneMoreDraw()
        {
            var B1 = FlatB(Lv(ele: 1));
            var c = Captive(AddMachine(B1, "dron", Pt(4, 3)), 0, 0);
            var B2 = B1.Clone();
            B2.Moves = MoveRules.From(B0, P(Lv(ele: 2)));
            Assert.IsTrue(Baskin.EndTurn(B1));
            Assert.IsTrue(Baskin.EndTurn(B2));
            var c1 = Fresh(B1, c);
            var c2 = Fresh(B2, c);
            Assert.AreEqual(('e', 'e'), (c1.Side, c2.Side), "ikisi de bırakıldı");
            Assert.IsNull(c1.Intent, "kademe 1: bu plan niyetsiz");
            Assert.IsNotNull(c2.Intent, "kademe 2: hemen niyet");
            Assert.IsFalse(c2.Idle);
            Assert.AreEqual(unchecked((int)0x6D2B79F5), unchecked(B2.Rs - B1.Rs), "bir çekim fazla");
        }

        // ---------- kopek · Koku yayıcı ----------
        /// <summary>
        /// Her odada kademe kadar yayıcı; batıya ≥ 3; konunca batıdan ulaşılan hiçbir kare ve hiçbir hedef kopmaz; makine sayısı ve
        /// çekim sayısı önlemsiz tahtayla aynı; plan = tahta.
        /// </summary>
        [Test]
        public void Emitter_PerRoom_Constraints_SameDraws_PlanEqualsBoard()
        {
            int drawsSame = 0, rooms = 0;
            foreach (bool events in new[] { false, true })
                for (int w = 0; w < Wings.Count; w++)
                    foreach (var room in Wings[w])
                    {
                        var b = Bal(events);
                        var k0 = RoomOf(P(Lv(), wing: w, events: events), room, b);
                        foreach (int level in new[] { 1, 2 })
                        {
                            var B = RoomOf(P(Lv(kopek: level), wing: w, events: events), room, b);
                            var em = Emitters(B.Cells);
                            Assert.AreEqual(level, em.Count, room.Id + " k" + level);
                            foreach (var t in em)
                            {
                                Assert.IsTrue(room.West.All(x => Math.Abs(x.X - t.X) + Math.Abs(x.Y - t.Y) >= 3), room.Id + ": batıya ≥ 3");
                                Assert.AreEqual(b.facilityEmitterHp, B.Cells[t.Y, t.X].O.Hp);
                                Assert.AreEqual(0, B.Cells[t.Y, t.X].Door + B.Cells[t.Y, t.X].Vent);
                            }
                            // Bağ: önlemsiz tahtada batıdan ulaşılan her açık kare ve her hedef ulaşılır kalır
                            var r0 = Reach(k0.Cells, room.West);
                            var r1 = Reach(B.Cells, room.West);
                            for (int y = 0; y < Baskin.N; y++)
                                for (int x = 0; x < Baskin.N; x++)
                                {
                                    if (r0[y, x] && B.Cells[y, x].O == null && B.Cells[y, x].T != "wall") Assert.IsTrue(r1[y, x], $"{room.Id}: {x},{y} koptu");
                                    if (B.Cells[y, x].O is Baskin.Obj g && Baskin.IsTarget(g.K))
                                        Assert.IsTrue(Baskin.Dirs.Any(d => x + d[0] >= 0 && y + d[1] >= 0 && x + d[0] < 8 && y + d[1] < 8 && r1[y + d[1], x + d[0]]), room.Id + ": hedef");
                                }
                            rooms++;
                            if (B.Rs == k0.Rs && B.Units.Count(u => u.Side == 'e') == k0.Units.Count(u => u.Side == 'e')) drawsSame++;
                            bool ev = events && room.Event != null;
                            var plan = room.PlanCells(1, ev);
                            Baskin.ApplyCounterCells(plan, room, ev, Lv(kopek: level), b);
                            Assert.AreEqual(Key(em), Key(Emitters(plan)), room.Id + ": plan = tahta");
                        }
                    }
            Assert.AreEqual(rooms, drawsSame, "yayıcı makine sayısını ve çekim sayısını değiştirmez");
        }

        static bool[,] Reach(Baskin.Cell[,] cells, IEnumerable<Baskin.Pt> from)
        {
            var seen = new bool[8, 8];
            var q = new Queue<Baskin.Pt>();
            foreach (var f in from) if (cells[f.Y, f.X].T != "wall" && cells[f.Y, f.X].O == null) { seen[f.Y, f.X] = true; q.Enqueue(f); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Baskin.Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (x < 0 || y < 0 || x >= 8 || y >= 8 || seen[y, x] || cells[y, x].T == "wall" || cells[y, x].O != null) continue;
                    seen[y, x] = true;
                    q.Enqueue(Pt(x, y));
                }
            }
            return seen;
        }

        /// <summary>Erimde (Manhattan ≤ 3) ayakta yayıcı varken köpek komut alamaz, 4. karede alır; sabotajla kırılınca açılır. Makine
        /// yayıcıyı bilerek vurmaz; yayıcı atışı keser.</summary>
        [Test]
        public void Emitter_BlocksDogCommands_InRange_BreaksBySabotage_MachinesIgnore()
        {
            var B = FlatB(Lv(kopek: 1));
            var d = AddDog(B, 0, Pt(3, 3));
            B.Cells[3, 7].O = Obj("yayici", 1);
            Assert.IsFalse(Baskin.EmitterBlocks(B, d), "4. kare");
            Assert.IsTrue(Baskin.CanAction(B, d, Baskin.ActBark));
            B.Cells[3, 7].O = null;
            B.Cells[3, 6].O = Obj("yayici", 1);
            Assert.IsTrue(Baskin.EmitterBlocks(B, d), "3. kare");
            foreach (var k in new[] { Baskin.ActBark, Baskin.ActFetch, Baskin.ActSniff }) Assert.IsFalse(Baskin.CanAction(B, d, k), k);
            Assert.IsNotEmpty(Baskin.Reachable(B, d), "yürüyüş serbest");
            var tek = BySlot(B, 0);
            Assert.IsFalse(Baskin.EmitterBlocks(B, tek), "yalnız köpek");
            Place(tek, Pt(6, 4));
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 6, 3));
            Assert.IsNull(B.Cells[3, 6].O);
            Assert.IsTrue(B.FxList.Any(f => f.K == "break" && f.Obj == "yayici"));
            Assert.IsTrue(Baskin.CanAction(B, d, Baskin.ActBark), "kırılınca açılır");
            // Makine yayıcıya bilerek vurmaz; patlama alanında da kaçınır
            var C = FlatB(Lv(kopek: 1));
            C.Cells[3, 3].O = Obj("yayici", 1);
            var m = AddMachine(C, "bakim", Pt(4, 3));
            Assert.AreEqual(-10, Baskin.TestScore(C, m, "melee", DirOf(new[] { -1, 0 })));
            var bomb = AddMachine(C, "bomba", Pt(3, 4));
            Assert.AreEqual(-10, Baskin.TestScore(C, bomb, "blast", 0));
            // Atışı keser
            var D = FlatB(Lv(kopek: 1));
            D.Cells[3, 3].O = Obj("yayici", 1);
            var dr = AddMachine(D, "dron", Pt(5, 3));
            Place(BySlot(D, 2), Pt(1, 3));
            Assert.AreEqual(-10, Baskin.TestScore(D, dr, "shoot", DirOf(new[] { -1, 0 })), "atış yayıcıda durur");
            D.Cells[3, 3].O = null;
            Assert.Greater(Baskin.TestScore(D, dr, "shoot", DirOf(new[] { -1, 0 })), 0, "yayıcısız atış uzmana");
        }

        // ---------- aygit · Parazit ve makine aklı ----------
        [Test]
        public void Jam_DeviceTurns_Spring()
        {
            foreach (var (level, spring, want) in new[] { (0, false, 3), (1, false, 2), (1, true, 3), (0, true, 4), (2, false, 2) })
            {
                var B = FlatB(Lv(aygit: level), spring ? new[] { FacilityGear.AnsSpring } : null);
                var tek = BySlot(B, 0);
                Place(tek, Pt(3, 3));
                Gear(tek, FacilityGear.Decoy, 1);
                Assert.AreEqual(want, Baskin.DeviceTurnsFor(B, tek));
                AssertUndoRestores(B, s => Baskin.DoAction(s, tek.Id, Baskin.ActDecoy, 4, 3), "yem");
                tek = Fresh(B, tek);
                ActPreviewed(B, tek, Baskin.ActDecoy, Pt(4, 3));
                var g = B.Units.Last();
                Assert.IsTrue(Baskin.IsGadget(g));
                Assert.AreEqual(want, g.Turns, $"k{level} zemberek {spring}");
                Assert.AreEqual(level >= 1, B.FxList.Any(f => f.K == "jam"));
            }
        }

        /// <summary>
        /// Makine aklı (aygıt 2. kademe): eşit puanlı üç atış karesinden pusu ve taret hattındakiler seçilmez; kademe 1'de seçilebilir.
        /// Çekim sayısı aynı (makine başına bir).
        /// </summary>
        [Test]
        public void MachineSense_AvoidsAmbushAndTurretLines_SameDraws()
        {
            Baskin.BState Setup(int level, int rs)
            {
                var B = FlatB(Lv(aygit: level));
                B.Smart = false;
                var tek = BySlot(B, 0);
                Place(tek, Pt(2, 2));
                var emp = BySlot(B, 1);
                Place(emp, Pt(5, 0));
                emp.AmbushDir = DirOf(new[] { 0, 1 }); emp.AmbushX = 5; emp.AmbushY = 0;
                Place(BySlot(B, 2), Pt(0, 7));
                AddGadget(B, "taret", Pt(6, 0), new[] { 0, 1 });
                AddMachine(B, "dron", Pt(5, 4));
                B.Rs = rs;
                return B;
            }
            var probe = Setup(2, 1);
            var danger = Baskin.DangerTiles(probe);
            Assert.IsTrue(Has(danger, Pt(5, 2)) && Has(danger, Pt(5, 3)), "pusu hattı");
            Assert.IsTrue(Has(danger, Pt(6, 2)), "taret hattı");
            int onLine1 = 0;
            for (int rs = 1; rs <= 40; rs++)
            {
                var B1 = Setup(1, rs * 7919);
                var B2 = Setup(2, rs * 7919);
                Assert.IsTrue(Baskin.EndTurn(B1));
                Assert.IsTrue(Baskin.EndTurn(B2));
                var e1 = B1.Units.Single(u => u.Type == "dron");
                var e2 = B2.Units.Single(u => u.Type == "dron");
                Assert.AreEqual(B1.Rs, B2.Rs, "çekim sayısı aynı");
                Assert.IsFalse(Has(danger, Pt(e2.X, e2.Y)), $"kademe 2 hatta durmaz: {e2.X},{e2.Y}");
                Assert.AreEqual("shoot", e2.Intent?.Kind);
                if (Has(danger, Pt(e1.X, e1.Y))) onLine1++;
            }
            Assert.Greater(onLine1, 0, "kademe 1'de hat seçilebilir (sınama anlamlı)");
        }

        // ---------- Okuyucular ----------
        [Test]
        public void NoCountermeasure_Defaults()
        {
            // Önlemsiz kural değerleri
            var m = MoveRules.From(null);
            Assert.IsFalse(m.Tally || m.TallyAmbush || m.Unbolt || m.Piercer);
            Assert.AreEqual(0, Enumerable.Range(0, 6).Sum(i => m.Level(i)));
            Assert.AreEqual(0, m.SteelWireHp + m.RelayTurns + m.SpringTurns + m.PiercerCharges);
            var k = MoveRules.From(new Balance(), new RaidParams { Counters = new List<int> { 9, -1, 1, 2, 0, 3 }, Answers = new List<string>(FacilityGear.Answers) });
            Assert.IsTrue(k.Tally);
            CollectionAssert.AreEqual(new[] { 2, 0, 1, 2, 0, 2 }, Enumerable.Range(0, 6).Select(i => k.Level(i)));
            Assert.AreEqual(2, k.Level(FacilityCounters.Capture));
            Assert.IsTrue(k.Unbolt && k.Piercer);
            Assert.AreEqual((1, 1, 1, 1), (k.SteelWireHp, k.RelayTurns, k.SpringTurns, k.PiercerCharges));
            // Önlemsiz tesis tahtasında kaplama yok
            var F = Open(P(Lv()), new List<RoomReport>(), B0);
            Assert.IsEmpty(Doubles(F.Cells));
            Assert.IsEmpty(Emitters(F.Cells));
        }

        [Test]
        public void Signature_NewSegments_OnlyWhenSet()
        {
            var B = FlatB(Lv());
            string s0 = Baskin.Signature(B);
            StringAssert.DoesNotContain("|T", s0);
            StringAssert.DoesNotContain("|D", s0);
            B.TallyBuild = 2;
            B.Cells[1, 2].T = "wall"; B.Cells[1, 2].Double = true;
            var z = AddMachine(B, "zdron", Pt(5, 5));
            z.Unbolted = true;
            string s = Baskin.Signature(B);
            StringAssert.Contains("|T0.0.2.0.0.0|D21", s);
            StringAssert.Contains("zdron55:3U", s);
            Assert.IsTrue(s.EndsWith("|R" + B.Room.Id), "oda eki sonda");
        }

        // ---------- Statik tablo ----------
        [Test]
        public void Table_NamesDescsAnswers_Learn()
        {
            var b = new Balance();
            foreach (var c in FacilityCounters.Classes)
            {
                Assert.IsNotEmpty(FacilityCounters.ClassName(c));
                for (int lv = 1; lv <= 2; lv++)
                {
                    Assert.IsNotEmpty(FacilityCounters.Name(c, lv), c);
                    Assert.IsNotEmpty(FacilityCounters.Desc(b, c, lv), c + lv);
                }
                Assert.IsEmpty(FacilityCounters.Desc(b, c, 0));
                var a = FacilityCounters.AnswerOf(c);
                if (c == FacilityCounters.Dog) Assert.IsNull(a);
                else { CollectionAssert.Contains(FacilityGear.Answers, a); Assert.AreEqual(c, FacilityCounters.ClassOfAnswer(a)); }
                Assert.AreEqual(c == FacilityCounters.Push, FacilityCounters.Short(c) != null);
            }
            Assert.AreNotEqual(FacilityCounters.Name(FacilityCounters.Gadget, 1), FacilityCounters.Name(FacilityCounters.Gadget, 2), "makine aklı ayrı ad");
            Assert.LessOrEqual(FacilityCounters.Short(FacilityCounters.Push).Length, 9);
            Assert.AreEqual(-1, FacilityCounters.Index("yok"));
            Assert.IsTrue(FacilityCounters.SignatureMachine("zdron") && !FacilityCounters.SignatureMachine("dron") && !FacilityCounters.SignatureMachine("bakim"));
            // Öğrenme (saf): eşik, en büyük, eşitlikte günlükte en eski (hiç = en eski), sonra tablo sırası; kademe 2 dışarıda
            var none = new List<FacilityCounterEntry>();
            Assert.AreEqual(0, FacilityCounters.Learn(new[] { 6, 6, 0, 0, 0, 0 }, new int[6], none, 6), "tablo sırası");
            Assert.AreEqual(1, FacilityCounters.Learn(new[] { 6, 7, 0, 0, 0, 0 }, new int[6], none, 6), "en büyük");
            Assert.AreEqual(-1, FacilityCounters.Learn(new[] { 5, 5, 0, 0, 0, 0 }, new int[6], none, 6), "eşik altı");
            var log = new List<FacilityCounterEntry> { new FacilityCounterEntry { Class = "itme", Level = 1 } };
            Assert.AreEqual(1, FacilityCounters.Learn(new[] { 6, 6, 0, 0, 0, 0 }, new[] { 1, 0, 0, 0, 0, 0 }, log, 6), "hiç öğrenilmeyen en eski");
            log.Add(new FacilityCounterEntry { Class = "yikim", Level = 1 });
            Assert.AreEqual(0, FacilityCounters.Learn(new[] { 6, 6, 0, 0, 0, 0 }, new[] { 1, 1, 0, 0, 0, 0 }, log, 6), "son girdisi en eski");
            Assert.AreEqual(1, FacilityCounters.Learn(new[] { 6, 6, 0, 0, 0, 0 }, new[] { 2, 1, 0, 0, 0, 0 }, log, 6), "kademe 2 dışarıda");
            Assert.AreEqual(-1, FacilityCounters.Learn(new int[6], new int[6], none, 0), "eşik en az 1");
            Assert.AreEqual(2, FacilityCounters.Learn(new[] { 0, 0, 1, 0, 0, 0 }, new int[6], none, 0));
            Assert.AreEqual(5, FacilityCounters.Learn(new[] { 0, 0, 0, 0, 0, 3 }, null, null, 2), "eksik liste 0 okunur");
        }
    }
}
