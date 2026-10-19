using Corrigible.Fx.Orb;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Corrigible.Fx.Tests
{
    public sealed class OrbIntegrationTests
    {
        [Test] public void UnknownAppearance_HidesEvenCoreAndQuarantine()
        {
            foreach(bool core in new[]{false,true})foreach(bool quarantine in new[]{false,true})
            {
                var a=OrbStateMapper.Map(99,FxTrust.Unknown,core,quarantine);
                Assert.That(a.Kind,Is.EqualTo(OrbKind.Unknown));Assert.That(a.Level,Is.Zero);Assert.That(a.Radius,Is.EqualTo(27));
                Assert.That(OrbStateMapper.Map(null,FxTrust.Verified,core,quarantine).Kind,Is.EqualTo(OrbKind.Unknown));
            }
        }
        [Test] public void ShownAppearance_ClampsAndPrioritizesCore()
        {
            var a=OrbStateMapper.Map(120,FxTrust.Verified,true,true);Assert.That(a.Kind,Is.EqualTo(OrbKind.Core));Assert.That(a.Level,Is.EqualTo(100));Assert.That(a.Radius,Is.EqualTo(51));
            a=OrbStateMapper.Map(-8,FxTrust.Verified,false,true);Assert.That(a.Kind,Is.EqualTo(OrbKind.Quarantine));Assert.That(a.Level,Is.Zero);Assert.That(a.Radius,Is.EqualTo(30));
        }
        [Test] public void ReducedIntro_DoesNotRestartFirstFrameOrLevelSmoothing()
        {
            var s=new OrbSimulation(48,3,OrbKind.Normal,0);s.CompleteIntro();s.Advance(.025);double first=s.FirstTime;s.Level=100;s.Advance(.025);
            Assert.That(s.DisplayedLevel,Is.EqualTo(7.5).Within(1e-10));Assert.That(s.FirstTime,Is.EqualTo(first));Assert.That(s.Time-s.FirstTime,Is.GreaterThanOrEqualTo(1.1));
        }
        [Test] public void Budget_StopsAtCountOrCpuBoundary()
        {
            var go=new GameObject();try{var b=go.AddComponent<OrbBudget>();Assert.That(b.Allows(100,100),Is.True);b.maxOrbsPerFrame=3;Assert.That(b.Allows(2,0),Is.True);Assert.That(b.Allows(3,0),Is.False);b.maxCpuMilliseconds=2;Assert.That(b.Allows(0,2),Is.False);}finally{Object.DestroyImmediate(go);}
        }
        [Test] public void OrbPrefab_HasSerializedRendererAndMaskCompatibleMaterial()
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Corrigible/Fx/Prefabs/Orb/OrbView.prefab");var v=p.GetComponent<OrbView>();Assert.That(v.image,Is.Not.Null);Assert.That(v.orbRenderer,Is.Not.Null);Assert.That(v.image.raycastTarget,Is.False);Assert.That(v.orbRenderer.settings.composite.HasProperty("_Stencil"),Is.True);Assert.That(v.orbRenderer.settings.maxSize,Is.EqualTo(512));Assert.That(v.orbRenderer.settings.reducedFps,Is.EqualTo(8));
        }
    }
}
