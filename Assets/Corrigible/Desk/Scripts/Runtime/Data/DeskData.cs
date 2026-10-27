using System;

namespace Corrigible.Desk
{
    /// <summary>Masanın görünüm modeli kaynağı; oyun ilerleyince ya da bir komut sonuçlanınca yeniden yayınlar.</summary>
    public interface IDeskDataSource
    {
        DeskViewModel Current { get; }
        event Action<DeskViewModel> Changed;
    }

    /// <summary>Görünüm yalnızca niyet yayınlar; kuralları oyun uygular.</summary>
    public interface IDeskCommands
    {
        /// <summary>Kâğıt ve oyun sonu düğmelerinin niyeti: VM'deki <see cref="CommandVM.Command"/> (ör. “Doc:Close”, “Restart”).</summary>
        void Run(string command);
    }

    /// <summary>Ayarlar: ses kanalları ve dil (oyunda ses tercihleri).</summary>
    public interface IDeskSettings
    {
        /// <summary>Kanallar: 0 ana ses, 1 müzik, 2 ortam, 3 efektler.</summary>
        float Get(int channel);
        void Set(int channel, float value);
        bool Muted { get; set; }
        DeskLang Language { get; }
        void SetLanguage(DeskLang lang);
        /// <summary>Kaydırırken sesi dinletmek için (efekt ve ortam kanalı).</summary>
        void Preview(int channel);
        void Save();
    }
}
