using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Ekipman ve birim eylemlerinin önizleme verisi ve aygıt hatları (aşama 9; belge §5, tasarım §4): taret ve vinç hattı, yemin
    /// yürüyüş yolu, havlamanın erimi; eylemin hedef karesindeki çizgileri, hayaletleri, kâğıt etiketleri ve hover satırları
    /// (<see cref="MovePreview"/>). Aygıtın ve kelepçenin süresi Core'un public yardımcılarından (aşama 10, K44: <c>Baskin.DeviceTurnsFor</c>,
    /// <c>ClampTurnsFor</c>; parazit, kısa bağ ve cevaplar dahil). Aygıtların kuralı Core'da private (<c>RunTurret</c>, <c>RunWinch</c>, <c>RunDecoy</c>): burada public
    /// parçalarla (<see cref="Baskin.Dirs"/>, <see cref="Baskin.CellAt"/>, <see cref="Baskin.UnitAt"/>, <see cref="Baskin.CanStep"/>)
    /// aynen yeniden kurulur; eşlik testleri Core'un makine evresiyle kilitler. Saf ve durumsuz; çizimi tahta yapar.
    /// </summary>
    public static class FacilityGearView
    {
        static Baskin.Pt P(int x, int y) => new Baskin.Pt(x, y);
        static bool Inb(int x, int y) => x >= 0 && y >= 0 && x < Baskin.N && y < Baskin.N;
        static RaidCardRow Row(string icon, string text, bool good = false) => new RaidCardRow { Icon = icon, Text = text, Good = good };
        static bool Passable(Baskin.BState B, int x, int y) => Inb(x, y) && Baskin.CellAt(B, x, y).T != "wall" && Baskin.CellAt(B, x, y).O == null;
        static bool InCloud(Baskin.BState B, int x, int y) => B.Ev != null && B.Ev.Id == "bulut" && Inb(x, y) && Baskin.CellAt(B, x, y).Cloud;

        /// <summary>Uzmandan dik komşu kareye yön (<see cref="Baskin.Dirs"/> dizini); komşu değilse −1 (Core: aygıtın yönü, K32).</summary>
        public static int DirTo(int fx, int fy, int tx, int ty)
        {
            for (int i = 0; i < Baskin.Dirs.Length; i++) if (Baskin.Dirs[i][0] == tx - fx && Baskin.Dirs[i][1] == ty - fy) return i;
            return -1;
        }

        /// <summary>
        /// Taretin hattı (x, y)'den <paramref name="dir"/> yönünde 1..<see cref="MoveRules.TurretRange"/>: tahta dışında, duvarda, tel
        /// dışındaki nesnede ya da bulutta biter; ilk ayakta birimin karesinde (dahil) durur. Hit: o birim makineyse hattaki dizini (vurulur),
        /// değilse −1.
        /// </summary>
        public static (List<Baskin.Pt> Line, int Hit) TurretLine(Baskin.BState B, int x, int y, int dir)
        {
            var line = new List<Baskin.Pt>();
            if (B?.Moves == null || dir < 0) return (line, -1);
            var d = Baskin.Dirs[dir];
            for (int k = 1; k <= B.Moves.TurretRange; k++)
            {
                int tx = x + d[0] * k, ty = y + d[1] * k;
                if (!Inb(tx, ty)) break;
                var c = Baskin.CellAt(B, tx, ty);
                if (c.T == "wall" || c.O != null && c.O.K != "tel" || InCloud(B, tx, ty)) break;
                line.Add(P(tx, ty));
                var t = Baskin.UnitAt(B, tx, ty);
                if (t != null) return (line, t.Side == 'e' ? line.Count - 1 : -1);
            }
            return (line, -1);
        }

        /// <summary>
        /// Vincin hattı (x, y)'den <paramref name="dir"/> yönünde 1..<see cref="MoveRules.WinchRange"/>: tahta dışında, duvarda ya
        /// da her nesnede biter. İlk dolu kare: ayakta birim (aygıt hattı keser, çekilmez) ya da yatan gövde / yerdeki yük. Hit: çekilecek
        /// karenin hattaki dizini (yoksa −1); Unit: çekilen ayaktaki birim (yük ve gövdede null).
        /// </summary>
        public static (List<Baskin.Pt> Line, int Hit, Baskin.Unit Unit) WinchLine(Baskin.BState B, int x, int y, int dir)
        {
            var line = new List<Baskin.Pt>();
            if (B?.Moves == null || dir < 0) return (line, -1, null);
            var d = Baskin.Dirs[dir];
            for (int k = 1; k <= B.Moves.WinchRange; k++)
            {
                int tx = x + d[0] * k, ty = y + d[1] * k;
                if (!Inb(tx, ty) || Baskin.CellAt(B, tx, ty).T == "wall" || Baskin.CellAt(B, tx, ty).O != null) break;
                line.Add(P(tx, ty));
                var t = Baskin.UnitAt(B, tx, ty);
                // Aşama 10: cıvatalı makine çekilmez, hat yine orada biter (Core RunWinch ile aynı kural: Baskin.WinchPulls)
                if (t != null) return Baskin.WinchPulls(B, t) ? (line, line.Count - 1, t) : (line, -1, null);
                bool load = B.CargoX == tx && B.CargoY == ty || B.Units.Any(b => Baskin.LyingBody(b) && b.X == tx && b.Y == ty);
                if (load) return (line, line.Count - 1, null);
            }
            return (line, -1, null);
        }

        /// <summary>
        /// Vincin çektiğinin bir evre sonunda varacağı kare (<see cref="MoveRules.WinchPull"/> adım, vince doğru; Core <c>PullUnit</c>,
        /// <c>PullLoad</c>): varış tahtada, geçilebilir, ayakta birimsiz. Ayakta birim turnikeye uyar ve hidrolik yağda kayar (Core
        /// <c>Slide</c>); yük ve gövde turnikeye bakmaz, kaymaz, ama kapı ve menfeze çekilmez. <paramref name="winch"/>: vincin karesi
        /// (kurulmadan önceki önizlemede tahtada henüz yok; varış olamaz). Kablo hasarı sayılmaz (çekilen düşerse Core'da çekiş erken biter).
        /// </summary>
        public static Baskin.Pt PullEnd(Baskin.BState B, Baskin.Pt from, int dir, bool load, Baskin.Pt? winch = null)
        {
            var at = from;
            if (B?.Moves == null || dir < 0) return at;
            var d = Baskin.Dirs[dir];
            var back = new[] { -d[0], -d[1] };
            for (int i = 0; i < B.Moves.WinchPull; i++)
            {
                int nx = at.X + back[0], ny = at.Y + back[1];
                if (!Passable(B, nx, ny) || Baskin.UnitAt(B, nx, ny) != null || Is(winch, nx, ny)) break;
                if (load && (Baskin.CellAt(B, nx, ny).Door != 0 || Baskin.CellAt(B, nx, ny).Vent != 0)) break;
                if (!load && B.Ev != null && !Baskin.CanStep(B, at.X, at.Y, nx, ny)) break;
                at = P(nx, ny);
                if (!load) at = SlideEnd(B, at, back, null, winch);
            }
            return at;
        }

        static bool Is(Baskin.Pt? p, int x, int y) => p.HasValue && p.Value.X == x && p.Value.Y == y;

        /// <summary>
        /// Hidrolik yağ olayında (Core <c>Slide</c>) yağdaki birimin kayıp durduğu kare: yağ bitene ya da önü (tahta dışı, duvar, nesne,
        /// ayakta birim) kapanana dek <paramref name="d"/> yönünde. Çarpışma hasarı ve kablo sayılmaz. Olay yoksa <paramref name="at"/>.
        /// <paramref name="self"/>: kayan birimin kendisi (kendi karesi engel değil); <paramref name="extra"/>: tahtada henüz olmayan engel.
        /// </summary>
        static Baskin.Pt SlideEnd(Baskin.BState B, Baskin.Pt at, int[] d, Baskin.Unit self, Baskin.Pt? extra = null)
        {
            if (B.Ev == null || B.Ev.Id != "yag") return at;
            for (int step = 0; step < Baskin.N && Baskin.CellAt(B, at.X, at.Y).Oil; step++)
            {
                int nx = at.X + d[0], ny = at.Y + d[1];
                if (!Inb(nx, ny) || Baskin.CellAt(B, nx, ny).T == "wall" || Baskin.CellAt(B, nx, ny).O != null) break;
                var o = Baskin.UnitAt(B, nx, ny);
                if (o != null && o != self || Is(extra, nx, ny)) break;
                at = P(nx, ny);
            }
            return at;
        }

        /// <summary>
        /// Yemin <paramref name="phases"/> makine evresindeki yolu (x, y'den başlayarak; evre başına <see cref="MoveRules.DecoyStep"/>
        /// adım): sıradaki kare tahtada, geçilebilir, ayakta birimsiz, kapı ve menfez değil, turnike açıksa yürür; durunca yol biter.
        /// Hidrolik yağda kayar (Core <c>Slide</c>); kablo hasarı sayılmaz (yem düşerse Core'da yol erken biter).
        /// <paramref name="self"/>: yemin kendisi (tahtadaysa; kendi karesi engel sayılmaz).
        /// </summary>
        public static List<Baskin.Pt> DecoyPath(Baskin.BState B, int x, int y, int dir, int phases, Baskin.Unit self = null)
        {
            var path = new List<Baskin.Pt> { P(x, y) };
            if (B?.Moves == null || dir < 0) return path;
            var d = Baskin.Dirs[dir];
            int cx = x, cy = y;
            for (int i = 0; i < B.Moves.DecoyStep * phases; i++)
            {
                int nx = cx + d[0], ny = cy + d[1];
                var other = Baskin.UnitAt(B, nx, ny);
                if (!Passable(B, nx, ny) || other != null && other != self || Baskin.CellAt(B, nx, ny).Door != 0 || Baskin.CellAt(B, nx, ny).Vent != 0) break;
                if (B.Ev != null && !Baskin.CanStep(B, cx, cy, nx, ny)) break;
                cx = nx; cy = ny;
                path.Add(P(cx, cy));
                // Hidrolik yağ: yağa basan yem kayar (Core Slide); kaydığı kareler de yolun parçası
                var slid = SlideEnd(B, P(cx, cy), d, self);
                while (cx != slid.X || cy != slid.Y) { cx += d[0]; cy += d[1]; path.Add(P(cx, cy)); }
            }
            return path;
        }

        /// <summary>Havlamanın erimi: köpeğin karesinden Manhattan uzaklığı ≤ <see cref="MoveRules.BarkRange"/> kareler (y*8+x).</summary>
        public static List<int> BarkArea(Baskin.BState B, int x, int y)
        {
            var o = new List<int>();
            int r = B?.Moves?.BarkRange ?? 0;
            for (int ty = 0; ty < Baskin.N; ty++)
                for (int tx = 0; tx < Baskin.N; tx++)
                    if (System.Math.Abs(tx - x) + System.Math.Abs(ty - y) <= r) o.Add(ty * Baskin.N + tx);
            return o;
        }

        /// <summary>Erimdeki ayakta makineler (havlamanın satırı için).</summary>
        public static int MachinesInBark(Baskin.BState B, int x, int y)
        {
            int r = B?.Moves?.BarkRange ?? 0;
            return B == null ? 0 : B.Units.Count(e => e.Side == 'e' && Baskin.OnBoard(e) && System.Math.Abs(e.X - x) + System.Math.Abs(e.Y - y) <= r);
        }

        /// <summary>
        /// Eylemin (x, y) hedefindeki önizlemesi: <paramref name="B"/> eylemden önceki durum, <paramref name="u"/> eylemi veren,
        /// <paramref name="preview"/> aynı eylemin Core önizlemesi (null ise boş). <paramref name="nextKnown"/>: koklanacak sıradaki
        /// odanın planı zaten biliniyor mu (yalnız satır).
        /// </summary>
        public static MovePreview Preview(Baskin.BState B, Baskin.Unit u, string kind, int x, int y, Baskin.PreviewResult preview, bool nextKnown = false)
        {
            var o = new MovePreview();
            if (B?.Room == null || B.Moves == null || u == null || preview == null) return o;
            var m = B.Moves;
            var fx = preview.Fx ?? new List<Baskin.Fx>();
            int dir = DirTo(u.X, u.Y, x, y);
            switch (kind)
            {
                case Baskin.ActWire:
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.T("TEL"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_eye_s", Loc.T("Yürüyüşü keser · atışı geçirir"), true));
                    o.Rows.Add(Row("ic_eye_s", Loc.F("Can {0}", m.WireHp)));
                    break;
                case Baskin.ActPlank:
                {
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.T("KALAS"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_eye_s", Loc.T("Yürüyüşü ve atışı keser"), true));
                    int cut = CutIntents(B, preview);
                    if (cut > 0) o.Rows.Add(Row("ic_x_s", Loc.F("Kesilen niyet: {0}", cut), true));
                    break;
                }
                case Baskin.ActCharge:
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.T("YIKILIR"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_x_s", Loc.T("Duvar kalıcı zemine döner"), true));
                    if (fx.Any(f => f.K == "debris")) o.Rows.Add(Row("ic_bolt_s", Loc.T("Moloz: bir kare öteye 1 hasar")));
                    // Aşama 10: çift cidar tek hamlede bedelini harcar (Core Baskin.DoubleWallCost; Delici şarjla az)
                    if (Baskin.IsDoubleWall(B, x, y)) o.Rows.Add(Row("ic_x_s", Loc.F("Çift cidar: {0} hak harcanır", Baskin.DoubleWallCost(B))));
                    break;
                case Baskin.ActTurret:
                {
                    var (line, hit) = TurretLine(B, x, y, dir);
                    if (line.Count > 0) o.Strokes.Add(new MoveStroke { Points = new[] { P(x, y) }.Concat(line).ToArray(), Color = "good" });
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.F("TARET · {0} TUR", Baskin.DeviceTurnsFor(B, u)), Color = "ink.green" });
                    if (hit >= 0) o.Labels.Add(new MoveLabel { X = line[hit].X, Y = line[hit].Y, Text = Loc.F("−{0}", 1), Color = "sade.status.threat" });
                    o.Rows.Add(Row("ic_bolt_s", Loc.T("Her makine evresinin sonunda hattaki ilk makine −1"), true));
                    o.Rows.Add(Row("ic_eye_s", Loc.F("Hat: {0} kare", line.Count)));
                    o.Rows.Add(Row("ic_x_s", Loc.T("Bu tur yetenek kullanılamaz")));
                    break;
                }
                case Baskin.ActDecoy:
                {
                    var path = DecoyPath(B, x, y, dir, Baskin.DeviceTurnsFor(B, u));
                    if (path.Count > 1) o.Strokes.Add(new MoveStroke { Points = path.ToArray(), Color = "good", Dashed = true, Tip = true });
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.T("YEM"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_eye_s", Loc.T("Makineler onu ekip gibi hedef alır"), true));
                    o.Rows.Add(Row("ic_boots_s", Loc.F("Evre başına {0} kare yürür · {1} tur", m.DecoyStep, Baskin.DeviceTurnsFor(B, u))));
                    break;
                }
                case Baskin.ActWinch:
                {
                    var (line, hit, pulled) = WinchLine(B, x, y, dir);
                    if (line.Count > 0) o.Strokes.Add(new MoveStroke { Points = new[] { P(x, y) }.Concat(line).ToArray(), Color = "good", Tip = hit >= 0 });
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.F("VİNÇ · {0} TUR", Baskin.DeviceTurnsFor(B, u)), Color = "ink.green" });
                    // Aşama 10: hattın sonundaki cıvatalı makine çekilmez (etiket kırmızı)
                    var end0 = line.Count > 0 ? Baskin.UnitAt(B, line[line.Count - 1].X, line[line.Count - 1].Y) : null;
                    if (hit < 0 && Baskin.Bolted(B, end0))
                        o.Labels.Add(new MoveLabel { X = end0.X, Y = end0.Y, Text = FacilityCounters.Short(FacilityCounters.Push), Color = "sade.status.threat" });
                    if (hit >= 0)
                    {
                        o.Labels.Add(new MoveLabel { X = line[hit].X, Y = line[hit].Y, Text = Loc.F("ÇEKER · {0}", m.WinchPull), Color = "ink.green" });
                        var end = PullEnd(B, line[hit], dir, pulled == null, P(x, y));
                        if (pulled != null && (end.X != pulled.X || end.Y != pulled.Y)) o.Ghosts.Add(new MoveGhost { Unit = pulled.Id, X = end.X, Y = end.Y });
                    }
                    o.Rows.Add(Row("ic_eye_s", Loc.F("Her evrenin sonunda hattaki ilk birimi, düşeni ya da yükü {0} kare çeker", m.WinchPull), true));
                    o.Rows.Add(Row("ic_eye_s", Loc.F("Hat: {0} kare", line.Count)));
                    break;
                }
                case Baskin.ActClamp:
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.F("ELE GEÇER · {0} TUR", Baskin.ClampTurnsFor(B, u)), Color = "ink.green" });
                    o.Rows.Add(Row("ic_eye_s", Loc.F("Makine {0} oyuncu turu senin: yürür ve komut verir", Baskin.ClampTurnsFor(B, u)), true));
                    o.Rows.Add(Row("ic_x_s", Loc.T("Sonra bir planı niyetsiz geçirir")));
                    o.Rows.Add(Row("ic_x_s", Loc.T("Kelepçe tükenir")));
                    break;
                case Baskin.ActBark:
                    o.Area.AddRange(BarkArea(B, u.X, u.Y));
                    o.Labels.Add(new MoveLabel { X = u.X, Y = u.Y, Text = Loc.T("HAV"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_eye_s", Loc.F("Erimdeki {0} makine sonraki planında köpeğe yönelir", MachinesInBark(B, u.X, u.Y)), true));
                    o.Rows.Add(Row("ic_eye_s", Loc.T("Etki niyetlere tur sonunda yansır")));
                    break;
                case Baskin.ActFetch:
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.T("YÜK"), Color = "ink.green" });
                    o.Rows.Add(Row("ic_eye_s", Loc.T("Köpek yükü taşır; çıkışa varınca yük çıkar"), true));
                    break;
                case Baskin.ActSniff:
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = Loc.F("KOKLA · PAFTA {0}", B.Room.Index + 1), Color = "ink.green" });
                    o.Rows.Add(Row("ic_eye_s", Loc.T("Sıradaki odanın planı çıkar (robot göstermez)"), true));
                    if (nextKnown) o.Rows.Add(Row("ic_eye_s", Loc.T("Bu odanın planı zaten biliniyor")));
                    break;
                case Baskin.ActCommand:
                {
                    var target = Baskin.UnitAt(B, x, y);
                    bool plate = fx.Any(f => f.K == "plate-give");
                    string label = plate ? Loc.T("PLAKA") : u.Type == "bomba" ? Loc.T("PATLAR") : u.Type == "kilit" ? Loc.T("KİLİTLER") : u.Type == "bakim" ? Loc.T("VURUR") : Loc.T("ATEŞ");
                    o.Labels.Add(new MoveLabel { X = x, Y = y, Text = label, Color = plate ? "ink.green" : "sade.status.threat" });
                    if (u.Type == "kilit") o.Rows.Add(Row("ic_x_s", Loc.T("Kilitlenen makine sonraki planında niyetsiz"), true));
                    else o.Rows.Add(Row("ic_bolt_s", Loc.F("{0} · {1}", FacilityGearText.Name(kind), FacilityGearText.CommandDetail(u.Type)), true));
                    break;
                }
            }
            return o;
        }

        /// <summary>Eylemden sonra hedefi değişen ya da hedefsiz kalan makine niyeti sayısı (kalasın kestiği atışlar).</summary>
        static int CutIntents(Baskin.BState B, Baskin.PreviewResult preview)
        {
            if (preview?.Intents == null) return 0;
            var before = Baskin.Intents(B);
            int n = 0;
            foreach (var i in before.Where(i => i.HasTarget && !i.TargetNone))
            {
                var a = preview.Intents.FirstOrDefault(j => j.Unit == i.Unit);
                if (a == null || !a.HasTarget || a.TargetNone || a.TargetX != i.TargetX || a.TargetY != i.TargetY) n++;
            }
            return n;
        }
    }
}
