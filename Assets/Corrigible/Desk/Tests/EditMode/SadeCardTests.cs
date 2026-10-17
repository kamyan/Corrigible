using Corrigible.Core;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>Kartın komut kapısı, anlık maliyet sunumu ve uzayan dosyanın kaydırma sözleşmesi.</summary>
    public class SadeCardTests
    {
        [SetUp] public void Turkish() => Loc.SetLanguage(Loc.Turkish);
        [TearDown] public void RestoreLanguage() => Loc.SetLanguage(Loc.Turkish);

        static T Ref<T>(Object owner, string name) where T : Object =>
            new SerializedObject(owner).FindProperty(name).objectReferenceValue as T;

        static GameObject Instance(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Sade/Cards/" + name + ".prefab");
            Assert.NotNull(asset, "F3 kart prefab'ı tek seferlik kurulumla yazılmalı.");
            return Object.Instantiate(asset);
        }

        static JObject Sample(string file, string field) =>
            JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Sade/Mock/" + file + ".json").text)[field] as JObject;

        [Test]
        public void RegionAction_DisabledDoesNotExecuteAndRebindUsesCurrentCommand()
        {
            var go = Instance("SadeRegionCard");
            try
            {
                var action = go.GetComponentsInChildren<SadeCardActionView>(true)[0];
                int count = 0; string command = null;
                action.Clicked += value => { count++; command = value; };
                action.Bind(new JObject { ["id"] = "monitorNet", ["disabledReason"] = "Fon yetersiz" });
                action.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                Assert.AreEqual(0, count);
                action.Bind(new JObject { ["id"] = "audit", ["key"] = "2" });
                action.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
                Assert.AreEqual(1, count);
                Assert.AreEqual("denetim", command);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RegionPreview_ImmediatePowerCostDoesNotInventZeroDayOrFundCost()
        {
            var go = Instance("SadeRegionCard");
            try
            {
                var view = go.GetComponent<SadeRegionCardView>();
                var data = Sample("masa_S2", "regionCard");
                data["preview"] = new JObject
                {
                    ["actionId"] = "quarantine", ["days"] = 0, ["costFund"] = 0,
                    ["costPower"] = 15, ["resultStampKey"] = "stamp.ready"
                };
                view.Bind(data);
                string text = Ref<TMP_Text>(view, "previewCost").text;
                StringAssert.Contains("hemen", text);
                StringAssert.Contains("15 SG", text);
                StringAssert.DoesNotContain("0 gün", text);
                StringAssert.DoesNotContain("0 fon", text);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void FullDossier_AdditionalPresentedSourcesGrowScrollablePage()
        {
            var go = Instance("SadeFullDossier");
            try
            {
                var view = go.GetComponent<SadeFullDossierView>();
                var data = Sample("masa_S9", "dossier");
                var rows = (JArray)data["sources"];
                while (rows.Count < 8) rows.Add(rows[1].DeepClone());
                view.Bind(data);
                var scroll = Ref<ScrollRect>(view, "scroll");
                Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
                Assert.IsTrue(scroll.vertical);
                Assert.IsFalse(scroll.horizontal);
                Assert.AreEqual(8, go.GetComponentsInChildren<SadeCardSourceRow>(true).Length);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void FullDossier_EmptyHistoryClearsPreviousRegionMarkers()
        {
            var go = Instance("SadeFullDossier");
            try
            {
                var view = go.GetComponent<SadeFullDossierView>();
                var data = Sample("masa_S9", "dossier");
                view.Bind(data);
                var history = Ref<SadeDossierHistoryView>(view, "history");
                Assert.IsTrue(Ref<Image>(history, "fieldDiamond").gameObject.activeSelf);
                data["history"] = new JObject();
                view.Bind(data);
                Assert.IsFalse(Ref<Image>(history, "fieldDiamond").gameObject.activeSelf);
                Assert.IsFalse(Ref<Image>(history, "lastRing").gameObject.activeSelf);
                Assert.IsFalse(Ref<UILineGraphic>(history, "report").gameObject.activeSelf);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
