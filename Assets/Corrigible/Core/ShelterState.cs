using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    /// <summary>Sığınak: baskının hazırlık yeri (odalar, erzak, malzeme, zırh plakaları, yedek uzmanlar).</summary>
    [Serializable]
    public sealed class ShelterState
    {
        /// <summary>Kişi, görev ve eşya sahipliğiyle vardiya planı; eski oda modeli kapalıyken asıl kaynaktır.</summary>
        public PlanningBoardState Plan;
        public double Supplies, Materials;
        public int Population;
        public bool Starving;
        public double LastSuppliesChange, LastMaterialsChange;
        public List<ShelterRoomState> Rooms = new List<ShelterRoomState>();
        /// <summary>Atölyenin döktüğü zırh plakaları; baskında takılır, kırılan düşer.</summary>
        public int Plates;
        /// <summary>Atölyenin plaka ve revirin yara sarma birikimi (vardiya vardiya dolar).</summary>
        public double PlateProgress, HealProgress;
        /// <summary>Yaşam alanının yedek uzmanları: düşen ya da yatan uzmanın yerine çıkar.</summary>
        public List<SpecialistState> Reserves = new List<SpecialistState>();

        public ShelterRoomState Room(string id) => Rooms.Find(r => r.Id == id);
    }

    [Serializable]
    public sealed class ShelterRoomState
    {
        public string Id;
        /// <summary>Sıfır: onarılmayı bekleyen oda; 1–3: kullanılabilir oda.</summary>
        public int Level, Workers;
    }

    public struct ShelterUpgradeCost
    {
        public double Fon, Materials;
    }
}
