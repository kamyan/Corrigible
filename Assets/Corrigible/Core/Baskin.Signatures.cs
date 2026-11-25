using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    public static partial class Baskin
    {
        static readonly MoveRules defaultSignatureRules = MoveRules.From(null);
        static MoveRules SignatureRules(BState b) => b.Moves ?? defaultSignatureRules;
        static bool IsSignatureThreat(string kind) => kind == "flame" || kind == "ram" || kind == "pull" || kind == "frost";
        static bool IsSignatureIntent(string kind) => IsSignatureThreat(kind) || kind == "seal" || kind == "build";

        /// <summary>Buzlu kare yürüyüşün son adımı olabilir.</summary>
        public static bool Frosted(BState b, int x, int y) =>
            CellAt(b, x, y).FrostUntil > 0 && CellAt(b, x, y).FrostUntil >= b.Turn;

        static string FrostSignature(BState b)
        {
            if (b.Signature != "kirag" && !b.Units.Any(u => u.Type == "kirag")) return "";
            var ice = AllTiles().Where(p => Frosted(b, p.X, p.Y))
                .Select(p => $"{p.X}{p.Y}:{CellAt(b, p.X, p.Y).FrostUntil}").ToArray();
            return ice.Length == 0 ? "" : "|I" + string.Join(",", ice);
        }

        static ITarget SignatureTarget(BState b, Unit e, string kind, int dir, int x, int y)
        {
            var d = Dirs[dir];
            var rules = SignatureRules(b);
            int range = kind == "flame" ? rules.FlameRange : kind == "ram" ? rules.RamRange
                : kind == "pull" ? rules.AnchorRange : kind == "frost" ? rules.FrostRange : 1;
            bool mechanical = kind == "seal" || kind == "build";
            ITarget last = null;
            for (int i = 1; i <= range; i++)
            {
                int nx = x + d[0] * i, ny = y + d[1] * i;
                if (!Inb(nx, ny)) break;
                var cell = CellAt(b, nx, ny);
                if (cell.T == "wall") break;
                var aim = new ITarget { X = nx, Y = ny, K = i };
                if (mechanical)
                {
                    aim.None = kind == "seal" ? !SealFits(b, nx, ny)
                        : cell.O?.K != "hurda" || UnitAt(b, nx, ny) != null || Machines(b) >= MaxEnemies;
                    return aim;
                }
                if (cell.Smoke || InCloud(b, nx, ny) || InCloud(b, x, y))
                {
                    if (kind == "flame" && last != null) return last;
                    aim.None = true; aim.Smoked = cell.Smoke; aim.Cloud = !cell.Smoke;
                    return aim;
                }
                if ((kind == "pull" || kind == "ram" || kind == "frost") && cell.O != null) break;
                if (kind == "ram" && !CanStep(b, nx - d[0], ny - d[1], nx, ny)) break;
                last = aim;
                if (kind == "pull" || kind == "ram")
                {
                    var unit = UnitAt(b, nx, ny);
                    if (unit != null && unit.Id != e.Id) return aim;
                    if (kind == "ram" && Frosted(b, nx, ny)) break;
                }
                if (kind == "frost" && i == range) return aim;
            }
            return kind == "flame" || kind == "ram" ? last : null;
        }

        static List<Pt> SignatureArea(string kind, int dir, int x, int y, ITarget aim)
        {
            if (aim == null || aim.None) return new List<Pt>();
            if (kind == "frost") return new[] { new Pt(aim.X, aim.Y) }.Concat(BlastArea(aim.X, aim.Y)).ToList();
            if (kind == "flame" || kind == "ram")
            {
                var d = Dirs[dir];
                return Enumerable.Range(1, aim.K).Select(i => new Pt(x + d[0] * i, y + d[1] * i)).ToList();
            }
            return kind == "pull" ? new List<Pt> { new Pt(aim.X, aim.Y) } : new List<Pt>();
        }

        static double SignatureScore(BState b, Unit e, string kind, int dir, int x, int y, ITarget aim, bool stuck)
        {
            if (aim == null || aim.None) return 0;
            var cell = CellAt(b, aim.X, aim.Y);
            var victim = UnitAt(b, aim.X, aim.Y);
            if (kind == "seal")
                return Alive(b, 'p').Any(u => IsSpecialist(u) && Man(u.X, u.Y, aim.X, aim.Y) <= 2) ? 6 : -1;
            if (kind == "build")
                return cell.O?.K == "hurda" && victim == null && Machines(b) < MaxEnemies ? 14 : -1;
            if (kind == "pull" || kind == "ram")
            {
                if (victim == null || victim.Id == e.Id || (kind == "pull" && aim.K < 2)) return 0;
                if (TeamLike(b, e, victim)) return Bolted(b, victim) ? 0 : 10 + (victim.Type == "tek" ? 3 : 0);
                return victim.Side == 'e' ? -4 : 2;
            }
            double score = 0;
            foreach (var pos in SignatureArea(kind, dir, x, y, aim))
            {
                var unit = UnitAt(b, pos.X, pos.Y);
                var target = CellAt(b, pos.X, pos.Y).O;
                if (unit != null && unit.Id != e.Id)
                    score += TeamLike(b, e, unit) ? (kind == "frost" ? 4 : 9) + (unit.Type == "tek" ? 2 : 0) : unit.Side == 'e' ? -4 : 2;
                else if (kind == "flame" && target != null)
                    score += IsTarget(target.K) ? -16 : target.K == "tup" ? 6 : stuck && IsBarrier(target.K) ? 5 : target.K == "civ" ? -3 : 0;
            }
            return score;
        }

        static bool SealFits(BState b, int x, int y) =>
            b.Room != null && FreeFloor(b, x, y) && BarrierFits(b, x, y, "kalas", TeamLinks(b));

        static void MachineScrap(BState b, Unit unit)
        {
            if (b.Room == null || b.Signature != "montaj" || unit.Assembled || (unit.Side != 'e' && !IsCaptive(unit))) return;
            if (!FreeFloor(b, unit.X, unit.Y) || !BarrierFits(b, unit.X, unit.Y, "hurda", TeamLinks(b))) return;
            CellAt(b, unit.X, unit.Y).O = MachineObject("hurda", SignatureRules(b).ScrapHp);
            AddFx(b, new Fx { K = "scrap", X = unit.X, Y = unit.Y, Obj = "hurda" });
        }

        static void ApplySignatureIntent(BState b, Unit e, ITarget aim)
        {
            string kind = e.Intent.Kind;
            int dir = e.Intent.Dir;
            var rules = SignatureRules(b);
            var d = Dirs[dir];
            var victim = UnitAt(b, aim.X, aim.Y);
            switch (kind)
            {
                case "flame":
                    foreach (var pos in SignatureArea(kind, dir, e.X, e.Y, aim))
                    {
                        Fire(b, pos.X, pos.Y);
                        MachineDamageAt(b, pos.X, pos.Y, rules.FlameDamage, UnitTypes[e.Type].Name);
                    }
                    break;
                case "frost":
                    foreach (var pos in SignatureArea(kind, dir, e.X, e.Y, aim))
                    {
                        var cell = CellAt(b, pos.X, pos.Y);
                        if (cell.T != "wall") cell.FrostUntil = Math.Max(cell.FrostUntil, b.Turn + rules.FrostTurns);
                    }
                    break;
                case "seal":
                    if (!SealFits(b, aim.X, aim.Y)) break;
                    CellAt(b, aim.X, aim.Y).O = MachineObject("kalas", rules.SealHp);
                    AddFx(b, new Fx { K = "build", X = aim.X, Y = aim.Y, Obj = "kalas" });
                    break;
                case "build":
                    if (victim != null || CellAt(b, aim.X, aim.Y).O?.K != "hurda" || Machines(b) >= MaxEnemies) break;
                    CellAt(b, aim.X, aim.Y).O = null;
                    var drone = NewUnit(b, "dron", aim.X, aim.Y);
                    drone.Assembled = true;
                    b.Units.Add(drone);
                    AddFx(b, new Fx { K = "spawn", X = aim.X, Y = aim.Y, Unit = drone.Id });
                    break;
                case "ram":
                    var path = SignatureArea(kind, dir, e.X, e.Y, aim);
                    foreach (var pos in path)
                    {
                        if (UnitAt(b, pos.X, pos.Y) != null) break;
                        AddFx(b, new Fx { K = "ram-step", X = e.X, Y = e.Y, Dx = d[0], Dy = d[1], Unit = e.Id });
                        e.X = pos.X; e.Y = pos.Y;
                        CableHurt(b, e);
                        if (e.Dead || Frosted(b, pos.X, pos.Y)) break;
                    }
                    if (!e.Dead && victim != null && Man(e.X, e.Y, victim.X, victim.Y) == 1)
                        Displace(b, victim, d, rules.RamPush);
                    break;
                case "pull":
                    if (victim != null && victim.Id != e.Id)
                        Displace(b, victim, new[] { -d[0], -d[1] }, Math.Min(aim.K - 1, rules.AnchorPull));
                    break;
            }
        }

        // Makinenin engeli dönüşte tekrar görülür, sonraki baskına iz olarak taşınmaz.
        static Obj MachineObject(string kind, int hp) => new Obj { K = kind, Hp = hp, Max = hp, MachineBuilt = true };

        static void Displace(BState b, Unit victim, int[] direction, int steps)
        {
            for (int i = 0; i < steps && OnBoard(victim); i++)
            {
                int x = victim.X, y = victim.Y;
                Push(b, victim, direction);
                if (victim.X == x && victim.Y == y) break;
            }
        }
    }
}
