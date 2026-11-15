using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using K = Corrigible.Desk.Tests.AdmissionPlayKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Aday grubunun sunumu (KOS aşama 2; tasarım <c>Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md</c> §7) gerçek DeskGame'de, Play'de. Tohum 79,
    /// varsayılan denge: grup V.005'te gelir (2 aday), bekleme 2 vardiya. Düzen <see cref="AdmissionPlayKit"/> ile aynı: kayıtlar test
    /// klasöründe, tıklar gerçek ışınla ve Input System 1.19 kuralıyla, damga <see cref="PointerGesture"/> sürüklemesiyle raftan kâğıda
    /// (komutu <c>SiginakAdmission.PressStamp</c> yazar).
    /// <list type="bullet">
    /// <item><b>Canlı grup</b> (<see cref="LiveGroup"/>): yeni harekât, grup gelene dek kapanış komutla (<c>GameController.PrintBoardShift</c> +
    /// <c>FileBoardShift</c>; yalnız kapanış sunumu atlanır). Vuruş ve teleks satırı canlı <c>Changed</c>'den gelir.</item>
    /// <item><b>Dolu sığınak</b> (<see cref="FullShelterGroup"/>): kayıt fikstürü (<c>boardResidentCapacity = 8</c>) + KAYITTAN DEVAM; denge
    /// çalışırken değiştirilmez.</item>
    /// <item><b>Gerçek ışın</b>: <see cref="TryHit"/> çarpıştırıcının ekranda ilk çarpılan noktasını arar. Araya yalnız kullanıcının kapı
    /// modeli (<c>SM_Door</c>) giriyorsa test kırılmaz: günlüğe yazılır (<c>[CandidatePlayTests]</c>) ve sentetik yola düşülür; başka bir
    /// örtü testi düşürür.</item>
    /// </list>
    /// </summary>
    public sealed class CandidatePlayTests
    {
        const string DoorBlocker = "SM_Door";
        const string TurkishOnly = "çğıöşüÇĞİÖŞÜ";

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
            yield return ShiftPlayKit.End();
        }

        // ================================================================== ortak

        static Simulation Sim => K.Sim;
        static AdmissionStation St => K.Station;
        static SiginakCameraRig Rig => K.Rig;
        static CandidateGroupView View => St != null ? St.Candidates : null;
        static CommunityAdmissionState Adm => Sim.Admission;
        static int Alive => Sim.Board.People.Count(p => p.Alive);

        void Logs()
        {
            poses?.Dispose();
            sounds?.Dispose();
            poses = new PoseLog(Rig);
            sounds = new AdmissionSoundLog(K.Admission);
        }

        void ListenTelex()
        {
            if (onTelex != null && K.Admission != null) K.Admission.TelexLine -= onTelex;
            onTelex = (t, b) => telex.Add((t, b));
            K.Admission.TelexLine += onTelex;
        }

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        static void Note(string line) => Debug.Log("[CandidatePlayTests] " + TestContext.CurrentContext.Test.Name + " · " + line);

        /// <summary>
        /// Açık sığınak olayı varsa güvenli seçenekle karara bağlar (gerçek komut, <c>event_decide</c>). Aşama 8 dengesiyle baskınsız
        /// harekâtta erzak olayı ilk gruplar sırasında açılabilir (tüketim 0,575 denemesinde V.005'te, 0,40'ta V.008'de) ve fişi
        /// kilitler; bu testler kapıyı sınar, olay tutanağını değil.
        /// </summary>
        static IEnumerator SettleEvent()
        {
            var e = Sim.PendingShelterEvent;
            if (e == null) yield break;
            Ok(K.Game.ExecuteBoardCommand(s => s.DecideShelterEvent(ShelterEventData.SafeOption(e.Kind))), "olay " + e.Kind + " (gün " + Sim.S.Day + ")");
            yield return BoardPlayKit.Frames(3);
        }

        /// <summary>Kapanış komutla (sunum atlanır): açık olay güvenli seçenekle kapanır, sonra tam bir <c>board_print</c> + <c>board_file</c>, gün bir ilerler.</summary>
        static IEnumerator QuickClose()
        {
            yield return SettleEvent();
            int day = Sim.S.Day;
            Ok(K.Game.PrintBoardShift(), "board_print (gün " + day + ")");
            Ok(K.Game.FileBoardShift(Sim.Board.ReceiptId), "board_file (gün " + day + ")");
            Assert.AreEqual(day + 1, Sim.S.Day, "kapanış günü bir ilerletir");
            yield return BoardPlayKit.Frames(3);
        }

        /// <summary>Yeni harekât (tohum 79) ve kapıya grup gelene dek komutla kapanış. Kamera K1'de.</summary>
        static IEnumerator LiveGroup(Action beforeCloses = null)
        {
            yield return K.Begin();
            Assert.IsNotNull(View, "AdmissionStation.candidates (Aday grubu) sahnede bağlı");
            beforeCloses?.Invoke();
            for (int guard = 0; Adm.Group.Count == 0 && guard < 8; guard++) yield return QuickClose();
            Assert.Greater(Adm.Group.Count, 0, "tohum 79: sekiz kapanışta grup gelmedi (gün " + Sim.S.Day + ")");
            yield return SettleEvent();
            yield return K.Settle();
        }

        /// <summary>Dolu sığınak: kapasite kurucu sayısına eşit bir kayıt fikstürü, grup gelene dek kapanış, sonra KAYITTAN DEVAM.</summary>
        static IEnumerator FullShelterGroup()
        {
            yield return K.Begin();
            var sim = K.FixtureGame(BoardPlayKit.DefaultSeed, b => b.boardResidentCapacity = b.boardStartResidents);
            for (int guard = 0; sim.Admission.Group.Count == 0 && guard < 8; guard++) K.FixtureClose(sim);
            Assert.Greater(sim.Admission.Group.Count, 0, "fikstürde grup gelmedi (gün " + sim.S.Day + ")");
            Assert.GreaterOrEqual(sim.Board.People.Count(p => p.Alive), sim.B.boardResidentCapacity, "fikstürde sığınak dolu");
            yield return K.ContinueFrom(sim);
            yield return K.Settle();
        }

        /// <summary>
        /// Çarpıştırıcının ekranda ışının ilk çarptığı bir noktası (<see cref="AdmissionPlayKit.HitOn"/> ile aynı ızgara; düşürmez).
        /// Bulunamazsa <paramref name="blocker"/> son denenen noktanın sonuç listesi.
        /// </summary>
        static bool TryHit(Collider c, out RaycastResult hit, out string blocker)
        {
            hit = default;
            blocker = null;
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) { blocker = "çarpıştırıcı kapalı"; return false; }
            var cam = BoardPlayKit.View;
            var fractions = new[] { 0f, -0.2f, 0.2f, -0.4f, 0.4f };
            foreach (float fy in fractions) foreach (float fx in fractions) foreach (float fz in fractions)
            {
                Vector3 world;
                if (c is BoxCollider box) world = box.transform.TransformPoint(box.center + Vector3.Scale(box.size, new Vector3(fx, fy, fz)));
                else world = c.bounds.center + Vector3.Scale(c.bounds.size, new Vector3(fx, fy, fz));
                var sp = cam.WorldToScreenPoint(world);
                if (sp.z <= cam.nearClipPlane || sp.x < 2 || sp.y < 2 || sp.x > cam.pixelWidth - 2 || sp.y > cam.pixelHeight - 2) continue;
                var screen = new Vector2(sp.x, sp.y);
                var first = BoardPlayKit.FirstHit(screen);
                if (first.gameObject != null && (first.gameObject == c.gameObject || first.gameObject.transform.IsChildOf(c.transform))) { hit = first; return true; }
                blocker = BoardPlayKit.DescribeHits(screen);
            }
            return false;
        }

        /// <summary>
        /// Çarpıştırıcıya gerçek ışınla tık. Araya yalnız kullanıcının kapı modeli giriyorsa günlüğe yazar ve <paramref name="fallback"/>
        /// ile sürer (false döner); başka bir örtü testi düşürür.
        /// </summary>
        static bool RealClick(Collider c, string what, Action fallback)
        {
            if (TryHit(c, out var hit, out string blocker))
            {
                K.ClickHit(hit);
                Note(what + ": gerçek ışın vardı (" + hit.gameObject.name + ", " + hit.distance.ToString("0.000", CultureInfo.InvariantCulture) + " m)");
                return true;
            }
            if (blocker != null && blocker.Contains(DoorBlocker))
            {
                Note(what + ": ENGEL " + DoorBlocker + " (" + blocker + "); sentetik yola düşüldü");
                fallback?.Invoke();
                return false;
            }
            Assert.Fail(what + ": gerçek ışın hedefe varmadı (" + blocker + " · " + K.Describe() + ")");
            return false;
        }

        /// <summary>K1 → K8 (KAPI tuşu) → K8A (inik tezgâha gerçek ışın). Kartlar ve fiş tezgâhta, etkileşimli.</summary>
        static IEnumerator ToCounter()
        {
            if (Rig.Current != SiginakPose.Door) yield return K.EnterDoor();
            Assert.AreEqual(CounterState.Down, St.Counter, "grup kapıda: tezgâh inik (" + K.Describe() + ")");
            RealClick(K.CounterHit, "K8 · inik tezgâh", () => K.Admission.ClickCounter());
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Admission, 3);
            yield return K.Settle();
            Assert.IsTrue(View.Shown, "K8A'da aday kâğıtları tezgâhta");
            Assert.IsTrue(View.Interactive, "K8A'da aday kâğıtları etkileşimli");
        }

        static Collider CardHit(int i) => View.CardPapers[i].GetComponentInChildren<Collider>(true);
        static Collider SlipHit => View.SlipPaper.GetComponentInChildren<Collider>(true);

        static Vector3 CounterUp => St.Top.transform.up;

        /// <summary>Elde tutulan KABUL damgasının <paramref name="card"/>. kartın ortasına bırakılacağı ekran noktası.</summary>
        static Vector2 CardDropScreen(StampDecision stamp, int card)
        {
            var screen = BoardPlayKit.ScreenPoint(View.CardPapers[card].position + CounterUp * stamp.HeldHeight);
            Assert.IsTrue(stamp.DropPoint(screen, out var drop), "KABUL: bırakış noktası çözülmedi");
            Assert.IsTrue(View.AcceptStamp(StampChoice.Admit, drop), "KABUL: hedef " + card + ". kartın üstünde değil");
            Assert.AreEqual(View.Data.Cards[card].Id, View.StampCandidateId, "KABUL: hedef kart");
            return screen;
        }

        /// <summary>Elde tutulan GERİ ÇEVİR damgasının grup fişinin ortasına bırakılacağı ekran noktası.</summary>
        static Vector2 SlipDropScreen(StampDecision stamp)
        {
            var screen = BoardPlayKit.ScreenPoint(View.SlipPaper.position + CounterUp * stamp.HeldHeight);
            Assert.IsTrue(stamp.DropPoint(screen, out var drop), "GERİ ÇEVİR: bırakış noktası çözülmedi");
            Assert.IsTrue(View.AcceptStamp(StampChoice.Reject, drop), "GERİ ÇEVİR: hedef grup fişinin üstünde değil");
            return screen;
        }

        /// <summary>Grup fişinde mm noktasının (sol üst köken, y aşağı) dünya yeri.</summary>
        static Vector3 SlipWorld(Vector2 mm)
        {
            var p = View.SlipPaper;
            return p.position + p.right * ((mm.x - CandidateGroupSlipFace.WidthMm / 2f) / 1000f) + p.up * ((CandidateGroupSlipFace.HeightMm / 2f - mm.y) / 1000f);
        }

        /// <summary>YER YOK listesinin <paramref name="row"/>. satırına gerçek ışınla tık (satırın ortası).</summary>
        static IEnumerator ClickRow(int row)
        {
            var face = View.SlipFace;
            Assert.Less(row, face.RowCount, "satır fişte çizili");
            var world = SlipWorld(face.RowRect(row).center);
            Assert.AreEqual(row, face.RowAt(View.SlipMm(world)), "satırın ortası fişin o satırına düşer");
            var screen = BoardPlayKit.ScreenPoint(world);
            var hit = BoardPlayKit.FirstHit(screen);
            string id = View.Data.Replace[row].Id;
            if (hit.gameObject == SlipHit.gameObject)
            {
                K.ClickHit(hit);
                Note("K8A · fiş satırı " + row + ": gerçek ışın vardı");
            }
            else
            {
                string hits = BoardPlayKit.DescribeHits(screen);
                Assert.IsTrue(hits.Contains(DoorBlocker), "fiş satırı " + row + ": gerçek ışın fişe varmadı (" + hits + ")");
                Note("K8A · fiş satırı " + row + ": ENGEL " + DoorBlocker + " (" + hits + "); sentetik yola düşüldü");
                K.Admission.ClickReplace(id);
            }
            yield return BoardPlayKit.Frames(2);
        }

        /// <summary>Karar dizisi bitene dek her karede <paramref name="onFrame"/>.</summary>
        static IEnumerator WaitSequence(Action onFrame = null)
        {
            float until = Time.unscaledTime + 10;
            while (K.Admission.Busy || K.Door.Moving || St.Moving)
            {
                if (Time.unscaledTime > until) Assert.Fail("karar dizisi bitmedi (" + K.Describe() + ")");
                yield return null;
                BoardPlayKit.TrackFrame();
                onFrame?.Invoke();
            }
            yield return K.Settle();
        }

        static IEnumerator ToDesk()
        {
            if (Rig.Current != SiginakPose.Desk)
            {
                Rig.ResetView();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk, 3);
            }
            yield return K.Settle();
        }

        /// <summary>Gerçek kapanış K1'den: şalter (kapak, kol) → fiş dile → EVRAK'a sürükle → dosyalama biter.</summary>
        static IEnumerator RealClose(Action<HandoverSlipFacts> printed = null)
        {
            yield return SettleEvent();
            yield return ToDesk();
            var sw = ShiftPlayKit.Switch;
            yield return BoardPlayKit.WaitFor(() => sw.Phase == ShiftPhase.Ready && !sw.Moving, 3, "şalter hazır değil (" + sw.Phase + " / " + sw.LockPlate + ")");
            yield return ShiftPlayKit.PrintToTongue();
            printed?.Invoke(ShiftPlayKit.Paper.Facts);
            yield return ShiftPlayKit.FileOnEvrak();
            yield return ToDesk();
        }

        static string CardsFingerprint(CandidateGroupData g) => g == null ? "(grup yok)" : g.Key + " | " + string.Join(" | ", g.Cards.Select(c =>
            c.Id + ":" + c.Name + ":" + c.Mastery + ":" + c.Stats + ":" + c.Tier + ":" + c.TierName + ":" + c.Suspicion + ":" +
            string.Join(",", c.Perks.Select(p => p.Id + "=" + p.Name + "/" + p.Effect))));

        static List<SavedCommand> Since((int commands, uint rng) mark) => ShiftPlayKit.Since(mark.commands);

        // ================================================================== (a) grup gelince tezgâh

        /// <summary>
        /// (a) Grup canlı kapanışta gelir: kapı bir kez vurur, teleks "KAPI · n aday kapıda. Bekleme: 2 vardiya." yazar, tezgâh iner. K8A'da
        /// 2–3 kart ve grup fişi etkin; emekli araçlar (büyüteç, talep defteri, kalem, KISITLI ve YARDIM damgaları) ve beş etiket (GİDEN ·
        /// TELSİZ, TALİMAT, ESKİ, BULGU, BULGULAR) aşama 7'de sahneden söküldü. Gezinme komut yazmaz.
        /// </summary>
        [UnityTest]
        public IEnumerator Group_Arrives_CardsAndSlipOnCounter_RetiredGone_Telex()
        {
            yield return LiveGroup(() => { Logs(); ListenTelex(); });
            int n = Adm.Group.Count;
            Assert.That(n, Is.InRange(2, 3), "grup 2–3 aday");
            Assert.AreEqual(1, sounds.Count(AdmissionSound.Knock), "yeni grup bir kez vurur (" + sounds + ")");
            Assert.IsTrue(K.Door.Knocking, "grup bekliyor: vuruş lambası");
            int wait = Adm.GroupWaitingUntil - Sim.S.Day;
            Assert.AreEqual(Sim.B.candidateWaitShifts, wait, "bekleme dengeden");
            yield return BoardPlayKit.WaitFor(() => telex.Count > 0, 3, "teleks satırı düşmedi");
            string body = Loc.F("{0} aday kapıda. Bekleme: {1} vardiya.", n, wait);
            Assert.IsTrue(telex.Any(t => t.title == Loc.T("KAPI") && t.body == body), "teleks: KAPI · " + body + " (gelen: " + string.Join(" / ", telex.Select(t => t.title + " · " + t.body)) + ")");
            Assert.IsTrue(ShiftPlayKit.Telex.Lines.Any(l => l.message != null && l.message.Contains(body)), "satır masadaki telekste");
            Assert.AreEqual(CounterState.Down, St.Counter, "tezgâh indi");

            var mark = ShiftPlayKit.Mark();
            yield return ToCounter();
            Assert.AreEqual(n, View.CardCount, "kart sayısı = aday sayısı");
            for (int i = 0; i < View.CardPapers.Count; i++)
            {
                bool want = i < n;
                Assert.AreEqual(want, View.CardPapers[i].gameObject.activeInHierarchy, "aday kartı " + (i + 1) + (want ? " görünür" : " gizli"));
                if (!want) continue;
                var face = View.CardFace(i);
                var c = Adm.Group[i];
                Assert.AreEqual(c.Resident.Id, face.Data.Id, "kart " + (i + 1) + " sıradaki adayın");
                Assert.AreEqual(DeskText.Upper(c.Resident.Name), face.NameText.text, "kartta ad");
                Assert.IsFalse(face.Overflowing, "kart " + (i + 1) + " taşmıyor");
                Assert.IsFalse(face.ImpressionVisible, "kartta iz yok");
                Assert.IsTrue(CardHit(i).enabled, "kart " + (i + 1) + " tıklanır");
            }
            Assert.IsTrue(View.SlipPaper.gameObject.activeInHierarchy, "grup fişi görünür");
            Assert.AreEqual(Loc.F("{0} ADAY", n), View.SlipFace.Title.text, "fiş başlığı");
            Assert.AreEqual(Loc.F("bekleme: {0} vardiya", wait), View.SlipFace.Body.text, "fişte bekleme");
            Assert.IsFalse(View.SlipFace.Overflowing, "fiş taşmıyor");
            Assert.IsTrue(SlipHit.enabled, "fiş tıklanır");

            // Aşama 7: emekli araçlar ve etiketler sahneden söküldü; istasyonda yalnız iki damga, kâğıt yalnız aday görünümünde
            Assert.AreEqual(2, St.Stamps.Count, "istasyonda iki damga");
            foreach (string gone in new[] { "Magnifier", "RequestPad", "Pencil", "Damga KISITLI", "Damga YARDIM", "TXT_Out", "TXT_Holder", "TXT_Archive", "TXT_Slip", "TXT_Findings", "Belge", "Şerit" })
                Assert.IsFalse(St.GetComponentsInChildren<Transform>(true).Any(x => x.name == gone), "emekli nesne sahnede: " + gone);
            foreach (var c in new[] { StampChoice.Admit, StampChoice.Reject })
            {
                Assert.IsTrue(K.Stamp(c).gameObject.activeInHierarchy, c + " damgası rafta");
                Assert.IsTrue(K.Stamp(c).Enabled, c + " damgası açık (" + K.Last.Stamps[(int)c].Reason + ")");
            }
            Assert.AreEqual(1, sounds.Count(AdmissionSound.Knock), "tezgâha gitmek yeniden vurdurmaz");
            ShiftPlayKit.AssertUnchanged(mark, "K8 → K8A");
        }

        // ================================================================== (b) KABUL bir karta

        /// <summary>
        /// (b) KABUL damgası raftan ikinci karta sürüklenir: tam bir <c>candidate_choose</c> (a = aday, b boş), iz kartta, kapı açılır, kişi
        /// panoda Boşta'da; öteki aday "notchosen" olarak gider, tezgâh katlanır.
        /// </summary>
        [UnityTest]
        public IEnumerator Admit_StampOnCard_CandidateChoose_PersonIdleOnBoard()
        {
            yield return LiveGroup();
            yield return ToCounter();
            int n = Adm.Group.Count, pick = n - 1, before = Alive, day = Sim.S.Day;
            string id = Adm.Group[pick].Resident.Id, name = Adm.Group[pick].Resident.Name;
            var face = View.CardFace(pick);
            Logs();
            var mark = ShiftPlayKit.Mark();
            yield return K.DragStamp(StampChoice.Admit, s => CardDropScreen(s, pick));
            yield return BoardPlayKit.WaitFor(() => sounds.Count(AdmissionSound.StampPress) > 0, 2, "basış olmadı (" + sounds + ")");
            bool sawMark = false, sawOpen = false, sawOther = false;
            yield return WaitSequence(() =>
            {
                sawMark |= face.ImpressionVisible;
                sawOpen |= K.Door.LeafDegrees > 1f;
                for (int i = 0; i < n; i++) if (i != pick) sawOther |= View.CardFace(i).ImpressionVisible;
            });

            var got = Since(mark);
            Assert.AreEqual(1, got.Count, "tek komut: " + ShiftPlayKit.Describe(got));
            Assert.AreEqual("candidate_choose", got[0].kind, "son komut");
            Assert.AreEqual(id, got[0].a, "seçilen aday");
            Assert.IsTrue(string.IsNullOrEmpty(got[0].b), "yer varken kimse yollanmaz (b boş): " + got[0].b);
            Assert.AreEqual(day, got[0].day, "komutun günü");
            Assert.AreEqual("candidate_choose", Sim.Record.commands[Sim.Record.commands.Count - 1].kind, "kaydın son komutu");
            Assert.IsTrue(sawMark, "KABUL izi seçilen kartta göründü");
            Assert.IsFalse(sawOther, "iz yalnız seçilen kartta");
            Assert.IsTrue(sawOpen, "KABUL'de kapı açılır");

            Assert.AreEqual(0, Adm.Group.Count, "kapı boş");
            Assert.AreEqual(before + 1, Alive, "sığınağa bir kişi eklendi");
            var person = Sim.Board.People.Last();
            Assert.AreEqual(id, person.Id, "kişi panoya eklendi");
            Assert.AreEqual("rest", person.Job, "Boşta'da");
            Assert.AreEqual(n - 1, Adm.Passed.Count(p => p.Kind == "notchosen" && p.Shift == day), "öteki adaylar seçilmedi olarak gitti");
            Assert.AreEqual(SiginakPose.Door, Rig.Current, "kabulde kamera K8'e döner");
            Assert.AreEqual(CounterState.Folded, St.Counter, "tezgâh kapandı");
            Assert.IsFalse(View.Shown, "aday kâğıtları kalktı");
            Assert.IsFalse(K.Door.Knocking, "vuruş lambası sönük");
            Assert.IsFalse(K.Door.IsOpen, "kapı kapandı");

            yield return BoardPlayKit.EnterBoard();
            yield return BoardPlayKit.Frames(2);
            var board = BoardPlayKit.Board.Last;
            CollectionAssert.Contains(board.DeckOrder, id, name + " Boşta destesinde (" + string.Join(", ", board.DeckOrder) + ")");
            Assert.IsNotNull(BoardPlayKit.Board.CardFor(id), "kartı panoda");
        }

        // ================================================================== (c) GERİ ÇEVİR fişe

        /// <summary>
        /// (c) GERİ ÇEVİR damgası grup fişine sürüklenir: tam bir <c>candidate_pass</c>, iz fişte, kapı açılmaz, kimse eklenmez; bütün adaylar
        /// "pass" olarak gider; tezgâh aileden çıkınca katlıdır.
        /// </summary>
        [UnityTest]
        public IEnumerator Pass_StampOnSlip_CandidatePass_NobodyAdded()
        {
            yield return LiveGroup();
            yield return ToCounter();
            int n = Adm.Group.Count, before = Alive, day = Sim.S.Day;
            var ids = Sim.Board.People.Select(p => p.Id).ToList();
            Logs();
            var mark = ShiftPlayKit.Mark();
            yield return K.DragStamp(StampChoice.Reject, SlipDropScreen);
            yield return BoardPlayKit.WaitFor(() => sounds.Count(AdmissionSound.StampPress) > 0, 2, "basış olmadı (" + sounds + ")");
            bool sawMark = false, sawOpen = false, sawCardMark = false;
            yield return WaitSequence(() =>
            {
                sawMark |= View.SlipFace.StampVisible;
                sawOpen |= K.Door.LeafDegrees > 0f;
                for (int i = 0; i < n; i++) sawCardMark |= View.CardFace(i).ImpressionVisible;
            });

            var got = Since(mark);
            Assert.AreEqual(1, got.Count, "tek komut: " + ShiftPlayKit.Describe(got));
            Assert.AreEqual("candidate_pass", got[0].kind, "son komut");
            Assert.AreEqual(day, got[0].day, "komutun günü");
            Assert.IsTrue(sawMark, "GERİ ÇEVRİLDİ izi fişte göründü");
            Assert.IsFalse(sawCardMark, "kartlarda iz yok");
            Assert.IsFalse(sawOpen, "geri çevirmede kapı açılmaz");
            Assert.AreEqual(0, sounds.Count(AdmissionSound.Open), "kapı sesi yok");

            Assert.AreEqual(0, Adm.Group.Count, "kapı boş");
            Assert.AreEqual(before, Alive, "kimse eklenmedi");
            CollectionAssert.AreEqual(ids, Sim.Board.People.Select(p => p.Id).ToList(), "pano aynı kişiler");
            Assert.AreEqual(n, Adm.Passed.Count(p => p.Kind == "pass" && p.Shift == day), "bütün adaylar geri çevrildi");
            Assert.IsFalse(View.Shown, "aday kâğıtları kalktı");
            Assert.IsFalse(K.Door.Knocking, "vuruş lambası sönük");
            Note("geri çevirmeden sonra " + K.Describe());
            yield return ToDesk();
            Assert.AreEqual(CounterState.Folded, St.Counter, "tezgâh katlı");
            Assert.AreEqual(1, Since(mark).Count, "masaya dönüş komut yazmaz");
        }

        // ================================================================== (d) dolu sığınak

        /// <summary>
        /// (d) Dolu sığınak (8/8): fiş "YER YOK 8/8" ve kişi listesini gösterir; KABUL işaretsizken kapalıdır (pirinç dil, sürükleme ve
        /// doğrudan basış komut yazmaz). Bir satıra gerçek ışınla tık kişiyi işaretler (komutsuz) ve KABUL açılır; basış tek komuttur
        /// (<c>candidate_choose</c>, b = işaretli kişi): işaretli kişi gider, aday gelir, yolluk erzaktan düşer.
        /// </summary>
        [UnityTest]
        public IEnumerator FullShelter_AdmitNeedsMark_OneCommand_SwapAndRations()
        {
            yield return FullShelterGroup();
            yield return ToCounter();
            var g = K.Last.Group;
            int alive = Alive, capacity = Sim.B.boardResidentCapacity, day = Sim.S.Day;
            Assert.IsTrue(g.Full, "sığınak dolu");
            Assert.AreEqual(Loc.F("YER YOK · {0}/{1}", alive, capacity), View.SlipFace.RoomTitle.text, "fişte yer durumu");
            Assert.AreEqual(alive, g.Replace.Count, "listede bütün canlılar");
            Assert.AreEqual(alive, View.SlipFace.RowCount, "satırlar fişte");
            Assert.IsFalse(View.SlipFace.Overflowing, "dolu fiş taşmıyor");
            Assert.IsNull(g.ReplaceId, "başta kimse işaretli değil");

            var admit = K.Stamp(StampChoice.Admit);
            Assert.IsFalse(admit.Enabled, "KABUL işaretsizken kapalı");
            Assert.IsTrue(admit.LockTongue != null && admit.LockTongue.activeInHierarchy, "KABUL'ün pirinç dili");
            Assert.AreEqual(Loc.F("YER YOK {0}/{1}", alive, capacity), K.Last.Stamps[(int)StampChoice.Admit].ShortReason, "kısa neden");
            Assert.IsTrue(K.Stamp(StampChoice.Reject).Enabled, "GERİ ÇEVİR açık");
            Logs();
            var mark = ShiftPlayKit.Mark();
            string cand = Adm.Group[0].Resident.Id;
            // kilitli damga kalkmaz: raftan sürükleme ret sesi verir, komut yazmaz
            var rack = K.HitOn(admit.Hit, "KABUL damgası");
            var gesture = BoardPlayKit.Press(rack);
            yield return gesture.Move(rack.screenPosition + new Vector2(40, 40), 4);
            gesture.Release();
            yield return K.Settle();
            Assert.IsFalse(admit.Held, "kilitli damga elde değil");
            Assert.Greater(sounds.Count(AdmissionSound.Deny), 0, "kilitli damga ret sesi verir");
            Assert.IsFalse(K.Admission.PressStamp(StampChoice.Admit, cand), "işaretsiz KABUL reddedilir");
            ShiftPlayKit.AssertUnchanged(mark, "dolu sığınakta işaretsiz KABUL");

            // satıra tık: işaret (komutsuz); ikinci tık kaldırır; üçüncü yeniden koyar
            int row = g.Replace.FindIndex(r => r.Enabled);
            Assert.GreaterOrEqual(row, 0, "yollanabilir bir satır var");
            string leaver = g.Replace[row].Id, leaverName = g.Replace[row].Name;
            yield return ClickRow(row);
            Assert.AreEqual(leaver, K.Admission.Ui.ReplaceId, "satır tıkı kişiyi işaretler");
            Assert.AreEqual(leaver, K.Last.Group.ReplaceId, "görüntüde işaretli kişi");
            Assert.IsTrue(View.SlipFace.RowMark(row).gameObject.activeInHierarchy, "satırın işareti görünür");
            StringAssert.Contains(leaverName, View.SlipFace.ReplaceLine.text, "yollanacak satırı kişiyi yazar");
            yield return ClickRow(row);
            Assert.IsNull(K.Admission.Ui.ReplaceId, "ikinci tık işareti kaldırır");
            Assert.IsFalse(admit.Enabled, "işaret kalkınca KABUL yine kapalı");
            yield return ClickRow(row);
            Assert.AreEqual(leaver, K.Admission.Ui.ReplaceId, "üçüncü tık yeniden işaretler");
            yield return K.Settle();
            Assert.IsTrue(admit.Enabled, "işaretliyken KABUL açık (" + K.Last.Stamps[(int)StampChoice.Admit].Reason + ")");
            ShiftPlayKit.AssertUnchanged(mark, "yollanacak kişiyi işaretlemek");

            double supplies = Sim.Shelter.Supplies, rations = Sim.DismissSuppliesPreview;
            Assert.Greater(rations, 0, "yolluk var");
            yield return K.DragStamp(StampChoice.Admit, s => CardDropScreen(s, 0));
            yield return BoardPlayKit.WaitFor(() => sounds.Count(AdmissionSound.StampPress) > 0, 2, "basış olmadı (" + sounds + ")");
            yield return WaitSequence();

            var got = Since(mark);
            Assert.AreEqual(1, got.Count, "tek komut: " + ShiftPlayKit.Describe(got));
            Assert.AreEqual("candidate_choose", got[0].kind);
            Assert.AreEqual(cand, got[0].a, "seçilen aday");
            Assert.AreEqual(leaver, got[0].b, "yollanan kişi aynı komutta");
            Assert.AreEqual(day, got[0].day);
            Assert.IsFalse(Sim.Board.People.Any(p => p.Id == leaver && p.Alive), leaverName + " panodan gitti");
            Assert.IsTrue(Sim.Board.People.Any(p => p.Id == cand && p.Alive && p.Job == "rest"), "aday Boşta'da");
            Assert.AreEqual(alive, Alive, "canlı sayısı aynı (bir gitti, bir geldi)");
            Assert.AreEqual(supplies - rations, Sim.Shelter.Supplies, 1e-9, "yolluk erzaktan düştü");
            Assert.IsTrue(Adm.File(leaver).Departed, "yollananın dosyası ayrıldı");
            Assert.AreEqual(0, Adm.Group.Count, "kapı boş");
            Assert.IsNull(K.Admission.Ui.ReplaceId, "işaret silindi");
        }

        // ================================================================== (e) iki vardiya kararsız

        /// <summary>
        /// (e) Karar verilmeden iki kapanış: ilkinden sonra grup fişi "SON VARDİYA" der ve basılan devir fişi KAPI uyarısını taşır
        /// (<see cref="SlipWarningKind.DoorLastShift"/>); ikincisi (gerçek arayüzle: şalter → fiş → EVRAK) grubu yollar: "expired", teleks
        /// "Adaylar karar beklemeden gitti.", tezgâh katlı, lamba sönük. Kabul komutu yazılmaz.
        /// </summary>
        [UnityTest]
        public IEnumerator Undecided_TwoShifts_SlipWarning_ThenGroupLeaves_CounterFolds()
        {
            yield return LiveGroup(() => { Logs(); ListenTelex(); });
            int n = Adm.Group.Count, arrived = Sim.S.Day;
            string key = AdmissionSnapshotBuilder.GroupKey(Sim);
            Assert.IsFalse(HandoverSlipFactsBuilder.Build(Sim).warnings.Any(w => w.kind == SlipWarningKind.DoorLastShift), "ilk vardiyada devir fişinde KAPI uyarısı yok");
            Assert.IsFalse(K.Last.Group.LastShift, "ilk vardiya son değil");

            yield return QuickClose();
            yield return K.Settle();
            Assert.AreEqual(key, AdmissionSnapshotBuilder.GroupKey(Sim), "grup bir kapanış bekledi");
            Assert.AreEqual(1, sounds.Count(AdmissionSound.Knock), "bekleyen grup yeniden vurmaz");
            Assert.IsTrue(K.Last.Group.LastShift, "ikinci vardiya son");
            yield return ToCounter();
            Assert.AreEqual(Loc.T("SON VARDİYA · karar yoksa giderler"), View.SlipFace.Body.text, "grup fişinde son vardiya uyarısı");
            Assert.IsFalse(View.SlipFace.Overflowing, "uyarılı fiş taşmıyor");

            HandoverSlipFacts facts = null;
            telex.Clear();
            yield return RealClose(f => facts = f);
            Assert.IsNotNull(facts, "basılan devir fişinin verisi");
            Assert.IsTrue(facts.warnings.Any(w => w.kind == SlipWarningKind.DoorLastShift), "devir fişinde KAPI · son vardiya uyarısı");
            Assert.AreEqual(n, facts.openApplications, "devir fişinde bekleyen aday sayısı");

            Assert.AreEqual(arrived + 2, Sim.S.Day, "iki kapanış geçti");
            Assert.AreEqual(0, Adm.Group.Count, "grup gitti");
            Assert.AreEqual(n, Adm.Passed.Count(p => p.Kind == "expired" && p.Shift == Sim.S.Day), "adaylar süre aşımıyla gitti");
            yield return BoardPlayKit.WaitFor(() => telex.Count > 0, 4, "gidiş teleksi düşmedi");
            Assert.IsTrue(telex.Any(t => t.title == Loc.T("KAPI") && t.body == Loc.T("Adaylar karar beklemeden gitti.")),
                "teleks: " + string.Join(" / ", telex.Select(t => t.title + " · " + t.body)));
            yield return K.Settle();
            Assert.AreEqual(CounterState.Folded, St.Counter, "tezgâh katlandı");
            Assert.IsFalse(View.Shown, "aday kâğıtları kalktı");
            for (int i = 0; i < View.CardPapers.Count; i++) Assert.IsFalse(View.CardPapers[i].gameObject.activeInHierarchy, "kart " + (i + 1) + " gizli");
            Assert.IsFalse(View.SlipPaper.gameObject.activeInHierarchy, "grup fişi gizli");
            Assert.IsFalse(K.Door.Knocking, "vuruş lambası sönük");
            Assert.AreEqual(1, sounds.Count(AdmissionSound.Knock), "giden grup vurmaz");
            Assert.IsFalse(Sim.Record.commands.Any(c => c.kind == "candidate_choose" || c.kind == "candidate_pass"), "kabul komutu yazılmadı");
        }

        // ================================================================== (f) fiş basılıyken

        /// <summary>
        /// (f) Vardiya fişi basılıyken (gerçek şalter) kapı kapalıdır: görüntü kilitli ("Fiş basıldı; kapı bu vardiya kapalı."), iki damga da
        /// kapalı; basış ve (tezgâha varılabiliyorsa) raftan sürükleme komut yazmaz, grup yerinde kalır.
        /// </summary>
        [UnityTest]
        public IEnumerator Printed_StampsLocked_NoCommand()
        {
            yield return LiveGroup();
            yield return ToCounter();
            string cand = Adm.Group[0].Resident.Id;
            int n = Adm.Group.Count;
            yield return ToDesk();
            yield return ShiftPlayKit.PrintToTongue();
            yield return BoardPlayKit.Frames(2);
            Assert.IsTrue(Sim.Board.Printed, "fiş basılı");
            Logs();
            var mark = ShiftPlayKit.Mark();
            var s = K.Last;
            Assert.IsTrue(s.Locked, "fiş basılı: kabul kilitli");
            Assert.AreEqual(Loc.T("Fiş basıldı; kapı bu vardiya kapalı."), s.LockReason, "kilit nedeni Core'dan");
            foreach (var c in new[] { StampChoice.Admit, StampChoice.Reject })
            {
                Assert.IsFalse(s.Stamps[(int)c].Enabled, c + " kapalı");
                Assert.AreEqual(s.LockReason, s.Stamps[(int)c].Reason, c + " nedeni");
                Assert.IsFalse(K.Stamp(c).Enabled, c + " damgası kilitli");
            }
            Assert.IsFalse(K.Admission.PressStamp(StampChoice.Admit, cand), "KABUL komut yazmaz");
            Assert.IsFalse(K.Admission.PressStamp(StampChoice.Reject), "GERİ ÇEVİR komut yazmaz");
            Assert.AreEqual(2, sounds.Count(AdmissionSound.Deny), "iki ret sesi (" + sounds + ")");
            ShiftPlayKit.AssertUnchanged(mark, "fiş basılıyken damga");

            // Tezgâha varılabiliyorsa gerçek sürükleme de denenir
            K.Admission.Enter();
            yield return BoardPlayKit.WaitSeconds(1.2f);
            if (Rig.Current == SiginakPose.Door && !Rig.InTransition)
            {
                K.Admission.ClickCounter();
                yield return BoardPlayKit.WaitSeconds(1.2f);
            }
            if (Rig.Current == SiginakPose.Admission && !Rig.InTransition && K.Stamp(StampChoice.Admit).Hit.enabled)
            {
                yield return K.Settle();
                var admit = K.Stamp(StampChoice.Admit);
                var rack = K.HitOn(admit.Hit, "KABUL damgası (fiş basılı)");
                var gesture = BoardPlayKit.Press(rack);
                yield return gesture.Move(BoardPlayKit.ScreenPoint(View.CardPapers[0].position), 6);
                gesture.Release();
                yield return K.Settle();
                Assert.IsFalse(admit.Held, "kilitli damga kalkmaz");
                Note("fiş basılıyken K8A'ya varıldı; kilitli KABUL sürüklendi, komut yok");
            }
            else Note("fiş basılıyken tezgâha gidilemiyor (" + K.Describe() + "): sürükleme denenmedi, basış doğrudan sınandı");
            ShiftPlayKit.AssertUnchanged(mark, "fiş basılıyken sürükleme");
            Assert.AreEqual(n, Adm.Group.Count, "grup yerinde");
        }

        // ================================================================== (g) kayıttan devam

        /// <summary>
        /// (g) Grup beklerken kayıt ve KAYITTAN DEVAM: komutlar ve RNG birebir, grup aynı anahtarla ve aynı kartlarla (ad, ustalık, beden,
        /// kademe, şüphe, özellikler) geri kurulur; yükleme vurmaz, komut yazmaz; tezgâh inik, K8A'da kartlar ve fiş yerinde.
        /// </summary>
        [UnityTest]
        public IEnumerator SaveContinue_WaitingGroup_SameCards_NoKnock()
        {
            yield return LiveGroup();
            yield return ToCounter();
            string cards = CardsFingerprint(K.Last.Group);
            int n = Adm.Group.Count, wait = K.Last.Group.WaitLeft;
            string slipBody = View.SlipFace.Body.text;
            yield return ToDesk();
            var lines = ShiftPlayKit.CommandLines(Sim.Record);
            uint rng = Sim.S.Rng.State;
            var saved = SaveStore.Load();
            Assert.IsNotNull(saved, "kayıt yazıldı");
            Assert.AreEqual(GameRecord.CurrentVersion, saved.version, "kayıt biçimi");
            Assert.AreEqual(17, saved.version, "kayıt biçimi 17 (KOS aşama 5: sonlar; 16 aday grubu)");
            CollectionAssert.AreEqual(lines, ShiftPlayKit.CommandLines(saved), "kayıt dosyasının komutları canlıyla aynı");

            yield return ShiftPlayKit.Continue();
            K.RequireWired();
            yield return BoardPlayKit.Frames(3);
            CollectionAssert.AreEqual(lines, ShiftPlayKit.CommandLines(Sim.Record), "devamdan sonra komutlar birebir");
            Assert.AreEqual(rng, Sim.S.Rng.State, "RNG aynı");
            BoardPlayKit.AssertNoCommand("kayıttan devam");
            Assert.AreEqual(n, Adm.Group.Count, "grup bekliyor");
            Assert.IsTrue(K.Door.Knocking, "grup bekliyor: vuruş lambası");
            Assert.IsFalse(K.Door.KnockPlaying, "yüklemede vuruş yok");
            Assert.AreEqual(cards, CardsFingerprint(K.Last.Group), "kartlar aynı");
            Assert.AreEqual(wait, K.Last.Group.WaitLeft, "bekleme aynı");
            yield return K.Settle();
            Assert.AreEqual(CounterState.Down, St.Counter, "devamda tezgâh inik");
            Logs();
            yield return ToCounter();
            Assert.AreEqual(0, sounds.Count(AdmissionSound.Knock), "devamdan sonra vuruş yok");
            Assert.AreEqual(n, View.CardCount, "kartlar tezgâhta");
            for (int i = 0; i < n; i++)
            {
                Assert.IsTrue(View.CardPapers[i].gameObject.activeInHierarchy, "kart " + (i + 1) + " görünür");
                Assert.AreEqual(Adm.Group[i].Resident.Id, View.CardFace(i).Data.Id, "kart " + (i + 1) + " aynı aday");
                Assert.IsFalse(View.CardFace(i).ImpressionVisible, "kartta iz yok");
            }
            Assert.AreEqual(slipBody, View.SlipFace.Body.text, "fişte bekleme aynı");
            Assert.IsFalse(View.SlipFace.StampVisible, "fişte iz yok");
            BoardPlayKit.AssertNoCommand("devamdan sonra K8A");
        }

        // ================================================================== (h) İngilizce

        /// <summary>
        /// (h) Dil EN iken (grup gelmeden seçilir) kartların, grup fişinin ve iki damganın çizilen bütün yazılarında ve verinin etiket
        /// satırlarında Türkçe'ye özgü harf kalmaz. Kurucu adları (kefil satırı) özel addır, sayılmaz. Teleks satırı da İngilizce.
        /// </summary>
        [UnityTest]
        public IEnumerator English_CardAndSlipTexts_NoTurkishLetters()
        {
            yield return LiveGroup(() =>
            {
                DeskLoc.Instance.SetLanguage(DeskLang.En);
                Logs();
                ListenTelex();
            });
            Assert.AreEqual(DeskLang.En, DeskLoc.Instance.Lang, "dil EN");
            yield return BoardPlayKit.WaitFor(() => telex.Count > 0, 3, "teleks satırı düşmedi");
            foreach (var (title, body) in telex) AssertEnglish(title + " · " + body, "teleks");
            yield return ToCounter();
            var g = K.Last.Group;
            int n = g.Cards.Count;
            int scanned = 0;
            for (int i = 0; i < n; i++)
            {
                var face = View.CardFace(i);
                Assert.IsFalse(face.Overflowing, "EN kart " + (i + 1) + " taşmıyor");
                Assert.AreEqual(Loc.T("KYGA · ADAY KARTI"), face.Header.text, "kart künyesi çevrildi");
                Assert.AreNotEqual("KYGA · ADAY KARTI", face.Header.text, "kart künyesi Türkçe kalmadı");
                foreach (var t in View.CardPapers[i].GetComponentsInChildren<TMP_Text>(false))
                {
                    if (string.IsNullOrEmpty(t.text)) continue;
                    AssertEnglish(t.text, "kart " + (i + 1) + " / " + t.name);
                    scanned++;
                }
                var c = g.Cards[i];
                foreach (string line in new[] { c.Name, c.Arrived, c.Mastery, c.Stats, c.TierName, c.Suspicion, c.HoverLine })
                    AssertEnglish(line, "kart verisi " + (i + 1));
                foreach (var p in c.Perks) AssertEnglish(p.Tag + " " + p.Name + " " + p.Effect, "özellik " + p.Id);
            }
            Assert.Greater(scanned, 6 * n, "kartlarda yazı tarandı");
            Assert.IsFalse(View.SlipFace.Overflowing, "EN fiş taşmıyor");
            foreach (var t in View.SlipPaper.GetComponentsInChildren<TMP_Text>(false))
                if (!string.IsNullOrEmpty(t.text)) AssertEnglish(t.text, "grup fişi / " + t.name);
            foreach (string line in new[] { g.SlipHeader, g.SlipShift, g.SlipTitle, g.WaitLine, g.HintLine, g.RoomTitle, g.SlipHoverTitle, g.SlipHoverLine, g.RowHoverLine })
                AssertEnglish(line, "fiş verisi");
            foreach (var choice in new[] { StampChoice.Admit, StampChoice.Reject })
            {
                var d = K.Last.Stamps[(int)choice];
                AssertEnglish(d.Plate + " " + d.Cap, choice + " damgası");
                foreach (var t in K.Stamp(choice).GetComponentsInChildren<TMP_Text>(false))
                    if (!string.IsNullOrEmpty(t.text)) AssertEnglish(t.text, choice + " damgası / " + t.name);
            }
            AssertEnglish(K.Last.CounterHoverTitle + " " + K.Last.CounterHoverLine, "tezgâh etiketi");
        }

        /// <summary>Metinde Türkçe'ye özgü harf yok (kurucu adları çıkarıldıktan sonra).</summary>
        static void AssertEnglish(string text, string where)
        {
            if (string.IsNullOrEmpty(text)) return;
            string rest = text;
            foreach (var p in Sim.Board.People)
            {
                if (string.IsNullOrEmpty(p.Name)) continue;
                rest = rest.Replace(p.Name, "").Replace(DeskText.Upper(p.Name), "");
            }
            int at = rest.IndexOfAny(TurkishOnly.ToCharArray());
            Assert.AreEqual(-1, at, where + ": EN metinde Türkçe harf “" + text + "”");
        }

        // ================================================================== gerçek ışın

        /// <summary>
        /// Gerçek ışın, K8A: her aday kartının çarpıştırıcısına kameradan giden ışının ilk çarptığı nesne o karttır; tık (Input System 1.19
        /// kuralı: basış ve tık <c>CandidateGroupView</c>'a) kartı okumaya açar (K8R, çapa kartın üstünde), ikinci tık K8A'ya döner. Komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator RealRay_K8A_CardClick_OpensThatCard()
        {
            yield return LiveGroup();
            yield return ToCounter();
            var mark = ShiftPlayKit.Mark();
            int n = View.CardCount, real = 0;
            for (int i = 0; i < n; i++)
            {
                Assert.AreEqual(SiginakPose.Admission, Rig.Current, "kart " + (i + 1) + " K8A'dan tıklanır");
                var paper = View.CardPapers[i];
                int index = i;
                bool viaRay = RealClick(CardHit(i), "K8A · aday kartı " + (i + 1), () => K.Admission.ClickPaper(AdmissionPaper.Candidate, index));
                if (viaRay)
                {
                    real++;
                    var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(CardHit(i).gameObject);
                    Assert.AreSame(View.gameObject, handler, "kart tıkını aday görünümü alır");
                }
                yield return ShiftPlayKit.WaitForPose(SiginakPose.AdmissionRead, 3);
                yield return K.Settle();
                var anchor = K.ReadAnchor;
                var flat = Vector3.ProjectOnPlane(anchor.position - paper.position, CounterUp);
                Assert.Less(flat.magnitude, 0.12f, "okuma çapası " + (i + 1) + ". kartın üstünde (" + flat.magnitude.ToString("0.000", CultureInfo.InvariantCulture) + " m)");
                var cam = BoardPlayKit.View;
                var face = View.CardFace(i);
                Note("K8R kart " + (i + 1) + " punto (1080p px): ad " + K.ScreenPx(face.NameText, cam).ToString("0.0", CultureInfo.InvariantCulture) +
                     " · ustalık " + K.ScreenPx(face.Profession, cam).ToString("0.0", CultureInfo.InvariantCulture) +
                     " · şüphe " + K.ScreenPx(face.Origin, cam).ToString("0.0", CultureInfo.InvariantCulture) +
                     " · beden " + K.ScreenPx(face.Stats, cam).ToString("0.0", CultureInfo.InvariantCulture) +
                     (face.Data.Perks.Count > 0 ? " · özellik adı " + K.ScreenPx(face.PerkName(0), cam).ToString("0.0", CultureInfo.InvariantCulture) +
                         " · özellik etkisi " + K.ScreenPx(face.PerkEffect(0), cam).ToString("0.0", CultureInfo.InvariantCulture) : ""));
                var sp = cam.WorldToViewportPoint(paper.position);
                Assert.IsTrue(sp.z > 0 && sp.x > 0.1f && sp.x < 0.9f && sp.y > 0.1f && sp.y < 0.9f, "okunan kart kadrajın içinde (" + sp + ")");
                K.Admission.HandleBack();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Admission, 3);
                yield return K.Settle();
            }
            Note("gerçek ışınla tıklanan kart: " + real + "/" + n);
            ShiftPlayKit.AssertUnchanged(mark, "kart okumak");
        }

        /// <summary>
        /// Gerçek ışın, K8A: grup fişinin çarpıştırıcısına giden ışının ilk çarptığı nesne fiştir; tık (satır dışı) fişi okumaya açar
        /// (K8R). Komut yok.
        /// </summary>
        [UnityTest]
        public IEnumerator RealRay_K8A_GroupSlipClick_OpensSlip()
        {
            yield return LiveGroup();
            yield return ToCounter();
            var mark = ShiftPlayKit.Mark();
            bool viaRay = RealClick(SlipHit, "K8A · grup fişi", () => K.Admission.ClickPaper(AdmissionPaper.GroupSlip, 0));
            if (viaRay) Assert.AreSame(View.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(SlipHit.gameObject), "fiş tıkını aday görünümü alır");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.AdmissionRead, 3);
            yield return K.Settle();
            var flat = Vector3.ProjectOnPlane(K.ReadAnchor.position - View.SlipPaper.position, CounterUp);
            Assert.Less(flat.magnitude, 0.12f, "okuma çapası fişin üstünde (" + flat.magnitude.ToString("0.000", CultureInfo.InvariantCulture) + " m)");
            var cam = BoardPlayKit.View;
            var face = View.SlipFace;
            Note("K8R grup fişi punto (1080p px): başlık " + K.ScreenPx(face.Title, cam).ToString("0.0", CultureInfo.InvariantCulture) +
                 " · bekleme " + K.ScreenPx(face.Body, cam).ToString("0.0", CultureInfo.InvariantCulture) +
                 " · ipucu " + K.ScreenPx(face.Hint, cam).ToString("0.0", CultureInfo.InvariantCulture) +
                 " · yer " + K.ScreenPx(face.RoomTitle, cam).ToString("0.0", CultureInfo.InvariantCulture));
            foreach (var corner in new[] { new Vector2(2, 2), new Vector2(CandidateGroupSlipFace.WidthMm - 2, CandidateGroupSlipFace.HeightMm - 2) })
            {
                var vp = cam.WorldToViewportPoint(SlipWorld(corner));
                Assert.IsTrue(vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1, "210 mm fiş K8R kadrajına sığar (köşe " + corner + " → " + vp + ")");
            }
            K.Admission.HandleBack();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Admission, 3);
            yield return K.Settle();
            Note("grup fişi gerçek ışınla: " + viaRay);
            ShiftPlayKit.AssertUnchanged(mark, "grup fişini okumak");
        }
    }
}
