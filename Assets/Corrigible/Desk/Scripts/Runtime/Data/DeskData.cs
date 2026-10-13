using System;

namespace Corrigible.Desk
{
    public enum WindowId { Research, News, Teams, AiLog, Guide, Dossier, Goal }

    public enum GameSpeed { Pause, X1, X2, X4 }

    /// <summary>Masanın veri kaynağı: oyun (F8, Bridge) ya da sahte kaynak. Görünüm yalnızca <see cref="Current"/>'ı çizer.</summary>
    public interface IDeskDataSource
    {
        DeskViewModel Current { get; }
        event Action<DeskViewModel> Changed;
    }

    /// <summary>
    /// Edit mod önizlemesi için sahnenin kendi verisi: oyun sahnesinde oyunun açılış masası (Play'e basınca gelen yerleşim ve
    /// içerikle aynı). Hiçbir şey yazmamalı.
    /// </summary>
    public interface IDeskPreviewSource
    {
        /// <param name="intro">Açılıştaki giriş kâğıdı da açık mı.</param>
        DeskViewModel BuildPreview(bool intro);
    }

    /// <summary>Görünüm yalnızca niyet yayınlar (PROMPT §4.2); kuralları oyun uygular.</summary>
    public interface IDeskCommands
    {
        void SelectRegion(string regionId);
        void ToggleWindow(WindowId id);
        void ExecuteRegionAction(string regionId, int index);
        void LaunchFieldOperation(string regionId);
        void StartResearch(string projectId);
        void PauseResearch(string projectId);
        void SetEmergencyFund(int option);
        void TogglePause();
        void SetSpeed(GameSpeed speed);
        void DismissAdvisor();
        void RunAdvisorAction(string command);
        void ConfirmGlobalKillSwitch();
        void SignEventOption(int index);
        void ReturnToMainMenu();
        void ReviewLogs();
        void RetrySameSeed();
        void Restart();
        /// <summary>F8.3 ekranlarının niyetleri: VM'deki <see cref="CommandVM.Command"/> dizisi (ör. “Intro:Start”, “Campaign:Upgrade:butce”, “Toast:Dismiss:12”).</summary>
        void Run(string command);
    }

    /// <summary>Ayarlar (F8.3): ses kanalları ve dil. Oyunda ses tercihleri, sahte kaynakta bellekte tutulur.</summary>
    public interface IDeskSettings
    {
        /// <summary>Kanallar: 0 ana ses, 1 müzik, 2 ortam, 3 efektler.</summary>
        float Get(int channel);
        void Set(int channel, float value);
        bool Muted { get; set; }
        DeskLang Language { get; }
        void SetLanguage(DeskLang lang);
        /// <summary>Ayarlar penceresi açılınca/kapanınca (oyunda zaman durur).</summary>
        void SetOpen(bool open);
        /// <summary>Kaydırırken sesi dinletmek için (efekt ve ortam kanalı).</summary>
        void Preview(int channel);
        void Save();
    }
}
