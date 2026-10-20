var game=UnityEngine.Object.FindFirstObjectByType<Corrigible.Ayna.Game.AynaGameController>();
for(int i=0;i<30 && game.Phase!=Corrigible.Ayna.Presentation.AynaPhase.End;i++)
{
    var phase=game.Phase;
    if(phase==Corrigible.Ayna.Presentation.AynaPhase.Planning)
    {
        var plan=Corrigible.Ayna.Mind.Bots.PlanFactory.Build(game.Match.Simulation.View(Corrigible.Ayna.Core.Side.Player),Corrigible.Ayna.Mind.Bots.BotStyle.Balanced,0);
        foreach(var order in plan.Orders)game.Queue(order);game.LockPlan();
    }
    else if(phase==Corrigible.Ayna.Presentation.AynaPhase.Conflict)break;
    else if(phase==Corrigible.Ayna.Presentation.AynaPhase.Board ||phase==Corrigible.Ayna.Presentation.AynaPhase.Roster)break;
    else game.Continue();
}
return new { game.Phase, Cycle=game.Match.Simulation.View(Corrigible.Ayna.Core.Side.Player).Cycle, Outcome=game.Match.Simulation.Outcome };
