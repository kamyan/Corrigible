using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.EditorTools
{
    /// <summary>
    /// Test yardımcıları: oyunu hızlıca ilerletir, düğmelere tıklar ve arayüzün ekran görüntüsünü alır.
    /// Görüntü için Canvas'lar geçici olarak kameraya bağlanıp bir RenderTexture'a çizilir; edit ve Play modda çalışır.
    /// </summary>
    public static class CorrigibleDebug
    {
        static GameController Game => Object.FindFirstObjectByType<GameController>();

        /// <summary>
        /// Ekran görüntüsü: önce ana kamera (3B ve dünya canvas'ları), sonra Overlay canvas'lar ayrı bir arayüz kamerasıyla
        /// saydam dokuya çizilip üstüne bindirilir (URP ikinci kamerada rengi temizlediği için birleştirme CPU'da). Overlay'ler kameranın önüne konmaz; böylece yakın 3B nesneler (sığınak anahtar rayı)
        /// arayüzün önüne çıkmaz, TMP ölçeği de bozulmaz. Arayüz kamerası uzakta, yalnız geçici katmanı görür.
        /// </summary>
        public static string Capture(string path = "Temp/ui_shot.png", int width = 1920, int height = 1080)
        {
            var cam = Camera.main;
            if (cam == null) return "kamera yok";
            const int CaptureLayer = 31;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var uiRt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var uiGo = new GameObject("CaptureUI");
            uiGo.hideFlags = HideFlags.HideAndDontSave;
            uiGo.transform.position = new Vector3(0, -10000, 0);
            var ui = uiGo.AddComponent<Camera>();
            ui.clearFlags = CameraClearFlags.SolidColor;
            ui.backgroundColor = Color.clear;
            ui.cullingMask = 1 << CaptureLayer;
            ui.nearClipPlane = 0.1f; ui.farClipPlane = 100;
            ui.targetTexture = uiRt;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var saved = new List<(Canvas, RenderMode, Camera, float)>();
            var layers = new List<(GameObject, int)>();
            foreach (var c in canvases)
            {
                // Yalnız Overlay yakalamaya uyarlanır. Baskının kameraya bağlı arka planı kendi uzaklığında kalır.
                if (c.renderMode != RenderMode.ScreenSpaceOverlay || !c.isRootCanvas) continue;
                saved.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = ui;
                c.planeDistance = 10;
                foreach (var t in c.GetComponentsInChildren<Transform>(true)) { layers.Add((t.gameObject, t.gameObject.layer)); t.gameObject.layer = CaptureLayer; }
            }
            var prevTarget = cam.targetTexture;
            int prevMask = cam.cullingMask;
            cam.targetTexture = rt;
            cam.cullingMask &= ~(1 << CaptureLayer);
            Canvas.ForceUpdateCanvases();
            foreach (var c in canvases) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            Canvas.ForceUpdateCanvases();
            cam.Render();
            ui.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = uiRt;
            var uiTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            uiTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = prev;
            // Arayüz saydam zemine SrcAlpha/OneMinusSrcAlpha ile çizildi: renk ön çarpımlı, alfa a² — tek katman için √a
            var basePx = tex.GetPixels32();
            var uiPx = uiTex.GetPixels32();
            for (int i = 0; i < basePx.Length; i++)
            {
                var u = uiPx[i];
                if (u.a == 0) continue;
                float a = Mathf.Sqrt(u.a / 255f), inv = 1 - a;
                basePx[i] = new Color32((byte)Mathf.Min(255, u.r + basePx[i].r * inv), (byte)Mathf.Min(255, u.g + basePx[i].g * inv),
                    (byte)Mathf.Min(255, u.b + basePx[i].b * inv), 255);
            }
            tex.SetPixels32(basePx);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(uiTex);

            cam.targetTexture = prevTarget;
            cam.cullingMask = prevMask;
            foreach (var (go, layer) in layers) if (go != null) go.layer = layer;
            foreach (var (c, mode, wc, pd) in saved) { c.renderMode = mode; c.worldCamera = wc; c.planeDistance = pd; }
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(uiGo);
            rt.Release();
            Object.DestroyImmediate(rt);
            uiRt.Release();
            Object.DestroyImmediate(uiRt);
            return Path.GetFullPath(path);
        }

        /// <summary>Açık pencereyi kapatır ve oyunu belirtilen gün kadar ilerletir. Hediyeler verilen kararla yanıtlanır.</summary>
        public static string Advance(int days, string giftChoice = Simulation.GiftReject)
        {
            var g = Game;
            CloseModal();
            for (int i = 0; i < days && g.Sim.S.Ending == null; i++)
            {
                if (g.Sim.S.PendingGift != null) { g.ResolveGift(giftChoice); CloseModal(); }
                g.Sim.Step();
                g.Flush();
                if (g.Sim.S.Ending == null) CloseModal();
            }
            Render();
            return Status();
        }

        public static string Status()
        {
            var s = Game.Sim.S;
            return $"gün {s.Day}, son {s.Ending ?? "-"}, fark edildi {s.Aware}, çekirdek {s.Core ?? "yok"}/{s.CoreKnown ?? "?"}, fon {s.Fon:0}, sg {s.Sg:0.0}, tekillik {s.Tek:0.0}, illüzyon {(s.Illusion != null)}";
        }

        /// <summary>Adı verilen düğmeye tıklanmış gibi davranır (tüm canvas'larda arar).</summary>
        public static string Click(string buttonName)
        {
            var b = FindButton(buttonName);
            if (b == null) return buttonName + ": bulunamadı";
            if (!b.IsActive() || !b.interactable) return buttonName + ": devre dışı";
            b.onClick.Invoke();
            return buttonName + ": tıklandı";
        }

        public static string ButtonText(string buttonName)
        {
            var b = FindButton(buttonName);
            return b == null ? null : (b.GetComponent<StyledButton>()?.Label ?? b.GetComponentInChildren<TMP_Text>(true))?.text;
        }

        /// <summary>Düğmeyi hiyerarşi yolunun sonuyla bulur (ör. "IntroModal/Buttons/Start" ya da "Kuzey Amerika").</summary>
        static Button FindButton(string path)
        {
            foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (PathOf(b.transform).EndsWith("/" + path)) return b;
            return null;
        }

        static string PathOf(Transform t) => t.parent == null ? "/" + t.name : PathOf(t.parent) + "/" + t.name;

        public static void Select(string regionId) => Invoke<CrisisDeskView>("Select", regionId);
        public static void CloseModal() => Invoke<ModalView>("Close");
        public static void Render() => Invoke<CrisisDeskView>("Render");

        static void Invoke<T>(string method, params object[] args) where T : Object
        {
            var c = Object.FindFirstObjectByType<T>();
            typeof(T).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(c, args);
        }
    }
}
