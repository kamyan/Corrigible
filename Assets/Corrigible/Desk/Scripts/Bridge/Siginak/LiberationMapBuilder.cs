using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Simülasyondan kurtuluş haritasının görünüm durumunu ve fiş içeriklerini kurar (Docs/UI_Siginak Aşama 5). Yalnız
    /// okur; kurallar simülasyonundur (<c>LiberationFrontier</c>, <c>*Check</c>). Durumlar: özgür = bütün sektörler
    /// alındı · cephe = <c>CurrentRegion</c> ve özgür değil · aday = cephe özgürken sınırdaki bölge · işgal = geri kalan.
    /// Ekip figürünün yeri de buradan türetilir (kayda yazılmaz): baskında kalkık, Teknisyen yatıp yedek de yokken S-07,
    /// yoksa cephede; yatan her uzmanın piyonu yatık.
    /// </summary>
    public static class LiberationMapBuilder
    {
        /// <summary>Ekip üyeleri (Ekip-figuru.md › piyonlar): mavi teknisyen, kehribar EMP uzmanı, yeşil analist.</summary>
        public static readonly string[] Members = { Loc.N("Teknisyen"), Loc.N("EMP uzmanı"), Loc.N("Analist") };

        public static LiberationMapState Build(Simulation sim, string selected = null, string pendingCandidate = null)
        {
            var s = new LiberationMapState { Shelter = Simulation.HQ };
            if (sim == null) return s;
            var lib = sim.S.Liberation;
            s.Tier = sim.Tier;
            s.Front = lib.CurrentRegion;
            var current = sim.LiberationRegion(lib.CurrentRegion);
            s.FrontOpen = current != null && !current.Completed;
            var candidates = s.FrontOpen ? new HashSet<string>() : new HashSet<string>(sim.LiberationFrontier);
            foreach (var def in GameData.Regions)
            {
                var area = sim.LiberationRegion(def.Id);
                if (area == null) continue;
                int freedDay = area.Completed ? area.Sectors.Max(x => x.CapturedDay) : -1;
                s.Regions.Add(new MapRegionState
                {
                    Id = def.Id,
                    Name = Loc.T(def.Name),
                    Kind = area.Completed ? MapRegionKind.Free
                        : def.Id == s.Front && s.FrontOpen ? MapRegionKind.Front
                        : candidates.Contains(def.Id) ? MapRegionKind.Candidate : MapRegionKind.Occupied,
                    Captured = area.CapturedCount,
                    Occupation = Occupation(area),
                    Selected = def.Id == selected || def.Id == pendingCandidate,
                    // Vardiya numarası gün + 1 (ilk vardiya 001)
                    FreeShift = area.Completed ? freedDay + 1 : -1,
                    FreshStamp = area.Completed && freedDay >= sim.S.Day,
                    Trait = def.Trait,
                    Signature = def.Enemy,
                    Reoccupied = Reoccupied(sim, area),
                });
            }
            s.Team = Team(sim, s.Front);
            return s;
        }

        static double Occupation(LiberationRegionState area) =>
            100.0 * (area.Sectors.Count - area.CapturedCount) / Math.Max(1, area.Sectors.Count);

        /// <summary>Son karşı saldırının yeniden işgal ettiği sektör hâlâ alınmamışsa sırası; yoksa −1.</summary>
        static int Reoccupied(Simulation sim, LiberationRegionState area)
        {
            string id = sim.S.LastCounterattack;
            if (string.IsNullOrEmpty(id)) return -1;
            var sector = area.Sectors.FirstOrDefault(x => x.Id == id);
            return sector != null && !sector.Captured ? sector.Index : -1;
        }

        /// <summary>Ekip adı gösterilen dilde (kayıttaki ad oluşturulduğu dilde kalır).</summary>
        public static string TeamName(TeamState team) => team == null ? "—" : TeamName(team.Id, team.Name);

        /// <summary>Kimlikten ad (vardiya raporu ekibi yalnız kimlik ve kayıttaki adla saklar).</summary>
        public static string TeamName(string id, string name)
        {
            foreach (var n in Simulation.TeamNames) if (n.ToLowerInvariant() == id) return Loc.T(n);
            return string.IsNullOrEmpty(name) ? "—" : name;
        }

        static TeamFigureState Team(Simulation sim, string front)
        {
            var team = sim.S.Teams.FirstOrDefault();
            if (team == null) return new TeamFigureState { Hidden = true };
            // Teknisyensiz kadro baskına çıkamaz: ekip sığınakta bekler
            bool blocked = !sim.SquadCanRaid(team);
            return new TeamFigureState
            {
                Name = Loc.F("EKİP {0}", DeskText.Upper(TeamName(team))),
                Members = string.Join(" · ", Members.Select(Loc.T)),
                Region = blocked ? Simulation.HQ : front,
                Hidden = sim.S.Raid != null,
                AtShelter = blocked,
                Down = team.Members.Count(m => m.Down > 0),
                Days = blocked ? TechnicianDown(team) : 0,
            };
        }

        /// <summary>
        /// Bölgenin kimliği: kısa (“Bölge: Kısa nöbet · İmza: Havan robotu”) ya da açıklamalı (özelliğin ve imza makinesinin
        /// ne yaptığı).
        /// </summary>
        public static string Identity(string regionId, bool full)
        {
            var def = GameData.Region(regionId);
            if (def.Trait == null || !Baskin.RegionTraits.TryGetValue(def.Trait, out var trait) || def.Enemy == null) return "";
            var enemy = Baskin.UnitTypes[def.Enemy];
            return full
                ? Loc.F("{0}: {1} İmza makinesi {2}: {3}", Loc.T(trait.Name), Loc.T(trait.Desc), enemy.Name, enemy.Desc)
                : Loc.F("Bölge: {0} · İmza: {1}", Loc.T(trait.Name), enemy.Name);
        }

        /// <summary>Teknisyenin kalkmasına kalan gün (ayaktaysa 0).</summary>
        public static int TechnicianDown(TeamState team) => team?.Members.FirstOrDefault(m => m.Role == "tek")?.Down ?? 0;

        /// <summary>
        /// Baskına çıkacak kadro, ekip diskleri olarak (S2): rol harfi (baskındaki 1–3 kısayolları, piyonla aynı) ve can;
        /// yedek uzman ve eksik rol işaretli. Plakalar disklerden sonra ayrı (“2 plaka”).
        /// </summary>
        public static List<SlipMember> SquadDiscs(Simulation sim, TeamState team, out string extra)
        {
            var squad = sim.Squad(team);
            var list = new List<SlipMember>();
            foreach (var role in Simulation.Roles)
            {
                var slot = squad.FirstOrDefault(x => x.Type == role);
                list.Add(new SlipMember
                {
                    Letter = Baskin.UnitTypes[role].Short,
                    Hp = slot == null ? "—" : Loc.F("{0}/{1}", slot.Hp, Simulation.MaxHp(role)),
                    Rookie = slot != null && slot.Rookie,
                    Missing = slot == null,
                });
            }
            int plates = Math.Min(sim.Shelter?.Plates ?? 0, squad.Count);
            extra = plates > 0 ? Loc.F("{0} plaka", plates) : null;
            return list;
        }

        static string N(double v) => DeskFormat.Number(v, 0);
        static string Kind(LiberationSectorState s) => s.MissionKind == "loot" ? Loc.T("YAĞMA") : Loc.T("SUNUCU YIKIMI");
        static SlipReward Reward(double amount, string unit) => new SlipReward { Amount = DeskFormat.Signed(amount, 0), Unit = unit };

        /// <summary>
        /// SAHA satırları (S2): bölge özelliği, imza makinesi (kare çerçeveli işaretleriyle) ve tehdit kademesi. Üzerine gelince
        /// oyundaki açıklama.
        /// </summary>
        public static void Fields(Simulation sim, string regionId, List<SlipField> into)
        {
            var def = GameData.Region(regionId);
            if (def.Trait != null && Baskin.RegionTraits.TryGetValue(def.Trait, out var trait))
            {
                string name = Loc.T(trait.Name);
                into.Add(new SlipField
                {
                    Glyph = SiginakGlyphs.Trait(def.Trait, framed: true), Label = Loc.T("BÖLGE"), Value = name,
                    HoverTitle = Loc.F("BÖLGE · {0}", DeskText.Upper(name)), HoverLine = Loc.T(trait.Desc),
                });
            }
            if (def.Enemy != null && Baskin.UnitTypes.TryGetValue(def.Enemy, out var enemy))
            {
                string name = Loc.T(enemy.Name);
                into.Add(new SlipField
                {
                    Glyph = SiginakGlyphs.Machine(def.Enemy, framed: true), Label = Loc.T("İMZA"), Value = name,
                    HoverTitle = Loc.F("İMZA · {0}", DeskText.Upper(name)), HoverLine = Loc.T(enemy.Desc),
                });
            }
            into.Add(new SlipField
            {
                Tier = sim.Tier, Label = Loc.T("TEHDİT"), Value = Loc.F("{0}/4", sim.Tier),
                HoverTitle = Loc.F("TEHDİT · {0}/4", sim.Tier),
                HoverLine = Loc.T("Özgür bölge sayısıyla yükselir. 3. kademede makineler kopyalanır, 4. kademede güçlenir."),
            });
        }

        /// <summary>Görev fişi: cephede tam form, öteki bölgelerde bilgi fişi.</summary>
        public static OperationSlipData Slip(Simulation sim, string regionId)
        {
            var d = new OperationSlipData { RegionId = regionId };
            var area = sim?.LiberationRegion(regionId);
            if (area == null) return d;
            var lib = sim.S.Liberation;
            var b = sim.B;
            bool front = regionId == lib.CurrentRegion && !area.Completed;
            var next = sim.NextLiberationSector(regionId);
            int captured = area.CapturedCount, count = area.Sectors.Count;
            double occupation = Occupation(area);
            int reoccupied = Reoccupied(sim, area);
            d.Code = front && next != null ? Loc.F("HAREKÂT FORMU · OP-{0:00}", next.Index + 1) : Loc.T("HAREKÂT FORMU");
            d.Region = DeskText.Upper(Loc.T(GameData.Region(regionId).Name));
            d.Progress = area.Completed
                ? Loc.F("SEKTÖRLER {0}/{1} · ÖZGÜR", captured, count)
                : Loc.F("SEKTÖRLER {0}/{1} · İŞGAL {2}", captured, count, DeskFormat.Percent(occupation, captured > 0 ? 1 : 0));
            Fields(sim, regionId, d.Fields);
            foreach (var s in area.Sectors)
            {
                // Karar 3: cephedeki ilk alınmamış sektör fon ya da ekip eksik olsa da BASKINA HAZIR; düğme gerekçeyle kapalı
                var mark = s.Captured ? SectorMark.Retaken : front && next == s ? SectorMark.Ready : SectorMark.Locked;
                d.Sectors.Add(new SlipSector
                {
                    No = (s.Index + 1).ToString("00"), Name = s.Name, Kind = Kind(s), Mark = mark, Highlight = mark == SectorMark.Ready,
                    Reoccupied = s.Index == reoccupied,
                    Hover = mark != SectorMark.Locked ? null : front ? Loc.T("Önceki sektör alınınca açılır.") : Loc.T("Bölge cephe olunca sırayla açılır."),
                });
            }
            if (!front || next == null)
            {
                bool frontOpen = !sim.LiberationRegion(lib.CurrentRegion).Completed;
                var frontArea = sim.LiberationRegion(lib.CurrentRegion);
                d.Info = area.Completed
                    ? (regionId == lib.CurrentRegion && sim.LiberationFrontier.Any()
                        ? Loc.T("Bölge özgür. Haritadan sınırdaki bir bölgeyi seç ve yeni cepheyi aç.")
                        : Loc.T("Bölge özgür."))
                    : frontOpen && sim.LiberationFrontier.Contains(regionId)
                        ? Loc.F("Cephe açık: {0} {1}/{2}. Aynı anda tek cephe açık olabilir.", Loc.T(GameData.Region(lib.CurrentRegion).Name),
                            frontArea.CapturedCount, frontArea.Sectors.Count)
                        : Loc.T("Cephe açmak için komşusunu özgürleştir.");
                return d;
            }

            d.TaskHead = Loc.F("GÖREV · {0:00} {1}", next.Index + 1, DeskText.Upper(next.Name));
            d.Task = next.MissionKind == "loot"
                ? Loc.T("Teknisyeni ikmal kasasının yanına götür. Q ile yeteneği seç ve kasaya tıklayarak malzemeleri al.")
                : Loc.T("Sunucu çekirdeğini yık. Son sektör alınınca bu bölge özgürleşir.");
            d.Cost = Loc.F("{0} fon", N(b.liberationRaidCost));
            bool last = captured == count - 1;
            // Ödül 2 × 2 (S2): yeşil miktar + birim
            if (next.MissionKind == "loot")
            {
                d.Rewards.Add(Reward(b.liberationLootFunds, Loc.T("fon")));
                d.Rewards.Add(Reward(b.shelterLootMaterials, Loc.T("malzeme")));
                d.Rewards.Add(Reward(b.shelterLootSupplies, Loc.T("erzak")));
                d.Rewards.Add(Reward(b.shelterLootResidents, Loc.T("kurtulan")));
            }
            else
            {
                d.Rewards.Add(Reward(b.liberationServerFunds, Loc.T("fon")));
                d.Rewards.Add(Reward(b.shelterServerMaterials, Loc.T("malzeme")));
                if (last) d.Rewards.Add(Reward(b.liberationCaptureFunds, Loc.T("fon · bölge özgür")));
            }

            var team = sim.S.Teams.FirstOrDefault();
            d.TeamName = Loc.F("EKİP {0}", DeskText.Upper(TeamName(team)));
            if (team != null)
            {
                d.Members.AddRange(SquadDiscs(sim, team, out var extra));
                d.TeamExtra = extra;
            }
            bool field = sim.S.Raid != null;
            bool back = team != null && !Simulation.TeamReady(team);
            d.TeamReady = team != null && !back && sim.SquadCanRaid(team);
            // Baskından dönen ekip sonraki vardiyada hazır; Teknisyen yatıp yedek de yoksa kalkana kadar bekler
            d.TeamStamp = field ? Loc.T("SAHADA") : d.TeamReady ? Loc.T("HAZIR")
                : back ? Loc.T("SONRAKİ VARDİYA") : Loc.F("TEKNİSYEN YARALI · {0} GÜN", TechnicianDown(team));

            d.HasAction = true;
            d.Action = Loc.T("BASKIN EMRİ VER");
            d.ActionCost = Loc.F("{0} fon", DeskFormat.Signed(-b.liberationRaidCost, 0));
            string why = sim.LiberationRaidCheck(next.Id);
            // Karar 7: eksik yalnız fonsa düğme açık kalır (basınca komut çağrılmaz, FON titrer); başka gerekçede devre dışı
            d.ActionState = why == null ? SlipActionState.Ready
                : TelexReasons.RaidFundsOnly(sim, next.Id) ? SlipActionState.Shortfall : SlipActionState.Disabled;
            if (d.ActionState == SlipActionState.Shortfall)
                d.Shortfall = Loc.F("Fon yetersiz · {0} gerekli", N(b.liberationRaidCost));
            if (d.ActionState == SlipActionState.Disabled)
                d.ActionNote = field ? Loc.T("SAHADA") : team != null && !back && !sim.SquadCanRaid(team) ? Loc.T("TEKNİSYEN YARALI")
                    : Loc.T("EKİP HAZIR DEĞİL");
            // Emir verildikten sonra gerekçe yazılmaz (damga konuşur)
            d.Reason = why == null || field ? null : TelexReasons.Raid(sim, next.Id);
            return d;
        }

        /// <summary>Yeni cephe onay fişi (yalnız aday için açılır).</summary>
        public static FrontSlipData Front(Simulation sim, string regionId)
        {
            var d = new FrontSlipData { RegionId = regionId };
            var area = sim?.LiberationRegion(regionId);
            if (area == null) return d;
            var cleared = new HashSet<string>(sim.S.Liberation.Regions.Where(r => r.Completed).Select(r => r.Id));
            var neighbors = GameData.Links
                .Where(l => l.A == regionId && cleared.Contains(l.B) || l.B == regionId && cleared.Contains(l.A))
                .Select(l => l.A == regionId ? l.B : l.A).Distinct().Select(id => Loc.T(GameData.Region(id).Name)).ToList();
            d.Head = Loc.T("YENİ CEPHE · ONAY");
            d.Region = DeskText.Upper(Loc.T(GameData.Region(regionId).Name));
            d.Neighbor = neighbors.Count > 0 ? Loc.F("Komşu: {0}", string.Join(", ", neighbors)) : "";
            d.Occupation = Loc.F("İŞGAL {0}", DeskFormat.Percent(Occupation(area)));
            foreach (var s in area.Sectors)
                d.Sectors.Add(new SlipSector { No = (s.Index + 1).ToString("00"), Name = s.Name, Kind = Kind(s), Mark = SectorMark.Locked });
            d.Rule = Loc.F("{0} Tehdit {1}/4. Cephe açılınca 01 {2} baskına hazır olur.", Identity(regionId, true), sim.Tier, area.Sectors[0].Name);
            d.Action = Loc.T("SEÇİLİ BÖLGEDE CEPHE AÇ");
            d.Cancel = Loc.T("Vazgeç");
            d.ActionEnabled = sim.SelectLiberationRegionCheck(regionId) == null;
            d.Reason = d.ActionEnabled ? null : TelexReasons.Front(sim, regionId);
            return d;
        }
    }
}
