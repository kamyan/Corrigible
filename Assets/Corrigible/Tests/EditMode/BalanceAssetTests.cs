using NUnit.Framework;
using UnityEditor;

namespace Corrigible.Tests
{
    /// <summary>Tek kip (kayıt 21): oyunun dengesi (Settings/BalanceConfig.asset) sahadaki sızmacıyla oynar (alan asset'te yoksa C#
    /// varsayılanı, varsa açık değer).</summary>
    public class BalanceAssetTests
    {
        [Test]
        public void Asset_InfiltratorFieldOn()
        {
            var config = AssetDatabase.LoadAssetAtPath<Corrigible.Game.BalanceConfig>("Assets/Corrigible/Settings/BalanceConfig.asset");
            Assert.IsNotNull(config, "Settings/BalanceConfig.asset");
            Assert.IsTrue(config.balance.infiltratorFieldEnabled);
            Assert.AreEqual("f10", Corrigible.Core.FacilityMaps.FingerprintFor(config.balance), "oyunun tesis izi");
        }

        /// <summary>
        /// Oyunun okuduğu asset C# varsayılanıyla alan alan aynıdır (testler <c>new Balance()</c> ile koşar, oyun asset'le); bilinçli
        /// tek fark <c>liberationMapGen</c> (oyunda 1, üretilmiş harita). Denge değeri iki yerde birden değişir (GELISTIRME.md).
        /// </summary>
        [Test]
        public void Asset_EqualsCodeDefaults_ExceptMapGen()
        {
            var config = AssetDatabase.LoadAssetAtPath<Corrigible.Game.BalanceConfig>("Assets/Corrigible/Settings/BalanceConfig.asset");
            Assert.IsNotNull(config, "Settings/BalanceConfig.asset");
            var a = config.balance;
            var d = new Corrigible.Core.Balance();
            Assert.AreEqual(1, a.liberationMapGen, "oyun üretilmiş haritayla oynar");
            var diff = new System.Collections.Generic.List<string>();
            foreach (var f in typeof(Corrigible.Core.Balance).GetFields())
                if (f.Name != "liberationMapGen" && !object.Equals(f.GetValue(a), f.GetValue(d)))
                    diff.Add(f.Name + " asset=" + f.GetValue(a) + " kod=" + f.GetValue(d));
            CollectionAssert.IsEmpty(diff, string.Join("; ", diff));
        }
    }
}
