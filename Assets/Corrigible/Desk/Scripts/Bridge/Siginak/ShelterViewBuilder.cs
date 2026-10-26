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
    /// hasarlı = seviye 0 · enerjisiz = enerji isteyen oda (atölye, revir) ve üretim talebi karşılamıyor · çalışıyor.
    /// Sonuç notu yalnız öneri yazar (sunum kuralı): hasar, enerji açığı, açlık, boştaki kişi; sorun yoksa not yok.
    /// </summary>
    public static class ShelterViewBuilder
    {
        public static readonly string[] Order = { ShelterLogic.Living, ShelterLogic.Generator, ShelterLogic.Workshop, ShelterLogic.Infirmary };

        static string N(double v, int d = 0) => DeskFormat.Number(v, d);
        static string Bold(string s) => "<b>" + s + "</b>";
        static string Hex(string key) => ColorUtility.ToHtmlStringRGB(SiginakTheme.Active.ColorOf(key));
        static string Tint(string s, string key) => "<color=#" + Hex(key) + ">" + s + "</color>";
        static string Delta(double v, int d) => Tint(DeskFormat.Signed(v, d), v < 0 ? "status.threat" : "status.good");

        static bool Consumer(string id) => id == ShelterLogic.Workshop || id == ShelterLogic.Infirmary;

        public static RoomCondition Condition(Simulation sim, ShelterRoomState room)
        {
            if (room.Level <= 0) return RoomCondition.Damaged;
            var s = sim.Shelter;
            if (Consumer(room.Id) && ShelterLogic.EnergyCapacity(s, sim.B) < ShelterLogic.EnergyDemand(s, sim.B)) return RoomCondition.NoPower;
            return RoomCondition.Working;
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
                ShelterLogic.Living => Loc.F("{0} kişilik yer; görevli başına günde {1} erzak toplanır.", ShelterLogic.Capacity(s, b), N(b.shelterSuppliesPerWorker, 1)),
                ShelterLogic.Generator => Loc.F("Sığınağa enerji verir: seviye başına {0}, görevli başına {1}.", N(b.shelterEnergyPerLevel), N(b.shelterWorkerEnergy)),
                ShelterLogic.Workshop => Loc.F("Görevli başına günde {0} malzeme üretir; seviye başına {1} enerji ister.", N(b.shelterMaterialsPerWorker, 1), N(b.shelterRoomEnergy)),
                ShelterLogic.Infirmary => Loc.F("En az {0} görevliyle yaralı ekip her gün {1} gün daha hızlı iyileşir; seviye başına {2} enerji ister.",
                    b.shelterHealingWorkers, b.shelterHealingDays, N(b.shelterRoomEnergy)),
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
            double deficit = ShelterLogic.EnergyDemand(s, b) - ShelterLogic.EnergyCapacity(s, b);
            if (deficit > 0)
            {
                int need = (int)Math.Ceiling(deficit / Math.Max(1e-6, b.shelterWorkerEnergy));
                var consumers = s.Rooms.Where(r => Consumer(r.Id) && r.Level > 0).Select(r => ShelterLogic.RoomName(r.Id)).ToList();
                if (id == ShelterLogic.Generator)
                    return Loc.F("{0} için {1} enerji eksik: {2} personel ata ya da geliştir.", string.Join(", ", consumers), N(deficit), need);
                if (Consumer(id)) return Loc.F("Enerji yok: {0} enerji eksik, üretim durdu. Jeneratöre personel ata ya da geliştir.", N(deficit));
            }
            if (s.Starving && id == ShelterLogic.Living) return Loc.T("Açlık: üretim yarıya indi. Yaşam alanına personel ata.");
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
            double capacity = ShelterLogic.EnergyCapacity(s, b), demand = ShelterLogic.EnergyDemand(s, b);
            d.Summary.Add((Loc.T("TOPLULUK"), Loc.F("{0}/{1} · boşta {2}", Bold(s.Population.ToString()), Bold(ShelterLogic.Capacity(s, b).ToString()),
                Bold(ShelterLogic.AvailableWorkers(s).ToString()))));
            d.Summary.Add((Loc.T("ENERJİ"), Loc.F("{0} üretim / {1} talep {2}", Bold(N(capacity)), Bold(N(demand)),
                demand > capacity ? Bold(Tint(DeskFormat.Signed(capacity - demand, 0), "status.threat")) : "")));
            d.Summary.Add((Loc.T("ERZAK"), Bold(Loc.F("{0}/{1}", N(Math.Floor(s.Supplies)), N(ShelterLogic.SuppliesCapacity(s, b))))));
            d.Summary.Add((Loc.T("SON VARDİYA"), Loc.F("erzak {0} · malzeme {1}", Bold(Delta(s.LastSuppliesChange, 1)), Bold(Delta(s.LastMaterialsChange, 1)))));
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
            var missing = new List<string>();
            if (!top && sim.S.Fon < cost.Fon) missing.Add(Loc.F("{0} fon", N(Math.Ceiling(cost.Fon - sim.S.Fon))));
            if (!top && s.Materials < cost.Materials) missing.Add(Loc.F("{0} malzeme", N(Math.Ceiling(cost.Materials - s.Materials))));
            d.Cost = top ? Loc.T("EN ÜST SEVİYE")
                : Loc.F("{0} · {1}", verb, Tint(Loc.F("{0} fon + {1} malzeme", N(cost.Fon), N(cost.Materials)), "status.threat"));
            d.ReadinessOk = why == null;
            d.Readiness = top ? Loc.T("Oda en yüksek seviyede.")
                : why == null ? (room.Level <= 0 ? Loc.T("Onarıma hazır.") : Loc.T("Geliştirmeye hazır."))
                : missing.Count > 0 ? Loc.F("Eksik: {0}", string.Join(" + ", missing)) : why;
            d.Action = new ShelterButtonData
            {
                Label = verb,
                State = why == null ? SlipButtonState.Normal : !top && missing.Count > 0 ? SlipButtonState.Short : SlipButtonState.Disabled,
                Stamp = Loc.T("EKSİK"),
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
