using System;
using System.IO;
using UnityEngine;
namespace Corrigible.Fx.EditorTools
{
    public static class FxRaidCapture
    {
        public static string Capture(string path,int width=1920,int height=1080)
        {
            var board=UnityEngine.Object.FindFirstObjectByType<FxRaidBoard>();if(board==null)throw new InvalidOperationException("FX Baskın tahtası açık olmalı.");var camera=board.boardCamera;var previous=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);Texture2D texture=null;
            try{camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();board.RefreshProjection(true);board.Tick(board.clock);var hud=UnityEngine.Object.FindFirstObjectByType<FxRaidHud>();if(hud!=null){hud.RefreshLayout(true);hud.Tick(board.clock);if(board.clock.Static)hud.CompleteAnimations();}Canvas.ForceUpdateCanvases();foreach(var batch in UnityEngine.Object.FindObjectsByType<FxGraphicBatch>(FindObjectsSortMode.None))batch.Refresh();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());return Path.GetFullPath(path);}
            finally{RenderTexture.active=active;camera.targetTexture=previous;if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);Canvas.ForceUpdateCanvases();board.RefreshProjection(true);}
        }
    }
}
