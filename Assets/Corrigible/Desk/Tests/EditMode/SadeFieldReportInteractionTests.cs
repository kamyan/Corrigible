using System;
using System.Collections.Generic;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk.Tests
{
    /// <summary>Gerçek rapor görünüm modeli, havuzdaki fiş ve masa komut yönlendirmesi birlikte çalışmalı.</summary>
    public sealed class SadeFieldReportInteractionTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);

        static Simulation Ready()
        {
            var sim = Simulation.NewGame(null, "manipulator", 731u,
                exp: new ExpOptions { Teams = true, TimeScale = 1.5 },
                rules: new RuleOptions { FieldReports = true });
            sim.S.Day = 20;
            sim.S.Aware = true;
            sim.S.Region("na").Monitored = true;
            AddReport(sim, "field-1");
            return sim;
        }

        static void AddReport(Simulation sim, string id) => sim.S.FieldReports.Add(new FieldReport
        {
            Id = id, Region = "na", Day = 18, ExpiresDay = 60, Reward = 1, Headline = "Açık saha raporu.",
        });

        static DeskViewModel Build(Simulation sim) => new SimDeskBuilder().Build(new DeskBuildInput
        {
            Sim = sim, Speed = 0, SecondsPerDay = 1, State = new DeskBridgeState(),
            MapSpec = JObject.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Corrigible/Desk/Data/Mock/map.json").text),
        });

        sealed class Fixture : IDisposable
        {
            readonly GameObject root;
            readonly SadeDeskInteraction interaction;
            readonly bool originalIllusion = DeskTheme.Illusion;
            bool wired;
            public readonly SadeMapView Map;
            public readonly FieldReportView Report;
            public readonly Button Button;

            public Fixture()
            {
                // Yaşam döngüsü çağrılarını açıkça sürmek için kök kapalı kalır; sahne değiştirilmez.
                root = new GameObject("Rapor etkileşim testi", typeof(RectTransform));
                root.SetActive(false);
                root.hideFlags = HideFlags.HideAndDontSave;
                Map = root.AddComponent<SadeMapView>();
                interaction = root.AddComponent<SadeDeskInteraction>();
                var paper = new GameObject("Havuz fişi", typeof(RectTransform));
                paper.SetActive(false);
                paper.transform.SetParent(root.transform, false);
                Button = paper.AddComponent<Button>();
                Report = paper.AddComponent<FieldReportView>();
                Set(Report, "collect", Button);
                Set(Map, "reports", new[] { Report });
                Set(interaction, "map", Map);
                Call(Report, "Awake");
                Call(Map, "Awake");
            }

            public void Wire(DeskViewModel model, IDeskCommands commands)
            {
                Set(interaction, "gameVm", model);
                Set(interaction, "gameCommands", commands);
                Call(interaction, "OnEnable");
                wired = true;
            }

            public void Dispose()
            {
                if (wired) Call(interaction, "OnDisable");
                UnityEngine.Object.DestroyImmediate(root);
                DeskTheme.Illusion = originalIllusion;
            }
        }

        [Test]
        public void PendingReport_ActivatesInactivePoolItem_HidesWhenRemoved_AndRebinds()
        {
            var sim = Ready();
            using (var fixture = new Fixture())
            {
                Assert.IsFalse(fixture.Report.gameObject.activeSelf);
                fixture.Map.Bind(Build(sim).Sade.View.Map);
                Assert.IsTrue(fixture.Report.gameObject.activeSelf, "yeni rapor sahnede kapalı duran fişi açmalı");
                Assert.AreEqual("field-1", fixture.Report.Data.Id);
                Assert.IsTrue(fixture.Button.interactable);

                sim.S.FieldReports.Clear();
                fixture.Map.Bind(Build(sim).Sade.View.Map);
                Assert.IsFalse(fixture.Report.gameObject.activeSelf, "toplanan veya süresi dolan rapor görünmemeli");
                Assert.IsNull(fixture.Report.Data);

                AddReport(sim, "field-2");
                fixture.Map.Bind(Build(sim).Sade.View.Map);
                Assert.IsTrue(fixture.Report.gameObject.activeSelf);
                Assert.AreEqual("field-2", fixture.Report.Data.Id);
                Assert.AreEqual("Report:Collect:field-2", fixture.Report.Data.Command);
                Assert.IsTrue(fixture.Button.interactable, "yeniden kullanılan fiş yeni komutla tıklanabilmeli");
            }
        }

        [Test]
        public void ReportButton_DispatchesCompleteCommandOnce_AndPaysOnce()
        {
            var sim = Ready();
            var model = Build(sim);
            var commands = new CommandSpy();
            double before = sim.S.Fon;
            commands.OnRun = command =>
            {
                Assert.AreEqual("Report:Collect:field-1", command, "görünüm modelindeki komuta ikinci önek eklenmemeli");
                Assert.IsTrue(sim.CollectFieldReport(command.Substring("Report:Collect:".Length)).Ok);
            };

            using (var fixture = new Fixture())
            {
                fixture.Wire(model, commands);
                fixture.Map.Bind(model.Sade.View.Map);
                fixture.Button.onClick.Invoke();

                CollectionAssert.AreEqual(new[] { "Report:Collect:field-1" }, commands.Calls);
                Assert.AreEqual(before + 1, sim.S.Fon, "tek düğme çağrısı tek rapor ödülü vermeli");
                Assert.IsEmpty(sim.PendingFieldReports);
                Assert.IsFalse(fixture.Button.interactable, "yenileme gelene kadar aynı fiş tekrar tıklanmamalı");
                fixture.Map.Bind(Build(sim).Sade.View.Map);
                Assert.IsFalse(fixture.Report.gameObject.activeSelf);
            }
        }

        sealed class CommandSpy : IDeskCommands
        {
            public readonly List<string> Calls = new List<string>();
            public Action<string> OnRun;
            public void Run(string command) { Calls.Add(command); OnRun?.Invoke(command); }
            public void SelectRegion(string regionId) { }
            public void ToggleWindow(WindowId id) { }
            public void ExecuteRegionAction(string regionId, int index) { }
            public void LaunchFieldOperation(string regionId) { }
            public void StartResearch(string projectId) { }
            public void PauseResearch(string projectId) { }
            public void SetEmergencyFund(int option) { }
            public void TogglePause() { }
            public void SetSpeed(GameSpeed speed) { }
            public void DismissAdvisor() { }
            public void RunAdvisorAction(string command) { }
            public void ConfirmGlobalKillSwitch() { }
            public void SignEventOption(int index) { }
            public void ReturnToMainMenu() { }
            public void ReviewLogs() { }
            public void RetrySameSeed() { }
            public void Restart() { }
        }
    }
}
