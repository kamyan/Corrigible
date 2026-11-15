using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Teleks modeli değişimi (kullanıcının daktilo modeli, <c>Assets/Typewriter</c>): kaydedilmiş <c>DeskGame</c>'de teleksin işlevsel
    /// parçaları daktiloya bağlı. Eski Rev3 Telex gövdesi yok; daktilo çarpıştırıcısız; teleks kâğıdı silindirden çıkar (dört yuva
    /// silindirin üstünde, alt kenarı silindirin altında), rulo arkada ve <c>Teleprinter.roll</c>; TELEKS plakası daktilonun
    /// kapağında; kök hedef kutusu silindirin altında kalır (fiş ışını alır), kâğıdın kendi hedefi var. Sahne açıkken (kullanıcının
    /// sahnesi) atlanır; sahne kaydedilmeden kapatılır. Ayrıca kâğıdın okunurluk ölçüsü (saf düzenek) ve koparılan kâğıdın yolu.
    /// </summary>
    public sealed class TelexTypewriterSceneTests
    {
        const string ScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const string TypewriterPrefab = "Assets/Typewriter/Prefabs/Typewriter_FBX_Unity.prefab";
        const string MeshDir = "Assets/Corrigible/Desk/Art/Rev3/SceneMeshes/Telex/";

        Scene scene, previous;
        GameObject[] roots;
        bool userScene, opened;

        [OneTimeSetUp]
        public void Prepare()
        {
            var existing = SceneManager.GetSceneByPath(ScenePath);
            userScene = existing.IsValid() && existing.isLoaded;
            previous = SceneManager.GetActiveScene();
        }

        [OneTimeTearDown]
        public void CloseSavedSceneWithoutSaving()
        {
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }

        void RequireScene()
        {
            if (userScene) Assert.Ignore("Sahne doğrulaması boş sahne açıkken çalıştırılmalı; kullanıcının açık sahnesine dokunulmadı.");
            if (opened) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            roots = scene.GetRootGameObjects();
            opened = true;
        }

        T One<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();

        static T Reference<T>(Object owner, string field) where T : Object
        {
            var property = new SerializedObject(owner).FindProperty(field);
            Assert.IsNotNull(property, owner.GetType().Name + "." + field);
            return property.objectReferenceValue as T;
        }

        static Bounds LocalBounds(Transform frame, Renderer r)
        {
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            var b = new Bounds(frame.InverseTransformPoint(r.transform.TransformPoint(mesh.vertices[0])), Vector3.zero);
            foreach (var v in mesh.vertices) b.Encapsulate(frame.InverseTransformPoint(r.transform.TransformPoint(v)));
            return b;
        }

        /// <summary>
        /// Daktilo Teleprinter'ın altında, kullanıcının prefab örneği, çarpıştırıcısız; eski Rev3 Telex gövdesi sahnede yok. Kullanıcının
        /// koyduğu yer, ölçek ve malzeme asıldır: Teleprinter yerelinde (−0,0053; −0,004; −0,097), y 0,1°, ölçek 1; prefab ezmeleri
        /// yalnız kök nesnenin adı/etkinliği ve kök dönüşümü (malzeme ya da mesh ezmesi yok).
        /// </summary>
        [Test]
        public void Typewriter_IsTheTelexModel_OldBodyGone()
        {
            RequireScene();
            var printer = One<Teleprinter>();
            var tp = printer.transform;
            var typewriter = tp.Find("Typewriter_FBX_Unity");
            Assert.IsNotNull(typewriter, "daktilo Teleprinter'ın çocuğu");
            Assert.IsTrue(PrefabUtility.IsOutermostPrefabInstanceRoot(typewriter.gameObject), "kullanıcının prefab örneği");
            Assert.AreEqual(TypewriterPrefab, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(typewriter.gameObject));
            Assert.IsEmpty(typewriter.GetComponentsInChildren<Collider>(true), "model çarpıştırıcısız (SiginakRoomTests kuralı)");
            Assert.Greater(typewriter.GetComponentsInChildren<MeshRenderer>(true).Length, 50, "daktilonun parçaları");
            Assert.IsFalse(roots.SelectMany(r => r.GetComponentsInChildren<Rev3Model>(true)).Any(m => m.ModelName == "Telex"), "eski Telex modeli yok");
            Assert.AreSame(printer, Reference<Teleprinter>(One<TelexFeed>(), "printer"));

            Assert.AreSame(tp, typewriter.parent);
            Assert.AreEqual(-0.0053f, typewriter.localPosition.x, 1e-4f, "kullanıcının yeri (x)");
            Assert.AreEqual(-0.004f, typewriter.localPosition.y, 1e-4f, "kullanıcının yeri (y)");
            Assert.AreEqual(-0.097f, typewriter.localPosition.z, 1e-4f, "kullanıcının yeri (z)");
            Assert.AreEqual(0.1f, Mathf.DeltaAngle(0, typewriter.localEulerAngles.y), 0.05f, "kullanıcının dönüşü (y 0,1°)");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0, typewriter.localEulerAngles.x), 0.01f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0, typewriter.localEulerAngles.z), 0.01f);
            Assert.Less((typewriter.localScale - Vector3.one).magnitude, 1e-5f, "kullanıcının ölçeği (1)");
            foreach (var m in PrefabUtility.GetPropertyModifications(typewriter.gameObject))
            {
                bool root = m.target is GameObject g ? g.name == "Typewriter_FBX_Unity" && (m.propertyPath == "m_Name" || m.propertyPath == "m_IsActive")
                    : m.target is Transform t && t.parent == null && m.propertyPath.StartsWith("m_Local");
                Assert.IsTrue(root, "kök dışında ezme yok (malzeme ve mesh kullanıcının prefab'ındaki gibi): " +
                    (m.target != null ? m.target.name + "/" + m.target.GetType().Name : "null") + " " + m.propertyPath + " = " + m.value);
            }
            Assert.IsEmpty(PrefabUtility.GetAddedComponents(typewriter.gameObject), "daktiloya bileşen eklenmedi");
            Assert.IsEmpty(PrefabUtility.GetAddedGameObjects(typewriter.gameObject), "daktiloya nesne eklenmedi");
            Assert.IsEmpty(PrefabUtility.GetRemovedComponents(typewriter.gameObject), "daktilodan bileşen silinmedi");
        }

        /// <summary>
        /// Teleks kâğıdı silindirden çıkar: türev mesh (yalnız ön kâğıt, şerit ve mandal yok), tek telexPaper malzemesi; alt kenarı
        /// silindirin altında (y &lt; 0,105), üstü ~0,3 m'de, arkaya yaslanır; dört yuva silindirin üstünde; önceki satır yuvası ileti
        /// yuvasının altında ama silindirin (K1'den görünen kâğıt ~0,142'de başlar) en az 25 mm üstünde, kâğıdın alt kenarına gömülmez;
        /// kopya kâğıdın yanında; <c>paperRenderer</c> bu kâğıt.
        /// </summary>
        [Test]
        public void TelexPaper_RisesFromPlaten_FourSlotsAbove()
        {
            RequireScene();
            var printer = One<Teleprinter>();
            var tp = printer.transform;
            var sheet = Reference<Renderer>(printer, "paperRenderer");
            Assert.IsNotNull(sheet, "paperRenderer");
            Assert.AreEqual("Body", sheet.name);
            Assert.AreEqual("TelexPaper", sheet.transform.parent.name);
            Assert.IsTrue(sheet.gameObject.activeInHierarchy, "kâğıt görünür (eski kapalı gövde değil)");
            var mesh = sheet.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(MeshDir + "TypewriterTelexPaper_SM.asset", AssetDatabase.GetAssetPath(mesh));
            Assert.AreEqual(24, mesh.triangles.Length / 3, "yalnız ön kâğıt");
            Assert.AreEqual(1, sheet.sharedMaterials.Length);
            Assert.AreEqual("telexPaper", sheet.sharedMaterial.name);

            var b = LocalBounds(tp, sheet);
            Assert.Less(b.min.y, 0.105f, "alt kenar silindirin altında (K1'den görünmez)");
            Assert.Greater(b.min.y, 0.05f);
            Assert.Greater(b.max.y, 0.29f, "kâğıt silindirin ~15 cm üstüne çıkar");
            Assert.Greater(b.max.z, b.min.z + 0.02f, "arkaya yaslanır");
            foreach (var slot in new[] { "frontCode", "frontStatus", "frontMessage", "frontPrev" })
            {
                var text = Reference<TMP_Text>(printer, slot);
                Assert.IsNotNull(text, slot);
                Assert.IsTrue(text.transform.IsChildOf(sheet.transform), slot + " kâğıdın üstünde");
                Assert.Greater(tp.InverseTransformPoint(text.transform.position).y, 0.14f, slot + " silindirin üstünde");
            }
            float RectY(TMP_Text text, bool top)
            {
                var c = new Vector3[4];
                text.rectTransform.GetWorldCorners(c);
                return top ? c.Max(v => tp.InverseTransformPoint(v).y) : c.Min(v => tp.InverseTransformPoint(v).y);
            }
            var prev = Reference<TMP_Text>(printer, "frontPrev");
            var message = Reference<TMP_Text>(printer, "frontMessage");
            Assert.GreaterOrEqual(RectY(prev, false), 0.142f + 0.025f - 1e-4f, "önceki satır silindirin en az 25 mm üstünde");
            Assert.LessOrEqual(RectY(prev, true), RectY(message, false) + 1e-4f, "önceki satır ileti yuvasının altında");
            var torn = Reference<Transform>(printer, "torn");
            Assert.Less(Vector3.Distance(torn.position, sheet.bounds.center), 0.06f, "koparılan kopya kâğıdın yerinde");
            Assert.IsFalse(torn.gameObject.activeSelf);
        }

        /// <summary>Rulo daktilonun arkasında masada (Teleprinter.roll), besleme şeridi silindirin arkasına çıkar; ikisi de çarpıştırıcısız.</summary>
        [Test]
        public void Roll_BehindTypewriter_Wired()
        {
            RequireScene();
            var printer = One<Teleprinter>();
            var tp = printer.transform;
            var roll = Reference<Transform>(printer, "roll");
            Assert.IsNotNull(roll, "Teleprinter.roll");
            Assert.AreEqual("Paper roll", roll.name);
            Assert.AreSame(tp, roll.parent);
            Assert.AreEqual(MeshDir + "TypewriterTelexRoll_SM.asset", AssetDatabase.GetAssetPath(roll.GetComponent<MeshFilter>().sharedMesh));
            var typewriter = tp.Find("Typewriter_FBX_Unity");
            float back = typewriter.GetComponentsInChildren<MeshRenderer>(true).Max(r => LocalBounds(tp, r).max.z);
            var rb = LocalBounds(tp, roll.GetComponent<MeshRenderer>());
            Assert.Greater(rb.min.z, back, "rulo daktilonun arkasında");
            Assert.AreEqual(-0.004f, rb.min.y, 0.002f, "rulo masada");
            var feed = tp.Find("Paper feed");
            Assert.IsNotNull(feed);
            Assert.AreEqual(MeshDir + "TypewriterTelexFeed_SM.asset", AssetDatabase.GetAssetPath(feed.GetComponent<MeshFilter>().sharedMesh));
            Assert.IsEmpty(roll.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(feed.GetComponentsInChildren<Collider>(true));
        }

        /// <summary>TELEKS plakası daktilonun kapağının önünde (pirinç levha + CP yazı, DeskLocText "TELEKS"), yazı levhanın ortasında.</summary>
        [Test]
        public void Plate_OnTypewriterHood()
        {
            RequireScene();
            var tp = One<Teleprinter>().transform;
            var plate = tp.Find("Telex plate");
            Assert.IsNotNull(plate, "Telex plate");
            Assert.AreEqual(MeshDir + "TelexPlate_SM.asset", AssetDatabase.GetAssetPath(plate.GetComponent<MeshFilter>().sharedMesh));
            var text = plate.Find("TXT_Plate");
            Assert.IsNotNull(text);
            Assert.AreEqual("TELEKS", text.GetComponent<DeskLocText>().Source);
            var rt = (RectTransform)text;
            Assert.Less(rt.anchoredPosition.magnitude, 1e-5f, "yazı levhanın ortasında");
            Assert.Less(Mathf.Abs(rt.localPosition.z), 0.005f);
            var local = tp.InverseTransformPoint(plate.position);
            Assert.Less(local.z, -0.1f, "kapağın önünde");
            Assert.That(local.y, Is.InRange(0.11f, 0.15f));
            Assert.Greater(Vector3.Dot(plate.forward * -1, tp.TransformDirection(new Vector3(0, 0.5f, -1).normalized)), 0.9f, "yüz öne ve yukarı");
            Assert.IsEmpty(plate.GetComponentsInChildren<Collider>(true));
        }

        /// <summary>
        /// Hedefler: kökteki TELEKS kutusu daktilonun gövdesinde (üstü fişin çıkışının altında, silindirdeki fiş ışını alır); kâğıdın
        /// kendi eğik kutusu aynı başlıkla; ikisi fişin kutusuyla x'te örtüşmez.
        /// </summary>
        [Test]
        public void Targets_BodyAndPaper_LeaveSlipClickable()
        {
            RequireScene();
            var printer = One<Teleprinter>();
            var tp = printer.transform;
            var paper = One<ShiftReceiptPaper>();
            var exit = Reference<Transform>(paper, "output");
            var box = tp.GetComponent<BoxCollider>();
            Assert.IsNotNull(box);
            Assert.LessOrEqual(box.center.y + box.size.y / 2, tp.InverseTransformPoint(exit.position).y + 1e-4f, "kök kutunun üstü çıkışın altında");
            var target = tp.Find("Paper target");
            Assert.IsNotNull(target);
            var dt = target.GetComponent<DeskTarget>();
            Assert.IsNotNull(dt);
            Assert.AreEqual(tp.GetComponent<DeskTarget>().Title, dt.Title, "aynı başlık (TELEKS)");
            var tbox = target.GetComponent<BoxCollider>();
            float targetMaxX = tp.InverseTransformPoint(target.TransformPoint(tbox.center + new Vector3(tbox.size.x / 2, 0, 0))).x;
            float slipMinX = tp.InverseTransformPoint(paper.TongueTarget.position).x - 0.05f;
            Assert.Less(targetMaxX, slipMinX, "kâğıdın hedefi fişin solunda biter");
        }

        /// <summary>
        /// Kâğıdın okunurluğu yalnız ekrandaki boydan (≥ 14 px) gelir: yakında okunur, uzakta okunmaz (satır şeride de düşer). Devir
        /// fişi kâğıdı örtmediği için ayrı bir örtü bayrağı yok.
        /// </summary>
        [Test]
        public void TelexFeed_Readable_BySizeOnly()
        {
            var go = new GameObject("Teleprinter");
            var camGo = new GameObject("Kamera");
            var texture = new RenderTexture(1920, 1080, 0);
            try
            {
                var text = new GameObject("Kâğıt", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
                text.transform.SetParent(go.transform, false);
                text.rectTransform.sizeDelta = new Vector2(0.4f, 0.1f);
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 0.5f;
                text.text = "001/01  ÖN KONTROL";
                var printer = go.AddComponent<Teleprinter>();
                printer.Configure(text, text.GetComponent<MeshRenderer>(), "TELEKS");
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                cam.targetTexture = texture;
                cam.transform.SetPositionAndRotation(new Vector3(0, 0, -0.3f), Quaternion.identity);
                cam.fieldOfView = 40;
                var feed = go.AddComponent<TelexFeed>();
                feed.Configure(printer, null, cam);
                text.ForceMeshUpdate();
                Assert.IsTrue(feed.PrinterReadable, "yakında okunur");
                cam.transform.position = new Vector3(0, 0, -10f);
                Assert.IsFalse(feed.PrinterReadable, "uzakta okunmaz (satır şeritte de)");
                cam.transform.position = new Vector3(0, 0, -0.3f);
                Assert.IsTrue(feed.PrinterReadable);
            }
            finally
            {
                camGo.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Koparılan teleks kâğıdının yolu (<see cref="Teleprinter.TearPose(float, SiginakTheme)"/>, token <c>motion.telexTear.*</c>):
        /// öne (klavyeye) hiç gitmez, merkezi hep daktilonun kapağının (y 0,15) üstünde kalır, yukarı çekilip arkaya düşer; uçlarda
        /// 0 ve token değerleri. Sahnedeki kopyanın dinlenme yerinden ölçülür.
        /// </summary>
        [Test]
        public void TornSheet_PulledUpAndBack_NeverThroughBody()
        {
            RequireScene();
            var printer = One<Teleprinter>();
            var torn = Reference<Transform>(printer, "torn");
            Assert.AreSame(printer.transform, torn.parent, "kopya Teleprinter yerelinde");
            var theme = SiginakTheme.Active;
            Assert.IsNotNull(theme);
            var start = Teleprinter.TearPose(0, theme);
            Assert.Less(start.position.magnitude, 1e-6f);
            Assert.Less(Quaternion.Angle(Quaternion.identity, start.rotation), 1e-3f);
            float peak = torn.localPosition.y;
            for (int i = 1; i <= 20; i++)
            {
                var p = Teleprinter.TearPose(i / 20f, theme);
                var at = torn.localPosition + p.position;
                Assert.GreaterOrEqual(at.z, torn.localPosition.z - 1e-5f, "öne (klavyeye) gitmez (t " + i / 20f + ")");
                Assert.Greater(at.y, 0.15f, "merkez kapağın üstünde (t " + i / 20f + ")");
                peak = Mathf.Max(peak, at.y);
            }
            Assert.Greater(peak, torn.localPosition.y + 0.02f, "önce yukarı çekilir");
            var end = Teleprinter.TearPose(1, theme);
            Assert.Greater(end.position.z, 0.1f, "arkaya atılır");
            Assert.AreEqual(theme.NumberOf("motion.telexTear.moveMm[1]") - theme.NumberOf("motion.telexTear.fallMm"), end.position.y * 1000f, 1e-3f);
            Assert.AreEqual(theme.NumberOf("motion.telexTear.euler[0]"), Mathf.DeltaAngle(0, end.rotation.eulerAngles.x), 0.01f, "üstü arkaya yatar");
        }
    }
}
