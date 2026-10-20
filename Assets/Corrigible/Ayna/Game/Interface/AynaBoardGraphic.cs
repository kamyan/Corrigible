using System;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    // Tek uGUI ağı: tahta, alçak poligonlu parçalar ve yalnız oyuncunun bildiği işaretler.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AynaBoardGraphic : Graphic, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        [SerializeField] Color floorColor = new Color(.018f, .055f, .082f, 1);
        [SerializeField] Color gridColor = new Color(.13f, .45f, .55f, .72f);
        [SerializeField] Color playerColor = new Color(1, .71f, .28f, 1);
        [SerializeField] Color aynaColor = new Color(.24f, .55f, 1, 1);
        [SerializeField] Color chromeColor = new Color(.36f, .89f, 1, 1);
        [SerializeField] Color dangerColor = new Color(1, .30f, .39f, 1);
        A6BoardVm data;
        int selectedId, hover = -1;
        BoardAction action;
        bool[] legal;
        BoardCommand preview;
        Vector2 origin;
        float halfWidth, halfHeight;
        public event Action<int> CellClicked, CellHovered;

        public void SetBoard(A6BoardVm value, int selected, BoardAction selectedAction, bool[] legalCells, int hoverCell, BoardCommand previewCommand)
        {
            data = value; selectedId = selected; action = selectedAction; legal = legalCells;
            hover = hoverCell; preview = previewCommand; SetVerticesDirty();
        }

        void Measure()
        {
            var r = rectTransform.rect;
            halfWidth = Mathf.Max(1, Mathf.Min((r.width - 32) / 16.6f, (r.height - 100) / 9.6f));
            halfHeight = halfWidth * .52f;
            origin = r.center + new Vector2(0, -halfWidth * .20f);
        }

        Vector2 Cell(int index) => origin + new Vector2((index % 8 - index / 8) * halfWidth, (7 - index % 8 - index / 8) * halfHeight);
        Vector2 Grid(float column, float row) => origin + new Vector2((column - row) * halfWidth, (7 - column - row) * halfHeight);

        public int CellAtScreen(Vector2 point, Camera camera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, point, camera, out var p)) return -1;
            Measure();
            var relative = p - origin;
            int column = Mathf.FloorToInt((7 + relative.x / halfWidth - relative.y / halfHeight) * .5f + .5f);
            int row = Mathf.FloorToInt((7 - relative.x / halfWidth - relative.y / halfHeight) * .5f + .5f);
            return column >= 0 && column < 8 && row >= 0 && row < 8 ? row * 8 + column : -1;
        }

        int PointerCell(PointerEventData e)
        {
            if (data == null) return -1;
            return CellAtScreen(e.position, e.pressEventCamera ?? e.enterEventCamera);
        }

        public override bool Raycast(Vector2 sp, Camera eventCamera)
        { return base.Raycast(sp, eventCamera) && data != null && CellAtScreen(sp, eventCamera) >= 0; }
        public void OnPointerClick(PointerEventData e)
        { if (e.button == PointerEventData.InputButton.Left) { int cell = PointerCell(e); if (cell >= 0) CellClicked?.Invoke(cell); } }
        public void OnPointerMove(PointerEventData e) { CellHovered?.Invoke(PointerCell(e)); }
        public void OnPointerExit(PointerEventData e) { CellHovered?.Invoke(-1); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (data == null || data.Tiles == null || data.Tiles.Length != 64) return;
            Measure();
            Platform(vh);
            for (int i = 0; i < 64; i++)
            {
                var p = Cell(i);
                Color fill = ((i % 8 + i / 8) & 1) == 0 ? floorColor : Shade(floorColor, 1.22f);
                Diamond(vh, p, halfWidth, halfHeight, fill);
                DiamondOutline(vh, p, halfWidth, halfHeight, gridColor, .8f);
                if (i % 8 == 0 || i % 8 == 7)
                {
                    var side = i % 8 == 0 ? aynaColor : playerColor;
                    Diamond(vh, p, halfWidth * .92f, halfHeight * .92f, Alpha(side, .045f));
                    if ((i / 8 & 1) == 0) Hatch(vh, p, halfWidth * .88f, halfHeight * .88f, Alpha(side, .25f));
                }
                if (legal != null && legal.Length > i && legal[i])
                {
                    var mark = action == BoardAction.Move ? chromeColor : action == BoardAction.Attack ? dangerColor : playerColor;
                    Diamond(vh, p, halfWidth * .94f, halfHeight * .94f, Alpha(mark, .105f));
                    DiamondOutline(vh, p, halfWidth * .94f, halfHeight * .94f, Alpha(mark, .65f), 1.25f);
                }
            }
            foreach (var intent in data.VisibleIntents)
            {
                var source = data.Units.FirstOrDefault(u => u.Id == intent.UnitId);
                if (source == null || source.Controller == Side.Player || intent.To < 0 || intent.To >= 64) continue;
                var to = Cell(intent.To);
                if (intent.Action == BoardAction.Attack || intent.Action == BoardAction.Ability)
                {
                    Diamond(vh, to, halfWidth * .92f, halfHeight * .92f, Alpha(dangerColor, .13f));
                    DiamondOutline(vh, to, halfWidth * .92f, halfHeight * .92f, Alpha(dangerColor, .9f), 1.8f);
                    if (intent.Action == BoardAction.Ability && source.Kind == UnitKind.Emp)
                        Area(vh, intent.To, dangerColor, .045f);
                }
            }
            foreach (var trap in data.VisibleTraps)
            {
                if (trap.Cell < 0 || trap.Cell >= 64) continue;
                var p = Cell(trap.Cell);
                Diamond(vh, p, halfWidth * .89f, halfHeight * .89f, Alpha(dangerColor, .13f));
                DiamondOutline(vh, p, halfWidth * .89f, halfHeight * .89f, dangerColor, 2.1f, true);
                var a = p + new Vector2(-halfWidth * .24f, 0); var b = p + new Vector2(halfWidth * .24f, 0);
                ZigZag(vh, a, b, halfHeight * .19f, dangerColor, 1.6f);
            }
            if (hover >= 0 && hover < 64)
                DiamondOutline(vh, Cell(hover), halfWidth * .97f, halfHeight * .97f, new Color(.89f, .98f, 1, .95f), 1.4f);
            // Arkadaki parça önce çizilir; raflar birimlerden alçaktır.
            for (int depth = 0; depth <= 14; depth++) for (int row = 0; row < 8; row++)
            {
                int col = depth - row; if (col < 0 || col >= 8) continue;
                int cell = row * 8 + col; var p = Cell(cell);
                var node = data.Nodes.FirstOrDefault(n => n.Cell == cell);
                if (node != null) Node(vh, p, node.Owner);
                if (data.Tiles[cell] == BoardTile.Rack) Cabinet(vh, p, halfWidth * .68f, halfWidth * .36f, false);
                if (data.Tiles[cell] == BoardTile.Civilian) Cabinet(vh, p, halfWidth * .56f, halfWidth * .90f, true);
                for (int s = 0; s < data.CoreCells.Length; s++)
                    if (data.CoreCells[s] == cell && data.CoreHp[s] > 0) Core(vh, p, (Side)s);
                foreach (var unit in data.Units.Where(u => u.Cell == cell && u.Hp > 0)) Unit(vh, p, unit);
            }
            foreach (var intent in data.VisibleIntents)
            {
                var source = data.Units.FirstOrDefault(u => u.Id == intent.UnitId);
                if (source == null || source.Controller == Side.Player || intent.To < 0 || intent.To >= 64 || intent.Action == BoardAction.Wait) continue;
                var from = Cell(source.Cell) + Vector2.up * halfWidth * .10f;
                var to = Cell(intent.To) + Vector2.up * 3;
                Arrow(vh, from, to, aynaColor, intent.Action == BoardAction.Move, 1.8f);
                Flag(vh, from + new Vector2(halfWidth * .34f, halfWidth * .76f), intent.Order + 1, aynaColor);
            }
            foreach (var intent in data.DraftIntents)
            {
                var source = data.Units.FirstOrDefault(u => u.Id == intent.UnitId);
                if (source != null && intent.To >= 0 && intent.To < 64 && intent.Action != BoardAction.Wait)
                    Arrow(vh, Cell(source.Cell) + Vector2.up * 5, Cell(intent.To) + Vector2.up * 5, Alpha(playerColor, .72f), true, 1.5f);
            }
            if (preview != null)
            {
                var source = data.Units.FirstOrDefault(u => u.Id == preview.UnitId);
                if (source != null)
                {
                    var target = Cell(preview.To);
                    if (preview.Action == BoardAction.Ability && (source.Kind == UnitKind.Emp || source.Kind == UnitKind.Overload)) Area(vh, preview.To, playerColor, .09f);
                    var tint = preview.Action == BoardAction.Move ? chromeColor : playerColor;
                    Arrow(vh, Cell(source.Cell) + Vector2.up * 8, target + Vector2.up * 8, tint, false, 2.6f);
                    DiamondOutline(vh, target, halfWidth * .93f, halfHeight * .93f, tint, 2.8f);
                }
            }
        }

        void Platform(VertexHelper vh)
        {
            var corners = new[] { Grid(-.5f, -.5f), Grid(7.5f, -.5f), Grid(7.5f, 7.5f), Grid(-.5f, 7.5f) };
            float thickness = halfWidth * .25f;
            for (int j = 3; j >= 1; j--)
            {
                var offset = new Vector2(0, -thickness * j / 3);
                Poly(vh, corners.Select(p => p + offset).ToArray(), new Color(.018f, .04f, .06f, 1));
                Outline(vh, corners.Select(p => p + offset).ToArray(), Alpha(gridColor, j == 1 ? .7f : .36f), .7f);
            }
            Poly(vh, corners, floorColor); Outline(vh, corners, chromeColor, 1.8f);
            var outer = corners.Select(p => origin + (p - origin) * 1.026f).ToArray();
            Outline(vh, outer, Alpha(playerColor, .35f), .8f);
            for (int n = 0; n <= 8; n++)
            {
                var p = Grid(n - .5f, -.5f);
                Line(vh, p + new Vector2(-4, -2), p + new Vector2(4, 2), gridColor, .8f);
                p = Grid(-.5f, n - .5f);
                Line(vh, p + new Vector2(-4, 2), p + new Vector2(4, -2), gridColor, .8f);
            }
        }

        void Area(VertexHelper vh, int center, Color tint, float opacity)
        {
            for (int y = Mathf.Max(0, center / 8 - 1); y <= Mathf.Min(7, center / 8 + 1); y++)
                for (int x = Mathf.Max(0, center % 8 - 1); x <= Mathf.Min(7, center % 8 + 1); x++)
                    Diamond(vh, Cell(y * 8 + x), halfWidth * .91f, halfHeight * .91f, Alpha(tint, opacity));
        }

        void Node(VertexHelper vh, Vector2 p, Side owner)
        {
            var tint = SideColor(owner);
            Ellipse(vh, p, halfWidth * .43f, halfHeight * .43f, Alpha(tint, .1f), tint, 1.6f, owner == Side.None);
            Box(vh, p, halfWidth * .24f, halfWidth * .17f, tint, .26f);
            var mark = p + Vector2.up * halfWidth * .55f;
            SideSymbol(vh, mark, halfWidth * .09f, owner, tint);
            Line(vh, p + Vector2.up * halfWidth * .24f, mark - Vector2.up * 7, Alpha(tint, .4f), 1);
        }

        void Cabinet(VertexHelper vh, Vector2 p, float radius, float height, bool civil)
        {
            var tint = civil ? chromeColor : new Color(.19f, .66f, .84f, 1);
            Box(vh, p, radius, height, tint, civil ? .27f : .18f);
            if (civil)
            {
                var cap = p + Vector2.up * height;
                Ellipse(vh, cap, radius * .22f, radius * .12f, Alpha(tint, .1f), tint, 1.4f);
                Line(vh, cap + new Vector2(-radius * .12f, 0), cap + new Vector2(radius * .12f, 0), tint, 1.5f);
                Line(vh, cap + new Vector2(0, -radius * .06f), cap + new Vector2(0, radius * .06f), tint, 1.5f);
            }
            for (int i = 1; i <= (civil ? 5 : 3); i++)
            {
                float y = height * i / (civil ? 7 : 5);
                var a = p + new Vector2(radius * .16f, y - radius * .44f);
                var b = p + new Vector2(radius * .80f, y - radius * .12f);
                Line(vh, a, b, Alpha(tint, .8f), 1);
            }
        }

        void Core(VertexHelper vh, Vector2 p, Side side)
        {
            var tint = SideColor(side);
            Ellipse(vh, p, halfWidth * .64f, halfHeight * .64f, Alpha(tint, .08f), Alpha(tint, .6f), 1);
            Ellipse(vh, p, halfWidth * .50f, halfHeight * .50f, Alpha(tint, .04f), tint, 2.2f);
            Box(vh, p, halfWidth * .41f, halfWidth * .67f, tint, .27f);
            SideSymbol(vh, p + Vector2.up * halfWidth * .87f, halfWidth * .11f, side, tint);
        }

        Color SideColor(Side side) => side == Side.Player ? playerColor : side == Side.Ayna ? aynaColor : new Color(.50f, .65f, .71f, 1);

        void Unit(VertexHelper vh, Vector2 p, UnitVm unit)
        {
            bool done = unit.Moved && unit.Acted;
            var tint = done ? new Color(.43f, .56f, .63f, 1) : SideColor(unit.Side);
            float s = halfWidth;
            if (unit.Id == selectedId)
            {
                Ellipse(vh, p, s * .53f, halfHeight * .53f, Alpha(chromeColor, .11f), chromeColor, 2.4f);
                Ellipse(vh, p, s * .59f, halfHeight * .59f, Color.clear, Alpha(chromeColor, .6f), .8f, true);
            }
            Ellipse(vh, p, s * .40f, halfHeight * .40f, Alpha(tint, .16f), tint, 1.7f);
            Ellipse(vh, p + Vector2.up * 3, s * .36f, halfHeight * .36f, Color.clear, Alpha(tint, .7f), .8f);
            SideSymbol(vh, p + new Vector2(0, -halfHeight * .42f), s * .075f, unit.Side, tint);
            Vector2 body = p + Vector2.up * s * .53f;
            bool air = AynaUnits.Get(unit.Kind).Class == UnitClass.Air;
            if (air) Dashed(vh, p + Vector2.up * 5, body - Vector2.up * s * .10f, Alpha(tint, .7f), 1.2f, 3, 3);
            switch (unit.Kind)
            {
                case UnitKind.Scout: case UnitKind.Sniper: case UnitKind.Emp:
                    Drone(vh, body, s * .36f, tint, unit.Kind == UnitKind.Emp);
                    if (unit.Kind == UnitKind.Sniper) Line(vh, body, body + new Vector2(s * .50f, s * .08f), tint, 3);
                    break;
                case UnitKind.Guardian:
                    Box(vh, p + Vector2.up * s * .05f, s * .33f, s * .85f, tint, .32f);
                    Box(vh, p + Vector2.up * s * .90f, s * .18f, s * .17f, tint, .42f);
                    Line(vh, p + new Vector2(-s * .24f, s * .50f), p + new Vector2(s * .06f, s * .34f), Tint(tint), 2);
                    break;
                case UnitKind.Spider:
                    body = p + Vector2.up * s * .20f;
                    for (int i = -1; i <= 1; i += 2) for (int j = -1; j <= 1; j += 2)
                    {
                        var knee = body + new Vector2(s * .31f * i, s * .17f * j);
                        Line(vh, body, knee, tint, 2);
                        Line(vh, knee, p + new Vector2(s * .47f * i, halfHeight * .44f * j), tint, 1.5f);
                    }
                    Box(vh, body, s * .16f, s * .12f, tint, .35f); break;
                case UnitKind.Overload:
                    Polygon(vh, body, s * .23f, 6, Tint(tint), Shade(tint, .20f), 1.8f);
                    for (int n = 0; n < 6; n++)
                    { float a = n * Mathf.PI / 3; Line(vh, body + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * .28f, body + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * .42f, tint, 1.5f); }
                    break;
                case UnitKind.Intrusion:
                    var apex = p + Vector2.up * s * .84f;
                    var left = p + new Vector2(-s * .24f, s * .25f);
                    var right = p + new Vector2(s * .24f, s * .25f);
                    Poly(vh, new[] { apex, left, p + Vector2.up * s * .11f }, Shade(tint, .19f));
                    Poly(vh, new[] { apex, right, p + Vector2.up * s * .11f }, Shade(tint, .29f));
                    Outline(vh, new[] { apex, right, p + Vector2.up * s * .11f, left }, tint, 1.7f);
                    Line(vh, apex, p + Vector2.up * s * .11f, tint, 1.1f); Line(vh, left, right, tint, 1); break;
                case UnitKind.Relay:
                    Box(vh, p, s * .18f, s * .22f, tint, .25f);
                    Line(vh, p + Vector2.up * s * .19f, p + Vector2.up * s * .85f, tint, 2);
                    Ellipse(vh, p + Vector2.up * s * .70f, s * .27f, s * .12f, Alpha(tint, .1f), tint, 1.8f);
                    SideSymbol(vh, p + Vector2.up * s * .99f, s * .08f, unit.Side, tint); break;
                case UnitKind.Decoy:
                    Box(vh, p + new Vector2(-s * .17f, s * .10f), s * .14f, s * .24f, tint, .25f);
                    Box(vh, p + new Vector2(s * .14f, 0), s * .14f, s * .24f, tint, .20f);
                    Box(vh, p + new Vector2(s * .07f, s * .29f), s * .14f, s * .24f, tint, .32f); break;
                case UnitKind.PhantomOrder:
                    var outline = new[] { body + new Vector2(-s * .22f, -s * .25f), body + new Vector2(-s * .20f, s * .14f), body + new Vector2(0, s * .31f), body + new Vector2(s * .20f, s * .14f), body + new Vector2(s * .22f, -s * .25f), body + new Vector2(s * .07f, -s * .14f), body + new Vector2(-s * .07f, -s * .25f) };
                    Poly(vh, outline, Shade(tint, .16f)); Outline(vh, outline, tint, 1.5f);
                    Line(vh, body + new Vector2(-s * .10f, s * .07f), body + new Vector2(-s * .05f, s * .07f), Tint(tint), 2);
                    Line(vh, body + new Vector2(s * .05f, s * .07f), body + new Vector2(s * .10f, s * .07f), Tint(tint), 2); break;
            }
            if (unit.Controller != unit.Side)
                Ellipse(vh, p, s * .47f, halfHeight * .47f, Color.clear, SideColor(unit.Controller), 2, true);
            Hp(vh, p + Vector2.up * s * (unit.Kind == UnitKind.Guardian ? 1.21f : .96f), unit.Hp, tint);
            if (done)
            {
                var mark = p + new Vector2(s * .35f, s * .65f);
                Line(vh, mark, mark + new Vector2(4, -4), Tint(tint), 1.7f);
                Line(vh, mark + new Vector2(4, -4), mark + new Vector2(11, 5), Tint(tint), 1.7f);
            }
        }

        void Drone(VertexHelper vh, Vector2 p, float r, Color tint, bool emp)
        {
            if (emp) Ellipse(vh, p + Vector2.up * r * .34f, r * 1.12f, r * .55f, Alpha(tint, .05f), Tint(tint), 1.8f);
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2)
            {
                var end = p + new Vector2(r * .78f * x, r * .40f * y);
                Line(vh, p, end, tint, 1.8f);
                Ellipse(vh, end, r * .37f, r * .18f, Alpha(tint, .11f), tint, 1.6f);
            }
            Polygon(vh, p, r * .27f, 6, Tint(tint), Shade(tint, .34f), 1.5f);
        }

        void Hp(VertexHelper vh, Vector2 p, double hp, Color tint)
        {
            int count = Mathf.Clamp(Mathf.CeilToInt((float)hp), 1, 8);
            float w = Mathf.Max(4, halfWidth * .075f), h = w * .95f;
            float start = -(count * (w + 2) + 13) * .5f;
            Quad(vh, p + new Vector2(start - 5, -5), new Vector2(count * (w + 2) + 21, h + 10), new Color(.015f, .03f, .055f, .96f));
            SideSymbol(vh, p + new Vector2(start + 1, h * .5f), 3, tint == aynaColor ? Side.Ayna : Side.Player, tint);
            for (int n = 0; n < count; n++)
                Quad(vh, p + new Vector2(start + 10 + n * (w + 2), 0), new Vector2(w, h), Tint(tint));
        }

        void Box(VertexHelper vh, Vector2 p, float radius, float height, Color tint, float light)
        {
            float depth = radius * .52f;
            Vector2 n = p + new Vector2(0, depth), e = p + new Vector2(radius, 0), s = p + new Vector2(0, -depth), w = p + new Vector2(-radius, 0);
            Vector2 up = Vector2.up * height;
            Poly(vh, new[] { w, s, s + up, w + up }, Shade(tint, light * .65f));
            Poly(vh, new[] { s, e, e + up, s + up }, Shade(tint, light * .85f));
            Poly(vh, new[] { n + up, e + up, s + up, w + up }, Shade(tint, light));
            Outline(vh, new[] { w, s, e, e + up, n + up, w + up }, tint, 1.3f);
            Line(vh, s, s + up, tint, 1.1f); Line(vh, w + up, s + up, tint, 1.1f); Line(vh, s + up, e + up, tint, 1.1f);
            DiamondOutline(vh, p + up, radius * .82f, depth * .82f, Alpha(tint, .72f), .8f);
        }

        static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        static Color Tint(Color c) => Color.Lerp(c, Color.white, .47f);
        static void Poly(VertexHelper vh, Vector2[] points, Color color)
        {
            if (points.Length < 3 || color.a <= 0) return;
            int first = vh.currentVertCount;
            for (int i = 0; i < points.Length; i++) vh.AddVert(points[i], color, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(first, first + i, first + i + 1);
        }
        static void Quad(VertexHelper vh, Vector2 p, Vector2 size, Color color)
        { Poly(vh, new[] { p, p + Vector2.right * size.x, p + size, p + Vector2.up * size.y }, color); }
        static void Line(VertexHelper vh, Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 side = (to - from).normalized; side = new Vector2(-side.y, side.x) * width * .5f;
            Poly(vh, new[] { from - side, from + side, to + side, to - side }, color);
        }
        static void Dashed(VertexHelper vh, Vector2 from, Vector2 to, Color color, float width, float dash = 6, float gap = 4)
        {
            float length = Vector2.Distance(from, to); var direction = (to - from).normalized;
            for (float n = 0; n < length; n += dash + gap)
                Line(vh, from + direction * n, from + direction * Mathf.Min(length, n + dash), color, width);
        }
        static void Outline(VertexHelper vh, Vector2[] points, Color color, float width, bool dashed = false)
        {
            for (int i = 0; i < points.Length; i++)
                if (dashed) Dashed(vh, points[i], points[(i + 1) % points.Length], color, width);
                else Line(vh, points[i], points[(i + 1) % points.Length], color, width);
        }
        static Vector2[] DiamondPoints(Vector2 p, float w, float h) => new[] { p + Vector2.up * h, p + Vector2.right * w, p - Vector2.up * h, p - Vector2.right * w };
        static void Diamond(VertexHelper vh, Vector2 p, float w, float h, Color color) => Poly(vh, DiamondPoints(p, w, h), color);
        static void DiamondOutline(VertexHelper vh, Vector2 p, float w, float h, Color color, float width, bool dashed = false) => Outline(vh, DiamondPoints(p, w, h), color, width, dashed);
        static void Ellipse(VertexHelper vh, Vector2 p, float rx, float ry, Color fill, Color edge, float width, bool dashed = false)
        {
            const int count = 32; var points = new Vector2[count];
            for (int i = 0; i < count; i++) { float a = Mathf.PI * 2 * i / count; points[i] = p + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry); }
            Poly(vh, points, fill);
            for (int i = 0; i < count; i++) if (!dashed || (i / 2 & 1) == 0) Line(vh, points[i], points[(i + 1) % count], edge, width);
        }
        static void Polygon(VertexHelper vh, Vector2 p, float radius, int sides, Color edge, Color fill, float width)
        {
            var points = new Vector2[sides];
            for (int i = 0; i < sides; i++) { float a = Mathf.PI * 2 * i / sides + Mathf.PI * .5f; points[i] = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; }
            Poly(vh, points, fill); Outline(vh, points, edge, width);
        }
        static void SideSymbol(VertexHelper vh, Vector2 p, float radius, Side side, Color color)
        {
            if (side == Side.Player) Diamond(vh, p, radius, radius, color);
            else if (side == Side.Ayna) Polygon(vh, p, radius, 6, color, color, .6f);
            else Ellipse(vh, p, radius, radius, Color.clear, color, 1, true);
        }
        static void Arrow(VertexHelper vh, Vector2 from, Vector2 to, Color color, bool dashed, float width)
        {
            float distance = Vector2.Distance(from, to); if (distance < 6) return;
            var dir = (to - from).normalized; var normal = new Vector2(-dir.y, dir.x);
            Line(vh, from, to, Alpha(color, .17f), width * 4);
            if (dashed) Dashed(vh, from, to, color, width); else Line(vh, from, to, color, width);
            float arrow = Mathf.Min(12, distance * .23f);
            Poly(vh, new[] { to, to - dir * arrow + normal * arrow * .45f, to - dir * arrow - normal * arrow * .45f }, color);
            Ellipse(vh, from, 3, 3, Alpha(color, .1f), color, 1.2f);
        }
        static void Flag(VertexHelper vh, Vector2 p, int number, Color tint)
        {
            Line(vh, p - Vector2.up * 16, p + Vector2.up * 9, tint, 1.1f);
            Quad(vh, p, new Vector2(16, 18), new Color(.015f, .04f, .10f, .96f));
            Outline(vh, new[] { p, p + new Vector2(16, 0), p + new Vector2(16, 18), p + new Vector2(0, 18) }, tint, 1);
            Digit(vh, p + new Vector2(5, 4), number % 10, Tint(tint));
        }
        static void Digit(VertexHelper vh, Vector2 p, int value, Color color)
        {
            int[] masks = { 0x3f, 0x06, 0x5b, 0x4f, 0x66, 0x6d, 0x7d, 0x07, 0x7f, 0x6f };
            var a = new[] { new Vector2(0, 10), new Vector2(6, 10), new Vector2(6, 5), new Vector2(0, 0), new Vector2(0, 5), new Vector2(0, 10), new Vector2(0, 5) };
            var b = new[] { new Vector2(6, 10), new Vector2(6, 5), new Vector2(6, 0), new Vector2(6, 0), new Vector2(0, 0), new Vector2(0, 5), new Vector2(6, 5) };
            for (int i = 0; i < 7; i++) if ((masks[value] & (1 << i)) != 0) Line(vh, p + a[i], p + b[i], color, 1.2f);
        }
        static void Hatch(VertexHelper vh, Vector2 p, float w, float h, Color color)
        {
            for (int n = -2; n <= 2; n++)
            {
                float x = w * n / 3, y = h * (1 - Mathf.Abs(x / w));
                Line(vh, p + new Vector2(x, -y), p + new Vector2(x, y), color, .8f);
            }
        }
        static void ZigZag(VertexHelper vh, Vector2 a, Vector2 b, float amplitude, Color color, float width)
        {
            var last = a;
            for (int n = 1; n <= 6; n++)
            { var next = Vector2.Lerp(a, b, n / 6f) + Vector2.up * (n == 6 ? 0 : (n % 2 == 0 ? amplitude : -amplitude)); Line(vh, last, next, color, width); last = next; }
        }
    }
}
