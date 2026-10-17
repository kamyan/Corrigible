using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    public sealed partial class SimDeskBuilder
    {
        // Fişler yalnızca raporun açık bölgesini, tarihini ve ayrılmış ödeneği taşır. Gerçek sızma okunmaz.
        void FieldReports(MapVM map)
        {
            if (!sim.ReportsOn || S.Ending != null || input.RaidScene) return;
            var placed = new List<Rect>();
            foreach (var report in sim.PendingFieldReports)
            {
                if (S.Day >= report.ExpiresDay || !sim.EligibleForFieldReport(report.Region)) continue;
                int remaining = report.ExpiresDay - S.Day;
                var position = ReportPosition(map, Center(report.Region), placed);
                map.Reports.Add(new FieldReportVM
                {
                    Id = report.Id, Region = report.Region, X = position.x, Y = position.y,
                    Title = Loc.T("RAPOR"), RewardText = Loc.F("+{0:0.#}", report.Reward),
                    Tooltip = Loc.F("{0} · {1}\n{2}\nDosyala: +{3:0.#} fon · {4} gün kaldı. Ödenek, raporun doğruluğunu onaylamaz.",
                        Def(report.Region).Name, Day(report.Day), report.Headline, report.Reward, remaining),
                    Command = "Report:Collect:" + report.Id,
                    Expiring = remaining <= sim.Dur(5),
                });
            }
        }

        // Düğüm, basılı bölge adı ve diğer fişler etrafında boşluk bulur. Haritanın tasarım çerçevesinde çalışır.
        static Vector2 ReportPosition(MapVM map, Vector2 center, List<Rect> placed)
        {
            var offsets = new[]
            {
                new Vector2(76, -52), new Vector2(-76, -52), new Vector2(76, 52), new Vector2(-76, 52),
                new Vector2(0, -84), new Vector2(0, 84), new Vector2(112, 0), new Vector2(-112, 0),
            };
            float bestScore = float.MaxValue;
            Vector2 best = center + offsets[0];
            foreach (var offset in offsets)
            {
                var p = center + offset;
                p.x = Mathf.Clamp(p.x, 478, 1448); p.y = Mathf.Clamp(p.y, 280, 854);
                var rect = new Rect(p.x - 54, p.y - 26, 108, 52);
                float score = (p - center).sqrMagnitude;
                foreach (var node in map.Regions)
                {
                    if (rect.Overlaps(new Rect(node.Center[0] - 30, node.Center[1] - 30, 60, 60))) score += 100000;
                    if (rect.Overlaps(new Rect(node.LabelX, node.LabelY - 3, node.LabelW, 23))) score += 50000;
                }
                foreach (var other in placed) if (rect.Overlaps(other)) score += 200000;
                if (score >= bestScore) continue;
                bestScore = score; best = p;
            }
            placed.Add(new Rect(best.x - 60, best.y - 32, 120, 64));
            return best;
        }
    }
}
