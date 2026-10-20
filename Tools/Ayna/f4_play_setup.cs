var game=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaGameController>();
if(game==null||!UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("play scene");
game.Initialize("Temp/AynaF4Play");
return new { Phase=game.Phase, SaveExists=game.HasSave };
