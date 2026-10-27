using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // Baskın raporu: karartma + bölge spotu + halka, rapor kâğıdı (başlık, alanlar, sonuç satırları, uyarı, Masaya dön) ve iplik.
    public sealed partial class SimDeskBuilder
    {
        static readonly float[] MemoRect = { 236, 60, 712, -1 };
        static readonly float[] MemoPin = { 926, 82 };

        EventVM RaidReport(RaidLogEntry log)
        {
            var sector = sim.LiberationSector(log.SectorId);
            var reward = log.LiberationReward;
            bool loot = log.MissionKind == "loot";
            string sectorName = sector?.Name ?? Loc.T("Sektör");
            string title = !log.Success ? Loc.T("Operasyon başarısız") : loot ? Loc.T("İKMAL ALINDI") : Loc.T("SUNUCU YIKILDI");
            string subtitle = !log.Success
                ? Loc.T("Ekip geri döndü. Sektör işgal altında; toparlandıktan sonra yeniden deneyebilirsin.")
                : loot ? Loc.T("Malzemeler alındı ve ekip tahliye edildi.") : Loc.T("Sunucu imha edildi; sektör geri alındı.");
            var resCmd = sim.Record?.commands.LastOrDefault(x => x.kind == "raidres");
            var result = resCmd != null ? RaidResult.Decode(resCmd.a) : null;

            var c = Center(log.Region);
            var e = new EventVM
            {
                Spotlight = new EventSpotVM { Region = log.Region, Center = A(c), HoleDiameter = 148, Ring = 12, ScrimAlpha = 0.78f },
                // Halka ve iplik her zaman verilir; bölge kâğıdın altında kalıyorsa görünüm gizler
                MarkerRing = new EventRingVM { Center = A(c + new Vector2(0, -2)), Sprite = "modal_marker_ring", Color = "#B8241A", Alpha = 0.92f },
                String = new EventStringVM { From = MemoPin, To = A(c + new Vector2(0, -14)), Color = "#B8241A", Width = 2.2f, ShadowAlpha = 0.35f },
                Memo = new EventMemoVM
                {
                    Rect = MemoRect,
                    Header = new HeaderVM { Title = Loc.T("KYGA · OPERASYON RAPORU"), Right = Loc.F("No. {0} · tek nüsha", Loc.F("SO-{0}/{1}", log.Day.ToString("000"), Math.Max(1, S.Raids))) },
                    Fields =
                    {
                        new EventFieldVM { K = Loc.T("TARİH"), V = Day(log.Day) },
                        new EventFieldVM { K = Loc.T("SONUÇ"), Stamp = new StampVM { Text = log.Success ? Loc.T("BAŞARILI") : Loc.T("BAŞARISIZ"), Color = log.Success ? "blue" : "red", FontSize = 13, Rot = -4 } },
                        new EventFieldVM { K = Loc.T("HEDEF"), V = Loc.F("{0} · {1}", Def(log.Region).Name, sectorName) },
                        new EventFieldVM { K = Loc.T("SÜRE"), V = result != null ? Loc.F("{0} tur", result.Turns) : "—" },
                    },
                    Title = Up(title), Subtitle = subtitle,
                },
            };
            var doc = new EventDocVM
            {
                LinesHeader = new HeaderVM { Title = Loc.T("SONUÇLAR") },
                Buttons = { new CommandVM { Label = Loc.T("Masaya dön"), Kind = "ink", Key = "Enter", Command = "Doc:Close" } },
                Torn = true,
            };
            doc.Lines.Add(new EventLineVM
            {
                Label = sectorName, Value = log.Success ? Loc.T("GERİ ALINDI") : Loc.T("İŞGAL ALTINDA"),
                ValueIsText = true, Status = log.Success ? "good" : "threat",
            });
            if (log.Success && reward != null)
            {
                if (reward.Fon > 0) doc.Lines.Add(new EventLineVM { Label = Loc.T("Fon"), Value = Loc.F("+{0}", N(reward.Fon)), Status = "good" });
                if (reward.Supplies > 0) doc.Lines.Add(new EventLineVM { Label = Loc.T("Erzak"), Value = Loc.F("+{0}", N(reward.Supplies)), Status = "good" });
                if (reward.Materials > 0) doc.Lines.Add(new EventLineVM { Label = Loc.T("Malzeme"), Value = Loc.F("+{0}", N(reward.Materials)), Status = "good" });
                if (reward.Residents > 0) doc.Lines.Add(new EventLineVM { Label = Loc.T("Kurtulan"), Value = Loc.F("+{0}", reward.Residents), Status = "good" });
            }
            if (log.Down > 0) doc.Lines.Add(new EventLineVM
            {
                Label = Loc.F("{0} yaralı · toparlanma", log.Down),
                Value = Loc.F("{0} gün", log.Down * sim.B.liberationInjuryDays), Status = "threat",
            });
            if (log.Collateral > 0) doc.Lines.Add(new EventLineVM { Label = Loc.T("Sivil sunucu hasarı"), Value = N(log.Collateral, 0), Status = "threat" });
            doc.LinesHeader.Right = doc.Lines.Count.ToString();
            doc.Risk = !log.Success ? subtitle : reward?.RegionCompleted == true
                ? Loc.T("Bölge özgür. Haritadan sınırdaki bir bölgeyi seç ve yeni cepheyi aç.")
                : Loc.T("Sonraki sektör operasyona açıldı.");
            e.Doc = doc;
            return e;
        }
    }
}
