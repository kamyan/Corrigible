using Corrigible.Core;
using Corrigible.Game;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Harekâtsız örneklerin (editör araçları, ekran yakalama setleri, tek başına açılan baskın sahnesi) bölge adı. Harekât
    /// varsa ondan okunur (<see cref="Simulation.RegionName"/>), yoksa paketin YZ adı (<see cref="RegionDef.Ai"/>); aşama 3'ten
    /// beri iki harita kipinde de aynıdır, görüntüler her koşuda aynı kalır. Yalnız sunum; simülasyonu değiştirmez.
    /// </summary>
    public static class SampleMap
    {
        /// <summary>Örneklerin harekât tohumu (Rev3CaptureSet ve sunum testleriyle aynı).</summary>
        public const uint Seed = 79;

        /// <summary>Denge asset'inin yolu (oyunun <c>GameController</c>'ı bunu kullanır).</summary>
        public const string BalancePath = "Assets/Corrigible/Settings/BalanceConfig.asset";

        /// <summary>
        /// Denge asset'inde üretilmiş harita açık mı (<c>liberationMapGen</c> = 1). Yalnız editörde okunur (harekâtsız örnekler
        /// editör araçlarıdır); oyunda false.
        /// </summary>
        public static bool ConfiguredGenerated
        {
            get
            {
#if UNITY_EDITOR
                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
                return config != null && config.balance != null && config.balance.liberationMapGen == 1;
#else
                return false;
#endif
            }
        }

        /// <summary>Örnek bölge adı, denge asset'indeki anahtara göre (<see cref="ConfiguredGenerated"/>).</summary>
        public static string RegionName(Simulation sim, string id) => RegionName(sim, id, ConfiguredGenerated);

        /// <summary>
        /// <paramref name="generated"/>: <c>Balance.liberationMapGen</c> = 1 (örnekte harekât yoksa çağıran bilir). Aşama 3'ten beri
        /// bölgenin görünen adı iki haritada da YZ adıdır (<see cref="RegionDef.Ai"/>); parametre çağıranların imzası için kalır.
        /// </summary>
        public static string RegionName(Simulation sim, string id, bool generated)
        {
            if (sim != null) return sim.RegionName(id);
            return GameData.Region(id).Ai;
        }
    }
}
