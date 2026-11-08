using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Yapılamayan emrin teleks gerekçesi (PROMPT §5 Aşama 4: “gerekçeyle, sayı içerir”). Komuttan önce simülasyonun kendi
    /// denetimi çağrılır; eksik kaynakta gerekli ve eldeki miktar yazılır (“malzeme yetersiz (8 gerekli / 5 var)”).
    /// Kural yeniden yazılmaz: karar yine <c>*Check</c> yöntemlerinindir, burada yalnız sayılar biçimlenir.
    /// </summary>
    public static class TelexReasons
    {
        static string N(double v, int decimals = 0) => DeskFormat.Number(v, decimals);

        public static string Upgrade(Simulation sim, string roomId)
        {
            string why = sim.ShelterUpgradeCheck(roomId);
            if (why == null) return null;
            var room = sim.Shelter.Room(roomId);
            string name = ShelterLogic.RoomName(roomId);
            string action = room != null && room.Level == 0 ? Loc.F("{0} onarımı", name) : Loc.F("{0} geliştirme", name);
            if (room != null && room.Level < sim.B.shelterMaxRoomLevel)
            {
                var cost = ShelterLogic.UpgradeCost(sim.Shelter, roomId, sim.B);
                if (sim.Shelter.Materials < cost.Materials)
                    return Loc.F("{0}: malzeme yetersiz ({1} gerekli / {2} var)", action, N(cost.Materials), N(System.Math.Floor(sim.Shelter.Materials)));
                if (sim.S.Fon < cost.Fon)
                    return Loc.F("{0}: fon yetersiz ({1} gerekli / {2} var)", action, N(cost.Fon), N(sim.S.Fon, 1));
            }
            return Loc.F("{0}: {1}", action, why);
        }

        public static string Assign(Simulation sim, string roomId, int workers)
        {
            string why = sim.ShelterAssignCheck(roomId, workers);
            return why == null ? null : Loc.F("{0} personeli: {1}", ShelterLogic.RoomName(roomId), why);
        }

        public static string Front(Simulation sim, string regionId)
        {
            string why = sim.SelectLiberationRegionCheck(regionId);
            if (why == null) return null;
            string name = string.IsNullOrEmpty(regionId) ? "—" : GameData.Region(regionId).Name;
            // K3 notu: cephe açıkken aday seçimi → “Avrasya: cephe zaten açık (Doğu Avrupa 2/3)”
            var lib = sim.S.Liberation;
            var front = lib != null ? sim.LiberationRegion(lib.CurrentRegion) : null;
            if (sim.S.Raid == null && front != null && !front.Completed && regionId != lib.CurrentRegion && sim.LiberationRegion(regionId) != null)
                return Loc.F("{0}: cephe zaten açık ({1} {2}/{3})", name, GameData.Region(lib.CurrentRegion).Name, front.CapturedCount, front.Sectors.Count);
            return Loc.F("{0}: {1}", name, why);
        }

        /// <summary>
        /// Ekip figürünü bırakma (Ekip-figuru.md › geçersiz): cephe açıkken yalnız kendi cephesi; cephe özgürken yalnız aday.
        /// Geçerliyse null. Karar simülasyonun sınır tanımıyla verilir (<c>LiberationFrontier</c>).
        /// </summary>
        public static string Drop(Simulation sim, string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return Loc.T("Figür bir bölgenin üstüne bırakılmalı.");
            var lib = sim.S.Liberation;
            var front = sim.LiberationRegion(lib.CurrentRegion);
            var area = sim.LiberationRegion(regionId);
            if (area == null) return Loc.T("Geçersiz bölge.");
            string name = GameData.Region(regionId).Name;
            if (front != null && !front.Completed)
                return regionId == lib.CurrentRegion ? null
                    : Loc.F("Cephe açık: {0} {1}/{2}", GameData.Region(lib.CurrentRegion).Name, front.CapturedCount, front.Sectors.Count);
            if (regionId == lib.CurrentRegion) return null;
            if (area.Completed) return Loc.F("{0}: bölge zaten özgür", name);
            if (!System.Linq.Enumerable.Contains(sim.LiberationFrontier, regionId)) return Loc.F("{0}: özgür bölgeye komşu değil", name);
            return null;
        }

        /// <summary>Figür alınabilir mi: Teknisyen yatıp yedek de yokken “Alfa: Teknisyen yaralı (1 gün)”; baskında sahada. Alınabiliyorsa null.</summary>
        public static string Pick(Simulation sim)
        {
            var team = System.Linq.Enumerable.FirstOrDefault(sim.S.Teams);
            if (team == null) return Loc.T("Ekip yok.");
            string name = LiberationMapBuilder.TeamName(team);
            if (sim.S.Raid != null) return Loc.F("{0} sahada", name);
            if (sim.Board != null)
            {
                string why = sim.PlanningRaidCheck();
                return why == null ? null : Loc.F("{0}: {1}", name, why);
            }
            if (!sim.SquadCanRaid(team)) return Loc.F("{0}: Teknisyen yaralı ({1} gün)", name, LiberationMapBuilder.TechnicianDown(team));
            return null;
        }

        public static string Raid(Simulation sim, string sectorId)
        {
            string why = sim.LiberationRaidCheck(sectorId);
            if (why == null) return null;
            if (RaidFundsOnly(sim, sectorId))
                return Loc.F("Baskın: fon yetersiz ({0} gerekli / {1} var)", N(sim.B.liberationRaidCost), N(sim.S.Fon, 1));
            if (sim.Board != null) return Loc.F("Baskın: {0}", why);
            var ready = System.Linq.Enumerable.FirstOrDefault(sim.S.Teams, Simulation.TeamReady);
            if (sim.S.Raid == null && ready != null && !sim.SquadCanRaid(ready))
                return Loc.F("{0}: Teknisyen yaralı ({1} gün), yedek uzman yok", LiberationMapBuilder.TeamName(ready), LiberationMapBuilder.TechnicianDown(ready));
            return Loc.F("Baskın: {0}", why);
        }

        /// <summary>
        /// Baskını tutan tek şey fon mu (sıra, hazır ekip, Teknisyen ya da yedeği tamam; fon bedelden az). Görev fişi bunu
        /// “yetersiz kaynak” olarak gösterir (Karar 7): düğme açık kalır, basınca komut çağrılmaz.
        /// </summary>
        public static bool RaidFundsOnly(Simulation sim, string sectorId)
        {
            if (sim.S.Ending != null || sim.S.Raid != null || sim.S.Fon >= sim.B.liberationRaidCost) return false;
            var sector = sim.LiberationSector(sectorId);
            var ready = System.Linq.Enumerable.FirstOrDefault(sim.S.Teams, Simulation.TeamReady);
            if (sim.Board != null && sim.PlanningRaidCheck() != null) return false;
            return sector != null && !sector.Captured && sector.Region == sim.S.Liberation.CurrentRegion &&
                   sim.NextLiberationSector(sector.Region)?.Id == sectorId && ready != null && sim.SquadCanRaid(ready);
        }
    }
}
