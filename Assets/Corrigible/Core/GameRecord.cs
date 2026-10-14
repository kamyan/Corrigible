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
        /// <summary>Dosya biçimi. Sürüm 3 ekonomi kural sürümünü ayrıca saklar.</summary>
        public int version = 3;
        /// <summary>1/2 eski ekonomi, 3 bütünleşik ekonomi; tekrar kaydetmede korunur.</summary>
        public int ruleVersion;
        public string type;
        public uint seed;
        public bool hasProfile;
        public PlayerProfile profileAtStart;
        public Balance balance;
        /// <summary>Deneysel mod ayarları (saha ekipleri, zaman ölçeği); hasExp false ise temel kurallar.</summary>
        public bool hasExp;
        public ExpOptions exp;
        /// <summary>Unity'ye özgü kurallar (aşamalı öğrenme, olay kartları, kampanya); hasRules false ise temel kurallar.</summary>
        public bool hasRules;
        public RuleOptions rules;
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
            int ruleVersion = rec.ruleVersion > 0 ? rec.ruleVersion : rec.version >= 3 ? 3 : Math.Max(1, rec.version);
            var sim = Simulation.NewGame(rec.hasProfile ? rec.profileAtStart : null, rec.type, rec.seed, rec.balance, rec.hasExp ? rec.exp : null, rec.hasRules ? rec.rules : null, ruleVersion);
            var s = sim.S;
            foreach (var c in rec.commands)
            {
                while (s.Day < c.day && s.Ending == null && !s.Blocked) sim.Step();
                Apply(sim, c, ruleVersion);
            }
            while (s.Day < rec.day && s.Ending == null && !s.Blocked) sim.Step();
            s.Events.Clear();
            rec.version = 3;
            rec.ruleVersion = ruleVersion;
            sim.Record = rec;
            return sim;
        }

        static void Apply(Simulation sim, SavedCommand c, int recordVersion)
        {
            switch (c.kind)
            {
                case "act": sim.Act(c.a, c.b); break;
                case "research": sim.ReplayResearch(c.a, recordVersion); break;
                case "gift": sim.ResolveGift(c.a); break;
                case "verify": sim.Verify(c.a); break;
                case "kill": sim.GlobalKillSwitch(); break;
                case "advice": sim.DismissAdvice(); break;
                case "reserve": sim.SetResearchReserve(double.Parse(c.a, System.Globalization.CultureInfo.InvariantCulture)); break;
                case "team": sim.MoveTeam(c.a, c.b); break;
                case "hire": sim.HireTeam(); break;
                case "raid": sim.StartRaid(c.a); break;
                case "raidres": sim.ResolveRaid(RaidResult.Decode(c.a)); break;
                case "event": sim.ResolveEvent(int.Parse(c.a, System.Globalization.CultureInfo.InvariantCulture)); break;
                case "operation": sim.BuyOperation(c.a); break;
                case "recovery": sim.StartRecovery(c.a); break;
                case "diplomacy": sim.DiplomaticTalk(c.a); break;
                case "research_pause": sim.SetResearchPaused(c.a == "1"); break;
            }
        }
    }
}
