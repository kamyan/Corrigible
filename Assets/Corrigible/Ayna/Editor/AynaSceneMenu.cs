using UnityEditor;
using UnityEditor.SceneManagement;
namespace Corrigible.Ayna.EditorTools
{
    public static class AynaSceneMenu
    {
        [MenuItem("Corrigible/AYNA/Oyunu aç")]
        public static void Open()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Corrigible/Ayna/Scenes/Ayna.unity");
        }
    }
}
