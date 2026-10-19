namespace Corrigible.Fx
{
    /// <summary>Grafik grubunun denetimi de ortak FX saatinden gelir; ayrı Update oluşturmaz.</summary>
    public sealed class FxGraphicBatchTick : FxTimedBehaviour
    {
        [UnityEngine.SerializeField] public FxGraphicBatch batch;
        public override void Tick(FxClock source){if(batch!=null)batch.Refresh();}
    }
}
