using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using static Corrigible.Desk.Tests.FacilityMovesTestKit;
using static Corrigible.Desk.Tests.FacilityGearTestKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Aygıt hatlarının önizleme eşliği (aşama 9; tasarım §4, §11.2): Core'un taret, vinç ve yem kuralı private olduğu için sunum
    /// (<see cref="FacilityGearView"/>) onu public parçalarla yeniden kurar; bu testler kopyayı Core'un gerçek makine evresiyle
    /// (<see cref="Baskin.EndTurn"/>, kopyada) kilitler: 60 rastgele tesis tahtasında tek aygıt kurulur, evrenin turret-hit / winch-pull /
    /// decoy-step efektlerinin kareleri önizlemenin ilk vuracağı, çekeceği ve yürüyeceği karelerle aynı. Ayrıca tel atışı geçirir, kalas
    /// keser, bulut taret hattını keser, turnike yemin adımını durdurur; kelepçe önizlemesinde hedefin niyeti düşer; havlama erimi
    /// Manhattan ≤ BarkRange. Uyumsuzlukta Core değil sunum düzeltilir. Sahnesiz.
    /// </summary>
    public class FacilityGearViewTests
    {
        static readonly string[] Types = { "taret", "vinc", "yem" };

        /// <summary>Makine evresini kopyada koşturur: niyetsiz makineler, uyanma yok; son tur (olay adımı ve yeni plan çalışmaz,
        /// aygıtlar yine çalışır). Evrenin efektleri.</summary>
        static List<Baskin.Fx> Phase(Baskin.BState B)
        {
            var C = B.Clone();
            foreach (var e in C.Units.Where(u => u.Side == 'e')) { e.Intent = null; e.Idle = false; }
            C.Spawns.Clear(); C.SpawnPerTurn = 0;
            C.Turn = C.MaxTurns;
            Assert.IsTrue(Baskin.EndTurn(C), "evre koşmadı");
            return C.FxList.ToList();
        }

        static List<Baskin.Pt> OpenTiles(Baskin.BState B) =>
            Enumerable.Range(0, 64).Select(i => new Baskin.Pt(i % 8, i / 8)).Where(p => Open(B, p.X, p.Y)).ToList();

        /// <summary>Taret, vinç ve yemin önizlemesi Core'un evresiyle aynı kareleri gösterir (60 rastgele tahta, her türden 20).</summary>
        [Test]
        public void Lines_MatchCoreMachinePhase_SixtyBoards()
        {
            var rng = new System.Random(19);
            var rooms = new[] { Room(1).B, Room(2).B, Room(3).B };
            foreach (var r in rooms) { Floor(r); Park(r); }
            int boards = 0, hits = 0, pulls = 0, steps = 0;
            for (int n = 0; boards < 60 && n < 400; n++)
            {
                var B = rooms[n % 3].Clone();
                // Bölge olayı yok (yağ kaydırması ve kanal hasarı önizlemenin konusu değil; turnike ve bulut ayrı testte)
                B.Ev = null;
                // Rastgele duvar, tel, kalas; makineler, yatan gövde ve yerdeki yük
                for (int k = 0; k < 12; k++)
                {
                    int x = rng.Next(8), y = rng.Next(8);
                    var c = Baskin.CellAt(B, x, y);
                    if (c.Door != 0 || c.Vent != 0 || c.Crate || c.Shutter || c.O != null || Baskin.UnitAt(B, x, y) != null) continue;
                    switch (rng.Next(3)) { case 0: c.T = "wall"; break; case 1: c.O = new Baskin.Obj { K = "tel", Hp = 1, Max = 1 }; break; default: c.O = new Baskin.Obj { K = "kalas", Hp = 3, Max = 3 }; break; }
                }
                var open = OpenTiles(B);
                if (open.Count < 8) continue;
                for (int k = 0; k < 3; k++) { var p = open[rng.Next(open.Count)]; if (Baskin.UnitAt(B, p.X, p.Y) == null) AddMachine(B, k == 2 ? "bakim" : "dron", p.X, p.Y); }
                open = OpenTiles(B);
                if (rng.Next(3) == 0 && open.Count > 0) { var p = open[rng.Next(open.Count)]; B.CargoX = p.X; B.CargoY = p.Y; }
                open = OpenTiles(B).Where(p => B.CargoX != p.X || B.CargoY != p.Y).ToList();
                if (open.Count == 0) continue;
                string type = Types[boards % 3];
                var at = open[rng.Next(open.Count)];
                int dir = rng.Next(4);
                var g = AddGadget(B, type, at.X, at.Y, dir, 3, 0);
                // Taret ve vinçte tahtaların yarısında hattın 2–3. karesine makine (vuruş ve çekiş sınansın)
                if (type != "yem" && rng.Next(2) == 0)
                {
                    int k = 2 + rng.Next(2);
                    int mx = at.X + Baskin.Dirs[dir][0] * k, my = at.Y + Baskin.Dirs[dir][1] * k;
                    if (Open(B, mx, my) && (B.CargoX != mx || B.CargoY != my)) AddMachine(B, "dron", mx, my);
                }
                var fx = Phase(B);
                switch (type)
                {
                    case "taret":
                    {
                        var (line, hit) = FacilityGearView.TurretLine(B, g.X, g.Y, dir);
                        var shot = fx.FirstOrDefault(f => f.K == "turret-hit");
                        if (hit < 0) Assert.IsNull(shot, Where(B, g) + ": önizleme vuruş göstermiyor, Core vurdu");
                        else
                        {
                            Assert.IsNotNull(shot, Where(B, g) + ": önizleme vuruş gösteriyor, Core vurmadı");
                            Assert.AreEqual((line[hit].X, line[hit].Y), (shot.X, shot.Y), Where(B, g) + ": vurulan kare");
                            hits++;
                        }
                        break;
                    }
                    case "vinc":
                    {
                        var (line, hit, unit) = FacilityGearView.WinchLine(B, g.X, g.Y, dir);
                        var pull = fx.Where(f => f.K == "winch-pull").ToList();
                        var end = hit >= 0 ? FacilityGearView.PullEnd(B, line[hit], dir, unit == null) : default;
                        bool moves = hit >= 0 && (end.X != line[hit].X || end.Y != line[hit].Y);
                        Assert.AreEqual(moves, pull.Count > 0, Where(B, g) + ": çekiş var mı");
                        if (moves)
                        {
                            Assert.AreEqual((line[hit].X, line[hit].Y), (pull[0].X, pull[0].Y), Where(B, g) + ": çekilen kare");
                            Assert.AreEqual((-Baskin.Dirs[dir][0], -Baskin.Dirs[dir][1]), (pull[0].Dx, pull[0].Dy), Where(B, g) + ": vince doğru");
                            int dist = System.Math.Abs(end.X - line[hit].X) + System.Math.Abs(end.Y - line[hit].Y);
                            Assert.AreEqual(dist, pull.Select(f => (f.X, f.Y)).Distinct().Count(), Where(B, g) + ": adım sayısı = önizlemenin varışı");
                            pulls++;
                        }
                        break;
                    }
                    case "yem":
                    {
                        var path = FacilityGearView.DecoyPath(B, g.X, g.Y, dir, 1, g);
                        var walk = fx.Where(f => f.K == "decoy-step").Select(f => (f.X + f.Dx, f.Y + f.Dy)).ToList();
                        CollectionAssert.AreEqual(path.Skip(1).Select(p => (p.X, p.Y)).ToList(), walk, Where(B, g) + ": yemin yolu");
                        steps += walk.Count;
                        break;
                    }
                }
                boards++;
            }
            Assert.AreEqual(60, boards, "60 tahta kurulmalı");
            Assert.Greater(hits, 0, "en az bir taret vuruşu sınandı");
            Assert.Greater(pulls, 0, "en az bir vinç çekişi sınandı");
            Assert.Greater(steps, 0, "en az bir yem adımı sınandı");
            TestContext.WriteLine("tahta " + boards + " · vuruş " + hits + " · çekiş " + pulls + " · yem adımı " + steps);
        }

        static string Where(Baskin.BState B, Baskin.Unit g) => g.Type + " " + g.X + "," + g.Y + " yön " + g.Dir + " · oda " + B.Room?.Id;

        /// <summary>Pafta 1, düz zemin, ekip köşede, makineler kalkmış.</summary>
        static Baskin.BState Flat()
        {
            var (_, _, B) = Room(1);
            Floor(B); Park(B);
            B.Ev = null;
            return B;
        }

        [Test]
        public void Turret_WirePasses_PlankStops_CloudStops()
        {
            var B = Flat();
            var g = AddGadget(B, "taret", 1, 1, 1, 3, 0);   // doğuya
            var m = AddMachine(B, "dron", 4, 1);
            Baskin.CellAt(B, 2, 1).O = new Baskin.Obj { K = "tel", Hp = 1, Max = 1 };
            var (line, hit) = FacilityGearView.TurretLine(B, g.X, g.Y, g.Dir);
            Assert.GreaterOrEqual(hit, 0, "tel atışı geçirir");
            Assert.AreEqual((4, 1), (line[hit].X, line[hit].Y));
            Assert.AreEqual((4, 1), Phase(B).Where(f => f.K == "turret-hit").Select(f => (f.X, f.Y)).Single(), "Core da vurur");

            Baskin.CellAt(B, 2, 1).O = new Baskin.Obj { K = "kalas", Hp = 3, Max = 3 };
            (line, hit) = FacilityGearView.TurretLine(B, g.X, g.Y, g.Dir);
            Assert.AreEqual(-1, hit, "kalas keser");
            Assert.IsEmpty(line, "hat kalasta biter");
            Assert.IsFalse(Phase(B).Any(f => f.K == "turret-hit"), "Core vurmaz");

            Baskin.CellAt(B, 2, 1).O = null;
            B.Ev = new Baskin.EvState { Id = "bulut" };
            Baskin.CellAt(B, 3, 1).Cloud = true;
            (line, hit) = FacilityGearView.TurretLine(B, g.X, g.Y, g.Dir);
            Assert.AreEqual(-1, hit, "bulut keser");
            CollectionAssert.AreEqual(new[] { (2, 1) }, line.Select(p => (p.X, p.Y)).ToArray(), "hat bulutun önünde biter");
            Assert.IsFalse(Phase(B).Any(f => f.K == "turret-hit"), "Core da bulutta vurmaz");
            Assert.IsFalse(m.Dead);
        }

        /// <summary>Makine evresi kopyada (<see cref="Phase"/> gibi); evreden sonraki durum.</summary>
        static Baskin.BState After(Baskin.BState B)
        {
            var C = B.Clone();
            foreach (var e in C.Units.Where(u => u.Side == 'e')) { e.Intent = null; e.Idle = false; }
            C.Spawns.Clear(); C.SpawnPerTurn = 0;
            C.Turn = C.MaxTurns;
            Assert.IsTrue(Baskin.EndTurn(C), "evre koşmadı");
            return C;
        }

        /// <summary>
        /// Olay odalarında eşlik (inceleme bulgusu 5): hidrolik yağda çekilen makine ve yürüyen yem kayar (Core <c>Slide</c>), önizleme
        /// varışı ve yolu Core'un evresindeki kareyle aynı; yük turnikeye bakmaz (Core <c>PullLoad</c>), ayaktaki birimi turnike durdurur.
        /// </summary>
        [Test]
        public void OilSlide_And_TurnstileLoad_MatchCore()
        {
            // Yağ: vinç (1,1) doğuya; makine (4,1) bir kare çekilir, (3,1) yağlı: (2,1)'e kayar
            var B = Flat();
            B.Ev = new Baskin.EvState { Id = "yag" };
            Baskin.CellAt(B, 3, 1).Oil = true;
            var w = AddGadget(B, "vinc", 1, 1, 1, 3, 0);
            var m = AddMachine(B, "dron", 4, 1);
            var (line, hit, unit) = FacilityGearView.WinchLine(B, w.X, w.Y, w.Dir);
            Assert.AreSame(m, unit);
            var end = FacilityGearView.PullEnd(B, line[hit], w.Dir, false);
            var C = After(B);
            var mc = C.Units.First(u => u.Id == m.Id);
            Assert.AreEqual((2, 1), (mc.X, mc.Y), "Core: çekilen makine yağda kaydı");
            Assert.AreEqual((mc.X, mc.Y), (end.X, end.Y), "önizlemenin varışı = Core");

            // Yağ: yem (1,3) doğuya; (2,3) yağlı: adım + kayma + adım
            B = Flat();
            B.Ev = new Baskin.EvState { Id = "yag" };
            Baskin.CellAt(B, 2, 3).Oil = true;
            var g = AddGadget(B, "yem", 1, 3, 1, 3, 0);
            var path = FacilityGearView.DecoyPath(B, g.X, g.Y, g.Dir, 1, g);
            C = After(B);
            var gc = C.Units.First(u => u.Id == g.Id);
            Assert.AreEqual((gc.X, gc.Y), (path.Last().X, path.Last().Y), "yemin varışı = Core");
            Assert.Greater(gc.X - g.X, B.Moves.DecoyStep, "yağ yemi adımından öteye taşıdı");
            CollectionAssert.AreEqual(Enumerable.Range(g.X, gc.X - g.X + 1).Select(x => (x, 3)).ToList(), path.Select(p => (p.X, p.Y)).ToList(), "yol kesintisiz");

            // Turnike: hol (3,3) çevresi, içeri kipinde holden dışarı geçilmez; vinç (6,3) batıya
            B = Flat();
            B.Ev = new Baskin.EvState { Id = "turnike", Q = new Baskin.Pt(3, 3), Mode = 0 };
            B.Ev.Area2.Add(new Baskin.Pt(5, 3));
            w = AddGadget(B, "vinc", 6, 3, 3, 3, 0);
            B.CargoX = 4; B.CargoY = 3;
            (line, hit, unit) = FacilityGearView.WinchLine(B, w.X, w.Y, w.Dir);
            Assert.IsNull(unit, "yük");
            end = FacilityGearView.PullEnd(B, line[hit], w.Dir, true);
            C = After(B);
            Assert.AreEqual((5, 3), (C.CargoX, C.CargoY), "Core: yük turnikeye bakmaz");
            Assert.AreEqual((C.CargoX, C.CargoY), (end.X, end.Y), "önizlemenin yük varışı = Core");
            B.CargoX = B.CargoY = -1;
            m = AddMachine(B, "dron", 4, 3);
            (line, hit, unit) = FacilityGearView.WinchLine(B, w.X, w.Y, w.Dir);
            end = FacilityGearView.PullEnd(B, line[hit], w.Dir, false);
            C = After(B);
            mc = C.Units.First(u => u.Id == m.Id);
            Assert.AreEqual((4, 3), (mc.X, mc.Y), "Core: turnike makineyi durdurur");
            Assert.AreEqual((mc.X, mc.Y), (end.X, end.Y));

            // Kurulum önizlemesi: vincin karesi henüz boş, ama çekilen oraya varmaz
            B = Flat();
            var t = Team(B, "tek");
            Put(t, 1, 3);
            m = AddMachine(B, "dron", 3, 3);
            Gear(t, FacilityGear.Winch, 1);
            var view = FacilityGearView.Preview(B, t, Baskin.ActWinch, 2, 3, Baskin.PreviewAction(B, t.Id, Baskin.ActWinch, 2, 3));
            Assert.IsFalse(view.Ghosts.Any(h => h.X == 2 && h.Y == 3), "çekilen vincin karesine varmaz");
        }

        [Test]
        public void Decoy_TurnstileStopsStep()
        {
            var B = Flat();
            // Turnike holü (3,3) çevresi; içeri kipinde holden dışarı (turnikeli kareye) adım yok
            B.Ev = new Baskin.EvState { Id = "turnike", Q = new Baskin.Pt(3, 3), Mode = 0 };
            B.Ev.Area2.Add(new Baskin.Pt(5, 3));
            var g = AddGadget(B, "yem", 4, 3, 1, 3, 0);
            var path = FacilityGearView.DecoyPath(B, g.X, g.Y, g.Dir, 1, g);
            Assert.AreEqual(1, path.Count, "turnike yemin adımını durdurur");
            Assert.IsFalse(Phase(B).Any(f => f.K == "decoy-step"), "Core da yürümez");
            B.Ev.Mode = 1;
            path = FacilityGearView.DecoyPath(B, g.X, g.Y, g.Dir, 1, g);
            Assert.Greater(path.Count, 1, "dışarı kipinde geçer");
            CollectionAssert.AreEqual(path.Skip(1).Select(p => (p.X, p.Y)).ToList(), Phase(B).Where(f => f.K == "decoy-step").Select(f => (f.X + f.Dx, f.Y + f.Dy)).ToList());
        }

        [Test]
        public void Clamp_DropsTargetIntent_BarkAreaManhattan()
        {
            var B = Flat();
            var t = Team(B, "tek");
            Put(t, 3, 3);
            var m = AddMachine(B, "dron", 4, 3);
            m.Intent = new Baskin.Intent { Kind = "shoot", Dir = 3 };
            Assert.IsTrue(Baskin.Intents(B).Any(i => i.Unit == m.Id), "makinenin niyeti var");
            Gear(t, FacilityGear.Clamp, 1);
            var preview = Baskin.PreviewAction(B, t.Id, Baskin.ActClamp, 4, 3);
            Assert.IsNotNull(preview);
            Assert.IsFalse(preview.Intents.Any(i => i.Unit == m.Id), "kelepçe önizlemesinde hedefin niyeti düşer");
            var view = FacilityGearView.Preview(B, t, Baskin.ActClamp, 4, 3, preview);
            Assert.IsTrue(view.Labels.Any(l => l.X == 4 && l.Y == 3), "hedefte ELE GEÇER etiketi");

            var dog = AddDog(B, t.Slot, 2, 5);
            int r = B.Moves.BarkRange;
            var area = FacilityGearView.BarkArea(B, dog.X, dog.Y);
            var expected = Enumerable.Range(0, 64).Where(k => System.Math.Abs(k % 8 - dog.X) + System.Math.Abs(k / 8 - dog.Y) <= r).ToList();
            CollectionAssert.AreEquivalent(expected, area, "erim Manhattan ≤ BarkRange");
            var bark = FacilityGearView.Preview(B, dog, Baskin.ActBark, dog.X, dog.Y, Baskin.PreviewAction(B, dog.Id, Baskin.ActBark, dog.X, dog.Y));
            CollectionAssert.AreEquivalent(expected, bark.Area, "havla önizlemesinin alan boyası erim");
        }
    }
}
