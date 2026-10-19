using UnityEngine;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Sabit piksel geometriyi ekran izdüşümünü koruyarak zemin düzlemine yerleştirir; parçalar işaretlerin önünde kalır.</summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class FxRaidGroundProjection : BaseMeshEffect
    {
        [SerializeField] public FxRaidBoard board;
        [SerializeField] public float height=.075f;
        public override void ModifyMesh(VertexHelper vertices)
        {
            if(!IsActive()||board==null)return;var cam=board.boardCamera;var rect=graphic.rectTransform;var plane=new Plane(Vector3.up,new Vector3(0,height,0));var vertex=UIVertex.simpleVert;
            for(int i=0;i<vertices.currentVertCount;i++)
            {vertices.PopulateUIVertex(ref vertex,i);var screen=cam.WorldToScreenPoint(rect.TransformPoint(vertex.position));var ray=cam.ScreenPointToRay(screen);if(plane.Raycast(ray,out float distance))vertex.position=rect.InverseTransformPoint(ray.GetPoint(distance));vertices.SetUIVertex(vertex,i);}
        }
    }
}
