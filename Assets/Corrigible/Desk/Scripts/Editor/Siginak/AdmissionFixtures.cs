using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Kapı ve kabulün (R9/R17) sunum fikstürleri (Revizyon 3 · Aşama 6, PLAN §3 W7, §5.4, E §5.3; <see cref="ProductionFixtures"/> kalıbı).
    /// Core'a dokunmaz, komut yazmaz: istasyonun anlık görüntüsü ayrı bir fikstür simülasyonundan (tohum 79, oyunun denge asset'i, panonun
    /// açılış komutları; gerçek Core komutlarıyla V.005 / V.009) kurulur, tasarımın adları ve uç durumları (Rauf'un N-17/204 ≠ N-17/240
    /// belgesi, damgalı belge, tenekeler, talimat 3, kişi dosyasındaki iz) kopyası üzerinde uygulanır ve görünümlere bağlanır.
    /// <list type="bullet">
    /// <item><see cref="Present"/> (Play): adı verilen fikstürü istasyona çizer (<c>AdmissionStation.Bind(s, false)</c>, araçlar ve damgalar
    /// da) ve kamerayı fikstürün pozuna oturtur. Bir sonraki <c>game.Changed</c>, dil değişimi ya da <see cref="Restore"/> gerçek görüntüye
    /// döner. Görüntü aynı çağrıda <see cref="PresentAndCapture"/> ile alınır (tek kare, <c>CorrigibleDebug.Capture</c>).</item>
    /// <item><see cref="RenderPaperFlat"/>: belge, şerit (v2, v3) ve bulgu fişinin düz, dik çizimi (tasarım PNG'lerinin @2x ölçeği, 8 px/mm) ve
    /// referansla yan yana kopyası (<c>A6_R17_kagit_{belge,serit_v2,serit_v3,fis}_{tr,en}_yan</c>). Önizleme sahnesinde kurar.</item>
    /// <item><see cref="ScanCases"/>: punto taramasının (<see cref="SiginakTextScan.ScanAdmissionPapers"/>) kâğıt verileri.</item>
    /// </list>
    /// Görüntüler kayıt yalıtımıyla alınmalı (SessionState <c>Corrigible.TestStorage</c>, GELISTIRME): akış görülen sürümü yan dosyaya yazar.
    /// </summary>
    public static class AdmissionFixtures
    {
        /// <summary>Kâğıt prefab'larının klasörü (W3 tek seferlik kurucusu).</summary>
        public const string PrefabDir = DeskSpriteManifest.Root + "/Prefabs/Rev3Admission";
        public const string DocumentPrefab = PrefabDir + "/AdmissionDocument.prefab";
        public const string StripPrefab = PrefabDir + "/OrdersStrip.prefab";
        public const string SlipPrefab = PrefabDir + "/FindingSlip.prefab";
        public const string RequestPrefab = PrefabDir + "/RequestForm.prefab";
        public const string ReferenceDir = ProductionFixtures.ReferenceDir;
        public const string OutDir = "Temp";

        /// <summary>
        /// Fikstür adları: <c>present</c> (V.005 aday, belge çekmecede; R17_present), <c>inspect</c> (Rauf, N-17/204 ≠ N-17/240, UYUŞMUYOR,
        /// işaretçi N-17 maddesinde; R17_inspect), <c>pending</c> (dış kayıt bekliyor, yaprak GİDEN'de; R17_pending), <c>stamped</c> (KABUL izi;
        /// R17_stamped), <c>aid</c> (YARDIM: çekmece açık, tenekeler; R17_aid), <c>v3</c> (talimat 3 yuvada, ESKİ cebinde v2 ve v1, İNCELEME
        /// FİŞİ; R17_v3), <c>trace</c> (kişi dosyası: İZ 1/İZ 2, İNCELEME etiketi; R17_trace, K4), <c>read</c> (inspect + K8R okuma pozu;
        /// R17_read), <c>inspect_en</c> (inspect İngilizce; R17_inspect_en).
        /// </summary>
        public static readonly string[] Names = { "present", "inspect", "pending", "stamped", "aid", "v3", "trace", "read", "inspect_en" };

        /// <summary>Tasarımın Rauf belgesi (KK <c>kimlik_sevk_rauf_*</c>): ad, seri ve sevk numaraları, mühür, düzenleme vardiyası.</summary>
        const string RaufName = "RAUF", RaufSerial = "N-17/204", RaufReferral = "N-17/240", RaufSeal = "KYGA-G";
        const int RaufShift = 12;
        /// <summary>Tasarımın Aylin'i (KK <c>kimlik_sevk_aylin_*</c>, <c>bulgu_*_aylin_*</c>).</summary>
        const string AylinName = "AYLİN";

        // ================================================================== veri (Core komutları; ayrı simülasyon)

        /// <summary>
        /// Oyunun denge asset'iyle tohum <paramref name="seed"/> ve panonun açılış komutları (<c>board_auto</c>, emekli görevler BOŞTA'ya;
        /// <c>PlanningBoardPresenter.RetireJobs</c> ile aynı): Boşta Cem, İpek, Zeynep (K4-A gözetmeni Cem).
        /// </summary>
        public static Simulation NewSimulation(uint seed = 79)
        {
            var sim = ProductionFixtures.NewSimulation(seed);
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                sim.AssignResident(p.Id, "rest");
            return sim;
        }

        /// <summary>Fikstürü <paramref name="day"/>. vardiyaya gerçek kapanışlarla (<c>PrintShift</c> + <c>FileShift</c>) getirir.</summary>
        public static void CloseTo(Simulation sim, int day)
        {
            for (int guard = 0; sim.S.Day < day && guard < 64; guard++)
            {
                if (!sim.PrintShift().Ok) break;
                if (!sim.FileShift(sim.Board.ReceiptId).Ok) break;
            }
        }

        /// <summary>Tohum 79 V.005: kapıda Suna 9; <paramref name="inspections"/> sırayla yapılır (Core komutları).</summary>
        public static Simulation FirstCandidate(params string[] inspections)
        {
            var sim = NewSimulation();
            CloseTo(sim, 4);
            foreach (var kind in inspections) sim.InspectApplicant(kind);
            return sim;
        }

        /// <summary>Tohum 79 V.009: talimat 3 (Suna 9 V.005'te soğuk gözlemle kabul edildi: İNCELEME FİŞİ), kapıda Eren.</summary>
        public static Simulation ThirdOrders()
        {
            var sim = FirstCandidate("breath");
            sim.DecideApplicant("admit");
            CloseTo(sim, 8);
            return sim;
        }

        static string Shift(int oneBased) => Loc.F("V.{0:000}", oneBased);

        static AdmissionSnapshot Build(Simulation sim, bool documentOnCounter)
        {
            var ui = new AdmissionUi();
            var id = sim.Admission?.Candidate?.Resident?.Id;
            if (documentOnCounter) ui.DocumentOnCounterFor = id;
            return AdmissionSnapshotBuilder.Build(sim, ui);
        }

        /// <summary>
        /// Adı verilen fikstürün anlık görüntüsü o anki dilde (null: bilinmeyen ad ya da istasyonsuz fikstür, <c>trace</c>). Tasarımın adları
        /// kopya üzerinde: belge adı Aylin / Rauf; Rauf'un numaraları ve mühürü KK'den, fiş gövdesi aynı Loc biçimiyle.
        /// </summary>
        public static AdmissionSnapshot Snapshot(string name)
        {
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "present":
                {
                    var s = Build(FirstCandidate(), false);
                    if (s.Document != null) s.Document.Name = AylinName;
                    return s;
                }
                case "inspect":
                case "inspect_en":
                case "read":
                    return Inspect();
                case "pending":
                {
                    var s = Build(FirstCandidate("papers", "record"), true);
                    if (s.Document != null) s.Document.Name = AylinName;
                    return s;
                }
                case "stamped":
                case "aid":
                {
                    var sim = FirstCandidate();
                    var s = Build(sim, true);
                    if (s.Document != null)
                    {
                        s.Document.Name = AylinName;
                        if (name.Trim().ToLowerInvariant() == "stamped") AdmissionSnapshotBuilder.Stamp(s.Document, StampChoice.Admit, sim.B);
                    }
                    return s;
                }
                case "v3":
                    return Build(ThirdOrders(), true);
            }
            return null;
        }

        /// <summary>R17_inspect: Rauf'un belgesi (belge No. ≠ sevk No.), karşılaştırma fişi UYUŞMUYOR + N-17 notu, işaretçi N-17 maddesinde.</summary>
        static AdmissionSnapshot Inspect()
        {
            var sim = FirstCandidate("papers");
            var s = Build(sim, true);
            var d = s.Document;
            if (d == null) return s;
            d.Name = RaufName;
            d.Serial = RaufSerial;
            d.Referral = RaufReferral;
            d.Seal = RaufSeal;
            d.Issued = Shift(RaufShift);
            int version = sim.Admission.Protocol.Version;
            var slip = s.Slips.LastOrDefault(x => x != null && x.Tool == AdmissionTool.Papers);
            if (slip != null)
            {
                slip.Shift = Shift(RaufShift);
                slip.Body = Loc.F("Belge No. {0} · sevk No. {1}. Mühür {2}, düzenleme V.{3:000}.", RaufSerial, RaufReferral, RaufSeal, RaufShift);
                slip.Kind = SlipVerdict.Mismatch;
                slip.Verdict = Loc.T("UYUŞMUYOR");
                int rule = AdmissionRuleMap.Record(version);
                slip.Note = rule > 0 ? Loc.F("N-17: dış kayıt gerekir (madde {0})", rule) : null;
            }
            int marker = AdmissionRuleMap.Papers(version, RaufSerial);
            s.MarkerRule = marker > 0 ? marker - 1 : -1;
            return s;
        }

        /// <summary>
        /// Punto taramasının kâğıt verileri (o anki dilde): V.005 aday (belge, şerit v2, ESKİ v1), V.005 dört incelemeden sonra (dört fiş,
        /// gönderilmiş talep ve karbon), V.009 (şerit v3, ESKİ v2 + v1, İNCELEME FİŞİ, Eren'in belgesi) ve Rauf (uyuşmazlık fişi).
        /// </summary>
        public static List<(string name, AdmissionSnapshot snapshot)> ScanCases() => new List<(string, AdmissionSnapshot)>
        {
            ("V005", Build(FirstCandidate(), false)),
            ("V005_incelendi", Build(FirstCandidate("papers", "interview", "breath", "record"), true)),
            ("V009", Build(ThirdOrders(), true)),
            ("Rauf", Inspect()),
        };

        // ================================================================== Play: istasyona sunum

        static T Ref<T>(Object owner, string field) where T : Object
        {
            if (owner == null) return null;
            var p = new SerializedObject(owner).FindProperty(field);
            return p != null && p.propertyType == SerializedPropertyType.ObjectReference ? p.objectReferenceValue as T : null;
        }

        static List<StampDecision> Stamps(SiginakAdmission admission)
        {
            var list = new List<StampDecision>();
            var p = new SerializedObject(admission).FindProperty("stamps");
            if (p == null || !p.isArray) return list;
            for (int i = 0; i < p.arraySize; i++)
                if (p.GetArrayElementAtIndex(i).objectReferenceValue is StampDecision s && s != null) list.Add(s);
            return list;
        }

        /// <summary>
        /// Play içinde (yalnız sunum): <paramref name="name"/> fikstürünü istasyona, araçlara ve damgalara bağlar ve kamerayı fikstürün pozuna
        /// oturtur (<c>read</c> K8R belgeye, <c>trace</c> K4 ve dosya açık, öteki K8A). <c>inspect_en</c> masa dilini İngilizceye çevirir. Komut
        /// yazılmaz, simülasyon değişmez. Bir sonraki <c>Changed</c> gerçek görüntüye döner.
        /// </summary>
        public static string Present(string name)
        {
            if (!Application.isPlaying) return "Play gerekli";
            string key = (name ?? "").Trim().ToLowerInvariant();
            if (!Names.Contains(key)) return "Bilinmeyen fikstür: " + name + " (" + string.Join(", ", Names) + ")";
            if (key == "trace") return PresentTrace();
            var admission = Object.FindFirstObjectByType<SiginakAdmission>(FindObjectsInactive.Include);
            if (admission == null) return "Sahnede SiginakAdmission yok (W6)";
            var station = Ref<AdmissionStation>(admission, "station");
            var rig = Ref<SiginakCameraRig>(admission, "rig");
            if (station == null || rig == null) return "SiginakAdmission bağları eksik (station, rig; W6)";
            if (key == "inspect_en" && DeskLoc.Instance.Lang != DeskLang.En) DeskLoc.Instance.SetLanguage(DeskLang.En);

            var s = Snapshot(key);
            if (s == null) return "Fikstür kurulamadı: " + key;
            // Önce poz (varış işleyicisi gerçek görüntüyü yeniden çizebilir), sonra fikstür
            rig.Snap(SiginakPose.Admission);
            station.Bind(s, false);
            var magnifier = Ref<MagnifierTool>(admission, "magnifier");
            var pad = Ref<RequestPadTool>(admission, "pad");
            var glass = Ref<ColdGlassView>(admission, "glass");
            var intercom = Ref<IntercomTool>(admission, "intercom");
            if (magnifier != null)
            {
                magnifier.Bind(s.Tools[(int)AdmissionTool.Papers]);
                // Büyüteç kuralı (IM Aşama 6, R17_pending/stamped): cetvel yalnız karşılaştırma fikstürlerinde belgede; öbürlerinde dinlenmede, kıskacı örtmez
                if (s.Tools[(int)AdmissionTool.Papers] != null && s.Tools[(int)AdmissionTool.Papers].Used && (key.StartsWith("inspect") || key == "read")) magnifier.SnapInspect("serial");
                else magnifier.ReturnToRest(false);
            }
            if (glass != null) { glass.Bind(s.Tools[(int)AdmissionTool.Breath]); glass.ResetClosed(); }
            if (intercom != null) { intercom.Bind(s.Tools[(int)AdmissionTool.Interview]); intercom.Stop(); }
            if (pad != null)
            {
                pad.Bind(s.Tools[(int)AdmissionTool.Record]);
                if (s.Request != null) pad.Bind(s.Request, s.Request.Sent && s.Slips.Any(x => x != null && x.Tool == AdmissionTool.Record && x.Kind == SlipVerdict.Pending));
            }
            foreach (var st in Stamps(admission))
            {
                var d = s.Stamps[(int)st.Choice];
                if (d != null) st.Bind(d);
                st.ReturnToRack(false);
            }
            if (key == "aid")
            {
                // R17_aid: çekmece dışarıdayken (iki teneke görünür); dizi Update'te sürer, görüntü aynı çağrıda alınmalı
                station.PlayDecision(StampChoice.Aid, null);
                station.Advance(AdmissionStation.DrawerS + 0.05f);
            }
            if (key == "read")
            {
                var anchor = Ref<Transform>(admission, "readAnchor");
                if (anchor == null) return "SiginakAdmission.readAnchor yok (Poses/K8R; W6)";
                var theme = SiginakTheme.Active;
                float distance = theme != null ? theme.NumberOf("admission.readDistanceM", 0.318f) : 0.318f;
                var pose = station.ReadPose(AdmissionPaper.Document, 0, distance);
                anchor.SetPositionAndRotation(pose.position, pose.rotation);
                rig.Snap(SiginakPose.AdmissionRead);
            }
            var d0 = s.Document;
            return $"{key}: aday {s.CandidateId ?? "yok"} · belge {(d0 != null ? d0.Name + " " + d0.Serial + "/" + d0.Referral : "yok")} · şerit v{s.Orders?.Version} · " +
                   $"{s.Slips.Count} fiş · işaretçi {s.MarkerRule} · poz {rig.Current} · dil {Loc.Lang}";
        }

        /// <summary>R17_trace: K4'te kişi dosyası açık; içerik bir sakinin gerçek dosyasından, tasarımın adı ve iz satırları kopyada.</summary>
        static string PresentTrace()
        {
            var game = Object.FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
            var presenter = Object.FindFirstObjectByType<PlanningBoardPresenter>(FindObjectsInactive.Include);
            var board = Object.FindFirstObjectByType<BoardView3D>(FindObjectsInactive.Include);
            if (game == null || game.Sim?.Board == null) return "Kartlı oyun yok (GameController.Sim.Board)";
            if (presenter == null || board == null || board.Folder == null) return "Sahnede PlanningBoardPresenter, BoardView3D ya da kişi dosyası yok";
            var sim = game.Sim;
            var person = sim.Board.People.FirstOrDefault(p => p.Alive && sim.Admission?.File(p.Id) != null && !sim.Admission.File(p.Id).Founder)
                         ?? sim.Board.People.FirstOrDefault(p => p.Alive);
            if (person == null) return "Sakin yok";
            bool english = DeskLoc.Instance.Lang == DeskLang.En;
            // Dosya yalnız seçili kişi için kurulur: seçim yalnız bu anlık görüntü için değişir (sunum durumu; kayda girmez)
            string selected = presenter.Ui.SelectedId;
            presenter.Ui.SelectedId = person.Id;
            BoardSnapshot s;
            try { s = PlanningBoardSnapshotBuilder.Build(sim, presenter.Ui, english, false, false); }
            finally { presenter.Ui.SelectedId = selected; }
            var f = s.Folder;
            if (f == null) return "Dosya kurulamadı: " + person.Id;
            f.Name = "Rauf"; f.PortraitKey = "rauf"; f.Profession = Loc.T("Elektrikçi");
            f.TagText = Loc.T("İNCELEME");
            f.Right.Add(new FolderLine(null, Loc.F("KARAR V.{0:000} · {1}", RaufShift, Loc.T("KABUL · TAM ERİŞİM"))));
            f.Right.Add(new FolderLine(Loc.T("BULGULAR"), null, StripTone.Ink, true));
            f.Right.Add(new FolderLine(null, Loc.T("Belge No. ≠ sevk No.")));
            f.Right.Add(new FolderLine(null, Loc.T("Soğuk gözlem: buğu var")));
            f.Right.Add(new FolderLine(Loc.T("İZLER"), null, StripTone.Ink, true));
            f.Right.Add(new FolderLine(Shift(RaufShift + 2), Loc.T("İZ 1 · erişim kaydı")));
            f.Right.Add(new FolderLine(Shift(RaufShift + 3), Loc.T("İZ 2 · parça kaydı")));
            f.Right.Add(new FolderLine(Shift(RaufShift + 4), Loc.F("İnceleme fişi açıldı · talimat {0}", 3)));
            f.DossierRight.Clear(); f.DossierRight.AddRange(f.Right.Skip(System.Math.Max(0, f.Right.Count - 8)));
            // Show önce: pano gerçek durumla yeniden çizilir; fikstür ondan sonra basılır (yoksa Show dosyayı boşaltır)
            presenter.Show();
            board.Render(s, false);
            board.Folder.Open(f);
            return $"trace: dosya {person.Id} (Rauf) · {f.Right.Count} sağ satır · etiket {f.TagText} · K4'e gidiliyor (varınca görüntü al) · dil {Loc.Lang}";
        }

        /// <summary>Fikstürü sunar ve aynı çağrıda görüntüyü alır (<c>aid</c> çekmece açıkken kalır).</summary>
        public static string PresentAndCapture(string name, string path = null)
        {
            string result = Present(name);
            path ??= Path.Combine(OutDir, "A6_fikstur_" + (name ?? "x").Trim().ToLowerInvariant() + ".png");
            return result + " · " + Corrigible.EditorTools.CorrigibleDebug.Capture(path);
        }

        /// <summary>Gerçek görüntüye döner (akışın çizimi; komut yazmaz).</summary>
        public static string Restore()
        {
            if (!Application.isPlaying) return "Play gerekli";
            var admission = Object.FindFirstObjectByType<SiginakAdmission>(FindObjectsInactive.Include);
            if (admission == null) return "Sahnede SiginakAdmission yok";
            admission.Restore();
            var presenter = Object.FindFirstObjectByType<PlanningBoardPresenter>(FindObjectsInactive.Include);
            if (presenter != null) presenter.Refresh();
            return "kabul ve pano gerçek görüntüde";
        }

        // ================================================================== düz çizim (8 px/mm = tasarımın @2x'i)

        /// <summary>Kâğıt adları: <c>belge</c> (Rauf), <c>serit_v2</c>, <c>serit_v3</c>, <c>fis</c> (Rauf karşılaştırma).</summary>
        public static readonly string[] Papers = { "belge", "serit_v2", "serit_v3", "fis" };

        /// <summary>Tasarımın düz kâğıt görüntüsü (KK PNG'leri, <c>threejs/r10/papers_new</c>).</summary>
        public static string ReferencePng(string paper, bool english)
        {
            string tag = english ? "en" : "tr";
            switch (paper)
            {
                case "belge": return ReferenceDir + "/kimlik_sevk_rauf_" + tag + "@2x.png";
                case "serit_v2": return ReferenceDir + "/kabul_talimati_v2_" + tag + "@2x.png";
                case "serit_v3": return ReferenceDir + "/kabul_talimati_v3_" + tag + "@2x.png";
                case "fis": return ReferenceDir + "/bulgu_karsilastir_rauf_" + tag + "@2x.png";
            }
            return null;
        }

        /// <summary>
        /// Kâğıdın düz, dik çizimi: W3 prefab'ı önizleme sahnesinde kurulur, fikstür verisiyle (<paramref name="english"/> dili) bağlanır ve
        /// önden ortografik kamerayla 8 px/mm çizilir. Kâğıt Quad'ı (ışıklı malzeme, önizlemede ışık yok) gizlenir, zemin malzemenin rengi;
        /// yazılar ve çizgiler ışıksız. Referans varsa <c>…_yan.png</c> (sol Unity, sağ tasarım). Sonuçta taşma yazılır.
        /// </summary>
        public static string RenderPaperFlat(string paper, bool english, string path = null)
        {
            string prefabPath = paper == "belge" ? DocumentPrefab : paper == "fis" ? SlipPrefab : paper != null && paper.StartsWith("serit") ? StripPrefab : null;
            if (prefabPath == null) return "Bilinmeyen kâğıt: " + paper + " (" + string.Join(", ", Papers) + ")";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return "Prefab yok: " + prefabPath;
            string tag = english ? "en" : "tr";
            path ??= Path.Combine(OutDir, "A6_R17_kagit_" + paper + "_" + tag + "_flat.png");

            string previous = Loc.Lang;
            var preview = EditorSceneManager.NewPreviewScene();
            Texture2D tex = null, reference = null, pair = null;
            RenderTexture rt = null;
            var active = RenderTexture.active;
            try
            {
                SiginakTextScan.UseLanguage(english ? Loc.English : Loc.Turkish);
                var go = Object.Instantiate(prefab);
                foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(go, preview);
                if (!go.activeSelf) go.SetActive(true);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var face = go.GetComponent<AdmissionPaperFace>();
                if (face == null) return "Prefab'ın kökünde kâğıt yüzü yok: " + prefabPath;
                Color ground = new Color32(0xFB, 0xF7, 0xEC, 0xFF);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.GetComponentInParent<Canvas>(true) != null) continue;
                    var m = r.sharedMaterial;
                    if (m != null && m.HasProperty("_BaseColor")) ground = m.GetColor("_BaseColor");
                    r.enabled = false;
                }
                SiginakTextScan.TranslateLabels(go);
                if (!BindFlat(paper, face)) return "Fikstür verisi bağlanamadı: " + paper;
                Canvas.ForceUpdateCanvases();

                var size = face.SizeMm;
                int width = Mathf.RoundToInt(size.x * 8), height = Mathf.RoundToInt(size.y * 8);
                rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.antiAliasing = 4;
                var camGo = new GameObject("Paper flat camera") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(camGo, preview);
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                cam.scene = preview;
                cam.orthographic = true;
                cam.orthographicSize = size.y * 0.0005f;
                cam.aspect = width / (float)height;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 2f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = ground;
                // Kâğıdın ön yüzü −z: kamera önde, kâğıda bakar
                camGo.transform.SetPositionAndRotation(go.transform.position + Vector3.back * 0.5f, Quaternion.LookRotation(Vector3.forward, Vector3.up));
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                string result = path + (face.Overflowing ? " · TAŞMA (içerik " + face.ContentBottomMm.ToString("0.0") + " mm)" : " · taşma yok");

                string refPath = ReferencePng(paper, english);
                if (refPath != null && File.Exists(refPath))
                {
                    reference = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (reference.LoadImage(File.ReadAllBytes(refPath)))
                    {
                        pair = SideBySide(tex, reference);
                        string pairPath = Path.Combine(dir ?? "", Path.GetFileNameWithoutExtension(path).Replace("_flat", "") + "_yan.png");
                        File.WriteAllBytes(pairPath, pair.EncodeToPNG());
                        result += " · yan yana " + pairPath;
                    }
                }
                return result;
            }
            finally
            {
                RenderTexture.active = active;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (tex != null) Object.DestroyImmediate(tex);
                if (reference != null) Object.DestroyImmediate(reference);
                if (pair != null) Object.DestroyImmediate(pair);
                EditorSceneManager.ClosePreviewScene(preview);
                SiginakTextScan.UseLanguage(previous);
            }
        }

        static bool BindFlat(string paper, AdmissionPaperFace face)
        {
            switch (paper)
            {
                case "belge":
                {
                    var s = Inspect();
                    if (!(face is AdmissionDocumentFace doc) || s.Document == null) return false;
                    doc.Bind(s.Document);
                    doc.ShowImpression(false, false);
                    return true;
                }
                case "serit_v2":
                {
                    var s = Build(FirstCandidate(), false);
                    if (!(face is OrdersStripFace strip) || s.Orders == null) return false;
                    strip.Bind(s.Orders);
                    return true;
                }
                case "serit_v3":
                {
                    var s = Build(ThirdOrders(), true);
                    if (!(face is OrdersStripFace strip) || s.Orders == null) return false;
                    strip.Bind(s.Orders);
                    return true;
                }
                case "fis":
                {
                    var slip = Inspect().Slips.LastOrDefault(x => x != null && x.Tool == AdmissionTool.Papers);
                    if (!(face is FindingSlipFace f) || slip == null) return false;
                    f.Bind(slip);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Dört kâğıdın TR ve EN düz çizimleri ve yan yana kopyaları.</summary>
        public static string RenderAllPapersFlat(string dir = OutDir)
        {
            var lines = new List<string>();
            foreach (var paper in Papers)
                foreach (bool en in new[] { false, true })
                    lines.Add(RenderPaperFlat(paper, en, Path.Combine(dir, "A6_R17_kagit_" + paper + "_" + (en ? "en" : "tr") + "_flat.png")));
            return string.Join("\n", lines);
        }

        /// <summary>İki görüntü yan yana (sol <paramref name="left"/>, sağ <paramref name="right"/> sol yüksekliğine en yakın komşuyla ölçeklenir).</summary>
        static Texture2D SideBySide(Texture2D left, Texture2D right)
        {
            int h = left.height;
            int rw = Mathf.Max(1, Mathf.RoundToInt(right.width * (h / (float)right.height)));
            var pair = new Texture2D(left.width + rw, h, TextureFormat.RGB24, false);
            pair.SetPixels(0, 0, left.width, h, left.GetPixels());
            var scaled = new Color[rw * h];
            for (int y = 0; y < h; y++)
            {
                int sy = Mathf.Min(right.height - 1, Mathf.FloorToInt(y * right.height / (float)h));
                for (int x = 0; x < rw; x++)
                {
                    int sx = Mathf.Min(right.width - 1, Mathf.FloorToInt(x * right.width / (float)rw));
                    scaled[y * rw + x] = right.GetPixel(sx, sy);
                }
            }
            pair.SetPixels(left.width, 0, rw, h, scaled);
            pair.Apply();
            return pair;
        }
    }
}
