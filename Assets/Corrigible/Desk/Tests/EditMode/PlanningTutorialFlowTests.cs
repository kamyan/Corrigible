using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
namespace Corrigible.Desk.Tests
{
    public sealed class PlanningTutorialFlowTests
    {
        static void At(PlanningTutorialFlow f,string id)=>Assert.AreEqual(id,f.Current?.Id);
        /// <summary>İlk vardiya dosyalanmış, hedef onaylanmış: konu bölümünün başı.</summary>
        static PlanningTutorialFlow InTopics(PlanningTutorialContext c)
        {
            var f=new PlanningTutorialFlow(PlanningTutorialFlow.GoalIndex);c.Day=System.Math.Max(c.Day,1);f.Update(c);At(f,"goal");f.Acknowledge();f.Update(c);return f;
        }

        [Test] public void FirstShift_RequiresRealActions_AndPhysicalFiling()
        {
            var f=new PlanningTutorialFlow();var c=new PlanningTutorialContext();
            void At(string id)=>Assert.AreEqual(id,f.Current?.Id);
            At("intro");f.Update(c);At("intro");f.Acknowledge();f.Update(c);At("counters");f.Acknowledge();f.Update(c);At("board");
            c.BoardOpen=true;f.Update(c);At("person");c.PersonSelected=true;f.Update(c);At("assign");f.Update(c);At("assign");
            c.AssignedCook=true;f.Update(c);At("gear");f.Acknowledge();f.Update(c);At("intel");f.Acknowledge();f.Update(c);At("back");
            c.BoardOpen=false;f.Update(c);At("slip");c.SlipOpen=true;f.Update(c);At("order");
            c.RaidOrdered=c.RaidLoaded=true;f.Update(c);At("raidBoard");f.Acknowledge();f.Update(c);At("raidIntent");f.Acknowledge();f.Update(c);At("raidSelect");
            c.RaidSelected=true;f.Update(c);At("raidMove");
            c.RaidMoved=true;f.Update(c);At("raidAbility");c.RaidActed=true;f.Update(c);At("raidTurn");c.RaidTurnEnded=true;f.Update(c);At("raidGate");
            c.RaidProgress=1;f.Update(c);At("raidPass");c.RaidProgress=2;f.Update(c);At("raidMiddle");c.RaidProgress=3;f.Update(c);At("raidTarget");
            c.RaidProgress=4;f.Update(c);At("raidEscape");
            c.RaidOver=true;f.Update(c);At("result");c.RaidOrdered=c.RaidLoaded=false;c.Raids=1;f.Update(c);At("result");
            c.ReportOpen=true;f.Update(c);At("report");c.ReportOpen=false;f.Update(c);At("rest");f.Acknowledge();f.Update(c);At("print");
            c.Printed=true;f.Update(c);At("file");f.Update(c);At("file");c.Day=1;c.Printed=false;f.Update(c);At("goal");
            f.Acknowledge();f.Update(c);At("watch");Assert.IsFalse(f.Finished,"rehber ilk aday grubunu ve ilk olayı bekler");
        }
        [Test] public void EarlierRaidOrEarlyLoss_DoesNotStrandTheGuideInPreparationOrActions()
        {
            var f=new PlanningTutorialFlow();var c=new PlanningTutorialContext{RaidOrdered=true};f.Update(c);Assert.AreEqual("raidBoard",f.Current.Id);
            c.RaidOver=true;f.Update(c);Assert.AreEqual("result",f.Current.Id);
        }
        [Test] public void FiledFirstShift_SkipsTheFirstShiftSteps_ButNotTheGoal()
        {
            var f=new PlanningTutorialFlow(1);var c=new PlanningTutorialContext{Day=1};f.Update(c);At(f,"goal");
            // Hedef bir bilgi adımıdır: kendiliğinden geçilmez; sonraki vardiyada sefere çıkmak geri götürmez.
            c.RaidOrdered=true;c.Day=2;f.Update(c);At(f,"goal");
        }
        [Test] public void BlockingSequence_Waits_AndResumeRestoresOnlyAValidStep()
        {
            var f=new PlanningTutorialFlow(PlanningTutorialFlow.IndexOf("board"));var c=new PlanningTutorialContext{Blocked=true,BoardOpen=true};f.Update(c);Assert.AreEqual("board",f.Current.Id);
            c.Blocked=false;f.Update(c);Assert.AreEqual("person",f.Current.Id);
            Assert.AreEqual(0,new PlanningTutorialFlow(-1).Index);Assert.AreEqual(PlanningTutorialFlow.Steps.Length-1,new PlanningTutorialFlow(999).Index);
        }
        [Test] public void RealAssignment_AlsoCountsAsHandlingAPersonCard()
        {
            var f=new PlanningTutorialFlow(PlanningTutorialFlow.IndexOf("person"));f.Update(new PlanningTutorialContext{AssignedCook=true});Assert.AreEqual("gear",f.Current.Id);
        }
        [Test] public void Steps_AreUniqueAndSkippable_WithoutGameState()
        {
            CollectionAssert.AllItemsAreUnique(PlanningTutorialFlow.Steps.Select(s=>s.Id));Assert.IsTrue(PlanningTutorialFlow.Steps.All(s=>!string.IsNullOrWhiteSpace(s.Body)&&(s.Info||s.Done!=null)));
            var f=new PlanningTutorialFlow();f.Skip();Assert.IsFalse(f.Update(new PlanningTutorialContext()));Assert.IsNull(f.Current);
        }

