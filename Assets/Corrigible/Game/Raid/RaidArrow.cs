using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Tahtanın üstünde düz bir ok (gövde + uç), istenirse kesik. Renk köşe renginden gelir (UI/Default tabanlı malzeme).
    /// Elle çizilmiş ok (yağlı kalem): gövde hafifçe dalgalanır, uçlarda incelir, uç açık bir V'dir; dalga uçların
    /// konumundan türetilir, eşitlemede titremez.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RaidArrow : MonoBehaviour
    {
        Mesh mesh;

        /// <param name="hand">Elle çizim: gövdenin yana dalgalanma genliği (dünya birimi); 0 düz ok.</param>
        /// <param name="tip">Ok ucu (kalkanın çift şeridinde yok).</param>
        /// <param name="headLength">Ucun boyu (dünya birimi); verilmezse kalınlığın 4,6 katı.</param>
        public void Set(Vector3 from, Vector3 to, Color color, bool dashed, float width = 0.04f, float arcHeight = 0, float hand = 0, bool tip = true, float headLength = -1)
        {
            width = Mathf.Max(0.001f, width);
            if (hand > 0) { SetHand(from, to, color, width, arcHeight, hand); return; }
            var verts = new System.Collections.Generic.List<Vector3>();
            var cols = new System.Collections.Generic.List<Color>();
            var tris = new System.Collections.Generic.List<int>();
            var dir = to - from;
            float len = dir.magnitude;
            EnsureMesh();
            if (len < 0.01f) { mesh.Clear(); return; }
            dir /= len;
            var side = Vector3.Cross(Vector3.up, dir).normalized * (width / 2);
            float head = tip ? Mathf.Min(headLength > 0 ? headLength : width * 4.6f, len * 0.35f) : 0;
            float shaft = len - head;
            void Quad(Vector3 a, Vector3 b)
            {
                int s = verts.Count;
                verts.Add(transform.InverseTransformPoint(a - side)); verts.Add(transform.InverseTransformPoint(a + side)); verts.Add(transform.InverseTransformPoint(b + side)); verts.Add(transform.InverseTransformPoint(b - side));
                for (int i = 0; i < 4; i++) cols.Add(color);
                tris.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            Vector3 Point(float distance) => from + dir * distance + Vector3.up * (Mathf.Sin(distance / len * Mathf.PI) * arcHeight);
            if (arcHeight > 0)
            {
                float step = Mathf.Max(0.02f, width * 2.6f);
                for (float t = 0; t < shaft; t += step)
                    if (!dashed || ((int)(t / (width * 3.3f)) & 1) == 0) Quad(Point(t), Point(Mathf.Min(shaft, t + step)));
            }
            else if (dashed)
            {
                float dash = width * 3.3f, gap = width * 2;
                for (float t = 0; t < shaft; t += dash + gap) Quad(from + dir * t, from + dir * Mathf.Min(shaft, t + dash));
            }
            else Quad(from, from + dir * shaft);
            if (tip)
            {
                int h = verts.Count;
                // Uç genişliği boyunun ~%70'i
                var hs = Vector3.Cross(Vector3.up, dir).normalized * Mathf.Max(width * 1.2f, head * 0.36f);
                verts.Add(transform.InverseTransformPoint(Point(shaft) - hs)); verts.Add(transform.InverseTransformPoint(Point(shaft) + hs)); verts.Add(transform.InverseTransformPoint(to));
                cols.Add(color); cols.Add(color); cols.Add(color);
                tris.AddRange(new[] { h, h + 1, h + 2 });
            }
            mesh.Clear();
            mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        void SetHand(Vector3 from, Vector3 to, Color color, float width, float arcHeight, float hand)
        {
            EnsureMesh();
            var dir = to - from;
            float len = dir.magnitude;
            if (len < 0.01f) { mesh.Clear(); return; }
            dir /= len;
            var across = Vector3.Cross(Vector3.up, dir).normalized;
            // Aynı ok her eşitlemede aynı çizilir: dalganın evresi uçların konumundan
            float seed = Mathf.Abs(Mathf.Sin(from.x * 12.9898f + from.z * 78.233f + to.x * 37.719f + to.z * 4.581f) * 43758.55f) % 6.283f;
            float head = Mathf.Min(width * 5.5f, len * 0.4f);
            int n = Mathf.Max(8, Mathf.CeilToInt(len / 0.08f));
            var points = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n, d = t * len;
                float wave = Mathf.Sin(t * 7.1f + seed) * 0.6f + Mathf.Sin(t * 15.3f + seed * 1.7f) * 0.4f;
                // Uçlarda dalga söner: ok kareden kareye tam gider
                points[i] = from + dir * d + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight) + across * (wave * hand * Mathf.Sin(t * Mathf.PI));
            }
            var verts = new System.Collections.Generic.List<Vector3>();
            var cols = new System.Collections.Generic.List<Color>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= n; i++)
            {
                var along = points[Mathf.Min(n, i + 1)] - points[Mathf.Max(0, i - 1)]; along.y = 0;
                var side = Vector3.Cross(Vector3.up, along.normalized);
                float t = (float)i / n;
                // Kalem baskısı: başta ince, ortada dolgun, uca doğru yine ince
                float w = width * (0.55f + 0.55f * Mathf.Sin(Mathf.Lerp(0.15f, 0.85f, t) * Mathf.PI)) * 0.5f;
                verts.Add(transform.InverseTransformPoint(points[i] - side * w));
                verts.Add(transform.InverseTransformPoint(points[i] + side * w));
                cols.Add(color); cols.Add(color);
                if (i > 0) { int s = (i - 1) * 2; tris.AddRange(new[] { s, s + 1, s + 3, s, s + 3, s + 2 }); }
            }
            // Uç: iki kalem çizgisi (açık V)
            var tip = points[n];
            var back = (points[n] - points[Mathf.Max(0, n - 2)]); back.y = 0; back = -back.normalized;
            foreach (float angle in new[] { 28f, -28f })
            {
                var wing = Quaternion.AngleAxis(angle, Vector3.up) * back;
                var end = tip + wing * head;
                var side = Vector3.Cross(Vector3.up, wing) * (width * 0.45f);
                int s = verts.Count;
                verts.Add(transform.InverseTransformPoint(tip - side)); verts.Add(transform.InverseTransformPoint(tip + side));
                verts.Add(transform.InverseTransformPoint(end + side * 0.6f)); verts.Add(transform.InverseTransformPoint(end - side * 0.6f));
                for (int i = 0; i < 4; i++) cols.Add(color);
                tris.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            mesh.Clear();
            mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        void EnsureMesh()
        {
            if (mesh != null) return;
            mesh = new Mesh { name = "Raid ink mark" };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        /// <summary>Karo üzerindeki şerit, tarama veya sınır; yeni malzeme yaratmaz.</summary>
        public void SetLines(System.Collections.Generic.IList<(Vector3 from, Vector3 to)> lines, Color color, float width, bool dashed = false)
        {
            var v = new System.Collections.Generic.List<Vector3>(); var c = new System.Collections.Generic.List<Color>(); var t = new System.Collections.Generic.List<int>();
            AddStrokes(lines, color, width, dashed, v, c, t);
            Apply(v, c, t);
        }

        static void AddStrokes(System.Collections.Generic.IList<(Vector3 from, Vector3 to)> lines, Color color, float width, bool dashed,
            System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<Color> c, System.Collections.Generic.List<int> t)
        {
            width = Mathf.Max(0.001f, width);
            void Stroke(Vector3 a, Vector3 b)
            {
                var side = Vector3.Cross(Vector3.up, (b - a).normalized) * width * 0.5f;
                int s = v.Count;
                v.Add(a - side); v.Add(a + side); v.Add(b + side); v.Add(b - side);
                for (int i = 0; i < 4; i++) c.Add(color);
                t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            foreach (var pair in lines)
            {
                if (!dashed) { Stroke(pair.from, pair.to); continue; }
                float length = Vector3.Distance(pair.from, pair.to), dash = width * 3.1f, gap = width * 2;
                var direction = (pair.to - pair.from).normalized;
                for (float i = 0; i < length; i += dash + gap) Stroke(pair.from + direction * i, pair.from + direction * Mathf.Min(length, i + dash));
            }
        }

        void Apply(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<Color> c, System.Collections.Generic.List<int> t)
        {
            EnsureMesh();
            mesh.Clear(); mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
        }

        static System.Collections.Generic.List<(Vector3, Vector3)> Square(float h) => new System.Collections.Generic.List<(Vector3, Vector3)>
        {
            (new Vector3(-h, 0, -h), new Vector3(h, 0, -h)), (new Vector3(h, 0, -h), new Vector3(h, 0, h)),
            (new Vector3(h, 0, h), new Vector3(-h, 0, h)), (new Vector3(-h, 0, h), new Vector3(-h, 0, -h)),
        };

        /// <summary>raid.js frameTex: kalın dış, ince iç kare (seçim, yetenek hedefi).</summary>
        public void SetDoubleFrame(float outerSize, float outerWidth, float innerSize, float innerWidth, Color color)
        {
            var v = new System.Collections.Generic.List<Vector3>(); var c = new System.Collections.Generic.List<Color>(); var t = new System.Collections.Generic.List<int>();
            AddStrokes(Square(outerSize * 0.5f), color, outerWidth, false, v, c, t);
            AddStrokes(Square(innerSize * 0.5f), color, innerWidth, false, v, c, t);
            Apply(v, c, t);
        }

        public void SetFrame(float size, float width, Color color, bool dashed = false, bool doubleLine = false)
        {
            var lines = Square(size * 0.5f);
            if (doubleLine) lines.AddRange(Square(size * 0.5f - 0.08f));
            SetLines(lines, color, width, dashed);
        }

        /// <summary>
        /// raid.js hatchTex: çizgiler karonun köşegeni boyunca, dik aralık <paramref name="pitch"/>. Kamera 45°'den baktığı için
        /// hareket taraması (<paramref name="acrossX"/>) ekranda dikey, isabet taraması yatay okunur.
        /// </summary>
        public void SetHatch(float size, Color color, float width, bool acrossX, float pitch = 0.076f)
        {
            float h = size * 0.5f;
            var d = (acrossX ? new Vector2(1, 1) : new Vector2(1, -1)).normalized;
            var n = new Vector2(-d.y, d.x);
            var lines = new System.Collections.Generic.List<(Vector3, Vector3)>();
            bool Clip(float p, float dir, ref float t0, ref float t1)
            {
                if (Mathf.Abs(dir) < 1e-5f) return Mathf.Abs(p) <= h;
                float a = (-h - p) / dir, b = (h - p) / dir;
                t0 = Mathf.Max(t0, Mathf.Min(a, b)); t1 = Mathf.Min(t1, Mathf.Max(a, b));
                return t0 < t1;
            }
            float reach = h * Mathf.Sqrt(2);
            for (float o = -reach + pitch * 0.5f; o < reach; o += pitch)
            {
                float t0 = -2 * reach, t1 = 2 * reach;
                if (!Clip(n.x * o, d.x, ref t0, ref t1) || !Clip(n.y * o, d.y, ref t0, ref t1)) continue;
                var a = n * o + d * t0; var b = n * o + d * t1;
                lines.Add((new Vector3(a.x, 0, a.y), new Vector3(b.x, 0, b.y)));
            }
            SetLines(lines, color, width);
        }

        public void SetCircle(float radius, Color color, float width, bool dashed = false, bool rays = false)
        {
            var lines = new System.Collections.Generic.List<(Vector3, Vector3)>();
            Vector3 At(float a, float r) => new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            // kesikli: raid.js ghostWake gibi 12 kesik, %60 mürekkep
            for (int i = 0; i < 64; i++) if (!dashed || Mathf.Repeat((i + 0.5f) / 64f * 12f, 1f) < 0.6f) lines.Add((At(i * Mathf.PI / 32, radius), At((i + 1) * Mathf.PI / 32, radius)));
            if (rays) for (int i = 0; i < 8; i++) lines.Add((At(i * Mathf.PI / 4, radius + 0.05f), At(i * Mathf.PI / 4, radius + 0.14f)));
            SetLines(lines, color, width);
        }

        public void SetOpacity(float alpha)
        {
            if (mesh == null) return;
            var colors = mesh.colors;
            for (int i = 0; i < colors.Length; i++) colors[i].a = alpha;
            mesh.colors = colors;
        }

        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
