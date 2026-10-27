using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın ekran kâğıtlarını bağlar: veri kaynağının görünüm modelini baskın raporuna (<see cref="EventModalView"/>) ve
    /// oyun sonu raporuna dağıtır, kâğıtların düğmelerini niyet (<see cref="IDeskCommands"/>) olarak yayınlar. Odanın kendisi
    /// (<see cref="IDeskSurface"/>) simülasyonu doğrudan okur.
    /// </summary>
    public sealed class DeskPresenter : MonoBehaviour
    {
        [Tooltip("IDeskDataSource uygulayan bileşen (oyun köprüsü).")]
        [SerializeField] MonoBehaviour dataSource;
        [Tooltip("IDeskCommands uygulayan bileşen.")]
        [SerializeField] MonoBehaviour commands;
        [SerializeField] EventModalView eventModal;
        [SerializeField] GameOverView gameOver;
        [SerializeField] SettingsView settingsView;
        [Tooltip("IDeskSettings uygulayan bileşen (oyunda ses tercihleri ve dil).")]
        [SerializeField] MonoBehaviour settingsSource;
        [Tooltip("IDeskSurface uygulayan oda bileşeni.")]
        [SerializeField] MonoBehaviour surface;

        IDeskDataSource source;

        public IDeskCommands Commands => commands as IDeskCommands;
        public IDeskSurface Surface => surface as IDeskSurface;
        public bool SurfaceBlocked => Surface?.InputBlocked == true;

        void Start()
        {
            source = dataSource as IDeskDataSource;
            if (source == null) { Debug.LogError("[Desk] Veri kaynağı IDeskDataSource değil", this); return; }
            source.Changed += Bind;
            if (eventModal != null) eventModal.DocInvoked += Run;
            if (gameOver != null) gameOver.ActionInvoked += Run;
            if (settingsView != null) settingsView.Init(settingsSource as IDeskSettings);
            Bind(source.Current);
        }

        void OnDestroy()
        {
            if (source != null) source.Changed -= Bind;
        }

        void Run(string c)
        {
            if (!string.IsNullOrEmpty(c)) Commands?.Run(c);
        }

        void Bind(DeskViewModel model)
        {
            if (model == null) return;
            if (eventModal != null) eventModal.Bind(model.Event);
            if (gameOver != null) gameOver.Bind(model.GameOver);
        }
    }
}
