using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Vardiya raporu (Docs/UI_Siginak Aşama 8, K6b): kol inmeden önceki ve sonraki anlık görüntüden, vardiyanın fon
    /// hareketlerinden ve teleksin YAPILAMADI satırlarından rapor satırlarını kurar. Sayılar simülasyondan gelir; formüller
    /// yalnız açıklama satırıdır (Soru 6): ayrı satırlar <see cref="Balance"/> değerlerinden, toplam simülasyonun net
    /// değişiminden. İkisi tutmazsa (depo sınırı, erzakın bitmesi) araya düzeltme satırı girer; rapor hep simülasyonla
    /// aynı sonuca çıkar. Saf C#: girdiden metin üretir, sahneye dokunmaz.
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
                energyCapacity = ShelterLogic.EnergyCapacity(s, b), energyDemand = ShelterLogic.EnergyDemand(s, b),
                starving = s.Starving,
            };
            foreach (var r in s.Rooms) snap.rooms.Add(new ShiftRoomSnapshot { id = r.Id, level = r.Level, workers = r.Workers });
            foreach (var t in sim.S.Teams) snap.teams.Add(new ShiftTeamSnapshot { id = t.Id, name = t.Name, down = t.Down });
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
            materialsPerWorker = b.shelterMaterialsPerWorker, starvingMultiplier = b.shelterStarvingWorkMultiplier,
            workerEnergy = b.shelterWorkerEnergy, workersPerLevel = b.shelterWorkersPerLevel,
            healingDays = b.shelterHealingDays, healingWorkers = b.shelterHealingWorkers,
        };

        /// <summary>Adımdan sonra: kapanış öncesi görüntü + defter + hatalarla raporun girdisi (teleks kodu sonra yazılır).</summary>
        public static ShiftReportInput Input(Simulation sim, ShiftSnapshot before, List<ShiftLedgerEntry> ledger, IEnumerable<TelexLine> failures)
        {
            var income = sim.S.LastIncome;
            return new ShiftReportInput
            {
                before = before, after = Capture(sim), ledger = ledger ?? new List<ShiftLedgerEntry>(),
                incomeCentral = income.Central, incomeRegional = income.RegionalBase + income.BudgetBonus,
                failures = failures?.ToList() ?? new List<TelexLine>(),
                rates = Rates(sim.B),
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
                TeamTitle = Loc.T("EKİP TOPARLANMASI"),
                FailedTitle = Loc.T("YAPILAMADI"),
                Action = Loc.F("VARDİYA {0} BAŞLAT", TrNumber.WithAccusative(a.shift, "000", true)),
                Hint = Loc.T("Esc · rapor EVRAK tepsisine dosyalanır"),
            };

            Funds(input, d);
            Production(input, d);
            d.Analyst = Analyst(input);
            Teams(input, d);
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
            double rest = d.SuppliesNet - (gather - eat);
            if (Math.Abs(rest) >= 0.05)
            {
                string why = b.supplies + gather - eat > b.suppliesCapacity
                    ? Loc.F("Depo sınırı · {0}", Compact(b.suppliesCapacity))
                    : Loc.T("Erzak tükendi");
                d.Production.Add(Row(why, null, Delta(rest, 1), Tone(rest, 1)));
            }

            var workshop = b.Room(ShelterLogic.Workshop);
            d.MaterialsNet = a.materials - b.materials;
            string note = null, badge = null;
            if (workshop == null || workshop.level <= 0) badge = Loc.T("HASARLI");
            else if (!b.EnergyEnough) badge = Loc.F("ENERJİSİZ {0}/{1}", Compact(b.energyCapacity), Compact(b.energyDemand));
            else note = a.starving
                ? Loc.F("{0} kişi × {1} · açlık × {2}", workshop.workers, Compact(r.materialsPerWorker), Compact(r.starvingMultiplier))
                : Loc.F("{0} kişi × {1}", workshop.workers, Compact(r.materialsPerWorker));
            d.Production.Add(new ReportRow
            {
                Label = Loc.T("Malzeme · atölye"), Note = note, Badge = badge,
                Value = Delta(d.MaterialsNet, 1), Tone = Tone(d.MaterialsNet, 1),
            });
            d.Production.Add(Total(Loc.F("Erzak {0} → {1} / {2}", DeskFormat.Number(b.supplies, 1), DeskFormat.Number(a.supplies, 1),
                Compact(a.suppliesCapacity)), Delta(d.SuppliesNet, 1), Tone(d.SuppliesNet, 1)));
        }

        static void Teams(ShiftReportInput input, ShiftReportData d)
        {
            var b = input.before;
            var a = input.after;
            var r = input.rates;
            foreach (var t in a.teams)
            {
                string name = LiberationMapBuilder.TeamName(t.id, t.name);
                int from = b.Team(t.id)?.down ?? t.down, to = t.down;
                if (from == 0 && to == 0)
                {
                    d.Teams.Add(new ReportTeamRow { Label = Loc.F("{0} · hazır", name), Value = Loc.T("HAZIR") });
                    continue;
                }
                int healed = from - to;
                bool infirmary = healed > 1;
                string heal = infirmary ? Loc.F("Revir çalışıyor: toparlanma {0} gün hızlandı.", healed - 1) : null;
                string ready;
                if (to == 0)
                {
                    // Toparlandı: revirin neden çalışmadığı artık önemsiz
                    ready = Loc.T("Ekip cepheye döndü.");
                    d.TeamReturned = true;
                }
                else
                {
                    heal ??= InfirmaryIdle(b, a, r);
                    int perShift = infirmary ? 1 + r.healingDays : 1;
                    int shift = a.shift + (to + perShift - 1) / perShift;
                    ready = Loc.F("Vardiya {0} baskına hazır.", TrNumber.WithLocative(shift));
                }
                d.Teams.Add(new ReportTeamRow
                {
                    Label = to == 0 ? Loc.F("{0} · toparlandı", name) : Loc.F("{0} · yaralı", name),
                    Value = Loc.F("{0} → {1} gün", from, to),
                    Detail = heal == null ? ready : heal + " " + ready,
                });
            }
        }

        /// <summary>Revir toparlanmayı neden hızlandırmadı (ShelterLogic.Advance koşulları); hızlandırması gerekmiyorsa null.</summary>
        static string InfirmaryIdle(ShiftSnapshot b, ShiftSnapshot a, ShiftRates r)
        {
            var room = b.Room(ShelterLogic.Infirmary);
            if (room == null || room.level <= 0) return Loc.T("Revir hasarlı: toparlanma hızlanmadı.");
            if (room.workers < r.healingWorkers) return Loc.T("Revirde görevli yok: toparlanma hızlanmadı.");
            if (!b.EnergyEnough) return Loc.T("Revir enerjisiz: toparlanma hızlanmadı.");
            if (a.starving) return Loc.T("Açlık: toparlanma hızlanmadı.");
            return null;
        }

        /// <summary>
        /// Analist notu (Kalam, tek satır, yalnız öneri): yeni vardiyanın hâlinden en önemli tek iş. Enerji açığı → jeneratör,
        /// erzak eksiye gidiyor → yaşam alanı, yaralı ekip → revir, boşta kişi → iş. Öneri yoksa null.
        /// </summary>
        static string Analyst(ShiftReportInput input)
        {
            var a = input.after;
            var r = input.rates;
            if (a.idle <= 0 && !a.teams.Any(t => t.down > 1)) return null;
            var generator = a.Room(ShelterLogic.Generator);
            if (!a.EnergyEnough && generator != null && generator.level > 0 && r.workerEnergy > 0)
            {
                int need = (int)Math.Ceiling((a.energyDemand - a.energyCapacity) / r.workerEnergy - 1e-9);
                int room = generator.level * r.workersPerLevel - generator.workers;
                if (need <= a.idle && need <= room)
                {
                    var workshop = a.Room(ShelterLogic.Workshop);
                    return workshop != null && workshop.level > 0 && workshop.workers > 0
                        ? Loc.F("→ jeneratöre {0} kişi, atölye döner", need)
                        : Loc.F("→ jeneratöre {0} kişi, enerji yeter", need);
                }
            }
            var living = a.Room(ShelterLogic.Living);
            double gather = (living?.workers ?? 0) * r.suppliesPerWorker, eat = a.population * r.suppliesPerResident;
            if (gather < eat && living != null && r.suppliesPerWorker > 0)
            {
                int need = (int)Math.Ceiling((eat - gather) / r.suppliesPerWorker - 1e-9);
                if (need <= a.idle && need <= living.level * r.workersPerLevel - living.workers)
                    return Loc.F("→ yaşam alanına {0} kişi, erzak artıya geçer", need);
            }
            if (a.teams.Any(t => t.down > 1))
            {
                var infirmary = a.Room(ShelterLogic.Infirmary);
                if (infirmary == null || infirmary.level <= 0) return Loc.T("→ revir onarılırsa ekip daha çabuk toparlanır");
                if (infirmary.workers < r.healingWorkers && a.idle > 0)
                    return Loc.F("→ revire {0} kişi, ekip daha çabuk toparlanır", r.healingWorkers - infirmary.workers);
            }
            return a.idle > 0 ? Loc.F("→ boştaki {0} kişiye iş ver", a.idle) : null;
        }

        static string LedgerLabel(ShiftLedgerEntry e)
        {
            switch (e.kind)
            {
                case "liberation_raid": return Loc.F("Baskın emri · OP-{0:00}", e.number);
                case "raidres": return Loc.F("Baskın ödülü · OP-{0:00}", e.number);
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

        /// <summary>Formül sayısı: tam sayı ondalıksız (“2”, “40”), değilse bir ondalık (“0,5”).</summary>
        static string Compact(double v) => DeskFormat.Number(v, Math.Abs(v - Math.Round(v)) < 1e-9 ? 0 : 1);

        static ReportRow Row(string label, string note, string value, ReportTone tone) =>
            new ReportRow { Label = label, Note = note, Value = value, Tone = tone };

        static ReportRow Total(string label, string value, ReportTone tone) =>
            new ReportRow { Label = label, Value = value, Tone = tone, Total = true };
    }
}
