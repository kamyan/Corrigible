using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis aşama 10: önlem defterinin oyuncu metinleri (Docs/TESIS-BASKINLARI-TASARIMI.md §6.3; aşama 10 tasarımı §9). ÖNLEM teleksi,
    /// görev fişinin ÖNLEM satırı ve üzerine gelme satırları, bina planının kırmızı kalem notu, tahtadaki robot etiketi, hover satırları,
    /// kullanılamama nedenleri, gözcü satırları, telsiz günlüğü, defterin cevap sayfası ve ödül kâğıdının cevap satırı. Kaynak yalnız
    /// Core'un public okumaları (<see cref="FacilityCounters"/>, <see cref="Simulation.Countermeasures"/>, <see cref="FacilityState.Learned"/>,
    /// <c>Baskin.Bolted/IsDoubleWall/DoubleWallCost/EmitterBlocks</c>, <see cref="FacilityGear"/>): kural kopyalanmaz. Saf: durum
    /// değiştirmez, RNG çekmez, komut yazmaz. Defter kapalıyken (<see cref="Simulation.CountermeasuresOn"/> false) hiçbir yüzey metin
    /// üretmez; örnek tahtada (Room ya da Moves null) tahta satırları boş.
    /// </summary>
    public static class FacilityCounterText
    {
        /// <summary>Uzun önlem listesinin yerine sayının yazıldığı sınır (karakter; fişin değer sütunu tek satırdır, KAZANIM ile aynı).</summary>
        public const int SlipListMax = 24;

        static DeskLang Lang => Loc.Lang == Loc.English ? DeskLang.En : DeskLang.Tr;
        static string Lower(string s) => DeskText.Lower(s, Lang);
        static RaidCardRow Row(string icon, string text, bool good = false) => new RaidCardRow { Icon = icon, Text = text, Good = good };

        /// <summary>Önlem defteri açık mı (alt bayrak ve tesis durumu).</summary>
        public static bool On(Simulation sim) => sim?.B != null && sim.CountermeasuresOn;

        /// <summary>Kademenin etkisi, sondaki nokta olmadan (teleks ve hover satırının ortası).</summary>
        static string Effect(Balance b, string cls, int level) => FacilityCounters.Desc(b, cls, level).TrimEnd('.');

        // ------------------------------------------------------------------ teleks

        /// <summary>ÖNLEM teleksinin durumu: "YZ ÖNLEMİ" (Alarm satırı; şeritte de görünür).</summary>
        public static string TelexStatus => Loc.T("YZ ÖNLEMİ");

        /// <summary>
        /// Öğrenilen önlemin iki teleks satırı: "{ad} · {k}. kademe · {etki}" ve "Kalan YZ'lerde geçerli · cevap: {cevap tarifi}" (köpek
        /// sınıfında "· yayıcıyı kır"). Defter kapalıyken ya da girdi bilinmiyorsa (null, null).
        /// </summary>
        public static (string First, string Second) Telex(Simulation sim, FacilityCounterEntry e)
        {
            if (!On(sim) || e == null || FacilityCounters.Index(e.Class) < 0 || e.Level <= 0) return (null, null);
            string first = Loc.F("{0} · {1}. kademe · {2}", FacilityCounters.Name(e.Class, e.Level), e.Level, Effect(sim.B, e.Class, e.Level));
            string answer = FacilityCounters.AnswerOf(e.Class);
            string second = answer != null ? Loc.F("Kalan YZ'lerde geçerli · cevap: {0}", FacilityGear.RecipeName(answer))
                : Loc.T("Kalan YZ'lerde geçerli · yayıcıyı kır");
            return (first, second);
        }

        /// <summary>Telsiz günlüğünün önlem satırları: her öğrenme için (gün, "YZ önlemi · {ad}"), günlük sırasıyla. Defter kapalıyken boş.</summary>
        public static List<(int Day, string Text)> RadioLines(Simulation sim)
        {
            var o = new List<(int, string)>();
            if (!On(sim)) return o;
            foreach (var e in sim.Facility.Learned)
                if (e != null && FacilityCounters.Index(e.Class) >= 0)
                    o.Add((e.Day, Loc.F("YZ önlemi · {0}", FacilityCounters.Name(e.Class, e.Level))));
            return o;
        }

        // ------------------------------------------------------------------ görev fişi

        /// <summary>
        /// Görev fişinin ÖNLEM satırı (K37, K38): tesis kipinde ve en az bir önlem ya da sıfırdan büyük bir YZ sayacı varken. Değer önlemlerin
        /// adları (" · " ile; <see cref="SlipListMax"/>'ı aşarsa "{n} önlem"), önlem yokken "yok"; önlem varken değer kırmızı. Üzerine gelince
        /// her önlemin kademesi, etkisi ve cevabı, son satırda YZ'nin gözlemi (sayaç / eşik). Görünmezse null.
        /// </summary>
        public static SlipField SlipField(Simulation sim)
        {
            if (!On(sim)) return null;
            var list = sim.Countermeasures;
            var seen = Observed(sim);
            if (list.Count == 0 && seen.Count == 0) return null;
            var names = list.Select(c => FacilityCounters.Name(c.Class, c.Level)).ToList();
            string joined = string.Join(" · ", names);
            string value = list.Count == 0 ? Loc.T("yok") : joined.Length <= SlipListMax ? joined : Loc.F("{0} önlem", list.Count);
            var lines = list.Select(c => HoverLine(sim, c.Class, c.Level)).ToList();
            if (seen.Count > 0) lines.Add(Loc.F("YZ gözlemi: {0}", string.Join(" · ", seen)));
            return new SlipField
            {
                Glyph = "ic_robot_s", Label = Loc.T("ÖNLEM"), Value = value, Alert = list.Count > 0,
                HoverTitle = Loc.F("ÖNLEM · {0}", list.Count), HoverLine = string.Join("\n", lines),
            };
        }

        /// <summary>
        /// K37'nin sığma yolu: fişin beş SAHA yuvası doluyken KAZANIM ve ÖNLEM tek satırda ("KAZANIM 2 · ÖNLEM 1"); üzerine gelince ikisi
        /// (kazanım listesi, sonra önlem satırları). Önlem varken değer kırmızı.
        /// </summary>
        public static SlipField MergeWithPerks(Simulation sim, SlipField perks, SlipField counters)
        {
            int p = sim?.Facility?.Perks.Count ?? 0, c = sim?.Countermeasures.Count ?? 0;
            return new SlipField
            {
                Glyph = perks.Glyph, Label = perks.Label, Value = Loc.F("{0} · ÖNLEM {1}", p, c), Alert = counters.Alert,
                HoverTitle = Loc.F("KAZANIM · {0} · ÖNLEM · {1}", p, c), HoverLine = string.Join("\n", perks.HoverLine, counters.HoverLine),
            };
        }

        /// <summary>Fişin üzerine gelme satırı: "Cıvatalı ayak (1): İmza makineleri itilmez ve çekilmez · cevap: Söküm anahtarı"
        /// (cevap tarifi açıksa "cevap açık: …"; köpek sınıfında "cevap: yayıcıyı kır").</summary>
        public static string HoverLine(Simulation sim, string cls, int level)
        {
            string name = FacilityCounters.Name(cls, level), fx = Effect(sim?.B, cls, level);
            string answer = FacilityCounters.AnswerOf(cls);
            if (answer == null) return Loc.F("{0} ({1}): {2} · cevap: yayıcıyı kır", name, level, fx);
            bool open = sim?.Facility != null && sim.Facility.Recipes.Contains(answer);
            return open ? Loc.F("{0} ({1}): {2} · cevap açık: {3}", name, level, fx, FacilityGear.RecipeName(answer))
                : Loc.F("{0} ({1}): {2} · cevap: {3}", name, level, fx, FacilityGear.RecipeName(answer));
        }

        /// <summary>YZ'nin gözlemi: sayacı sıfırdan büyük ve kademesi 2'den küçük sınıflar, tablo sırasıyla ("itme 4/6").</summary>
        public static List<string> Observed(Simulation sim)
        {
            var o = new List<string>();
            if (!On(sim)) return o;
            int need = Math.Max(1, sim.B.facilityCounterThreshold);
            foreach (var cls in FacilityCounters.Classes)
            {
                int t = sim.CounterTally(cls);
                if (t > 0 && sim.CounterLevel(cls) < 2) o.Add(Loc.F("{0} {1}/{2}", Lower(FacilityCounters.ClassName(cls)), t, need));
            }
            return o;
        }

        // ------------------------------------------------------------------ bina planı (kırmızı kalem)

        /// <summary>Paftanın kırmızı kalem satırları (pafta hover'ına eklenir): bulunan her tür için bir satır, sabit sırayla (çift cidar,
        /// koku yayıcı). Kalem yoksa boş.</summary>
        public static List<string> PlanLines(Simulation sim, bool doubleWall, bool emitter)
        {
            var o = new List<string>();
            if (!On(sim)) return o;
            if (doubleWall) o.Add(Loc.F("Önlem (kırmızı kalem): Çift cidar · bu duvar {0} hak ister", Math.Max(1, sim.B.facilityDoubleWallCharges)));
            if (emitter) o.Add(Loc.F("Önlem (kırmızı kalem): Koku yayıcı · {0} kare içinde köpek komut alamaz", Math.Max(0, sim.B.facilityEmitterRange)));
            return o;
        }

        // ------------------------------------------------------------------ baskın tahtası

        /// <summary>Cıvatalı makinenin tahta etiketi ("CIVATALI"; <see cref="FacilityCounters.Short"/>); cıvatasızda null.</summary>
        public static string BoltTag(Baskin.BState B, Baskin.Unit u) => Baskin.Bolted(B, u) ? FacilityCounters.Short(FacilityCounters.Push) : null;

        /// <summary>
        /// Makinenin önlem satırları (hover kartı): cıvatalıda "ÖNLEM · Cıvatalı ayak · itilmez, çekilmez", sökülmüşte "Cıvatası sökük". Eski
        /// tahtada ve önlemsiz tahtada boş.
        /// </summary>
        public static List<RaidCardRow> UnitRows(Baskin.BState B, Baskin.Unit u)
        {
            var o = new List<RaidCardRow>();
            if (B?.Room == null || B.Moves == null || u == null || u.Side != 'e' || u.Dead) return o;
            if (Baskin.Bolted(B, u)) o.Add(Row("ic_x_s", Loc.F("ÖNLEM · {0} · itilmez, çekilmez", FacilityCounters.Name(FacilityCounters.Push, B.Moves.Bolt))));
            else if (u.Unbolted) o.Add(Row("ic_eye_s", Loc.T("Cıvatası sökük"), true));
            return o;
        }

        /// <summary>
        /// Nesnenin önlem satırları: tel (kesici kol 1. kademeden) ve kalas (2. kademede) "Kesici kol: makineler +{n} hasar"; koku yayıcı
        /// "Koku yayıcı: {r} kare içinde köpek komut alamaz · kırılınca biter". Öbürlerinde ve örnek tahtada boş.
        /// </summary>
        public static List<RaidCardRow> ObjectRows(Baskin.BState B, Baskin.Obj o)
        {
            var rows = new List<RaidCardRow>();
            var m = B?.Moves;
            if (B?.Room == null || m == null || o == null) return rows;
            if ((o.K == "tel" && m.Cutter >= 1 || o.K == "kalas" && m.Cutter >= 2) && m.CutterDamage > 0)
                rows.Add(Row("ic_x_s", Loc.F("Kesici kol: makineler +{0} hasar", m.CutterDamage)));
            if (o.K == "yayici")
                rows.Add(Row("ic_x_s", Loc.F("Koku yayıcı: {0} kare içinde köpek komut alamaz · kırılınca biter", m.EmitterRange)));
            return rows;
        }

        /// <summary>Çift cidarlı duvarın hover başlığı ("Çift cidar"); değilse null.</summary>
        public static string WallTitle(Baskin.BState B, int x, int y) => Baskin.IsDoubleWall(B, x, y) ? Loc.T("Çift cidar") : null;

        /// <summary>Çift cidarlı duvarın hover satırı: "yıkmak {0} hak" (Delici şarjla bedeli); değilse boş.</summary>
        public static List<RaidCardRow> WallRows(Baskin.BState B, int x, int y)
        {
            var o = new List<RaidCardRow>();
            if (Baskin.IsDoubleWall(B, x, y)) o.Add(Row("ic_x_s", Loc.F("ÖNLEM · Çift cidar · yıkmak {0} hak", Baskin.DoubleWallCost(B))));
            return o;
        }

        /// <summary>
        /// Önlemin kullanılamama nedeni (Core reddeder, metin yalnız açıklar; önlem değilse null): köpek komutunda erimde ayakta koku yayıcı
        /// ("koku yayıcı yakında"); lokumda bitişikte yalnız hakkın yetmediği çift cidar ("çift cidar: {0} hak gerekir"); koşu darbesinde
        /// hattın ilk makinesi cıvatalı ("cıvatalı: itilmez"). Koşu denetimi Core'un hedef kuralını sökülmüş kopyada çalıştırır (kural
        /// kopyalanmaz).
        /// </summary>
        public static string Reason(Baskin.BState B, Baskin.Unit u, string kind)
        {
            if (B?.Room == null || B.Moves == null || u == null) return null;
            switch (kind)
            {
                case Baskin.ActBark: case Baskin.ActFetch: case Baskin.ActSniff:
                    return Baskin.EmitterBlocks(B, u) ? Loc.T("koku yayıcı yakında") : null;
                case Baskin.ActCharge:
                {
                    int cost = Baskin.DoubleWallCost(B);
                    if (u.GearLeft <= 0 || u.GearLeft >= cost) return null;
                    foreach (var d in Baskin.Dirs)
                        if (Baskin.IsDoubleWall(B, u.X + d[0], u.Y + d[1])) return Loc.F("çift cidar: {0} hak gerekir", cost);
                    return null;
                }
                case Baskin.ActRush:
                {
                    if (B.Moves.Bolt <= 0 || !B.Units.Any(e => Baskin.Bolted(B, e))) return null;
                    var copy = B.Clone();
                    foreach (var e in copy.Units) if (e.Side == 'e') e.Unbolted = true;
                    var cu = copy.Units.FirstOrDefault(v => v.Id == u.Id);
                    return cu != null && Baskin.ActionTargets(copy, cu, kind).Count > 0 ? Loc.T("cıvatalı: itilmez") : null;
                }
                default: return null;
            }
        }

        /// <summary>
        /// Gözcü raporunun önlem satırları (oda açılışında; yalnız bulunanlar): çift cidarlı duvarlar, koku yayıcılar, cıvatalı makineler
        /// türüne göre ("Gözcü · cıvatalı: Zırhlı dron 1-F2"). Kareler pafta önekiyle. Örnek tahtada boş.
        /// </summary>
        public static List<RaidLogModel> Spotter(Baskin.BState B)
        {
            var o = new List<RaidLogModel>();
            if (B?.Room == null || B.Moves == null) return o;
            int room = B.Room.Index;
            string Cells(IEnumerable<Baskin.Pt> tiles) => string.Join(", ", tiles.Select(t => FacilityRaidText.Cell(room, t.X, t.Y)));
            var walls = new List<Baskin.Pt>();
            var emitters = new List<Baskin.Pt>();
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    if (Baskin.IsDoubleWall(B, x, y)) walls.Add(new Baskin.Pt(x, y));
                    if (Baskin.CellAt(B, x, y).O?.K == "yayici") emitters.Add(new Baskin.Pt(x, y));
                }
            if (walls.Count > 0) o.Add(new RaidLogModel { Text = Loc.F("Gözcü · çift cidar: {0}", Cells(walls)), Kind = "bad" });
            if (emitters.Count > 0) o.Add(new RaidLogModel { Text = Loc.F("Gözcü · koku yayıcı: {0}", Cells(emitters)), Kind = "bad" });
            foreach (var g in B.Units.Where(e => !e.Out && Baskin.Bolted(B, e)).GroupBy(e => e.Type))
                o.Add(new RaidLogModel
                {
                    Text = Loc.F("Gözcü · cıvatalı: {0} {1}", Baskin.UnitTypes[g.Key].Name, Cells(g.Select(e => new Baskin.Pt(e.X, e.Y)))),
                    Kind = "bad",
                });
            return o;
        }

        // ------------------------------------------------------------------ defter ve ödül kâğıdı

        /// <summary>Cevap sayfasının el notu: "Cevap tarifi · {önlem} önlemine karşı · +{n} devre"; cevap değilse boş.</summary>
        public static string AnswerNote(Simulation sim, BoardRecipe r)
        {
            if (r == null || string.IsNullOrEmpty(r.Answer)) return string.Empty;
            string cls = FacilityCounters.ClassOfAnswer(r.Answer);
            int level = Math.Max(1, sim?.CounterLevel(cls) ?? 1);
            return Loc.F("Cevap tarifi · {0} önlemine karşı · +{1} devre", FacilityCounters.Name(cls, level), Math.Max(0, sim?.B?.facilityAnswerCircuits ?? 1));
        }

        /// <summary>Cevap sayfasının kaynakları: temelin YZ'si ve cevabın YZ'si ("VERITY · cevap: HALCYON"; temel baştan açıksa yalnız cevap).</summary>
        public static string AnswerSource(Simulation sim, BoardRecipe r)
        {
            if (r == null || string.IsNullOrEmpty(r.Answer)) return string.Empty;
            string basis = FacilityRewardText.SourceName(sim, r.Id), answer = FacilityRewardText.SourceName(sim, r.Answer);
            return string.IsNullOrEmpty(basis) ? Loc.F("cevap: {0}", answer) : Loc.F("{0} · cevap: {1}", basis, answer);
        }

        /// <summary>Ödül kâğıdının cevap satırının açıklaması: cevabın etkisi (<see cref="FacilityGear.AnswerDesc"/>) ve sınıfın önlemi
        /// yürürlükteyse "· önlem yürürlükte".</summary>
        public static string AnswerCost(Simulation sim, string answer)
        {
            string desc = FacilityGear.AnswerDesc(sim?.B, answer);
            string cls = FacilityCounters.ClassOfAnswer(answer);
            return On(sim) && cls != null && sim.CounterLevel(cls) >= 1 ? Loc.F("{0} · önlem yürürlükte", desc.TrimEnd('.')) : desc;
        }

        /// <summary>Cevap seçildiyse teleksin ikinci satırı: "Defterde TARİF {nn} değişti: {cevap}" (Çift zemberekte birden çok sayfa
        /// virgülle); defterde dönüşen sayfa yoksa null.</summary>
        public static string ChangedPageLine(Simulation sim, string answer)
        {
            if (!On(sim) || !FacilityGear.IsAnswer(answer)) return null;
            var pages = sim.BoardRecipes.Select((r, i) => (r, i)).Where(p => p.r.Answer == answer).Select(p => (p.i + 1).ToString("00")).ToList();
            return pages.Count == 0 ? null : Loc.F("Defterde TARİF {0} değişti: {1}", string.Join(", ", pages), FacilityGear.RecipeName(answer));
        }
    }
}
