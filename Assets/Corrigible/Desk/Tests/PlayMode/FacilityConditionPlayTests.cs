using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using K = Corrigible.Desk.Tests.FacilityPlayKit;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 12, parça P4b — bitti ölçütü (aşama 12 tasarımı §11.7; "iki tesis oynanır"): Katar ve Yalpa gerçek DeskGame'de, Play'de
    /// oynanır. <see cref="Katar_WingPlayedFromSlip_FallDetachFollow"/>: WAYPOINT fişinden (bina "Sevkiyat katarı", OLAY "Katar") Enter →
    /// kadro raporu → Enter; giriş vagonunda kenar yok; ara vagonda kenar çizgileri (Core sayısı), gerçek Q ve tahta tıkıyla EMP makineyi
    /// kenardan düşürür (önizleme satırı, düşme canlandırması), Z geri getirir, yeniden düşürür; EMP kendi Analist'ini düşürür (kart ve telsiz
    /// fişi); hedeften kaçışta TAKİP EDER etiketi ve "1 makine takip eder", gerçek Q ile AYIR, Enter (rapor Decouple); dönüşte AYIR'sız
    /// Enter ile takip (giriş vagonu oynanır, gözcü satırı); sonuç kartının eki, operasyon raporunun satırı (5 vardiya, Alert), pano kartı,
    /// sonuç teleksi bir kez; kayıttan devamda yinelenme yok. <see cref="Yalpa_WingPlayedFromSlip_TiltShownAndSlides"/>: HALCYON fişi
    /// ("Yüzer maden platformu", OLAY "Yalpa"); giriş odasında şerit çipsiz; ara odada şerit okları, önceki turda soluk oklar ve "YALPA 2",
    /// yatış turunda kayma hatları (çarpanın ucu kırmızı, cıvatalı hatsız) ve hover; gerçek Enter: yatış canlandırması, birim, gövde, yük ve
    /// hurda Core'daki yeni karelerinde, duvara çarpan −1, cıvatalı yerinde; kayan makine önce kayar, sonra kayma karesinden yürür (ters
    /// sınama hedefi: itmenin kısayolu); kanat biter, rapor. Tohum 79 (Reach birkaç saniye). Kurulum (ışınlama, makineler, cıvata kademesi)
    /// test kurulumudur; emir, hamle, AYIR, GEÇ ve raporlar gerçek yoldan.
    /// </summary>
    public sealed class FacilityConditionPlayTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            yield return K.UnloadRaid();
            yield return ShiftPlayKit.End();
        }

        static Simulation Sim => BoardPlayKit.Sim;
        static SiginakOperation Operation => BoardPlayKit.Find<SiginakOperation>();
        static int Telexes(string status) => ShiftPlayKit.Telex.Lines.Count(l => l.status == status);
        static (int x, int y) TileOf(Vector3 w) => (Mathf.RoundToInt(w.x + 3.5f), Mathf.RoundToInt(3.5f - w.z));

        /// <summary>Hover kartı (gecikme dolana kadar beklenir).</summary>
        static IEnumerator HoverCard(LiveRaidDataSource src, int x, int y)
        {
            src.ClearHover();
            src.Hover(new Vector2Int(x, y));
            yield return BoardPlayKit.WaitFor(() => src.Current.HoverCard != null, 2, "hover kartı " + x + "," + y);
        }

        static bool Row(RaidInspectorModel card, string text, string detail = null) => card != null && card.Rows.Any(r => r.Text == text && (detail == null || r.Detail == detail));
        static string Rows(RaidInspectorModel card) => card == null ? "(kart yok)" : card.Title + ": " + string.Join(" | ", card.Rows.Select(r => r.Text + " [" + r.Detail + "]"));

        /// <summary>Masa: harekât bölgeye getirilir, rapor dosyalanır, fişte bina ve OLAY; Enter → kadro raporu → Enter (baskın emri).</summary>
        static IEnumerator OrderFromSlip(string region, string building, string ev, System.Action<Simulation> setup = null)
        {
            yield return BoardPlayKit.LoadDesk(tune: FacilityRewardPlayKit.Tune);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            Assert.IsNotNull(Sim.FacilityPlan, "tesis planı");
            FacilityRegionPlayKit.Reach(Sim, region);
            setup?.Invoke(Sim);
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.WaitSeconds(.5f);
            yield return FacilityRegionPlayKit.Drain();
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var slip = Object.FindFirstObjectByType<OperationSlip>(FindObjectsInactive.Include);
            map.ShowMissions();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
            yield return BoardPlayKit.WaitFor(() => map.SlipOpen, 2, "görev fişi açılmadı");
            yield return BoardPlayKit.WaitSeconds(.8f);
            Assert.AreEqual(region, slip.RegionId, "fiş hedef bölgenin");
            var data = LiberationMapBuilder.Slip(Sim, region);
            Assert.AreEqual(building, data.Building, "bina satırı");
            Assert.AreEqual(ev, data.Fields.First(f => f.Label == Loc.T("OLAY")).Value, "OLAY satırı");
            int commands = Sim.Record.commands.Count;
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitSeconds(.4f);
            Assert.IsTrue(map.SquadReportOpen, "kadro raporu açıldı");
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.Frames(3);
            Assert.IsTrue(Sim.Record.commands.Skip(commands).Any(c => c.kind == "liberation_raid"), "baskın emri kayıtta (liberation_raid)");
            yield return K.Source();
            Assert.AreEqual(region, Sim.S.Raid.Params.Region);
            Assert.AreEqual(region, K.Live.KitId, "bölgenin kiti");
        }

        /// <summary>Herkes kapıda (gerçek seçim ve kapıya giriş); önce makineler kalkar, <paramref name="setup"/> kurar.</summary>
        static IEnumerator AllToGate(LiveRaidDataSource src, System.Action setup = null)
        {
            K.Calm(src);
            setup?.Invoke();
            foreach (var u in K.Standing(src.State)) yield return K.ToGate(src, u);
            Assert.IsTrue(src.Current.CanPass, "herkes kapıda");
        }

        [UnityTest, Timeout(480000)]
        public IEnumerator Katar_WingPlayedFromSlip_FallDetachFollow()
        {
            // ---- 1–2. Masa ve emir
            yield return OrderFromSlip("na", "Sevkiyat katarı", "Katar");
            var src = K.Live;
            var board = K.Board;
            Assert.AreEqual("katar", Sim.S.Raid.Params.Event, "emir Katar şartıyla");

            // ---- 3. Giriş vagonu: şartsız, kenar çizgisi yok
            Assert.AreEqual(1, src.State.Room.Index);
            Assert.IsNull(src.State.Ev);
            Assert.AreEqual(0, board.HazardShownCount("brink"), "giriş vagonunda kenar yok");
            yield return K.ThroughGate(src);

            // ---- 4. Ara vagon: kenar, EMP ile makine ve Analist düşer
            var B = src.State;
            Assert.AreEqual(2, B.Room.Index);
            Assert.AreEqual("katar", B.Ev?.Id);
            Assert.IsTrue(src.Current.Log.Any(l => l.Text == Loc.F("— PAFTA {0}/3 · {1} —", 2, B.Room.Name)), "gözcü vagonun adını yazar");
            Assert.AreEqual(DeskText.Upper(B.Room.Name), src.Current.Place, "görev çipinde vagonun adı");
            Assert.AreEqual("Sevkiyat katarı", src.Current.BlueprintBuilding, "antette bina");
            int core = Baskin.Hazards(B).Edges.Count(e => e.Kind == "brink");
            Assert.Greater(core, 0);
            Assert.AreEqual(2 * core, board.HazardShownCount("brink"), "kenar çizgileri (çizgi + tırnak) Core sayısı kadar");
            yield return K.Settle(src, "kurulum öncesi");
            K.Calm(src); K.Floor(src); K.Park(src);
            var m = K.Machine(B, 3, 0);
            int mid = m.Id;
            var E = K.Team(src, "emp");
            K.Put(E, 3, 3); K.Put(K.Team(src, "ana"), 6, 6); K.Put(K.Team(src, "tek"), 5, 6);
            src.ClearHover();
            yield return K.Settle(src, "kurulum");
            var played = new List<Baskin.Fx>();
            System.Action<Baskin.Fx, int, int> watch = (fx, i, n) => played.Add(fx);
            board.EffectPlaying += watch;
            src.Select('E');
            yield return BoardPlayKit.PressKey(Key.Q);
            Assert.IsTrue(src.Armed, "gerçek Q EMP'yi kurar");
            yield return HoverCard(src, 3, 1);
            Assert.IsTrue(Row(src.Current.HoverCard, "Kenardan düşer · yok olur"), "önizleme: makine kenardan düşer");
            Assert.IsTrue(src.MarksFor(src.Current) != null);
            K.ClickTile(3, 1);
            Assert.IsTrue(B.Units.First(u => u.Id == mid).Dead, "makine kenardan düştü");
            StringAssert.Contains(Loc.F("kenardan düştü · {0}", FacilityRaidText.Cell(2, 3, 0)), src.Current.EventLine, "telsiz fişi");
            yield return K.Settle(src, "düşme");
            Assert.IsTrue(played.Any(f => f.K == "fall" && f.Unit == mid), "fall canlandırması oynadı");
            Assert.IsTrue(board.UnitPiece(mid) == null || !board.UnitPiece(mid).gameObject.activeSelf, "parça yok");
            yield return BoardPlayKit.PressKey(Key.Z);
            yield return K.Settle(src, "geri alma");
            B = src.State;
            Assert.IsFalse(B.Units.First(u => u.Id == mid).Dead, "Z makineyi geri getirir");
            Assert.IsTrue(board.UnitPiece(mid) != null && board.UnitPiece(mid).gameObject.activeSelf, "parça yeniden kuruldu");
            Assert.AreEqual((3, 0), TileOf(board.UnitPiece(mid).transform.position), "parça kendi karesinde");
            src.Select('E');
            yield return BoardPlayKit.PressKey(Key.Q);
            K.ClickTile(3, 1);
            yield return K.Settle(src, "yeniden düşme");
            Assert.IsTrue(B.Units.First(u => u.Id == mid).Dead, "Q + tık yeniden verildi");
            // EMP kendi Analist'ini düşürür (yeteneği yeniden açık: test kurulumu)
            var A = K.Team(src, "ana");
            K.Team(src, "emp").Acted = false;
            K.Put(A, 3, 0);
            src.ClearHover();
            yield return K.Settle(src, "Analist");
            src.Select('E');
            yield return BoardPlayKit.PressKey(Key.Q);
            yield return HoverCard(src, 3, 1);
            Assert.IsTrue(Row(src.Current.HoverCard, Loc.F("Vagondan düşer · {0} vardiya yatar", 5)), "önizleme: ekip üyesi");
            K.ClickTile(3, 1);
            Assert.IsTrue(A.Overboard, "Analist kenardan düştü");
            StringAssert.Contains(Loc.F("vagondan düştü · {0}", FacilityRaidText.Cell(2, 3, 0)), src.Current.EventLine, "telsiz fişi");
            yield return K.Settle(src, "ekip düşmesi");
            Assert.AreEqual("VAGONDAN DÜŞTÜ", src.Current.Team.Single(t => t.Letter == 'A').DownText, "kartta VAGONDAN DÜŞTÜ");
            Assert.IsTrue(board.UnitPiece(A.Id) == null || !board.UnitPiece(A.Id).gameObject.activeSelf, "kenardan düşen tahtada yatmaz");
            board.EffectPlaying -= watch;
            // Geçiş: dönüşte bu vagon oynansın diye bir makine geçitlerden uzakta ayakta kalır
            yield return AllToGate(src, () => K.Machine(src.State, 3, 6, mid));
            src.EndTurn();
            yield return K.Settle(src, "pafta 3");

            // ---- 5. Hedef vagon: kaçış (Teknisyen yeteneğini hedefe kullandı: AYIR bir sonraki turda)
            yield return K.TakeTarget(src);
            yield return K.EndTurnConfirmed(src);
            B = src.State;
            Assert.IsTrue(FacilityConditionText.IsKatarReturn(B), "hedefin kaçışı Katar'ın dönüş geçidi");

            // ---- 6. AYIR (gerçek Q, gerçek Enter)
            Baskin.Unit follower = null;
            yield return AllToGate(src, () => { K.Floor(src); var w = src.State.Room.West[0]; follower = K.Machine(src.State, w.X + 1, w.Y); });
            yield return K.Settle(src, "kapı");
            CollectionAssert.AreEqual(new[] { follower.Id }, Baskin.FollowerIds(src.State), "bağlantıya yakın makine takipçi");
            Assert.AreEqual(("TAKİP EDER", true), src.MarksFor(src.Current).Tags[follower.Id], "TAKİP EDER etiketi");
            Assert.AreEqual(Loc.F("{0} makine takip eder", 1), src.Current.PassNote, "EMRİ İLET notu");
            Assert.AreEqual("GEÇ", FacilityDefensePlayKit.EndLabel);
            yield return BoardPlayKit.PressKey(Key.Q);
            Assert.IsTrue(src.Detach, "gerçek Q AYIR'ı açar");
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual("AYIR", FacilityDefensePlayKit.EndLabel, "EMRİ İLET AYIR");
            Assert.IsFalse(src.MarksFor(src.Current).Tags.ContainsKey(follower.Id), "etiket kalkar");
            int filed = Sim.S.Raid.Reports.Count;
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => Sim.S.Raid.Reports.Count == filed + 1, 4, "AYIR raporu");
            Assert.IsTrue(Sim.S.Raid.Reports.Last().Decouple, "rapor Decouple");
            Assert.IsEmpty(Sim.S.Raid.Reports.Last().Follow);
            Assert.AreEqual("raid_room", Sim.Record.commands.Last().kind);
            yield return K.Settle(src, "AYIR geçişi");
            B = src.State;
            Assert.AreEqual(2, B.Room.Index, "dönüşte ara vagon oynanır (ayakta makine)");
            Assert.IsTrue(FacilityRaidText.IsReturnBoard(B));
            Assert.AreEqual(1, B.Units.Count(u => u.Side == 'e' && !u.Dead), "yalnız bu vagonda kalan makine");
            Assert.IsTrue(src.Current.Log.Any(l => l.Text == Loc.F("Vagon ayrıldı: PAFTA {0}", 3)), "telsiz kapanışı");

            // ---- 7. Takip: AYIR'sız Enter, giriş vagonu oynanır
            Baskin.Unit chaser = null;
            yield return AllToGate(src, () => { K.Floor(src); var w = src.State.Room.West[0]; chaser = K.Machine(src.State, w.X + 1, w.Y); });
            yield return K.Settle(src, "dönüş kapısı");
            Assert.AreEqual(Loc.F("{0} makine takip eder", 1), src.Current.PassNote);
            filed = Sim.S.Raid.Reports.Count;
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => Sim.S.Raid.Reports.Count == filed + 1, 4, "takip raporu");
            Assert.AreEqual(1, Sim.S.Raid.Reports.Last().Follow.Count, "rapor takipçiyi yazar");
            Assert.IsFalse(Sim.S.Raid.Reports.Last().Decouple);
            yield return K.Settle(src, "takip geçişi");
            B = src.State;
            Assert.AreEqual(1, B.Room.Index, "giriş vagonu oynanır (Skipped boş)");
            Assert.IsTrue(Baskin.IsFinalExit(B));
            var chasers = B.Units.Where(u => u.Side == 'e' && !u.Dead).ToList();
            Assert.AreEqual(1, chasers.Count, "takipçi giriş vagonunda");
            Assert.IsTrue(src.Current.Log.Any(l => l.Text.StartsWith(Loc.F("Gözcü · takip: {0} makine doğu geçidinde:", 1).TrimEnd())), "gözcü: takip");
            Assert.IsTrue(src.Current.Log.Any(l => l.Text == Loc.F("Takip: {0} makine · PAFTA {1}", 1, 1)), "telsiz kapanışı");
            yield return K.WalkOut(src);

            // ---- 8. Sonuç kartı
            yield return BoardPlayKit.WaitFor(() => src.Current.Finished, 6, "sonuç kartı");
            Assert.AreEqual(Loc.T("SEKTÖR GERİ ALINDI"), src.Current.ResultTitle);
            StringAssert.EndsWith(Loc.F("· {0} vagondan düştü", 1), src.Current.ResultRows.Last().Value, "ekip satırının eki");
            int retaken = Telexes("SEKTÖR GERİ ALINDI");
            var person = Sim.Board.People.First(p => p.Id == Sim.S.Raid.Params.Squad.First(s => s.Type == "ana").PersonId);
            yield return BoardPlayKit.WaitSeconds(.8f);
            src.Continue();
            yield return BoardPlayKit.WaitFor(() => !RaidLauncher.IsRaidLoaded, 8, "baskın sahnesi kapanmadı");
            var raidres = Sim.Record.commands.Last(c => c.kind == "raidres");
            Assert.AreEqual(1, RaidResult.Decode(raidres.a).Overboard.Count, "raidres: Overboard dolu");
            Assert.IsTrue(person.Alive, "kenardan düşen yaşar");
            Assert.AreEqual(5, person.Down, "5 vardiya yatış");

            // ---- 9. Rapor, pano, teleks
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, "operasyon raporu inmedi");
            yield return BoardPlayKit.WaitSeconds(1.2f);
            var report = OperationReportBuilder.Build(Sim);
            var row = report.Results.FirstOrDefault(r => r.Value == FacilityConditionText.ReportOverboard(5));
            Assert.IsNotNull(row, "Analist satırı: vagondan düştü · 5 vardiya yatış (" + string.Join(" | ", report.Results.Select(r => r.Label + "=" + r.Value)) + ")");
            Assert.AreEqual(OperationTone.Alert, row.Tone);
            var card = PlanningBoardSnapshotBuilder.Build(Sim, new BoardUiState(), false, false, false).People.First(p => p.Id == person.Id);
            Assert.AreEqual(Loc.F("Yatıyor · {0} v.", 5), card.Strip, "pano kartında yatış 5");
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => !Operation.Busy, 4, "rapor dosyalanmadı");
            yield return BoardPlayKit.WaitSeconds(.6f);
            Assert.AreEqual(retaken + 1, Telexes("SEKTÖR GERİ ALINDI"), "sonuç teleksi bir kez");

            // ---- 10. Kayıttan devam: yinelenme yok, yatış aynı
            int telex = Telexes("SEKTÖR GERİ ALINDI");
            Sim.Record.day = Sim.S.Day;
            SaveStore.Write(Sim.Record);
            yield return ShiftPlayKit.Continue();
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            yield return BoardPlayKit.WaitSeconds(1f);
            Assert.AreEqual(telex, Telexes("SEKTÖR GERİ ALINDI"), "kayıttan devamda teleks yinelenmez");
            Assert.IsFalse(Operation.ReportOpen, "rapor yeniden açılmaz");
            var again = Sim.Board.People.First(p => p.Id == person.Id);
            Assert.IsTrue(again.Alive);
            Assert.AreEqual(5, again.Down, "kayıttan devamda yatış aynı");
        }

        [UnityTest, Timeout(420000)]
        public IEnumerator Yalpa_WingPlayedFromSlip_TiltShownAndSlides()
        {
            // ---- 1. Masa ve emir (cıvata kademesi 1: kurulum)
            yield return OrderFromSlip("oc", "Yüzer maden platformu", "Yalpa", s => s.Facility.Counter[FacilityCounters.Index(FacilityCounters.Push)] = 1);
            var src = K.Live;
            var board = K.Board;
            Assert.AreEqual("yalpa", Sim.S.Raid.Params.Event);

            // ---- 2. Giriş odası şartsız: şerit çipsiz
            Assert.IsNull(src.State.Ev);
            Assert.IsTrue(src.Current.TurnEvents == null || src.Current.TurnEvents.All(t => t == null), "şerit çipsiz");
            yield return K.ThroughGate(src);

            // ---- 3. Ara oda (6 tur): şerit
            var B = src.State;
            Assert.AreEqual(2, B.Room.Index);
            Assert.AreEqual("yalpa", B.Ev?.Id);
            Assert.AreEqual(6, B.MaxTurns);
            Assert.AreEqual("Yüzer maden platformu", src.Current.BlueprintBuilding, "antette bina");
            Assert.AreEqual("←", src.Current.TurnEvents[2]);
            Assert.AreEqual("→", src.Current.TurnEvents[4]);
            Assert.IsTrue(src.Current.TurnEvents.Length <= 6 || src.Current.TurnEvents[6] == null, "6. turda yatış yok (son tur)");
            yield return K.Settle(src, "kurulum öncesi");
            K.Calm(src); K.Floor(src); K.Park(src);
            yield return K.Settle(src, "kurulum");

            // ---- 4. Tur 1: soluk oklar ve YALPA 2
            int col = Enumerable.Range(0, Baskin.N).Count(y => Baskin.CellAt(B, 0, y).T != "wall");
            Assert.AreEqual(col + 1, board.HazardShownCount("tilt_next"), "soluk oklar ve YALPA 2");
            Assert.AreEqual("YALPA 2", src.MarksFor(src.Current).Hazards.Marks.Single(h => h.Kind == "tilt_next").Label);
            Assert.AreEqual(0, board.HazardShownCount("slide"));
            yield return K.EndTurnConfirmed(src);
            Assert.AreEqual(2, B.Turn, "yatış turu");

            // ---- 5. Tur 2: kurulum, hatlar, hover
            K.Calm(src); K.Floor(src);
            var A = K.Team(src, "ana"); var T = K.Team(src, "tek"); var E = K.Team(src, "emp");
            K.Put(A, 0, 3);
            K.Put(T, 7, 0);
            K.Put(E, 3, 6); K.MakeBody(src, E);
            // Açıktaki makine: batısı boş zemin olan kare (kapalı kepenk ve sandık nişi duvar kalır)
            var spot = (from y in new[] { 2, 3, 1, 4 } from x in new[] { 4, 3, 5 }
                        where Baskin.CellAt(B, x, y).T == "floor" && Baskin.CellAt(B, x - 1, y).T == "floor" && Baskin.CellAt(B, x - 1, y).O == null
                              && Baskin.UnitAt(B, x, y) == null && Baskin.UnitAt(B, x - 1, y) == null && !Baskin.IsGateTile(B, x, y)
                        select (x, y)).First();
            var e = K.Machine(B, spot.x, spot.y);
            var bolted = K.AddMachine(src, "zdron", 5, 5);
            Baskin.CellAt(B, 2, 4).O = new Baskin.Obj { K = "hurda", Hp = 2, Max = 2 };
            B.CargoX = 6; B.CargoY = 1;
            src.ClearHover();
            yield return K.Settle(src, "yatış kurulumu");
            Assert.IsTrue(Baskin.Bolted(B, bolted), "imza makinesi cıvatalı");
            var slides = Baskin.Slides(B);
            Assert.AreEqual(slides.Count(l => !l.Hit) + 2 * slides.Count(l => l.Hit), board.HazardShownCount("slide"), "kayma hatları çizili");
            Assert.IsTrue(slides.Any(l => l.X == 0 && l.Y == 3 && l.Hit), "duvara bitişik uzmanın hattının ucu kırmızı (Hit)");
            Assert.IsFalse(slides.Any(l => l.X == 5 && l.Y == 5), "cıvatalıda hat yok");
            Assert.AreEqual(col + 1, board.HazardShownCount("tilt"), "koyu oklar ve YALPA 1");
            yield return HoverCard(src, e.X, e.Y);
            Assert.IsTrue(Row(src.Current.HoverCard, "Yatışta 1 kare kayar", "batı"), "hover: kayar · " + Rows(src.Current.HoverCard) + " · hatlar " + string.Join(";", slides.Select(l => l.X + "," + l.Y + ">" + l.X2 + (l.Hit ? "!" : ""))) + " · tur " + B.Turn + " e " + e.X + "," + e.Y);
            yield return HoverCard(src, 5, 5);
            Assert.IsTrue(Row(src.Current.HoverCard, "Cıvatalı: yatışta kaymaz"), "hover: cıvatalı · " + Rows(src.Current.HoverCard));
            src.ClearHover();
            // Beklenen: kopyada evre (makinelerin planı yatıştan sonra); kayan makine kaydıktan sonra yürümeli
            var copy = B.Clone();
            Assert.IsTrue(Baskin.EndTurn(copy));
            var slideE = slides.Single(l => l.X == e.X && l.Y == e.Y);
            Assert.IsFalse(slideE.Hit, "açıktaki makine kayar");
            var arrival = (slideE.X2, slideE.Y2);
            var planned = copy.Units.First(u => u.Id == e.Id);
            Assert.AreNotEqual(arrival, (planned.X, planned.Y), "kurulum: makine kaydıktan sonra yürür");
            int aHp = A.Hp;

            // ---- Gerçek Enter: yatış
            var lines = new List<string>();
            var fxs = new List<Baskin.Fx>();
            System.Action<Baskin.Fx, int, int> watch = (fx, i, n) => fxs.Add(fx);
            board.EffectPlaying += watch;
            (int, int)? walkFrom = null;
            int turn = B.Turn;
            yield return BoardPlayKit.PressKey(Key.Enter);
            if (src.Current.EndConfirm) yield return BoardPlayKit.PressKey(Key.Enter);
            float waited = 0;
            while ((src.Executing || src.Busy || B.Turn == turn) && waited < 15)
            {
                if (!string.IsNullOrEmpty(src.Current.EventLine) && !lines.Contains(src.Current.EventLine)) lines.Add(src.Current.EventLine);
                if (walkFrom == null && src.Current.EventLine == Loc.T("gözcü · makineler yer değiştiriyor") && board.UnitPiece(e.Id) != null)
                    walkFrom = TileOf(board.UnitPiece(e.Id).transform.position);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            board.EffectPlaying -= watch;
            yield return K.Settle(src, "yatış evresi");
            Assert.IsTrue(fxs.Any(f => f.K == "ev" && f.Obj == "yalpa" && f.Dx < 0), "yalpa canlandırması oynadı (batı)");
            Assert.IsTrue(lines.Contains("Yalpa · batıya"), "telsiz fişi: " + string.Join(" | ", lines));
            Assert.AreEqual((planned.X, planned.Y), (e.X, e.Y), "makine Core'daki karesinde");
            Assert.AreEqual(arrival, walkFrom, "makine önce kaydı, sonra kayma karesinden yürüdü");
            Assert.AreEqual(RaidBoard3D.W(e.X, e.Y), board.UnitPiece(e.Id).transform.position, "parça W(x,y)");
            // Cıvatalı yatışta tutar (kayma ve cıvata efekti yok); sonra planına yürüyebilir
            Assert.IsFalse(fxs.Any(f => (f.K == "slide" || f.K == "bolted") && f.Unit == bolted.Id), "cıvatalı kaymaz, sessiz tutar");
            var boltedPlanned = copy.Units.First(u => u.Id == bolted.Id);
            Assert.AreEqual((boltedPlanned.X, boltedPlanned.Y), (bolted.X, bolted.Y), "cıvatalı Core'daki karesinde");
            Assert.AreEqual(aHp - 1, A.Hp, "duvara çarpan 1 hasar");
            Assert.AreEqual((0, 3), (A.X, A.Y));
            Assert.AreEqual((2, 6), (E.X, E.Y), "gövde kaydı");
            Assert.AreEqual(RaidBoard3D.W(2, 6), board.UnitPiece(E.Id).transform.position, "gövdenin parçası");
            Assert.AreEqual((5, 1), (B.CargoX, B.CargoY), "yük kaydı");
            Assert.AreEqual("hurda", Baskin.CellAt(B, 1, 4).O?.K, "hurda kaydı");
            Assert.IsNotNull(board.ObjectPiece(1, 4), "hurdanın parçası yeni karede");
            Assert.IsNull(board.ObjectPiece(2, 4), "eski karede parça yok");
            Assert.AreEqual(RaidBoard3D.W(1, 4), board.ObjectPiece(1, 4).transform.position);

            // ---- 7. Kanadın kalanı, sonuç kartı, rapor (yatan EMP uzmanı kurulumdu: ayağa kalkar)
            E.Dead = false; E.Hp = E.Max;
            src.ClearHover();
            var visits = new List<string>();
            yield return K.PlayWing(src, visits);
            Assert.AreEqual(Loc.T("SEKTÖR GERİ ALINDI"), src.Current.ResultTitle);
            StringAssert.StartsWith(Sim.RegionName("oc") + " · ", src.Current.ResultPlace, "sonuç kartının yeri");
            yield return BoardPlayKit.WaitSeconds(.8f);
            src.Continue();
            yield return BoardPlayKit.WaitFor(() => !RaidLauncher.IsRaidLoaded, 8, "baskın sahnesi kapanmadı");
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, "operasyon raporu inmedi");
            var report = OperationReportBuilder.Build(Sim);
            Assert.IsTrue(report.Success);
            StringAssert.StartsWith(Sim.RegionName("oc") + " · ", report.Target, "raporun hedefi (bölge ve kanat)");
        }
    }
}
