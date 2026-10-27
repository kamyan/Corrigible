using Corrigible.Game;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Masanın kâğıt niyetlerini oyuna iletir: baskın raporu kapanır (“Doc:Close”), harekât bitince yeniden başlanır
    /// (“Restart”). Oda komutları (baskın emri, cephe, sığınak, vardiya) <see cref="LiberationDeskView"/>'dan geçer.
    /// </summary>
    public sealed class SimDeskCommands : MonoBehaviour, IDeskCommands
    {
        [SerializeField] SimDeskDataSource source;

        GameController Game => source.Game;

        public void Run(string command)
        {
            switch (command)
            {
                case "Doc:Close":
                    // Baskın raporu okundu: oda ve kamera kilidi açılır
                    source.State.RaidLogSeen = Game.Sim.S.RaidLog.Count;
                    source.MarkDirty();
                    break;
                case "Restart": Game.NewGame(); break;
                default: Debug.LogWarning("[Desk] Bilinmeyen komut: " + command); break;
            }
        }
    }
}
