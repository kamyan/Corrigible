using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Performans ölçümü (PROMPT §12; F7). Play modunda kareler boyunca adım adım çalışır ve Profiler kaydı alır:
    /// 1) UI Details: canvas başına batch sayısı, her batch'in kırılma nedeni ve ilk öğeleri (Game görünümü arka planda
    /// çizilmese de doğru; UnityStats ise yalnız odaktaki Game görünümünde güncellenir),
    /// 2) boşta N kare: masa betiklerinin (…[Invoke]) GC ayırması ve canvas yeniden kurulum işaretleri.
    /// Sonuç <c>Docs/UI_C/captures/F7_performans.txt</c>.
    /// </summary>
    public static class DeskPerfProbe
    {
        const string ReportPath = "Docs/UI_C/captures/F7_performans.txt";
        const int Settle = 4, IdleFrames = 120;

        static readonly Queue<System.Func<bool>> steps = new Queue<System.Func<bool>>();
        static StringBuilder sb;
        public static bool Running => steps.Count > 0;
        public static string Last { get; private set; } = "";

        [MenuItem("Corrigible/Desk UI/Performans ölçümü (Play)")]
        public static string Run()
        {
            if (!Application.isPlaying) return "Play modunda çalıştırın (DeskUI_Sandbox).";
            if (Running) return "Zaten çalışıyor.";
            sb = new StringBuilder();
            sb.AppendLine($"Desk performans ölçümü · {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine("Kaynak: Profiler UI Details (canvas toplayıcısının ürettiği batch'ler; Game görünümünün çizilmesinden bağımsız).");
            Last = "";
            // Boşta GC ve yeniden kurulum: Profiler kayıtları
            int first = -1;
            steps.Enqueue(() =>
            {
                ProfilerDriver.ClearAllFrames();
                ProfilerDriver.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.UI, true);
                ProfilerDriver.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.UIDetails, true);
                ProfilerDriver.enabled = true;
                UnityEngine.Profiling.Profiler.enabled = true;
                return true;
            });
            Wait(Settle);
            steps.Enqueue(() => { first = ProfilerDriver.lastFrameIndex; return true; });
            Wait(IdleFrames);
            steps.Enqueue(() =>
            {
                int last = ProfilerDriver.lastFrameIndex;
                ProfilerDriver.enabled = false;
                UnityEngine.Profiling.Profiler.enabled = false;
                Batches(last - 1);
                Analyze(System.Math.Max(first, ProfilerDriver.firstFrameIndex), last);
                File.WriteAllText(ReportPath, sb.ToString());
                Last = sb.ToString();
                return true;
            });
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return "Başladı; bitince " + ReportPath;
        }

        static void Wait(int frames)
        {
            int left = -1;
            steps.Enqueue(() =>
            {
                if (left < 0) left = frames;
                return --left <= 0;
            });
        }

        static int lastFrame = -1;

        static void Tick()
        {
            if (!Application.isPlaying) { steps.Clear(); EditorApplication.update -= Tick; return; }
            // Oyuncu karesi başına bir adım
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            if (steps.Count == 0) { EditorApplication.update -= Tick; return; }
            if (steps.Peek()()) steps.Dequeue();
        }

        /// <summary>Son karede canvas başına batch sayısı ve her batch'in kırılma nedeni + ilk öğeleri.</summary>
        public static int LastTotal { get; private set; }

        static void Batches(int frame)
        {
            var prop = new ProfilerProperty();
            try
            {
                prop.SetRoot(frame, HierarchyFrameDataView.columnDontSort, 0);
                prop.onlyShowGPUSamples = false;
                var infos = prop.GetUISystemProfilerInfo();
                var ids = prop.GetUISystemBatchInstanceIDs();
                if (infos == null) { sb.AppendLine("\nUI Details verisi yok."); return; }
                int total = 0;
                var lines = new StringBuilder();
                for (int i = 0; i < infos.Length; i++)
                {
                    var info = infos[i];
                    if (info.isBatch) continue;
                    string name = prop.GetUISystemProfilerNameByOffset(info.objectNameOffset);
                    total += info.batchCount;
                    lines.AppendLine($"  {name,-12} batch {info.batchCount,3} (alt dahil {info.totalBatchCount,3}) · köşe {info.vertexCount}");
                    for (int j = 0; j < infos.Length; j++)
                    {
                        var b = infos[j];
                        if (!b.isBatch || b.parentId != info.objectInstanceId) continue;
                        var names = new List<string>();
                        string mat = "";
                        for (int k = 0; k < b.instanceIDsCount; k++)
                        {
                            var o = EditorUtility.InstanceIDToObject(ids[b.instanceIDsIndex + k]);
                            // Kimlik GameObject'e (ya da bileşenine) ait; malzeme o nesnedeki Graphic'ten
                            var go0 = o as GameObject ?? (o as Component)?.gameObject;
                            if (k == 0 && go0 != null && go0.TryGetComponent(out UnityEngine.UI.Graphic g0)) mat = MaterialKey(g0);
                            if (k < 4) names.Add(o == null ? "?" : o.name);
                        }
                        lines.AppendLine($"      {b.batchBreakingReason,-26} {b.instanceIDsCount,3} öğe · [{mat}] {string.Join(", ", names)}{(b.instanceIDsCount > 4 ? " …" : "")}");
                    }
                }
                LastTotal = total;
                sb.AppendLine($"\nUI batch toplamı (son kare): {total}");
                sb.Append(lines);
            }
            finally { prop.Cleanup(); }
        }

        /// <summary>Batch'i belirleyen malzeme + doku (TMP alt ağlarında yedek malzeme adı).</summary>
        static string MaterialKey(UnityEngine.UI.Graphic g)
        {
            var m = g.materialForRendering;
            var tex = g.mainTexture;
            string mn = m == null ? "-" : m.name.Replace(" (Instance)", "");
            return tex == null || mn.Contains(tex.name) ? mn : mn + " · " + tex.name;
        }

        static readonly string[] RebuildMarkers = { "Canvas.BuildBatch", "Canvas.SendWillRenderCanvases", "Layout", "Graphic.PerformUpdate" };

        static void Analyze(int first, int last)
        {
            var gcByScript = new Dictionary<string, (long bytes, int frames)>();
            var markerCalls = new Dictionary<string, (int frames, double ms)>();
            int frames = 0;
            for (int f = first + 1; f <= last; f++)
            {
                using var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnDontSort, false);
                if (view == null || !view.valid) continue;
                frames++;
                var seenThisFrame = new HashSet<string>();
                Walk(view, view.GetRootItemID(), 0, (id, name) =>
                {
                    if (name.EndsWith("[Invoke]") || name.Contains("Corrigible"))
                    {
                        long gc = (long)view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnGcMemory);
                        if (gc > 0)
                        {
                            gcByScript.TryGetValue(name, out var v);
                            gcByScript[name] = (v.bytes + gc, v.frames + 1);
                        }
                    }
                    foreach (var m in RebuildMarkers)
                        if (name == m && seenThisFrame.Add(m))
                        {
                            markerCalls.TryGetValue(m, out var v);
                            markerCalls[m] = (v.frames + 1, v.ms + view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnTotalTime));
                        }
                });
            }
            sb.AppendLine($"\nBoşta {frames} kare (Profiler):");
            if (gcByScript.Count == 0) sb.AppendLine("  Betik GC ayırması: 0 B (hiçbir …[Invoke] örneğinde GC yok)");
            foreach (var kv in gcByScript.OrderByDescending(k => k.Value.bytes))
                sb.AppendLine($"  GC {kv.Key}: {kv.Value.bytes} B, {kv.Value.frames} karede");
            foreach (var m in RebuildMarkers)
                sb.AppendLine(markerCalls.TryGetValue(m, out var v)
                    ? $"  {m}: {v.frames}/{frames} kare, ort. {v.ms / System.Math.Max(1, v.frames):F3} ms"
                    : $"  {m}: 0/{frames} kare");
        }

        static void Walk(HierarchyFrameDataView view, int id, int depth, System.Action<int, string> visit)
        {
            if (depth > 40) return;
            var children = new List<int>();
            view.GetItemChildren(id, children);
            foreach (var c in children)
            {
                visit(c, view.GetItemName(c));
                Walk(view, c, depth + 1, visit);
            }
        }
    }
}
