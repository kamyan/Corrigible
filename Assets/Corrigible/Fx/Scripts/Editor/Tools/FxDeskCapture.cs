using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx.EditorTools
{
    /// <summary>FX masası: geçici piksel boyutunda cover, HUD ve ortak mesh'leri birlikte yeniler.</summary>
    public static class FxDeskCapture
    {
        public static string Capture(string path,int width=1920,int height=1080)
        {
            var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Masa kamerası gerekli.");
            var canvas=UnityEngine.Object.FindFirstObjectByType<FxDeskInteraction>().GetComponent<Canvas>();
            var previous=camera.targetTexture;var previousDistance=canvas.planeDistance;
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);Texture2D texture=null;
            var active=RenderTexture.active;
            var overlays=new System.Collections.Generic.List<(Canvas canvas,Camera camera,float distance)>();
            foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay){overlays.Add((c,c.worldCamera,c.planeDistance));c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=100;}
            try
            {
                camera.targetTexture=rt;canvas.planeDistance=100;
                Canvas.ForceUpdateCanvases();camera.Render();Refresh();camera.Render();
                RenderTexture.active=rt;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());return Path.GetFullPath(path);
            }
            finally
            {
                RenderTexture.active=active;camera.targetTexture=previous;canvas.planeDistance=previousDistance;
                foreach(var item in overlays){item.canvas.renderMode=RenderMode.ScreenSpaceOverlay;item.canvas.worldCamera=item.camera;item.canvas.planeDistance=item.distance;}
                if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                Canvas.ForceUpdateCanvases();Refresh();
            }
        }
        static void Refresh()
        {
            foreach(var cover in UnityEngine.Object.FindObjectsByType<Corrigible.Desk.MapCoverFitter>(FindObjectsSortMode.None))cover.Refresh();
            foreach(var hud in UnityEngine.Object.FindObjectsByType<FxDeskHud>(FindObjectsSortMode.None))hud.RefreshLayout();
            foreach(var panels in UnityEngine.Object.FindObjectsByType<FxDeskPanels>(FindObjectsSortMode.None))panels.CompleteAnimations();
            foreach(var orb in UnityEngine.Object.FindObjectsByType<OrbView>(FindObjectsSortMode.None))orb.RenderFixed(.025,100);
            Canvas.ForceUpdateCanvases();
            foreach(var batch in UnityEngine.Object.FindObjectsByType<FxGraphicBatch>(FindObjectsSortMode.None))batch.Refresh();
            Canvas.ForceUpdateCanvases();
        }
    }
}
