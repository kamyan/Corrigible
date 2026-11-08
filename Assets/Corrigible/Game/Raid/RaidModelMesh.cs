using System;
using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>İçe aktarılmış modelin ağ ve malzeme bağı; çalışma anında geometri üretmez.</summary>
    [Serializable]
    public sealed class RaidModelMesh
    {
        public Mesh mesh;
        public Material[] materials = Array.Empty<Material>();

        public bool Apply(MeshFilter filter, Renderer renderer)
        {
            if (mesh == null || filter == null || renderer == null) return false;
            filter.sharedMesh = mesh;
            renderer.sharedMaterials = materials;
            return true;
        }
    }
}
