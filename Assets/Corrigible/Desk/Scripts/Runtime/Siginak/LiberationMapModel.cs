using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>Kurtuluş haritasında bölge durumu (K2 notu › durumlar). Seçili ve üzerinde hâlleri ayrı bayraktır.</summary>
    public enum MapRegionKind { Occupied, Front, Candidate, Free }

    /// <summary>Sektör damgası (K2 notu): GERİ ALINDI mavi ✓ · BASKINA HAZIR kırmızı ▸ · KİLİTLİ gri kesik kilit.</summary>
    public enum SectorMark { Retaken, Ready, Locked }

    /// <summary>Bir bölgenin haritadaki görünümü; simülasyondan bağlayıcı kurar, görünüm yalnız okur.</summary>
    [Serializable]
    public sealed class MapRegionState
    {
        public string Id, Name;
        public MapRegionKind Kind;
        /// <summary>Geri alınan sektör (0–3).</summary>
        public int Captured;
        /// <summary>Kalan işgal yüzdesi: 100 × kalan / 3.</summary>
        public double Occupation;
        /// <summary>Fişi açık bölge ya da onay bekleyen aday (sarı dolgu, etiket çipi).</summary>
        public bool Selected;
        /// <summary>Özgürleştiği vardiya (damgadaki V.xxx); özgür değilse −1.</summary>
        public int FreeShift = -1;
        /// <summary>Bu vardiyada özgürleşti: taze mürekkep damga ve silgi lekesi (K3 notu).</summary>
        public bool FreshStamp;
    }

    /// <summary>Ekip figürünün yeri ve hâli (Ekip-figuru.md › yeri oyun durumundan gelir). Kayda yazılmaz.</summary>
    [Serializable]
    public sealed class TeamFigureState
    {
        public string Name, Members;
        /// <summary>Figürün durduğu bölge; Teknisyen yatıp yedek de yokken sığınak bölgesi (S-07).</summary>
        public string Region;
        /// <summary>Ekip sahada (baskın): figür haritadan kalkar.</summary>
        public bool Hidden;
        public bool AtShelter;
        /// <summary>Yatık piyon sayısı (0–3): yatan uzmanlar.</summary>
        public int Down;
        /// <summary>Ekip sığınakta beklerken Teknisyenin kalkmasına kalan gün.</summary>
        public int Days;
        public bool Ready => !Hidden && Days <= 0;
    }

    public sealed class LiberationMapState
    {
        public readonly List<MapRegionState> Regions = new List<MapRegionState>();
        public string Front, Shelter;
        /// <summary>Cephe bölgesi henüz özgür değil: aday çizilmez, aynı anda tek cephe.</summary>
        public bool FrontOpen;
        public TeamFigureState Team = new TeamFigureState();

        public MapRegionState Find(string id)
        {
            foreach (var r in Regions) if (r.Id == id) return r;
            return null;
        }

        public bool IsCandidate(string id) => Find(id)?.Kind == MapRegionKind.Candidate;
    }

    /// <summary>Fişteki sektör satırı; metinler gösterilecek (çevrilmiş) hâlde.</summary>
    public sealed class SlipSector
    {
        public string No, Name, Kind;
        public SectorMark Mark;
        public bool Highlight;
    }

    /// <summary>Görev fişi (K2): cephe için tam form, öteki bölgeler için bilgi fişi (eylem yok).</summary>
    public sealed class OperationSlipData
    {
        public string RegionId, Code, Region, Progress;
        public readonly List<SlipSector> Sectors = new List<SlipSector>();
        public string TaskHead, Task, Cost, Reward;
        public string TeamName, TeamMembers, TeamStamp;
        public bool TeamReady;
        public bool HasAction, ActionEnabled;
        public string Action, ActionCost, Reason;
        /// <summary>Bilgi fişi metni (görev, bedel, ekip ve eylem yerine).</summary>
        public string Info;
    }

    /// <summary>Yeni cephe onay fişi (K3).</summary>
    public sealed class FrontSlipData
    {
        public string RegionId, Head, Region, Neighbor, Occupation, Rule, Action, Cancel, Reason;
        public readonly List<SlipSector> Sectors = new List<SlipSector>();
        public bool ActionEnabled;
    }
}
