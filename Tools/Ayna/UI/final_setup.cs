var g=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaGameController>();
g.Initialize("Temp/AynaInterface/Acceptance");
var ui=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaInterfaceView>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
((UnityEngine.UI.Toggle)typeof(Corrigible.Ayna.Game.AynaInterfaceView).GetField("reduceMotion",flags).GetValue(ui)).isOn=true;
Corrigible.Ayna.Game.AynaInterfaceMotion.Reduced=true;
foreach(var cg in UnityEngine.Object.FindObjectsByType<UnityEngine.CanvasGroup>(UnityEngine.FindObjectsInactive.Include,UnityEngine.FindObjectsSortMode.None))cg.alpha=1;
return Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A1_TR.png");
