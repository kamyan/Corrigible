using System;
using System.IO;
using System.Collections;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Corrigible.Fx.Orb;
using Corrigible.Fx.EditorTools;
namespace Corrigible.Fx.Tests
{
    public sealed class OrbParityTests
    {
        static JObject oracle;
        static JObject Oracle=>oracle??(oracle=JObject.Parse(File.ReadAllText("Assets/Corrigible/Fx/Data/OrbParity.json")));
        static void Equal(double value,JToken expected,string label="")=>Assert.That(value,Is.EqualTo((double)expected).Within(1e-10),label);
        static void Vector(OrbVector value,JToken expected){Equal(value.X,expected[0]);Equal(value.Y,expected[1]);Equal(value.Z,expected[2]);}
        [Test] public void Oracle_MatchesCanonicalSourceHash(){using(var sha=SHA256.Create()){var hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes("Docs/UI_Fx/SonSalter_Fx_Paketi/art/orb/fx_orb.js"))).Replace("-","").ToLowerInvariant();Assert.That(hash,Is.EqualTo((string)Oracle["sourceHash"]));}}
        [TestCase(3)] [TestCase(5)] [TestCase(7)] [TestCase(9)]
        public void Rng_FirstTenMatchJavaScript(int seed){var rng=new OrbRng((uint)(seed*9973+17));foreach(var expected in Oracle["rng"][seed.ToString()])Assert.That(rng.Next(),Is.EqualTo((double)expected));}
        [Test] public void Icosphere_AllVerticesFacesAndEdgeOrderMatchJavaScript()
        {
            var ico=Oracle["ico"];Assert.That(OrbIcosphere.Vertices.Length,Is.EqualTo(42));Assert.That(OrbIcosphere.Faces.Length,Is.EqualTo(80));Assert.That(OrbIcosphere.Edges.Length,Is.EqualTo(120));
            for(int i=0;i<42;i++)Vector(OrbIcosphere.Vertices[i],ico["v"][i]);for(int i=0;i<80;i++)for(int j=0;j<3;j++)Assert.That(OrbIcosphere.Faces[i][j],Is.EqualTo((int)ico["f"][i][j]));for(int i=0;i<120;i++)for(int j=0;j<2;j++)Assert.That(OrbIcosphere.Edges[i][j],Is.EqualTo((int)ico["e"][i][j]));
        }
        [Test] public void Build_AllRandomFieldsAndNextRngMatchJavaScript()
        {
            var sim=new OrbSimulation(48,3,OrbKind.Normal,60,211);var data=Oracle["build"];Assert.That(sim.Shell.Length,Is.EqualTo(((JArray)data["shell"]).Count));Assert.That(sim.Bands.Length,Is.EqualTo(((JArray)data["bands"]).Count));
            void Particles(OrbSimulation.Particle[] values,JToken json,bool shell){for(int i=0;i<values.Length;i++){var p=values[i];var e=json[i];Equal(p.P.X,e["x"]);Equal(p.P.Y,e["y"]);Equal(p.P.Z,e["z"]);Equal(p.Tau,e["tau"]);Equal(p.Phase,e["ph"]);if(shell){Equal(p.Size,e["s"]);Assert.That(p.Ice,Is.EqualTo((bool)e["ice"]));}}}
            Particles(sim.Shell,data["shell"],true);Particles(sim.Bands,data["bands"],false);
            for(int i=0;i<34;i++){var b=sim.Blocks[i];var e=data["blocks"][i];Equal(b.Lat,e["lat"]);Equal(b.Lon,e["lon"]);Equal(b.Width,e["w"]);Equal(b.Height,e["h"]);Equal(b.Tau,e["tau"]);Equal(b.Phase,e["ph"]);Assert.That(b.Dash,Is.EqualTo((bool)e["dash"]));}
            for(int i=0;i<42;i++)Equal(sim.Jag[i],data["jag"][i]);for(int i=0;i<80;i++)Equal(sim.FaceThreshold[i],data["faces"][i]);for(int i=0;i<120;i++)Equal(sim.EdgeThreshold[i],data["edges"][i]);
            for(int i=0;i<10;i++){var a=sim.Shards[i];var e=data["shards"][i];Equal(a.Angle,e["a"]);Equal(a.Radius,e["rr"]);Equal(a.Speed,e["sp"]);Equal(a.Tilt,e["tilt"]);Equal(a.Size,e["sz"]);Equal(a.Rotation,e["rot"]);Equal(a.Tau,e["tau"]);}
            Equal(sim.Rng.Next(),data["next"]);
        }
        public static IEnumerable Cases(){foreach(JObject c in Oracle["cases"])yield return new TestCaseData((string)c["kind"],(int)c["level"],(int)c["steps"]).SetName("OrbSnapshot_"+(string)c["kind"]+"_"+(int)c["level"]+"_"+(int)c["steps"]);}
        [TestCaseSource(nameof(Cases))] public void Frames_AllFilamentsArcsSparksAndNextRngMatchJavaScript(string kind,int level,int steps)
        {
            JObject expected=null;foreach(JObject c in Oracle["cases"])if((string)c["kind"]==kind&&(int)c["level"]==level&&(int)c["steps"]==steps){expected=c;break;}
            var sim=new OrbSimulation(48,(int)expected["seed"],OrbCapture.Kind(kind),level,211);sim.Advance(.025,steps);
            Equal(sim.Time,expected["time"]);Equal(sim.Angle,expected["angle"]);Equal(sim.DisplayedLevel,expected["displayed"]);Equal(sim.NextFilament,expected["nextFil"]);Equal(sim.SparkAccumulator,expected["acc"]);
            Assert.That(sim.FilamentCount,Is.EqualTo(((JArray)expected["fil"]).Count));for(int i=0;i<sim.FilamentCount;i++)for(int j=0;j<10;j++)Vector(sim.Filaments[i*10+j],expected["fil"][i][j]);
            Assert.That(sim.ArcCount,Is.EqualTo(((JArray)expected["arcs"]).Count));for(int i=0;i<sim.ArcCount;i++)for(int j=0;j<17;j++)Vector(sim.Arcs[i*17+j],expected["arcs"][i][j]);
            Assert.That(sim.SparkCount,Is.EqualTo(((JArray)expected["sparks"]).Count));for(int i=0;i<sim.SparkCount;i++){var p=sim.Sparks[i];var e=expected["sparks"][i];Vector(p.P,e["p"]);Vector(p.V,e["v"]);Equal(p.Life,e["life"]);Equal(p.Max,e["max"]);Equal(p.Color.R,e["col"][0]);Equal(p.Color.G,e["col"][1]);Equal(p.Color.B,e["col"][2]);}
            Equal(sim.Rng.Next(),expected["next"]);Assert.That(sim.DrawList.Count,Is.GreaterThan(0).And.LessThan(OrbDrawList.Capacity));
        }
        [Test] public void SweepAndWeights_MatchSourceTables(){foreach(var e in Oracle["sweep"])Equal(OrbMath.Sweep((double)e[0]),e[1]);foreach(var e in Oracle["weights"]){var w=OrbMath.Weights((double)e[0]);Equal(w.A,e[1]);Equal(w.B,e[2]);Equal(w.C,e[3]);Equal(w.Blue,e[4]);}}
        [Test] public void LevelChange_EasesAndUnknownHidesValue(){var s=new OrbSimulation(48,3,OrbKind.Normal,0);s.Advance(.025,40);s.Level=100;s.Advance(.025);Assert.That(s.DisplayedLevel,Is.EqualTo(7.5).Within(1e-12));s.Kind=OrbKind.Unknown;s.Advance(.025);Assert.That(s.DisplayedLevel,Is.EqualTo(6.9375).Within(1e-12));}
        [Test] public void Simulation_WarmFramesAllocateZeroManagedBytes(){var s=new OrbSimulation(48,3,OrbKind.Normal,60);s.Advance(.025,40);long before=GC.GetAllocatedBytesForCurrentThread();s.Advance(.025,100);long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(bytes,Is.Zero);}
    }
}
