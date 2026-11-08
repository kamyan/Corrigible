using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.EditorTools;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Revizyon 3 · Aşama 10 dil ve punto geçişi (PLAN §1.2 a, §1.3): 82 durumu tohum 79'la, 1920×1080'de, TR ve EN kurar; her durumda
    /// <see cref="SiginakTextScan.ScanLiveAll"/> ve görüntü. Eval'den sürülür (Play açık), ilerleme <see cref="StatusPath"/>'tedir.
    /// <list type="bullet">
    /// <item><b>Oturumlar:</b> <c>menu</c> (Giris açıkken), <c>k1</c>, <c>k4</c>, <c>k6</c>, <c>k8</c>, <c>hover</c>, <c>raid</c> (DeskGame açıkken).
    /// <see cref="Start"/> hemen döner; adımlar <c>EditorApplication.update</c> üstünde yürür (sahne yeniden yüklense de koşu sürer; AGENTS 6: yeni
    /// MonoBehaviour yok). <see cref="Sessions"/> adımları listeler; <c>only</c> yalnız seçilen kimlikleri koşar (kalan hataları yeniden çekmek için).</item>
    /// <item><b>Dil sırası (§0.4.3):</b> EN'de önce <c>DeskLoc.Instance.SetLanguage</c>, sonra oyun kurulur: Core'un gün 0 metinleri (YZ satırları, ekip adları) üretildikleri
    /// dilde kalır, kurulu oyunda dil değiştirmek ölçüm sayılmaz. <c>BridgeSettings.ApplyLanguage</c> kullanılmaz (PlayerPrefs yazar).</item>
    /// <item><b>Kayıt koruması (§0.4.2):</b> <c>SessionState "Corrigible.TestStorage"</c> yoksa koşu başlamaz.</item>
    /// <item><b>Hoşgörü:</b> her adım kendi denetleyicisinde sürülür; atılan istisna, zaman aşımı ya da eksik sahne adımı <c>FAIL</c> yazar, sonraki adıma geçilir, koşu
    /// hiçbir adımın başarısızlığıyla bitmez. Başarısızlıktan sonra bir sonraki adım sahneyi yeniden yükler.</item>
    /// <item><b>Çıktı:</b> <see cref="OutDir"/> altında <c>A10_&lt;ID&gt;_{TR,EN}.png</c>, TR koşusunda <c>…_gray.png</c>; ölçümler <see cref="SiginakTextScan.LiveAllPath"/>;
    /// durum dosyası her adımda yazılır, son satır <c>bitti</c> ile başlar. Tüm koşuların özeti <c>Temp/capture_set_all.txt</c>'ye eklenir.</item>
    /// </list>
    /// Kullanım: Play'den önce test deposu ve 1920×1080 çözünürlük (<c>PlayModeWindow.SetCustomRenderingResolution</c>); ilk oturumdan önce bir kez
    /// <c>SiginakTextScan.ResetLiveAll()</c>; sonra <c>Rev3CaptureSet.Start("k1", "en")</c> ve <see cref="StatusPath"/> izlenir. Kartlı oyun öncesi eski
    /// fikstürlere dayanan adımlar (K3d, FR) UNVERIFIED'dir, ilk koşuda görülür.
    /// </summary>
    public static class Rev3CaptureSet
    {
        public const string OutDir = "Docs/UI_Siginak_Rev3/screens_unity_rev3";
        public const string StatusPath = "Temp/capture_set.txt";
        const string AllStatusPath = "Temp/capture_set_all.txt";
        const string StorageKey = "Corrigible.TestStorage";
        const uint Seed = 79;
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        sealed class Step
        {
            public readonly string Session, Id;
            public readonly Func<IEnumerator> Body;
            public readonly string OnlyLang;
            public readonly float Timeout;
            public Step(string session, string id, Func<IEnumerator> body, string onlyLang = null, float timeout = 75f)
            { Session = session; Id = id; Body = body; OnlyLang = onlyLang; Timeout = timeout; }
        }

        // ---------------------------------------------------------------- oturum tablosu (PLAN §1.3)

        static readonly Step[] Steps =
        {
            new Step("menu", "B1", B1, "en"),
            new Step("menu", "M0a", M0a),
            new Step("menu", "M0b", M0b),
            new Step("menu", "H1_M0", () => MenuSweep("M0")),
            new Step("menu", "M1", M1),
            new Step("menu", "M2", M2),
            new Step("menu", "M2c", M2c),
            new Step("menu", "H1_M2", () => MenuSweep("M2")),
            new Step("menu", "MS", Ms),
            new Step("menu", "H1_MS", () => MenuSweep("MS")),
            new Step("menu", "M3", M3),

            new Step("k1", "K1a", K1a),
            new Step("k1", "K1b", K1b),
            new Step("k1", "K1c", K1c),
            new Step("k1", "K1d", K1d),
            new Step("k1", "BR", Br),
            new Step("k1", "K2a", K2a),
            new Step("k1", "K3a", K3a),
            new Step("k1", "K3b", K3b),
            new Step("k1", "K3c", K3c),
            new Step("k1", "K3d", K3d),
            new Step("k1", "C1", C1),
            new Step("k1", "C2", C2),
            new Step("k1", "C4", C4),

            new Step("k4", "K4a", K4a),
            new Step("k4", "K4b", K4b),
            new Step("k4", "K4c", K4c),
            new Step("k4", "K4d", K4d),
            new Step("k4", "K4e", K4e),
            new Step("k4", "K4f", () => Order("K4f", "durdu_aclik")),
            new Step("k4", "K4g", () => Order("K4g", "durdu_depo")),
            new Step("k4", "K4h", () => Order("K4h", "eksik")),
            new Step("k4", "K4i", () => Order("K4i", "fis8")),
            new Step("k4", "K4j", () => Order("K4j", "en_uzun")),
            new Step("k4", "K4k", () => Order("K4k", "ilerleme")),
            new Step("k4", "K4l", () => Order("K4l", "yeni")),
            new Step("k4", "K4m", K4m),
            new Step("k4", "K4n", K4n),
            new Step("k4", "F1", F1),
            new Step("k4", "F2", F2),
            new Step("k4", "F3", F3),
            new Step("k4", "BK1", () => Book("BK1", 0)),
            new Step("k4", "BK2", () => Book("BK2", 1)),
            new Step("k4", "BK3", () => Book("BK3", 2)),

            new Step("k6", "S1", S1),
            new Step("k6", "S2", S2),
            new Step("k6", "S3", S3),
            new Step("k6", "S4", S4),
            new Step("k6", "S5", () => Slip("S5", "V001")),
            new Step("k6", "S6", () => Slip("S6", "V031")),
            new Step("k6", "S7", () => Slip("S7", "worst")),
            new Step("k6", "S8", S8),
            new Step("k6", "K9a", K9a),
            new Step("k6", "K9b", K9b),
            new Step("k6", "FR1", () => FinalReport("FR1", "silme")),
            new Step("k6", "FR2", () => FinalReport("FR2", "dagilma")),

            new Step("k8", "D1", D1),
            new Step("k8", "D2", D2),
            new Step("k8", "A1", () => Admission("A1", "present")),
            new Step("k8", "A2", () => Admission("A2", "inspect")),
            new Step("k8", "A3", () => Admission("A3", "pending")),
            new Step("k8", "A4", () => Admission("A4", "stamped")),
            new Step("k8", "A5", () => Admission("A5", "aid")),
            new Step("k8", "A6", () => Admission("A6", "v3")),
            new Step("k8", "A7", A7),
            new Step("k8", "G1", G1),
            new Step("k8", "AR1", () => ReadPaper("AR1", "inspect", AdmissionPaper.Document, 0)),
            new Step("k8", "AR2", () => ReadPaper("AR2", "inspect", AdmissionPaper.Strip, 0)),
            new Step("k8", "AR3", () => ReadPaper("AR3", "inspect", AdmissionPaper.Slip, 0)),
            new Step("k8", "AR4", () => ReadPaper("AR4", "pending", AdmissionPaper.Request, 0)),

            new Step("hover", "H1", H1, timeout: 420f),

            new Step("raid", "C3", C3),
            new Step("raid", "Z1", Z1),
            new Step("raid", "Z5", () => RaidPanel("Z5", "rules")),
            new Step("raid", "Z6", () => RaidPanel("Z6", "log")),
            new Step("raid", "Z7", Z7),
            new Step("raid", "Z9", Z9),
            new Step("raid", "Z2", Z2),
            new Step("raid", "Z3", Z3),
            new Step("raid", "Z4", Z4),
            new Step("raid", "Z8", Z8),
            new Step("raid", "Z10", Z10),
            new Step("raid", "O1", O1),
            new Step("raid", "Z11", Z11),
            new Step("raid", "O2", O2),
        };

        /// <summary>Oturumlar ve adımları.</summary>
        public static string Sessions() =>
            string.Join("\n", Steps.GroupBy(s => s.Session).Select(g => g.Key + ": " + string.Join(" ", g.Select(s => s.Id))));

        // ---------------------------------------------------------------- koşu altyapısı

        static Runner current;

        /// <summary>
        /// Koşuyu başlatır ve hemen döner: <paramref name="session"/> (<see cref="Sessions"/>), <paramref name="lang"/> <c>tr</c> ya da <c>en</c>,
        /// <paramref name="only"/> virgülle ayrılmış kimlikler (boşsa oturumun hepsi). Çalışan koşu varsa reddeder.
        /// </summary>
        public static string Start(string session, string lang, string only = null)
        {
            if (!EditorApplication.isPlaying) return "Play gerekli";
            if (current != null) return "koşu sürüyor: " + current.Label;
            if (string.IsNullOrEmpty(SessionState.GetString(StorageKey, ""))) return "TestStorage ayarlı değil (SessionState \"" + StorageKey + "\"): oyuncunun kaydı korunamaz, koşu başlamadı";
            bool english = (lang ?? "").Trim().ToLowerInvariant() == "en";
            string code = english ? "en" : "tr";
            var ids = string.IsNullOrWhiteSpace(only) ? null : new HashSet<string>(only.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0));
            var steps = Steps.Where(s => s.Session == session && (s.OnlyLang == null || s.OnlyLang == code) && (ids == null || ids.Contains(s.Id))).ToList();
            if (steps.Count == 0) return "adım yok: " + session + " · " + (only ?? "") + "\n" + Sessions();
            string needed = session == "menu" ? "Giris" : "DeskGame";
            var active = SceneManager.GetActiveScene();
            if (!active.IsValid() || active.name != needed) return "sahne: " + needed + " gerekli, açık: " + active.name;
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(Path.GetDirectoryName(StatusPath));
            current = new Runner(session, english, steps);
            EditorApplication.update += Tick;
            return "başladı · " + current.Label + " · " + steps.Count + " adım · " + StatusPath;
        }

        /// <summary>Çalışan koşuyu durdurur (dili geri alır, ölçümleri birleştirir).</summary>
        public static string Stop()
        {
            if (current == null) return "koşu yok";
            current.Finish("durduruldu");
            return "durduruldu";
        }

        /// <summary>Durum dosyasının içeriği.</summary>
        public static string Status() => File.Exists(StatusPath) ? File.ReadAllText(StatusPath) : "henüz koşu yok";

        static void Tick()
        {
            if (current == null) { EditorApplication.update -= Tick; return; }
            if (!EditorApplication.isPlaying) { current.Finish("Play kapandı"); return; }
            current.Advance();
        }

        /// <summary>Bir eşyordam yığını: iç içe <c>IEnumerator</c>'ları yürütür, her istisnayı yakalar (sarmalayan eşyordam içindeki istisna dışarı sızmaz).</summary>
        sealed class Pump
        {
            readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
            public Exception Failure { get; private set; }
            public Pump(IEnumerator body) { if (body != null) stack.Push(body); }

            public bool MoveNext()
            {
                while (stack.Count > 0)
                {
                    var top = stack.Peek();
                    bool moved;
                    object yielded;
                    try { moved = top.MoveNext(); yielded = moved ? top.Current : null; }
                    catch (Exception e) { Failure = e; stack.Clear(); return false; }
                    if (!moved) { stack.Pop(); continue; }
                    if (yielded is IEnumerator inner) { stack.Push(inner); continue; }
                    return true;
                }
                return false;
            }
        }

        sealed class Runner
        {
            public readonly string Label;
            readonly string session;
            readonly bool english;
            readonly List<Step> steps;
            readonly StringBuilder log = new StringBuilder();
            readonly DeskLang startLang;
            int index = -1, ok, fail;
            Pump pump;
            float stepStart;
            readonly float runStart;
            readonly List<string> shots = new List<string>();
            readonly List<string> notes = new List<string>();
            readonly DeskLang bootLang;
            bool finished;
            string finishReason;

            public string Suffix => english ? "EN" : "TR";
            public bool English => english;
            public DeskLang BootLang => bootLang;
            public string Session => session;

            public Runner(string session, bool english, List<Step> steps)
            {
                this.session = session; this.english = english; this.steps = steps;
                Label = session + " " + Suffix;
                bootLang = DeskLoc.Instance.Lang;
                startLang = bootLang;
                DeskLoc.Instance.SetLanguage(english ? DeskLang.En : DeskLang.Tr);
                runStart = Time.realtimeSinceStartup;
                SiginakTextScan.BeginLiveAll();
                Line($"# Rev3CaptureSet · {Label} · {DateTime.Now:yyyy-MM-dd HH:mm} · {steps.Count} adım · tohum {Seed} · depo {SaveStore.StorageDirectory}");
            }

            public void Line(string s)
            {
                log.AppendLine(s);
                try
                {
                    File.WriteAllText(StatusPath, log.ToString());
                    File.AppendAllText(AllStatusPath, s + "\n");
                }
                catch (Exception e) { Debug.LogWarning("Rev3CaptureSet durum dosyası: " + e.Message); }
            }

            public void Note(string s) => notes.Add(s);
            public void Shot(string path) => shots.Add(path);

            public void Advance()
            {
                if (finished) return;
                try
                {
                    if (pump == null) { Next(); if (finished) return; }
                    if (!pump.MoveNext()) { Complete(pump.Failure); return; }
                    float limit = steps[index].Timeout;
                    if (Time.realtimeSinceStartup - stepStart > limit) Complete(new TimeoutException($"adım {limit:0} s içinde bitmedi"));
                }
                catch (Exception e) { Complete(e); }
            }

            void Next()
            {
                index++;
                if (index >= steps.Count) { Finish("bitti"); return; }
                notes.Clear();
                try { ClearTelexStrip(); } catch (Exception e) { Note("teleks şeridi: " + e.Message); }
                stepStart = Time.realtimeSinceStartup;
                pump = new Pump(steps[index].Body());
            }

            void Complete(Exception failure)
            {
                var step = steps[index];
                float took = Time.realtimeSinceStartup - stepStart;
                string detail = notes.Count > 0 ? string.Join(" ; ", notes) : "";
                if (failure == null) { ok++; Line($"OK   {step.Id,-6} {Suffix} {took,6:0.0}s {detail}"); }
                else
                {
                    fail++;
                    Line($"FAIL {step.Id,-6} {Suffix} {took,6:0.0}s {failure.GetType().Name}: {failure.Message} {(detail.Length > 0 ? "· " + detail : "")}");
                    needReload = true;
                    try { HideAllHover(); } catch (Exception) { }
                }
                pump = null;
            }

            public void Finish(string reason)
            {
                if (finished) return;
                finished = true;
                finishReason = reason;
                EditorApplication.update -= Tick;
                string gray = "", merged = "", restored = "";
                try { SiginakTextScan.EndLiveAll(); } catch (Exception e) { Debug.LogWarning(e); }
                try { if (!english) gray = WriteGray(shots) + " gri · "; } catch (Exception e) { gray = "gri hatası: " + e.Message + " · "; }
                try { merged = SiginakTextScan.WriteLiveAll(); } catch (Exception e) { merged = "ölçüm birleştirme hatası: " + e.Message; }
                try { RestoreEnvironment(startLang); restored = "dil " + startLang; } catch (Exception e) { restored = "geri yükleme hatası: " + e.Message; }
                Line($"{(reason == "bitti" ? "bitti" : "bitti (" + reason + ")")} · ok={ok} fail={fail} · {Time.realtimeSinceStartup - runStart:0}s · {gray}{merged} · {restored}");
                if (current == this) current = null;
            }
        }

        static Runner Cur => current;

        // ---------------------------------------------------------------- bekleme, yansıma, sanal işaretçi

        static IEnumerator Wait(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        static IEnumerator Frames(int count)
        {
            int until = Time.frameCount + count;
            float guard = Time.realtimeSinceStartup + 4;
            while (Time.frameCount < until && Time.realtimeSinceStartup < guard) yield return null;
        }

        static IEnumerator WaitFor(Func<bool> done, float seconds, string what)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!done())
            {
                if (Time.realtimeSinceStartup > until) throw new TimeoutException(what);
                yield return null;
            }
        }

        /// <summary><see cref="WaitFor"/> gibi; süre dolarsa adım düşmez, not düşülür ve görüntü yine alınır (görüntü, atılan istisnadan daha çok şey söyler).</summary>
        static IEnumerator WaitOrNote(Func<bool> done, float seconds, string what)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!done())
            {
                if (Time.realtimeSinceStartup > until) { Cur.Note("zaman aşımı: " + what); yield break; }
                yield return null;
            }
        }

        static T Any<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        /// <summary>Tam yol (kök dahil): üzerine gelme etiketlerinin dilden bağımsız kimliği.</summary>
        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>Teleks şeridindeki bekleyen satırları atar (DEFECTS §5.11): önceki durumun satırı bu durumun ölçümüne ve görüntüsüne düşmesin.</summary>
        static void ClearTelexStrip()
        {
            var strip = Get<TelexStrip>(Any<TelexFeed>(), "strip");
            if (strip != null) strip.Clear();
        }

        /// <summary>Teleks yazıcısı en yeni satırı basıp bitirene dek bekler (harf harf basım, <c>Teleprinter.TypedChars</c>); yarım satır görüntüye düşmesin.</summary>
        static IEnumerator PrinterDone()
        {
            var printer = Get<Teleprinter>(Any<TelexFeed>(), "printer");
            if (printer == null) yield break;
            yield return WaitOrNote(() => !Get<bool>(printer, "typing"), 4, "teleks yazıcısı bitmedi");
        }

        static T Get<T>(object owner, string name)
        {
            if (owner == null || owner is Object o && o == null) return default;
            for (var type = owner.GetType(); type != null; type = type.BaseType)
            {
                var f = type.GetField(name, Members);
                if (f != null) return f.GetValue(owner) is T value ? value : default;
                var p = type.GetProperty(name, Members);
                if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(owner) is T pv ? pv : default;
            }
            return default;
        }

        static object Call(object owner, string method, params object[] args)
        {
            for (var type = owner.GetType(); type != null; type = type.BaseType)
            {
                var m = type.GetMethods(Members).FirstOrDefault(x => x.Name == method && x.GetParameters().Length == args.Length);
                if (m != null) return m.Invoke(owner, args);
            }
            throw new MissingMethodException(owner.GetType().Name, method);
        }

        static void RaiseChanged()
        {
            var f = typeof(GameController).GetField("Changed", Members);
            (f?.GetValue(Game) as Action)?.Invoke();
        }

        static PointerEventData Ped(GameObject target, Vector2 screen) => new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left, position = screen, pressPosition = screen, pointerPress = target, rawPointerPress = target,
            pointerEnter = target, eligibleForClick = true, useDragThreshold = true, clickCount = 1,
        };

        static void Click(GameObject target)
        {
            var cam = CurrentCamera();
            var ped = Ped(target, cam != null ? (Vector2)cam.WorldToScreenPoint(target.transform.position) : Vector2.zero);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerExitHandler);
        }

        // ---------------------------------------------------------------- odanın tutamakları

        static GameController Game => Any<GameController>();
        static Simulation Sim => Game != null ? Game.Sim : null;
        static SiginakCameraRig Rig => Object.FindFirstObjectByType<SiginakCameraRig>(FindObjectsInactive.Include);
        static PlanningBoardPresenter Presenter => Any<PlanningBoardPresenter>();
        static BoardView3D Board => Any<BoardView3D>();
        static SiginakBriefing Briefing => Any<SiginakBriefing>();
        static SiginakMap MapView => Any<SiginakMap>();
        static SiginakAdmission AdmissionFlow => Any<SiginakAdmission>();
        static SiginakShiftClose Close => Any<SiginakShiftClose>();
        static LiveRaidDataSource Src => Any<LiveRaidDataSource>();
        static bool RaidOpen => RaidLauncher.IsRaidLoaded;

        static ShiftSwitch ShiftLever => Get<ShiftSwitch>(Close, "shiftSwitch");
        static ShiftReceiptPaper Paper => Get<ShiftReceiptMachine>(Close, "machine")?.Paper;

        static Camera RaidCamera()
        {
            var scene = SceneManager.GetSceneByName(RaidLauncher.SceneName);
            if (!scene.IsValid() || !scene.isLoaded) return null;
            return scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(false)).FirstOrDefault(c => c.enabled);
        }

        static Camera CurrentCamera()
        {
            if (RaidOpen) { var raid = RaidCamera(); if (raid != null) return raid; }
            var rig = Rig;
            if (rig != null) return rig.View;
            var entrance = Object.FindFirstObjectByType<EntranceCamera>(FindObjectsInactive.Include);
            return entrance != null ? entrance.View : Camera.main;
        }

        static bool needReload;
        static PhysicsRaycaster disabledRaycaster;

        /// <summary>Editördeki gerçek imleç görüntüye üzerine gelme etiketi düşürmesin (<c>SiginakCaptureSet</c> ile aynı): odanın fizik ışını kapalı.</summary>
        static void PhysicsOff()
        {
            var cam = Rig != null ? Rig.View : null;
            var ray = cam != null ? cam.GetComponent<PhysicsRaycaster>() : null;
            if (ray != null && ray.enabled) { ray.enabled = false; disabledRaycaster = ray; }
        }

        static void RestoreEnvironment(DeskLang lang)
        {
            if (disabledRaycaster != null) disabledRaycaster.enabled = true;
            disabledRaycaster = null;
            if (DeskLoc.Instance.Lang != lang) DeskLoc.Instance.SetLanguage(lang);
        }

        static void HideAllHover()
        {
            foreach (var tag in Object.FindObjectsByType<HoverTag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (tag != null && tag.Owner != null) tag.Hide(tag.Owner);
        }

        // ---------------------------------------------------------------- ölçüm ve görüntü

        static string ShotName(string id) => "A10_" + id + "_" + Cur.Suffix + ".png";

        /// <summary>Bir durumun taraması ve görüntüsü. Biri atarsa öteki yine çalışır; hatalar adımı FAIL yapar.</summary>
        static void Capture(string id, bool shot = true, Func<TMPro.TMP_Text, bool> only = null, Camera camera = null)
        {
            string scan = null, file = null;
            Exception first = null;
            try { scan = SiginakTextScan.ScanLiveAll(camera != null ? camera : CurrentCamera(), id, only); }
            catch (Exception e) { first = e; scan = "tarama hatası: " + e.Message; }
            if (shot)
            {
                try
                {
                    string path = Path.Combine(OutDir, ShotName(id));
                    file = CorrigibleDebug.Capture(path);
                    Cur.Shot(path);
                }
                catch (Exception e) { first = first ?? e; file = "görüntü hatası: " + e.Message; }
            }
            Cur.Note(id + ": " + (scan ?? "-") + (file != null ? " · " + Path.GetFileName(file) : ""));
            if (first != null) throw first;
        }

        /// <summary>Oda kamerasıyla (Overlay'siz) görüntü: raf ve K5a gibi Overlay dışı kareler için (<see cref="SiginakCapture.Render"/>; rig'in pozu geri alınır).</summary>
        static void RenderRoom(string id, string pose)
        {
            string path = SiginakCapture.Render(pose, ShotName(id));
            Cur.Shot(Path.Combine(OutDir, ShotName(id)));
            Cur.Note(id + " oda görüntüsü: " + path);
        }

        static string WriteGray(List<string> shots)
        {
            int n = 0;
            foreach (var path in shots.Where(File.Exists).Distinct())
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                try
                {
                    tex.LoadImage(File.ReadAllBytes(path));
                    var px = tex.GetPixels32();
                    for (int i = 0; i < px.Length; i++)
                    {
                        byte g = (byte)Mathf.RoundToInt(((Color)px[i]).grayscale * 255);
                        px[i] = new Color32(g, g, g, 255);
                    }
                    tex.SetPixels32(px);
                    File.WriteAllBytes(Path.ChangeExtension(path, null) + "_gray.png", tex.EncodeToPNG());
                    n++;
                }
                finally { Object.DestroyImmediate(tex); }
            }
            return n.ToString();
        }

        // ---------------------------------------------------------------- oyunu kurmak (dil önce değişti, §0.4.3)

        static string BriefingState() => typeof(SiginakBriefing).GetField("state", Members)?.GetValue(Briefing)?.ToString() ?? (Briefing.Busy ? "Ready" : "Idle");

        static IEnumerator SkipToDocument()
        {
            float until = Time.realtimeSinceStartup + 10;
            while (BriefingState() != "Ready")
            {
                Briefing.Skip();
                if (Time.realtimeSinceStartup > until) throw new TimeoutException("emir belgesi hazır olmadı (" + BriefingState() + ")");
                yield return null;
            }
        }

        static IEnumerator PressStart()
        {
            for (int press = 0; press < 3 && BriefingState() == "Ready"; press++)
            {
                yield return WaitFor(() => !(Get<object>(Briefing, "view") is BriefingView v && v != null && v.Animating), 5, "emir belgesi canlanması bitmedi");
                Call(Briefing, "Action");
                yield return null;
            }
            yield return WaitFor(() => !Briefing.Busy, 10, "emir belgesi kapanmadı (BAŞLA)");
            yield return WaitFor(() => !Game.StoryHold, 3, "hikâye kilidi kalkmadı");
        }

        /// <summary>
        /// Yeni harekât (tohum 79). <paramref name="passBriefing"/> false ise belge hazır beklerken durur (BR). Hata sonrası sahne yeniden yüklenir.
        /// </summary>
        static IEnumerator Fresh(bool passBriefing = true)
        {
            if (needReload) yield return LoadDesk(GameLaunch.Mode.None);
            if (Game == null) throw new InvalidOperationException("GameController yok (DeskGame açık değil)");
            Game.NewGame(Seed);
            // Menüden gelen oyuncuda kayıt özeti yoktur (SiginakBriefing.OpenOpening(entrance: true)); doğrudan DeskGame açılışının "KAYIT VAR" uyarısı
            // (geliştirici yolu, PLAN:140) durumlara düşmesin. Kayıt sekmesini BR bir kez gösterir: passBriefing false iken dokunulmaz.
            if (passBriefing) typeof(SiginakBriefing).GetField("save", Members)?.SetValue(Briefing, null);
            yield return SkipToDocument();
            if (passBriefing)
            {
                yield return PressStart();
                Rig.Snap(SiginakPose.Desk);
            }
            ClearTelexStrip();
            yield return PrinterDone();
            yield return Frames(3);
            PhysicsOff();
        }

        static IEnumerator LoadDesk(GameLaunch.Mode mode)
        {
            GameLaunch.Pending = mode;
            var op = SceneUtility.GetBuildIndexByScenePath(SiginakTextScan.DeskScene) >= 0
                ? SceneManager.LoadSceneAsync(SiginakTextScan.DeskScene, LoadSceneMode.Single)
                : EditorSceneManager.LoadSceneAsyncInPlayMode(SiginakTextScan.DeskScene, new LoadSceneParameters(LoadSceneMode.Single));
            if (op == null) throw new InvalidOperationException("DeskGame yüklenemedi");
            while (!op.isDone) yield return null;
            yield return Frames(2);
            needReload = false;
            if (mode == GameLaunch.Mode.None) yield return WaitFor(() => Briefing != null && Briefing.Busy, 6, "açılış başlamadı");
            else yield return WaitFor(() => GameLaunch.Pending == GameLaunch.Mode.None, 3, "kayıttan devam alınmadı");
            PhysicsOff();
        }

        /// <summary>V.(n+1) kapıda/masada: <c>AdmissionFixtures</c> tarifi (panonun açılış komutları + kapanışlar).</summary>
        static Simulation FixtureAt(int day, bool exact = true)
        {
            var sim = AdmissionFixtures.NewSimulation(Seed);
            AdmissionFixtures.CloseTo(sim, day);
            if (exact && sim.S.Day != day) throw new InvalidOperationException($"fikstür {day}. güne varmadı (gün {sim.S.Day}: kapanış reddedildi)");
            return sim;
        }

        /// <summary>Fikstürü kayıt dosyasına yazar, KAYITTAN DEVAM eder ve dönüş açılışını atlatır (K1'de durur; AdmissionPlayKit.ContinueFrom ile aynı).</summary>
        static IEnumerator ContinueFrom(Simulation sim)
        {
            sim.Record.day = sim.S.Day;
            string desk = Path.Combine(SaveStore.StorageDirectory, SiginakDeskStore.FileName);
            if (File.Exists(desk)) File.Delete(desk);
            SaveStore.Write(sim.Record);
            if (SaveStore.Load() == null) throw new InvalidOperationException("fikstür kaydı okunamadı");
            yield return LoadDesk(GameLaunch.Mode.Continue);
            float until = Time.realtimeSinceStartup + 10;
            while (Briefing.Busy)
            {
                Briefing.Skip();
                if (Time.realtimeSinceStartup > until) throw new TimeoutException("dönüş açılışı bitmedi");
                yield return null;
            }
            yield return WaitFor(() => !Game.StoryHold, 3, "kayıttan devamda hikâye kilidi kalkmadı");
            yield return PrinterDone();
            yield return Frames(3);
            PhysicsOff();
        }

        static IEnumerator ToPose(SiginakPose pose, float settle = 0.5f)
        {
            Rig.Focus(pose);
            yield return WaitFor(() => !Rig.InTransition && Rig.Current == pose, 5, "poz: " + pose);
            yield return Wait(settle);
        }

        // ---------------------------------------------------------------- Giris (M0–MS)

        static EntranceController Entrance => Object.FindFirstObjectByType<EntranceController>(FindObjectsInactive.Include);
        static TimeClock Clock => Get<TimeClock>(Entrance, "clock");
        static IEnumerator MenuReady()
        {
            if (needReload)
            {
                var load = SceneManager.LoadSceneAsync(GameLaunch.EntranceScene, LoadSceneMode.Single);
                if (load == null) throw new InvalidOperationException("Giris yüklenemedi");
                while (!load.isDone) yield return null;
                yield return Frames(2);
                needReload = false;
            }
            var ctrl = Entrance;
            if (ctrl == null) throw new InvalidOperationException("EntranceController yok (Giris açık değil)");
            yield return WaitFor(() => ctrl.Ready, 6, "menü hazır olmadı");
            PhysicsOffEntrance();
        }

        static void PhysicsOffEntrance()
        {
            var view = Get<EntranceCamera>(Entrance, "cam")?.View;
            var ray = view != null ? view.GetComponent<PhysicsRaycaster>() : null;
            if (ray != null && ray.enabled) { ray.enabled = false; disabledRaycaster = ray; }
        }

        static IEnumerator CamMenu(string anchorField, string fovField)
        {
            var ctrl = Entrance;
            var cam = Get<EntranceCamera>(ctrl, "cam");
            var anchor = Get<Transform>(ctrl, anchorField);
            if (cam == null || anchor == null) throw new InvalidOperationException("çapa yok: " + anchorField);
            cam.Go(anchor, Get<float>(ctrl, fovField), 0);
            yield return Frames(2);
        }

        static IEnumerator B1()
        {
            if (Cur.BootLang != DeskLang.En)
                throw new InvalidOperationException("Temiz EN açılış için Play'den önce PlayerPrefs corrigible.language=en olmalı (şimdi " + Cur.BootLang + ")");
            yield return MenuReady();
            yield return CamMenu("overview", "overviewFieldOfView");
            Capture("B1");
        }

        static IEnumerator M0a()
        {
            yield return MenuReady();
            Clock.HideCard();
            yield return CamMenu("overview", "overviewFieldOfView");
            yield return Wait(0.3f);
            Capture("M0a");
        }

        static IEnumerator M0b()
        {
            yield return MenuReady();
            Clock.ShowCard(13, 3, 12);
            yield return CamMenu("overview", "overviewFieldOfView");
            yield return Wait(0.3f);
            Capture("M0b");
        }

        static IEnumerator M1()
        {
            yield return MenuReady();
            var cabinet = SettingsCabinet.Instance;
            if (cabinet == null) throw new InvalidOperationException("dolap yok");
            cabinet.RequestOpen();
            yield return WaitFor(() => cabinet.Current == SettingsCabinet.State.Open, 4, "dolap açılmadı");
            yield return Wait(0.4f);
            Capture("M1");
            yield return Sweep("M1");
            cabinet.Close();
            yield return WaitFor(() => cabinet.Current == SettingsCabinet.State.Closed, 4, "dolap kapanmadı");
        }

        static IEnumerator M2()
        {
            yield return MenuReady();
            Clock.ShowCard(13, 3, 12);
            yield return CamMenu("clockPose", "clockFieldOfView");
            yield return Wait(0.3f);
            Capture("M2");
        }

        static IEnumerator M2c()
        {
            yield return MenuReady();
            var clock = Clock;
            clock.ShowCard(13, 3, 12);
            yield return CamMenu("clockPose", "clockFieldOfView");
            if (!clock.Armed) Call(clock, "Stack");
            if (!clock.Armed) throw new InvalidOperationException("yeni harekât kurulmadı");
            yield return Wait(0.3f);
            Capture("M2c");
            yield return Sweep("M2c");
            clock.Disarm();
        }

        static IEnumerator Ms()
        {
            yield return MenuReady();
            var sw = Get<ShiftSwitch>(Entrance, "mainSwitch");
            if (sw == null) throw new InvalidOperationException("son şalter yok");
            yield return CamMenu("switchPose", "switchFieldOfView");
            sw.Press();
            yield return WaitFor(() => !sw.Moving, 3, "kapak açılmadı");
            yield return Wait(0.6f);
            Capture("MS");
            yield return Sweep("MS_kapak");
            sw.Cancel();
            yield return WaitFor(() => !sw.Moving, 3, "kapak kapanmadı");
            yield return CamMenu("overview", "overviewFieldOfView");
        }

        /// <summary>Kart delgisi, sonra kapı ve siyah tutuş; metin beklenmez, görüntü kart delinirken alınır. Son adım: sahne DeskGame'e geçer.</summary>
        static IEnumerator M3()
        {
            yield return MenuReady();
            var clock = Clock;
            var ctrl = Entrance;
            clock.ShowCard(13, 3, 12);
            yield return CamMenu("clockPose", "clockFieldOfView");
            Call(ctrl, "Enter", GameLaunch.Mode.Continue);
            yield return WaitFor(() => clock.Punching, 3, "delgi başlamadı");
            yield return Wait(0.70f);   // kaldırma 0,12 + taşıma 0,40 + itme 0,14 s: kart yuvada (TimeClock.PunchRoutine)
            Capture("M3");
            // Sahne geçişi başlamadan koşu sonlanır: yükleme sonrası EntranceController yok olur
            yield return Wait(0.5f);
        }

        static IEnumerator MenuSweep(string label)
        {
            yield return MenuReady();
            if (label == "M0") { Clock.ShowCard(13, 3, 12); yield return CamMenu("overview", "overviewFieldOfView"); }
            else if (label == "M2") yield return CamMenu("clockPose", "clockFieldOfView");
            else yield return CamMenu("switchPose", "switchFieldOfView");
            yield return Sweep(label);
            if (label == "M0") yield return M0Targets();
        }

        /// <summary>
        /// H1 · M0'ın zorunlu hedefleri (PLAN §1.3 H1, DEFECTS §5.13): puantaj saati, kart, kart destesi (kurulu ve kurulmamış satır), son şalter ve kapı, kadraj
        /// süzgecine bakılmadan tek tek ölçülür (önceki koşuda M0 süpürmesi yalnız dolap hedeflerini verdi).
        /// </summary>
        static IEnumerator M0Targets()
        {
            var clock = Clock;
            var stack = Get<DeskTarget>(clock, "stackTarget");
            yield return ForceTag(Get<DeskTarget>(clock, "clockTarget"));
            yield return ForceTag(Get<DeskTarget>(clock, "cardTarget"));
            yield return ForceTag(stack);
            var sw = Get<ShiftSwitch>(Entrance, "mainSwitch");
            yield return ForceTag(sw != null ? sw.Target : null);
            yield return ForceTag(Get<DeskTarget>(Entrance, "doorTarget"));
            if (stack == null || clock.Armed) yield break;
            Call(clock, "Stack");
            if (!clock.Armed) yield break;
            yield return ForceTag(stack, "~kurulu");
            clock.Disarm();
        }

        /// <summary>Tek hedefin etiketini açar ve yalnız etiketin yazılarını ölçer; ekran noktası kadraja sıkıştırılır (etiket kâğıdı ekranda kalsın).</summary>
        static IEnumerator ForceTag(DeskTarget target, string suffix = "")
        {
            if (target == null) { Cur.Note("H1 M0: hedef bağı yok"); yield break; }
            string label = "H1/M0/" + PathOf(target.transform) + suffix;
            var cam = CurrentCamera();
            var tag = TagOf(target);
            var p = cam.WorldToScreenPoint(target.transform.position);
            tag.ShowAt(target, new Vector2(Mathf.Clamp(p.x, 60, Screen.width - 60), Mathf.Clamp(p.y, 60, Screen.height - 60)), true);
            yield return Frames(2);
            try { Cur.Note(label + ": " + SiginakTextScan.ScanLiveAll(cam, label, OnlyPaper(tag))); }
            finally { tag.Hide(target); }
        }

        // ---------------------------------------------------------------- K1, harita, CRT

        static IEnumerator K1a() { yield return Fresh(); yield return Wait(0.8f); Capture("K1a"); }

        static IEnumerator K1b()
        {
            yield return ContinueFrom(FixtureAt(4));
            yield return Wait(0.8f);
            Capture("K1b");
        }

        static IEnumerator K1c()
        {
            yield return Fresh();
            HandoverSlipFixtures.Present("V031");
            var board = Any<FreedRegionsBoard>();
            if (board == null) throw new InvalidOperationException("FreedRegionsBoard yok");
            var freed = new List<(string name, int shift)>
            {
                (GameData.Region("weu").Name, 6), (GameData.Region("eeu").Name, 12), (GameData.Region("me").Name, 19),
                (GameData.Region("af").Name, 24), (GameData.Region("ru").Name, 30),
            };
            board.Show(freed, GameData.Region("in").Name, false);
            yield return Wait(0.6f);
            Capture("K1c");
        }

        static IEnumerator K1d()
        {
            yield return ContinueFrom(FixtureAt(1, false));
            yield return Wait(0.8f);
            Capture("K1d");
        }

        static IEnumerator Br()
        {
            yield return Fresh(false);
            yield return WaitFor(() => !(Get<object>(Briefing, "view") is BriefingView v && v != null && v.Animating), 5, "belge canlanması bitmedi");
            yield return Wait(0.4f);
            Capture("BR");
            needReload = true;
        }

        static IEnumerator K2a()
        {
            yield return Fresh();
            yield return ToPose(SiginakPose.Map);
            Capture("K2a");
        }

        static IEnumerator Prep() { if (needReload || Game == null) yield return Fresh(); }

        static IEnumerator K3a()
        {
            yield return Prep();
            if (Rig.Current != SiginakPose.Map) yield return ToPose(SiginakPose.Map);
            var region = Object.FindObjectsByType<RegionMeshGraphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(r => r.RegionId == Sim.S.Liberation.CurrentRegion)
                ?? Object.FindObjectsByType<RegionMeshGraphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault();
            if (region == null) throw new InvalidOperationException("harita bölgesi yok");
            ExecuteEvents.Execute(region.gameObject, Ped(region.gameObject, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), ExecuteEvents.pointerEnterHandler);
            yield return Wait(0.7f);
            Capture("K3a");
            ExecuteEvents.Execute(region.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
        }

        static IEnumerator K3b()
        {
            yield return Prep();
            yield return ToPose(SiginakPose.Map);
            MapView.ShowMissions();
            yield return WaitFor(() => MapView.SlipOpen, 3, "görev fişi açılmadı");
            yield return Wait(1.0f);
            Capture("K3b");
        }

        static IEnumerator K3c()
        {
            yield return Prep();
            if (!MapView.SlipOpen) { MapView.ShowMissions(); yield return Wait(1.2f); }
            MapView.HandleEnter();
            yield return WaitFor(() => MapView.SquadReportOpen, 3, "kadro raporu açılmadı");
            yield return Wait(0.8f);
            Capture("K3c");
            MapView.HandleEscape();
            MapView.HandleEscape();
            yield return Wait(0.6f);
        }

        /// <summary>Kurtuluş dizisi ve karşı saldırı fikstürleri (kartlı oyunda UNVERIFIED): ikisi ayrı görüntü (K3d1, K3d2).</summary>
        static IEnumerator K3d()
        {
            yield return Fresh();
            Cur.Note(SiginakFixtures.Load("V014"));
            yield return Wait(1.2f);
            Cur.Note(SiginakFixtures.Load("V015ca"));
            yield return Wait(5f);
            Capture("K3d1");
            yield return Fresh();
            Cur.Note(SiginakFixtures.Load("V014"));
            yield return Wait(1.2f);
            Cur.Note(SiginakFixtures.Load("V019"));
            yield return Wait(5f);
            Capture("K3d2");
            needReload = true;
        }

        static IEnumerator C1() { yield return Fresh(); yield return ToPose(SiginakPose.Crt); yield return Wait(0.6f); Capture("C1"); }

        static IEnumerator C2()
        {
            yield return Fresh();
            InvalidatePlan();
            yield return ToPose(SiginakPose.Crt);
            yield return Wait(0.6f);
            Capture("C2");
        }

        /// <summary>Telsiz defteri dolu (DEFECTS §5.10): V.014'ün üç baskını ve karşı saldırı satırı (K3d1 ile aynı fikstür; gün 4'te defter iki satırdı).</summary>
        static IEnumerator C4()
        {
            yield return Fresh();
            Cur.Note(SiginakFixtures.Load("V014"));
            yield return Wait(1.2f);
            Cur.Note(SiginakFixtures.Load("V015ca"));
            yield return Wait(1.2f);
            yield return ToPose(SiginakPose.Crt);
            yield return Wait(0.6f);
            Capture("C4");
            needReload = true;
        }

        /// <summary>Teknisyeni Boşta'ya alır: plan geçersiz olur (C2 devre dışı, S4 kilitli; gerçek komut, bir sonraki Fresh sıfırlar).</summary>
        static void InvalidatePlan()
        {
            var tek = Sim.Board.People.FirstOrDefault(p => p.Alive && p.Job == "tek");
            if (tek == null) throw new InvalidOperationException("Teknisyen yok");
            Sim.AssignResident(tek.Id, "rest");
            Sim.DrainEvents();
            RaiseChanged();
        }

        // ---------------------------------------------------------------- K4

        static IEnumerator AtBoard(bool fresh = false)
        {
            if (fresh || needReload) yield return Fresh();
            if (Rig.Current != SiginakPose.Shelf)
            {
                Presenter.Show();
                yield return WaitFor(() => !Rig.InTransition && Rig.Current == SiginakPose.Shelf, 5, "K4'e varılmadı");
            }
            yield return Wait(0.7f);
        }

        static IEnumerator K4a() { yield return AtBoard(true); Capture("K4a"); }

        static IEnumerator K4b()
        {
            yield return AtBoard();
            RenderMax();
            yield return Wait(1.0f);
            Capture("K4b");
            Presenter.Refresh();
            yield return Wait(0.4f);
        }

        /// <summary>Baskın sonrası operasyon raporu inip dosyalanana kadar bekler: rapor okunmadan masa kilitli (LiberationDeskView.InputBlocked).</summary>
        static IEnumerator FileOperationReport()
        {
            var op = Any<SiginakOperation>();
            if (op == null) throw new InvalidOperationException("SiginakOperation yok");
            yield return WaitFor(() => op.ReportOpen, 6, "operasyon raporu açılmadı");
            var report = Get<OperationReportView>(op, "report");
            yield return WaitFor(() => !report.Animating, 4, "operasyon raporu inmedi");
            Call(op, "File");
            yield return WaitFor(() => !op.Busy, 4, "operasyon raporu dosyalanmadı");
        }

        static IEnumerator K4c()
        {
            yield return Fresh();
            var next = Sim.NextLiberationSector(Sim.S.Liberation.CurrentRegion);
            var r = Sim.StartLiberationRaid(next.Id);
            if (!r.Ok) throw new InvalidOperationException("baskın: " + r.Msg);
            var squad = Sim.S.Raid.Params.Squad;
            var res = new RaidResult { Success = true, Down = 0, Turns = 6 };
            foreach (var m in squad) { res.Hp.Add(m.Hp); res.Armor.Add(m.Armor); }
            r = Sim.ResolveRaid(res);
            if (!r.Ok) throw new InvalidOperationException("sonuç: " + r.Msg);
            Sim.DrainEvents();
            RaiseChanged();
            yield return FileOperationReport();
            yield return AtBoard();
            Capture("K4c");
            needReload = true;
        }

        static IEnumerator K4d()
        {
            yield return AtBoard(true);
            Board.Deck.Open(false);
            yield return Wait(0.6f);
            Capture("K4d");
            Board.Deck.Close(false);
        }

        static IEnumerator K4e()
        {
            yield return AtBoard();
            var list = new List<LostEntry>();
            for (int i = 0; i < 6; i++)
            {
                var p = Sim.Board.People[i % Sim.Board.People.Count];
                list.Add(new LostEntry { Id = p.Id, Name = p.Name, Line = Loc.F("Kayıp · V.{0:000}", 6 + i * 4) });
            }
            var env = Board.LostEnvelope;
            if (env == null) throw new InvalidOperationException("KAYIPLAR zarfı yok");
            env.SetCover(Loc.F("KAYIPLAR · {0}", list.Count));
            env.Open(list);
            yield return Wait(0.9f);
            Capture("K4e");
            env.Close();
            yield return Wait(0.4f);
        }

        static IEnumerator Order(string id, string name)
        {
            yield return AtBoard();
            Cur.Note(ProductionFixtures.Present(name));
            yield return Wait(0.5f);
            Capture(id);
            ProductionFixtures.Restore();
            yield return Wait(0.3f);
        }

        static HoverTag TagOf(object source)
        {
            var tag = Get<HoverTag>(source, "Tag") ?? Get<HoverTag>(source, "hoverTag");
            return tag != null ? tag : Any<HoverTag>();
        }

        static Vector2 BoardScreen(Vector2 mm)
        {
            var board = Board.transform;
            var world = board.TransformPoint(BoardLayout.MmToLocal(mm, BoardLayout.CardZ(BoardLayout.PocketLevel)));
            return Rig.View.WorldToScreenPoint(world);
        }

        /// <summary>K4m: sürükleme önizlemesi (geçerli hedef, kart elde). Olay zinciri BoardPlayKit.Drag ile aynı; önizleme etiketi çıkmazsa aynı metin doğrudan basılır.</summary>
        static IEnumerator K4m()
        {
            yield return AtBoard(true);
            var idle = Sim.Board.People.FirstOrDefault(p => p.Alive && p.Job == "rest");
            if (idle == null) throw new InvalidOperationException("Boşta kişi yok");
            string job = new[] { "craft", "cook", "tek", "emp", "ana" }.First(j => BoardPreviewText.For(BoardCardKind.Person, idle.Id, BoardTarget.Pocket(j), Sim, Presenter.Ui).Valid);
            var preview = BoardPreviewText.For(BoardCardKind.Person, idle.Id, BoardTarget.Pocket(job), Sim, Presenter.Ui);
            var card = Board.CardFor(idle.Id);
            if (card == null) throw new InvalidOperationException("kartı yok: " + idle.Id);
            Vector2 from = Rig.View.WorldToScreenPoint(card.transform.position), to = BoardScreen(BoardLayout.CardCentre(job));
            var go = card.gameObject;
            var ped = Ped(go, from);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.initializePotentialDrag);
            yield return Frames(1);
            const int Moves = 12;
            for (int i = 1; i <= Moves; i++)
            {
                var p = Vector2.Lerp(from, to, i / (float)Moves);
                ped.delta = p - ped.position;
                ped.position = p;
                if (i == 1) { ped.dragging = true; ped.pointerDrag = go; ped.eligibleForClick = false; ExecuteEvents.Execute(go, ped, ExecuteEvents.beginDragHandler); }
                ExecuteEvents.Execute(go, ped, ExecuteEvents.dragHandler);
                yield return Frames(1);
            }
            yield return Wait(0.5f);
            var drag = Board.Drag;
            var tag = Any<HoverTag>();
            if (tag.Owner == null) Call(drag, "ShowPreview", preview, to, true);
            yield return Frames(3);
            Capture("K4m");
            drag.Cancel();
            ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.endDragHandler);
            ExecuteEvents.Execute(go, ped, ExecuteEvents.pointerExitHandler);
            yield return Wait(0.6f);
        }

        /// <summary>K4n: ret nedeni. Geçersiz bir hedef için BoardPreviewText'in ürettiği metin, kontrolörün ret etiketiyle gösterilir (ShowReason).</summary>
        static IEnumerator K4n()
        {
            yield return AtBoard();
            // Sağlıklı boştaki kişinin her cebi geçerlidir. Gerçek ret: taslak iş yokken hammadde kartı iş emrine (BoardDropRules.ReserveCheck);
            // yedek: zaten uzman olunan eğitim konusu (TrainCheck)
            var tries = new List<(BoardCardKind kind, string id, BoardTarget target)>();
            foreach (var material in PlanningBoardData.MaterialKinds) tries.Add((BoardCardKind.Material, material, BoardTarget.Order()));
            foreach (var resident in Sim.Board.People.Where(p => p.Alive))
                for (int row = 0; row < PlanningBoardData.Subjects.Length; row++) tries.Add((BoardCardKind.Person, resident.Id, BoardTarget.Topic(row)));
            DropPreview? invalid = null;
            foreach (var (kind, id, target) in tries)
            {
                var p = BoardPreviewText.For(kind, id, target, Sim, Presenter.Ui);
                if (!p.Valid && !string.IsNullOrEmpty(p.Title) && !string.IsNullOrEmpty(p.Body)) { invalid = p; break; }
            }
            if (invalid == null) throw new InvalidOperationException("geçersiz bırakma hedefi bulunamadı");
            Call(Board.Drag, "ShowReason", invalid.Value, BoardScreen(BoardLayout.Order.center));
            yield return Frames(3);
            Capture("K4n");
            Board.Drag.Cancel();
            HideAllHover();
            yield return Wait(0.3f);
        }

        static IEnumerator F1()
        {
            yield return AtBoard();
            var presenter = Presenter;
            presenter.ShowPerson(Sim.Board.People[0].Id);   // yalnız seçer (IM:1307); dosyayı kapak tıkının yolu açar
            Call(presenter, "OpenFolder");
            var folder = Board.Folder;
            yield return WaitFor(() => folder.IsOpen && !folder.Moving, 4, "dosya açılmadı");
            yield return Wait(0.3f);
            Capture("F1");
            folder.Close();
            yield return WaitFor(() => !folder.Moving, 4, "dosya kapanmadı");
            presenter.Ui.SelectedId = null;
            presenter.Refresh();
        }

        static IEnumerator F2()
        {
            yield return AtBoard();
            var presenter = Presenter;
            var person = Sim.Board.People[0];
            presenter.ShowPerson(person.Id);   // yalnız seçer; 40 satırlı veriyle dosyayı aşağıda bu adım açar (MapPaperPlayTests deseni)
            var folder = Board.Folder;
            bool english = DeskLoc.Instance.Lang == DeskLang.En;
            var data = PlanningBoardSnapshotBuilder.Build(Sim, presenter.Ui, english, false, false).Folder;
            for (int i = 0; i < 40; i++) data.DossierRight.Add(new FolderLine("V.031", Loc.T("İZ 1 · erişim kaydı")));
            folder.Open(data);
            yield return WaitFor(() => folder.IsOpen && !folder.Moving, 4, "dosya okuma pozuna varmadı");
            var dossier = Get<PersonDossierFace>(folder, "dossier");
            if (dossier == null || dossier.PageCount < 2) throw new InvalidOperationException("ek yaprak oluşmadı");
            dossier.Turn(1);
            yield return Wait(0.3f);
            Capture("F2");
            dossier.Turn(-dossier.Page);
            folder.Close();
            yield return WaitFor(() => !folder.Moving, 4, "dosya kapanmadı");
            presenter.Ui.SelectedId = null;
            presenter.Refresh();
        }

        static IEnumerator F3()
        {
            yield return AtBoard();
            Cur.Note(AdmissionFixtures.Present("trace"));
            yield return WaitFor(() => !Rig.InTransition && Rig.Current == SiginakPose.Shelf, 4, "K4'e varılmadı");
            var folder = Board.Folder;
            yield return WaitFor(() => !folder.Moving, 4, "dosya okuma pozuna varmadı");
            yield return Wait(0.3f);
            Capture("F3");
            folder.Close();
            AdmissionFixtures.Restore();
            yield return Wait(0.4f);
        }

        static IEnumerator Book(string id, int spread)
        {
            yield return AtBoard();
            var book = Board.Book;
            if (book == null) throw new InvalidOperationException("tarif defteri yok");
            if (book.State != BookState.Open) book.Open();
            yield return WaitFor(() => book.State == BookState.Open && !book.Moving, 4, "defter açılmadı");
            book.SetSpread(spread);
            yield return WaitFor(() => !book.Moving, 3, "sayfa dönmedi");
            yield return Wait(0.3f);
            Capture(id);
            if (id == "BK3") { book.Close(false); yield return Wait(0.5f); }
        }

        // ---------------------------------------------------------------- K6, K9, son rapor

        static IEnumerator S1()
        {
            yield return Fresh();
            yield return ToPose(SiginakPose.ShiftClose);
            yield return Frames(3);
            Capture("S1");
        }

        static IEnumerator EnsureShiftPose()
        {
            if (needReload || Rig.Current != SiginakPose.ShiftClose)
            {
                yield return Fresh();
                yield return ToPose(SiginakPose.ShiftClose);
            }
        }

        static IEnumerator S2()
        {
            yield return EnsureShiftPose();
            var sw = ShiftLever;
            if (sw == null) throw new InvalidOperationException("şalter yok");
            sw.Press();
            yield return WaitFor(() => !sw.Moving, 3, "kapak açılmadı");
            yield return Wait(0.3f);
            Capture("S2");
        }

        static IEnumerator S3()
        {
            yield return EnsureShiftPose();
            var sw = ShiftLever;
            if (sw.Current == ShiftSwitch.State.Idle)
            {
                sw.Press();
                yield return WaitFor(() => !sw.Moving, 3, "kapak açılmadı");
            }
            sw.Press();
            var paper = Paper;
            yield return WaitFor(() => paper.State == ShiftReceiptPaper.Where.Tongue && !paper.FeedPending, 4, "fiş dile çıkmadı (" + paper.State + ")");
            yield return WaitFor(() => !Rig.InTransition && !sw.Moving, 3, "şalter durmadı");
            yield return Wait(0.3f);
            // Fiş dilde duruyor (okuma pozu S5-S7): yazıları kuşbakışı küçük kalır, ölçüme girmez (DEFECTS §5.12); görüntü bütün kalır
            Capture("S3", true, t => !t.transform.IsChildOf(paper.transform));
            needReload = true;
        }

        static IEnumerator S4()
        {
            yield return Fresh();
            // Şalter BoardPlanCheck'e kilitlenir (SiginakShiftClose); girdisi ayrılmamış iş emri planı bozar. InvalidatePlan yalnız baskın denetimini bozar (C2)
            var queued = Game.ExecuteBoardCommand(sim => sim.QueueProduction("emp_cell"));
            if (!queued.Ok) throw new InvalidOperationException("iş emri: " + queued.Msg);
            Close.UpdateLock();
            yield return ToPose(SiginakPose.ShiftClose);
            Close.UpdateLock();
            yield return Frames(3);
            Capture("S4");
        }

        static IEnumerator Slip(string id, string fixture)
        {
            yield return Fresh();
            yield return ToPose(SiginakPose.ShiftClose);
            var sw = ShiftLever;
            sw.Press();
            yield return WaitFor(() => !sw.Moving, 3, "kapak açılmadı");
            sw.Press();
            var paper = Paper;
            yield return WaitFor(() => paper.State == ShiftReceiptPaper.Where.Tongue && !paper.FeedPending, 4, "fiş dile çıkmadı");
            Click(paper.gameObject);
            yield return WaitFor(() => paper.State == ShiftReceiptPaper.Where.Hand, 3, "fiş ele gelmedi (" + paper.State + ")");
            yield return Wait(0.5f);
            Cur.Note(HandoverSlipFixtures.Present(fixture));
            yield return Frames(2);
            Capture(id);
            needReload = true;
        }

        /// <summary>EVRAK: dosyalanmış fişin salt okunur görünümü (arşive fikstür fişi eklenir; yalnız sunum).</summary>
        static IEnumerator S8()
        {
            yield return Fresh();
            var close = Close;
            var archive = Get<List<HandoverSlipFacts>>(close, "archive");
            if (archive == null) throw new InvalidOperationException("arşiv listesi yok");
            archive.Add(HandoverSlipFixtures.V001());
            if (!close.OpenTray()) throw new InvalidOperationException("OpenTray reddedildi");
            var paper = Paper;
            yield return WaitFor(() => paper.State == ShiftReceiptPaper.Where.Reading, 4, "fiş okumaya gelmedi (" + paper.State + ")");
            yield return Wait(0.5f);
            Capture("S8");
            needReload = true;
        }

        static IEnumerator K9a()
        {
            yield return Fresh();
            var cabinet = SettingsCabinet.Instance;
            cabinet.RequestOpen();
            yield return WaitFor(() => cabinet.Current == SettingsCabinet.State.Open, 5, "dolap açılmadı");
            yield return Wait(0.4f);
            Capture("K9a");
            cabinet.Close();
            yield return WaitFor(() => cabinet.Current == SettingsCabinet.State.Closed, 4, "dolap kapanmadı");
        }

        static IEnumerator K9b()
        {
            yield return Fresh(false);
            var cabinet = SettingsCabinet.Instance;
            cabinet.RequestOpen();
            yield return Wait(2.0f);
            Capture("K9b");
            needReload = true;
        }

        /// <summary>Son rapor (Fix; kartlı oyunun canlı son yolu UNVERIFIED): ayrı fikstürde sonuç işaretlenir, rapor o fikstürden kurulur.</summary>
        static IEnumerator FinalReport(string id, string ending)
        {
            yield return Fresh();
            var sim = FixtureAt(2, false);
            sim.S.Ending = ending;
            var data = FinalReportBuilder.Build(sim);
            if (data == null) throw new InvalidOperationException("rapor verisi yok");
            var view = FinalReportView.Instance != null ? FinalReportView.Instance : Any<FinalReportView>();
            if (view == null) throw new InvalidOperationException("FinalReportView yok");
            view.Bind(data);
            var chart = Get<OccupationChart>(view, "chart");
            if (chart == null) yield return Wait(5f);
            else yield return WaitFor(() => chart.Drawn >= 1f, 8, "grafik çizimi bitmedi");
            yield return Wait(0.8f);   // işaretler, satırlar ve göstergelerin yazımı grafiğin sonunda biter
            Capture(id);
            needReload = true;
        }

        // ---------------------------------------------------------------- K8 ailesi

        static IEnumerator D1() { yield return Fresh(); yield return ToPose(SiginakPose.Door); yield return Wait(0.3f); Capture("D1"); }

        /// <summary>Sürücü bileşimi (DEFECTS §5.9): vuruş durumu üstünde kanat zorla açılır, tezgâh inik kalır; oyuncu bunu görmez (D10: kanattan önce tezgâh katlanır). Yalnız yazı ölçümü, açık kapının görsel kanıtı değil.</summary>
        static IEnumerator D2()
        {
            yield return ContinueFrom(FixtureAt(4));
            yield return ToPose(SiginakPose.Door);
            var door = Get<DoorProp>(AdmissionFlow, "door");
            if (door == null) throw new InvalidOperationException("DoorProp yok");
            door.SetOpen(true);
            yield return WaitFor(() => !door.Moving, 5, "kapı açılmadı");
            yield return Wait(0.3f);
            Cur.Note("D2 sürücü bileşimi: kanat zorla açık, tezgâh inik (oyuncu durumu değil)");
            Capture("D2");
            door.SetOpen(false);
            yield return WaitFor(() => !door.Moving, 5, "kapı kapanmadı");
        }

        /// <summary>Üzerine gelme kâğıdının yazıları (kâğıt bağı yoksa etiketin altı).</summary>
        static Func<TMPro.TMP_Text, bool> OnlyPaper(HoverTag tag)
        {
            var paper = Get<RectTransform>(tag, "paper");
            Transform root = paper != null ? paper : tag.transform;
            return t => t.transform.IsChildOf(root);
        }

        static IEnumerator EnsureRoom()
        {
            if (needReload || Game == null || Rig == null) yield return Fresh();
        }

        static IEnumerator Admission(string id, string fixture)
        {
            yield return EnsureRoom();
            Cur.Note(AdmissionFixtures.Present(fixture));
            if (fixture != "aid")
            {
                var station = Get<AdmissionStation>(AdmissionFlow, "station");
                yield return Wait(0.5f);
                yield return WaitOrNote(() => station == null || !station.Moving, 3, "istasyon durmadı (fişler)");
            }
            Capture(id);
            AdmissionFixtures.Restore();
            yield return Wait(0.2f);
        }

        static IEnumerator A7()
        {
            yield return EnsureRoom();
            Cur.Note(AdmissionFixtures.Present("inspect"));
            yield return Wait(0.5f);
            var stamps = Get<StampDecision[]>(AdmissionFlow, "stamps") ?? new StampDecision[0];
            if (stamps.Length == 0) throw new InvalidOperationException("damga yok");
            foreach (var stamp in stamps.Where(s => s != null))
            {
                var tag = TagOf(stamp);
                var screen = CurrentCamera().WorldToScreenPoint(stamp.transform.position);
                tag.ShowAt(stamp, screen, true);
                yield return Frames(2);
                Capture("A7_" + stamp.Choice, true, OnlyPaper(tag));
                tag.Hide(stamp);
            }
            // Raf plakaları ve sap yazıları EN'de ayrıca ölçülür (A7 satırının bütünü)
            Capture("A7_rack", false);
            AdmissionFixtures.Restore();
        }

        static IEnumerator G1()
        {
            yield return ContinueFrom(FixtureAt(4));
            yield return ToPose(SiginakPose.Door);
            var admission = AdmissionFlow;
            admission.ClickCounter();
            yield return WaitFor(() => !admission.Busy && !Rig.InTransition, 5, "tezgâha gelinmedi");
            if (!admission.UseTool(AdmissionTool.Breath)) throw new InvalidOperationException("buğu camı reddedildi");
            yield return WaitFor(() => Rig.Current == SiginakPose.BreathGlass && !Rig.InTransition, 4, "K8G'ye varılmadı");
            yield return Wait(0.95f);   // kapak 0,2 + buğu 0,6 s (ColdGlassView); buğu 2,5 s'de solmaya başlar
            Capture("G1");
            needReload = true;
        }

        static IEnumerator ReadPaper(string id, string fixture, AdmissionPaper kind, int index)
        {
            yield return EnsureRoom();
            Cur.Note(AdmissionFixtures.Present(fixture));
            var admission = AdmissionFlow;
            var station = Get<AdmissionStation>(admission, "station");
            yield return Wait(0.5f);
            yield return WaitOrNote(() => station == null || !station.Moving, 3, "istasyon durmadı (fişler)");
            if (kind == AdmissionPaper.Request)
            {
                // Yaprak yalnız GİDEN'e varınca okunur (RequestPadTool.OutgoingPaper); önceki bağlama varsa gönderim canlı oynar (TearS + FlyS)
                var pad = Get<RequestPadTool>(admission, "pad");
                yield return WaitOrNote(() => pad != null && pad.State == RequestPadTool.PadState.Outgoing && !pad.Moving, 3, "talep yaprağı GİDEN'e varmadı");
                if (pad == null || pad.OutgoingPaper == null) { kind = AdmissionPaper.Carbon; Cur.Note(id + ": talep yaprağı yok, karbon okundu"); }
            }
            var anchor = Get<Transform>(admission, "readAnchor");
            var theme = SiginakTheme.Active;
            float distance = theme != null ? theme.NumberOf("admission.readDistanceM", 0.318f) : 0.318f;
            var pose = station.ReadPose(kind, index, distance);
            anchor.SetPositionAndRotation(pose.position, pose.rotation);
            Rig.Snap(SiginakPose.AdmissionRead);
            yield return Wait(0.4f);
            Capture(id);
            AdmissionFixtures.Restore();
            yield return Wait(0.2f);
        }

        // ---------------------------------------------------------------- üzerine gelme süpürmesi (H1)

        static IEnumerator H1()
        {
            yield return ContinueFrom(FixtureAt(4));
            foreach (SiginakPose pose in Enum.GetValues(typeof(SiginakPose)))
            {
                if (!Rig.TryGetPose(pose, out _)) continue;
                try { Rig.Snap(pose); }
                catch (Exception e) { Cur.Note("poz " + pose + ": " + e.Message); continue; }
                yield return Wait(0.4f);
                yield return Sweep(pose.ToString());
            }
            if (longestSource != null && longestTag != null)
            {
                Rig.Snap(longestPose);
                yield return Wait(0.4f);
                var src = (IHoverSource)longestSource;
                longestTag.ShowAt(src, longestScreen, true);
                yield return Frames(3);
                Capture("H1_longest", true, OnlyPaper(longestTag));
                longestTag.Hide(src);
                Cur.Note("en uzun: " + longestPose + " · " + longestSource.name + " · " + longestLen + " harf");
            }
        }

        static MonoBehaviour longestSource;
        static HoverTag longestTag;
        static SiginakPose longestPose;
        static Vector2 longestScreen;
        static int longestLen;

        /// <summary>Bir pozda görünen her IHoverSource için etiketi anında açar ve yalnız etiketin yazılarını ölçer (H1). Tek hedefin hatası süpürmeyi durdurmaz.</summary>
        static IEnumerator Sweep(string pose)
        {
            var cam = CurrentCamera();
            // Dilden bağımsız sıra ve etiket: tam yola göre sıralanır, aynı yolda tekrar eden nesne ~2 ile ayrılır (TR ve EN satırları birleşebilsin; DEFECTS §5.13)
            var sources = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(m => m is IHoverSource)
                .OrderBy(m => PathOf(m.transform), StringComparer.Ordinal).ThenBy(m => m.GetType().FullName, StringComparer.Ordinal).ToList();
            var usedLabels = new Dictionary<string, int>();
            int done = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                var behaviour = sources[i];
                if (behaviour == null) continue;
                var source = (IHoverSource)behaviour;
                string title = null, line = null;
                Vector3 screen = Vector3.zero;
                HoverTag tag = null;
                bool ok = false;
                try
                {
                    title = source.HoverTitle;
                    line = source.Line;
                    if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(line)) continue;
                    var root = behaviour.GetComponentInParent<Canvas>()?.rootCanvas;
                    screen = root != null && root.renderMode == RenderMode.ScreenSpaceOverlay
                        ? (Vector3)RectTransformUtility.WorldToScreenPoint(null, behaviour.transform.position)
                        : cam.WorldToScreenPoint(behaviour.transform.position);
                    bool visible = screen.z >= 0 && screen.x >= 0 && screen.y >= 0 && screen.x <= Screen.width && screen.y <= Screen.height;
                    if (!visible) continue;
                    tag = TagOf(behaviour);
                    if (tag != null) { tag.ShowAt(source, screen, true); ok = true; }
                }
                catch (Exception e) { Cur.Note("hover " + behaviour.name + ": " + e.Message); }
                if (!ok) continue;
                yield return Frames(2);
                try
                {
                    string path = PathOf(behaviour.transform);
                    usedLabels.TryGetValue(path, out int seen);
                    usedLabels[path] = seen + 1;
                    string label = "H1/" + pose + "/" + path + (seen > 0 ? "~" + (seen + 1) : "");
                    SiginakTextScan.ScanLiveAll(cam, label, OnlyPaper(tag));
                    int len = (title ?? "").Length + (line ?? "").Length;
                    if (len > longestLen)
                    {
                        longestLen = len; longestSource = behaviour; longestTag = tag; longestScreen = screen;
                        if (Rig != null) longestPose = Rig.Current;
                    }
                    done++;
                }
                catch (Exception e) { Cur.Note("hover ölçüm " + behaviour.name + ": " + e.Message); }
                finally { tag.Hide(source); }
            }
            Cur.Note("H1 " + pose + ": " + done + " hedef");
        }

        // ---------------------------------------------------------------- baskın

        static IEnumerator OpenRaid()
        {
            yield return Fresh();
            var next = Sim.NextLiberationSector(Sim.S.Liberation.CurrentRegion);
            var r = Game.StartLiberationRaid(next.Id);
            if (!r.Ok) throw new InvalidOperationException("baskın emri: " + r.Msg);
            yield return WaitFor(() => Src != null && Src.State != null, 12, "baskın sahnesi yüklenmedi");
            yield return Wait(1.2f);
        }

        static IEnumerator EnsureRaid()
        {
            if (needReload || !RaidOpen || Src?.State == null) yield return OpenRaid();
        }

        static IEnumerator Z1()
        {
            yield return OpenRaid();
            Src.Select('T');
            yield return Wait(0.6f);
            Capture("Z1");
        }

        /// <summary>
        /// K5a'da baskın (DEFECTS §5.2): oyuncunun gördüğü kare dalışın sonudur (HAT AÇILIYOR, <c>CrtState.Establishing</c>); baskın sahnesi açıkken oda kökü gizlidir.
        /// Simülasyon baskını kurar, sahne yüklenmez (K4c deseni). <c>C3_canli</c>: Live durumunun yazıları aynı ekrana bağlanır (yalnız ekran yazıları ölçülür; sunum, kayda girmez).
        /// </summary>
        static IEnumerator C3()
        {
            yield return Fresh();
            var next = Sim.NextLiberationSector(Sim.S.Liberation.CurrentRegion);
            var r = Sim.StartLiberationRaid(next.Id);
            if (!r.Ok) throw new InvalidOperationException("baskın: " + r.Msg);
            Sim.DrainEvents();
            yield return ToPose(SiginakPose.Crt);
            yield return Wait(0.6f);
            Capture("C3");
            var screen = Get<CrtScreen>(Any<SiginakCrt>(), "screen");
            if (screen == null) throw new InvalidOperationException("CrtScreen yok");
            screen.Bind(CrtViewBuilder.Screen(Sim, true));
            yield return Frames(3);
            Capture("C3_canli", true, t => t.transform.IsChildOf(screen.transform));
            needReload = true;
        }

        static IEnumerator RaidPanel(string id, string panel)
        {
            yield return EnsureRaid();
            Src.Panel(panel);
            yield return Wait(0.6f);
            Capture(id);
            Src.Panel(panel);
            yield return Wait(0.3f);
        }

        static IEnumerator Z7()
        {
            yield return EnsureRaid();
            Src.Retreat();
            yield return Wait(0.4f);
            Capture("Z7");
            Src.Cancel();
            yield return Wait(0.3f);
        }

        static IEnumerator Z9()
        {
            yield return EnsureRaid();
            var src = Src;
            var enemy = src.State.Units.FirstOrDefault(u => u.Side != 'p' && Baskin.OnBoard(u));
            var hover = enemy != null ? new Vector2Int(enemy.X, enemy.Y) : Vector2Int.zero;
            src.Hover(hover);
            yield return Wait(0.5f);
            Capture("Z9");
            src.ClearHover();
            yield return Wait(0.2f);
        }

        static IEnumerator Z2()
        {
            yield return EnsureRaid();
            var src = Src;
            src.EndTurn();
            yield return Wait(0.3f);
            src.EndTurn();
            yield return Wait(1.1f);
            Capture("Z2");
            yield return WaitFor(() => !src.Busy && !src.Executing, 25, "makine evresi bitmedi");
            yield return Wait(0.4f);
        }

        /// <summary>Olay (A9 tohum 731 deseni; olay kimliği Baskin.RegionEvents'in ilki).</summary>
        static IEnumerator Z3()
        {
            yield return EnsureRaid();
            var p = new RaidParams
            {
                Seed = 731, Region = "eeu", RegionName = GameData.Region("eeu").Name, MissionKind = "loot", Tier = 2, Compute = 3, RaidNumber = 2,
                Traits = new List<string> { "dar" }, Signature = "kilit", Event = Baskin.RegionEvents.Keys.First(),
                MapSeed = Rev3CaptureSet.Seed,   // 0'da bina kiti varsayılana düşer (RaidKitText.KitFor); Z4 ve Z8 bu tahtayı sürdürür
            };
            var src = Src;
            src.OpenForValidation(Baskin.Create(p));
            yield return Wait(0.8f);
            src.Select('T');
            yield return Wait(0.5f);
            Capture("Z3");
        }

        static IEnumerator Z4()
        {
            yield return EnsureRaid();
            var src = Src;
            src.State.Turn = src.State.MaxTurns;
            src.Select('T');
            yield return Wait(0.3f);
            src.Arm();
            yield return Wait(0.6f);
            Capture("Z4");
        }

        static IEnumerator Z8()
        {
            yield return EnsureRaid();
            var src = Src;
            var b = src.State.Clone();
            b.Escape = true;
            b.Turn = b.MaxTurns;
            foreach (var u in b.Units.Where(u => u.Side == 'p')) { u.Moved = true; u.Acted = true; }
            src.OpenForValidation(b);
            yield return Frames(2);
            src.EndTurn();
            yield return Wait(0.5f);
            Capture("Z8");
            src.Cancel();
            yield return Wait(0.3f);
        }

        /// <summary>Tahtayı kazanılmış hâle getirir (yalnız görüntü; SiginakCaptureSet.Win ile aynı): görünüm durumu dil değişimiyle yeniden kurulur.</summary>
        static void WinBoard(RaidDataSource src)
        {
            var st = src.State;
            int cx = -1, cy = -1;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if (st.Cells[y, x].O != null && Baskin.IsTarget(st.Cells[y, x].O.K)) { cx = x; cy = y; }
            if (cx >= 0)
            {
                var tek = st.Units.First(u => u.Type == "tek");
                tek.X = cx > 0 ? cx - 1 : cx + 1;
                tek.Y = cy;
                st.Cells[cy, cx].O = null;
            }
            st.Phase = "won";
            st.Turn = 6;
            var l = DeskLoc.Instance;
            var cur = l.Lang;
            l.SetLanguage(cur == DeskLang.En ? DeskLang.Tr : DeskLang.En);
            l.SetLanguage(cur);
        }

        static IEnumerator ReturnFromRaid(RaidDataSource src)
        {
            src.Continue();
            yield return WaitFor(() => !RaidOpen, 10, "baskın kapanmadı");
            yield return Wait(2.6f);
        }

        static IEnumerator Z10()
        {
            yield return OpenRaid();
            var src = Src;
            WinBoard(src);
            yield return Wait(1.3f);
            Capture("Z10");
            yield return ReturnFromRaid(src);
        }

        static IEnumerator O1()
        {
            var op = Any<SiginakOperation>();
            if (op == null) throw new InvalidOperationException("SiginakOperation yok");
            yield return WaitFor(() => op.ReportOpen, 6, "operasyon raporu açılmadı");
            yield return Wait(0.5f);
            Capture("O1");
            needReload = true;
        }

        static IEnumerator Z11()
        {
            yield return OpenRaid();
            var src = Src;
            src.EndTurn();
            yield return Wait(0.3f);
            src.EndTurn();
            yield return Wait(0.5f);
            yield return WaitFor(() => !src.Busy && !src.Executing, 25, "makine evresi bitmedi");
            src.Retreat();
            yield return Wait(0.2f);
            src.Retreat();
            yield return Wait(1.3f);
            Capture("Z11");
            yield return ReturnFromRaid(src);
        }

        static IEnumerator O2()
        {
            var op = Any<SiginakOperation>();
            if (op == null) throw new InvalidOperationException("SiginakOperation yok");
            yield return WaitFor(() => op.ReportOpen, 6, "operasyon raporu açılmadı");
            yield return Wait(0.5f);
            Capture("O2");
            needReload = true;
        }

        // ---------------------------------------------------------------- K4b: dolu pano (10 kişi + 32 eşya + 4 fiş; Aşama 3'ün sunum fikstürü)

        static void RenderMax()
        {
            var src = Board.Last;
            if (src == null) throw new InvalidOperationException("pano anlık görüntüsü yok");
            var s = new BoardSnapshot
            {
                English = src.English, Header = src.Header, Locked = false, LockReason = "", Editable = true, EntryLine = src.EntryLine, HurdaLine = src.HurdaLine,
                HurdaPlateLine = src.HurdaPlateLine, Topics = src.Topics, Folder = src.Folder, FolderCoverLine = src.FolderCoverLine, LostCover = src.LostCover,
                SelectedId = src.SelectedId, Words = src.Words,
            };
            foreach (var p in src.People) s.People.Add(p);
            foreach (var kv in src.PocketOccupant) s.PocketOccupant[kv.Key] = kv.Value;
            foreach (var id in src.DeckOrder) s.DeckOrder.Add(id);
            var template = src.People.First();
            string[] names = { "Rauf", "Melek", "Tarık", "Ebru" };
            for (int i = 0, extra = 10 - s.People.Count; i < extra; i++)
            {
                var p = new PersonCardData
                {
                    Id = "fx" + i, Name = names[i % names.Length], Job = template.Job, PortraitKey = src.People[(i + 3) % src.People.Count].PortraitKey, JobId = "rest",
                    Strip = template.Strip, StripTone = template.StripTone, HoverTitle = names[i % names.Length], HoverLine = template.HoverLine,
                };
                Array.Copy(template.MasteryLetters, p.MasteryLetters, 7);
                Array.Copy(template.MasteryTiers, p.MasteryTiers, 7);
                Array.Copy(template.StatLabels, p.StatLabels, 3);
                Array.Copy(template.StatValues, p.StatValues, 3);
                s.People.Add(p);
                s.DeckOrder.Add(p.Id);
            }
            var pocketed = s.PocketOccupant.Values.ToList();
            if (pocketed.Count >= 4)
            {
                s.Person(pocketed[0]).Injured = true; s.Person(pocketed[0]).Hurt = true;
                s.Person(pocketed[1]).Review = true;
                s.Person(pocketed[2]).Restricted = true; s.Person(pocketed[2]).StripTone = StripTone.Red;
                s.Person(pocketed[3]).Bedridden = true; s.Person(pocketed[3]).StripTone = StripTone.Blue;
            }
            var proto = src.Items.GroupBy(i => i.Kind).ToDictionary(g => g.Key, g => g.First());
            string[] kinds = { "kit_tek", "armor", "medkit" };
            ItemSlotKind[] slots = { ItemSlotKind.Kit, ItemSlotKind.Armor, ItemSlotKind.Aid };
            int n = 0;
            foreach (var p in s.People)
                for (int k = 0; k < 3 && n < 30; k++, n++)
                {
                    string kind = kinds[k];
                    s.Items.Add(new ItemCardData
                    {
                        Id = "fxi" + n, Kind = kind, Name = PlanningBoardData.EquipmentName(kind),
                        SlotLabel = Loc.T(slots[k] == ItemSlotKind.Kit ? "KİT" : slots[k] == ItemSlotKind.Armor ? "ZIRH" : "YARDIMCI"), Slot = slots[k], OwnerId = p.Id,
                        UseLine = proto.TryGetValue(kind, out var pr) ? pr.UseLine : "", HoverTitle = PlanningBoardData.EquipmentName(kind), HoverLine = "",
                    });
                }
            for (int k = 0; k < 6; k++)
            {
                var kit = new KitStackData { Kind = BoardLayout.KitKinds[k] };
                if (k < 2)
                {
                    string id = "fxs" + k, kind = BoardLayout.KitKinds[k];
                    s.Items.Add(new ItemCardData { Id = id, Kind = kind, Name = PlanningBoardData.EquipmentName(kind), SlotLabel = Loc.T("KİT"), Slot = ItemSlotKind.Kit, UseLine = "", HoverTitle = PlanningBoardData.EquipmentName(kind), HoverLine = "" });
                    kit.Count = 1; kit.TopIds.Add(id); kit.CountText = "×1";
                }
                else { kit.Count = 0; kit.CountText = "×0"; }
                s.Kits[k] = kit;
            }
            for (int m = 0; m < 4; m++) s.Materials[m] = src.Materials[m];
            for (int i = 0; i < 4; i++)
            {
                string kind = BoardLayout.MaterialKinds[i];
                s.Chips.Add(new ChipData { Id = "chip:" + kind + ":0", Kind = kind, Name = src.Materials[i].Name, Index = 0 });
            }
            Board.Render(s, false);
        }
    }
}
