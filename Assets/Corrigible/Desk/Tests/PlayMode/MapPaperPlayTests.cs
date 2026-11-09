using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Corrigible.Desk.Tests
{
    /// <summary>R6 gerçek ekran/kamera: düğmeler kadrajda, kişi satırı panoya gider, kapatmak komut yazmaz.</summary>
    public sealed class MapPaperPlayTests
    {
        static T Field<T>(Object o,string name) => (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        [UnitySetUp] public IEnumerator SetUp(){yield return BoardPlayKit.LoadDesk();}
        [UnityTearDown] public IEnumerator TearDown(){yield return BoardPlayKit.TearDown();}
        static void OnScreen(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            foreach(var p in corners){Assert.That(p.x,Is.InRange(0f,(float)Screen.width),rect.name+": X");Assert.That(p.y,Is.InRange(0f,(float)Screen.height),rect.name+": Y");}
        }
        [UnityTest]
        public IEnumerator MissionAndSquadActions_AreVisible_InBothLanguages()
        {
            var map=Object.FindFirstObjectByType<SiginakMap>();var slip=Object.FindFirstObjectByType<OperationSlip>(FindObjectsInactive.Include);var report=Object.FindFirstObjectByType<SquadReportSlip>(FindObjectsInactive.Include);var before=BoardPlayKit.Snapshot();
            foreach(var lang in new[]{DeskLang.Tr,DeskLang.En})
            {
                DeskLoc.Instance.SetLanguage(lang);
                BoardPlayKit.Rig.ResetView();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
                yield return BoardPlayKit.PressKey(lang == DeskLang.Tr ? Key.F : Key.Digit3);
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
                yield return BoardPlayKit.WaitFor(() => map.SlipOpen, 2, "haritaya varınca görev fişi açılmadı");
                yield return BoardPlayKit.WaitSeconds(.6f);
                Assert.AreEqual(map.State.Front, slip.RegionId);
                OnScreen((RectTransform)Field<Button>(slip,"action").transform);
                map.HandleEnter();yield return BoardPlayKit.WaitSeconds(.4f);Assert.IsTrue(map.SquadReportOpen);OnScreen((RectTransform)Field<Button>(report,"action").transform);
                Assert.IsTrue(map.HandleEscape());yield return BoardPlayKit.WaitSeconds(.2f);Assert.IsTrue(map.SlipOpen);Assert.IsFalse(map.SquadReportOpen);map.HandleEscape();yield return BoardPlayKit.WaitSeconds(.5f);
                Assert.IsFalse(map.SlipOpen, "kapatılan fiş aynı harita ziyaretinde yeniden açılmaz");
            }
            Assert.AreEqual(before,BoardPlayKit.Snapshot());
        }
        [UnityTest]
        public IEnumerator MapCanvasClick_OpensMissions_AndFocusedLandStillSelectsRegion()
        {
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var layer = Object.FindFirstObjectByType<LiberationMapLayer>();
            var slip = Object.FindFirstObjectByType<OperationSlip>(FindObjectsInactive.Include);
            var landPoint = new Vector2(1463, 383);
            var seaPoint = new Vector2(730, 825);
            var region = layer.Regions.First(r => r.Hit.ContainsMapPoint(landPoint));
            Assert.AreNotEqual(map.State.Front, region.RegionId);
            var before = BoardPlayKit.Snapshot();

            ClickMap(seaPoint, null);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
            Assert.AreEqual(map.State.Front, slip.RegionId);
            Assert.IsTrue(map.HandleEscape());
            yield return BoardPlayKit.WaitSeconds(.5f);
            Assert.IsFalse(map.SlipOpen);
            ClickMap(seaPoint, null);
            Assert.AreEqual(map.State.Front, slip.RegionId);

            ClickMap(landPoint, region.Hit);
            Assert.AreEqual(region.RegionId, slip.RegionId);
            BoardPlayKit.Rig.ResetView();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk);
            ClickMap(landPoint, region.Hit);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Map);
            Assert.AreEqual(map.State.Front, slip.RegionId);
            Assert.AreEqual(before, BoardPlayKit.Snapshot());

            void ClickMap(Vector2 point, RegionMeshGraphic land)
            {
                var screen = BoardPlayKit.ScreenOfWorld(layer.MapToWorld(point));
                var hit = BoardPlayKit.FirstHit(screen);
                Assert.IsNotNull(hit.gameObject, point.ToString());
                if (land != null) Assert.AreSame(land.gameObject, hit.gameObject);
                else Assert.IsNotNull(hit.gameObject.GetComponent<DeskHitArea>());
                Assert.IsTrue(BoardPlayKit.ClickHit(hit));
                Assert.IsTrue(map.SlipOpen);
            }
        }
        [UnityTest]
        public IEnumerator NamedPerson_ClickUsesTheRealRaycast_AndSelectsThatCardWithoutAssignment()
        {
            var map=Object.FindFirstObjectByType<SiginakMap>();map.ShowMissions();yield return BoardPlayKit.WaitSeconds(1.4f);var slip=Object.FindFirstObjectByType<OperationSlip>();var member=Field<FieldMemberFace[]>(slip,"planningMembers")[0];var label=Field<TMP_Text>(member,"personName");var point=RectTransformUtility.WorldToScreenPoint(null,label.rectTransform.TransformPoint(label.rectTransform.rect.center));var hit=BoardPlayKit.FirstHit(point);
            var before=BoardPlayKit.Snapshot();Assert.IsNotNull(hit.gameObject);Assert.IsTrue(BoardPlayKit.ClickHit(hit));yield return BoardPlayKit.WaitSeconds(.7f);
            var presenter=Object.FindFirstObjectByType<PlanningBoardPresenter>();Assert.AreEqual(BoardPlayKit.Sim.PlanningSquad()[0].PersonId,presenter.SelectedPersonId);Assert.AreEqual(SiginakPose.Shelf,BoardPlayKit.Rig.Current);Assert.AreEqual(before,BoardPlayKit.Snapshot());
        }
        [UnityTest]
        public IEnumerator Dossier_ExtraPageClickUsesTheModelCollider_WithoutWritingACommand()
        {
            var presenter=Object.FindFirstObjectByType<PlanningBoardPresenter>();presenter.ShowPerson(BoardPlayKit.Sim.Board.People[0].Id);
            yield return BoardPlayKit.WaitFor(()=>BoardPlayKit.Rig.Current==SiginakPose.Shelf&&!BoardPlayKit.Rig.InTransition,3,"K4'e varılmadı");
            var data=PlanningBoardSnapshotBuilder.Build(BoardPlayKit.Sim,presenter.Ui,false,false,false).Folder;
            for(int i=0;i<40;i++)data.DossierRight.Add(new FolderLine("V.031",Loc.T("İZ 1 · erişim kaydı")));
            var folder=presenter.Board3D.Folder;folder.Open(data);
            yield return BoardPlayKit.WaitFor(()=>!folder.Moving,3,"dosya okuma pozuna varmadı");
            yield return BoardPlayKit.Frames(2);
            Physics.SyncTransforms();
            var face=folder.GetComponent<PersonDossierFace>();Assert.Greater(face.PageCount,1);var next=Field<Collider>(folder,"nextPage");Assert.IsTrue(next.enabled);
            var before=BoardPlayKit.Snapshot();var hit=BoardPlayKit.FirstHit(BoardPlayKit.Rig.View.WorldToScreenPoint(next.bounds.center));Assert.AreSame(next.gameObject,hit.gameObject);Assert.IsTrue(BoardPlayKit.ClickHit(hit));Assert.AreEqual(1,face.Page);Assert.AreEqual(before,BoardPlayKit.Snapshot());
        }
        [UnityTest]
        public IEnumerator Crt_ModelPowerAndBoost_ArePresentationOnly()
        {
            var instruments=Object.FindFirstObjectByType<CrtInstruments>();Assert.IsNotNull(instruments);var before=BoardPlayKit.Snapshot();instruments.BeginApproach();yield return BoardPlayKit.WaitSeconds(.35f);instruments.CompleteApproach();Assert.AreEqual(1.4f,instruments.Boost,.001f);instruments.Power(false);Assert.IsFalse(instruments.Powered);Assert.AreEqual(0,instruments.Boost);instruments.Power(true);Assert.IsTrue(instruments.Powered);Assert.AreEqual(before,BoardPlayKit.Snapshot());
        }
    }
}
