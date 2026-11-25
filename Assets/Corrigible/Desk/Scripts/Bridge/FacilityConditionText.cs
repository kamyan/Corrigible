using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis aşama 12 (Docs/TESIS-BASKINLARI-TASARIMI.md §10; aşama 12 tasarımı §11): iki tesis şartının (Katar, Yalpa) masa ve baskın
    /// metinleri. Aşama 12 P4a: bina planı paftasının satırları (açık kenar, yatış takvimi), takvim metni, operasyon raporunun kenar satırı
    /// ve fişin olayı. Aşama 12 P4b: baskın sahnesinin metinleri (açık kenar ve düşme önizlemesi, ekip kartı, telsiz fişi ve kapanış
    /// satırları, sonuç kartı eki, AYIR: EMRİ İLET, durum satırı, Teknisyen'in kartı, takipçi etiketi ve hover'ı, kapı ve geçiş fişleri,
    /// gözcü; Yalpa: gözcü takvimi, karesiz fiş, kayma hover'ı).
    /// Kaynak yalnız Core'un public okumalarıdır (<see cref="Simulation.RegionEvent"/>, <see cref="Baskin.OpenEdgeRows"/>,
    /// <see cref="Baskin.TiltSchedule"/>, <see cref="SquadOutcome.Overboard"/>): takvim ve kural kopyalanmaz. Saf: durumu okur,
    /// değiştirmez; UnityEngine kullanmaz. Şart yoksa (bölge olayları kapalı, alt bayrak kapalı, şartsız oda ya da örnek tahta) metin
    /// üretmez (null): şartsız sunum bugünkü gibidir.
    /// </summary>
    public static class FacilityConditionText
    {
        public const string Katar = "katar", Yalpa = "yalpa";

        // ------------------------------------------------------------------ fiş

        /// <summary>Bölgenin fişteki ve kimlikteki olayı (<see cref="Simulation.RegionEvent"/>: emre yazılan şart; tesis açıkken na Katar, oc
        /// Yalpa). Simülasyon yoksa bölgenin bugünkü olayı (<see cref="RegionDef.Event"/>); bölge bilinmiyorsa null.</summary>
        public static string EventOf(Simulation sim, string regionId)
        {
            if (sim != null) return sim.RegionEvent(regionId);
            return GameData.Regions.FirstOrDefault(r => r.Id == regionId)?.Event;
        }

        // ------------------------------------------------------------------ pafta

        /// <summary>Kenardan düşen ekip üyesinin yatışı (Core'un dönüş kuralıyla aynı alan: <see cref="Balance.facilityFallInjuryTurns"/>,
        /// en az 1).</summary>
        public static int FallShifts(Balance b) => Math.Max(1, b?.facilityFallInjuryTurns ?? 1);

        /// <summary>Katar paftasının hover satırı: "Açık kenar (kuzey, güney): itilen düşer; makine yok olur, ekip üyesi 5 vardiya yatar."</summary>
        public static string PlanOpenEdge(Balance b) =>
            Loc.F("Açık kenar (kuzey, güney): itilen düşer; makine yok olur, ekip üyesi {0} vardiya yatar.", FallShifts(b));

        /// <summary>Yatışın tek adımı: "2. tur batı" / "4. tur doğu" (yön Core'dan: −1 batı, +1 doğu).</summary>
        public static string TiltStep(int turn, int dir) => dir < 0 ? Loc.F("{0}. tur batı", turn) : Loc.F("{0}. tur doğu", turn);

        /// <summary>Oda bütçesinin yatış takvimi (<see cref="Baskin.TiltSchedule"/>): "2. tur batı, 4. tur doğu" (5 tur), 8 turda ayrıca
        /// "6. tur batı". Takvim boşsa null.</summary>
        public static string TiltCalendar(int maxTurns)
        {
            var steps = Baskin.TiltSchedule(maxTurns);
            return steps.Count == 0 ? null : string.Join(", ", steps.Select(s => TiltStep(s.Turn, s.Dir)));
        }

        /// <summary>Yalpa paftasının hover satırı: "Yalpa: 2. tur batı, 4. tur doğu · sabitlenmemiş her şey 1 kare kayar". Takvim boşsa null.</summary>
        public static string PlanTilt(int maxTurns)
        {
            string calendar = TiltCalendar(maxTurns);
            return calendar == null ? null : Loc.F("Yalpa: {0} · sabitlenmemiş her şey 1 kare kayar", calendar);
        }

        /// <summary>
        /// Odanın tur bütçesi: emrin açacağı tahtanın MaxTurns'ü, Core okuyucusundan (<see cref="Simulation.FacilityRoomTurns"/>; kural
        /// kopyalanmaz, aşama 12 kapanışı). Simülasyon ya da oda yoksa 0.
        /// </summary>
        public static int RoomTurns(Simulation sim, FacilityRoom room) => sim == null || room == null ? 0 : sim.FacilityRoomTurns(room);

        /// <summary>
        /// Paftanın şart satırı (hover): Katar odasında açık kenar (<see cref="Baskin.OpenEdgeRows"/> doluysa), Yalpa odasında yatış takvimi.
        /// Bölge olayları kapalıyken (plan şartı göstermez), tesis planı yokken ve öbür odalarda null.
        /// </summary>
        public static string PlanLine(Simulation sim, FacilityRoom room)
        {
            if (sim?.FacilityPlan == null || room == null || !sim.B.liberationRegionEvents) return null;
            if (room.Event == Katar && Baskin.OpenEdgeRows(room).Count > 0) return PlanOpenEdge(sim.B);
            if (room.Event == Yalpa) return PlanTilt(RoomTurns(sim, room));
            return null;
        }

        /// <summary>Paftanın açık kenar satırları (0 kuzey, 7 güney; <see cref="Baskin.OpenEdgeRows"/>): Katar odasında, bölge olayları açıkken.</summary>
        public static IReadOnlyList<int> PlanOpenRows(Simulation sim, FacilityRoom room) =>
            sim?.FacilityPlan == null || room == null || !sim.B.liberationRegionEvents ? Array.Empty<int>() : Baskin.OpenEdgeRows(room);

        // ------------------------------------------------------------------ rapor

        /// <summary>Operasyon raporunda kenardan düşenin durumu: "vagondan düştü · 5 vardiya yatış".</summary>
        public static string ReportOverboard(int shifts) => Loc.F("vagondan düştü · {0} vardiya yatış", shifts);

        // ------------------------------------------------------------------ baskın sahnesi (P4b)

        /// <summary>Tahta Katar odası mı (tesis odası, olay "katar": ara ve hedef vagon; giriş vagonu ve savunma tahtası değil).</summary>
        public static bool IsKatar(Baskin.BState B) => B?.Room != null && B.Ev?.Id == Katar;

        /// <summary>Tahta Yalpa odası mı (tesis odası, olay "yalpa").</summary>
        public static bool IsYalpa(Baskin.BState B) => B?.Room != null && B.Ev?.Id == Yalpa;

        /// <summary>Katar'ın dönüş geçidi: kaçışta Katar vagonunda (AYIR ve takip yalnız burada; Core <see cref="Baskin.DecoupleBlock"/>
        /// "rule" değil).</summary>
        public static bool IsKatarReturn(Baskin.BState B) => IsKatar(B) && B.Escape && Baskin.DecoupleBlock(B) != "rule";

        /// <summary>Yatışın yön sözcüğü: −1 "batı", +1 "doğu".</summary>
        public static string DirWord(int dir) => dir < 0 ? Loc.T("batı") : Loc.T("doğu");

        // ---------- Katar: kenar ve düşme ----------

        /// <summary>
        /// Açık kenar karesinin hover satırı (<see cref="Baskin.IsOpenEdge"/>): "Açık kenar: buradan itilen düşer"; ayrıntı karedeki birime
        /// göre (makine "yok olur", ekip üyesi "5 vardiya yatar"). Kenar karesi değilse null.
        /// </summary>
        public static RaidCardRow OpenEdgeRow(Baskin.BState B, int x, int y, Balance rules)
        {
            if (!Baskin.IsOpenEdge(B, x, y) || Baskin.CellAt(B, x, y).T == "wall") return null;
            var u = Baskin.UnitAt(B, x, y);
            string detail = u == null ? null : u.Side == 'e' ? Loc.T("yok olur") : Baskin.IsSpecialist(u) ? Loc.F("{0} vardiya yatar", FallShifts(rules)) : null;
            return new RaidCardRow { Icon = "ic_x_s", Text = Loc.T("Açık kenar: buradan itilen düşer"), Detail = detail };
        }

        /// <summary>
        /// Önizlemenin düşme satırları (Core'un <c>fall</c> efektleri; önizleme kopyada uygular, kural kopyalanmaz): makinede "Kenardan düşer ·
        /// yok olur" (iyi), ekip üyesinde "Vagondan düşer · 5 vardiya yatar" (kötü), köpekte "Kenardan düşer", tutsak ve aygıtta makinenin
        /// metni. <paramref name="B"/> önizlemeden önceki durum (birimin türü için).
        /// </summary>
        public static List<RaidCardRow> FallRows(Baskin.BState B, IEnumerable<Baskin.Fx> fx, Balance rules)
        {
            var o = new List<RaidCardRow>();
            if (B == null || fx == null) return o;
            foreach (var f in fx.Where(f => f.K == "fall"))
            {
                var u = B.Units.FirstOrDefault(v => v.Id == f.Unit);
                if (u == null) continue;
                if (Baskin.IsSpecialist(u)) o.Add(new RaidCardRow { Icon = "ic_x_s", Text = Loc.F("Vagondan düşer · {0} vardiya yatar", FallShifts(rules)) });
                else if (Baskin.IsDog(u)) o.Add(new RaidCardRow { Icon = "ic_x_s", Text = Loc.T("Kenardan düşer") });
                else o.Add(new RaidCardRow { Icon = "ic_x_s", Text = Loc.T("Kenardan düşer · yok olur"), Good = u.Side == 'e' });
            }
            return o;
        }

        /// <summary>Önizleme hayaletinin kâğıdı (kenarın dışında): "DÜŞER".</summary>
        public static string FallsTag => Loc.T("DÜŞER");

        /// <summary>Ekip kartının durumu: kenardan düşen (bu odada) "VAGONDAN DÜŞTÜ"; önceki odada düşen "VAGONDAN DÜŞTÜ · PAFTA 2".</summary>
        public static string CardOverboard(int sheet = 0) => sheet > 0 ? Loc.F("VAGONDAN DÜŞTÜ · PAFTA {0}", sheet) : Loc.T("VAGONDAN DÜŞTÜ");

        /// <summary>Düşmenin telsiz fişi (ad ve önekli kare): ekip üyesi "… vagondan düştü · 2-C8", öbürleri "… kenardan düştü · 2-C8".</summary>
        public static string FallSlip(bool member, string name, string cell) =>
            member ? Loc.F("{0} vagondan düştü · {1}", name, cell) : Loc.F("{0} kenardan düştü · {1}", name, cell);

        /// <summary>Oda kapanınca telsiz satırı: "Vagondan düştü: Quinn · 2-C8 · 5 vardiya".</summary>
        public static string ClosedOverboard(string name, string cell, Balance rules) =>
            Loc.F("Vagondan düştü: {0} · {1} · {2} vardiya", name, cell, FallShifts(rules));

        /// <summary>Sonuç kartının ekip satırı eki: "· 1 vagondan düştü" (raporlarda <c>"k"</c> olan sayısı); yoksa null.</summary>
        public static string ResultOverboard(int count) => count > 0 ? Loc.F("· {0} vagondan düştü", count) : null;

        /// <summary>Koşu darbesinin Katar odasındaki ikinci etkisi (kart düğmesi): "kenardan düşerse yok olur".</summary>
        public static string RushEdgeEffect => Loc.T("kenardan düşerse yok olur");

        // ---------- Katar: AYIR ve takip ----------

        /// <summary>EMRİ İLET'in yazısı AYIR açıkken.</summary>
        public static string DetachLabel => Loc.T("AYIR");

        /// <summary>EMRİ İLET'in notu AYIR açıkken: "takip yok" (düğmenin not yuvası kısa: "GERİ ALINMAZ" boyu; Teknisyen'in yeteneği durum
        /// satırında ve kartta yazar).</summary>
        public static string DetachNote => Loc.T("takip yok");

        /// <summary>EMRİ İLET'in notu GEÇ'te takipçi varken: "1 makine takip eder"; takipçi yoksa null (bugünkü GERİ ALINMAZ).</summary>
        public static string FollowNote(int followers) => followers > 0 ? Loc.F("{0} makine takip eder", followers) : null;

        /// <summary>Durum satırı (Katar'ın dönüş geçidinde, herkes kapıdayken): AYIR verilebiliyorsa "Enter: GEÇ · Q: AYIR (Teknisyen)",
        /// Teknisyen yeteneğini kullandıysa "AYIR yok: …"; öbür durumlarda null.</summary>
        public static string DetachStatus(Baskin.BState B)
        {
            if (!IsKatarReturn(B) || !Baskin.CanPass(B)) return null;
            if (Baskin.CanDecouple(B)) return Loc.T("Enter: GEÇ · Q: AYIR (Teknisyen)");
            return Baskin.DecoupleBlock(B) == "acted" ? Loc.T("AYIR yok: Teknisyen bu tur yeteneğini kullandı") : null;
        }

        /// <summary>Teknisyen'in ekip kartının hak satırı AYIR verilebilirken: "AYIR · Q".</summary>
        public static string DetachCard => Loc.T("AYIR · Q");

        /// <summary>Takipçi makinenin tahta etiketi (GEÇ verilebilirken, AYIR kapalıyken).</summary>
        public static string FollowTag => Loc.T("TAKİP EDER");

        /// <summary>Takipçi makinenin hover satırı (dönüşte, Katar'da, bağlantıya menzil içinde; her zaman).</summary>
        public static RaidCardRow FollowRow(Baskin.BState B, Baskin.Unit u)
        {
            if (u == null || u.Side != 'e' || !Baskin.FollowerIds(B).Contains(u.Id)) return null;
            return new RaidCardRow { Icon = "ic_bolt_s", Text = Loc.F("Bağlantıya {0} kare içinde: GEÇ'te sonraki vagona gelir, AYIR'da kalır", B.Moves?.FollowRange ?? 2) };
        }

        /// <summary>Kapı fişi (herkes kapıda) Katar'ın dönüş geçidinde AYIR verilebilirken: "ekip kapıda · GEÇ ya da AYIR (Q)"; değilse null.</summary>
        public static string PassSlip(Baskin.BState B) => IsKatarReturn(B) && Baskin.CanDecouple(B) ? Loc.T("ekip kapıda · GEÇ ya da AYIR (Q)") : null;

        /// <summary>Geçiş fişi (pafta çevrilirken): AYIR'da "vagon ayrıldı · takip yok", takipte "1 makine takip ediyor"; ikisi de yoksa null.</summary>
        public static string PassedSlip(RoomReport r)
        {
            if (r == null) return null;
            if (r.Decouple) return Loc.T("vagon ayrıldı · takip yok");
            int n = r.Follow?.Count ?? 0;
            return n > 0 ? Loc.F("{0} makine takip ediyor", n) : null;
        }

        /// <summary>
        /// Yeni vagonun gözcü satırı (takipli geçişten sonra): "Gözcü · takip: 1 makine doğu geçidinde: 1-H4". Takipçiler Core'dan
        /// (<see cref="Baskin.FollowedInIds"/>: tahtaya gerçekten gelenler; makine sınırının kestiği takipçi ve donmuş odanın makineleri
        /// sayılmaz; aşama 12 kapanışı). Takip yoksa boş.
        /// </summary>
        public static List<RaidLogModel> SpotterFollow(Baskin.BState B)
        {
            var o = new List<RaidLogModel>();
            if (B?.Room == null || !B.Escape) return o;
            var ids = Baskin.FollowedInIds(B);
            var tail = B.Units.Where(u => ids.Contains(u.Id)).ToList();
            if (tail.Count == 0) return o;
            int room = B.Room.Index;
            string cells = string.Join(Loc.T(" · "), tail.Select(u => FacilityRaidText.Cell(room, u.X, u.Y)));
            o.Add(new RaidLogModel { Text = Loc.F("Gözcü · takip: {0} makine doğu geçidinde: {1}", tail.Count, cells), Kind = "bad" });
            return o;
        }

        /// <summary>Oda kapanınca telsiz satırları: AYIR'da "Vagon ayrıldı: PAFTA 2", takipte "Takip: 1 makine · PAFTA 1" (gidilen pafta).</summary>
        public static List<RaidLogModel> ClosedChain(RoomReport r)
        {
            var o = new List<RaidLogModel>();
            if (r == null) return o;
            if (r.Decouple) o.Add(new RaidLogModel { Text = Loc.F("Vagon ayrıldı: PAFTA {0}", r.Room), Kind = "good" });
            else if (r.Follow != null && r.Follow.Count > 0) o.Add(new RaidLogModel { Text = Loc.F("Takip: {0} makine · PAFTA {1}", r.Follow.Count, Math.Max(1, r.Room - 1)), Kind = "bad" });
            return o;
        }

        // ---------- Yalpa ----------

        /// <summary>Yalpa odasının gözcü satırı: "Gözcü · yalpa: 2. tur batı, 4. tur doğu" (odanın tur bütçesinin takvimi); öbür odalarda boş.</summary>
        public static List<RaidLogModel> SpotterTilt(Baskin.BState B)
        {
            var o = new List<RaidLogModel>();
            if (!IsYalpa(B)) return o;
            string calendar = TiltCalendar(B.MaxTurns);
            if (calendar != null) o.Add(new RaidLogModel { Text = Loc.F("Gözcü · yalpa: {0}", calendar), Kind = "sys" });
            return o;
        }

        /// <summary>Yatışın telsiz fişi (karesiz): "Yalpa · batıya" / "Yalpa · doğuya".</summary>
        public static string TiltSlip(int dir) => dir < 0 ? Loc.T("Yalpa · batıya") : Loc.T("Yalpa · doğuya");

        /// <summary>
        /// Yalpa odasında karenin hover satırları: yatış turunda kayacak birim ya da nesne "Yatışta 1 kare kayar" (ayrıntı yön), çarpacak birim
        /// "Yatışta çarpar" (−1) (Core <see cref="Baskin.Slides"/>); her turda cıvatalı makine "Cıvatalı: yatışta kaymaz", sabit nesne
        /// (hurda dışında) "Sabit: kaymaz". Öbür odalarda boş.
        /// </summary>
        public static List<RaidCardRow> TiltRows(Baskin.BState B, int x, int y)
        {
            var o = new List<RaidCardRow>();
            if (!IsYalpa(B)) return o;
            var u = Baskin.UnitAt(B, x, y);
            var obj = Baskin.CellAt(B, x, y).O;
            var line = Baskin.Slides(B).FirstOrDefault(l => l.X == x && l.Y == y);
            if (line != null)
            {
                string dir = DirWord(line.X2 - line.X);
                o.Add(line.Hit ? new RaidCardRow { Icon = "ic_bolt_s", Text = Loc.T("Yatışta çarpar"), Detail = Loc.T("−1") }
                    : new RaidCardRow { Icon = "ic_boots_s", Text = Loc.T("Yatışta 1 kare kayar"), Detail = dir });
            }
            if (u != null && u.Side == 'e' && Baskin.Bolted(B, u)) o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Cıvatalı: yatışta kaymaz") });
            if (u == null && obj != null && obj.K != "hurda") o.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Sabit: kaymaz") });
            return o;
        }
    }
}
