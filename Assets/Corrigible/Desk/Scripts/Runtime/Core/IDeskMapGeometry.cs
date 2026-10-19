using UnityEngine;
namespace Corrigible.Desk
{
    /// <summary>Ortak olay katmanının alternatif haritalardaki aynı noktayı izlemesi.</summary>
    public interface IDeskMapGeometry
    {
        float WorldScale {get;}
        Vector3 MapToWorld(float x,float y);
        Vector3 RegionWorld(string id);
    }
}
