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
    /// süreleri, kenar satırı ve kayıt pozu, vuruşun aday başına bir kez (yüklemede yok), her aracın ve damganın tam bir Core komutu,
    /// kilitte komut yok, geri zinciri, panonun eski API'si, görülen durumun yan dosyası, "TALİMAT n" teleksi, İNCELE sekmesinin H2/H4
    /// uyarısı ve ad araması yapılmadığı. Sahneye bağlı değildir (istasyon görünümleri bağlanmaz; akış Core'a göre çalışır); kayıtlar
    /// geçici klasöre yazılır.
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
            public string Candidate => Game.Sim.Admission?.Candidate?.Resident?.Id;
            public int Knocks => Sounds.Count(s => s == AdmissionSound.Knock);
        }

        /// <summary>
        /// Gerçek kartlı oyun (GameController, tohum 79; her vardiya bir aday, alarm kapalı) ve ona bağlı kabul akışı: kamera (K1, K8, K8A,
        /// K8G, K8R çapalarıyla), masa yüzü, teleks, isteğe bağlı kapı. <paramref name="toCandidate"/>: ilk aday gelene dek kapanış.
        /// </summary>
        Flow Build(bool toCandidate = true, bool withDoor = false)
        {
            var f = new Flow();
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            created.Add(config);
            config.balance = new Balance
            {
                planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1,
            };
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
                new SiginakCameraRig.PoseSlot { pose = SiginakPose.BreathGlass, anchor = Anchor("K8G", new Vector3(1.62f, 1.52f, 0.40f), new Vector3(6, 70, 0)), fieldOfView = 30 },
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
                for (int i = 0; i < 6 && f.Candidate == null; i++) Close(f.Game);
            f.Admission.Restore();
            if (toCandidate) Assert.IsNotNull(f.Candidate, "kurulum: kapıda aday");
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

        /// <summary>PLAN §1.13: her kabul sesi açıkça eşlenir; yeni cue'lar 120–128, var olanlar damga 63, dönüş 99, ret 100, kâğıt 57, kapı 50/51/62.</summary>
        [Test]
        public void CueOf_MapsEveryAdmissionSound()
        {
            var expected = new Dictionary<AdmissionSound, Cue>
            {
                { AdmissionSound.CounterDown, Cue.AdmissionCounterDown }, { AdmissionSound.Drawer, Cue.AdmissionDrawer },
                { AdmissionSound.TinsOut, Cue.AdmissionDrawer }, { AdmissionSound.Marker, Cue.AdmissionMarker },
                { AdmissionSound.SlipOut, Cue.AdmissionSlip }, { AdmissionSound.IntercomCrackle, Cue.AdmissionIntercom },
                { AdmissionSound.GlassCover, Cue.AdmissionGlass }, { AdmissionSound.Fog, Cue.AdmissionGlass },
                { AdmissionSound.PadTear, Cue.AdmissionPadTear }, { AdmissionSound.StampLift, Cue.AdmissionStampLift },
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

        /// <summary>PLAN §1.5: K8 ↔ K8A motion.poseS (0,45), K8/K8A ↔ K8G admission.motion.toK8GS (0,45), K8A ↔ K8R admission.motion.readS (0,25).</summary>
        [Test]
        public void PoseSeconds_K8Family()
        {
            var camGo = new GameObject("Camera", typeof(Camera));
            created.Add(camGo);
            var rig = camGo.AddComponent<SiginakCameraRig>();
            rig.Configure(camGo.GetComponent<Camera>(), null, Array.Empty<SiginakCameraRig.PoseSlot>(), new Vector2(36, 20.25f), 0.45f, 0.15f);
            float toGlass = Theme.NumberOf("admission.motion.toK8GS", -1), read = Theme.NumberOf("admission.motion.readS", -1);
            Assert.AreEqual(0.45f, toGlass, 1e-5f, "admission.motion.toK8GS");
            Assert.AreEqual(0.25f, read, 1e-5f, "admission.motion.readS (_local_a6)");
            Assert.AreEqual(0.45f, rig.PoseSeconds(SiginakPose.Door, SiginakPose.Admission), 1e-6f, "K8 → K8A");
            Assert.AreEqual(0.45f, rig.PoseSeconds(SiginakPose.Admission, SiginakPose.Door), 1e-6f, "K8A → K8");
            Assert.AreEqual(toGlass, rig.PoseSeconds(SiginakPose.Admission, SiginakPose.BreathGlass), 1e-6f, "K8A → K8G");
            Assert.AreEqual(toGlass, rig.PoseSeconds(SiginakPose.Door, SiginakPose.BreathGlass), 1e-6f, "K8 → K8G");
            Assert.AreEqual(toGlass, rig.PoseSeconds(SiginakPose.BreathGlass, SiginakPose.Admission), 1e-6f, "K8G → K8A");
            Assert.AreEqual(read, rig.PoseSeconds(SiginakPose.Admission, SiginakPose.AdmissionRead), 1e-6f, "K8A → K8R");
            Assert.AreEqual(read, rig.PoseSeconds(SiginakPose.AdmissionRead, SiginakPose.Admission), 1e-6f, "K8R → K8A");
            Assert.AreEqual(0.45f, rig.PoseSeconds(SiginakPose.Desk, SiginakPose.Door), 1e-6f, "K1 → K8 değişmedi");
            Assert.AreEqual(Theme.NumberOf("motion.board.backToDeskS", -1), rig.PoseSeconds(SiginakPose.Shelf, SiginakPose.Door), 1e-6f,
                "K4'ten çıkış kuralı önce gelir");
        }

        /// <summary>PLAN §1.5: kenar satırı kartlı oyunda K8, K8A, K8G, K8R'de gizli (eski yolun K8'i değişmez); K8 ailesinde KAPI tuşu yanık.</summary>
        [Test]
        public void EdgeLine_HiddenInK8Family()
        {
            foreach (SiginakPose pose in Enum.GetValues(typeof(SiginakPose)))
            {
                bool family = pose == SiginakPose.Door || pose == SiginakPose.Admission || pose == SiginakPose.BreathGlass || pose == SiginakPose.AdmissionRead;
                Assert.AreEqual(family, SiginakAdmission.InFamily(pose), pose + " K8 ailesi");
                bool always = pose == SiginakPose.Shelf || pose == SiginakPose.ShiftClose;
                Assert.AreEqual(always || family, SiginakDesk.EdgeHidden(pose, true), pose + " kartlı oyun");
                Assert.AreEqual(always, SiginakDesk.EdgeHidden(pose, false), pose + " eski yol");
                if (family) Assert.AreEqual(StationKeyRail.Door, SiginakDesk.StationOf(pose), pose + " KAPI tuşu");
            }
            Assert.AreEqual(StationKeyRail.Desk, SiginakDesk.StationOf(SiginakPose.Desk));
            Assert.AreEqual(StationKeyRail.Shelter, SiginakDesk.StationOf(SiginakPose.Shelf));
            Assert.AreEqual(-1, SiginakDesk.StationOf(SiginakPose.ShiftClose));
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

        /// <summary>Eleştiri 8: yeni oyunda v1 görülmüş sayılır (R9a boş kapı: tezgâh katlı); vuruş yok; adaysız kapı Esc'i tüketmez.</summary>
        [Test]
        public void Restore_NewGame_SeesCurrentOrders_NoKnock()
        {
            var f = Build(toCandidate: false);
            Assert.IsNull(f.Candidate, "gün 0 kapı boş");
            Assert.AreEqual(f.Sim.Admission.Protocol.Version, f.Admission.Ui.SeenOrdersVersion);
            Assert.AreEqual(1, f.Admission.Ui.SeenOrdersVersion);
            Assert.IsEmpty(f.Sounds);
            Assert.IsFalse(f.Admission.Busy);
            Assert.IsFalse(f.Admission.Active, "kamera K1'de");
            Assert.IsFalse(f.Admission.HandleEscape(), "K1'de Esc ayarlara kalır");
            Assert.IsNotNull(f.Admission.Last, "anlık görüntü kuruldu");
        }

        /// <summary>
        /// PLAN §1.4 (D12): canlı oyunda aday kimliği değişince bir kez vurur (lamba amber), aynı aday için ikinci vuruş yok; kayıttan devam
        /// vurmaz. Vuruş komut yazmaz.
        /// </summary>
        [Test]
        public void Knock_OncePerCandidate_NotOnLoad()
        {
            var f = Build(toCandidate: false, withDoor: true);
            Assert.IsFalse(f.Door.Knocking);
            string seen = null;
            for (int i = 0; i < 6 && f.Candidate == null; i++) { Close(f.Game); f.Admission.Refresh(); }
            Assert.IsNotNull(f.Candidate);
            Assert.AreEqual(1, f.Knocks, "yeni aday bir kez vurdu");
            Assert.IsTrue(f.Door.Knocking, "vuruştan sonra amber");
            int commands = f.Commands;
            f.Admission.Refresh();
            f.Admission.Refresh();
            Assert.AreEqual(1, f.Knocks, "aynı aday için ikinci vuruş yok");
            Assert.AreEqual(commands, f.Commands, "vuruş komut yazmaz");

            seen = f.Candidate;
            for (int i = 0; i < 6 && f.Candidate == seen; i++) { Close(f.Game); f.Admission.Refresh(); }
            Assert.AreNotEqual(seen, f.Candidate, "kurulum: sonraki aday");
            Assert.IsNotNull(f.Candidate);
            Assert.AreEqual(2, f.Knocks, "yeni aday yine bir kez");

            // Kayıttan devam: aynı kayıt yeniden oynatılır (yeni Record); bekleyen aday vurmaz
            Assert.IsTrue(f.Game.ContinueSaved(), "kayıt yüklendi");
            f.Admission.Refresh();
            Assert.AreEqual(2, f.Knocks, "yüklemede vuruş yok");
            f.Admission.Restore();
            f.Admission.Refresh();
            Assert.AreEqual(2, f.Knocks);
        }

        // ---------------------------------------------------------------- araçlar → InspectApplicant

        /// <summary>PLAN §1.3: her araç tam bir <c>inspectApplicant</c> yazar; ikinci kullanım komutsuz ve ret sesli. RNG durumu korunur.</summary>
        [TestCase(AdmissionTool.Papers, "papers")]
        [TestCase(AdmissionTool.Interview, "interview")]
        [TestCase(AdmissionTool.Breath, "breath")]
        [TestCase(AdmissionTool.Record, "record")]
        public void Inspect_WritesExactlyOneCommand(AdmissionTool tool, string kind)
        {
            var f = Build();
            AtCounter(f);
            Assert.IsNull(f.Sim.ApplicantInspectionCheck(kind), "kurulum: inceleme açık (dış kayıtta Boşta'da gözetmen var)");
            int before = f.Commands;
            uint rng = f.Sim.S.Rng.State;
            Assert.IsTrue(f.Admission.UseTool(tool));
            Assert.AreEqual(before + 1, f.Commands, "tam bir komut");
            Assert.AreEqual("inspectApplicant", f.LastCommand.kind);
            Assert.AreEqual(kind, f.LastCommand.a);
            Assert.AreEqual(rng, f.Sim.S.Rng.State, "inceleme RNG çekmez");
            Assert.IsTrue(f.Sim.Admission.Candidate.Inspections.Contains(kind));
            if (tool == AdmissionTool.Breath)
            {
                Assert.AreEqual(SiginakPose.BreathGlass, f.Rig.Current, "SOĞUK GÖZLEM K8G'ye götürür");
                f.Rig.Step(1);
                f.Admission.Advance(0);
                f.Rig.Snap(SiginakPose.Admission);
            }

            Assert.IsFalse(f.Admission.UseTool(tool), "ikinci kullanım");
            Assert.AreEqual(before + 1, f.Commands, "ikinci kullanım komutsuz");
            Assert.AreEqual(AdmissionSound.Deny, f.Sounds.Last(), "ret sesi");
        }

        /// <summary>
        /// PLAN §1.8 (bütünleyici C): dış kayıt sonucu kapanışta (K1) gelir ama fişi ilk K8/K8A varışında bir kez ağızdan çıkar; o zamana
        /// dek BEKLENİYOR kalır ve GİDEN yaprağı yerindedir. Varış görülen sonucu yan dosyaya yazılacak duruma işler.
        /// </summary>
        [Test]
        public void RecordResult_EmergesOnFirstArrival()
        {
            var f = Build();
            AtCounter(f);
            string id = f.Candidate;
            Assert.IsTrue(f.Admission.UseTool(AdmissionTool.Record));
            f.Rig.Snap(SiginakPose.Desk);
            Close(f.Game);
            f.Admission.Refresh();
            Assert.AreEqual(id, f.Candidate, "dış kayıt adayı bir vardiya daha bekletir");
            Assert.IsTrue(f.Sim.Admission.Candidate.Inspections.Contains("record_done"), "kapanışta sonuç geldi (Boşta'da gözetmen)");
            var held = f.Admission.Last.Slips.First(x => x.Tool == AdmissionTool.Record);
            Assert.AreEqual(SlipVerdict.Pending, held.Kind, "K1'de sonuç fişi çıkmaz");
            Assert.AreNotEqual(id, f.Admission.Ui.SeenResultFor);

            f.Admission.OnArrived(SiginakPose.Desk);
            Assert.AreNotEqual(id, f.Admission.Ui.SeenResultFor, "K1 varışı saymaz");
            f.Admission.OnArrived(SiginakPose.Door);
            Assert.AreEqual(id, f.Admission.Ui.SeenResultFor, "K8 varışında görüldü");
            Assert.AreEqual(id, f.Admission.SeenFor(f.Sim.Record).resultFor, "yan dosyaya yazılacak durum");
            var result = f.Admission.Last.Slips.First(x => x.Tool == AdmissionTool.Record);
            Assert.AreNotEqual(SlipVerdict.Pending, result.Kind, "sonuç fişi (KAYIT BULUNDU / FARKLI)");
            Assert.AreEqual(AdmissionSnapshotBuilder.ResultSlipKey(id), result.Key);
        }

        /// <summary>PLAN §1.6 T3: ilk araç kullanımı belgeyi çekmeceden tezgâha alır (sunum durumu; kayda girmez).</summary>
        [Test]
        public void Inspect_FirstToolTakesDocument()
        {
            var f = Build();
            AtCounter(f);
            Assert.IsFalse(f.Admission.Ui.DocumentOnCounter(f.Candidate), "belge çekmecede başlar");
            Assert.IsTrue(f.Admission.UseTool(AdmissionTool.Interview));
            Assert.IsTrue(f.Admission.Ui.DocumentOnCounter(f.Candidate), "belge tezgâhta");
            Assert.AreEqual(f.Candidate, f.Admission.SeenFor(f.Sim.Record).documentFor, "yan dosyaya yazılacak durum");
        }

        // ---------------------------------------------------------------- damgalar → DecideApplicant

        /// <summary>
        /// PLAN §1.11 (D10, eleştiri 3): KABUL tam bir <c>decideApplicant admit</c> yazar; dizi sürerken masa kilitli ve kamera kilitli;
        /// 0,35 s'de kamera K8'e döner, çark tezgâh katlandıktan sonra (0,93 s) döner, kanat açılınca <c>DoorOpen</c>, beklemeden sonra
        /// <c>DoorClose</c>; kapı kapanınca dizi biter, komut sayısı değişmez. Sonraki damga (aday yok) komutsuz reddedilir.
        /// </summary>
        [Test]
        public void Decide_WritesOneCommand_ThenSequence()
        {
            var f = Build(withDoor: true);
            AtCounter(f);
            Assert.IsNull(f.Sim.ApplicantDecisionCheck("admit"), "kurulum: kabul açık");
            string id = f.Candidate;
            int before = f.Commands;
            Assert.IsTrue(f.Admission.PressStamp(StampChoice.Admit));
            Assert.AreEqual(before + 1, f.Commands);
            Assert.AreEqual("decideApplicant", f.LastCommand.kind);
            Assert.AreEqual("admit", f.LastCommand.a);
            Assert.IsNotNull(f.Sim.Board.Person(id), "kişi panoda");
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
                if (time < t.DocsS - 0.02f) Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "belge çekmeceye girerken K8A");
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

            Assert.IsNull(f.Candidate);
            Assert.IsFalse(f.Admission.PressStamp(StampChoice.Reject), "aday yok");
            Assert.AreEqual(before + 1, f.Commands);
            Assert.AreEqual(AdmissionSound.Deny, f.Sounds.Last());
        }

        /// <summary>D10: KISITLI'da da kapı açılır; REDDET ve YARDIM'da kapı kapalı kalır, kamera K8A'da.</summary>
        [TestCase(StampChoice.Restricted, "restricted", true)]
        [TestCase(StampChoice.Reject, "reject", false)]
        public void Decide_DoorOnlyForAdmission(StampChoice choice, string id, bool opens)
        {
            var f = Build(withDoor: true);
            AtCounter(f);
            Assert.IsNull(f.Sim.ApplicantDecisionCheck(id), "kurulum");
            int before = f.Commands;
            Assert.IsTrue(f.Admission.PressStamp(choice));
            Assert.AreEqual(before + 1, f.Commands);
            Assert.AreEqual(id, f.LastCommand.a);
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
        }

        /// <summary>PLAN §1.10: plan kilitliyken, baskında ve hikâye açıkken hiçbir araç ve damga komut yazmaz (Core denetimi; ret sesi).</summary>
        [Test]
        public void Locked_NoCommand()
        {
            var f = Build();
            AtCounter(f);
            Assert.IsTrue(f.Game.PrintBoardShift().Ok, "fiş basıldı: plan kilitli");
            int before = f.Commands;
            foreach (AdmissionTool tool in Enum.GetValues(typeof(AdmissionTool)))
                if (tool != AdmissionTool.Breath) Assert.IsFalse(f.Admission.UseTool(tool), tool + " plan kilitli");
            foreach (StampChoice c in Enum.GetValues(typeof(StampChoice))) Assert.IsFalse(f.Admission.PressStamp(c), c + " plan kilitli");
            Assert.AreEqual(before, f.Commands, "plan kilitliyken komut yok");
            Assert.IsFalse(f.Admission.Busy);

            var raid = Build();
            AtCounter(raid);
            Assert.IsTrue(raid.Game.StartLiberationRaid("weu_1").Ok, "kurulum: baskın");
            before = raid.Commands;
            uint rng = raid.Sim.S.Rng.State;
            Assert.IsFalse(raid.Admission.UseTool(AdmissionTool.Papers), "baskında");
            Assert.IsFalse(raid.Admission.PressStamp(StampChoice.Admit), "baskında");
            Assert.AreEqual(before, raid.Commands, "baskında komut yok");
            Assert.AreEqual(rng, raid.Sim.S.Rng.State);

            var story = Build();
            AtCounter(story);
            story.Game.StoryHold = true;
            before = story.Commands;
            Assert.IsFalse(story.Admission.UseTool(AdmissionTool.Interview), "hikâye açıkken");
            Assert.IsFalse(story.Admission.PressStamp(StampChoice.Reject), "hikâye açıkken");
            Assert.AreEqual(before, story.Commands);
        }

        // ---------------------------------------------------------------- geri zinciri

        /// <summary>
        /// PLAN §1.5 (D13): K8 → K8A (inik tezgâha tık) → belgeyi al → K8R (kâğıda tık) → Esc K8A → K8G (cam) → geri K8A → geri K8 →
        /// K8A → KABUL dizisi → geri diziyi sona atlar (K8) → geri K1; K1'de Esc kabulü tüketmez. Hiçbir poz adımı komut yazmaz.
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
            f.Admission.ClickPaper(AdmissionPaper.Document, 0);
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "çekmecedeki belge önce tezgâha alınır");
            Assert.IsTrue(f.Admission.Ui.DocumentOnCounter(f.Candidate));
            f.Admission.ClickPaper(AdmissionPaper.Document, 0);
            Assert.AreEqual(SiginakPose.AdmissionRead, f.Rig.Current, "tezgâhtaki belge → K8R");
            Assert.AreEqual(before, f.Commands, "okuma komut yazmaz");
            Assert.IsTrue(f.Admission.HandleEscape());
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "K8R → K8A");

            Assert.IsTrue(f.Admission.UseTool(AdmissionTool.Breath));
            Assert.AreEqual(SiginakPose.BreathGlass, f.Rig.Current);
            f.Rig.Step(1);
            f.Admission.Advance(0);
            f.Admission.HandleBack();
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current, "K8G geldiği poza (K8A)");
            f.Admission.HandleBack();
            Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "K8A → K8");
            f.Admission.ClickCounter();
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current);

            int afterInspect = f.Commands;
            Assert.IsTrue(f.Admission.PressStamp(StampChoice.Admit));
            Assert.IsTrue(f.Admission.Busy);
            Assert.IsTrue(f.Admission.HandleEscape(), "Esc diziyi sona atlatır");
            Assert.IsFalse(f.Admission.Busy);
            Assert.IsFalse(f.Rig.Locked);
            Assert.IsFalse(f.Door.Moving, "kapı kapalı oturdu");
            Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "atlanan kabul dizisi K8'de biter");
            Assert.AreEqual(afterInspect + 1, f.Commands, "atlama komut yazmaz");

            f.Admission.HandleBack();
            Assert.AreEqual(SiginakPose.Desk, f.Rig.Current, "K8 → K1");
            Assert.IsFalse(f.Admission.Active);
            Assert.IsFalse(f.Admission.HandleEscape(), "K1'de Esc tüketilmez");
        }

        /// <summary>K8G'de cama ikinci tık geldiği poza (K8) döner; MASA tuşu her pozdan K1.</summary>
        [Test]
        public void Glass_FromDoor_ReturnsToDoor_DeskKeyLeaves()
        {
            var f = Build();
            f.Admission.Enter();
            Assert.IsTrue(f.Admission.UseTool(AdmissionTool.Breath), "K8'den SOĞUK GÖZLEM");
            Assert.AreEqual(SiginakPose.BreathGlass, f.Rig.Current);
            f.Rig.Step(1);
            Assert.IsFalse(f.Admission.UseTool(AdmissionTool.Breath), "K8G'de cama tık komut değil");
            Assert.AreEqual(SiginakPose.Door, f.Rig.Current, "geldiği poz K8");
            f.Admission.ClickCounter();
            Assert.AreEqual(SiginakPose.Admission, f.Rig.Current);
            f.Admission.ClickDeskKey();
            Assert.AreEqual(SiginakPose.Desk, f.Rig.Current, "MASA → K1");
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

        /// <summary>
        /// PLAN §1.8 (H2/H4, D3-a): kapıda bekleyen dış kayıt talebi varken bir dosyanın İNCELE'si onu iptal eder → sunum önce uyarır.
        /// Talep yoksa, kişi adaysa ya da sonuç gelmişse uyarı yok.
        /// </summary>
        [Test]
        public void ReviewWarning_WhenDoorRecordPending()
        {
            var f = Build();
            AtCounter(f);
            var sim = f.Sim;
            Func<ResidentState, bool> reviewable = p => p.Id != f.Candidate && sim.Admission.File(p.Id) != null && sim.ResidentReviewCheck(p.Id) == null;
            var founder = sim.Board.People.FirstOrDefault(reviewable);
            Assert.IsNotNull(founder, "kurulum: İNCELE'si açık bir sakin");
            Assert.IsNull(PlanningBoardPresenter.ReviewCancelsDoorRecord(sim, founder.Id), "talep yokken uyarı yok");

            Assert.IsTrue(f.Admission.UseTool(AdmissionTool.Record), "DIŞ KAYIT talebi");
            founder = sim.Board.People.FirstOrDefault(reviewable);
            Assert.IsNotNull(founder, "kurulum: talepten sonra da İNCELE'si açık bir sakin");
            Assert.AreEqual("Kapıdaki dış kayıt talebi iptal olur.", PlanningBoardPresenter.ReviewCancelsDoorRecord(sim, founder.Id));
            Assert.IsNull(PlanningBoardPresenter.ReviewCancelsDoorRecord(sim, f.Candidate), "aday dosya değil");
            Assert.IsNull(PlanningBoardPresenter.ReviewCancelsDoorRecord(sim, null));
            Assert.IsNull(PlanningBoardPresenter.ReviewCancelsDoorRecord(null, founder.Id));
        }

        // ---------------------------------------------------------------- yan dosya, teleks, varış

        /// <summary>Eleştiri 7: görülen durum (sürüm, sonuç, belge) yan dosyaya gider ve kayıttan devamda geri gelir; başka kaydın durumu yazılmaz.</summary>
        [Test]
        public void Seen_RoundTripsThroughDeskFile()
        {
            var f = Build();
            AtCounter(f);
            f.Admission.ClickPaper(AdmissionPaper.Document, 0);
            string id = f.Candidate;
            Assert.IsTrue(f.Admission.Ui.DocumentOnCounter(id));
            var record = f.Sim.Record;
            var seen = f.Admission.SeenFor(record);
            Assert.AreEqual(f.Admission.Ui.SeenOrdersVersion, seen.ordersVersion);
            Assert.AreEqual(id, seen.documentFor);

            var other = new GameRecord { seed = record.seed + 1 };
            Assert.IsNull(f.Admission.SeenFor(other), "başka kayıt: dosyada girdi yok");

            SiginakDeskStore.Save(record, null, null, null, null, seen);
            var loaded = SiginakDeskStore.LoadAdmissionSeen(record);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(id, loaded.documentFor);

            Assert.IsTrue(f.Game.ContinueSaved());
            f.Admission.Restore();
            Assert.AreEqual(id, f.Candidate, "aynı aday");
            Assert.IsTrue(f.Admission.Ui.DocumentOnCounter(id), "belge tezgâhta geri geldi (P15)");
            Assert.AreEqual(seen.ordersVersion, f.Admission.Ui.SeenOrdersVersion);
            Assert.IsEmpty(f.Sounds.Where(s => s == AdmissionSound.Knock), "yüklemede vuruş yok");
        }

        /// <summary>
        /// PLAN §1.14, §1.9: talimat değişince kapanıştan sonra bir kez "TALİMAT n" + değişiklik metni (Info); görülmemiş değişim ilk K8/K8A
        /// varışında oynar ve görülen sürüm güncellenir (K1'e varış saymaz).
        /// </summary>
        [Test]
        public void Telex_TalimatOnProtocolChange_ArrivalMarksSeen()
        {
            var f = Build(toCandidate: false);
            var lines = new List<(string, string)>();
            f.Admission.TelexLine += (a, b) => lines.Add((a, b));
            for (int i = 0; i < 12 && f.Sim.Admission.Protocol.Version < 2; i++)
            {
                Close(f.Game);
                f.Admission.Refresh();
                f.Admission.Advance(0);
            }
            var protocol = f.Sim.Admission.Protocol;
            Assert.AreEqual(2, protocol.Version, "kurulum: talimat 2");
            Assert.AreEqual(1, lines.Count, "bir satır");
            Assert.AreEqual("TALİMAT 2", lines[0].Item1);
            Assert.AreEqual(Loc.T(protocol.Change), lines[0].Item2);
            var telexLine = f.Telex.Lines.Last();
            Assert.AreEqual("TALİMAT 2", telexLine.status);
            Assert.AreEqual(TelexKind.Info, telexLine.kind);
            f.Admission.Refresh();
            f.Admission.Advance(0);
            Assert.AreEqual(1, lines.Count, "aynı sürüm ikinci kez basılmaz");

            Assert.AreEqual(1, f.Admission.Ui.SeenOrdersVersion, "değişim henüz görülmedi");
            f.Admission.OnArrived(SiginakPose.Desk);
            Assert.AreEqual(1, f.Admission.Ui.SeenOrdersVersion, "K1 varışı saymaz");
            f.Admission.OnArrived(SiginakPose.Door);
            Assert.AreEqual(2, f.Admission.Ui.SeenOrdersVersion, "K8 varışında görüldü");
        }

        /// <summary>PLAN §1.10, E7: damga izinin satırları dengeden (KISITLI vardiya sayısı, YARDIM erzakı) ve dile göre.</summary>
        [Test]
        public void ImpressionLines_FromBalance()
        {
            var b = new Balance { admissionRestrictedShifts = 4, admissionAidRations = 2 };
            CollectionAssert.AreEqual(new[] { "KABUL", "TAM ERİŞİM" }, SiginakAdmission.ImpressionLines(StampChoice.Admit, b));
            CollectionAssert.AreEqual(new[] { "KISITLI KABUL", "4 VARDİYA" }, SiginakAdmission.ImpressionLines(StampChoice.Restricted, b));
            CollectionAssert.AreEqual(new[] { "REDDET" }, SiginakAdmission.ImpressionLines(StampChoice.Reject, b));
            CollectionAssert.AreEqual(new[] { "DIŞARIDAN YARDIM", "2 ERZAK" }, SiginakAdmission.ImpressionLines(StampChoice.Aid, b));
            Assert.AreEqual("3 VARDİYA", SiginakAdmission.ImpressionLines(StampChoice.Restricted, new Balance())[1], "varsayılan 3");
            Assert.AreEqual("#24592E", SiginakAdmission.ImpressionColor(StampChoice.Admit), "admission.stampMarks.admit.color");
            Assert.AreEqual("#9A1E14", SiginakAdmission.ImpressionColor(StampChoice.Reject));

            DeskLoc.Instance.SetLanguage(DeskLang.En);
            CollectionAssert.AreEqual(new[] { "ADMIT", "FULL ACCESS" }, SiginakAdmission.ImpressionLines(StampChoice.Admit, b));
            Assert.AreEqual("4 SHIFTS", SiginakAdmission.ImpressionLines(StampChoice.Restricted, b)[1]);
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
