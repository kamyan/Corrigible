using UnityEngine;
namespace Corrigible.Desk
{
    /// <summary>Kartın açılışında 120 ms geçiş; bitince canvas'a tekrar yazılmaz.</summary>
    public sealed class RaidCardMotion : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        float elapsed; bool opening;
        void OnEnable(){elapsed=0;opening=group!=null;if(opening)group.alpha=0;}
        void Update()
        {
            if(!opening)return;
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/SadeTheme.Active.Number("motion.popOpenS",.12f));
            float alpha=1-(1-t)*(1-t);
            if(group.alpha!=alpha)group.alpha=alpha;
            opening=t<1;
        }
    }
}
