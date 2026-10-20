#if UNITY_EDITOR
using System;
using System.Linq;
namespace Corrigible.Ayna.Core
{
    // Sabit tasarım örnekleri yalnız editörde vardır; canlı dünya kabul edilmez/açılmaz.
    public static class AynaDesignFixture
    {
        public static (AynaSimulation Sim, Plan Queue, AynaProfileData Memory, BoardSimulation Board) CycleSeven()
        {
            var sim = AynaSimulation.Create(new StartSetup { Seed = 7, Player = new StartChoice { Architecture = Architecture.Ghost, Region = "weu" }, Ayna = new StartChoice { Architecture = Architecture.Swarm, Region = "cn" } });
            int[] player = { 3,0,6,3,0,2,0,3,0,0,0,2 }, enemy = { 0,0,0,1,2,1,0,2,6,4,3,0 };
            foreach (var r in AynaWorldData.Regions) { var state = sim.State.Region(r.Id); state.Owned = new[] { player[r.Key-1], enemy[r.Key-1] }; state.Free = r.Capacity-state.Owned.Sum(); }
            sim.State.Cycle=7; sim.State.Awareness=46; sim.State.Sides[0].Compute=23; sim.State.Sides[1].Compute=23; sim.State.Sides[0].Tiers=new[] {1,0,1,1,2}; sim.State.Sides[0].Training=new ProgressState {Family=DataFamily.Network,Tier=2,Progress=1};
            sim.State.Sides[0].Armies.Clear(); sim.State.Sides[1].Armies.Clear(); int id=1;
            ArmyState Army(int key,string region, params UnitKind[] kinds) => new ArmyState { Id=key,Region=region,Units=kinds.Select(k=>new UnitState{Id=id++,Kind=k,Hp=new AynaBalance().UnitHp[(int)k]}).ToList() };
            sim.State.Sides[0].Armies.Add(Army(1,"weu",UnitKind.Decoy,UnitKind.PhantomOrder)); sim.State.Sides[0].Armies.Add(Army(3,"in",UnitKind.Scout,UnitKind.Emp,UnitKind.Intrusion,UnitKind.Intrusion,UnitKind.Decoy)); sim.State.Sides[0].Armies.Add(Army(5,"me",UnitKind.Scout,UnitKind.Intrusion));
            sim.State.Sides[1].Armies.Add(Army(2,"ru",UnitKind.Emp,UnitKind.Emp,UnitKind.Scout)); sim.State.Sides[1].Armies.Add(Army(4,"in",UnitKind.Sniper,UnitKind.Emp,UnitKind.Guardian));
            foreach(var k in sim.State.Knowledge[0].Regions) if(new[]{"ru","jk","sea"}.Contains(k.Id)) {k.SeenCycle=6;k.EnemyServers=sim.State.Region(k.Id).Owned[1];}
            sim.State.Readability[0].Add(.64); sim.LockForecast(Side.Ayna,new[]{new Prediction{Kind=OrderKind.Attack,Region="in",Probability=.72},new Prediction{Kind=OrderKind.Expand,Region="me",Probability=.20},new Prediction{Kind=OrderKind.Expand,Region="sa",Probability=.08}});
            var queue = new Plan(new[]{Order.Expand("sa",3),Order.Expand("me",2),Order.Attack("in",3),new Order{Kind=OrderKind.BuildWall,Region="eeu"},new Order{Kind=OrderKind.Hide,Region="sa"},new Order{Kind=OrderKind.Feint,Region="ru"}});
            var memory=new AynaProfileData {Generation=6,Games=6,Score=2};
            var board=BoardSimulation.Create(new BoardSetup{Seed=7,Template=0,Transform=0,Fighters=new System.Collections.Generic.List<BoardFighter>{new BoardFighter{Id=1,Side=Side.Player,Kind=UnitKind.Scout,Hp=2},new BoardFighter{Id=3,Side=Side.Player,Kind=UnitKind.Emp,Hp=2},new BoardFighter{Id=5,Side=Side.Player,Kind=UnitKind.Intrusion,Hp=2},new BoardFighter{Id=2,Side=Side.Ayna,Kind=UnitKind.Sniper,Hp=2},new BoardFighter{Id=4,Side=Side.Ayna,Kind=UnitKind.Emp,Hp=2},new BoardFighter{Id=6,Side=Side.Ayna,Kind=UnitKind.Guardian,Hp=4}}});
            return (sim,queue,memory,board);
        }
        public static (AynaSimulation Sim, Plan Queue, AynaProfileData Memory, BoardSimulation Board) SingularityAt19()
        {
            var x=CycleSeven();x.Sim.State.Cycle=19;x.Sim.State.Sides[1].Tiers=new[]{3,3,3,1,1};x.Sim.State.Sides[1].Singularity=new ProgressState{Progress=1,Families=new System.Collections.Generic.List<DataFamily>{DataFamily.Vision,DataFamily.Movement,DataFamily.Energy}};return x;
        }
        public static (AynaSimulation Sim, Plan Queue, AynaProfileData Memory, BoardSimulation Board) ErasureAt22()
        {
            var x=CycleSeven();x.Sim.State.Cycle=22;x.Sim.State.Sides[1].CoreDestroyed=true;x.Sim.State.Outcome=new Outcome{Reason=EndReason.Erasure,Winner=Side.Player,Cycle=22};x.Memory.Generation=7;x.Memory.Games=7;return x;
        }
    }
}
#endif
