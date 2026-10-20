var scenes = System.Linq.Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i)).Select(s => new { s.path, s.isDirty, s.isLoaded }).ToArray();
return new
{
    Playing = UnityEditor.EditorApplication.isPlaying,
    Compiling = UnityEditor.EditorApplication.isCompiling,
    Scenes = scenes,
    CoreReferences = typeof(Corrigible.Ayna.Core.AynaSimulation).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray(),
    MindReferences = typeof(Corrigible.Ayna.Mind.MindSession).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray()
};
