using UnityEngine;
namespace Corrigible.Desk
{
    /// <summary>R11: kâğıt yukarıdan hafif eğimli düşer; düzen grubunun yönettiği çocuklar yerinde kalır.</summary>
    public sealed class RaidCardMotion : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] bool paperDrop;
        RectTransform rect;Vector2 rest;Quaternion rotation;
        float elapsed;bool opening;
        void OnEnable()
        {
            elapsed=0;opening=group!=null;if(opening)group.alpha=0;
            if(paperDrop){rect=(RectTransform)transform;rest=rect.anchoredPosition;rotation=rect.localRotation;}
        }
        void Update()
        {
            if(!opening)return;
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/(paperDrop?RaidTheme.Active.Number("motion.paperDropS",.28f):SadeTheme.Active.Number("motion.popOpenS",.12f)));
            float e=1-Mathf.Pow(1-t,3);group.alpha=e;
            if(paperDrop){rect.anchoredPosition=rest+Vector2.up*RaidTheme.Active.Number("motion.paperDropFromPx",40)*(1-e);rect.localRotation=rotation*Quaternion.Euler(0,0,RaidTheme.Active.Number("motion.paperDropRotDeg",1.5f)*(transform.GetSiblingIndex()%2==0?1:-1)*(1-e));}
            opening=t<1;
        }
        void OnDisable(){if(paperDrop&&rect){rect.anchoredPosition=rest;rect.localRotation=rotation;}}
    }
}
