using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Baskın tahtasının teması (Docs/UI_Sade › spec/raid/raid_tokens.json): 3D malzeme renkleri, işaret ve niyet renkleri,
    /// kamera ve hareket sayıları JSON yoluyla. Sade HUD renkleri <see cref="SadeTheme"/>'de (“raid.” dalı).
    /// İçeriği “Corrigible/Sade UI/Tokenları içe aktar” doldurur.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Raid Theme", fileName = "RaidTheme")]
    public sealed class RaidTheme : TokenTheme
    {
        static RaidTheme active;
        public static RaidTheme Active
        {
            get { if (active == null) active = Resources.Load<RaidTheme>("RaidTheme"); return active; }
            set => active = value;
        }
    }
}
