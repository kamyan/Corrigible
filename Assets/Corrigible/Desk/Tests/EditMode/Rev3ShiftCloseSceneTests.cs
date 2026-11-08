using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using Corrigible.Game.Audio;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 4 (R19 vardiya kapanışı) kaydedilmiş <c>DeskGame</c>: tek seferlik sahne script'inin (PLAN §2.2) kurduğu
    /// bağlar ve ölçüler. Plakanın iki satırı TR/EN sığar (E6), fiş yolu SlipMouth çapalarından (E16, Karar D12-B), kapanış
    /// odadaki şaltere ve modelin parçalarına bağlı (E17), EVRAK hedef ışığı (E18), K6 kadrajı (E19), token ↔ tablo ↔ kod metinleri
    /// (E7), çalışma kodunda ada göre arama yok (E20), kolun sesi (D10). Sahne açıkken (kullanıcının sahnesi) sahne testleri atlanır;
    /// sahne kaydedilmeden kapatılır.
    /// </summary>
    public sealed class Rev3ShiftCloseSceneTests
    {
        const string Path = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const string StringsPath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const string SlipViewPath = "Assets/Corrigible/Desk/Prefabs/Siginak/HandoverSlipView.prefab";
        const string SoundBankPath = "Assets/Corrigible/Settings/SoundBank.asset";
        /// <summary>CP-Bold SDF: punto birimi başına büyük harf (16 mm ↔ 0,27169812; D §2).</summary>
        const float CapMmToFontSize = 0.27169812f / 16f;
        /// <summary>Pirinç plakanın model yereli y aralığı (OBJ; D §1.1).</summary>
        const float BrassMinY = 0.0449f, BrassMaxY = 0.0770f;

        /// <summary>PLAN §4 plaka metinlerinin İngilizcesi: tabloda henüz yoksa ölçüm bunlarla yapılır (tablo denetimi E7'de).</summary>
        static readonly Dictionary<string, string> PlateEnglish = new Dictionary<string, string>
        {
            { "kapağı kaldır", "lift the cover" }, { "kolu indir", "pull the lever" }, { "fişi EVRAK’a koy", "file the slip in PAPERS" },
            { "fiş basılamadı", "slip not printed" }, { "baskın sürüyor", "raid in progress" }, { "harekât bitti", "campaign over" },
            { "belge açık", "document open" }, { "rapor açık", "report open" }, { "plan düzeltilmeli", "plan needs fixing" },
        };

        Scene scene, previous;
        GameObject[] roots;
        bool userScene, opened;

        [OneTimeSetUp]
        public void Prepare()
        {
            var existing = SceneManager.GetSceneByPath(Path);
            userScene = existing.IsValid() && existing.isLoaded;
            previous = SceneManager.GetActiveScene();
        }

        [OneTimeTearDown]
        public void CloseSavedSceneWithoutSaving()
        {
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }

        /// <summary>Kaydedilmiş sahneyi bir kez ekleyerek açar (PlanningBoardSceneTests kalıbı); kullanıcının açık sahnesine dokunmaz.</summary>
        void RequireScene()
        {
            if (userScene) Assert.Ignore("Sahne doğrulaması boş sahne açıkken çalıştırılmalı; kullanıcının açık sahnesine dokunulmadı.");
            if (opened) return;
            scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
            roots = scene.GetRootGameObjects();
            opened = true;
        }

        // ================================================================== E6 · plakanın iki satırı

        /// <summary>
        /// E6: 1. satır (VARDİYAYI BİTİR, CP-Bold 11,5 mm) ve 2. satır (CP-Bold 6,4 mm; dinamik, DeskLocText yok) otomatik puntosuz ve
        /// sarmasız; 2. satırın her durum ve her kilit nedeni metni TR ve EN'de 236 mm yuvaya tek satır sığar, çizilen sınırı pirinç
        /// plakanın içinde (model y 0,0449..0,0770) ve 1. satırla çakışmaz (C §9 #9).
        /// </summary>
        [Test]
        public void Plate_TwoLines_FitTrEn_NoAutoSize()
        {
            RequireScene();
            var theme = Theme();
            var desk = DeskTheme.Active;
            Assert.IsNotNull(desk, "DeskTheme");
            var sw = RoomSwitch();
            var model = ModelUnder(sw.transform, "ShiftSwitch");
            var line1 = model.Slot("TXT_Plate").Text;
            var plate = Reference<ShiftPlate>(sw, "plate");
            var line2 = Reference<TMP_Text>(plate, "line2");
            var problems = new List<string>();

            var loc1 = line1.GetComponent<DeskLocText>();
            if (loc1 == null || loc1.Source != theme.Text("shiftClose.plate.line1.tr"))
                problems.Add("TXT_Plate: DeskLocText kaynağı '" + (loc1 != null ? loc1.Source : "(yok)") + "' ≠ token shiftClose.plate.line1.tr");
            CheckStyle(line1, "TXT_Plate", desk.Font(DeskFont.CpBold), theme.NumberOf("shiftClose.plate.line1.capMm", 11.5f) * CapMmToFontSize, problems);
            if (line2.name != "TXT_Plate2") problems.Add("ShiftPlate.line2 '" + line2.name + "' (TXT_Plate2 bekleniyordu)");
            if (!line2.transform.IsChildOf(model.transform)) problems.Add("TXT_Plate2 ShiftSwitch modelinin içinde değil");
            if (line2.GetComponent<DeskLocText>() != null) problems.Add("TXT_Plate2: dinamik yazıda DeskLocText var");
            CheckStyle(line2, "TXT_Plate2", desk.Font(DeskFont.CpBold), theme.NumberOf("shiftClose.plate.line2CapMm", 6.4f) * CapMmToFontSize, problems);
            float slot = line2.rectTransform.rect.width;
            Assert.AreEqual(0.236f, slot, 1e-4f, "plaka yuvasının genişliği (manifest 236 mm)");

            var en = Loc.ParseTable(File.ReadAllText(StringsPath)).TryGetValue(Loc.English, out var table) ? new Dictionary<string, string>(table) : new Dictionary<string, string>();
            foreach (var kv in PlateEnglish) if (!en.ContainsKey(kv.Key)) en[kv.Key] = kv.Value;

            string text1 = line1.text, text2 = line2.text;
            bool wasEnglish = DeskLoc.Instance.Lang == DeskLang.En;
            int measured = 0;
            float minGap = float.MaxValue;
            try
            {
                foreach (var lang in new[] { Loc.Turkish, Loc.English })
                {
                    Loc.SetLanguage(lang, lang == Loc.English ? en : null);
                    line1.text = theme.Text(lang == Loc.English ? "shiftClose.plate.line1.en" : "shiftClose.plate.line1.tr");
                    line1.ForceMeshUpdate(true, true);
                    var b1 = line1.textBounds;
                    if (line1.textInfo.lineCount != 1 || b1.size.x > line1.rectTransform.rect.width * 1.001f)
                        problems.Add($"{lang} 1. satır '{line1.text}' {b1.size.x * 1000:0.0} mm, {line1.textInfo.lineCount} satır");
                    var ink1 = Ink(line1);
                    float line1Bottom = ink1.Min(p => p.y);
                    CheckOnBrass(line1, ink1, model.transform, lang + " 1. satır", problems);

                    foreach (var text in Line2Texts())
                    {
                        line2.text = text;
                        line2.ForceMeshUpdate(true, true);
                        var b = line2.textBounds;
                        measured++;
                        if (line2.textInfo.lineCount != 1 || b.size.x > slot * 1.001f)
                            problems.Add($"{lang} 2. satır '{text}' {b.size.x * 1000:0.0} mm > {slot * 1000:0} mm ya da {line2.textInfo.lineCount} satır");
                        var ink = Ink(line2);
                        CheckOnBrass(line2, ink, model.transform, lang + " 2. satır '" + text + "'", problems);
                        float top = ink.Select(p => line1.transform.InverseTransformPoint(line2.transform.TransformPoint(p)).y).Max();
                        minGap = Mathf.Min(minGap, line1Bottom - top);
                        if (top > line1Bottom + 1e-5f)
                            problems.Add($"{lang} 2. satır '{text}' 1. satırla çakışıyor: üst {top * 1000:0.00} mm > 1. satırın altı {line1Bottom * 1000:0.00} mm (plaka yereli)");
                    }
                }
            }
            finally
            {
                Loc.SetLanguage(Loc.Turkish);
                if (wasEnglish) DeskLoc.Instance.SetLanguage(DeskLang.En);
                line1.text = text1;
                line2.text = text2;
                line1.ForceMeshUpdate(true, true);
                line2.ForceMeshUpdate(true, true);
            }
            Assert.AreEqual(2 * 9, measured, "dört durum + beş kilit nedeni, iki dil");
            TestContext.WriteLine($"1. ve 2. satır arasında en dar boşluk {minGap * 1000:0.00} mm (mürekkep sınırı)");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>2. satırın o anki dilde üretilebilen bütün metinleri: dört durum (Locked hariç) ve <c>LockReason</c>'ın beş nedeni.</summary>
        static IEnumerable<string> Line2Texts()
        {
            yield return ShiftPlate.TextFor(ShiftPhase.Ready, null);
            yield return ShiftPlate.TextFor(ShiftPhase.CoverUp, null);
            yield return ShiftPlate.TextFor(ShiftPhase.Waiting, null);
            yield return ShiftPlate.TextFor(ShiftPhase.Rejecting, null);
            yield return SiginakShiftClose.LockReason(true, false, false, false, false, null).plate;
            yield return SiginakShiftClose.LockReason(false, true, false, false, false, null).plate;
            yield return SiginakShiftClose.LockReason(false, false, true, false, false, null).plate;
            yield return SiginakShiftClose.LockReason(false, false, false, true, false, null).plate;
            yield return SiginakShiftClose.LockReason(false, false, false, false, false, "plan").plate;
        }

        /// <summary>
        /// Görünür karakterlerin çizilen (mürekkep) sınırı, metnin yerelinde dört köşe: x <c>textBounds</c>'tan, y her karakterin
        /// dörtgeninin ortası ± glifinin yarı yüksekliği (SDF dolgusu sayılmaz; <c>textBounds</c> dolguyu da kapsar).
        /// </summary>
        static Vector3[] Ink(TMP_Text t)
        {
            float bottom = float.MaxValue, top = float.MinValue;
            var info = t.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;
                float mid = (ch.topLeft.y + ch.bottomLeft.y) * 0.5f;
                float half = (ch.topLeft.y - ch.bottomLeft.y) * 0.5f;
                if (ch.textElement != null && ch.textElement.glyph != null)
                    half = Mathf.Min(half, ch.textElement.glyph.metrics.height * ch.scale * 0.5f);
                bottom = Mathf.Min(bottom, mid - half);
                top = Mathf.Max(top, mid + half);
            }
            var b = t.textBounds;
            if (bottom > top) { bottom = b.min.y; top = b.max.y; }
            return new[] { new Vector3(b.min.x, bottom, 0), new Vector3(b.max.x, bottom, 0), new Vector3(b.min.x, top, 0), new Vector3(b.max.x, top, 0) };
        }

        static void CheckOnBrass(TMP_Text t, IEnumerable<Vector3> corners, Transform model, string what, List<string> problems)
        {
            foreach (var p in corners)
            {
                float y = model.InverseTransformPoint(t.transform.TransformPoint(p)).y;
                if (y < BrassMinY - 1e-5f || y > BrassMaxY + 1e-5f)
                {
                    problems.Add($"{what}: çizilen sınır pirinç plakanın dışında (model y {y * 1000:0.0} mm; plaka {BrassMinY * 1000:0.0}..{BrassMaxY * 1000:0.0})");
                    return;
                }
            }
        }

        static void CheckStyle(TMP_Text t, string what, TMP_FontAsset font, float size, List<string> problems)
        {
            if (font != null && t.font != font) problems.Add($"{what}: font {(t.font != null ? t.font.name : "(yok)")} ≠ {font.name}");
            if (Mathf.Abs(t.fontSize - size) > 2e-4f) problems.Add($"{what}: punto {t.fontSize:0.0000} ≠ {size:0.0000}");
            if (t.enableAutoSizing) problems.Add(what + ": otomatik punto açık (punto küçülmez)");
            if (t.textWrappingMode != TextWrappingModes.NoWrap) problems.Add(what + ": sarıyor (tek satır olmalı)");
        }

        // ================================================================== E16 · fiş yolu

        /// <summary>
        /// E16: fişin başı ve ucu SlipMouth modelinin SlipStart (−0,18; 0,14; 0,015) ve SlipExit (−0,30; 0,14; 0,015) çapaları (±0,5 mm,
        /// şalter yereli; token <c>shiftClose.slip.switchLocal*</c>), aralarındaki yol 120 mm (<c>travelMm</c>); Karar D12-B ile çıkış ağzın
        /// 40 mm içinden başlar, yol 160 mm. Dil pozu (−0,30; 0,1356; 0,015), dönüş (90, −90, 0): üst ağızdan uzakta, yüz yukarı; fiş
        /// orada pasif kaydedilir.
        /// </summary>
        [Test]
        public void SlipPath_UsesSlipMouthAnchors()
        {
            RequireScene();
            var theme = Theme();
            var sw = RoomSwitch();
            var mouth = ModelUnder(sw.transform, "SlipMouth");
            var paper = One<ShiftReceiptPaper>();
            var machine = One<ShiftReceiptMachine>();
            var start = Reference<Transform>(paper, "start");
            var exit = Reference<Transform>(paper, "output");
            Assert.AreSame(mouth.Anchor("SlipStart"), start, "çıkışın başı SlipStart çapası");
            Assert.AreSame(mouth.Anchor("SlipExit"), exit, "ağzın dış ucu SlipExit çapası");
            Assert.AreSame(exit, Reference<Transform>(machine, "output"));

            AssertNear(theme.Vector("shiftClose.slip.switchLocalStart"), sw.transform.InverseTransformPoint(start.position), 0.0005f, "SlipStart şalter yereli");
            AssertNear(theme.Vector("shiftClose.slip.switchLocalExit"), sw.transform.InverseTransformPoint(exit.position), 0.0005f, "SlipExit şalter yereli");
            Assert.AreEqual(theme.NumberOf("shiftClose.slip.travelMm", 120) / 1000f, Vector3.Distance(start.position, exit.position), 0.0005f, "çapalar arası 120 mm");

            var so = new SerializedObject(paper);
            Assert.AreEqual(0.04f, so.FindProperty("feedHiddenM").floatValue, 1e-5f, "D12-B: 40 mm içeriden");
            AssertNear(new Vector3(0, -0.0044f, 0), so.FindProperty("tongueOffset").vector3Value, 1e-5f, "dil ofseti");
            AssertNear(new Vector3(90, -90, 0), so.FindProperty("tongueEuler").vector3Value, 1e-3f, "dil dönüşü");
            var inward = (start.position - exit.position).normalized;
            float path = Vector3.Distance(start.position + inward * 0.04f, paper.TongueTarget.position);
            Assert.AreEqual(path, paper.FeedPathM, 1e-4f, "çıkış yolu SlipStart + 40 mm'den dile");
            Assert.AreEqual(0.160f, paper.FeedPathM, 0.0005f, "D12-B: 160 mm (tasarım 120 mm; sapma IMPLEMENTATION_MAP'te)");

            var tongue = paper.TongueTarget;
            var local = sw.transform.InverseTransformPoint(tongue.position);
            AssertNear(sw.transform.InverseTransformPoint(exit.position) + new Vector3(0, -0.0044f, 0), local, 0.0005f, "dil pozu şalter yereli (y 0,1356)");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(90, -90, 0), Quaternion.Inverse(sw.transform.rotation) * tongue.rotation), 0.05f, "dil dönüşü (90, −90, 0)");
            Assert.Less(sw.transform.InverseTransformDirection(tongue.rotation * Vector3.up).x, -0.99f, "fişin üstü ağızdan uzakta (şalter −x)");
            Assert.Greater(sw.transform.InverseTransformDirection(tongue.rotation * Vector3.back).y, 0.99f, "fişin yüzü (−z) yukarı");
            Assert.Less(Vector3.Distance(paper.transform.position, tongue.position), 1e-4f, "kaydedilmiş poz = dil pozu");
            Assert.Less(Quaternion.Angle(paper.transform.rotation, tongue.rotation), 0.05f, "kaydedilmiş dönüş = dil dönüşü");
            Assert.IsFalse(paper.gameObject.activeSelf, "basılmamış fiş görünmez");
        }

        // ================================================================== E17 · bağlar

        /// <summary>Kapanışın ve parçalarının dolu olması gereken alanları (tek seferlik sahne script'i yazar; R3/R8 bekçisi).</summary>
        static readonly (Type type, string[] fields)[] Required =
        {
            (typeof(SiginakShiftClose), new[] { "game", "surface", "desk", "rig", "shiftSwitch", "machine", "tray", "telex", "fade", "view", "briefing" }),
            (typeof(ShiftPlate), new[] { "line2", "target" }),
            (typeof(ShiftReceiptPaper), new[] { "output", "start", "dragSurface", "hitArea", "view", "face", "tray" }),
            (typeof(ShiftReceiptMachine), new[] { "shiftSwitch", "paper", "tray", "view", "output", "slipLamp" }),
            (typeof(EvrakTray), new[] { "sheet", "sheetTitle", "receiptDropArea", "targetLight", "slipOver", "filedSlip" }),
        };

        /// <summary>
        /// E17: makinenin ve kapanışın şalteri SiginakShift'inki (dolabın MENÜYE DÖN kolu değil, o eski kipte); şalterin kapak/kol/lamba/bant
        /// bağları ShiftSwitch modelinin Cover/Lever/ReadyLamp/LockBand parçalarında; fiş ağzı lambası SlipMouth/Lamp; plakanın 2. satırı
        /// modelin TXT_Plate2'si; fiş yüzü ve tepsideki kopya HandoverSlipView prefab'ının örnekleri (içinde HandoverSlip modeli);
        /// bütün zorunlu alanlar dolu.
        /// </summary>
        [Test]
        public void ShiftCloseIsWired()
        {
            RequireScene();
            var problems = new List<string>();
            foreach (var (type, fields) in Required)
            {
                var all = roots.SelectMany(r => r.GetComponentsInChildren(type, true)).ToList();
                if (all.Count != 1) { problems.Add(type.Name + ": sahnede " + all.Count + " tane (1 olmalı)"); continue; }
                var so = new SerializedObject(all[0]);
                foreach (var field in fields)
                {
                    var p = so.FindProperty(field);
                    if (p == null) problems.Add(type.Name + "." + field + ": alan yok");
                    else if (p.objectReferenceValue == null) problems.Add(type.Name + "." + field + ": boş");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));

            var shift = One<SiginakShift>();
            var close = One<SiginakShiftClose>();
            var rig = One<SiginakCameraRig>();
            var sw = RoomSwitch();
            var machine = One<ShiftReceiptMachine>();
            var paper = One<ShiftReceiptPaper>();
            var tray = One<EvrakTray>();
            Assert.AreSame(close, Reference<SiginakShiftClose>(shift, "close"));
            Assert.AreSame(close, Reference<SiginakShiftClose>(One<SiginakDesk>(), "shiftClose"));
            Assert.AreSame(One<GameController>(), Reference<GameController>(close, "game"));
            Assert.AreSame(One<LiberationDeskView>(), Reference<LiberationDeskView>(close, "surface"));
            Assert.AreSame(One<SiginakDesk>(), Reference<SiginakDesk>(close, "desk"));
            Assert.AreSame(rig, Reference<SiginakCameraRig>(close, "rig"));
            Assert.AreSame(sw, Reference<ShiftSwitch>(close, "shiftSwitch"));
            Assert.AreSame(machine, Reference<ShiftReceiptMachine>(close, "machine"));
            Assert.AreSame(tray, Reference<EvrakTray>(close, "tray"));
            Assert.AreSame(Reference<TelexFeed>(shift, "telex"), Reference<TelexFeed>(close, "telex"));
            Assert.AreSame(Reference<SiginakBriefing>(shift, "briefing"), Reference<SiginakBriefing>(close, "briefing"));
            foreach (var owner in new Object[] { close, machine, paper })
                Assert.AreSame(rig.View, Reference<Camera>(owner, "view"), owner.GetType().Name + ".view rig'in kamerası");
            var fade = Reference<CanvasGroup>(close, "fade");
            Assert.AreEqual("Planning shift blackout", fade.name);
            Assert.IsNull(fade.transform.parent, "kararma kök tuvalde");

            // Şalter: odadaki tek R19 şalteri; dolabın kolu eski kipte (Risk 1, 5)
            Assert.AreSame(sw, Reference<ShiftSwitch>(machine, "shiftSwitch"));
            Assert.AreSame(sw.gameObject, machine.gameObject);
            Assert.IsTrue(sw.ShiftCloseMode, "oda şalteri R19 kipinde");
            var others = roots.SelectMany(r => r.GetComponentsInChildren<ShiftSwitch>(true)).Where(s => s != sw).ToList();
            Assert.IsNotEmpty(others, "ayarlar dolabının MENÜYE DÖN kolu da ShiftSwitch");
            foreach (var other in others) Assert.IsFalse(other.ShiftCloseMode, other.name + ": eski kip (shiftClose kapalı)");

            var model = ModelUnder(sw.transform, "ShiftSwitch");
            Assert.IsTrue(Reference<Transform>(sw, "coverHinge").IsChildOf(model.Part("Cover").transform), "kapak = Cover parçası");
            Assert.IsTrue(Reference<Transform>(sw, "lever").IsChildOf(model.Part("Lever").transform), "kol = Lever parçası");
            Assert.IsTrue(Reference<Renderer>(sw, "readyLamp").transform.IsChildOf(model.Part("ReadyLamp").transform), "hazır lambası = ReadyLamp");
            Assert.IsTrue(Reference<GameObject>(sw, "lockBand").transform.IsChildOf(model.Part("LockBand").transform), "kilit bandı = LockBand");
            var mouth = ModelUnder(sw.transform, "SlipMouth");
            Assert.AreSame(mouth.Part("Lamp"), Reference<PartDriver>(machine, "slipLamp"), "fiş ağzı lambası = SlipMouth/Lamp");

            var plate = Reference<ShiftPlate>(sw, "plate");
            Assert.AreSame(sw.gameObject, plate.gameObject);
            var line2 = Reference<TMP_Text>(plate, "line2");
            Assert.AreEqual("TXT_Plate2", line2.name);
            Assert.IsTrue(line2.transform.IsChildOf(model.transform));
            var target = Reference<DeskTarget>(plate, "target");
            Assert.AreSame(sw.Target, target);
            Assert.AreSame(sw.GetComponent<DeskTarget>(), target);
            Assert.IsNull(new SerializedObject(target).FindProperty("lift").objectReferenceValue, "kapağı şalter döndürür (DeskTarget.lift boş)");
            Assert.IsNull(new SerializedObject(machine).FindProperty("status").objectReferenceValue, "plakayı yalnız ShiftPlate yazar");

            // Fiş yüzü ve tepsideki kopya
            var face = Reference<HandoverSlipFace>(paper, "face");
            CheckSlipInstance(face, "fiş yüzü");
            Assert.AreSame(paper.transform, face.transform.parent);
            Assert.Less(face.transform.localPosition.magnitude, 1e-6f, "kimlik yereli");
            Assert.Less(Quaternion.Angle(Quaternion.identity, face.transform.localRotation), 0.01f, "kimlik yereli");
            Assert.AreSame(tray, Reference<EvrakTray>(paper, "tray"));
            Assert.AreSame(tray, Reference<EvrakTray>(machine, "tray"));
            var filed = Reference<HandoverSlipFace>(tray, "filedSlip");
            CheckSlipInstance(filed, "tepsideki kopya");
            Assert.AreNotSame(face, filed);
            Assert.AreEqual("Filed slip", filed.name);
            Assert.AreSame(tray.transform, filed.transform.parent);
            Assert.IsFalse(filed.gameObject.activeSelf, "kopya dosyalanan fiş yokken gizli");
            var drop = tray.SlipDropPose;
            Assert.Less(Vector3.Distance(drop.position, filed.transform.position), 0.0005f, "kopya bırakma pozunda");
            Assert.Less(Quaternion.Angle(drop.rotation, filed.transform.rotation), 0.05f, "kopya bırakma pozunda");

            // Tek fiş çarpıştırıcısı: kökte, 100 × 150 × 6 mm, merkez z −2 mm (D §0.9)
            var box = Reference<Collider>(paper, "hitArea") as BoxCollider;
            Assert.IsNotNull(box, "hitArea BoxCollider");
            Assert.AreSame(paper.gameObject, box.gameObject);
            AssertNear(new Vector3(0.10f, 0.15f, 0.006f), box.size, 1e-5f, "fiş kutusu");
            AssertNear(new Vector3(0, 0, -0.002f), box.center, 1e-5f, "fiş kutusunun merkezi");
            Assert.AreEqual(1, paper.GetComponentsInChildren<Collider>(true).Length, "fiş ağacında tek çarpıştırıcı");
            Assert.IsEmpty(paper.GetComponentsInChildren<GraphicRaycaster>(true), "fiş yüzü ışın yakalamaz");
        }

        static void CheckSlipInstance(HandoverSlipFace face, string what)
        {
            Assert.IsTrue(PrefabUtility.IsOutermostPrefabInstanceRoot(face.gameObject), what + ": prefab örneğinin kökü");
            Assert.AreEqual(SlipViewPath, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(face.gameObject), what + ": HandoverSlipView örneği");
            Assert.IsTrue(face.GetComponentsInChildren<Rev3Model>(true).Any(m => m.ModelName == "HandoverSlip"), what + ": içinde HandoverSlip modeli");
            Assert.IsEmpty(face.GetComponentsInChildren<Collider>(true), what + ": çarpıştırıcısız");
        }

        /// <summary>
        /// PLAN §2.2 #3, #8, #9 ve §2.3: Revizyon 2 fiş küpü ve tuvali, eski sürükleme düzlemi ve öksüz ikinci karartma silindi; eski yolun
        /// parçaları (K6a pozu, amber ışık) kaldı.
        /// </summary>
        [Test]
        public void Rev2Leftovers_RemovedAndOldPathKept()
        {
            RequireScene();
            var paper = One<ShiftReceiptPaper>();
            Assert.IsNull(paper.transform.Find("Devir kâğıdı"), "Revizyon 2 küpü silindi");
            Assert.IsNull(paper.transform.Find("Fiş baskısı"), "Revizyon 2 tuvali silindi");
            Assert.IsEmpty(paper.GetComponentsInChildren<Canvas>(true).Where(c => c.GetComponentInParent<HandoverSlipFace>(true) == null),
                "fişin altında yüz dışında tuval yok");
            var room = roots.Single(r => r.name == "SiginakRoom").transform;
            Assert.IsNull(room.Find("Desk/Kâğıt taşıma düzlemi"), "eski sürükleme düzlemi silindi");
            Assert.AreEqual(1, roots.Count(r => r.name == "Planning shift blackout"), "öksüz ikinci karartma silindi");

            Assert.IsNotNull(room.Find("Poses/K6a"), "eski yolun K6a pozu kalır");
            Assert.IsTrue(One<SiginakCameraRig>().TryGetPose(SiginakPose.Shift, out var k6a) && k6a.anchor == room.Find("Poses/K6a"), "Shift → K6a");
            Assert.IsNotNull(room.Find("Desk/Shift switch/Amber light"), "eski ShiftSequence'ın amber ışığı kalır");
        }

        // ================================================================== E18 · EVRAK hedef ışığı

        /// <summary>
        /// E18: EVRAK hedef ışığı Point, renk / şiddet (× <c>targetIntensityScale</c>) / menzil / gölge <c>shiftClose.evrakHighlight</c>'tan,
        /// kaydedilmiş hâlde kapalı, tepsi kutusunun xz sınırında ve tepsinin üstünde, çarpıştırıcısız, PoseLighting'e bağlı değil; EVRAK
        /// üstü çapası tepsi yereli (0; 0,10; 0), dünya Euler (77,39; −4; 4,58).
        /// </summary>
        [Test]
        public void EvrakTargetLight()
        {
            RequireScene();
            var theme = Theme();
            var tray = One<EvrakTray>();
            var light = Reference<Light>(tray, "targetLight");
            Assert.AreSame(tray.transform, light.transform.parent);
            Assert.AreEqual("Slip target light", light.name);
            Assert.AreEqual(LightType.Point, light.type);
            Assert.IsTrue(theme.TryGet("shiftClose.evrakHighlight.color", out var colour), "token renk");
            Assert.AreEqual(colour.r, light.color.r, 1.01f / 255);
            Assert.AreEqual(colour.g, light.color.g, 1.01f / 255);
            Assert.AreEqual(colour.b, light.color.b, 1.01f / 255);
            float scale = new SerializedObject(tray).FindProperty("targetIntensityScale").floatValue;
            Assert.Greater(scale, 0f, "URP çarpanı");
            Assert.AreEqual(theme.NumberOf("shiftClose.evrakHighlight.intensity", 0.55f) * scale, light.intensity, 1e-4f);
            Assert.AreEqual(theme.NumberOf("shiftClose.evrakHighlight.range", 0.35f), light.range, 1e-4f);
            Assert.AreEqual(LightShadows.None, light.shadows, "gölgesiz (K6 gölge bütçesi 2)");
            Assert.IsFalse(light.enabled, "kaydedilmiş hâlde kapalı");
            Assert.IsEmpty(light.GetComponentsInChildren<Collider>(true));
            AssertNear(new Vector3(0, 0.11f, -0.02f), light.transform.localPosition, 0.001f, "three.js dress_room_switch.js:64");

            var box = Reference<Collider>(tray, "receiptDropArea") as BoxCollider;
            Assert.IsNotNull(box);
            var inBox = box.transform.InverseTransformPoint(light.transform.position) - box.center;
            Assert.LessOrEqual(Mathf.Abs(inBox.x), box.size.x / 2, "tepsinin xz sınırında (x)");
            Assert.LessOrEqual(Mathf.Abs(inBox.z), box.size.z / 2, "tepsinin xz sınırında (z)");
            var trayModel = ModelUnder(tray.transform, "PaperTray");
            Assert.Greater(trayModel.transform.InverseTransformPoint(light.transform.position).y, trayModel.ManifestBounds.max.y, "tepsinin dudağının üstünde");

            var lighting = roots.SelectMany(r => r.GetComponentsInChildren<PoseLighting>(true)).Single();
            Assert.IsFalse(Enumerable.Range(0, lighting.BindingCount).Any(i => lighting.BindingAt(i).light == light), "poz tarifi şiddetini ezmesin");

            var over = Reference<Transform>(tray, "slipOver");
            Assert.AreEqual("Slip over", over.name);
            Assert.AreSame(tray.transform, over.parent);
            AssertNear(new Vector3(0, 0.10f, 0), over.localPosition, 0.001f, "EVRAK üstü konum (tepsi yereli)");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(77.39f, -4f, 4.58f), over.rotation), 0.1f, "EVRAK üstü dönüş (dress_room_switch.js:59)");
            Assert.IsEmpty(over.GetComponentsInChildren<Collider>(true));
            Assert.AreSame(over, Reference<Transform>(One<ShiftReceiptPaper>(), "dragSurface"));
        }

        // ================================================================== E19 · K6

        /// <summary>
        /// E19: K6 pozu <c>cameras.K6</c> (0,40; 1,28; 0,42) → (0,665; 0,82; 1,08), FOV 42, lens kaydırması yok; 1920 × 1080'de şalter, fiş
        /// ağzı, dildeki fiş, EVRAK bırakma kutusu ve plakanın iki satırı dört köşesiyle kadrajda.
        /// </summary>
        [Test]
        public void K6Pose_FramesSwitchMouthTrayAndPlate()
        {
            RequireScene();
            var theme = Theme();
            var rig = One<SiginakCameraRig>();
            Assert.IsTrue(rig.TryGetPose(SiginakPose.ShiftClose, out var k6), "rig'de ShiftClose (K6) yuvası");
            var room = roots.Single(r => r.name == "SiginakRoom").transform;
            Assert.AreSame(room.Find("Poses/K6"), k6.anchor);
            var pos = theme.Vector("cameras.K6.pos");
            var target = theme.Vector("cameras.K6.target");
            AssertNear(pos, k6.anchor.position, 0.001f, "K6 konumu");
            Assert.Less(Vector3.Angle(target - pos, k6.anchor.forward), 0.05f, "K6 hedefe bakar");
            Assert.Less(Vector3.Angle(Vector3.up, k6.anchor.up), 90f, "K6 dik");
            Assert.AreEqual(theme.NumberOf("cameras.K6.fovV", 42), k6.fieldOfView, 1e-4f);
            Assert.AreEqual(0, k6.lensShiftY, 1e-6f);

            var sw = RoomSwitch();
            var switchModel = ModelUnder(sw.transform, "ShiftSwitch");
            var mouth = ModelUnder(sw.transform, "SlipMouth");
            var paper = One<ShiftReceiptPaper>();
            var paperBox = (BoxCollider)Reference<Collider>(paper, "hitArea");
            var trayBox = (BoxCollider)Reference<Collider>(One<EvrakTray>(), "receiptDropArea");
            var plate = Reference<ShiftPlate>(sw, "plate");

            var go = new GameObject("K6 kadraj testi", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var texture = new RenderTexture(1920, 1080, 0) { hideFlags = HideFlags.HideAndDontSave };
            var problems = new List<string>();
            try
            {
                var cam = go.GetComponent<Camera>();
                cam.enabled = false;
                cam.targetTexture = texture;
                cam.aspect = 16f / 9f;
                cam.usePhysicalProperties = true;
                cam.sensorSize = new SerializedObject(rig).FindProperty("sensorSizeMm").vector2Value;
                cam.gateFit = Camera.GateFitMode.Vertical;
                cam.focalLength = Camera.FieldOfViewToFocalLength(k6.fieldOfView, cam.sensorSize.y);
                cam.lensShift = new Vector2(0, k6.lensShiftY);
                cam.nearClipPlane = 0.03f;
                go.transform.SetPositionAndRotation(k6.anchor.position, k6.anchor.rotation);

                InFrame(cam, BoundsCorners(switchModel.ManifestBounds).Select(switchModel.transform.TransformPoint), "şalter", problems);
                InFrame(cam, BoundsCorners(mouth.ManifestBounds).Select(mouth.transform.TransformPoint), "fiş ağzı", problems);
                var tongue = paper.TongueTarget;
                var atTongue = Matrix4x4.TRS(tongue.position, tongue.rotation, Vector3.one);
                InFrame(cam, BoundsCorners(new Bounds(paperBox.center, paperBox.size)).Select(atTongue.MultiplyPoint3x4), "dildeki fiş", problems);
                InFrame(cam, BoundsCorners(new Bounds(trayBox.center, trayBox.size)).Select(trayBox.transform.TransformPoint), "EVRAK kutusu", problems);
                foreach (var t in new[] { switchModel.Slot("TXT_Plate").Text, Reference<TMP_Text>(plate, "line2") })
                {
                    var corners = new Vector3[4];
                    t.rectTransform.GetWorldCorners(corners);
                    InFrame(cam, corners, "plaka " + t.name, problems);
                }
            }
            finally
            {
                go.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(go);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        static IEnumerable<Vector3> BoundsCorners(Bounds b)
        {
            for (int i = 0; i < 8; i++)
                yield return b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
        }

        static void InFrame(Camera cam, IEnumerable<Vector3> world, string what, List<string> problems)
        {
            foreach (var w in world)
            {
                var v = cam.WorldToViewportPoint(w);
                if (v.z <= cam.nearClipPlane || v.x < 0.01f || v.x > 0.99f || v.y < 0.01f || v.y > 0.99f)
                {
                    problems.Add($"{what}: köşe kadraj dışında (görüntü {v.x:0.000}, {v.y:0.000}, z {v.z:0.000})");
                    return;
                }
            }
        }

        // ================================================================== E7 · metinler

        /// <summary>
        /// E7: R19 token metinlerinin (plaka 1. satır, dört durumun 2. satırı, fiş ağzı plakası, devir teleksi) Türkçesi çeviri
        /// tablosunda ve İngilizcesi token'la aynı (D3-A: EN'de PAPERS); kod aynı Türkçe anahtarları kullanır; sahnedeki sabit plaka
        /// yazıları token kaynağından.
        /// </summary>
        [Test]
        public void ShiftCloseTexts_TokenStringsAndTableAgree()
        {
            var theme = Theme();
            var tables = Loc.ParseTable(File.ReadAllText(StringsPath));
            tables.TryGetValue(Loc.English, out var en);
            en ??= new Dictionary<string, string>();
            var problems = new List<string>();
            foreach (var key in new[]
                     {
                         "shiftClose.plate.line1", "shiftClose.states.ready.plate2", "shiftClose.states.cover.plate2", "shiftClose.states.waiting.plate2",
                         "shiftClose.states.locked.plate2", "shiftClose.slipMouth.plate", "shiftClose.afterDrop.telex",
                     })
            {
                string tr = theme.Text(key + ".tr"), enToken = theme.Text(key + ".en");
                if (string.IsNullOrEmpty(tr) || string.IsNullOrEmpty(enToken)) { problems.Add(key + ": token yok"); continue; }
                if (!en.TryGetValue(tr, out var e)) problems.Add($"{key}: '{tr}' çeviri tablosunda yok (token EN '{enToken}')");
                else if (e != enToken) problems.Add($"{key}: strings.txt EN '{e}' ≠ token '{enToken}'");
            }
            Assert.AreEqual("file the slip in PAPERS", theme.Text("shiftClose.states.waiting.plate2.en"), "D3-A yerel ezme (_local_a4)");

            bool wasEnglish = DeskLoc.Instance.Lang == DeskLang.En;
            try
            {
                Loc.SetLanguage(Loc.Turkish);
                void Same(string got, string key, string what) { if (got != theme.Text(key)) problems.Add($"{what}: kod '{got}' ≠ token {key} '{theme.Text(key)}'"); }
                Same(ShiftPlate.TextFor(ShiftPhase.Ready, null), "shiftClose.states.ready.plate2.tr", "hazır");
                Same(ShiftPlate.TextFor(ShiftPhase.CoverUp, null), "shiftClose.states.cover.plate2.tr", "kapak kalkık");
                Same(ShiftPlate.TextFor(ShiftPhase.Waiting, null), "shiftClose.states.waiting.plate2.tr", "fiş bekliyor");
                Same(SiginakShiftClose.LockReason(true, false, false, false, false, null).plate, "shiftClose.states.locked.plate2.tr", "kilitli (baskın)");
            }
            finally { if (wasEnglish) DeskLoc.Instance.SetLanguage(DeskLang.En); }

            if (!userScene)
            {
                RequireScene();
                var sw = RoomSwitch();
                var plate1 = ModelUnder(sw.transform, "ShiftSwitch").Slot("TXT_Plate").Text.GetComponent<DeskLocText>();
                if (plate1 == null || plate1.Source != theme.Text("shiftClose.plate.line1.tr")) problems.Add("TXT_Plate kaynağı token line1.tr değil");
                var mouth = ModelUnder(sw.transform, "SlipMouth").Slot("TXT_Plate").Text.GetComponent<DeskLocText>();
                if (mouth == null || mouth.Source != theme.Text("shiftClose.slipMouth.plate.tr")) problems.Add("SlipMouth/TXT_Plate kaynağı token slipMouth.plate.tr değil");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // ================================================================== E20 · ada göre arama yok

        /// <summary>E20 (AGENTS kural 5): Desk çalışma ve köprü kodunda ada göre arama yok (<c>GameObject.Find</c>, <c>.Find("…")</c>,
        /// <c>transform.Find</c>, etiket araması). Yorum satırları sayılmaz.</summary>
        [Test]
        public void ShiftCloseCode_HasNoNameLookups()
        {
            var lookup = new Regex(@"GameObject\.Find\s*\(|\.Find\s*\(\s*""|\btransform\.Find\s*\(|FindWithTag\s*\(|FindGameObjectsWithTag\s*\(");
            var files = new[] { "Assets/Corrigible/Desk/Scripts/Runtime", "Assets/Corrigible/Desk/Scripts/Bridge" }
                .SelectMany(d => Directory.GetFiles(d, "*.cs", SearchOption.AllDirectories)).ToList();
            Assert.Greater(files.Count, 100, "kaynaklar bulundu");
            foreach (var r19 in new[] { "SiginakShiftClose.cs", "ShiftPlate.cs", "HandoverSlipFace.cs", "ShiftReceiptPaper.cs", "ShiftReceiptMachine.cs", "EvrakTray.cs", "ShiftSwitch.cs" })
                Assert.IsTrue(files.Any(f => f.Replace('\\', '/').EndsWith("/" + r19, StringComparison.Ordinal)), r19 + " taranır");
            var hits = new List<string>();
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].TrimStart();
                    if (line.StartsWith("//", StringComparison.Ordinal)) continue;
                    if (lookup.IsMatch(line)) hits.Add(file.Replace('\\', '/') + ":" + (i + 1) + "  " + line.Trim());
                }
            }
            Assert.IsEmpty(hits, "ada göre arama:\n" + string.Join("\n", hits));
        }

        // ================================================================== D10 · ses

        /// <summary>D10: kol sesi (cue 109, <c>ShiftLever</c>) bankada geçici olarak <c>SwitchClack</c> klibiyle tek girdi.</summary>
        [Test]
        public void SoundBank_LeverUsesSwitchClackClip()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankPath);
            Assert.IsNotNull(bank, SoundBankPath);
            Assert.AreEqual(109, (int)Cue.ShiftLever);
            var lever = bank.cues.Where(c => c.cue == Cue.ShiftLever).ToList();
            Assert.AreEqual(1, lever.Count, "cue 109 bankada bir kez");
            var clack = bank.Cue(Cue.SwitchClack);
            Assert.IsNotNull(clack.clip, "SwitchClack (92) klibi");
            Assert.AreSame(clack.clip, lever[0].clip, "geçici klip SwitchClack");
            Assert.That(lever[0].volume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
            Assert.AreEqual(Cue.ShiftLever, SiginakShiftClose.CueOf(ShiftSound.Lever));
        }

        /// <summary>D5 = c: kapanışın kol dışındaki sesleri (107, 108, 110–114) kendi yer tutucu kliplerinde; hiçbiri kolun geçici klibini ödünç almaz.</summary>
        [Test]
        public void SoundBank_ShiftCloseCuesHaveOwnClips()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankPath);
            Assert.IsNotNull(bank, SoundBankPath);
            var clack = bank.Cue(Cue.SwitchClack).clip;
            foreach (var cue in new[] { Cue.ShiftCoverOpen, Cue.ShiftCoverShut, Cue.SlipPrint, Cue.SlipTear, Cue.SlipFile, Cue.ShiftGong, Cue.SlipMouthEmpty })
            {
                Assert.IsNotNull(bank.Cue(cue).clip, cue + ": klip yok (D5 = c)");
                Assert.AreNotSame(clack, bank.Cue(cue).clip, cue.ToString());
            }
        }

        // ================================================================== yardımcılar

        static SiginakTheme Theme()
        {
            var theme = SiginakTheme.Active;
            Assert.IsNotNull(theme, "SiginakTheme (Resources)");
            return theme;
        }

        /// <summary>Odadaki vardiya şalteri (SiginakShift'in; sahnede dolabın kolu da ShiftSwitch, E §0.3).</summary>
        ShiftSwitch RoomSwitch() => Reference<ShiftSwitch>(One<SiginakShift>(), "shiftSwitch");

        static Rev3Model ModelUnder(Transform t, string name)
        {
            var model = t.GetComponentsInChildren<Rev3Model>(true).FirstOrDefault(m => m.ModelName == name);
            Assert.IsNotNull(model, t.name + " altında " + name + " modeli");
            return model;
        }

        T One<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();

        static T Reference<T>(Object owner, string field) where T : Object
        {
            var property = new SerializedObject(owner).FindProperty(field);
            Assert.IsNotNull(property, owner.GetType().Name + "." + field);
            var reference = property.objectReferenceValue as T;
            Assert.IsNotNull(reference, owner.GetType().Name + "." + field);
            return reference;
        }

        static void AssertNear(Vector3 expected, Vector3 actual, float tolerance, string what)
        {
            Assert.LessOrEqual(Vector3.Distance(expected, actual), tolerance,
                $"{what}: {actual.x:0.#####}, {actual.y:0.#####}, {actual.z:0.#####} ≠ {expected.x:0.#####}, {expected.y:0.#####}, {expected.z:0.#####}");
        }
    }
}
