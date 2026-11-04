namespace Corrigible.Game
{
    /// <summary>
    /// Menü sahnesinden (kapının dışı) oyun sahnesine taşınan niyet: yeni harekât ya da kayıttan devam. Menü kapıyı açarken
    /// yazar, DeskGame açılışta bir kez okur (<see cref="Take"/>). DeskGame doğrudan açıldıysa (editör, testler, görüntü seti)
    /// niyet yoktur ve bugünkü açılış (emir belgesi, BAŞLA, KAYITTAN DEVAM) çalışır.
    /// </summary>
    public static class GameLaunch
    {
        public enum Mode { None, NewCampaign, Continue }

        public const string EntranceScene = "Giris", DeskScene = "DeskGame";

        public static Mode Pending { get; set; }

        /// <summary>Niyeti okur ve sıfırlar.</summary>
        public static Mode Take()
        {
            var m = Pending;
            Pending = Mode.None;
            return m;
        }
    }
}
