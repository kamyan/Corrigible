using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    /// <summary>Oyuncunun bir komutu ve verildiği gün.</summary>
    [Serializable]
    public sealed class SavedCommand
    {
        public int day;
        /// <summary>act, research, gift, verify, kill, advice, reserve; deneysel: team, hire, raid, raidres.</summary>
        public string kind;
        public string a, b;
    }

    /// <summary>
    /// Kayıt dosyası: tohum + başlangıç koşulları + komut geçmişi. Simülasyon deterministik olduğu için
    /// komutları tekrar oynatmak oyunu bit düzeyinde aynı duruma getirir.
    /// </summary>
    [Serializable]
    public sealed class GameRecord
    {
        public int version = 1;
        public string type;
        public uint seed;
        public bool hasProfile;
        public PlayerProfile profileAtStart;
        public Balance balance;
        /// <summary>Deneysel mod ayarları (saha ekipleri, zaman ölçeği); hasExp false ise temel kurallar.</summary>
        public bool hasExp;
        public ExpOptions exp;
        public List<SavedCommand> commands = new List<SavedCommand>();
        public int day;
    }

    public static class Replay
    {
        /// <summary>Kayıttan oyunu yeniden kurar. Oluşan olaylar atılır; bekleyen bir hediye teklifi varsa korunur.</summary>
        public static Simulation Restore(GameRecord rec)
        {
            // JSON boş dizgeyi null'dan ayırmaz; profil karşılaştırmaları null bekler
            if (rec.hasProfile && rec.profileAtStart != null && string.IsNullOrEmpty(rec.profileAtStart.lastBranch)) rec.profileAtStart.lastBranch = null;
            var sim = Simulation.NewGame(rec.hasProfile ? rec.profileAtStart : null, rec.type, rec.seed, rec.balance, rec.hasExp ? rec.exp : null);
            var s = sim.S;
            foreach (var c in rec.commands)
            {
                while (s.Day < c.day && s.Ending == null && s.PendingGift == null && s.Raid == null) sim.Step();
                Apply(sim, c);
            }
            while (s.Day < rec.day && s.Ending == null && s.PendingGift == null && s.Raid == null) sim.Step();
            s.Events.Clear();
            sim.Record = rec;
            return sim;
        }

        static void Apply(Simulation sim, SavedCommand c)
        {
            switch (c.kind)
            {
                case "act": sim.Act(c.a, c.b); break;
                case "research": sim.StartResearch(c.a); break;
                case "gift": sim.ResolveGift(c.a); break;
                case "verify": sim.Verify(c.a); break;
                case "kill": sim.GlobalKillSwitch(); break;
                case "advice": sim.DismissAdvice(); break;
                case "reserve": sim.SetResearchReserve(double.Parse(c.a, System.Globalization.CultureInfo.InvariantCulture)); break;
                case "team": sim.MoveTeam(c.a, c.b); break;
                case "hire": sim.HireTeam(); break;
                case "raid": sim.StartRaid(c.a); break;
                case "raidres": sim.ResolveRaid(RaidResult.Decode(c.a)); break;
            }
        }
    }
}
