using System;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Operasyon raporunun içeriği (Revizyon 2 · S7c/S7d; görünüm <see cref="OperationReportView"/>). Başlık sonucu söyler
    /// (SEKTÖR GERİ ALINDI / SEKTÖR İŞGAL ALTINDA), altında tek cümle (bölgenin kalan sektörü ya da “Ekip geri döndü…”;
    /// Teknisyen yedeksiz düştüyse ya da güven azaldıysa onun uyarısı). Başarılıda KAZANÇ gözleri (FON, MALZEME, ERZAK,
    /// KURTULAN) ve sayaçların ± fişleri; başarısızda ödül alanı yok, YZ alarmı ölçekle. SONUÇLAR: sektör, kırılan sivil
    /// sunucu, ekip (düşen, yeni yara, kırılan plaka), sivil güven, YZ alarmı ve karşı saldırı. Yalnız simülasyonu okur.
    /// </summary>
    public static class OperationReportBuilder
    {
        /// <summary>Güven bu değere ya da altına inince rapor cümlesi hatırlatır.</summary>
        const int TrustWarning = 3;

        static string N(double v, int d = 1) => DeskFormat.Number(v, d);
        static int Places(double v) => Math.Abs(v - Math.Round(v)) < 1e-9 ? 0 : 1;

        /// <summary>Uzmanın rapordaki adı (yedekse “(yedek)”).</summary>
        static string RoleName(SquadOutcome o) =>
            o.Rookie ? Loc.F("{0} (yedek)", Baskin.UnitTypes[o.Type].Name) : Baskin.UnitTypes[o.Type].Name;

        /// <summary>“Bölge · Sektör”; karşı saldırıda geri alınacak sektör kalmadıysa “—”.</summary>
        static string SectorLabel(Simulation sim, string id)
        {
            var sector = string.IsNullOrEmpty(id) ? null : sim.LiberationSector(id);
            return sector == null ? "—" : Loc.F("{0} · {1}", Loc.T(GameData.Region(sector.Region).Name), sector.Name);
        }

        /// <summary>Son baskının raporu (rapor yalnız son baskın için kurulur: güven ve alarm bugünkü değer).</summary>
        public static OperationReportData Build(Simulation sim)
        {
            var S = sim.S;
            return S.RaidLog.Count == 0 ? null : Build(sim, S.RaidLog[S.RaidLog.Count - 1]);
        }

        public static OperationReportData Build(Simulation sim, RaidLogEntry log)
        {
            var S = sim.S;
            var sector = sim.LiberationSector(log.SectorId);
            var region = sim.LiberationRegion(log.Region);
            var reward = log.LiberationReward;
            string regionName = Loc.T(GameData.Region(log.Region).Name);
            string sectorName = sector?.Name ?? Loc.T("Sektör");
            var resCmd = sim.Record?.commands.LastOrDefault(x => x.kind == "raidres");
            var result = resCmd != null ? RaidResult.Decode(resCmd.a) : null;
            bool counter = reward?.Counterattack != null;

            var d = new OperationReportData
            {
                Head = Loc.T("KYGA · OPERASYON RAPORU"),
                Number = Loc.F("No. {0} · tek nüsha", Loc.F("SO-{0}/{1}", log.Day.ToString("000"), Math.Max(1, S.Raids))),
                DateLabel = Loc.T("TARİH"), Date = DeskFormat.DayCode(log.Day),
                ResultLabel = Loc.T("SONUÇ"), ResultStamp = log.Success ? Loc.T("BAŞARILI") : Loc.T("BAŞARISIZ"),
                TargetLabel = Loc.T("HEDEF"), Target = Loc.F("{0} · {1}", regionName, sectorName),
                DurationLabel = Loc.T("SÜRE"), Duration = result != null ? Loc.F("{0} tur", result.Turns) : "—",
                Success = log.Success,
                Title = log.Success ? Loc.T("SEKTÖR GERİ ALINDI") : Loc.T("SEKTÖR İŞGAL ALTINDA"),
                GainsTitle = Loc.T("KAZANÇ"), ResultsTitle = Loc.T("SONUÇLAR"),
                Action = Loc.T("MASAYA DÖN"),
            };
            d.Sentence = Sentence(sim, log, region, regionName, reward);

            // KAZANÇ ve fişler yalnız başarılı raporda (başarısızda ödül alanı hiç çıkmaz)
            if (log.Success && reward != null)
            {
                d.Gains.Add(Gain(reward.Fon, Loc.T("FON")));
                d.Gains.Add(Gain(reward.Materials, Loc.T("MALZEME")));
                d.Gains.Add(Gain(reward.Supplies, Loc.T("ERZAK")));
                d.Gains.Add(Gain(reward.Residents, Loc.T("KURTULAN")));
                d.Slips[1] = Slip(reward.Fon);
                d.Slips[2] = Slip(reward.Supplies);
                d.Slips[3] = Slip(reward.Materials);
            }

            var sectorRow = new OperationRow
            {
                Label = sectorName,
                Value = log.Success ? Loc.T("GERİ ALINDI") : Loc.T("İŞGAL ALTINDA"),
                Tone = log.Success ? OperationTone.Retaken : OperationTone.Alert,
            };
            var civilians = new OperationRow { Label = Loc.T("Kırılan sivil sunucu"), Value = N(log.Collateral, 0), Tone = OperationTone.Plain };
            var alarm = AlarmRow(sim, reward, !log.Success);
            d.Results.Add(sectorRow);
            if (log.Success)
            {
                d.Results.Add(civilians);
                TeamRows(d, sim, reward);
                TrustRow(d, sim, reward);
                if (alarm != null) d.Results.Add(alarm);
            }
            else
            {
                if (alarm != null) d.Results.Add(alarm);
                TeamRows(d, sim, reward);
                if (log.Collateral > 0) d.Results.Add(civilians);
                TrustRow(d, sim, reward);
            }
            if (counter)
                d.Results.Add(new OperationRow { Label = Loc.T("YZ karşı saldırısı"), Value = SectorLabel(sim, reward.Counterattack), Tone = OperationTone.Alert });
            return d;
        }

        /// <summary>Başlığın altındaki tek cümle: uyarı varsa o, yoksa bölgenin durumu ya da dönüş.</summary>
        static string Sentence(Simulation sim, RaidLogEntry log, LiberationRegionState region, string regionName, LiberationRaidReward reward)
        {
            var S = sim.S;
            var team = S.Teams.FirstOrDefault();
            if (team != null && !sim.SquadCanRaid(team))
                return Loc.F("Teknisyen düştü ve yerine çıkacak yedek yok: {0} gün baskın yapılamaz. Revir kalkışı hızlandırır.", LiberationMapBuilder.TechnicianDown(team));
            if (reward != null && reward.Trust < 0 && S.Trust <= TrustWarning)
                return Loc.F("Sivil güven {0}/{1}. Sıfıra inerse halk direnişten çekilir; atıştan kaçan ekip arkasındaki sunucuyu açıkta bırakır.", S.Trust, sim.TrustMax);
            if (!log.Success) return Loc.T("Ekip geri döndü. Toparlandıktan sonra yeniden deneyebilirsin.");
            if (region == null) return Loc.T("Sonraki sektör operasyona açıldı.");
            if (region.Completed) return Loc.F("{0} özgür; haritadan sınırdaki bir bölgeyi seç ve yeni cepheyi aç.", regionName);
            int captured = region.CapturedCount, total = region.Sectors.Count, left = total - captured;
            return left == 1
                ? Loc.F("{0}: {1}/{2} sektör KYGA'da; bölgenin özgürlüğüne bir sektör kaldı.", regionName, captured, total)
                : Loc.F("{0}: {1}/{2} sektör KYGA'da; bölgenin özgürlüğüne {3} sektör kaldı.", regionName, captured, total, left);
        }

        static OperationGain Gain(double v, string label) =>
            new OperationGain { Value = Math.Abs(v) < 0.05 ? "±0" : DeskFormat.Signed(v, Places(v)), Label = label, Zero = Math.Abs(v) < 0.05 };

        static string Slip(double v) => Math.Abs(v) < 0.05 ? null : DeskFormat.Signed(v, Places(v));

        /// <summary>Ekip: ayakta kalanlar; düşenler (yatacakları gün), yeni yaralar ve kırılan zırh plakaları.</summary>
        static void TeamRows(OperationReportData d, Simulation sim, LiberationRaidReward reward)
        {
            if (reward == null || reward.Squad.Count == 0) return;
            int standing = reward.Squad.Count(o => !o.Down);
            d.Results.Add(new OperationRow { Label = Loc.T("Ekip"), Value = Loc.F("{0}/{1} ayakta", standing, reward.Squad.Count), Tone = OperationTone.Plain });
            var down = reward.Squad.Where(o => o.Down).ToList();
            if (down.Count > 0) d.Results.Add(new OperationRow
            {
                Label = Loc.F("Düştü: {0}", string.Join(", ", down.Select(RoleName))),
                Value = Loc.F("{0} gün", sim.B.liberationInjuryDays), Tone = OperationTone.Alert,
            });
            var hurt = reward.Squad.Where(o => !o.Down && !o.Rookie && o.HpAfter < o.HpBefore).ToList();
            if (hurt.Count > 0) d.Results.Add(new OperationRow
            {
                Label = Loc.T("Yeni yara"),
                Value = string.Join(" · ", hurt.Select(o => Loc.F("{0} −{1}", Baskin.UnitTypes[o.Type].Short, o.HpBefore - o.HpAfter))),
                Tone = OperationTone.Plain,
            });
            int plates = reward.Squad.Count(o => o.PlateBroken);
            if (plates > 0) d.Results.Add(new OperationRow { Label = Loc.T("Zırh plakası kırıldı"), Value = N(plates, 0), Tone = OperationTone.Plain });
        }

        /// <summary>Sivil güvenin net değişimi ve bugünkü değeri (değişmediyse satır yok).</summary>
        static void TrustRow(OperationReportData d, Simulation sim, LiberationRaidReward reward)
        {
            if (reward == null || reward.Trust == 0) return;
            d.Results.Add(new OperationRow
            {
                Label = Loc.T("Sivil güven"),
                Value = Loc.F("{0} · {1}/{2}", DeskFormat.Signed(reward.Trust, 0), sim.S.Trust, sim.TrustMax),
                Tone = reward.Trust > 0 ? OperationTone.Good : OperationTone.Plain,
            });
        }

        /// <summary>YZ alarmı: “değişmedi · 5/10” ya da “+1 · 6/10”; başarısızda ölçekle; karşı saldırıda EŞİK AŞILDI.</summary>
        static OperationRow AlarmRow(Simulation sim, LiberationRaidReward reward, bool scale)
        {
            if (reward == null) return null;
            double threshold = sim.B.liberationAlarmThreshold, alarm = sim.S.Alarm;
            int steps = (int)Math.Round(threshold);
            var row = new OperationRow { Label = Loc.T("YZ alarmı") };
            if (reward.Counterattack != null)
            {
                row.Value = Loc.T("EŞİK AŞILDI");
                row.Tone = OperationTone.Alert;
                return row;
            }
            string level = Loc.F("{0}/{1}", N(alarm, Places(alarm)), N(threshold, 0));
            bool unchanged = Math.Abs(reward.Alarm) < 0.05;
            row.Value = unchanged ? Loc.F("değişmedi · {0}", level) : Loc.F("{0} · {1}", DeskFormat.Signed(reward.Alarm, Places(reward.Alarm)), level);
            row.Tone = unchanged ? OperationTone.Neutral : OperationTone.Plain;
            if (scale)
            {
                row.AlarmSteps = steps;
                row.AlarmHighFrom = Math.Max(1, steps - 2);
                row.AlarmCells = Math.Max(0, Math.Min(steps, (int)Math.Floor(alarm + 1e-9)));
            }
            return row;
        }
    }
}
