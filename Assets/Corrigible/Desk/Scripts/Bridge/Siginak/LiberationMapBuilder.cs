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
                    Name = def.Name,
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
            if (sim.Board != null)
            {
                var squad = sim.PlanningSquad();
                bool complete = squad.Count == PlanningBoardData.FieldJobs.Length;
                return new TeamFigureState
                {
                    Name = Loc.F("EKİP {0}", DeskText.Upper(TeamName(team))),
                    Members = string.Join(" · ", squad.Select(p => Loc.F("{0} · {1}", p.PersonName, Baskin.UnitTypes[p.Type].Name))),
                    Region = complete ? front : Simulation.HQ, Hidden = sim.S.Raid != null, AtShelter = !complete,
                    Down = sim.Board.People.Count(p => p.Alive && p.Down > 0), Days = 0,
                };
            }
            // Teknisyensiz kadro baskına çıkamaz: ekip sığınakta bekler. Figürün üç piyonu varsayılan kadro (Teknisyen ve seçilecek ikisi)
            bool blocked = !sim.SquadCanRaid(team);
            var roles = new[] { "tek" }.Concat(Simulation.DefaultPick(team)).ToList();
            return new TeamFigureState
            {
                Name = Loc.F("EKİP {0}", DeskText.Upper(TeamName(team))),
                Members = string.Join(" · ", roles.Select(r => Baskin.UnitTypes[r].Name)),
                Region = blocked ? Simulation.HQ : front,
                Hidden = sim.S.Raid != null,
                AtShelter = blocked,
                Down = team.Members.Count(m => roles.Contains(m.Role) && m.Down > 0),
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
            if (full && def.Event != null && Baskin.RegionEvents.TryGetValue(def.Event, out var ev))
                return Loc.F("{0}: {1} Olay · {2}: {3} İmza makinesi {4}: {5}", Loc.T(trait.Name), Loc.T(trait.Desc), Loc.T(ev.Name), Loc.T(ev.Desc), enemy.Name, enemy.Desc);
            return full
                ? Loc.F("{0}: {1} İmza makinesi {2}: {3}", Loc.T(trait.Name), Loc.T(trait.Desc), enemy.Name, enemy.Desc)
                : Loc.F("Bölge: {0} · İmza: {1}", Loc.T(trait.Name), enemy.Name);
        }

        /// <summary>Teknisyenin kalkmasına kalan gün (ayaktaysa 0).</summary>
        public static int TechnicianDown(TeamState team) => team?.Members.FirstOrDefault(m => m.Role == "tek")?.Down ?? 0;

        /// <summary>
        /// Baskına çıkacak kadro, ekip diskleri olarak (S2): Teknisyen ve seçilen iki uzman (verilmezse varsayılan seçim), rol
        /// harfi (baskındaki 1–3 kısayolları, piyonla aynı) ve can; yedek uzman ve eksik rol işaretli. Plakalar disklerden sonra
        /// ayrı (“2 plaka”).
        /// </summary>
        public static List<SlipMember> SquadDiscs(Simulation sim, TeamState team, out string extra, IList<string> pick = null)
        {
            var roles = sim.Board != null ? (IList<string>)PlanningBoardData.FieldJobs : pick ?? Simulation.DefaultPick(team);
            var squad = sim.Squad(team, roles);
            var list = new List<SlipMember>();
            foreach (var role in Simulation.Roles.Where(r => r == "tek" || roles.Contains(r)))
            {
                var slot = squad.FirstOrDefault(x => x.Type == role);
                list.Add(new SlipMember
                {
                    Letter = Baskin.UnitTypes[role].Short,
                    Hp = slot == null ? "—" : Loc.F("{0}/{1}", slot.Hp, slot.MaxHp > 0 ? slot.MaxHp : Simulation.MaxHp(role)),
                    Rookie = slot != null && slot.Rookie,
                    Missing = slot == null,
                });
            }
            int plates = sim.Board != null ? squad.Count(p => p.Armor > 0) : Math.Min(sim.Shelter?.Plates ?? 0, squad.Count);
            extra = plates > 0 ? Loc.F("{0} plaka", plates) : null;
            return list;
        }

        static string N(double v) => DeskFormat.Number(v, 0);
        static string Kind(LiberationSectorState s) => s.MissionKind == "loot" ? Loc.T("YAĞMA") : Loc.T("SUNUCU YIKIMI");
        static SlipReward Reward(double amount, string unit) => new SlipReward { Amount = DeskFormat.Signed(amount, 0), Unit = unit };

        /// <summary>
        /// SAHA satırları (S2): bölge özelliği, bölge olayı, imza makinesi (kare çerçeveli işaretleriyle) ve tehdit kademesi.
        /// Üzerine gelince oyundaki açıklama. <paramref name="noEvent"/>: eğitim baskını olaysızdır (fişte de OLAY satırı yok).
        /// </summary>
        public static void Fields(Simulation sim, string regionId, List<SlipField> into, bool noEvent = false)
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
            if (!noEvent && sim.B.liberationRegionEvents && def.Event != null && Baskin.RegionEvents.TryGetValue(def.Event, out var ev))
            {
                string name = Loc.T(ev.Name);
                into.Add(new SlipField
                {
                    Glyph = "ic_bolt_s", Label = Loc.T("OLAY"), Value = name,
                    HoverTitle = Loc.F("OLAY · {0}", DeskText.Upper(name)), HoverLine = Loc.T(ev.Desc),
                });
            }
            if (def.Enemy != null && Baskin.UnitTypes.TryGetValue(def.Enemy, out var enemy))
            {
                string name = enemy.Name;
                into.Add(new SlipField
                {
                    Glyph = SiginakGlyphs.Machine(def.Enemy, framed: true), Label = Loc.T("İMZA"), Value = name,
                    HoverTitle = Loc.F("İMZA · {0}", DeskText.Upper(name)), HoverLine = enemy.Desc,
                });
            }
            into.Add(new SlipField
            {
                Tier = sim.Tier, Label = Loc.T("TEHDİT"), Value = Loc.F("{0}/4", sim.Tier),
                HoverTitle = Loc.F("TEHDİT · {0}/4", sim.Tier),
                HoverLine = Loc.T("Özgür bölge sayısıyla yükselir. 3. kademede makineler kopyalanır, 4. kademede güçlenir."),
            });
        }

        /// <summary>
        /// Kadro seçici: beş uzman kadro sırasıyla. Teknisyen sabit; yatan uzmanın yerine yedek çıkar (yedek yoksa rol boş kalır);
        /// henüz katılmamış uzman kilitli, yanında açılacağı atölye seviyesi (“SV 2”). <paramref name="fixedSquad"/> eğitim baskınında seçimi kilitler.
        /// </summary>
        static void Pick(Simulation sim, TeamState team, IList<string> pick, bool fixedSquad, OperationSlipData d)
        {
            if (sim.Board != null)
            {
                d.PickHead = Loc.T("KADRO · VARDİYA PANOSU");
                d.PickNote = Loc.T("Kişi, rol ve teçhizat dağılımını vardiya panosunda düzenle.");
                return;
            }
            int freeReserves = sim.Shelter?.Reserves.Count(r => r.Down <= 0) ?? 0;
            string next = null;
            int nextLevel = int.MaxValue;
            foreach (var role in Simulation.Roles)
            {
                var m = team.Members.FirstOrDefault(x => x.Role == role);
                var type = Baskin.UnitTypes[role];
                var ability = Baskin.Abilities[type.Ability];
                if (m == null)
                {
                    // Atölyenin seviyesiyle katılır: soluk disk, yanında seviye (“SV 2”), tıklanmaz; üzerine gelince ne zaman katılacağı
                    int need = Simulation.UnlockLevel(role, sim.B);
                    // Not en önce katılacak uzmanı söyler (seviyeler dengeden; kadro sırasına bakmaz)
                    if (need < nextLevel) { nextLevel = need; next = Loc.F("{0} atölye seviye {1} olunca kadroya katılır.", type.Name, need); }
                    d.Pick.Add(new SlipPick
                    {
                        Role = role, Letter = type.Short, Hp = Loc.F("SV {0}", need), Locked = true,
                        HoverTitle = Loc.F("{0} · KİLİTLİ", DeskText.Upper(type.Name)),
                        HoverLine = Loc.F("Atölye seviye {0} olunca kadroya katılır. {1}: {2}", need, ability.Name, ability.Desc),
                    });
                    continue;
                }
                int max = Simulation.MaxHp(role), hp = Math.Max(1, max - m.Wounds);
                bool down = m.Down > 0;
                // Durum çipin genişliğine sığar (≤ 7 harf; ayrıntısı üzerine gelince): yerine yedek çıkacaksa YEDEK, yoksa kalan gün
                string status = role == "tek" ? Loc.T("SABİT") : down ? (freeReserves > 0 ? Loc.T("YEDEK") : Loc.F("{0} GÜN", m.Down)) : null;
                d.Pick.Add(new SlipPick
                {
                    Role = role, Letter = type.Short, Hp = down ? "—" : Loc.F("{0}/{1}", hp, max), Status = status, Down = down,
                    Picked = role == "tek" || pick.Contains(role), Locked = role == "tek" || fixedSquad,
                    HoverTitle = Loc.F("{0} · {1}", DeskText.Upper(type.Name), down ? Loc.F("yatıyor · {0} gün", m.Down) : Loc.F("can {0}/{1}", hp, max)),
                    HoverLine = Loc.F("{0}: {1}", ability.Name, ability.Desc) + (down
                        ? " " + (freeReserves > 0 ? Loc.T("Seçilirse yerine bir eksik canla yedek uzman çıkar.") : Loc.T("Yedek yok: seçilirse kadro eksik çıkar."))
                        : ""),
                });
            }
            int picked = pick.Count;
            d.PickHead = Loc.T("KADRO");
            bool choice = team.Members.Count(m => m.Role != "tek") > Simulation.PickCount;
            d.PickNote = fixedSquad ? Loc.T("İlk baskın: kadro sabit.") : picked < Simulation.PickCount ? Loc.F("Teknisyen'in yanına {0} uzman daha seç.", Simulation.PickCount - picked)
                : !choice && next != null ? next
                : Loc.T("Teknisyen sabit · ikisini seç; baskına çıkmayanlar dinlenir.");
        }

        /// <summary>
        /// Görev fişi: cephede tam form, öteki bölgelerde bilgi fişi. <paramref name="pick"/> oyuncunun seçtiği uzmanlar (seçim
        /// sırasıyla; boşsa varsayılan seçim); <paramref name="fixedSquad"/> eğitim baskınında kadroyu kilitler.
        /// </summary>
        public static OperationSlipData Slip(Simulation sim, string regionId, IList<string> pick = null, bool fixedSquad = false)
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
            if (sim.Board != null) d.Code = Loc.F("KYGA · {0} · V.{1:000}", d.Code, sim.S.Day + 1);
            d.Region = DeskText.Upper(GameData.Region(regionId).Name);
            d.Building = GameData.Region(regionId).Building;
            // Bina satırının üzerine gelince binanın notu (bilgi fişinde de)
            d.BuildingHoverTitle = Loc.F("BİNA · {0}", DeskText.Upper(d.Building));
            d.BuildingHoverLine = GameData.Region(regionId).BuildingNote;
            d.Progress = area.Completed
                ? Loc.F("SEKTÖRLER {0}/{1} · ÖZGÜR", captured, count)
                : Loc.F("SEKTÖRLER {0}/{1} · İŞGAL {2}", captured, count, DeskFormat.Percent(occupation, captured > 0 ? 1 : 0));
            Fields(sim, regionId, d.Fields, fixedSquad);
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
                        ? Loc.F("Cephe açık: {0} {1}/{2}. Aynı anda tek cephe açık olabilir.", GameData.Region(lib.CurrentRegion).Name,
                            frontArea.CapturedCount, frontArea.Sectors.Count)
                        : Loc.T("Cephe açmak için komşusunu özgürleştir.");
                return d;
            }

            d.TaskHead = Loc.F("GÖREV · {0:00} {1}", next.Index + 1, DeskText.Upper(next.Name));
            d.Task = next.MissionKind == "loot"
                ? Loc.T("Teknisyeni ikmal kasasının yanına götür, yeteneğini seç ve kasaya tıklayarak malzemeleri al.")
                : Loc.T("Sunucu çekirdeğini yık. Son sektör alınınca bu bölge özgürleşir.");
            if (sim.Board != null) d.Task = next.MissionKind == "loot"
                ? Loc.T("İkmal kasasını yağmala; ekibi çıkışa ulaştır.")
                : Loc.T("Sunucu çekirdeğini yık; ekibi çıkışa ulaştır.");
            d.Cost = Loc.F("{0} fon", N(b.liberationRaidCost));
            bool last = captured == count - 1;
            // Ödül 2 × 2 (S2): yeşil miktar + birim
            if (sim.Board != null)
            {
                bool loot = next.MissionKind == "loot";
                d.Planning = true;
                d.Rewards.Add(Reward((loot ? b.liberationLootFunds : b.liberationServerFunds) + (last ? b.liberationCaptureFunds : 0), Loc.T("fon")));
                d.Rewards.Add(Reward(loot ? b.boardLootSupplies : 0, Loc.T("erzak")));
                var amounts = RaidPaperFacts.Nominal(sim, loot);
                for (int i = 0; i < amounts.Length; i++) d.Rewards.Add(Reward(amounts[i], PlanningBoardData.MaterialName(PlanningBoardData.MaterialKinds[i])));
            }
            else
            {
            if (next.MissionKind == "loot")
            {
                d.Rewards.Add(Reward(b.liberationLootFunds, Loc.T("fon")));
                d.Rewards.Add(Reward(sim.Board != null ? b.boardLootMetal + b.boardLootCircuits + b.boardLootCloth + b.boardLootChemicals : b.shelterLootMaterials, Loc.T("malzeme")));
                d.Rewards.Add(Reward(sim.Board != null ? b.boardLootSupplies : b.shelterLootSupplies, Loc.T("erzak")));
                if (sim.Board == null) d.Rewards.Add(Reward(b.shelterLootResidents, Loc.T("kurtulan")));
            }
            else
            {
                d.Rewards.Add(Reward(b.liberationServerFunds, Loc.T("fon")));
                d.Rewards.Add(Reward(sim.Board != null ? b.boardServerMetal + b.boardServerCircuits + b.boardServerCloth + b.boardServerChemicals : b.shelterServerMaterials, Loc.T("malzeme")));
                if (last) d.Rewards.Add(Reward(b.liberationCaptureFunds, Loc.T("fon · bölge özgür")));
            }

            }

            var team = sim.S.Teams.FirstOrDefault();
            d.TeamName = Loc.F("EKİP {0}", DeskText.Upper(TeamName(team)));
            var chosen = team == null ? new List<string>() : fixedSquad ? new List<string> { "emp", "ana" }
                : pick != null ? pick.Where(r => r != "tek").Distinct().ToList() : Simulation.DefaultPick(team);
            if (team != null)
            {
                d.Members.AddRange(SquadDiscs(sim, team, out var extra, chosen));
                d.TeamExtra = extra;
                Pick(sim, team, chosen, fixedSquad, d);
            }
            if (sim.Board != null)
            {
                d.TeamName = Loc.T("SAHA KADROSU"); d.TeamExtra = null; d.Members.Clear();
                d.Members.AddRange(RaidPaperFacts.Members(sim));
            }
            bool field = sim.S.Raid != null;
            bool back = team != null && !Simulation.TeamReady(team);
            d.TeamReady = team != null && !back && sim.SquadCanRaid(team);
            // Baskından dönen ekip sonraki vardiyada hazır; Teknisyen yatıp yedek de yoksa kalkana kadar bekler
            d.TeamStamp = field ? Loc.T("SAHADA") : d.TeamReady ? Loc.T("HAZIR")
                : back ? Loc.T("SONRAKİ VARDİYA") : Loc.F("TEKNİSYEN YARALI · {0} GÜN", TechnicianDown(team));
            if (sim.Board != null && !field && !back)
            {
                d.TeamReady = sim.PlanningSquad().Count == PlanningBoardData.FieldJobs.Length;
                d.TeamStamp = d.TeamReady ? Loc.T("HAZIR") : Loc.T("KADRO EKSİK · PANOYU DÜZENLE");
            }

            d.HasAction = true;
            d.Action = Loc.T("BASKIN EMRİ VER");
            d.ActionCost = Loc.F("{0} fon", DeskFormat.Signed(-b.liberationRaidCost, 0));
            bool short_ = sim.Board == null && chosen.Count < Simulation.PickCount;
            string why = short_ ? Loc.T("Teknisyen'in yanına iki uzman seç.") : sim.LiberationRaidCheck(next.Id, Simulation.PickKey(chosen));
            // Karar 7: eksik yalnız fonsa düğme açık kalır (basınca komut çağrılmaz, FON titrer); başka gerekçede devre dışı
            d.ActionState = why == null ? SlipActionState.Ready
                : !short_ && TelexReasons.RaidFundsOnly(sim, next.Id) ? SlipActionState.Shortfall : SlipActionState.Disabled;
            if (d.ActionState == SlipActionState.Shortfall)
                d.Shortfall = Loc.F("Fon yetersiz · {0} gerekli", N(b.liberationRaidCost));
            if (d.ActionState == SlipActionState.Disabled)
                d.ActionNote = field ? Loc.T("SAHADA") : team != null && !back && !sim.SquadCanRaid(team) ? Loc.T("TEKNİSYEN YARALI")
                    : short_ && !back ? Loc.T("KADRO EKSİK") : Loc.T("EKİP HAZIR DEĞİL");
            // Emir verildikten sonra gerekçe yazılmaz (damga konuşur)
            d.Reason = why == null || field ? null : short_ && !back ? why : TelexReasons.Raid(sim, next.Id);
            if (sim.Board != null && d.ActionState == SlipActionState.Disabled)
            {
                d.ActionNote = RaidPaperFacts.DisabledReason(sim);
                d.Reason = field ? null : why;
            }
            return d;
        }

        /// <summary>
        /// Kadro raporu (BASKIN EMRİ VER'den sonra, emir verilmeden): baskına çıkacak her uzman rol sırasıyla, emrin kendi
        /// kadrosundan (<see cref="Simulation.LiberationSquadPreview"/>; plakalar takılı). Durum: sağlam, tam iyileşmedi, yatıyor
        /// ve yerine yedek, yatıyor ve yedek yok; plaka takılacaksa eki. <paramref name="pick"/> ve <paramref name="fixedSquad"/>
        /// görev fişindeki gibi.
        /// </summary>
        public static SquadReportData SquadReport(Simulation sim, string regionId, IList<string> pick = null, bool fixedSquad = false)
        {
            var d = new SquadReportData { RegionId = regionId };
            var next = sim?.NextLiberationSector(regionId);
            if (next == null) return d;
            d.Head = Loc.T("KADRO RAPORU");
            if (sim.Board != null) d.Head = Loc.T("KYGA · KADRO RAPORU");
            d.Region = DeskText.Upper(GameData.Region(regionId).Name);
            d.Sector = Loc.F("{0:00} {1} · {2}", next.Index + 1, DeskText.Upper(next.Name), Kind(next));
            // Emrin göndereceği seçim (SiginakMap.ConfirmSquad): eğitim baskınında ve seçim yokken varsayılan
            var preview = sim.LiberationSquadPreview(fixedSquad || pick == null ? null : Simulation.PickKey(pick));
            var team = sim.S.Teams.FirstOrDefault(Simulation.TeamReady);
            foreach (var s in Baskin.SquadReport(preview))
            {
                var slot = preview.Squad?.FirstOrDefault(p => p.Type == s.Type);
                var person = sim.Board?.Person(slot?.PersonId);
                int days = sim.Board != null ? person?.Down ?? 0 : team?.Members.FirstOrDefault(m => m.Role == s.Type)?.Down ?? 0;
                string down = days > 0 ? Loc.F("yatıyor · {0} gün", days) : Loc.T("yatıyor");
                string status = s.Missing ? Loc.F("{0} · yedek yok, rol boş kalır", down)
                    : s.Rookie ? Loc.F("{0} · yerine yedek uzman, can {1}/{2}", down, s.Hp, s.Max)
                    : s.Wounded ? Loc.F("tam iyileşmedi · can {0}/{1}", s.Hp, s.Max)
                    : Loc.F("sağlam · can {0}/{1}", s.Hp, s.Max);
                if (s.Armor > 0) status = Loc.F("{0} · zırh plakası", status);
                var type = Baskin.UnitTypes[s.Type];
                if (sim.Board != null && s.Missing) status = Loc.T("Panoda uygun kişi ve rol kiti seçilmeli.");
                if (sim.Board != null && slot != null) status = Loc.F("{0} · hareket {1} · güç {2}", status, slot.Move, slot.Power);
                d.Rows.Add(new SquadReportRow { Letter = type.Short, Name = string.IsNullOrEmpty(person?.Name) ? type.Name : Loc.F("{0} · {1}", person.Name, type.Name), Status = status, Good = !s.Missing && !s.Rookie && !s.Wounded, Missing = s.Missing });
            }
            if (sim.Board != null)
            {
                d.Planning = true;
                d.Head = Loc.T("KYGA · KADRO RAPORU");
                d.Number = Loc.F("No. OP-{0:00} · V.{1:000}", next.Index + 1, sim.S.Day + 1);
                d.Warning = Loc.T("Geride kalan kişi kalıcı ölür; ustalığı ve eşyası kaybolur.");
                d.Rows.Clear();
                foreach (var m in RaidPaperFacts.Members(sim))
                {
                    var person = sim.Board.Person(m.PersonId);
                    bool good = person != null && person.Down <= 0 && person.Hp >= person.MaxHp;
                    d.Rows.Add(new SquadReportRow { Letter = m.Letter, Name = m.Name, PersonId = m.PersonId, Profession = m.Profession,
                        Stats = m.Stats, Slots = m.Slots, Health = m.Health, MaxHealth = m.MaxHealth, Hover = m.Hover,
                        Good = good, Missing = m.Missing, Status = m.Missing ? Loc.T("KADRO EKSİK") : person.Down > 0 ? Loc.T("YATIYOR") : good ? Loc.T("SAĞLAM") : Loc.T("YARALI") });
                }
            }
            // Odada klavye ipucu yok (Enter onaylar, Esc vazgeçer ama yazılmaz); düğmede fişteki gibi bedel
            d.Action = Loc.T("BASKINA ÇIK");
            d.ActionCost = Loc.F("{0} fon", DeskFormat.Signed(-sim.B.liberationRaidCost, 0));
            d.Cancel = Loc.T("Vazgeç");
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
                .Select(l => l.A == regionId ? l.B : l.A).Distinct().Select(id => GameData.Region(id).Name).ToList();
            d.Head = Loc.T("YENİ CEPHE · ONAY");
            d.Region = DeskText.Upper(GameData.Region(regionId).Name);
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
