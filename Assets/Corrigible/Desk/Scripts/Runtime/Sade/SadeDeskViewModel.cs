using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Corrigible.Desk
{
    /// <summary>S-Masa oyun köprüsü paketi: HUD/harita görünümü + bölge kartı, tam dosya, çekmece ve ipucu JSON modelleri.</summary>
    [Serializable]
    public sealed class SadeDeskData
    {
        public SadeDeskViewModel View;
        public Dictionary<string, JObject> Cards = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> Dossiers = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> Drawers = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> NodeTooltips = new Dictionary<string, JObject>();
        public Dictionary<string, JObject> Breakdowns = new Dictionary<string, JObject>();
    }

    /// <summary>S-Masa durağan görünümü. Tüm sayılar ve güven düzeyleri mock kaynağının gösterilecek değerleridir.</summary>
    [Serializable]
    public sealed class SadeDeskViewModel
    {
        public ClockVM Clock;
        public List<SadeChipVM> Resources = new List<SadeChipVM>();
        public string GoalTitle;
        public List<SadeGoalVM> Goals = new List<SadeGoalVM>();
        public List<SadeChipVM> Risks = new List<SadeChipVM>();
        public SadeMapStateVM Map = new SadeMapStateVM();
        public SadeDockVM Dock;
        public bool LegendOpen;
        public KillSwitchVM KillSwitch;
    }

    /// <summary>Haritanın görünür durumu; geometri, projeksiyon veya simülasyon bağımlılığı içermez.</summary>
    [Serializable]
    public sealed class SadeMapStateVM
    {
        public List<RegionNodeVM> Regions = new List<RegionNodeVM>();
        public List<SadeMapThreadVM> SpreadThreads = new List<SadeMapThreadVM>();
        public List<SadeMapTeamVM> Teams = new List<SadeMapTeamVM>();
        public List<string> AlertRegions = new List<string>();
        public List<FieldReportVM> Reports = new List<FieldReportVM>();
        public string AdvisorTooltip;
    }

    [Serializable] public sealed class SadeMapThreadVM { public string From, To; }

    [Serializable]
    public sealed class SadeMapTeamVM
    {
        public string Id, Region, City, Status, To;
        public float EtaDays;
        public bool HasPoint;
        public float X, Y, FromX, FromY, ToX, ToY;
    }
}
