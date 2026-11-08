using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using Corrigible.Game.Audio;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 6 (R9, R17) sözleşmesi (PLAN §3 W0): yeni poz ve ses numaraları sona eklenir (eski sıralar kaymaz), kabul türleri
    /// Core kimlikleriyle aynı sırada, anlık görüntü sınıfları Türkçe ilkleyicisiz, sunum durumu (<see cref="AdmissionUi"/>) yan dosya
    /// biçimine gidip gelir, yerel token anahtarları (<c>_local_a6</c>) temaya girer, poz ışık tarifi yeni pozları K8'in kopyasıyla taşır,
    /// kabukların genel API'si yerinde. Core gözetmen kuralı <c>AdmissionSupervisorTests</c>'te.
    /// </summary>
    public sealed class Rev3Stage6ContractTests
    {
        const string RecipePath = "Assets/Corrigible/Desk/Data/Rev3/PoseLightRecipe.asset";

        [SetUp]
        public void Turkish() => Loc.SetLanguage(Loc.Turkish);

        [TearDown]
        public void RestoreTurkish() => Loc.SetLanguage(Loc.Turkish);

        // ---------------------------------------------------------------- enumlar

        [Test]
        public void SiginakPose_K8FamilyAppendedAs8To10()
        {
            Assert.AreEqual(5, (int)SiginakPose.Door, "K8 değişmez");
            Assert.AreEqual(7, (int)SiginakPose.ShiftClose, "Aşama 4 pozu yerinde");
            Assert.AreEqual(8, (int)SiginakPose.Admission);
            Assert.AreEqual(9, (int)SiginakPose.BreathGlass);
            Assert.AreEqual(10, (int)SiginakPose.AdmissionRead);
            CollectionAssert.AreEqual(new[] { "Desk", "Map", "Shelf", "Crt", "Shift", "Door", "Cabinet", "ShiftClose", "Admission", "BreathGlass", "AdmissionRead" },
                Enum.GetNames(typeof(SiginakPose)));
            for (int i = 0; i <= 10; i++) Assert.IsTrue(Enum.IsDefined(typeof(SiginakPose), i), i.ToString());
        }

        [Test]
        public void Cues_120To128_AreTheAdmissionSounds()
        {
            Assert.AreEqual(120, (int)Cue.AdmissionCounterDown);
            Assert.AreEqual(121, (int)Cue.AdmissionDrawer);
            Assert.AreEqual(122, (int)Cue.AdmissionMarker);
            Assert.AreEqual(123, (int)Cue.AdmissionSlip);
            Assert.AreEqual(124, (int)Cue.AdmissionIntercom);
            Assert.AreEqual(125, (int)Cue.AdmissionGlass);
            Assert.AreEqual(126, (int)Cue.AdmissionPadTear);
            Assert.AreEqual(127, (int)Cue.AdmissionStampLift);
            Assert.AreEqual(128, (int)Cue.AdmissionCounterFold);
            Assert.AreEqual(119, (int)Cue.BoardProductSlide, "Aşama 5'in sesleri yerinde");
            Assert.AreEqual(50, (int)Cue.DoorKnock);
            Assert.AreEqual(51, (int)Cue.DoorOpen);
            Assert.AreEqual(62, (int)Cue.DoorClose);
            Assert.AreEqual(63, (int)Cue.StampHeavy);
            Assert.AreEqual(128, Enum.GetValues(typeof(Cue)).Cast<int>().Max(), "yeni ses yalnız sonda");
            var twice = Enum.GetValues(typeof(Cue)).Cast<int>().GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            Assert.IsEmpty(twice, "bir ses numarası iki ada verilmiş");
        }

        [Test]
        public void AdmissionTypes_Order()
        {
            CollectionAssert.AreEqual(new[] { "Folded", "Lowering", "Down", "Folding" }, Enum.GetNames(typeof(CounterState)));
            CollectionAssert.AreEqual(new[] { "Papers", "Interview", "Breath", "Record" }, Enum.GetNames(typeof(AdmissionTool)));
            CollectionAssert.AreEqual(new[] { "Admit", "Restricted", "Reject", "Aid" }, Enum.GetNames(typeof(StampChoice)));
            CollectionAssert.AreEqual(new[] { "Match", "Mismatch", "Neutral", "Pending", "Found", "Differs", "Review" }, Enum.GetNames(typeof(SlipVerdict)));
            CollectionAssert.AreEqual(new[] { "Document", "Strip", "Slip", "Request", "Carbon" }, Enum.GetNames(typeof(AdmissionPaper)));
            CollectionAssert.AreEqual(new[]
            {
                "CounterDown", "Drawer", "TinsOut", "Marker", "SlipOut", "IntercomCrackle", "GlassCover", "Fog", "PadTear", "StampLift",
                "CounterFold", "StampPress", "StampReturn", "Deny", "DocDrop", "Knock", "Open", "Close",
            }, Enum.GetNames(typeof(AdmissionSound)));
        }

        [Test]
        public void AdmissionIds_AreTheCoreCommandArguments()
        {
            CollectionAssert.AreEqual(new[] { "papers", "interview", "breath", "record" },
                ((AdmissionTool[])Enum.GetValues(typeof(AdmissionTool))).Select(AdmissionIds.KindOf).ToArray());
            CollectionAssert.AreEqual(new[] { "admit", "restricted", "reject", "aid" },
                ((StampChoice[])Enum.GetValues(typeof(StampChoice))).Select(AdmissionIds.ChoiceOf).ToArray());
            Assert.IsTrue(AdmissionIds.TryTool("breath", out var tool));
            Assert.AreEqual(AdmissionTool.Breath, tool);
            Assert.IsFalse(AdmissionIds.TryTool("record_done", out _), "record_done bir araç değil");
            Assert.IsTrue(AdmissionIds.TryChoice("aid", out var stamp));
            Assert.AreEqual(StampChoice.Aid, stamp);
            Assert.IsFalse(AdmissionIds.TryChoice("default_reject", out _));

            // Core aynı türleri ve seçimleri tanır (ileti "Böyle bir inceleme/karar yok." değil)
            var sim = Simulation.NewGame(79, new Balance { planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1 });
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            Assert.IsNotNull(sim.Admission.Candidate);
            foreach (AdmissionTool t in Enum.GetValues(typeof(AdmissionTool)))
                Assert.AreNotEqual(Loc.T("Böyle bir inceleme yok."), sim.ApplicantInspectionCheck(AdmissionIds.KindOf(t)), t.ToString());
            foreach (StampChoice c in Enum.GetValues(typeof(StampChoice)))
                Assert.AreNotEqual(Loc.T("Böyle bir kabul kararı yok."), sim.ApplicantDecisionCheck(AdmissionIds.ChoiceOf(c)), c.ToString());
        }

        // ---------------------------------------------------------------- veri

        [Test]
        public void Snapshot_Defaults_NoTurkishInitialisers()
        {
            var s = new AdmissionSnapshot();
            Assert.IsFalse(s.Active);
            Assert.AreEqual(CounterState.Folded, s.Counter);
            Assert.IsNull(s.CandidateId);
            Assert.IsNull(s.Document);
            Assert.IsEmpty(s.Archive);
            Assert.IsEmpty(s.Slips);
            Assert.AreEqual(4, s.Tools.Length);
            Assert.AreEqual(4, s.Stamps.Length);
            Assert.AreEqual(-1, s.MarkerRule, "−1 = işaretçi en üstte");
            Assert.IsNull(s.Words, "AdmissionWords kurucu doldurur");
            var w = new AdmissionWords();
            foreach (var f in typeof(AdmissionWords).GetFields(BindingFlags.Public | BindingFlags.Instance))
                Assert.IsNull(f.GetValue(w), "AdmissionWords." + f.Name + ": Türkçe ilkleyici yok (AGENTS kural 9)");
            Assert.IsNull(new SlipData().Tool, "null = İNCELEME FİŞİ");
            Assert.IsEmpty(new OrdersData().Rules);
            Assert.IsNull(new FolderData().TagText, "etiket yok");
        }

        [Test]
        public void AdmissionUi_RoundTripsThroughTheDeskFileShape()
        {
            var ui = new AdmissionUi { DocumentOnCounterFor = "resident-9", SeenOrdersVersion = 2, SeenResultFor = "resident-9" };
            Assert.IsTrue(ui.DocumentOnCounter("resident-9"));
            Assert.IsFalse(ui.DocumentOnCounter("resident-10"), "aday değişince belge tezgâhta sayılmaz");
            Assert.IsFalse(ui.DocumentOnCounter(null));
            var seen = ui.ToSeen();
            Assert.AreEqual(2, seen.ordersVersion);
            Assert.AreEqual("resident-9", seen.resultFor);
            Assert.AreEqual("resident-9", seen.documentFor);
            var json = JsonUtility.ToJson(seen);
            var back = new AdmissionUi();
            Assert.IsTrue(back.Apply(JsonUtility.FromJson<SiginakDeskStore.AdmissionSeen>(json)));
            Assert.AreEqual(2, back.SeenOrdersVersion);
            Assert.AreEqual("resident-9", back.DocumentOnCounterFor);
            Assert.AreEqual("resident-9", back.SeenResultFor);

            var empty = new AdmissionUi().ToSeen();
            Assert.AreEqual("", empty.resultFor, "JsonUtility null dizgiyi boş yazar; Apply boşu null okur");
            var fresh = new AdmissionUi { SeenOrdersVersion = 3 };
            Assert.IsFalse(fresh.Apply(empty), "sürüm 0 = girdi yok");
            Assert.IsFalse(fresh.Apply(null));
            Assert.AreEqual(3, fresh.SeenOrdersVersion, "boş girdi durumu değiştirmez");
            ui.Reset();
            Assert.IsNull(ui.DocumentOnCounterFor);
            Assert.AreEqual(0, ui.SeenOrdersVersion);
            Assert.IsNull(ui.SeenResultFor);
        }

        // ---------------------------------------------------------------- kabukların genel API'si

        static void HasMethod(Type t, string name, params Type[] args)
        {
            Assert.IsNotNull(t.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, args, null),
                t.Name + "." + name + "(" + string.Join(", ", args.Select(a => a.Name)) + ")");
        }

        static void HasEvent(Type t, string name) => Assert.IsNotNull(t.GetEvent(name), t.Name + " olay " + name);

        [Test]
        public void Shells_PublicApi()
        {
            var station = typeof(AdmissionStation);
            HasMethod(station, "Bind", typeof(AdmissionSnapshot), typeof(bool));
            HasMethod(station, "PlayInspection", typeof(AdmissionTool), typeof(SlipData));
            HasMethod(station, "PlayOrdersChange", typeof(OrdersData), typeof(OrdersData), typeof(SlipData));
            HasMethod(station, "PlayDecision", typeof(StampChoice), typeof(Action));
            HasMethod(station, "TakeDocument");
            HasMethod(station, "ReadPose", typeof(AdmissionPaper), typeof(int), typeof(float));
            HasMethod(station, "SetInteractive", typeof(bool));
            HasMethod(station, "Advance", typeof(float));
            foreach (var e in new[] { "CounterClicked", "DeskKeyClicked", "PaperClicked", "Sound" }) HasEvent(station, e);
            Assert.IsTrue(typeof(IHoverSource).IsAssignableFrom(station));

            foreach (var tool in new[] { typeof(MagnifierTool), typeof(IntercomTool), typeof(RequestPadTool), typeof(ColdGlassView) })
            {
                HasEvent(tool, "Used");
                HasEvent(tool, "Sound");
                HasMethod(tool, "Bind", typeof(ToolData));
                HasMethod(tool, "Advance", typeof(float));
                Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(tool));
            }
            HasMethod(typeof(MagnifierTool), "PlayInspect", typeof(string), typeof(Action));
            HasMethod(typeof(ColdGlassView), "Play", typeof(bool), typeof(Action));
            HasMethod(typeof(ColdGlassView), "Close");
            HasMethod(typeof(IntercomTool), "Play", typeof(Action));
            HasMethod(typeof(RequestPadTool), "Bind", typeof(RequestData));
            HasMethod(typeof(RequestPadTool), "PlaySend", typeof(Action));

            var stamp = typeof(StampDecision);
            HasEvent(stamp, "Pressed");
            HasEvent(stamp, "Sound");
            HasMethod(stamp, "Bind", typeof(StampData));
            HasMethod(stamp, "ReturnToRack", typeof(bool));
            HasMethod(stamp, "Advance", typeof(float));
            Assert.IsNotNull(stamp.GetProperty("Held"));
            Assert.AreEqual(typeof(Func<Vector3, bool>), stamp.GetProperty("DropAccepts")?.PropertyType);
            Assert.IsNotNull(stamp.GetField("choice", BindingFlags.NonPublic | BindingFlags.Instance)?.GetCustomAttribute<SerializeField>(), "StampDecision.choice [SerializeField]");

            HasMethod(typeof(AdmissionDocumentFace), "Bind", typeof(DocumentData));
            HasMethod(typeof(AdmissionDocumentFace), "ShowImpression", typeof(bool), typeof(bool));
            HasMethod(typeof(OrdersStripFace), "Bind", typeof(OrdersData));
            HasMethod(typeof(OrdersStripFace), "BindHead", typeof(OrdersData));
            HasMethod(typeof(OrdersStripFace), "RuleCenterMm", typeof(int));
            HasMethod(typeof(FindingSlipFace), "Bind", typeof(SlipData));
            HasMethod(typeof(FindingSlipFace), "Type", typeof(float));
            Assert.IsNotNull(typeof(FindingSlipFace).GetProperty("Typing"));
            HasMethod(typeof(RequestFormFace), "Bind", typeof(RequestData), typeof(bool));
            HasMethod(typeof(FileTagView), "Bind", typeof(string));
            foreach (var face in new[] { typeof(AdmissionDocumentFace), typeof(OrdersStripFace), typeof(FindingSlipFace), typeof(RequestFormFace), typeof(FileTagView) })
                Assert.IsNotNull(face.GetProperty("Overflowing"), face.Name + ".Overflowing");

            HasMethod(typeof(AdmissionSnapshotBuilder), "Build", typeof(Simulation), typeof(AdmissionUi));
            var flow = typeof(SiginakAdmission);
            foreach (var m in new[] { "Enter", "Leave", "HandleEscape", "HandleBack" }) HasMethod(flow, m);
            Assert.AreEqual(typeof(bool), flow.GetMethod("HandleEscape").ReturnType);
            HasEvent(flow, "TelexLine");
            Assert.IsNotNull(flow.GetProperty("Busy"));
            Assert.IsNotNull(flow.GetProperty("Active"));

            var door = typeof(DoorProp);
            HasMethod(door, "Advance", typeof(float));
            HasMethod(door, "SetOpen", typeof(bool), typeof(IReadOnlyList<string>), typeof(float), typeof(bool), typeof(float), typeof(float));
            HasMethod(door, "SetOpen", typeof(bool), typeof(IReadOnlyList<string>), typeof(float), typeof(bool));
            Assert.IsNotNull(door.GetProperty("Knocking"));
            HasMethod(typeof(SiginakCameraRig), "PoseSeconds", typeof(SiginakPose), typeof(SiginakPose));

            var tag = typeof(PersonFolderView).GetField("fileTag", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(tag, "PersonFolderView.fileTag (PLAN 'tag': Component.tag ile çakışmasın)");
            Assert.AreEqual(typeof(FileTagView), tag.FieldType);
            Assert.IsNotNull(tag.GetCustomAttribute<SerializeField>());
            HasMethod(typeof(SiginakDeskStore), "LoadAdmissionSeen", typeof(GameRecord));
        }

        /// <summary>Kabukların gövdesi bugünkü davranışı değiştirmez: anlık görüntü boş, akış pasif, kapının yeni açılışı eskisiyle aynı yoldan.</summary>
        [Test]
        public void Shells_AreInert()
        {
            var sim = Simulation.NewGame(79, new Balance { planningBoardEnabled = true, liberationAlarmPerShift = 0 });
            uint rng = sim.S.Rng.State;
            var s = AdmissionSnapshotBuilder.Build(sim, new AdmissionUi());
            Assert.IsNotNull(s);
            Assert.AreEqual(rng, sim.S.Rng.State);
            var go = new GameObject("Aşama 6 kabuk denetimi");
            try
            {
                var flow = go.AddComponent<SiginakAdmission>();
                Assert.IsFalse(flow.Busy);
                Assert.IsFalse(flow.Active);
                Assert.IsFalse(flow.HandleEscape(), "kabuk Esc'i tüketmez");
                var station = go.AddComponent<AdmissionStation>();
                bool done = false;
                station.PlayDecision(StampChoice.Reject, () => done = true);
                Assert.IsTrue(done, "kabuk dizisi beklemeden biter (asılı kalmaz)");
                Assert.AreEqual(CounterState.Folded, station.Counter);
                var door = go.AddComponent<DoorProp>();
                Assert.IsFalse(door.Knocking);
                door.SetKnocking(true);
                Assert.IsTrue(door.Knocking);
                door.SetOpen(true, null, 1, false, 0.5f, 0.8f);
                Assert.IsTrue(door.Moving, "süreli açılış eski açılışla aynı hedefi kurar");
                door.ResetClosed();
                Assert.IsFalse(door.Knocking);
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ---------------------------------------------------------------- yerel tokenlar (_local_a6)

        static SiginakTheme Fresh()
        {
            var theme = ScriptableObject.CreateInstance<SiginakTheme>();
            SiginakThemeSetup.Fill(theme, System.IO.File.ReadAllText(SiginakThemeSetup.TokensPath));
            return theme;
        }

        static bool HasKey(SiginakTheme t, string key) =>
            t.colors.Any(c => Matches(c.key, key)) || t.numbers.Any(n => Matches(n.key, key)) || t.strings.Any(s => Matches(s.key, key));

        static bool Matches(string have, string key) => have == key || have.StartsWith(key + ".") || have.StartsWith(key + "[");

        [Test]
        public void Tokens_LocalA6_EveryNewPathResolves()
        {
            var t = Fresh();
            try
            {
                var root = JObject.Parse(System.IO.File.ReadAllText(SiginakThemeSetup.TokensPath));
                var paths = ((JArray)root["_local_a6"]["new"]).Select(p => (string)p).ToList();
                Assert.AreEqual(43, paths.Count, "42 (W0) + admission.paper.seal (bütünleyici C: mühür halkasının moru koda gömülmesin)");
                var missing = paths.Where(p => !HasKey(t, p.StartsWith("colors.") ? p.Substring(7) : p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan yerel tokenlar: " + string.Join(", ", missing));
                Assert.IsFalse(t.strings.Any(s => s.key.Contains("_local_a6")), "notlar temaya girmez");
            }
            finally { Object.DestroyImmediate(t); }
        }

        [Test]
        public void Tokens_LocalA6_Values()
        {
            var t = Fresh();
            try
            {
                Assert.AreEqual(0.25f, t.Number("admission.motion.readS"), 1e-4);
                Assert.AreEqual(0.15f, t.Number("admission.motion.counterBracketS"), 1e-4);
                Assert.AreEqual(0.35f, t.Number("admission.motion.counterFoldS"), 1e-4);
                Assert.AreEqual(0.15f, t.Number("admission.motion.slipDropS"), 1e-4);
                Assert.AreEqual(0.25f, t.Number("admission.motion.stripOutS"), 1e-4);
                Assert.AreEqual(0.3f, t.Number("admission.motion.stripInS"), 1e-4);
                Assert.AreEqual(0.15f, t.Number("admission.motion.padTearS"), 1e-4);
                Assert.AreEqual(0.25f, t.Number("admission.motion.magnifierMoveS"), 1e-4);
                Assert.AreEqual(0.6f, t.Number("admission.motion.admitHoldS"), 1e-4);
                Assert.AreEqual(0.4f, t.Number("admission.motion.hoverDelayS"), 1e-4);
                Assert.AreEqual(0.318f, t.Number("admission.readDistanceM"), 1e-4);
                Assert.AreEqual(160f, t.Number("admission.slipPathMm"), 1e-4);
                Assert.AreEqual(0.93f, t.Number("admission.stampMarks.opacity"), 1e-4);
                Assert.AreEqual("24592E", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.stampMarks.admit.color")));
                Assert.AreEqual(new Vector2(60f, 29.9f), t.Vector2Of("admission.stampMarks.admit.sizeMm"));
                Assert.AreEqual(new Vector2(149.3f, 25.2f), t.Vector2Of("admission.stampMarks.restricted.sizeMmEn"));
                Assert.AreEqual("854005", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.stampMarks.restricted.color")));
                Assert.AreEqual("9A1E14", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.stampMarks.reject.color")));
                Assert.AreEqual("463F34", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.stampMarks.aid.color")));
                Assert.AreEqual("24592E", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.slipVerdict.match")));
                Assert.AreEqual("675F50", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.slipVerdict.pending")));
                Assert.AreEqual(68f, t.Number("admission.counterSpot.spotAngle"), 1e-4);
                Assert.AreEqual("FBF7EC", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.paper.strip")));
                Assert.AreEqual("2E3C6E", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.paper.carbonInk")));
                Assert.AreEqual("9A1E14", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.paper.alarm")));
                Assert.AreEqual("5A3F78", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.paper.seal")));
                // Paketin değerleri değişmedi
                Assert.AreEqual(0.4f, t.Number("admission.motion.counterLeafS"), 1e-4);
                Assert.AreEqual(0.45f, t.Number("admission.motion.toK8GS"), 1e-4);
                Assert.AreEqual(0.8f, t.Number("admission.paperAlbedo"), 1e-4);
                Assert.AreEqual(0.9f, t.Number("motion.doorKnockS"), 1e-4);
            }
            finally { Object.DestroyImmediate(t); }
        }

        // ---------------------------------------------------------------- poz ışık tarifi

        /// <summary>
        /// Yeni pozların tarifi var (tek seferlik <c>Rev3Stage6PoseRecipe.Apply</c> K8'in kopyasıyla başlatır): bütün K8 ışıkları tarifte,
        /// gölgeli ışık sınırı aşılmaz. W6 tezgâh lambasını (<c>counterSpot</c>) ekler ve değerleri kalibre eder (bu testi bozmaz).
        /// </summary>
        [Test]
        public void PoseLightRecipe_K8FamilyStartsAsK8Copy()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<PoseLightRecipe>(RecipePath);
            Assert.IsNotNull(recipe, RecipePath);
            Assert.IsTrue(recipe.TryGet(SiginakPose.Door, out var k8));
            foreach (var pose in new[] { SiginakPose.Admission, SiginakPose.BreathGlass, SiginakPose.AdmissionRead })
            {
                Assert.IsTrue(recipe.TryGet(pose, out var e), "tarifte poz yok: " + pose + " (Rev3Stage6PoseRecipe.Apply çalıştırılmalı)");
                // Yalnız ışık kimlikleri kilitli: W6 tezgâh lambasını ekler ve K8A/G/R yoğunluk, gölge ve ortamını kalibre edebilir
                // (değer eşitliği W6'nın kalibrasyonunu kırardı; R0 incelemesi).
                foreach (var l in k8.lights)
                    Assert.IsTrue(e.lights != null && e.lights.Any(x => x.id == l.id), pose + ": K8 ışığı tarifte yok: " + l.id);
                Assert.LessOrEqual(PoseLightRecipe.ShadowedCount(e), PoseLightRecipe.MaxShadowed, pose.ToString());
            }
        }
    }
}
