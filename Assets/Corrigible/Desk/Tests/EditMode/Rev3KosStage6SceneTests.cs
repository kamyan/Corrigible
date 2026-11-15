using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Audio;
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
    /// KOS aşama 6: kaydedilmiş <c>DeskGame</c>'de tek seferlik sahne script'inin kurduğu bağlar. İFŞA şeridi (altı hücre, KONUM lambası,
    /// çarpıştırıcı ↔ DeskTarget), son şalter (R19 kipi, <c>lastSwitch</c> açıları, gerekçe plakası, mühür bandı, DeskLocText'ler), KLS pozu,
    /// dosyasız emir kâğıdı, köprünün ve yüzeyin bağları, son raporunun yeni yuvaları ve <c>Cue.ExposureStep</c>'in klibi. Sahne açıkken
    /// (kullanıcının sahnesi) atlanır; sahne kaydedilmeden kapatılır.
    /// </summary>
    public sealed class Rev3KosStage6SceneTests
    {
        const string Path = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";
        const string SoundBankPath = "Assets/Corrigible/Settings/SoundBank.asset";

        Scene scene, previous;
        bool userScene, opened;

        [OneTimeSetUp]
        public void Prepare()
        {
            var existing = SceneManager.GetSceneByPath(Path);
            userScene = existing.IsValid() && existing.isLoaded;
            previous = SceneManager.GetActiveScene();
        }

        [OneTimeTearDown]
        public void CloseWithoutSaving()
        {
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }

        void RequireScene()
        {
            if (userScene) Assert.Ignore("Sahne doğrulaması boş sahne açıkken çalıştırılmalı; kullanıcının açık sahnesine dokunulmadı.");
            if (opened) return;
            scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
            opened = true;
        }

        T One<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        static Object Ref(Object o, string field) => new SerializedObject(o).FindProperty(field).objectReferenceValue;

        [Test]
        public void ExposurePanel_Wired_SixCells_AndTarget()
        {
            RequireScene();
            var panel = One<ExposurePanel>();
            Assert.AreEqual("Exposure panel", panel.name);
            Assert.AreSame(panel, Ref(One<SiginakDesk>(), "exposure"), "SiginakDesk.exposure");
            Assert.AreEqual(6, panel.CellCount, "altı hücre");
            var so = new SerializedObject(panel);
            for (int i = 0; i < 6; i++) Assert.IsNotNull(so.FindProperty("cells").GetArrayElementAtIndex(i).objectReferenceValue, "hücre " + i);
            foreach (var n in new[] { "cellEmpty", "cellEmptyHigh", "cellLow", "cellHigh", "lampOn", "lampOff", "beacon", "target" })
                Assert.IsNotNull(so.FindProperty(n).objectReferenceValue, n);
            var alarm = One<AlarmPanel>();
            Assert.AreSame(Ref(alarm, "cellHigh"), so.FindProperty("cellHigh").objectReferenceValue, "alarm panosunun malzemeleri");
            Assert.AreNotSame(so.FindProperty("cellEmpty").objectReferenceValue, so.FindProperty("cellEmptyHigh").objectReferenceValue,
                "alarm bölgesi ve eşik hücresi boşken de ayırt edilir (kırmızı kenarlı boş hücre)");
            var labels = panel.GetComponentsInChildren<TMP_Text>(true);
            var beaconLabel = labels.FirstOrDefault(t => t.name == "TXT_BeaconLabel");
            Assert.IsNotNull(beaconLabel, "KONUM lambasının plakası");
            Assert.AreEqual("KONUM", beaconLabel.GetComponent<DeskLocText>()?.Source);
            Assert.IsTrue(beaconLabel.gameObject.activeInHierarchy || !panel.gameObject.activeInHierarchy);
            var note = labels.FirstOrDefault(t => t.name == "TXT_Note");
            if (note != null)
            {
                Assert.IsNull(note.GetComponent<DeskLocText>(), "kapalı not çevrilmez");
                Assert.IsTrue(string.IsNullOrEmpty(note.text), "kapalı notta Balance'ın kopyası eşik sayıları yok: " + note.text);
            }
            Assert.IsNotNull(panel.GetComponent<Collider>().GetComponent<DeskTarget>(), "çarpıştırıcı ↔ DeskTarget");
            Assert.AreEqual("İFŞA", panel.Target.Title);
            var title = panel.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "TXT_Title");
            Assert.AreEqual("İFŞA", title.GetComponent<DeskLocText>().Source);
            Assert.Less(panel.transform.position.y + 0.14f, alarm.transform.position.y - 0.15f, "şerit alarm panosunun altında");
        }

        [Test]
        public void LastSwitch_Wired_R19_TokensPlateBand_AndPose()
        {
            RequireScene();
            var bridge = One<SiginakLastSwitch>();
            var sw = (ShiftSwitch)Ref(bridge, "lastSwitch");
            Assert.IsNotNull(sw);
            Assert.AreEqual("Last switch", sw.name);
            Assert.IsTrue(sw.ShiftCloseMode, "R19 kipi");
            Assert.AreEqual("lastSwitch", sw.StateTokens);
            Assert.IsNull(sw.Plate, "masa şalterinin plakası yok; gerekçe plakası ayrı");
            var plate = (LastSwitchPlate)Ref(bridge, "plate");
            Assert.AreSame(sw.gameObject, plate.gameObject);
            Assert.AreEqual("TXT_PlateEN", plate.Line2.name, "ikinci satır gerekçe plakası");
            Assert.IsNull(plate.Line2.GetComponent<DeskLocText>(), "dinamik satır DeskLocText taşımaz");
            var name = sw.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "TXT_PlateTR");
            Assert.AreSame(name, plate.Line1, "birinci satır (şalterin adı) köprüden yazılır");
            Assert.IsNull(name.GetComponent<DeskLocText>(), "oyunun adı olan \"SON ŞALTER\" anahtarı (EN CORRIGIBLE) kullanılmaz");
            Assert.AreEqual("SON ŞALTERİ ÇEK", sw.Target.Title, "üzerine gelme başlığı kendi anahtarıyla");
            Assert.AreEqual("HAZIR", sw.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "TXT_Lamp").GetComponent<DeskLocText>().Source);
            var band = (GameObject)Ref(sw, "lockBand");
            Assert.IsNotNull(band, "mühür bandı");
            Assert.IsTrue(band.transform.IsChildOf(sw.transform), "bant son şalterin kapağında");
            Assert.IsNotNull(sw.GetComponent<Collider>().GetComponent<DeskTarget>());
            Assert.AreSame(sw.Target, plate.Target);
            Assert.AreNotSame(sw, Ref(One<SiginakDesk>(), "shiftSwitch"), "vardiya şalterinden ayrı nesne");

            foreach (var f in new[] { "game", "surface", "rig", "order", "telex", "tutorial", "shiftClose", "shelterEvent", "admission", "briefing" })
                Assert.IsNotNull(Ref(bridge, f), "SiginakLastSwitch." + f);
            Assert.AreSame(bridge, Ref(One<LiberationDeskView>(), "lastSwitch"), "LiberationDeskView.lastSwitch");
            var order = (BriefingView)Ref(bridge, "order");
            Assert.AreEqual("Withdrawal order", order.name);
            Assert.IsFalse(order.gameObject.activeSelf, "emir kâğıdı kapalı başlar");
            Assert.AreNotSame(order, Ref(One<SiginakBriefing>(), "view"), "açılış belgesinden ayrı");

            var rig = One<SiginakCameraRig>();
            Assert.IsTrue(rig.TryGetPose(SiginakPose.LastSwitch, out var slot), "KLS pozu");
            Assert.AreEqual("KLS", slot.anchor.name);
            Assert.AreEqual(44, slot.fieldOfView, 0.01f, "plaka hizasından önden bakış (görsel düzeltme)");
            Assert.AreEqual(SiginakPose.Desk, SiginakDesk.SavedPose(SiginakPose.LastSwitch), "kayıttan devam masaya döner");
            Assert.IsTrue(SiginakDesk.EdgeHidden(SiginakPose.LastSwitch, true), "KLS'de kenar satırı gizli");
        }

        [Test]
        public void FinalReport_NewSlots_Wired()
        {
            RequireScene();
            var view = One<FinalReportView>();
            foreach (var f in new[] { "toneLine", "criteriaRoot", "criteriaTitle", "criteriaRule", "mainTab", "recordTab", "mainTabLabel", "recordTabLabel", "mainPage", "recordPage" })
                Assert.IsNotNull(Ref(view, f), "FinalReportView." + f);
            var so = new SerializedObject(view);
            Assert.AreEqual(5, so.FindProperty("criteria").arraySize);
            Assert.AreEqual(3, so.FindProperty("record").arraySize);
            var action = (RectTransform)((Component)Ref(view, "action")).transform;
            var crit = (RectTransform)((GameObject)Ref(view, "criteriaRoot")).transform;
            Assert.Less(action.anchoredPosition.y, crit.anchoredPosition.y - crit.sizeDelta.y, "YENİDEN BAŞLA ölçüt kutusunun altında");
        }

        [Test]
        public void ExposureStep_HasPlaceholderClip_AndNoMissingScripts()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankPath);
            Assert.IsNotNull(bank.Cue(Cue.ExposureStep).clip, "Cue.ExposureStep (129) yer tutucu klibi");
            RequireScene();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Exposure panel" || t.name == "Last switch" || t.name == "Last switch controller" || t.name == "Withdrawal order")
                        Assert.IsFalse(t.GetComponents<Component>().Any(c => c == null), "Missing Script: " + t.name);
        }
    }
}
