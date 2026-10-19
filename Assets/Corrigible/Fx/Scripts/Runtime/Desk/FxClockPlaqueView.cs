using System;
using Corrigible.Desk;
using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    public sealed class FxClockPlaqueView : FxTimedBehaviour
    {
        [SerializeField] public TMP_Text day,hour,colon,minute;
        [SerializeField] public FxButton[] speeds;
        public event Action<int> SpeedClicked;
        ClockVM model;
        protected override void OnEnable(){base.OnEnable();for(int i=0;i<speeds.Length;i++){int index=i;speeds[i].button.onClick.AddListener(()=>SpeedClicked?.Invoke(index));}}
        protected override void OnDisable(){base.OnDisable();foreach(var b in speeds)b.button.onClick.RemoveAllListeners();}
        public void Bind(ClockVM vm){model=vm;day.text=vm.DayText;var pieces=(vm.Time??"00:00").Split(':');hour.text=pieces[0];minute.text=pieces.Length>1?pieces[1]:"00";int selected=vm.Paused?0:vm.Speed=="4x"?3:vm.Speed=="2x"?2:1;for(int i=0;i<speeds.Length;i++){speeds[i].kind=i==selected?FxButtonKind.Primary:FxButtonKind.Normal;speeds[i].solidPrimary=true;speeds[i].Refresh();speeds[i].label.font=speeds[i].theme.Font("IBMPlexMono-SemiBold");speeds[i].label.fontSize=12;speeds[i].label.characterSpacing=0;}}
        public override void Tick(FxClock source){colon.canvasRenderer.SetAlpha(source.Phase(1,0,FxPhase.Blink)<.5f?1:0);}
    }
}
