using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis aşama 11 (Docs/TESIS-BASKINLARI-TASARIMI.md §9.1–9.3; aşama 11 tasarımı §8.1–8.6): bekleyen karşı saldırının (kuşatma),
    /// SAVUN/BIRAK kararının, savunmanın sonucunun ve kalıcı tahtanın (izler, YZ onarımı) masadaki metinleri. Teleks satırları, görev
    /// fişinin kuşatma kipi (SAHA satırları DALGA, İZ, RÖLE), CRT ipucu, telsiz günlüğü, devir fişinin uyarısı, ön kontrol notu, alarm
    /// panosunun açıklaması ve bina planının iz satırları. Kaynak yalnız Core'un public okumalarıdır (<see cref="Simulation.DefenseOn"/>,
    /// <see cref="Simulation.MarksOn"/>, <see cref="Simulation.PendingSiege"/>, <see cref="Simulation.SiegeDecisionDue"/>,
    /// <see cref="Simulation.SiegeSector"/>, <see cref="Simulation.DefendSectorCheck"/>, <see cref="Simulation.YieldSectorCheck"/>,
    /// <see cref="Simulation.WingMarks"/>, <see cref="Simulation.DefenseWaveSize"/>, <see cref="Simulation.DefenseRelayHpFor"/>,
    /// <see cref="Simulation.DefenseSchedulePreview"/>, <see cref="FacilityState.Sieges"/>, <see cref="FacilityState.Repairs"/>,
    /// <see cref="RaidLogEntry.Mode"/>): kural kopyalanmaz (K40). Saf: durum değiştirmez, RNG çekmez, komut yazmaz. Savunma alt bayrağı ya
    /// da tesis durumu yokken (<see cref="On"/> false) hiçbir yüzey metin üretmez; masa aşama 10'daki gibidir.
    /// </summary>
    public static class FacilityDefenseText
    {
        /// <summary>Kuşatma ve savunma işler (savunma alt bayrağı ve tesis durumu).</summary>
        public static bool On(Simulation sim) => sim?.B != null && sim.S?.Liberation != null && sim.DefenseOn;

        /// <summary>Kalıcı tahta (izler, onarım) işler.</summary>
        public static bool MarksOn(Simulation sim) => sim?.B != null && sim.S?.Liberation != null && sim.MarksOn;

        /// <summary>Günlük girdisi bir savunmanın mı (<see cref="RaidLogEntry.Mode"/>; saldırı baskınında boş).</summary>
        public static bool IsDefense(RaidLogEntry log) => log != null && log.Mode == Simulation.DefenseMode;

        /// <summary>Süren baskın bir savunma mı.</summary>
        public static bool Defending(Simulation sim) => sim?.S?.Raid != null && Simulation.IsDefense(sim.S.Raid.Params);

        /// <summary>"Bölge · Sektör" (sektör bulunamazsa "—").</summary>
        public static string Place(Simulation sim, string sectorId)
        {
            var s = string.IsNullOrEmpty(sectorId) ? null : sim?.LiberationSector(sectorId);
            return s == null ? "—" : Loc.F("{0} · {1}", sim.RegionName(s.Region), s.Name);
        }

        /// <summary>Komutla ya da kendiliğinden bırakılan kanadın güven bedeli (dengeden; Core'un FallSector'ı bunu düşer).</summary>
        public static int YieldTrust(Simulation sim) => Math.Max(0, sim?.B?.liberationCounterattackTrust ?? 0);

        // ------------------------------------------------------------------ teleks

        /// <summary>Kuşatma kurulduğunda teleksin durumu ve vardiya raporu kutusunun başlığı: "KARŞI SALDIRI BEKLİYOR" (Alarm).</summary>
        public static string PendingStatus => Loc.T("KARŞI SALDIRI BEKLİYOR");

        /// <summary>Karar verilmeyen kuşatmada kanat düştüğünde teleksin durumu: bugünkü "YZ KARŞI SALDIRISI".</summary>
        public static string FallStatus => Loc.T("YZ KARŞI SALDIRISI");

        /// <summary>"{bölge} · {sektör} kuşatıldı · sonraki vardiya savun ya da bırak" (teleks ve kutu).</summary>
        public static string PendingLine(Simulation sim, string sectorId) =>
            Loc.F("{0} kuşatıldı · sonraki vardiya savun ya da bırak", Place(sim, sectorId));

        /// <summary>Kendiliğinden bırakmanın teleks satırı: "{yer} · kanat bırakıldı (karar verilmedi) · sivil güven −{n}".</summary>
        public static string AutoYieldLine(Simulation sim, string sectorId) =>
            Loc.F("{0} · kanat bırakıldı (karar verilmedi) · sivil güven −{1}", Place(sim, sectorId), YieldTrust(sim));

        /// <summary>SAVUN komutunun teleksi (EMİR UYGULANDI): "Savunma emri: {bölge} · {sektör}".</summary>
        public static string DefendDone(Simulation sim, string sectorId)
        {
            var s = sim?.LiberationSector(sectorId);
            return s == null ? null : Loc.F("Savunma emri: {0} · {1}", sim.RegionName(s.Region), s.Name);
        }

        /// <summary>BIRAK komutunun teleksi (EMİR UYGULANDI): "Kanat bırakıldı: {bölge} · {sektör} · güven −{n}".</summary>
        public static string YieldDone(Simulation sim, string sectorId)
        {
            var s = sim?.LiberationSector(sectorId);
            return s == null ? null : Loc.F("Kanat bırakıldı: {0} · {1} · güven −{2}", sim.RegionName(s.Region), s.Name, YieldTrust(sim));
        }

        // ------------------------------------------------------------------ görev fişi

        /// <summary>
        /// Görev fişi bu bölgede kuşatma kipinde mi: bekleyen ya da süren kuşatmanın kanadı bu bölgedeyse ve (karar vardiyasıysa, savunma
        /// sürüyorsa ya da bölgede emir verilecek başka kanat yoksa) kuşatılan sektör; değilse null. Kuşatmanın kurulduğu vardiyada cephe
        /// fişi baskın emrine açık kalır (Core karar vardiyasına kadar baskını kilitlemez).
        /// </summary>
        public static LiberationSectorState SlipSector(Simulation sim, string regionId)
        {
            if (!On(sim) || string.IsNullOrEmpty(regionId)) return null;
            var sector = sim.SiegeSector;
            if (sector == null || sector.Region != regionId) return null;
            if (sim.SiegeDecisionDue || Defending(sim)) return sector;
            var area = sim.LiberationRegion(regionId);
            bool front = regionId == sim.S.Liberation.CurrentRegion && area != null && !area.Completed;
            return front && sim.NextLiberationSector(regionId) != null ? null : sector;
        }

        /// <summary>Fişin kodu: "KARŞI SALDIRI · OP-{nn}".</summary>
        public static string SlipCode(LiberationSectorState s) => Loc.F("KARŞI SALDIRI · OP-{0:00}", s.Index + 1);

        /// <summary>Görev başlığı: "SAVUNMA · {nn} {SEKTÖR}".</summary>
        public static string TaskHead(LiberationSectorState s) => Loc.F("SAVUNMA · {0:00} {1}", s.Index + 1, DeskText.Upper(s.Name));

        /// <summary>Görev cümlesi: süre ve bırakmanın bedeli dengeden.</summary>
        public static string Task(Simulation sim) =>
            Loc.F("Kanadı üç odada savun: {0} tur dayan, röleyi koru. Bırakırsan kanat düşer, güven −{1}.",
                sim.DefenseTurns, YieldTrust(sim));

        /// <summary>BEDEL satırı: savunma fon istemez, vardiyanın baskın hakkını kullanır.</summary>
        /// <para>P4b: 1080'de BEDEL yuvasına sığsın diye kısa ("fon yok · vardiyanın baskın hakkı" taşıyordu).</para>
        public static string Cost => Loc.T("fon yok · baskın hakkı");

        /// <summary>Sektör satırının damgası (kuşatılan kanat).</summary>
        public static string SectorStamp => Loc.T("KUŞATMA");

        /// <summary>Kuşatma kipinin düğmeleri ve kadro raporunun düğmesi.</summary>
        public static string DefendAction => Loc.T("SAVUN");
        public static string YieldAction => Loc.T("BIRAK");
        public static string ReportAction => Loc.T("SAVUNMAYA ÇIK");

        /// <summary>BIRAK'ın bedeli ("güven −1") ve ilk tıktan sonraki onay sorusu ("EMİN MİSİN? · güven −1").</summary>
        public static string YieldCost(Simulation sim) => Loc.F("güven −{0}", YieldTrust(sim));
        public static string YieldConfirm(Simulation sim) => Loc.F("EMİN MİSİN? · güven −{0}", YieldTrust(sim));

        /// <summary>SAVUN düğmesinin sağındaki tur sayısı ("10 tur").</summary>
        public static string DefendCost(Simulation sim) => Loc.F("{0} tur", sim.DefenseTurns);

        /// <summary>Karar vardiyasında cephe fişindeki BASKIN EMRİ VER'in kısa notu.</summary>
        public static string DecideFirst => Loc.T("ÖNCE KARAR VER");

        /// <summary>
        /// Kuşatma kipinin SAHA satırları (beş yuva): DALGA ("{n} dalga · {k} makine"; üzerine gelince geliş turları, kademe ve terk
        /// gecikmesi), İZ ("{n} iz"; yalnız kalıcı tahta açıkken; üzerine gelince tür ve kare), RÖLE ("{h} can", Siper varsa "· Siper +{d}").
        /// </summary>
        public static List<SlipField> SlipFields(Simulation sim, LiberationSectorState sector)
        {
            var o = new List<SlipField>();
            int tier = sim.Tier, size = sim.DefenseWaveSize(tier), waves = sim.DefenseWaveCount;
            var schedule = sim.DefenseSchedulePreview(tier);
            string turns = string.Join(", ", schedule.Select(w => Loc.F("{0}. tur", w.Arrive)));
            o.Add(new SlipField
            {
                Glyph = SiginakGlyphs.Machine(sim.RegionSignature(sector.Region), framed: true), Label = Loc.T("DALGA"),
                Value = Loc.F("{0} dalga · {1} makine", waves, size), Alert = true,
                HoverTitle = Loc.F("DALGA · {0} × {1}", waves, size),
                HoverLine = Loc.F("Giriş kapısından gelir: {0}. İlk tur barikat, makine yok. Terk edilen her oda dalgayı bir tur geciktirir. Tehdit {1}/{2}.",
                    turns, tier, sim.MaxTier),
            });
            if (MarksOn(sim))
            {
                var marks = sim.WingMarks(sector.Id);
                o.Add(new SlipField
                {
                    Glyph = "ic_bolt_s", Label = Loc.T("İZ"), Value = Loc.F("{0} iz", marks.Count),
                    HoverTitle = Loc.F("İZ · {0}", marks.Count),
                    HoverLine = marks.Count == 0 ? Loc.T("Kanatta önceki baskından iz kalmadı.") : MarkList(sim, sector.Id, marks),
                });
            }
            int relay = sim.DefenseRelayHpFor(sim.Facility.Perks), bare = sim.DefenseRelayHpFor(new string[0]);
            o.Add(new SlipField
            {
                Glyph = "ic_shield_s", Label = Loc.T("RÖLE"),
                Value = relay > bare ? Loc.F("{0} can · Siper +{1}", relay, relay - bare) : Loc.F("{0} can", relay),
                HoverTitle = Loc.F("SAHA RÖLESİ · {0} can", relay),
                HoverLine = Loc.T("Hedef odasında durur. Makineler yalnız ekip hedef odasındayken röleyi hedef alır; röle kırılırsa kanat düşer."),
            });
            return o;
        }

        /// <summary>Kanadın izleri sözle, pafta pafta: "1: yıkık duvar 1-C3 · tel 1-D4 (2 can)".</summary>
        public static string MarkList(Simulation sim, string sectorId, IEnumerable<FacilityMark> marks)
        {
            var rooms = sim.FacilityPlan?.Wing(sectorId);
            var parts = new List<string>();
            foreach (var m in marks)
            {
                int index = rooms?.FirstOrDefault(r => r.Id == m.Room)?.Index ?? 0;
                parts.Add(MarkWord(m, index));
            }
            return string.Join(" · ", parts);
        }

        /// <summary>Tek izin adı ve karesi: "yıkık duvar 1-C3", engelde kalan canıyla "tel 1-D4 (2 can)".</summary>
        public static string MarkWord(FacilityMark m, int room)
        {
            string cell = RaidBoard3D.CellName(room, m.X, m.Y);
            return FacilityMarks.IsObjectKind(m.Kind) ? Loc.F("{0} {1} ({2} can)", KindName(m.Kind), cell, Math.Max(1, m.Hp))
                : Loc.F("{0} {1}", KindName(m.Kind), cell);
        }

        /// <summary>İz türünün adı (küçük harf, satır içinde).</summary>
        public static string KindName(string kind) => kind switch
        {
            FacilityMarks.Wall => Loc.T("yıkık duvar"),
            FacilityMarks.Shutter => Loc.T("açık kepenk"),
            FacilityMarks.Device => Loc.T("kırık donanım"),
            FacilityMarks.Cool => Loc.T("kırık soğutma ünitesi"),
            FacilityMarks.Spawner => Loc.T("kırık kopya dolabı"),
            FacilityMarks.Wire => Loc.T("tel"),
            FacilityMarks.Plank => Loc.T("kalas"),
            FacilityMarks.Scrap => Loc.T("hurda"),
            _ => kind ?? "",
        };

        // ------------------------------------------------------------------ bina planı

        /// <summary>Paftanın iz satırı: "İz: 2 yıkık duvar · 1 tel (2 can)" (aynı tür bir araya, engelde canlar). İz yoksa null.</summary>
        public static string PlanMarks(IReadOnlyList<FacilityMark> marks)
        {
            if (marks == null || marks.Count == 0) return null;
            var parts = new List<string>();
            foreach (var group in marks.GroupBy(m => m.Kind))
            {
                int n = group.Count();
                parts.Add(FacilityMarks.IsObjectKind(group.Key)
                    ? Loc.F("{0} {1} ({2} can)", n, KindName(group.Key), string.Join("/", group.Select(m => Math.Max(1, m.Hp))))
                    : Loc.F("{0} {1}", n, KindName(group.Key)));
            }
            return Loc.F("İz: {0}", string.Join(" · ", parts));
        }

        /// <summary>Odanın son YZ onarımları (en yeni önce, en çok iki): "YZ onardı · V.012: yıkık duvar 1-C3". Yoksa boş.</summary>
        public static List<string> PlanRepairs(Simulation sim, FacilityRoom room, int max = 2)
        {
            var o = new List<string>();
            if (!MarksOn(sim) || room == null) return o;
            foreach (var m in sim.Facility.Repairs.Where(r => r != null && r.Room == room.Id).Reverse().Take(max))
                o.Add(Loc.F("YZ onardı · V.{0:000}: {1}", m.Day + 1, MarkWord(m, room.Index)));
            return o;
        }

        /// <summary>Savunulan kanadın giriş ve ara odasında: terk edilen odanın izleri silinir.</summary>
        public static string PlanAbandonNote => Loc.T("Terk edilirse izler silinir.");

        // ------------------------------------------------------------------ CRT, telsiz, devir fişi, ön kontrol, alarm

        /// <summary>Boşta CRT ipucu (bekleyen ödülden sonra gelir): kuşatma beklerken "KARŞI SALDIRI BEKLİYOR · SAVUN YA DA BIRAK".</summary>
        public static string CrtHint(Simulation sim) =>
            On(sim) && sim.PendingSiege != null ? Loc.T("KARŞI SALDIRI BEKLİYOR · SAVUN YA DA BIRAK") : null;

        /// <summary>CRT geçişinde görev türü: savunmada "SAVUNMA"; değilse null.</summary>
        public static string CrtMission(Simulation sim) => Defending(sim) ? Loc.T("SAVUNMA") : null;

        /// <summary>
        /// Telsiz günlüğünün kuşatma ve onarım satırları (gün, metin): kurulan her kuşatma "Karşı saldırı bekliyor · {yer}" (kurulduğu
        /// gün); kalıcı tahta açıkken onarım günü ve kanat başına "YZ {yer} onardı". <paramref name="place"/> kısa yer adı.
        /// </summary>
        public static List<(int Day, string Text)> RadioLines(Simulation sim, Func<LiberationSectorState, string> place)
        {
            var o = new List<(int, string)>();
            if (!On(sim) && !MarksOn(sim)) return o;
            if (On(sim))
                foreach (var e in sim.Facility.Sieges)
                {
                    var s = e == null ? null : sim.LiberationSector(e.Sector);
                    if (s != null) o.Add((e.Day, Loc.F("Karşı saldırı bekliyor · {0}", place(s))));
                }
            if (MarksOn(sim))
                foreach (var g in sim.Facility.Repairs.Where(m => m != null).GroupBy(m => (m.Day, Wing: SectorOf(m.Room))))
                {
                    var s = sim.LiberationSector(g.Key.Wing);
                    if (s != null) o.Add((g.Key.Day, Loc.F("YZ {0} onardı", place(s))));
                }
            return o.OrderBy(x => x.Item1).ToList();
        }

        /// <summary>Oda kimliğinden kanat kimliği ("weu_2.1" → "weu_2").</summary>
        static string SectorOf(string roomId) => FacilityMaps.ParseRoomId(roomId, out var sector, out _) ? sector : roomId;

        /// <summary>Telsizin bugünkü satırı: karar vardiyasında "Karar bekleniyor: savun ya da bırak"; değilse null.</summary>
        public static string RadioToday(Simulation sim) =>
            On(sim) && sim.SiegeDecisionDue ? Loc.T("Karar bekleniyor: savun ya da bırak") : null;

        /// <summary>Telsizin baskın dönüşü satırı (savunma): "{ekip} döndü · {yer} tutuldu" ya da "… düştü".</summary>
        public static string RadioReturn(RaidLogEntry log, string team, string place) =>
            log.Success ? Loc.F("{0} döndü · {1} tutuldu", team, place) : Loc.F("{0} döndü · {1} düştü", team, place);

        /// <summary>Ön kontrolün notu: karar vardiyasında karar verilmediyse "karşı saldırıya karar verilmedi: bırakılacak"; değilse null.</summary>
        public static string PreCheck(Simulation sim) =>
            On(sim) && sim.SiegeDecisionDue && sim.S.Raid == null ? Loc.T("karşı saldırıya karar verilmedi: bırakılacak") : null;

        /// <summary>Devir fişinin uyarı metni (kutunun öneki "KARŞI SALDIRI ·").</summary>
        public static string SlipWarning(int trust) => Loc.F("Karar verilmezse kanat bırakılır (güven −{0}).", trust);

        /// <summary>Alarm panosunun açıklaması (savunma açıkken): eşikte kuşatma, karar sonraki vardiya.</summary>
        public static string AlarmHover(Simulation sim, string threshold, string rate) =>
            Loc.F("Eşiğe ({0}) varınca YZ son alınan kanada saldırır: sonraki vardiya savun ya da bırak. Her vardiya +{1}; telsiz odası düşürür.",
                threshold, rate);

        // ------------------------------------------------------------------ operasyon raporu

        /// <summary>Savunmanın başlığı: "KANAT TUTULDU" ya da "KANAT DÜŞTÜ".</summary>
        public static string ReportTitle(RaidLogEntry log) => log.Success ? Loc.T("KANAT TUTULDU") : Loc.T("KANAT DÜŞTÜ");

        /// <summary>Savunmanın sonuç damgası: "TUTULDU" ya da "KAYBEDİLDİ".</summary>
        public static string ReportStamp(RaidLogEntry log) => log.Success ? Loc.T("TUTULDU") : Loc.T("KAYBEDİLDİ");

        /// <summary>Savunmanın cümlesi (uyarı yoksa).</summary>
        public static string ReportSentence(RaidLogEntry log) => log.Success
            ? Loc.T("YZ'nin karşı saldırısı püskürtüldü; kanat KYGA'da kaldı.")
            : Loc.T("Kanat düştü; sektör yeniden işgal altında. Yeniden alınabilir.");

        /// <summary>Terk edilen odaların satırı: "Terk edilen oda" · "{n} · güven −{n × bedel}"; terk yoksa null.</summary>
        public static OperationRow AbandonRow(Simulation sim, LiberationRaidReward reward)
        {
            int n = reward?.Abandoned ?? 0;
            if (n <= 0) return null;
            return new OperationRow { Label = Loc.T("Terk edilen oda"), Value = Loc.F("{0} · güven −{1}", n, n * sim.DefenseRoomTrust), Tone = OperationTone.Alert };
        }

        /// <summary>Başarısız baskının kurduğu kuşatma: "Karşı saldırı bekliyor" · "{bölge} · {sektör}" (Alert); yoksa null.</summary>
        public static OperationRow SiegeRow(Simulation sim, LiberationRaidReward reward)
        {
            if (string.IsNullOrEmpty(reward?.Siege)) return null;
            return new OperationRow { Label = Loc.T("Karşı saldırı bekliyor"), Value = Place(sim, reward.Siege), Tone = OperationTone.Alert };
        }

        /// <summary>
        /// Savunmanın sonuç teleksi: "KANAT TUTULDU" (Done) ya da "KANAT DÜŞTÜ" (Failed); ileti "{bölge} · {sektör} · {t} tur · {n} oda terk ·
        /// ekip {a}/{b} ayakta" (+ "· {k} düştü").
        /// </summary>
        public static (string Status, string Message, TelexKind Kind) Telex(Simulation sim, RaidLogEntry log, RaidResult result)
        {
            var sector = sim.LiberationSector(log.SectorId);
            var reward = log.LiberationReward;
            string region = sim.RegionName(log.Region), sectorName = sector?.Name ?? Loc.T("Sektör");
            int total = reward?.Squad.Count ?? 0, standing = reward?.Squad.Count(o => !o.Down && !o.Dead) ?? 0, abandoned = reward?.Abandoned ?? 0;
            string message = result != null
                ? Loc.F("{0} · {1} · {2} tur · {3} oda terk · ekip {4}/{5} ayakta", region, sectorName, result.Turns, abandoned, standing, total)
                : Loc.F("{0} · {1} · {2} oda terk · ekip {3}/{4} ayakta", region, sectorName, abandoned, standing, total);
            if (total - standing > 0) message = Loc.F("{0} · {1} düştü", message, total - standing);
            return log.Success ? (Loc.T("KANAT TUTULDU"), message, TelexKind.Done) : (Loc.T("KANAT DÜŞTÜ"), message, TelexKind.Failed);
        }

        /// <summary>Pano kilidinin nedeni (süren ya da bu vardiya biten savunma): "savunma emri verildi".</summary>
        public static string LockReason => Loc.T("savunma emri verildi");
    }
}
