using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Corrigible.Fx.EditorTools
{
    /// <summary>Laboratuvarın Editor yakalama düğmesini çalışma zamanı assembly bağımlılığı olmadan bağlar.</summary>
    [InitializeOnLoad]
    public static class OrbLabEditorHooks
    {
        static OrbLabEditorHooks(){EditorApplication.playModeStateChanged+=Changed;SceneManager.sceneLoaded+=Loaded;EditorApplication.delayCall+=Install;}
        static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode)Install();}
        static void Loaded(Scene scene,LoadSceneMode mode)=>Install();
        static void Install(){if(!Application.isPlaying)return;foreach(var lab in Object.FindObjectsByType<FxOrbLab>(FindObjectsSortMode.None)){lab.GoldenRequested-=Capture;lab.GoldenRequested+=Capture;}}
        static void Capture()=>Debug.Log(OrbCapture.CaptureAll());
    }
}
