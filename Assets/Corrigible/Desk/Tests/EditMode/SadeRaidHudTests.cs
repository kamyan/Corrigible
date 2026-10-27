using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>Baskın HUD'unun komut davranışı: geri alma, önizleme, iptal sırası, tur onayı, seçim kartı, geri çekilme.</summary>
    public class SadeRaidHudTests
    {
        GameObject owner;
        LiveRaidDataSource source;

        [SetUp]
        public void Setup()
        {
            owner = new GameObject("Baskın veri sınırı");
            owner.SetActive(false);
            source = owner.AddComponent<LiveRaidDataSource>();
            var so = new SerializedObject(source);
            so.FindProperty("theme").objectReferenceValue = SadeTheme.Active;
            so.ApplyModifiedPropertiesWithoutUndo();
            source.OpenForValidation(Baskin.Create(new RaidParams { Seed = 11, Compute = 3 }));
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(owner);

        static Baskin.Unit Technician(Baskin.BState b) => b.Units.First(u => u.Type == "tek");

        /// <summary>Teknisyenin bitişiğine bir robot koyar (sabotaj hedefi) ve tahtayı yeniden açar; robotun karesini döner.</summary>
        Vector2Int RobotNextToTechnician()
        {
            var b = Baskin.Create(new RaidParams { Seed = 11, Compute = 3 });
            var tech = Technician(b);
            var robot = b.Units.First(u => u.Side == 'e' && !u.Dead);
            foreach (var d in Baskin.Dirs)
            {
                int x = tech.X + d[0], y = tech.Y + d[1];
                if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) continue;
                var c = Baskin.CellAt(b, x, y);
                if (c.T != "floor" || c.O != null || b.Units.Any(u => !u.Dead && u.X == x && u.Y == y)) continue;
                robot.X = x; robot.Y = y;
                source.OpenForValidation(b);
                return new Vector2Int(x, y);
            }
            Assert.Fail("Teknisyenin yanında boş kare yok");
            return default;
        }

        [Test]
        public void MoveUndo_RestoresPositionRightsAndDoesNotConsumeRandom()
        {
            source.Select('T');
            var tech = Technician(source.State);
            var before = (tech.X, tech.Y);
            int rng = source.State.Rs;
            var destination = source.Current.Moves.First();
            source.Move(destination);
            Assert.IsFalse(source.Current.Team[0].Move);
            Assert.IsTrue(source.Current.CanUndo);
            Assert.AreEqual(destination.x, Technician(source.State).X);
            source.Undo();
            Assert.AreEqual(before, (Technician(source.State).X, Technician(source.State).Y));
            Assert.IsTrue(source.Current.Team[0].Move);
            Assert.AreEqual(rng, source.State.Rs);
        }

        [Test]
        public void Preview_IsReadOnlyAndCancelUnwindsArmedThenSelectionThenPanel()
        {
            var robot = RobotNextToTechnician();
            source.Select('T');
            source.Arm();
            Assert.IsTrue(source.Current.AbilityArmed);
            var positions = source.State.Units.Select(u => (u.X, u.Y, u.Hp)).ToArray();
            source.Hover(robot);
            CollectionAssert.AreEqual(positions, source.State.Units.Select(u => (u.X, u.Y, u.Hp)).ToArray());
            source.Cancel();
            Assert.IsFalse(source.Current.AbilityArmed);
            Assert.IsNotNull(source.Current.UnitCard);
            source.Cancel();
            Assert.IsNull(source.Current.UnitCard);
            source.Panel("log");
            source.Cancel();
            Assert.IsNull(source.Current.OpenPanel);
            Assert.IsFalse(source.Current.Paused);
        }

        [Test]
        public void AbilityUndo_RestoresVictimAndActionRights()
        {
            var robot = RobotNextToTechnician();
            var before = source.State.Units.Select(u => (u.Id, u.Hp, u.X, u.Y)).ToArray();
            source.Select('T');
            source.Arm();
            source.Use(robot);
            Assert.IsFalse(source.Current.Team[0].Ability);
            Assert.IsTrue(source.Current.CanUndo);
            source.Undo();
            CollectionAssert.AreEqual(before, source.State.Units.Select(u => (u.Id, u.Hp, u.X, u.Y)).ToArray());
            Assert.IsTrue(source.Current.Team[0].Ability);
        }

        [Test]
        public void TurnRequiresConfirmationThenLocksAllCommands()
        {
            source.EndTurn();
            Assert.IsTrue(source.Current.EndConfirm);
            Assert.IsFalse(source.Current.Locked);
            source.Cancel();
            Assert.IsFalse(source.Current.EndConfirm);
            source.EndTurn(); source.EndTurn();
            Assert.IsTrue(source.Current.Locked);
            source.Select('T');
            Assert.IsNull(source.Current.UnitCard);
            source.Arm();
            Assert.IsFalse(source.Current.AbilityArmed);
            source.Speed(true);
            Assert.IsTrue(source.Current.FastMachines);
            source.Speed(false);
            Assert.IsFalse(source.Current.FastMachines);
        }

        [TestCase('T', 2)]
        [TestCase('E', 2)]
        [TestCase('A', 2)]
        public void Selection_UsesUnitSpecificRows(char unit, int count)
        {
            source.Select(unit);
            Assert.AreEqual(count, source.Current.UnitRows.Length);
            Assert.AreEqual(unit, source.Current.UnitCard.Letter);
        }

        [Test]
        public void BadDestinationsCannotMoveOrDamageAndRetreatRequiresTwoRequests()
        {
            source.Select('T');
            source.Move(new Vector2Int(-1, -1));
            source.Arm();
            source.Use(new Vector2Int(99, 99));
            Assert.IsFalse(source.Current.CanUndo);
            source.Retreat();
            Assert.IsTrue(source.Current.RetreatArmed);
            source.Cancel();
            Assert.IsFalse(source.Current.RetreatArmed);
            source.Retreat(); source.Retreat();
            Assert.IsTrue(source.State.Retreated);
        }
    }
}
