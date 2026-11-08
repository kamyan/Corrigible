using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game.Raid;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Corrigible.Desk.Tests
{
    /// <summary>R11: gerçek eklemeli sahne; isimli kadro, komut/geri al, menteşeler ve kalıcı kayıp onayı.</summary>
    public sealed class RaidModelPlayTests
    {
        LiveRaidDataSource source;
        static T Field<T>(Object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return BoardPlayKit.LoadDesk();var next=BoardPlayKit.Sim.NextLiberationSector(BoardPlayKit.Sim.S.Liberation.CurrentRegion);
            var started=BoardPlayKit.Game.StartLiberationRaid(next.Id);Assert.IsTrue(started.Ok,started.Msg);
            yield return BoardPlayKit.WaitFor(()=>Object.FindFirstObjectByType<LiveRaidDataSource>()?.State!=null,8,"baskın sahnesi yüklenmedi");source=Object.FindFirstObjectByType<LiveRaidDataSource>();
            yield return BoardPlayKit.WaitSeconds(.6f);
        }
        [UnityTearDown] public IEnumerator Teardown(){if(RaidLauncher.IsRaidLoaded){var unload=UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Baskin");while(unload!=null&&!unload.isDone)yield return null;}yield return BoardPlayKit.TearDown();}
        static void Visible(RectTransform rect)
        {
            var points=new Vector3[4];rect.GetWorldCorners(points);foreach(var p in points){Assert.That(p.x,Is.InRange(-1f,Screen.width+1f),rect.name);Assert.That(p.y,Is.InRange(-1f,Screen.height+1f),rect.name);}
        }
        [UnityTest]
        public IEnumerator NamedCards_StayOnScreenInBothLanguages_AndMoveUndoUsesTheSameState()
        {
            var frozen=BoardPlayKit.Snapshot();var squad=BoardPlayKit.Sim.PlanningSquad();
            foreach(var lang in new[]{DeskLang.Tr,DeskLang.En})
            {
                DeskLoc.Instance.SetLanguage(lang);source.Select('T');yield return BoardPlayKit.WaitSeconds(.4f);
                Assert.AreEqual(3,source.Current.Team.Length);foreach(var card in Object.FindObjectsByType<RaidTeamCardView>(FindObjectsSortMode.None))
                {Visible((RectTransform)card.transform);Assert.IsFalse(string.IsNullOrEmpty(Field<TMP_Text>(card,"stats").text));}
                foreach(var person in squad)StringAssert.Contains(person.PersonName,source.Current.Team.Single(t=>t.Letter==Baskin.UnitTypes[person.Type].Short[0]).Name);
                StringAssert.Contains(lang==DeskLang.Tr?"KASADA":"CRATE:",source.Current.CrateContents);
            }
            var u=source.State.Units.First(x=>x.Side=='p'&&x.Type=="tek");int x=u.X,y=u.Y;var dest=source.Current.Moves.First(p=>!source.State.Exits.Any(e=>e.X==p.x&&e.Y==p.y));source.Move(dest);
            yield return BoardPlayKit.WaitFor(()=>!source.Busy,3,"yürüyüş bitmedi");Assert.AreEqual(dest.x,u.X);Assert.AreEqual(dest.y,u.Y);source.Undo();Assert.AreEqual(x,source.State.Units.Single(p=>p.Id==u.Id).X);Assert.AreEqual(y,source.State.Units.Single(p=>p.Id==u.Id).Y);Assert.AreEqual(frozen,BoardPlayKit.Snapshot(),"tahta hamlesi kampanya kaydını değiştirmez");
        }
        [UnityTest]
        public IEnumerator PawnDoneDamageArmorAndFall_UseIndependentModelParts()
        {
            var lib=Field<RaidPieceLibrary>(Object.FindFirstObjectByType<RaidBoard3D>(),"lib");var piece=Object.Instantiate(lib.technician);try
            {
                var letter=Field<Transform>(piece,"letterPivot");var tilt=Field<Transform>(piece,"fallPivot");var rest=letter.localRotation;var tiltRest=tilt.localRotation;
                piece.SetHp(1,2);piece.SetPlate(true);Assert.IsTrue(Field<GameObject>(piece,"damageMark").activeSelf);Assert.IsTrue(Field<GameObject>(piece,"plateMark").activeSelf);
                piece.SetSpent(true);yield return BoardPlayKit.WaitSeconds(.24f);Assert.AreEqual(180,Quaternion.Angle(rest,letter.localRotation),.1f);Assert.AreEqual(0,Quaternion.Angle(tiltRest,tilt.localRotation),.1f);Assert.IsTrue(Field<GameObject>(piece,"spentMark").activeSelf);
                piece.SetDown(true);Assert.AreEqual(70,Quaternion.Angle(tiltRest,tilt.localRotation),.1f);Assert.IsFalse(Field<GameObject>(piece,"spentMark").activeSelf);
                piece.SetDown(false);piece.SetSpent(false);yield return BoardPlayKit.WaitSeconds(.24f);Assert.AreEqual(0,Quaternion.Angle(rest,letter.localRotation),.1f);
            }finally{Object.Destroy(piece.gameObject);}
        }
        [UnityTest]
        public IEnumerator LastEscapeTurn_AlwaysRequiresNamedPermanentLossConfirmation()
        {
            var b=source.State.Clone();b.Escape=true;b.Turn=b.MaxTurns;foreach(var u in b.Units.Where(u=>u.Side=='p')){u.Moved=u.Acted=true;}
            source.OpenForValidation(b);var frozen=BoardPlayKit.Snapshot();source.EndTurn();yield return BoardPlayKit.Frames(2);
            Assert.IsTrue(source.Current.EndConfirm);Assert.AreEqual("player",source.State.Phase);foreach(var u in b.Units.Where(u=>u.Side=='p'&&Baskin.OnBoard(u)))StringAssert.Contains(u.PersonName,source.Current.FatalWarning);
            Assert.IsTrue(Object.FindFirstObjectByType<RaidPanelsView>().GetComponentsInChildren<TMP_Text>().Any(t=>t.text==source.Current.FatalWarning));source.Cancel();Assert.IsFalse(source.Current.EndConfirm);Assert.AreEqual(frozen,BoardPlayKit.Snapshot());
        }
        [UnityTest]
        public IEnumerator RetreatResult_ReturnsToDesk_AndReplayPreservesNamedSurvivors()
        {
            var ids=BoardPlayKit.Sim.PlanningSquad().Select(p=>p.PersonId).ToArray();source.Retreat();Assert.IsTrue(source.Current.RetreatArmed);source.Retreat();yield return BoardPlayKit.WaitSeconds(.7f);Assert.IsTrue(source.Current.Finished);source.Continue();
            yield return BoardPlayKit.WaitFor(()=>!RaidLauncher.IsRaidLoaded,8,"baskın kapanmadı");Assert.IsNull(BoardPlayKit.Sim.S.Raid);foreach(string id in ids)Assert.IsTrue(BoardPlayKit.Sim.Board.Person(id).Alive);
            BoardPlayKit.Game.Save();var restored=Replay.Restore(BoardPlayKit.Game.SavedRecord());Assert.IsNull(restored.S.Raid);foreach(string id in ids)Assert.IsTrue(restored.Board.Person(id).Alive);
        }
    }
}
