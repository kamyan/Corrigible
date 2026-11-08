using System;
using System.IO;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Corrigible.Desk.Tests
{
    /// <summary>R6/R10 verisi: gerçek isimler, eşya, depo sınırı, kalıcı sonuç ve kayıt saflığı.</summary>
    public sealed class Rev3Stage7PaperTests
    {
        static Simulation New(int capacity=50) => Simulation.NewGame(79,new Balance { boardMaterialsCapacity=capacity, liberationAlarmPerShift=0 });
        static void Language(bool en) => Loc.SetLanguage(en?Loc.English:Loc.Turkish,en?Loc.ParseTable(File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"))[Loc.English]:null);
        [TearDown] public void Restore() => Loc.SetLanguage(Loc.Turkish);
        static string Fingerprint(Simulation sim) => sim.S.Rng.State+"|"+sim.Record.commands.Count+"|"+string.Join(",",sim.Board.People.Select(p=>p.Id+p.Job+p.Hp+p.Down+string.Join(";",p.Experience.Select(x=>x.Points))))+"|"+string.Join(",",sim.Board.Materials.Select(m=>m.Quantity));
        [TestCase(false)] [TestCase(true)]
        public void FieldSquad_IsTheBoardPeople_WithTheirOwnedItemsAndRealStats(bool en)
        {
            Language(en);var sim=New();string before=Fingerprint(sim);var slip=LiberationMapBuilder.Slip(sim,"weu");var report=LiberationMapBuilder.SquadReport(sim,"weu");
            Assert.IsTrue(slip.Planning&&report.Planning);Assert.AreEqual(6,slip.Rewards.Count);Assert.AreEqual(3,slip.Members.Count);Assert.IsNotEmpty(report.Warning);
            foreach(var slot in sim.PlanningSquad())
            {
                var person=sim.Board.Person(slot.PersonId);var member=slip.Members.Single(x=>x.PersonId==person.Id);var row=report.Rows.Single(x=>x.PersonId==person.Id);
                Assert.AreEqual(person.Name,member.Name);Assert.AreEqual(member.Name,row.Name);StringAssert.Contains(slot.Move.ToString(),member.Stats);StringAssert.Contains(slot.Power.ToString(),member.Stats);
                StringAssert.Contains(PlanningBoardData.EquipmentName("kit_"+slot.Type),member.Equipment);Assert.AreEqual(3,row.Slots.Length);Assert.AreEqual(member.MaxHealth,person.MaxHp);
            }
            Assert.AreEqual(before,Fingerprint(sim));
        }
        [Test]
        public void MissingKit_HasItsOwnReason_AndDoesNotCreateAnOrder()
        {
            var sim=New();var slot=sim.PlanningSquad()[0];var kit=sim.Board.Equipment.First(e=>e.OwnerId==slot.PersonId&&e.Kind=="kit_tek");Assert.IsTrue(sim.EquipResident(kit.Id,null).Ok);
            var before=Fingerprint(sim);var d=LiberationMapBuilder.Slip(sim,"weu");Assert.AreEqual(SlipActionState.Disabled,d.ActionState);Assert.AreEqual(Loc.T("KİT EKSİK"),d.ActionNote);Assert.AreEqual(before,Fingerprint(sim));
        }
        [Test]
        public void DownPerson_RemainsNamedInTheReport_InsteadOfBecomingAnAnonymousRole()
        {
            var sim=New();var person=sim.Board.Person(sim.PlanningSquad()[1].PersonId);person.Down=2;person.Hp=0;
            var row=LiberationMapBuilder.SquadReport(sim,"weu").Rows.Single(x=>x.PersonId==person.Id);Assert.AreEqual(person.Name,row.Name);Assert.AreEqual(Loc.T("YATIYOR"),row.Status);Assert.IsFalse(row.Good);
        }
        [TestCase(false)] [TestCase(true)]
        public void MaterialGains_UseTheRecordedPreRaidStocks_IncludingCapacity_WithoutMutatingLiveGame(bool en)
        {
            Language(en);var sim=New(4);var before=PlanningBoardData.MaterialKinds.Select(k=>sim.Board.Material(k)?.Quantity??0).ToArray();Assert.IsTrue(sim.StartLiberationRaid("weu_1").Ok);Assert.IsTrue(sim.ResolveRaid(new RaidResult { Success=true,Turns=6 }).Ok);
            string frozen=Fingerprint(sim);var log=sim.S.RaidLog.Last();var gains=RaidPaperFacts.MaterialGains(sim,log);var nominal=RaidPaperFacts.Nominal(sim,true);
            CollectionAssert.AreEqual(before.Select((n,i)=>Math.Max(0,Math.Min(4-n,nominal[i]))),gains);
            var report=OperationReportBuilder.Build(sim);Assert.AreEqual(6,report.Gains.Count);Assert.AreEqual(log.LiberationReward.Materials,gains.Sum());Assert.IsTrue(report.Results.Any(r=>r.Label.Contains(log.LiberationReward.Squad[0].PersonName)));
            Assert.AreEqual(Loc.F("V.{0:000}",1),report.Date);Assert.AreEqual(Loc.F("{0} tur",6),report.Duration);
            Assert.AreEqual(frozen,Fingerprint(sim));var replay=Replay.Restore(sim.Record);CollectionAssert.AreEqual(gains,RaidPaperFacts.MaterialGains(replay,replay.S.RaidLog.Last()));
        }
        [Test]
        public void OldReport_StillUsesItsOwnStocks_AfterLaterShifts()
        {
            var sim=New(4);sim.StartLiberationRaid("weu_1");sim.ResolveRaid(new RaidResult {Success=true});var log=sim.S.RaidLog.Last();var gains=RaidPaperFacts.MaterialGains(sim,log);
            Assert.IsTrue(sim.PrintShift().Ok);Assert.IsTrue(sim.FileShift(sim.Board.ReceiptId).Ok);CollectionAssert.AreEqual(gains,RaidPaperFacts.MaterialGains(sim,log));
        }
        [Test]
        public void BoardReport_ListsEachNamedReturnRescueAndPermanentLoss()
        {
            var sim=New();var squad=sim.PlanningSquad();sim.StartLiberationRaid("weu_1");sim.ResolveRaid(new RaidResult {Retreat=true,Hp={0,squad[1].Hp,0},Left={2}});
            var report=OperationReportBuilder.Build(sim);Assert.IsEmpty(report.Gains);Assert.AreEqual(3,report.Results.Count(r=>squad.Any(p=>r.Label.Contains(p.PersonName))));
            Assert.IsTrue(report.Results.Any(r=>r.Label.Contains(squad[2].PersonName)&&r.Value==Loc.T("KALICI KAYIP")));
        }
        [TestCase(false)] [TestCase(true)]
        public void Dossier_LongHistoryIsPaged_AndLeftFactsAreStructured(bool en)
        {
            Language(en);var scene=EditorSceneManager.OpenScene("Assets/Corrigible/Desk/Scenes/DeskGame.unity",OpenSceneMode.Additive);
            try
            {
                var folder=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PersonFolderView>(true)).Single();var face=folder.GetComponent<PersonDossierFace>();Assert.IsNotNull(face);
                var sim=New();var person=sim.Board.People[0];var data=PlanningBoardSnapshotBuilder.Build(sim,new BoardUiState {SelectedId=person.Id},en,false,false).Folder;
                for(int i=0;i<40;i++)data.DossierRight.Add(new FolderLine("V.031",Loc.T("İZ 1 · erişim kaydı")));
                folder.Open(data);Assert.Greater(face.PageCount,1);Assert.IsFalse(face.Overflowing);int pages=face.PageCount;
                var right=(RectTransform)new SerializedObject(face).FindProperty("right").objectReferenceValue;
                var footer=(TMPro.TMP_Text)new SerializedObject(face).FindProperty("pageNumber").objectReferenceValue;
                for(int i=0;i<pages+1;i++)
                {
                    foreach(var text in right.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t=>t.gameObject.activeSelf&&t.name.EndsWith("(Clone)")))
                        Assert.LessOrEqual(-text.rectTransform.anchoredPosition.y+text.rectTransform.rect.height,-footer.rectTransform.anchoredPosition.y-4,"metin sayfa oklarına taşmaz");
                    face.Turn(1);
                }
                Assert.AreEqual(pages-1,face.Page);for(int i=0;i<pages+1;i++)face.Turn(-1);Assert.AreEqual(0,face.Page);
                Assert.AreEqual(7,data.Mastery.Count);Assert.AreEqual(person.Name,data.Name);
            }
            finally {EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
