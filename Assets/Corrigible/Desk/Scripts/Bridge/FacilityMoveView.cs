using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Hamle önizlemesinin çizgisi (kare koordinatlarında): renk anlamı ("good" oyuncu mürekkebi, "ink", "graphite"),
    /// kesik mi, kavis yüksekliği (0 düz), ok ucu.</summary>
    public struct MoveStroke { public Baskin.Pt[] Points; public string Color; public bool Dashed; public float Arc; public bool Tip; }

    /// <summary>Hamle önizlemesinin hayaleti: birim (kimlikle; aynı karede uzman ve gövde üst üste durabilir) ve varacağı kare.</summary>
    public struct MoveGhost { public int Unit, X, Y; }

    /// <summary>Hamle önizlemesinin kâğıt etiketi: kare, yazı, renk (masa teması anahtarı).</summary>
    public struct MoveLabel { public int X, Y; public string Text, Color; }

    /// <summary>Bir hamlenin hedef karesindeki önizlemesi: çizgiler, hayaletler, kâğıt etiketleri, hover kartının satırları (önce
    /// bunlar) ve odak karesi (iki kişilik iş; y*8+x, yoksa −1).</summary>
    public sealed class MovePreview
    {
        public readonly List<MoveStroke> Strokes = new List<MoveStroke>();
        public readonly List<MoveGhost> Ghosts = new List<MoveGhost>();
        public readonly List<MoveLabel> Labels = new List<MoveLabel>();
        public readonly List<RaidCardRow> Rows = new List<RaidCardRow>();
        public int Focus = -1;
        /// <summary>Aşama 9: alan boyası (y*8+x; havlamanın erimi); öbür hamlelerde boş.</summary>
        public readonly List<int> Area = new List<int>();
    }

    /// <summary>
    /// Tesis hamlesinin önizleme verisi (aşama 7; belge §4): Core'un <see cref="Baskin.PreviewAction"/> sonucundan ve hamleden önceki
    /// durumdan çizgi, hayalet, etiket ve hover satırları kurar. Saf ve durumsuz (EditMode'da sınanır); çizimi tahta yapar. Yollar
    /// <see cref="Baskin.WalkPath"/>'ten (Core'un yürüyüş kuralı), değerler <see cref="Baskin.BState.Moves"/>'tan.
    /// </summary>
    public static class FacilityMoveView
    {
        static string Cell(Baskin.BState B, int x, int y) => RaidBoard3D.CellName(B?.Room?.Index ?? 0, x, y);
        static Baskin.Pt P(int x, int y) => new Baskin.Pt(x, y);
        static RaidCardRow Row(string icon, string text, bool good = false) => new RaidCardRow { Icon = icon, Text = text, Good = good };

        /// <summary>
        /// <paramref name="B"/>: hamleden önceki durum; <paramref name="u"/>: hamleyi veren; (x, y) hedef kare;
        /// <paramref name="preview"/>: aynı hamlenin Core önizlemesi (null ise boş önizleme). <paramref name="rules"/>: sandık ödülünün
        /// değerleri (null: varsayılan denge). Aşama 9: ekipman ve birim eylemleri <see cref="FacilityGearView.Preview"/>'dan
        /// (<paramref name="nextKnown"/>: koklanacak odanın planı biliniyor).
        /// </summary>
        public static MovePreview Preview(Baskin.BState B, Baskin.Unit u, string kind, int other, int x, int y, Baskin.PreviewResult preview, Balance rules = null, bool nextKnown = false)
        {
            if (FacilityGearText.Handles(kind)) return FacilityGearView.Preview(B, u, kind, x, y, preview, nextKnown);
            var o = new MovePreview();
            if (B?.Room == null || B.Moves == null || u == null || preview == null) return o;
            var fx = preview.Fx ?? new List<Baskin.Fx>();
            switch (kind)
            {
                case Baskin.ActAmbush:
                {
                    var line = preview.Line ?? new List<Baskin.Pt>();
                    o.Strokes.Add(new MoveStroke { Points = new[] { P(u.X, u.Y) }.Concat(line).ToArray(), Color = "good", Dashed = true });
                    var at = line.Count > 0 ? line[0] : P(x, y);
                    o.Labels.Add(new MoveLabel { X = at.X, Y = at.Y, Text = Loc.T("PUSU"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_bolt_s", Loc.F("Pusu hattı: {0} kare", line.Count), true));
                    o.Rows.Add(Row("ic_bolt_s", Loc.T("Makine evresinde hatta duran ilk makine −1")));
                    o.Rows.Add(Row("ic_x_s", Loc.T("Bu tur yetenek kullanılamaz")));
                    break;
                }
                case Baskin.ActRush:
                {
                    var rush = fx.FirstOrDefault(f => f.K == "rush");
                    if (rush == null) break;
                    var stop = P(rush.X + rush.Dx, rush.Y + rush.Dy);
                    o.Strokes.Add(new MoveStroke { Points = new[] { P(rush.X, rush.Y), stop }, Color = "ink" });
                    o.Ghosts.Add(new MoveGhost { Unit = u.Id, X = stop.X, Y = stop.Y });
                    o.Labels.Add(new MoveLabel { X = stop.X, Y = stop.Y, Text = Loc.T("KOŞU"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_boots_s", Loc.F("Koşu: {0} kare", System.Math.Abs(rush.Dx) + System.Math.Abs(rush.Dy))));
                    break;
                }
                case Baskin.ActDrag:
                {
                    var path = Baskin.WalkPath(B, u, FacilityMoveText.Range(u) - Baskin.DragCostFor(B, u), x, y);
                    if (path != null) o.Strokes.Add(new MoveStroke { Points = path.ToArray(), Color = "ink" });
                    foreach (var m in preview.Moves.Where(m => m.Unit == u.Id || m.Unit == other))
                        o.Ghosts.Add(new MoveGhost { Unit = m.Unit, X = m.To.X, Y = m.To.Y });
                    bool gate = Baskin.IsGateTile(B, x, y);
                    var drop = preview.Moves.Where(m => m.Unit == other).Select(m => (Baskin.Pt?)m.To).FirstOrDefault() ?? P(x, y);
                    o.Labels.Add(new MoveLabel { X = drop.X, Y = drop.Y, Text = gate ? Loc.T("KAPIYA") : Loc.T("BIRAKIR"), Color = "ink.green" });
                    o.Rows.Add(gate ? Row("ic_boots_s", Loc.T("Kapıya girer; gövde ekiple geçer"), true) : Row("ic_boots_s", Loc.F("Bırakılır: {0}", Cell(B, drop.X, drop.Y))));
                    o.Rows.Add(Row("ic_x_s", Loc.T("Bu tur yetenek kullanılamaz")));
                    break;
                }
                case Baskin.ActThrow:
                    o.Strokes.Add(new MoveStroke { Points = new[] { P(u.X, u.Y), P(x, y) }, Color = "good", Dashed = true, Arc = .5f, Tip = true });
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.T("ALICI"), Color = "ink.green" });
                    var carrier = B.Units.FirstOrDefault(v => v.Id == preview.Carrier);
                    o.Rows.Add(Row("ic_eye_s", Loc.F("Yükü {0} taşır", FacilityMoveText.Who(carrier)), true));
                    break;
                case Baskin.ActJoint:
                {
                    o.Focus = y * Baskin.N + x;
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = preview.Opens ? Loc.T("AÇILIR") : Loc.T("TUTAR"), Color = preview.Opens ? "ink.green" : "ink.ink2" });
                    var r = rules ?? new Balance();
                    bool crate = Baskin.CellAt(B, x, y).Crate;
                    o.Rows.Add(!preview.Opens ? Row("ic_eye_s", Loc.T("Tutar: ikinci uzman bu tur gelmeli"))
                        : crate ? Row("ic_eye_s", Loc.F("Sandık açılır: kazanılırsa +{0} metal +{1} devre", r.facilityBigCrateMetal, r.facilityBigCrateCircuits), true)
                        : Row("ic_eye_s", Loc.T("Kepenk kalkar; makineler de geçer"), true));
                    break;
                }
                case Baskin.ActVent:
                {
                    var vent = fx.FirstOrDefault(f => f.K == "vent");
                    var near = vent != null ? P(vent.X, vent.Y) : P(u.X, u.Y);
                    if (near.X != u.X || near.Y != u.Y)
                    {
                        var walk = Baskin.WalkPath(B, u, FacilityMoveText.Range(u), near.X, near.Y);
                        if (walk != null) o.Strokes.Add(new MoveStroke { Points = walk.ToArray(), Color = "ink" });
                    }
                    o.Strokes.Add(new MoveStroke { Points = new[] { near, P(x, y) }, Color = "graphite", Dashed = true, Arc = .6f, Tip = true });
                    o.Ghosts.Add(new MoveGhost { Unit = u.Id, X = x, Y = y });
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.T("ÇIKAR"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_boots_s", Loc.F("Öbür uçtan çıkar: {0}", Cell(B, x, y)), true));
                    o.Rows.Add(Row("ic_bolt_s", Loc.T("Yetenek kullanılabilir kalır"), true));
                    break;
                }
            }
            if (fx.Any(f => f.K == "cargo-pick")) o.Rows.Add(Row("ic_eye_s", Loc.T("Yükü yerden alır"), true));
            if (fx.Any(f => f.K == "cargo-drop")) o.Rows.Add(Row("ic_x_s", Loc.T("Yük yere düşer")));
            return o;
        }
    }
}
