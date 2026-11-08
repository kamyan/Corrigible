using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Menü sahnesinin (kapının dışı) kamerası: çapalar arasında yumuşak geçiş (sığınak kamerasının eğrisiyle) ve kapıdan
    /// içeri yürüyüş (<see cref="Walk"/>: noktalar boyunca tek ve kesintisiz hareket). Girdi okumaz; ne zaman nereye
    /// gideceğini menünün bağlayıcısı söyler.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class EntranceCamera : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField, Min(0.01f)] float transitionSeconds = 0.6f;

        struct State { public Vector3 position; public Quaternion rotation; public float fieldOfView; }

        State from, to;
        Transform[] path;
        float[] pathFov;
        float elapsed = float.MaxValue, duration = 1;
        bool linearWalk;

        public Camera View => view;
        public bool Moving => elapsed < duration;
        public float Remaining => Mathf.Max(0, duration - elapsed);

        void Awake() { if (view == null) view = GetComponent<Camera>(); }

        /// <summary>Çapaya geçer; <paramref name="seconds"/> &lt; 0 ise varsayılan süre, 0 ise hemen.</summary>
        public void Go(Transform anchor, float fieldOfView, float seconds = -1)
        {
            if (anchor == null) return;
            path = null;
            linearWalk = false;
            from = Capture();
            to = new State { position = anchor.position, rotation = anchor.rotation, fieldOfView = fieldOfView };
            duration = seconds < 0 ? transitionSeconds : Mathf.Max(0.0001f, seconds);
            elapsed = seconds == 0 ? duration : 0;
            if (seconds == 0) Apply(to);
        }

        /// <summary>Bulunduğu yerden <paramref name="points"/> boyunca yürür (uzunluğa göre zamanlanır, baş ve son yumuşak).</summary>
        public void Walk(Transform[] points, float fieldOfView, float seconds, bool linear = false)
        {
            path = points;
            linearWalk = linear;
            pathFov = new float[points.Length];
            for (int i = 0; i < pathFov.Length; i++) pathFov[i] = fieldOfView;
            from = Capture();
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0;
        }

        State Capture() => new State { position = transform.position, rotation = transform.rotation, fieldOfView = view.fieldOfView };

        void LateUpdate()
        {
            if (elapsed >= duration) return;
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float t = linearWalk ? progress : Ease(progress);
            Apply(path != null ? AlongPath(t) : Lerp(from, to, t));
        }

        State AlongPath(float t)
        {
            // Durakların konumları: başlangıç + noktalar; parça, uzunluk oranına göre seçilir
            int n = path.Length + 1;
            var stops = new State[n];
            stops[0] = from;
            for (int i = 1; i < n; i++) stops[i] = new State { position = path[i - 1].position, rotation = path[i - 1].rotation, fieldOfView = pathFov[i - 1] };
            float total = 0;
            var lengths = new float[n - 1];
            for (int i = 0; i < n - 1; i++) { lengths[i] = Vector3.Distance(stops[i].position, stops[i + 1].position); total += lengths[i]; }
            float d = t * Mathf.Max(1e-4f, total);
            for (int i = 0; i < n - 1; i++)
            {
                if (d <= lengths[i] || i == n - 2) return Lerp(stops[i], stops[i + 1], lengths[i] > 0 ? Mathf.Clamp01(d / lengths[i]) : 1);
                d -= lengths[i];
            }
            return stops[n - 1];
        }

        static State Lerp(State a, State b, float t) => new State
        {
            position = Vector3.Lerp(a.position, b.position, t),
            rotation = Quaternion.Slerp(a.rotation, b.rotation, t),
            fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t),
        };

        void Apply(State s)
        {
            transform.SetPositionAndRotation(s.position, s.rotation);
            view.fieldOfView = s.fieldOfView;
        }

        static float Ease(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
}
