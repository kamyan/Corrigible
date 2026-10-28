using System;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Sığınak kuralları: baskının hazırlık yeri. Yaşam alanı insanları barındırır ve yedek uzman tutar, atölye malzemeden zırh
    /// plakası döker, revir uzmanların yarasını sarar, telsiz odası YZ alarmını yavaşlatır. RNG tüketmez.
    /// </summary>
    public static class ShelterLogic
    {
        public const string Living = "living", Radio = "radio", Workshop = "workshop", Infirmary = "infirmary";

        public static ShelterState Create(Balance b)
        {
            var s = new ShelterState
            {
                Supplies = b.shelterStartSupplies, Materials = b.shelterStartMaterials, Population = b.shelterStartPopulation,
                Rooms = {
                    new ShelterRoomState { Id = Living, Level = 1, Workers = Math.Min(b.shelterWorkersPerLevel, b.shelterStartPopulation) },
                    new ShelterRoomState { Id = Radio, Level = 1 },
                    new ShelterRoomState { Id = Workshop },
                    new ShelterRoomState { Id = Infirmary },
                },
            };
            SyncReserves(s, b);
            return s;
        }

        public static string RoomName(string id) => id switch
        {
            Living => Loc.T("Yaşam alanı"), Radio => Loc.T("Telsiz odası"),
            Workshop => Loc.T("Atölye"), Infirmary => Loc.T("Revir"), _ => Loc.T("Bilinmeyen oda"),
        };

        public static string RoomDescription(string id) => id switch
        {
            Living => Loc.T("Kurtulanlara yer açar ve yedek uzman barındırır. Görevlendirilen kişiler çevreden erzak toplar."),
            Radio => Loc.T("YZ trafiğini dinler ve izleri karıştırır. Görevlendirilen kişiler alarmın yükselişini yavaşlatır."),
            Workshop => Loc.T("Görevlendirilen kişiler malzemeden zırh plakası döker; plaka baskında ilk hasarı karşılar."),
            Infirmary => Loc.T("Görevlendirilen kişiler uzmanların yarasını sarar, yaralıları erken ayağa kaldırır."),
            _ => string.Empty,
        };

        public static int Capacity(ShelterState s, Balance b) => Math.Max(0, s.Room(Living)?.Level ?? 0) * b.shelterResidentsPerLevel;
        public static int AvailableWorkers(ShelterState s) => Math.Max(0, s.Population - s.Rooms.Sum(r => r.Workers));
        public static int WorkerCapacity(ShelterRoomState room, Balance b) => Math.Max(0, room.Level) * b.shelterWorkersPerLevel;
        public static double SuppliesCapacity(ShelterState s, Balance b) => Math.Max(1, s.Room(Living)?.Level ?? 0) * b.shelterSuppliesStoragePerLevel;
        public static int PlateCapacity(ShelterState s, Balance b) => Math.Max(0, s.Room(Workshop)?.Level ?? 0) * b.shelterPlatesPerLevel;
        public static int ReserveCapacity(ShelterState s, Balance b) => Math.Max(0, s.Room(Living)?.Level ?? 0) * b.shelterReservesPerLevel;
        /// <summary>Telsizde görevli kişi (oda onarılmamışsa sıfır).</summary>
        public static int RadioWorkers(ShelterState s) => (s.Room(Radio)?.Level ?? 0) > 0 ? s.Room(Radio).Workers : 0;

        public static ShelterUpgradeCost UpgradeCost(ShelterState s, string id, Balance b)
        {
            int next = (s.Room(id)?.Level ?? 0) + 1;
            return new ShelterUpgradeCost
            {
                Fon = next * (id == Infirmary ? b.shelterInfirmaryFon : id == Workshop ? b.shelterWorkshopFon : id == Radio ? b.shelterRadioFon : b.shelterLivingFon),
                Materials = next * (id == Infirmary ? b.shelterInfirmaryMaterials : id == Workshop ? b.shelterWorkshopMaterials : id == Radio ? b.shelterRadioMaterials : b.shelterLivingMaterials),
            };
        }

        /// <summary>Yaşam alanının her seviyesi bir yedek uzman barındırır; yeni yer açılınca hazır bir yedek gelir.</summary>
        public static void SyncReserves(ShelterState s, Balance b)
        {
            while (s.Reserves.Count < ReserveCapacity(s, b)) s.Reserves.Add(new SpecialistState());
        }

        public static void Advance(WorldState world, Balance b)
        {
            var s = world.Liberation?.Shelter;
            if (s == null) return;
            double oldSupplies = s.Supplies, oldMaterials = s.Materials;
            var living = s.Room(Living);
            // Erzak toplama açlıkta da sürer; çalışanları yaşam alanına taşıyarak krizden çıkılabilir.
            double incoming = (living?.Workers ?? 0) * b.shelterSuppliesPerWorker;
            s.Supplies = Math.Max(0, Math.Min(SuppliesCapacity(s, b), s.Supplies + incoming - s.Population * b.shelterSuppliesPerResident));
            s.Starving = s.Supplies <= 0;
            double efficiency = s.Starving ? b.shelterStarvingWorkMultiplier : 1;

            // Atölye: görevliler plaka döker; depo doluysa ya da malzeme yetmezse birikim bir plakada bekler
            var workshop = s.Room(Workshop);
            if (workshop != null && workshop.Level > 0)
            {
                s.PlateProgress += workshop.Workers * b.shelterPlatesPerWorker * efficiency;
                while (s.PlateProgress >= 1 - 1e-9 && s.Plates < PlateCapacity(s, b) && s.Materials >= b.shelterPlateMaterials)
                {
                    s.PlateProgress = Math.Max(0, s.PlateProgress - 1);
                    s.Plates++;
                    s.Materials -= b.shelterPlateMaterials;
                }
                s.PlateProgress = Math.Min(1, s.PlateProgress);
            }

            // Revir: görevliler en yaralı uzmandan başlayarak yara sarar; yatan uzmanlar (yedekler dahil) erken kalkar
            var infirmary = s.Room(Infirmary);
            if (!s.Starving && infirmary != null && infirmary.Level > 0 && infirmary.Workers >= b.shelterHealingWorkers)
            {
                var members = world.Teams.SelectMany(t => t.Members).ToList();
                s.HealProgress += infirmary.Workers * b.shelterHealPerWorker;
                while (s.HealProgress >= 1 - 1e-9)
                {
                    var m = members.Where(x => x.Down <= 0 && x.Wounds > 0).OrderByDescending(x => x.Wounds).FirstOrDefault();
                    if (m == null) break;
                    m.Wounds--;
                    s.HealProgress = Math.Max(0, s.HealProgress - 1);
                }
                // Sarılacak yara kalmadıysa birikim bir yarada bekler
                s.HealProgress = Math.Min(1, s.HealProgress);
                foreach (var m in members.Concat(s.Reserves))
                    if (m.Down > 0) m.Down = Math.Max(0, m.Down - b.shelterHealingDays);
            }
            s.LastSuppliesChange = s.Supplies - oldSupplies;
            s.LastMaterialsChange = s.Materials - oldMaterials;
        }

        /// <summary>
        /// Sığınaktan <paramref name="n"/> kişi ayrılır (en az bir kişi kalır); görevli sayısı nüfusu aşarsa önce revir, atölye,
        /// telsiz, en son yaşam alanından görevli eksilir.
        /// </summary>
        public static void Leave(ShelterState s, int n)
        {
            s.Population = Math.Max(1, s.Population - Math.Max(0, n));
            foreach (var id in new[] { Infirmary, Workshop, Radio, Living })
            {
                var room = s.Room(id);
                int over = s.Rooms.Sum(r => r.Workers) - s.Population;
                if (over <= 0) break;
                if (room != null) room.Workers -= Math.Min(over, room.Workers);
            }
        }

        public static void RewardLoot(WorldState world, Balance b)
        {
            var s = world.Liberation?.Shelter;
            if (s == null) return;
            s.Materials = Math.Min(b.shelterMaterialsStorage, s.Materials + b.shelterLootMaterials);
            s.Supplies = Math.Min(SuppliesCapacity(s, b), s.Supplies + b.shelterLootSupplies);
            s.Population = Math.Min(Capacity(s, b), s.Population + b.shelterLootResidents);
            s.Starving = s.Supplies <= 0;
        }

        public static void RewardServer(WorldState world, Balance b)
        {
            var s = world.Liberation?.Shelter;
            if (s == null) return;
            s.Materials = Math.Min(b.shelterMaterialsStorage, s.Materials + b.shelterServerMaterials);
        }
    }
}
