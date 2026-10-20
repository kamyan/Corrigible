using UnityEngine;
namespace Corrigible.Ayna.Game
{
    // Hareket tercihi yalnız bu oturumun sunumunu etkiler; simülasyon zamanı değişmez.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class AynaInterfaceMotion : MonoBehaviour
    {
        [SerializeField] float duration=.2f;
        public static bool Reduced { get; set; }
        public static bool ModalOpen { get; set; }
        CanvasGroup group;float started;
        void OnEnable(){group=GetComponent<CanvasGroup>();started=Time.unscaledTime;if(Application.isPlaying)group.alpha=Reduced?1:0;}
        void Update(){if(group!=null)group.alpha=Reduced?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-started)/Mathf.Max(.01f,duration)));}
        void OnDisable(){if(group!=null)group.alpha=1;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetPreference(){Reduced=false;ModalOpen=false;}
    }
}
