using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Kapının yanındaki çift taraflı belge çekmecesi (<see cref="PassDrawer"/>; Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A) tek
    /// başına: açma ve kapama hedefi, animasyonsuz kurulum, lambanın üç durumu, şalterin çekilip kendiliğinden dönmesi, tıkların ve
    /// seslerin olayları. Parçalar boş nesnelerdeki <see cref="PartDriver"/>'dır (hareketi parçanın Update'i sürer; burada hedef ve
    /// anlık konum sınanır). Örnekler test sonunda silinir, açık sahneye dokunulmaz.
    /// </summary>
    public sealed class PassDrawerTests
    {
        readonly List<Object> spawned = new List<Object>();
        PassDrawer drawer;
        PartDriver slide, lamp, lever;
        Material off, amber, green;

        [TearDown]
        public void TearDown()
        {
            for (int i = spawned.Count - 1; i >= 0; i--) if (spawned[i] != null) Object.DestroyImmediate(spawned[i]);
            spawned.Clear();
        }

        T Keep<T>(T o) where T : Object { spawned.Add(o); return o; }

        void Build()
        {
            var root = Keep(new GameObject("Belge çekmecesi"));
            var tabla = new GameObject("Tabla"); tabla.transform.SetParent(root.transform, false);
            slide = tabla.AddComponent<PartDriver>();
            slide.Configure("Drawer", PartKind.Slide, PartAxis.Z, 0f, -0.28f, 0f, Vector3.zero);
            var lampGo = GameObject.CreatePrimitive(PrimitiveType.Cube); lampGo.transform.SetParent(root.transform, false);
            off = Keep(new Material(Shader.Find("Hidden/InternalErrorShader")) { name = "off" });
            amber = Keep(new Material(off) { name = "amber" });
            green = Keep(new Material(off) { name = "green" });
            lamp = lampGo.AddComponent<PartDriver>();
            lamp.Configure("Lamp", PartKind.Swap, PartAxis.Y, 0, 0, 0, Vector3.zero, lampGo.GetComponent<Renderer>(), 0, new[] { off, amber, green });
            var kol = new GameObject("Kol"); kol.transform.SetParent(root.transform, false);
            lever = kol.AddComponent<PartDriver>();
            lever.Configure("Lever", PartKind.Rotate, PartAxis.X, -20f, 60f, -20f, Vector3.zero);
            drawer = root.AddComponent<PassDrawer>();
            drawer.Configure(slide, lamp, lever, null, null);
        }

        [Test]
        public void OpenClose_TargetsTheEndsOfTheSlide()
        {
            Build();
            var sounds = new List<PassDrawerSound>();
            drawer.Sound += sounds.Add;
            Assert.IsFalse(drawer.IsOpen);
            Assert.IsTrue(drawer.FullyClosed, "dinlenmede kapalı");
            drawer.Open();
            Assert.IsTrue(drawer.IsOpen);
            Assert.IsTrue(slide.Moving, "tabla açılmaya başladı");
            Assert.IsFalse(drawer.FullyOpen, "hareket bitmeden tam açık değil");
            drawer.Open();
            CollectionAssert.AreEqual(new[] { PassDrawerSound.Open }, sounds, "açıkken yeniden açılmaz");
            drawer.SetOpenNow(true);
            Assert.AreEqual(-0.28f, slide.Value, 1e-5f, "tam açık: aralığın öbür ucu");
            Assert.IsTrue(drawer.FullyOpen);
            Assert.AreEqual(-0.28f, slide.transform.localPosition.z, 1e-5f, "tabla odaya doğru (yerel −Z) kaydı");
            drawer.Close();
            Assert.IsFalse(drawer.IsOpen);
            Assert.IsFalse(drawer.FullyClosed, "hareket bitmeden tam kapalı değil");
            drawer.SetOpenNow(false);
            Assert.IsTrue(drawer.FullyClosed);
            Assert.AreEqual(0f, slide.transform.localPosition.z, 1e-5f);
            CollectionAssert.AreEqual(new[] { PassDrawerSound.Open, PassDrawerSound.Close }, sounds);
        }

        [Test]
        public void Lamp_ThreeStates()
        {
            Build();
            var r = lamp.GetComponent<Renderer>();
            drawer.SetLamp(PassDrawer.Lamp.Amber, false);
            Assert.AreSame(amber, r.sharedMaterial);
            drawer.SetLamp(PassDrawer.Lamp.Green, false);
            Assert.AreSame(green, r.sharedMaterial);
            Assert.AreEqual(PassDrawer.Lamp.Green, drawer.LampState);
            drawer.SetLamp(PassDrawer.Lamp.Off, true);
            Assert.AreSame(off, r.sharedMaterial);
            Assert.IsFalse(drawer.Blinking, "sönük lamba yanıp sönmez");
            drawer.SetLamp(PassDrawer.Lamp.Green, true);
            Assert.IsTrue(drawer.Blinking);
            Assert.AreSame(green, r.sharedMaterial, "edit modda yanıp sönme kareyi söndürmez");
        }

        [Test]
        public void Switch_PullsAndReturns()
        {
            Build();
            var sounds = new List<PassDrawerSound>();
            drawer.Sound += sounds.Add;
            drawer.PullSwitch();
            Assert.IsTrue(drawer.Pulling);
            Assert.IsTrue(lever.Moving, "kol iniyor");
            drawer.PullSwitch();
            CollectionAssert.AreEqual(new[] { PassDrawerSound.Switch }, sounds, "çekilirken yeniden çekilmez");
            lever.Set(60f);
            drawer.Advance(5f);
            Assert.IsFalse(drawer.Pulling, "bekleme bitti");
            Assert.IsTrue(lever.Moving, "kol dinlenmeye dönüyor");
            drawer.Deny();
            CollectionAssert.AreEqual(new[] { PassDrawerSound.Switch, PassDrawerSound.Deny }, sounds);
            drawer.SetOpenNow(false);
            Assert.AreEqual(-20f, lever.Value, 1e-5f, "SetOpenNow kolu dinlenmeye alır");
        }

        [Test]
        public void Clicks_RaiseEvents()
        {
            Build();
            int d = 0, s = 0;
            drawer.DrawerClicked += () => d++;
            drawer.SwitchClicked += () => s++;
            drawer.ClickDrawer();
            drawer.ClickSwitch();
            drawer.ClickSwitch();
            Assert.AreEqual(1, d);
            Assert.AreEqual(2, s);
        }
    }
}
