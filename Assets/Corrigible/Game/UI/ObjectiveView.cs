using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Hedef kartı: kazanmanın iki yolu (Hizalama, Silme) için ilerleme şeritleri ve sıradaki adım.</summary>
    public sealed class ObjectiveView : MonoBehaviour
    {
        [SerializeField] RectTransform alignFill;
        [SerializeField] TMP_Text alignText;
        [SerializeField] RectTransform eraseFill;
        [SerializeField] TMP_Text eraseText;
        [SerializeField] TMP_Text next;

        public void Refresh(Simulation sim)
        {
            var a = Explain.Alignment(sim);
            var e = Explain.Erasure(sim);
            Fill(alignFill, a.Progress); alignText.text = a.Text;
            Fill(eraseFill, e.Progress); eraseText.text = e.Text;
            var n = Explain.NextStep(sim);
            next.gameObject.SetActive(n != null);
            if (n != null) next.text = n;
        }

        static void Fill(RectTransform r, float p) => r.anchorMax = new Vector2(Mathf.Clamp01(p), 1);
    }
}
