using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Where = Corrigible.Desk.ShiftReceiptPaper.Where;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Vardiya kapanışı PlayMode testlerinin ortak düzeni (Revizyon 3 · Aşama 4, R19; PLAN §5.2, E §3.1). <see cref="BoardPlayKit"/>'in
    /// üstüne kurulur: sahne yükleme, kayıt yalıtımı, tıklama ve sanal aygıtlar oradadır.
    /// <list type="bullet">
    /// <item>Tutamaklar <see cref="SiginakShiftClose"/>'un bağlarından yansımayla (sahnede iki <see cref="ShiftSwitch"/> var:
    /// ayarlar dolabının MENÜYE DÖN kolu da; E §0.3). Sahne yeniden yüklenince kendiliğinden yeniden bulunur.</item>
    /// <item><see cref="RequireWired"/>: kapanış sahneye bağlı (W6 sahne adımı) değilse test nedeniyle düşer.</item>
    /// <item>Zaman: olaylar <see cref="ShiftLog"/>'la (olayın karesinin <c>Time.unscaledTime</c>'ı), her kare <see cref="ShiftSample"/>
    /// ile; tolerans <see cref="BoardPlayKit.FrameTolerance"/> (token + iki kare).</item>
    /// <item>İkiz simülasyon (<see cref="Twin"/>) kaydın <b>kopyasından</b> kurulur (<c>Replay.Restore</c> <c>Record</c>'u kendine
    /// atar; aynı nesne verilirse ikizin komutları canlı kayda yazılırdı; E §3.1).</item>
    /// <item>Fiş sürüklemesi ekran noktalarıyla (<see cref="DragSlip"/>): EVRAK hedefi ve ışığı bırakılan ekran noktasının ışınından
    /// çözülür (sentetik olay <c>Mouse.current</c>'ı taşımaz).</item>
    /// </list>
    /// Ses <c>Sfx.Play</c>'den gözlenemez (Game assembly'si; E §3.1): sunumun ses olayları (<see cref="ShiftSound"/>) gözlenir.
    /// </summary>
    public static class ShiftPlayKit
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        static SiginakShiftClose close;
        static ShiftSwitch shiftSwitch;
        static readonly List<IDisposable> logs = new List<IDisposable>();

        // ---------- Tutamaklar ----------

        public static Simulation Sim => BoardPlayKit.Sim;
        public static GameController Game => BoardPlayKit.Game;
        public static SiginakCameraRig Rig => BoardPlayKit.Rig;

        /// <summary>Kartlı oyunun kapanışı (sahnede tek; <c>SiginakRoom/Shift controller</c>).</summary>
        public static SiginakShiftClose Close => close != null ? close : close = BoardPlayKit.Find<SiginakShiftClose>();
        public static SiginakShift Shift => BoardPlayKit.Find<SiginakShift>();
        /// <summary>Odanın vardiya şalteri (kapanışın bağı; yoksa <see cref="SiginakShift"/>'inki).</summary>
        public static ShiftSwitch Switch
        {
            get
            {
                if (shiftSwitch != null) return shiftSwitch;
                var owner = Close != null ? (Object)Close : Shift;
                return shiftSwitch = BoardPlayKit.ShiftSwitchOf(owner);
            }
        }
        public static ShiftReceiptMachine Machine => Get<ShiftReceiptMachine>(Close, "machine");
        public static ShiftReceiptPaper Paper => Machine != null ? Machine.Paper : null;
        public static EvrakTray Tray => Get<EvrakTray>(Close, "tray");
        public static CanvasGroup Fade => Get<CanvasGroup>(Close, "fade");
        public static TelexFeed Telex => Get<TelexFeed>(Close, "telex");
        public static Teleprinter Printer => Get<Teleprinter>(Telex, "printer");
        public static ShiftPlate Plate => Switch != null ? Switch.Plate : null;
        public static CounterBank Counters => Get<CounterBank>(BoardPlayKit.Desk, "counters");
        public static CounterModule ShiftCounter => Counters != null ? Counters[CounterBank.Slot.Shift] : null;
        /// <summary>EVRAK tepsisinin tıklanır hedefi (<c>SiginakDesk.trayTarget</c>).</summary>
        public static DeskTarget TrayTarget => Get<DeskTarget>(BoardPlayKit.Desk, "trayTarget");
        /// <summary>EVRAK'ın bırakma alanı (<c>EvrakTray.receiptDropArea</c>; bırakış ve hedef ışığı bunun ışınıyla).</summary>
        public static Collider DropArea => Get<Collider>(Tray, "receiptDropArea");
        /// <summary>Tepsideki dosyalanmış fişin kopyası (Karar D7-A).</summary>
        public static HandoverSlipFace FiledSlip => Get<HandoverSlipFace>(Tray, "filedSlip");
        /// <summary>Fişin ekranı (kâğıdın kamerası; makinenin bırakma ölçütü de bu kamerayla).</summary>
        public static Camera Cam => Paper != null && Paper.View != null ? Paper.View : BoardPlayKit.View;

        /// <summary>Özel alan (yansıma; temel sınıflar da aranır). Sahip ya da alan yoksa varsayılan.</summary>
        public static T Get<T>(object owner, string name)
        {
            if (owner == null || owner is Object o && o == null) return default;
            for (var type = owner.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, Members);
                if (field != null) return field.GetValue(owner) is T value ? value : default;
            }
            return default;
        }

        static void Forget()
        {
            close = null;
            shiftSwitch = null;
        }

        // ---------- Kurulum ve söküm ----------

        /// <summary>
        /// DeskGame (tohum 79), kapanış bağlı mı denetlenir; <paramref name="passBriefing"/> false ise emir belgesi açık kalır (P10).
        /// Bittiğinde (belge geçildiyse) kamera K1'de, şalter hazır, fiş gizli.
        /// </summary>
        public static IEnumerator Begin(bool passBriefing = true)
        {
            Forget();
            yield return BoardPlayKit.LoadDesk(BoardPlayKit.DefaultSeed, passBriefing);
            RequireWired();
            if (!passBriefing) yield break;
            // Şalter UpdateLock'u bir kare sonra yazar
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(ShiftPhase.Ready, Switch.Phase, "açılışta şalter hazır (" + Switch.LockPlate + " / " + Switch.LockLine + ")");
            Assert.AreEqual(Where.Hidden, Paper.State, "açılışta fiş gizli");
            Assert.IsFalse(Sim.Board.Printed, "açılışta fiş basılmamış");
            BoardPlayKit.ResetFrameStats();
        }

        /// <summary>Aynı test klasöründen KAYITTAN DEVAM (<see cref="BoardPlayKit.ContinueDesk"/>); tutamaklar yeni sahneden.</summary>
        public static IEnumerator Continue()
        {
            DisposeLogs();
            Forget();
            yield return BoardPlayKit.ContinueDesk();
            Forget();
            RequireWired();
        }

        /// <summary>
        /// Söküm (<c>[UnityTearDown]</c>): günlükler bırakılır, süren bir baskın (P9) doğrudan çözülür (söküm sırasında bir
        /// <c>Changed</c> baskın sahnesini yüklemesin), sonra <see cref="BoardPlayKit.TearDown"/>.
        /// </summary>
        public static IEnumerator End()
        {
            DisposeLogs();
            if (Sim != null && Sim.S.Raid != null) ResolveRaidDirect();
            Forget();
            yield return BoardPlayKit.TearDown();
        }

        static void DisposeLogs()
        {
            foreach (var log in logs) log.Dispose();
            logs.Clear();
        }

        /// <summary>Yeni günlük (söküm bırakır).</summary>
        public static ShiftLog Log()
        {
            var log = new ShiftLog();
            logs.Add(log);
            return log;
        }

        /// <summary>
        /// Kartlı kapanış sahneye bağlı (W6 sahne adımı, PLAN §2.2): kapanış, köprülerin bağları, R19 kipli oda şalteri ve plakası,
        /// makine, kâğıt (fiş yüzüyle), tepsi, kararma, teleks ve K6 pozu.
        /// </summary>
        public static void RequireWired()
        {
            Assert.IsNotNull(Close, "SiginakShiftClose sahnede yok (W6: SiginakRoom/Shift controller)");
            Assert.AreSame(Close, Get<SiginakShiftClose>(Shift, "close"), "SiginakShift.close → SiginakShiftClose (W6)");
            Assert.AreSame(Close, Get<SiginakShiftClose>(BoardPlayKit.Desk, "shiftClose"), "SiginakDesk.shiftClose → SiginakShiftClose (W6)");
            var sw = Switch;
            Assert.IsNotNull(sw, "SiginakShiftClose.shiftSwitch");
            Assert.AreSame(sw, BoardPlayKit.ShiftSwitchOf(Shift), "kapanışın şalteri = SiginakShift'inki (ayarlar dolabının kolu değil)");
            Assert.IsTrue(sw.ShiftCloseMode, "oda şalteri R19 kipinde (shiftClose = true; W6)");
            Assert.IsNotNull(sw.Plate, "ShiftSwitch.plate → ShiftPlate (W6)");
            Assert.IsNotNull(sw.Target, "şalterin DeskTarget'ı");
            Assert.IsNotNull(Machine, "SiginakShiftClose.machine");
            Assert.IsNotNull(Paper, "ShiftReceiptMachine.paper");
            Assert.IsNotNull(Get<HandoverSlipFace>(Paper, "face"), "ShiftReceiptPaper.face → HandoverSlipView (W6)");
            Assert.IsNotNull(Tray, "SiginakShiftClose.tray");
            Assert.IsNotNull(DropArea, "EvrakTray.receiptDropArea");
            Assert.IsNotNull(Fade, "SiginakShiftClose.fade (Planning shift blackout)");
            Assert.IsNotNull(Telex, "SiginakShiftClose.telex");
            Assert.IsNotNull(Cam, "fişin kamerası");
            Assert.IsTrue(Rig.TryGetPose(SiginakPose.ShiftClose, out _), "K6 pozu rig'de (Poses/K6; W6)");
        }

        /// <summary>Tema token'ı; PLAN'daki sayıyla da karşılaştırılır (token değişirse plan da güncellenmeli).</summary>
        public static float Token(string key, float plan)
        {
            var theme = SiginakTheme.Active;
            Assert.IsNotNull(theme, "SiginakTheme.Active");
            Assert.IsTrue(theme.TryNumber(key, out var value), "token " + key);
            Assert.AreEqual(plan, value, 1e-4f, key + " (PLAN)");
            return value;
        }

        /// <summary>Temada yoksa yedek (PLAN §1.12: <c>motion.board.denyShakeS</c>, <c>motion.tooltipDelayS</c> temada yok).</summary>
        public static float TokenOr(string key, float fallback)
        {
            var theme = SiginakTheme.Active;
            return theme != null ? theme.NumberOf(key, fallback) : fallback;
        }

        // ---------- Durum ----------

        /// <summary>(komut sayısı, RNG durumu) şimdi.</summary>
        public static (int commands, uint rng) Mark() => BoardPlayKit.Snapshot();

        /// <summary>İşaretten bu yana komut yazılmadı ve RNG çekilmedi.</summary>
        public static void AssertUnchanged((int commands, uint rng) mark, string context)
        {
            var now = Mark();
            Assert.AreEqual(mark.commands, now.commands, context + ": komut kaydedilmemeli (" + Describe(Since(mark.commands)) + ")");
            Assert.AreEqual(mark.rng, now.rng, context + ": RNG durumu değişmemeli");
        }

        /// <summary><paramref name="count"/>. komuttan sonraki komutlar.</summary>
        public static List<SavedCommand> Since(int count) => Sim.Record.commands.Skip(count).ToList();

        public static string Describe(IEnumerable<SavedCommand> commands) =>
            string.Join(", ", commands.Select(c => c.day + ":" + c.kind + "(" + c.a + "," + c.b + ")"));

        /// <summary>Komut geçmişi (<c>gün:tür:a:b</c>), birebir karşılaştırma için.</summary>
        public static List<string> CommandLines(GameRecord record) =>
            record.commands.Select(c => c.day.ToString(CultureInfo.InvariantCulture) + ":" + c.kind + ":" + c.a + ":" + c.b).ToList();

        /// <summary>İkiz: kaydın kopyasından yeniden oynatılmış simülasyon (E §3.1; kopya şart).</summary>
        public static Simulation Twin()
        {
            var copy = JsonUtility.FromJson<GameRecord>(JsonUtility.ToJson(Sim.Record));
            copy.day = Sim.S.Day;
            var twin = Replay.Restore(copy);
            Assert.AreNotSame(Sim.Record, twin.Record, "ikiz canlı kaydı paylaşmamalı");
            return twin;
        }

        /// <summary>Gün, RNG, erzak ve kişilerin (kimlik, görev, can, yatış, canlı) parmak izi.</summary>
        public static string Fingerprint(Simulation sim)
        {
            var inv = CultureInfo.InvariantCulture;
            var people = sim.Board != null
                ? string.Join(";", sim.Board.People.Select(p => p.Id + ":" + p.Job + ":" + p.Hp.ToString(inv) + ":" + p.Down.ToString(inv) + ":" + (p.Alive ? 1 : 0)))
                : "";
            return "gün " + sim.S.Day.ToString(inv) + " · rng " + sim.S.Rng.State.ToString(inv) + " · erzak " + sim.Shelter.Supplies.ToString("R", inv) + " · " + people;
        }

        /// <summary>
        /// <c>GameController.Changed</c>'i yayınlar (herhangi bir komutun bildirimiyle aynı; komut yazmaz). Sunum fikstüründen sonra
        /// gerçek görüntüye dönmek için (P5).
        /// </summary>
        public static void RaiseChanged()
        {
            var field = typeof(GameController).GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "GameController.Changed olayının alanı");
            (field.GetValue(Game) as Action)?.Invoke();
        }

        // ---------- Şalter ----------

        /// <summary>Hazır lambası yanık (<c>readyLamp.sharedMaterial == lampOn</c>).</summary>
        public static bool ReadyLampLit
        {
            get
            {
                var lamp = Get<Renderer>(Switch, "readyLamp");
                return lamp != null && lamp.sharedMaterial == Get<Material>(Switch, "lampOn");
            }
        }

        /// <summary>Kilit bandı (sarı-siyah KİLİTLİ) görünür.</summary>
        public static bool LockBandShown
        {
            get
            {
                var band = Get<GameObject>(Switch, "lockBand");
                return band != null && band.activeSelf;
            }
        }

        /// <summary>Kapağın ön kenarının yuvasından kalkışı (mm): <c>coverRadiusM · tan(açı)</c>.</summary>
        public static float CoverEdgeMm(float deg)
        {
            float radius = Get<float>(Switch, "coverRadiusM");
            if (radius <= 0) radius = 0.159f;
            return radius * Mathf.Tan(deg * Mathf.Deg2Rad) * 1000f;
        }

        /// <summary>Şalterin DeskTarget'ına sol tık (DeskTarget.Blocked ve Interactable'a uyar).</summary>
        public static void ClickSwitch() => BoardPlayKit.Click(Switch.Target.gameObject);

        // ---------- Fiş ----------

        public static Vector2 ScreenOf(Vector3 world)
        {
            var p = Cam.WorldToScreenPoint(world);
            return new Vector2(p.x, p.y);
        }

        /// <summary>Fişin dil pozuna uzaklığı (mm).</summary>
        public static float FromTongueMm => Vector3.Distance(Paper.transform.position, Paper.TongueTarget.position) * 1000f;

        /// <summary>Fişin ekrandaki yüksekliği / ekran yüksekliği (üst ve alt kenarın ortası izdüşürülür; düzlem içi eğimden bağımsız).</summary>
        public static float SlipHeightFraction()
        {
            float half = TokenOr("shiftClose.slip.sizeMm[1]", 150) * 0.0005f;
            var t = Paper.transform;
            var cam = Cam;
            Vector3 top = cam.WorldToScreenPoint(t.TransformPoint(new Vector3(0, half, 0)));
            Vector3 bottom = cam.WorldToScreenPoint(t.TransformPoint(new Vector3(0, -half, 0)));
            return Vector2.Distance(top, bottom) / Mathf.Max(1, cam.pixelHeight);
        }

        /// <summary>Fişin dört köşesi kadrajda.</summary>
        public static bool SlipInFrame()
        {
            var size = new Vector2(TokenOr("shiftClose.slip.sizeMm[0]", 100), TokenOr("shiftClose.slip.sizeMm[1]", 150)) * 0.0005f;
            var t = Paper.transform;
            var cam = Cam;
            foreach (var c in new[] { new Vector3(-size.x, -size.y), new Vector3(size.x, -size.y), new Vector3(-size.x, size.y), new Vector3(size.x, size.y) })
            {
                var p = cam.WorldToScreenPoint(t.TransformPoint(c));
                if (p.z <= cam.nearClipPlane || p.x < 0 || p.x > cam.pixelWidth || p.y < 0 || p.y > cam.pixelHeight) return false;
            }
            return true;
        }

        /// <summary>Fişe sürüklemesiz sol tık (kâğıdın IPointerClickHandler'ı; anlamını kapanış verir).</summary>
        public static void ClickSlip() => BoardPlayKit.Click(Paper.gameObject);

        /// <summary>Ekran noktası EVRAK'ın bırakma alanının üstünde (makinenin ölçütü).</summary>
        public static bool OverTray(Vector2 screen) => Tray.ContainsReceiptDrop(screen, Cam);

        static bool OnScreen(Vector2 p, float margin = 0.04f)
        {
            var cam = Cam;
            return p.x > cam.pixelWidth * margin && p.x < cam.pixelWidth * (1 - margin) && p.y > cam.pixelHeight * margin && p.y < cam.pixelHeight * (1 - margin);
        }

        /// <summary>EVRAK'ın bırakma alanının üstünde bir ekran noktası (alanın merkezi, yoksa üst yüzü).</summary>
        public static Vector2 TrayScreen()
        {
            var area = DropArea;
            Assert.IsNotNull(area, "EvrakTray.receiptDropArea");
            var b = area.bounds;
            foreach (var world in new[] { b.center, b.center + Vector3.up * b.extents.y * 0.9f, b.center + Vector3.up * b.extents.y * 0.5f })
            {
                var p = Cam.WorldToScreenPoint(world);
                var s = new Vector2(p.x, p.y);
                if (p.z > 0 && OnScreen(s, 0) && OverTray(s)) return s;
            }
            Assert.Fail("EVRAK'ın bırakma alanı kadrajda değil (kamera " + Rig.Current + ")");
            return default;
        }

        /// <summary>
        /// EVRAK dışında, <paramref name="from"/>'dan düz çizgiyle <paramref name="frames"/> adımda giderken hiçbir adımı tepsinin üstüne
        /// düşmeyen bir ekran noktası (sürükleme yolunda hedef ışığı yanmasın; masaya düşen ışın).
        /// </summary>
        public static Vector2 OutsidePoint(Vector2 from, int frames)
        {
            var cam = Cam;
            float w = cam.pixelWidth, h = cam.pixelHeight;
            var offsets = new[]
            {
                new Vector2(0, 0.22f * h), new Vector2(-0.22f * w, 0), new Vector2(0.22f * w, 0), new Vector2(0, -0.2f * h),
                new Vector2(-0.18f * w, 0.18f * h), new Vector2(0.18f * w, 0.18f * h), new Vector2(-0.18f * w, -0.18f * h), new Vector2(0.18f * w, -0.18f * h),
            };
            foreach (var offset in offsets)
            {
                var to = from + offset;
                if (!OnScreen(to)) continue;
                bool clear = true;
                for (int i = 0; i <= frames && clear; i++) clear = !OverTray(Vector2.Lerp(from, to, i / (float)Mathf.Max(1, frames)));
                if (clear && cam.ScreenPointToRay(to).direction.y < 0) return to;
            }
            Assert.Fail("fişin sürüklenebileceği EVRAK dışı bir ekran noktası bulunamadı");
            return default;
        }

        /// <summary>
        /// Fişi sürükler (EventSystem sırası: giriş, basış, olası sürükleme; ilk adımda sürükleme başlar, her karede sürükleme): bulunduğu
        /// yerden <paramref name="legs"/> noktalarına sırayla, her biri <paramref name="framesPerLeg"/> karede; son noktada
        /// <paramref name="holdFrames"/> kare bekler, sonra bırakır (bırakış, sürükleme sonu, çıkış). <paramref name="onFrame"/> her
        /// karede (o anki bacak; bekleme <c>legs.Count</c>) çağrılır.
        /// </summary>
        public static IEnumerator DragSlip(IReadOnlyList<Vector2> legs, int framesPerLeg, int holdFrames, Action<int> onFrame = null)
        {
            var go = Paper.gameObject;
            var from = ScreenOf(go.transform.position);
            var ped = BoardPlayKit.Pointer(go, from);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.initializePotentialDrag);
            yield return null;
            BoardPlayKit.TrackFrame();
            framesPerLeg = Mathf.Max(1, framesPerLeg);
            var last = from;
            bool begun = false;
            for (int leg = 0; leg < legs.Count; leg++)
            {
                for (int i = 1; i <= framesPerLeg; i++)
                {
                    var p = Vector2.Lerp(last, legs[leg], i / (float)framesPerLeg);
                    ped.delta = p - ped.position;
                    ped.position = p;
                    if (!begun)
                    {
                        begun = true;
                        ped.dragging = true;
                        ped.pointerDrag = go;
                        ped.eligibleForClick = false;
                        ExecuteEvents.Execute(go, ped, ExecuteEvents.beginDragHandler);
                    }
                    ExecuteEvents.Execute(go, ped, ExecuteEvents.dragHandler);
                    yield return null;
                    BoardPlayKit.TrackFrame();
                    onFrame?.Invoke(leg);
                }
                last = legs[leg];
            }
            for (int i = 0; i < holdFrames; i++)
            {
                ped.delta = Vector2.zero;
                ExecuteEvents.Execute(go, ped, ExecuteEvents.dragHandler);
                yield return null;
                BoardPlayKit.TrackFrame();
                onFrame?.Invoke(legs.Count);
            }
            ped.delta = Vector2.zero;
            ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.endDragHandler);
            ped.dragging = false;
            ped.pointerDrag = null;
            ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerExitHandler);
        }

        // ---------- Akışlar ----------

        /// <summary>Kamera <paramref name="pose"/>'a vardı (geçiş bitti).</summary>
        public static IEnumerator WaitForPose(SiginakPose pose, float timeout = 2)
        {
            yield return BoardPlayKit.WaitFor(() => Rig.Current == pose && !Rig.InTransition, timeout, "kamera " + pose + " pozuna varmadı (" + Rig.Current + ")");
        }

        /// <summary>
        /// P1'in yolu: şaltere tık (kapak), kapak açılınca tık (kol: <c>board_print</c>); fiş kol inince çıkar ve dile oturur, kamera
        /// K6'ya varır. Bittiğinde fiş dilde, şalter fiş bekliyor.
        /// </summary>
        public static IEnumerator PrintToTongue()
        {
            var sw = Switch;
            Assert.AreEqual(ShiftPhase.Ready, sw.Phase, "basımdan önce şalter hazır (" + sw.LockPlate + ")");
            ClickSwitch();
            Assert.AreEqual(ShiftPhase.CoverUp, sw.Phase, "1. tık kapağı kaldırır");
            yield return BoardPlayKit.WaitFor(() => !sw.Moving, 2, "kapak açılışı bitmedi");
            ClickSwitch();
            Assert.IsTrue(Sim.Board.Printed, "2. tık (kol) fişi basar (board_print)");
            yield return BoardPlayKit.WaitFor(() => Paper.State == Where.Tongue && !Paper.FeedPending, 3, "fiş dile çıkmadı (" + Paper.State + ")");
            yield return WaitForPose(SiginakPose.ShiftClose);
            yield return BoardPlayKit.WaitFor(() => !sw.Moving, 1, "şalter durmadı");
            Assert.AreEqual(ShiftPhase.Waiting, sw.Phase, "fiş basılı: şalter fiş bekliyor");
        }

        /// <summary>Dildeki fişe tık: ele gelir (0,25 s); kamera K6'da değilse önce K6'ya gider.</summary>
        public static IEnumerator TakeToHand()
        {
            ClickSlip();
            yield return BoardPlayKit.WaitFor(() => Paper.State == Where.Hand, 2, "fiş ele gelmedi (" + Paper.State + ")");
            yield return WaitForPose(SiginakPose.ShiftClose);
        }

        /// <summary>
        /// Fişi bulunduğu yerden EVRAK'a sürükleyip bırakır ve dosyalamanın (kararma, gün, şalter) bitmesini bekler. Gün tam bir kez
        /// ilerlemiş olmalı.
        /// </summary>
        public static IEnumerator FileOnEvrak(List<ShiftSample> samples = null)
        {
            int day = Sim.S.Day;
            yield return DragSlip(new[] { TrayScreen() }, 8, 2);
            Assert.IsTrue(Close.Filing, "EVRAK'a bırakılan geçerli fiş dosyalanır (SiginakShiftClose.Filing)");
            yield return WatchUntil(samples, () => !Close.Filing && !Switch.Moving, 3, "dosyalama bitmedi");
            Assert.AreEqual(day + 1, Sim.S.Day, "EVRAK'a bırakış günü tam bir kez ilerletir");
        }

        // ---------- Baskın (P9: yapay sunum durumu) ----------

        /// <summary>
        /// Sıradaki sektöre baskın emri doğrudan simülasyona (GameController'sız: <c>Changed</c> yok, baskın sahnesi yüklenmez;
        /// GELISTIRME:163–165). Komut kayda girer.
        /// </summary>
        public static ActResult StartRaidDirect()
        {
            var sim = Sim;
            var next = sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            if (next == null) return ActResult.Fail("sıradaki sektör yok");
            return sim.StartLiberationRaid(next.Id);
        }

        /// <summary>Süren baskın doğrudan, kayıpsız başarıyla çözülür; operasyon raporu okunmuş sayılır (masa kilitlenmesin).</summary>
        public static void ResolveRaidDirect()
        {
            var sim = Sim;
            if (sim?.S.Raid == null) return;
            var squad = sim.S.Raid.Params.Squad;
            var result = new RaidResult { Success = true, Turns = 6 };
            foreach (var slot in squad) { result.Hp.Add(slot.Hp); result.Armor.Add(slot.Armor); }
            var r = sim.ResolveRaid(result);
            Assert.IsTrue(r.Ok, "baskın çözülemedi: " + r.Msg);
            if (BoardPlayKit.Surface != null) BoardPlayKit.Surface.MarkRaidReportSeen();
        }

        // ---------- Kare örnekleri ----------

        /// <summary><paramref name="seconds"/> boyunca her karede örnek alır (<paramref name="into"/> boşsa yalnız bekler).</summary>
        public static IEnumerator Watch(List<ShiftSample> into, float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
            {
                yield return null;
                BoardPlayKit.TrackFrame();
                into?.Add(ShiftSample.Now());
            }
        }

        /// <summary>Koşul doğru olana dek her karede örnek alır; <paramref name="timeout"/> saniyede olmazsa testi düşürür.</summary>
        public static IEnumerator WatchUntil(List<ShiftSample> into, Func<bool> condition, float timeout, string what)
        {
            float until = Time.unscaledTime + timeout;
            while (!condition())
            {
                if (Time.unscaledTime > until) Assert.Fail("zaman aşımı (" + timeout + " s): " + what);
                yield return null;
                BoardPlayKit.TrackFrame();
                into?.Add(ShiftSample.Now());
            }
        }

        /// <summary>VARDİYA sayacının çarklarından biri dönüyor.</summary>
        public static bool ShiftCounterRolling
        {
            get
            {
                var wheels = Get<CounterWheel[]>(ShiftCounter, "wheels");
                return wheels != null && wheels.Any(w => w != null && w.Rolling);
            }
        }

        /// <summary>Teleksin en yeni satırı hâlâ basılıyor.</summary>
        public static bool TelexTyping => Get<bool>(Printer, "typing");
        /// <summary>Basılan satırın toplam süresi (≤ 0: harf başına token).</summary>
        public static float TelexLineSeconds => Get<float>(Printer, "typeSeconds");

        /// <summary>Teleksin son satırı.</summary>
        public static TelexLine LastTelex
        {
            get
            {
                var lines = Telex.Lines;
                Assert.Greater(lines.Count, 0, "teleks satırı yok");
                return lines[lines.Count - 1];
            }
        }
    }

    /// <summary>Kapanışın bir karesi (test kare döngüsünde, Update'lerden sonra alınır).</summary>
    public struct ShiftSample
    {
        public float At;
        public int Frame;
        public float Cover, Lever;
        public bool Moving;
        public ShiftPhase Phase;
        public Where Slip;
        public Vector3 SlipPosition;
        public bool FeedPending;
        public float Alpha;
        public bool Blocks;
        public int Day, Commands;
        public uint Rng;
        public bool Printed;
        public bool TargetLit, LightOn, MouthLit;
        public double ShiftCounter;
        public bool ShiftRolling, Typing;
        public SiginakPose Pose;

        public static ShiftSample Now()
        {
            var sw = ShiftPlayKit.Switch;
            var paper = ShiftPlayKit.Paper;
            var fade = ShiftPlayKit.Fade;
            var tray = ShiftPlayKit.Tray;
            var sim = ShiftPlayKit.Sim;
            var light = tray != null ? tray.TargetLight : null;
            var counter = ShiftPlayKit.ShiftCounter;
            return new ShiftSample
            {
                At = Time.unscaledTime,
                Frame = Time.frameCount,
                Cover = sw != null ? sw.CoverDeg : 0,
                Lever = sw != null ? sw.LeverDeg : 0,
                Moving = sw != null && sw.Moving,
                Phase = sw != null ? sw.Phase : ShiftPhase.Ready,
                Slip = paper != null ? paper.State : Where.Hidden,
                SlipPosition = paper != null ? paper.transform.position : Vector3.zero,
                FeedPending = paper != null && paper.FeedPending,
                Alpha = fade != null ? fade.alpha : 0,
                Blocks = fade != null && fade.blocksRaycasts,
                Day = sim != null ? sim.S.Day : -1,
                Commands = sim != null ? sim.Record.commands.Count : -1,
                Rng = sim != null ? sim.S.Rng.State : 0,
                Printed = sim?.Board != null && sim.Board.Printed,
                TargetLit = tray != null && tray.ReceiptTargetLit,
                LightOn = light != null && light.enabled,
                MouthLit = ShiftPlayKit.Machine != null && ShiftPlayKit.Machine.LampLit,
                ShiftCounter = counter != null ? counter.Value : -1,
                ShiftRolling = ShiftPlayKit.ShiftCounterRolling,
                Typing = ShiftPlayKit.TelexTyping,
                Pose = ShiftPlayKit.Rig != null ? ShiftPlayKit.Rig.Current : SiginakPose.Desk,
            };
        }
    }

    /// <summary>
    /// Kapanışın olayları zamanıyla (olayın karesinin <c>Time.unscaledTime</c>'ı): şalter (Armed, Thrown, Cancelled, Denied, Phase,
    /// Lever, SwitchSound), fiş (Slip = yer, SlipSound), makine (Filed), teleks (Telex = durum), oyun (Changed = gün), kamera (Pose,
    /// Arrived, Back). Günün değiştiği <c>Changed</c>'de (dosyalamanın <c>FileBoardShift</c>'i, aynı çağrının içinde) kararmanın
    /// alfası, simülasyonun parmak izi ve VARDİYA sayacı yakalanır: kare sırasından bağımsız kesin ölçü.
    /// </summary>
    public sealed class ShiftLog : IDisposable
    {
        public readonly struct Entry
        {
            public readonly string Name, Value;
            public readonly float At;
            public readonly int Frame;

            public Entry(string name, string value, float at, int frame)
            {
                Name = name; Value = value; At = at; Frame = frame;
            }

            public override string ToString() => Name + (Value != null ? "=" + Value : "") + "@" + At.ToString("0.000", CultureInfo.InvariantCulture);
        }

        public readonly List<Entry> Entries = new List<Entry>();
        readonly List<(string status, float seconds)> telexSeconds = new List<(string, float)>();

        /// <summary>Durumu <paramref name="status"/> olan ilk teleks satırının basılış süresi (yayın anında; ≤ 0 harf başına token).</summary>
        public float TelexSeconds(string status)
        {
            foreach (var (s, seconds) in telexSeconds) if (s == status) return seconds;
            Assert.Fail("teleks satırı yok: " + status + " (günlük: " + this + ")");
            return 0;
        }

        /// <summary>Günlük açılırken gün.</summary>
        public readonly int DayBefore;
        public bool DayChanged { get; private set; }
        public float DayChangedAt { get; private set; }
        public int DayChangedFrame { get; private set; }
        public float AlphaAtDayChange { get; private set; }
        public bool BlocksAtDayChange { get; private set; }
        public double ShiftCounterAtDayChange { get; private set; }
        public string FingerprintAtDayChange { get; private set; }

        readonly ShiftSwitch sw;
        readonly ShiftReceiptPaper paper;
        readonly ShiftReceiptMachine machine;
        readonly TelexFeed telex;
        readonly GameController game;
        readonly SiginakCameraRig rig;
        readonly Action armed, thrown, cancelled, denied, changed;
        readonly Action<ShiftPhase> phase;
        readonly Action<bool> lever;
        readonly Action<ShiftSound> switchSound, slipSound;
        readonly Action<Where> slip;
        readonly Action<string> filed;
        readonly Action<TelexLine> posted;
        readonly Action<SiginakPose> pose, arrived, back;

        public ShiftLog()
        {
            sw = ShiftPlayKit.Switch;
            paper = ShiftPlayKit.Paper;
            machine = ShiftPlayKit.Machine;
            telex = ShiftPlayKit.Telex;
            game = ShiftPlayKit.Game;
            rig = ShiftPlayKit.Rig;
            DayBefore = ShiftPlayKit.Sim.S.Day;

            armed = () => Add("Armed", null);
            thrown = () => Add("Thrown", null);
            cancelled = () => Add("Cancelled", null);
            denied = () => Add("Denied", null);
            phase = p => Add("Phase", p.ToString());
            lever = down => Add("Lever", down ? "down" : "up");
            switchSound = s => Add("SwitchSound", s.ToString());
            slipSound = s => Add("SlipSound", s.ToString());
            slip = w => Add("Slip", w.ToString());
            filed = id => Add("Filed", id);
            posted = line =>
            {
                Add("Telex", line.status);
                // Satırın basılış süresi yayın anında (sonraki bir satır Teleprinter'ın süresini değiştirir)
                telexSeconds.Add((line.status, ShiftPlayKit.TelexLineSeconds));
            };
            pose = p => Add("Pose", p.ToString());
            arrived = p => Add("Arrived", p.ToString());
            back = p => Add("Back", p.ToString());
            changed = OnChanged;

            if (sw != null)
            {
                sw.Armed += armed; sw.Thrown += thrown; sw.Cancelled += cancelled; sw.Denied += denied;
                sw.PhaseChanged += phase; sw.LeverArrived += lever; sw.Sound += switchSound;
            }
            if (paper != null) { paper.StateChanged += slip; paper.Sound += slipSound; }
            if (machine != null) machine.ReceiptFiled += filed;
            if (telex != null) telex.Posted += posted;
            if (game != null) game.Changed += changed;
            if (rig != null) { rig.PoseChanged += pose; rig.Arrived += arrived; rig.BackClicked += back; }
        }

        void Add(string name, string value) => Entries.Add(new Entry(name, value, Time.unscaledTime, Time.frameCount));

        void OnChanged()
        {
            var sim = game != null ? game.Sim : null;
            if (sim == null) return;
            Add("Changed", sim.S.Day.ToString(CultureInfo.InvariantCulture));
            if (DayChanged || sim.S.Day == DayBefore) return;
            DayChanged = true;
            DayChangedAt = Time.unscaledTime;
            DayChangedFrame = Time.frameCount;
            var fade = ShiftPlayKit.Fade;
            AlphaAtDayChange = fade != null ? fade.alpha : -1;
            BlocksAtDayChange = fade != null && fade.blocksRaycasts;
            var counter = ShiftPlayKit.ShiftCounter;
            ShiftCounterAtDayChange = counter != null ? counter.Value : -1;
            FingerprintAtDayChange = ShiftPlayKit.Fingerprint(sim);
        }

        public int Count(string name, string value = null) => Entries.Count(e => e.Name == name && (value == null || e.Value == value));

        /// <summary>İlk eşleşen olayın zamanı (<paramref name="after"/> ve sonrası); yoksa null.</summary>
        public float? First(string name, string value = null, float after = float.NegativeInfinity)
        {
            foreach (var e in Entries)
                if (e.Name == name && (value == null || e.Value == value) && e.At >= after) return e.At;
            return null;
        }

        /// <summary>İlk eşleşen olayın zamanı; yoksa testi düşürür.</summary>
        public float At(string name, string value = null, float after = float.NegativeInfinity)
        {
            var at = First(name, value, after);
            Assert.IsTrue(at.HasValue, "olay yok: " + name + (value != null ? "=" + value : "") + " (günlük: " + this + ")");
            return at.Value;
        }

        public override string ToString() => string.Join(", ", Entries.Select(e => e.ToString()));

        public void Dispose()
        {
            if (sw != null)
            {
                sw.Armed -= armed; sw.Thrown -= thrown; sw.Cancelled -= cancelled; sw.Denied -= denied;
                sw.PhaseChanged -= phase; sw.LeverArrived -= lever; sw.Sound -= switchSound;
            }
            if (paper != null) { paper.StateChanged -= slip; paper.Sound -= slipSound; }
            if (machine != null) machine.ReceiptFiled -= filed;
            if (telex != null) telex.Posted -= posted;
            if (game != null) game.Changed -= changed;
            if (rig != null) { rig.PoseChanged -= pose; rig.Arrived -= arrived; rig.BackClicked -= back; }
        }
    }
}
