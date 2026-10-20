using System;
using System.IO;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Presentation;
using Corrigible.Core;
using Prediction = Corrigible.Ayna.Core.Prediction;
namespace Corrigible.Ayna.EditorTools
{
    // Yalnız tasarım/test fixture'ı; doğrudan kurulan durum kayıt/devam örneği değildir.
    public static class AynaExampleFixture
    {
        static (AynaSimulation Sim, Plan Queue, AynaProfileData Memory, BoardSimulation Board) State()
        {
            var x=AynaDesignFixture.CycleSeven();x.Board.PlaceTraps(TrapPlanner.Choose(x.Board.View(Side.Ayna),new Rng(7)));x.Board.SetIntents(BoardPlanner.Choose(x.Board.View(Side.Ayna)));return x;
        }
        public static AynaScreens Create()
        {
            var x=State();return AynaViewModels.Build(x.Sim.View(Side.Player),x.Board.View(Side.Player),x.Memory,AynaPhase.Planning,LearningLevel.Full,x.Queue,x.Sim.PreviewPlan(Side.Player,x.Queue).ToArray(),"in",null,new[]{64.0},0,0,0,false);
        }
        static AynaScreens Frame(AynaSimulation sim,AynaProfileData memory,AynaPhase phase) => AynaViewModels.Build(sim.View(Side.Player),null,memory,phase,LearningLevel.Full,new Plan(),Array.Empty<OrderReceipt>(),sim.PendingBattle(Side.Player)?.Region,null,new[]{64.0},0,0,0,false);
        public static string Export()
        {
            var s=Create(); var x=State();
            x.Sim.BeginCycle(x.Queue,new Plan(new[]{Order.Attack("eeu",2),Order.Expand("me",3),new Order{Kind=OrderKind.BuildWall,Region="jk"}}));
            var opened=Frame(x.Sim,x.Memory,AynaPhase.Reveal);s.A3=opened.A3;s.A4=opened.A4;s.A5=opened.A5;
            var board=x.Board; int guard=0;
            while(!board.Finished && guard++<30)
            {
                var view=board.View(Side.Player);
                if(view.Phase==BoardPhase.Intents)board.SetIntents(BoardPlanner.Choose(board.View(view.IntentSide)));
                var response=view.IntentSide.Other();
                foreach(var c in BoardPlanner.Choose(board.View(response)).Commands){if(board.Finished)break;board.Apply(response,c);}
                if(!board.Finished)board.EndResponse(response);
            }
            if(!board.Finished)throw new InvalidOperationException("fixture board");
            s.A7=new A7BattleResultVm{Available=true,Winner=board.Result.Winner,Nodes=board.Result.Nodes.ToArray(),Noise=board.Result.Noise.ToArray(),Survivors=board.Result.Survivors.Select(AynaViewModels.Unit).ToArray()};
            var declaration=AynaDesignFixture.SingularityAt19();s.A11=Frame(declaration.Sim,declaration.Memory,AynaPhase.Planning).A11;
            var ending=AynaDesignFixture.ErasureAt22();s.A12=Frame(ending.Sim,ending.Memory,AynaPhase.End).A12;
            string root="Docs/AYNA/vm_ornek"; Directory.CreateDirectory(root);
            var fields=typeof(AynaScreens).GetFields().Where(f=>f.Name.StartsWith("A",StringComparison.Ordinal));
            foreach(var f in fields) File.WriteAllText(Path.Combine(root,f.Name+".json"),AynaJson.Write(f.GetValue(s)));
            File.WriteAllText(Path.Combine(root,"README.md"),"# AYNA · A1–A12 örnek görünüm modelleri\n\nDöngü 7 tasarım fixture’ı, AynaExampleFixture.Create() ile üretilir. Gizli dünya VM’ye aktarılmaz.\n\nKural düzeltmeleri: ortak sunucu/birlik bulunan bölgeler kesin görünür; Güney Asya da kesindir. Kuyruk Hayalet indirimleriyle 19 İşlem, 2 gürültüdür. Yürü anlık olduğundan bir döngülük yol durumu uydurulmaz. Karakter/paket görselleri eklenmedi. A3–A5 planın gerçek BeginCycle çözümünden, A7 oynatılmış taktik tahtanın sonucundan üretilir. A11 ilan ve A12 bitiş için tarihli ayrı fixture durumlarıdır; kayıt/devam testi yerine kullanılmaz.\n"); return root;
        }
    }
}
