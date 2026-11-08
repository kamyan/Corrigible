using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Kartlı pano PlayMode testlerinin ortak düzeni (Revizyon 3 Aşama 3, E §5.2). <see cref="LoadDesk"/> kayıtları geçici bir
    /// klasöre ayırır (<c>SessionState "Corrigible.TestStorage"</c>, sahne yüklenmeden önce), DeskGame'i yükler, verilen
    /// tohumla yeni harekât açar, emir belgesini BAŞLA ile geçer, kamerayı K1'e oturtur ve <c>RetireJobs</c>'un komutlarını
    /// bekler; sonra komut sayısını ve RNG durumunu <see cref="Baseline"/>'a yazar. <see cref="TearDown"/> önce DeskGame'i
    /// boşaltır (kapanışta yazılan dosyalar hâlâ test klasörüne gider; oyuncunun gerçek kaydı hiç açılmaz), sonra klasörü
    /// siler, önceki SessionState değerini ve dili geri yükler.
    /// Tıklama ve sürükleme <see cref="ExecuteEvents"/> ile doğrudan nesneye gider (ışın yok); klavye ve sağ tuş, odanın
    /// kendisi <c>Keyboard.current</c> / <c>Mouse.current</c> okuduğu için testin eklediği sanal aygıtlarla sürülür
    /// (Game görünümü odakta değilken de: Input System ayarı test boyunca "girdi her zaman oyuna").
    /// Yalnız editörde koşar: kayıt klasörünün ayrılması editör API'sidir (<c>SaveStore.StorageDirectory</c>).
    /// Revizyon 3 · Aşama 4: <see cref="LoadDesk"/> emir belgesini açık bırakabilir (<c>passBriefing: false</c>),
    /// <see cref="ContinueDesk"/> aynı klasörden KAYITTAN DEVAM eder, <see cref="ShiftSwitchOf"/> odanın şalterini verir
    /// (vardiya kapanışının kendi yardımcıları <c>ShiftPlayKit</c>'te).
    /// Revizyon 3 · Aşama 5: <see cref="Book"/> (tarif defteri), gerçek ışın (<see cref="RaycastAll"/>, <see cref="FirstHit"/>;
    /// örtülme sınanır) ve EventSystem'in hiyerarşi kuralıyla tık (<see cref="ClickHit"/>) ile bırakmadan tutulan sürükleme
    /// (<see cref="Press"/>, <see cref="PressOnly"/> → <see cref="PointerGesture"/>); panonun ses olayları <see cref="BoardSoundLog"/>.
    /// </summary>
    public static class BoardPlayKit
    {
        public const string StorageKey = "Corrigible.TestStorage";
        public const string DeskScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        /// <summary>Varsayılan harekât tohumu (EditMode pano testleriyle aynı).</summary>
        public const uint DefaultSeed = 79;

        public static GameController Game { get; private set; }
        public static SiginakCameraRig Rig { get; private set; }
        public static LiberationDeskView Surface { get; private set; }
        public static SiginakDesk Desk { get; private set; }
        public static PlanningBoardPresenter Presenter { get; private set; }
        public static SiginakBriefing Briefing { get; private set; }
        public static EventSystem Events { get; private set; }
        public static Camera View => Rig != null ? Rig.View : null;
        public static Simulation Sim => Game != null ? Game.Sim : null;
        /// <summary>Kartlı pano (W4); sahnede yoksa null.</summary>
        public static BoardView3D Board => Find<BoardView3D>();
        /// <summary>Komut sayısı ve RNG durumu, açılış komutlarından (board_auto, emekli görevler) sonra.</summary>
        public static (int commands, uint rng) Baseline { get; private set; }
        /// <summary><see cref="ResetFrameStats"/>'tan bu yana görülen en uzun kare (s); süre toleransı = token + 2 kare.</summary>
        public static float MaxFrameDt { get; private set; }
        /// <summary>Test klasörü (kayıt, masa notları, pano yerleşimi).</summary>
        public static string Storage => storage;

        static string storage, previousStorage;
        static bool storageIsolated;
        static DeskLang previousLanguage;
        static int emptySceneCount;

        // ---------- Kurulum ve söküm ----------

        /// <summary>
        /// DeskGame'i yükler ve harekâtı <paramref name="seed"/> ile başlatır (bkz. sınıf özeti). Bittiğinde kamera K1'de,
        /// hikâye kilidi kalkık, <see cref="Baseline"/> alınmış. <paramref name="passBriefing"/> false ise açılış atlanır ama emir
        /// belgesi açık kalır (BAŞLA'ya basılmaz; hikâye kilidi ve masa kilidi sürer, kamera belgede; Revizyon 3 · Aşama 4 P10
        /// "belge açık" kilidi için). O durumda <c>RetireJobs</c>'un komutları da reddedilir (<c>StoryHold</c>).
        /// </summary>
        public static IEnumerator LoadDesk(uint seed = DefaultSeed, bool passBriefing = true)
        {
#if !UNITY_EDITOR
            Assert.Ignore("Pano PlayMode testleri yalnız editörde koşar: kayıt klasörü SessionState ile ayrılır.");
#endif
            IsolateStorage();
            previousLanguage = DeskLoc.Instance.Lang;
            GameLaunch.Pending = GameLaunch.Mode.None;

            var load = LoadDeskScene();
            Assert.IsNotNull(load, "DeskGame yüklenemedi: " + DeskScenePath);
            while (!load.isDone) yield return null;
            yield return null; // Awake/OnEnable bitti; Start'lar bu karede (LiberationDeskView açılışı başlatır)
            FindHandles();

            // Açılış Start'ta başladı; yeni harekât (NewGameStarted) açılışı aynı tohumla baştan açar
            yield return WaitFor(() => Briefing.Busy, 5, "açılış başlamadı (LiberationDeskView.Start)");
            Game.NewGame(seed);
            if (passBriefing)
            {
                yield return PassBriefing();
                Rig.Snap(SiginakPose.Desk);
            }
            else yield return SkipToDocument();
            // PlanningBoardPresenter.RetireJobs (LateUpdate) board_auto ve emekli görevlerin BOŞTA'ya alınışını kaydeder
            yield return Frames(3);
            Assert.IsNotNull(Sim, "Simulation");
            Assert.IsNotNull(Sim.Board, "yeni harekât kartlı pano kipinde (Sim.Board)");
            if (passBriefing)
            {
                Assert.IsFalse(Game.StoryHold, "hikâye kilidi kalkmalı");
                Assert.AreEqual(SiginakPose.Desk, Rig.Current);
            }
            else
            {
                Assert.IsTrue(Game.StoryHold, "emir belgesi açık: hikâye kilidi sürmeli");
                Assert.IsTrue(Briefing.Busy, "emir belgesi açık kalmalı (BAŞLA'ya basılmadı)");
            }
            Assert.AreEqual(storage, SaveStore.StorageDirectory, "kayıtlar hâlâ test klasörüne gidiyor");
            Baseline = Snapshot();
            ResetFrameStats();
        }

        /// <summary>
        /// Kaydedilmiş oyunu aynı test klasöründen KAYITTAN DEVAM ile açar (menünün yolu: <c>GameLaunch.Pending = Continue</c>, sonra
        /// DeskGame yeniden yüklenir; <c>LiberationDeskView.Start</c> → <c>GameController.ContinueSaved</c> → <c>GameLoaded</c> →
        /// dönüş açılışı). Klasör yeniden ayrılmaz; önce <see cref="LoadDesk"/> çağrılmış olmalı ve klasörde kayıt bulunmalı.
        /// Dönüş açılışı atlanır (Boşluk / ATLA), kamera K1'de; tutamaklar yeni sahneden yeniden bulunur, <see cref="Baseline"/>
        /// yeniden alınır (yükleme yeni komut yazmamalı; çağıran kayıt dosyasıyla karşılaştırır).
        /// </summary>
        public static IEnumerator ContinueDesk()
        {
            Assert.IsTrue(storageIsolated, "ContinueDesk önce LoadDesk ister (kayıt test klasöründe)");
            Assert.AreEqual(storage, SaveStore.StorageDirectory, "kayıtlar hâlâ test klasörüne gidiyor");
            Assert.IsNotNull(SaveStore.Load(), "test klasöründe devam edilecek kayıt yok (" + SaveStore.SavePath + ")");
            GameLaunch.Pending = GameLaunch.Mode.Continue;

            var load = LoadDeskScene();
            Assert.IsNotNull(load, "DeskGame yeniden yüklenemedi: " + DeskScenePath);
            while (!load.isDone) yield return null;
            yield return null; // Start bu karede: kayıttan devam ve dönüş açılışı
            FindHandles();
            yield return WaitFor(() => GameLaunch.Pending == GameLaunch.Mode.None, 2, "LiberationDeskView.Start kayıttan devamı almadı (GameLaunch.Take)");

            float until = Time.unscaledTime + 10;
            while (Briefing.Busy)
            {
                Briefing.Skip();
                if (Time.unscaledTime > until) Assert.Fail("kayıttan devamın dönüş açılışı bitmedi (SiginakBriefing.OpenReturn)");
                yield return null;
                TrackFrame();
            }
            yield return WaitFor(() => !Game.StoryHold, 2, "kayıttan devamda hikâye kilidi kalkmadı");
            yield return Frames(3);
            Assert.IsNotNull(Sim, "Simulation (kayıttan)");
            Assert.IsNotNull(Sim.Board, "kayıttan dönen oyun kartlı pano kipinde (Sim.Board)");
            Assert.AreEqual(storage, SaveStore.StorageDirectory, "kayıtlar hâlâ test klasörüne gidiyor");
            Baseline = Snapshot();
            ResetFrameStats();
        }

        /// <summary>Yüklenen DeskGame'in tutamakları (yeniden yüklemede eskileri yok olur).</summary>
        static void FindHandles()
        {
            Game = Find<GameController>();
            Rig = Find<SiginakCameraRig>();
            Surface = Find<LiberationDeskView>();
            Desk = Find<SiginakDesk>();
            Presenter = Find<PlanningBoardPresenter>();
            Briefing = Find<SiginakBriefing>();
            Events = EventSystem.current != null ? EventSystem.current : Find<EventSystem>();
            Assert.IsNotNull(Game, "GameController");
            Assert.IsNotNull(Rig, "SiginakCameraRig");
            Assert.IsNotNull(Surface, "LiberationDeskView");
            Assert.IsNotNull(Desk, "SiginakDesk");
            Assert.IsNotNull(Presenter, "PlanningBoardPresenter");
            Assert.IsNotNull(Briefing, "SiginakBriefing");
            Assert.IsNotNull(Events, "EventSystem");
        }

        /// <summary>
        /// Testin sonunda (<c>[UnityTearDown]</c>): sanal aygıtlar ve Input System ayarı geri alınır, DeskGame boşaltılır,
        /// sonra test klasörü silinir; önceki SessionState değeri ve dil geri gelir. Sıra önemli: sahne kapanmadan klasör
        /// bırakılırsa Play'den çıkarken <c>GameController.OnApplicationQuit</c> gerçek kaydın üstüne yazar.
        /// </summary>
        public static IEnumerator TearDown()
        {
            RestoreInput();
            var desk = SceneManager.GetSceneByPath(DeskScenePath);
            if (desk.IsValid() && desk.isLoaded)
            {
                // Son yüklü sahne boşaltılamaz: önce boş bir sahne
                var empty = SceneManager.CreateScene("BoardPlayKit-" + (++emptySceneCount) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
                SceneManager.SetActiveScene(empty);
                var unload = SceneManager.UnloadSceneAsync(desk);
                while (unload != null && !unload.isDone) yield return null;
            }
            yield return null;
            Game = null; Rig = null; Surface = null; Desk = null; Presenter = null; Briefing = null; Events = null;
            if (DeskLoc.Instance.Lang != previousLanguage) DeskLoc.Instance.SetLanguage(previousLanguage);
            GameLaunch.Pending = GameLaunch.Mode.None;
            RestoreStorage();
        }

        static AsyncOperation LoadDeskScene()
        {
            if (SceneUtility.GetBuildIndexByScenePath(DeskScenePath) >= 0)
                return SceneManager.LoadSceneAsync(DeskScenePath, LoadSceneMode.Single);
#if UNITY_EDITOR
            // Build listesinde değilse (olmamalı) editörün yolu
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(DeskScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            return null;
#endif
        }

        static void IsolateStorage()
        {
            storage = Path.Combine(Path.GetTempPath(), "Corrigible-PlayTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(storage);
#if UNITY_EDITOR
            if (!storageIsolated) previousStorage = UnityEditor.SessionState.GetString(StorageKey, "");
            UnityEditor.SessionState.SetString(StorageKey, storage);
#endif
            storageIsolated = true;
            Assert.AreEqual(storage, SaveStore.StorageDirectory, "kayıt klasörü testten önce ayrılmalı (SessionState " + StorageKey + ")");
        }

        static void RestoreStorage()
        {
#if UNITY_EDITOR
            if (storageIsolated)
            {
                if (string.IsNullOrEmpty(previousStorage)) UnityEditor.SessionState.EraseString(StorageKey);
                else UnityEditor.SessionState.SetString(StorageKey, previousStorage);
            }
#endif
            storageIsolated = false;
            previousStorage = null;
            try
            {
                if (!string.IsNullOrEmpty(storage) && Directory.Exists(storage)) Directory.Delete(storage, true);
            }
            catch (IOException e) { Debug.LogWarning("BoardPlayKit: test klasörü silinemedi: " + e.Message); }
            catch (UnauthorizedAccessException e) { Debug.LogWarning("BoardPlayKit: test klasörü silinemedi: " + e.Message); }
            storage = null;
        }

        // ---------- Emir belgesi ----------

        static readonly MethodInfo BriefingAction = typeof(SiginakBriefing).GetMethod("Action", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo BriefingState = typeof(SiginakBriefing).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo BriefingViewField = typeof(SiginakBriefing).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>Belgenin durumu ("Idle", "Opening", "Ready", "Reading", "Leaving"); alan bulunamazsa açılışa bakılır.</summary>
        static string BriefingStateName()
        {
            if (BriefingState != null) return BriefingState.GetValue(Briefing)?.ToString();
            return !Briefing.Busy ? "Idle" : Briefing.OpeningPlaying ? "Opening" : "Ready";
        }

        /// <summary>Açılış atlanır (Boşluk / ATLA); emir belgesi hazır (BAŞLA bekliyor) olana dek beklenir, basılmaz.</summary>
        static IEnumerator SkipToDocument()
        {
            float until = Time.unscaledTime + 10;
            while (BriefingStateName() != "Ready")
            {
                Briefing.Skip();
                if (Time.unscaledTime > until) Assert.Fail("emir belgesi hazır olmadı (durum " + BriefingStateName() + ")");
                yield return null;
            }
        }

        static bool BriefingAnimating() => BriefingViewField != null && BriefingViewField.GetValue(Briefing) is BriefingView v && v != null && v.Animating;

        /// <summary>
        /// Açılış atlanır (Boşluk / ATLA), belge BAŞLA ile geçilir (GELISTIRME › Oyunu ajan olarak test etmek: kayıt varken
        /// ilk basış uyarı, ikincisi onay; test klasörü boş olduğundan çoğunlukla tek basış). Belge EVRAK'a gidene dek beklenir.
        /// </summary>
        static IEnumerator PassBriefing()
        {
            Assert.IsNotNull(BriefingAction, "SiginakBriefing.Action (özel BAŞLA) bulunamadı");
            yield return SkipToDocument();
            for (int press = 0; press < 3 && BriefingStateName() == "Ready"; press++)
            {
                yield return WaitFor(() => !BriefingAnimating(), 5, "emir belgesinin canlandırması bitmedi");
                BriefingAction.Invoke(Briefing, null);
                yield return null;
            }
            yield return WaitFor(() => !Briefing.Busy, 10, "emir belgesi kapanmadı (BAŞLA)");
            yield return WaitFor(() => !Game.StoryHold, 2, "hikâye kilidi kalkmadı");
        }

        // ---------- Durum ----------

        /// <summary>(komut sayısı, RNG durumu).</summary>
        public static (int commands, uint rng) Snapshot() => (Sim.Record.commands.Count, Sim.S.Rng.State);

        /// <summary><see cref="Baseline"/>'dan sonra kaydedilen komutlar.</summary>
        public static List<SavedCommand> NewCommands() => Sim.Record.commands.Skip(Baseline.commands).ToList();

        /// <summary>Sunum hareketi komut yazmadı ve RNG çekmedi.</summary>
        public static void AssertNoCommand(string context)
        {
            var now = Snapshot();
            Assert.AreEqual(Baseline.commands, now.commands, context + ": komut kaydedilmemeli (" +
                string.Join(", ", NewCommands().Select(c => c.kind + "(" + c.a + "," + c.b + ")")) + ")");
            Assert.AreEqual(Baseline.rng, now.rng, context + ": RNG durumu değişmemeli");
        }

        public static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        /// <summary>
        /// Odanın vardiya şalteri: <paramref name="owner"/>'ın (<c>SiginakShiftClose</c>, <c>SiginakShift</c>, <c>SiginakDesk</c>,
        /// <c>ShiftReceiptMachine</c>) özel <c>shiftSwitch</c> alanı; boşsa sahnedeki <c>SiginakShift</c>'inki. Sahnede iki
        /// <see cref="ShiftSwitch"/> var (ayarlar dolabının MENÜYE DÖN kolu da; E §0.3), bu yüzden <see cref="Find{T}"/> kullanılmaz.
        /// </summary>
        public static ShiftSwitch ShiftSwitchOf(Object owner = null)
        {
            if (owner == null) owner = Find<SiginakShift>();
            if (owner == null) return null;
            for (var type = owner.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                var field = type.GetField("shiftSwitch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null) return field.GetValue(owner) as ShiftSwitch;
            }
            return null;
        }

        /// <summary>Panonun model örneği (Rev3Model "PlanningBoard").</summary>
        public static Rev3Model BoardModel => Object.FindObjectsByType<Rev3Model>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(m => m.ModelName == "PlanningBoard");

        /// <summary>Panonun çapası (Slot_tek, Bosta_Deck, Mat_metal …).</summary>
        public static Transform Anchor(string name)
        {
            var model = BoardModel;
            return model != null ? model.Anchor(name) : null;
        }

        /// <summary>K4'e gider (SIĞINAK tuşunun yolu: <see cref="PlanningBoardPresenter.Show"/>) ve varışı bekler.</summary>
        public static IEnumerator EnterBoard()
        {
            Presenter.Show();
            yield return WaitFor(() => Rig.Current == SiginakPose.Shelf && !Rig.InTransition, 3, "K4'e varılmadı");
            yield return null; // varışta pano girdisi açılır (BoardView3D.SetInteractive)
            ResetFrameStats();
        }

        // ---------- Zaman ----------

        public static void ResetFrameStats() => MaxFrameDt = 0;

        /// <summary>Testin kendi kare döngüsünde her <c>yield return null</c>'dan sonra çağrılır (süre toleransı).</summary>
        public static void TrackFrame() => MaxFrameDt = Mathf.Max(MaxFrameDt, Time.unscaledDeltaTime);

        /// <summary>Token süresi için tolerans: iki kare (+ 20 ms kayan nokta ve zamanlayıcı payı).</summary>
        public static float FrameTolerance => 2 * MaxFrameDt + 0.02f;

        public static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) { yield return null; TrackFrame(); }
        }

        public static IEnumerator WaitSeconds(float seconds)
        {
            float until = Time.unscaledTime + seconds;
            while (Time.unscaledTime < until) { yield return null; TrackFrame(); }
        }

        /// <summary>Koşul doğru olana dek bekler; <paramref name="timeout"/> saniyede olmazsa testi düşürür.</summary>
        public static IEnumerator WaitFor(Func<bool> condition, float timeout, string what)
        {
            float until = Time.unscaledTime + timeout;
            while (!condition())
            {
                if (Time.unscaledTime > until) Assert.Fail("zaman aşımı (" + timeout + " s): " + what);
                yield return null;
                TrackFrame();
            }
        }

        /// <summary>
        /// Son <paramref name="pose"/> geçişi: başında kalan süre token'a eşit (±1 ms) ve varış, başlangıçtan token ± iki kare
        /// sonra (rig süreyi kare süreleriyle sayar; ölçüm kare başında alındığından bir kare erken ya da geç görünebilir).
        /// </summary>
        public static void AssertTransition(PoseLog log, SiginakPose pose, float seconds, string route)
        {
            Assert.IsTrue(log.Changes.Any(c => c.Pose == pose), route + ": poz " + pose + " olmadı");
            var change = log.Changes.Last(c => c.Pose == pose);
            Assert.AreEqual(seconds, change.Remaining, 1e-3f, route + ": geçiş süresi");
            Assert.IsTrue(log.Arrivals.Any(a => a.Pose == pose && a.Frame >= change.Frame), route + ": " + pose + " pozuna varılmadı");
            var arrival = log.Arrivals.Last(a => a.Pose == pose && a.Frame >= change.Frame);
            float took = arrival.At - change.At, tolerance = FrameTolerance;
            Assert.That(took, Is.InRange(seconds - tolerance, seconds + tolerance),
                route + ": varış " + took.ToString("0.000") + " s (beklenen " + seconds + " ± " + tolerance.ToString("0.000") + ")");
        }

        /// <summary>Kamera pozun çapasında (±0,1 mm, ±0,01°; bakış sıfır).</summary>
        public static void AssertAtAnchor(SiginakPose pose, string context)
        {
            Assert.IsTrue(Rig.TryGetPose(pose, out var slot), context + ": " + pose + " pozu yok");
            Assert.Less(Vector3.Distance(slot.anchor.position, Rig.transform.position), 1e-4f, context + ": kamera " + slot.anchor.name + " çapasında (±0,1 mm)");
            Assert.Less(Quaternion.Angle(slot.anchor.rotation, Rig.transform.rotation), 0.01f, context + ": kamera " + slot.anchor.name + " yönünde");
        }

        // ---------- İşaretçi (ExecuteEvents) ----------

        public static Vector2 ScreenPoint(Vector3 world)
        {
            var p = View.WorldToScreenPoint(world);
            return new Vector2(p.x, p.y);
        }

        /// <summary>Hedefe ışın yapılmış gibi dolu bir işaretçi olayı (sol tuş varsayılan; modül odanın PhysicsRaycaster'ı).</summary>
        public static PointerEventData Pointer(GameObject target, Vector2 screen, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            var module = View != null ? View.GetComponent<BaseRaycaster>() : null;
            var hit = new RaycastResult
            {
                gameObject = target,
                module = module,
                screenPosition = screen,
                worldPosition = target != null ? target.transform.position : Vector3.zero,
                distance = target != null && View != null ? Vector3.Distance(View.transform.position, target.transform.position) : 0,
            };
            return new PointerEventData(Events)
            {
                button = button,
                position = screen,
                pressPosition = screen,
                clickTime = Time.unscaledTime,
                clickCount = 1,
                eligibleForClick = true,
                useDragThreshold = true,
                pointerCurrentRaycast = hit,
                pointerPressRaycast = hit,
                pointerEnter = target,
                pointerPress = target,
                rawPointerPress = target,
            };
        }

        /// <summary>
        /// Sol tıklama (giriş, basış, bırakış, tık, çıkış) hedefin kendi ekran noktasında; <paramref name="clickCount"/> 2 çift
        /// tıktır (önceki tıktan sonra bir kare bekleyip çağır).
        /// </summary>
        public static void Click(GameObject target, int clickCount = 1)
        {
            var ped = Pointer(target, ScreenPoint(target.transform.position));
            ped.clickCount = clickCount;
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerExitHandler);
        }

        /// <summary>
        /// Sürükleme, EventSystem'in sırasıyla: giriş, basış, olası sürükleme; sonra <paramref name="frames"/> karede doğrusal
        /// hareket (ilk adımda sürükleme başlar), bırakış, sürükleme sonu, çıkış. Hedef <c>eventData.position</c>'dan çözülmeli
        /// (BoardDragController; <c>pointerCurrentRaycast</c> güncellenmez).
        /// </summary>
        public static IEnumerator Drag(GameObject card, Vector3 worldTo, int frames = 10)
        {
            Vector2 from = ScreenPoint(card.transform.position), to = ScreenPoint(worldTo);
            var ped = Pointer(card, from);
            ExecuteEvents.Execute(card, ped, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(card, ped, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(card, ped, ExecuteEvents.initializePotentialDrag);
            yield return null;
            TrackFrame();
            frames = Mathf.Max(1, frames);
            for (int i = 1; i <= frames; i++)
            {
                var p = Vector2.Lerp(from, to, i / (float)frames);
                ped.delta = p - ped.position;
                ped.position = p;
                if (i == 1)
                {
                    ped.dragging = true;
                    ped.pointerDrag = card;
                    ped.eligibleForClick = false;
                    ExecuteEvents.Execute(card, ped, ExecuteEvents.beginDragHandler);
                }
                ExecuteEvents.Execute(card, ped, ExecuteEvents.dragHandler);
                yield return null;
                TrackFrame();
            }
            ped.delta = Vector2.zero;
            ExecuteEvents.Execute(card, ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(card, ped, ExecuteEvents.endDragHandler);
            ped.dragging = false;
            ped.pointerDrag = null;
            ExecuteEvents.Execute(card, ped, ExecuteEvents.pointerExitHandler);
        }

        // ---------- Gerçek ışın ve hiyerarşi (Revizyon 3 · Aşama 5, E §3.1) ----------

        /// <summary>Kancadaki tarif defteri (<see cref="BoardView3D.Book"/>; sahne bağlanmadıysa null).</summary>
        public static RecipeBookView Book => Board != null ? Board.Book : null;

        /// <summary>
        /// EventSystem'in o ekran noktası için bulduğu bütün sonuçlar, öndeki önce (odanın PhysicsRaycaster'ı ve tuvallerin
        /// GraphicRaycaster'ları). Aşama 3'te örtülme sorunu yaşandığı için Aşama 5 testleri tıkı bununla doğrular.
        /// </summary>
        public static List<RaycastResult> RaycastAll(Vector2 screen)
        {
            var list = new List<RaycastResult>();
            if (Events != null) Events.RaycastAll(new PointerEventData(Events) { position = screen }, list);
            return list;
        }

        /// <summary>Ekran noktasındaki ilk (en öndeki) sonuç; hiçbir şey yoksa <c>gameObject == null</c>.</summary>
        public static RaycastResult FirstHit(Vector2 screen)
        {
            var list = RaycastAll(screen);
            return list.Count > 0 ? list[0] : default;
        }

        /// <summary>Sonuçların okunur listesi (hata iletileri için): "ad (uzaklık)".</summary>
        public static string DescribeHits(Vector2 screen) =>
            string.Join(", ", RaycastAll(screen).Take(5).Select(h => (h.gameObject != null ? h.gameObject.name : "?") + " (" + h.distance.ToString("0.000") + ")"));

        /// <summary>Gerçek ışın sonucundan dolu bir işaretçi olayı (basış ve şimdiki ışın aynı; tıklanabilir).</summary>
        public static PointerEventData PointerAt(RaycastResult hit, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            var screen = hit.screenPosition;
            return new PointerEventData(Events)
            {
                button = button,
                position = screen,
                pressPosition = screen,
                clickTime = Time.unscaledTime,
                clickCount = 1,
                eligibleForClick = true,
                useDragThreshold = true,
                pointerCurrentRaycast = hit,
                pointerPressRaycast = hit,
                pointerEnter = hit.gameObject,
                rawPointerPress = hit.gameObject,
            };
        }

        /// <summary>
        /// Gerçek ışının ilk sonucuna sol tık, EventSystem'in kuralıyla: giriş ve çıkış hedefin bütün atalarına, basış hiyerarşide ilk
        /// <c>IPointerDownHandler</c>'a (yoksa tık işleyicisine), bırakış ona, tık yalnız basılan nesne tık işleyicisiyse. Işın bir
        /// şeye çarpmazsa false.
        /// </summary>
        public static bool ClickHit(RaycastResult hit, int clickCount = 1)
        {
            var go = hit.gameObject;
            if (go == null) return false;
            var ped = PointerAt(hit);
            ped.clickCount = clickCount;
            for (var t = go.transform; t != null; t = t.parent) ExecuteEvents.Execute(t.gameObject, ped, ExecuteEvents.pointerEnterHandler);
            var press = ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerDownHandler);
            var click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
            if (press == null) press = click;
            ped.pointerPress = press;
            if (press != null) ExecuteEvents.Execute(press, ped, ExecuteEvents.pointerUpHandler);
            if (press != null && press == click && ped.eligibleForClick) ExecuteEvents.Execute(press, ped, ExecuteEvents.pointerClickHandler);
            ped.pointerPress = null;
            for (var t = go.transform; t != null; t = t.parent) ExecuteEvents.Execute(t.gameObject, ped, ExecuteEvents.pointerExitHandler);
            return true;
        }

        /// <summary>Ekran noktasına gerçek ışınla sol tık; çarpılan nesne (yoksa null).</summary>
        public static GameObject ClickScreen(Vector2 screen)
        {
            var hit = FirstHit(screen);
            return ClickHit(hit) ? hit.gameObject : null;
        }

        /// <summary>Dünya noktasının ekranı (odanın kamerası).</summary>
        public static Vector2 ScreenOfWorld(Vector3 world) => ScreenPoint(world);

        /// <summary>Panonun mm noktasının dünya konumu (model çerçevesinde yüz düzlemi; <see cref="BoardLayout.MmToLocal"/>).</summary>
        public static Vector3 BoardWorld(Vector2 mm)
        {
            var model = BoardModel;
            var local = BoardLayout.MmToLocal(mm, BoardLayout.FaceZ);
            return model != null ? model.transform.TransformPoint(local) : local;
        }

        /// <summary>Panonun ekran noktası (mm).</summary>
        public static Vector2 BoardScreen(Vector2 mm) => ScreenPoint(BoardWorld(mm));

        /// <summary>Basış (girdi, basış, olası sürükleme); <see cref="PointerGesture"/> sürer ve bırakır. Gerçek ışın sonucuyla.</summary>
        public static PointerGesture Press(RaycastResult hit) => PointerGesture.Begin(PointerAt(hit), hit.gameObject);

        /// <summary>Basış, hedefe ışın yapılmış gibi (<see cref="Pointer"/>; kart kendi ekran noktasında). Bırakılmaz.</summary>
        public static PointerGesture PressOnly(GameObject target)
        {
            var ped = Pointer(target, ScreenPoint(target.transform.position));
            return PointerGesture.Begin(ped, target);
        }

        // ---------- Klavye ve fare (sanal aygıtlar) ----------

        static Mouse testMouse, previousMouse;
        static Keyboard testKeyboard, previousKeyboard;
        static bool inputPatched;
        static InputSettings.BackgroundBehavior previousBackground;
        static InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;

        /// <summary>
        /// Test boyunca girdi Game görünümünün odağına bakmadan oyuna gider (Editor arka plandayken de); söküm geri alır.
        /// </summary>
        static void PatchInput()
        {
            if (inputPatched) return;
            var settings = InputSystem.settings;
            previousBackground = settings.backgroundBehavior;
            previousEditorBehavior = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            inputPatched = true;
        }

        /// <summary>Testin faresi (geçerli fare yapılır; odanın <c>Mouse.current</c> okuyan kodu onu görür).</summary>
        public static Mouse TestMouse
        {
            get
            {
                PatchInput();
                if (testMouse == null || !testMouse.added)
                {
                    if (previousMouse == null) previousMouse = Mouse.current;
                    testMouse = InputSystem.AddDevice<Mouse>("BoardPlayKitMouse");
                }
                if (Mouse.current != testMouse) testMouse.MakeCurrent();
                return testMouse;
            }
        }

        /// <summary>Testin klavyesi (geçerli klavye yapılır).</summary>
        public static Keyboard TestKeyboard
        {
            get
            {
                PatchInput();
                if (testKeyboard == null || !testKeyboard.added)
                {
                    if (previousKeyboard == null) previousKeyboard = Keyboard.current;
                    testKeyboard = InputSystem.AddDevice<Keyboard>("BoardPlayKitKeyboard");
                }
                if (Keyboard.current != testKeyboard) testKeyboard.MakeCurrent();
                return testKeyboard;
            }
        }

        static void RestoreInput()
        {
            if (testMouse != null && testMouse.added) InputSystem.RemoveDevice(testMouse);
            if (testKeyboard != null && testKeyboard.added) InputSystem.RemoveDevice(testKeyboard);
            testMouse = null;
            testKeyboard = null;
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            previousMouse = null;
            previousKeyboard = null;
            if (!inputPatched) return;
            var settings = InputSystem.settings;
            settings.backgroundBehavior = previousBackground;
            settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            inputPatched = false;
        }

        /// <summary>
        /// Tuşa bir kez basıp bırakır: basış bir sonraki karenin girdi güncellemesinde işlenir, o karenin Update/LateUpdate'leri
        /// <c>wasPressedThisFrame</c> görür; bırakış ondan sonraki karede.
        /// </summary>
        public static IEnumerator PressKey(Key key)
        {
            var keyboard = TestKeyboard;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            TrackFrame();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            TrackFrame();
        }

        /// <summary>
        /// Sağ tuş: <paramref name="at"/>'ta basış, <paramref name="frames"/> karede <paramref name="move"/> kadar doğrusal
        /// hareket (kare başına bir olay; konum ve delta tutarlı), sonra bırakış. frames 0 ise basıp yerinde bırakır.
        /// </summary>
        public static IEnumerator RightDrag(Vector2 at, Vector2 move, int frames)
        {
            var mouse = TestMouse;
            QueueMouse(mouse, at, Vector2.zero, true);
            yield return null;
            TrackFrame();
            var last = at;
            for (int i = 1; i <= frames; i++)
            {
                var p = at + move * (i / (float)frames);
                QueueMouse(mouse, p, p - last, true);
                last = p;
                yield return null;
                TrackFrame();
            }
            QueueMouse(mouse, last, Vector2.zero, false);
            yield return null;
            TrackFrame();
            yield return null; // bırakışın sonucu (BackClicked → poz) bu karede kesinleşmiş olur
            TrackFrame();
        }

        /// <summary>Fareyi tuşsuz bir noktaya taşır (üzerine gelme).</summary>
        public static IEnumerator MoveMouse(Vector2 to)
        {
            var mouse = TestMouse;
            var from = mouse.position.ReadValue();
            QueueMouse(mouse, to, to - from, false);
            yield return null;
            TrackFrame();
        }

        static void QueueMouse(Mouse mouse, Vector2 position, Vector2 delta, bool right)
        {
            var state = new MouseState { position = position, delta = delta };
            if (right) state = state.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(mouse, state);
        }

        /// <summary>Ekranın panoya düşen boş bir noktası (K4'te ekranın ortası; kart değil, levha değil).</summary>
        public static Vector2 ScreenCentre => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        static readonly FieldInfo BoardBackTarget = typeof(BoardView3D).GetField("backTarget", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// MASAYA DÖN levhasının tıklanır hedefi: önce <see cref="BoardView3D"/>'nin bağlı <c>backTarget</c>'ı (sahne bağlayıcısının
        /// kurduğu), yoksa BackSign parçası (ya da onun altı) veya başlığı "MASAYA DÖN" olan.
        /// </summary>
        public static DeskTarget BackSignTarget()
        {
            var board = Board;
            if (board != null && BoardBackTarget != null && BoardBackTarget.GetValue(board) is DeskTarget wired && wired != null) return wired;
            foreach (var target in Object.FindObjectsByType<DeskTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (target.Title == "MASAYA DÖN") return target;
                for (var t = target.transform; t != null; t = t.parent)
                    if (t.name == "BackSign") return target;
            }
            return null;
        }
    }

    /// <summary>
    /// EventSystem'in sürükleme sırasıyla süren bir işaretçi hareketi (Revizyon 3 · Aşama 5, E §3.1 "HoldDrag"): basışta giriş
    /// atalara, basış hiyerarşide ilk <c>IPointerDownHandler</c>'a, olası sürükleme <c>IDragHandler</c>'a (<c>pointerDrag</c>).
    /// İlk <see cref="Move"/> adımında sürükleme başlar; sonraki sürükleme olayları o anki <c>pointerDrag</c>'a gider: sürüklemenin
    /// başında işleyici onu değiştirirse (defterin tarif kopyası: <c>BoardDragController.Adopt</c>) olaylar yeni nesneye akar.
    /// <see cref="Release"/> bırakır (bırakış, sürükleme sonu, çıkış). Bırakmadan tutmak için <see cref="Hold"/>.
    /// </summary>
    public sealed class PointerGesture
    {
        public readonly PointerEventData Data;
        /// <summary>Basılan (ışının çarptığı) nesne.</summary>
        public readonly GameObject Over;
        public bool Dragging => Data.dragging;
        public bool Released { get; private set; }
        /// <summary>Sürükleme olaylarının o anki alıcısı (<c>pointerDrag</c>).</summary>
        public GameObject Dragged => Data.pointerDrag;

        PointerGesture(PointerEventData data, GameObject over)
        {
            Data = data;
            Over = over;
        }

        internal static PointerGesture Begin(PointerEventData ped, GameObject over)
        {
            var g = new PointerGesture(ped, over);
            if (over == null) return g;
            for (var t = over.transform; t != null; t = t.parent) ExecuteEvents.Execute(t.gameObject, ped, ExecuteEvents.pointerEnterHandler);
            var press = ExecuteEvents.ExecuteHierarchy(over, ped, ExecuteEvents.pointerDownHandler);
            if (press == null) press = ExecuteEvents.GetEventHandler<IPointerClickHandler>(over);
            ped.pointerPress = press;
            ped.rawPointerPress = over;
            ped.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(over);
            if (ped.pointerDrag != null) ExecuteEvents.Execute(ped.pointerDrag, ped, ExecuteEvents.initializePotentialDrag);
            return g;
        }

        /// <summary>
        /// İşaretçiyi <paramref name="to"/>'ya <paramref name="frames"/> karede doğrusal götürür (kare başına bir sürükleme olayı);
        /// <paramref name="onFrame"/> her kareden sonra. İlk adım sürüklemeyi başlatır.
        /// </summary>
        public IEnumerator Move(Vector2 to, int frames, Action onFrame = null)
        {
            var from = Data.position;
            frames = Mathf.Max(1, frames);
            for (int i = 1; i <= frames; i++)
            {
                Step(Vector2.Lerp(from, to, i / (float)frames));
                yield return null;
                BoardPlayKit.TrackFrame();
                onFrame?.Invoke();
            }
        }

        /// <summary>Yerinde <paramref name="frames"/> kare tutar (sıfır deltalı sürükleme olayı; sürükleme başlamışsa).</summary>
        public IEnumerator Hold(int frames, Action onFrame = null)
        {
            for (int i = 0; i < frames; i++)
            {
                if (Data.dragging) { Data.delta = Vector2.zero; Send(ExecuteEvents.dragHandler); }
                yield return null;
                BoardPlayKit.TrackFrame();
                onFrame?.Invoke();
            }
        }

        /// <summary>Tek bir hareket adımı (kare beklemeden).</summary>
        public void Step(Vector2 p)
        {
            if (Released) return;
            Data.delta = p - Data.position;
            Data.position = p;
            if (!Data.dragging && Data.pointerDrag != null)
            {
                Data.dragging = true;
                Data.eligibleForClick = false;
                ExecuteEvents.Execute(Data.pointerDrag, Data, ExecuteEvents.beginDragHandler);
            }
            Send(ExecuteEvents.dragHandler);
        }

        void Send<T>(ExecuteEvents.EventFunction<T> f) where T : IEventSystemHandler
        {
            if (Data.pointerDrag != null) ExecuteEvents.Execute(Data.pointerDrag, Data, f);
        }

        /// <summary>Bırakış: basılan nesneye bırakış, sürükleniyorsa o anki alıcıya sürükleme sonu, basılan nesnenin atalarına çıkış.</summary>
        public void Release()
        {
            if (Released) return;
            Released = true;
            Data.delta = Vector2.zero;
            if (Data.pointerPress != null) ExecuteEvents.Execute(Data.pointerPress, Data, ExecuteEvents.pointerUpHandler);
            if (Data.dragging) Send(ExecuteEvents.endDragHandler);
            Data.dragging = false;
            Data.pointerDrag = null;
            Data.pointerPress = null;
            if (Over != null)
                for (var t = Over.transform; t != null; t = t.parent) ExecuteEvents.Execute(t.gameObject, Data, ExecuteEvents.pointerExitHandler);
        }
    }

    /// <summary>
    /// Panonun ses olayları zamanıyla (<see cref="BoardView3D.Sound"/>: defter, depo, kilit; <see cref="BoardDragController.Sound"/>:
    /// kartlar). Ses <c>Sfx.Play</c>'den gözlenemez (Game assembly'si); sunumun olayları gözlenir (Aşama 4 kalıbı).
    /// </summary>
    public sealed class BoardSoundLog : IDisposable
    {
        public readonly struct Entry
        {
            public readonly BoardSound Sound;
            public readonly float At;
            public readonly int Frame;
            public readonly bool FromDrag;

            public Entry(BoardSound sound, float at, int frame, bool fromDrag)
            {
                Sound = sound; At = at; Frame = frame; FromDrag = fromDrag;
            }
        }

        public readonly List<Entry> Entries = new List<Entry>();
        readonly BoardView3D board;
        readonly BoardDragController drag;

        public BoardSoundLog(BoardView3D boardView)
        {
            board = boardView;
            drag = boardView != null ? boardView.Drag : null;
            if (board != null) board.Sound += OnBoard;
            if (drag != null) drag.Sound += OnDrag;
        }

        void OnBoard(BoardSound s) => Entries.Add(new Entry(s, Time.unscaledTime, Time.frameCount, false));
        void OnDrag(BoardSound s) => Entries.Add(new Entry(s, Time.unscaledTime, Time.frameCount, true));

        public int Count(BoardSound s) => Entries.Count(e => e.Sound == s);

        /// <summary>Sesin ilk olayının zamanı (<paramref name="after"/>'dan sonra); yoksa testi düşürür.</summary>
        public float At(BoardSound s, float after = float.MinValue)
        {
            foreach (var e in Entries) if (e.Sound == s && e.At >= after) return e.At;
            Assert.Fail("ses olayı yok: " + s + " (" + this + ")");
            return 0;
        }

        public void Clear() => Entries.Clear();

        public override string ToString() => string.Join(", ", Entries.Select(e => e.Sound + "@" + e.At.ToString("0.000")));

        public void Dispose()
        {
            if (board != null) board.Sound -= OnBoard;
            if (drag != null) drag.Sound -= OnDrag;
        }
    }

    /// <summary>Kameranın poz değişimlerini, varışlarını ve sürüklemesiz sağ tıklarını zamanıyla tutar (kare başı zaman).</summary>
    public sealed class PoseLog : IDisposable
    {
        public readonly struct Entry
        {
            public readonly SiginakPose Pose;
            /// <summary><c>Time.unscaledTime</c> (olayın karesi).</summary>
            public readonly float At;
            /// <summary>Olay anında geçişin kalan süresi (poz değişiminde = geçişin süresi).</summary>
            public readonly float Remaining;
            public readonly int Frame;

            public Entry(SiginakPose pose, float at, float remaining, int frame)
            {
                Pose = pose; At = at; Remaining = remaining; Frame = frame;
            }
        }

        public readonly List<Entry> Changes = new List<Entry>();
        public readonly List<Entry> Arrivals = new List<Entry>();
        public readonly List<Entry> Backs = new List<Entry>();
        readonly SiginakCameraRig rig;

        public PoseLog(SiginakCameraRig cameraRig)
        {
            rig = cameraRig;
            rig.PoseChanged += OnChanged;
            rig.Arrived += OnArrived;
            rig.BackClicked += OnBack;
        }

        Entry Now(SiginakPose pose) => new Entry(pose, Time.unscaledTime, rig.TransitionRemaining, Time.frameCount);
        void OnChanged(SiginakPose pose) => Changes.Add(Now(pose));
        void OnArrived(SiginakPose pose) => Arrivals.Add(Now(pose));
        void OnBack(SiginakPose pose) => Backs.Add(Now(pose));

        public bool Arrived(SiginakPose pose) => Arrivals.Any(a => a.Pose == pose);

        public void Dispose()
        {
            if (rig == null) return;
            rig.PoseChanged -= OnChanged;
            rig.Arrived -= OnArrived;
            rig.BackClicked -= OnBack;
        }
    }
}
