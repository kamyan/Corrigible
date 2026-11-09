using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using Corrigible.Game;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Kaydedilmiş DeskGame: kartlı pano (Revizyon 3 · Aşama 3) PlanningBoard modeline bağlı, eski uGUI pano tuvali kaldırıldı (Aşama 10, T1c), kapıdaki
    /// kabul istasyonu (Aşama 6; eski uGUI kabul penceresi kaldırıldı, D20) ve masadaki vardiya şalteri (fiş çıkışıyla) kendi
    /// denetçileriyle ayrı çalışır.
    /// </summary>
    public sealed class PlanningBoardSceneTests
    {
        const string Path = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const string StringsPath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const string CardDir = "Assets/Corrigible/Desk/Prefabs/Rev3Board/";
        Scene scene, previous;
        GameObject[] roots;
        PlanningBoardPresenter presenter;
        PlanningBoardView board;

        /// <summary>PLAN §3.6 (D3) sabit plakaları: yuva, token anahtarı (board.plates.*), font, em mm, renk.</summary>
        static readonly (string slot, string key, DeskFont font, float em, string colour)[] StaticPlates =
        {
            ("TXT_P_tek", "tek", DeskFont.CpBold, 12f, "#1C150E"),
            ("TXT_P_emp", "emp", DeskFont.CpBold, 10.6f, "#1C150E"),
            ("TXT_P_ana", "ana", DeskFont.CpBold, 12f, "#1C150E"),
            ("TXT_P_uret", "uret", DeskFont.CpBold, 12f, "#1C150E"),
            ("TXT_P_mutf", "mutf", DeskFont.CpBold, 12f, "#1C150E"),
            ("TXT_P_egit", "egit", DeskFont.CpBold, 12f, "#1C150E"),
            ("TXT_P_bosta", "bosta", DeskFont.CpBold, 12f, "#1C150E"),
            ("TXT_P_order", "order", DeskFont.CpBold, 12f, "#1C150E"),
            ("TXT_P_dosya", "dosya", DeskFont.CpBold, 12f, "#1C150E"),
            ("TXT_P_ham", "ham", DeskFont.CpBold, 10.86f, "#1C150E"),
            ("TXT_P_kit", "kit", DeskFont.CpBold, 10.86f, "#1C150E"),
            ("TXT_Back", "back", DeskFont.CpBold, 11.71f, "#1E1B16"),
            ("TXT_FlapA", "flapA", DeskFont.CpBold, 12.29f, "#1E1B16"),
            ("TXT_FlapB", "flapB", DeskFont.CpBold, 12.29f, "#F2E9CC"),
            ("TXT_Title", "title", DeskFont.StencilExtraBold, 20f, "#D9D2BF"),
            ("TXT_FieldBand", "field", DeskFont.CpBold, 6.57f, "#E8D9B0"),
            ("TXT_P_hurda", "hurda", DeskFont.StencilExtraBold, 18.6f, "#2A2B27"),
            ("TXT_LockTag", "lockTag[0]", DeskFont.KalamBold, 7.14f, "#1A2F5A"),
        };

        /// <summary>Dinamik yuvalar (görünüm yazar; DeskLocText yok).</summary>
        static readonly (string slot, DeskFont font, float em, string colour)[] DynamicPlates =
        {
            ("TXT_Header", DeskFont.KalamBold, 15f, "#EDEBE2"),
            ("TXT_Count_0", DeskFont.CpBold, 9.43f, "#1C150E"), ("TXT_Count_1", DeskFont.CpBold, 9.43f, "#1C150E"),
            ("TXT_Count_2", DeskFont.CpBold, 9.43f, "#1C150E"), ("TXT_Count_3", DeskFont.CpBold, 9.43f, "#1C150E"),
            ("TXT_Count_4", DeskFont.CpBold, 9.43f, "#1C150E"), ("TXT_Count_5", DeskFont.CpBold, 9.43f, "#1C150E"),
        };

        static readonly string[] HiddenSlots = { "TXT_S_tek", "TXT_S_emp", "TXT_S_ana", "TXT_S_uret", "TXT_S_mutf", "TXT_S_egit", "TXT_Topics" };

        /// <summary>Her bileşenin dolu olması gereken alanları (tek seferlik bağlayıcı yazar; R3 yeniden içe aktarma bekçisi).</summary>
        static readonly (Type type, string[] fields)[] Required =
        {
            (typeof(BoardView3D), new[] { "model", "backSign", "flap", "hurdaLid", "lockRod", "lamp", "boardLamp", "header", "lockTagLine2",
                "hurdaLine2", "backTarget", "personPrefab", "itemPrefab", "materialPrefab", "chipPrefab", "cardRoot", "portraits", "deckView",
                "store", "order", "topics", "folder", "lostEnvelope", "hurda", "marks", "drag", "hoverTag", "boardEntry", "staticColliders",
                // Revizyon 3 · Aşama 5 (PLAN §2.3 #8)
                "recipePrefab", "book", "boardFace" }),
            (typeof(BoardDragController), new[] { "view", "modelRoot", "rig", "board", "marks", "hurda", "hoverTag" }),
            (typeof(BoardMarks), new[] { "root", "modelRoot", "glowPrefab", "focusPrefab", "dimPrefab", "smudge" }),
            (typeof(HurdaBin), new[] { "lid", "front", "slot", "focusFrame" }),
            (typeof(StoreView), new[] { "counts", "newTag" }),
            (typeof(WorkOrderView), new[] { "numberText", "recipeText", "stampText", "stampFrame", "recipeHome", "dropHintText" }),
            // Aşama 5 (R18a, R18d): defter (dört RecipeBookPage ayrı denetlenir: sahnede dört tane) ve YENİ etiketi
            (typeof(RecipeBookView), new[] { "root", "frame", "cover", "leaf", "pageLeft", "pageRight", "leafFront", "leafBack", "closedHit", "hitLeft", "hitRight" }),
            (typeof(NewItemTag), new[] { "background", "word", "group" }),
            (typeof(TrainingTopics), new[] { "rows", "ring", "rowHits", "frame", "hoverTag" }),
            (typeof(PersonFolderView), new[] { "cover", "frame", "coverTitle", "coverLine", "coverHit", "hoverTag", "pageLeft", "pageRight",
                "rowTemplate", "note", "reviewTab", "accessTab", "dismissTab", "reviewLabel", "accessLabel", "dismissLabel", "stamp", "stampText",
                // Revizyon 3 · Aşama 6 (R17f, PLAN §2.3; W0 adı fileTag)
                "fileTag",
                // Revizyon 3 · Aşama 10 (D-F, R10): sekme alt satırları
                "reviewSub", "accessSub", "dismissSub" }),
            (typeof(FileTagView), new[] { "label" }),
            (typeof(LostEnvelopeView), new[] { "cover", "frame", "body", "coverText", "hit", "hoverTag", "page", "pageTitle", "rowTemplate" }),
            (typeof(PlanningBoardPresenter), new[] { "board3D", "drag", "backSign", "hurdaFront", "admission" }),
        };

        [OneTimeSetUp]
        public void OpenSavedScene()
        {
            var existing = SceneManager.GetSceneByPath(Path);
            if (existing.IsValid() && existing.isLoaded)
                Assert.Ignore("Sahne doğrulaması boş sahne açıkken çalıştırılmalı; kullanıcının açık sahnesine dokunulmadı.");
            previous = SceneManager.GetActiveScene();
            scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
            roots = scene.GetRootGameObjects();
            presenter = One<PlanningBoardPresenter>();
            board = One<PlanningBoardView>();
        }

        [OneTimeTearDown]
        public void CloseSavedSceneWithoutSaving()
        {
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }

        [Test]
        public void SavedScene_BoardEntryPointsAndPhysicalShiftUseTheirOwnControllers()
        {
            var game = One<GameController>();
            var surface = One<LiberationDeskView>();
            var desk = One<SiginakDesk>();
            var shift = One<SiginakShift>();
            var door = One<SiginakDoor>();
            Assert.AreSame(board, Reference<PlanningBoardView>(presenter, "view"));
            Assert.AreSame(game, Reference<GameController>(presenter, "game"));
            Assert.AreSame(surface, Reference<LiberationDeskView>(presenter, "surface"));
            Assert.AreSame(One<SiginakCameraRig>(), Reference<SiginakCameraRig>(presenter, "rig"));
            foreach (var entryPoint in new Component[] { surface, desk, door })
                Assert.AreSame(presenter, Reference<PlanningBoardPresenter>(entryPoint, "planningBoard"), entryPoint.GetType().Name);
            var target = Reference<DeskTarget>(presenter, "target");
            Assert.AreSame(target, Reference<DeskTarget>(desk, "shelfTarget"));
            Assert.IsTrue(target.gameObject.activeInHierarchy);
            Assert.IsNotNull(target.GetComponent<Collider>(), "Odadaki fiziksel panoya basılabilmeli.");
            // Revizyon 3 · Aşama 4: fiş kodu SiginakShift'ten kapanışa taşındı (PLAN §2.2 #7)
            var close = One<SiginakShiftClose>();
            Assert.AreSame(close, Reference<SiginakShiftClose>(shift, "close"));
            Assert.AreSame(close, Reference<SiginakShiftClose>(desk, "shiftClose"));
            Assert.AreSame(One<ShiftReceiptMachine>(), Reference<ShiftReceiptMachine>(close, "machine"));
            Assert.IsNull(new SerializedObject(shift).FindProperty("receiptMachine"), "SiginakShift.receiptMachine kalktı");
            // Revizyon 3 · Aşama 6 (PLAN §2.2 #12–#13): kabul akışı aynı sunucuyu kullanır; dört giriş noktası aynı akışa bağlı
            var admission = One<SiginakAdmission>();
            Assert.AreSame(presenter, Reference<PlanningBoardPresenter>(admission, "planningBoard"), "SiginakAdmission.planningBoard");
            Assert.AreSame(game, Reference<GameController>(admission, "game"));
            Assert.AreSame(surface, Reference<LiberationDeskView>(admission, "surface"));
            Assert.AreSame(desk, Reference<SiginakDesk>(admission, "desk"));
            Assert.AreSame(door, Reference<SiginakDoor>(admission, "doorController"));
            foreach (var owner in new Component[] { surface, desk, door, presenter })
                Assert.AreSame(admission, Reference<SiginakAdmission>(owner, "admission"), owner.GetType().Name + ".admission");
        }

        /// <summary>Pano eski sığınak dolabının yerinde, ayaklı model yerde; K4 kamerası modelin Cam_K4 çapası, FOV 41° (D1).</summary>
        [Test]
        public void SavedScene_SmallerBoardOccupiesOldShelterAreaAndPreservesSettingsCabinet()
        {
            var room = presenter.transform.parent;
            // Aşama 10 (T1a): eski raf sahneden kalktı; kıyas için silinmeden önceki dünya konumu sabitte
            var oldShelfPosition = new Vector3(-1.5f, 0, 1.72f);
            Assert.IsNull(room.Find("Shelf"), "T1a: eski raf silindi");
            Assert.IsNull(room.Find("Shelter controller"), "T1a: maket denetçisi silindi");
            Assert.IsEmpty(roots.SelectMany(r => r.GetComponentsInChildren<SiginakShelter>(true)), "T1a: sahnede SiginakShelter yok");
            Assert.IsTrue(One<SiginakTutorial>().enabled, "Kartlı başlangıç rehberi sahnede etkin");
            Assert.Less(Vector3.Distance(presenter.transform.position, oldShelfPosition), .35f,
                "Pano eski sığınak dolabının alanında olmalı; duvarı aşmamak için küçük poz düzeltmesi yapılabilir.");
            var cabinet = One<SettingsCabinet>();
            Assert.IsTrue(cabinet.gameObject.activeInHierarchy);
            var model = Model();
            var body = model.transform.Find("Body");
            var mesh = body.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.Less(mesh.size.x, 2.4f, "Önceki 2,86 m pano küçültülmeli.");
            Assert.LessOrEqual(BoardLayout.InnerWidthMm / 1000f, mesh.size.x, "iç baskı alanı modelin içinde");
            Assert.Less(BoardLayout.InnerHeightMm / 1000f, 1.7f, "Önceki 1,83 m pano küçültülmeli.");
            Assert.Less(body.GetComponent<MeshRenderer>().bounds.min.y, .1f, "pano ayakları yerde");
            var rig = One<SiginakCameraRig>();
            Assert.IsTrue(rig.TryGetPose(SiginakPose.Shelf, out var k4));
            Assert.AreEqual(model.Anchor("Cam_K4"), k4.anchor);
            // D1: 41° → kart düzleminde 1,50 px/mm; token cameras.K4.fovV 42,5 olarak kalır (SiginakFoundationTests)
            Assert.AreEqual(41f, k4.fieldOfView, 0.01f, "D1: K4 FOV 41°");
        }

        /// <summary>Altı cep, BOŞTA, iş emri, depo ve hurda hedefleri token board.layoutMm'den; model çapaları aynı yerde (±0,5 mm).</summary>
        [Test]
        public void SavedScene_PocketsAndTargetsFollowLayoutMm()
        {
            var model = Model();
            Vector2 Mm(Transform t) => BoardLayout.LocalToMm(model.transform.InverseTransformPoint(t.position));
            void Near(Vector2 want, Vector2 got, float tol, string what)
            {
                Assert.AreEqual(want.x, got.x, tol, what + " x");
                Assert.AreEqual(want.y, got.y, tol, what + " y");
            }
            for (int i = 0; i < BoardLayout.Slots.Length; i++)
            {
                string job = BoardLayout.Jobs[i];
                var anchor = model.Anchor("Slot_" + BoardLayout.Slots[i]);
                Assert.IsNotNull(anchor, "Slot_" + BoardLayout.Slots[i]);
                Assert.IsTrue(BoardLayout.Pocket(job).Contains(Mm(anchor)), job + " çapası cebinde");
                Near(BoardLayout.CardCentre(job), Mm(anchor), 0.5f, job);
            }
            Assert.AreEqual(180f, BoardLayout.Pocket("train").width, 0.01f, "EĞİTİM 180 mm (konu listesi dahil)");
            var deck = model.Anchor("Bosta_Deck");
            Assert.IsTrue(BoardLayout.Bosta.Contains(Mm(deck)));
            Near(BoardLayout.Deck, Mm(deck), 0.5f, "Bosta_Deck");
            Assert.AreEqual(BoardLayout.HandLeftX, Mm(model.Anchor("Bosta_HandLeft")).x, 0.5f, "Bosta_HandLeft");
            Assert.IsTrue(BoardLayout.Order.Contains(Mm(model.Anchor("Order_Recipe"))), "Order_Recipe");
            Assert.IsTrue(BoardLayout.Inputs.Contains(Mm(model.Anchor("Order_Inputs"))), "Order_Inputs");
            Near(BoardLayout.FolderFace.center, Mm(model.Anchor("Folder")), 0.5f, "Folder");
            for (int i = 0; i < BoardLayout.MaterialKinds.Length; i++)
            {
                var a = model.Anchor("Mat_" + BoardLayout.MaterialKinds[i]);
                Near(BoardLayout.Material(i), Mm(a), 0.5f, a.name);
                Assert.IsTrue(BoardLayout.Depot.Contains(Mm(a)), a.name + " DEPO bandında");
            }
            for (int i = 0; i < BoardLayout.KitKinds.Length; i++)
            {
                var a = model.Anchor("Kit_" + i);
                Near(BoardLayout.Kit(i), Mm(a), 0.5f, a.name);
                Assert.IsTrue(BoardLayout.Depot.Contains(Mm(a)), a.name + " DEPO bandında");
            }

            var view3D = One<BoardView3D>();
            var hurda = Reference<HurdaBin>(view3D, "hurda");
            var front = (BoxCollider)hurda.Front;
            Assert.IsNotNull(front, "hurda ön yüzü");
            var centre = model.transform.InverseTransformPoint(front.transform.TransformPoint(front.center));
            Near(BoardLayout.Hurda.center, BoardLayout.LocalToMm(centre), 1f, "HURDA");
            Assert.AreEqual(BoardLayout.Hurda.width, front.size.x * 1000f, 1f, "HURDA genişlik");
            Assert.AreEqual(BoardLayout.Hurda.height, front.size.y * 1000f, 1f, "HURDA yükseklik");
            Assert.AreEqual(-0.089f, centre.z - front.size.z / 2f, 0.001f, "hurda ön yüzü z −0,089 (75 mm öne çıkık)");

            var statics = References<Collider>(view3D, "staticColliders");
            var face = statics.OfType<BoxCollider>().SingleOrDefault(c => c.name == "Pano yüzü");
            Assert.IsNotNull(face, "Pano yüzü sabit hedeflerde");
            var faceCentre = BoardLayout.LocalToMm(model.transform.InverseTransformPoint(face.transform.TransformPoint(face.center)));
            Near(new Vector2(BoardLayout.InnerWidthMm, BoardLayout.InnerHeightMm) * 0.5f, faceCentre, 0.5f, "Pano yüzü merkezi");
            Assert.AreEqual(BoardLayout.InnerWidthMm, face.size.x * 1000f, 0.5f);
            Assert.AreEqual(BoardLayout.InnerHeightMm, face.size.y * 1000f, 0.5f);
            Assert.IsEmpty(face.GetComponents<MonoBehaviour>().OfType<IEventSystemHandler>(), "Pano yüzü işleyicisiz (tıklama denetleyiciye kabarır)");
            var back = Reference<DeskTarget>(view3D, "backTarget").GetComponent<BoxCollider>();
            Near(BoardLayout.Back.center, BoardLayout.LocalToMm(model.transform.InverseTransformPoint(back.transform.TransformPoint(back.center))), 0.5f, "MASAYA DÖN");
            CollectionAssert.Contains(statics, back);
            // Aşama 5 (§1.12 defter kipi): pano yüzü ayrıca boardFace; defterin çarpıştırıcıları sabit hedef değil (defter yönetir)
            Assert.AreSame(face, Reference<Collider>(view3D, "boardFace"), "boardFace = Pano yüzü");
            var bookView = Reference<RecipeBookView>(view3D, "book");
            foreach (var c in bookView.GetComponentsInChildren<Collider>(true))
                CollectionAssert.DoesNotContain(statics, c, "defter çarpıştırıcısı sabit hedeflerde değil: " + c.name);
            foreach (var c in new Collider[] { hurda.Front, Reference<PersonFolderView>(view3D, "folder").Hit, Reference<LostEnvelopeView>(view3D, "lostEnvelope").Hit })
                CollectionAssert.DoesNotContain(statics, c, "kendi görünümü açıp kapar: " + c.name);
        }

        /// <summary>BoardView3D, sürükleme denetleyicisi ve sunucu, PlanningBoard modelinin parçalarına ve yuvalarına bağlı.</summary>
        [Test]
        public void SavedScene_BoardView3DRefsPointIntoThePlanningBoardModel()
        {
            var model = Model();
            var view3D = One<BoardView3D>();
            Assert.AreSame(presenter.gameObject, view3D.gameObject, "BoardView3D Planning board kökünde (model örneğinin içinde değil)");
            Assert.AreSame(model, Reference<Rev3Model>(view3D, "model"));
            foreach (var (field, part) in new[] { ("backSign", "BackSign"), ("flap", "Flap"), ("hurdaLid", "HurdaLid"), ("lockRod", "LockRod"), ("lamp", "Lamp") })
                Assert.AreSame(model.Part(part), Reference<PartDriver>(view3D, field), field);
            Assert.AreSame(model.LightPoint("BoardLamp"), Reference<Light>(view3D, "boardLamp"));
            Assert.AreSame(model.Slot("TXT_Header").Text, Reference<TMP_Text>(view3D, "header"));
            var rod = model.Part("LockRod");
            Assert.IsFalse(rod.gameObject.activeSelf, "kilit çubuğu dinlenmede gizli (BoardView3D kilitte gösterir)");
            Assert.IsTrue(Reference<TMP_Text>(view3D, "lockTagLine2").transform.IsChildOf(rod.transform), "kilit 2. satırı çubukla kayar");
            var hurda = Reference<HurdaBin>(view3D, "hurda");
            Assert.IsNull(hurda.GetComponent<BoardView3D>(), "HurdaBin kendi nesnesinde (punto taraması istisnası)");
            Assert.IsTrue(Reference<TMP_Text>(view3D, "hurdaLine2").transform.IsChildOf(hurda.transform));
            Assert.AreSame(model.Part("HurdaLid"), Reference<PartDriver>(hurda, "lid"));
            Assert.AreSame(model.Anchor("Hurda_Slot"), Reference<Transform>(hurda, "slot"));
            var back = Reference<DeskTarget>(view3D, "backTarget");
            Assert.IsNotNull(back.GetComponent<Collider>());
            Assert.IsNull(new SerializedObject(back).FindProperty("lift").objectReferenceValue, "tabelayı BoardView3D kaydırır");
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(back), "MASAYA DÖN hedefi sahneye ait");

            var cardRoot = Reference<Transform>(view3D, "cardRoot");
            Assert.AreEqual("Kartlar", cardRoot.name);
            Assert.AreSame(presenter.transform, cardRoot.parent);
            Assert.Less(Vector3.Distance(model.transform.position, cardRoot.position), 1e-5f, "kart kökü = model çerçevesi");
            Assert.Less(Quaternion.Angle(model.transform.rotation, cardRoot.rotation), 0.01f);
            Assert.AreEqual(Vector3.one, cardRoot.localScale);
            Assert.AreEqual(0, cardRoot.childCount, "havuz kartları çalışma anında kurulur, sahneye kaydedilmez");

            var entry = Reference<DeskTarget>(presenter, "target");
            Assert.AreSame(entry.GetComponent<Collider>(), Reference<Collider>(view3D, "boardEntry"));
            Assert.AreSame(entry.Tag, Reference<HoverTag>(view3D, "hoverTag"));
            Assert.IsEmpty(new SerializedObject(entry).FindProperty("line").stringValue, "eski yönerge satırı yok (sunucu LineOverride yazar)");

            var drag = One<BoardDragController>();
            Assert.AreSame(presenter.gameObject, drag.gameObject);
            Assert.AreSame(drag, Reference<BoardDragController>(view3D, "drag"));
            var rig = One<SiginakCameraRig>();
            Assert.AreSame(rig.View, Reference<Camera>(drag, "view"));
            Assert.AreSame(model.transform, Reference<Transform>(drag, "modelRoot"));
            Assert.AreSame(rig, Reference<SiginakCameraRig>(drag, "rig"));
            Assert.AreSame(view3D, Reference<BoardView3D>(drag, "board"));
            Assert.AreSame(Reference<BoardMarks>(view3D, "marks"), Reference<BoardMarks>(drag, "marks"));
            Assert.AreSame(hurda, Reference<HurdaBin>(drag, "hurda"));
            Assert.AreSame(entry.Tag, Reference<HoverTag>(drag, "hoverTag"));
            Assert.AreSame(view3D, Reference<BoardView3D>(presenter, "board3D"));
            Assert.AreSame(drag, Reference<BoardDragController>(presenter, "drag"));
            Assert.AreSame(back, Reference<DeskTarget>(presenter, "backSign"), "MASAYA DÖN: sunucu ve görünüm aynı hedefi dinler");
            var hurdaTarget = Reference<DeskTarget>(presenter, "hurdaFront");
            Assert.AreSame(hurda.Front.gameObject, hurdaTarget.gameObject, "hurda DeskTarget'ı ön yüz çarpıştırıcısında");
            Assert.AreEqual("HURDA", hurdaTarget.Title);
            Assert.IsNull(new SerializedObject(hurdaTarget).FindProperty("lift").objectReferenceValue, "kapağı HurdaBin sürer");
            Assert.AreSame(presenter.gameObject, Reference<BostaDeckView>(view3D, "deckView").gameObject);

            var counts = References<TMP_Text>(Reference<StoreView>(view3D, "store"), "counts");
            Assert.AreEqual(6, counts.Length);
            for (int i = 0; i < 6; i++) Assert.AreSame(model.Slot("TXT_Count_" + i).Text, counts[i], "TXT_Count_" + i);

            // Eleştiri #7: üzerine gelme etiketinin ≈ 2 px çerçevesi kâğıdın çocuğu, kapalı başlar
            var frame = Reference<Image>(entry.Tag, "frame");
            Assert.IsTrue(frame.transform.IsChildOf(Reference<RectTransform>(entry.Tag, "paper")));
            Assert.IsFalse(frame.enabled);
            Assert.IsFalse(frame.raycastTarget);
        }

        /// <summary>Beş kart prefab'ı (CONTRACTS › Shared assets; Aşama 5 tarif kartı) türleriyle bağlı.</summary>
        [Test]
        public void SavedScene_CardPrefabsAreReferenced()
        {
            var view3D = One<BoardView3D>();
            foreach (var (field, file, kind) in new[]
                     {
                         ("personPrefab", "PersonCard", BoardCardKind.Person), ("itemPrefab", "ItemCard", BoardCardKind.Item),
                         ("materialPrefab", "MaterialCard", BoardCardKind.Material), ("chipPrefab", "ChipCard", BoardCardKind.Chip),
                         ("recipePrefab", "RecipeCard", BoardCardKind.Recipe),
                     })
            {
                var card = Reference<BoardCardView>(view3D, field);
                Assert.IsTrue(EditorUtility.IsPersistent(card), field + " prefab varlığı olmalı");
                Assert.AreEqual(CardDir + file + ".prefab", AssetDatabase.GetAssetPath(card), field);
                Assert.AreEqual(kind, card.Kind, field);
                Assert.IsNotNull(card.GetComponent<BoxCollider>(), field);
            }
            Assert.IsTrue(EditorUtility.IsPersistent(Reference<PortraitLibrary>(view3D, "portraits")));
        }

        /// <summary>Alt görünümler kendi nesnelerinde ve PLAN §1.2 / NOTES_W4 yerlerinde (işaret tuvali, konular, dosya, KAYIPLAR, iş emri).</summary>
        [Test]
        public void SavedScene_BoardSubViewsAreWired()
        {
            var model = Model();
            var view3D = One<BoardView3D>();
            Vector3 Local(Transform t) => model.transform.InverseTransformPoint(t.position);

            var marks = Reference<BoardMarks>(view3D, "marks");
            var canvas = marks.GetComponent<Canvas>();
            Assert.IsNotNull(canvas, "Pano işaretleri tuval");
            Assert.AreEqual("Pano işaretleri", marks.name);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.IsEmpty(marks.GetComponentsInChildren<GraphicRaycaster>(true), "işaretler ışın yakalamaz");
            Assert.AreEqual(0.001f, marks.transform.lossyScale.x, 1e-6f, "1 birim = 1 mm");
            Assert.AreEqual(BoardLayout.FaceZ - 0.0026f, Local(marks.transform).z, 1e-5f, "yüzün 2,6 mm önünde");
            Assert.AreSame(model.transform, Reference<Transform>(marks, "modelRoot"));

            var topics = Reference<TrainingTopics>(view3D, "topics");
            var rows = References<TMP_Text>(topics, "rows");
            var hits = References<BoxCollider>(topics, "rowHits");
            Assert.AreEqual(7, rows.Length);
            Assert.AreEqual(7, hits.Length);
            Assert.IsTrue(hits.All(h => h != null && h.transform.IsChildOf(topics.transform)), "satır hedefleri konuların çocuğu (olaylar kabarır)");
            Assert.AreSame(model.transform, Reference<Transform>(topics, "frame"));
            var texts = presenter.transform.Find("Pano yazıları");
            Assert.IsNotNull(texts, "Planning board/Pano yazıları");
            Assert.IsTrue(topics.transform.IsChildOf(texts), "eleştiri #12: sahneye ait kardeş");

            var order = Reference<WorkOrderView>(view3D, "order");
            Assert.IsNull(order.GetComponent<BoardView3D>(), "WorkOrderView kendi nesnesinde (punto taraması istisnası)");
            Assert.IsTrue(Reference<TMP_Text>(order, "numberText").transform.IsChildOf(order.transform));
            Assert.IsTrue(Reference<TMP_Text>(order, "recipeText").transform.IsChildOf(order.transform));
            var form = order.GetComponentsInChildren<DeskLocText>(true).Select(l => l.Source).ToList();
            CollectionAssert.IsSupersetOf(form, new[] { "İŞ EMRİ · ÜRETİM", "TARİF", "KAPANIŞTA: 1 ürün → DEPO" });
            Assert.IsFalse(form.Any(s => s.Contains("bırak")), "yönerge yok (B C17)");
            // Aşama 5 (R18c; PLAN §2.3 #7): DURDU damgası formun çocuğu, boş, koddan yazılır; TARİF kutusunun çapası Order_Recipe
            var stamp = Reference<TMP_Text>(order, "stampText");
            Assert.IsTrue(stamp.transform.IsChildOf(order.transform), "damga formun çocuğu");
            Assert.IsEmpty(stamp.text, "damga boş kaydedilir");
            Assert.IsNull(stamp.GetComponent<DeskLocText>(), "damga dinamik (DeskLocText yok)");
            Assert.IsFalse(Reference<GameObject>(order, "stampFrame").activeSelf, "damga çerçevesi pasif");
            Assert.AreSame(model.Anchor("Order_Recipe"), Reference<Transform>(order, "recipeHome"), "recipeHome = Order_Recipe");

            var folder = Reference<PersonFolderView>(view3D, "folder");
            var fm = folder.GetComponent<Rev3Model>();
            Assert.IsNotNull(fm, "PersonFolderView PersonFolder örneğinin kökünde");
            Assert.AreEqual("PersonFolder", fm.ModelName);
            Assert.AreSame(presenter.transform, folder.transform.parent);
            var want = model.transform.InverseTransformPoint(model.Anchor("Folder").position) - new Vector3(0.065f, 0.09f, 0f);
            Assert.Less(Vector3.Distance(want, Local(folder.transform)), 1e-4f, "dosya kökü = Folder − (0,065, 0,09, 0)");
            Assert.AreSame(fm.Part("Cover"), Reference<PartDriver>(folder, "cover"));
            Assert.AreSame(fm.Slot("TXT_Face").Text, Reference<TMP_Text>(folder, "coverTitle"));
            Assert.AreEqual("KİŞİ DOSYASI", fm.Slot("TXT_Face").Text.GetComponent<DeskLocText>().Source);
            Assert.IsTrue(Reference<TMP_Text>(folder, "coverLine").transform.IsChildOf(fm.Part("Cover").transform), "kapak 2. satırı kapakla döner");
            foreach (var (field, slot) in new[] { ("pageLeft", "TXT_PageL"), ("pageRight", "TXT_PageR") })
            {
                var page = Reference<RectTransform>(folder, field);
                Assert.AreSame(fm.Slot(slot).transform, page.parent, field);
                Assert.AreEqual(Vector3.one * 0.001f, page.localScale, field);
                Assert.AreEqual(new Vector2(120f, 168f), page.sizeDelta, field);
                Assert.IsNotNull(page.GetComponent<Canvas>(), field);
            }
            Assert.IsFalse(Reference<TMP_Text>(folder, "rowTemplate").gameObject.activeSelf, "satır şablonu pasif");
            foreach (var tab in new[] { "reviewTab", "accessTab", "dismissTab" })
                Assert.IsTrue(Reference<Collider>(folder, tab).transform.IsChildOf(folder.transform), tab);
            Assert.AreEqual(0f, Reference<CanvasGroup>(folder, "stamp").alpha);
            Assert.IsEmpty(folder.GetComponentsInChildren<GraphicRaycaster>(true));
            Assert.IsEmpty(folder.GetComponentsInChildren<DeskTarget>(true), "dosya tıklamayı kendisi alır");

            var lost = Reference<LostEnvelopeView>(view3D, "lostEnvelope");
            var body = Reference<GameObject>(lost, "body");
            Assert.AreNotSame(lost.gameObject, body);
            Assert.AreSame(lost.transform, body.transform.parent);
            var lm = body.GetComponent<Rev3Model>();
            Assert.AreEqual("PersonFolder", lm.ModelName);
            var offset = Local(lost.transform) - Local(folder.transform);
            Assert.AreEqual(0.050f, offset.x, 1e-4f, "D5: kişi dosyasının 50 mm sağında");
            Assert.Greater(offset.z, 0f, "D5: kişi dosyasının arkasında");
            Assert.IsFalse(lm.Slot("TXT_Face").gameObject.activeSelf, "KAYIPLAR örneğinin kapak yazısı gizli");
            Assert.AreSame(lm.Part("Cover"), Reference<PartDriver>(lost, "cover"));
            Assert.IsTrue(Reference<TMP_Text>(lost, "coverText").transform.IsChildOf(lm.Part("Cover").transform));
            Assert.IsTrue(Reference<Collider>(lost, "hit").transform.IsChildOf(lost.transform));
            Assert.AreSame(lm.Slot("TXT_PageR").transform, Reference<RectTransform>(lost, "page").parent);
            var strip = BoardLayout.LocalToMm(model.transform.InverseTransformPoint(lost.transform.TransformPoint(new Vector3(0.105f, 0.09f, 0f))));
            Assert.IsTrue(BoardLayout.Folder.Contains(strip), "görünen şerit KİŞİ DOSYASI bölgesinde (K4 kadrajında)");
        }

        /// <summary>
        /// Eleştiri #11: her plaka yuvasının DeskLocText kaynağı token board.plates.tr.&lt;k&gt; ile, strings.txt İngilizcesi
        /// board.plates.en.&lt;k&gt; ile aynı; font, punto (3B TMP: em mm / 100), renk PLAN §3.6 (D3). Belgeli sapmalar: hurda plakası
        /// yalnız 1. satır (2. satır denge değerinden, eleştiri #9), kilit etiketi yalnız 1. satır (2. satır neden, B C22).
        /// </summary>
        [Test]
        public void SavedScene_BoardPlates_TokenStringsAndSlotsAgree()
        {
            var model = Model();
            var theme = SiginakTheme.Active;
            var desk = DeskTheme.Active;
            Assert.IsNotNull(theme);
            Assert.IsNotNull(desk);
            var tables = Loc.ParseTable(File.ReadAllText(StringsPath));
            tables.TryGetValue("en", out var en);
            en ??= new Dictionary<string, string>();
            var problems = new List<string>();
            string First(string s) { int cut = s?.IndexOf(" · ", StringComparison.Ordinal) ?? -1; return cut > 0 ? s.Substring(0, cut) : s; }

            foreach (var (slot, key, font, em, colour) in StaticPlates)
            {
                var t = model.Slot(slot)?.Text;
                if (t == null) { problems.Add(slot + ": yuva yok"); continue; }
                string tr = theme.Text("board.plates.tr." + key), enToken = theme.Text("board.plates.en." + key);
                if (key == "hurda") { tr = First(tr); enToken = First(enToken); }
                if (!t.gameObject.activeSelf) problems.Add(slot + ": etkin değil");
                var loc = t.GetComponent<DeskLocText>();
                if (loc == null) problems.Add(slot + ": DeskLocText yok");
                else if (loc.Source != tr) problems.Add($"{slot}: kaynak '{loc.Source}' ≠ token '{tr}'");
                if (tr != null && (!en.TryGetValue(tr, out var e) || e != enToken))
                    problems.Add($"{slot}: strings.txt EN '{(en.TryGetValue(tr, out var x) ? x : "(yok)")}' ≠ token '{enToken}'");
                CheckStyle(t, slot, desk.Font(font), em, colour, problems);
            }
            foreach (var (slot, font, em, colour) in DynamicPlates)
            {
                var t = model.Slot(slot)?.Text;
                if (t == null) { problems.Add(slot + ": yuva yok"); continue; }
                if (!t.gameObject.activeSelf) problems.Add(slot + ": etkin değil");
                if (t.GetComponent<DeskLocText>() != null) problems.Add(slot + ": dinamik yuvada DeskLocText var");
                CheckStyle(t, slot, desk.Font(font), em, colour, problems);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>TXT_S_* ve TXT_Topics gizli; yerlerinde Pano yazıları altında 18 şerit satırı (14 mm aralık) ve 7 konu satırı (16 mm).</summary>
        [Test]
        public void SavedScene_StripAndTopicRowsReplaceImportedSlots()
        {
            var model = Model();
            foreach (var name in HiddenSlots) Assert.IsFalse(model.Slot(name).gameObject.activeSelf, name + " gizli");
            var texts = presenter.transform.Find("Pano yazıları");
            Assert.IsNotNull(texts, "eleştiri #12: Planning board/Pano yazıları");
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(texts), "sahneye ait (model örneğinin içinde değil)");
            var cp = DeskTheme.Active.Font(DeskFont.CpBold);
            var problems = new List<string>();
            Vector2 Mm(Transform t) => BoardLayout.LocalToMm(model.transform.InverseTransformPoint(t.position));
            for (int s = 0; s < BoardLayout.Slots.Length; s++)
            {
                bool field = s < 3;
                var centre = BoardLayout.CardCentre(BoardLayout.Jobs[s]);
                for (int i = 0; i < 3; i++)
                {
                    string name = "Şerit · " + BoardLayout.Slots[s] + " · " + (i + 1);
                    var t = texts.GetComponentsInChildren<TMP_Text>(true).SingleOrDefault(x => x.name == name);
                    if (t == null) { problems.Add(name + " yok"); continue; }
                    string source = i == 0 ? (field ? "KİT · GEREKLİ" : "KİT") : i == 1 ? "ZIRH" : "YARDIMCI";
                    var loc = t.GetComponent<DeskLocText>();
                    if (loc == null || loc.Source != source) problems.Add(name + ": kaynak " + (loc != null ? loc.Source : "(yok)") + " ≠ " + source);
                    var mm = Mm(t.transform);
                    if (Mathf.Abs(mm.x - centre.x) > 0.5f || Mathf.Abs(mm.y - (centre.y + 67.5f + 14f * i)) > 0.5f) problems.Add(name + ": yer " + mm);
                    CheckStyle(t, name, cp, 9.4f, i == 0 && field ? "#F0A08A" : "#E8E0C89E", problems);
                }
            }
            var topics = texts.GetComponentInChildren<TrainingTopics>(true);
            Assert.IsNotNull(topics);
            var rows = References<TMP_Text>(topics, "rows");
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].GetComponent<DeskLocText>() != null) problems.Add("konu " + i + ": DeskLocText (anlık görüntüden yazılır)");
                var mm = Mm(rows[i].transform);
                var b = BoardLayout.TopicBaseline(i);
                if (Vector2.Distance(mm, b) > 0.5f) problems.Add("konu " + i + ": yer " + mm + " ≠ " + b);
                if (i > 0 && Mathf.Abs(mm.y - Mm(rows[i - 1].transform).y - 16f) > 0.1f) problems.Add("konu " + i + ": aralık 16 mm değil");
                CheckStyle(rows[i], "konu " + i, cp, 9.43f, "#E8E0C8", problems);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>
        /// Aşama 5 (R18a; PLAN §2.3, E25): kapalı 3B tarif defteri Aşama 3'ün referans pozunda kalır (D11), kökünde RecipeBookView ve
        /// kapalı gövde çarpıştırıcısı; dört sayfa yüzü RecipeBookPage.prefab örneği (sol Cover'da, sağ Body'de, yaprağın iki yüzü Leaf'te),
        /// kapak ve yaprak PartDriver'ları manifestle aynı; defter malzemeleri sahne örneğinde. D6'nın geçici uGUI defter tuvali,
        /// PlanningBoardMotion'ın defter alanları ve eski tarif kartları yok; defter altında düğme, ışın yakalayıcı ya da ekran tuvali yok.
        /// </summary>
        [Test]
        public void SavedScene_RecipeBook_3D_NoTemporaryCanvas()
        {
            var model = Model();
            var book = presenter.GetComponentsInChildren<Rev3Model>(true).SingleOrDefault(m => m.ModelName == "RecipeBook");
            Assert.IsNotNull(book, "kapalı 3B RecipeBook");
            Assert.AreSame(presenter.transform, book.transform.parent);
            var local = model.transform.InverseTransformPoint(book.transform.position);
            Assert.Less(Vector3.Distance(new Vector3(-0.562f, 0.993f, -0.033f), local), 1e-4f, "D11: Aşama 3 pozu (−0,562, 0,993, −0,033)");
            var tilt = Quaternion.Inverse(model.transform.rotation) * book.transform.rotation;
            Assert.Less(Quaternion.Angle(Quaternion.Euler(-4f, 0f, 0f), tilt), 0.05f, "x −4° eğik");
            var coverText = book.Slot("TXT_Cover").Text;
            Assert.AreEqual("TARİF DEFTERİ", coverText.GetComponent<DeskLocText>().Source);
            Assert.AreEqual(TextWrappingModes.Normal, coverText.textWrappingMode, "TARİF / DEFTERİ iki satır");
            Assert.IsFalse(coverText.enableAutoSizing);
            var coverLine = coverText.transform.parent.GetComponentsInChildren<TextMeshPro>(true)
                .SingleOrDefault(t => t.TryGetComponent<DeskLocText>(out var l) && l.Source == "KYGA · 07");
            Assert.IsNotNull(coverLine, "kapakta KYGA · 07 (DeskLocText)");

            // Kapak ve yaprak parçaları manifestle aynı (D §1.1)
            var cover = book.Part("Cover");
            var leaf = book.Part("Leaf");
            foreach (var (part, to) in new[] { (cover, -178f), (leaf, -176f) })
            {
                Assert.IsNotNull(part);
                Assert.AreEqual(PartKind.Rotate, part.Kind, part.PartName);
                Assert.AreEqual(PartAxis.Y, part.Axis, part.PartName);
                Assert.AreEqual(0f, part.RangeFrom, 1e-4f, part.PartName);
                Assert.AreEqual(to, part.RangeTo, 1e-4f, part.PartName);
                Assert.AreEqual(0f, part.Rest, 1e-4f, part.PartName);
            }
            Assert.IsTrue(leaf.gameObject.activeSelf, "kapalı defterde yaprak görünür");
            var body = cover.transform.parent;
            Assert.AreEqual("Body", body.name);

            // RecipeBookView ve bağları (BoardView3D.book)
            var view = book.GetComponent<RecipeBookView>();
            Assert.IsNotNull(view, "RecipeBookView defter kökünde");
            Assert.AreSame(view, Reference<RecipeBookView>(One<BoardView3D>(), "book"));
            Assert.AreSame(book.transform, Reference<Transform>(view, "root"));
            Assert.AreSame(model.transform, Reference<Transform>(view, "frame"));
            Assert.AreSame(cover, Reference<PartDriver>(view, "cover"));
            Assert.AreSame(leaf, Reference<PartDriver>(view, "leaf"));
            var pageLeft = Reference<RecipeBookPage>(view, "pageLeft");
            var pageRight = Reference<RecipeBookPage>(view, "pageRight");
            var leafFront = Reference<RecipeBookPage>(view, "leafFront");
            var leafBack = Reference<RecipeBookPage>(view, "leafBack");
            Assert.AreSame(cover.transform, pageLeft.transform.parent, "sol sayfa kapakla döner");
            Assert.AreSame(body, pageRight.transform.parent, "sağ sayfa gövdede");
            Assert.AreSame(leaf.transform, leafFront.transform.parent.parent, "yaprak ön yüzü Leaf/Yaprak ön altında");
            Assert.AreSame(leaf.transform, leafBack.transform.parent.parent, "yaprak arka yüzü Leaf/Yaprak arka altında");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 180f, 0f), pageLeft.transform.localRotation), 0.01f, "sol sayfa kapağın +z'sine bakar");
            Assert.Less(Quaternion.Angle(Quaternion.identity, pageRight.transform.localRotation), 0.01f, "sağ sayfa −z'ye bakar");
            Assert.Less(Vector3.Distance(book.Slot("TXT_PageR").transform.position, pageRight.transform.position), 1e-5f, "sağ sayfa TXT_PageR yuvasında");
            foreach (var page in new[] { pageLeft, pageRight, leafFront, leafBack })
            {
                Assert.AreEqual(CardDir + "RecipeBookPage.prefab", PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(page.gameObject), page.name + ": sayfa prefab'ı örneği");
                Assert.IsFalse(page.gameObject.activeSelf, page.name + ": kapalı defterde pasif (görünüm açınca etkinleştirir)");
                var canvas = page.GetComponent<Canvas>();
                Assert.IsNotNull(canvas, page.name);
                Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, page.name);
                Assert.AreEqual(Vector3.one * 0.001f, page.transform.localScale, page.name + ": 1 birim = 1 mm");
                Assert.IsNotNull(page.Hit, page.name + ": çarpıştırıcı");
                Assert.IsFalse(page.Hit.enabled, page.name + ": çarpıştırıcı kapalı");
            }
            var closedHit = Reference<BoxCollider>(view, "closedHit");
            Assert.AreSame(book.gameObject, closedHit.gameObject, "kapalı gövde çarpıştırıcısı kökte");
            Assert.Less(Vector3.Distance(new Vector3(0.0525f, 0.074f, 0f), closedHit.center), 1e-5f);
            Assert.Less(Vector3.Distance(new Vector3(0.105f, 0.148f, 0.018f), closedHit.size), 1e-5f);
            Assert.AreSame(pageLeft.Hit, Reference<BoxCollider>(view, "hitLeft"));
            Assert.AreSame(pageRight.Hit, Reference<BoxCollider>(view, "hitRight"));
            Assert.IsTrue(book.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), "defter çarpıştırıcıları kapalı kaydedilir (yalnız K4 etkileşimliyken açılır)");

            // Malzemeler (sahne örneğinde; prefab ve paylaşılan malzemeler değişmez)
            var coverMats = cover.GetComponent<MeshRenderer>().sharedMaterials;
            var bodyMats = body.GetComponent<MeshRenderer>().sharedMaterials;
            Assert.AreEqual(Rev3BoardTextureBaker.BookCoverMaterialPath, AssetDatabase.GetAssetPath(coverMats[0]), "Cover yuva 0");
            Assert.AreEqual(Rev3BoardTextureBaker.RecipePageLeftMaterialPath, AssetDatabase.GetAssetPath(coverMats[2]), "Cover yuva 2 (sol sayfa)");
            Assert.AreEqual(Rev3BoardTextureBaker.RecipePageRightMaterialPath, AssetDatabase.GetAssetPath(bodyMats[2]), "Body yuva 2 (sağ sayfa)");
            foreach (var (page, path) in new[] { (leafFront, Rev3BoardTextureBaker.RecipePageRightMaterialPath), (leafBack, Rev3BoardTextureBaker.RecipePageLeftMaterialPath) })
            {
                var paper = page.transform.parent.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.GetComponent<MeshFilter>() != null && !r.transform.IsChildOf(page.transform));
                Assert.AreEqual(path, AssetDatabase.GetAssetPath(paper.sharedMaterial), page.transform.parent.name + ": kâğıt malzemesi");
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, paper.shadowCastingMode, page.transform.parent.name + ": gölgesiz");
                Assert.IsNull(paper.GetComponent<Collider>(), page.transform.parent.name + ": kâğıt çarpıştırıcısız");
            }

            // D6'nın geçici defteri ve eski uGUI tarif kartları yok
            var room = presenter.transform.parent;
            Assert.IsFalse(room.Cast<Transform>().Any(t => t.name == "Geçici tarif defteri"), "geçici tarif defteri tuvali silindi");
            var motion = board.Motion;
            Assert.IsNotNull(motion, "PlanningBoardMotion kalır (deste kodu; Aşama 10)");
            var mso = new SerializedObject(motion);
            foreach (var field in new[] { "bookButton", "bookClose", "previous", "next", "bookRoot", "cover", "leftPage", "rightPage", "bookGroup", "pageNumber", "detailsRoot" })
                Assert.IsNull(mso.FindProperty(field).objectReferenceValue, "PlanningBoardMotion." + field + " boş");
            Assert.IsEmpty(board.Recipes, "PlanningBoardView.recipes boş (API kalır)");
            Assert.IsEmpty(book.GetComponentsInChildren<Button>(true), "ekranda düğme yok");
            Assert.IsEmpty(book.GetComponentsInChildren<GraphicRaycaster>(true), "defter altında uGUI ışın yakalayıcısı yok");
            Assert.IsTrue(book.GetComponentsInChildren<Canvas>(true).All(c => c.renderMode == RenderMode.WorldSpace), "defter altında ekran tuvali yok");
            foreach (var t in room.GetComponentsInChildren<TMP_Text>(true))
                Assert.AreNotEqual("AÇMAK İÇİN TIKLA", t.text, "D6: tıkla yazısı yok");
        }

        [Test]
        public void SavedScene_NoCommunityRegisterOrShiftControlRemainsOnBoard()
        {
            Assert.IsEmpty(board.Traces, "İkinci bir topluluk sicil listesi kaldırılmalı.");
            foreach (string field in new[] { "machine", "machineLabel", "receipt", "receiptRoot", "coverOpen", "coverClosed" })
                Assert.IsNull(new SerializedObject(board).FindProperty(field).objectReferenceValue, field);
            Assert.IsNull(board.MachineButton);
            Assert.IsNull(board.Receipt);
            Assert.IsEmpty(board.GetComponentsInChildren<ShiftReceiptMachine>(true));
            Assert.IsEmpty(board.GetComponentsInChildren<ShiftReceiptPaper>(true));
            Assert.IsEmpty(board.GetComponentsInChildren<EvrakTray>(true));
        }

        [Test]
        public void SavedScene_PhysicalPrinterPaperAndExistingEvrakTrayAreWired()
        {
            var shift = One<SiginakShift>();
            var machine = One<ShiftReceiptMachine>();
            var paper = One<ShiftReceiptPaper>();
            var tray = One<EvrakTray>();
            Assert.IsFalse(machine.transform.IsChildOf(board.transform), "Fiş çıkışı panonun dışında, vardiya şalterinde durmalı.");
            Assert.IsFalse(paper.transform.IsChildOf(board.transform));
            Assert.AreSame(paper, machine.Paper);
            Assert.AreSame(tray, Reference<EvrakTray>(machine, "tray"), "Mevcut EVRAK kutusu kullanılmalı.");
            var stationSwitch = Reference<ShiftSwitch>(shift, "shiftSwitch");
            Assert.AreSame(stationSwitch, Reference<ShiftSwitch>(machine, "shiftSwitch"));
            Assert.IsNotNull(Reference<Transform>(stationSwitch, "coverHinge"), "Kapak gerçek dönüş parçası olmalı.");
            Assert.IsNotNull(Reference<Transform>(stationSwitch, "lever"), "Kol gerçek dönüş parçası olmalı (vardiya şalteri).");
            Assert.IsNotNull(Reference<Transform>(machine, "output"));
            // R19: plakanın 2. satırını ShiftPlate yazar; makinenin eski satır alanı boş (PLAN §2.2 #5)
            Assert.IsNull(new SerializedObject(machine).FindProperty("status").objectReferenceValue, "ShiftReceiptMachine.status boş");
            var plate = Reference<ShiftPlate>(stationSwitch, "plate");
            Assert.AreSame(stationSwitch.gameObject, plate.gameObject);
            Assert.IsNotNull(Reference<TMP_Text>(plate, "line2"));
            var camera = One<SiginakCameraRig>().View;
            Assert.AreSame(camera, Reference<Camera>(machine, "view"));
            Assert.AreSame(camera, Reference<Camera>(paper, "view"));
            Assert.AreSame(Reference<Transform>(machine, "output"), Reference<Transform>(paper, "output"), "fiş ve makine aynı ağız ucunu kullanır");
            Assert.IsNotNull(Reference<Transform>(paper, "start"), "çıkışın başı SlipStart çapası");
            Assert.AreSame(tray, Reference<EvrakTray>(paper, "tray"));
            var face = Reference<HandoverSlipFace>(paper, "face");
            Assert.AreSame(paper.transform, face.transform.parent, "fiş yüzü kâğıdın çocuğu (kimlik yereli)");
            var dragSurface = Reference<Transform>(paper, "dragSurface");
            Assert.AreEqual("Slip over", dragSurface.name, "sürükleme düzlemi EVRAK üstü çapası");
            Assert.AreSame(tray.transform, dragSurface.parent);
            Assert.AreSame(dragSurface, Reference<Transform>(tray, "slipOver"));
            Assert.IsNotNull(Reference<Collider>(paper, "hitArea"));
            Assert.IsNull(new SerializedObject(paper).FindProperty("heading").objectReferenceValue, "Revizyon 2 başlık yazısı kalktı");
            Assert.IsNull(new SerializedObject(paper).FindProperty("body").objectReferenceValue, "Revizyon 2 gövde yazısı kalktı");
            var dropArea = Reference<Collider>(tray, "receiptDropArea");
            Assert.IsTrue(dropArea.transform == tray.transform || dropArea.transform.IsChildOf(tray.transform));
            Assert.IsFalse(paper.gameObject.activeSelf, "Yeni sahnede henüz basılmamış kâğıt görünmemeli.");
        }

        /// <summary>
        /// Revizyon 3 · Aşama 6 (D20, PLAN §2.2 #14–#15): eski uGUI kabul penceresi (<c>SiginakRoom/Admission papers</c>) alt ağacıyla
        /// kalktı; <c>PlanningBoardView</c>'ın kabul alanları boş (alanlar ve erişimciler API olarak kalır); kabul kapıdaki istasyonda
        /// (<see cref="AdmissionStation"/>, <see cref="SiginakAdmission"/>). Eski uGUI pano tuvali yine gizli; pano altında etkin bölge,
        /// kart, yüzey ya da GraphicRaycaster yok.
        /// </summary>
        [Test]
        public void SavedScene_OldAdmissionWindowRemoved_StationWired()
        {
            var room = presenter.transform.parent;
            Assert.IsFalse(room.Cast<Transform>().Any(t => t.name == "Admission papers"), "eski kabul penceresi silindi");
            Assert.IsFalse(roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)).Any(t => t.text == "KAPI · BAŞVURU VE GÜVENLİK DOSYASI"),
                "eski pencerenin başlığı sahnede yok");
            var so = new SerializedObject(board);
            foreach (var field in new[] { "admissionCanvas", "admissionControls", "admissionsRoot", "admissionProtocol", "admissionPapers", "admissionFindings", "admissionClose" })
            {
                var p = so.FindProperty(field);
                Assert.IsNotNull(p, "PlanningBoardView." + field + " alanı kalır (API)");
                Assert.IsNull(p.objectReferenceValue, "PlanningBoardView." + field + " boş");
            }
            Assert.AreEqual(0, board.Inspections.Length);
            Assert.AreEqual(0, board.Decisions.Length);
            Assert.IsNull(board.AdmissionClose);

            var station = One<AdmissionStation>();
            var admission = One<SiginakAdmission>();
            Assert.AreSame(room, station.transform.parent, "istasyon SiginakRoom altında");
            Assert.AreSame(station, Reference<AdmissionStation>(admission, "station"));
            Assert.IsEmpty(station.GetComponentsInChildren<Canvas>(true).Where(c => c.renderMode != RenderMode.WorldSpace), "istasyonda ekran tuvali yok");
            Assert.IsEmpty(station.GetComponentsInChildren<Button>(true), "istasyonda düz düğme yok");
            Assert.IsEmpty(station.GetComponentsInChildren<GraphicRaycaster>(true), "istasyonda uGUI ışın yakalayıcısı yok");

            // Aşama 10 (T1c): eski uGUI tuval silindi; bileşenler yerinde, alanlar boş (D20 emsali, API kalır)
            Assert.IsNull(presenter.transform.Find("Serbest pano kartları"), "T1c: eski pano tuvali silindi");
            Assert.IsNull(so.FindProperty("canvas").objectReferenceValue, "PlanningBoardView.canvas boş");
            Assert.IsNull(board.DragPlane, "PlanningBoardView.dragPlane boş");
            foreach (var array in new Array[] { board.ProductionMaterials, board.People, board.Equipment, board.Materials, board.Zones, board.Subjects })
                Assert.IsEmpty(array, "T1c: dizi boşaltıldı, null eleman kalmaz");
            Assert.IsEmpty(presenter.GetComponentsInChildren<PlanningBoardDropZone>(false), "etkin eski bırakma bölgesi yok");
            Assert.IsEmpty(presenter.GetComponentsInChildren<PlanningBoardCard>(false), "etkin eski kart yok");
            Assert.IsEmpty(presenter.GetComponentsInChildren<PlanningBoardSurface>(false), "etkin eski yüzey yok");
            Assert.IsEmpty(presenter.GetComponentsInChildren<GraphicRaycaster>(false), "pano altında etkin uGUI ışın yakalayıcısı yok");
        }

        [Test]
        public void SavedScene_PhysicalClosingFadeCoversTheScreenButStartsTransparent()
        {
            // Revizyon 3 · Aşama 4: kararma SiginakShiftClose'un (PLAN §2.2 #7); öksüz ikinci kopya silindi (#9)
            var fade = Reference<CanvasGroup>(One<SiginakShiftClose>(), "fade");
            Assert.IsNull(new SerializedObject(One<SiginakShift>()).FindProperty("receiptFade"), "SiginakShift.receiptFade kalktı");
            Assert.AreEqual(1, roots.Count(r => r.name == "Planning shift blackout"), "tek kararma tuvali");
            Assert.AreEqual(0, fade.alpha);
            Assert.IsFalse(fade.blocksRaycasts);
            var canvas = fade.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            var image = fade.GetComponentInChildren<Image>(true);
            Assert.IsNotNull(image);
            Assert.AreEqual(Color.black, image.color);
            var rect = image.rectTransform;
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.AreEqual(Vector2.zero, rect.offsetMin);
            Assert.AreEqual(Vector2.zero, rect.offsetMax);
        }

        /// <summary>Eksik betik yok; dinamik pano yazılarında DeskLocText yok; pano yazılarında otomatik punto yok.</summary>
        [Test]
        public void SavedScene_NoMissingScriptsAndBoardTextsFollowTheirKind()
        {
            foreach (var transform in roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), transform.name);
            var view3D = One<BoardView3D>();
            var dynamic = new List<TMP_Text>
            {
                Reference<TMP_Text>(view3D, "header"), Reference<TMP_Text>(view3D, "lockTagLine2"), Reference<TMP_Text>(view3D, "hurdaLine2"),
                Reference<TMP_Text>(Reference<WorkOrderView>(view3D, "order"), "numberText"),
                Reference<TMP_Text>(Reference<WorkOrderView>(view3D, "order"), "recipeText"),
                Reference<TMP_Text>(Reference<PersonFolderView>(view3D, "folder"), "coverLine"),
                Reference<TMP_Text>(Reference<LostEnvelopeView>(view3D, "lostEnvelope"), "coverText"),
                // Aşama 5: damga ve YENİ sözcüğü koddan yazılır
                Reference<TMP_Text>(Reference<WorkOrderView>(view3D, "order"), "stampText"),
                Reference<TMP_Text>(Reference<NewItemTag>(Reference<StoreView>(view3D, "store"), "newTag"), "word"),
                // Aşama 6: dosya etiketi koddan (FileTagView)
                Reference<TMP_Text>(Reference<FileTagView>(Reference<PersonFolderView>(view3D, "folder"), "fileTag"), "label"),
            };
            dynamic.AddRange(References<TMP_Text>(Reference<StoreView>(view3D, "store"), "counts"));
            dynamic.AddRange(References<TMP_Text>(Reference<TrainingTopics>(view3D, "topics"), "rows"));
            foreach (var t in dynamic)
                Assert.IsNull(t.GetComponent<DeskLocText>(), "Dinamik yazı; sabit çevirici metni ezmemeli: " + t.name);

            foreach (var t in presenter.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.IsFalse(t.enableAutoSizing, "pano yazısı otomatik puntosuz: " + t.name);
                Assert.IsNotNull(t.font, t.name);
            }
        }

        /// <summary>Kartlı pano bileşenlerinin bütün zorunlu referansları dolu (eleştiri #12 ve R3: yeniden içe aktarmada kopan bağ).</summary>
        [Test]
        public void SavedScene_Rev3BoardComponentsHaveEveryReference()
        {
            var problems = new List<string>();
            foreach (var (type, fields) in Required)
            {
                var all = roots.SelectMany(r => r.GetComponentsInChildren(type, true)).ToList();
                if (all.Count != 1) { problems.Add(type.Name + ": sahnede " + all.Count + " tane (1 olmalı)"); continue; }
                var so = new SerializedObject(all[0]);
                foreach (var field in fields)
                {
                    var p = so.FindProperty(field);
                    if (p == null) { problems.Add(type.Name + "." + field + ": alan yok"); continue; }
                    if (p.isArray && p.propertyType != SerializedPropertyType.String)
                    {
                        if (p.arraySize == 0) problems.Add(type.Name + "." + field + ": boş dizi");
                        for (int i = 0; i < p.arraySize; i++)
                            if (p.GetArrayElementAtIndex(i).objectReferenceValue == null) problems.Add(type.Name + "." + field + "[" + i + "]: boş");
                    }
                    else if (p.propertyType != SerializedPropertyType.ObjectReference) problems.Add(type.Name + "." + field + ": nesne alanı değil");
                    else if (p.objectReferenceValue == null) problems.Add(type.Name + "." + field + ": boş");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // ================================================================== yardımcılar

        Rev3Model Model() => presenter.GetComponentsInChildren<Rev3Model>(true).Single(m => m.ModelName == "PlanningBoard");

        static void CheckStyle(TMP_Text t, string what, TMP_FontAsset font, float emMm, string colour, List<string> problems)
        {
            bool ui = t is TextMeshProUGUI;
            float want = ui ? emMm : emMm / 100f;
            if (font != null && t.font != font) problems.Add($"{what}: font {(t.font != null ? t.font.name : "(yok)")} ≠ {font.name}");
            if (Mathf.Abs(t.fontSize - want) > (ui ? 0.01f : 1e-4f)) problems.Add($"{what}: punto {t.fontSize} ≠ {want}");
            if (t.enableAutoSizing) problems.Add(what + ": otomatik punto");
            if (t.textWrappingMode != TextWrappingModes.NoWrap) problems.Add(what + ": tek satır değil (sarıyor)");
            if (colour != null && ColorUtility.TryParseHtmlString(colour, out var c))
            {
                var got = t.color;
                if (Mathf.Abs(got.r - c.r) > 0.006f || Mathf.Abs(got.g - c.g) > 0.006f || Mathf.Abs(got.b - c.b) > 0.006f || Mathf.Abs(got.a - c.a) > 0.006f)
                    problems.Add($"{what}: renk #{ColorUtility.ToHtmlStringRGBA(got)} ≠ {colour}");
            }
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

        static T[] References<T>(Object owner, string field) where T : Object
        {
            var property = new SerializedObject(owner).FindProperty(field);
            Assert.IsNotNull(property, owner.GetType().Name + "." + field);
            Assert.IsTrue(property.isArray, owner.GetType().Name + "." + field + " dizi");
            var result = new T[property.arraySize];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as T;
                Assert.IsNotNull(result[i], owner.GetType().Name + "." + field + "[" + i + "]");
            }
            return result;
        }
    }
}
