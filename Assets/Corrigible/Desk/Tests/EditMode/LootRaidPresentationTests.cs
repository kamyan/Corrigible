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
                state.Units.First(u => u.Type == "emp").X = 6; state.Units.First(u => u.Type == "ana").X = 7;
                source.OpenForValidation(state);
                Assert.IsTrue(source.Current.IsLootMission);
                Assert.AreEqual(Loc.T("İkmal kasası"), source.Current.Targets.Single().Name);
                source.Select('T');
                Assert.AreEqual(Loc.T("İkmal alma"), source.Current.UnitRows[1].Text);
                source.Arm();
                source.Use(new Vector2Int(3, 3));
                // Kasa açıldı: malzeme girişe taşınır
                Assert.IsFalse(source.Current.Finished);
                Assert.IsTrue(source.Current.Escaping);
                // Düzenleme modunda EMRİ İLET makineleri oynatmaz: tur motorda biter (makineler devre dışı)
                foreach (var e in state.Units.Where(u => u.Side == 'e')) e.Dead = true;
                Assert.IsTrue(Baskin.EndTurn(state));
                var exit = state.Exits.OrderBy(p => System.Math.Abs(p.X - technician.X) + System.Math.Abs(p.Y - technician.Y)).First();
                source.Select('T');
                source.Move(new Vector2Int(exit.X, exit.Y));
                Assert.IsTrue(source.Current.Finished);
                Assert.IsTrue(source.Current.Success);
                Assert.AreEqual(Loc.T("İKMAL ALINDI"), source.Current.ResultTitle);
                Assert.IsTrue(source.Current.Targets.Single().Broken);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
