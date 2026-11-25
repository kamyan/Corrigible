using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using static Corrigible.Tests.FacilityKit;

namespace Corrigible.Tests
{
    /// <summary>
    /// Yürüyüşün yolu (<see cref="Baskin.MovePath"/>; geliştirici 2026-11-24: hareket emrinde fareyle gezilen kareye yol çizilir, parça
    /// bu yoldan yürür). Yol <see cref="Baskin.DoMove"/>'un kuralıyla tutarlı olmalı: yol var ⇔ kare <see cref="Baskin.Reachable"/>'da;
    /// yol birimin karesinden başlar, kareye biter, dik komşu adımlarla menzil içinde kalır, duvar, nesne ve düşman içinden geçmez.
    /// </summary>
    public class FacilityMovePathTests
    {
        static void CheckAll(Baskin.BState B, string where)
        {
            foreach (var u in B.Units.Where(u => u.Side == 'p' && Baskin.OnBoard(u)).ToList())
            {
                bool can = Baskin.CanMove(B, u);
                var reach = can ? new HashSet<int>(Baskin.Reachable(B, u).Select(p => p.Y * Baskin.N + p.X)) : new HashSet<int>();
                int range = u.MoveRange > 0 ? u.MoveRange : Baskin.UnitTypes[u.Type].Move;
                for (int y = 0; y < Baskin.N; y++)
                    for (int x = 0; x < Baskin.N; x++)
                    {
                        var path = Baskin.MovePath(B, u, x, y);
                        string at = where + " · " + u.Type + " (" + u.X + "," + u.Y + ") → (" + x + "," + y + ")";
                        Assert.AreEqual(reach.Contains(y * Baskin.N + x), path != null, at + ": yol ⇔ ulaşılabilir");
                        if (path == null) continue;
                        Assert.AreEqual((u.X, u.Y), (path[0].X, path[0].Y), at + ": başlangıç");
                        Assert.AreEqual((x, y), (path[path.Count - 1].X, path[path.Count - 1].Y), at + ": varış");
                        Assert.LessOrEqual(path.Count - 1, range, at + ": menzil");
                        for (int i = 1; i < path.Count; i++)
                        {
                            Assert.AreEqual(1, System.Math.Abs(path[i].X - path[i - 1].X) + System.Math.Abs(path[i].Y - path[i - 1].Y), at + ": dik komşu");
                            var c = B.Cells[path[i].Y, path[i].X];
                            Assert.AreNotEqual("wall", c.T, at + ": duvar");
                            Assert.IsNull(c.O, at + ": nesne");
                            var other = Baskin.UnitAt(B, path[i].X, path[i].Y);
                            Assert.IsTrue(other == null || other.Side == u.Side, at + ": düşmandan geçmez");
                        }
                    }
                Assert.IsNull(Baskin.MovePath(B, u, u.X, u.Y), "kendi karesine yol yok");
            }
        }

        [Test]
        public void MovePath_MatchesReachable_EveryRegion_ThreeSheets()
        {
            int rooms = 0;
            foreach (var wing in Wings)
            {
                var p = Params(wing, 79);
                var reps = new List<RoomReport>();
                for (int sheet = 1; sheet <= 3; sheet++)
                {
                    var B = Open(p, reps);
                    CheckAll(B, wing[sheet - 1].Id);
                    rooms++;
                    reps.Add(Rep(sheet));
                }
            }
            Assert.Greater(rooms, 30);
        }

        [Test]
        public void MovePath_Null_WhenTheUnitCannotMove()
        {
            var wing = Wings[0];
            var B = Open(Params(wing, 79), new List<RoomReport>());
            var u = B.Units.First(v => v.Side == 'p' && Baskin.IsSpecialist(v));
            var target = Baskin.Reachable(B, u).First();
            Assert.IsNotNull(Baskin.MovePath(B, u, target.X, target.Y));
            u.Moved = true;
            Assert.IsNull(Baskin.MovePath(B, u, target.X, target.Y), "yürüdü: yol yok");
            u.Moved = false;
            B.Phase = "enemy";
            Assert.IsNull(Baskin.MovePath(B, u, target.X, target.Y), "makine evresi: yol yok");
        }

        /// <summary>Yol durumu değiştirmez ve RNG çekmez (sunum her fare hareketinde sorar).</summary>
        [Test]
        public void MovePath_IsPure()
        {
            var B = Open(Params(Wings[0], 79), new List<RoomReport>());
            var u = B.Units.First(v => v.Side == 'p' && Baskin.IsSpecialist(v));
            int rs = B.Rs; var at = (u.X, u.Y); int fx = B.FxList.Count;
            foreach (var t in Baskin.Reachable(B, u)) Baskin.MovePath(B, u, t.X, t.Y);
            Assert.AreEqual(rs, B.Rs);
            Assert.AreEqual(at, (u.X, u.Y));
            Assert.AreEqual(fx, B.FxList.Count);
        }
    }
}