        // ---------------------------------------------------------------- KOS aşama 8: aday grubu, olay, İFŞA, son şalter

        [Test] public void Topics_EventThenGroup_FollowTheGame_ThenExposureSwitchAndEnd()
        {
            var c=new PlanningTutorialContext();var f=InTopics(c);At(f,"watch");Assert.IsTrue(f.InTopics);
            c.Day=2;c.EventOpen=true;f.Update(c);At(f,"event");
            c.EventInHand=true;f.Update(c);At(f,"decide");
            c.EventInHand=false;f.Update(c);At(f,"event");   // tepsiye geri kondu: önce al
            c.EventInHand=true;f.Update(c);At(f,"decide");
            c.EventInHand=c.EventOpen=false;c.EventDone=true;f.Update(c);At(f,"watch");
            c.Day=3;c.GroupWaiting=true;f.Update(c);At(f,"door");
            c.AtCounter=true;f.Update(c);At(f,"stamp");
            c.AtCounter=false;f.Update(c);At(f,"door");      // tezgâhtan ayrıldı: geri götürür
            c.AtCounter=true;f.Update(c);At(f,"stamp");
            c.GroupWaiting=false;c.GroupDone=true;f.Update(c);At(f,"exposure");Assert.IsFalse(f.InTopics);
            f.Acknowledge();f.Update(c);At(f,"switch");f.Acknowledge();f.Update(c);At(f,"go");f.Acknowledge();Assert.IsTrue(f.Finished);
        }
        [Test] public void Topics_GroupFirst_AndASecondEventDoesNotReopenADoneTopic()
        {
            var c=new PlanningTutorialContext();var f=InTopics(c);
            c.Day=3;c.GroupWaiting=true;f.Update(c);At(f,"door");
            // Grup varken olay da açıldı: sıradaki konu olaydır (önce karar, sonra kapı)
            c.EventOpen=true;f.Update(c);At(f,"event");c.EventOpen=false;c.EventDone=true;f.Update(c);At(f,"door");
            c.GroupWaiting=false;c.GroupDone=true;f.Update(c);At(f,"exposure");
            c.EventOpen=true;c.GroupWaiting=true;f.Update(c);At(f,"exposure");
        }
        [Test] public void Topics_ExpiredGroupCountsAsDone_AndWatchEndsOnItsLastDay()
        {
            var c=new PlanningTutorialContext{EventDone=true};var f=InTopics(c);At(f,"watch");
            c.Day=PlanningTutorialFlow.WatchLastDay-1;f.Update(c);At(f,"watch");
            c.Day=PlanningTutorialFlow.WatchLastDay;c.GroupWaiting=true;f.Update(c);At(f,"door");   // kapıda grup varken son gün de bekler
            c.GroupWaiting=false;f.Update(c);At(f,"exposure");                                    // konu çıkmadan son gün: rehber sürer, mühür kalkar
            var g=InTopics(new PlanningTutorialContext());var d=new PlanningTutorialContext{Day=4,GroupDone=true,EventDone=true};g.Update(d);At(g,"exposure");
        }
        [Test] public void Resume_InsideTopics_RepicksFromTheGame()
        {
            int door=PlanningTutorialFlow.IndexOf("door");
            var f=new PlanningTutorialFlow(door);f.Update(new PlanningTutorialContext{Day=5});At(f,"watch");
            f=new PlanningTutorialFlow(door);f.Update(new PlanningTutorialContext{Day=5,EventOpen=true,EventInHand=true});At(f,"decide");
            f=new PlanningTutorialFlow(door);f.Update(new PlanningTutorialContext{Day=5,GroupDone=true,EventDone=true});At(f,"exposure");
        }
        [Test] public void Blocked_DoesNotPickATopic()
        {
            var c=new PlanningTutorialContext();var f=InTopics(c);c.GroupWaiting=true;c.Blocked=true;Assert.IsFalse(f.Update(c));At(f,"watch");
            c.Blocked=false;f.Update(c);At(f,"door");
        }
        [Test] public void Bodies_FillLiveValues_GoalLineReadsTheFourCounters()
        {
            var sim=Simulation.NewGame(79,new Balance());
            Loc.SetLanguage(Loc.Turkish);
            string goal=PlanningTutorialFlow.GoalLine(sim);
            int total=sim.S.Liberation.Regions.Sum(r=>r.Sectors.Count);
            StringAssert.Contains("sektör\u00a00/"+total,goal);   // etiket ve değer bölünmez boşlukla bağlı
            StringAssert.Contains("güven\u00a0"+sim.S.Trust,goal);
            StringAssert.Contains("/"+Loc.Num(sim.B.liberationAlarmThreshold,"0.#"),goal);
            StringAssert.Contains("ifşa\u00a00/"+sim.B.exposureThreshold,goal);
            foreach(var s in PlanningTutorialFlow.Steps)
            {
                string body=PlanningTutorialFlow.BodyText(s,sim);
                Assert.IsFalse(body.Contains("{0}"),s.Id+": yer tutucu dolmadı");
                if(s.Id=="goal"||s.Id=="watch")StringAssert.EndsWith(goal,body);
            }
            StringAssert.Contains(sim.B.lastSwitchMinShift+". vardiyadan",PlanningTutorialFlow.BodyText(PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("switch")],sim));
            StringAssert.Contains("İfşa "+sim.B.exposureThreshold,PlanningTutorialFlow.BodyText(PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("exposure")],sim));
            StringAssert.Contains(Loc.Num(sim.DismissSuppliesPreview,"0.##")+" erzak",PlanningTutorialFlow.BodyText(PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("stamp")],sim));
        }
        /// <summary>
        /// 2026-11-24 (geliştirici: "ilk baskın bitene kadar adım adım"): ilk baskın tahtadan dönüşe dokuz adımdır; her adım tesisin
        /// gerçek etiketini anar (GEÇ, EMRİ İLET, kasa, giriş geçidi). Sahadaki sızmacı varsayılan açık: İFŞA adımı köpeği ve EMP sonrası
        /// kilitlenmeyi anar.
        /// </summary>
        [Test] public void RaidSteps_WalkTheWholeFirstWing()
        {
            Loc.SetLanguage(Loc.Turkish);
            var sim=Simulation.NewGame(79,new Balance{shelterEventsEnabled=false});
            string[] raid={"raidBoard","raidIntent","raidSelect","raidMove","raidAbility","raidTurn","raidGate","raidPass","raidMiddle","raidTarget","raidEscape"};
            int at=PlanningTutorialFlow.IndexOf("order");
            foreach(var id in raid){Assert.AreEqual(++at,PlanningTutorialFlow.IndexOf(id),id+" sırası");Assert.IsNotNull(PlanningTutorialFlow.Steps[at].Done,id+": baskın ilerleyince kendiliğinden geçer");}
            Assert.AreEqual("result",PlanningTutorialFlow.Steps[at+1].Id);
            StringAssert.Contains("GEÇ",Body("raidPass",sim));
            StringAssert.Contains("yol çizilir",Body("raidMove",sim));
            StringAssert.Contains("kasa",Body("raidTarget",sim));
            StringAssert.Contains("giriş geçidi",Body("raidEscape",sim));
            StringAssert.Contains("01",Body("slip",sim));
            Assert.AreEqual(PlanningTutorialFlow.GoalIndex,PlanningTutorialFlow.IndexOf("goal"));
            var exposure=PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf("exposure")];
            Assert.IsTrue(new Balance().infiltratorFieldEnabled,"saha alt bayrağı varsayılan açık");
            StringAssert.Contains("EMP",PlanningTutorialFlow.BodyText(exposure,Simulation.NewGame(79,new Balance())));
            Assert.AreEqual("HEDEF: KASAYI AÇ",Step("raidTarget").Title);
        }
        // ---------------------------------------------------------------- tesis rehberi (aşama 17): taban dersleri, koşullu notlar, alt bayraklar

        const string TablePath="Assets/Corrigible/Resources/Localization/strings.txt";
        static PlanningTutorialStep Step(string id)=>PlanningTutorialFlow.Steps[PlanningTutorialFlow.IndexOf(id)];
        static string Body(string id,Simulation sim)=>PlanningTutorialFlow.BodyText(Step(id),sim);
        static Simulation Facility(System.Action<Balance> tune=null)
        {
            var b=new Balance{shelterEventsEnabled=false};tune?.Invoke(b);return Simulation.NewGame(79,b);
        }
        /// <summary>Aynı oyunun alt bayraksız (taban) metni: adımın tablodaki gövdesi, yer tutucuları <see cref="PlanningTutorialFlow.BodyText"/>'in
        /// varsayılan koluyla aynı değerlerle dolu (tesis notu, unvan, kuşatma ve saha şüphesi yok).</summary>
        static string BaseBody(string id,Simulation sim)
        {
            var s=Step(id);
            switch(id)
            {
                case "goal":case "watch":return Loc.F(s.Body,PlanningTutorialFlow.GoalLine(sim));
                case "exposure":return Loc.F(s.Body,sim.B.exposureThreshold);
                case "switch":return Loc.F(s.Body,sim.B.lastSwitchMinShift);
                default:return Loc.T(s.Body);
            }
        }
        static void UseEnglish()
        {
            Loc.ParseTable(System.IO.File.ReadAllText(TablePath)).TryGetValue(Loc.English,out var en);Loc.SetLanguage(Loc.English,en);
        }
        /// <summary>Yürürlükte bir önlem (itme sınıfı 1. kademe): öğrenme bölge tamamlanınca olur, ilk baskında hiç yoktur.</summary>
        static void ForceCountermeasure(Simulation sim)
        {
            sim.Facility.Counter.Clear();foreach(var _ in FacilityCounters.Classes)sim.Facility.Counter.Add(0);
            sim.Facility.Counter[0]=1;
        }

        /// <summary>
        /// Tesis notları taban metnin tesiste de geçerli derslerini silmez: tur başına bir hareket ve bir yetenek, Z ile geri alma,
        /// EMP ve Analist yetenekleri ve önizleme, kalıcı ölüm, kaybetme koşulları, izler ve kurtuluş yolları; tesise özgü ders eklenir.
        /// </summary>
        [Test] public void FacilityNotes_KeepTheBaseLessons()
        {
            Loc.SetLanguage(Loc.Turkish);
            var sim=Facility(b=>b.infiltratorFieldEnabled=true);
            string move=Body("raidMove",sim);
            StringAssert.StartsWith(BaseBody("raidMove",sim),move);
            foreach(var k in new[]{"bir hareket ve bir yetenek","Z son hamleyi geri alır","E ile","kapıya sürüklesin","düşeni"})StringAssert.Contains(k,move,"raidMove");
            StringAssert.DoesNotContain("yaralı",move,"tahtanın terimi düşen");
            string ability=Body("raidAbility",sim);
            StringAssert.StartsWith(BaseBody("raidAbility",sim),ability);
            foreach(var k in new[]{"EMP makineleri iter","Analist çeker","önizleme","sabote","tur sayacını"})StringAssert.Contains(k,ability,"raidAbility");
            string goal=Body("goal",sim);
            foreach(var k in new[]{"kazanırsın","GÜVEN 0","İFŞA eşikte","açlıkla","ALARM eşikte YZ bir kanadı kuşatır","SAVUN","BIRAK"})StringAssert.Contains(k,goal,"goal");
            StringAssert.DoesNotContain("bir sektörü geri alır",goal,"savunma açıkken eşik anlık karşı saldırı değil, kuşatmadır");
            StringAssert.EndsWith(PlanningTutorialFlow.GoalLine(sim),goal);
            string exposure=Body("exposure",sim);
            foreach(var k in new[]{"Bir iz şüphedir, iki iz güçlü kanıttır","İfşa "+sim.B.exposureThreshold+" olunca","KONUM ÇÖZÜLDÜ","sorgu, son baskın ya da son şalter","köpeğin hırlaması"})
                StringAssert.Contains(k,exposure,"exposure");
            string go=Body("go",sim);
            foreach(var k in new[]{"rehber mührü kalkar","tarif defterini","E tesis hamlesini","R taşınan ekipmanı","SONRAKİ"})StringAssert.Contains(k,go,"go");
            foreach(var k in new[]{"Yeni tarifi","Sonraki baskında"})StringAssert.DoesNotContain(k,go,"go: gerçekleşmemiş olabilecek durum ya da E'nin ertelenmesi");
            StringAssert.Contains("unvan",Body("report",sim));
            StringAssert.Contains("kazançları",Body("report",sim),"report: tabanın kazanç dersi");
        }

        /// <summary>
        /// İlk kanadın gerçek hâli: yeni oyunda sıradaki kanadın üç odası da bilinmez ve paftası "PLAN YOK" damgalıdır; fiş adımı bunu
        /// ve panodan haritaya geçişi söyler, görünmeyen bina planını ya da donanımı incelemeyi söylemez.
        /// </summary>
        [Test] public void FacilitySlip_MatchesTheUnknownFirstWing()
        {
            Loc.SetLanguage(Loc.Turkish);
            var sim=Facility();
            var region=sim.S.Liberation.CurrentRegion;
            var plan=FacilityPlanBuilder.Build(sim,region);
            Assert.IsNotNull(plan);Assert.AreEqual(1,plan.Wing,"ilk kanat");
            Assert.AreEqual(3,plan.Sheets.Count);
            string slip=Body("slip",sim);
            foreach(var sheet in plan.Sheets){Assert.IsFalse(sheet.Known,sheet.RoomId+" bilinmemeli");StringAssert.Contains(sheet.Stamp,slip);}
            StringAssert.Contains(Loc.T("MASAYA DÖN"),Body("back",sim),"panodan çıkış ayrı adım");
            StringAssert.Contains("01 kanadı",slip);
            foreach(var k in new[]{"donanım","karargâh","Bina planında"})StringAssert.DoesNotContain(k,slip);
        }

        /// <summary>
        /// Henüz anlamı olmayan bilgi koşula bağlı: ilk baskın ikmal kanadıdır, sunucu ödülü çıkamaz (result taban metindir); önlem notu
        /// yalnız yürürlükte önlem varken ve önlem defteri açıkken çıkar.
        /// </summary>
        [Test] public void FacilityNotes_OnlyWhenTheyMeanSomething()
        {
            Loc.SetLanguage(Loc.Turkish);
            var sim=Facility();
            var first=sim.NextLiberationSector(sim.S.Liberation.CurrentRegion);
            Assert.IsTrue(sim.FacilityPlan.Wing(first.Id).All(r=>r.Mission=="loot"),"ilk baskın ikmal kanadına gider");
            Assert.AreEqual("",sim.Facility.PendingReward);
            Assert.AreEqual(BaseBody("result",sim),Body("result",sim),"result: ilk baskında ödül yok");
            StringAssert.Contains("ölen kişi geri gelmez",Body("result",sim));
            Assert.AreEqual(0,sim.Countermeasures.Count,"yeni oyunda önlem yok");
            Assert.AreEqual(BaseBody("raidTurn",sim),Body("raidTurn",sim),"raidTurn: önlem yokken taban metin");
            ForceCountermeasure(sim);
            Assert.AreEqual(1,sim.Countermeasures.Count);
            string turn=Body("raidTurn",sim);
            StringAssert.StartsWith(BaseBody("raidTurn",sim),turn);
            StringAssert.Contains("önlem",turn);
            var off=Facility(b=>b.facilityCountermeasures=false);ForceCountermeasure(off);
            Assert.AreEqual(BaseBody("raidTurn",off),Body("raidTurn",off),"önlem defteri kapalı: taban metin");
        }

        /// <summary>Alt bayrağı kapalı adım taban metne döner; açıkken tesis metnidir (unvan, kuşatma, saha şüphesi).</summary>
        [Test] public void FacilitySubFlagsOff_ReturnToTheBaseText()
        {
            Loc.SetLanguage(Loc.Turkish);
            var on=Facility(b=>b.infiltratorFieldEnabled=true);
            var off=Facility(b=>{b.facilityTitlesEnabled=false;b.facilityDefenseEnabled=false;b.infiltratorFieldEnabled=false;});
            foreach(var (id,key) in new[]{("report","unvan"),("goal","kuşatır"),("exposure","köpeğin")})
            {
                Assert.AreNotEqual(BaseBody(id,on),Body(id,on),id+": alt bayrak açık");StringAssert.Contains(key,Body(id,on),id);
                Assert.AreEqual(BaseBody(id,off),Body(id,off),id+": alt bayrak kapalı");StringAssert.DoesNotContain(key,Body(id,off),id);
            }
        }

        /// <summary>
        /// Rehberin andığı düğme ve damgalar oyundaki etiketlerle birebir (iki dilde): etiketin kendi çevirisi metnin içinde geçer;
        /// tipografik kesme yok.
        /// </summary>
        [Test] public void FacilityTexts_NameTheRealLabels_InBothLanguages()
        {
            var sim=Facility(b=>b.infiltratorFieldEnabled=true);ForceCountermeasure(sim);
            var labels=new[]{("back","MASAYA DÖN"),("slip","PLAN YOK"),("order","BASKIN EMRİ VER"),("order","BASKINA ÇIK"),("raidMove","GEÇ"),
                ("raidTurn","EMRİ İLET"),("report","EVRAK"),("goal","SAVUN"),("goal","BIRAK"),("exposure","KONUM ÇÖZÜLDÜ"),("go","KURALLAR"),("go","SONRAKİ")};
            try
            {
                foreach(bool english in new[]{false,true})
                {
                    if(english)UseEnglish();else Loc.SetLanguage(Loc.Turkish);
                    foreach(var (id,label) in labels)StringAssert.Contains(Loc.T(label),Body(id,sim),(english?"EN ":"TR ")+id+" · "+label);
                    foreach(var s in PlanningTutorialFlow.Steps)StringAssert.DoesNotContain("’",PlanningTutorialFlow.BodyText(s,sim),s.Id);
                }
                StringAssert.DoesNotContain("position",Body("exposure",sim),"oyunun terimi location");
                StringAssert.DoesNotContain("headquarters",Body("slip",sim));
            }
            finally{Loc.SetLanguage(Loc.Turkish);}
        }
        [Test] public void OldAdmissionPath_LeftNoGuideText()
        {
            foreach(var s in PlanningTutorialFlow.Steps)
                foreach(var word in new[]{"büyüteç","talimat","gözetmen","interkom","gözetleme","KISITLI","YARDIM"})
                    Assert.IsFalse(s.Body.Contains(word)||s.Title.Contains(word),s.Id+": eski kabul yolunun sözcüğü: "+word);
        }
    }
}
