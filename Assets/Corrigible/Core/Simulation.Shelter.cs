using System;
using System.Linq;
using System.Globalization;

namespace Corrigible.Core
{
    public sealed partial class Simulation
    {
        public ShelterState Shelter => S.Liberation?.Shelter;

        string ShelterCommandCheck()
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            if (Shelter == null) return Loc.T("Bu kayıtta sığınak yönetimi yok.");
            if (Board != null) return Loc.T("Oda geliştirmeleri kaldırıldı. Kişi kartlarını planlama panosunda görevlendir.");
            return null;
        }

        public string ShelterUpgradeCheck(string roomId)
        {
            string why = ShelterCommandCheck();
            if (why != null) return why;
            var room = Shelter.Room(roomId);
            if (room == null) return Loc.T("Böyle bir sığınak odası yok.");
            if (room.Level >= B.shelterMaxRoomLevel) return Loc.T("Oda en yüksek seviyede.");
            string locked = ShelterLevelLock(roomId);
            if (locked != null) return locked;
            var cost = ShelterLogic.UpgradeCost(Shelter, roomId, B);
            if (S.Fon < cost.Fon || Shelter.Materials < cost.Materials)
                return Loc.F("{0} fon ve {1} malzeme gerekiyor.", cost.Fon, cost.Materials);
            return null;
        }

        /// <summary>Özgür bölge sayısı odanın sıradaki seviyesine yetmiyorsa gerekçe (seviye 2 ve 3 bölgeyle açılır); yetiyorsa null.</summary>
        public string ShelterLevelLock(string roomId)
        {
            var room = Shelter?.Room(roomId);
            if (room == null) return null;
            int next = room.Level + 1, free = S.Liberation.Regions.Count(r => r.Completed);
            int need = next >= 3 ? B.shelterLevel3Regions : next == 2 ? B.shelterLevel2Regions : 0;
            return free >= need ? null : Loc.F("Seviye {0} için {1} özgür bölge gerekiyor (şimdi {2}).", next, need, free);
        }

        public ActResult ShelterUpgrade(string roomId) => RunCommand(() =>
        {
            string why = ShelterUpgradeCheck(roomId);
            if (why != null) return ActResult.Fail(why);
            var cost = ShelterLogic.UpgradeCost(Shelter, roomId, B);
            S.Fon -= cost.Fon;
            Shelter.Materials -= cost.Materials;
            var room = Shelter.Room(roomId);
            room.Level++;
            // Yaşam alanının yeni seviyesi bir yedek uzman getirir; atölyenin seviyesi uzman katar
            ShelterLogic.SyncReserves(Shelter, B);
            var joined = SyncSpecialists();
            return joined.Count == 0
                ? ActResult.Success(Loc.F("{0} hazır: seviye {1}.", ShelterLogic.RoomName(roomId), room.Level))
                : ActResult.Success(Loc.F("{0} hazır: seviye {1}. {2} kadroya katıldı.", ShelterLogic.RoomName(roomId), room.Level,
                    string.Join(", ", joined.Select(r => Baskin.UnitTypes[r].Name))));
        }, "shelter_upgrade", roomId);

        public string ShelterAssignCheck(string roomId, int workers)
        {
            string why = ShelterCommandCheck();
            if (why != null) return why;
            var room = Shelter.Room(roomId);
            if (room == null) return Loc.T("Böyle bir sığınak odası yok.");
            if (room.Level <= 0) return Loc.T("Önce odayı onar.");
            if (workers < 0 || workers > ShelterLogic.WorkerCapacity(room, B)) return Loc.T("Odanın personel kapasitesi yetersiz.");
            if (workers == room.Workers) return Loc.T("Personel görevlendirmesi zaten böyle.");
            if (workers - room.Workers > ShelterLogic.AvailableWorkers(Shelter)) return Loc.T("Boşta yeterli kişi yok. Önce başka bir odadan personel ayır.");
            return null;
        }

        public ActResult ShelterAssign(string roomId, int workers) => RunCommand(() =>
        {
            string why = ShelterAssignCheck(roomId, workers);
            if (why != null) return ActResult.Fail(why);
            Shelter.Room(roomId).Workers = workers;
            return ActResult.Success(Loc.F("{0}: {1} kişi görevlendirildi.", ShelterLogic.RoomName(roomId), workers));
        }, "shelter_assign", roomId, workers.ToString(CultureInfo.InvariantCulture));
    }
}
