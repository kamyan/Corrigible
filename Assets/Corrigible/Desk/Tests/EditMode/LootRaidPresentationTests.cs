using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    public sealed class LootRaidPresentationTests
    {
        [Test]
        public void LootCommandRoute_PresentsCargoObjectiveAndCollectedResult()
        {
            var root = new GameObject("İkmal görev sunumu");
            root.SetActive(false);
            try
            {
                var source = root.AddComponent<LiveRaidDataSource>();
                var state = Baskin.Create(new RaidParams { Seed = 11, MissionKind = "loot" });
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) state.Cells[y, x] = new Baskin.Cell { T = "floor" };
                state.Cells[3, 3].O = new Baskin.Obj { K = "node", Hp = 4, Max = 4 };
                var technician = state.Units.First(u => u.Type == "tek");
                technician.X = 2; technician.Y = 3;
                source.OpenForValidation(state);
                Assert.IsTrue(source.Current.IsLootMission);
                Assert.AreEqual(Loc.T("İkmal kasası"), source.Current.Targets.Single().Name);
                source.Select('T');
                Assert.AreEqual(Loc.T("İkmal alma"), source.Current.UnitRows[1].Text);
                source.Arm();
                source.Use(new Vector2Int(3, 3));
                Assert.IsTrue(source.Current.Finished);
                Assert.IsTrue(source.Current.Success);
                Assert.AreEqual(Loc.T("İKMAL ALINDI"), source.Current.ResultTitle);
                Assert.IsTrue(source.Current.Targets.Single().Broken);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
