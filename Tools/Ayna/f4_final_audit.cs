Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
int missing=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Transform>(true)).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
return new {Scene=scene.path,scene.isDirty,Playing=UnityEditor.EditorApplication.isPlaying,MissingScripts=missing,Learning=Corrigible.Ayna.EditorTools.AynaLearningBench.Status()};
