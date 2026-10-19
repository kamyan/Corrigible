using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Corrigible.Fx.Orb;
using UnityEngine;
using UnityEditor;
namespace Corrigible.Fx.EditorTools
{
    public static class OrbCapture
    {
        public const string SettingsPath="Assets/Corrigible/Fx/Settings/OrbRenderSettings.asset";
        [MenuItem("Tools/FX UI/Setup Orb Assets")]
        public static void Setup()
        {
            var settings=AssetDatabase.LoadAssetAtPath<OrbRenderSettings>(SettingsPath);
            if(settings==null){settings=ScriptableObject.CreateInstance<OrbRenderSettings>();AssetDatabase.CreateAsset(settings,SettingsPath);}
            settings.draw=Material("OrbDraw","Hidden/FxOrbDraw");settings.composite=Material("OrbComposite","UI/FxOrbComposite");
            settings.glowProfile=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Corrigible/Fx/Art/Orb/orb_glow_profile_128.png");
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
        }
        static Material Material(string name,string shader)
        {
            string path="Assets/Corrigible/Fx/Settings/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){var sh=Shader.Find(shader);if(sh==null)throw new Exception(shader);m=new Material(sh);AssetDatabase.CreateAsset(m,path);}return m;
        }
        public static OrbKind Kind(string text)=>text=="unknown"?OrbKind.Unknown:text=="quar"?OrbKind.Quarantine:text=="core"?OrbKind.Core:OrbKind.Normal;
        [MenuItem("Tools/FX UI/Orb Golden Capture")]
        public static string CaptureAll()
        {
            Setup();string dir="Docs/UI_Fx/captures/orb";Directory.CreateDirectory(dir);
            var manifest=JObject.Parse(File.ReadAllText("Docs/UI_Fx/SonSalter_Fx_Paketi/reference/orb/golden/golden.json"));int count=0;
            var go=new GameObject("Orb Golden Capture"){hideFlags=HideFlags.HideAndDontSave};var renderer=go.AddComponent<OrbRenderer>();renderer.settings=AssetDatabase.LoadAssetAtPath<OrbRenderSettings>(SettingsPath);
            try{foreach(JObject file in manifest["files"]){var sim=new OrbSimulation(48,(int)file["seed"],Kind((string)file["kind"]),(double)file["level"],211);sim.Advance(.025,OrbMath.Round((double)file["t"]/.025));renderer.Render(sim,422);Save(renderer.Target,dir+"/"+(string)file["file"]);count++;}}
            finally{UnityEngine.Object.DestroyImmediate(go);}return count+" altın küre karesi kaydedildi.";
        }
        public static void Save(RenderTexture rt,string path)
        {
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true);
            texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();RenderTexture.active=previous;
            var data=texture.GetPixels32();for(int i=0;i<data.Length;i++){var c=data[i];double a=1-c.a/255.0;data[i]=new Color32((byte)Math.Min(255,Math.Floor(c.r+2*a+.5)),(byte)Math.Min(255,Math.Floor(c.g+5*a+.5)),(byte)Math.Min(255,Math.Floor(c.b+10*a+.5)),255);}
            texture.SetPixels32(data);texture.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
