using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Sığınak (K4) görünüm durumu: kesit maketinin odaları ve kıskaçlı panonun içeriği. Simülasyonu yalnız okur; karar
    /// <c>ShelterUpgradeCheck</c> / <c>ShelterAssignCheck</c>'indir, burada sayılar ve metin biçimlenir. Oda hâli:
    /// hasarlı = seviye 0 · durdu = atölye plaka dökemiyor (depo dolu, malzeme yok) ya da revir açlıkta · çalışıyor.
    /// Sonuç notu yalnız öneri yazar (sunum kuralı): hasar, durma nedeni, görevsiz oda, açlık, boştaki kişi; sorun yoksa not yok.
    /// </summary>
    public static class ShelterViewBuilder
    {
        public static readonly string[] Order = { ShelterLogic.Living, ShelterLogic.Radio, ShelterLogic.Workshop, ShelterLogic.Infirmary };

        static string N(double v, int d = 0) => DeskFormat.Number(v, d);
        /// <summary>Oran: tam sayı ondalıksız (“1”), değilse gereken kadar ondalık (“0,9”, “0,25”).</summary>
        public static string Rate(double v) => N(v, Math.Abs(v - Math.Round(v)) < 1e-9 ? 0 : Math.Abs(v * 10 - Math.Round(v * 10)) < 1e-9 ? 1 : 2);
        static string Bold(string s) => "<b>" + s + "</b>";

        /// <summary>Atölye plaka dökemiyor: depo dolu ya da bir plakalık malzeme yok.</summary>
        static bool WorkshopStopped(Simulation sim) =>
            sim.Shelter.Plates >= ShelterLogic.PlateCapacity(sim.Shelter, sim.B) || sim.Shelter.Materials < sim.B.shelterPlateMaterials;

        public static RoomCondition Condition(Simulation sim, ShelterRoomState room)
        {
            if (room.Level <= 0) return RoomCondition.Damaged;
            if (room.Id == ShelterLogic.Workshop && WorkshopStopped(sim)) return RoomCondition.Stopped;
            if (room.Id == ShelterLogic.Infirmary && sim.Shelter.Starving) return RoomCondition.Stopped;
            return RoomCondition.Working;
        }

        /// <summary>Uzmanların durumu: “T 3/3 · E 2/3 · A yaralı (2)”.</summary>
        public static string Squad(Simulation sim)
        {
            var team = sim.S.Teams.FirstOrDefault();
            if (team == null) return "—";
            return string.Join(" · ", team.Members.Select(m => m.Down > 0
                ? Loc.F("{0} yaralı ({1})", Baskin.UnitTypes[m.Role].Short, m.Down)
                : Loc.F("{0} {1}/{2}", Baskin.UnitTypes[m.Role].Short, Simulation.MaxHp(m.Role) - m.Wounds, Simulation.MaxHp(m.Role))));
        }

        /// <summary>Bu vardiyada onarıldı ya da geliştirildi (kayıttaki komut, bugünün günü).</summary>
        public static bool Fresh(Simulation sim, string roomId) =>
            sim.Record?.commands.Any(c => c.kind == "shelter_upgrade" && c.a == roomId && c.day == sim.S.Day) == true;

        public static List<ShelterRoomData> Rooms(Simulation sim, string selected)
        {
            var list = new List<ShelterRoomData>();
            var s = sim?.Shelter;
            if (s == null) return list;
            foreach (var id in Order)
            {
                var r = s.Room(id);
                if (r == null) continue;
                int max = sim.B.shelterMaxRoomLevel, cap = ShelterLogic.WorkerCapacity(r, sim.B);
                list.Add(new ShelterRoomData
                {
                    Id = id, Name = DeskText.Upper(ShelterLogic.RoomName(id)), Level = r.Level, MaxLevel = max, Workers = r.Workers, Capacity = cap,
                    Condition = Condition(sim, r), Selected = id == selected, Fresh = Fresh(sim, id),
                    // Revizyon 2 · S1: rafta da okunan iki satır, “SV 1 · 2/2” (en üst seviyede “SV 3/3 · 4/6”)
                    Line = r.Level >= max
                        ? Loc.F("SV {0}/{1} · {2}/{3}", r.Level, max, r.Workers, cap)
                        : Loc.F("SV {0} · {1}/{2}", r.Level, r.Workers, cap),
                    Active = Active(sim, r),
                    Occupants = id == ShelterLogic.Living ? s.Reserves.Count(x => x.Down <= 0)
                        : id == ShelterLogic.Infirmary && r.Level > 0 && Wounded(sim) ? 1 : 0,
                });
            }
            return list;
        }

        /// <summary>
        /// Odanın çıktısı görünür mü (S3 › odaların çıktısı): yaşam alanında görevli varsa erzak kasası, telsiz odasında görevli
        /// varsa kadran ve amber lamba, atölyede plaka dökülürken kalıp.
        /// </summary>
        static bool Active(Simulation sim, ShelterRoomState r) => r.Level > 0 && (r.Id switch
        {
            ShelterLogic.Living => r.Workers > 0,
            ShelterLogic.Radio => ShelterLogic.RadioWorkers(sim.Shelter) > 0,
            ShelterLogic.Workshop => r.Workers > 0 && sim.Shelter.PlateProgress > 0 && !WorkshopStopped(sim),
            _ => false,
        });

        /// <summary>Yatan uzman var (ekipte ya da yedeklerde): revirin yatağında sargılı hasta.</summary>
        static bool Wounded(Simulation sim) =>
            sim.S.Teams.SelectMany(t => t.Members).Any(m => m.Down > 0) || sim.Shelter.Reserves.Any(r => r.Down > 0);

        /// <summary>
        /// Erzak kaç vardiya yeter (S3 › özet): ⌊erzak ÷ |vardiya farkı|⌋. Fark ≥ 0 ise null (ERZAK ARTIYOR). Saf hesap.
        /// </summary>
        public static int? ShiftsLeft(double supplies, double perShift)
        {
            if (perShift >= 0) return null;
            return (int)Math.Floor(Math.Max(0, supplies) / -perShift + 1e-9);
        }

        /// <summary>
        /// Vardiyadaki erzak farkı: son vardiyanınki; ilk vardiyada (henüz vardiya geçmedi) bugünkü görevli ve nüfusla beklenen.
        /// </summary>
        public static double SuppliesPerShift(Simulation sim)
        {
            var s = sim.Shelter;
            if (sim.S.Day > 0) return s.LastSuppliesChange;
            return (s.Room(ShelterLogic.Living)?.Workers ?? 0) * sim.B.shelterSuppliesPerWorker - s.Population * sim.B.shelterSuppliesPerResident;
        }

        /// <summary>
        /// DEPO bandının sayıları (S3; Soru 6 ara kararı a): 10 sandık = erzak sınırı (sandık 3 / 6 / 9 erzak), 5 kova = malzeme
        /// deposu (kova 12), son sandık yarım, son kova kısmi; plaka rafı 3 yuva, atölye seviyesi kadar açık. Saf hesap.
        /// </summary>
        public static ShelterStoreData Store(double supplies, double capacity, double materials, double storage, int plates, int plateCapacity)
        {
            var d = new ShelterStoreData
            {
                CrateValue = capacity / ShelterStoreData.CrateSlots,
                BinValue = storage / ShelterStoreData.BinSlots,
                PlateCapacity = Math.Max(0, Math.Min(plateCapacity, ShelterStoreData.PlateSlots)),
            };
            d.Plates = Math.Max(0, Math.Min(plates, d.PlateCapacity));
            if (d.CrateValue > 0)
            {
                double crates = Math.Max(0, supplies) / d.CrateValue;
                d.Crates = Math.Min(ShelterStoreData.CrateSlots, (int)Math.Floor(crates + 1e-9));
                d.CratePartial = d.Crates < ShelterStoreData.CrateSlots && crates - d.Crates > 1e-6;
            }
            if (d.BinValue > 0)
            {
                double bins = Math.Max(0, materials) / d.BinValue;
                d.Bins = Math.Min(ShelterStoreData.BinSlots, (int)Math.Ceiling(bins - 1e-9));
                d.LastBinFill = d.Bins > 0 ? Math.Min(1, bins - (d.Bins - 1)) : 1;
            }
            return d;
        }

        /// <summary>Simülasyondan DEPO: sayılar ve tebeşir (“ERZAK · 15 VARDİYA” / “ERZAK ARTIYOR”).</summary>
        public static ShelterStoreData Store(Simulation sim)
        {
            var s = sim.Shelter;
            var b = sim.B;
            var d = Store(s.Supplies, ShelterLogic.SuppliesCapacity(s, b), s.Materials, b.shelterMaterialsStorage, s.Plates, ShelterLogic.PlateCapacity(s, b));
            int? left = ShiftsLeft(s.Supplies, SuppliesPerShift(sim));
            d.Slate = left == null ? Loc.T("ERZAK ARTIYOR") : Loc.F("ERZAK · {0} VARDİYA", left.Value);
            return d;
        }

        /// <summary>Oda açıklaması, Balance değerleriyle (kural tekrar yazılmaz; yalnız sayılar okunur).</summary>
        public static string Description(Simulation sim, string id)
        {
            var b = sim.B;
            var s = sim.Shelter;
            return id switch
            {
                ShelterLogic.Living => Loc.F("{0} kişilik yer ve {1} yedek uzman; görevli başına günde {2} erzak toplanır.",
                    ShelterLogic.Capacity(s, b), ShelterLogic.ReserveCapacity(s, b), N(b.shelterSuppliesPerWorker, 1)),
                ShelterLogic.Radio => Loc.F("Görevli başına vardiya alarmı {0} yavaşlar; şimdi vardiyada +{1}.",
                    Rate(b.shelterRadioAlarmPerWorker), Rate(sim.AlarmPerShift)),
                ShelterLogic.Workshop => NextSpecialist(sim) is string next
                    ? Loc.F("Görevli başına günde {0} zırh plakası döker (plaka {1} malzeme); depo {2} plaka. {3}",
                        Rate(b.shelterPlatesPerWorker), N(b.shelterPlateMaterials), ShelterLogic.PlateCapacity(s, b), next)
                    : Loc.F("Görevli başına günde {0} zırh plakası döker (plaka {1} malzeme); depo {2} plaka.",
                        Rate(b.shelterPlatesPerWorker), N(b.shelterPlateMaterials), ShelterLogic.PlateCapacity(s, b)),
                ShelterLogic.Infirmary => Loc.F("Görevli başına günde {0} yara sarar; en az {1} görevliyle yaralı uzman {2} gün erken kalkar.",
                    Rate(b.shelterHealPerWorker), b.shelterHealingWorkers, b.shelterHealingDays),
                _ => ShelterLogic.RoomDescription(id),
            };
        }

        /// <summary>Atölyenin açacağı sıradaki uzman: “Seviye 2: Madenci kadroya katılır.”; hepsi katıldıysa null.</summary>
        public static string NextSpecialist(Simulation sim)
        {
            var team = sim.S.Teams.FirstOrDefault();
            string role = Simulation.Roles.Where(r => !Simulation.HasRole(team, r)).OrderBy(r => Simulation.UnlockLevel(r, sim.B)).FirstOrDefault();
            return role == null ? null : Loc.F("Seviye {0}: {1} kadroya katılır.", Simulation.UnlockLevel(role, sim.B), Baskin.UnitTypes[role].Name);
        }

        /// <summary>Sonuç notu (sarı yapışkan): odalar arası etki ve öneri; sorun yoksa null.</summary>
        public static string Note(Simulation sim, string id)
        {
            var s = sim.Shelter;
            var b = sim.B;
            var room = s.Room(id);
            if (room == null) return null;
            if (room.Level <= 0) return Loc.T("Hasarlı: onarılana kadar çalışmaz, personel alamaz.");
            if (id == ShelterLogic.Workshop && WorkshopStopped(sim))
                return s.Plates >= ShelterLogic.PlateCapacity(s, b)
                    ? Loc.F("Depo dolu: {0} plaka hazır. Kırılan plakanın yerine yenisi dökülür; geliştirirsen depo büyür.", s.Plates)
                    : Loc.F("Malzeme yetmiyor: plaka {0} malzeme ister. İkmal baskını malzeme getirir.", N(b.shelterPlateMaterials));
            if (id == ShelterLogic.Infirmary && s.Starving) return Loc.T("Açlık: revir durdu. Yaşam alanına personel ata.");
            bool hurt = sim.S.Teams.SelectMany(t => t.Members).Any(m => m.Wounds > 0 || m.Down > 0);
            if (id == ShelterLogic.Infirmary && hurt && room.Workers < b.shelterHealingWorkers) return Loc.T("Yaralı uzman var: revire personel ata.");
            if (id == ShelterLogic.Radio && room.Workers == 0) return Loc.F("Görevli yok: alarm her vardiya +{0} yükselir.", Rate(sim.AlarmPerShift));
            if (s.Starving && id == ShelterLogic.Living) return Loc.T("Açlık: atölye yarı hızda, revir durdu. Yaşam alanına personel ata.");
            int idle = ShelterLogic.AvailableWorkers(s);
            if (idle > 0 && room.Workers < ShelterLogic.WorkerCapacity(room, b))
                return Loc.F("Boşta {0} kişi var: + ile bu odaya görevlendir.", idle);
            return null;
        }

        /// <summary>
        /// Uzmanların diskleri (S3), kadro sırasıyla: rol harfi + can; yatan uzmanın diski soluk, yanında kalkmasına kalan gün;
        /// henüz katılmamış uzmanın diski soluk, yanında açılacağı atölye seviyesi (“sv 2”; ayrıntısı atölyenin açıklamasında).
        /// </summary>
        public static List<SlipMember> SquadDiscs(Simulation sim)
        {
            var list = new List<SlipMember>();
            var team = sim.S.Teams.FirstOrDefault();
            if (team == null) return list;
            foreach (var role in Simulation.Roles)
            {
                var m = team.Members.FirstOrDefault(x => x.Role == role);
                if (m == null)
                {
                    list.Add(new SlipMember
                    {
                        Letter = Baskin.UnitTypes[role].Short, Hp = Loc.F("sv {0}", Simulation.UnlockLevel(role, sim.B)), Missing = true,
                    });
                    continue;
                }
                int max = Simulation.MaxHp(m.Role);
                list.Add(new SlipMember
                {
                    Letter = Baskin.UnitTypes[m.Role].Short,
                    Hp = m.Down > 0 ? Loc.F("{0} gün", m.Down) : Loc.F("{0}/{1}", max - m.Wounds, max),
                    Missing = m.Down > 0,
                });
            }
            return list;
        }

        public static ShelterBoardData Board(Simulation sim, string id)
        {
            var d = new ShelterBoardData();
            var s = sim?.Shelter;
            if (s == null) return d;
            var b = sim.B;
            d.Title = Loc.T("SIĞINAK ÖZETİ");
            d.Shift = Loc.F("V.{0:000}", sim.S.Day + 1);

            // Asıl soru: erzak kaç vardiya yeter (3 ve altı kırmızı + AZ KALDI; açlıkta AÇLIK); artıyorsa ERZAK ARTIYOR
            int? left = ShiftsLeft(s.Supplies, SuppliesPerShift(sim));
            d.SuppliesLabel = left == null ? Loc.T("ERZAK") : Loc.T("ERZAK YETER");
            d.SuppliesBig = left == null ? Loc.T("ARTIYOR") : Loc.F("{0} VARDİYA", left.Value);
            d.SuppliesLow = s.Starving || left != null && left.Value <= 3;
            d.SuppliesStamp = s.Starving ? Loc.T("AÇLIK") : d.SuppliesLow ? Loc.T("AZ KALDI") : null;
            d.SuppliesLine = Loc.F("{0} · son vardiya {1}", Bold(Loc.F("{0}/{1}", N(Math.Floor(s.Supplies)), N(ShelterLogic.SuppliesCapacity(s, b)))),
                Bold(DeskFormat.Signed(s.LastSuppliesChange, 1)));
            int reserves = s.Reserves.Count(r => r.Down <= 0);
            d.Community = Loc.F("{0}/{1} · boşta {2} · yedek {3}", Bold(s.Population.ToString()), Bold(ShelterLogic.Capacity(s, b).ToString()),
                Bold(ShelterLogic.AvailableWorkers(s).ToString()), Bold(reserves.ToString()));
            d.Squad.AddRange(SquadDiscs(sim));
            d.Plates = Loc.F("{0} hazır · döküm %{1}", Bold(Loc.F("{0}/{1}", s.Plates, ShelterLogic.PlateCapacity(s, b))),
                Bold(N(Math.Floor(s.PlateProgress * 100))));

            var room = s.Room(id);
            if (room == null) return d;
            int max = b.shelterMaxRoomLevel, cap = ShelterLogic.WorkerCapacity(room, b), idle = ShelterLogic.AvailableWorkers(s);
            d.RoomId = id;
            d.RoomName = DeskText.Upper(ShelterLogic.RoomName(id));
            d.SelectedChip = Loc.T("SEÇİLİ");
            d.RoomLine = Loc.F("SEVİYE {0}/{1} · PERSONEL {2}/{3}", room.Level, max, room.Workers, cap);
            d.Description = Description(sim, id);
            d.Note = Note(sim, id);

            // Personel adımlayıcısı [−] a/b [+]: devre dışında üzerine gelince neden; boşta kimse yokken altında not
            bool full = cap > 0 && room.Workers >= cap;
            string minusWhy = sim.ShelterAssignCheck(id, room.Workers - 1), plusWhy = sim.ShelterAssignCheck(id, room.Workers + 1);
            d.Staff = Loc.F("{0}/{1}", room.Workers, cap);
            d.Minus = new ShelterButtonData
            {
                Label = "−",
                State = minusWhy == null ? SlipButtonState.Normal : SlipButtonState.Disabled,
                Hover = minusWhy == null ? null : room.Workers <= 0 ? Loc.T("Görevli yok") : minusWhy,
            };
            d.Plus = new ShelterButtonData
            {
                Label = "+",
                State = plusWhy == null ? SlipButtonState.Normal : SlipButtonState.Disabled,
                Hover = plusWhy == null ? null : full ? Loc.T("Oda dolu") : idle <= 0 ? Loc.T("Boşta kimse yok") : plusWhy,
            };
            d.StaffNote = plusWhy != null && !full && room.Level > 0 && idle <= 0 ? Loc.T("Boşta kimse yok") : null;

            // GELİŞTİRME: HASARLI → ONAR · 1–2 → GELİŞTİR · 3 → EN ÜST SEVİYE damgası. Yetersiz kaynakta düğme açık kalır,
            // eksik kalem çizik + “yetersiz” (basınca teleks YAPILAMADI); kilitli seviyede devre dışı, altında gerekçe
            d.Top = room.Level >= max;
            if (d.Top) return d;
            string verb = room.Level <= 0 ? Loc.T("ONAR") : Loc.T("GELİŞTİR");
            var cost = ShelterLogic.UpgradeCost(s, id, b);
            string why = sim.ShelterUpgradeCheck(id);
            string locked = sim.ShelterLevelLock(id);
            bool fonShort = sim.S.Fon < cost.Fon, materialsShort = s.Materials < cost.Materials;
            string fon = Loc.F("{0} fon", N(cost.Fon)), materials = Loc.F("{0} malzeme", N(cost.Materials));
            bool shortOnly = why != null && locked == null && (fonShort || materialsShort);
            d.Action = new ShelterButtonData
            {
                Label = shortOnly
                    ? Loc.F("{0} · {1} + {2} · yetersiz", verb, fonShort ? "<s>" + fon + "</s>" : fon, materialsShort ? "<s>" + materials + "</s>" : materials)
                    : Loc.F("{0} · {1} + {2}", verb, fon, materials),
                State = why == null ? SlipButtonState.Normal : shortOnly ? SlipButtonState.Short : SlipButtonState.Disabled,
                Hover = why == null ? null : shortOnly ? TelexReasons.Upgrade(sim, id) : locked ?? why,
            };
            d.ActionNote = why == null || shortOnly ? null : locked ?? why;
            return d;
        }
    }
}
