using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>Yazıyı ve rozetleri kameraya döndürür (ortografik kamera sabit olduğu için her karede kopyalamak yeterli).</summary>
    public sealed class Billboard : MonoBehaviour
    {
        static Camera cam;

        void LateUpdate()
        {
            if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
