using System.Linq;
using Corrigible.Desk.Bridge;
using Corrigible.Desk.EditorTools;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 10, parça D (aşama 10 tasarımı §9.1, §9.3–9.5; K37, K40, K41, K47): önlem defterinin sahne ve asset bağları. Görev
    /// fişi beş SAHA yuvasıyla kalır (K37 sığma yolu), üç paftanın kırmızı kalem katmanı (<c>FacilityPlanSheet.counter</c>, parts'ın
    /// üstünde, düz çizgi), <c>SiginakOperation.telex</c> (SiginakMap'in teleksi), koku yayıcının parçası (<c>RaidPieceLibrary.scentEmitter</c>,
    /// kendi prefab'ı, gövde ve çarpıştırıcı) ve küçük resmi; tema token'ı <c>plan.counter</c>.
    /// </summary>
    public sealed class FacilityCounterSceneTests
    {
        const string ScenePath = "Assets/Corrigible/Desk/Scenes/DeskGame.unity";

        static T One<T>(UnityEngine.SceneManagement.Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();

        [Test]
        public void DeskGame_SlipFiveFields_PlanCounterLayers_OperationTelex()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                // K37 sığma yolu: fiş beş SAHA yuvasıyla kalır (ÖNLEM boş yuvaya ya da KAZANIM'la tek satıra iner)
                var slip = One<OperationSlip>(scene);
                Assert.AreEqual(LiberationMapBuilder.SlipFieldsMax, new SerializedObject(slip).FindProperty("fields").arraySize);

                var sheets = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FacilityPlanSheet>(true)).ToList();
                Assert.AreEqual(3, sheets.Count);
                foreach (var sheet in sheets)
                {
                    var sso = new SerializedObject(sheet);
                    var counter = (UILineGraphic)sso.FindProperty("counter").objectReferenceValue;
                    var parts = (UILineGraphic)sso.FindProperty("parts").objectReferenceValue;
                    Assert.IsNotNull(counter, sheet.name + ".counter");
                    Assert.AreNotSame(parts, counter);
                    Assert.AreSame(parts.transform.parent, counter.transform.parent, "drawing altında parts'ın kardeşi");
                    Assert.Greater(counter.transform.GetSiblingIndex(), parts.transform.GetSiblingIndex(), "kırmızı kalem parts'ın üstünde");
                    Assert.AreEqual(1.75f, counter.Thickness, 0.01f);
                    Assert.IsFalse(counter.raycastTarget);
                    Assert.AreEqual(0, new SerializedObject(counter).FindProperty("dash").arraySize, "kalem düz çizgi");
                }

                var map = One<SiginakMap>(scene);
                var op = One<SiginakOperation>(scene);
                var feed = new SerializedObject(map).FindProperty("telex").objectReferenceValue;
                Assert.IsNotNull(feed);
                Assert.AreSame(feed, new SerializedObject(op).FindProperty("telex").objectReferenceValue, "SiginakOperation.telex = masanın teleksi");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void ScentEmitter_OwnPiece_Thumb_Token()
        {
            var lib = AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>("Assets/Corrigible/Settings/RaidPieceLibrary.asset");
            Assert.IsNotNull(lib.scentEmitter, "RaidPieceLibrary.scentEmitter atanmış");
            Assert.AreEqual("Assets/Corrigible/Desk/Prefabs/Raid/Gear/ScentEmitter.prefab", AssetDatabase.GetAssetPath(lib.scentEmitter));
            Assert.IsTrue(lib.HasOwnPiece("yayici"));
            Assert.AreSame(lib.scentEmitter, lib.Object("yayici"));
            Assert.AreEqual(Vector3.one, lib.FallbackScale("yayici"));
            Assert.IsNotNull(lib.scentEmitter.transform.Find("Visual/Nozzle"), "buhar ağzı");
            Assert.IsNotNull(lib.scentEmitter.GetComponent<BoxCollider>(), "tıklanır");
            var renderers = lib.scentEmitter.GetComponentsInChildren<Renderer>();
            Assert.Greater(renderers.Length, 3);
            Assert.IsTrue(renderers.All(r => r.sharedMaterials.All(m => m != null)), "malzemeler bağlı");

            var empty = ScriptableObject.CreateInstance<RaidPieceLibrary>();
            try
            {
                Assert.IsFalse(empty.HasOwnPiece("yayici"));
                Assert.AreEqual(empty.emitterFallbackScale, empty.FallbackScale("yayici"), "atanmamışsa yedek ölçek");
            }
            finally { Object.DestroyImmediate(empty); }

            CollectionAssert.Contains(SiginakTextureBaker.PieceThumbNames, "thumb_emitter");
            Assert.IsTrue(DeskSprites.Instance.TryGet("thumb_emitter", out var entry) && entry.sprite != null, "küçük resim kayıtta");
            Assert.AreEqual("thumb_emitter", FacilityGearText.ObjectThumb("yayici"));

            var theme = SiginakTheme.Active;
            var red = theme.ColorOf("plan.counter");
            var part = theme.ColorOf("plan.part");
            Assert.AreNotEqual(part, red, "kırmızı kalem kahve mürekkepten ayrı");
            Assert.Greater(red.r, red.g * 3, "kırmızı");
        }
    }
}
