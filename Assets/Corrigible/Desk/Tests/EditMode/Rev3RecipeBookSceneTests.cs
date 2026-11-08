using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
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
    /// Revizyon 3 · Aşama 5 (R18; PLAN §2.3, W6) kaydedilmiş DeskGame: tarif defterinin çarpıştırıcıları K4'te örtülmez ve K1'de pano
    /// erişimini çalmaz (E26), sayfa tuvalleri sayfa yüzlerinin önünde, yeni kod ada göre aramaz (E28), tarif kartı prefab'ı bağlı,
    /// sahnedeki yeni yazılar token ve çeviri tablosuyla tutarlı. Sahne gerektiren testler kullanıcının açık DeskGame'ine dokunmaz
    /// (<c>Assert.Ignore</c>); boş sahnede koş.
    /// </summary>
    public sealed class Rev3RecipeBookSceneTests
    {
        const string ScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const string StringsPath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const string CardDir = "Assets/Corrigible/Desk/Prefabs/Rev3Board/";
        const string SoundBankPath = "Assets/Corrigible/Settings/SoundBank.asset";

        Scene scene, previous;
        bool opened;
        PlanningBoardPresenter presenter;

        // ================================================================== sahne

        /// <summary>Sahneyi ilk istekte açar (kullanıcının açık DeskGame'i varsa yoksayar).</summary>
        void Open()
        {
            if (opened) return;
            var existing = SceneManager.GetSceneByPath(ScenePath);
            if (existing.IsValid() && existing.isLoaded)
                Assert.Ignore("Sahne doğrulaması boş sahne açıkken çalıştırılmalı; kullanıcının açık sahnesine dokunulmadı.");
            previous = SceneManager.GetActiveScene();
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            opened = true;
            presenter = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlanningBoardPresenter>(true)).Single();
        }

        [OneTimeTearDown]
        public void CloseWithoutSaving()
        {
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            opened = false;
        }

        BoardView3D View3D => presenter.GetComponent<BoardView3D>();
        Rev3Model Model() => presenter.GetComponentsInChildren<Rev3Model>(true).Single(m => m.ModelName == "PlanningBoard");
        RecipeBookView Book() => Reference<RecipeBookView>(View3D, "book");
        SiginakCameraRig Rig() => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SiginakCameraRig>(true)).Single();

        // ================================================================== E26 · ışın

        /// <summary>
        /// E26: K4 kamerasından (Cam_K4) kapalı defterin merkezine atılan ışının ilk çarpıştırıcısı defterin gövdesi (K4 etkileşimi
        /// taklit edilir: Pano erişimi kapalı, defter gövdesi açık; sabit hedefler, KAYIPLAR, dosya önüne geçmez). K1'de (kaydedilmiş
        /// hâl: defter etkileşimsiz, çarpıştırıcıları kapalı) aynı ışın panoda önce Pano erişimi kutusuna çarpar: defter tıkı çalmaz.
        /// Okuma pozunda (kök <see cref="RecipeBookView.ReadPoseLocal"/>, kapak −178°, yaprak gizli) iki köşe sekmesinin merkezine atılan
        /// ışın o sayfanın çarpıştırıcısına, sekmenin bölgesinde çarpar.
        /// </summary>
        [Test]
        public void BookHit_NotOccluded_K4_And_NotStealingK1()
        {
            Open();
            var model = Model();
            var view = Book();
            var closedHit = Reference<BoxCollider>(view, "closedHit");
            var entry = Reference<Collider>(View3D, "boardEntry");
            Assert.IsFalse(closedHit.enabled, "kaydedilmiş sahnede defter gövdesi kapalı (yalnız K4 etkileşimliyken açılır)");
            Assert.IsTrue(entry.enabled, "Pano erişimi K1'de açık");
            var rig = Rig();
            Assert.IsTrue(rig.TryGetPose(SiginakPose.Shelf, out var k4), "K4 pozu");
            Assert.IsTrue(rig.TryGetPose(SiginakPose.Desk, out var k1), "K1 pozu");
            Assert.AreSame(model.Anchor("Cam_K4"), k4.anchor);
            var centre = closedHit.transform.TransformPoint(closedHit.center);

            // K1: kaydedilmiş hâl
            Physics.SyncTransforms();
            var k1Hits = Hits(k1.anchor.position, centre);
            Assert.IsFalse(k1Hits.Any(h => h.collider.transform.IsChildOf(view.transform)), "K1'de defter çarpıştırıcısı ışında yok");
            var k1Board = k1Hits.FirstOrDefault(h => h.collider.transform.IsChildOf(presenter.transform));
            Assert.IsNotNull(k1Board.collider, "K1'den defterin yönündeki ışın panoya ulaşır");
            Assert.AreSame(entry, k1Board.collider, "K1'de panodaki ilk çarpışma Pano erişimi: " + k1Board.collider.name);

            // K4 etkileşimi: Pano erişimi kapanır, kapalı defterin gövdesi açılır (BoardView3D.ApplyInteractivity, RecipeBookView.UpdateColliders)
            try
            {
                entry.enabled = false;
                closedHit.enabled = true;
                Physics.SyncTransforms();
                var k4Hits = Hits(k4.anchor.position, centre);
                Assert.IsNotEmpty(k4Hits, "K4'ten defterin merkezine ışın");
                Assert.AreSame(closedHit, k4Hits[0].collider, "K4'te defterin önünde başka çarpıştırıcı yok: ilk çarpışma " + k4Hits[0].collider.name);
            }
            finally
            {
                closedHit.enabled = false;
                entry.enabled = true;
            }

            // Okuma pozu: köşe sekmeleri sayfa çarpıştırıcılarında
            var root = view.Root;
            var cover = Reference<PartDriver>(view, "cover");
            var leaf = Reference<PartDriver>(view, "leaf");
            var pageLeft = Reference<RecipeBookPage>(view, "pageLeft");
            var pageRight = Reference<RecipeBookPage>(view, "pageRight");
            Vector3 rootPos = root.position; Quaternion rootRot = root.rotation; Quaternion coverRot = cover.transform.localRotation;
            try
            {
                var read = RecipeBookView.ReadPoseLocal;
                root.SetPositionAndRotation(model.transform.TransformPoint(read.position), model.transform.rotation * read.rotation);
                cover.Set(-178f);
                leaf.gameObject.SetActive(false);
                foreach (var p in new[] { pageLeft, pageRight }) { p.gameObject.SetActive(true); p.Hit.enabled = true; }
                Physics.SyncTransforms();
                Assert.AreEqual(0.315f, Vector3.Distance(k4.anchor.position, root.TransformPoint(new Vector3(0f, 0.074f, 0f))), 0.03f,
                    "okuma pozunda sırt ortası kameraya ≈ 0,32 m (R18a)");
                foreach (var (page, right, mm) in new[] { (pageRight, true, new Vector2(91f, 132f)), (pageLeft, false, new Vector2(8f, 132f)) })
                {
                    var target = page.WorldAt(mm);
                    var hits = Hits(k4.anchor.position, target);
                    Assert.IsNotEmpty(hits, page.name);
                    Assert.AreSame(page.Hit, hits[0].collider, page.name + ": köşe sekmesinin önünde " + hits[0].collider.name);
                    var got = page.PageMm(hits[0].point);
                    Assert.Less(Vector2.Distance(mm, got), 1.5f, page.name + ": çarpma noktası sekmede " + got);
                    var zone = RecipeBookView.ZoneAt(right, got, right ? 0 : 1, 3, false);
                    Assert.AreEqual(right ? BookZone.Next : BookZone.Prev, zone, page.name);
                }
            }
            finally
            {
                foreach (var p in new[] { pageLeft, pageRight }) { p.Hit.enabled = false; p.gameObject.SetActive(false); }
                leaf.gameObject.SetActive(true);
                cover.transform.localRotation = coverRot;
                root.SetPositionAndRotation(rootPos, rootRot);
                Physics.SyncTransforms();
            }
        }

        /// <summary>Bu sahnenin çarpıştırıcılarına <paramref name="from"/>'dan <paramref name="to"/>'ya (biraz ötesine) ışın; yakından uzağa.</summary>
        List<RaycastHit> Hits(Vector3 from, Vector3 to)
        {
            var dir = to - from;
            return Physics.RaycastAll(from, dir.normalized, dir.magnitude + 0.05f, ~0, QueryTriggerInteraction.Collide)
                .Where(h => h.collider != null && h.collider.gameObject.scene == scene)
                .OrderBy(h => h.distance).ToList();
        }

        // ================================================================== sayfa tuvalleri

        /// <summary>
        /// Sayfa tuvalleri sayfa yüzlerinin önünde (Risk 4): sol tuval Cover yerelinde z ≥ 0,0090 ve Cover ağının sayfa yüzü (alt ağ 2)
        /// ile bütün ağın önünde (kapak açılınca +z okuyucuya döner); sağ tuval <c>TXT_PageR</c> yuvasında, gövdenin sayfa yüzünün önünde
        /// (−z); yaprak yüzlerinin tuvalleri kendi kâğıtlarının önünde, kâğıtlar Leaf ağının dışında.
        /// </summary>
        [Test]
        public void PageCanvases_InFrontOfPageFaces()
        {
            Open();
            var view = Book();
            var cover = Reference<PartDriver>(view, "cover");
            var leaf = Reference<PartDriver>(view, "leaf");
            var body = cover.transform.parent;
            var pageLeft = Reference<RecipeBookPage>(view, "pageLeft");
            var pageRight = Reference<RecipeBookPage>(view, "pageRight");
            var leafFront = Reference<RecipeBookPage>(view, "leafFront");
            var leafBack = Reference<RecipeBookPage>(view, "leafBack");

            var coverMesh = cover.GetComponent<MeshFilter>().sharedMesh;
            float coverFace = SubmeshZ(coverMesh, 2, max: true);
            float coverMax = coverMesh.vertices.Max(v => v.z);
            float zl = pageLeft.transform.localPosition.z;
            Assert.GreaterOrEqual(zl, 0.0090f, "sol tuval Cover yereli z ≥ 0,0090 (TXT_PageL yuvası 0,7 mm içeride)");
            Assert.Greater(zl, coverFace + 0.0002f, "sol tuval sol sayfa yüzünün ≥ 0,2 mm önünde (" + coverFace + ")");
            Assert.Greater(zl, coverMax, "sol tuval kapak ağının bütün köşelerinin önünde");
            Assert.Less(Vector3.Dot(pageLeft.transform.forward, cover.transform.forward), -0.999f, "sol tuval kapağın +z'sinden okunur (tuvalin yazı yüzü −ileri)");

            var bodyMesh = body.GetComponent<MeshFilter>().sharedMesh;
            float bodyFace = SubmeshZ(bodyMesh, 2, max: false);
            var slot = view.GetComponent<Rev3Model>().Slot("TXT_PageR");
            Assert.Less(Vector3.Distance(slot.transform.position, pageRight.transform.position), 1e-5f, "sağ tuval TXT_PageR yuvasında");
            Assert.Less(pageRight.transform.localPosition.z, bodyFace - 0.0002f, "sağ tuval sağ sayfa yüzünün ≥ 0,2 mm önünde (" + bodyFace + ")");

            var leafMesh = leaf.GetComponent<MeshFilter>().sharedMesh;
            float leafMin = leafMesh.vertices.Min(v => v.z), leafMax = leafMesh.vertices.Max(v => v.z);
            foreach (var (page, front) in new[] { (leafFront, true), (leafBack, false) })
            {
                var face = page.transform.parent;
                var paper = face.GetComponentsInChildren<MeshFilter>(true).Single(f => !f.transform.IsChildOf(page.transform)).transform;
                float paperZ = leaf.transform.InverseTransformPoint(paper.position).z;
                float pageZ = leaf.transform.InverseTransformPoint(page.transform.position).z;
                if (front)
                {
                    Assert.LessOrEqual(paperZ, leafMin + 1e-6f, "ön kâğıt yaprak ağının önünde (−z; kayan nokta payı)");
                    Assert.Less(pageZ, paperZ, "ön tuval kâğıdın önünde");
                    Assert.Greater(Vector3.Dot(page.transform.forward, leaf.transform.forward), 0.999f, "ön tuval −z'ye okunur");
                }
                else
                {
                    Assert.GreaterOrEqual(paperZ, leafMax - 1e-6f, "arka kâğıt yaprak ağının arkasında (+z; kayan nokta payı)");
                    Assert.Greater(pageZ, paperZ, "arka tuval kâğıdın arkasında (+z yüzünde)");
                    Assert.Less(Vector3.Dot(page.transform.forward, leaf.transform.forward), -0.999f, "arka tuval +z'ye okunur");
                }
                var size = Vector3.Scale(paper.GetComponent<MeshFilter>().sharedMesh.bounds.size, paper.localScale);
                Assert.AreEqual(0.099f, size.x, 1e-4f, face.name + " kâğıt genişliği");
                Assert.AreEqual(0.140f, size.y, 1e-4f, face.name + " kâğıt yüksekliği");
            }
        }

        static float SubmeshZ(Mesh mesh, int submesh, bool max)
        {
            Assert.Greater(mesh.subMeshCount, submesh, mesh.name + ": alt ağ " + submesh + " (pageFace)");
            var v = mesh.vertices;
            var zs = mesh.GetIndices(submesh).Select(i => v[i].z).ToList();
            return max ? zs.Max() : zs.Min();
        }

        // ================================================================== E28 · ada göre arama yok

        /// <summary>E28 (AGENTS kural 5): Aşama 5'in yeni ve değişen görünüm/köprü dosyalarında ada göre arama yok. Yorum satırları sayılmaz.</summary>
        [Test]
        public void BookCode_HasNoNameLookups()
        {
            var lookup = new Regex(@"GameObject\.Find\s*\(|\.Find\s*\(\s*""|\btransform\.Find\s*\(|FindWithTag\s*\(|FindGameObjectsWithTag\s*\(|FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|FindObjectsByType");
            const string rt = "Assets/Corrigible/Desk/Scripts/Runtime/PlanningBoard/Rev3/";
            const string br = "Assets/Corrigible/Desk/Scripts/Bridge/";
            var files = new[]
            {
                rt + "RecipeBookView.cs", rt + "RecipeBookPage.cs", rt + "BookPageShape.cs", rt + "BookTypes.cs", rt + "RecipeCardFace.cs",
                rt + "NewItemTag.cs", rt + "WorkOrderView.cs", rt + "StoreView.cs", rt + "BoardView3D.cs", rt + "BoardDragController.cs",
                rt + "BoardCardView.cs", br + "Board3D/ProductionForecast.cs", br + "Board3D/PlanningBoardSnapshotBuilder.cs",
                br + "Board3D/BoardDropRules.cs", br + "PlanningBoardPresenter.cs",
            };
            var hits = new List<string>();
            foreach (var file in files)
            {
                Assert.IsTrue(File.Exists(file), file);
                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].TrimStart();
                    if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("///", StringComparison.Ordinal)) continue;
                    if (lookup.IsMatch(line)) hits.Add(file + ":" + (i + 1) + "  " + line.Trim());
                }
            }
            Assert.IsEmpty(hits, "ada göre arama:\n" + string.Join("\n", hits));
        }

        // ================================================================== tarif kartı prefab'ı

        /// <summary>PLAN §1.6 / §2.3 #8: BoardView3D.recipePrefab = RecipeCard.prefab kökü (tür Recipe, kök kutu 112 × 80 × 4 mm,
        /// merkez z −2 mm, yüz RecipeCardFace World Space tuvalde, mıknatıs iç içe CardRecipe'in Magnet parçası); sahnede tarif kartı yok.</summary>
        [Test]
        public void RecipeCardPrefab_Wired()
        {
            Open();
            var card = Reference<BoardCardView>(View3D, "recipePrefab");
            Assert.IsTrue(EditorUtility.IsPersistent(card), "prefab varlığı");
            Assert.AreEqual(CardDir + "RecipeCard.prefab", AssetDatabase.GetAssetPath(card));
            Assert.AreEqual(BoardCardKind.Recipe, card.Kind);
            var box = card.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "kök çarpıştırıcı");
            Assert.Less(Vector3.Distance(new Vector3(0.112f, 0.08f, 0.004f), box.size), 1e-5f, "112 × 80 × 4 mm");
            Assert.Less(Vector3.Distance(new Vector3(0f, 0f, -0.002f), box.center), 1e-5f, "merkez z −2 mm");
            var face = card.GetComponentInChildren<RecipeCardFace>(true);
            Assert.IsNotNull(face, "RecipeCardFace");
            var canvas = face.GetComponentInParent<Canvas>(true);
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.IsNotNull(card.Model, "iç içe CardRecipe modeli");
            Assert.AreEqual("CardRecipe", card.Model.ModelName);
            Assert.AreSame(card.Model.Part("Magnet"), Reference<PartDriver>(card, "magnet"), "mıknatıs = modelin Magnet parçası");
            var cardRoot = Reference<Transform>(View3D, "cardRoot");
            Assert.AreEqual(0, cardRoot.childCount, "havuz kartları (tarif dahil) çalışma anında kurulur");
            Assert.IsEmpty(presenter.GetComponentsInChildren<RecipeCardFace>(true), "sahnede tarif kartı kayıtlı değil");
        }

        // ================================================================== yazılar, token, tablo

        /// <summary>
        /// Sahnedeki Aşama 5 yazıları: kapağın "TARİF DEFTERİ" ve "KYGA · 07" kaynaklarının İngilizcesi tabloda (RECIPE BOOK, KYGA · 07);
        /// kapak alt yazısı CP 700 em 6,2 mm rgba(233,223,194,0,65); YENİ levhası token <c>board.newTag.*</c> (32 × 13 mm, #B4291C,
        /// #F7EFD8, em 9,4 mm, ışıksız CP-Bold); DURDU damgası CP 700 em 9,4 mm, <c>stamp.stopped</c>, tasarım −6° (Unity z +6°), boş,
        /// formun içinde ve fiş yerleriyle çakışmaz.
        /// </summary>
        [Test]
        public void Texts_TokensAndTableAgree()
        {
            Open();
            var theme = SiginakTheme.Active;
            var desk = DeskTheme.Active;
            Assert.IsNotNull(theme, "SiginakTheme");
            Assert.IsNotNull(desk, "DeskTheme");
            var tables = Loc.ParseTable(File.ReadAllText(StringsPath));
            Assert.IsTrue(tables.TryGetValue(Loc.English, out var en), "EN tablosu");
            var problems = new List<string>();
            void Near(Color want, Color got, string what)
            {
                if (Mathf.Abs(want.r - got.r) > 0.006f || Mathf.Abs(want.g - got.g) > 0.006f || Mathf.Abs(want.b - got.b) > 0.006f || Mathf.Abs(want.a - got.a) > 0.006f)
                    problems.Add(what + ": renk #" + ColorUtility.ToHtmlStringRGBA(got) + " ≠ #" + ColorUtility.ToHtmlStringRGBA(want));
            }
            var model = Model();

            // Kapak
            var book = Book().GetComponent<Rev3Model>();
            var coverText = book.Slot("TXT_Cover").Text;
            if (!en.TryGetValue("TARİF DEFTERİ", out var coverEn) || coverEn != book.Slot("TXT_Cover").SampleEn)
                problems.Add("TARİF DEFTERİ: EN '" + coverEn + "' ≠ manifest sampleEn '" + book.Slot("TXT_Cover").SampleEn + "'");
            var line = coverText.transform.parent.GetComponentsInChildren<TextMeshPro>(true)
                .SingleOrDefault(t => t.TryGetComponent<DeskLocText>(out var l) && l.Source == "KYGA · 07");
            if (line == null) problems.Add("kapakta KYGA · 07 yok");
            else
            {
                if (!en.TryGetValue("KYGA · 07", out var lineEn) || lineEn != "KYGA · 07") problems.Add("KYGA · 07: EN '" + lineEn + "'");
                if (line.text != "KYGA · 07") problems.Add("KYGA · 07: yazı '" + line.text + "'");
                if (line.font != desk.Font(DeskFont.CpBold)) problems.Add("KYGA · 07: font " + (line.font != null ? line.font.name : "yok"));
                if (Mathf.Abs(line.fontSize - 0.062f) > 1e-4f) problems.Add("KYGA · 07: punto " + line.fontSize + " ≠ 0,062 (em 6,2 mm)");
                if (line.enableAutoSizing) problems.Add("KYGA · 07: otomatik punto");
                Near(new Color(233f / 255f, 223f / 255f, 194f / 255f, 0.65f), line.color, "KYGA · 07");
                var mm = line.transform.localPosition * 1000f;
                if (Mathf.Abs(mm.x - 55f) > 0.5f || mm.y < 14f || mm.y > 20f) problems.Add("KYGA · 07: Cover yereli " + mm + " (x 55, alttan ≈ 14 mm)");
            }

            // YENİ levhası
            var tag = Reference<NewItemTag>(Reference<StoreView>(View3D, "store"), "newTag");
            var tagRt = (RectTransform)tag.transform;
            var size = new Vector2(theme.NumberOf("board.newTag.sizeMm[0]", 32f), theme.NumberOf("board.newTag.sizeMm[1]", 13f));
            if (Vector2.Distance(size, tagRt.sizeDelta) > 0.01f) problems.Add("YENİ: boyut " + tagRt.sizeDelta + " ≠ token " + size);
            if (Mathf.Abs(tagRt.localScale.x - 0.001f) > 1e-7f) problems.Add("YENİ: ölçek " + tagRt.localScale.x + " (1 birim = 1 mm)");
            if (tag.gameObject.activeSelf) problems.Add("YENİ: etkin kaydedilmiş (pasif olmalı)");
            var back = Reference<Image>(tag, "background");
            var word = Reference<TMP_Text>(tag, "word");
            Near(theme.Get("board.newTag.color"), back.color, "YENİ zemin");
            Near(theme.Get("board.newTag.ink"), word.color, "YENİ sözcük");
            if (Mathf.Abs(word.fontSize - theme.NumberOf("board.newTag.capMm", 9.4f)) > 0.01f) problems.Add("YENİ: punto " + word.fontSize);
            if (word.font != desk.Font(DeskFont.CpBold)) problems.Add("YENİ: font CP-Bold değil");
            if (word.font != null && word.fontSharedMaterial != word.font.material) problems.Add("YENİ: fontun ışıksız malzemesi değil (" + word.fontSharedMaterial?.name + ")");
            if (word.raycastTarget || back.raycastTarget) problems.Add("YENİ: ışın hedefi");
            if (word.enableAutoSizing) problems.Add("YENİ: otomatik punto");
            if (tag.GetComponentsInChildren<DeskLocText>(true).Length > 0) problems.Add("YENİ: DeskLocText (sözcük koddan)");
            if (tag.GetComponentsInChildren<GraphicRaycaster>(true).Length > 0) problems.Add("YENİ: GraphicRaycaster");

            // DURDU damgası
            var order = Reference<WorkOrderView>(View3D, "order");
            var stamp = Reference<TMP_Text>(order, "stampText");
            if (!(stamp is TextMeshPro)) problems.Add("damga 3B TMP değil");
            if (stamp.font != desk.Font(DeskFont.CpBold)) problems.Add("damga: font CP-Bold değil");
            if (Mathf.Abs(stamp.fontSize - 0.094f) > 1e-4f) problems.Add("damga: punto " + stamp.fontSize + " ≠ 0,094 (em 9,4 mm)");
            Near(theme.Get("stamp.stopped"), stamp.color, "damga");
            if (!string.IsNullOrEmpty(stamp.text)) problems.Add("damga boş kaydedilmeli: '" + stamp.text + "'");
            if (stamp.GetComponent<DeskLocText>() != null) problems.Add("damga: DeskLocText");
            if (stamp.enableAutoSizing || stamp.textWrappingMode != TextWrappingModes.NoWrap) problems.Add("damga: otomatik punto ya da sarma");
            var rel = Quaternion.Inverse(model.transform.rotation) * stamp.transform.rotation;
            if (Quaternion.Angle(Quaternion.Euler(0f, 0f, 6f), rel) > 0.05f) problems.Add("damga: dönüş " + rel.eulerAngles + " (tasarım −6° = Unity z +6°)");
            var mmStamp = BoardLayout.LocalToMm(model.transform.InverseTransformPoint(stamp.transform.position));
            if (!BoardLayout.Sheet.Contains(mmStamp)) problems.Add("damga formun dışında: " + mmStamp);
            var box = ((RectTransform)stamp.transform).sizeDelta * 1000f;
            var stampHalf = Extent(box * 0.5f, 6f);
            for (int i = 0; i < 4; i++)
            {
                var spot = BoardLayout.ChipSpot(i, out float angle, out _);
                var chipHalf = Extent(new Vector2(28f, 20f), angle);
                if (Mathf.Abs(spot.x - mmStamp.x) < chipHalf.x + stampHalf.x && Mathf.Abs(spot.y - mmStamp.y) < chipHalf.y + stampHalf.y)
                    problems.Add("damga fiş yeri " + i + " " + spot + " ile çakışıyor (damga " + mmStamp + ")");
            }
            var frame = Reference<GameObject>(order, "stampFrame");
            if (frame.activeSelf) problems.Add("damga çerçevesi etkin kaydedilmiş");
            if (frame.TryGetComponent<Graphic>(out var g)) Near(theme.Get("stamp.stopped"), g.color, "damga çerçevesi");
            else problems.Add("damga çerçevesinde Graphic yok");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>Dönük dikdörtgenin eksen hizalı yarı boyutu (mm, derece).</summary>
        static Vector2 Extent(Vector2 half, float degrees)
        {
            float r = Mathf.Abs(degrees) * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(half.x * c + half.y * s, half.x * s + half.y * c);
        }

        /// <summary>D5 = c: defter ve üretim sesleri (115–119) bankada yer tutucu klipli (klipsiz cue sessiz kalırdı).</summary>
        [Test]
        public void SoundBank_BookCuesHaveClips()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankPath);
            Assert.IsNotNull(bank, SoundBankPath);
            foreach (var cue in new[] { Cue.BoardBookOpen, Cue.BoardPageTurn, Cue.BoardBookClose, Cue.BoardRecipeLift, Cue.BoardProductSlide })
            {
                Assert.That((int)cue, Is.InRange(115, 119), cue.ToString());
                Assert.IsNotNull(bank.Cue(cue).clip, cue + ": klip yok (D5 = c)");
            }
        }

        // ================================================================== yardımcılar

        static T Reference<T>(Object owner, string field) where T : Object
        {
            var property = new SerializedObject(owner).FindProperty(field);
            Assert.IsNotNull(property, owner.GetType().Name + "." + field);
            var reference = property.objectReferenceValue as T;
            Assert.IsNotNull(reference, owner.GetType().Name + "." + field);
            return reference;
        }
    }
}
