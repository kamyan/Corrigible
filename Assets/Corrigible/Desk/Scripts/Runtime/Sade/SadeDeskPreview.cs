using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>F2 örnek sahnesi: oyun kaydını başlatmadan mock veriyi HUD ve haritaya bağlar.</summary>
    [ExecuteAlways]
    public sealed class SadeDeskPreview : MonoBehaviour
    {
        [SerializeField] MockSadeDeskDataSource source;
        [SerializeField] SadeDeskView hud;
        [SerializeField] SadeMapView map;
        [SerializeField] KillSwitchEnvelopeView envelope;

        void OnEnable()
        {
            if (source != null) source.Changed += Refresh;
            DeskLoc.Instance.Changed += Refresh;
            Refresh();
        }
        void OnDisable()
        {
            if (source != null) source.Changed -= Refresh;
            DeskLoc.Instance.Changed -= Refresh;
        }
        void OnValidate() => DeskUi.Deferred(this, Refresh);
        public void Refresh()
        {
            if (source == null || source.Current == null) return;
            if (hud != null) hud.Bind(source.Current);
            if (map != null) map.Bind(source.Current.Map);
            if (envelope != null) envelope.Bind(source.Current.KillSwitch);
        }
    }
}
