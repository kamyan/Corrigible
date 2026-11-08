using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using Corrigible.Game.Audio;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Revizyon 3 · Aşama 6 (R9, R17; PLAN §2.2–§2.3, W6) kaydedilmiş DeskGame: kapıdaki kabul istasyonu, araçları, damgaları, kâğıtları,
    /// buğu camı, interkom, tezgâh lambası, K8A/K8G/K8R pozları, poz ışığı, kişi dosyasının etiketi ve plaka çevirileri. Sahne gerektiren
    /// testler kullanıcının açık DeskGame'ine dokunmaz (<c>Assert.Ignore</c>); boş sahnede koş. Sahneyi tek seferlik
    /// <c>Rev3Stage6Scene.Apply()</c> kurar; testler o script'e bağlı değildir (o silinir), yalnız kayıtlı sahneyi okur.
    /// </summary>
    public sealed class Rev3AdmissionSceneTests
    {
        const string ScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const string StringsPath = "Assets/Corrigible/Resources/Localization/strings.txt";
        const string RecipePath = "Assets/Corrigible/Desk/Data/Rev3/PoseLightRecipe.asset";
        const string SoundBankPath = "Assets/Corrigible/Settings/SoundBank.asset";
        const float Mm = 0.001f;

        Scene scene, previous;
        bool opened;
        GameObject[] roots;

        /// <summary>Sahneyi ilk istekte açar (kullanıcının açık DeskGame'i varsa yoksayar).</summary>
        void Open()
        {
            if (opened) return;
            var existing = SceneManager.GetSceneByPath(ScenePath);
            if (existing.IsValid() && existing.isLoaded)
                Assert.Ignore("Sahne doğrulaması boş sahne açıkken çalıştırılmalı; kullanıcının açık sahnesine dokunulmadı.");
            previous = SceneManager.GetActiveScene();
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            roots = scene.GetRootGameObjects();
            opened = true;
        }

        [OneTimeTearDown]
        public void CloseWithoutSaving()
        {
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            opened = false;
        }

        T One<T>() where T : Component
        {
            var all = roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToList();
            Assert.AreEqual(1, all.Count, typeof(T).Name + " sayısı");
            return all[0];
        }

        AdmissionStation Station => One<AdmissionStation>();
        SiginakAdmission Flow => One<SiginakAdmission>();
        DoorProp Door => One<DoorProp>();
        SiginakCameraRig Rig => One<SiginakCameraRig>();
        Transform Room => roots.Single(g => g.name == "SiginakRoom").transform;
        static SiginakTheme Theme => SiginakTheme.Active;

        Rev3Model ModelUnder(Component under, string name)
        {
            var all = under.GetComponentsInChildren<Rev3Model>(true).Where(m => m.ModelName == name).ToList();
            Assert.AreEqual(1, all.Count, under.name + " altında " + name);
            return all[0];
        }

        // ================================================================== S1 · bileşenler

        /// <summary>S1: her istasyon bileşeninden bir tane (damga dört, dört karar), bütün bağlar dolu; dört giriş noktası akışa bağlı.</summary>
        [Test]
        public void S1_Required_OneEach()
        {
            Open();
            var station = Station;
            var flow = Flow;
            var glass = One<ColdGlassView>();
            var intercom = One<IntercomTool>();
            var magnifier = One<MagnifierTool>();
            var pad = One<RequestPadTool>();
            var tag = One<FileTagView>();
            var stamps = roots.SelectMany(r => r.GetComponentsInChildren<StampDecision>(true)).ToList();
            Assert.AreEqual(4, stamps.Count, "dört damga");
            CollectionAssert.AreEquivalent(new[] { StampChoice.Admit, StampChoice.Restricted, StampChoice.Reject, StampChoice.Aid }, stamps.Select(s => s.Choice).ToArray());

            var problems = new List<string>();
            Filled(station, problems, "hoverTag", "counterBody", "top", "bracketL", "bracketR", "counterHit", "deskKeyHit", "lampBody", "lampOn", "lampOff",
                "counterSpot", "panel", "drawer", "marker", "jewel", "documentPaper", "stripPaper", "magnifier", "pad");
            ArrayOf(station, "archivePapers", 2, problems);
            ArrayOf(station, "slipPapers", 5, problems);
            ArrayOf(station, "tins", 2, problems);
            ArrayOf(station, "stamps", 4, problems);
            if (new SerializedObject(station).FindProperty("looseTools").arraySize < 6) problems.Add("AdmissionStation.looseTools < 6");
            Assert.AreEqual(4, new SerializedObject(station).FindProperty("lampSlot").intValue, "lamba camı yuvası 4");
            foreach (var s in stamps) Filled(s, problems, "hit", "band", "lockTongue", "frame", "view", "capLabel");
            Filled(magnifier, problems, "frame", "carriage", "hit", "view");
            Filled(pad, problems, "sheet", "sheetPaper", "carbonPaper", "panel", "hit", "view");
            Filled(glass, problems, "cover", "fog", "hit");
            Filled(intercom, problems, "button", "led", "ledOn", "ledOff", "hit");
            Filled(tag, problems, "label");
            Filled(flow, problems, "game", "surface", "rig", "telex", "door", "doorController", "station", "glass", "intercom", "magnifier", "pad", "desk",
                "planningBoard", "hoverTag", "readAnchor");
            ArrayOf(flow, "stamps", 4, problems);
            Assert.IsEmpty(problems, string.Join("\n", problems));

            Assert.AreSame(station, Reference<AdmissionStation>(flow, "station"));
            Assert.AreSame(glass, Reference<ColdGlassView>(flow, "glass"));
            Assert.AreSame(intercom, Reference<IntercomTool>(flow, "intercom"));
            Assert.AreSame(magnifier, Reference<MagnifierTool>(flow, "magnifier"));
            Assert.AreSame(pad, Reference<RequestPadTool>(flow, "pad"));
            Assert.AreSame(magnifier, Reference<MagnifierTool>(station, "magnifier"));
            Assert.AreSame(pad, Reference<RequestPadTool>(station, "pad"));
            Assert.AreSame(Door, Reference<DoorProp>(flow, "door"));
            Assert.AreSame(One<SiginakDoor>(), Reference<SiginakDoor>(flow, "doorController"));
            Assert.AreSame(One<PlanningBoardPresenter>(), Reference<PlanningBoardPresenter>(flow, "planningBoard"));
            Assert.AreSame(Rig, Reference<SiginakCameraRig>(flow, "rig"));
            Assert.AreSame(Reference<HoverTag>(Door.GetComponent<DeskTarget>(), "hoverTag"), Reference<HoverTag>(flow, "hoverTag"), "HUD etiketi kapınınkiyle aynı");
            Assert.AreSame(Reference<HoverTag>(flow, "hoverTag"), Reference<HoverTag>(station, "hoverTag"));
            foreach (var owner in new Component[] { One<SiginakDoor>(), One<SiginakDesk>(), One<PlanningBoardPresenter>(), One<LiberationDeskView>() })
                Assert.AreSame(flow, Reference<SiginakAdmission>(owner, "admission"), owner.GetType().Name + ".admission");
            Assert.AreSame(tag, Reference<FileTagView>(One<PersonFolderView>(), "fileTag"), "PersonFolderView.fileTag");
            // kâğıt kökleri, damgalar ve araçlar istasyonun altında (tık ata zincirinden istasyona çıkar; W2 sözleşmesi)
            foreach (var t in new[] { Reference<Transform>(station, "documentPaper"), Reference<Transform>(station, "stripPaper") }
                         .Concat(References<Transform>(station, "archivePapers")).Concat(References<Transform>(station, "slipPapers")))
                Assert.IsTrue(t.IsChildOf(station.transform), t.name + " istasyonun altında");
            foreach (var s in stamps) Assert.IsTrue(s.transform.IsChildOf(station.transform), s.name);
            Assert.IsTrue(magnifier.transform.IsChildOf(station.transform) && pad.transform.IsChildOf(station.transform));
        }

        // ================================================================== S2 · yerleşim

        /// <summary>
        /// S2: modeller manifestteki yerde (±1 mm): istasyon ADM tabanında, lamba (2,20; 1,78; −0,43), araçlar Top yerelinde (DA:112–133),
        /// damgalar Slot1..4'te, tenekeler çekmecede; buğu camı kanadın Hinge'inin çocuğu ve kanat 95° açılınca birlikte döner. Uydu model
        /// örneklerinde parça taşıma geçersiz kılması yok; tezgâh ve panelde yalnız bileşenin sürdüğü parçalar (katlı hâl).
        /// </summary>
        [Test]
        public void S2_Models_PlacedByManifest()
        {
            Open();
            var station = Station;
            Near(new Vector3(2.2f, 0.98f, -0.27f), station.transform.position, Mm, "istasyon kökü");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), station.transform.rotation), 0.05f, "yaw 90");
            var counter = ModelUnder(station, "AdmissionCounter");
            var panel = ModelUnder(station, "AdmissionPanel");
            var lamp = ModelUnder(station, "CounterLamp");
            Near(station.transform.position, counter.transform.position, Mm, "AdmissionCounter");
            Near(station.transform.position, panel.transform.position, Mm, "AdmissionPanel");
            Near(new Vector3(2.2f, 1.78f, -0.43f), lamp.transform.position, Mm, "CounterLamp");
            var top = counter.Part("Top");
            Assert.AreSame(top, Reference<PartDriver>(station, "top"));

            void OnTop(string model, Vector3 local, float yaw)
            {
                var m = ModelUnder(station, model);
                Assert.AreSame(top.transform, m.transform.parent, model + " Top'un çocuğu");
                Near(local, m.transform.localPosition, Mm, model);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, yaw, 0f), m.transform.localRotation), 0.1f, model + " yaw " + yaw);
            }
            OnTop("DecisionStampRack", new Vector3(0.30f, 0.003f, -0.22f), -8f);
            OnTop("Magnifier", new Vector3(0.02f, 0.0035f, -0.22f), 182f);
            OnTop("RequestPad", new Vector3(-0.295f, 0.003f, -0.215f), 6f);
            OnTop("Pencil", new Vector3(-0.215f, 0.0075f, -0.245f), 70f);
            OnTop("KeyKey", new Vector3(-0.385f, 0f, -0.225f), 0f);
            var key = ModelUnder(station, "KeyKey");
            Assert.IsFalse(key.Part("Tag").gameObject.activeSelf, "MASA tuşunda dikkat fişi (Tag) gizli");

            var rack = ModelUnder(station, "DecisionStampRack");
            foreach (var s in station.GetComponentsInChildren<StampDecision>(true))
            {
                var slot = rack.Anchor("Slot" + ((int)s.Choice + 1));
                Assert.AreSame(slot, s.transform.parent, s.name + " kendi yuvasında (" + s.Choice + ")");
                Near(new Vector3(0f, 0.0005f, 0f), s.transform.localPosition, 1e-4f, s.name);
                Assert.IsNotNull(s.LockTongue, s.name + " kilit dili");
                Assert.IsNull(s.LockTongue.GetComponent<Collider>(), "kilit dilinde çarpıştırıcı yok (ışını yutmasın)");
            }
            var drawer = panel.Part("Drawer");
            var tins = References<GameObject>(station, "tins");
            Assert.IsTrue(tins.All(t => t.transform.parent == drawer.transform && t.GetComponent<Rev3Model>().ModelName == "RationTin"), "iki teneke çekmecede");

            // Buğu camı: Hinge çocuğu, kapalıyken (2,213; 1,46; 0,58), kanatla döner
            var glass = One<ColdGlassView>();
            var hinge = Reference<Transform>(Door, "leaf");
            Assert.AreSame(hinge, glass.transform.parent, "ColdGlass DoorLeaf/Hinge'in çocuğu");
            Near(new Vector3(-0.27f, 1.46f, -0.025f), glass.transform.localPosition, 1e-4f, "Hinge yereli");
            Assert.Less(Quaternion.Angle(Quaternion.identity, glass.transform.localRotation), 0.01f, "birim dönme");
            Near(new Vector3(2.213f, 1.46f, 0.58f), glass.transform.position, 2 * Mm, "kapalı kapıda cam");
            var rest = hinge.localRotation;
            try
            {
                hinge.localRotation = rest * Quaternion.Euler(0f, 95f, 0f);
                Assert.Greater(Vector3.Distance(new Vector3(2.213f, 1.46f, 0.58f), glass.transform.position), 0.1f, "kanat 95°: cam birlikte döner");
                Near(hinge.TransformPoint(glass.transform.localPosition), glass.transform.position, 1e-5f, "cam kanadın çerçevesinde");
            }
            finally { hinge.localRotation = rest; }

            // Parça taşıma geçersiz kılmaları
            var problems = new List<string>();
            var satellites = new List<Rev3Model> { rack, ModelUnder(station, "Magnifier"), ModelUnder(station, "RequestPad"), ModelUnder(station, "Pencil"), key,
                glass.GetComponent<Rev3Model>(), One<FileTagView>().GetComponent<Rev3Model>() };
            satellites.AddRange(station.GetComponentsInChildren<StampDecision>(true).Select(s => s.GetComponent<Rev3Model>()));
            satellites.AddRange(tins.Select(t => t.GetComponent<Rev3Model>()));
            foreach (var m in satellites) problems.AddRange(PartMoves(m.gameObject, new HashSet<string>()));
            var driven = new HashSet<string> { "Top", "BracketL", "BracketR", "Drawer", "Marker" };
            problems.AddRange(PartMoves(counter.gameObject, driven));
            problems.AddRange(PartMoves(panel.gameObject, driven));
            problems.AddRange(PartMoves(lamp.gameObject, new HashSet<string>()));
            Assert.IsEmpty(problems, "model hattı kaynaktır; parçayı bileşen sürer:\n" + string.Join("\n", problems));
            Assert.AreEqual(88f, top.transform.localEulerAngles.x, 0.05f, "kayıtlı tezgâh katlı (Top 88°)");
            Assert.AreEqual(0f, drawer.transform.localPosition.z - drawer.PivotLocal.z, 1e-5f, "çekmece kapalı");
        }

        /// <summary>Örneğin kökü dışındaki Transform'larda konum/dönüş/ölçek geçersiz kılması (izinli parça adları hariç).</summary>
        static IEnumerable<string> PartMoves(GameObject instanceRoot, HashSet<string> allowed)
        {
            var rootSource = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot.transform);
            foreach (var mod in PrefabUtility.GetPropertyModifications(instanceRoot) ?? Array.Empty<PropertyModification>())
            {
                if (!(mod.target is Transform t) || t == rootSource) continue;
                if (!(mod.propertyPath.StartsWith("m_LocalPosition", StringComparison.Ordinal) || mod.propertyPath.StartsWith("m_LocalRotation", StringComparison.Ordinal)
                      || mod.propertyPath.StartsWith("m_LocalScale", StringComparison.Ordinal))) continue;
                if (allowed.Contains(t.name)) continue;
                yield return instanceRoot.name + "/" + t.name + " · " + mod.propertyPath + " = " + mod.value;
            }
        }

        // ================================================================== S3 · kapı

        /// <summary>S3: kartlı oyunun kapısı kırmızı kaydedilmez, siluetler gizli, koridor boş; eski yolun bağları (çetele, siluet, ölümcül lamba) yerinde (D19).</summary>
        [Test]
        public void S3_Door_NoRed_NoVisitors()
        {
            Open();
            var door = Door;
            var lamp = Reference<Renderer>(door, "lamp");
            var fatal = Reference<Material>(door, "lampFatal");
            Assert.IsFalse(lamp.sharedMaterials.Contains(fatal), "vuruş lambası kırmızı kaydedilmez");
            Assert.IsFalse(Reference<Light>(door, "lampLight").enabled, "vuruş ışığı sönük kaydedilir");
            var silhouette = Reference<VisitorSilhouette>(door, "silhouette");
            Assert.IsTrue(silhouette.GetComponentsInChildren<Renderer>(true).All(r => !r.gameObject.activeInHierarchy), "koridorda siluet görünmez");
            Assert.IsNotNull(Reference<ChalkTally>(One<SiginakDoor>(), "tally"), "eski yolun çetelesi bağlı (D19)");
            var station = Station;
            Assert.IsFalse(station.GetComponentsInChildren<Renderer>(true).Any(r => r.sharedMaterials.Contains(fatal)), "istasyonda ölümcül kırmızı malzeme yok");
            Assert.AreSame(Flow, Reference<SiginakAdmission>(One<SiginakDoor>(), "admission"), "kartlı kapı akışa devredilir");
        }

        // ================================================================== S4 · tıklanmayanlar ve gerçek ışın

        /// <summary>
        /// S4: kanat, çark ve gözetleme kapağı tıklanmaz (kanatta buğu camından başka çarpıştırıcı yok; K8'den çarka/gözetlemeye ışın yalnız
        /// kapının kendi kutusuna çarpar, ayrı hedef yok). Etkileşimli hâl taklit edilince (cam, interkom kutuları açık) K8'den cama ve interkoma
        /// ilk çarpılan kendi hedefidir (kapı kutusu kanat yüzünün arkasında, D25). K8A'dan EVRAK çekmecesi ve GİDEN ağzı kapı kutusunca yutulmaz.
        /// </summary>
        [Test]
        public void S4_NotClickable_DoorWheelPeep_RealRays()
        {
            Open();
            var door = Door;
            var leaf = Reference<Transform>(door, "leaf");
            var glass = One<ColdGlassView>();
            var intercom = One<IntercomTool>();
            var doorBox = door.GetComponent<BoxCollider>();
            foreach (var col in leaf.GetComponentsInChildren<Collider>(true))
                Assert.IsTrue(col.transform.IsChildOf(glass.transform), "kanatta buğu camı dışında çarpıştırıcı: " + col.name);
            Assert.IsTrue(Rig.TryGetPose(SiginakPose.Door, out var k8));
            Assert.IsTrue(Rig.TryGetPose(SiginakPose.Admission, out var k8a));
            var glassHit = Reference<Collider>(glass, "hit");
            var intercomHit = Reference<Collider>(intercom, "hit");
            Assert.IsFalse(glassHit.enabled || intercomHit.enabled, "cam ve interkom kutuları kapalı kaydedilir (akış K8 ailesinde açar)");
            try
            {
                glassHit.enabled = true;
                intercomHit.enabled = true;
                Physics.SyncTransforms();
                var toGlass = Hits(k8.anchor.position, glassHit.bounds.center);
                Assert.IsNotEmpty(toGlass, "K8'den cama ışın");
                Assert.AreSame(glassHit, toGlass[0].collider, "cama ilk çarpılan: " + toGlass[0].collider.name);
                var toIntercom = Hits(k8.anchor.position, intercomHit.bounds.center);
                Assert.IsNotEmpty(toIntercom, "K8'den interkoma ışın");
                Assert.AreSame(intercomHit, toIntercom[0].collider, "interkoma ilk çarpılan: " + toIntercom[0].collider.name);
                foreach (var (part, what) in new[] { (Reference<Transform>(door, "wheel"), "çark"), (Reference<Transform>(door, "peepCover"), "gözetleme kapağı") })
                {
                    var hits = Hits(k8.anchor.position, part.position);
                    Assert.IsTrue(hits.Count == 0 || hits[0].collider == doorBox, what + ": ayrı hedef yok (ilk çarpılan " + (hits.Count > 0 ? hits[0].collider.name : "-") + ")");
                }
                var panel = ModelUnder(Station, "AdmissionPanel");
                var drawerFace = panel.Slot("TXT_Drawer").transform.position;
                var outSlot = panel.Anchor("OutSlot").position;
                foreach (var (target, what) in new[] { (drawerFace, "EVRAK çekmecesi"), (outSlot, "GİDEN ağzı") })
                    Assert.IsFalse(Hits(k8a.anchor.position, target).Any(h => h.collider == doorBox), what + " kapı kutusunca yutulmaz");
            }
            finally
            {
                glassHit.enabled = false;
                intercomHit.enabled = false;
                Physics.SyncTransforms();
            }
        }

        List<RaycastHit> Hits(Vector3 from, Vector3 to)
        {
            var dir = to - from;
            return Physics.RaycastAll(from, dir.normalized, dir.magnitude + 0.05f, ~0, QueryTriggerInteraction.Collide)
                .Where(h => h.collider != null && h.collider.gameObject.scene == scene)
                .OrderBy(h => h.distance).ToList();
        }

        // ================================================================== S5 · çarpıştırıcılar

        /// <summary>
        /// S5: istasyon, cam ve interkomun bütün çarpıştırıcıları kendi nesnelerinin ekseninde kutu (GELISTIRME "dönük nesnenin çarpıştırıcısı")
        /// ve kapalı kaydedilir; tezgâh hedefinin üst yüzü keçeyi (Top yereli y 0,003) geçmez; kapı kutusu çerçeve genişliğinde (1,27 m) ve
        /// kanat yüzünün arkasında (dünya x ≥ 2,213). İstasyon, cam ve interkom kutularının hiçbiri kapı kutusunu kesmez ve kapı kutusu onlara
        /// giden odadan (K8, K8A) ışınların önüne geçmez: kapı kutusunun dz/dy aralığıyla örtüşen her kutu onun ön yüzünün (dünya x) önündedir.
        /// Yalnız z aralıklarına bakmak yetmez: istasyon dünya z −0,69 … 0,15, kapı kutusu z 0,115 … 1,385 (W6 sapma 1) — 35 mm'lik z örtüşmesi
        /// duvar düzleminin (x 2,20) iki yanındadır (istasyon odada, kutu kanadın arkasında), 3B'de kesişme yoktur.
        /// </summary>
        [Test]
        public void S5_Colliders_LocalAxes()
        {
            Open();
            var station = Station;
            var glass = One<ColdGlassView>();
            var intercom = One<IntercomTool>();
            var all = station.GetComponentsInChildren<Collider>(true).Concat(glass.GetComponentsInChildren<Collider>(true)).Concat(intercom.GetComponentsInChildren<Collider>(true)).ToList();
            Assert.Greater(all.Count, 12);
            foreach (var col in all)
            {
                Assert.IsInstanceOf<BoxCollider>(col, col.name + ": kutu (kendi ekseninde)");
                Assert.IsFalse(col.enabled, col.name + ": kapalı kaydedilir (yalnız etkileşimliyken açılır)");
            }
            var counterHit = Reference<BoxCollider>(station, "counterHit");
            var top = Reference<PartDriver>(station, "top");
            Assert.AreSame(top.transform, counterHit.transform.parent, "tezgâh hedefi kanatla döner");
            Assert.LessOrEqual(counterHit.center.y + counterHit.size.y / 2f, 0.003f + 1e-6f, "tezgâh hedefi keçenin altında kalır (kâğıt/araç kutuları üstte)");
            var key = Reference<BoxCollider>(station, "deskKeyHit");
            Assert.IsTrue(References<GameObject>(station, "looseTools").Any(go => key.transform.IsChildOf(go.transform)), "MASA tuşu kutusu gevşek araçlarla gizlenir");

            var box = Door.GetComponent<BoxCollider>();
            Assert.AreEqual(1.27f, box.size.x, 1e-4f, "kapı kutusu çerçeve genişliği");
            Assert.AreEqual(2.10f, box.size.y, 1e-4f);
            var b = WorldBounds(box);
            Assert.GreaterOrEqual(b.min.x, 2.213f - 1e-4f, "kapı kutusu kanat yüzünün (buğu camının dibinin) arkasında");
            // Odadan bakan kameralar kapı kutusunun ön yüzünün önünde: kutunun önündeki hedefe giden ışın kutuya girmez
            foreach (var pose in new[] { SiginakPose.Door, SiginakPose.Admission })
            {
                Assert.IsTrue(Rig.TryGetPose(pose, out var slot), "rig'de " + pose);
                Assert.Less(slot.anchor.position.x, b.min.x, pose + " kamerası kapı kutusunun önünde");
            }
            foreach (var col in all.OfType<BoxCollider>())
            {
                var cb = WorldBounds(col);
                bool apart = cb.max.z < b.min.z || cb.min.z > b.max.z || cb.max.y < b.min.y || cb.min.y > b.max.y;
                // Yandaki (z/y aralığı örtüşmeyen) kutuya K8A'dan (z −0,27) giden ışın da kapı kutusunun z aralığına girmez; örtüşen kutu ön yüzün önünde olmalı
                Assert.IsTrue(apart || cb.max.x < b.min.x, Rev3SceneRefAudit.PathOf(col.transform) + ": kapı kutusuyla örtüşür ya da onun arkasında (kutu "
                    + cb.min.ToString("F3") + " … " + cb.max.ToString("F3") + ", kapı " + b.min.ToString("F3") + " … " + b.max.ToString("F3") + ")");
            }
            var gb = WorldBounds(Reference<BoxCollider>(glass, "hit"));
            Assert.Less(gb.max.x, b.min.x, "cam kutusu kapı kutusunun önünde");
            Assert.Less(WorldBounds(Reference<BoxCollider>(intercom, "hit")).max.z, b.min.z, "interkom kutusu kapı kutusunun dışında");
        }

        /// <summary>Kutunun dünya sınırı (kapalı çarpıştırıcıda <c>Collider.bounds</c> boştur; köşelerden).</summary>
        static Bounds WorldBounds(BoxCollider box)
        {
            var t = box.transform;
            var bounds = new Bounds(t.TransformPoint(box.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = box.center + Vector3.Scale(box.size * 0.5f, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                bounds.Encapsulate(t.TransformPoint(corner));
            }
            return bounds;
        }

        // ================================================================== S6 · pozlar

        /// <summary>S6: K8A, K8G, K8R çapaları <c>Poses</c> altında token kameralarında (K8R tezgâh çerçevesinden; çalışma anında kâğıda göre),
        /// FOV 47/30/40; K8 değişmedi (D8: 58°); akışın okuma çapası K8R.</summary>
        [Test]
        public void S6_Poses_K8A_K8G_K8R()
        {
            Open();
            var rig = Rig;
            Assert.IsTrue(rig.TryGetPose(SiginakPose.Door, out var k8));
            Assert.AreEqual(58f, k8.fieldOfView, 0.01f, "K8 58° (D8)");
            Near(new Vector3(0.05f, 1.35f, 0.95f), k8.anchor.position, Mm, "K8 çapası");
            var counter = ModelUnder(Station, "AdmissionCounter").transform;
            var cases = new (SiginakPose pose, string name, Vector3 pos, Vector3 target, float fov)[]
            {
                (SiginakPose.Admission, "K8A", Theme.Vector("cameras.K8A.pos"), Theme.Vector("cameras.K8A.target"), Theme.NumberOf("cameras.K8A.fovV", 47f)),
                (SiginakPose.BreathGlass, "K8G", Theme.Vector("cameras.K8G.pos"), Theme.Vector("cameras.K8G.target"), Theme.NumberOf("cameras.K8G.fovV", 30f)),
                (SiginakPose.AdmissionRead, "K8R", counter.TransformPoint(Theme.Vector("cameras.K8R.counterLocalPos")), counter.TransformPoint(Theme.Vector("cameras.K8R.counterLocalTarget")),
                    Theme.NumberOf("cameras.K8R.fovV", 40f)),
            };
            foreach (var (pose, name, pos, target, fov) in cases)
            {
                Assert.IsTrue(rig.TryGetPose(pose, out var slot), "rig'de " + pose);
                Assert.AreEqual(name, slot.anchor.name, pose.ToString());
                Assert.AreSame(k8.anchor.parent, slot.anchor.parent, name + " Poses altında");
                Near(pos, slot.anchor.position, Mm, name);
                Assert.Less(Vector3.Angle((target - pos).normalized, slot.anchor.forward), 0.2f, name + " bakışı");
                Assert.AreEqual(fov, slot.fieldOfView, 0.01f, name + " FOV");
                Assert.AreEqual(0f, slot.lensShiftY, 1e-6f, name + " lens kaydırmasız");
            }
            Assert.AreEqual(47f, cases[0].fov, 0.01f); Assert.AreEqual(30f, cases[1].fov, 0.01f); Assert.AreEqual(40f, cases[2].fov, 0.01f);
            Near(new Vector3(1.87f, 1.38f, -0.27f), cases[2].pos, 2 * Mm, "K8R başlangıcı (D §1.3)");
            Assert.IsTrue(rig.TryGetPose(SiginakPose.AdmissionRead, out var k8r));
            Assert.AreSame(k8r.anchor, Reference<Transform>(Flow, "readAnchor"), "akışın okuma çapası K8R");
        }

        // ================================================================== S7 · poz ışığı

        /// <summary>
        /// S7: CounterSpot koni 68° (three.js yarım açı 34°), gölgeli, <c>shadowNearPlane</c> ≥ 0,1, ışık ve nesnesi kapalı kaydedilir (istasyon
        /// nesneyi, PoseLighting ışığı sürer); PoseLighting bağı <c>counterSpot</c>; tarifte K8A/K8G/K8R 3 gölgeli (ampul, masa, tezgâh), K8 en
        /// çok 3 ve tezgâh lambası açık; öteki pozlarda tezgâh lambası kapalı.
        /// </summary>
        [Test]
        public void S7_PoseLight_K8Family()
        {
            Open();
            var spotGo = Reference<GameObject>(Station, "counterSpot");
            var light = spotGo.GetComponent<Light>();
            Assert.IsNotNull(light);
            Assert.AreEqual(LightType.Spot, light.type);
            Assert.AreEqual(Theme.NumberOf("admission.counterSpot.spotAngle", 68f), light.spotAngle, 0.01f);
            Assert.AreEqual(68f, light.spotAngle, 0.01f, "three.js yarım açı 34° → Unity koni 68°");
            Assert.AreNotEqual(LightShadows.None, light.shadows);
            Assert.GreaterOrEqual(light.shadowNearPlane, 0.1f, "lambanın kendi siperi gölge düşürmesin");
            Assert.IsFalse(spotGo.activeSelf, "aday yokken tezgâh lambası nesnesi kapalı");
            Assert.IsFalse(light.enabled, "ışığı PoseLighting pozla açar");
            var lighting = One<PoseLighting>();
            var bindings = Enumerable.Range(0, lighting.BindingCount).Select(lighting.BindingAt).ToList();
            Assert.AreSame(light, bindings.Single(b => b.id == "counterSpot").light, "PoseLighting counterSpot");

            var recipe = AssetDatabase.LoadAssetAtPath<PoseLightRecipe>(RecipePath);
            foreach (var pose in new[] { SiginakPose.Admission, SiginakPose.BreathGlass, SiginakPose.AdmissionRead })
            {
                Assert.IsTrue(recipe.TryGet(pose, out var e), pose.ToString());
                Assert.AreEqual(3, PoseLightRecipe.ShadowedCount(e), pose + " 3 gölgeli");
                Assert.IsTrue(e.lights.Single(l => l.id == "counterSpot").on && e.lights.Single(l => l.id == "counterSpot").shadows, pose + " tezgâh lambası");
            }
            Assert.IsTrue(recipe.TryGet(SiginakPose.Door, out var k8));
            Assert.LessOrEqual(PoseLightRecipe.ShadowedCount(k8), 3, "K8 en çok 3 gölgeli (kabul dizisinde tezgâh lambası kanat açılmadan söner)");
            Assert.IsTrue(k8.lights.Single(l => l.id == "counterSpot").on, "K8'de aday varken tezgâh lambası (3. gölgeli)");
            foreach (var e in recipe.Entries.Where(x => x.pose != SiginakPose.Door && x.pose != SiginakPose.Admission && x.pose != SiginakPose.BreathGlass && x.pose != SiginakPose.AdmissionRead))
                Assert.IsFalse(e.lights.Single(l => l.id == "counterSpot").on, e.pose + ": tezgâh lambası kapalı");
            // kanat açıkken koridorun yakın lambası gölgeli: K8'in tarifi + koridor = en çok 4, tezgâh lambası o anda sönük (§1.11)
            var stairs = References<Light>(Door, "stairLights");
            Assert.LessOrEqual(PoseLightRecipe.ShadowedCount(k8) - 1 + stairs.Count(l => l.shadows != LightShadows.None), 3,
                "kabul dizisinde kanat açıkken: ampul, masa, koridor (tezgâh lambası katlı)");
        }

        // ================================================================== S9 · kâğıtlar

        /// <summary>S9: kâğıt yüzlerinin yazıları fontun kendi (ışıksız) malzemesiyle ve ışın yakalamaz; GraphicRaycaster yok; kâğıt malzemesinin
        /// rengi albedo ≤ 0,8 (token <c>admission.paperAlbedo</c>); kâğıt Quad'ları gölge düşürmez.</summary>
        [Test]
        public void S9_Papers_UnlitInk_NoRaycast()
        {
            Open();
            var station = Station;
            var pad = One<RequestPadTool>();
            var papers = new List<Transform> { Reference<Transform>(station, "documentPaper"), Reference<Transform>(station, "stripPaper"),
                Reference<Transform>(pad, "sheetPaper"), Reference<Transform>(pad, "carbonPaper") };
            papers.AddRange(References<Transform>(station, "archivePapers"));
            papers.AddRange(References<Transform>(station, "slipPapers"));
            Assert.AreEqual(11, papers.Distinct().Count(), "belge, şerit, 2 ESKİ, 5 fiş, talep yaprağı, karbon");
            float albedo = Theme.NumberOf("admission.paperAlbedo", 0.8f);
            var problems = new List<string>();
            foreach (var p in papers)
            {
                Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(p.gameObject), p.name + " kâğıt prefab'ı örneği");
                if (p.GetComponentsInChildren<GraphicRaycaster>(true).Length > 0) problems.Add(p.name + ": GraphicRaycaster");
                foreach (var t in p.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.font != null && t.fontSharedMaterial != t.font.material) problems.Add(p.name + "/" + t.name + ": ışıksız font malzemesi değil (" + t.fontSharedMaterial?.name + ")");
                    if (t.raycastTarget) problems.Add(p.name + "/" + t.name + ": raycastTarget");
                }
                foreach (var g in p.GetComponentsInChildren<Graphic>(true)) if (g.raycastTarget) problems.Add(p.name + "/" + g.name + ": Graphic raycastTarget");
                foreach (var mr in p.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<TMP_Text>() == null))
                {
                    if (mr.shadowCastingMode != ShadowCastingMode.Off) problems.Add(p.name + "/" + mr.name + ": gölge düşürür");
                    var m = mr.sharedMaterial;
                    if (m != null && m.HasProperty("_BaseColor"))
                    {
                        var c = m.GetColor("_BaseColor");
                        if (Mathf.Max(c.r, c.g, c.b) > albedo + 0.002f) problems.Add(p.name + "/" + mr.name + ": " + m.name + " albedo " + Mathf.Max(c.r, c.g, c.b).ToString("0.000") + " > " + albedo);
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            var fog = Reference<Renderer>(One<ColdGlassView>(), "fog");
            Assert.AreEqual("Cold fog", fog.sharedMaterial.name);
            Assert.AreEqual(108f, fog.transform.localScale.x * 1000f, 0.5f, "buğu Ø 108 mm");
            Assert.AreEqual(ShadowCastingMode.Off, fog.shadowCastingMode);
            Assert.IsNull(fog.GetComponent<Collider>(), "buğu Quad'ında çarpıştırıcı yok");
        }

        // ================================================================== S10 · sesler

        /// <summary>S10 (D23, D5 = c): kabul cue'ları 120–128 enum'un sonunda, bankada yer tutucu klipli (klipsiz cue sessiz kalırdı).</summary>
        [Test]
        public void S10_Sounds_CueMap()
        {
            var cues = new[] { Cue.AdmissionCounterDown, Cue.AdmissionDrawer, Cue.AdmissionMarker, Cue.AdmissionSlip, Cue.AdmissionIntercom,
                Cue.AdmissionGlass, Cue.AdmissionPadTear, Cue.AdmissionStampLift, Cue.AdmissionCounterFold };
            CollectionAssert.AreEqual(Enumerable.Range(120, 9).ToArray(), cues.Select(c => (int)c).ToArray());
            Assert.AreEqual(128, Enum.GetValues(typeof(Cue)).Cast<Cue>().Max(c => (int)c), "128 en sonda (asset'te sayı)");
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankPath);
            Assert.IsNotNull(bank, SoundBankPath);
            foreach (var cue in cues) Assert.IsNotNull(bank.Cue(cue).clip, cue + ": klip yok (D5 = c)");
        }

        // ================================================================== plakalar

        /// <summary>
        /// R17-EN: plakalar yuva yuva (EVRAK/PAPERS, GİDEN · TELSİZ/OUT · RADIO, TALİMAT/ORDERS, ESKİ/OLD, BULGU/FINDING, BULGULAR/FINDINGS,
        /// KABUL/ADMISSION, raf TXT_S1–4, ERZAK/RATIONS ×2, BUĞU CAMI/BREATH GLASS, İNTERKOM/INTERCOM, MASA/DESK) <c>DeskLocText</c> kaynağı = metin
        /// ve tablodaki EN; 3B plakalar ışıklı TMP malzemesinde; TALİMAT/BULGULAR em ≥ 10,6 mm (D14); damga sapları ve dosya etiketi koddan
        /// yazılır (DeskLocText yok).
        /// </summary>
        [Test]
        public void Plates_DeskLocText_TrEn()
        {
            Open();
            var station = Station;
            var english = Corrigible.Core.Loc.ParseTable(File.ReadAllText(StringsPath))[Corrigible.Core.Loc.English];
            var cases = new (Component under, string model, string slot, string tr, string en)[]
            {
                (station, "AdmissionPanel", "TXT_Drawer", "EVRAK", "PAPERS"),
                (station, "AdmissionPanel", "TXT_Out", "GİDEN · TELSİZ", "OUT · RADIO"),
                (station, "AdmissionPanel", "TXT_Holder", "TALİMAT", "ORDERS"),
                (station, "AdmissionPanel", "TXT_Archive", "ESKİ", "OLD"),
                (station, "AdmissionPanel", "TXT_Slip", "BULGU", "FINDING"),
                (station, "AdmissionCounter", "TXT_Findings", "BULGULAR", "FINDINGS"),
                (station, "AdmissionCounter", "TXT_Under", "KABUL", "ADMISSION"),
                (station, "DecisionStampRack", "TXT_S1", "KABUL ·\nTAM ERİŞİM", "ADMIT ·\nFULL ACCESS"),
                (station, "DecisionStampRack", "TXT_S2", "KISITLI\nKABUL", "RESTRICTED\nADMISSION"),
                (station, "DecisionStampRack", "TXT_S3", "REDDET", "REJECT"),
                (station, "DecisionStampRack", "TXT_S4", "DIŞARIDAN\nYARDIM", "OUTSIDE\nAID"),
                (One<ColdGlassView>(), "ColdGlass", "TXT_Plate", "BUĞU CAMI", "BREATH GLASS"),
                (One<IntercomTool>(), "IntercomBox", "TXT_Plate", "İNTERKOM", "INTERCOM"),
            };
            var problems = new List<string>();
            void Check(TMP_Text t, string what, string tr, string en)
            {
                var loc = t != null ? t.GetComponent<DeskLocText>() : null;
                if (loc == null) { problems.Add(what + ": DeskLocText yok"); return; }
                if (loc.Source != tr) problems.Add(what + ": kaynak '" + loc.Source + "' ≠ '" + tr + "'");
                if (t.text != loc.Source) problems.Add(what + ": metin kaynakla aynı değil");
                if (!english.TryGetValue(tr, out var got) || got != en) problems.Add(what + ": EN '" + got + "' ≠ '" + en + "'");
                if (t is TextMeshPro && (t.fontSharedMaterial == null || !t.fontSharedMaterial.name.Contains("URP Lit"))) problems.Add(what + ": ışıklı TMP malzemesi değil (" + t.fontSharedMaterial?.name + ")");
            }
            foreach (var (under, model, slot, tr, en) in cases)
            {
                var m = under.GetComponentsInChildren<Rev3Model>(true).Single(x => x.ModelName == model);
                var t = m.Slot(slot)?.Text;
                Check(t, model + "/" + slot, tr, en);
                if (slot == "TXT_Holder" || slot == "TXT_Findings") Assert.GreaterOrEqual(t.fontSize, 0.106f - 1e-5f, slot + " em ≥ 10,6 mm (D14)");
            }
            var tins = References<GameObject>(station, "tins");
            Assert.AreEqual(2, tins.Length);
            foreach (var tin in tins) Check(tin.GetComponent<Rev3Model>().Slot("TXT_Stencil")?.Text, tin.name + "/TXT_Stencil", "ERZAK", "RATIONS");
            var masa = station.GetComponentsInChildren<TextMeshPro>(true).Where(t => t.GetComponent<DeskLocText>() != null && t.GetComponent<DeskLocText>().Source == "MASA").ToList();
            Assert.AreEqual(1, masa.Count, "MASA plakası");
            Check(masa[0], "MASA plakası", "MASA", "DESK");
            Assert.IsTrue(References<GameObject>(station, "looseTools").Any(go => masa[0].transform.IsChildOf(go.transform)), "MASA plakası gevşek araçlarla gizlenir");
            foreach (var s in station.GetComponentsInChildren<StampDecision>(true))
            {
                var cap = Reference<TMP_Text>(s, "capLabel");
                if (cap.GetComponent<DeskLocText>() != null) problems.Add(s.name + ": sap yazısında DeskLocText (StampData.Cap yazar)");
                if (cap.transform.parent == null || !cap.transform.IsChildOf(s.transform)) problems.Add(s.name + ": capLabel kendi sapında değil");
            }
            var tagLabel = Reference<TMP_Text>(One<FileTagView>(), "label");
            if (tagLabel.GetComponent<DeskLocText>() != null) problems.Add("FileTag/TXT_Tag: DeskLocText (TagText koddan)");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // ================================================================== yardımcılar

        static void Near(Vector3 want, Vector3 got, float tol, string what) =>
            Assert.Less(Vector3.Distance(want, got), tol, what + ": " + got.ToString("F4") + " (beklenen " + want.ToString("F4") + ")");

        static void Filled(Object owner, List<string> problems, params string[] fields)
        {
            var so = new SerializedObject(owner);
            foreach (var f in fields)
            {
                var p = so.FindProperty(f);
                if (p == null) problems.Add(owner.GetType().Name + "." + f + ": alan yok");
                else if (p.objectReferenceValue == null) problems.Add(owner.GetType().Name + "." + f + ": boş");
            }
        }

        static void ArrayOf(Object owner, string field, int count, List<string> problems)
        {
            var p = new SerializedObject(owner).FindProperty(field);
            if (p == null || !p.isArray) { problems.Add(owner.GetType().Name + "." + field + ": dizi yok"); return; }
            if (p.arraySize != count) problems.Add(owner.GetType().Name + "." + field + ": " + p.arraySize + " (beklenen " + count + ")");
            for (int i = 0; i < p.arraySize; i++)
                if (p.GetArrayElementAtIndex(i).objectReferenceValue == null) problems.Add(owner.GetType().Name + "." + field + "[" + i + "]: boş");
        }

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
