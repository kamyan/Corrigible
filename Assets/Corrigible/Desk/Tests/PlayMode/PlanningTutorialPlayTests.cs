using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using K=Corrigible.Desk.Tests.AdmissionPlayKit;
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
            F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("counters",Tutor.CurrentStepId);
            yield return BoardPlayKit.WaitFor(()=>Tutor.Pointer!=null&&Tutor.Pointer.Visible,3,"sayaçlar adımında işaretçi görünmeli");
            F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("board",Tutor.CurrentStepId);Assert.AreEqual(before,BoardPlayKit.Snapshot());
            yield return BoardPlayKit.WaitFor(()=>Tutor.Pointer.Visible&&Tutor.Pointer.Label==Loc.T("BURAYA BAS"),3,"SIĞINAK tuşu işaretlenmeli");
            Assert.IsTrue(Screen.safeArea.Overlaps(Tutor.Pointer.Target),"işaretçi ekranda");
            BoardPlayKit.Presenter.Show();yield return BoardPlayKit.WaitSeconds(.8f);Assert.AreEqual("person",Tutor.CurrentStepId);
            var board=BoardPlayKit.Board;var card=board.Deck.Top;Assert.IsNotNull(card);
            yield return BoardPlayKit.Drag(card.gameObject,BoardPlayKit.BoardWorld(BoardLayout.CardCentre("cook")),12);
            yield return BoardPlayKit.WaitSeconds(.5f);
            Assert.AreEqual("gear",Tutor.CurrentStepId);Assert.AreEqual("board_assign",BoardPlayKit.Sim.Record.commands.Last().kind);Assert.AreEqual("cook",BoardPlayKit.Sim.Record.commands.Last().b);
            before=BoardPlayKit.Snapshot();F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("intel",Tutor.CurrentStepId);
            F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("back",Tutor.CurrentStepId);Assert.AreEqual(before,BoardPlayKit.Snapshot());
            // Panodan dönüş sürerken baskın açılırsa oda kamerası kilitlenir; rehber onu beklememeli.
            BoardPlayKit.Presenter.Close();
            var next=BoardPlayKit.Sim.NextLiberationSector(BoardPlayKit.Sim.S.Liberation.CurrentRegion);
            Assert.IsTrue(BoardPlayKit.Game.StartLiberationRaid(next.Id).Ok);
            yield return BoardPlayKit.WaitFor(()=>Object.FindFirstObjectByType<LiveRaidDataSource>()?.State!=null,8,"baskın sahnesi yüklenmedi");
            yield return BoardPlayKit.WaitSeconds(.4f);Assert.AreEqual("raidBoard",Tutor.CurrentStepId);
            Assert.Greater(F<CanvasGroup>(Note,"group").alpha,.99f,"baskında rehber görünmeli");
            yield return BoardPlayKit.WaitFor(()=>Tutor.Pointer.Visible,3,"baskında tur şeridi işaretlenmeli");
            var unload=UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Baskin");while(unload!=null&&!unload.isDone)yield return null;
        }
        // ---------------------------------------------------------------- KOS aşama 8: aday grubu, olay tutanağı, son şalter

        static SiginakLastSwitch LastSwitch=>BoardPlayKit.Find<SiginakLastSwitch>();
        static SiginakShelterEvent Events=>BoardPlayKit.Find<SiginakShelterEvent>();
        static string Sealed=>Loc.T("mühürlü · rehber");
        void Log(string line)=>Debug.Log("[PlanningTutorialPlayTests] "+line);
        IEnumerator Ack(string id)
        {
            Assert.AreEqual(id,Tutor.CurrentStepId);var before=BoardPlayKit.Snapshot();
            F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual(before,BoardPlayKit.Snapshot(),id+": SONRAKİ komut yazmaz");
        }
        IEnumerator QuickClose()
        {
            int day=BoardPlayKit.Sim.S.Day;
            var p=BoardPlayKit.Game.PrintBoardShift();Assert.IsTrue(p!=null&&p.Ok,"board_print: "+p?.Msg);
            var f=BoardPlayKit.Game.FileBoardShift(BoardPlayKit.Sim.Board.ReceiptId);Assert.IsTrue(f!=null&&f.Ok,"board_file: "+f?.Msg);
            Assert.AreEqual(day+1,BoardPlayKit.Sim.S.Day);yield return BoardPlayKit.WaitSeconds(.6f);
        }
        IEnumerator AssertVisible(string id)
        {
            // Not yeni adımda 0,18 s'de belirir; kamera geçişinde ve dosyalamada gizlidir (Blocked)
            yield return BoardPlayKit.WaitFor(()=>Tutor.CurrentStepId==id&&F<CanvasGroup>(Note,"group").alpha>.99f,4,id+": rehber notu görünmedi (adım "+Tutor.CurrentStepId+", alfa "+F<CanvasGroup>(Note,"group").alpha+")");
            var body=F<TMP_Text>(Note,"body");body.ForceMeshUpdate();Assert.IsFalse(body.isTextOverflowing,id+": metin taşmıyor");
            Assert.AreEqual(PlanningTutorialFlow.BodyText(PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf(id)],BoardPlayKit.Sim),body.text,id+": not adımın canlı metnini yazar");
        }
        static Vector3 PaperWorld(ShelterEventPaperView v,Vector2 mm)=>v.transform.TransformPoint(new Vector3(
            (mm.x-ShelterEventPaperFace.WidthMm/2f)/1000f,(ShelterEventPaperFace.HeightMm/2f-mm.y)/1000f,0f));
        /// <summary>Kâğıdın mm noktasına gerçek ışınla sol tık (ShelterEventPlayTests ile aynı kural).</summary>
        static IEnumerator ClickPaper(ShelterEventPaperView v,Vector2 mm,string what)
        {
            var screen=BoardPlayKit.ScreenPoint(PaperWorld(v,mm));var hit=BoardPlayKit.FirstHit(screen);
            Assert.AreSame(v.HitArea.gameObject,hit.gameObject,what+": ilk ışın tutanağa düşmeli ("+BoardPlayKit.DescribeHits(screen)+")");
            K.ClickHit(hit);yield return BoardPlayKit.Frames(2);
        }
        /// <summary>Notun kâğıdının ekran köşeleri (Screen Space Overlay: dünya köşeleri piksel).</summary>
        Vector2[] NoteQuad(){var c=new Vector3[4];F<RectTransform>(Note,"card").GetWorldCorners(c);return c.Select(p=>(Vector2)p).ToArray();}
        /// <summary>
        /// Tutanağın yazı çerçevesinin ekran köşeleri (iç çerçeve kenardan 3 mm içeride; eğik görünümde de tam). Dış kenar payını
        /// örtmek okumayı engellemez.
        /// </summary>
        static Vector2[] PaperQuad(ShelterEventPaperView v,float insetMm=3f)
        {
            float w=ShelterEventPaperFace.WidthMm,h=ShelterEventPaperFace.HeightMm;
            return new[]{new Vector2(insetMm,insetMm),new Vector2(w-insetMm,insetMm),new Vector2(w-insetMm,h-insetMm),new Vector2(insetMm,h-insetMm)}
                .Select(mm=>BoardPlayKit.ScreenPoint(PaperWorld(v,mm))).ToArray();
        }
        /// <summary>Bir nesnenin çizilen parçalarının ekran dikdörtgeni (dünya sınır kutularının köşeleri; biraz geniş tutar).</summary>
        static Vector2[] RenderedQuad(Transform t)
        {
            var pts=new System.Collections.Generic.List<Vector2>();
            foreach(var r in t.GetComponentsInChildren<Renderer>()){if(!r.enabled||!r.gameObject.activeInHierarchy)continue;var b=r.bounds;
                for(int i=0;i<8;i++)pts.Add(BoardPlayKit.ScreenPoint(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z)));}
            if(pts.Count==0)return new Vector2[0];
            float x0=pts.Min(p=>p.x),x1=pts.Max(p=>p.x),y0=pts.Min(p=>p.y),y1=pts.Max(p=>p.y);
            return new[]{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)};
        }
        /// <summary>İki dışbükey çokgenin iç içe geçme derinliği (ayırıcı eksen; px). Ayrıksa 0 ya da eksi.</summary>
        static float Penetration(Vector2[] a,Vector2[] b)
        {
            float depth=float.MaxValue;
            foreach(var poly in new[]{a,b})
                for(int i=0;i<poly.Length;i++)
                {
                    var e=poly[(i+1)%poly.Length]-poly[i];if(e.sqrMagnitude<1e-6f)continue;var n=new Vector2(-e.y,e.x).normalized;
                    float a0=a.Min(p=>Vector2.Dot(p,n)),a1=a.Max(p=>Vector2.Dot(p,n)),b0=b.Min(p=>Vector2.Dot(p,n)),b1=b.Max(p=>Vector2.Dot(p,n));
                    depth=Mathf.Min(depth,Mathf.Min(a1,b1)-Mathf.Max(a0,b0));
                }
            return depth;
        }
        /// <summary>Not, okunması gereken kâğıdı ya da kartı örtmez (4 px pay: bitişik kenar örtmek sayılmaz).</summary>
        void AssertNoteClear(Vector2[] thing,string what)
        {
            if(thing.Length==0){Log("örtüşme · "+what+" · çizilen parça yok");return;}
            float d=Penetration(NoteQuad(),thing);
            Log("örtüşme · "+what+" · derinlik "+d.ToString("0.0")+" px · nesne "+string.Join(" ",thing.Select(p=>p.ToString("0"))));
            Assert.LessOrEqual(d,4f,what+": not üstüne biniyor ("+d.ToString("0.0")+" px)");
        }
        void AssertSealed(string when)
        {
            Assert.IsTrue(Tutor.Active,when+": rehber sürüyor");
            Assert.AreEqual(Sealed,LastSwitch.LockPlate,when+": son şalter rehber mührüyle kapalı");
            Assert.IsNotNull(LastSwitch.Switch.LockPlate,when+": şalterin kendisi kilitli");
        }

        /// <summary>
        /// KOS aşama 8: ilk vardiyadan sonra rehber hedef satırını gösterir, sonra gerçek akışta gelen ilk olay tutanağını (kapı
        /// panosuna düşer → KAPI tuşu → panodaki kâğıda tık → elde → satır → KARAR) ve ilk aday grubunu (KAPI → tezgâh → GERİ ÇEVİR damgası fişe) sırasıyla değil,
        /// geldikleri anda anlatır. Rehber sürerken son şalter "mühürlü · rehber"dir; İFŞA, son şalter ve kapanış bilgileri onaylanınca
        /// mühür kalkar. Rehber kendi komutunu yazmaz.
        /// </summary>
        [UnityTest] public IEnumerator Guide_ShowsTheFirstGroupAndEvent_FromTheRealFlow_WhileTheLastSwitchStaysSealed()
        {
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);yield return BoardPlayKit.Frames(2);
            var sim=BoardPlayKit.Sim;
            yield return Ack("intro");AssertSealed("ilk adım");
            yield return QuickClose();
            yield return AssertVisible("goal");StringAssert.EndsWith(PlanningTutorialFlow.GoalLine(sim),F<TMP_Text>(Note,"body").text,"hedef satırı canlı");
            yield return Ack("goal");yield return AssertVisible("watch");
            bool sawEvent=false,sawGroup=false;
            for(int guard=0;guard<10&&!(sawEvent&&sawGroup);guard++)
            {
                AssertSealed("gün "+sim.S.Day);
                if(sim.PendingShelterEvent!=null&&!sawEvent)
                {
                    var view=Events.View;
                    yield return ShiftPlayKit.WaitDoor(view,"rehberin ilk tutanağı",6);
                    yield return BoardPlayKit.Frames(2);yield return AssertVisible("event");Log("olay "+sim.PendingShelterEvent.Kind+" · gün "+sim.S.Day);
                    // Adımın dediği gibi: KAPI tuşuyla kapıya dön (adım sürer), çekmeceyi açıp tutanağı al
                    yield return K.EnterDoor();yield return AssertVisible("event");
                    yield return ShiftPlayKit.TakeFromDoor(view,"rehberin ilk tutanağı");
                    yield return BoardPlayKit.Frames(2);yield return AssertVisible("decide");AssertSealed("tutanak elde");
                    AssertNoteClear(PaperQuad(view),"elde tutanağın yazı çerçevesi");
                    var rows=Events.Last.Rows;int pick=rows.FindIndex(r=>r.Enabled&&r.Safe);if(pick<0)pick=rows.FindIndex(r=>r.Enabled);
                    Assert.GreaterOrEqual(pick,0,"açık satır var");
                    yield return ClickPaper(view,view.Face.RowRect(pick).center,"satır "+(pick+1));
                    Assert.AreEqual(rows[pick].Id,Events.Last.Selected?.Id,"satır işaretlendi");
                    yield return ClickPaper(view,view.Face.ConfirmRect.center,"KARAR kutusu");
                    Assert.AreEqual("event_decide",sim.Record.commands.Last().kind);
                    yield return BoardPlayKit.WaitFor(()=>!Events.Busy,6,"tutanak çekmeceye konmadı");
                    // Adımın dediği gibi: karar çekmeceye konunca şalteri çekip dışarıya bildir
                    yield return ShiftPlayKit.SendFromDrawer(view,"rehberin ilk tutanağı");
                    yield return BoardPlayKit.Frames(2);sawEvent=true;
                    Assert.IsFalse(Tutor.CurrentStepId=="event"||Tutor.CurrentStepId=="decide","karar verilince olay adımı kapanır");
                    // Karar K8'de verildi; kamera orada kalır. Grup kolu KAPI tuşuyla masadan girer: masaya dön
                    BoardPlayKit.Rig.ResetView();yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk,3);yield return BoardPlayKit.Frames(2);
                    continue;
                }
                if(sim.Admission.Group.Count>0&&!sawGroup)
                {
                    yield return BoardPlayKit.Frames(2);yield return AssertVisible("door");Log("grup "+sim.Admission.Group.Count+" aday · gün "+sim.S.Day);
                    yield return K.EnterDoor();yield return AssertVisible("door");
                    yield return K.ToCounter();yield return AssertVisible("stamp");AssertSealed("tezgâhta");
                    var cards=K.Station.Candidates;
                    for(int i=0;i<cards.CardCount;i++)AssertNoteClear(RenderedQuad(cards.CardPapers[i]),"aday kartı "+(i+1));
                    AssertNoteClear(RenderedQuad(cards.SlipPaper),"grup fişi");var up=K.Station.Top.transform.up;int before=sim.Record.commands.Count;
                    yield return K.DragStamp(StampChoice.Reject,st=>BoardPlayKit.ScreenPoint(cards.SlipPaper.position+up*st.HeldHeight));
                    yield return BoardPlayKit.WaitSeconds(.5f);
                    if(sim.Record.commands.Count==before){Log("GERİ ÇEVİR sürüklemesi fişe varmadı; damga basışı doğrudan");Assert.IsTrue(K.Admission.PressStamp(StampChoice.Reject));}
                    Assert.AreEqual("candidate_pass",sim.Record.commands.Last().kind);
                    yield return K.Settle(10);
                    Assert.IsFalse(Tutor.CurrentStepId=="door"||Tutor.CurrentStepId=="stamp","karar verilince kapı adımı kapanır");
                    BoardPlayKit.Rig.ResetView();yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk,3);yield return BoardPlayKit.Frames(2);
                    sawGroup=true;continue;
                }
                if(!(sawEvent&&sawGroup))yield return AssertVisible("watch");
                yield return QuickClose();
            }
            Assert.IsTrue(sawEvent,"ilk olay gelmedi (gün "+sim.S.Day+")");Assert.IsTrue(sawGroup,"ilk grup gelmedi (gün "+sim.S.Day+")");
            yield return BoardPlayKit.Frames(2);yield return AssertVisible("exposure");AssertSealed("İFŞA bilgisi");
            yield return Ack("exposure");yield return AssertVisible("switch");AssertSealed("son şalter bilgisi");
            yield return Ack("switch");yield return AssertVisible("go");
            yield return Ack("go");yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(Tutor.Active,"rehber bitti");Assert.AreNotEqual(Sealed,LastSwitch.LockPlate,"rehber mührü kalktı");
            Log("bitti · gün "+sim.S.Day+" · son şalter: "+(LastSwitch.LockPlate??"açık"));
        }

        /// <summary>
        /// Her adımın canlı metni notun kâğıdına sığar ve kâğıt ekranda kalır (TR ve EN; 1920×1080 oyun görünümü). Başlık ve sayaç oyundaki
        /// gibidir (başlık "REHBER" tek satır, konu bölümünde sayaç "REHBER · KONU"); ilk vardiyanın baskın adımları
        /// baskın yerleşiminde (sağ üst) de ölçülür.
        /// </summary>
        [UnityTest] public IEnumerator EveryStep_FitsTheNote_InBothLanguages()
        {
            F<Button>(Note,"skip").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.IsFalse(Tutor.Active);var bad=new System.Collections.Generic.List<string>();
            foreach(var lang in new[]{DeskLang.Tr,DeskLang.En})
            {
                DeskLoc.Instance.SetLanguage(lang);yield return BoardPlayKit.Frames(2);
                // Asset oyunu (tesis, alt bayraklar açık) ve alt bayraksız tesis (rehberin taban metinleri)
                var plain=Simulation.NewGame(79,new Balance{facilityTitlesEnabled=false,facilityDefenseEnabled=false,infiltratorFieldEnabled=false});
                foreach(var simulation in new[]{BoardPlayKit.Sim,plain})
                foreach(var step in PlanningTutorialFlow.Steps)
                foreach(bool raidLayout in new[]{false,true})
                {
                    int index=PlanningTutorialFlow.IndexOf(step.Id);
                    bool raidStep=index>=PlanningTutorialFlow.IndexOf("raidBoard")&&index<=PlanningTutorialFlow.IndexOf("raidEscape");
                    if(raidLayout&&!raidStep)continue;
                    bool topic=step.Topic!=null||step.Id=="watch";
                    string expected=PlanningTutorialFlow.BodyText(step,simulation);
                    Note.SetRaid(raidLayout);
                    Note.Show(new TutorialNoteData{Head=Loc.T("REHBER"),
                        Step=topic?Loc.T("REHBER · KONU"):Loc.F("ADIM {0}/{1}",index+1,PlanningTutorialFlow.Steps.Length),Title=Loc.T(step.Title),Body=expected,ShowOk=step.Info,Ok=Loc.T("SONRAKİ"),Skip=Loc.T("EĞİTİMİ BİTİR")});
                    yield return BoardPlayKit.WaitSeconds(.3f);
                    Assert.AreEqual(expected,F<TMP_Text>(Note,"body").text,step.Id);
                    foreach(var t in new[]{F<TMP_Text>(Note,"body"),F<TMP_Text>(Note,"title")}){t.ForceMeshUpdate();if(t.isTextOverflowing)bad.Add(lang+" · "+step.Id+": "+t.name+" taşıyor");Assert.GreaterOrEqual(t.fontSize,14);}
                    foreach(var t in new[]{F<TMP_Text>(Note,"head"),F<TMP_Text>(Note,"step")}){t.ForceMeshUpdate();if(t.isTextOverflowing)bad.Add(lang+" · "+step.Id+": "+t.name+" taşıyor");
                        if(t.textInfo.lineCount>1)bad.Add(lang+" · "+step.Id+": "+t.name+" "+t.textInfo.lineCount+" satıra kırılıyor ("+t.text+")");}
                    var corners=new Vector3[4];F<RectTransform>(Note,"card").GetWorldCorners(corners);
                    float top=corners.Max(c=>c.y),left=corners.Min(c=>c.x),right=corners.Max(c=>c.x),bottom=corners.Min(c=>c.y);
                    string at=lang+(simulation==plain?" · alt bayraksız":" · tesis")+" · "+step.Id+(raidLayout?" · baskın":"");
                    Debug.Log("[PlanningTutorialPlayTests] not "+at+" · üst "+top.ToString("0")+" alt "+bottom.ToString("0")+" / "+Screen.height);
                    if(top>Screen.height+1||bottom< -1||left< -1||right>Screen.width+1)bad.Add(at+": kâğıt ekrandan taşıyor (üst "+top.ToString("0")+", alt "+bottom.ToString("0")+" / "+Screen.height+")");
                }
                Note.SetRaid(false);
            }
            Assert.IsEmpty(bad,string.Join("\n",bad));
        }
        [UnityTest] public IEnumerator ResumeAndSkip_PreserveTheCampaignAndRememberTheTutorialChoice()
        {
            F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("counters",Tutor.CurrentStepId);
            F<Button>(Note,"ok").onClick.Invoke();yield return BoardPlayKit.Frames(2);Assert.AreEqual("board",Tutor.CurrentStepId);BoardPlayKit.Game.Save();
            yield return BoardPlayKit.ContinueDesk();yield return BoardPlayKit.Frames(3);Assert.IsTrue(Tutor.Active);Assert.AreEqual("board",Tutor.CurrentStepId,"adım kimlikle sürer");
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

    public sealed class FacilityPlanningTutorialPlayTests
    {
        [UnityTearDown] public IEnumerator Teardown() => BoardPlayKit.TearDown();

        [UnityTest] public IEnumerator FirstWing_OpensWithTheSpecialistNote()
        {
            yield return BoardPlayKit.LoadDesk(tutorial:true);
            var sim=BoardPlayKit.Sim;
            var wing=sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsTrue(BoardPlayKit.Game.StartLiberationRaid(wing.Id).Ok);
            yield return BoardPlayKit.WaitFor(()=>Object.FindFirstObjectByType<LiveRaidDataSource>()?.State?.Room!=null,12,"ilk tesis odası yüklenmedi");
            var tutor=Object.FindFirstObjectByType<SiginakTutorial>();
            yield return BoardPlayKit.WaitFor(()=>tutor.CurrentStepId=="raidBoard",5,"rehber tesis tahtasına geçmedi");
            var note=tutor.GetComponentInChildren<TutorialNoteView>(true);
            var body=(TMP_Text)typeof(TutorialNoteView).GetField("body",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(note);
            var step=PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("raidBoard")];
            Assert.AreEqual(PlanningTutorialFlow.BodyText(step,sim),body.text);
        }
    }
}
