using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace Corrigible.Desk.Tests
{
    public sealed class PlanningTutorialPlayTests
    {
        static T F<T>(Object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        SiginakTutorial Tutor=>Object.FindFirstObjectByType<SiginakTutorial>();
        TutorialNoteView Note=>Tutor.GetComponentInChildren<TutorialNoteView>(true);
        [UnitySetUp] public IEnumerator Setup(){yield return BoardPlayKit.LoadDesk(tutorial:true);yield return BoardPlayKit.Frames(3);}
        [UnityTearDown] public IEnumerator Teardown(){yield return BoardPlayKit.TearDown();}
        [UnityTest] public IEnumerator Guide_SeesARealCardDrop_AndDoesNotWriteItsOwnCommands()
        {
            Assert.IsTrue(Tutor.Active);Assert.AreEqual("intro",Tutor.CurrentStepId);var before=BoardPlayKit.Snapshot();
            F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("board",Tutor.CurrentStepId);Assert.AreEqual(before,BoardPlayKit.Snapshot());
            BoardPlayKit.Presenter.Show();yield return BoardPlayKit.WaitSeconds(.8f);Assert.AreEqual("person",Tutor.CurrentStepId);
            var board=BoardPlayKit.Board;var card=board.Deck.Top;Assert.IsNotNull(card);
            yield return BoardPlayKit.Drag(card.gameObject,BoardPlayKit.BoardWorld(BoardLayout.CardCentre("cook")),12);
            yield return BoardPlayKit.WaitSeconds(.5f);
            Assert.AreEqual("gear",Tutor.CurrentStepId);Assert.AreEqual("board_assign",BoardPlayKit.Sim.Record.commands.Last().kind);Assert.AreEqual("cook",BoardPlayKit.Sim.Record.commands.Last().b);
            before=BoardPlayKit.Snapshot();F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("slip",Tutor.CurrentStepId);Assert.AreEqual(before,BoardPlayKit.Snapshot());
            // Panodan dönüş sürerken baskın açılırsa oda kamerası kilitlenir; rehber onu beklememeli.
            BoardPlayKit.Presenter.Close();
            var next=BoardPlayKit.Sim.NextLiberationSector(BoardPlayKit.Sim.S.Liberation.CurrentRegion);
            Assert.IsTrue(BoardPlayKit.Game.StartLiberationRaid(next.Id).Ok);
            yield return BoardPlayKit.WaitFor(()=>Object.FindFirstObjectByType<LiveRaidDataSource>()?.State!=null,8,"baskın sahnesi yüklenmedi");
            yield return BoardPlayKit.WaitSeconds(.4f);Assert.AreEqual("raidSelect",Tutor.CurrentStepId);
            Assert.Greater(F<CanvasGroup>(Note,"group").alpha,.99f,"baskında rehber görünmeli");
            var unload=UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Baskin");while(unload!=null&&!unload.isDone)yield return null;
        }
        [UnityTest] public IEnumerator ResumeAndSkip_PreserveTheCampaignAndRememberTheTutorialChoice()
        {
            F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("board",Tutor.CurrentStepId);BoardPlayKit.Game.Save();
            yield return BoardPlayKit.ContinueDesk();yield return BoardPlayKit.Frames(3);Assert.IsTrue(Tutor.Active);Assert.AreEqual("board",Tutor.CurrentStepId);
            var before=BoardPlayKit.Snapshot();F<Button>(Note,"skip").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.IsFalse(Tutor.Active);Assert.IsTrue(SiginakTutorial.PlanningDone);Assert.AreEqual(before,BoardPlayKit.Snapshot());
            BoardPlayKit.Game.NewGame(81);yield return BoardPlayKit.Frames(2);Assert.IsFalse(Tutor.Active);
        }
        [UnityTest] public IEnumerator OldSave_DoesNotForceAGuide_AndNewOpeningDoesNotOverwriteProgressBeforeStart()
        {
            BoardPlayKit.Game.Save();PlayerPrefs.DeleteKey(SiginakTutorial.PlanningSeedKey);PlayerPrefs.DeleteKey(SiginakTutorial.PlanningStepKey);
            yield return BoardPlayKit.ContinueDesk();yield return BoardPlayKit.Frames(3);Assert.IsFalse(Tutor.Active);
            PlayerPrefs.SetString(SiginakTutorial.PlanningSeedKey,"old-progress");PlayerPrefs.SetInt(SiginakTutorial.PlanningStepKey,4);
            BoardPlayKit.Game.NewGame(82);yield return BoardPlayKit.Frames(3);Assert.IsTrue(BoardPlayKit.Game.StoryHold);
            Assert.AreEqual("old-progress",PlayerPrefs.GetString(SiginakTutorial.PlanningSeedKey),"açılışta devam seçeneği henüz kararlaştırılmadı");
        }
        [UnityTest] public IEnumerator Guide_IsReadableInBothLanguages_AndEscapeKeepsTheBoardBackAction()
        {
            foreach(var lang in new[]{DeskLang.Tr,DeskLang.En})
            {
                DeskLoc.Instance.SetLanguage(lang);yield return BoardPlayKit.WaitSeconds(.3f);var text=F<TMP_Text>(Note,"body");text.ForceMeshUpdate();Assert.IsFalse(text.isTextOverflowing);Assert.GreaterOrEqual(text.fontSize,14);
                var corners=new Vector3[4];F<RectTransform>(Note,"card").GetWorldCorners(corners);foreach(var p in corners){Assert.That(p.x,Is.InRange(-1f,Screen.width+1f));Assert.That(p.y,Is.InRange(-1f,Screen.height+1f));}
            }
            BoardPlayKit.Presenter.Show();yield return BoardPlayKit.WaitSeconds(.8f);Assert.IsFalse(Tutor.HandleEscape());Assert.IsTrue(BoardPlayKit.Presenter.HandleEscape());Assert.IsTrue(Tutor.Active);
        }
    }
}
