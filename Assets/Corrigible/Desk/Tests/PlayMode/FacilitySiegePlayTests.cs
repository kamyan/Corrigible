using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 11, parça P4a (aşama 11 tasarımı §8.10): bekleyen karşı saldırı gerçek DeskGame'de, Play'de. Kurulum
    /// <see cref="Tune"/> (= <see cref="FacilityRewardPlayKit.Tune"/> + alarm her vardiya eşik kadar; bellek dengesi, asset değişmez): ilk
    /// kanat komutlarla alınır, operasyon raporu dosyalanır, vardiya kapanır → Core kuşatmayı kurar (teleks "KARŞI SALDIRI BEKLİYOR" bir
    /// kez); ardından alarm artışı bellekte kapatılır (ikinci eşik kurulmasın). <see cref="Siege_Yield_FromSlip"/>: 3 kuşatma fişini açar,
    /// BIRAK iki tıkla (ilk tık onay), <c>siege_yield</c>, güven −1, teleks, haritada YENİDEN İŞGAL, aynı bölgede BASKIN EMRİ VER Core
    /// gerekçesiyle kapalı. <see cref="Siege_AutoYield_AtClose"/>: devir fişinde KARŞI SALDIRI uyarısı, ÖN KONTROL notu, gerçek şalter ve
    /// EVRAK kapanışında kanat kendiliğinden bırakılır, teleks "YZ KARŞI SALDIRISI · … karar verilmedi" bir kez. Sayılar Core'dan.
    /// </summary>
    public sealed class FacilitySiegePlayTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            yield return FacilityPlayKit.UnloadRaid();
            yield return ShiftPlayKit.End();
        }

        static Simulation Sim => BoardPlayKit.Sim;
        static SiginakOperation Operation => BoardPlayKit.Find<SiginakOperation>();
        static TelexFeed Telex => ShiftPlayKit.Telex;
        static int Count(string status) => Telex.Lines.Count(l => l.status == status);

        static T Field<T>(object o, string name) =>
            (T)o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);

        /// <summary>Tesis açık, olaylar kapalı; alarm her vardiya eşik kadar (ilk kapanış kuşatma kurar).</summary>
        public static void Tune(Balance b)
        {
            FacilityRewardPlayKit.Tune(b);
            b.liberationAlarmPerShift = b.liberationAlarmThreshold;
        }

        static LiberationSectorState sector;

        /// <summary>İlk kanat alınır, rapor dosyalanır, vardiya kapanır: kuşatma kurulur ve teleksten bir kez duyurulur.</summary>
        static IEnumerator Siege()
        {
            yield return BoardPlayKit.LoadDesk(tune: Tune);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            Assert.IsTrue(Sim.DefenseOn, "savunma açık (alt bayrak varsayılanı)");
            int pending = Count(FacilityDefenseText.PendingStatus);
            sector = Sim.NextLiberationSector(Sim.S.Liberation.CurrentRegion);
            var started = Sim.StartLiberationRaid(sector.Id);
            Assert.IsTrue(started.Ok, started.Msg);
            FacilityRewardPlayKit.WinRaid(Sim);
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, "operasyon raporu inmedi");
            yield return BoardPlayKit.WaitSeconds(1.2f);
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => !Operation.Busy, 4, "rapor dosyalanmadı");
            FacilityRewardPlayKit.Close(Sim);
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.Frames(3);
            Assert.IsNotNull(Sim.PendingSiege, "kapanışta kuşatma kuruldu");
            Assert.AreEqual(sector.Id, Sim.PendingSiege.Sector);
            Assert.IsTrue(Sim.SiegeDecisionDue, "karar vardiyası");
            Assert.AreEqual(0, Sim.S.Counterattacks, "kanat henüz düşmedi");
            Assert.AreEqual(pending + 1, Count(FacilityDefenseText.PendingStatus), "KARŞI SALDIRI BEKLİYOR bir kez");
            var line = Telex.Lines.Last(l => l.status == FacilityDefenseText.PendingStatus);
            Assert.AreEqual(FacilityDefenseText.PendingLine(Sim, sector.Id), line.message);
            Assert.AreEqual(TelexKind.Alarm, line.kind);
            Assert.AreEqual(0, Count(FacilityDefenseText.FallStatus), "YZ KARŞI SALDIRISI basılmaz");
            // İkinci eşik kurulmasın (bellek dengesi; kayıttan devam bu testte yok)
            Sim.B.liberationAlarmPerShift = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Siege_Yield_FromSlip()
        {
            yield return Siege();
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var slip = Object.FindFirstObjectByType<OperationSlip>(FindObjectsInactive.Include);
            var layer = Field<LiberationMapLayer>(map, "layer");

            // 3 (ShowMissions) karar vardiyasında kuşatılan kanadın fişini açar
            map.ShowMissions();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
            yield return BoardPlayKit.WaitFor(() => map.SlipOpen, 2, "görev fişi açılmadı");
            yield return BoardPlayKit.WaitSeconds(.8f);
            Assert.AreEqual(sector.Region, slip.RegionId);
            var data = LiberationMapBuilder.Slip(Sim, slip.RegionId);
            Assert.IsTrue(data.Siege, "kuşatma kipi");
            Assert.IsTrue(slip.YieldShown, "BIRAK görünür");
            Assert.IsTrue(slip.YieldInteractable);
            Assert.AreEqual(Loc.F("{0} · {1}", FacilityDefenseText.YieldAction, FacilityDefenseText.YieldCost(Sim)), slip.YieldText);
            Assert.AreEqual(sector.Index, layer.Marks(sector.Region).SiegeMark, "haritada kuşatma işareti");
            Assert.AreEqual(Loc.T("KUŞATMA"), layer.Marks(sector.Region).StampWord, "cephede KUŞATMA damgası");

            // İlk tık onay sorar, komut yok
            int commands = Sim.Record.commands.Count, trust = Sim.S.Trust;
            BoardPlayKit.Click(slip.YieldButton.gameObject);
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(FacilityDefenseText.YieldConfirm(Sim), slip.YieldText, "onay sorusu");
            Assert.AreEqual(commands, Sim.Record.commands.Count, "ilk tık komut vermez");
            Assert.IsNotNull(Sim.PendingSiege);

            // İkinci tık: siege_yield
            BoardPlayKit.Click(slip.YieldButton.gameObject);
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(commands + 1, Sim.Record.commands.Count, "bir komut");
            Assert.AreEqual("siege_yield", Sim.Record.commands.Last().kind);
            Assert.AreEqual(trust - Sim.B.liberationCounterattackTrust, Sim.S.Trust, "güven −1");
            Assert.IsFalse(sector.Captured, "kanat düştü");
            Assert.AreEqual(sector.Id, Sim.S.LastCounterattack);
            var done = Telex.Lines.Last(l => l.status == Loc.T("EMİR UYGULANDI"));
            Assert.AreEqual(FacilityDefenseText.YieldDone(Sim, sector.Id), done.message);
            yield return BoardPlayKit.WaitSeconds(.6f);
            Assert.AreEqual(sector.Index, layer.Marks(sector.Region).State.Reoccupied, "haritada ✕");
            Assert.AreEqual(Loc.T("YENİDEN İŞGAL"), layer.Marks(sector.Region).StampWord);
            Assert.AreEqual(-1, layer.Marks(sector.Region).SiegeMark);

            // Fiş cephe fişine döner: BASKIN EMRİ VER Core gerekçesiyle kapalı, BIRAK yok
            Assert.IsTrue(map.SlipOpen, "fiş açık kalır");
            Assert.IsFalse(slip.YieldShown, "kuşatma bitti: tek düğme");
            Assert.AreEqual(SlipActionState.Disabled, slip.ActionState);
            Assert.AreEqual(Loc.T("Bu vardiya bırakılan kanada baskın verilemez."), LiberationMapBuilder.Slip(Sim, slip.RegionId).Reason);
            Assert.AreEqual(0, Count(FacilityDefenseText.FallStatus), "komutla bırakmada YZ KARŞI SALDIRISI teleksi basılmaz (teleks komutta)");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Siege_AutoYield_AtClose()
        {
            yield return Siege();
            // Devir fişinin uyarısı (fiş verisi Core'dan)
            var facts = HandoverSlipFactsBuilder.Build(Sim);
            Assert.IsTrue(facts.warnings.Any(w => w.kind == SlipWarningKind.Siege), "devir fişinde KARŞI SALDIRI kutusu");
            int prechecks = Count(Loc.T("ÖN KONTROL")), falls = Count(FacilityDefenseText.FallStatus), trust = Sim.S.Trust;

            // Gerçek şalter: kapak (ÖN KONTROL notu) ve kol, fiş EVRAK'a
            yield return ShiftPlayKit.PrintToTongue();
            Assert.AreEqual(prechecks + 1, Count(Loc.T("ÖN KONTROL")));
            StringAssert.Contains(Loc.T("karşı saldırıya karar verilmedi: bırakılacak"), Telex.Lines.Last(l => l.status == Loc.T("ÖN KONTROL")).message);
            yield return ShiftPlayKit.FileOnEvrak();
            yield return BoardPlayKit.WaitSeconds(1.5f);

            // Kapanış: kanat kendiliğinden bırakıldı, teleks bir kez
            Assert.IsNull(Sim.PendingSiege);
            Assert.AreEqual(Simulation.SiegeAuto, Sim.Facility.Sieges.Last().Outcome);
            Assert.IsFalse(sector.Captured);
            Assert.AreEqual(1, Sim.S.Counterattacks);
            Assert.AreEqual(trust - Sim.B.liberationCounterattackTrust, Sim.S.Trust, "güven −1");
            Assert.AreEqual(falls + 1, Count(FacilityDefenseText.FallStatus), "YZ KARŞI SALDIRISI bir kez");
            var line = Telex.Lines.Last(l => l.status == FacilityDefenseText.FallStatus);
            Assert.AreEqual(FacilityDefenseText.AutoYieldLine(Sim, sector.Id), line.message);
            Assert.AreEqual(TelexKind.Alarm, line.kind);
            // Dosyalanan fiş uyarıyı taşır
            var filed = ShiftPlayKit.Close.Archive.Last();
            Assert.IsTrue(filed.warnings.Any(w => w.kind == SlipWarningKind.Siege), "arşivdeki fişte KARŞI SALDIRI kutusu");
            // Yeni vardiyada uyarı ve ipucu yok; haritada ✕
            Assert.IsNull(FacilityDefenseText.PreCheck(Sim));
            Assert.IsFalse(HandoverSlipFactsBuilder.Build(Sim).warnings.Any(w => w.kind == SlipWarningKind.Siege));
            Assert.AreEqual(sector.Index, LiberationMapBuilder.Build(Sim).Find(sector.Region).Reoccupied);
        }
    }
}
