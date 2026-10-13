using System;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Game.Campaign
{
    /// <summary>Krizler arasında seçilen ajans yükseltmesi: denge değerlerini (Balance) ya da başlangıç koşullarını değiştirir.</summary>
    public sealed class CampaignUpgrade
    {
        public string Id, Title, Desc;
        public bool NeedsTeams;
        public Action<Balance, ExpOptions> Apply;

        public static CampaignUpgrade Get(string id) => All.FirstOrDefault(u => u.Id == id);

        public static readonly CampaignUpgrade[] All =
        {
            new CampaignUpgrade { Id = "butce", Title = Loc.N("Ek bütçe"), Desc = Loc.N("Her krize 40 fon fazlasıyla başla."), Apply = (b, e) => b.startFon += 40 },
            new CampaignUpgrade { Id = "lobi", Title = Loc.N("Meclis lobisi"), Desc = Loc.N("Her krize 4 siyasi güç fazlasıyla başla."), Apply = (b, e) => b.startSg += 4 },
            new CampaignUpgrade { Id = "sensor", Title = Loc.N("Ucuz sensörler"), Desc = Loc.N("İzleme ağları %30 daha ucuz."), Apply = (b, e) => b.monitorCostMul *= 0.7 },
            new CampaignUpgrade { Id = "tarama", Title = Loc.N("Derin tarama"), Desc = Loc.N("Denetim sızmayı 20 yerine 26 düşürür."), Apply = (b, e) => b.audit += 6 },
            new CampaignUpgrade { Id = "hizalama", Title = Loc.N("Hizalama ekibi"), Desc = Loc.N("Her gözlem noktası Hizalama'yı daha çok hızlandırır."), Apply = (b, e) => b.alignPer += 0.06 },
            new CampaignUpgrade { Id = "iletisim", Title = Loc.N("İletişim ofisi"), Desc = Loc.N("Hediye reddetmenin güven ve siyasi güç bedeli azalır."), Apply = (b, e) => { b.rejectTrust = 0; b.rejectSg = 1; } },
            new CampaignUpgrade { Id = "guvenlik", Title = Loc.N("Laboratuvar güvenliği"), Desc = Loc.N("YZ'nin araştırmayı sabote etme ihtimali %40 azalır."), Apply = (b, e) => b.sabotage *= 0.6 },
            new CampaignUpgrade { Id = "yama", Title = Loc.N("Kalıcı yamalar"), Desc = Loc.N("Denetim yaması 20 yerine 30 gün sürer."), Apply = (b, e) => b.patchDays += 10 },
            new CampaignUpgrade { Id = "yasa", Title = Loc.N("Acil durum yasası"), Desc = Loc.N("Son Şalter veto eşiği 8 puan yükselir."), Apply = (b, e) => { b.vetoBase += 8; b.vetoAnalog += 8; } },
            new CampaignUpgrade { Id = "fon", Title = Loc.N("Uluslararası fon"), Desc = Loc.N("Günlük fon geliri %15 artar."), Apply = (b, e) => b.fonDiv *= 0.85 },
            new CampaignUpgrade { Id = "erken", Title = Loc.N("Erken uyarı bütçesi"), Desc = Loc.N("YZ fark edilmeden önceki gelir %40 yerine %60."), Apply = (b, e) => b.preAwareFund = 0.6 },
            new CampaignUpgrade { Id = "ekip", Title = Loc.N("Dördüncü saha ekibi"), Desc = Loc.N("Her krize 4 saha ekibiyle başla."), NeedsTeams = true, Apply = (b, e) => { if (e != null) e.StartTeams = Math.Max(e.StartTeams, 4); } },
        };
    }
}
