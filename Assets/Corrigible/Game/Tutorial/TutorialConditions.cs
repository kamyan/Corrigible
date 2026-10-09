using System.Linq;
using Corrigible.Core;
using Corrigible.Game.UI;

namespace Corrigible.Game.Tutorial
{
    /// <summary>Eğitim koşullarını oyun durumundan değerlendirir. Simülasyonu yalnızca okur; oyuncunun gördüğü değerleri kullanır.</summary>
    public static class TutorialConditions
    {
        public static bool Check(TutorialCondition c, GameController game, CrisisDeskView desk)
        {
            var sim = game.Sim;
            if (sim == null) return false;
            var s = sim.S;
            switch (c)
            {
                case TutorialCondition.None: return true;
                case TutorialCondition.NextButton: return false;
                case TutorialCondition.RegionSelected: return desk.SelectedRegion != null;
                case TutorialCondition.MonitorBuilt: return s.RunActions.Izleme > 0;
                case TutorialCondition.TimeRunning: return game.Speed > 0;
                case TutorialCondition.AiDetected: return s.Aware;
                case TutorialCondition.InfiltrationVisible: return s.Regions.Any(r => (sim.Shown(r).V ?? 0) >= 10);
                case TutorialCondition.AlignmentStarted:
                    return GameData.Research.Any(r => r.Branch == GameData.BranchAlign && (s.Research.Cur == r.Id || s.Done(r.Id)));
                case TutorialCondition.AuditUsed: return s.RunActions.Denetim > 0;
                case TutorialCondition.CoreKnown: return s.CoreKnown != null && s.Ending == null;
                case TutorialCondition.GiftOffered: return s.PendingGift != null;
                case TutorialCondition.VeilTorn: return s.VeilsTorn > 0;
                case TutorialCondition.DependencyAbove40: return sim.Global().Dep >= 40 && s.Ending == null;
                case TutorialCondition.TrustBelow30: return sim.Global().Trust < 30 && s.Ending == null;
                case TutorialCondition.ResearchStarved: return s.Research.Starved >= 3 && s.ResearchReserve <= 0 && s.Fon < 1 && s.Ending == null;
                case TutorialCondition.TeamDispatched: return !sim.TeamsOn || s.Teams.Any(t => t.Dest != null || t.At != Simulation.HQ);
                case TutorialCondition.RaidAvailable: return sim.TeamsOn && s.Regions.Any(r => sim.RaidCheck(r.Id) == null);
                case TutorialCondition.ObservationPoint: return sim.Studied > 0;
                case TutorialCondition.ResearchCompleted: return s.Research.Done.Count > 0 && s.Research.Cur == null && s.Ending == null;
                default: return false;
            }
        }
    }
}
