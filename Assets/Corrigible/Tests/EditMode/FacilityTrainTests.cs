using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 12, Katar (na, "katar"; Docs/TESIS-BASKINLARI-TASARIMI.md §10.1): kimlik ve plan (doğu geçidi), açık kenardan düşme
    /// (makine yok olur, ekip üyesi "k": ölmez, yatar; taşıyıcının yükü son karede; köpek, tutsak, aygıt), cıvata, geri alma ve önizleme,
    /// dönüşte takip ve AYIR, rapor ve sonuç dizgileri, savunmada kenar kapalı. Tahta ve zincir düzeyi (plan 79, na_1 kanadı).
    /// </summary>
    public class FacilityTrainTests
    {
        static readonly Balance B0 = Bal();
        static IReadOnlyList<FacilityRoom> NaWing(int w = 1) => Wing("na_" + w);
        static RaidParams P(int w = 1, int seed = 11) => Params(NaWing(w), seed);
        static Baskin.Pt Pt(int x, int y) => new Baskin.Pt(x, y);
        static readonly int[] North = Baskin.Dirs[0], East = Baskin.Dirs[1], South = Baskin.Dirs[2], West = Baskin.Dirs[3];

        /// <summary>Ara oda (A, Katar) gidiş tahtası, raporlarda elle G raporu; düz zemin, makinesiz; uzmanlar verilen karelerde.</summary>
        static Baskin.BState Mid(RaidParams p, List<RoomReport> reps, params Baskin.Pt[] spots)
        {
            reps.Add(Rep(1));
            var B = Open(p, reps);
            Assert.AreEqual(2, B.Room.Index);
            Assert.AreEqual("katar", B.Ev?.Id, "A Katar odası");
            Flat(B, spots);
            return B;
        }

        static Baskin.BState Mid(params Baskin.Pt[] spots) => Mid(P(), new List<RoomReport>(), spots);

        static Baskin.Fx FxOf(Baskin.BState B, string k, int unit) => B.FxList.FirstOrDefault(f => f.K == k && f.Unit == unit);

        /// <summary>Hedef odası (H, Katar) kaçışta: elle G ve A raporu (temiz), düz zemin; uzmanlar verilen karelerde.</summary>
        static Baskin.BState EscapeH(RaidParams p, List<RoomReport> reps, params Baskin.Pt[] spots)
        {
            reps.Add(Rep(1));
            reps.Add(Rep(2));
            var B = Open(p, reps);
            Assert.AreEqual(3, B.Room.Index);
            Assert.AreEqual("katar", B.Ev?.Id);
            Flat(B, spots);
            StartEscape(B);
            return B;
        }

        /// <summary>Kopya önizlemesi (<see cref="Baskin.Preview"/>) uygulamayla aynı: efektler, yer değişimleri ve imza.</summary>
        static void ActPreviewedAbility(Baskin.BState B, Baskin.Unit u, Baskin.Pt at)
        {
            string dump = FullDump(B);
            var pv = Baskin.Preview(B, u.Id, at.X, at.Y);
            Assert.AreEqual(dump, FullDump(B), "önizleme durumu değiştirdi");
            var before = B.Units.ToDictionary(v => v.Id, v => (v.X, v.Y));
            Assert.IsTrue(Baskin.DoAct(B, u.Id, at.X, at.Y));
            CollectionAssert.AreEqual(pv.Fx.Select(f => f.K + f.X + f.Y + f.Unit + f.Obj + f.Dx + f.Dy), B.FxList.Select(f => f.K + f.X + f.Y + f.Unit + f.Obj + f.Dx + f.Dy), "önizleme efektleri");
            var moved = B.Units.Where(v => before.ContainsKey(v.Id) && before[v.Id] != (v.X, v.Y)).Select(v => v.Id).OrderBy(i => i);
            CollectionAssert.AreEqual(moved, pv.Moves.Select(m => m.Unit).OrderBy(i => i));
        }

        // ---------- 1. Kimlik ----------
        [Test]
        public void Katar_Identity()
        {
            foreach (var r in Plan79.Region("na"))
                Assert.AreEqual(r.Type == RoomType.Entry ? null : "katar", r.Event, r.Id);
            FacilityMaps.ClearCache();
            foreach (var r in FacilityMaps.For(79, false, true).Region("na"))
                Assert.AreEqual(r.Type == RoomType.Entry ? null : "asansor", r.Event, "alt bayrak kapalı " + r.Id);
            FacilityMaps.ClearCache();
            var def = GameData.Region("na");
            Assert.AreEqual("katar", FacilityMaps.EventFor(def, true, false));
            Assert.AreEqual("asansor", FacilityMaps.EventFor(def, false, true));
            Assert.AreEqual(Baskin.OpenEdgeRows(Plan79.Room("na_1.2")), new[] { 0, Baskin.N - 1 });
            Assert.IsEmpty(Baskin.OpenEdgeRows(Plan79.Room("na_1.1")), "giriş vagonu şartsız");
            Assert.IsEmpty(Baskin.OpenEdgeRows(Plan79.Room("weu_1.2")));
            // Tesis emri Katar'ı (varsayılan denge de), alt bayrak kapalıyken asansörü taşır
            Assert.AreEqual("katar", NaOrder(new Balance { shelterEventsEnabled = false, startFon = 100 }).Event);
            Assert.AreEqual("katar", NaOrder(Bal()).Event);
            var off = Bal();
            off.facilityKatar = false;
            Assert.AreEqual("asansor", NaOrder(off).Event);
        }

        /// <summary>weu (karargâh) tamamlanmış sayılır, na seçilir (komut), na_1 baskın emri verilir; emrin parametreleri.</summary>
        static RaidParams NaOrder(Balance b)
        {
            var sim = ShelterEventKit.New(b, 79);
            foreach (var s in sim.LiberationRegion("weu").Sectors) s.Captured = true;
            var sel = sim.SelectLiberationRegion("na");
            Assert.IsTrue(sel.Ok, sel.Msg);
            ShelterEventKit.FillSquad(sim);
            var r = sim.StartLiberationRaid("na_1");
            Assert.IsTrue(r.Ok, r.Msg);
            return sim.S.Raid.Params;
        }

        // ---------- 2. Doğu geçidi ----------
        /// <summary>Katar tesisinde G ve A'nın ileri geçidi doğu sütununda (200 tohumluk tükenme denetimi FacilityMapsTests'te).</summary>
        [Test]
        public void Katar_ForwardGate_EastColumn()
        {
            for (uint seed = 1; seed <= 30; seed++)
                foreach (var r in new FacilityPlan(seed).Region("na").Where(r => r.Type != RoomType.Target))
                    Assert.IsTrue(r.Forward.Count == 2 && r.Forward.All(f => f.X == Baskin.N - 1 && f.Y > 0 && f.Y < Baskin.N - 1), seed + " " + r.Id);
            // Katar ve Yalpa dışında geçit kuzey ya da güney kenarında da olabilir (sonraki oda döndürülür; FacilityMapsTests.Mirror_…);
            // Yalpa (oc) odaları da döndürülmez: geçit doğuda
            bool other = false;
            for (uint seed = 1; seed <= 10 && !other; seed++)
                other = new FacilityPlan(seed).Region("sa").Any(r => r.Forward.Count == 2 && r.Forward.Any(f => f.X != Baskin.N - 1));
            Assert.IsTrue(other);
            for (uint seed = 1; seed <= 10; seed++)
                Assert.IsTrue(new FacilityPlan(seed).Region("oc").All(r => r.Turn == 0 && r.Forward.All(f => f.X == Baskin.N - 1)), seed + " oc");
        }

        // ---------- 3. Makine düşer ----------
        [Test]
        public void Katar_PushOffNorthSouth_MachineGone()
        {
            // EMP: hedefin kuzeyindeki makine kuzey kenarından düşer
            var reps = new List<RoomReport>();
            var p = P();
            var B = Mid(p, reps, Pt(1, 6), Pt(3, 3), Pt(6, 6));
            var emp = BySlot(B, 1);
            var m = AddMachine(B, "dron", Pt(3, 0));
            Assert.IsTrue(Baskin.DoAct(B, emp.Id, 3, 1));
            Assert.IsTrue(m.Dead);
            Assert.AreEqual(0, m.Hp);
            var fall = FxOf(B, "fall", m.Id);
            Assert.IsNotNull(fall);
            Assert.AreEqual((3, 0, 0, -1), (fall.X, fall.Y, fall.Dx, fall.Dy));
            Assert.IsNull(FxOf(B, "push", m.Id), "düşen itme için push yazılmaz");
            Assert.IsNull(FxOf(B, "die", m.Id));
            Assert.IsNull(Baskin.CellAt(B, 3, 0).O, "hurda yok");
            Assert.IsTrue(B.Log.Any(l => l.Text == Loc.F("{0} kenardan düştü.", Baskin.UnitTypes["dron"].Name)));
            ToGate(B);
            Assert.IsTrue(Baskin.Pass(B));
            var r = File(p, reps, B);
            Assert.IsEmpty(r.Machines, "düşen makine raporda yok");

            // Sabotaj: Teknisyen güneydeki makineyi güney kenarından iter
            B = Mid(Pt(4, 6), Pt(1, 1), Pt(6, 1));
            var tek = BySlot(B, 0);
            m = AddMachine(B, "dron", Pt(4, 7));
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 4, 7));
            Assert.IsTrue(m.Dead);
            Assert.AreEqual((4, 7, 0, 1), (FxOf(B, "fall", m.Id).X, FxOf(B, "fall", m.Id).Y, FxOf(B, "fall", m.Id).Dx, FxOf(B, "fall", m.Id).Dy));

            // Koşu darbesi: birinci itme kenara, ikincisi düşürür (bir push, bir fall)
            B = Mid(Pt(1, 6), Pt(2, 6), Pt(6, 6));
            var ana = BySlot(B, 2);
            m = AddMachine(B, "dron", Pt(6, 1));
            Assert.IsTrue(Baskin.ActionTargets(B, ana, Baskin.ActRush).Any(t => t.X == 6 && t.Y == 1));
            Assert.IsTrue(Baskin.DoAction(B, ana.Id, Baskin.ActRush, 6, 1));
            Assert.IsTrue(m.Dead);
            Assert.AreEqual(1, B.FxList.Count(f => f.K == "push" && f.Unit == m.Id));
            Assert.AreEqual(1, B.FxList.Count(f => f.K == "fall" && f.Unit == m.Id));
            // Birinci itme düşürürse ikinci itme yapılmaz
            B = Mid(Pt(1, 6), Pt(2, 6), Pt(6, 5));
            ana = BySlot(B, 2);
            m = AddMachine(B, "dron", Pt(6, 0));
            Assert.IsTrue(Baskin.DoAction(B, ana.Id, Baskin.ActRush, 6, 0));
            Assert.IsTrue(m.Dead);
            Assert.AreEqual(0, B.FxList.Count(f => f.K == "push" && f.Unit == m.Id));
            Assert.AreEqual(1, B.FxList.Count(f => f.K == "fall" && f.Unit == m.Id));

            // Doğu ve batı kenarı duvar: 1 hasar, yerinde kalır
            B = Mid(Pt(1, 6), Pt(6, 5), Pt(1, 1));
            emp = BySlot(B, 1);
            m = AddMachine(B, "dron", Pt(7, 3));
            Assert.IsTrue(Baskin.DoAct(B, emp.Id, 6, 3), "EMP doğu");
            Assert.IsFalse(m.Dead);
            Assert.AreEqual((7, 3, 2), (m.X, m.Y, m.Hp));
            Assert.IsNull(FxOf(B, "fall", m.Id));

            // Giriş vagonunda (G) kenar yok: kuzeye itme 1 hasar
            var G = Open(P(), new List<RoomReport>());
            Assert.AreEqual(1, G.Room.Index);
            Assert.IsNull(G.Ev);
            Flat(G, Pt(1, 6), Pt(3, 3), Pt(6, 6));
            m = AddMachine(G, "dron", Pt(3, 0));
            Assert.IsTrue(Baskin.DoAct(G, BySlot(G, 1).Id, 3, 1));
            Assert.IsFalse(m.Dead);
            Assert.AreEqual((3, 0, 2), (m.X, m.Y, m.Hp));
            Assert.IsFalse(Baskin.IsOpenEdge(G, 3, 0));
        }

        // ---------- 4. Ekip üyesi kenardan düşer ----------
        [Test]
        public void Katar_Specialist_Overboard_NotBodyNotStanding()
        {
            var reps = new List<RoomReport>();
            var p = P();
            var B = Mid(p, reps, Pt(4, 1), Pt(3, 3), Pt(3, 0));
            var tek = BySlot(B, 0);
            var emp = BySlot(B, 1);
            var ana = BySlot(B, 2);
            Assert.IsTrue(Baskin.IsOpenEdge(B, 3, 0));
            Assert.IsTrue(Baskin.DoAct(B, emp.Id, 3, 1));
            Assert.IsTrue(ana.Dead && ana.Overboard && !ana.Out && ana.Hp == 0);
            Assert.AreEqual((3, 0), (ana.X, ana.Y), "son kare");
            Assert.IsFalse(Baskin.IsBody(ana));
            Assert.IsFalse(Baskin.LyingBody(ana));
            Assert.IsFalse(Baskin.OnBoard(ana));
            Assert.IsEmpty(Baskin.DragBodies(B, tek), "sürüklenemez");
            Assert.IsTrue(B.Log.Any(l => l.Text == Loc.F("{0} vagondan düştü.", Baskin.UnitTypes["ana"].Name)));
            StringAssert.Contains("K", Baskin.Signature(B).Split('|')[4 + (B.Carrier != 0 ? 1 : 0)]);
            // GEÇ onu beklemez; son nefes almaz
            ToGate(B);
            Assert.IsTrue(Baskin.CanPass(B));
            Assert.IsTrue(Baskin.Pass(B));
            Assert.IsFalse(ana.Breath);
            var r = File(p, reps, B);
            var k = r.Team[2];
            Assert.AreEqual(("k", 0, 2, 3, 0, 0), (k.State, k.Hp, k.Fell, k.X, k.Y, k.Body));
            // Sonraki odaya konmaz, rapor onu taşır
            var H = Open(p, reps);
            Assert.AreEqual(3, H.Room.Index);
            Assert.IsNull(H.Units.FirstOrDefault(u => u.Side == 'p' && u.Slot == 2), "kenardan düşen sonraki tahtada yok");
            Calm(H);
            StartEscape(H);
            ToGate(H);
            Assert.IsTrue(Baskin.Pass(H));
            var r3 = File(p, reps, H);
            Assert.AreEqual("k", r3.Team[2].State);
            Assert.AreEqual(2, r3.Team[2].Fell);
            Assert.IsTrue(r3.Last, "batıdaki odalar temiz: yürüyerek çıkış");
            Assert.AreEqual(0, r3.Team[2].Body, "yürüyerek çıkış onu çıkarmaz");
            var res = RaidChain.Result(p, reps);
            CollectionAssert.AreEqual(new[] { 2 }, res.Overboard);
            CollectionAssert.DoesNotContain(res.Extracted, 2);
            Assert.IsFalse(RaidChain.IsExtracted(p, r3, r3.Team[2]));
            Assert.AreEqual(1, res.Down);
            Assert.AreEqual(0, res.Hp[2]);
        }

        // ---------- 5. Ayaktaki son uzman ----------
        [Test]
        public void Katar_LastStandingFalls_TeamDown()
        {
            var reps = new List<RoomReport>();
            var p = P();
            var B = Mid(p, reps, Pt(5, 5), Pt(3, 3), Pt(3, 0));
            Down(B, BySlot(B, 0));
            Assert.IsTrue(Baskin.DoAct(B, BySlot(B, 1).Id, 3, 1));
            Assert.IsTrue(BySlot(B, 2).Overboard);
            Assert.AreEqual("player", B.Phase, "EMP uzmanı ayakta");
            Down(B, BySlot(B, 1));
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual("lost", B.Phase);
            Assert.AreEqual(Loc.T("Ekibin tamamı yere düştü."), B.OutcomeReason);
            var r = File(p, reps, B);
            CollectionAssert.AreEqual(new[] { "d", "d", "k" }, r.Team.Select(m => m.State).ToArray());
            var res = RaidChain.Result(p, reps);
            CollectionAssert.AreEqual(new[] { 2 }, res.Overboard);
            CollectionAssert.IsEmpty(res.Extracted, "yerde yatanlar çıkarılmadı");

            // Kaçışta tesisin girişinden son çıkış: kenardan düşen çıkan sayılmaz (sunucu kanadı, na_3)
            var p3 = P(3);
            var rr = new List<RoomReport> { Rep(1), Rep(2), Rep(3) };
            rr[2].Team[2] = new RoomMember { State = "k", Hp = 0, Fell = 2, X = 3, Y = 0 };
            var gw = NaWing(3)[0];
            var G = Baskin.CreateRoom(p3, gw, RaidChain.Setup(B0, gw, p3, rr, true));
            Assert.IsTrue(Baskin.IsFinalExit(G));
            Calm(G);
            var team = Standing(G);
            Assert.AreEqual(2, team.Count, "kenardan düşen dönüş tahtasında yok");
            foreach (var u in team)
            {
                var w = gw.West.First(t => Baskin.UnitAt(G, t.X, t.Y) == null);
                Baskin.CellAt(G, 1, w.Y).T = "floor"; Baskin.CellAt(G, 1, w.Y).O = null;
                Place(u, Pt(1, w.Y));
                Assert.IsTrue(Baskin.DoMove(G, u.Id, w.X, w.Y), "tahliye");
            }
            Assert.AreEqual("won", G.Phase);
            Assert.AreEqual(Loc.F("YZ düğümü kırıldı. Ekipten {0}/{1} uzman çıktı.", 2, 3), G.OutcomeReason);
        }

        // ---------- 6. Taşıyıcı, gövde ve yerdeki yük ----------
        [Test]
        public void Katar_CarrierFalls_CargoOnEdgeSquare()
        {
            // Uzman taşıyıcı (ikmal kanadı na_1, kaçışta A)
            var B = Mid(Pt(3, 0), Pt(3, 3), Pt(6, 6));
            StartEscape(B);
            var tek = BySlot(B, 0);
            Assert.AreEqual(tek.Id, B.Carrier);
            Assert.IsTrue(Baskin.DoAct(B, BySlot(B, 1).Id, 3, 1));
            Assert.IsTrue(tek.Overboard);
            Assert.AreEqual((0, 3, 0), (B.Carrier, B.CargoX, B.CargoY), "yük son karede");
            Assert.IsNotNull(FxOf(B, "cargo-drop", tek.Id));
            // Köpek taşıyıcı (K33): yük son karede, köpek düşer
            B = Mid(Pt(1, 1), Pt(5, 3), Pt(6, 1));
            StartEscape(B);
            var dog = AddDog(B, 0, Pt(5, 7));
            B.Carrier = dog.Id;
            Assert.IsTrue(Baskin.DoAct(B, BySlot(B, 1).Id, 5, 6));
            Assert.IsTrue(dog.Dead);
            Assert.IsNotNull(FxOf(B, "fall", dog.Id));
            Assert.AreEqual((0, 5, 7), (B.Carrier, B.CargoX, B.CargoY));
        }

        [Test]
        public void Katar_BodyAndFloorCargo_NotPushedOff()
        {
            var B = Mid(Pt(3, 0), Pt(3, 3), Pt(6, 6));
            StartEscape(B);
            var tek = BySlot(B, 0);
            Down(B, tek);
            Assert.AreEqual((3, 0), (B.CargoX, B.CargoY), "taşıyıcı düştü, yük yerde");
            Assert.IsTrue(Baskin.LyingBody(tek));
            Assert.IsTrue(Baskin.DoAct(B, BySlot(B, 1).Id, 3, 1));
            Assert.IsTrue(Baskin.LyingBody(tek), "gövde itilmez");
            Assert.IsFalse(tek.Overboard);
            Assert.AreEqual((3, 0), (tek.X, tek.Y));
            Assert.AreEqual((3, 0), (B.CargoX, B.CargoY), "yerdeki yük itilmez");
            Assert.IsFalse(B.FxList.Any(f => f.K == "fall"));
        }

        // ---------- 7. Cıvata ----------
        [Test]
        public void Katar_BoltedMachine_DoesNotFall()
        {
            var p = P();
            p.Counters = new List<int> { 1, 0, 0, 0, 0, 0 };
            var B = Mid(p, new List<RoomReport>(), Pt(1, 6), Pt(3, 3), Pt(6, 6));
            var m = AddMachine(B, "havan", Pt(3, 0));
            Assert.IsTrue(Baskin.Bolted(B, m));
            Assert.IsTrue(Baskin.DoAct(B, BySlot(B, 1).Id, 3, 1));
            Assert.IsFalse(m.Dead);
            Assert.AreEqual((3, 0), (m.X, m.Y));
            Assert.IsNotNull(FxOf(B, "bolted", m.Id));
            Assert.IsNull(FxOf(B, "fall", m.Id));
        }

        // ---------- 8. Köpek, tutsak, aygıt ----------
        [Test]
        public void Katar_Dog_Captive_Gadget_Fall()
        {
            var reps = new List<RoomReport>();
            var p = WithGear(P(), 0, FacilityGear.Dog);
            var B = Mid(p, reps, Pt(1, 1), Pt(3, 3), Pt(6, 6));
            var dog = B.Units.FirstOrDefault(Baskin.IsDog) ?? AddDog(B, 0, Pt(3, 0));
            Place(dog, Pt(3, 0));
            Assert.IsTrue(Baskin.DoAct(B, BySlot(B, 1).Id, 3, 1));
            Assert.IsTrue(dog.Dead);
            Assert.IsNotNull(FxOf(B, "fall", dog.Id));
            ToGate(B);
            Assert.IsTrue(Baskin.Pass(B));
            var r = File(p, reps, B);
            Assert.AreEqual("d", r.Units.Single(u => u.Class == Baskin.ClassDog).State, "köpek düştü (baskından sonra dinlenir)");

            // Tutsak ve aygıt yok olur, hurda bırakmaz
            B = Mid(Pt(1, 1), Pt(5, 3), Pt(6, 6));
            var cap = Captive(AddMachine(B, "bakim", Pt(5, 0)));
            Assert.IsTrue(Baskin.DoAct(B, BySlot(B, 1).Id, 5, 1));
            Assert.IsTrue(cap.Dead);
            Assert.IsNotNull(FxOf(B, "fall", cap.Id));
            var tek = BySlot(B, 0);
            var g = AddGadget(B, "taret", Pt(1, 0), South);
            Assert.IsTrue(Baskin.DoAct(B, tek.Id, 1, 0), "sabotaj aygıtı iter");
            Assert.IsTrue(g.Dead);
            Assert.IsNotNull(FxOf(B, "fall", g.Id));
            Assert.IsNull(Baskin.CellAt(B, 1, 0).O, "aygıt hurda bırakmaz");
        }

        // ---------- 9. Geri alma ve önizleme ----------
        [Test]
        public void Katar_EveryFall_UndoAndPreviewEqualApply()
        {
            // EMP makineyi ve ekip üyesini düşürür
            var B = Mid(Pt(1, 6), Pt(3, 3), Pt(3, 7));
            AddMachine(B, "dron", Pt(3, 0));
            AssertUndoRestores(B, b => Baskin.DoAct(b, BySlot(b, 1).Id, 3, 1), "EMP makine");
            AssertUndoRestores(B, b => Baskin.DoAct(b, BySlot(b, 1).Id, 3, 6), "EMP ekip");
            ActPreviewedAbility(B, BySlot(B, 1), Pt(3, 1));
            Assert.IsTrue(B.FxList.Any(f => f.K == "fall"));
            // Ekip üyesinin düşmesi de önizlemeyle aynı
            B = Mid(Pt(1, 6), Pt(3, 3), Pt(3, 0));
            ActPreviewedAbility(B, BySlot(B, 1), Pt(3, 1));
            Assert.IsTrue(BySlot(B, 2).Overboard);
            Assert.IsTrue(Baskin.Undo(B));
            Assert.IsFalse(Fresh(B, BySlot(B, 2)).Overboard, "geri alma ekip üyesini getirir");
            // Sabotaj
            B = Mid(Pt(4, 6), Pt(1, 1), Pt(6, 1));
            AddMachine(B, "dron", Pt(4, 7));
            AssertUndoRestores(B, b => Baskin.DoAct(b, BySlot(b, 0).Id, 4, 7), "sabotaj");
            ActPreviewedAbility(B, BySlot(B, 0), Pt(4, 7));
            // Koşu darbesinin ikinci itmesi
            B = Mid(Pt(1, 6), Pt(2, 6), Pt(6, 6));
            AddMachine(B, "dron", Pt(6, 1));
            AssertUndoRestores(B, b => Baskin.DoAction(b, BySlot(b, 2).Id, Baskin.ActRush, 6, 1), "koşu");
            ActPreviewed(B, BySlot(B, 2), Baskin.ActRush, Pt(6, 1));
            Assert.IsTrue(B.FxList.Any(f => f.K == "fall"));
        }

        // ---------- 10. Takip ----------
        /// <summary>H'nin batı geçidinin yanında üç makine: 1 ve 2 adımdaki takip eder, 3 adımdaki etmez; duvar arkasındaki (Manhattan
        /// 2, yürüyüş daha uzun) etmez.</summary>
        static (Baskin.Unit Near, Baskin.Unit Two, Baskin.Unit Far, Baskin.Unit Behind) FollowSetup(Baskin.BState B)
        {
            var w = B.Room.West.OrderBy(t => t.Y).ToList();
            int wy = w[0].Y, wy2 = w[w.Count - 1].Y;
            var near = AddMachine(B, "dron", Pt(1, wy));
            var two = AddMachine(B, "bakim", Pt(2, wy), 2);
            var far = AddMachine(B, "dron", Pt(3, wy));
            // Duvar arkası: geçidin öbür ucunun dışında, duvarla ayrılmış
            int by = wy2 + 1 < Baskin.N ? wy2 + 1 : wy - 1, sy = by > wy2 ? 1 : -1;
            int gy = sy > 0 ? wy2 : wy;
            Baskin.CellAt(B, 0, by).T = "wall";
            Baskin.CellAt(B, 1, gy).T = "wall";
            var behind = AddMachine(B, "dron", Pt(1, by));
            Assert.LessOrEqual(Math.Abs(1 - 0) + Math.Abs(by - gy), 2, "Manhattan ≤ 2");
            return (near, two, far, behind);
        }

        [Test]
        public void Katar_Follow_EntersNextRoom_NearEastGate_NotClean()
        {
            var reps = new List<RoomReport>();
            var p = P();
            var H = EscapeH(p, reps, Pt(5, 5), Pt(6, 5), Pt(6, 6));
            var (near, two, far, behind) = FollowSetup(H);
            CollectionAssert.AreEqual(new[] { near.Id, two.Id }, Baskin.FollowerIds(H));
            ToGate(H);
            Assert.IsTrue(Baskin.Pass(H));
            var r = File(p, reps, H);
            Assert.AreEqual(4, r.Machines.Count);
            CollectionAssert.AreEqual(new[] { 0, 1 }, r.Follow);
            Assert.IsFalse(r.Decouple);
            Assert.IsFalse(r.Last, "takip A'yı oynatır");
            Assert.IsTrue(RaidChain.IsClean(RaidChain.LastVisit(reps, 2)), "A temizdi");
            Assert.AreEqual(new RoomStep(2, true), RaidChain.Next(p, reps).Value);
            // Yerleşim planlamadan önce (ReturnBoard; CreateRoom'un planı makineleri yürütür)
            var aRoom = NaWing()[1];
            var A = Baskin.ReturnBoard(p, aRoom, RaidChain.Setup(B0, aRoom, p, reps, true));
            Assert.IsTrue(A.Escape);
            var foes = A.Units.Where(u => u.Side == 'e' && !u.Dead).ToList();
            CollectionAssert.AreEqual(new[] { "dron", "bakim" }, foes.Select(u => u.Type).ToArray(), "takipçiler raporun sırasıyla");
            // Aşama 12 kapanışı: tahtaya takip ederek gelenlerin okuyucusu (gözcü satırı bunu okur)
            CollectionAssert.AreEqual(foes.Select(u => u.Id).ToArray(), Baskin.FollowedInIds(A), "takipçi okuyucusu");
            CollectionAssert.AreEqual(Baskin.FollowedInIds(A), Baskin.FollowedInIds(A.Clone()), "kopya (geri alma, önizleme) taşır");
            Assert.IsTrue(A.Units.Where(Baskin.IsSpecialist).All(u => !u.Follower));
            Assert.AreEqual(2, foes[1].Hp, "can rapordan");
            var squad = new HashSet<(int, int)>(A.Units.Where(Baskin.IsSpecialist).Select(u => (u.X, u.Y)));
            var steps = Bfs(A, A.Room.Forward);
            foreach (var f in foes)
            {
                var c = Baskin.CellAt(A, f.X, f.Y);
                Assert.IsFalse(squad.Contains((f.X, f.Y)), "kadronun karesi");
                Assert.AreEqual(0, c.Door);
                Assert.AreEqual(0, c.Vent);
                Assert.AreEqual("floor", c.T);
                Assert.GreaterOrEqual(steps[f.Y, f.X], 0);
            }
            // Doğu geçidine en yakın boş zemin: daha yakın boş zemin kalmadı
            int worst = foes.Max(f => steps[f.Y, f.X]);
            foreach (var t in AllTiles())
            {
                var c = Baskin.CellAt(A, t.X, t.Y);
                if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || steps[t.Y, t.X] < 0 || Baskin.UnitAt(A, t.X, t.Y) != null || squad.Contains((t.X, t.Y))) continue;
                Assert.GreaterOrEqual(steps[t.Y, t.X], foes.Min(f => steps[f.Y, f.X]), "daha yakın boş kare: " + t.X + "," + t.Y);
            }
            Assert.LessOrEqual(worst, 4);

            // A'dan G'ye takip (giriş vagonu), temizken de
            Calm(A);
            var aw = A.Room.West.OrderBy(t => t.Y).First();
            var m = AddMachine(A, "dron", Pt(1, aw.Y));
            Baskin.CellAt(A, 1, aw.Y).T = "floor"; Baskin.CellAt(A, 1, aw.Y).O = null;
            CollectionAssert.AreEqual(new[] { m.Id }, Baskin.FollowerIds(A));
            ToGate(A);
            Assert.IsTrue(Baskin.Pass(A));
            var ra = File(p, reps, A);
            CollectionAssert.AreEqual(new[] { 0 }, ra.Follow);
            Assert.AreEqual(new RoomStep(1, true), RaidChain.Next(p, reps).Value);
            var gRoom = NaWing()[0];
            var G = Baskin.ReturnBoard(p, gRoom, RaidChain.Setup(B0, gRoom, p, reps, true));
            Assert.IsTrue(Baskin.IsFinalExit(G));
            var gs = Bfs(G, G.Room.Forward);
            Assert.AreEqual(1, G.Units.Count(u => u.Side == 'e' && !u.Dead && u.Type == "dron" && gs[u.Y, u.X] >= 0 && gs[u.Y, u.X] <= 3), "giriş vagonunun doğu geçidinde takipçi");
            Assert.AreEqual(1, Baskin.FollowedInIds(G).Count, "giriş vagonunda tek takipçi");
            Assert.AreEqual("dron", G.Units.Single(u => u.Id == Baskin.FollowedInIds(G)[0]).Type);
            Assert.IsNotNull(Open(p, reps), "giriş vagonu oynanır");
        }

        static IEnumerable<Baskin.Pt> AllTiles()
        {
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) yield return new Baskin.Pt(x, y);
        }

        /// <summary>Testin kendi BFS'i: karelerden yürüyüş uzaklığı (duvar ve nesne geçilmez).</summary>
        static int[,] Bfs(Baskin.BState B, IEnumerable<Baskin.Pt> from)
        {
            var d = new int[Baskin.N, Baskin.N];
            for (int y = 0; y < Baskin.N; y++) for (int x = 0; x < Baskin.N; x++) d[y, x] = -1;
            var q = new Queue<Baskin.Pt>();
            foreach (var f in from) { d[f.Y, f.X] = 0; q.Enqueue(f); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var dd in Baskin.Dirs)
                {
                    int x = p.X + dd[0], y = p.Y + dd[1];
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N || d[y, x] >= 0) continue;
                    var c = Baskin.CellAt(B, x, y);
                    if (c.T == "wall" || c.O != null) continue;
                    d[y, x] = d[p.Y, p.X] + 1;
                    q.Enqueue(new Baskin.Pt(x, y));
                }
            }
            return d;
        }

        [Test]
        public void Katar_Follow_MachineCap_AndNotOnTheWayIn()
        {
            // Makine sınırı (7): donmuş odada 6 makine varsa iki takipçiden biri gelir
            var reps = new List<RoomReport>();
            var p = P();
            var H = EscapeH(p, reps, Pt(5, 5), Pt(6, 5), Pt(6, 6));
            for (int i = 0; i < 6; i++) reps[1].Machines.Add(new RoomMachine { Type = "dron", X = 2 + i % 4, Y = 2 + i / 4, Hp = 2 });
            var (near, two, _, _) = FollowSetup(H);
            ToGate(H);
            Assert.IsTrue(Baskin.Pass(H));
            var r = File(p, reps, H);
            Assert.AreEqual(2, r.Follow.Count);
            var A = Open(p, reps);
            int frozen = A.Units.Count(u => u.Side == 'e' && !u.Dead) - 1;
            Assert.AreEqual(7, A.Units.Count(u => u.Side == 'e' && !u.Dead), "makine sınırı");
            Assert.AreEqual(6, frozen);
            // Aşama 12 kapanışı (P4b H4): okuyucu yalnız gelen takipçiyi verir; sınırın kestiği takipçi ve donmuş odanın makineleri yok
            Assert.AreEqual(1, Baskin.FollowedInIds(A).Count, "makine sınırı: tek takipçi geldi");
            Assert.AreEqual(6, A.Units.Count(u => u.Side == 'e' && !u.Dead && !u.Follower), "donmuş odanın makineleri takipçi değil");

            // Gidişte takip yok (A gidiş, batı geçidinin yanında makine)
            reps = new List<RoomReport>();
            var B = Mid(p, reps, Pt(5, 5), Pt(6, 5), Pt(6, 6));
            var w = B.Room.West[0];
            AddMachine(B, "dron", Pt(1, w.Y));
            Assert.IsEmpty(Baskin.FollowerIds(B));
            ToGate(B);
            Assert.IsTrue(Baskin.Pass(B));
            Assert.IsEmpty(File(p, reps, B).Follow);
        }

        // ---------- 11. AYIR ----------
        [Test]
        public void Katar_Decouple_ConditionsAndReasons()
        {
            var reps = new List<RoomReport>();
            var p = P();
            var H = EscapeH(p, reps, Pt(5, 5), Pt(6, 5), Pt(6, 6));
            var (near, two, _, _) = FollowSetup(H);
            var tek = BySlot(H, 0);
            Assert.AreEqual("gate", Baskin.DecoupleBlock(H), "herkes kapıda değil");
            ToGate(H);
            Assert.IsNull(Baskin.DecoupleBlock(H));
            Assert.IsTrue(Baskin.CanDecouple(H));
            tek.Acted = true;
            Assert.AreEqual("acted", Baskin.DecoupleBlock(H), "yeteneği harcandı");
            Assert.IsFalse(Baskin.CanDecouple(H));
            Assert.IsFalse(Baskin.Decouple(H));
            tek.Acted = false;
            Assert.IsTrue(Baskin.Decouple(H));
            Assert.AreEqual("pass", H.Phase);
            Assert.IsTrue(H.Decoupled);
            Assert.IsTrue(Fresh(H, tek).Acted);
            Assert.IsNotNull(FxOf(H, "decouple", tek.Id));
            Assert.IsTrue(H.Log.Any(l => l.Text == Loc.T("Vagon ayrıldı; takip yok.")));
            StringAssert.Contains("|A|R", Baskin.Signature(H));
            Assert.IsFalse(Baskin.Undo(H), "geri alınmaz");
            Assert.IsEmpty(Baskin.FollowerIds(H), "AYIR'dan sonra takip yok");
            var r = File(p, reps, H);
            Assert.IsTrue(r.Decouple);
            Assert.IsEmpty(r.Follow);
            Assert.IsTrue(r.Last, "A ve G temiz: yürüyerek çıkış");

            // Gidişte "rule"; Teknisyen düştü ya da kenardan düştü "tek"
            var B = Mid(Pt(5, 5), Pt(6, 5), Pt(6, 6));
            ToGate(B);
            Assert.AreEqual("rule", Baskin.DecoupleBlock(B));
            Assert.IsFalse(Baskin.CanDecouple(B));
            reps = new List<RoomReport>();
            H = EscapeH(p, reps, Pt(5, 5), Pt(6, 5), Pt(6, 6));
            Down(H, BySlot(H, 0));
            ToGate(H);
            Assert.AreEqual("tek", Baskin.DecoupleBlock(H));
            reps = new List<RoomReport>();
            H = EscapeH(p, reps, Pt(3, 0), Pt(3, 3), Pt(6, 6));
            Assert.IsTrue(Baskin.DoAct(H, BySlot(H, 1).Id, 3, 1));
            Assert.IsTrue(BySlot(H, 0).Overboard);
            ToGate(H);
            Assert.AreEqual("tek", Baskin.DecoupleBlock(H));
            Assert.IsFalse(Baskin.CanDecouple(H));
        }

        [Test]
        public void Katar_AutoPass_FollowWorks()
        {
            // Kendiliğinden geçiş: tahtadaki son uzman makine evresinde düşer, öbürleri kapıda; AYIR seçilemez, takip işler
            var reps = new List<RoomReport>();
            var p = P();
            var H = EscapeH(p, reps, Pt(5, 5), Pt(6, 5), Pt(6, 6));
            var w = H.Room.West.OrderBy(t => t.Y).First();
            ToGate(H, BySlot(H, 0));
            ToGate(H, BySlot(H, 1));
            var ana = BySlot(H, 2);
            Place(ana, Pt(2, w.Y));
            var m = AddMachine(H, "bakim", Pt(1, w.Y));
            m.Intent = new Baskin.Intent { Kind = "melee", Dir = 1 };
            H.StillTurns = 99;
            Assert.IsTrue(Baskin.EndTurn(H));
            Assert.IsTrue(ana.Dead);
            Assert.AreEqual("pass", H.Phase, "kendiliğinden geçiş");
            Assert.IsFalse(H.Decoupled);
            var r = File(p, reps, H);
            CollectionAssert.AreEqual(new[] { 0 }, r.Follow);
            Assert.AreEqual(new RoomStep(2, true), RaidChain.Next(p, reps).Value);
        }

        // ---------- 12. Dizgiler ----------
        [Test]
        public void RoomReport_And_RaidResult_RoundTrip()
        {
            var r = Rep(2, true);
            string plain = r.Encode();
            Assert.AreEqual(15, plain.Split('|').Length, "eski dizgi değişmez");
            r.Team[2] = new RoomMember { State = "k", Hp = 0, Fell = 2, X = 3, Y = 0 };
            r.Machines.Add(new RoomMachine { Type = "dron", X = 1, Y = 2, Hp = 2 });
            r.Machines.Add(new RoomMachine { Type = "bakim", X = 2, Y = 2, Hp = 1 });
            r.Machines.Add(new RoomMachine { Type = "dron", X = 6, Y = 2, Hp = 2 });
            string k = r.Encode();
            Assert.AreEqual(15, k.Split('|').Length, "kişi durumu yeni alan istemez");
            var d = RoomReport.Decode(k);
            Assert.AreEqual(("k", 0, 2, 3, 0), (d.Team[2].State, d.Team[2].Hp, d.Team[2].Fell, d.Team[2].X, d.Team[2].Y));
            r.Follow = new List<int> { 0, 2 };
            string f = r.Encode();
            var parts = f.Split('|');
            Assert.AreEqual(23, parts.Length);
            Assert.AreEqual("0:0:0:0:0:0", parts[21]);
            Assert.AreEqual("0,2", parts[22]);
            d = RoomReport.Decode(f);
            CollectionAssert.AreEqual(new[] { 0, 2 }, d.Follow);
            Assert.IsFalse(d.Decouple);
            Assert.AreEqual(f, d.Encode());
            r.Follow.Clear();
            r.Decouple = true;
            string a = r.Encode();
            Assert.AreEqual(24, a.Split('|').Length);
            Assert.IsTrue(a.EndsWith("||1", StringComparison.Ordinal));
            d = RoomReport.Decode(a);
            Assert.IsTrue(d.Decouple);
            Assert.IsEmpty(d.Follow);
            Assert.AreEqual(a, d.Encode());
            // Eski raporlar: kuyruk eksik, varsayılanlar
            var old = RoomReport.Decode(plain);
            Assert.IsEmpty(old.Follow);
            Assert.IsFalse(old.Decouple);

            var res = new RaidResult { Success = true, Turns = 9, Hp = new List<int> { 3, 3, 0 }, Armor = new List<int> { 0, 0, 0 }, CiviliansLeft = 2, Down = 1 };
            string legacy = res.Encode();
            Assert.AreEqual("1|0|0|1|9|3,3,0|0,0,0|2", legacy, "boş Overboard: eski dizgi");
            res.Overboard.Add(2);
            string over = res.Encode();
            Assert.AreEqual("1|0|0|1|9|3,3,0|0,0,0|2|||2", over);
            var back = RaidResult.Decode(over);
            CollectionAssert.AreEqual(new[] { 2 }, back.Overboard);
            CollectionAssert.IsEmpty(back.Left);
            CollectionAssert.IsEmpty(back.Extracted);
            Assert.AreEqual(over, back.Encode());
            CollectionAssert.IsEmpty(RaidResult.Decode(legacy).Overboard);
            res.Extracted.Add(0);
            Assert.AreEqual("1|0|0|1|9|3,3,0|0,0,0|2||0|2", res.Encode());
        }

        // ---------- 13. Savunma ----------
        [Test]
        public void Katar_Defense_EdgeClosed_NoFollow()
        {
            var p = P();
            p.Mode = "defense";
            var room = NaWing()[1];
            var B = Baskin.CreateRoom(p, room, RaidChain.Setup(B0, room, p, new List<RoomReport>(), false));
            Assert.IsTrue(B.Defense);
            Assert.IsNull(B.Ev, "savunma olay durumu kurmaz");
            Flat(B, Pt(1, 6), Pt(3, 3), Pt(6, 6));
            Assert.IsFalse(Baskin.IsOpenEdge(B, 3, 0));
            var m = AddMachine(B, "dron", Pt(3, 0));
            Assert.IsTrue(Baskin.DoAct(B, BySlot(B, 1).Id, 3, 1));
            Assert.IsFalse(m.Dead);
            Assert.AreEqual((3, 0, 2), (m.X, m.Y, m.Hp), "kuzeye itme 1 hasar");
            B.Escape = true;
            AddMachine(B, "dron", Pt(1, room.West[0].Y));
            Assert.IsEmpty(Baskin.FollowerIds(B));
            Assert.AreEqual("rule", Baskin.DecoupleBlock(B));
        }
    }
}
