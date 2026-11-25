using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis baskınında açılan büyük sandığın operasyon raporu (aşama 7; Docs/TESIS-BASKINLARI-TASARIMI.md §9): KAZANÇ'taki metal
    /// ve devre sandığın ekini de sayar (Core'un verdiğiyle aynı), SONUÇLAR'da sektör satırından hemen sonra "Büyük sandık ·
    /// +{m} metal · +{c} devre" satırı (Balance'tan), kâğıdın yuvası (<see cref="OperationReportData.MaxResults"/>) aşılırsa
    /// değişmemiş YZ alarmı satırı düşer. TR ve EN. Sahnesiz; komutlar gerçek yoldan (AdvanceRaidRoom, ResolveRaid).
    /// </summary>
    public class FacilityBigCrateReportTests
    {
        static readonly Regex TurkishLetters = new Regex("[ğüşıöçĞÜŞİÖÇ]");
        DeskLang lang;

        [SetUp] public void Remember() => lang = DeskLoc.Instance.Lang;
        [TearDown] public void Restore() => DeskLoc.Instance.SetLanguage(lang);

        /// <summary>İkmal hedefli ilk tesis baskınının tohumu (karargâhın ilk kanadı); bulunamazsa açık mesajla kırılır.</summary>
        static uint LootSeed()
        {
            for (uint seed = 79; seed < 110; seed++)
            {
                var sim = FacilityDeskKit.New(seed);
                FacilityDeskKit.StartFirst(sim);
                if (RaidChain.IsFacility(sim.S.Raid.Params) && sim.S.Raid.Params.MissionKind == "loot") return seed;
            }
            Assert.Fail("ikmal hedefli tesis baskını bulunamadı (tohum 79–109)");
            return 0;
        }

        /// <summary>Elle üç oda raporu (son oda kazanıldı, yük çıktı, sandık <paramref name="crate"/>), sonuç ve rapor.</summary>
        static (Simulation sim, OperationReportData report) Run(uint seed, bool crate, int collateral = 0)
        {
            var sim = FacilityDeskKit.New(seed);
            FacilityDeskKit.StartFirst(sim);
            Assert.IsNotNull(sim.Board, "kartlı yol");
            int n = sim.S.Raid.Params.Squad.Count;
            Assert.IsTrue(sim.AdvanceRaidRoom(FacilityDeskKit.Rep(1, squad: n)).Ok);
            Assert.IsTrue(sim.AdvanceRaidRoom(FacilityDeskKit.Rep(2, squad: n)).Ok);
            var last = FacilityDeskKit.Rep(3, end: "won", squad: n);
            last.Last = true; last.Success = true; last.Cargo = 0; last.Crate = crate; last.Collateral = collateral;
            var advanced = sim.AdvanceRaidRoom(last);
            Assert.IsTrue(advanced.Ok, advanced.Msg);
            var res = sim.FacilityRaidResult();
            Assert.IsTrue(res.Success, "baskın kazanıldı");
            var resolved = sim.ResolveRaid(res);
            Assert.IsTrue(resolved.Ok, resolved.Msg);
            Assert.AreEqual(crate, sim.S.RaidLog.Last().LiberationReward.BigCrate, "Core'un sandık ödülü");
            return (sim, OperationReportBuilder.Build(sim));
        }

        static string Label(string kind) => DeskText.Upper(PlanningBoardData.MaterialName(kind));
        static OperationGain GainOf(OperationReportData d, string kind) => d.Gains.Single(g => g.Label == Label(kind));
        static int Qty(Simulation sim, string kind) => sim.Board.Material(kind)?.Quantity ?? 0;

        [Test]
        public void BigCrate_GainsIncludeCrate()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            uint seed = LootSeed();
            var (plain, without) = Run(seed, false);
            var (opened, with) = Run(seed, true);
            int metal = opened.B.facilityBigCrateMetal, circuits = opened.B.facilityBigCrateCircuits;
            Assert.Less(Qty(opened, "metal"), opened.B.boardMaterialsCapacity, "kapasite altında (kırpma yok)");
            Assert.Less(Qty(opened, "devre"), opened.B.boardMaterialsCapacity, "kapasite altında (kırpma yok)");

            var g0 = RaidPaperFacts.MaterialGains(plain, plain.S.RaidLog.Last());
            var g1 = RaidPaperFacts.MaterialGains(opened, opened.S.RaidLog.Last());
            int iMetal = System.Array.IndexOf(PlanningBoardData.MaterialKinds, "metal"), iDev = System.Array.IndexOf(PlanningBoardData.MaterialKinds, "devre");
            Assert.AreEqual(metal, g1[iMetal] - g0[iMetal], "METAL'e sandığın eki");
            Assert.AreEqual(circuits, g1[iDev] - g0[iDev], "DEVRE'ye sandığın eki");
            for (int i = 0; i < g0.Length; i++)
                if (i != iMetal && i != iDev) Assert.AreEqual(g0[i], g1[i], PlanningBoardData.MaterialKinds[i] + " değişmez");
            // Rapor Core'un gerçekte verdiğini yazar (sandıklı ve sandıksız)
            Assert.AreEqual(Qty(opened, "metal") - Qty(plain, "metal"), metal, "Core: sandığın metal eki");
            Assert.AreEqual(Qty(opened, "devre") - Qty(plain, "devre"), circuits, "Core: sandığın devre eki");
            Assert.AreEqual(DeskFormat.Signed(g1[iMetal], 0), GainOf(with, "metal").Value);
            Assert.AreEqual(DeskFormat.Signed(g1[iDev], 0), GainOf(with, "devre").Value);
            Assert.AreEqual(g0[iMetal] == 0 ? "±0" : DeskFormat.Signed(g0[iMetal], 0), GainOf(without, "metal").Value);
            Assert.AreEqual(with.Gains.Count, without.Gains.Count, "KAZANÇ gözlerinin sayısı aynı");
        }

        [Test]
        public void BigCrate_ResultRow_TrAndEn()
        {
            uint seed = LootSeed();
            var missing = new List<string>();
            void OnMissing(string s) => missing.Add(s);
            Loc.Missing += OnMissing;
            try
            {
                foreach (var l in new[] { DeskLang.Tr, DeskLang.En })
                {
                    DeskLoc.Instance.SetLanguage(l);
                    var (sim, d) = Run(seed, true);
                    Assert.LessOrEqual(d.Results.Count, OperationReportData.MaxResults, "kâğıdın yuvası");
                    Assert.AreEqual(Loc.T("GERİ ALINDI"), d.Results[0].Value, "ilk satır sektör");
                    var row = d.Results[1];
                    Assert.AreEqual(Loc.T("Büyük sandık"), row.Label, "sektör satırından hemen sonra");
                    Assert.AreEqual(Loc.F("+{0} metal · +{1} devre", sim.B.facilityBigCrateMetal, sim.B.facilityBigCrateCircuits), row.Value);
                    Assert.AreEqual(OperationTone.Good, row.Tone);
                    Assert.AreEqual(1, d.Results.Count(r => r.Label == Loc.T("Büyük sandık")));
                    if (l == DeskLang.Tr)
                    {
                        Assert.AreEqual("Büyük sandık · +2 metal · +1 devre", row.Label + " · " + row.Value, "varsayılan denge");
                    }
                    else
                    {
                        Assert.AreEqual("Big crate · +2 metal · +1 circuits", row.Label + " · " + row.Value, "varsayılan denge");
                        Assert.IsFalse(TurkishLetters.IsMatch(row.Label + row.Value), row.Label + " · " + row.Value);
                    }

                    var (_, plain) = Run(seed, false);
                    Assert.IsFalse(plain.Results.Any(r => r.Label == Loc.T("Büyük sandık")), "sandıksızda satır yok");
                    CollectionAssert.AreEqual(plain.Results.Select(r => r.Label + "|" + r.Value),
                        d.Results.Where(r => r != row).Select(r => r.Label + "|" + r.Value), "öbür satırlar ve sırası aynı (yuva aşılmadı)");
                }
                CollectionAssert.IsEmpty(missing, "eksik çeviri: " + string.Join(" | ", missing));
            }
            finally { Loc.Missing -= OnMissing; }
        }

        /// <summary>Sivil bedeli güven satırını da getirince satırlar yuvayı aşar: değişmemiş YZ alarmı satırı düşer, ötekiler kalır.</summary>
        [Test]
        public void BigCrate_OverflowDropsUnchangedAlarm()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            uint seed = LootSeed();
            var (_, plain) = Run(seed, false, collateral: 2);
            var (_, with) = Run(seed, true, collateral: 2);
            Assert.IsTrue(plain.Results.Any(r => r.Label == Loc.T("Sivil güven")), "kurulum: güven satırı var");
            var alarm = plain.Results.SingleOrDefault(r => r.Label == Loc.T("YZ alarmı"));
            Assert.IsNotNull(alarm, "sandıksız raporda YZ alarmı satırı");
            Assert.AreEqual(OperationTone.Neutral, alarm.Tone, "ikmal baskını alarmı değiştirmez");
            Assert.AreEqual(OperationReportData.MaxResults, plain.Results.Count, "kurulum: sandıksız rapor yuvayı doldurur");

            Assert.AreEqual(OperationReportData.MaxResults, with.Results.Count, "yuva aşılmaz");
            Assert.AreEqual(Loc.T("Büyük sandık"), with.Results[1].Label);
            Assert.IsFalse(with.Results.Any(r => r.Label == Loc.T("YZ alarmı")), "değişmemiş alarm satırı düştü");
            CollectionAssert.AreEqual(plain.Results.Where(r => r != alarm).Select(r => r.Label + "|" + r.Value),
                with.Results.Where(r => r.Label != Loc.T("Büyük sandık")).Select(r => r.Label + "|" + r.Value), "öbür satırlar aynı");
        }
    }
}
