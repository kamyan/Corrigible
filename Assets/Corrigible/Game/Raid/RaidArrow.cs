using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>Tahtanın üstünde düz bir ok (gövde + uç), istenirse kesik. Renk köşe renginden gelir (UI/Default tabanlı malzeme).</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RaidArrow : MonoBehaviour
    {
        Mesh mesh;

        public void Set(Vector3 from, Vector3 to, Color color, bool dashed, float width = 0.12f)
        {
            if (mesh == null) { mesh = new Mesh { name = "RaidArrow" }; GetComponent<MeshFilter>().sharedMesh = mesh; }
            var verts = new System.Collections.Generic.List<Vector3>();
            var cols = new System.Collections.Generic.List<Color>();
            var tris = new System.Collections.Generic.List<int>();
            var dir = to - from; dir.y = 0;
            float len = dir.magnitude;
            if (len < 0.01f) { mesh.Clear(); return; }
            dir /= len;
            var side = Vector3.Cross(Vector3.up, dir) * (width / 2);
            float head = Mathf.Min(0.38f, len * 0.5f);
            float shaft = len - head;
            void Quad(Vector3 a, Vector3 b)
            {
                int s = verts.Count;
                verts.Add(a - side); verts.Add(a + side); verts.Add(b + side); verts.Add(b - side);
                for (int i = 0; i < 4; i++) cols.Add(color);
                tris.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
            }
            if (dashed)
            {
                const float dash = 0.16f, gap = 0.1f;
                for (float t = 0; t < shaft; t += dash + gap) Quad(from + dir * t, from + dir * Mathf.Min(shaft, t + dash));
            }
            else Quad(from, from + dir * shaft);
            int h = verts.Count;
            var hs = side * 2.6f;
            verts.Add(from + dir * shaft - hs); verts.Add(from + dir * shaft + hs); verts.Add(to);
            cols.Add(color); cols.Add(color); cols.Add(color);
            tris.AddRange(new[] { h, h + 1, h + 2 });
            mesh.Clear();
            mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
