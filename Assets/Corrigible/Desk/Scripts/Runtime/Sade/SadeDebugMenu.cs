using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>Geliştirme ön ayarları: F1–9 masa, Shift+F1–9 baskın, Ctrl+F1–3 B3/B4/B7b.</summary>
    public sealed class SadeDebugMenu : MonoBehaviour
    {
        [SerializeField] MockSadeDeskDataSource source;
        [SerializeField] MonoBehaviour raidSource;
        [SerializeField] SadeDeskInteraction interaction;
        [SerializeField] ReferenceOverlay reference;
        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var raid=raidSource as IRaidPreviewSource;
            if(raid!=null)
            {
                for(int i=0;i<9;i++)if(keyboard[Key.F1+i].wasPressedThisFrame)
                {
                    if(keyboard.shiftKey.isPressed){raid.LoadPreset(i+1);if(reference!=null)reference.ScreenName="S-Baskin"+(i+1);}
                    else if(keyboard.ctrlKey.isPressed&&i<3){raid.LoadBoardPreset(new[]{"B3","B4","B7b"}[i]);if(reference!=null)reference.ScreenName=i==0?"S-Baskin2":i==1?"S-Baskin1":"S-Baskin9";}
                }
            }
            for (int i = 0; i < 9; i++)
                if (keyboard[Key.F1 + i].wasPressedThisFrame && source != null && !keyboard.shiftKey.isPressed && !keyboard.ctrlKey.isPressed)
                {
                    source.LoadSample(i + 1);
                    if (reference != null) reference.ScreenName = "S-Masa" + (i + 1);
                }
            if (keyboard.f10Key.wasPressedThisFrame) DeskLoc.Instance.Toggle();
            if (keyboard.iKey.wasPressedThisFrame && keyboard.leftCtrlKey.isPressed && interaction != null) interaction.SetIllusion(!source.Illusion);
#endif
        }
    }
}
