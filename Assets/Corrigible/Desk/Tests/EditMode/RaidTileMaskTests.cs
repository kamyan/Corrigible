using System;
using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Game.Raid;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Karo giydirmenin komşu maskesi (BASKIN-MEKANLARI Katman 3–4): düz, köşe, T, uç, tek kare, dörtlü kavşak; ayrık blok 8 komşuya
    /// (çaprazlar dahil) bakar; tahta dışı zemin sayılır; raf duvarı sabit duvarın köprüsüne girmez ve ray yönünü komşu raf
    /// karelerinden alır. Tahta ve kit önizlemesi aynı hesabı kullanır.
    /// </summary>
    public class RaidTileMaskTests
    {
        const int N = RaidTileMask.N, E = RaidTileMask.E, S = RaidTileMask.S, W = RaidTileMask.W;

        /// <summary>Satırlardan tür: '#' duvar, '~' kanal, 'R' raf duvarı, 'g' raf geçidi (zemin), başka her şey zemin; eksik satır ve sütun zemin.</summary>
        static Func<int, int, string> Kinds(params string[] rows)
            => (x, y) => { char c = At(rows, x, y); return c == '#' || c == 'R' ? "wall" : c == '~' ? "cable" : "floor"; };
        static Func<int, int, bool> Shelves(params string[] rows) => (x, y) => { char c = At(rows, x, y); return c == 'R' || c == 'g'; };
        static char At(string[] rows, int x, int y) => y < rows.Length && x < rows[y].Length ? rows[y][x] : '.';

        static Baskin.BState State(params string[] rows)
        {
            var B = new Baskin.BState();
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    B.Cells[y, x] = new Baskin.Cell { T = Kinds(rows)(x, y), Shelf = Shelves(rows)(x, y) };
            return B;
        }

        [Test]
        public void Cable_StraightCornerTeeCrossEndAndSingle()
        {
            // x:  0123456
            var k = Kinds(
                "~~~....",   // (0,0) köşe E+S, (1,0) düz E+W, (2,0) köşe W+S
                "~.~..~.",   // (0,1) düz N+S, (2,1) düz N+S, (5,1) tek
                "~.~~~..",   // (0,2) uç N, (2,2) T N+E+S, (4,2) uç W
                "..~....",
                ".~~~...",   // (2,4) dörtlü kavşak, (1,4) uç E, (3,4) uç W
                "..~....");  // (2,5) uç N
            Assert.AreEqual(E | S, RaidTileMask.Mask(k, 0, 0), "köşe");
            Assert.AreEqual(E | W, RaidTileMask.Mask(k, 1, 0), "yatay düz");
            Assert.AreEqual(W | S, RaidTileMask.Mask(k, 2, 0), "köşe");
            Assert.AreEqual(N | S, RaidTileMask.Mask(k, 0, 1), "dikey düz");
            Assert.AreEqual(N, RaidTileMask.Mask(k, 0, 2), "uç");
            Assert.AreEqual(N | E | S, RaidTileMask.Mask(k, 2, 2), "T");
            Assert.AreEqual(W, RaidTileMask.Mask(k, 4, 2), "uç");
            Assert.AreEqual(N | E | S | W, RaidTileMask.Mask(k, 2, 4), "dörtlü kavşak");
            Assert.AreEqual(E, RaidTileMask.Mask(k, 1, 4));
            Assert.AreEqual(N, RaidTileMask.Mask(k, 2, 5));
            Assert.AreEqual(0, RaidTileMask.Mask(k, 5, 1), "komşusuz tek kare");
            Assert.IsFalse(RaidTileMask.Isolated(k, 5, 1), "kanal hiçbir zaman ayrık blok değil");
        }

        [Test]
        public void Mask_BitsFollowTheBoardAxes()
        {
            // N = y − 1 (dünya +Z), E = x + 1 (+X), S = y + 1 (−Z), W = x − 1 (−X); tahta dışı zemin
            var k = Kinds("##.", "##.");
            Assert.AreEqual(E | S, RaidTileMask.Mask(k, 0, 0), "köşedeki karo tahta dışına bit vermez");
            Assert.AreEqual(W | N, RaidTileMask.Mask(k, 1, 1));
            Assert.AreEqual(new[] { N, E, S, W }, Array.ConvertAll(RaidTileMask.Sides, s => s.bit));
            Assert.AreEqual((0, -1), (RaidTileMask.Sides[0].dx, RaidTileMask.Sides[0].dy), "N: y − 1");
            Assert.AreEqual((1, 0), (RaidTileMask.Sides[1].dx, RaidTileMask.Sides[1].dy), "E: x + 1");
            Assert.AreEqual("floor", RaidTileMask.KindAt(k, -1, 0));
            Assert.AreEqual("floor", RaidTileMask.KindAt(k, 0, Baskin.N));
            var edge = Kinds("", "", "", "", "", "", "", ".......~");
            Assert.AreEqual(0, RaidTileMask.Mask(edge, 7, 7), "tahtanın köşesindeki tek kanal");
        }

        [Test]
        public void Isolated_LooksAtAllEightNeighboursIncludingDiagonals()
        {
            var k = Kinds(
                "#.......",  // (0,0) tahta köşesinde yalnız: ayrık
                "........",
                "...#....",  // (3,2) çaprazında (4,3) duvar: ayrık değil
                "....#...",
                "........",
                ".#......",  // (1,5) ortogonal komşusu (1,6): ayrık değil
                ".#....#.",  // (6,6) çevresi boş: ayrık; yanında kanal ayrıklığı bozmaz
                ".....~..");
            Assert.IsTrue(RaidTileMask.Isolated(k, 0, 0));
            Assert.IsFalse(RaidTileMask.Isolated(k, 3, 2), "çapraz komşu duvar");
            Assert.IsFalse(RaidTileMask.Isolated(k, 4, 3), "çapraz komşu duvar");
            Assert.IsFalse(RaidTileMask.Isolated(k, 1, 5));
            Assert.IsTrue(RaidTileMask.Isolated(k, 6, 6), "kanal komşu duvar değil");
            Assert.IsFalse(RaidTileMask.Isolated(k, 2, 2), "zemin ayrık blok değil");
            Assert.AreEqual(0, RaidTileMask.Mask(k, 3, 2), "çapraz komşu maskeye girmez");
        }

        [Test]
        public void Dress_ShelfWallsStayOutOfBridgesAndTakeTheRailAxis()
        {
            // Dikey ray x = 1 (g geçit, R raf duvarı); yatay ray y = 6; (2,2)-(2,3) sabit duvar rafa yaslı
            var rows = new[] { "........", ".g......", ".R#.....", ".R#.....", ".R......", "........", "...gRRR.", "........" };
            var B = State(rows);
            var shelf = RaidTileMask.For(B, 1, 2);
            Assert.AreEqual(0, shelf.Mask, "raf duvarı köprü almaz");
            Assert.AreEqual(RaidTileMask.AxisNS, shelf.ShelfAxis, "dikey ray: el çarkı batı yüzünde");
            Assert.IsFalse(shelf.Isolated);
            Assert.AreEqual(RaidTileMask.AxisEW, RaidTileMask.For(B, 4, 6).ShelfAxis, "yatay ray (komşusu geçit): el çarkı güney yüzünde");
            Assert.AreEqual(RaidTileMask.AxisEW, RaidTileMask.For(B, 6, 6).ShelfAxis, "rayın ucu");
            var fixedWall = RaidTileMask.For(B, 2, 2);
            Assert.AreEqual(S, fixedWall.Mask, "sabit duvar raf komşusunu saymaz, sabit komşusunu sayar");
            Assert.AreEqual(RaidTileMask.AxisNone, fixedWall.ShelfAxis);
            Assert.IsFalse(fixedWall.Isolated, "raf duvarı da duvardır: ayrık değil");
            Assert.AreEqual(new RaidTileMask.Dress(0, false, 0).ToString(), RaidTileMask.For(B, 0, 0).ToString(), "zemin");
            Assert.AreEqual(RaidTileMask.AxisNone, RaidTileMask.For(B, 1, 1).ShelfAxis, "geçit zemindir");
        }

        [Test]
        public void ShelfAxis_FallsBackToTheEventRailWhenNoNeighbourIsShelf()
        {
            // Madenci rayın ortasını yıktı (raf bayrağı kalktı): uçtaki blokun raf komşusu yok, yön olayın rayından
            var B = State("........", "........", "...R....", "........", "...R....");
            B.Ev = new Baskin.EvState { Id = "raf", Area = new List<Baskin.Pt> { new Baskin.Pt(2, 2), new Baskin.Pt(3, 2), new Baskin.Pt(4, 2) } };
            Assert.AreEqual(RaidTileMask.AxisEW, RaidTileMask.RailAxis(B));
            Assert.AreEqual(RaidTileMask.AxisEW, RaidTileMask.For(B, 3, 2).ShelfAxis);
            B.Ev.Area = new List<Baskin.Pt> { new Baskin.Pt(3, 2), new Baskin.Pt(3, 3), new Baskin.Pt(3, 4) };
            Assert.AreEqual(RaidTileMask.AxisNS, RaidTileMask.For(B, 3, 4).ShelfAxis);
            B.Ev = null;
            Assert.AreEqual(RaidTileMask.AxisNS, RaidTileMask.RailAxis(B), "olay yoksa kuzey–güney");
            Assert.AreEqual(RaidTileMask.AxisEW, RaidTileMask.ShelfAxis(Shelves("..", ".Rg"), 1, 1, RaidTileMask.AxisNS), "komşu raf karesi önce gelir");
        }

        [Test]
        public void Dress_FromStateMatchesTheKindLookup()
        {
            var rows = new[] { "~~~..#.#", "~.~..#..", "~.~~~###", "..~...#.", ".~~~..#.", "..~..#..", "~.....##", "..~~..#." };
            var B = State(rows);
            var k = Kinds(rows);
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var d = RaidTileMask.For(B, x, y);
                    string t = k(x, y);
                    Assert.AreEqual(t == "floor" ? 0 : RaidTileMask.Mask(k, x, y), d.Mask, $"{x},{y}");
                    Assert.AreEqual(RaidTileMask.Isolated(k, x, y), d.Isolated, $"{x},{y}");
                }
            Assert.IsTrue(RaidTileMask.For(B, 7, 0).Isolated, "çevresi boş blok kolon olur");
            Assert.AreEqual(N | E, RaidTileMask.For(B, 5, 2).Mask, "duvar köşesi");
            Assert.AreEqual(W | E | S, RaidTileMask.For(B, 6, 2).Mask, "duvar T");
        }
    }
}
