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
    /// <c>IPointerClickHandler</c>'a (<c>pointerClick</c>; damga istasyonun çocuğu olsa da tıkı kendisi alır).
    /// <c>BoardPlayKit.ClickHit</c> eski kuralı (tık yalnız basılan nesneye) uygular; kabul için kullanılmaz.</item>
    /// <item><b>Sürükleme</b> <see cref="PointerGesture"/> ile (sürükleme olayları her adımda <c>pointerDrag</c>'a; GELISTIRME).</item>
    /// <item>Ölçüm hareket durduktan sonra (<see cref="Settle"/>). Tolerans <see cref="BoardPlayKit.FrameTolerance"/>.</item>
    /// <item><b>Ses</b> <c>Sfx.Play</c>'den gözlenemez (Game assembly'si): akışın <see cref="SiginakAdmission.Sounded"/> olayı
    /// (<see cref="AdmissionSoundLog"/>).</item>
    /// </list>
    /// </summary>
    public static class AdmissionPlayKit
    {
        // ---------- Tutamaklar ----------

        public static Simulation Sim => BoardPlayKit.Sim;
        public static GameController Game => BoardPlayKit.Game;
        public static SiginakCameraRig Rig => BoardPlayKit.Rig;
        public static SiginakAdmission Admission => BoardPlayKit.Find<SiginakAdmission>();
        public static AdmissionStation Station => Get<AdmissionStation>("station");
        public static DoorProp Door => Get<DoorProp>("door");
        public static Transform ReadAnchor => Get<Transform>("readAnchor");
        public static StampDecision[] Stamps => Get<StampDecision[]>("stamps") ?? new StampDecision[0];
        public static AdmissionSnapshot Last => Admission != null ? Admission.Last : null;

        /// <summary>İstasyonun tezgâh çarpıştırıcısı (inik tezgâha tık → K8A).</summary>
        public static Collider CounterHit => ShiftPlayKit.Get<Collider>(Station, "counterHit");
        /// <summary>MASA tuşunun çarpıştırıcısı (doğrudan K1).</summary>
        public static Collider DeskKeyHit => ShiftPlayKit.Get<Collider>(Station, "deskKeyHit");
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
        /// Kabul sahneye bağlı (W6 §2.2; KOS aşama 2 ve 7): akış, istasyon (tezgâh, panel, aday görünümü), iki damga (KABUL, GERİ ÇEVİR; her
        /// biri bir kez), kapı, K8R çapası, masanın ve kapı denetleyicisinin <c>admission</c> bağı, K8/K8A/K8R pozları.
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
            Assert.IsNotNull(Station.Candidates, "AdmissionStation: aday grubu görünümü (ConfigureCandidates)");
            Assert.IsNotNull(CounterHit, "AdmissionStation.counterHit");
            Assert.IsNotNull(DeskKeyHit, "AdmissionStation.deskKeyHit (MASA tuşu)");
            Assert.IsNotNull(Door, "SiginakAdmission.door (DoorProp)");
            Assert.IsNotNull(ReadAnchor, "SiginakAdmission.readAnchor (Poses/K8R)");
            Assert.AreEqual(2, Stamps.Count(x => x != null), "iki damga (KABUL, GERİ ÇEVİR; aşama 7)");
            foreach (var c in new[] { StampChoice.Admit, StampChoice.Reject })
            {
                var s = Stamp(c);
                Assert.IsNotNull(s.Hit, c + " damgasının çarpıştırıcısı");
                Assert.AreEqual(1, Stamps.Count(x => x != null && x.Choice == c), c + " damgası bir kez");
            }
            foreach (var pose in new[] { SiginakPose.Door, SiginakPose.Admission, SiginakPose.AdmissionRead })
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
        /// canlı oyunun ilk karelerinde yazılanlarla aynı kayıt (K4-A gözetmeni Boşta'dan; tohum 79'da Victor, Margot, Iris).
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
            Assert.AreEqual(CounterState.Down, Station.Counter, "tezgâh inik (kapıda grup var)");
            ClickCollider(CounterHit, "inik tezgâh (K8)");
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Admission, 3);
            yield return Settle();
        }

        /// <summary>Kamera, istasyon ve damgalar durana dek (en çok <paramref name="timeout"/> s), sonra bir kare.</summary>
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

        // ---------- Damga ----------

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
