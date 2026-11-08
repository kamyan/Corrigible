using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace Corrigible.Desk.Tests
{
    /// <summary>R1–R4 gerçek sahne: kayıtlar test klasöründe; ses kontrolleri bellek içi ayarlarla sınanır.</summary>
    public sealed class EntrancePlayTests
    {
        const string Menu="Assets/Corrigible/Desk/Scenes/Giris.unity";
        string storage,previousStorage;DeskLang previousLang;EntranceController ctrl;TimeClock clock;SettingsCabinet cabinet;
        static T F<T>(Object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        static void Click(DeskTarget t)=>t.OnPointerClick(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
        static IEnumerator Wait(Func<bool> done,float seconds=12){float until=Time.realtimeSinceStartup+seconds;while(!done()&&Time.realtimeSinceStartup<until)yield return null;Assert.IsTrue(done(),"beklenen durum oluşmadı");}
        [UnitySetUp] public IEnumerator SetUp()
        {
            previousStorage=UnityEditor.SessionState.GetString(BoardPlayKit.StorageKey,"");storage=Path.Combine(Path.GetTempPath(),"Corrigible-Entrance-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(storage);UnityEditor.SessionState.SetString(BoardPlayKit.StorageKey,storage);previousLang=DeskLoc.Instance.Lang;DeskLoc.Instance.SetLanguage(DeskLang.Tr);yield return Load();
        }
        IEnumerator Load(){var a=SceneManager.LoadSceneAsync(Menu,LoadSceneMode.Single);while(!a.isDone)yield return null;yield return null;ctrl=Object.FindFirstObjectByType<EntranceController>();clock=F<TimeClock>(ctrl,"clock");cabinet=F<SettingsCabinet>(ctrl,"cabinet");yield return Wait(()=>ctrl.Ready);yield return null;}
        [UnityTearDown] public IEnumerator TearDown()
        {
            var empty=SceneManager.CreateScene("Entrance-empty-"+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(empty);
            foreach(string p in new[]{Menu,BoardPlayKit.DeskScenePath}){var s=SceneManager.GetSceneByPath(p);if(s.IsValid()&&s.isLoaded){var a=SceneManager.UnloadSceneAsync(s);while(a!=null&&!a.isDone)yield return null;}}
            yield return null;GameLaunch.Pending=GameLaunch.Mode.None;Cursor.visible=true;DeskLoc.Instance.SetLanguage(previousLang);UnityEditor.SessionState.SetString(BoardPlayKit.StorageKey,previousStorage);if(Directory.Exists(storage))Directory.Delete(storage,true);
        }
        [UnityTest] public IEnumerator Card_Clamps80Holes_AndKeepsTheTrueShift_InBothLanguages()
        {
            foreach(var lang in new[]{DeskLang.Tr,DeskLang.En})
            {
                DeskLoc.Instance.SetLanguage(lang);clock.ShowCard(93,3,12);yield return null;Assert.AreEqual(80,F<GameObject[]>(clock,"punches").Count(x=>x.activeSelf));Assert.AreEqual(Loc.F("V.{0:000}",94),F<TMP_Text>(clock,"cardSummary").text);Assert.AreEqual(Loc.F("özgür {0}/{1}",3,12),F<TMP_Text>(clock,"cardFree").text);
                clock.ShowCard(13,3,12);Assert.AreEqual(13,F<GameObject[]>(clock,"punches").Count(x=>x.activeSelf));Assert.IsFalse(F<TMP_Text>(clock,"cardFree").isTextOverflowing);
            }
        }
        [UnityTest] public IEnumerator NewCampaign_RequiresTwoClicksWithSave_AndExpiresWithoutDeletingIt()
        {
            clock.ShowCard(13,3,12);int chosen=0;clock.NewChosen+=()=>chosen++;
            Click(F<DeskTarget>(clock,"stackTarget"));Assert.IsTrue(clock.Armed);Assert.AreEqual(0,chosen);yield return new WaitForSecondsRealtime(.25f);Assert.Less(F<PartDriver>(clock,"clipPart").Value,-69);Assert.IsTrue(F<GameObject>(clock,"voidTag").activeSelf);
            yield return new WaitForSecondsRealtime(4.1f);Assert.IsFalse(clock.Armed);Assert.IsFalse(ctrl.Entering);Assert.AreEqual(0,chosen);
        }
        [UnityTest] public IEnumerator Mute_DropsNeedlesAndLamps_ButPreservesSlidersAndDigits()
        {
            var mock=new MemorySettings();cabinet.Init(mock);cabinet.RequestOpen();yield return new WaitForSecondsRealtime(1.5f);var f=F<CabinetFader[]>(cabinet,"channels")[0];f.Nudge(-3);yield return new WaitForSecondsRealtime(.15f);var before=F<CounterWheel[]>(f,"digits").Select(w=>w.Digit).ToArray();var handle=F<PartDriver>(f,"handlePart").Value;
            F<CabinetToggle>(cabinet,"sound").Flip();yield return new WaitForSecondsRealtime(.8f);Assert.IsTrue(mock.Muted);Assert.AreEqual(0,F<PartDriver>(f,"lampPart").SwapState);Assert.AreEqual(50,F<PartDriver>(f,"needlePart").Value,1);Assert.AreEqual(handle,F<PartDriver>(f,"handlePart").Value,.001f);CollectionAssert.AreEqual(before,F<CounterWheel[]>(f,"digits").Select(w=>w.Digit));
            F<CabinetToggle>(cabinet,"sound").Flip();yield return new WaitForSecondsRealtime(.8f);Assert.AreEqual(1,F<PartDriver>(f,"lampPart").SwapState);Assert.AreEqual(-5,F<PartDriver>(f,"needlePart").Value,1);
        }
        [UnityTest] public IEnumerator Fader_RaycastAndPointerProjection_UpdatesTheChosenChannel()
        {
            var mock=new MemorySettings();cabinet.Init(mock);cabinet.RequestOpen();yield return new WaitForSecondsRealtime(1.5f);var view=F<EntranceCamera>(ctrl,"cam").View;var f=F<CabinetFader[]>(cabinet,"channels")[1];Physics.SyncTransforms();var point=view.WorldToScreenPoint(f.transform.TransformPoint(new Vector3(0,-.04f,-.05f)));var e=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var ray=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(e,ray);var hit=ray.FirstOrDefault(x=>x.gameObject&&x.gameObject.GetComponentInParent<CabinetFader>()==f);Assert.IsNotNull(hit.gameObject,"sürgü gerçek ışınla erişilebilir");e.pointerPressRaycast=hit;f.OnPointerDown(e);f.OnPointerUp(e);Assert.AreEqual(.25f,f.Value,.051f);Assert.AreEqual(f.Value,mock.Values[1]);Assert.Greater(mock.Saves,0);
        }
        [UnityTest] public IEnumerator ClockFace_ContinuesTheSavedCampaign_AfterStampDoorAndBlackHold()
        {
            var sim=Simulation.NewGame(79,new Balance());Assert.IsTrue(sim.PrintShift().Ok);SaveStore.Write(sim.Record);yield return Load();yield return Wait(()=>clock.HasCard);var door=F<DoorProp>(ctrl,"door");float stamp=-1,load=-1,black=-1,start=Time.realtimeSinceStartup;clock.Stamped+=()=>stamp=Time.realtimeSinceStartup;
            Click(F<DeskTarget>(clock,"clockTarget"));Assert.IsTrue(ctrl.Entering);Assert.IsFalse(Cursor.visible);Assert.IsTrue(DeskTarget.Blocked());
            while(ctrl&&Time.realtimeSinceStartup-start<15)
            {
                if(F<AsyncOperation>(ctrl,"loading")!=null&&load<0){load=Time.realtimeSinceStartup;Assert.AreNotEqual(0,door.WheelDegrees);}
                if(F<Image>(ctrl,"fade").color.a>=.999f&&black<0)black=Time.realtimeSinceStartup;
                yield return null;
            }
            Assert.IsFalse(ctrl,"menü sahnesi boşaltılmalı");Assert.Greater(load,stamp);Assert.Greater(black,load);Assert.GreaterOrEqual(Time.realtimeSinceStartup-black,.18f);Assert.AreEqual(GameLaunch.Mode.None,GameLaunch.Pending,"niyet yalnız bir kez tüketilir");var game=Object.FindFirstObjectByType<GameController>();yield return Wait(()=>game.Sim!=null);Assert.AreEqual((uint)79,game.Sim.Record.seed);
        }
        [UnityTest] public IEnumerator BlankDeck_WithoutSave_StartsNewCampaign_WithFirstShiftCard()
        {
            Assert.IsFalse(clock.HasCard);Click(F<DeskTarget>(clock,"stackTarget"));Assert.IsTrue(ctrl.Entering);yield return Wait(()=>clock.Punching);Assert.AreEqual(Loc.F("V.{0:000}",1),F<TMP_Text>(clock,"newCardShift").text);Assert.IsFalse(F<GameObject>(clock,"freshPunch").activeSelf);yield return Wait(()=>!ctrl,15);Assert.IsNotNull(Object.FindFirstObjectByType<GameController>());Assert.AreEqual(GameLaunch.Mode.None,GameLaunch.Pending);
        }
        sealed class MemorySettings:IDeskSettings
        {
            public readonly float[] Values={.7f,.8f,.9f,1};public int Saves;public float Get(int c)=>Values[c];public void Set(int c,float v)=>Values[c]=v;public bool Muted{get;set;}public DeskLang Language=>DeskLoc.Instance.Lang;public void SetLanguage(DeskLang lang)=>DeskLoc.Instance.SetLanguage(lang);public void Preview(int c){}public void Save()=>Saves++;
        }
    }
}
