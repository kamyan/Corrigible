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

        public static string Capture(string path = "Temp/ui_shot.png", int width = 1920, int height = 1080)
        {
            var cam = Camera.main;
            if (cam == null) return "kamera yok";
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var saved = new List<(Canvas, RenderMode, Camera, float)>();
            foreach (var c in canvases)
            {
                if (!c.isRootCanvas) continue;
                saved.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1 + c.sortingOrder * 0.0f;
            }
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            foreach (var c in canvases) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
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
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
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
