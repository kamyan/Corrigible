using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>İllüzyon sunum anahtarına tepki veren görünüm (PROMPT §7.2). Yalnızca kendi izinli öğelerini değiştirir; yerleşime dokunmaz.</summary>
    public interface IIllusionAware
    {
        void SetIllusion(bool on);
    }

    /// <summary>
    /// İllüzyon / Sessiz Zafer sunumu (PROMPT §7.2, açık karar 5). Geçiş sessizdir: efekt yok, bir sonraki güncellemede masa
    /// “fazla temiz” görünür. Anahtar <see cref="DeskTheme.Illusion"/>'ı (palet, damga açısı/opaklığı, bantlar) değiştirir ve
    /// sahnedeki <see cref="IIllusionAware"/> görünümlerine bildirir. Gösterilen değerler VM'den gelir; burası kural hesaplamaz.
    /// </summary>
    public sealed class IllusionPresenter : MonoBehaviour
    {
        [Tooltip("İllüzyona duyarlı görünümlerin arandığı kökler (canvas'lar).")]
        [SerializeField] Transform[] roots = System.Array.Empty<Transform>();
        [Tooltip("Açık karar 5 seçeneği: analist katmanı bu sürede (sn, ölçeksiz) solarak kaybolur. 0 = sessiz, tek seferde (varsayılan).")]
        [SerializeField] float analystFadeSeconds;

        readonly List<IIllusionAware> aware = new List<IIllusionAware>();
        bool? state;

        public bool On => state == true;
        public float AnalystFadeSeconds => analystFadeSeconds;
        public static IllusionPresenter Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            foreach (var r in roots)
                if (r != null) aware.AddRange(r.GetComponentsInChildren<IIllusionAware>(true));
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Sunumu açar/kapar. Aynı değerde hiçbir şey yapmaz (her bağlamada çağrılabilir).</summary>
        public void Set(bool on)
        {
            if (state == on) return;
            state = on;
            DeskTheme.Illusion = on;
            foreach (var a in aware) a.SetIllusion(on);
        }
    }
}
