using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Geliştirme kısayolları (PROMPT §8, §9): F1–F4 C1/C2/C3/C4 ön ayarı, F5 sayaçları kaydır (ibre yumuşatması),
    /// F6 İllüzyon sunumunu veriyi değiştirmeden aç/kapa (yalnızca izinli öğeler değişmeli), F9 dil TR↔EN, F10 ipucu katmanı.
    /// Referans katmanı F11/F12 <see cref="ReferenceOverlay"/>'de. Yalnızca Editor/Development.
    /// </summary>
    public sealed class DeskDebugMenu : MonoBehaviour
    {
        [SerializeField] MockDeskDataSource source;
        [SerializeField] ReferenceOverlay overlay;
        [SerializeField] string[] referenceNames = { "C1_main", "C2_illusion", "C3_event", "C4_gameover" };
        [SerializeField] GameObject clueOverlay;
        [SerializeField] IllusionPresenter illusion;

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame) Preset(0);
            if (kb.f2Key.wasPressedThisFrame) Preset(1);
            if (kb.f3Key.wasPressedThisFrame) Preset(2);
            if (kb.f4Key.wasPressedThisFrame) Preset(3);
            if (kb.f5Key.wasPressedThisFrame && source != null) source.Perturb();
            if (kb.f6Key.wasPressedThisFrame && illusion != null) illusion.Set(!illusion.On);
            if (kb.f9Key.wasPressedThisFrame) DeskLoc.Instance.Toggle();
            if (kb.f10Key.wasPressedThisFrame && clueOverlay != null) clueOverlay.SetActive(!clueOverlay.activeSelf);
#endif
        }

        public void Preset(int i)
        {
            if (source == null || i >= source.PresetCount) return;
            source.Load(i);
            if (overlay != null && i < referenceNames.Length) overlay.ScreenName = referenceNames[i];
        }
    }
}
