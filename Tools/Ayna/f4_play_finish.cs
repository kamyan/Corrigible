var game=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaGameController>();
for(int i=0;i<60 && game.Phase!=Corrigible.Ayna.Presentation.AynaPhase.End;i++)
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
return new { game.Phase, Cycle=game.Match.Simulation.View(Corrigible.Ayna.Core.Side.Player).Cycle, Outcome=game.Match.Simulation.Outcome, End=game.Screens.A12 };
