using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Vardiya raporu (Revizyon 2 · S5): kol inmeden önceki ve sonraki anlık görüntüden, vardiyanın fon hareketlerinden ve
    /// teleksin YAPILAMADI satırlarından raporu kurar. Başta üç göz (ERZAK n vardiya yeter · YZ ALARMI ölçekle · FON); erzak ve
    /// alarm dökümde bir daha yazılmaz. Karşı saldırıda raporun başına kutu girer, alarm gözü eşikte kırmızıdır. Sayılar
    /// simülasyondan gelir; formüller yalnız açıklama satırıdır (Soru 6): ayrı satırlar <see cref="Balance"/> değerlerinden,
    /// toplam simülasyonun net değişiminden. İkisi tutmazsa (depo sınırı, erzakın bitmesi) araya düzeltme satırı girer.
    /// İşaret ve renk kuralı (+ yeşil, − mürekkep, ±0 gri, kırmızı yalnız eşik ve işgal) <see cref="ToneColor"/>'da.
    /// Saf C#: girdiden metin üretir, sahneye dokunmaz.
    /// </summary>
    public static class ShiftReportBuilder
    {
        /// <summary>YAPILAMADI bölümünde en çok bu kadar satır; fazlası “… ve n satır daha”.</summary>
        public const int MaxFailures = 4;

        // ---------- Girdi ----------

        /// <summary>Simülasyonun şu anki hâli (vardiya = gün + 1).</summary>
        public static ShiftSnapshot Capture(Simulation sim)
        {
            var s = sim.Shelter;
            var b = sim.B;
            var lib = sim.S.Liberation;
            var snap = new ShiftSnapshot
            {
                shift = sim.S.Day + 1, fon = sim.S.Fon,
                freeRegions = lib.Regions.Count(r => r.Completed), regions = lib.Regions.Count,
                population = s.Population, idle = ShelterLogic.AvailableWorkers(s),
                supplies = s.Supplies, suppliesCapacity = ShelterLogic.SuppliesCapacity(s, b),
                materials = s.Materials, materialsCapacity = b.shelterMaterialsStorage,
                plates = s.Plates, plateCapacity = ShelterLogic.PlateCapacity(s, b), plateProgress = s.PlateProgress,
                reserves = s.Reserves.Count(r => r.Down <= 0),
                starving = s.Starving,
                alarm = sim.S.Alarm, alarmThreshold = b.liberationAlarmThreshold,
                counterattacks = sim.S.Counterattacks, lastCounterattack = sim.S.LastCounterattack,
                doorVisits = sim.S.Door?.Log.Count ?? 0,
            };
            foreach (var r in s.Rooms) snap.rooms.Add(new ShiftRoomSnapshot { id = r.Id, level = r.Level, workers = r.Workers });
            foreach (var t in sim.S.Teams)
            {
                var team = new ShiftTeamSnapshot { id = t.Id, name = t.Name, busy = !Simulation.TeamReady(t), canRaid = sim.SquadCanRaid(t) };
                foreach (var m in t.Members) team.members.Add(new ShiftMemberSnapshot { role = m.Role, wounds = m.Wounds, down = m.Down, benched = m.Role != "tek" && !(t.LastPick?.Contains(m.Role) ?? false) });
                snap.teams.Add(team);
            }
            return snap;
        }

        /// <summary>Bu vardiyanın fon hareketleri (gelir hariç; geliri adım yazar). Kol inmeden önce alınır.</summary>
        public static List<ShiftLedgerEntry> Ledger(Simulation sim)
        {
            int day = sim.S.Day;
            var moves = sim.TransactionLedger.Where(t => t.Day == day && t.Kind != "income" && t.Fon != 0).ToList();
            // Ödül kaydı baskın sonucunun kodunu taşır; sektör aynı günün başarılı baskın kayıtlarından sırayla
            var wins = sim.S.RaidLog.Where(l => l.Day == day && l.Success && l.SectorId != null).ToList();
            int win = 0;
            var list = new List<ShiftLedgerEntry>();
            for (int i = 0; i < moves.Count; i++)
            {
                var t = moves[i];
                var e = new ShiftLedgerEntry { kind = t.Kind, id = t.Id, fon = t.Fon };
                switch (t.Kind)
                {
                    case "liberation_raid":
                        e.number = (sim.LiberationSector(t.Id)?.Index ?? -1) + 1;
                        break;
                    case "raidres":
                        if (win < wins.Count) { e.id = wins[win].SectorId; e.number = (sim.LiberationSector(e.id)?.Index ?? -1) + 1; win++; }
                        break;
                    case "shelter_upgrade":
                        int later = moves.Skip(i + 1).Count(x => x.Kind == "shelter_upgrade" && x.Id == t.Id);
                        e.number = (sim.Shelter.Room(t.Id)?.Level ?? 0) - later;
                        break;
                }
                list.Add(e);
            }
            return list;
        }

        public static ShiftRates Rates(Balance b) => new ShiftRates
        {
            dailyFunds = b.liberationDailyFunds, regionIncome = b.liberationRegionIncome,
            suppliesPerWorker = b.shelterSuppliesPerWorker, suppliesPerResident = b.shelterSuppliesPerResident,
            starvingMultiplier = b.shelterStarvingWorkMultiplier, workersPerLevel = b.shelterWorkersPerLevel,
            platesPerWorker = b.shelterPlatesPerWorker, plateMaterials = b.shelterPlateMaterials, healPerWorker = b.shelterHealPerWorker,
            healingDays = b.shelterHealingDays, healingWorkers = b.shelterHealingWorkers, restHeal = b.liberationRestHeal,
            counterattackTrust = b.liberationCounterattackTrust,
        };

        /// <summary>Adımdan sonra: kapanış öncesi görüntü + defter + hatalarla raporun girdisi (teleks kodu sonra yazılır).</summary>
        public static ShiftReportInput Input(Simulation sim, ShiftSnapshot before, List<ShiftLedgerEntry> ledger, IEnumerable<TelexLine> failures)
        {
            var income = sim.S.LastIncome;
            return new ShiftReportInput
            {
                before = before, after = Capture(sim), ledger = ledger ?? new List<ShiftLedgerEntry>(),
                incomeCentral = income.Central, incomeRegional = income.RegionalBase,
                failures = failures?.ToList() ?? new List<TelexLine>(),
                rates = Rates(sim.B),
                door = (sim.S.Door?.Log ?? new List<DoorVisit>()).Skip(before.doorVisits).Where(v => v.Auto).Select(v => new ShiftDoorEntry
                {
                    card = v.Card, result = v.Result, supplies = v.Supplies, materials = v.Materials, alarm = v.Alarm, trust = v.Trust,
                    counterattack = v.Counterattack,
                }).ToList(),
            };
        }

        /// <summary>Kapanan vardiyanın YAPILAMADI satırları (teleks kodu “VVV/…”).</summary>
        public static IEnumerable<TelexLine> Failures(IEnumerable<TelexLine> telex, int shift)
        {
            string prefix = shift.ToString("000", CultureInfo.InvariantCulture) + "/";
            return telex.Where(l => l.kind == TelexKind.Failed && l.code != null && l.code.StartsWith(prefix, StringComparison.Ordinal));
        }

        // ---------- Rapor ----------

        public static ShiftReportData Build(ShiftReportInput input)
        {
            var b = input.before;
            var a = input.after;
            var d = new ShiftReportData
            {
                Head = Loc.T("TELEKS · KYGA-07 → KOMUTA"),
                Code = input.code ?? "",
                Title = Loc.F("VARDİYA RAPORU · {0:000}", b.shift),
                Sub = Loc.F("{0:000} → {1:000} · bir gün ilerledi", b.shift, a.shift),
                Stamp = Loc.T("KAPANDI"),
                FundsTitle = Loc.T("FON · GELİR / GİDER"),
                ProductionTitle = Loc.T("ÜRETİM VE TÜKETİM"),
                TeamTitle = Loc.T("EKİP"),
                FailedTitle = Loc.T("YAPILAMADI"),
                Action = Loc.F("VARDİYA {0} BAŞLAT", TrNumber.WithAccusative(a.shift, "000", true)),
                Hint = Loc.T("rapor EVRAK tepsisine dosyalanır"),
            };

            Funds(input, d);
            Production(input, d);
            Summary(input, d);
            Counterattack(input, d);
            d.Analyst = Analyst(input);
            Teams(input, d);
            Door(input, d);
            foreach (var f in input.failures.Take(MaxFailures)) d.Failures.Add((f.code, f.message));
            if (input.failures.Count > MaxFailures) d.MoreFailures = Loc.F("… ve {0} satır daha", input.failures.Count - MaxFailures);

            d.Slips[0] = (Delta(a.shift - b.shift, 0), Tone(a.shift - b.shift, 0));
            d.Slips[1] = (Delta(a.fon - b.fon, 1), Tone(a.fon - b.fon, 1));
            d.Slips[2] = (Delta(a.supplies - b.supplies, 1), Tone(a.supplies - b.supplies, 1));
            d.Slips[3] = (Delta(a.materials - b.materials, 1), Tone(a.materials - b.materials, 1));
            return d;
        }

        static void Funds(ShiftReportInput input, ShiftReportData d)
        {
            var b = input.before;
            var a = input.after;
            var r = input.rates;
            d.Opening = b.fon - input.ledger.Sum(e => e.fon);
            d.Income = input.incomeCentral + input.incomeRegional;
            d.Closing = a.fon;
            d.Funds.Add(Row(Loc.T("Açılış"), null, DeskFormat.Number(d.Opening, 1), ReportTone.Plain));
            foreach (var e in input.ledger) d.Funds.Add(Row(LedgerLabel(e), null, Delta(e.fon, 1), Tone(e.fon, 1)));
            d.Funds.Add(Row(Loc.T("Vardiya geliri"), Loc.F("{0} + {1} × {2} özgür bölge", Compact(r.dailyFunds), Compact(r.regionIncome), b.freeRegions),
                Delta(d.Income, 1), Tone(d.Income, 1)));
            // Adımda gelirden başka fon hareketi olduysa (bugün yok) rapor yine kapanışa çıkar
            double other = a.fon - b.fon - d.Income;
            if (Math.Abs(other) >= 0.05) d.Funds.Add(Row(Loc.T("Diğer"), null, Delta(other, 1), Tone(other, 1)));
            d.Funds.Add(Total(Loc.T("Kapanış"), DeskFormat.Number(d.Closing, 1), ReportTone.Plain));
        }

        static void Production(ShiftReportInput input, ShiftReportData d)
        {
            var b = input.before;
            var a = input.after;
            var r = input.rates;
            int gatherers = b.Room(ShelterLogic.Living)?.workers ?? 0;
            double gather = gatherers * r.suppliesPerWorker, eat = b.population * r.suppliesPerResident;
            d.Production.Add(Row(Loc.T("Erzak toplama"), Loc.F("yaşam alanı · {0} kişi", gatherers), Delta(gather, 1), Tone(gather, 1)));
            d.Production.Add(Row(Loc.T("Erzak tüketimi"), Loc.F("{0} kişi × {1}", b.population, Compact(r.suppliesPerResident)), Delta(-eat, 1), Tone(-eat, 1)));
            d.SuppliesNet = a.supplies - b.supplies;
            // Vardiya biterken kapıda kalan ziyaretçinin erzağı (açılmadı sayıldı)
            double door = input.door.Sum(x => x.supplies);
            if (Math.Abs(door) >= 0.05) d.Production.Add(Row(Loc.T("Kapı · açılmadı"), DoorWho(input.door.First(x => Math.Abs(x.supplies) >= 0.05).card), Delta(door, 1), Tone(door, 1)));
            double rest = d.SuppliesNet - (gather - eat) - door;
            if (Math.Abs(rest) >= 0.05)
            {
                string why = b.supplies + gather - eat > b.suppliesCapacity
                    ? Loc.F("Depo sınırı · {0}", Compact(b.suppliesCapacity))
                    : Loc.T("Erzak tükendi");
                d.Production.Add(Row(why, null, Delta(rest, 1), Tone(rest, 1)));
            }

            // Atölye: plaka dökümü (malzeme harcar); depo dolu ya da malzeme yoksa döküm bekler
            var workshop = b.Room(ShelterLogic.Workshop);
            d.MaterialsNet = a.materials - b.materials;
            int cast = a.plates - b.plates;
            string note = null, badge = null;
            if (workshop == null || workshop.level <= 0) badge = Loc.T("HASARLI");
            else if (b.plates >= b.plateCapacity) badge = Loc.T("DEPO DOLU");
            else if (cast == 0 && b.materials < r.plateMaterials) badge = Loc.T("MALZEME YOK");
            else note = a.starving
                ? Loc.F("{0} kişi × {1} · açlık × {2} · plaka {3} malzeme", workshop.workers, Compact(r.platesPerWorker), Compact(r.starvingMultiplier), Compact(r.plateMaterials))
                : Loc.F("{0} kişi × {1} · plaka {2} malzeme", workshop.workers, Compact(r.platesPerWorker), Compact(r.plateMaterials));
            d.Production.Add(new ReportRow
            {
                Label = Loc.F("Zırh plakası · {0}/{1}", a.plates, a.plateCapacity), Note = note, Badge = badge,
                Value = Delta(cast, 0), Tone = Tone(cast, 0),
            });
            // Erzakın toplamı özetin ERZAK gözünde (dökümde tekrar edilmez)
        }

        /// <summary>
        /// Üç göz (S5): ERZAK (yeni değer / sınır, “n vardiya yeter” S3'ün hesabıyla), YZ ALARMI (değer / eşik, on adımlık
        /// ölçek, “4 → 5 · 10'da karşı saldırı”; karşı saldırıda eşik kırmızı “9 → 10 · EŞİK”), FON (önce → sonra). Alarm notu
        /// özetin altında (karşı saldırıda yok).
        /// </summary>
        static void Summary(ShiftReportInput input, ShiftReportData d)
        {
            var b = input.before;
            var a = input.after;
            double net = a.supplies - b.supplies;
            int? left = ShelterViewBuilder.ShiftsLeft(a.supplies, net);
            d.Supplies = new ReportEye
            {
                Label = Loc.T("ERZAK"), Delta = Delta(net, 1), DeltaTone = Tone(net, 1),
                Value = Over(DeskFormat.Number(a.supplies, 1), Compact(a.suppliesCapacity)),
                Line = a.supplies <= 0.05 ? Loc.T("erzak bitti") : left == null ? Loc.T("erzak artıyor") : Loc.F("{0} vardiya yeter", left.Value),
            };

            int steps = (int)Math.Round(a.alarmThreshold);
            d.AlarmSteps = steps;
            d.AlarmHighFrom = Math.Max(1, steps - 2);
            if (a.counterattacks > b.counterattacks)
            {
                // Alarm eşiğe vardı ve sıfırlandı: göz eşiği gösterir
                double rise = a.alarmThreshold - b.alarm;
                d.Alarm = new ReportEye
                {
                    Label = Loc.T("YZ ALARMI"), Delta = Delta(rise, Places(rise)), DeltaTone = ReportTone.Alert, Alert = true, Hover = Loc.T("duvarda: YZ ALARMI"),
                    Value = Over(Compact(a.alarmThreshold), Compact(a.alarmThreshold)),
                    Line = Loc.F("{0} → {1} · EŞİK", Compact(b.alarm), Compact(a.alarmThreshold)),
                };
                d.AlarmCells = steps;
            }
            else
            {
                d.Alarm = new ReportEye
                {
                    Label = Loc.T("YZ ALARMI"), Delta = Delta(a.alarm - b.alarm, Places(a.alarm - b.alarm)), DeltaTone = Math.Abs(a.alarm - b.alarm) < 0.05 ? ReportTone.Neutral : ReportTone.Plain,
                    Hover = Loc.T("duvarda: YZ ALARMI"),
                    Value = Over(Compact(a.alarm), Compact(a.alarmThreshold)),
                    Line = Loc.F("{0} → {1} · {2} karşı saldırı", Compact(b.alarm), Compact(a.alarm), TrNumber.WithLocative(steps, "0")),
                };
                d.AlarmCells = Math.Max(0, Math.Min(steps, (int)Math.Floor(a.alarm + 1e-9)));
                d.AlarmNote = Loc.T("Alarmı her vardiya ve her başarısız baskın yükseltir, kırılan YZ sunucusu düşürür. Eşikte YZ son geri alınan sektörü yeniden işgal eder.");
            }

            // FON gözü vardiyanın bütününü gösterir (açılış → kapanış; baskın emri ve onarım dahil)
            double fon = a.fon - d.Opening;
            d.FundsEye = new ReportEye
            {
                Label = Loc.T("FON"), Delta = Delta(fon, 1), DeltaTone = Tone(fon, 1), Value = DeskFormat.Number(a.fon, 1),
                Line = Loc.F("{0} → {1}", DeskFormat.Number(d.Opening, 1), DeskFormat.Number(a.fon, 1)),
            };
        }

        /// <summary>Alarm adımı tam sayıysa ondalıksız (“+1”), değilse bir ondalık (“+0,5”; telsiz odası yavaşlatır).</summary>
        static int Places(double v) => Math.Abs(v - Math.Round(v)) < 1e-9 ? 0 : 1;

        /// <summary>Büyük sayı ve küçük sınır: “20,6<size=20>/30</size>”.</summary>
        static string Over(string value, string limit) => value + "<size=20>/" + limit + "</size>";

        /// <summary>
        /// Karşı saldırı (S5b): raporun başında çift çizgili kutu “YZ KARŞI SALDIRISI” + teleks kodu + sektör; teleks iletisi
        /// (güven kaybıyla) ayrıca <see cref="ShiftReportData.Counterattack"/>.
        /// </summary>
        static void Counterattack(ShiftReportInput input, ShiftReportData d)
        {
            var b = input.before;
            var a = input.after;
            if (a.counterattacks <= b.counterattacks) return;
            string sector = SectorLabel(a.lastCounterattack);
            d.Counterattack = Loc.F("{0} yeniden işgal altında · sivil güven −{1}", sector, input.rates.counterattackTrust);
            d.CounterTitle = Loc.T("YZ KARŞI SALDIRISI");
            d.CounterCode = input.counterCode ?? "";
            d.CounterText = Loc.F("{0} yeniden işgal altında.", sector);
        }

        /// <summary>Kapı satırı (ekiplerin altında): vardiya biterken kapıda kalan ziyaretçi açılmadı sayıldı.</summary>
        static void Door(ShiftReportInput input, ShiftReportData d)
        {
            foreach (var e in input.door)
            {
                var visit = new DoorVisit { Card = e.card, Supplies = e.supplies, Materials = e.materials, Alarm = e.alarm, Trust = e.trust, Counterattack = e.counterattack };
                string effects = DoorViewBuilder.Effects(visit);
                d.Teams.Add(new ReportTeamRow
                {
                    Label = Loc.F("Kapı · {0}", DoorWho(e.card)), Value = Loc.T("AÇILMADI"), HideIcon = true,
                    Detail = string.Join(" ", new[] { e.result == null ? null : Loc.T(e.result), effects == null ? null : effects + "." }.Where(x => x != null)),
                });
            }
        }

        static string DoorWho(string card) => DoorCards.Exists(card) ? Loc.T(DoorCards.Get(card).Who) : "—";

        /// <summary>“Bölge · 01 Sektör” (sektör kimliği “weu_1”); geri alınacak sektör yoksa “—”.</summary>
        static string SectorLabel(string id)
        {
            int cut = id?.LastIndexOf('_') ?? -1;
            if (cut <= 0 || !int.TryParse(id.Substring(cut + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) return "—";
            var region = GameData.Region(id.Substring(0, cut));
            return Loc.F("{0} · {1:00} {2}", region.Name, n, region.Sector(n - 1));
        }

        /// <summary>
        /// Ekip satırı: uzmanların toplam yarası önce → sonra; altında dinlenme (baskına çıkmayan vardiya), revirin sardığı yara,
        /// yatan uzmanlar ve Teknisyen yatıp yedek de yoksa ekibin ne zaman hazır olacağı.
        /// </summary>
        static void Teams(ShiftReportInput input, ShiftReportData d)
        {
            var b = input.before;
            var a = input.after;
            var r = input.rates;
            foreach (var t in a.teams)
            {
                string name = LiberationMapBuilder.TeamName(t.id, t.name);
                var bt = b.Team(t.id) ?? t;
                int from = bt.members.Where(m => m.down <= 0).Sum(m => m.wounds), to = t.members.Where(m => m.down <= 0).Sum(m => m.wounds);
                var lying = t.members.Where(m => m.down > 0).ToList();
                var back = t.members.Where(m => m.down <= 0 && (bt.Member(m.role)?.down ?? 0) > 0).ToList();
                if (from == 0 && to == 0 && lying.Count == 0 && back.Count == 0)
                {
                    d.Teams.Add(new ReportTeamRow { Label = Loc.F("{0} · hazır", name), Value = Loc.T("HAZIR"), Tone = ReportTone.Good });
                    continue;
                }
                // Dinlenme önce sarar (baskına çıkmayan her ayaktaki uzman), kalan iyileşme revirindir
                int rested = bt.members.Where(m => m.down <= 0 && (!bt.busy || m.benched)).Sum(m => Math.Min(m.wounds, r.restHeal));
                int healed = Math.Max(0, from - to - rested);
                var lines = new List<string>();
                if (rested > 0) lines.Add(Loc.F("Dinlenme {0} yara toparladı.", rested));
                if (healed > 0) lines.Add(Loc.F("Revir {0} yara sardı.", healed));
                else if (to > 0 || lying.Count > 0) { string idle = InfirmaryIdle(b, a, r); if (idle != null) lines.Add(idle); }
                foreach (var m in back) lines.Add(Loc.F("{0} ayağa kalktı.", Role(m.role)));
                foreach (var m in lying) lines.Add(Loc.F("{0} yaralı: {1} gün.", Role(m.role), m.down));
                if (!t.canRaid)
                {
                    var tech = t.Member("tek");
                    int perShift = 1 + (healed > 0 || InfirmaryIdle(b, a, r) == null ? r.healingDays : 0);
                    lines.Add(Loc.F("Teknisyen kalkana kadar baskın yok: vardiya {0} hazır.", TrNumber.WithLocative(a.shift + ((tech?.down ?? 1) + perShift - 1) / perShift)));
                }
                // Teknisyen kalktı ya da bir yedek hazır: ekip sığınaktan cepheye döner
                if (!bt.canRaid && t.canRaid) d.TeamReturned = true;
                d.Teams.Add(new ReportTeamRow
                {
                    Label = lying.Count > 0 ? Loc.F("{0} · {1} yaralı", name, lying.Count) : Loc.F("{0} · uzmanlar", name),
                    Value = from > 0 || to > 0 ? Loc.F("yara {0} → {1}", from, to)
                        : lying.Count > 0 ? (t.canRaid ? Loc.T("YEDEKLE HAZIR") : Loc.T("BEKLİYOR")) : Loc.T("HAZIR"),
                    Tone = from == 0 && to == 0 && t.canRaid ? ReportTone.Good : ReportTone.Plain,
                    Detail = string.Join(" ", lines),
                });
            }
        }

        static string Role(string role) => Baskin.UnitTypes.TryGetValue(role ?? "", out var type) ? type.Name : role;

        /// <summary>Revir neden yara sarmadı (ShelterLogic.Advance koşulları); sarabiliyorsa null.</summary>
        static string InfirmaryIdle(ShiftSnapshot b, ShiftSnapshot a, ShiftRates r)
        {
            var room = b.Room(ShelterLogic.Infirmary);
            if (room == null || room.level <= 0) return Loc.T("Revir hasarlı: yara sarılmadı.");
            if (room.workers < r.healingWorkers) return Loc.T("Revirde görevli yok: yara sarılmadı.");
            if (a.starving) return Loc.T("Açlık: revir durdu.");
            return null;
        }

        /// <summary>Toplayıcılar yetmezken erzak bu kadar vardiyada biterse analist ikmal baskını önerir.</summary>
        const int SuppliesWarnShifts = 6;

        /// <summary>
        /// Analist notu (Kalam, tek satır, yalnız öneri): yeni vardiyanın hâlinden en önemli tek iş. Erzak eksiye gidiyor →
        /// yaşam alanı (yer yoksa ve erzak yakında bitiyorsa ikmal baskını), yaralı uzman → revir, telsizde kimse yok → telsiz,
        /// boşta kişi → iş. Öneri yoksa null.
        /// </summary>
        static string Analyst(ShiftReportInput input)
        {
            var a = input.after;
            var r = input.rates;
            bool hurt = a.teams.Any(t => t.members.Any(m => m.wounds > 0 || m.down > 0));
            var living = a.Room(ShelterLogic.Living);
            double gather = (living?.workers ?? 0) * r.suppliesPerWorker, eat = a.population * r.suppliesPerResident;
            if (gather < eat && living != null && r.suppliesPerWorker > 0)
            {
                int need = (int)Math.Ceiling((eat - gather) / r.suppliesPerWorker - 1e-9);
                if (need <= a.idle && need <= living.level * r.workersPerLevel - living.workers)
                    return Loc.F("→ yaşam alanına {0} kişi, erzak artıya geçer", need);
                // Toplayıcılar yetmiyor: erzak birkaç vardiyada biterse ikmal baskını
                int runway = (int)Math.Floor(a.supplies / (eat - gather) + 1e-9);
                if (runway <= SuppliesWarnShifts)
                    return runway <= 0 ? Loc.T("→ erzak bitti, ikmale çık") : Loc.F("→ erzak {0} vardiyaya yeter, ikmale çık", runway);
            }
            if (a.idle <= 0 && !hurt) return null;
            if (hurt)
            {
                var infirmary = a.Room(ShelterLogic.Infirmary);
                if (infirmary == null || infirmary.level <= 0) return Loc.T("→ revir onarılırsa yaralar sarılır");
                if (infirmary.workers < r.healingWorkers && a.idle > 0)
                    return Loc.F("→ revire {0} kişi, yaralar sarılır", r.healingWorkers - infirmary.workers);
            }
            var radio = a.Room(ShelterLogic.Radio);
            if (radio != null && radio.level > 0 && radio.workers == 0 && a.idle > 0) return Loc.T("→ telsize 1 kişi, alarm yavaşlar");
            return a.idle > 0 ? Loc.F("→ boştaki {0} kişiye iş ver", a.idle) : null;
        }

        static string LedgerLabel(ShiftLedgerEntry e)
        {
            switch (e.kind)
            {
                case "liberation_raid": return Loc.F("Baskın emri · OP-{0:00}", e.number);
                case "raidres": return Loc.F("Baskın ödülü · OP-{0:00}", e.number);
                case "door_answer": return Loc.F("Kapı · {0}", DoorWho(e.id));
                case "shelter_upgrade":
                    return e.number <= 1
                        ? Loc.F("Onarım · {0}", ShelterLogic.RoomName(e.id))
                        : Loc.F("Geliştirme · {0} SV{1}", ShelterLogic.RoomName(e.id), e.number);
                default: return Loc.T("Diğer");
            }
        }

        // ---------- Biçim ----------

        /// <summary>İşaretli değişim; sıfır “±0” (gri). Eksi U+2212.</summary>
        public static string Delta(double v, int decimals) =>
            Math.Round(v, decimals, MidpointRounding.AwayFromZero) == 0 ? "±0" : DeskFormat.Signed(v, decimals);

        public static ReportTone Tone(double v, int decimals)
        {
            double rounded = Math.Round(v, decimals, MidpointRounding.AwayFromZero);
            return rounded > 0 ? ReportTone.Good : rounded < 0 ? ReportTone.Bad : ReportTone.Neutral;
        }

        /// <summary>
        /// Renk kuralı (Revizyon 2 · S5; rapor ve ± fişleri): artı yeşil, eksi mürekkep (işaret “−” taşır), ±0 gri, düz değer
        /// mürekkep; kırmızı yalnız alarm eşiği ve işgal. Tema renk anahtarı döner.
        /// </summary>
        public static string ToneColor(ReportTone tone) => ReportTones.ColorKey(tone);

        /// <summary>Formül sayısı: tam sayı ondalıksız (“2”, “40”), değilse gereken kadar ondalık (“0,5”, “0,25”).</summary>
        static string Compact(double v) =>
            DeskFormat.Number(v, Math.Abs(v - Math.Round(v)) < 1e-9 ? 0 : Math.Abs(v * 10 - Math.Round(v * 10)) < 1e-9 ? 1 : 2);

        static ReportRow Row(string label, string note, string value, ReportTone tone) =>
            new ReportRow { Label = label, Note = note, Value = value, Tone = tone };

        static ReportRow Total(string label, string value, ReportTone tone) =>
            new ReportRow { Label = label, Value = value, Tone = tone, Total = true };
    }
}
