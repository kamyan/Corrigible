using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Ekran yakalama (PROMPT §9.2): açık sahnenin canvas'ları geçici olarak kameraya bağlanıp istenen çözünürlükte
    /// bir RenderTexture'a çizilir ve <c>Docs/UI_C/captures/&lt;ad&gt;_&lt;tarih&gt;.png</c>'ye yazılır. Edit ve Play modda çalışır;
    /// Game görünümünün boyutuna bağlı değildir (en-boy denemeleri için genişlik/yükseklik verilebilir).
    /// </summary>
    public static class DeskCapture
    {
        public const string Dir = "Docs/UI_C/captures";

        [MenuItem("Corrigible/Desk UI/Capture Game View")]
        static void Menu() => Debug.Log("[Desk] " + Capture(SceneManager.GetActiveScene().name));

        public static string Capture(string name, int width = 1920, int height = 1080, string path = null)
        {
            Directory.CreateDirectory(Dir);
            path ??= Path.Combine(Dir, $"{name}_{DateTime.Now:yyyy-MM-dd_HHmmss}.png");

            var cam = Camera.main;
            GameObject temp = null;
            if (cam == null)
            {
                temp = new GameObject("_DeskCaptureCam") { hideFlags = HideFlags.HideAndDontSave };
                cam = temp.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = true;
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var saved = new List<(Canvas, RenderMode, Camera, float)>();
            foreach (var c in canvases)
            {
                if (!c.isRootCanvas || !c.isActiveAndEnabled) continue;
                saved.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                // Sıralama: yüksek sortingOrder kameraya yakın
                c.planeDistance = 100 - Mathf.Clamp(c.sortingOrder, 0, 1000) * 0.09f;
            }
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            foreach (var c in canvases) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            // Geçişler bitmiş hâliyle yakalansın (olay modalının açılışı)
            foreach (var m in UnityEngine.Object.FindObjectsByType<EventModalView>(FindObjectsSortMode.None)) m.CompleteAnimation();
            // Konuma bağlı görünümler (seçim ipliği) yeni çözünürlükte yeniden hesaplansın
            foreach (var t in UnityEngine.Object.FindObjectsByType<SelectionThreadView>(FindObjectsSortMode.None)) t.Refresh();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());

            cam.targetTexture = prevTarget;
            foreach (var (c, mode, wc, pd) in saved) { c.renderMode = mode; c.worldCamera = wc; c.planeDistance = pd; }
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            if (temp != null) UnityEngine.Object.DestroyImmediate(temp);
            Canvas.ForceUpdateCanvases();
            return Path.GetFullPath(path);
        }
    }
}
