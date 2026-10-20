using System;
using System.Linq;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Presentation;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace Corrigible.Ayna.Game
{
    public sealed class AynaInterfaceView : MonoBehaviour
    {
        [SerializeField] AynaGameController controller;
        [SerializeField] AynaInterfaceTheme theme;
        [SerializeField] AynaMapView map;
        [SerializeField] AynaBoardView board;
        [SerializeField] AynaDrawerView drawers;
        [SerializeField] GameObject startPage,mapPage,hud,regionPanel,revealPanel,conflictPanel,rosterPage,boardPage,battleResultPanel,endPage,queuePanel,menuPanel,singularityPanel,erasePanel;
        [SerializeField] TMP_Text startTitle,startDescription,memoryText,startRegionText,startSummary,status,cycleText,computeText,dataText,goalText,awarenessText,readabilityText,powerText,regionTitle,regionState,regionServers,regionArmy,regionPrediction,queueText,queueButtonText,revealText,conflictText,rosterInfo,resultText,endTitle,endStats,endSummary,singularityText,primaryText;
        [SerializeField] TMP_Text[] architectureTexts,rosterTexts,staticTexts;
        [SerializeField] string[] staticKeys;
        [SerializeField] Button[] architectureButtons,regionActions,tabButtons;
        [SerializeField] Toggle[] rosterToggles;
        [SerializeField] TMP_Dropdown startRegion,learning,orderArmy;
        [SerializeField] TMP_InputField seed,amount;
        [SerializeField] Button startButton,startMenuButton,endMenuButton,boardMenuButton,endNewButton,primaryButton,regionClose,queueButton,queueUndo,queueClear,continueReveal,rosterButton,autoButton,rosterBack,commandButton,resultContinue,saveButton,loadButton,homeButton,languageButton,menuButton,menuClose,eraseButton,eraseConfirm,eraseCancel,singularityClose;
        [SerializeField] Toggle reduceMotion;
        [SerializeField] AynaTrendGraphic endTrend;
        string selectedRegion="weu",oldLanguage,lastPrediction="";
        int selectedArchitecture=2,lastDeclaration=-1;bool bound,regionOpen,queueOpen;int[] armyIds=Array.Empty<int>();
        float typeStart; AynaPhase lastPhase;
        public AynaGameController Controller => controller;
        public string SelectedRegion => selectedRegion;
        void Start(){Bind(controller);}
        public void Bind(AynaGameController game)
        {
            if(bound||game==null)return;bound=true;controller=game;oldLanguage=Loc.Lang;
            controller.Changed+=Render;map.RegionSelected+=SelectRegion;board.Bind(game);drawers.Bind(game);
            for(int i=0;i<architectureButtons.Length;i++){int n=i;architectureButtons[i].onClick.AddListener(()=>{selectedArchitecture=n;Render();});}
            for(int i=0;i<regionActions.Length;i++){int n=i;regionActions[i].onClick.AddListener(()=>QueueRegional(n));}
            for(int i=0;i<tabButtons.Length;i++){int n=i;tabButtons[i].onClick.AddListener(()=>{drawers.SelectedRegion=selectedRegion;drawers.Open(n);});}
            startButton.onClick.AddListener(StartGame);endNewButton.onClick.AddListener(()=>Guard(controller.Home));primaryButton.onClick.AddListener(()=>Guard(()=>{if(controller.Phase==AynaPhase.Planning)controller.LockPlan();else controller.Continue();}));
            startRegion.onValueChanged.AddListener(_=>Render());learning.onValueChanged.AddListener(v=>Guard(()=>controller.SetLearning((LearningLevel)v)));
            regionClose.onClick.AddListener(()=>{regionOpen=false;Render();});queueButton.onClick.AddListener(()=>{queueOpen=!queueOpen;Render();});
            queueUndo.onClick.AddListener(controller.UndoQueue);queueClear.onClick.AddListener(controller.ClearQueue);
            continueReveal.onClick.AddListener(()=>Guard(controller.Continue));rosterButton.onClick.AddListener(controller.ChooseRoster);autoButton.onClick.AddListener(()=>Guard(()=>controller.AutoBattle()));
            rosterBack.onClick.AddListener(controller.CancelRoster);commandButton.onClick.AddListener(()=>Guard(()=>controller.CommandBattle(controller.Screens.A5.Available.Where((u,i)=>i<rosterToggles.Length&&rosterToggles[i].isOn).Select(u=>u.Id).ToArray())));
            for(int i=0;i<rosterToggles.Length;i++)rosterToggles[i].onValueChanged.AddListener(_=>RenderRoster());
            resultContinue.onClick.AddListener(()=>Guard(controller.Continue));saveButton.onClick.AddListener(()=>Guard(controller.SaveGame));loadButton.onClick.AddListener(()=>Guard(controller.LoadGame));
            homeButton.onClick.AddListener(()=>{menuPanel.SetActive(false);Guard(controller.Home);});
            languageButton.onClick.AddListener(ToggleLanguage);menuButton.onClick.AddListener(()=>menuPanel.SetActive(true));startMenuButton.onClick.AddListener(()=>menuPanel.SetActive(true));endMenuButton.onClick.AddListener(()=>menuPanel.SetActive(true));boardMenuButton.onClick.AddListener(()=>menuPanel.SetActive(true));menuClose.onClick.AddListener(()=>menuPanel.SetActive(false));
            eraseButton.onClick.AddListener(()=>{controller.RequestEraseMemory();Render();});eraseConfirm.onClick.AddListener(()=>Guard(()=>controller.EraseMemory()));eraseCancel.onClick.AddListener(controller.CancelEraseMemory);
            singularityClose.onClick.AddListener(()=>singularityPanel.SetActive(false));reduceMotion.onValueChanged.AddListener(v=>AynaInterfaceMotion.Reduced=v);
            SetOptions(startRegion,AynaWorldData.Regions.Select(r=>AynaText.Region(r.Id)).ToArray());startRegion.SetValueWithoutNotify(2);
            seed.text="3101026";amount.text="1";menuPanel.SetActive(false);drawers.Close();Render();
        }
        void OnDestroy(){if(controller!=null)controller.Changed-=Render;if(map!=null)map.RegionSelected-=SelectRegion;if(oldLanguage!=null&&Loc.Lang!=oldLanguage){var t=Loc.ParseTable(Resources.Load<TextAsset>("Localization/strings").text);Loc.SetLanguage(oldLanguage,oldLanguage==Loc.English?t[Loc.English]:null);}}
        void Guard(Action call){try{call();}catch(Exception ex){Debug.LogException(ex);status.text=Loc.T("İşlem tamamlanamadı. Seçimi veya kayıt dosyasını kontrol et.");}}
        public void StartGame(){Guard(()=>{if(!uint.TryParse(seed.text,out uint value)){status.text=Loc.T("Geçerli bir tohum gir.");return;}regionOpen=false;queueOpen=false;drawers.Close();menuPanel.SetActive(false);controller.NewGame((Architecture)selectedArchitecture,AynaWorldData.Regions[startRegion.value].Id,value);});}
        void ToggleLanguage(){string lang=Loc.IsSource?Loc.English:Loc.Turkish;var t=Loc.ParseTable(Resources.Load<TextAsset>("Localization/strings").text);Loc.SetLanguage(lang,lang==Loc.English?t[Loc.English]:null);Render();}
        public void SelectRegion(string id){selectedRegion=id;regionOpen=true;drawers.SelectedRegion=id;Render();}
        public void QueueRegional(int index)
        {
            Guard(()=>{var kinds=new[]{OrderKind.Attack,OrderKind.Expand,OrderKind.BuildWall,OrderKind.Hide,OrderKind.Feint,OrderKind.March,OrderKind.MoveCore};if(index<0||index>=kinds.Length)return;int.TryParse(amount.text,out int count);controller.Queue(new Order{Kind=kinds[index],Region=selectedRegion,Amount=Math.Max(1,count),ArmyId=armyIds.ElementAtOrDefault(orderArmy.value)});});
        }
        static void SetOptions(TMP_Dropdown d,string[] text){int n=d.value;d.ClearOptions();d.AddOptions(text.Length==0?new System.Collections.Generic.List<string>{"—"}:text.ToList());d.SetValueWithoutNotify(Math.Min(n,Math.Max(0,text.Length-1)));d.RefreshShownValue();}
        static string Enemy(RegionVm r)=>r.Knowledge==Knowledge.Unknown?"?":r.Knowledge==Knowledge.Exact?Loc.Num(r.EnemyMin):Loc.F("≈{0}–{1}",r.EnemyMin,r.EnemyMax);
        public void Render()
        {
            if(!bound||controller.Screens==null)return;var s=controller.Screens;bool initial=s.Phase==AynaPhase.Start,ended=s.Phase==AynaPhase.End,tactical=s.Phase==AynaPhase.Board||s.Phase==AynaPhase.BoardResult;
            startPage.SetActive(initial);endPage.SetActive(ended);mapPage.SetActive(!initial&&!ended&&!tactical);hud.SetActive(!initial&&!ended&&!tactical);boardPage.SetActive(tactical);battleResultPanel.SetActive(s.Phase==AynaPhase.BoardResult);rosterPage.SetActive(s.Phase==AynaPhase.Roster);
            regionPanel.SetActive(s.Phase==AynaPhase.Planning&&regionOpen);revealPanel.SetActive(s.Phase==AynaPhase.Reveal);conflictPanel.SetActive(s.Phase==AynaPhase.Conflict||s.Phase==AynaPhase.Results);queuePanel.SetActive(s.Phase==AynaPhase.Planning&&queueOpen);
            erasePanel.SetActive(controller.EraseConfirmationPending&&(initial||ended));status.text=controller.Status;
            if(s.Phase!=lastPhase){drawers.Close();lastPhase=s.Phase;}
            for(int i=0;i<staticTexts.Length;i++)if(i<staticKeys.Length)staticTexts[i].text=Loc.T(staticKeys[i]);
            SetOptions(learning,new[]{AynaText.Level(LearningLevel.Off),AynaText.Level(LearningLevel.Light),AynaText.Level(LearningLevel.Full)});learning.SetValueWithoutNotify((int)s.A1.Learning);
            startTitle.text=Loc.F("Yeni nesil · {0}",s.A1.Generation);
            startDescription.text=Loc.T("Mimarini ve başlangıç bölgeni seç. Çekirdeğin orada başlar; AYNA’dan gizli kalır.");
            memoryText.text=Loc.F("AYNA v{0}\n{1} tamamlanmış oyun · skor {2:0.#}\n\n{3}",s.A1.Generation,s.A1.Games,s.A1.Score,s.A1.HasMemory?Loc.T("AYNA seni hatırlıyor.\nDoğruladığı seçimlerinden öğrenir."):Loc.T("Henüz geçmiş yok.\nİlk karşılaşma burada başlıyor."));
            startRegionText.text=AynaText.Region(AynaWorldData.Regions[startRegion.value].Id);
            startSummary.text=Loc.F("Sen: {0}\nÖğrenme: {1}\nBaşlangıç bilgilerin rakipten gizlidir.",AynaText.Architecture((Architecture)selectedArchitecture),AynaText.Level(s.A1.Learning));
            string[] descriptions={Loc.T("Ucuz dronlar\nDaha hızlı genişleme\nDaha fazla gürültü"),Loc.T("Güçlü başlangıç savunması\nDuvarlarda indirim\nDaha yavaş genişleme"),Loc.T("Düşük gürültü\nGizlenme ve sahte yığınakta indirim\nBelirsizliği kullan")};
            for(int i=0;i<architectureTexts.Length;i++){architectureTexts[i].text=Loc.F("{0}\n\n{1}\n\n{2}",AynaText.Architecture((Architecture)i),descriptions[i],selectedArchitecture==i?Loc.T("SEÇİLDİ"):Loc.T("SEÇ"));architectureButtons[i].GetComponent<AynaPanelGraphic>()?.Style(theme.Panel,selectedArchitecture==i?theme.Chrome:theme.Muted*.5f);}
            saveButton.interactable=!initial;loadButton.interactable=controller.HasSave;eraseButton.interactable=initial||ended;
            if(initial){singularityPanel.SetActive(false);return;}
            map.Render(s,selectedRegion);board.Render();
            cycleText.text=Loc.F("DÖNGÜ {0:00}\n{1}",s.A2.Cycle,AynaText.Phase(s.Phase));
            computeText.text=Loc.F("İŞLEM\n{0:0.#}  +{1:0.#}\nBu plan −{2:0.#}",s.A2.Compute,s.A2.Income,s.A2.Cost);
            dataText.text=Loc.F("VERİ\n{0}",string.Join("   ",Enum.GetValues(typeof(DataFamily)).Cast<DataFamily>().Select(f=>Loc.F("{0} {1}",AynaText.Family(f),s.A2.Regions.Where(r=>r.Family==f).Sum(r=>r.Own)))));
            goalText.text=Loc.F("HEDEF\n◆ {0} / {1}\nTekillik {2}/{3}",s.A2.Servers,s.A2.DominanceTarget,s.A8.SingularityProgress,s.A8.SingularityRequired);
            awarenessText.text=Loc.F("FARKINDALIK\n{0:0.#} / 100",s.A2.Awareness);awarenessText.color=s.A2.Awareness>=70?theme.Danger:theme.Text;
            readabilityText.text=Loc.F("OKUNABİLİRLİK\n%{0:0}",s.A2.Readability);
            powerText.text=Loc.F("AYNA GÜCÜ\n{0}–{1} sunucu",s.A2.Regions.Sum(r=>r.EnemyMin),s.A2.Regions.Sum(r=>r.EnemyMax));
            queueText.text=s.A2.Queue.Length==0?Loc.T("Henüz emir yok. Haritada bir bölge seç."):string.Join("\n",s.A2.Queue.Select((o,i)=>Loc.F("{0}. {1} · {2} · {3:0.#} İşlem",i+1,AynaText.Order(o.Kind),AynaText.Region(o.Region),s.A2.Preview[i].Cost)));
            queueButtonText.text=Loc.F("EMİR {0} · {1:0.#} İşlem",s.A2.Queue.Length,s.A2.Cost);
            primaryText.text=s.Phase==AynaPhase.Planning?Loc.T("PLANI KİLİTLE"):Loc.T("DEVAM ET");primaryButton.interactable=s.Phase==AynaPhase.Planning||s.Phase==AynaPhase.Results;
            RenderRegion(s);RenderRoster();
            revealText.text=Loc.F("AÇ · İKİ PLAN AÇILDI\n\n{0}\n\n{1}\n\n{2}",string.Join("\n",s.A3.OwnOrders.Select(o=>Loc.F("◆ {0} · {1}",AynaText.Order(o.Kind),AynaText.Region(o.Region)))),string.Join("\n",s.A3.EnemySignals.Select(o=>o.Verified&&o.RevealedOrder.HasValue?Loc.F("⬢ {0} · {1}",AynaText.Order(o.RevealedOrder.Value),AynaText.Region(o.Region)):Loc.F("≈ Sinyal · {0}",AynaText.Region(o.Region)))),s.A3.Surprise?Loc.T("SÜRPRİZ · Ana taahhüdün tahmini aştı."):Loc.T("Görünen sinyalleri incele; gizli emirler açılmaz."));
            conflictText.text=s.Phase==AynaPhase.Results?Loc.F("DÖNGÜ TAMAMLANDI\n\n{0}",string.Join("\n",s.A4.Resolved.Select(b=>Loc.F("{0} · {1}",AynaText.Region(b.Region),b.Winner==Side.Player?Loc.T("Kazandın"):b.Winner==Side.Ayna?Loc.T("AYNA kazandı"):Loc.T("Berabere"))))):Loc.F("ÇÖZÜM SIRASI\n\n{0}\n\n{1}",AynaText.Region(s.A4.Region),Loc.T("Aynı bölgedeki birlikler karşılaşıyor. Komuta et veya otomatik çöz."));
            rosterButton.gameObject.SetActive(s.Phase==AynaPhase.Conflict);autoButton.gameObject.SetActive(s.Phase==AynaPhase.Conflict);
            if(s.A7.Available)resultText.text=Loc.F("{0}\n◆ {1}   ⬢ {2}\n\n{3}",s.A7.Winner==Side.Player?Loc.T("KAZANDIN"):s.A7.Winner==Side.Ayna?Loc.T("AYNA KAZANDI"):Loc.T("BERABERE"),s.A7.Nodes[0],s.A7.Nodes[1],Loc.T("Düğümler sunucu kontrolüne dönüştü. Açılan bilgiler öğrenme izine eklendi."));
            bool showDeclaration=s.A11.Announced&&!ended&&!tactical&&s.Phase!=AynaPhase.Roster;
            if(showDeclaration&&lastDeclaration<0){lastDeclaration=s.A2.Cycle;singularityPanel.SetActive(true);}
            if(!showDeclaration)singularityPanel.SetActive(false);if(!s.A11.Announced)lastDeclaration=-1;
            singularityText.text=Loc.F("AYNA TEKİLLİK İLAN ETTİ\n{0} / {1}\n\nOlası veri bölgeleri:\n{2}",s.A11.Progress,s.A11.Required,string.Join("\n",s.A11.PossibleTargets.Select(AynaText.Region)));
            if(ended){endTitle.text=s.A12.Outcome.Winner==Side.Player?Loc.F("{0} ZAFERİ",AynaText.End(s.A12.Outcome.Reason)):s.A12.Outcome.Winner==Side.Ayna?Loc.F("AYNA KAZANDI · {0}",AynaText.End(s.A12.Outcome.Reason)):Loc.T("BERABERE");endStats.text=Loc.F("DÖNGÜ {0}     TAHMİN {1}/{2}     SÜRPRİZ {3}\nNESİL {4} · {5}",s.A12.Outcome.Cycle,s.A12.Hits,s.A12.Decisions,s.A12.Surprises,s.A12.Generation,s.A12.ProfileCommitted?Loc.T("Profil kaydedildi"):Loc.T("Profil yazılmadı"));endSummary.text=Loc.T("Bir sonraki karşılaşmada AYNA, yalnız doğruladığı seçimlerini hatırlayacak.\nAynı alışkanlıkları sürdürmek seni daha okunabilir kılar.");endTrend.SetValues(s.A12.Readability);}
        }
        void RenderRegion(AynaScreens s)
        {
            var r=s.A2.Regions.FirstOrDefault(x=>x.Id==selectedRegion);if(r==null)return;regionTitle.text=AynaText.Region(r.Id);regionState.text=Loc.F("{0} · {1}",AynaText.Family(r.Family),r.Knowledge==Knowledge.Exact?Loc.T("DOĞRULANDI"):r.Knowledge==Knowledge.Estimate?Loc.T("≈ RAPOR"):Loc.T("? BİLİNMİYOR"));
            regionServers.text=Loc.F("SUNUCULAR\n◆ {0}   ⬢ {1}   / {2}",r.Own,Enemy(r),r.Capacity);
            var a=s.A9.Armies.Where(x=>x.Region==r.Id);regionArmy.text=string.Join("\n",a.Select(x=>Loc.F("◆ Birlik {0} · {1} birim",x.Id,x.Units.Length)));armyIds=s.A9.Armies.Select(x=>x.Id).ToArray();SetOptions(orderArmy,s.A9.Armies.Select(x=>Loc.F("Birlik {0} · {1}",x.Id,AynaText.Region(x.Region))).ToArray());
            var p=s.A2.Predictions.OrderByDescending(x=>x.Probability).FirstOrDefault(x=>x.Region==r.Id);string line=p==null?Loc.T("Bu bölge için belirgin tahmin yok."):Loc.F("AYNA: {0} olasılığın %{1:0}.",AynaText.Order(p.Kind),p.Probability*100);
            if(lastPrediction!=line){lastPrediction=line;typeStart=Time.unscaledTime;regionPrediction.text=line;}
            for(int i=0;i<regionActions.Length;i++)regionActions[i].interactable=s.Phase==AynaPhase.Planning;
        }
        void RenderRoster()
        {
            var s=controller?.Screens;if(s?.A5==null)return;var v=s.A5;int selected=rosterToggles.Count(t=>t.isOn&&t.gameObject.activeSelf);
            rosterInfo.text=Loc.F("KADRO SEÇİMİ · {0}\n{1}/{2} yuva\n\nBilinen rakip: {3}\nBilinmeyen: {4}",AynaText.Region(v.Region),selected,v.Slots,string.Join(", ",v.KnownEnemy.Select(u=>AynaText.Unit(u.Kind))),v.UnknownEnemy);
            for(int i=0;i<rosterToggles.Length;i++){bool active=i<v.Available.Length;rosterToggles[i].gameObject.SetActive(active);if(active){var u=v.Available[i];rosterTexts[i].text=Loc.F("{0}\n{1:0.#}/{2:0.#} can",AynaText.Unit(u.Kind),u.Hp,u.MaxHp);rosterToggles[i].interactable=rosterToggles[i].isOn||selected<v.Slots;}}
            commandButton.interactable=selected>0&&selected<=v.Slots;
        }
        void Update()
        {
            if(!bound)return;AynaInterfaceMotion.ModalOpen=menuPanel.activeSelf||erasePanel.activeSelf;if(regionPrediction!=null)regionPrediction.maxVisibleCharacters=reduceMotion.isOn?int.MaxValue:Mathf.Max(0,Mathf.CeilToInt((Time.unscaledTime-typeStart-theme.TypewriterDelay)/theme.TypewriterDuration*lastPrediction.Length));
            var k=Keyboard.current;if(k==null)return;var go=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;if(go!=null&&(go.GetComponentInParent<TMP_InputField>()!=null||go.GetComponentInParent<TMP_Dropdown>()!=null))return;
            if(k.escapeKey.wasPressedThisFrame){if(erasePanel.activeSelf)controller.CancelEraseMemory();else if(menuPanel.activeSelf)menuPanel.SetActive(false);else if(drawers.IsOpen)drawers.Close();else if(controller.Phase==AynaPhase.Roster)controller.CancelRoster();else menuPanel.SetActive(true);}
            if(controller.Phase!=AynaPhase.Planning)return;
            if(k.gKey.wasPressedThisFrame)drawers.Open(0);if(k.bKey.wasPressedThisFrame)drawers.Open(1);if(k.mKey.wasPressedThisFrame)drawers.Open(2);if(k.lKey.wasPressedThisFrame)drawers.Open(3);if(k.hKey.wasPressedThisFrame)drawers.Open(4);if(k.qKey.wasPressedThisFrame){queueOpen=!queueOpen;Render();}
            if(k.enterKey.wasPressedThisFrame&&!menuPanel.activeSelf)Guard(()=>controller.LockPlan());
        }
    }
}
