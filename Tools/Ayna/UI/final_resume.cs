var game=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaGameController>();
game.Initialize("Temp/AynaInterface/Acceptance");game.LoadGame();Corrigible.Ayna.Game.AynaInterfaceMotion.Reduced=true;
string profileBefore=System.IO.File.ReadAllText("Temp/AynaInterface/Acceptance/ayna_profile.json");
for(int i=0;i<300 && game.Phase!=Corrigible.Ayna.Presentation.AynaPhase.End;i++)
{
    var phase=game.Phase;
    if(phase==Corrigible.Ayna.Presentation.AynaPhase.Planning)
    {
        var plan=Corrigible.Ayna.Mind.Bots.PlanFactory.Build(game.Match.Simulation.View(Corrigible.Ayna.Core.Side.Player),Corrigible.Ayna.Mind.Bots.BotStyle.Balanced,0);
        foreach(var order in plan.Orders)game.Queue(order);game.LockPlan();
    }
    else if(phase==Corrigible.Ayna.Presentation.AynaPhase.Conflict)game.AutoBattle();
    else if(phase==Corrigible.Ayna.Presentation.AynaPhase.Board)
    {
        var board=game.Match.Simulation.Board(Corrigible.Ayna.Core.Side.Player);var plan=Corrigible.Ayna.Mind.BoardPlanner.Choose(board);
        foreach(var command in plan.Commands){if(game.Phase!=Corrigible.Ayna.Presentation.AynaPhase.Board)break;game.BoardCommand(command);}
        if(game.Phase==Corrigible.Ayna.Presentation.AynaPhase.Board)game.EndBoardTurn();
    }
    else game.Continue();
}
var ui=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaInterfaceView>();var panel=(UnityEngine.GameObject)typeof(Corrigible.Ayna.Game.AynaInterfaceView).GetField("singularityPanel",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(ui);
System.IO.File.WriteAllText("Docs/AYNA/UI/end_acceptance.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {Finished=game.Screens.A12.Finished,DeclarationHidden=!panel.activeSelf,ProfileNotCommittedTwice=profileBefore==System.IO.File.ReadAllText("Temp/AynaInterface/Acceptance/ayna_profile.json"),game.Screens.A12.Generation},Newtonsoft.Json.Formatting.Indented));
System.IO.File.WriteAllText("Docs/AYNA/UI/live_end.json",Corrigible.Ayna.Core.AynaJson.Write(game.Screens.A12));Corrigible.EditorTools.CorrigibleDebug.Capture("Docs/AYNA/UI/A12_TR.png");return new { game.Phase, Cycle=game.Match.Simulation.View(Corrigible.Ayna.Core.Side.Player).Cycle, Outcome=game.Match.Simulation.Outcome, End=game.Screens.A12 };
