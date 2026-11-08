using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Punto taraması (Aşama 10; Revizyon 2'de genişletildi; Revizyon 3 · Aşama 3'te kartlı pano için): sığınak odasındaki ve
    /// ekran katmanlarındaki (oda, CRT, eğitim notu, DeskUI: son rapor ve ayarlar) bütün TMP metinlerinin ve baskın HUD'unun
    /// (Baskin sahnesinin Overlay canvas'ları ve çalışırken kurulan etiket, telsiz satırı, üzerine gelme prefab'ları) 1920×1080'de
    /// ekrandaki em boyunu ölçer. Overlay metin: punto × canvas'a göre ölçek (1080p referans). Dünya metni: her rig pozunun
    /// kamerasıyla izdüşürülür, metnin kadrajda olduğu pozların en büyüğü alınır (okunduğu poz). Eşik girdi başınadır (ikincil
    /// ≥ 14; kart adı ≥ 18). Tasarımın kendi küçük puntoları ve anlatı süsleri gerekçeli istisnadır. Sahnelerin kaydedilmiş
    /// hâlini okur; değiştirmez.
    /// <list type="bullet">
    /// <item>Kartlı pano (PLAN §5.3, E §3): pano ailesinin metinleri (<c>Planning board</c> altı) yalnız K4 (<c>Shelf</c>) pozunda,
    /// dört köşesi kadrajdayken, kendi dikdörtgeninin merkezinden ve çizilen puntoyla (otomatik punto, <c>&lt;size&gt;</c>)
    /// ölçülür. Kişi dosyası ve KAYIPLAR zarfının sayfaları okuma pozunda (<c>personFile.readPose</c>, kapak açık) ölçülür; kapak
    /// yazıları panoda kalır.</item>
    /// <item>Kart yüzleri genel taramada atlanır; <see cref="ScanBoardCards"/> her kart prefab'ını en kötü örnekle bağlar, panonun
    /// yerlerine koyar ve K4'te ölçer (ad ≥ 18, değerler ≥ 14, küçük yazılar yüzün verdiği gerekçeyle). Kart yazısında otomatik
    /// punto ve tek satırlık alanda taşma eşikten bağımsız ihlaldir.</item>
    /// <item>Öteki dünya metinleri önceki kuralla ölçülür (tasarlanan punto; dünya tuvalinde metnin kendi merkezi ve geriye uyum için
    /// tuvalin merkezi, büyüğü). Odadaki 25 otomatik puntolu metin gözden geçirilince çizilen punto kuralı bütün odaya açılabilir.</item>
    /// <item>Vardiya kapanışı (Revizyon 3 · Aşama 4, PLAN §5.3, E §5.2–5.3; <see cref="ScanShiftClose"/>): devir fişinin yazıları genel
    /// taramada atlanır ve fiş elde, K6 (<c>ShiftClose</c>) kamerasının önündeki el pozunda (<c>ShiftReceiptPaper.HandPose</c>, ekran
    /// yüksekliğinin %67'si) üç fikstürle (<see cref="HandoverSlipFixtures"/>: V001, V031, en kötü) bağlanıp çizilen puntoyla ölçülür:
    /// adlar ≥ 18 px, öteki yazılar ≥ 14 px (Karar D9). Şalter plakasının 2. satırı (<c>TXT_Plate2</c>) her durum ve kilit metniyle
    /// K6'da ve K1'de ayrı satır olarak, çizilen puntoyla ölçülür; otomatik punto ve taşma eşikten bağımsız ihlaldir. K1'de 14 px
    /// altı gerekçeli istisnadır (plaka K6'da okunur). <see cref="Scan"/> Türkçeyi, <see cref="Run"/> ayrıca İngilizceyi ekler.</item>
    /// <item>Tarif defteri ve üretim (Revizyon 3 · Aşama 5, PLAN §5.3, E §5.2–5.3): defterin sayfa yazıları genel taramada atlanır;
    /// <see cref="ScanRecipeBook"/> sahnedeki iki statik sayfayı okuma pozunda (<c>recipeBook.readPoseLocal</c>, kapak açık; aynı dönüşüm
    /// <see cref="ReadingDelta"/>'da) V.001'in altı tarifiyle üç açılımda bağlar ve K4 kamerasıyla ölçer: adlar (başlık, çıktı adı)
    /// ≥ 18 px, öteki ≥ 14 px; taşma, otomatik punto ve ışıklı font malzemesi eşikten bağımsız ihlaldir. İş emri formunun yazıları K4'te
    /// ve "K4 yakın" pozda (yakınlaşma alt sınırı, <c>zoomLimit.x</c> 0,6) ölçülür, büyüğü alınır (Aşama 3'ün "içerik Aşama 5" istisnası
    /// kalktı: form yakın pozda okunur). <see cref="ScanBoardCards"/> tarif kartını TARİF kutusunda (<c>Order_Recipe</c>) en kötü örnekle
    /// ve YENİ etiketini zırh yığınının üst kartında ölçer. Canlı: <see cref="ScanLiveBook"/>.</item>
    /// <item>Kapı ve kabul (Revizyon 3 · Aşama 6, PLAN §5.3, E §6): K8A/K8G/K8R pozları <see cref="PoseCameras"/>'a enum'dan kendiliğinden
    /// girer (plakalar ve tezgâh yazıları genel kuralla). Kabul kâğıtlarının (belge, talimat şeridi ve ESKİ cebi, bulgu fişi, talep yaprağı
    /// ve karbon) yazıları genel taramada atlanır; <see cref="ScanAdmissionPapers"/> her kâğıdı K8R okuma pozunda (kâğıda
    /// <c>admission.readDistanceM</c> 0,318 m, K8R'nin FOV'u; tezgâhta yatan kâğıtta token K8R yönü, dik kâğıtta yüz normali; akışın
    /// <c>AdmissionStation.ReadPose</c> hesabı, <see cref="AdmissionReadPose"/>) <see cref="AdmissionFixtures.ScanCases"/> verileriyle bağlar ve
    /// çizilen puntoyla ölçer: belgedeki ad ≥ 18 px, öteki ≥ 14 px; taşma, otomatik punto ve ışıklı font malzemesi ihlaldir. Raf plakaları ve
    /// damga sapları gerekçeli istisnadır (D14); erzak tenekelerinin şablonu da (silindir yüzündeki yuva büyüyemez). Kişi dosyasının kıskaçlı
    /// etiketi (<see cref="FileTagView"/>) panoda (K4) ve dosyanın okuma pozunda ölçülür, büyüğü alınır. Canlı: <see cref="ScanLiveAdmission"/>.</item>
    /// <item>Dil ve punto geçişi (Revizyon 3 · Aşama 10, PLAN §1.2): <see cref="Run"/> Giris'i de açar (<see cref="ScanEntrance"/>: sahanlık, dolap, saat, son şalter
    /// çapalarından). <see cref="ScanLiveAll"/> Play'de yüklü bütün sahnelerin etkin yazılarını çizilen puntoyla ölçer; taşma iki eksende, otomatik punto,
    /// glif ve İngilizcede Türkçe harf bayrakları eşikten bağımsızdır. Durumları <see cref="Rev3CaptureSet"/> kurar.</item>
    /// </list>
    /// </summary>
    public static class SiginakTextScan
    {
        public const float Minimum = 14;
        /// <summary>Kart adının eşiği (board.readability: "1920 × 1080'de kart adı ≥ 18 px").</summary>
        public const float CardNameMinimum = 18;
        public const string ReportPath = "Docs/UI_Siginak_Rev3/punto_taramasi.txt";
        /// <summary>Oyunda (Play) K4'teki gerçek kartların ölçüsü (<see cref="ScanLive"/>).</summary>
        public const string LiveReportPath = "Temp/punto_k4_live.txt";
        /// <summary>Devir fişinin adlarının eşiği (Karar D9: gövde ≥ 18 px; adlar), öteki fiş yazıları <see cref="Minimum"/>.</summary>
        public const float SlipNameMinimum = 18;
        /// <summary>Oyunda (Play) eldeki devir fişinin ölçüsü (<see cref="ScanLiveSlip"/>).</summary>
        public const string LiveSlipReportPath = "Temp/punto_fis_live.txt";
        /// <summary>Fişin ve plakanın okunduğu poz (K6, vardiya şalteri yakın pozu).</summary>
        public static readonly string ShiftClosePose = SiginakPose.ShiftClose.ToString();
        const string PlateK1Reason = "şalter plakasının 2. satırı K6'da (vardiya şalteri yakın pozu) okunur; K1'de aynı durum şalterin üzerine gelme etiketinin satırında ≥ 14 px (R19a, E §5.3)";
        public const string DeskScene = "Assets/Corrigible/Desk/Scenes/DeskGame.unity", RaidScene = "Assets/Corrigible/Scenes/Baskin.unity";
        /// <summary>Menü sahnesi (Aşama 10: düzenli rapora girer; <see cref="ScanEntrance"/>).</summary>
        public const string EntranceScene = "Assets/Corrigible/Desk/Scenes/Giris.unity";
        /// <summary>Kart prefab'larının klasörü (W2 kurucusu).</summary>
        public const string CardPrefabDir = DeskSpriteManifest.Root + "/Prefabs/Rev3Board";
        /// <summary>Panonun okunduğu poz (K4).</summary>
        public static readonly string BoardPose = SiginakPose.Shelf.ToString();
        const string OutOfFrame = "kadraj dışı";
        static readonly string[] Roots = { "SiginakRoom", "SiginakCrtOverlay", "SiginakTutorial", "DeskUI" };
        /// <summary>
        /// Baskında çalışırken kurulan Overlay metinleri ve kuruldukları yer (sahnedeki yol; ölçeği çarpılır): dünya etiketi,
        /// telsiz kaydının satırı. Üzerine gelme kartı sahnede (RaidHud içinde) tarandığı için listede yok.
        /// </summary>
        static readonly (string prefab, string host)[] RaidPrefabs =
        {
            ("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidScreenLabel.prefab", null),
            ("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidLogRow.prefab", "RaidPanelCanvas/RaidPanels/RaidLogDrawer"),
        };

        public struct Entry
        {
            public string Path, Text, Pose, Exception;
            public float Pixels;
            /// <summary>Bu girdinin eşiği (px); 0 ise genel eşik (<see cref="SiginakTextScan.Minimum"/>).</summary>
            public float Minimum;
            /// <summary>Eşikten bağımsız sorun (taşma, otomatik punto); yoksa null.</summary>
            public string Problem;
            public float Threshold => Minimum > 0 ? Minimum : SiginakTextScan.Minimum;
            public bool Small => Pixels < Threshold;
            public bool Violation => Problem != null || (Exception == null && Pixels < Threshold);
        }

        /// <summary>
        /// Gerekçeli istisnalar: yol parçası → neden. Tasarım panolarında (K1–K8) bu boyda olan ya da hiçbir pozda okunmak için
        /// konmamış (anlatı, doku) metinler.
        /// </summary>
        static readonly (string part, string why)[] Exceptions =
        {
            ("Shell/Doctrine poster/", "afişin küçük satırları anlatı süsü"),
            ("Shell/Freed board/", "kurtarılan bölgeler panosu (mantar pano; K1'de ölçülen 3,5–12,6 px, anlatı süsü: K1'den okunmak için konmadı; kurtarılan bölge sayısı üzerine gelme satırında, bölgeler haritada okunur)"),
            ("Shell/Chalk tally/", "tebeşir çetelesinin “vardiya” yazısı (anlatı süsü; vardiya sayacı rafta)"),
            ("Shell/Alarm panel/Alarm counter/", "alarm eşiği ve vardiya artışı (tasarım: pencere içi baskı ve fiş); eşik ve artış üzerine gelme satırında da yazılı"),
            ("Shell/Alarm panel/Radio plaque/", "telsiz odası fişi (Revizyon 2 · S3; görevli varken görünür, alarm panosunun altında); görevli sığınak panosunda ve CRT'de de okunur"),
            ("Shell/Alarm panel/AlarmBoard/Body/TXT_Note", "alarm panosunun alt notu (tasarım ~9 px; aynı bilgi üzerine gelme satırında)"),
            ("Radio log/", "CRT önündeki telsiz kayıt defteri (tex2.js logBook ölçüsünde; son dört kayıt teleks, CRT ve raporlarda da okunur)"),
            ("/Stamp/Word", "haritadaki ÖZGÜR damgasının bandı (tasarım ~9 ekran px, Aşama 5; durum tarama ve lejantta da var)"),
            ("/Stamp/Day", "haritadaki ÖZGÜR damgasının vardiya kodu (tasarım ~9 ekran px, Aşama 5)"),
            ("Report sheet/", "EVRAK tepsisindeki A5 kâğıt başlığı (okuma görünümü ayrıca açılır)"),
            ("TelexPaper/Body/TXT_Code", "teleks kâğıdının kodu (Revizyon 3 TelexPaper: büyük harf 9 mm); durum ve ileti satırı K1'de okunur"),
            ("TelexPaper/Body/TXT_Prev", "teleks kâğıdının önceki satırı (büyük harf 7 mm, kesikli ayracın altında); satır teleks şeridinde ve raporlarda da var"),
            ("/Tag/TXT_Tag", "tuş etiketi yalnız KAPI tuşunda (“kapıda biri bekliyor”, ≥ 14 px); öbür tuşlarda boş ve kapalı"),
            ("IntercomBox/Body/TXT_Plate", "interkom plakası (Revizyon 3 IntercomBox: büyük harf 11 mm); kapının adı üzerine gelme etiketinde"),
            ("PlanningBoard/Body/TXT_FieldBand", "panonun SAHA baskı bandı (tasarım büyük harf 4,4 mm; süs: saha cepleri plakalarında TEKNİSYEN, EMP UZMANI, ANALİST yazar)"),
            ("PlanningBoard/Body/LockRod/TXT_LockTag", "panonun kilit etiketi (tasarım büyük harf 5 mm, el yazısı); kilit ve nedeni pano erişiminin üzerine gelme satırında (“PLAN KİLİTLİ · neden”) ≥ 14 px"),
            ("PlanningBoard/Body/LockRod/Kilit etiketi 2. satır", "kilit etiketinin 2. satırı (neden; tasarımın küçük el yazısı satırı); aynı neden pano erişiminin üzerine gelme satırında (“PLAN KİLİTLİ · neden”) ≥ 14 px"),
            ("/KYGA · 07", "tarif defteri kapağının künyesi (tasarım 6,2 mm, %65 soluk; anlatı süsü): defterin adı kapak etiketinde (TXT_Cover) ve üzerine gelme etiketinde ≥ 14 px"),
            ("DecisionStampRack/", "karar damgalarının raf plakaları (yuva 6 mm büyük harf; K8A'da ölçülen 6,4–9,1 px; Aşama 6 D14 gerekçeli istisna): geri dönüşsüz eylemin adı damganın üzerine gelme satırında ≥ 14 px (Aşama 10 A7 ölçümü, TR + EN) ve belgedeki izde okuma pozunda (K8R) okunur"),
            ("/Drawer/Teneke ", "erzak tenekesinin ERZAK şablonu (Aşama 6: K8A ≈ 13,9 px; büyütülemez: 50 mm yuva silindir yüzünde düz TMP, EN “RATIONS” yuvayı şimdiden dolduruyor, büyürse yazı silindirden kopar): tenekeler yalnız DIŞARIDAN YARDIM kararında çekmeceyle görünür; kararın adı raf plakasında, damganın üzerine gelme satırında (≥ 14 px) ve izde"),
            ("/TXT_Cap", "karar damgalarının sap yazısı (4 mm, K8A ≈ 8 px; Aşama 6 D14 gerekçeli istisna): kararın adı raf plakasında, üzerine gelme satırında ve izde"),
            ("ShiftCard/Body/Tilt/TXT_Sign", "vardiya kartının imza satırı (anlatı süsü; M3'te 11,5 px): kartın kimliği TXT_Shift ve TXT_Role ≥ 14 px"),
            ("LastSwitch/Body/TXT_Lamp", "son şalterin lamba yazısı (M0'da 6,7 px, MS kadrajı dışında): şalterin adı plakada ≥ 14 px"),
            ("Leaf/TXT_LeafPlate", "ayar dolabı kapağının küçük plakası (M0'da 9,1 px): dolabın adı TXT_Stencil 36 px"),
        };

        /// <summary>
        /// İş emri formunun okunduğu ek poz (Aşama 5, E §5.3; R18c "YAKIN · FORM OKUNUR"): K4 kamerası yakınlaşmanın alt sınırında
        /// (<c>SiginakCameraRig.zoomLimit.x</c>, 0,6: FOV × 0,6). Yalnız formun yazıları bu pozla da ölçülür.
        /// </summary>
        public static readonly string BoardNearPose = SiginakPose.Shelf + " yakın";
        /// <summary>Defter sayfalarının adlarının eşiği (başlık, çıktı adı; PLAN §5.3), öteki sayfa yazıları <see cref="Minimum"/>.</summary>
        public const float BookNameMinimum = 18;
        /// <summary>Oyunda (Play) açık defterin sayfa ölçüsü (<see cref="ScanLiveBook"/>).</summary>
        public const string LiveBookReportPath = "Temp/punto_defter_live.txt";
        const string HurdaReason = "hurda kutusunun küçük satırı (1 eşya → n metal); aynı bilgi hurdanın üzerine gelme satırında ≥ 14 px";

        [MenuItem("Corrigible/Sığınak/Punto taraması")]
        public static string Run()
        {
            var entries = new List<Entry>();
            foreach (var path in new[] { DeskScene, RaidScene, EntranceScene })
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    if (path == DeskScene)
                    {
                        // Scan Türkçe fiş ve plaka satırlarını içerir; İngilizcesi burada
                        entries.AddRange(Scan(scene));
                        entries.AddRange(ScanShiftClose(scene, true));
                        entries.AddRange(ScanBoardCards(scene, false));
                        entries.AddRange(ScanBoardCards(scene, true));
                        entries.AddRange(ScanRecipeBook(scene, false));
                        entries.AddRange(ScanRecipeBook(scene, true));
                        entries.AddRange(ScanAdmissionPapers(scene, false));
                        entries.AddRange(ScanAdmissionPapers(scene, true));
                    }
                    else if (path == RaidScene) entries.AddRange(ScanRaid(scene));
                    else entries.AddRange(ScanEntrance(scene));
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, Report(entries));
            int bad = entries.Count(e => e.Violation);
            return $"{entries.Count} metin, {bad} ihlal, {entries.Count(e => e.Exception != null && e.Small)} istisna · {ReportPath}";
        }

        /// <summary>Baskın: sahnenin Overlay canvas'ları ve çalışırken kurulan prefab'lar (hepsi 1080p referanslı Overlay).</summary>
        public static List<Entry> ScanRaid(Scene scene)
        {
            var list = new List<Entry>();
            foreach (var canvas in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true))
                         .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay))
                foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true))
                    list.Add(Overlay(t, canvas.transform, PathOf(t.transform)));
            foreach (var (path, host) in RaidPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                float hostScale = 1;
                var at = host == null ? null : Find(scene, host);
                for (var p = at; p != null && !(p.TryGetComponent<Canvas>(out var c) && c.isRootCanvas); p = p.parent) hostScale *= Mathf.Abs(p.localScale.y);
                foreach (var t in prefab.GetComponentsInChildren<TMP_Text>(true))
                    list.Add(Overlay(t, prefab.transform.parent, System.IO.Path.GetFileNameWithoutExtension(path) + "/" + PathOf(t.transform), hostScale));
            }
            return list;
        }

        static Transform Find(Scene scene, string path)
        {
            int slash = path.IndexOf('/');
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == (slash < 0 ? path : path.Substring(0, slash)));
            return root == null ? null : slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
        }

        static Entry Overlay(TMP_Text t, Transform canvas, string path, float scale = 1)
        {
            for (var p = t.transform; p != null && p != canvas; p = p.parent) scale *= Mathf.Abs(p.localScale.y);
            float px = t.fontSize * scale;
            return new Entry { Path = path, Text = Short(t.text), Pixels = px, Pose = "Overlay", Minimum = Minimum, Exception = ExceptionOf(t, path, px, Minimum) };
        }

        public static List<Entry> Scan(Scene scene)
        {
            var list = new List<Entry>();
            var roots = scene.GetRootGameObjects().Where(g => Roots.Contains(g.name)).ToArray();
            var rig = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).FirstOrDefault();
            var cameras = rig != null ? PoseCameras(rig) : new List<(string, Camera)>();
            var near = rig != null ? NearBoardCameras(rig) : new List<(string, Camera)>();
            try
            {
                foreach (var root in roots)
                {
                    // Yeni panoyla arşivlenen eski ünite hiçbir oyun pozunda açılmaz. Geçici kapalı kapı/popup metinleri
                    // taranmaya devam eder; yalnız bu kalıcı kapalı eski sunum ağacı kapsam dışıdır.
                    var archivedShelf = root.GetComponentInChildren<PlanningBoardView>(true) != null ? root.transform.Find("Shelf") : null;
                    foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        // Boş ve kapalı anahtar etiketi hiçbir pozda çizilmez (Aşama 10 §1.7 D); etiketin dolu hâli üzerine gelme yerine KAPI tuşunda
                        if (IsIdleKeyTag(t)) continue;
                        if (archivedShelf != null && !archivedShelf.gameObject.activeSelf && t.transform.IsChildOf(archivedShelf)) continue;
                        // Kart yüzleri (sahnede kayıtlı havuz olsa bile) kendi taramalarında ölçülür
                        if (t.GetComponentInParent<CardFace>(true) != null || t.GetComponentInParent<BoardCardView>(true) != null) continue;
                        // Devir fişi (dildeki fiş ve tepsideki kopya) elde, K6'da ölçülür (ScanShiftClose)
                        if (t.GetComponentInParent<HandoverSlipFace>(true) != null) continue;
                        // Defter sayfaları okuma pozunda (ScanRecipeBook), YENİ etiketi yığının kartında (ScanBoardCards) ölçülür
                        if (t.GetComponentInParent<RecipeBookPage>(true) != null || t.GetComponentInParent<NewItemTag>(true) != null) continue;
                        // Kabul kâğıtları (Aşama 6) K8R okuma pozunda ölçülür (ScanAdmissionPapers)
                        if (t.GetComponentInParent<AdmissionPaperFace>(true) != null) continue;
                        var (px, pose) = Measure(t, cameras, near);
                        string path = PathOf(t.transform);
                        list.Add(new Entry { Path = path, Text = Short(t.text), Pixels = px, Pose = pose, Minimum = Minimum, Exception = ExceptionOf(t, path, px, Minimum) });
                    }
                }
            }
            finally { Release(cameras); Release(near); }
            list.AddRange(ScanShiftClose(scene, false));
            return list;
        }

        static bool IsIdleKeyTag(TMP_Text t) => string.IsNullOrEmpty(t.text) && !t.gameObject.activeInHierarchy && t.name == "TXT_Tag";

        static void Release(List<(string, Camera)> cameras)
        {
            foreach (var (_, cam) in cameras)
            {
                if (cam == null) continue;
                var rt = cam.targetTexture;
                cam.targetTexture = null;
                if (rt != null) rt.Release();
                Object.DestroyImmediate(cam.gameObject);
            }
        }

        static string ExceptionOf(TMP_Text t, string path, float px, float minimum)
        {
            if (px >= minimum) return null;
            foreach (var (part, why) in Exceptions) if (path.Contains(part)) return why;
            return t != null ? ComponentException(t) : null;
        }

        /// <summary>
        /// Bileşene bağlı istisnalar (nesne adları sahne kurucusunda belirlenir): hurda. İş emri formunun Aşama 3
        /// istisnası ("içerik Aşama 5") Aşama 5'te kalktı: form "K4 yakın" pozda ölçülür (<see cref="IsWorkOrderText"/>).
        /// </summary>
        static string ComponentException(TMP_Text t)
        {
            var hurda = t.GetComponentInParent<HurdaBin>(true);
            if (hurda != null && hurda.GetComponent<BoardView3D>() == null) return HurdaReason;
            return null;
        }

        static Object Ref(Object owner, string field)
        {
            if (owner == null) return null;
            var p = new SerializedObject(owner).FindProperty(field);
            return p != null && p.propertyType == SerializedPropertyType.ObjectReference ? p.objectReferenceValue : null;
        }

        /// <summary>Rig pozlarıyla aynı fiziksel kameralar (sensör, odak, lens kaydırması), 1920×1080 hedef. <paramref name="only"/> verilirse yalnız o poz.</summary>
        static List<(string, Camera)> PoseCameras(SiginakCameraRig rig, string only = null)
        {
            var sensor = new SerializedObject(rig).FindProperty("sensorSizeMm").vector2Value;
            var list = new List<(string, Camera)>();
            foreach (SiginakPose pose in System.Enum.GetValues(typeof(SiginakPose)))
            {
                if (only != null && pose.ToString() != only) continue;
                if (!rig.TryGetPose(pose, out var slot)) continue;
                list.Add((pose.ToString(), MakeCamera("Scan camera " + pose, sensor, slot.fieldOfView, slot.lensShiftY, slot.anchor.position, slot.anchor.rotation)));
            }
            return list;
        }

        /// <summary>Rig'in fiziksel kamerasıyla aynı (sensör, dikey kapı, odak FOV'dan, lens kaydırması), 1920×1080 hedefli ölçüm kamerası.</summary>
        static Camera MakeCamera(string name, Vector2 sensor, float fieldOfView, float lensShiftY, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.targetTexture = new RenderTexture(1920, 1080, 0) { hideFlags = HideFlags.HideAndDontSave };
            cam.usePhysicalProperties = true;
            cam.sensorSize = sensor;
            cam.gateFit = Camera.GateFitMode.Vertical;
            cam.focalLength = Camera.FieldOfViewToFocalLength(fieldOfView, sensor.y);
            cam.lensShift = new Vector2(0, lensShiftY);
            cam.nearClipPlane = 0.03f;
            go.transform.SetPositionAndRotation(position, rotation);
            return cam;
        }

        static IEnumerable<(string name, Camera cam)> AtBoardPose(List<(string, Camera)> cameras) => cameras.Where(c => c.Item1 == BoardPose);

        /// <summary>
        /// "K4 yakın" ölçüm kamerası (<see cref="BoardNearPose"/>): K4 yuvası, FOV × rig'in yakınlaşma alt sınırı (<c>zoomLimit.x</c>;
        /// K4'te etkin aralık <c>SiginakCameraRig.ZoomRange</c>, alt sınır aynı). Bakış K4'ün merkezinde (yaw/pitch 0).
        /// </summary>
        static List<(string, Camera)> NearBoardCameras(SiginakCameraRig rig)
        {
            var list = new List<(string, Camera)>();
            if (!rig.TryGetPose(SiginakPose.Shelf, out var slot)) return list;
            var so = new SerializedObject(rig);
            var sensor = so.FindProperty("sensorSizeMm").vector2Value;
            var limit = so.FindProperty("zoomLimit");
            float zoom = limit != null ? limit.vector2Value.x : 0.6f;
            if (zoom <= 0 || zoom >= 1) return list;
            float fov = Mathf.Clamp(slot.fieldOfView * zoom, 10, 100);
            list.Add((BoardNearPose, MakeCamera("Scan camera " + BoardNearPose, sensor, fov, slot.lensShiftY, slot.anchor.position, slot.anchor.rotation)));
            return list;
        }

        /// <summary>İş emri formunun yazısı (<see cref="WorkOrderView"/>'in altı; görünümün kendisi panonun kökünde değilse).</summary>
        static bool IsWorkOrderText(TMP_Text t)
        {
            var order = t.GetComponentInParent<WorkOrderView>(true);
            return order != null && order.GetComponent<BoardView3D>() == null;
        }

        static (float px, string pose) Measure(TMP_Text t, List<(string, Camera)> cameras) => Measure(t, cameras, null);

        static (float px, string pose) Measure(TMP_Text t, List<(string, Camera)> cameras, List<(string, Camera)> near)
        {
            var canvas = t.GetComponentsInParent<Canvas>(true).LastOrDefault();
            if (t is TextMeshProUGUI && canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                // Overlay: CanvasScaler 1080 referans, yüksekliğe göre → 1080p'de ölçek 1; canvas altındaki yerel ölçekler çarpılır
                float scale = 1;
                for (var p = t.transform; p != null && p != canvas.transform; p = p.parent) scale *= Mathf.Abs(p.localScale.y);
                return (t.fontSize * scale, "Overlay");
            }
            // Kişi dosyasının kıskaçlı etiketi (Aşama 6, R17f): dosyanın üst kenarından 24 mm dışarı taşan sekme; panoda (K4) işaret eder,
            // okuma pozunda dosya kadrajı doldurunca üst kenarın dışında kalabilir. İki yerde ölçülür, büyüğü alınır (sayfa yazıları
            // yalnız okuma pozunda, kapak yazıları yalnız panoda)
            if (t.GetComponentInParent<FileTagView>(true) != null)
            {
                var onBoard = MeasureWorld(t, AtBoardPose(cameras), true, true);
                if (ReadingDelta(t, out var tagDelta))
                {
                    var reading = MeasureWorld(t, AtBoardPose(cameras), true, true, tagDelta, " · okuma");
                    if (reading.px > onBoard.px) return reading;
                }
                return onBoard;
            }
            // Kişi dosyası / KAYIPLAR sayfaları: okuma pozunda (K4 kamerası, dosya öne gelmiş ve açık)
            if (ReadingDelta(t, out var delta)) return MeasureWorld(t, AtBoardPose(cameras), true, true, delta, " · okuma");
            // İş emri formu (Aşama 5): K4 ve "K4 yakın" (R18c "form okunur"), dört köşe kadrajda, çizilen punto; büyüğü
            if (near != null && near.Count > 0 && IsWorkOrderText(t)) return MeasureWorld(t, AtBoardPose(cameras).Concat(near), true, true);
            // Pano ailesi: yalnız K4, dört köşe kadrajda, çizilen punto
            if (IsBoardFamily(t)) return MeasureWorld(t, AtBoardPose(cameras), true, true);
            // Öteki dünya metinleri: tasarlanan punto, metnin kendi merkezi; dünya tuvalinde tuvalin merkezi de aday (geriye uyum:
            // düzeni editörde kurulmamış metinler eski kuralla aynı sonucu verir)
            var own = MeasureWorld(t, cameras, false, false);
            if (t is TextMeshProUGUI && canvas != null)
            {
                float em = t.fontSize * Mathf.Abs(t.transform.lossyScale.y);
                var legacy = MeasureAt(canvas.transform.position, t.transform.up * em, null, cameras);
                if (legacy.px > own.px) return (legacy.px, legacy.pose + " · tuval merkezi");
            }
            return own;
        }

        /// <summary>Metin kartlı panonun parçası mı (<c>Planning board</c> altı ya da <see cref="BoardView3D"/>'nin çocuğu).</summary>
        static bool IsBoardFamily(TMP_Text t)
        {
            if (t.GetComponentInParent<BoardView3D>(true) != null) return true;
            for (var p = t.transform.parent; p != null; p = p.parent) if (p.name == "Planning board") return true;
            return false;
        }

        /// <summary>Metnin dikdörtgeninin dünya merkezi (UGUI ve 3B TMP aynı).</summary>
        public static Vector3 Centre(TMP_Text t) => t.rectTransform.TransformPoint(t.rectTransform.rect.center);

        /// <summary>
        /// Çizilen en küçük punto (görünür karakterlerin <c>pointSize</c>'ı: otomatik punto ve <c>&lt;size&gt;</c> dahil). Boş metinde
        /// tasarlanan punto döner ve <paramref name="empty"/> true olur.
        /// </summary>
        public static float RenderedPointSize(TMP_Text t, out bool empty)
        {
            empty = true;
            if (t.font == null || string.IsNullOrEmpty(t.text)) return t.fontSize;
            t.ForceMeshUpdate(true, true);
            var info = t.textInfo;
            float pt = float.MaxValue;
            if (info != null)
                for (int i = 0; i < info.characterCount && i < info.characterInfo.Length; i++)
                    if (info.characterInfo[i].isVisible) pt = Mathf.Min(pt, info.characterInfo[i].pointSize);
            if (pt == float.MaxValue) return t.fontSize;
            empty = false;
            return pt;
        }

        /// <summary>
        /// Dünya metnini verilen kameralarla ölçer: metnin kendi merkezi ve em yüksekliği (yukarı yönde) izdüşürülür, en büyüğü alınır.
        /// <paramref name="cornersInFrame"/>: dört köşe de kadrajda olmalı. <paramref name="rendered"/>: çizilen punto (yoksa tasarlanan).
        /// Hiçbir kamerada kadrajda değilse (0, "kadraj dışı").
        /// </summary>
        public static (float px, string pose) MeasureWorld(TMP_Text t, IEnumerable<(string name, Camera cam)> cameras, bool cornersInFrame, bool rendered) =>
            MeasureWorld(t, cameras, cornersInFrame, rendered, Matrix4x4.identity, null);

        static (float px, string pose) MeasureWorld(TMP_Text t, IEnumerable<(string name, Camera cam)> cameras, bool cornersInFrame, bool rendered,
            Matrix4x4 delta, string poseSuffix)
        {
            float pt = rendered ? RenderedPointSize(t, out _) : t.fontSize;
            float em = pt * Mathf.Abs(t.transform.lossyScale.y) * (t is TextMeshPro ? 0.1f : 1);
            var centre = delta.MultiplyPoint3x4(Centre(t));
            var up = delta.MultiplyVector(t.transform.up * em);
            Vector3[] corners = null;
            if (cornersInFrame)
            {
                corners = new Vector3[4];
                t.rectTransform.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++) corners[i] = delta.MultiplyPoint3x4(corners[i]);
            }
            var (px, pose) = MeasureAt(centre, up, corners, cameras);
            return (px, px > 0 && poseSuffix != null ? pose + poseSuffix : pose);
        }

        static (float px, string pose) MeasureAt(Vector3 centre, Vector3 up, Vector3[] corners, IEnumerable<(string name, Camera cam)> cameras)
        {
            float best = 0;
            string bestPose = OutOfFrame;
            foreach (var (name, cam) in cameras)
            {
                if (cam == null || !InFrame(cam, centre)) continue;
                if (corners != null && !corners.All(c => InFrame(cam, c))) continue;
                var a = cam.WorldToScreenPoint(centre);
                var b = cam.WorldToScreenPoint(centre + up);
                float px = Vector2.Distance(a, b);
                if (px > best) { best = px; bestPose = name; }
            }
            return (best, bestPose);
        }

        static bool InFrame(Camera cam, Vector3 world)
        {
            var a = cam.WorldToScreenPoint(world);
            return a.z > cam.nearClipPlane && a.x >= 0 && a.x <= cam.pixelWidth && a.y >= 0 && a.y <= cam.pixelHeight;
        }

        /// <summary>
        /// Kişi dosyası ve KAYIPLAR zarfının sayfa yazıları için okuma pozu dönüşümü (kapak yazıları hariç): kök
        /// <c>personFile.readPose</c>'a (pano çerçevesinde konum ve x eğimi) gelir; kapak parçasının altındaki yazılar kapak açık
        /// açıdayken (PartDriver aralığının ucu) ölçülür. Görünümler <c>FolderMotion</c> ile aynı hesabı yapar.
        /// </summary>
        static bool ReadingDelta(TMP_Text t, out Matrix4x4 delta)
        {
            delta = Matrix4x4.identity;
            // Tarif defterinin statik sayfaları (Aşama 5): kök okuma pozunda, kapak açık (yaprak yüzleri ve kapak yazıları hariç)
            var book = t.GetComponentInParent<RecipeBookView>(true);
            if (book != null)
            {
                var page = t.GetComponentInParent<RecipeBookPage>(true);
                if (page == null || (page != Ref(book, "pageLeft") && page != Ref(book, "pageRight"))) return false;
                delta = BookReadingDelta(book, t.transform);
                return true;
            }
            MonoBehaviour view = t.GetComponentInParent<PersonFolderView>(true);
            if (view == null) view = t.GetComponentInParent<LostEnvelopeView>(true);
            if (view == null) return false;
            foreach (var cover in new[] { "coverTitle", "coverLine", "coverText" })
                if (Ref(view, cover) == t) return false;
            var root = Ref(view, "root") as Transform;
            if (root == null) root = view.transform;
            var frame = Ref(view, "frame") as Transform;
            var coverPart = Ref(view, "cover") as PartDriver;

            var theme = SiginakTheme.Active;
            float N(string key, float fallback) => theme != null ? theme.NumberOf(key, fallback) : fallback;
            var pos = new Vector3(N("personFile.readPose.pos[0]", 0.015f), N("personFile.readPose.pos[1]", 1.203f), N("personFile.readPose.pos[2]", -0.680f));
            float tilt = N("personFile.readPose.tiltX", -8f);
            var worldPos = frame != null ? frame.TransformPoint(pos) : pos;
            var worldRot = (frame != null ? frame.rotation : Quaternion.identity) * Quaternion.Euler(tilt, 0, 0);
            var read = Matrix4x4.TRS(worldPos, worldRot, root.lossyScale) * root.worldToLocalMatrix;

            var open = Matrix4x4.identity;
            if (coverPart != null && t.transform.IsChildOf(coverPart.transform))
            {
                var ct = coverPart.transform;
                float end = Mathf.Abs(coverPart.RangeTo) > Mathf.Abs(coverPart.RangeFrom) ? coverPart.RangeTo : coverPart.RangeFrom;
                float angle = end < 0f ? 360f + end : end;
                var axis = coverPart.Axis == PartAxis.X ? Vector3.right : coverPart.Axis == PartAxis.Y ? Vector3.up : Vector3.forward;
                var parent = ct.parent != null ? ct.parent.localToWorldMatrix : Matrix4x4.identity;
                open = parent * Matrix4x4.TRS(coverPart.PivotLocal, Quaternion.AngleAxis(angle, axis), ct.localScale) * ct.worldToLocalMatrix;
            }
            delta = read * open;
            return true;
        }

        /// <summary>
        /// Tarif defterinin okuma pozu dönüşümü (<see cref="RecipeBookView"/>'in hesabıyla aynı): kök pano çerçevesinde
        /// <see cref="RecipeBookView.ReadPoseLocal"/>'a (<c>recipeBook.readPoseLocal</c>: konum ve x eğimi) gelir; kapak parçasının altındaki
        /// nesneler (sol sayfa) kapak açık uçtayken (−178°, okuyucuya doğru dönülmüş) ölçülür. <paramref name="t"/> kitabın altındaki nesne.
        /// </summary>
        static Matrix4x4 BookReadingDelta(RecipeBookView book, Transform t)
        {
            var root = Ref(book, "root") as Transform;
            if (root == null) root = book.transform;
            var frame = Ref(book, "frame") as Transform;
            var coverPart = Ref(book, "cover") as PartDriver;
            var pose = RecipeBookView.ReadPoseLocal;
            var worldPos = frame != null ? frame.TransformPoint(pose.position) : pose.position;
            var worldRot = (frame != null ? frame.rotation : Quaternion.identity) * pose.rotation;
            var read = Matrix4x4.TRS(worldPos, worldRot, root.lossyScale) * root.worldToLocalMatrix;
            var open = Matrix4x4.identity;
            if (coverPart != null && t.IsChildOf(coverPart.transform))
            {
                var ct = coverPart.transform;
                float end = Mathf.Abs(coverPart.RangeTo) > Mathf.Abs(coverPart.RangeFrom) ? coverPart.RangeTo : coverPart.RangeFrom;
                var axis = coverPart.Axis == PartAxis.X ? Vector3.right : coverPart.Axis == PartAxis.Y ? Vector3.up : Vector3.forward;
                var parent = ct.parent != null ? ct.parent.localToWorldMatrix : Matrix4x4.identity;
                open = parent * Matrix4x4.TRS(coverPart.PivotLocal, Quaternion.AngleAxis(end, axis), ct.localScale) * ct.worldToLocalMatrix;
            }
            return read * open;
        }

        // ================================================================ tarif defteri (Revizyon 3 · Aşama 5, PLAN §5.3)

        /// <summary>
        /// Defter sayfaları okuma pozunda (E §5.2): sahnedeki <c>pageLeft</c>/<c>pageRight</c> önizleme sahnesine kopyalanır, okuma
        /// dönüşümüyle (<see cref="BookReadingDelta"/>) yerleştirilir, V.001'in altı tarifiyle (<see cref="ProductionFixtures.BookSnapshot"/>,
        /// <paramref name="english"/> dili) üç açılımda bağlanır ve K4 kamerasıyla çizilen puntoyla ölçülür: adlar
        /// (<see cref="RecipeBookPage.NameTexts"/>) ≥ 18 px, öteki görünür yazılar (<see cref="RecipeBookPage.ValueTexts"/>) ≥ 14 px, dört
        /// köşe kadrajda. Otomatik punto, ışıklı font malzemesi (Aşama 4 bulgusu: World Space uGUI'de mürekkep solar) ve sayfa taşması
        /// (<see cref="RecipeBookPage.Overflowing"/>) eşikten bağımsız ihlaldir. Defter sahneye bağlı değilse boş liste.
        /// </summary>
        public static List<Entry> ScanRecipeBook(Scene scene, bool english)
        {
            var list = new List<Entry>();
            var rig = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).FirstOrDefault();
            var book = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RecipeBookView>(true)).FirstOrDefault();
            if (rig == null || book == null) return list;
            var sources = new[] { Ref(book, "pageLeft") as RecipeBookPage, Ref(book, "pageRight") as RecipeBookPage };
            if (sources.Any(p => p == null)) return list;

            string previous = Corrigible.Core.Loc.Lang;
            var cameras = PoseCameras(rig, BoardPose);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                UseLanguage(english ? Corrigible.Core.Loc.English : Corrigible.Core.Loc.Turkish);
                string tag = english ? "EN" : "TR";
                var s = ProductionFixtures.BookSnapshot(english);
                var pages = new RecipeBookPage[2];
                for (int side = 0; side < 2; side++)
                {
                    var source = sources[side];
                    var go = Object.Instantiate(source.gameObject);
                    foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(go, preview);
                    if (!go.activeSelf) go.SetActive(true);
                    var world = BookReadingDelta(book, source.transform) * source.transform.localToWorldMatrix;
                    go.transform.SetPositionAndRotation(world.GetColumn(3), world.rotation);
                    go.transform.localScale = source.transform.lossyScale;
                    pages[side] = go.GetComponent<RecipeBookPage>();
                }
                int count = (s.Recipes.Count + 1) / 2;
                for (int spread = 0; spread < count; spread++)
                {
                    var left = s.Recipes[spread * 2];
                    var right = spread * 2 + 1 < s.Recipes.Count ? s.Recipes[spread * 2 + 1] : null;
                    pages[0].Bind(left, s.Book, false, spread > 0, false, true);
                    pages[1].Bind(right, s.Book, true, false, spread < count - 1, true);
                    Canvas.ForceUpdateCanvases();
                    for (int side = 0; side < 2; side++)
                    {
                        var page = pages[side];
                        var names = new HashSet<TMP_Text>(page.NameTexts);
                        bool first = true;
                        foreach (var t in page.NameTexts.Concat(page.ValueTexts))
                        {
                            if (t == null || !t.gameObject.activeInHierarchy || !t.enabled || string.IsNullOrEmpty(t.text)) continue;
                            var (px, pose) = MeasureWorld(t, cameras, true, true);
                            bool isName = names.Contains(t);
                            string problem = t.enableAutoSizing ? "otomatik punto (sayfa yazısı küçülmez)"
                                : t.font != null && t.fontSharedMaterial != t.font.material ? "ışıklı ya da başka font malzemesi (fontun kendi ışıksız malzemesi olmalı)"
                                : null;
                            if (problem == null && first && page.Overflowing) problem = "taşma: " + string.Join(", ", page.OverflowFields);
                            first = false;
                            list.Add(new Entry
                            {
                                Path = "Book/" + (spread + 1) + (side == 0 ? "L" : "R") + "/" + RelativePath(page.transform, t.transform), Text = Short(t.text),
                                Pixels = px, Pose = pose + " · okuma · " + tag, Minimum = isName ? BookNameMinimum : Minimum, Problem = problem,
                            });
                        }
                    }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                Release(cameras);
                UseLanguage(previous);
            }
            return list;
        }

        /// <summary>
        /// Oyunda (Play) açık defterin sayfa ölçüsü (E §5.2 canlı): <paramref name="cam"/> odanın kamerası (K4), defter açık ve durgun
        /// (<see cref="BookState.Open"/>); iki statik sayfanın adları ≥ 18 px, öteki yazıları ≥ 14 px, çizilen puntoyla, 1080p'ye
        /// ölçeklenmiş. Sonuç <see cref="LiveBookReportPath"/>'e eklenir (her çağrı bir blok: dil ve açılım başlıkta); dönen satır sınıfların
        /// en küçükleri. TR/EN ve üç açılım için dili değiştirip sayfa çevirerek yeniden çağır.
        /// </summary>
        public static string ScanLiveBook(Camera cam)
        {
            if (cam == null) return "kamera yok";
            var book = Object.FindFirstObjectByType<RecipeBookView>(FindObjectsInactive.Exclude);
            if (book == null) return "sahnede defter yok";
            if (book.State != BookState.Open || book.Moving) return "defter açık ve durgun değil (" + book.State + ")";
            float norm = 1080f / Mathf.Max(1, cam.pixelHeight);
            var cams = new[] { ("Live", cam) };
            var rows = new List<(float px, string line)>();
            var minima = new Dictionary<string, float>();
            int bad = 0;
            foreach (var page in new[] { book.PageLeft, book.PageRight })
            {
                if (page == null || !page.isActiveAndEnabled) continue;
                var names = new HashSet<TMP_Text>(page.NameTexts);
                foreach (var t in page.NameTexts.Concat(page.ValueTexts))
                {
                    if (t == null || !t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) continue;
                    var (raw, _) = MeasureWorld(t, cams, true, true);
                    float px = raw * norm;
                    bool isName = names.Contains(t);
                    string cls = isName ? "ad" : "öteki";
                    float min = isName ? BookNameMinimum : Minimum;
                    if (!minima.TryGetValue(cls, out var m) || px < m) minima[cls] = px;
                    bool violation = px < min || t.enableAutoSizing;
                    if (violation) bad++;
                    rows.Add((px, $"{px,5:0.0}/{min:0} px · {cls} · {(page.RightPage ? "R" : "L")} · {RelativePath(page.transform, t.transform)} · {Short(t.text)}{(violation ? " · İHLAL" : "")}"));
                }
            }
            bool overflow = (book.PageLeft != null && book.PageLeft.Overflowing) || (book.PageRight != null && book.PageRight.Overflowing);
            var sb = new StringBuilder();
            sb.AppendLine($"Tarif defteri açık, canlı · {System.DateTime.Now:yyyy-MM-dd HH:mm} · {cam.pixelWidth}×{cam.pixelHeight} → 1080p · dil {Corrigible.Core.Loc.Lang} · açılım {book.Spread + 1}/{book.SpreadCount} · ad ≥ {BookNameMinimum}, öteki ≥ {Minimum}");
            foreach (var kv in minima.OrderBy(k => k.Key)) sb.AppendLine($"en küçük {kv.Key}: {kv.Value:0.0} px");
            sb.AppendLine($"{rows.Count} yazı · {bad} ihlal{(overflow ? " · TAŞMA" : "")}");
            foreach (var (_, line) in rows.OrderBy(r => r.px)) sb.AppendLine(line);
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(LiveBookReportPath));
            File.AppendAllText(LiveBookReportPath, sb.ToString());
            return string.Join(" · ", minima.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value:0.0} px")) + $" · {bad} ihlal · açılım {book.Spread + 1} · {LiveBookReportPath}";
        }

        // ================================================================ kapı ve kabul (Revizyon 3 · Aşama 6, PLAN §5.3, E §6)

        /// <summary>Kabul kâğıtlarının adının eşiği (belgedeki ad; PLAN §5.3), öteki kâğıt yazıları <see cref="Minimum"/>.</summary>
        public const float AdmissionNameMinimum = 18;
        /// <summary>Oyunda (Play) kabul kâğıtlarının ölçüsü (<see cref="ScanLiveAdmission"/>).</summary>
        public const string LiveAdmissionReportPath = "Temp/punto_kabul_live.txt";
        /// <summary>Kâğıtların okunduğu poz (K8R).</summary>
        public static readonly string AdmissionReadPoseName = SiginakPose.AdmissionRead.ToString();

        /// <summary>
        /// K8R okuma pozu, kâğıdın kendi çerçevesinde (akışın <c>AdmissionStation.ReadPose</c> hesabı): tezgâhta yatan kâğıtta
        /// (<paramref name="flat"/>; belge, bulgu fişi) kamera kâğıt merkezinden tezgâh yereli (0; 0,4; −0,1) yönünde <paramref name="distance"/>
        /// geride (eğim 75,96°, ekranın üstü duvara), dik kâğıtta (şerit, ESKİ, GİDEN yaprağı, karbon) yüz normali boyunca. Kâğıt kökü
        /// (merkez) <paramref name="paper"/>; ön yüz −z, yatan kâğıt Euler(90, 0, 0) ile yüzü yukarı konmuş olmalı.
        /// </summary>
        public static Pose AdmissionReadPose(Transform paper, bool flat, float distance)
        {
            var centre = paper.position;
            if (flat)
            {
                // Tezgâh çerçevesi: yukarı +y, duvar +z (kâğıdın üstü)
                var dir = AdmissionStation.CounterReadDirection.normalized;
                var pos = centre + dir * distance;
                return new Pose(pos, Quaternion.LookRotation(centre - pos, Vector3.forward));
            }
            var normal = paper.rotation * Vector3.back;
            var at = centre + normal * distance;
            return new Pose(at, Quaternion.LookRotation(-normal, paper.up));
        }

        static Object RefAt(Object owner, string field, int index)
        {
            if (owner == null) return null;
            var p = new SerializedObject(owner).FindProperty(field);
            if (p == null || !p.isArray || index >= p.arraySize) return null;
            var e = p.GetArrayElementAtIndex(index);
            return e.propertyType == SerializedPropertyType.ObjectReference ? e.objectReferenceValue : null;
        }

        /// <summary>
        /// Sahnedeki kabul kâğıtlarının kökleri (istasyonun ve defterin bağları); bağ yoksa W3 prefab'ı. Tür: belge, şerit, ESKİ, fiş, talep,
        /// karbon; <c>flat</c> tezgâhta yatanlar.
        /// </summary>
        static List<(string kind, GameObject source, bool flat)> AdmissionSources(Scene scene)
        {
            var list = new List<(string, GameObject, bool)>();
            var station = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AdmissionStation>(true)).FirstOrDefault();
            var pad = station != null ? Ref(station, "pad") as RequestPadTool : null;
            GameObject Of(Object o) => o is Transform t && t != null ? t.gameObject : null;
            GameObject Prefab(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
            list.Add(("Belge", Of(Ref(station, "documentPaper")) ?? Prefab(AdmissionFixtures.DocumentPrefab), true));
            list.Add(("Şerit", Of(Ref(station, "stripPaper")) ?? Prefab(AdmissionFixtures.StripPrefab), false));
            list.Add(("ESKİ", Of(RefAt(station, "archivePapers", 0)) ?? Prefab(AdmissionFixtures.StripPrefab), false));
            list.Add(("Fiş", Of(RefAt(station, "slipPapers", 0)) ?? Prefab(AdmissionFixtures.SlipPrefab), true));
            list.Add(("Talep", Of(Ref(pad, "sheetPaper")) ?? Prefab(AdmissionFixtures.RequestPrefab), false));
            list.Add(("Karbon", Of(Ref(pad, "carbonPaper")) ?? Prefab(AdmissionFixtures.RequestPrefab), false));
            return list.Where(x => x.Item2 != null).ToList();
        }

        /// <summary>Kopyanın sabit etiketleri (<see cref="DeskLocText"/>) o anki çekirdek dilinde (editörde bileşen yazmaz).</summary>
        internal static void TranslateLabels(GameObject root)
        {
            foreach (var label in root.GetComponentsInChildren<DeskLocText>(true))
            {
                var t = label.GetComponent<TMP_Text>();
                if (t != null && !string.IsNullOrEmpty(label.Source)) t.text = Corrigible.Core.Loc.T(label.Source);
            }
        }

        /// <summary>
        /// Kabul kâğıtları K8R okuma pozunda (PLAN §5.3, E §6.1–6.2; <see cref="AdmissionReadPose"/>): sahnedeki kâğıtlar (yoksa W3 prefab'ları)
        /// önizleme sahnesine kopyalanır, <see cref="AdmissionFixtures.ScanCases"/>'in her durumuyla (<paramref name="english"/> dili) bağlanır
        /// ve K8R kamerasıyla (rig'in yuvasının FOV'u, yoksa <c>cameras.K8R.fovV</c>; sensör rig'in) çizilen puntoyla, dört köşe kadrajda
        /// ölçülür: belgedeki ad ≥ 18 px, öteki görünür yazılar ≥ 14 px. Otomatik punto, ışıklı ya da başka font malzemesi (Aşama 4 bulgusu)
        /// ve kâğıt taşması (<see cref="AdmissionPaperFace.Overflowing"/>) eşikten bağımsız ihlaldir. Sahneyi değiştirmez.
        /// </summary>
        public static List<Entry> ScanAdmissionPapers(Scene scene, bool english)
        {
            var list = new List<Entry>();
            var rig = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).FirstOrDefault();
            if (rig == null) return list;
            var sources = AdmissionSources(scene);
            if (sources.Count == 0) return list;
            var theme = SiginakTheme.Active;
            float distance = theme != null ? theme.NumberOf("admission.readDistanceM", 0.318f) : 0.318f;
            float fov = rig.TryGetPose(SiginakPose.AdmissionRead, out var slot) ? slot.fieldOfView : theme != null ? theme.NumberOf("cameras.K8R.fovV", 40) : 40;
            var sensor = new SerializedObject(rig).FindProperty("sensorSizeMm").vector2Value;

            string previous = Corrigible.Core.Loc.Lang;
            var preview = EditorSceneManager.NewPreviewScene();
            var cameras = new List<(string, Camera)>();
            try
            {
                UseLanguage(english ? Corrigible.Core.Loc.English : Corrigible.Core.Loc.Turkish);
                string tag = english ? "EN" : "TR";
                var cases = AdmissionFixtures.ScanCases();
                foreach (var (kind, source, flat) in sources)
                {
                    var go = Object.Instantiate(source);
                    foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(go, preview);
                    if (!go.activeSelf) go.SetActive(true);
                    go.transform.SetPositionAndRotation(Vector3.zero, flat ? Quaternion.Euler(90, 0, 0) : Quaternion.identity);
                    go.transform.localScale = source.transform.lossyScale;
                    TranslateLabels(go);
                    var face = go.GetComponentInChildren<AdmissionPaperFace>(true);
                    if (face == null) continue;
                    var read = AdmissionReadPose(go.transform, flat, distance);
                    var cam = MakeCamera("Scan camera K8R " + kind, sensor, fov, 0, read.position, read.rotation);
                    var cams = new List<(string, Camera)> { (AdmissionReadPoseName, cam) };
                    cameras.Add((kind, cam));
                    foreach (var (caseName, s) in cases)
                        foreach (var (label, bound) in BindAdmission(kind, face, s))
                        {
                            if (!bound) continue;
                            Canvas.ForceUpdateCanvases();
                            list.AddRange(MeasureAdmissionFace(face, cams, "Admission/" + caseName + "/" + kind + label, " · okuma · " + tag));
                        }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                Release(cameras);
                UseLanguage(previous);
            }
            return list;
        }

        /// <summary>Kâğıt türüne göre durumun verisi (fişte her fiş ayrı, talepte gönderilmişse); etiket yol eki.</summary>
        static IEnumerable<(string label, bool bound)> BindAdmission(string kind, AdmissionPaperFace face, AdmissionSnapshot s)
        {
            switch (kind)
            {
                case "Belge":
                    if (face is AdmissionDocumentFace doc && s.Document != null) { doc.Bind(s.Document); doc.ShowImpression(false, false); yield return ("", true); }
                    break;
                case "Şerit":
                    if (face is OrdersStripFace strip && s.Orders != null) { strip.Bind(s.Orders); yield return ("/v" + s.Orders.Version, true); }
                    break;
                case "ESKİ":
                    if (face is OrdersStripFace head && s.Archive.Count > 0) { head.BindHead(s.Archive[0]); yield return ("/v" + s.Archive[0].Version, true); }
                    break;
                case "Fiş":
                    if (face is FindingSlipFace slip)
                        foreach (var d in s.Slips.Where(x => x != null))
                        {
                            slip.Bind(d);
                            yield return ("/" + (d.Review ? "inceleme" : d.Tool?.ToString() ?? "?") + "-" + d.Kind, true);
                        }
                    break;
                case "Talep":
                case "Karbon":
                    if (face is RequestFormFace form && s.Request != null && s.Request.Sent) { form.Bind(s.Request, kind == "Karbon"); yield return ("", true); }
                    break;
            }
        }

        static List<Entry> MeasureAdmissionFace(AdmissionPaperFace face, List<(string, Camera)> cams, string prefix, string poseSuffix)
        {
            var list = new List<Entry>();
            var name = face is AdmissionDocumentFace doc ? doc.NameText : null;
            bool first = true;
            foreach (var t in face.GetComponentsInChildren<TMP_Text>(false))
            {
                if (t == null || !t.enabled || !t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text)) continue;
                var (px, pose) = MeasureWorld(t, cams, true, true);
                string problem = t.enableAutoSizing ? "otomatik punto (kâğıt yazısı küçülmez)"
                    : t.font != null && t.fontSharedMaterial != t.font.material ? "ışıklı ya da başka font malzemesi (fontun kendi ışıksız malzemesi olmalı)"
                    : null;
                if (problem == null && first && face.Overflowing) problem = "taşma: içerik " + face.ContentBottomMm.ToString("0.0") + " mm";
                first = false;
                list.Add(new Entry
                {
                    Path = prefix + "/" + RelativePath(face.transform, t.transform), Text = Short(t.text), Pixels = px, Pose = pose + poseSuffix,
                    Minimum = t == name ? AdmissionNameMinimum : Minimum, Problem = problem,
                });
            }
            return list;
        }

        /// <summary>
        /// Oyunda (Play) kabul kâğıtlarının ölçüsü (E §6 canlı): <paramref name="cam"/> odanın kamerası (K8R'de bir kâğıda bakarken ya da
        /// K8A'da), dört köşesi kadrajda olan bütün etkin kâğıt yazıları, çizilen puntoyla, 1080p'ye ölçeklenmiş; belgedeki ad ≥ 18 px, öteki
        /// ≥ 14 px. Sonuç <see cref="LiveAdmissionReportPath"/>'e eklenir (her çağrı bir blok: dil ve poz başlıkta); dönen satır sınıfların en
        /// küçükleri. TR ve EN için dili değiştirip yeniden çağır.
        /// </summary>
        public static string ScanLiveAdmission(Camera cam)
        {
            if (cam == null) return "kamera yok";
            float norm = 1080f / Mathf.Max(1, cam.pixelHeight);
            var cams = new[] { ("Live", cam) };
            var rows = new List<(float px, string line)>();
            var minima = new Dictionary<string, float>();
            int bad = 0, overflow = 0;
            foreach (var face in Object.FindObjectsByType<AdmissionPaperFace>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!face.isActiveAndEnabled) continue;
                if (face.Overflowing) overflow++;
                var name = face is AdmissionDocumentFace doc ? doc.NameText : null;
                foreach (var t in face.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (t == null || !t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) continue;
                    var (raw, _) = MeasureWorld(t, cams, true, true);
                    if (raw <= 0) continue;   // kadraj dışı: bu pozda okunmuyor
                    float px = raw * norm;
                    bool isName = t == name;
                    string cls = isName ? "ad" : "öteki";
                    float min = isName ? AdmissionNameMinimum : Minimum;
                    if (!minima.TryGetValue(cls, out var m) || px < m) minima[cls] = px;
                    bool lit = t.font != null && t.fontSharedMaterial != t.font.material;
                    bool violation = px < min || t.enableAutoSizing || lit;
                    if (violation) bad++;
                    rows.Add((px, $"{px,5:0.0}/{min:0} px · {cls} · {face.GetType().Name} · {RelativePath(face.transform, t.transform)} · {Short(t.text)}{(violation ? " · İHLAL" + (lit ? " (ışıklı malzeme)" : "") : "")}"));
                }
            }
            var rig = Object.FindFirstObjectByType<SiginakCameraRig>(FindObjectsInactive.Exclude);
            var sb = new StringBuilder();
            sb.AppendLine($"Kabul kâğıtları, canlı · {System.DateTime.Now:yyyy-MM-dd HH:mm} · {cam.pixelWidth}×{cam.pixelHeight} → 1080p · dil {Corrigible.Core.Loc.Lang} · poz {(rig != null ? rig.Current.ToString() : "?")} · ad ≥ {AdmissionNameMinimum}, öteki ≥ {Minimum}");
            foreach (var kv in minima.OrderBy(k => k.Key)) sb.AppendLine($"en küçük {kv.Key}: {kv.Value:0.0} px");
            sb.AppendLine($"{rows.Count} yazı kadrajda · {bad} ihlal{(overflow > 0 ? " · " + overflow + " kâğıtta TAŞMA" : "")}");
            foreach (var (_, line) in rows.OrderBy(r => r.px)) sb.AppendLine(line);
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(LiveAdmissionReportPath));
            File.AppendAllText(LiveAdmissionReportPath, sb.ToString());
            return string.Join(" · ", minima.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value:0.0} px")) + $" · {rows.Count} yazı · {bad} ihlal · {LiveAdmissionReportPath}";
        }

        // ================================================================ kart yüzleri (PLAN §5.3, E §3.2-6/7)

        /// <summary>Kart yüzünün bir yazısı: sınıfı (ad, değer, küçük, diğer), eşiği ve küçükse gerekçesi.</summary>
        public struct CardField
        {
            public TMP_Text Text;
            public string Class, Reason;
            public float Minimum;
        }

        /// <summary>
        /// Yüzün ölçülecek yazıları, sınıfları <see cref="CardFace"/>'in kendi başvurularından (adlardan değil): ad 18, değerler 14,
        /// küçük yazılar yüzün gerekçesiyle, kalanlar 14.
        /// </summary>
        public static IEnumerable<CardField> FieldsOf(CardFace face)
        {
            var seen = new HashSet<TMP_Text>();
            if (face.NameText != null && seen.Add(face.NameText)) yield return new CardField { Text = face.NameText, Class = "ad", Minimum = CardNameMinimum };
            foreach (var t in face.ValueTexts)
                if (t != null && seen.Add(t)) yield return new CardField { Text = t, Class = "değer", Minimum = Minimum };
            foreach (var (t, why) in face.SmallTexts)
                if (t != null && seen.Add(t)) yield return new CardField { Text = t, Class = "küçük", Minimum = Minimum, Reason = why };
            foreach (var t in face.AllTexts)
                if (t != null && seen.Add(t)) yield return new CardField { Text = t, Class = "diğer", Minimum = Minimum };
        }

        static readonly (string kind, string prefab)[] CardPrefabs =
        {
            ("person", "PersonCard"), ("item", "ItemCard"), ("material", "MaterialCard"), ("chip", "ChipCard"), ("recipe", "RecipeCard"),
        };

        /// <summary>
        /// Kart prefab'larını en kötü örneklerle bağlayıp panonun yerlerine koyar ve K4'te ölçer (E §3.2-6): kişi <c>tek</c> cebinde
        /// (dört durum katmanı açık, seçili), arkasında üç eşya (yalnız görünen 14 mm bandın adı), tam eşya ilk kit yerinde, hammadde
        /// ilk stok yerinde, fiş ilk girdi yerinde, on kartlık açık el. Dil <c>Loc</c> ile geçici değişir (sahnedeki yazılar
        /// etkilenmez), sonunda eski dile döner. Prefab yoksa ya da pano bulunamazsa boş liste.
        /// </summary>
        public static List<Entry> ScanBoardCards(Scene scene, bool english)
        {
            var list = new List<Entry>();
            var rig = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).FirstOrDefault();
            var board = BoardFrame(scene);
            var prefabs = CardPrefabs.ToDictionary(p => p.kind, p => AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabDir + "/" + p.prefab + ".prefab"));
            if (rig == null || board == null || prefabs.Values.All(p => p == null)) return list;

            string previous = Corrigible.Core.Loc.Lang;
            var cameras = PoseCameras(rig, BoardPose);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                UseLanguage(english ? Corrigible.Core.Loc.English : Corrigible.Core.Loc.Turkish);
                var lang = english ? DeskLang.En : DeskLang.Tr;
                string tag = english ? "EN" : "TR";
                var placed = new List<(BoardCardView view, string place, bool bandOnly)>();

                BoardCardView Spawn(string kind, Vector2 mm, float level, float angle, string place, bool bandOnly = false)
                {
                    if (!prefabs.TryGetValue(kind, out var prefab) || prefab == null) return null;
                    var go = Object.Instantiate(prefab);
                    foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(go, preview);
                    var local = BoardLayout.MmToLocal(mm, BoardLayout.CardZ(level));
                    go.transform.SetPositionAndRotation(board.TransformPoint(local), board.rotation * Quaternion.Euler(0, 0, angle));
                    go.transform.localScale = Vector3.Scale(prefab.transform.localScale, board.lossyScale);
                    var view = go.GetComponent<BoardCardView>();
                    if (view == null) return null;
                    view.English = english;
                    placed.Add((view, place, bandOnly));
                    return view;
                }

                var samples = new CardSamples(lang);

                // kişi: tek cebi, bütün durumlar
                var centre = BoardLayout.CardCentre("tek");
                var person = Spawn("person", centre, BoardLayout.PocketLevel, 0, "Slot_tek");
                if (person != null)
                {
                    samples.Measure(person.Face as PersonCardFace);
                    person.Bind(samples.Person(0, states: true, lost: false));
                }
                // kişinin arkasında üç eşya (yalnız bant)
                for (int n = 0; n < 3; n++)
                {
                    var item = Spawn("item", centre + BoardLayout.ItemOffsetMm(n), BoardLayout.ItemLevel(n), 0, "ItemStrip_" + n, true);
                    if (item == null) continue;
                    samples.Measure(item.Face as ItemCardFace);
                    item.Bind(samples.Tucked(n));
                }
                // tam eşya: ilk kit yeri
                var full = Spawn("item", BoardLayout.KitCard(0, 0), BoardLayout.KitLevel(0), 0, "Kit_0");
                if (full != null)
                {
                    samples.Measure(full.Face as ItemCardFace);
                    full.Bind(samples.FullItem());
                }
                // hammadde: ilk stok yeri
                var material = Spawn("material", BoardLayout.Material(0), 1, 0, "Mat_metal");
                if (material != null)
                {
                    samples.Measure(material.Face as MaterialCardFace);
                    material.Bind(samples.Material());
                }
                // fiş: ilk girdi yeri
                var spot = BoardLayout.ChipSpot(0, out float chipAngle, out float chipLevel);
                var chip = Spawn("chip", spot, chipLevel, chipAngle, "Order_Inputs");
                if (chip != null)
                {
                    samples.Measure(chip.Face as ChipCardFace);
                    chip.Bind(samples.Chip());
                }
                // on kartlık açık el (Bosta_HandLeft'ten, adım 70,67 mm)
                const int Hand = 10;
                for (int j = 0; j < Hand; j++)
                {
                    var card = Spawn("person", BoardLayout.HandOpen(j, Hand), BoardLayout.HandLevel(j, Hand), 0, "Hand_" + j);
                    if (card == null) continue;
                    samples.Measure(card.Face as PersonCardFace);
                    card.Bind(samples.Person(j + 1, states: false, lost: j == 0));
                }
                // tarif kartı (Aşama 5): iş emrinin TARİF kutusunda, en geniş ad + en geniş girdiler + iki vardiya ve iki kutu (biri dolu)
                var recipe = Spawn("recipe", BoardLayout.Recipe, BoardLayout.RecipeLevel, 0, "Order_Recipe");
                if (recipe != null && recipe.Face is RecipeCardFace recipeFace)
                    recipe.Bind(CardSamples.Recipe(recipeFace, ProductionFixtures.BookSnapshot(english)));
                // YENİ etiketi (Aşama 5): zırh yığınının üst kartında (sahnedeki etiketin kopyası)
                NewItemTag newTag = null;
                var sceneTag = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NewItemTag>(true)).FirstOrDefault();
                var armorCard = sceneTag != null ? Spawn("item", BoardLayout.KitCard(3, 0), BoardLayout.KitLevel(0), 0, "Kit_3") : null;
                if (armorCard != null)
                {
                    samples.Measure(armorCard.Face as ItemCardFace);
                    armorCard.Bind(samples.FullItem());
                    var tagGo = Object.Instantiate(sceneTag.gameObject);
                    foreach (var tr in tagGo.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(tagGo, preview);
                    tagGo.transform.localScale = sceneTag.transform.lossyScale;
                    newTag = tagGo.GetComponent<NewItemTag>();
                    newTag.SetText(Corrigible.Core.Loc.T("YENİ"));
                    newTag.Show(armorCard.transform, true, false);
                }

                Canvas.ForceUpdateCanvases();
                var shelf = cameras.Select(c => (c.Item1, c.Item2)).ToList();
                foreach (var (view, place, bandOnly) in placed)
                {
                    var face = view.Face;
                    if (face == null) continue;
                    string kind = face.Kind.ToString().ToLowerInvariant();
                    var singles = new Dictionary<TMP_Text, float>();
                    foreach (var (text, maxWidthMm) in face.SingleLineFields) if (text != null) singles[text] = maxWidthMm;
                    var item = face as ItemCardFace;
                    foreach (var field in FieldsOf(face))
                    {
                        var t = field.Text;
                        if (!t.gameObject.activeInHierarchy || !t.enabled || string.IsNullOrEmpty(t.text)) continue;
                        if (bandOnly && (item == null || t != item.ItemName)) continue;
                        var (px, pose) = MeasureWorld(t, shelf, true, true);
                        string problem = null;
                        if (t.enableAutoSizing) problem = "otomatik punto (kart yazısı küçülmez)";
                        else if (singles.TryGetValue(t, out float max)) problem = Overflow(t, max);
                        else if (item != null && t == item.UseLine && t.textInfo != null && t.textInfo.lineCount > item.MaxUseLines)
                            problem = $"taşma: {t.textInfo.lineCount} satır > {item.MaxUseLines}";
                        string path = "Card/" + kind + "/" + RelativePath(view.transform, t.transform);
                        list.Add(new Entry
                        {
                            Path = path, Text = Short(t.text), Pixels = px, Pose = pose + " · " + place + " · " + tag, Minimum = field.Minimum,
                            Exception = px < field.Minimum ? field.Reason : null, Problem = problem,
                        });
                    }
                }
                // YENİ etiketi: sözcük ≥ 14 px (durumu taşıyan tek sözcük, D6-A), otomatik punto yok, levhaya 1 mm payla sığar
                var word = newTag != null ? newTag.Word : null;
                if (word != null && word.gameObject.activeInHierarchy && !string.IsNullOrEmpty(word.text))
                {
                    var (px, pose) = MeasureWorld(word, shelf, true, true);
                    string problem = word.enableAutoSizing ? "otomatik punto (YENİ küçülmez)" : Overflow(word, NewItemTag.SizeMm.x - 2f);
                    list.Add(new Entry
                    {
                        Path = "Card/yeni/" + RelativePath(newTag.transform, word.transform), Text = Short(word.text), Pixels = px,
                        Pose = pose + " · Kit_3 · " + tag, Minimum = Minimum, Problem = problem,
                    });
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                Release(cameras);
                UseLanguage(previous);
            }
            return list;
        }

        /// <summary>Tek satırlık alan: bir satır, taşma yok ve genişlik sınırın içinde (mm, yazının biriminde).</summary>
        static string Overflow(TMP_Text t, float maxWidth)
        {
            t.ForceMeshUpdate(true, true);
            var info = t.textInfo;
            if (info != null && info.lineCount > 1) return $"taşma: {info.lineCount} satır (tek satırlık alan)";
            if (t.isTextOverflowing) return "taşma: metin kutusundan taşıyor";
            float w = t.GetPreferredValues(t.text, float.PositiveInfinity, float.PositiveInfinity).x;
            return w > maxWidth + 0.05f ? $"taşma: {w:0.0} > {maxWidth:0.0}" : null;
        }

        static string RelativePath(Transform root, Transform t)
        {
            var parts = new List<string>();
            for (var p = t; p != null && p != root; p = p.parent) parts.Add(p.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>Panonun model çerçevesi: <see cref="BoardView3D"/>'nin nesnesi ya da <c>SiginakRoom/Planning board</c>.</summary>
        static Transform BoardFrame(Scene scene)
        {
            var view = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BoardView3D>(true)).FirstOrDefault();
            return view != null ? view.transform : Find(scene, "SiginakRoom/Planning board");
        }

        /// <summary>Çekirdek dili tablosuyla değiştirir; masanın dil olayı tetiklenmez (sahnedeki DeskLocText'ler yeniden yazılmaz).</summary>
        internal static void UseLanguage(string code)
        {
            if (code == Corrigible.Core.Loc.Turkish) { Corrigible.Core.Loc.SetLanguage(Corrigible.Core.Loc.Turkish); return; }
            var asset = Resources.Load<TextAsset>("Localization/strings");
            var tables = asset != null ? Corrigible.Core.Loc.ParseTable(asset.text) : new Dictionary<string, Dictionary<string, string>>();
            tables.TryGetValue(code, out var table);
            Corrigible.Core.Loc.SetLanguage(code, table);
        }

        /// <summary>
        /// En kötü örnekler (E §3.5): oyunun gerçek kaynak dizgileri (Core ve anlık görüntü kurucusunun <c>Loc</c> kaynakları), her
        /// alan için yüzün kendi yazısıyla ölçülmüş en geniş aday. Ad listesi Core'un kurucu ve başvuru listeleridir (çevrilmez).
        /// </summary>
        sealed class CardSamples
        {
            static readonly string[] Names =
            {
                "Derya", "Selin", "İpek", "Deniz", "Nermin", "Zeynep", "Orhan", "Cem", "Eren", "Suna", "Rauf", "Meral", "Arda", "Ece", "Murat",
                "Aylin", "Baran", "Banu", "Ozan", "Lale", "Sinan", "Asli", "Kerem", "Sema", "Tamer", "Dilan", "Yalcin", "Feride", "Umut", "Ayca",
                "Levent", "Naz",
            };
            static readonly string[] Professions =
            {
                "Elektronikçi", "Elektrikçi", "Haberleşmeci", "Tamirci", "Aşçı", "Sağlık çalışanı", "Depo görevlisi",
            };
            static readonly string[] Topics = { "Teknisyen", "EMP", "Analiz", "Üretim", "Mutfak", "Bakım", "İkmal" };
            static readonly int[] Tiers = { 3, 2, 1, 0, 3, 2, 1 };

            readonly DeskLang lang;
            string[] names, jobs, strips, letters;
            string[] kitNames, aidNames, allItemNames;
            string materialName;
            bool personMeasured, itemMeasured, materialMeasured, chipMeasured;

            public CardSamples(DeskLang lang)
            {
                this.lang = lang;
                names = Names;
                jobs = Professions.Select(Corrigible.Core.Loc.T).ToArray();
                var s = new List<string>();
                foreach (var topic in Topics) s.Add(Corrigible.Core.Loc.F("{0} +{1}", Corrigible.Core.Loc.T(topic), 2));
                s.Add(Corrigible.Core.Loc.F("Yatıyor · {0} v.", 3));
                s.Add(Corrigible.Core.Loc.F("Kısıtlı · {0} v.", 3));
                s.Add(Corrigible.Core.Loc.T("Kısıtlı · bitti"));
                s.Add(Corrigible.Core.Loc.F("Kayıp · V.{0:000}", 31));
                s.Add(Corrigible.Core.Loc.F("Boşta · +{0} can", 1));
                foreach (var job in new[] { "tek", "emp", "ana", "craft", "cook" }) s.Add(Corrigible.Core.PlanningBoardData.JobName(job));
                strips = s.ToArray();
                letters = Corrigible.Core.Loc.T(Corrigible.Core.Loc.N("T E A Ü M B İ")).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                kitNames = new[] { "kit_tek", "kit_emp", "kit_ana" }.Select(Corrigible.Core.PlanningBoardData.EquipmentName).ToArray();
                aidNames = new[] { "medkit", "emp_cell" }.Select(Corrigible.Core.PlanningBoardData.EquipmentName).ToArray();
                allItemNames = BoardLayout.KitKinds.Select(Corrigible.Core.PlanningBoardData.EquipmentName).ToArray();
                materialName = DeskText.Upper(Corrigible.Core.PlanningBoardData.MaterialName("kimyasal"), lang);
            }

            static string[] ByWidth(TMP_Text t, IEnumerable<string> candidates)
            {
                var list = candidates.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                if (t == null || t.font == null) return list.ToArray();
                return list.OrderByDescending(c => t.GetPreferredValues(c, float.PositiveInfinity, float.PositiveInfinity).x).ToArray();
            }

            static TMP_Text TextRef(Object face, string field) => Ref(face, field) as TMP_Text;

            /// <summary>Adayları ilk kartın yazılarıyla genişliğe göre sıralar (en genişi önce).</summary>
            public void Measure(PersonCardFace face)
            {
                if (face == null || personMeasured) return;
                personMeasured = true;
                names = ByWidth(face.NameText, Names);
                jobs = ByWidth(TextRef(face, "jobText"), jobs);
                strips = ByWidth(TextRef(face, "stripText"), strips);
            }

            public void Measure(ItemCardFace face)
            {
                if (face == null || itemMeasured) return;
                itemMeasured = true;
                kitNames = ByWidth(face.ItemName, kitNames);
                aidNames = ByWidth(face.ItemName, aidNames);
                allItemNames = ByWidth(face.ItemName, allItemNames);
            }

            public void Measure(MaterialCardFace face)
            {
                if (face == null || materialMeasured) return;
                materialMeasured = true;
                var all = BoardLayout.MaterialKinds.Select(k => DeskText.Upper(Corrigible.Core.PlanningBoardData.MaterialName(k), lang));
                materialName = ByWidth(TextRef(face, "nameText"), all).FirstOrDefault() ?? materialName;
            }

            public void Measure(ChipCardFace face)
            {
                if (face == null || chipMeasured) return;
                chipMeasured = true;
                var all = BoardLayout.MaterialKinds.Select(k => DeskText.Upper(Corrigible.Core.PlanningBoardData.MaterialName(k), lang));
                chipName = ByWidth(TextRef(face, "nameText"), all).FirstOrDefault();
            }

            string chipName;

            static T Pick<T>(T[] a, int i) => a.Length == 0 ? default : a[i % a.Length];

            /// <summary><paramref name="i"/>. örnek kişi (0 = en geniş her şey). Durumlar: cepteki kişide hepsi açık; elde birer birer.</summary>
            public PersonCardData Person(int i, bool states, bool lost)
            {
                var d = new PersonCardData
                {
                    Id = "tarama-" + i, Name = Pick(names, i), Job = Pick(jobs, i), JobId = states ? "tek" : "rest", Strip = Pick(strips, i),
                    StripTone = StripTone.Ink, Hurt = states || i % 5 == 3,
                };
                for (int k = 0; k < d.MasteryLetters.Length; k++)
                {
                    d.MasteryLetters[k] = k < letters.Length ? letters[k] : "?";
                    d.MasteryTiers[k] = Tiers[(k + i) % Tiers.Length];
                }
                d.StatLabels[0] = Corrigible.Core.Loc.T("CAN");
                d.StatLabels[1] = Corrigible.Core.Loc.T("HAR");
                d.StatLabels[2] = Corrigible.Core.Loc.T("GÜÇ");
                d.StatValues[0] = Corrigible.Core.Loc.F("{0}/{1}", d.Hurt ? 2 : 3, 3);
                d.StatValues[1] = "3";
                d.StatValues[2] = "1";
                if (lost)
                {
                    d.Lost = true;
                    d.Strip = Corrigible.Core.Loc.F("Kayıp · V.{0:000}", 31);
                }
                else if (states)
                {
                    d.Injured = d.Bedridden = d.Restricted = d.Review = d.Selected = true;
                }
                else
                {
                    d.Injured = i % 5 == 3;
                    d.Bedridden = i % 5 == 1;
                    d.Restricted = i % 5 == 2;
                    d.Review = i % 5 == 4;
                }
                d.HoverTitle = d.Name;
                d.HoverLine = d.Strip;
                return d;
            }

            static string UseLineOf(string kind)
            {
                switch (kind)
                {
                    case "armor": return Corrigible.Core.Loc.T("ilk hasarı karşılar, kırılır");
                    case "medkit": return Corrigible.Core.Loc.F("bakım görevinde +{0} can; panoda bakım cebi yok", 1);
                    case "emp_cell": return Corrigible.Core.Loc.F("EMP uzmanı: ilk {0} atış +{1} hasar; baskında tükenir", 2, 1);
                    default: return Corrigible.Core.Loc.F("saha rolü: {0}", Corrigible.Core.PlanningBoardData.JobName(kind.Length > 4 ? kind.Substring(4) : kind));
                }
            }

            static ItemSlotKind SlotOf(string kind) => kind.StartsWith("kit_", System.StringComparison.Ordinal) ? ItemSlotKind.Kit
                : kind == "armor" ? ItemSlotKind.Armor : ItemSlotKind.Aid;

            static string SlotWord(ItemSlotKind slot) => slot == ItemSlotKind.Kit ? Corrigible.Core.Loc.T("KİT")
                : slot == ItemSlotKind.Armor ? Corrigible.Core.Loc.T("ZIRH") : Corrigible.Core.Loc.T("YARDIMCI");

            static string KindOfName(string name)
            {
                foreach (var kind in BoardLayout.KitKinds) if (Corrigible.Core.PlanningBoardData.EquipmentName(kind) == name) return kind;
                return "kit_tek";
            }

            ItemCardData Item(string name, string id)
            {
                string kind = KindOfName(name);
                var slot = SlotOf(kind);
                return new ItemCardData
                {
                    Id = id, Kind = kind, Name = name, Slot = slot, SlotLabel = SlotWord(slot), UseLine = UseLineOf(kind), OwnerId = "tarama-0",
                    HoverTitle = name, HoverLine = UseLineOf(kind),
                };
            }

            /// <summary>Kişinin arkasındaki n. eşya: kit, zırh, yardımcı (en geniş adla).</summary>
            public ItemCardData Tucked(int n) => n == 0 ? Item(Pick(kitNames, 0), "tarama-kit")
                : n == 1 ? Item(Corrigible.Core.PlanningBoardData.EquipmentName("armor"), "tarama-zirh") : Item(Pick(aidNames, 0), "tarama-yardimci");

            /// <summary>Tam görünen eşya: en geniş ad; kullanım satırı en uzun olan (EMP hücresi) adın türünden.</summary>
            public ItemCardData FullItem()
            {
                var d = Item(Pick(allItemNames, 0), "tarama-depo");
                string longest = BoardLayout.KitKinds.Select(UseLineOf).OrderByDescending(s => s.Length).First();
                d.UseLine = longest;
                d.OwnerId = null;
                return d;
            }

            public MaterialCardData Material() => new MaterialCardData
            {
                Kind = "kimyasal", Name = materialName, Stock = 23, Free = 20, Held = 3,
                StockLine = Corrigible.Core.Loc.F("stok {0}", 23), HeldLine = Corrigible.Core.Loc.F("ayrı {0}", 3), HoverTitle = materialName,
            };

            public ChipData Chip() => new ChipData { Id = "chip:kimyasal:0", Kind = "kimyasal", Name = chipName ?? materialName, Index = 0 };

            /// <summary>
            /// En kötü tarif kartı (E §5.3, E11): V.001'in tariflerinden yüzün kendi yazısıyla ölçülmüş en geniş ad, girdi adları en geniş
            /// tarifin girdileri, çok vardiyalı tarifin süre satırı ve iki ilerleme kutusu (biri dolu; D7-A), "→ 1 ürün".
            /// </summary>
            public static RecipeCardData Recipe(RecipeCardFace face, BoardSnapshot s)
            {
                var recipes = s.Recipes.Where(r => r != null).ToList();
                if (recipes.Count == 0) return new RecipeCardData { Id = "tarama-tarif", RecipeId = "armor" };
                float Width(TMP_Text t, string text) => t != null && t.font != null && !string.IsNullOrEmpty(text)
                    ? t.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x : (text ?? "").Length;
                var nameT = face.NameText;
                var inputT = face.InputNames.Count > 0 ? face.InputNames[0] : null;
                var widest = recipes.OrderByDescending(r => Width(nameT, r.Name)).First();
                var inputs = recipes.OrderByDescending(r => r.Inputs.Sum(i => i != null ? Width(inputT, i.Name) : 0)).First();
                var multi = recipes.Where(r => r.Turns > 1).OrderByDescending(r => Width(face.TurnsText, r.TurnsLine)).FirstOrDefault() ?? widest;
                var d = new RecipeCardData
                {
                    Id = "tarama-tarif", RecipeId = widest.Id, Name = widest.Name, IconKey = widest.Id, TurnsLine = multi.TurnsLine,
                    OutLine = Corrigible.Core.Loc.T("→ 1 ürün"), NeedsLabel = s.Book != null ? s.Book.Input : null,
                    Turns = Mathf.Max(2, multi.Turns), Done = Mathf.Max(1, multi.Turns - 1),
                };
                foreach (var i in inputs.Inputs) if (i != null) d.Inputs.Add(i);
                return d;
            }
        }

        /// <summary>
        /// Oyunda (Play) K4'teki gerçek kartların ölçüsü (E §3.2-8): <paramref name="cam"/> rig kamerası (yakınlık 1, bakış 0), her
        /// etkin kart yazısı kendi yerinde, çizilen puntoyla, 1080p'ye ölçeklenmiş. Sonuç <see cref="LiveReportPath"/>'e
        /// (<c>px · tür · kimlik · alan · metin</c>); dönen satır sınıfların en küçükleridir (ad ≥ 18, değerler ≥ 14).
        /// </summary>
        public static string ScanLive(Camera cam)
        {
            if (cam == null) return "kamera yok";
            float norm = 1080f / Mathf.Max(1, cam.pixelHeight);
            var rows = new List<(float px, string line)>();
            var minima = new Dictionary<string, float>();
            int bad = 0;
            var cams = new[] { ("Live", cam) };
            foreach (var view in Object.FindObjectsByType<BoardCardView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var face = view.Face;
                if (face == null || !view.isActiveAndEnabled) continue;
                foreach (var field in FieldsOf(face))
                {
                    var t = field.Text;
                    if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) continue;
                    var (raw, _) = MeasureWorld(t, cams, true, true);
                    float px = raw * norm;
                    if (!minima.TryGetValue(field.Class, out var m) || px < m) minima[field.Class] = px;
                    bool violation = field.Reason == null && px < field.Minimum || t.enableAutoSizing;
                    if (violation) bad++;
                    rows.Add((px, $"{px,5:0.0} · {face.Kind} · {view.Id} · {RelativePath(view.transform, t.transform)} · {Short(t.text)}{(violation ? " · İHLAL" : "")}"));
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine($"K4 canlı kart taraması · {System.DateTime.Now:yyyy-MM-dd HH:mm} · {cam.pixelWidth}×{cam.pixelHeight} → 1080p · ad ≥ {CardNameMinimum}, değer ≥ {Minimum}");
            foreach (var kv in minima.OrderBy(k => k.Key)) sb.AppendLine($"en küçük {kv.Key}: {kv.Value:0.0} px");
            sb.AppendLine($"{rows.Count} yazı · {bad} ihlal");
            sb.AppendLine();
            foreach (var (_, line) in rows.OrderBy(r => r.px)) sb.AppendLine(line);
            Directory.CreateDirectory(Path.GetDirectoryName(LiveReportPath));
            File.WriteAllText(LiveReportPath, sb.ToString());
            return string.Join(" · ", minima.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value:0.0} px")) + $" · {bad} ihlal · {LiveReportPath}";
        }

        // ================================================================ vardiya kapanışı (Revizyon 3 · Aşama 4, PLAN §5.3)

        /// <summary>
        /// Devir fişi elde ve şalter plakasının 2. satırı (E §5.2–5.3), <paramref name="english"/> diline göre (dil <c>Loc</c> ile geçici
        /// değişir, sahnedeki yazılar yeniden yazılmaz; sonunda eski dile döner). Kaydedilmiş sahneyi değiştirmez: fiş ve plaka yazısı
        /// önizleme sahnesine kopyalanıp orada bağlanır. Fiş yüzü ya da plaka yoksa (sahne adımından önce) o bölüm boş döner.
        /// <list type="bullet">
        /// <item>Fiş: sahnedeki fişin yüzü (yoksa <see cref="HandoverSlipFixtures.PrefabPath"/>) K6 kamerasının önündeki el pozuna
        /// (<c>ShiftReceiptPaper.HandPose</c>; kâğıdın kendi <c>handHeightFraction / handOffset / handEuler</c> değerleriyle) konur,
        /// V001, V031 ve en kötü fişle bağlanır; adlar (<see cref="HandoverSlipFace.NameTexts"/>) ≥ 18 px, öteki görünür yazılar
        /// (<see cref="HandoverSlipFace.ValueTexts"/>) ≥ 14 px, çizilen puntoyla, dört köşesi kadrajda. Otomatik punto ve taşma
        /// (<see cref="HandoverSlipFace.Overflowing"/>) eşikten bağımsız ihlaldir.</item>
        /// <item>Plaka: <c>TXT_Plate2</c> her durumun ve her kilit nedeninin metniyle K6'da ve K1'de (iki ayrı satır, her pozda en küçük
        /// metin) ölçülür; otomatik punto, ikinci satıra kayma ve yuvadan taşma ihlaldir. K1'de eşik altı gerekçeli istisnadır.</item>
        /// </list>
        /// K6 pozu rig'de yoksa token kamerası (<c>cameras.K6.pos/target/fovV</c>) kullanılır.
        /// </summary>
        public static List<Entry> ScanShiftClose(Scene scene, bool english)
        {
            var list = new List<Entry>();
            var rig = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SiginakCameraRig>(true)).FirstOrDefault();
            if (rig == null) return list;
            string previous = Corrigible.Core.Loc.Lang;
            var k6 = ShiftCloseCameras(rig);
            var k1 = PoseCameras(rig, SiginakPose.Desk.ToString());
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                UseLanguage(english ? Corrigible.Core.Loc.English : Corrigible.Core.Loc.Turkish);
                string tag = english ? "EN" : "TR";
                list.AddRange(ScanSlipInHand(scene, preview, k6, tag));
                list.AddRange(ScanPlate2(scene, preview, k6, k1, tag));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                Release(k6);
                Release(k1);
                UseLanguage(previous);
            }
            return list;
        }

        /// <summary>K6 kamerası: rig'in <c>ShiftClose</c> yuvası, yoksa token (<c>cameras.K6</c>: konum, hedef, dikey FOV; lens kaydırması 0).</summary>
        static List<(string, Camera)> ShiftCloseCameras(SiginakCameraRig rig)
        {
            var list = PoseCameras(rig, ShiftClosePose);
            if (list.Count > 0) return list;
            var theme = SiginakTheme.Active;
            if (theme == null || !theme.TryNumber("cameras.K6.pos[0]", out _)) return list;
            var sensor = new SerializedObject(rig).FindProperty("sensorSizeMm").vector2Value;
            var pos = theme.Vector("cameras.K6.pos");
            var target = theme.Vector("cameras.K6.target");
            var rot = Quaternion.LookRotation(target - pos, Vector3.up);
            list.Add((ShiftClosePose + " (token)", MakeCamera("Scan camera K6 (token)", sensor, theme.NumberOf("cameras.K6.fovV", 42), 0, pos, rot)));
            return list;
        }

        /// <summary>Sahnedeki fişin yüzü ve kâğıdı (kâğıdın <c>face</c> bağı; yoksa sahnedeki ilk yüz, o da yoksa prefab).</summary>
        static (HandoverSlipFace face, ShiftReceiptPaper paper) SlipSource(Scene scene)
        {
            var papers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShiftReceiptPaper>(true)).ToList();
            foreach (var paper in papers)
                if (Ref(paper, "face") is HandoverSlipFace face && face != null) return (face, paper);
            var any = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HandoverSlipFace>(true)).FirstOrDefault();
            if (any != null) return (any, papers.FirstOrDefault());
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandoverSlipFixtures.PrefabPath);
            return (prefab != null ? prefab.GetComponent<HandoverSlipFace>() : null, papers.FirstOrDefault());
        }

        /// <summary>
        /// Kâğıdın el pozu K6 kamerasından: kâğıdın serileştirilmiş oranı, ofseti ve eğimi (yoksa varsayılanlar), fiş boyu token'dan
        /// (<c>shiftClose.slip.sizeMm[1]</c>). Oyundaki <c>ShiftReceiptPaper.HandTarget</c> ile aynı hesap.
        /// </summary>
        static Pose HandPoseFor(ShiftReceiptPaper paper, Camera cam)
        {
            float fraction = 0.67f;
            Vector2 offset = ShiftReceiptPaper.DefaultHandOffset;
            Vector3 euler = ShiftReceiptPaper.DefaultHandEuler;
            if (paper != null)
            {
                var so = new SerializedObject(paper);
                var p = so.FindProperty("handHeightFraction");
                if (p != null) fraction = p.floatValue;
                p = so.FindProperty("handOffset");
                if (p != null) offset = p.vector2Value;
                p = so.FindProperty("handEuler");
                if (p != null) euler = p.vector3Value;
            }
            var theme = SiginakTheme.Active;
            float height = (theme != null ? theme.NumberOf("shiftClose.slip.sizeMm[1]", 150) : 150) * 0.001f;
            return ShiftReceiptPaper.HandPose(cam.transform.position, cam.transform.rotation, cam.fieldOfView, fraction, height, offset, euler);
        }

        static List<Entry> ScanSlipInHand(Scene scene, Scene preview, List<(string, Camera)> cameras, string tag)
        {
            var list = new List<Entry>();
            if (cameras.Count == 0) return list;
            var (source, paper) = SlipSource(scene);
            if (source == null) return list;
            var cam = cameras[0].Item2;

            var go = Object.Instantiate(source.gameObject);
            foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(go, preview);
            if (!go.activeSelf) go.SetActive(true);
            var face = go.GetComponent<HandoverSlipFace>();
            if (face == null) return list;

            // Kâğıdın kökü el pozunda; yüz (HandoverSlipView örneği) kâğıdın altında kendi yerel dönüşümüyle (sahnede kimlik)
            var hand = HandPoseFor(paper, cam);
            var rel = paper != null && source.transform.IsChildOf(paper.transform)
                ? paper.transform.worldToLocalMatrix * source.transform.localToWorldMatrix
                : Matrix4x4.identity;
            var world = Matrix4x4.TRS(hand.position, hand.rotation, Vector3.one) * rel;
            go.transform.SetPositionAndRotation(world.GetColumn(3), world.rotation);
            go.transform.localScale = source.transform.lossyScale;

            foreach (var (name, facts) in HandoverSlipFixtures.All())
            {
                face.Bind(facts);
                Canvas.ForceUpdateCanvases();
                var names = new HashSet<TMP_Text>(face.NameTexts);
                bool first = true;
                foreach (var t in face.NameTexts.Concat(face.ValueTexts))
                {
                    if (t == null || !t.gameObject.activeInHierarchy || !t.enabled || string.IsNullOrEmpty(t.text)) continue;
                    var (px, pose) = MeasureWorld(t, cameras, true, true);
                    bool isName = names.Contains(t);
                    string problem = t.enableAutoSizing ? "otomatik punto (fiş yazısı küçülmez)" : null;
                    if (problem == null && first && face.Overflowing)
                        problem = $"taşma: içerik {face.ContentBottomMm:0.0} mm > alt blok {face.FooterTopMm:0.0} mm (D9-A)";
                    first = false;
                    list.Add(new Entry
                    {
                        Path = "Slip/" + name + "/" + RelativePath(go.transform, t.transform), Text = Short(t.text), Pixels = px,
                        Pose = pose + " · el · " + tag, Minimum = isName ? SlipNameMinimum : Minimum, Problem = problem,
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// Şalter plakasının 2. satırı (<see cref="ShiftPlate.Line2"/>). Plaka bileşeni yoksa (sahne adımından önce satırı R19 yazmaz)
        /// null: o zaman satır genel kuralla ölçülmeye devam eder.
        /// </summary>
        static TMP_Text Plate2Text(Scene scene) =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShiftPlate>(true)).FirstOrDefault(p => p.Line2 != null)?.Line2;

        /// <summary>Plakanın yazabileceği bütün metinler (o anki dilde): dört durum ve köprünün beş kilit nedeni.</summary>
        static IEnumerable<string> Plate2Candidates()
        {
            yield return ShiftPlate.TextFor(ShiftPhase.Ready, null);
            yield return ShiftPlate.TextFor(ShiftPhase.CoverUp, null);
            yield return ShiftPlate.TextFor(ShiftPhase.Waiting, null);
            yield return ShiftPlate.TextFor(ShiftPhase.Rejecting, null);
            yield return Corrigible.Desk.Bridge.SiginakShiftClose.LockReason(true, false, false, false, false, null).plate;
            yield return Corrigible.Desk.Bridge.SiginakShiftClose.LockReason(false, true, false, false, false, null).plate;
            yield return Corrigible.Desk.Bridge.SiginakShiftClose.LockReason(false, false, true, false, false, null).plate;
            yield return Corrigible.Desk.Bridge.SiginakShiftClose.LockReason(false, false, false, true, false, null).plate;
            yield return Corrigible.Desk.Bridge.SiginakShiftClose.LockReason(false, false, false, false, false, "plan").plate;
        }

        static List<Entry> ScanPlate2(Scene scene, Scene preview, List<(string, Camera)> k6, List<(string, Camera)> k1, string tag)
        {
            var list = new List<Entry>();
            var line = Plate2Text(scene);
            if (line == null) return list;
            var go = Object.Instantiate(line.gameObject);
            foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(go, preview);
            if (!go.activeSelf) go.SetActive(true);
            go.transform.SetPositionAndRotation(line.transform.position, line.transform.rotation);
            go.transform.localScale = line.transform.lossyScale;
            var t = go.GetComponent<TMP_Text>();
            if (t == null) return list;
            var texts = Plate2Candidates().Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            string path = PathOf(line.transform);
            foreach (var (poseName, cams) in new[] { ("K6", k6), ("K1", k1) })
            {
                if (cams.Count == 0) continue;
                float worst = float.MaxValue;
                string worstText = null, worstPose = OutOfFrame, problem = null;
                foreach (var text in texts)
                {
                    t.text = text;
                    var (px, pose) = MeasureWorld(t, cams, true, true);
                    if (px < worst) { worst = px; worstText = text; worstPose = pose; }
                    problem ??= Plate2Problem(t, text);
                }
                if (worstText == null) continue;
                list.Add(new Entry
                {
                    Path = path + " @" + poseName, Text = Short(worstText), Pixels = worst, Pose = worstPose + " · " + tag, Minimum = Minimum,
                    Exception = poseName == "K1" && worst < Minimum ? PlateK1Reason : null, Problem = problem,
                });
            }
            return list;
        }

        /// <summary>Plakanın 2. satırında eşikten bağımsız sorun: otomatik punto (PROMPT §0.4 "punto küçülmez"), satır kayması, yuvadan taşma.</summary>
        static string Plate2Problem(TMP_Text t, string text)
        {
            if (t.enableAutoSizing) return "otomatik punto (plakanın 2. satırı küçülmez)";
            t.ForceMeshUpdate(true, true);
            var info = t.textInfo;
            if (info != null && info.lineCount > 1) return $"taşma: “{Short(text)}” {info.lineCount} satır";
            float width = t.textBounds.size.x, slot = t.rectTransform.rect.width;
            return width > slot + 1e-4f ? $"taşma: “{Short(text)}” {width:0.000} > {slot:0.000}" : null;
        }

        /// <summary>
        /// Oyunda (Play) eldeki devir fişinin ölçüsü (E §5.2 canlı): <paramref name="cam"/> odanın kamerası (K6), fişin yüzü kâğıdın
        /// el ya da okuma durumundaysa; adlar ≥ 18 px, öteki yazılar ≥ 14 px, çizilen puntoyla, 1080p'ye ölçeklenmiş. Sonuç
        /// <see cref="LiveSlipReportPath"/>'e; dönen satır sınıfların en küçükleri ve yüzdeki yazı sayısı (kare süresi denetimi için).
        /// TR ve EN için dili değiştirip yeniden çağır (<c>DeskLoc.Instance.SetLanguage</c>; fiş yüzü dil değişince yeniden yazar).
        /// </summary>
        public static string ScanLiveSlip(Camera cam)
        {
            if (cam == null) return "kamera yok";
            HandoverSlipFace face = null;
            foreach (var paper in Object.FindObjectsByType<ShiftReceiptPaper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (paper.State != ShiftReceiptPaper.Where.Hand && paper.State != ShiftReceiptPaper.Where.Reading) continue;
                face = paper.GetComponentInChildren<HandoverSlipFace>();
                if (face != null) break;
            }
            if (face == null) return "fiş elde değil (dildeki fişe tıkla ya da EVRAK'tan son fişi aç)";
            float norm = 1080f / Mathf.Max(1, cam.pixelHeight);
            var cams = new[] { ("Live", cam) };
            var names = new HashSet<TMP_Text>(face.NameTexts);
            var rows = new List<(float px, string line)>();
            var minima = new Dictionary<string, float>();
            int bad = 0, texts = face.GetComponentsInChildren<TMP_Text>(false).Length;
            foreach (var t in face.NameTexts.Concat(face.ValueTexts))
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) continue;
                var (raw, _) = MeasureWorld(t, cams, true, true);
                float px = raw * norm;
                bool isName = names.Contains(t);
                string cls = isName ? "ad" : "öteki";
                float min = isName ? SlipNameMinimum : Minimum;
                if (!minima.TryGetValue(cls, out var m) || px < m) minima[cls] = px;
                bool violation = px < min || t.enableAutoSizing;
                if (violation) bad++;
                rows.Add((px, $"{px,5:0.0}/{min:0} px · {cls} · {RelativePath(face.transform, t.transform)} · {Short(t.text)}{(violation ? " · İHLAL" : "")}"));
            }
            var sb = new StringBuilder();
            sb.AppendLine($"Devir fişi elde, canlı · {System.DateTime.Now:yyyy-MM-dd HH:mm} · {cam.pixelWidth}×{cam.pixelHeight} → 1080p · dil {Corrigible.Core.Loc.Lang} · ad ≥ {SlipNameMinimum}, öteki ≥ {Minimum}");
            foreach (var kv in minima.OrderBy(k => k.Key)) sb.AppendLine($"en küçük {kv.Key}: {kv.Value:0.0} px");
            sb.AppendLine($"{rows.Count} yazı ölçüldü · yüzde {texts} etkin TMP · {bad} ihlal{(face.Overflowing ? " · TAŞMA" : "")}");
            sb.AppendLine();
            foreach (var (_, line) in rows.OrderBy(r => r.px)) sb.AppendLine(line);
            Directory.CreateDirectory(Path.GetDirectoryName(LiveSlipReportPath));
            File.WriteAllText(LiveSlipReportPath, sb.ToString());
            return string.Join(" · ", minima.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value:0.0} px")) + $" · {texts} TMP · {bad} ihlal · {LiveSlipReportPath}";
        }

        // ================================================================ menü ve canlı tarama (Revizyon 3 · Aşama 10, PLAN §1.2)

        /// <summary>
        /// Giris (menü): sahnenin bütün yazıları, <see cref="EntranceController"/>'ın çapalarından (M0 sahanlık, M2 saat, MS son şalter, M1 dolap) kurulan
        /// kameralarla, genel kurallarla ölçülür (Overlay ve dünya).
        /// Çapa eksikse o poz atlanır; denetleyici yoksa liste boş döner. Sahneyi değiştirmez.
        /// </summary>
        public static List<Entry> ScanEntrance(Scene scene)
        {
            var list = new List<Entry>();
            var roots = scene.GetRootGameObjects();
            var ctrl = roots.SelectMany(g => g.GetComponentsInChildren<Corrigible.Desk.Bridge.EntranceController>(true)).FirstOrDefault();
            if (ctrl == null) return list;
            var so = new SerializedObject(ctrl);
            float Fov(string field, float fallback) => so.FindProperty(field) is SerializedProperty p ? p.floatValue : fallback;
            var cabinet = Ref(ctrl, "cabinet") as SettingsCabinet;
            var anchors = new (string pose, Transform at, float fov)[]
            {
                ("M0", Ref(ctrl, "overview") as Transform, Fov("overviewFieldOfView", 52)),
                ("M2", Ref(ctrl, "clockPose") as Transform, Fov("clockFieldOfView", 40)),
                ("MS", Ref(ctrl, "switchPose") as Transform, Fov("switchFieldOfView", 40)),
                ("M1", cabinet != null ? cabinet.CameraAnchor : null, cabinet != null ? cabinet.CameraFieldOfView : 40),
            };
            var sensor = new Vector2(36, 24);
            var cameras = new List<(string, Camera)>();
            try
            {
                foreach (var (pose, at, fov) in anchors)
                    if (at != null) cameras.Add((pose, MakeCamera("Scan camera " + pose, sensor, fov, 0, at.position, at.rotation)));
                foreach (var t in roots.SelectMany(g => g.GetComponentsInChildren<TMP_Text>(true)))
                {
                    var (px, pose) = Measure(t, cameras);
                    string path = PathOf(t.transform);
                    string exception = px <= 0 ? "menü çapalarından kadraj dışı (kapalı ya da yürüyüş sırasında görünen yazı)" : ExceptionOf(t, path, px, Minimum);
                    list.Add(new Entry { Path = path, Text = Short(t.text), Pixels = px, Pose = pose, Minimum = Minimum, Exception = exception });
                }
            }
            finally { Release(cameras); }
            return list;
        }

        /// <summary>Canlı taramanın çıktısı (Aşama 10): bütün durumların JSON dizisi.</summary>
        public const string LiveAllPath = "Docs/UI_Siginak_Rev3/A10_dil_olcumleri.json";
        /// <summary>Canlı taramanın ara dosyası (her taramanın satırları eklenir; <see cref="WriteLiveAll"/> birleştirir).</summary>
        public const string LiveAllRowsPath = "Temp/a10_dil_satirlari.jsonl";

        [System.Serializable]
        sealed class LiveRow
        {
            public string tag, lang, pose, path, text, flag, exception;
            public float px, min, design;
            public bool autosize, overflow;
            public int glyphFallback;
            public long block;
            /// <summary>Yazının kutusu (kenar boşlukları düşülmüş) ve mürekkep sınırları, yazının kendi biriminde (Overlay'de punto, dünyada metre).</summary>
            public float boxW, boxH, inkW, inkH;
            public int lines;
            public string overflowMode;
            /// <summary>Otomatik puntonun alt sınırı; otomatik değilse 0.</summary>
            public float fontMin;
            /// <summary>Otomatik punto, alt sınıra inmeden küçüldüyse çizilen/tasarlanan oranı (bilgi, bayrak değil); küçülmediyse 0.</summary>
            public float autoRatio;
            /// <summary><c>okuma | uzak | gecis | kagit | form</c>; <see cref="WriteLiveAll"/> doldurur, <c>küçük</c> bayrağı yalnız <c>okuma</c>'da konur.</summary>
            public string reading;
        }

        const string MissingPath = "Loc.Missing", OverlayPose = "Overlay";
        const string ReadingFocus = "okuma", ReadingFar = "uzak", ReadingTransit = "gecis", ReadingPaper = "kagit", ReadingForm = "form";
        /// <summary>İş emri formu yakın pozda okunur (R18c; <c>zoomLimit.x</c>): K4'te ölçülen px bu orana bölünür.</summary>
        const float WorkOrderNearZoom = 0.6f;
        const string WorkOrderPath = "Planning board/İş emri/", HandoverSlipPath = "Desk/Physical receipt/";

        static readonly Regex TurkishLetters = new Regex("[ğüşıöçĞÜŞİÖÇ]", RegexOptions.CultureInvariant);
        static readonly Regex TurkishWords = new Regex(@"\b(bir|gun|fon|ekip|ekibi|bolge|sabotaj|elektrik|denetim|karantina|baskin|hasar|tur|yolda|hazir|ile|daha|yok|var|kez|sira|hedef|yetenek|bekliyor|ayakta|izleme|kabul|sevk|vardiya|belge|talimat)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly Regex RichTag = new Regex("<[^>]+>", RegexOptions.CultureInvariant);
        static readonly Regex WordToken = new Regex(@"\p{L}+", RegexOptions.CultureInvariant);
        /// <summary>İngilizcede de Türkçe kalan bütün metinler (dil anahtarı, künye).</summary>
        static readonly HashSet<string> EnglishFixedTexts = new HashSet<string> { "TÜRKÇE", "Türkçe · English", "DİL · LANGUAGE", "KYGA", "ANA ŞALTER" };
        /// <summary>Oyundaki kişi adları (<c>Simulation.PlanningBoard</c> kurucu listesi + aday adları + fikstürlerin tasarım adları).</summary>
        static readonly string[] AdmittedNames =
        {
            "Derya", "Selin", "İpek", "Deniz", "Nermin", "Zeynep", "Orhan", "Cem", "Eren", "Suna", "Rauf", "Meral", "Arda", "Ece", "Murat",
            "Aylin", "Baran", "Banu", "Ozan", "Lale", "Sinan", "Asli", "Kerem", "Sema", "Tamer", "Dilan", "Yalcin", "Feride", "Umut", "Ayca",
            "Levent", "Naz", "Melek", "Tarık", "Ebru", "Gülizar", "Mehmet",
        };

        /// <summary>Çevrilmeyen sözcükler: kişi adları (Core'un kurucu listesi, aday adları) büyük ve küçük harfli, künye.</summary>
        static readonly HashSet<string> EnglishFixedWords = BuildFixedWords();

        static HashSet<string> BuildFixedWords()
        {
            var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
            var words = new HashSet<string> { "KYGA" };
            foreach (var name in AdmittedNames)
            {
                words.Add(name);
                words.Add(name.ToUpper(tr));
            }
            return words;
        }


        static readonly HashSet<string> liveMissing = new HashSet<string>();
        static bool liveListening;

        static void OnLocMissing(string source) { lock (liveMissing) liveMissing.Add(source); }

        /// <summary>Koşu başı: <c>Loc.Missing</c> dinlenir (EN'de tek olay başarısızlıktır; bir sonraki <see cref="ScanLiveAll"/> satıra yazar).</summary>
        public static void BeginLiveAll()
        {
            englishTable = null;
            if (liveListening) return;
            lock (liveMissing) liveMissing.Clear();
            Corrigible.Core.Loc.Missing += OnLocMissing;
            liveListening = true;
        }

        /// <summary>Koşu sonu: <c>Loc.Missing</c> dinlemesi kapanır. Kalan olaylar <paramref name="cam"/> ile <paramref name="tag"/> satırına boşaltılır.</summary>
        public static void EndLiveAll(string tag = "koşu sonu")
        {
            FlushMissing(tag);
            if (!liveListening) return;
            Corrigible.Core.Loc.Missing -= OnLocMissing;
            liveListening = false;
        }

        /// <summary>Ara satır dosyasını siler (ilk oturumdan önce çağır; tekrar koşularda çağırma, <see cref="WriteLiveAll"/> son koşuyu seçer).</summary>
        public static string ResetLiveAll()
        {
            if (File.Exists(LiveAllRowsPath)) File.Delete(LiveAllRowsPath);
            return "silindi · " + LiveAllRowsPath;
        }

        /// <summary>
        /// Biriken <c>Loc.Missing</c> olaylarını <paramref name="tag"/> satırına yazar. <paramref name="block"/>: durumun blok damgası (<see cref="ScanLiveAll"/>);
        /// satırlar o duruma ait kalır, yoksa <see cref="WriteLiveAll"/> ölçüm satırlarını eski blok sayıp atar. Verilmezse (koşu sonu) kendi damgası.
        /// </summary>
        static void FlushMissing(string tag, long? block = null)
        {
            List<string> items;
            lock (liveMissing)
            {
                if (liveMissing.Count == 0) return;
                items = liveMissing.ToList();
                liveMissing.Clear();
            }
            string lang = Corrigible.Core.Loc.Lang;
            long stamp = block ?? System.DateTime.UtcNow.Ticks;
            AppendRows(items.Select(source => new LiveRow { tag = tag, lang = lang, pose = "-", path = MissingPath, text = source, flag = MissingPath, block = stamp }));
        }

        static void AppendRows(IEnumerable<LiveRow> rows)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LiveAllRowsPath));
            var sb = new StringBuilder();
            foreach (var row in rows) sb.AppendLine(JsonUtility.ToJson(row));
            File.AppendAllText(LiveAllRowsPath, sb.ToString());
        }

        /// <summary>
        /// Ara satırları <see cref="LiveAllPath"/>'e JSON dizisi olarak yazar: aynı (tag, lang) için yalnız son koşunun bloğu kalır (bir durumu yeniden çekmek eskisini
        /// değiştirir). Blok ölçüm satırlarından seçilir; <c>Loc.Missing</c> satırları o bloktan ya da sonrasından kalır (S1 öncesi ham dosyada kendi damgaları vardı, ölçümü
        /// silmezler). Ardından <see cref="AssignReading"/> okuma alanını ve <c>küçük</c> bayrağını yazar. Dönen: satır sayısı, küçük sayısı ve dosya yolu.
        /// </summary>
        public static string WriteLiveAll()
        {
            if (!File.Exists(LiveAllRowsPath)) return "satır yok · " + LiveAllRowsPath;
            var rows = File.ReadAllLines(LiveAllRowsPath).Where(l => !string.IsNullOrWhiteSpace(l)).Select(JsonUtility.FromJson<LiveRow>).ToList();
            var last = rows.GroupBy(r => (r.tag, r.lang)).ToDictionary(g => g.Key, g => (g.Any(r => r.path != MissingPath) ? g.Where(r => r.path != MissingPath) : g).Max(r => r.block));
            var kept = rows.Where(r => r.path == MissingPath ? r.block >= last[(r.tag, r.lang)] : r.block == last[(r.tag, r.lang)]).ToList();
            int small = AssignReading(kept);
            Directory.CreateDirectory(Path.GetDirectoryName(LiveAllPath));
            File.WriteAllText(LiveAllPath, "[\n" + string.Join(",\n", kept.Select(r => "  " + JsonUtility.ToJson(r))) + "\n]\n");
            return $"{kept.Count} satır · küçük {small} · {LiveAllPath}";
        }

        /// <summary>
        /// Her satırın okuma alanını yazar (Aşama 10 §1; kamera gerekmez, ölçümler üzerinden): Overlay her zaman <c>okuma</c>; kabul kâğıdı yalnız kendi hedef durumunda
        /// <c>okuma</c>, öteki yerde <c>kagit</c>; elde değilken devir fişi ve sıfır ölçek <c>gecis</c>; öteki dünya yazıları (yol, dil) için medyan px'i en yüksek poz ailesinde
        /// <c>okuma</c>, dışında <c>uzak</c>. İş emri formu yakın pozda (px / <see cref="WorkOrderNearZoom"/>) eşiği geçiyorsa <c>form</c>; geçemiyorsa <c>okuma</c> kalır ve
        /// bayrak alır. <c>küçük</c> yalnız <c>okuma</c> satırına, gerekçesi yoksa konur (eski ham satırda duran <c>küçük</c> silinir). Dönen: <c>küçük</c> satır sayısı.
        /// </summary>
        static int AssignReading(List<LiveRow> rows)
        {
            var best = rows.Where(r => r.path != MissingPath && r.pose != OverlayPose)
                .GroupBy(r => (r.path, r.lang))
                .ToDictionary(g => g.Key, g => g.GroupBy(r => ReadingFamily(r.tag)).OrderByDescending(f => Median(f.Select(r => r.px))).First().Key);
            int small = 0;
            foreach (var r in rows)
            {
                if (r.path == MissingPath) continue;
                r.reading = ReadingOf(r, r.pose == OverlayPose ? null : best[(r.path, r.lang)]);
                bool order = r.path.Contains(WorkOrderPath);
                float px = order ? r.px / WorkOrderNearZoom : r.px;
                var flags = (r.flag ?? "").Split(new[] { " | " }, System.StringSplitOptions.RemoveEmptyEntries).Where(f => !f.StartsWith("küçük ", System.StringComparison.Ordinal)).ToList();
                // Eski ham satırda sonradan eklenen yol istisnası da geçerli olsun
                if (r.reading == ReadingFocus && string.IsNullOrEmpty(r.exception)) r.exception = ExceptionOf(null, r.path, px, r.min) ?? "";
                if (r.reading == ReadingFocus && px < r.min && string.IsNullOrEmpty(r.exception))
                {
                    flags.Insert(0, $"küçük {px:0.0}/{r.min:0}" + (order ? " (yakın)" : ""));
                    small++;
                }
                r.flag = string.Join(" | ", flags);
            }
            return small;
        }

        static string ReadingOf(LiveRow r, string bestFamily)
        {
            if (r.px <= 0) return ReadingTransit;
            if (r.pose == OverlayPose) return ReadingFocus;
            string target = AdmissionPaperTarget(r.path);
            if (target != null) return target == r.tag ? ReadingFocus : ReadingPaper;
            if (r.path.Contains(HandoverSlipPath) && r.tag is "S1" or "S2" or "S3" or "S4") return ReadingTransit;
            if (ReadingFamily(r.tag) != bestFamily) return ReadingFar;
            return r.path.Contains(WorkOrderPath) && r.px / WorkOrderNearZoom >= r.min ? ReadingForm : ReadingFocus;
        }

        /// <summary>Kabul kâğıdının okunduğu durum kimliği; hedefi canlı ölçülmeyen kâğıtta (statik taramada K8R) boş; kabul kâğıdı değilse null.</summary>
        static string AdmissionPaperTarget(string path)
        {
            const string station = "Admission station/";
            if (path.Contains(station + "Belge/")) return "AR1";
            if (path.Contains(station + "Şerit/")) return "AR2";
            if (path.Contains(station + "Fiş 1/")) return "AR3";
            if (path.Contains("Talep yaprağı/")) return "AR4";
            return path.Contains(station + "Fiş 2/") || path.Contains(station + "ESKİ şerit") || path.Contains("RequestPad/Karbon/") ? "" : null;
        }

        /// <summary>Durum kimliğinin poz ailesi (PLAN §1.3); listede olmayan durum kendi ailesidir.</summary>
        static string ReadingFamily(string tag)
        {
            bool Starts(string prefix) => tag.StartsWith(prefix, System.StringComparison.Ordinal);
            bool Numbered(char letter) => tag.Length > 1 && tag[0] == letter && char.IsDigit(tag[1]);
            if (Starts("H1")) return "H1";
            if (Starts("K1")) return "K1";
            if (Starts("K2") || Starts("K3")) return "Harita";
            if (Starts("K4") || Starts("BK") || Numbered('F')) return "K4";
            if (Numbered('C')) return "K5a";
            if (tag.Length == 2 && tag[0] == 'S' && tag[1] >= '1' && tag[1] <= '7') return "K6";
            if (Starts("K8") || tag is "D1" or "D2") return "K8";
            if (Starts("AR")) return "K8R";
            if (Numbered('A')) return "K8A";
            if (tag == "G1") return "K8G";
            if (Starts("K9")) return "K9";
            if (tag is "B1" or "M0a" or "M0b") return "M0";
            return Starts("Z") ? "Z" : tag;
        }

        static float Median(IEnumerable<float> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }

        static bool LiveVisible(TMP_Text t)
        {
            if (t == null || !t.isActiveAndEnabled || string.IsNullOrEmpty(t.text) || t.font == null) return false;
            if (t.alpha <= 0.005f || t.color.a <= 0.005f) return false;
            if (Mathf.Abs(t.transform.lossyScale.y) < 1e-4f) return false;
            foreach (var group in t.GetComponentsInParent<CanvasGroup>(true))
            {
                if (group.isActiveAndEnabled && group.alpha <= 0.005f) return false;
                if (group.isActiveAndEnabled && group.ignoreParentGroups) break;
            }
            foreach (var canvas in t.GetComponentsInParent<Canvas>(true)) if (!canvas.enabled) return false;
            return !(t is TextMeshPro) || (t.TryGetComponent<MeshRenderer>(out var r) && r.enabled);
        }

        /// <summary>
        /// Yüklü bütün sahnelerde etkin her yazıyı ölçer (Aşama 10, PLAN §1.2 b): Overlay'de çizilen punto × tuval ölçeği, dünyada <paramref name="cam"/> ile
        /// dört köşe kadrajda çizilen punto, ikisi de 1080p'ye ölçeklenir; kadraj dışı atlanır. Eşikler kart adı, fiş adı, defter adı ve belgedeki ad için 18 px, öteki
        /// 14 px; kart yüzünün gerekçeleri ve <see cref="Exceptions"/> uygulanır. Eşikten bağımsız bayraklar: kesilme, dikey ve yatay taşma, otomatik puntonun alt sınırı,
        /// .notdef, Dyn yedeğe düşen karakter, İngilizcede izin listesi dışı Türkçe harf, sözcük ya da çevrilmemiş kaynak metin. <c>küçük</c> bayrağı burada konmaz: yazının okunduğu
        /// poz bütün durumlar bitince belli olur (<see cref="AssignReading"/>); dönen özetteki "eşik altı" ham sayıdır. <paramref name="only"/> verilirse yalnız onun seçtiği yazılar (ör.
        /// üzerine gelme kağıdı). Satırlar <see cref="LiveAllRowsPath"/>'e eklenir (<paramref name="tag"/> durum kimliği, ör. <c>K4a</c>; aynı tag tekrar yazılırsa son blok
        /// geçerli olur), dönen satır özetidir. Dil: o anki <c>Loc.Lang</c>.
        /// </summary>
        public static string ScanLiveAll(Camera cam, string tag, System.Func<TMP_Text, bool> only = null)
        {
            if (string.IsNullOrEmpty(tag)) return "tag gerekli";
            var cams = cam != null ? new[] { ("Live", cam) } : new (string, Camera)[0];
            float norm = cam != null ? 1080f / Mathf.Max(1, cam.pixelHeight) : 1;
            string lang = Corrigible.Core.Loc.Lang;
            bool english = lang == Corrigible.Core.Loc.English;
            long block = System.DateTime.UtcNow.Ticks;
            var classes = LiveClasses();
            var rows = new List<LiveRow>();
            int small = 0, overflow = 0, turkish = 0, notdef = 0, autosize = 0, fallback = 0;
            FlushMissing(tag, block);
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!LiveVisible(t) || (only != null && !only(t))) continue;
                float px;
                string pose;
                var root = t.GetComponentsInParent<Canvas>(true).LastOrDefault();
                if (t is TextMeshProUGUI && root != null && root.renderMode != RenderMode.WorldSpace)
                {
                    float pt = RenderedPointSize(t, out bool empty);
                    if (empty) continue;
                    float scale = root.scaleFactor;
                    for (var p = t.transform; p != null && p != root.transform; p = p.parent) scale *= Mathf.Abs(p.localScale.y);
                    px = pt * scale * 1080f / Mathf.Max(1, root.pixelRect.height);
                    pose = "Overlay";
                }
                else
                {
                    if (cam == null) continue;
                    var (raw, _) = MeasureWorld(t, cams, true, true);
                    if (raw <= 0) continue;
                    px = raw * norm;
                    pose = "Live";
                }
                string path = PathOf(t.transform);
                float min = Minimum;
                string reason = null;
                if (classes.TryGetValue(t, out var cls)) { min = cls.min; reason = cls.reason; }
                string exception = px < min ? reason ?? ExceptionOf(t, path, px, min) : null;
                var flags = new List<string>();
                if (px < min && exception == null) small++;
                string over = OverflowTwoAxes(t);
                if (over != null) { flags.Add(over); overflow++; }
                float design = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
                var info = t.textInfo;
                float autoRatio = 0;
                if (t.enableAutoSizing && design > 0)
                {
                    float drawn = RenderedPointSize(t, out _);
                    float ratio = drawn / design;
                    if (ratio < 0.985f)
                    {
                        if (drawn <= t.fontSizeMin * 1.01f) { flags.Add($"otomatik alt sınır %{ratio * 100:0}"); autosize++; }
                        else autoRatio = ratio;
                    }
                }
                var (missing, dyn) = GlyphProblems(t);
                if (missing > 0) { flags.Add($".notdef {missing}"); notdef++; }
                if (dyn > 0) { flags.Add($"Dyn yedek {dyn}"); fallback += dyn; }
                if (english && (LeftTurkish(t.text) ?? UntranslatedSource(t.text)) is string word) { flags.Add("Türkçe: " + word); turkish++; }
                var (boxW, boxH) = BoxOf(t);
                var ink = t.textBounds.size;
                rows.Add(new LiveRow
                {
                    tag = tag, lang = lang, pose = pose, path = path, text = Short(t.text), px = px, min = min, design = design,
                    autosize = t.enableAutoSizing, overflow = over != null, flag = flags.Count > 0 ? string.Join(" | ", flags) : "",
                    exception = exception ?? "", glyphFallback = dyn, block = block,
                    boxW = boxW, boxH = boxH, inkW = ink.x, inkH = ink.y, lines = info != null ? info.lineCount : 0, overflowMode = t.overflowMode.ToString(),
                    fontMin = t.enableAutoSizing ? t.fontSizeMin : 0, autoRatio = autoRatio,
                });
            }
            AppendRows(rows);
            return $"{rows.Count} yazı · eşik altı {small} · taşma {overflow} · Türkçe {turkish} · .notdef {notdef} · otomatik {autosize} · Dyn {fallback}";
        }

        /// <summary>Ad sınıfları (18 px) ve kart yüzünün gerekçeleri: yazı → (eşik, gerekçe).</summary>
        static Dictionary<TMP_Text, (float min, string reason)> LiveClasses()
        {
            var map = new Dictionary<TMP_Text, (float min, string reason)>();
            foreach (var face in Object.FindObjectsByType<CardFace>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                foreach (var f in FieldsOf(face)) map[f.Text] = (f.Minimum, f.Reason);
            foreach (var slip in Object.FindObjectsByType<HandoverSlipFace>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                foreach (var t in slip.NameTexts) if (t != null) map[t] = (SlipNameMinimum, null);
            foreach (var page in Object.FindObjectsByType<RecipeBookPage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                foreach (var t in page.NameTexts) if (t != null) map[t] = (BookNameMinimum, null);
            foreach (var doc in Object.FindObjectsByType<AdmissionDocumentFace>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (doc.NameText != null) map[doc.NameText] = (AdmissionNameMinimum, null);
            return map;
        }

        /// <summary>Yazının kutusu, kenar boşlukları düşülmüş (yazının kendi biriminde).</summary>
        static (float w, float h) BoxOf(TMP_Text t)
        {
            var rect = t.rectTransform.rect;
            var margin = t.margin;
            return (rect.width - margin.x - margin.z, rect.height - margin.y - margin.w);
        }

        /// <summary>
        /// İki eksenli taşma, mürekkebe göre: <c>isTextOverflowing</c> fontun satır ölçüsünü karşılaştırır, büyük harf yüksekliğine çizilmiş dünya yuvalarında hep doğrudur.
        /// Kırpan kipte kesilme önce gelir (<c>kesildi @i</c>). Dikey ve yatay %3 pay taşır (yatayda em'in %5'i de: sondaki karakterin ilerlemesi ve yuvarlama). Tercih edilen genişlik
        /// yalnız kırpan kipte (otomatik punto dışında) alınır: kırpılmış yazı sınırlardan görünmez. Satır kırmada satır sayısı yalnız <c>maxVisibleLines</c>'a karşı denetlenir
        /// (kutu yüksekliği mürekkeple denetlendi). Sıfır dikdörtgen (çapa) atlanır. Sorun yoksa null.
        /// </summary>
        static string OverflowTwoAxes(TMP_Text t)
        {
            t.ForceMeshUpdate(true, true);
            var info = t.textInfo;
            if (info == null || info.characterCount == 0) return null;
            var rect = t.rectTransform.rect;
            // Dünya kutuları metre: 6 mm'lik gerçek yuva "sıfır" sayılmasın
            float zero = Mathf.Min(0.01f, t.fontSize * 0.01f);
            if (rect.width <= zero || rect.height <= zero) return null;
            var (boxW, boxH) = BoxOf(t);
            var mode = t.overflowMode;
            bool clips = mode is TextOverflowModes.Truncate or TextOverflowModes.Ellipsis or TextOverflowModes.Masking or TextOverflowModes.Page;
            if (clips && t.firstOverflowCharacterIndex >= 0) return $"kesildi @{t.firstOverflowCharacterIndex}";
            var ink = t.textBounds.size;
            if (boxH > 0 && ink.y > boxH * 1.03f + 1e-4f) return $"taşma dikey {ink.y:0.###}/{boxH:0.###}";
            float widest = boxW * 1.03f + t.fontSize * 0.05f;
            bool wrapping = t.textWrappingMode != TextWrappingModes.NoWrap && t.textWrappingMode != TextWrappingModes.PreserveWhitespaceNoWrap;
            float width = ink.x;
            if (!wrapping && clips && !t.enableAutoSizing)
                width = Mathf.Max(width, t.GetPreferredValues(t.text, float.PositiveInfinity, float.PositiveInfinity).x);
            if (boxW > 0 && width > widest) return $"taşma yatay {width:0.###}/{boxW:0.###}";
            if (!wrapping) return null;
            int limit = t.maxVisibleLines > 0 && t.maxVisibleLines < 9999 ? t.maxVisibleLines : int.MaxValue;
            return info.lineCount > limit ? $"taşma {info.lineCount} satır/{limit}" : null;
        }

        /// <summary>Yazının karakterlerinde .notdef sayısı ve dinamik yedek atlasa düşen karakter sayısı (<c>characterInfo[i].fontAsset</c>).</summary>
        static (int notdef, int dyn) GlyphProblems(TMP_Text t)
        {
            var info = t.textInfo;
            int missing = 0, dyn = 0;
            if (info == null) return (0, 0);
            uint fallbackGlyph = TMP_Settings.missingGlyphCharacter != 0 ? (uint)TMP_Settings.missingGlyphCharacter : 9633;
            for (int i = 0; i < info.characterCount && i < info.characterInfo.Length; i++)
            {
                var ch = info.characterInfo[i];
                if (ch.elementType != TMP_TextElementType.Character || !ch.isVisible) continue;
                if (ch.textElement == null || ch.textElement.glyphIndex == 0 || (uint)ch.character == fallbackGlyph) { missing++; continue; }
                var font = ch.fontAsset;
                if (font != null && font != t.font && font.atlasPopulationMode == AtlasPopulationMode.Dynamic) dyn++;
            }
            return (missing, dyn);
        }

        /// <summary>İngilizcede kalan ilk Türkçe harfli ya da sık Türkçe sözcük (izin listesi dışı); yoksa null.</summary>
        static string LeftTurkish(string text)
        {
            string plain = RichTag.Replace(text ?? "", "");
            if (EnglishFixedTexts.Contains(plain.Trim())) return null;
            foreach (Match m in WordToken.Matches(plain))
            {
                string w = m.Value;
                if (EnglishFixedWords.Contains(w)) continue;
                if (TurkishLetters.IsMatch(w) || TurkishWords.IsMatch(w)) return w;
            }
            return null;
        }

        /// <summary>Çeviri tablosunun İngilizcesi ve bütün çeviri değerleri (koşu başında yeniden okunur).</summary>
        static (Dictionary<string, string> table, HashSet<string> values)? englishTable;

        /// <summary>
        /// İngilizcede çevirisi başka olan bir kaynak anahtarının aynısı duruyorsa metin (harf denetimi yakalayamaz); izin listesindeki metin ve aynı zamanda bir çevirinin
        /// değeri olan metin hariç. Yoksa null.
        /// </summary>
        static string UntranslatedSource(string text)
        {
            string plain = RichTag.Replace(text ?? "", "").Trim();
            if (plain.Length == 0 || EnglishFixedTexts.Contains(plain)) return null;
            if (englishTable == null)
            {
                var parsed = File.Exists(Corrigible.EditorTools.LocalizationTool.TableFile) ? Corrigible.Core.Loc.ParseTable(File.ReadAllText(Corrigible.EditorTools.LocalizationTool.TableFile)) : null;
                var en = parsed != null && parsed.TryGetValue(Corrigible.Core.Loc.English, out var column) ? column : new Dictionary<string, string>();
                englishTable = (en, new HashSet<string>(en.Values));
            }
            var (table, values) = englishTable.Value;
            return table.TryGetValue(plain, out var translation) && translation != plain && !values.Contains(plain) ? plain : null;
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var p = t; p != null; p = p.parent) parts.Add(p.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        static string Short(string s)
        {
            s = (s ?? "").Replace("\n", " ⏎ ");
            return s.Length > 40 ? s.Substring(0, 40) + "…" : s;
        }

        static readonly Regex FreedSlot = new Regex(@"Shell/Freed board/Slot \d+", RegexOptions.CultureInvariant);

        public static string Report(List<Entry> entries)
        {
            var sb = new StringBuilder();
            int bad = entries.Count(e => e.Violation);
            sb.AppendLine($"Sığınak punto taraması · {System.DateTime.Now:yyyy-MM-dd HH:mm} · 1920×1080 · en az {Minimum} px (kart adı {CardNameMinimum} px)");
            sb.AppendLine($"{entries.Count} metin · {bad} ihlal · {entries.Count(e => e.Exception != null && e.Small)} gerekçeli istisna");
            sb.AppendLine();
            sb.AppendLine("İHLALLER");
            foreach (var e in entries.Where(e => e.Violation).OrderBy(e => e.Pixels))
                sb.AppendLine($"  {e.Pixels,5:0.0}/{e.Threshold:0} px · {e.Pose,-11} · {e.Path} · “{e.Text}”{(e.Problem != null ? " · " + e.Problem : "")}");
            if (bad == 0) sb.AppendLine("  yok");
            sb.AppendLine();
            sb.AppendLine("İSTİSNALAR (eşik altı, gerekçeli)");
            var exceptions = entries.Where(e => e.Exception != null && e.Small).ToList();
            // Özgür bölge panosunun yuvaları tek satıra iner (alan başına: Slot * / alan; §1.7 P)
            foreach (var g in exceptions.Where(e => FreedSlot.IsMatch(e.Path)).GroupBy(e => FreedSlot.Replace(e.Path, "Slot *")).OrderBy(g => g.Key))
                sb.AppendLine($"  {g.Min(e => e.Pixels),5:0.0}–{g.Max(e => e.Pixels):0.0}/{g.First().Threshold:0} px · {g.First().Pose,-11} · {g.Key} · {g.Count()} yuva · {g.First().Exception}");
            foreach (var e in exceptions.Where(e => !FreedSlot.IsMatch(e.Path)).OrderBy(e => e.Path))
                sb.AppendLine($"  {e.Pixels,5:0.0}/{e.Threshold:0} px · {e.Pose,-11} · {e.Path} · {e.Exception}");
            sb.AppendLine();
            sb.AppendLine("EN KÜÇÜK 20 (istisnasız)");
            foreach (var e in entries.Where(e => e.Exception == null).OrderBy(e => e.Pixels - e.Threshold).Take(20))
                sb.AppendLine($"  {e.Pixels,5:0.0}/{e.Threshold:0} px · {e.Pose,-11} · {e.Path}");
            var cards = entries.Where(e => e.Path.StartsWith("Card/")).ToList();
            if (cards.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("KARTLAR (K4, en kötü örnek; sınıfın en küçüğü)");
                foreach (var g in cards.GroupBy(e => e.Path).OrderBy(g => g.Key))
                {
                    var worst = g.OrderBy(e => e.Pixels).First();
                    sb.AppendLine($"  {worst.Pixels,5:0.0}/{worst.Threshold:0} px · {g.Key} · {worst.Pose}{(worst.Exception != null && worst.Small ? " · istisna" : "")}");
                }
            }
            var pages = entries.Where(e => e.Path.StartsWith("Book/")).ToList();
            if (pages.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("TARİF DEFTERİ (R18a: sayfalar okuma pozunda, K4 kamerası; ad ≥ 18 / öteki ≥ 14; üç açılım)");
                foreach (var g in pages.GroupBy(e => (lang: e.Pose.Substring(e.Pose.LastIndexOf(' ') + 1), name: e.Threshold >= BookNameMinimum))
                             .OrderBy(g => g.Key.lang).ThenByDescending(g => g.Key.name))
                {
                    var worst = g.OrderBy(e => e.Pixels - e.Threshold).First();
                    sb.AppendLine($"  {worst.Pixels,5:0.0}/{worst.Threshold:0} px · sayfa {(g.Key.name ? "adları" : "öteki")} · {g.Key.lang} · {worst.Path} · “{worst.Text}”");
                }
                foreach (var e in pages.Where(e => e.Problem != null).OrderBy(e => e.Path))
                    sb.AppendLine($"  sorun · {e.Path} · {e.Pose} · {e.Problem}");
            }
            var orderTexts = entries.Where(e => e.Pose != null && e.Pose.StartsWith(BoardNearPose)).ToList();
            if (orderTexts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("İŞ EMRİ FORMU (R18c: K4 yakın pozda okunur, yakınlaşma alt sınırı)");
                foreach (var e in orderTexts.OrderBy(e => e.Pixels))
                    sb.AppendLine($"  {e.Pixels,5:0.0}/{e.Threshold:0} px · {e.Path} · “{e.Text}”");
            }
            var admission = entries.Where(e => e.Path.StartsWith("Admission/")).ToList();
            if (admission.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("KAPI VE KABUL (R17: kâğıtlar K8R okuma pozunda, 0,318 m; belgedeki ad ≥ 18 / öteki ≥ 14; kâğıt türü başına en kötü)");
                foreach (var g in admission.GroupBy(e => (lang: e.Pose.Substring(e.Pose.LastIndexOf(' ') + 1), kind: e.Path.Split('/')[2].Split('/')[0]))
                             .OrderBy(g => g.Key.lang).ThenBy(g => g.Key.kind))
                {
                    var worst = g.OrderBy(e => e.Pixels - e.Threshold).First();
                    sb.AppendLine($"  {worst.Pixels,5:0.0}/{worst.Threshold:0} px · {g.Key.kind} · {g.Key.lang} · {worst.Path} · “{worst.Text}”");
                }
                foreach (var e in admission.Where(e => e.Problem != null).OrderBy(e => e.Path))
                    sb.AppendLine($"  sorun · {e.Path} · {e.Pose} · {e.Problem}");
            }
            var slips = entries.Where(e => e.Path.StartsWith("Slip/")).ToList();
            var plates = entries.Where(e => e.Path.EndsWith(" @K6") || e.Path.EndsWith(" @K1")).ToList();
            if (slips.Count > 0 || plates.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("VARDİYA KAPANIŞI (R19: fiş elde K6'da, ad ≥ 18 / öteki ≥ 14; plakanın 2. satırı K6 ve K1, en küçük metin)");
                foreach (var g in slips.GroupBy(e => (lang: e.Pose.Substring(e.Pose.LastIndexOf(' ') + 1), name: e.Threshold >= SlipNameMinimum))
                             .OrderBy(g => g.Key.lang).ThenByDescending(g => g.Key.name))
                {
                    var worst = g.OrderBy(e => e.Pixels - e.Threshold).First();
                    sb.AppendLine($"  {worst.Pixels,5:0.0}/{worst.Threshold:0} px · fiş {(g.Key.name ? "adları" : "öteki")} · {g.Key.lang} · {worst.Path} · “{worst.Text}”");
                }
                foreach (var e in plates.OrderBy(e => e.Path).ThenBy(e => e.Pose))
                    sb.AppendLine($"  {e.Pixels,5:0.0}/{e.Threshold:0} px · {e.Path} · {e.Pose} · “{e.Text}”{(e.Exception != null && e.Small ? " · istisna" : "")}{(e.Problem != null ? " · " + e.Problem : "")}");
            }
            return sb.ToString();
        }
    }
}
