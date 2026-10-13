using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masa klavye haritası (PROMPT §8, açık karar 1): R/N/T/L/H pencereler, 1–4 bölge eylemleri, B saha operasyonu,
    /// Boşluk duraklat, +/− (ve numpad) hız, Esc en üstteki pencere. Onay kartı açıkken yalnızca Enter/Esc karta gider.
    /// Olay bildirimi açıkken: 1/2/3 seç, Enter imzala, Boşluk basılıyken haritaya bak (PROMPT §8).
    /// Proje düzeni gibi <c>Keyboard.current</c> okunur; her karede bellek ayırmaz.
    /// </summary>
    public sealed class DeskInput : MonoBehaviour
    {
        [SerializeField] DeskPresenter presenter;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || presenter.Commands == null) return;
            bool enter = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

            // F8.3 ekranları (üstten alta): ayarlar, sahte zafer, giriş, kampanya
            var st = SettingsView.Instance;
            if (st != null && st.IsOpen) { if (kb.escapeKey.wasPressedThisFrame) st.Close(); return; }
            var fw = FakeWinView.Instance;
            if (fw != null && fw.IsOpen) return;
            // Eğitim notu: Enter İleri (ya da ipucunu kapatır), Esc ipucunu kapatır; koşulla ilerleyen adımda tuşlar masaya kalır
            var tut = DeskTutorialView.Instance;
            if (tut != null && tut.HasNext && (enter || (tut.IsTip && kb.escapeKey.wasPressedThisFrame))) { tut.PressNext(); return; }
            var intro = IntroView.Instance;
            if (intro != null && intro.IsOpen)
            {
                for (int i = 0; i < 3; i++)
                    if (kb[Key.Digit1 + i].wasPressedThisFrame || kb[Key.Numpad1 + i].wasPressedThisFrame) intro.Choose(i);
                if (enter) intro.Begin();
                return;
            }
            var cp = CampaignView.Instance;
            if (cp != null && cp.IsOpen)
            {
                for (int i = 0; i < 3; i++)
                    if (kb[Key.Digit1 + i].wasPressedThisFrame || kb[Key.Numpad1 + i].wasPressedThisFrame) cp.Select(i);
                if (enter) cp.SignSelected();
                return;
            }
            // Oyun sonu (C4): Enter yeniden başla, L günlükler; masa kısayolları kapalı
            var go = GameOverView.Instance;
            if (go != null && go.IsOpen)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) go.InvokeKey("Enter");
                if (kb.lKey.wasPressedThisFrame) go.InvokeKey("L");
                return;
            }

            // Olay bildirimi (C3): 1/2/3 seç, Enter imzala, Boşluk basılı haritaya bak; masa kısayolları ve Esc kapalı (karar zorunlu)
            var ev = EventModalView.Instance;
            if (ev != null && ev.IsOpen && ev.IsDoc)
            {
                // Belge: Enter birincil (ör. taktik haritaya geç / masaya dön), Esc ikincil (vazgeç)
                if (enter) ev.PressDoc(true);
                else if (kb.escapeKey.wasPressedThisFrame) ev.PressDoc(false);
                ev.SetPeek(kb.spaceKey.isPressed);
                return;
            }
            if (ev != null && ev.IsOpen)
            {
                for (int i = 0; i < 3; i++)
                    if (kb[Key.Digit1 + i].wasPressedThisFrame || kb[Key.Numpad1 + i].wasPressedThisFrame) ev.Select(i);
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) ev.SignSelected();
                ev.SetPeek(kb.spaceKey.isPressed);
                return;
            }

            // Onay kartı açıkken yalnızca Enter (imzala) / Esc (vazgeç); masa kısayolları kapalı
            var modal = ConfirmModalView.Instance;
            if (modal != null && modal.IsOpen)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) modal.Sign();
                else if (kb.escapeKey.wasPressedThisFrame) modal.Cancel();
                return;
            }

            if (kb.rKey.wasPressedThisFrame) presenter.ToggleWindow(WindowId.Research);
            if (kb.nKey.wasPressedThisFrame) presenter.ToggleWindow(WindowId.News);
            if (kb.tKey.wasPressedThisFrame) presenter.ToggleWindow(WindowId.Teams);
            if (kb.lKey.wasPressedThisFrame) presenter.ToggleWindow(WindowId.AiLog);
            if (kb.hKey.wasPressedThisFrame) presenter.ToggleWindow(WindowId.Guide);

            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) presenter.RegionAction(0);
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) presenter.RegionAction(1);
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) presenter.RegionAction(2);
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) presenter.RegionAction(3);
            if (kb.bKey.wasPressedThisFrame) presenter.FieldOperation();

            if (kb.spaceKey.wasPressedThisFrame) presenter.Commands.TogglePause();
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) presenter.StepSpeed(+1);
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) presenter.StepSpeed(-1);
            if (kb.escapeKey.wasPressedThisFrame) presenter.Escape();
        }
    }
}
