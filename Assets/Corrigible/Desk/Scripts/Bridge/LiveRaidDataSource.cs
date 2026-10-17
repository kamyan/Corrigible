using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Corrigible.Desk.Bridge
{
    public sealed class LiveRaidDataSource : RaidDataSource
    {
        [SerializeField] GameController game;
        bool finished,previousHold;
        readonly List<DeskInput> suspendedInputs=new List<DeskInput>();
        public bool ResultReported=>finished;
        protected override void Start()
        {
            // GameController eklemeli yüklenen ana sahnededir; ada göre nesne araması yapılmaz.
            if(game==null)game=FindFirstObjectByType<GameController>();
            foreach(var input in FindObjectsByType<DeskInput>(FindObjectsSortMode.None))if(input.enabled){suspendedInputs.Add(input);input.enabled=false;}
            if(game!=null){previousHold=game.Hold;game.Hold=true;game.SetSpeed(0);}
            var p=game?.Sim?.S.Raid?.Params??new RaidParams{Seed=731,Region="cn",RegionName=GameData.Region("cn").Name,Compute=5,IsCore=true,Inf=60,Traits=new List<string>{"aldatma"}};
            OpenState(Baskin.Create(p));
        }
        public void OpenForValidation(Baskin.BState state)=>OpenState(state);
        protected override void Finish()
        {
            if(finished)return;
            if(game?.Sim?.S.Raid==null){finished=true;SceneManager.LoadScene(gameObject.scene.name);return;}
            var result=Baskin.Result(State);var resolved=game.ResolveRaid(result);
            if(!resolved.Ok){Debug.LogError(resolved.Msg);return;}
            finished=true;game.Hold=previousHold;
            // Sonucu bir kez kaydet, taktik sahneyi kapat; RaidLauncher ana masayı geri açar.
            if(gameObject.scene.isLoaded&&SceneManager.sceneCount>1)SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        void OnDestroy(){foreach(var input in suspendedInputs)if(input!=null)input.enabled=true;if(game!=null&&!finished)game.Hold=previousHold;}
    }
}
