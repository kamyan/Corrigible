using System.IO;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Corrigible.EditorTools
{
    /// <summary>
    /// Play modunda test yardımcıları: oyunu hızlıca ilerletir ve arayüzün ekran görüntüsünü alır
    /// (Editor arka plandayken Game görünümü güncellenmediği için panel bir RenderTexture'a çizdirilir).
    /// </summary>
    public static class CorrigibleDebug
    {
        static GameController Game => Object.FindFirstObjectByType<GameController>();
        static CorrigibleUI UI => Object.FindFirstObjectByType<CorrigibleUI>();

        public static string BeginCapture(int width = 1920, int height = 1080)
        {
            Application.runInBackground = true;
            var doc = Object.FindFirstObjectByType<UIDocument>();
            doc.panelSettings.targetTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Pump();
            return "ok";
        }

        public static void Pump()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public static string EndCapture(string path = "Temp/ui_shot.png")
        {
            var doc = Object.FindFirstObjectByType<UIDocument>();
            var rt = doc.panelSettings.targetTexture;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            doc.panelSettings.targetTexture = null;
            Object.Destroy(tex);
            rt.Release();
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
            var s = g.Sim.S;
            return $"gün {s.Day}, son {s.Ending ?? "-"}, fark edildi {s.Aware}, çekirdek {s.Core}/{s.CoreKnown ?? "?"}, fon {s.Fon:0}, sg {s.Sg:0.0}, tekillik {s.Tek:0.0}";
        }

        public static void Select(string regionId) => Invoke("Select", regionId);
        public static void CloseModal() => Invoke("CloseModal");
        public static void Render() => Invoke("Render");

        static void Invoke(string method, params object[] args)
        {
            var ui = UI;
            ui.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(ui, args);
        }
    }
}
