using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Ayarlar dolabının oda bağlantısı (Docs/AYARLAR-DOLABI-VE-MENU.md Katman 1, 8). Dolap açılmak isteyince (dolaba tıklama; K1'den
    /// görünür, ya da Esc; ekranda AYARLAR düğmesi yok) kamera K9'a döner ve kapak kamera varınca açılır; kapanınca kamera önceki poza
    /// döner. Dolabın seslerini çalar, dil değişince teleks bir satır basar. Dolaptaki kapaklı kol (MENÜYE DÖN) oyunu
    /// kaydeder, oda kararır ve menü sahnesi açılır; menü sahnesi build'de yoksa oyundan çıkar.
    /// </summary>
    public sealed class SiginakCabinet : MonoBehaviour
    {
        [SerializeField] SettingsCabinet cabinet;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] BridgeSettings settings;
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] TelexFeed telex;
        [Tooltip("Emir belgesi açıkken dolap açılırsa belge gizlenir.")]
        [SerializeField] SiginakBriefing briefing;
        [Tooltip("Menüye dönüşte oda kararır (açılışın siyah örtüsü).")]
        [SerializeField] OpeningSequence opening;
        [Tooltip("Dolabın sesinin geldiği yer (sağ-sol dengesi).")]
        [SerializeField] Camera view;

        SiginakPose back = SiginakPose.Desk;
        bool leaving;

        public SettingsCabinet Cabinet => cabinet;
        /// <summary>Menüye dönüş sürüyor (oda kararıyor, sahne yükleniyor).</summary>
        public bool Leaving => leaving;

        void OnEnable()
        {
            cabinet.OpenRequested += Open;
            cabinet.Closing += Closing;
            cabinet.Sound += Play;
            cabinet.LanguageChanged += LanguageChanged;
            if (cabinet.Exit != null) { cabinet.Exit.Thrown += Leave; cabinet.Exit.Armed += Armed; }
        }

        void OnDisable()
        {
            cabinet.OpenRequested -= Open;
            cabinet.Closing -= Closing;
            cabinet.Sound -= Play;
            cabinet.LanguageChanged -= LanguageChanged;
            if (cabinet.Exit != null) { cabinet.Exit.Thrown -= Leave; cabinet.Exit.Armed -= Armed; }
        }

        void Open()
        {
            if (cabinet.IsOpen || leaving) return;
            back = rig.Current == SiginakPose.Cabinet ? SiginakPose.Desk : rig.Current;
            rig.Focus(SiginakPose.Cabinet);
            cabinet.Open(rig.TransitionRemaining);
            if (briefing != null) briefing.SetVeiled(true);
        }

        void Closing()
        {
            if (leaving) return;
            if (briefing != null) briefing.SetVeiled(false);
            rig.Focus(back);
            settings.RebuildIfPending();
        }

        void Play(SettingsCabinet.Noise n)
        {
            float pan = Sfx.PanOf(view, cabinet.transform.position);
            switch (n)
            {
                case SettingsCabinet.Noise.Open: Sfx.Play(Cue.CabinetOpen, 1, pan); break;
                case SettingsCabinet.Noise.Close: Sfx.Play(Cue.CabinetClose, 1, pan); break;
                case SettingsCabinet.Noise.Detent: Sfx.Play(Cue.FaderDetent, 1, pan); break;
                case SettingsCabinet.Noise.DetentSoft: Sfx.Play(Cue.FaderDetent, 0.45f, pan, 1.15f); break;
                case SettingsCabinet.Noise.Switch: Sfx.Play(Cue.SwitchClack, 1, pan); break;
                case SettingsCabinet.Noise.Selector: Sfx.Play(Cue.SelectorClunk, 1, pan); break;
            }
        }

        void LanguageChanged(DeskLang lang) =>
            telex.Post(surface.Shift, Loc.T("DİL"), lang == DeskLang.En ? "ENGLISH" : "TÜRKÇE", TelexKind.Info);

        void Armed() => Sfx.Play(Cue.SwitchClack, 0.6f, Sfx.PanOf(view, cabinet.transform.position), 1.2f);

        /// <summary>Kol indi: kaydet, karart, menüye dön.</summary>
        void Leave()
        {
            if (leaving) return;
            leaving = true;
            Sfx.Play(Cue.SwitchClack, 1, Sfx.PanOf(view, cabinet.transform.position), 0.8f);
            game.Save();   // masa notları her poz değişiminde zaten yazıldı (SiginakDesk.SaveNotes)
            opening.FadeOut(0.5f, () =>
            {
                if (Application.CanStreamedLevelBeLoaded(GameLaunch.EntranceScene)) SceneManager.LoadScene(GameLaunch.EntranceScene);
                else Quit();
            });
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
