using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using K = Corrigible.Desk.Tests.AdmissionPlayKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Kapı ve kabul (Revizyon 3 · Aşama 6, R9/R17; PLAN §5.2 P1–P19, E §4.2). DeskGame gerçek sahnesiyle Play'de, tohum 79 (V.005'te kapıda
    /// Suna 9 <c>resident-9</c>, N-17/009, insan, kusursuz; gözetmen Boşta'nın ilk uygunu Cem; V.009 Eren, V.013 Banu; W0 C8). Kurulum günleri
    /// kayıt fikstürüyle (gerçek Core komutları, sonra KAYITTAN DEVAM; <see cref="AdmissionPlayKit.ContinueFrom"/>), test altındaki kapanış
    /// gerçek arayüzle (şalter → fiş → EVRAK). Tıklar gerçek ışınla ve Input System 1.19 modülünün kuralıyla (<see cref="AdmissionPlayKit.ClickHit"/>),
    /// sürüklemeler <see cref="PointerGesture"/> ile. Süreler token'dan (<c>admission.motion.*</c>, yerel <c>_local_a6</c>), PLAN'daki sayıyla da
    /// karşılaştırılır; tolerans token + iki kare. Hareket <c>Update</c>'te yürür: örnek <c>yield return null</c>'dan sonra alınır, ölçüm hareket
    /// durunca. Her sunum adımı komut yazmaz ve RNG çekmez; komut beklenen adımda tür, <c>a</c> ve gün denetlenir. Sahne bağlı değilse (W6'dan
    /// önce) testler nedeniyle düşer.
    /// </summary>
    public sealed class AdmissionPlayTests
    {
        PoseLog poses;
        AdmissionSoundLog sounds;
        readonly List<(string title, string body)> telex = new List<(string, string)>();
        Action<string, string> onTelex;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            K.ReleaseGesture();
            poses?.Dispose();
            poses = null;
            sounds?.Dispose();
            sounds = null;
            if (onTelex != null && K.Admission != null) K.Admission.TelexLine -= onTelex;
            onTelex = null;
            telex.Clear();
            if (BoardPlayKit.Presenter != null && !BoardPlayKit.Presenter.enabled) BoardPlayKit.Presenter.enabled = true;
            yield return ShiftPlayKit.End();
        }

        // ================================================================== ortak

        static Simulation Sim => K.Sim;
        static AdmissionStation St => K.Station;
        static SiginakCameraRig Rig => K.Rig;
        static float Tol => BoardPlayKit.FrameTolerance;

        void Logs()
        {
            poses?.Dispose();
            sounds?.Dispose();
            poses = new PoseLog(Rig);
            sounds = new AdmissionSoundLog(K.Admission);
        }

        void ListenTelex()
        {
            onTelex = (t, b) => telex.Add((t, b));
            K.Admission.TelexLine += onTelex;
        }

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        /// <summary>İşaretten sonraki komutlar tam olarak verilenler (tür, a), hepsi <paramref name="day"/>. günde.</summary>
        static void AssertCommands((int commands, uint rng) mark, string context, int day, params (string kind, string a)[] want)
        {
            var got = ShiftPlayKit.Since(mark.commands);
            string list = ShiftPlayKit.Describe(got);
            Assert.AreEqual(want.Length, got.Count, context + ": komutlar " + list);
            for (int i = 0; i < want.Length; i++)
            {
                Assert.AreEqual(want[i].kind, got[i].kind, context + " #" + i + ": " + list);
                Assert.AreEqual(want[i].a, got[i].a, context + " #" + i + " a: " + list);
                Assert.AreEqual(day, got[i].day, context + " #" + i + " gün: " + list);
            }
        }

        static SlipData SlipOf(AdmissionTool tool) => K.Last?.Slips.LastOrDefault(s => s != null && s.Tool == tool);

        /// <summary>Kıskaçtaki fişin kâğıdı (yüzünün anahtarıyla).</summary>
        static Transform SlipPaper(string key) => St.SlipPapers.FirstOrDefault(p => p != null && p.gameObject.activeInHierarchy &&
            p.GetComponentInChildren<FindingSlipFace>(true)?.Data?.Key == key);

        static float FoldedDeg => St.Top.RangeTo != 0 ? St.Top.RangeTo : 88f;
        static float DrawerOpen => St.Drawer.RangeTo != 0 ? St.Drawer.RangeTo : -0.22f;

        /// <summary>Kamera K1'e (MASA'nın yolu: <c>ResetView</c>) ve durgun.</summary>
        static IEnumerator ToDesk()
        {
            if (Rig.Current != SiginakPose.Desk)
            {
                if (SiginakAdmission.InFamily(Rig.Current) && St.Counter == CounterState.Down && Rig.Current == SiginakPose.Admission)
                    K.ClickCollider(K.DeskKeyHit, "MASA tuşu");
                else Rig.ResetView();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk, 3);
            }
            yield return K.Settle();
        }

        /// <summary>Gerçek kapanış K1'den: şalter (kapak, kol) → fiş dile → EVRAK'a sürükle → dosyalama biter.</summary>
        static IEnumerator RealClose()
        {
            yield return ToDesk();
            var sw = ShiftPlayKit.Switch;
            yield return BoardPlayKit.WaitFor(() => sw.Phase == ShiftPhase.Ready && !sw.Moving, 3, "şalter hazır değil (" + sw.Phase + " / " + sw.LockPlate + ")");
            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            yield return ToDesk();
        }

        /// <summary>Dosyalama, fiş EVRAK'a bırakıldıktan sonra her karede kabul örneğiyle (vuruşun zamanlaması dosyalamanın içinde).</summary>
        static IEnumerator FileWatching(List<AdmissionSample> samples)
        {
            int day = Sim.S.Day;
            yield return ShiftPlayKit.DragSlip(new[] { ShiftPlayKit.TrayScreen() }, 8, 2);
            Assert.IsTrue(ShiftPlayKit.Close.Filing, "EVRAK'a bırakılan fiş dosyalanır");
            yield return K.WatchUntil(samples, () => !ShiftPlayKit.Close.Filing && !ShiftPlayKit.Switch.Moving, 4, "dosyalama bitmedi");
            Assert.AreEqual(day + 1, Sim.S.Day, "EVRAK günü bir ilerletir");
        }

        /// <summary>Talep defterinin yaprağını GİDEN ağzına sürükler (defterin kendi bırakma ölçütüyle).</summary>
        static IEnumerator DragSheetToOut()
        {
            var pad = K.Pad;
            var panel = ShiftPlayKit.Get<Transform>(pad, "panel");
            Assert.IsNotNull(panel, "RequestPadTool.panel (AdmissionPanel kökü; W6)");
            var to = BoardPlayKit.ScreenPoint(panel.TransformPoint(RequestPadTool.OutSlotLocal));
            Assert.IsTrue(pad.OverOutSlot(to), "GİDEN ağzının ekran noktası bırakma alanında");
            var hit = K.HitOn(pad.Hit, "talep defteri");
            var g = BoardPlayKit.Press(hit);
            Assert.AreSame(pad.gameObject, g.Dragged, "sürüklemenin alıcısı defter");
            yield return g.Move(to, 8);
            yield return g.Hold(1);
            g.Release();
            yield return BoardPlayKit.Frames(1);
        }

        static string RequestSupervisorText(AdmissionSnapshot s) =>
            s?.Request?.Values != null && s.Request.Values.Length > AdmissionSnapshotBuilder.RequestSupervisor ? s.Request.Values[AdmissionSnapshotBuilder.RequestSupervisor] : null;

        static string CarbonSupervisorText()
        {
            var carbon = K.Pad.CarbonPaper;
            Assert.IsNotNull(carbon, "karbon kopya görünür");
            var face = carbon.GetComponentInChildren<RequestFormFace>(true);
            Assert.IsNotNull(face, "karbon kopyanın yüzü (RequestFormFace)");
            return face.Row(RequestFormFace.SupervisorRow)?.value?.text;
        }

        static IEnumerable<string> FolderTexts(FolderData f)
        {
            if (f == null) yield break;
            yield return f.Name;
            yield return f.TagText;
            foreach (var l in f.Left.Concat(f.Right)) { yield return l.Label; yield return l.Value; }
        }

        // ================================================================== P1 · vuruş

        /// <summary>
        /// P1 (R9a, D12): tohum 79 V.004 fikstürden devam (aday yok: lamba sönük, tezgâh katlı), V.004 kapanışı gerçek arayüzle → V.005: Suna 9.
        /// Dosyalamanın içinde tek vuruş: üç parlama 0 / 0,3 / 0,6 s'de, her biri <c>motion.doorLampFlashS</c> 0,08; toplam <c>doorKnockS</c> 0,9,
        /// sonra amber sabit; tek <c>Knock</c> sesi. Kamera kapıya dönmez; KAPI tuşu dikkat; tezgâh K1'den iner (konsollar, kanat 0°, lamba,
        /// çekmece −0,22). Komut yalnız <c>board_file</c>. <c>Changed</c> yeniden yayınlansa da ikinci vuruş yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Knock_NewCandidate_Once_NotOnLoad()
        {
            yield return K.Begin();
            var sim = K.FixtureGame();
            K.FixtureToDay(sim, K.FirstCandidateDay - 1);
            Assert.IsNull(sim.Admission.Candidate, "V.004'te aday yok");
            yield return K.ContinueFrom(sim);
            var door = K.Door;
            Assert.IsFalse(door.Knocking, "aday yokken vuruş lambası sönük");
            Assert.IsFalse(door.KnockPlaying, "yüklemede vuruş yok (D12)");
            Assert.AreEqual(CounterState.Folded, St.Counter, "aday yokken tezgâh katlı");
            float knockS = K.Token("motion.doorKnockS", 0.9f), flashS = K.Token("motion.doorLampFlashS", 0.08f);

            Logs();
            yield return RealCloseUntilTongue();
            var mark = ShiftPlayKit.Mark();
            var samples = new List<AdmissionSample>();
            yield return FileWatching(samples);
            yield return K.WatchUntil(samples, () => sounds.Count(AdmissionSound.Knock) > 0 && !door.KnockPlaying, 5, "vuruş bitmedi");
            yield return K.Watch(samples, 0.2f);

            Assert.AreEqual(K.FirstCandidate, K.CandidateId, "V.005 adayı");
            Assert.AreEqual(1, sounds.Count(AdmissionSound.Knock), "tek vuruş sesi (" + sounds + ")");
            var got = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, got.Count(c => c.kind == "board_file"), "tam bir board_file: " + ShiftPlayKit.Describe(got));
            Assert.IsTrue(got.All(c => c.kind == "board_file"), "kapanış dışında komut yok: " + ShiftPlayKit.Describe(got));

            // Üç parlama: yükselen kenarlar ve süreleri
            float knockAt = sounds.At(AdmissionSound.Knock);
            var rises = new List<float>();
            var lengths = new List<float>();
            for (int i = 1; i < samples.Count; i++)
            {
                if (samples[i].Flash && !samples[i - 1].Flash) rises.Add(samples[i].At);
                if (!samples[i].Flash && samples[i - 1].Flash && rises.Count > lengths.Count) lengths.Add(samples[i].At - rises[lengths.Count]);
            }
            Assert.AreEqual(3, rises.Count, "üç parlama (" + string.Join(", ", rises.Select(r => (r - knockAt).ToString("0.000"))) + ")");
            K.AssertDuration(rises[1] - rises[0], knockS / 3, "2. parlama");
            K.AssertDuration(rises[2] - rises[0], 2 * knockS / 3, "3. parlama");
            Assert.AreEqual(3, lengths.Count, "her parlama biter");
            foreach (var l in lengths) K.AssertDuration(l, flashS, "parlama süresi");
            float end = K.FirstAt(samples, s => !s.KnockPlaying, "vuruşun sonu", knockAt + 0.05f);
            K.AssertDuration(end - knockAt, knockS, "vuruş (doorKnockS)");
            Assert.IsTrue(door.Knocking && !door.LampFlashing, "vuruştan sonra amber sabit");
            Assert.IsTrue(door.Silhouette == null || !door.Silhouette.Visible, "koridor boş (siluet yok)");

            // Kamera dönmez; KAPI tuşu dikkat; tezgâh iner
            Assert.IsFalse(poses.Changes.Any(c => SiginakAdmission.InFamily(c.Pose)), "vuruşta kamera kapıya dönmez (" +
                string.Join(", ", poses.Changes.Select(c => c.Pose)) + ")");
            Assert.IsTrue(K.DoorKey.Attention, "KAPI tuşu dikkat (1 Hz yakut)");
            yield return K.Settle();
            Assert.AreEqual(CounterState.Down, St.Counter, "aday gelince tezgâh iner (K1'den)");
            Assert.AreEqual(0f, St.TopAngle, 1e-4f, "kanat 0°");
            Assert.AreEqual(0f, St.BracketProgress, 1e-4f, "konsollar açık");
            Assert.IsTrue(St.LampOn, "tezgâh lambası yanık");
            Assert.AreEqual(DrawerOpen, St.DrawerOffset, 1e-4f, "çekmece açık, belge tablada");
            Assert.IsTrue(St.DocumentInDrawer, "belge çekmecede");
            Assert.AreEqual(1, sounds.Count(AdmissionSound.CounterDown), "tek tezgâh inişi sesi");

            var before = ShiftPlayKit.Mark();
            ShiftPlayKit.RaiseChanged();
            yield return K.Watch(null, 0.3f);
            Assert.AreEqual(1, sounds.Count(AdmissionSound.Knock), "aynı aday için ikinci vuruş yok");
            Assert.IsFalse(door.KnockPlaying, "ikinci Changed vuruş başlatmaz");
            ShiftPlayKit.AssertUnchanged(before, "Changed yeniden yayını");
        }

        /// <summary>K1'den şalter ve fiş dile (dosyalama ayrı ölçülür).</summary>
        static IEnumerator RealCloseUntilTongue()
        {
            yield return ToDesk();
            var sw = ShiftPlayKit.Switch;
            yield return BoardPlayKit.WaitFor(() => sw.Phase == ShiftPhase.Ready && !sw.Moving, 3, "şalter hazır değil (" + sw.Phase + ")");
            yield return ShiftPlayKit.PrintToTongue();
        }

        // ================================================================== P2 · başvuru yok

        /// <summary>
        /// P2 (R9a): V.001, KAPI → K8: vuruş lambası sönük, tezgâh katlı (kanat 88°, konsollar ∓90°), tezgâh lambası ve <c>CounterSpot</c>
        /// kapalı, araçlar gizli. Tezgâhın üzerine gelme etiketi "KABUL TEZGÂHI · başvuru yok", kanat kalkmaz; tık pozu değiştirmez; komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Idle_CounterFolded_NoLift()
        {
            yield return K.Begin();
            Assert.IsNull(K.CandidateId, "V.001'de aday yok");
            Logs();
            yield return K.EnterDoor();
            Assert.AreEqual(SiginakPose.Door, Rig.Current, "KAPI tuşu K8'e götürür");
            Assert.IsFalse(K.Door.Knocking, "vuruş lambası sönük");
            Assert.AreEqual(CounterState.Folded, St.Counter, "tezgâh katlı");
            Assert.AreEqual(FoldedDeg, St.TopAngle, 1e-3f, "kanat katlı (88°)");
            Assert.AreEqual(1f, St.BracketProgress, 1e-4f, "konsollar katlı (∓90°)");
            Assert.IsFalse(St.LampOn, "tezgâh lambası sönük");
            var spot = ShiftPlayKit.Get<GameObject>(St, "counterSpot");
            if (spot != null) Assert.IsFalse(spot.activeSelf, "CounterSpot kapalı (istasyon kapısı; PoseLighting'den ayrı)");
            Assert.IsFalse(St.ToolsVisible, "katlı tezgâhta araçlar gizli");
            Assert.IsFalse(St.DocumentVisible, "belge yok");

            var hit = K.HitOn(K.CounterHit, "katlı tezgâh");
            var topBefore = St.Top.transform.localPosition;
            var ped = K.HoverHit(hit);
            yield return K.Watch(null, 0.2f);
            Assert.AreEqual(Loc.T("KABUL TEZGÂHI"), St.HoverTitle, "üzerine gelme başlığı");
            Assert.AreEqual(Loc.T("başvuru yok"), St.Line, "üzerine gelme satırı");
            Assert.Less(Vector3.Distance(St.Top.transform.localPosition, topBefore), 1e-6f, "katlı tezgâh üzerine gelince kalkmaz (4 mm yalnız inikken)");
            K.Unhover(hit, ped);
            K.ClickHit(K.HitOn(K.CounterHit, "katlı tezgâh"));
            yield return K.Watch(null, 0.3f);
            Assert.AreEqual(SiginakPose.Door, Rig.Current, "katlı tezgâha tık pozu değiştirmez");
            Assert.IsFalse(poses.Changes.Any(c => c.Pose == SiginakPose.Admission), "K8A'ya gidilmedi");
            BoardPlayKit.AssertNoCommand("başvurusuz kapı");
        }

        // ================================================================== P3 · pozlar ve geri zinciri

        /// <summary>
        /// P3 (§1.5, D13): V.005, KAPI → K8 → inik tezgâha tık → K8A (0,45) → çekmecedeki belge tezgâha (komutsuz) → belgeye tık → K8R
        /// (<c>admission.motion.readS</c> 0,25; çapa belgeden <c>readDistanceM</c> 0,318 m) → Esc → K8A → sürüklemesiz sağ tık → K8 → tezgâh
        /// → K8A → MASA tuşu → K1; KAPI → K8 → Esc → K1. Komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator K8_K8A_K8R_BackChain()
        {
            yield return K.AtFirstCandidate();
            Logs();
            float toDoor = Rig.PoseSeconds(SiginakPose.Desk, SiginakPose.Door);
            yield return K.EnterDoor();
            BoardPlayKit.AssertTransition(poses, SiginakPose.Door, toDoor, "K1 → K8");
            float toCounter = Rig.PoseSeconds(SiginakPose.Door, SiginakPose.Admission);
            Assert.AreEqual(0.45f, toCounter, 1e-4f, "K8 → K8A 0,45 s (PLAN §1.5)");
            yield return K.ToCounter();
            BoardPlayKit.AssertTransition(poses, SiginakPose.Admission, toCounter, "K8 → K8A");

            yield return K.TakeDocument();
            Assert.AreEqual(SiginakPose.Admission, Rig.Current, "belgeyi almak pozu değiştirmez");
            Assert.AreEqual(K.FirstCandidate, K.Admission.Ui.DocumentOnCounterFor, "belge tezgâhta (sunum durumu)");

            float readS = K.Token("admission.motion.readS", 0.25f), distance = K.Token("admission.readDistanceM", 0.318f);
            K.ClickCollider(K.DocumentHit, "tezgâhtaki belge");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.AdmissionRead);
            BoardPlayKit.AssertTransition(poses, SiginakPose.AdmissionRead, readS, "K8A → K8R");
            Assert.AreEqual(distance, Vector3.Distance(K.ReadAnchor.position, St.DocumentPaper.position), 1e-3f, "K8R çapası belgeden 0,318 m");
            BoardPlayKit.AssertAtAnchor(SiginakPose.AdmissionRead, "K8R");

            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Admission);
            yield return BoardPlayKit.RightDrag(BoardPlayKit.ScreenCentre, Vector2.zero, 0);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Door);
            yield return K.Settle();
            yield return K.ToCounter();
            K.ClickCollider(K.DeskKeyHit, "MASA tuşu");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            yield return K.EnterDoor();
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            BoardPlayKit.AssertNoCommand("pozlar ve geri zinciri");
        }

        // ================================================================== P4 · KARŞILAŞTIR

        /// <summary>
        /// P4 (R17b): büyüteç belgeye sürüklenir → tam bir <c>inspectApplicant papers</c>; fiş ağızdan çıkar (yol <c>slipS</c> 0,35 + kıskaca
        /// <c>slipDropS</c> 0,15), damga EŞLEŞİYOR (Suna 9 kodu <c>match</c>); işaretçi N-17 maddesine <c>markerS</c> 0,2, yakut yanık; büyüteç
        /// inceleme pozunda (alan <c>match</c>). İkinci kullanım komutsuz: ret sesi, satır "Bu inceleme zaten yapıldı.".
        /// </summary>
        [UnityTest]
        public IEnumerator Inspect_Papers_ByMagnifierDrag()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            float slipS = K.Token("admission.motion.slipS", 0.35f);
            float markerS = K.Token("admission.motion.markerS", 0.2f);
            Logs();
            var mark = ShiftPlayKit.Mark();
            string id = K.CandidateId;

            var mag = K.Magnifier;
            var hit = K.HitOn(mag.Hit, "büyüteç");
            var g = BoardPlayKit.Press(hit);
            Assert.AreSame(mag.gameObject, g.Dragged, "sürüklemenin alıcısı büyüteç");
            yield return g.Move(BoardPlayKit.ScreenPoint(St.DocumentPaper.position), 8);
            yield return g.Hold(1);
            g.Release();
            yield return BoardPlayKit.Frames(1);
            AssertCommands(mark, "büyüteç belgeye", K.FirstCandidateDay, ("inspectApplicant", "papers"));

            string key = AdmissionSnapshotBuilder.CandidateSlipKey(id, AdmissionTool.Papers);
            yield return BoardPlayKit.WaitFor(() => SlipPaper(key) != null, 1, "karşılaştırma fişinin kâğıdı görünmedi (" + string.Join(", ", St.SlipKeys) + ")");
            var paper = SlipPaper(key);
            var track = new List<(float at, Vector3 pos, float marker)>();
            yield return BoardPlayKit.WaitFor(() => { track.Add((Time.unscaledTime, paper.position, St.MarkerOffset)); return K.Still(); }, 3, "fiş ve işaretçi durmadı");
            var final = paper.position;
            float landed = track.First(t => Vector3.Distance(t.pos, final) < 1e-5f).at;
            // A10 G06 (R17_inspect): cetvel belgedeyken karşılaştırma fişi ağızda kalır; kıskaca cetvel kalkınca düşer
            K.AssertDuration(landed - sounds.At(AdmissionSound.SlipOut), slipS, "fiş ağızda durur");
            Assert.AreEqual(1, sounds.Count(AdmissionSound.SlipOut), "tek fiş tıkırtısı");

            Assert.AreEqual("match", Sim.AdmissionFindingCode(id, "papers"), "Suna 9 belgesi uyumlu (W0 C8)");
            var slip = SlipOf(AdmissionTool.Papers);
            Assert.IsNotNull(slip, "anlık görüntüde karşılaştırma fişi");
            Assert.AreEqual(SlipVerdict.Match, slip.Kind, "damga türü");
            Assert.AreEqual(Loc.T("EŞLEŞİYOR"), slip.Verdict, "damga sözcüğü (koddan; metinden değil)");
            Assert.AreEqual(K.Last.MarkerRule, St.MarkerRule, "işaretçi anlık görüntünün maddesinde");
            Assert.GreaterOrEqual(St.MarkerRule, 0, "N-17 serisi: talimatta madde var");
            Assert.IsTrue(St.JewelOn, "işaretçinin yakutu yanık");
            float markerEnd = St.MarkerOffset;
            if (Mathf.Abs(markerEnd) > 1e-4f)
            {
                Assert.AreEqual(1, sounds.Count(AdmissionSound.Marker), "işaretçi tıkı");
                float markerAt = sounds.At(AdmissionSound.Marker);
                float settled = track.First(t => t.at >= markerAt && t.marker == markerEnd).at;
                K.AssertDuration(settled - markerAt, markerS, "işaretçi (markerS)");
                Assert.GreaterOrEqual(markerEnd, St.MarkerPart.RangeTo - 1e-5f, "işaretçi alt sınırın (−0,17) üstünde");
            }
            Assert.AreEqual(MagnifierTool.MagnifierState.Inspect, mag.State, "büyüteç inceleme pozunda kalır");
            Assert.AreEqual("match", mag.FieldCode, "büyütecin alanı Core kodundan");

            var again = ShiftPlayKit.Mark();
            yield return K.ClickTool(AdmissionTool.Papers);
            yield return K.Watch(null, 0.25f);
            ShiftPlayKit.AssertUnchanged(again, "ikinci karşılaştırma");
            Assert.GreaterOrEqual(sounds.Count(AdmissionSound.Deny), 1, "ikinci kullanım ret sesi");
            Assert.AreEqual(Loc.T("Bu inceleme zaten yapıldı."), mag.Line, "üzerine gelme satırı Core'un iletisi");
        }

        // ================================================================== P5 · GÖRÜŞME

        /// <summary>
        /// P5 (R17b): K8'de interkoma tık → tam bir <c>inspectApplicant interview</c>; düğme 4 mm basılır, cızırtı <c>intercomCrackleS</c> 0,2
        /// sonra bırakılır, LED cızırtı + döküm (<c>transcriptPerCharS</c> 0,03 × harf) boyunca yanık; fiş gövdesi Core'un görüşme metni,
        /// harf harf dökülür.
        /// </summary>
        [UnityTest]
        public IEnumerator Inspect_Interview_ByIntercom()
        {
            yield return K.AtFirstCandidate();
            yield return K.EnterDoor();
            float crackleS = K.Token("admission.motion.intercomCrackleS", 0.2f), perChar = K.Token("admission.motion.transcriptPerCharS", 0.03f);
            Logs();
            var mark = ShiftPlayKit.Mark();
            var intercom = K.Intercom;
            var samples = new List<AdmissionSample>();
            yield return K.ClickTool(AdmissionTool.Interview);
            samples.Add(AdmissionSample.Now());
            AssertCommands(mark, "interkom", K.FirstCandidateDay, ("inspectApplicant", "interview"));
            float crackleAt = sounds.At(AdmissionSound.IntercomCrackle);
            var slip = SlipOf(AdmissionTool.Interview);
            Assert.IsNotNull(slip, "görüşme fişi");
            Assert.AreEqual(Loc.T(Sim.Admission.Candidate.Interview), slip.Body, "fiş gövdesi Core'un görüşme metni");
            yield return K.WatchUntil(samples, () => !intercom.LineOpen && !intercom.Playing, 15, "hat kapanmadı");

            Assert.AreEqual(IntercomPressed(intercom), samples.Max(s => s.Button), 1e-4f, "düğme 4 mm basılır");
            float released = K.FirstAt(samples, s => s.Button <= 0f, "düğme bırakıldı", crackleAt + 0.05f);
            K.AssertDuration(released - crackleAt, crackleS + IntercomTool.PressS, "cızırtı + bırakış");
            float closed = K.FirstAt(samples, s => !s.LineOpen, "LED söndü", crackleAt + 0.05f);
            K.AssertDuration(closed - crackleAt, crackleS + slip.Body.Length * perChar, "hat (cızırtı + döküm)");
            var face = SlipPaper(slip.Key)?.GetComponentInChildren<FindingSlipFace>(true);
            Assert.IsNotNull(face, "görüşme fişinin yüzü");
            Assert.IsFalse(face.Typing, "döküm bitti");
            Assert.AreEqual(face.BodyCharacters, face.VisibleCharacters, "gövde tam görünür");
        }

        static float IntercomPressed(IntercomTool t)
        {
            var button = ShiftPlayKit.Get<PartDriver>(t, "button");
            return button != null && button.RangeTo != 0 ? button.RangeTo : 0.004f;
        }

        // ================================================================== P6 · SOĞUK GÖZLEM

        /// <summary>
        /// P6 (R17b): K8'de (cam kapı kanadında, K8 kadrajında) buğu camına tık → tam bir <c>inspectApplicant breath</c>; kamera K8G'ye
        /// <c>toK8GS</c> 0,45; varışta kapak 105° <c>glassCoverS</c> 0,2, buğu <c>fogInS</c> 0,6'da belirir, <c>fogOutS</c> 2,5'te söner (ölçek
        /// 0,85'e). Suna 9 insan: buğu var. Cama tık geldiği poza (K8) döner.
        /// </summary>
        [UnityTest]
        public IEnumerator Inspect_Breath_ByColdGlass()
        {
            yield return K.AtFirstCandidate();
            yield return K.EnterDoor();
            float toGlass = K.Token("admission.motion.toK8GS", 0.45f), coverS = K.Token("admission.motion.glassCoverS", 0.2f);
            float fogIn = K.Token("admission.motion.fogInS", 0.6f), fogOut = K.Token("admission.motion.fogOutS", 2.5f);
            Logs();
            var mark = ShiftPlayKit.Mark();
            var glass = K.Glass;
            var samples = new List<AdmissionSample>();
            yield return K.ClickTool(AdmissionTool.Breath);
            AssertCommands(mark, "buğu camı", K.FirstCandidateDay, ("inspectApplicant", "breath"));
            Assert.AreEqual("fog", Sim.AdmissionFindingCode(K.CandidateId, "breath"), "Suna 9 insan: buğu var");
            yield return K.WatchUntil(samples, () => Rig.Current == SiginakPose.BreathGlass && !Rig.InTransition && sounds.Count(AdmissionSound.Fog) > 0 &&
                !glass.Playing, 6, "buğu oynamadı");
            BoardPlayKit.AssertTransition(poses, SiginakPose.BreathGlass, toGlass, "K8 → K8G");

            float coverAt = sounds.At(AdmissionSound.GlassCover);
            float opened = K.FirstAt(samples, s => s.Cover >= 105f - 0.01f, "kapak açıldı", coverAt);
            K.AssertDuration(opened - coverAt, coverS, "kapak (glassCoverS)");
            float fogAt = sounds.At(AdmissionSound.Fog);
            // Tutma yok (PLAN: 1 → 0 hemen): tepe karesi Update'ten sonra örneklendiğinde sönüş zaten başlamıştır, hiçbir örnek tam 1
            // olmaz. Tepe = buğunun en yüksek örneği (planlanan tepenin bir karesi içinde: ya son belirme ya ilk sönüş karesi); tam belirmiş olmalı.
            var fogSamples = samples.Where(s => s.At >= fogAt).ToList();
            Assert.IsNotEmpty(fogSamples, "buğu sesinden sonra örnek yok");
            float top = fogSamples.Max(s => s.Fog);
            Assert.Greater(top, 0.999f, "buğu tam belirir (tepe ≈ 1)");
            float peak = K.FirstAt(samples, s => s.Fog >= top, "buğu tepede", fogAt);
            K.AssertDuration(peak - fogAt, fogIn, "buğu belirir (fogInS)");
            float gone = K.FirstAt(samples, s => s.Fog <= 0f, "buğu söndü", peak + 0.01f);
            K.AssertDuration(gone - peak, fogOut, "buğu söner (fogOutS)");
            Assert.AreEqual(ColdGlassView.FogEndScale, samples.Last().FogScale, 1e-3f, "kenardan söner (ölçek 0,85)");
            Assert.AreEqual(SiginakPose.BreathGlass, Rig.Current, "K8G'den çıkış oyuncunun tıkıyla");

            var back = ShiftPlayKit.Mark();
            K.ClickCollider(glass.Hit, "buğu camı (K8G)");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Door);
            ShiftPlayKit.AssertUnchanged(back, "K8G'den geri (geldiği poz K8)");
        }

        // ================================================================== P7 · DIŞ KAYIT

        /// <summary>
        /// P7 (R17c, K4-A, D2): yaprak GİDEN ağzına sürüklenir → tam bir <c>inspectApplicant record</c>; gözetmen Boşta'nın ilk uygunu
        /// <b>Cem</b> (destenin üstü); BEKLENİYOR fişi (kesikli) ve karbonda "Cem". Gerçek kapanış → V.006: aday aynı, <c>record_done</c>, kod
        /// <c>match</c>. K1'de sonuç tutulur (BEKLENİYOR, yaprak GİDEN'de); ilk K8 varışında KAYIT BULUNDU ağızdan çıkar, yaprak deftere döner.
        /// </summary>
        [UnityTest]
        public IEnumerator Record_Pending_ThenResultOnFirstArrival()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            string id = K.CandidateId;
            var watcher = Sim.AdmissionWatcher(id);
            Assert.IsNotNull(watcher, "Boşta'da gözetmen var");
            Assert.AreEqual("Cem", watcher.Name, "gözetmen Boşta'nın ilk uygunu (W0 C1)");
            var deck = BoardPlayKit.Board != null ? BoardPlayKit.Board.Last : null;
            if (deck != null && deck.DeckOrder.Count > 0) Assert.AreEqual(watcher.Id, deck.DeckOrder[0], "aday talebinde gözetmen BOŞTA'nın üst kartı");

            Logs();
            var mark = ShiftPlayKit.Mark();
            yield return DragSheetToOut();
            AssertCommands(mark, "yaprak GİDEN'e", K.FirstCandidateDay, ("inspectApplicant", "record"));
            yield return K.Settle();
            Assert.AreEqual(1, sounds.Count(AdmissionSound.PadTear), "koparma sesi");
            Assert.AreEqual(RequestPadTool.PadState.Outgoing, K.Pad.State, "yaprak GİDEN'de");
            Assert.IsNotNull(K.Pad.CarbonPaper, "karbon kopya defterde");
            StringAssert.Contains("Cem", RequestSupervisorText(K.Last), "talep formunda gözetmen");
            StringAssert.Contains("Cem", CarbonSupervisorText(), "karbonda gözetmen");
            var pending = SlipOf(AdmissionTool.Record);
            Assert.IsNotNull(pending, "dış kayıt fişi");
            Assert.AreEqual(SlipVerdict.Pending, pending.Kind, "BEKLENİYOR");
            Assert.IsTrue(pending.Dashed, "kesikli damga");
            StringAssert.Contains("Cem", pending.Body, "BEKLENİYOR gövdesinde gözetmen");

            yield return RealClose();
            Assert.AreEqual(K.FirstCandidateDay + 1, Sim.S.Day);
            Assert.AreEqual(id, K.CandidateId, "dış kayıt adayı bir vardiya tutar");
            Assert.IsTrue(Sim.Admission.Candidate.Inspections.Contains("record_done"), "kapanışta sonuç geldi");
            Assert.AreEqual("match", Sim.AdmissionFindingCode(id, "record"), "kayıt belgeyle eşleşiyor");
            Assert.AreNotEqual(id, K.Admission.Ui.SeenResultFor, "K1'de sonuç görülmedi");
            Assert.AreEqual(SlipVerdict.Pending, SlipOf(AdmissionTool.Record)?.Kind, "K1'de sonuç tutulur (BEKLENİYOR)");
            Assert.AreEqual(RequestPadTool.PadState.Outgoing, K.Pad.State, "yaprak GİDEN'de bekler");

            sounds.Clear();
            yield return K.EnterDoor();
            yield return K.Settle();
            Assert.AreEqual(id, K.Admission.Ui.SeenResultFor, "ilk K8 varışında sonuç görüldü");
            string result = AdmissionSnapshotBuilder.ResultSlipKey(id);
            CollectionAssert.Contains(St.SlipKeys, result, "sonuç fişi kıskaçta");
            var found = K.Last.Slips.FirstOrDefault(s => s != null && s.Key == result);
            Assert.AreEqual(SlipVerdict.Found, found?.Kind, "KAYIT BULUNDU");
            Assert.AreEqual(Loc.T("KAYIT BULUNDU"), found?.Verdict);
            Assert.GreaterOrEqual(sounds.Count(AdmissionSound.SlipOut), 1, "sonuç fişi ağızdan çıktı");
            Assert.IsNull(K.Pad.OutgoingPaper, "yaprak GİDEN'den kalktı");
            foreach (StampChoice c in Enum.GetValues(typeof(StampChoice)))
                Assert.IsTrue(K.Last.Stamps[(int)c].Enabled, c + " damgası etkin (" + K.Last.Stamps[(int)c].Reason + ")");
        }

        // ================================================================== P8 · KABUL

        /// <summary>
        /// P8 (R17d, R9b, §1.11): KABUL damgası raftan belgeye sürüklenir: kalkış (<c>StampLift</c>), bırakıştan <c>stampPressS</c> 0,12 sonra
        /// basış (<c>StampPress</c>) ve tam bir <c>decideApplicant admit</c>. Dizi: <c>drawerS</c> 0,35'te kamera K8'e; tezgâh katlanır, çark
        /// ancak katlanış bitince (<c>OpenAtS</c>) 0 → 450° <c>admitWheelS</c> 0,5; kanat 95° <c>admitLeafS</c> 0,8; merdiven ışığı
        /// <c>stairLightS</c> 0,3; açık bekleme <c>admitHoldS</c> 0,6; kapanış 0,6 + <c>DoorClose</c>. Koridor boş, vuruş lambası sönük. Kart
        /// panoda Boşta destesinde uygunların sonunda; başlık "9/10 kişi".
        /// </summary>
        [UnityTest]
        public IEnumerator Stamp_Admit_DoorOpens_CardToBoard()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            var t = DecisionTimeline.From(SiginakTheme.Active);
            float pressS = K.Token("admission.motion.stampPressS", 0.12f);
            float wheelS = K.Token("admission.motion.admitWheelS", 0.5f), leafS = K.Token("admission.motion.admitLeafS", 0.8f);
            float stairS = K.Token("motion.stairLightS", 0.3f), holdS = K.Token("admission.motion.admitHoldS", 0.6f);
            Logs();
            var mark = ShiftPlayKit.Mark();
            float released = -1;
            yield return K.DragStamp(StampChoice.Admit, K.StampDropScreen, 8, () => released = Time.unscaledTime);
            var samples = new List<AdmissionSample>();
            yield return K.WatchUntil(samples, () => sounds.Count(AdmissionSound.StampPress) > 0, 2, "basış olmadı");
            AssertCommands(mark, "KABUL damgası", K.FirstCandidateDay, ("decideApplicant", "admit"));
            Assert.AreEqual(1, sounds.Count(AdmissionSound.StampLift), "kalkış sesi");
            float t0 = sounds.At(AdmissionSound.StampPress);
            K.AssertDuration(t0 - released, pressS, "basış (stampPressS)");
            yield return K.WatchUntil(samples, () => !K.Admission.Busy && !K.Door.Moving, 8, "kabul dizisi bitmedi");

            var toDoor = poses.Changes.LastOrDefault(c => c.Pose == SiginakPose.Door);
            Assert.AreEqual(SiginakPose.Door, toDoor.Pose, "kabulde kamera K8'e döner (D10)");
            K.AssertDuration(toDoor.At - t0, t.DocsS, "kamera dönüşü (drawerS)");

            float wheelStart = K.FirstAt(samples, s => s.Wheel > 0f, "çark döndü", t0);
            var atWheel = samples.First(s => s.At >= wheelStart);
            Assert.AreEqual(CounterState.Folded, atWheel.Counter, "çark dönerken tezgâh katlı (R9_open; eleştiri 3)");
            Assert.AreEqual(FoldedDeg, atWheel.Top, 1e-3f, "kanat katlı");
            Assert.IsFalse(atWheel.Lamp, "tezgâh lambası kanat açılmadan söner (3 gölgeli ışık)");
            Assert.IsFalse(atWheel.Knocking, "vuruş lambası sönük");
            K.AssertDuration(wheelStart - t0, t.OpenAtS, "çark başlangıcı (OpenAtS)");
            float wheelEnd = K.FirstAt(samples, s => s.Wheel >= 450f - 0.01f, "çark 450°", wheelStart);
            K.AssertDuration(wheelEnd - wheelStart, wheelS, "çark (admitWheelS)");
            float leafStart = K.FirstAt(samples, s => s.Leaf > 0f, "kanat kımıldadı", wheelStart);
            float leafOpen = K.FirstAt(samples, s => s.Leaf >= 95f - 0.01f, "kanat 95°", leafStart);
            K.AssertDuration(leafOpen - leafStart, leafS, "kanat (admitLeafS)");
            float stairStart = K.FirstAt(samples, s => s.Stair > 0f, "merdiven ışığı", wheelStart);
            float stairFull = K.FirstAt(samples, s => s.Stair >= 1f, "merdiven ışığı tam", stairStart);
            K.AssertDuration(stairFull - stairStart, stairS, "merdiven ışığı (stairLightS)");
            Assert.AreEqual(leafStart, sounds.At(AdmissionSound.Open), Tol, "DoorOpen kanat kımıldarken");
            float closeStart = K.FirstAt(samples, s => s.Leaf < 95f - 0.01f, "kanat kapanmaya başladı", leafOpen);
            K.AssertDuration(closeStart - leafOpen, holdS, "açık bekleme (admitHoldS)");
            float shut = K.FirstAt(samples, s => s.Leaf <= 0f, "kanat kapandı", closeStart);
            K.AssertDuration(shut - closeStart, 0.6f, "kapanış (DoorProp.CloseS)");
            Assert.AreEqual(closeStart, sounds.At(AdmissionSound.Close), Tol, "DoorClose kapanışta");
            Assert.IsFalse(samples.Any(s => s.Silhouette), "koridor boş (siluet yok)");
            Assert.IsFalse(samples.Any(s => s.Leaf > 0f && s.Lamp), "kanat açıkken tezgâh lambası yok");

            Assert.IsFalse(K.Admission.Busy, "dizi bitti");
            Assert.AreEqual(CounterState.Folded, St.Counter, "başka aday yok: tezgâh katlı");
            Assert.IsNull(K.CandidateId, "aday kapıdan girdi");
            var person = Sim.Board.People.Last();
            Assert.AreEqual(K.FirstCandidate, person.Id, "kişi panoya en sona eklendi");
            Assert.AreEqual("rest", person.Job, "Boşta'da");

            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            var s0 = BoardPlayKit.Board.Last;
            int eligible = s0.DeckOrder.Count(pid => { var p = Sim.Board.Person(pid); return p != null && p.Down <= 0 && !p.Restricted; });
            Assert.AreEqual(eligible - 1, s0.DeckOrder.IndexOf(K.FirstCandidate), "kart Boşta destesinde uygunların sonunda (" + string.Join(", ", s0.DeckOrder) + ")");
            Assert.IsNotNull(BoardPlayKit.Board.CardFor(K.FirstCandidate), "kartı panoda");
            StringAssert.Contains("9/10", s0.Header, "pano başlığı");
        }

        // ================================================================== P9 · KISITLI

        /// <summary>
        /// P9 (R17d, D10, D11): tık-tık ile KISITLI: tam bir <c>decideApplicant restricted</c>; iz "KISITLI KABUL" + "3 VARDİYA" (dengeden);
        /// kapı KISITLI'da da açılır (tasarımdan sapma, D10), kamera K8'e. Kişi kısıtlı, destede uygunlardan sonra; kartında KISITLI.
        /// </summary>
        [UnityTest]
        public IEnumerator Stamp_Restricted_ClickClick_DoorOpens()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            Logs();
            var mark = ShiftPlayKit.Mark();
            var face = St.DocumentPaper.GetComponentInChildren<AdmissionDocumentFace>(true);
            Assert.IsNotNull(face, "belgenin yüzü");
            string[] lines = null;
            bool shown = false;
            yield return K.ClickClickStamp(StampChoice.Restricted);
            var samples = new List<AdmissionSample>();
            yield return K.WatchUntil(samples, () =>
            {
                if (lines == null && sounds.Count(AdmissionSound.StampPress) > 0) { lines = face.Data?.StampLines; shown = face.ImpressionVisible; }
                return sounds.Count(AdmissionSound.StampPress) > 0;
            }, 2, "basış olmadı");
            AssertCommands(mark, "KISITLI damgası", K.FirstCandidateDay, ("decideApplicant", "restricted"));
            Assert.IsTrue(shown, "iz basışta belgede");
            CollectionAssert.AreEqual(AdmissionSnapshotBuilder.ImpressionLines(StampChoice.Restricted, Sim.B), lines, "iz satırları");
            Assert.AreEqual(Loc.F("{0} VARDİYA", Sim.B.admissionRestrictedShifts), lines?[1], "2. satır dengeden");
            yield return K.WatchUntil(samples, () => !K.Admission.Busy && !K.Door.Moving, 8, "dizi bitmedi");
            Assert.IsTrue(samples.Any(s => s.Leaf >= 95f - 0.01f), "KISITLI'da kapı açılır (D10)");
            Assert.IsTrue(poses.Changes.Any(c => c.Pose == SiginakPose.Door), "kamera K8'e");
            var p = Sim.Board.Person(K.FirstCandidate);
            Assert.IsNotNull(p, "kişi panoda");
            Assert.IsTrue(p.Restricted, "kısıtlı kabul");

            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            var s0 = BoardPlayKit.Board.Last;
            Assert.AreEqual(s0.DeckOrder.Count - 1, s0.DeckOrder.IndexOf(K.FirstCandidate), "kısıtlı kart destenin sonunda (uygunlardan sonra)");
            Assert.IsTrue(s0.Person(K.FirstCandidate).Restricted, "kartta KISITLI");
        }

        // ================================================================== P10 · REDDET

        /// <summary>P10 (R17d): REDDET: tam bir <c>decideApplicant reject</c>; çekmece açılıp kapanır (belge dışarı), kapı kapalı, kamera K8A'da, kart yok.</summary>
        [UnityTest]
        public IEnumerator Stamp_Reject_DoorStaysShut()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            Logs();
            var mark = ShiftPlayKit.Mark();
            int people = Sim.Board.People.Count;
            yield return K.DragStamp(StampChoice.Reject, K.StampDropScreen);
            var samples = new List<AdmissionSample>();
            yield return K.WatchUntil(samples, () => sounds.Count(AdmissionSound.StampPress) > 0, 2, "basış olmadı");
            AssertCommands(mark, "REDDET damgası", K.FirstCandidateDay, ("decideApplicant", "reject"));
            yield return K.WatchUntil(samples, () => !K.Admission.Busy, 6, "dizi bitmedi");
            yield return K.Settle();
            Assert.IsTrue(samples.Any(s => s.Drawer <= DrawerOpen + 1e-3f), "çekmece açıldı (belge dışarı döner)");
            Assert.IsFalse(samples.Any(s => s.Wheel > 0f || s.Leaf > 0f), "kapı kapalı kalır");
            Assert.AreEqual(0, sounds.Count(AdmissionSound.Open), "DoorOpen yok");
            Assert.AreEqual(SiginakPose.Admission, Rig.Current, "kamera K8A'da kalır");
            Assert.AreEqual(people, Sim.Board.People.Count, "kart yok");
            Assert.AreEqual(0f, St.DrawerOffset, 1e-4f, "çekmece kapalı");
            Assert.AreEqual(CounterState.Folded, St.Counter, "tezgâh katlandı");
        }

        // ================================================================== P11 · YARDIM

        /// <summary>
        /// P11 (R17d): YARDIM: tam bir <c>decideApplicant aid</c>; çekmece <c>drawerS</c> 0,35'te açılır, iki teneke görünür (<c>TinsOut</c>),
        /// bekleme sonra çekmece kapanır; ERZAK <c>admissionAidRations</c> (2) düşer; kapı kapalı, kamera K8A'da.
        /// </summary>
        [UnityTest]
        public IEnumerator Stamp_Aid_TinsAndRations()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            float drawerS = K.Token("admission.motion.drawerS", 0.35f);
            double supplies = Sim.Shelter.Supplies;
            Logs();
            var mark = ShiftPlayKit.Mark();
            yield return K.DragStamp(StampChoice.Aid, K.StampDropScreen);
            var samples = new List<AdmissionSample>();
            yield return K.WatchUntil(samples, () => !K.Admission.Busy && sounds.Count(AdmissionSound.StampPress) > 0, 8, "dizi bitmedi");
            AssertCommands(mark, "YARDIM damgası", K.FirstCandidateDay, ("decideApplicant", "aid"));
            Assert.AreEqual(supplies - Sim.B.admissionAidRations, Sim.Shelter.Supplies, 1e-9, "erzak yardım payı kadar düştü");
            Assert.AreEqual(1, sounds.Count(AdmissionSound.TinsOut), "tenekeler çıktı");
            Assert.IsTrue(samples.Any(s => s.Tins), "tenekeler göründü");
            float tinsAt = sounds.At(AdmissionSound.TinsOut);
            float open = K.FirstAt(samples, s => s.Drawer <= DrawerOpen + 1e-5f, "çekmece açıldı", tinsAt);
            K.AssertDuration(open - tinsAt, drawerS, "çekmece (drawerS)");
            Assert.IsFalse(samples.Any(s => s.Wheel > 0f || s.Leaf > 0f), "kapı kapalı");
            Assert.AreEqual(SiginakPose.Admission, Rig.Current, "kamera K8A'da");
            yield return K.Settle();
            Assert.IsFalse(St.TinsVisible, "tenekeler dışarı gitti");
        }

        // ================================================================== P12 · kilitler

        /// <summary>Kilitli damga: rafta pirinç dille; tık → ret sesi ve 2 mm titreme, kalkmaz; satır Core'un iletisi; komut ve RNG aynı.</summary>
        IEnumerator AssertStampLocked(StampChoice c, string coreReason, string shortReason)
        {
            var d = K.Last.Stamps[(int)c];
            Assert.IsFalse(d.Enabled, c + " kilitli");
            Assert.AreEqual(coreReason, d.Reason, c + ": neden Core'un iletisi aynen");
            if (shortReason != null) Assert.AreEqual(shortReason, d.ShortReason, c + ": kısa neden");
            var stamp = K.Stamp(c);
            Assert.IsFalse(stamp.Enabled, c + " görünümü kilitli");
            Assert.IsNotNull(stamp.LockTongue, c + ": pirinç dil (W6)");
            Assert.IsTrue(stamp.LockTongue.activeInHierarchy, c + ": pirinç dil görünür (D21)");
            Assert.AreEqual(coreReason, stamp.Line, c + ": üzerine gelme satırı Core'un iletisi");
            var mark = ShiftPlayKit.Mark();
            int denies = sounds.Count(AdmissionSound.Deny);
            K.ClickCollider(stamp.Hit, c + " (kilitli)");
            float shake = 0;
            float until = Time.unscaledTime + 0.4f;
            while (Time.unscaledTime < until) { yield return null; BoardPlayKit.TrackFrame(); shake = Mathf.Max(shake, Mathf.Abs(stamp.ShakeOffsetMm)); }
            Assert.AreEqual(denies + 1, sounds.Count(AdmissionSound.Deny), c + ": ret sesi");
            // Gerçek kareler sinüsün tepesine denk gelmeyebilir; 2 mm tepe EditMode'da tam zamanda ölçülür.
            float amplitude = K.Token("motion.board.denyShakeMm", 2f);
            Assert.That(shake, Is.InRange(amplitude * 0.5f, amplitude + 0.01f), c + ": görünür titreme, genliği aşmaz (mm)");
            Assert.AreEqual(StampDecision.StampState.Rack, stamp.State, c + ": kalkmaz");
            ShiftPlayKit.AssertUnchanged(mark, c + " kilitli tık");
        }

        /// <summary>
        /// P12a: Suna 9 ve Eren kabul edilmiş (10/10), V.013 Banu: KABUL ve KISITLI kapasite kilidinde ("KAPASİTE 10/10"); REDDET ve YARDIM açık.
        /// Varsayılan erzakla (24) 8 → 9 → 10 kişilik topluluk V.013'e erzaksız girer ve Core YARDIM'ı da (doğru olarak) kilitler; kapasite
        /// kilidini erzak kilidinden ayırmak için fikstür erzağı depo sınırından başlar (erzak RNG çekmez, kapı adımı kartlı kipte çalışmaz:
        /// geliş çizelgesi aynı, aşağıdaki resident-10/11 denetimleri bunu doğrular).
        /// </summary>
        [UnityTest]
        public IEnumerator Stamps_Locked_Capacity()
        {
            yield return K.Begin();
            var sim = K.FixtureGame(tweak: b => { b.liberationAlarmPerShift = 0; b.boardStartSupplies = b.boardSuppliesCapacity; });
            K.FixtureToDay(sim, 4);
            Ok(sim.DecideApplicant("admit"), "Suna 9 kabul");
            K.FixtureToDay(sim, 8);
            Assert.AreEqual("resident-10", sim.Admission.Candidate?.Resident?.Id, "V.009 Eren (W0 C8)");
            Ok(sim.DecideApplicant("admit"), "Eren kabul");
            K.FixtureToDay(sim, 12);
            Assert.AreEqual("resident-11", sim.Admission.Candidate?.Resident?.Id, "V.013 Banu (W0 C8; gözlenen " + sim.Admission.Candidate?.Resident?.Name + ")");
            yield return K.ContinueFrom(sim);
            int cap = Sim.B.boardResidentCapacity;
            Assert.AreEqual(cap, Sim.Board.People.Count(p => p.Alive), "topluluk dolu");
            Assert.GreaterOrEqual(Sim.Shelter.Supplies, Sim.B.admissionAidRations, "fikstür erzağı yardım payını karşılar (YARDIM kilidi kapasiteden ayrı)");
            Assert.IsNull(Sim.ApplicantDecisionCheck("aid"), "Core YARDIM'a izin verir");
            yield return K.ToCounter();
            Logs();
            string why = Sim.ApplicantDecisionCheck("admit");
            Assert.IsNotNull(why, "Core kapasiteyi reddeder");
            string shortWhy = Loc.F("KAPASİTE {0}/{1}", cap, cap);
            yield return AssertStampLocked(StampChoice.Admit, why, shortWhy);
            yield return AssertStampLocked(StampChoice.Restricted, Sim.ApplicantDecisionCheck("restricted"), shortWhy);
            Assert.IsTrue(K.Last.Stamps[(int)StampChoice.Reject].Enabled, "REDDET açık");
            Assert.IsTrue(K.Last.Stamps[(int)StampChoice.Aid].Enabled, "YARDIM açık");
        }

        /// <summary>P12b: Suna 9 kısıtlı kabul edilmiş, V.009 Eren: KISITLI "tek gözetim yeri dolu"; KABUL açık.</summary>
        [UnityTest]
        public IEnumerator Stamps_Locked_SingleRestricted()
        {
            yield return K.Begin();
            var sim = K.FixtureGame();
            K.FixtureToDay(sim, 4);
            Ok(sim.DecideApplicant("restricted"), "Suna 9 kısıtlı");
            K.FixtureToDay(sim, 8);
            Assert.IsNotNull(sim.Admission.Candidate, "V.009 aday");
            yield return K.ContinueFrom(sim);
            yield return K.ToCounter();
            Logs();
            yield return AssertStampLocked(StampChoice.Restricted, Sim.ApplicantDecisionCheck("restricted"), null);
            Assert.AreEqual(Loc.T("Tek gözetim yeri dolu; önce mevcut kısıtlı kabulü çöz."), K.Last.Stamps[(int)StampChoice.Restricted].Reason);
            Assert.IsTrue(K.Last.Stamps[(int)StampChoice.Admit].Enabled, "KABUL açık");
        }

        /// <summary>P12c: <c>admissionAidRations</c> erzaktan büyük (kayıt fikstürünün dengesi): YARDIM kilitli, kısa neden "erzak yetersiz".</summary>
        [UnityTest]
        public IEnumerator Stamps_Locked_Rations()
        {
            yield return K.Begin();
            var sim = K.FixtureGame(tweak: b => b.admissionAidRations = 30);
            K.FixtureToDay(sim, 4);
            Assert.IsNotNull(sim.Admission.Candidate, "V.005 aday");
            yield return K.ContinueFrom(sim);
            Assert.Less(Sim.Shelter.Supplies, Sim.B.admissionAidRations, "erzak yardım payından az");
            yield return K.ToCounter();
            Logs();
            yield return AssertStampLocked(StampChoice.Aid, Sim.ApplicantDecisionCheck("aid"), Loc.T("erzak yetersiz"));
            Assert.IsTrue(K.Last.Stamps[(int)StampChoice.Admit].Enabled, "KABUL açık");
        }

        /// <summary>P12d: fiş basılı (plan kilitli): bütün araçlar ve damgalar kilitli, neden "Vardiya planı kilitli."; komut yazılmaz.</summary>
        [UnityTest]
        public IEnumerator Locked_PlanPrinted_AllToolsAndStamps()
        {
            yield return K.AtFirstCandidate();
            yield return ShiftPlayKit.PrintToTongue();
            yield return BoardPlayKit.Frames(2);
            var mark = ShiftPlayKit.Mark();
            var s = K.Last;
            Assert.IsTrue(s.Locked, "plan kilitli: kabul kilitli");
            Assert.AreEqual(Loc.T("Vardiya planı kilitli."), s.LockReason);
            foreach (AdmissionTool tool in Enum.GetValues(typeof(AdmissionTool)))
                Assert.IsFalse(s.Tools[(int)tool].Enabled, tool + " kilitli");
            foreach (StampChoice c in Enum.GetValues(typeof(StampChoice)))
            {
                Assert.IsFalse(s.Stamps[(int)c].Enabled, c + " kilitli");
                Assert.IsTrue(K.Stamp(c).LockTongue != null && K.Stamp(c).LockTongue.activeInHierarchy, c + ": pirinç dil");
            }
            Assert.IsFalse(K.Admission.PressStamp(StampChoice.Admit), "damga komut yazmaz");
            Assert.IsFalse(K.Admission.UseTool(AdmissionTool.Papers), "araç komut yazmaz");
            ShiftPlayKit.AssertUnchanged(mark, "plan kilitliyken");
        }

        /// <summary>P12e: baskın sürerken bütün araçlar ve damgalar kilitli ("Baskın sürüyor."); komut yazılmaz.</summary>
        [UnityTest]
        public IEnumerator Locked_Raid()
        {
            yield return K.AtFirstCandidate();
            Ok(ShiftPlayKit.StartRaidDirect(), "baskın emri (doğrudan)");
            K.Admission.Refresh();
            var mark = ShiftPlayKit.Mark();
            var s = K.Last;
            Assert.IsTrue(s.Locked, "baskında kabul kilitli");
            Assert.AreEqual(Loc.T("Baskın sürüyor."), s.LockReason);
            foreach (AdmissionTool tool in Enum.GetValues(typeof(AdmissionTool))) Assert.IsFalse(s.Tools[(int)tool].Enabled, tool + " kilitli");
            foreach (StampChoice c in Enum.GetValues(typeof(StampChoice))) Assert.IsFalse(s.Stamps[(int)c].Enabled, c + " kilitli");
            Assert.IsFalse(K.Admission.PressStamp(StampChoice.Reject), "damga komut yazmaz");
            Assert.IsFalse(K.Admission.UseTool(AdmissionTool.Interview), "araç komut yazmaz");
            ShiftPlayKit.AssertUnchanged(mark, "baskındayken");
            yield return null;
        }

        // ================================================================== P13 · başka yere bırakış

        /// <summary>P13: damga belge dışına bırakılır → rafına 0,25 s kavis (<c>StampReturn</c>), komut yok.</summary>
        [UnityTest]
        public IEnumerator Stamp_DropElsewhere_ReturnsToRack()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            var stamp = K.Stamp(StampChoice.Admit);
            var rest = stamp.transform.position;
            Logs();
            var mark = ShiftPlayKit.Mark();
            float released = -1;
            yield return K.DragStamp(StampChoice.Admit, K.StampElsewhereScreen, 8, () => released = Time.unscaledTime);
            yield return BoardPlayKit.WaitFor(() => stamp.State == StampDecision.StampState.Rack, 2, "damga rafına dönmedi");
            float back = Time.unscaledTime;
            Assert.AreEqual(1, sounds.Count(AdmissionSound.StampReturn), "dönüş sesi");
            Assert.AreEqual(0, sounds.Count(AdmissionSound.StampPress), "basış yok");
            K.AssertDuration(back - released, K.Token("motion.board.returnArcS", 0.25f), "rafına dönüş");
            Assert.Less(Vector3.Distance(rest, stamp.transform.position), 1e-4f, "damga rafındaki yerinde");
            ShiftPlayKit.AssertUnchanged(mark, "belge dışına bırakış");
        }

        // ================================================================== P14 · talimat sürümleri

        /// <summary>
        /// P14 (R17e, D7, D12): tohum 79 V.004 kapanışı gerçek arayüzle → teleks "TALİMAT 2" (Core'un değişiklik metni; "VARDİYA DEVREDİLDİ"den
        /// sonra); ilk K8 varışında eski şerit <c>stripOutS</c> 0,25'te ESKİ cebine, yenisi <c>stripInS</c> 0,3'te yuvaya, işaretçi en üstte;
        /// görülen sürüm 2 (yan dosyaya); ikinci varışta oynamaz.
        /// </summary>
        [UnityTest]
        public IEnumerator Protocol_V2_TelexAndStripChange()
        {
            yield return K.Begin();
            var sim = K.FixtureGame();
            K.FixtureToDay(sim, 3);
            Assert.AreEqual(1, sim.Admission.Protocol.Version, "V.004'te talimat 1");
            yield return K.ContinueFrom(sim);
            ListenTelex();
            Logs();
            yield return RealClose();
            yield return BoardPlayKit.WaitFor(() => telex.Count > 0, 3, "TALİMAT teleksi gelmedi");
            Assert.AreEqual(2, Sim.Admission.Protocol.Version, "V.005'te talimat 2 (W0 C8)");
            Assert.AreEqual(Loc.F("TALİMAT {0}", 2), telex[0].title, "teleks başlığı");
            Assert.AreEqual(Loc.T(Sim.Admission.Protocol.Change), telex[0].body, "teleks gövdesi Core'un değişiklik metni");
            Assert.AreEqual(1, K.Admission.Ui.SeenOrdersVersion, "değişim henüz görülmedi");
            Assert.IsTrue(K.Last.OrdersUnseen, "anlık görüntü: görülmemiş talimat");

            float outS = K.Token("admission.motion.stripOutS", 0.25f), inS = K.Token("admission.motion.stripInS", 0.3f);
            var samples = new List<AdmissionSample>();
            BoardPlayKit.Click(K.DoorKey.Target.gameObject);
            yield return K.WatchUntil(samples, () => Rig.Current == SiginakPose.Door && !Rig.InTransition && !St.OrdersChanging && !St.Moving, 6, "talimat değişimi bitmedi");
            float start = K.FirstAt(samples, s => s.OrdersChanging, "şerit değişimi başladı");
            float end = K.FirstAt(samples, s => !s.OrdersChanging, "şerit değişimi bitti", start);
            K.AssertDuration(end - start, outS + inS, "şerit ESKİ'ye + yeni yuvaya");
            Assert.AreEqual(2, K.Admission.Ui.SeenOrdersVersion, "değişim görüldü");
            Assert.AreEqual(2, SiginakDeskStore.LoadAdmissionSeen(Sim.Record)?.ordersVersion, "görülen sürüm yan dosyada");
            Assert.AreEqual(2, St.StripPaper.GetComponentInChildren<OrdersStripFace>(true)?.Data?.Version, "yuvada sürüm 2");
            Assert.AreEqual(-1, K.Last.MarkerRule, "işaretçi en üstte (inceleme yok)");
            Assert.AreEqual(0f, St.MarkerOffset, 1e-5f, "işaretçi en üstte");

            yield return ToDesk();
            samples.Clear();
            BoardPlayKit.Click(K.DoorKey.Target.gameObject);
            yield return K.WatchUntil(samples, () => Rig.Current == SiginakPose.Door && !Rig.InTransition, 3, "K8'e varılmadı");
            yield return K.Watch(samples, 0.3f);
            Assert.IsFalse(samples.Any(s => s.OrdersChanging), "ikinci varışta değişim oynamaz");
        }

        /// <summary>
        /// P14b (R17e, PLAN §1.9): V.005'te soğuk gözlemle kabul edilen Suna 9 için V.009'da (talimat 3) İNCELEME FİŞİ: dosyada inceleme gerekli,
        /// ilk K8 varışında şerit değişir ve inceleme fişi kıskaca çıkar; panoda kartın İNCELE durumu.
        /// </summary>
        [UnityTest]
        public IEnumerator Protocol_V3_ReviewSlipForEarlierAdmission()
        {
            yield return K.Begin();
            var sim = K.FixtureGame();
            K.FixtureToDay(sim, 4);
            Ok(sim.InspectApplicant("breath"), "Suna 9 soğuk gözlem");
            Ok(sim.DecideApplicant("admit"), "Suna 9 kabul");
            K.FixtureToDay(sim, 7);
            yield return K.ContinueFrom(sim);
            ListenTelex();
            yield return RealClose();
            Assert.AreEqual(3, Sim.Admission.Protocol.Version, "V.009'da talimat 3");
            var file = Sim.Admission.File(K.FirstCandidate);
            Assert.IsTrue(file != null && file.ReviewNeeded, "R-4 talimatı buğuyla kabul edilen dosyayı işaretler (E §4.2 P14)");
            Assert.IsTrue(K.Last.Slips.Any(s => s != null && s.Review), "anlık görüntüde İNCELEME FİŞİ");
            yield return BoardPlayKit.WaitFor(() => telex.Count > 0, 3, "TALİMAT 3 teleksi");
            Assert.AreEqual(Loc.F("TALİMAT {0}", 3), telex[0].title);
            yield return K.EnterDoor();
            yield return K.Settle();
            var review = K.Last.Slips.First(s => s != null && s.Review);
            CollectionAssert.Contains(St.SlipKeys, review.Key, "İNCELEME FİŞİ kıskaçta");
            Assert.AreEqual(SlipVerdict.Review, review.Kind);
            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            Assert.IsTrue(BoardPlayKit.Board.Last.Person(K.FirstCandidate).Review, "kartta İNCELE");
        }

        /// <summary>
        /// P14c (R17e "başvuru olmasa da iner"): tohum 1 V.008 kapanışı (E §5.2: V.009'da aday yok, UNVERIFIED) → talimat 3 görülmemiş: tezgâh
        /// adaysız iner; ilk K8 varışında değişim oynar.
        /// </summary>
        [UnityTest]
        public IEnumerator Protocol_V3_CounterLowersWithoutApplicant_Seed1()
        {
            yield return K.Begin(1);
            var sim = K.FixtureGame(1);
            K.FixtureToDay(sim, 7);
            yield return K.ContinueFrom(sim);
            yield return RealClose();
            Assert.AreEqual(3, Sim.Admission.Protocol.Version, "V.009'da talimat 3");
            if (K.CandidateId != null)
                TestContext.WriteLine("UNVERIFIED (E §5.2): tohum 1 V.009'da kapıda aday var (" + Sim.Admission.Candidate.Resident.Name + "); adaysız iniş bu koşuda sınanmadı");
            else
            {
                yield return K.Settle();
                Assert.AreEqual(CounterState.Down, St.Counter, "görülmemiş talimatla tezgâh adaysız iner (K1'den)");
                Assert.IsFalse(St.DocumentVisible, "belge yok");
            }
            var samples = new List<AdmissionSample>();
            BoardPlayKit.Click(K.DoorKey.Target.gameObject);
            yield return K.WatchUntil(samples, () => Rig.Current == SiginakPose.Door && !Rig.InTransition && !St.OrdersChanging && !St.Moving, 6, "değişim bitmedi");
            Assert.IsTrue(samples.Any(s => s.OrdersChanging), "ilk varışta şerit değişti");
            Assert.AreEqual(3, K.Admission.Ui.SeenOrdersVersion);
        }

        // ================================================================== P15 · kayıttan devam

        /// <summary>
        /// P15 (D12, §1.2): V.005'te belge tezgâhta, KARŞILAŞTIR ve DIŞ KAYIT yapılmış → KAYITTAN DEVAM: dosyanın komutları canlıyla birebir, RNG
        /// aynı, biçim 15; devam komut yazmaz; vuruş çalmaz (amber); kamera K1; istasyon aynı (belge tezgâhta, karşılaştırma fişi ve BEKLENİYOR
        /// kıskaçta, yaprak GİDEN'de, büyüteç inceleme pozunda).
        /// </summary>
        [UnityTest]
        public IEnumerator SaveContinue_MidAdmission()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            yield return K.ClickTool(AdmissionTool.Papers);
            yield return K.Settle();
            yield return K.ClickTool(AdmissionTool.Record);
            yield return K.Settle();
            string id = K.CandidateId;
            Assert.IsTrue(Sim.Admission.Candidate.Inspections.Contains("papers") && Sim.Admission.Candidate.Inspections.Contains("record"), "iki inceleme yapıldı");
            var lines = ShiftPlayKit.CommandLines(Sim.Record);
            uint rng = Sim.S.Rng.State;
            var saved = SaveStore.Load();
            Assert.IsNotNull(saved, "kayıt yazıldı");
            Assert.AreEqual(15, saved.version, "kayıt biçimi 15 (Aşama 6 K4-A)");
            CollectionAssert.AreEqual(lines, ShiftPlayKit.CommandLines(saved), "kayıt dosyasının komutları canlıyla aynı");
            Assert.AreEqual(id, SiginakDeskStore.LoadAdmissionSeen(Sim.Record)?.documentFor, "belge tezgâhta yan dosyada");

            yield return ShiftPlayKit.Continue();
            K.RequireWired();
            CollectionAssert.AreEqual(lines, ShiftPlayKit.CommandLines(Sim.Record), "devamdan sonra komutlar birebir");
            Assert.AreEqual(rng, Sim.S.Rng.State, "RNG aynı");
            yield return BoardPlayKit.Frames(3);
            BoardPlayKit.AssertNoCommand("kayıttan devam");
            Assert.AreEqual(SiginakPose.Desk, Rig.Current, "devam K1'de (SavedPose)");
            Assert.IsTrue(K.Door.Knocking, "aday bekliyor: amber");
            Assert.IsFalse(K.Door.KnockPlaying, "yüklemede vuruş yok (D12)");
            Assert.IsTrue(K.Admission.Ui.DocumentOnCounter(id), "belge tezgâhta (sunum durumu)");
            Assert.IsTrue(St.DocumentOnCounter, "istasyonda belge tezgâhta");
            CollectionAssert.Contains(St.SlipKeys, AdmissionSnapshotBuilder.CandidateSlipKey(id, AdmissionTool.Papers), "karşılaştırma fişi kıskaçta");
            CollectionAssert.Contains(St.SlipKeys, AdmissionSnapshotBuilder.CandidateSlipKey(id, AdmissionTool.Record), "BEKLENİYOR kıskaçta");
            Assert.AreEqual(RequestPadTool.PadState.Outgoing, K.Pad.State, "yaprak GİDEN'de");
            Assert.AreEqual(MagnifierTool.MagnifierState.Rest, K.Magnifier.State, "devamda büyüteç dinlenmede (tasarım: yalnız karşılaştırma anında belgede)");
            Logs();
            yield return K.EnterDoor();
            Assert.AreEqual(0, sounds.Count(AdmissionSound.Knock), "devamdan sonra vuruş yok");
            BoardPlayKit.AssertNoCommand("devamdan sonra K8");
        }

        // ================================================================== P16 · kişi dosyasında iz

        /// <summary>
        /// P16 (R17f, D5, D17): tohum 79 V.009 Eren kabul + ÜRETİM'e (takas) → bağımsız iz olayları (E §5.1: V.011 erişim, V.012 parça;
        /// UNVERIFIED gün) → dosyada İZLER "İZ 1 · erişim kaydı" / "İZ 2 · parça kaydı"; dosyanın ve kartın hiçbir yazısında "robot" yok; portre
        /// olağan ("eren").
        /// </summary>
        [UnityTest]
        public IEnumerator Trace_InPersonFile_NoRobotWord()
        {
            yield return K.Begin();
            var sim = K.FixtureGame(tweak: b => b.liberationAlarmPerShift = 0);
            K.FixtureToDay(sim, 8);
            const string eren = "resident-10";
            Assert.AreEqual(eren, sim.Admission.Candidate?.Resident?.Id, "V.009 Eren (W0 C8)");
            Ok(sim.DecideApplicant("admit"), "Eren kabul");
            Ok(sim.AssignResident(eren, "craft"), "Eren ÜRETİM'e (takas)");
            bool Clues() => sim.Admission.Events.Count(e => e.PersonId == eren && (e.Kind == "clue_access" || e.Kind == "clue_component")) >= 2;
            for (int guard = 0; !Clues() && guard < 6; guard++) K.FixtureClose(sim);
            Assert.IsTrue(Clues(), "iki bağımsız iz olayı (gün " + sim.S.Day + ")");
            TestContext.WriteLine("iz günleri (gözlenen): " + string.Join(", ", sim.Admission.Events.Where(e => e.PersonId == eren && e.Kind.StartsWith("clue_"))
                .Select(e => e.Kind + " V." + (e.Shift + 1).ToString("000"))));
            yield return K.ContinueFrom(sim);

            yield return BoardPlayKit.EnterBoard();
            var board = BoardPlayKit.Board;
            var card = board.CardFor(eren);
            Assert.IsNotNull(card, "Eren'in kartı");
            BoardPlayKit.Click(card.gameObject);
            yield return BoardPlayKit.Frames(1);
            BoardPlayKit.Click(card.gameObject, 2);
            yield return BoardPlayKit.WaitFor(() => board.Folder.IsOpen && !board.Folder.Moving, 3, "Eren'in dosyası açılmadı");
            var folder = board.Last.Folder;
            Assert.IsNotNull(folder, "dosya verisi");
            Assert.AreEqual(eren, folder.PersonId);
            var values = folder.Right.Select(l => l.Value).ToList();
            CollectionAssert.Contains(values, Loc.T("İZ 1 · erişim kaydı"), "İZ 1");
            CollectionAssert.Contains(values, Loc.T("İZ 2 · parça kaydı"), "İZ 2");
            foreach (var e in Sim.Admission.Events.Where(e => e.PersonId == eren && e.Kind.StartsWith("clue_")))
                Assert.IsTrue(folder.Right.Any(l => l.Label == Loc.F("V.{0:000}", e.Shift + 1)), "iz satırının vardiyası " + (e.Shift + 1));
            foreach (var text in FolderTexts(folder).Where(x => !string.IsNullOrEmpty(x)))
                Assert.AreEqual(-1, text.IndexOf("robot", StringComparison.OrdinalIgnoreCase), "dosya verisi \"robot\" demez: " + text);
            foreach (var t in board.Folder.GetComponentsInChildren<TMP_Text>(false).Concat(card.GetComponentsInChildren<TMP_Text>(false)))
                Assert.AreEqual(-1, (t.text ?? "").IndexOf("robot", StringComparison.OrdinalIgnoreCase), "yazı \"robot\" demez: " + t.name + " “" + t.text + "”");
            Assert.AreEqual("eren", board.Last.Person(eren).PortraitKey, "portre olağan (PortraitLibrary)");
        }

        // ================================================================== P17 · okuma pozu

        /// <summary>
        /// P17 (R17b okuma, D8-A, D14): tezgâhtaki belgeye tık → K8R (<c>readS</c> 0,25); canlı ölçüm 1080p'de: ad ≥ 18 px, öteki yazılar ≥ 14 px,
        /// TR ve EN; ikinci tık K8A'ya döner; komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Reading_Document_K8R_Readable()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            Logs();
            K.ClickCollider(K.DocumentHit, "tezgâhtaki belge");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.AdmissionRead);
            BoardPlayKit.AssertTransition(poses, SiginakPose.AdmissionRead, K.Token("admission.motion.readS", 0.25f), "K8A → K8R");
            yield return K.Settle();
            var face = St.DocumentPaper.GetComponentInChildren<AdmissionDocumentFace>(true);
            Assert.IsNotNull(face, "belgenin yüzü");
            foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
            {
                if (DeskLoc.Instance.Lang != lang) DeskLoc.Instance.SetLanguage(lang);
                yield return BoardPlayKit.Frames(3);
                var bad = new List<string>();
                var all = new List<string>();
                foreach (var t in face.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (string.IsNullOrEmpty(t.text) || !t.enabled) continue;
                    float px = K.ScreenPx(t, BoardPlayKit.View);
                    float min = t == face.NameText ? 18 : 14;
                    all.Add(t.name + " " + px.ToString("0.0"));
                    if (px < min) bad.Add(t.name + " " + px.ToString("0.0") + "/" + min + " “" + t.text + "”");
                }
                TestContext.WriteLine("K8R belge (" + lang + "): " + string.Join(", ", all));
                Assert.IsEmpty(bad, "K8R'de okunurluk (" + lang + ")");
            }
            K.ClickCollider(K.DocumentHit, "belge (K8R, 2. tık)");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Admission);
            BoardPlayKit.AssertNoCommand("okuma");
        }

        // ================================================================== P18 · gözetmen yok

        /// <summary>
        /// P18 (K4-A, §1.8): kayıt fikstürü <c>boardStartResidents = 5</c> (görev dizisinin ilk beşi ceplerde: Boşta boş; eleştiri 9) → aday
        /// gelince talep defteri kilitli: kısa satır "gözetmen yok · BOŞTA boş", tam neden Core'un C2 iletisi; tık ret sesi, yaprak kalkmaz,
        /// komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator Supervisor_Empty_PadLocked()
        {
            yield return K.Begin();
            var sim = K.FixtureGame(tweak: b => b.boardStartResidents = 5);
            for (int guard = 0; sim.Admission.Candidate == null && guard < 8; guard++) K.FixtureClose(sim);
            string id = sim.Admission.Candidate?.Resident?.Id;
            Assert.IsNotNull(id, "aday geldi (gün " + sim.S.Day + ")");
            Assert.IsNull(sim.AdmissionWatcher(id), "Boşta boş: gözetmen yok (" + string.Join(", ", sim.Board.People.Select(p => p.Name + ":" + p.Job)) + ")");
            yield return K.ContinueFrom(sim);
            yield return K.ToCounter();
            var tool = K.Last.Tools[(int)AdmissionTool.Record];
            Assert.IsFalse(tool.Enabled, "DIŞ KAYIT kilitli");
            Assert.AreEqual(Loc.T("gözetmen yok · BOŞTA boş"), tool.Reason, "kısa satır");
            Assert.AreEqual(Sim.ApplicantInspectionCheck("record"), K.Last.Request.Reason, "tam neden Core'un iletisi (C2)");
            Assert.IsTrue(K.Last.Request.SupervisorMissing, "gözetmen yok");
            Assert.AreEqual(tool.Reason, K.Pad.Line, "defterin üzerine gelme satırı");
            Logs();
            var mark = ShiftPlayKit.Mark();
            var samples = new List<AdmissionSample>();
            yield return K.ClickTool(AdmissionTool.Record);
            yield return K.Watch(samples, 0.4f);
            Assert.GreaterOrEqual(sounds.Count(AdmissionSound.Deny), 1, "ret sesi");
            Assert.IsFalse(samples.Any(s => s.Sheet > 0f), "yaprak kalkmaz");
            Assert.AreEqual(RequestPadTool.PadState.OnPad, K.Pad.State);
            ShiftPlayKit.AssertUnchanged(mark, "gözetmensiz talep");
        }

        // ================================================================== P19 · dinamik gözetmen

        /// <summary>
        /// P19 (D2-A, W0 düzeltmesi): talepten sonra gözetmen Cem ÜRETİM'e alınır (takas: ÜRETİM'deki Suna BOŞTA'ya döner ve
        /// <c>Board.People</c> sırasında önde) → gözetmen <b>Suna</b>; talep formu, karbon ve BEKLENİYOR gövdesi canlı adla yeniden çizilir.
        /// Kapanışta sonuç gelir.
        /// </summary>
        [UnityTest]
        public IEnumerator Supervisor_MovedAfterRequest_LiveName_ThenResult()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            yield return K.ClickTool(AdmissionTool.Record);
            yield return K.Settle();
            string id = K.CandidateId;
            var cem = Sim.AdmissionWatcher(id);
            Assert.AreEqual("Cem", cem?.Name, "talepte gözetmen Cem");
            StringAssert.Contains("Cem", CarbonSupervisorText(), "karbonda Cem");
            Ok(K.Game.ExecuteBoardCommand(s => s.AssignResident(cem.Id, "craft")), "Cem ÜRETİM'e (takas)");
            yield return BoardPlayKit.Frames(2);
            var now = Sim.AdmissionWatcher(id);
            Assert.AreEqual("Suna", now?.Name, "takasla BOŞTA'ya dönen Suna sırada önde: yeni gözetmen (W0 C5)");
            StringAssert.Contains(now.Name, RequestSupervisorText(K.Last), "talep formu canlı adla");
            StringAssert.Contains(now.Name, CarbonSupervisorText(), "karbon canlı adla");
            StringAssert.Contains(now.Name, SlipOf(AdmissionTool.Record)?.Body, "BEKLENİYOR gövdesi canlı adla");
            Assert.IsFalse(K.Last.Request.SupervisorMissing, "gözetmen var");

            yield return RealClose();
            Assert.AreEqual(id, K.CandidateId, "aday bekler");
            Assert.IsTrue(Sim.Admission.Candidate.Inspections.Contains("record_done"), "kapanışta gözetmen Suna ile sonuç geldi");
        }

        /// <summary>
        /// P19b (D2-A, Risk 3): talepten sonra Boşta tamamen boşaltılır (emekli görevlere; <c>RetireJobs</c> geri almasın diye pano sunucusu
        /// kapalı) → talep formunda ve BEKLENİYOR gövdesinde kırmızı "gözetmen yok · sonuç gelmez"; kapanışta sonuç gelmez, aday otomatik ret.
        /// </summary>
        [UnityTest]
        public IEnumerator Supervisor_IdleEmptied_RedLine_NoResult()
        {
            yield return K.AtFirstCandidate();
            yield return K.ToCounter();
            yield return K.TakeDocument();
            yield return K.ClickTool(AdmissionTool.Record);
            yield return K.Settle();
            string id = K.CandidateId;
            BoardPlayKit.Presenter.enabled = false;
            // AssignResident bir takastır: dolu göreve atanan, oradakini kendi eski görevine (BOŞTA) yollar. Bu yüzden her gözetmen adayı
            // yalnız BOŞ bir emekli göreve alınır ve döngü Core'un kendi kuralı (AdmissionWatcher) boşalana dek sürer. "contact" en sonda:
            // kapanışta admissionContactAdvance gelişi öne çeker (tohum 79'da 3 kişi için gerekmez: watch, care, gather).
            var jobs = new[] { "watch", "care", "gather", "contact" };
            for (int guard = 0; guard < 12; guard++)
            {
                var w = Sim.AdmissionWatcher(id);
                if (w == null) break;
                string job = jobs.FirstOrDefault(j => !Sim.Board.People.Any(o => o.Alive && o.Job == j) && Sim.AssignResidentCheck(w.Id, j) == null);
                Assert.IsNotNull(job, w.Name + " için boş emekli görev yok; Boşta boşaltılamadı");
                Ok(K.Game.ExecuteBoardCommand(s => s.AssignResident(w.Id, job)), w.Name + " → " + job);
            }
            Assert.IsNull(Sim.AdmissionWatcher(id), "Boşta boş");
            K.Admission.Refresh();
            Assert.IsTrue(K.Last.Request.SupervisorMissing, "gözetmen yok");
            Assert.AreEqual(Loc.T("gözetmen yok · sonuç gelmez"), RequestSupervisorText(K.Last), "talep formunda kırmızı satır");
            StringAssert.Contains(Loc.T("gözetmen yok · sonuç gelmez"), SlipOf(AdmissionTool.Record)?.Body, "BEKLENİYOR gövdesi");

            yield return RealClose();
            Assert.AreNotEqual(id, K.CandidateId, "sonuç gelmedi: aday otomatik ret");
            Assert.IsTrue(Sim.Admission.Events.Any(e => e.PersonId == id && e.Kind == "default_reject"), "otomatik ret olayı");
        }
    }
}
