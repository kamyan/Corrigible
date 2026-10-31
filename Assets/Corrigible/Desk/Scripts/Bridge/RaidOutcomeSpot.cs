using Corrigible.Game.Raid;
using UnityEngine;
namespace Corrigible.Desk.Bridge
{
    /// <summary>Sonuç kartında spot nedenin üstünde açılır (Revizyon 2 · S7: kararmayla birlikte, 0,1–0,4 s).</summary>
    public sealed class RaidOutcomeSpot : MonoBehaviour
    {
        [SerializeField] RaidHudView hud;
        [SerializeField] Camera boardCamera;
        [SerializeField] Canvas canvas;
        [SerializeField] SpotlightGraphic spot;
        float finishedAt=-1;
        public void Refresh()
        {
            if(hud.Current?.Finished!=true){finishedAt=-1;return;}if(finishedAt<0)finishedAt=Time.unscaledTime;
            float open=Application.isPlaying?Mathf.Clamp01((Time.unscaledTime-finishedAt-.1f)/.3f):1;open=1-(1-open)*(1-open);var p=hud.Current.Spot;var screen=boardCamera.WorldToScreenPoint(RaidBoard3D.W(p.x,p.y)+Vector3.up*p.z);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(spot.rectTransform,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);
            var ring=spot.color;ring.a=.36f*open;spot.SetSpot(local,150*open,14,ring,open>0);
        }
        void LateUpdate()=>Refresh();
    }
}
