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
            Assert.AreEqual(11, (int)SiginakPose.LastSwitch, "KOS aşama 6: KLS sonda");
            CollectionAssert.AreEqual(new[] { "Desk", "Map", "Shelf", "Crt", "Shift", "Door", "Cabinet", "ShiftClose", "Admission", "BreathGlass", "AdmissionRead", "LastSwitch" },
                Enum.GetNames(typeof(SiginakPose)));
            for (int i = 0; i <= 10; i++) Assert.IsTrue(Enum.IsDefined(typeof(SiginakPose), i), i.ToString());
        }

        [Test]
        public void Cues_120To128_AreTheAdmissionSounds()
        {
            Assert.AreEqual(120, (int)Cue.AdmissionCounterDown);
            Assert.AreEqual(121, (int)Cue.AdmissionDrawer);
            Assert.AreEqual(122, (int)Cue.AdmissionMarker);
            for (int n = 123; n <= 126; n++) Assert.IsFalse(Enum.IsDefined(typeof(Cue), n), n + ": aşama 7'de sökülen sesin sayısı boş kalır");
            Assert.AreEqual(127, (int)Cue.AdmissionStampLift);
            Assert.AreEqual(128, (int)Cue.AdmissionCounterFold);
            Assert.AreEqual(119, (int)Cue.BoardProductSlide, "Aşama 5'in sesleri yerinde");
            Assert.AreEqual(50, (int)Cue.DoorKnock);
            Assert.AreEqual(51, (int)Cue.DoorOpen);
            Assert.AreEqual(62, (int)Cue.DoorClose);
            Assert.AreEqual(63, (int)Cue.StampHeavy);
            Assert.AreEqual(129, (int)Cue.ExposureStep, "KOS aşama 6: İFŞA şeridinin sesi sonda");
            Assert.AreEqual(129, Enum.GetValues(typeof(Cue)).Cast<int>().Max(), "yeni ses yalnız sonda");
            var twice = Enum.GetValues(typeof(Cue)).Cast<int>().GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            Assert.IsEmpty(twice, "bir ses numarası iki ada verilmiş");
        }

        [Test]
        public void AdmissionTypes_Order()
        {
            CollectionAssert.AreEqual(new[] { "Folded", "Lowering", "Down", "Folding" }, Enum.GetNames(typeof(CounterState)));
            CollectionAssert.AreEqual(new[] { "Admit", "Restricted", "Reject", "Aid" }, Enum.GetNames(typeof(StampChoice)), "sahnede sayıyla (0 KABUL, 2 GERİ ÇEVİR)");
            CollectionAssert.AreEqual(new[] { "Document", "Strip", "Slip", "Request", "Carbon", "Candidate", "GroupSlip" }, Enum.GetNames(typeof(AdmissionPaper)),
                "KOS aşama 2: aday kartı ve grup fişi sonda");
            CollectionAssert.AreEqual(new[] { "DoorLastShift", "Rations", "Exposure", "ShelterShort" }, Enum.GetNames(typeof(SlipWarningKind)),
                "fiş verisi yan dosyada sayıyla saklanır: yeni tür sonda; aşama 7'de sökülen 0–3 boş");
            CollectionAssert.AreEqual(new[] { 4, 5, 6, 7 }, Enum.GetValues(typeof(SlipWarningKind)).Cast<int>().ToArray(), "sayılar korunur");
            CollectionAssert.AreEqual(new[]
            {
                "CounterDown", "Drawer", "StampLift", "CounterFold", "StampPress", "StampReturn", "Deny", "DocDrop", "Knock", "Open", "Close",
            }, Enum.GetNames(typeof(AdmissionSound)));
        }

        [Test]
        public void AdmissionIds_AreTheCoreCommandArguments()
        {
            CollectionAssert.AreEqual(new[] { "admit", "restricted", "reject", "aid" },
                ((StampChoice[])Enum.GetValues(typeof(StampChoice))).Select(AdmissionIds.ChoiceOf).ToArray());
            Assert.IsTrue(AdmissionIds.TryChoice("aid", out var stamp));
            Assert.AreEqual(StampChoice.Aid, stamp);
            Assert.IsFalse(AdmissionIds.TryChoice("default_reject", out _));

            // KOS aşama 2: kapıda aday grubu durur; KABUL ve GERİ ÇEVİR Core'un grup komutlarıdır (kapanıştan sonra ikisi de açık)
            var sim = Simulation.NewGame(79, new Balance { shelterEventsEnabled = false, planningBoardEnabled = true, liberationAlarmPerShift = 0, admissionKnockMin = 1, admissionKnockMax = 1 });
            Assert.IsTrue(sim.PrintShift().Ok);
            Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);
            Assert.Greater(sim.Admission.Group.Count, 0, "kapanıştan sonra aday grubu");
            Assert.IsNull(sim.CandidateChoiceCheck(sim.Admission.Group[0].Resident.Id, null));
            Assert.IsNull(sim.CandidatePassCheck());
            Assert.AreEqual("group:" + sim.Admission.GroupArrivalShift + ":" + sim.Admission.Group[0].Resident.Id, AdmissionSnapshotBuilder.GroupKey(sim));
            Assert.IsTrue(AdmissionSnapshotBuilder.GroupWaiting(sim));
        }

        // ---------------------------------------------------------------- veri

        [Test]
        public void Snapshot_Defaults_NoTurkishInitialisers()
        {
            var s = new AdmissionSnapshot();
            Assert.IsFalse(s.Active);
            Assert.AreEqual(CounterState.Folded, s.Counter);
            Assert.AreEqual(4, s.Stamps.Length, "StampChoice dizinli");
            Assert.IsNull(s.Words, "AdmissionWords kurucu doldurur");
            var w = new AdmissionWords();
            foreach (var f in typeof(AdmissionWords).GetFields(BindingFlags.Public | BindingFlags.Instance))
                Assert.IsNull(f.GetValue(w), "AdmissionWords." + f.Name + ": Türkçe ilkleyici yok (AGENTS kural 9)");
            Assert.IsNull(new FolderData().TagText, "etiket yok");

            // KOS aşama 2: aday grubu verisi de metinsiz başlar (bütün dizgiler kurucudan, o anki dilde)
            Assert.IsNull(s.Group, "grup yok");
            foreach (object data in new object[] { new CandidateGroupData(), new CandidateCardData(), new PerkRowData(), new ReplaceRowData() })
                foreach (var f in data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (f.FieldType == typeof(string) || f.FieldType == typeof(string[]))
                        Assert.IsNull(f.GetValue(data), data.GetType().Name + "." + f.Name + ": ilkleyici yok (AGENTS kural 9)");
            Assert.IsEmpty(new CandidateGroupData().Cards);
            Assert.IsEmpty(new CandidateGroupData().Replace);
            Assert.IsEmpty(new CandidateCardData().Perks);
        }

        [Test]
        public void AdmissionUi_ReplaceMarkBelongsToItsGroup()
        {
            var ui = new AdmissionUi { ReplaceId = "resident-4", ReplaceForGroup = "group:4:resident-9" };
            Assert.AreEqual("resident-4", ui.Replace("group:4:resident-9"));
            Assert.IsNull(ui.Replace("group:7:resident-12"), "grup değişince işaret geçersiz");
            Assert.IsNull(ui.Replace(null));
            ui.Reset();
            Assert.IsNull(ui.ReplaceId);
            Assert.IsNull(ui.ReplaceForGroup);
            // Aşama 7: eski görülen durum (belge tezgâhta, talimat sürümü, dış kayıt sonucu) yok; yan dosyada da alan yok
            Assert.IsNull(typeof(AdmissionUi).GetField("SeenOrdersVersion"));
            Assert.IsNull(typeof(SiginakDeskStore).GetNestedType("AdmissionSeen"));
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
            HasMethod(station, "PlayDecision", typeof(StampChoice), typeof(Action));
            HasMethod(station, "ReadPose", typeof(AdmissionPaper), typeof(int), typeof(float));
            HasMethod(station, "SetInteractive", typeof(bool));
            HasMethod(station, "Advance", typeof(float));
            foreach (var e in new[] { "CounterClicked", "DeskKeyClicked", "PaperClicked", "Sound" }) HasEvent(station, e);
            Assert.IsTrue(typeof(IHoverSource).IsAssignableFrom(station));

            var stamp = typeof(StampDecision);
            HasEvent(stamp, "Pressed");
            HasEvent(stamp, "Sound");
            HasMethod(stamp, "Bind", typeof(StampData));
            HasMethod(stamp, "ReturnToRack", typeof(bool));
            HasMethod(stamp, "Advance", typeof(float));
            Assert.IsNotNull(stamp.GetProperty("Held"));
            Assert.AreEqual(typeof(Func<Vector3, bool>), stamp.GetProperty("DropAccepts")?.PropertyType);
            Assert.IsNotNull(stamp.GetField("choice", BindingFlags.NonPublic | BindingFlags.Instance)?.GetCustomAttribute<SerializeField>(), "StampDecision.choice [SerializeField]");

            HasMethod(typeof(FileTagView), "Bind", typeof(string));
            Assert.IsNotNull(typeof(FileTagView).GetProperty("Overflowing"), "FileTagView.Overflowing");

            // KOS aşama 2: aday grubu görünümü ve yüzleri
            var group = typeof(CandidateGroupView);
            HasMethod(group, "Bind", typeof(CandidateGroupData), typeof(bool));
            HasMethod(group, "AcceptStamp", typeof(StampChoice), typeof(Vector3));
            HasMethod(group, "StampPose", typeof(StampChoice));
            HasMethod(group, "PaperOf", typeof(AdmissionPaper), typeof(int));
            HasMethod(group, "Stow", typeof(float));
            HasMethod(group, "HideAll");
            HasMethod(group, "SetShown", typeof(bool));
            HasMethod(group, "SetInteractive", typeof(bool));
            HasMethod(group, "Advance", typeof(float));
            foreach (var e in new[] { "CardClicked", "SlipClicked", "ReplaceClicked", "Sound" }) HasEvent(group, e);
            Assert.IsTrue(typeof(IHoverSource).IsAssignableFrom(group));
            Assert.IsNull(group.GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), "tek saat istasyonda: görünümün Update'i yok");
            HasMethod(typeof(CandidateCardFace), "Bind", typeof(CandidateCardData));
            HasMethod(typeof(CandidateCardFace), "ShowImpression", typeof(bool), typeof(bool));
            HasMethod(typeof(CandidateGroupSlipFace), "Bind", typeof(CandidateGroupData));
            HasMethod(typeof(CandidateGroupSlipFace), "ShowStamp", typeof(bool), typeof(bool));
            HasMethod(typeof(CandidateGroupSlipFace), "RowAt", typeof(Vector2));
            foreach (var face in new[] { typeof(CandidateCardFace), typeof(CandidateGroupSlipFace) })
                Assert.IsNotNull(face.GetProperty("Overflowing"), face.Name + ".Overflowing");
            HasMethod(station, "ConfigureCandidates", typeof(CandidateGroupView));
            HasEvent(station, "ReplaceClicked");
            Assert.IsNotNull(station.GetProperty("Candidates"));
            Assert.IsNull(station.GetField("retiredTools", BindingFlags.NonPublic | BindingFlags.Instance), "emekli araç listesi aşama 7'de söküldü");
            foreach (string field in new[] { "candidates" })
                Assert.IsNotNull(station.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.GetCustomAttribute<SerializeField>(), "AdmissionStation." + field + " [SerializeField]");
            HasMethod(typeof(SiginakAdmission), "PressStamp", typeof(StampChoice), typeof(string));
            HasMethod(typeof(SiginakAdmission), "ClickReplace", typeof(string));
            HasMethod(typeof(AdmissionSnapshotBuilder), "GroupKey", typeof(Simulation));
            HasMethod(typeof(AdmissionSnapshotBuilder), "GroupWaiting", typeof(Simulation));
            HasMethod(typeof(AdmissionSnapshotBuilder), "GroupLastShift", typeof(Simulation));
            HasMethod(typeof(AdmissionSnapshotBuilder), "StampCard", typeof(CandidateGroupData), typeof(string), typeof(Balance));
            HasMethod(typeof(AdmissionSnapshotBuilder), "StampPass", typeof(CandidateGroupData));

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
        }

        /// <summary>Kabukların gövdesi bugünkü davranışı değiştirmez: anlık görüntü boş, akış pasif, kapının yeni açılışı eskisiyle aynı yoldan.</summary>
        [Test]
        public void Shells_AreInert()
        {
            var sim = Simulation.NewGame(79, new Balance { shelterEventsEnabled = false, planningBoardEnabled = true, liberationAlarmPerShift = 0 });
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
                Assert.AreEqual(21, paths.Count, "43 (W0 + mühür) − KOS aşama 7'de eski kabul araçlarıyla silinen 22");
                var missing = paths.Where(p => !HasKey(t, p.StartsWith("colors.") ? p.Substring(7) : p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan yerel tokenlar: " + string.Join(", ", missing));
                Assert.IsFalse(t.strings.Any(s => s.key.Contains("_local_a6")), "notlar temaya girmez");
            }
            finally { Object.DestroyImmediate(t); }
        }

        /// <summary>KOS aşama 2 yerel tokenları (<c>_local_kos2</c>): aday kartlarının yeri ve kademe renkleri temada çözülür; değerler kilitli.</summary>
        [Test]
        public void Tokens_LocalKos2_EveryNewPathResolves()
        {
            var t = Fresh();
            try
            {
                var root = JObject.Parse(System.IO.File.ReadAllText(SiginakThemeSetup.TokensPath));
                var paths = ((JArray)root["_local_kos2"]["new"]).Select(p => (string)p).ToList();
                Assert.AreEqual(9, paths.Count);
                var missing = paths.Where(p => !HasKey(t, p)).ToList();
                Assert.IsEmpty(missing, "temada karşılığı olmayan yerel tokenlar: " + string.Join(", ", missing));
                Assert.IsFalse(t.strings.Any(s => s.key.Contains("_local_kos2")), "notlar temaya girmez");
                Assert.AreEqual("24592E", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.candidates.tier0")), "kefilli: KABUL izinin yeşili");
                Assert.AreEqual("854005", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.candidates.tier1")));
                Assert.AreEqual("9A1E14", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.candidates.tier2")));
                Assert.Greater(t.Number("admission.candidates.cardScale"), 0.5f);
                Assert.LessOrEqual(t.Number("admission.candidates.cardScale"), 1f);
                // Kayıtlı tema asset'i tokenlarla aynı (içe aktarım çalıştırılmış)
                var saved = SiginakTheme.Active;
                foreach (string key in paths.Where(p => !p.Contains("tier")))
                    Assert.AreEqual(t.Number(key), saved.NumberOf(key, float.NaN), 1e-6f, "SiginakTheme.asset: " + key);
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
                Assert.AreEqual(0.6f, t.Number("admission.motion.admitHoldS"), 1e-4);
                Assert.AreEqual(0.4f, t.Number("admission.motion.hoverDelayS"), 1e-4);
                Assert.AreEqual(0.318f, t.Number("admission.readDistanceM"), 1e-4);
                Assert.AreEqual(0.93f, t.Number("admission.stampMarks.opacity"), 1e-4);
                Assert.AreEqual("24592E", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.stampMarks.admit.color")));
                Assert.AreEqual(new Vector2(60f, 29.9f), t.Vector2Of("admission.stampMarks.admit.sizeMm"));
                Assert.AreEqual("9A1E14", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.stampMarks.reject.color")));
                Assert.AreEqual(68f, t.Number("admission.counterSpot.spotAngle"), 1e-4);
                Assert.AreEqual("9A1E14", ColorUtility.ToHtmlStringRGB(t.ColorOf("admission.paper.alarm")));
                // Paketin değerleri değişmedi
                Assert.AreEqual(0.4f, t.Number("admission.motion.counterLeafS"), 1e-4);
                Assert.AreEqual(0.8f, t.Number("admission.paperAlbedo"), 1e-4);
                // KOS aşama 7: eski kabul araçlarının tokenları silindi (okuyucuları yok)
                foreach (string gone in new[] { "admission.instruction", "admission.checks", "admission.decisions", "admission.supervisor", "admission.slipPathMm",
                             "admission.slipVerdict", "admission.stampMarks.restricted", "admission.stampMarks.aid", "admission.motion.toK8GS", "admission.motion.padTearS",
                             "admission.motion.markerS", "admission.motion.fogInS", "admission.paper.strip", "admission.paper.carbon", "admission.paper.seal", "personFile.pages" })
                    Assert.IsFalse(HasKey(t, gone), gone + " silindi");
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
