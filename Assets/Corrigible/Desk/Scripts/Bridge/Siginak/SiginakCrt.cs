using System.Collections;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// CRT'nin oyun bağlantısı (Docs/UI_Siginak Aşama 7): masadaki ekranı (<see cref="CrtScreen"/>) durumla günceller ve
    /// <see cref="RaidLauncher"/>'ın geçiş kancası olarak tam ekran CRT dizisini (<see cref="CrtTransition"/>) oynatır.
    /// Sunum anahtarı yeni görünümde kancayı başlatıcıya yazar; eski görünümde kanca yoktur. Kanca çağrıları başlatıcının
    /// coroutine'inde koşar (bu nesne baskın sırasında gizli oda kökünde olsa da). Önündeki telsiz kayıt defterini de yazar
    /// (son dört kayıt, <see cref="RadioLogBuilder"/>).
    /// </summary>
    public sealed class SiginakCrt : MonoBehaviour, IRaidTransition
    {
        [SerializeField] GameController game;
        [SerializeField] CrtScreen screen;
        [SerializeField] CrtTransition overlay;
        [SerializeField] RadioLogBook logBook;
        [Tooltip("Baskına geçişte kamerayı CRT'ye götüren ve dönüşte geri getiren rig (geri çekil → dön → yaklaş).")]
        [SerializeField] SiginakCameraRig rig;

        CrtState shown = (CrtState)(-1);
        Simulation Sim => game != null ? game.Sim : null;

        public void Configure(GameController controller, CrtScreen crtScreen, CrtTransition transition)
        {
            game = controller; screen = crtScreen; overlay = transition;
        }

        void OnEnable()
        {
            if (game != null) { game.Changed += Refresh; game.GameLoaded += Refresh; game.NewGameStarted += Refresh; }
            DeskLoc.Instance.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Refresh; game.GameLoaded -= Refresh; game.NewGameStarted -= Refresh; }
            DeskLoc.Instance.Changed -= Refresh;
        }

        // Durum yükleme anında da değişir (sahne açıldı / kapandı): her kare yalnız durumu karşılaştır
        void Update()
        {
            if (CrtViewBuilder.State(Sim, RaidLauncher.IsRaidLoaded) != shown) Refresh();
        }

        void Refresh()
        {
            var sim = Sim;
            if (sim == null) return;
            var d = CrtViewBuilder.Screen(sim, RaidLauncher.IsRaidLoaded);
            shown = d.State;
            screen.Bind(d);
            if (logBook != null) logBook.Show(RadioLogBuilder.Entries(sim, RaidLauncher.IsRaidLoaded));
        }

        // ---------- IRaidTransition (K5b) ----------

        public float ApproachSeconds => rig != null ? rig.DiveSeconds : SiginakTheme.Active.NumberOf("motion.crtTurnS", 0.5f);
        public void BeginApproach() => overlay.BeginApproach(ApproachSeconds);

        // Kamera önce geri çekilir, CRT'ye döner, ekrana yaklaşır; boşluk ya da tık dalışı ve diziyi birlikte atlar
        public IEnumerator Approach(Transform target, float fieldOfView)
        {
            if (rig == null) return null;
            rig.BeginDive(target, fieldOfView);
            return rig.RunDive(overlay.PollSkip);
        }

        // Dönüşte kamera tüp kapanırken monitörde bekler, sonra aynı yoldan geri çıkar; boşluk ya da tık yolu atlar
        public IEnumerator Depart(Transform from, float fieldOfView)
        {
            if (rig == null) return null;
            rig.BeginReturn(from, fieldOfView, SiginakTheme.Active.NumberOf("motion.crtReturnDelayS", 0.3f));
            return rig.RunDive(CrtTransition.SkipPressed);
        }
        // Hat açılırken telsiz frekansı arar; dönüşte hat kapanır
        public IEnumerator Run() { Sfx.Play(Cue.RadioTune); return overlay.Run(CrtViewBuilder.Transition(Sim)); }
        public void SceneLoaded() => overlay.SceneLoaded();
        public IEnumerator Return() { Sfx.Play(Cue.RadioReport, 0.6f); return overlay.Return(CrtViewBuilder.Transition(Sim)); }
    }
}
