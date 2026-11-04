using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın ekran kâğıtlarını bağlar: veri kaynağının görünüm modelini oyun sonu raporuna dağıtır, kâğıdın düğmelerini
    /// niyet (<see cref="IDeskCommands"/>) olarak yayınlar. Odanın kendisi (<see cref="IDeskSurface"/>) ve operasyon raporu
    /// simülasyonu doğrudan okur.
    /// </summary>
    public sealed class DeskPresenter : MonoBehaviour
    {
        [Tooltip("IDeskDataSource uygulayan bileşen (oyun köprüsü).")]
        [SerializeField] MonoBehaviour dataSource;
        [Tooltip("IDeskCommands uygulayan bileşen.")]
        [SerializeField] MonoBehaviour commands;
        [SerializeField] FinalReportView finalReport;
        [Tooltip("Ayarlar dolabı (sol duvar).")]
        [SerializeField] SettingsCabinet settingsCabinet;
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
            if (finalReport != null) finalReport.RestartClicked += Restart;
            if (settingsCabinet != null) settingsCabinet.Init(settingsSource as IDeskSettings);
            Bind(source.Current);
        }

        void OnDestroy()
        {
            if (source != null) source.Changed -= Bind;
        }

        void Restart() => Run("Restart");

        void Run(string c)
        {
            if (!string.IsNullOrEmpty(c)) Commands?.Run(c);
        }

        void Bind(DeskViewModel model)
        {
            if (model == null) return;
            if (finalReport != null) finalReport.Bind(model.FinalReport);
        }
    }
}
