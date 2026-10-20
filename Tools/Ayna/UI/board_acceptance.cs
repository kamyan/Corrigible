var game=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaGameController>();
var ui=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaInterfaceView>();
var b=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaBoardView>();
string save=game.ExportSave();var before=Corrigible.Ayna.Core.AynaJson.Write(game.Match.Simulation.Board(Corrigible.Ayna.Core.Side.Player));
var cmd=Corrigible.Ayna.Mind.BoardPlanner.Choose(game.Match.Simulation.Board(Corrigible.Ayna.Core.Side.Player)).Commands.FirstOrDefault();
var preview=cmd==null?null:game.Preview(cmd);bool pure=save==game.ExportSave();
if(cmd!=null){b.SelectUnit(cmd.UnitId);b.SelectAction((int)cmd.Action);b.ClickCell(cmd.To);}
bool applied=save!=game.ExportSave();b.Undo();bool undone=before==Corrigible.Ayna.Core.AynaJson.Write(game.Match.Simulation.Board(Corrigible.Ayna.Core.Side.Player));
game.SaveGame();string mid=game.ExportSave();game.LoadGame();bool loaded=mid==game.ExportSave();
Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A6_TR.png");
Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A6_2580x1080.png",2580,1080);
Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A6_1920x1200.png",1920,1200);
var table=Corrigible.Core.Loc.ParseTable(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Localization/strings").text);
Corrigible.Core.Loc.SetLanguage("en",table["en"]);ui.Render();Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A6_EN.png");Corrigible.Core.Loc.SetLanguage("tr");ui.Render();
for(int i=0;i<24&&game.Phase==Corrigible.Ayna.Presentation.AynaPhase.Board;i++){var p=Corrigible.Ayna.Mind.BoardPlanner.Choose(game.Match.Simulation.Board(Corrigible.Ayna.Core.Side.Player));foreach(var c in p.Commands){if(game.Phase!=Corrigible.Ayna.Presentation.AynaPhase.Board)break;game.BoardCommand(c);}if(game.Phase==Corrigible.Ayna.Presentation.AynaPhase.Board)game.EndBoardTurn();}
Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A7_TR.png");
var proof=new {PreviewAvailable=preview!=null,PreviewPreservedSave=pure,Applied=applied,UndoRestoredBoard=undone,MidBattleLoadExact=loaded,BattleFinished=game.Phase==Corrigible.Ayna.Presentation.AynaPhase.BoardResult};
System.IO.File.WriteAllText("Docs/AYNA/UI/board_acceptance.json",Newtonsoft.Json.JsonConvert.SerializeObject(proof,Newtonsoft.Json.Formatting.Indented));return proof;
