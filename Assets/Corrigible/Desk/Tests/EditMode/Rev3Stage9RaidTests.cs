using System.Linq;
using Corrigible.Desk.EditorTools;
using Corrigible.Game.Raid;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Corrigible.Desk.Tests
{
    /// <summary>R11/R12: bölge kitlerinin gerçek model yolları, kanal maskeleri ve menteşe sözleşmesi.</summary>
    public sealed class Rev3Stage9RaidTests
    {
        static RaidPieceLibrary Lib=>AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(RaidKitPreview.LibraryPath);
        static SerializedProperty P(Object o,string n)=>new SerializedObject(o).FindProperty(n);
        static T R<T>(Object o,string n)where T:Object=>(T)P(o,n).objectReferenceValue;
        [Test]
        public void AllThirteenKits_UseDifferentReliefsAndCivilDevices_AndLowEntryDoors()
        {
            var kits=new[]{Lib.defaultKit}.Concat(Lib.kits).ToArray();Assert.AreEqual(13,kits.Length);
            Assert.AreEqual(13,kits.Select(k=>k.wallModel.mesh).Distinct().Count());Assert.AreEqual(13,kits.Select(k=>k.civilianModel).Distinct().Count());
            foreach(var k in kits)
            {
                Assert.IsNotNull(k.wallModel.mesh,k.id);Assert.IsNotNull(k.isolatedModel.mesh,k.id);if(k!=Lib.defaultKit){Assert.IsNotNull(k.bridgeModel.mesh,k.id);Assert.IsNotNull(k.wallRubble,k.id);}
                var door=Object.Instantiate(k.entryDoor);try{Assert.IsEmpty(door.GetComponentsInChildren<Collider>(true));Assert.LessOrEqual(door.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y),.2901f,k.id);}finally{Object.DestroyImmediate(door);}
            }
        }
        [Test]
        public void AllSixteenChannelMasks_UseOneShadowlessRenderer_AndNoProceduralMesh()
        {
            var root=Object.Instantiate(Lib.cable);try
            {
                var skin=root.GetComponent<RaidTileSkin>();Assert.AreEqual(16,P(skin,"channelModels").arraySize);
                for(int mask=0;mask<16;mask++)
                {
                    skin.Apply(mask,false,0,Lib.kits[0]);var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();Assert.AreEqual(1,renderers.Length,mask.ToString());Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off,renderers[0].shadowCastingMode);
                    Assert.IsTrue(AssetDatabase.Contains(renderers[0].GetComponent<MeshFilter>().sharedMesh));
                }
            }finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void NamedPawns_SeparateLetterAndFallHinges_AndKeepLegacySpecialists()
        {
            foreach(var pawn in new[]{Lib.technician,Lib.emp,Lib.analyst})
            {
                Assert.AreEqual("Letter",R<Transform>(pawn,"letterPivot").name);Assert.AreEqual("Tilt",R<Transform>(pawn,"fallPivot").name);Assert.AreEqual(-70,P(pawn,"downEuler").vector3Value.x);Assert.AreEqual(.2f,P(pawn,"turnSeconds").floatValue);
                foreach(string field in new[]{"damageMark","spentMark","plateMark"})Assert.IsNotNull(R<GameObject>(pawn,field));
            }
            Assert.IsNotNull(Lib.miner);Assert.IsNotNull(Lib.smokeSpecialist);
            Assert.IsNotNull(R<Transform>(Lib.weldArm,"aimingArm"));Assert.IsNotNull(R<Transform>(Lib.weldArm,"aimingBase"));
        }
        [Test]
        public void ScreenLabel_CanReturnFromNamedHealthToFlagAndStatusWithoutTextOverflow()
        {
            var canvas=new GameObject("Label canvas",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var camera=new GameObject("Label camera").AddComponent<Camera>();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidScreenLabel.prefab");var root=Object.Instantiate(prefab,canvas.transform);var label=root.GetComponent<RaidScreenLabel>();var text=R<TMPro.TMP_Text>(label,"caption");label.Configure(camera,canvas,text.font,14);
            try
            {
                label.ShowHealth(2,3,Color.black,false,false,personName:"Nermin");label.ShowFlag("12",Color.black,Color.black);text.ForceMeshUpdate();Assert.IsFalse(text.isTextOverflowing);
                label.ShowText("TURN DONE",Color.black,Color.black,14);text.ForceMeshUpdate();Assert.IsFalse(text.isTextOverflowing);Assert.GreaterOrEqual(text.rectTransform.rect.width,text.GetPreferredValues(text.text).x);
            }finally{Object.DestroyImmediate(canvas.gameObject);Object.DestroyImmediate(camera.gameObject);}
        }
        [Test]
        public void Hud_HasBoundCartouche_RealStats_AndPermanentLossWarning()
        {
            var hud=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidHud.prefab");var view=hud.GetComponent<RaidHudView>();Assert.IsNotNull(R<RaidBlueprintView>(view,"blueprint"));Assert.IsNotNull(R<TMPro.TMP_Text>(view,"crateContents"));
            foreach(var card in hud.GetComponentsInChildren<RaidTeamCardView>(true)){Assert.IsNotNull(R<TMPro.TMP_Text>(card,"stats"));Assert.GreaterOrEqual(R<TMPro.TMP_Text>(card,"equipment").fontSize,14);}
            var panels=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Desk/Prefabs/Sade/Raid/RaidPanels.prefab").GetComponent<RaidPanelsView>();Assert.IsNotNull(R<TMPro.TMP_Text>(panels,"fatalWarning"));
        }
    }
}
