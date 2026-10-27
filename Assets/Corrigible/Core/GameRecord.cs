using System.Collections.Generic;
using System.Globalization;

namespace Corrigible.Core
{
    /// <summary>Oyuncunun bir komutu ve verildiği gün.</summary>
    [System.Serializable]
    public sealed class SavedCommand
    {
        public int day;
        /// <summary>liberation_region, liberation_raid, raidres, shelter_upgrade, shelter_assign.</summary>
        public string kind;
        public string a, b;
    }

    /// <summary>
    /// Kayıt dosyası: tohum + denge değerleri + komut geçmişi. Simülasyon deterministik olduğu için komutları tekrar
    /// oynatmak oyunu aynı duruma getirir. Vardiyalar komutların günlerinden ve son günden yeniden kurulur.
    /// </summary>
    [System.Serializable]
    public sealed class GameRecord
    {
        /// <summary>Dosya biçimi. Bu sürümden eski (kriz oyunu dönemi) kayıtlar yüklenmez.</summary>
        public const int CurrentVersion = 4;
        public int version = CurrentVersion;
        public uint seed;
        public Balance balance;
        public List<SavedCommand> commands = new List<SavedCommand>();
        public int day;
    }

    public static class Replay
    {
        /// <summary>Kayıttan oyunu yeniden kurar; oluşan olaylar atılır.</summary>
        public static Simulation Restore(GameRecord rec)
        {
            var sim = Simulation.NewGame(rec.seed, rec.balance);
            var s = sim.S;
            foreach (var c in rec.commands)
            {
                while (s.Day < c.day && s.Ending == null && !s.Blocked) sim.Step();
                Apply(sim, c);
            }
            while (s.Day < rec.day && s.Ending == null && !s.Blocked) sim.Step();
            s.Events.Clear();
            sim.Record = rec;
            return sim;
        }

        static void Apply(Simulation sim, SavedCommand c)
        {
            switch (c.kind)
            {
                case "liberation_region": sim.SelectLiberationRegion(c.a); break;
                case "liberation_raid": sim.StartLiberationRaid(c.a); break;
                case "raidres": sim.ResolveRaid(RaidResult.Decode(c.a)); break;
                case "shelter_upgrade": sim.ShelterUpgrade(c.a); break;
                case "shelter_assign": sim.ShelterAssign(c.a, int.Parse(c.b, CultureInfo.InvariantCulture)); break;
            }
        }
    }
}
