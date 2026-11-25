using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 9, parça 3: sunucu ödülünün masadaki metinleri ve bekleyen ödül kilidi (aşama 9 tasarımı §7.5–7.6, K25–K30, K38).
    /// Bekleyen ödül yardımcısı, kâğıt verisi, karar komutu ve yeniden oynatma (gerçek bölge tamamlatma), görev fişi gerekçesi ve fon kısa
    /// uyarısı, CRT ve telsiz günlüğü, operasyon raporunun satırları ve düğmesi, KAZANIM satırı, son ekran.
    /// </summary>
    public sealed class FacilityServerRewardTests
    {
        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [Test]
        public void Pending_NoEnding_NoRaid_PendingReward()
        {
            var sim = FacilityDeskKit.New();
            Assert.IsFalse(FacilityRewardText.Pending(sim), "bekleyen yok");
            FacilityDeskKit.Reward(sim, "weu", null);
            Assert.IsTrue(FacilityRewardText.Pending(sim));
            Assert.AreEqual("weu", FacilityRewardText.Region(sim));
            sim.S.Ending = "silme";
            Assert.IsFalse(FacilityRewardText.Pending(sim), "son ekran: bekleyen ödül gösterilmez (K38)");
            Assert.IsNull(FacilityRewardText.Paper(sim), "kâğıt kurucusu null");
            sim.S.Ending = null;
            Assert.IsTrue(FacilityRewardText.Pending(sim));

            // Kartsız oyun (pano kapalı; inceleme bulgusu 6): Core'un kilidi yalnız panolu oyunda, tarifler panonun defterinde: hiçbir yüzey
            // ödülü beklemez, baskın emri ödül yüzünden kapanmaz
            var cardless = Simulation.NewGame(79, new Balance { shelterEventsEnabled = false, planningBoardEnabled = false });
            Assert.IsNull(cardless.Board, "kartsız oyun");
            cardless.Facility.PendingReward = "weu";
            Assert.IsFalse(FacilityRewardText.Pending(cardless), "kartsız oyunda bekleyen ödül gösterilmez");
            Assert.IsNull(FacilityRewardText.Paper(cardless));
            var next = cardless.NextLiberationSector(cardless.S.Liberation.CurrentRegion);
            Assert.AreNotEqual(Loc.T("Önce sunucu ödülünü seç: tarif ya da kazanım."), cardless.LiberationRaidCheck(next.Id), "Core kilidi kartsız oyunda yok");
        }

        [Test]
        public void Pending_FalseDuringRaid()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.StartFirst(sim);
            sim.Facility.PendingReward = "weu";
            Assert.IsFalse(FacilityRewardText.Pending(sim), "baskın sürerken kâğıt yok");
        }

        [Test]
        public void Paper_TwoRows_RecipeAndPerk_Texts()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.Reward(sim, "sa", null);   // taret · Yedek çanta
            var d = FacilityRewardText.Paper(sim);
            Assert.IsNotNull(d);
            Assert.AreEqual(sim.RegionName("sa"), d.Title, "başlık YZ'nin adı");
            Assert.AreEqual(GameData.Region("sa").Building, d.Person, "kişi satırı bina");
            Assert.AreEqual("Sunucu kanadı alındı. Biri seçilir; öbürü bu harekâtta gelmez.", d.Body);
            Assert.AreEqual(2, d.Rows.Count);
            Assert.AreEqual("tarif", d.Rows[0].Id);
            Assert.AreEqual("TARİF · Yaylı taret", d.Rows[0].Name);
            Assert.AreEqual("Bir yöne kurulur; her evrenin sonunda hattaki ilk makineye 1 hasar, 3 tur.", d.Rows[0].Cost);
            Assert.AreEqual("2 metal + 1 devre · 2 vardiya", d.Rows[0].Risk);
            Assert.AreEqual("kazanim", d.Rows[1].Id);
            Assert.AreEqual("KAZANIM · Yedek çanta", d.Rows[1].Name);
            Assert.AreEqual(FacilityGear.PerkDesc(sim.B, FacilityGear.SpareBag), d.Rows[1].Cost);
            Assert.IsTrue(d.Rows.All(r => r.Enabled));
            Assert.IsFalse(d.ConfirmReady);
            Assert.AreEqual("Seçmek için 1 ya da 2, sonra Enter", d.Confirm);
            Assert.IsNull(d.Note, "kilit yok");

            var chosen = FacilityRewardText.Paper(sim, "kazanim");
            Assert.IsTrue(chosen.Rows[1].Selected);
            Assert.IsTrue(chosen.ConfirmReady);
            Assert.AreEqual("KAZANIM · Yedek çanta · seç", chosen.Confirm);

            // Köpek: erzak bedeli girdi satırında; Tezgâh süreyi kısaltır
            sim.Facility.PendingReward = "jk";
            sim.Facility.Perks.Add(FacilityGear.Workbench);
            Assert.AreEqual("1 kumaş + 4 erzak · 2 vardiya", FacilityRewardText.Paper(sim).Rows[0].Risk);

            // Cevap paketleri: temel tarif açık değilse not; Söküm anahtarı (Teknisyen kiti) notsuz; Çift zemberek açık aygıtları sayar
            sim.Facility.PendingReward = "cn";
            Assert.AreEqual("Temel tarif açık değil: bu harekâtta etkisiz", FacilityRewardText.Paper(sim).Rows[0].Risk);
            sim.Facility.Recipes.Add(FacilityGear.Clamp);
            Assert.IsNull(FacilityRewardText.Paper(sim).Rows[0].Risk, "Yedek röle: kelepçe açık");
            sim.Facility.PendingReward = "na";
            Assert.IsNull(FacilityRewardText.Paper(sim).Rows[0].Risk, "Söküm anahtarı: Teknisyen kiti hep açık");
            sim.Facility.PendingReward = "oc";
            Assert.AreEqual("Temel tarif açık değil: bu harekâtta etkisiz", FacilityRewardText.Paper(sim).Rows[0].Risk);
            sim.Facility.Recipes.Add(FacilityGear.Turret);
            Assert.AreEqual("Açık olan taret, yem, vinç tariflerinin yerine geçer (Yaylı taret)", FacilityRewardText.Paper(sim).Rows[0].Risk);
            Assert.AreEqual("TARİF · Çift zemberek", FacilityRewardText.Paper(sim).Rows[0].Name);
        }

        [Test]
        public void GearDesc_AllSeven_TrEn_NoMissing()
        {
            var b = new Balance();
            foreach (var kind in FacilityGear.Kinds) Assert.IsNotEmpty(FacilityRewardText.GearDesc(b, kind), kind);
            var missing = new System.Collections.Generic.List<string>();
            System.Action<string> onMissing = m => missing.Add(m);
            var table = Loc.ParseTable(System.IO.File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"));
            table.TryGetValue(Loc.English, out var en);
            Loc.SetLanguage(Loc.English, en);
            Loc.Missing += onMissing;
            try
            {
                foreach (var kind in FacilityGear.Kinds) FacilityRewardText.GearDesc(b, kind);
                var sim = FacilityDeskKit.New();
                FacilityDeskKit.Reward(sim, "jk", null);
                var d = FacilityRewardText.Paper(sim, "tarif");
                Assert.AreEqual("RECIPE · Dog", d.Rows[0].Name);
                Assert.AreEqual("1 cloth + 4 supplies · 3 shifts", d.Rows[0].Risk);
                FacilityRewardText.Paper(sim, "kazanim");
                OperationReportBuilder.Build(sim);
                CollectionAssert.IsEmpty(missing, "EN çevirisi eksik: " + string.Join(" | ", missing));
            }
            finally { Loc.Missing -= onMissing; Loc.SetLanguage(Loc.Turkish); }
        }

        [Test]
        public void RealRegion_ReportRow_Lock_Choose_Replay()
        {
            var sim = FacilityDeskKit.New();
            string region = FacilityDeskKit.CompleteRegion(sim);
            Assert.AreEqual(region, sim.Facility.PendingReward, "bölge tamamlandı: ödül bekliyor");
            Assert.IsTrue(FacilityRewardText.Pending(sim));
            var log = sim.S.RaidLog.Last();
            Assert.AreEqual(region, log.LiberationReward.ServerReward);

            // Operasyon raporu: SONUÇLAR'ın son satırı ve "ÖDÜLÜ SEÇ"; 7 yuva
            var report = OperationReportBuilder.Build(sim);
            Assert.AreEqual("ÖDÜLÜ SEÇ", report.Action);
            Assert.AreEqual("Sunucu ödülü", report.Results.Last().Label);
            Assert.AreEqual("tarif ya da kazanım", report.Results.Last().Value);
            Assert.AreEqual(OperationTone.Good, report.Results.Last().Tone);
            Assert.LessOrEqual(report.Results.Count, OperationReportData.MaxResults);

            // Kilit: görev fişi gerekçesi, fon kısa uyarısı örtmez, baskın emri Core'da reddedilir, figür
            Assert.IsTrue(sim.SelectLiberationRegion(sim.LiberationFrontier.First()).Ok);
            // Ekip sonraki vardiyada hazır: kapanış (ödül seçilmeden de kapanış olur)
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.AreEqual("ÖDÜL SEÇİLMEDİ", RaidPaperFacts.DisabledReason(sim));
            var slip = LiberationMapBuilder.Slip(sim, sim.S.Liberation.CurrentRegion);
            Assert.AreEqual(SlipActionState.Disabled, slip.ActionState);
            Assert.AreEqual("ÖDÜL SEÇİLMEDİ", slip.ActionNote);
            Assert.AreEqual("Önce sunucu ödülünü seç: tarif ya da kazanım.", slip.Reason);
            double fon = sim.S.Fon;
            sim.S.Fon = 0;
            Assert.IsFalse(TelexReasons.RaidFundsOnly(sim, next.Id), "fon kısa uyarısı ödül kilidini örtmez");
            StringAssert.Contains("Önce sunucu ödülünü seç", TelexReasons.Raid(sim, next.Id));
            sim.S.Fon = fon;
            StringAssert.Contains("Önce sunucu ödülünü seç", TelexReasons.Pick(sim));
            Assert.IsFalse(sim.StartLiberationRaid(next.Id).Ok, "Core baskın emrini reddeder");
            var crt = CrtViewBuilder.Screen(sim, false);
            Assert.AreEqual("SUNUCU ÖDÜLÜ BEKLİYOR · KAPI ÇEKMECESİ", crt.Hint);
            Assert.IsTrue(RadioLogBuilder.Entries(sim, false).Any(e => e.text == "Sunucu ödülü bekliyor · kapının çekmecesi"));

            // Karar: server_reward kaydı, yeniden oynatma aynı tarif/kazanım
            int count = sim.Record.commands.Count;
            var r = sim.ChooseServerReward(region, FacilityGear.ChoicePerk);
            Assert.IsTrue(r.Ok, r.Msg);
            var cmd = sim.Record.commands[count];
            Assert.AreEqual("server_reward", cmd.kind);
            Assert.AreEqual(region, cmd.a);
            Assert.AreEqual("kazanim", cmd.b);
            Assert.IsFalse(FacilityRewardText.Pending(sim));
            var replay = Replay.Restore(sim.Record);
            CollectionAssert.AreEqual(sim.Facility.Perks, replay.Facility.Perks);
            CollectionAssert.AreEqual(sim.Facility.Recipes, replay.Facility.Recipes);
            Assert.AreEqual("", replay.Facility.PendingReward ?? "");

            // Seçimden sonra: rapor satırı yok, düğme MASAYA DÖN; KAZANIM satırı görev fişinde
            report = OperationReportBuilder.Build(sim, log);
            Assert.AreEqual("MASAYA DÖN", report.Action);
            Assert.IsFalse(report.Results.Any(x => x.Label == "Sunucu ödülü"));
            slip = LiberationMapBuilder.Slip(sim, sim.S.Liberation.CurrentRegion);
            var perk = slip.Fields.SingleOrDefault(f => f.Label == "KAZANIM");
            Assert.IsNotNull(perk, "KAZANIM satırı");
            Assert.AreEqual(FacilityGear.PerkName(FacilityGear.Reward(region).Perk), perk.Value);
            Assert.AreNotEqual("ÖDÜL SEÇİLMEDİ", RaidPaperFacts.DisabledReason(sim));
            Assert.AreNotEqual("SUNUCU ÖDÜLÜ BEKLİYOR · KAPI ÇEKMECESİ", CrtViewBuilder.Screen(sim, false).Hint, "ipucu kalktı");
        }

        [Test]
        public void Report_GearRow_ClampAndDog_DropsBeforeReward()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.CompleteRegion(sim);
            var log = sim.S.RaidLog.Last();
            log.LiberationReward.Squad[0].ClampSpent = true;
            log.LiberationReward.Squad[1].DogDown = true;
            var report = OperationReportBuilder.Build(sim, log);
            var gear = report.Results.SingleOrDefault(x => x.Label == "Ekipman");
            if (report.Results.Count < OperationReportData.MaxResults || gear != null)
            {
                Assert.IsNotNull(gear, "ekipman satırı");
                StringAssert.StartsWith("kelepçe harcandı (" + log.LiberationReward.Squad[0].PersonName + ") · köpek dinlenecek (3 V)", gear.Value);
            }
            Assert.AreEqual("Sunucu ödülü", report.Results.Last().Label, "ödül satırı en son düşer");
            Assert.LessOrEqual(report.Results.Count, OperationReportData.MaxResults);
            // Ekipman harcanmamış (alanlar boş): satır yok
            log.LiberationReward.Squad[0].ClampSpent = false;
            log.LiberationReward.Squad[1].DogDown = false;
            Assert.IsFalse(OperationReportBuilder.Build(sim, log).Results.Any(x => x.Label == "Ekipman"));
        }

        [Test]
        public void Ending_PendingHidden_FinalReportSilent()
        {
            var sim = FacilityDeskKit.New();
            FacilityDeskKit.Reward(sim, "weu", null);
            sim.S.Ending = "dagilma";
            Assert.IsFalse(FacilityRewardText.Pending(sim));
            Assert.IsNull(FacilityRewardText.Paper(sim));
            Assert.AreNotEqual("ÖDÜL SEÇİLMEDİ", RaidPaperFacts.DisabledReason(sim));
            var final = FinalReportBuilder.Build(sim);
            string json = JsonUtility.ToJson(final);
            StringAssert.DoesNotContain("Sunucu ödülü", json);
            StringAssert.DoesNotContain("tarif ya da kazanım", json);
            StringAssert.DoesNotContain("SUNUCU ÖDÜLÜ", json);
            Assert.AreNotEqual("SUNUCU ÖDÜLÜ BEKLİYOR · KAPI ÇEKMECESİ", CrtViewBuilder.Screen(sim, false).Hint);
        }
    }
}
