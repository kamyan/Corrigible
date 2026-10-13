using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Sahte komutlar (PROMPT §4.2): her niyeti günlüğe yazar ve sahte durumu en basit biçimde günceller.</summary>
    public sealed class MockDeskCommands : MonoBehaviour, IDeskCommands
    {
        [SerializeField] MockDeskDataSource source;
        [SerializeField] bool log = true;

        void Log(string s) { if (log) Debug.Log("[Desk komut] " + s); }

        public void SelectRegion(string regionId) { Log("SelectRegion " + regionId); source.SelectRegion(regionId); }
        public void ToggleWindow(WindowId id) => Log("ToggleWindow " + id);
        public void ExecuteRegionAction(string regionId, int index) => Log($"ExecuteRegionAction {regionId} {index + 1}");
        public void LaunchFieldOperation(string regionId) => Log("LaunchFieldOperation " + regionId);
        public void StartResearch(string projectId) { Log("StartResearch " + projectId); source.SetResearchState(projectId, true); }
        public void PauseResearch(string projectId) { Log("PauseResearch " + projectId); source.SetResearchState(projectId, false); }

        public void SetEmergencyFund(int option)
        {
            Log("SetEmergencyFund " + option);
            var f = source.Current.Research?.EmergencyFund;
            if (f != null) { f.Selected = option; source.Notify(); }
        }

        public void TogglePause() { Log("TogglePause"); source.TogglePause(); }
        public void SetSpeed(GameSpeed speed) { Log("SetSpeed " + speed); source.SetSpeed(speed); }
        public void DismissAdvisor() { Log("DismissAdvisor"); source.DismissAdvisor(); }

        public void RunAdvisorAction(string command)
        {
            Log("RunAdvisorAction " + command);
            if (command == null) return;
            if (command.StartsWith("FocusRegion:")) source.SelectRegion(command.Substring("FocusRegion:".Length));
            else if (command.StartsWith("SetSpeed:"))
            {
                var sp = command.Substring("SetSpeed:".Length);
                source.SetSpeed(sp == "4x" ? GameSpeed.X4 : sp == "2x" ? GameSpeed.X2 : sp == "pause" ? GameSpeed.Pause : GameSpeed.X1);
            }
            else if (command == "DismissAdvisor") source.DismissAdvisor();
        }

        public void ConfirmGlobalKillSwitch() => Log("ConfirmGlobalKillSwitch");
        public void SignEventOption(int index) { Log("SignEventOption " + index); source.CloseEvent(); }
        public void ReturnToMainMenu() => Log("ReturnToMainMenu");
        public void ReviewLogs() => Log("ReviewLogs");
        public void RetrySameSeed() => Log("RetrySameSeed");
        /// <summary>Sahte: yeniden başla = C1 ön ayarı.</summary>
        public void Restart() { Log("Restart"); source.Load(0); }

        /// <summary>F8.3 ekranlarının niyetleri (sahte: yalnızca günlüğe).</summary>
        public void Run(string command) => Log("Run " + command);

        /// <summary>+/− tuşları için (oyunda SetSpeed ile aynı niyet).</summary>
        public void StepSpeed(int dir) { Log("StepSpeed " + dir); source.StepSpeed(dir); }
    }
}
