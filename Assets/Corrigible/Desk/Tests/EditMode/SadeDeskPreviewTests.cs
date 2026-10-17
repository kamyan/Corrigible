using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    public class SadeDeskPreviewTests
    {
        static SadeDeskViewModel Sample() => MockSadeDeskDataSource.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(
            "Assets/Corrigible/Desk/Data/Sade/Mock/masa_S1.json"));

        [Test]
        public void Source_ReenableRebuildsNullableValuesFromSample()
        {
            var go = new GameObject("Sade veri kaynağı");
            go.SetActive(false);
            try
            {
                var source = go.AddComponent<MockSadeDeskDataSource>();
                var serialized = new SerializedObject(source);
                serialized.FindProperty("sample").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/Corrigible/Desk/Data/Sade/Mock/masa_S1.json");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual(12, source.Current.Map.Regions.Single(r => r.Id == "KA").Value);
                source.Current.Map.Regions.Single(r => r.Id == "KA").Value = null;
                go.SetActive(true);
                Assert.AreEqual(12, source.Current.Map.Regions.Single(r => r.Id == "KA").Value);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void S1_LeavesUnknownInfiltrationHiddenAndKeepsUncertainty()
        {
            var vm = Sample();
            Assert.AreEqual(12, vm.Map.Regions.Count);
            Assert.AreEqual(12, vm.Map.Regions.Select(r => r.Id).Distinct().Count());
            Assert.IsTrue(vm.Map.Regions.All(r => new[] { "verified", "report", "suspicious", "unknown" }.Contains(r.Trust)));
            Assert.IsTrue(vm.Map.Regions.Where(r => r.Value.HasValue).All(r => r.Value.Value >= 0 && r.Value.Value <= 100));
            var unknown = vm.Map.Regions.Where(r => !r.Value.HasValue).ToList();
            CollectionAssert.AreEquivalent(new[] { "OA", "SAA" }, unknown.Select(r => r.Id));
            Assert.IsTrue(unknown.All(r => r.Trust == "unknown" && r.QuestionMark));
            CollectionAssert.AreEqual(new[] { "DoA" }, vm.Map.AlertRegions);
            Assert.AreEqual("GDA", vm.Map.Regions.Single(r => r.Core).Id);
            var singularity = vm.Risks.Single(r => r.Id == "singularity");
            Assert.AreEqual("report", singularity.Trust);
            Assert.IsTrue(singularity.Approx && singularity.Percent);
            Assert.AreEqual(6, singularity.Uncertainty);
            CollectionAssert.AreEqual(new[] { 31f, 43f }, singularity.Bar.Uncertainty);
        }

        [Test]
        public void S1_AcceptsNullBadgesAndKeepsClockStatic()
        {
            var vm = Sample();
            Assert.AreEqual(5, vm.Dock.Tabs.Count);
            Assert.IsNull(vm.Dock.Tabs.Single(t => t.Id == "research").BadgeType);
            var news = vm.Dock.Tabs.Single(t => t.Id == "news");
            Assert.AreEqual("count", news.BadgeType);
            Assert.AreEqual(3, news.BadgeValue);
            Assert.AreEqual("dot", vm.Dock.Tabs.Single(t => t.Id == "aiLog").BadgeType);
            Assert.AreEqual("074", vm.Clock.DayText);
            Assert.AreEqual("14:32", vm.Clock.Time);
            Assert.AreEqual(0, vm.Clock.MinutesPerSecond);
            Assert.IsFalse(vm.Clock.Paused);
        }

        [Test]
        public void S1_RoutesTeamsAndThreadsKeepRegionIdentities()
        {
            var vm = Sample();
            Assert.AreEqual(4, vm.Map.SpreadThreads.Count);
            var ids = vm.Map.Regions.Select(r => r.Id).ToArray();
            foreach (var thread in vm.Map.SpreadThreads)
            {
                CollectionAssert.Contains(ids, thread.From);
                CollectionAssert.Contains(ids, thread.To);
            }
            Assert.AreEqual(3, vm.Map.Teams.Count);
            Assert.AreEqual("KA", vm.Map.Teams.Single(t => t.Id == "A").Region);
            var seoul = vm.Map.Teams.Single(t => t.Id == "B");
            Assert.AreEqual("DoA", seoul.Region);
            Assert.AreEqual("Seul", seoul.City);
            var travelling = vm.Map.Teams.Single(t => t.Id == "C");
            Assert.AreEqual("travelling", travelling.Status);
            Assert.AreEqual("BA", travelling.To);
            Assert.AreEqual(2, travelling.EtaDays);
        }

        [Test]
        public void Illusion_MasksMockSnapshotAndRestoresUnknownRegions()
        {
            var go = new UnityEngine.GameObject("İllüzyon örnek kaynağı");
            try
            {
                var source = go.AddComponent<MockSadeDeskDataSource>();
                var so = new UnityEditor.SerializedObject(source);
                so.FindProperty("sample").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/Corrigible/Desk/Data/Sade/Mock/masa_S1.json");
                so.ApplyModifiedPropertiesWithoutUndo();
                source.Reload();
                Assert.IsNull(source.Current.Map.Regions.Single(r => r.Id == "OA").Value);
                source.SetIllusion(true);
                Assert.IsTrue(source.Current.Map.Regions.All(r => r.Value == 0 && r.Trust == "verified"));
                Assert.IsEmpty(source.Current.Map.SpreadThreads);
                Assert.IsFalse(source.Current.Map.Regions.Any(r => r.Core));
                Assert.AreEqual(Newtonsoft.Json.Linq.JTokenType.Null, source.ReadExample(1)["map"]["regions"][1]["infiltration"].Type);
                source.SetIllusion(false);
                Assert.IsNull(source.Current.Map.Regions.Single(r => r.Id == "OA").Value);
                Assert.AreEqual("unknown", source.Current.Map.Regions.Single(r => r.Id == "OA").Trust);
                Assert.AreEqual(4, source.Current.Map.SpreadThreads.Count);
                Assert.AreEqual(71, source.Current.Map.Regions.Single(r => r.Core).Value);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void S1_ReparseKeepsSourcesStableAcrossLanguagesAndDoesNotShareMutableState()
        {
            var original = DeskLoc.Instance.Lang;
            try
            {
                DeskLoc.Instance.SetLanguage(DeskLang.Tr);
                var tr = Sample();
                var fund = tr.Resources.Single(c => c.Id == "fund");
                var risk = tr.Risks.Single(c => c.Id == "singularity");
                Assert.AreEqual("FON", fund.Label);
                Assert.AreEqual("47,3", SadeUi.Value(fund.Value, fund.Decimals, fund.Percent, fund.Approx));
                Assert.AreEqual("≈%37", SadeUi.Value(risk.Value, risk.Decimals, risk.Percent, risk.Approx));
                Assert.AreEqual("−1,2", DeskFormat.Signed(fund.Delta));
                var ids = tr.Map.Regions.Select(r => r.Id).ToArray();
                fund.Value = -99;
                tr.Map.Regions.Single(r => r.Id == "OA").Value = 99;
                tr.Map.SpreadThreads.Clear();

                DeskLoc.Instance.SetLanguage(DeskLang.En);
                var en = Sample();
                fund = en.Resources.Single(c => c.Id == "fund");
                risk = en.Risks.Single(c => c.Id == "singularity");
                Assert.AreEqual("FON", fund.Label, "VM kaynak metni çevrilmeden kalmalı");
                Assert.AreEqual("FUNDS", DeskLoc.Instance.T(fund.Label));
                Assert.AreEqual("47.3", SadeUi.Value(fund.Value, fund.Decimals, fund.Percent, fund.Approx));
                Assert.AreEqual("≈37%", SadeUi.Value(risk.Value, risk.Decimals, risk.Percent, risk.Approx));
                Assert.AreEqual("−1.2", DeskFormat.Signed(fund.Delta));
                CollectionAssert.AreEqual(ids, en.Map.Regions.Select(r => r.Id));
                Assert.IsNull(en.Map.Regions.Single(r => r.Id == "OA").Value);
                Assert.AreEqual(4, en.Map.SpreadThreads.Count);
            }
            finally { DeskLoc.Instance.SetLanguage(original); }
        }
    }
}
