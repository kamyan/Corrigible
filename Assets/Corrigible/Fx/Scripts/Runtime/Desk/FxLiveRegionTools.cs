using Corrigible.Desk;
using TMPro;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Canlı bölgenin fon katkısı, toparlanma ve sahaya ekip gönderme komutları.</summary>
    public sealed class FxLiveRegionTools : MonoBehaviour
    {
        [SerializeField] public FxDeskInteraction interaction;
        [SerializeField] public TMP_Text finance,recoveryText,diplomacyText;
        [SerializeField] public FxDeskCommandTarget recovery,diplomacy;
        [SerializeField] public FxDeskCommandTarget[] teams;
        [SerializeField] public TMP_Text[] teamNames;
        public void Bind(DossierVM data)
        {
            finance.text=data?.Finance==null?"":data.Finance.Contribution+"\n"+data.Finance.Note;
            var r=data?.Recovery;recoveryText.text=r?.RecoveryText??"";diplomacyText.text=r?.DiplomacyText??"";
            recovery.Bind("run",r?.Recovery?.Command,!string.IsNullOrEmpty(r?.Recovery?.Command));
            diplomacy.Bind("run",r?.Diplomacy?.Command,!string.IsNullOrEmpty(r?.Diplomacy?.Command));
            var ready=data?.Field?.Send;
            for(int n=0;n<teams.Length;n++)
            {
                var entry=ready!=null&&n<ready.Count?ready[n]:null;
                teams[n].gameObject.SetActive(entry!=null);
                if(entry==null)continue;
                teamNames[n].text=entry.Label;
                teams[n].Bind("run",entry.Command,!string.IsNullOrEmpty(entry.Command));
            }
        }
    }
}
