using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Tüm kara dolguları tek materyal kullanır. Parametre güncellemesi Canvas mesh'ini kirletmez.</summary>
    public sealed class FxRegionLayer : MonoBehaviour
    {
        [SerializeField] public Material sourceMaterial;
        [SerializeField] public RegionFxGraphic[] regions;
        [SerializeField] public Vector4[] nodes=new Vector4[12];
        readonly Vector4[] values=new Vector4[12];
        Material runtime;
        static readonly int DataId=Shader.PropertyToID("_RegionData"),NodesId=Shader.PropertyToID("_RegionNodes");
        public Material RuntimeMaterial=>runtime;
        void OnEnable(){if(sourceMaterial==null||regions==null)return;runtime=new Material(sourceMaterial){name="FX Region Shared",hideFlags=HideFlags.DontSave};foreach(var r in regions)r.material=runtime;runtime.SetVectorArray(NodesId,nodes);runtime.SetVectorArray(DataId,values);}
        void OnDisable(){if(regions!=null)foreach(var r in regions)if(r!=null)r.material=sourceMaterial;if(runtime!=null){if(Application.isPlaying)Destroy(runtime);else DestroyImmediate(runtime);}runtime=null;}
        public void Set(int index,double level,bool unknown,bool quarantined,bool selected=false)
        {
            var data=new Vector4((float)level/100,unknown?1:0,quarantined?1:0,selected?1:0);if(values[index]==data)return;values[index]=data;
            if(runtime!=null)runtime.SetVectorArray(DataId,values);
        }
        public Vector4 Value(int index)=>values[index];
    }
}
