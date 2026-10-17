using System;
using Corrigible.Game.Raid;
using UnityEngine;
namespace Corrigible.Desk.Bridge
{
    /// <summary>VM çarpışma karelerini kamera üzerinden ekran işaretlerine taşır; yıldızlar sahnede hazırlanır.</summary>
    public sealed class RaidPreviewOverlay : MonoBehaviour
    {
        [SerializeField] Camera boardCamera;
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform[] stars;
        Vector2Int[] tiles=Array.Empty<Vector2Int>();
        public void Render(Vector2Int[] value){tiles=value;Refresh();}
        public void Refresh()
        {
            for(int i=0;i<stars.Length;i++)
            {
                if(stars[i].gameObject.activeSelf!=(i<tiles.Length))stars[i].gameObject.SetActive(i<tiles.Length);if(i>=tiles.Length)continue;
                Vector3 point=boardCamera.WorldToScreenPoint(RaidBoard3D.W(tiles[i].x,tiles[i].y)+Vector3.up*.25f);
                Camera ui=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)stars[i].parent,point,ui,out var local);if(stars[i].anchoredPosition!=local)stars[i].anchoredPosition=local;
            }
        }
        void LateUpdate()=>Refresh();
    }
}
