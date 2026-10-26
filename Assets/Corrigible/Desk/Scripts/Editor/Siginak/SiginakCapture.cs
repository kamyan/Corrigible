using System.IO;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Sığınak odasının 3B görüntüsü (PROMPT §7; reference/renders ile karşılaştırma için Overlay arayüz olmadan): ana kamera
    /// rig pozundan render edilir, sonra eski pozuna döner. Overlay dahil durumlu set için <see cref="SiginakCaptureSet"/>.
    /// </summary>
    public static class SiginakCapture
    {
        public const string OutDir = "Docs/UI_Siginak/screens_unity";

        /// <summary>Tasarım panolarının poz adları → rig pozu.</summary>
        static readonly System.Collections.Generic.Dictionary<string, SiginakPose> PoseNames = new System.Collections.Generic.Dictionary<string, SiginakPose>
        {
            ["K1"] = SiginakPose.Desk, ["Map"] = SiginakPose.Map, ["K2"] = SiginakPose.Map, ["K3"] = SiginakPose.Map,
            ["K4"] = SiginakPose.Shelf, ["K5a"] = SiginakPose.Crt, ["K6a"] = SiginakPose.Shift, ["K6b"] = SiginakPose.Map, ["K7"] = SiginakPose.Map,
        };

        static SiginakCameraRig Rig() => Object.FindFirstObjectByType<SiginakCameraRig>(FindObjectsInactive.Include);

        /// <summary>Pozu (K1, Map/K2/K3/K6b/K7, K4, K5a, K6a) sığınak rig'iyle render eder; <paramref name="gray"/> renk körlüğü denetimi için gri kopya da yazar.</summary>
        public static string Render(string pose = "K1", string file = null, bool gray = false, int width = 1920, int height = 1080)
        {
            var rig = Rig();
            if (rig == null) return "Sahnede SiginakCameraRig yok (DeskGame açık mı?)";
            if (!PoseNames.TryGetValue(pose, out var rigPose)) return "Poz yok: " + pose + " (K1, Map, K4, K5a, K6a)";
            var cam = rig.GetComponent<Camera>();
            var before = rig.Current;
            try
            {
                rig.Snap(rigPose);
                string path = Path.Combine(OutDir, file ?? pose + ".png");
                Write(cam, width, height, path, false);
                if (gray) Write(cam, width, height, Path.ChangeExtension(path, null) + "_gray.png", true);
                return path;
            }
            finally { rig.Snap(before); }
        }

        /// <summary>Serbest bakış (ayrıntı denetimi): dünya konumundan hedefe, dikey FOV.</summary>
        public static string RenderFrom(Vector3 position, Vector3 target, float fieldOfView, string file, int width = 1920, int height = 1080)
        {
            var rig = Rig();
            if (rig == null) return "Sahnede SiginakCameraRig yok (DeskGame açık mı?)";
            var cam = rig.GetComponent<Camera>();
            var before = rig.Current;
            try
            {
                cam.usePhysicalProperties = false;
                cam.lensShift = Vector2.zero;
                cam.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
                cam.fieldOfView = fieldOfView;
                string path = Path.Combine(OutDir, file);
                Write(cam, width, height, path, false);
                return path;
            }
            finally { rig.Snap(before); }
        }

        static void Write(Camera cam, int width, int height, string path, bool gray)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            var previous = RenderTexture.active;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                if (gray)
                {
                    var px = tex.GetPixels();
                    for (int i = 0; i < px.Length; i++) { float g = px[i].grayscale; px[i] = new Color(g, g, g); }
                    tex.SetPixels(px);
                }
                tex.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(tex);
            }
        }
    }
}
