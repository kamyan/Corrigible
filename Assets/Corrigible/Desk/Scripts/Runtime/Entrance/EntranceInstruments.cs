using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Menü modellerinin zili, ışımaları ve kapı açılırken üç gölge sınırı. Oyun durumuna dokunmaz.</summary>
    public sealed class EntranceInstruments : MonoBehaviour
    {
        [SerializeField] DoorProp door;
        [SerializeField] ShiftSwitch mains;
        [SerializeField] PartDriver striker, mainsLamp;
        [SerializeField] Light clockLight, warmLeak;
        [SerializeField] Renderer[] emitters = new Renderer[0];
        MaterialPropertyBlock block;
        float bellAt=-1, power=1, leakBase;
        bool initialized;
        void Awake()=>Init();
        void Init(){if(initialized)return;initialized=true;block=new MaterialPropertyBlock();leakBase=warmLeak?warmLeak.intensity:0;}
        public void Ring()=>bellAt=Time.unscaledTime;
        public void SetPower(float value)
        {
            Init();
            power=value;
            if(warmLeak){warmLeak.intensity=leakBase*power;warmLeak.enabled=power>.001f;}
            block??=new MaterialPropertyBlock();
            foreach(var renderer in emitters)
            {
                if(!renderer)continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                    if(materials[i]&&materials[i].IsKeywordEnabled("_EMISSION"))
                    {
                        renderer.GetPropertyBlock(block,i);block.SetColor("_EmissionColor",materials[i].GetColor("_EmissionColor")*power);renderer.SetPropertyBlock(block,i);
                    }
            }
        }
        void Update()
        {
            if(striker&&bellAt>=0)
            {
                float t=Time.unscaledTime-bellAt;
                striker.Set(t<.35f?Mathf.Sin(t*60)*6:0);
                if(t>=.35f)bellAt=-1;
            }
            if(clockLight)clockLight.shadows=door&&(door.Moving||door.IsOpen)?LightShadows.None:LightShadows.Soft;
            if(mainsLamp)mainsLamp.SetSwap(power>.001f&&mains&&mains.Current!=ShiftSwitch.State.Thrown?1:0);
        }
    }
}
