using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>Oyuncunun erişebildiği saha gözleminden hazırlanmış, henüz dosyalanmamış rapor.</summary>
    [Serializable]
    public sealed class FieldReport
    {
        public string Id, Region, Headline;
        public int Day, ExpiresDay;
        public double Reward;
    }

    public sealed partial class Simulation
    {
        /// <summary>Temel ve deneysel JS paritesi bu seçenek kapalıyken değişmez.</summary>
        public bool ReportsOn => ModernEconomy && S.Rules != null && S.Rules.FieldReports;

        public IReadOnlyList<FieldReport> PendingFieldReports => S.FieldReports;

        bool ReportTeamAt(string id) => TeamsOn && S.Teams.Any(t => t.At == id && t.Dest == null && t.Down == 0);

        /// <summary>Üye ve çevrimiçi bölgede bir izleme ağı veya yerleşmiş sağlam ekip gerekir.</summary>
        public bool EligibleForFieldReport(string regionId)
        {
            var r = S.Region(regionId);
            return ReportsOn && r != null && r.Off == 0 && !r.Left && (r.Monitored || ReportTeamAt(regionId));
        }

        /// <summary>Raporun nereden geleceğini yalnız oyuncunun gördüğü oran belirler; gizli gerçekler kullanılmaz.</summary>
        public double FieldReportWeight(string regionId)
        {
            if (!EligibleForFieldReport(regionId)) return 0;
            var r = S.Region(regionId);
            double saturation = Math.Max(1, B.reportSaturation);
            double weight = 1 + Math.Max(0, B.reportInfiltrationWeight) * Clamp((Shown(r).V ?? 0) / saturation, 0, 1);
            return r.Monitored && ReportTeamAt(regionId) ? weight * (1 + Math.Max(0, B.reportBothBonus)) : weight;
        }

        /// <summary>Eski kaydı bozmadan, bu günden sonraki saha raporlarını açan tekrar oynatılabilir geçiş.</summary>
        public ActResult EnableFieldReports() => RunCommand(() =>
        {
            if (!ModernEconomy) return ActResult.Fail(Loc.T("Saha raporları bütünleşik ekonomi kuralları gerektirir."));
            if (ReportsOn || S.Ending != null) return ActResult.Fail(Loc.T("Saha raporları şu anda açılamaz."));
            // Başlangıçtaki Record.rules değişmez: geçmiş günler yeni RNG tüketimiyle yeniden oynatılmamalı.
            S.Rules = S.Rules?.Clone() ?? new RuleOptions { Stage = 4, Events = false };
            S.Rules.FieldReports = true;
            S.NextFieldReportDay = S.Day + Math.Max(1, Dur(B.reportIntervalDays));
            return ActResult.Success();
        }, "reports_enable");

        /// <summary>Ödenek yalnızca başarılı dosyalamada ödenir; yinelenen tıklama bir kez daha fon vermez.</summary>
        public ActResult CollectFieldReport(string id) => RunCommand(() =>
        {
            if (!ReportsOn || S.Ending != null || S.Blocked) return ActResult.Fail(Loc.T("Saha raporu şu anda dosyalanamaz."));
            var report = S.FieldReports.FirstOrDefault(x => x.Id == id);
            if (report == null || S.Day >= report.ExpiresDay || !EligibleForFieldReport(report.Region))
                return ActResult.Fail(Loc.T("Bu saha raporuna artık erişilemiyor."));
            S.FieldReports.Remove(report);
            S.Fon += report.Reward;
            ArchiveFieldReport(report);
            return ActResult.Success(Loc.F("Saha raporu dosyalandı. +{0} fon.", report.Reward));
        }, "report", id);

        void ArchiveFieldReport(FieldReport report)
        {
            // Saha tahmini, gerçek/sahte haber ikiliğine sokulmaz. Doğrulama Ağı bu oranı kanıtlamaz.
            var h = AddNews(report.Headline, kind: "field", src: report.Region);
            h.Day = report.Day;
            h.Agency = Loc.T("KYGA SAHA");
        }

        void StepFieldReports()
        {
            // Ömrü dolan haber arşive kalır; bölgeye erişim kaybolursa fiş iptal edilir. Ödenek iade edilmez.
            for (int i = S.FieldReports.Count - 1; i >= 0; i--)
            {
                var report = S.FieldReports[i];
                bool expired = S.Day >= report.ExpiresDay;
                if (!expired && EligibleForFieldReport(report.Region)) continue;
                S.FieldReports.RemoveAt(i);
                if (expired) ArchiveFieldReport(report);
            }

            var eligible = S.Regions.Where(r => EligibleForFieldReport(r.Id)).ToList();
            if (eligible.Count > 0)
            {
                // Bu pay düzenli geliri azaltmaz; toplam ek ödemenin üst sınırını oluşturur.
                double earned = Math.Max(0, LastIncome.Total) * Clamp(B.reportIncomeShare, 0, 1);
                S.FieldReportBudget = Math.Min(Math.Max(0, B.reportBudgetCap), S.FieldReportBudget + earned);
            }

            // Takvim toplamayla veya ekip hareketiyle sıfırlanmaz; tüm harita ortak üretim aralığını paylaşır.
            if (S.Day < S.NextFieldReportDay) return;
            S.NextFieldReportDay = S.Day + Math.Max(1, Dur(B.reportIntervalDays));
            double reward = Math.Max(0, B.reportReward);
            if (reward <= 0 || S.FieldReportBudget < reward || S.FieldReports.Count >= Math.Max(0, B.reportMaxPending)) return;
            var candidates = eligible.Where(r => !S.FieldReports.Any(x => x.Region == r.Id)
                && (!S.FieldReportCooldowns.TryGetValue(r.Id, out int until) || S.Day >= until)).ToList();
            if (candidates.Count == 0) return;

            double roll = S.Rng.Next() * candidates.Sum(r => FieldReportWeight(r.Id));
            var selected = candidates[candidates.Count - 1];
            foreach (var r in candidates)
            {
                roll -= FieldReportWeight(r.Id);
                if (roll < 0) { selected = r; break; }
            }
            var shown = Shown(selected).V;
            string headline = shown.HasValue
                ? Loc.F("{0} saha raporu: ağlarda yaklaşık %{1} YZ varlığı bildiriliyor. Bu oran bağımsız denetimle doğrulanmadı.", Def(selected.Id).Name, Js.Round(shown.Value))
                : Loc.F("{0} saha raporu: ağ etkinliği gözlemleri dosyaya alındı. YZ varlığının oranı henüz belirlenemedi.", Def(selected.Id).Name);
            S.FieldReports.Add(new FieldReport
            {
                Id = "field-" + (++S.FieldReportSequence), Region = selected.Id, Day = S.Day,
                ExpiresDay = S.Day + Math.Max(1, Dur(B.reportLifetimeDays)), Reward = reward, Headline = headline,
            });
            S.FieldReportBudget -= reward;
            S.FieldReportCooldowns[selected.Id] = S.Day + Math.Max(1, Dur(B.reportRegionCooldownDays));
        }
    }
}
