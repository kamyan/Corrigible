using System;
using System.Linq;

namespace Corrigible.Core
{
    public static partial class Baskin
    {
        public static bool IsTurncoat(Unit u) => u != null && u.Side == 'e' && u.Slot >= 0;

        static bool InFieldMask(int mask, Unit u) => u.Slot >= 0 && (mask & (1 << u.Slot)) != 0;

        // Maruziyet eylem anındaki konuma aittir. Geri al ve önizleme bu değeri birimle kopyalar.
        static void MarkPulse(BState b, int x, int y)
        {
            if (b.Params == null || b.Params.FieldTurn <= 0) return;
            foreach (var u in b.Units)
                if (IsSpecialist(u) && OnBoard(u) && Man(x, y, u.X, u.Y) <= 1) u.PulseContact = true;
        }

        static void ApplyPulseContacts(BState b)
        {
            if (b.Params == null || b.Params.FieldTurn <= 0) return;
            foreach (var u in b.Units)
            {
                if (!u.PulseContact) continue;
                u.PulseContact = false;
                if (!IsSpecialist(u) || !OnBoard(u) || !InFieldMask(b.Params.Tells, u)) continue;
                u.Locked = true;
                AddFx(b, new Fx { K = "lock", Unit = u.Id, X = u.X, Y = u.Y });
                Say(b, Loc.F("EMP sonrası {0} yerinden kıpırdayamıyor.", u.PersonName), "bad");
            }
        }

        static void FieldMachinePhase(BState b)
        {
            var p = b.Params;
            if (p == null || p.FieldTurn <= 0 || b.Room == null) return;
            bool turn = !b.Defense && !RaidChain.IsDefense(p) && p.MissionKind == "server"
                && b.Room.Type == RoomType.Target && b.Turn == p.FieldTurn;
            foreach (var u in b.Units)
            {
                if (!turn || !IsSpecialist(u) || u.Dead || u.Out || !InFieldMask(p.Turncoats, u)) continue;
                if (u.AtGate) ReturnFromGate(b, u);
                u.Side = 'e';
                u.Intent = null;
                u.AmbushDir = -1;
                Say(b, Loc.F("{0} taraf değiştirdi. Telsizde düşman kanalına geçti.", u.PersonName), "bad");
            }
        }

        static void FieldDogReport(BState b)
        {
            if (b.Params == null || b.Params.Tells == 0 || b.Phase != "player") return;
            foreach (var u in b.Units)
                if (IsSpecialist(u) && OnBoard(u) && InFieldMask(b.Params.Tells, u)
                    && b.Units.Any(d => IsDog(d) && OnBoard(d) && Man(d.X, d.Y, u.X, u.Y) == 1))
                    Say(b, Loc.F("Köpek {0} yaklaşınca hırladı.", u.PersonName), "sys");
        }

        // Aynı öncelikteki hedeflerde kare ve yön sırası sabittir. Mevcut hedef ayak altından
        // çekilirse niyet yönünü ve menzilini korur; kanca önündeki ilk yeni engele çarpar.
        static void PlanTurncoat(BState b, Unit u, ref int order)
        {
            string kind = UnitTypes[u.Type].Ability;
            var players = Alive(b, 'p').Where(IsSpecialist).ToList();
            u.Intent = null;
            u.Order = 0;
            if (players.Count == 0) return;
            var start = new Pt(u.X, u.Y);
            var spots = new System.Collections.Generic.List<Pt> { start };
            if (b.Turn > b.Params.FieldTurn + 1 && !u.Frozen) spots.AddRange(Reachable(b, u));
            var best = start;
            int bestScore = int.MinValue, bestDir = -1, bestRange = 0;
            foreach (var pos in spots)
            {
                u.X = pos.X; u.Y = pos.Y;
                int near = players.Min(v => Man(pos.X, pos.Y, v.X, v.Y));
                // Seçilebilir hedef yoksa sonraki niyete hazırlanmak için ekibe yaklaşır.
                if (-near > bestScore) { bestScore = -near; best = pos; bestDir = -1; }
                foreach (var tile in Targets(b, u))
                {
                    var victim = UnitAt(b, tile.X, tile.Y);
                    if (victim == null || !IsSpecialist(victim)) continue;
                    int distance = Man(pos.X, pos.Y, tile.X, tile.Y);
                    int score = 100 - distance;
                    if (score <= bestScore) continue;
                    bestScore = score; best = pos; bestRange = distance;
                    bestDir = Array.FindIndex(Dirs, d => d[0] == Sign(tile.X - pos.X) && d[1] == Sign(tile.Y - pos.Y));
                }
            }
            u.X = best.X; u.Y = best.Y;
            if (bestDir < 0)
            {
                // İlk plan yerinde kalır; hedef erişimde olmasa da görülebilen bir hattı tutar.
                var target = players.OrderBy(v => Man(best.X, best.Y, v.X, v.Y)).First();
                var available = kind == "sabotaj" ? BlastArea(u.X, u.Y) : Targets(b, u);
                var aim = available.OrderBy(t => Man(t.X, t.Y, target.X, target.Y)).ToList();
                if (aim.Count == 0) return;
                var tile = aim[0];
                bestRange = Man(best.X, best.Y, tile.X, tile.Y);
                bestDir = Array.FindIndex(Dirs, d => d[0] == Sign(tile.X - best.X) && d[1] == Sign(tile.Y - best.Y));
                if (bestDir < 0) return;
            }
            u.Intent = new Intent { Kind = kind, Dir = bestDir, Range = bestRange };
            u.Order = ++order;
        }

        static ITarget FieldTarget(BState b, Unit u, int dir)
        {
            var d = Dirs[dir];
            int range = u.Intent.Range;
            if (u.Intent.Kind == "kanca")
            {
                var hit = HookTrace(b, u, d);
                if (hit == null) return null;
                range = hit.K;
            }
            int x = u.X + d[0] * range, y = u.Y + d[1] * range;
            if (!Inb(x, y)) return null;
            bool blocked = InCloud(b, u.X, u.Y) || InCloud(b, x, y);
            return new ITarget { X = x, Y = y, K = range, None = blocked, Cloud = blocked };
        }
    }
}
