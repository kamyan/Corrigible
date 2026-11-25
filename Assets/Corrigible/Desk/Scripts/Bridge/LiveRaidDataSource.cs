using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Corrigible.Desk.Bridge
{
    public sealed class LiveRaidDataSource : RaidDataSource
    {
        [SerializeField] GameController game;
        bool finished;
        readonly List<DeskInput> suspendedInputs=new List<DeskInput>();
        public bool ResultReported=>finished;
        protected override void Start()
        {
            // GameController eklemeli yüklenen ana sahnededir; ada göre nesne araması yapılmaz.
            if(game==null)game=FindFirstObjectByType<GameController>();
            foreach(var input in FindObjectsByType<DeskInput>(FindObjectsSortMode.None))if(input.enabled){suspendedInputs.Add(input);input.enabled=false;}
            // Harekât baskını hep tesis baskınıdır (emirde kanadın üç odası): içinde bulunulan oda raporlardan, kayıttan devamda baştan.
            // Sonuç kartında çıkılıp devam edildiyse (son oda raporlandı) kart gösterilmez: sonuç yazılır, masada operasyon raporu açılır.
            var sim=game?.Sim;
            if(sim?.S.Raid!=null)
            {
                // Odasız emir olmamalı (Core her emre üç oda yazar); olursa tahta açılmaz, sahne kapanır
                if(!FacilityRaidSession.Applies(sim)){Debug.LogError("Baskının odaları yok: tesis baskını açılamadı.");Unload();return;}
                if(sim.RaidRoomsClosed){ResolveClosedRaid();return;}
                OpenFacility(new FacilityRaidSession(sim,game.AdvanceRaidRoom));
                return;
            }
            // Baskin sahnesi tek başına açıldıysa (ana masa ya da baskın yok) örnek bir sunucu baskını kurulur: MapSeed 0, elle çizilmiş
            // haritalardan biri (Baskin.Create örnek tahtası), varsayılan bina kiti
            OpenState(Baskin.Create(new RaidParams{Seed=731,Region="cn",RegionName=SampleMap.RegionName(null,"cn"),Compute=5,IsCore=true,Tier=3,RaidNumber=3,
                Traits=new List<string>{GameData.Region("cn").Trait,"kopya"},Signature=GameData.Region("cn").Enemy}));
        }
        public void OpenForValidation(Baskin.BState state){CloseFacility();OpenState(state);}
        /// <summary>EditMode testleri: tesis baskınını sahnede, GameController'sız açar (raid_room doğrudan simülasyona).</summary>
        public void OpenFacilityForValidation(Simulation sim)=>OpenFacility(new FacilityRaidSession(sim,sim.AdvanceRaidRoom));
        /// <summary>Son oda raporlanmış tesis baskını (sonuç kartında çıkıldı): tahta açılmadan sonuç yazılır, sahne kapanır.</summary>
        void ResolveClosedRaid()
        {
            var resolved=game.ResolveRaid(new RaidResult{Reason=""});
            if(!resolved.Ok){Debug.LogError(resolved.Msg);return;}
            finished=true;
            Unload();
        }
        /// <summary>Başlatıcısız eklemeli sahnede sahneyi kapatır (başlatıcı yönetiyorsa o kapatır).</summary>
        void Unload(){if(!RaidLauncher.ManagesScene(gameObject.scene.name)&&gameObject.scene.isLoaded&&SceneManager.sceneCount>1)SceneManager.UnloadSceneAsync(gameObject.scene);}
        // Sonuç kartının yer satırı: bölge · sıra sektör (vardiya raporuyla aynı biçim)
        protected override string ResultPlace{get{var sector=game?.Sim?.LiberationSector(State.Params.SectorId);return sector==null?base.ResultPlace:Loc.F("{0} · {1:00} {2}",game.Sim.RegionName(sector.Region),sector.Index+1,sector.Name);}}
        protected override string RegionTitle=>game?.Sim!=null&&GameData.Regions.Any(d=>d.Id==State.Params.Region)?game.Sim.RegionName(State.Params.Region):base.RegionTitle;
        protected override (int Trust,int Max,int PerCivilian) TrustStake=>game?.Sim==null?(0,0,0):(game.Sim.S.Trust,game.Sim.TrustMax,game.Sim.B.liberationTrustPerCivilian);
        protected override int BlueprintShift=>game?.Sim==null?0:game.Sim.S.Day+1;
        protected override string CrateContents=>game?.Sim?.Board==null?base.CrateContents:
            Loc.F("KASADA: {0} metal · {1} devre · {2} kumaş · {3} kimyasal · {4} erzak",game.Sim.B.boardLootMetal,game.Sim.B.boardLootCircuits,game.Sim.B.boardLootCloth,game.Sim.B.boardLootChemicals,game.Sim.B.boardLootSupplies);
        protected override string EquipmentLabels(Baskin.Unit u)
        {
            if(game?.Sim?.Board==null||string.IsNullOrEmpty(u.PersonId))return base.EquipmentLabels(u);
            return string.Join(Loc.T(" · "),game.Sim.Board.Equipment.Where(e=>e.OwnerId==u.PersonId)
                .Select(e=>e.Kind=="emp_cell"&&u.EmpBoosts<=0?Loc.F("<s>{0}</s>",PlanningBoardData.EquipmentName(e.Kind))
                    :e.Kind=="armor"&&u.Armor<=0?Loc.F("<s>{0}</s>",PlanningBoardData.EquipmentName(e.Kind))
                    // Tesis aşama 9: ekipman kalan hakkıyla, hak bitince üstü çizili; köpek ve baskına götürülmeyen eşya yalnız adı
                    :FacilityGear.IsGear(e.Kind)?GearLabel(u,e.Kind):PlanningBoardData.EquipmentName(e.Kind)));
        }
        protected override void Finish()
        {
            if(finished)return;
            if(game?.Sim?.S.Raid==null){finished=true;SceneManager.LoadScene(gameObject.scene.name);return;}
            // Tesis baskını: sonuç raporlardan toplanır (Core yalnız Reason'ı alır); son oda raporlanmadan istenmez (olmamalı)
            if(Session!=null&&!game.Sim.RaidRoomsClosed){Debug.LogError("Tesis baskını: son oda raporlanmadan sonuç istendi.");return;}
            var result=State!=null?Baskin.Result(State):new RaidResult{Reason=""};var resolved=game.ResolveRaid(result);
            if(!resolved.Ok){Debug.LogError(resolved.Msg);return;}
            finished=true;
            // ResolveRaid'in Changed olayı başlatıcıya kapatma emrini zaten verdi. Aynı sahneyi ikinci kez kapatma.
            // Başlatıcısız eklemeli test/entegrasyon sahnelerinde eski bağımsız kapatma yolu sürer.
            Unload();
        }
        void OnDestroy(){foreach(var input in suspendedInputs)if(input!=null)input.enabled=true;}
    }
}
