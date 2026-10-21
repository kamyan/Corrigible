using System;
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
            return null;
        }

        public string ShelterUpgradeCheck(string roomId)
        {
            string why = ShelterCommandCheck();
            if (why != null) return why;
            var room = Shelter.Room(roomId);
            if (room == null) return Loc.T("Böyle bir sığınak odası yok.");
            if (room.Level >= B.shelterMaxRoomLevel) return Loc.T("Oda en yüksek seviyede.");
            var cost = ShelterLogic.UpgradeCost(Shelter, roomId, B);
            if (S.Fon < cost.Fon || Shelter.Materials < cost.Materials)
                return Loc.F("{0} fon ve {1} malzeme gerekiyor.", cost.Fon, cost.Materials);
            if ((roomId == ShelterLogic.Workshop || roomId == ShelterLogic.Infirmary) &&
                ShelterLogic.EnergyDemand(Shelter, B) + B.shelterRoomEnergy > ShelterLogic.EnergyCapacity(Shelter, B))
                return Loc.T("Enerji yetersiz. Önce jeneratörü geliştir veya oraya personel görevlendir.");
            return null;
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
            return ActResult.Success(Loc.F("{0} hazır: seviye {1}.", ShelterLogic.RoomName(roomId), room.Level));
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
            if (roomId == ShelterLogic.Generator &&
                ShelterLogic.EnergyCapacity(Shelter, B) + (workers - room.Workers) * B.shelterWorkerEnergy < ShelterLogic.EnergyDemand(Shelter, B))
                return Loc.T("Bu görevlendirme çalışan odaların enerjisini keser.");
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
