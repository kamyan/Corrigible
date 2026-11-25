using System.Reflection;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Tek kip (kayıt 21): dengede tek tahtanın alanları yok (ana bayrak ve görev ek turları); tesis kipinin alt bayrakları kalır,
    /// sahadaki sızmacı C# varsayılanıyla açık. Asset'in okunan değeri <see cref="BalanceAssetTests"/>'te.
    /// </summary>
    public class BalanceTests
    {
        [Test]
        public void NoSingleBoardFields()
        {
            foreach (string name in new[] { "raidFacilityEnabled", "liberationLootExtraTurns", "liberationServerExtraTurns" })
                Assert.IsNull(typeof(Balance).GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), name);
            foreach (string name in new[] { "MapSlot", "ExtraTurns" })
                Assert.IsNull(typeof(RaidParams).GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), name);
            Assert.IsNotNull(typeof(RaidParams).GetField("MapSeed"), "MapSeed kalır: sunum tesis odasının bina kitini onunla seçer");
            var b = new Balance();
            Assert.IsTrue(b.infiltratorFieldEnabled, "sahadaki sızmacı varsayılan açık");
            Assert.IsTrue(b.facilityDefenseEnabled && b.facilityMarks && b.facilityCountermeasures && b.facilityTitlesEnabled
                && b.facilitySignatureMachines && b.facilityKatar && b.facilityYalpa, "tesis kipinin alt bayrakları varsayılan açık");
            Assert.IsTrue(b.Clone().infiltratorFieldEnabled);
        }
    }
}
