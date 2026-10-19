using Corrigible.Core;
using Corrigible.Game;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Eklemeli FX baskınının gerçek oyun bağlantısı. Örnek durum üretmez; sonucu GameController kaydeder.</summary>
    public sealed class FxLiveRaidSource : FxRaidDataSource
    {
        [SerializeField] GameController game;
        bool finished;
        public bool ResultReported => finished;
        protected override void Start()
        {
            if(game==null)game=FindFirstObjectByType<GameController>();
            var pending=game?.Sim?.S.Raid;
            if(pending==null){Debug.LogError("Canlı FX baskını için bekleyen operasyon gerekli.",this);return;}
            game.SetSpeed(0);animateActions=false;OpenState(Baskin.Create(pending.Params));
        }
        protected override void Finish()
        {
            if(finished||MachineRunning||game?.Sim?.S.Raid==null)return;
            var result=game.ResolveRaid(Baskin.Result(State));
            if(!result.Ok){Debug.LogError(result.Msg,this);return;}
            // Sahneyi RaidLauncher kapatır; ikinci bir kapatma veya rapor komutu gönderilmez.
            finished=true;
        }
    }
}
