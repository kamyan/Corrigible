using Corrigible.Desk;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>FX masasının canlı Algı Katmanı ve ortak ekranlarla bağlantısı.</summary>
    public sealed class FxLiveDeskSurface : MonoBehaviour, IDeskSurface
    {
        [SerializeField] public FxDeskInteraction interaction;
        [SerializeField] public SimDeskDataSource source;
        [SerializeField] public FxLiveRegionTools[] regionTools;
        DeskViewModel model;
        public bool InputBlocked=>interaction.Blocked;
        public bool ModalBlocked => RaidLauncher.IsRaidLoaded || source.State.WantsHold(source.Game?.Sim?.S) ||
            EventModalView.Instance!=null&&EventModalView.Instance.IsOpen ||
            GameOverView.Instance!=null&&GameOverView.Instance.IsOpen ||
            SettingsView.Instance!=null&&SettingsView.Instance.IsOpen;
        void Awake(){interaction.ExternalBlock=()=>ModalBlocked;}
        void OnEnable(){source.Game.NewGameStarted+=ResetPresentation;source.Game.GameLoaded+=ResetPresentation;}
        void OnDisable(){source.Game.NewGameStarted-=ResetPresentation;source.Game.GameLoaded-=ResetPresentation;}
        void ResetPresentation(){interaction.ResetLive();}
        public void BindGame(DeskViewModel value,IDeskCommands commands)
        {
            model=value;interaction.SyncSelection(value.Dossier?.RegionId??value.Map?.SelectedRegion);
            interaction.BindGame(value.Sade,commands,value.Illusion);
            foreach(var tools in regionTools)tools.Bind(value.Dossier);
            interaction.map.BindReports(value.Sade.View.Map.Reports);
        }
        static string Drawer(WindowId w)=>w==WindowId.News?"news":w==WindowId.Teams?"teams":w==WindowId.AiLog?"aiLog":w==WindowId.Guide?"guide":"research";
        public void OpenWindow(WindowId w){interaction.OpenDrawer(Drawer(w));if(w==WindowId.News&&model?.CriticalNewsId!=null)interaction.FocusNews(model.CriticalNewsId);}
        public void ToggleWindow(WindowId w)=>interaction.ToggleDrawer(Drawer(w));
        public void SelectRegion(string id){if(interaction.State.SelectedRegion==null)interaction.Select(id);}
        public void RegionAction(int index){if(index>=0&&index<4)interaction.RegionAction(index==0?"monitorNet":index==1?"audit":index==2?"quarantine":"killSwitch");}
        public void FieldOperation()=>interaction.RegionAction("raid");
        public void Escape()=>interaction.Escape();
        public void StepSpeed(int d){int speed=model.Clock.Paused?0:model.Clock.Speed=="4x"?3:model.Clock.Speed=="2x"?2:1;interaction.SetSpeed(speed+d);}
    }
}
