using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tesis aşama 12, Yalpa (oc, "yalpa"; Docs/TESIS-BASKINLARI-TASARIMI.md §10.2): kimlik, takvim (2., 4., 6. tur sonu; batı, doğu,
    /// batı; bütçe içinde), kayanlar ve sabitler, çarpma hasarı ve tavanı, cıvata, kapı ve menfez, pusu ve tutuş, RNG, işaretler,
    /// efektler ve makine aklı. Tahta düzeyi (plan 79, oc_1 kanadı); düz zemin, makinesiz kurulum.
    /// </summary>
    public class FacilityTiltTests
    {
        static readonly Balance B0 = Bal();
        static IReadOnlyList<FacilityRoom> OcWing => Wing("oc_1");
        static RaidParams P(int seed = 13) => Params(OcWing, seed);
        static Baskin.Pt Pt(int x, int y) => new Baskin.Pt(x, y);

        /// <summary>Ara oda (A, Yalpa) gidiş tahtası: düz zemin, makinesiz, 5 turluk bütçe, makineler yürümez, verilen tur.</summary>
        static Baskin.BState Mid(RaidParams p, int turn, params Baskin.Pt[] spots)
        {
            var B = Open(p, new List<RoomReport> { Rep(1) });
            Assert.AreEqual(2, B.Room.Index);
            Assert.AreEqual("yalpa", B.Ev?.Id, "A Yalpa odası");
            Flat(B, spots);
            B.MaxTurns = 5;
            B.StillTurns = 99;
            B.Turn = turn;
            return B;
        }

        static Baskin.BState Mid(int turn, params Baskin.Pt[] spots) => Mid(P(), turn, spots);

        static void Obj(Baskin.BState B, Baskin.Pt at, string k, int hp = 1) => Baskin.CellAt(B, at.X, at.Y).O = new Baskin.Obj { K = k, Hp = hp, Max = hp };

        /// <summary>Yalnız olay adımı (makine evresi yok): efektler temizlenir.</summary>
        static void Step(Baskin.BState B)
        {
            B.FxList.Clear();
            Baskin.TestEventStep(B);
        }

        static List<Baskin.Fx> Slides(Baskin.BState B) => B.FxList.Where(f => f.K == "slide").ToList();

        // ---------- 1. Kimlik ----------
        [Test]
        public void Yalpa_Identity()
        {
            foreach (var r in Plan79.Region("oc"))
                Assert.AreEqual(r.Type == RoomType.Entry ? null : "yalpa", r.Event, r.Id);
            FacilityMaps.ClearCache();
            foreach (var r in FacilityMaps.For(79, true, false).Region("oc"))
                Assert.AreEqual(r.Type == RoomType.Entry ? null : "yag", r.Event, "alt bayrak kapalı " + r.Id);
            FacilityMaps.ClearCache();
            Assert.AreEqual("yalpa", FacilityMaps.EventFor(GameData.Region("oc"), false, true));
            Assert.AreEqual("yag", FacilityMaps.EventFor(GameData.Region("oc"), true, false));
            Assert.IsEmpty(Baskin.OpenEdgeRows(Plan79.Room("oc_1.2")), "Yalpa'da açık kenar yok");
        }

        // ---------- 2. Takvim ----------
        [Test]
        public void Yalpa_Tilt_Schedule_WestEastWest()
        {
            CollectionAssert.AreEqual(new[] { (2, -1), (4, 1) }, Baskin.TiltSchedule(5));
            CollectionAssert.AreEqual(new[] { (2, -1), (4, 1), (6, -1) }, Baskin.TiltSchedule(8));
            CollectionAssert.AreEqual(new[] { (2, -1) }, Baskin.TiltSchedule(4));
            var B = Mid(1, Pt(3, 3), Pt(3, 5), Pt(4, 1));
            Assert.IsTrue(Baskin.TiltAt(B, 2) && Baskin.TiltAt(B, 4));
            Assert.IsFalse(Baskin.TiltAt(B, 1) || Baskin.TiltAt(B, 3) || Baskin.TiltAt(B, 6), "bütçeyi aşan tur");
            B.MaxTurns = 4;
            Assert.IsFalse(Baskin.TiltAt(B, 4), "son turda yatış yok");
            B.MaxTurns = 5;
            // Gerçek tur sonları: 2. turda batı, 4. turda doğu
            var tilts = new List<(int Turn, int Dir)>();
            for (int t = 1; t <= 4; t++)
            {
                Assert.AreEqual(t, B.Turn);
                Assert.IsTrue(Baskin.EndTurn(B));
                foreach (var f in B.FxList.Where(f => f.K == "ev" && f.Obj == "yalpa")) tilts.Add((t, f.Dx));
            }
            CollectionAssert.AreEqual(new[] { (2, -1), (4, 1) }, tilts);
            Assert.IsTrue(B.Log.Any(l => l.Text == Loc.T("Platform batıya yattı.")));
            Assert.IsTrue(B.Log.Any(l => l.Text == Loc.T("Platform doğuya yattı.")));
            // Dönüş odasının sayacı 1'den: 6 turda 2 ve 4 (6. tur son tur, yatış yok)
            var room = OcWing[1];
            var p = P();
            var reps = new List<RoomReport> { Rep(1), Rep(2), Rep(3) };
            var R = Baskin.CreateRoom(p, room, RaidChain.Setup(B0, room, p, reps, true));
            Assert.AreEqual((1, B0.facilityReturnTurns), (R.Turn, R.MaxTurns));
            var hv = Baskin.Hazards(R);
            Assert.AreEqual(("←", "→"), (hv.Turns[2], hv.Turns[4]));
            Assert.IsNull(hv.Turns[3]);
            // Hedef odası (10 tur; sürüm 10 telafisinden önce 9, tek kip denge turundan önce 8): 6. tur da
            var H = Open(p, new List<RoomReport> { Rep(1), Rep(2) });
            Assert.AreEqual(B0.facilityTargetTurns, H.MaxTurns);
            Assert.AreEqual(10, H.MaxTurns);
            Assert.AreEqual("←", Baskin.Hazards(H).Turns[6]);
        }

        // ---------- 3. Kayanlar ve sabitler ----------
        [Test]
        public void Yalpa_Tilt_SlidesUnitsBodiesCargoScrapGadgetsDog_ObjectsStay()
        {
            var B = Mid(2, Pt(3, 3), Pt(2, 5), Pt(6, 1));
            var tek = BySlot(B, 0);
            var emp = BySlot(B, 1);
            var ana = BySlot(B, 2);
            Down(B, ana);
            Obj(B, Pt(1, 5), "civ");
            var dron = AddMachine(B, "dron", Pt(5, 6));
            B.CargoX = 6; B.CargoY = 2;
            var g = AddGadget(B, "taret", Pt(6, 4), Baskin.Dirs[0]);
            var dog = AddDog(B, 0, Pt(6, 5));
            Obj(B, Pt(4, 7), "hurda");
            Obj(B, Pt(6, 7), "tel", 3);
            Obj(B, Pt(1, 0), "yayici");
            Obj(B, Pt(7, 0), "vana", 2);
            Step(B);
            Assert.AreEqual((2, 3), (tek.X, tek.Y));
            Assert.AreEqual((2, 5, 2), (emp.X, emp.Y, emp.Hp), "sivile çarptı: 1 hasar, yerinde");
            Assert.AreEqual((5, 1), (ana.X, ana.Y), "gövde kayar");
            Assert.AreEqual((4, 6), (dron.X, dron.Y));
            Assert.AreEqual((5, 2), (B.CargoX, B.CargoY), "yerdeki yük kayar");
            Assert.AreEqual((5, 4), (g.X, g.Y), "aygıt kayar");
            Assert.AreEqual((5, 5), (dog.X, dog.Y), "köpek kayar");
            Assert.AreEqual("hurda", Baskin.CellAt(B, 3, 7).O?.K, "hurda kayar");
            Assert.IsNull(Baskin.CellAt(B, 4, 7).O);
            Assert.AreEqual(("civ", 1), (Baskin.CellAt(B, 1, 5).O.K, Baskin.CellAt(B, 1, 5).O.Hp), "sivil sabit ve hasarsız");
            Assert.AreEqual("tel", Baskin.CellAt(B, 6, 7).O?.K, "örülmüş engel sabit");
            Assert.AreEqual("yayici", Baskin.CellAt(B, 1, 0).O?.K, "yayıcı sabit");
            Assert.AreEqual("vana", Baskin.CellAt(B, 7, 0).O?.K, "donanım sabit");
        }

        [Test]
        public void Yalpa_Scrap_StaysIfItWouldCutTheTeamOff()
        {
            // Doğu yarı batı geçidine yalnız x = 2 koridorundan bağlı (kuzeyde (3,0), güneyde (1,6) ağzı); hurda koridorun yanındaki cepte
            var B = Mid(2, Pt(6, 5), Pt(6, 6), Pt(6, 7));
            for (int y = 0; y < Baskin.N; y++)
            {
                if (y != 6) Baskin.CellAt(B, 1, y).T = "wall";
                if (y != 0 && y != 3) Baskin.CellAt(B, 3, y).T = "wall";
            }
            Obj(B, Pt(3, 3), "hurda");
            Step(B);
            Assert.AreEqual("hurda", Baskin.CellAt(B, 3, 3).O?.K, "koridoru kesecek hurda yerinde kalır");
            Assert.IsNull(Baskin.CellAt(B, 2, 3).O);
            Assert.IsFalse(Slides(B).Any(f => f.Obj == "hurda"));
        }

        // ---------- 4. Çarpma ve tavan ----------
        [Test]
        public void Yalpa_Tilt_CrashDamage_CappedOnePerStep()
        {
            var B = Mid(2, Pt(0, 3), Pt(0, 5), Pt(1, 5));
            var tek = BySlot(B, 0);
            var emp = BySlot(B, 1);
            var ana = BySlot(B, 2);
            var d1 = AddMachine(B, "dron", Pt(4, 2));
            Baskin.CellAt(B, 3, 2).T = "cable";
            var d2 = AddMachine(B, "dron", Pt(5, 6));
            Obj(B, Pt(4, 6), "civ");
            int team0 = B.EvDamageTeam;
            Step(B);
            Assert.AreEqual((0, 3, 2), (tek.X, tek.Y, tek.Hp), "tahta kenarı (batı duvar): 1 hasar");
            Assert.AreEqual((0, 5, 2), (emp.X, emp.Y, emp.Hp), "öndeki çarptı; arkadaki ona çarpınca ikinci hasarı almaz (tavan)");
            Assert.AreEqual((1, 5, 1), (ana.X, ana.Y, ana.Hp), "arkadaki öndekine çarptı: 1 hasar");
            Assert.AreEqual((3, 2, 2), (d1.X, d1.Y, d1.Hp), "kabloya kayan yanar");
            Assert.AreEqual((5, 6, 2), (d2.X, d2.Y, d2.Hp), "nesneye çarpan 1 hasar");
            Assert.AreEqual(1, Baskin.CellAt(B, 4, 6).O.Hp, "nesne hasar almaz");
            Assert.AreEqual(team0 + 3, B.EvDamageTeam, "olay hasarı");
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 0 }, Slides(B).Select(f => f.N).ToArray(), "yokuş aşağı sıra: tek, emp, ana, d1, d2");
            Assert.IsEmpty(B.Ev.Hurt, "tavan adımın sonunda sıfırlanır");
        }

        // ---------- 5. Cıvata ve önlem defteri ----------
        [Test]
        public void Yalpa_BoltedMachine_DoesNotSlide_NoBoltFx()
        {
            var p = P();
            p.Counters = new List<int> { 1, 0, 0, 0, 0, 0 };
            var B = Mid(p, 2, Pt(3, 3), Pt(3, 5), Pt(4, 1));
            var z = AddMachine(B, "zdron", Pt(5, 4));
            var d = AddMachine(B, "dron", Pt(5, 6));
            Assert.IsTrue(Baskin.Bolted(B, z));
            Step(B);
            Assert.AreEqual((5, 4, 3), (z.X, z.Y, z.Hp));
            Assert.IsFalse(B.FxList.Any(f => f.K == "bolted"), "sessiz tutar");
            Assert.IsFalse(B.Log.Any(l => l.Text.Contains(Loc.F("{0} cıvatalı: yerinden oynamadı.", Baskin.UnitTypes["zdron"].Name))));
            Assert.IsFalse(Slides(B).Any(f => f.Unit == z.Id), "cıvatalının hattı yok");
            Assert.AreEqual((4, 6), (d.X, d.Y));
        }

        [Test]
        public void Yalpa_Tilt_NotCountedAsPush()
        {
            var p = P();
            p.Counters = new List<int> { 0, 0, 0, 0, 0, 0 };
            var B = Mid(p, 2, Pt(3, 3), Pt(3, 5), Pt(4, 1));
            Assert.IsTrue(B.Moves.Tally);
            AddMachine(B, "dron", Pt(5, 6));
            AddMachine(B, "dron", Pt(0, 6));
            Step(B);
            Assert.GreaterOrEqual(Slides(B).Count, 4);
            CollectionAssert.AreEqual(new int[6], B.TallyArray(), "kayma itme sayılmaz");
        }

        // ---------- 6. Kapı ve menfez ----------
        [Test]
        public void Yalpa_BodiesAndCargo_NotOntoGateOrVent_GateUnitsStay()
        {
            var B = Mid(2, Pt(5, 5), Pt(5, 6), Pt(6, 6));
            StartEscape(B);
            var w = B.Room.West.OrderBy(t => t.Y).ToList();
            var tek = BySlot(B, 0);
            var emp = BySlot(B, 1);
            var ana = BySlot(B, 2);
            ToGate(B, tek);
            var gate = (tek.X, tek.Y);
            Assert.AreEqual((w[0].X, w[0].Y), gate);
            // Kapı karesinin yanında yatan gövde ve menfezin yanında yerdeki yük
            Place(ana, Pt(1, w[0].Y));
            Down(B, ana);
            Baskin.CellAt(B, 2, 1).Vent = 1;
            B.Carrier = 0;
            B.CargoX = 3; B.CargoY = 1;
            Place(emp, Pt(1, w[1].Y));
            Step(B);
            Assert.AreEqual((1, w[0].Y), (ana.X, ana.Y), "gövde kapı karesine kaymaz");
            Assert.AreEqual((3, 1), (B.CargoX, B.CargoY), "yük menfeze kaymaz");
            Assert.AreEqual(gate, (tek.X, tek.Y), "kapıdaki uzman kaymaz");
            Assert.IsTrue(tek.AtGate);
            Assert.AreEqual((0, w[1].Y), (emp.X, emp.Y), "geçit karesine kayar");
            Assert.IsFalse(emp.AtGate, "kayan uzman kapıda sayılmaz");
            Assert.IsTrue(Baskin.OnBoard(emp));
        }

        // ---------- 7. Pusu ve tutuş ----------
        [Test]
        public void Yalpa_Ambush_FiresFromSetSquare()
        {
            var B = Mid(2, Pt(5, 1), Pt(6, 1), Pt(3, 3));
            var ana = BySlot(B, 2);
            ana.AmbushDir = 2; ana.AmbushX = 3; ana.AmbushY = 3;
            var d = AddMachine(B, "dron", Pt(3, 5));
            Obj(B, Pt(2, 5), "civ");
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual((2, 3), (ana.X, ana.Y), "pusucu kaydı");
            Assert.AreEqual((3, 5), (d.X, d.Y), "makine sivile çarptı, yerinde");
            Assert.IsTrue(B.FxList.Any(f => f.K == "ambush-hit" && f.Unit == d.Id), "hat kurulduğu kareden ateşlenir");
            Assert.AreEqual(1, d.Hp, "çarpma 1 + pusu 1");
        }

        [Test]
        public void Yalpa_Hold_SameAsPushed()
        {
            foreach (int turn in new[] { 1, 2 })
            {
                var B = Mid(turn, Pt(3, 3), Pt(3, 5), Pt(4, 1));
                var tek = BySlot(B, 0);
                B.HoldId = tek.Id; B.HoldX = 2; B.HoldY = 3;
                Assert.IsTrue(Baskin.EndTurn(B));
                Assert.AreEqual((0, -1, -1), (B.HoldId, B.HoldX, B.HoldY), "tutuş tur sonunda boşa gider (yatıştan önce) " + turn);
            }
        }

        // ---------- 8. RNG ----------
        [Test]
        public void Yalpa_DrawsNoRandomness()
        {
            var B = Mid(2, Pt(3, 3), Pt(3, 5), Pt(4, 1));
            AddMachine(B, "dron", Pt(5, 6));
            Obj(B, Pt(4, 7), "hurda");
            int rs = B.Rs;
            Baskin.Hazards(B);
            Baskin.Slides(B);
            Assert.AreEqual(rs, B.Rs);
            Step(B);
            Assert.AreEqual(rs, B.Rs, "yatış çekim yapmaz");
        }

        // ---------- 9. İşaretler ----------
        [Test]
        public void Yalpa_Hazards()
        {
            // Önceki tur: soluk oklar batı kenarında, "YALPA 2"
            var B = Mid(1, Pt(3, 3), Pt(1, 5), Pt(4, 1));
            Obj(B, Pt(0, 5), "civ");
            var hv = Baskin.Hazards(B);
            Assert.AreEqual(6, hv.Turns.Length);
            Assert.AreEqual(("←", "→"), (hv.Turns[2], hv.Turns[4]));
            Assert.IsNull(hv.Turns[1] ?? hv.Turns[3] ?? hv.Turns[5]);
            Assert.IsNull(hv.Next);
            Assert.AreEqual(8, hv.Edges.Count(e => e.Kind == "tilt_next" && e.X == 0 && e.X2 == -1));
            Assert.AreEqual(Loc.F("YALPA {0}", 2), hv.Marks.Single(m => m.Kind == "tilt_next").Label);
            Assert.IsEmpty(hv.Lines);
            // Yatış turu: kayma hatları = Slides = uygulamanın sonucu; koyu oklar, "YALPA 1"; uyarılar
            B.Turn = 2;
            var tek = BySlot(B, 0);
            var emp = BySlot(B, 1);
            hv = Baskin.Hazards(B);
            Assert.AreEqual("←", hv.Next);
            Assert.AreEqual(8, hv.Edges.Count(e => e.Kind == "tilt" && e.X == 0));
            Assert.AreEqual(Loc.F("YALPA {0}", 1), hv.Marks.Single(m => m.Kind == "tilt").Label);
            string Line(Baskin.HazardLine l) => $"{l.Kind}{l.X}{l.Y}>{l.X2}{l.Y2}:{l.Hit}";
            var predicted = hv.Lines.Select(Line).ToList();
            CollectionAssert.AreEqual(Baskin.Slides(B).Select(Line), predicted);
            Assert.Contains("slide33>23:False", predicted);
            Assert.Contains("slide15>05:True", predicted, "sivile çarpan: ucu kırmızı");
            string tag = Loc.T(Baskin.RegionEvents["yalpa"].Tag);
            Assert.AreEqual(Loc.F("{0}: yer değiştirir", tag), hv.Warnings.Single(x => x.Unit == tek.Id).Text);
            Assert.AreEqual(Loc.F("{0}: {1} hasar", tag, 1), hv.Warnings.Single(x => x.Unit == emp.Id).Text);
            Assert.IsTrue(Baskin.EndTurn(B));
            var actual = B.FxList.Where(f => f.K == "slide").Select(f => $"slide{f.X}{f.Y}>{f.X + f.Dx}{f.Y + f.Dy}:{f.N == 0}").ToList();
            CollectionAssert.AreEqual(predicted, actual, "önizleme = uygulama");
            // 3. tur: sonraki yatış doğuya (4. tur), doğu kenarında soluk oklar
            hv = Baskin.Hazards(B);
            Assert.AreEqual(3, B.Turn);
            Assert.AreEqual(8, hv.Edges.Count(e => e.Kind == "tilt_next" && e.X == Baskin.N - 1 && e.X2 == Baskin.N));
        }

        // ---------- 10. Efektler ----------
        [Test]
        public void Yalpa_Fx()
        {
            var B = Mid(2, Pt(3, 3), Pt(1, 5), Pt(4, 1));
            Obj(B, Pt(0, 5), "civ");
            Obj(B, Pt(5, 7), "hurda");
            B.CargoX = 6; B.CargoY = 2;
            Assert.IsTrue(Baskin.EndTurn(B));
            var ev = B.FxList.Where(f => f.K == "ev" && f.Obj == "yalpa").ToList();
            Assert.AreEqual(1, ev.Count);
            Assert.AreEqual((-1, -1, -1), (ev[0].Dx, ev[0].X, ev[0].Y));
            var sl = Slides(B);
            Assert.AreEqual(5, sl.Count, "üç uzman, hurda, yük");
            Assert.AreEqual(0, sl.Single(f => f.Unit == BySlot(B, 1).Id).N, "çarpan: N 0");
            Assert.AreEqual(1, sl.Single(f => f.Obj == "hurda").N);
            Assert.AreEqual(1, sl.Single(f => f.Obj == "cargo").N);
            Assert.IsFalse(B.FxList.Any(f => f.K == "push"), "push yazılmaz");
            // 4. tur doğuya
            B.Turn = 4;
            Assert.IsTrue(Baskin.EndTurn(B));
            Assert.AreEqual(1, B.FxList.Single(f => f.K == "ev" && f.Obj == "yalpa").Dx);
        }

        // ---------- 11. Makine aklı ----------
        [Test]
        public void Yalpa_MachineMind_AvoidsCrashSquareOnTiltTurn()
        {
            var B = Mid(2, Pt(3, 3), Pt(3, 5), Pt(4, 1));
            var e = AddMachine(B, "dron", Pt(5, 6));
            Baskin.CellAt(B, 4, 2).T = "wall";
            Assert.AreEqual(-0.5, Baskin.TestEventPos(B, e, Pt(0, 6)), "yokuş aşağısı tahta kenarı");
            Assert.AreEqual(-0.5, Baskin.TestEventPos(B, e, Pt(5, 2)), "yokuş aşağısı duvar");
            Assert.AreEqual(0.0, Baskin.TestEventPos(B, e, Pt(5, 5)), "açık");
            B.Turn = 3;
            Assert.AreEqual(0.0, Baskin.TestEventPos(B, e, Pt(0, 6)), "yatış turu değil");
            B.Turn = 4;
            Assert.AreEqual(-0.5, Baskin.TestEventPos(B, e, Pt(7, 6)), "doğu yatışı: doğu kenarı");
            Assert.AreEqual(0.0, Baskin.TestEventPos(B, e, Pt(0, 6)));
        }
    }
}
