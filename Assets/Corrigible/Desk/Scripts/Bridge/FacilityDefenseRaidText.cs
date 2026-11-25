using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis aşama 11 (Docs/TESIS-BASKINLARI-TASARIMI.md §9.2; aşama 11 tasarımı §8.7): üç odalı savunmanın baskın sahnesindeki metinleri.
    /// Antet, tur şeridinin dalga işaretleri, amaç ve dalga satırı, barikat turu, gözcü, telsiz, İÇERİ ÇEKİL düğmesi ve onayı, son tur ve
    /// geri çekilme onayları, sonuç kartı, kurallar kartının savunma bölümü ve hover satırları (saha rölesi, iz, batı girişi). Kaynak yalnız
    /// Core'un public okumalarıdır (<see cref="Baskin.IsDefense"/>, <see cref="Baskin.DefenseWaves"/>, <see cref="Baskin.DefenseIncoming"/>,
    /// <see cref="Baskin.RelayHp"/>, <see cref="Baskin.RelayMax"/>, <see cref="Baskin.BarricadeTurn"/>, <see cref="Baskin.Abandoned"/>,
    /// <see cref="RaidParams.Marks"/>, <see cref="Simulation.DefenseRoomTrust"/>): dalga takvimi, röle canı ve bedeller kopyalanmaz (K40).
    /// Saf: tahtayı ve raporları okur, değiştirmez; UnityEngine kullanmaz. Savunma değilse (<see cref="On"/> false) metin üretmez: saldırı
    /// baskını aşama 10'daki gibidir (iz hover'ı hariç: izli saldırı tahtasında da önceki baskının izini söyler).
    /// </summary>
    public static class FacilityDefenseRaidText
    {
        /// <summary>Tesis odasında savunma tahtası.</summary>
        public static bool On(Baskin.BState B) => B?.Room != null && Baskin.IsDefense(B);

        static int RoomOf(Baskin.BState B) => B?.Room?.Index ?? 0;
        static string Cell(int room, int x, int y) => RaidBoard3D.CellName(room, x, y);
        static RaidLogModel Line(string text, string kind) => new RaidLogModel { Text = text, Kind = kind };

        // ------------------------------------------------------------------ antet, şerit, amaç

        /// <summary>Tur şeridinin dalga işareti (numaranın yanında küçük harf): "D" (dalga).</summary>
        public static string WaveGlyph => Loc.T("D");

        /// <summary>
        /// Tur şeridi (<see cref="RaidViewModel.TurnEvents"/>): [tur] o turun başında bu odaya dalga gelir (Core <see cref="Baskin.DefenseWaves"/>,
        /// terk gecikmesi dahil); dizi uzunluğu süre + 1. Savunma değilse null.
        /// </summary>
        public static string[] TurnEvents(Baskin.BState B)
        {
            if (!On(B)) return null;
            var o = new string[Math.Max(1, B.MaxTurns) + 1];
            foreach (var w in Baskin.DefenseWaves(B))
                if (w.Arrive >= 1 && w.Arrive < o.Length) o[w.Arrive] = WaveGlyph;
            return o;
        }

        /// <summary>Bu odaya henüz gelmemiş ilk dalga (geliş sırasıyla); yoksa null.</summary>
        public static Baskin.DefenseWave NextWave(Baskin.BState B) =>
            !On(B) ? null : Baskin.DefenseWaves(B).Where(w => w.Arrive > B.Turn).OrderBy(w => w.Arrive).ThenBy(w => w.N).FirstOrDefault();

        /// <summary>Savunmanın dalga sayısı (dengeden; denge yoksa tahtanın bildiği en büyük dalga numarası).</summary>
        static int WaveTotal(Baskin.BState B, Balance rules) =>
            rules != null ? RaidChain.DefenseWaveCount(rules) : B.Waves.Count > 0 ? B.Waves.Max(w => w.N) : 0;

        /// <summary>Amaç satırı: "Amaç: 10. tura dayan" (hedef odasında "· röleyi koru"). Savunma değilse null.</summary>
        public static string Goal(Baskin.BState B)
        {
            if (!On(B)) return null;
            return B.Room.Type == RoomType.Target ? Loc.F("Amaç: {0}. tura dayan · röleyi koru", B.MaxTurns) : Loc.F("Amaç: {0}. tura dayan", B.MaxTurns);
        }

        /// <summary>
        /// Görev çipinin dalga satırı (can taşımaz, Max 0): "DALGA 2/3 · 3 makine · 5. tur" (bu odaya gelecek ilk dalga). Gelecek dalga
        /// yoksa ya da savunma değilse null.
        /// </summary>
        public static RaidTargetModel WaveRow(Baskin.BState B, Balance rules)
        {
            var w = NextWave(B);
            if (w == null) return null;
            return new RaidTargetModel
            {
                Name = Loc.F("DALGA {0}/{1} · {2} makine · {3}. tur", w.N, Math.Max(w.N, WaveTotal(B, rules)), w.Count, w.Arrive),
                Thumb = "ic_arrow_s", Hp = 0, Max = 0, Armor = false, Broken = false, Cell = null,
            };
        }

        /// <summary>Görev çipinin saha rölesi satırının adı.</summary>
        public static string RelayName => Loc.T(Baskin.ObjNames[Baskin.RelayKind]);

        /// <summary>Barikat turunun telsiz fişi (odanın ilk turu; makine yok).</summary>
        public static string BarricadeSlip => Loc.T("SAVUNMA · BARİKAT · makine yok");

        /// <summary>Görev çipinin başlığı: "{oda} · SAVUNMA".</summary>
        public static string Objective(string place) => string.IsNullOrEmpty(place) ? Loc.T("GÖREV · SAVUNMA") : Loc.F("{0} · SAVUNMA", place);

        // ------------------------------------------------------------------ gözcü ve telsiz

        /// <summary>
        /// Savunma odasının gözcü satırları (oda açılışında, <see cref="FacilityRaidText.Spotter"/>'dan sonra): barikat turu ve ilk dalga
        /// ("Gözcü · barikat turu · dalga 1: 2. tur, 2 makine, batı girişi"), terk edilen odadan gelecek takipçiler, önceki baskının izleri
        /// ("Gözcü · önceki baskından: 2 yıkık duvar, 1 tel"), hedef odasında saha rölesi. Savunma değilse yalnız iz satırı (izli saldırı tahtası).
        /// </summary>
        public static List<RaidLogModel> Spotter(Baskin.BState B)
        {
            var o = new List<RaidLogModel>();
            if (B?.Room == null) return o;
            int room = RoomOf(B);
            if (On(B))
            {
                var w = NextWave(B);
                o.Add(Line(w != null ? Loc.F("Gözcü · barikat turu · dalga {0}: {1}. tur, {2} makine, giriş kapısı", w.N, w.Arrive, w.Count)
                    : Loc.T("Gözcü · barikat turu · bu odaya dalga gelmez"), "bad"));
                int followers = Baskin.DefenseIncoming(B).Count(i => i.Wave == 0);
                if (followers > 0) o.Add(Line(Loc.F("Gözcü · terk edilen odadan {0} makine geliyor", followers), "bad"));
                if (B.Room.Type == RoomType.Target && RelayAt(B) is Baskin.Pt r)
                    o.Add(Line(Loc.F("Gözcü · saha rölesi: {0} · {1} can", Cell(room, r.X, r.Y), Baskin.RelayHp(B)), "sys"));
            }
            string traces = TraceList(B);
            if (traces != null) o.Add(Line(Loc.F("Gözcü · önceki baskından: {0}", traces), "sys"));
            return o;
        }

        /// <summary>Dalga gelişinin telsiz fişi: "DALGA 2 geldi: 3 makine".</summary>
        public static string WaveSlip(int wave, int count) => Loc.F("DALGA {0} geldi: {1} makine", wave, count);

        /// <summary>Takipçinin (terk edilen odadan gelen makine) ve dalga makinesinin gelişi: "makine geldi · 2-A4".</summary>
        public static string ArrivalSlip(string cell) => Loc.F("makine geldi · {0}", cell);

        /// <summary>
        /// Savunmada oda kapanınca ek telsiz satırları: içeri çekilmede "Oda terk edildi: PAFTA 1 · güven −1"; son odada savunmanın kapanışı
        /// ("Savunma bitti: kanat tutuldu · 10 tur" / "Savunma bitti: kanat düştü"). Savunma değilse boş.
        /// </summary>
        public static List<RaidLogModel> Closed(RaidParams p, RoomReport r, int roomTrust)
        {
            var o = new List<RaidLogModel>();
            if (r == null || !RaidChain.IsDefense(p)) return o;
            if (r.End == "pass") o.Add(Line(Loc.F("Oda terk edildi: PAFTA {0} · güven −{1}", r.Room, Math.Max(0, roomTrust)), "bad"));
            if (r.Last) o.Add(r.Success ? Line(Loc.F("Savunma bitti: kanat tutuldu · {0} tur", r.Turns), "good") : Line(Loc.T("Savunma bitti: kanat düştü"), "bad"));
            return o;
        }

        // ------------------------------------------------------------------ düğmeler ve onaylar

        /// <summary>Herkes ileri geçitteyken EMRİ İLET'in yazısı: "İÇERİ ÇEKİL".</summary>
        public static string PassLabel => Loc.T("İÇERİ ÇEKİL");

        /// <summary>İÇERİ ÇEKİL'in alt satırı: "güven −1".</summary>
        public static string PassNote(int roomTrust) => Loc.F("güven −{0}", Math.Max(0, roomTrust));

        /// <summary>İçeri çekilmenin onay başlığı.</summary>
        public static string PassTitle => Loc.T("İÇERİ ÇEKİLME");

        /// <summary>İçeri çekilmenin onay metni: "Bu oda terk edilir: güven −1, izler silinir." ve yerde yatan her kişi için "Yerde yatan {ad} ölür.".</summary>
        public static string PassWarning(int roomTrust, IEnumerable<string> lying)
        {
            var lines = new List<string> { Loc.F("Bu oda terk edilir: güven −{0}, izler silinir.", Math.Max(0, roomTrust)) };
            if (lying != null) lines.AddRange(lying.Where(n => !string.IsNullOrEmpty(n)).Select(n => Loc.F("Yerde yatan {0} ölür.", n)));
            return string.Join(Loc.T("\n"), lines);
        }

        /// <summary>Savunmanın son turu: onay başlığı ve metni.</summary>
        public static string LastTurnTitle => Loc.T("SON TUR · SAVUNMA BİTER");
        public static string LastTurnWarning => Loc.T("Ayakta biri kalırsa kanat tutulur.");

        /// <summary>GERİ ÇEKİL'in onay metni: "Savunma kaybedilir: kanat düşer, güven −{n}. Yerde yatanlar ölür." (kanadın düşüşü + terk edilen odalar).</summary>
        public static string RetreatBody(int trust) => Loc.F("Savunma kaybedilir: kanat düşer, güven −{0}. Yerde yatanlar ölür.", Math.Max(0, trust));

        /// <summary>
        /// Kurallar kartının kazanma satırının yerine savunma bölümü (dört satır): barikat, dalga, içeri çekilme, röle ve süre.
        /// </summary>
        public static string Rules(int turns, int roomTrust) => string.Join(Loc.T("\n"), new[]
        {
            Loc.T("Savunma · barikat: odanın ilk turu makinesizdir; ekibi yerleştir, engel kur."),
            Loc.T("Savunma · dalga: makineler giriş kapısından gelir; tur şeridinde D işaretli turlarda."),
            Loc.F("Savunma · içeri çekil: herkes ileri geçitteyken oda terk edilir (güven −{0}, izler silinir). Kapı sığınak değildir: kapıdaki uzman tur sonunda tahtaya döner.", Math.Max(0, roomTrust)),
            Loc.F("Savunma · röle: hedef odasındaki saha rölesi kırılırsa kanat düşer. {0}. tur sonunda ayakta biri varsa kanat tutulur.", turns),
        });

        // ------------------------------------------------------------------ sonuç kartı

        /// <summary>Sonuç kartının damgası: "KANAT TUTULDU", röle kırıldıysa "RÖLE KIRILDI", değilse "KANAT DÜŞTÜ".</summary>
        public static string ResultTitle(bool held, bool relayBroken) =>
            held ? Loc.T("KANAT TUTULDU") : relayBroken ? Loc.T("RÖLE KIRILDI") : Loc.T("KANAT DÜŞTÜ");

        /// <summary>Sonuç kartının tur satırı: "TUR 10/10 · PAFTA 2/3".</summary>
        public static string ResultTurn(int turn, int turns, int sheet) => Loc.F("TUR {0}/{1} · PAFTA {2}/3", turn, turns, sheet);

        /// <summary>Sonuç kartının satırları: saha rölesi, terk edilen oda, kırılan sivil sunucu, ekip (dört satır).</summary>
        public static RaidResultRow[] ResultRows(bool held, int relayHp, int relayMax, bool relayReached, int abandoned, int roomTrust, int collateral,
            int standing, int team, int extracted)
        {
            var relay = new RaidResultRow
            {
                Label = RelayName,
                Value = relayReached && relayHp < 0 ? Loc.T("kırıldı") : relayReached ? Loc.F("{0}/{1}", relayHp, relayMax) : Loc.F("{0}/{1} · ulaşılmadı", relayMax, relayMax),
                Good = !(relayReached && relayHp < 0),
            };
            var room = new RaidResultRow { Label = Loc.T("Terk edilen oda"), Value = Loc.F("{0} · güven −{1}", abandoned, abandoned * Math.Max(0, roomTrust)), Good = abandoned == 0 };
            var civ = new RaidResultRow { Label = Loc.T("Kırılan sivil sunucu"), Value = collateral.ToString() };
            var squad = new RaidResultRow
            {
                Label = Loc.T("Ekip"),
                Value = extracted > 0 ? Loc.F("{0}/{1} ayakta · {2} çıkarıldı", standing, team, extracted) : Loc.F("{0}/{1} ayakta", standing, team),
                Good = held && standing == team,
            };
            return new[] { relay, room, civ, squad };
        }

        // ------------------------------------------------------------------ hover

        /// <summary>Saha rölesinin hover satırları: can ve kural, makinelerin hedef alması.</summary>
        public static List<RaidCardRow> RelayRows(Baskin.Obj o)
        {
            var rows = new List<RaidCardRow>();
            if (o?.K != Baskin.RelayKind) return rows;
            rows.Add(new RaidCardRow { Icon = "ic_x_s", Text = Loc.F("Saha rölesi · {0} can · kırılırsa kanat düşer", o.Hp) });
            rows.Add(new RaidCardRow { Icon = "ic_eye_s", Text = Loc.T("Makineler ekip hedef odasındayken röleyi de hedef alır") });
            return rows;
        }

        /// <summary>
        /// Savunmada batı girişi karesi: başlık ve satır ("Dalga buradan girer (5. tur)"; gelecek dalga yoksa "Bu odaya başka dalga gelmez").
        /// Savunma değilse ya da kare batı girişi değilse null.
        /// </summary>
        public static (string Title, RaidCardRow Row)? WestEntry(Baskin.BState B, int x, int y)
        {
            if (!On(B) || !B.Room.West.Any(w => w.X == x && w.Y == y)) return null;
            var w = NextWave(B);
            var row = new RaidCardRow { Icon = "ic_arrow_s", Text = w != null ? Loc.F("Dalga buradan girer ({0}. tur)", w.Arrive) : Loc.T("Bu odaya başka dalga gelmez") };
            return (Loc.T("Giriş kapısı"), row);
        }

        /// <summary>Bu odanın, tahtaya uygulanmış izleri (emirdeki <see cref="RaidParams.Marks"/>; kare tahtada okunur, Core'un uygulamasıyla).</summary>
        public static List<FacilityMark> AppliedMarks(Baskin.BState B)
        {
            var o = new List<FacilityMark>();
            var marks = B?.Params?.Marks;
            if (B?.Room == null || marks == null) return o;
            foreach (var m in marks)
            {
                if (m == null || m.Room != B.Room.Id || m.X < 0 || m.Y < 0 || m.X >= Baskin.N || m.Y >= Baskin.N) continue;
                if (Applied(Baskin.CellAt(B, m.X, m.Y), m)) o.Add(m);
            }
            return o;
        }

        /// <summary>İz tahtanın karesine uygulanmış mı (bina planının okuması gibi: kare tahtada okunur).</summary>
        static bool Applied(Baskin.Cell c, FacilityMark m) => m.Kind switch
        {
            FacilityMarks.Wall => c.T == "floor" && !c.Shutter,
            FacilityMarks.Shutter => c.Shutter && c.T == "floor",
            FacilityMarks.Device => c.O == null,
            FacilityMarks.Cool or FacilityMarks.Spawner => c.O == null,
            _ => c.O?.K == m.Kind,
        };

        /// <summary>Odanın izleri sözle, türüne göre: "2 yıkık duvar, 1 tel"; iz yoksa null.</summary>
        public static string TraceList(Baskin.BState B)
        {
            var marks = AppliedMarks(B);
            if (marks.Count == 0) return null;
            return string.Join(Loc.T(", "), marks.GroupBy(m => m.Kind).Select(g => Loc.F("{0} {1}", g.Count(), FacilityDefenseText.KindName(g.Key))));
        }

        /// <summary>Karenin iz satırı: "Önceki baskından: yıkık duvar" (engelde kalan canıyla). İz yoksa boş.</summary>
        public static List<RaidCardRow> TraceRows(Baskin.BState B, int x, int y) =>
            AppliedMarks(B).Where(m => m.X == x && m.Y == y).Take(1)
                .Select(m => new RaidCardRow { Icon = "ic_eye_s", Text = Loc.F("Önceki baskından: {0}", FacilityDefenseText.KindName(m.Kind)) }).ToList();

        /// <summary>Gidişte izle kırık gelen donanım (emirde donanım izi ve karesi boş): görev çipinde "KIRILDI" satırı. Yoksa null.</summary>
        public static Baskin.Pt? MarkedDevice(Baskin.BState B)
        {
            if (B?.Room?.Device == null || B.Escape || !(B.Room.DeviceAt is Baskin.Pt d)) return null;
            if (Baskin.CellAt(B, d.X, d.Y).O != null) return null;
            return AppliedMarks(B).Any(m => m.Kind == FacilityMarks.Device) ? d : (Baskin.Pt?)null;
        }

        /// <summary>Saha rölesinin karesi; tahtada yoksa null.</summary>
        public static Baskin.Pt? RelayAt(Baskin.BState B)
        {
            if (B == null) return null;
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (Baskin.CellAt(B, x, y).O?.K == Baskin.RelayKind) return new Baskin.Pt(x, y);
            return null;
        }

        /// <summary>Savunmada gelecek tur gelecek makinelerin tahmini kareleri (Core <see cref="Baskin.DefenseIncoming"/>; tahtada "robot
        /// gelecek" işareti). Savunma değilse boş.</summary>
        public static List<Baskin.Pt> IncomingTiles(Baskin.BState B) =>
            !On(B) ? new List<Baskin.Pt>() : Baskin.DefenseIncoming(B).Where(i => i.X >= 0 && i.Y >= 0 && i.Turn == B.Turn + 1).Select(i => new Baskin.Pt(i.X, i.Y)).ToList();
    }
}
