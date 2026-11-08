using System.Collections.Generic;
using System.Globalization;
using System;

namespace Corrigible.Core
{
    /// <summary>Oyuncunun bir komutu ve verildiği gün.</summary>
    [System.Serializable]
    public sealed class SavedCommand
    {
        public int day;
        /// <summary>liberation_region, liberation_raid (a: sektör, b: Teknisyen'in yanına seçilen roller), raidres, shelter_upgrade,
        /// shelter_assign, door_answer (a: seçenek, b: kart).</summary>
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
        /// <summary>Dosya biçimi. Bu sürümden eski kayıtlar yüklenmez (15: dış kayıt ve dosya incelemesinin gözetmeni Boşta'nın ilk uygun kişisi (K4-A); İNCELE+DIŞ KAYIT takılması ve kurucu kimliği düzeltildi; 14: isimli sakinler, kartlı görev/teçhizat, ayrılmış üretim girdileri, kişisel eğitim, devir fişi ve topluluk kabulü; 13: Madenci ve Sis uzmanı atölyenin seviyesiyle katılır; 12: beş uzmanlı kadro ve baskın öncesi seçim; 11: 30 haritalık havuz, kıt sığınak ekonomisi; 10: sığınağın kapısı ve ziyaretçiler; öncekiler başka kurallarla oynandı).</summary>
        public const int CurrentVersion = 15;
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
                AdvanceUntil(sim, c.day);
                Apply(sim, c);
            }
            AdvanceUntil(sim, rec.day);
            s.Events.Clear();
            sim.Record = rec;
            return sim;
        }

        static void AdvanceUntil(Simulation sim, int day)
        {
            while (sim.S.Day < day && sim.S.Ending == null && !sim.S.Blocked)
            {
                int before = sim.S.Day;
                sim.Step();
                // Kartlı modelde her günün devir komutu zorunludur; eksik fiş sonsuz döngüye girmesin.
                if (sim.S.Day == before) throw new InvalidOperationException("Kayıtta vardiya devir komutu eksik.");
            }
        }

        static void Apply(Simulation sim, SavedCommand c)
        {
            switch (c.kind)
            {
                case "liberation_region": sim.SelectLiberationRegion(c.a); break;
                case "liberation_raid": sim.StartLiberationRaid(c.a, c.b); break;
                case "raidres": sim.ResolveRaid(RaidResult.Decode(c.a)); break;
                case "shelter_upgrade": sim.ShelterUpgrade(c.a); break;
                case "shelter_assign": sim.ShelterAssign(c.a, int.Parse(c.b, CultureInfo.InvariantCulture)); break;
                case "door_answer": sim.AnswerDoor(c.a); break;
                case "board_assign": sim.AssignResident(c.a, c.b); break;
                case "board_train": sim.TrainResident(c.a, c.b); break;
                case "board_equip": sim.EquipResident(c.a, c.b); break;
                case "board_scrap": sim.ScrapEquipment(c.a); break;
                case "board_auto": sim.EnableAutomaticProduction(); break;
                case "board_stage": sim.StageProductionMaterial(c.a, int.Parse(c.b, CultureInfo.InvariantCulture)); break;
                case "board_queue": sim.QueueProduction(c.a); break;
                case "board_reserve": sim.ReserveProductionMaterial(c.a, int.Parse(c.b, CultureInfo.InvariantCulture)); break;
                case "board_cancel": sim.CancelProduction(c.a); break;
                case "board_print": sim.PrintShift(); break;
                case "board_file": sim.FileShift(c.a); break;
                case "inspectApplicant": sim.InspectApplicant(c.a); break;
                case "decideApplicant": sim.DecideApplicant(c.a); break;
                case "reviewResident": sim.ReviewResident(c.a); break;
                case "restrictResident": sim.RestrictResident(c.a, c.b == "1"); break;
                case "dismissResident": sim.DismissResident(c.a); break;
            }
        }
    }
}
