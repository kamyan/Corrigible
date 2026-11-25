using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 10, parça D (aşama 10 tasarımı §9.8; sunum kabulü): önlem defteri gerçek DeskGame'de, Play'de, altı yüzeyde. Eşik 1
    /// (bellek dengesi; asset değişmez); bölge komutlarla tamamlanır, ilk odada gerçek bir koşu darbesi (<c>ActRush</c>) itme sayacını
    /// artırır. Sonuç teleksi "SEKTÖR GERİ ALINDI · … · bölge özgür" rapor inerken bir kez; ödül seçilince harita dizisinde "BÖLGE
    /// ÖZGÜR"den sonra iki "YZ ÖNLEMİ" satırı; kayıttan devamda ikisi de yinelenmez. Görev fişinde ÖNLEM (beş yuva doluyken KAZANIM'la tek
    /// satır, K37; kırmızı, BASKIN EMRİ VER kadrajda, paftalar fişi örtmez; TR ve EN), sonraki kanadın paftasında kırmızı kalem (çift cidar ve koku yayıcı kurulumu),
    /// sonraki baskında imza makinesinin etiketi CIVATALI.
    /// </summary>
    public sealed class FacilityCounterPlayTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            // Baskın sahnesi açıksa boşaltılır (FacilityRaidPlayTests kalıbı; süren baskın çözülmez, kayda girmez)
            yield return FacilityPlayKit.UnloadRaid();
            yield return BoardPlayKit.TearDown();
        }

        static Simulation Sim => BoardPlayKit.Sim;
        static SiginakServerReward Reward => BoardPlayKit.Find<SiginakServerReward>();
        static SiginakOperation Operation => BoardPlayKit.Find<SiginakOperation>();
        static TelexFeed Telex => ShiftPlayKit.Telex;

        static T Field<T>(object o, string name) =>
            (T)o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);

        static Rect ScreenRect(RectTransform r)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);
            return Rect.MinMaxRect(c.Min(p => p.x), c.Min(p => p.y), c.Max(p => p.x), c.Max(p => p.y));
        }

        static int Count(string status) => Telex.Lines.Count(l => l.status == status);

        /// <summary>Eşik 1: bir koşu darbesi itme önlemini öğretir.</summary>
        public static void Tune(Balance b)
        {
            FacilityRewardPlayKit.Tune(b);
            b.facilityCounterThreshold = 1;
        }

        /// <summary>
        /// Odanın ilk hamlesi gerçek bir koşu darbesi: makineler kalkar, ayaktaki bir uzmanın hattına (koşu mesafesinin bir ötesine) bir dron
        /// konur ve <c>Baskin.DoAction(ActRush)</c> ona çarpar; sayaç oda raporuna yazılır. Kurulum ışınlamadır, hamle Core'dandır.
        /// </summary>
        public static void RushOnce(Baskin.BState B)
        {
            if (B.TallyPush > 0 || B.Moves == null || B.Phase != "player" || B.Escape) return;
            foreach (var e in B.Units.Where(u => u.Side == 'e' && !u.Dead)) { e.Dead = true; e.Intent = null; }
            int k = B.Moves.RushRun + 1;
            foreach (var u in B.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).OrderBy(u => u.Slot).ToList())
                foreach (var d in Baskin.Dirs)
                {
                    int x = u.X + d[0] * k, y = u.Y + d[1] * k;
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) continue;
                    var c = Baskin.CellAt(B, x, y);
                    if (c.T != "floor" || c.O != null || Baskin.UnitAt(B, x, y) != null) continue;
                    var t = Baskin.UnitTypes["dron"];
                    var m = new Baskin.Unit { Id = B.NextId++, Type = "dron", Side = 'e', X = x, Y = y, Hp = t.Hp, Max = t.Hp };
                    B.Units.Add(m);
                    if (Baskin.ActionTargets(B, u, Baskin.ActRush).Any(p => p.X == x && p.Y == y) && Baskin.DoAction(B, u.Id, Baskin.ActRush, x, y))
                    {
                        Assert.AreEqual(1, B.TallyPush, "koşu darbesi itme sayacını artırdı");
                        u.Moved = false; u.Acted = false;
                        return;
                    }
                    B.Units.Remove(m);
                }
            // Bu odada hat yok (ör. ekip kapıda): sonraki odada ya da baskında denenir; öğrenme testin sonunda sınanır
            rushMisses++;
        }

        static int rushMisses;

        [UnityTest, Timeout(420000)]
        public IEnumerator Countermeasure_ShownEverywhere()
        {
            yield return BoardPlayKit.LoadDesk(tune: Tune);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            Assert.IsTrue(Sim.CountermeasuresOn, "önlem defteri açık (alt bayrak varsayılanı)");
            Assert.IsNotNull(Field<TelexFeed>(Operation, "telex"), "SiginakOperation.telex bağlı");
            int results = Count("SEKTÖR GERİ ALINDI"), alarms = Count("YZ ÖNLEMİ");

            // ---- bölge tamamlanır (her baskının ilk odasında bir koşu darbesi); öğrenme Core'da
            string region = FacilityRewardPlayKit.CompleteRegion(Sim, RushOnce);
            var log = Sim.S.RaidLog.Last();
            Assert.AreEqual(1, Sim.Facility.Learned.Count, "bölge tamamlanınca bir önlem (hatsız oda: " + rushMisses + ")");
            Assert.AreEqual(FacilityCounters.Push, Sim.Facility.Learned[0].Class);
            Assert.AreEqual("itme:1", log.LiberationReward.Countermeasure);
            Assert.AreEqual(1, Sim.CounterLevel(FacilityCounters.Push));

            // ---- rapor iner: sonuç teleksi bir kez
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, "operasyon raporu inmedi");
            Assert.AreEqual(results + 1, Count("SEKTÖR GERİ ALINDI"), "sonuç teleksi bir kez");
            var result = Telex.Lines.Last(l => l.status == "SEKTÖR GERİ ALINDI");
            Assert.AreEqual(OperationReportBuilder.Telex(Sim, log).Message, result.message);
            StringAssert.EndsWith("· bölge özgür", result.message);
            Assert.AreEqual(TelexKind.Done, result.kind);
            Assert.AreEqual(alarms, Count("YZ ÖNLEMİ"), "önlem rapor inerken duyurulmaz");
            yield return BoardPlayKit.WaitSeconds(1.2f);

            // ---- rapor dosyalanır, ödül kâğıdı çekmeceye iner, seçilir (kazanım; sunumun karar yolu, kamera masada kalır); harita dizisi:
            // BÖLGE ÖZGÜR, sonra iki YZ ÖNLEMİ
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return ShiftPlayKit.WaitDoor(Reward.View, "ödül kâğıdı", 4);
            Assert.IsTrue(Reward.Decide(1), "karar: kazanım");
            // Karar çekmeceye konur; şalter köprüden çekilir (kamera masada kalır, harita dizisi sürer)
            yield return BoardPlayKit.WaitFor(() => Reward.View.State == ShelterEventPaperView.Where.Outbox && !Reward.View.Moving, 4, "karar çekmeceye konmadı");
            BoardPlayKit.Find<SiginakPassDrawer>().ClickSwitch();
            yield return BoardPlayKit.WaitFor(() => !Reward.View.Visible && !Reward.Busy, 4, "kâğıt gönderilmedi");
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.WaitFor(() => Count("YZ ÖNLEMİ") >= alarms + 2, 6, "YZ ÖNLEMİ teleksi basılmadı");
            yield return BoardPlayKit.WaitSeconds(.5f);
            Assert.AreEqual(alarms + 2, Count("YZ ÖNLEMİ"), "önlem başına iki satır, bir kez");
            var lines = Telex.Lines.ToList();
            int free = lines.FindLastIndex(l => l.status == "BÖLGE ÖZGÜR");
            int first = lines.FindIndex(l => l.status == "YZ ÖNLEMİ");
            Assert.GreaterOrEqual(free, 0, "BÖLGE ÖZGÜR");
            Assert.AreEqual(free + 1, first, "YZ ÖNLEMİ BÖLGE ÖZGÜR'den hemen sonra");
            Assert.AreEqual("Cıvatalı ayak · 1. kademe · İmza makineleri itilmez ve çekilmez", lines[first].message);
            Assert.AreEqual("Kalan YZ'lerde geçerli · cevap: Söküm anahtarı", lines[first + 1].message);
            Assert.AreEqual(TelexKind.Alarm, lines[first].kind);
            CollectionAssert.Contains(FacilityCounterText.RadioLines(Sim).Select(x => x.Text).ToList(), "YZ önlemi · Cıvatalı ayak");

            // ---- yeni vardiya ve yeni cephe; kaydet ve kayıttan devam: duyurular yinelenmez
            FacilityRewardPlayKit.Close(Sim);
            Assert.IsTrue(Sim.SelectLiberationRegion(Sim.LiberationFrontier.First()).Ok, "yeni cephe");
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.Frames(3);
            Sim.Record.day = Sim.S.Day;
            SaveStore.Write(Sim.Record);
            yield return ShiftPlayKit.Continue();
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            yield return BoardPlayKit.WaitSeconds(1f);
            Assert.AreEqual(1, Sim.Facility.Learned.Count, "kayıttan: aynı defter");
            Assert.AreEqual(alarms + 2, Count("YZ ÖNLEMİ"), "kayıttan devamda önlem teleksi yinelenmez");
            Assert.AreEqual(results + 1, Count("SEKTÖR GERİ ALINDI"), "kayıttan devamda sonuç teleksi yinelenmez");
            Assert.IsFalse(Operation.ReportOpen, "rapor yeniden açılmaz");

            // ---- görev fişi: ÖNLEM satırı; sonraki kanadın paftası kırmızı kalemli (kurulum: yıkım ve köpek önlemi, odalar bilinir)
            Sim.Facility.Counter[FacilityCounters.Index(FacilityCounters.Breach)] = 1;
            Sim.Facility.Counter[FacilityCounters.Index(FacilityCounters.Dog)] = 1;
            Sim.Facility.Tally[FacilityCounters.Index(FacilityCounters.Build)] = 2;
            var next = Sim.NextLiberationSector(Sim.S.Liberation.CurrentRegion);
            foreach (var room in Sim.FacilityPlan.Wing(next.Id)) if (!Sim.Facility.Known.Contains(room.Id)) Sim.Facility.Known.Add(room.Id);
            ShiftPlayKit.RaiseChanged();
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var slip = Object.FindFirstObjectByType<OperationSlip>(FindObjectsInactive.Include);
            var plans = Object.FindFirstObjectByType<FacilityPlanSheets>(FindObjectsInactive.Include);
            var theme = SiginakTheme.Active;
            foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
            {
                DeskLoc.Instance.SetLanguage(lang);
                map.ShowMissions();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
                yield return BoardPlayKit.WaitFor(() => map.SlipOpen, 2, "görev fişi açılmadı");
                yield return BoardPlayKit.WaitSeconds(.8f);
                var data = LiberationMapBuilder.Slip(Sim, slip.RegionId);
                var fields = Field<OperationSlip.Field[]>(slip, "fields");
                Assert.AreEqual(LiberationMapBuilder.SlipFieldsMax, fields.Length);
                Assert.LessOrEqual(data.Fields.Count, LiberationMapBuilder.SlipFieldsMax, "en çok beş SAHA satırı");
                Assert.IsTrue(Sim.Facility.Perks.Count > 0, "seçilen kazanım");
                // K37: beş yuva doluyken KAZANIM ve ÖNLEM tek satır ("1 · ÖNLEM 3"); değilse ayrı ÖNLEM satırı ("3 önlem"); değer kırmızı
                var last = fields[data.Fields.Count - 1];
                Assert.IsTrue(last.root.activeInHierarchy, lang + ": son satır açık");
                Assert.AreEqual(data.Fields.Last().Label, last.label.text);
                Assert.AreEqual(data.Fields.Last().Value, last.value.text);
                bool merged = data.Fields.Last().Label == Loc.T("KAZANIM");
                Assert.AreEqual(data.Fields.Count == LiberationMapBuilder.SlipFieldsMax && merged || !merged, true);
                StringAssert.Contains(merged ? Loc.F("{0} · ÖNLEM {1}", Sim.Facility.Perks.Count, 3) : Loc.F("{0} önlem", 3), last.value.text);
                Debug.Log("A10D fiş satırları " + lang + ": " + string.Join(" | ", data.Fields.Select(f => f.Label + "=" + f.Value)));
                Assert.AreEqual(theme.ColorOf("status.threat"), last.value.color, "önlem varken değer kırmızı");
                var paper = ScreenRect(slip.Paper);
                var action = ScreenRect((RectTransform)Field<UnityEngine.UI.Button>(slip, "action").transform);
                Debug.Log("A10D fiş " + lang + ": kâğıt " + paper + " düğme " + action + " ekran " + Screen.width + "x" + Screen.height);
                Assert.That(paper.yMax, Is.LessThanOrEqualTo((float)Screen.height), lang + ": fiş kadrajda (üst)");
                // ÖNLEM fişi uzatmaz: önlem defteri kapalıyken (aşama 9'un fişi) BASKIN EMRİ VER aynı yerde (bellek dengesi, geri alınır)
                Sim.B.facilityCountermeasures = false;
                ShiftPlayKit.RaiseChanged();
                yield return BoardPlayKit.WaitSeconds(.8f);
                var baseAction = ScreenRect((RectTransform)Field<UnityEngine.UI.Button>(slip, "action").transform);
                Debug.Log("A10D fiş " + lang + " (defter kapalı): düğme " + baseAction + " satırlar " + LiberationMapBuilder.Slip(Sim, slip.RegionId).Fields.Count);
                Sim.B.facilityCountermeasures = true;
                ShiftPlayKit.RaiseChanged();
                yield return BoardPlayKit.WaitSeconds(.8f);
                Assert.AreEqual(baseAction.yMin, ScreenRect((RectTransform)Field<UnityEngine.UI.Button>(slip, "action").transform).yMin, 0.5f,
                    lang + ": ÖNLEM fişi uzatmaz");
                if (lang == DeskLang.Tr) Assert.That(action.yMin, Is.GreaterThanOrEqualTo(0f), "TR: BASKIN EMRİ VER kadrajda (beş satırlı fiş)");
                Assert.IsTrue(map.PlansOpen, "paftalar açık");
                Assert.IsFalse(ScreenRect(plans.Band).Overlaps(paper), lang + ": paftalar fişi örtmez");
                for (int i = 0; i < 2; i++)
                {
                    var sheet = plans.Sheet(i);
                    Assert.IsTrue(sheet.Data.Known);
                    Assert.Greater(sheet.Data.Counters.Count, 0, "giriş ve ara odada kırmızı kalem");
                    Assert.Greater(sheet.Sketch.Counter.Count, 0);
                    var pen = Field<UILineGraphic>(sheet, "counter");
                    Assert.IsTrue(pen.gameObject.activeInHierarchy, "kalem katmanı çizili");
                    Assert.AreEqual(theme.ColorOf("plan.counter"), pen.color);
                }
                Assert.IsTrue(map.HandleEscape());
                yield return BoardPlayKit.WaitSeconds(.5f);
                BoardPlayKit.Rig.ResetView();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            }
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);

            // ---- sonraki baskın: imza makinesinde CIVATALI
            var started = BoardPlayKit.Game.StartLiberationRaid(next.Id);
            Assert.IsTrue(started.Ok, started.Msg);
            yield return FacilityPlayKit.Source();
            var src = FacilityPlayKit.Live;
            Assert.AreEqual(1, src.State.Moves.Bolt, "emirdeki kademe");
            var sig = src.State.Units.FirstOrDefault(u => u.Side == 'e' && !u.Dead && FacilityCounters.SignatureMachine(u.Type));
            if (sig == null)
            {
                var spot = Enumerable.Range(0, 64).Select(i => new Baskin.Pt(i % 8, i / 8))
                    .First(p => Baskin.CellAt(src.State, p.X, p.Y).T == "floor" && Baskin.CellAt(src.State, p.X, p.Y).O == null
                        && Baskin.UnitAt(src.State, p.X, p.Y) == null && Baskin.CellAt(src.State, p.X, p.Y).Door == 0);
                sig = FacilityPlayKit.AddMachine(src, "zdron", spot.X, spot.Y);
            }
            FacilityMovesTestKitPlay.Republish(src);
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(("CIVATALI", true), src.MarksFor(src.Current).Tags[sig.Id], "imza makinesi cıvatalı");
            Assert.IsTrue(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t => t.isActiveAndEnabled && t.text.Contains("CIVATALI")),
                "tahtada CIVATALI etiketi çizili");
            Assert.IsTrue(FacilityRaidText.Spotter(src.State).Any(l => l.Text.StartsWith("Gözcü · cıvatalı")), "gözcü satırı");
        }
    }

    /// <summary>Veri kaynağının görünüm modelini yeniden kurar (ışınlamadan sonra; EditMode kitinin PlayMode karşılığı).</summary>
    static class FacilityMovesTestKitPlay
    {
        public static void Republish(RaidDataSource source) =>
            typeof(RaidDataSource).GetMethod("Publish", BindingFlags.Instance | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null).Invoke(source, null);
    }
}
