using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Yazıları ortak çizim katmanında toplar; serileştirilmiş düğüm çapalarını ve görünürlüğünü izler.</summary>
    [ExecuteAlways, DefaultExecutionOrder(100)]
    public sealed class SadeMapTextLayer : MonoBehaviour
    {
        [Serializable] public sealed class Anchor
        {
            public RectTransform Source, Target;
            public CanvasGroup Visibility;
        }
        [SerializeField] Anchor[] anchors=Array.Empty<Anchor>();
        void LateUpdate()=>Refresh();
        public void Refresh()
        {
            var scale=transform.lossyScale;
            foreach(var anchor in anchors)
            {
                var source=anchor.Source;var target=anchor.Target;
                if(source==null||target==null)continue;
                if(target.position!=source.position)target.position=source.position;
                if(target.rotation!=source.rotation)target.rotation=source.rotation;
                var desired=new Vector3(source.lossyScale.x/scale.x,source.lossyScale.y/scale.y,source.lossyScale.z/scale.z);
                if(target.localScale!=desired)target.localScale=desired;
                if(target.sizeDelta!=source.rect.size)target.sizeDelta=source.rect.size;
                float alpha=source.gameObject.activeInHierarchy?1:0;
                if(anchor.Visibility!=null&&anchor.Visibility.alpha!=alpha)anchor.Visibility.alpha=alpha;
            }
        }
    }
}
