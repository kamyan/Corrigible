using UnityEngine;
namespace Corrigible.Fx
{
    [CreateAssetMenu(menuName="Corrigible/FX Orb Render Settings")]
    public sealed class OrbRenderSettings : ScriptableObject
    {
        public Material draw,composite;
        public Texture2D glowProfile;
        [Range(1,2)] public float supersample=1.5f;
        public int maxSize=512,labMaxSize=1024;
        public int maxFps=40,minFps=16,reducedFps=8;
        public float costDownMs=14,costUpMs=6;
    }
}
