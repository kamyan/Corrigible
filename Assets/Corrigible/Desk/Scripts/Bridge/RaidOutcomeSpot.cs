using Corrigible.Game.Raid;
using UnityEngine;
namespace Corrigible.Desk.Bridge
{
    public sealed class RaidOutcomeSpot : MonoBehaviour
    {
        [SerializeField] RaidHudView hud;
        [SerializeField] Camera boardCamera;
        [SerializeField] Canvas canvas;
        [SerializeField] SpotlightGraphic spot;
        public void Refresh()
        {
            if(hud.Current?.Finished!=true)return;var p=hud.Current.Spot;var screen=boardCamera.WorldToScreenPoint(RaidBoard3D.W(p.x,p.y)+Vector3.up*p.z);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(spot.rectTransform,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);
            var ring=spot.color;ring.a=.36f;spot.SetSpot(local,150,14,ring);
        }
        void LateUpdate()=>Refresh();
    }
}
