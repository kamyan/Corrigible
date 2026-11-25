using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 11, parça P4b — bitti ölçütü (aşama 11 tasarımı "Bitti ölçütü" 1, §8.10; kesif_sunum §7.2): savunma baskını gerçek
    /// DeskGame'de, Play'de baştan sona oynanır. <see cref="Defense_PlayedFromSlipToReport"/>: iz bırakan gerçek kanat (lokumla yıkılan
    /// duvar), kapanış şalter ve EVRAK'la (kuşatma kurulur, teleks bir kez, harita işareti, CRT ipucu), karar vardiyasında kuşatma fişi (SAVUN,
    /// BIRAK, DALGA/İZ/RÖLE, paftada gedik, kadraj), SAVUN Enter → kadro raporu → Enter (<c>siege_defend</c>, S.Rng aynı), baskın sahnesinde
    /// on hücreli şerit ve dalga işaretleri, barikat turu, gerçek EMRİ İLET ile gelen dalga (wave/spawn efekti, telsiz), onaylı İÇERİ ÇEKİL,
    /// KANAT TUTULDU sonuç kartı, operasyon raporu ve sonuç teleksi birer kez, telsiz günlüğü, terk edilen odanın izi paftadan silinir, pano
    /// kilidi; kayıttan devamda hiçbiri yinelenmez. <see cref="Defense_Lost_RelayBroken"/>: hedef odaya çekilinir, röle kırılır, RÖLE KIRILDI
    /// / KANAT DÜŞTÜ, güven −(kanat + iki oda). Sayılar Core okuyucularından (dalga büyüklüğü, tur, bedeller) alınır.
    /// </summary>
    public sealed class FacilityDefensePlayTests
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

        static Rect ScreenRect(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            var cam = rt.GetComponentInParent<Canvas>()?.renderMode == RenderMode.ScreenSpaceOverlay ? null : rt.GetComponentInParent<Canvas>()?.worldCamera;
            var a = RectTransformUtility.WorldToScreenPoint(cam, c[0]);
            var b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static LiberationSectorState sector;

        /// <summary>Kanat komutla alınır (giriş odasında gerçek duvar yıkımı), operasyon raporu Enter ile dosyalanır.</summary>
        static IEnumerator TakeWing(bool breach)
        {
            FacilityDefensePlayKit.ResetBreach();
            yield return BoardPlayKit.LoadDesk(tune: FacilityDefensePlayKit.Tune);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            Assert.IsNotNull(Sim.FacilityPlan, "tesis planı");
            Assert.IsTrue(Sim.DefenseOn && Sim.MarksOn, "savunma ve iz alt bayrakları (C# varsayılanı)");
            sector = Sim.NextLiberationSector(Sim.S.Liberation.CurrentRegion);
            var started = Sim.StartLiberationRaid(sector.Id);
            Assert.IsTrue(started.Ok, started.Msg);
            FacilityRewardPlayKit.WinRaid(Sim, breach ? FacilityDefensePlayKit.Breach : (System.Action<Baskin.BState>)null);
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, "operasyon raporu inmedi");
            yield return BoardPlayKit.WaitSeconds(1.2f);
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => !Operation.Busy, 4, "rapor dosyalanmadı");
        }

        [UnityTest, Timeout(480000)]
        public IEnumerator Defense_PlayedFromSlipToReport()
        {
            // ---- 1. İz bırakan gerçek kanat
            yield return TakeWing(true);
            var breach = FacilityDefensePlayKit.BreachAt.Value;
            string breachRoom = FacilityDefensePlayKit.BreachRoom;
            Assert.IsTrue(Sim.Facility.Marks.Any(m => m.Room == breachRoom && m.Kind == FacilityMarks.Wall && m.X == breach.X && m.Y == breach.Y),
                "gedik raporla kalıcı tahtaya girdi");

            // ---- 2. Kapanış sunum yolundan (şalter, devir fişi, EVRAK): kuşatma kurulur, teleks bir kez
            int pending = Count(FacilityDefenseText.PendingStatus), falls = Count(FacilityDefenseText.FallStatus);
            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            yield return BoardPlayKit.WaitSeconds(1.5f);
            Assert.IsNotNull(Sim.PendingSiege, "kapanışta kuşatma kuruldu");
            Assert.AreEqual(sector.Id, Sim.SiegeSector?.Id, "son alınan kanat kuşatıldı");
            Assert.IsTrue(Sim.SiegeDecisionDue, "karar vardiyası");
            Assert.AreEqual(pending + 1, Count(FacilityDefenseText.PendingStatus), "KARŞI SALDIRI BEKLİYOR bir kez");
            Assert.AreEqual(TelexKind.Alarm, Telex.Lines.Last(l => l.status == FacilityDefenseText.PendingStatus).kind);
            Assert.AreEqual(falls, Count(FacilityDefenseText.FallStatus), "YZ KARŞI SALDIRISI basılmaz");
            Assert.AreEqual("KARŞI SALDIRI BEKLİYOR · SAVUN YA DA BIRAK", FacilityDefenseText.CrtHint(Sim), "CRT ipucu");
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var layer = Field<LiberationMapLayer>(map, "layer");
            Assert.AreEqual(sector.Index, layer.Marks(sector.Region).SiegeMark, "haritada kuşatma işareti");

            // ---- 3. Karar vardiyası: cephenin baskını Core gerekçesiyle kapalı; 3 kuşatma fişini açar
            Assert.AreEqual(Loc.T("Önce karşı saldırıya karar ver: savun ya da bırak."),
                Sim.LiberationRaidCheck(Sim.NextLiberationSector(sector.Region)?.Id ?? sector.Id), "cephede BASKIN EMRİ VER kapalı");
            var slip = Object.FindFirstObjectByType<OperationSlip>(FindObjectsInactive.Include);
            var plans = Object.FindFirstObjectByType<FacilityPlanSheets>(FindObjectsInactive.Include);
            map.ShowMissions();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
            yield return BoardPlayKit.WaitFor(() => map.SlipOpen, 2, "görev fişi açılmadı");
            yield return BoardPlayKit.WaitSeconds(.8f);
            var data = LiberationMapBuilder.Slip(Sim, slip.RegionId);
            Assert.AreEqual(sector.Region, slip.RegionId);
            Assert.IsTrue(data.Siege, "kuşatma kipi");
            Assert.AreEqual(SlipActionState.Ready, slip.ActionState, "SAVUN etkin");
            Assert.IsTrue(slip.YieldShown, "BIRAK görünür");
            CollectionAssert.IsSubsetOf(new[] { Loc.T("DALGA"), Loc.T("İZ"), Loc.T("RÖLE") }, data.Fields.Select(f => f.Label).ToList(), "SAHA: DALGA, İZ, RÖLE");
            Assert.IsTrue(map.PlansOpen, "paftalar açık");
            var sheet1 = plans.Data.Sheets.First();
            Assert.IsTrue(sheet1.Traces.Any(t => t.Kind == FacilityMarks.Wall && t.X == breach.X && t.Y == breach.Y), "paftada gedik (moloz taraması)");
            Assert.That(ScreenRect(slip.Paper).yMax, Is.LessThanOrEqualTo((float)Screen.height + 1), "fiş kadrajda (üst)");
            // Yükseklik eşlemeli ölçek: dikey yerleşim 1080'dekiyle aynı oranda; BIRAK (fişin en alt düğmesi) ekranın içinde
            var yieldRect = ScreenRect((RectTransform)slip.YieldButton.transform);
            Debug.Log("A11P4b fiş: kâğıt " + ScreenRect(slip.Paper) + " BIRAK " + yieldRect + " ekran " + Screen.width + "x" + Screen.height);
            Assert.That(yieldRect.yMin, Is.GreaterThanOrEqualTo(-0.5f), "BIRAK kadrajda (alt)");
            DeskLoc.Instance.SetLanguage(DeskLang.En);
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.WaitSeconds(.8f);
            Assert.IsTrue(map.SlipOpen && slip.YieldShown, "EN: fiş açık");
            Assert.That(ScreenRect(slip.Paper).yMax, Is.LessThanOrEqualTo((float)Screen.height + 1), "EN: fiş kadrajda (üst)");
            Assert.That(ScreenRect((RectTransform)slip.YieldButton.transform).yMin, Is.GreaterThanOrEqualTo(-0.5f), "EN: BIRAK kadrajda (alt)");
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.WaitSeconds(.6f);
            Assert.IsTrue(map.SlipOpen, "fiş açık kaldı");

            // ---- 4. SAVUN: Enter → kadro raporu → Enter (SAVUNMAYA ÇIK)
            uint rng = Sim.S.Rng.State;
            int commands = Sim.Record.commands.Count;
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitSeconds(.4f);
            Assert.IsTrue(map.SquadReportOpen, "kadro raporu açıldı");
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(commands + 1, Sim.Record.commands.Count, "bir komut");
            Assert.AreEqual("siege_defend", Sim.Record.commands.Last().kind);
            Assert.AreEqual(rng, Sim.S.Rng.State, "savunma emri S.Rng çekmez (tohum ayrı akıştan)");
            yield return FacilityPlayKit.Source();
            var src = FacilityPlayKit.Live;
            Assert.IsTrue(FacilityRaidSession.Applies(Sim));
            Assert.IsTrue(Simulation.IsDefense(Sim.S.Raid.Params), "Mode = defense");

            // ---- 5. Tahta: antet, on hücre, dalga işaretleri, amaç, gedik, barikat
            var B = src.State;
            StringAssert.EndsWith("· PAFTA 1/3 · SAVUNMA", FacilityDefensePlayKit.Heading);
            Assert.AreEqual(10, src.Current.Turns);
            Assert.AreEqual(10, FacilityDefensePlayKit.StripCells, "şeritte on hücre açık");
            var waves = Baskin.DefenseWaves(B);
            foreach (var w in waves) Assert.AreEqual(FacilityDefenseRaidText.WaveGlyph, src.Current.TurnEvents[w.Arrive], "dalga turu işaretli: " + w.Arrive);
            Assert.AreEqual(waves.Count, src.Current.TurnEvents.Count(e => e != null));
            Assert.AreEqual("Amaç: 10. tura dayan", src.Current.Goal);
            Assert.AreEqual(breachRoom, B.Room.Id);
            Assert.AreEqual("floor", Baskin.CellAt(B, breach.X, breach.Y).T, "gedik tahtada zemin");
            Assert.IsTrue(Field<System.Collections.Generic.Dictionary<int, GameObject>>(FacilityPlayKit.Board, "breaches").ContainsKey(breach.Y * Baskin.N + breach.X), "gedikte moloz parçası");
            Assert.IsTrue(Baskin.BarricadeTurn(B));
            Assert.IsFalse(B.Units.Any(u => u.Side == 'e' && !u.Dead), "barikat turunda makine yok");
            Assert.AreEqual(FacilityDefenseRaidText.BarricadeSlip, src.Current.EventLine);
            // Gerçek EMRİ İLET: ilk dalga batı girişinden gelir
            var w1 = waves.OrderBy(w => w.Arrive).First();
            var played = new System.Collections.Generic.List<Baskin.Fx>();
            System.Action<Baskin.Fx, int, int> watch = (fx, i, n) => played.Add(fx);
            FacilityPlayKit.Board.EffectPlaying += watch;
            for (int guard = 0; guard < 4 && B.Turn < w1.Arrive; guard++) yield return FacilityPlayKit.EndTurnConfirmed(src);
            FacilityPlayKit.Board.EffectPlaying -= watch;
            Assert.AreEqual(w1.Arrive, B.Turn);
            var arrived = B.Units.Where(u => u.Side == 'e' && !u.Dead).ToList();
            Assert.AreEqual(w1.Count, arrived.Count, "dalga 1'in makineleri (Core'un büyüklüğü)");
            Assert.AreEqual(Sim.DefenseWaveSize(B.Params.Tier), w1.Count);
            var waveFx = played.FirstOrDefault(f => f.K == "wave");
            Assert.IsNotNull(waveFx, "wave efekti oynadı");
            Assert.AreEqual(w1.N, waveFx.N);
            Assert.IsTrue(B.Room.West.Any(p => p.X == waveFx.X && p.Y == waveFx.Y), "dalga batı girişinden");
            Assert.AreEqual(w1.Count, played.Count(f => f.K == "spawn"), "her makineye spawn efekti");
            CollectionAssert.Contains(src.Current.Log.Select(l => l.Text).ToList(), Loc.F("Dalga {0}: {1} makine giriş kapısında.", w1.N, w1.Count), "telsizde DALGA 1");
            // Kapanış (inceleme bulgusu 7): dalga çatışması bir tur gerçekten oynanır (makineler kaldırılmadan, gerçek EMRİ İLET): makine
            // evresi işler (makineler yürür, vurur ya da düşer), ekip ayakta kalır, savunma sürer.
            var before = arrived.Select(u => (Unit: u, u.X, u.Y, u.Hp)).ToList();
            int teamHp = FacilityPlayKit.Standing(B).Sum(u => u.Hp + u.Armor);
            yield return FacilityPlayKit.EndTurnConfirmed(src);
            Assert.AreEqual("player", B.Phase, "savunma sürüyor");
            Assert.AreEqual(w1.Arrive + 1, B.Turn, "makine evresinden sonra yeni tur");
            Assert.IsTrue(before.Any(f => f.Unit.Dead || f.Unit.X != f.X || f.Unit.Y != f.Y || f.Unit.Hp != f.Hp)
                || FacilityPlayKit.Standing(B).Sum(u => u.Hp + u.Armor) != teamHp, "makine evresi oynandı");
            Assert.IsNotEmpty(FacilityPlayKit.Standing(B), "ekip ayakta");

            // ---- 6. İçeri çekilme (onaylı); pafta 2'nin ilk turu barikat, dalga işareti kayar
            yield return FacilityDefensePlayKit.FallBack(src, Sim.DefenseRoomTrust);
            B = src.State;
            StringAssert.EndsWith("· PAFTA 2/3 · SAVUNMA", FacilityDefensePlayKit.Heading);
            Assert.IsTrue(Baskin.BarricadeTurn(B), "iç odanın ilk turu barikat");
            var waves2 = Baskin.DefenseWaves(B);
            foreach (var w in waves2) Assert.AreEqual(FacilityDefenseRaidText.WaveGlyph, src.Current.TurnEvents[w.Arrive], "kayan dalga işareti: " + w.Arrive);

            // ---- 7. Bitiş: süre dolana kadar; KANAT TUTULDU
            yield return FacilityDefensePlayKit.HoldToEnd(src);
            var vm = src.Current;
            Assert.IsTrue(vm.Success);
            Assert.AreEqual("KANAT TUTULDU", vm.ResultTitle);
            Assert.AreEqual(Loc.F("{0} · güven −{1}", 1, Sim.DefenseRoomTrust), vm.ResultRows.First(r => r.Label == "Terk edilen oda").Value);
            Assert.IsTrue(vm.ResultRows.Any(r => r.Label == "Ekip"));
            int trust = Sim.S.Trust, results = Count("KANAT TUTULDU"), retaken = Count("SEKTÖR GERİ ALINDI");
            yield return FacilityDefensePlayKit.Continue(src);

            // ---- 8. Masa: rapor, teleks, telsiz, pafta, pano kilidi
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, "operasyon raporu inmedi");
            yield return BoardPlayKit.WaitSeconds(1.2f);
            var log = Sim.S.RaidLog.Last();
            Assert.AreEqual(Simulation.DefenseMode, log.Mode);
            Assert.IsTrue(log.Success);
            var report = OperationReportBuilder.Build(Sim, log);
            Assert.IsTrue(report.Defense);
            Assert.AreEqual("KANAT TUTULDU", report.Title);
            Assert.AreEqual(trust - Sim.DefenseRoomTrust - log.Collateral * Sim.B.liberationTrustPerCivilian, Sim.S.Trust, "güven −1 (terk edilen oda)");
            Assert.AreEqual(results + 1, Count("KANAT TUTULDU"), "sonuç teleksi bir kez");
            StringAssert.Contains(Loc.F("{0} oda terk", 1), Telex.Lines.Last(l => l.status == "KANAT TUTULDU").message);
            Assert.AreEqual(retaken, Count("SEKTÖR GERİ ALINDI"), "SEKTÖR GERİ ALINDI basılmaz");
            Assert.IsTrue(RadioLogBuilder.Entries(Sim, false).Any(e => e.text.Contains("tutuldu")), "telsiz günlüğünde tutuldu");
            Assert.IsTrue(sector.Captured, "kanat KYGA'da");
            Assert.IsNull(Sim.SiegeSector, "kuşatma bitti");
            Assert.IsFalse(Sim.Facility.Marks.Any(m => m.Room == breachRoom), "terk edilen odanın izi silindi");
            Assert.IsFalse(FacilityPlanBuilder.Build(Sim, sector.Region, sector.Id).Sheets.First().Traces.Any(), "pafta 1'de gedik yok");
            Assert.AreEqual(FacilityDefenseText.LockReason, PlanningBoardSnapshotBuilder.LockReason(Sim), "pano kilidi");
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => !Operation.Busy, 4, "rapor dosyalanmadı");
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.WaitSeconds(.6f);
            Assert.AreEqual(-1, layer.Marks(sector.Region).SiegeMark, "harita işareti kalktı");
            string marks = string.Join("|", Sim.Facility.Marks.Select(m => m.Room + m.Kind + m.X + m.Y));
            int pendingNow = Count(FacilityDefenseText.PendingStatus), resultsNow = Count("KANAT TUTULDU");

            // ---- 9. Kayıttan devam: hiçbiri yinelenmez
            Sim.Record.day = Sim.S.Day;
            SaveStore.Write(Sim.Record);
            yield return ShiftPlayKit.Continue();
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            yield return BoardPlayKit.WaitSeconds(1f);
            Assert.AreEqual(pendingNow, Count(FacilityDefenseText.PendingStatus), "kayıttan devamda kuşatma teleksi yinelenmez");
            Assert.AreEqual(resultsNow, Count("KANAT TUTULDU"), "kayıttan devamda sonuç teleksi yinelenmez");
            Assert.IsFalse(Operation.ReportOpen, "rapor yeniden açılmaz");
            Assert.IsNull(Sim.SiegeSector);
            Assert.AreEqual(marks, string.Join("|", Sim.Facility.Marks.Select(m => m.Room + m.Kind + m.X + m.Y)), "kayıttan: aynı izler");
            var map2 = Object.FindFirstObjectByType<SiginakMap>();
            Assert.AreEqual(-1, Field<LiberationMapLayer>(map2, "layer").Marks(sector.Region).SiegeMark, "kayıttan: harita işareti yok");
            Assert.IsFalse(FacilityPlanBuilder.Build(Sim, sector.Region, sector.Id).Sheets.First().Traces.Any(), "kayıttan: pafta aynı");
        }

        [UnityTest, Timeout(360000)]
        public IEnumerator Defense_Lost_RelayBroken()
        {
            yield return TakeWing(false);
            FacilityRewardPlayKit.Close(Sim);
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.Frames(3);
            Assert.IsTrue(Sim.SiegeDecisionDue, "kuşatma kuruldu, karar vardiyası");
            int trust = Sim.S.Trust, counter = Sim.S.Counterattacks;
            var defended = BoardPlayKit.Game.DefendSector();
            Assert.IsTrue(defended.Ok, defended.Msg);
            yield return FacilityPlayKit.Source();
            var src = FacilityPlayKit.Live;
            yield return FacilityDefensePlayKit.FallBack(src, Sim.DefenseRoomTrust);
            yield return FacilityDefensePlayKit.FallBack(src, Sim.DefenseRoomTrust);
            Assert.AreEqual(RoomType.Target, src.State.Room.Type, "hedef odasına çekildi");
            Assert.IsFalse(src.Current.CanPass, "hedef odasında İÇERİ ÇEKİL yok");
            Assert.AreEqual(Sim.DefenseRelayHpFor(src.State.Params.Perks), Baskin.RelayMax(src.State), "röle canı Core'dan");
            yield return FacilityDefensePlayKit.BreakRelay(src);
            var vm = src.Current;
            Assert.IsFalse(vm.Success);
            Assert.AreEqual("RÖLE KIRILDI", vm.ResultTitle);
            Assert.AreEqual("kırıldı", vm.ResultRows[0].Value);
            Assert.AreEqual(Loc.F("{0} · güven −{1}", 2, 2 * Sim.DefenseRoomTrust), vm.ResultRows[1].Value);
            yield return FacilityDefensePlayKit.Continue(src);
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, "operasyon raporu inmedi");
            var log = Sim.S.RaidLog.Last();
            Assert.IsFalse(log.Success);
            Assert.AreEqual("KANAT DÜŞTÜ", OperationReportBuilder.Build(Sim, log).Title);
            int expected = Sim.B.liberationCounterattackTrust + 2 * Sim.DefenseRoomTrust + log.Collateral * Sim.B.liberationTrustPerCivilian;
            Assert.AreEqual(3, Sim.B.liberationCounterattackTrust + 2 * Sim.DefenseRoomTrust, "kanat −1, iki oda −2 (varsayılan denge)");
            Assert.AreEqual(trust - expected, Sim.S.Trust, "güven −3");
            Assert.IsFalse(sector.Captured, "kanat düştü");
            Assert.AreEqual(counter + 1, Sim.S.Counterattacks);
            Assert.AreEqual(Simulation.SiegeLost, Sim.Facility.Sieges.Last().Outcome);
        }
    }
}
