using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    /// <summary>Özgürleştirme kampanyasının sığınak durumu; eski oyunlarda oluşturulmaz.</summary>
    [Serializable]
    public sealed class ShelterState
    {
        public double Supplies, Materials;
        public int Population;
        public bool Starving;
        public double LastSuppliesChange, LastMaterialsChange;
        public List<ShelterRoomState> Rooms = new List<ShelterRoomState>();

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
