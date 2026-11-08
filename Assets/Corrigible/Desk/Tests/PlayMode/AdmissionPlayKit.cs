using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Kapı ve kabul PlayMode testlerinin ortak düzeni (Revizyon 3 · Aşama 6, R9/R17; PLAN §5.2, E §4.1). <see cref="BoardPlayKit"/> ve
    /// <see cref="ShiftPlayKit"/>'in üstüne kurulur: sahne yükleme, kayıt yalıtımı, sanal aygıtlar, gerçek kapanış (şalter + EVRAK) orada.
    /// <list type="bullet">
    /// <item><b>Tutamaklar</b> <see cref="SiginakAdmission"/>'ın sahne bağlarından (yansıma; ad araması yok). <see cref="RequireWired"/>: W6 sahne
    /// adımı yapılmadıysa test nedeniyle düşer.</item>
    /// <item><b>Durumlara gerçek komutlarla ulaşmak</b> (E §5.1): kurulum günleri <b>kayıt fikstürüyle</b> geçilir: canlı oyunun denge
    /// değerlerinin kopyasıyla (<see cref="FixtureGame"/>; istenirse değiştirilmiş) ayrı bir <see cref="Simulation"/> açılır, panonun açılış
    /// komutları (<c>board_auto</c> + emekli görevlerin BOŞTA'ya alınışı; <c>PlanningBoardPresenter.RetireJobs</c> ile aynı sıra) ve kapanışlar
    /// (<c>PrintShift</c> + <c>FileShift</c>) ona yazılır, kayıt dosyasına konur ve KAYITTAN DEVAM edilir (<see cref="ContinueFrom"/>). Kapanış
    /// sunumu atlanmaz (Risk 18): canlı sahnede komut yoktur, devam bir yüklemedir (vuruş çalmaz, D12). Test altındaki kapanış gerçek arayüzle
    /// (<see cref="ShiftPlayKit.PrintToTongue"/> + <see cref="ShiftPlayKit.FileOnEvrak"/>).</item>
    /// <item><b>Tık</b> gerçek ışınla (<see cref="HitOn"/>: çarpıştırıcının ekranda ilk çarpılan noktası; örtülme sınanır) ve Input System 1.19
    /// modülünün kuralıyla (<see cref="ClickHit"/>): basış hiyerarşide ilk <c>IPointerDownHandler</c>'a (istasyon), tık en yakın
    /// <c>IPointerClickHandler</c>'a (<c>pointerClick</c>; damga, büyüteç, defter istasyonun çocuğu olsa da tıkı kendileri alır).
    /// <c>BoardPlayKit.ClickHit</c> eski kuralı (tık yalnız basılan nesneye) uygular; kabul araçları için kullanılmaz.</item>
    /// <item><b>Sürükleme</b> <see cref="PointerGesture"/> ile (sürükleme olayları her adımda <c>pointerDrag</c>'a; GELISTIRME). Damganın bırakılış
    /// noktası damganın elde tutulduğu yükseklikteki düzlemin ışını (<see cref="StampDropScreen"/>; görünümün kendi hesabı).</item>
    /// <item><b>Örnekleme</b> <c>yield return null</c>'dan sonra (o karenin <c>Update</c>'lerinden sonra, <c>LateUpdate</c>'ten önce; GELISTIRME);
    /// hareketler <c>Update</c>'te yürür, ölçüm hareket durduktan sonra (<see cref="Settle"/>). Tolerans <see cref="BoardPlayKit.FrameTolerance"/>.</item>
    /// <item><b>Ses</b> <c>Sfx.Play</c>'den gözlenemez (Game assembly'si): akışın <see cref="SiginakAdmission.Sounded"/> olayı
    /// (<see cref="AdmissionSoundLog"/>).</item>
    /// </list>
    /// </summary>
    public static class AdmissionPlayKit
    {
        /// <summary>Tohum 79'un ilk adayı (W0 C8 altın değerleri, E §5.1).</summary>
        public const string FirstCandidate = "resident-9";
        public const int FirstCandidateDay = 4;

        // ---------- Tutamaklar ----------

        public static Simulation Sim => BoardPlayKit.Sim;
        public static GameController Game => BoardPlayKit.Game;
        public static SiginakCameraRig Rig => BoardPlayKit.Rig;
        public static SiginakAdmission Admission => BoardPlayKit.Find<SiginakAdmission>();
        public static AdmissionStation Station => Get<AdmissionStation>("station");
        public static ColdGlassView Glass => Get<ColdGlassView>("glass");
        public static IntercomTool Intercom => Get<IntercomTool>("intercom");
        public static MagnifierTool Magnifier => Get<MagnifierTool>("magnifier");
        public static RequestPadTool Pad => Get<RequestPadTool>("pad");
        public static DoorProp Door => Get<DoorProp>("door");
        public static Transform ReadAnchor => Get<Transform>("readAnchor");
        public static StampDecision[] Stamps => Get<StampDecision[]>("stamps") ?? new StampDecision[0];
        public static AdmissionSnapshot Last => Admission != null ? Admission.Last : null;
        public static string CandidateId => Sim?.Admission?.Candidate?.Resident?.Id;

        /// <summary>İstasyonun tezgâh çarpıştırıcısı (inik tezgâha tık → K8A).</summary>
        public static Collider CounterHit => ShiftPlayKit.Get<Collider>(Station, "counterHit");
        /// <summary>MASA tuşunun çarpıştırıcısı (doğrudan K1).</summary>
        public static Collider DeskKeyHit => ShiftPlayKit.Get<Collider>(Station, "deskKeyHit");
        /// <summary>Belge kâğıdının çarpıştırıcısı (kökte; işleyicisiz, tık istasyona çıkar).</summary>
        public static Collider DocumentHit => ColliderOf(Station != null ? Station.DocumentPaper : null);
        /// <summary>Talimat şeridinin çarpıştırıcısı.</summary>
        public static Collider StripHit => ColliderOf(Station != null ? Station.StripPaper : null);

        static Collider ColliderOf(Transform root)
        {
            if (root == null) return null;
            var c = root.GetComponent<Collider>();
            return c != null ? c : root.GetComponentInChildren<Collider>(true);
        }

        public static StampDecision Stamp(StampChoice choice)
        {
            var s = Stamps.FirstOrDefault(x => x != null && x.Choice == choice);
            Assert.IsNotNull(s, "damga yok: " + choice + " (SiginakAdmission.stamps; W6)");
            return s;
        }

        /// <summary>Odanın KAPI tuşu (<c>SiginakDesk.rail</c>[Door]).</summary>
        public static StationKey DoorKey
        {
            get
            {
                var rail = ShiftPlayKit.Get<StationKeyRail>(BoardPlayKit.Desk, "rail");
                return rail != null && rail.Count > StationKeyRail.Door ? rail[StationKeyRail.Door] : null;
            }
        }

        static T Get<T>(string field) => ShiftPlayKit.Get<T>(Admission, field);

        /// <summary>
        /// Kabul sahneye bağlı (W6 §2.2 #1–#13): akış, istasyon (tezgâh, panel, kâğıtlar), cam, interkom, büyüteç, defter, dört damga (her seçim
        /// bir kez), kapı, K8R çapası, masanın ve kapı denetleyicisinin <c>admission</c> bağı, K8A/K8G/K8R pozları.
        /// </summary>
        public static void RequireWired()
        {
            var a = Admission;
            Assert.IsNotNull(a, "SiginakAdmission sahnede yok (W6: SiginakRoom/Admission controller)");
            Assert.AreSame(a, ShiftPlayKit.Get<SiginakAdmission>(BoardPlayKit.Desk, "admission"), "SiginakDesk.admission → SiginakAdmission (W6)");
            Assert.AreSame(a, ShiftPlayKit.Get<SiginakAdmission>(BoardPlayKit.Surface, "admission"), "LiberationDeskView.admission (W6)");
            Assert.AreSame(a, ShiftPlayKit.Get<SiginakAdmission>(BoardPlayKit.Presenter, "admission"), "PlanningBoardPresenter.admission (W6)");
            Assert.IsNotNull(a.DoorController, "SiginakAdmission.doorController (W6)");
            Assert.AreSame(a, ShiftPlayKit.Get<SiginakAdmission>(a.DoorController, "admission"), "SiginakDoor.admission (W6)");
            Assert.IsNotNull(Station, "SiginakAdmission.station → Admission station (W6)");
            Assert.IsNotNull(Station.Top, "AdmissionStation: Counter Top PartDriver (ConfigureCounter)");
            Assert.IsNotNull(Station.Drawer, "AdmissionStation: Panel Drawer PartDriver (ConfigurePanel)");
            Assert.IsNotNull(Station.DocumentPaper, "AdmissionStation: belge kâğıdı (ConfigurePapers)");
            Assert.IsNotNull(Station.StripPaper, "AdmissionStation: talimat şeridi (ConfigurePapers)");
            Assert.Greater(Station.SlipPapers.Count, 0, "AdmissionStation: fiş havuzu (ConfigurePapers)");
            Assert.IsNotNull(CounterHit, "AdmissionStation.counterHit");
            Assert.IsNotNull(DeskKeyHit, "AdmissionStation.deskKeyHit (MASA tuşu)");
            Assert.IsNotNull(Glass, "SiginakAdmission.glass (ColdGlassView)");
            Assert.IsNotNull(Glass.Hit, "ColdGlassView çarpıştırıcısı");
            Assert.IsNotNull(Intercom, "SiginakAdmission.intercom (IntercomTool)");
            Assert.IsNotNull(Intercom.Hit, "IntercomTool çarpıştırıcısı");
            Assert.IsNotNull(Magnifier, "SiginakAdmission.magnifier");
            Assert.IsNotNull(Magnifier.Hit, "MagnifierTool çarpıştırıcısı");
            Assert.IsNotNull(Pad, "SiginakAdmission.pad");
            Assert.IsNotNull(Pad.Hit, "RequestPadTool çarpıştırıcısı");
            Assert.IsNotNull(Door, "SiginakAdmission.door (DoorProp)");
            Assert.IsNotNull(ReadAnchor, "SiginakAdmission.readAnchor (Poses/K8R)");
            foreach (StampChoice c in Enum.GetValues(typeof(StampChoice)))
            {
                var s = Stamp(c);
                Assert.IsNotNull(s.Hit, c + " damgasının çarpıştırıcısı");
                Assert.AreEqual(1, Stamps.Count(x => x != null && x.Choice == c), c + " damgası bir kez");
            }
            foreach (var pose in new[] { SiginakPose.Door, SiginakPose.Admission, SiginakPose.BreathGlass, SiginakPose.AdmissionRead })
                Assert.IsTrue(Rig.TryGetPose(pose, out _), pose + " pozu rig'de (W6 §2.2 #9–#10)");
            Assert.IsNotNull(DoorKey, "SiginakDesk.rail[Door] (KAPI tuşu)");
        }

        // ---------- Kurulum ----------

        /// <summary>DeskGame (<paramref name="seed"/>), kabul bağlı; kamera K1'de, şalter hazır.</summary>
        public static IEnumerator Begin(uint seed = BoardPlayKit.DefaultSeed)
        {
            yield return BoardPlayKit.LoadDesk(seed);
            ShiftPlayKit.RequireWired();
            RequireWired();
            yield return BoardPlayKit.Frames(2);
            BoardPlayKit.ResetFrameStats();
        }

        /// <summary>Canlı oyunun denge değerlerinin kopyası (kayıt fikstürü kendi dengesini taşır; <c>GameRecord.balance</c>).</summary>
        public static Balance BalanceCopy(Action<Balance> tweak = null)
        {
            Assert.IsNotNull(Sim, "önce DeskGame (Begin)");
            var b = JsonUtility.FromJson<Balance>(JsonUtility.ToJson(Sim.B));
            b.planningBoardEnabled = true;
            tweak?.Invoke(b);
            return b;
        }

        /// <summary>
        /// Kayıt fikstürünün oyunu: canlı dengenin kopyası (<paramref name="tweak"/> değiştirir), tohum <paramref name="seed"/>, panonun açılış
        /// komutları (<see cref="Retire"/>). Uzun yollarda (gün ≥ 10) <c>liberationAlarmPerShift</c> 0 verilir: karşı saldırı kurulumu bozmasın
        /// (alarm RNG çekmez; W0 C8 aynı denge).
        /// </summary>
        public static Simulation FixtureGame(uint seed = BoardPlayKit.DefaultSeed, Action<Balance> tweak = null)
        {
            var sim = Simulation.NewGame(seed, BalanceCopy(tweak));
            Assert.IsNotNull(sim.Board, "fikstür kartlı pano kipinde");
            Assert.IsNotNull(sim.Admission, "fikstürde topluluk kabulü var");
            Retire(sim);
            return sim;
        }

        /// <summary>
        /// <c>PlanningBoardPresenter.RetireJobs</c>'un komutları aynı sırayla (<c>board_auto</c>, sonra emekli görevli her canlı BOŞTA'ya):
        /// canlı oyunun ilk karelerinde yazılanlarla aynı kayıt (K4-A gözetmeni Boşta'dan; tohum 79'da Cem, İpek, Zeynep).
        /// </summary>
        public static void Retire(Simulation sim)
        {
            if (!sim.Board.AutomaticProduction) Ok(sim.EnableAutomaticProduction(), "board_auto");
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                Ok(sim.AssignResident(p.Id, "rest"), "emekli görev BOŞTA'ya: " + p.Name);
        }

        /// <summary>Fikstürde tam bir kapanış (<c>board_print</c> + <c>board_file</c>).</summary>
        public static void FixtureClose(Simulation sim)
        {
            int day = sim.S.Day;
            Ok(sim.PrintShift(), "fikstür board_print (gün " + day + ")");
            Ok(sim.FileShift(sim.Board.ReceiptId), "fikstür board_file (gün " + day + ")");
            Assert.AreEqual(day + 1, sim.S.Day, "fikstür kapanışı günü bir ilerletir");
        }

        /// <summary>Fikstürü <paramref name="day"/>. vardiyaya kapanışlarla getirir.</summary>
        public static void FixtureToDay(Simulation sim, int day)
        {
            for (int guard = 0; sim.S.Day < day && guard < 64; guard++) FixtureClose(sim);
            Assert.AreEqual(day, sim.S.Day, "fikstür günü");
        }

        /// <summary>
        /// Fikstürü kayıt dosyasına yazar (test klasörü) ve KAYITTAN DEVAM eder. Masanın yan dosyası (<c>liberation_desk.json</c>) silinir:
        /// canlı oyunun (aynı tohum) görülen sürümü fikstüre karışmasın; devam yeni oyun kuralıyla güncel sürümü görülmüş sayar (eleştiri 8).
        /// Yazım ve yükleme aynı karede (canlı oyun araya kayıt yazamaz).
        /// </summary>
        public static IEnumerator ContinueFrom(Simulation sim)
        {
            Assert.IsTrue(sim.Record.commands.Count > 0, "fikstür en az bir komut taşır (SaveStore.Load kartlı oyunda komutsuz günü reddeder)");
            sim.Record.day = sim.S.Day;
            string desk = Path.Combine(SaveStore.StorageDirectory, SiginakDeskStore.FileName);
            if (File.Exists(desk)) File.Delete(desk);
            SaveStore.Write(sim.Record);
            var saved = SaveStore.Load();
            Assert.IsNotNull(saved, "fikstür kaydı okunur (sürüm " + GameRecord.CurrentVersion + ")");
            Assert.AreEqual(sim.Record.commands.Count, saved.commands.Count, "fikstürün bütün komutları dosyada");
            yield return ShiftPlayKit.Continue();
            RequireWired();
            Assert.AreEqual(sim.S.Day, Sim.S.Day, "devam fikstürün gününde");
            Assert.AreEqual(sim.S.Rng.State, Sim.S.Rng.State, "devam fikstürle aynı RNG durumunda (yeniden oynatma belirlenimci)");
            yield return BoardPlayKit.Frames(2);
            BoardPlayKit.ResetFrameStats();
        }

        /// <summary>
        /// Tohum 79 V.005: kapıda Suna 9 (<see cref="FirstCandidate"/>). <paramref name="before"/> fikstürde son kapanıştan önce (ör. bir
        /// önceki adaya karar). Kamera K1'de, tezgâh inik.
        /// </summary>
        public static IEnumerator AtFirstCandidate()
        {
            yield return Begin();
            var sim = FixtureGame();
            FixtureToDay(sim, FirstCandidateDay);
            Assert.AreEqual(FirstCandidate, sim.Admission.Candidate?.Resident?.Id, "tohum 79 V.005 adayı (W0 C8; gözlenen: " + sim.Admission.Candidate?.Resident?.Name + ")");
            yield return ContinueFrom(sim);
            yield return Settle();
        }

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        // ---------- Pozlar ve geçişler ----------

        /// <summary>KAPI tuşuna tık (SiginakDesk.Station → <c>admission.Enter</c>), K8'e varış ve istasyonun durgunluğu.</summary>
        public static IEnumerator EnterDoor()
        {
            var key = DoorKey;
            Assert.IsNotNull(key?.Target, "KAPI tuşunun hedefi");
            BoardPlayKit.Click(key.Target.gameObject);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Door, 3);
            yield return Settle();
        }

        /// <summary>K8'de inik tezgâha gerçek ışınla tık → K8A.</summary>
        public static IEnumerator ToCounter()
        {
            if (Rig.Current != SiginakPose.Door) yield return EnterDoor();
            Assert.AreEqual(CounterState.Down, Station.Counter, "tezgâh inik (aday var ya da görülmemiş talimat)");
            ClickCollider(CounterHit, "inik tezgâh (K8)");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Admission, 3);
            yield return Settle();
        }

        /// <summary>K8A'da çekmecedeki belgeye tık: belge tezgâha (T3, komutsuz).</summary>
        public static IEnumerator TakeDocument()
        {
            Assert.AreEqual(SiginakPose.Admission, Rig.Current, "belge K8A'da alınır");
            if (!Station.DocumentOnCounter)
            {
                yield return BoardPlayKit.WaitFor(() => Station.DocumentInDrawer, 2, "belge çekmecede durmadı");
                ClickCollider(DocumentHit, "çekmecedeki belge");
                yield return BoardPlayKit.WaitFor(() => Station.DocumentOnCounter, 2, "belge tezgâha gelmedi (T3)");
            }
            yield return Settle();
        }

        /// <summary>Kamera, istasyon, araçlar ve damgalar durana dek (en çok <paramref name="timeout"/> s), sonra bir kare.</summary>
        public static IEnumerator Settle(float timeout = 4)
        {
            yield return BoardPlayKit.WaitFor(Still, timeout, "kabul istasyonu durmadı (" + Describe() + ")");
            yield return BoardPlayKit.Frames(1);
        }

        public static bool Still()
        {
            if (Rig != null && Rig.InTransition) return false;
            if (Admission != null && Admission.Busy) return false;
            var st = Station;
            if (st != null && st.Moving) return false;
            if (Magnifier != null && Magnifier.Moving) return false;
            if (Pad != null && Pad.Moving) return false;
            if (Glass != null && Glass.Playing) return false;
            if (Intercom != null && Intercom.Playing) return false;
            if (Door != null && Door.Moving) return false;
            foreach (var s in Stamps) if (s != null && s.Moving) return false;
            return true;
        }

        public static string Describe()
        {
            var st = Station;
            return "poz " + (Rig != null ? Rig.Current.ToString() : "?") + (Rig != null && Rig.InTransition ? " (geçişte)" : "") +
                   " · tezgâh " + (st != null ? st.Counter + " " + st.TopAngle.ToString("0.0", CultureInfo.InvariantCulture) + "°" : "?") +
                   " · istasyon " + (st != null && st.Moving ? "hareketli" : "durgun") + " · dizi " + (Admission != null && Admission.Busy) +
                   " · kapı " + (Door != null && Door.Moving ? "hareketli" : "durgun");
        }

        // ---------- Gerçek ışın ve tık (Input System 1.19 kuralı) ----------

        /// <summary>
        /// <paramref name="c"/>'nin ekranda ışının ilk çarptığı bir noktası: kutu çarpıştırıcıda yerel kutunun 5 × 5 × 5 ızgarası, ötekilerde
        /// sınır kutusu, merkezden dışa sırayla. Hiçbirinde ilk sonuç bu çarpıştırıcı (ya da çocuğu) değilse testi düşürür (örtülme).
        /// </summary>
        public static RaycastResult HitOn(Collider c, string what)
        {
            Assert.IsNotNull(c, what + ": çarpıştırıcı yok");
            Assert.IsTrue(c.enabled && c.gameObject.activeInHierarchy, what + ": çarpıştırıcı kapalı (" + Describe() + ")");
            var cam = BoardPlayKit.View;
            Assert.IsNotNull(cam, "odanın kamerası");
            var fractions = new[] { 0f, -0.2f, 0.2f, -0.4f, 0.4f };
            string last = null;
            var points = new List<Vector3>();
            if (c is BoxCollider box)
            {
                foreach (float fy in fractions) foreach (float fx in fractions) foreach (float fz in fractions)
                    points.Add(box.transform.TransformPoint(box.center + Vector3.Scale(box.size, new Vector3(fx, fy, fz))));
            }
            else
            {
                var b = c.bounds;
                foreach (float fy in fractions) foreach (float fx in fractions) foreach (float fz in fractions)
                    points.Add(b.center + Vector3.Scale(b.size, new Vector3(fx, fy, fz)));
            }
            foreach (var world in points)
            {
                var sp = cam.WorldToScreenPoint(world);
                if (sp.z <= cam.nearClipPlane || sp.x < 2 || sp.y < 2 || sp.x > cam.pixelWidth - 2 || sp.y > cam.pixelHeight - 2) continue;
                var screen = new Vector2(sp.x, sp.y);
                var hit = BoardPlayKit.FirstHit(screen);
                if (hit.gameObject != null && (hit.gameObject == c.gameObject || hit.gameObject.transform.IsChildOf(c.transform))) return hit;
                last = BoardPlayKit.DescribeHits(screen);
            }
            Assert.Fail(what + ": ekranda ilk çarpılan nokta bulunamadı (örtülü ya da kadraj dışı; son: " + last + " · " + Describe() + ")");
            return default;
        }

        /// <summary>
        /// Gerçek ışın sonucuna sol tık, InputSystemUIInputModule 1.19'un sırasıyla: giriş bütün atalara, basış hiyerarşide ilk
        /// <c>IPointerDownHandler</c>'a (yoksa tık işleyicisine), olası sürükleme, bırakış basılana, tık en yakın <c>IPointerClickHandler</c>'a
        /// (<c>pointerClick</c>), çıkış atalara.
        /// </summary>
        public static GameObject ClickHit(RaycastResult hit)
        {
            var go = hit.gameObject;
            if (go == null) return null;
            var ped = BoardPlayKit.PointerAt(hit);
            for (var t = go.transform; t != null; t = t.parent) ExecuteEvents.Execute(t.gameObject, ped, ExecuteEvents.pointerEnterHandler);
            var press = ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerDownHandler);
            var click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
            if (press == null) press = click;
            ped.pointerPress = press;
            ped.pointerClick = click;
            ped.rawPointerPress = go;
            ped.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(go);
            if (ped.pointerDrag != null) ExecuteEvents.Execute(ped.pointerDrag, ped, ExecuteEvents.initializePotentialDrag);
            if (press != null) ExecuteEvents.Execute(press, ped, ExecuteEvents.pointerUpHandler);
            if (click != null && ped.eligibleForClick) ExecuteEvents.Execute(click, ped, ExecuteEvents.pointerClickHandler);
            ped.pointerPress = null;
            ped.pointerClick = null;
            ped.pointerDrag = null;
            for (var t = go.transform; t != null; t = t.parent) ExecuteEvents.Execute(t.gameObject, ped, ExecuteEvents.pointerExitHandler);
            return go;
        }

        /// <summary>Çarpıştırıcıya gerçek ışınla tık (<see cref="HitOn"/> + <see cref="ClickHit"/>).</summary>
        public static GameObject ClickCollider(Collider c, string what) => ClickHit(HitOn(c, what));

        /// <summary>Ekran noktasına gerçek ışınla tık (ilk sonuç); çarpılan nesne.</summary>
        public static GameObject ClickScreen(Vector2 screen) => ClickHit(BoardPlayKit.FirstHit(screen));

        /// <summary>Aracın çarpıştırıcısı (büyüteç, interkom, buğu camı, talep defteri).</summary>
        public static Collider ToolHit(AdmissionTool tool)
        {
            switch (tool)
            {
                case AdmissionTool.Papers: return Magnifier.Hit;
                case AdmissionTool.Interview: return Intercom.Hit;
                case AdmissionTool.Breath: return Glass.Hit;
                default: return Pad.Hit;
            }
        }

        /// <summary>Araca gerçek ışınla tık ve bir kare.</summary>
        public static IEnumerator ClickTool(AdmissionTool tool)
        {
            ClickCollider(ToolHit(tool), tool + " aracı");
            yield return BoardPlayKit.Frames(1);
        }

        /// <summary>Üzerine gelme: ışın sonucunun atalarına giriş (istasyon parça parça <c>pointerCurrentRaycast</c>'ten çözer).</summary>
        public static PointerEventData HoverHit(RaycastResult hit)
        {
            var ped = BoardPlayKit.PointerAt(hit);
            for (var t = hit.gameObject.transform; t != null; t = t.parent) ExecuteEvents.Execute(t.gameObject, ped, ExecuteEvents.pointerEnterHandler);
            return ped;
        }

        public static void Unhover(RaycastResult hit, PointerEventData ped)
        {
            for (var t = hit.gameObject.transform; t != null; t = t.parent) ExecuteEvents.Execute(t.gameObject, ped, ExecuteEvents.pointerExitHandler);
        }

        // ---------- Damga ----------

        /// <summary>Tezgâhın yukarısı (damga görünümünün düzlemi: <c>Top</c> çerçevesinin y'si).</summary>
        static Vector3 CounterUp => Station != null && Station.Top != null ? Station.Top.transform.up : Vector3.up;

        /// <summary>
        /// Elde tutulan damganın belgenin damga alanının merkezine basılacağı ekran noktası: damga, bırakış noktasını tutulduğu yükseklikteki
        /// düzlemin ışınından bulur ve yüksekliği çıkarır (<see cref="StampDecision.DropPoint"/>); bu yüzden hedef = alan merkezi + yukarı ×
        /// <see cref="StampDecision.HeldHeight"/>. Noktanın gerçekten alanda olduğu ayrıca sınanır.
        /// </summary>
        public static Vector2 StampDropScreen(StampDecision stamp)
        {
            var pose = Station.StampPressPose();
            var screen = BoardPlayKit.ScreenPoint(pose.position + CounterUp * stamp.HeldHeight);
            Assert.IsTrue(stamp.DropPoint(screen, out var drop), stamp.Choice + ": bırakış noktası çözülmedi");
            Assert.IsTrue(Station.StampAreaContains(drop), stamp.Choice + ": hedef belgenin damga alanında değil (" + Station.DocumentMm(drop, out _) + " mm)");
            return screen;
        }

        /// <summary>Belgenin dışında, tezgâhta bir ekran noktası (damganın rafına döneceği bırakış).</summary>
        public static Vector2 StampElsewhereScreen(StampDecision stamp)
        {
            var top = Station.Top.transform;
            foreach (var local in new[] { new Vector3(-0.2f, 0.004f, -0.15f), new Vector3(0.25f, 0.004f, -0.12f), new Vector3(-0.15f, 0.004f, -0.05f) })
            {
                var world = top.TransformPoint(local);
                var screen = BoardPlayKit.ScreenPoint(world + CounterUp * stamp.HeldHeight);
                if (screen.x < 4 || screen.y < 4 || screen.x > Screen.width - 4 || screen.y > Screen.height - 4) continue;
                if (stamp.DropPoint(screen, out var drop) && !Station.StampAreaContains(drop)) return screen;
            }
            Assert.Fail("belge dışında bir bırakış noktası bulunamadı");
            return default;
        }

        static PointerGesture held;

        /// <summary>Söküm: bırakılmamış sürükleme bırakılır.</summary>
        public static void ReleaseGesture()
        {
            if (held != null && !held.Released) held.Release();
            held = null;
        }

        /// <summary>
        /// Damgayı raftan sürükler (ilk adımda sürükleme başlar → kalkış) ve <paramref name="target"/>'ın verdiği ekran noktasına
        /// <paramref name="frames"/> karede götürüp bırakır. <paramref name="onReleased"/> bırakış anında (basış zamanı ölçümü için).
        /// </summary>
        public static IEnumerator DragStamp(StampChoice choice, Func<StampDecision, Vector2> target, int frames = 8, Action onReleased = null)
        {
            var stamp = Stamp(choice);
            var hit = HitOn(stamp.Hit, choice + " damgası");
            held = BoardPlayKit.Press(hit);
            Assert.AreSame(stamp.gameObject, held.Dragged, choice + ": sürüklemenin alıcısı damganın kendisi (IDragHandler)");
            held.Step(hit.screenPosition + new Vector2(4, 4));
            yield return null;
            BoardPlayKit.TrackFrame();
            var to = target(stamp);
            yield return held.Move(to, frames);
            yield return held.Hold(2);
            held.Release();
            held = null;
            onReleased?.Invoke();
        }

        /// <summary>Tık-tık: damgaya tık (elde), fare hedefe, hedefe gerçek ışınla tık (istasyon ya da elde damga işler).</summary>
        public static IEnumerator ClickClickStamp(StampChoice choice, Action onSecondClick = null)
        {
            var stamp = Stamp(choice);
            ClickCollider(stamp.Hit, choice + " damgası (1. tık)");
            Assert.IsTrue(stamp.Held, choice + ": 1. tık damgayı ele alır (" + stamp.State + ")");
            yield return BoardPlayKit.WaitFor(() => stamp.State == StampDecision.StampState.Held, 1, "kalkış bitmedi");
            var to = StampDropScreen(stamp);
            yield return BoardPlayKit.MoveMouse(to);
            var hit = BoardPlayKit.FirstHit(to);
            Assert.IsNotNull(hit.gameObject, "2. tıkın noktasında bir şey yok");
            Assert.IsTrue(hit.gameObject.transform.IsChildOf(Station.transform), "2. tık istasyona düşer (" + BoardPlayKit.DescribeHits(to) + ")");
            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject);
            Assert.IsTrue(handler == stamp.gameObject || handler == Station.gameObject,
                "2. tık elde damgaya ya da istasyona işlenir, araca değil (" + (handler != null ? handler.name : "yok") + " · " + BoardPlayKit.DescribeHits(to) + ")");
            ClickHit(hit);
            onSecondClick?.Invoke();
            yield return BoardPlayKit.Frames(1);
        }

        // ---------- Okunurluk (canlı) ----------

        /// <summary>
        /// Dünya metninin 1080p'deki em yüksekliği (px): görünür karakterlerin en küçük çizilen puntosu × ölçek, metnin dikdörtgen merkezinden
        /// yukarı izdüşürülür (<c>SiginakTextScan.MeasureWorld</c> ile aynı kural; editör assembly'si buradan görülmez).
        /// </summary>
        public static float ScreenPx(TMP_Text t, Camera cam)
        {
            if (t == null || cam == null || string.IsNullOrEmpty(t.text)) return 0;
            t.ForceMeshUpdate(true, true);
            float pt = float.MaxValue;
            var info = t.textInfo;
            if (info != null)
                for (int i = 0; i < info.characterCount && i < info.characterInfo.Length; i++)
                    if (info.characterInfo[i].isVisible) pt = Mathf.Min(pt, info.characterInfo[i].pointSize);
            if (pt == float.MaxValue) pt = t.fontSize;
            float em = pt * Mathf.Abs(t.transform.lossyScale.y) * (t is TextMeshPro ? 0.1f : 1f);
            var centre = t.rectTransform.TransformPoint(t.rectTransform.rect.center);
            var a = cam.WorldToScreenPoint(centre);
            var b = cam.WorldToScreenPoint(centre + t.transform.up * em);
            if (a.z <= 0) return 0;
            return Vector2.Distance(a, b) * 1080f / Mathf.Max(1, cam.pixelHeight);
        }

        // ---------- Token ----------

        public static float Token(string key, float plan) => ShiftPlayKit.Token(key, plan);

        public static void AssertDuration(float took, float seconds, string what)
        {
            float tol = BoardPlayKit.FrameTolerance;
            Assert.That(took, Is.InRange(seconds - tol, seconds + tol), what + ": " + took.ToString("0.000", CultureInfo.InvariantCulture) + " s (beklenen " + seconds +
                " ± " + tol.ToString("0.000", CultureInfo.InvariantCulture) + ")");
        }

        // ---------- Kare örnekleri ----------

        /// <summary>Koşul doğru olana dek her karede örnek (o karenin Update'inden sonra); <paramref name="timeout"/>'ta düşürür.</summary>
        public static IEnumerator WatchUntil(List<AdmissionSample> into, Func<bool> condition, float timeout, string what)
        {
            float until = Time.unscaledTime + timeout;
            while (!condition())
            {
                if (Time.unscaledTime > until) Assert.Fail("zaman aşımı (" + timeout + " s): " + what + " · " + Describe());
                yield return null;
                BoardPlayKit.TrackFrame();
                into?.Add(AdmissionSample.Now());
            }
        }

        /// <summary><paramref name="seconds"/> boyunca her karede örnek.</summary>
        public static IEnumerator Watch(List<AdmissionSample> into, float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
            {
                yield return null;
                BoardPlayKit.TrackFrame();
                into?.Add(AdmissionSample.Now());
            }
        }

        /// <summary>Koşulu sağlayan ilk örneğin zamanı (<paramref name="after"/>'dan sonra); yoksa testi düşürür.</summary>
        public static float FirstAt(List<AdmissionSample> samples, Func<AdmissionSample, bool> p, string what, float after = float.NegativeInfinity)
        {
            foreach (var s in samples) if (s.At >= after && p(s)) return s.At;
            Assert.Fail("örneklerde yok: " + what + " (" + samples.Count + " örnek)");
            return 0;
        }

        public static bool Any(List<AdmissionSample> samples, Func<AdmissionSample, bool> p) => samples.Any(p);
    }

    /// <summary>Kapı ve istasyonun bir karesi (test kare döngüsünde, Update'lerden sonra alınır).</summary>
    public struct AdmissionSample
    {
        public float At;
        public int Frame;
        public SiginakPose Pose;
        public bool InTransition;
        public CounterState Counter;
        public float Top, Bracket, Drawer, Marker;
        public bool Lamp, Tools, Tins, Jewel, DocOnCounter, StationMoving, Sequence, OrdersChanging;
        public float Wheel, Leaf, Stair;
        public bool Flash, KnockPlaying, Knocking, DoorMoving, DoorOpen, Silhouette;
        public float Cover, Fog, FogScale, Button, Sheet, Carriage;
        public bool LineOpen;
        public bool Busy;
        public int Commands;
        public uint Rng;

        public static AdmissionSample Now()
        {
            var st = AdmissionPlayKit.Station;
            var door = AdmissionPlayKit.Door;
            var glass = AdmissionPlayKit.Glass;
            var intercom = AdmissionPlayKit.Intercom;
            var pad = AdmissionPlayKit.Pad;
            var magnifier = AdmissionPlayKit.Magnifier;
            var rig = AdmissionPlayKit.Rig;
            var sim = AdmissionPlayKit.Sim;
            var a = AdmissionPlayKit.Admission;
            return new AdmissionSample
            {
                At = Time.unscaledTime,
                Frame = Time.frameCount,
                Pose = rig != null ? rig.Current : SiginakPose.Desk,
                InTransition = rig != null && rig.InTransition,
                Counter = st != null ? st.Counter : CounterState.Folded,
                Top = st != null ? st.TopAngle : 0,
                Bracket = st != null ? st.BracketProgress : 0,
                Drawer = st != null ? st.DrawerOffset : 0,
                Marker = st != null ? st.MarkerOffset : 0,
                Lamp = st != null && st.LampOn,
                Tools = st != null && st.ToolsVisible,
                Tins = st != null && st.TinsVisible,
                Jewel = st != null && st.JewelOn,
                DocOnCounter = st != null && st.DocumentOnCounter,
                StationMoving = st != null && st.Moving,
                Sequence = st != null && st.SequenceActive,
                OrdersChanging = st != null && st.OrdersChanging,
                Wheel = door != null ? door.WheelDegrees : 0,
                Leaf = door != null ? door.LeafDegrees : 0,
                Stair = door != null ? door.StairGlow : 0,
                Flash = door != null && door.LampFlashing,
                KnockPlaying = door != null && door.KnockPlaying,
                Knocking = door != null && door.Knocking,
                DoorMoving = door != null && door.Moving,
                DoorOpen = door != null && door.IsOpen,
                Silhouette = door != null && door.Silhouette != null && door.Silhouette.Visible,
                Cover = glass != null ? glass.CoverAngle : 0,
                Fog = glass != null ? glass.FogAlpha : 0,
                FogScale = glass != null ? glass.FogScale : 1,
                Button = intercom != null ? intercom.ButtonOffset : 0,
                LineOpen = intercom != null && intercom.LineOpen,
                Sheet = pad != null ? pad.SheetAngle : 0,
                Carriage = magnifier != null ? magnifier.CarriageValue : 0,
                Busy = a != null && a.Busy,
                Commands = sim != null ? sim.Record.commands.Count : -1,
                Rng = sim != null ? sim.S.Rng.State : 0,
            };
        }
    }

    /// <summary>Kabul akışının ses olayları zamanıyla (<see cref="SiginakAdmission.Sounded"/>; olayın karesinin <c>Time.unscaledTime</c>'ı).</summary>
    public sealed class AdmissionSoundLog : IDisposable
    {
        public readonly struct Entry
        {
            public readonly AdmissionSound Sound;
            public readonly float At;
            public readonly int Frame;

            public Entry(AdmissionSound sound, float at, int frame)
            {
                Sound = sound; At = at; Frame = frame;
            }
        }

        public readonly List<Entry> Entries = new List<Entry>();
        readonly SiginakAdmission admission;

        public AdmissionSoundLog(SiginakAdmission flow)
        {
            admission = flow;
            if (admission != null) admission.Sounded += OnSound;
        }

        void OnSound(AdmissionSound s) => Entries.Add(new Entry(s, Time.unscaledTime, Time.frameCount));

        public int Count(AdmissionSound s) => Entries.Count(e => e.Sound == s);

        /// <summary>Sesin ilk olayının zamanı (<paramref name="after"/>'dan sonra); yoksa testi düşürür.</summary>
        public float At(AdmissionSound s, float after = float.NegativeInfinity)
        {
            foreach (var e in Entries) if (e.Sound == s && e.At >= after) return e.At;
            Assert.Fail("ses olayı yok: " + s + " (" + this + ")");
            return 0;
        }

        public void Clear() => Entries.Clear();

        public override string ToString() => string.Join(", ", Entries.Select(e => e.Sound + "@" + e.At.ToString("0.000", CultureInfo.InvariantCulture)));

        public void Dispose()
        {
            if (admission != null) admission.Sounded -= OnSound;
        }
    }
}
