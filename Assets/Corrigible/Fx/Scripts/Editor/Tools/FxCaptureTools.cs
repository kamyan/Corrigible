using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Fx.EditorTools
{
    public static class FxCaptureTools
    {
        [MenuItem("Tools/FX UI/Capture Game View")]
        static void Menu(){var clock=UnityEngine.Object.FindFirstObjectByType<FxClock>();if(clock==null)throw new InvalidOperationException("Bir FX sahnesi açık olmalı.");var lab=UnityEngine.Object.FindFirstObjectByType<FxOrbLab>();if(lab!=null){clock.Resume();lab.SetManual(26);clock.Advance(clock.Now+.3);lab.Tick(clock);}clock.Freeze();clock.Advance(clock.Now);foreach(var orb in UnityEngine.Object.FindObjectsByType<OrbView>(FindObjectsSortMode.None))orb.RenderFixed(.025,lab!=null?160:100);var debug=UnityEngine.Object.FindFirstObjectByType<FxDebugMenu>();if(debug!=null)debug.RefreshReference();string name=debug!=null?debug.reference.ScreenName:"FxUI_Gallery";Debug.Log(Capture("Docs/UI_Fx/captures/"+name+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".png"));}
        public static string Capture(string path,int width=1920,int height=1080)
        {
            if(UnityEngine.Object.FindFirstObjectByType<FxRaidBoard>()!=null)return FxRaidCapture.Capture(path,width,height);
            if(UnityEngine.Object.FindFirstObjectByType<FxDeskInteraction>()!=null)return FxDeskCapture.Capture(path,width,height);
            var camera=Camera.main;if(camera==null)throw new InvalidOperationException("FX kamerası gerekli.");var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);Texture2D texture=null;
            try{camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();var debug=UnityEngine.Object.FindFirstObjectByType<FxDebugMenu>();if(debug!=null)debug.reference.RefreshLayout();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());return Path.GetFullPath(path);}
            finally{RenderTexture.active=active;camera.targetTexture=previous;if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);Canvas.ForceUpdateCanvases();}
        }
    }
}
