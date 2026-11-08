using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Poz başına ışık tarifi (Revizyon 3 · R14c, token <c>lightsRecipe.*</c>, IMPLEMENTATION_MAP §3.13): her pozda hangi
    /// ışığın yandığı, taban şiddeti (URP değeri, Revizyon 2 ışıklarıyla kalibre) ve gölge taşıyıp taşımadığı. Token metni yalnız
    /// kaynak; değerler bu asset'te elle tutulur. Bir pozda en çok 4 gölgeli ışık (test). Tarifte geçmeyen ışığa dokunulmaz.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Revizyon 3/Poz ışık tarifi", fileName = "PoseLightRecipe")]
    public sealed class PoseLightRecipe : ScriptableObject
    {
        public const int MaxShadowed = 4;

        [Serializable]
        public struct LightState
        {
            [Tooltip("Sahnedeki bağlamanın kimliği (bulbSpot, bulbFill, deskLamp, boardLamp, crtLeak, radioDial).")]
            public string id;
            public bool on;
            [Min(0)] public float intensity;
            public bool shadows;
        }

        [Serializable]
        public struct Entry
        {
            public SiginakPose pose;
            [Tooltip("Sahnenin ortam ışığı çarpanı (K1 = 1).")]
            [Min(0)] public float ambientScale;
            public LightState[] lights;
        }

        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(SiginakPose pose, out Entry entry)
        {
            foreach (var e in entries)
                if (e.pose == pose) { entry = e; return true; }
            entry = default;
            return false;
        }

        /// <summary>Pozda yanık ve gölgeli ışık sayısı.</summary>
        public static int ShadowedCount(Entry entry)
        {
            int n = 0;
            if (entry.lights != null) foreach (var l in entry.lights) if (l.on && l.shadows) n++;
            return n;
        }

        public void Configure(Entry[] values) => entries = values ?? Array.Empty<Entry>();
    }
}
