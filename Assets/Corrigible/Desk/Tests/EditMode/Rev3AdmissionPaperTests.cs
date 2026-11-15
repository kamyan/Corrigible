using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 6 kabul kâğıtlarının ortak kuralları (PLAN W3, §1.16): prefab kuralları (ışıksız TMP, raycast yok, kâğıt albedosu
    /// ≤ 0,8) aday kartı ve grup fişinde, kâğıt malzemeleri, dosya etiketi (FileTag TR/EN yuvaya sığar), kişi dosyasının etiketi bağlaması.
    /// Eski tek adaylı kabulün kâğıtları (belge, talimat şeridi, bulgu fişi, talep formu), buğu camı (malzeme, doku) ve kişi
    /// dosyasının sekmeleri aşama 7'de söküldü; testleri de. Testler açık sahneye dokunmaz (yalnız örnek oluşturup siler).
    /// </summary>
    public class Rev3AdmissionPaperTests
    {
        const string FileTagPath = Rev3ModelImporter.PrefabsDir + "/admission/FileTag.prefab";
        const string MaterialDir = Rev3ModelImporter.ArtDir + "/SceneMaterials/admission";

        readonly List<Object> spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.DestroyImmediate(o);
            spawned.Clear();
            Loc.SetLanguage(Loc.Turkish);
        }

        // ================================================================== prefab ve malzeme kuralları

        [Test]
        public void Faces_UnlitInk_NoRaycast()
        {
            var desk = DeskTheme.Active;
            var fonts = new[] { desk.Font(DeskFont.CpBold), desk.Font(DeskFont.LfRegular), desk.Font(DeskFont.LfSemiBold), desk.Font(DeskFont.KalamBold) };
            var cases = new (string path, System.Type type, Vector2 mm)[]
            {
                // KOS aşama 2: aday kartı ve kapı fişi
                (AdmissionFixtures.CardPrefab, typeof(CandidateCardFace), new Vector2(CandidateCardFace.WidthMm, CandidateCardFace.HeightMm)),
                (AdmissionFixtures.GroupSlipPrefab, typeof(CandidateGroupSlipFace), new Vector2(CandidateGroupSlipFace.WidthMm, CandidateGroupSlipFace.HeightMm)),
            };
            foreach (var c in cases)
            {
                var prefab = Load(c.path);
                var face = (AdmissionPaperFace)prefab.GetComponent(c.type);
                Assert.IsNotNull(face, c.path + ": kökte yüz");
                Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(b => b == null), c.path + ": eksik script yok");
                Assert.IsEmpty(prefab.GetComponentsInChildren<GraphicRaycaster>(true), c.path + ": GraphicRaycaster yok");
                Assert.IsEmpty(prefab.GetComponentsInChildren<CanvasScaler>(true), c.path);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Mask>(true), c.path);

                // Tuval: World Space, kâğıt ölçüsü, 1 birim = 1 mm, sol üst pivot kâğıdın sol üst köşesinde, 0,4 mm önde (ön yüz −z)
                var canvasRect = face.FaceRect;
                var canvas = canvasRect.GetComponent<Canvas>();
                Assert.IsNotNull(canvas, c.path);
                Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, c.path);
                Near(c.mm, canvasRect.sizeDelta, 1e-3f, c.path + ": tuval");
                Near(new Vector2(0, 1), canvasRect.pivot, 1e-6f, c.path);
                Near(Vector3.one * 0.001f, canvasRect.localScale, 1e-7f, c.path);
                Near(new Vector3(-c.mm.x / 2000f, c.mm.y / 2000f, -0.0004f), canvasRect.localPosition, 1e-5f, c.path + ": sol üst köşe, kâğıdın 0,4 mm önünde");
                Assert.IsFalse(face.Content.gameObject.activeSelf, c.path + ": boş yüz (veri gelene dek)");

                // Kâğıt: Quad, kâğıt ölçüsünde, gölge düşürmez; ince çarpıştırıcı (okuma tıkı)
                var paper = prefab.GetComponentsInChildren<MeshRenderer>(true).Single();
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, paper.shadowCastingMode, c.path);
                Near(new Vector3(c.mm.x / 1000f, c.mm.y / 1000f, 1f), paper.transform.localScale, 1e-6f, c.path + ": Quad ölçüsü");
                var albedo = paper.sharedMaterial.GetColor("_BaseColor");
                Assert.LessOrEqual(Mathf.Max(albedo.r, albedo.g, albedo.b), 0.8f + 1e-4f, c.path + ": kâğıt albedosu ≤ 0,8 (admission.paperAlbedo)");
                var box = prefab.GetComponent<BoxCollider>();
                Assert.IsNotNull(box, c.path);
                Near(new Vector3(c.mm.x / 1000f, c.mm.y / 1000f, 0.002f), box.size, 1e-6f, c.path + ": çarpıştırıcı");

                // Yazılar: uGUI, fontun kendi (ışıksız) malzemesi, raycast yok, otomatik punto yok
                foreach (var t in prefab.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.IsInstanceOf<TextMeshProUGUI>(t, c.path + " " + t.name);
                    CollectionAssert.Contains(fonts, t.font, c.path + " " + t.name + ": proje fontu");
                    Assert.AreSame(t.font.material, t.fontSharedMaterial, c.path + " " + t.name + ": fontun kendi (ışıksız) malzemesi");
                    Assert.IsFalse(t.enableAutoSizing, t.name);
                    Assert.IsFalse(t.raycastTarget, t.name);
                }
                foreach (var g in prefab.GetComponentsInChildren<Graphic>(true)) Assert.IsFalse(g.raycastTarget, c.path + " " + g.name + ": raycastTarget");
                // Sabit etiketlerin kaynağı yazının kendisi (Türkçe)
                foreach (var loc in prefab.GetComponentsInChildren<DeskLocText>(true))
                    Assert.AreEqual(loc.Source, loc.GetComponent<TMP_Text>().text, c.path + " " + loc.name + ": DeskLocText kaynağı");
                AssertWired(face, c.path);
            }
        }

        [Test]
        public void Materials_PapersMatchTheirSources()
        {
            if (!AssetDatabase.IsValidFolder(MaterialDir)) Assert.Ignore("Aşama 6 kâğıt kurulumu yapılmamış: " + MaterialDir);
            foreach (var name in new[] { "Admission paper", "Admission document" })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/" + name + ".mat");
                Assert.IsNotNull(m, name);
                var c = m.GetColor("_BaseColor");
                Assert.LessOrEqual(Mathf.Max(c.r, c.g, c.b), 0.8f + 1e-4f, name + ": albedo ≤ 0,8");
                Assert.AreEqual(0f, m.GetFloat("_Surface"), name + ": opak");
            }
        }

        // ================================================================== FileTag

        [Test]
        public void FileTag_TrEnFits()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FileTagPath);
            Assert.IsNotNull(prefab, FileTagPath + " (Aşama 1 model paketi)");
            var go = (GameObject)Object.Instantiate(prefab);
            spawned.Add(go);
            var label = go.GetComponentsInChildren<TMP_Text>(true).Single();
            var view = go.AddComponent<FileTagView>();
            view.Configure(label);
            float design = label.fontSize;
            var box = label.rectTransform.rect.size;
            Near(new Vector2(0.046f, 0.02f), box, 1e-5f, "TXT_Tag yuvası 46 × 20 mm");
            foreach (var text in new[] { "İZ 2", "TECRİT · KARAR", "KISITLI · 3 V", "LIMIT · 3 S", "KISITLI · 12 V", "LIMIT · 12 S" })
            {
                view.Bind(text);
                Assert.IsTrue(go.activeSelf, text);
                Assert.AreEqual(text, view.Text);
                Assert.IsFalse(view.Overflowing, text + ": yuvaya sığar (punto %" + (view.Scale * 100).ToString("0") + ")");
                label.ForceMeshUpdate(true, true);
                var b = label.textBounds.size;
                Assert.LessOrEqual(b.x, box.x + 0.0003f, text + ": genişlik (textBounds; preferredWidth 0,01 birime yuvarlanır)");
                Assert.LessOrEqual(b.y, box.y + 0.0003f, text + ": yükseklik");
                Assert.GreaterOrEqual(label.fontSize, design * FileTagView.MinScale - 1e-6f, text);
                var info = label.textInfo;
                for (int l = 1; l < info.lineCount; l++)
                {
                    int first = info.lineInfo[l].firstCharacterIndex;
                    Assert.IsTrue(char.IsWhiteSpace(info.characterInfo[first - 1].character), text + ": sözcük bölünmez");
                    Assert.AreNotEqual('·', info.characterInfo[first].character, text + ": satır noktayla başlamaz");
                }
                TestContext.WriteLine(text + ": punto %" + (view.Scale * 100).ToString("0") + ", " + info.lineCount + " satır, " + (b.x * 1000).ToString("0.0") + " × " + (b.y * 1000).ToString("0.0") + " mm");
            }
            Assert.AreEqual(1, Fit(view, label, "İZ 2"), "kısa etiket tek satır, yuvanın puntosunda");
            Assert.AreEqual(1f, view.Scale, 1e-6f, "İZ 2 küçülmez");
            view.Bind(null);
            Assert.IsFalse(go.activeSelf, "etiket yok: gizli");
            Assert.IsNull(view.Text);
            view.Bind("");
            Assert.IsFalse(go.activeSelf);
            view.Bind("İZ 2");
            Assert.IsTrue(go.activeSelf, "yeniden takılır");
        }

        [Test]
        public void PersonFolder_OpenBindsTheTag_CloseRemovesIt()
        {
            var folderGo = new GameObject("Kişi dosyası (test)");
            spawned.Add(folderGo);
            var folder = folderGo.AddComponent<PersonFolderView>();
            var tagGo = new GameObject("FileTag (test)");
            tagGo.transform.SetParent(folderGo.transform, false);
            var tag = tagGo.AddComponent<FileTagView>();
            typeof(PersonFolderView).GetField("fileTag", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(folder, tag);
            Assert.AreSame(tag, folder.FileTag);

            folder.Open(new FolderData { PersonId = "resident-10", Name = "Eren", TagText = "İZ 1" });
            Assert.IsTrue(tagGo.activeSelf, "izli dosyada etiket");
            Assert.AreEqual("İZ 1", tag.Text);
            folder.Open(new FolderData { PersonId = "resident-10", Name = "Eren", TagText = "KISITLI · 2 V" });
            Assert.AreEqual("KISITLI · 2 V", tag.Text, "açıkken veri yenilenince etiket de");
            folder.Close();
            Assert.IsFalse(tagGo.activeSelf, "kapanınca kalkar (seçim değişince bayat kalmaz)");
            folder.Open(new FolderData { PersonId = "resident-4", Name = "Suna", TagText = null });
            Assert.IsFalse(tagGo.activeSelf, "etiket yok");
            folder.SetTag("İZ 1");
            Assert.IsTrue(tagGo.activeSelf, "sunucu doğrudan takabilir");
        }

        // ================================================================== yardımcılar

        static GameObject Load(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p == null) Assert.Ignore("Kâğıt prefab'ı yok (KOS aşama 2 kurulumu çalışmamış): " + path);
            return p;
        }

        /// <summary>
        /// Unity'nin her bileşende serileştirdiği iç nesne alanları (<c>Object</c>/<c>Component</c>/<c>MonoBehaviour</c> tabanı).
        /// Prefab olmayan ya da prefab kaynağının kendisi olan nesnede <c>m_CorrespondingSourceObject</c>/<c>m_PrefabInstance</c>
        /// doğal olarak boştur; bunlar yüzün bağları değildir.
        /// </summary>
        static readonly HashSet<string> UnityInternalRefs = new HashSet<string>
        {
            "m_Script", "m_GameObject", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
        };

        /// <summary>Yüzün bütün [SerializeField] nesne bağları dolu (dizi öğeleri ve çerçeveler dahil; Unity iç alanları hariç).</summary>
        static void AssertWired(Object face, string what)
        {
            var so = new SerializedObject(face);
            var p = so.GetIterator();
            var missing = new List<string>();
            bool enter = true;
            while (p.Next(enter))
            {
                enter = true;
                if (p.depth == 0 && UnityInternalRefs.Contains(p.propertyPath)) { enter = false; continue; }
                if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null) missing.Add(p.propertyPath);
            }
            Assert.IsEmpty(missing, what + ": bağlanmamış alanlar: " + string.Join(", ", missing));
        }

        static int Fit(FileTagView view, TMP_Text label, string text)
        {
            view.Bind(text);
            label.ForceMeshUpdate(true, true);
            return label.textInfo.lineCount;
        }

        static void Near(Vector2 expected, Vector2 actual, float tol, string what)
        {
            Assert.AreEqual(expected.x, actual.x, tol, what + " x");
            Assert.AreEqual(expected.y, actual.y, tol, what + " y");
        }

        static void Near(Vector3 expected, Vector3 actual, float tol, string what)
        {
            Assert.AreEqual(expected.x, actual.x, tol, what + " x");
            Assert.AreEqual(expected.y, actual.y, tol, what + " y");
            Assert.AreEqual(expected.z, actual.z, tol, what + " z");
        }
    }
}
