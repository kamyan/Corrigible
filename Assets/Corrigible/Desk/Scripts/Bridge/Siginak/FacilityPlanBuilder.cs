using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Bina planının üç paftası (Docs/TESIS-BASKINLARI-TASARIMI.md §7.1; tesis aşama 5): görev fişinin açık olduğu bölgenin sıradaki
    /// kanadı (<see cref="Simulation.NextLiberationSector"/>). Bilinen odanın (<see cref="Simulation.FacilityKnown"/>) paftası planın
    /// kendisidir: yapı, sabit nesneler ve bugünkü kademede görünen şart parçaları (<see cref="Simulation.FacilityPlanCells"/>,
    /// <see cref="FacilityRoom.PartsAt"/>); bilinmeyen odada yalnız başlık ve "PLAN YOK" damgası. Yalnız <see cref="FacilityRoom"/> ve
    /// planın kareleri okunur: oda kurulumu, baskın tahtası ve baskın kayıtları okunmaz, bu yüzden robot, niyet, robot sayısı ve
    /// uyanma karesi veriye giremez, veri baskın tohumundan bağımsızdır. Saf okumadır: komut yazmaz, RNG çekmez. Tesis aşama 10: planın
    /// kaplamasındaki yapısal önlemler (çift cidar, koku yayıcı) kırmızı kalem işaretleridir (<see cref="FacilityPlanSheetData.Counters"/>).
    /// Tesis planı yokken (koddan kurulan simülasyonsuz çağrı), bölgede alınacak kanat yokken ya da plan kanadı bulamazsa null.
    /// Cephe dışındaki bölgenin bilgi fişinde sıradaki kanat yalnız en az bir odası biliniyorsa gösterilir
    /// (<see cref="FacilityPlanData.Front"/> false; plan bilgisi zaten oyuncunundur, emir yine yalnız cepheden verilir). Hiçbir odası
    /// bilinmeyen kanadın üç boş "PLAN YOK" paftası bilgi fişinde haritayı örtmekten başka iş görmez (cephe dışında bilgi kaynağı yok).
    /// </summary>
    public static class FacilityPlanBuilder
    {
        public static FacilityPlanData Build(Simulation sim, string regionId) => Build(sim, regionId, null);

        /// <summary>
        /// Tesis aşama 11 (§8.4): <paramref name="sectorId"/> verilirse o kanadın paftaları (bölgenin kanadı olmalı); verilmezse görev fişinin
        /// kuşatma kipindeki kanat (<see cref="FacilityDefenseText.SlipSector"/>: savunulacak, alınmış kanat), yoksa sıradaki kanat.
        /// </summary>
        public static FacilityPlanData Build(Simulation sim, string regionId, string sectorId)
        {
            var plan = sim?.FacilityPlan;
            if (plan == null || string.IsNullOrEmpty(regionId) || sim.S.Liberation == null) return null;
            var siege = FacilityDefenseText.SlipSector(sim, regionId);
            var next = sectorId != null ? sim.LiberationSector(sectorId) : siege ?? sim.NextLiberationSector(regionId);
            if (next != null && next.Region != regionId) next = null;
            var wing = next == null ? null : plan.Wing(next.Id);
            if (wing == null) return null;
            bool defended = siege != null && siege.Id == next.Id;
            var d = new FacilityPlanData
            {
                RegionId = regionId, SectorId = next.Id, Wing = next.Index + 1,
                Front = regionId == sim.S.Liberation.CurrentRegion || defended,
            };
            foreach (var room in wing) d.Sheets.Add(Sheet(sim, room, defended));
            return d.Front || d.Sheets.Any(s => s.Known) ? d : null;
        }

        /// <summary>Oda türünün paftadaki kısa adı.</summary>
        public static string TypeWord(RoomType type) => type switch
        {
            RoomType.Entry => Loc.T("GİRİŞ"),
            RoomType.Middle => Loc.T("ARA ODA"),
            _ => Loc.T("HEDEF"),
        };

        static FacilityPlanSheetData Sheet(Simulation sim, FacilityRoom room, bool defended = false)
        {
            var s = new FacilityPlanSheetData { RoomId = room.Id, Index = room.Index, Known = sim.FacilityKnown(room.Id) };
            s.Title = Loc.F("PAFTA {0}/3 · {1}", room.Index, TypeWord(room.Type));
            if (!s.Known)
            {
                s.Stamp = Loc.T("PLAN YOK");
                s.HoverTitle = Loc.F("PAFTA {0}/3 · {1}", room.Index, Loc.T("PLAN YOK"));
                s.HoverLine = Loc.T("Bu odanın planı bilinmiyor. Ekibin girdiği oda teybe kaydedilir; kanat alınınca sıradakinin giriş planı evraktan çıkar; Gözetim görevindeki kişi de her vardiya bir odanın planını çıkarır.");
                return s;
            }

            var cells = sim.FacilityPlanCells(room);
            // Şart parçaları planın kareleriyle aynı kurala uyar (Simulation.FacilityPlanCells): bölge olayları açık ve odada şart var
            bool events = sim.B.liberationRegionEvents && room.Event != null;
            int copies = 0, coolers = 0, civilians = 0, cables = 0;
            bool vent = false, shutter = false, forward = false;
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = cells?[y, x];
                    var t = new PlanTile { X = x, Y = y };
                    if (c != null)
                    {
                        t.Ground = c.Shutter && c.T == "wall" ? PlanGround.Shutter : c.T == "wall" ? PlanGround.Wall : c.T == "cable" ? PlanGround.Cable : PlanGround.Floor;
                        t.Phase = t.Ground == PlanGround.Cable ? c.Phase : 0;
                        t.Door = c.Door;
                        t.Vent = c.Vent;
                        t.Item = Item(c.O?.K);
                    }
                    switch (t.Item)
                    {
                        case PlanItem.Copy: copies++; break;
                        case PlanItem.Cooler: coolers++; break;
                        case PlanItem.Civilian: civilians++; break;
                    }
                    // Tesis aşama 10 (K41): yapısal önlemler planın kaplamasından (Simulation.FacilityPlanCells = emirdeki tahta)
                    if (c != null && c.T == "wall" && c.Double) s.Counters.Add(new PlanMark { Kind = "cidar", X = x, Y = y });
                    if (c?.O?.K == "yayici") s.Counters.Add(new PlanMark { Kind = "yayici", X = x, Y = y });
                    if (t.Ground == PlanGround.Cable) cables++;
                    if (t.Ground == PlanGround.Shutter) shutter = true;
                    vent |= t.Vent != 0;
                    forward |= t.Door == 2;
                    s.Tiles.Add(t);
                }
            // Tesis aşama 11 (§8.4, K39-6): kalıcı tahtanın izleri; yalnız plana uygulanmış olanlar (Core FacilityMarks.Apply'ın atladığı iz
            // planda da tahtada da yoktur: kareye bakılır, kural kopyalanmaz)
            var marks = sim.FacilityMarksOf(room.Id);
            var shown = new List<FacilityMark>();
            foreach (var m in marks)
            {
                if (m == null || m.X < 0 || m.Y < 0 || m.X >= Baskin.N || m.Y >= Baskin.N) continue;
                var c = cells?[m.Y, m.X];
                if (c == null || !Applied(c, m)) continue;
                shown.Add(m);
                if (FacilityMarks.IsObjectKind(m.Kind)) continue;
                s.Traces.Add(new PlanMark
                {
                    Kind = m.Kind, X = m.X, Y = m.Y, Hp = m.Hp, Max = m.Max,
                    Item = m.Kind == FacilityMarks.Device ? Item(room.Device) : Item(m.Kind),
                });
            }
            foreach (var m in shown.Where(x => FacilityMarks.IsObjectKind(x.Kind)))
                s.Traces.Add(new PlanMark { Kind = m.Kind, X = m.X, Y = m.Y, Hp = m.Hp, Max = m.Max, Item = Item(m.Kind) });
            if (events)
                foreach (var p in room.PartsAt(sim.Tier))
                    s.Marks.Add(new PlanMark
                    {
                        Kind = p.Kind, X = p.At.X, Y = p.At.Y, Dir = p.Dir,
                        ToX = p.To.HasValue ? p.To.Value.X : -1, ToY = p.To.HasValue ? p.To.Value.Y : -1,
                    });

            // Tesis aşama 12 (Katar): açık kenar satırları (Core Baskin.OpenEdgeRows; giriş vagonu şartsız)
            s.OpenEdges.AddRange(FacilityConditionText.PlanOpenRows(sim, room));

            string eventName = events && Baskin.RegionEvents.TryGetValue(room.Event, out var ev) ? Loc.T(ev.Name) : null;
            string device = room.Device != null && Baskin.ObjNames.TryGetValue(room.Device, out var dn) ? Loc.T(dn) : null;
            // Antet: oda adı; altında şartın adı (şart rengiyle) ya da donanım. Sözle tamamı üzerine gelme etiketinde
            s.Caption = room.Name;
            s.Note = eventName ?? device;
            s.NoteIsEvent = eventName != null;

            // Üzerine gelme: plan sözle (çizimin anahtarı)
            var lines = new List<string>();
            if (device != null) lines.Add(Loc.F("Donanım: {0}", device));
            if (s.Tiles.Any(t => t.Item == PlanItem.Target)) lines.Add(room.Mission == "loot" ? Loc.T("İkmal kasası") : Loc.T("Hedef dolap"));
            if (copies > 0) lines.Add(Loc.F("Kopya dolabı: {0}", copies));
            if (coolers > 0) lines.Add(Loc.F("Soğutma ünitesi: {0}", coolers));
            if (civilians > 0) lines.Add(Loc.F("Sivil sunucu: {0}", civilians));
            if (cables > 0) lines.Add(Loc.T("Kablo kanalı"));
            if (shutter) lines.Add(Loc.T("Ağır kepenk"));
            if (vent) lines.Add(Loc.T("Menfez"));
            if (forward) lines.Add(Loc.T("İleri geçit"));
            if (eventName != null) lines.Add(Loc.F("Olay: {0}", eventName));
            // Tesis aşama 12: şartın kuralı (Katar açık kenar, Yalpa yatış takvimi; odanın tur bütçesiyle)
            string condition = FacilityConditionText.PlanLine(sim, room);
            if (condition != null) lines.Add(condition);
            s.CounterRange = sim.B.facilityEmitterRange;
            lines.AddRange(FacilityCounterText.PlanLines(sim, s.Counters.Any(m => m.Kind == "cidar"), s.Counters.Any(m => m.Kind == "yayici")));
            // Tesis aşama 11: izler, YZ onarımı ve savunulan kanatta terk notu (hedef odası terk edilemez)
            string traceLine = FacilityDefenseText.PlanMarks(shown);
            if (traceLine != null) lines.Add(traceLine);
            lines.AddRange(FacilityDefenseText.PlanRepairs(sim, room));
            if (defended && room.Type != RoomType.Target && FacilityDefenseText.MarksOn(sim)) lines.Add(FacilityDefenseText.PlanAbandonNote);
            s.HoverTitle = Loc.F("PAFTA {0}/3 · {1}", room.Index, DeskText.Upper(room.Name));
            s.HoverLine = string.Join(" · ", lines);
            return s;
        }

        /// <summary>İz planın karesine uygulanmış mı (Core <c>FacilityMarks.Apply</c>'ın sonucu karede okunur).</summary>
        static bool Applied(Baskin.Cell c, FacilityMark m) => m.Kind switch
        {
            FacilityMarks.Wall => c.T == "floor" && !c.Shutter,
            FacilityMarks.Shutter => c.Shutter && c.T == "floor",
            FacilityMarks.Device => c.O == null,
            FacilityMarks.Cool or FacilityMarks.Spawner => c.O == null,
            _ => c.O?.K == m.Kind,
        };

        /// <summary>Planın nesnesi (Core nesne kimliği → pafta işareti). Hedef dolabın türü (çekirdek ya da düğüm) baskında belli olur.</summary>
        public static PlanItem Item(string kind) => kind switch
        {
            null => PlanItem.None,
            "core" or "node" => PlanItem.Target,
            "spawner" => PlanItem.Copy,
            "cool" => PlanItem.Cooler,
            "civ" => PlanItem.Civilian,
            "pano" => PlanItem.Panel,
            "anten" => PlanItem.Antenna,
            "vana" => PlanItem.Valve,
            "enkaz" => PlanItem.Debris,
            "tup" => PlanItem.Tank,
            "kol" => PlanItem.Arm,
            "tel" or "kalas" => PlanItem.Barrier,
            "hurda" => PlanItem.Scrap,
            _ => PlanItem.None,
        };
    }
}
