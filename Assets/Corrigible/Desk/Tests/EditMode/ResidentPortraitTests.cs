using System.Linq;
using Corrigible.Core;
using NUnit.Framework;
using UnityEditor;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Sakin adları İngilizceye geçince (aşama 3, Docs/TESIS-BASKINLARI-TASARIMI.md §11.4; TASARIM §7.2) portre kitaplığının anahtarları
    /// yeni adlardır: on kurucu ve aday havuzunun ilk 22'si portrelidir, her biri kendi dokusunu bulur (yer tutucu değil). Eşleme
    /// sıraya bağlıdır: eski listenin i'inci kişisinin yüzü yeni listenin i'inci adına geçti.
    /// </summary>
    public class ResidentPortraitTests
    {
        const string LibraryPath = "Assets/Corrigible/Desk/Data/Rev3/PortraitLibrary.asset";

        [Test]
        public void EveryPortraitedResident_HasATexture()
        {
            var lib = AssetDatabase.LoadAssetAtPath<PortraitLibrary>(LibraryPath);
            Assert.IsNotNull(lib, LibraryPath);
            var portraited = CandidateData.Founders.Concat(CandidateData.Names.Take(22)).ToList();
            CollectionAssert.AreEquivalent(portraited.Select(n => n.ToLowerInvariant()).ToArray(),
                lib.Entries.Select(e => e.key).ToArray(), "kitaplığın anahtarları = kurucular + ilk 22 aday (küçük harf)");
            foreach (string name in portraited)
            {
                Assert.IsNotNull(lib.TextureFor(PortraitLibrary.KeyOf(name)), name + ": portre dokusu yok");
                Assert.AreEqual(PortraitLibrary.KeyOf(name), Corrigible.Desk.Bridge.PlanningBoardSnapshotBuilder.PortraitKeyOf(name),
                    name + ": kartın portre anahtarı kitaplığınkiyle aynı");
            }
            Assert.IsNull(lib.TextureFor(PortraitLibrary.KeyOf(CandidateData.Names[22])), "23. aday portresiz (yer tutucu)");
        }
    }
}
