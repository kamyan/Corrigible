using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

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
        static string Hex(string key) => ColorUtility.ToHtmlStringRGB(SiginakTheme.Active.ColorOf(key));
        static string Tint(string s, string key) => "<color=#" + Hex(key) + ">" + s + "</color>";
        static string Delta(double v, int d) => Tint(DeskFormat.Signed(v, d), v < 0 ? "status.threat" : "status.good");

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
                    // K8: en üst seviyede kartta “3/3”
                    Line = r.Level >= max
                        ? Loc.F("SEVİYE {0}/{1} · PERSONEL {2}/{3}", r.Level, max, r.Workers, cap)
                        : Loc.F("SEVİYE {0} · PERSONEL {1}/{2}", r.Level, r.Workers, cap),
                });
            }
            return list;
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
                ShelterLogic.Workshop => Loc.F("Görevli başına günde {0} zırh plakası döker (plaka {1} malzeme); depo {2} plaka.",
                    Rate(b.shelterPlatesPerWorker), N(b.shelterPlateMaterials), ShelterLogic.PlateCapacity(s, b)),
                ShelterLogic.Infirmary => Loc.F("Görevli başına günde {0} yara sarar; en az {1} görevliyle yaralı uzman {2} gün erken kalkar.",
                    Rate(b.shelterHealPerWorker), b.shelterHealingWorkers, b.shelterHealingDays),
                _ => ShelterLogic.RoomDescription(id),
            };
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
                return Loc.F("Boşta {0} kişi var: + PERS. ile bu odaya görevlendir.", idle);
            return null;
        }

        public static ShelterBoardData Board(Simulation sim, string id)
        {
            var d = new ShelterBoardData();
            var s = sim?.Shelter;
            if (s == null) return d;
            var b = sim.B;
            d.Title = Loc.T("SIĞINAK ÖZETİ");
            d.Shift = Loc.F("V.{0:000}", sim.S.Day + 1);
            int reserves = s.Reserves.Count(r => r.Down <= 0);
            d.Summary.Add((Loc.T("TOPLULUK"), Loc.F("{0}/{1} · boşta {2} · yedek {3}", Bold(s.Population.ToString()), Bold(ShelterLogic.Capacity(s, b).ToString()),
                Bold(ShelterLogic.AvailableWorkers(s).ToString()), Bold(reserves.ToString()))));
            d.Summary.Add((Loc.T("UZMANLAR"), Bold(Squad(sim))));
            d.Summary.Add((Loc.T("ERZAK"), Loc.F("{0} · son vardiya {1}", Bold(Loc.F("{0}/{1}", N(Math.Floor(s.Supplies)), N(ShelterLogic.SuppliesCapacity(s, b)))),
                Bold(Delta(s.LastSuppliesChange, 1)))));
            d.Summary.Add((Loc.T("PLAKA"), Loc.F("{0} hazır · döküm %{1}", Bold(Loc.F("{0}/{1}", s.Plates, ShelterLogic.PlateCapacity(s, b))),
                N(Math.Floor(s.PlateProgress * 100)))));
            d.SummaryStamp = s.Starving ? Loc.T("AÇLIK") : null;

            var room = s.Room(id);
            if (room == null) return d;
            int max = b.shelterMaxRoomLevel, cap = ShelterLogic.WorkerCapacity(room, b);
            d.RoomId = id;
            d.RoomName = DeskText.Upper(ShelterLogic.RoomName(id));
            d.SelectedChip = Loc.T("SEÇİLİ");
            d.RoomLine = Loc.F("SEVİYE {0}/{1} · PERSONEL {2}/{3}", room.Level, max, room.Workers, cap);
            d.Description = Description(sim, id);
            d.Note = Note(sim, id);

            // Eylem: HASARLI → ONAR · 1–2 → GELİŞTİR · 3 → EN ÜST SEVİYE (devre dışı). Kaynak yetmezse EKSİK + “Eksik: …”
            bool top = room.Level >= max;
            string verb = room.Level <= 0 ? Loc.T("ONAR") : top ? Loc.T("EN ÜST SEVİYE") : Loc.T("GELİŞTİR");
            var cost = ShelterLogic.UpgradeCost(s, id, b);
            string why = sim.ShelterUpgradeCheck(id);
            // Seviye 2 ve 3 özgür bölgeyle açılır: kilitliyken eksik kaynak yazılmaz
            string locked = top ? null : sim.ShelterLevelLock(id);
            var missing = new List<string>();
            if (!top && sim.S.Fon < cost.Fon) missing.Add(Loc.F("{0} fon", N(Math.Ceiling(cost.Fon - sim.S.Fon))));
            if (!top && s.Materials < cost.Materials) missing.Add(Loc.F("{0} malzeme", N(Math.Ceiling(cost.Materials - s.Materials))));
            d.Cost = top ? Loc.T("EN ÜST SEVİYE")
                : Loc.F("{0} · {1}", verb, Tint(Loc.F("{0} fon + {1} malzeme", N(cost.Fon), N(cost.Materials)), "status.threat"));
            d.ReadinessOk = why == null;
            d.Readiness = top ? Loc.T("Oda en yüksek seviyede.")
                : locked != null ? locked
                : why == null ? (room.Level <= 0 ? Loc.T("Onarıma hazır.") : Loc.T("Geliştirmeye hazır."))
                : missing.Count > 0 ? Loc.F("Eksik: {0}", string.Join(" + ", missing)) : why;
            d.Action = new ShelterButtonData
            {
                Label = verb,
                State = why == null ? SlipButtonState.Normal : locked == null && !top && missing.Count > 0 ? SlipButtonState.Short : SlipButtonState.Disabled,
                Stamp = locked != null ? Loc.T("KİLİTLİ") : Loc.T("EKSİK"),
            };
            d.Minus = new ShelterButtonData
            {
                Label = Loc.T("− PERS."),
                State = sim.ShelterAssignCheck(id, room.Workers - 1) == null ? SlipButtonState.Normal : SlipButtonState.Disabled,
            };
            bool full = cap > 0 && room.Workers >= cap;
            d.Plus = new ShelterButtonData
            {
                Label = Loc.T("+ PERS."),
                State = sim.ShelterAssignCheck(id, room.Workers + 1) == null ? SlipButtonState.Normal : full ? SlipButtonState.Short : SlipButtonState.Disabled,
                Stamp = Loc.T("DOLU"),
            };
            return d;
        }
    }
}
