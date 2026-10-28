using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>Masanın görünüm durumu: okunmuş baskın raporları.</summary>
    public sealed class DeskBridgeState
    {
        /// <summary>Okunmuş baskın raporu sayısı; fazlası rapor belgesi olarak açılır.</summary>
        public int RaidLogSeen;
        /// <summary>Son rapor bekletiliyor: harekâtı kapıda biten kararın kâğıdı henüz açık.</summary>
        public bool HoldEnding;
    }

    /// <summary>
    /// Simülasyondan masanın ekran kâğıtlarını kurar: okunmamış baskın raporu (<see cref="EventVM"/>) ve harekât bittiyse son
    /// rapor (<see cref="GameOverVM"/>). Oda ve sayaçlar simülasyonu doğrudan okur.
    /// </summary>
    public sealed partial class SimDeskBuilder
    {
        Simulation sim;
        WorldState S;
        DeskBridgeState st;

        static string N(double v, int d = 1) => DeskFormat.Number(v, d);
        static string Pct(double v, int d = 0) => DeskFormat.Percent(v, d);
        static string Day(int d) => DeskFormat.DayCode(d);
        static string Up(string s) => DeskText.Upper(s);
        static RegionDef Def(string id) => GameData.Region(id);

        /// <param name="raidScene">Taktik harita açıkken rapor beklemede kalır.</param>
        public DeskViewModel Build(Simulation simulation, DeskBridgeState state, bool raidScene)
        {
            sim = simulation; S = simulation.S; st = state;
            var vm = new DeskViewModel();
            if (S.Ending != null) { if (!st.HoldEnding) vm.GameOver = GameOver(); }
            else if (!raidScene && S.Raid == null && S.RaidLog.Count > st.RaidLogSeen) vm.Event = RaidReport(S.RaidLog[S.RaidLog.Count - 1]);
            return vm;
        }

        /// <summary>Bölgenin harita pikseli (yedek koordinat; kâğıt haritası bağlıysa spot bölgenin gerçek yerini okur).</summary>
        static Vector2 Center(string id) { var d = Def(id); return DeskMath.Project(d.Lon, d.Lat); }
        static float[] A(Vector2 v) => new[] { Mathf.Round(v.x * 10) / 10, Mathf.Round(v.y * 10) / 10 };
    }
}
