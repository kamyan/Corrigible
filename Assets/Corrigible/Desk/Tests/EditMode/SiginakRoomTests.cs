using System.IO;
using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Sığınak odası (Aşama 2, Aşama 10'dan beri DeskGame'in tek sunumu): sahnenin kurulu hâli, baskın bağlantısı, eski
    /// komuta odasının kalmadığı, oda yazıları ve malzemeler.
    /// </summary>
    public class SiginakRoomTests
    {
        const string ScenePath = DeskSpriteManifest.Root + "/Scenes/DeskGame.unity";

        [Test]
        public void DeskGameScene_SiginakRoomIsTheOnlyPresentation()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var room = roots.Single(g => g.name == "SiginakRoom");
                Assert.IsTrue(room.activeSelf, "oda sahnede açık kayıtlı");
                foreach (var path in new[] { "Shell", "Desk", "Desk/MapMount", "Desk/Counter bank", "Desk/Key rail", "Desk/Shift switch", "Desk/Teleprinter",
                             "CRT desk", "CRT desk/CRT/CRT/Body/Screen", "Ceiling bulb", "Poses/K1", "Poses/Map", "Poses/K4", "Poses/K5a", "Poses/K6a",
                             "Poses/K6", "Desk/Paper tray/Slip over", "Desk/Paper tray/Slip target light", "Desk/Paper tray/Filed slip" })
                    Assert.IsNotNull(room.transform.Find(path), path);
                var mount = room.transform.Find("Desk/MapMount");
                Assert.Less(Vector3.Distance(new Vector3(0, 0.7612f, 1.3175f), mount.position), 1e-4f);

                // Eski komuta odası, eski HUD / alt çubuk, eski hikâye kâğıdı ve eski görev fişi yok
                var commandPost = roots.Single(g => g.name == "CommandPost3D").transform;
                Assert.AreEqual(new[] { "Main Camera" }, commandPost.Cast<Transform>().Select(t => t.name).ToArray());
                Assert.IsFalse(roots.Any(g => g.name == "LiberationMapCanvas"));

                // Kâğıt harita odanın masasında (1,2 m), kamera fiziksel ve K1'de, odanın fizik ışını açık
                var map = roots.SelectMany(g => g.GetComponentsInChildren<SadeMapView>(true)).Single();
                Assert.IsTrue(map.transform.IsChildOf(mount));
                Assert.AreEqual(1.2f / 1920f, map.transform.parent.localScale.x * map.transform.localScale.x, 1e-7f);
                var cam = commandPost.GetComponentInChildren<Camera>(true);
                var rig = cam.GetComponent<SiginakCameraRig>();
                Assert.IsTrue(rig.enabled);
                Assert.IsTrue(cam.usePhysicalProperties);
                Assert.AreEqual(-0.324f, cam.lensShift.y, 1e-4f);
                Assert.IsTrue(cam.GetComponent<PhysicsRaycaster>().enabled);

                // Baskın bağlantısı: CRT geçişi, monitöre yaklaşma pozu, baskında odanın gizlenmesi
                var link = roots.SelectMany(g => g.GetComponentsInChildren<SiginakRaidLink>(true)).Single();
                Assert.IsNull(link.transform.parent, "odanın kökünün dışında (oda gizliyken de çalışır)");
                var ls = new SerializedObject(link);
                Assert.AreSame(room, ls.FindProperty("room").objectReferenceValue);
                Assert.IsInstanceOf<SiginakCrt>(ls.FindProperty("transition").objectReferenceValue);
                var launcher = (Corrigible.Game.Raid.RaidLauncher)ls.FindProperty("launcher").objectReferenceValue;
                Assert.AreEqual("CrtDive", launcher.MonitorFocusPose.name);

                var lights = room.GetComponentsInChildren<Light>(true);
                var bulb = lights.Single(l => l.name == "Bulb light");
                Assert.AreEqual(LightShadows.Soft, bulb.shadows);
                // Revizyon 3: ampul 120° aşağı bakan spot (tek gölge dilimi; eskiden küp gölgeli nokta, 6 × 1024)
                Assert.AreEqual(LightType.Spot, bulb.type);
                Assert.AreEqual(2048, bulb.shadowCustomResolution);
                var lamp = lights.Single(l => l.name == "Desk lamp light");
                Assert.AreEqual(LightType.Spot, lamp.type);
                Assert.AreEqual(2048, lamp.shadowCustomResolution);
                Assert.IsNotNull(room.GetComponentInChildren<BulbFlicker>(true));
                // Dekor çarpıştırıcı taşımaz. İstasyon hedefleri ve kendi sürükleme işleyicisi olan fiziksel kâğıt girdi alır.
                int receiptColliders = 0, admissionColliders = 0, eventColliders = 0;
                foreach (var collider in room.GetComponentsInChildren<Collider>(true))
                {
                    if (collider.GetComponent<DeskTarget>() != null) continue;
                    // Model entegrasyonu: kullanıcının sahnede KAPALI park ettiği üçüncü taraf alternatif (Pekdata monitörünün cam
                    // kırıkları; Rigidbody + MeshCollider). Kapalı hiyerarşide ışın almaz; mesh'i Assets/Corrigible dışından gelir.
                    if (IsParkedThirdPartyCollider(collider)) continue;
                    // Revizyon 3 kartlı pano: konu satırları, deste tepsisi, pano yüzü, dosya ve KAYIPLAR sayfaları panonun girdi
                    // alanlarıdır. Ya bir pano bileşeni çarpıştırıcıyı serileştirilmiş alanında tutar ya da tık hiyerarşi boyunca
                    // bir atadaki pano işleyicisine çıkar (işleyicisiz sayfa yüzeyi → PersonFolderView, pano yüzü → BoardDragController).
                    // Aşama 5: tarif defterinin kapalı gövdesi (RecipeBookView.closedHit) ve sayfa çarpıştırıcıları (RecipeBookPage.hit,
                    // RecipeBookView.hitLeft/hitRight) da pano girdisidir; kaydedilmiş sahnede kapalıdırlar.
                    var boardOwners = BoardInputOwners(collider.transform);
                    if (boardOwners.Count > 0)
                    {
                        Assert.IsTrue(boardOwners.Any(o => ReferencesObject(o, collider) || o is IPointerClickHandler), collider.name + ": " +
                            string.Join(", ", boardOwners.Select(o => o.GetType().Name)) + " bu girdi alanını tutmalı ya da tıkını almalı");
                        continue;
                    }
                    // Aşama 6 (R9, R17) ve KOS aşama 2/7: kapıdaki kabul istasyonu. Tezgâh ve MASA tuşu (AdmissionStation.counterHit/deskKeyHit)
                    // ve damgaları kendi bileşenleri tutar; aday kartlarının ve grup fişinin tıkı CandidateGroupView'a çıkar. Buğu camı ve
                    // interkomun etkileşimi aşama 7'de söküldü (çarpıştırıcıları da). Kaydedilmiş sahnede hepsi kapalıdır (Rev3AdmissionSceneTests.S5).
                    var admissionOwners = InputOwners(collider.transform, AdmissionInputTypes);
                    if (admissionOwners.Count > 0)
                    {
                        Assert.IsTrue(admissionOwners.Any(o => ReferencesObject(o, collider) || o is IPointerClickHandler), collider.name + ": " +
                            string.Join(", ", admissionOwners.Select(o => o.GetType().Name)) + " bu girdi alanını tutmalı ya da tıkını almalı");
                        Assert.IsFalse(collider.enabled, collider.name + ": kabul çarpıştırıcısı kapalı kaydedilir (yalnız etkileşimliyken açılır)");
                        admissionColliders++;
                        continue;
                    }
                    // KOS aşama 4: olay tutanağının tek girdi alanı kökteki kutu; görünüm onu hitArea'da tutar, kaydedilmiş sahnede kapalı
                    var eventPaper = collider.GetComponent<ShelterEventPaperView>();
                    if (eventPaper != null)
                    {
                        Assert.AreSame(collider, eventPaper.HitArea, collider.name + ": olay tutanağının girdi alanı açıkça bağlanmalı");
                        Assert.IsFalse(collider.enabled, collider.name + ": olay tutanağının çarpıştırıcısı kapalı kaydedilir (yalnız veri varken açılır)");
                        eventColliders++;
                        continue;
                    }
                    var paper = collider.GetComponent<ShiftReceiptPaper>();
                    Assert.IsNotNull(paper, collider.name + ": hedef veya fiziksel devir kâğıdı olmayan çarpıştırıcı");
                    Assert.IsInstanceOf<IBeginDragHandler>(paper, "Kâğıt collider'ı gerçek sürükleme işleyicisiyle aynı nesnede olmalı.");
                    Assert.AreSame(collider, new SerializedObject(paper).FindProperty("hitArea").objectReferenceValue,
                        "Fiziksel kâğıdın girdi alanı açıkça bağlanmalı.");
                    receiptColliders++;
                }
                Assert.AreEqual(1, receiptColliders, "Masada tek sürüklenebilir devir kâğıdı bulunmalı.");
                // Tesis aşama 9: aynı görünümün ikinci örneği sunucu ödülünün kâğıdı (SiginakServerReward)
                Assert.AreEqual(2, eventColliders, "olay tutanağı (KOS aşama 4) ve sunucu ödülü kâğıdı (tesis aşama 9): ikisi de kapı panosuna gelir, EVRAK tepsisine iner; pano çarpıştırıcısız");
                Assert.GreaterOrEqual(admissionColliders, 8, "kabul istasyonu: tezgâh, MASA tuşu, iki damga, üç aday kartı ve grup fişi");
                // Revizyon 3 · Aşama 4 (R19): fiş yüzü, tepsideki kopya, EVRAK üstü çapası ve hedef ışığı çarpıştırıcı taşımaz;
                // fişin tek girdi alanı kökteki kutu (D §0.9; tek kâğıt akışında teleks kâğıdının boyu, 260 × 225 mm)
                var receipt = room.GetComponentsInChildren<ShiftReceiptPaper>(true).Single();
                Assert.AreEqual(1, receipt.GetComponentsInChildren<Collider>(true).Length, "fiş ağacında tek çarpıştırıcı");
                var receiptBox = receipt.GetComponent<BoxCollider>();
                Assert.AreEqual(0.26f, receiptBox.size.x, 1e-4f);
                Assert.AreEqual(0.225f, receiptBox.size.y, 1e-4f);
                Assert.IsNotEmpty(receipt.GetComponentsInChildren<HandoverSlipFace>(true), "fiş yüzü kâğıdın altında");
                foreach (var path in new[] { "Desk/Paper tray/Slip over", "Desk/Paper tray/Slip target light", "Desk/Paper tray/Filed slip" })
                    Assert.IsEmpty(room.transform.Find(path).GetComponentsInChildren<Collider>(true), path + ": çarpıştırıcısız");
                // Hareket etmeyen parçalar statik birleşir (Aşama 10 performans)
                Assert.IsTrue(GameObjectUtility.GetStaticEditorFlags(room.transform.Find("Desk/DeskModel/SM_Table_2_LOD0").gameObject).HasFlag(StaticEditorFlags.BatchingStatic));
                Assert.IsFalse(GameObjectUtility.GetStaticEditorFlags(room.GetComponentInChildren<StationKey>(true).gameObject).HasFlag(StaticEditorFlags.BatchingStatic));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Kapalı hiyerarşide duran ve mesh'i <c>Assets/Corrigible</c> dışından gelen MeshCollider (park edilmiş alternatif model).</summary>
        static bool IsParkedThirdPartyCollider(Collider collider)
        {
            if (collider.gameObject.activeInHierarchy || !(collider is MeshCollider mesh) || mesh.sharedMesh == null) return false;
            string path = AssetDatabase.GetAssetPath(mesh.sharedMesh);
            return !string.IsNullOrEmpty(path) && !path.StartsWith("Assets/Corrigible/");
        }

        static readonly System.Type[] BoardInputTypes =
        {
            typeof(BoardDragController), typeof(BoardView3D), typeof(PersonFolderView), typeof(LostEnvelopeView),
            typeof(BoardCardView), typeof(HurdaBin), typeof(BostaDeckView),
            // Aşama 5 (E27): kapalı defterin gövdesi ve sayfa çarpıştırıcıları (RecipeBookView ve RecipeBookPage alanlarında tutulur)
            typeof(RecipeBookView), typeof(RecipeBookPage),
        };

        /// <summary>Aşama 6 / KOS aşama 2: kabul istasyonunun girdi bileşenleri (hepsi <see cref="IPointerClickHandler"/>).</summary>
        static readonly System.Type[] AdmissionInputTypes =
        {
            typeof(AdmissionStation), typeof(StampDecision), typeof(CandidateGroupView),
        };

        /// <summary>Çarpıştırıcının atalarındaki (kendisi dahil) kartlı pano girdi bileşenleri; pano dışındaysa boş.</summary>
        static System.Collections.Generic.List<MonoBehaviour> BoardInputOwners(Transform t) => InputOwners(t, BoardInputTypes);

        /// <summary>Çarpıştırıcının atalarındaki (kendisi dahil) verilen türlerden girdi bileşenleri.</summary>
        static System.Collections.Generic.List<MonoBehaviour> InputOwners(Transform t, System.Type[] types)
        {
            var owners = new System.Collections.Generic.List<MonoBehaviour>();
            for (var p = t; p != null; p = p.parent)
                foreach (var type in types)
                    if (p.GetComponent(type) is MonoBehaviour m) owners.Add(m);
            return owners;
        }

        /// <summary>Bileşenin serileştirilmiş alanlarından biri (dizi öğeleri dahil) <paramref name="target"/>'ı gösteriyor mu.</summary>
        static bool ReferencesObject(Object owner, Object target)
        {
            var it = new SerializedObject(owner).GetIterator();
            while (it.Next(true))
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == target) return true;
            return false;
        }

        /// <summary>
        /// Tıklanır hedefin kutusu kendi eksenindeki modele oturur: dönük nesnede dünya eksenli kutu derinliği de kapsayıp
        /// boşluğu (ve önündeki nesneleri) yakalıyordu.
        /// </summary>
        [Test]
        public void RoomTargets_CollidersFitTheirModels()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var room = scene.GetRootGameObjects().Single(g => g.name == "SiginakRoom");
                int checkedTargets = 0;
                foreach (var target in room.GetComponentsInChildren<DeskTarget>(true))
                {
                    var box = target.GetComponent<BoxCollider>();
                    var meshes = target.GetComponentsInChildren<MeshFilter>(true)
                        .Where(f => f.sharedMesh != null && f.GetComponent<TMP_Text>() == null).ToList();
                    if (box == null || meshes.Count == 0) continue;
                    var min = Vector3.positiveInfinity; var max = Vector3.negativeInfinity;
                    foreach (var f in meshes)
                    {
                        var b = f.sharedMesh.bounds;
                        for (int i = 0; i < 8; i++)
                        {
                            var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                            var local = target.transform.InverseTransformPoint(f.transform.TransformPoint(corner));
                            min = Vector3.Min(min, local); max = Vector3.Max(max, local);
                        }
                    }
                    var slack = box.size - (max - min);
                    // 1e-5: ölçekli ebeveyn (model entegrasyonu 2: dolabın iç düzeni 0,615) dönüşüm yuvarlaması; sürgü kutusu tam 5 cm pay taşır
                    Assert.LessOrEqual(Mathf.Max(slack.x, slack.y, slack.z), 0.05f + 1e-5f, target.name + ": çarpıştırıcı modelden büyük " + slack.ToString("F3"));
                    checkedTargets++;
                }
                Assert.Greater(checkedTargets, 8);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Odadaki her yazı: çevrilebilir sabit metinse kaynak = yazı; fontun statik atlası her glifi içerir (TR + EN).</summary>
        [Test]
        public void RoomTexts_AreLocalizableAndCovered()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var room = scene.GetRootGameObjects().Single(g => g.name == "SiginakRoom");
                var texts = room.GetComponentsInChildren<TextMeshPro>(true);
                Assert.Greater(texts.Length, 30);
                var table = Corrigible.Core.Loc.ParseTable(File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"));
                var english = table[Corrigible.Core.Loc.English];
                foreach (var t in texts)
                {
                    Assert.AreEqual("", SiginakFontPreview.MissingStatic(t.font, t.text), t.name + " eksik glif (TR)");
                    var loc = t.GetComponent<DeskLocText>();
                    if (loc == null) continue;
                    Assert.AreEqual(t.text, loc.Source, t.name);
                    Assert.IsTrue(english.TryGetValue(loc.Source, out var en) && !string.IsNullOrEmpty(en), "çevirisi yok: " + loc.Source);
                    Assert.AreEqual("", SiginakFontPreview.MissingStatic(t.font, en), t.name + " eksik glif (EN)");
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void RoomMaterials_ComeFromTokens()
        {
            var mats = Directory.GetFiles(SiginakSpriteManifest.Root + "/Materials", "*.mat", SearchOption.AllDirectories)
                .Select(p => AssetDatabase.LoadAssetAtPath<Material>(p.Replace('\\', '/'))).ToList();
            // Aşama 10 (T1): kullanılmayan malzemeler ve yalnız eski rafın malzemeleri silindi (161 → 34)
            Assert.Greater(mats.Count, 30);
            foreach (var m in mats)
            {
                Assert.IsTrue(m.shader.name.StartsWith("Universal Render Pipeline/") || m.shader.name.StartsWith("TextMeshPro/"), m.name + ": " + m.shader.name);
                if (m.HasProperty("_BaseColor")) Assert.AreNotEqual(Color.magenta, m.GetColor("_BaseColor"), m.name + ": eksik token");
            }
        }
    }
}
