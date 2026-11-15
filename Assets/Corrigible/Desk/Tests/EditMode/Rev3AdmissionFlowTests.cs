using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using Corrigible.Game.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 6 (R9/R17) kapı ve kabul akışı (PLAN W5): ses eşlemesi, kabul dizisinin zaman çizelgesi, K8 ailesinin poz
    /// süreleri, kenar satırı ve kayıt pozu, vuruşun aday grubu başına bir kez (yüklemede yok), KABUL ve GERİ ÇEVİR damgalarının tam bir
    /// Core komutu (<c>candidate_choose</c> / <c>candidate_pass</c>; KOS aşama 2), dolu sığınakta yollanacak kişi, emekli araçların ve
    /// kilidin komut yazmadığı, geri zinciri, panonun eski API'si, görülen durumun yan dosyası, grup teleksi ve ad araması yapılmadığı.
    /// Sahneye bağlı değildir (istasyon görünümleri bağlanmaz; akış Core'a göre çalışır); kayıtlar geçici klasöre yazılır. Eski tek adaylı
    /// yolun testleri (inceleme araçları, dış kayıt) aşama 7'de silindi.
    /// </summary>
    public sealed class Rev3AdmissionFlowTests
    {
        const string StorageKey = "Corrigible.TestStorage";
        string originalStorage, directory;
        readonly List<Object> created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            originalStorage = SessionState.GetString(StorageKey, "");
            directory = Path.Combine(Path.GetTempPath(), "Corrigible-Admission-" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(StorageKey, directory);
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            if (string.IsNullOrEmpty(originalStorage)) SessionState.EraseString(StorageKey);
            else SessionState.SetString(StorageKey, originalStorage);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            created.Add(go);
            return go.AddComponent<T>();
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            Assert.IsNotNull(p, target.GetType().Name + "." + field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SiginakTheme Theme => SiginakTheme.Active;

        sealed class Flow
        {
            public GameController Game;
            public SiginakAdmission Admission;
            public SiginakCameraRig Rig;
            public TelexFeed Telex;
            public DoorProp Door;
            public LiberationDeskView Surface;
            public readonly List<AdmissionSound> Sounds = new List<AdmissionSound>();
            public Simulation Sim => Game.Sim;
            public int Commands => Game.Sim.Record.commands.Count;
            public SavedCommand LastCommand => Game.Sim.Record.commands[Game.Sim.Record.commands.Count - 1];
            /// <summary>Kapıda bekleyen grubun sunum anahtarı; grup yoksa null.</summary>
            public string Group => AdmissionSnapshotBuilder.GroupWaiting(Game.Sim) ? AdmissionSnapshotBuilder.GroupKey(Game.Sim) : null;
            /// <summary>Gruptaki ilk adayın kimliği.</summary>
            public string First => Game.Sim.Admission.Group.Count > 0 ? Game.Sim.Admission.Group[0].Resident.Id : null;
            public int Knocks => Sounds.Count(s => s == AdmissionSound.Knock);
        }

        /// <summary>
        /// Gerçek kartlı oyun (GameController, tohum 79; alarm kapalı; <paramref name="knockMin"/>–<paramref name="knockMax"/> vardiyada bir
        /// aday grubu) ve ona bağlı kabul akışı: kamera (K1, K8, K8A, K8R çapalarıyla), masa yüzü, teleks, isteğe bağlı kapı.
        /// <paramref name="toCandidate"/>: ilk grup gelene dek kapanış. <paramref name="capacity"/> &gt; 0 ise sığınak kapasitesi (8 = kurucularla dolu).
        /// </summary>
        Flow Build(bool toCandidate = true, bool withDoor = false, int knockMin = 1, int knockMax = 1, int capacity = 0)
        {
            var f = new Flow();
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            created.Add(config);
            config.balance = new Balance
            { shelterEventsEnabled = false,
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = knockMin, admissionKnockMax = knockMax,
            };
            if (capacity > 0) config.balance.boardResidentCapacity = capacity;
            f.Game = Make<GameController>("Game");
            Set(f.Game, "balanceConfig", config);
            f.Surface = Make<LiberationDeskView>("Surface");
            Set(f.Surface, "game", f.Game);
            f.Telex = Make<TelexFeed>("Telex");

            var root = new GameObject("Poses");
            created.Add(root);
            Transform Anchor(string name, Vector3 pos, Vector3 euler)
            {
                var t = new GameObject(name).transform;
                t.SetParent(root.transform);
                t.SetPositionAndRotation(pos, Quaternion.Euler(euler));
                return t;
            }
            var read = Anchor("K8R", new Vector3(1.87f, 1.38f, -0.27f), new Vector3(76, 90, 0));
            var camGo = new GameObject("Camera", typeof(Camera));
            created.Add(camGo);
            f.Rig = camGo.AddComponent<SiginakCameraRig>();
            f.Rig.Configure(camGo.GetComponent<Camera>(), null, new[]
            {
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Desk, anchor = Anchor("K1", new Vector3(0, 1.52f, -0.15f), Vector3.zero), fieldOfView = 56.7f, lensShiftY = -0.324f },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Door, anchor = Anchor("K8", new Vector3(0.05f, 1.35f, 0.95f), new Vector3(4, 90, 0)), fieldOfView = 58 },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.Admission, anchor = Anchor("K8A", new Vector3(1.44f, 1.69f, -0.27f), new Vector3(47, 90, 0)), fieldOfView = 47 },
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.AdmissionRead, anchor = read, fieldOfView = 40 },
            }, new Vector2(36, 20.25f), 0.45f, 0.15f);
            f.Rig.Snap(SiginakPose.Desk);

            f.Admission = Make<SiginakAdmission>("Admission controller");
            Set(f.Admission, "game", f.Game);
            Set(f.Admission, "surface", f.Surface);
            Set(f.Admission, "rig", f.Rig);
            Set(f.Admission, "telex", f.Telex);
            Set(f.Admission, "readAnchor", read);
            if (withDoor)
            {
                f.Door = Make<DoorProp>("Bunker door");
                Set(f.Admission, "door", f.Door);
            }
            f.Game.NewGame(79);
            if (toCandidate)
                for (int i = 0; i < 8 && f.Group == null; i++) Close(f.Game);
            f.Admission.Restore();
            if (toCandidate) Assert.IsNotNull(f.Group, "kurulum: kapıda aday grubu");
            f.Admission.Sounded += f.Sounds.Add;
            return f;
        }

        /// <summary>Kurulum kapanışı (gerçek komutlar: fiş bas, dosyala).</summary>
        static void Close(GameController game)
        {
            var printed = game.PrintBoardShift();
            Assert.IsTrue(printed.Ok, printed.Msg);
            var filed = game.FileBoardShift(game.Sim.Board.ReceiptId);
            Assert.IsTrue(filed.Ok, filed.Msg);
        }

        static void AtCounter(Flow f)
        {
            f.Rig.Snap(SiginakPose.Admission);
            Assert.IsTrue(f.Admission.Active, "K8A kartlı oyunda K8 ailesidir");
        }

        // ---------------------------------------------------------------- ses, zaman çizelgesi, pozlar

        /// <summary>
        /// PLAN §1.13: her kabul sesi açıkça eşlenir; kabul cue'ları 120–122 ve 127–128 (aşama 7: 123–126 söküldü), var olanlar damga 63,
        /// dönüş 99, ret 100, kâğıt 57, kapı 50/51/62.
        /// </summary>
        [Test]
        public void CueOf_MapsEveryAdmissionSound()
        {
            var expected = new Dictionary<AdmissionSound, Cue>
            {
                { AdmissionSound.CounterDown, Cue.AdmissionCounterDown }, { AdmissionSound.Drawer, Cue.AdmissionDrawer },
                { AdmissionSound.StampLift, Cue.AdmissionStampLift },
                { AdmissionSound.CounterFold, Cue.AdmissionCounterFold }, { AdmissionSound.StampPress, Cue.StampHeavy },
                { AdmissionSound.StampReturn, Cue.BoardReturn }, { AdmissionSound.Deny, Cue.BoardDeny },
                { AdmissionSound.DocDrop, Cue.PaperSlide }, { AdmissionSound.Knock, Cue.DoorKnock },
                { AdmissionSound.Open, Cue.DoorOpen }, { AdmissionSound.Close, Cue.DoorClose },
            };
            var all = (AdmissionSound[])Enum.GetValues(typeof(AdmissionSound));
            Assert.AreEqual(all.Length, expected.Count, "tabloda her kabul sesi var");
            foreach (var s in all) Assert.AreEqual(expected[s], SiginakAdmission.CueOf(s), s.ToString());
            Assert.AreEqual(120, (int)Cue.AdmissionCounterDown);
            Assert.AreEqual(128, (int)Cue.AdmissionCounterFold);
            Assert.AreEqual(63, (int)Cue.StampHeavy);
            Assert.AreEqual(50, (int)Cue.DoorKnock);
            Assert.AreEqual(51, (int)Cue.DoorOpen);
            Assert.AreEqual(62, (int)Cue.DoorClose);
        }

        /// <summary>
        /// PLAN §1.11 (eleştiri 3): belge çekmeceye 0,35 s, tezgâh katlanışı 0,08 + 0,35 + 0,15 s, çark kanat açılmadan önce (0,93 s),
        /// çark 0,5, kanat 0,8, bekleme 0,6 → kapanış 2,83 s'de. Süreler token'dan.
        /// </summary>
        [Test]
        public void DecisionTimeline_FromTokens()
        {
            var t = DecisionTimeline.From(Theme);
            Assert.AreEqual(Theme.NumberOf("admission.motion.drawerS", -1), t.DocsS, 1e-6f);
            Assert.AreEqual(0.35f, t.DocsS, 1e-5f);
            Assert.AreEqual(0.58f, t.FoldS, 1e-5f, "lampS 0,08 + counterFoldS 0,35 + counterBracketS 0,15");
            Assert.AreEqual(0.93f, t.OpenAtS, 1e-5f, "kanat tezgâh katlandıktan sonra");
            Assert.AreEqual(Theme.NumberOf("admission.motion.admitWheelS", -1), t.WheelS, 1e-6f);
            Assert.AreEqual(Theme.NumberOf("admission.motion.admitLeafS", -1), t.LeafS, 1e-6f);
            Assert.AreEqual(Theme.NumberOf("admission.motion.admitHoldS", -1), t.HoldS, 1e-6f);
            Assert.AreEqual(2.83f, t.CloseAtS, 1e-5f);
            Assert.Greater(t.CloseCapS, 0.6f, "kapanışın oturması için üst sınır kanadın 0,6 s'sinden uzun");

            var fallback = DecisionTimeline.From(null);
            Assert.AreEqual(t.OpenAtS, fallback.OpenAtS, 1e-5f, "temasız yedek değerler token'la aynı");
            Assert.AreEqual(t.CloseAtS, fallback.CloseAtS, 1e-5f);
        }

        /// <summary>PLAN §1.5: K8 ↔ K8A motion.poseS (0,45), K8A ↔ K8R admission.motion.readS (0,25). K8G aşama 7'de emekli: varsayılan süre.</summary>
        [Test]
        public void PoseSeconds_K8Family()
        {
            var camGo = new GameObject("Camera", typeof(Camera));
            created.Add(camGo);
            var rig = camGo.AddComponent<SiginakCameraRig>();
            rig.Configure(camGo.GetComponent<Camera>(), null, Array.Empty<SiginakCameraRig.PoseSlot>(), new Vector2(36, 20.25f), 0.45f, 0.15f);
            float read = Theme.NumberOf("admission.motion.readS", -1);
            Assert.AreEqual(0.25f, read, 1e-5f, "admission.motion.readS (_local_a6)");
            Assert.AreEqual(0.45f, rig.PoseSeconds(SiginakPose.Door, SiginakPose.Admission), 1e-6f, "K8 → K8A");
            Assert.AreEqual(0.45f, rig.PoseSeconds(SiginakPose.Admission, SiginakPose.Door), 1e-6f, "K8A → K8");
            Assert.AreEqual(0.45f, rig.PoseSeconds(SiginakPose.Admission, SiginakPose.BreathGlass), 1e-6f, "K8G'nin özel süresi yok (emekli)");
            Assert.AreEqual(read, rig.PoseSeconds(SiginakPose.Admission, SiginakPose.AdmissionRead), 1e-6f, "K8A → K8R");
            Assert.AreEqual(read, rig.PoseSeconds(SiginakPose.AdmissionRead, SiginakPose.Admission), 1e-6f, "K8R → K8A");
            Assert.AreEqual(0.45f, rig.PoseSeconds(SiginakPose.Desk, SiginakPose.Door), 1e-6f, "K1 → K8 değişmedi");
            Assert.AreEqual(Theme.NumberOf("motion.board.backToDeskS", -1), rig.PoseSeconds(SiginakPose.Shelf, SiginakPose.Door), 1e-6f,
                "K4'ten çıkış kuralı önce gelir");
        }

        /// <summary>PLAN §1.5: kenar satırı kartlı oyunda K8, K8A, K8R'de gizli (eski yolun K8'i değişmez); K8 ailesinde KAPI tuşu yanık. K8G aileden çıktı (aşama 7).</summary>
        [Test]
        public void EdgeLine_HiddenInK8Family()
        {
            foreach (SiginakPose pose in Enum.GetValues(typeof(SiginakPose)))
            {
                bool family = pose == SiginakPose.Door || pose == SiginakPose.Admission || pose == SiginakPose.AdmissionRead;
                Assert.AreEqual(family, SiginakAdmission.InFamily(pose), pose + " K8 ailesi");
                // KOS aşama 6: son şalterin KLS pozunda da kenar satırı gizli
                bool always = pose == SiginakPose.Shelf || pose == SiginakPose.ShiftClose || pose == SiginakPose.LastSwitch;
                Assert.AreEqual(always || family, SiginakDesk.EdgeHidden(pose, true), pose + " kartlı oyun");
                Assert.AreEqual(always, SiginakDesk.EdgeHidden(pose, false), pose + " eski yol");
                if (family) Assert.AreEqual(StationKeyRail.Door, SiginakDesk.StationOf(pose), pose + " KAPI tuşu");
            }
            Assert.AreEqual(StationKeyRail.Desk, SiginakDesk.StationOf(SiginakPose.Desk));
            Assert.AreEqual(StationKeyRail.Shelter, SiginakDesk.StationOf(SiginakPose.Shelf));
            Assert.AreEqual(-1, SiginakDesk.StationOf(SiginakPose.ShiftClose));
            Assert.AreEqual(-1, SiginakDesk.StationOf(SiginakPose.BreathGlass), "emekli K8G bir istasyon değil");
        }

        /// <summary>PLAN §1.5 (eleştiri 18): KAYITTAN DEVAM K8 ailesinin hiçbir pozuna dönmez; masaya (K1) döner.</summary>
        [Test]
        public void SavedPose_K8FamilyToDesk()
        {
            foreach (var pose in new[] { SiginakPose.Door, SiginakPose.Admission, SiginakPose.BreathGlass, SiginakPose.AdmissionRead })
                Assert.AreEqual(SiginakPose.Desk, SiginakDesk.SavedPose(pose), pose.ToString());
            Assert.AreEqual(SiginakPose.Map, SiginakDesk.SavedPose(SiginakPose.Map));
            Assert.AreEqual(SiginakPose.Shelf, SiginakDesk.SavedPose(SiginakPose.Shelf));
        }

        // ---------------------------------------------------------------- durum ve vuruş

        /// <summary>Yeni oyun (R9a boş kapı: tezgâh katlı); vuruş yok; adaysız kapı Esc'i tüketmez.</summary>
        [Test]
        public void Restore_NewGame_EmptyDoor_NoKnock()
        {
            var f = Build(toCandidate: false);
            Assert.IsNull(f.Group, "gün 0 kapı boş");
            Assert.IsNull(f.Admission.Ui.ReplaceId);
            Assert.IsEmpty(f.Sounds);
            Assert.IsFalse(f.Admission.Busy);
            Assert.IsFalse(f.Admission.Active, "kamera K1'de");
            Assert.IsFalse(f.Admission.HandleEscape(), "K1'de Esc ayarlara kalır");
            Assert.IsNotNull(f.Admission.Last, "anlık görüntü kuruldu");
            Assert.IsNull(f.Admission.Last.Group, "grup yok");
            Assert.AreEqual(CounterState.Folded, f.Admission.Last.Counter, "tezgâh katlı");
        }

        /// <summary>
        /// PLAN §1.4 (D12), KOS §7: canlı oyunda yeni aday grubu bir kez vurur (lamba amber), aynı grup için ikinci vuruş yok (bekleme
        /// vardiyasında da); sonraki grup yine bir kez; kayıttan devam vurmaz. Vuruş komut yazmaz.
        /// </summary>
        [Test]
        public void Knock_OncePerGroup_NotOnLoad()
        {
            var f = Build(toCandidate: false, withDoor: true, knockMin: 3, knockMax: 3);
            Assert.IsFalse(f.Door.Knocking);
            for (int i = 0; i < 8 && f.Group == null; i++) { Close(f.Game); f.Admission.Refresh(); }
            Assert.IsNotNull(f.Group);
            Assert.AreEqual(1, f.Knocks, "yeni grup bir kez vurdu");
            Assert.IsTrue(f.Door.Knocking, "vuruştan sonra amber");
            int commands = f.Commands;
            f.Admission.Refresh();
            f.Admission.Refresh();
            Assert.AreEqual(1, f.Knocks, "aynı grup için ikinci vuruş yok");
            Assert.AreEqual(commands, f.Commands, "vuruş komut yazmaz");

            // Bekleme vardiyası: grup aynı, vuruş yok
            string seen = f.Group;
            Close(f.Game);
            f.Admission.Refresh();
            Assert.AreEqual(seen, f.Group, "kurulum: grup ikinci vardiyasını bekliyor");
            Assert.AreEqual(1, f.Knocks, "bekleyen grup yeniden vurmaz");

            for (int i = 0; i < 8 && (f.Group == null || f.Group == seen); i++) { Close(f.Game); f.Admission.Refresh(); }
            Assert.IsNotNull(f.Group, "kurulum: sonraki grup");
            Assert.AreNotEqual(seen, f.Group);
            Assert.AreEqual(2, f.Knocks, "yeni grup yine bir kez");

            // Kayıttan devam: aynı kayıt yeniden oynatılır (yeni Record); bekleyen grup vurmaz
            Assert.IsTrue(f.Game.ContinueSaved(), "kayıt yüklendi");
            f.Admission.Refresh();
            Assert.AreEqual(2, f.Knocks, "yüklemede vuruş yok");
            f.Admission.Restore();
            f.Admission.Refresh();
            Assert.AreEqual(2, f.Knocks);
        }

        // ---------------------------------------------------------------- damgalar → ChooseCandidate / PassCandidates

        /// <summary>
        /// PLAN §1.11 (D10, eleştiri 3), KOS §2: KABUL bir karta basılınca tam bir <c>candidate_choose</c> yazılır (a = aday, b boş); dizi
        /// sürerken masa ve kamera kilitli; 0,35 s'de kamera K8'e döner, çark tezgâh katlandıktan sonra (0,93 s) döner, kanat açılınca
        /// <c>DoorOpen</c>, beklemeden sonra <c>DoorClose</c>; kapı kapanınca dizi biter, komut sayısı değişmez. Kişi panoda (Boşta), öteki
        /// adaylar gitti. Sonraki damga (grup yok) komutsuz reddedilir.
        /// </summary>
        [Test]
        public void Choose_WritesOneCommand_ThenAdmitSequence()
        {
            var f = Build(withDoor: true);
            AtCounter(f);
            string id = f.First;
            int others = f.Sim.Admission.Group.Count - 1, passed = f.Sim.Admission.Passed.Count;
            Assert.IsNull(f.Sim.CandidateChoiceCheck(id, null), "kurulum: seçim açık");
            Assert.IsNull(f.Sim.Board.Person(id), "aday panoda değil");
            int before = f.Commands;
            uint rng = f.Sim.S.Rng.State;
            Assert.IsTrue(f.Admission.PressStamp(StampChoice.Admit, id));
            Assert.AreEqual(before + 1, f.Commands);
            Assert.AreEqual("candidate_choose", f.LastCommand.kind);
            Assert.AreEqual(id, f.LastCommand.a);
            Assert.IsTrue(string.IsNullOrEmpty(f.LastCommand.b), "boş yer: kimse yollanmaz");
            Assert.AreEqual(rng, f.Sim.S.Rng.State, "seçim RNG çekmez");
            Assert.IsNotNull(f.Sim.Board.Person(id), "kişi panoda");
            Assert.AreEqual("rest", f.Sim.Board.Person(id).Job, "Boşta'ya gelir");
            Assert.AreEqual(0, f.Sim.Admission.Group.Count, "öteki adaylar gitti");
            Assert.AreEqual(passed + others, f.Sim.Admission.Passed.Count);
            Assert.IsTrue(f.Admission.Busy, "dizi sürüyor");
            Assert.IsTrue(f.Rig.Locked, "kamera girdisi dizide kilitli");
            Assert.IsFalse(f.Door.Knocking, "kapı açılırken vuruş lambası sönük");

            var t = DecisionTimeline.From(Theme);
            const float dt = 0.01f;
            float time = 0;
            bool movedBeforeOpen = false, movedAfterOpen = false;
            int guard = 0;
            while (f.Admission.Busy && guard++ < 2000)
            {
                f.Admission.Advance(dt);
                f.Door.Advance(dt);
                time += dt;
                if (time < t.DocsS - 0.02f) Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "kartlar çekmeceye girerken K8A");
                if (time > t.DocsS + 0.02f && f.Admission.Busy) Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "0,35 s'de K8'e");
                if (time < t.OpenAtS - 0.02f && f.Door.Moving) movedBeforeOpen = true;
                if (time > t.OpenAtS + 0.05f && time < t.CloseAtS && f.Door.Moving) movedAfterOpen = true;
            }
            Assert.IsFalse(f.Admission.Busy, "dizi bitti");
            Assert.IsFalse(movedBeforeOpen, "kanat tezgâh katlanmadan açılmaz");
            Assert.IsTrue(movedAfterOpen, "çark ve kanat açıldı");
            Assert.GreaterOrEqual(time, t.CloseAtS, "kapanış beklemeden sonra");
            Assert.LessOrEqual(time, t.CloseAtS + t.CloseCapS + 0.05f);
            Assert.IsFalse(f.Door.IsOpen);
            Assert.IsFalse(f.Rig.Locked, "kilit kalktı");
            Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "kabulden sonra K8");
            var doorSounds = f.Sounds.Where(s => s == AdmissionSound.Open || s == AdmissionSound.Close).ToList();
            CollectionAssert.AreEqual(new[] { AdmissionSound.Open, AdmissionSound.Close }, doorSounds, "açılış ve kapanış birer kez");
            Assert.AreEqual(before + 1, f.Commands, "dizi komut yazmaz");
            Assert.AreEqual(0, f.Knocks, "karar vuruş çalmaz");

            Assert.IsNull(f.Group);
            Assert.IsNull(f.Admission.Last.Group, "dizi bitince grup görüntüde yok");
            Assert.IsFalse(f.Admission.PressStamp(StampChoice.Reject, null), "grup yok");
            Assert.IsFalse(f.Admission.PressStamp(StampChoice.Admit, id), "aynı aday ikinci kez alınamaz");
            Assert.AreEqual(before + 1, f.Commands);
            Assert.AreEqual(AdmissionSound.Deny, f.Sounds.Last());
        }

        /// <summary>
        /// D10, KOS §2: KABUL'de (<c>candidate_choose</c>) kapı açılır ve kamera K8'e döner; GERİ ÇEVİR'de (<c>candidate_pass</c>, tam bir
        /// komut, bütün grup gider, ceza yok) kapı kapalı kalır, kamera K8A'da. Her ikisi de tam bir komut.
        /// </summary>
        [TestCase(StampChoice.Admit, "candidate_choose", true)]
        [TestCase(StampChoice.Reject, "candidate_pass", false)]
        public void Decide_DoorOnlyForAdmission(StampChoice choice, string kind, bool opens)
        {
            var f = Build(withDoor: true);
            AtCounter(f);
            string id = f.First;
            int size = f.Sim.Admission.Group.Count, people = f.Sim.Board.People.Count;
            double supplies = f.Sim.Shelter.Supplies;
            int before = f.Commands;
            Assert.IsTrue(f.Admission.PressStamp(choice, opens ? id : null));
            Assert.AreEqual(before + 1, f.Commands);
            Assert.AreEqual(kind, f.LastCommand.kind);
            Assert.AreEqual(0, f.Sim.Admission.Group.Count, "kapı boşaldı");
            Assert.AreEqual(people + (opens ? 1 : 0), f.Sim.Board.People.Count);
            Assert.AreEqual(supplies, f.Sim.Shelter.Supplies, 1e-9, "karar erzak harcamaz");
            if (!opens)
            {
                var gone = f.Sim.Admission.Passed.Skip(f.Sim.Admission.Passed.Count - size).ToList();
                Assert.IsTrue(gone.All(p => p.Kind == "pass"), "hepsi geri çevrildi");
            }
            bool moved = false;
            for (int i = 0; i < 600 && f.Admission.Busy; i++)
            {
                f.Admission.Advance(0.01f);
                f.Door.Advance(0.01f);
                moved |= f.Door.Moving;
            }
            Assert.IsFalse(f.Admission.Busy);
            Assert.AreEqual(opens, moved, opens ? "kapı açıldı" : "kapı kapalı kaldı");
            Assert.AreEqual(opens ? SiginakPose.Door : SiginakPose.Admission, f.Rig.Current);
            Assert.AreEqual(before + 1, f.Commands);
            Assert.IsNull(f.Admission.Ui.ReplaceId, "işaret sıfır");
        }

        /// <summary>
        /// KOS §2 (dolu sığınak): kapasite 8'de (kurucularla dolu) KABUL işaretsizken komutsuz reddedilir. Fişteki satıra tık kişiyi
        /// işaretler (yalnız sunum durumu; komut yok, ikinci tık kaldırır); sonra KABUL tek komutla adayı alır ve işaretli kişiyi yolluğuyla
        /// gönderir (b = kişi). GERİ ÇEVİR dolu sığınakta da açıktır.
        /// </summary>
        [Test]
        public void Full_ReplaceThenChoose_OneCommand()
        {
            var f = Build(capacity: 8);
            AtCounter(f);
            var sim = f.Sim;
            Assert.AreEqual(8, sim.Board.People.Count(p => p.Alive), "kurulum: sığınak dolu");
            string id = f.First;
            var leaver = sim.Board.People.Last(p => p.Alive);
            int before = f.Commands;
            Assert.IsFalse(f.Admission.PressStamp(StampChoice.Admit, id), "işaretsiz KABUL");
            Assert.AreEqual(before, f.Commands);
            Assert.AreEqual(AdmissionSound.Deny, f.Sounds.Last());
            Assert.IsTrue(f.Admission.Last.Group.Full);
            Assert.IsFalse(f.Admission.Last.Stamps[(int)StampChoice.Admit].Enabled);
            Assert.IsTrue(f.Admission.Last.Stamps[(int)StampChoice.Reject].Enabled, "geri çevirme dolu sığınakta açık");

            f.Admission.ClickReplace(leaver.Id);
            Assert.AreEqual(before, f.Commands, "işaret komut yazmaz");
            Assert.AreEqual(leaver.Id, f.Admission.Ui.ReplaceId);
            Assert.AreEqual(leaver.Id, f.Admission.Last.Group.ReplaceId);
            Assert.IsTrue(f.Admission.Last.Group.Replace.Single(r => r.Id == leaver.Id).Selected);
            Assert.IsTrue(f.Admission.Last.Stamps[(int)StampChoice.Admit].Enabled, "işaretle KABUL açıldı");
            f.Admission.ClickReplace(leaver.Id);
            Assert.IsNull(f.Admission.Ui.Replace(f.Group), "ikinci tık işareti kaldırır");
            Assert.IsNull(f.Admission.Last.Group.ReplaceId);
            f.Admission.ClickReplace(leaver.Id);
            Assert.AreEqual(before, f.Commands);

            double supplies = sim.Shelter.Supplies, rations = sim.DismissSuppliesPreview;
            Assert.Greater(rations, 0, "kurulum: yolluk");
            Assert.IsTrue(f.Admission.PressStamp(StampChoice.Admit, id));
            Assert.AreEqual(before + 1, f.Commands, "tek komut");
            Assert.AreEqual("candidate_choose", f.LastCommand.kind);
            Assert.AreEqual(id, f.LastCommand.a);
            Assert.AreEqual(leaver.Id, f.LastCommand.b);
            Assert.IsNull(sim.Board.Person(leaver.Id), "yollanan panoda değil");
            Assert.IsNotNull(sim.Board.Person(id));
            Assert.AreEqual(8, sim.Board.People.Count(p => p.Alive));
            Assert.AreEqual(supplies - rations, sim.Shelter.Supplies, 1e-9, "yolluk düştü");
            Assert.IsNull(f.Admission.Ui.ReplaceId, "karardan sonra işaret sıfır");
            Assert.IsTrue(f.Admission.Busy);
        }

        /// <summary>
        /// Dolu sığınakta kişi işaretlendikten sonra yer açılırsa (tahliye, ölüm) eski işaret KABUL'ü kilitlemez: işaret sunum durumundan
        /// silinir, KABUL yollanacak kişi olmadan tek <c>candidate_choose</c> yazar. Çizimden kaçmış bayat bir işaret de Core'a gitmez
        /// (komutun kaynağı görüntünün doğruladığı değerdir).
        /// </summary>
        [Test]
        public void Full_MarkThenRoomOpens_AdmitWithoutReplace()
        {
            var f = Build(capacity: 8);
            AtCounter(f);
            var sim = f.Sim;
            string id = f.First;
            var alive = sim.Board.People.Where(p => p.Alive).ToList();
            var marked = alive[alive.Count - 1];
            var other = alive[alive.Count - 2];
            f.Admission.ClickReplace(marked.Id);
            Assert.AreEqual(marked.Id, f.Admission.Last.Group.ReplaceId, "kurulum: işaretli");

            // Yer açıldı: başka biri öldü (test düzeni; eski serbest yollama aşama 7'de silindi)
            other.Alive = false;
            other.Hp = 0;
            f.Admission.Refresh();   // oyunda GameController.Changed çağırır
            Assert.AreEqual(7, sim.Board.People.Count(p => p.Alive));
            Assert.IsFalse(f.Admission.Last.Group.Full, "yer var");
            Assert.IsNull(f.Admission.Last.Group.ReplaceId);
            Assert.AreEqual(0, f.Admission.Last.Group.Replace.Count, "yollanacak listesi çizilmez");
            Assert.IsTrue(f.Admission.Last.Group.Cards.All(c => c.CanChoose), "kartlar seçilebilir");
            Assert.IsNull(f.Admission.Ui.ReplaceId, "bayat işaret sunum durumundan silindi");
            Assert.IsNull(f.Admission.Ui.ReplaceForGroup);

            // Çizimden kaçmış bayat işaret (savunma): komut yine görüntünün değerini kullanır
            f.Admission.Ui.ReplaceId = marked.Id;
            f.Admission.Ui.ReplaceForGroup = f.Group;
            int before = f.Commands;
            Assert.IsTrue(f.Admission.PressStamp(StampChoice.Admit, id), "yer açılınca KABUL işler");
            Assert.AreEqual(before + 1, f.Commands, "tek komut");
            Assert.AreEqual("candidate_choose", f.LastCommand.kind);
            Assert.AreEqual(id, f.LastCommand.a);
            Assert.IsTrue(string.IsNullOrEmpty(f.LastCommand.b), "kimse yollanmadı");
            Assert.IsNotNull(sim.Board.Person(marked.Id), "işaretli kişi sığınakta kaldı");
            Assert.IsNotNull(sim.Board.Person(id));
            Assert.AreEqual(8, sim.Board.People.Count(p => p.Alive));
            Assert.IsNull(f.Admission.Ui.ReplaceId);
        }

        /// <summary>
        /// PLAN §1.10, KOS §2: fiş basılıyken, baskında ve hikâye açıkken hiçbir damga komut yazmaz (Core denetimi; ret sesi). Başka
        /// seçim (eski KISITLI ve YARDIM sayıları) kilit yokken de komut yazmaz.
        /// </summary>
        [Test]
        public void Locked_NoCommand()
        {
            var f = Build();
            AtCounter(f);
            string id = f.First;
            int before = f.Commands;
            foreach (var retired in new[] { StampChoice.Restricted, StampChoice.Aid })
            {
                Assert.IsFalse(f.Admission.PressStamp(retired, id), retired + " kullanılmıyor");
                Assert.AreEqual(AdmissionSound.Deny, f.Sounds.Last());
            }
            Assert.IsFalse(f.Admission.PressStamp(StampChoice.Admit, null), "hedef kart yok");
            Assert.IsFalse(f.Admission.PressStamp(StampChoice.Admit, "resident-yok"), "kapıda olmayan aday");
            Assert.AreEqual(before, f.Commands, "kilit yokken de komut yok");

            Assert.IsTrue(f.Game.PrintBoardShift().Ok, "fiş basıldı: kapı bu vardiya kapalı");
            before = f.Commands;
            foreach (StampChoice c in Enum.GetValues(typeof(StampChoice))) Assert.IsFalse(f.Admission.PressStamp(c, id), c + " fiş basılı");
            Assert.AreEqual(before, f.Commands, "fiş basılıyken komut yok");
            Assert.AreEqual(AdmissionSound.Deny, f.Sounds.Last());
            Assert.IsFalse(f.Admission.Busy);
            Assert.IsNotNull(f.Group, "grup yerinde");

            var raid = Build();
            AtCounter(raid);
            Assert.IsTrue(raid.Game.StartLiberationRaid("weu_1").Ok, "kurulum: baskın");
            before = raid.Commands;
            uint rng = raid.Sim.S.Rng.State;
            Assert.IsFalse(raid.Admission.PressStamp(StampChoice.Admit, raid.First), "baskında");
            Assert.IsFalse(raid.Admission.PressStamp(StampChoice.Reject, null), "baskında");
            Assert.AreEqual(before, raid.Commands, "baskında komut yok");
            Assert.AreEqual(rng, raid.Sim.S.Rng.State);

            var story = Build();
            AtCounter(story);
            story.Game.StoryHold = true;
            before = story.Commands;
            Assert.IsFalse(story.Admission.PressStamp(StampChoice.Admit, story.First), "hikâye açıkken");
            Assert.IsFalse(story.Admission.PressStamp(StampChoice.Reject, null), "hikâye açıkken");
            Assert.AreEqual(before, story.Commands);
        }

        // ---------------------------------------------------------------- geri zinciri

        /// <summary>
        /// PLAN §1.5 (D13), KOS §7: K8 → K8A (inik tezgâha tık) → aday kartı K8R (karta tık) → Esc K8A → grup fişi K8R → ikinci tık K8A →
        /// geri K8 → K8A → KABUL dizisi → geri diziyi sona atlar (K8) → geri K1; K1'de Esc kabulü tüketmez. Hiçbir poz adımı komut yazmaz.
        /// </summary>
        [Test]
        public void HandleBack_Chain()
        {
            var f = Build(withDoor: true);
            f.Admission.Enter();
            Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "KAPI → K8");
            Assert.IsTrue(f.Admission.Active);
            int before = f.Commands;

            f.Admission.ClickCounter();
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "inik tezgâh → K8A");
            f.Admission.ClickPaper(AdmissionPaper.Candidate, 1);
            Assert.AreEqual(SiginakPose.AdmissionRead, f.Rig.Current, "aday kartı doğrudan okunur (K8R)");
            Assert.IsTrue(f.Admission.HandleEscape());
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "K8R → K8A");
            f.Admission.ClickPaper(AdmissionPaper.GroupSlip, 0);
            Assert.AreEqual(SiginakPose.AdmissionRead, f.Rig.Current, "grup fişi → K8R");
            f.Admission.ClickPaper(AdmissionPaper.GroupSlip, 0);
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "K8R'de ikinci tık K8A'ya döner");
            Assert.AreEqual(before, f.Commands, "okuma komut yazmaz");

            f.Admission.HandleBack();
            Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "K8A → K8");
            f.Admission.ClickPaper(AdmissionPaper.Candidate, 0);
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "K8'de kâğıda tık tezgâha tık gibi");

            Assert.IsTrue(f.Admission.PressStamp(StampChoice.Admit, f.First));
            Assert.IsTrue(f.Admission.Busy);
            Assert.IsTrue(f.Admission.HandleEscape(), "Esc diziyi sona atlatır");
            Assert.IsFalse(f.Admission.Busy);
            Assert.IsFalse(f.Rig.Locked);
            Assert.IsFalse(f.Door.Moving, "kapı kapalı oturdu");
            Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "atlanan kabul dizisi K8'de biter");
            Assert.AreEqual(before + 1, f.Commands, "atlama komut yazmaz");

            f.Admission.HandleBack();
            Assert.AreEqual(SiginakPose.Desk, f.Rig.Current, "K8 → K1");
            Assert.IsFalse(f.Admission.Active);
            Assert.IsFalse(f.Admission.HandleEscape(), "K1'de Esc tüketilmez");
        }

        /// <summary>MASA tuşu K8 ailesinin her pozundan K1'e döner; komut yazmaz.</summary>
        [Test]
        public void DeskKey_LeavesFromCounterAndRead()
        {
            var f = Build();
            int before = f.Commands;
            f.Admission.Enter();
            f.Admission.ClickCounter();
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current);
            f.Admission.ClickDeskKey();
            Assert.AreEqual(SiginakPose.Desk, f.Rig.Current, "MASA → K1 (K8A'dan)");
            f.Admission.Enter();
            f.Admission.ClickCounter();
            f.Admission.ClickPaper(AdmissionPaper.Candidate, 0);
            Assert.AreEqual(SiginakPose.AdmissionRead, f.Rig.Current);
            f.Admission.ClickDeskKey();
            Assert.AreEqual(SiginakPose.Desk, f.Rig.Current, "MASA → K1 (K8R'den)");
            Assert.AreEqual(before, f.Commands);
        }

        // ---------------------------------------------------------------- pano API'si ve İNCELE uyarısı

        /// <summary>D20: <c>ShowAdmissions</c> / <c>CloseAdmissions</c> genel API olarak kalır ve 3B kabule gider; pano yalnız K4'te açıktır.</summary>
        [Test]
        public void PresenterApi_Kept()
        {
            var show = typeof(PlanningBoardPresenter).GetMethod("ShowAdmissions", BindingFlags.Public | BindingFlags.Instance);
            var close = typeof(PlanningBoardPresenter).GetMethod("CloseAdmissions", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(show, "ShowAdmissions public");
            Assert.IsNotNull(close, "CloseAdmissions public");
            Assert.IsNotNull(typeof(PlanningBoardPresenter).GetProperty("View"), "View erişimcisi kalır");

            var f = Build();
            var presenter = Make<PlanningBoardPresenter>("Planning board");
            Set(presenter, "game", f.Game);
            Set(presenter, "surface", f.Surface);
            Set(presenter, "rig", f.Rig);
            Set(presenter, "admission", f.Admission);
            presenter.ShowAdmissions();
            Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "ShowAdmissions → K8");
            Assert.IsFalse(presenter.IsOpen, "kabul panonun parçası değil");
            Assert.IsFalse(presenter.HandleBack(), "pano kapalıyken geri adım panonun değil");
            presenter.CloseAdmissions();
            Assert.AreEqual(SiginakPose.Desk, f.Rig.Current, "CloseAdmissions → K1");
        }

        // ---------------------------------------------------------------- yan dosya, teleks, varış

        /// <summary>
        /// Dolu sığınakta işaretlenen kişi yan dosyaya yazılmaz: kayıttan devamda seçim sıfırdır, bekleyen grup aynıdır ve vurmaz. Eski
        /// kabulün görülen durumu (talimat sürümü, belge, dış kayıt sonucu) aşama 7'de söküldü; yan dosyada alanı yok.
        /// </summary>
        [Test]
        public void Replace_NotSaved_ContinueClearsIt()
        {
            var f = Build(capacity: 8);
            AtCounter(f);
            string key = f.Group;
            f.Admission.ClickReplace(f.Sim.Board.People.First(p => p.Alive).Id);
            Assert.IsNotNull(f.Admission.Ui.Replace(key), "kurulum: işaretli kişi");
            Assert.IsNull(typeof(SiginakDeskStore).GetNestedType("AdmissionSeen"), "yan dosyada kabul girdisi yok");

            Assert.IsTrue(f.Game.ContinueSaved());
            f.Admission.Restore();
            Assert.AreEqual(key, f.Group, "aynı grup");
            Assert.IsNull(f.Admission.Ui.ReplaceId, "işaret kayıttan devamda sıfır");
            Assert.IsNull(f.Admission.Last.Group.ReplaceId);
            Assert.IsEmpty(f.Sounds.Where(s => s == AdmissionSound.Knock), "yüklemede vuruş yok");
        }

        /// <summary>
        /// KOS §7 (teleks): yeni grup kapanıştan sonra bir kez "KAPI · n aday kapıda. Bekleme: 2 vardiya." (Info) basar; bekleme
        /// vardiyasında satır yok; karar verilmeden süre dolunca bir kez "Adaylar karar beklemeden gitti."; geri çevirme teleks yazmaz.
        /// Talimat artık değişmez: "TALİMAT n" satırı hiç basılmaz.
        /// </summary>
        [Test]
        public void Telex_GroupArrival_ThenExpiry()
        {
            var f = Build(toCandidate: false, knockMin: 3, knockMax: 3);
            var lines = new List<(string, string)>();
            f.Admission.TelexLine += (a, b) => lines.Add((a, b));
            void Shift()
            {
                Close(f.Game);
                f.Admission.Refresh();
                f.Admission.Advance(0);
            }
            for (int i = 0; i < 8 && f.Group == null; i++) Shift();
            Assert.IsNotNull(f.Group, "kurulum: grup");
            var adm = f.Sim.Admission;
            Assert.AreEqual(1, lines.Count, "bir satır");
            Assert.AreEqual("KAPI", lines[0].Item1);
            Assert.AreEqual(Loc.F("{0} aday kapıda. Bekleme: {1} vardiya.", adm.Group.Count, 2), lines[0].Item2);
            var telexLine = f.Telex.Lines.Last();
            Assert.AreEqual("KAPI", telexLine.status);
            Assert.AreEqual(TelexKind.Info, telexLine.kind);
            f.Admission.Refresh();
            f.Admission.Advance(0);
            Assert.AreEqual(1, lines.Count, "aynı grup ikinci kez basılmaz");

            Shift();
            Assert.IsNotNull(f.Group, "bekleme vardiyası");
            Assert.AreEqual(1, lines.Count, "beklerken satır yok");
            Shift();
            Assert.IsNull(f.Group, "süre doldu");
            Assert.AreEqual("expired", adm.Passed.Last().Kind);
            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual(("KAPI", "Adaylar karar beklemeden gitti."), lines[1]);
            f.Admission.Refresh();
            f.Admission.Advance(0);
            Assert.AreEqual(2, lines.Count, "gidiş bir kez");

            // Sonraki grup: geliş satırı; geri çevirme satır yazmaz
            for (int i = 0; i < 8 && f.Group == null; i++) Shift();
            Assert.IsNotNull(f.Group, "kurulum: ikinci grup");
            Assert.AreEqual(3, lines.Count);
            AtCounter(f);
            Assert.IsTrue(f.Admission.PressStamp(StampChoice.Reject, null));
            for (int i = 0; i < 200 && f.Admission.Busy; i++) f.Admission.Advance(0.01f);
            f.Admission.Refresh();
            f.Admission.Advance(0);
            Assert.AreEqual(3, lines.Count, "geri çevirme teleks yazmaz");
            Assert.IsFalse(lines.Any(l => l.Item1.StartsWith("TALİMAT")), "talimat emekli");

            DeskLoc.Instance.SetLanguage(DeskLang.En);
            f.Rig.Snap(SiginakPose.Desk);
            for (int i = 0; i < 8 && f.Group == null; i++) Shift();
            Assert.AreEqual(4, lines.Count);
            Assert.AreEqual("DOOR", lines[3].Item1);
            Assert.AreEqual(f.Sim.Admission.Group.Count + " candidates at the door. Waiting: 2 shifts.", lines[3].Item2);
        }

        /// <summary>
        /// Aynı kapanışta gidiş ve geliş (kapı her vardiya çalarsa ya da acil grup gelirse): süresi dolan grubun "gitti" satırı kaybolmaz,
        /// yeni grubun geliş satırından önce basılır; kapı yeni grup için bir kez vurur.
        /// </summary>
        [Test]
        public void Telex_ExpiryAndArrival_InTheSameClosing()
        {
            var f = Build(toCandidate: false, withDoor: true);
            var lines = new List<(string, string)>();
            f.Admission.TelexLine += (a, b) => lines.Add((a, b));
            void Shift()
            {
                Close(f.Game);
                f.Admission.Refresh();
                f.Admission.Advance(0);
            }
            Shift();
            string first = f.Group;
            Assert.IsNotNull(first, "kurulum: ilk grup");
            Assert.AreEqual(1, lines.Count);
            Shift();
            Assert.AreEqual(first, f.Group, "bekleme vardiyası");
            Assert.AreEqual(1, lines.Count);
            Shift();
            Assert.IsNotNull(f.Group, "kurulum: aynı kapanışta yeni grup");
            Assert.AreNotEqual(first, f.Group);
            Assert.AreEqual(3, lines.Count, "gidiş + geliş");
            Assert.AreEqual(("KAPI", "Adaylar karar beklemeden gitti."), lines[1]);
            Assert.AreEqual(("KAPI", Loc.F("{0} aday kapıda. Bekleme: {1} vardiya.", f.Sim.Admission.Group.Count, 2)), lines[2]);
            Assert.AreEqual(2, f.Knocks, "yeni grup bir kez vurdu");
            f.Admission.Refresh();
            f.Admission.Advance(0);
            Assert.AreEqual(3, lines.Count, "yinelenmez");

            // Geri çevrilen grubun ardından gelen grup "gitti" yazmaz (karar verildi)
            AtCounter(f);
            Assert.IsTrue(f.Admission.PressStamp(StampChoice.Reject, null));
            for (int i = 0; i < 200 && f.Admission.Busy; i++) f.Admission.Advance(0.01f);
            f.Rig.Snap(SiginakPose.Desk);
            Shift();
            Assert.IsNotNull(f.Group);
            Assert.AreEqual(4, lines.Count, "yalnız geliş satırı");
            StringAssert.Contains("aday kapıda", lines[3].Item2);
        }

        /// <summary>PLAN §1.10, E7: kullanılan iki damganın izi ve rengi, dile göre; KISITLI ve YARDIM izi yok (eski karar yolu silindi).</summary>
        [Test]
        public void ImpressionLines_AdmitAndReject()
        {
            var b = new Balance();
            CollectionAssert.AreEqual(new[] { "KABUL", "TAM ERİŞİM" }, SiginakAdmission.ImpressionLines(StampChoice.Admit, b));
            CollectionAssert.AreEqual(new[] { "REDDET" }, SiginakAdmission.ImpressionLines(StampChoice.Reject, b));
            Assert.IsEmpty(SiginakAdmission.ImpressionLines(StampChoice.Restricted, b));
            Assert.IsEmpty(SiginakAdmission.ImpressionLines(StampChoice.Aid, b));
            Assert.AreEqual("#24592E", SiginakAdmission.ImpressionColor(StampChoice.Admit), "admission.stampMarks.admit.color");
            Assert.AreEqual("#9A1E14", SiginakAdmission.ImpressionColor(StampChoice.Reject));

            DeskLoc.Instance.SetLanguage(DeskLang.En);
            CollectionAssert.AreEqual(new[] { "ADMIT", "FULL ACCESS" }, SiginakAdmission.ImpressionLines(StampChoice.Admit, b));
        }

        /// <summary>AGENTS kural 5: akışın dosyaları obje adıyla arama yapmaz (bağlar <c>[SerializeField]</c>).</summary>
        [Test]
        public void NoNameLookups()
        {
            var files = new[]
            {
                "Assets/Corrigible/Desk/Scripts/Bridge/Siginak/SiginakAdmission.cs",
                "Assets/Corrigible/Desk/Scripts/Bridge/Siginak/SiginakDesk.cs",
                "Assets/Corrigible/Desk/Scripts/Bridge/PlanningBoardPresenter.cs",
                "Assets/Corrigible/Desk/Scripts/Bridge/LiberationDeskView.cs",
            };
            foreach (var path in files)
            {
                Assert.IsTrue(File.Exists(path), path);
                var source = File.ReadAllText(path);
                foreach (var banned in new[] { "Find(\"", "GameObject.Find", "FindObjectOfType", "FindObjectsOfType", "FindFirstObjectByType", "FindAnyObjectByType" })
                    Assert.IsFalse(source.Contains(banned), path + ": " + banned);
            }
            // Robotluk sunuma geçmez (PLAN §0.1): akış gizli kimliği okumaz
            var flow = File.ReadAllText(files[0]);
            foreach (var hidden in new[] { "Identity", ".Robot", "PaperFault", "ActiveShifts", "Sabotaged", "Rng" })
                Assert.IsFalse(flow.Contains(hidden), "SiginakAdmission: " + hidden);
        }
    }
}
