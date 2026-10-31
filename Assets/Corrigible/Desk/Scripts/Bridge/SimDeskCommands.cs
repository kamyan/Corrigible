using Corrigible.Game;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Masanın kâğıt niyetlerini oyuna iletir: harekât bitince yeniden başlanır (“Restart”). Oda komutları (baskın emri,
    /// cephe, sığınak, vardiya) ve operasyon raporunun kapanışı <see cref="LiberationDeskView"/>'dan geçer.
    /// </summary>
    public sealed class SimDeskCommands : MonoBehaviour, IDeskCommands
    {
        [SerializeField] SimDeskDataSource source;

        GameController Game => source.Game;

        public void Run(string command)
        {
            switch (command)
            {
                case "Restart": Game.NewGame(); break;
                default: Debug.LogWarning("[Desk] Bilinmeyen komut: " + command); break;
            }
        }
    }
}
