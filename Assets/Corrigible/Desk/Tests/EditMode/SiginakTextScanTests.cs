using System.Linq;
using Corrigible.Desk.EditorTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;

namespace Corrigible.Desk.Tests
{
    /// <summary>Sığınak Aşama 10: punto taraması (1920×1080'de 14 px altı metin yok; tasarım istisnaları gerekçeli).</summary>
    public class SiginakTextScanTests
    {
        [Test]
        public void DeskGameScene_NoTextBelowMinimum()
        {
            var scene = EditorSceneManager.OpenScene(DeskSpriteManifest.Root + "/Scenes/DeskGame.unity", OpenSceneMode.Additive);
            try
            {
                var entries = SiginakTextScan.Scan(scene);
                Assert.Greater(entries.Count, 200, "oda ve ekran katmanlarındaki metinler bulundu");
                var bad = entries.Where(e => e.Violation).Select(e => $"{e.Pixels:0.0} px · {e.Pose} · {e.Path}").ToList();
                Assert.IsEmpty(bad, string.Join("\n", bad));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        /// <summary>Revizyon 2 · Aşama 10: baskın HUD'u ve panelleri (kurallar, telsiz kaydı, onaylar, üzerine gelme kartı) de ≥ 14 px.</summary>
        [Test]
        public void BaskinScene_NoTextBelowMinimum()
        {
            var scene = EditorSceneManager.OpenScene(SiginakTextScan.RaidScene, OpenSceneMode.Additive);
            try
            {
                var entries = SiginakTextScan.ScanRaid(scene);
                Assert.Greater(entries.Count, 100, "baskın HUD'u, panelleri ve çalışırken kurulan satırlar bulundu");
                var bad = entries.Where(e => e.Violation).Select(e => $"{e.Pixels:0.0} px · {e.Path}").ToList();
                Assert.IsEmpty(bad, string.Join("\n", bad));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
