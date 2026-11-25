using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Rehber adımının işaret edeceği yer: sahnedeki bir hedef (<see cref="TutorialAnchor"/> kimliği), panonun bir alanı (görev cebi,
    /// "bosta", "depot") ya da baskın tahtasının bir karesi; <see cref="Label"/> işaretçinin etiketi (o dilde).</summary>
    public sealed class TutorialTargetSpec
    {
        public string Anchor, Pocket, Label;
        public Baskin.Pt? Tile;
        public bool IsEmpty => Anchor == null && Pocket == null && Tile == null;
    }

    /// <summary>
    /// Rehber adımlarının hedefleri (geliştirici, 2026-11-24: "şuraya bas şuraya bas"). Saf: adımı, rehber bağlamını ve baskın durumunu
    /// okur, işaret edilecek yeri seçer; durumu değiştirmez, RNG çekmez. İlk baskın tohumdan üretilen gerçek kanattır; tahta kareleri bu
    /// yüzden sabit değil, durumdan seçilir (yürünecek kare, kapı, kasa, niyetli makine). Kimlikler sahnedeki <see cref="TutorialAnchor"/>'larla
    /// aynıdır (<see cref="Anchors"/>).
    /// </summary>
    public static class TutorialTargets
    {
        public const string KeyShelter = "key.shelter", DeskCounters = "desk.counters",
            BoardBack = "board.back", DeskMap = "desk.map", SlipOrder = "slip.order", SquadGo = "squad.go", ShiftSwitch = "shift.switch",
            DeskEvrak = "desk.evrak", ReportAction = "report.action", RaidTurns = "raid.turns", RaidTeam1 = "raid.team1",
            RaidAbility = "raid.ability", RaidEnd = "raid.end", RaidUndo = "raid.undo", RaidContinue = "raid.continue";

        /// <summary>Sahnede ve prefab'larda <see cref="TutorialAnchor"/> olarak bulunması gereken kimlikler (sahne testleri).</summary>
        public static readonly string[] DeskAnchors = { KeyShelter, DeskCounters, BoardBack, DeskMap, SlipOrder, SquadGo, ShiftSwitch, DeskEvrak, ReportAction };
        public static readonly string[] RaidAnchors = { RaidTurns, RaidTeam1, RaidAbility, RaidEnd, RaidUndo, RaidContinue };
        public static IEnumerable<string> Anchors => DeskAnchors.Concat(RaidAnchors);

        static readonly string LabelHere = Loc.N("BURAYA BAS"), LabelOpen = Loc.N("AÇ"), LabelDrag = Loc.N("SÜRÜKLE: MUTFAK"),
            LabelWatch = Loc.N("GÖZETİM"), LabelWalk = Loc.N("YÜRÜ"), LabelGate = Loc.N("KAPI"), LabelCrate = Loc.N("KASA"),
            LabelIntent = Loc.N("NİYET"), LabelExit = Loc.N("ÇIKIŞ"), LabelPass = Loc.N("GEÇ"), LabelLook = Loc.N("BAK"),
            LabelPick = Loc.N("SEÇ"), LabelQ = Loc.N("Q · YETENEK"), LabelEnd = Loc.N("EMRİ İLET");

        static TutorialTargetSpec A(string anchor, string label) => new TutorialTargetSpec { Anchor = anchor, Label = Loc.T(label) };
        static TutorialTargetSpec T(Baskin.Pt? tile, string label) => tile == null ? null : new TutorialTargetSpec { Tile = tile, Label = Loc.T(label) };

        /// <summary>
        /// Adımın hedefi; işaret edilecek bir şey yoksa null. <paramref name="raid"/> baskın tahtası (yüklüyse), <paramref name="selected"/>
        /// seçili uzman (yoksa null).
        /// </summary>
        public static TutorialTargetSpec For(PlanningTutorialStep step, PlanningTutorialContext c, Baskin.BState raid, Baskin.Unit selected)
        {
            if (step == null || c == null) return null;
            switch (step.Id)
            {
                case "counters": return A(DeskCounters, LabelLook);
                case "board": return c.BoardOpen ? null : A(KeyShelter, LabelHere);
                // Pano adımları: pano kapanırsa (Esc, devam) önce SIĞINAK tuşu
                case "person": return c.BoardOpen ? new TutorialTargetSpec { Pocket = "bosta", Label = Loc.T(LabelOpen) } : A(KeyShelter, LabelOpen);
                case "assign": return c.BoardOpen ? new TutorialTargetSpec { Pocket = "cook", Label = Loc.T(LabelDrag) } : A(KeyShelter, LabelOpen);
                case "gear": return c.BoardOpen ? new TutorialTargetSpec { Pocket = "depot", Label = Loc.T(LabelLook) } : null;
                case "intel": return c.BoardOpen ? new TutorialTargetSpec { Pocket = "watch", Label = Loc.T(LabelWatch) } : null;
                case "back": return c.BoardOpen ? A(BoardBack, LabelHere) : null;
                case "slip": return c.BoardOpen ? A(BoardBack, LabelHere) : A(DeskMap, LabelHere);
                case "order": return c.BoardOpen ? A(BoardBack, LabelHere) : c.SquadOpen ? A(SquadGo, LabelHere) : c.SlipOpen ? A(SlipOrder, LabelHere) : A(DeskMap, LabelHere);
                case "raidBoard": return A(RaidTurns, LabelLook);
                case "raidIntent": return T(IntentTile(raid), LabelIntent);
                case "raidSelect": return A(RaidTeam1, LabelPick);
                case "raidMove": return selected == null ? A(RaidTeam1, LabelPick) : T(MoveHint(raid, selected), LabelWalk);
                case "raidAbility": return A(RaidAbility, LabelQ);
                case "raidTurn": return A(RaidEnd, LabelEnd);
                case "raidGate": return T(GateTile(raid), LabelGate);
                case "raidPass": return AllAtGate(raid) ? A(RaidEnd, LabelPass) : T(GateTile(raid), LabelGate);
                case "raidMiddle": return AllAtGate(raid) ? A(RaidEnd, LabelPass) : T(GateTile(raid), LabelGate);
                case "raidTarget": return T(CrateTile(raid), LabelCrate);
                case "raidEscape": return AllAtGate(raid) ? A(RaidEnd, LabelPass) : T(GateTile(raid), LabelExit);
                case "result": return A(RaidContinue, LabelHere);
                case "report": return A(ReportAction, LabelHere);
                case "print": return c.BoardOpen ? A(BoardBack, LabelHere) : A(ShiftSwitch, LabelHere);
                case "file": return c.BoardOpen ? A(BoardBack, LabelHere) : A(DeskEvrak, LabelHere);
                default: return null;
            }
        }

        /// <summary>Niyetli ilk makinenin karesi (emir sırasıyla); yoksa null.</summary>
        public static Baskin.Pt? IntentTile(Baskin.BState B)
        {
            if (B == null || B.Phase != "player") return null;
            var first = Baskin.Intents(B).OrderBy(i => i.Order).FirstOrDefault();
            if (first == null) return null;
            var u = B.Units.FirstOrDefault(v => v.Id == first.Unit && !v.Dead);
            return u == null ? (Baskin.Pt?)null : new Baskin.Pt(u.X, u.Y);
        }

        /// <summary>Seçili uzmanın yürüyebildiği kareler içinden kapıya (kaçışta çıkışa) en yakını; eşitlikte yolu kısa olan, sonra tarama
        /// sırası. Yürüyemiyorsa null.</summary>
        public static Baskin.Pt? MoveHint(Baskin.BState B, Baskin.Unit u)
        {
            if (B == null || u == null || !Baskin.CanMove(B, u)) return null;
            var reach = Baskin.Reachable(B, u);
            if (reach.Count == 0) return null;
            var gates = Baskin.GateTiles(B);
            var goals = gates.Count > 0 ? gates : (IReadOnlyList<Baskin.Pt>)B.Exits;
            int Near(Baskin.Pt p) => goals.Count == 0 ? 0 : goals.Min(g => System.Math.Abs(g.X - p.X) + System.Math.Abs(g.Y - p.Y));
            return reach.OrderBy(Near).ThenBy(p => Baskin.MovePath(B, u, p.X, p.Y)?.Count ?? int.MaxValue).ThenBy(p => p.Y).ThenBy(p => p.X).First();
        }

        /// <summary>Boş ilk kapı (kaçışta çıkış) karesi; hepsi doluysa ilki; kapı yoksa null.</summary>
        public static Baskin.Pt? GateTile(Baskin.BState B)
        {
            if (B == null) return null;
            var gates = Baskin.GateTiles(B);
            if (gates.Count == 0) return B.Escape && B.Exits.Count > 0 ? B.Exits[0] : (Baskin.Pt?)null;
            foreach (var g in gates) if (Baskin.UnitAt(B, g.X, g.Y) == null && !B.Units.Any(u => !u.Dead && u.AtGate && u.X == g.X && u.Y == g.Y)) return g;
            return gates[0];
        }

        /// <summary>Ayaktaki bütün uzmanlar kapıda mı (GEÇ verilebilir).</summary>
        public static bool AllAtGate(Baskin.BState B) =>
            B != null && B.Units.Any(u => Baskin.IsSpecialist(u) && !u.Dead && u.AtGate) && Baskin.CanPass(B);

        /// <summary>Kalan ilk hedefin (kasa, dolap) karesi; yoksa null.</summary>
        public static Baskin.Pt? CrateTile(Baskin.BState B)
        {
            if (B == null) return null;
            var left = Baskin.TargetsLeft(B);
            return left.Count == 0 ? (Baskin.Pt?)null : new Baskin.Pt(left[0].X, left[0].Y);
        }
    }
}
