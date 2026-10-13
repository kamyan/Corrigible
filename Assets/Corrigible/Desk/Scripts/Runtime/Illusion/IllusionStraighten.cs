using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// İllüzyonda elle konmuş eğik süsü (bant parçası vb.) düzleştirir (PROMPT §7.2: “Bant parçaları düz”). Asıl açı
    /// prefab'daki değerdir; İllüzyon kapanınca geri döner. Konum değişmez.
    /// </summary>
    public sealed class IllusionStraighten : MonoBehaviour, IIllusionAware
    {
        float authored;
        bool cached;

        void Awake() => Cache();

        void Cache()
        {
            if (cached) return;
            authored = transform.localEulerAngles.z;
            cached = true;
        }

        public void SetIllusion(bool on)
        {
            Cache();
            transform.localEulerAngles = new Vector3(0, 0, on ? 0 : authored);
        }
    }
}
